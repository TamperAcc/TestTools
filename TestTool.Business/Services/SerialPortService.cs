using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using TestTool.Business.Enums;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 串口服务实现：封装串口打开/关闭、发送、接收和状态通知逻辑
    /// 使用 ISerialPortAdapter 以便于后续替换底层实现
    /// </summary>
    public class SerialPortService : ISerialPortService
    {
        private readonly IOptionsMonitor<AppConfig>? _appConfigMonitor;
        private readonly object _policyLock = new();
        private AsyncRetryPolicy<bool>? _connectRetryPolicy;
        private AsyncRetryPolicy<bool>? _sendRetryPolicy;
        private int _currentConnectRetries;
        private int _currentSendRetries;
        private int _currentBaseDelayMs;
        private DateTime _lastPolicyUpdateUtc;
        private int _policyUpdateErrors;
        private readonly TimeSpan _policyDebounce = TimeSpan.FromSeconds(1);
        private IDisposable? _optionsChangeToken;
        
        /// <summary>
        /// 用于串口连接操作的互斥锁，避免并发连接/断开造成竞态
        /// </summary>
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        
        /// <summary>
        /// 底层串口适配器（可为 null，表示未连接）
        /// </summary>
        private ISerialPortAdapter? _adapter;
        
        /// <summary>
        /// Channel 用于异步方式传递数据
        /// </summary>
        private Channel<string>? _receiveChannel;
        
        /// <summary>
        /// 当前连接状态（本地缓存）
        /// </summary>
        private ConnectionState _currentState = ConnectionState.Disconnected;
        
        /// <summary>
        /// 当前连接配置（可空）
        /// </summary>
        private ConnectionConfig? _currentConfig;
        
        /// <summary>
        /// 标记服务是否已释放（Dispose 调用过后）
        /// </summary>
        private bool _disposed;
        
        private readonly ILogger<SerialPortService> _logger;
        private readonly ISerialPortAdapterFactory _adapterFactory;

        /// <summary>
        /// 连接状态变化事件，供上层订阅 UI 更新
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        
        /// <summary>
        /// 数据接收事件，当串口收到数据时触发
        /// </summary>
        public event EventHandler<DataReceivedEventArgs>? DataReceived;
        
        /// <summary>
        /// 数据发送事件，在成功写入串口后触发（可用于监控和记录数据）
        /// </summary>
        public event EventHandler<DataSentEventArgs>? DataSent;

        /// <summary>
        /// 是否已连接，根据底层 Adapter 的 IsOpen 判断
        /// </summary>
        public bool IsConnected => _adapter?.IsOpen ?? false;
        
        /// <summary>
        /// 当前配置（可空）
        /// </summary>
        public ConnectionConfig? CurrentConfig => _currentConfig;
        
        /// <summary>
        /// 当前连接状态
        /// </summary>
        public ConnectionState CurrentState => _currentState;

        /// <summary>
        /// 通过 DI 注入 ILogger，适配器工厂和配置监视
        /// </summary>
        public SerialPortService(ILogger<SerialPortService> logger, ISerialPortAdapterFactory adapterFactory, IOptionsMonitor<AppConfig>? appConfigMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
            _appConfigMonitor = appConfigMonitor;

            // 从配置初始化重试策略，如果没有配置则使用默认值
            var cfg = _appConfigMonitor?.CurrentValue?.RetryPolicy;
            var connectRetries = cfg?.ConnectRetries ?? 3;
            var sendRetries = cfg?.SendRetries ?? 2;
            var baseDelayMs = cfg?.BaseDelayMs ?? 200;
            _currentConnectRetries = connectRetries;
            _currentSendRetries = sendRetries;
            _currentBaseDelayMs = baseDelayMs;

            // 使用 HandleResult<TResult> 在返回 false 时重试，并使用 Or<Exception>() 处理异常
            _connectRetryPolicy = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(connectRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("连接重试 #{Retry}，原因: {Reason}，下次延迟 {Delay}", retryCount, reason, timespan);
                });

            _sendRetryPolicy = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(sendRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("发送重试 #{Retry}，原因: {Reason}，下次延迟 {Delay}", retryCount, reason, timespan);
                });

            // 订阅配置变更以热更新重试策略
            if (_appConfigMonitor != null)
            {
                _optionsChangeToken = _appConfigMonitor.OnChange(newCfg =>
                {
                    try
                    {
                        RebuildPolicies(newCfg);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _policyUpdateErrors);
                        _logger.LogWarning(ex, "配置变更时重建重试策略失败");
                    }
                });
            }
        }

        /// <summary>
        /// 重建重试策略
        /// </summary>
        private void RebuildPolicies(AppConfig cfg)
        {
            // 防抖，避免配置频繁重载导致策略频繁重建
            lock (_policyLock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastPolicyUpdateUtc < _policyDebounce)
                {
                    _logger.LogInformation("策略重建被防抖（间隔 {Interval}ms）", _policyDebounce.TotalMilliseconds);
                    return;
                }
                _lastPolicyUpdateUtc = now;
            }

            var connectRetries = cfg?.RetryPolicy?.ConnectRetries ?? 3;
            var sendRetries = cfg?.RetryPolicy?.SendRetries ?? 2;
            var baseDelayMs = cfg?.RetryPolicy?.BaseDelayMs ?? 200;

            var newConnect = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(connectRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("[策略热更新] 连接重试 #{Retry}，原因: {Reason}，下次延迟 {Delay}", retryCount, reason, timespan);
                });

            var newSend = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(sendRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("[策略热更新] 发送重试 #{Retry}，原因: {Reason}，下次延迟 {Delay}", retryCount, reason, timespan);
                });

            // 原子替换策略
            Interlocked.Exchange(ref _connectRetryPolicy, newConnect);
            Interlocked.Exchange(ref _sendRetryPolicy, newSend);
            _logger.LogInformation("重试策略已重建: ConnectRetries {OldConnect}->{NewConnect}, SendRetries {OldSend}->{NewSend}, BaseDelayMs {OldDelay}->{NewDelay}, 累计错误={Errors}",
                _currentConnectRetries, connectRetries, _currentSendRetries, sendRetries, _currentBaseDelayMs, baseDelayMs, _policyUpdateErrors);

            _currentConnectRetries = connectRetries;
            _currentSendRetries = sendRetries;
            _currentBaseDelayMs = baseDelayMs;
        }

        /// <summary>
        /// 异步连接到指定串口配置
        /// </summary>
        public async Task<bool> ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken = default)
        {
            // 如果已释放，直接返回错误状态
            if (_disposed)
            {
                UpdateState(ConnectionState.Error, "服务已释放");
                return false;
            }

            // 校验配置对象是否有效
            if (config == null || !config.IsValid())
            {
                UpdateState(ConnectionState.Error, "无效配置");
                return false;
            }

            // 等待获取连接锁，避免并发连接/断开
            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 更新状态为正在连接
                UpdateState(ConnectionState.Connecting, "正在连接...");

                // 如果已有 adapter 实例，先断开旧连接
                if (_adapter != null)
                {
                    await DisconnectInternalAsync().ConfigureAwait(false);
                }

                // 使用重试策略尝试连接
                var policy = _connectRetryPolicy;
                if (policy != null)
                {
                    return await policy.ExecuteAsync(async () =>
                    {
                        _adapter = CreateAdapter(config);
                        _receiveChannel = _receiveChannel ?? Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
                        _adapter.DataReceived += OnSerialPortDataReceived;
                        await Task.Run(() => _adapter.Open(), cancellationToken).ConfigureAwait(false);
                        _currentConfig = config;
                        UpdateState(ConnectionState.Connected, "已连接");
                        _logger.LogInformation("已连接到端口 {Port}（波特率 {Baud}，校验 {Parity}，停止位 {StopBits}）", config.PortName, config.BaudRate, config.Parity, config.StopBits);
                        return true;
                    }).ConfigureAwait(false);
                }
                else
                {
                    _adapter = CreateAdapter(config);
                    _receiveChannel = _receiveChannel ?? Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
                    _adapter.DataReceived += OnSerialPortDataReceived;
                    await Task.Run(() => _adapter.Open(), cancellationToken).ConfigureAwait(false);
                    _currentConfig = config;
                    UpdateState(ConnectionState.Connected, "已连接");
                    _logger.LogInformation("已连接到端口 {Port}（波特率 {Baud}，校验 {Parity}，停止位 {StopBits}）", config.PortName, config.BaudRate, config.Parity, config.StopBits);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 出错时尝试安全释放适配器，并设置错误状态
                SafeDisposeAdapter();
                UpdateState(ConnectionState.Error, $"连接失败: {ex.Message}");
                _logger.LogError(ex, "连接端口 {Port} 失败", config?.PortName);
                return false;
            }
            finally
            {
                // 始终释放锁
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// 异步断开连接
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            // 如果已释放，什么也不做
            if (_disposed)
            {
                return;
            }

            // 使用连接锁保证断开操作的互斥
            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
                _logger.LogInformation("已断开连接");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// 异步发送一条串口命令
        /// </summary>
        public async Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            // 如果服务已释放，不能发送
            if (_disposed)
            {
                UpdateState(ConnectionState.Error, "服务已释放");
                return false;
            }

            // 检查连接状态和命令是否为空
            if (!IsConnected || string.IsNullOrWhiteSpace(command))
            {
                UpdateState(ConnectionState.Error, "未连接或命令无效");
                return false;
            }

            try
            {
                var policy = _sendRetryPolicy;
                if (policy != null)
                {
                    return await policy.ExecuteAsync(async () =>
                    {
                        await Task.Run(() => _adapter!.WriteLine(command), cancellationToken).ConfigureAwait(false);
                        try
                        {
                            DataSent?.Invoke(this, new DataSentEventArgs(command));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "DataSent 事件处理程序抛出异常");
                        }
                        _logger.LogInformation("已发送命令（长度 {Length}）到 {Port}", command.Length, _adapter!.PortName);
                        return true;
                    }).ConfigureAwait(false);
                }
                else
                {
                    await Task.Run(() => _adapter!.WriteLine(command), cancellationToken).ConfigureAwait(false);
                    try
                    {
                        DataSent?.Invoke(this, new DataSentEventArgs(command));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "DataSent 事件处理程序抛出异常");
                    }
                    _logger.LogInformation("已发送命令（长度 {Length}）到 {Port}", command.Length, _adapter!.PortName);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                // 处理写超时
                UpdateState(ConnectionState.Error, $"发送超时: {ex.Message}");
                _logger.LogError(ex, "发送超时");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                // 端口状态不允许写入
                UpdateState(ConnectionState.Error, $"串口不可用: {ex.Message}");
                _logger.LogError(ex, "串口操作无效");
                return false;
            }
            catch (Exception ex)
            {
                // 其他写入错误
                UpdateState(ConnectionState.Error, $"发送失败: {ex.Message}");
                _logger.LogError(ex, "发送失败");
                return false;
            }
        }

        /// <summary>
        /// 创建 adapter 实例并应用配置
        /// </summary>
        private ISerialPortAdapter CreateAdapter(ConnectionConfig config)
        {
            return _adapterFactory.Create(config);
        }

        /// <summary>
        /// 内部断开实现：关闭并释放底层串口适配器
        /// </summary>
        private async Task DisconnectInternalAsync()
        {
            // 如果没有打开的端口，则只更新状态
            if (_adapter == null)
            {
                _currentConfig = null;
                // 关闭并完成接收通道
                _receiveChannel?.Writer.TryComplete();
                _receiveChannel = null;
                UpdateState(ConnectionState.Disconnected, "已断开");
                return;
            }

            try
            {
                // 更新状态为正在断开
                UpdateState(ConnectionState.Disconnecting, "正在断开...");

                // 将当前适配器引用保存后在后台线程处理
                var adapter = _adapter;
                await Task.Run(() =>
                {
                    try
                    {
                        // 如果端口打开，尝试关闭
                        if (adapter.IsOpen)
                        {
                            adapter.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "关闭端口时出错");
                    }

                    try
                    {
                        // 解除事件订阅
                        adapter.DataReceived -= OnSerialPortDataReceived;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "取消订阅 DataReceived 时出错");
                    }

                    try
                    {
                        // 释放适配器资源
                        adapter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "释放适配器时出错");
                    }
                }).ConfigureAwait(false);

                // 清空字段并更新状态为已断开
                _adapter = null;
                _receiveChannel?.Writer.TryComplete();
                _receiveChannel = null;
                _currentConfig = null;
                UpdateState(ConnectionState.Disconnected, "已断开");
            }
            catch (Exception ex)
            {
                // 处理断开过程中的异常，设置错误状态
                UpdateState(ConnectionState.Error, $"断开失败: {ex.Message}");
                _logger.LogError(ex, "断开连接失败");
            }
        }

        /// <summary>
        /// 底层串口的 DataReceived 事件处理程序
        /// </summary>
        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 如果服务已释放或适配器为空，忽略
            if (_disposed || _adapter == null)
            {
                return;
            }

            try
            {
                var data = _adapter.ReadExisting();
                try
                {
                    DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DataReceived 事件处理程序抛出异常");
                }
                _logger.LogDebug("从 {Port} 接收到数据（长度 {Length}）: {Preview}", _adapter.PortName, data.Length, data);
                // 尝试写入通道，如果写入失败则忽略
                try
                {
                    _receiveChannel?.Writer.TryWrite(data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "将接收数据写入通道失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取数据时出错");
            }
        }

        /// <summary>
        /// 异步方式读取接收到的数据。调用方可以使用 CancellationToken 取消读取。
        /// </summary>
        public async IAsyncEnumerable<string> ReceiveStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var reader = _receiveChannel?.Reader;
            if (reader == null)
                yield break;

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 更新内部连接状态并触发 ConnectionStateChanged 事件
        /// </summary>
        private void UpdateState(ConnectionState newState, string message)
        {
            var oldState = _currentState;
            _currentState = newState;
            try
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState, message));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ConnectionStateChanged 事件处理程序抛出异常");
            }
         }

        /// <summary>
        /// 在异常情况下安全释放底层串口适配器（与 DisconnectInternal 不重复）
        /// </summary>
        private void SafeDisposeAdapter()
        {
            // 如果没有适配器，直接更新状态即可
            if (_adapter == null)
            {
                _currentConfig = null;
                _currentState = ConnectionState.Disconnected;
                return;
            }

            try
            {
                // 如果端口已打开，先尝试关闭
                if (_adapter.IsOpen)
                {
                    try
                    {
                        _adapter.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "安全释放时关闭适配器出错");
                    }
                }

                try
                {
                    // 解除事件订阅
                    _adapter.DataReceived -= OnSerialPortDataReceived;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "安全释放时取消订阅 DataReceived 出错");
                }

                try
                {
                    // 释放适配器资源
                    _adapter.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "安全释放时释放适配器出错");
                }
            }
            finally
            {
                // 清空字段并更新状态
                _adapter = null;
                _currentConfig = null;
                _currentState = ConnectionState.Disconnected;
                _receiveChannel?.Writer.TryComplete();
                _receiveChannel = null;
            }
        }

        /// <summary>
        /// 释放服务资源
        /// </summary>
        public void Dispose()
        {
            // 如果已释放，直接返回
            if (_disposed)
            {
                return;
            }

            // 标记为已释放
            _disposed = true;

            try
            {
                // 尝试获取锁，以安全释放资源
                if (_connectionLock.Wait(0))
                {
                    try
                    {
                        SafeDisposeAdapter();
                    }
                    finally
                    {
                        // 释放锁
                        _connectionLock.Release();
                    }
                }
                else
                {
                    // 如果无法立即获取锁，直接进行清理，避免 UI 线程死锁
                    SafeDisposeAdapter();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dispose 时发生异常");
            }
            finally
            {
                // 取消配置订阅
                try
                {
                    _optionsChangeToken?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "释放配置变更令牌时发生异常");
                }

                // 释放信号量资源
                _connectionLock.Dispose();
            }
        }
    }
}

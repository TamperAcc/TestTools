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
    /// 使用 ISerialPortAdapter 以便测试和替换底层实现
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
        // 用于串口连接操作的互斥锁，避免并发连接/断开导致竞争
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        // 底层适配器，可能为 null（未连接）
        private ISerialPortAdapter? _adapter;
        // Channel 用于异步流式接收数据
        private Channel<string>? _receiveChannel;
        // 当前连接状态（本地缓存）
        private ConnectionState _currentState = ConnectionState.Disconnected;
        // 当前连接配置（可空）
        private ConnectionConfig? _currentConfig;
        // 标记服务是否已释放（Dispose 调用过）
        private bool _disposed;
        private readonly ILogger<SerialPortService> _logger;
        private readonly ISerialPortAdapterFactory _adapterFactory;

        // 连接状态变化事件，供上层订阅 UI 更新
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        // 数据接收事件，当串口收到数据时触发
        public event EventHandler<DataReceivedEventArgs>? DataReceived;
        // 数据发送事件，在成功写入串口后触发（供监视器记录发送内容）
        public event EventHandler<DataSentEventArgs>? DataSent;

        // 是否已连接（根据底层 Adapter 的 IsOpen 判断）
        public bool IsConnected => _adapter?.IsOpen ?? false;
        // 当前配置（可空）
        public ConnectionConfig? CurrentConfig => _currentConfig;
        // 当前连接状态
        public ConnectionState CurrentState => _currentState;

        // 通过 DI 注入 ILogger、适配器工厂和配置监控
        public SerialPortService(ILogger<SerialPortService> logger, ISerialPortAdapterFactory adapterFactory, IOptionsMonitor<AppConfig>? appConfigMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
            _appConfigMonitor = appConfigMonitor;

            // Initialize retry policies from config if available, otherwise use defaults
            var cfg = _appConfigMonitor?.CurrentValue?.RetryPolicy;
            var connectRetries = cfg?.ConnectRetries ?? 3;
            var sendRetries = cfg?.SendRetries ?? 2;
            var baseDelayMs = cfg?.BaseDelayMs ?? 200;
            _currentConnectRetries = connectRetries;
            _currentSendRetries = sendRetries;
            _currentBaseDelayMs = baseDelayMs;

            // Use HandleResult<TResult> to retry on false results, and Or<Exception>() to also handle exceptions
            _connectRetryPolicy = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(connectRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("Connect retry #{Retry} due to: {Reason}, next delay {Delay}", retryCount, reason, timespan);
                });

            _sendRetryPolicy = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(sendRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("Send retry #{Retry} due to: {Reason}, next delay {Delay}", retryCount, reason, timespan);
                });

            // Subscribe to configuration changes to hot-rebuild policies
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
                        _logger.LogWarning(ex, "Failed to rebuild retry policies on config change");
                    }
                });
            }
        }

        private void RebuildPolicies(AppConfig cfg)
        {
            // Debounce to avoid rapid churn on noisy config reloads
            lock (_policyLock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastPolicyUpdateUtc < _policyDebounce)
                {
                    _logger.LogInformation("Policy rebuild debounced (interval {Interval}ms)", _policyDebounce.TotalMilliseconds);
                    return;
                }
                _lastPolicyUpdateUtc = now;
            }

            var connectRetries = cfg?.RetryPolicy?.ConnectRetries ?? 3;
            var sendRetries = cfg?.RetryPolicy?.SendRetries ?? 2;
            var baseDelayMs = cfg?.RetryPolicy?.BaseDelayMs ?? 200;

            var oldConnect = _connectRetryPolicy;
            var oldSend = _sendRetryPolicy;

            var newConnect = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(connectRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("[Policy HotReload] Connect retry #{Retry} due to: {Reason}, next delay {Delay}", retryCount, reason, timespan);
                });

            var newSend = Policy.HandleResult<bool>(r => r == false)
                .Or<Exception>()
                .WaitAndRetryAsync(sendRetries, attempt => TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)), onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? "false-result";
                    _logger.LogWarning("[Policy HotReload] Send retry #{Retry} due to: {Reason}, next delay {Delay}", retryCount, reason, timespan);
                });

            // Atomically replace policies
            Interlocked.Exchange(ref _connectRetryPolicy, newConnect);
            Interlocked.Exchange(ref _sendRetryPolicy, newSend);
            _logger.LogInformation("Retry policies rebuilt: ConnectRetries {OldConnect}->{NewConnect}, SendRetries {OldSend}->{NewSend}, BaseDelayMs {OldDelay}->{NewDelay}, ErrorsSoFar={Errors}",
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
            // 如果已释放则直接返回错误状态
            if (_disposed)
            {
                UpdateState(ConnectionState.Error, "服务已释放");
                return false;
            }

            // 校验配置对象和有效性
            if (config == null || !config.IsValid())
            {
                UpdateState(ConnectionState.Error, "无效的配置");
                return false;
            }

            // 等待连接锁，避免并发连接/断开
            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 更新状态为正在连接
                UpdateState(ConnectionState.Connecting, "正在连接...");

                // 如果已有 adapter 实例，先断开清理
                if (_adapter != null)
                {
                    await DisconnectInternalAsync().ConfigureAwait(false);
                }

                // 创建并配置 adapter 使用重试策略
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
                        _logger.LogInformation("Connected to port {Port} (Baud {Baud}, Parity {Parity}, StopBits {StopBits})", config.PortName, config.BaudRate, config.Parity, config.StopBits);
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
                    _logger.LogInformation("Connected to port {Port} (Baud {Baud}, Parity {Parity}, StopBits {StopBits})", config.PortName, config.BaudRate, config.Parity, config.StopBits);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 出错时尝试安全释放适配器并设置错误状态
                SafeDisposeAdapter();
                UpdateState(ConnectionState.Error, $"连接失败: {ex.Message}");
                _logger.LogError(ex, "Failed to connect to port {Port}", config?.PortName);
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
            // 如果已释放则什么也不做
            if (_disposed)
            {
                return;
            }

            // 使用连接锁保证断开操作的互斥
            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
                _logger.LogInformation("Disconnected");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// 异步发送一条命令到串口
        /// </summary>
        public async Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            // 如果服务已释放则不能发送
            if (_disposed)
            {
                UpdateState(ConnectionState.Error, "服务已释放");
                return false;
            }

            // 必须已连接且命令非空
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
                            _logger.LogWarning(ex, "DataSent handler threw");
                        }
                        _logger.LogInformation("Sent command (len {Length}) on {Port}", command.Length, _adapter!.PortName);
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
                        _logger.LogWarning(ex, "DataSent handler threw");
                    }
                    _logger.LogInformation("Sent command (len {Length}) on {Port}", command.Length, _adapter!.PortName);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                // 处理写超时
                UpdateState(ConnectionState.Error, $"发送超时: {ex.Message}");
                _logger.LogError(ex, "Send timeout");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                // 端口状态不允许写入
                UpdateState(ConnectionState.Error, $"串口不可用: {ex.Message}");
                _logger.LogError(ex, "Invalid operation on serial port");
                return false;
            }
            catch (Exception ex)
            {
                // 其它写入错误
                UpdateState(ConnectionState.Error, $"发送失败: {ex.Message}");
                _logger.LogError(ex, "Send failed");
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
        /// 内部断开实现：关闭并释放底层适配器
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

                // 将当前适配器引用缓存以在后台线程处理
                var adapter = _adapter;
                await Task.Run(() =>
                {
                    try
                    {
                        // 如果端口打开，则尝试关闭
                        if (adapter.IsOpen)
                        {
                            adapter.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing port");
                    }

                    try
                    {
                        // 解除事件订阅
                        adapter.DataReceived -= OnSerialPortDataReceived;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error unsubscribing DataReceived");
                    }

                    try
                    {
                        // 释放适配器对象
                        adapter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, " Error disposing adapter");
                    }
                }).ConfigureAwait(false);

                // 将字段清空并设置状态为已断开
                _adapter = null;
                _receiveChannel?.Writer.TryComplete();
                _receiveChannel = null;
                _currentConfig = null;
                UpdateState(ConnectionState.Disconnected, "已断开");
            }
            catch (Exception ex)
            {
                // 处理断开过程中的异常并设置错误状态
                UpdateState(ConnectionState.Error, $"断开失败: {ex.Message}");
                _logger.LogError(ex, "DisconnectInternal failed");
            }
        }

        /// <summary>
        /// 底层适配器的 DataReceived 事件处理器
        /// </summary>
        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 如果服务已释放或适配器为空则忽略
            if (_disposed || _adapter == null)
            {
                return;
            }

            try
            {
                var data = _adapter.ReadExisting();
                DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
                _logger.LogDebug("Received data (len {Length}) from {Port}: {Preview}", data.Length, _adapter.PortName, data);
                // 尝试写入通道，若写入失败则忽略
                try
                {
                    _receiveChannel?.Writer.TryWrite(data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write received data into channel");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading data");
            }
        }

        /// <summary>
        /// 异步流式读取接收到的数据。消费者可以使用 CancellationToken 取消读取。
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
        /// 更新连接状态并触发 ConnectionStateChanged 事件
        /// </summary>
        private void UpdateState(ConnectionState newState, string message)
        {
            // 记录旧状态，更新为新状态
            var oldState = _currentState;
            _currentState = newState;
            // 触发事件通知订阅者（UI 等）
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState, message));
        }

        /// <summary>
        /// 在异常情况下安全释放底层适配器（比 DisconnectInternal 更保守）
        /// </summary>
        private void SafeDisposeAdapter()
        {
            // 如果没有适配器，直接清理状态并返回
            if (_adapter == null)
            {
                _currentConfig = null;
                _currentState = ConnectionState.Disconnected;
                return;
            }

            try
            {
                // 如果端口已打开，尝试先关闭
                if (_adapter.IsOpen)
                {
                    try
                    {
                        _adapter.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing adapter in SafeDispose");
                    }
                }

                try
                {
                    // 解除事件订阅
                    _adapter.DataReceived -= OnSerialPortDataReceived;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unsubscribing DataReceived in SafeDispose");
                }

                try
                {
                    // 释放适配器资源
                    _adapter.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing adapter in SafeDispose");
                }
            }
            finally
            {
                // 最终清理字段并设置状态
                _adapter = null;
                _currentConfig = null;
                _currentState = ConnectionState.Disconnected;
            }
        }

        /// <summary>
        /// 释放服务资源
        /// </summary>
        public void Dispose()
        {
            // 如果已释放则直接返回
            if (_disposed)
            {
                return;
            }

            // 标记为已释放
            _disposed = true;

            try
            {
                // 尝试立即获取连接锁以安全释放资源
                if (_connectionLock.Wait(0))
                {
                    try
                    {
                        SafeDisposeAdapter();
                    }
                    finally
                    {
                        // 释放连接锁
                        _connectionLock.Release();
                    }
                }
                else
                {
                    // 如果无法立即获取锁，直接进行清理以避免 UI 线程阻塞
                    SafeDisposeAdapter();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during Dispose");
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
                    _logger.LogWarning(ex, "Exception disposing options change token");
                }

                // 释放信号量资源
                _connectionLock.Dispose();
            }
        }
    }
}

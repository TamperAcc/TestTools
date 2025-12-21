using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestTool.Business.Enums;
using TestTool.Business.Models;
using TestTool.Infrastructure.Constants;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 电源设备控制器实现：基于串口服务发送开/关命令并维护设备状态
    /// </summary>
    public class PowerDeviceController : IDeviceController
    {
        // 注入的串口服务实例（可能为空，直到 InitializeAsync 被调用）
        private ISerialPortService? _serialPortService;
        // 当前设备状态缓存（非空）
        private DeviceStatus _currentStatus;
        private readonly IProtocolParser _parser;
        private readonly ILogger<PowerDeviceController>? _logger;

        // 设备状态变化事件，供外部订阅（例如 UI）
        public event EventHandler<DeviceStatusChangedEventArgs>? StatusChanged;

        // 将当前设备状态暴露给外部
        public DeviceStatus CurrentStatus => _currentStatus;

        // 设备名称属性（读写），保证不返回 null
        public string DeviceName
        {
            get => _currentStatus.DeviceName ?? string.Empty;
            set => _currentStatus.DeviceName = value ?? string.Empty;
        }

        // 构造函数：初始化设备状态为默认值，强制注入 ILogger
        public PowerDeviceController(ILogger<PowerDeviceController> logger, IProtocolParserFactory parserFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (parserFactory == null) throw new ArgumentNullException(nameof(parserFactory));
            _parser = parserFactory.Create();
            _currentStatus = new DeviceStatus
            {
                DeviceName = AppConstants.Defaults.DeviceName,
                ConnectionState = ConnectionState.Disconnected,
                PowerState = DevicePowerState.Unknown
            };
        }

        /// <summary>
        /// 初始化控制器并订阅串口服务的连接状态变化事件
        /// 这个方法用于在构造完成后注入运行时的串口服务实例
        /// </summary>
        public Task<bool> InitializeAsync(ISerialPortService serialPortService)
        {
            // 保存串口服务实例以便后续发送命令
            _serialPortService = serialPortService;

            // 订阅串口状态变化事件，用于根据连接状态调整设备可操作性
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;
            _serialPortService.DataReceived += OnDataReceived;

            _logger?.LogInformation("PowerDeviceController initialized and subscribed to serial port events");

            // 返回成功（保留 Task 签名以便兼容异步初始化场景）
            return Task.FromResult(true);
        }

        /// <summary>
        /// 异步发送“打开电源”命令
        /// </summary>
        public async Task<bool> TurnOnAsync()
        {
            // 如果不能发送命令或串口服务未注入则直接返回 false
            if (!_currentStatus.CanSendCommand || _serialPortService == null)
                return false;

            try
            {
                // 通过串口服务发送命令并等待结果
                var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOn);
                if (success)
                {
                    // 如果发送成功，更新内部电源状态并触发状态变更事件
                    UpdatePowerState(DevicePowerState.On, "电源已打开");
                }

                _logger?.LogInformation("TurnOnAsync result: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TurnOnAsync failed");
                // 出现异常时返回 false（不要抛出异常到 UI）
                return false;
            }
        }

        /// <summary>
        /// 异步发送“关闭电源”命令
        /// </summary>
        public async Task<bool> TurnOffAsync()
        {
            if (!_currentStatus.CanSendCommand || _serialPortService == null)
                return false;

            try
            {
                var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOff);
                if (success)
                {
                    UpdatePowerState(DevicePowerState.Off, "电源已关闭");
                }
                _logger?.LogInformation("TurnOffAsync result: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TurnOffAsync failed");
                return false;
            }
        }

        /// <summary>
        /// 串口连接状态变化处理器：更新内部设备状态并在必要时触发状态事件
        /// 注意：该方法由串口服务在其上下文中调用，订阅者（本类）应保证线程安全或在上层切换线程
        /// </summary>
        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            // 将收到的连接状态和消息保存到当前状态
            _currentStatus.ConnectionState = e.NewState;
            _currentStatus.StatusMessage = e.Message;
            _currentStatus.LastUpdateTime = DateTime.Now;

            // 在断开连接时重置电源状态为未知
            if (e.NewState == ConnectionState.Disconnected)
            {
                UpdatePowerState(DevicePowerState.Unknown, e.Message);
            }
            else
            {
                // 否则触发状态变更事件，通知订阅者当前状态（不改变电源状态）
                StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(_currentStatus, _currentStatus.PowerState));
            }

            _logger?.LogInformation("Device connection state changed {Old}->{New}: {Message}", e.OldState, e.NewState, e.Message);
        }

        private void OnDataReceived(object? sender, DataReceivedEventArgs e)
        {
            foreach (var frame in _parser.Parse(e.Data))
            {
                if (frame.PowerState.HasValue)
                {
                    UpdatePowerState(frame.PowerState.Value, frame.Command ?? frame.Raw);
                }
                else
                {
                    _logger?.LogDebug("Received frame (no power state): {Raw}", frame.Raw);
                }
            }
        }

        /// <summary>
        /// 内部方法：更新电源状态并触发状态变更通知
        /// </summary>
        private void UpdatePowerState(DevicePowerState newState, string message)
        {
            var oldState = _currentStatus.PowerState;
            _currentStatus.PowerState = newState;
            _currentStatus.StatusMessage = message;
            _currentStatus.LastUpdateTime = DateTime.Now;

            _logger?.LogInformation("Device power state {Old}->{New}: {Message}", oldState, newState, message);

            // 将事件触发，包含新状态和旧状态封装到事件参数中
            StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(_currentStatus, oldState));
        }

        /// <summary>
        /// 释放资源：解除事件订阅，断开与串口服务的耦合
        /// </summary>
        public void Dispose()
        {
            if (_serialPortService != null)
            {
                // 取消订阅串口服务的事件以避免在销毁后继续回调
                _serialPortService.ConnectionStateChanged -= OnConnectionStateChanged;
                _serialPortService.DataReceived -= OnDataReceived;
            }
        }
    }
}

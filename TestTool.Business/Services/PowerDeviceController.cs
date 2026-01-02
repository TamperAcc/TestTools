using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Core.Services;
using TestTool.Infrastructure.Constants;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 电源设备控制器实现：通过串口发送开/关命令并维护设备状态
    /// </summary>
    public class PowerDeviceController : IDeviceController
    {
        private ISerialPortService? _serialPortService;
        private DeviceStatus _currentStatus;
        private readonly IProtocolParser _parser;
        private readonly ILogger<PowerDeviceController>? _logger;

        public event EventHandler<DeviceStatusChangedEventArgs>? StatusChanged;

        public DeviceStatus CurrentStatus => _currentStatus;

        public string DeviceName
        {
            get => _currentStatus.DeviceName ?? string.Empty;
            set => _currentStatus.DeviceName = value ?? string.Empty;
        }

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

        public Task<bool> InitializeAsync(ISerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;
            _serialPortService.DataReceived += OnDataReceived;

            _logger?.LogInformation("PowerDeviceController initialized and subscribed to serial port events");
            return Task.FromResult(true);
        }

        public async Task<bool> TurnOnAsync()
        {
            if (!_currentStatus.CanSendCommand || _serialPortService == null)
                return false;

            try
            {
                var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOn);
                if (success)
                {
                    UpdatePowerState(DevicePowerState.On, "电源已开");
                }

                _logger?.LogInformation("TurnOnAsync result: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TurnOnAsync failed");
                return false;
            }
        }

        public async Task<bool> TurnOffAsync()
        {
            if (!_currentStatus.CanSendCommand || _serialPortService == null)
                return false;

            try
            {
                var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOff);
                if (success)
                {
                    UpdatePowerState(DevicePowerState.Off, "电源已关");
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

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            _currentStatus.ConnectionState = e.NewState;
            _currentStatus.StatusMessage = e.Message;
            _currentStatus.LastUpdateTime = DateTime.Now;

            if (e.NewState == ConnectionState.Disconnected)
            {
                UpdatePowerState(DevicePowerState.Unknown, e.Message);
            }
            else
            {
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

        private void UpdatePowerState(DevicePowerState newState, string message)
        {
            var oldState = _currentStatus.PowerState;
            _currentStatus.PowerState = newState;
            _currentStatus.StatusMessage = message;
            _currentStatus.LastUpdateTime = DateTime.Now;

            _logger?.LogInformation("Device power state {Old}->{New}: {Message}", oldState, newState, message);
            StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(_currentStatus, oldState));
        }

        public void Dispose()
        {
            if (_serialPortService != null)
            {
                _serialPortService.ConnectionStateChanged -= OnConnectionStateChanged;
                _serialPortService.DataReceived -= OnDataReceived;
            }
        }
    }
}

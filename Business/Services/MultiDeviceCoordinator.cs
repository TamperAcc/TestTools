using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestTool.Business.Enums;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 多设备协调器事件参数：携带设备类型标识
    /// </summary>
    public class DeviceConnectionStateChangedEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public ConnectionStateChangedEventArgs ConnectionArgs { get; }

        public DeviceConnectionStateChangedEventArgs(DeviceType deviceType, ConnectionStateChangedEventArgs args)
        {
            DeviceType = deviceType;
            ConnectionArgs = args;
        }
    }

    public class DeviceDataReceivedEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public DataReceivedEventArgs DataArgs { get; }

        public DeviceDataReceivedEventArgs(DeviceType deviceType, DataReceivedEventArgs args)
        {
            DeviceType = deviceType;
            DataArgs = args;
        }
    }

    public class DeviceDataSentEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public DataSentEventArgs DataArgs { get; }

        public DeviceDataSentEventArgs(DeviceType deviceType, DataSentEventArgs args)
        {
            DeviceType = deviceType;
            DataArgs = args;
        }
    }

    public class DeviceStatusChangedWithTypeEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public DeviceStatusChangedEventArgs StatusArgs { get; }

        public DeviceStatusChangedWithTypeEventArgs(DeviceType deviceType, DeviceStatusChangedEventArgs args)
        {
            DeviceType = deviceType;
            StatusArgs = args;
        }
    }

    /// <summary>
    /// 多设备协调器接口：管理多个独立设备的连接和控制
    /// </summary>
    public interface IMultiDeviceCoordinator : IDisposable
    {
        AppConfig AppConfig { get; }
        
        /// <summary>
        /// 检查指定设备是否已连接
        /// </summary>
        bool IsConnected(DeviceType deviceType);

        /// <summary>
        /// 设备连接状态变化事件
        /// </summary>
        event EventHandler<DeviceConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// 设备数据接收事件
        /// </summary>
        event EventHandler<DeviceDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 设备数据发送事件
        /// </summary>
        event EventHandler<DeviceDataSentEventArgs> DataSent;

        /// <summary>
        /// 设备状态变化事件
        /// </summary>
        event EventHandler<DeviceStatusChangedWithTypeEventArgs> DeviceStatusChanged;

        /// <summary>
        /// 初始化所有设备
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 连接指定设备
        /// </summary>
        Task<bool> ConnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开指定设备
        /// </summary>
        Task DisconnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 打开指定设备电源
        /// </summary>
        Task<bool> TurnOnAsync(DeviceType deviceType);

        /// <summary>
        /// 关闭指定设备电源
        /// </summary>
        Task<bool> TurnOffAsync(DeviceType deviceType);

        /// <summary>
        /// 保存配置
        /// </summary>
        Task SaveConfigAsync();

        /// <summary>
        /// 更新指定设备的连接配置
        /// </summary>
        bool TryUpdateConnectionConfig(DeviceType deviceType, string port, int baudRate, bool isLocked);

        /// <summary>
        /// 获取指定设备的配置
        /// </summary>
        DeviceConfig GetDeviceConfig(DeviceType deviceType);
    }

    /// <summary>
    /// 多设备协调器实现：为每个设备维护独立的串口服务和设备控制器
    /// </summary>
    public class MultiDeviceCoordinator : IMultiDeviceCoordinator
    {
        private readonly Dictionary<DeviceType, ISerialPortService> _serialServices = new();
        private readonly Dictionary<DeviceType, IDeviceController> _deviceControllers = new();
        private readonly IDeviceControllerFactory _deviceControllerFactory;
        private readonly ISerialPortServiceFactory _serialPortServiceFactory;
        private readonly Data.IConfigRepository _configRepository;
        private readonly ILogger<MultiDeviceCoordinator>? _logger;
        private AppConfig _appConfig = new();
        private bool _initialized;

        public AppConfig AppConfig => _appConfig;

        public event EventHandler<DeviceConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<DeviceDataReceivedEventArgs>? DataReceived;
        public event EventHandler<DeviceDataSentEventArgs>? DataSent;
        public event EventHandler<DeviceStatusChangedWithTypeEventArgs>? DeviceStatusChanged;

        public MultiDeviceCoordinator(
            ISerialPortServiceFactory serialPortServiceFactory,
            IDeviceControllerFactory deviceControllerFactory,
            Data.IConfigRepository configRepository,
            ILogger<MultiDeviceCoordinator>? logger = null)
        {
            _serialPortServiceFactory = serialPortServiceFactory ?? throw new ArgumentNullException(nameof(serialPortServiceFactory));
            _deviceControllerFactory = deviceControllerFactory ?? throw new ArgumentNullException(nameof(deviceControllerFactory));
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _logger = logger;
        }

        public bool IsConnected(DeviceType deviceType)
        {
            return _serialServices.TryGetValue(deviceType, out var service) && service.IsConnected;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            // 加载持久化配置
            _appConfig = await _configRepository.LoadAsync().ConfigureAwait(false);

            // 为每个设备类型创建独立的串口服务和设备控制器
            foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
            {
                var deviceConfig = _appConfig.GetDeviceConfig(deviceType);

                // 创建串口服务实例
                var serialService = _serialPortServiceFactory.Create();
                _serialServices[deviceType] = serialService;

                // 创建设备控制器实例
                var controller = _deviceControllerFactory.Create();
                await controller.InitializeAsync(serialService).ConfigureAwait(false);
                controller.DeviceName = deviceConfig.DeviceName;
                _deviceControllers[deviceType] = controller;

                // 订阅事件并转发（附带设备类型）
                var dt = deviceType; // 捕获变量
                serialService.ConnectionStateChanged += (s, e) => OnConnectionStateChanged(dt, e);
                serialService.DataReceived += (s, e) => OnDataReceived(dt, e);
                serialService.DataSent += (s, e) => OnDataSent(dt, e);
                controller.StatusChanged += (s, e) => OnDeviceStatusChanged(dt, e);
            }

            _initialized = true;
            _logger?.LogInformation("MultiDeviceCoordinator initialized with {Count} devices", _serialServices.Count);
        }

        public async Task<bool> ConnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
            if (string.IsNullOrWhiteSpace(deviceConfig.SelectedPort))
            {
                _logger?.LogWarning("ConnectAsync skipped for {Device}: SelectedPort is empty", deviceType);
                return false;
            }

            if (!_serialServices.TryGetValue(deviceType, out var serialService))
            {
                _logger?.LogError("Serial service not found for {Device}", deviceType);
                return false;
            }

            var settings = (deviceConfig.ConnectionSettings ?? new ConnectionConfig()).NormalizeWithDefaults();
            var config = new ConnectionConfig(deviceConfig.SelectedPort)
            {
                BaudRate = settings.BaudRate,
                DataBits = settings.DataBits,
                Parity = settings.Parity,
                StopBits = settings.StopBits,
                Encoding = settings.Encoding,
                ReadTimeout = settings.ReadTimeout,
                WriteTimeout = settings.WriteTimeout
            };

            var success = await serialService.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                await SaveConfigAsync().ConfigureAwait(false);
            }
            return success;
        }

        public async Task DisconnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (_serialServices.TryGetValue(deviceType, out var serialService))
            {
                await serialService.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> TurnOnAsync(DeviceType deviceType)
        {
            EnsureInitialized();

            var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
            
            if (!_serialServices.TryGetValue(deviceType, out var serialService) || !serialService.IsConnected)
            {
                _logger?.LogWarning("TurnOnAsync skipped for {Device}: not connected", deviceType);
                return false;
            }

            try
            {
                var command = string.IsNullOrEmpty(deviceConfig.OnCommand) ? "ON" : deviceConfig.OnCommand;
                var success = await serialService.SendCommandAsync(command).ConfigureAwait(false);
                
                if (success && _deviceControllers.TryGetValue(deviceType, out var controller))
                {
                    // 通知控制器更新状态
                    controller.CurrentStatus.PowerState = DevicePowerState.On;
                    controller.CurrentStatus.StatusMessage = "电源已打开";
                }
                
                _logger?.LogInformation("TurnOnAsync {Device} with command '{Command}': {Success}", deviceType, command, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TurnOnAsync failed for {Device}", deviceType);
                return false;
            }
        }

        public async Task<bool> TurnOffAsync(DeviceType deviceType)
        {
            EnsureInitialized();

            var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
            
            if (!_serialServices.TryGetValue(deviceType, out var serialService) || !serialService.IsConnected)
            {
                _logger?.LogWarning("TurnOffAsync skipped for {Device}: not connected", deviceType);
                return false;
            }

            try
            {
                var command = string.IsNullOrEmpty(deviceConfig.OffCommand) ? "OFF" : deviceConfig.OffCommand;
                var success = await serialService.SendCommandAsync(command).ConfigureAwait(false);
                
                if (success && _deviceControllers.TryGetValue(deviceType, out var controller))
                {
                    // 通知控制器更新状态
                    controller.CurrentStatus.PowerState = DevicePowerState.Off;
                    controller.CurrentStatus.StatusMessage = "电源已关闭";
                }
                
                _logger?.LogInformation("TurnOffAsync {Device} with command '{Command}': {Success}", deviceType, command, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TurnOffAsync failed for {Device}", deviceType);
                return false;
            }
        }

        public async Task SaveConfigAsync()
        {
            EnsureInitialized();
            await _configRepository.SaveAsync(_appConfig).ConfigureAwait(false);
        }

        public bool TryUpdateConnectionConfig(DeviceType deviceType, string port, int baudRate, bool isLocked)
        {
            EnsureInitialized();

            // 允许空端口（设备未配置）
            var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
            deviceConfig.SelectedPort = port ?? string.Empty;
            deviceConfig.IsPortLocked = isLocked;
            deviceConfig.ConnectionSettings ??= new ConnectionConfig();
            deviceConfig.ConnectionSettings.BaudRate = baudRate > 0 ? baudRate : 115200;

            // 只要波特率有效就返回 true
            return baudRate > 0;
        }

        public DeviceConfig GetDeviceConfig(DeviceType deviceType)
        {
            return _appConfig.GetDeviceConfig(deviceType);
        }

        private void OnConnectionStateChanged(DeviceType deviceType, ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, new DeviceConnectionStateChangedEventArgs(deviceType, e));
        }

        private void OnDataReceived(DeviceType deviceType, DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, new DeviceDataReceivedEventArgs(deviceType, e));
        }

        private void OnDataSent(DeviceType deviceType, DataSentEventArgs e)
        {
            DataSent?.Invoke(this, new DeviceDataSentEventArgs(deviceType, e));
        }

        private void OnDeviceStatusChanged(DeviceType deviceType, DeviceStatusChangedEventArgs e)
        {
            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedWithTypeEventArgs(deviceType, e));
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Coordinator not initialized. Call InitializeAsync first.");
            }
        }

        public void Dispose()
        {
            foreach (var kvp in _serialServices)
            {
                kvp.Value.Dispose();
            }
            foreach (var kvp in _deviceControllers)
            {
                kvp.Value.Dispose();
            }
            _serialServices.Clear();
            _deviceControllers.Clear();
        }
    }

    /// <summary>
    /// 串口服务工厂接口：用于创建独立的串口服务实例
    /// </summary>
    public interface ISerialPortServiceFactory
    {
        ISerialPortService Create();
    }

    /// <summary>
    /// 默认串口服务工厂实现
    /// </summary>
    public class DefaultSerialPortServiceFactory : ISerialPortServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultSerialPortServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISerialPortService Create()
        {
            // 通过 DI 容器创建新实例
            return (ISerialPortService)_serviceProvider.GetService(typeof(ISerialPortService))
                ?? throw new InvalidOperationException("Cannot resolve ISerialPortService");
        }
    }
}

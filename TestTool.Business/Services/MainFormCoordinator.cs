using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestTool.Core.Models;
using TestTool.Core.Services;
using TestTool.Core.Enums;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 主窗体协调器：集中处理配置加载、设备控制器初始化、连接/断开及事件转发
    /// </summary>
    public interface IMainFormCoordinator : IDisposable
    {
        AppConfig AppConfig { get; }
        bool IsConnected { get; }
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<DataSentEventArgs> DataSent;
        event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;
        Task InitializeAsync();
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task<bool> TurnOnAsync();
        Task<bool> TurnOffAsync();
        Task SaveConfigAsync();
        bool TryUpdateConnectionConfig(string port, int baudRate, bool isLocked);
    }

    /// <summary>
    /// 主窗体协调器实现：封装串口服务与设备控制器交互，提供 UI 所需的统一接口
    /// </summary>
    public class MainFormCoordinator : IMainFormCoordinator
    {
        private readonly ISerialPortService _serialPortService;
        private readonly IDeviceControllerFactory _deviceControllerFactory;
        private IDeviceController? _deviceController;
        private readonly Data.IConfigRepository _configRepository;
        private readonly ILogger<MainFormCoordinator>? _logger;
        private AppConfig _appConfig = new();
        private bool _initialized;

        public AppConfig AppConfig => _appConfig;
        public bool IsConnected => _serialPortService.IsConnected;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs>? DataReceived;
        public event EventHandler<DataSentEventArgs>? DataSent;
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public MainFormCoordinator(
            ISerialPortService serialPortService,
            IDeviceControllerFactory deviceControllerFactory,
            Data.IConfigRepository configRepository,
            ILogger<MainFormCoordinator>? logger = null)
        {
            _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
            _deviceControllerFactory = deviceControllerFactory ?? throw new ArgumentNullException(nameof(deviceControllerFactory));
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            // 加载持久化配置
            _appConfig = await _configRepository.LoadAsync().ConfigureAwait(false);
            var deviceConfig = _appConfig.GetDeviceConfig(DeviceType.FCC1);

            // 创建并初始化设备控制器
            _deviceController = _deviceControllerFactory.Create();
            await _deviceController.InitializeAsync(_serialPortService).ConfigureAwait(false);
            _deviceController.DeviceName = deviceConfig.DeviceName;

            // 订阅串口与设备事件，供 UI 使用
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;
            _serialPortService.DataReceived += OnDataReceived;
            _serialPortService.DataSent += OnDataSent;
            _deviceController.StatusChanged += OnDeviceStatusChanged;

            _initialized = true;
            _logger?.LogInformation("MainFormCoordinator initialized");
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            var deviceConfig = _appConfig.GetDeviceConfig(DeviceType.FCC1);
            if (string.IsNullOrWhiteSpace(deviceConfig.SelectedPort))
            {
                _logger?.LogWarning("ConnectAsync skipped: SelectedPort is empty");
                return false;
            }

            // 组装连接配置（包含串口参数和编码、超时）
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

            var success = await _serialPortService.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                await SaveConfigAsync().ConfigureAwait(false);
            }
            return success;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            await _serialPortService.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TurnOnAsync()
        {
            EnsureInitialized();
            return await _deviceController!.TurnOnAsync().ConfigureAwait(false);
        }

        public async Task<bool> TurnOffAsync()
        {
            EnsureInitialized();
            return await _deviceController!.TurnOffAsync().ConfigureAwait(false);
        }

        public async Task SaveConfigAsync()
        {
            EnsureInitialized();
            await _configRepository.SaveAsync(_appConfig).ConfigureAwait(false);
        }

        public bool TryUpdateConnectionConfig(string port, int baudRate, bool isLocked)
        {
            EnsureInitialized();
            var deviceConfig = _appConfig.GetDeviceConfig(DeviceType.FCC1);
            if (string.IsNullOrWhiteSpace(port)) return false;
            deviceConfig.SelectedPort = port;
            deviceConfig.IsPortLocked = isLocked;
            deviceConfig.ConnectionSettings ??= new ConnectionConfig();
            deviceConfig.ConnectionSettings.BaudRate = baudRate;
            return deviceConfig.ConnectionSettings.IsValid();
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        private void OnDataReceived(object? sender, DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        private void OnDataSent(object? sender, DataSentEventArgs e)
        {
            DataSent?.Invoke(this, e);
        }

        private void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            DeviceStatusChanged?.Invoke(this, e);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Coordinator not initialized. Call InitializeAsync first.");
            }
            if (_deviceController == null)
            {
                throw new InvalidOperationException("Device controller not initialized.");
            }
        }

        public void Dispose()
        {
            _serialPortService.ConnectionStateChanged -= OnConnectionStateChanged;
            _serialPortService.DataReceived -= OnDataReceived;
            _serialPortService.DataSent -= OnDataSent;
            if (_deviceController != null)
            {
                _deviceController.StatusChanged -= OnDeviceStatusChanged;
            }
        }
    }
}

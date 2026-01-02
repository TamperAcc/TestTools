using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Core.Services;
using TestTool.Business.Services;
using TestTool.Forms.Base;
using TestTool.Infrastructure.Constants;
using TestTool.Infrastructure.Helpers;

namespace TestTool
{
    /// <summary>
    /// 串口工具主窗体 - 多设备版本
    /// </summary>
    public partial class MainForm : ResizableFormBase, IMainFormUi
    {
        // 多设备协调器
        private readonly IMultiDeviceCoordinator _coordinator = null!;
        private IOptionsMonitor<AppConfig>? _optionsMonitor;
        private IDisposable? _optionsChangeToken;
        private readonly ILogger<MainForm>? _logger;
        private readonly bool _isDesignMode;

        // 应用配置缓存
        private AppConfig _appConfig = null!;

        // 每设备独立的监视器窗口
        private readonly Dictionary<DeviceType, DeviceMonitorForm?> _monitorForms = new();
        private readonly List<SerialMonitorHostForm> _monitorHosts = new();
        private readonly Dictionary<DeviceType, SerialMonitorHostForm> _deviceHostMap = new();

        // 设置窗口实例
        private MultiDeviceSettingsForm? _settingsForm;

        // 设备控件映射（便于统一处理）
        private Dictionary<DeviceType, (Label status, Button connect, Button on, Button off)> _deviceControls = null!;

        /// <summary>
        /// 监视器状态变化事件：当监视器窗口打开或关闭时触发
        /// </summary>
        public event EventHandler<MonitorStateChangedEventArgs>? MonitorStateChanged;

        // 无参构造：供 WinForms 设计器使用
        public MainForm()
        {
            _isDesignMode = true;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            UIHelper.InitializeFonts(this.Font);
        }

        // 构造函数：通过 DI 注入依赖
        public MainForm(IMultiDeviceCoordinator coordinator, IOptionsMonitor<AppConfig> optionsMonitor, ILogger<MainForm>? logger = null)
            : this()
        {
            _isDesignMode = false;
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger;
            _optionsChangeToken = _optionsMonitor.OnChange(OnAppConfigChanged);
        }

        // 配置变更回调
        private void OnAppConfigChanged(AppConfig newConfig)
        {
            try
            {
                if (_appConfig == null)
                {
                    _appConfig = newConfig;
                }

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UpdateAllDevicesUI));
                }
                else
                {
                    UpdateAllDevicesUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error applying AppConfig change");
            }
        }

        // 窎体加载事件
        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (_isDesignMode)
                return;

            try
            {
                // 初始化控件映射
                InitializeDeviceControlMappings();

                await _coordinator.InitializeAsync();
                _appConfig = _coordinator.AppConfig;

                RestoreHostsFromConfig();

                // 订阅多设备事件
                _coordinator.ConnectionStateChanged += OnConnectionStateChanged;
                _coordinator.DataReceived += OnSerialDataReceived;
                _coordinator.DataSent += OnSerialDataSent;
                _coordinator.DeviceStatusChanged += OnDeviceStatusChanged;

                // 更新所有设备 UI
                UpdateAllDevicesUI();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MainForm");
            }
        }

        // 初始化设备控件映射
        private void InitializeDeviceControlMappings()
        {
            _deviceControls = new Dictionary<DeviceType, (Label, Button, Button, Button)>
            {
                { DeviceType.FCC1, (lblStatusFCC1, btnConnectFCC1, btnOnFCC1, btnOffFCC1) },
                { DeviceType.FCC2, (lblStatusFCC2, btnConnectFCC2, btnOnFCC2, btnOffFCC2) },
                { DeviceType.FCC3, (lblStatusFCC3, btnConnectFCC3, btnOnFCC3, btnOffFCC3) },
                { DeviceType.HIL, (lblStatusHIL, btnConnectHIL, btnOnHIL, btnOffHIL) }
            };
        }

        // 更新所有设备 UI
        private void UpdateAllDevicesUI()
        {
            foreach (var deviceType in _deviceControls.Keys)
            {
                UpdateDeviceUI(deviceType);
            }
        }

        // 更新单个设备 UI
        private void UpdateDeviceUI(DeviceType deviceType)
        {
            if (!_deviceControls.TryGetValue(deviceType, out var controls))
                return;

            var config = _appConfig.GetDeviceConfig(deviceType);
            UIHelper.SetStatusLabel(controls.status, ConnectionState.Disconnected, config.DeviceName, UIConstants.StatusMessages.Disconnected);
            UIHelper.SetButtonDisabled(controls.on);
            UIHelper.SetButtonDisabled(controls.off);
        }

        #region 设备连接按钮事件

        private async void btnConnectFCC1_Click(object? sender, EventArgs e) => await HandleConnectClick(DeviceType.FCC1);
        private async void btnConnectFCC2_Click(object? sender, EventArgs e) => await HandleConnectClick(DeviceType.FCC2);
        private async void btnConnectFCC3_Click(object? sender, EventArgs e) => await HandleConnectClick(DeviceType.FCC3);
        private async void btnConnectHIL_Click(object? sender, EventArgs e) => await HandleConnectClick(DeviceType.HIL);

        private async Task HandleConnectClick(DeviceType deviceType)
        {
            if (!_deviceControls.TryGetValue(deviceType, out var controls))
                return;

            if (!controls.connect.Enabled)
                return;

            var config = _appConfig.GetDeviceConfig(deviceType);

            // 如果已连接，允许断开
            if (_coordinator.IsConnected(deviceType))
            {
                await _coordinator.DisconnectAsync(deviceType);
                return;
            }

            // 检查是否已锁定，未锁定不允许连接
            if (!config.IsPortLocked)
            {
                UIHelper.SetStatusLabel(controls.status, ConnectionState.Error, config.DeviceName, "请锁定串口配置");
                return;
            }

            if (string.IsNullOrEmpty(config.SelectedPort) || config.SelectedPort == "无可用串口")
            {
                UIHelper.SetStatusLabel(controls.status, ConnectionState.Error, config.DeviceName, UIConstants.StatusMessages.PleaseSelectPort);
                return;
            }

            controls.connect.Enabled = false;
            UIHelper.SetStatusLabel(controls.status, ConnectionState.Connecting, config.DeviceName, UIConstants.StatusMessages.Connecting);

            try
            {
                await _coordinator.ConnectAsync(deviceType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error connecting {Device} to port {Port}", deviceType, config.SelectedPort);
                UIHelper.SetStatusLabel(controls.status, ConnectionState.Error, config.DeviceName, $"连接失败: {ex.Message}");
            }
            finally
            {
                controls.connect.Enabled = true;
            }
        }

        #endregion

        #region 设备 ON/OFF 按钮事件

        private async void btnOnFCC1_Click(object? sender, EventArgs e) => await _coordinator.TurnOnAsync(DeviceType.FCC1);
        private async void btnOffFCC1_Click(object? sender, EventArgs e) => await _coordinator.TurnOffAsync(DeviceType.FCC1);

        private async void btnOnFCC2_Click(object? sender, EventArgs e) => await _coordinator.TurnOnAsync(DeviceType.FCC2);
        private async void btnOffFCC2_Click(object? sender, EventArgs e) => await _coordinator.TurnOffAsync(DeviceType.FCC2);

        private async void btnOnFCC3_Click(object? sender, EventArgs e) => await _coordinator.TurnOnAsync(DeviceType.FCC3);
        private async void btnOffFCC3_Click(object? sender, EventArgs e) => await _coordinator.TurnOffAsync(DeviceType.FCC3);

        private async void btnOnHIL_Click(object? sender, EventArgs e) => await _coordinator.TurnOnAsync(DeviceType.HIL);
        private async void btnOffHIL_Click(object? sender, EventArgs e) => await _coordinator.TurnOffAsync(DeviceType.HIL);

        #endregion

        #region 一键连接/断开事件

        private void btnConnectAll_Click(object? sender, EventArgs e)
        {
            btnConnectAll.Enabled = false;
            
            foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
            {
                var config = _appConfig.GetDeviceConfig(deviceType);
                if (!_deviceControls.TryGetValue(deviceType, out var controls))
                    continue;

                if (_coordinator.IsConnected(deviceType))
                    continue;

                if (!config.IsPortLocked)
                {
                    UIHelper.SetStatusLabel(controls.status, ConnectionState.Error, config.DeviceName, "请锁定串口配置");
                }
            }

            _ = _coordinator.ConnectAllAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Error in ConnectAllAsync");
                }
            }, TaskScheduler.Default);

            btnConnectAll.Enabled = true;
        }

        private void btnDisconnectAll_Click(object? sender, EventArgs e)
        {
            btnDisconnectAll.Enabled = false;

            _ = _coordinator.DisconnectAllAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Error in DisconnectAllAsync");
                }
            }, TaskScheduler.Default);

            btnDisconnectAll.Enabled = true;
        }

        #endregion

        #region 一键开/关事件

        private async void btnPowerOnAll_Click(object? sender, EventArgs e) => await ExecutePowerAllAsync(true);

        private async void btnPowerOffAll_Click(object? sender, EventArgs e) => await ExecutePowerAllAsync(false);

        private async Task ExecutePowerAllAsync(bool turnOn)
        {
            if (_isDesignMode)
                return;

            btnPowerOnAll.Enabled = false;
            btnPowerOffAll.Enabled = false;

            try
            {
                if (turnOn)
                {
                    await _coordinator.TurnOnAllAsync().ConfigureAwait(false);
                }
                else
                {
                    await _coordinator.TurnOffAllAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in PowerAll operation");
            }
            finally
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        btnPowerOnAll.Enabled = true;
                        btnPowerOffAll.Enabled = true;
                    }));
                }
                else
                {
                    btnPowerOnAll.Enabled = true;
                    btnPowerOffAll.Enabled = true;
                }
            }
        }

        #endregion

        #region 事件处理

        // 连接状态变化
        private void OnConnectionStateChanged(object? sender, DeviceConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnConnectionStateChanged(sender, e)));
                return;
            }

            if (!_deviceControls.TryGetValue(e.DeviceType, out var controls))
                return;

            var config = _appConfig.GetDeviceConfig(e.DeviceType);
            var args = e.ConnectionArgs;

            UIHelper.SetStatusLabel(controls.status, args.NewState, config.DeviceName, args.Message);
            UIHelper.UpdateConnectButton(controls.connect, args.NewState == ConnectionState.Connected);

            if (args.NewState == ConnectionState.Connected)
            {
                controls.on.Enabled = true;
                controls.off.Enabled = true;
                UIHelper.SetButtonDefault(controls.on);
                UIHelper.SetButtonDefault(controls.off);
            }
            else
            {
                UIHelper.SetButtonDisabled(controls.on);
                UIHelper.SetButtonDisabled(controls.off);
            }
        }

        // 设备状态变化
        private void OnDeviceStatusChanged(object? sender, DeviceStatusChangedWithTypeEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnDeviceStatusChanged(sender, e)));
                return;
            }

            if (!_deviceControls.TryGetValue(e.DeviceType, out var controls))
                return;

            var status = e.StatusArgs.Status;

            if (status.PowerState == DevicePowerState.On)
            {
                UIHelper.SetButtonActive(controls.on, DevicePowerState.On);
                UIHelper.SetButtonDefault(controls.off);
            }
            else if (status.PowerState == DevicePowerState.Off)
            {
                UIHelper.SetButtonDefault(controls.on);
                UIHelper.SetButtonActive(controls.off, DevicePowerState.Off);
            }
            else
            {
                UIHelper.SetButtonDefault(controls.on);
                UIHelper.SetButtonDefault(controls.off);
            }
        }

        // 数据接收
        private void OnSerialDataReceived(object? sender, DeviceDataReceivedEventArgs e)
        {
            if (_deviceHostMap.TryGetValue(e.DeviceType, out var host) && host != null && !host.IsDisposed && host.TryGetMonitor(e.DeviceType, out var hostMonitor) && hostMonitor != null)
            {
                hostMonitor.AppendLine(e.DataArgs.Data);
                return;
            }

            if (_monitorForms.TryGetValue(e.DeviceType, out var monitor) && monitor != null && !monitor.IsDisposed && monitor.Visible)
            {
                monitor.AppendLine(e.DataArgs.Data);
            }
        }

        // 数据发送
        private void OnSerialDataSent(object? sender, DeviceDataSentEventArgs e)
        {
            if (_deviceHostMap.TryGetValue(e.DeviceType, out var host) && host != null && !host.IsDisposed && host.TryGetMonitor(e.DeviceType, out var hostMonitor) && hostMonitor != null)
            {
                hostMonitor.AppendSent(e.DataArgs.Command);
                return;
            }

            if (_monitorForms.TryGetValue(e.DeviceType, out var monitor) && monitor != null && !monitor.IsDisposed && monitor.Visible)
            {
                monitor.AppendSent(e.DataArgs.Command);
            }
        }

        #endregion

        #region 监视器窗口管理

        // 切换指定设备的监视器窗口
        public void ToggleMonitor(DeviceType deviceType)
        {
            var config = _appConfig.GetDeviceConfig(deviceType);

            // 如果需要合并到 Host
            if (config.IsMonitorInHost)
            {
                var monitor = EnsureMonitorForm(deviceType);
                var host = GetOrCreateHostForDevice(deviceType);
                DetachFromOtherHost(deviceType, host);
                host.AttachMonitor(monitor);
                if (!host.Visible)
                {
                    host.Show();
                }
                MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(deviceType, true));
                return;
            }

            if (_monitorForms.TryGetValue(deviceType, out var monitorWindow) && monitorWindow != null && !monitorWindow.IsDisposed && monitorWindow.Visible)
            {
                monitorWindow.Close();
                return;
            }

            var monitorForm = EnsureMonitorForm(deviceType);
            monitorForm.Show();
            monitorForm.BringToFront();
            monitorForm.Activate();
            MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(deviceType, true));
        }

        // 确保监视器窗口存在并订阅关闭事件
        private DeviceMonitorForm EnsureMonitorForm(DeviceType deviceType)
        {
            var config = _appConfig.GetDeviceConfig(deviceType);
            var title = string.IsNullOrWhiteSpace(config.SelectedPort)
                ? $"{config.DeviceName} 打印"
                : $"{config.DeviceName} 打印 ({config.SelectedPort})";

            if (!_monitorForms.TryGetValue(deviceType, out var monitor) || monitor == null || monitor.IsDisposed)
            {
                var newMonitor = new DeviceMonitorForm(deviceType, title);
                 _monitorForms[deviceType] = newMonitor;

                 var dt = deviceType;
                 newMonitor.FormClosed += (s, args) =>
                 {
                     config.MonitorPosition = newMonitor.GetCurrentPosition();
                     _ = _coordinator.SaveConfigAsync();
                     _logger?.LogDebug("Monitor {Device} closed, firing MonitorStateChanged", dt);
                     MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(dt, false));
                 };

                newMonitor.MonitorDroppedOnMe += OnMonitorDroppedBetweenWindows;
             }
             else
             {
                 monitor.Text = title;
             }

             _monitorForms[deviceType]!.ApplyPosition(config.MonitorPosition);
             return _monitorForms[deviceType]!;
         }

        private void OnMonitorDroppedBetweenWindows(DeviceMonitorForm source, DeviceMonitorForm target)
        {
            // 唤起或创建 Host
            var fallbackPosition = target.GetCurrentPosition();
            var host = GetOrCreateHostForDevice(target.DeviceType, fallbackPosition) ?? GetOrCreateHostForDevice(source.DeviceType, fallbackPosition);

            DetachFromOtherHost(target.DeviceType, host);
            DetachFromOtherHost(source.DeviceType, host);
            host.AttachMonitor(target);
            host.AttachMonitor(source);
             if (!host.Visible)
             {
                 host.Show();
             }

            // 更新合并状态并通知
            _appConfig.GetDeviceConfig(target.DeviceType).IsMonitorInHost = true;
            _appConfig.GetDeviceConfig(source.DeviceType).IsMonitorInHost = true;
            MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(target.DeviceType, true));
            MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(source.DeviceType, true));
            _ = _coordinator.SaveConfigAsync();
        }

        // 检查指定设备监视器是否打开
        public bool IsMonitorOpen(DeviceType deviceType)
        {
            if (_deviceHostMap.TryGetValue(deviceType, out var mappedHost) && mappedHost != null && !mappedHost.IsDisposed)
            {
                if (mappedHost.TryGetMonitor(deviceType, out var hostMonitor) && hostMonitor != null)
                {
                    return hostMonitor.IsHandleCreated && hostMonitor.Visible;
                }
            }

            if (!_monitorForms.TryGetValue(deviceType, out var monitor))
                return false;
            if (monitor == null || monitor.IsDisposed)
                return false;
            return monitor.IsHandleCreated && monitor.Visible;
        }

        /// <summary>
        /// 打开所有设备的打印窗口
        /// </summary>
        public void OpenAllMonitors()
        {
            foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
            {
                var config = _appConfig.GetDeviceConfig(deviceType);
                var monitor = EnsureMonitorForm(deviceType);

                if (config.IsMonitorInHost)
                {
                    var host = GetOrCreateHostForDevice(deviceType);
                    DetachFromOtherHost(deviceType, host);
                    host.AttachMonitor(monitor);
                    if (!host.Visible)
                    {
                        host.Show();
                    }
                }
                else if (!IsMonitorOpen(deviceType))
                {
                    if (!monitor.Visible)
                    {
                        monitor.Show();
                        monitor.BringToFront();
                        monitor.Activate();
                    }
                }

                MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(deviceType, true));
            }
        }

        /// <summary>
        /// 关闭所有设备的打印窗口
        /// </summary>
        public void CloseAllMonitors()
        {
            SaveMonitorPositions();
            foreach (var host in _monitorHosts.ToArray())
            {
                if (host != null && !host.IsDisposed)
                {
                    host.Close();
                }
            }
            _monitorHosts.Clear();
            _deviceHostMap.Clear();

            foreach (var kvp in _monitorForms)
            {
                if (kvp.Value != null && !kvp.Value.IsDisposed && kvp.Value.Visible)
                {
                    kvp.Value.Close();
                }
            }
        }

        private SerialMonitorHostForm CreateHostForm(string? hostId = null, MonitorWindowPosition? position = null)
         {
             var host = new SerialMonitorHostForm();
            host.HostId = string.IsNullOrWhiteSpace(hostId) ? Guid.NewGuid().ToString("N") : hostId;
             host.MonitorMerged += OnMonitorMergedToHost;
             host.MonitorPopped += OnMonitorPoppedFromHost;
             host.ApplyPosition(position ?? _appConfig.MonitorHostPosition);
             host.FormClosing += async (_, _) =>
             {
                 try
                 {
                     SaveHostConfigs();
                     await _coordinator.SaveConfigAsync().ConfigureAwait(false);
                 }
                 catch (Exception ex)
                 {
                     _logger?.LogWarning(ex, "Error saving host configs during FormClosing");
                 }
             };
             host.FormClosed += (_, _) =>
             {
                // 先保存当前所有 Host 的位置与设备映射，确保关闭前已持久化
                SaveHostConfigs();
                _ = _coordinator.SaveConfigAsync();

                // 再清理运行时映射
                _monitorHosts.Remove(host);
                foreach (var kv in _deviceHostMap.Where(kv => kv.Value == host).ToList())
                {
                    _deviceHostMap.Remove(kv.Key);
                }
             };
             _monitorHosts.Add(host);
             return host;
         }

         private SerialMonitorHostForm GetOrCreateHostForDevice(DeviceType deviceType, MonitorWindowPosition? fallbackPosition = null)
         {
             if (_deviceHostMap.TryGetValue(deviceType, out var mappedHost) && mappedHost != null && !mappedHost.IsDisposed)
             {
                 return mappedHost;
             }
 
             var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
             var desiredHostId = deviceConfig.MonitorHostId;
             var isInHost = deviceConfig.IsMonitorInHost;
 
             // 尝试按配置的 HostId 找到现有 Host
             var host = _monitorHosts.FirstOrDefault(h => h != null && !h.IsDisposed && !string.IsNullOrWhiteSpace(desiredHostId) && string.Equals(h.HostId, desiredHostId, StringComparison.OrdinalIgnoreCase));
 
             // 若未找到：
             if (host == null)
             {
                 // 如果有指定 HostId，优先用该 HostId 创建新的 Host
                if (!string.IsNullOrWhiteSpace(desiredHostId))
                {
                    var hostConfig = _appConfig.MonitorHosts.FirstOrDefault(h => string.Equals(h.HostId, desiredHostId, StringComparison.OrdinalIgnoreCase));
                    var position = hostConfig?.Position ?? fallbackPosition ?? _appConfig.MonitorHostPosition;
                    host = CreateHostForm(desiredHostId, position);
                }
                else if (isInHost)
                {
                    // 无指定 HostId 但标记在 Host：复用已有第一个，否则创建新 Host
                    host = _monitorHosts.FirstOrDefault(h => h != null && !h.IsDisposed)
                           ?? CreateHostForm(null, fallbackPosition ?? _appConfig.MonitorHostPosition);
                }
                else
                {
                    // 不在 Host，直接创建独立 Host 以防误合并
                    host = CreateHostForm(null, fallbackPosition ?? _appConfig.MonitorHostPosition);
                }
             }
 
             return host;
         }
 
         /// <summary>
         /// 附加到目标 Host 前，先从其他 Host 脱离，避免出现重复窗口。
         /// </summary>
         private void DetachFromOtherHost(DeviceType deviceType, SerialMonitorHostForm targetHost)
         {
             if (_deviceHostMap.TryGetValue(deviceType, out var existingHost) && existingHost != null && existingHost != targetHost && !existingHost.IsDisposed)
             {
                 existingHost.PopOut(deviceType);
             }
         }
 
         private void OnMonitorMergedToHost(DeviceType deviceType, SerialMonitorHostForm host)
         {
             var config = _appConfig.GetDeviceConfig(deviceType);
             _deviceHostMap[deviceType] = host;
             config.IsMonitorInHost = true;
             config.MonitorHostId = host.HostId;
             EnsureHostConfigMembership(host.HostId, deviceType, host.GetCurrentPosition());
             _ = _coordinator.SaveConfigAsync();
             MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(deviceType, true));
         }
 
         private void OnMonitorPoppedFromHost(DeviceType deviceType, DeviceMonitorForm monitor, SerialMonitorHostForm host)
         {
             var config = _appConfig.GetDeviceConfig(deviceType);
             config.IsMonitorInHost = false;
             config.MonitorHostId = null;
             _deviceHostMap.Remove(deviceType);
             RemoveDeviceFromHostConfig(host.HostId, deviceType);
             _monitorForms[deviceType] = monitor;
             monitor.StartPosition = FormStartPosition.CenterParent;
             if (!monitor.Visible)
             {
                 monitor.Show();
             }
             else
             {
                 monitor.BringToFront();
             }
             _ = _coordinator.SaveConfigAsync();
             MonitorStateChanged?.Invoke(this, new MonitorStateChangedEventArgs(deviceType, true));
         }

         private void SaveMonitorPositions()
         {
            SaveHostConfigs();
             _ = _coordinator.SaveConfigAsync();
         }
 
         private void SaveHostConfigs()
         {
             var hostMap = (_appConfig.MonitorHosts ?? new List<MonitorHostConfig>())
                 .Where(h => h != null && !string.IsNullOrWhiteSpace(h.HostId))
                 .ToDictionary(h => h.HostId!, StringComparer.OrdinalIgnoreCase);
 
             foreach (var host in _monitorHosts.Where(h => h != null && !h.IsDisposed))
             {
                 var devices = _deviceHostMap.Where(kv => kv.Value == host).Select(kv => kv.Key).Distinct().ToList();
                 hostMap[host.HostId] = new MonitorHostConfig
                 {
                     HostId = host.HostId,
                     Position = host.GetCurrentPosition(),
                     Devices = devices
                 };
             }
 
             var hostConfigs = hostMap.Values
                 .Where(h => h != null && (!string.IsNullOrWhiteSpace(h.HostId)) && ((h.Devices?.Count ?? 0) > 0 || h.Position != null))
                 .ToList();
 
             _appConfig.MonitorHosts = hostConfigs;
             _appConfig.MonitorHostPosition = hostConfigs.FirstOrDefault()?.Position;
         }
 
         private void EnsureHostConfigMembership(string hostId, DeviceType deviceType, MonitorWindowPosition? position)
         {
             if (string.IsNullOrWhiteSpace(hostId))
                 return;
 
             var hostConfig = _appConfig.MonitorHosts.FirstOrDefault(h => string.Equals(h.HostId, hostId, StringComparison.OrdinalIgnoreCase));
             if (hostConfig == null)
             {
                 hostConfig = new MonitorHostConfig
                 {
                     HostId = hostId,
                     Position = position,
                     Devices = new List<DeviceType>()
                 };
                 _appConfig.MonitorHosts.Add(hostConfig);
             }
 
             if (position != null)
             {
                 hostConfig.Position = position;
             }
 
             if (!hostConfig.Devices.Contains(deviceType))
             {
                 hostConfig.Devices.Add(deviceType);
             }
 
             var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
             deviceConfig.IsMonitorInHost = true;
             deviceConfig.MonitorHostId = hostId;
         }
 
         private void RemoveDeviceFromHostConfig(string? hostId, DeviceType deviceType)
         {
             if (string.IsNullOrWhiteSpace(hostId))
                 return;
             var hostConfig = _appConfig.MonitorHosts.FirstOrDefault(h => string.Equals(h.HostId, hostId, StringComparison.OrdinalIgnoreCase));
             if (hostConfig != null)
             {
                 hostConfig.Devices.Remove(deviceType);
                 if (hostConfig.Devices.Count == 0 && hostConfig.Position == null)
                 {
                     _appConfig.MonitorHosts.Remove(hostConfig);
                 }
             }
         }
 
         private void RestoreHostsFromConfig()
         {
             foreach (var hostConfig in _appConfig.MonitorHosts)
             {
                 var host = CreateHostForm(hostConfig.HostId, hostConfig.Position);
                 foreach (var deviceType in hostConfig.Devices)
                 {
                     _deviceHostMap[deviceType] = host;
                     var deviceConfig = _appConfig.GetDeviceConfig(deviceType);
                     deviceConfig.IsMonitorInHost = true;
                     deviceConfig.MonitorHostId = host.HostId;
                 }
             }
         }

        #endregion

        #region 设置窗口

        private void menuSettings_Click(object? sender, EventArgs e)
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                var presenter = new MultiDeviceSettingsPresenter(_coordinator, this, null);
                _settingsForm = new MultiDeviceSettingsForm(_appConfig, this, presenter);
                _settingsForm.SettingsConfirmed += async (_, _) => await ApplySettingsFromDialogAsync();
                _settingsForm.DeviceSettingsChanged += async (_, args) => await ApplyDeviceSettingsAsync(args);
                _settingsForm.FormClosing += async (_, _) =>
                {
                    try
                    {
                        _appConfig.SettingsWindowPosition = _settingsForm.GetCurrentPosition();
                        await _coordinator.SaveConfigAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error saving settings window position");
                    }
                };
                _settingsForm.FormClosed += (_, _) => _settingsForm = null;
            }

            _settingsForm.StartPosition = FormStartPosition.CenterScreen;
            _settingsForm.Show();
            _settingsForm.BringToFront();
        }

        // 应用单个设备的设置变更（锁定时立即保存）
        private async Task ApplyDeviceSettingsAsync(DeviceSettingsChangedEventArgs args)
        {
            try
            {
                // 如果解锁且设备正在连接中，先断开连接
                if (!args.IsLocked && _coordinator.IsConnected(args.DeviceType))
                {
                    _logger?.LogInformation("Device {Device} unlocked, disconnecting...", args.DeviceType);
                    await _coordinator.DisconnectAsync(args.DeviceType);
                    // DisconnectAsync 完成后，OnConnectionStateChanged 会自动更新 UI
                }

                _coordinator.TryUpdateConnectionConfig(args.DeviceType, args.Port, args.BaudRate, args.IsLocked);
                await _coordinator.SaveConfigAsync();

                _logger?.LogInformation("Device {Device} settings saved: Port={Port}, BaudRate={BaudRate}, Locked={Locked}", 
                    args.DeviceType, args.Port, args.BaudRate, args.IsLocked);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error saving device settings for {Device}", args.DeviceType);
            }
        }

        private async Task ApplySettingsFromDialogAsync()
        {
            if (_settingsForm == null)
                return;

            try
            {
                // 从设置窗口获取所有设备配置并更新
                foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
                {
                    var (port, baudRate, isLocked) = _settingsForm.GetDeviceSettings(deviceType);
                    _coordinator.TryUpdateConnectionConfig(deviceType, port, baudRate, isLocked);
                }

                await _coordinator.SaveConfigAsync();
                UpdateAllDevicesUI();

                // 更新所有打开的监视器窗口标题
                foreach (var kvp in _monitorForms)
                {
                    if (kvp.Value != null && !kvp.Value.IsDisposed)
                    {
                        var config = _appConfig.GetDeviceConfig(kvp.Key);
                        kvp.Value.Text = string.IsNullOrWhiteSpace(config.SelectedPort)
                            ? $"{config.DeviceName} 打印"
                            : $"{config.DeviceName} 打印 ({config.SelectedPort})";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error saving settings");
            }
        }

        #endregion

        #region 窗口管理

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDesignMode || _coordinator == null)
            {
                base.OnFormClosing(e);
                return;
            }

            try
            {
                _coordinator.ConnectionStateChanged -= OnConnectionStateChanged;
                _coordinator.DataReceived -= OnSerialDataReceived;
                _coordinator.DataSent -= OnSerialDataSent;
                _coordinator.DeviceStatusChanged -= OnDeviceStatusChanged;
                _optionsChangeToken?.Dispose();

                if (_settingsForm != null)
                {
                    _settingsForm.Dispose();
                    _settingsForm = null;
                }

                // 保存监视窗口位置
                SaveMonitorPositions();

                // 关闭所有监视器窗口
                foreach (var host in _monitorHosts.ToArray())
                {
                    if (host != null && !host.IsDisposed)
                    {
                        host.Close();
                    }
                }
                _monitorHosts.Clear();
                _deviceHostMap.Clear();

                foreach (var kvp in _monitorForms)
                {
                    if (kvp.Value != null && !kvp.Value.IsDisposed)
                    {
                        kvp.Value.Close();
                    }
                }
                _monitorForms.Clear();

                _coordinator.Dispose();
                UIHelper.DisposeFonts();
            }
            catch
            {
                // 忽略释放错误
            }

            base.OnFormClosing(e);
        }

        #endregion

        private async void btnOpenAllMonitors_Click(object? sender, EventArgs e)
        {
            btnOpenAllMonitors.Enabled = false;
            btnCloseAllMonitors.Enabled = false;
            try
            {
                OpenAllMonitors();
            }
            finally
            {
                btnOpenAllMonitors.Enabled = true;
                btnCloseAllMonitors.Enabled = true;
            }
        }

        private async void btnCloseAllMonitors_Click(object? sender, EventArgs e)
        {
            btnOpenAllMonitors.Enabled = false;
            btnCloseAllMonitors.Enabled = false;
            try
            {
                CloseAllMonitors();
            }
            finally
            {
                btnOpenAllMonitors.Enabled = true;
                btnCloseAllMonitors.Enabled = true;
            }
        }
    }
}

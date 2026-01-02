using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Infrastructure.Constants;
using TestTool.Infrastructure.Helpers;
using TestTool.Forms.Base;

namespace TestTool
{
    /// <summary>
    /// 多设备设置窗口：统一管理 4 个设备的串口配置
    /// </summary>
    public partial class MultiDeviceSettingsForm : ResizableFormBase, IMultiDeviceSettingsView
    {
        private readonly AppConfig _appConfig;
        private readonly MainForm _mainForm;
        private readonly IMultiDeviceSettingsPresenter? _presenter;
        private EventHandler<MonitorStateChangedEventArgs>? _monitorStateHandler;

        // 每设备的控件集合
        private readonly Dictionary<DeviceType, DeviceSettingsPanel> _devicePanels = new();

        // 设置确认事件
        public event EventHandler? SettingsConfirmed;

        // 单个设备设置变化事件（锁定时触发）
        public event EventHandler<DeviceSettingsChangedEventArgs>? DeviceSettingsChanged;

        public MultiDeviceSettingsForm() : this(new AppConfig(), null!)
        {
        }

        public MultiDeviceSettingsForm(AppConfig appConfig, MainForm mainForm)
        {
            _appConfig = appConfig ?? new AppConfig();
            _mainForm = mainForm;
            InitializeComponent();
        }

        public MultiDeviceSettingsForm(AppConfig appConfig, MainForm mainForm, IMultiDeviceSettingsPresenter presenter)
            : this(appConfig, mainForm)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.Bind(this);
            _monitorStateHandler = (_, e) => _presenter.UpdateMonitorState(e.DeviceType, e.IsOpen);
            _mainForm.MonitorStateChanged += _monitorStateHandler;
        }

        private void InitializeComponent()
        {
            this.Text = "串口设置";
            this.Size = new Size(560, 380);  // 增加宽度
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new Size(560, 380);  // 增加最小宽度
            this.StartPosition = FormStartPosition.CenterParent;

            int panelHeight = 60;
            int startY = 10;
            int currentY = startY;

            // 为每个设备创建设置面板，传入获取已锁定端口的委托
            foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
            {
                var config = _appConfig.GetDeviceConfig(deviceType);
                var panel = new DeviceSettingsPanel(deviceType, config, _mainForm, GetLockedPortsExcept);
                panel.Location = new Point(10, currentY);
                panel.Size = new Size(520, panelHeight);  // 增加面板宽度
                panel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;  // 自适应宽度

                // 订阅面板的锁定事件
                panel.SettingsLocked += OnPanelSettingsLocked;

                this.Controls.Add(panel);
                _devicePanels[deviceType] = panel;
                currentY += panelHeight + 5;
            }

            // 底部按钮：一键锁定和一键解锁
            var btnLockAll = new Button
            {
                Text = "一键锁定",
                Location = new Point(300, currentY + 10),  // 调整位置
                Size = new Size(90, 30),
                BackColor = Color.LightGreen,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right  // 锚定到右下
            };
            btnLockAll.Click += BtnLockAll_Click;

            var btnUnlockAll = new Button
            {
                Text = "一键解锁",
                Location = new Point(400, currentY + 10),
                Size = new Size(90, 30),
                BackColor = Color.LightCoral,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right  // 锚定到右下
            };
            btnUnlockAll.Click += BtnUnlockAll_Click;

            this.Controls.Add(btnLockAll);
            this.Controls.Add(btnUnlockAll);
        }

        /// <summary>
        /// 获取除指定设备外，其他已锁定设备占用的串口列表
        /// </summary>
        private HashSet<string> GetLockedPortsExcept(DeviceType excludeDevice)
        {
            var lockedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in _devicePanels)
            {
                if (kvp.Key == excludeDevice)
                    continue;

                var (port, _, isLocked) = kvp.Value.GetSettings();
                if (isLocked && !string.IsNullOrEmpty(port))
                {
                    lockedPorts.Add(port);
                }
            }

            return lockedPorts;
        }

        // 面板锁定时立即触发保存，并通知其他面板刷新串口列表
        private void OnPanelSettingsLocked(object? sender, DeviceSettingsChangedEventArgs e)
        {
            DeviceSettingsChanged?.Invoke(this, e);

            // 通知所有其他面板刷新可用串口（排除已锁定的）
            foreach (var kvp in _devicePanels)
            {
                if (kvp.Key != e.DeviceType)
                {
                    kvp.Value.RefreshAvailablePorts();
                }
            }
        }

        // 一键锁定所有设备
        private void BtnLockAll_Click(object? sender, EventArgs e)
        {
            foreach (var kvp in _devicePanels)
            {
                kvp.Value.SetLocked(true);
            }
        }

        // 一键解锁所有设备
        private void BtnUnlockAll_Click(object? sender, EventArgs e)
        {
            foreach (var kvp in _devicePanels)
            {
                kvp.Value.SetLocked(false);
            }
        }

        /// <summary>
        /// 获取指定设备的设置
        /// </summary>
        public (string port, int baudRate, bool isLocked) GetDeviceSettings(DeviceType deviceType)
        {
            if (_devicePanels.TryGetValue(deviceType, out var panel))
            {
                return panel.GetSettings();
            }
            return (string.Empty, 115200, false);
        }

        public void RefreshAvailablePorts(DeviceType deviceType)
        {
            foreach (var kvp in _devicePanels)
            {
                if (kvp.Key != deviceType)
                {
                    kvp.Value.RefreshAvailablePorts();
                }
            }
        }

        public void SetMonitorState(DeviceType deviceType, bool isOpen)
        {
            if (_devicePanels.TryGetValue(deviceType, out var panel))
            {
                UIHelper.SafeInvoke(this, () => panel.SetMonitorState(isOpen));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _mainForm != null && _monitorStateHandler != null)
            {
                _mainForm.MonitorStateChanged -= _monitorStateHandler;
                _monitorStateHandler = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 设备设置变化事件参数
    /// </summary>
    public class DeviceSettingsChangedEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public string Port { get; }
        public int BaudRate { get; }
        public bool IsLocked { get; }

        public DeviceSettingsChangedEventArgs(DeviceType deviceType, string port, int baudRate, bool isLocked)
        {
            DeviceType = deviceType;
            Port = port;
            BaudRate = baudRate;
            IsLocked = isLocked;
        }
    }

    /// <summary>
    /// 单个设备的设置面板
    /// </summary>
    public class DeviceSettingsPanel : Panel
    {
        private static readonly object _portCacheLock = new();
        private static string[] _cachedPorts = Array.Empty<string>();
        private static DateTime _lastPortRefresh = DateTime.MinValue;
        private const int PortCacheTtlMs = 1000;

        private readonly DeviceType _deviceType;
        private readonly ComboBox _cmbPort;
        private readonly ComboBox _cmbBaudRate;
        private readonly Button _btnLock;
        private readonly Button _btnMonitor;
        private readonly MainForm? _mainForm;
        private readonly Func<DeviceType, HashSet<string>>? _getLockedPorts;
        private bool _isLocked;

        // 锁定时触发的事件（立即保存）
        public event EventHandler<DeviceSettingsChangedEventArgs>? SettingsLocked;

        public DeviceSettingsPanel(DeviceType deviceType, DeviceConfig config, MainForm? mainForm, Func<DeviceType, HashSet<string>>? getLockedPorts = null)
        {
            _deviceType = deviceType;
            _mainForm = mainForm;
            _getLockedPorts = getLockedPorts;
            _isLocked = config.IsPortLocked;

            this.BorderStyle = BorderStyle.FixedSingle;
            this.Size = new Size(520, 60);  // 增加面板宽度

            // 设备名称标签
            var lblName = new Label
            {
                Text = config.DeviceName,
                Location = new Point(5, 20),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // 串口选择
            _cmbPort = new ComboBox
            {
                Location = new Point(120, 17),
                Size = new Size(110, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            LoadAvailablePorts();
            if (!string.IsNullOrEmpty(config.SelectedPort) && _cmbPort.Items.Contains(config.SelectedPort))
            {
                _cmbPort.SelectedItem = config.SelectedPort;
            }
            else if (_cmbPort.Items.Count > 0)
            {
                _cmbPort.SelectedIndex = 0;
            }
            _cmbPort.DropDown += (_, _) => RefreshAvailablePorts();

            // 波特率选择
            _cmbBaudRate = new ComboBox
            {
                Location = new Point(240, 17),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            LoadBaudRates();
            var baudRate = config.ConnectionSettings?.BaudRate ?? 115200;
            if (_cmbBaudRate.Items.Contains(baudRate))
            {
                _cmbBaudRate.SelectedItem = baudRate;
            }
            else
            {
                _cmbBaudRate.SelectedIndex = 0;
            }

            // 锁定按钮 - 使用左锚定，固定位置
            _btnLock = new Button
            {
                Location = new Point(350, 15),
                Size = new Size(80, 28),
                Text = _isLocked ? "已锁定" : "未锁定",
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            UpdateLockButtonState();
            _btnLock.Click += BtnLock_Click;

            // 打印按钮 - 使用左锚定，固定位置
            _btnMonitor = new Button
            {
                Location = new Point(440, 15),
                Size = new Size(90, 28),
                Text = "打印",
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _btnMonitor.Click += BtnMonitor_Click;

            this.Controls.Add(lblName);
            this.Controls.Add(_cmbPort);
            this.Controls.Add(_cmbBaudRate);
            this.Controls.Add(_btnLock);
            this.Controls.Add(_btnMonitor);

            // 根据锁定状态启用/禁用控件
            UpdateControlsState();
            // 初始化打印按钮状态
            UpdateMonitorButtonText();

            // 订阅主窗体的监视器状态变化事件
            if (_mainForm != null)
            {
                _mainForm.MonitorStateChanged += OnMonitorStateChanged;
            }
        }

        // 监视器状态变化回调
        private void OnMonitorStateChanged(object? sender, MonitorStateChangedEventArgs e)
        {
            if (e.DeviceType == _deviceType)
            {
                UIHelper.SafeInvoke(this, () => UpdateMonitorButtonState(e.IsOpen));
            }
        }

        private void LoadAvailablePorts()
        {
            _cmbPort.Items.Clear();
            var allPorts = GetCachedPorts();

            // 获取其他设备已锁定的串口
            var lockedPorts = _getLockedPorts?.Invoke(_deviceType) ?? new HashSet<string>();

            // 过滤掉已被其他设备锁定的串口
            var availablePorts = allPorts.Where(p => !lockedPorts.Contains(p)).ToArray();

            if (availablePorts.Length > 0)
            {
                _cmbPort.Items.AddRange(availablePorts);
            }
            else
            {
                _cmbPort.Items.Add("无可用串口");
            }
        }

        private static string[] GetCachedPorts()
        {
            var now = DateTime.UtcNow;
            lock (_portCacheLock)
            {
                if ((now - _lastPortRefresh).TotalMilliseconds < PortCacheTtlMs && _cachedPorts.Length > 0)
                {
                    return _cachedPorts;
                }
                _cachedPorts = SerialPort.GetPortNames();
                _lastPortRefresh = now;
                return _cachedPorts;
            }
        }

        private void LoadBaudRates()
        {
            _cmbBaudRate.Items.Clear();
            int[] rates = { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
            foreach (var rate in rates)
            {
                _cmbBaudRate.Items.Add(rate);
            }
        }

        private void BtnLock_Click(object? sender, EventArgs e)
        {
            if (!_isLocked && !ValidateSettings())
            {
                return;
            }

            SetLocked(!_isLocked);
        }

        private void BtnMonitor_Click(object? sender, EventArgs e)
        {
            _mainForm?.ToggleMonitor(_deviceType);
            // ToggleMonitor 会触发 MonitorStateChanged 事件，按钮状态会自动更新
        }

        private void UpdateLockButtonState()
        {
            if (_isLocked)
            {
                _btnLock.Text = "已锁定";
                _btnLock.BackColor = Color.LightCoral;
            }
            else
            {
                _btnLock.Text = "未锁定";
                _btnLock.BackColor = SystemColors.Control;
            }
        }

        private void UpdateControlsState()
        {
            _cmbPort.Enabled = !_isLocked;
            _cmbBaudRate.Enabled = !_isLocked;
        }

        private void UpdateMonitorButtonText()
        {
            if (_mainForm != null)
            {
                var isOpen = _mainForm.IsMonitorOpen(_deviceType);
                UpdateMonitorButtonState(isOpen);
            }
        }

        private void UpdateMonitorButtonState(bool isOpen)
        {
            _btnMonitor.Text = isOpen ? "关闭打印" : "打印";
            _btnMonitor.BackColor = isOpen ? Color.LightBlue : SystemColors.Control;
        }

        public void SetMonitorState(bool isOpen)
        {
            UpdateMonitorButtonState(isOpen);
        }

        public bool ValidateSettings()
        {
            var port = _cmbPort.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(port) || port == "无可用串口")
            {
                MessageBox.Show($"{_deviceType} 未选择可用串口，无法锁定。", "设置校验", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var lockedPorts = _getLockedPorts?.Invoke(_deviceType);
            if (lockedPorts != null && lockedPorts.Contains(port))
            {
                MessageBox.Show($"{_deviceType} 选择的串口 {port} 已被其他设备占用。", "串口占用提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(_cmbBaudRate.Text, out var baudRate) || baudRate <= 0)
            {
                MessageBox.Show($"请为 {_deviceType} 输入有效的波特率", "设置校验", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        public (string port, int baudRate, bool isLocked) GetSettings()
        {
            var port = _cmbPort.SelectedItem?.ToString() ?? string.Empty;
            if (port == "无可用串口") port = string.Empty;

            int.TryParse(_cmbBaudRate.Text, out var baudRate);
            if (baudRate <= 0) baudRate = 115200;

            return (port, baudRate, _isLocked);
        }

        /// <summary>
        /// 设置锁定状态（供外部一键锁定/解锁调用）
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (_isLocked == locked)
                return;

            var (port, baudRate, _) = GetSettings();

            _isLocked = locked;
            UpdateLockButtonState();
            UpdateControlsState();

            SettingsLocked?.Invoke(this, new DeviceSettingsChangedEventArgs(_deviceType, port, baudRate, _isLocked));
        }

        /// <summary>
        /// 刷新可用串口列表（公开方法，供外部调用）
        /// </summary>
        public void RefreshAvailablePorts()
        {
            // 如果已锁定，不刷新
            if (_isLocked)
                return;

            var currentSelection = _cmbPort.SelectedItem?.ToString();
            LoadAvailablePorts();

            var hasConflict = !string.IsNullOrEmpty(currentSelection) && !_cmbPort.Items.Contains(currentSelection) && currentSelection != "无可用串口";

            // 尝试恢复之前的选择
            if (!string.IsNullOrEmpty(currentSelection) && _cmbPort.Items.Contains(currentSelection))
            {
                _cmbPort.SelectedItem = currentSelection;
            }
            else if (_cmbPort.Items.Count > 0 && _cmbPort.SelectedIndex == -1)
            {
                _cmbPort.SelectedIndex = 0;
            }

            if (hasConflict && _cmbPort.SelectedItem != null && _cmbPort.SelectedItem.ToString() != currentSelection)
            {
                MessageBox.Show($"{_deviceType} 当前选择的串口已被占用，已切换到 {_cmbPort.SelectedItem}。", "串口占用提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _mainForm != null)
            {
                _mainForm.MonitorStateChanged -= OnMonitorStateChanged;
            }
            base.Dispose(disposing);
        }
    }
}

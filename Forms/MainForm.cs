using System;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestTool.Business.Enums;
using TestTool.Business.Models;
using TestTool.Business.Services;
using TestTool.Infrastructure.Constants;
using TestTool.Infrastructure.Helpers;

namespace TestTool
{
    /// <summary>
    /// 串口工具主窗体 - 重构版本
    /// </summary>
    public partial class MainForm : Form
    {
        // 业务协调器：封装连接/命令/配置
        private readonly IMainFormCoordinator _coordinator = null!;
        private IOptionsMonitor<AppConfig>? _optionsMonitor;
        private IDisposable? _optionsChangeToken;
        private readonly ILogger<MainForm>? _logger;
        private readonly bool _isDesignMode;

        // 应用配置缓存（从仓库加载）
        private AppConfig _appConfig = null!;
        // 串口监视窗口实例（可为空）
        private SerialMonitorForm? _serialMonitorForm;

        // 用于自定义窗体可缩放区域的把手尺寸常量
        private const int RESIZE_HANDLE_SIZE = AppConstants.UI.ResizeHandleSize;

        // 参数less 构造：供 WinForms 设计器使用（不要在此处进行服务绑定）
        public MainForm()
        {
            _isDesignMode = true;
            // 注册编码提供者以支持更多编码（如 GBK）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            UIHelper.InitializeFonts(this.Font);
        }

        // 构造函数：通过 DI 注入依赖
        public MainForm(IMainFormCoordinator coordinator, IOptionsMonitor<AppConfig> optionsMonitor, ILogger<MainForm>? logger = null)
            : this()
        {
            // 在 DI 构造器中覆盖设计时设置
            _isDesignMode = false;
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger;
            _optionsChangeToken = _optionsMonitor.OnChange(OnAppConfigChanged);
        }

        // 配置变更回调：合并来自 appsettings 的更新并刷新 UI
        private void OnAppConfigChanged(AppConfig newConfig)
        {
            try
            {
                // 仅更新允许由 appsettings 控制的字段（不覆盖用户已保存的选择）
                if (_appConfig == null)
                {
                    _appConfig = newConfig;
                }
                else
                {
                    _appConfig.DeviceName = newConfig.DeviceName ?? _appConfig.DeviceName;
                    // 如果用户未选择端口，则使用配置中的端口
                    if (string.IsNullOrWhiteSpace(_appConfig.SelectedPort))
                    {
                        _appConfig.SelectedPort = newConfig.SelectedPort ?? _appConfig.SelectedPort;
                    }
                    _appConfig.ConnectionSettings = (newConfig.ConnectionSettings ?? _appConfig.ConnectionSettings).NormalizeWithDefaults();
                }

                // 在 UI 线程更新显示（如果需要）
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UpdateUI));
                }
                else
                {
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error applying AppConfig change");
            }
        }

        // 窗体加载事件：异步加载配置并初始化设备控制器与事件订阅
        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (_isDesignMode)
                return; // 在设计模式下不执行运行时初始化

            try
            {
                await _coordinator.InitializeAsync();
                _appConfig = _coordinator.AppConfig;

                _coordinator.ConnectionStateChanged += OnConnectionStateChanged;
                _coordinator.DataReceived += OnSerialDataReceived;
                _coordinator.DataSent += OnSerialDataSent;
                _coordinator.DeviceStatusChanged += OnDeviceStatusChanged;

                // 根据初始配置更新界面控件状态
                UpdateUI();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MainForm");
            }
        }

        // 处理串口发送事件：转发到监视器显示发送记录
        private void OnSerialDataSent(object? sender, DataSentEventArgs e)
        {
            if (_serialMonitorForm == null || _serialMonitorForm.IsDisposed || !_serialMonitorForm.Visible)
            {
                return;
            }

            // 在监视器中以发送样式追加文本
            _serialMonitorForm.AppendSent(e.Command);
        }

        // 连接按钮点击：根据当前连接状态执行连接或断开
        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            // 防止重复点击导致并发操作
            if (!btnConnect.Enabled)
            {
                return;
            }

            if (_coordinator.IsConnected)
            {
                await _coordinator.DisconnectAsync();
                return;
            }

            if (string.IsNullOrEmpty(_appConfig.SelectedPort) || _appConfig.SelectedPort == "无可用串口")
            {
                UIHelper.SetStatusLabel(lblStatus, ConnectionState.Error, _appConfig.DeviceName, UIConstants.StatusMessages.PleaseSelectPort);
                return;
            }

            btnConnect.Enabled = false;
            UIHelper.SetStatusLabel(lblStatus, ConnectionState.Connecting, _appConfig.DeviceName, UIConstants.StatusMessages.Connecting);

            try
            {
                await _coordinator.ConnectAsync();
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        // 从设置界面切换监视器显示（确保单例）
        private void ToggleMonitorFromSettings()
        {
            if (_serialMonitorForm != null && !_serialMonitorForm.IsDisposed && _serialMonitorForm.Visible)
            {
                _serialMonitorForm.Close();
                return;
            }

            EnsureMonitorForm();
            _serialMonitorForm!.Show(this);
        }

        // 打开电源按钮处理：委托给设备控制器
        private async void btnOn_Click(object? sender, EventArgs e)
        {
            await _coordinator.TurnOnAsync();
        }

        // 关闭电源按钮处理：委托给设备控制器
        private async void btnOff_Click(object? sender, EventArgs e)
        {
            await _coordinator.TurnOffAsync();
        }

        // 菜单设置点击：弹出设置窗体并处理结果
        private async void menuSettings_Click(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_appConfig.SelectedPort, _appConfig.IsPortLocked, _appConfig.DeviceName, _serialMonitorForm?.Visible == true))
            {
                // 点击设置中切换打印时调用
                settingsForm.ToggleMonitorRequested += (_, _) => ToggleMonitorFromSettings();

                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // 保存用户更新的配置
                    _appConfig.SelectedPort = settingsForm.SelectedPort;
                    _appConfig.IsPortLocked = settingsForm.IsPortLocked;

                    try
                    {
                        await _coordinator.SaveConfigAsync();
                    }
                    catch
                    {
                        // 忽略保存错误，不阻塞 UI
                    }
                }
            }
        }

        // 连接状态变化处理：在 UI 线程更新状态标签和按钮状态
        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionStateChanged(sender, e)));
                return;
            }

            UIHelper.SetStatusLabel(lblStatus, e.NewState, _appConfig.DeviceName, e.Message);
            UIHelper.UpdateConnectButton(btnConnect, e.NewState == ConnectionState.Connected);

            // 根据连接状态启/禁用电源按钮
            if (e.NewState == ConnectionState.Connected)
            {
                btnOn.Enabled = true;
                btnOff.Enabled = true;
                UIHelper.SetButtonDefault(btnOn);
                UIHelper.SetButtonDefault(btnOff);
            }
            else
            {
                UIHelper.SetButtonDisabled(btnOn);
                UIHelper.SetButtonDisabled(btnOff);
            }
        }

        // 设备状态变化处理：更新电源按钮外观以反映当前电源状态
        private void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDeviceStatusChanged(sender, e)));
                return;
            }

            if (e.Status.PowerState == DevicePowerState.On)
            {
                UIHelper.SetButtonActive(btnOn, DevicePowerState.On);
                UIHelper.SetButtonDefault(btnOff);
            }
            else if (e.Status.PowerState == DevicePowerState.Off)
            {
                UIHelper.SetButtonDefault(btnOn);
                UIHelper.SetButtonActive(btnOff, DevicePowerState.Off);
            }
            else
            {
                UIHelper.SetButtonDefault(btnOn);
                UIHelper.SetButtonDefault(btnOff);
            }
        }

        // 串口接收数据处理：将收到的数据追加到监视器
        private void OnSerialDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (_serialMonitorForm == null || _serialMonitorForm.IsDisposed || !_serialMonitorForm.Visible)
            {
                return;
            }

            // 使用兼容的 AppendLine（映射为接收显示）
            _serialMonitorForm.AppendLine(e.Data);
        }

        // 确保监视器窗口存在并设置标题
        private void EnsureMonitorForm()
        {
            if (_serialMonitorForm == null || _serialMonitorForm.IsDisposed)
            {
                var title = string.IsNullOrWhiteSpace(_appConfig?.SelectedPort)
                    ? "串口打印"
                    : $"{_appConfig.DeviceName} 打印 ({_appConfig.SelectedPort})";
                _serialMonitorForm = new SerialMonitorForm(title);
            }
            else
            {
                _serialMonitorForm.Text = string.IsNullOrWhiteSpace(_appConfig?.SelectedPort)
                    ? "串口打印"
                    : $"{_appConfig.DeviceName} 打印 ({_appConfig.SelectedPort})";
            }
        }

        // 更新 UI 初始状态：设置状态标签和禁用电源按钮
        private void UpdateUI()
        {
            UIHelper.SetStatusLabel(lblStatus, ConnectionState.Disconnected, _appConfig.DeviceName, UIConstants.StatusMessages.Disconnected);
            UIHelper.SetButtonDisabled(btnOn);
            UIHelper.SetButtonDisabled(btnOff);
        }

        // 窗口边框拖动支持：处理 WM_NCHITTEST 并为边角返回相应 HT 值
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                var cursor = this.PointToClient(Cursor.Position);

                if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (cursor.X <= RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTLEFT;
                else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTRIGHT;
                else if (cursor.Y <= RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTTOP;
                else if (cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
                    m.Result = (IntPtr)HTBOTTOM;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        // 窗口关闭时释放资源与取消订阅，防止回调继续触发
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _coordinator.ConnectionStateChanged -= OnConnectionStateChanged;
                _coordinator.DataReceived -= OnSerialDataReceived;
                _coordinator.DataSent -= OnSerialDataSent;
                _coordinator.DeviceStatusChanged -= OnDeviceStatusChanged;
                _optionsChangeToken?.Dispose();
                _coordinator.Dispose();
                UIHelper.DisposeFonts();
            }
            catch
            {
                // 忽略释放错误，确保窗口能关闭
            }

            base.OnFormClosing(e);
        }
    }
}

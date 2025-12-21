using System;
using System.IO.Ports;
using System.Drawing;
using System.Windows.Forms;
using TestTool.Infrastructure.Constants;

namespace TestTool
{
    /// <summary>
    /// 设置对话框：选择串口、锁定端口，并打开/关闭打印监视器
    /// </summary>
    public partial class SettingsForm : Form
    {
        // 用户选择的串口号
        public string SelectedPort { get; private set; } = string.Empty;
        // 串口是否被锁定以防修改
        public bool IsPortLocked { get; private set; }
        // 设备名称显示文本
        private string deviceName;
        // 当前监视器是否已打开
        private bool _isMonitorOpen;

        // 设置界面中切换串口打印监视器的事件
        public event EventHandler? ToggleMonitorRequested;

        // 参数less 构造函数：供设计器使用，使用默认值委托给主构造函数
        public SettingsForm() : this(string.Empty, false, AppConstants.Defaults.DeviceName, false)
        {
        }

        // 构造函数：接受当前配置作为初始显示值
        public SettingsForm(string currentPort, bool isLocked, string devName, bool isMonitorOpen)
        {
            InitializeComponent();
            SelectedPort = currentPort ?? string.Empty;
            IsPortLocked = isLocked;
            deviceName = devName ?? string.Empty;
            _isMonitorOpen = isMonitorOpen;
        }

        // 窗体加载时初始化控件显示与事件
        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            // 设置窗口标题为设备名称 + 设置
            this.Text = $"{deviceName}设置";

            // 更新分组标题展示设备名
            groupBox1.Text = $"{deviceName}串口设置";

            // 加载并显示可用串口
            LoadAvailablePorts();

            // 尝试恢复上次选择
            if (!string.IsNullOrEmpty(SelectedPort) && cmbSettingsPort.Items.Contains(SelectedPort))
            {
                cmbSettingsPort.SelectedItem = SelectedPort;
            }
            else if (cmbSettingsPort.Items.Count > 0)
            {
                cmbSettingsPort.SelectedIndex = 0;
            }

            // 根据锁定状态更新 UI
            UpdateLockButtonState();
            UpdateMonitorButtonState();

            // 注册下拉展开时自动刷新串口列表
            cmbSettingsPort.DropDown += cmbSettingsPort_DropDown;
        }

        // 下拉展开时刷新串口列表并尝试恢复选择
        private void cmbSettingsPort_DropDown(object? sender, EventArgs e)
        {
            string? currentSelection = cmbSettingsPort.SelectedItem?.ToString();
            LoadAvailablePorts();

            // 恢复选择或默认选中第一个
            if (!string.IsNullOrEmpty(currentSelection) && cmbSettingsPort.Items.Contains(currentSelection))
            {
                cmbSettingsPort.SelectedItem = currentSelection;
            }
            else if (cmbSettingsPort.Items.Count > 0 && cmbSettingsPort.SelectedIndex == -1)
            {
                cmbSettingsPort.SelectedIndex = 0;
            }
        }

        // 获取系统可用串口并填充下拉列表
        private void LoadAvailablePorts()
        {
            cmbSettingsPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                cmbSettingsPort.Items.AddRange(ports);
            }
            else
            {
                // 当没有可用串口时显示占位项
                cmbSettingsPort.Items.Add("无可用串口");
            }
        }

        // 锁定按钮：切换锁定状态（并在 UI 中体现）
        private void btnLockPort_Click(object? sender, EventArgs e)
        {
            if (cmbSettingsPort.SelectedItem == null || cmbSettingsPort.SelectedItem.ToString() == "无可用串口")
            {
                MessageBox.Show("请选择一个串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 切换锁定状态并更新 UI
            IsPortLocked = !IsPortLocked;
            UpdateLockButtonState();
        }

        // 监视器按钮：通知父窗体切换监视器显示
        private void btnMonitor_Click(object? sender, EventArgs e)
        {
            ToggleMonitorRequested?.Invoke(this, EventArgs.Empty);
            _isMonitorOpen = !_isMonitorOpen;
            UpdateMonitorButtonState();
        }

        // 根据锁定状态设置按钮文本和下拉列表可用性
        private void UpdateLockButtonState()
        {
            if (IsPortLocked)
            {
                cmbSettingsPort.Enabled = false;
                btnLockPort.Text = "已锁定";
                btnLockPort.BackColor = Color.LightCoral;
            }
            else
            {
                cmbSettingsPort.Enabled = true;
                btnLockPort.Text = "未锁定";
                btnLockPort.BackColor = SystemColors.Control;
            }
        }

        // 更新监视器按钮文本以反映当前状态
        private void UpdateMonitorButtonState()
        {
            btnMonitor.Text = _isMonitorOpen ? "关闭打印" : "打印";
        }

        // 确认按钮：保存选择并关闭对话框
        private void btnOK_Click(object? sender, EventArgs e)
        {
            if (cmbSettingsPort.SelectedItem == null)
            {
                MessageBox.Show("请选择一个串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedPort = cmbSettingsPort.SelectedItem.ToString() ?? string.Empty;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // 取消按钮：不保存直接关闭
        private void btnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

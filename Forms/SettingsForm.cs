using System;
using System.Windows.Forms;
using TestTool.Infrastructure.Constants;
using TestTool.Core.Models;

namespace TestTool
{
    /// <summary>
    /// 旧版串口设置对话框（精简占位，仅保留字段和事件以通过编译）。
    /// </summary>
    public partial class SettingsForm : Form
    {
        public string SelectedPort { get; private set; } = string.Empty;
        public int SelectedBaudRate { get; private set; } = 115200;
        public bool IsPortLocked { get; private set; }
        private string deviceName;
        private bool _isMonitorOpen;

        public event EventHandler? ToggleMonitorRequested;
        public event EventHandler? SettingsConfirmed;

        public SettingsForm() : this(string.Empty, 115200, false, AppConstants.Defaults.DeviceName, false)
        {
        }

        public SettingsForm(string currentPort, int currentBaudRate, bool isLocked, string devName, bool isMonitorOpen)
        {
            SelectedPort = currentPort ?? string.Empty;
            SelectedBaudRate = currentBaudRate > 0 ? currentBaudRate : 115200;
            IsPortLocked = isLocked;
            deviceName = devName ?? string.Empty;
            _isMonitorOpen = isMonitorOpen;
        }

        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            // 占位实现
        }

        private void cmbSettingsPort_DropDown(object? sender, EventArgs e)
        {
            // 占位实现
        }

        // 以下方法为占位实现，保留事件触发与字段更新，避免 UI 控件依赖
        private bool TryUpdateSelections(bool showWarning = true)
        {
            // 无实际 UI，直接接受当前选择
            return true;
        }

        private void btnLockPort_Click(object? sender, EventArgs e)
        {
            if (!TryUpdateSelections()) return;
            IsPortLocked = !IsPortLocked;
            SettingsConfirmed?.Invoke(this, EventArgs.Empty);
        }

        private void btnMonitor_Click(object? sender, EventArgs e)
        {
            ToggleMonitorRequested?.Invoke(this, EventArgs.Empty);
            _isMonitorOpen = !_isMonitorOpen;
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            if (!TryUpdateSelections()) return;
            SettingsConfirmed?.Invoke(this, EventArgs.Empty);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

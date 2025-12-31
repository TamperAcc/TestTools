using System.Drawing;
using System.Windows.Forms;
using TestTool.Business.Enums;
using TestTool.Infrastructure.Constants;

namespace TestTool.Infrastructure.Helpers
{
    /// <summary>
    /// UI 辅助类：封装控件状态/颜色/字体的统一处理。
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// 跨线程安全调用控件更新（已释放或句柄无效时自动忽略）。
        /// </summary>
        public static void SafeInvoke(Control control, Action action)
        {
            if (control == null || control.IsDisposed || !control.IsHandleCreated)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                try
                {
                    control.BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
            }
            else
            {
                action();
            }
        }

        private static Font _boldFont = null!;
        private static Font _regularFont = null!;

        public static void InitializeFonts(Font baseFont)
        {
            _boldFont = new Font(baseFont, FontStyle.Bold);
            _regularFont = new Font(baseFont, FontStyle.Regular);
        }

        public static void DisposeFonts()
        {
            _boldFont?.Dispose();
            _regularFont?.Dispose();
        }

        public static void SetStatusLabel(Label label, ConnectionState state, string deviceName, string message)
        {
            label.Text = $"{deviceName} - 状态: {message}";
            label.ForeColor = UIConstants.TextColors.StatusLabel;
            label.Font = _boldFont;

            label.BackColor = state switch
            {
                ConnectionState.Connected => UIConstants.StatusColors.Connected,
                ConnectionState.Disconnected => UIConstants.StatusColors.Disconnected,
                ConnectionState.Connecting => UIConstants.StatusColors.Connecting,
                ConnectionState.Error => UIConstants.StatusColors.Error,
                _ => UIConstants.StatusColors.Disconnected
            };
        }

        public static void SetButtonDefault(Button button)
        {
            button.BackColor = UIConstants.ButtonColors.Default;
            button.ForeColor = SystemColors.ControlText;
            button.Font = _regularFont;
        }

        public static void SetButtonDisabled(Button button)
        {
            button.BackColor = UIConstants.ButtonColors.Disabled;
            button.ForeColor = SystemColors.ControlText;
            button.Font = _regularFont;
            button.Enabled = false;
        }

        public static void SetButtonActive(Button button, DevicePowerState powerState)
        {
            button.Enabled = true;
            button.Font = _boldFont;
            button.ForeColor = UIConstants.TextColors.ButtonActive;

            button.BackColor = powerState switch
            {
                DevicePowerState.On => UIConstants.ButtonColors.PowerOn,
                DevicePowerState.Off => UIConstants.ButtonColors.PowerOff,
                _ => UIConstants.ButtonColors.Default
            };
        }

        public static void UpdateConnectButton(Button button, bool isConnected)
        {
            if (isConnected)
            {
                button.Text = "断开连接";
                button.BackColor = UIConstants.ButtonColors.Connected;
            }
            else
            {
                button.Text = "连接";
                button.BackColor = SystemColors.Control;
            }
        }
    }
}

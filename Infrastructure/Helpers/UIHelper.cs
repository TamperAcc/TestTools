using System.Drawing;
using System.Windows.Forms;
using TestTool.Business.Enums;
using TestTool.Infrastructure.Constants;

namespace TestTool.Infrastructure.Helpers
{
    /// <summary>
    /// UI 辅助类：集中封装常用控件的样式与状态更新，减少重复代码
    /// </summary>
    public static class UIHelper
    {
        // 全局缓存的字体实例（在 InitializeFonts 中创建）
        private static Font _boldFont = null!;
        private static Font _regularFont = null!;

        /// <summary>
        /// 基于传入字体创建粗体/常规字体，需在应用启动时调用一次
        /// </summary>
        public static void InitializeFonts(Font baseFont)
        {
            _boldFont = new Font(baseFont, FontStyle.Bold);
            _regularFont = new Font(baseFont, FontStyle.Regular);
        }

        /// <summary>
        /// 释放初始化生成的字体资源，避免内存泄漏
        /// </summary>
        public static void DisposeFonts()
        {
            _boldFont?.Dispose();
            _regularFont?.Dispose();
        }

        /// <summary>
        /// 设置状态标签的文本、字体和背景色
        /// </summary>
        public static void SetStatusLabel(Label label, ConnectionState state, string deviceName, string message)
        {
            // 文本使用设备名与状态消息
            label.Text = $"{deviceName} - 状态: {message}";
            label.ForeColor = UIConstants.TextColors.StatusLabel;
            label.Font = _boldFont;

            // 根据连接状态选择背景色
            label.BackColor = state switch
            {
                ConnectionState.Connected => UIConstants.StatusColors.Connected,
                ConnectionState.Disconnected => UIConstants.StatusColors.Disconnected,
                ConnectionState.Connecting => UIConstants.StatusColors.Connecting,
                ConnectionState.Error => UIConstants.StatusColors.Error,
                _ => UIConstants.StatusColors.Disconnected
            };
        }

        /// <summary>
        /// 将按钮恢复为默认外观
        /// </summary>
        public static void SetButtonDefault(Button button)
        {
            button.BackColor = UIConstants.ButtonColors.Default;
            button.ForeColor = SystemColors.ControlText;
            button.Font = _regularFont;
        }

        /// <summary>
        /// 将按钮置为禁用状态并调整样式
        /// </summary>
        public static void SetButtonDisabled(Button button)
        {
            button.BackColor = UIConstants.ButtonColors.Disabled;
            button.ForeColor = SystemColors.ControlText;
            button.Font = _regularFont;
            button.Enabled = false;
        }

        /// <summary>
        /// 设置按钮为激活状态，颜色随电源状态变化
        /// </summary>
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

        /// <summary>
        /// 根据连接状态更新连接按钮的文本与颜色
        /// </summary>
        public static void UpdateConnectButton(Button button, bool isConnected)
        {
            if (isConnected)
            {
                button.Text = "已连接";
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

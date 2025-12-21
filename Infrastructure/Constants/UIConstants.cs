using System.Drawing;

namespace TestTool.Infrastructure.Constants
{
    /// <summary>
    /// UI 相关常量（颜色、消息等）
    /// </summary>
    public static class UIConstants
    {
        /// <summary>
        /// 状态颜色集合，用于表示不同连接状态的背景色
        /// </summary>
        public static class StatusColors
        {
            public static readonly Color Connected = Color.Green;
            public static readonly Color Disconnected = Color.DarkGray;
            public static readonly Color Warning = Color.Orange;
            public static readonly Color Error = Color.Red;
            public static readonly Color Connecting = Color.Orange;
        }

        /// <summary>
        /// 按钮颜色集合，用于统一按钮风格
        /// </summary>
        public static class ButtonColors
        {
            public static readonly Color Default = Color.LightSteelBlue;
            public static readonly Color Disabled = Color.LightGray;
            public static readonly Color PowerOn = Color.LimeGreen;
            public static readonly Color PowerOff = Color.Crimson;
            public static readonly Color Connected = Color.LightGreen;
        }

        /// <summary>
        /// 文本颜色集合，用于统一文本前景色的使用
        /// </summary>
        public static class TextColors
        {
            public static readonly Color StatusLabel = Color.White;
            public static readonly Color ButtonActive = Color.White;
        }

        /// <summary>
        /// 状态消息文本，用于在 UI 中显示连接描述
        /// </summary>
        public static class StatusMessages
        {
            public const string Connected = "已连接";
            public const string Disconnected = "未连接";
            public const string Connecting = "连接中...";
            public const string ConnectionFailed = "连接失败";
            public const string PleaseSelectPort = "请先选择串口";
            public const string SendFailed = "发送失败";
        }
    }
}

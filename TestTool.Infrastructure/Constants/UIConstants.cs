using System.Drawing;

namespace TestTool.Infrastructure.Constants
{
    /// <summary>
    /// UI 颜色与状态文本常量，统一按钮/标签风格。
    /// </summary>
    public static class UIConstants
    {
        public static class StatusColors
        {
            public static readonly Color Connected = Color.Green;
            public static readonly Color Disconnected = Color.DarkGray;
            public static readonly Color Warning = Color.Orange;
            public static readonly Color Error = Color.Red;
            public static readonly Color Connecting = Color.Orange;
        }

        public static class ButtonColors
        {
            public static readonly Color Default = Color.LightSteelBlue;
            public static readonly Color Disabled = Color.LightGray;
            public static readonly Color PowerOn = Color.LimeGreen;
            public static readonly Color PowerOff = Color.Crimson;
            public static readonly Color Connected = Color.LightGreen;
        }

        public static class TextColors
        {
            public static readonly Color StatusLabel = Color.White;
            public static readonly Color ButtonActive = Color.White;
        }

        public static class StatusMessages
        {
            public const string Connected = "已连接";
            public const string Disconnected = "未连接";
            public const string Connecting = "连接中...";
            public const string ConnectionFailed = "连接失败";
            public const string PleaseSelectPort = "请选择串口";
            public const string SendFailed = "发送失败";
        }
    }
}

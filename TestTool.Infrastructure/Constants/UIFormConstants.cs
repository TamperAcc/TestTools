namespace TestTool.Infrastructure.Constants
{
    /// <summary>
    /// 窗体尺寸与控件尺寸常量，供 UI 统一布局使用。
    /// </summary>
    public static class UIFormConstants
    {
        public static class MainForm
        {
            public const int DefaultWidth = 800;
            public const int DefaultHeight = 600;
            public const int MinWidth = 600;
            public const int MinHeight = 400;
        }

        public static class SettingsForm
        {
            public const int DefaultWidth = 520;
            public const int DefaultHeight = 420;
            public const int MinWidth = 520;
            public const int MinHeight = 420;
        }

        public static class MonitorForm
        {
            public const int DefaultWidth = 700;
            public const int DefaultHeight = 400;
            public const int MinWidth = 400;
            public const int MinHeight = 300;
        }

        public static class DevicePanel
        {
            public const int Width = 485;
            public const int Height = 60;
            public const int StartY = 10;
            public const int Spacing = 5;
        }

        public static class Buttons
        {
            public const int StandardWidth = 90;
            public const int StandardHeight = 30;
            public const int LargeWidth = 105;
            public const int SmallWidth = 70;
        }

        public static class Offsets
        {
            public const int PanelLeftMargin = 10;
            public const int ButtonRightMargin = 10;
            public const int VerticalSpacing = 5;
        }
    }
}

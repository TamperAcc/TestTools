using System;

namespace TestTool.Infrastructure.Constants
{
    /// <summary>
    /// 应用程序常量
    /// </summary>
    public static class AppConstants
    {
        public static class ConfigFiles
        {
            public const string DevicesConfig = "devices.json";
            public const string SerialPort = "serialport.config";
            public const string LockState = "portlock.config";
            public const string DeviceName = "devicename.config";
        }

        public static class Defaults
        {
            public const string DeviceName = "FCC1电源";
            public const int BaudRate = 115200;
            public const int DataBits = 8;
        }

        public static class Commands
        {
            public const string PowerOn = "ON";
            public const string PowerOff = "OFF";
        }

        public static class UI
        {
            public const int ResizeHandleSize = 15;
        }
    }
}

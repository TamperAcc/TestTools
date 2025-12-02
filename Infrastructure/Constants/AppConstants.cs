using System;

namespace WinFormsApp3.Infrastructure.Constants
{
    /// <summary>
    /// 应用程序常量
  /// </summary>
    public static class AppConstants
    {
    /// <summary>
   /// 配置文件名称
        /// </summary>
        public static class ConfigFiles
   {
            public const string SerialPort = "serialport.config";
            public const string LockState = "portlock.config";
      public const string DeviceName = "devicename.config";
        }

        /// <summary>
   /// 默认值
   /// </summary>
        public static class Defaults
    {
            public const string DeviceName = "FCC1电源";
        public const int BaudRate = 115200;
          public const int DataBits = 8;
        }

   /// <summary>
   /// 设备命令
  /// </summary>
      public static class Commands
        {
     public const string PowerOn = "ON";
            public const string PowerOff = "OFF";
 }

        /// <summary>
  /// UI设置
        /// </summary>
        public static class UI
        {
       public const int ResizeHandleSize = 10;
     }
    }
}

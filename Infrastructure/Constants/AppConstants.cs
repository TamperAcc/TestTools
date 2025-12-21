using System;

namespace TestTool.Infrastructure.Constants
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
            // 存储串口名的配置文件
            public const string SerialPort = "serialport.config";
            // 存储锁定状态的配置文件
            public const string LockState = "portlock.config";
            // 存储设备名称的配置文件
            public const string DeviceName = "devicename.config";
        }

        /// <summary>
   /// 默认值
   /// </summary>
        public static class Defaults
    {
            // 默认设备名
            public const string DeviceName = "FCC1电源";
            // 默认波特率
            public const int BaudRate = 115200;
          // 默认数据位
          public const int DataBits = 8;
        }

   /// <summary>
   /// 设备命令
  /// </summary>
      public static class Commands
        {
     // 开机命令
     public const string PowerOn = "ON";
            // 关机命令
            public const string PowerOff = "OFF";
 }

        /// <summary>
  /// UI设置
        /// </summary>
        public static class UI
        {
       // 窗口边角可拖拽区域大小（像素）
       public const int ResizeHandleSize = 10;
     }
    }
}

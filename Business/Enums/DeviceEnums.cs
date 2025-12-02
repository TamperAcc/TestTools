using System;

namespace WinFormsApp3.Business.Enums
{
    /// <summary>
 /// 连接状态枚举
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
     /// 断开连接
        /// </summary>
        Disconnected = 0,

   /// <summary>
  /// 连接中
        /// </summary>
        Connecting = 1,

        /// <summary>
/// 已连接
        /// </summary>
        Connected = 2,

  /// <summary>
    /// 断开连接中
  /// </summary>
        Disconnecting = 3,

        /// <summary>
   /// 错误状态
        /// </summary>
        Error = 4
    }

    /// <summary>
 /// 电源状态枚举
    /// </summary>
    public enum PowerState
    {
     /// <summary>
        /// 未知状态
/// </summary>
 Unknown = 0,

    /// <summary>
   /// 电源关闭
 /// </summary>
 Off = 1,

        /// <summary>
   /// 电源打开
        /// </summary>
        On = 2
    }

    /// <summary>
    /// 设备状态枚举
    /// </summary>
    public enum DeviceState
    {
        /// <summary>
   /// 离线
        /// </summary>
 Offline = 0,

        /// <summary>
/// 在线
   /// </summary>
   Online = 1,

    /// <summary>
  /// 就绪
    /// </summary>
   Ready = 2,

      /// <summary>
        /// 忙碌
        /// </summary>
  Busy = 3,

      /// <summary>
   /// 错误
      /// </summary>
   Error = 4
    }
}

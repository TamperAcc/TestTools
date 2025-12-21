using System;

namespace TestTool.Business.Enums
{
    /// <summary>
    /// 连接状态枚举，用于表示串口连接流程各阶段
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// 未连接状态
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// 正在连接
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected = 2,

        /// <summary>
        /// 正在断开
        /// </summary>
        Disconnecting = 3,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error = 4
    }

    /// <summary>
    /// 设备电源状态枚举
    /// </summary>
    public enum DevicePowerState
    {
        /// <summary>
        /// 未知或未初始化
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
    /// 设备高层状态（业务意义），表示设备整体工作情况
    /// </summary>
    public enum DeviceState
    {
        /// <summary>
        /// 设备离线
        /// </summary>
        Offline = 0,

        /// <summary>
        /// 设备在线
        /// </summary>
        Online = 1,

        /// <summary>
        /// 设备准备就绪
        /// </summary>
        Ready = 2,

        /// <summary>
        /// 设备忙
        /// </summary>
        Busy = 3,

        /// <summary>
        /// 设备错误
        /// </summary>
        Error = 4
    }
}

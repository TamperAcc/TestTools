using System;

namespace TestTool.Business.Enums
{
    /// <summary>
    /// 连接状态枚举，用于表示串口连接流程各阶段
    /// </summary>
    public enum ConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
        Error = 4
    }

    /// <summary>
    /// 设备电源状态枚举
    /// </summary>
    public enum DevicePowerState
    {
        Unknown = 0,
        Off = 1,
        On = 2
    }

    /// <summary>
    /// 设备高层状态（业务意义），表示设备整体工作情况
    /// </summary>
    public enum DeviceState
    {
        Offline = 0,
        Online = 1,
        Ready = 2,
        Busy = 3,
        Error = 4
    }

    /// <summary>
    /// 设备类型枚举：标识不同的电源设备
    /// </summary>
    public enum DeviceType
    {
        FCC1 = 0,
        FCC2 = 1,
        FCC3 = 2,
        HIL = 3
    }
}

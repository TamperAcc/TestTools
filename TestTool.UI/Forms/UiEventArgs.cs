using System;
using TestTool.Core.Enums;

namespace TestTool
{
    /// <summary>
    /// 监视器状态变化事件参数
    /// </summary>
    public class MonitorStateChangedEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }
        public bool IsOpen { get; }

        public MonitorStateChangedEventArgs(DeviceType deviceType, bool isOpen)
        {
            DeviceType = deviceType;
            IsOpen = isOpen;
        }
    }
}

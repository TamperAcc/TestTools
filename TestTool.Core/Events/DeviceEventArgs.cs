using System;
using TestTool.Core.Enums;

namespace TestTool.Core.Events
{
    public class DeviceEventArgs<T> : EventArgs
    {
        public DeviceType DeviceType { get; }
        public T Data { get; }

        public DeviceEventArgs(DeviceType deviceType, T data)
        {
            DeviceType = deviceType;
            Data = data;
        }
    }

    public class DeviceEventArgs : EventArgs
    {
        public DeviceType DeviceType { get; }

        public DeviceEventArgs(DeviceType deviceType)
        {
            DeviceType = deviceType;
        }
    }
}

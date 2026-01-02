using System;
using System.Threading;
using System.Threading.Tasks;
using TestTool.Core.Enums;
using TestTool.Core.Models;

namespace TestTool.Core.Services
{
    /// <summary>
    /// 串口连接状态变化事件参数（旧状态、新状态、提示消息）。
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState NewState { get; }
        public ConnectionState OldState { get; }
        public string Message { get; }

        public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState, string message = "")
        {
            OldState = oldState;
            NewState = newState;
            Message = message;
        }
    }

    /// <summary>
    /// 串口数据接收事件参数（原始数据与时间戳）。
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public string Data { get; }
        public DateTime ReceivedTime { get; }

        public DataReceivedEventArgs(string data)
        {
            Data = data;
            ReceivedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 串口数据发送事件参数（命令与时间戳）。
    /// </summary>
    public class DataSentEventArgs : EventArgs
    {
        public string Command { get; }
        public DateTime SentTime { get; }

        public DataSentEventArgs(string command)
        {
            Command = command;
            SentTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 串口服务接口，负责连接、断开、发送命令与事件通知。
    /// </summary>
    public interface ISerialPortService : IDisposable
    {
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<DataSentEventArgs> DataSent;

        Task<bool> ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken = default);
        bool IsConnected { get; }
        ConnectionConfig? CurrentConfig { get; }
        ConnectionState CurrentState { get; }
    }

    /// <summary>
    /// 设备状态变化事件参数（当前状态与旧电源状态）。
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceStatus Status { get; }
        public DevicePowerState OldPowerState { get; }

        public DeviceStatusChangedEventArgs(DeviceStatus status, DevicePowerState oldPowerState)
        {
            Status = status;
            OldPowerState = oldPowerState;
        }
    }

    /// <summary>
    /// 设备控制器接口，封装开关机与初始化能力。
    /// </summary>
    public interface IDeviceController : IDisposable
    {
        event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;
        Task<bool> TurnOnAsync();
        Task<bool> TurnOffAsync();
        Task<bool> InitializeAsync(ISerialPortService serialPortService);
        DeviceStatus CurrentStatus { get; }
        string DeviceName { get; set; }
    }
}

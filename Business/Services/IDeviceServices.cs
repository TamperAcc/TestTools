using System;
using System.Threading;
using System.Threading.Tasks;
using TestTool.Business.Enums;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 连接状态变化事件参数
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        // 新的连接状态
        public ConnectionState NewState { get; }
        // 旧的连接状态
        public ConnectionState OldState { get; }
        // 附带的状态消息（可用于显示错误或提示）
        public string Message { get; }

        public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState, string message = "")
        {
            OldState = oldState;
            NewState = newState;
            Message = message;
        }
    }

    /// <summary>
    /// 数据接收事件参数：封装接收到的数据文本及时间戳
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        // 接收到的字符串数据
        public string Data { get; }
        // 接收时间（本地时间）
        public DateTime ReceivedTime { get; }

        public DataReceivedEventArgs(string data)
        {
            Data = data;
            ReceivedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 数据发送事件参数：封装发送命令和时间戳
    /// </summary>
    public class DataSentEventArgs : EventArgs
    {
        // 发送的命令字符串
        public string Command { get; }
        // 发送时间（本地时间）
        public DateTime SentTime { get; }

        public DataSentEventArgs(string command)
        {
            Command = command;
            SentTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 串口服务接口：定义连接、断开、发送和事件回调契约
    /// 实现应负责线程安全与资源管理。
    /// </summary>
    public interface ISerialPortService : IDisposable
    {
        /// <summary>
        /// 连接状态变化事件：在连接、断开、错误等状态发生变化时触发
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// 数据接收事件：当串口收到数据时触发，携带接收的数据
        /// </summary>
        event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 数据发送事件：在写入串口后触发（用于记录和监视）
        /// </summary>
        event EventHandler<DataSentEventArgs> DataSent;

        /// <summary>
        /// 异步连接到指定串口配置，成功返回 true。
        /// </summary>
        Task<bool> ConnectAsync(ConnectionConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步断开当前串口连接
        /// </summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步发送命令到串口（成功返回 true）
        /// </summary>
        Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 当前连接配置（如果未连接则可能为 null）
        /// </summary>
        ConnectionConfig? CurrentConfig { get; }

        /// <summary>
        /// 当前连接状态
        /// </summary>
        ConnectionState CurrentState { get; }
    }

    /// <summary>
    /// 设备状态变化事件参数：封装当前设备状态和之前的电源状态
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
    /// 设备控制器接口：定义电源开/关及初始化等操作契约
    /// </summary>
    public interface IDeviceController : IDisposable
    {
        /// <summary>
        /// 设备状态变化事件：当内部状态（如电源）发生变化时触发
        /// </summary>
        event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// 打开电源（异步）
        /// </summary>
        Task<bool> TurnOnAsync();

        /// <summary>
        /// 关闭电源（异步）
        /// </summary>
        Task<bool> TurnOffAsync();

        /// <summary>
        /// 初始化设备控制器并注入串口服务实例
        /// </summary>
        Task<bool> InitializeAsync(ISerialPortService serialPortService);

        /// <summary>
        /// 当前设备状态
        /// </summary>
        DeviceStatus CurrentStatus { get; }

        /// <summary>
        /// 设备名称（用于UI显示）
        /// </summary>
        string DeviceName { get; set; }
    }
}

using System;
using System.Threading.Tasks;
using WinFormsApp3.Business.Enums;
using WinFormsApp3.Business.Models;

namespace WinFormsApp3.Business.Services
{
    /// <summary>
    /// 连接状态变化事件参数
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
 /// 数据接收事件参数
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
   /// 串口服务接口
    /// </summary>
 public interface ISerialPortService : IDisposable
    {
        /// <summary>
/// 连接状态变化事件
  /// </summary>
     event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

  /// <summary>
   /// 数据接收事件
 /// </summary>
      event EventHandler<DataReceivedEventArgs> DataReceived;

/// <summary>
      /// 异步连接串口
/// </summary>
        Task<bool> ConnectAsync(ConnectionConfig config);

/// <summary>
   /// 异步断开连接
      /// </summary>
        Task DisconnectAsync();

        /// <summary>
  /// 异步发送命令
   /// </summary>
      Task<bool> SendCommandAsync(string command);

/// <summary>
        /// 是否已连接
   /// </summary>
 bool IsConnected { get; }

  /// <summary>
  /// 当前配置
        /// </summary>
        ConnectionConfig CurrentConfig { get; }

        /// <summary>
 /// 当前连接状态
   /// </summary>
 ConnectionState CurrentState { get; }
    }

/// <summary>
    /// 设备状态变化事件参数
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
  /// 设备控制器接口
 /// </summary>
 public interface IDeviceController : IDisposable
  {
   /// <summary>
/// 设备状态变化事件
        /// </summary>
    event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

      /// <summary>
  /// 打开电源
/// </summary>
    Task<bool> TurnOnAsync();

        /// <summary>
    /// 关闭电源
/// </summary>
 Task<bool> TurnOffAsync();

/// <summary>
     /// 初始化设备
      /// </summary>
     Task<bool> InitializeAsync(ISerialPortService serialPortService);

 /// <summary>
   /// 当前设备状态
   /// </summary>
   DeviceStatus CurrentStatus { get; }

/// <summary>
        /// 设备名称
     /// </summary>
   string DeviceName { get; set; }
 }
}

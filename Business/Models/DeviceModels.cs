using System;
using System.IO.Ports;
using System.Text;
using WinFormsApp3.Business.Enums;

namespace WinFormsApp3.Business.Models
{
    /// <summary>
    /// 连接配置模型
    /// </summary>
  public class ConnectionConfig
  {
        public string PortName { get; set; }
  public int BaudRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public int ReadTimeout { get; set; } = 500;
    public int WriteTimeout { get; set; } = 500;

    public ConnectionConfig() { }

        public ConnectionConfig(string portName)
        {
  PortName = portName;
}

 /// <summary>
   /// 验证配置是否有效
        /// </summary>
      public bool IsValid()
{
  return !string.IsNullOrEmpty(PortName) && 
         BaudRate > 0 && 
          DataBits > 0;
     }
    }

    /// <summary>
   /// 设备状态模型
    /// </summary>
    public class DeviceStatus
    {
        public string DeviceName { get; set; }
  public ConnectionState ConnectionState { get; set; }
      public DevicePowerState PowerState { get; set; }
    public DateTime LastUpdateTime { get; set; }
      public string StatusMessage { get; set; }

     public DeviceStatus()
{
   LastUpdateTime = DateTime.Now;
      ConnectionState = ConnectionState.Disconnected;
      PowerState = DevicePowerState.Unknown;
  }

 /// <summary>
        /// 是否已连接
        /// </summary>
   public bool IsConnected => ConnectionState == ConnectionState.Connected;

      /// <summary>
        /// 是否可以发送命令
        /// </summary>
      public bool CanSendCommand => IsConnected && ConnectionState != ConnectionState.Error;
    }

    /// <summary>
  /// 应用配置模型
  /// </summary>
  public class AppConfig
    {
   public string SelectedPort { get; set; }
      public bool IsPortLocked { get; set; }
        public string DeviceName { get; set; }
      public ConnectionConfig ConnectionSettings { get; set; }

      public AppConfig()
        {
            DeviceName = "FCC1电源";
     ConnectionSettings = new ConnectionConfig();
  }
}
}

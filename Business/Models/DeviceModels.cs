using System;
using System.IO.Ports;
using System.Text;
using System.ComponentModel.DataAnnotations;
using TestTool.Business.Enums;
using TestTool.Infrastructure.Constants;

namespace TestTool.Business.Models
{
    /// <summary>
    /// 串口连接配置模型，封装端口名、波特率、校验位等参数
    /// </summary>
    public class ConnectionConfig
    {
        // 串口名称，如 COM1
        [Required(ErrorMessage = "PortName is required")]
        public string PortName { get; set; } = string.Empty;
        // 默认波特率
        [Range(110, 1152000, ErrorMessage = "BaudRate must be between 110 and 1152000")]
        public int BaudRate { get; set; } = 115200;
        // 校验位类型
        public Parity Parity { get; set; } = Parity.None;
        // 数据位
        [Range(5, 8, ErrorMessage = "DataBits must be between 5 and 8")]
        public int DataBits { get; set; } = 8;
        // 停止位
        public StopBits StopBits { get; set; } = StopBits.One;
        // 文本编码
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        // 读取超时（毫秒）
        [Range(100, 60000, ErrorMessage = "ReadTimeout must be between 100ms and 60s")]
        public int ReadTimeout { get; set; } = 2000;
        // 写入超时（毫秒）
        [Range(100, 60000, ErrorMessage = "WriteTimeout must be between 100ms and 60s")]
        public int WriteTimeout { get; set; } = 2000;

        public ConnectionConfig NormalizeWithDefaults()
        {
            if (BaudRate <= 0) BaudRate = 115200;
            if (DataBits <= 0) DataBits = 8;
            if (!Enum.IsDefined(typeof(Parity), Parity)) Parity = Parity.None;
            if (!Enum.IsDefined(typeof(StopBits), StopBits)) StopBits = StopBits.One;
            Encoding ??= Encoding.UTF8;
            if (ReadTimeout <= 0) ReadTimeout = 2000;
            if (WriteTimeout <= 0) WriteTimeout = 2000;
            return this;
        }

        // 无参构造函数用于序列化或手动构造场景
        public ConnectionConfig() { }

        // 根据端口名构造
        public ConnectionConfig(string portName)
        {
            PortName = portName;
        }

        /// <summary>
        /// 校验配置是否合法（简单校验：端口名与数值参数）
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(PortName) &&
                   BaudRate > 0 &&
                   DataBits > 0;
        }
    }

    /// <summary>
    /// 设备状态模型：包含设备名称、连接状态、电源状态、最后更新时间与状态消息
    /// </summary>
    public class DeviceStatus
    {
        public string DeviceName { get; set; } = string.Empty;
        public ConnectionState ConnectionState { get; set; }
        public DevicePowerState PowerState { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string StatusMessage { get; set; } = string.Empty;

        public DeviceStatus()
        {
            // 初始化默认时间与状态
            LastUpdateTime = DateTime.Now;
            ConnectionState = ConnectionState.Disconnected;
            PowerState = DevicePowerState.Unknown;
        }

        /// <summary>
        /// 是否处于已连接状态
        /// </summary>
        public bool IsConnected => ConnectionState == ConnectionState.Connected;

        /// <summary>
        /// 是否可以发送命令：需要已连接且非错误状态
        /// </summary>
        public bool CanSendCommand => IsConnected && ConnectionState != ConnectionState.Error;
    }

    /// <summary>
    /// 应用配置模型：用于在磁盘持久化的基本配置
    /// </summary>
    public class AppConfig
    {
        // 用户选中的串口
        public string SelectedPort { get; set; } = string.Empty;
        // 串口是否被界面锁定，禁止修改
        public bool IsPortLocked { get; set; }
        // 设备名称
        public string DeviceName { get; set; }
        // 串口连接的默认设置
        public ConnectionConfig ConnectionSettings { get; set; }
        public RetryPolicyConfig? RetryPolicy { get; set; }

        public AppConfig()
        {
            // 默认设备名称
            DeviceName = "FCC1电源";
            // 初始化连接配置为默认值
            ConnectionSettings = new ConnectionConfig();
            RetryPolicy = new RetryPolicyConfig();
        }
    }

    public class RetryPolicyConfig
    {
        [Range(0, 10, ErrorMessage = "ConnectRetries must be between 0 and 10")]
        public int ConnectRetries { get; set; } = 3;
        [Range(0, 10, ErrorMessage = "SendRetries must be between 0 and 10")]
        public int SendRetries { get; set; } = 2;
        [Range(50, 5000, ErrorMessage = "BaseDelayMs must be between 50ms and 5000ms")]
        public int BaseDelayMs { get; set; } = 200;
    }
}

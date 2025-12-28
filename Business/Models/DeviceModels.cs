using System;
using System.IO.Ports;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
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
    /// 单个设备的配置信息
    /// </summary>
    public class DeviceConfig
    {
        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// 设备显示名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 选择的串口
        /// </summary>
        public string SelectedPort { get; set; } = string.Empty;

        /// <summary>
        /// 端口是否锁定
        /// </summary>
        public bool IsPortLocked { get; set; }

        /// <summary>
        /// 连接配置
        /// </summary>
        public ConnectionConfig ConnectionSettings { get; set; } = new();

        /// <summary>
        /// 开启命令（默认 "ON"）
        /// </summary>
        public string OnCommand { get; set; } = "ON";

        /// <summary>
        /// 关闭命令（默认 "OFF"）
        /// </summary>
        public string OffCommand { get; set; } = "OFF";

        public DeviceConfig() { }

        public DeviceConfig(DeviceType type, string name)
        {
            DeviceType = type;
            DeviceName = name;
        }
    }

    /// <summary>
    /// 应用配置模型：保存于磁盘持久化的基本配置
    /// </summary>
    public class AppConfig
    {
        // 保留旧属性以兼容单设备场景（映射到 FCC1）
        public string SelectedPort
        {
            get => GetDeviceConfig(DeviceType.FCC1).SelectedPort;
            set => GetDeviceConfig(DeviceType.FCC1).SelectedPort = value;
        }

        public bool IsPortLocked
        {
            get => GetDeviceConfig(DeviceType.FCC1).IsPortLocked;
            set => GetDeviceConfig(DeviceType.FCC1).IsPortLocked = value;
        }

        public string DeviceName
        {
            get => GetDeviceConfig(DeviceType.FCC1).DeviceName;
            set => GetDeviceConfig(DeviceType.FCC1).DeviceName = value;
        }

        public ConnectionConfig ConnectionSettings
        {
            get => GetDeviceConfig(DeviceType.FCC1).ConnectionSettings;
            set => GetDeviceConfig(DeviceType.FCC1).ConnectionSettings = value;
        }

        /// <summary>
        /// 多设备配置字典
        /// </summary>
        public Dictionary<DeviceType, DeviceConfig> Devices { get; set; } = new();

        public RetryPolicyConfig? RetryPolicy { get; set; }

        public AppConfig()
        {
            RetryPolicy = new RetryPolicyConfig();
            // 初始化 4 个设备的默认配置
            InitializeDefaultDevices();
        }

        private void InitializeDefaultDevices()
        {
            if (!Devices.ContainsKey(DeviceType.FCC1))
                Devices[DeviceType.FCC1] = new DeviceConfig(DeviceType.FCC1, "FCC1电源");
            if (!Devices.ContainsKey(DeviceType.FCC2))
                Devices[DeviceType.FCC2] = new DeviceConfig(DeviceType.FCC2, "FCC2电源");
            if (!Devices.ContainsKey(DeviceType.FCC3))
                Devices[DeviceType.FCC3] = new DeviceConfig(DeviceType.FCC3, "FCC3电源");
            if (!Devices.ContainsKey(DeviceType.HIL))
                Devices[DeviceType.HIL] = new DeviceConfig(DeviceType.HIL, "HIL电源");
        }

        /// <summary>
        /// 获取指定设备类型的配置（若不存在则创建默认）
        /// </summary>
        public DeviceConfig GetDeviceConfig(DeviceType type)
        {
            if (!Devices.ContainsKey(type))
            {
                Devices[type] = new DeviceConfig(type, GetDefaultDeviceName(type));
            }
            return Devices[type];
        }

        private static string GetDefaultDeviceName(DeviceType type) => type switch
        {
            DeviceType.FCC1 => "FCC1电源",
            DeviceType.FCC2 => "FCC2电源",
            DeviceType.FCC3 => "FCC3电源",
            DeviceType.HIL => "HIL电源",
            _ => "未知设备"
        };
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

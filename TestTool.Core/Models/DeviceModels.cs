using System;
using System.IO.Ports;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using TestTool.Business.Enums;

namespace TestTool.Business.Models
{
    public class ConnectionConfig
    {
        [Required(ErrorMessage = "PortName is required")]
        public string PortName { get; set; } = string.Empty;
        [Range(110, 1152000, ErrorMessage = "BaudRate must be between 110 and 1152000")]
        public int BaudRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.None;
        [Range(5, 8, ErrorMessage = "DataBits must be between 5 and 8")]
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        [Range(100, 60000, ErrorMessage = "ReadTimeout must be between 100ms and 60s")]
        public int ReadTimeout { get; set; } = 2000;
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

        public ConnectionConfig() { }
        public ConnectionConfig(string portName)
        {
            PortName = portName;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(PortName) &&
                   BaudRate > 0 &&
                   DataBits > 0;
        }
    }

    public class DeviceStatus
    {
        public string DeviceName { get; set; } = string.Empty;
        public ConnectionState ConnectionState { get; set; }
        public DevicePowerState PowerState { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string StatusMessage { get; set; } = string.Empty;

        public DeviceStatus()
        {
            LastUpdateTime = DateTime.Now;
            ConnectionState = ConnectionState.Disconnected;
            PowerState = DevicePowerState.Unknown;
        }

        public bool IsConnected => ConnectionState == ConnectionState.Connected;
        public bool CanSendCommand => IsConnected && ConnectionState != ConnectionState.Error;
    }

    public class DeviceConfig
    {
        public DeviceType DeviceType { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string SelectedPort { get; set; } = string.Empty;
        public bool IsPortLocked { get; set; }
        public ConnectionConfig ConnectionSettings { get; set; } = new();
        public string OnCommand { get; set; } = "ON";
        public string OffCommand { get; set; } = "OFF";
        public MonitorWindowPosition? MonitorPosition { get; set; }

        public DeviceConfig() { }

        public DeviceConfig(DeviceType type, string name)
        {
            DeviceType = type;
            DeviceName = name;
        }
    }

    public class MonitorWindowPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsValid => Width > 0 && Height > 0;

        public MonitorWindowPosition() { }

        public MonitorWindowPosition(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public class AppConfig
    {
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

        public Dictionary<DeviceType, DeviceConfig> Devices { get; set; } = new();

        public RetryPolicyConfig? RetryPolicy { get; set; }

        public MonitorWindowPosition? MainWindowPosition { get; set; }
        public MonitorWindowPosition? SettingsWindowPosition { get; set; }

        public AppConfig()
        {
            RetryPolicy = new RetryPolicyConfig();
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

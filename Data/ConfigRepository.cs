using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestTool.Business.Enums;
using TestTool.Business.Models;
using TestTool.Infrastructure.Constants;

namespace TestTool.Data
{
    /// <summary>
    /// 配置仓库接口：定义配置加载与保存操作
    /// </summary>
    public interface IConfigRepository
    {
        Task<AppConfig> LoadAsync();
        Task SaveAsync(AppConfig config);
        Task<T> GetValueAsync<T>(string key, T defaultValue);
        Task SetValueAsync<T>(string key, T value);
    }

    /// <summary>
    /// 设备配置 JSON 模型（用于序列化）
    /// </summary>
    public class DeviceConfigJson
    {
        [JsonPropertyName("port")]
        public string Port { get; set; } = string.Empty;
        
        [JsonPropertyName("baudRate")]
        public int BaudRate { get; set; } = 115200;
        
        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }
        
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;
        
        [JsonPropertyName("onCommand")]
        public string OnCommand { get; set; } = "ON";
        
        [JsonPropertyName("offCommand")]
        public string OffCommand { get; set; } = "OFF";

        [JsonPropertyName("monitorPosition")]
        public MonitorPositionJson? MonitorPosition { get; set; }
    }

    /// <summary>
    /// 打印窗口位置 JSON 模型（用于序列化）
    /// </summary>
    public class MonitorPositionJson
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    /// <summary>
    /// 多设备配置 JSON 模型（用于序列化）
    /// </summary>
    public class DevicesConfigJson
    {
        [JsonPropertyName("device1")]
        public DeviceConfigJson FCC1 { get; set; } = new();
        
        [JsonPropertyName("device2")]
        public DeviceConfigJson FCC2 { get; set; } = new();
        
        [JsonPropertyName("device3")]
        public DeviceConfigJson FCC3 { get; set; } = new();
        
        [JsonPropertyName("device4")]
        public DeviceConfigJson HIL { get; set; } = new();

        /// <summary>
        /// 主窗口位置配置
        /// </summary>
        [JsonPropertyName("mainWindowPosition")]
        public MonitorPositionJson? MainWindowPosition { get; set; }

        /// <summary>
        /// 设置窗口位置配置
        /// </summary>
        [JsonPropertyName("settingsWindowPosition")]
        public MonitorPositionJson? SettingsWindowPosition { get; set; }
    }

    /// <summary>
    /// 基于文件的配置仓库实现：将配置读写到 Config 目录下的文件中
    /// 支持多设备 JSON 格式配置
    /// </summary>
    public class FileConfigRepository : IConfigRepository
    {
        private const string CONFIG_DIR = "Config";
        private AppConfig? _cachedConfig;
        private readonly ILogger<FileConfigRepository> _logger;
        private readonly IOptionsMonitor<AppConfig>? _optionsMonitor;
        
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly object _saveLock = new();
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private const int SaveDebounceMs = 500;

        public FileConfigRepository(ILogger<FileConfigRepository> logger, IOptionsMonitor<AppConfig>? optionsMonitor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionsMonitor = optionsMonitor;

            try
            {
                Directory.CreateDirectory(CONFIG_DIR);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create config directory {Dir}", CONFIG_DIR);
            }
        }

        /// <summary>
        /// 异步加载应用配置：优先从 JSON 文件读取，支持旧格式迁移
        /// </summary>
        public async Task<AppConfig> LoadAsync()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            _cachedConfig = new AppConfig();
            
            // 尝试从新的 JSON 格式加载
            var jsonPath = Path.Combine(CONFIG_DIR, AppConstants.ConfigFiles.DevicesConfig);
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(jsonPath);
                    var devicesJson = JsonSerializer.Deserialize<DevicesConfigJson>(json, _jsonOptions);
                    if (devicesJson != null)
                    {
                        ApplyJsonToConfig(_cachedConfig, devicesJson);
                        _logger.LogInformation("Loaded devices config from JSON");
                        return _cachedConfig;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load JSON config, falling back to defaults");
                }
            }

            // 如果没有 JSON 文件，尝试从旧格式迁移（仅 FCC1）
            await MigrateFromOldFormat(_cachedConfig);
            
            return _cachedConfig;
        }

        /// <summary>
        /// 从旧格式配置文件迁移数据（兼容性）
        /// </summary>
        private async Task MigrateFromOldFormat(AppConfig config)
        {
            var oldPort = await LoadFileAsync(AppConstants.ConfigFiles.SerialPort, "");
            var oldLock = await LoadFileAsync(AppConstants.ConfigFiles.LockState, "false") == "true";
            var oldName = await LoadFileAsync(AppConstants.ConfigFiles.DeviceName, "FCC1电源");

            if (!string.IsNullOrEmpty(oldPort))
            {
                var fcc1 = config.GetDeviceConfig(DeviceType.FCC1);
                fcc1.SelectedPort = oldPort;
                fcc1.IsPortLocked = oldLock;
                fcc1.DeviceName = oldName;
                
                _logger.LogInformation("Migrated old config format to new JSON format");
                
                // 保存为新格式
                await SaveAsync(config);
            }
        }

        /// <summary>
        /// 将 JSON 配置应用到 AppConfig
        /// </summary>
        private void ApplyJsonToConfig(AppConfig config, DevicesConfigJson json)
        {
            ApplyDeviceJson(config.GetDeviceConfig(DeviceType.FCC1), json.FCC1, "FCC1电源");
            ApplyDeviceJson(config.GetDeviceConfig(DeviceType.FCC2), json.FCC2, "FCC2电源");
            ApplyDeviceJson(config.GetDeviceConfig(DeviceType.FCC3), json.FCC3, "FCC3电源");
            ApplyDeviceJson(config.GetDeviceConfig(DeviceType.HIL), json.HIL, "HIL电源");

            // 读取主窗口位置配置
            if (json.MainWindowPosition != null && json.MainWindowPosition.Width > 0 && json.MainWindowPosition.Height > 0)
            {
                config.MainWindowPosition = new MonitorWindowPosition(
                    json.MainWindowPosition.X,
                    json.MainWindowPosition.Y,
                    json.MainWindowPosition.Width,
                    json.MainWindowPosition.Height);
            }

            // 读取设置窗口位置配置
            if (json.SettingsWindowPosition != null && json.SettingsWindowPosition.Width > 0 && json.SettingsWindowPosition.Height > 0)
            {
                config.SettingsWindowPosition = new MonitorWindowPosition(
                    json.SettingsWindowPosition.X,
                    json.SettingsWindowPosition.Y,
                    json.SettingsWindowPosition.Width,
                    json.SettingsWindowPosition.Height);
            }
        }

        private void ApplyDeviceJson(DeviceConfig device, DeviceConfigJson json, string defaultName)
        {
            device.SelectedPort = json.Port ?? string.Empty;
            device.IsPortLocked = json.IsLocked;
            device.DeviceName = string.IsNullOrEmpty(json.DeviceName) ? defaultName : json.DeviceName;
            device.ConnectionSettings ??= new ConnectionConfig();
            device.ConnectionSettings.BaudRate = json.BaudRate > 0 ? json.BaudRate : 115200;
            device.OnCommand = string.IsNullOrEmpty(json.OnCommand) ? "ON" : json.OnCommand;
            device.OffCommand = string.IsNullOrEmpty(json.OffCommand) ? "OFF" : json.OffCommand;
            
            // 如果配置的串口不存在，回退为空并解除锁定，避免占用无效端口
            if (!string.IsNullOrEmpty(device.SelectedPort))
            {
                var exists = SerialPort.GetPortNames()
                    .Any(p => string.Equals(p, device.SelectedPort, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    _logger.LogInformation("Configured port {Port} not found, fallback to empty for device {Device}", device.SelectedPort, device.DeviceName);
                    device.SelectedPort = string.Empty;
                    device.IsPortLocked = false;
                }
            }
                
             // 读取窗口位置配置
             if (json.MonitorPosition != null && json.MonitorPosition.Width > 0 && json.MonitorPosition.Height > 0)
             {
                 device.MonitorPosition = new MonitorWindowPosition(
                     json.MonitorPosition.X,
                     json.MonitorPosition.Y,
                     json.MonitorPosition.Width,
                     json.MonitorPosition.Height);
             }
         }

        /// <summary>
        /// 异步保存应用配置到 JSON 文件
        /// </summary>
        public async Task SaveAsync(AppConfig config)
        {
            _cachedConfig = config;

            // 简单去抖：500ms 内重复保存则跳过
            lock (_saveLock)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastSaveUtc).TotalMilliseconds < SaveDebounceMs)
                {
                    _logger.LogInformation("SaveAsync skipped by debounce window {Window}ms", SaveDebounceMs);
                    return;
                }
                _lastSaveUtc = now;
            }

            var devicesJson = new DevicesConfigJson
            {
                FCC1 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC1)),
                FCC2 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC2)),
                FCC3 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC3)),
                HIL = CreateDeviceJson(config.GetDeviceConfig(DeviceType.HIL))
            };

            // 保存主窗口位置配置
            if (config.MainWindowPosition != null && config.MainWindowPosition.IsValid)
            {
                devicesJson.MainWindowPosition = new MonitorPositionJson
                {
                    X = config.MainWindowPosition.X,
                    Y = config.MainWindowPosition.Y,
                    Width = config.MainWindowPosition.Width,
                    Height = config.MainWindowPosition.Height
                };
            }

            // 保存设置窗口位置配置
            if (config.SettingsWindowPosition != null && config.SettingsWindowPosition.IsValid)
            {
                devicesJson.SettingsWindowPosition = new MonitorPositionJson
                {
                    X = config.SettingsWindowPosition.X,
                    Y = config.SettingsWindowPosition.Y,
                    Width = config.SettingsWindowPosition.Width,
                    Height = config.SettingsWindowPosition.Height
                };
            }

            try
            {
                var json = JsonSerializer.Serialize(devicesJson, _jsonOptions);
                var jsonPath = Path.Combine(CONFIG_DIR, AppConstants.ConfigFiles.DevicesConfig);
                await File.WriteAllTextAsync(jsonPath, json);
                _logger.LogInformation("Saved devices config to JSON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save devices config to JSON");
            }
        }

        private DeviceConfigJson CreateDeviceJson(DeviceConfig device)
        {
            var json = new DeviceConfigJson
            {
                Port = device.SelectedPort ?? string.Empty,
                BaudRate = device.ConnectionSettings?.BaudRate ?? 115200,
                IsLocked = device.IsPortLocked,
                DeviceName = device.DeviceName ?? string.Empty,
                OnCommand = device.OnCommand ?? "ON",
                OffCommand = device.OffCommand ?? "OFF"
            };

            // 保存窗口位置配置
            if (device.MonitorPosition != null && device.MonitorPosition.IsValid)
            {
                json.MonitorPosition = new MonitorPositionJson
                {
                    X = device.MonitorPosition.X,
                    Y = device.MonitorPosition.Y,
                    Width = device.MonitorPosition.Width,
                    Height = device.MonitorPosition.Height
                };
            }

            return json;
        }

        public async Task<T> GetValueAsync<T>(string key, T defaultValue)
        {
            try
            {
                var value = await LoadFileAsync(key, defaultValue?.ToString() ?? "");
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetValueAsync failed for key {Key}", key);
                return defaultValue;
            }
        }

        public async Task SetValueAsync<T>(string key, T value)
        {
            await SaveFileAsync(key, value?.ToString() ?? "");
        }

        private async Task<string> LoadFileAsync(string fileName, string defaultValue)
        {
            try
            {
                var filePath = Path.Combine(CONFIG_DIR, fileName);
                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    return string.IsNullOrWhiteSpace(content) ? defaultValue : content.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading config file {File}", fileName);
            }

            return defaultValue;
        }

        private async Task SaveFileAsync(string fileName, string value)
        {
            try
            {
                var filePath = Path.Combine(CONFIG_DIR, fileName);
                await File.WriteAllTextAsync(filePath, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing config file {File}", fileName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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
        }

        /// <summary>
        /// 异步保存应用配置到 JSON 文件
        /// </summary>
        public async Task SaveAsync(AppConfig config)
        {
            _cachedConfig = config;

            var devicesJson = new DevicesConfigJson
            {
                FCC1 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC1)),
                FCC2 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC2)),
                FCC3 = CreateDeviceJson(config.GetDeviceConfig(DeviceType.FCC3)),
                HIL = CreateDeviceJson(config.GetDeviceConfig(DeviceType.HIL))
            };

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
            return new DeviceConfigJson
            {
                Port = device.SelectedPort ?? string.Empty,
                BaudRate = device.ConnectionSettings?.BaudRate ?? 115200,
                IsLocked = device.IsPortLocked,
                DeviceName = device.DeviceName ?? string.Empty,
                OnCommand = device.OnCommand ?? "ON",
                OffCommand = device.OffCommand ?? "OFF"
            };
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

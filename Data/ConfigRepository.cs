using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// 基于文件的配置仓库实现：将配置项写入到 Config 目录下的文件中
    /// 现在支持从 IOptionsMonitor&lt;AppConfig&gt; 获取初始配置
    /// </summary>
    public class FileConfigRepository : IConfigRepository
    {
        // 配置文件目录常量
        private const string CONFIG_DIR = "Config";
        // 缓存的应用配置（可空，延迟加载）
        private AppConfig? _cachedConfig;
        private readonly ILogger<FileConfigRepository> _logger;
        private readonly IOptionsMonitor<AppConfig>? _optionsMonitor;

        // 构造函数：确保配置目录存在，并注入 logger 与可选的 IOptionsMonitor
        public FileConfigRepository(ILogger<FileConfigRepository> logger, IOptionsMonitor<AppConfig>? optionsMonitor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionsMonitor = optionsMonitor;

            // 尝试创建配置目录
            try
            {
                Directory.CreateDirectory(CONFIG_DIR);
            }
            catch (Exception ex)
            {
                // 记录但不抛出
                _logger.LogWarning(ex, "Failed to create config directory {Dir}", CONFIG_DIR);
            }
        }

        /// <summary>
        /// 异步加载应用配置，如果已缓存则直接返回缓存
        /// 否则优先从磁盘文件读取，若不存在则从 IOptionsMonitor 或默认值构造
        /// </summary>
        public async Task<AppConfig> LoadAsync()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            // 如果有 appsettings 配置则使用它作为默认
            var optionsConfig = _optionsMonitor?.CurrentValue;

            var settings = optionsConfig?.ConnectionSettings ?? new ConnectionConfig
            {
                BaudRate = AppConstants.Defaults.BaudRate,
                DataBits = AppConstants.Defaults.DataBits
            };

            NormalizeConnection(settings);

            _cachedConfig = new AppConfig
            {
                SelectedPort = optionsConfig?.SelectedPort ?? await LoadFileAsync(AppConstants.ConfigFiles.SerialPort, ""),
                IsPortLocked = optionsConfig?.IsPortLocked ?? (await LoadFileAsync(AppConstants.ConfigFiles.LockState, "false") == "true"),
                DeviceName = optionsConfig?.DeviceName ?? await LoadFileAsync(AppConstants.ConfigFiles.DeviceName, AppConstants.Defaults.DeviceName),
                ConnectionSettings = settings
            };

            return _cachedConfig;
        }

        private void NormalizeConnection(ConnectionConfig config)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Parity), config.Parity))
                {
                    _logger.LogWarning("Invalid Parity value {Parity}, falling back to None", config.Parity);
                    config.Parity = Parity.None;
                }

                if (!Enum.IsDefined(typeof(StopBits), config.StopBits))
                {
                    _logger.LogWarning("Invalid StopBits value {StopBits}, falling back to One", config.StopBits);
                    config.StopBits = StopBits.One;
                }

                if (config.Encoding == null)
                {
                    config.Encoding = Encoding.UTF8;
                }

                config.NormalizeWithDefaults();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error normalizing connection settings, applying defaults");
                config.Parity = Parity.None;
                config.StopBits = StopBits.One;
                config.Encoding = Encoding.UTF8;
                config.BaudRate = AppConstants.Defaults.BaudRate;
                config.DataBits = AppConstants.Defaults.DataBits;
                config.ReadTimeout = 2000;
                config.WriteTimeout = 2000;
            }
        }

        /// <summary>
        /// 异步保存应用配置到文件，并更新缓存
        /// </summary>
        public async Task SaveAsync(AppConfig config)
        {
            // 更新缓存
            _cachedConfig = config;

            // 持久化各项到对应的配置文件
            await SaveFileAsync(AppConstants.ConfigFiles.SerialPort, config.SelectedPort ?? "");
            await SaveFileAsync(AppConstants.ConfigFiles.LockState, config.IsPortLocked.ToString().ToLower());
            await SaveFileAsync(AppConstants.ConfigFiles.DeviceName, config.DeviceName ?? AppConstants.Defaults.DeviceName);
        }

        /// <summary>
        /// 以键值方式读取配置项并转换为指定类型，读取失败时返回默认值
        /// </summary>
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
                // 在转换失败时返回提供的默认值
                return defaultValue;
            }
        }

        /// <summary>
        /// 以键值方式保存配置项（将值转为字符串写入文件）
        /// </summary>
        public async Task SetValueAsync<T>(string key, T value)
        {
            await SaveFileAsync(key, value?.ToString() ?? "");
        }

        // 私有工具：从文件读取字符串值，读取失败返回默认值
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
                // 忽略读取错误并返回默认值
            }

            return defaultValue;
        }

        // 私有工具：将字符串值写入文件，写入失败时忽略错误
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
                // 忽略写入错误
            }
        }
    }
}

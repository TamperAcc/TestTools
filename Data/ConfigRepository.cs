using System;
using System.IO;
using System.Threading.Tasks;
using WinFormsApp3.Business.Models;
using WinFormsApp3.Infrastructure.Constants;

namespace WinFormsApp3.Data
{
 /// <summary>
/// 配置仓储接口
    /// </summary>
    public interface IConfigRepository
    {
        Task<AppConfig> LoadAsync();
        Task SaveAsync(AppConfig config);
    Task<T> GetValueAsync<T>(string key, T defaultValue);
        Task SetValueAsync<T>(string key, T value);
    }

    /// <summary>
    /// 文件配置仓储实现
    /// </summary>
    public class FileConfigRepository : IConfigRepository
    {
  private const string CONFIG_DIR = "Config";
  private AppConfig _cachedConfig;

   public FileConfigRepository()
    {
            // 确保配置目录存在
     Directory.CreateDirectory(CONFIG_DIR);
        }

        public async Task<AppConfig> LoadAsync()
        {
 if (_cachedConfig != null)
       return _cachedConfig;

   _cachedConfig = new AppConfig
            {
      SelectedPort = await LoadFileAsync(AppConstants.ConfigFiles.SerialPort, ""),
 IsPortLocked = await LoadFileAsync(AppConstants.ConfigFiles.LockState, "false") == "true",
    DeviceName = await LoadFileAsync(AppConstants.ConfigFiles.DeviceName, AppConstants.Defaults.DeviceName),
    ConnectionSettings = new ConnectionConfig
           {
                BaudRate = AppConstants.Defaults.BaudRate,
        DataBits = AppConstants.Defaults.DataBits
   }
  };

        return _cachedConfig;
    }

     public async Task SaveAsync(AppConfig config)
        {
            _cachedConfig = config;

     await SaveFileAsync(AppConstants.ConfigFiles.SerialPort, config.SelectedPort ?? "");
         await SaveFileAsync(AppConstants.ConfigFiles.LockState, config.IsPortLocked.ToString().ToLower());
    await SaveFileAsync(AppConstants.ConfigFiles.DeviceName, config.DeviceName ?? AppConstants.Defaults.DeviceName);
        }

public async Task<T> GetValueAsync<T>(string key, T defaultValue)
      {
     try
      {
     var value = await LoadFileAsync(key, defaultValue?.ToString() ?? "");
         return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
         {
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
        catch
    {
    // 忽略读取错误，返回默认值
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
            catch
            {
       // 忽略保存错误
            }
}
    }
}

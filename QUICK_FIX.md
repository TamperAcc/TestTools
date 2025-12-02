## ?? 修复代码片段

### 需要修改的位置：Form1.cs 第 122 行附近

**找到这段代码：**
```csharp
private void menuSettings_Click(object sender, EventArgs e)
{
    using (var settingsForm = new SettingsForm(_appConfig.SelectedPort, _appConfig.IsPortLocked, _appConfig.DeviceName))
    {
 if (settingsForm.ShowDialog() == DialogResult.OK)
        {
    // 更新配置
     _appConfig.SelectedPort = settingsForm.SelectedPort;
            _appConfig.IsPortLocked = settingsForm.IsPortLocked;
   
         // 保存配置
        _configRepository.SaveAsync(_appConfig).Wait();  // ? 问题在这里！
        }
   }
}
```

**替换为：**
```csharp
private async void menuSettings_Click(object sender, EventArgs e)  // ? 添加 async
{
using (var settingsForm = new SettingsForm(_appConfig.SelectedPort, _appConfig.IsPortLocked, _appConfig.DeviceName))
    {
        if (settingsForm.ShowDialog() == DialogResult.OK)
   {
        // 更新配置
   _appConfig.SelectedPort = settingsForm.SelectedPort;
       _appConfig.IsPortLocked = settingsForm.IsPortLocked;
   
  // 保存配置
   try
   {
       await _configRepository.SaveAsync(_appConfig);  // ? 使用 await
            }
   catch
   {
    // 忽略保存错误
   }
     }
    }
}
```

---

## ?? 修改步骤

1. 在 Visual Studio 中打开 `Form1.cs`
2. 按 `Ctrl+F` 搜索 `menuSettings_Click`
3. 找到方法签名，添加 `async` 关键字
4. 找到 `.Wait()` 那一行
5. 替换为 `await`，并包裹在 try-catch 中
6. 保存文件
7. 重新编译

---

## ? 快速修复（复制粘贴）

直接复制下面的完整方法替换原方法：

```csharp
private async void menuSettings_Click(object sender, EventArgs e)
{
    using (var settingsForm = new SettingsForm(_appConfig.SelectedPort, _appConfig.IsPortLocked, _appConfig.DeviceName))
    {
        if (settingsForm.ShowDialog() == DialogResult.OK)
        {
   _appConfig.SelectedPort = settingsForm.SelectedPort;
_appConfig.IsPortLocked = settingsForm.IsPortLocked;
      
       try
   {
 await _configRepository.SaveAsync(_appConfig);
            }
          catch
      {
    // 忽略保存错误
     }
        }
    }
}
```

---

## ? 验证修改

编译后测试：
1. 打开设置
2. 选择串口
3. 点击锁定
4. 点击确定
5. 主窗口应该**立即可操作**，不再卡死

---

**注意**: 这是第3个因 Wait() 导致的严重Bug！请立即修复！

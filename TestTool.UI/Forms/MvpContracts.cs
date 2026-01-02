using System;
using TestTool.Core.Enums;
using TestTool.Core.Models;

namespace TestTool
{
    /// <summary>
    /// 多设备设置视图接口：提供视图交互点
    /// </summary>
    public interface IMultiDeviceSettingsView
    {
        event EventHandler<DeviceSettingsChangedEventArgs> DeviceSettingsChanged;
        event EventHandler SettingsConfirmed;

        (string port, int baudRate, bool isLocked) GetDeviceSettings(DeviceType deviceType);
        void RefreshAvailablePorts(DeviceType deviceType);
        void SetMonitorState(DeviceType deviceType, bool isOpen);
    }

    /// <summary>
    /// 多设备设置 Presenter 接口
    /// </summary>
    public interface IMultiDeviceSettingsPresenter
    {
        void Bind(IMultiDeviceSettingsView view);
        void HandleDeviceLocked(DeviceSettingsChangedEventArgs args);
        void HandleConfirm();
        void UpdateMonitorState(DeviceType deviceType, bool isOpen);
    }

    /// <summary>
    /// 主窗体对设置窗体暴露的操作接口
    /// </summary>
    public interface IMainFormUi
    {
        void OpenAllMonitors();
        void CloseAllMonitors();
        void ToggleMonitor(DeviceType deviceType);
        bool IsMonitorOpen(DeviceType deviceType);
        event EventHandler<MonitorStateChangedEventArgs> MonitorStateChanged;
    }
}

// test marker

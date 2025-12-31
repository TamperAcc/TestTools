using System;

namespace TestTool
{
    // 分离的局部类：引用并触发 SettingsConfirmed 以避免未使用警告
    public partial class MultiDeviceSettingsForm
    {
        private void NotifySettingsConfirmed()
        {
            SettingsConfirmed?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 当内部设备设置变化事件触发时，同步触发 SettingsConfirmed 供外部使用
            DeviceSettingsChanged += (_, _) => NotifySettingsConfirmed();
        }
    }
}

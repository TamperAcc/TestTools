using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestTool.Business.Enums;
using TestTool.Business.Services;

namespace TestTool
{
    public class MultiDeviceSettingsPresenter : IMultiDeviceSettingsPresenter
    {
        private readonly IMultiDeviceCoordinator _coordinator;
        private readonly IMainFormUi _mainForm;
        private readonly ILogger<MultiDeviceSettingsPresenter>? _logger;
        private IMultiDeviceSettingsView? _view;

        public MultiDeviceSettingsPresenter(
            IMultiDeviceCoordinator coordinator,
            IMainFormUi mainForm,
            ILogger<MultiDeviceSettingsPresenter>? logger = null)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _logger = logger;
        }

        public void Bind(IMultiDeviceSettingsView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _view.DeviceSettingsChanged += async (_, args) => await OnDeviceSettingsChangedAsync(args);
            _view.SettingsConfirmed += async (_, __) => await OnSettingsConfirmedAsync();
        }

        public void HandleDeviceLocked(DeviceSettingsChangedEventArgs args)
        {
            _ = OnDeviceSettingsChangedAsync(args);
        }

        public void HandleConfirm()
        {
            _ = OnSettingsConfirmedAsync();
        }

        public void UpdateMonitorState(DeviceType deviceType, bool isOpen)
        {
            _view?.SetMonitorState(deviceType, isOpen);
        }

        private async Task OnDeviceSettingsChangedAsync(DeviceSettingsChangedEventArgs args)
        {
            if (_view == null) return;

            try
            {
                if (!args.IsLocked && _coordinator.IsConnected(args.DeviceType))
                {
                    _logger?.LogInformation("Device {Device} unlocked, disconnecting...", args.DeviceType);
                    await _coordinator.DisconnectAsync(args.DeviceType).ConfigureAwait(false);
                }

                _coordinator.TryUpdateConnectionConfig(args.DeviceType, args.Port, args.BaudRate, args.IsLocked);
                await _coordinator.SaveConfigAsync().ConfigureAwait(false);
                _logger?.LogInformation("Device {Device} settings saved: Port={Port}, BaudRate={BaudRate}, Locked={Locked}",
                    args.DeviceType, args.Port, args.BaudRate, args.IsLocked);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error saving device settings for {Device}", args.DeviceType);
            }
            finally
            {
                _view.RefreshAvailablePorts(args.DeviceType);
            }
        }

        private async Task OnSettingsConfirmedAsync()
        {
            if (_view == null) return;

            try
            {
                foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
                {
                    var (port, baudRate, isLocked) = _view.GetDeviceSettings(deviceType);
                    _coordinator.TryUpdateConnectionConfig(deviceType, port, baudRate, isLocked);
                }

                await _coordinator.SaveConfigAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error saving settings");
            }
        }
    }
}

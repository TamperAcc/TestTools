using System;
using Microsoft.Extensions.Logging;
using Moq;
using TestTool;
using TestTool.Business.Services;
using TestTool.Core.Enums;
using Xunit;

namespace TestTool.Tests
{
    public class MultiDeviceSettingsPresenterTests
    {
        [Fact]
        public void DeviceSettingsChanged_Unlocked_WhenConnected_DisconnectsAndSaves()
        {
            var coordinator = new Mock<IMultiDeviceCoordinator>();
            coordinator.Setup(c => c.IsConnected(DeviceType.FCC1)).Returns(true);
            var mainForm = new Mock<IMainFormUi>();
            var logger = new Mock<ILogger<MultiDeviceSettingsPresenter>>();
            var view = new Mock<IMultiDeviceSettingsView>();

            var presenter = new MultiDeviceSettingsPresenter(coordinator.Object, mainForm.Object, logger.Object);
            presenter.Bind(view.Object);

            var args = new DeviceSettingsChangedEventArgs(DeviceType.FCC1, "COM1", 9600, isLocked: false);
            view.Raise(v => v.DeviceSettingsChanged += null, args);

            coordinator.Verify(c => c.DisconnectAsync(DeviceType.FCC1, default), Times.Once);
            coordinator.Verify(c => c.TryUpdateConnectionConfig(DeviceType.FCC1, "COM1", 9600, false), Times.Once);
            coordinator.Verify(c => c.SaveConfigAsync(), Times.Once);
            view.Verify(v => v.RefreshAvailablePorts(DeviceType.FCC1), Times.Once);
        }

        [Fact]
        public void SettingsConfirmed_SavesAllDevices()
        {
            var coordinator = new Mock<IMultiDeviceCoordinator>();
            var mainForm = new Mock<IMainFormUi>();
            var logger = new Mock<ILogger<MultiDeviceSettingsPresenter>>();
            var view = new Mock<IMultiDeviceSettingsView>();

            view.SetupSequence(v => v.GetDeviceSettings(It.IsAny<DeviceType>()))
                .Returns(() => ("C1", 9600, true))
                .Returns(() => ("C2", 9600, true))
                .Returns(() => ("C3", 9600, true))
                .Returns(() => ("C4", 9600, true));

            var presenter = new MultiDeviceSettingsPresenter(coordinator.Object, mainForm.Object, logger.Object);
            presenter.Bind(view.Object);

            view.Raise(v => v.SettingsConfirmed += null, EventArgs.Empty);

            coordinator.Verify(c => c.TryUpdateConnectionConfig(It.IsAny<DeviceType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Exactly(4));
            coordinator.Verify(c => c.SaveConfigAsync(), Times.Once);
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TestTool.Business.Enums;
using TestTool.Business.Models;
using TestTool.Business.Services;
using Xunit;

namespace TestTool.Tests
{
    public class PowerDeviceControllerTests
    {
        [Fact]
        public async Task TurnOnAsync_UpdatesState_WhenConnected()
        {
            var serial = new Mock<ISerialPortService>();
            serial.Setup(s => s.SendCommandAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
            serial.SetupGet(s => s.IsConnected).Returns(true);

            var logger = new Mock<ILogger<PowerDeviceController>>();
            var parser = new Mock<IProtocolParser>();
            var parserFactory = new Mock<IProtocolParserFactory>();
            parserFactory.Setup(f => f.Create()).Returns(parser.Object);
            var controller = new PowerDeviceController(logger.Object, parserFactory.Object);
            await controller.InitializeAsync(serial.Object);

            DeviceStatusChangedEventArgs? captured = null;
            controller.StatusChanged += (_, e) => captured = e;

            // Simulate connection established
            serial.Raise(s => s.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Connected, "ok"));

            var ok = await controller.TurnOnAsync();

            Assert.True(ok);
            Assert.NotNull(captured);
            Assert.Equal(DevicePowerState.On, controller.CurrentStatus.PowerState);
        }

        [Fact]
        public void ConnectionLost_SetsPowerUnknown()
        {
            var serial = new Mock<ISerialPortService>();
            var logger = new Mock<ILogger<PowerDeviceController>>();
            var parser = new Mock<IProtocolParser>();
            var parserFactory = new Mock<IProtocolParserFactory>();
            parserFactory.Setup(f => f.Create()).Returns(parser.Object);
            var controller = new PowerDeviceController(logger.Object, parserFactory.Object);
            controller.InitializeAsync(serial.Object);

            serial.Raise(s => s.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected, "lost"));

            Assert.Equal(DevicePowerState.Unknown, controller.CurrentStatus.PowerState);
        }

        [Fact]
        public void DataReceived_ParsesAndUpdatesPowerState()
        {
            var serial = new Mock<ISerialPortService>();
            var logger = new Mock<ILogger<PowerDeviceController>>();
            var parser = new Mock<IProtocolParser>();
            parser.Setup(p => p.Parse(It.IsAny<string>())).Returns(new[]
            {
                new ParsedFrame { Raw = "status on", PowerState = DevicePowerState.On, Command = "STATUS" }
            });
            var parserFactory = new Mock<IProtocolParserFactory>();
            parserFactory.Setup(f => f.Create()).Returns(parser.Object);

            var controller = new PowerDeviceController(logger.Object, parserFactory.Object);
            controller.InitializeAsync(serial.Object);

            DeviceStatusChangedEventArgs? captured = null;
            controller.StatusChanged += (_, e) => captured = e;

            serial.Raise(s => s.DataReceived += null, new DataReceivedEventArgs("status on"));

            Assert.NotNull(captured);
            Assert.Equal(DevicePowerState.On, controller.CurrentStatus.PowerState);
        }
    }
}

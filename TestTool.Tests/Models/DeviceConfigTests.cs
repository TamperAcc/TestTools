using Xunit;
using TestTool.Business.Models;

namespace TestTool.Tests.Models;

/// <summary>
/// DeviceConfig 单元测试
/// </summary>
public class DeviceConfigTests
{
    [Fact]
    public void Constructor_Default_HasEmptyDeviceName()
    {
        var config = new DeviceConfig();
        Assert.Equal(string.Empty, config.DeviceName);
    }

    [Fact]
    public void Constructor_WithDeviceName_SetsDeviceName()
    {
        var config = new DeviceConfig { DeviceName = "FCC1" };
        Assert.Equal("FCC1", config.DeviceName);
    }

    [Fact]
    public void IsPortLocked_Default_IsFalse()
    {
        var config = new DeviceConfig();
        Assert.False(config.IsPortLocked);
    }

    [Fact]
    public void SelectedPort_CanBeSetAndRetrieved()
    {
        var config = new DeviceConfig();
        config.SelectedPort = "COM3";
        Assert.Equal("COM3", config.SelectedPort);
    }

    [Fact]
    public void ConnectionSettings_Default_IsNull()
    {
        var config = new DeviceConfig();
        Assert.Null(config.ConnectionSettings);
    }

    [Fact]
    public void ConnectionSettings_CanBeSet()
    {
        var config = new DeviceConfig();
        var connectionConfig = new ConnectionConfig("COM3") { BaudRate = 9600 };
        config.ConnectionSettings = connectionConfig;
        Assert.NotNull(config.ConnectionSettings);
        Assert.Equal("COM3", config.ConnectionSettings.PortName);
        Assert.Equal(9600, config.ConnectionSettings.BaudRate);
    }

    [Fact]
    public void OnCommand_Default_IsEmpty()
    {
        var config = new DeviceConfig();
        Assert.Equal(string.Empty, config.OnCommand);
    }

    [Fact]
    public void OffCommand_Default_IsEmpty()
    {
        var config = new DeviceConfig();
        Assert.Equal(string.Empty, config.OffCommand);
    }
}

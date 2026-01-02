using Xunit;
using TestTool.Core.Models;

namespace TestTool.Tests.Models;

/// <summary>
/// ConnectionConfig 单元测试
/// </summary>
public class ConnectionConfigTests
{
    [Fact]
    public void Constructor_WithPortName_SetsPortName()
    {
        var config = new ConnectionConfig("COM3");
        Assert.Equal("COM3", config.PortName);
    }

    [Fact]
    public void Constructor_Default_HasDefaultValues()
    {
        var config = new ConnectionConfig();
        Assert.Equal(string.Empty, config.PortName);
        Assert.Equal(115200, config.BaudRate);
        Assert.Equal(8, config.DataBits);
        Assert.Equal(System.IO.Ports.Parity.None, config.Parity);
        Assert.Equal(System.IO.Ports.StopBits.One, config.StopBits);
    }

    [Fact]
    public void NormalizeWithDefaults_WithZeroBaudRate_ReturnsDefaultBaudRate()
    {
        var config = new ConnectionConfig { BaudRate = 0 };
        var normalized = config.NormalizeWithDefaults();
        Assert.Equal(115200, normalized.BaudRate);
    }

    [Fact]
    public void NormalizeWithDefaults_WithValidBaudRate_KeepsOriginalValue()
    {
        var config = new ConnectionConfig { BaudRate = 9600 };
        var normalized = config.NormalizeWithDefaults();
        Assert.Equal(9600, normalized.BaudRate);
    }

    [Fact]
    public void NormalizeWithDefaults_PreservesPortName()
    {
        var config = new ConnectionConfig("COM5") { BaudRate = 0 };
        var normalized = config.NormalizeWithDefaults();
        Assert.Equal("COM5", normalized.PortName);
    }

    [Theory]
    [InlineData(9600)]
    [InlineData(19200)]
    [InlineData(38400)]
    [InlineData(57600)]
    [InlineData(115200)]
    [InlineData(230400)]
    public void NormalizeWithDefaults_WithCommonBaudRates_KeepsOriginalValue(int baudRate)
    {
        var config = new ConnectionConfig { BaudRate = baudRate };
        var normalized = config.NormalizeWithDefaults();
        Assert.Equal(baudRate, normalized.BaudRate);
    }
}

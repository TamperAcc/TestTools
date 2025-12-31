using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 串口适配器工厂接口，用于根据配置创建适配器实例。
    /// </summary>
    public interface ISerialPortAdapterFactory
    {
        ISerialPortAdapter Create(ConnectionConfig config);
    }

    /// <summary>
    /// 协议解析器工厂接口，负责创建解析器实例。
    /// </summary>
    public interface IProtocolParserFactory
    {
        IProtocolParser Create();
    }

    /// <summary>
    /// 设备控制器工厂接口，生成控制器实例以支持 DI。
    /// </summary>
    public interface IDeviceControllerFactory
    {
        IDeviceController Create();
    }
}

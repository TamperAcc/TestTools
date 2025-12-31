using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 默认串口适配器工厂：从 DI 获取或创建默认适配器并配置参数
    /// </summary>
    public class DefaultSerialPortAdapterFactory : ISerialPortAdapterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultSerialPortAdapterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ISerialPortAdapter Create(ConnectionConfig config)
        {
            var adapter = _serviceProvider.GetService<ISerialPortAdapter>() ?? new DefaultSerialPortAdapter();
            adapter.PortName = config.PortName;
            adapter.BaudRate = config.BaudRate;
            adapter.Parity = config.Parity;
            adapter.DataBits = config.DataBits;
            adapter.StopBits = config.StopBits;
            adapter.Encoding = config.Encoding;
            adapter.ReadTimeout = config.ReadTimeout;
            adapter.WriteTimeout = config.WriteTimeout;
            return adapter;
        }
    }

    /// <summary>
    /// 默认协议解析器工厂：从 DI 获取或创建简单解析器
    /// </summary>
    public class DefaultProtocolParserFactory : IProtocolParserFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultProtocolParserFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IProtocolParser Create()
        {
            return _serviceProvider.GetService<IProtocolParser>() ?? new SimpleProtocolParser();
        }
    }

    /// <summary>
    /// 默认设备控制器工厂：从 DI 解析控制器（默认单例）
    /// </summary>
    public class DefaultDeviceControllerFactory : IDeviceControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultDeviceControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IDeviceController Create()
        {
            return _serviceProvider.GetRequiredService<IDeviceController>();
        }
    }
}

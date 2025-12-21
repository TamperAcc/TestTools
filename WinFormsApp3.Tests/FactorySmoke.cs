using System;
using Microsoft.Extensions.DependencyInjection;
using TestTool.Business.Services;
using TestTool.Business.Models;

namespace WinFormsApp3.Tests
{
    // 简易烟囱检查：确保工厂可以创建默认实例（非单元测试运行时调用）
    internal static class FactorySmoke
    {
        public static void Run(IServiceProvider serviceProvider)
        {
            var adapterFactory = serviceProvider.GetService<ISerialPortAdapterFactory>();
            var parserFactory = serviceProvider.GetService<IProtocolParserFactory>();
            var controllerFactory = serviceProvider.GetService<IDeviceControllerFactory>();

            var cfg = new ConnectionConfig("COM1");
            _ = adapterFactory?.Create(cfg);
            _ = parserFactory?.Create();
            _ = controllerFactory?.Create();
        }
    }
}

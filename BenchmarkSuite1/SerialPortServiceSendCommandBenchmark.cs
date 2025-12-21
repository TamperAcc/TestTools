using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using TestTool.Business.Services;
using Microsoft.VSDiagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace TestTool.Benchmarks
{
    [CPUUsageDiagnoser]
    public class SerialPortServiceSendCommandBenchmark
    {
        private SerialPortService _service;
        [GlobalSetup]
        public void Setup()
        {
            // 基准初始化：注册默认适配器与工厂，构造串口服务
            var logger = LoggerFactory.Create(builder => { }).CreateLogger<SerialPortService>();
            var services = new ServiceCollection();
            services.AddTransient<ISerialPortAdapter, DefaultSerialPortAdapter>();
            services.AddSingleton<ISerialPortAdapterFactory, DefaultSerialPortAdapterFactory>();
            var sp = services.BuildServiceProvider();
            var adapterFactory = sp.GetRequiredService<ISerialPortAdapterFactory>();
            _service = new SerialPortService(logger, adapterFactory, null);
        }

        [Benchmark]
        public Task<bool> SendCommandWithoutConnection()
        {
            // 未连接状态下发送命令，测量异常路径开销
            return _service.SendCommandAsync("TEST_COMMAND");
        }
    }
}
using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TestTool.Business.Models;
using TestTool.Business.Services;

namespace TestTool
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // 设置全局异常处理
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show($"发生未捕获的 UI 异常: {e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"发生未捕获的非 UI 异常: {ex?.Message}", "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved(); // 防止程序崩溃
                // 实际场景建议记录日志
            };

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // 确保加载 appsettings.json
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // 将 AppConfig 节绑定到 IOptions<AppConfig>
                    services.AddOptions<AppConfig>()
                        .Bind(context.Configuration.GetSection("AppConfig"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    // 注册应用服务和窗体类型到 DI 容器，按建议选择生命周期：

                    // SerialPortService: 单例
                    // - 代表物理串口资源，需全局唯一。
                    // - 持有连接状态并发布事件，UI/控制器消费。
                    // - 需线程安全且长生命周期，由 Host 管理释放。
                    services.AddSingleton<Business.Services.ISerialPortService, Business.Services.SerialPortService>();
                    // 串口适配器：瞬态，保证每次获取都是新实例
                    services.AddTransient<Business.Services.ISerialPortAdapter, Business.Services.DefaultSerialPortAdapter>();
                    // 适配器工厂：单例，用于创建并配置适配器
                    services.AddSingleton<Business.Services.ISerialPortAdapterFactory, Business.Services.DefaultSerialPortAdapterFactory>();
 
                    // 协议解析器（可按设备替换）
                    services.AddSingleton<Business.Services.IProtocolParser, Business.Services.SimpleProtocolParser>();
                    // 协议解析器工厂：可替换为其他协议
                    services.AddSingleton<Business.Services.IProtocolParserFactory, Business.Services.DefaultProtocolParserFactory>();
 
                    // 电源控制器：单例
                    // - 管理设备状态并与串口服务协作。
                    // - 单实例保证状态与事件一致。
                    services.AddSingleton<Business.Services.IDeviceControllerFactory, Business.Services.DefaultDeviceControllerFactory>();
                    services.AddSingleton<Business.Services.IDeviceController, Business.Services.PowerDeviceController>();
 
                    // 配置仓库：单例，负责配置缓存与磁盘持久化，避免重复磁盘访问
                    services.AddSingleton<Data.IConfigRepository, Data.FileConfigRepository>();
 
                    // 协调器：减轻 MainForm 职责
                    services.AddSingleton<IMainFormCoordinator, MainFormCoordinator>();
 
                    // 主窗体：单例（应用生命周期内唯一窗口）
                    services.AddSingleton<MainForm>();
 
                    // 对话框/监视器：瞬态，每次创建新实例，避免状态残留
                    services.AddTransient<SettingsForm>();
                    services.AddTransient<SerialMonitorForm>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
 
            // 解析主窗体并运行
            var form = host.Services.GetRequiredService<MainForm>();
            Application.Run(form);
        }
    }
}
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
                MessageBox.Show($"捕获未处理的 UI 异常: {e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"捕获未处理的非 UI 异常: {ex?.Message}", "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved(); // 防止程序崩溃
            };

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // 确保加载 appsettings.json
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // 绑定 AppConfig 到 IOptions<AppConfig>
                    services.AddOptions<AppConfig>()
                        .Bind(context.Configuration.GetSection("AppConfig"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    // 串口适配器：瞬态，每次获取新实例
                    services.AddTransient<Business.Services.ISerialPortAdapter, Business.Services.DefaultSerialPortAdapter>();
                    // 适配器工厂：单例，用于创建并配置适配器
                    services.AddSingleton<Business.Services.ISerialPortAdapterFactory, Business.Services.DefaultSerialPortAdapterFactory>();
 
                    // 串口服务：改为瞬态，由工厂按需创建多个实例（每设备一个）
                    services.AddTransient<Business.Services.ISerialPortService, Business.Services.SerialPortService>();
                    services.AddSingleton<Business.Services.ISerialPortServiceFactory, Business.Services.DefaultSerialPortServiceFactory>();

                    // 协议解析器（可按设备替换）
                    services.AddSingleton<Business.Services.IProtocolParser, Business.Services.SimpleProtocolParser>();
                    // 协议解析器工厂：可替换为其他协议
                    services.AddSingleton<Business.Services.IProtocolParserFactory, Business.Services.DefaultProtocolParserFactory>();
 
                    // 设备控制器：瞬态，由工厂按需创建多个实例（每设备一个）
                    services.AddTransient<Business.Services.IDeviceController, Business.Services.PowerDeviceController>();
                    services.AddSingleton<Business.Services.IDeviceControllerFactory, Business.Services.DefaultDeviceControllerFactory>();
 
                    // 配置仓库：单例，负责配置缓存与磁盘持久化，避免重复磁盘访问
                    services.AddSingleton<Data.IConfigRepository, Data.FileConfigRepository>();
 
                    // 多设备协调器：单例，管理所有设备
                    services.AddSingleton<IMultiDeviceCoordinator, MultiDeviceCoordinator>();
 
                    // 主窗体：单例（应用生命周期内唯一窗口）
                    services.AddSingleton<MainForm>();
 
                    // 对话框/监视器：瞬态，每次创建新实例，避免状态残留
                    services.AddTransient<MultiDeviceSettingsForm>();
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

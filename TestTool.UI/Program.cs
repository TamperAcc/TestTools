using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TestTool.Business.Models;
using TestTool.Business.Services;
using TestTool;
namespace TestTool.UI
{
    internal static class UiProgram
    {
        /// <summary>
        ///  应用入口。
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

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
                e.SetObserved();
            };

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions<AppConfig>()
                        .Bind(context.Configuration.GetSection("AppConfig"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    services.AddTransient<Business.Services.ISerialPortAdapter, Business.Services.DefaultSerialPortAdapter>();
                    services.AddSingleton<Business.Services.ISerialPortAdapterFactory, Business.Services.DefaultSerialPortAdapterFactory>();
                    services.AddTransient<Business.Services.ISerialPortService, Business.Services.SerialPortService>();
                    services.AddSingleton<Business.Services.ISerialPortServiceFactory, Business.Services.DefaultSerialPortServiceFactory>();
                    services.AddSingleton<Business.Services.IProtocolParser, Business.Services.SimpleProtocolParser>();
                    services.AddSingleton<Business.Services.IProtocolParserFactory, Business.Services.DefaultProtocolParserFactory>();
                    services.AddTransient<Business.Services.IDeviceController, Business.Services.PowerDeviceController>();
                    services.AddSingleton<Business.Services.IDeviceControllerFactory, Business.Services.DefaultDeviceControllerFactory>();
                    services.AddSingleton<Data.IConfigRepository, Data.FileConfigRepository>();
                    services.AddSingleton<IMultiDeviceCoordinator, MultiDeviceCoordinator>();

                    services.AddSingleton<MainForm>();
                    services.AddTransient<MultiDeviceSettingsForm>();
                    services.AddTransient<SerialMonitorForm>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            var form = host.Services.GetRequiredService<MainForm>();
            Application.Run(form);
        }
    }
}

using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TestTool.Core.Models;
using TestTool.Core.Services;
using TestTool.Business.Services;
using TestTool;
namespace TestTool.UI
{
    internal static class UiProgram
    {
        /// <summary>
        ///  搴旂敤鍏ュ彛銆?
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

                    services.AddTransient<ISerialPortAdapter, DefaultSerialPortAdapter>();
                    services.AddSingleton<ISerialPortAdapterFactory, DefaultSerialPortAdapterFactory>();
                    services.AddTransient<ISerialPortService, SerialPortService>();
                    services.AddSingleton<ISerialPortServiceFactory, DefaultSerialPortServiceFactory>();
                    services.AddSingleton<IProtocolParser, SimpleProtocolParser>();
                    services.AddSingleton<IProtocolParserFactory, DefaultProtocolParserFactory>();
                    services.AddTransient<IDeviceController, PowerDeviceController>();
                    services.AddSingleton<IDeviceControllerFactory, DefaultDeviceControllerFactory>();
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

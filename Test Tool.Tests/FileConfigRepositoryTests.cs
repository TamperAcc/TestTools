using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestTool.Business.Models;
using TestTool.Data;
using Xunit;

namespace TestTool.Tests
{
    public class FileConfigRepositoryTests
    {
        [Fact]
        public async Task SaveAndLoad_RoundtripInTempDir()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var originalCwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tmp;
            try
            {
                var logger = new Mock<ILogger<FileConfigRepository>>();
                var repo = new FileConfigRepository(logger.Object);
                var cfg = new AppConfig
                {
                    SelectedPort = "COM9",
                    IsPortLocked = true,
                    DeviceName = "TestDevice",
                    ConnectionSettings = new ConnectionConfig("COM9")
                };

                await repo.SaveAsync(cfg);
                var repo2 = new FileConfigRepository(logger.Object);
                var loaded = await repo2.LoadAsync();

                Assert.Equal("COM9", loaded.SelectedPort);
                Assert.True(loaded.IsPortLocked);
                Assert.Equal("TestDevice", loaded.DeviceName);
            }
            finally
            {
                Environment.CurrentDirectory = originalCwd;
                Directory.Delete(tmp, true);
            }
        }

        [Fact]
        public async Task Load_UsesOptionsDefaults_WhenFilesMissing()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var originalCwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tmp;
            try
            {
                var logger = new Mock<ILogger<FileConfigRepository>>();
                var options = new Mock<IOptionsMonitor<AppConfig>>();
                options.Setup(o => o.CurrentValue).Returns(new AppConfig
                {
                    SelectedPort = "COM1",
                    IsPortLocked = false,
                    DeviceName = "FromOptions",
                    ConnectionSettings = new ConnectionConfig("COM1") { BaudRate = 9600 }
                });

                var repo = new FileConfigRepository(logger.Object, options.Object);
                var loaded = await repo.LoadAsync();

                Assert.Equal("COM1", loaded.SelectedPort);
                Assert.Equal("FromOptions", loaded.DeviceName);
                Assert.Equal(9600, loaded.ConnectionSettings.BaudRate);
            }
            finally
            {
                Environment.CurrentDirectory = originalCwd;
                Directory.Delete(tmp, true);
            }
        }
    }
}

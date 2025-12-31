using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestTool.Business.Models;
using TestTool.Business.Services;
using TestTool.Business.Enums;
using Polly;
using Xunit;

namespace TestTool.Tests
{
    public class SerialPortServiceTests
    {
        [Fact]
        public async Task ConnectAsync_RetriesOnFailure_ThenSucceeds()
        {
            var adapter = new FakeAdapter();
            adapter.OpenBehavior = () =>
            {
                adapter.OpenAttempts++;
                if (adapter.OpenAttempts < 3)
                    throw new InvalidOperationException("fail");
            };

            var options = new StubOptionsMonitor(new AppConfig { RetryPolicy = new RetryPolicyConfig { ConnectRetries = 3 } });
            var logger = new Mock<ILogger<SerialPortService>>();
            var factory = new StubAdapterFactory(adapter);

            var svc = new SerialPortService(logger.Object, factory, options);
            var result = await svc.ConnectAsync(new ConnectionConfig("COM1"));

            Assert.True(result);
            Assert.Equal(3, adapter.OpenAttempts);
        }

        [Fact]
        public async Task ConnectAsync_UsesHotReloadedPolicy()
        {
            var adapter = new FakeAdapter();
            adapter.OpenBehavior = () =>
            {
                adapter.OpenAttempts++;
                if (adapter.OpenAttempts < 3)
                    throw new InvalidOperationException("fail");
            };

            var options = new StubOptionsMonitor(new AppConfig { RetryPolicy = new RetryPolicyConfig { ConnectRetries = 1 } });
            var logger = new Mock<ILogger<SerialPortService>>();
            var factory = new StubAdapterFactory(adapter);

            var svc = new SerialPortService(logger.Object, factory, options);

            // reload with higher retries
            options.TriggerChange(new AppConfig { RetryPolicy = new RetryPolicyConfig { ConnectRetries = 3 } });

            var result = await svc.ConnectAsync(new ConnectionConfig("COM1"));

            Assert.True(result);
            Assert.Equal(3, adapter.OpenAttempts);
        }

        [Fact]
        public async Task SendCommandAsync_RetriesOnFailure()
        {
            var adapter = new FakeAdapter();
            adapter.WriteBehavior = text =>
            {
                adapter.WriteAttempts++;
                if (adapter.WriteAttempts == 1)
                    throw new InvalidOperationException("fail");
            };
            adapter.OpenBehavior = () => adapter.IsOpen = true;

            var options = new StubOptionsMonitor(new AppConfig { RetryPolicy = new RetryPolicyConfig { SendRetries = 2 } });
            var logger = new Mock<ILogger<SerialPortService>>();
            var factory = new StubAdapterFactory(adapter);
            var svc = new SerialPortService(logger.Object, factory, options);

            // ensure connected state
            var connected = await svc.ConnectAsync(new ConnectionConfig("COM1"));
            Assert.True(connected);
            Assert.True(svc.IsConnected);

            var ok = await svc.SendCommandAsync("CMD");

            Assert.True(ok);
            Assert.Equal(2, adapter.WriteAttempts);
        }

        [Fact]
        public async Task ConnectAsync_StateChangedHandlerThrows_ServiceStillConnects()
        {
            var adapter = new FakeAdapter();
            var options = new StubOptionsMonitor(new AppConfig());
            var logger = new Mock<ILogger<SerialPortService>>();
            var factory = new StubAdapterFactory(adapter);
            var svc = new SerialPortService(logger.Object, factory, options);

            var throwCount = 0;
            svc.ConnectionStateChanged += (_, __) => throwCount++;

            var ok = await svc.ConnectAsync(new ConnectionConfig("COM1"));

            Assert.True(ok);
            Assert.True(svc.IsConnected);
            Assert.True(throwCount > 0);
        }

        [Fact]
        public async Task DataReceived_HandlerThrows_ServiceContinues()
        {
            var adapter = new FakeAdapter();
            adapter.OpenBehavior = () => adapter.IsOpen = true;
            var options = new StubOptionsMonitor(new AppConfig());
            var logger = new Mock<ILogger<SerialPortService>>();
            var factory = new StubAdapterFactory(adapter);
            var svc = new SerialPortService(logger.Object, factory, options);

            await svc.ConnectAsync(new ConnectionConfig("COM1"));

            var handled = false;
            svc.DataReceived += (_, __) => { handled = true; throw new InvalidOperationException("boom"); };

            // 触发 DataReceived
            adapter.WriteLine("TEST");

            Assert.True(handled); // 事件触发过
            Assert.True(svc.IsConnected); // 服务保持连接
        }

        private class StubAdapterFactory : ISerialPortAdapterFactory
        {
            private readonly ISerialPortAdapter _adapter;
            public StubAdapterFactory(ISerialPortAdapter adapter) => _adapter = adapter;
            public ISerialPortAdapter Create(ConnectionConfig config) => _adapter;
        }

        private class StubOptionsMonitor : IOptionsMonitor<AppConfig>
        {
            private AppConfig _value;
            public StubOptionsMonitor(AppConfig value) => _value = value;
            public AppConfig CurrentValue => _value;
            public AppConfig Get(string? name) => _value;

            public IDisposable OnChange(Action<AppConfig, string> listener)
            {
                _listeners.Add(listener);
                return new ChangeToken(() => _listeners.Remove(listener));
            }

            private readonly List<Action<AppConfig, string>> _listeners = new();

            public void TriggerChange(AppConfig newValue)
            {
                _value = newValue;
                foreach (var l in _listeners.ToArray())
                {
                    l(newValue, string.Empty);
                }
            }

            private class ChangeToken : IDisposable
            {
                private readonly Action _dispose;
                public ChangeToken(Action dispose) => _dispose = dispose;
                public void Dispose() => _dispose();
            }
        }

        private class FakeAdapter : ISerialPortAdapter
        {
            public event SerialDataReceivedEventHandler? DataReceived;
            public string PortName { get; set; } = "COM1";
            public int BaudRate { get; set; }
            public Parity Parity { get; set; }
            public int DataBits { get; set; }
            public StopBits StopBits { get; set; }
            public System.Text.Encoding? Encoding { get; set; }
            public int ReadTimeout { get; set; }
            public int WriteTimeout { get; set; }
            public bool IsOpen { get; set; }

            public int OpenAttempts { get; set; }
            public int WriteAttempts { get; set; }

            public Action? OpenBehavior { get; set; }
            public Action<string>? WriteBehavior { get; set; }

            public void Open()
            {
                OpenBehavior?.Invoke();
                IsOpen = true;
            }

            public void Close() => IsOpen = false;
            public void WriteLine(string text)
            {
                WriteBehavior?.Invoke(text);
                var args = (SerialDataReceivedEventArgs?)Activator.CreateInstance(typeof(SerialDataReceivedEventArgs), true, new object?[] { SerialData.Chars });
                DataReceived?.Invoke(this, args!);
            }
            public string ReadExisting() => string.Empty;
            public void Dispose() { }
        }
    }
}

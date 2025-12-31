using System;
using System.IO.Ports;
using System.Text;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 默认串口适配器，基于 System.IO.Ports.SerialPort 的简单封装。
    /// </summary>
    public class DefaultSerialPortAdapter : ISerialPortAdapter
    {
        private readonly SerialPort _port;
        private bool _disposed;
        private const int DefaultTimeoutMs = 3000;

        public event SerialDataReceivedEventHandler? DataReceived
        {
            add => _port.DataReceived += value;
            remove => _port.DataReceived -= value;
        }

        public DefaultSerialPortAdapter()
        {
            _port = new SerialPort
            {
                ReadTimeout = DefaultTimeoutMs,
                WriteTimeout = DefaultTimeoutMs,
                Encoding = Encoding.UTF8
            };
        }

        public string PortName { get => _port.PortName; set => _port.PortName = value; }
        public int BaudRate { get => _port.BaudRate; set => _port.BaudRate = value; }
        public Parity Parity { get => _port.Parity; set => _port.Parity = value; }
        public int DataBits { get => _port.DataBits; set => _port.DataBits = value; }
        public StopBits StopBits { get => _port.StopBits; set => _port.StopBits = value; }
        public Encoding? Encoding { get => _port.Encoding; set => _port.Encoding = value ?? Encoding.UTF8; }
        public int ReadTimeout { get => _port.ReadTimeout; set => _port.ReadTimeout = value; }
        public int WriteTimeout { get => _port.WriteTimeout; set => _port.WriteTimeout = value; }

        public bool IsOpen => !_disposed && _port.IsOpen;

        public void Open()
        {
            ThrowIfDisposed();
            if (!_port.IsOpen)
            {
                _port.Open();
            }
        }

        public void Close()
        {
            if (_disposed) return;
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        public void WriteLine(string text)
        {
            ThrowIfDisposed();
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("串口未打开，无法写入数据。");
            }
            _port.WriteLine(text);
        }

        public string ReadExisting()
        {
            ThrowIfDisposed();
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("串口未打开，无法读取数据。");
            }
            return _port.ReadExisting();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _port.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DefaultSerialPortAdapter));
            }
        }
    }
}

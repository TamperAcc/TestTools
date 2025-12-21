using System;
using System.IO.Ports;
using System.Text;

namespace TestTool.Business.Services
{
    public class DefaultSerialPortAdapter : ISerialPortAdapter
    {
        private readonly SerialPort _port;

        public event SerialDataReceivedEventHandler? DataReceived
        {
            add => _port.DataReceived += value;
            remove => _port.DataReceived -= value;
        }

        public DefaultSerialPortAdapter()
        {
            _port = new SerialPort();
        }

        public string PortName { get => _port.PortName; set => _port.PortName = value; }
        public int BaudRate { get => _port.BaudRate; set => _port.BaudRate = value; }
        public Parity Parity { get => _port.Parity; set => _port.Parity = value; }
        public int DataBits { get => _port.DataBits; set => _port.DataBits = value; }
        public StopBits StopBits { get => _port.StopBits; set => _port.StopBits = value; }
        public Encoding? Encoding { get => _port.Encoding; set => _port.Encoding = value ?? Encoding.UTF8; }
        public int ReadTimeout { get => _port.ReadTimeout; set => _port.ReadTimeout = value; }
        public int WriteTimeout { get => _port.WriteTimeout; set => _port.WriteTimeout = value; }

        public bool IsOpen => _port.IsOpen;

        public void Open() => _port.Open();
        public void Close() => _port.Close();
        public void WriteLine(string text) => _port.WriteLine(text);
        public string ReadExisting() => _port.ReadExisting();

        public void Dispose() => _port.Dispose();
    }
}
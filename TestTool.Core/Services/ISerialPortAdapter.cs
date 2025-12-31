using System;
using System.IO.Ports;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 串口适配器接口，抽象底层 SerialPort，便于替换与测试。
    /// </summary>
    public interface ISerialPortAdapter : IDisposable
    {
        event SerialDataReceivedEventHandler? DataReceived;

        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        System.Text.Encoding? Encoding { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }

        bool IsOpen { get; }

        void Open();
        void Close();
        void WriteLine(string text);
        string ReadExisting();
    }
}

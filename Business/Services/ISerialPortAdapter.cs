using System;
using System.Threading.Tasks;
using System.IO.Ports;

namespace TestTool.Business.Services
{
    /// <summary>
    /// Adapter abstraction for System.IO.Ports.SerialPort to allow testing and alternative transports.
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
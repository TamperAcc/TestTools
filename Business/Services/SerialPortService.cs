using System;
using System.IO.Ports;
using System.Threading.Tasks;
using WinFormsApp3.Business.Enums;
using WinFormsApp3.Business.Models;

namespace WinFormsApp3.Business.Services
{
    /// <summary>
    /// 串口服务实现
    /// </summary>
    public class SerialPortService : ISerialPortService
    {
        private SerialPort _serialPort;
   private ConnectionState _currentState = ConnectionState.Disconnected;
   private ConnectionConfig _currentConfig;

    public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public bool IsConnected => _serialPort?.IsOpen ?? false;
      public ConnectionConfig CurrentConfig => _currentConfig;
        public ConnectionState CurrentState => _currentState;

        public async Task<bool> ConnectAsync(ConnectionConfig config)
        {
            if (config == null || !config.IsValid())
 {
           UpdateState(ConnectionState.Error, "无效的配置");
         return false;
            }

      try
    {
   UpdateState(ConnectionState.Connecting, "正在连接...");

     // 如果已有连接，先关闭
       if (_serialPort != null)
     {
    await DisconnectAsync();
          }

       // 创建新的串口对象
    _serialPort = new SerialPort
        {
        PortName = config.PortName,
        BaudRate = config.BaudRate,
              Parity = config.Parity,
      DataBits = config.DataBits,
           StopBits = config.StopBits,
        Encoding = config.Encoding,
       ReadTimeout = config.ReadTimeout,
     WriteTimeout = config.WriteTimeout
      };

     _serialPort.DataReceived += OnSerialPortDataReceived;

            // 使用 ConfigureAwait(false) 避免阻塞UI线程
  await Task.Run(() => 
            {
      _serialPort.Open();
              }).ConfigureAwait(false);

 _currentConfig = config;
     UpdateState(ConnectionState.Connected, "已连接");
         return true;
     }
  catch (Exception ex)
        {
                UpdateState(ConnectionState.Error, $"连接失败: {ex.Message}");
           return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_serialPort == null)
       return;

        try
 {
            UpdateState(ConnectionState.Disconnecting, "正在断开...");

            await Task.Run(() =>
 {
     if (_serialPort.IsOpen)
{
                _serialPort.Close();
      }
   _serialPort.DataReceived -= OnSerialPortDataReceived;
    _serialPort.Dispose();
    });

    _serialPort = null;
                _currentConfig = null;
           UpdateState(ConnectionState.Disconnected, "已断开");
   }
        catch (Exception ex)
          {
            UpdateState(ConnectionState.Error, $"断开失败: {ex.Message}");
   }
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            if (!IsConnected)
            {
                return false;
        }

  try
       {
       await Task.Run(() => _serialPort.WriteLine(command));
      return true;
            }
            catch (Exception)
      {
         return false;
       }
        }

        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
         try
       {
            var data = _serialPort.ReadExisting();
DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
     }
    catch
        {
            // 忽略接收错误
            }
        }

private void UpdateState(ConnectionState newState, string message)
        {
            var oldState = _currentState;
    _currentState = newState;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState, message));
     }

        public void Dispose()
  {
            DisconnectAsync().Wait();
        }
    }
}

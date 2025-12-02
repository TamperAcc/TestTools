using System;
using System.Threading.Tasks;
using WinFormsApp3.Business.Enums;
using WinFormsApp3.Business.Models;
using WinFormsApp3.Infrastructure.Constants;

namespace WinFormsApp3.Business.Services
{
 /// <summary>
    /// 电源设备控制器实现
    /// </summary>
  public class PowerDeviceController : IDeviceController
    {
  private ISerialPortService _serialPortService;
     private DeviceStatus _currentStatus;

public event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

      public DeviceStatus CurrentStatus => _currentStatus;
        public string DeviceName
{
  get => _currentStatus?.DeviceName;
          set
          {
       if (_currentStatus != null)
      {
    _currentStatus.DeviceName = value;
       }
       }
      }

        public PowerDeviceController()
     {
     _currentStatus = new DeviceStatus
    {
       DeviceName = AppConstants.Defaults.DeviceName,
    ConnectionState = ConnectionState.Disconnected,
        PowerState = DevicePowerState.Unknown
     };
   }

        public Task<bool> InitializeAsync(ISerialPortService serialPortService)
      {
  _serialPortService = serialPortService;

   // 订阅串口状态变化
 _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;

    return Task.FromResult(true);
        }

  public async Task<bool> TurnOnAsync()
     {
  if (!_currentStatus.CanSendCommand)
     return false;

 try
       {
   var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOn);
     if (success)
      {
    UpdatePowerState(DevicePowerState.On, "电源已打开");
    }
    return success;
      }
            catch
   {
    return false;
        }
 }

        public async Task<bool> TurnOffAsync()
      {
      if (!_currentStatus.CanSendCommand)
    return false;

try
   {
       var success = await _serialPortService.SendCommandAsync(AppConstants.Commands.PowerOff);
if (success)
           {
     UpdatePowerState(DevicePowerState.Off, "电源已关闭");
    }
  return success;
  }
      catch
    {
            return false;
            }
    }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
       _currentStatus.ConnectionState = e.NewState;
  _currentStatus.StatusMessage = e.Message;
            _currentStatus.LastUpdateTime = DateTime.Now;

      // 断开连接时重置电源状态
     if (e.NewState == ConnectionState.Disconnected)
            {
  UpdatePowerState(DevicePowerState.Unknown, e.Message);
       }
            else
    {
       StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(_currentStatus, _currentStatus.PowerState));
      }
        }

        private void UpdatePowerState(DevicePowerState newState, string message)
        {
        var oldState = _currentStatus.PowerState;
    _currentStatus.PowerState = newState;
       _currentStatus.StatusMessage = message;
      _currentStatus.LastUpdateTime = DateTime.Now;

        StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(_currentStatus, oldState));
   }

        public void Dispose()
        {
    if (_serialPortService != null)
       {
      _serialPortService.ConnectionStateChanged -= OnConnectionStateChanged;
  }
    }
    }
}

using System;
using System.Text;
using System.Windows.Forms;
using WinFormsApp3.Business.Enums;
using WinFormsApp3.Business.Models;
using WinFormsApp3.Business.Services;
using WinFormsApp3.Data;
using WinFormsApp3.Infrastructure.Constants;
using WinFormsApp3.Infrastructure.Helpers;

namespace WinFormsApp3
{
    /// <summary>
 /// 串口工具主窗体 - 重构版本
    /// </summary>
    public partial class Form1 : Form
    {
// 业务层服务
   private readonly ISerialPortService _serialPortService;
   private readonly IDeviceController _deviceController;
        private readonly IConfigRepository _configRepository;
        
 // 应用配置
        private AppConfig _appConfig;
    
        // UI增强
 private const int RESIZE_HANDLE_SIZE = AppConstants.UI.ResizeHandleSize;

        public Form1()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
     InitializeComponent();
            
      // 初始化服务（依赖注入的简化版本）
  _configRepository = new FileConfigRepository();
     _serialPortService = new SerialPortService();
 _deviceController = new PowerDeviceController();
    
       // 初始化UI辅助工具
        UIHelper.InitializeFonts(this.Font);
        }

    private async void Form1_Load(object sender, EventArgs e)
   {
            // 加载配置
     _appConfig = await _configRepository.LoadAsync();
            
          // 初始化设备控制器
         await _deviceController.InitializeAsync(_serialPortService);
            _deviceController.DeviceName = _appConfig.DeviceName;
            
  // 订阅事件
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;
      _deviceController.StatusChanged += OnDeviceStatusChanged;
  
   // 更新UI
      UpdateUI();
 }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
    if (_serialPortService.IsConnected)
    {
       // 断开连接
         await _serialPortService.DisconnectAsync();
  }
      else
            {
     // 检查串口配置
          if (string.IsNullOrEmpty(_appConfig.SelectedPort) || _appConfig.SelectedPort == "无可用串口")
      {
              UIHelper.SetStatusLabel(lblStatus, ConnectionState.Error, _appConfig.DeviceName, UIConstants.StatusMessages.PleaseSelectPort);
 return;
      }
           
             // 创建连接配置
         var config = new ConnectionConfig(_appConfig.SelectedPort)
      {
       BaudRate = _appConfig.ConnectionSettings.BaudRate,
             DataBits = _appConfig.ConnectionSettings.DataBits,
        Parity = _appConfig.ConnectionSettings.Parity,
    StopBits = _appConfig.ConnectionSettings.StopBits,
   Encoding = _appConfig.ConnectionSettings.Encoding
    };
       
      // 连接
   btnConnect.Enabled = false;
                var success = await _serialPortService.ConnectAsync(config);
     btnConnect.Enabled = true;
   
                if (success)
         {
        // 保存配置
                 await _configRepository.SaveAsync(_appConfig);
     }
            }
        }

        private async void btnOn_Click(object sender, EventArgs e)
        {
            await _deviceController.TurnOnAsync();
        }

        private async void btnOff_Click(object sender, EventArgs e)
        {
  await _deviceController.TurnOffAsync();
        }

   private void menuSettings_Click(object sender, EventArgs e)
        {
   using (var settingsForm = new SettingsForm(_appConfig.SelectedPort, _appConfig.IsPortLocked, _appConfig.DeviceName))
   {
       if (settingsForm.ShowDialog() == DialogResult.OK)
            {
     // 更新配置
           _appConfig.SelectedPort = settingsForm.SelectedPort;
            _appConfig.IsPortLocked = settingsForm.IsPortLocked;
   
        // 保存配置
            _configRepository.SaveAsync(_appConfig).Wait();
  }
       }
        }

        // 事件处理
        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
       if (InvokeRequired)
     {
   Invoke(new Action(() => OnConnectionStateChanged(sender, e)));
         return;
            }
   
            UIHelper.SetStatusLabel(lblStatus, e.NewState, _appConfig.DeviceName, e.Message);
            UIHelper.UpdateConnectButton(btnConnect, e.NewState == ConnectionState.Connected);
            
     // 更新按钮状态
  if (e.NewState == ConnectionState.Connected)
            {
       btnOn.Enabled = true;
        btnOff.Enabled = true;
           UIHelper.SetButtonDefault(btnOn);
      UIHelper.SetButtonDefault(btnOff);
   }
      else
       {
              UIHelper.SetButtonDisabled(btnOn);
                UIHelper.SetButtonDisabled(btnOff);
     }
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
if (InvokeRequired)
      {
   Invoke(new Action(() => OnDeviceStatusChanged(sender, e)));
       return;
            }
   
       // 更新按钮状态
      if (e.Status.PowerState == DevicePowerState.On)
     {
       UIHelper.SetButtonActive(btnOn, DevicePowerState.On);
     UIHelper.SetButtonDefault(btnOff);
        }
else if (e.Status.PowerState == DevicePowerState.Off)
     {
      UIHelper.SetButtonDefault(btnOn);
   UIHelper.SetButtonActive(btnOff, DevicePowerState.Off);
   }
   else
 {
  UIHelper.SetButtonDefault(btnOn);
        UIHelper.SetButtonDefault(btnOff);
   }
 }

        private void UpdateUI()
        {
  UIHelper.SetStatusLabel(lblStatus, ConnectionState.Disconnected, _appConfig.DeviceName, UIConstants.StatusMessages.Disconnected);
  UIHelper.SetButtonDisabled(btnOn);
   UIHelper.SetButtonDisabled(btnOff);
        }

        // 窗口边框拖动支持
   protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
       const int HTTOPLEFT = 13;
       const int HTTOPRIGHT = 14;
          const int HTBOTTOM = 15;
       const int HTBOTTOMLEFT = 16;
 const int HTBOTTOMRIGHT = 17;

     if (m.Msg == WM_NCHITTEST)
            {
 base.WndProc(ref m);
                var cursor = this.PointToClient(Cursor.Position);

           if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE)
          m.Result = (IntPtr)HTTOPLEFT;
       else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE)
    m.Result = (IntPtr)HTTOPRIGHT;
     else if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
    m.Result = (IntPtr)HTBOTTOMLEFT;
     else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
       m.Result = (IntPtr)HTBOTTOMRIGHT;
      else if (cursor.X <= RESIZE_HANDLE_SIZE)
                m.Result = (IntPtr)HTLEFT;
         else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE)
m.Result = (IntPtr)HTRIGHT;
          else if (cursor.Y <= RESIZE_HANDLE_SIZE)
     m.Result = (IntPtr)HTTOP;
       else if (cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE)
   m.Result = (IntPtr)HTBOTTOM;
  }
         else
            {
     base.WndProc(ref m);
            }
 }

protected override void OnFormClosing(FormClosingEventArgs e)
        {
  // 释放资源
 _serialPortService?.Dispose();
            _deviceController?.Dispose();
        UIHelper.DisposeFonts();
        
            base.OnFormClosing(e);
        }
    }
}
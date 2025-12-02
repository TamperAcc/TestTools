using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp3
{
    /// <summary>
    /// 串口工具主窗体
    /// </summary>
    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private const string CONFIG_FILE = "serialport.config";
        private const string LOCK_STATE_FILE = "portlock.config";
        private const string DEVICE_NAME_FILE = "devicename.config";
        private bool isPortLocked = false;
        private string selectedPort = "";
        private string deviceName = "FCC1电源";
        private string currentPowerState = "";
        
        // 增加边框感应区域的宽度（像素）
        private const int RESIZE_HANDLE_SIZE = 10;
        
        // 缓存 Font 对象以避免内存泄漏
        private Font boldFont;
        private Font regularFont;

        public Form1()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
   
            // 初始化字体对象
     boldFont = new Font(this.Font, FontStyle.Bold);
     regularFont = new Font(this.Font, FontStyle.Regular);
        }

        // 统一的状态更新方法
  private void SetStatus(string message, Color backgroundColor)
        {
    lblStatus.Text = $"{deviceName} - 状态: {message}";
        lblStatus.ForeColor = Color.White;
     lblStatus.BackColor = backgroundColor;
    lblStatus.Font = boldFont;
        }

        // 重写 WndProc 以扩大边框调整大小的识别区域
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

// 检测边角和边缘
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

        private void Form1_Load(object sender, EventArgs e)
        {
    // 加载设备名称
            LoadDeviceName();
         
      // 加载上次保存的串口号
            LoadSavedPort();
      
            // 加载锁定状态
     LoadLockState();
 
       // 更新状态显示
            UpdateStatusLabel();
        }

        private void LoadDeviceName()
        {
            try
{
        if (File.Exists(DEVICE_NAME_FILE))
          {
         deviceName = File.ReadAllText(DEVICE_NAME_FILE).Trim();
     if (string.IsNullOrEmpty(deviceName))
        {
     deviceName = "FCC1电源";
          }
                }
   else
    {
       // 创建默认配置文件
          File.WriteAllText(DEVICE_NAME_FILE, deviceName);
        }
     }
       catch
  {
         deviceName = "FCC1电源";
            }
   }

      private void UpdateStatusLabel()
   {
            if (serialPort != null && serialPort.IsOpen)
            {
    SetStatus("已连接", Color.Green);

                // 连接后启用 ON/OFF 按钮，设置为浅蓝色背景
         btnOn.Enabled = true;
         btnOff.Enabled = true;
   btnOn.BackColor = Color.LightSteelBlue;
       btnOff.BackColor = Color.LightSteelBlue;
       }
            else
            {
    SetStatus("未连接", Color.DarkGray);

      // 未连接时禁用 ON/OFF 按钮并设置为灰色背景
   btnOn.Enabled = false;
 btnOff.Enabled = false;
        btnOn.BackColor = Color.LightGray;
  btnOff.BackColor = Color.LightGray;
 }
        }

private void LoadSavedPort()
     {
  try
   {
    if (File.Exists(CONFIG_FILE))
        {
      selectedPort = File.ReadAllText(CONFIG_FILE).Trim();
   }
    }
       catch
  {
     selectedPort = "";
      }
        }

        private void SaveSelectedPort()
        {
            try
  {
    if (!string.IsNullOrEmpty(selectedPort))
    {
  File.WriteAllText(CONFIG_FILE, selectedPort);
            }
   }
            catch
        {
       // 忽略保存错误
   }
        }

        private void LoadLockState()
        {
            try
            {
                if (File.Exists(LOCK_STATE_FILE))
                {
                    string lockState = File.ReadAllText(LOCK_STATE_FILE).Trim();
                    isPortLocked = lockState == "true";
                }
                else
                {
                    isPortLocked = false;
                }
            }
            catch
            {
                isPortLocked = false;
            }
        }

        private void SaveLockState()
        {
            try
            {
                File.WriteAllText(LOCK_STATE_FILE, isPortLocked.ToString().ToLower());
            }
            catch
            {
                // 忽略保存错误
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
 {
   try
 {
if (string.IsNullOrEmpty(selectedPort) || selectedPort == "无可用串口")
 {
  SetStatus("请先选择串口", Color.Orange);
 return;
   }

    if (serialPort == null || serialPort.PortName != selectedPort)
 {
       // 如果串口对象不存在或端口号改变了，重新创建
     if (serialPort != null && serialPort.IsOpen)
       {
    serialPort.Close();
        serialPort.Dispose();
      }

    // 创建串口对象，波特率可根据设备修改
   serialPort = new SerialPort(selectedPort, 115200, Parity.None, 8, StopBits.One);
       serialPort.Encoding = System.Text.Encoding.UTF8;
          serialPort.DataReceived += SerialPort_DataReceived;
  }

if (!serialPort.IsOpen)
  {
       SetStatus("连接中...", Color.Orange);
btnConnect.Enabled = false;
         
   // 异步打开串口，不阻塞 UI 线程
    await Task.Run(() => serialPort.Open());
    
btnConnect.Text = "断开连接";
   btnConnect.BackColor = Color.LightGreen;
    btnConnect.Enabled = true;
          SaveSelectedPort();
     }
   else
     {
     serialPort.Close();
  btnConnect.Text = "连接";
       btnConnect.BackColor = SystemColors.Control;
   currentPowerState = "";
        UpdatePowerButtonState();
   }

       UpdateStatusLabel();
 }
    catch (Exception ex)
 {
   SetStatus("连接失败", Color.Red);
       btnConnect.Enabled = true;
   }
  }


        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
   {
// 数据接收事件（如需处理数据，在此添加逻辑）
   // 当前不处理接收数据，保留事件处理器以避免异常
   }

        private void btnOn_Click(object sender, EventArgs e)
 {
       SendCommand("ON");
    if (serialPort != null && serialPort.IsOpen)
   {
            currentPowerState = "ON";
            UpdatePowerButtonState();
        }
        }

        private void btnOff_Click(object sender, EventArgs e)
        {
   SendCommand("OFF");
        if (serialPort != null && serialPort.IsOpen)
        {
        currentPowerState = "OFF";
      UpdatePowerButtonState();
  }
        }

        private void UpdatePowerButtonState()
        {
         // 先将所有按钮重置为浅蓝色默认状态
     btnOn.BackColor = Color.LightSteelBlue;
 btnOn.ForeColor = SystemColors.ControlText;
 btnOn.Font = regularFont;

    btnOff.BackColor = Color.LightSteelBlue;
  btnOff.ForeColor = SystemColors.ControlText;
    btnOff.Font = regularFont;

     // 只高亮显示当前点击的按钮
   if (serialPort != null && serialPort.IsOpen)
 {
      if (currentPowerState == "ON")
    {
      btnOn.BackColor = Color.LimeGreen;
     btnOn.ForeColor = Color.White;
   btnOn.Font = boldFont;
     }
   else if (currentPowerState == "OFF")
{
         btnOff.BackColor = Color.Crimson;
    btnOff.ForeColor = Color.White;
  btnOff.Font = boldFont;
      }
   }
        }

      private void SendCommand(string command)
     {
     try
      {
        if (serialPort == null || !serialPort.IsOpen)
       {
          SetStatus("未连接", Color.Orange);
   return;
      }

        serialPort.WriteLine(command);
   }
    catch (Exception ex)
    {
 SetStatus("发送失败", Color.Red);
    }
  }

 private void menuSettings_Click(object sender, EventArgs e)
 {
     // 打开设置窗口，传递设备名称
  using (var settingsForm = new SettingsForm(selectedPort, isPortLocked, deviceName))
  {
if (settingsForm.ShowDialog() == DialogResult.OK)
{
 // 获取设置窗口返回的值
 selectedPort = settingsForm.SelectedPort;
       isPortLocked = settingsForm.IsPortLocked;
  
  // 保存设置
  SaveSelectedPort();
 SaveLockState();
   
     // 更新状态显示
 UpdateStatusLabel();
 }
   }
   }

  protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
         {
    serialPort.Close();
serialPort.Dispose();
      }
     
 // 释放字体资源
  boldFont?.Dispose();
         regularFont?.Dispose();
      
        base.OnFormClosing(e);
   }
    }
}
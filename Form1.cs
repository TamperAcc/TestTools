using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp3
{
    /// <summary>
    /// 串口通信工具主窗体
    /// </summary>
    public partial class Form1 : Form
 {
   private SerialPort serialPort;
      private const string CONFIG_FILE = "serialport.config";          // 串口配置文件
     private const string LOCK_STATE_FILE = "portlock.config";    // 锁定状态配置文件
        private bool isPortLocked = false;            // 串口选择是否锁定

    public Form1()
    {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 加载可用串口列表
            LoadAvailablePorts();
            
            // 加载上次保存的串口号
            LoadSavedPort();
      
            // 加载锁定状态
            LoadLockState();
            
            // 初始化锁定按钮状态
            UpdateLockButtonState();
        }

        private void LoadAvailablePorts()
        {
            cmbPortName.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            
            if (ports.Length > 0)
            {
                cmbPortName.Items.AddRange(ports);
            }
            else
            {
                cmbPortName.Items.Add("无可用串口");
            }
        }

        private void LoadSavedPort()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    string savedPort = File.ReadAllText(CONFIG_FILE).Trim();
                    if (!string.IsNullOrEmpty(savedPort) && cmbPortName.Items.Contains(savedPort))
                    {
                        cmbPortName.SelectedItem = savedPort;
                    }
                    else if (cmbPortName.Items.Count > 0)
                    {
                        cmbPortName.SelectedIndex = 0;
                    }
                }
                else if (cmbPortName.Items.Count > 0)
                {
                    cmbPortName.SelectedIndex = 0;
                }
            }
            catch
            {
                if (cmbPortName.Items.Count > 0)
                {
                    cmbPortName.SelectedIndex = 0;
                }
            }
        }

        private void SaveSelectedPort()
        {
            try
            {
                if (cmbPortName.SelectedItem != null)
                {
                    File.WriteAllText(CONFIG_FILE, cmbPortName.SelectedItem.ToString());
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

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbPortName.SelectedItem == null || cmbPortName.SelectedItem.ToString() == "无可用串口")
                {
                    MessageBox.Show("请先选择一个串口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedPort = cmbPortName.SelectedItem.ToString();

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
                    // 设置编码格式为 UTF-8，解决中文乱码问题
                    serialPort.Encoding = System.Text.Encoding.UTF8;
                    serialPort.DataReceived += SerialPort_DataReceived;
                }

                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    txtOutput.AppendText($"已连接 {selectedPort} 串口。\r\n");
                    btnConnect.Text = "断开连接";
                    btnConnect.BackColor = Color.LightGreen;
                    lblStatus.Text = "状态: 已连接";
                    cmbPortName.Enabled = false; // 连接时禁用串口选择
                    btnLock.Enabled = false; // 连接时禁用锁定按钮
      
                    // 保存选择的串口号
                    SaveSelectedPort();
                }
                else
                {
                    serialPort.Close();
                    txtOutput.AppendText($"已断开 {selectedPort} 连接。\r\n");
                    btnConnect.Text = "连接";
                    btnConnect.BackColor = SystemColors.Control;
                    lblStatus.Text = "状态: 未连接";
                    btnLock.Enabled = true; // 断开后启用锁定按钮

      // 断开后根据锁定状态决定是否解锁串口选择
    if (!isPortLocked)
         {
     cmbPortName.Enabled = true;
  }
        }
            }
   catch (Exception ex)
     {
     MessageBox.Show("连接失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
          btnLock.Enabled = true;
 if (!isPortLocked)
         {
     cmbPortName.Enabled = true;
          }
            }
    }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadExisting();
                this.Invoke(new Action(() =>
                {
                    txtOutput.AppendText("收到数据: " + data + "\r\n");
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    txtOutput.AppendText("接收错误: " + ex.Message + "\r\n");
                }));
            }
        }

        private void btnLock_Click(object sender, EventArgs e)
        {
            if (cmbPortName.SelectedItem == null || cmbPortName.SelectedItem.ToString() == "无可用串口")
            {
                MessageBox.Show("请先选择一个串口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isPortLocked = !isPortLocked;
            UpdateLockButtonState();
   
            // 保存锁定状态
            SaveLockState();
    
 // 如果锁定，也保存当前选择的串口
if (isPortLocked)
       {
SaveSelectedPort();
      }
        }

  private void btnRefresh_Click(object sender, EventArgs e)
 {
   // 刷新可用串口列表
   LoadAvailablePorts();
      txtOutput.AppendText("已刷新串口列表。\r\n");
}

        private void btnClear_Click(object sender, EventArgs e)
        {
 // 清空输出文本框
   txtOutput.Clear();
  }

        private void UpdateLockButtonState()
        {
            if (isPortLocked)
            {
                cmbPortName.Enabled = false;
                btnLock.Text = "已锁定";
                btnLock.BackColor = Color.LightCoral;
            }
            else
            {
                // 只有在未连接状态下才允许解锁
                if (serialPort == null || !serialPort.IsOpen)
                {
                    cmbPortName.Enabled = true;
                }
                btnLock.Text = "未锁定";
                btnLock.BackColor = SystemColors.Control;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
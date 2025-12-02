using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace WinFormsApp3
{
    /// <summary>
    /// 设置窗口
    /// </summary>
    public partial class SettingsForm : Form
    {
    public string SelectedPort { get; private set; }
      public bool IsPortLocked { get; private set; }
      private string deviceName;

 public SettingsForm(string currentPort, bool isLocked, string devName)
  {
      InitializeComponent();
    SelectedPort = currentPort;
        IsPortLocked = isLocked;
        deviceName = devName;
    }

        private void SettingsForm_Load(object sender, EventArgs e)
     {
       // 设置窗口标题
     this.Text = $"{deviceName}设置";
     
   // 更新分组框标题
          groupBox1.Text = $"{deviceName}串口设置";
     
   // 加载可用串口列表
     LoadAvailablePorts();

     // 设置当前选中的串口
     if (!string.IsNullOrEmpty(SelectedPort) && cmbSettingsPort.Items.Contains(SelectedPort))
      {
  cmbSettingsPort.SelectedItem = SelectedPort;
      }
else if (cmbSettingsPort.Items.Count > 0)
    {
cmbSettingsPort.SelectedIndex = 0;
  }

    // 设置锁定状态
     UpdateLockButtonState();
     
 // 订阅下拉框的 DropDown 事件，实现自动刷新
     cmbSettingsPort.DropDown += cmbSettingsPort_DropDown;
        }

        private void cmbSettingsPort_DropDown(object sender, EventArgs e)
      {
            // 下拉时自动刷新串口列表
 string currentSelection = cmbSettingsPort.SelectedItem?.ToString();
     LoadAvailablePorts();
         
          // 尝试恢复之前的选择
  if (!string.IsNullOrEmpty(currentSelection) && cmbSettingsPort.Items.Contains(currentSelection))
      {
       cmbSettingsPort.SelectedItem = currentSelection;
       }
else if (cmbSettingsPort.Items.Count > 0 && cmbSettingsPort.SelectedIndex == -1)
            {
    cmbSettingsPort.SelectedIndex = 0;
          }
        }

 private void LoadAvailablePorts()
        {
  cmbSettingsPort.Items.Clear();
     string[] ports = SerialPort.GetPortNames();

     if (ports.Length > 0)
     {
      cmbSettingsPort.Items.AddRange(ports);
   }
 else
      {
  cmbSettingsPort.Items.Add("无可用串口");
      }
 }

        private void btnLockPort_Click(object sender, EventArgs e)
    {
          if (cmbSettingsPort.SelectedItem == null || cmbSettingsPort.SelectedItem.ToString() == "无可用串口")
 {
          MessageBox.Show("请先选择一个串口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
    }

         // 切换锁定状态
       IsPortLocked = !IsPortLocked;
 UpdateLockButtonState();
        }

  private void UpdateLockButtonState()
  {
       if (IsPortLocked)
{
          cmbSettingsPort.Enabled = false;
    btnLockPort.Text = "已锁定";
       btnLockPort.BackColor = Color.LightCoral;
       }
 else
      {
       cmbSettingsPort.Enabled = true;
     btnLockPort.Text = "未锁定";
    btnLockPort.BackColor = SystemColors.Control;
            }
        }

  private void btnOK_Click(object sender, EventArgs e)
 {
   if (cmbSettingsPort.SelectedItem == null)
            {
      MessageBox.Show("请选择一个串口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
     return;
     }

      SelectedPort = cmbSettingsPort.SelectedItem.ToString();
this.DialogResult = DialogResult.OK;
  this.Close();
        }

 private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
        }
 }
}

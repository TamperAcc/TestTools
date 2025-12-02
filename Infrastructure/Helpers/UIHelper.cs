using System.Drawing;
using System.Windows.Forms;
using WinFormsApp3.Business.Enums;
using WinFormsApp3.Infrastructure.Constants;

namespace WinFormsApp3.Infrastructure.Helpers
{
    public static class UIHelper
    {
        private static Font _boldFont;
        private static Font _regularFont;

   public static void InitializeFonts(Font baseFont)
     {
       _boldFont = new Font(baseFont, FontStyle.Bold);
_regularFont = new Font(baseFont, FontStyle.Regular);
        }

  public static void DisposeFonts()
{
     _boldFont?.Dispose();
      _regularFont?.Dispose();
 }

 public static Font BoldFont => _boldFont;
   public static Font RegularFont => _regularFont;

   public static void SetStatusLabel(Label label, ConnectionState state, string deviceName, string message)
     {
      label.Text = $"{deviceName} - 状态: {message}";
      label.ForeColor = UIConstants.TextColors.StatusLabel;
   label.Font = _boldFont;

  label.BackColor = state switch
      {
ConnectionState.Connected => UIConstants.StatusColors.Connected,
   ConnectionState.Disconnected => UIConstants.StatusColors.Disconnected,
     ConnectionState.Connecting => UIConstants.StatusColors.Connecting,
    ConnectionState.Error => UIConstants.StatusColors.Error,
     _ => UIConstants.StatusColors.Disconnected
   };
 }

     public static void SetButtonDefault(Button button)
 {
button.BackColor = UIConstants.ButtonColors.Default;
    button.ForeColor = SystemColors.ControlText;
 button.Font = _regularFont;
 }

 public static void SetButtonDisabled(Button button)
        {
  button.BackColor = UIConstants.ButtonColors.Disabled;
   button.ForeColor = SystemColors.ControlText;
       button.Font = _regularFont;
   button.Enabled = false;
      }

   public static void SetButtonActive(Button button, DevicePowerState powerState)
  {
      button.Enabled = true;
   button.Font = _boldFont;
    button.ForeColor = UIConstants.TextColors.ButtonActive;

        button.BackColor = powerState switch
     {
     DevicePowerState.On => UIConstants.ButtonColors.PowerOn,
      DevicePowerState.Off => UIConstants.ButtonColors.PowerOff,
     _ => UIConstants.ButtonColors.Default
      };
 }

 public static void UpdateConnectButton(Button button, bool isConnected)
    {
     if (isConnected)
   {
     button.Text = "断开连接";
   button.BackColor = UIConstants.ButtonColors.Connected;
   }
   else
   {
       button.Text = "连接";
   button.BackColor = SystemColors.Control;
    }
   }
 }
}

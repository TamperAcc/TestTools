using System;
using System.Drawing;
using System.Windows.Forms;
using TestTool.Core.Models;
using TestTool.Forms.Base;
using TestTool.Infrastructure.Constants;

namespace TestTool
{
    public class SerialMonitorForm : ResizableFormBase
    {
        private readonly RichTextBox _rtbLog;

        public SerialMonitorForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(UIFormConstants.MonitorForm.DefaultWidth, UIFormConstants.MonitorForm.DefaultHeight);
            MinimumSize = new Size(UIFormConstants.MonitorForm.MinWidth, UIFormConstants.MonitorForm.MinHeight);

            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font(FontFamily.GenericMonospace, 10f),
                DetectUrls = false
            };
            Controls.Add(_rtbLog);
        }

        public void ApplyPosition(MonitorWindowPosition? position)
        {
            ApplyWindowPosition(position, FormStartPosition.CenterParent);
        }

        public MonitorWindowPosition GetCurrentPosition()
        {
            return GetCurrentWindowPosition();
        }

        public void AppendLine(string text) => AppendReceived(text);

        public void AppendReceived(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => AppendReceived(text))); return; }
            var time = DateTime.Now.ToString("HH:mm:ss");
            AppendColoredText("[" + time + "] ", Color.Gray);
            AppendColoredText(text + Environment.NewLine, Color.Lime);
            ScrollToEnd();
        }

        public void AppendSent(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => AppendSent(text))); return; }
            var time = DateTime.Now.ToString("HH:mm:ss");
            AppendColoredText("[" + time + "] ", Color.Gray);
            AppendColoredText("[SENT] " + text + Environment.NewLine, Color.Cyan);
            ScrollToEnd();
        }

        private void AppendColoredText(string text, Color color)
        {
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(text);
            _rtbLog.SelectionColor = _rtbLog.ForeColor;
        }

        private void ScrollToEnd()
        {
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.ScrollToCaret();
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Forms.Base;
using TestTool.Infrastructure.Constants;

namespace TestTool
{
    /// <summary>
    /// 设备打印窗口：支持拖拽合并到总监视窗。
    /// </summary>
    public class DeviceMonitorForm : ResizableFormBase
    {
        private readonly RichTextBox _rtbLog;
        private readonly Panel _dropHint;
        private bool _isDragging;
        private Point _dragStart;

        public DeviceType DeviceType { get; }

        /// <summary>
        /// 当其他打印窗口拖放到本窗口时触发（source,target）。
        /// </summary>
        public event Action<DeviceMonitorForm, DeviceMonitorForm>? MonitorDroppedOnMe;

        public DeviceMonitorForm(DeviceType deviceType, string title)
        {
            DeviceType = deviceType;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(UIFormConstants.MonitorForm.DefaultWidth, UIFormConstants.MonitorForm.DefaultHeight);
            MinimumSize = new Size(UIFormConstants.MonitorForm.MinWidth, UIFormConstants.MonitorForm.MinHeight);
            AllowDrop = true;

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

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            _rtbLog.MouseDown += OnMouseDown;
            _rtbLog.MouseMove += OnMouseMove;
            _rtbLog.MouseUp += OnMouseUp;

            // 让日志区域也响应拖放
            _rtbLog.AllowDrop = true;
            _rtbLog.DragEnter += OnDragEnter;
            _rtbLog.DragDrop += OnDragDrop;
            _rtbLog.DragOver += OnDragOver;
            _rtbLog.DragLeave += OnDragLeave;

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            DragOver += OnDragOver;
            DragLeave += OnDragLeave;

            _dropHint = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(120, 135, 206, 250), // 更高透明度的浅蓝
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            var hintLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "释放以合并到总监视窗",
                BackColor = Color.FromArgb(180, 70, 130, 180),
                ForeColor = Color.White,
                Font = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold)
            };
            _dropHint.Controls.Add(hintLabel);
            Controls.Add(_dropHint);
            _dropHint.BringToFront();
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

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStart = e.Location;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging || e.Button != MouseButtons.Left)
                return;

            if (!TopLevel)
                return; // 嵌入 Host 时不触发拖拽

            var dx = Math.Abs(e.X - _dragStart.X);
            var dy = Math.Abs(e.Y - _dragStart.Y);
            if (dx >= SystemInformation.DragSize.Width || dy >= SystemInformation.DragSize.Height)
            {
                _isDragging = false;
                DoDragDrop(this, DragDropEffects.Move);
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
            {
                var src = e.Data.GetData(typeof(DeviceMonitorForm)) as DeviceMonitorForm;
                if (src != null && src != this)
                {
                    e.Effect = DragDropEffects.Move;
                    ShowDropHint(true);
                }
            }
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
                return;

            if (e.Data.GetData(typeof(DeviceMonitorForm)) is DeviceMonitorForm src && src != this)
            {
                MonitorDroppedOnMe?.Invoke(src, this);
            }

            ShowDropHint(false);
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
            {
                var src = e.Data.GetData(typeof(DeviceMonitorForm)) as DeviceMonitorForm;
                if (src != null && src != this)
                {
                    e.Effect = DragDropEffects.Move;
                    ShowDropHint(true);
                }
            }
        }

        private void OnDragLeave(object? sender, EventArgs e)
        {
            ShowDropHint(false);
        }

        private void ShowDropHint(bool visible)
        {
            _dropHint.Visible = visible;
            if (visible)
            {
                _dropHint.BringToFront();
            }
        }
    }
}

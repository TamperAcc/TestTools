using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Forms.Base;
using TestTool.Infrastructure.Constants;

namespace TestTool
{
    /// <summary>
    /// 总监视窗：承载各设备的打印窗口 Tab，支持拖入合并与弹出。
    /// </summary>
    public class SerialMonitorHostForm : ResizableFormBase
    {
        /// <summary>
        /// Host 唯一标识（持久化使用）。
        /// </summary>
        public string HostId { get; set; } = Guid.NewGuid().ToString("N");

        private readonly TabControl _tabControl;
        private readonly Panel _dropHint;
        private readonly Dictionary<DeviceType, DeviceMonitorForm> _tabForms = new();

        /// <summary>
        /// 当设备打印窗口被合并到 Host 时触发。
        /// </summary>
        public event Action<DeviceType, SerialMonitorHostForm>? MonitorMerged;

        /// <summary>
        /// 当设备打印窗口从 Host 弹出为独立窗体时触发。
        /// </summary>
        public event Action<DeviceType, DeviceMonitorForm, SerialMonitorHostForm>? MonitorPopped;

        public SerialMonitorHostForm()
        {
            Text = "打印总监视窗";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(UIFormConstants.MonitorForm.DefaultWidth + 200, UIFormConstants.MonitorForm.DefaultHeight + 120);
            MinimumSize = new Size(UIFormConstants.MonitorForm.MinWidth + 100, UIFormConstants.MonitorForm.MinHeight + 80);
            AllowDrop = true;

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 4),
                AllowDrop = true
            };
            _tabControl.MouseUp += OnTabMouseUp;
            _tabControl.DragEnter += OnHostDragEnter;
            _tabControl.DragDrop += OnHostDragDrop;
            _tabControl.DragOver += OnHostDragOver;
            _tabControl.DragLeave += OnHostDragLeave;

            Controls.Add(_tabControl);

            DragEnter += OnHostDragEnter;
            DragDrop += OnHostDragDrop;
            DragOver += OnHostDragOver;
            DragLeave += OnHostDragLeave;
            AllowDrop = true;

            _dropHint = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(120, 135, 206, 250),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            var hintLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "释放以合并到标签页",
                BackColor = Color.FromArgb(180, 70, 130, 180),
                ForeColor = Color.White,
                Font = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold)
            };
            _dropHint.Controls.Add(hintLabel);
            Controls.Add(_dropHint);
            _dropHint.BringToFront();
        }

        public bool TryGetMonitor(DeviceType deviceType, out DeviceMonitorForm? monitor)
        {
            if (_tabForms.TryGetValue(deviceType, out var form) && !form.IsDisposed)
            {
                monitor = form;
                return true;
            }

            monitor = null;
            return false;
        }

        /// <summary>
        /// 将独立窗口合并到 Host 中（或确保已存在）。
        /// </summary>
        public void AttachMonitor(DeviceMonitorForm monitor)
        {
            if (monitor == null || monitor.IsDisposed)
                return;

            var deviceType = monitor.DeviceType;
            if (_tabForms.ContainsKey(deviceType))
            {
                ActivateTab(deviceType);
                EnsureVisibleAndActive();
                return;
            }

            monitor.TopLevel = false;
            monitor.FormBorderStyle = FormBorderStyle.None;
            monitor.Dock = DockStyle.Fill;
            monitor.Visible = true;

            var page = new TabPage(monitor.Text) { Tag = deviceType };
            page.Controls.Add(monitor);
            _tabControl.TabPages.Add(page);
            _tabForms[deviceType] = monitor;

            _tabControl.SelectedTab = page;
            EnsureVisibleAndActive();
             MonitorMerged?.Invoke(deviceType, this);
        }

        /// <summary>
        /// 从 Host 弹出指定设备的监视器为独立窗体。
        /// </summary>
        public void PopOut(DeviceType deviceType)
        {
            if (!_tabForms.TryGetValue(deviceType, out var monitor))
                return;

            var page = _tabControl.TabPages.Cast<TabPage>().FirstOrDefault(t => Equals(t.Tag, deviceType));
            if (page != null)
            {
                // 先从 TabPage 移除监视器，解除父子关系，再调整 TopLevel
                page.Controls.Remove(monitor);
                _tabControl.TabPages.Remove(page);
                page.Dispose();
            }

            _tabForms.Remove(deviceType);

            // 解除父子关系后再设置 TopLevel，避免 SetTopLevelInternal 异常
            monitor.Parent = null;
            monitor.TopLevel = true;
            monitor.FormBorderStyle = FormBorderStyle.Sizable;
            monitor.Dock = DockStyle.None;
            monitor.Show();

            MonitorPopped?.Invoke(deviceType, monitor, this);

            // 如果剩余标签<=1，并且当前未处于关闭流程，则弹出剩余并关闭 Host
            if (!_isClosing && _tabForms.Count <= 1)
            {
                _isClosing = true;
                foreach (var remaining in _tabForms.Keys.ToArray())
                {
                    PopOut(remaining);
                }
                if (!IsDisposed)
                {
                    Close();
                }
                _isClosing = false;
            }
        }

        /// <summary>
        /// 关闭 Host 时，先关闭所有嵌入的监视器（直接退出，不再弹出到独立窗口）。
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                foreach (var monitor in _tabForms.Values.ToArray())
                {
                    if (monitor != null && !monitor.IsDisposed)
                    {
                        monitor.Close();
                    }
                }
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// 移除所有 Tab，但不释放监视器窗体。
        /// </summary>
        public void ClearTabs()
        {
            foreach (var kv in _tabForms.ToArray())
            {
                PopOut(kv.Key);
            }
        }

        /// <summary>
        /// 获取当前窗口位置。
        /// </summary>
        public MonitorWindowPosition GetCurrentPosition()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                return GetCurrentWindowPosition();
            }

            var bounds = this.RestoreBounds;
            return new MonitorWindowPosition(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// 应用保存的位置。
        /// </summary>
        public void ApplyPosition(MonitorWindowPosition? position)
        {
            ApplyWindowPosition(position, FormStartPosition.CenterParent);
        }

        private void ActivateTab(DeviceType deviceType)
        {
            var page = _tabControl.TabPages.Cast<TabPage>().FirstOrDefault(t => Equals(t.Tag, deviceType));
            if (page != null)
            {
                _tabControl.SelectedTab = page;
            }
        }

        private void OnHostDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
            {
                e.Effect = DragDropEffects.Move;
                ShowDropHint(true);
            }
        }

        private void OnHostDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void OnHostDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(DeviceMonitorForm)))
                return;

            if (e.Data.GetData(typeof(DeviceMonitorForm)) is DeviceMonitorForm form)
            {
                AttachMonitor(form);
            }

            ShowDropHint(false);
        }

        private void OnTabMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                var rect = _tabControl.GetTabRect(i);
                if (rect.Contains(e.Location))
                {
                    var page = _tabControl.TabPages[i];
                    if (page.Tag is DeviceType deviceType)
                    {
                        PopOut(deviceType);
                    }
                    break;
                }
            }
        }

        private void OnHostDragLeave(object? sender, EventArgs e)
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

        private bool _isClosing;

        private void EnsureVisibleAndActive()
        {
            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            BringToFront();
            Activate();
        }
    }
}

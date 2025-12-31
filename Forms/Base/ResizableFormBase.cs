using System;
using System.Drawing;
using System.Windows.Forms;
using TestTool.Business.Models;
using TestTool.Infrastructure.Constants;

namespace TestTool.Forms.Base
{
    /// <summary>
    /// 可调整大小的窗体基类：提供边缘拖拽调整大小和屏幕边界检查功能
    /// </summary>
    public class ResizableFormBase : Form
    {
        /// <summary>
        /// 窗口边缘感应范围（像素）
        /// </summary>
        protected virtual int ResizeHandleSize => AppConstants.UI.ResizeHandleSize;

        /// <summary>
        /// 重写 WndProc 以支持边缘拖拽调整窗口大小
        /// </summary>
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

                if (cursor.X <= ResizeHandleSize && cursor.Y <= ResizeHandleSize)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (cursor.X >= this.ClientSize.Width - ResizeHandleSize && cursor.Y <= ResizeHandleSize)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (cursor.X <= ResizeHandleSize && cursor.Y >= this.ClientSize.Height - ResizeHandleSize)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (cursor.X >= this.ClientSize.Width - ResizeHandleSize && cursor.Y >= this.ClientSize.Height - ResizeHandleSize)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (cursor.X <= ResizeHandleSize)
                    m.Result = (IntPtr)HTLEFT;
                else if (cursor.X >= this.ClientSize.Width - ResizeHandleSize)
                    m.Result = (IntPtr)HTRIGHT;
                else if (cursor.Y <= ResizeHandleSize)
                    m.Result = (IntPtr)HTTOP;
                else if (cursor.Y >= this.ClientSize.Height - ResizeHandleSize)
                    m.Result = (IntPtr)HTBOTTOM;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// 应用保存的窗口位置
        /// </summary>
        /// <param name="position">窗口位置配置</param>
        /// <param name="fallbackStartPosition">位置无效时的备用起始位置</param>
        protected void ApplyWindowPosition(MonitorWindowPosition? position, FormStartPosition fallbackStartPosition = FormStartPosition.CenterParent)
        {
            if (position == null || !position.IsValid)
                return;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(position.X, position.Y);
            this.Size = new Size(position.Width, position.Height);

            // 确保窗口在屏幕可见范围内
            EnsureVisibleOnScreen(fallbackStartPosition);
        }

        /// <summary>
        /// 获取当前窗口位置配置
        /// </summary>
        /// <returns>窗口位置配置对象</returns>
        protected MonitorWindowPosition GetCurrentWindowPosition()
        {
            // 只有在正常窗口状态下才返回有效位置
            if (this.WindowState == FormWindowState.Normal)
            {
                return new MonitorWindowPosition(
                    this.Location.X,
                    this.Location.Y,
                    this.Size.Width,
                    this.Size.Height);
            }

            // 最大化或最小化时返回空位置
            return new MonitorWindowPosition();
        }

        /// <summary>
        /// 确保窗口在屏幕可见范围内
        /// </summary>
        /// <param name="fallbackStartPosition">完全不可见时的备用起始位置</param>
        protected void EnsureVisibleOnScreen(FormStartPosition fallbackStartPosition = FormStartPosition.CenterScreen)
        {
            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;

            // 如果窗口完全在屏幕外，移动到可见位置
            if (Left >= workingArea.Right || Right <= workingArea.Left ||
                Top >= workingArea.Bottom || Bottom <= workingArea.Top)
            {
                this.StartPosition = fallbackStartPosition;
                return;
            }

            // 调整位置使窗口完全可见
            if (Right > workingArea.Right)
                Left = workingArea.Right - Width;
            if (Bottom > workingArea.Bottom)
                Top = workingArea.Bottom - Height;
            if (Left < workingArea.Left)
                Left = workingArea.Left;
            if (Top < workingArea.Top)
                Top = workingArea.Top;
        }
    }
}

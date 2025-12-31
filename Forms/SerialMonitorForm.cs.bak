using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestTool
{
    /// <summary>
    /// 串口打印监视窗口：显示接收与发送日志并区分颜色
    /// </summary>
    public class SerialMonitorForm : Form
    {
        // 日志展示区，支持彩色文本
        private readonly RichTextBox _rtbLog;

        // 构造函数：初始化窗体和日志控件
        public SerialMonitorForm(string title)
        {
            // 设置窗体标题
            Text = title;
            // 将窗体显示位置设为相对于父窗体的居中
            StartPosition = FormStartPosition.CenterParent;
            // 设置窗体默认大小为 700x400
            Size = new Size(700, 400);

            // 创建并配置 RichTextBox，用作日志显示区
            _rtbLog = new RichTextBox
            {
                // 填充整个窗体客户端区域
                Dock = DockStyle.Fill,
                // 启用多行显示
                Multiline = true,
                // 只读，用户不能直接编辑日志内容
                ReadOnly = true,
                // 垂直滚动条
                ScrollBars = RichTextBoxScrollBars.Vertical,
                // 不自动换行，以便按字节/字符显示原始数据
                WordWrap = false,
                // 背景设为黑色，便于仿真终端外观
                BackColor = Color.Black,
                // 前景色设为荧光绿，作为默认接收文本颜色
                ForeColor = Color.Lime,
                // 使用等宽字体以便对齐显示十六进制或列数据
                Font = new Font(FontFamily.GenericMonospace, 10f),
                // 禁止自动识别 URL，保持原始文本不变色
                DetectUrls = false
            };

            // 将 RichTextBox 添加到窗体控件集合中，使其可见并参与布局
            Controls.Add(_rtbLog);
        }

        // 将接收文本按行追加（兼容旧方法名）
        public void AppendLine(string text) => AppendReceived(text);

        // 将接收到的文本追加到日志中（绿色）
        public void AppendReceived(string text)
        {
            // 如果窗体已被释放则直接返回，避免访问托管资源导致异常
            if (IsDisposed)
            {
                return;
            }

            // 如果调用发生在非 UI 线程，使用 BeginInvoke 切换到 UI 线程执行
            if (InvokeRequired)
            {
                // 使用 BeginInvoke 异步排队调用，避免死锁
                BeginInvoke(new Action(() => AppendReceived(text)));
                return;
            }

            // 获取当前时间字符串（时:分:秒），用于时间戳
            var time = DateTime.Now.ToString("HH:mm:ss");
            // 格式化时间前缀，例如 "[12:34:56] "
            var line = $"[{time}] ";
            // 先追加时间前缀，使用灰色显示以和内容区分
            AppendColoredText(line, Color.Gray);
            // 再追加实际接收到的文本，使用绿色显示，并换行
            AppendColoredText(text + Environment.NewLine, Color.Lime);
            // 将光标滚动到日志末尾，确保最新行可见
            ScrollToEnd();
        }

        // 将发送的命令追加到日志中（青色，并带 [SENT] 前缀）
        public void AppendSent(string text)
        {
            // 如果窗体已被释放则直接返回，避免访问托管资源导致异常
            if (IsDisposed)
            {
                return;
            }

            // 如需在非 UI 线程调用，切换到 UI 线程
            if (InvokeRequired)
            {
                // 使用 BeginInvoke 异步调用以避免阻塞
                BeginInvoke(new Action(() => AppendSent(text)));
                return;
            }

            // 获取当前时间字符串，用于时间戳
            var time = DateTime.Now.ToString("HH:mm:ss");
            // 格式化时间前缀
            var line = $"[{time}] ";
            // 追加灰色时间前缀
            AppendColoredText(line, Color.Gray);
            // 追加发送标记和命令文本，使用青色，使其与接收内容区分开
            AppendColoredText("[SENT] " + text + Environment.NewLine, Color.Cyan);
            // 滚动到末尾
            ScrollToEnd();
        }

        // 内部方法：在 RichTextBox 末尾追加带颜色的文本
        private void AppendColoredText(string text, Color color)
        {
            // 将选择起始位置设置为当前文本末尾（在末尾插入）
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            // 确保没有选中文本
            _rtbLog.SelectionLength = 0;
            // 设置当前选择的文本颜色
            _rtbLog.SelectionColor = color;
            // 将文本追加到 RichTextBox
            _rtbLog.AppendText(text);
            // 恢复选择颜色为默认前景色，避免影响后续文本的默认颜色
            _rtbLog.SelectionColor = _rtbLog.ForeColor;
        }

        // 将光标移动到文本末尾并滚动，使最新内容可见
        private void ScrollToEnd()
        {
            // 设置光标位置到文本末尾
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            // 滚动到当前插入点
            _rtbLog.ScrollToCaret();
        }
    }
}

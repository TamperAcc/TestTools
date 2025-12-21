namespace TestTool
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblPortSelect = new Label();
            cmbSettingsPort = new ComboBox();
            lblBaudRate = new Label();
            cmbBaudRate = new ComboBox();
            btnLockPort = new Button();
            btnMonitor = new Button();
            btnOK = new Button();
            btnCancel = new Button();
            groupBox1 = new GroupBox();
            lblMessage = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // lblPortSelect
            // 
            lblPortSelect.AutoSize = true;
            lblPortSelect.Location = new Point(20, 35);
            lblPortSelect.Name = "lblPortSelect";
            lblPortSelect.Size = new Size(69, 20);
            lblPortSelect.TabIndex = 0;
            lblPortSelect.Text = "串口号：";
            // 
            // cmbSettingsPort
            // 
            cmbSettingsPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbSettingsPort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSettingsPort.FormattingEnabled = true;
            cmbSettingsPort.Location = new Point(95, 32);
            cmbSettingsPort.Name = "cmbSettingsPort";
            cmbSettingsPort.Size = new Size(150, 28);
            cmbSettingsPort.TabIndex = 1;
            // 
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Location = new Point(20, 75);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(69, 20);
            lblBaudRate.TabIndex = 7;
            lblBaudRate.Text = "波特率：";
            // 
            // cmbBaudRate
            // 
            cmbBaudRate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBaudRate.FormattingEnabled = true;
            cmbBaudRate.Location = new Point(95, 72);
            cmbBaudRate.Name = "cmbBaudRate";
            cmbBaudRate.Size = new Size(150, 28);
            cmbBaudRate.TabIndex = 8;
            // 
            // btnLockPort
            // 
            btnLockPort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLockPort.Location = new Point(260, 30);
            btnLockPort.Name = "btnLockPort";
            btnLockPort.Size = new Size(80, 32);
            btnLockPort.TabIndex = 2;
            btnLockPort.Text = "未锁定";
            btnLockPort.UseVisualStyleBackColor = true;
            btnLockPort.Click += btnLockPort_Click;
            // 
            // btnMonitor
            // 
            btnMonitor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMonitor.Location = new Point(260, 70);
            btnMonitor.Name = "btnMonitor";
            btnMonitor.Size = new Size(80, 32);
            btnMonitor.TabIndex = 6;
            btnMonitor.Text = "打开打印";
            btnMonitor.UseVisualStyleBackColor = true;
            btnMonitor.Click += btnMonitor_Click;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(100, 200);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 35);
            btnOK.TabIndex = 3;
            btnOK.Text = "确定";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(210, 200);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 35);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "取消";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnMonitor);
            groupBox1.Controls.Add(lblMessage);
            groupBox1.Controls.Add(lblPortSelect);
            groupBox1.Controls.Add(cmbSettingsPort);
            groupBox1.Controls.Add(lblBaudRate);
            groupBox1.Controls.Add(cmbBaudRate);
            groupBox1.Controls.Add(btnLockPort);
            groupBox1.Location = new Point(20, 20);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(360, 160);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "连接设置";
            // 
            // lblMessage
            // 
            lblMessage.AutoSize = true;
            lblMessage.ForeColor = SystemColors.ControlDarkDark;
            lblMessage.Location = new Point(20, 120);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(0, 20);
            lblMessage.TabIndex = 3;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 260);
            Controls.Add(groupBox1);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(400, 260);
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "设置";
            Load += SettingsForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lblPortSelect;
        private ComboBox cmbSettingsPort;
        private Label lblBaudRate;
        private ComboBox cmbBaudRate;
        private Button btnLockPort;
        private Button btnMonitor;
        private Button btnOK;
        private Button btnCancel;
        private GroupBox groupBox1;
        private Label lblMessage;
    }
}

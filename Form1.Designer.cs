namespace WinFormsApp3
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnConnect = new Button();
            txtOutput = new TextBox();
            lblStatus = new Label();
            cmbPortName = new ComboBox();
            lblPort = new Label();
            btnLock = new Button();
            btnRefresh = new Button();
            btnClear = new Button();
            SuspendLayout();
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnect.Location = new Point(668, 20);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 35);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "连接";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // txtOutput
            // 
            txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtOutput.Location = new Point(12, 70);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(776, 328);
            txtOutput.TabIndex = 1;
            txtOutput.ReadOnly = true;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 27);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(109, 20);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "状态: 未连接";
            // 
            // cmbPortName
            // 
            cmbPortName.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPortName.FormattingEnabled = true;
            cmbPortName.Location = new Point(310, 24);
            cmbPortName.Name = "cmbPortName";
            cmbPortName.Size = new Size(120, 28);
            cmbPortName.TabIndex = 3;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(240, 27);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(64, 20);
            lblPort.TabIndex = 4;
            lblPort.Text = "串口号:";
            // 
            // btnLock
            // 
            btnLock.Location = new Point(440, 22);
            btnLock.Name = "btnLock";
            btnLock.Size = new Size(70, 32);
            btnLock.TabIndex = 5;
            btnLock.Text = "未锁定";
            btnLock.UseVisualStyleBackColor = true;
            btnLock.Click += btnLock_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(520, 22);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(70, 32);
            btnRefresh.TabIndex = 6;
            btnRefresh.Text = "刷新";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnClear
            // 
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.Location = new Point(668, 405);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(120, 35);
            btnClear.TabIndex = 7;
            btnClear.Text = "清空输出";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnClear);
            Controls.Add(btnRefresh);
            Controls.Add(btnLock);
            Controls.Add(lblPort);
            Controls.Add(cmbPortName);
            Controls.Add(lblStatus);
            Controls.Add(txtOutput);
            Controls.Add(btnConnect);
            Name = "Form1";
            Text = "串口通信工具";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnConnect;
        private TextBox txtOutput;
        private Label lblStatus;
        private ComboBox cmbPortName;
        private Label lblPort;
        private Button btnLock;
        private Button btnRefresh;
        private Button btnClear;
    }
}

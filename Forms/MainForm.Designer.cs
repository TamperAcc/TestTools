namespace TestTool
{
    partial class MainForm
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
            menuStrip1 = new MenuStrip();
            menuSettings = new ToolStripMenuItem();
            menuDeviceSettings = new ToolStripMenuItem();

            // FCC1 控件
            lblStatusFCC1 = new Label();
            btnConnectFCC1 = new Button();
            btnOnFCC1 = new Button();
            btnOffFCC1 = new Button();

            // FCC2 控件
            lblStatusFCC2 = new Label();
            btnConnectFCC2 = new Button();
            btnOnFCC2 = new Button();
            btnOffFCC2 = new Button();

            // FCC3 控件
            lblStatusFCC3 = new Label();
            btnConnectFCC3 = new Button();
            btnOnFCC3 = new Button();
            btnOffFCC3 = new Button();

            // HIL 控件
            lblStatusHIL = new Label();
            btnConnectHIL = new Button();
            btnOnHIL = new Button();
            btnOffHIL = new Button();

            menuStrip1.SuspendLayout();
            SuspendLayout();

            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuSettings });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(600, 25);
            menuStrip1.TabIndex = 0;

            // 
            // menuSettings
            // 
            menuSettings.DropDownItems.AddRange(new ToolStripItem[] { menuDeviceSettings });
            menuSettings.Name = "menuSettings";
            menuSettings.Size = new Size(44, 21);
            menuSettings.Text = "设置";

            // 
            // menuDeviceSettings
            // 
            menuDeviceSettings.Name = "menuDeviceSettings";
            menuDeviceSettings.Size = new Size(148, 22);
            menuDeviceSettings.Text = "串口设置";
            menuDeviceSettings.Click += menuSettings_Click;

            // 行高和起始位置
            int rowHeight = 45;
            int startY = 30;
            int labelWidth = 260;
            int buttonWidth = 70;
            int connectWidth = 80;

            // ========== FCC1 行 ==========
            // 
            // lblStatusFCC1
            // 
            lblStatusFCC1.AutoSize = false;
            lblStatusFCC1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatusFCC1.Location = new Point(12, startY);
            lblStatusFCC1.Name = "lblStatusFCC1";
            lblStatusFCC1.Size = new Size(labelWidth, 35);
            lblStatusFCC1.TabIndex = 1;
            lblStatusFCC1.Text = "FCC1电源 - 未连接";
            lblStatusFCC1.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusFCC1.BorderStyle = BorderStyle.FixedSingle;

            // 
            // btnOnFCC1
            // 
            btnOnFCC1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOnFCC1.Location = new Point(280, startY + 2);
            btnOnFCC1.Name = "btnOnFCC1";
            btnOnFCC1.Size = new Size(buttonWidth, 30);
            btnOnFCC1.TabIndex = 2;
            btnOnFCC1.Text = "ON";
            btnOnFCC1.UseVisualStyleBackColor = true;
            btnOnFCC1.Click += btnOnFCC1_Click;

            // 
            // btnOffFCC1
            // 
            btnOffFCC1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOffFCC1.Location = new Point(355, startY + 2);
            btnOffFCC1.Name = "btnOffFCC1";
            btnOffFCC1.Size = new Size(buttonWidth, 30);
            btnOffFCC1.TabIndex = 3;
            btnOffFCC1.Text = "OFF";
            btnOffFCC1.UseVisualStyleBackColor = true;
            btnOffFCC1.Click += btnOffFCC1_Click;

            // 
            // btnConnectFCC1
            // 
            btnConnectFCC1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnectFCC1.Location = new Point(508, startY + 2);
            btnConnectFCC1.Name = "btnConnectFCC1";
            btnConnectFCC1.Size = new Size(connectWidth, 30);
            btnConnectFCC1.TabIndex = 4;
            btnConnectFCC1.Text = "连接";
            btnConnectFCC1.UseVisualStyleBackColor = true;
            btnConnectFCC1.Click += btnConnectFCC1_Click;

            // ========== FCC2 行 ==========
            int row2Y = startY + rowHeight;

            // 
            // lblStatusFCC2
            // 
            lblStatusFCC2.AutoSize = false;
            lblStatusFCC2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatusFCC2.Location = new Point(12, row2Y);
            lblStatusFCC2.Name = "lblStatusFCC2";
            lblStatusFCC2.Size = new Size(labelWidth, 35);
            lblStatusFCC2.TabIndex = 5;
            lblStatusFCC2.Text = "FCC2电源 - 未连接";
            lblStatusFCC2.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusFCC2.BorderStyle = BorderStyle.FixedSingle;

            // 
            // btnOnFCC2
            // 
            btnOnFCC2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOnFCC2.Location = new Point(280, row2Y + 2);
            btnOnFCC2.Name = "btnOnFCC2";
            btnOnFCC2.Size = new Size(buttonWidth, 30);
            btnOnFCC2.TabIndex = 6;
            btnOnFCC2.Text = "ON";
            btnOnFCC2.UseVisualStyleBackColor = true;
            btnOnFCC2.Click += btnOnFCC2_Click;

            // 
            // btnOffFCC2
            // 
            btnOffFCC2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOffFCC2.Location = new Point(355, row2Y + 2);
            btnOffFCC2.Name = "btnOffFCC2";
            btnOffFCC2.Size = new Size(buttonWidth, 30);
            btnOffFCC2.TabIndex = 7;
            btnOffFCC2.Text = "OFF";
            btnOffFCC2.UseVisualStyleBackColor = true;
            btnOffFCC2.Click += btnOffFCC2_Click;

            // 
            // btnConnectFCC2
            // 
            btnConnectFCC2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnectFCC2.Location = new Point(508, row2Y + 2);
            btnConnectFCC2.Name = "btnConnectFCC2";
            btnConnectFCC2.Size = new Size(connectWidth, 30);
            btnConnectFCC2.TabIndex = 8;
            btnConnectFCC2.Text = "连接";
            btnConnectFCC2.UseVisualStyleBackColor = true;
            btnConnectFCC2.Click += btnConnectFCC2_Click;

            // ========== FCC3 行 ==========
            int row3Y = startY + rowHeight * 2;

            // 
            // lblStatusFCC3
            // 
            lblStatusFCC3.AutoSize = false;
            lblStatusFCC3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatusFCC3.Location = new Point(12, row3Y);
            lblStatusFCC3.Name = "lblStatusFCC3";
            lblStatusFCC3.Size = new Size(labelWidth, 35);
            lblStatusFCC3.TabIndex = 9;
            lblStatusFCC3.Text = "FCC3电源 - 未连接";
            lblStatusFCC3.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusFCC3.BorderStyle = BorderStyle.FixedSingle;

            // 
            // btnOnFCC3
            // 
            btnOnFCC3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOnFCC3.Location = new Point(280, row3Y + 2);
            btnOnFCC3.Name = "btnOnFCC3";
            btnOnFCC3.Size = new Size(buttonWidth, 30);
            btnOnFCC3.TabIndex = 10;
            btnOnFCC3.Text = "ON";
            btnOnFCC3.UseVisualStyleBackColor = true;
            btnOnFCC3.Click += btnOnFCC3_Click;

            // 
            // btnOffFCC3
            // 
            btnOffFCC3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOffFCC3.Location = new Point(355, row3Y + 2);
            btnOffFCC3.Name = "btnOffFCC3";
            btnOffFCC3.Size = new Size(buttonWidth, 30);
            btnOffFCC3.TabIndex = 11;
            btnOffFCC3.Text = "OFF";
            btnOffFCC3.UseVisualStyleBackColor = true;
            btnOffFCC3.Click += btnOffFCC3_Click;

            // 
            // btnConnectFCC3
            // 
            btnConnectFCC3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnectFCC3.Location = new Point(508, row3Y + 2);
            btnConnectFCC3.Name = "btnConnectFCC3";
            btnConnectFCC3.Size = new Size(connectWidth, 30);
            btnConnectFCC3.TabIndex = 12;
            btnConnectFCC3.Text = "连接";
            btnConnectFCC3.UseVisualStyleBackColor = true;
            btnConnectFCC3.Click += btnConnectFCC3_Click;

            // ========== HIL 行 ==========
            int row4Y = startY + rowHeight * 3;

            // 
            // lblStatusHIL
            // 
            lblStatusHIL.AutoSize = false;
            lblStatusHIL.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatusHIL.Location = new Point(12, row4Y);
            lblStatusHIL.Name = "lblStatusHIL";
            lblStatusHIL.Size = new Size(labelWidth, 35);
            lblStatusHIL.TabIndex = 13;
            lblStatusHIL.Text = "HIL电源 - 未连接";
            lblStatusHIL.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusHIL.BorderStyle = BorderStyle.FixedSingle;

            // 
            // btnOnHIL
            // 
            btnOnHIL.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOnHIL.Location = new Point(280, row4Y + 2);
            btnOnHIL.Name = "btnOnHIL";
            btnOnHIL.Size = new Size(buttonWidth, 30);
            btnOnHIL.TabIndex = 14;
            btnOnHIL.Text = "ON";
            btnOnHIL.UseVisualStyleBackColor = true;
            btnOnHIL.Click += btnOnHIL_Click;

            // 
            // btnOffHIL
            // 
            btnOffHIL.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOffHIL.Location = new Point(355, row4Y + 2);
            btnOffHIL.Name = "btnOffHIL";
            btnOffHIL.Size = new Size(buttonWidth, 30);
            btnOffHIL.TabIndex = 15;
            btnOffHIL.Text = "OFF";
            btnOffHIL.UseVisualStyleBackColor = true;
            btnOffHIL.Click += btnOffHIL_Click;

            // 
            // btnConnectHIL
            // 
            btnConnectHIL.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnectHIL.Location = new Point(508, row4Y + 2);
            btnConnectHIL.Name = "btnConnectHIL";
            btnConnectHIL.Size = new Size(connectWidth, 30);
            btnConnectHIL.TabIndex = 16;
            btnConnectHIL.Text = "连接";
            btnConnectHIL.UseVisualStyleBackColor = true;
            btnConnectHIL.Click += btnConnectHIL_Click;

            // 
            // MainForm
            // 
            ClientSize = new Size(600, 220);
            Controls.Add(menuStrip1);
            // FCC1
            Controls.Add(lblStatusFCC1);
            Controls.Add(btnOnFCC1);
            Controls.Add(btnOffFCC1);
            Controls.Add(btnConnectFCC1);
            // FCC2
            Controls.Add(lblStatusFCC2);
            Controls.Add(btnOnFCC2);
            Controls.Add(btnOffFCC2);
            Controls.Add(btnConnectFCC2);
            // FCC3
            Controls.Add(lblStatusFCC3);
            Controls.Add(btnOnFCC3);
            Controls.Add(btnOffFCC3);
            Controls.Add(btnConnectFCC3);
            // HIL
            Controls.Add(lblStatusHIL);
            Controls.Add(btnOnHIL);
            Controls.Add(btnOffHIL);
            Controls.Add(btnConnectHIL);

            FormBorderStyle = FormBorderStyle.Sizable;
            MainMenuStrip = menuStrip1;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(500, 260);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "电源控制工具";
            Load += MainForm_Load;

            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuSettings;
        private ToolStripMenuItem menuDeviceSettings;

        // FCC1 控件
        private Label lblStatusFCC1;
        private Button btnConnectFCC1;
        private Button btnOnFCC1;
        private Button btnOffFCC1;

        // FCC2 控件
        private Label lblStatusFCC2;
        private Button btnConnectFCC2;
        private Button btnOnFCC2;
        private Button btnOffFCC2;

        // FCC3 控件
        private Label lblStatusFCC3;
        private Button btnConnectFCC3;
        private Button btnOnFCC3;
        private Button btnOffFCC3;

        // HIL 控件
        private Label lblStatusHIL;
        private Button btnConnectHIL;
        private Button btnOnHIL;
        private Button btnOffHIL;
    }
}

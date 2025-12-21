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
            btnConnect = new Button();
            lblStatus = new Label();
            menuStrip1 = new MenuStrip();
            menuSettings = new ToolStripMenuItem();
            menuDeviceSettings = new ToolStripMenuItem();
            btnOn = new Button();
            btnOff = new Button();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuSettings });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 25);
            menuStrip1.TabIndex = 8;
            menuStrip1.Text = "menuStrip1";
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
            menuDeviceSettings.Text = "FCC1电源设置";
            menuDeviceSettings.Click += menuSettings_Click;
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnect.Location = new Point(668, 35);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 35);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "连接";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(12, 35);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(250, 30);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "状态: 未连接";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.BorderStyle = BorderStyle.FixedSingle;
            // 
            // btnOn
            // 
            btnOn.Location = new Point(272, 35);
            btnOn.Name = "btnOn";
            btnOn.Size = new Size(70, 30);
            btnOn.TabIndex = 9;
            btnOn.Text = "ON";
            btnOn.UseVisualStyleBackColor = true;
            btnOn.Click += btnOn_Click;
            // 
            // btnOff
            // 
            btnOff.Location = new Point(352, 35);
            btnOff.Name = "btnOff";
            btnOff.Size = new Size(70, 30);
            btnOff.TabIndex = 10;
            btnOff.Text = "OFF";
            btnOff.UseVisualStyleBackColor = true;
            btnOff.Click += btnOff_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(800, 100);
            Controls.Add(btnOff);
            Controls.Add(btnOn);
            Controls.Add(lblStatus);
            Controls.Add(btnConnect);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.Sizable;
            MainMenuStrip = menuStrip1;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(600, 100);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "工具";
            Load += MainForm_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnConnect;
        private Label lblStatus;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuSettings;
        private ToolStripMenuItem menuDeviceSettings;
        private Button btnOn;
        private Button btnOff;
    }
}

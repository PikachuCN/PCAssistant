namespace PCAssistant
{
    partial class Main
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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            txtComputerInfo = new TextBox();
            tabPage2 = new TabPage();
            button3 = new Button();
            button2 = new Button();
            ActivationSystemBtn = new Button();
            tabPage3 = new TabPage();
            groupBox1 = new GroupBox();
            LogTxtbox = new TextBox();
            statusStrip1 = new StatusStrip();
            menuStrip1 = new MenuStrip();
            设置ToolStripMenuItem = new ToolStripMenuItem();
            切换管理权限ToolStripMenuItem = new ToolStripMenuItem();
            服务器设置ToolStripMenuItem = new ToolStripMenuItem();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            groupBox1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Dock = DockStyle.Top;
            tabControl1.Location = new Point(5, 30);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(816, 198);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(txtComputerInfo);
            tabPage1.Location = new Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(808, 168);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "系统基础信息";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtComputerInfo
            // 
            txtComputerInfo.Dock = DockStyle.Fill;
            txtComputerInfo.Location = new Point(3, 3);
            txtComputerInfo.Multiline = true;
            txtComputerInfo.Name = "txtComputerInfo";
            txtComputerInfo.ReadOnly = true;
            txtComputerInfo.ScrollBars = ScrollBars.Vertical;
            txtComputerInfo.Size = new Size(802, 162);
            txtComputerInfo.TabIndex = 1;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(button3);
            tabPage2.Controls.Add(button2);
            tabPage2.Controls.Add(ActivationSystemBtn);
            tabPage2.Location = new Point(4, 26);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(808, 168);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "系统维护";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(282, 6);
            button3.Name = "button3";
            button3.Size = new Size(132, 55);
            button3.TabIndex = 2;
            button3.Text = "我的文档与桌面迁移";
            button3.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(144, 6);
            button2.Name = "button2";
            button2.Size = new Size(132, 55);
            button2.TabIndex = 1;
            button2.Text = "网络稳定性测试";
            button2.UseVisualStyleBackColor = true;
            // 
            // ActivationSystemBtn
            // 
            ActivationSystemBtn.Location = new Point(6, 6);
            ActivationSystemBtn.Name = "ActivationSystemBtn";
            ActivationSystemBtn.Size = new Size(132, 55);
            ActivationSystemBtn.TabIndex = 0;
            ActivationSystemBtn.Text = "检查并激活系统";
            ActivationSystemBtn.UseVisualStyleBackColor = true;
            ActivationSystemBtn.Click += ActivationSystemBtn_Click;
            // 
            // tabPage3
            // 
            tabPage3.Location = new Point(4, 26);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(808, 168);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "系统备份";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(LogTxtbox);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(5, 228);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(816, 261);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "软件日志";
            // 
            // LogTxtbox
            // 
            LogTxtbox.Dock = DockStyle.Fill;
            LogTxtbox.Location = new Point(3, 19);
            LogTxtbox.Multiline = true;
            LogTxtbox.Name = "LogTxtbox";
            LogTxtbox.ScrollBars = ScrollBars.Vertical;
            LogTxtbox.Size = new Size(810, 239);
            LogTxtbox.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new Point(5, 489);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(816, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { 设置ToolStripMenuItem });
            menuStrip1.Location = new Point(5, 5);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(816, 25);
            menuStrip1.TabIndex = 3;
            menuStrip1.Text = "menuStrip1";
            // 
            // 设置ToolStripMenuItem
            // 
            设置ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 切换管理权限ToolStripMenuItem, 服务器设置ToolStripMenuItem });
            设置ToolStripMenuItem.Name = "设置ToolStripMenuItem";
            设置ToolStripMenuItem.Size = new Size(44, 21);
            设置ToolStripMenuItem.Text = "设置";
            // 
            // 切换管理权限ToolStripMenuItem
            // 
            切换管理权限ToolStripMenuItem.Name = "切换管理权限ToolStripMenuItem";
            切换管理权限ToolStripMenuItem.Size = new Size(148, 22);
            切换管理权限ToolStripMenuItem.Text = "切换管理权限";
            // 
            // 服务器设置ToolStripMenuItem
            // 
            服务器设置ToolStripMenuItem.Name = "服务器设置ToolStripMenuItem";
            服务器设置ToolStripMenuItem.Size = new Size(148, 22);
            服务器设置ToolStripMenuItem.Text = "服务器设置";
            服务器设置ToolStripMenuItem.Click += 服务器设置ToolStripMenuItem_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(826, 516);
            Controls.Add(groupBox1);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            Name = "Main";
            Padding = new Padding(5);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PC管理助手";
            Load += Main_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TextBox txtComputerInfo;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private GroupBox groupBox1;
        private StatusStrip statusStrip1;
        private TextBox LogTxtbox;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 设置ToolStripMenuItem;
        private ToolStripMenuItem 切换管理权限ToolStripMenuItem;
        private ToolStripMenuItem 服务器设置ToolStripMenuItem;
        private Button ActivationSystemBtn;
        private Button button2;
        private Button button3;
    }
}

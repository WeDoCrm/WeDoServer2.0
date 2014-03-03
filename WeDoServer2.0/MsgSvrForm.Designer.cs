namespace WDMsgServer
{
    partial class MsgSvrForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MsgSvrForm));
            this.ButtonStart = new System.Windows.Forms.Button();
            this.ButtonStop = new System.Windows.Forms.Button();
            this.LogBox = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MnControl = new System.Windows.Forms.ToolStripMenuItem();
            this.MnServerStart = new System.Windows.Forms.ToolStripMenuItem();
            this.MnServerStop = new System.Windows.Forms.ToolStripMenuItem();
            this.MnView = new System.Windows.Forms.ToolStripMenuItem();
            this.MnDBSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.StripMenu_svrconfig = new System.Windows.Forms.ToolStripMenuItem();
            this.콜테스트ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notify_svr = new System.Windows.Forms.NotifyIcon(this.components);
            this.MnStrip_noti = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.보이기ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.서버종료ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.MnStrip_noti.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonStart
            // 
            this.ButtonStart.Location = new System.Drawing.Point(8, 36);
            this.ButtonStart.Name = "ButtonStart";
            this.ButtonStart.Size = new System.Drawing.Size(75, 27);
            this.ButtonStart.TabIndex = 0;
            this.ButtonStart.Text = "서버 Start";
            this.ButtonStart.UseVisualStyleBackColor = true;
            this.ButtonStart.Click += new System.EventHandler(this.start_Click);
            // 
            // ButtonStop
            // 
            this.ButtonStop.Location = new System.Drawing.Point(86, 36);
            this.ButtonStop.Name = "ButtonStop";
            this.ButtonStop.Size = new System.Drawing.Size(75, 27);
            this.ButtonStop.TabIndex = 1;
            this.ButtonStop.Text = "서버 Stop";
            this.ButtonStop.UseVisualStyleBackColor = true;
            this.ButtonStop.Click += new System.EventHandler(this.stop_Click);
            // 
            // LogBox
            // 
            this.LogBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LogBox.BackColor = System.Drawing.Color.Black;
            this.LogBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogBox.ForeColor = System.Drawing.Color.White;
            this.LogBox.Location = new System.Drawing.Point(8, 69);
            this.LogBox.Multiline = true;
            this.LogBox.Name = "LogBox";
            this.LogBox.ReadOnly = true;
            this.LogBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LogBox.Size = new System.Drawing.Size(620, 490);
            this.LogBox.TabIndex = 6;
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnControl,
            this.MnView});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(638, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MnControl
            // 
            this.MnControl.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnServerStart,
            this.MnServerStop});
            this.MnControl.Name = "MnControl";
            this.MnControl.Size = new System.Drawing.Size(59, 20);
            this.MnControl.Text = "제어(&C)";
            // 
            // MnServerStart
            // 
            this.MnServerStart.Name = "MnServerStart";
            this.MnServerStart.Size = new System.Drawing.Size(143, 22);
            this.MnServerStart.Text = "서버 시작(&S)";
            this.MnServerStart.Click += new System.EventHandler(this.MnServerStart_Click);
            // 
            // MnServerStop
            // 
            this.MnServerStop.Name = "MnServerStop";
            this.MnServerStop.Size = new System.Drawing.Size(143, 22);
            this.MnServerStop.Text = "서버 중지(&O)";
            this.MnServerStop.Click += new System.EventHandler(this.MnServerStop_Click);
            // 
            // MnView
            // 
            this.MnView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnDBSetting,
            this.StripMenu_svrconfig,
            this.콜테스트ToolStripMenuItem});
            this.MnView.Name = "MnView";
            this.MnView.Size = new System.Drawing.Size(58, 20);
            this.MnView.Text = "설정(&S)";
            // 
            // MnDBSetting
            // 
            this.MnDBSetting.Name = "MnDBSetting";
            this.MnDBSetting.Size = new System.Drawing.Size(162, 22);
            this.MnDBSetting.Text = "DB 설정(&O)";
            this.MnDBSetting.Click += new System.EventHandler(this.MnDBSetting_Click);
            // 
            // StripMenu_svrconfig
            // 
            this.StripMenu_svrconfig.Name = "StripMenu_svrconfig";
            this.StripMenu_svrconfig.Size = new System.Drawing.Size(162, 22);
            this.StripMenu_svrconfig.Text = "통신환경설정(&C)";
            this.StripMenu_svrconfig.Click += new System.EventHandler(this.StripMenu_svrconfig_Click);
            // 
            // 콜테스트ToolStripMenuItem
            // 
            this.콜테스트ToolStripMenuItem.Name = "콜테스트ToolStripMenuItem";
            this.콜테스트ToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.콜테스트ToolStripMenuItem.Text = "콜테스트";
            this.콜테스트ToolStripMenuItem.Click += new System.EventHandler(this.콜테스트ToolStripMenuItem_Click);
            // 
            // notify_svr
            // 
            this.notify_svr.ContextMenuStrip = this.MnStrip_noti;
            this.notify_svr.Icon = ((System.Drawing.Icon)(resources.GetObject("notify_svr.Icon")));
            this.notify_svr.Text = "WeDo서버";
            this.notify_svr.Visible = true;
            this.notify_svr.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notify_svr_MouseClick);
            // 
            // MnStrip_noti
            // 
            this.MnStrip_noti.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.보이기ToolStripMenuItem,
            this.서버종료ToolStripMenuItem});
            this.MnStrip_noti.Name = "MnStrip_noti";
            this.MnStrip_noti.Size = new System.Drawing.Size(139, 48);
            // 
            // 보이기ToolStripMenuItem
            // 
            this.보이기ToolStripMenuItem.Name = "보이기ToolStripMenuItem";
            this.보이기ToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.보이기ToolStripMenuItem.Text = "보이기(&V)";
            this.보이기ToolStripMenuItem.Click += new System.EventHandler(this.보이기ToolStripMenuItem_Click);
            // 
            // 서버종료ToolStripMenuItem
            // 
            this.서버종료ToolStripMenuItem.Name = "서버종료ToolStripMenuItem";
            this.서버종료ToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.서버종료ToolStripMenuItem.Text = "서버종료(&C)";
            this.서버종료ToolStripMenuItem.Click += new System.EventHandler(this.서버종료ToolStripMenuItem_Click);
            // 
            // MsgSvrForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(638, 568);
            this.Controls.Add(this.LogBox);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.ButtonStart);
            this.Controls.Add(this.ButtonStop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(654, 606);
            this.Name = "MsgSvrForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WeDo CTI 서버";
            this.MinimumSizeChanged += new System.EventHandler(this.MsgSvrForm_MinimumSizeChanged);
            this.Load += new System.EventHandler(this.MsgSvrForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MsgSvrForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.MnStrip_noti.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonStart;
        private System.Windows.Forms.Button ButtonStop;
        private System.Windows.Forms.TextBox LogBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MnControl;
        private System.Windows.Forms.ToolStripMenuItem MnServerStart;
        private System.Windows.Forms.ToolStripMenuItem MnServerStop;
        private System.Windows.Forms.ToolStripMenuItem MnView;
        private System.Windows.Forms.ToolStripMenuItem MnDBSetting;
        private System.Windows.Forms.ToolStripMenuItem StripMenu_svrconfig;
        private System.Windows.Forms.ToolStripMenuItem 콜테스트ToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notify_svr;
        private System.Windows.Forms.ContextMenuStrip MnStrip_noti;
        private System.Windows.Forms.ToolStripMenuItem 보이기ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 서버종료ToolStripMenuItem;
    }
}


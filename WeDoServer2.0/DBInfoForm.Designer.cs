namespace WDMsgServer
{
    partial class DBInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBInfoForm));
            this.label1 = new System.Windows.Forms.Label();
            this.tbx_id = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbx_pass = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbx_host = new System.Windows.Forms.TextBox();
            this.btn_confirm = new System.Windows.Forms.Button();
            this.grb_account = new System.Windows.Forms.GroupBox();
            this.grb_connect = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbx_dbname = new System.Windows.Forms.TextBox();
            this.grb_account.SuspendLayout();
            this.grb_connect.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 12);
            this.label1.TabIndex = 0;
            // 
            // tbx_id
            // 
            this.tbx_id.Location = new System.Drawing.Point(107, 21);
            this.tbx_id.Name = "tbx_id";
            this.tbx_id.ReadOnly = true;
            this.tbx_id.Size = new System.Drawing.Size(118, 21);
            this.tbx_id.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new System.Drawing.Point(26, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "ID";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label3.Location = new System.Drawing.Point(26, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 15);
            this.label3.TabIndex = 7;
            this.label3.Text = "Pass";
            // 
            // tbx_pass
            // 
            this.tbx_pass.Location = new System.Drawing.Point(107, 48);
            this.tbx_pass.Name = "tbx_pass";
            this.tbx_pass.PasswordChar = '●';
            this.tbx_pass.ReadOnly = true;
            this.tbx_pass.Size = new System.Drawing.Size(118, 21);
            this.tbx_pass.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label4.Location = new System.Drawing.Point(26, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 15);
            this.label4.TabIndex = 8;
            this.label4.Text = "DB Host";
            // 
            // tbx_host
            // 
            this.tbx_host.Location = new System.Drawing.Point(107, 23);
            this.tbx_host.Name = "tbx_host";
            this.tbx_host.ReadOnly = true;
            this.tbx_host.Size = new System.Drawing.Size(118, 21);
            this.tbx_host.TabIndex = 1;
            // 
            // btn_confirm
            // 
            this.btn_confirm.Location = new System.Drawing.Point(101, 207);
            this.btn_confirm.Name = "btn_confirm";
            this.btn_confirm.Size = new System.Drawing.Size(69, 24);
            this.btn_confirm.TabIndex = 12;
            this.btn_confirm.Text = "확인";
            this.btn_confirm.UseVisualStyleBackColor = true;
            // 
            // grb_account
            // 
            this.grb_account.Controls.Add(this.label3);
            this.grb_account.Controls.Add(this.tbx_pass);
            this.grb_account.Controls.Add(this.label2);
            this.grb_account.Controls.Add(this.tbx_id);
            this.grb_account.Enabled = false;
            this.grb_account.Location = new System.Drawing.Point(13, 121);
            this.grb_account.Name = "grb_account";
            this.grb_account.Size = new System.Drawing.Size(254, 79);
            this.grb_account.TabIndex = 13;
            this.grb_account.TabStop = false;
            this.grb_account.Text = "DB 계정 정보";
            // 
            // grb_connect
            // 
            this.grb_connect.Controls.Add(this.label5);
            this.grb_connect.Controls.Add(this.tbx_dbname);
            this.grb_connect.Controls.Add(this.label4);
            this.grb_connect.Controls.Add(this.tbx_host);
            this.grb_connect.Controls.Add(this.label1);
            this.grb_connect.Enabled = false;
            this.grb_connect.Location = new System.Drawing.Point(13, 21);
            this.grb_connect.Name = "grb_connect";
            this.grb_connect.Size = new System.Drawing.Size(253, 89);
            this.grb_connect.TabIndex = 14;
            this.grb_connect.TabStop = false;
            this.grb_connect.Text = "DB 접속 정보";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label5.Location = new System.Drawing.Point(26, 60);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 15);
            this.label5.TabIndex = 10;
            this.label5.Text = "DB Name";
            // 
            // tbx_dbname
            // 
            this.tbx_dbname.Location = new System.Drawing.Point(106, 56);
            this.tbx_dbname.Name = "tbx_dbname";
            this.tbx_dbname.ReadOnly = true;
            this.tbx_dbname.Size = new System.Drawing.Size(118, 21);
            this.tbx_dbname.TabIndex = 9;
            // 
            // DBInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(281, 240);
            this.Controls.Add(this.grb_connect);
            this.Controls.Add(this.grb_account);
            this.Controls.Add(this.btn_confirm);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DBInfoForm";
            this.Text = "DB 설정";
            this.grb_account.ResumeLayout(false);
            this.grb_account.PerformLayout();
            this.grb_connect.ResumeLayout(false);
            this.grb_connect.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox tbx_id;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox tbx_pass;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.TextBox tbx_host;
        public System.Windows.Forms.Button btn_confirm;
        public System.Windows.Forms.GroupBox grb_account;
        public System.Windows.Forms.GroupBox grb_connect;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.TextBox tbx_dbname;
    }
}
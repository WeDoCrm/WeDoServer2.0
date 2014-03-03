namespace WDMsgServer
{
    partial class CallTestForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtbox_ani = new System.Windows.Forms.TextBox();
            this.txtbox_ext = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtbox_time = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.btn_confirm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(31, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "발신번호";
            // 
            // txtbox_ani
            // 
            this.txtbox_ani.Location = new System.Drawing.Point(119, 35);
            this.txtbox_ani.Name = "txtbox_ani";
            this.txtbox_ani.Size = new System.Drawing.Size(100, 21);
            this.txtbox_ani.TabIndex = 1;
            // 
            // txtbox_ext
            // 
            this.txtbox_ext.Location = new System.Drawing.Point(119, 62);
            this.txtbox_ext.Name = "txtbox_ext";
            this.txtbox_ext.Size = new System.Drawing.Size(100, 21);
            this.txtbox_ext.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new System.Drawing.Point(31, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "받는내선";
            // 
            // txtbox_time
            // 
            this.txtbox_time.Location = new System.Drawing.Point(119, 89);
            this.txtbox_time.Name = "txtbox_time";
            this.txtbox_time.Size = new System.Drawing.Size(100, 21);
            this.txtbox_time.TabIndex = 5;
            this.txtbox_time.Text = "1000";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label3.Location = new System.Drawing.Point(31, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(79, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "Delay Time";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(44, 167);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "취소";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btn_confirm
            // 
            this.btn_confirm.Location = new System.Drawing.Point(153, 167);
            this.btn_confirm.Name = "btn_confirm";
            this.btn_confirm.Size = new System.Drawing.Size(75, 23);
            this.btn_confirm.TabIndex = 9;
            this.btn_confirm.Text = "시작";
            this.btn_confirm.UseVisualStyleBackColor = true;
            // 
            // CallTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(272, 216);
            this.Controls.Add(this.btn_confirm);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtbox_time);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtbox_ext);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtbox_ani);
            this.Controls.Add(this.label1);
            this.Name = "CallTestForm";
            this.Text = "콜테스트";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox txtbox_ani;
        public System.Windows.Forms.TextBox txtbox_ext;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.TextBox txtbox_time;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Button button1;
        public System.Windows.Forms.Button btn_confirm;
    }
}
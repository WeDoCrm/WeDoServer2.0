namespace WDMsgServer
{
    partial class SetNICForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetNICForm));
            this.btn_comfirm = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbt_type_cid4 = new System.Windows.Forms.RadioButton();
            this.rbt_type_cid2 = new System.Windows.Forms.RadioButton();
            this.rbt_type_cid1 = new System.Windows.Forms.RadioButton();
            this.rbt_type_lg = new System.Windows.Forms.RadioButton();
            this.rbt_type_sip = new System.Windows.Forms.RadioButton();
            this.label_COM_CODE = new System.Windows.Forms.Label();
            this.tbx_com_code = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_comfirm
            // 
            this.btn_comfirm.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_comfirm.Location = new System.Drawing.Point(108, 159);
            this.btn_comfirm.Name = "btn_comfirm";
            this.btn_comfirm.Size = new System.Drawing.Size(75, 23);
            this.btn_comfirm.TabIndex = 0;
            this.btn_comfirm.Text = "확인";
            this.btn_comfirm.UseVisualStyleBackColor = true;
            this.btn_comfirm.Click += new System.EventHandler(this.btn_comfirm_Click);
            // 
            // btn_cancel
            // 
            this.btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_cancel.Location = new System.Drawing.Point(215, 159);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_cancel.TabIndex = 1;
            this.btn_cancel.Text = "취소";
            this.btn_cancel.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            ":::::::::::::::::선 택::::::::::::::::::::"});
            this.comboBox1.Location = new System.Drawing.Point(75, 131);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(295, 20);
            this.comboBox1.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 135);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "통신장치";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbt_type_cid4);
            this.groupBox1.Controls.Add(this.rbt_type_cid2);
            this.groupBox1.Controls.Add(this.rbt_type_cid1);
            this.groupBox1.Controls.Add(this.rbt_type_lg);
            this.groupBox1.Controls.Add(this.rbt_type_sip);
            this.groupBox1.Location = new System.Drawing.Point(20, 42);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(350, 74);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "전화장치 유형";
            // 
            // rbt_type_cid4
            // 
            this.rbt_type_cid4.AutoSize = true;
            this.rbt_type_cid4.Location = new System.Drawing.Point(191, 45);
            this.rbt_type_cid4.Name = "rbt_type_cid4";
            this.rbt_type_cid4.Size = new System.Drawing.Size(83, 16);
            this.rbt_type_cid4.TabIndex = 5;
            this.rbt_type_cid4.TabStop = true;
            this.rbt_type_cid4.Tag = "CI4";
            this.rbt_type_cid4.Text = "CID(4포트)";
            this.rbt_type_cid4.UseVisualStyleBackColor = true;
            this.rbt_type_cid4.CheckedChanged += new System.EventHandler(this.rbt_type_sip_CheckedChanged);
            // 
            // rbt_type_cid2
            // 
            this.rbt_type_cid2.AutoSize = true;
            this.rbt_type_cid2.Location = new System.Drawing.Point(102, 45);
            this.rbt_type_cid2.Name = "rbt_type_cid2";
            this.rbt_type_cid2.Size = new System.Drawing.Size(83, 16);
            this.rbt_type_cid2.TabIndex = 4;
            this.rbt_type_cid2.TabStop = true;
            this.rbt_type_cid2.Tag = "CI2";
            this.rbt_type_cid2.Text = "CID(2포트)";
            this.rbt_type_cid2.UseVisualStyleBackColor = true;
            this.rbt_type_cid2.CheckedChanged += new System.EventHandler(this.rbt_type_sip_CheckedChanged);
            // 
            // rbt_type_cid1
            // 
            this.rbt_type_cid1.AutoSize = true;
            this.rbt_type_cid1.Location = new System.Drawing.Point(13, 45);
            this.rbt_type_cid1.Name = "rbt_type_cid1";
            this.rbt_type_cid1.Size = new System.Drawing.Size(83, 16);
            this.rbt_type_cid1.TabIndex = 3;
            this.rbt_type_cid1.TabStop = true;
            this.rbt_type_cid1.Tag = "CI1";
            this.rbt_type_cid1.Text = "CID(1포트)";
            this.rbt_type_cid1.UseVisualStyleBackColor = true;
            this.rbt_type_cid1.CheckedChanged += new System.EventHandler(this.rbt_type_sip_CheckedChanged);
            // 
            // rbt_type_lg
            // 
            this.rbt_type_lg.AutoSize = true;
            this.rbt_type_lg.Location = new System.Drawing.Point(102, 23);
            this.rbt_type_lg.Name = "rbt_type_lg";
            this.rbt_type_lg.Size = new System.Drawing.Size(63, 16);
            this.rbt_type_lg.TabIndex = 1;
            this.rbt_type_lg.TabStop = true;
            this.rbt_type_lg.Tag = "LG";
            this.rbt_type_lg.Text = "LG키폰";
            this.rbt_type_lg.UseVisualStyleBackColor = true;
            this.rbt_type_lg.CheckedChanged += new System.EventHandler(this.rbt_type_sip_CheckedChanged);
            // 
            // rbt_type_sip
            // 
            this.rbt_type_sip.AutoSize = true;
            this.rbt_type_sip.Location = new System.Drawing.Point(13, 23);
            this.rbt_type_sip.Name = "rbt_type_sip";
            this.rbt_type_sip.Size = new System.Drawing.Size(83, 16);
            this.rbt_type_sip.TabIndex = 0;
            this.rbt_type_sip.TabStop = true;
            this.rbt_type_sip.Tag = "SIP";
            this.rbt_type_sip.Text = "인터넷전화";
            this.rbt_type_sip.UseVisualStyleBackColor = true;
            this.rbt_type_sip.CheckedChanged += new System.EventHandler(this.rbt_type_sip_CheckedChanged);
            // 
            // label_COM_CODE
            // 
            this.label_COM_CODE.AutoSize = true;
            this.label_COM_CODE.Location = new System.Drawing.Point(28, 20);
            this.label_COM_CODE.Name = "label_COM_CODE";
            this.label_COM_CODE.Size = new System.Drawing.Size(53, 12);
            this.label_COM_CODE.TabIndex = 5;
            this.label_COM_CODE.Text = "회사코드";
            // 
            // tbx_com_code
            // 
            this.tbx_com_code.Location = new System.Drawing.Point(87, 14);
            this.tbx_com_code.Name = "tbx_com_code";
            this.tbx_com_code.ReadOnly = true;
            this.tbx_com_code.Size = new System.Drawing.Size(184, 21);
            this.tbx_com_code.TabIndex = 0;
            // 
            // SetNICForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(394, 193);
            this.Controls.Add(this.tbx_com_code);
            this.Controls.Add(this.label_COM_CODE);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_comfirm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SetNICForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "통신환경설정";
            this.TopMost = true;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btn_comfirm;
        public System.Windows.Forms.Button btn_cancel;
        public System.Windows.Forms.ComboBox comboBox1;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.RadioButton rbt_type_lg;
        public System.Windows.Forms.RadioButton rbt_type_sip;
        public System.Windows.Forms.RadioButton rbt_type_cid1;
        public System.Windows.Forms.Label label_COM_CODE;
        public System.Windows.Forms.TextBox tbx_com_code;
        public System.Windows.Forms.RadioButton rbt_type_cid2;
        public System.Windows.Forms.RadioButton rbt_type_cid4;
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WDMsgServer
{
    public partial class MemberListForm : Form
    {
        public MemberListForm()
        {
            InitializeComponent();
        }

        private void MemberListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MsgSvrForm.svrStart == true)
            {
                e.Cancel = true;
                this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            }
            
        }
    }
}
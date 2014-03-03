using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WDMsgServer
{
    public partial class CallTestForm : Form
    {
        public CallTestForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.txtbox_ani.Clear();
            this.txtbox_ext.Clear();
            this.txtbox_time.Clear();
            this.txtbox_ani.Focus();
        }
    }
}
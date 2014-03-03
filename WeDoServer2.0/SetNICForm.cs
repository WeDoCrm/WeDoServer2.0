using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeDoCommon;
using SharpPcap;
using System.IO.Ports;
using System.Diagnostics;

namespace WDMsgServer
{
    public partial class SetNICForm : Form
    {
        ServerConfigController ctrl;
        string serverType = "";
        public event EventHandler<StringEventArgs> LogWriteHandler;
        delegate void stringDele(string str);
        private CaptureDeviceList deviceList = null;

        public SetNICForm()
        {
            InitializeComponent();
        }

        public SetNICForm(ServerConfigController ctrl)
        {
            this.ctrl = ctrl;
            InitializeComponent();
            initialize();
        }

        private void initialize()
        {
            if (ctrl.CompanyCode.Length > 0)
            {
                tbx_com_code.Text = ctrl.CompanyCode;
            }

            if (ctrl.ServerType != null && ctrl.ServerType.Length > 0)
            {
                switch (ctrl.ServerType)
                {
                    case ConstDef.NIC_SIP:
                        rbt_type_sip.Checked = true;

                        break;

                    case ConstDef.NIC_LG_KP:
                        rbt_type_lg.Checked = true;
                        break;

                    case ConstDef.NIC_CID_PORT1:
                        rbt_type_cid1.Checked = true;
                        break;
                    case ConstDef.NIC_CID_PORT2:
                        rbt_type_cid2.Checked = true;
                        break;
                    case ConstDef.NIC_CID_PORT4:
                        rbt_type_cid4.Checked = true;
                        break;
                }
            }

            if (ctrl.Device != null && ctrl.Device.Length > 0)
            {
                comboBox1.SelectedItem = ctrl.Device;
            }

        }

        private void btn_comfirm_Click(object sender, EventArgs e)
        {
            string nicName = "";

            nicName = (string)comboBox1.SelectedItem;


            if (nicName.Length > 1)
            {
                ctrl.Device = nicName;

                if (!ctrl.AutoStart)
                {
                    if (MessageBox.Show("WeDo 서버를 자동실행 설정하시겠습니까?", "알림", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        ctrl.AutoStart = true;
                    }
                }
                ctrl.ServerType = serverType;
            }
            else
            {
                if (MessageBox.Show("통신장치를 선택하세요.", "통신장치 선택"
                                   , MessageBoxButtons.OK) == DialogResult.OK)
                {
                    DialogResult = DialogResult.None;
                }
            }

        }

        private void rbt_type_sip_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                RadioButton rbt = (RadioButton)sender;
                if (rbt.Checked == true)
                {
                    stringDele changerbt = new stringDele(chageRBTstatus);
                    Invoke(changerbt, new object[] { rbt.Name });
                    OnWriteLog("통화장치 타입변경 : " + rbt.Name);
                    serverType = rbt.Tag.ToString();
                    OnWriteLog("server_type : " + serverType);
                }
            }
            catch (Exception ex)
            {
                OnWriteLog(ex.ToString());
            }
        }

        private void chageRBTstatus(string rbtname)
        {
            if (rbtname.Equals(ConstDef.RBT_TYPE_SIP))
            {

                if (Utils.CheckWincapInstall())
                {
                    listupDevice(rbtname);
                }
                else
                {
                    if (MessageBox.Show("SIP 폰 사용의 경우, WinPcap 프로그램을 설치해야 합니다.\r\n 설치 하시겠습니까?"
                        , "알림"
                        , MessageBoxButtons.YesNo
                        , MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Process.Start(Application.StartupPath + ConstDef.WINPCAP);
                    }
                }
            }
            else
            {
                listupDevice(rbtname);
            }
        }

        private void listupDevice(string rbtname)
        {
            try
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("::::::::::::장 치 선 택::::::::::::");
                if (rbtname.Equals(ConstDef.RBT_TYPE_SIP))
                {
                    deviceList = CaptureDeviceList.Instance;
                    if (deviceList.Count != 0)
                    {
                        foreach (ICaptureDevice d in deviceList)
                        {
                            comboBox1.Items.Add(d.Description);
                        }

                        if (ctrl.ServerType != null && !ctrl.ServerType.Equals(ConstDef.NIC_SIP))
                        {
                            comboBox1.DroppedDown = true;
                        }

                        else if (ctrl.ServerType == null)
                        {
                            comboBox1.DroppedDown = true;
                        }
                    }
                }
                else
                {
                    string[] ports = SerialPort.GetPortNames();
                    if (ports.Length > 0)
                    {
                        foreach (string item in ports)
                        {
                            comboBox1.Items.Add(item);
                        }

                        if (ctrl.ServerType != null && ctrl.ServerType.Equals(ConstDef.NIC_SIP))
                        {
                            comboBox1.DroppedDown = true;
                        }
                        else if (ctrl.ServerType == null)
                        {
                            comboBox1.DroppedDown = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnWriteLog("장치선택 오류발생:"+ex.ToString());

            }
        }

        public virtual void OnWriteLog(string msg)
        {
            Logger.info(msg);
            EventHandler<StringEventArgs> handler = this.LogWriteHandler;
            if (this.LogWriteHandler != null)
            {
                handler(this, new StringEventArgs(msg));
            }
        }
    }
}

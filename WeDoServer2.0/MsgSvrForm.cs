using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Data.SqlClient;
using System.Xml;
using PacketDotNet;
using SharpPcap;
using System.Diagnostics;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data;
using MySql.Data.Types;
using CallControl;
using System.Net.NetworkInformation;
using WindowsInstaller;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using WeDoCommon;
using WeDoCommon.Sockets;


namespace WDMsgServer
{
    public partial class MsgSvrForm : Form
    {

        #region 기본 서버설정 정보...

        //라이센스파일로 교체 2013/05/13
        //라이선스 서버 정보
        //private string SVR_HOST = null;
        //private string LICENSE_PORT = null;
        private LicenseHandler mLicenseHandler;
        private ServerConfigController configCtrl;

        private static System.Windows.Forms.Timer timerForLicense;


        //DB 정보


        //기본 설정 정보

        private System.Windows.Forms.Timer callLog_timer;

        protected internal static bool svrStart = false;
        private AddTextDelegate AddText = null;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        private SIPMessage SIPM = null;
        private ICaptureDevice dev = null;
        private CaptureDeviceList deviceList = null;

        //DB관련 
        private MySqlHandler dbHandler = null;

        private CommCtl commctl = new CommCtl();

        private ArrayList tList = new ArrayList();          //팀이름 저장
        private Hashtable CustomerList_Primary = new Hashtable();  
        private Hashtable CustomerList_Backup = new Hashtable();
        private Hashtable Statlist = new Hashtable();
        private Dictionary<string, string> TeamNameList = new Dictionary<string, string>();  //
        private ArrayList SendErrorList = new ArrayList();
        protected internal static string serverip = null;

        public Dictionary<string, IPEndPoint> InClientList = new Dictionary<string,IPEndPoint>();  //로그인 사용자 EndPoint정보 테이블(key=id, value=IPEndPoint)
        private Dictionary<string, string> InClientStat = new Dictionary<string, string>();  //사용자, 상태정보 ex: "online"
        private Dictionary<string, IPEndPoint> ExtensionList = new Dictionary<string, IPEndPoint>(); //로그인 사용자 내선리스트(key = 내선번호, value = IPEndPoint)
        private Dictionary<string, string> CallLogTable = new Dictionary<string, string>();  // ani, key값(datetime.now, 등)
        private Dictionary<string, Client> ClientInfoList = new Dictionary<string, Client>();
        private Dictionary<string, string> ExtensionIDpair = new Dictionary<string, string>();//내선과 id 정보 테이블

        private ArrayList TeamList = new ArrayList();  //구성 : M|팀이름|id|이름|....


        private CallTestForm calltestform = null;
        private DBInfoForm dbinfo = null;
        private bool CustomerCacheSwitch = false;
        private bool CustomerCacheReload = false;  //동시실행 방지
        private static bool serviceStart = false;

        delegate void stringDele(string str);
        delegate void ringingDele(string st1, string st2, string st3);
        delegate void AbandonDele();
        delegate void NoParamDele();

        #endregion

        public MsgSvrForm()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                
            }
        }

        private void MsgSvrForm_Load(object sender, EventArgs e)
        {
            try
            {
                Logger.setLogLevel(LOGLEVEL.INFO);
                Logger.BackupOnInit();
                LogWrite("svr_FileCheck() 완료!");

                configCtrl = new ServerConfigController();
                configCtrl.LogWriteHandler += this.OnLogWrite;
                if (!configCtrl.LoadData())
                    throw new Exception("설정정보 로딩 실패.");

                commctl.OnEvent += new CommCtl.CommCtl_MessageDelegate(RecvMessage);

                timerForLicense = new System.Windows.Forms.Timer();
                timerForLicense.Interval = 3600000;
                timerForLicense.Tick += new EventHandler(timerForLicense_Tick);
                timerForLicense.Start();

                callLog_timer = new System.Windows.Forms.Timer();
                callLog_timer.Interval = 300000;
                callLog_timer.Tick += new EventHandler(callLog_timer_Tick);
                callLog_timer.Start();

                dbHandler = new MySqlHandler(configCtrl.DbServerIp, configCtrl.DbPort, configCtrl.DbName, configCtrl.DbUser, configCtrl.DbPasswd);
                dbHandler.LogWriteHandler += this.OnLogWrite;

                startServer();
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        private void callLog_timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT1)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT2)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT4)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP))
                {
                    lock (CallLogTable)
                    {
                        foreach (var de in CallLogTable)
                        {
                            string call_id = de.Key.ToString();
                            string[] arr = call_id.Split('$');
                            if (arr.Length > 1)
                            {
                                string time = arr[0]; //yyyyMMddHHmmss
                                int year = Convert.ToInt32(time.Substring(0, 4));
                                int month = Convert.ToInt32(time.Substring(4, 2));
                                int day = Convert.ToInt32(time.Substring(6, 2));
                                int hour = Convert.ToInt32(time.Substring(8, 2));
                                int minute = Convert.ToInt32(time.Substring(10, 2));
                                int second = Convert.ToInt32(time.Substring(12, 2));

                                if (day != DateTime.Now.Day)
                                {
                                    LogWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                                else if (hour != DateTime.Now.Hour)
                                {
                                    LogWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                                else if ((DateTime.Now.Minute - minute) > 5)
                                {
                                    LogWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                            }
                            else
                            {
                                LogWrite("callLog_timer_Tick Error : call_id is not devided two string[]");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        private void timerForLicense_Tick(object sender, EventArgs e)
        {
            int hour = DateTime.Now.Hour;
            if (hour == 1)
            {
                registerLicenseInfo();
            }
            
        }

        // Cross-Thread 호출를 실행하기 위해 사용합니다.
        private delegate void AddTextDelegate(string strText);  //로그기록 델리게이트
        private delegate void MakeTree();    //전체 사용자 리스트 생성 델리게이트
        private delegate string GetInformation(string str);
        private delegate ArrayList GetNoticeListDelegate();

        private void start_Click(object sender, EventArgs e)
        {
            startServer();
        }

        private void startServer()
        {
            try
            {
                //자동버전체크 폐기 2013/05/18 
                //VersionCheck();
                LogWrite("Installed Version = " + ConstDef.VERSION);

                if ((configCtrl.Device == null || configCtrl.Device.Equals("")) 
                    && (configCtrl.ServerType == null ||configCtrl.ServerType.Equals("")))
                {
                    setDevice();
                }
                else
                {
                    registerLicenseInfo();
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        private void initService()
        {
            try
            {
                checkFirstStart(configCtrl.CompanyCode, configCtrl.CompanyName);
                commctl.Select_Type(configCtrl.ServerType);
                commctl.Connect(configCtrl.Device);

                mServer = new TcpServerMgr(configCtrl.MsgrPort);
                mServer.SocStatusChanged += ProcessOnSocStatusChanged;
                // mServer.FTPListenRequested += ProcessOnFTPMessageReceived;
                mServer.DoRun();

                ListenThread = new Thread(StartListener);
                ListenThread.Start();
                svrStart = true;
                ButtonStart.Enabled = false;
                loadCustomerList();
                serviceStart = true;
                MnServerStart.Enabled = false;
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        /// <summary>
        /// 최초 설치시점에 회사등록코드 등록시
        /// </summary>
        /// <param name="com_code"></param>
        /// <param name="com_name"></param>
        private void checkFirstStart(string com_code, string com_name)
        {
            Hashtable parameters = new Hashtable();
            //MySqlDataReader dr = null;
            DataTable dt = null;

            try
            {

                parameters.Add("@param_comcd", com_code);
                parameters.Add("@param_comnm", com_name);

                string query = "select COM_CD, COM_NM from t_company where COM_CD = @param_comcd";

                dt = DoQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    throw new Exception("회사코드 미등록");
                }
                else
                {
                    LogWrite("회사코드 기등록 및 DB 데이터 기완료");
                }

            }
            catch (Exception ex)
            {
                LogWrite("checkFirstStart 회사코드 검증 오류: " + ex.ToString());
            }
            finally
            {
            }

        }

        /// <summary>
        /// 파일에서 라이센스정보를 읽어, 유효성검사를 한다.
        /// </summary>
        private void registerLicenseInfo()
        {
            try
            {
                LogWrite("라이선스 체크중.....");
                mLicenseHandler = new LicenseHandler(ConstDef.APP_DATA_CONFIG_DIR);
                mLicenseHandler.LogWriteHandler += this.OnLogWrite;
                //파일읽음&라이센스값 decode
                if (mLicenseHandler.ReadLicense())
                {

                    stringDele dele = new stringDele(disposeLicenseResult);
                    Invoke(dele, mLicenseHandler.ResultMessage);
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result">코드&회사명&만료일자</param>
        private void disposeLicenseResult(string result)
        {
            try
            {
                //result = -1 : 등록되어있지 않음
                //result = 1 : 라이선스 만료
                //result = 2 : 인증
                //result = 3: 30일 남음
                //result = 4: 7일 이하 남음
                //result = 5: 중복등록
                string license_message = "";
                string[] license_info = result.Split('&');

                if (license_info.Length < 1)
                {
                    throw new Exception("라이센스 결과코드 오류");
                }
                LicenseResult resultCode = (LicenseResult)Convert.ToInt16(license_info[0]);

                if (license_info.Length > 1)
                {
                    configCtrl.CompanyName = license_info[1];
                }

                switch (resultCode)
                {
                    case LicenseResult.ERR_INVALID_FILE:
                        MessageBox.Show("라이센스파일이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "라이센스파일이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;
                    case LicenseResult.ERR_MAC_ADDR:
                        MessageBox.Show("Mac 주소값이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "Mac 주소값이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;
                    case LicenseResult.ERR_NO_FILE:
                        MessageBox.Show("라이센스파일이 존재하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "라이센스파일이 존재하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.ERR_UNREGISTERED:
                        MessageBox.Show("등록되지 않은 회사코드입니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "등록되지 않은 회사코드입니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.ERR_EXPIRED:
                        MessageBox.Show("라이선스가 만료되었습니다. 연장 후 시작해 주세요.\r\n서버를 종료합니다.");
                        license_message = "라이선스가 만료되었습니다. 연장 후 시작해 주세요.\r\n서버를 종료합니다.";
                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.SUCCESS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        LogWrite("라이선스 인증 완료");
                        //MessageBox.Show("인증성공");

                        break;

                    case LicenseResult.WARN_30_DAYS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        LogWrite("라이선스 인증 완료");
                        //MessageBox.Show("라이선스 만료 30일 전입니다.");
                        license_message = "라이선스 만료 30일 전입니다.";
                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        break;

                    case LicenseResult.WARN_7_DAYS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        LogWrite("라이선스 인증 완료");
                        MessageBox.Show("라이선스 만료 7일 전입니다.");
                        license_message = "라이선스 만료 7일 전입니다.";
                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        break;

                        //case "5":
                        //    MessageBox.Show("해당 회사코드로 이미 사용중입니다.");
                        //    license_message = "해당 회사코드로 이미 사용중입니다.";
                        //    if (InClientList != null)
                        //    {
                        //        foreach (var de in InClientList)
                        //        {
                        //            if (de.Value != null)
                        //            {
                        //                SendMsg(license_message, (IPEndPoint)de.Value);
                        //            }
                        //        }
                        //    }

                        //break;
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        private void loadCustomerList()
        {
            Hashtable parameters = new Hashtable();
            DataTable dt = new DataTable();

            try
            {

                string query = "select C.CUSTOMER_ID, ifnull(C.CUSTOMER_NM,'') CUSTOMER_NM "
                            + ", ifnull(C.C_TELNO,'') C_TELNO, ifnull(C.H_TELNO,'') H_TELNO, ifnull(C.C_TELNO1,'') C_TELNO1 "
                            + ", ifnull(C.H_TELNO1,'') H_TELNO1, ifnull(D.TELNO,'') TELNO " 
                            + "FROM t_customer C LEFT JOIN " 
                            + "(SELECT B.TELNO, A.CUSTOMER_NM FROM t_customer A, t_customer_telno B WHERE A.CUSTOMER_ID=B.CUSTOMER_ID GROUP BY B.TELNO) D " 
                            + "ON C.CUSTOMER_NM=D.CUSTOMER_NM";

                dt = DoQuery(query, parameters);

                string cName = "";

                foreach (DataRow row in dt.Rows)
                {
                    cName = row["CUSTOMER_ID"].ToString() + "$" + row["CUSTOMER_NM"].ToString();

                    if (row["C_TELNO"].ToString().Length > 1)
                    {
                        CustomerList_Primary[row["C_TELNO"].ToString()] = cName;
                        CustomerList_Backup[row["C_TELNO"].ToString()] = cName;
                    }
                    if (row["H_TELNO"].ToString().Length > 1)
                    {
                        CustomerList_Primary[row["H_TELNO"].ToString()] = cName;
                        CustomerList_Backup[row["H_TELNO"].ToString()] = cName;
                    }
                    if (row["C_TELNO1"].ToString().Length > 1)
                    {
                        CustomerList_Primary[row["C_TELNO1"].ToString()] = cName;
                        CustomerList_Backup[row["C_TELNO1"].ToString()] = cName;
                    }
                    if (row["H_TELNO1"].ToString().Length > 1)
                    {
                        CustomerList_Primary[row["H_TELNO1"].ToString()] = cName;
                        CustomerList_Backup[row["H_TELNO1"].ToString()] = cName;
                    }
                    if (row["TELNO"].ToString().Length > 1)
                    {
                        CustomerList_Primary[row["TELNO"].ToString()] = cName;
                        CustomerList_Backup[row["TELNO"].ToString()] = cName;
                    }
                }
                LogWrite("CustomerListCache 데이터로드 완료");
               
            }
            catch (Exception ex)
            {
                LogWrite("CustomerListCache 데이터로드 실패" + ex.ToString());
            }
        }

        private void reloadCustomerListCache()
        {
            Hashtable parameters = new Hashtable();
            CustomerCacheReload = true;
            DataTable dt = new DataTable();
            try
            {
                string query = "select C.CUSTOMER_ID, ifnull(C.CUSTOMER_NM,'') CUSTOMER_NM "
                    + ", ifnull(C.C_TELNO,'') C_TELNO, ifnull(C.H_TELNO,'') H_TELNO, ifnull(C.C_TELNO1,'') C_TELNO1 "
                    + ", ifnull(C.H_TELNO1,'') H_TELNO1, ifnull(D.TELNO,'') TELNO " +
                    "FROM t_customer C LEFT JOIN " +
                    "(SELECT B.TELNO, A.CUSTOMER_NM FROM t_customer A, t_customer_telno B WHERE A.CUSTOMER_ID=B.CUSTOMER_ID GROUP BY B.TELNO) D " +
                    "ON C.CUSTOMER_NM=D.CUSTOMER_NM";

                dt = DoQuery(query, parameters);

                string cName = "";

                if (CustomerCacheSwitch == false)
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        cName = row["CUSTOMER_ID"].ToString() + "$" + row["CUSTOMER_NM"].ToString();

                        if (row["C_TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Backup[row["C_TELNO"].ToString()] = cName;
                        }
                        if (row["H_TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Backup[row["H_TELNO"].ToString()] = cName;
                        }
                        if (row["C_TELNO1"].ToString().Length > 1)
                        {
                            CustomerList_Backup[row["C_TELNO1"].ToString()] = cName;
                        }
                        if (row["H_TELNO1"].ToString().Length > 1)
                        {
                            CustomerList_Backup[row["H_TELNO1"].ToString()] = cName;
                        }
                        if (row["TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Backup[row["TELNO"].ToString()] = cName;
                        }
                    }
                    //foreach (var de in CustomerList_Backup)
                    //{
                    //    logWrite("CustomerList_Backup[" + de.Key.ToString() + "] = " + de.Value.ToString());
                    //}

                    CustomerCacheSwitch = true;
                }
                else
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        cName = row["CUSTOMER_ID"].ToString() + "$" + row["CUSTOMER_NM"].ToString();

                        if (row["C_TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Primary[row["C_TELNO"].ToString()] = cName;
                        }
                        if (row["H_TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Primary[row["H_TELNO"].ToString()] = cName;
                        }
                        if (row["C_TELNO1"].ToString().Length > 1)
                        {
                            CustomerList_Primary[row["C_TELNO1"].ToString()] = cName;
                        }
                        if (row["H_TELNO1"].ToString().Length > 1)
                        {
                            CustomerList_Primary[row["H_TELNO1"].ToString()] = cName;
                        }
                        if (row["TELNO"].ToString().Length > 1)
                        {
                            CustomerList_Primary[row["TELNO"].ToString()] = cName;
                        }
                    }

                    //foreach (var de in CustomerList_Primary)
                    //{
                    //    logWrite("CustomerList_Primary[" + de.Key.ToString() + "] = " + de.Value.ToString());
                    //}

                    CustomerCacheSwitch = false;

                }

            }
            catch (Exception ex)
            {
                LogWrite("고객정보 cache 갱신실패 : " + ex.ToString());
            }
            LogWrite("고객정보 cache 갱신 : " + CustomerCacheSwitch.ToString());
            CustomerCacheReload = false;
        }

        private void setDevice()
        {
            SetNICForm nicform = null;

            try
            {
                nicform = new SetNICForm(configCtrl);

                if (nicform != null)
                {
                    nicform.LogWriteHandler += this.OnLogWrite;


                    if (nicform.ShowDialog() == DialogResult.OK)
                    {
                        LogWrite("서버설정 완료");
                        startServer();
                    }
                    else
                    {
                        LogWrite("서버설정 취소");
                    }
                    nicform.LogWriteHandler -= this.OnLogWrite;
                    nicform.Dispose();
                }

                
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        private string GetTeamName(string id)
        {
            
            string teamname = "";
            try
            {
                if (TeamNameList != null)
                {
                    teamname = (string)TeamNameList[id];
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
            return teamname;
        }

        #region tcp테스트 receiveReq
        //public void receiveReq(int mode, string msg, IPEndPoint iep)
        //public void receiveReq(object Obj)
        //{
        //    try
        //    {
        //        logWrite("receiveReq thread 시작!");
        //        Thread.Sleep(10);
        //        ArrayList List = (ArrayList)Obj;
        //        int mode = (int)List[0];
        //        string msg = (string)List[1];
        //        Socket reSocket = (Socket)List[2];
        //        string[] re = null;

        //        switch (mode)
        //        {
        //            case 8://로그인

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id|비밀번호|내선번호
        //                Login(re, reSocket);

        //                break;

        //            case 2:  //중복 로그인 상황에서 기존접속 끊기 선택한 경우
        //                re = msg.Split('|');  //msg 구성 : 2|id|passwd

        //                SendMsg("u|", (Socket)InClientList[re[1]]); //기존 접속 원격 로그아웃 

        //                lock (InClientList)
        //                {
        //                    Logout(re[1]);      //기존 접속 서버측 로그아웃
        //                }
        //                Login(re, reSocket);

        //                break;

        //            case 1: //공지사항 목록 요청(1|)

        //                SelectNoticeAll(reSocket);
        //                break;


        //            case 0: //대화종료

        //                break;

        //            case 4: //메모 처리
        //                logWrite("메모처리");

        //                re = msg.Split('|'); //msg 구성 : 4|name|발신자id|메시지|수신자id

        //                if (re[1].Equals("m")) //쪽지 전송 실패로 서버측으로 메시지 반송시(4|m|name|발신자id|메시지|수신자id)
        //                {
        //                    InsertNoReceive(re[5], "", re[4], re[1], re[3]);
        //                }
        //                else
        //                {
        //                    InsertNoReceive(re[4], "", re[3], "m", re[2]);
        //                }

        //                break;

        //            case 5: //파일 전송 메시지(5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
        //                re = msg.Split('|');
        //                string[] filenameArray = re[1].Split('\\');
        //                string filename = filenameArray[(filenameArray.Length - 1)];
        //                int filesize = int.Parse(re[2]);

        //                Hashtable fileinfotable = new Hashtable();
        //                fileinfotable[re] = filename;

        //                //파일 수신 스레드 시작
        //                lock (filesock)
        //                {
        //                    filesock = new UdpClient(filesender);
        //                    if (!ThreadList.ContainsKey(re[3] + re[4]) || ThreadList[re[3] + re[4]] == null) //같은 파일에 대한 전송 쓰레드가 시작되지 않았다면
        //                    {
        //                        Thread filereceiver = new Thread(new ParameterizedThreadStart(FileReceiver));
        //                        filereceiver.Start(fileinfotable);
        //                        ThreadList[re[3] + re[4]] = filereceiver;
        //                        SendMsg("Y|" + re[1] + "|" + re[3] + "|" + re[5], reSocket);  //re[5]==all 의 경우 전체에 대한 파일 전송
        //                        ErrorConnClear();
        //                    }
        //                }
        //                break;

        //            case 3:   //사용자 로그인 상태 체크요청(코드번호|id)
        //                re = msg.Split('|');
        //                if (InClientList.ContainsKey(re[1]) && InClientList[re[1]] != null)
        //                {
        //                    SendMsg("+|", (Socket)InClientList[re[1]]);
        //                }
        //                else
        //                {
        //                    lock (InClientList)
        //                    {
        //                        Logout(re[1]);
        //                    }
        //                }
        //                break;

        //            case 6:  //공지사항 전달(6|메시지|발신자id|n 또는 e|noticetime)  n : 일반공지 , e : 긴급공지
        //                re = msg.Split('|');
        //                Socket rsock = null;

        //                foreach (var de in InClientList)
        //                {
        //                    if (de.Value != null)
        //                    {
        //                        rsock = (Socket)de.Value;
        //                        logWrite("공지사항 전송 : " + de.Key.ToString());
        //                        SendMsg("n|" + re[1] + "|" + re[2] + "|" + re[3] + "|" + re[4], rsock);
        //                    }
        //                }
        //                ErrorConnClear();
        //                GetNoticeListDelegate notice = new GetNoticeListDelegate(GetNoticeList);
        //                ArrayList list = (ArrayList)Invoke(notice, null);

        //                InsertNotice(list, re[1], re[2], re[3]);

        //                break;

        //            case 9://로그아웃             

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                //로그아웃 처리
        //                lock (InClientList)
        //                {
        //                    Logout(re[1]);
        //                }

        //                break;

        //            case 7:  //안읽은 메모 요청

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                ArrayList memolist = ReadMemo(re[1]);
        //                string cmsg = "Q";
        //                if (memolist != null && memolist.Count != 0)
        //                {
        //                    foreach (object obj in memolist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3];
        //                            cmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(cmsg, reSocket);

        //                break;

        //            case 10:  //안받은 파일 요청
        //                re = msg.Split('|'); //msg 구성 : 코드번호|id
        //                GetInformation getName = new GetInformation(GetName);
        //                ArrayList filelist = ReadFile(re[1]);
        //                string fmsg = "R";
        //                if (filelist != null && filelist.Count != 0)
        //                {
        //                    foreach (object obj in filelist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3] + ";" + array[4] + ";" + array[5];
        //                            fmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(fmsg, reSocket);
        //                ErrorConnClear();
        //                break;

        //            case 11:  //안읽은 공지 요청(11|id)
        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                ArrayList noticelist = ReadNotice(re[1]);
        //                string nmsg = "T";
        //                if (noticelist != null && noticelist.Count != 0)
        //                {
        //                    foreach (object obj in noticelist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3] + ";" + array[4];
        //                            nmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(nmsg, reSocket);
        //                ErrorConnClear();
        //                break;

        //            case 12: //파일 전송 요청
        //                re = msg.Split('|'); //msg 구성 : 12|filenum

        //                //StartSendFile(re[1], );
        //                break;

        //            case 13:  //보낸 공지 리스트 요청
        //                re = msg.Split('|');  //13|id

        //                SelectNoticeList(re[1]);
        //                break;

        //            case 14: //읽은 정보 삭제 요청(14|seqnum)

        //                re = msg.Split('|');

        //                DeleteNoreceive(re[1]);

        //                break;

        //            case 15://관리자 공지 삭제(15|seqnum;seqnum;seqnum;...)
        //                re = msg.Split('|');
        //                string[] msg_array = re[1].Split(';');
        //                DeleteNotice(msg_array);
        //                break;

        //            case 16://대화 메시지 전달(16|Formkey|id/id/..|발신자name|메시지 )
        //                re = msg.Split('|');
        //                string fwdmsg = "d" + msg.Substring(2);
        //                string[] party = re[2].Split('/');
        //                foreach (string item in party)
        //                {
        //                    Socket partySocket = (Socket)InClientList[item];
        //                    if (partySocket == null || partySocket.Connected == false)
        //                    {

        //                    }
        //                    else
        //                    {
        //                        SendMsg(fwdmsg, partySocket);
        //                    }
        //                }

        //                break;
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}
        #endregion

        public void receiveReq(object Obj)
        {
            try
            {
                LogWrite("receiveReq thread 시작!");
                Thread.Sleep(10);
                ArrayList List = (ArrayList)Obj;
                int mode = (int)List[0];
                string msg = (string)List[1];
                IPEndPoint iep = null;
                if (List.Count > 2)
                {
                    iep = (IPEndPoint)((Socket)List[2]).RemoteEndPoint;
                }
                string[] re = null;

                switch (mode)
                {
                    #region //8 로그인
                    case 8://로그인
                        LogWrite("case 8 로그인 요청");

                        re = msg.Split('|'); //msg 구성 : 코드번호|id|비밀번호|내선번호|ip주소
                        //iep = new IPEndPoint(IPAddress.Parse(re[4]), 8883);
                        Login(re, iep);

                        break;
                    #endregion

                    #region //2 중복 로그인 상황에서 기존접속 끊기 선택한 경우
                    case 2: //중복 로그인 상황에서 기존접속 끊기 선택한 경우
                        re = msg.Split('|');  //msg 구성 : 2|id

                        SendMsg("u|", (IPEndPoint)InClientList[re[1]]); //기존 접속 원격 로그아웃 
                        Logout(re[1]);      //기존 접속 서버측 로그아웃
                        break;
                    #endregion

                    #region //1 공지사항 목록 요청
                    case 1: //공지사항 목록 요청(1|id)

                        re = msg.Split('|');
                        if (InClientList.Count != 0)
                        {
                            if (InClientList[re[1]] != null)
                            {
                                iep = (IPEndPoint)InClientList[re[1]];
                                SelectNoticeAll(re[1]);
                            }
                            else
                            {
                                LogWrite("InClientList[" + re[1] + "] is null");
                            }
                        }
                        else
                        {
                            LogWrite("InClientList is empty");
                        }
                        break;
                    #endregion

                    #region //0 대화종료
                    case 0: //대화종료
                        break;
                    #endregion

                    #region //4 메모 처리
                    case 4: //메모 처리
                        LogWrite("메모처리");

                        re = msg.Split('|'); //msg 구성 : 4|name|발신자id|메시지|수신자id

                        if (re[1].Equals("m")) //쪽지 전송 실패로 서버측으로 메시지 반송시(4|m|name|발신자id|메시지|수신자id)
                        {
                            InsertNoReceive(re[5], "", re[4], re[1], re[3], "x");
                        }
                        else
                        {
                            InsertNoReceive(re[4], "", re[3], "m", re[2], "x");
                        }

                        break;
                    #endregion

                    #region //5 파일 전송 메시지
                    case 5: //파일 전송 메시지(5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
                        //re = msg.Split('|');
                        //string[] filenameArray = re[1].Split('\\');
                        //string filename = filenameArray[(filenameArray.Length - 1)];
                        //int filesize = int.Parse(re[2]);

                        //Hashtable fileinfotable = new Hashtable();
                        //fileinfotable[re] = filename;

                        ////파일 수신 스레드 시작
                        //if (filesock == null)
                        //{
                        //    filesock = new UdpClient(filesender);
                        //}

                        //lock (filesock)
                        //{
                        //    if (!ThreadList.ContainsKey(re[3] + re[4]) || ThreadList[re[3] + re[4]] == null) //같은 파일에 대한 전송 쓰레드가 시작되지 않았다면
                        //    {
                        //        Thread filereceiver = new Thread(new ParameterizedThreadStart(FileReceiver));
                        //        filereceiver.Start(fileinfotable);
                        //        ThreadList[re[3] + re[4]] = filereceiver;
                        //        SendMsg("FS|" + re[1] + "|" + re[3] + "|" + re[5], iep);  //re[5]==all 의 경우 전체에 대한 파일 전송
                        //    }
                        //}
                        break;
                    #endregion

                    #region //3 사용자 로그인 상태 체크요청
                    case 3: //사용자 로그인 상태 체크요청(코드번호|id)
                        //re = msg.Split('|');
                        //if (InClientList.ContainsKey(re[1]) && InClientList[re[1]] != null)
                        //{
                        //    SendMsg("+|", (IPEndPoint)InClientList[re[1]]);
                        //}
                        //else
                        //{

                        //    Logout(re[1]);

                        //}
                        break;
                    #endregion

                    #region //6 공지사항 전달
                    case 6: //공지사항 전달(6|메시지|발신자id | n 또는 e | noticetime | 제목)  n : 일반공지 , e : 긴급공지
                        re = msg.Split('|');
                        IPEndPoint niep = null;

                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                niep = (IPEndPoint)de.Value;
                                LogWrite("공지사항 전송 : " + de.Key.ToString());
                                SendMsg("n|" + re[1] + "|" + re[2] + "|" + re[3] + "|" + re[4] + "|" + re[5], niep);
                            }
                        }
                        GetNoticeListDelegate notice = new GetNoticeListDelegate(GetNoticeList);
                        ArrayList list = (ArrayList)Invoke(notice, null);

                        InsertNotice(list, re[4], re[1], re[2], re[3], re[5]);

                        break;
                    #endregion

                    #region //9 로그아웃
                    case 9://로그아웃

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        //로그아웃 처리

                        //SendMsg("u|", (IPEndPoint)InClientList[re[1]]);
                        Logout(re[1]);


                        break;
                    #endregion

                    #region //7 안읽은 메모 요청
                    case 7: //안읽은 메모 요청

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList memolist = ReadMemo(re[1]);
                        string cmsg = "Q";
                        if (memolist != null && memolist.Count != 0)
                        {
                            foreach (object obj in memolist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                    cmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(cmsg, iep);

                        break;
                    #endregion

                    #region //10 안받은 파일 요청
                    case 10: //안받은 파일 요청
                        re = msg.Split('|'); //msg 구성 : 코드번호|id
                        ArrayList filelist = ReadFile(re[1]);
                        string fmsg = "R";
                        if (filelist != null && filelist.Count != 0)
                        {
                            foreach (object obj in filelist)
                            {
                                string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                    fmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(fmsg, iep);

                        break;
                    #endregion

                    #region //11 안읽은 공지 요청
                    case 11: //안읽은 공지 요청(11|id)
                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList noticelist = ReadNotice(re[1]);
                        string nmsg = "T";
                        if (noticelist != null && noticelist.Count != 0)
                        {
                            foreach (object obj in noticelist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum, title }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                    nmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(nmsg, iep);
                        break;
                    #endregion

                    #region //12 파일 전송 요청
                    case 12: //파일 전송 요청
                        re = msg.Split('|'); //msg 구성 : 12|id|filenum

                        StartSendFile(re[2], re[1]);
                        break;
                    #endregion

                    #region //13 보낸 공지 리스트 요청
                    case 13: //보낸 공지 리스트 요청
                        re = msg.Split('|');  //13|id

                        SelectNoticeList(re[1]);
                        break;
                    #endregion

                    #region //14 읽은 정보 삭제 요청
                    case 14: //읽은 정보 삭제 요청(14|seqnum)

                        re = msg.Split('|');

                        DeleteNoreceive(re[1]);

                        break;
                    #endregion

                    #region //15 관리자 공지 삭제
                    case 15://관리자 공지 삭제(15|seqnum;seqnum;seqnum;...)
                        re = msg.Split('|');
                        string[] msg_array = re[1].Split(';');
                        DeleteNotice(msg_array);
                        break;
                    #endregion

                    #region //16 채팅메시지 전달
                    case 16://채팅메시지 전달(16|Formkey|id/id/..|발신자name|메시지 ) 구분자 : |(pipe) 

                        string chatmsg = "d" + msg.Substring(2);
                        re = msg.Split('|');
                        string[] ids = re[2].Split('/');
                        foreach (string iditem in ids)
                        {
                            if (InClientList.ContainsKey(iditem))
                            {
                                SendMsg(chatmsg, (IPEndPoint)InClientList[iditem]);
                            }
                        }
                        break;
                    #endregion

                    #region //17 추가한 상담원 리스트 기존 대화자에게 전송
                    case 17://추가한 상담원 리스트 기존 대화자에게 전송 (17|formkey|id/id/...|name|receiverID)

                        string amsg = "c" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[4]))
                        {
                            SendMsg(amsg, (IPEndPoint)InClientList[re[4]]);
                        }

                        break;
                    #endregion

                    #region //18 2명 이상과 대화중 폼을 닫은 경우
                    case 18: //2명 이상과 대화중 폼을 닫은 경우(q|Formkey|id|receiverID) 
                        string qmsg = "q" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[3]))
                        {
                            SendMsg(qmsg, (IPEndPoint)InClientList[re[3]]);
                        }

                        break;
                    #endregion

                    #region //19 쪽지 전송요청
                    case 19: //쪽지 전송요청(m|recName|recID|content|senderID);

                        string mmsg = "m" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[4]))
                        {
                            SendMsg(mmsg, (IPEndPoint)InClientList[re[4]]);
                        }

                        break;
                    #endregion

                    #region//20 상태변경 알림
                    case 20: //상태변경 알림(20|senderid|상태값)

                        string statmsg = "s" + msg.Substring(2);
                        re = msg.Split('|');
                        lock (InClientStat)
                        {
                            InClientStat[re[1]] = re[2];
                        }

                        statmsg += "|" + ((IPEndPoint)InClientList[re[1]]).Address.ToString();
                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(statmsg, (IPEndPoint)de.Value);
                            }
                        }
                        break;
                    #endregion

                    #region //21 공지 읽음 확인 메시지
                    case 21: //공지 읽음 확인 메시지(21 | receiverid | notice id | sender id)

                        string Nmsg = "C" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[3]))
                        {
                            SendMsg(Nmsg, (IPEndPoint)InClientList[re[3]]);
                        }
                        break;
                    #endregion

                    #region//22 고객정보 전달시도
                    case 22://고객정보 전달시도(22&ani&senderID&receiverID&일자&시간&CustomerName) 이관
                        string passmsg = "pass" + msg.Substring(2);
                        re = msg.Split('&');
                        passmsg = passmsg.Replace('&', '|');
                        passmsg += "|" + getCustomerNM(re[1]);
                        LogWrite("passmsg : " + passmsg);
                        if (InClientList.ContainsKey(re[3]))
                        {
                            if (InClientList[re[3]] != null)
                            {
                                SendMsg(passmsg, (IPEndPoint)InClientList[re[3]]);
                                //SendMsg(passmsg, (IPEndPoint)InClientList[re[2]]);
                            }
                            else
                            {
                                InsertNoReceive(re[3], "N/A", msg, "t", re[2], "t");
                            }
                        }
                        else
                        {
                            InsertNoReceive(re[3], "N/A", msg, "t", re[2], "t");
                        }

                        if (CustomerCacheReload == false)
                            reloadCustomerListCache();
                        break;
                    #endregion

                    #region//23 안읽은 이관 요청
                    case 23:  //안읽은 이관 요청

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList transflist = ReadTransfer(re[1]);
                        string tmsg = "trans";
                        if (transflist != null && transflist.Count != 0)
                        {
                            foreach (object obj in transflist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, seqnum, type }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                    tmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(tmsg, iep);

                        break;
                    #endregion

                    #region //24 FTP
                    case 24:
                        LogWrite("FTP요청:" + msg );
                        break;
                    #endregion
                    #region 디폴트
                    default:

                        LogWrite("잘못된 요청 코드 입니다. : " + mode.ToString());
                        break;
                    #endregion
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        /// <summary>
        /// CallControl에서 발생하는 이벤트메시지 처리
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="sInfo"></param>
        private void RecvMessage(string sEvent, string sInfo)
        {
            try
            {
                IPEndPoint iep = null;
                string userid = "";
                if (sEvent.Equals("Connect") || sEvent.Equals("disConnect"))
                {
                    LogWrite("Event : " + sEvent + "sInfo : " + sInfo + "\r\n");
                }
                #region NIC_SIP
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_SIP))
                {
                    string[] infoarr = sInfo.Split('|'); //sInfo(FROM | TO | Call_ID)
                    if (infoarr.Length > 1)
                    {
                        LogWrite("Event : " + sEvent + "  FROM : " + infoarr[0] + " TO : " + infoarr[1] + "\r\n");

                        //from, to 모두 4자리이하이면 내선 호전환으로 판단
                        if(infoarr[0].Length<5 && infoarr[1].Length<5)
                        {
                            LogWrite("Internal Call Event");
                            return;
                        }
                        //from, to 앞자리 3자리가 동일하면(070) 내선 호전환으로 판단
                        else if (infoarr[0].Substring(0, 3).Equals(infoarr[1].Substring(0, 3)))
                        {
                            LogWrite("Internal Call Event");
                            return;
                        }
                        //from 또는 call_id가 내선목록에 있으면 내선 호전환으로 판단
                        else if (ExtensionList.ContainsKey(infoarr[0]) && ExtensionList.ContainsKey(infoarr[2]))
                        {
                            LogWrite("Internal Call Event");
                            return;
                        }
                        //이슈: 내선호전환때도 팝업가능하도록
                        //옆의사람이 당겨받을때 팝업도 같이 되어야 한다.
                        //원래 다음과 같이 전화옴 01042047723 -> 07033332222
                        //01042047723 -> 07033332224
                        //

                        switch (sEvent)
                        {
                            case "Ringing":
                                string cname = "";

                                bool needAnswer = false;////Answer가 안들어오는 경우를 감안 Ringing=>Answer로 바로 처리 20130306   부천 대부 dk전용

                                //call_id가 있으면 오류
                                if (!CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    //로그인 내선목록에서 찾아 해당 사용자에게 전달
                                    cname = getCustomerNM(infoarr[0]);
                                    if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                    {
                                        iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                        needAnswer = SendRinging("Ring|" + infoarr[0] + "|" + cname + "|" + configCtrl.ServerType, iep);
                                    }

                                    //해당 콜에서 to(내선)가 사용자목록에 있으면 사용자명, 없으면 내선번호로 해서 insertCallLog등록
                                    //2건이상의 Ringing발생시 중복발생방지처리
                                    lock (CallLogTable)
                                    {
                                        if (!CallLogTable.ContainsKey(infoarr[2]))
                                        {
                                            CallLogTable[infoarr[2]] = "Ringing";  //중복방지
                                            
                                            insertCallLog(infoarr[1], infoarr[0], "1", infoarr[2], "1");
                                        }
                                    }

                                    if (CustomerCacheReload == false)
                                        reloadCustomerListCache();
                                }
                                else
                                {
                                    LogWrite(infoarr[2] + " is already inbounded");
                                }
                                if (!needAnswer)
                                    break;
                                break;  //Answer가 안들어오는 경우를 감안 Ringing=>Answer로 바로 처리 20130306   부천 대부 dk전용
                                Thread.Sleep(2000);
                            case "Answer":
                                //CallLogTable에 call_id로 건이 있고, "Ringing"으로 표현된 건에 대해 로그인한 내선번호로 전달하고
                                //"Ringing"flag을 시간값으로 변경
                                if (CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    if (CallLogTable[infoarr[2]].ToString().Equals("Ringing")) //응답시 Answer 중복 이벤트 처리
                                    {
                                        userid = infoarr[1];
                                        if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                        {
                                            iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                            SendMsg("Answer|" + infoarr[0] + "|" + "1", iep); //SIP 폰의 경우 직통전화이므로 바로 전송

                                            userid = GetUserNameByExtension(infoarr[1]);
                                        }

                                        insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "3");

                                        lock (CallLogTable)
                                        {
                                            CallLogTable[infoarr[2]] = Utils.TimeKey(); ;
                                        }
                                    }
                                    else
                                    {
                                        LogWrite(infoarr[2] + " is already answered");
                                    }
                                }
                                else
                                {
                                    LogWrite(infoarr[2] + " is already invalid callid");
                                }
                                break;

                            case "CallConnect": //발신 후 연결

                                lock (CallLogTable)
                                {
                                    CallLogTable[infoarr[2]] = Utils.TimeKey();
                                }
                                break;

                            case "Dialing":
                                //로그인된 내선번호로 호전환 신호 전달
                                lock (CallLogTable)
                                {
                                    if (!CallLogTable.ContainsKey(infoarr[2]))
                                    {
                                        if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[0]))
                                        {
                                            iep = (IPEndPoint)ExtensionList[infoarr[0]];
                                            SendMsg("Dial|" + infoarr[1] + "|", iep);

                                        }
                                        CallLogTable[infoarr[2]] = Utils.TimeKey();
                                        insertCallLog(infoarr[0], infoarr[1], "2", infoarr[2], "2");
                                    }
                                }
                                break;

                            case "Abandon":
                                //CallLogTable에 건이 있고, "A"라는 flag이 아니면 해당 로그인한 내선번호에 전달하고 CallLogTable에서 제거
                                lock (CallLogTable)
                                {
                                    if (CallLogTable.ContainsKey(infoarr[2]))
                                    {
                                        if (!CallLogTable[infoarr[2]].ToString().Equals("A"))
                                        {
                                            if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                            {
                                                iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                                SendMsg("Abandon|" + infoarr[0], iep);

                                            }

                                            userid = GetUserNameByExtension(infoarr[1]);

                                            insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "4");
                                            CallLogTable.Remove(infoarr[2]);
                                        }
                                    }
                                    else
                                    {
                                        LogWrite(infoarr[2] + " is invalid callid");
                                    }
                                }

                                break;

                            case "HangUp":
                                //[로그인사용자가 있는 경우]
                                //1. From이 로그인 상태인 경우 inserCallLog2 calltype=2, call_result=5로 처리
                                //2. From이 외부또는 로그아웃, To가 로그인인 경우   inserCallLog2 calltype=1, call_result=5로 처리
                                //3. From, To모두 외부또는 로그아웃인경우 
                                //    ==> 자릿수로 판단하여 To가 내선인 경우     inserCallLog2 calltype=1, call_result=5로 처리
                                //    ==> 자릿수로 판단하여 From이 내선인 경우 inserCallLog2 calltype=2, call_result=5로 처리
                                //[로그인사용자가 없는 경우]
                                //    ==> 자릿수로 판단하여 To가 내선인 경우     inserCallLog2 calltype=1, call_result=5로 처리
                                //    ==> 자릿수로 판단하여 From이 내선인 경우 inserCallLog2 calltype=2, call_result=5로 처리
                                if (CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    LogWrite("통화종료 이벤트!");
                                    //From이 로그인 상태인 경우
                                    if ((ExtensionList.Count > 0) && ExtensionList.ContainsKey(infoarr[0]))
                                    {
                                        userid = GetUserNameByExtension(infoarr[0]);

                                        insertCallLog2(infoarr[2], infoarr[0], infoarr[1], userid, "2", "5");
                                    }
                                    //From이 외부또는 로그아웃, To가 로그인인 경우
                                    else if ((ExtensionList.Count > 0) && ExtensionList.ContainsKey(infoarr[1])) //TO == 사용자일 경우
                                    {
                                        userid = GetUserNameByExtension(infoarr[1]);

                                        insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "5");
                                    }
                                    //From, To모두 외부또는 로그아웃인경우
                                    else
                                    {
                                        int numlen = infoarr[0].Length - infoarr[1].Length; //리스트에 없는 경우(해당 내선사용자 로그아웃) 짧은 번호를 사용자로 판단
                                        //자릿수로 판단하여 To가 내선인 경우
                                        if (numlen > 0)
                                        {
                                            userid = GetUserNameByExtension(infoarr[1]);

                                            insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "5");
                                        }
                                        //자릿수로 판단하여 From이 내선인 경우
                                        else
                                        {
                                            userid = GetUserNameByExtension(infoarr[0]);

                                            insertCallLog2(infoarr[2], infoarr[0], infoarr[1], userid, "2", "5");
                                        }
                                    }
                           
                                }
                                else
                                {
                                    LogWrite(infoarr[2] + " is already invalid callid");
                                }
                                break;
                        }
                    }
                }
                #endregion //NIC_SIP
                #region NIC_LG_KP
                //KP은 Ringing/Answer만 존재
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP)) //CID 장치 또는 KEYPHONE
                {
                    LogWrite("Event : " + sEvent + "  sInfo : " + sInfo + "\r\n");
                    string[] infoarr = sInfo.Split('>');
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss") +"$"+ infoarr[0];
                    switch (sEvent)
                    {
                        case "Ringing":
                            //전체 로그인사용자에게 전달하고 "all"로 로그남김
                            string cname = "";
                            cname = getCustomerNM(sInfo);
                            LogWrite("고객이름 : " + cname);
                            foreach (var de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Ring|" + sInfo + "|" + cname + "|" + configCtrl.ServerType, (IPEndPoint)de.Value);
                                }
                            }

                            if (CustomerCacheReload == false)
                                reloadCustomerListCache();

                            CallLogTable[call_id] = infoarr[0];
                            insertCallLog("All", infoarr[0], "1", call_id, "1");

                            break;

                        case "Answer":
                            //1. 인입호를 다른 클라이언트가 수신한것으로 처리
                            //2. 수신한 내선번호에 Answer로 처리
                            //3. CallLogTable에 해당ani값이 있는지 확인후 insertCallLog3하고 CallLogTable에서 제거 

                            if (infoarr.Length > 1)
                            {
                                LogWrite("받은 내선 : " + infoarr[1]);

                                foreach (var de in InClientList)
                                {
                                    if (de.Value != null)
                                    {
                                        SendRinging("Other|", (IPEndPoint)de.Value);
                                    }
                                }

                                if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                {
                                    iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                    SendMsg("Answer|" + infoarr[0] + "|" + "1", iep);
                                }

                                if (CallLogTable.ContainsValue(infoarr[0])) //CallLogTable[call_id] = ani
                                {
                                    foreach (var de in CallLogTable)
                                    {
                                        if (de.Value.ToString().Equals(infoarr[0]))
                                        {
                                            userid = GetUserNameByExtension(infoarr[1]);

                                            insertCallLog3(call_id, infoarr[1], infoarr[0], userid, "1", "3");
                                            CallLogTable.Remove(de.Key);
                                            break;
                                        }
                                    }

                                }
                                else
                                {
                                    LogWrite("CallLogTable 에 Ringing 정보 없음");
                                }
                            }
                            break;
                    }
                }
                #endregion
                #region NIC_CID
                //CID 1포트는 Ringing/OffHook/OnHook 모두 존재하나, 
                //CID 2,4포트는 Ringing 만 존재
                //CID는 발신번호만 알수있다. 내선번호가 존재하지 않음.
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT1)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT2)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT4))
                {
                    LogWrite("Event : " + sEvent + "  sInfo : " + sInfo + "\r\n");
                    string[] infoarr = sInfo.Split('>');
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss") +"$"+ infoarr[0];
                    switch (sEvent)
                    {

                        case "Ringing" :
                            //로그인사용자 모두에게 ringing전달
                            //all로 insertCallLog남김
                            string cname = "";
                            cname = getCustomerNM(sInfo);
                            LogWrite("고객이름 : " + cname);
                            foreach (var de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Ring|" + sInfo + "|" + cname + "|" + configCtrl.ServerType, (IPEndPoint)de.Value);
                                }
                            }

                            if (CustomerCacheReload == false)
                                reloadCustomerListCache();

                            CallLogTable[call_id] = infoarr[0];
                            insertCallLog("All", infoarr[0], "1", call_id, "1");

                            break;

                        case "OffHook" :
                            //1. 인입호를 다른 클라이언트가 수신한것으로 처리
                            //2. CallLogTable에 해당ani값이 있는지 확인후 insertCallLog3하고 CallLogTable에서 제거 
                            //3. 로그인사용자 모두에게 answer처리 => 고객팝업
                            foreach (var de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Other|", (IPEndPoint)de.Value);
                                }
                            }

                            if (CallLogTable.Count > 0) //CallLogTable[call_id] = ani
                            {
                                foreach (var logitem in CallLogTable)
                                {
                                    //해당 ani에 대해서만 처리
                                    if (logitem.Value.ToString().Equals(infoarr[0]))
                                    {
                                        //로그인사용자 모두에게 answer처리 => 고객팝업
                                        foreach (var clientItem in InClientList)
                                        {
                                            if (clientItem.Value != null)
                                            {
                                                SendMsg("Answer|" + logitem.Value.ToString() + "|" + "1", (IPEndPoint)clientItem.Value);
                                            }
                                        }
                                    }

                                    insertCallLog3(call_id, "All", logitem.Value.ToString(), "All", "1", "3");
                                    CallLogTable.Remove(logitem.Key);
                                    break;
                                }

                            }
                            else
                            {
                                LogWrite("CallLogTable 에 Ringing 정보 없음");
                            }

                            break;

                        case "OnHook" :


                            break;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                LogWrite("RecvMessage Exception : " + ex.ToString());
            }
        }

        /// <summary>
        /// 내선번호목록에서 사용자 정보를 찾아  [userid].[username]값을 리턴
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>[userid].[username]값을 리턴, 못찾는 경우 원 내선번호값 리턴</returns>
        private string GetUserNameByExtension(string extension)
        {
            string result = "";
            if (ExtensionIDpair.ContainsKey(extension))
            {
                string tempid = ExtensionIDpair[extension].ToString();

                Client cinfo = (Client)ClientInfoList[tempid];
                result = tempid + "." + cinfo.getName();
            }
            else
            {
                result = extension;
            }
            return result;
        }

        private string getCustomerNM(string ani)
        {
            string value = getCustomerInfo(ani);
            string[] valArr = value.Split('$');
            string customerName = "";
            if (valArr.Length == 2)
                customerName = valArr[1];
            return customerName;
        }

        private string getCustomerInfo(string ani)
        {
            string cname = "";
            try
            {
                if (CustomerCacheSwitch == false)
                {
                    if (CustomerList_Primary.ContainsKey(ani))
                    {
                        cname = CustomerList_Primary[ani].ToString();
                        LogWrite("고객이름 찾음 : " + cname);
                    }
                }
                else
                {
                    if (CustomerList_Backup.ContainsKey(ani))
                    {
                        cname = CustomerList_Backup[ani].ToString();
                        LogWrite("고객이름 찾음 : " + cname);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWrite("getCustomerNM() Exception : " + ex.ToString());
            }

            return cname;
        }

        /// <summary>
        /// ------------
        /// 콜통계 처리
        /// ------------
        /// 1. ringing -> 정상등록(customer_id, customer_nm포함)
        /// 2. abandon -> SIP :  call_result update (call_id가 키)
        /// 3. answer  -> SIP :  start_time , call_result, tong_user update (call_id가 키)
        ///               KP  :  start_time , call_result, tong_user, ext update (ani,start_time이 키, 가장 최근것) 
        ///               CID :  start_time , call_result   update (ani,start_time이 키, 가장 최근것) 
        /// 4. hangup  -> SIP :  start_time , end_time, duration, call_result update (call_id가 키)              
        /// 5. 상담완료-> *   :  consult_dd, consult_time update (ani, start_time이 키, answer인것 가장 최근것)
        /// --------------------------------
        /// 
        /// 1.콜로그 등록:Ringing/Dialing등 최초등록 건인 경우
        /// 2.콜통계 등록
        /// call_result(1: Ringing, 2: Dialing, 3: Answer, 4: Abandon, 5: Hangup)
        /// call_type  (1:인바운드, 2:아웃바운드, 3:내선통화, 4:기타 )
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="ani"></param>
        /// <param name="call_type"></param>
        /// <param name="call_id"></param>
        /// <param name="call_result"></param>
        private void insertCallLog(string ext, string ani, string call_type, string call_id, string call_result)
        {
            try
            {
                string startTime = Utils.TimeKey();

                Hashtable parameters = new Hashtable();

                parameters.Add("@com_cd", configCtrl.CompanyCode);
                parameters.Add("@starttime", startTime);
                parameters.Add("@ext_num", ext);
                parameters.Add("@call_type", call_type);
                parameters.Add("@call_result", call_result);
                parameters.Add("@ani", ani);
                parameters.Add("@call_id", call_id);

                if (configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP)) { parameters.Add("@pbx_type", "1"); }
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_SIP)) { parameters.Add("@pbx_type", "2"); }
                else { parameters.Add("@pbx_type", "3"); }

                string query = "insert into t_call_history" +
                "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, PBX_TYPE) " +
                "VALUES(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @pbx_type)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite("insertCallLog 실패: " + call_id + " 콜로그 등록");
                    throw new Exception("insertCallLog 실패: " + call_id);
                }

                LogWrite("insertCallLog : " + call_id + " 콜로그 등록"); 

                //콜통계등록
                string value = getCustomerInfo(ani);
                string customerId = "0";
                string customerName = "";
                string[] valArr = value.Split('$');

                customerId = valArr[0].Equals("") ? "0" : valArr[0];
                if (valArr.Length == 2)
                    customerName = valArr[1];
                
                parameters.Add("@customer_id", customerId);
                parameters.Add("@customer_nm", customerName);

                query = "insert into t_call_history_stat" +
                "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, PBX_TYPE, CUSTOMER_ID, CUSTOMER_NM) " +
                "VALUES(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @pbx_type, @customer_id, @customer_nm)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite("insertCallLog 실패: " + call_id + " 콜통계 등록");
                    throw new Exception("insertCallLog 실패: " + call_id);
                }

                LogWrite("insertCallLog : " + call_id + " 콜통계 등록"); 

            }
            catch (Exception ex)
            {
                LogWrite("insertCallLog Exception : " + ex.ToString());
            }

        }

        /// <summary>
        /// 1.콜로그 등록
        /// 2.콜통계 등록
        /// SIP answer/Hangup/abandon인 경우
        /// --------------------------------------------------------------------
        /// call_result(1: Ringing, 2: Dialing, 3: Answer, 4: Abandon, 5: Hangup)
        /// call_type  (1:인바운드, 2:아웃바운드, 3:내선통화, 4:기타 )
        /// </summary>
        /// <param name="call_id"></param>
        /// <param name="extension"></param>
        /// <param name="ani"></param>
        /// <param name="user"></param>
        /// <param name="call_type"></param>
        /// <param name="call_result"></param>
        private void insertCallLog2(string call_id, string extension, string ani, string user, string call_type, string call_result) //Answer or HangUp or Abandon
        {
            try
            {
                string start_time;
                string call_start;
                string call_end;
                int call_duration = 0;
                string result_time = Utils.TimeKey();
                Hashtable parameters = new Hashtable();

                parameters.Add("@com_cd", configCtrl.CompanyCode);
                parameters.Add("@ext_num", extension);
                parameters.Add("@call_type", call_type);
                parameters.Add("@call_result", call_result);
                parameters.Add("@ani", ani);
                parameters.Add("@call_id", call_id);
                parameters.Add("@userid", user);

                if (configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP)) { parameters.Add("@pbx_type", "1"); }
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_SIP)) { parameters.Add("@pbx_type", "2"); }
                else /*                CID                  */ { parameters.Add("@pbx_type", "3"); }

                
                if (call_result.Equals("3")) // answer
                {
                    if (CallLogTable.ContainsKey(call_id))
                    {
                        call_start = result_time;
                        LogWrite("call_start = " + call_start);

                        CallLogTable[call_id] = result_time;

                        parameters.Add("@starttime", call_start);
                        
                        string query = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                        "VALUES(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜로그 등록");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜로그 등록");

                        //콜통계등록
                        query = "update t_call_history_stat" +
                        " set TONG_START_TIME=@starttime, CALL_RESULT=@call_result, TONG_USER=@userid " +
                        "where CALL_ID=@call_id";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜통계 갱신");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜통계 갱신"); 

                    }
                    else
                    {
                        LogWrite("CallLogTable 에 해당 키 없음 : " + call_id);
                    }
                }
                else if (call_result.Equals("4")) // abandon
                {
                    call_start = result_time;
                    LogWrite("call_start = " + call_start);
                    
                    if (CallLogTable.ContainsKey(call_id))
                    {
                        CallLogTable.Remove(call_id);

                        parameters.Add("@starttime", call_start);
                        
                        string query = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                        "VALUES(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜로그 등록");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜로그 등록");

                        //콜통계등록
                        query = "update t_call_history_stat" +
                        " set CALL_RESULT=@call_result" +
                        "where CALL_ID=@call_id";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜통계 갱신");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜통계 갱신");
                    }
                }
                else if (call_result.Equals("5"))//released
                {
                    call_end = result_time;
                    LogWrite("call_end = " + call_end);

                    if (CallLogTable.ContainsKey(call_id))
                    {
                        start_time = CallLogTable[call_id];
                        call_start = start_time;
                        LogWrite("call_start = " + call_start);
                        CallLogTable.Remove(call_id);

                        call_duration = Utils.TimeGap(result_time, start_time);

                        parameters.Add("@starttime", call_start);
                        parameters.Add("@endtime", call_end);
                        parameters.Add("@duration", Convert.ToString(call_duration));

                        string query = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, TONG_END_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, TONG_DURATION, PBX_TYPE) " +
                        "VALUES(@com_cd, @starttime, @endtime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @duration, @pbx_type)";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜로그 등록");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜로그 등록");

                        //콜통계등록
                        query = "update t_call_history_stat" +
                        " set TONG_END_TIME=@endtime, TONG_DURATION=@duration, CALL_RESULT=@call_result " +
                        "where CALL_ID=@call_id";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("insertCallLog2 실패: " + call_id + " 콜통계 갱신");
                            throw new Exception("insertCallLog2 실패: " + call_id);
                        }

                        LogWrite("insertCallLog2 : " + call_id + " 콜통계 갱신");
                    }
                }

            }
            catch (Exception ex)
            {
                LogWrite("insertCallLog2 Exception : " + ex.ToString());
            }
        }

        /// <summary>
        /// 콜로그 등로: CID/KP 용 Answer일때 처리
        /// SIP는 CallLogTable에서 call_id 를 검색
        /// 
        /// call_result(1: Ringing, 2: Dialing, 3: Answer, 4: Abandon, 5: Hangup)
        /// call_type  (1:인바운드, 2:아웃바운드, 3:내선통화, 4:기타 )
        /// </summary>
        /// <param name="call_id"></param>
        /// <param name="extension"></param>
        /// <param name="ani"></param>
        /// <param name="user"></param>
        /// <param name="call_type"></param>
        /// <param name="call_result"></param>
        private void insertCallLog3(string call_id, string extension, string ani, string user, string call_type, string call_result) //Answer or HangUp or Abandon
        {
            try
            {
                string call_start = Utils.TimeKey();
                string call_end = "";
                int call_duration = 0;
                DateTime result_time = DateTime.Now;

                Hashtable parameters = new Hashtable();

                if (call_result.Equals("3")) // answer
                {

                    parameters.Add("@com_cd", configCtrl.CompanyCode);
                    parameters.Add("@starttime", call_start);
                    parameters.Add("@ext_num", extension);
                    parameters.Add("@call_type", call_type);
                    parameters.Add("@call_result", call_result);
                    parameters.Add("@ani", ani);
                    parameters.Add("@call_id", call_id);
                    parameters.Add("@userid", user);

                    if (configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP)) { parameters.Add("@pbx_type", "1"); }
                    else if (configCtrl.ServerType.Equals(ConstDef.NIC_SIP)) { parameters.Add("@pbx_type", "2"); }
                    else /*                CID                  */ { parameters.Add("@pbx_type", "3"); }

                    string query = "insert into t_call_history" +
                    "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                    "VALUES(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                    if (DoExecute(query, parameters) < 1)
                    {
                        LogWrite("insertCallLog3 실패: " + call_id + " 콜로그 등록");
                        throw new Exception("insertCallLog3 실패: " + call_id);
                    }

                    LogWrite("insertCallLog3 : " + call_id + " 콜로그 등록");

                    //콜통계등록  KP, CID 모두 포함
                    query = "update t_call_history_stat"
                    + " set TONG_START_TIME=@starttime, EXTENSION_NO=@ext_num, TONG_USER=@userid, CALL_RESULT=@call_result "
                    +"where ANI=@ani"
                    + "  and CALL_ID = (select max(CALL_ID) from t_call_history_stat where ANI=@ani and CALL_RESULT=1) ";

                    if (DoExecute(query, parameters) < 1)
                    {
                        LogWrite("insertCallLog3 실패: " + call_id + " 콜통계 갱신");
                        throw new Exception("insertCallLog3 실패: " + call_id);
                    }

                    LogWrite("insertCallLog3 : " + call_id + " 콜통계 갱신");

                }
            }
            catch (Exception ex)
            {
                LogWrite("insertCallLog3 Exception : " + ex.ToString());
            }
        }

        private DataTable DoQuery(string query, Hashtable parameters)
        {
            DataTable dt = new DataTable();

            try
            {
                dbHandler.Open();
                dbHandler.SetQuery(query);

                if (parameters != null)
                {
                    foreach (string key in parameters.Keys)
                    {
                        dbHandler.Parameters(key, (string)parameters[key]);
                    }
                }

                dt = dbHandler.DoQuery();
            }
            catch (Exception e)
            {
                LogWrite("쿼리실행에러 : " + e.ToString());
                throw new Exception("쿼리실행에러");
            }
            finally
            {
                dbHandler.Close();
            }
            return dt;
        }

        private void DbHandlerClose()
        {
            try
            {
                dbHandler.Close();
            }
            catch (Exception e)
            {
                LogWrite("DbHandlerClose : " + e.ToString());
            }
            finally
            {
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private int DoExecute(string query, Hashtable parameters)
        {
            int result = 0;

            try
            {
                dbHandler.Open();
                dbHandler.SetQuery(query);

                if (parameters != null)
                {
                    foreach (string key in parameters.Keys)
                    {
                        dbHandler.Parameters(key, (string)parameters[key]);
                    }
                }

                result = dbHandler.DoExecute();
            }
            catch (Exception e)
            {
                LogWrite("트랜잭션 실행에러 : " + e.ToString());
                throw new Exception("트랜잭션 실행에러");
            }
            finally
            {
                dbHandler.Close();
            }
            return result;

        }

        private Hashtable GetMember(string DBType)
        {
            Hashtable result = null;
            try
            {
                switch (DBType)
                {
                    case "my":
                        result = readMemberFromMySql();
                        break;

                    case "ms":
                        //result = readMemberFromMSSql();
                        break;
                };
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
            return result;
        }

        #region readMemberFromMSSql()...

        //private Hashtable readMemberFromMSSql()
        //{
        //    SqlConnection conn = GetSqlConnection();
        //    try
        //    {
        //        conn.Open();
        //        logWrite("DB 접속 성공!(DB HOST : " + WDdbHost + " DB NAME : " + WDdbName + ")");
        //    }
        //    catch (Exception open)
        //    {
        //        logWrite("GetMember() conn.Open() 에러 :" + open.ToString());
        //    }

        //    SqlCommand cmd = new SqlCommand();
        //    cmd.Connection = conn;
        //    string cmdstring = "select info.user_cd, info.user_nm, code.team_nm from tbl_user_inf as info, tbl_team_cd as code where info.team_cd=code.team_cd";
        //    cmd.CommandText = cmdstring;
        //    cmd.CommandType = CommandType.Text;

        //    SqlDataReader reader = null;
        //    try
        //    {
        //        reader = cmd.ExecuteReader();
        //    }
        //    catch (Exception re)
        //    {
        //        logWrite("GetMember() ExecuteReader() 에러 : " + re.ToString());
        //    }

        //    Hashtable clientList = new Hashtable();

        //    try
        //    {
        //        if (reader.HasRows == true)
        //        {
        //            logWrite("GetMember() : 읽어오기 성공!");
        //            while (reader.Read())
        //            {
        //                string id = reader.GetString(0);
        //                string name = reader.GetString(1);
        //                string team = reader.GetString(2);
        //                string com_nm = reader.GetString(3);

        //                Client cl = new Client(id, name, team, "Unknown", com_nm);
        //                clientList.Add(id, cl);

        //            }
        //            conn.Close();
        //            logWrite("conn.Close!");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logWrite("ShowMemList() SqlDataReader 에러 : " + e.ToString());
        //    }

        //    return clientList;
        //}
        #endregion

        private Hashtable readMemberFromMySql()
        {
            Hashtable clientList = new Hashtable();
            DataTable dt = new DataTable();

            try
            {
                string query = "select u.user_id, u.user_nm, ifnull(u.team_nm, '') team_nm, c.com_nm from t_user as u, t_company as c where u.com_cd=c.com_cd";

                dt = DoQuery(query, null);

                LogWrite("readMemberFromMySql() : 사용자 목록 읽어오기");
                foreach (DataRow row in dt.Rows)
                {
                    string id = row["user_id"].ToString();
                    string name = row["user_nm"].ToString();
                    string team = row["team_nm"].ToString();
                    string com_nm = row["com_nm"].ToString();
                    LogWrite(id + "|" + name + "|" + team);
                }
            }
            catch (Exception ex)
            {
                LogWrite("readMemberFromMySql() 에러 : " + ex.ToString());
            }
            return clientList;
        }

        private string GetPresence(string statnum)
        {
            string statstr = statnum;
            try
            {
                switch (statnum)
                {
                    case "2":  //작업

                        statstr = "작업";
                        break;

                    case "3": //대기

                        statstr = "대기";
                        break;

                    case "4":  //작업

                        statstr = "작업";
                        break;

                    case "5":  //보류

                        statstr = "통화";
                        break;

                    case "6": //로그아웃

                        statstr = "로그아웃";
                        break;

                    case "11": //인바운드
                        statstr = "통화";
                        break;

                    case "12": //아웃바운드
                        statstr = "통화";
                        break;

                    case "13": //협의통화
                        statstr = "통화";
                        break;

                    case "14": //내선통화
                        statstr = "통화";
                        break;

                    case "41":  //휴식
                        statstr = "휴식";
                        break;

                    case "42":  //식사
                        statstr = "식사";
                        break;

                    case "43":  //교육
                        statstr = "교육";
                        break;

                    case "44":  //기타
                        statstr = "이석";
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
            return statstr;
        }

        private void ChangePresence(object obj)
        {
            try
            {
                ArrayList list = (ArrayList)obj;
                string IPaddr = (string)list[0];
                string statnum = (string)list[1];
                string presence = GetPresence(statnum);
                string statid = null;

                if (InClientList != null && InClientList.Count != 0)
                {
                    foreach (var de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            string addr = ((IPEndPoint)de.Value).Address.ToString();
                            if (addr.Equals(IPaddr))
                            {
                                //logWrite(IPaddr);
                                //logWrite(addr);
                                statid = de.Key.ToString();

                                break;
                            }
                        }
                    }
                }

                if (InClientList != null && InClientList.Count != 0)
                {
                    foreach (var de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            SendMsg("s|" + statid + "|" + presence, (IPEndPoint)de.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }

        //private void FileReceiver(object obj)
        //{
        //    try
        //    {
        //        Hashtable fileinfo = (Hashtable)obj;
        //        string filename = null;
        //        int filesize = 0;
        //        int size = 0;
        //        string[] ids = null;
        //        string[] msg = null;
        //        string filesender = null;
        //        foreach (var de in fileinfo)
        //        {
        //            filename = (string)de.Value;
        //            logWrite("FileReceiver() : filename=" + filename);

        //            msg = (string[])de.Key;  //5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
        //            ids = msg[5].Split(';');
        //        }
        //        filesender = msg[4];
        //        string fsize = msg[2];
        //        filesize = int.Parse(msg[2]);
        //        byte[] buffer = null;

        //        string filelocation = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString();
        //        DirectoryInfo dinfo = new DirectoryInfo(filelocation);
        //        if (dinfo.Exists == false)
        //        {
        //            dinfo.Create();
        //        }
        //        string tempfilename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + filename;
        //        string filesavename = null;
        //        FileInfo fi = new FileInfo(tempfilename);
        //        bool ok = false;
        //        int num=0;
        //        if (fi.Exists == true)
        //        {
        //            do
        //            {
        //                num++;
        //                ok = GetFileName(filename, num);
        //            } while (ok == false);
        //            filesavename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + "(" + num.ToString() + ")" + filename;
        //        }
        //        else
        //            filesavename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + filename;

               
        //        FileStream fs = new FileStream(filesavename, FileMode.Append, FileAccess.Write, FileShare.Read, 40960);
                
        //        try
        //        {
        //            lock (filesock)
        //            {
        //                filesock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
        //                while (true)
        //                {
        //                    //logWrite("FileReceiver() 수신대기 ");
        //                    try
        //                    {
        //                        buffer = filesock.Receive(ref sender);
        //                        //logWrite("수신! ");
        //                    }
        //                    catch (SocketException se)
        //                    {
        //                        logWrite("FileReceiver()  filesock.Receive 에러 : " + se.ToString());
        //                        break;
        //                    }
        //                    if (buffer != null && buffer.Length != 0)
        //                    {
        //                        //logWrite("sender IP : " + sender.Address.ToString());
        //                        //logWrite("sender port : " + sender.Port.ToString());

        //                        byte[] receivebyte = Encoding.UTF8.GetBytes(buffer.Length.ToString());

        //                        try
        //                        {
        //                            filesock.Send(receivebyte, receivebyte.Length, sender);  //정상적으로 메시지 수신하면 응답(udp통신의 실패방지)
        //                        }
        //                        catch (SocketException se1)
        //                        {
        //                            logWrite("FileReceiver() filesock.Send 에러 : " + se1.ToString());
        //                            break;
        //                        }
        //                        if (fs.CanWrite == true)
        //                        {
        //                            try
        //                            {
        //                                fs.Write(buffer, 0, buffer.Length);
        //                                fs.Flush();
        //                            }
        //                            catch (Exception e)
        //                            {
        //                                logWrite("FileStream.Write() 에러 : " + e.ToString());
        //                                break;
        //                            }
        //                        }
        //                        FileInfo finfo = new FileInfo(filesavename);

        //                        size = Convert.ToInt32(finfo.Length);

        //                        if (size >= filesize)
        //                        {
        //                            logWrite("받은 크기 : " + size.ToString());
        //                            logWrite("파일 크기 : " + filesize.ToString());
        //                            logWrite("파일 전송 완료");
        //                            fs.Close();
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (ThreadAbortException e)
        //        { }
        //        catch (SocketException e)
        //        {
        //            logWrite("FileReceive() 에러 : " + e.ToString());
        //        }
        //        logWrite("FileReceiver 가 중단되었습니다. ");
        //        if (size!=0&&size >= filesize)
        //        {
        //            logWrite(ids[0]);
        //            FileInfoInsert(ids, filename, filesavename, fsize, filesender);
        //        }
        //    }
        //    catch (Exception e3)
        //    {
        //        logWrite("FileReceiver() 에러 : " + e3.ToString());
        //    }
            
        //}

        private bool GetFileName(string filename, int num)
        {
            string tempfilename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\(" + num + ")" + filename;
            FileInfo fi = new FileInfo(tempfilename);
            bool ok = false;
            if (fi.Exists == false)
            {
                ok = true;
            }
            return ok;
        }

        private void FileInfoInsert(string[] ids, string filename, string fileloc, string filesize, string filesender )
        {
            Hashtable parameters = new Hashtable();
            DataTable dt = new DataTable();
            try
            {
                LogWrite(filename + "/" + fileloc + "/" + filesize + "/" + filesender);

                string loc = null;
                string form = "f";

                LogWrite("ids.Length : " + ids.Length.ToString());

                parameters.Add("@filename", filename);
                parameters.Add("@filetime", Utils.TimeKey());
                parameters.Add("@fileloc", fileloc);
                parameters.Add("@filesize", filesize);

                string query = "insert into t_files(fname, ftime, floc, fsize) values(@filename, @filetime, @fileloc, @filesize)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite("FileInfoInsert 실패: file " + filename + " insert DB");
                    throw new Exception("FileInfoInsert 실패: file " + filename + " insert DB");
                }
                LogWrite("FileInfoInsert file " + filename + " insert DB");
                

                query = "select seqnum from t_files where ftime = @filetime";

                parameters.Clear();
                parameters.Add("@filename", filename);

                dt = DoQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    loc = row["seqnum"].ToString();
                }

                foreach (string tempid in ids)
                {
                    if (tempid != null && tempid.Length != 0)
                    {
                        InsertNoReceive(tempid, loc, filename, "f", filesender, "x");
                    }
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        private void DeleteNoreceive(string seqnum)
        {
            Hashtable parameters = new Hashtable();

            try
            {
                int row = 0;
                
                parameters.Add("@seq", seqnum);
                string query = "delete from t_noreceive where seqnum=@seq";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite("DeleteNoreceive 실패: seqnum[" + seqnum + "] 삭제");
                    throw new Exception("DeleteNoreceive 실패: seqnum[" + seqnum + "] 삭제");
                }

                LogWrite("DeleteNoreceive() 삭제 완료! : " + row.ToString() + " 개 행");
            }
            catch (Exception exception)
            {
                LogWrite("DeleteNoreceive() 에러:" + exception.ToString());
            }
        }

        /// <summary>
        /// 공지 정보 삭제
        /// </summary>
        /// <param name="array"></param>
        private void DeleteNotice(string[] array)
        {
            Hashtable parameters = new Hashtable();

            try
            {
                
                int row = 0;
                
                foreach (string seqnum in array)
                {
                    if (seqnum.Length != 0)
                    {
                        parameters.Add("@seq", seqnum);
                        string query = "delete from t_notices where seqnum=@seq";

                        if (DoExecute(query, parameters) < 1)
                        {
                            LogWrite("DeleteNotice 실패: seqnum[" + seqnum + "] 삭제");
                            throw new Exception("DeleteNotice 실패: seqnum[" + seqnum + "] 삭제");
                        }

                        else LogWrite("DeleteNotice() 삭제 완료! : " + row.ToString() + " 개 행");
                    }
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        /// <summary>
        /// 모든 공지정보 전달
        /// </summary>
        /// <param name="id"></param>
        private void SelectNoticeAll(string id)
        {
            DataTable dt = new DataTable();

            try
            {
                string noticeitems = "L";
                string seqnum = null;
                string ntime = null;
                string content = null;
                string nmode = null;
                string sender = null;
                string title = null;

                string query = "select seqnum, ntime, content, nmode, sender, ntitle from t_notices order by seqnum desc";

                dt = DoQuery(query, null);

                foreach (DataRow row in dt.Rows)
                {
                    seqnum = row["seqnum"].ToString();
                    ntime = row["ntime"].ToString();
                    content = row["content"].ToString();
                    nmode = row["nmode"].ToString();
                    sender = row["sender"].ToString();
                    title = row["ntitle"].ToString();
                    noticeitems += "|" + ntime + "‡" + content + "‡" + nmode + "‡" + sender + "‡" + seqnum + "‡" + title;
                }

                LogWrite("공지 읽어오기 성공");

                SendMsg(noticeitems, (IPEndPoint)InClientList[id]);
            }
            catch (Exception exception)
            {
                LogWrite("SelectNoticeAll() 에러:" + exception.ToString());
            }
        }

        /// <summary>
        /// 공지목록 조회. 부재중 수신자정보를 포함해 발송자에게 전송 
        /// </summary>
        /// <param name="id">공지 발송자</param>
        private void SelectNoticeList(string id)
        {
            Hashtable parameters = new Hashtable();
            Hashtable noticesFromSender = new Hashtable();
            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();

            try
            {
                string noticeitems = "t";
                string seqnum = null;
                string ntime = null;
                string content = null;
                string nmode = null;
                string title = null;


                //특정전송자가 올린 공지를 조회
                parameters.Add("@id", id);
                string query = "select seqnum, ntime, content, nmode, ntitle from t_notices where sender=@id order by seqnum";

                dt1 = DoQuery(query, parameters);

                foreach (DataRow row in dt1.Rows)
                {
                    seqnum = row["seqnum"].ToString();
                    ntime = row["ntime"].ToString();
                    content = row["content"].ToString();
                    nmode = row["nmode"].ToString();
                    title = row["ntitle"].ToString();
                    noticesFromSender[seqnum] = "|" + ntime + "†" + content + "†" + nmode + "†" + title + "†";
                }

                //공지목록중 부재건에 대해 수신자 ID를 추가
                string queryNoRecevie= "select receiver, loc from t_noreceive where form='n'";
                dt2 = DoQuery(queryNoRecevie, null);

                foreach (DataRow row in dt2.Rows)
                {
                    string loc = row["loc"].ToString();
                    string receiver = row["receiver"].ToString();

                    if (noticesFromSender.ContainsKey(loc)) //loc 값이 같은 부재중 공지 수신자 아이디 추가
                    {
                        noticesFromSender[loc] = noticesFromSender[loc].ToString() + receiver + ":";
                    }
                }

                foreach (DictionaryEntry de in noticesFromSender)
                {
                    LogWrite("notices sequence number from not reader [" + de.Key.ToString() + "] = " + de.Value.ToString());
                    noticeitems += de.Value.ToString();
                }

                if (InClientList.ContainsKey(id) && InClientList[id] != null)
                {
                    IPEndPoint iep = (IPEndPoint)InClientList[id];
                    SendMsg(noticeitems, iep);
                }

            }
            catch (Exception ex)
            {
                LogWrite("SelectNoticeList() 공지목록조회 에러 :" + ex.ToString());
            }
        }

        //파일전송시작 --> 안쓰임
        private void StartSendFile(string filenum, string id)
        {
            //Hashtable parameters = new Hashtable();
            //DataTable dt = new DataTable();

            //try
            //{
            //    string fileloc = null;
          
            //    int num = Convert.ToInt32(filenum);
                
            //    parameters.Add("@filenum", num);
            //    string query = "select floc from t_files where seqnum=@filenum";

            //    dt = DoQuery(query, parameters);

            //    foreach (DataRow row in dt.Rows)
            //    {
            //        fileloc = row["floc"].ToString();
            //    }

            //    SendFile(fileloc, id);
            //}
            //catch (Exception exception)
            //{
            //    logWrite("StartSendFile(): 파일전송시작 오류" + exception.ToString());
            //}
        }

        //private void SendFile(string fileloc, string id) //
        //{
        //    try
        //    {
        //        IPEndPoint sendfileIEP = new IPEndPoint(IPAddress.Any, 0);
        //        UdpClient filesendSock = new UdpClient(sendfileIEP);

        //        IPEndPoint iep = (IPEndPoint)InClientList[id];
        //        iep.Port = 9003;    //파일전용 포트로 변경
        //        logWrite("SendFile() 파일전송 포트 변경 :" + iep.Port.ToString());

        //        FileInfo fi = new FileInfo(fileloc);
        //        logWrite("SendFile() FileInfo 인스턴스 생성 : " + fileloc);

        //        int read = 0;
        //        byte[] buffer = null;
        //        byte[] re = null;

        //        filesendSock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);

        //        if (fi.Exists == true)
        //        {
        //            BufferedStream bs = new BufferedStream(new FileStream(fileloc, FileMode.Open, FileAccess.Read, FileShare.Read, 40960), 40960);

        //            double sendfilesize = Convert.ToDouble(fi.Length);
        //            double percent = (40960 / sendfilesize) * 100;
        //            double total = 0.0;

        //            lock (filesendSock)
        //            {
        //                while (true)
        //                {
        //                    for (int i = 0; i < 3; i++) //udp 통신의 전송실패 방지
        //                    {
        //                        try
        //                        {
        //                            logWrite("FileReceiver IP : " + iep.Address.ToString());
        //                            logWrite("FileReceiver port : " + iep.Port.ToString());
        //                            if (sendfilesize >= 40960.0)
        //                                buffer = new byte[40960];
        //                            else buffer = new byte[Convert.ToInt32(sendfilesize)];
        //                            read = bs.Read(buffer, 0, buffer.Length);
        //                            filesendSock.Send(buffer, buffer.Length, iep);
        //                            //logWrite("filesendSock.Send() : " + i.ToString() + " 번째 시도!");
        //                        }
        //                        catch (Exception e)
        //                        {
        //                            logWrite("SendFile() BufferedStream.Read() 에러 :" + e.ToString());
        //                        }
        //                        try
        //                        {
        //                            re = filesendSock.Receive(ref iep);
        //                            int reSize = int.Parse(Encoding.UTF8.GetString(re));
        //                            if (reSize == buffer.Length) break;
        //                        }
        //                        catch (SocketException e1)
        //                        { }
        //                    }

        //                    if (re == null || re.Length == 0)
        //                    {
        //                        logWrite("filesendSock.Send() 상대방이 응답하지 않습니다. 수신자 정보 : " + iep.Address.ToString() + ":" + iep.Port.ToString());
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        sendfilesize = (sendfilesize - 40960.0);
        //                        total += percent;
        //                        if (total > 100) total = 100.0;
        //                        string[] totalArray = (total.ToString()).Split('.');
        //                    }
        //                    if (total == 100.0)
        //                    {
        //                        logWrite("전송완료");
        //                        filesendSock.Close();
        //                    }
        //                    if (total == 100.0) break;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            logWrite("SendFile() 파일이 없음 : " + fileloc);
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}

        private void Login(string[] arr, IPEndPoint iep)
        {
            try
            {
                Client cl = null;
                string id = arr[1];
                string output = CheckRegist(arr);

                switch (output)
                {
                    case "match":
                        LogWrite("case : match (인증성공)");
                        string svrMsg = null;

                        if (InClientList.ContainsKey(id) && InClientList[id] != null)
                        {

                            LogWrite("중복 로그인 시도! : " + id.ToString());
                            svrMsg = "a|";
                            SendMsg(svrMsg, iep);

                        }
                        else
                        {
                            ArrayList objlist = GetClientInfo(id);
                            cl = (Client)objlist[0];

                            //로그인 사용자 내선번호 등록
                            cl.setPosition(arr[3]);
                            ExtensionIDpair[arr[3]] = cl.getId();
                            LogWrite(arr[3] + " = " + cl.getId() + " 등록");
                            //로그인 사용자 IPEndPoint 등록
                            //iep = new IPEndPoint(IPAddress.Parse(arr[4]), sendport);

                            //로그인 사용자 정보리스트에 등록
                            ClientInfoList[cl.getId()] = cl;
                            
                            LogWrite(cl.getName() + "(" + cl.getId() + ") 님 로그인 성공!(" + DateTime.Now.ToString() + ")");

                            Statlist[id] = "6"; //로그인 성공시 사용자 프리젠스 6(로그아웃)으로 설정

                            //로그인 성공시 (g|name|team|company|com_cd|db_port)
                            svrMsg = "g|" + cl.getName() + "|" + cl.getTeam() + "|" + cl.getCompany() + "|" + cl.getComCode() + "|" + configCtrl.DbPort;

                            SendMsg(svrMsg, iep);     //로그인 클라이언트에게 로그인 성공 알림

                            SendUserList((List<string>)objlist[1], iep);     //멤버 리스트 전송 

                            LogWrite("트리리스트 데이터 전송 완료!");

                            //// 다른 모든 클라이언트에게 로그인 사용자 정보 보내기(i|id|소속|ip|이름)
                            string smsg = "i|" + cl.getId() + "|" + cl.getTeam() + "|" + iep.Address.ToString() + "|" + cl.getName();
                            if (InClientList.Count != 0)
                            {
                                foreach (var de in InClientList)
                                {
                                    if (de.Value != null)
                                        SendMsg(smsg, (IPEndPoint)de.Value);
                                }
                            }

                            //다른 로그인 사용자 정보 전송
                            TransferInList(iep);
                            LogWrite("현재 로그인 사용자 정보 전송" + cl.getId());

                            //로그인 사용자 리스트(Hashtable) 등록(key=id , value=Client)
                            lock (InClientList)
                            {
                                InClientList[cl.getId()] = iep;
                                LogWrite("InClientList[" + cl.getId() + "] = " + iep.Address.ToString() + ":" + iep.Port.ToString());
                            }

                            lock (InClientStat)
                            {
                                InClientStat[cl.getId()] = "online";
                                LogWrite("InClientList[" + cl.getId() + "] = " + iep.Address.ToString() + ":" + iep.Port.ToString());
                            }

                            //로그인 사용자 내선리스트 등록
                            ExtensionList[cl.getPosition()] = iep;

                            LogWrite("InClientList에 등록 : " + cl.getId());
                            LogWrite("ExtensionList에 등록 : " + cl.getId() + " >> " + cl.getPosition());

                            //부재중 정보 전송
                            GetAbsenceData(cl.getId(), iep);


                        }
                        break;

                    case "mis":

                        SendMsg("f|p", iep);
                        break;

                    case "no":
                        SendMsg("f|n", iep);
                        break;
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        //private void Login(string[] arr, Socket s)
        //{
        //    try
        //    {
        //        string result = null;
        //        Client cl = null;
        //        string id = arr[1];
        //        string output = CheckRegist(arr);

        //        switch (output)
        //        {
        //            case "match":

        //                string svrMsg = null;
        //                cl = getClientInfo(id);

        //                if (InClientList.ContainsKey(id))
        //                {
        //                    if (InClientList[id] != null)  //이미 로그인 되어 있다면(중복 로그인 시도)
        //                    {
        //                        logWrite("중복 로그인 시도! : " + id.ToString());
        //                        svrMsg = "a|";
        //                        SendMsg(svrMsg, s);
        //                    }
        //                }
        //                else
        //                {
        //                    cl.setPosition(arr[3]);
        //                    logWrite(cl.getName() + "(" + cl.getId() + ") 님 로그인 성공!(" + DateTime.Now.ToString() + ")");

        //                    Statlist[id] = "6"; //로그인 성공시 사용자 프리젠스 6(로그아웃)으로 설정

        //                    svrMsg = "g|" + cl.getName() + "|" + cl.getTeam() + "|" + cl.getCompany();

        //                    if (s.Connected == true)
        //                    {
        //                        SendMsg(svrMsg, s);     //로그인 클라이언트에게 로그인 성공 알림
        //                    }
        //                    else
        //                    {
        //                        logWrite("Client Socket Disconnected");
        //                    }

        //                    LoadTreeList(cl.getId(), s);      //멤버 리스트 전송 

        //                    logWrite("트리리스트 데이터 전송 완료!");

        //                    string smsg = "i|" + cl.getId() + "|" + cl.getTeam() + "|" + cl.getName(); //i|id|소속|ip|이름
        //                    if (InClientList.Count != 0)
        //                    {
        //                        foreach (var de in InClientList)
        //                        {
        //                            if (de.Value != null)
        //                                SendMsg(smsg, (Socket)de.Value);           // 다른 모든 클라이언트에게 로그인 사용자 정보 보내기
        //                        }
        //                    }

        //                    //로그인 사용자 정보 전송
        //                    TransferInList(s);
        //                    logWrite("현재 로그인 사용자 정보 전송" + cl.getId());

        //                    //로그인 사용자 리스트(Hashtable) 등록(key=id , value=Client)
        //                    InClientList[cl.getId()] = s;


        //                    //로그인 사용자 내선리스트 등록
        //                    ExtensionList[cl.getPosition()] = s;

        //                    logWrite("InClientList에 등록 : " + cl.getId());
        //                    logWrite("ExtensionList에 등록 : " + cl.getId() + " >> " + cl.getPosition());

        //                    //부재중 정보 전송
        //                    //GetAbsenceData(cl.getId(), s);

        //                    //MemberList 상태 변경
        //                    AddTextDelegate ChangeStat = new AddTextDelegate(ChangeListStat);
        //                    Invoke(ChangeStat, cl.getId() + "|i");

        //                }
        //                break;

        //            case "mis":

        //                SendMsg("f|p", s);
        //                break;

        //            case "no":
        //                SendMsg("f|n", s);
        //                break;
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}

        /// <summary>
        /// 부재중 목록 조회
        /// </summary>
        /// <param name="id"></param>
        /// <param name="iep"></param>
        private void GetAbsenceData(string id, IPEndPoint iep)
        {
            Hashtable parameters = new Hashtable();
            DataTable dt = new DataTable(); 
            
            try
            {
                int mnum = 0; //메모
                int fnum = 0; //파일
                int nnum = 0; //공지
                int tnum = 0; //이관
              
                parameters.Add("@id", id);
                string query = "select form from t_noreceive where receiver=@id";

                dt = DoQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    string formVal = row["form"].ToString();
                    if (formVal.Equals("m")) mnum++;
                    if (formVal.Equals("f")) fnum++;
                    if (formVal.Equals("n")) nnum++;
                    if (formVal.Equals("t")) tnum++;
                }
                
                //부재중 목록 건수 전송
                string msg = "A|" + mnum.ToString() + "|" + fnum.ToString() + "|" + nnum.ToString() + "|" + tnum.ToString();
                SendMsg(msg, iep);

                //메모 내역전송
                if (mnum > 0 )
                {
                    ArrayList memolist = ReadMemo(id);
                    string cmsg = "Q";
                    if (memolist != null && memolist.Count != 0)
                    {
                        foreach (object obj in memolist)
                        {
                            string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                cmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(cmsg, iep);
                }

                //파일수신 내역전송
                if (fnum > 0)
                {
                    ArrayList filelist = ReadFile(id);
                    string fmsg = "R";
                    if (filelist != null && filelist.Count != 0)
                    {
                        foreach (object obj in filelist)
                        {
                            string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                fmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(fmsg, iep);
                }

                //공지 내역전송
                if (nnum > 0)
                {
                    ArrayList noticelist = ReadNotice(id);
                    string nmsg = "T";
                    if (noticelist != null && noticelist.Count != 0)
                    {
                        foreach (object obj in noticelist)
                        {
                            string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum, title }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                nmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(nmsg, iep);
                }
                //이관 내역전송
                if (tnum > 0)
                {
                    ArrayList translist = ReadTransfer(id);
                    string tmsg = "trans";
                    if (translist != null && translist.Count != 0)
                    {
                        foreach (object obj in translist)
                        {
                            string[] array = (string[])obj;//string[]{sender,content, time, seqnum} , content => pass|ani|senderID|receiverID|일자|시간|CustomerName
                            string temp = array[1];
                            array[1] = temp.Replace('|', '&');
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                tmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(tmsg, iep);
                }
               
            }
            catch (Exception ex)
            {
                LogWrite("GetAbsenceData() 부재건 조회 에러 :" + ex.ToString());
            }
        }

        /// <summary>
        /// 사용자 비번 인증
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        private string CheckRegist(string[] arr)
        {
            string result = null;
            Hashtable parameters = new Hashtable();
            DataTable dt = new DataTable();

            try
            {
                parameters.Add("@id", arr[1]);
                string query = "select user_pwd from t_user where user_id=@id";

                dt = DoQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["user_pwd"].ToString().Equals(arr[2]))
                        {
                            result = "match";
                        }
                        else
                        {
                            result = "mis";
                        }
                        break;
                    }
                }
                else
                {
                    result = "no";
                }
                LogWrite("인증결과 : " + result);
            }
            catch (Exception ex)
            {
                LogWrite("CheckRegist 사용자 비번인증 오류 : " + ex.ToString());
            }
         
            return result;
        }

        /// <summary>
        /// 사용자 목록을 DB에서 조회
        /// </summary>
        /// <returns></returns>
        private List<string> getClientIDs()
        {
            List<string> idList = new List<string>();
            DataTable dt = new DataTable();

            try
            {
                string query = "select USER_ID from t_user";
                dt = DoQuery(query, null);

                foreach (DataRow row in dt.Rows)
                {
                    idList.Add(row["USER_ID"].ToString());
                }
            }
            catch (Exception ex)
            {
                LogWrite("getClientIDs() 사용자목록 조회 오류: " + ex.ToString());
            }
            return idList;
        }

        /// <summary>
        /// 사용자정보조회 및 팀리스트 생성
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ArrayList GetClientInfo(string id)
        {
            ArrayList objlist = new ArrayList();
            Client cl = null;
            List<string> team_list = new List<string>();
            Dictionary<string, List<string>> TeamTable = new Dictionary<string, List<string>>();
            DataTable dt = new DataTable();

            try
            {
                string TeamString = "M|";

                string query = "select u.user_id, u.user_nm, u.team_nm, c.com_nm, u.com_cd from t_user as u, t_company as c where u.com_cd=c.com_cd";

                dt = DoQuery(query, null);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string dbid = row["user_id"].ToString();
                        string name = row["user_nm"].ToString();
                        string team = row["team_nm"].ToString();
                        string com_nm = row["com_nm"].ToString();
                        string com_cd = row["com_cd"].ToString();

                        if (com_cd.Equals(configCtrl.CompanyCode))
                        {
                            if (id.Equals(dbid))
                            {
                                cl = new Client(id, name, team, "", com_nm, com_cd);
                            }

                            lock (TeamNameList)
                            {
                                TeamNameList[dbid] = team;
                                LogWrite("TeamNameList[" + dbid + "] = " + team);
                            }
                            string minfo = null;
                            minfo = dbid + "!" + name;
                            string tname = team;
                            List<string> temp = null;
                            if (TeamTable.Count > 0)
                            {
                                if (TeamTable.ContainsKey(tname))
                                {
                                    TeamTable[tname].Add(minfo);
                                }
                                else
                                {
                                    temp = new List<string>();
                                    temp.Add(minfo);
                                    TeamTable[tname] = temp;
                                }
                            }
                            else
                            {
                                temp = new List<string>();
                                temp.Add(minfo);
                                TeamTable[tname] = temp;
                            }
                        }
                    }

                }

                LogWrite("팀리스트 생성 시작!");

                foreach (var de in TeamTable)
                {
                    if (de.Value != null)
                    {
                        string teamstr = de.Key;
                        List<string> temp = de.Value;
                        foreach (string str in temp)
                        {
                            teamstr += "|" + str;
                        }
                        team_list.Add(TeamString + teamstr);
                        LogWrite(TeamString + teamstr);
                    }
                }

                LogWrite("팀리스트 생성 완료!");

            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }

            objlist.Add(cl);
            objlist.Add(team_list);

            return objlist;
        }

        private void Logout(string id)
        {
            try
            {
                id = id.ToLower();
                if (InClientList.ContainsKey(id) && InClientList[id] != null)
                {
                    lock (InClientList)
                    {
                        InClientList[id] = null;
                    }
                    LogWrite("InClientList로부터 삭제 : " + id);


                    string temp_team = GetTeamName(id);
                    string smsg = "o|" + id + "|" + temp_team;   //smsg= o|id|소속

                    // 다른 모든 클라이언트에게 로그아웃 정보 보내기
                    //2010.4.28일 수정 : 로그아웃 중 다른 쓰레드의 InClientList 접근으로 생기는 오류 방지 lock(InClientList) 
                    lock (InClientList)
                    {
                        foreach (var de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                if (!de.Key.ToString().Equals(id))
                                {
                                    SendMsg(smsg, (IPEndPoint)de.Value);
                                }
                            }
                        }
                    }
                    LogWrite(id + " 로그아웃 완료!");
                }
                else
                {
                    LogWrite("Logout() : " + id + "가 InClientList key에서 찾을 수 없습니다.");
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        public void TransferInList(IPEndPoint iep)
        {
            try
            {
                if (InClientList.Count != 0)
                {
                    foreach (var de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            string ip = ((IPEndPoint)de.Value).Address.ToString();
                            string msg = "y|" + (String)de.Key + "|" + InClientStat[de.Key.ToString()].ToString() + "|" + ip;       //y|로그인상담원id|(string)IP주소, de.Key=id
                            SendMsg(msg, iep);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }
         
        //public void SendMsg(string msg, Socket sendsocket)
        //{
        //    try
        //    {
        //        byte[] msgbuffer = Encoding.UTF8.GetBytes(msg);
        //        try
        //        {
        //            if (sendsocket != null && sendsocket.Connected == true)
        //            {
        //                byte[] lengbuffer = new byte[9];

        //                byte[] tempbuffer = Encoding.ASCII.GetBytes("SIZE" + msgbuffer.Length.ToString());

        //                for (int i = 0; i < tempbuffer.Length; i++)
        //                {
        //                    lengbuffer[i] = tempbuffer[i];
        //                }

        //                int sendbuffer = sendsocket.Send(lengbuffer);

        //                if (sendbuffer == lengbuffer.Length)
        //                {
        //                    sendsocket.Send(msgbuffer);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            logWrite(e.ToString());
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}
        
        #region 미사용중
        private ArrayList getBufferList(byte[] buffer)
        {
            ArrayList bufferArray = new ArrayList();
            try
            {
                int size = 0;
                byte[] part = new byte[64000];
                for (int i = 0; i < buffer.Length; i++)
                {
                    part[size] = buffer[i];
                    size++;
                    if (size > 64000)
                    {
                        bufferArray.Add(part);
                        size = 0;
                        part.Initialize();
                    }
                }
            }
            catch (Exception ex)
            {
                LogWrite("getBufferList() Exception : " + ex.ToString());
            }
            return bufferArray;
        }
        #endregion

        public void ErrorConnClear(string id) //전송에러 접속자 로그아웃 처리
        {
            try
            {
                Logout(id);
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        /// <summary>
        /// 부재중 공지등록
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="loc">공지건 ID(시퀀스)</param>
        /// <param name="content"></param>
        /// <param name="form"></param>
        /// <param name="sender"></param>
        /// <param name="mode"></param>
        /// <param name="title"></param>
        private void InsertNoReceiveforNotice(string receiver, int loc, string content, string form, string sender, string mode, string title)
        {
            Hashtable parameters = new Hashtable();
            try
            {
                parameters.Add("@nreceiver", receiver);
                parameters.Add("@ntime", Utils.TimeKey());
                parameters.Add("@nloc", loc);
                parameters.Add("@ncontent", content);
                parameters.Add("@nform", form);
                parameters.Add("@nsender", sender);
                parameters.Add("@nmode", mode);
                parameters.Add("@title", title);

                string query = "insert into t_noreceive "
                              +"       (receiver, time, loc, content, form, sender, nmode, ntitle)"
                              +"values (@nreceiver, @ntime, @nloc, @ncontent, @nform, @nsender, @nmode, @title)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite(string.Format("부재중 공지 등록 실패 sneder[{0}] to receiver[{1}]", sender, receiver));
                    throw new Exception(string.Format("부재중 공지 등록 실패 sneder[{0}] to receiver[{1}]", sender, receiver));
                }

                LogWrite(string.Format("부재중 공지 등록 sneder[{0}] to receiver[{1}]",sender,receiver));
            }
            catch (Exception ex)
            {
                LogWrite("InsertNoReceive() 부재중 공지 등록 에러 :" + ex.ToString());
            }
        }

        /// <summary>
        /// 부재중 이관건/쪽지 등록
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="loc"></param>
        /// <param name="content"></param>
        /// <param name="form"></param>
        /// <param name="sender"></param>
        /// <param name="mode"></param>
        private void InsertNoReceive(string receiver, string loc, string content, string form, string sender, string mode)
        {
            Hashtable parameters = new Hashtable();
            try
            {
                parameters.Add("@nreceiver", receiver);
                parameters.Add("@ntime", Utils.TimeKey());
                parameters.Add("@nloc", loc);
                parameters.Add("@ncontent", content);
                parameters.Add("@nform", form);
                parameters.Add("@nsender", sender);
                parameters.Add("@mode", mode);

                string query = "insert into t_noreceive(receiver, time, loc, content, form, sender, nmode) values(@nreceiver, @ntime, @nloc, @ncontent, @nform, @nsender, @mode)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite(string.Format("부재중 업무등록 실패 sneder[{0}] to receiver[{1}]", sender, receiver));
                    throw new Exception(string.Format("부재중 업무등록 실패 sneder[{0}] to receiver[{1}]", sender, receiver));
                }

                LogWrite(string.Format("부재중 업무등록 sneder[{0}] to receiver[{1}]", sender, receiver));

            }
            catch (Exception ex)
            {
                LogWrite("InsertNoReceive() 부재중 업무등록 에러 : " + ex.ToString());
            }
        }

        /// <summary>
        /// 부재중 쪽지 읽기
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ArrayList ReadMemo(string id)
        {
            Hashtable parameters = new Hashtable();
            ArrayList list = new ArrayList();
            DataTable dt = new DataTable();

            try
            {
                parameters.Add("@id", id);
                string query = "select sender, content, time, seqnum from t_noreceive where receiver=@id and form='m'";

                dt = DoQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string sender = row["sender"].ToString();
                        string content = row["content"].ToString();
                        string time = row["time"].ToString();
                        string seqnum = row["seqnum"].ToString();
                        string[] str = new string[] { sender, content, time, seqnum };
                        list.Add(str);
                        LogWrite(string.Format("부재중 메모 sender[{0}] receiver[{1}] 내용[{2}]", sender, id, content));
                    }
                }
                else LogWrite(id + " 의 메모 없음!");
            }
            catch (Exception ex)
            {
                LogWrite("ReadMemo() 부재중 메모읽기 에러 : " + ex.ToString());
            }

            return list;
        }

        /// <summary>
        /// 부재중 공지 읽기
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ArrayList ReadNotice(string id)
        {
            Hashtable parameters = new Hashtable();
            ArrayList list = new ArrayList();
            DataTable dt = new DataTable();

            try
            {
                parameters.Add("@id", id);

                string query = "select sender, content, time, nmode, seqnum, ifnull(ntitle,'공지사항') ntitle from t_noreceive where receiver=@id and form='n'";
                dt = DoQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string sender = row["sender"].ToString();
                        string content = row["content"].ToString();
                        string time = row["time"].ToString();
                        string nmode = row["nmode"].ToString();
                        string seqnum = row["seqnum"].ToString();
                        string title = row["ntitle"].ToString();

                        string[] str = new string[] { sender, content, time, nmode, seqnum, title };
                        list.Add(str);
                        LogWrite(string.Format("부재중 공지 sender[{0}] 제목[{1}] 내용[{2}]", sender, title, content));

                    }
                }
                else LogWrite(id + "의 공지 없음!");

            }
            catch (Exception ex)
            {
                LogWrite("ReadNotice() 부재중 공지 읽기 에러 :" + ex.ToString());
            }

            return list;
        }

        /// <summary>
        /// 부재중 수신파일 읽기
        /// </summary>
        /// <param name="id"></param>
        /// <returns>수신건목록</returns>
        private ArrayList ReadFile(string id)
        {
            Hashtable parameters = new Hashtable();
            ArrayList list = new ArrayList();
            DataTable dt = new DataTable();

            try
            {
                parameters.Add("@id", id);

                string query = "select tn.sender, tn.loc, tn.content, tn.time, tn.seqnum, tf.fsize "
                              + " from t_noreceive tn, t_files tf "
                              + "where tn.receiver=@id and tn.form='f' and tn.loc=tf.seqnum";

                dt = DoQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string sender = row["sender"].ToString();
                        string loc = row["loc"].ToString();
                        int num = Convert.ToInt32(loc);
                        string content = row["content"].ToString();  //파일명
                        string time = row["time"].ToString();
                        string seqnum = row["seqnum"].ToString();
                        string fsize = row["fsize"].ToString();
                        string[] str = new string[] { sender, loc, content, time, fsize, seqnum }; //content= 파일명
                        list.Add(str);

                        LogWrite(string.Format("부재중 수신파일 sender[{0}] 목록id[{1}] 내용[{2}]", sender, loc, content));
                    }
                }
                else LogWrite(id + "의 부재중 파일 없음!");

            }
            catch (Exception ex1)
            {
                LogWrite("ReadFile() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
            }

            return list;
        }

        /// <summary>
        /// 부재중 이관건 읽기
        /// </summary>
        /// <param name="id">이관건 수신자</param>
        /// <returns>이관건 목록</returns>
        private ArrayList ReadTransfer(string id)
        {
            Hashtable parameters = new Hashtable();
            ArrayList list = new ArrayList();
            DataTable dt = new DataTable();

            try
            {
                parameters.Add("@id", id);
                string query = "select sender, content, time, seqnum from t_noreceive where receiver=@id and form='t'";

                dt = DoQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    string sender = row["sender"].ToString();
                    string content = row["content"].ToString();
                    string time = row["time"].ToString();
                    string seqnum = row["seqnum"].ToString();

                    string[] str = new string[] { sender, content, time, seqnum };
                    list.Add(str);
                    LogWrite(string.Format("부재중 이관건 sender[{0}] receiver[{1}] 내용[{2}]", sender, id, content));
                }
            }
            catch (Exception ex1)
            {
                LogWrite("ReadTransfer() 부재중 이관건 읽기 에러 : " + ex1.ToString());
            }

            return list;
        }

        /// <summary>
        /// 로그인 사용자 ID목록 받기
        /// </summary>
        /// <returns></returns>
        private ArrayList GetNoticeList()
        {
            List<string> ids = getClientIDs();
            ArrayList list = new ArrayList();
            try
            {
                foreach (string cid in ids)
                {
                    if (InClientList.ContainsKey(cid))
                    {
                        if (InClientList[cid] == null)
                        {
                            list.Add(cid);
                        }
                    }
                    else
                    {
                        list.Add(cid);
                    }
                }
                LogWrite("공지사항 전송 : " + list.Count.ToString());
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
            return list;
        }

        /// <summary>
        /// 공지 등록
        /// </summary>
        /// <param name="list">공지대상자 명단</param>
        /// <param name="noticetime"></param>
        /// <param name="content"></param>
        /// <param name="sender"></param>
        /// <param name="mode"></param>
        /// <param name="title"></param>
        private void InsertNotice(ArrayList list, string noticetime, string content, string sender, string mode, string title)
        {
            Hashtable parameters = new Hashtable();
            DataTable dt = new DataTable();

            try
            {              
                LogWrite(string.Format("InsertNotice 공지등록: 대상자수[{0}]",list.Count));
                
                parameters.Add("@content", content);
                parameters.Add("@ntime", noticetime);
                parameters.Add("@sender", sender);
                parameters.Add("@nmode", mode);
                parameters.Add("@Title", title);

                string query = "insert into t_notices(content, ntime, sender, nmode, ntitle) values(@content, @ntime, @sender, @nmode, @Title)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite(string.Format("InsertNotice 공지등록 실패: title[{0}] sender[{1}] time[{2}]", title, sender, noticetime));
                    throw new Exception("InsertNotice 공지등록 실패 ");
                }

                LogWrite(string.Format("InsertNotice 공지등록 : title[{0}] sender[{1}] time[{2}]", title, sender, noticetime));


                //현재 시퀀스 값 받아옴
                int seqNum = 0;
                string querySelect = "select seqnum from t_notices order by seqnum desc";
                dt = DoQuery(querySelect, null);

                foreach (DataRow row in dt.Rows)
                {
                    seqNum = Convert.ToInt16(row["seqnum"].ToString());
                    LogWrite(string.Format("InsertNotice 공지등록: 최종순번[{0}]", seqNum));
                }

                if (list != null)
                {
                    foreach (object userId in list)
                    {
                        InsertNoReceiveforNotice((string)userId, seqNum, content, "n", sender, mode, title);
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception exception)
            {
                LogWrite("InsertNotice 공지등록 오류: " + exception.ToString());
                
            }
        }

        #region 임시 테스트용
        public int getListenPort(int port) //임시 테스트용
        {
            
            if (port == 8884) port = 8883;
            if (port == 8886) port = 8885;
            if (port == 8888) port = 8887;
            return port;
        }
        #endregion

        public ArrayList makeList() //리스너 시작시 멤버 리스트 얻어옴.
        {
            Hashtable parameters = new Hashtable();
            ArrayList team_list = new ArrayList();
            Dictionary<string, List<string>> TeamTable = new Dictionary<string, List<string>>();
            DataTable dt = new DataTable();

            try
            {
                string TeamString = "M|";

                string query = "select user_id, user_nm, team_nm from t_user";

                dt = DoQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    string minfo = null;
                    minfo = row["user_id"].ToString() + "!" + row["user_nm"].ToString();
                    string tname = row["team_nm"].ToString();

                    List<string> temp = new List<string>();

                    if (TeamTable.Count > 0
                        && TeamTable.ContainsKey(tname))
                    {
                        temp = TeamTable[tname];
                        temp.Add(minfo);
                        TeamTable.Add(tname, temp);
                    }
                    else
                    {
                        temp = new List<string>();
                        temp.Add(minfo);
                        TeamTable.Add(tname, temp);
                    }

                }
            

                LogWrite("팀리스트 생성 시작!");

                foreach (var de in TeamTable)
                {
                    if (de.Value != null)
                    {
                        string teamstr = (string)de.Key;
                        List<string> temp = de.Value;
                        foreach (string str in temp)
                        {
                            teamstr += "|" + str;
                        }
                        team_list.Add(TeamString + teamstr);
                        LogWrite(TeamString + teamstr);
                    }
                }

                LogWrite("팀리스트 생성 완료!");
            }
            catch (Exception exception)
            {
                LogWrite("makeList() 팀리스트 생성 오류: " + exception.ToString());
            }
            return team_list;
        }

        public void SendUserList(List<string> team_list, IPEndPoint iep) //로그인 상태와는 무관한 전체 사용자 리스트 생성
        {
            try
            {
                foreach (string tempString in team_list)   //TeamList(M|팀명|id!name|id!name|....
                {
                    if (tempString != null && tempString.Length != 0)
                    {
                        SendMsg(tempString, iep);    //팀멤버 목록 전송
                    }
                }
                SendMsg("M|e", iep); //모든 팀리스트 정보 전송완료 메시지 전송
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }
                
        public int getMode(string msg)
        {
            int mode = 0;
            string[] udata = null;
            try
            {
                msg = msg.Trim();
                if (msg.Contains("|"))
                {
                    udata = msg.Split('|');
                }
                if (udata[0].Equals(MsgDef.MSG_FTP_INFO_TO_SVR)
                    || udata[0].Equals(MsgDef.MSG_FTP_INFO_TO_RCV)
                    || udata[0].Equals(MsgDef.MSG_FTP_READY_TO_SVR)
                    || udata[0].Equals(MsgDef.MSG_FTP_READY_TO_SND)
                    || udata[0].Equals(MsgDef.MSG_FTP_REJECT_TO_SVR)
                    || udata[0].Equals(MsgDef.MSG_FTP_REJECT_TO_SND))
                {
                    LogWrite("getMode() FTP명령어로 mode=24으로 처리 :" + udata[0]);
                    mode = 24;
                }
                else 
                    mode = Convert.ToInt32(udata[0]);
             
            }
            catch (Exception ex)
            {
                LogWrite("getMode() Exception : " + ex.ToString());
                mode = 100;
            }
            return mode;
        }

        private void ProcessOnSocStatusChanged(object sender, SocStatusEventArgs e)
        {
            switch (e.Status.Status)
            {
                case SocHandlerStatus.RECEIVING:
                    LogWrite("수신 메시지 : " + e.Status.Data); 
                    if (e.Status.Cmd.Equals(MsgDef.MSG_TEXT))// 메시지 형식 "MSG|...."
                    {
                        string msg = e.Status.Data.Substring(MsgDef.MSG_TEXT.Length+1);


                        int mode = getMode(msg);
                        ArrayList list = new ArrayList();
                        list.Add(mode);
                        list.Add(msg);
                        list.Add(e.Status.Soc);
                        Thread msgThread = new Thread(new ParameterizedThreadStart(receiveReq));
                        msgThread.Start(list);
                    }
                    break;
                default:
                    break;
            }
            string logMsg = e.Status.SocMessage;
            if (e.Status.exception != null)
            {
                if (e.Status.exception is SocketException)
                    logMsg += "\n" + string.Format("Socket Error: {0} : {1}",
                        ((SocketException)e.Status.exception).ErrorCode,
                        ((SocketException)e.Status.exception).Message) + "\n";
                else
                    logMsg += "\n" + string.Format(">Received Socket Error: {0}", e.Status.exception.Message) + "\n";
            }
            LogWrite(logMsg);        
        }

        private void OnLogWrite(object sender, StringEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                LogWrite(e.EventString);
            });
        }
        #region 로그 처리
        /// <summary>
        /// 서버 로그창에 로그 쓰기 및 로그파일에 쓰기
        /// </summary>
        /// <param name="svrLog"></param>
        public void LogWrite(string svrLog)
        {
            try
            {
                AddText = new AddTextDelegate(writeLogBox);
                svrLog = "[" + DateTime.Now.ToString() + "] " + svrLog + "\r\n";
                if (LogBox.InvokeRequired)
                {
                    Invoke(AddText, svrLog);
                    Logger.info(svrLog);
                }
                else
                {
                    LogBox.AppendText(svrLog);
                    Logger.info(svrLog);
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        private void writeLogBox(string str)
        {
            LogBox.AppendText(str);
        }

        #endregion

        /// <summary>
        /// 서버 중지
        /// </summary>
        public void ServerStop()
        {
            try
            {
                timer.Stop();
                if (InClientList != null)
                {
                    if (InClientList.Count != 0)
                        InClientList.Clear();

                    LogWrite("InClientList 삭제");
                    
                    if (TeamList != null)
                        if (TeamList.Count != 0) TeamList.Clear();
                    
                    LogWrite("TeamList 삭제");
                    if (Statlist != null)
                        if (Statlist.Count != 0) Statlist.Clear();
                    
                    //if (dev.Started == true)
                    //{
                    //    dev.Close();
                    //}
                }
            }
            catch (Exception ex)
            {
                LogWrite("ServerStop 에러 : " + ex.ToString());
            }

            try
            {
                //TCP방식
                if (mServer != null)
                    mServer.Dispose();


                if (CrmReceiverThread != null)
                {
                    if (CrmReceiverThread.IsAlive == true)
                    {
                        CrmReceiverThread.Abort();
                        LogWrite("CrmReceiverThread가 종료되었습니다.");
                    }
                }
               
                if (ListenThread != null)
                {                    
                    if (ListenThread.IsAlive == true)
                    {                        
                        ListenThread.Abort();
                        LogWrite("ListenThread가 종료되었습니다.");
                    }
                }

                commctl.disConnect();
            }
            catch (Exception ex1)
            {
                LogWrite("Listenthread close 에러 : "+ex1.ToString());
                svrStart = false;
            }
            ButtonStart.Enabled = true;
            svrStart = false;
            this.Close();
            notify_svr.Visible = false;
            Process.GetCurrentProcess().Kill();
        }

        private void stop_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void stopApplication()
        {
            DialogResult result = MessageBox.Show(this, "정말 메신저 서버를 중단시키겠습니까?", "서버 중단 경고", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                ServerStop();
                Application.ExitThread();
                Application.Exit();
                notify_svr.Visible = false;
                Process.GetCurrentProcess().Kill();
            }
        }

        private void MsgSvrForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        private void btn_confirm_ClickforTeam(object sender, EventArgs e)
        {
            try
            {
                string teamname = null;
                Button button = (Button)sender;
                int count = button.Parent.Controls.Count;

                for (int i = 0; i < count; i++)
                {
                    if (button.Parent.Controls[i].Name.Equals("txtbox_teamname"))
                    {
                        TextBox box = (TextBox)button.Parent.Controls[i];
                        if (box.Text.Length != 0)
                        {
                            teamname = box.Text;
                        }
                        break;
                    }
                }
                if (teamname != null && teamname.Length != 0)
                {
                    InsertTeam(teamname);
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        private void InsertTeam(string teamname)
        {
            Hashtable parameters = new Hashtable();
            try
            {
                parameters.Add("@team", teamname);
                string query = "insert into team values(@team)";

                if (DoExecute(query, parameters) < 1)
                {
                    LogWrite(string.Format("InsertTeam 실패: new team[{0}]", teamname));
                    throw new Exception(string.Format("InsertTeam 실패: new team[{0}]", teamname));
                }

                LogWrite(string.Format("InsertTeam : new team[{0}]", teamname));
           
            }
            catch (Exception exception)
            {
                LogWrite("InsertTeam() cmd.ExecuteNonQuery() 에러 : " + exception.ToString());
            }
        }

        private void button1_Click(object sender1, EventArgs e)
        {
            makeCallTestForm();
        }

        private void makeCallTestForm()
        {
            calltestform = new CallTestForm();
            calltestform.btn_confirm.MouseClick += new MouseEventHandler(btn_confirm_MouseClick);
            calltestform.button1.MouseClick += new MouseEventHandler(button1_MouseClick);
            calltestform.Show();
        }

        private void button1_MouseClick(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            CallTestForm form = (CallTestForm)button.Parent;
            form.Close();
        }

        private void btn_confirm_MouseClick(object sender, MouseEventArgs e)
        {
            Thread t1 = new Thread(new ThreadStart(sendTestRing));
            t1.Start();
        }

        private void sendTestRing()
        {
            try
            {
                Thread.Sleep(3000);
                string aniNum = calltestform.txtbox_ani.Text;
                string extNum = calltestform.txtbox_ext.Text;
                int delay = Convert.ToInt32(calltestform.txtbox_time.Text);


                if (configCtrl.ServerType.Equals(ConstDef.NIC_LG_KP))
                {
                    RecvMessage("Ringing", aniNum);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("Answer", aniNum + ">" + extNum);
                    }
                }
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_SIP))
                {
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss#" + aniNum);
                    RecvMessage("Ringing", aniNum + "|" + extNum + "|" + call_id);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("Answer", aniNum + "|" + extNum + "|" + call_id);

                        Thread.Sleep(delay);
                        RecvMessage("HangUp", aniNum + "|" + extNum + "|" + call_id);
                        //RecvMessage("Abandon", aniNum + "|" + extNum + "|" + call_id);
                        
                    }
                }
                else if (configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT1)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT2)
                    || configCtrl.ServerType.Equals(ConstDef.NIC_CID_PORT4))
                {
                    RecvMessage("Ringing", aniNum);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("OffHook", "");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
        }
    
        private SIPMessage makeSIPConstructor(string data)
        {
            SIPM = new SIPMessage();
            StreamWriter sw = new StreamWriter("PacketDump_" + DateTime.Now.ToShortDateString() + ".txt", true, Encoding.Default);
            try
            {
                string code = "Unknown";
                string method = "Unknown";
                string callid = "Unknown";
                string cseq = "Unknown";
                string from = "Unknown";
                string to = "Unknown";
                string agent = "Unknown";
                string sName = "Unknown";


                StringReader sr = new StringReader(data);
                
                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string Gubun = "";

                    if (line.Length > 2)
                    {
                        Gubun = line.Substring(0, 3);
                    }
                    if (Gubun.Equals("REG"))
                    {
                        break;
                    }
                    else
                    {
                        if (Gubun.Equals(ConstDef.NIC_SIP))  //Status Line
                        {
                            string[] sipArr = line.Split(' ');
                            if (sipArr.Length > 0)
                            {
                                code = sipArr[1].Trim();
                                method = sipArr[2].Trim();
                                sw.WriteLine("code : "+code + " / method : " + method);
                            }
                        }
                        else if (Gubun.Equals("INV"))
                        {
                            method = "INVITE";
                        }
                        else if (Gubun.Equals("CAN"))
                        {
                            method = "CANCEL";
                        }
                        else
                        {
                            string[] sipArr = line.Split(':');
                            if (sipArr.Length < 2)
                            {
                                sipArr = line.Split('=');
                                if (sipArr.Length > 1)
                                {
                                    sw.WriteLine(sipArr[0] + " = " + sipArr[1]);
                                    if (sipArr[0].Equals("s")) sName = sipArr[1];
                                }
                            }
                            else
                            {
                                string key = sipArr[0];

                                switch (key)
                                {
                                    case "From":
                                        from = sipArr[2].Split('@')[0];
                                        sw.WriteLine("From = " + from);
                                        break;

                                    case "To":
                                        to = sipArr[2].Split('@')[0];
                                        sw.WriteLine("To = " + to);
                                        break;

                                    case "Call-ID":
                                        callid = sipArr[1].Split('@')[0];
                                        sw.WriteLine("Call-ID = " + callid);
                                        break;

                                    case "CSeq":
                                        cseq = sipArr[1].Split('@')[0];
                                        sw.WriteLine("CSeq = " + cseq);
                                        break;

                                    case "User-Agent":
                                        agent = sipArr[1].Split('@')[0];
                                        sw.WriteLine("User-Agent = " + cseq);
                                        break;

                                    default:

                                        string value = "";
                                        for (int i = 1; i < sipArr.Length; i++)
                                        {
                                            value += sipArr[i];
                                        }
                                        sw.WriteLine(key + " = " + value);

                                        break;
                                }
                            }
                        }
                    }
                }
                sw.WriteLine("\r\n");
                sw.WriteLine("###########");
                sw.Flush();
                sw.Close();
                if (!from.Equals(to) && !from.Equals("unknown") && !to.Equals("unknown"))
                {
                    LogWrite(data);
                }
                SIPM.setSIPMessage(code, method, callid, cseq, from, to, agent, sName);

            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
                sw.Close();
            }

            return SIPM;
        }

        private void MnDBSetting_Click(object sender, EventArgs e)
        {
            dbinfo = new DBInfoForm();
            dbinfo.btn_confirm.MouseClick += new MouseEventHandler(btn_dbConfirm_MouseClick);
            dbinfo.tbx_host.Text = configCtrl.DbServerIp + ":" + configCtrl.DbPort;
            dbinfo.tbx_dbname.Text = configCtrl.DbName;
            dbinfo.tbx_id.Text = configCtrl.DbUser;
            dbinfo.tbx_pass.Text = configCtrl.DbPasswd;
            dbinfo.Show();
        }

        private void btn_dbConfirm_MouseClick(object sender, MouseEventArgs e)
        {
            dbinfo.Close();
        }

        #region sip 캡쳐 테스트 부분

        public string Connect(string Device_Name)
        {
            int failCount = 0;
            string result = "";

            deviceList = CaptureDeviceList.Instance;
            foreach (ICaptureDevice item in deviceList)
            {
                if (item.Description.Equals(Device_Name))
                {
                    dev = item;
                    break;
                }
            }

            try
            {
                if (dev != null)
                {
                    dev.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
                    dev.Open(DeviceMode.Promiscuous, 500);
                    dev.Filter = "udp src port 5060";

                    try
                    {
                        dev.StartCapture();
                        //log("Packet capture Start!!");
                    }
                    catch (Exception ex1)
                    {
                        //log("capture fail");
                        failCount++;

                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                //Logwriter(ex.ToString());
            }
            if (failCount == 0)
            {
                result = "Success";
            }
            else
            {
                result = "Fail";
            }
            return result;
        }

        /// <summary>
        /// 설정된 NIC 디바이스의 패킷 수신 이벤트 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            //log("Packet 수신!");
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            UdpPacket udpPacket = UdpPacket.GetEncapsulated(p);
            string data = Encoding.ASCII.GetString(udpPacket.PayloadData);
            SIPM = makeSIPConstructor(data);

            //log(data);
            if (!SIPM.from.Equals(SIPM.to) && !SIPM.from.Equals("unknown") && !SIPM.to.Equals("unknown"))
            {

                if (SIPM.method.Equals("INVITE"))
                {
                    if (SIPM.sName.Equals("session")) //Ringing
                    {
                        LogWrite("Ringing : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                    else if (SIPM.sName.Equals("SIP Call")) //Dial
                    {
                        LogWrite("Dialing : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                }
                else if (SIPM.code.Equals("200")) //Answer
                {
                    if (SIPM.sName.Equals("SIP Call"))
                    {
                        LogWrite("Answer : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                    else if (SIPM.sName.Equals("session")) //발신 후 연결 
                    {
                        LogWrite("CallConnect : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                }
                else if (SIPM.method.Equals("CANCEL")) //Abandon
                {
                    LogWrite("Abandon : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                }
                else if (SIPM.method.Equals("BYE")) //Abandon
                {
                    LogWrite("HangUp : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                }
            }
        }

        #endregion

        private void StripMenu_svrconfig_Click(object sender, EventArgs e)
        {
            setDevice();
        }

        private void 콜테스트ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCallTestForm();
        }

        private void 보이기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.TopMost = true;
            this.Show();
        }

        private void notify_svr_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void MsgSvrForm_MinimumSizeChanged(object sender, EventArgs e)
        {
            NoParamDele dele = new NoParamDele(formHide);
            Invoke(dele);
        }

        private void formHide()
        {
            this.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        private void 서버종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void MnServerStop_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void MnServerStart_Click(object sender, EventArgs e)
        {
            startServer();
        }

    }
}
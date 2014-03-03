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
        private IPEndPoint sender = null;
        
        private Socket crmSocket = null;
        
        private Thread ListenThread = null;
        private Thread CrmReceiverThread;

        private Socket statsock = null;


        private TcpServerMgr mServer;

        /// <summary>
        /// 사용자 리스트 생성, 통신소켓 생성 및 Listen 쓰레드 시작
        /// </summary>
        public void StartListener()                 //서버 시작 메소드 
        {
            try
            {
                crmSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint crm = new IPEndPoint(IPAddress.Any, configCtrl.CrmPort);       //서버체크 전용 EndPoint 설정 
                crmSocket.Bind(crm);
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
                throw new Exception("CRM 통신 전용 소켓을 특정 EndPoint 바인딩하는데 실패하였습니다.");
            }

            try
            {
                CrmReceiverThread = new Thread(new ThreadStart(ReceiveCRMRequest));
                CrmReceiverThread.Start();
                LogWrite("CrmReceiverThread Thread 시작!");
                LogWrite("서버 준비 완료!");
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
                throw new Exception("Listener 쓰레드기동을 실패하였습니다.");
            }
        }

        /// <summary>
        /// 사용자의 Connect에 응답하여 사용자 전용 소켓 쓰레드를 시작한다.
        /// </summary>
        //private void Listener()
        //{
        //    try
        //    {
        //        tcpsock.Listen(5);
        //        while (true)
        //        {
        //            Socket listen = tcpsock.Accept();
        //            listen.Blocking = true;
        //            logWrite("연결요청 허용!");
        //            ReceiverThread = new Thread(new ParameterizedThreadStart(ReceiveMsg));
        //            ReceiverThread.Start(listen);
        //            logWrite("ReceiverThread 시작!");
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}

  
        public void ReceiveCRMRequest()
        {
            try
            {
                crmSocket.Listen(10);
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[56];
                        Socket tempSocket = crmSocket.Accept();
                        LogWrite("CRM 접속");
                        int count = tempSocket.Receive(buffer);
                        LogWrite("CRM Request 수신!  " + count.ToString() + " byte");
                        byte[] content = new byte[count];
                        for (int i = 0; i < count; i++)
                        {
                            content[i] = buffer[i];
                        }
                        string crmmsg = Encoding.Default.GetString(content);
                        LogWrite("CRM Request Message : " + crmmsg);
                        string[] tempStr = crmmsg.Split('&');
                        int mode = Convert.ToInt32(tempStr[0]);
                        ArrayList list = new ArrayList();
                        list.Add(mode);
                        list.Add(crmmsg);
                        Thread msgThread = new Thread(new ParameterizedThreadStart(receiveReq));
                        msgThread.Start(list);
                    }
                    catch (SocketException e)
                    {
                        LogWrite("ReceiveCRMRequest() 에러 : " + e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                LogWrite(exception.ToString());
            }
        }

        public bool SendMsg(Socket clientSoc, string msg)
        {
            LogWrite(string.Format("메시지전송 to[{0}] msg[{1}] ",clientSoc.RemoteEndPoint.ToString(), msg));
            return mServer.SendMsg(clientSoc, msg);
        }

        public bool SendMsg(string msg, IPEndPoint iep)
        {
            LogWrite(string.Format("메시지전송 to[{0}] msg[{1}] ", iep.ToString(), msg));
            return mServer.SendMsg(msg, iep);
        }

        public bool SendRinging(string msg, IPEndPoint iep)
        {
            LogWrite(string.Format("콜전송 to[{0}] msg[{1}] ", iep.ToString(), msg));
            return mServer.SendMsg(msg, iep);
        }
    }
}
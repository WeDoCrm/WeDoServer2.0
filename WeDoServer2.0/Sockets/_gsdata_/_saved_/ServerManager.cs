using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using WeDoCommon;
using System.Net;
using System.Net.Sockets;

namespace WeDoCommon.Sockets
{
    public class AbstractServerMgr<T>
    {
        protected T server;
        protected Thread thServer;
        protected int mPort = 0;
        protected string mMgrKey;

        public event EventHandler<SocStatusEventArgs> SocStatusChanged;
        public event EventHandler<SocStatusEventArgs> ReadyToListen;


        public AbstractServerMgr(int port)
        {
            mPort = port;
        }

        public virtual void OnSocStatusChanged(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = SocStatusChanged;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void ServerMgrStatusChanged(object sender, SocStatusEventArgs e)
        {
            OnSocStatusChanged(e);
        }

        public virtual void DoRun()
        {
            thServer = new Thread(new ThreadStart(Start));
            thServer.Start();
        }

        public void Dispose()
        {
            (server as SyncSocListener).Dispose();
        }

        public void Start()
        {
            Logger.info(string.Format("server manager[{0}] starting",mMgrKey));
            (server as SyncSocListener).ReadyToListen += ProcessOnReadyToListen;
            (server as SyncSocListener).StartListening();
        }

        public bool isListening()
        {
            return ((server as SyncSocListener) != null && (server as SyncSocListener).IsListenerBound());
        }

        public void Stop()
        {
            Logger.info(string.Format("server manager[{0}] stopping",mMgrKey));
            (server as SyncSocListener).StopListening();
        }

        public bool IsListenerReady()
        {
            return (server as SyncSocListener).IsListenerBound();
        }

        public void listClient()
        {
            Logger.info(string.Format("server manager connection[{0}] listup",mMgrKey));
            (server as SyncSocListener).listClient();
        }

        public void BroadCast(string msg)
        {
            (server as SyncSocListener).BroadCast(msg);
        }

        public bool SendMsg(Socket clientSoc, string message)
        {
            return ((server as SyncSocListener).Send(clientSoc, string.Format(MsgDef.MSG_TEXT_FMT, MsgDef.MSG_TEXT, message)) == SocCode.SOC_ERR_CODE);
        }

        public bool SendMsg(string message, IPEndPoint iep)
        {
            return ((server as SyncSocListener).Send(iep, string.Format(MsgDef.MSG_TEXT_FMT, MsgDef.MSG_TEXT, message)) == SocCode.SOC_ERR_CODE);
        }

        public void OnReadyToListen(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = ReadyToListen;
            if (handler != null)
                handler(this, e);
        }

        public void ProcessOnReadyToListen(object sender, SocStatusEventArgs e)
        {
            OnReadyToListen(e);
        }
    }

    public class TcpServerMgr : AbstractServerMgr<TcpSocketListener>
    {
        public event EventHandler<SocStatusEventArgs> FTPListenRequested;

        public TcpServerMgr(int port)
            : base(port)
        {
            this.mMgrKey = "TCP_SMGR";
            server = new TcpSocketListener(mPort);
            server.SocStatusChanged += ServerMgrStatusChanged;

            server.FTPListenRequested += ProcessOnFTPListenRequested;
        }

        public void OnFTPListenRequested(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = FTPListenRequested;
            if (handler != null)
                handler(this, e);
        }

        public void ProcessOnFTPListenRequested(object sender, SocStatusEventArgs e)
        {
            OnFTPListenRequested(e);
        }

        public void NoticeFTPReady(StateObject obj)
        {
            server.NotifyFTPReady(obj);
        }
    }

    public class FtpServerMgr : AbstractServerMgr<FtpSocketListener>
    {
        protected Thread thCheckListenerRunning;
        public event EventHandler<FTPStatusEventArgs> FTPReceivingProgressed;   // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPReceivingFinished;     // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPReceivingCanceled;     // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPReceivingFailed;     // FTP Manager에서 발생하는 이벤트


        public FtpServerMgr(int port, string savePath)
            : base(port)
        {
            this.mMgrKey = "TCP_SMGR";
            server = new FtpSocketListener(mPort, savePath);
            server.SocStatusChanged += ServerMgrStatusChanged;
        }

        public void CheckRunning(object invokingObject)
        {
            server.CheckRunning((StateObject)invokingObject);
        }

        public virtual void DoRun(StateObject invokingObject)
        {
            thServer = new Thread(new ThreadStart(Start));
            thServer.Start();
            thCheckListenerRunning = new Thread(new ParameterizedThreadStart(CheckRunning));
            thCheckListenerRunning.Start((object)invokingObject);
        }

        protected override void ServerMgrStatusChanged(object sender, SocStatusEventArgs e)
        {
            if (e.Status.Cmd == MsgDef.MSG_BYE)
            {
                Stop();
            }

            if (e.Status.Status == SocHandlerStatus.RECEIVING && e.Status.FtpStatus == FTPStatus.RECEIVE_STREAM)
            {
                int index = (int)(e.Status.FileSizeDone * (long)100 / e.Status.FileSize);
                string msg = "수신중(" + index + " %)";
                string printMsg = "수신중(" + index + " %)";
                Logger.info("ServerMgrStatusChanged: msg=" + msg);
                OnFTPReceivingProgressed(new FTPStatusEventArgs(e, msg, printMsg, index));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_END)
            {
                string msg = "수신완료";
                string printMsg = string.Format("파일 수신이 완료되었습니다.({0})", e.Status.FileName);
                Logger.info("ServerMgrStatusChanged: msg=" + msg);
                OnFTPReceivingFinished(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_SERVER_CANCELED)
            {
                string msg = "수신취소";
                string printMsg = string.Format("파일 수신이 취소되었습니다.({0})", e.Status.FileName);
                Logger.info("ServerMgrStatusChanged: msg=" + msg);
                OnFTPReceivingCanceled(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_CANCELED)
            {
                string msg = "수신취소";
                string printMsg = string.Format("전송자가 파일 전송을 취소되었습니다.({0})", e.Status.FileName);
                Logger.info("ServerMgrStatusChanged: msg=" + msg);
                OnFTPReceivingCanceled(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.ERROR)
            {
                string msg = "수신실패";
                string printMsg = string.Format("파일 수신이 실패하였습니다.({0})", e.Status.FileName);
                Logger.info("ServerMgrStatusChanged: msg=" + msg);
                OnFTPReceivingFailed(new FTPStatusEventArgs(e, msg, printMsg));
            }

            base.OnSocStatusChanged(e);
        }

        public void OnFTPReceivingProgressed(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPReceivingProgressed;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPReceivingFinished(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPReceivingFinished;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPReceivingCanceled(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPReceivingCanceled;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPReceivingFailed(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPReceivingFailed;
            if (handler != null)
                handler(this, e);
        }

        public void CancelReceiving(StateObject stateObj)
        {
            server.CancelReceiving(stateObj);
        }
    }
}

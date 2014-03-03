using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;

namespace WeDoCommon.Sockets
{
    class MsgrClientManager
    {
        TcpClientManager tcpManager;

        Dictionary<string, FtpClientManager> ftpManagers = new Dictionary<string,FtpClientManager>();//key=ftpManager.key;
        //FtpClientManager ftpManager;
        Object ftpManagersLock = new Object();

        string mIpAddress;
        int mPort;
        string mKey;
        Dictionary<string, FTPSendObj> mFileInfoMap = new Dictionary<string, FTPSendObj>();

        public event EventHandler<SocStatusEventArgs> TCPStatusChanged;     // TCP Manager에서 발생하는 이벤트 
        public event EventHandler<SocStatusEventArgs> ManagerStatusChanged; // MsgrClientManager 자체메시지 전달용
        public event EventHandler<SocStatusEventArgs> TCPMsgReceived;

        public event EventHandler<FTPStatusEventArgs> FTPSendingProgressed;   // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPSendingFinished;     // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPSendingCanceled;     // FTP Manager에서 발생하는 이벤트
        public event EventHandler<FTPStatusEventArgs> FTPSendingFailed;     // FTP Manager에서 발생하는 이벤트
        public event EventHandler<SocStatusEventArgs> TCPConnectionError;
        public event EventHandler<FTPStatusEventArgs> FTPConnectionError;

        public event EventHandler<SocFTPInfoEventArgs<FTPRcvObj>> FTPSendingNotified;
        public event EventHandler<SocFTPInfoEventArgs<FTPSendObj>> FTPSendingAccepted;
        public event EventHandler<SocFTPInfoEventArgs<FTPSendObj>> FTPSendingRejected;

        public MsgrClientManager(string ipAddress, int port)
        {
            mIpAddress = ipAddress;
            mPort = port;
            Initialize();
        }

        public MsgrClientManager(string ipAddress, int port, string key)
        {
            mIpAddress = ipAddress;
            mPort = port;
            mKey = key;
            Initialize();
        }

        private void Initialize()
        {
            tcpManager = new TcpClientManager(mIpAddress, mPort, mKey);
            tcpManager.SocStatusChanged += this.TcpClientStatusChanged;
            tcpManager.MessageReceived += ProcessOnMessageReceived;
            tcpManager.ConnectionError += ProcessOnTCPConnectionError;
        }

        public bool IsConnected()
        {
            return tcpManager.IsConnected();
        }

        public bool Connect()
        {
            return tcpManager.Connect();
        }

        public void Close()
        {
            tcpManager.Close();
        }

        #region TCPStatusChanged
        public virtual void OnSocStatusChanged(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = TCPStatusChanged;
            if (handler != null)
                handler(this, e);
        }

        public virtual void OnSocStatusChangedOnDebug(SocStatusEventArgs e)
        {
            if (Logger.level >= LOGLEVEL.DEBUG)
                OnSocStatusChanged(e);
        }

        public virtual void OnSocStatusChangedOnInfo(SocStatusEventArgs e)
        {
            if (Logger.level >= LOGLEVEL.INFO)
                OnSocStatusChanged(e);
        }

        public virtual void OnSocStatusChangedOnError(SocStatusEventArgs e)
        {
            if (Logger.level >= LOGLEVEL.ERROR)
                OnSocStatusChanged(e);
        }

        protected virtual void TcpClientStatusChanged(object sender, SocStatusEventArgs e)
        {
            OnSocStatusChanged(e);
        }

        public void ErrorRaise(Exception ex)
        {
            StateObject stateObj = new StateObject(ex);
            OnSocStatusChanged(new SocStatusEventArgs(stateObj));
        }
        #endregion

        public virtual void OnManagerStatusChanged(string msg)
        {
            EventHandler<SocStatusEventArgs> handler = ManagerStatusChanged;
            if (handler != null)
            {
                StateObject stObj = new StateObject();
                stObj.SocMessage = msg;
                handler(this, new SocStatusEventArgs(stObj));
            }
        }

        public void ReceiveMessage()
        {
            tcpManager.ReceiveMessage();
        }

        public bool SendMsg(string msg)
        {
            return tcpManager.Send(string.Format(MsgDef.MSG_TEXT_FMT, MsgDef.MSG_TEXT, msg));
        }

        /**
         * 1. (A->B) 서버에 파일전송 알림
         *    FTP_INFO_TO_SVR 전송자-파일-수신자
         *    action: B->C로 전달
         * 2. (B->C) 파일정보 알려줌
         *    FTP_INFO_TO_RCV 전송자-파일-수신자
         *    action: C->B로 리턴. 추가조치없음
         * ----->FTP 리스너 기동
         * 3.(C->B) 수신준비대기/거부 통지
         *    FTP_READY_TO_SVR/FTP_REJECT_TO_SVR 전송자-파일-수신자
         *    action: A로 릴레이
         * 4. (B->A) 수신준비대기/거부 통지
         *   FTP_READY_TO_SND/FTP_REJECT_TO_SND 전송자-파일-수신자
         *   action: 파일전송소켓 접속
         * ----->FTP접속--------------------------------------
         * 파일전송 알림 1번작업
         * 1. Cli A Send File Noti , Wait for Ack
         * 2. Svr B Run FTPListener
         * 3. Svr B Send Info | Nack
                     * 6. Cli A Run FTPClient
                     * 7. Cli A Done
                     * 8. Cli A BYE
         */
        public bool NotifyFTPStart(IPEndPoint remoteIE, string receiverId, string fileName, long fileSize)
        {
            string msgRequest = String.Format(MsgDef.FMT_FTP_INFO_TO_SVR, MsgDef.MSG_FTP_INFO_TO_SVR, this.mKey, SocUtils.GetFileName(fileName), fileSize, receiverId);
            Logger.info(string.Format("1단계 전송자=>서버 파일전송공지 NotifyFTPStart[{0}]", msgRequest));
            return _NotifyFTPMsg(msgRequest, remoteIE, this.mKey, fileName, fileSize, receiverId);
        }

        /// <summary>
        /// FTP전송 통지에 대해 승인 및 FTP수신대기
        /// </summary>
        /// <param name="remoteIE"></param>
        /// <param name="receiverId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public bool NotifyFTPReady(IPEndPoint remoteIE, string senderId, string fileName, long fileSize)
        {
            string msgRequest = String.Format(MsgDef.FMT_FTP_READY_TO_SVR, MsgDef.MSG_FTP_READY_TO_SVR, senderId, SocUtils.GetFileName(fileName), fileSize, this.mKey);
            Logger.info(string.Format("3단계 수신자=>서버 파일수신준비완료 NotifyFTPReady[{0}]", msgRequest));
            return _NotifyFTPMsg(msgRequest, remoteIE, senderId, fileName, fileSize, this.mKey);
        }

        /// <summary>
        /// FTP전송 통지에 대해 거부
        /// </summary>
        /// <param name="remoteIE"></param>
        /// <param name="receiverId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public bool NotifyFTPReject(IPEndPoint remoteIE, string senderId, string fileName, long fileSize)
        {
            string msgRequest = String.Format(MsgDef.FMT_FTP_REJECT_TO_SVR, MsgDef.MSG_FTP_REJECT_TO_SVR, senderId, SocUtils.GetFileName(fileName), fileSize, this.mKey);
            Logger.info(string.Format("3단계 수신자=>서버 파일수신준비거부 NotifyFTPReject[{0}]", msgRequest));
            return _NotifyFTPMsg(msgRequest, remoteIE, senderId, fileName, fileSize, this.mKey);
        }

        public bool _NotifyFTPMsg(string msgRequest, IPEndPoint remoteIE, string senderId, string fileName, long fileSize, string receiverId)
        {
            tcpManager.Send(msgRequest);
            //다음 부분은 Sender부분으로 Receiver부분인 NotifyFTPReady, NotifyFTPReject에 대해선 필요없는 부분
            if (senderId.Equals(this.mKey))
            {
                string msgKey = SocUtils.GenerateFTPClientKey(senderId, SocUtils.GetFileName(fileName), fileSize, receiverId);
                if (mFileInfoMap.ContainsKey(msgKey))
                    mFileInfoMap.Remove(msgKey);
                mFileInfoMap.Add(msgKey, new FTPSendObj(remoteIE, msgKey, fileName, fileSize, receiverId));
            }
            OnManagerStatusChanged(string.Format("파일정보 전송[{0}].", fileName));
            return true;
        }

        public bool StartFTP(IPEndPoint ie, string receiverId, string fileName, long fileSize)
        {
            FtpClientManager ftpManager = new FtpClientManager(ie, mKey, fileName,fileSize, receiverId);

            ftpManager.FTPStatusChanged += ProcessOnFTPStatusChanged;
            ftpManager.FTPConnectionError += ProcessOnFTPConnectionError;

            if (!ftpManager.IsConnected())
            {
                if (ftpManager.Connect())
                    OnManagerStatusChanged("[SERVER_CONNECT]Server Connected.");
                else
                {
                    OnManagerStatusChanged("[SERVER_CONNECT]Server Not Connected.");
                    ftpManager.ForceClose();
                    return false;
                }
            }
            else
                OnManagerStatusChanged("[SERVER_CONNECT]Server Already Connected.");
            lock (ftpManagersLock)
            {
                if (ftpManagers.ContainsKey(ftpManager.GetKey()))
                    ftpManagers.Remove(ftpManager.GetKey());
                ftpManagers.Add(ftpManager.GetKey(), ftpManager);
            }
            return true;
        }


        public void SendFile(object key)
        {
            try
            {
                    FtpClientManager ftpManager;
                    lock (ftpManagersLock)
                    {
                        if (ftpManagers.ContainsKey((string)key))
                            ftpManager = ftpManagers[(string)key];
                        else
                            throw new Exception(string.Format("해당 키[{0}]로 지정된 ftp코넥션이 없음", key));
                    }
                    if (ftpManager == null)
                    {
                        OnManagerStatusChanged("FTP미접속상태");
                        return;
                    }
                    if (ftpManager.SendFile())
                        OnManagerStatusChanged(string.Format("Sent:{0} finished.", key));
                    else
                    {
                        OnManagerStatusChanged(string.Format("Sent:{0} error", key));
                        ftpManager.ForceClose();
                        ftpManagers.Remove((string)key);
                    }
            }
            catch (IOException ie)
            {
                Logger.error(ie.ToString());
            }
        }

        public void CancelFTPSending(object key)
        {
            try
            {
                FtpClientManager ftpManager;
                lock (ftpManagersLock)
                {
                    if (ftpManagers.ContainsKey((string)key))
                        ftpManager = ftpManagers[(string)key];
                    else
                        throw new Exception(string.Format("해당 키[{0}]로 지정된 ftp코넥션이 없음", key));
                }
                if (ftpManager == null)
                {
                    OnManagerStatusChanged("FTP미접속상태");
                    return;
                }
                ftpManager.CancelSending();
            }
            catch (IOException ie)
            {
                Logger.error(ie.ToString());
                ErrorRaise(ie);
            }
        }

        public void OnFTPSendingProgressed(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPSendingProgressed;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingFinished(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPSendingFinished;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingCanceled(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPSendingCanceled;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingFailed(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPSendingFailed;
            if (handler != null)
                handler(this, e);
        }

        private void ProcessOnFTPStatusChanged(object sender, FTPStatusEventArgs e)
        {
            if (e.Status.Status == SocHandlerStatus.FTP_SENDING)
            {
                int index = (int)(e.Status.FileSizeDone * (long)100 / e.Status.FileSize);
                string msg = "전송중(" + index + " %)";
                string printMsg = "전송중(" + index+ " %)";
                Logger.info("ProcessOnFTPStatusChanged: msg=" + msg);
                OnFTPSendingProgressed(new FTPStatusEventArgs(e, msg, printMsg, index));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_END)
            {
                string msg = "전송완료";
                string printMsg = string.Format("파일 전송이 완료되었습니다.({0})", e.Status.FileName);
                Logger.info("ProcessOnFTPStatusChanged: msg=" + msg);
                OnFTPSendingFinished(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_CANCELED)
            {
                string msg = "전송취소";
                string printMsg = string.Format("파일 전송이 취소되었습니다.({0})", e.Status.FileName);
                Logger.info("ProcessOnFTPStatusChanged: msg=" + msg);
                OnFTPSendingCanceled(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.FTP_SERVER_CANCELED)
            {
                string msg = "전송취소";
                string printMsg = string.Format("수신자가 파일 파일수신을 취소되었습니다.({0})", e.Status.FileName);
                Logger.info("ProcessOnFTPStatusChanged: msg=" + msg);
                OnFTPSendingCanceled(new FTPStatusEventArgs(e, msg, printMsg));
            }
            else if (e.Status.Status == SocHandlerStatus.ERROR)
            {
                string msg = "전송실패";
                string printMsg = string.Format("파일 전송이 실패하였습니다.({0})", e.Status.FileName);
                Logger.info("ProcessOnFTPStatusChanged: msg=" + msg);
                OnFTPSendingFailed(new FTPStatusEventArgs(e, msg, printMsg));
            }
        }

        public void OnTCPMsgReceived(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = TCPMsgReceived;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingNotified(SocFTPInfoEventArgs<FTPRcvObj> e)
        {
            EventHandler<SocFTPInfoEventArgs<FTPRcvObj>> handler = FTPSendingNotified;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingAccepted(SocFTPInfoEventArgs<FTPSendObj> e)
        {
            EventHandler<SocFTPInfoEventArgs<FTPSendObj>> handler = FTPSendingAccepted;
            if (handler != null)
                handler(this, e);
        }

        public void OnFTPSendingRejected(SocFTPInfoEventArgs<FTPSendObj> e)
        {
            EventHandler<SocFTPInfoEventArgs<FTPSendObj>> handler = FTPSendingRejected;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// TcpClient인 경우 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ProcessOnMessageReceived(object sender, SocStatusEventArgs e)
        {
            //OnMessageReceived(e);
            switch (e.Status.Cmd)
            {
                case MsgDef.MSG_TEXT:
                    {
                        OnTCPMsgReceived(e);
                        break;
                    }
                case MsgDef.MSG_BYE:
                    //처리없음
                    break;
                case MsgDef.MSG_FTP_INFO_TO_RCV:
                    //받는쪽. 파일전송의사가 전달됨
                    //1. 서버로 재전송
                    {
                        //string msg = e.Status.Data.Substring(MsgDef.MSG_TEXT.Length+1); //"MSG|..."
                        //this.Send(e.Status.Data);
                        //2. 하위단에서 이벤트처리토록 둠.
                        OnManagerStatusChanged(string.Format("2단계 서버 =>전송자 파일전송공지[{0}]", e.Status.Data));
                        string[] msgToken = e.Status.Data.Split(SocConst.TOKEN);
                        string senderId = msgToken[1];
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string mapKey = SocUtils.GenerateFTPClientKey(mKey, fileName, fileSize, senderId);
                        FTPRcvObj rcvObj = new FTPRcvObj((IPEndPoint)e.Status.Soc.RemoteEndPoint, mapKey, fileName, fileSize, senderId);
                        OnFTPSendingNotified(new SocFTPInfoEventArgs<FTPRcvObj>(rcvObj));
                        break;
                    }
                case MsgDef.MSG_FTP_READY_TO_SND://header|(senderid|ip)|filename|filesize|(receiverId|ip or m) 'm'은 서버
                    {
                        //수신준비된것이 확인됨.==> FTP기동
                        //필요한 인자: 파일정보, 수신자정보
                        OnManagerStatusChanged(string.Format("4단계 서버=>전송자 파일수신준비완료[{0}]", e.Status.Data));
                        string[] msgToken = e.Status.Data.Split(SocConst.TOKEN);
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string receiverId = msgToken[4];
                        string mapKey = SocUtils.GenerateFTPClientKey(mKey, fileName, fileSize, receiverId);
                        FTPSendObj sendObj = mFileInfoMap[mapKey];
                        string fullFileName = sendObj.FileName;

                        if (mFileInfoMap.ContainsKey(mapKey))
                            mFileInfoMap.Remove(mapKey);
                        StartFTP(sendObj.RemoteEndPoint, receiverId, fullFileName, fileSize);
                        Thread thServer = new Thread(new ParameterizedThreadStart(SendFile));
                        thServer.Start((object)mapKey);

                        OnFTPSendingAccepted(new SocFTPInfoEventArgs<FTPSendObj>(sendObj));
                        break;
                    }
                case MsgDef.MSG_FTP_REJECT_TO_SND://header|(senderid|ip)|filename|filesize|(receiverId|ip or m) 'm'은 서버
                    {
                        //파일전송의사가 전달됨
                        //처리없음
                        OnManagerStatusChanged(string.Format("4단계 서버=>전송자 파일수신준비거부[{0}]", e.Status.Data));
                        string[] msgToken = e.Status.Data.Split(SocConst.TOKEN);
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string receiverId = msgToken[4];
                        string mapKey = SocUtils.GenerateFTPClientKey(mKey, fileName, fileSize, receiverId);
                        FTPSendObj sendObj = mFileInfoMap[mapKey];
                        OnFTPSendingRejected(new SocFTPInfoEventArgs<FTPSendObj>(sendObj));
                        break;
                    }
                default:
                    break;

            }
        }

        protected virtual void ProcessOnFTPConnectionError(object sender, FTPStatusEventArgs e)
        {
            FtpClientManager ftpManager;
            try
            {
                lock (ftpManagersLock)
                {
                    if (ftpManagers.ContainsKey(e.Status.Key))
                        ftpManager = ftpManagers[e.Status.Key];
                    else
                        throw new Exception(string.Format("해당 키[{0}]로 지정된 ftp코넥션이 없음", e.Status.Key));
                    ftpManager.ForceClose();
                    ftpManagers.Remove(e.Status.Key);
                    OnFTPConnectionError(e);
                }
            }
            catch (Exception ex)
            {
                Logger.error(ex.ToString());
                ErrorRaise(ex);
            }
        }

        public void OnFTPConnectionError(FTPStatusEventArgs e)
        {
            EventHandler<FTPStatusEventArgs> handler = FTPConnectionError;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void ProcessOnTCPConnectionError(object sender, SocStatusEventArgs e)
        {
            tcpManager.ForceClose();
            OnTCPConnectionError(e);
        }

        public void OnTCPConnectionError(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = TCPConnectionError;
            if (handler != null)
                handler(this, e);
        }
    }
}

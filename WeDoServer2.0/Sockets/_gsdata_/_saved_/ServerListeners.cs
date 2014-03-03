using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WeDoCommon.Sockets
{
    public class TcpSocketListener : SyncSocListener
    {

        public TcpSocketListener(int port)
            : base(port)
        {
        }

        public override void ProcessMsg(StateObject socObj)
        {
            base.ProcessMsg(socObj);

            Logger.debug("TCP ProcessMsg Start");
            StateObject stateObj = (StateObject)socObj;

            switch (stateObj.Cmd)
            {
                case MsgDef.MSG_KEY_INFO:
                    {
                        stateObj.SocMessage = string.Format("KEY메시지 수신[{0}]", stateObj.Data);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        //클라이언트 key ( user id)정보 
                        string[] msgToken = stateObj.Data.Split('|');
                        string userKey = msgToken[1];
                        lock (mClientTableLock)
                        {
                            stateObj.Key = userKey;
                            mClientKeyMap[userKey] = stateObj.Soc.RemoteEndPoint.ToString();
                        }
                        if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                        {
                            stateObj.MsgStatus = MSGStatus.ERROR;
                            throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                        }
                    }
                    break;
                case MsgDef.MSG_FTP_INFO_TO_RCV:
                    stateObj.SocMessage = string.Format("FTP확인메시지 수신[{0}]", stateObj.Data);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                    //아무것도 안함
                    break;
                case MsgDef.MSG_FTP_INFO_TO_SVR:
                    {
                        stateObj.SocMessage = string.Format("FTP메시지 수신: 1단계 전송자=>서버 파일전송공지[{0}]", stateObj.Data);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        //파일 정보 수신 
                        // 수신자가 서버이면 
                        string[] msgToken = stateObj.Data.Split('|');
                        string senderId = msgToken[1];
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string receiverId = msgToken[4];
                        if (receiverId.Equals(MsgDef.FTP_ON_SERVER)) //서버전송인 경우
                            OnFTPListenRequested(new SocStatusEventArgs(stateObj.Clone()));
                        else
                        {
                            //수신자 접속소켓 구함
                            Socket receiverSoc;
                            lock (mClientTableLock)
                            {
                                string clientStateObjKey = mClientKeyMap[receiverId];
                                StateObject receiverStateObj = mHtClientTable[clientStateObjKey];
                                receiverSoc = receiverStateObj.Soc;
                            }
                            //수신자에게 메시지 전달
                            string message = string.Format(MsgDef.FMT_FTP_INFO_TO_RCV, MsgDef.MSG_FTP_INFO_TO_RCV, senderId, fileName, fileSize, receiverId);
                            stateObj.SocMessage = string.Format("FTP메시지 전송:2단계 서버 =>수신자 파일전송공지[{0}]", message);
                            OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                            if (Send(receiverSoc, message) == SocCode.SOC_ERR_CODE)
                            {
                                stateObj.MsgStatus = MSGStatus.ERROR;
                                throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                            }
                        }
                    }
                    break;
                case MsgDef.MSG_FTP_READY_TO_SVR://수신자가 수신준비완료
                    {
                        stateObj.SocMessage = string.Format("FTP메시지 수신:3단계 수신자=>서버 파일수신준비완료[{0}]", stateObj.Data);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        //A로 전달함.
                        string[] msgToken = stateObj.Data.Split('|');
                        string senderId = msgToken[1];
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string receiverId = msgToken[4];
                        //전송자 접속 구함
                        Socket senderSoc;
                        lock (mClientTableLock)
                        {
                            string clientStateObjKey = mClientKeyMap[senderId];
                            StateObject senderStateObj = mHtClientTable[clientStateObjKey];
                            senderSoc = senderStateObj.Soc;
                        }
                        //전송자에게 메시지 전달
                        string message = string.Format(MsgDef.FMT_FTP_READY_TO_SND, MsgDef.MSG_FTP_READY_TO_SND, senderId, fileName, fileSize, receiverId);
                        stateObj.SocMessage = string.Format("FTP메시지 전송:4단계 서버=>전송자 파일수신준비완료[{0}]", message);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        if (Send(senderSoc, message) == SocCode.SOC_ERR_CODE)
                        {
                            stateObj.MsgStatus = MSGStatus.ERROR;
                            throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                        }
                    }
                    break;
                case MsgDef.MSG_FTP_REJECT_TO_SVR://수신자가 수신거부
                    {
                        stateObj.SocMessage = string.Format("FTP메시지 수신: 3단계 수신자=>서버 파일수신준비거부[{0}]", stateObj.Data);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        //A로 전달함.
                        string[] msgToken = stateObj.Data.Split('|');
                        string senderId = msgToken[1];
                        string fileName = msgToken[2];
                        long fileSize = Convert.ToInt64(msgToken[3]);
                        string receiverId = msgToken[4];
                        //전송자 접속 구함
                        Socket senderSoc;
                        lock (mClientTableLock)
                        {
                            string clientStateObjKey = mClientKeyMap[senderId];
                            StateObject senderStateObj = mHtClientTable[clientStateObjKey];
                            senderSoc = senderStateObj.Soc;
                        }
                        //전송자에게 메시지 전달
                        string message = string.Format(MsgDef.FMT_FTP_REJECT_TO_SND, MsgDef.MSG_FTP_REJECT_TO_SND, senderId, fileName, fileSize, receiverId);
                        stateObj.SocMessage = string.Format("FTP메시지 전송:4단계 서버=>전송자 파일수신준비거부[{0}]", message);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        if (Send(senderSoc, message) == SocCode.SOC_ERR_CODE)
                        {
                            stateObj.MsgStatus = MSGStatus.ERROR;
                            throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                        }
                    }
                    break;
                case MsgDef.MSG_TEXT:
                    stateObj.SocMessage = string.Format("일반메시지 수신[{0}]", stateObj.Data);
                    stateObj.Status = SocHandlerStatus.RECEIVING;
                    Logger.info(stateObj);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));

                    if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                    {
                        stateObj.MsgStatus = MSGStatus.ERROR;
                        throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                    }
                    break;
                case MsgDef.MSG_BYE:
                    stateObj.SocMessage = string.Format("종료 메시지 수신:MSG_BYE/Msg[{0}]", stateObj.Data);
                    stateObj.Status = SocHandlerStatus.DISCONNECTED;

                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                    Logger.info(stateObj);
                    if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                    {
                        stateObj.MsgStatus = MSGStatus.ERROR;
                        throw new Exception(string.Format("종료 메시지 전송에러:MSG_BYE/Msg[{0}]", stateObj.Data));
                    }

                    CloseClient(stateObj.Soc);
                    break;

                default:
                    stateObj.MsgStatus = MSGStatus.NONE;
                    stateObj.SocMessage = string.Format("Unknown Msg[{0}]:MSGStatus.NONE", stateObj.Data);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                    break;
                    //전송완료 메시지 수신 -> 

            }
            stateObj.SocMessage = "TCP ProcessMsg End";
            Logger.debug(stateObj);
        }

        public event EventHandler<SocStatusEventArgs> FTPListenRequested;

        public void OnFTPListenRequested(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = FTPListenRequested;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void NotifyFTPReady(StateObject obj)
        {
            string[] msgToken = obj.Data.Split('|');
            string senderId = msgToken[1];
            string fileName = msgToken[2];
            long fileSize = Convert.ToInt64(msgToken[3]);
            string receiverId = msgToken[4];
            //전송자에게 메시지 전달
            obj.Data = string.Format(MsgDef.FMT_FTP_READY_TO_SND, MsgDef.MSG_FTP_READY_TO_SND, senderId, fileName, fileSize, receiverId);
            obj.SocMessage = string.Format("4단계 서버=>전송자 파일수신준비완료[{0}]", obj.Data);
            OnSocStatusChangedOnInfo(new SocStatusEventArgs(obj));
            if (this.SendMsg(obj) == SocCode.SOC_ERR_CODE)
            {
                obj.MsgStatus = MSGStatus.ERROR;
                throw new Exception(string.Format("일반메시지 전송에러:MSG_TEXT/Msg[{0}]", obj.Data));
            }
        }
    }

    /**
     * 1. Receive READY:File Name:File Size
     * 2. Check File Path
     * 3. Send Ack
     *        - Fail Send Nack
     * 4. Receive Stream
     *      - Save to File
     *      - Check EndOfFile by File Size
     * 5. Send Done
     * 6. Receive Ack
     * 7. Send Ack
     * 8. Receive BYE
     * 9. Send Bye
     */
    public class FtpSocketListener : SyncSocListener
    {
        string savePath = "c:\\temp";
        public string SavePath { get { return savePath; } set { savePath = value; } }

        public FtpSocketListener(int port, string path)
            : base(port)
        {
            this.mKey = "FTP_CON_";
            savePath = path;
            mWaitCount = SocConst.FTP_WAIT_COUNT;
            mWaitTimeOut = SocConst.FTP_WAIT_TIMEOUT;
        }

        public override void OnSocStatusChanged(SocStatusEventArgs e)
        {
            if (e.Status.Status == SocHandlerStatus.ERROR)
                closeFTPConnection(e.Status);
            base.OnSocStatusChanged(e);
        }

        private void closeOnError(StateObject socObj, string errMsg)
        {
            socObj.FtpStatus = FTPStatus.NONE;
            closeFTPConnection(socObj);
            throw new Exception(errMsg);
        }

        public void closeFTPConnection(StateObject statObj)
        {
            if (statObj.TempFileName != null && File.Exists(statObj.TempFileName))
                File.Delete(statObj.TempFileName);
            this.CloseClient(statObj.Soc);

            //FTP는 더이상 접속이 없으면 ftp listener를 종료한다.
            lock (mClientTableLock)
            {
                if (mHtClientTable.Count == 0) 
                    this.StopListening();
            }
        }

        public override void ReceiveMsg(object client)
        {
            base.ReceiveMsg(client);
        }

        public void CancelReceiving(StateObject obj)
        {
            lock (this.mClientTableLock)
            {
                obj.AbortFTP = true;
            }
        }

        public void CancelReceiving(Socket soc)
        {
            lock (this.mClientTableLock)
            {
                if (mHtClientTable.ContainsKey(soc.ToString()))
                {
                    StateObject obj = mHtClientTable[soc.ToString()];
                    obj.AbortFTP = true;
                }
            }
        }

        public void SetSaveFilePath(string path)
        {
            this.savePath = path;
            mServerStateObj.SocMessage = string.Format("Save Path to [{0}].", savePath);
            Logger.debug(mServerStateObj);
            OnSocStatusChangedOnDebug(new SocStatusEventArgs(mServerStateObj));
        }

        public override void ProcessMsg(StateObject socObj)
        {
            base.ProcessMsg(socObj);
            StateObject stateObj = (StateObject)socObj;

            switch (stateObj.FtpStatus)
            {
                case FTPStatus.NONE:
                    #region FTPStatus.NONE
                    {
                        //파일수신정보
                        if (stateObj.Cmd == MsgDef.MSG_SEND_FILE)
                        {
                            string[] list = stateObj.Data.Split(SocConst.TOKEN);

                            stateObj.FileName = list[1];
                            stateObj.FileSize = Convert.ToInt64(list[2]);
                            //중복방지 파일명
                            stateObj.FullFileName = SocUtils.GetValidFileName(this.savePath, stateObj.FileName, 0);
                            stateObj.TempFileName = stateObj.FullFileName + SocConst.TEMP_FILE_SUFFIX;

                            //수신대기상태
                            stateObj.Data = MsgDef.MSG_ACK;

                            //파일수신상태로 변경
                            stateObj.Status = SocHandlerStatus.RECEIVING;
                            stateObj.FtpStatus = FTPStatus.RECEIVE_STREAM;
                            stateObj.SocMessage = string.Format("파일수신정보[{0}/{1}]==>[{2}]/FTPStatus.RECEIVE_STREAM",
                                stateObj.FileName, stateObj.FileSize, stateObj.FullFileName);

                            Logger.info(stateObj);
                            OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));

                            File.Delete(stateObj.TempFileName);
                            FileStream fs = File.Open(stateObj.TempFileName, FileMode.Create, FileAccess.Write);
                            fs.Close();

                            //수신대기 알림
                            if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                                closeOnError(stateObj, string.Format("파일수신 대기메시지 전송에러:FTPStatus.NONE/Msg[{0}]", stateObj.Data));
                        } 
                        else if (stateObj.Cmd == MsgDef.MSG_KEY_INFO)
                        {   //클라이언트 key ( senderid_filename_filesize_receiverid )
                            string[] msgToken = stateObj.Data.Split('|');
                            string userKey = msgToken[1];
                            lock (mClientTableLock)
                            {
                                stateObj.Key = userKey;
                                mClientKeyMap[userKey] = stateObj.Soc.RemoteEndPoint.ToString();
                            }
                            if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                            {
                                stateObj.MsgStatus = MSGStatus.ERROR;
                                throw new Exception(string.Format("FTP메시지 전송에러:MSG_TEXT/Msg[{0}]", stateObj.Data));
                            }
                        }
                        else
                            closeOnError(stateObj, string.Format("Unknown Msg[{0}]:FTPStatus.NONE", stateObj.Data));
                    }
                    break;
                    #endregion
                #region FTPStatus.RECEIVE_STREAM
                //파일 수신
                case FTPStatus.RECEIVE_STREAM:
                    {
                        //파일 전송 취소인경우
                        if (stateObj.Cmd == MsgDef.MSG_CANCEL)
                        {
                            //수신종료
                            stateObj.Data = MsgDef.MSG_BYE;
                            if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                                closeOnError(stateObj, string.Format("파일수신종료메시지 전송에러:FTPStatus.RECEIVE_STREAM/Msg[{0}]", stateObj.Data));

                            //종료로 상태변경
                            stateObj.FtpStatus = FTPStatus.SENT_DONE;
                            stateObj.Status = SocHandlerStatus.FTP_CANCELED;
                            stateObj.SocMessage = string.Format("파일수신종료/Msg[{0}]", MsgDef.MSG_BYE);
                            Logger.info(stateObj);
                            OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                            closeFTPConnection(stateObj);
                            break;
                        }
                        //수신파일스트림 Write
                        FileStream fs = File.Open(stateObj.TempFileName, FileMode.Append, FileAccess.Write);
                        fs.Write(stateObj.Buffer, 0, stateObj.BufferSize);
                        fs.Close();
                        stateObj.FileSizeDone += stateObj.BufferSize;

                        stateObj.SocMessage = string.Format("수신중인 바이트:rcvSize[{0}]/fileSize[{1}]", stateObj.FileSizeDone, stateObj.FileSize);
                        Logger.debug(stateObj);
                        //수신완료
                        if (stateObj.FileSizeDone >= stateObj.FileSize)
                        {
                            File.Move(stateObj.TempFileName, stateObj.FullFileName);
                            stateObj.SocMessage = string.Format("수신완료한 바이트 rcvSize[{0}]/fileSize[{1}]", stateObj.FileSizeDone, stateObj.FileSize);
                            stateObj.FtpStatus = FTPStatus.SENT_DONE;
                        }

                        stateObj.Data = string.Format(MsgDef.MSG_RCVCHECK_FMT, MsgDef.MSG_RCVCHECK, stateObj.BufferSize);
                        OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));

                        stateObj.SocMessage = string.Format("수신바이트 확인전송:Msg[{0}]", stateObj.Data);
                        Logger.debug(stateObj);

                        if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                        {
                            closeOnError(stateObj, string.Format("수신바이트 확인전송에러:FTPStatus.RECEIVE_STREAM/Msg[{0}]", stateObj.Data));
                        }
                    }
                    break;
                #endregion
                #region FTPStatus.RECEIVE_CANCELED
                //수신취소
                case FTPStatus.RECEIVE_CANCELED:
                    {
                        //취소전송
                        stateObj.Data = MsgDef.MSG_CANCEL;
                        if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                            closeOnError(stateObj, string.Format("파일수신취소메시지 전송에러:FTPStatus.RECEIVE_CANCELED/Msg[{0}]", stateObj.Data));

                        //완료로 상태변경
                        stateObj.FtpStatus = FTPStatus.SENT_DONE;
                        stateObj.Status = SocHandlerStatus.FTP_SERVER_CANCELED;
                        stateObj.SocMessage = string.Format("파일수신취소/Msg[{0}]", stateObj.Data);
                        Logger.info(stateObj);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                    }
                    break;
                #endregion
                #region FTPStatus.SENT_DONE
                //수신완료
                case FTPStatus.SENT_DONE:
                    if (stateObj.Cmd == MsgDef.MSG_COMPLETE)
                    {
                        //수신종료
                        stateObj.Data = MsgDef.MSG_BYE;

                        if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                            closeOnError(stateObj, string.Format("파일수신종료메시지 전송에러:FTPStatus.SENT_DONE/Msg[{0}]", stateObj.Data));

                        stateObj.Status = SocHandlerStatus.FTP_END;
                        stateObj.SocMessage = string.Format("파일수신종료:{0}", MsgDef.MSG_BYE);
                        Logger.info(stateObj);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                        closeFTPConnection(stateObj);
                    }
                    else
                    {
                        closeOnError(stateObj, string.Format("Unknown Msg[{0}]:FTPStatus.SENT_DONE", stateObj.Data));
                    }
                    break;
                #endregion
            }
            stateObj.SocMessage = string.Format("Ftp ProcessMsg End");
            if (stateObj.AbortFTP)
            {
                stateObj.FtpStatus = FTPStatus.RECEIVE_CANCELED;
                stateObj.AbortFTP = false;
            }
            Logger.debug(stateObj);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace WeDoCommon.Sockets
{
    public class SyncSocListener
    {
        protected Socket mServerSoc;

        public event EventHandler<SocStatusEventArgs> SocStatusChanged;
        public event EventHandler<SocStatusEventArgs> ReadyToListen;
        protected int mPort = 0;
        //public Hashtable mHtClientTable;
        public Dictionary<string, StateObject> mHtClientTable = new Dictionary<string, StateObject>();
        public Dictionary<string, string> mClientKeyMap = new Dictionary<string, string>();
        protected Object mClientTableLock = new Object();
        protected Object mServerLock = new Object();
        protected StateObject mServerStateObj;
        protected string mKey;

        protected int mClientSize = 20;
        protected int mWaitCount = 0;
        protected int mWaitTimeOut = 0;//milsec

        public SyncSocListener(int port)
        {
            mPort = port;
        }

        public void SetKey(string key)
        {
            mKey = key;
        }

        public void StartListening()
        {

            IPHostEntry ipHostInfo = new IPHostEntry();
            ipHostInfo.AddressList = new IPAddress[] { new IPAddress(new Byte[] { 127, 0, 0, 1 }) };
            IPAddress ipAddress = ipHostInfo.AddressList[0];

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, mPort);

            // Create a TCP/IP socket.
            mServerSoc = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            mServerSoc.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                mServerSoc.Bind(localEndPoint);
                mServerSoc.Listen(mClientSize);
                mServerStateObj = new StateObject(mServerSoc, "");
                mServerStateObj.Key = mKey;
                mServerStateObj.Status = SocHandlerStatus.LISTENING;

                if (mPort == 0)
                    mPort = ((IPEndPoint)mServerSoc.LocalEndPoint).Port;

                Logger.info(string.Format("Start Listening. port[{0}]", mPort));

                int waitCount = 0;

                // Start listening for connections.
                while (true)
                {
                    lock (mClientTableLock)
                    {
                        if (mHtClientTable.Count >= mClientSize)
                        {
                            if (mWaitCount > 0) //0인경우 무한반복허용
                            {
                                if (waitCount >= mWaitCount)
                                {
                                    Logger.info(string.Format("Wait 횟수 {0}회 초과.", waitCount));
                                    StopListening();
                                    break;
                                } else 
                                    waitCount++;
                            }
                            mServerStateObj.SocMessage = string.Format("Wait 허용접속자수 초과 {0} >= {1}", mHtClientTable.Count, mClientSize);
                            Logger.debug(mServerStateObj);
                            System.Threading.Thread.Sleep(SocConst.WAIT_MIL_SEC);
                            continue;
                        }
                    }

                    lock (mServerLock)
                    {
                        if (mServerStateObj.Status == SocHandlerStatus.STOP)
                        {
                            Logger.info("ServerSocket Stopped.");
                            return;
                        }
                    }

                    // Program is suspended while waiting for an incoming connection.
                    mServerStateObj.SocMessage = string.Format("Port[{0}] 접속대기...", mPort);
                    Logger.info(mServerStateObj);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(mServerStateObj));
                    Socket soc = mServerSoc.Accept();
                    //WaitTimeOut 0 이상 설정한 경우 적용
                    if (mWaitTimeOut > 0)
                    {
                        soc.ReceiveTimeout = mWaitTimeOut;
                        soc.SendTimeout = mWaitTimeOut;
                    }

                    lock (mClientTableLock)
                    {
                        StateObject stateObj = new StateObject(soc, "");
                        stateObj.Status = SocHandlerStatus.CONNECTED;
                        stateObj.SocMessage = string.Format("Soc Accepted:{0}", soc.RemoteEndPoint.ToString());
                        Logger.info(stateObj);
                        OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));

                        mHtClientTable[soc.RemoteEndPoint.ToString()] = stateObj;

                        Thread thClient = new Thread(new ParameterizedThreadStart(this.ReceiveMsg));
                        thClient.Start(stateObj);
                    }
                }               
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10004) //정상 종료
                {
                    Logger.info("Socket errorCode[{0}]", e.ErrorCode);
                }
                else {
                    setErrorMessage(e, "Server Listening Error:" + e.ToString());
                }
            }
            catch (Exception e)
            {
                setErrorMessage(e, "Server Listening Error:" + e.ToString());
            }
        }

        /// <summary>
        /// 리스닝이 준비됐을때 알림
        /// </summary>
        /// <param name="invokingObject"></param>
        public void CheckRunning(StateObject invokingObject)
        {
            try
            {
                Logger.info(string.Format("checking server [{0}] running", this.mKey));
                int limitCnt = 20;
                int cnt = 0;
                while (true)
                {
                    if (IsListenerBound()) break;
                    if (cnt >= limitCnt)
                    {
                        StopListening();
                        throw new Exception("리스닝 체크 타임아웃");
                    }
                    Thread.Sleep(100);
                    cnt++;
                }
                OnReadyToListen(new SocStatusEventArgs(invokingObject));
            }
            catch (Exception e)
            {
                setErrorMessage(e, "Server Listening Error:" + e.ToString());
            }
        }

        public void StopListening()
        {
            lock (mServerLock)
            {
                if (mServerStateObj != null)
                {
                    mServerStateObj.Status = SocHandlerStatus.STOP;
                    mServerStateObj.SocMessage = "Server Listening Stopped.";
                    Logger.info(mServerStateObj);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(mServerStateObj));
                }
                if (mServerSoc != null)
                    mServerSoc.Close();
            }
        }

        public void Dispose()
        {
            StopListening();
            lock (mClientTableLock)
            {
                if (mHtClientTable == null) return;

                StateObject[] stoList = new StateObject[mHtClientTable.Values.Count];
                mHtClientTable.Values.CopyTo(stoList, 0);

                foreach (StateObject obj in stoList)
                    CloseClient(obj.Soc);
            }
        }

        public void listClient()
        {
            lock (mClientTableLock)
            {
                if (mHtClientTable == null) return;

                foreach (var entry in mHtClientTable)
                {
                    StateObject stateObj = entry.Value;
                    stateObj.SocMessage = string.Format("socket info[{0}], client key[{1}]", entry.Key, stateObj.Key);
                    Logger.info(stateObj);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                }
            }
        }

        public void OnReadyToListen(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = ReadyToListen;
            if (handler != null)
                handler(this, e);
        }

        #region OnSocStatusChanged
        public virtual void OnSocStatusChanged(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = SocStatusChanged;
            if (handler != null)
                handler(this, new SocStatusEventArgs(e.Status.Clone()));
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
        #endregion

        public virtual void ReceiveMsg(object stObj)
        {
            byte[] bufferTxtRcv = new byte[SocConst.MAX_STR_BUFFER_SIZE];
            byte[] bufferBinRcv = new byte[SocConst.TEMP_BUFFER_SIZE];
            byte[] bufferTxtStateObj = new byte[SocConst.MAX_STR_BUFFER_SIZE];
            byte[] bufferBinStateObj = new byte[SocConst.MAX_BUFFER_SIZE];

            StateObject stateObj = (StateObject)stObj;
            Socket clientSoc = stateObj.Soc;

            int connectionCheckCnt = 0;
            try
            {
                while (true) // An incoming connection needs to be processed.
                {
                    int recv = 0;
                    stateObj.BufferSize = 0;
                    stateObj.Data = "";
                    stateObj.SocMessage = "";
                    byte[] buffer;
                    if (stateObj.FtpStatus == FTPStatus.RECEIVE_STREAM)//파일수신
                    {
                        Array.Clear(bufferBinRcv, 0, bufferBinRcv.Length);
                        buffer = bufferBinRcv;
                        Array.Clear(bufferBinStateObj, 0, bufferBinStateObj.Length);
                        stateObj.Buffer = bufferBinStateObj;
                    }
                    else //일반메시지 
                    {
                        Array.Clear(bufferTxtRcv, 0, bufferTxtRcv.Length);
                        buffer = bufferTxtRcv;
                        Array.Clear(bufferTxtStateObj, 0, bufferTxtStateObj.Length);
                        stateObj.Buffer = bufferTxtStateObj;
                    }
                    Logger.debug("set buffer size =>" + buffer.Length);

                    //
                    if (!IsSocketReadyToReceive(stateObj))
                    {
                        stateObj.SocMessage = "소켓수신상태 Disconnected or Error.";
                        Logger.info(stateObj);
                        return;
                    }

                    int receivingByteInfo = 0;

                    while (true)//건별 메시지 수신
                    {
                        stateObj.Status = SocHandlerStatus.RECEIVING;
                        if (stateObj.FtpStatus != FTPStatus.RECEIVE_STREAM)//텍스트
                        {
                            stateObj.SocMessage = "메시지 수신대기";
                            Logger.debug(stateObj);
                            OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));
                        }
                        recv = clientSoc.Receive(buffer);

                        if (recv == 0)  //수신데이터 없음
                        {
                            if (connectionCheckCnt > 10)  //10회반복 데이터없으면 에러처리
                            {
                                stateObj.Data = string.Format(MsgDef.MSG_TEXT_FMT, MsgDef.MSG_TEXT, "TEST");
                                if (SendMsg(stateObj) == SocCode.SOC_ERR_CODE)
                                {
                                    stateObj.SocMessage = "소켓수신상태 Disconnected or Error.";
                                    Logger.info(stateObj);
                                    return;
                                }
                            }
                            connectionCheckCnt++;
                        }

                        if (stateObj.FtpStatus != FTPStatus.RECEIVE_STREAM)//텍스트
                        {
                            //텍스트는 메시지가 잘려서 넘어오는 경우보다 
                            //여러건이 연결해서 한꺼번에 오는 경우가 있어 이를 감안함.
                            int msgBufSize = 0;
                            while (buffer.Length > 0) 
                            {
                                //잘라서 처리
                                string resultMsg = SocUtils.GetMsgByPrefixLengthInfo(ref buffer,recv, ref msgBufSize);
                                if (resultMsg == null) break;

                                stateObj.Data = resultMsg;
                                stateObj.BufferSize = msgBufSize;
                                stateObj.SocMessage = string.Format("일반메시지수신 Msg[{0}] Size[{0}]", stateObj.Data, stateObj.BufferSize);
                                Logger.debug(stateObj);
                                OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));
                                this.ProcessMsg(stateObj);
                            } 
                        }
                        else
                        {
                            //FTP는 한 블록을 보낼때마다, 우선 블록사이즈정보를 보내고 실제바이트를 전송한다.
                            if (receivingByteInfo == 0)
                            {
                                receivingByteInfo = SocUtils.ConvertByteArrayToFileSize(buffer);
                                if (receivingByteInfo != 0)//블록사이즈정보있는 헤더
                                {
                                    recv = recv - SocConst.PREFIX_BYTE_INFO_LENGTH;
                                    //Logger.debug(string.Format("BlockCopy 헤더 receivingByteInfo[{0}]stateObj.BufferSize[{1}]recv[{2}]", receivingByteInfo, stateObj.BufferSize, recv));
                                    Buffer.BlockCopy(buffer, SocConst.PREFIX_BYTE_INFO_LENGTH, stateObj.Buffer, stateObj.BufferSize, recv);
                                }
                                else   //중간부분
                                {// error or abnormal stop , cancel
                                    //Logger.debug(string.Format("BlockCopy 헤더없는블록 receivingByteInfo[{0}]stateObj.BufferSize[{1}]recv[{2}]", receivingByteInfo, stateObj.BufferSize, recv));
                                    Buffer.BlockCopy(buffer, 0, stateObj.Buffer, stateObj.BufferSize, recv);
                                    stateObj.Data += Encoding.UTF8.GetString(buffer, 0, recv);
                                }
                                stateObj.SocMessage = string.Format("파일수신바이트[{0}].", recv);
                                //Logger.debug(string.Format("receivingByteInfo==0: receivingByteInfo[{0}]recv[{1}]", receivingByteInfo, recv));
                            }
                            else
                            {
                                //Logger.debug(string.Format("BlockCopy 중간부분 receivingByteInfo[{0}]stateObj.BufferSize[{1}]recv[{2}]", receivingByteInfo, stateObj.BufferSize, recv));
                                Buffer.BlockCopy(buffer, 0, stateObj.Buffer, stateObj.BufferSize, recv);
                                stateObj.SocMessage = string.Format("파일수신바이트[{0}].", recv);
                            }
                            stateObj.BufferSize += recv;
                            //Logger.debug(string.Format("stateObj.BufferSize += recv: stateObj.BufferSize[{0}]recv[{1}]", stateObj.BufferSize, recv));
                            //Logger.debug(stateObj.SocMessage);
                            //OnSocStatusChanged(new SocStatusEventArgs(stateObj));
                        }

                        if ((receivingByteInfo == 0 /*&& clientSoc.Available == 0*/ ) //텍스트이거나, 
                            || (receivingByteInfo > 0 && receivingByteInfo == stateObj.BufferSize)) //바이너리인데 buffer리딩이 완료된 경우
                            break;

                    }//while (true)//건별 메시지 수신

                    if (receivingByteInfo > 0 && receivingByteInfo == stateObj.BufferSize) //바이너리인데 buffer리딩이 완료된 경우
                    {
                        stateObj.SocMessage = string.Format("메시지수신 완료바이트Size[{0}]", stateObj.BufferSize);
                        Logger.debug(stateObj);
                        OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));
                        this.ProcessMsg(stateObj);
                        //리셋
                        receivingByteInfo = 0;
                        stateObj.BufferSize = 0;
                    }
                } //while (true)
            } catch (WeDoFTPCancelException wde) {
                CloseClient(clientSoc);
                StopListening();
                setErrorMessage(wde, stateObj);
            }
            catch (Exception e)
            {
                CloseClient(clientSoc);
                setErrorMessage(e, "수신에러:" + e.ToString());
            }
        }

        public bool IsListenerConnected()
        {
            return mServerSoc.Connected;
        }

        public bool IsListenerBound()
        {
            try {
                return (mServerSoc !=null && mServerSoc.LocalEndPoint != null);
            }
            catch (System.ObjectDisposedException e) {
                return false;
            }
        }

        public bool IsSocketReadyToReceive(StateObject obj)
        {
            return (obj.Status != SocHandlerStatus.DISCONNECTED && obj.Status != SocHandlerStatus.ERROR);
        }

        public virtual void ProcessMsg(StateObject socObj)
        {
            //need implementing
        }

        public void BroadCast(string msg)
        {
            lock (mClientTableLock)
            {

                foreach (var entry in mHtClientTable)
                {
                    Logger.info("[SyncSoc:BroadCast] {0}, {1}", entry.Key, entry.Value.Key);
                    Send((Socket)entry.Value.Soc, msg);
                    Send((Socket)entry.Value.Soc, msg);
                    Send((Socket)entry.Value.Soc, msg);
                }
            }
        }

        public int SendMsg(StateObject socObj)
        {
            return Send(socObj.Soc, socObj.Data);
        }

        public int Send(Socket soc, string msg)
        {
            return (Send(soc, SocUtils.GetPrefixInfo(msg)) - 8); //TEXT[msg length].... 8자리 감안
        }

        public int Send(IPEndPoint iep, string msg)
        {
            StateObject stateObj;
            lock(mClientTableLock) {
                if (mHtClientTable.ContainsKey(iep.ToString())) 
                {
                    stateObj = mHtClientTable[iep.ToString()];
                } 
                else 
                {
                    stateObj = new StateObject();
                    stateObj.SocMessage = String.Format("{0} Send 대상연결이 없음 ", iep.ToString());
                    OnSocStatusChanged(new SocStatusEventArgs(stateObj));
                    return SocCode.SOC_ERR_CODE;
                }
            }
            return (Send(stateObj.Soc, SocUtils.GetPrefixInfo(msg)) - 8); //TEXT[msg length].... 8자리 감안
        }

        public int Send(Socket soc, byte[] buffer)
        {
            int retry = 0;
            int recv = 0;
            
            StateObject stateObj = new StateObject(soc);

            while (true)
            {
                try
                {
                    stateObj.Data = Encoding.UTF8.GetString(buffer, 8, buffer.Length - 8);
                    stateObj.Status = SocHandlerStatus.SENDING;
                    stateObj.SocMessage = string.Format("메시지전송 {0} Msg[{1}]", soc.RemoteEndPoint.ToString(), stateObj.Data);

                    recv = soc.Send(buffer, SocketFlags.None);
                    Logger.debug(stateObj);
                    OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));
                    if (recv == buffer.Length) break;
                }
                catch (ArgumentNullException ane)
                {
                    stateObj.SocMessage = string.Format("메시지전송에러:{0}", ane.ToString());
                    setErrorMessage(ane, stateObj);
                    return SocCode.SOC_ERR_CODE;
                }
                catch (SocketException se)
                {
                    stateObj.SocMessage = string.Format("메시지전송에러:{0}", se.ToString());
                    setErrorMessage(se, stateObj);
                    return SocCode.SOC_ERR_CODE;
                }
                catch (Exception e)
                {
                    stateObj.SocMessage = string.Format("메시지전송에러:{0}", e.ToString());
                    setErrorMessage(e, stateObj);
                    return SocCode.SOC_ERR_CODE;
                }

                if (retry >= 3)
                {
                    stateObj.SocMessage = String.Format("메시지전송에러:retry >= 3 " + Encoding.UTF8.GetString(buffer, 0, recv));
                    setErrorMessage(new Exception("메시지전송에러:retry >= 3"), stateObj);
                    return SocCode.SOC_ERR_CODE;
                }
                retry++;
            }
            return recv;
        }

        public void CloseClient(Socket client)
        {
            Socket clientSoc = (Socket)client;
            StateObject stateObj;
            string socId = "";

            try
            {
                socId = clientSoc.RemoteEndPoint.ToString();
                lock (mClientTableLock)
                {
                    stateObj = mHtClientTable[socId];
                    mHtClientTable.Remove(clientSoc.RemoteEndPoint.ToString());
                    mClientKeyMap.Remove(stateObj.Key);
                    stateObj.Status = SocHandlerStatus.DISCONNECTED;
                    stateObj.SocMessage = String.Format("{0} socket is removed from Socket list: ", socId);
                    Logger.debug(stateObj);
                    OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));
                }
            }
            catch (Exception ex) { }

            try
            {
                if (clientSoc != null)
                {
                    clientSoc.Shutdown(SocketShutdown.Both);
                    clientSoc.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.error(ex.ToString());
            }
        }

        public void setErrorMessage(Exception e, StateObject obj)
        {
            obj.SocErrorMessage = e.Message;
            obj.Status = SocHandlerStatus.ERROR;
            Logger.error(obj);
            OnSocStatusChangedOnError(new SocStatusEventArgs(obj));
        }

        public void setErrorMessage(Exception e, string errMsg)
        {
            StateObject stateObj = new StateObject(e);
            stateObj.SocMessage = errMsg;
            setErrorMessage(e, stateObj);
        }

    }

}

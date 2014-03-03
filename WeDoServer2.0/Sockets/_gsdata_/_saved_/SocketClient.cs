using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace WeDoCommon.Sockets
{
    public class SyncSocClient
    {

        // Data buffer for incoming data.
        Socket mSender = null;
        IPEndPoint remoteEP = null;
        int mPort = 1100;

        string mRemoteInfo;

        StateObject stateObj;

        string mKey;
        public event EventHandler<SocStatusEventArgs> SocStatusChanged;
        public event EventHandler<SocStatusEventArgs> MessageReceived;
        public event EventHandler<SocStatusEventArgs> ConnectionError;

        protected bool IsText = true;

        public SyncSocClient(string _ipAddress, int port) : this(_ipAddress, port, SocConst.SOC_TIME_OUT_MIL_SEC)
        {
        }

        public SyncSocClient(string _ipAddress, int port, int timeout)
        {

            try
            {
                IPAddress ipAddress = System.Net.IPAddress.Parse(_ipAddress);
                mPort = port;
                remoteEP = new IPEndPoint(ipAddress, mPort);

                mSender = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Stream, ProtocolType.Tcp);

                if (timeout > 0)
                {
                    mSender.ReceiveTimeout = timeout;
                    mSender.SendTimeout = timeout;
                }
                //mSender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);

                // The socket will linger for 10 seconds after Socket.Close is called.
                LingerOption lingerOption = new LingerOption(true, 10);

                mSender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);

                stateObj = new StateObject(mSender);
            }
            catch (Exception e)
            {
                SetErrorMessage(e, string.Format("소켓생성Error ip[{0}]/port[{1}]/timeout[{2}]]",_ipAddress,port,timeout));
                Logger.error(e.ToString());
            }
        }

        public void SetKey(string key)
        {
            mKey = key;
            this.stateObj.Key = key;
        }

        public void SetText()
        {
            this.IsText = true;
        }

        public void SetBinary()
        {
            this.IsText = false;
        }

        public virtual void OnMessageReceived(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = MessageReceived;

            if (handler != null)
            {
                Logger.info("OnMessageReceived:"+e.Status.Data);
                handler(this, new SocStatusEventArgs(e.Status.Clone()));
            }
        }

        #region OnSocStatusChanged
        public virtual void OnSocStatusChanged(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = SocStatusChanged;
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
        #endregion

        public Socket getSocket()
        {
            return this.mSender;
        }

        public bool IsConnected()
        {
            return ((mSender != null) && mSender.Connected);
        }

        public int Connect()
        {
            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                mSender.Connect(remoteEP);

                if (mSender.Connected && mSender.RemoteEndPoint != null)
                {
                    mRemoteInfo = mSender.RemoteEndPoint.ToString();
                    stateObj.SocMessage = string.Format("Socket Connected to [{0}]",
                        mRemoteInfo);
                    stateObj.Status = SocHandlerStatus.CONNECTED;
                    Logger.info(stateObj);
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                }
                else
                {
                    stateObj.Status = SocHandlerStatus.ERROR;
                    OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
                    throw new SocketException((int)SocketError.NotConnected);
                }
            }
            catch (ArgumentNullException ane)
            {
                SetErrorMessage(ane, string.Format("ArgumentNullException : {0}", ane.ToString()));
                return SocCode.SOC_ERR_CODE;
            }
            catch (SocketException se)
            {
                SetErrorMessage(se, string.Format("SocketException[{0}] :{1}",
                    se.ErrorCode, mRemoteInfo));
                return SocCode.SOC_ERR_CODE;
            }
            catch (Exception e)
            {
                SetErrorMessage(e,string.Format("Unexpected exception : {0}", e.ToString()));
                return SocCode.SOC_ERR_CODE;
            }
            return SocCode.SOC_SUC_CODE;
        }

        public int Close()
        {
            try
            {
                continueReceiving = false;
                mSender.Shutdown(SocketShutdown.Both);
            }
            catch (ArgumentNullException ane)
            {
                SetErrorMessage(ane,string.Format("ArgumentNullException : {0}", ane.ToString()));
                return SocCode.SOC_ERR_CODE;
            }
            catch (SocketException se)
            {
                SetErrorMessage(se,string.Format("SocketException[{0}] :{1}",
                    se.ErrorCode,mRemoteInfo));
                return SocCode.SOC_ERR_CODE;
            }
            catch (Exception e)
            {
                SetErrorMessage(e,string.Format("Unexpected exception : {0}", e.ToString()));
                return SocCode.SOC_ERR_CODE;
            }
            finally
            {
                mSender.Close();
                stateObj.Status = SocHandlerStatus.DISCONNECTED;
                stateObj.SocMessage = "Socket Closed";
                Logger.info(stateObj);
                OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
            }
            return SocCode.SOC_SUC_CODE;
        }

        #region Send
        public int Send(string msg)
        {                
            // Encode the data string into a byte array.
            return (Send(Utils.GetPrefixInfo(msg)) - 8); //TEXT[msg length].... 8자리 감안
        }

        public int Send(byte[] msg)
        {
            return Send(msg, msg.Length);
        }

        public int Send(byte[] msg, int bytesSize)
        {
            int bytesSent;
            try
            {
                stateObj.Status = SocHandlerStatus.SENDING;
                // Send the data through the socket.
                bytesSent = mSender.Send(msg, bytesSize,SocketFlags.None);
                if (IsText)
                    if (bytesSent>=8)
                        stateObj.SocMessage = string.Format("Send to [{0}] text data[{1}]",
                                                            mSender.RemoteEndPoint.ToString(), Encoding.UTF8.GetString(msg, 8, bytesSent - 8));
                    else 
                        stateObj.SocMessage = string.Format("Send to [{0}] text data[{1}]",
                                                            mSender.RemoteEndPoint.ToString(), Encoding.UTF8.GetString(msg));
                else
                    stateObj.SocMessage = string.Format("Send to [{0}] bin data[{1}]",
                        mSender.RemoteEndPoint.ToString(), bytesSent);
                Logger.debug(stateObj);
                OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));

            }
            catch (ArgumentNullException ane)
            {
                SetErrorMessageOnConnectionError(stateObj,
                                                 ane, 
                                                 string.Format("Error Sending to {0} ArgumentNullException : {1}", 
                                                               mRemoteInfo, ane.ToString()));
                bytesSent = SocCode.SOC_ERR_CODE;
            }
            catch (SocketException se)
            {
                SetErrorMessageOnConnectionError(stateObj,
                                                 se, 
                                                 string.Format("Error Sending to {0} SocketException[{1}] : {2}", 
                                                                mRemoteInfo, se.ErrorCode, se.ToString()));
                bytesSent = SocCode.SOC_ERR_CODE;
            }
            catch (Exception e)
            {
                SetErrorMessageOnConnectionError(stateObj,
                                                 e, 
                                                 string.Format("Unexpected Error in Sending : {0}", e.ToString()));
                bytesSent = SocCode.SOC_ERR_CODE;
            }
            return bytesSent;
        }
        #endregion

        public int Receive(byte[] byteStr)
        {
            return Receive(byteStr, 1000);
        }

        public int Receive(byte[] byteStr, int timeoutMilSec)
        {
            // Connect the socket to the remote endpoint. Catch any errors.
            int bytesRec = 0;
            try
            {
                // Receive the response from the remote device.
                stateObj.Status = SocHandlerStatus.RECEIVING;
                stateObj.SocMessage = "Wait for receiving";
                Logger.debug(stateObj);
                OnSocStatusChangedOnDebug(new SocStatusEventArgs(stateObj));

                // Poll the socket for reception with a 10 ms timeout.
                if ((mSender.Poll(timeoutMilSec, SelectMode.SelectRead) && mSender.Available == 0) 
                    || !mSender.Connected)
                {
                    // Timed out
                    throw new Exception("소켓접속에러");
                }
                else
                {
                    bytesRec = mSender.Receive(byteStr);
                }

                if (bytesRec >= 8) 
                stateObj.SocMessage = string.Format("수신메시지[{0}]",
                                                    Encoding.UTF8.GetString(byteStr, 8, bytesRec-8));
                else
                    stateObj.SocMessage = string.Format("수신메시지[{0}]",
                                                        Encoding.UTF8.GetString(byteStr, 0, bytesRec));

                Logger.debug(stateObj);
            }
            catch (ArgumentNullException ane)
            {
                SetErrorMessageOnConnectionError(stateObj, 
                                                 ane, 
                                                 string.Format("ArgumentNullException : {0}", ane.ToString()));
            }
            catch (SocketException se)
            {
                SetErrorMessageOnConnectionError(stateObj, 
                                                 se, 
                                                 string.Format("Error Receiving from [{0}] SocketException[{1}] : {2}", 
                                                                   mRemoteInfo, se.ErrorCode, se.ToString()));
            }
            catch (Exception ex)
            {
                SetErrorMessageOnConnectionError(stateObj, 
                                                 ex, 
                                                 string.Format("Unexpected exception : {0}", ex.ToString()));
            }
            return bytesRec;
        }

        public string ReadLine()
        {
            string line = null;
            while (true)
            {
                byte[] bytes = new byte[SocConst.MAX_STR_BUFFER_SIZE];
                int bytesRec = Receive(bytes);
                int msgBufSize = 0;

                if (bytesRec > 0)
                    line = Utils.GetMsgByPrefixLengthInfo(ref bytes, bytesRec, ref msgBufSize);
                else
                    break;

                if (line.IndexOf("") > -1) break;
            }
            return line;
        }

        public string ReadFTPMsg()
        {
            string line = null;
            while (true)
            {
                byte[] bytes = new byte[SocConst.MAX_STR_BUFFER_SIZE];
                int bytesRec = Receive(bytes);
                int msgBufSize = 0;

                if (bytesRec > 0)
                    line = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                else
                    break;

                if (line.IndexOf("") > -1) break;
            }
            return line;
        }

        bool continueReceiving = true;

        public void StopReceiving()
        {
            continueReceiving = false;
        }

        public void ReceiveMessage()
        {
            string line = "";
            try
            {
                mSender.ReceiveTimeout = 0;

                while (continueReceiving)
                {
                    byte[] bytes = new byte[SocConst.MAX_STR_BUFFER_SIZE];

                    int bytesRec = Receive(bytes);

                    //텍스트는 메시지가 잘려서 넘어오는 경우보다 
                    //여러건이 연결해서 한꺼번에 오는 경우가 있어 이를 감안함.
                    int msgBufSize = 0;
                    while (bytesRec > 0)
                    {
                        //잘라서 처리
                        line = Utils.GetMsgByPrefixLengthInfo(ref bytes, bytesRec, ref msgBufSize);
                        if (line == null) break;
                        if (msgBufSize > (bytesRec - 8)) break;//메시지가 잘린 경우

                        stateObj.Data = line;
                        line = "";
                        OnMessageReceived(new SocStatusEventArgs(stateObj));
                    }

                    //if (bytesRec <= 0) {
                    //    if (line.Length > 0)
                    //    {
                    //        stateObj.Data = line;
                    //        line = "";
                    //        OnMessageReceived(new SocStatusEventArgs(stateObj));
                    //    }
                    //    continue;
                    //}

                    //if (line.IndexOf("") > -1 && line.Length > 0)
                    //{
                    //    stateObj.Data = line;
                    //    line = "";
                    //    OnMessageReceived(new SocStatusEventArgs(stateObj));
                    //}
                }
                continueReceiving = true;
            }
            catch (Exception ex)
            {
                Logger.error(ex.ToString());
            }
        }

        public void OnConnectionError(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = ConnectionError;
            if (handler != null)
                handler(this, e);
        }

        public void SetErrorMessageOnConnectionError(StateObject stateObj, Exception ex, string errMsg)
        {
            OnConnectionError(new SocStatusEventArgs(stateObj));
            SetErrorMessage(ex, errMsg);
        }

        public void SetErrorMessage(Exception e, string errMsg)
        {
            stateObj = new StateObject(e);
            stateObj.Key = mKey;
            stateObj.SocErrorMessage = e.ToString();
            stateObj.SocMessage = errMsg;
            stateObj.Status = SocHandlerStatus.ERROR;
            Logger.error(stateObj);
            OnSocStatusChangedOnError(new SocStatusEventArgs(stateObj));
        }
    }
}

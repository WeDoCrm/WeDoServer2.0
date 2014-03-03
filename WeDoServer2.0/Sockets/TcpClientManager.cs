using System;
using System.Collections.Generic;
using System.Text;

namespace WeDoCommon.Sockets
{
    public class TcpClientManager
    {

        protected SyncSocClient mSocClient;
        protected StateObject stateObj;

        protected byte[] bufferTxt = new byte[SocConst.MAX_STR_BUFFER_SIZE];
        protected byte[] bufferBin = new byte[SocConst.MAX_BUFFER_SIZE];

        protected bool IsText = true;

        public event EventHandler<SocStatusEventArgs> MessageReceived;
        public event EventHandler<SocStatusEventArgs> SocStatusChanged;
        public event EventHandler<SocStatusEventArgs> ConnectionError;

        private System.Timers.Timer connCloseTimer;

        public TcpClientManager(string ipAddress, int port) : this(ipAddress, port, "")
        {
        }

        public TcpClientManager(string ipAddress, int port, string key, int timeout)
        {
            mSocClient = new SyncSocClient(ipAddress, port, timeout);
            mSocClient.SocStatusChanged += TcpClientStatusChanged;
            mSocClient.MessageReceived += ProcessOnMessageReceived;
            mSocClient.ConnectionError += ProcessOnConnectionError;
            mSocClient.SetKey(key);
            stateObj = new StateObject(mSocClient.getSocket());
            stateObj.Key = key;

            connCloseTimer = new System.Timers.Timer(10000);
            connCloseTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnConnStopTimerElapsed);
        }


        public TcpClientManager(string ipAddress, int port, string key) : this(ipAddress, port, key, SocConst.SOC_TIME_OUT_MIL_SEC)
        {
        }


        public void SetText()
        {
            this.IsText = true;
        }

        public void SetBinary()
        {
            this.IsText = false;
        }


        public bool IsConnected()
        {
            return mSocClient.IsConnected();
        }

        /// <summary>
        /// 1.접속
        /// 2.key값넘김
        /// 3.key값리턴받은후 확인
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            bool result = false;
            if (SocCode.SOC_ERR_CODE != mSocClient.Connect())
            {
                string msg = string.Format(MsgDef.MSG_KEY_INFO_FMT, MsgDef.MSG_KEY_INFO, this.stateObj.Key);
                if (SocCode.SOC_ERR_CODE != mSocClient.Send(msg))
                {
                    string returnMsg = mSocClient.ReadLine();
                    result = (msg.Equals(returnMsg));
                }
            }
            return result;
        }

        public bool Send(string msg)
        {
            return (SocCode.SOC_ERR_CODE != mSocClient.Send(msg));
        }

        public void Close()
        {
            if (Send(MsgDef.MSG_BYE))
                connCloseTimer.Start();
            else 
                ForceClose();
            
        }

        private void OnConnStopTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ForceClose();
            connCloseTimer.Stop();
        }

        public void ForceClose()
        {
            mSocClient.Close();
            stateObj.Status = SocHandlerStatus.DISCONNECTED;
            OnSocStatusChangedOnInfo(new SocStatusEventArgs(stateObj));
            connCloseTimer.Stop();
        }

        public void CloseOnCloseMsgReceived()
        {
            ForceClose();
        }

        #region OnSocStatusChanged
        public virtual void OnSocStatusChanged(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = SocStatusChanged;

            if (handler != null)
            {
                handler(this, e);
            }
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
        #endregion

        public void ReceiveMessage()
        {
            mSocClient.ReceiveMessage();
        }

        public virtual void OnMessageReceived(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = MessageReceived;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void ProcessOnMessageReceived(object sender, SocStatusEventArgs e)
        {
            OnMessageReceived(e);
            switch (e.Status.Cmd)
            {
                case MsgDef.MSG_BYE:
                    CloseOnCloseMsgReceived();
                    break;
            }
        }

        protected virtual void ProcessOnConnectionError(object sender, SocStatusEventArgs e)
        {
            ForceClose();
            OnConnectionError(e);
        }

        public void OnConnectionError(SocStatusEventArgs e)
        {
            EventHandler<SocStatusEventArgs> handler = ConnectionError;
            if (handler != null)
                handler(this, e);
        }
    }
}

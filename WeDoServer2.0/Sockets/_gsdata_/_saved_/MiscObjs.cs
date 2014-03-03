using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WeDoCommon.Sockets
{
    public class StateObject
    {

        public StateObject()
        {
        }

        public StateObject(Socket soc)
        {
            this.Soc = soc;
        }

        public StateObject(Exception e)
        {
            status = SocHandlerStatus.ERROR;
            this.exception = e;
        }

        public StateObject(Socket soc, string msg)
        {
            this.Soc = soc;
            this.Data = msg;
        }

        public StateObject(string msg)
        {
            this.Data = msg;
        }

        // Client socket.
        public Socket Soc { get; set; }
        // Size of receive buffer.
        public int BufferSize { get; set; } 
        // Receive buffer.
        public byte[] Buffer { get; set; }// = new byte[SocConst.MAX_BUFFER_SIZE];
        // Received data string.
        //public StringBuilder data = new StringBuilder();
        public string Data { get; set; }

        public string Key { get; set; }

        string cmd;
        public string Cmd { get { return Utils.getCmd(Data); } set { cmd = value; } }

        public string SocMessage { get; set; }
        public string SocErrorMessage { get; set; }
        private SocHandlerStatus status = SocHandlerStatus.UNINIT;
        public SocHandlerStatus Status { get { return status; } set { status = value; } }
        public Exception exception = null;

        private MSGStatus msgStatus = MSGStatus.NONE;
        public MSGStatus MsgStatus { get { return msgStatus; } set { msgStatus = value; } }
        private FTPStatus ftpStatus = FTPStatus.NONE;
        public FTPStatus FtpStatus { get { return ftpStatus; } set { ftpStatus = value; } }

        private bool abortFTP = false;
        public bool AbortFTP { get { return abortFTP; } set { abortFTP = value; } }

        public int FtpPort { get; set; }

        private long fileSizeDone;
        public long FileSizeDone { get { return fileSizeDone; } set { fileSizeDone = value; } }

        string fileName;
        public string FileName { get { return fileName; } set { fileName = value; } }

        private string tempFileName;
        public string TempFileName { get { return tempFileName; } set { tempFileName = value; } }

        private string fullFileName;
        public string FullFileName { get { return fullFileName; } set { fullFileName = value; } }

        long fileSize;
        public long FileSize { get { return fileSize; } set { fileSize = value; } }

        public StateObject Clone()
        {
            return (StateObject)this.MemberwiseClone();
        }
    }

    public class FTPSendObj
    {
        public FTPSendObj(IPEndPoint ie, string key, string fileName, long fileSize, string receiverId)
        {
            RemoteEndPoint = ie;
            Key = key;
            FileName = fileName;
            FileSize = fileSize;
            ReceiverId = receiverId;
        }
        public IPEndPoint RemoteEndPoint { get; set; }
        public string Key { get; set; }
        public string FileName { get; set; }
        public string ReceiverId { get; set; }
        public long FileSize { get; set; }
    }

    public class FTPRcvObj
    {
        public FTPRcvObj(IPEndPoint ie, string key, string fileName, long fileSize, string senderId)
        {
            RemoteEndPoint = ie;
            Key = key; 
            FileName = fileName;
            FileSize = fileSize;
            SenderId = senderId;
        }

        public FTPRcvObj(FTPRcvObj obj, string receiverId)
        {
            RemoteEndPoint = obj.RemoteEndPoint;
            Key = obj.Key;
            FileName = obj.FileName;
            FileSize = obj.FileSize;
            SenderId = obj.SenderId;
            ReceiverId = receiverId;

        }

        public IPEndPoint RemoteEndPoint { get; set; }
        public string Key { get; set; }
        public string FileName { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public long FileSize { get; set; }
    }

    public class WeDoFTPCancelException : Exception
    {
        public WeDoFTPCancelException()
            : base()
        {
        }
        public WeDoFTPCancelException(string message)
            : base(message)
        {
        }

    }

    //이벤트 전달할 소켓상태 정보
    public class SocStatusEventArgs : EventArgs
    {
        private StateObject status;

        public SocStatusEventArgs(StateObject s)
        {
            status = s;
        }

        public StateObject Status { get { return status; } }
    }

    //이벤트 전달할 소켓상태 정보
    public class SocFTPInfoEventArgs<T> : EventArgs
    {
        private T getObj;

        public SocFTPInfoEventArgs(T getObj)
        {
            this.getObj = getObj;
        }

        public T GetObj { get { return getObj; } }
    }

    
    //이벤트 전달할 소켓상태 정보
    public class FTPStatusEventArgs : EventArgs
    {
        private StateObject status;
        private string receiverId;
        private string msg;
        private string printMsg;
        private int progressIndex;

        public FTPStatusEventArgs(StateObject s, string receiverId)
        {
            status = s;
            this.receiverId = receiverId;
        }

        public FTPStatusEventArgs(FTPStatusEventArgs e, string msg, string printMsg, int index)
        {
            status = e.Status;
            this.receiverId = e.ReeceiverId;
            this.msg = msg;
            this.printMsg = printMsg;
            this.progressIndex = index;
        }

        public FTPStatusEventArgs(SocStatusEventArgs e, string msg, string printMsg, int index)
        {
            status = e.Status;
            this.msg = msg;
            this.printMsg = printMsg;
            this.progressIndex = index;
        }

        public FTPStatusEventArgs(FTPStatusEventArgs e, string msg, string printMsg)
        {
            status = e.Status;
            this.receiverId = e.ReeceiverId;
            this.msg = msg;
            this.printMsg = printMsg;
        }

        public FTPStatusEventArgs(SocStatusEventArgs e, string msg, string printMsg)
        {
            status = e.Status;
            this.msg = msg;
            this.printMsg = printMsg;
        }
        
        public StateObject Status { get { return status; } }
        public string ReeceiverId { get { return receiverId; } }
        public string Msg { get { return msg; } }
        public string PrintMsg { get { return printMsg; } }
        public int ProgressIndex { get { return progressIndex; } }
    }

}

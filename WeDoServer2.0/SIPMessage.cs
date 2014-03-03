using System;
using System.Collections.Generic;
using System.Text;

namespace WDMsgServer
{
    class SIPMessage
    {
        string Status_Code = "Unknown";
        string Method_Name = "Unknown";
        string Call_ID = "Unknown";
        string Cseq = "Unknown";
        string FromDN = "Unknown";
        string ToDN = "Unknown";
        string User_Agent = "Unknown";
        string Session_Name = "Unknown";

        public void setSIPMessage(string code, string method, string callid, string cseq, string from, string to, string agent, string sName)
        {
            Status_Code = code;
            Method_Name = method;
            Call_ID = callid;
            Cseq = cseq;
            FromDN = from;
            ToDN = to;
            User_Agent = agent;
            Session_Name = sName;
        }

        public string code
        {
            get { return Status_Code; }
        }

        public string method
        {
            get { return Method_Name; }
        }

        public string callid
        {
            get { return Call_ID; }
        }

        public string cseq
        {
            get { return Cseq; }
        }

        public string from
        {
            get { return FromDN; }
        }

        public string to
        {
            get { return ToDN; }
        }

        public string agent
        {
            get { return User_Agent; }
        }

        public string sName
        {
            get { return Session_Name; }
        }
    }
}

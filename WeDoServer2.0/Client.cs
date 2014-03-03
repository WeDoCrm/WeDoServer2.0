using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections;

namespace WDMsgServer
{
    public class Client
    {
        private string id = null;
        private string name = null;
        private string team = null;
        private string passwd = null;
        private string position = null;
        private string com_code = null;
        private string company = null;
        

        public Client(string id, string name, string team, string extension, string com_nm, string com_cd)
        {
            this.id = id;
            this.name = name;
            this.team = team;
            this.position = extension;
            this.company = com_nm;
            this.com_code = com_cd;
           
        }
        
        public string getId()
        {
            return id;
        }

        public string getName()
        {
            return name;
        }

        public string getTeam()
        {
            return team;
        }

        public string getPasswd()
        {
            return passwd;
        }

        public string getPosition()
        {
            return position;
        }

        public void setPosition(string ext)
        {
            position = ext;
        }

        public string getCompany()
        {
            return company;
        }

        public string getComCode()
        {
            return com_code;
        }
    }
}

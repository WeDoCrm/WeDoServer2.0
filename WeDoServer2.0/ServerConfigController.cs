using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeDoCommon;
using Microsoft.Win32;
using System.Windows.Forms;

namespace WDMsgServer
{
    public class ServerConfigController
    {
        private ConfigFileHandler configFileHandler = null;
        public event EventHandler<StringEventArgs> LogWriteHandler;

        public ServerConfigController()
        {
            configFileHandler = new ConfigFileHandler(ConstDef.WEDO_SERVER_DIR, ConstDef.WEDO_SERVER_EXE);
        }

        public bool LoadData()
        {
            bool result = false;
            try
            {
                crmPort = Convert.ToInt16(configFileHandler.GetValue("CRM_PORT"));
                OnWriteLog(string.Format("CRM_PORT    => [{0}] 초기화", crmPort));
                msgrPort = Convert.ToInt16(configFileHandler.GetValue("MSGR_PORT"));
                OnWriteLog(string.Format("MSGR_PORT    => [{0}] 초기화", msgrPort));

                dbServerIp = configFileHandler.GetValue("DB_HOST");
                OnWriteLog(string.Format("DB_HOST        => [{0}] 초기화", dbServerIp));
                dbPort = Convert.ToInt16(configFileHandler.GetValue("DB_PORT"));
                OnWriteLog(string.Format("DB_PORT        => [{0}] 초기화", dbPort));

                companyCode = configFileHandler.GetValue("COM_CODE");
                OnWriteLog(string.Format("COM_CODE       => [{0}] 초기화", companyCode));
                serverType = configFileHandler.GetValue("SVR_TYPE");
                OnWriteLog(string.Format("SVR_TYPE       => [{0}] 초기화", serverType));
                device = configFileHandler.GetValue("DEVICE");
                OnWriteLog(string.Format("DEVICE         => [{0}] 초기화", device));
                autoStart = (configFileHandler.GetValue("AUTO_START").Equals(ConstDef.TRUE));
                OnWriteLog(string.Format("AUTO_START     => [{0}] 초기화", autoStart));
                result = true;
            }
            catch (Exception ex)
            {
                Logger.error(ex.ToString());
            }
            return result;
        }

        //private string serverHost;
        private int crmPort;
        private int msgrPort;

        private string dbServerIp = "";
        private int dbPort;
        private string dbName = ConstDef.WEDO_DB;
        private string dbUser = ConstDef.WEDO_DB_USER;
        private string dbPasswd = ConstDef.WEDO_DB_PASSWORD;
        private string companyCode = "";
        private string companyName = "";
        private string serverType = "";
        private string device = "";
        private bool autoStart;

            public int CrmPort
        {
            get { return crmPort; }
            set
            {
                crmPort = value;
                configFileHandler.SetValue("CRM_PORT", Convert.ToString(value));
            }
        }

        public int MsgrPort
        {
            get { return msgrPort; }
            set
            {
                msgrPort = value;
                configFileHandler.SetValue("MSGR_PORT", Convert.ToString(value));
            }
        }

        public string DbServerIp
        {
            get { return dbServerIp; }
            set
            {
                dbServerIp = value;
                configFileHandler.SetValue("DB_HOST", value);
            }
        }
        //dbServerIp = configFileHandler.GetValue("DB_HOST");
        public int DbPort
        {
            get { return dbPort; }
            set
            {
                dbPort = value;
                configFileHandler.SetValue("DB_PORT", Convert.ToString(value));
            }
        }
        //dbPort = Convert.ToInt16(configFileHandler.GetValue("DB_PORT"));


        public string DbName
        {
            get { return dbName; }
        }

        public string DbUser
        {
            get { return dbUser; }
        }

        public string DbPasswd
        {
            get { return dbPasswd; }
        }

        public string CompanyCode
        {
            get { return companyCode; }
            set
            {
                companyCode = value;
                configFileHandler.SetValue("COM_CODE", value);
            }
        }

        public string CompanyName
        {
            get { return companyName; }
            set { companyName = value; }
        }
        //companyCode = configFileHandler.GetValue("COM_CODE");
        public string ServerType
        {
            get { return serverType; }
            set
            {
                serverType = value;
                configFileHandler.SetValue("SVR_TYPE", value);
            }
        }
        //serverType = configFileHandler.GetValue("SVR_TYPE");
        public string Device
        {
            get { return device; }
            set
            {
                device = value;
                configFileHandler.SetValue("DEVICE", value);
            }
        }
        //device = configFileHandler.GetValue("DEVICE");

        public bool AutoStart
        {
            get { return autoStart; }
            set
            {
                autoStart = value;
                configFileHandler.SetValue("AUTO_START", (value ? ConstDef.TRUE : ConstDef.FALSE));

                if (value)
                {
                    RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rkApp.SetValue(ConstDef.REG_APP_NAME, Application.ExecutablePath.ToString(), RegistryValueKind.String);
                    rkApp.Close();
                }
                else
                {
                    RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rkApp.GetValue(ConstDef.REG_APP_NAME) != null)
                    {
                        rkApp.DeleteValue(ConstDef.REG_APP_NAME);
                    }
                    rkApp.Close();
                }

            }
        }

        public virtual void OnWriteLog(string msg)
        {
            EventHandler<StringEventArgs> handler = this.LogWriteHandler;
            if (this.LogWriteHandler != null)
                handler(this, new StringEventArgs(msg));
        }
    }
}

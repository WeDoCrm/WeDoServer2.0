using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.IO;
using System.Diagnostics;
using WeDoCommon.Sockets;

namespace WeDoCommon
{
    public partial class Logger
    {
        public static void debug(StateObject arg)
        {
            if (level >= LOGLEVEL.DEBUG)
                Logger.debug(string.Format("[{0}]{1}", arg.Key, arg.SocMessage));
        }
        public static void info(StateObject arg)
        {
            if (level >= LOGLEVEL.INFO)
                Logger.info(string.Format("[{0}]{1}", arg.Key, arg.SocMessage));
        }
        public static void error(StateObject arg)
        {
            if (level >= LOGLEVEL.ERROR)
                Logger.error(string.Format("[{0}]{1}", arg.Key, arg.SocMessage));
        }
    }
}

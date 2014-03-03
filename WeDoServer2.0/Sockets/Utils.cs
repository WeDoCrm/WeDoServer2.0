using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace WeDoCommon.Sockets
{
    public class SocUtils
    {
        public static byte[] GetBytesFromFile(string fullFilePath)
        {
            // this method is limited to 2^32 byte files (4.2 GB)

            FileStream fs = File.OpenRead(fullFilePath);
            try
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                return bytes;
            }
            finally
            {
                fs.Close();
            }
        }

        public static string GetFileName(string path)
        {
            string[] token = path.Split('\\');
            if (token.Length == 1) return path;
            return token[token.Length - 1];
        }

        public static string GetPath(string path)
        {
            if (path.LastIndexOf('\\') < 0) return path;
            return path.Substring(0, path.LastIndexOf('\\'));
        }


        public static string GetIndexedFileName(string fileName, int index)
        {
            string shortFileName = fileName.Substring(0, fileName.LastIndexOf('.'));
            string extension = fileName.Substring(fileName.LastIndexOf('.') + 1);
            if (index == 0)
                return fileName;
            else
                return string.Format("{0}({1}).{2}", shortFileName, index, extension);
        }

        public static string GetValidFileName(string path, string fileName, int index)
        {
            string fileRename = GetIndexedFileName(fileName, index);
            FileInfo fInfo = new FileInfo(string.Format("{0}\\{1}", path, fileRename));
            string fullFileRename = fInfo.FullName;

            string[] files = Directory.GetFiles(path, fileRename, SearchOption.TopDirectoryOnly);

            bool fileExists = false;
            foreach (string file in files)
            {
                FileInfo itemInfo = new FileInfo(file);
                fileExists = (itemInfo.FullName == fullFileRename);
            }

            if (fileExists)
            {
                return GetValidFileName(path, fileName, ++index);
            }
            else
            {
                return fullFileRename;
            }
        }

        public static string getCmd(string msg)
        {
            string[] udata = null;
            string cmd;
            try
            {
                msg = msg.Trim();
                if (msg == null)
                    return "";
                if (msg.IndexOf(SocConst.TOKEN) < 0)
                    return msg;
                udata = msg.Split(SocConst.TOKEN);
                cmd = udata[0];
            }
            catch (Exception ex)
            {
                Logger.error("getCmd() Exception : " + ex.ToString());
                cmd = "";
            }
            return cmd;
        }

        public static string getIpAddress(string ipStr1, string ipStr2, string ipStr3, string ipStr4)
        {
            string ipAddress;
            try
            {
                ipAddress = string.Format("{0}.{1}.{2}.{3}",
                    Int64.Parse(ipStr1),
                    Int64.Parse(ipStr2),
                    Int64.Parse(ipStr3),
                    Int64.Parse(ipStr4));

            }
            catch (Exception e)
            {
                Logger.error("getIpAddress처리오류: " + e.ToString());
                return "";
            }
            return ipAddress;
        }

        public static void showMsgBox(IWin32Window win, string msg, string title) {
            MessageBox.Show(win, msg, title,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.RtlReading);
        }

        /// <summary>
        /// 파일사이즈정보 메시지포맷: "|2452|"
        /// </summary>
        public static byte[] ConvertFileSizeToByteArray(Int32 fileSize)
        {
            byte[] buffer = new byte[6];
            byte[] bDelim = Encoding.UTF8.GetBytes("|");
            byte[] bSrc = BitConverter.GetBytes(fileSize);
            Buffer.BlockCopy(bDelim, 0, buffer, 0, bDelim.Length);
            Buffer.BlockCopy(bSrc, 0, buffer, bDelim.Length, bSrc.Length);
            Buffer.BlockCopy(bDelim, 0, buffer, bSrc.Length + bDelim.Length, bDelim.Length);
            return buffer;
        }

        const string DELIM = "|";

        /// <summary>
        /// 파일사이즈정보 메시지포맷: "|2452|"
        /// </summary>
        /// <param name="b">파일사이즈 정보 메시지배열</param>
        /// <returns>파일사이즈</returns>
        public static Int32 ConvertByteArrayToFileSize(byte[] b)
        {
            string sDelim = Encoding.UTF8.GetString(b, 0, 1);
            if (sDelim != DELIM) return 0;
            sDelim = Encoding.UTF8.GetString(b, 5, 1);
            if (sDelim != DELIM) return 0;
            return BitConverter.ToInt32(b, 1);
        }

        /// <summary>
        /// text메시지를 text길이정보를 포함하여 byte변형함.
        /// "MSG|...."
        /// => "TEXT[msg length]...."(byte배열행태)
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetPrefixInfo(string msg)
        {
            byte[] buffer = new byte[4];
            byte[] tempBuf = Encoding.UTF8.GetBytes(msg);
            buffer = new byte[tempBuf.Length + 8];
            Encoding.UTF8.GetBytes("TEXT").CopyTo(buffer, 0);
            BitConverter.GetBytes(tempBuf.Length).CopyTo(buffer, 4);
            tempBuf.CopyTo(buffer, 8);
            return buffer;
        }

        /// <summary>
        /// 연속으로 붙은 text길이정보를 포함된 byte배열을 text메시지로 변형.
        /// "TEXT[msg length]...."(byte배열행태)
        /// ==>"MSG|...."
        /// </summary>
        /// <param name="buff">처리한 배열은 제거하여 리턴</param>
        /// <returns></returns>
        public static string GetMsgByPrefixLengthInfo(ref byte[] buff, int buffSize, ref int msgBufSize)//, out byte[] resultBuf )
        {
            byte[] bufPrefix = new byte[4];
            byte[] bufLength = new byte[4];
            byte[] bufMsg = new byte[1024 * 16];
            byte[] resultBuf = new byte[1024 * 16];
            int msgSize;
            string resultMsg;
            Array.Copy(buff, 0, bufPrefix, 0, 4);
            Array.Copy(buff, 4, bufLength, 0, 4);
            if (Encoding.UTF8.GetString(bufPrefix).Equals("TEXT"))
            {
                msgSize = BitConverter.ToInt32(bufLength, 0);
                Array.Copy(buff, 8, bufMsg, 0, msgSize);
                resultMsg = Encoding.UTF8.GetString(bufMsg, 0, msgSize);
                Array.Copy(buff, msgSize + 8, resultBuf, 0, (buff.Length - (msgSize + 8)));
                buff = resultBuf;
                msgBufSize = msgSize;
            }
            else  //잘려서 넘어온 경우
            {
                resultMsg = null;// Encoding.UTF8.GetString(buff, 0, buffSize);
                msgBufSize = -1;// buffSize;
            }
            return resultMsg;
        }

        //public static IPEndPoint Parse(string endpointstring)
        //{
        //    return Parse(endpointstring, -1);
        //}

        //public static IPEndPoint Parse(string endpointstring, int defaultport)
        //{
        //    if (string.IsNullOrEmpty(endpointstring)
        //        || endpointstring.Trim().Length == 0)
        //    {
        //        throw new ArgumentException("Endpoint descriptor may not be empty.");
        //    }

        //    if (defaultport != -1 &&
        //        (defaultport < IPEndPoint.MinPort
        //        || defaultport > IPEndPoint.MaxPort))
        //    {
        //        throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultport));
        //    }

        //    string[] values = endpointstring.Split(new char[] { ':' });
        //    IPAddress ipaddy;
        //    int port = -1;

        //    //check if we have an IPv6 or ports
        //    if (values.Length <= 2) // ipv4 or hostname
        //    {
        //        if (values.Length == 1)
        //            //no port is specified, default
        //            port = defaultport;
        //        else
        //            port = getPort(values[1]);

        //        //try to use the address as IPv4, otherwise get hostname
        //        if (!IPAddress.TryParse(values[0], out ipaddy))
        //            ipaddy = getIPfromHost(values[0]);
        //    }
        //    else if (values.Length > 2) //ipv6
        //    {
        //        //could [a:b:c]:d
        //        if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
        //        {
        //            string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
        //            ipaddy = IPAddress.Parse(ipaddressstring);
        //            port = getPort(values[values.Length - 1]);
        //        }
        //        else //[a:b:c] or a:b:c
        //        {
        //            ipaddy = IPAddress.Parse(endpointstring);
        //            port = defaultport;
        //        }
        //    }
        //    else
        //    {
        //        throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", endpointstring));
        //    }

        //    if (port == -1)
        //        throw new ArgumentException(string.Format("No port specified: '{0}'", endpointstring));

        //    return new IPEndPoint(ipaddy, port);
        //}

        private static int getPort(string p)
        {
            int port;

            if (!int.TryParse(p, out port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(string.Format("Invalid end point port '{0}'", p));
            }

            return port;
        }

        public static IPAddress getIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(string.Format("Host not found: {0}", p));

            return hosts[0];
        }

        public static string GenerateFTPClientKey(string senderId, string fileName, long fileSize, string receiverId)
        {
            return string.Format("{0}_{1}_{2}_{3}", senderId, EncodeMsg(fileName), fileSize, receiverId);
        }

        public static string EncodeMsg(string msg)
        {
            StringBuilder b = new StringBuilder(msg);
            b.Replace("_", "&UNS");
            return b.ToString();
        }

        //public static string GenerateFTPListenerKey(string senderId, string fileName, long fileSize)
        //{
        //    return string.Format("{0}_{1}_{2}", senderId, fileName, fileSize);
        //}
    }
}

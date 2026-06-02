using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace VCM.POSBLUE.API.Helpers
{
    public class VersionHelper
    {
        public static string GetIpServer()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return addr.LastOrDefault()?.ToString();
        }
        public static string GetDllModificationDate(string dllPath)
        {
            if (File.Exists(dllPath))
            {
                FileInfo fileInfo = new FileInfo(dllPath);
                return fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        public static DateTime GetModificationDateFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                FileInfo fileInfo = new FileInfo(fileName);
                return fileInfo.LastWriteTime;
            }
            else
            {
                return DateTime.Now.AddDays(-7);
            }
        }
    }
}

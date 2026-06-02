using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Shared
{
    public static class ComputerHelper
    {
        public static string GetIpServer()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return addr.LastOrDefault()?.ToString();
        }
        public static string GetHostName()
        {
            try
            {
                return System.Net.Dns.GetHostName() ?? "BLUEPOS";
            }
            catch
            {
                return "BLUEPOS";
            }
        }
    }
}

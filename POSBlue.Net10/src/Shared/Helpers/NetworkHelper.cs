using System.Net;
using System.Net.Sockets;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper mạng — lấy IP server (thay ComputerHelper.GetIpServer cũ).</summary>
public static class NetworkHelper
{
    public static string GetIpServer()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .LastOrDefault()?.ToString() ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }
}

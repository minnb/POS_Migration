using System.Diagnostics;
using System.Runtime.InteropServices;

namespace POS.Common.Helpers;

public static class HostHelper
{
    #region P/Invoke - GlobalMemoryStatusEx

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    #endregion

    public static int GetTotalCpuCores()
    {
        try
        {
            return Environment.ProcessorCount;
        }
        catch
        {
            return 0;
        }
    }

    public static long GetTotalRamMB()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
            if (GlobalMemoryStatusEx(ref memStatus))
                return (long)(memStatus.ullTotalPhys / (1024 * 1024));
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public static decimal GetMemoryUsageMB()
    {
        try
        {
            return (decimal)Math.Round(Process.GetCurrentProcess().WorkingSet64 / (1024f * 1024f), 0);
        }
        catch
        {
            return 0;
        }
    }

    public static decimal GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return (decimal)process.TotalProcessorTime.TotalSeconds;
        }
        catch
        {
            return 0;
        }
    }

    public static string GetDateFileDll(string dllPath)
    {
        if (File.Exists(dllPath))
        {
            FileInfo fileInfo = new FileInfo(dllPath);
            return fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string GetIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString();
            return ipAddress ?? GetServerName();
        }
        catch
        {
            return GetServerName();
        }
    }

    public static string GetServerName()
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

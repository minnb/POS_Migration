using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace POS.Infrastructure.Files;

/// <summary>
/// Kết nối UNC shared folder với credentials (net use tạm thời trong scope using).
/// Migrated từ VCM.POSBLUE.API.Models.ConnectToSharedFolderAPI — P/Invoke mpr.dll
/// chạy bình thường trên .NET 10 (Windows-only, app chỉ deploy Windows).
/// </summary>
public sealed class ConnectToSharedFolder : IDisposable
{
    private readonly string _networkName;

    public ConnectToSharedFolder(string networkName, NetworkCredential credentials)
    {
        _networkName = networkName;

        var netResource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = networkName
        };

        var userName = string.IsNullOrEmpty(credentials.Domain)
            ? credentials.UserName
            : $@"{credentials.Domain}\{credentials.UserName}";

        var result = WNetAddConnection2(netResource, credentials.Password, userName, 0);
        if (result != 0)
            throw new Win32Exception(result, "Error connecting to remote share");
    }

    ~ConnectToSharedFolder() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        WNetCancelConnection2(_networkName, 0, true);
    }

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource,
        string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    private sealed class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplaytype DisplayType;
        public int Usage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }

    private enum ResourceScope
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    }

    private enum ResourceType
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8
    }

    private enum ResourceDisplaytype
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        Shareadmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        Ndscontainer = 0x0b
    }
}

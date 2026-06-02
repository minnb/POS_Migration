using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Infrastructure.Integration;

/// <summary>
/// Stub implementation of the legacy TichHopSAP functionality.
/// Returns a successful "OK" result for WriteFileByManualAsync.
/// Replace with real DLL integration once the library is built for .NET.
/// </summary>
public class LegacySyncDataToPosClient : ISyncDataToPosClient
{
    public Task<string> WriteFileByManualAsync(string storeNo, string posTerminal, bool isUpdFtp, bool isFullSync)
    {
        // TODO: call the actual TichHopSAP DLL when available.
        // For now we simply simulate success.
        return Task.FromResult("OK");
    }
}

using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Wrapper interface cho module TichHopSAP (DLL legacy .NET Framework).
/// Implementation tạm thời là stub — sẽ được thay thế khi DLL được port sang .NET Standard.
/// </summary>
public interface ISyncDataToPosClient
{
    /// <summary>
    /// Ghi file dữ liệu POS cho một cửa hàng/quầy thu ngân.
    /// Tương đương SyncDataToPos.writeFileByManual() của hệ thống cũ.
    /// </summary>
    Task<string> WriteFileByManualAsync(string storeNo, string posTerminal, bool isUpdFtp, bool isFullSync);
}

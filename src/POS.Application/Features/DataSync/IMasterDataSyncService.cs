using POS.Common.Dtos.DataSync;

namespace POS.Application.Features.DataSync;

/// <summary>
/// Sinh file master data .zip cho máy POS (SP1 → SP2 stream → zip → publish atomic).
/// </summary>
public interface IMasterDataSyncService
{
    /// <summary>
    /// Đảm bảo file .zip master data của ngày hôm nay tồn tại trong TargetDir.
    /// Idempotent: nếu đã có file hợp lệ trong ngày → trả ngay; chưa có → khóa + sinh.
    /// Throw khi sinh lỗi (đã cleanup, KHÔNG publish file thiếu bảng).
    /// </summary>
    Task<GetMasterDataFileResult> EnsureMasterDataFileAsync(GetMasterDataFileRequest req, CancellationToken ct = default);

    /// <summary>
    /// Ghi log 1 lượt POS tải file (gọi sau khi stream xong). Fail-safe: KHÔNG throw — lỗi log được nuốt.
    /// <paramref name="status"/>: "Success" (gửi đủ byte) | "Aborted" (client ngắt) | "Error".
    /// </summary>
    Task LogDownloadAsync(
        string? fileName, string? filePath, long fileSizeBytes, long durationMs,
        string status, string? clientIp, CancellationToken ct = default);
}

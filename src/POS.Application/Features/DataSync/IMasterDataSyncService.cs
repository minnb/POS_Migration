using POS.Common.Dtos.DataSync;

namespace POS.Application.Features.DataSync;

/// <summary>
/// Sinh file master data .zip cho máy POS (SP1 → SP2 stream → zip → publish atomic).
/// </summary>
public interface IMasterDataSyncService
{
    /// <summary>
    /// Đảm bảo (các) file .zip master data của ngày hôm nay tồn tại trong TargetDir.
    /// Bảng có SyncTableList.IsSingleFile=1 → 1 zip riêng/bảng; các bảng còn lại → gom chung 1 zip "common".
    /// Trả 1 phần tử/zip đã publish (hoặc đã hợp lệ sẵn trong ngày).
    /// Idempotent: nếu TOÀN BỘ zip dự kiến đã hợp lệ trong ngày → trả ngay; thiếu bất kỳ zip nào → khóa + sinh lại toàn bộ.
    /// Throw khi sinh lỗi (đã cleanup, KHÔNG publish file thiếu bảng).
    /// </summary>
    Task<List<GetMasterDataFileResult>> EnsureMasterDataFileAsync(GetMasterDataFileRequest req, CancellationToken ct = default);

    /// <summary>
    /// Ghi log 1 lượt POS tải file (gọi sau khi stream xong). Fail-safe: KHÔNG throw — lỗi log được nuốt.
    /// <paramref name="status"/>: "Success" (gửi đủ byte) | "Aborted" (client ngắt) | "Error".
    /// </summary>
    Task LogDownloadAsync(
        string? fileName, string? filePath, long fileSizeBytes, long durationMs,
        string status, string? clientIp, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật dấu vết xóa file vào bản ghi MasterDataDownloadLog tương ứng (gọi sau khi
    /// DeleteFileFromFTP xử lý xong). Fail-safe: KHÔNG throw — lỗi log được nuốt.
    /// <paramref name="deleteStatus"/>: "Success" | "Failed".
    /// </summary>
    Task LogDeleteAsync(string? fileName, string deleteStatus, CancellationToken ct = default);
}

namespace POS.Common.Dtos.DataSync;

/// <summary>
/// Tham số sinh file master data .zip cho 1 máy POS (gom từ query của GetFileFromFTP).
/// </summary>
public sealed class GetMasterDataFileRequest
{
    public string SiteCode { get; set; } = string.Empty;
    public string PosTerminal { get; set; } = string.Empty;
    public string FolderFile { get; set; } = string.Empty;
    public string PathSync { get; set; } = string.Empty;
    public string TypeSync { get; set; } = string.Empty;

    /// <summary>Thư mục vật lý đích (đã map qua MapFtpPath: FtpRootPath/pathSync/folderFile).</summary>
    public string TargetDir { get; set; } = string.Empty;

    /// <summary>
    /// @IsChange truyền vào SP [SyncTable_Get]. "A" = ALL sync (mặc định) — Action lấy từ SP theo
    /// từng bảng, batch 1 dùng Action đó, batch sau luôn "INSERT" (append kỹ thuật). "W" = Web
    /// Sync/push 1 POS — Action lấy từ SP (nhánh W luôn trả "DELETE-INSERT"), áp dụng MỌI batch.
    /// </summary>
    public string IsChangeMode { get; set; } = "A";
}

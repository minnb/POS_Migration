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

    /// <summary>
    /// True → bỏ qua short-circuit "đã có zip hợp lệ hôm nay" trong EnsureMasterDataFileAsync, luôn
    /// generate. Dùng cho MasterDataZipGeneratorWorker (watermark-driven, không phụ thuộc theo ngày).
    /// Mặc định false — giữ nguyên hành vi hiện hữu cho GetFileFromFTP/PushStartOfDayDataAsync.
    /// </summary>
    public bool ForceRegenerate { get; set; }

    /// <summary>
    /// Nguồn kích hoạt sinh file — ghi vào dbo.MasterDataGenerationLog.TriggerSource để đối soát.
    /// "AutoChange" (MasterDataZipGeneratorWorker), "ManualSync" (nút Đồng bộ trên PosMapPage),
    /// "PosPull" (POS gọi GetFileFromFTP). Chỉ dùng nội bộ để log — KHÔNG ảnh hưởng contract JSON.
    /// </summary>
    public string? TriggerSource { get; set; }
}

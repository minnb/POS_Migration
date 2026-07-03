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
    /// Action ghi vào envelope cho MỌI batch (override khuôn mặc định TRUNC-INSERT→INSERT).
    /// null = mặc định (POS ALL: batch1 TRUNC-INSERT, batch sau INSERT). Web Sync đặt "DELETE-INSERT".
    /// </summary>
    public string? SyncAction { get; set; }
}

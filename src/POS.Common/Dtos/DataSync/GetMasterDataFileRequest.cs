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
}

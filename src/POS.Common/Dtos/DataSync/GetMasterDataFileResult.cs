namespace POS.Common.Dtos.DataSync;

/// <summary>
/// Kết quả nội bộ của MasterDataSyncService — dùng để log + quyết định luồng controller.
/// KHÔNG phải HTTP body: response của GetFileFromFTP vẫn là List&lt;PathFileAPIModel&gt; (giữ contract POS).
/// </summary>
public sealed class GetMasterDataFileResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? RelativePath { get; set; }
    public int TableCount { get; set; }
    public string? Message { get; set; }
}

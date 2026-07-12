namespace POS.Common.Dtos.DataSync;

/// <summary>
/// 1 dòng đọc từ bảng dbo.MasterDataGenerationLog (log mỗi lần sinh file master data .zip).
/// Dùng cho trang giám sát POS.Web /ops/masterdata-generation-log. Không phải HTTP contract POS.
/// </summary>
public sealed class MasterDataGenerationLogDto
{
    public long Id { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public int TableCount { get; set; }
    public long DurationMs { get; set; }
    public string? TriggerSource { get; set; }
    public string? IsChangeMode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? InstanceId { get; set; }
    public DateTime GeneratedAt { get; set; }
}

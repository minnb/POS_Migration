namespace POS.Common.Dtos.Ops.SpAudit;

public sealed class SpAuditSnapshotDto
{
    public DateTime RunStartedUtc { get; set; }
    public DateTime RunFinishedUtc { get; set; }
    public string[] DatabasesScanned { get; set; } = [];
    public int TotalProcedures { get; set; }
    public bool ProceduresTruncated { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SpInventoryItemDto> Items { get; set; } = [];
}

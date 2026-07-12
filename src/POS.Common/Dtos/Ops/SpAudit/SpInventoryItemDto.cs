using POS.Common.Enums;

namespace POS.Common.Dtos.Ops.SpAudit;

public sealed class SpInventoryItemDto
{
    public string Schema { get; set; } = "";
    public string ProcedureName { get; set; } = "";
    public string DatabaseKey { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public DateTime ModifyDate { get; set; }
    public int LineCount { get; set; }
    public long ExecutionCount { get; set; }
    public DateTime? LastExecutionAt { get; set; }
    public SpComplexity Complexity { get; set; }
    public bool IsCalledFromCode { get; set; }
    public SpRecommendation Recommendation { get; set; }
    public string Note { get; set; } = "";
}

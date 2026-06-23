namespace POS.Common.Dtos.RptCentralSale;

// Map RESULT SET 1 của sp_ReportTopProduct — KPI tổng hợp Top N.
public sealed class TopProductKpiDto
{
    public decimal TopN_TotalNetRevenue  { get; set; }
    public decimal TopN_TotalQty         { get; set; }
    public decimal TopN_RevenueSharePct  { get; set; }   // % DT của Top N trên tổng DT
    public int     TopN_SKUCount         { get; set; }
    public int     TopN_CategoryCount    { get; set; }
    public decimal Grand_TotalNetRevenue { get; set; }   // tổng DT toàn bộ SP có DT dương
    public int     FilterTopN            { get; set; }
    public string  FilterSortBy          { get; set; } = string.Empty;
}

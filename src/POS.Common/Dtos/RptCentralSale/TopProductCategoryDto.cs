namespace POS.Common.Dtos.RptCentralSale;

// Map RESULT SET 3 của sp_ReportTopProduct — tổng hợp theo ngành hàng (Treemap).
// Hiện chưa render trên page: SP trả CategoryNo/Name = NULL cho tới khi JOIN Item master.
public sealed class TopProductCategoryDto
{
    public string  CategoryNo         { get; set; } = string.Empty;
    public string  CategoryName       { get; set; } = string.Empty;
    public decimal CategoryNetRevenue { get; set; }
    public decimal CategoryNetQty     { get; set; }
    public int     CategorySKUCount   { get; set; }
    public decimal CategorySharePct   { get; set; }
}

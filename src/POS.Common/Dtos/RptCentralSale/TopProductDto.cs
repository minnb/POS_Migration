namespace POS.Common.Dtos.RptCentralSale;

// Map RESULT SET 2 của sp_ReportTopProduct — chi tiết từng SP trong Top N.
public sealed class TopProductDto
{
    public int     Rank               { get; set; }
    public string  ItemNo             { get; set; } = string.Empty;
    public string? Barcode            { get; set; }
    public string  ProductName        { get; set; } = string.Empty;
    public string? UnitOfMeasure      { get; set; }
    public string? CategoryNo         { get; set; }   // hiện NULL — chờ JOIN Item master
    public string? CategoryName       { get; set; }
    public decimal NetRevenue         { get; set; }
    public decimal GrossRevenue       { get; set; }
    public decimal TotalDiscount      { get; set; }
    public decimal NetQty             { get; set; }
    public decimal SoldQty            { get; set; }
    public decimal ReturnQty          { get; set; }
    public int     OrderCount         { get; set; }
    public decimal AvgSellingPrice    { get; set; }
    public decimal RevenueSharePct    { get; set; }
    public decimal CumulativeSharePct { get; set; }   // Pareto 80-20
    public int     RankByRevenue      { get; set; }
    public int     RankByQty          { get; set; }
}

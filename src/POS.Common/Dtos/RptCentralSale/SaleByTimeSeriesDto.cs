namespace POS.Common.Dtos.RptCentralSale;

public sealed class SaleByTimeSeriesDto
{
    public string  TimeLabel         { get; set; } = string.Empty;
    public int     SortOrder         { get; set; }
    public string? StoreNo           { get; set; }
    public decimal GrossRevenue      { get; set; }
    public decimal NetRevenue        { get; set; }
    public decimal TotalDiscount     { get; set; }
    public int     TotalOrders       { get; set; }
    public int     TotalReturnOrders { get; set; }
    public decimal TotalQty          { get; set; }
    public decimal ATV               { get; set; }
}

namespace POS.Common.Dtos.RptCentralSale;

// Drill-through: 1 dòng hóa đơn (order line) của 1 SP — query trực tiếp ReportSaleDetail.
public sealed class ProductOrderLineDto
{
    public string  OrderNo          { get; set; } = string.Empty;
    public DateTime OrderDate        { get; set; }
    public string? OrderTime        { get; set; }
    public string  StoreNo          { get; set; } = string.Empty;
    public string? POSTerminalNo    { get; set; }
    public string? CashierID        { get; set; }
    public decimal Quantity         { get; set; }
    public decimal UnitPrice        { get; set; }
    public decimal DiscountAmount   { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public bool    SalesIsReturn    { get; set; }
}

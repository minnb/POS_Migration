namespace POS.Common.Dtos.RptCentralSale;

public sealed class SaleByTimeKpiDto
{
    public decimal GrossRevenue      { get; set; }
    public decimal NetRevenue        { get; set; }
    public decimal TotalDiscount     { get; set; }
    public decimal TotalVAT          { get; set; }
    public int     TotalOrders       { get; set; }
    public int     TotalReturnOrders { get; set; }
    public decimal ATV               { get; set; }
    public decimal ReturnRate        { get; set; }
    public int     StoreCount        { get; set; }
}

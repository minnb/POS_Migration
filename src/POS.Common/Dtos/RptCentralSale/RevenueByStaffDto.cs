namespace POS.Common.Dtos.RptCentralSale;

public sealed class RevenueByStaffDto
{
    public int      ID              { get; set; }
    public string?  StaffCode       { get; set; }
    public string?  FullName        { get; set; }
    public decimal? AmountTotal     { get; set; }
    public int?     CountOrderTotal { get; set; }
}

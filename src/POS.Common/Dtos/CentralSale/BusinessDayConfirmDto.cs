namespace POS.Common.Dtos.CentralSale;

public sealed class BusinessDayConfirmDto
{
    public string StoreNo { get; set; } = string.Empty;
    public DateTime BusinessDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalShifts { get; set; }
    public string ConfirmedBy { get; set; } = string.Empty;
    public DateTime ConfirmedDate { get; set; }
}

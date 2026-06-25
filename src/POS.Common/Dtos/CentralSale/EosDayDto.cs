namespace POS.Common.Dtos.CentralSale;

public sealed class EosDayDto
{
    public string StoreNo { get; set; } = string.Empty;
    public DateTime BussinessDate { get; set; }
    public int TotalShifts { get; set; }
    public int ClosedShifts { get; set; }
    public int OpenShifts { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalTienHeThong { get; set; }
    public decimal TotalTienMat { get; set; }
    public decimal TotalChenLech { get; set; }
}

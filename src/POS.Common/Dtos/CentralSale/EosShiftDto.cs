namespace POS.Common.Dtos.CentralSale;

public sealed class EosShiftDto
{
    public string StoreNo { get; set; } = string.Empty;
    public string PosTerminal { get; set; } = string.Empty;
    public DateTime BussinessDate { get; set; }
    public int ShiftNumber { get; set; }
    public decimal BeginAmount { get; set; }
    public decimal TienMat { get; set; }
    public decimal TienHeThong { get; set; }
    public decimal ChenLech { get; set; }
    public string? StaffCode { get; set; }
    public DateTime? OpenShiftDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? CloseShiftDate { get; set; }
    public bool IsShiftClosed { get; set; }
}

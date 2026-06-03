namespace POS.Application.Common.DTOs;

public class CheckTotalBillResponse
{
    public bool Status { get; set; }
    public string? Description { get; set; }
    public int TotalBillPOS { get; set; }
    public int TotalBillCentral { get; set; }
}

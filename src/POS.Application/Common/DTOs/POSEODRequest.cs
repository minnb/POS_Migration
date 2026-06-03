namespace POS.Application.Common.DTOs;

public class POSEODRequest
{
    public string? POSTerminal { get; set; }
    public string? StoreNo { get; set; }
    public DateTime BussinessDate { get; set; }
    public int TotalSale { get; set; }
}

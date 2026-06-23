using System;

namespace POS.Common.Dtos.RptCentralSale;

public sealed class SalesByCategoryDto
{
    public string StoreNo { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string MCHCode { get; set; } = string.Empty;
    public string MCHName { get; set; } = string.Empty;
    public double AmountTotal { get; set; }
    public int OrderTotal { get; set; }
    public DateTime BussinessDate { get; set; }
}

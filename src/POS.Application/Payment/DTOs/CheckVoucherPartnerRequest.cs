using System.Collections.Generic;

namespace POS.Application.Payment.DTOs;

public class CheckVoucherPartnerRequest
{
    public string Partner { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    public string PosNo { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> SerialNo { get; set; } = new();
    public List<SkuApplyVoucherPartner>? Items { get; set; }
}

public class SkuApplyVoucherPartner
{
    public int LineNo { get; set; }
    public string ItemNo { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Qty { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmount { get; set; }
}

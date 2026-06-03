namespace POS.Application.Payment.DTOs;

public class UpdateStatusVoucherPartnerRequest
{
    public string Partner { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    public string PosNo { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public List<LstVoucherPartner> SerialNo { get; set; } = new();
    public List<SkuApplyVoucherPartner>? Items { get; set; }
}

public class LstVoucherPartner
{
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

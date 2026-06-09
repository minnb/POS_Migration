using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.PartnerApi;

public class CheckVoucherPartnerPOSRequest
{
    [Required]
    public string Partner { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    [Required]
    public List<string> SerialNo { get; set; } = [];
    public List<SkuApplyVoucherPartner>? Items { get; set; }
}

public class SkuApplyVoucherPartner
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? Barcode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Qty { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmount { get; set; }
}

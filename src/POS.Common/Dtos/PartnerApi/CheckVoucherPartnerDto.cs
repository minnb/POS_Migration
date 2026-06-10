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

public class UpdateStatusVoucherPartnerRequest
{
    [Required]
    public string Partner { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public List<LstVoucherPartner>? SerialNo { get; set; }
    public List<SkuApplyVoucherPartner>? Items { get; set; }
}

public class LstVoucherPartner
{
    [Required]
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class DataVoucherPartnerResponse
{
    public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int VoucherAmount { get; set; }
    public int Amount { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public bool IsApplySku { get; set; }
}

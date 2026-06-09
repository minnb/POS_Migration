using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Coupon;

public class CheckCouponRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string Partner { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    [Required]
    public string[] SerialNo { get; set; } = [];
}

public class ValidateCouponDataResponse
{
    public string? Mobile { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountApply { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public string? ValidTillDate { get; set; }
    public bool IsRedeemable { get; set; }
    public bool IsApplySku { get; set; }
    public List<ItemApplyCoupon>? Items { get; set; }
}

public class ItemApplyCoupon
{
    public string? ItemNo { get; set; }
    public string? Barcode { get; set; }
    public string? ItemName { get; set; }
    public string? ExpiryDate { get; set; }
    public string? SellDate { get; set; }
}

public class RedeemCouponRequestPOS
{
    public List<SerialCouponModel>? ListSeriNo { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? OrderNo { get; set; }
    public string? UserPOS { get; set; }
}

public class SerialCouponModel
{
    public string? SeriNo { get; set; }
    public long QuantityRedeem { get; set; }
    public string? Status { get; set; }
}

public class ReActiveCouponRequest
{
    [Required]
    public string Partner { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public List<string> SerialNo { get; set; } = [];
    [Required]
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

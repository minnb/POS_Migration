namespace POS.Application.Payment.DTOs;

public class ValidateCouponDataResponse
{
    public string Mobile { get; set; } = string.Empty;
    public string DiscountCode { get; set; } = string.Empty;
    public decimal DiscountApply { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public string ValidTillDate { get; set; } = string.Empty;
    public bool IsRedeemable { get; set; }
    public bool IsApplySku { get; set; }
    public List<ItemApplyCoupon> Items { get; set; } = new();
}

public class ItemApplyCoupon
{
    public string ItemNo { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string SellDate { get; set; } = string.Empty;
}

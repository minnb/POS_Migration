namespace POS.Common.Dtos.Capillary.Vouchers;

public class ValidateVoucherCapillaryResponse
{
    public List<VouchersCapillaryResponse>? Vouchers { get; set; }
    public VoucherRecommend? Recommends { get; set; }
    public List<object>? Warnings { get; set; }
}

public class VoucherRecommend
{
    public decimal Total_discount { get; set; }
    public decimal Coupon_discount { get; set; }
    public string? Discount_coupons { get; set; }
    public List<string>? Vouchers_recommend { get; set; }
    public List<LineItemRecommendVouchers>? LineItems { get; set; }
}

public class LineItemRecommendVouchers
{
    public string? ItemCode { get; set; }
    public int Pos_line_no { get; set; }
    public int Coupon_discount { get; set; }
    public string? Discount_description { get; set; }
}

public class CustomerCappillary
{
    public long Id { get; set; }
    public long Mobile { get; set; }
}

public class ValidateVoucherCapillary
{
    public List<VouchersCapillary>? Vouchers { get; set; }
    public decimal BillAmount { get; set; }
    public List<LineItemVouchersCapillary>? LineItems { get; set; }
}

public class LineItemVouchersCapillary
{
    public string? ItemCode { get; set; }
    public decimal Amount { get; set; }
    public int Pos_line_no { get; set; }
}

public class VouchersCapillary
{
    public string? Code { get; set; }
}

public class VouchersCapillaryResponse : VouchersCapillary
{
    public bool IsRedeemable { get; set; }
    public string? Description { get; set; }
    public decimal DiscountValue { get; set; }
    public int StatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public List<LineItemVouchersCapillary>? LineItems { get; set; }
    public List<object>? Warnings { get; set; }
    public string? AppendedErrorMessage { get; set; }
}

public class VouchersCapillaryResponseError
{
    public List<VouchersCapillaryResponse>? Errors { get; set; }
}

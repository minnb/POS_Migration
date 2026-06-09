using System.ComponentModel.DataAnnotations;
using POS.Common.Dtos.Capillary;
using POS.Common.Dtos.Capillary.Redemption;
using POS.Common.Dtos.Capillary.Transaction;

namespace POS.Common.Dtos.Capillary.Coupons;

public class CheckActiveCouponResponseCap
{
    public DataCheckActiveCouponResponseCap? Response { get; set; }
}

public class DataCheckActiveCouponResponseCap
{
    public PaginationResponseCap? Pagination { get; set; }
    public StatusCodeCouponCapillary? Status { get; set; }
    public CustomersResponseInCoupon? Customers { get; set; }
}

public class CustomersResponseInCoupon
{
    public List<DataCustomersResponseInCoupon>? Customer { get; set; }
}

public class DataCustomersResponseInCoupon
{
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Email { get; set; }
    public string? External_id { get; set; }
    public string? Mobile { get; set; }
    public CouponsCapillary? Coupons { get; set; }
    public ItemStatusCapillary? Item_status { get; set; }
}

public class PaginationResponseCap
{
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public int Total { get; set; }
}

public class CapillaryCouponDetailResponse
{
    public CapillaryCouponDetailResponseData? Response { get; set; }
}

public class CapillaryCouponDetailResponseData
{
    public CapillaryCouponDetailData? Coupons { get; set; }
    public RedemptionStatus? Status { get; set; }
    public List<object>? Warnings { get; set; }
}

public class CapillaryCouponDetailData
{
    public List<CapillaryCouponDetail>? Coupon { get; set; }
}

public class CapillaryCouponDetail
{
    public string? Id { get; set; }
    public string? Code { get; set; }
    public string? Valid_till { get; set; }
    public string? Issued_on { get; set; }
    public string? Valid_from { get; set; }
    public string? Purpose { get; set; }
    public long Series_id { get; set; }
    public bool Is_absolute { get; set; }
    public int Value { get; set; }
    public int Discount_upto { get; set; }
    public CouponCustomer? Customer { get; set; }
    public ItemStatusCoupon? Item_status { get; set; }
    public Redemption_info? Redemption_info { get; set; }
    public Coupon_custom_properties? Custom_properties { get; set; }
}

public class Redemption_info
{
    public bool Redeemed { get; set; }
    public long Id { get; set; }
    public string? Redeemed_on { get; set; }
    public string? Redeemed_at { get; set; }
    public Redeemed_by? Redeemed_by { get; set; }
    public Redemption_store? Store { get; set; }
    public Redemption_transaction? Transaction { get; set; }
}

public class Redemption_transaction
{
    public string? Id { get; set; }
    public string? Bill_number { get; set; }
}

public class Redemption_store
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}

public class Redeemed_by
{
    public string? Firstname { get; set; }
    public string? Mobile { get; set; }
}

public class CouponCustomer
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Mobile { get; set; }
}

public class Coupon_custom_properties
{
    public List<FieldCapillary>? Custom_property { get; set; }
}

public class CouponCapillaryByUserResponse
{
    public CouponCapillaryByUserEntity? Entity { get; set; }
    public List<object>? Warnings { get; set; }
}

public class CouponCapillaryByUserEntity
{
    public List<CapillaryCouponListByUser>? Customers { get; set; }
}

public class CapillaryCouponListByUser
{
    public string? Firstname { get; set; }
    public string? Mobile { get; set; }
    public string? Id { get; set; }
    public List<CapillaryCouponListByUserData>? Coupons { get; set; }
}

public class CapillaryCouponListByUserData
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public double DiscountValue { get; set; }
    public List<CouponRedemptions>? Redemptions { get; set; }
}

public class CouponRedemptions
{
    public string? Id { get; set; }
    public string? Date { get; set; }
    public string? TransactionNumber { get; set; }
    public RegisteredBy? RedeemedAt { get; set; }
}

public class ReActivateCouponsRequest
{
    public List<string>? RedemptionIds { get; set; }
}

public class ReActivateCouponsResponse
{
    public List<object>? Warnings { get; set; }
    public List<ErrorCapillary>? Errors { get; set; }
    public bool Success { get; set; }
}

public class POSRedeemCouponCapillary
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public int BillAmount { get; set; }
    [Required]
    public List<string> Code { get; set; } = [];
}

public class RedeemCouponRequestCap
{
    public List<CodeCouponCapillary>? RedemptionRequestList { get; set; }
    public string? TransactionNumber { get; set; }
    public UserCapillary? User { get; set; }
}

public class UserCapillary
{
    public string? Mobile { get; set; }
}

public class CodeCouponCapillary
{
    public string? Code { get; set; }
}

public class RedeemCouponResponseCap
{
    public List<ResultRedeemCouponResponseCap>? Redemption { get; set; }
}

public class DataRedeemCouponResponseCap
{
    public long EntityId { get; set; }
    public ResultRedeemCouponResponseCap? Result { get; set; }
    public List<object>? Errors { get; set; }
}

public class ResultRedeemCouponResponseCap
{
    public string? RedemptionId { get; set; }
    public long Id { get; set; }
    public bool CurrencyInput { get; set; }
    public int LocalToBaseCurrencyExchangeRate { get; set; }
    public List<object>? Warnings { get; set; }
    public string? AppendedErrorMessage { get; set; }
    public string? Code { get; set; }
    public string? DiscountCode { get; set; }
    public int SeriesCode { get; set; }
    public bool IsAbsolute { get; set; }
    public decimal CouponValue { get; set; }
    public RedemptionStatusCouponCapillary? RedemptionStatus { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int DiscountUpto { get; set; }
}

public class StatusCodeCouponCapillary
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public int Code { get; set; }
}

public class RedemptionStatusCouponCapillary
{
    public StatusCodeCouponCapillary? StatusCode { get; set; }
    public List<object>? Warnings { get; set; }
    public List<object>? WarningsAsStatusCode { get; set; }
    public string? Message { get; set; }
    public int Code { get; set; }
    public bool Success { get; set; }
}

public class ValidateCouponResponseError
{
    public ValidateCouponDataError? Response { get; set; }
}

public class ValidateCouponDataError
{
    public RedemptionStatus? Status { get; set; }
    public ErrorDataCoupons? Coupons { get; set; }
}

public class ErrorDataCoupons
{
    public RedeemableDataError? Redeemable { get; set; }
}

public class RedeemableDataError
{
    public string? Mobile { get; set; }
    public string? Code { get; set; }
    public string? Is_redeemable { get; set; }
    public ItemStatusCapillary? Item_status { get; set; }
}

public class ValidateCouponResponse
{
    public ValidateCouponData? Response { get; set; }
}

public class ValidateCouponData
{
    public DataCoupons? Coupons { get; set; }
    public RedemptionStatus? Status { get; set; }
    public List<object>? Warnings { get; set; }
}

public class DataCoupons
{
    public RedeemableData? Redeemable { get; set; }
}

public class RedeemableData
{
    public string? Mobile { get; set; }
    public string? Code { get; set; }
    public string? Is_redeemable { get; set; }
    public int Redemptions_left { get; set; }
    public int No_of_redemptions_by_user { get; set; }
    public string? Coupon_value { get; set; }
    public RedemptionCustomer? Customer { get; set; }
    public ItemStatusCoupon? Item_status { get; set; }
    public Series_info? Series_info { get; set; }
}

public class ItemStatusCoupon : ItemStatusCapillary
{
}

public class Series_info
{
    public int Id { get; set; }
    public int Org_id { get; set; }
    public string? Description { get; set; }
    public string? Series_type { get; set; }
    public string? Client_handling_type { get; set; }
    public string? Discount_code { get; set; }
    public string? Sms_template { get; set; }
    public string? Created { get; set; }
    public string? Tag { get; set; }
    public string? Valid_till_date { get; set; }
    public int Valid_days_from_create { get; set; }
    public int Transferrable { get; set; }
    public int Expiry_strategy_value { get; set; }
    public int Any_user { get; set; }
    public int Same_user_multiple_redeem { get; set; }
    public int Allow_referral_existing_users { get; set; }
    public int Multiple_use { get; set; }
    public int Is_validation_required { get; set; }
    public bool Valid_with_discounted_item { get; set; }
    public long Created_by { get; set; }
    public string? Series_code { get; set; }
    public string? Info { get; set; }
    public string? Dvs_expiry_date { get; set; }
    public bool Show_pin_code { get; set; }
    public string? Discount_on { get; set; }
    public string? Discount_type { get; set; }
    public int Discount_value { get; set; }
    public int Discount_upto { get; set; }
    public string? Dvs_items { get; set; }
    public string? Terms_and_condition { get; set; }
    public string? Redeem_store_type { get; set; }
    public List<ProductInfoOfCounpon>? ProductInfo { get; set; }
    public ProductDescription? Products { get; set; }
}

public class ProductDescription
{
    public List<ProductDescriptionData>? Product { get; set; }
}

public class ProductDescriptionData
{
    public string? Sku { get; set; }
    public string? Ean { get; set; }
    public string? Description { get; set; }
}

public class ProductInfoOfCounpon
{
    public string? ProductType { get; set; }
    public List<string>? ProductIds { get; set; }
}

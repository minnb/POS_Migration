using System.Collections.Generic;
using TCX.API.Common.Dtos.Capillary.Redemption;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class ValidateCouponResponseError
    {
        public ValidateCouponDataError Response { get; set; }
    }
    public class ValidateCouponDataError
    {
        public RedemptionStatus Status { get; set; }
        public ErrorDataCoupons Coupons { get; set; }
    }
    public class ErrorDataCoupons
    {
        public RedeemableDataError Redeemable { get; set; }
    }
    public class RedeemableDataError
    {
        public string Mobile { get; set; }
        public string Code { get; set; }
        public string Is_redeemable { get; set; }
        public ItemStatusCapillary Item_status { get; set; }
    }
    public class ValidateCouponResponse
    {
        public ValidateCouponData Response { get; set; }
    }
    public class ValidateCouponData
    {
        public DataCoupons Coupons { get; set; }
        public RedemptionStatus Status { get; set; }
        public List<object> Warnings { get; set; }
    }
   
    public class DataCoupons
    {
        public RedeemableData Redeemable { get; set; }
    }
    public class RedeemableData 
    { 
        public string Mobile { get; set; }
        public string Code { get; set; }
        public string Is_redeemable { get; set; }
        public int Redemptions_left { get; set; }
        public int No_of_redemptions_by_user { get; set; }
        public string Coupon_value { get; set; }
        public RedemptionCustomer Customer { get; set; }
        public ItemStatusCoupon Item_status { get; set; }
        public Series_info Series_info { get; set; }
    }
    public class ItemStatusCoupon: ItemStatusCapillary
    {
    }
    public class Series_info
    {
        public int Id { get; set; }
        public int Org_id { get; set; }
        public string Description { get; set; }
        public string Series_type { get; set; }
        public string Client_handling_type { get; set; }
        public string Discount_code { get; set; }
        public string Sms_template { get; set; }
        public string Created { get; set; }
        public string Tag { get; set; }
        public string Valid_till_date { get; set; }
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
        public string Series_code { get; set; }
        public string Info { get; set; }
        public string Dvs_expiry_date { get; set; }
        public bool Show_pin_code { get; set; }
        public string Discount_on { get; set; }
        public string Discount_type { get; set; }
        public int Discount_value { get; set; }
        public int Discount_upto { get; set; }
        public string Dvs_items { get; set; }
        public string Terms_and_condition { get; set; }
        public string Redeem_store_type { get; set; }
        public List<ProductInfoOfCounpon>  ProductInfo { get; set; }
        public ProductDescription Products { get; set; }

    }
    public class ProductDescription
    {
        public List<ProductDescriptionData> Product { get; set; }
    }
    public class ProductDescriptionData
    {
        public string Sku { get; set; }
        public string Ean { get; set; }
        public string Description { get; set; }
    }
    
    public class ProductInfoOfCounpon 
    {
        public string ProductType { get; set; }
        public List<string> ProductIds { get; set; }
    }
}

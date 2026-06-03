using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Redemption;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class CapillaryCouponDetailResponse
    {
        public CapillaryCouponDetailResponseData Response { get; set; }
    }
    public class CapillaryCouponDetailResponseData
    {
        public CapillaryCouponDetailData Coupons { get; set; }
        public RedemptionStatus Status { get; set; }
        public List<object> Warnings { get; set; }
    }
    public class CapillaryCouponDetailData
    {
        public List<CapillaryCouponDetail> Coupon { get; set; }
    }

    public class CapillaryCouponDetail
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Valid_till { get; set; }
        public string Issued_on { get; set; }
        public string Valid_from { get; set; }
        public string Purpose { get; set; }
        public long Series_id { get; set; }
        public bool Is_absolute { get; set; }
        public int Value { get; set; }
        public int Discount_upto { get; set; }
        public CouponCustomer Customer { get; set; }
        public ItemStatusCoupon Item_status { get; set; }
        public Redemption_info Redemption_info { get; set; }
        public Coupon_custom_properties Custom_properties { get; set; }
    }
    public class Redemption_info
    {
        public bool Redeemed { get; set; }
        public long Id { get; set; }
        public string Redeemed_on { get; set; }
        public string Redeemed_at { get; set; }
        public Redeemed_by Redeemed_by { get; set; }
        public Redemption_store Store { get; set; }
        public Redemption_transaction Transaction { get; set; }
    }
    public class Redemption_transaction
    {
        public string Id { get; set; }
        public string Bill_number { get; set; }
    }
    public class Redemption_store
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
    public class Redeemed_by
    {
        public string Firstname { get; set; }
        public string Mobile { get; set; }
    }
    public class CouponCustomer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
    }
    public class Coupon_custom_properties
    {
       public List<FieldCapillary> Custom_property { get; set; }
    }
}

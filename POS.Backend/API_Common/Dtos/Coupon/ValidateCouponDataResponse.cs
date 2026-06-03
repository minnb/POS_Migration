using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Coupons;

namespace TCX.API.Common.Dtos.Coupon
{
    public class ValidateCouponDataResponse
    {
        public string Mobile { get; set; }
        public string DiscountCode { get; set; }
        public decimal DiscountApply { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public string ValidTillDate { get; set; }
        public bool IsRedeemable { get; set; }
        public bool IsApplySku { get; set; }
        public List<ItemApplyCoupon> Items { get; set; }
    }
    public class ItemApplyCoupon
    {
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string ItemName { get; set; }
        public string ExpiryDate { get; set; }
        public string SellDate { get; set; }
    }
}

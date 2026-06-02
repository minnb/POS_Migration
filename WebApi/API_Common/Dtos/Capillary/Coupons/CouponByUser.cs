using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Redemption;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class CouponCapillaryByUserResponse
    {
        public CouponCapillaryByUserEntity Entity { get; set; }
        public List<object> Warnings { get; set; }
    }
    public class CouponCapillaryByUserEntity
    {
        public List<CapillaryCouponListByUser> Customers { get; set; }        
    }
    public class CapillaryCouponListByUser
    {
        public string Firstname { get; set; }
        public string Mobile { get; set; }
        public string Id { get; set; }
        public List<CapillaryCouponListByUserData> Coupons { get; set; }
    }
    public class CapillaryCouponListByUserData
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public double DiscountValue { get; set; }
        public List<CouponRedemptions> Redemptions { get; set; }
    }
    public class CouponRedemptions
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string TransactionNumber { get; set; }
        public RegisteredBy RedeemedAt { get; set; }
    }
}

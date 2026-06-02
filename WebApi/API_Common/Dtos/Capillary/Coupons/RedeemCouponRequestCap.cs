using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class POSRedeemCouponCapillary
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public int BillAmount { get; set; }
        [Required]
        public List<string> Code { get; set; }
    }
    public class RedeemCouponRequestCap
    {
        public List<CodeCouponCapillary> RedemptionRequestList { get; set; }
        public string TransactionNumber { get; set; }
        public UserCapillary User { get; set; }
        //public string Bill_number { get { return TransactionNumber; } }
    }
    public class UserCapillary
    {
        public string Mobile { get; set; }
    }
    public class CodeCouponCapillary
    {
        public string Code { get; set; }
    }
}

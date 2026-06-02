using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class RedeemCouponPartnerPOSRequest
    {
        [Required]
        public string Partner { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public int TotalAmount { get; set; }
        public List<LstVoucherPartner> SerialNo { get; set; }
    }
    public class UpdateStatusVoucherPartnerRequest
    {
        [Required]
        public string Partner { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public List<LstVoucherPartner> SerialNo { get; set; }
        public List<SkuApplyVoucherPartner> Items { get; set; }
    }
    public class LstVoucherPartner
    {
        [Required]
        public string Code { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}

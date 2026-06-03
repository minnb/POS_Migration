using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Coupon
{
    public class CheckCouponRequest
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string Partner {  get; set; }
        public string PhoneNumber { get; set; }
        [Required]
        public string[] SerialNo { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Attribute;

namespace TCX.API.Common.Dtos.Coupon
{
    
    public class ReActiveCouponRequest
    {
        [Required]
        public string Partner { get; set; }
        [Required]
        public string StoreNo { get; set; }
        
        [Required]
        [NotEmptyItems(ErrorMessage = "SerialNo không được chứa phần tử rỗng hoặc chỉ khoảng trắng.")]
        public List<string> SerialNo { get; set; }
        [Required]
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }

    }
}

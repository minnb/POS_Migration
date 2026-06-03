using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
   public class RequestVoucherPaymentModel
    {
        public int amount { get; set; } //voucher item(type= 1): giá trị hiện tại trên hoá đơn của item được áp dụng. voucher fixed amount (type=2): không cần truyền,voucher unfixed amount (type=3): giá trị có thể thanh toán mà UrBox trả về
        public string bill_id { get; set; }
        public string code { get; set; }
        public string staff_id { get; set; }
        public string store_id { get; set; }
        public string terminal_id { get; set; }
        public string time { get; set; }//dd-m-yyyy hh:mm:ss

    }
}

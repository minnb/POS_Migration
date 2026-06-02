using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
   public class ResponseVoucherPaymentModel
    {
        public int done { get; set; }
        public int type { get; set; }
        public int status { get; set; }
        public ResponseDataPayment data { get; set; }
    }
    public class ResponseDataPayment
    {
        public string msg { get; set; }
        public string code { get; set; }
        public int amount { get; set; }
        public string bill_id { get; set; }
        public string coupon { get; set; }        
        public string using_time { get; set; }       
        public string item_id { get; set; }
    }
}

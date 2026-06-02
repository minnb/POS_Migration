using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class TransactionDetailResponse
    {
        public DMeta meta { get; set; }
        public DData data { get; set; }
    }
    public class DMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class DData
    {
        public string transaction_id { get; set; }
        public string transaction_ref_number { get; set; }
        public string transaction_status { get; set; }
        public string order_id { get; set; }
        public string order_title { get; set; }
        public string order_created_at { get; set; }
        public string oe_points_using { get; set; }
        public string bill_amount { get; set; }
        public string fee { get; set; }
        public string payment_amount { get; set; }
        public string currency { get; set; }
    }
}

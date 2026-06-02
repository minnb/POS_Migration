using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class PLRefundCXRequestModel
    {
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string cardNumber { get; set; }
        public string merchantId { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string orderNo { get; set; }
        public string origOrderNo { get; set; }
        public int spendPoints { get; set; }
        public double refundAmount { get; set; }
        public double orderAmount { get; set; }
        public string referenceInvoice { get; set; }
        public bool lastTxnInd { get; set; }
        public string clubCode { get; set; }
        public int refundStampsPoints { get; set; }
    }
}

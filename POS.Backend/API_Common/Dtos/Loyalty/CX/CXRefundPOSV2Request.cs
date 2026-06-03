using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class CXRefundPOSV2Request
    {
        public string clubCode { get; set; }
        public string cardNumber { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string orderNo { get; set; }
        public string origOrderNo { get; set; }
        public int spendPoints { get; set; }
        public List<CXRefundAmount> refundAmount { get; set; }
    }

}

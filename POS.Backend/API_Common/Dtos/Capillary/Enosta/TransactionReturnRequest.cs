using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Transaction;

namespace TCX.API.Common.Dtos.Capillary.Enosta
{
    public class TransactionReturnFullRequest: HeaderTransctionReturn
    {

    }
    public class HeaderTransctionReturn
    {
        public string Type { get; set; }
        public string ReturnType { get; set; }
        public string BillNumber { get; set; }
        public string Mobile { get; set; }
        public string Return_trans_id { get; set; }
    }
    public class TransactionReturnPartialRequest : HeaderTransctionReturn
    {
        public string BillingDate { get; set; }
        public decimal BillAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public List<EnostaLineItemV2> LineItemsV2 { get; set; }

    }
}

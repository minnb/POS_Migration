using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class CXRefundPOSRequest
    {
        public string CardNumber { get; set; }
        // public string PhoneNumber { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string InvoiceNo { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public int SpendPoints { get; set; }
        public decimal RefundAmount { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public string Source { get; set; }
        public string ReferenceInvoice { get; set; }
        public bool LastTxnInd { get; set; }
    }
}

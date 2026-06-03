using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class CXSalesPOSRequest
    {
        public string CardNumber { get; set; }
        // public string PhoneNumber { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string InvoiceNo { get; set; }
        public int SpendPoints { get; set; }
        public double BillAmount { get; set; }
        public string OrderNo { get; set; }
        public bool IsOffline { get; set; }
        public double OrderAmount { get; set; }
        public string Source { get; set; }
    }
    public class CXRefundV2ModelRequest
    {
        public string phoneNo { get; set; }
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public int refundPoint { get; set; }
        public long transactionTime { get; set; }
        public string merchantId { get; set; }
        public string clubCode { get; set; }
        public List<CXRefundAmount> refundAmount { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class CXSalesModelRequest
    {
        public string phoneNo { get; set; }
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }      
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public int spendPoints { get; set; }
        public double totalBillAmount { get; set; }
        public long transactionTime { get; set; }
        public string merchantId { get; set; }
        public string clubCode { get; set; }

    }

    public class CXRefundModelRequest
    {
        public string phoneNo { get; set; }
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public int refundPoint { get; set; }
        public double refundAmount { get; set; }
        public long transactionTime { get; set; }
        public string merchantId { get; set; }
        public string clubCode { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
   public class CXRefundBlueModel
    {
        public string cardNo { get; set; }
        public string phone { get; set; }
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string transactionType { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
        public double totalBillAmount { get; set; }
        public List<CXRefundBlueItemModel> billLines { get; set; }
    }
    public class CXRefundBlueItemModel
    {
        public int recordNo { get; set; }
        public int originalRecordNo { get; set; }
        public string article { get; set; }
        public string barcode { get; set; }
        public string uom { get; set; }
        public float? quantity { get; set; }
        public float? salePrice { get; set; }
        public float? amount { get; set; }
        public float? discountAmount { get; set; }
        public float? lineAmount { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
   public class CXRefundBlueResponse
    {
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string transactionType { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
        public double totalBillAmount { get; set; }
        public string employeeCode { get; set; }
        public string companyCode { get; set; }
        public double extraEarnByItems { get; set; }
        public double extraEarnByCampaign { get; set; }
        public double remainingCampaignQuota { get; set; }
        public double currencyRate { get; set; }
        public List<CXRefundBlueItemRespone> billLines { get; set; }
    }
    public class CXRefundBlueItemRespone
    {
        public int recordNo { get; set; }
        public string article { get; set; }
        public string barcode { get; set; }
        public string uom { get; set; }
        public float? quantity { get; set; }
        public float? salePrice { get; set; }
        public float? amount { get; set; }
        public float? discountAmount { get; set; }
        public float? lineAmount { get; set; }
        public float? extraQuantityEarn { get; set; }
        public float? extraAmountEarn { get; set; }
        public float? remainingItemQuota { get; set; }
        public int originalRecordNo { get; set; }
    }
}

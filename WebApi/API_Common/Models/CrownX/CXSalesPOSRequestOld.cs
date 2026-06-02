using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class CXSalesPOSRequestOld
    {
        public string ClubCode { get; set; }
        public string MerchantID { get; set; }
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
        public string ReferenceInvoice { get; set; }
    }
    public class CXRefundPOSRequestOld
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
        public string ReferenceInvoice { get; set; }
        public bool LastTxnInd { get; set; }

    }

    public class CXRefundPOSV2Request1
    {
        public string cardNumber { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string orderNo { get; set; }
        public string origOrderNo { get; set; }
        public int spendPoints { get; set; }
        public List<CXRefundAmount1> refundAmount { get; set; }
    }
    public class CXRefundV2ModelRequest1
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
        public List<CXRefundAmount1> refundAmount { get; set; }
    }
    public class CXRefundAmount1
    {
        public string loyaltyMerchantId { get; set; }
        public double amount { get; set; }
    }
}

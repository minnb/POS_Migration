using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace TCX.API.Common.Dtos.Loyalty
{
    public class VinIDRefundRequest
    {
        public string QRCode { get; set; }
        [Required]
        public string CardNumber { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }
        public string OrderNo { get; set; }
        [Required]
        public string OrigOrderNo { get; set; }
        public long SpendPoints { get; set; }
        public decimal RefundAmount { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public bool IsScanAndGo { get; set; } = false;
        public DateTime OrderTime { get; set; }
        public int TransactionType { get; set; } //2 - FULL, 3 - LINE_ITEM
        public string RedemptionId { get; set; }
        public string CashierID { get; set; }
        public List<CXRefundAmount> CXRefundAmount { get; set; }
        public List<TransLineLoyalty> TransLine { get; set; }
        public List<PaymentEntryLoyalty> TransPaymentEntry { get; set; }
        public int RefundStampsPoints { get; set; }
    }
    public class CXRefundAmount
    {
        public string LoyaltyMerchantId { get; set; }
        public double Amount { get; set; }
    }
}

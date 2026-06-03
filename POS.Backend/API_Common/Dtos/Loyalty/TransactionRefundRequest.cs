using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Model.VINID;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class TransactionRefundRequest
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
        public int SpendPoints { get; set; }
        public decimal RefundAmount { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public bool IsScanAndGo { get; set; } = false;
        public DateTime OrderTime { get; set; }
        public int TransactionType { get; set; } //2 - FULL, 3 - LINE_ITEM
        public string RedemptionId { get; set; }
        public List<AmountToEarnData> AmountToEarn { get; set; }
        public List<TransLineLoyaltyData> TransLine { get; set; }
        public List<PaymentEntryLoyaltyData> TransPaymentEntry { get; set; }
    }
}

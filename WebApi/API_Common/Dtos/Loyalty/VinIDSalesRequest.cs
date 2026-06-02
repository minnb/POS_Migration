using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TCX.API.Common.Models;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class VinIDSalesRequest
    {
        public string QRCode { get; set; }
        [Required]
        public string CardNumber { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string InvoiceNo { get; set; }
        public long SpendPoints { get; set; }
        public decimal BillAmount { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public string VirtualCard { get; set; } = "";
        public string CashierID { get; set; }
        public DateTime OrderTime { get; set; }
        public int TransactionType { get; set; }
        public int RedeemStampsPoint { get; set; }
        public List<amountToEarn> AmountToEarn { get; set; }
        public List<TransLineLoyalty> TransLine { get; set; }
        public List<PaymentEntryLoyalty> TransPaymentEntry { get; set; }
        public string OrigOrderNo { get; set; } = ""; 
        public bool IsMobile { get; set; }
        public string OrderChannel { get; set; } = "";
        public bool IsRetry { get; set; }
    }

    public class MemberBusinessRequest
    {
        [Required]
        public string CardNumber { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public string CashierID { get; set; }
        [Required]
        public string Status { get; set; }
        public string Key { get; set; }
        public List<TransLineLoyalty> TransLine { get; set; }
    }
}

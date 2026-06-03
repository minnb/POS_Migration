using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class TransactionLoyaltyRequest
    {
        public string QRCode { get; set; }
        [Required]
        public string CardNumber { get; set; }
        [Required]
        public string MerchantId { get; set; }
        [Required]
        public string TerminalId { get; set; }
        [Required]
        public string InvoiceNo { get; set; }
        public int SpendPoints { get; set; }
        public decimal BillAmount { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public string VirtualCard { get; set; } = "";
        public DateTime OrderTime { get; set; }
        public int TransactionType { get; set; } //1-SALES, 2 - FULL, 3 - LINE_ITEM
        public List<AmountToEarnData> AmountToEarn { get; set; }
        public List<TransLineLoyaltyData> TransLine { get; set; }
        public List<PaymentEntryLoyaltyData> TransPaymentEntry { get; set; }
    }
    public class AmountToEarnData//Tich diem
    {
        public string loyaltyMerchantId { get; set; }//Mã đối tác quản lý chương trình Loyalty PLH/VCM/DWN
        public double amount { get; set; }//Giá trị tính điểm thưởng theo chương trình Loyalty tương ứng
    }
    public class TransLineLoyaltyData
    {
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal VatAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public int DiscountType { get; set; }
        public decimal LineAmountIncVAT { get; set; }
        public string Size { get; set; }
    }
    public class PaymentEntryLoyaltyData
    {
        public int LineNo { get; set; }
        public string TenderType { get; set; }
        public decimal AmountTendered { get; set; }
        public string CardType { get; set; }
    }
}

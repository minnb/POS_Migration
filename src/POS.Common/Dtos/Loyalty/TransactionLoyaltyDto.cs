using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Loyalty;

public class TransactionLoyaltyRequest
{
    public string? QRCode { get; set; }
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    [Required]
    public string MerchantId { get; set; } = string.Empty;
    [Required]
    public string TerminalId { get; set; } = string.Empty;
    [Required]
    public string InvoiceNo { get; set; } = string.Empty;
    public int SpendPoints { get; set; }
    public decimal BillAmount { get; set; }
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public string VirtualCard { get; set; } = "";
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public List<AmountToEarnData>? AmountToEarn { get; set; }
    public List<TransLineLoyaltyData>? TransLine { get; set; }
    public List<PaymentEntryLoyaltyData>? TransPaymentEntry { get; set; }
}

public class AmountToEarnData
{
    public string? loyaltyMerchantId { get; set; }
    public double amount { get; set; }
}

public class TransLineLoyaltyData
{
    public string? ItemCode { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int DiscountType { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? Size { get; set; }
}

public class PaymentEntryLoyaltyData
{
    public int LineNo { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

public class TransactionRefundRequest
{
    public string? QRCode { get; set; }
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? InvoiceNo { get; set; }
    public string? OrderNo { get; set; }
    [Required]
    public string OrigOrderNo { get; set; } = string.Empty;
    public int SpendPoints { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public bool IsScanAndGo { get; set; } = false;
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public string? RedemptionId { get; set; }
    public List<AmountToEarnData>? AmountToEarn { get; set; }
    public List<TransLineLoyaltyData>? TransLine { get; set; }
    public List<PaymentEntryLoyaltyData>? TransPaymentEntry { get; set; }
}

public class TransactionOfflineDto
{
    public string? TableName { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? OrigOrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string? CardNumber { get; set; }
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public string? ItemNo { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal VatAmmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

public class amountToEarn
{
    public string? loyaltyMerchantId { get; set; }
    public double amount { get; set; }
}

public class VinIDSalesRequest
{
    public string? QRCode { get; set; }
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? InvoiceNo { get; set; }
    public long SpendPoints { get; set; }
    public decimal BillAmount { get; set; }
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public string VirtualCard { get; set; } = "";
    public string? CashierID { get; set; }
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public int RedeemStampsPoint { get; set; }
    public List<amountToEarn>? AmountToEarn { get; set; }
    public List<TransLineLoyalty>? TransLine { get; set; }
    public List<PaymentEntryLoyalty>? TransPaymentEntry { get; set; }
    public string OrigOrderNo { get; set; } = "";
    public bool IsMobile { get; set; }
    public string OrderChannel { get; set; } = "";
    public bool IsRetry { get; set; }
}

public class MemberBusinessRequest
{
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public string? CashierID { get; set; }
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Key { get; set; }
    public List<TransLineLoyalty>? TransLine { get; set; }
}

public class VinIDRefundRequest
{
    public string? QRCode { get; set; }
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? InvoiceNo { get; set; }
    public string? OrderNo { get; set; }
    [Required]
    public string OrigOrderNo { get; set; } = string.Empty;
    public long SpendPoints { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public bool IsScanAndGo { get; set; } = false;
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public string? RedemptionId { get; set; }
    public string? CashierID { get; set; }
    public List<CXRefundAmount>? CXRefundAmount { get; set; }
    public List<TransLineLoyalty>? TransLine { get; set; }
    public List<PaymentEntryLoyalty>? TransPaymentEntry { get; set; }
    public int RefundStampsPoints { get; set; }
}

public class CXRefundAmount
{
    public string? LoyaltyMerchantId { get; set; }
    public double Amount { get; set; }
}

public class CXSalesPOSRequest
{
    public string? CardNumber { get; set; }
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? InvoiceNo { get; set; }
    public int SpendPoints { get; set; }
    public double BillAmount { get; set; }
    public string? OrderNo { get; set; }
    public bool IsOffline { get; set; }
    public double OrderAmount { get; set; }
    public string? Source { get; set; }
}

public class CXRefundV2ModelRequest
{
    public string? phoneNo { get; set; }
    public string? cardNo { get; set; }
    public string? invoiceNo { get; set; }
    public string? originalInvoiceNo { get; set; }
    public string? storeCode { get; set; }
    public string? posCode { get; set; }
    public int refundPoint { get; set; }
    public long transactionTime { get; set; }
    public string? merchantId { get; set; }
    public string? clubCode { get; set; }
    public List<CXRefundAmount>? refundAmount { get; set; }
}

public class CXRefundPOSRequest
{
    public string? CardNumber { get; set; }
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? InvoiceNo { get; set; }
    public string? OrderNo { get; set; }
    public string? OrigOrderNo { get; set; }
    public int SpendPoints { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public string? Source { get; set; }
    public string? ReferenceInvoice { get; set; }
    public bool LastTxnInd { get; set; }
}

public class CXRefundPOSV2Request
{
    public string? clubCode { get; set; }
    public string? cardNumber { get; set; }
    public string? storeCode { get; set; }
    public string? posCode { get; set; }
    public string? orderNo { get; set; }
    public string? origOrderNo { get; set; }
    public int spendPoints { get; set; }
    public List<CXRefundAmount>? refundAmount { get; set; }
}

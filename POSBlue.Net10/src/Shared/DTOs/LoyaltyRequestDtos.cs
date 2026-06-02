using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Shared.DTOs;

// Request DTOs từ POS client → LoyaltyController.
// Giữ nguyên tên field theo convention camelCase/PascalCase của từng endpoint cũ để client không cần thay đổi.

public class VinIdSalesRequest
{
    public string? QRCode { get; set; }
    [Required] public string CardNumber { get; set; } = "";
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? InvoiceNo { get; set; }
    public long SpendPoints { get; set; }
    public decimal BillAmount { get; set; }
    [Required] public string OrderNo { get; set; } = "";
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public string VirtualCard { get; set; } = "";
    public string? CashierID { get; set; }
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public int RedeemStampsPoint { get; set; }
    public List<AmountToEarnItem>? AmountToEarn { get; set; }
    public List<TransLineLoyaltyItem>? TransLine { get; set; }
    public List<PaymentEntryLoyaltyItem>? TransPaymentEntry { get; set; }
    public string OrigOrderNo { get; set; } = "";
    public bool IsMobile { get; set; }
    public string OrderChannel { get; set; } = "";
    public bool IsRetry { get; set; }
}

public class VinIdRefundRequest
{
    public string? QRCode { get; set; }
    [Required] public string CardNumber { get; set; } = "";
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? InvoiceNo { get; set; }
    public string? OrderNo { get; set; }
    [Required] public string OrigOrderNo { get; set; } = "";
    public long SpendPoints { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public bool IsScanAndGo { get; set; }
    public DateTime OrderTime { get; set; }
    public int TransactionType { get; set; }
    public string? RedemptionId { get; set; }
    public string? CashierID { get; set; }
    public List<CxRefundAmountItem>? CXRefundAmount { get; set; }
    public List<TransLineLoyaltyItem>? TransLine { get; set; }
    public List<PaymentEntryLoyaltyItem>? TransPaymentEntry { get; set; }
    public int RefundStampsPoints { get; set; }
}

public class AmountToEarnItem
{
    public string? loyaltyMerchantId { get; set; }
    public double amount { get; set; }
}

public class CxRefundAmountItem
{
    public string? LoyaltyMerchantId { get; set; }
    public double Amount { get; set; }
}

public class TransLineLoyaltyItem
{
    public int LineNo { get; set; }
    public int? ParentLineNo { get; set; }
    public string? ItemCode { get; set; }
    public string? Barcode { get; set; }
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

public class PaymentEntryLoyaltyItem
{
    public int LineNo { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

// --- POST api/v2/loyalty/customer (đăng ký hội viên) ---
public class CustomerRegisterRequest
{
    [Required] public string fullName { get; set; } = "";
    [Required] public string title { get; set; } = "";
    [Required, MinLength(9)] public string phoneNo { get; set; } = "";
    [Required] public string gender { get; set; } = "";
    public string? otp { get; set; }
    public string? userPOS { get; set; }
    [Required, StringLength(6, MinimumLength = 4)] public string storeCode { get; set; } = "";
    [Required] public string posCode { get; set; } = "";
    public string? dob { get; set; }
    public string? identifierCardid { get; set; }
    public string? issuedDate { get; set; }
    public string? issuedPlace { get; set; }
    public string status { get; set; } = "REGISTER";
    public string? email { get; set; }
    public string? address { get; set; }
    public string? province { get; set; }
    public string? district { get; set; }
    public string? city { get; set; }
    public string? ward { get; set; }
}

// --- POST api/v2/loyalty/customer/update ---
public class CustomerUpdateRequest
{
    public string? fullName { get; set; }
    public string? phoneNo { get; set; }
    public string? gender { get; set; }
    public string? title { get; set; }
    public string? storeCode { get; set; }
    public string? posCode { get; set; }
    public string? dob { get; set; }
    public string? email { get; set; }
    public string? address { get; set; }
    public string? province { get; set; }
    public string? district { get; set; }
    public string? city { get; set; }
    public string? ward { get; set; }
}

// --- POST api/v2/loyalty/other-status ---
public class OtherStatusUpdateRequest
{
    [Required] public string OtherStatus { get; set; } = "";
    [Required] public string PhoneNumber { get; set; } = "";
    [Required] public string StoreNo { get; set; } = "";
    [Required] public string PosNo { get; set; } = "";
    public string? CashierID { get; set; }
}

// --- POST api/vinid/InitTransaction ---
public class PosTransactionRequest
{
    public string? QRCode { get; set; }
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public decimal BillAmount { get; set; }
    public decimal Amount { get; set; }
    public string? InvoiceNo { get; set; }
}

// --- PUT api/vinid/CancelTransaction ---
public class PosCancelTransactionRequest
{
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? OrderNo { get; set; }
    public string? TransactionId { get; set; }
}

// --- PUT api/vinid/ScanAndGo/UpdateStatusOrder ---
public class ScanAndGoStatusOrderRequest
{
    public string? ScanAndGoOrderNo { get; set; }
    public string? OrderNo { get; set; }
    public int? Status { get; set; }
    public double? CollectedAmount { get; set; }
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? CardNumber { get; set; }
    public double? SpendPoints { get; set; }
}

// --- POST api/vinid/ExtraSales ---
public class ExtraSalesPosRequest
{
    public string? CardNumber { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreCode { get; set; }
    public string? PosTerminal { get; set; }
    public string? CashierCode { get; set; }
    public string? CustomerName { get; set; }
    public string? VinidCardNumber { get; set; }
    public string? VinidCSN { get; set; }
    public float? TotalBillAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public List<BillLineRequestItem>? BillLines { get; set; }
}

public class BillLineRequestItem
{
    public double RecordNo { get; set; }
    public string? Barcode { get; set; }
    public string? Article { get; set; }
    public string? ArticleName { get; set; }
    public string? UOM { get; set; }
    public float? Quantity { get; set; }
    public float? SalePrice { get; set; }
    public float? Amount { get; set; }
    public float? DiscountAmount { get; set; }
    public float? LineAmount { get; set; }
}

// --- POST api/vinid/ExtraRefund ---
public class ExtraRefundPosRequest
{
    public string? CardNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreCode { get; set; }
    public string? PosTerminal { get; set; }
    public string? CashierCode { get; set; }
    public float? TotalBillAmount { get; set; }
    public string? OriginalReceiptNumber { get; set; }
    public string? OriginalPosNumber { get; set; }
    public string? OriginalReferenceNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public float? EarnByDefault { get; set; }
    public float? RefundAmount { get; set; }
    public float? RefundPointDefault { get; set; }
    public List<BillLineRefundRequestItem>? BillLines { get; set; }
}

public class BillLineRefundRequestItem
{
    public double RecordNo { get; set; }
    public string? Barcode { get; set; }
    public string? Article { get; set; }
    public string? ArticleName { get; set; }
    public string? UOM { get; set; }
    public float? Quantity { get; set; }
    public float? RefundQuantity { get; set; }
    public float? SalePrice { get; set; }
    public float? Amount { get; set; }
    public float? DiscountAmount { get; set; }
    public float? LineAmount { get; set; }
}

// --- POST api/vinid/ScanAndGo/SnGRefund ---
public class ScanAndGoRefundRequest
{
    public string? BarcodeOrder { get; set; }
    public string? OrigOrderNo { get; set; }
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? CardNumber { get; set; }
    public string? ReturnNo { get; set; }
    public long? RedeemingPoint { get; set; }
    public List<ScanAndGoRefundItem>? Items { get; set; }
}

public class ScanAndGoRefundItem
{
    public long? OrderItemId { get; set; }
    public string? BarcodeItem { get; set; }
    public long? ReturnSaleQuantity { get; set; }
    public double? ReturnUnitQuantity { get; set; }
    public string? Note { get; set; }
}

// --- POST api/vinid/ScanAndGo/SnGTopup ---
public class ScanAndGoTopupRequest
{
    public string? BarcodeOrder { get; set; }
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? CardNumber { get; set; }
    public string? TopupNo { get; set; }
    public List<TopupItem>? TopupData { get; set; }
}

public class TopupItem
{
    public long? Amount { get; set; }
    public string? Type { get; set; }
}

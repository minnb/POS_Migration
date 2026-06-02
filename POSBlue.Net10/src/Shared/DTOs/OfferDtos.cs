using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Shared.DTOs;

// ── Offer / Promotion Staff DTOs ──────────────────────────────────────────────

public class AmountToEarn
{
    public string? LoyaltyMerchantId { get; set; }
    public double  Amount { get; set; }
}

public class TransLineLoyalty
{
    public int     LineNo { get; set; }
    public int?    ParentLineNo { get; set; }
    public string? ItemCode { get; set; }
    public string? Barcode { get; set; }
    public string? PackId { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? ParentQuantity { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int     DiscountType { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? Size { get; set; }
    public string? CardLevel { get; set; }
    public string? DivisionCode { get; set; }
    public bool    IsAward { get; set; } = true;
}

public class PaymentEntryLoyalty
{
    public int     LineNo { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

public class VinIDSalesRequest
{
    public string? QRCode { get; set; }
    [Required] public string? CardNumber { get; set; }
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? InvoiceNo { get; set; }
    public long    SpendPoints { get; set; }
    public decimal BillAmount { get; set; }
    [Required] public string? OrderNo { get; set; }
    public bool    IsOffline { get; set; }
    public decimal OrderAmount { get; set; }
    public string? VirtualCard { get; set; } = "";
    public string? CashierID { get; set; }
    public DateTime OrderTime { get; set; }
    public int     TransactionType { get; set; }
    public int     RedeemStampsPoint { get; set; }
    public List<AmountToEarn>? AmountToEarn { get; set; }
    public List<TransLineLoyalty>? TransLine { get; set; }
    public List<PaymentEntryLoyalty>? TransPaymentEntry { get; set; }
    public string? OrigOrderNo { get; set; } = ""; 
    public bool    IsMobile { get; set; }
    public string? OrderChannel { get; set; } = "";
    public bool    IsRetry { get; set; }
}

public class OfferStaffTransactionRequest
{
    [Required] public string? ClubCode { get; set; }
    [Required] public string? StoreNo { get; set; }
    [Required] public string? PosNo { get; set; }
    [Required] public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    [Required] public string? PhoneNumber { get; set; }
    [Required] public string? StaffCode { get; set; }
    public decimal Amount { get; set; }
    [Required] public int TransactionType { get; set; }
    public string? ReturnedOrderNo { get; set; }
}

public class PromotionStaffRedeemPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RedeemDate { get; set; }
    public float   Amount { get; set; }
    public string? OrderNo { get; set; }
}

public class PromotionStaffRedeemPartnerRequest
{
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RedeemDate { get; set; }
    public float   Amount { get; set; }
    public string? RefCode { get; set; }
}

public class PromotionStaffRefundPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RefundDate { get; set; }
    public float   Amount { get; set; }
    public string? OrderNo { get; set; }
}

public class PromotionStaffResponse
{
    public int     Status { get; set; }
    public string? Description { get; set; }
    public int     TotalItems { get; set; }
    public string? TechMsg { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

public class OfferStaffSetupDto
{
    public string? ClubCode { get; set; }
    public DateTime ApplyFrom { get; set; }
    public DateTime ValidBefore { get; set; }
    public bool Blocked { get; set; }
}

public class OfferStaffRemnDto
{
    public string? ClubCode { get; set; }
    public string? Month { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StaffCode { get; set; }
    public decimal RemainQuota { get; set; }
    public decimal InitQuota { get; set; }
    public DateTime ChgDate { get; set; }
    public DateTime CrtDate { get; set; }
}

public class CheckOfferStaffRemn : OfferStaffRemnDto
{
    public string? PosNo { get; set; }
    public DateTime ExpiryTime { get; set; }
}

public class OfferStaffTransactionDto
{
    public string? ClubCode { get; set; }
    public string? Month { get; set; }
    public decimal InitQuota { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StaffCode { get; set; }
    public decimal Amount { get; set; }
    public int TransactionType { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public DateTime CrtDate { get; set; }
}

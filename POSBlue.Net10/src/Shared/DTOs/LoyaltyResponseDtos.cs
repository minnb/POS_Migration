namespace VCM.POSBLUE.Shared.DTOs;

// Response DTOs trả về cho POS client từ LoyaltyController.
// Giữ nguyên tên field để client không cần thay đổi.

public class InfoMemberDto
{
    public string? CardNumber { get; set; }
    public string? VirtualCard { get; set; }
    public string? CMND { get; set; }
    public string? MemberName { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CardLevel { get; set; }
    public long? MemberPoint { get; set; }
    public long? TotalPoint { get; set; }
    public long? RedemptionValue { get; set; }
    public int? CurrentRate { get; set; }
    public string? MemberCSN { get; set; }
    public string? OtherInfo { get; set; }
    public string? QRCode { get; set; }
    public bool? ExtraPoint { get; set; }
    public bool IsRedeem { get; set; }
    public bool? IsOfflineVinID { get; set; }
    public bool IsShowMessage { get; set; }
    public string? Status { get; set; }
    public string? System { get; set; }
    public string? ClubCode { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Dob { get; set; }
    public bool? BirthdayGiftInd { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ExternalId { get; set; }
    public string? OtherStatus { get; set; }
    public string? MemberType { get; set; }
    public string? Source { get; set; }
    public List<ExtendedFieldItem>? ExtendedFields { get; set; }
    public List<AvailablePromotionItem>? AvailablePromotion { get; set; }
}

public class ExtendedFieldItem
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class AvailablePromotionItem
{
    public string? WinCode { get; set; }
    public int? Quantity { get; set; }
    public string? DiscountType { get; set; }
    public string? MerchantId { get; set; }
    public decimal InitQuota { get; set; }
    public decimal RemainQuota { get; set; }
    public string? Serial { get; set; }
    public string? PromotionCode { get; set; }
}

public class PointDto
{
    public int? PointEarn { get; set; }
    public int? PointRedeem { get; set; }
    public int? CurrentRate { get; set; }
    public bool? IsOfflineVinID { get; set; }
    public string? RedemptionId { get; set; }
    public string? ReversalId { get; set; }
    public string? OrderNo { get; set; }
    public string? EmpCode { get; set; }
    public bool? MasanerPackageInd { get; set; }
    public int? StaffPercentage { get; set; }
    public int? NormCustPercentage { get; set; }
    public List<PointEarnCampaignItem>? ExtraEarnByCampaign { get; set; }
}

public class PointEarnCampaignItem
{
    public string? LoyaltyMerchantId { get; set; }
    public double Amount { get; set; }
    public double EarnedPoints { get; set; }
}

public class ResultTransactionDto
{
    public int Code { get; set; }
    public string? TransactionID { get; set; }
    public string? Message { get; set; }
    public string? TransactionStatus { get; set; }
    public string? TransactionWalletNumber { get; set; }
}

public class CancelTransactionResultDto
{
    public bool Status { get; set; }
}

public class ScanAndGoPosResponseDto
{
    public string? OrderNo { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? OrderDate { get; set; }
    public double? Amount { get; set; }
    public double? AmountDeducted { get; set; }
    public double? VinIdPaid { get; set; }
    public double? AmountCollect { get; set; }
    public string? VoucherId { get; set; }
    public string? VoucherName { get; set; }
    public string? VinIdCardNumber { get; set; }
    public string? VinIdCardName { get; set; }
    public double? DeliveryType { get; set; }
    public string? DeliveryTypeName { get; set; }
    public double? Status { get; set; }
    public string? Address { get; set; }
    public double? Fee { get; set; }
    public bool? IsExpired { get; set; }
    public string? SiteCode { get; set; }
    public string? PricingStoreCode { get; set; }
    public int? VinIdEarn { get; set; }
    public bool? HasVatInvoice { get; set; }
    public List<ScanPaymentItem>? ListPayments { get; set; }
    public List<ScanItemDto>? ListItems { get; set; }
    public BillInfoDto? BillingInfo { get; set; }
}

public class ScanPaymentItem
{
    public string? PaymentMethod { get; set; }
    public double? PaidAmount { get; set; }
    public string? TransactionId { get; set; }
}

public class ScanItemDto
{
    public string? Id { get; set; }
    public string? ArticleId { get; set; }
    public string? ArticleName { get; set; }
    public double? SoldPrice { get; set; }
    public double? Quantity { get; set; }
    public string? UnitCode { get; set; }
    public string? ProductBarcode { get; set; }
    public string? OriginBarcode { get; set; }
    public string? ItemType { get; set; }
    public double? VatGroup { get; set; }
    public double? VatPercent { get; set; }
    public double? NetPrice { get; set; }
    public string? PromotionCode { get; set; }
    public double? PromotionValue { get; set; }
    public double? UnitQuantity { get; set; }
    public double? SaleQuantity { get; set; }
    public bool? IsFreshfood { get; set; }
    public double? UnitSoldPrice { get; set; }
    public double? TotalSoldPrice { get; set; }
}

public class BillInfoDto
{
    public string? CustomerName { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SiteCode { get; set; }
}

public class StatusOrderPosDto
{
    public string? ReceiptNumber { get; set; }
    public string? TransactionType { get; set; }
    public string? StoreCode { get; set; }
    public string? PosCode { get; set; }
    public string? CashierCode { get; set; }
    public string? MerchantId { get; set; }
    public string? TerminalId { get; set; }
    public string? CustomerName { get; set; }
    public string? VinidCardNumber { get; set; }
    public string? VinidCSN { get; set; }
    public double? TotalBillAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? ExtraEarnByItems { get; set; }
    public int? ExtraEarnByCampaign { get; set; }
    public int? VinIdEarn { get; set; }
    public bool? IsEarnSuccess { get; set; }
    public bool? OverQuota { get; set; }
    public List<StatusBillLineDto>? BillLines { get; set; }
}

public class StatusBillLineDto
{
    public float? RecordNo { get; set; }
    public string? Barcode { get; set; }
    public string? Article { get; set; }
    public string? ArticleName { get; set; }
    public string? UOM { get; set; }
    public float? Quantity { get; set; }
    public float? SalePrice { get; set; }
    public float? Amount { get; set; }
    public float? DiscountAmount { get; set; }
    public float? LineAmount { get; set; }
    public float? ExtraQuantityEarn { get; set; }
    public float? ExtraAmountEarn { get; set; }
    public float? VCMUnitPrice { get; set; }
}

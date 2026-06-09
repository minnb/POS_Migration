using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Loyalty;

public class CustomerInfo
{
    public string? Title { get; set; }
    public string? PhoneNo { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Dob { get; set; }
}

public class CustomerUpdatePOSRequest
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

public class LoyaltyRetryDto
{
    public string? OrderNo { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
}

public class MemoryCacheConfig
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public string? MemoryCacheType { get; set; }
    public bool Blocked { get; set; }
    public string? ListStore { get; set; }
}

public class PaymentEntryLoyalty
{
    public int LineNo { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

public class TransLineLoyalty
{
    public int LineNo { get; set; }
    public int? ParentLineNo { get; set; }
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
    public int DiscountType { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? Size { get; set; }
    public string? CardLevel { get; set; }
    public string? DivisionCode { get; set; }
    public bool IsAward { get; set; } = true;
}

public class GiftCodeDto
{
    public string? GiftCode { get; set; }
    public string? Description { get; set; }
}

public class CouponValidTimeData
{
    public string? PosNo { get; set; }
    public string? MaterialNo { get; set; }
    public string? CouponCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class Gift_CouponValidTime
{
    public string? PosNo { get; set; }
    public string? MaterialNo { get; set; }
    public string? CouponCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? MemberCard { get; set; }
    public string? OrderNo { get; set; }
    public string? GiftCode { get; set; }
    public bool IsUsed { get; set; }
    public string? OrderNoUsed { get; set; }
    public bool IsSync { get; set; }
    public string? Message { get; set; }
    public DateTime CrtDate { get; set; }
}

public class LoggingLoyaltyDto
{
    public string? AppCode { get; set; }
    public string? OrderNo { get; set; }
    public string? MemberCardNo { get; set; }
    public string? ActionType { get; set; }
    public long LoyaltyPoints { get; set; }
    public string? Transaction { get; set; }
    public string? Status { get; set; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public DateTime CrtDate { get; set; }
    public string? OrigOrderNo { get; set; }
    public string? Items { get; set; }
    public string? CustName { get; set; }
    public int TransactionType { get; set; }
}

public class ExtendedFieldLoyalty
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? StoreCode { get; set; }
    public string? StoreMapping { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Desc { get; set; }
    public bool IsSuccess { get; set; }
    public string? CashierID { get; set; }
    public DateTime UpdateTime { get; set; }
    public bool Blocked { get; set; }
}

public class OtherStatusUpdate
{
    [Required]
    public string OtherStatus { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    public string? CashierID { get; set; }
}

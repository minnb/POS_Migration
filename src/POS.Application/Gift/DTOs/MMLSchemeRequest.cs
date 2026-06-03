using System.ComponentModel.DataAnnotations;

namespace POS.Application.Gift.DTOs;

public class MMLSchemeRequest
{
    [Required]
    public string PosNo { get; set; } = string.Empty;

    [Required]
    public string OrderNo { get; set; } = string.Empty;

    [Required]
    public string StoreNo { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    public string? MemberCardNo { get; set; }
    public string? UserId => MemberCardNo;
    public DateTime OrderTime { get; set; } = DateTime.Now;
    public bool IsMember { get; set; }
    public List<MMLSchemeItemsRequest> Items { get; set; } = new();
    public List<PaymentEntryLoyalty> Payments { get; set; } = new();
}

public class MMLSchemeItemsRequest
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? UOM { get; set; }
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? PackId { get; set; }
}

public class PaymentEntryLoyalty
{
    public int LineNo { get; set; }
    public string? TenderType { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

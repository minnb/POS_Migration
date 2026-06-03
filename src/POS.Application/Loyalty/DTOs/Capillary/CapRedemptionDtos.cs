namespace POS.Application.Loyalty.DTOs.Capillary;

// ── GET CouponIsRedemption (validation) ──────────────────────────────────────

public class CapRedemptionResponse
{
    public List<CapRedemptionData>? Redemption { get; set; }
    public CapRedemptionStatus? RedemptionStatus { get; set; }
    public CapRedemptionCustomer? Customer { get; set; }
    public List<object>? Warnings { get; set; }
}

public class CapRedemptionStatus
{
    public bool Status { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}

public class CapRedemptionData
{
    public bool CurrencyInput { get; set; }
    public int LocalToBaseCurrencyExchangeRate { get; set; }
    public bool IsRedeemable { get; set; }
    public List<CapRedemptionStatus>? Warnings { get; set; }
    public string? AppendedErrorMessage { get; set; }
    public string? Code { get; set; }
    public CapCouponSeriesData? Couponseries { get; set; }
    public bool IsAbsolute { get; set; }
    public int RedemptionsLeft { get; set; }
    public int NumberOfRedemptionsByUser { get; set; }
    public string? DiscountType { get; set; }
    public int DiscountValue { get; set; }
    public int DiscountUpto { get; set; }
}

public class CapCouponSeriesData
{
    public string? Description { get; set; }
    public string? DiscountCode { get; set; }
    public string? ValidTillDate { get; set; }
    public bool IsUpdateProductData { get; set; }
    public string? Info { get; set; }
    public int DiscountUpto { get; set; }
    public int DiscountValue { get; set; }
    public int MaxRedemptionsInSeriesPerUser { get; set; }
    public bool IssueToLoyalty { get; set; }
    public string? DiscountOn { get; set; }
    public string? DiscountType { get; set; }
    public bool UpdateProductData { get; set; }
    public string? ValidTilldateFormat { get; set; }
}

public class CapRedemptionCustomer
{
    public long Id { get; set; }
    public List<CapCustomerProfile2>? Profiles { get; set; }
}

public class CapCustomerProfile2
{
    public string? FirstName { get; set; }
    public List<CapIdentifier>? Identifiers { get; set; }
    public long UserId { get; set; }
    public string? AccountId { get; set; }
}

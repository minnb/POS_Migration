using POS.Common.Dtos.Capillary;

namespace POS.Common.Dtos.Capillary.Redemption;

public class RedemptionResponse
{
    public List<RedemptionData>? Redemption { get; set; }
    public RedemptionStatus? RedemptionStatus { get; set; }
    public RedemptionCustomer? Customer { get; set; }
    public List<object>? Warnings { get; set; }
}

public class RedemptionStatus
{
    public bool Status { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}

public class RedemptionData
{
    public bool CurrencyInput { get; set; }
    public int LocalToBaseCurrencyExchangeRate { get; set; }
    public bool IsRedeemable { get; set; }
    public List<RedemptionStatus>? Warnings { get; set; }
    public string? AppendedErrorMessage { get; set; }
    public string? Code { get; set; }
    public CouponSeriesData? Couponseries { get; set; }
    public bool IsAbsolute { get; set; }
    public int RedemptionsLeft { get; set; }
    public int NumberOfRedemptionsByUser { get; set; }
    public string? DiscountType { get; set; }
    public int DiscountValue { get; set; }
    public int DiscountUpto { get; set; }
}

public class CouponSeriesData
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
    public List<object>? EntityLevelRedemptionConfigsValues { get; set; }
    public List<object>? Brands { get; set; }
    public List<object>? Products { get; set; }
    public List<object>? Categories { get; set; }
}

public class RedemptionCustomer
{
    public long Id { get; set; }
    public List<CustomerProfiles>? Profiles { get; set; }
}

public class CustomerProfiles
{
    public string? FirstName { get; set; }
    public object? Fields { get; set; }
    public object? AllFields { get; set; }
    public List<IdentifiersCapillary>? Identifiers { get; set; }
    public List<object>? CommChannels { get; set; }
    public long UserId { get; set; }
    public string? AccountId { get; set; }
    public string? AutoUpdateTime { get; set; }
    public List<IdentifiersCapillary>? IdentifiersAll { get; set; }
}

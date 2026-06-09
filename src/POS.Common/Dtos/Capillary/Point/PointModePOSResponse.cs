using POS.Common.Dtos.Loyalty;

namespace POS.Common.Dtos.Capillary.Point;

public class LoggingLoyaltyData
{
    public string? AppCode { get; set; }
    public string? OrderNo { get; set; }
    public string? RefOrderNo { get; set; }
    public string? ActionType { get; set; }
    public string? TransactionId { get; set; }
    public string? MemberCardNo { get; set; }
    public string? HttpStatus { get; set; }
    public string? DataRequest { get; set; }
    public string? DataResponse { get; set; }
    public long PointsRedeem { get; set; }
    public long PointsEarn { get; set; }
    public int TransactionType { get; set; }
    public PointModePOSResponse? DataPoints { get; set; }
}

public class PointModePOSResponse
{
    public long? PointEarn { get; set; }
    public long? PointRedeem { get; set; }
    public long? RedemptionValue { get; set; }
    public long? Balance { get; set; }
    public long? CurrentRate { get; set; }
    public bool? IsOfflineVinID { get; set; }
    public string? EmpCode { get; set; }
    public bool? MasanerPackageInd { get; set; }
    public int? StaffPercentage { get; set; }
    public int? NormCustPercentage { get; set; }
    public string? RedemptionId { get; set; }
    public string? ReversalId { get; set; }
    public string? OrderNo { get; set; }
    public string? CreatedId { get; set; }
    public List<PointModePOSData>? ExtraEarnByCampaign { get; set; }
    public List<EarnLineItems>? TransLine { get; set; }
    public int StatusCode { get; set; }
}

public class PointModePOSData
{
    public string? LoyaltyMerchantId { get; set; }
    public decimal Amount { get; set; }
    public long EarnedPoints { get; set; }
    public string? EntityType { get; set; }
    public string? Type { get; set; }
}

public class EarnLineItems : TransLineLoyalty
{
    public long EarnPoints { get; set; }
    public bool IsPOS { get; set; }
    public string? PromoName { get; set; }
}

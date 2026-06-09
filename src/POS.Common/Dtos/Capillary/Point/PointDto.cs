using System.ComponentModel.DataAnnotations;
using POS.Common.Dtos.Capillary;
using POS.Common.Dtos.Capillary.Customer;

namespace POS.Common.Dtos.Capillary.Point;

public class PointRedeemableCapResponse
{
    public PointRedeemableDataCapResponse? Response { get; set; }
}

public class PointRedeemableDataCapResponse
{
    public StatusCodeCapillary? Status { get; set; }
    public PointsRedeemableCap? Points { get; set; }
}

public class PointsRedeemableCap
{
    public DataRedeemableCap? Redeemable { get; set; }
}

public class DataRedeemableCap
{
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? External_id { get; set; }
    public int Points { get; set; }
    public string? Is_redeemable { get; set; }
    public int Points_redeem_value { get; set; }
    public int Points_redeem_local_value { get; set; }
    public string? Input_type { get; set; }
    public string? Points_currency_ratio { get; set; }
    public ItemStatusCapillary? Item_status { get; set; }
}

public class PointsRedeemCapRequest
{
    public RootPointRedeemCap? Root { get; set; }
}

public class RootPointRedeemCap
{
    public List<RedeemCap>? Redeem { get; set; }
}

public class RedeemCap
{
    public string? Points_redeemed { get; set; }
    public string? Transaction_number { get; set; }
    public CustRedeemCap? Customer { get; set; }
}

public class CustRedeemCap
{
    public string? Mobile { get; set; }
}

public class PointRedeemPOSRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    public int PointRedeem { get; set; }
}

public class PointRedeemCapResponse
{
    public PointRedeemDataCapResponse? Response { get; set; }
}

public class PointRedeemDataCapResponse
{
    public StatusCodeCapillary? Status { get; set; }
    public PointsDataCapResponses? Responses { get; set; }
}

public class PointsDataCapResponses
{
    public PointsDataCap? Points { get; set; }
}

public class PointsDataCap
{
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? External_id { get; set; }
    public string? User_id { get; set; }
    public string? Redemption_id { get; set; }
    public string? Points_redeemed { get; set; }
    public string? Redemption_purpose { get; set; }
    public string? Redeemed_value { get; set; }
    public string? Redeemed_local_value { get; set; }
    public string? Balance { get; set; }
    public SideEffectRedeemCap? Side_effects { get; set; }
    public ItemStatusCapillary? Item_status { get; set; }
}

public class SideEffectRedeemCap
{
    public List<SideEffectRedeemDataCap>? Effect { get; set; }
}

public class SideEffectRedeemDataCap
{
    public long Id { get; set; }
    public string? Case_value { get; set; }
    public decimal Num_points { get; set; }
    public string? Validation_code { get; set; }
    public string? Points_redemption_summary_id { get; set; }
    public string? Redeemed_on_bill_number { get; set; }
    public int Redeemed_on_bill_id { get; set; }
    public string? Type { get; set; }
}

public class PointReverseCapResponse
{
    public long OrgId { get; set; }
    public IdentifiersCapillary? Identifier { get; set; }
    public long CustomerId { get; set; }
    public string? ReversalId { get; set; }
    public string? RedemptionId { get; set; }
    public decimal PointsToBeReversed { get; set; }
    public decimal PointsReversed { get; set; }
    public string Balance { get; set; } = string.Empty;
    public DataPointsReversedDetails? PointsReversedDetails { get; set; }
}

public class DataPointsReversedDetails
{
    public decimal Available { get; set; }
    public decimal Expired { get; set; }
}

public class PointReverseCapRequest
{
    public string? RedemptionId { get; set; }
    public long PointsToBeReversed { get; set; }
    public IdentifiersCapillary? Identifier { get; set; }
}

public class RevertTopUpPointsCapRequest
{
    public string? PointsToBeAdjusted { get; set; }
    public int ProgramId { get; set; }
    public string? PointCategoryTypes { get; set; }
    public string? ReasonOfReturn { get; set; }
}

public class RevertTopUpPointsPOSRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public long PointsToBeAdjusted { get; set; }
    public string? ReasonOfReturn { get; set; }
}

public class RevertPointsRedeemRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public string RedeemId { get; set; } = string.Empty;
    public long Points { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class PointReversePOSRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    [Required]
    public string RedemptionId { get; set; } = string.Empty;
    [Required]
    public int PointsToBeReversed { get; set; }
    public string? OrderNo { get; set; }
}

public class RevertPointsRedeemResponse
{
    public string? CustomerId { get; set; }
    public string? ReversalId { get; set; }
    public string? RedemptionId { get; set; }
    public decimal PointsToBeReversed { get; set; }
    public decimal PointsReversed { get; set; }
}

public class PointsHistoryResponse
{
    public List<LedgerEntry>? LedgerEntries { get; set; }
}

public class LedgerEntry
{
    public string? Store { get; set; }
    public string? StoreCode { get; set; }
    public int SourceProgramId { get; set; }
    public string? NetPointsOnEvent { get; set; }
    public LedgerTransactionDetails? TransactionDetails { get; set; }
    public string? SourceProgramName { get; set; }
}

public class LedgerTransactionDetails
{
    public long TransactionId { get; set; }
    public DateTime Date { get; set; }
    public string? TransactionNumber { get; set; }
    public string? Source { get; set; }
    public double GrossBillAmount { get; set; }
}

public class PointTopupCapillaryRequest
{
    public RootTopupCapillaryRequest? Root { get; set; }
}

public class PointTopupPOSRequest
{
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public long Points { get; set; }
    public string? Comments { get; set; }
}

public class RootTopupCapillaryRequest
{
    public List<RequestTopupCapillaryRequest>? Request { get; set; }
}

public class RequestTopupCapillaryRequest
{
    public CustomerTopupCapillary? Customer { get; set; }
    public string? Reason { get; set; }
    public string? Type { get; set; }
    public string? Base_type { get; set; }
    public string? Points { get; set; }
    public string? Comments { get; set; }
}

public class CustomerTopupCapillary
{
    public string? Mobile { get; set; }
}

public class ItemsTopUpCapillary
{
    public string? ItemNo { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public int Points { get; set; }
}

public class PointTopUpCapillaryResponse
{
    public PointTopUpDataCapResponse? Response { get; set; }
}

public class PointTopUpDataCapResponse
{
    public StatusCodeCapillary? Status { get; set; }
    public TopUpDataRootCapResponse? Requests { get; set; }
}

public class TopUpDataRootCapResponse
{
    public List<TopUpDataCapResponse>? Request { get; set; }
}

public class TopUpDataCapResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Requested_on { get; set; }
    public string? Type { get; set; }
    public string? Base_type { get; set; }
    public string? Reason { get; set; }
    public string? Comments { get; set; }
    public DataCustomerCapillaryResponse? Customer { get; set; }
    public ItemStatusCapillary? Item_status { get; set; }
}

public class RevertTopUpPointsCapResponse
{
    public string? Status { get; set; }
    public string? PointsAvailable { get; set; }
    public string? Message { get; set; }
    public List<object>? Warnings { get; set; }
}

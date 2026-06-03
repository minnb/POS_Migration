namespace POS.Application.Loyalty.DTOs.Capillary;

// ── POST RedeemPoints ────────────────────────────────────────────────────────

public class CapPointsRedeemRequest
{
    public CapRedeemRoot? Root { get; set; }
}

public class CapRedeemRoot
{
    public List<CapRedeemItem>? Redeem { get; set; }
}

public class CapRedeemItem
{
    public string? Points_redeemed { get; set; }
    public string? Transaction_number { get; set; }
    public CapRedeemCustomer? Customer { get; set; }
}

public class CapRedeemCustomer
{
    public string? Mobile { get; set; }
}

public class CapPointRedeemResponse
{
    public CapPointRedeemBody? Response { get; set; }
}

public class CapPointRedeemBody
{
    public CapStatusCode? Status { get; set; }
    public CapPointsDataResponses? Responses { get; set; }
}

public class CapPointsDataResponses
{
    public CapPointsData? Points { get; set; }
}

public class CapPointsData
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
    public CapSideEffectRedeem? Side_effects { get; set; }
    public CapItemStatus? Item_status { get; set; }
}

public class CapSideEffectRedeem
{
    public List<CapSideEffectRedeemData>? Effect { get; set; }
}

public class CapSideEffectRedeemData
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

// ── POST PointReverse ────────────────────────────────────────────────────────

public class CapPointReverseRequest
{
    public string? RedemptionId { get; set; }
    public long PointsToBeReversed { get; set; }
    public CapIdentifier? Identifier { get; set; }
}

public class CapPointReverseResponse
{
    public long OrgId { get; set; }
    public CapIdentifier? Identifier { get; set; }
    public long CustomerId { get; set; }
    public string? ReversalId { get; set; }
    public string? RedemptionId { get; set; }
    public decimal PointsToBeReversed { get; set; }
    public decimal PointsReversed { get; set; }
    public string Balance { get; set; } = string.Empty;
    public CapPointsReversedDetails? PointsReversedDetails { get; set; }
}

public class CapPointsReversedDetails
{
    public decimal Available { get; set; }
    public decimal Expired { get; set; }
}

// ── GET PointsRedeemable ─────────────────────────────────────────────────────

public class CapPointRedeemableResponse
{
    public CapPointRedeemableBody? Response { get; set; }
}

public class CapPointRedeemableBody
{
    public CapStatusCode? Status { get; set; }
    public CapPointsRedeemable? Points { get; set; }
}

public class CapPointsRedeemable
{
    public CapRedeemableData? Redeemable { get; set; }
}

public class CapRedeemableData
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
    public CapItemStatus? Item_status { get; set; }
}

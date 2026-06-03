namespace POS.Application.Loyalty.DTOs.Capillary;

// ── POST /api/v2/transaction (Add) ──────────────────────────────────────────

public class CapAddTransactionRequest
{
    public string? Type { get; set; }
    public string? BillNumber { get; set; }
    public int Discount { get; set; }
    public decimal BillAmount { get; set; }
    public decimal GrossAmount => BillAmount + Discount;
    public string? BillingDate { get; set; }
    public List<CapPaymentMode>? PaymentModes { get; set; }
    public CapTransactionExtendedFields? ExtendedFields { get; set; }
    public List<CapLineItem>? LineItemsV2 { get; set; }
    public CapIdenModeField? CustomFields { get; set; }
}

public class CapAddTransactionPosRequest : CapAddTransactionRequest
{
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? MemberCard { get; set; }
    public int TransactionType { get; set; }
    public string? OrderChannel { get; set; }
    public bool IsMobile { get; set; }
}

public class CapLineItem
{
    public string? ItemCode { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int Discount { get; set; }
    public decimal Rate { get; set; }
    public decimal Qty { get; set; }
    public long Serial { get; set; }
    public decimal Value => Rate * Qty;
    public CapLineItemExtendedFields? ExtendedFields { get; set; }
}

public class CapReturnLineItem
{
    public string? ItemCode { get; set; }
    public string? Description { get; set; }
    public int Amount { get; set; }
    public int Discount { get; set; }
    public int Rate { get; set; }
    public decimal Qty { get; set; }
    public decimal Value => Rate * Qty;
    public string? Type { get; set; }
    public string? ReturnType { get; set; }
}

public class CapTransactionExtendedFields
{
    public DateTime Delivery_date { get; set; }
    public decimal Amount_excluding_tax { get; set; }
    public decimal Tax_amount { get; set; }
    public string? Store_associate_id { get; set; }
    public string? Order_channel { get; set; }
    public string? Order_date_time { get; set; }
    public decimal Delivery_charges { get; set; }
    public string? Cashier_id { get; set; }
    public string? App_mode { get; set; }
    public string? Dist_code { get; set; }
}

public class CapLineItemExtendedFields
{
    public string? Size { get; set; }
    public string? Uuid { get; set; }
    public string? Pos_line_no { get; set; }
    public string? Lineitem_brand_type { get; set; }
    public string? Discount_description { get; set; }
}

public class CapPaymentMode
{
    public string? Mode { get; set; }
    public int Value { get; set; }
    public CapPaymentModeAttributes? Attributes { get; set; }
}

public class CapPaymentModeAttributes
{
    public string? Card_type { get; set; }
}

// ── Return transaction ───────────────────────────────────────────────────────

public class CapReturnFullTransactionRequest
{
    public string? Type { get; set; }
    public string? ReturnType { get; set; }
    public string? BillNumber { get; set; }
    public string? BillingDate { get; set; }
    public int BillAmount { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? Note { get; set; }
}

public class CapReturnPartialTransactionRequest
{
    public string? Type { get; set; }
    public string? ReturnType { get; set; }
    public string? BillNumber { get; set; }
    public string? BillingDate { get; set; }
    public int BillAmount { get; set; }
    public List<CapReturnLineItem>? LineItemsV2 { get; set; }
    public List<CapPaymentMode>? PaymentModes { get; set; }
    public string? Note { get; set; }
}

// ── AddTransaction Response ──────────────────────────────────────────────────

public class CapAddTransactionResponse
{
    public long CreatedId { get; set; }
    public List<object>? Warnings { get; set; }
    public List<CapRawSideEffect>? RawSideEffects { get; set; }
    public CapPointsCustomer? Customer { get; set; }
}

public class CapPointsCustomer
{
    public long ProgramId { get; set; }
    public long TotalPoints { get; set; }
    public long MainPoints { get; set; }
    public long PromisedPoints { get; set; }
}

public class CapRawSideEffect
{
    public string? AwardOn { get; set; }
    public string? AwardedOn { get; set; }
    public string? BillNumber { get; set; }
    public List<CapLineItemId>? LineItemId { get; set; }
    public string? Points { get; set; }
    public string? Quantity { get; set; }
    public string? SourceType { get; set; }
    public string? SourceValue { get; set; }
    public string? Type { get; set; }
    public string? ProgramId { get; set; }
    public string? PromoId { get; set; }
    public string? PromoName { get; set; }
}

public class CapLineItemId
{
    public string? Id { get; set; }
    public string? ItemCode { get; set; }
    public decimal Amount { get; set; }
    public decimal Qty { get; set; }
    public CapLineItemExtendedFields? ExtendedFields { get; set; }
}

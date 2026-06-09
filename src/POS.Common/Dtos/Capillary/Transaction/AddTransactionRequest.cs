using POS.Common.Dtos.Capillary;

namespace POS.Common.Dtos.Capillary.Transaction;

public class AddTransactionCapPOSRequest : AddTransactionCapillaryRequest
{
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? MemberCard { get; set; }
    public int TransactionType { get; set; }
    public string? OrderChannel { get; set; }
    public bool IsMobile { get; set; } = false;
}

public class ReturnFullTransactionCapillaryRequest
{
    public string? Type { get; set; }
    public string? ReturnType { get; set; }
    public string? BillNumber { get; set; }
    public string? BillingDate { get; set; }
    public int BillAmount { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? Note { get; set; }
}

public class ReturnPartialTransactionCapillaryRequest
{
    public string? Type { get; set; }
    public string? ReturnType { get; set; }
    public string? BillNumber { get; set; }
    public string? BillingDate { get; set; }
    public int BillAmount { get; set; }
    public List<ReturnLineItemsCapillary>? LineItemsV2 { get; set; }
    public List<PaymentModesCapillary>? PaymentModes { get; set; }
    public string? Note { get; set; }
}

public class AddTransactionCapillaryRequest
{
    public string? Type { get; set; }
    public string? BillNumber { get; set; }
    public int Discount { get; set; }
    public decimal BillAmount { get; set; }
    public decimal GrossAmount => BillAmount + Discount;
    public string? BillingDate { get; set; }
    public List<PaymentModesCapillary>? PaymentModes { get; set; }
    public TransactionExtendedFields? ExtendedFields { get; set; }
    public List<LineItemsCapillary>? LineItemsV2 { get; set; }
    public Iden_mode_field? CustomFields { get; set; }
}

public class LineItemsCapillary
{
    public string? ItemCode { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int Discount { get; set; }
    public decimal Rate { get; set; }
    public decimal Qty { get; set; }
    public long Serial { get; set; }
    public decimal Value => Rate * Qty;
    public LineItemsExtendedFields? ExtendedFields { get; set; }
}

public class ReturnLineItemsCapillary
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

public class TransactionExtendedFields
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

public class PaymentModesCapillary
{
    public string? Mode { get; set; }
    public int Value { get; set; }
    public AttributesCardTypeCapillary? Attributes { get; set; }
}

public class AttributesCardTypeCapillary
{
    public string? Card_type { get; set; }
}

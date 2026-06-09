using POS.Common.Dtos.Capillary;
using POS.Common.Dtos.Capillary.Transaction;

namespace POS.Common.Dtos.Capillary.Enosta;

public class TransactionReturnResponse
{
    public int Status { get; set; }
    public EnostaResponse? Data { get; set; }
}

public class EnostaResponse
{
    public DataEnostaResponse? Response { get; set; }
}

public class DataEnostaResponse
{
    public ItemStatusCapillary? Status { get; set; }
    public CustomerEnostaResponse? Customer { get; set; }
}

public class CustomerEnostaResponse
{
    public string? Firstname { get; set; }
    public string? User_id { get; set; }
    public string? Mobile { get; set; }
    public string? External_id { get; set; }
    public EnostaTransaction? Transactions { get; set; }
}

public class EnostaTransaction
{
    public List<EnostaTransactionInfo>? Transaction { get; set; }
}

public class EnostaTransactionInfo
{
    public string? Id { get; set; }
    public string? Number { get; set; }
    public string? Type { get; set; }
    public string? Amount { get; set; }
    public string? Notes { get; set; }
    public string? Billing_time { get; set; }
    public string? Gross_amount { get; set; }
    public string? Discount { get; set; }
    public string? Store_code { get; set; }
    public EnostaLineItems? Line_items { get; set; }
}

public class EnostaLineItems
{
    public List<EnostaLineItemV2>? Line_item { get; set; }
}

public class EnostaLineItemV2
{
    public string? Id { get; set; }
    public string? Issued { get; set; }
    public string? Item_code { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int Discount { get; set; }
    public decimal Rate { get; set; }
    public decimal Qty { get; set; }
    public long Serial { get; set; }
    public decimal Value { get; set; }
    public LineItemsExtendedFields? ExtendedFields { get; set; }
    public string? Base_points_returned { get; set; }
}

using POS.Common.Dtos.Capillary;

namespace POS.Common.Dtos.Capillary.Transaction;

public class TransactionDetailCapillaryResponse
{
    public PaginationCapillary? Pagination { get; set; }
    public List<TransactionDetailCapillary>? Data { get; set; }
}

public class TransactionDetailCapillary
{
    public BillDetailsCap? BillDetails { get; set; }
    public List<LineItemsCap>? LineItems { get; set; }
}

public class BillDetailsCap
{
    public decimal Amount { get; set; }
    public string? BillNumber { get; set; }
    public decimal GrossAmount { get; set; }
}

public class LineItemsCap
{
    public long Id { get; set; }
    public LineItemDetailCap? Details { get; set; }
}

public class LineItemDetailCap
{
    public int Points { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public decimal Discount { get; set; }
    public string? ItemCode { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public long Serial { get; set; }
    public decimal Value { get; set; }
    public bool Returnable { get; set; }
    public int ReturnableDays { get; set; }
    public LineItemsExtendedFields? ExtendedFields { get; set; }
}

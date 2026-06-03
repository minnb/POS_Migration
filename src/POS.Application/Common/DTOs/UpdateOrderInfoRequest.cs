namespace POS.Application.Common.DTOs;

public class UpdateOrderInfoRequest
{
    public OrderHeader? Header { get; set; }
    public List<OrderLine> ListLine { get; set; } = new();
    public List<LineBluePoint>? ListLineBluePoint { get; set; }
    public List<LineDiscountEntry>? ListLineDiscountEntry { get; set; }
    public List<TransPaymentEntryModel>? ListTransPaymentEntry { get; set; }
}

public class OrderHeader
{
    public string? OrderNo { get; set; }
    public double MemberPointsRedeemReturned { get; set; }
    public double WinPayAmountReturned { get; set; }
}

public class OrderLine
{
    public double QtyDiscountReturned { get; set; }
    public double ReturnedQuantity { get; set; }
    public double DiscountQuantity { get; set; }
    public string? DocumentNo { get; set; }
    public int LineNo { get; set; }
}

public class LineBluePoint
{
    public string? OrderNo { get; set; }
    public int LineNo { get; set; }
    public double ExtraQuantityNotEarnReturned { get; set; }
}

public class LineDiscountEntry
{
    public string? OrderNo { get; set; }
    public int LineNo { get; set; }
    public double ReturnedQuantity { get; set; }
}

public class TransPaymentEntryModel
{
    public string? OrderNo { get; set; }
    public int LineNo { get; set; }
    public int TransactionNo { get; set; }
}

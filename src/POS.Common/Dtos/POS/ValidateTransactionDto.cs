namespace POS.Common.Dtos.POS;

public class Cash_OrderNoSentToEInvoice
{
    public string? CompTaxCode { get; set; }
    public string? Key { get; set; }
    public string? OrderNo { get; set; }
    public bool IsEOD { get; set; }
}

public class ValidateTransactionDto
{
    public string? StoreNo { get; set; }
    public string? StoreName { get; set; }
    public string? Address { get; set; }
    public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime OrderTime { get; set; }
    public string? CustomerName { get; set; }
    public string? MemberCardNo { get; set; }
    public int? MemberPointsEarn { get; set; }
    public int TransactionType { get; set; }
    public int DeliveringMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public int SalesIsReturn { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public string? RefKey1 { get; set; }
    public string? CQT { get; set; }
    public bool IsEOD { get; set; }
    public string? Key { get; set; }
    public string? CompTaxCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<ValidateTransactionLine>? TransLine { get; set; }
}

public class ValidateTransactionLine
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public int VATPercent { get; set; }
    public decimal VATAmount { get; set; }
    public string? DivisionCode { get; set; }
    public string? Barcode { get; set; }
}

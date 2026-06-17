namespace POS.Common.Dtos.CentralSale;

public sealed class TransactionListDto
{
    public string   OrderNo         { get; set; } = string.Empty;
    public DateTime OrderDate       { get; set; }
    public DateTime OrderTime       { get; set; }
    public string   StoreNo         { get; set; } = string.Empty;
    public string   POSTerminalNo   { get; set; } = string.Empty;
    public decimal  DiscountAmount  { get; set; }
    public decimal  AmountInclVAT   { get; set; }
    public int      TransactionType { get; set; }
    public DateTime CreatedDate     { get; set; }
    public string?  MemberCardNo    { get; set; }
}

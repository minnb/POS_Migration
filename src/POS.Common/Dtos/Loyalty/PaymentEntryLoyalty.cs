namespace POS.Common.Dtos.Loyalty;

public class PaymentEntryLoyalty
{
    public int LineNo { get; set; }
    public string TenderType { get; set; } = string.Empty;
    public decimal AmountTendered { get; set; }
    public string? CardType { get; set; }
}

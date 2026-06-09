namespace POS.Common.Dtos.WinMoney;

public class WinMoneyConversion
{
    public string? PhoneNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? CashierID { get; set; }
    public DateTime UpdateTime { get; set; }
}

namespace VCM.POSBLUE.Shared.DTOs;

// Read/write model cho LoyaltyRepository (giữ nguyên field từ bản cũ).

public class LoggingLoyaltyDto
{
    public string? AppCode { get; set; }
    public string? OrderNo { get; set; }
    public string? MemberCardNo { get; set; }
    public string? ActionType { get; set; }
    public long LoyaltyPoints { get; set; }
    public string? Transaction { get; set; }
    public string? Status { get; set; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public DateTime CrtDate { get; set; }
    public string? OrigOrderNo { get; set; }
    public string? Items { get; set; }
    public string? CustName { get; set; }
    public int TransactionType { get; set; }
}

public class GiftCodeDto
{
    public string? GiftCode { get; set; }
    public string? Description { get; set; }
}

public class WinMoneyConversion
{
    public string? PhoneNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? CashierID { get; set; }
    public DateTime UpdateTime { get; set; }
}

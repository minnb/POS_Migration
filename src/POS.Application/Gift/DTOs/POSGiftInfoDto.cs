namespace POS.Application.Gift.DTOs;

public class POSGiftInfoDto
{
    public string GiftCode { get; set; } = string.Empty;
    public string? GiftType { get; set; }
    public int GiftValue { get; set; }
    public string GiftStatus { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PosCreated { get; set; }
    public DateTime? CreatedTime { get; set; }
    public string? PosUsed { get; set; }
    public DateTime? UsedTime { get; set; }
}

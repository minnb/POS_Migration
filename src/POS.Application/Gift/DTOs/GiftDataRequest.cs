using System.ComponentModel.DataAnnotations;

namespace POS.Application.Gift.DTOs;

public class GiftDataRequest
{
    [Required]
    public string PosNo { get; set; } = string.Empty;

    [Required]
    public string[] GiftCode { get; set; } = Array.Empty<string>();

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? GiftType { get; set; }

    public string StoreNo => PosNo.Length >= 4 ? PosNo[..4] : PosNo;
}

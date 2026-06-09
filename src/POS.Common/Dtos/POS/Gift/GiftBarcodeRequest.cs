using System.ComponentModel.DataAnnotations;
using POS.Common.Dtos.POS;

namespace POS.Common.Dtos.POS.Gift;

public class GiftDataRequest : POSRequest
{
    [Required]
    public string[] GiftCode { get; set; } = [];
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? GiftType { get; set; }
}

public class GiftDataResponse
{
    public List<GiftDataRespone>? GiftData { get; set; }
}

public class GiftDataRespone
{
    public string? GiftCode { get; set; }
    public string? GiftStatus { get; set; }
    public string? PosUsed { get; set; }
    public DateTime TimeUsed { get; set; }
}

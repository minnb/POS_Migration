using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Loyalty.WinScore;

public class TokenWinscore
{
    public string? Token_type { get; set; }
    public int Expires_in { get; set; }
    public int Ext_expires_in { get; set; }
    public string? Access_token { get; set; }
}

public class UpdateStatusWinscoreDto
{
    public string? PhoneNumber { get; set; }
    public int Status { get; set; }
    public string? PosNo { get; set; }
}

public class CheckStatusWinscore
{
    public int Is_qualified { get; set; }
    public string? Display_message { get; set; }
}

public class UpdateStatusWinscore
{
    public string? win_membership { get; set; }
    public string? pos_id { get; set; }
    public string? store_id { get; set; }
    public string? updated_status { get; set; }
}

public class UpdateStatusWinscorePOS
{
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public int Status { get; set; }
}

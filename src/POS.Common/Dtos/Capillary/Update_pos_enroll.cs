using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Capillary;

public class Update_pos_enroll
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string Mobile_enroll { get; set; } = string.Empty;
}

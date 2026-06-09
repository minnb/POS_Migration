using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.POS;

public class POSRequest
{
    [Required]
    [DefaultValue("201801")]
    [StringLength(6, MinimumLength = 6)]
    public string PosNo { get; set; } = "201801";
    public string StoreNo => PosNo.Length >= 4 ? PosNo[..4] : PosNo;
}

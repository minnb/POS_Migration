using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Api.Models.Gift;

public class MMLSchemeRequest
{
    // Replicate the fields used in the legacy controller (subset needed for stub).
    // Adjust as needed when real implementation is added.
    [Required]
    public string StoreNo { get; set; }
    [Required]
    public string PosNo { get; set; }
    public string Code { get; set; }
    public string OrderNo { get; set; }
    public string PhoneNumber { get; set; }
    public string RefCode { get; set; }
    public int Amount { get; set; }
    public string OperationId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.MSN;

public class OfferStaffSetupDto
{
    public string? ClubCode { get; set; }
    public DateTime ApplyFrom { get; set; }
    public DateTime ValidBefore { get; set; }
    public bool IsMonth { get; set; }
    public bool Blocked { get; set; }
    public decimal InitQuota { get; set; }
    public string? Description { get; set; }
}

public class OfferStaffTransactionRequest
{
    [Required]
    public string ClubCode { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string StaffCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    [Required]
    public int TransactionType { get; set; }
    public string? ReturnedOrderNo { get; set; }
}

public class OfferStaffTransactionDto
{
    public string? ClubCode { get; set; }
    public string? Month { get; set; }
    public decimal InitQuota { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StaffCode { get; set; }
    public decimal Amount { get; set; }
    public int TransactionType { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public DateTime CrtDate { get; set; }
}

public class CheckOfferStaffRemn : OfferStaffRemnDto
{
    public string? PosNo { get; set; }
    public DateTime ExpiryTime { get; set; }
}

public class OfferStaffRemnDto
{
    public string? ClubCode { get; set; }
    public string? Month { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StaffCode { get; set; }
    public decimal InitQuota { get; set; }
    public decimal RemainQuota { get; set; }
    public DateTime CrtDate { get; set; }
    public DateTime ChgDate { get; set; }
}

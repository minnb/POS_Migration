using POS.Common.Dtos.Loyalty.MemberBusiness;

namespace POS.Common.Dtos.Loyalty;

public class InfoMemberModel
{
    public string? CardNumber { get; set; }
    public string? VirtualCard { get; set; }
    public string? CMND { get; set; }
    public string? MemberName { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CardLevel { get; set; }
    public long? MemberPoint { get; set; }
    public long? TotalPoint { get; set; }
    public long? RedemptionValue { get; set; }
    public int? CurrentRate { get; set; }
    public string? MemberCSN { get; set; }
    public string? OtherInfo { get; set; }
    public string? QRCode { get; set; }
    public bool? ExtraPoint { get; set; }
    public bool IsRedeem { get; set; }
    public bool? IsOfflineVinID { get; set; }
    public bool IsShowMessage { get; set; }
    public string? Status { get; set; }
    public string? System { get; set; }
    public string? ClubCode { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Dob { get; set; }
    public bool? BirthdayGiftInd { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ExternalId { get; set; }
    public string? OtherStatus { get; set; }
    public string? MemberType { get; set; }
    public string? Source { get; set; }
    // TODO: type to specific DTO when Capillary/CX flows are migrated
    public List<object>? ExtendedFields { get; set; }
    public List<object>? AvailablePromotion { get; set; }
    public MemberBusinessData? MemberBusiness { get; set; }
    public List<object>? PointsSummaries { get; set; }
}

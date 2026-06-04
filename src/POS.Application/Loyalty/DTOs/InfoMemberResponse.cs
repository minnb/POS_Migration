namespace POS.Application.Loyalty.DTOs;

// Khớp 100% với InfoMemberModel cũ — không đổi tên field.
// Serializer dùng DefaultContractResolver (PascalCase) → JSON field names giữ nguyên.
public class InfoMemberResponse
{
    public string CardNumber { get; set; } = string.Empty;
    public string VirtualCard { get; set; } = string.Empty;
    public string CMND { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CardLevel { get; set; } = string.Empty;
    public long MemberPoint { get; set; }
    public long TotalPoint { get; set; }
    public long RedemptionValue { get; set; }
    public int CurrentRate { get; set; }
    public string MemberCSN { get; set; } = string.Empty;
    public string OtherInfo { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public bool ExtraPoint { get; set; }
    public bool IsRedeem { get; set; }
    public bool IsOfflineVinID { get; set; }
    public bool IsShowMessage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string ClubCode { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Dob { get; set; } = string.Empty;
    public bool BirthdayGiftInd { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? OtherStatus { get; set; }
    public string MemberType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<ExtendedFieldItem> ExtendedFields { get; set; } = new();
    public List<BLUEAvailablePromotion> AvailablePromotion { get; set; } = new();
    public object? MemberBusiness { get; set; }
    public List<object> PointsSummaries { get; set; } = new();
}

public class ExtendedFieldItem
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class BLUEAvailablePromotion
{
    public string MerchantId { get; set; } = string.Empty;
    public string WinCode { get; set; } = string.Empty;
    public string PromotionCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public int InitQuota { get; set; }
    public int RemainQuota { get; set; }
    public string? Serial { get; set; }
}

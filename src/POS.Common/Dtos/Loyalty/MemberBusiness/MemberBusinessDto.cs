namespace POS.Common.Dtos.Loyalty.MemberBusiness;

public class MemberBusiness
{
    public string? MemberCard { get; set; }
    public string? MemberStore { get; set; }
    public string? StoreNo { get; set; }
    public string? DistcountType { get; set; }
    public decimal DistcountValue { get; set; }
    public string? CardLevel { get; set; }
    public bool Blocked { get; set; }
    public string? Description { get; set; }
    public DateTime CrtDate { get; set; }
}

public class CheckMemberBusinessData
{
    public bool IsChecked { get; set; }
    public string? Description { get; set; }
    public MemberBusinessData? MemberBusinessData { get; set; }
}

public class MemberBusinessData
{
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? MemberCard { get; set; }
    public string? MemberStore { get; set; }
    public bool IsDiscount { get; set; }
    public string? Key { get; set; }
    public ExistsProcessing? ExistsProcessing { get; set; }
    public List<MemberBusinessItemQuota>? Items { get; set; }
}

public class MemberBusinessItemQuota
{
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal MaxValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemnValue { get; set; }
}

public class ExistsProcessing
{
    public string? PosNo { get; set; }
    public string? KeyExists { get; set; }
}

public class MemberBusinessItemDto
{
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? MemberCard { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal MaxValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemnValue { get; set; }
}

public class MemberItemDiscount
{
    public string? CardLevel { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? Uom { get; set; }
    public string? Month { get; set; }
    public decimal MaxValue { get; set; }
    public bool Blocked { get; set; }
    public DateTime CrtDate { get; set; }
}

public class MemberRemnItem
{
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? MemberCard { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal MaxValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemnValue { get; set; }
    public string? OrderNo { get; set; }
    public long Id { get; set; }
    public string? Type { get; set; }
    public DateTime CrtDate { get; set; }
}

public class MemberRemnItemGroupBy
{
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? MemberCard { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal UsedValue { get; set; }
}

public class ValidateMemberBusiness
{
    [System.ComponentModel.DataAnnotations.Required]
    public string StoreNo { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string PosNo { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string MemberCard { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string Key { get; set; } = string.Empty;
}

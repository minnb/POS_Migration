namespace POS.Common.Dtos.RptLoyalty;

public sealed class InvoiceLoyaltyDto
{
    public string   OrderNo         { get; set; } = string.Empty;
    public int      TransactionType { get; set; }
    public string?  OrigOrderNo     { get; set; }
    public string   MemberCardNo    { get; set; } = string.Empty;
    public string?  StoreNo         { get; set; }
    public string   ActionType      { get; set; } = string.Empty;
    public long     LoyaltyPoints   { get; set; }
    public DateTime CrtDate         { get; set; }
    public DateTime? OrderTime      { get; set; }
    public string   Transaction     { get; set; } = string.Empty;

    // COUNT(*) OVER() từ query phân trang server-side — không phải cột nghiệp vụ.
    public int Total { get; set; }

    public string MemberCardMasked =>
        string.IsNullOrEmpty(MemberCardNo) ? string.Empty
        : MemberCardNo.Length <= 4 ? MemberCardNo
        : new string('*', MemberCardNo.Length - 4) + MemberCardNo[^4..];
}

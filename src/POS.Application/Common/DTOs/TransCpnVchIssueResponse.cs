namespace POS.Application.Common.DTOs;

public class TransCpnVchIssueResponse
{
    public string? ArticleNo { get; set; }
    public string? VoucherType { get; set; }
    public int MaxQtyUse { get; set; }
    public int QtyUse { get; set; }
}

namespace POS.Common.Dtos.Giftee;

public class GTStatusVoucherRequest
{
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? SeriNo { get; set; }
}

public class StatusReferenceResponse
{
    public string? code { get; set; }
    public int available_begin { get; set; }
    public int available_end { get; set; }
    public string? status { get; set; }
    public int issued_at { get; set; }
    public int? exchanged_at { get; set; }
    public string? exchanged_shop_code { get; set; }
    public int? disabled_at { get; set; }
    public List<StatusHistoryRepsonse>? histories { get; set; }
}

public class StatusHistoryRepsonse
{
    public string? terminal_code { get; set; }
    public string? exchanged_shop_code { get; set; }
    public string? request_date { get; set; }
    public string? cancel_code { get; set; }
    public string? operation_kind { get; set; }
    public int? actioned_at { get; set; }
    public int? disabled_at { get; set; }
}

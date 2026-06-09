namespace POS.Common.Dtos.Reward;

public class RewardCodeRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? OrderNo { get; set; }
    public DateTime BussinessDate { get; set; }
    public string? OfferNo { get; set; }
    public string? IPServer { get; set; }
}

public class RewardCodeSendModel
{
    public string? Code { get; set; }
    public string? OfferNo { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Description { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsReward { get; set; }
    public string? SubDesc { get; set; }
}

namespace POS.Common.Dtos.LogService;

public class LogServiceModel
{
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? RequestPOS { get; set; }
    public string? RequestPartner { get; set; }
    public string? ResponsPOS { get; set; }
    public string? ResponsePartner { get; set; }
    public string? EndPointPartner { get; set; }
    public string? MethodPartner { get; set; }
    public string? ActionController { get; set; }
    public int TimeResponse { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class PromotionStaffRefundPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RefundDate { get; set; }
    public float Amount { get; set; }
    public string? OrderNo { get; set; }
}

public class PromotionStaffRedeemPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RedeemDate { get; set; }
    public float Amount { get; set; }
    public string? OrderNo { get; set; }
}

public class PromotionStaffRedeemPartnerRequest
{
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public DateTime RedeemDate { get; set; }
    public float Amount { get; set; }
    public string? RefCode { get; set; }
}

public class PromotionStaffResponse
{
    public int Status { get; set; }
    public string? Description { get; set; }
    public int TotalItems { get; set; }
    public string? TechMsg { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

public class ForceCheckResponse
{
    public string? StaffCode { get; set; }
    public string? ApplyFrom { get; set; }
    public string? ValidBefore { get; set; }
    public float InitQuota { get; set; }
    public float RemainQuota { get; set; }
}

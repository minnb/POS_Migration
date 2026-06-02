using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

public interface IPromotionPartnerClient
{
    Task<(int StatusCode, PromotionStaffResponse? Response)> ForceCheckAsync(string staffCode);
    Task<(int StatusCode, PromotionStaffResponse? Response)> CheckAsync(string staffCode, string quotaCode);
    Task<(int StatusCode, PromotionStaffResponse? Response)> RedeemAsync(PromotionStaffRedeemPartnerRequest request);
    Task<(int StatusCode, PromotionStaffResponse? Response)> RefundAsync(PromotionStaffRedeemPartnerRequest request);
}

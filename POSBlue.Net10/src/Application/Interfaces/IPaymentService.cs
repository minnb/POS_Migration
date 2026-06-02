using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use cases cho PaymentController (api/v2/partner/*):
/// voucher check/update-status (UrBox, GotIt, OneU)
/// và coupon check/redeem/list/detail/re-active (Capillary).
/// </summary>
public interface IPaymentService
{
    // ── Voucher ───────────────────────────────────────────────────────────────
    /// <summary>POST api/v2/partner/voucher/check — kiểm tra voucher theo partner (URBOX/GOTIT/ONEU).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ValidateVoucherAsync(
        CheckVoucherPartnerPOSRequest model);

    /// <summary>POST api/v2/partner/voucher/update-status — sử dụng voucher theo partner (URBOX/GOTIT/ONEU).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdateStatusVoucherAsync(
        UpdateStatusVoucherPartnerRequest model);

    // ── Coupon (Capillary) ────────────────────────────────────────────────────
    /// <summary>POST api/v2/partner/coupon/check — kiểm tra coupon Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ValidateCouponAsync(
        CheckVoucherPartnerPOSRequest model);

    /// <summary>POST api/v2/partner/coupon/redeem — sử dụng coupon Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CouponRedeemAsync(
        UpdateStatusVoucherPartnerRequest model);

    /// <summary>GET api/v2/partner/coupon/list/user — lấy danh sách coupon của user từ Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetCouponByUserAsync(
        string partner, string storeNo, string mobile, string status);

    /// <summary>GET api/v2/partner/coupon/detail — lấy chi tiết coupon từ Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetCouponDetailAsync(
        string partner, string storeNo, string serialNo);

    /// <summary>POST api/v2/partner/coupon/re-active — kích hoạt lại coupon Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ReActiveCouponAsync(
        ReActiveCouponRequest request);
}

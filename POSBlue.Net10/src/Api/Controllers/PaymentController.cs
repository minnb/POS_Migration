using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Partner payment controller — giữ NGUYÊN routes cũ: api/v2/partner/*.
/// Thin delegate → IPaymentService.
/// Partners: URBOX, GOTIT, ONEU (voucher); CAP (coupon).
/// </summary>
[Route("api/v2/partner")]
public class PaymentController : BaseController
{
    private readonly IPaymentService _payment;

    public PaymentController(IPaymentService payment) => _payment = payment;

    // ── Voucher check / update-status ─────────────────────────────────────────

    [HttpPost("voucher/check")]
    public async Task<IActionResult> ValidateVoucher([FromBody] CheckVoucherPartnerPOSRequest model)
    {
        if (!ModelState.IsValid)
            return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
        try
        {
            var (status, message, data) = await _payment.ValidateVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Exception:{ex.Message}", null, ex.Message);
        }
    }

    [HttpPost("voucher/update-status")]
    public async Task<IActionResult> UpdateStatusVoucher([FromBody] UpdateStatusVoucherPartnerRequest model)
    {
        if (!ModelState.IsValid)
            return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
        try
        {
            var (status, message, data) = await _payment.UpdateStatusVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    // ── Coupon (Capillary) ────────────────────────────────────────────────────

    [HttpPost("coupon/check")]
    public async Task<IActionResult> ValidateCoupon([FromBody] CheckVoucherPartnerPOSRequest model)
    {
        if (!ModelState.IsValid)
            return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
        try
        {
            var (status, message, data) = await _payment.ValidateCouponAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi Exception:{ex.Message}", null, ex.Message);
        }
    }

    [HttpPost("coupon/redeem")]
    public async Task<IActionResult> CouponRedeem([FromBody] UpdateStatusVoucherPartnerRequest model)
    {
        if (!ModelState.IsValid)
            return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
        try
        {
            var (status, message, data) = await _payment.CouponRedeemAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("coupon/list/user")]
    public async Task<IActionResult> GetCouponByUser(
        [FromQuery] string partner, [FromQuery] string storeNo,
        [FromQuery] string mobile, [FromQuery] string status)
    {
        if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(mobile)
            || string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(status))
            return ApiResult(HttpStatusCode.BadRequest, "Tham số truyền vào không đúng", null);
        try
        {
            var (httpStatus, message, data) = await _payment.GetCouponByUserAsync(partner, storeNo, mobile, status);
            return ApiResult(httpStatus, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("coupon/detail")]
    public async Task<IActionResult> GetCouponDetail(
        [FromQuery] string partner, [FromQuery] string storeNo, [FromQuery] string serialNo)
    {
        if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(serialNo) || string.IsNullOrEmpty(storeNo))
            return ApiResult(HttpStatusCode.BadRequest, "Tham số truyền vào không đúng", null);
        try
        {
            var (httpStatus, message, data) = await _payment.GetCouponDetailAsync(partner, storeNo, serialNo);
            return ApiResult(httpStatus, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpPost("coupon/re-active")]
    public async Task<IActionResult> ReActiveCoupon([FromBody] ReActiveCouponRequest request)
    {
        if (!ModelState.IsValid)
            return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
        try
        {
            var (status, message, data) = await _payment.ReActiveCouponAsync(request);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống:{ex.Message}", null);
        }
    }
}

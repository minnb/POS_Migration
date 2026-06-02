using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// CX Voucher controller — giữ NGUYÊN routes cũ: api/vc/*.
/// Thin delegate → IVoucherService.
/// </summary>
[Route("api/vc")]
public class VoucherController : BaseController
{
    private readonly IVoucherService _voucher;

    public VoucherController(IVoucherService voucher) => _voucher = voucher;

    [HttpGet("generateOTP")]
    public async Task<IActionResult> GenerateOTP(
        [FromQuery] string storeNo, [FromQuery] string posID, [FromQuery] string codeValue)
    {
        try
        {
            var (status, message, data) = await _voucher.GenerateOtpAsync(storeNo, posID, codeValue);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi hệ thống API Blue, vui lòng liên hệ IT", null, ex.Message);
        }
    }

    [HttpGet("getVoucherSerial")]
    public async Task<IActionResult> GetVoucherSerial(
        [FromQuery] string storeNo, [FromQuery] string posID,
        [FromQuery] string codeValue, [FromQuery] string otp)
    {
        try
        {
            var (status, message, data) = await _voucher.GetVoucherSerialAsync(storeNo, posID, codeValue, otp);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi hệ thống API Blue, Vui lòng liên hệ IT", null, ex.Message);
        }
    }

    [HttpPost("UpdateVoucherStatus")]
    public async Task<IActionResult> UpdateVoucherStatus([FromBody] UpdateVoucherStatusRequest model)
    {
        try
        {
            var (status, message, data) = await _voucher.UpdateVoucherStatusAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi hệ thống API Blue, Vui lòng liên hệ IT", null, ex.Message);
        }
    }
}

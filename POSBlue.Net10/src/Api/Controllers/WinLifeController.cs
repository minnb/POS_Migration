using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// WinLife controller — giữ NGUYÊN tất cả routes cũ:
/// api/v2/otp/*, api/blue/winlife/*.
/// Thin delegate: validate input tối thiểu → gọi IWinLifeService → trả ApiResult.
/// </summary>
[Route("api")]
public class WinLifeController : BaseController
{
    private readonly IWinLifeService _winLife;

    public WinLifeController(IWinLifeService winLife)
    {
        _winLife = winLife;
    }

    [HttpGet("v2/otp/generate")]
    public async Task<IActionResult> GenerateOtpV2(
        string posNo = "201801", string phoneNumber = "0948886666", string action = "WIN_MEMBER_REGISTER")
    {
        try
        {
            var (status, message, data) = await _winLife.GenerateOtpV2Async(posNo, phoneNumber, action);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi kết nối tạo tin nhắn OTP, vui lòng liên hệ IT", null,
                ex.Message);
        }
    }

    [HttpPost("v2/otp/verify")]
    public async Task<IActionResult> VerifyOtpV2([FromBody] PosVerifyOtpRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _winLife.VerifyOtpV2Async(request);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi kết nối tạo tin nhắn OTP, vui lòng liên hệ IT", null,
                ex.Message);
        }
    }

    [HttpGet("blue/winlife/generateOTP")]
    public async Task<IActionResult> GenerateOTP(
        [FromQuery] string storeNo = "2018",
        [FromQuery] string posID = "201801",
        [FromQuery] string codeValue = "")
    {
        try
        {
            var (status, message, data) = await _winLife.GenerateOtpAsync(storeNo, posID, codeValue);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT", null,
                ex.Message);
        }
    }

    [HttpPost("blue/winlife/register")]
    public async Task<IActionResult> Register([FromBody] WinLifeRegisterRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest,
                    ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage
                    ?? "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _winLife.RegisterAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi dữ liệu server {ex.Message}", null);
        }
    }

    [HttpPost("blue/winlife/update-promotions")]
    public async Task<IActionResult> UpdatePromotions([FromBody] WinLifeUpdatePromotionsRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _winLife.UpdatePromotionsAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi dữ liệu server {ex.Message}", null);
        }
    }

    [HttpGet("blue/winlife/winCode-histories")]
    public async Task<IActionResult> WinCodeHistories(
        [FromQuery] string csn, [FromQuery] string winCode)
    {
        try
        {
            var (status, message, data) = await _winLife.WinCodeHistoriesAsync(csn, winCode);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpGet("blue/winlife/smart-pos/customer-by-last-digits-phone")]
    public async Task<IActionResult> CustomerByLastDigitsPhone(
        [FromQuery] string storeNo,
        [FromQuery] string posID,
        [FromQuery] string codeValue)
    {
        try
        {
            var (status, message, data) = await _winLife.CustomerByLastDigitsPhoneAsync(
                storeNo, posID, codeValue);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.ServiceUnavailable,
                $"Exception: {ex.Message}", null);
        }
    }

    [HttpPost("blue/winlife/smart-pos/update-customer-info")]
    public async Task<IActionResult> UpdateCustomerInfo([FromBody] WinLifeUpdateCustomerRequest model)
    {
        try
        {
            var (status, message, data) = await _winLife.UpdateCustomerInfoAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Exception {ex.Message}", null);
        }
    }
}

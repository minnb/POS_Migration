using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// VINID TopUp + EVoucher controller — giữ NGUYÊN routes cũ: api/vinid/*.
/// Thin delegate → IVinIdTopUpService.
/// </summary>
[Route("api/vinid")]
public class VoucherTopUpVinIDController : BaseController
{
    private readonly IVinIdTopUpService _topUp;

    public VoucherTopUpVinIDController(IVinIdTopUpService topUp) => _topUp = topUp;

    [HttpGet("TopUpCheckMember")]
    public async Task<IActionResult> TopUpCheckMember(
        [FromQuery] string phoneNumber, [FromQuery] string posID,
        [FromQuery] string storeNo, bool isLog = true)
    {
        try
        {
            var (status, message, data) = await _topUp.TopUpCheckMemberAsync(phoneNumber, posID, storeNo);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }

    [HttpPost("TopUpPoinToPhone")]
    public async Task<IActionResult> TopUpPoinToPhone([FromBody] VinIdTopUpPointPosRequest model)
    {
        try
        {
            var (status, message, data) = await _topUp.TopUpPointToPhoneAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }

    [HttpGet("TopUpCheckStatusOrder")]
    public async Task<IActionResult> TopUpCheckStatusOrder(
        [FromQuery] string orderNo, [FromQuery] string posID, [FromQuery] string storeNo)
    {
        try
        {
            var (status, message, data) = await _topUp.TopUpCheckStatusOrderAsync(orderNo, posID, storeNo);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }

    [HttpGet("EVoucherVerify")]
    public async Task<IActionResult> EVoucherVerify(
        [FromQuery] string storeNo, [FromQuery] string posID,
        [FromQuery] string serialNumber, [FromQuery] string userPOS)
    {
        try
        {
            var (status, message, data) = await _topUp.EVoucherVerifyAsync(storeNo, posID, serialNumber, userPOS);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }

    [HttpPost("EVoucherRefund")]
    public async Task<IActionResult> EVoucherRefund([FromBody] VinIdEVoucherRefundPosRequest model)
    {
        try
        {
            var (status, message, data) = await _topUp.EVoucherRefundAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }

    [HttpPost("EVoucherMarkUsed")]
    public async Task<IActionResult> EVoucherMarkUsed([FromBody] VinIdEVoucherMarkUsedPosRequest model)
    {
        try
        {
            var (status, message, data) = await _topUp.EVoucherMarkUsedAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Lỗi hệ thống API BluePOS {ex.Message}", null);
        }
    }
}

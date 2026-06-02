using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Validate controller — giữ NGUYÊN routes cũ: api/v2/telegram/*, api/v2/validate/*, api/v2/invoice/*.
/// Thin delegate: validate input tối thiểu → gọi IValidateService → trả ApiResult.
/// </summary>
[Route("api/v2")]
public class ValidateController : BaseController
{
    private readonly IValidateService _validate;

    public ValidateController(IValidateService validate)
    {
        _validate = validate;
    }

    [HttpPost("telegram/send")]
    public async Task<IActionResult> TelegramSend([FromBody] MessageTelegramRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _validate.TelegramSendAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi Exception:{ex.Message}", null);
        }
    }

    [HttpGet("validate/tax-code")]
    public async Task<IActionResult> ValidateTaxCode(string taxCode)
    {
        try
        {
            if (string.IsNullOrEmpty(taxCode))
                return ApiResult(HttpStatusCode.BadRequest, "taxCode đang bị trống", null);

            var (status, message, data) = await _validate.ValidateTaxCodeAsync(taxCode);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.Conflict, $"Lỗi Exception:{ex.Message}", null);
        }
    }

    [HttpGet("validate/transaction")]
    public async Task<IActionResult> ValidateTransaction([FromQuery] string orderNo)
    {
        try
        {
            if (string.IsNullOrEmpty(orderNo))
                return ApiResult(HttpStatusCode.BadRequest, "Bạn phải chưa nhập Phiếu tính tiền", null);

            var (status, message, data) = await _validate.ValidateTransactionAsync(orderNo);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi Exception:{ex.Message}", null);
        }
    }

    [HttpPost("invoice/create")]
    public async Task<IActionResult> InvoiceCreate([FromBody] InvoiceCreatedRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _validate.InvoiceCreateAsync(model);
            return ApiResult(status, message, data, data as string);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi Exception:{ex.Message}", null);
        }
    }

    [HttpPost("validate/member-business")]
    public async Task<IActionResult> ValidateMemberBusiness([FromBody] ValidateMemberBusinessRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _validate.ValidateMemberBusinessAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }
}

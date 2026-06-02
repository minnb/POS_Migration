using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// SAP Controller — giữ NGUYÊN tất cả routes cũ: api/sap/*.
/// Thin delegate: validate input tối thiểu → gọi ISAPService → trả ApiResult.
/// </summary>
[Route("api/sap")]
public class SAPController : BaseController
{
    private readonly ISAPService _sap;

    public SAPController(ISAPService sap)
    {
        _sap = sap;
    }

    [HttpGet("CheckVoucherBySerials")]
    public async Task<IActionResult> CheckVoucherBySerials(
        [FromQuery] string voucherNumber, [FromQuery] string posTerminal = "", bool IsLog = true)
    {
        try
        {
            if (string.IsNullOrEmpty(voucherNumber))
                return ApiResult(HttpStatusCode.BadRequest, "voucherNumber đang bị trống", null);

            var (status, message, data) = await _sap.CheckVoucherBySerialsAsync(voucherNumber, posTerminal, IsLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("CheckVoucher")]
    public async Task<IActionResult> CheckVoucher(
        [FromQuery] string voucherNumber,
        [FromQuery] string siteNo = "",
        [FromQuery] string posTerminal = "",
        [FromQuery] string companyCode = "",
        [FromQuery] string isEmployee = "",
        [FromQuery] string partner = "",
        [FromQuery] string isCardVoucher = "",
        [FromQuery] int quantity = 1,
        [FromQuery] bool isVoucher = true,
        [FromQuery] string phoneNumber = "",
        bool IsLog = true)
    {
        try
        {
            if (string.IsNullOrEmpty(voucherNumber))
                return ApiResult(HttpStatusCode.BadRequest, "voucherNumber đang bị trống", null);

            var (status, message, data) = await _sap.CheckVoucherAsync(
                voucherNumber, siteNo, posTerminal, companyCode, isEmployee,
                partner, isCardVoucher, quantity, isVoucher, phoneNumber, IsLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpPost("CreateNewVoucher")]
    public async Task<IActionResult> CreateNewVoucher([FromBody] List<CreateVoucherModel> model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest,
                    ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu request không hợp lệ", null);

            var (status, message, data) = await _sap.CreateNewVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpPost("UpdateVoucher")]
    public async Task<IActionResult> UpdateVoucher([FromBody] List<VoucherUpdateRequest> model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest,
                    ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _sap.UpdateVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpPost("winlife/redeemCpnVch")]
    public async Task<IActionResult> RedeemCpnVch([FromBody] VoucherUpdateModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
            if (model?.ListSeriNo == null || model.ListSeriNo.Count == 0)
                return ApiResult(HttpStatusCode.BadRequest, "Danh sách voucher đang bị trống", null);

            var (status, message, data) = await _sap.RedeemCpnVchAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.BadRequest, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("CheckReturnVoucher")]
    public async Task<IActionResult> CheckReturnVoucher(
        [FromQuery] string voucherNumber, [FromQuery] string siteNo = "",
        [FromQuery] string posTerminal = "", bool IsLog = true)
    {
        try
        {
            var (status, message, data) = await _sap.CheckReturnVoucherAsync(voucherNumber, siteNo, posTerminal, IsLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpPost("UpdateReturnVoucher")]
    public async Task<IActionResult> UpdateReturnVoucher([FromBody] List<VoucherUpdateRequest> model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest,
                    ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _sap.UpdateReturnVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API:{ex.Message}", null);
        }
    }

    [HttpPost("CreateReturnVoucher")]
    public async Task<IActionResult> CreateReturnVoucher([FromBody] List<CreateVoucherModel> model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest,
                    ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _sap.CreateReturnVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("EVoucher")]
    public async Task<IActionResult> EVoucher(
        [FromQuery] string CSN, [FromQuery] string storeNo = "",
        [FromQuery] string posTerminal = "", bool isLog = true)
    {
        try
        {
            var (status, message, data) = await _sap.EVoucherAsync(CSN, storeNo, posTerminal, isLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("SendCodeCpnVch")]
    public async Task<IActionResult> SendCodeCpnVch(
        [FromQuery] string storeNo, [FromQuery] string posID,
        [FromQuery] string bussinessDate, [FromQuery] string orderNo,
        [FromQuery] string itemNo, [FromQuery] string phoneNumber = "")
    {
        try
        {
            var (status, message, data) = await _sap.SendCodeCpnVchAsync(
                storeNo, posID, bussinessDate, orderNo, itemNo, phoneNumber);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }

    [HttpGet("retry")]
    public async Task<IActionResult> Retry()
    {
        try
        {
            var (status, message, data) = await _sap.RetryAsync();
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống:{ex.Message}", null);
        }
    }
}

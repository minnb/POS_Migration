using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// PLG controller — giữ NGUYÊN routes cũ: api/plg/*.
/// Thin delegate → IPLGService.
/// Auth: Basic Auth filter xử lý globally (không cần kiểm tra inline như bản cũ).
/// </summary>
[Route("api/plg")]
public class PLGController : BaseController
{
    private readonly IPLGService _plg;

    public PLGController(IPLGService plg) => _plg = plg;

    [HttpPost("GetInfoCard")]
    public async Task<IActionResult> GetInfoCard([FromBody] PlCheckVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GetInfoCardAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPut("SaleVoucher")]
    public async Task<IActionResult> SaleVoucher([FromBody] SaleVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.SaleVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("RedeemVoucher")]
    public async Task<IActionResult> RedeemVoucher([FromBody] RedeemVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.RedeemVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("Check_VC_Odoo")]
    public async Task<IActionResult> CheckVcOdoo([FromBody] PlCheckVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.CheckVcOdooAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("OdooSAP_Update_Voucher")]
    public async Task<IActionResult> OdooSapUpdateVoucher([FromBody] OdooUpdateVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.OdooSapUpdateVoucherAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("UrBoxCheck")]
    public async Task<IActionResult> UrBoxCheck([FromBody] PlCheckVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.UrBoxCheckAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("UrBox_Payment")]
    public async Task<IActionResult> UrBoxPayment([FromBody] RedeemVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.UrBoxPaymentAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi hệ thống API {ex.Message}", null);
        }
    }

    [HttpPost("GiftBox_mutilVoucherStatus")]
    public async Task<IActionResult> GiftBoxMultiVoucherStatus([FromBody] PlCheckVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GiftBoxMultiVoucherStatusAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("GiftBox_voucherStatus")]
    public async Task<IActionResult> GiftBoxVoucherStatus([FromBody] GiftBoxVoucherStatusRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GiftBoxVoucherStatusAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("GiftBox_voucherUsed")]
    public async Task<IActionResult> GiftBoxVoucherUsed([FromBody] RedeemVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GiftBoxVoucherUsedAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("Giftee_statusTickets")]
    public async Task<IActionResult> GifteeStatusTickets([FromBody] GifteeStatusRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GifteeStatusTicketsAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("GotItCheck")]
    public async Task<IActionResult> GotItCheck([FromBody] GotItCheckPosRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GotItCheckAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("GotIt_MultilCheckVoucher")]
    public async Task<IActionResult> GotItMultiCheckVoucher([FromBody] PlCheckVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GotItMultiCheckAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("GotItUsed")]
    public async Task<IActionResult> GotItUsed([FromBody] RedeemVoucherRequest model)
    {
        try
        {
            var (status, message, data) = await _plg.GotItUsedAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("Sale")]
    public async Task<IActionResult> Sale([FromBody] PlgSaleCxRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _plg.SaleAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi dữ liệu server {ex.Message}", null);
        }
    }

    [HttpGet("GetInfoMember")]
    public async Task<IActionResult> GetInfoMember(
        [FromQuery] string numberCard, [FromQuery] string storeNo, [FromQuery] string posID)
    {
        try
        {
            var (status, message, data) = await _plg.GetInfoMemberAsync(numberCard, storeNo, posID);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Lỗi dữ liệu server {ex.Message}", null);
        }
    }
}

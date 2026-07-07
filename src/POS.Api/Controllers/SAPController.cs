using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Sap;
using POS.Common;
using POS.Common.Dtos.SAP;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace POS.Api.Controllers;

[Route("api/sap")]
public sealed class SAPController(ISAPService sapService) : BaseController
{
    private readonly ISAPService _sapService = sapService;

    //sap_check_voucher
    [HttpGet]
    [Route("CheckVoucher")]
    public async Task<IActionResult> CheckVoucher(
        [Required][FromQuery] string voucherNumber,
        [FromQuery] string siteNo = "",
        [FromQuery] string posTerminal = "",
        [FromQuery] string companyCode = "",
        [FromQuery] string isEmployee = "",
        [FromQuery] string partner = "",
        [FromQuery] string isCardVoucher = "",
        [FromQuery] int quantity = 1,
        [FromQuery] bool isVoucher = true,
        [FromQuery] string phoneNumber = "",
        [FromQuery] bool isLog = true,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _sapService.CheckVoucherAsync(voucherNumber, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data    = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status  = HttpStatusCode.InternalServerError
            });
        }
    }

    //sap_check_return_voucher
    [HttpGet]
    [Route("CheckReturnVoucher")]
    public async Task<IActionResult> CheckReturnVoucher(
        [Required][FromQuery] string voucherNumber,
        [FromQuery] string siteNo = "",
        [FromQuery] string posTerminal = "",
        [FromQuery] bool isLog = true,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _sapService.CheckVoucherAsync(voucherNumber, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data    = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status  = HttpStatusCode.InternalServerError
            });
        }
    }

    //sap_create_voucher
    [HttpPost]
    [Route("CreateNewVoucher")]
    public async Task<IActionResult> CreateNewVoucher(
        [FromBody] List<CreateVoucherModel> model,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _sapService.CreateNewVoucherAsync(model, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status = HttpStatusCode.InternalServerError
            });
        }
    }

    //sap_create_return_voucher
    [HttpPost]
    [Route("CreateReturnVoucher")]
    public async Task<IActionResult> CreateReturnVoucher(
        [FromBody] List<CreateVoucherModel> model,
        CancellationToken ct = default)
    {
        try
        {
            model.ForEach(v => v.VoucherType = $"BNMH");
            model.ForEach(v => v.Article_No = $"10000001");
            var result = await _sapService.CreateNewVoucherAsync(model, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data    = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status  = HttpStatusCode.InternalServerError
            });
        }
    }

    //sap_update_return_voucher
    [HttpPost]
    [Route("UpdateReturnVoucher")]
    public async Task<IActionResult> UpdateReturnVoucher(
        [FromBody] List<VoucherUpdateRequest> model,
        CancellationToken ct = default)
    {
        if (model == null || model.Count == 0)
            return BadRequest(new ResultResponse
            {
                Status  = HttpStatusCode.BadRequest,
                Message = "Danh sách voucher không được trống"
            });
        try
        {
            var result = await _sapService.UpdateReturnVoucherAsync(model, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data    = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status  = HttpStatusCode.InternalServerError
            });
        }
    }

    //sap_redeem_cpn_vch
    [HttpPost]
    [Route("winlife/redeemCpnVch")]
    public async Task<IActionResult> RedeemCpnVch(
        [FromBody] VoucherUpdateModel model,
        CancellationToken ct = default)
    {
        if (model.ListSeriNo == null || model.ListSeriNo.Count == 0)
            return BadRequest(new ResultResponse
            {
                Status  = HttpStatusCode.BadRequest,
                Message = "Danh sách voucher không được trống"
            });
        try
        {
            var result = await _sapService.RedeemCpnVchAsync(model, ct);
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data    = null,
                Message = $"Lỗi hệ thống: {ex.Message}",
                Status  = HttpStatusCode.InternalServerError
            });
        }
    }
}

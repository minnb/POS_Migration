using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Payment.DTOs;
using POS.Application.Payment.Services;
using POS.API.Filters;
using POS.Shared.Models;

namespace POS.API.Controllers;

[ApiController]
[ServiceFilter(typeof(BasicAuthFilter))]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPartnerVoucherService _service;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPartnerVoucherService service, ILogger<PaymentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // POST api/v2/partner/voucher/check
    [HttpPost("api/v2/partner/voucher/check")]
    public async Task<IActionResult> ValidateVoucher([FromBody] CheckVoucherPartnerRequest request)
    {
        try
        {
            var (httpStatus, body) = await _service.CheckVoucherAsync(request);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateVoucher failed partner={Partner} pos={PosNo}", request.Partner, request.PosNo);
            return StatusCode(500, new ResultResponse
            {
                Status = 500,
                Message = $"Exception:{ex.Message}",
                Data = null,
                MessageTechnical = ex.Message
            });
        }
    }
}

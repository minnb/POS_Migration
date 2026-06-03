using Microsoft.AspNetCore.Mvc;
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
    private readonly ICapillaryCouponService _capillaryCouponService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPartnerVoucherService service,
        ICapillaryCouponService capillaryCouponService,
        ILogger<PaymentController> logger)
    {
        _service = service;
        _capillaryCouponService = capillaryCouponService;
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

    // POST api/v2/partner/voucher/update-status
    [HttpPost("api/v2/partner/voucher/update-status")]
    public async Task<IActionResult> UpdateStatusVoucher([FromBody] UpdateStatusVoucherPartnerRequest request)
    {
        try
        {
            var (httpStatus, body) = await _service.UpdateStatusVoucherAsync(request);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateStatusVoucher failed partner={Partner} pos={PosNo} orderNo={OrderNo}", request.Partner, request.PosNo, request.OrderNo);
            return StatusCode(500, new ResultResponse
            {
                Status = 500,
                Message = $"Lỗi hệ thống:{ex.Message}",
                Data = null,
                MessageTechnical = ex.Message
            });
        }
    }

    // POST api/v2/partner/coupon/check
    [HttpPost("api/v2/partner/coupon/check")]
    public async Task<IActionResult> ValidateCouponPartner([FromBody] CheckVoucherPartnerRequest request)
    {
        try
        {
            if (request.Partner?.ToUpper() != "CAP")
                return BadRequest(new ResultResponse { Status = 400, Message = $"Tham số PartnerCode truyền vào không đúng {request.Partner}", Data = null });

            var (httpStatus, body) = await _capillaryCouponService.ValidateCouponAsync(request);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateCouponPartner failed partner={Partner} pos={PosNo}", request.Partner, request.PosNo);
            return StatusCode(500, new ResultResponse { Status = 500, Message = $"Lỗi Exception:{ex.Message}", Data = null, MessageTechnical = ex.Message });
        }
    }

    // POST api/v2/partner/coupon/redeem
    [HttpPost("api/v2/partner/coupon/redeem")]
    public async Task<IActionResult> RedeemCouponPartner([FromBody] UpdateStatusVoucherPartnerRequest request)
    {
        try
        {
            if (request.Partner?.ToUpper() != "CAP")
                return BadRequest(new ResultResponse { Status = 400, Message = $"Tham số PartnerCode truyền vào không đúng {request.Partner}", Data = null });

            var (httpStatus, body) = await _capillaryCouponService.RedeemCouponAsync(request);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedeemCouponPartner failed partner={Partner} pos={PosNo} orderNo={OrderNo}", request.Partner, request.PosNo, request.OrderNo);
            return StatusCode(500, new ResultResponse { Status = 500, Message = $"Lỗi hệ thống:{ex.Message}", Data = null });
        }
    }

    // GET api/v2/partner/coupon/list/user
    [HttpGet("api/v2/partner/coupon/list/user")]
    public async Task<IActionResult> GetCouponListByUser(
        [FromQuery] string partner,
        [FromQuery] string storeNo,
        [FromQuery] string mobile,
        [FromQuery] string status)
    {
        if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(mobile)
            || string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(status))
            return BadRequest(new ResultResponse { Status = 400, Message = "Tham số truyền vào không đúng", Data = null });

        try
        {
            if (partner.ToUpper() != "CAP")
                return BadRequest(new ResultResponse { Status = 400, Message = $"Tham số PartnerCode truyền vào không đúng {partner}", Data = null });

            var (httpStatus, body) = await _capillaryCouponService.GetListCouponByUserAsync(storeNo, mobile, status);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCouponListByUser failed partner={Partner} storeNo={StoreNo} mobile={Mobile}", partner, storeNo, mobile);
            return StatusCode(500, new ResultResponse { Status = 500, Message = $"Lỗi hệ thống:{ex.Message}", Data = null });
        }
    }

    // GET api/v2/partner/coupon/detail
    [HttpGet("api/v2/partner/coupon/detail")]
    public async Task<IActionResult> GetCouponDetail(
        [FromQuery] string partner,
        [FromQuery] string storeNo,
        [FromQuery] string serialNo)
    {
        if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(serialNo) || string.IsNullOrEmpty(storeNo))
            return BadRequest(new ResultResponse { Status = 400, Message = "Tham số truyền vào không đúng", Data = null });

        try
        {
            if (partner.ToUpper() != "CAP")
                return BadRequest(new ResultResponse { Status = 400, Message = $"Tham số PartnerCode truyền vào không đúng {partner}", Data = null });

            var (httpStatus, body) = await _capillaryCouponService.GetCouponDetailAsync(storeNo, serialNo);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCouponDetail failed partner={Partner} storeNo={StoreNo} serialNo={SerialNo}", partner, storeNo, serialNo);
            return StatusCode(500, new ResultResponse { Status = 500, Message = $"Lỗi hệ thống:{ex.Message}", Data = null });
        }
    }

    // POST api/v2/partner/coupon/re-active
    [HttpPost("api/v2/partner/coupon/re-active")]
    public async Task<IActionResult> ReActiveCoupon([FromBody] ReActiveCouponRequest request)
    {
        try
        {
            if (request.Partner?.ToUpper() != "CAP")
                return BadRequest(new ResultResponse { Status = 400, Message = $"Tham số Partner truyền vào không đúng {request.Partner}", Data = null });

            var (httpStatus, body) = await _capillaryCouponService.ReActivateCouponsAsync(request);
            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReActiveCoupon failed partner={Partner} storeNo={StoreNo}", request.Partner, request.StoreNo);
            return StatusCode(500, new ResultResponse { Status = 500, Message = $"Lỗi hệ thống:{ex.Message}", Data = null });
        }
    }
}

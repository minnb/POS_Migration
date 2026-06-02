using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

public class OfferController : BaseController
{
    private readonly IOfferService _offerService;
    private readonly IPromotionPartnerClient _promotionPartnerClient;

    public OfferController(IOfferService offerService, IPromotionPartnerClient promotionPartnerClient)
    {
        _offerService = offerService;
        _promotionPartnerClient = promotionPartnerClient;
    }

    [HttpPost("v2/offer/staff/points/topup/retry")]
    public async Task<IActionResult> ProcessRetryTopUpPointsStaff([FromBody] VinIDSalesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));

        try
        {
            await _offerService.ProcessRetryTopUpPointsStaffAsync(request);
            
            return Ok(ApiResult.Success(
                message: $"Retry orderNo {request.OrderNo} vs {request.CardNumber} success",
                messageTechnical: "queue_loyalty_topup_points"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProcessRetryTopUpPointsStaff Error");
            return BadRequest(ApiResult.Error(ex.Message, (int)HttpStatusCode.BadRequest, ex.ToString()));
        }
    }

    [HttpGet("v2/offer/staff/points/topup")]
    public async Task<IActionResult> ProcessTopUpPointsStaff(
        [FromQuery, Required(ErrorMessage = "queueName is required")] string queueName,
        [FromQuery, Required(ErrorMessage = "number is required")] int number, 
        [FromQuery] int isRetry = 0)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));

        try
        {
            var running = await _offerService.ProcessTopUpPointsStaffAsync(queueName, number, isRetry);
            return Ok(ApiResult.Success(running, "Success"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProcessTopUpPointsStaff Error");
            return BadRequest(ApiResult.Error(ex.Message, (int)HttpStatusCode.BadRequest, ex.ToString()));
        }
    }

    [HttpGet("v2/offer/staff/check")]
    public async Task<IActionResult> OfferStaffCheck(
        [FromQuery, Required(ErrorMessage = "storeNo is required")] string storeNo,
        [FromQuery, Required(ErrorMessage = "posNo is required")] string posNo,
        [FromQuery, Required(ErrorMessage = "phoneNumber is required"), RegularExpression(@"^0\d{9}$", ErrorMessage = "Invalid phone number format")] string phoneNumber)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));

        var result = await _offerService.OfferStaffCheckAsync(storeNo, posNo, phoneNumber);
        
        if (result.Status == HttpStatusCode.OK && result.Data != null)
        {
            return Ok(ApiResult.Success(result.Data, "Success"));
        }

        return StatusCode((int)result.Status, ApiResult.Error(result.Message, (int)result.Status));
    }

    [HttpPost("v2/offer/staff/apply")]
    public async Task<IActionResult> OfferStaffTransaction([FromBody] OfferStaffTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));

        if (request.TransactionType > 1 && string.IsNullOrEmpty(request.ReturnedOrderNo))
        {
            return BadRequest(ApiResult.Error("Không tìm thấy đơn hàng gốc", (int)HttpStatusCode.BadRequest));
        }

        var result = await _offerService.OfferStaffTransactionAsync(request);
        if (result.Status == HttpStatusCode.OK)
        {
            return Ok(ApiResult.Success("Success"));
        }

        return StatusCode((int)result.Status, ApiResult.Error(result.Message, (int)result.Status));
    }

    [HttpGet("promotion/staff/force-check")]
    public async Task<IActionResult> ForceCheck(
        [FromQuery] string storeNo, [FromQuery] string posID, [FromQuery] string staffCode)
    {
        if (string.IsNullOrEmpty(staffCode)) return BadRequest(ApiResult.Error("staffCode đang bị trống", 400));
        if (string.IsNullOrEmpty(storeNo)) return BadRequest(ApiResult.Error("storeNo đang bị trống", 400));
        if (string.IsNullOrEmpty(posID)) return BadRequest(ApiResult.Error("posID đang bị trống", 400));

        var (statusCode, response) = await _promotionPartnerClient.ForceCheckAsync(staffCode);
        return HandlePartnerResponse(statusCode, response);
    }

    [HttpGet("promotion/staff/check")]
    public async Task<IActionResult> Check(
        [FromQuery] string storeNo, [FromQuery] string posID, [FromQuery] string quota_code)
    {
        if (string.IsNullOrEmpty(quota_code)) return BadRequest(ApiResult.Error("quota_code đang bị trống", 400));
        if (string.IsNullOrEmpty(storeNo)) return BadRequest(ApiResult.Error("storeNo đang bị trống", 400));
        if (string.IsNullOrEmpty(posID)) return BadRequest(ApiResult.Error("posID đang bị trống", 400));

        var staffCode = quota_code.Length >= 8 ? quota_code.Substring(0, 8) : quota_code;
        var (statusCode, response) = await _promotionPartnerClient.CheckAsync(staffCode, quota_code);
        return HandlePartnerResponse(statusCode, response);
    }

    [HttpPost("promotion/staff/redeem")]
    public async Task<IActionResult> Redeem([FromBody] PromotionStaffRedeemPOSRequest model)
    {
        if (string.IsNullOrEmpty(model.StaffCode)) return BadRequest(ApiResult.Error("staffCode đang bị trống", 400));
        if (string.IsNullOrEmpty(model.StoreNo)) return BadRequest(ApiResult.Error("storeNo đang bị trống", 400));
        if (string.IsNullOrEmpty(model.PosID)) return BadRequest(ApiResult.Error("posID đang bị trống", 400));

        var partnerReq = new PromotionStaffRedeemPartnerRequest
        {
            StaffCode = model.StaffCode,
            StaffName = model.StaffName,
            RedeemDate = model.RedeemDate,
            RefCode = model.OrderNo,
            Amount = model.Amount
        };

        var (statusCode, response) = await _promotionPartnerClient.RedeemAsync(partnerReq);
        return HandlePartnerResponse(statusCode, response);
    }

    [HttpPost("promotion/staff/refund")]
    public async Task<IActionResult> Refund([FromBody] PromotionStaffRefundPOSRequest model)
    {
        if (string.IsNullOrEmpty(model.StaffCode)) return BadRequest(ApiResult.Error("staffCode đang bị trống", 400));
        if (string.IsNullOrEmpty(model.StoreNo)) return BadRequest(ApiResult.Error("storeNo đang bị trống", 400));
        if (string.IsNullOrEmpty(model.PosID)) return BadRequest(ApiResult.Error("posID đang bị trống", 400));

        var partnerReq = new PromotionStaffRedeemPartnerRequest
        {
            StaffCode = model.StaffCode,
            StaffName = model.StaffName,
            RedeemDate = model.RefundDate,
            RefCode = model.OrderNo,
            Amount = model.Amount
        };

        var (statusCode, response) = await _promotionPartnerClient.RefundAsync(partnerReq);
        return HandlePartnerResponse(statusCode, response);
    }

    private IActionResult HandlePartnerResponse(int statusCode, PromotionStaffResponse? response)
    {
        if (statusCode == (int)HttpStatusCode.OK && response != null)
        {
            if (response.Status == 1)
            {
                return Ok(ApiResult.Success(response.Data, response.Message ?? ""));
            }
            return BadRequest(ApiResult.Error(response.Message ?? "", (int)HttpStatusCode.BadRequest));
        }

        return StatusCode(statusCode, ApiResult.Error("Có lỗi xảy ra khi kết nối API Partner", statusCode));
    }
}

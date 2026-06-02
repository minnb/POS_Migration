using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Loyalty controller — giữ NGUYÊN tất cả routes cũ: api/v2/loyalty/* và api/vinid/*.
/// Thin delegate: validate input tối thiểu → gọi ILoyaltyService → trả ApiResult.
/// Không chứa business logic.
/// </summary>
[Route("api")]
public class LoyaltyController : BaseController
{
    private readonly ILoyaltyService _loyalty;

    public LoyaltyController(ILoyaltyService loyalty)
    {
        _loyalty = loyalty;
    }

    // ── api/v2/loyalty/* ──────────────────────────────────────────────────────

    [HttpGet("v2/loyalty/retry")]
    public async Task<IActionResult> LoyaltyRetry()
    {
        try
        {
            await _loyalty.LoyaltyRetryAsync();
            return ApiResult(HttpStatusCode.OK, "Success", null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("v2/loyalty/customer/get")]
    public async Task<IActionResult> GetCustomerDetail(
        string numberCard, string posID, string storeNo,
        string clubCode = "", bool isMobile = false, bool isLog = true)
    {
        try
        {
            if (string.IsNullOrEmpty(numberCard))
                return ApiResult(HttpStatusCode.BadRequest, "numberCard đang bị trống", null);
            if (string.IsNullOrEmpty(storeNo))
                return ApiResult(HttpStatusCode.BadRequest, "storeNo đang bị trống", null);

            var (status, message, data) = await _loyalty.GetCustomerDetailAsync(
                numberCard, posID, storeNo, clubCode, isMobile, isLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("v2/loyalty/customer")]
    public async Task<IActionResult> CustomerRegistration([FromBody] CustomerRegisterRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _loyalty.CustomerRegistrationAsync(model, "POS");
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("v2/loyalty/customer/update")]
    public async Task<IActionResult> CustomerUpdate([FromBody] CustomerUpdateRequest model)
    {
        try
        {
            var (status, message, data) = await _loyalty.CustomerUpdateAsync(model, "POS");
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("v2/loyalty/transaction/add")]
    public async Task<IActionResult> AddTransaction([FromBody] VinIdSalesRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
            if (string.IsNullOrEmpty(model?.CardNumber))
                return ApiResult(HttpStatusCode.BadRequest, "CardNumber đang bị trống", null);

            var (status, message, data) = await _loyalty.AddTransactionAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("v2/loyalty/transaction/refund")]
    public async Task<IActionResult> RefundTransaction([FromBody] VinIdRefundRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);
            if (string.IsNullOrEmpty(model?.CardNumber))
                return ApiResult(HttpStatusCode.BadRequest, "CardNumber đang bị trống", null);

            var (status, message, data) = await _loyalty.RefundTransactionAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("v2/loyalty/other-status")]
    public async Task<IActionResult> OtherStatusUpdate([FromBody] OtherStatusUpdateRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null);

            var (status, message, data) = await _loyalty.OtherStatusUpdateAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── api/vinid/* ───────────────────────────────────────────────────────────

    [HttpGet("vinid/GetInfoMember")]
    public async Task<IActionResult> GetInfoMember(
        string numberCard, string posID, string storeNo,
        string clubCode = "", bool isLog = true)
    {
        try
        {
            if (string.IsNullOrEmpty(numberCard))
                return ApiResult(HttpStatusCode.BadRequest, "numberCard đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdGetInfoMemberAsync(
                numberCard, posID, storeNo, clubCode, isLog);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("vinid/Sales")]
    public IActionResult Sales(
        string QRCode, string CardNumber, string MerchantId, string TerminalId,
        string InvoiceNo, int SpendPoints, decimal BillAmount,
        string OrderNo, bool IsOffline, decimal OrderAmount, string VirtualCard = "")
    {
        try
        {
            if (string.IsNullOrEmpty(MerchantId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã cửa hàng đang bị trống", null);
            if (string.IsNullOrEmpty(TerminalId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
            if (string.IsNullOrEmpty(InvoiceNo))
                return ApiResult(HttpStatusCode.BadRequest, "InvoiceNo đang bị trống", null);
            if (OrderAmount <= 0)
                return ApiResult(HttpStatusCode.BadRequest, "OrderAmount không hợp lệ", null);

            var (status, message, data) = _loyalty.VinIdSales(
                QRCode, CardNumber, MerchantId, TerminalId,
                InvoiceNo, SpendPoints, BillAmount, OrderNo, IsOffline, OrderAmount, VirtualCard);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/SalesV2")]
    public IActionResult SalesV2([FromBody] VinIdSalesRequest model)
    {
        try
        {
            if (string.IsNullOrEmpty(model?.MerchantId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã cửa hàng đang bị trống", null);
            if (string.IsNullOrEmpty(model.TerminalId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
            if (string.IsNullOrEmpty(model.InvoiceNo))
                return ApiResult(HttpStatusCode.BadRequest, "InvoiceNo đang bị trống", null);
            if (model.OrderAmount <= 0)
                return ApiResult(HttpStatusCode.BadRequest, "OrderAmount không hợp lệ", null);

            var (status, message, data) = _loyalty.VinIdSalesV2(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("vinid/Refund")]
    public IActionResult Refund(
        string QRCode, string CardNumber, string MerchantId, string TerminalId,
        string InvoiceNo, string OrderNo, string OrigOrderNo,
        int SpendPoints, decimal RefundAmount, bool IsOffline,
        decimal OrderAmount, bool IsScanAndGo = false)
    {
        try
        {
            if (string.IsNullOrEmpty(CardNumber))
                return ApiResult(HttpStatusCode.BadRequest, "Số thẻ khách hàng đang bị trống", null);
            if (string.IsNullOrEmpty(MerchantId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã cửa hàng đang bị trống", null);
            if (string.IsNullOrEmpty(TerminalId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
            if (string.IsNullOrEmpty(InvoiceNo))
                return ApiResult(HttpStatusCode.BadRequest, "InvoiceNo đang bị trống", null);
            if (string.IsNullOrEmpty(OrigOrderNo))
                return ApiResult(HttpStatusCode.BadRequest, "Đơn hàng bán đang bị trống", null);
            if (OrderAmount <= 0)
                return ApiResult(HttpStatusCode.BadRequest, "OrderAmount không được bằng 0", null);

            var (status, message, data) = _loyalty.VinIdRefund(
                QRCode, CardNumber, MerchantId, TerminalId,
                InvoiceNo, OrderNo, OrigOrderNo, SpendPoints, RefundAmount,
                IsOffline, OrderAmount, IsScanAndGo);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/RefundV2")]
    public async Task<IActionResult> RefundV2([FromBody] VinIdRefundRequest model)
    {
        try
        {
            if (string.IsNullOrEmpty(model?.CardNumber))
                return ApiResult(HttpStatusCode.BadRequest, "Số thẻ khách hàng đang bị trống", null);
            if (string.IsNullOrEmpty(model.OrigOrderNo))
                return ApiResult(HttpStatusCode.BadRequest, "Đơn hàng bán đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdRefundV2Async(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/InitTransaction")]
    public async Task<IActionResult> InitTransaction([FromBody] PosTransactionRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);
            if (string.IsNullOrEmpty(model.QRCode))
                return ApiResult(HttpStatusCode.BadRequest, "Mã QRCode đang bị trống", null);
            if (string.IsNullOrEmpty(model.MerchantId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã siêu thị đang bị trống", null);
            if (string.IsNullOrEmpty(model.TerminalId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
            // QRCode không được là thẻ VINID (8888xxxx / 66668xxxx)
            if (model.QRCode.Length >= 4 &&
                (model.QRCode[..4] == "8888" || (model.QRCode.Length >= 5 && model.QRCode[..5] == "66668")))
                return ApiResult(HttpStatusCode.BadRequest, $"Mã QRCode {model.QRCode} không hợp lệ", null);

            var (status, message, data) = await _loyalty.VinIdInitTransactionAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("vinid/GetTransaction")]
    public async Task<IActionResult> GetTransaction(string transactionId)
    {
        try
        {
            if (string.IsNullOrEmpty(transactionId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã giao dịch không được rỗng", null);

            var (status, message, data) = await _loyalty.VinIdGetTransactionAsync(transactionId);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPut("vinid/CancelTransaction")]
    public async Task<IActionResult> CancelTransaction([FromBody] PosCancelTransactionRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);
            if (string.IsNullOrEmpty(model.TransactionId))
                return ApiResult(HttpStatusCode.BadRequest, "Mã giao dịch không được trống", null);

            var (status, message, data) = await _loyalty.VinIdCancelTransactionAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("vinid/ScanAndGo")]
    public async Task<IActionResult> ScanAndGo(string barcode)
    {
        try
        {
            if (string.IsNullOrEmpty(barcode))
                return ApiResult(HttpStatusCode.BadRequest, "barcode đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdScanAndGoAsync(barcode);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPut("vinid/ScanAndGo/UpdateStatusOrder")]
    public async Task<IActionResult> UpdateStatusOrder([FromBody] ScanAndGoStatusOrderRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdUpdateStatusOrderAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/ExtraSales")]
    public async Task<IActionResult> ExtraSales([FromBody] ExtraSalesPosRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdExtraSalesAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/ExtraRefund")]
    public async Task<IActionResult> ExtraRefund([FromBody] ExtraRefundPosRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdExtraRefundAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/ScanAndGo/SnGRefund")]
    public async Task<IActionResult> SnGRefund([FromBody] ScanAndGoRefundRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdSnGRefundAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpPost("vinid/ScanAndGo/SnGTopup")]
    public async Task<IActionResult> SnGTopup([FromBody] ScanAndGoTopupRequest model)
    {
        try
        {
            if (model == null)
                return ApiResult(HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdSnGTopupAsync(model);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    [HttpGet("vinid/TransactionEnquiry")]
    public async Task<IActionResult> TransactionEnquiry(
        string MemberCard, string PageNo, string TotalRecordPerPage,
        string StartDate, string EndDate)
    {
        try
        {
            if (string.IsNullOrEmpty(MemberCard))
                return ApiResult(HttpStatusCode.BadRequest, "MemberCard đang bị trống", null);

            var (status, message, data) = await _loyalty.VinIdTransactionEnquiryAsync(
                MemberCard, PageNo, TotalRecordPerPage, StartDate, EndDate);
            return ApiResult(status, message, data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Controller Common — GIỮ NGUYÊN 100% route gốc "api/common/...".
/// KHÔNG chứa business logic: chỉ validate input → gọi ICommonService → trả ApiResult.
/// Port từ VCM.POSBLUE.API/Controllers/CommonController.cs (.NET 4.6 Web API 2).
/// </summary>
[Route("api/common")]
public class CommonController : BaseController
{
    private readonly ICommonService _commonService;

    public CommonController(ICommonService commonService)
    {
        _commonService = commonService;
    }

    // ── GET api/common/GetCurrentTime ─────────────────────────────────────────
    [HttpGet("GetCurrentTime")]
    public IActionResult GetCurrentTime()
    {
        try
        {
            return ApiResult(HttpStatusCode.OK, "Success", _commonService.GetCurrentTime());
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "GetCurrentTime");
            return ApiResult(HttpStatusCode.InternalServerError, "Lỗi hệ thống: " + ex.Message, null);
        }
    }

    // ── GET api/common/CheckIPaddressPos ──────────────────────────────────────
    [HttpGet("CheckIPaddressPos")]
    public async Task<IActionResult> CheckIPaddressPos([FromQuery] string IPAddress)
    {
        if (string.IsNullOrEmpty(IPAddress))
            return ApiResult(HttpStatusCode.BadRequest, "IPAddress đang bị trống", null);
        try
        {
            var data = await _commonService.CheckIpAddressPosAsync(IPAddress);
            if (data is null)
                return ApiResult(HttpStatusCode.BadRequest,
                    $"Hiện tại chưa thiết lập POSTerminal cho IP {IPAddress}, Vui lòng liên hệ IT để được hỗ trợ", null);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/TransactionIssue ───────────────────────────────────────
    [HttpGet("TransactionIssue")]
    public async Task<IActionResult> TransactionIssue(
        [FromQuery] string articleNo,
        [FromQuery] string siteCode)
    {
        if (string.IsNullOrEmpty(articleNo))
            return ApiResult(HttpStatusCode.BadRequest, "ArticleNo đang bị trống", null);
        try
        {
            var data = await _commonService.GetTransactionQtyUseAsync(articleNo, siteCode);
            if (data is null)
                return ApiResult(HttpStatusCode.BadRequest, "Không có dữ liệu", null);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "TransactionIssue Store={SiteCode}", siteCode);
            return ApiResult(HttpStatusCode.InternalServerError, "Lỗi hệ thống: " + ex.Message, null);
        }
    }

    // ── GET api/common/GetBusinessDate ────────────────────────────────────────
    [HttpGet("GetBusinessDate")]
    public async Task<IActionResult> GetBusinessDate(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal = "")
    {
        try
        {
            var result = await _commonService.GetBusinessDateAsync(siteCode, posTerminal);
            return ApiResult(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            var errResult = new BusinessDateDto { Status = 3, Message = "Lỗi hệ thống: " + ex.Message };
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, errResult);
        }
    }

    // ── GET api/common/CheckEndShift ──────────────────────────────────────────
    [HttpGet("CheckEndShift")]
    public async Task<IActionResult> CheckEndShift(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal,
        [FromQuery] DateTime businessDate)
    {
        if (string.IsNullOrEmpty(siteCode))
            return ApiResult(HttpStatusCode.BadRequest, "siteCode đang bị trống", null);
        if (string.IsNullOrEmpty(posTerminal))
            return ApiResult(HttpStatusCode.BadRequest, "posTerminal đang bị trống", null);
        try
        {
            var isClosed = await _commonService.GetShiftIsClosedAsync(siteCode, posTerminal, businessDate);
            if (isClosed is null)
                return ApiResult(HttpStatusCode.BadRequest, "Không có dữ liệu", null);
            return ApiResult(HttpStatusCode.OK, "Success", isClosed);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, "Lỗi hệ thống: " + ex.Message, null);
        }
    }

    // ── POST api/common/POSMonitor ────────────────────────────────────────────
    [HttpPost("POSMonitor")]
    public IActionResult POSMonitor([FromBody] PosMonitorInsertRequest model)
    {
        var ipServer = GetIpServer();
        try
        {
            // Fire-and-forget — POS không cần chờ kết quả ghi log
            _ = Task.Run(() => _commonService.InsertPosMonitorAsync(model));
            return ApiResult(HttpStatusCode.OK,
                $"Response from IPServer {ipServer} => Monitor Ins thành công",
                new PosMonitorInsertResponse { ReturnAction = "Ins" });
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError,
                $"Response from IPServer {ipServer} ({ex.Message})", null);
        }
    }

    // ── GET api/common/POSDataSetup ───────────────────────────────────────────
    [HttpGet("POSDataSetup")]
    public async Task<IActionResult> POSDataSetup()
    {
        try
        {
            var data = await _commonService.GetDataSetupAsync();
            if (data is null || data.Count == 0)
                return ApiResult(HttpStatusCode.BadRequest, "Không có dữ liệu", null);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/GetPOSVersion ──────────────────────────────────────────
    [HttpGet("GetPOSVersion")]
    public async Task<IActionResult> GetPOSVersion()
    {
        try
        {
            var data = await _commonService.GetPosVersionAsync();
            if (data is null || data.Count == 0)
                return ApiResult(HttpStatusCode.BadRequest, "Không có dữ liệu", null);
            Serilog.Log.Information("GetPOSVersion: {@Data}", data);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/GetOrderInfo ───────────────────────────────────────────
    [HttpGet("GetOrderInfo")]
    public async Task<IActionResult> GetOrderInfo(
        [FromQuery] string orderNo,
        [FromQuery] string storeNo = "",
        [FromQuery] string posNo   = "")
    {
        if (string.IsNullOrEmpty(orderNo))
            return ApiResult(HttpStatusCode.BadRequest, "Đơn hàng đang bị trống", null);
        try
        {
            var (data, error) = await _commonService.GetOrderInfoAsync(orderNo, storeNo, posNo);
            if (error is not null)
                return ApiResult(HttpStatusCode.BadRequest, error, null);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/WriteFileByManual ──────────────────────────────────────
    /// <remarks>
    /// Endpoint cũ dùng SyncDataToPos (class nội bộ Data project, đã bị loại khỏi kiến trúc mới).
    /// Tạm giữ route để POS client không lỗi 404; trả 501 kèm thông báo rõ ràng.
    /// Sẽ được implement lại trong phase SyncDataPosController.
    /// </remarks>
    [HttpGet("WriteFileByManual")]
    public IActionResult WriteFileByManual(
        [FromQuery] string storeNo,
        [FromQuery] string posTerminal)
    {
        if (string.IsNullOrEmpty(storeNo))
            return ApiResult(HttpStatusCode.BadRequest, "CH/ST đang bị trống", null);
        if (string.IsNullOrEmpty(posTerminal))
            return ApiResult(HttpStatusCode.BadRequest, "POSTerminal đang bị trống", null);

        return ApiResult(HttpStatusCode.NotImplemented,
            "WriteFileByManual chưa được triển khai trong phase này — sẽ hoàn thiện cùng SyncDataPosController.", null);
    }

    // ── GET api/common/GetListPOSDocumentNo ───────────────────────────────────
    [HttpGet("GetListPOSDocumentNo")]
    public async Task<IActionResult> GetListPOSDocumentNo(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal)
    {
        try
        {
            var data = await _commonService.ListPosDocumentNoAsync(siteCode, posTerminal);
            var msg  = data.Count > 0 ? "Success" : "Không có dữ liệu";
            return ApiResult(HttpStatusCode.OK, msg, data);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "GetListPOSDocumentNo");
            return ApiResult(HttpStatusCode.BadRequest, ex.Message, null);
        }
    }

    // ── GET api/common/CheckCouponLine ────────────────────────────────────────
    [HttpGet("CheckCouponLine")]
    public async Task<IActionResult> CheckCouponLine(
        [FromQuery] string itemNo,
        [FromQuery] string barCode)
    {
        try
        {
            var result = await _commonService.CheckCouponLineAsync(itemNo, barCode);
            return ApiResult(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "CheckCouponLine");
            return ApiResult(HttpStatusCode.BadRequest, ex.Message, false);
        }
    }

    // ── POST api/common/UpdateOrderTrans ──────────────────────────────────────
    [HttpPost("UpdateOrderTrans")]
    public async Task<IActionResult> UpdateOrderTrans([FromBody] UpdateOrderInfoRequest model)
    {
        if (model.Header is null)
            return ApiResult(HttpStatusCode.BadRequest, "Fail",
                new ResponseUpdateTransDto { Status = false, Message = "Dữ liệu trả hàng(Header) không được rỗng" });

        if (!model.ListLine.Any())
            return ApiResult(HttpStatusCode.BadRequest, "Fail",
                new ResponseUpdateTransDto { Status = false, Message = "Dữ liệu trả hàng(Line) không được rỗng" });

        try
        {
            var result = await _commonService.UpdateOrderTransAsync(model);
            Serilog.Log.Information("UpdateOrderTrans result: {@Result}", result);
            return ApiResult(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.BadRequest, "Fail",
                new ResponseUpdateTransDto { Status = false, Message = ex.Message });
        }
    }

    // ── GET api/common/GetInsurance ───────────────────────────────────────────
    [HttpGet("GetInsurance")]
    public async Task<IActionResult> GetInsurance(
        [FromQuery] string receiptNo,
        [FromQuery] string posNo,
        [FromQuery] string staffCode)
    {
        if (string.IsNullOrEmpty(receiptNo))
            return ApiResult(HttpStatusCode.BadRequest, "receiptNo đang bị trống", null);
        if (string.IsNullOrEmpty(posNo))
            return ApiResult(HttpStatusCode.BadRequest, "posNo đang bị trống", null);
        if (string.IsNullOrEmpty(staffCode))
            return ApiResult(HttpStatusCode.BadRequest, "staffCode đang bị trống", null);
        try
        {
            var data = await _commonService.GetInsuranceAsync(receiptNo, posNo, staffCode);
            Serilog.Log.Information("GetInsurance result: {@Data}", data);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "GetInsurance");
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, false);
        }
    }

    // ── PUT api/common/UpdateEOD ──────────────────────────────────────────────
    [HttpPut("UpdateEOD")]
    public async Task<IActionResult> UpdateEOD([FromBody] PosEodRequest model)
    {
        if (string.IsNullOrEmpty(model.StoreNo))
            return ApiResult(HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(model.POSTerminal))
            return ApiResult(HttpStatusCode.BadRequest, "POSTerminal đang bị trống", null);
        try
        {
            var result = await _commonService.UpdateEodAsync(model);
            return ApiResult(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, false);
        }
    }

    // ── GET api/common/CheckTotalBill ─────────────────────────────────────────
    [HttpGet("CheckTotalBill")]
    public async Task<IActionResult> CheckTotalBill(
        [FromQuery] string storeNo,
        [FromQuery] string posTerminal,
        [FromQuery] DateTime bussinessDate,
        [FromQuery] int posTotal)
    {
        if (string.IsNullOrEmpty(storeNo))
            return ApiResult(HttpStatusCode.BadRequest, "storeNo đang bị trống", null);
        if (string.IsNullOrEmpty(posTerminal))
            return ApiResult(HttpStatusCode.BadRequest, "posNo đang bị trống", null);
        try
        {
            var data = await _commonService.CheckTotalBillAsync(storeNo, posTerminal, bussinessDate, posTotal);
            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── POST api/common/kios/insert-sale ──────────────────────────────────────
    [HttpPost("kios/insert-sale")]
    public async Task<IActionResult> KiosInsertSale([FromBody] KiosInsertSaleRequest req)
    {
        if (string.IsNullOrEmpty(req.StoreNo))
            return ApiResult(HttpStatusCode.BadRequest, "CH/ST đang bị trống", null);
        try
        {
            var (success, message) = await _commonService.KiosInsertSaleAsync(req);
            return success
                ? ApiResult(HttpStatusCode.OK, message, null)
                : ApiResult(HttpStatusCode.BadRequest, message, null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/kios/check-order ───────────────────────────────────────
    [HttpGet("kios/check-order")]
    public async Task<IActionResult> KiosCheckOrder(
        [FromQuery] string storeNo,
        [FromQuery] string posNo,
        [FromQuery] string orderNo)
    {
        if (string.IsNullOrEmpty(storeNo))
            return ApiResult(HttpStatusCode.BadRequest, "CH/ST đang bị trống", null);
        if (string.IsNullOrEmpty(posNo))
            return ApiResult(HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
        if (string.IsNullOrEmpty(orderNo))
            return ApiResult(HttpStatusCode.BadRequest, "Mã đơn hàng đang bị trống", null);
        try
        {
            var (success, message, data) = await _commonService.KiosCheckOrderAsync(storeNo, posNo, orderNo);
            return success
                ? ApiResult(HttpStatusCode.OK, message, data)
                : ApiResult(HttpStatusCode.BadRequest, message, null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── GET api/common/SendCodeReward ─────────────────────────────────────────
    [HttpGet("SendCodeReward")]
    public async Task<IActionResult> SendCodeReward(
        [FromQuery] string storeNo,
        [FromQuery] string posID,
        [FromQuery] string bussinessDate,
        [FromQuery] string orderNo,
        [FromQuery] string offerNo)
    {
        if (string.IsNullOrEmpty(storeNo))    return ApiResult(HttpStatusCode.BadRequest, "storeNo không được rỗng", null);
        if (string.IsNullOrEmpty(posID))      return ApiResult(HttpStatusCode.BadRequest, "posID không được rỗng", null);
        if (string.IsNullOrEmpty(orderNo))    return ApiResult(HttpStatusCode.BadRequest, "orderNo không được rỗng", null);
        if (string.IsNullOrEmpty(bussinessDate)) return ApiResult(HttpStatusCode.BadRequest, "bussinessDate không được rỗng", null);
        if (string.IsNullOrEmpty(offerNo))    return ApiResult(HttpStatusCode.BadRequest, "offerNo không được rỗng", null);

        try
        {
            var request = new SendCodeRewardRequest
            {
                StoreNo       = storeNo,
                PosID         = posID,
                BussinessDate = bussinessDate,
                OrderNo       = orderNo,
                OfferNo       = offerNo
            };

            var (success, statusCode, message, data) =
                await _commonService.SendCodeRewardAsync(request, GetIpServer() ?? "unknown");

            if (success)
            {
                // 204 = không tìm thấy code nhưng POS vẫn cho qua
                return statusCode == 204
                    ? ApiResult(HttpStatusCode.OK, message, null)
                    : ApiResult(HttpStatusCode.OK, "Success", data);
            }

            return statusCode == 500
                ? ApiResult(HttpStatusCode.InternalServerError, message, null)
                : ApiResult(HttpStatusCode.NotFound, message, null);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "SendCodeReward Store={StoreNo} Offer={OfferNo}", storeNo, offerNo);
            return ApiResult(HttpStatusCode.InternalServerError, "Lỗi hệ thống: " + ex.Message, null);
        }
    }

    // ── POST api/common/logging ───────────────────────────────────────────────
    /// <remarks>
    /// Thay thế KibanaService (Elasticsearch legacy) bằng Serilog structured logging.
    /// Client POS gọi cùng route, payload vẫn được chấp nhận và ghi log ra Serilog sink.
    /// </remarks>
    [HttpPost("logging")]
    public IActionResult PutLogging([FromBody] PosLoggingRequest request)
    {
        try
        {
            _commonService.LogFromPos(request);
            return ApiResult(HttpStatusCode.OK, "Success", request);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

}

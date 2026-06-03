using System.Net;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Common.DTOs;
using POS.Application.Common.Services;
using POS.API.Filters;
using POS.Shared.Models;

namespace POS.API.Controllers;

[ApiController]
[Route("api/common")]
[ServiceFilter(typeof(BasicAuthFilter))]
[Produces("application/json")]
public class CommonController : ControllerBase
{
    private readonly ICommonService _service;
    private readonly ILogger<CommonController> _logger;

    public CommonController(ICommonService service, ILogger<CommonController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Helper ──────────────────────────────────────────────────────────────
    private IActionResult ApiOk(string message, object? data = null)
        => Ok(new ResultResponse { Status = (int)HttpStatusCode.OK, Message = message, Data = data });

    private IActionResult ApiBad(string message, object? data = null)
        => BadRequest(new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = message, Data = data });

    private IActionResult ApiError(string message)
        => StatusCode(500, new ResultResponse { Status = (int)HttpStatusCode.InternalServerError, Message = message });

    // ── Endpoints ───────────────────────────────────────────────────────────

    /// <summary>Kiểm tra số lượng giao dịch theo article</summary>
    [HttpGet("TransactionIssue")]
    public async Task<IActionResult> TransactionIssue([FromQuery] string articleNo, [FromQuery] string siteCode)
    {
        _logger.LogInformation("TransactionIssue {ArticleNo} {SiteCode}", articleNo, siteCode);
        try
        {
            if (string.IsNullOrEmpty(articleNo))
                return ApiBad("ArticleNo đang bị trống");

            var data = await _service.TransactionQtyUseAsync(articleNo, siteCode);
            if (data == null)
                return ApiBad("Không có dữ liệu");

            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransactionIssue failed for {ArticleNo}", articleNo);
            return ApiError("Lỗi hệ thống: " + ex.Message);
        }
    }

    /// <summary>Lấy thời gian hiện tại của server</summary>
    [HttpGet("GetCurrentTime")]
    public IActionResult GetCurrentTime()
    {
        try
        {
            return ApiOk("Success", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentTime failed");
            return ApiError("Lỗi hệ thống: " + ex.Message);
        }
    }

    /// <summary>Lấy ngày kinh doanh của cửa hàng</summary>
    [HttpGet("GetBusinessDate")]
    public async Task<IActionResult> GetBusinessDate([FromQuery] string siteCode, [FromQuery] string? posTerminal)
    {
        _logger.LogInformation("GetBusinessDate {SiteCode} {PosTerminal}", siteCode, posTerminal);
        BusinessDateResponse? result;
        try
        {
            result = await _service.GetBusinessDateAsync(siteCode, posTerminal);
            return ApiOk("Success", result);
        }
        catch (Exception ex)
        {
            result = new BusinessDateResponse { BussinessDate = null, CurrentDate = null, Status = 3, Message = "Lỗi hệ thống: " + ex.Message };
            return ApiError("Lỗi hệ thống: " + ex.Message);
        }
    }

    /// <summary>Kiểm tra trạng thái đóng ca</summary>
    [HttpGet("CheckEndShift")]
    public async Task<IActionResult> CheckEndShift([FromQuery] string siteCode, [FromQuery] string posTerminal, [FromQuery] DateTime businessDate)
    {
        _logger.LogInformation("CheckEndShift {SiteCode} {PosTerminal}", siteCode, posTerminal);
        try
        {
            if (string.IsNullOrEmpty(siteCode)) return ApiBad("siteCode đang bị trống");
            if (string.IsNullOrEmpty(posTerminal)) return ApiBad("posTerminal đang bị trống");

            var result = await _service.CheckEndShiftAsync(siteCode, posTerminal, businessDate);
            return ApiOk("Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckEndShift failed");
            return ApiError("Lỗi hệ thống: " + ex.Message);
        }
    }

    /// <summary>Insert bản ghi monitor POS (fire-and-forget)</summary>
    [HttpPost("POSMonitor")]
    public IActionResult POSMonitor([FromBody] POSMonitorInsertRequest model)
    {
        var ipServer = HttpContext.Connection.LocalIpAddress?.ToString() ?? "unknown";
        try
        {
            _ = Task.Run(() => _service.POSMonitorInsertAsync(model));
            return ApiOk($"Response from IPServer {ipServer}=> Monitor Ins thành công", new POSMonitorInsertResponse { ReturnAction = "Ins" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POSMonitor failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Tra cứu POS Terminal theo IP</summary>
    [HttpGet("CheckIPaddressPos")]
    public async Task<IActionResult> CheckIPaddressPos([FromQuery] string IPAddress)
    {
        _logger.LogInformation("CheckIPaddressPos {IPAddress}", IPAddress);
        try
        {
            if (string.IsNullOrEmpty(IPAddress)) return ApiBad("IPAddress đang bị trống");

            var data = await _service.CheckIPaddressPosAsync(IPAddress);
            if (data == null)
                return ApiBad($"Hiện tại chưa thiết lập POSTerminal cho IP {IPAddress}, Vui lòng liên hệ IT để được hỗ trợ");

            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckIPaddressPos failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Lấy cấu hình setup POS</summary>
    [HttpGet("POSDataSetup")]
    public async Task<IActionResult> POSDataSetup()
    {
        try
        {
            var data = await _service.GetDataSetupAsync();
            if (data == null || data.Count == 0)
                return ApiBad("Không có dữ liệu");

            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POSDataSetup failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Lấy danh sách phiên bản POS</summary>
    [HttpGet("GetPOSVersion")]
    public async Task<IActionResult> GetPOSVersion()
    {
        try
        {
            var data = await _service.GetPOSVersionAsync();
            if (data == null || data.Count == 0)
                return ApiBad("Không có dữ liệu");

            _logger.LogInformation("GetPOSVersion: {Count} records", data.Count);
            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPOSVersion failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Lấy thông tin đơn hàng để trả hàng</summary>
    [HttpGet("GetOrderInfo")]
    public async Task<IActionResult> GetOrderInfo([FromQuery] string orderNo, [FromQuery] string storeNo = "", [FromQuery] string posNo = "")
    {
        _logger.LogInformation("GetOrderInfo {OrderNo} {StoreNo}", orderNo, storeNo);
        try
        {
            if (string.IsNullOrEmpty(orderNo))
                return ApiBad("Đơn hàng đang bị trống");

            var data = await _service.GetOrderInfoAsync(orderNo, storeNo);
            return ApiOk("Success", data);
        }
        catch (InvalidOperationException ex)
        {
            return ApiBad(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrderInfo failed for {OrderNo}", orderNo);
            return ApiError(ex.Message);
        }
    }

    /// <summary>Ghi file dữ liệu thủ công cho POS</summary>
    [HttpGet("WriteFileByManual")]
    public async Task<IActionResult> WriteFileByManual([FromQuery] string storeNo, [FromQuery] string posTerminal)
    {
        try
        {
            if (string.IsNullOrEmpty(storeNo)) return ApiBad("CH/ST đang bị trống");
            if (string.IsNullOrEmpty(posTerminal)) return ApiBad("POSTerminal đang bị trống");

            var result = await _service.WriteFileByManualAsync(storeNo, posTerminal);
            if (result == "OK")
                return ApiOk(result, result);

            return ApiBad(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WriteFileByManual failed for {StoreNo}", storeNo);
            return ApiError($"Lỗi hệ thống API WriteFileByManual: {ex.Message}");
        }
    }

    /// <summary>Lấy danh sách số chứng từ POS</summary>
    [HttpGet("GetListPOSDocumentNo")]
    public async Task<IActionResult> GetListPOSDocumentNo([FromQuery] string siteCode, [FromQuery] string posTerminal)
    {
        try
        {
            var data = await _service.GetListPOSDocumentNoAsync(siteCode, posTerminal);
            if (data != null && data.Count > 0)
                return ApiOk("Success", data);

            return ApiOk("Không có dữ liệu", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetListPOSDocumentNo failed");
            return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = ex.Message });
        }
    }

    /// <summary>Kiểm tra coupon theo itemNo/barCode</summary>
    [HttpGet("CheckCouponLine")]
    public async Task<IActionResult> CheckCouponLine([FromQuery] string itemNo, [FromQuery] string barCode)
    {
        try
        {
            var data = await _service.CheckCouponLineAsync(itemNo, barCode);
            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckCouponLine failed");
            return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = ex.Message, Data = false });
        }
    }

    /// <summary>Cập nhật thông tin đơn hàng trả hàng</summary>
    [HttpPost("UpdateOrderTrans")]
    public async Task<IActionResult> UpdateOrderTrans([FromBody] UpdateOrderInfoRequest model)
    {
        try
        {
            if (model.Header == null)
                return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = "Fail", Data = new ResponseUpdateTransModel { Status = false, Message = "Dữ liệu trả hàng(Header) không được rỗng" } });

            if (model.ListLine.Count == 0)
                return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = "Fail", Data = new ResponseUpdateTransModel { Status = false, Message = "Dữ liệu trả hàng(Line) không được rỗng" } });

            var result = await _service.UpdateOrderTransAsync(model);
            _logger.LogInformation("UpdateOrderTrans result: {Result}", System.Text.Json.JsonSerializer.Serialize(result));
            return ApiOk("Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateOrderTrans failed");
            var result = new ResponseUpdateTransModel { Status = false, Message = ex.Message };
            return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = "Fail", Data = result });
        }
    }

    /// <summary>Lấy thông tin bảo hiểm theo receipt</summary>
    [HttpGet("GetInsurance")]
    public async Task<IActionResult> GetInsurance([FromQuery] string receiptNo, [FromQuery] string posNo, [FromQuery] string staffCode)
    {
        _logger.LogInformation("GetInsurance {ReceiptNo} {PosNo}", receiptNo, posNo);
        try
        {
            if (string.IsNullOrEmpty(receiptNo)) return ApiBad("receiptNo đang bị trống");
            if (string.IsNullOrEmpty(posNo)) return ApiBad("posNo đang bị trống");
            if (string.IsNullOrEmpty(staffCode)) return ApiBad("staffCode đang bị trống");

            var data = await _service.GetInsuranceAsync(receiptNo, posNo, staffCode);
            _logger.LogInformation("GetInsurance result: {Data}", System.Text.Json.JsonSerializer.Serialize(data));
            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInsurance failed for {ReceiptNo}", receiptNo);
            return StatusCode(500, new ResultResponse { Status = (int)HttpStatusCode.InternalServerError, Message = ex.Message, Data = false });
        }
    }

    /// <summary>Cập nhật End-of-Day cho POS</summary>
    [HttpPut("UpdateEOD")]
    public async Task<IActionResult> UpdateEOD([FromBody] POSEODRequest model)
    {
        try
        {
            if (string.IsNullOrEmpty(model.StoreNo)) return ApiBad("StoreNo đang bị trống");
            if (string.IsNullOrEmpty(model.POSTerminal)) return ApiBad("POSTerminal đang bị trống");

            var data = await _service.UpdateEODAsync(model);
            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateEOD failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Kiểm tra tổng số hóa đơn của POS terminal</summary>
    [HttpGet("CheckTotalBill")]
    public async Task<IActionResult> CheckTotalBill([FromQuery] string storeNo, [FromQuery] string posTerminal, [FromQuery] DateTime bussinessDate, [FromQuery] int posTotal)
    {
        try
        {
            if (string.IsNullOrEmpty(storeNo)) return ApiBad("storeNo đang bị trống");
            if (string.IsNullOrEmpty(posTerminal)) return ApiBad("posNo đang bị trống");

            var data = await _service.CheckTotalBillAsync(storeNo, posTerminal, bussinessDate, posTotal);
            return ApiOk("Success", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckTotalBill failed");
            return ApiError(ex.Message);
        }
    }

    /// <summary>Insert dữ liệu bán hàng từ kios</summary>
    [HttpPost("kios/insert-sale")]
    public async Task<IActionResult> KiosInsertSale([FromBody] KiosInsertSalePOSRequest req)
    {
        try
        {
            if (string.IsNullOrEmpty(req.StoreNo))
                return ApiBad("CH/ST đang bị trống");

            if (string.IsNullOrEmpty(req.SaleJson))
                return ApiBad("SaleJson không được để trống");

            var (success, message) = await _service.KiosInsertSaleAsync(req.StoreNo, req.PosID ?? "", req.SaleJson);
            if (success)
                return ApiOk(message);

            return StatusCode(400, new ResultResponse { Status = (int)HttpStatusCode.BadRequest, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KiosInsertSale failed for {StoreNo}", req.StoreNo);
            return ApiError(ex.Message);
        }
    }

    /// <summary>Kiểm tra đơn hàng kios</summary>
    [HttpGet("kios/check-order")]
    public async Task<IActionResult> KiosCheckOrder([FromQuery] string storeNo, [FromQuery] string posNo, [FromQuery] string orderNo)
    {
        try
        {
            if (string.IsNullOrEmpty(storeNo)) return ApiBad("CH/ST đang bị trống");
            if (string.IsNullOrEmpty(posNo)) return ApiBad("Mã POS đang bị trống");
            if (string.IsNullOrEmpty(orderNo)) return ApiBad("Mã đơn hàng đang bị trống");

            var (success, message, data) = await _service.KiosCheckOrderAsync(storeNo, posNo, orderNo);
            if (success)
                return ApiOk(message, data);

            return ApiBad(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KiosCheckOrder failed for {OrderNo}", orderNo);
            return ApiError(ex.Message);
        }
    }

    /// <summary>Gửi mã reward về POS (phụ thuộc PLG module)</summary>
    [HttpGet("SendCodeReward")]
    public IActionResult SendCodeReward([FromQuery] string storeNo, [FromQuery] string posID, [FromQuery] string bussinessDate, [FromQuery] string orderNo, [FromQuery] string offerNo)
    {
        // TODO: Implement sau khi convert PLG module
        // Endpoint này gọi IpLGBLO.SendCodeReWardToPOS — phụ thuộc PLG business logic
        _logger.LogWarning("SendCodeReward called but not yet implemented — pending PLG module conversion");
        return ApiOk("Success", null);
    }

    /// <summary>Log dữ liệu tới Elasticsearch (Kibana)</summary>
    [HttpPost("logging")]
    public async Task<IActionResult> PutLoggingElastic([FromBody] LoggingElasticRequest model)
    {
        try
        {
            await _service.LoggingAsync(model);
            return ApiOk("Success", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logging failed");
            return ApiError(ex.Message);
        }
    }
}

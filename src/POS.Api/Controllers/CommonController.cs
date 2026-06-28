using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Features.Common;
using POS.Common;
using POS.Common.Dtos.POS.Common;
using POS.Common.Dtos.Reward;
using POS.Infrastructure.Logging;

namespace POS.Api.Controllers;

[Route("api/common")]
public sealed class CommonController(
    ICommonService commonService,
    IHealthCheckService healthCheckService,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper
) : BaseController
{
    // ─── CheckConnection (chẩn đoán kết nối hạ tầng) ──────────────────────────

    /// <summary>
    /// Check kết nối Redis, RabbitMQ và SQL (CentralMD/CentralGeneral/CentralSale/
    /// CentralSaleTemplate). Truyền ?storeNo=... để test CentralSaleTemplate theo
    /// đúng logic routing StoreSetServer của store đó.
    /// HTTP 200 = tất cả OK; 503 = có ít nhất 1 kết nối fail (chi tiết trong Data).
    /// </summary>
    [HttpGet("CheckConnection")]
    public async Task<IActionResult> CheckConnection(
        [FromQuery] string? storeNo = null,
        CancellationToken ct = default)
    {
        try
        {
            var items = await healthCheckService.CheckAllAsync(storeNo, ct);
            var failed = items.Count(x => !x.Ok);
            var status = failed == 0 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            return StatusCode((int)status, new ResultResponse
            {
                Status = status,
                Message = failed == 0
                    ? $"OK — {items.Count}/{items.Count} kết nối thành công"
                    : $"{failed}/{items.Count} kết nối thất bại",
                Data = items,
                MessageTechnical = string.Empty
            });
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.CheckConnection", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── TransactionIssue ─────────────────────────────────────────────────────

    [HttpGet("TransactionIssue")]
    public async Task<IActionResult> TransactionIssue(
        [FromQuery] string articleNo,
        [FromQuery] string siteCode)
    {
        if (string.IsNullOrEmpty(articleNo))
            return BadRequestResult("ArticleNo đang bị trống");

        try
        {
            var data = await commonService.TransactionQtyUseAsync(articleNo, siteCode);
            if (data == null)
                return BadRequestResult("Không có dữ liệu");
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.TransactionIssue", ex);
            kibanaService.LogException("TransactionIssue", siteCode, 0, "", JsonConvert.SerializeObject(ex.InnerException));
            return BadRequestResult($"Lỗi hệ thống: {ex.Message}");
        }
    }

    // ─── GetCurrentTime ───────────────────────────────────────────────────────

    [HttpGet("GetCurrentTime")]
    public IActionResult GetCurrentTime()
    {
        return OkResult(DateTime.Now);
    }

    // ─── GetBusinessDate ──────────────────────────────────────────────────────

    [HttpGet("GetBusinessDate")]
    public async Task<IActionResult> GetBusinessDate(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal = "")
    {
        try
        {
            var data = await commonService.GetBusinessDateAsync(siteCode);

            if (data == null)
            {
                var result = new BusinessDateResponse
                {
                    BussinessDate = DateTime.Now,
                    CurrentDate = DateTime.Now,
                    Status = 1,
                    Message = "Success"
                };

                if (!string.IsNullOrEmpty(siteCode))
                {
                    await commonService.InsertBussinessDateOpenAsync(new BussinessDateOpenModel
                    {
                        Code = siteCode,
                        StoreNo = siteCode,
                        BussinessDate = DateTime.Now,
                        CreatedUser = "api",
                        CreatedDate = DateTime.Now
                    });
                }

                if (!string.IsNullOrEmpty(posTerminal))
                {
                    await commonService.InsertSignalStoreAsync(new SignalStoreModel
                    {
                        StoreNO = siteCode,
                        POSTerminalID = posTerminal,
                        BusinessDate = DateTime.Now.Date,
                        CreatedDate = DateTime.Now
                    });
                }

                return OkResult(result);
            }

            if (data.BussinessDate.HasValue && data.CurrentDate.HasValue
                && data.BussinessDate.Value.Date == data.CurrentDate.Value.Date)
            {
                data.Status = 1;
                data.Message = "Đúng ngày";
            }
            else
            {
                data.Status = 2;
                data.Message = $"Ngày kinh doanh:{data.BussinessDate?.ToString("dd/MM/yyyy")} đang nhỏ hơn ngày hiện tại:{data.CurrentDate?.ToString("dd/MM/yyyy")}";
            }

            if (!string.IsNullOrEmpty(posTerminal))
            {
                await commonService.InsertSignalStoreAsync(new SignalStoreModel
                {
                    StoreNO = siteCode,
                    POSTerminalID = posTerminal,
                    BusinessDate = data.Status == 2 ? data.CurrentDate : data.BussinessDate,
                    CreatedDate = DateTime.Now
                });
            }

            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetBusinessDate", ex);
            var errorResult = new BusinessDateResponse
            {
                BussinessDate = null,
                CurrentDate = null,
                Status = 3,
                Message = "Lỗi hệ thống: " + ex.Message
            };
            return OkResult(errorResult);
        }
    }

    // ─── CheckEndShift ────────────────────────────────────────────────────────

    [HttpGet("CheckEndShift")]
    public async Task<IActionResult> CheckEndShift(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal,
        [FromQuery] DateTime businessDate)
    {
        if (string.IsNullOrEmpty(siteCode))
            return BadRequestResult("siteCode đang bị trống");
        if (string.IsNullOrEmpty(posTerminal))
            return BadRequestResult("posTerminal đang bị trống");

        try
        {
            var data = await commonService.GetShiftHeaderAsync(siteCode, posTerminal, businessDate);
            if (data == null)
                return BadRequestResult("Không có dữ liệu");
            return OkResult(data.IsShiftClosed);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.CheckEndShift", ex);
            return BadRequestResult($"Lỗi hệ thống: {ex.Message}");
        }
    }

    // ─── POSMonitor ───────────────────────────────────────────────────────────

    [HttpPost("POSMonitor")]
    public async Task<IActionResult> POSMonitor([FromBody] POSMonitorInsertRequest model)
    {
        var ipServer = GetIpServer();
        try
        {
            _ = Task.Run(() => commonService.POSMonitorInsertAsync(model));
            return OkResult(
                new POSMonitorInsertResponse { ReturnAction = "Ins" },
                $"Response from IPServer {ipServer}=> Monitor Ins thành công");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.POSMonitor", ex);
            return BadRequestResult($"Response from IPServer {ipServer}: {ex.Message}");
        }
    }

    // ─── CheckIPaddressPos ────────────────────────────────────────────────────

    [HttpGet("CheckIPaddressPos")]
    public async Task<IActionResult> CheckIPaddressPos([FromQuery] string IPAddress)
    {
        if (string.IsNullOrEmpty(IPAddress))
            return BadRequestResult("IPAddress đang bị trống");

        try
        {
            var data = await commonService.CheckIPaddressPosAsync(IPAddress);
            if (data == null)
                return BadRequestResult($"Hiện tại chưa thiết lập POSTerminal cho IP {IPAddress}, Vui lòng liên hệ IT để được hỗ trợ");
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.CheckIPaddressPos", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── POSDataSetup ─────────────────────────────────────────────────────────

    [HttpGet("POSDataSetup")]
    public async Task<IActionResult> POSDataSetup()
    {
        try
        {
            var data = await commonService.GetDataSetupAsync();
            // Bug fix: code cũ là `data == null && data.Count <= 0` (NPE nếu data null)
            if (data == null || data.Count == 0)
                return BadRequestResult("Không có dữ liệu");
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.POSDataSetup", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── GetPOSVersion ────────────────────────────────────────────────────────

    [HttpGet("GetPOSVersion")]
    public async Task<IActionResult> GetPOSVersion()
    {
        try
        {
            var data = await commonService.GetPOSVersionAsync();
            if (data == null || data.Count == 0)
                return BadRequestResult("Không có dữ liệu");
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetPOSVersion", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── GetOrderInfo ─────────────────────────────────────────────────────────

    [HttpGet("GetOrderInfo")]
    public async Task<IActionResult> GetOrderInfo(
        [FromQuery] string orderNo,
        [FromQuery] string storeNo = "",
        [FromQuery] string posNo = "")
    {
        if (string.IsNullOrEmpty(orderNo))
            return BadRequestResult("Đơn hàng đang bị trống");

        try
        {
            if (await commonService.CheckSaleReturnAsync(orderNo))
                return BadRequestResult($"Đơn hàng: {orderNo} là đơn hàng trả, không thể thực hiện trả hàng với đơn hàng này");

            var data = await commonService.GetOrderInfoAsync(orderNo);
            if (data.Count == 0)
                return BadRequestResult($"Đơn hàng: {orderNo} chưa được đồng bộ lên Central, vui lòng đợi dữ liệu đồng bộ thành công trước khi trả hàng");

            //kibanaService.LogResponse("GetOrderInfo", posNo, 0, orderNo, JsonConvert.SerializeObject(data));

            if (!string.IsNullOrEmpty(storeNo))
            {
                var transHeaderStr = data.FirstOrDefault(x => x.TableName == "TransHeader");
                if (transHeaderStr != null)
                {
                    var transHeaderData = JsonConvert.DeserializeObject<List<TransHeader>>(transHeaderStr.TableData ?? "[]");
                    if (transHeaderData?.FirstOrDefault()?.StoreNo != storeNo)
                        return BadRequestResult($"Đơn hàng {orderNo} không thuộc siêu thị {storeNo}");
                }
            }

            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetOrderInfo", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── WriteFileByManual ────────────────────────────────────────────────────

    [HttpGet("WriteFileByManual")]
    public IActionResult WriteFileByManual(
        [FromQuery] string storeNo,
        [FromQuery] string posTerminal)
    {
        if (string.IsNullOrEmpty(storeNo))
            return BadRequestResult("CH/ST đang bị trống");
        if (string.IsNullOrEmpty(posTerminal))
            return BadRequestResult("POSTerminal đang bị trống");

        // STUB: SyncDataToPos.writeFileByManual nằm trong TichHopSAP.dll (net452) —
        // dùng System.Web.Services + System.Data.Linq, KHÔNG chạy được trên .NET 10.
        // Cùng pattern với SyncDataPosController.WriteFileByManual (api/posblue).
        return StatusCode((int)HttpStatusCode.NotImplemented, new ResultResponse
        {
            Status = HttpStatusCode.NotImplemented,
            Message = "Chức năng tạo file SOD chưa được migrate (TichHopSAP.dll). Liên hệ dev team.",
            MessageTechnical = "SyncDataToPos.writeFileByManual (TichHopSAP.dll net452) chưa migrate"
        });
    }

    // ─── GetListPOSDocumentNo ─────────────────────────────────────────────────

    [HttpGet("GetListPOSDocumentNo")]
    public async Task<IActionResult> GetListPOSDocumentNo(
        [FromQuery] string siteCode,
        [FromQuery] string posTerminal)
    {
        try
        {
            var data = await commonService.ListPOSDocumentNoAsync(siteCode, posTerminal);
            return OkResult(data, data?.Count > 0 ? "Success" : "Không có dữ liệu");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetListPOSDocumentNo", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── GetTopOrderNo ────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy top 10 OrderNo mới nhất của 1 POS (CentralSales.TransHeader, routed theo store).
    /// </summary>
    [HttpGet("GetTopOrderNo")]
    public async Task<IActionResult> GetTopOrderNo(
        string storeNo,
        string posNo)
    {
        try
        {
            var data = await commonService.GetTopOrderNoAsync(storeNo, posNo);
            if (data.Count == 0)
                return BadRequestResult("Không có dữ liệu");
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetTopOrderNo", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── CheckCouponLine ──────────────────────────────────────────────────────

    [HttpGet("CheckCouponLine")]
    public async Task<IActionResult> CheckCouponLine(
        [FromQuery] string itemNo,
        [FromQuery] string barCode)
    {
        try
        {
            var data = await commonService.CheckCouponLineAsync(itemNo, barCode);
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.CheckCouponLine", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── UpdateOrderTrans ─────────────────────────────────────────────────────

    [HttpPost("UpdateOrderTrans")]
    public async Task<IActionResult> UpdateOrderTrans([FromBody] UpdateOrderInfoModel model)
    {
        if (model.Header == null)
            return BadRequestResult("", "Dữ liệu trả hàng(Header) không được rỗng");
        if (!model.ListLine.Any())
            return BadRequestResult("", "Dữ liệu trả hàng(Line) không được rỗng");

        try
        {
            var result = await commonService.InsertLineOrig_UpdateOrderInfoAsync(model);
            return OkResult(result);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.UpdateOrderTrans", ex);
            var errorResult = new ResponseUpdateTransModel { Status = false, Message = ex.Message };
            return BadRequestResult(JsonConvert.SerializeObject(errorResult));
        }
    }

    // ─── GetInsurance ─────────────────────────────────────────────────────────

    [HttpGet("GetInsurance")]
    public async Task<IActionResult> GetInsurance(
        [FromQuery] string receiptNo,
        [FromQuery] string posNo,
        [FromQuery] string staffCode)
    {
        if (string.IsNullOrEmpty(receiptNo)) return BadRequestResult("receiptNo đang bị trống");
        if (string.IsNullOrEmpty(posNo)) return BadRequestResult("posNo đang bị trống");
        if (string.IsNullOrEmpty(staffCode)) return BadRequestResult("staffCode đang bị trống");

        try
        {
            var data = await commonService.GetInsuranceAsync(receiptNo, posNo, staffCode);
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.GetInsurance", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── UpdateEOD ────────────────────────────────────────────────────────────

    [HttpPut("UpdateEOD")]
    public async Task<IActionResult> UpdateEOD([FromBody] POSEOD_APIModel model)
    {
        if (string.IsNullOrEmpty(model.StoreNo)) return BadRequestResult("StoreNo đang bị trống");
        if (string.IsNullOrEmpty(model.POSTerminal)) return BadRequestResult("POSTerminal đang bị trống");

        try
        {
            var data = await commonService.UpdatePOSEODAsync(model);
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.UpdateEOD", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── CheckTotalBill ───────────────────────────────────────────────────────

    [HttpGet("CheckTotalBill")]
    public async Task<IActionResult> CheckTotalBill(
        [FromQuery] string storeNo,
        [FromQuery] string posTerminal,
        [FromQuery] DateTime bussinessDate,
        [FromQuery] int posTotal)
    {
        if (string.IsNullOrEmpty(storeNo)) return BadRequestResult("storeNo đang bị trống");
        if (string.IsNullOrEmpty(posTerminal)) return BadRequestResult("posNo đang bị trống");

        try
        {
            var data = await commonService.CheckTotalBillAsync(storeNo, posTerminal, bussinessDate, posTotal);
            return OkResult(data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.CheckTotalBill", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── KiosInsertSale ───────────────────────────────────────────────────────

    [HttpPost("kios/insert-sale")]
    public async Task<IActionResult> KiosInsertSale([FromBody] KiosInsertSalePOSRequest req)
    {
        if (string.IsNullOrEmpty(req.StoreNo))
            return BadRequestResult("CH/ST đang bị trống");

        var jsonSale = JsonConvert.DeserializeObject<SyncSaleObject>(req.SaleJson);
        var json = JsonConvert.SerializeObject(jsonSale?.Data)
            .Replace("‘", "").Replace("’", "").Replace("'", "");

        var model = new KiosInsertSaleRequest
        {
            StoreNo = req.StoreNo,
            Type = jsonSale?.Type ?? string.Empty,
            Json = json
        };

        var logSale = new LogSaleKiosModel
        {
            StoreNo = req.StoreNo,
            PosID = req.PosID,
            RequestPOS = req.SaleJson,
            CreatedDate = DateTime.Now,
            RequestAPI = JsonConvert.SerializeObject(model)
        };

        try
        {
            var (success, message) = await commonService.KiosInsertSaleAsync(model);
            logSale.ResponseAPI = message;

            var res = new { Success = success, Message = message };
            logSale.ResponsePOS = JsonConvert.SerializeObject(res);
            _ = Task.Run(() => commonService.LogSaleKiosAsync(logSale));

            return success ? OkResult(null, message) : BadRequestResult(message);
        }
        catch (Exception ex)
        {
            logSale.ResponsePOS = JsonConvert.SerializeObject(ex);
            _ = Task.Run(() => commonService.LogSaleKiosAsync(logSale));
            fileLogHelper.WriteExpLogs("CommonController.KiosInsertSale", ex);
            return BadRequestResult($"Lỗi hệ thống: {ex.Message}");
        }
    }

    // ─── KiosCheckOrder ───────────────────────────────────────────────────────

    [HttpGet("kios/check-order")]
    public async Task<IActionResult> KiosCheckOrder(
        [FromQuery] string storeNo,
        [FromQuery] string posNo,
        [FromQuery] string orderNo)
    {
        if (string.IsNullOrEmpty(storeNo)) return BadRequestResult("CH/ST đang bị trống");
        if (string.IsNullOrEmpty(posNo)) return BadRequestResult("Mã POS đang bị trống");
        if (string.IsNullOrEmpty(orderNo)) return BadRequestResult("Mã đơn hàng đang bị trống");

        try
        {
            var (success, message, data) = await commonService.KiosCheckOrderAsync(storeNo, posNo, orderNo);
            return success ? OkResult(data, message) : BadRequestResult(message);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.KiosCheckOrder", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── SendCodeReward ───────────────────────────────────────────────────────

    [HttpGet("SendCodeReward")]
    public async Task<IActionResult> SendCodeReward(
        [FromQuery] string storeNo,
        [FromQuery] string posID,
        [FromQuery] string bussinessDate,   // format: yyyy-MM-dd
        [FromQuery] string orderNo,
        [FromQuery] string offerNo)
    {
        if (string.IsNullOrEmpty(storeNo)) return BadRequestResult("storeNo không được rỗng");
        if (string.IsNullOrEmpty(posID)) return BadRequestResult("posID không được rỗng");
        if (string.IsNullOrEmpty(orderNo)) return BadRequestResult("orderNo không được rỗng");
        if (string.IsNullOrEmpty(bussinessDate)) return BadRequestResult("bussinessDate không được rỗng");
        if (string.IsNullOrEmpty(offerNo)) return BadRequestResult("offerNo không được rỗng");

        var model = new RewardCodeRequest
        {
            StoreNo = storeNo,
            PosID = posID,
            BussinessDate = Convert.ToDateTime(bussinessDate),
            OrderNo = orderNo,
            OfferNo = offerNo,
            IPServer = GetIpServer()
        };

        try
        {
            var (success, code, message, data) = await commonService.SendCodeRewardAsync(model);

            if (success)
            {
                // code 204 = không tìm thấy code, POS cho qua
                var status = code == 204 ? HttpStatusCode.NoContent : HttpStatusCode.OK;
                return StatusCode((int)status, BuildResult(status, message, code == 204 ? null : (object?)data));
            }

            // Ghi log khi thất bại (thay VinIDBLO.WriteLogAPI)
            var logModel = new LogAPIModel
            {
                CardNumber = offerNo,
                StoreNo = storeNo,
                POSTerminal = posID,
                InvoiceNo = orderNo,
                ActionType = "SendCodeReward",
                RequestPOS = JsonConvert.SerializeObject(model),
                RequestXML = JsonConvert.SerializeObject(model),
                ResponseXML = message,
                DateTime = DateTime.Now
            };
            _ = Task.Run(() => commonService.WriteLogApiAsync(logModel));

            return StatusCode(code == 500 ? 500 : 404,
                BuildResult(code == 500 ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound, message, null));
        }
        catch (Exception ex)
        {
            var logModel = new LogAPIModel
            {
                CardNumber = offerNo,
                StoreNo = storeNo,
                POSTerminal = posID,
                InvoiceNo = orderNo,
                ActionType = "SendCodeReward",
                RequestPOS = JsonConvert.SerializeObject(model),
                RequestXML = JsonConvert.SerializeObject(model),
                ResponseXML = JsonConvert.SerializeObject(ex),
                DateTime = DateTime.Now
            };
            _ = Task.Run(() => commonService.WriteLogApiAsync(logModel));

            fileLogHelper.WriteExpLogs("CommonController.SendCodeReward", ex);
            return BadRequestResult($"Lỗi hệ thống: {JsonConvert.SerializeObject(ex)}");
        }
    }

    // ─── Logging (PutLoggingElastic) ──────────────────────────────────────────

    [HttpPost("logging")]
    public IActionResult PutLoggingElastic([FromBody] LoggingElastic loggingElastic)
    {
        try
        {
            // Thay _kibanaService.LoggingResponseWebApi() cũ (method không còn tồn tại)
            // → dùng LogInfo để forward log từ POS lên Kibana
            kibanaService.LogInfo(
                loggingElastic.Endpoint ?? "unknown",
                loggingElastic.PosNo ?? string.Empty,
                JsonConvert.SerializeObject(loggingElastic));
            return OkResult(loggingElastic);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CommonController.PutLoggingElastic", ex);
            return BadRequestResult(ex);
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static POS.Common.ResultResponse BuildResult(HttpStatusCode status, string message, object? data)
        => new() { Status = status, Message = message, Data = data };
}

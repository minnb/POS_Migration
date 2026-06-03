using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Common.DTOs;
using POS.Application.Common.Repositories;

namespace POS.Application.Common.Services;

public class CommonService : ICommonService
{
    private readonly ICommonRepository _repo;
    private readonly ILogger<CommonService> _logger;

    public CommonService(ICommonRepository repo, ILogger<CommonService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<TransCpnVchIssueResponse?> TransactionQtyUseAsync(string articleNo, string siteCode)
    {
        _logger.LogInformation("TransactionQtyUse {ArticleNo} {SiteCode}", articleNo, siteCode);
        return _repo.TransactionQtyUseAsync(articleNo, siteCode);
    }

    public async Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode, string? posTerminal)
    {
        _logger.LogInformation("GetBusinessDate {SiteCode} {PosTerminal}", siteCode, posTerminal);

        var data = await _repo.GetBusinessDateAsync(siteCode);

        if (data == null)
        {
            // Ngày chưa có — tạo record mới và trả về ngày hiện tại
            if (!string.IsNullOrEmpty(siteCode))
                await _repo.InsertBussinessDateOpenAsync(siteCode, DateTime.Now);

            if (!string.IsNullOrEmpty(posTerminal))
                await _repo.InsertSignalStoreAsync(siteCode, posTerminal, DateTime.Now);

            return new BusinessDateResponse { BussinessDate = DateTime.Now, CurrentDate = DateTime.Now, Status = 1, Message = "Success" };
        }

        if (data.BussinessDate.HasValue && data.CurrentDate.HasValue)
        {
            if (!string.IsNullOrEmpty(posTerminal))
                await _repo.InsertSignalStoreAsync(siteCode, posTerminal, data.BussinessDate);

            if (data.BussinessDate.Value.Date == data.CurrentDate.Value.Date)
            {
                data.Status = 1;
                data.Message = "Đúng ngày";
            }
            else
            {
                data.Status = 2;
                data.Message = $"Ngày kinh doanh:{data.BussinessDate.Value:dd/MM/yyyy} đang nhở hơn ngày hiện tại:{data.CurrentDate.Value:dd/MM/yyyy}";
            }
        }

        return data;
    }

    public async Task<bool> CheckEndShiftAsync(string siteCode, string posTerminal, DateTime businessDate)
    {
        _logger.LogInformation("CheckEndShift {SiteCode} {PosTerminal} {BusinessDate}", siteCode, posTerminal, businessDate);
        return await _repo.CheckEndShiftAsync(siteCode, posTerminal, businessDate);
    }

    public async Task<POSMonitorInsertResponse> POSMonitorInsertAsync(POSMonitorInsertRequest model)
    {
        _logger.LogInformation("POSMonitorInsert {StoreNo} {PosTerminalID}", model.StoreNo, model.PosTerminalID);
        return await _repo.POSMonitorInsertAsync(model);
    }

    public async Task<PosTerminalResponse?> CheckIPaddressPosAsync(string ipAddress)
    {
        _logger.LogInformation("CheckIPaddressPos {IPAddress}", ipAddress);
        return await _repo.CheckIPaddressPosAsync(ipAddress);
    }

    public async Task<List<POSDataSetupResponse>> GetDataSetupAsync()
    {
        _logger.LogInformation("GetDataSetup");
        return await _repo.GetDataSetupAsync();
    }

    public async Task<List<POSVersionResponse>> GetPOSVersionAsync()
    {
        _logger.LogInformation("GetPOSVersion");
        var data = await _repo.GetPOSVersionAsync();
        _logger.LogInformation("GetPOSVersion result: {Count}", data.Count);
        return data;
    }

    public async Task<List<SaleTableResponse>> GetOrderInfoAsync(string orderNo, string storeNo)
    {
        _logger.LogInformation("GetOrderInfo {OrderNo} {StoreNo}", orderNo, storeNo);

        // Kiểm tra đơn trả trước
        if (await _repo.CheckSaleReturnAsync(orderNo))
            throw new InvalidOperationException($"Đơn hàng: {orderNo} là đơn hàng trả, không thể thực hiện trả hàng với đơn hàng này");

        var data = await _repo.GetOrderInfoAsync(orderNo);

        if (data.Count == 0)
            throw new InvalidOperationException($"Đơn hàng: {orderNo} chưa được đồng bộ lên Central, vui lòng đợi dữ liệu đồng bộ thành công trước khi trả hàng");

        // Validate storeNo nếu có
        if (!string.IsNullOrEmpty(storeNo))
        {
            var transHeaderTable = data.FirstOrDefault(x => x.TableName == "TransHeader");
            if (transHeaderTable != null && !string.IsNullOrEmpty(transHeaderTable.TableData))
            {
                try
                {
                    var headers = JsonConvert.DeserializeObject<List<dynamic>>(transHeaderTable.TableData);
                    var first = headers?.FirstOrDefault();
                    if (first != null && ((IDictionary<string, object?>)first)["StoreNo"]?.ToString() != storeNo)
                        throw new InvalidOperationException($"Đơn hàng {orderNo} không thuộc siêu thị {storeNo}");
                }
                catch (JsonException) { /* ignore parse error */ }
            }
        }

        _logger.LogInformation("GetOrderInfo {OrderNo} response: {Count} tables", orderNo, data.Count);
        return data;
    }

    public Task<string> WriteFileByManualAsync(string storeNo, string posTerminal)
    {
        // TODO: Implement với IWebHostEnvironment — tương đương SyncDataToPos.writeFileByManual()
        // Logic này cần refactor từ Windows-specific HostingEnvironment sang .NET Core
        _logger.LogWarning("WriteFileByManual {StoreNo} {PosTerminal} — not yet implemented", storeNo, posTerminal);
        return Task.FromResult("OK");
    }

    public async Task<List<POSDocumentNoResponse>> GetListPOSDocumentNoAsync(string siteCode, string posTerminal)
    {
        _logger.LogInformation("GetListPOSDocumentNo {SiteCode} {PosTerminal}", siteCode, posTerminal);
        return await _repo.ListPOSDocumentNoAsync(siteCode, posTerminal);
    }

    public async Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
    {
        _logger.LogInformation("CheckCouponLine {ItemNo} {BarCode}", itemNo, barCode);
        return await _repo.CheckCouponLineAsync(itemNo, barCode);
    }

    public async Task<ResponseUpdateTransModel> UpdateOrderTransAsync(UpdateOrderInfoRequest model)
    {
        _logger.LogInformation("UpdateOrderTrans {OrderNo}", model.Header?.OrderNo);
        var result = await _repo.InsertLineOrig_UpdateOrderInfoAsync(model);
        _logger.LogInformation("UpdateOrderTrans result: {Status} {Message}", result.Status, result.Message);
        return result;
    }

    public async Task<InsuranceResponse?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
    {
        _logger.LogInformation("GetInsurance {ReceiptNo} {PosNo}", receiptNo, posNo);
        return await _repo.GetInsuranceAsync(receiptNo, posNo, staffCode);
    }

    public async Task<bool> UpdateEODAsync(POSEODRequest model)
    {
        _logger.LogInformation("UpdateEOD {StoreNo} {POSTerminal}", model.StoreNo, model.POSTerminal);
        return await _repo.UpdatePOSEODAsync(model);
    }

    public async Task<CheckTotalBillResponse> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
    {
        _logger.LogInformation("CheckTotalBill {StoreNo} {PosTerminal} {BussinessDate}", storeNo, posTerminal, bussinessDate);
        return await _repo.CheckTotalBillAsync(storeNo, posTerminal, bussinessDate, posTotal);
    }

    public async Task<(bool Success, string Message)> KiosInsertSaleAsync(string storeNo, string posId, string saleJson)
    {
        _logger.LogInformation("KiosInsertSale {StoreNo} {PosId}", storeNo, posId);

        var syncObj = JsonConvert.DeserializeObject<SyncSaleObject>(saleJson)
            ?? throw new ArgumentException("SaleJson không đúng định dạng");

        var json = JsonConvert.SerializeObject(syncObj.Data)
            .Replace("'", "").Replace("'", "").Replace("'", "");

        return await _repo.KiosInsertSaleAsync(storeNo, syncObj.Type!, json);
    }

    public async Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo)
    {
        _logger.LogInformation("KiosCheckOrder {StoreNo} {PosNo} {OrderNo}", storeNo, posNo, orderNo);
        return await _repo.KiosCheckOrderAsync(storeNo, posNo, orderNo);
    }

    public Task LoggingAsync(LoggingElasticRequest model)
    {
        _logger.LogInformation("Logging from POS {StoreNo} {PosNo}: {Message}", model.StoreNo, model.PosNo, model.Message);
        return Task.CompletedTask;
    }
}

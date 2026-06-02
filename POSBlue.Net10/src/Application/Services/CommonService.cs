using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Triển khai use case Common — orchestration, gọi repository qua interface.
/// Thay thế CommonBLO (Business) + CommonData (Data EF6) của bản cũ.
/// Mọi business rule nằm ở đây; Controller không chứa logic.
/// </summary>
public sealed class CommonService : ICommonService
{
    private readonly IPosTerminalRepository _posTerminalRepository;
    private readonly ICommonRepository      _commonRepository;

    public CommonService(
        IPosTerminalRepository posTerminalRepository,
        ICommonRepository commonRepository)
    {
        _posTerminalRepository = posTerminalRepository;
        _commonRepository      = commonRepository;
    }

    // ── Đã có ─────────────────────────────────────────────────────────────────
    public DateTime GetCurrentTime() => DateTime.Now;

    public Task<PosTerminalDto?> CheckIpAddressPosAsync(string ipAddress)
        => _posTerminalRepository.GetByIpAddressAsync(ipAddress);

    // ── TransactionIssue ──────────────────────────────────────────────────────
    public Task<TransCpnVchIssueDto?> GetTransactionQtyUseAsync(string articleNo, string siteCode)
        => _commonRepository.GetTransactionQtyUseAsync(articleNo, siteCode);

    // ── BusinessDate ──────────────────────────────────────────────────────────
    /// <summary>
    /// Logic gốc từ CommonBLO.GetBusinessDate:
    ///  - Nếu chưa có bản ghi → tạo mới, trả BussinessDate = hôm nay (Status=1).
    ///  - Nếu BusinessDate == hôm nay → Status=1.
    ///  - Nếu BusinessDate &lt; hôm nay → Status=2 (cảnh báo cho POS).
    /// Đồng thời ghi nhận SignalStore nếu posTerminal được cung cấp.
    /// </summary>
    public async Task<BusinessDateDto> GetBusinessDateAsync(string siteCode, string posTerminal)
    {
        var data = await _commonRepository.GetBusinessDateAsync(siteCode);
        DateTime resolvedBusinessDate;

        if (data is null)
        {
            resolvedBusinessDate = DateTime.Now;
            if (!string.IsNullOrEmpty(siteCode))
                await _commonRepository.InsertBussinessDateOpenAsync(siteCode, resolvedBusinessDate);

            if (!string.IsNullOrEmpty(posTerminal))
                await _commonRepository.InsertSignalStoreAsync(siteCode, posTerminal, resolvedBusinessDate.Date);

            return new BusinessDateDto
            {
                Status       = 1,
                Message      = "Success",
                BussinessDate = resolvedBusinessDate,
                CurrentDate  = resolvedBusinessDate
            };
        }

        resolvedBusinessDate = data.BussinessDate ?? DateTime.Now;

        if (!string.IsNullOrEmpty(posTerminal))
            await _commonRepository.InsertSignalStoreAsync(
                siteCode, posTerminal,
                data.BussinessDate?.Date ?? DateTime.Now.Date);

        if (data.BussinessDate.HasValue && data.CurrentDate.HasValue
            && data.BussinessDate.Value.Date == data.CurrentDate.Value.Date)
        {
            return new BusinessDateDto
            {
                Status       = 1,
                Message      = "Đúng ngày",
                BussinessDate = data.BussinessDate,
                CurrentDate  = data.CurrentDate
            };
        }

        return new BusinessDateDto
        {
            Status = 2,
            Message = $"Ngày kinh doanh:{data.BussinessDate?.ToString("dd/MM/yyyy")} đang nhỏ hơn ngày hiện tại:{data.CurrentDate?.ToString("dd/MM/yyyy")}",
            BussinessDate = data.BussinessDate,
            CurrentDate   = data.CurrentDate
        };
    }

    // ── CheckEndShift ─────────────────────────────────────────────────────────
    public async Task<bool?> GetShiftIsClosedAsync(string siteCode, string posTerminal, DateTime businessDate)
    {
        var shift = await _commonRepository.GetShiftHeaderAsync(siteCode, posTerminal, businessDate);
        return shift?.IsShiftClosed;
    }

    // ── POSMonitor ────────────────────────────────────────────────────────────
    public Task InsertPosMonitorAsync(PosMonitorInsertRequest model)
        => _commonRepository.InsertPosMonitorAsync(model);

    // ── POSDataSetup ──────────────────────────────────────────────────────────
    public Task<List<PosDataSetupDto>> GetDataSetupAsync()
        => _commonRepository.GetDataSetupAsync();

    // ── POSVersion ────────────────────────────────────────────────────────────
    public Task<List<PosVersionDto>> GetPosVersionAsync()
        => _commonRepository.GetPosVersionAsync();

    // ── GetOrderInfo ──────────────────────────────────────────────────────────
    /// <summary>
    /// Logic gốc: kiểm tra đơn trả → lấy dữ liệu → validate StoreNo.
    /// </summary>
    public async Task<(List<SaleTableDto> Data, string? ErrorMessage)> GetOrderInfoAsync(
        string orderNo, string storeNo, string posNo)
    {
        if (await _commonRepository.CheckSaleReturnAsync(orderNo))
            return ([], $"Đơn hàng: {orderNo} là đơn hàng trả, không thể thực hiện trả hàng với đơn hàng này");

        var data = await _commonRepository.GetOrderInfoAsync(orderNo);
        if (data.Count == 0)
            return ([], $"Đơn hàng: {orderNo} chưa được đồng bộ lên Central, vui lòng đợi dữ liệu đồng bộ thành công trước khi trả hàng");

        // Validate storeNo nếu POS truyền lên
        if (!string.IsNullOrEmpty(storeNo))
        {
            var headerEntry = data.FirstOrDefault(x => x.TableName == "TransHeader");
            if (headerEntry is not null && !string.IsNullOrEmpty(headerEntry.TableData))
            {
                var headers = JsonConvert.DeserializeObject<List<TransHeaderDto>>(headerEntry.TableData);
                var first   = headers?.FirstOrDefault();
                if (first is not null && first.StoreNo != storeNo)
                    return ([], $"Đơn hàng {orderNo} không thuộc siêu thị {storeNo}");
            }
        }

        Log.Information("GetOrderInfo {OrderNo} POS={PosNo} Count={Count}", orderNo, posNo, data.Count);
        return (data, null);
    }

    // ── GetListPOSDocumentNo ──────────────────────────────────────────────────
    public Task<List<PosDocumentNoDto>> ListPosDocumentNoAsync(string storeNo, string posTerminal)
        => _commonRepository.ListPosDocumentNoAsync(storeNo, posTerminal);

    // ── CheckCouponLine ───────────────────────────────────────────────────────
    public Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
        => _commonRepository.CheckCouponLineAsync(itemNo, barCode);

    // ── UpdateOrderTrans ──────────────────────────────────────────────────────
    public Task<ResponseUpdateTransDto> UpdateOrderTransAsync(UpdateOrderInfoRequest request)
        => _commonRepository.InsertLineOrigUpdateOrderInfoAsync(request);

    // ── GetInsurance ──────────────────────────────────────────────────────────
    public Task<InsuranceDto?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
        => _commonRepository.GetInsuranceAsync(receiptNo, posNo, staffCode);

    // ── UpdateEOD ─────────────────────────────────────────────────────────────
    public Task<bool> UpdateEodAsync(PosEodRequest model)
        => _commonRepository.UpdatePosEodAsync(model);

    // ── CheckTotalBill ────────────────────────────────────────────────────────
    public Task<CheckTotalBillDto?> CheckTotalBillAsync(
        string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        => _commonRepository.CheckTotalBillAsync(storeNo, posTerminal, bussinessDate, posTotal);

    // ── Kios ──────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest request)
    {
        // Sanitize single-quotes giống code cũ
        var saleJson = request.SaleJson ?? "";
        var parsed   = JsonConvert.DeserializeObject<dynamic>(saleJson);
        if (parsed is null)
            return (false, "SaleJson không hợp lệ");

        string type = (string)parsed.Type;
        string json = JsonConvert.SerializeObject(parsed.Data)
                           .Replace("'", "").Replace("\u2018", "").Replace("\u2019", "");

        var result = await _commonRepository.KiosInsertSaleAsync(request.StoreNo ?? "", type, json);

        // Log bất đồng bộ
        _ = _commonRepository.LogSaleKiosAsync(
            request.StoreNo ?? "", request.PosID ?? "",
            saleJson, JsonConvert.SerializeObject(new { StoreNo = request.StoreNo, Type = type, Json = json }),
            "", JsonConvert.SerializeObject(result));

        return result;
    }

    public Task<(bool Success, string Message, object? Data)> KiosCheckOrderAsync(
        string storeNo, string posNo, string orderNo)
        => _commonRepository.KiosCheckOrderAsync(storeNo, posNo, orderNo);

    // ── SendCodeReward ────────────────────────────────────────────────────────
    public Task<(bool Success, int StatusCode, string Message, object? Data)> SendCodeRewardAsync(
        SendCodeRewardRequest request, string ipServer)
        => _commonRepository.SendCodeRewardAsync(
            request.StoreNo ?? "", request.PosID ?? "",
            request.BussinessDate ?? "", request.OrderNo ?? "",
            request.OfferNo ?? "", ipServer);

    // ── Logging ───────────────────────────────────────────────────────────────
    public void LogFromPos(PosLoggingRequest request)
        => Log.Information(
            "[POS-LOG] Action={Action} Store={Store} POS={POS} Msg={Msg} Detail={Detail}",
            request.ActionType, request.StoreNo, request.POSTerminal,
            request.Message, request.Detail);
}

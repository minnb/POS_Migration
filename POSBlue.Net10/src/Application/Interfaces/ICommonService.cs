using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use case cho nhóm API Common.
/// Controller chỉ gọi service này — không chứa business logic, không reference Infrastructure.
/// </summary>
public interface ICommonService
{
    // ── Đã có ─────────────────────────────────────────────────────────────────
    DateTime GetCurrentTime();
    Task<PosTerminalDto?> CheckIpAddressPosAsync(string ipAddress);

    // ── TransactionIssue ──────────────────────────────────────────────────────
    Task<TransCpnVchIssueDto?> GetTransactionQtyUseAsync(string articleNo, string siteCode);

    // ── BusinessDate ──────────────────────────────────────────────────────────
    Task<BusinessDateDto> GetBusinessDateAsync(string siteCode, string posTerminal);

    // ── CheckEndShift ─────────────────────────────────────────────────────────
    Task<bool?> GetShiftIsClosedAsync(string siteCode, string posTerminal, DateTime businessDate);

    // ── POSMonitor (fire-and-forget) ──────────────────────────────────────────
    Task InsertPosMonitorAsync(PosMonitorInsertRequest model);

    // ── POSDataSetup ──────────────────────────────────────────────────────────
    Task<List<PosDataSetupDto>> GetDataSetupAsync();

    // ── POSVersion ────────────────────────────────────────────────────────────
    Task<List<PosVersionDto>> GetPosVersionAsync();

    // ── GetOrderInfo ──────────────────────────────────────────────────────────
    Task<(List<SaleTableDto> Data, string? ErrorMessage)> GetOrderInfoAsync(
        string orderNo, string storeNo, string posNo);

    // ── GetListPOSDocumentNo ──────────────────────────────────────────────────
    Task<List<PosDocumentNoDto>> ListPosDocumentNoAsync(string storeNo, string posTerminal);

    // ── CheckCouponLine ───────────────────────────────────────────────────────
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode);

    // ── UpdateOrderTrans ──────────────────────────────────────────────────────
    Task<ResponseUpdateTransDto> UpdateOrderTransAsync(UpdateOrderInfoRequest request);

    // ── GetInsurance ──────────────────────────────────────────────────────────
    Task<InsuranceDto?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode);

    // ── UpdateEOD ─────────────────────────────────────────────────────────────
    Task<bool> UpdateEodAsync(PosEodRequest model);

    // ── CheckTotalBill ────────────────────────────────────────────────────────
    Task<CheckTotalBillDto?> CheckTotalBillAsync(
        string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);

    // ── Kios ──────────────────────────────────────────────────────────────────
    Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest request);
    Task<(bool Success, string Message, object? Data)> KiosCheckOrderAsync(
        string storeNo, string posNo, string orderNo);

    // ── SendCodeReward ────────────────────────────────────────────────────────
    Task<(bool Success, int StatusCode, string Message, object? Data)> SendCodeRewardAsync(
        SendCodeRewardRequest request, string ipServer);

    // ── Logging (thay Kibana) ─────────────────────────────────────────────────
    void LogFromPos(PosLoggingRequest request);
}

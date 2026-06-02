using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Repository contract cho module Common.
/// Mỗi method ánh xạ 1-1 với một stored procedure / query trong CommonData cũ.
/// Application chỉ biết interface này — không reference Infrastructure trực tiếp.
/// </summary>
public interface ICommonRepository
{
    // ── TransactionIssue ──────────────────────────────────────────────────────
    Task<TransCpnVchIssueDto?> GetTransactionQtyUseAsync(string articleNo, string siteCode);

    // ── BusinessDate ──────────────────────────────────────────────────────────
    Task<BusinessDateDto?> GetBusinessDateAsync(string siteCode);
    Task InsertBussinessDateOpenAsync(string siteCode, DateTime bussinessDate);
    Task InsertSignalStoreAsync(string storeNo, string posTerminal, DateTime? businessDate);

    // ── Shift ─────────────────────────────────────────────────────────────────
    Task<ShiftHeaderDto?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate);

    // ── POSMonitor ────────────────────────────────────────────────────────────
    Task InsertPosMonitorAsync(PosMonitorInsertRequest model);

    // ── POSDataSetup ──────────────────────────────────────────────────────────
    Task<List<PosDataSetupDto>> GetDataSetupAsync();

    // ── POSVersion ────────────────────────────────────────────────────────────
    Task<List<PosVersionDto>> GetPosVersionAsync();

    // ── OrderInfo / SaleReturn ────────────────────────────────────────────────
    Task<bool> CheckSaleReturnAsync(string orderNo);
    Task<List<SaleTableDto>> GetOrderInfoAsync(string orderNo);

    // ── DocumentNo ────────────────────────────────────────────────────────────
    Task<List<PosDocumentNoDto>> ListPosDocumentNoAsync(string storeNo, string posTerminal);

    // ── CouponLine ────────────────────────────────────────────────────────────
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode);

    // ── UpdateOrderTrans (ReturnOrder) ────────────────────────────────────────
    Task<ResponseUpdateTransDto> InsertLineOrigUpdateOrderInfoAsync(UpdateOrderInfoRequest request);

    // ── Insurance ─────────────────────────────────────────────────────────────
    Task<InsuranceDto?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode);

    // ── EOD ───────────────────────────────────────────────────────────────────
    Task<bool> UpdatePosEodAsync(PosEodRequest model);

    // ── CheckTotalBill ────────────────────────────────────────────────────────
    Task<CheckTotalBillDto?> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);

    // ── Kios ──────────────────────────────────────────────────────────────────
    Task<(bool Success, string Message)> KiosInsertSaleAsync(string storeNo, string type, string json);
    Task LogSaleKiosAsync(string storeNo, string posId, string requestPos, string requestApi, string responsePos, string responseApi);
    Task<(bool Success, string Message, object? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo);

    // ── SendCodeReward (PLG) ──────────────────────────────────────────────────
    Task<(bool Success, int StatusCode, string Message, object? Data)> SendCodeRewardAsync(
        string storeNo, string posId, string bussinessDate, string orderNo, string offerNo, string ipServer);
}

using POS.Common.Dtos.CentralSale;
using POS.Common.Dtos.POS.Common;
using POS.Common.Dtos.POS;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Truy vấn DB CentralSales (routed per-store qua StoreSetServer).
/// Migrated từ VCM.POSBLUE.Data.Common.CommonData (EF6 CentralSaleContainer).
/// Mọi method catch exception → log → trả default (parity với CommonData cũ —
/// controller dựa vào null/empty để trả 400 "Không có dữ liệu", không phải 500).
/// </summary>
public interface ICentralSaleRepository
{
    Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode, CancellationToken ct = default);
    Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode, CancellationToken ct = default);
    Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model, CancellationToken ct = default);
    Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate, CancellationToken ct = default);
    Task<bool> CheckSaleReturnAsync(string orderNo, CancellationToken ct = default);
    Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo, CancellationToken ct = default);
    Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal, CancellationToken ct = default);
    Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo, CancellationToken ct = default);
    Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model, CancellationToken ct = default);
    Task<(bool, string)> InInsertToTableByJson(string storeNo, string posNo, string transactionId, string message, string source, CancellationToken ct = default);

    // ── Revenue Dashboard ─────────────────────────────────────────────────────
    Task<List<RevenueDailyDto>>  GetRevenueDailyAsync(DateTime fromDate, DateTime toDate,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default);
    Task<List<RevenueHourlyDto>> GetRevenueHourlyAsync(DateTime saleDate,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default);
    Task<RevenueSummaryDto>      GetRevenueSummaryAsync(DateTime today,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default);

    // ── Transaction Dashboard ─────────────────────────────────────────────────
    Task<List<TransactionListDto>> GetTransactionListAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? orderNo, int maxRows = 500, CancellationToken ct = default);

    Task<List<ValidateTransactionLine>> GetTransLinesAsync(string orderNo, CancellationToken ct = default);
    Task<List<TransPaymentEntryDto>> GetTransPaymentEntriesAsync(string orderNo, CancellationToken ct = default);

    // ── Void Transaction Dashboard ────────────────────────────────────────────
    Task<List<VoidTransactionListDto>> GetVoidTransactionListAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? orderNo, string? userVoid = null, string? posNo = null,
        int maxRows = 500, CancellationToken ct = default);

    Task<List<ValidateTransactionLine>> GetVoidTransLinesAsync(string orderNo, CancellationToken ct = default);

    // ── EOS Shift Dashboard ───────────────────────────────────────────────────
    Task<List<EosShiftDto>> GetEosShiftListAsync(
        DateTime businessDate,
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default);

    // ── DataRawJson Log ───────────────────────────────────────────────────────
    Task<DataRawJsonSummaryDto> GetDataRawJsonSummaryAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    Task<List<DataRawJsonLogDto>> GetDataRawJsonListAsync(
        DateTime fromDate, DateTime toDate,
        string? dataType = null, bool? flag = null, int maxRows = 100,
        CancellationToken ct = default);

    // ── Interface_Errors Log ──────────────────────────────────────────────────
    Task InsertInterfaceErrorAsync(
        string? userName, string? errorProcedure, string? errorMessage,
        int? errorNumber = null, int? errorSeverity = null,
        CancellationToken ct = default);

    Task<InterfaceErrorSummaryDto> GetInterfaceErrorSummaryAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    Task<List<InterfaceErrorDto>> GetInterfaceErrorsAsync(
        DateTime fromDate, DateTime toDate,
        string? procedure = null, int maxRows = 200,
        CancellationToken ct = default);
}

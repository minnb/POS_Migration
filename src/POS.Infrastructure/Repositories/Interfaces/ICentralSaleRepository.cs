using POS.Common.Dtos.POS.Common;

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
    Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model, CancellationToken ct = default);
}

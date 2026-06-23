using POS.Common.Dtos.RptCentralSale;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Repository dành riêng cho các báo cáo POS.Web trên CentralSales DB.
/// Tách khỏi ICentralSaleRepository (dùng chung với POS.Api) để tránh coupling.
/// </summary>
public interface IRptCentralSaleRepository
{
    // ── Doanh thu chi tiết ────────────────────────────────────────────────────
    Task<List<DetailRevenueSalesDto>> GetDetailRevenueSalesAsync(
        DateTime fromDate, DateTime toDate,
        string storeNo, string orderType, string salesType,
        string returnOrder, string vatCode, string textSearch,
        string userId, string partner,
        int pageSize, int pageNumber,
        CancellationToken ct = default);
}

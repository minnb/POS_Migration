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
    // ── Doanh thu theo ngành hàng ─────────────────────────────────────────────
    Task<List<SalesByCategoryDto>> GetSalesByCategoryAsync(
        string storeNo, DateTime fromDate, DateTime toDate,
        CancellationToken ct = default);
    // ── Doanh thu theo thời gian (KPI + series) ───────────────────────────────
    // groupBy: 'HOUR' | 'DAY' | 'WEEKDAY' | 'MONTH'
    // includeKpi=false: chỉ lấy series (HOUR/WEEKDAY/compare) — KPI lấy 1 lần ở call DAY.
    // Kết quả được cache Redis (key MD:RptSaleByTime:*); TTL ngắn nếu khoảng chứa hôm nay.
    Task<(SaleByTimeKpiDto Kpi, List<SaleByTimeSeriesDto> Series)> GetSaleByTimeAsync(
        DateTime fromDate, DateTime toDate,
        string? storeNo,
        string groupBy,
        bool includeKpi = true,
        CancellationToken ct = default);
}

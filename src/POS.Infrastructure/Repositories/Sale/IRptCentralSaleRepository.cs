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

    // ── Top sản phẩm bán chạy (KPI + chi tiết SP + ngành hàng) ────────────────
    // sortBy: 'REVENUE' | 'QUANTITY'. includeKpi=false: bỏ KPI (call kỳ trước cho trend).
    // Kết quả cache Redis (key MD:RptTopProduct:*), TTL ngắn nếu khoảng chứa hôm nay.
    Task<(TopProductKpiDto Kpi, List<TopProductDto> Products, List<TopProductCategoryDto> Categories)>
        GetTopProductAsync(
            DateTime fromDate, DateTime toDate,
            string? storeNo,
            int topN, string sortBy,
            bool includeKpi = true,
            CancellationToken ct = default);

    // Drill-through: danh sách dòng hóa đơn của 1 SP (TOP 500, ReportSaleDetail trực tiếp).
    Task<List<ProductOrderLineDto>> GetProductOrderLinesAsync(
        string itemNo, DateTime fromDate, DateTime toDate, string? storeNo,
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

    // ── Doanh thu theo hình thức thanh toán (KPI + theo HTTT + xu hướng ngày) ──
    // Nguồn: TransPaymentEntry ⋈ TransHeader (realtime). Số tiền đã Net (trừ hàng trả).
    // Kết quả cache Redis (key MD:RptSaleByPayment:*), TTL ngắn nếu khoảng chứa hôm nay.
    Task<(PaymentKpiDto Kpi, List<PaymentByMethodDto> ByMethod, List<PaymentTrendDto> Trend)>
        GetSaleByPaymentAsync(
            DateTime fromDate, DateTime toDate,
            string? storeNo,
            CancellationToken ct = default);

    // ── 15.4 Doanh thu theo Nhân viên ────────────────────────────────────────
    // SP [dbo].[GET_REVENUE_ORDER_SALES_BY_STAFF] trên CentralSales.
    // staffCode rỗng = tất cả nhân viên.
    Task<List<RevenueByStaffDto>> GetRevenueByStaffAsync(
        DateTime fromDate, DateTime toDate,
        string storeNo, string? staffCode,
        CancellationToken ct = default);

    // ── 15.5 Doanh thu theo Cửa hàng ─────────────────────────────────────────
    // SP [dbo].[SP_SALES_BY_STORE_BUSSINESS_DATE] trên CentralSales.
    // listStoreJson: JSON [{"SiteNo":"VIN001","SiteName":"..."},...].
    // pageNumber: 0-based (hoặc 1-based — kiểm tra với SP). Total trong mỗi row = tổng records.
    Task<List<RevenueByStoreDto>> GetRevenueByStoreAsync(
        string listStoreJson, DateTime fromDate, DateTime toDate,
        int pageSize, int pageNumber,
        CancellationToken ct = default);
}

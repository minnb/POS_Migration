using Dapper;
using POS.Common.Dtos.RptCentralSale;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Báo cáo POS.Web — truy vấn trực tiếp CentralSales DB qua directConnectionFactory.
/// Không route per-store vì SP nhận StoreNo như filter parameter.
/// </summary>
public sealed class RptCentralSaleRepository(
    CentralSaleConnectionFactory directConnectionFactory,
    IRedisService redis,
    IFileLogHelper fileLogHelper) : IRptCentralSaleRepository
{
    private const int Timeout = 120;

    // ── sp_ReportSaleByTime: cache + fail-fast timeout ────────────────────────
    // Timeout ngắn hơn 120s vì đây là query dashboard — quá ngưỡng này nghĩa là
    // thiếu index trên ReportSaleDetail, nên fail nhanh thay vì giữ connection.
    private const int SaleByTimeTimeout = 45;
    private const string KeySaleByTimePrefix = "MD:RptSaleByTime";
    private const int TtlSaleByTimeToday = 180;    // khoảng có "hôm nay" — worker rebuild mỗi 60s
    private const int TtlSaleByTimePast  = 43200;  // khoảng quá khứ bất biến — 12h

    private const string KeyTopProductPrefix = "MD:RptTopProduct";

    public async Task<List<DetailRevenueSalesDto>> GetDetailRevenueSalesAsync(
        DateTime fromDate, DateTime toDate,
        string storeNo, string orderType, string salesType,
        string returnOrder, string vatCode, string textSearch,
        string userId, string partner,
        int pageSize, int pageNumber,
        CancellationToken ct = default)
    {
        try
        {
            // Normalize parameters: empty string cho string fields, "-1" cho filter defaults
            var normalizedStoreNo   = string.IsNullOrWhiteSpace(storeNo)   ? "" : storeNo.Trim();
            var normalizedOrderType = string.IsNullOrWhiteSpace(orderType) ? "" : orderType.Trim();
            var normalizedSalesType = string.IsNullOrWhiteSpace(salesType) ? "-1" : salesType.Trim();
            var normalizedReturnOrd = string.IsNullOrWhiteSpace(returnOrder) ? "" : returnOrder.Trim();
            var normalizedVatCode   = string.IsNullOrWhiteSpace(vatCode)   ? "" : vatCode.Trim();
            var normalizedTextSrch  = string.IsNullOrWhiteSpace(textSearch) ? "" : textSearch.Trim();
            var normalizedUserId    = string.IsNullOrWhiteSpace(userId)    ? "" : userId.Trim();
            var normalizedPartner   = string.IsNullOrWhiteSpace(partner)   ? "-1" : partner.Trim();
            var finalPageSize       = Math.Max(1, pageSize);
            var finalPageNumber     = Math.Max(0, pageNumber);

            // DEBUG LOG
            Console.WriteLine($"[RptCentralSaleRepository] GetDetailRevenueSales called:");
            Console.WriteLine($"  FromDate={fromDate:O}, ToDate={toDate:O}");
            Console.WriteLine($"  StoreNo='{normalizedStoreNo}', OrderType='{normalizedOrderType}', SalesType='{normalizedSalesType}'");
            Console.WriteLine($"  ReturnOrder='{normalizedReturnOrd}', VatCode='{normalizedVatCode}', TextSearch='{normalizedTextSrch}'");
            Console.WriteLine($"  UserID='{normalizedUserId}', Partner='{normalizedPartner}'");
            Console.WriteLine($"  PageSize={finalPageSize}, PageNumber={finalPageNumber}");

            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var data = await conn.QueryAsync<DetailRevenueSalesDto>(
                new CommandDefinition(
                    "[dbo].[RPT_GET_DETAIL_REVENUE_SALES_LIST]",
                    new
                    {
                        FromDate    = fromDate.Date,
                        ToDate      = toDate.Date,
                        StoreNo     = normalizedStoreNo,
                        OrderType   = normalizedOrderType,
                        SalesType   = normalizedSalesType,
                        ReturnOrder = normalizedReturnOrd,
                        VatCode     = normalizedVatCode,
                        TextSearch  = normalizedTextSrch,
                        UserID      = normalizedUserId,
                        Partner     = normalizedPartner,
                        PageSize    = finalPageSize,
                        PageNumber  = finalPageNumber
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: Timeout,
                    cancellationToken: ct));

            var resultList = data.ToList();
            Console.WriteLine($"[RptCentralSaleRepository] Query returned {resultList.Count} rows");
            return resultList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RptCentralSaleRepository] Exception: {ex.GetType().Name} - {ex.Message}");
            fileLogHelper.WriteExpLogs("GetDetailRevenueSales", ex);
            return [];
        }
    }
    public async Task<(SaleByTimeKpiDto Kpi, List<SaleByTimeSeriesDto> Series)> GetSaleByTimeAsync(
        DateTime fromDate, DateTime toDate,
        string? storeNo,
        string groupBy,
        bool includeKpi = true,
        CancellationToken ct = default)
    {
        var store     = string.IsNullOrWhiteSpace(storeNo) ? "ALL" : storeNo.Trim();
        var range     = $"{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
        var seriesKey = $"{KeySaleByTimePrefix}:{groupBy}:{store}:{range}";
        var kpiKey    = $"{KeySaleByTimePrefix}:KPI:{store}:{range}";

        // ── Cache lookup (Redis lỗi → bỏ qua, rơi xuống DB) ───────────────────
        try
        {
            var cachedSeries = await redis.StringGetAsync<List<SaleByTimeSeriesDto>>(seriesKey);
            if (cachedSeries != null)
            {
                if (!includeKpi) return (new SaleByTimeKpiDto(), cachedSeries);
                var cachedKpi = await redis.StringGetAsync<SaleByTimeKpiDto>(kpiKey);
                if (cachedKpi != null) return (cachedKpi, cachedSeries);
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetSaleByTime.CacheGet", ex);
        }

        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            using var multi = await conn.QueryMultipleAsync(
                new CommandDefinition(
                    "[dbo].[sp_ReportSaleByTime]",
                    new
                    {
                        FromDate = fromDate.Date,
                        ToDate   = toDate.Date,
                        StoreNo  = string.IsNullOrWhiteSpace(storeNo) ? (string?)null : storeNo.Trim(),
                        GroupBy  = groupBy
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: SaleByTimeTimeout,
                    cancellationToken: ct));

            // RS1 (KPI) luôn được SP trả về — đọc và cache "miễn phí", kể cả khi
            // caller không cần (call HOUR/WEEKDAY vẫn warm cache KPI cho call DAY).
            var kpi    = await multi.ReadFirstOrDefaultAsync<SaleByTimeKpiDto>() ?? new();
            var series = (await multi.ReadAsync<SaleByTimeSeriesDto>()).ToList();

            // ── Cache write: khoảng có hôm nay → TTL ngắn; quá khứ → TTL dài ──
            try
            {
                var ttl = toDate.Date >= DateTime.Today ? TtlSaleByTimeToday : TtlSaleByTimePast;
                await redis.StringSetAsync(seriesKey, series, ttl);
                await redis.StringSetAsync(kpiKey, kpi, ttl);
            }
            catch (Exception ex)
            {
                fileLogHelper.WriteExpLogs("GetSaleByTime.CacheSet", ex);
            }

            return (includeKpi ? kpi : new SaleByTimeKpiDto(), series);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetSaleByTime", ex);
            return (new SaleByTimeKpiDto(), []);
        }
    }

    public async Task<(TopProductKpiDto Kpi, List<TopProductDto> Products, List<TopProductCategoryDto> Categories)>
        GetTopProductAsync(
            DateTime fromDate, DateTime toDate,
            string? storeNo,
            int topN, string sortBy,
            bool includeKpi = true,
            CancellationToken ct = default)
    {
        var store     = string.IsNullOrWhiteSpace(storeNo) ? "ALL" : storeNo.Trim();
        var range     = $"{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
        var dataKey   = $"{KeyTopProductPrefix}:{store}:{range}:{topN}:{sortBy}";
        var kpiKey    = $"{KeyTopProductPrefix}:KPI:{store}:{range}:{topN}:{sortBy}";

        // ── Cache lookup (Redis lỗi → bỏ qua, rơi xuống DB) ───────────────────
        try
        {
            var cachedData = await redis.StringGetAsync<TopProductCachePayload>(dataKey);
            if (cachedData != null)
            {
                if (!includeKpi) return (new TopProductKpiDto(), cachedData.Products, cachedData.Categories);
                var cachedKpi = await redis.StringGetAsync<TopProductKpiDto>(kpiKey);
                if (cachedKpi != null) return (cachedKpi, cachedData.Products, cachedData.Categories);
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTopProduct.CacheGet", ex);
        }

        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            using var multi = await conn.QueryMultipleAsync(
                new CommandDefinition(
                    "[dbo].[sp_ReportTopProduct]",
                    new
                    {
                        FromDate   = fromDate.Date,
                        ToDate     = toDate.Date,
                        StoreNo    = string.IsNullOrWhiteSpace(storeNo) ? (string?)null : storeNo.Trim(),
                        CategoryNo = (string?)null,   // chiều ngành hàng chưa kích hoạt (SP chưa JOIN Item master)
                        TopN       = topN,
                        SortBy     = sortBy
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: SaleByTimeTimeout,
                    cancellationToken: ct));

            var kpi        = await multi.ReadFirstOrDefaultAsync<TopProductKpiDto>() ?? new();
            var products   = (await multi.ReadAsync<TopProductDto>()).ToList();
            var categories = (await multi.ReadAsync<TopProductCategoryDto>()).ToList();

            try
            {
                var ttl = toDate.Date >= DateTime.Today ? TtlSaleByTimeToday : TtlSaleByTimePast;
                await redis.StringSetAsync(dataKey, new TopProductCachePayload(products, categories), ttl);
                await redis.StringSetAsync(kpiKey, kpi, ttl);
            }
            catch (Exception ex)
            {
                fileLogHelper.WriteExpLogs("GetTopProduct.CacheSet", ex);
            }

            return (includeKpi ? kpi : new TopProductKpiDto(), products, categories);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTopProduct", ex);
            return (new TopProductKpiDto(), [], []);
        }
    }

    // Drill-through: hóa đơn (order line) của 1 SP — query trực tiếp ReportSaleDetail (NOLOCK).
    // Repo được phép raw SQL tham số hóa; page/Service thì không. Không cache (ít lặp).
    public async Task<List<ProductOrderLineDto>> GetProductOrderLinesAsync(
        string itemNo, DateTime fromDate, DateTime toDate, string? storeNo,
        CancellationToken ct = default)
    {
        try
        {
            const string sql = @"
                SELECT TOP 500
                    OrderNo,
                    OrderDate,
                    OrderTime,
                    StoreNo,
                    POSTerminalNo,
                    CashierID,
                    ISNULL(Quantity, 0)         AS Quantity,
                    ISNULL(UnitPrice, 0)        AS UnitPrice,
                    ISNULL(DiscountAmount, 0)   AS DiscountAmount,
                    ISNULL(LineAmountIncVAT, 0) AS LineAmountIncVAT,
                    ISNULL(SalesIsReturn, 0)    AS SalesIsReturn
                FROM [dbo].[ReportSaleDetail] (NOLOCK)
                WHERE ItemNo = @ItemNo
                  AND OrderDate BETWEEN @FromDate AND @ToDate
                  AND (@StoreNo IS NULL OR StoreNo = @StoreNo)
                ORDER BY OrderDate DESC, OrderNo DESC;";

            var toInclusive = toDate.Date.AddDays(1).AddSeconds(-1);   // tới 23:59:59 ngày @ToDate

            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var data = await conn.QueryAsync<ProductOrderLineDto>(
                new CommandDefinition(sql,
                    new
                    {
                        ItemNo   = itemNo.Trim(),
                        FromDate = fromDate.Date,
                        ToDate   = toInclusive,
                        StoreNo  = string.IsNullOrWhiteSpace(storeNo) ? (string?)null : storeNo.Trim()
                    },
                    commandTimeout: SaleByTimeTimeout,
                    cancellationToken: ct));
            return data.ToList();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetProductOrderLines", ex);
            return [];
        }
    }

    // Payload gộp RS2 + RS3 để cache 1 key (giữ products & categories nhất quán theo cùng filter).
    private sealed record TopProductCachePayload(
        List<TopProductDto> Products,
        List<TopProductCategoryDto> Categories);

    public async Task<List<SalesByCategoryDto>> GetSalesByCategoryAsync(
        string storeNo, DateTime fromDate, DateTime toDate,
        CancellationToken ct = default)
    {
        try
        {
            var normalizedStoreNo = string.IsNullOrWhiteSpace(storeNo) ? "" : storeNo.Trim();

            Console.WriteLine($"[RptCentralSaleRepository] GetSalesByCategory called:");
            Console.WriteLine($"  FromDate={fromDate:O}, ToDate={toDate:O}, StoreNo='{normalizedStoreNo}'");

            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var data = await conn.QueryAsync<SalesByCategoryDto>(
                new CommandDefinition(
                    "[dbo].[Rpt_SalesByCategory]",
                    new
                    {
                        StoreNo  = normalizedStoreNo,
                        FromDate = fromDate.Date,
                        ToDate   = toDate.Date
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: Timeout,
                    cancellationToken: ct));

            var resultList = data.ToList();
            Console.WriteLine($"[RptCentralSaleRepository] Query returned {resultList.Count} rows");
            return resultList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RptCentralSaleRepository] Exception in GetSalesByCategory: {ex.GetType().Name} - {ex.Message}");
            fileLogHelper.WriteExpLogs("GetSalesByCategory", ex);
            return [];
        }
    }
}

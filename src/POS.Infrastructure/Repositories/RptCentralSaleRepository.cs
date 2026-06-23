using Dapper;
using POS.Common.Dtos.RptCentralSale;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Báo cáo POS.Web — truy vấn trực tiếp CentralSales DB qua directConnectionFactory.
/// Không route per-store vì SP nhận StoreNo như filter parameter.
/// </summary>
public sealed class RptCentralSaleRepository(
    CentralSaleConnectionFactory directConnectionFactory,
    IFileLogHelper fileLogHelper) : IRptCentralSaleRepository
{
    private const int Timeout = 120;

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
}

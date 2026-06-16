using Confluent.Kafka;
using Dapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using POS.Common.Dtos.CentralSale;
using POS.Common.Dtos.POS;
using POS.Common.Dtos.POS.Common;
using POS.Common.Helpers;
using POS.Infrastructure.Database;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;
using POS.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Migrated từ CommonData (VCM.POSBLUE.Data/Common/CommonData.cs) — phần CentralSales.
/// Connection routed per-store qua StoreRoutedConnectionFactory.
/// CommandTimeout 120s như code cũ (db.Database.CommandTimeout = 2 * 60).
/// </summary>
public sealed class CentralSaleRepository(
    StoreRoutedConnectionFactory connectionFactory,
    CentralSaleConnectionFactory directConnectionFactory,
    IFileLogHelper fileLogHelper) : ICentralSaleRepository
{
    private const int Timeout = 120;

    public async Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            const string sql = @"SELECT Article_No AS ArticleNo, Voucher_Type AS VoucherType, MaxQtyUse, COUNT(*) AS QtyUse
                                 FROM TransCpnVchIssue (NOLOCK)
                                 WHERE Site = @siteCode AND Article_No = @articleNo
                                 GROUP BY Article_No, Voucher_Type, MaxQtyUse;";
            var data = await conn.QueryFirstOrDefaultAsync<TransCpnVchIssueModel>(
                new CommandDefinition(sql, new { siteCode, articleNo }, commandTimeout: Timeout, cancellationToken: ct));

            // Giữ default cũ: không có data → coi như chưa dùng lần nào
            return data ?? new TransCpnVchIssueModel { ArticleNo = articleNo, MaxQtyUse = 9999, QtyUse = 0, VoucherType = "V" };
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("TransactionQtyUse", ex);
            return default;
        }
    }

    public async Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            const string sql = "SELECT TOP 1 BussinessDate FROM BussinessDateOpen (NOLOCK) WHERE StoreNo = @siteCode;";
            var bussinessDate = await conn.QueryFirstOrDefaultAsync<DateTime?>(
                new CommandDefinition(sql, new { siteCode }, commandTimeout: Timeout, cancellationToken: ct));

            // Parity cũ: store chưa có row → trả null (controller sẽ insert BussinessDateOpen mới)
            if (bussinessDate == null) return null;
            return new BusinessDateResponse { BussinessDate = bussinessDate, CurrentDate = DateTime.Now };
        }
        catch
        {
            return default;
        }
    }

    public async Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(model.StoreNo ?? "", ct: ct);
            const string sql = @"IF NOT EXISTS (SELECT 1 FROM BussinessDateOpen (NOLOCK) WHERE StoreNo = @StoreNo)
                                 INSERT INTO BussinessDateOpen (Code, StoreNo, BussinessDate, CreatedUser, CreatedDate)
                                 VALUES (@Code, @StoreNo, @BussinessDate, @CreatedUser, @CreatedDate);";
            await conn.ExecuteAsync(new CommandDefinition(sql,
                new { model.Code, model.StoreNo, model.BussinessDate, model.CreatedUser, model.CreatedDate },
                commandTimeout: Timeout, cancellationToken: ct));
        }
        catch
        {
            // Parity cũ: BussinessDateOpen không tồn tại ở một số shard DB (store chưa mapping)
            // → swallow như CommonData.InsertBussinessDateOpen() cũ, không break response
        }
    }

    public async Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            return await conn.QueryFirstOrDefaultAsync<ShiftHeaderModel>(new CommandDefinition(
                "[dbo].[API_POS_CHECK_SHIFT_HEADER]",
                new { SiteCode = siteCode, PosTerminal = posTerminal, BusinessDate = businessDate },
                commandType: CommandType.StoredProcedure, commandTimeout: Timeout, cancellationToken: ct));
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetShiftHeader", ex);
            // Parity cũ: lỗi → trả model rỗng (không phải null) — controller check null mới trả "Không có dữ liệu"
            return new ShiftHeaderModel();
        }
    }

    public async Task<bool> CheckSaleReturnAsync(string orderNo, CancellationToken ct = default)
    {
        try
        {
            var siteCode = orderNo.Substring(0, 4);
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            const string sql = @"SELECT CASE WHEN EXISTS (
                                     SELECT 1 FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo AND SalesIsReturn = 1
                                 ) THEN 1 ELSE 0 END;";
            return await conn.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { orderNo }, commandTimeout: Timeout, cancellationToken: ct));
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo, CancellationToken ct = default)
    {
        var result = new List<SaleTableModel>();
        try
        {
            string query = @"SELECT 'TransHeader' TableName, * FROM TransHeader(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransLine' TableName,* FROM TransLine(NOLOCK)a where a.DocumentNo=@OrderNo;
	                        SELECT 'TransPaymentEntry' TableName,* FROM TransPaymentEntry(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransDiscountEntry' TableName,* FROM TransDiscountEntry(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransInfocodeEntry' TableName,* FROM TransInfocodeEntry(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransBonus' TableName,* FROM TransBonus(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransDiscountCouponEntry' TableName,* FROM TransDiscountCouponEntry(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransCpnVchIssue' TableName,* FROM TransCpnVchIssue(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransBluePoint' TableName,* FROM TransBluePoint(NOLOCK)a where a.OrderNo=@OrderNo;
	                        SELECT 'TransInputData' TableName,* FROM TransInputData(NOLOCK)a where a.TransNo=@OrderNo;
	                        SELECT 'TransPaymentInfo' TableName,* FROM TransPaymentInfo (NOLOCK)a where a.OrderNo=@OrderNo;";
            var siteCode = orderNo.Substring(0, 4);
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);

            // SP trả multi-resultset, mỗi resultset = 1 bảng sale (TransHeader, TransLine...).
            // Code cũ dùng DataSet rồi serialize từng DataTable; ở đây dùng Dapper dynamic
            // (mỗi row là IDictionary<string, object>) — JSON output tương đương.
            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(query, new { OrderNo = orderNo },
                //"API_SALE_INFO_ORDERNO", new { OrderNo = orderNo },
                commandType: CommandType.Text, commandTimeout: Timeout, cancellationToken: ct));

            while (!grid.IsConsumed)
            {
                var rows = (await grid.ReadAsync()).Cast<IDictionary<string, object?>>().ToList();
                if (rows.Count == 0) continue;

                var tableModel = new SaleTableModel
                {
                    TableName = rows[0].TryGetValue("TableName", out var tn) ? tn?.ToString() : null
                };

                // Bỏ cột TableName + timestamp khỏi data như code cũ (timestamp là rowversion không serialize được)
                foreach (var row in rows)
                {
                    row.Remove("TableName");
                    row.Remove("timestamp");
                }

                tableModel.TableData = JsonConvert.SerializeObject(rows);
                result.Add(tableModel);
            }
            return result;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"GetOrderInfo.Exception: {JsonConvert.SerializeObject(ex)}");
            return result;
        }
    }

    public async Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal, CancellationToken ct = default)
    {
        var data = new List<POSDocumentNoModel>();
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo, ct: ct);

            const string sqlOrder = @"SELECT TOP 1 StoreNo, POSTerminalNo AS POSTerminal, OrderNo AS LastNumber,
                                             'ORDER' AS DocumentType, OrderTime AS LastDateTime
                                      FROM TransHeader (NOLOCK)
                                      WHERE StoreNo = @storeNo AND POSTerminalNo = @posTerminal
                                      ORDER BY OrderTime DESC;";
            var orderLast = await conn.QueryFirstOrDefaultAsync<POSDocumentNoModel>(
                new CommandDefinition(sqlOrder, new { storeNo, posTerminal }, commandTimeout: Timeout, cancellationToken: ct));

            // LINQ cũ: Voucher_Type == "V" && Site + POSNo.Substring(1, 2) == posTerminal
            // (EF Substring(1,2) = SQL SUBSTRING(POSNo, 2, 2))
            const string sqlVoucher = @"SELECT TOP 1 Site AS StoreNo, @posTerminal AS POSTerminal, SerialNo AS LastNumber,
                                               'VOUCHER' AS DocumentType, CreatedDate AS LastDateTime
                                        FROM TransCpnVchIssue (NOLOCK)
                                        WHERE Voucher_Type = 'V' AND Site = @storeNo
                                          AND Site + SUBSTRING(POSNo, 2, 2) = @posTerminal
                                        ORDER BY CreatedDate DESC;";
            var voucherLast = await conn.QueryFirstOrDefaultAsync<POSDocumentNoModel>(
                new CommandDefinition(sqlVoucher, new { storeNo, posTerminal }, commandTimeout: Timeout, cancellationToken: ct));

            if (orderLast != null) data.Add(orderLast);
            if (voucherLast != null) data.Add(voucherLast);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("ListPOSDocumentNo", ex);
        }
        return data;
    }

    public async Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo, ct: ct);
            string sql = @"SELECT TOP 10 StoreNo, POSTerminalNo, CAST(OrderDate AS DATE) OrderDate, CreatedDate, OrderNo, MemberCardNo, AmountInclVAT
                                 FROM TransHeader (NOLOCK)
                                 ORDER BY CreatedDate DESC;";

            if (!string.IsNullOrEmpty(posNo) && !string.IsNullOrEmpty(storeNo)) 
            {
                sql = @"SELECT TOP 10 StoreNo, POSTerminalNo, CAST(OrderDate AS DATE) OrderDate, CreatedDate, OrderNo, MemberCardNo, AmountInclVAT
                                 FROM TransHeader (NOLOCK)
                                 WHERE StoreNo = @storeNo AND POSTerminalNo = @posNo
                                 ORDER BY CreatedDate DESC;";
            }
            var data = await conn.QueryAsync<TransHeaderOrderModel>(
                new CommandDefinition(sql, new { storeNo, posNo }, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTopOrderNo", ex);
            return [];
        }
    }

    public async Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(model.StoreNo ?? "", ct: ct);
            const string sql = @"UPDATE POSEOD_API SET TotalSale = @TotalSale
                                 WHERE StoreNo = @StoreNo AND POSTerminal = @POSTerminal
                                   AND CAST(BussinessDate AS DATE) = CAST(@BussinessDate AS DATE);
                                 IF @@ROWCOUNT = 0
                                     INSERT INTO POSEOD_API (StoreNo, POSTerminal, BussinessDate, TotalSale, CreatedDate)
                                     VALUES (@StoreNo, @POSTerminal, @BussinessDate, @TotalSale, GETDATE());";
            await conn.ExecuteAsync(new CommandDefinition(sql,
                new { model.StoreNo, model.POSTerminal, model.BussinessDate, model.TotalSale },
                commandTimeout: Timeout, cancellationToken: ct));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool, string)> InInsertToTableByJson(string storeNo, string posNo, string message, CancellationToken ct = default)
    {
        try
        {
            var data = StringHelper.StringToObject<KafkaMessagePOS>(message.Replace("'", ""));
            if (data == null)
            {
               return (false, "Lỗi convert Json");
            }
            string jsonData = JsonConvert.SerializeObject(data.Data);
            var jObject = StringHelper.StringToJObject(message);
            
            if (jObject is null)
            {
                return (false, "Invalid message format");
            }

            string? type = (string?)jObject["Type"];
            if (type == "HARDWARE")
            {
                return (true, "Continue");
            }

            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo ?? "", ct: ct);

            var parameters = new DynamicParameters();
            parameters.Add("@Type", data.Type);
            parameters.Add("@Json", jsonData);
            
            // Call stored procedure
            var result = await conn.QueryAsync<QueryResult>(
                "Sale_InsertDataByOrder_KAFKA",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 90
            );

            if (result == null)
            {
                return (false, $"lỗi thực thi tra cứu log trong Interface_Errors");
            }
            else
            {
                var checkStatus = result.FirstOrDefault() ?? null;
                if (checkStatus != null && checkStatus.STATUS == 0)
                {
                    return (false, $"lỗi thực thi tra cứu log trong Interface_Errors");
                }

                if (data.Type == "REGISTER" && !string.IsNullOrEmpty(posNo))
                {
                    //await RegisterExecuteAsync(dbContext, POSTerminal);
                }
                return (true, "OK");
            }
        }
        catch(Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Revenue Dashboard ─────────────────────────────────────────────────────

    public async Task<List<RevenueDailyDto>> GetRevenueDailyAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT CAST(OrderDate AS DATE) AS SaleDate,
                       SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                ELSE 0 END) AS NetRevenue,
                       COUNT(CASE WHEN TransactionType = 1 THEN 1 END) AS SaleCount,
                       COUNT(CASE WHEN TransactionType IN (2,3) THEN 1 END) AS ReturnCount
                FROM TransHeader (NOLOCK)
                WHERE OrderDate >= @FromDate AND OrderDate < @ToDate
                GROUP BY CAST(OrderDate AS DATE)
                ORDER BY SaleDate;";
            var data = await conn.QueryAsync<RevenueDailyDto>(
                new CommandDefinition(sql, new { FromDate = fromDate.Date, ToDate = toDate.Date }, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueDaily", ex);
            return [];
        }
    }

    public async Task<List<RevenueHourlyDto>> GetRevenueHourlyAsync(DateTime saleDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT DATEPART(HOUR, OrderTime) AS Hour,
                       SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                ELSE 0 END) AS NetRevenue,
                       COUNT(*) AS TransactionCount
                FROM TransHeader (NOLOCK)
                WHERE CAST(OrderDate AS DATE) = @SaleDate
                GROUP BY DATEPART(HOUR, OrderTime)
                ORDER BY Hour;";
            var data = await conn.QueryAsync<RevenueHourlyDto>(
                new CommandDefinition(sql, new { SaleDate = saleDate.Date }, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueHourly", ex);
            return [];
        }
    }

    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime today, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                    WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                    ELSE 0 END), 0) AS TodayRevenue,
                    COUNT(CASE WHEN TransactionType = 1 THEN 1 END) AS TodayOrders,
                    COUNT(CASE WHEN TransactionType IN (2,3) THEN 1 END) AS TodayReturns
                FROM TransHeader (NOLOCK)
                WHERE CAST(OrderDate AS DATE) = @Today;";
            var result = await conn.QueryFirstOrDefaultAsync<RevenueSummaryDto>(
                new CommandDefinition(sql, new { Today = today.Date }, commandTimeout: Timeout, cancellationToken: ct));
            return result ?? new RevenueSummaryDto();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueSummary", ex);
            return new RevenueSummaryDto();
        }
    }
}

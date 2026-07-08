using Confluent.Kafka;
using Dapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using POS.Common.Dtos.CentralSale;
using POS.Common.Dtos.POS;
using POS.Common.Dtos.POS.Common;
using POS.Common.Enums;
using POS.Common.Helpers;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;
using System.Data;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

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

    public async Task<(bool, string)> InInsertToTableByJson(string storeNo, string posNo, string transactionId, string message, string source, CancellationToken ct = default)
    {
        bool   _flag     = false;
        string _errorMsg = "";
        string _dataType = "";

        try
        {
            var data = StringHelper.StringToObject<KafkaMessagePOS>(message.Replace("'", ""));
            if (data == null)
            {
                _errorMsg = "Lỗi convert Json";
                return (false, _errorMsg);
            }

            _dataType = data.Type;
            string jsonData = JsonConvert.SerializeObject(data.Data);
            var jObject = StringHelper.StringToJObject(message);

            if (jObject is null)
            {
                _errorMsg = "Invalid message format";
                return (false, _errorMsg);
            }

            string? type = (string?)jObject["Type"];
            if (type == PushSaleDataTypeEnum.HARDWARE.ToString())
            {
                _flag = true;
                return (true, "Continue");
            }

            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo ?? "", ct: ct);

            var parameters = new DynamicParameters();
            parameters.Add("@Type", data.Type);
            parameters.Add("@Json", jsonData);

            var result = await conn.QueryAsync<QueryResult>(
                "Sale_InsertDataByOrder_KAFKA",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 90
            );

            if (result == null)
            {
                _errorMsg = "lỗi thực thi tra cứu log trong Interface_Errors";
                return (false, _errorMsg);
            }

            var checkStatus = result.FirstOrDefault();
            if (checkStatus != null && checkStatus.STATUS == 0)
            {
                _errorMsg = "lỗi thực thi tra cứu log trong Interface_Errors";
                return (false, _errorMsg);
            }

            _flag = true;
            if (data.Type == PushSaleDataTypeEnum.REGISTER.ToString() && !string.IsNullOrEmpty(posNo))
            {
                await RegisterExecuteAsync(conn, posNo);
                _flag = true;
                return (true, "OK");
            }

            return (true, "OK");
        }
        catch (Exception ex)
        {
            _errorMsg = ex.Message;
            return (false, _errorMsg);
        }
        finally
        {
            await InsertDataRawJsonAsync(
                transactionId, _dataType, message, _flag,
                _flag ? null : _errorMsg, source);
        }
    }

    private async Task InsertDataRawJsonAsync(
        string transactionId, string dataType, string message,
        bool flag, string? errorMessage, string source)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            const string sql = @"
                IF EXISTS (SELECT 1 FROM DataRawJson WHERE TransactionId = @TransactionId)
                    UPDATE DataRawJson
                    SET Flag = @Flag, ErrorMessage = @ErrorMessage, [Source] = @Source, CrtDate = GETDATE()
                    WHERE TransactionId = @TransactionId;
                ELSE
                    INSERT INTO DataRawJson
                        (TransactionId, DataType, Message, Flag, ErrorMessage, [Source], CrtDate, Id)
                    VALUES
                        (@TransactionId, @DataType, @Message, @Flag, @ErrorMessage, @Source, GETDATE(), NEWID());";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                TransactionId = StringHelper.Left(transactionId, 30),
                DataType      = StringHelper.Left(dataType, 20),
                Message       = message,
                Flag          = flag,
                ErrorMessage  = errorMessage,
                Source        = StringHelper.Left(source, 20)
            }, commandTimeout: Timeout));
        }
        catch
        {
            // Swallow — nếu log fail mà main processing cũng fail,
            // PushSalesToTopic() sẽ đẩy sang RabbitMQ retry tự động.
        }
    }
    public static async Task RegisterExecuteAsync(IDbConnection dbContext, string POSTerminal)
    {
        if (!string.IsNullOrEmpty(POSTerminal))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@POSTerminal", POSTerminal);

            await dbContext.ExecuteAsync(
                "Register_Insert",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30
            );
        }
        else
        {
        }
    }
    // ── Revenue Dashboard ─────────────────────────────────────────────────────

    public async Task<List<RevenueDailyDto>> GetRevenueDailyAsync(DateTime fromDate, DateTime toDate,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT CAST(OrderDate AS DATE) AS SaleDate,
                       SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                ELSE 0 END) AS NetRevenue,
                       COUNT(CASE WHEN TransactionType = 1 THEN 1 END) AS SaleCount,
                       COUNT(CASE WHEN TransactionType IN (2,3) THEN 1 END) AS ReturnCount
                FROM TransHeader (NOLOCK)
                WHERE OrderDate >= @FromDate AND OrderDate < @ToDate
                {storeFilter}
                GROUP BY CAST(OrderDate AS DATE)
                ORDER BY SaleDate;";
            var p = new DynamicParameters();
            p.Add("FromDate", fromDate.Date);
            p.Add("ToDate", toDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);
            var data = await conn.QueryAsync<RevenueDailyDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueDaily", ex);
            return [];
        }
    }

    public async Task<List<RevenueHourlyDto>> GetRevenueHourlyAsync(DateTime saleDate,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT DATEPART(HOUR, OrderTime) AS Hour,
                       SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                ELSE 0 END) AS NetRevenue,
                       COUNT(*) AS TransactionCount
                FROM TransHeader (NOLOCK)
                WHERE CAST(OrderDate AS DATE) = @SaleDate
                {storeFilter}
                GROUP BY DATEPART(HOUR, OrderTime)
                ORDER BY Hour;";
            var p = new DynamicParameters();
            p.Add("SaleDate", saleDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);
            var data = await conn.QueryAsync<RevenueHourlyDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueHourly", ex);
            return [];
        }
    }

    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime today,
        IReadOnlyList<string>? storeCodes = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT
                    ISNULL(SUM(CASE WHEN TransactionType = 1 THEN AmountInclVAT
                                    WHEN TransactionType IN (2,3) THEN -AmountInclVAT
                                    ELSE 0 END), 0) AS TodayRevenue,
                    COUNT(CASE WHEN TransactionType = 1 THEN 1 END) AS TodayOrders,
                    COUNT(CASE WHEN TransactionType IN (2,3) THEN 1 END) AS TodayReturns
                FROM TransHeader (NOLOCK)
                WHERE CAST(OrderDate AS DATE) = @Today
                {storeFilter};";
            var p = new DynamicParameters();
            p.Add("Today", today.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);
            var result = await conn.QueryFirstOrDefaultAsync<RevenueSummaryDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return result ?? new RevenueSummaryDto();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetRevenueSummary", ex);
            return new RevenueSummaryDto();
        }
    }

    // ── Transaction Dashboard ─────────────────────────────────────────────────

    // ── EOS Shift Dashboard ───────────────────────────────────────────────────

    public async Task<List<EosShiftDto>> GetEosShiftListAsync(
        DateTime businessDate,
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND h.StoreNo IN @StoreCodes" : "";
            // TODO: Xác minh TenderType code cho tiền mặt tại DB (SELECT DISTINCT TenderType FROM TransPaymentEntry)
            var sql = $@"
                SELECT
                    h.StoreNo,
                    h.PosTerminal,
                    CAST(h.BussinessDate AS DATE)       AS BussinessDate,
                    CAST(h.ShiftNumber  AS INT)          AS ShiftNumber,
                    h.StaffCode,
                    h.OpenShiftDate,
                    h.BeginAmount,
                    ISNULL(sl.TienMat,      0)           AS TienMat,
                    ISNULL(rv.TienHeThong,  0)           AS TienHeThong,
                    ISNULL(sl.TienMat, 0)
                        - h.BeginAmount
                        - ISNULL(rv.TienHeThong, 0)      AS ChenLech,
                    ISNULL(rv.TotalRevenue,    0)        AS TotalRevenue,
                    ISNULL(rv.TransactionCount,0)        AS TransactionCount,
                    h.CloseShiftDate,
                    h.IsShiftClosed
                FROM POSShiftHeader h (NOLOCK)

                LEFT JOIN (
                    SELECT ShiftCode, SUM(CashAmount) AS TienMat
                    FROM   POSShiftLine (NOLOCK)
                    GROUP BY ShiftCode
                ) sl ON sl.ShiftCode = h.ShiftCode

                LEFT JOIN (
                    SELECT
                        th.POSTerminalNo,
                        CAST(th.ShiftNo AS INT)                                                          AS ShiftNo,
                        SUM(IIF(th.TransactionType = 1, th.AmountInclVAT, th.AmountInclVAT * (-1)))     AS TienHeThong,
                        SUM(CASE
                            WHEN th.TransactionType = 1       THEN  th.AmountInclVAT
                            WHEN th.TransactionType IN (2, 3) THEN -th.AmountInclVAT
                            ELSE 0
                        END)                                                                             AS TotalRevenue,
                        COUNT(CASE WHEN th.TransactionType = 1 THEN 1 END)                              AS TransactionCount
                    FROM TransHeader th (NOLOCK)
                    WHERE CAST(th.OrderDate AS DATE) = @BusinessDate
                    GROUP BY th.POSTerminalNo, CAST(th.ShiftNo AS INT)
                ) rv ON rv.POSTerminalNo = h.PosTerminal
                    AND rv.ShiftNo       = CAST(h.ShiftNumber AS INT)

                WHERE CAST(h.BussinessDate AS DATE) = @BusinessDate
                  {storeFilter}
                ORDER BY h.StoreNo, h.PosTerminal;";

            var p = new DynamicParameters();
            p.Add("BusinessDate", businessDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);

            var data = await conn.QueryAsync<EosShiftDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetEosShiftList", ex);
            return [];
        }
    }

    public async Task<List<EosDayDto>> GetEosDayListAsync(
        DateTime fromDate, DateTime toDate,
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND h.StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT
                    h.StoreNo,
                    CAST(h.BussinessDate AS DATE)                                             AS BussinessDate,
                    COUNT(*)                                                                   AS TotalShifts,
                    SUM(CASE WHEN h.IsShiftClosed = 1 THEN 1 ELSE 0 END)                     AS ClosedShifts,
                    SUM(CASE WHEN h.IsShiftClosed = 0 THEN 1 ELSE 0 END)                     AS OpenShifts,
                    SUM(ISNULL(rv.TotalRevenue,     0))                                       AS TotalRevenue,
                    SUM(ISNULL(rv.TransactionCount, 0))                                       AS TotalTransactions,
                    SUM(ISNULL(rv.TienHeThong,      0))                                       AS TotalTienHeThong,
                    SUM(ISNULL(sl.TienMat,          0))                                       AS TotalTienMat,
                    SUM(ISNULL(sl.TienMat, 0) - h.BeginAmount - ISNULL(rv.TienHeThong, 0))   AS TotalChenLech
                FROM POSShiftHeader h (NOLOCK)

                LEFT JOIN (
                    SELECT ShiftCode, SUM(CashAmount) AS TienMat
                    FROM   POSShiftLine (NOLOCK)
                    GROUP BY ShiftCode
                ) sl ON sl.ShiftCode = h.ShiftCode

                LEFT JOIN (
                    SELECT
                        th.POSTerminalNo,
                        CAST(th.ShiftNo AS INT)                                                          AS ShiftNo,
                        SUM(IIF(th.TransactionType = 1, th.AmountInclVAT, th.AmountInclVAT * (-1)))     AS TienHeThong,
                        SUM(CASE
                            WHEN th.TransactionType = 1       THEN  th.AmountInclVAT
                            WHEN th.TransactionType IN (2, 3) THEN -th.AmountInclVAT
                            ELSE 0
                        END)                                                                             AS TotalRevenue,
                        COUNT(CASE WHEN th.TransactionType = 1 THEN 1 END)                              AS TransactionCount
                    FROM TransHeader th (NOLOCK)
                    WHERE CAST(th.OrderDate AS DATE) BETWEEN @FromDate AND @ToDate
                    GROUP BY th.POSTerminalNo, CAST(th.ShiftNo AS INT)
                ) rv ON rv.POSTerminalNo = h.PosTerminal
                    AND rv.ShiftNo       = CAST(h.ShiftNumber AS INT)

                WHERE CAST(h.BussinessDate AS DATE) BETWEEN @FromDate AND @ToDate
                  {storeFilter}
                GROUP BY h.StoreNo, CAST(h.BussinessDate AS DATE)
                ORDER BY BussinessDate DESC, h.StoreNo;";

            var p = new DynamicParameters();
            p.Add("FromDate", fromDate.Date);
            p.Add("ToDate",   toDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);

            var data = await conn.QueryAsync<EosDayDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetEosDayList", ex);
            return [];
        }
    }

    // ── Business Day Confirm (Xác nhận kết thúc ngày) ────────────────────────
    // Ported from src/legacy/VCM.BLUEPOS.Data/StoreActivities/StoreActivitiesData.cs
    // (SaleBusinessStoreStaging, dòng 219 — gọi SP SP_END_DATE_CONFIRM_STAGING đã compile sẵn,
    // không có script để đối chiếu chính xác). Viết lại từ TransHeader/TransLine/POSShiftHeader/
    // POSShiftLine/POSEOD_API hiện có, theo đúng connection per-store (giống CentralSalesContainer
    // cũ routed theo ServerIPConnection.GetIPServerByStore). "Đã đóng ngày" = có dòng POSEOD_API
    // cho (Store, Terminal, Ngày) — terminal tự báo cáo qua API UpdatePOSEOD hiện có.
    public async Task<List<PosDayStagingDto>> GetPosDayStagingAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo, ct: ct);
            // SP dbo.GetSalesEODConfirm trả cột theo tên legacy (TerminalID, AmountTotal, CashMoney…) —
            // map thủ công sang PosDayStagingDto (tên property khác) để Dapper không để trống PosTerminal.
            var rows = await conn.QueryAsync<SalesEodConfirmRow>(new CommandDefinition(
                "GetSalesEODConfirm",
                new { StoreNo = storeNo, BusinessDate = businessDate.Date },
                commandType: CommandType.StoredProcedure,
                commandTimeout: Timeout, cancellationToken: ct));

            return rows.Select(r => new PosDayStagingDto
            {
                PosTerminal   = r.TerminalID ?? string.Empty,
                IsClosed      = r.IsClosed ?? false,
                CloseTime     = r.CloseDate,
                LastSaleTime  = r.LastOrderTime,
                TotalRevenue  = r.AmountTotal ?? 0m,
                TienMat       = r.CashMoney ?? 0m,
                CustomerCount = r.CountCustomer ?? 0,
                TotalQuantity = r.CountOrderNo ?? 0,   // TODO confirm: SP không có cột "số lượng bán" (item qty);
                                                       // CountOrderNo = số đơn — dùng tạm cho cột "Số lượng bán"
            }).ToList();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetPosDayStaging", ex);
            return [];
        }
    }

    // Cột SP dbo.GetSalesEODConfirm (đặt tên theo legacy SaleBusinessStoreModel) — dùng làm class
    // trung gian cho Dapper rồi project sang PosDayStagingDto ở GetPosDayStagingAsync.
    private sealed class SalesEodConfirmRow
    {
        public string? TerminalID { get; set; }
        public bool? IsClosed { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime? LastOrderTime { get; set; }
        public decimal? AmountTotal { get; set; }
        public decimal? CashMoney { get; set; }
        public int? CountCustomer { get; set; }
        public int? CountOrderNo { get; set; }
    }

    public async Task<BusinessDayConfirmDto?> GetBusinessDayConfirmAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo, ct: ct);
            const string sql = @"SELECT TOP 1 StoreNo, BusinessDate, TotalRevenue, TotalShifts, ConfirmedBy, ConfirmedDate
                                 FROM dbo.BusinessDayConfirm (NOLOCK)
                                 WHERE StoreNo = @StoreNo AND BusinessDate = @BusinessDate;";
            return await conn.QueryFirstOrDefaultAsync<BusinessDayConfirmDto>(new CommandDefinition(
                sql, new { StoreNo = storeNo, BusinessDate = businessDate.Date },
                commandTimeout: Timeout, cancellationToken: ct));
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetBusinessDayConfirm", ex);
            return null;
        }
    }

    // SP dbo.usp_BusinessDay_ConfirmEndDate (docs/sql/BusinessDay_ConfirmEndDate.sql) — 1 transaction:
    // insert ledger + advance BussinessDateOpen (+1 ngày) trên cùng DB CentralSale của store.
    public async Task<ConfirmBusinessDayResult> ConfirmBusinessDayAsync(
        ConfirmBusinessDayRequest request, CancellationToken ct = default)
    {
        try
        {
            using var conn = await connectionFactory.CreateOpenConnectionAsync(request.StoreNo, ct: ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_BusinessDay_ConfirmEndDate",
                new
                {
                    request.StoreNo,
                    BusinessDate = request.BusinessDate.Date,
                    request.TotalRevenue,
                    request.TotalShifts,
                    request.ConfirmedBy
                },
                commandType: CommandType.StoredProcedure, commandTimeout: Timeout, cancellationToken: ct));

            return new ConfirmBusinessDayResult
            {
                Success = true,
                Message = "Xác nhận kết thúc ngày thành công.",
                NewBusinessDate = request.BusinessDate.Date.AddDays(1)
            };
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50002)
        {
            return new ConfirmBusinessDayResult { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("ConfirmBusinessDay", ex);
            return new ConfirmBusinessDayResult { Success = false, Message = "Lỗi hệ thống. Vui lòng thử lại." };
        }
    }

    public async Task<List<ShiftNumberSummaryDto>> GetShiftNumberSummaryAsync(
        DateTime fromDate, DateTime toDate,
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND h.StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT
                    CAST(h.ShiftNumber AS INT)                                                AS ShiftNumber,
                    COUNT(*)                                                                  AS TotalOccurrences,
                    SUM(CASE WHEN h.IsShiftClosed = 1 THEN 1 ELSE 0 END)                    AS ClosedCount,
                    SUM(CASE WHEN h.IsShiftClosed = 0 THEN 1 ELSE 0 END)                    AS OpenCount,
                    SUM(ISNULL(rv.TotalRevenue,     0))                                      AS TotalRevenue,
                    SUM(ISNULL(rv.TransactionCount, 0))                                      AS TotalTransactions,
                    SUM(CASE WHEN (ISNULL(sl.TienMat, 0) - h.BeginAmount
                                   - ISNULL(rv.TienHeThong, 0)) <> 0 THEN 1 ELSE 0 END)    AS DiscrepancyCount,
                    SUM(ISNULL(sl.TienMat, 0) - h.BeginAmount - ISNULL(rv.TienHeThong, 0)) AS TotalChenLech
                FROM POSShiftHeader h (NOLOCK)

                LEFT JOIN (
                    SELECT ShiftCode, SUM(CashAmount) AS TienMat
                    FROM   POSShiftLine (NOLOCK)
                    GROUP BY ShiftCode
                ) sl ON sl.ShiftCode = h.ShiftCode

                LEFT JOIN (
                    SELECT
                        th.POSTerminalNo,
                        CAST(th.ShiftNo AS INT)                                                          AS ShiftNo,
                        SUM(IIF(th.TransactionType = 1, th.AmountInclVAT, th.AmountInclVAT * (-1)))     AS TienHeThong,
                        SUM(CASE
                            WHEN th.TransactionType = 1       THEN  th.AmountInclVAT
                            WHEN th.TransactionType IN (2, 3) THEN -th.AmountInclVAT
                            ELSE 0
                        END)                                                                             AS TotalRevenue,
                        COUNT(CASE WHEN th.TransactionType = 1 THEN 1 END)                              AS TransactionCount
                    FROM TransHeader th (NOLOCK)
                    WHERE CAST(th.OrderDate AS DATE) BETWEEN @FromDate AND @ToDate
                    GROUP BY th.POSTerminalNo, CAST(th.ShiftNo AS INT)
                ) rv ON rv.POSTerminalNo = h.PosTerminal
                    AND rv.ShiftNo       = CAST(h.ShiftNumber AS INT)

                WHERE CAST(h.BussinessDate AS DATE) BETWEEN @FromDate AND @ToDate
                  {storeFilter}
                GROUP BY CAST(h.ShiftNumber AS INT)
                ORDER BY ShiftNumber;";

            var p = new DynamicParameters();
            p.Add("FromDate", fromDate.Date);
            p.Add("ToDate",   toDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);

            var data = await conn.QueryAsync<ShiftNumberSummaryDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetShiftNumberSummary", ex);
            return [];
        }
    }

    public async Task<List<EosShiftDto>> GetEosShiftRangeAsync(
        DateTime fromDate, DateTime toDate,
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            var storeFilter = storeCodes?.Count > 0 ? "AND h.StoreNo IN @StoreCodes" : "";
            var sql = $@"
                SELECT
                    h.StoreNo,
                    h.PosTerminal,
                    CAST(h.BussinessDate AS DATE)       AS BussinessDate,
                    CAST(h.ShiftNumber  AS INT)          AS ShiftNumber,
                    h.StaffCode,
                    h.OpenShiftDate,
                    h.BeginAmount,
                    ISNULL(sl.TienMat,     0)            AS TienMat,
                    ISNULL(rv.TienHeThong, 0)            AS TienHeThong,
                    ISNULL(sl.TienMat, 0)
                        - h.BeginAmount
                        - ISNULL(rv.TienHeThong, 0)      AS ChenLech,
                    ISNULL(rv.TotalRevenue,    0)        AS TotalRevenue,
                    ISNULL(rv.TransactionCount,0)        AS TransactionCount,
                    h.CloseShiftDate,
                    h.IsShiftClosed
                FROM POSShiftHeader h (NOLOCK)

                LEFT JOIN (
                    SELECT ShiftCode, SUM(CashAmount) AS TienMat
                    FROM   POSShiftLine (NOLOCK)
                    GROUP BY ShiftCode
                ) sl ON sl.ShiftCode = h.ShiftCode

                LEFT JOIN (
                    SELECT
                        th.POSTerminalNo,
                        CAST(th.ShiftNo AS INT)                                                          AS ShiftNo,
                        SUM(IIF(th.TransactionType = 1, th.AmountInclVAT, th.AmountInclVAT * (-1)))     AS TienHeThong,
                        SUM(CASE
                            WHEN th.TransactionType = 1       THEN  th.AmountInclVAT
                            WHEN th.TransactionType IN (2, 3) THEN -th.AmountInclVAT
                            ELSE 0
                        END)                                                                             AS TotalRevenue,
                        COUNT(CASE WHEN th.TransactionType = 1 THEN 1 END)                              AS TransactionCount
                    FROM TransHeader th (NOLOCK)
                    WHERE CAST(th.OrderDate AS DATE) BETWEEN @FromDate AND @ToDate
                    GROUP BY th.POSTerminalNo, CAST(th.ShiftNo AS INT)
                ) rv ON rv.POSTerminalNo = h.PosTerminal
                    AND rv.ShiftNo       = CAST(h.ShiftNumber AS INT)

                WHERE CAST(h.BussinessDate AS DATE) BETWEEN @FromDate AND @ToDate
                  {storeFilter}
                ORDER BY h.StoreNo, h.PosTerminal, CAST(h.BussinessDate AS DATE) DESC, CAST(h.ShiftNumber AS INT);";

            var p = new DynamicParameters();
            p.Add("FromDate", fromDate.Date);
            p.Add("ToDate",   toDate.Date);
            if (storeCodes?.Count > 0) p.Add("StoreCodes", storeCodes);

            var data = await conn.QueryAsync<EosShiftDto>(
                new CommandDefinition(sql, p, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetEosShiftRange", ex);
            return [];
        }
    }

    public async Task<List<TransactionListDto>> GetTransactionListAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? orderNo, int maxRows = 500, CancellationToken ct = default)
    {
        try
        {
            string? normalizedStore = string.IsNullOrWhiteSpace(storeNo) ? null : storeNo.Trim();
            string? normalizedOrderNo = string.IsNullOrWhiteSpace(orderNo) ? null : orderNo.Trim();

            var sql = $@"
                SELECT TOP (@MaxRows)
                    OrderNo, OrderDate, OrderTime, StoreNo, POSTerminalNo,
                    ISNULL(DiscountAmount, 0) AS DiscountAmount,
                    AmountInclVAT, TransactionType, CreatedDate, MemberCardNo
                FROM TransHeader (NOLOCK)
                WHERE CAST(OrderDate AS DATE) >= @FromDate AND CAST(OrderDate AS DATE) <= @ToDate
                  AND (@StoreNo IS NULL OR StoreNo = @StoreNo)
                  AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%')
                ORDER BY CreatedDate DESC;";

            var param = new
            {
                MaxRows  = maxRows,
                FromDate = fromDate.Date,
                ToDate   = toDate.Date,
                StoreNo  = normalizedStore,
                OrderNo  = normalizedOrderNo
            };

            // Route to store shard when storeNo specified; otherwise use central connection
            System.Data.IDbConnection conn = normalizedStore is not null
                ? await connectionFactory.CreateOpenConnectionAsync(normalizedStore, ct: ct)
                : await directConnectionFactory.CreateOpenConnectionAsync(ct);

            using (conn)
            {
                var data = await conn.QueryAsync<TransactionListDto>(
                    new CommandDefinition(sql, param, commandTimeout: Timeout, cancellationToken: ct));
                return [.. data];
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTransactionList", ex);
            return [];
        }
    }

    public async Task<List<ValidateTransactionLine>> GetTransLinesAsync(string orderNo, CancellationToken ct = default)
    {
        try
        {
            var siteCode = orderNo.Substring(0, 4);
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            const string sql = @"SELECT DISTINCT [LineNo], ItemNo, [Description] ItemName, UnitOfMeasure, Quantity, UnitPrice,
                                        DiscountAmount, VATPercent, VATAmount, LineAmountIncVAT, DivisionCode, Barcode
                                 FROM TransLine (NOLOCK)
                                 WHERE LineType = 0 AND DocumentNo = @OrderNo
                                 ORDER BY [LineNo]";
            var data = await conn.QueryAsync<ValidateTransactionLine>(
                new CommandDefinition(sql, new { OrderNo = orderNo }, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTransLines", ex);
            return [];
        }
    }

    public async Task<List<TransPaymentEntryDto>> GetTransPaymentEntriesAsync(string orderNo, CancellationToken ct = default)
    {
        try
        {
            var siteCode = orderNo.Substring(0, 4);
            using var conn = await connectionFactory.CreateOpenConnectionAsync(siteCode, ct: ct);
            const string sql = @"SELECT TenderType, TenderTypeName AS Description, AmountTendered, CardNo, ApprovalCode AS AppCode,
                                        BankPOSCode AS POSBankTerminalNo, BankCardType AS CardType, PaymentType AS BankPayment
                                 FROM TransPaymentEntry (NOLOCK)
                                 WHERE OrderNo = @OrderNo
                                 ORDER BY [LineNo]";
            var data = await conn.QueryAsync<TransPaymentEntryDto>(
                new CommandDefinition(sql, new { OrderNo = orderNo }, commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetTransPaymentEntries", ex);
            return [];
        }
    }

    // ── Void Transaction Dashboard (InvoiceVoid + LineVoid hợp nhất) ───────────
    // TransVoidLine là driving table (grain mịn nhất — 1 dòng = 1 sản phẩm bị hủy).
    // LEFT JOIN TransVoidHeader để phân loại VoidType; LEFT JOIN TransHeader để lấy context
    // (Store/POS/ngày/số tiền đơn gốc) cho các dòng LineVoid (đơn không bị hủy cả hóa đơn).

    public async Task<List<VoidReportLineDto>> GetVoidReportAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? orderNo = null, string? userVoid = null, string? posNo = null,
        string? voidType = null, int maxRows = 1000, CancellationToken ct = default)
    {
        try
        {
            string? normalizedStore = string.IsNullOrWhiteSpace(storeNo) ? null : storeNo.Trim();
            string? normalizedOrderNo = string.IsNullOrWhiteSpace(orderNo) ? null : orderNo.Trim();
            string? normalizedUserVoid = string.IsNullOrWhiteSpace(userVoid) ? null : userVoid.Trim();
            string? normalizedPosNo = string.IsNullOrWhiteSpace(posNo) ? null : posNo.Trim();
            string? normalizedVoidType = string.IsNullOrWhiteSpace(voidType) ? null : voidType.Trim();

            const string sql = @"
                SELECT TOP (@MaxRows)
                    vl.DocumentNo                                                 AS OrderNo,
                    vl.[LineNo],
                    CASE WHEN vh.OrderNo IS NOT NULL THEN 'InvoiceVoid' ELSE 'LineVoid' END AS VoidType,
                    vl.ScanTime,
                    COALESCE(vh.StoreNo, th.StoreNo)                              AS StoreNo,
                    COALESCE(vh.POSTerminalNo, th.POSTerminalNo, vl.OrigTransPos) AS POSTerminalNo,
                    vl.ItemNo, vl.Description AS ItemName, vl.UnitOfMeasure,
                    vl.Quantity, vl.UnitPrice, ISNULL(vl.DiscountAmount, 0) AS DiscountAmount,
                    vl.VATPercent, vl.VATAmount, vl.LineAmountIncVAT,
                    COALESCE(vh.AmountInclVAT, th.AmountInclVAT)                  AS OrderAmountInclVAT,
                    COALESCE(vh.TransactionType, th.TransactionType)              AS TransactionType,
                    COALESCE(vh.UserVoid, vl.StaffID)                            AS UserVoid,
                    vh.CashierID, vh.ShiftNo,
                    COALESCE(vh.Note, vl.Note)                                    AS Note,
                    COALESCE(vh.MemberCardNo, th.MemberCardNo)                    AS MemberCardNo,
                    vl.DivisionCode, vl.Barcode
                FROM TransVoidLine vl (NOLOCK)
                LEFT JOIN TransVoidHeader vh (NOLOCK) ON vh.OrderNo = vl.DocumentNo
                LEFT JOIN TransHeader th (NOLOCK) ON th.OrderNo = vl.DocumentNo
                WHERE CAST(vl.ScanTime AS DATE) >= @FromDate AND CAST(vl.ScanTime AS DATE) <= @ToDate
                  AND (@StoreNo IS NULL OR COALESCE(vh.StoreNo, th.StoreNo) = @StoreNo)
                  AND (@OrderNo IS NULL OR vl.DocumentNo LIKE '%' + @OrderNo + '%')
                  AND (@UserVoid IS NULL OR COALESCE(vh.UserVoid, vl.StaffID) LIKE '%' + @UserVoid + '%')
                  AND (@PosNo IS NULL OR COALESCE(vh.POSTerminalNo, th.POSTerminalNo, vl.OrigTransPos) = @PosNo)
                  AND (@VoidType IS NULL OR CASE WHEN vh.OrderNo IS NOT NULL THEN 'InvoiceVoid' ELSE 'LineVoid' END = @VoidType)
                ORDER BY vl.ScanTime DESC;";

            var param = new
            {
                MaxRows  = maxRows,
                FromDate = fromDate.Date,
                ToDate   = toDate.Date,
                StoreNo  = normalizedStore,
                OrderNo  = normalizedOrderNo,
                UserVoid = normalizedUserVoid,
                PosNo    = normalizedPosNo,
                VoidType = normalizedVoidType
            };

            // Route to store shard when storeNo specified; otherwise use central connection
            System.Data.IDbConnection conn = normalizedStore is not null
                ? await connectionFactory.CreateOpenConnectionAsync(normalizedStore, ct: ct)
                : await directConnectionFactory.CreateOpenConnectionAsync(ct);

            using (conn)
            {
                var data = await conn.QueryAsync<VoidReportLineDto>(
                    new CommandDefinition(sql, param, commandTimeout: Timeout, cancellationToken: ct));
                return [.. data];
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetVoidReport", ex);
            return [];
        }
    }

    // ── Interface_Errors Log ──────────────────────────────────────────────────

    public async Task InsertInterfaceErrorAsync(
        string? userName, string? errorProcedure, string? errorMessage,
        int? errorNumber = null, int? errorSeverity = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"INSERT INTO Interface_Errors
                                     (UserName, ErrorNumber, ErrorSeverity, ErrorProcedure, ErrorMessage, ErrorDateTime)
                                 VALUES (@UserName, @ErrorNumber, @ErrorSeverity, @ErrorProcedure, @ErrorMessage, GETDATE());";
            await conn.ExecuteAsync(new CommandDefinition(sql,
                new { UserName = userName, ErrorNumber = errorNumber, ErrorSeverity = errorSeverity,
                      ErrorProcedure = errorProcedure, ErrorMessage = errorMessage },
                commandTimeout: Timeout, cancellationToken: ct));
        }
        catch
        {
            // Swallow — never throw when logging an error

        }
    }

    public async Task<InterfaceErrorSummaryDto> GetInterfaceErrorSummaryAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT COUNT(*) AS TotalErrors,
                       COUNT(CASE WHEN CAST(ErrorDateTime AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) AS TodayErrors,
                       COUNT(DISTINCT ErrorProcedure) AS UniqueProcedures,
                       MAX(ErrorDateTime) AS LastErrorAt
                FROM Interface_Errors (NOLOCK)
                WHERE ErrorDateTime >= @FromDate AND ErrorDateTime < @ToDate;";
            var result = await conn.QueryFirstOrDefaultAsync<InterfaceErrorSummaryDto>(
                new CommandDefinition(sql, new { FromDate = fromDate.Date, ToDate = toDate.Date.AddDays(1) },
                    commandTimeout: Timeout, cancellationToken: ct));
            return result ?? new InterfaceErrorSummaryDto();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetInterfaceErrorSummary", ex);
            return new InterfaceErrorSummaryDto();
        }
    }

    public async Task<List<InterfaceErrorDto>> GetInterfaceErrorsAsync(
        DateTime fromDate, DateTime toDate,
        string? procedure = null, int maxRows = 200,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            string? normalizedProc = string.IsNullOrWhiteSpace(procedure) ? null : procedure.Trim();
            const string sql = @"
                SELECT TOP (@MaxRows)
                    ErrorID, UserName, ErrorNumber, ErrorState, ErrorSeverity,
                    ErrorLine, ErrorProcedure, ErrorMessage, ErrorDateTime
                FROM Interface_Errors (NOLOCK)
                WHERE ErrorDateTime >= @FromDate AND ErrorDateTime < @ToDate
                  AND (@Procedure IS NULL OR ErrorProcedure LIKE '%' + @Procedure + '%')
                ORDER BY ErrorDateTime DESC;";
            var data = await conn.QueryAsync<InterfaceErrorDto>(
                new CommandDefinition(sql,
                    new { MaxRows = maxRows, FromDate = fromDate.Date, ToDate = toDate.Date.AddDays(1),
                          Procedure = normalizedProc },
                    commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetInterfaceErrors", ex);
            return [];
        }
    }

    public async Task<DataRawJsonSummaryDto> GetDataRawJsonSummaryAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT
                    COUNT(*) AS TotalInPeriod,
                    SUM(CASE WHEN CAST(CrtDate AS date) = CAST(GETDATE() AS date) THEN 1 ELSE 0 END) AS TotalToday,
                    SUM(CASE WHEN Flag = 1 THEN 1 ELSE 0 END) AS TotalSuccess,
                    SUM(CASE WHEN Flag = 0 THEN 1 ELSE 0 END) AS TotalFailed,
                    MAX(CrtDate) AS LastCrtDate
                FROM DataRawJson (NOLOCK)
                WHERE CrtDate >= @FromDate AND CrtDate < @ToDate;";
            return await conn.QueryFirstOrDefaultAsync<DataRawJsonSummaryDto>(
                new CommandDefinition(sql,
                    new { FromDate = fromDate.Date, ToDate = toDate.Date.AddDays(1) },
                    commandTimeout: Timeout, cancellationToken: ct))
                ?? new DataRawJsonSummaryDto();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetDataRawJsonSummary", ex);
            return new DataRawJsonSummaryDto();
        }
    }

    public async Task<List<DataRawJsonLogDto>> GetDataRawJsonListAsync(
        DateTime fromDate, DateTime toDate,
        string? dataType = null, bool? flag = null, int maxRows = 100,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory.CreateOpenConnectionAsync(ct);
            string? normalizedType = string.IsNullOrWhiteSpace(dataType) ? null : dataType.Trim();
            const string sql = @"
                SELECT TOP (@MaxRows)
                    Id, TransactionId, DataType, Message, Flag, ErrorMessage, [Source], CrtDate
                FROM DataRawJson (NOLOCK)
                WHERE CrtDate >= @FromDate AND CrtDate < @ToDate
                  AND (@DataType IS NULL OR DataType LIKE '%' + @DataType + '%')
                  AND (@Flag IS NULL OR Flag = @Flag)
                ORDER BY CrtDate DESC;";
            var data = await conn.QueryAsync<DataRawJsonLogDto>(
                new CommandDefinition(sql,
                    new { MaxRows = maxRows, FromDate = fromDate.Date, ToDate = toDate.Date.AddDays(1),
                          DataType = normalizedType, Flag = flag },
                    commandTimeout: Timeout, cancellationToken: ct));
            return [.. data];
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("GetDataRawJsonList", ex);
            return [];
        }
    }
}

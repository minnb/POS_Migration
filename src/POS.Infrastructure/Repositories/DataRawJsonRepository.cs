using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using POS.Common.Dtos;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.POS;
using POS.Common.Dtos.StagingDB;
using POS.Common.Dtos.Tax;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class DataRawJsonRepository(
    CentralMDConnectionFactory connectionFactory,
    IRedisService redis)
    : BaseRepository(connectionFactory), IDataRawJsonRepository
{
    private static string InvoiceCheckKey(string orderNo)   => "InvoiceCreated:CHECK_"   + orderNo;
    private static string InvoicePrimaryKey(string orderNo) => "InvoiceCreated:PRIMARY_" + orderNo;

    // Validates that the string contains only alphanumeric characters and spaces
    private static bool IsValidOrderNo(string? value)
        => value != null && Regex.IsMatch(value, @"^[a-zA-Z0-9 ]*$");

    private static TimeSpan TimeUntilMidnight()
    {
        var now = DateTime.Now;
        return now.AddDays(1).Date - now;
    }

    private static async Task<IDbConnection> OpenStoreConnectionAsync(string connectionString, CancellationToken ct)
    {
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task<(bool Success, string Message, string? Detail)> InsertInvoiceCreatedAsync(
        List<InvoiceCreated> invoiceCreated, string connectStringDb, CancellationToken ct = default)
    {
        try
        {
            var sanitizedInvoices = new List<InvoiceCreated>();
            foreach (var item in invoiceCreated)
            {
                if (!IsValidOrderNo(item.OrderNo))
                    return (false, $"Phiếu tính tiền {item.OrderNo} sai định dạng", null);

                if (string.IsNullOrEmpty(redis.StringGetRaw(InvoiceCheckKey(item.OrderNo!))))
                    return (false, $"Phiếu tính tiền {item.OrderNo} không hợp lệ", null);

                if (!string.IsNullOrEmpty(redis.StringGetRaw(InvoicePrimaryKey(item.OrderNo!))))
                    return (false, $"Phiếu tính tiền {item.OrderNo} đã được nhập thông tin HD GTGT", null);

                sanitizedInvoices.Add(item);
            }

            const string sql = @"INSERT INTO EInvoice.[dbo].[InvoiceCreated_Temp]
                ([SiteNo],[OrderNo],[CustomerName],[CompanyName],[TaxCode],[PhoneNumber],[Email],[Address],
                 [IsNotVAT],[IsNotVATPartner],[CreatedUser],[CreatedDate],[Id],Passport,CCCD,DVQHNS)
                VALUES (@SiteNo,@OrderNo,@CustomerName,@CompanyName,@TaxCode,@PhoneNumber,@Email,@Address,
                        @IsNotVAT,@IsNotVATPartner,'WEB',getdate(),@Id,@Passport,@CCCD,@DVQHNS)";

            using var conn = await OpenStoreConnectionAsync(connectStringDb, ct);
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(new CommandDefinition(sql, sanitizedInvoices, tx, commandTimeout: 3600, cancellationToken: ct));
                tx.Commit();

                _ = Task.Run(() =>
                {
                    foreach (var item in sanitizedInvoices)
                        redis.Delete(InvoiceCheckKey(item.OrderNo!));
                });

                return (true, "OK", "OK");
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                tx.Rollback();
                var duplicateKey = ExtractDuplicateKey(ex.Message);
                redis.StringSetRaw(InvoicePrimaryKey(duplicateKey), JsonConvert.SerializeObject(sanitizedInvoices), TimeUntilMidnight());
                return (false, $"Phiếu tính tiền {duplicateKey} đã được nhập thông tin HD GTGT", null);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, JsonConvert.SerializeObject(ex));
        }
    }

    public async Task<(bool Success, string Message, object? Data, HttpStatusCode StatusCode)> ValidateTransactionAsync(
        StoreDto storeInfo, string connectStringDb, string orderNo, string appCode = "WCM", CancellationToken ct = default)
    {
        try
        {
            using var conn = await OpenStoreConnectionAsync(connectStringDb, ct);
            try
            {
                string queryTransHeader = appCode switch
                {
                    "PLH" => @"SELECT OrderNo, OrderDate, OrderTime, TransactionType, DeliveringMethod, SalesIsReturn,
                                      ReturnedOrderNo, CreatedDate, RefKey1, RefKey5 AS CQT, CustomerName, MemberCardNo
                               FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo;",
                    _ =>     @"SELECT OrderNo, OrderDate, OrderTime, TransactionType, DeliveringMethod, SalesIsReturn,
                                      ReturnedOrderNo, CreatedDate, RefKey1, Ref4 AS CQT, CustomerName, MemberCardNo,
                                      ISNULL(MemberPointsEarn,0) MemberPointsEarn
                               FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo;"
                };

                // 1. Check exists in InvoiceCreated_Temp
                var checkTransaction = await conn.QueryFirstOrDefaultAsync<InvoiceCreated>(
                    new CommandDefinition(
                        "SELECT OrderNo, TaxCode, CustomerName FROM EInvoice.[dbo].[InvoiceCreated_Temp] (NOLOCK) WHERE OrderNo = @orderNo;",
                        new { orderNo }, cancellationToken: ct));

                if (checkTransaction != null)
                {
                    string mes = await GetMessageWarningsVATCheckAsync("ExistsTemp", ct)
                        ?? "Phiếu tính tiền không hợp lệ";
                    mes += !string.IsNullOrEmpty(checkTransaction.TaxCode)
                        ? $" cho MST {checkTransaction.TaxCode}"
                        : $" cho khách hàng {checkTransaction.CustomerName}";

                    var cashOrder1 = await conn.QueryFirstOrDefaultAsync<Cash_OrderNoSentToEInvoice>(
                        new CommandDefinition(
                            "SELECT * FROM EInvoice.[dbo].Cash_OrderNoSentToEInvoice (NOLOCK) WHERE OrderNo = @orderNo;",
                            new { orderNo }, cancellationToken: ct));
                    if (cashOrder1 != null)
                    {
                        return (false, await GetMessageWarningsVATCheckAsync("ExistsEOD", ct) ?? mes,
                            new ValidateTransactionDto { OrderNo = orderNo, IsEOD = cashOrder1.IsEOD, Key = cashOrder1.Key, CompTaxCode = cashOrder1.CompTaxCode },
                            HttpStatusCode.Created);
                    }
                    return (false, mes, null, HttpStatusCode.BadRequest);
                }

                // 2. Check TransHeader exists
                var transHeader = await conn.QueryFirstOrDefaultAsync<ValidateTransactionDto>(
                    new CommandDefinition(queryTransHeader, new { orderNo }, cancellationToken: ct));
                if (transHeader == null)
                    return (false, await GetMessageWarningsVATCheckAsync("NotExistsTrans", ct) ?? "Không tìm thấy giao dịch", null, HttpStatusCode.BadRequest);

                // 3. Check not return
                if (transHeader.SalesIsReturn > 0)
                    return (false, await GetMessageWarningsVATCheckAsync("OrderReturned", ct) ?? "Phiếu trả hàng", null, HttpStatusCode.BadRequest);

                var transLines = (await conn.QueryAsync<ValidateTransactionLine>(
                    new CommandDefinition(
                        @"SELECT DISTINCT [LineNo], ItemNo, [Description] ItemName, UnitOfMeasure, Quantity, UnitPrice,
                                 DiscountAmount, VATPercent, VATAmount, LineAmountIncVAT, DivisionCode, Barcode
                          FROM TransLine (NOLOCK) WHERE [LineType] = 0 AND DocumentNo = @orderNo;",
                        new { orderNo }, cancellationToken: ct))).ToList();

                transHeader.TransLine   = transLines;
                transHeader.StoreNo     = storeInfo.StoreNo;
                transHeader.StoreName   = storeInfo.Name;
                transHeader.Address     = storeInfo.Address;
                transHeader.TotalAmount = transLines.Sum(x => x.LineAmountIncVAT);
                transHeader.IsEOD       = false;
                transHeader.Key         = string.Empty;
                transHeader.CompTaxCode = string.Empty;

                // 4. Check Cash_OrderNoSentToEInvoice
                var cashOrder = await conn.QueryFirstOrDefaultAsync<Cash_OrderNoSentToEInvoice>(
                    new CommandDefinition(
                        "SELECT * FROM EInvoice.[dbo].Cash_OrderNoSentToEInvoice (NOLOCK) WHERE OrderNo = @orderNo;",
                        new { orderNo }, cancellationToken: ct));
                if (cashOrder != null)
                {
                    transHeader.IsEOD       = cashOrder.IsEOD;
                    transHeader.Key         = cashOrder.Key;
                    transHeader.CompTaxCode = cashOrder.CompTaxCode;
                    return (false, await GetMessageWarningsVATCheckAsync("ExistsEOD", ct) ?? "Đã EOD", transHeader, HttpStatusCode.Created);
                }

                // 5. Check invoice time limit from POSDataSetup
                var setup = await QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT [Value] FROM POSDataSetup (NOLOCK) WHERE [Code] = @code", new { code = "WWW_INVOICE_MINUTE" }, ct: ct);
                int minuteInvoice = int.TryParse((string?)setup?.Value, out var m) ? m : 60;

                if ((DateTime.Now - transHeader.CreatedDate).TotalMinutes > minuteInvoice)
                    return (false, await GetMessageWarningsVATCheckAsync("OverTime", ct) ?? "Quá thời gian", null, HttpStatusCode.BadRequest);

                // 6. Check date
                if (transHeader.OrderDate.Date < DateTime.Now.Date)
                    return (false, await GetMessageWarningsVATCheckAsync("OverDate", ct) ?? "Quá ngày", null, HttpStatusCode.BadRequest);

                // Set Redis check key
                if (!redis.KeyExists(InvoiceCheckKey(orderNo)))
                    redis.StringSetRaw(InvoiceCheckKey(orderNo), JsonConvert.SerializeObject(transHeader), TimeSpan.FromSeconds(3660));

                return (true, "OK", transHeader, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, ex, HttpStatusCode.BadRequest);
            }
        }
        catch (Exception e)
        {
            return (false, e.Message, e, HttpStatusCode.BadRequest);
        }
    }

    public async Task<(bool Success, string Message)> InsertDataRawJsonAsync(
        string connectStringDb, List<DataRawJsonDto> request, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.DataRawJson
            (StoreNo,PosNo,BatchFile,[FileName],DataJson,CrtDate,IsRead,Srv,Id,Type,OrderNo,OrderDate,ChgDate)
            VALUES (@StoreNo,@PosNo,@BatchFile,@FileName,@DataJson,@CrtDate,@IsRead,@Srv,@Id,@Type,@OrderNo,@OrderDate,@ChgDate)";

        var dataIns = request.Select(item => new DataRawJsonDto
        {
            Id        = Guid.NewGuid(),
            StoreNo   = item.StoreNo,
            PosNo     = item.PosNo,
            Type      = item.Type,
            BatchFile = item.BatchFile,
            FileName  = item.FileName,
            DataJson  = item.DataJson,
            CrtDate   = DateTime.Now,
            ChgDate   = DateTime.Now,
            IsRead    = false,
            OrderDate = DateTime.Now.Date,
            OrderNo   = string.Empty,
            Srv       = item.Srv
        }).ToList();

        using var conn = await OpenStoreConnectionAsync(connectStringDb, ct);
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, dataIns, tx, commandTimeout: 3600, cancellationToken: ct));
            tx.Commit();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, ex.Message);
        }
    }

    public async Task<string?> GetMessageWarningsVATCheckAsync(string actionType, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM NotifyConfig (NOLOCK) WHERE AppCode = 'VAT';";
        var configs = await QueryAsync<NotifyConfigDto>(sql, ct: ct);
        return configs.FirstOrDefault(x => x.Status?.ToUpper() == actionType.ToUpper())?.Message
               ?? "Phiếu tính tiền không hợp lệ";
    }

    public async Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT TableName FROM SyncTableList (NOLOCK) WHERE IsAll = 1 GROUP BY TableName ORDER BY TableName;";
        return (await QueryAsync<string>(sql, ct: ct)).ToList();
    }

    private static string ExtractDuplicateKey(string exMessage)
    {
        // SQL Server error 2627: "Violation of PRIMARY KEY constraint '...'. Cannot insert duplicate key in object '...'. The duplicate key value is (value)."
        var match = Regex.Match(exMessage, @"The duplicate key value is \((.+?)\)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}

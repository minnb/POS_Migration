using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using POS.Application.Common.DTOs;
using POS.Application.Common.Repositories;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories.Common;

public class CommonRepository : BaseRepository, ICommonRepository
{
    private readonly IStoreConnectionFactory _storeFactory;
    private readonly ILogger<CommonRepository> _logger;

    public CommonRepository(
        IDbConnectionFactory connectionFactory,
        IStoreConnectionFactory storeFactory,
        ILogger<CommonRepository> logger) : base(connectionFactory)
    {
        _storeFactory = storeFactory;
        _logger = logger;
    }

    public async Task<TransCpnVchIssueResponse?> TransactionQtyUseAsync(string articleNo, string siteCode)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            var data = await conn.QueryFirstOrDefaultAsync<TransCpnVchIssueResponse>(
                @"SELECT Article_No AS ArticleNo, Voucher_Type AS VoucherType, MaxQtyUse, COUNT(*) AS QtyUse
                  FROM TransCpnVchIssue
                  WHERE Site = @SiteCode AND Article_No = @ArticleNo
                  GROUP BY Article_No, Voucher_Type, MaxQtyUse",
                new { SiteCode = siteCode, ArticleNo = articleNo });

            return data ?? new TransCpnVchIssueResponse { ArticleNo = articleNo, MaxQtyUse = 9999, QtyUse = 0, VoucherType = "V" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransactionQtyUse failed for {ArticleNo} {SiteCode}", articleNo, siteCode);
            return null;
        }
    }

    public async Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            return await conn.QueryFirstOrDefaultAsync<BusinessDateResponse>(
                @"SELECT BussinessDate, GETDATE() AS CurrentDate
                  FROM BussinessDateOpen
                  WHERE StoreNo = @StoreNo",
                new { StoreNo = siteCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBusinessDate failed for {SiteCode}", siteCode);
            return null;
        }
    }

    public async Task InsertBussinessDateOpenAsync(string siteCode, DateTime bussinessDate)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM BussinessDateOpen WHERE StoreNo = @StoreNo",
                new { StoreNo = siteCode });

            if (exists == 0)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO BussinessDateOpen (Code, StoreNo, BussinessDate, CreatedUser, CreatedDate)
                      VALUES (@Code, @StoreNo, @BussinessDate, @CreatedUser, @CreatedDate)",
                    new { Code = siteCode, StoreNo = siteCode, BussinessDate = bussinessDate, CreatedUser = "api", CreatedDate = DateTime.Now });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertBussinessDateOpen failed for {SiteCode}", siteCode);
        }
    }

    public async Task InsertSignalStoreAsync(string storeNo, string posTerminalId, DateTime? bussinessDate)
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            await conn.ExecuteAsync(
                @"INSERT INTO SignalStore (StoreNO, POSTerminalID, BusinessDate, CreatedDate)
                  VALUES (@StoreNO, @POSTerminalID, @BusinessDate, @CreatedDate)",
                new { StoreNO = storeNo, POSTerminalID = posTerminalId, BusinessDate = bussinessDate?.Date ?? DateTime.Now.Date, CreatedDate = DateTime.Now });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertSignalStore failed for {StoreNo}", storeNo);
        }
    }

    public async Task<bool> CheckEndShiftAsync(string siteCode, string posTerminal, DateTime businessDate)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "EXEC [dbo].[API_POS_CHECK_SHIFT_HEADER] @SiteCode, @PosTerminal, @BusinessDate",
                new { SiteCode = siteCode, PosTerminal = posTerminal, BusinessDate = businessDate });
            return result?.IsShiftClosed ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckEndShift failed for {SiteCode} {PosTerminal}", siteCode, posTerminal);
            return false;
        }
    }

    public async Task<POSMonitorInsertResponse> POSMonitorInsertAsync(POSMonitorInsertRequest model)
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            var result = await conn.QueryFirstOrDefaultAsync<POSMonitorInsertResponse>(
                @"EXEC [dbo].[POSMonitorInsert] @StoreNo, @IpAddress, @PosTerminalID, @BluePosVersion,
                   @BluePosVersionUpdate, @BluePosDatabaseStatus, @IsOpenBluePos, @DateTimePos,
                   @IntervalJob, @LastTimeInsertAll, @LastTimeInsertChange, @JobVersion, @ScriptVersion, @ComputerName",
                new
                {
                    model.StoreNo, model.IpAddress, model.PosTerminalID,
                    BluePosVersion = model.BluePosVersion ?? "",
                    model.BluePosVersionUpdate, model.BluePosDatabaseStatus, model.IsOpenBluePos,
                    model.DateTimePos, model.IntervalJob, model.LastTimeInsertAll, model.LastTimeInsertChange,
                    JobVersion = model.JobVersion ?? "",
                    ScriptVersion = model.ScriptVersion ?? "",
                    model.ComputerName
                });
            return result ?? new POSMonitorInsertResponse { ReturnAction = "Ins" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POSMonitorInsert failed for {StoreNo}", model.StoreNo);
            return new POSMonitorInsertResponse { ReturnAction = "Ins" };
        }
    }

    public async Task<PosTerminalResponse?> CheckIPaddressPosAsync(string ipAddress)
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            return await conn.QueryFirstOrDefaultAsync<PosTerminalResponse>(
                @"SELECT IPAddress, StoreNo, No AS TerminalPOS, TerminalNetworkID, StyleProfile,
                         DefaultSalesType, SalesTypeFilter, Pkey, DualDisHost, BillNoseri, Placement,
                         ISNULL(StatementMethod,0) AS StatementMethod, TerminalStatement,
                         ISNULL(TerminalConnection,0) AS TerminalConnection,
                         PrintReceiptLogo, CustomerDisplayText1, CustomerDisplayText2,
                         ISNULL(PrintReceiptBCType,0) AS PrintReceiptBCType, InterfaceProfile
                  FROM POSTerminal
                  WHERE IPAddress = @IPAddress",
                new { IPAddress = ipAddress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckIPaddressPos failed for {IPAddress}", ipAddress);
            return null;
        }
    }

    public async Task<List<POSDataSetupResponse>> GetDataSetupAsync()
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            var result = await conn.QueryAsync<POSDataSetupResponse>("SELECT Code, Value FROM POSDataSetup");
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDataSetup failed");
            return new List<POSDataSetupResponse>();
        }
    }

    public async Task<List<POSVersionResponse>> GetPOSVersionAsync()
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            var result = await conn.QueryAsync<POSVersionResponse>(
                "SELECT LastVersion, CurVersion, UpdateTime, Counter, Source, Pkey, IsUpdate, Folder FROM POSVersion");
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPOSVersion failed");
            return new List<POSVersionResponse>();
        }
    }

    public async Task<bool> CheckSaleReturnAsync(string orderNo)
    {
        try
        {
            var siteCode = orderNo[..4];
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM TransHeader WHERE OrderNo = @OrderNo AND SalesIsReturn = 1",
                new { OrderNo = orderNo });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckSaleReturn failed for {OrderNo}", orderNo);
            return false;
        }
    }

    public async Task<List<SaleTableResponse>> GetOrderInfoAsync(string orderNo)
    {
        var result = new List<SaleTableResponse>();
        try
        {
            var siteCode = orderNo[..4];
            using var conn = await _storeFactory.CreateSaleConnectionAsync(siteCode);
            using var multi = await conn.QueryMultipleAsync(
                "EXEC [dbo].[API_SALE_INFO_ORDERNO] @OrderNo",
                new { OrderNo = orderNo });

            // SP returns multiple result sets — read as dynamic and serialize
            while (!multi.IsConsumed)
            {
                try
                {
                    var rows = (await multi.ReadAsync()).ToList();
                    if (rows.Count > 0)
                    {
                        var firstRow = (IDictionary<string, object?>)rows[0];
                        if (firstRow.TryGetValue("TableName", out var tableName))
                        {
                            var tableData = rows.Select(r =>
                            {
                                var dict = (IDictionary<string, object?>)r;
                                return dict.Where(kvp => kvp.Key != "TableName" && kvp.Key != "timestamp")
                                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            }).ToList();

                            result.Add(new SaleTableResponse
                            {
                                TableName = tableName?.ToString(),
                                TableData = System.Text.Json.JsonSerializer.Serialize(tableData)
                            });
                        }
                    }
                }
                catch { break; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrderInfo failed for {OrderNo}", orderNo);
        }
        return result;
    }

    public async Task<List<POSDocumentNoResponse>> ListPOSDocumentNoAsync(string storeNo, string posTerminal)
    {
        var result = new List<POSDocumentNoResponse>();
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(storeNo);

            var orderLast = await conn.QueryFirstOrDefaultAsync<POSDocumentNoResponse>(
                @"SELECT TOP 1 StoreNo, POSTerminalNo AS POSTerminal, OrderNo AS LastNumber,
                          'ORDER' AS DocumentType, OrderTime AS LastDateTime
                  FROM TransHeader
                  WHERE StoreNo = @StoreNo AND POSTerminalNo = @PosTerminal
                  ORDER BY OrderTime DESC",
                new { StoreNo = storeNo, PosTerminal = posTerminal });

            var voucherLast = await conn.QueryFirstOrDefaultAsync<POSDocumentNoResponse>(
                @"SELECT TOP 1 Site AS StoreNo, @PosTerminal AS POSTerminal, SerialNo AS LastNumber,
                          'VOUCHER' AS DocumentType, CreatedDate AS LastDateTime
                  FROM TransCpnVchIssue
                  WHERE Voucher_Type = 'V' AND Site = @StoreNo AND Voucher_Type <> 'R'
                    AND Site + SUBSTRING(POSNo, 2, 2) = @PosTerminal
                  ORDER BY CreatedDate DESC",
                new { StoreNo = storeNo, PosTerminal = posTerminal });

            if (orderLast != null) result.Add(orderLast);
            if (voucherLast != null) result.Add(voucherLast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPOSDocumentNo failed for {StoreNo}", storeNo);
        }
        return result;
    }

    public async Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
    {
        try
        {
            using var conn = CreateConnection(); // CentralMD
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM CpnVchBOMLine WHERE ItemNo = @ItemNo AND Barcode = @Barcode",
                new { ItemNo = itemNo, Barcode = barCode });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckCouponLine failed for {ItemNo}", itemNo);
            return false;
        }
    }

    public async Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoRequest model)
    {
        try
        {
            var orderNo = model.Header!.OrderNo!;
            var siteCode = orderNo[..4];
            using var conn = await _storeFactory.CreateSaleStagingConnectionAsync(siteCode);

            // Insert TransLineOrig
            foreach (var line in model.ListLine)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO TransLineOrig (DocumentNo, LineNo, QtyDiscountReturned, ReturnedQuantity, DiscountQuantity)
                      VALUES (@DocumentNo, @LineNo, @QtyDiscountReturned, @ReturnedQuantity, @DiscountQuantity)",
                    new { line.DocumentNo, line.LineNo, line.QtyDiscountReturned, line.ReturnedQuantity, line.DiscountQuantity });
            }

            // Insert TransBluePointOrig
            if (model.ListLineBluePoint?.Count > 0)
            {
                foreach (var bp in model.ListLineBluePoint)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO TransBluePointOrig (OrderNo, LineNo, ExtraQuantityNotEarnReturned)
                          VALUES (@OrderNo, @LineNo, @ExtraQuantityNotEarnReturned)",
                        new { bp.OrderNo, bp.LineNo, bp.ExtraQuantityNotEarnReturned });
                }
            }

            // Insert TransHeaderOrig
            await conn.ExecuteAsync(
                @"INSERT INTO TransHeaderOrig (OrderNo, MemberPointsRedeemReturned, OrderTime, PaymentAtPOSAmount)
                  VALUES (@OrderNo, @MemberPointsRedeemReturned, @OrderTime, @PaymentAtPOSAmount)",
                new
                {
                    OrderNo = orderNo,
                    model.Header.MemberPointsRedeemReturned,
                    OrderTime = DateTime.Now,
                    PaymentAtPOSAmount = model.Header.WinPayAmountReturned
                });

            // Update TransDiscountEntry
            if (model.ListLineDiscountEntry?.Count > 0)
            {
                foreach (var de in model.ListLineDiscountEntry)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE TransDiscountEntry SET Ref1 = @ReturnedQty
                          WHERE OrderNo = @OrderNo AND LineNo = @LineNo",
                        new { OrderNo = de.OrderNo, LineNo = de.LineNo, ReturnedQty = de.ReturnedQuantity.ToString() });
                }
            }

            // Update TransPaymentEntry
            if (model.ListTransPaymentEntry?.Count > 0)
            {
                foreach (var pe in model.ListTransPaymentEntry)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE TransPaymentEntry SET TransactionNo = @TransactionNo
                          WHERE OrderNo = @OrderNo AND LineNo = @LineNo",
                        new { pe.OrderNo, pe.LineNo, pe.TransactionNo });
                }
            }

            // Call stored procedure InsertTransOrig
            var callResult = await conn.ExecuteScalarAsync<int>(
                "EXEC [dbo].[InsertTransOrig] @OrderNo, @StoreNo",
                new { OrderNo = model.ListLine.FirstOrDefault()?.DocumentNo, StoreNo = "" });

            return new ResponseUpdateTransModel { Status = true, Message = $"Cập nhật thành công {callResult} LineNo" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertLineOrig_UpdateOrderInfo failed for {OrderNo}", model.Header?.OrderNo);
            return new ResponseUpdateTransModel { Status = false, Message = ex.Message };
        }
    }

    public async Task<InsuranceResponse?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
    {
        try
        {
            using var conn = CreateConnectionByName("DCMStar");
            return await conn.QueryFirstOrDefaultAsync<InsuranceResponse>(
                "EXEC [dbo].[BAOHIEM5] @ReceiptNo, @POSNo, @Staff",
                new { ReceiptNo = receiptNo, POSNo = posNo, Staff = staffCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInsurance failed for {ReceiptNo}", receiptNo);
            return null;
        }
    }

    public async Task<bool> UpdatePOSEODAsync(POSEODRequest model)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleConnectionAsync(model.StoreNo!);
            var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT 1 FROM POSEOD_API
                  WHERE StoreNo = @StoreNo AND POSTerminal = @POSTerminal
                    AND CAST(BussinessDate AS DATE) = @BussinessDate",
                new { model.StoreNo, model.POSTerminal, BussinessDate = model.BussinessDate.Date });

            if (existing != null)
            {
                await conn.ExecuteAsync(
                    @"UPDATE POSEOD_API SET TotalSale = @TotalSale
                      WHERE StoreNo = @StoreNo AND POSTerminal = @POSTerminal
                        AND CAST(BussinessDate AS DATE) = @BussinessDate",
                    new { model.StoreNo, model.POSTerminal, BussinessDate = model.BussinessDate.Date, model.TotalSale });
            }
            else
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO POSEOD_API (StoreNo, POSTerminal, BussinessDate, TotalSale, CreatedDate)
                      VALUES (@StoreNo, @POSTerminal, @BussinessDate, @TotalSale, @CreatedDate)",
                    new { model.StoreNo, model.POSTerminal, model.BussinessDate, model.TotalSale, CreatedDate = DateTime.Now });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePOSEOD failed for {StoreNo}", model.StoreNo);
            return false;
        }
    }

    public async Task<CheckTotalBillResponse> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
    {
        try
        {
            using var conn = await _storeFactory.CreateSaleStagingConnectionAsync(storeNo);
            var totalCentral = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1) FROM TransHeaderStaging
                  WHERE StoreNo = @StoreNo AND POSTerminalNo = @POSTerminal
                    AND CAST(OrderDate AS DATE) = @BussinessDate",
                new { StoreNo = storeNo, POSTerminal = posTerminal, BussinessDate = bussinessDate.Date });

            return new CheckTotalBillResponse
            {
                Status = totalCentral == posTotal,
                Description = totalCentral == posTotal ? "" : "Pos và central lệch sale",
                TotalBillPOS = posTotal,
                TotalBillCentral = totalCentral
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckTotalBill failed for {StoreNo}", storeNo);
            return new CheckTotalBillResponse { Status = false, Description = ex.Message, TotalBillPOS = posTotal, TotalBillCentral = 0 };
        }
    }

    public async Task<(bool Success, string Message)> KiosInsertSaleAsync(string storeNo, string type, string json)
    {
        try
        {
            using var conn = await _storeFactory.CreateKiosConnectionAsync(storeNo);
            var result = await conn.ExecuteScalarAsync<int>(
                "EXEC [dbo].[Sale_InsertDataByOrderV2] @Type, @Json",
                new { Type = type, Json = json });
            return result == 1 ? (true, "OK") : (false, "Thất bại");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KiosInsertSale failed for {StoreNo}", storeNo);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo)
    {
        try
        {
            using var conn = await _storeFactory.CreateKiosConnectionAsync(storeNo);
            var data = await conn.QueryFirstOrDefaultAsync<KiosCheckOrderResponse>(
                "EXEC [dbo].[SP_API_KIOS_ORDER_CHECK] @OrderNo",
                new { OrderNo = orderNo });

            return data != null
                ? (true, "OK", data)
                : (false, $"Không tìm thấy đơn hàng {orderNo} trong hệ thống", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KiosCheckOrder failed for {OrderNo}", orderNo);
            return (false, ex.Message, null);
        }
    }
}

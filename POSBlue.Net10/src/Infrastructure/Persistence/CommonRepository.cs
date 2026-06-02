using System.Data;
using Dapper;
using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Triển khai ICommonRepository bằng Dapper.
/// Port từ VCM.POSBLUE.Data.Common.CommonData (EF6 + raw ADO) sang Dapper thuần.
/// Connection names: CentralMD (master data), Partner (kios/order).
/// </summary>
public sealed class CommonRepository : ICommonRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CommonRepository(ISqlConnectionFactory factory) => _factory = factory;

    private IDbConnection CentralMd() => _factory.CreateConnection(ConnectionNames.CentralMD);
    private IDbConnection Partner()   => _factory.CreateConnection(ConnectionNames.Partner);

    // ── TransactionIssue ──────────────────────────────────────────────────────
    public async Task<TransCpnVchIssueDto?> GetTransactionQtyUseAsync(string articleNo, string siteCode)
    {
        try
        {
            using var db = CentralMd();
            return await db.QueryFirstOrDefaultAsync<TransCpnVchIssueDto>(
                "EXEC SP_API_TransactionQtyUse @ArticleNo, @SiteCode",
                new { ArticleNo = articleNo, SiteCode = siteCode });
        }
        catch (Exception ex) { Log.Error(ex, "GetTransactionQtyUseAsync"); return null; }
    }

    // ── BusinessDate ──────────────────────────────────────────────────────────
    public async Task<BusinessDateDto?> GetBusinessDateAsync(string siteCode)
    {
        try
        {
            using var db = CentralMd();
            return await db.QueryFirstOrDefaultAsync<BusinessDateDto>(
                "EXEC SP_API_GetBusinessDate @SiteCode",
                new { SiteCode = siteCode });
        }
        catch (Exception ex) { Log.Error(ex, "GetBusinessDateAsync"); return null; }
    }

    public async Task InsertBussinessDateOpenAsync(string siteCode, DateTime bussinessDate)
    {
        try
        {
            using var db = CentralMd();
            await db.ExecuteAsync(
                "EXEC SP_API_InsertBussinessDateOpen @Code, @StoreNo, @BussinessDate, @CreatedUser, @CreatedDate",
                new { Code = siteCode, StoreNo = siteCode, BussinessDate = bussinessDate, CreatedUser = "api", CreatedDate = DateTime.Now });
        }
        catch (Exception ex) { Log.Error(ex, "InsertBussinessDateOpenAsync"); }
    }

    public async Task InsertSignalStoreAsync(string storeNo, string posTerminal, DateTime? businessDate)
    {
        try
        {
            using var db = CentralMd();
            await db.ExecuteAsync(
                "EXEC SP_API_InsertSignalStore @StoreNO, @POSTerminalID, @BusinessDate, @CreatedDate",
                new { StoreNO = storeNo, POSTerminalID = posTerminal, BusinessDate = businessDate ?? DateTime.Now.Date, CreatedDate = DateTime.Now });
        }
        catch (Exception ex) { Log.Error(ex, "InsertSignalStoreAsync"); }
    }

    // ── Shift ─────────────────────────────────────────────────────────────────
    public async Task<ShiftHeaderDto?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate)
    {
        try
        {
            using var db = CentralMd();
            return await db.QueryFirstOrDefaultAsync<ShiftHeaderDto>(
                "EXEC SP_API_GET_SHIFT_HEADER @SiteCode, @POSTerminal, @BusinessDate",
                new { SiteCode = siteCode, POSTerminal = posTerminal, BusinessDate = businessDate });
        }
        catch (Exception ex) { Log.Error(ex, "GetShiftHeaderAsync"); return null; }
    }

    // ── POSMonitor ────────────────────────────────────────────────────────────
    public async Task InsertPosMonitorAsync(PosMonitorInsertRequest model)
    {
        try
        {
            using var db = CentralMd();
            await db.ExecuteAsync(
                "EXEC SP_API_POSMonitorInsert @StoreNo, @POSTerminal, @Status, @Note, @CreatedDate",
                new { model.StoreNo, model.POSTerminal, model.Status, model.Note, CreatedDate = model.CreatedDate ?? DateTime.Now });
        }
        catch (Exception ex) { Log.Error(ex, "InsertPosMonitorAsync"); }
    }

    // ── POSDataSetup ──────────────────────────────────────────────────────────
    public async Task<List<PosDataSetupDto>> GetDataSetupAsync()
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryAsync<PosDataSetupDto>("EXEC SP_API_GetDataSetup");
            return result.ToList();
        }
        catch (Exception ex) { Log.Error(ex, "GetDataSetupAsync"); return []; }
    }

    // ── POSVersion ────────────────────────────────────────────────────────────
    public async Task<List<PosVersionDto>> GetPosVersionAsync()
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryAsync<PosVersionDto>("EXEC SP_API_GetPOSVersion");
            return result.ToList();
        }
        catch (Exception ex) { Log.Error(ex, "GetPosVersionAsync"); return []; }
    }

    // ── OrderInfo / SaleReturn ────────────────────────────────────────────────
    public async Task<bool> CheckSaleReturnAsync(string orderNo)
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryFirstOrDefaultAsync<int>(
                "EXEC SP_API_CheckSaleReturn @OrderNo", new { OrderNo = orderNo });
            return result > 0;
        }
        catch (Exception ex) { Log.Error(ex, "CheckSaleReturnAsync"); return false; }
    }

    public async Task<List<SaleTableDto>> GetOrderInfoAsync(string orderNo)
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryAsync<SaleTableDto>(
                "EXEC SP_API_GetOrderInfo @OrderNo", new { OrderNo = orderNo });
            return result.ToList();
        }
        catch (Exception ex) { Log.Error(ex, "GetOrderInfoAsync"); return []; }
    }

    // ── DocumentNo ────────────────────────────────────────────────────────────
    public async Task<List<PosDocumentNoDto>> ListPosDocumentNoAsync(string storeNo, string posTerminal)
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryAsync<PosDocumentNoDto>(
                "EXEC SP_API_ListPOSDocumentNo @StoreNo, @POSTerminal",
                new { StoreNo = storeNo, POSTerminal = posTerminal });
            return result.ToList();
        }
        catch (Exception ex) { Log.Error(ex, "ListPosDocumentNoAsync"); return []; }
    }

    // ── CouponLine ────────────────────────────────────────────────────────────
    public async Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryFirstOrDefaultAsync<int>(
                "EXEC SP_API_CheckCouponLine @ItemNo, @BarCode",
                new { ItemNo = itemNo, BarCode = barCode });
            return result > 0;
        }
        catch (Exception ex) { Log.Error(ex, "CheckCouponLineAsync"); return false; }
    }

    // ── UpdateOrderTrans ──────────────────────────────────────────────────────
    public async Task<ResponseUpdateTransDto> InsertLineOrigUpdateOrderInfoAsync(UpdateOrderInfoRequest request)
    {
        try
        {
            using var db = CentralMd();
            var result = await db.QueryFirstOrDefaultAsync<ResponseUpdateTransDto>(
                "EXEC SP_API_InsertLineOrig_UpdateOrderInfo @HeaderJson, @LinesJson",
                new
                {
                    HeaderJson = JsonConvert.SerializeObject(request.Header),
                    LinesJson  = JsonConvert.SerializeObject(request.ListLine)
                });
            return result ?? new ResponseUpdateTransDto { Status = false, Message = "Không có kết quả từ SP" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InsertLineOrigUpdateOrderInfoAsync");
            return new ResponseUpdateTransDto { Status = false, Message = ex.Message };
        }
    }

    // ── Insurance ─────────────────────────────────────────────────────────────
    public async Task<InsuranceDto?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
    {
        try
        {
            using var db = CentralMd();
            return await db.QueryFirstOrDefaultAsync<InsuranceDto>(
                "EXEC SP_API_GetInsurance @ReceiptNo, @POSNo, @StaffCode",
                new { ReceiptNo = receiptNo, POSNo = posNo, StaffCode = staffCode });
        }
        catch (Exception ex) { Log.Error(ex, "GetInsuranceAsync"); return null; }
    }

    // ── EOD ───────────────────────────────────────────────────────────────────
    public async Task<bool> UpdatePosEodAsync(PosEodRequest model)
    {
        try
        {
            using var db = CentralMd();
            var rows = await db.ExecuteAsync(
                "EXEC SP_API_UpdatePOSEOD @StoreNo, @POSTerminal, @BusinessDate",
                new { model.StoreNo, model.POSTerminal, model.BusinessDate });
            return rows > 0;
        }
        catch (Exception ex) { Log.Error(ex, "UpdatePosEodAsync"); return false; }
    }

    // ── CheckTotalBill ────────────────────────────────────────────────────────
    public async Task<CheckTotalBillDto?> CheckTotalBillAsync(
        string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
    {
        try
        {
            using var db = CentralMd();
            return await db.QueryFirstOrDefaultAsync<CheckTotalBillDto>(
                "EXEC SP_API_CheckTotalBill @StoreNo, @POSTerminal, @BussinessDate, @POSTotal",
                new { StoreNo = storeNo, POSTerminal = posTerminal, BussinessDate = bussinessDate, POSTotal = posTotal });
        }
        catch (Exception ex) { Log.Error(ex, "CheckTotalBillAsync"); return null; }
    }

    // ── Kios ──────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> KiosInsertSaleAsync(
        string storeNo, string type, string json)
    {
        try
        {
            using var db = Partner();
            var result = await db.QueryFirstOrDefaultAsync<(bool, string)>(
                "EXEC SP_API_KiosInsertSale @StoreNo, @Type, @Json",
                new { StoreNo = storeNo, Type = type, Json = json });
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "KiosInsertSaleAsync");
            return (false, ex.Message);
        }
    }

    public async Task LogSaleKiosAsync(
        string storeNo, string posId, string requestPos, string requestApi, string responsePos, string responseApi)
    {
        try
        {
            using var db = Partner();
            await db.ExecuteAsync(
                """
                INSERT INTO [LogSaleKios] (StoreNo, PosID, RequestPOS, RequestAPI, ResponsePOS, ResponseAPI, CreatedDate)
                VALUES (@StoreNo, @PosID, @RequestPOS, @RequestAPI, @ResponsePOS, @ResponseAPI, @CreatedDate)
                """,
                new { StoreNo = storeNo, PosID = posId, RequestPOS = requestPos, RequestAPI = requestApi, ResponsePOS = responsePos, ResponseAPI = responseApi, CreatedDate = DateTime.Now });
        }
        catch (Exception ex) { Log.Error(ex, "LogSaleKiosAsync"); }
    }

    public async Task<(bool Success, string Message, object? Data)> KiosCheckOrderAsync(
        string storeNo, string posNo, string orderNo)
    {
        try
        {
            using var db = Partner();
            var data = await db.QueryFirstOrDefaultAsync<dynamic>(
                "EXEC SP_API_KiosCheckOrder @StoreNo, @PosNo, @OrderNo",
                new { StoreNo = storeNo, PosNo = posNo, OrderNo = orderNo });
            if (data is null) return (false, "Không tìm thấy đơn hàng", null);
            return (true, "Success", (object)data);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "KiosCheckOrderAsync");
            return (false, ex.Message, null);
        }
    }

    // ── SendCodeReward (PLG) ──────────────────────────────────────────────────
    public async Task<(bool Success, int StatusCode, string Message, object? Data)> SendCodeRewardAsync(
        string storeNo, string posId, string bussinessDate, string orderNo, string offerNo, string ipServer)
    {
        // TODO: implement full PLG logic khi convert PLGController.
        // Tạm thời trả về 501 để POS biết endpoint chưa sẵn sàng.
        await Task.CompletedTask;
        return (false, 501, "SendCodeReward chưa được triển khai trong phase này — sẽ hoàn thiện cùng PLGController.", null);
    }
}

using Dapper;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Dapper implementation của IWinCodeRepository — bảng WinCodeCustomer trên CentralMD.
/// Port logic từ WincodeRepository cũ.
/// </summary>
public sealed class WinCodeRepository : IWinCodeRepository
{
    private readonly ISqlConnectionFactory _db;
    private readonly IMemoryCacheService _cache;

    public WinCodeRepository(ISqlConnectionFactory db, IMemoryCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    private string ConnStr() => _cache.GetConnectStringCentralMD();

    public async Task<List<WinCodeCustomerRecord>> GetWinCodeCustomerAsync(string phoneNumber)
    {
        using var conn = _db.CreateConnectionFromRaw(ConnStr());
        conn.Open();
        var result = await conn.QueryAsync<WinCodeCustomerRecord>(
            "SELECT * FROM WinCodeCustomer (NOLOCK) WHERE MemberCard = @phoneNumber;",
            new { phoneNumber });
        return result.ToList();
    }

    public async Task<(bool Success, string Message)> InsertWinCodeCustomerAsync(
        WinLifeUpdatePromotionsRequest request)
    {
        using var conn = _db.CreateConnectionFromRaw(ConnStr());
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var items = (request.appliedCodes ?? []).Select(item => new WinCodeCustomerRecord
            {
                ID = Guid.NewGuid(),
                WinCode = item.winCode,
                MemberCard = request.phoneNumber,
                Csn = request.csn,
                QuantityReciepted = item.quantity,
                QuantityRecieptedSum = item.quantity,
                OrderNo = request.orderNo,
                StoreNo = request.storeCode,
                PosNo = request.posCode,
                IsDeleted = false,
                TransDate = DateTime.Now.Date,
                CreatedBy = request.posCode,
                UpdatedBy = request.posCode,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            }).ToList();

            const string sql = @"INSERT INTO [dbo].[WinCodeCustomer]
                ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted],[TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy])
                VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted,@TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy)";
            await conn.ExecuteAsync(sql, items, tx);
            tx.Commit();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            if (ex.Message.Contains("UNIQUE KEY") || ex.Message.Contains("Cannot insert duplicate key"))
                return (false, $"Đơn hàng {request.orderNo} đã thực hiện nhận {request.appliedCodes?.FirstOrDefault()?.winCode}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateWinCodeCustomerAsync(
        WinLifeUpdatePromotionsRequest request)
    {
        using var conn = _db.CreateConnectionFromRaw(ConnStr());
        conn.Open();

        var existing = (await conn.QueryAsync<WinCodeCustomerRecord>(
            "SELECT * FROM WinCodeCustomer (NOLOCK) WHERE OrderNo = @orderNo AND MemberCard = @phone;",
            new { orderNo = request.orderNo, phone = request.phoneNumber })).ToList();

        if (existing.Count == 0)
            return (false, $"Đơn hàng {request.orderNo} không tồn tại");

        using var tx = conn.BeginTransaction();
        try
        {
            var items = (request.appliedCodes ?? []).Select(item => new WinCodeCustomerRecord
            {
                ID = Guid.NewGuid(),
                WinCode = item.winCode,
                MemberCard = request.phoneNumber,
                Csn = request.csn,
                QuantityReciepted = item.quantity * -1,
                QuantityRecieptedSum = item.quantity * -1,
                OrderNo = request.orderNo + "9",
                StoreNo = request.storeCode,
                PosNo = request.posCode,
                IsDeleted = true,
                TransDate = DateTime.Now.Date,
                CreatedBy = request.posCode,
                UpdatedBy = request.posCode,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            }).ToList();

            const string sql = @"INSERT INTO [dbo].[WinCodeCustomer]
                ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted],[TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy])
                VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted,@TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy)";
            await conn.ExecuteAsync(sql, items, tx);
            tx.Commit();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            if (ex.Message.Contains("UNIQUE KEY") || ex.Message.Contains("Cannot insert duplicate key"))
                return (false, $"Đơn hàng {request.orderNo} đã thực hiện hoàn trả số lượng");
            return (false, $"Exception: {ex.Message}");
        }
    }
}

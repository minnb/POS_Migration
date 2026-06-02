using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Triển khai ILoyaltyRepository bằng Dapper trên DB Loyalty (port từ LoyaltyRepository cũ).
/// Thay DapperContext/DapperLoyaltyFactory bằng ISqlConnectionFactory, RedisManager→IRedisService,
/// RabbitMQProducer→IMessageQueueService, FileHelper→Serilog.
/// </summary>
public sealed class LoyaltyRepository : ILoyaltyRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IRedisService _redis;
    private readonly IMessageQueueService _queue;

    public LoyaltyRepository(ISqlConnectionFactory connectionFactory, IRedisService redis, IMessageQueueService queue)
    {
        _connectionFactory = connectionFactory;
        _redis = redis;
        _queue = queue;
    }

    private IDbConnection Loyalty() => _connectionFactory.CreateConnection(ConnectionNames.Loyalty);

    public string ConnectStringLoyaltyDb() => ""; // không expose raw connstring; dùng factory theo tên

    public List<StoreMappingModel> GetLoyaltyStoreMapping()
    {
        try
        {
            using var db = Loyalty();
            return db.Query<StoreMappingModel>(
                "SELECT NewStoreID,NewTerminalID,OldStoreID,OldTerminalID,OldVinPayStoreID,OldVinPayTerminalID FROM StoreMapping (NOLOCK)").ToList();
        }
        catch { return new List<StoreMappingModel>(); }
    }

    public bool UpdateStatusLoggingLoyalty(LoggingLoyaltyDto dto)
    {
        try
        {
            using var db = Loyalty();
            return db.Execute("UPDATE [LoggingLoyalty] SET [Status] = @Status WHERE OrderNo = @OrderNo;", dto) > 0;
        }
        catch { return false; }
    }

    public List<LoggingLoyaltyDto>? GetLoggingLoyalty(string actionType, string status)
    {
        try
        {
            using var db = Loyalty();
            var result = db.Query<LoggingLoyaltyDto>(
                "SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [Status] = @status;",
                new { actionType, status }).ToList();
            return result.Count > 0 ? result : null;
        }
        catch (Exception ex) { Log.Error(ex, "GetLoggingLoyalty"); return null; }
    }

    public async Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyalty(string orderNo, string actionType)
    {
        try
        {
            using var db = Loyalty();
            var result = (await db.QueryAsync<LoggingLoyaltyDto>(
                "SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [OrderNo] = @orderNo;",
                new { actionType, orderNo })).ToList();
            return result.Count > 0 ? result : null;
        }
        catch (Exception ex) { Log.Error(ex, "GetListLoggingLoyalty"); return null; }
    }

    public async Task<LoggingLoyaltyDto?> InsertLoggingLoyalty(LoggingLoyaltyDto? dto, string orderNo = "", bool isRetry = false)
    {
        try
        {
            if (!string.IsNullOrEmpty(orderNo))
            {
                using var dbCheck = Loyalty();
                var status = new List<string> { "Success", "OK" };
                var existing = (await dbCheck.QueryAsync<LoggingLoyaltyDto>(
                    "SELECT TOP 1 * FROM [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo ORDER BY CrtDate DESC",
                    new { OrderNo = orderNo })).ToList();
                if (existing.Count > 0)
                {
                    if (isRetry && !status.Contains(existing.First().Status ?? ""))
                    {
                        await dbCheck.ExecuteAsync("DELETE [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo;", new { OrderNo = orderNo });
                        return existing.First();
                    }
                    return existing.First();
                }
                return null;
            }

            if (dto is null) return null;
            if (dto.TransactionType == 0) dto.TransactionType = 1;

            const string insertSql = @"INSERT INTO [dbo].[LoggingLoyalty]
                ([AppCode],[OrderNo],[MemberCardNo],[ActionType],[LoyaltyPoints],[Transaction],[Status],[Request],[Response],[CrtDate],OrigOrderNo, Items,[TransactionType],CustName)
                VALUES (@AppCode,@OrderNo,@MemberCardNo,@ActionType,@LoyaltyPoints,@Transaction,@Status,@Request,@Response,@CrtDate,@OrigOrderNo,@Items,@TransactionType,@CustName)";

            using var db = Loyalty();
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                var checkData = (await db.QueryAsync<LoggingLoyaltyDto>(
                    "SELECT * FROM [dbo].[LoggingLoyalty] WHERE [OrderNo] = @OrderNo AND ActionType = @ActionType;",
                    new { dto.OrderNo, dto.ActionType }, transaction)).ToList();
                if (checkData.Count > 0)
                {
                    await db.ExecuteAsync(
                        "UPDATE LoggingLoyalty SET [LoyaltyPoints] = @LoyaltyPoints, [Status] = @Status, [Response] = @Response, [Items] = @Items, [Transaction] = ISNULL(@Transaction, ''), CrtDate = @CrtDate WHERE OrderNo = @OrderNo AND ActionType = @ActionType;",
                        dto, transaction);
                    transaction.Commit();
                    return dto;
                }
                var result = await db.ExecuteAsync(insertSql, dto, transaction);
                if (result > 0)
                {
                    transaction.Commit();
                    return dto;
                }
                transaction.Rollback();
                return null;
            }
            catch (SqlException e)
            {
                await _queue.ProduceClusterAsync(RabbitMqConstants.Queue_Loyalty_LoggingLoyalty, dto.OrderNo ?? "", JsonConvert.SerializeObject(dto));
                Log.Error(e, "InsertLoggingLoyalty.Rollback {OrderNo}", dto.OrderNo);
                transaction.Rollback();
                return null;
            }
        }
        catch (Exception ex) { Log.Error(ex, "InsertLoggingLoyalty"); return null; }
    }

    public async Task<GiftCodeDto?> GetGiftCode(string orderNo, string saleType, string memberCard, int amount)
    {
        try
        {
            using var db = Loyalty();
            var data = await db.QueryAsync<GiftCodeDto>(
                "EXEC SP_API_GetAndUseGiftCode @OrderNo, @MemberCard, @Amount, @SaleType",
                new { OrderNo = orderNo, MemberCard = memberCard, Amount = amount, SaleType = saleType });
            return data.FirstOrDefault();
        }
        catch { return null; }
    }

    public bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess)
    {
        try
        {
            using var db = Loyalty();
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                db.Execute(
                    "UPDATE [WinMoneyConversion] SET IsSuccess = 1, StoreNo = @StoreNo, PosNo = @PosNo, CashierID = @CashierID, UpdateTime = @UpdateTime WHERE PhoneNumber = @PhoneNumber;",
                    winMoneyConversion, transaction, commandTimeout: 3600);
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "UpdateWinMoneyConversion.Rollback {Phone}", winMoneyConversion.PhoneNumber);
                return false;
            }
        }
        catch (Exception ex) { Log.Error(ex, "UpdateWinMoneyConversion"); errMess = ex.Message; return false; }
    }

    public async Task<bool> UpdateMemoryCacheConfig(string code, bool isBlocked)
    {
        try
        {
            using var db = Loyalty();
            var result = await db.ExecuteAsync("UPDATE MemoryCacheConfig SET Blocked = @blocked WHERE [Code] = @code;",
                new { code, blocked = isBlocked });
            _redis.Delete(RedisConst.Redis_Key_MemoryCacheConfig);
            return result > 0;
        }
        catch (Exception ex) { Log.Error(ex, "UpdateMemoryCacheConfig"); return false; }
    }

    public MemoryCacheConfig? GetMemoryCacheConfig(string code)
    {
        try
        {
            var data = _redis.HashGet<MemoryCacheConfig>(RedisConst.Redis_Key_MemoryCacheConfig, code, true);
            if (data != null) return data;

            using var db = Loyalty();
            data = db.QueryFirstOrDefault<MemoryCacheConfig>(
                "SELECT * FROM MemoryCacheConfig (NOLOCK) WHERE [Code] = @code;", new { code })
                ?? new MemoryCacheConfig { Code = code, Desc = "Default", MemoryCacheType = "Default", Blocked = false, ListStore = "" };
            _redis.HashSet(RedisConst.Redis_Key_MemoryCacheConfig, code, data);
            return data;
        }
        catch (Exception ex) { Log.Error(ex, "GetMemoryCacheConfig"); return null; }
    }
}

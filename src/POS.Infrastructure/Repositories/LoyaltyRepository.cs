using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using POS.Common.Dtos;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.Loyalty.MemberBusiness;
using POS.Common.Dtos.WinCustomer;
using POS.Common.Dtos.WinMoney;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class LoyaltyRepository(
    LoyaltyConnectionFactory connectionFactory,
    IConfiguration configuration,
    IRedisService redis)
    : BaseRepository(connectionFactory), ILoyaltyRepository
{
    private const string KeyMemoryCacheConfig = "BLUEPOS:Loyalty_MemoryCacheConfig";
    private const string ParentKeyWinPay      = "WinPayAccumulation";
    private readonly string _loyaltyConnStr   = configuration.GetConnectionString("Loyalty")
        ?? throw new InvalidOperationException("ConnectionString 'Loyalty' không tìm thấy.");

    public string ConnectStringLoyaltyDb() => _loyaltyConnStr;

    public async Task<List<StoreMappingModel>?> GetLoyaltyStoreMappingAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT NewStoreID,NewTerminalID,OldStoreID,OldTerminalID,OldVinPayStoreID,OldVinPayTerminalID FROM StoreMapping (NOLOCK)";
        return (await QueryAsync<StoreMappingModel>(sql, ct: ct)).ToList();
    }

    public async Task<bool> InsertWinPayAccumulateAsync(IDbConnection db, WinPayAccumulationData data, bool isRetry = false)
    {
        const string query = @"INSERT INTO [dbo].[WinPayAccumulate]
            ([StoreNo],[PosNo],[PhoneNumber],[RefCode],[Amount],[Condition],[IsSync],[OrderNo],[OrderDate],[TotalAmount]
            ,[PaymentAmount],[TenderType],[Day],[Month],[OperationId],[TraceId],[Response],[CrtDate],[Version])
            VALUES (@StoreNo,@PosNo,@PhoneNumber,@RefCode,@Amount,@Condition,@IsSync,@OrderNo,@OrderDate,@TotalAmount,
                    @PaymentAmount,@TenderType,@Day,@Month,@OperationId,@TraceId,@Response,@CrtDate,@Version)";
        try
        {
            if (isRetry)
            {
                var existing = await db.QueryFirstOrDefaultAsync<WinPayAccumulationData>(
                    "SELECT * FROM [WinPayAccumulate] WHERE [RefCode] = @RefCode;", new { data.RefCode });

                if (existing != null)
                    await db.ExecuteAsync(
                        "UPDATE [WinPayAccumulate] SET [TraceId]=@TraceId, IsSync=@IsSync, [Response]=@Response, [CrtDate]=getdate() WHERE [PhoneNumber]=@PhoneNumber AND OrderNo=@OrderNo;",
                        data);
                else
                    await db.ExecuteAsync(query, data);

                redis.HashDelete(ParentKeyWinPay, data.RefCode);
            }
            else
            {
                await db.ExecuteAsync(query, data);
            }
            return true;
        }
        catch
        {
            redis.HashSet(ParentKeyWinPay, data.RefCode, data, ttlSeconds: (int)TimeSpan.FromDays(3).TotalSeconds);
            return false;
        }
    }

    public bool InsertMemberRemnItem(List<MemberRemnItem> memberRemnItems, string parentKeyMemberRemnItem, ref string errMess)
    {
        const string sql = @"INSERT INTO [dbo].[MemberRemnItem]
            ([Month],[CardLevel],[MemberCard],[ItemNo],[Uom],[MaxValue],[UsedValue],[RemnValue],[OrderNo],[CrtDate],[Type])
            VALUES (@Month,@CardLevel,@MemberCard,@ItemNo,@Uom,@MaxValue,@UsedValue,@RemnValue,@OrderNo,@CrtDate,'S')";
        try
        {
            using var conn = _connectionFactory.CreateOpenConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                conn.Execute(sql, memberRemnItems, tx, commandTimeout: 3600);
                tx.Commit();
                var first = memberRemnItems.First();
                redis.HashDelete(parentKeyMemberRemnItem, $"{first.MemberCard}_{first.OrderNo}");
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                if (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
                {
                    errMess = $"Đơn hàng {memberRemnItems.First().OrderNo} đã được tính HVDN";
                    return false;
                }
                var first = memberRemnItems.First();
                redis.HashSet(parentKeyMemberRemnItem, $"{first.MemberCard}_{first.OrderNo}", memberRemnItems);
                return false;
            }
        }
        catch (Exception ex)
        {
            errMess = ex.Message;
            return false;
        }
    }

    public async Task<Tuple<bool, string>> RefundMemberRemnItemAsync(string orderNo, string memberCard, CancellationToken ct = default)
    {
        const string sqlInsert = @"INSERT INTO [dbo].[MemberRemnItemRefund]
            ([Month],[CardLevel],[MemberCard],[ItemNo],[Uom],[MaxValue],[UsedValue],[RemnValue],[OrderNo],[CrtDate],[Type])
            VALUES (@Month,@CardLevel,@MemberCard,@ItemNo,@Uom,@MaxValue,@UsedValue,@RemnValue,@OrderNo,@CrtDate,'R')";
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var dataRefund = (await conn.QueryAsync<MemberRemnItem>(
                new CommandDefinition("SELECT * FROM [MemberRemnItem] (NOLOCK) WHERE OrderNo = @orderNo AND MemberCard = @memberCard;",
                    new { orderNo, memberCard }, cancellationToken: ct))).ToList();

            if (dataRefund.Count == 0)
                return new Tuple<bool, string>(false, $"Không tìm thấy dữ liệu refund của đơn hàng {orderNo}");

            if (dataRefund.Any(x => x.Type == "R"))
                return new Tuple<bool, string>(false,
                    $"Đơn hàng {dataRefund[0].OrderNo} đã thực hiện refund và lúc {dataRefund[0].CrtDate:dd-MM-yyyy HH:mm:ss}");

            if (!dataRefund.Any(x => x.Type == "S"))
                return new Tuple<bool, string>(false, $"Không tìm thấy dữ liệu refund của đơn hàng {orderNo}");

            using var tx = conn.BeginTransaction();
            try
            {
                dataRefund.ForEach(x => { x.CrtDate = DateTime.Now; });
                await conn.ExecuteAsync(new CommandDefinition(sqlInsert, dataRefund, tx, commandTimeout: 3600, cancellationToken: ct));
                tx.Commit();
                return new Tuple<bool, string>(true, "OK");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return new Tuple<bool, string>(false, $"Rollback RefundMemberRemnItem: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return new Tuple<bool, string>(false, $"RefundMemberRemnItem.Exception: {ex.Message}");
        }
    }

    public bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess)
    {
        const string sql = @"UPDATE [WinMoneyConversion]
                             SET IsSuccess=1, StoreNo=@StoreNo, PosNo=@PosNo, CashierID=@CashierID, UpdateTime=@UpdateTime
                             WHERE PhoneNumber=@PhoneNumber;";
        try
        {
            using var conn = _connectionFactory.CreateOpenConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                conn.Execute(sql, winMoneyConversion, tx, commandTimeout: 3600);
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                errMess = ex.Message;
                return false;
            }
        }
        catch (Exception ex)
        {
            errMess = ex.Message;
            return false;
        }
    }

    public async Task<bool> UpdateStatusLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, CancellationToken ct = default)
    {
        const string sql = "UPDATE [LoggingLoyalty] SET [Status] = @Status WHERE OrderNo = @OrderNo;";
        return await ExecuteAsync(sql, loggingLoyaltyDto, ct: ct) > 0;
    }

    public async Task<List<LoggingLoyaltyDto>?> GetLoggingLoyaltyAsync(string actionType, string status, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [Status] = @status;";
        var result = (await QueryAsync<LoggingLoyaltyDto>(sql, new { actionType, status }, ct: ct)).ToList();
        return result.Count > 0 ? result : null;
    }

    public async Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyaltyAsync(string orderNo, string actionType, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [OrderNo] = @orderNo;";
        var result = (await QueryAsync<LoggingLoyaltyDto>(sql, new { actionType, orderNo }, ct: ct)).ToList();
        return result.Count > 0 ? result : null;
    }

    public async Task<LoggingLoyaltyDto?> InsertLoggingLoyaltyAsync(LoggingLoyaltyDto dto, string orderNo = "", bool isRetry = false, CancellationToken ct = default)
    {
        const string sqlInsert = @"INSERT INTO [dbo].[LoggingLoyalty]
            ([AppCode],[OrderNo],[MemberCardNo],[ActionType],[LoyaltyPoints],[Transaction],[Status],[Request],[Response],[CrtDate],OrigOrderNo,Items,[TransactionType],CustName)
            VALUES (@AppCode,@OrderNo,@MemberCardNo,@ActionType,@LoyaltyPoints,@Transaction,@Status,@Request,@Response,@CrtDate,@OrigOrderNo,@Items,@TransactionType,@CustName)";

        if (!string.IsNullOrEmpty(orderNo))
        {
            const string sqlCheck = "SELECT TOP 1 * FROM [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo ORDER BY CrtDate DESC";
            using var connCheck = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var existing = await connCheck.QueryFirstOrDefaultAsync<LoggingLoyaltyDto>(
                new CommandDefinition(sqlCheck, new { OrderNo = orderNo }, cancellationToken: ct));
            if (existing != null)
            {
                var successStatuses = new[] { "Success", "OK" };
                if (isRetry && !successStatuses.Contains(existing.Status))
                {
                    await connCheck.ExecuteAsync(new CommandDefinition(
                        "DELETE [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo;",
                        new { OrderNo = orderNo }, cancellationToken: ct));
                }
                return existing;
            }
            return null;
        }

        if (dto.TransactionType == 0) dto.TransactionType = 1;

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            conn.Open(); // already opened by factory but ensuring
            using var tx = conn.BeginTransaction();
            try
            {
                var checkData = await conn.QueryFirstOrDefaultAsync<LoggingLoyaltyDto>(
                    new CommandDefinition(
                        "SELECT * FROM [dbo].[LoggingLoyalty] WHERE [OrderNo] = @OrderNo AND ActionType = @ActionType;",
                        new { dto.OrderNo, dto.ActionType }, tx, cancellationToken: ct));

                if (checkData != null)
                {
                    const string sqlUpdate = @"UPDATE LoggingLoyalty
                        SET [LoyaltyPoints]=@LoyaltyPoints,[Status]=@Status,[Response]=@Response,[Items]=@Items,
                            [Transaction]=ISNULL(@Transaction,''),CrtDate=@CrtDate
                        WHERE OrderNo=@OrderNo AND ActionType=@ActionType;";
                    await conn.ExecuteAsync(new CommandDefinition(sqlUpdate, dto, tx, cancellationToken: ct));
                    tx.Commit();
                    return dto;
                }

                var rowsAffected = await conn.ExecuteAsync(new CommandDefinition(sqlInsert, dto, tx, cancellationToken: ct));
                if (rowsAffected > 0)
                {
                    tx.Commit();
                    return dto;
                }
            }
            catch (SqlException)
            {
                tx.Rollback();
                return null;
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    public async Task<GiftCodeDto?> GetGiftCodeAsync(string orderNo, string saleType, string memberCard, int amount, CancellationToken ct = default)
    {
        const string sql = "EXEC SP_API_GetAndUseGiftCode @OrderNo, @MemberCard, @Amount, @SaleType";
        return await QueryFirstOrDefaultAsync<GiftCodeDto>(sql,
            new { OrderNo = orderNo, MemberCard = memberCard, Amount = amount, SaleType = saleType }, ct: ct);
    }

    public async Task<bool> UpdateMemoryCacheConfigAsync(string code, bool isBlocked, CancellationToken ct = default)
    {
        const string sql = "UPDATE MemoryCacheConfig SET Blocked = @blocked WHERE [Code] = @code;";
        var rows = await ExecuteAsync(sql, new { code, blocked = isBlocked }, ct: ct);
        if (rows > 0) redis.Delete(KeyMemoryCacheConfig);
        return rows > 0;
    }

    public async Task<MemoryCacheConfig?> GetMemoryCacheConfigAsync(string code, CancellationToken ct = default)
    {
        var cached = redis.HashGet<MemoryCacheConfig>(KeyMemoryCacheConfig, code);
        if (cached != null) return cached;

        const string sql = "SELECT * FROM MemoryCacheConfig (NOLOCK) WHERE [Code] = @code;";
        var data = await QueryFirstOrDefaultAsync<MemoryCacheConfig>(sql, new { code }, ct: ct)
            ?? new MemoryCacheConfig { Code = code, Desc = "Default", MemoryCacheType = "Default", Blocked = false, ListStore = string.Empty };

        redis.HashSet(KeyMemoryCacheConfig, code, data);
        return data;
    }
}

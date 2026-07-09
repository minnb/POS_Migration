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
    IConfiguration configuration)
    : BaseRepository(connectionFactory), ILoyaltyRepository
{
    private readonly string _loyaltyConnStr   = configuration.GetConnectionString("Loyalty")
        ?? throw new InvalidOperationException("ConnectionString 'Loyalty' không tìm thấy.");

    public string ConnectStringLoyaltyDb() => _loyaltyConnStr;

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
            ([AppCode],StoreNo,[OrderNo],[MemberCardNo],[ActionType],[LoyaltyPoints],[Transaction],[Status],[Request],[Response],[CrtDate],OrigOrderNo,Items,[TransactionType],CustName,[OrderTime])
            VALUES (@AppCode,@StoreNo,@OrderNo,@MemberCardNo,@ActionType,@LoyaltyPoints,@Transaction,@Status,@Request,@Response,@CrtDate,@OrigOrderNo,@Items,@TransactionType,@CustName,@OrderTime)";

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
}

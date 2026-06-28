using Dapper;
using Microsoft.Data.SqlClient;
using POS.Common.Dtos.MSN;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class OfferStaffRepository(LoyaltyConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IOfferStaffRepository
{
    public async Task<(bool Success, string Message)> InsertOfferStaffTransactionAsync(
        OfferStaffTransactionDto request, CancellationToken ct = default)
    {
        const string queryCheck = "SELECT * FROM [OfferStaffTransaction] (NOLOCK) WHERE OrderNo = @ReturnedOrderNo;";
        const string queryInsert = @"INSERT INTO [dbo].[OfferStaffTransaction]
            ([Month],[ClubCode],[OrderDate],InitQuota,[StoreNo],[PosNo],[PhoneNumber],[StaffCode],[OrderNo],[Amount],[TransactionType],[ReturnedOrderNo],[CrtDate])
            VALUES (@Month,@ClubCode,@OrderDate,@InitQuota,@StoreNo,@PosNo,@PhoneNumber,@StaffCode,@OrderNo,@Amount,@TransactionType,@ReturnedOrderNo,@CrtDate);";
        const string queryDelete = "DELETE [OfferStaffTransaction] WHERE OrderNo = @OrderNo;";

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);

            if (request.TransactionType == 2)
            {
                var checkReturnOrder = await conn.QueryFirstOrDefaultAsync<OfferStaffTransactionDto>(
                    new CommandDefinition(queryCheck, new { request.ReturnedOrderNo }, cancellationToken: ct));
                if (checkReturnOrder == null)
                    return (false, $"Đơn trả hàng {request.OrderNo} Không tìm thấy số đơn hàng gốc {request.ReturnedOrderNo}");
                request.InitQuota = checkReturnOrder.InitQuota;
            }

            using var tx = conn.BeginTransaction();
            try
            {
                if (request.TransactionType == 3)
                    await conn.ExecuteAsync(new CommandDefinition(queryDelete, request, tx, cancellationToken: ct));
                else
                    await conn.ExecuteAsync(new CommandDefinition(queryInsert, request, tx, cancellationToken: ct));

                tx.Commit();
                return (true, "OK");
            }
            catch (SqlException e)
            {
                tx.Rollback();
                return (false, $"SqlException: {e.Message}");
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<OfferStaffRemnDto?> GetOfferStaffRemnAsync(
        string staffCode, string phoneNumber, string clubCode, CancellationToken ct = default)
    {
        const string sql = "EXEC SP_API_GET_OfferStaffRemn @PhoneNumber, @ClubCode, @StaffCode;";
        return await QueryFirstOrDefaultAsync<OfferStaffRemnDto>(sql,
            new { PhoneNumber = phoneNumber, ClubCode = clubCode, StaffCode = staffCode }, ct: ct);
    }

    public async Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT TOP 1 *
                             FROM [OfferStaffSetup]
                             WHERE Blocked = 0
                               AND CAST(getdate() AS DATE) >= CAST(ApplyFrom AS DATE)
                               AND CAST(getdate() AS DATE) <= CAST(ValidBefore AS DATE)
                             ORDER BY ApplyFrom DESC;";
        return await QueryFirstOrDefaultAsync<OfferStaffSetupDto>(sql, ct: ct);
    }
}

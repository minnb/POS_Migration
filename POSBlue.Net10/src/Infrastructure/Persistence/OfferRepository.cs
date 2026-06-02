using System.Data;
using System.Data.SqlClient;
using Dapper;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

public class OfferRepository : IOfferRepository
{
    private readonly ISqlConnectionFactory _factory;

    public OfferRepository(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    private IDbConnection Loyalty() => _factory.CreateConnection(ConnectionNames.Loyalty);

    public async Task<(bool Success, string Message)> InsertOfferStaffTransactionAsync(OfferStaffTransactionDto request)
    {
        try
        {
            string queryCheck = @"SELECT * FROM [OfferStaffTransaction] (NOLOCK) WHERE OrderNo = @ReturnedOrderNo;";
            string queryInsert = @"INSERT INTO [dbo].[OfferStaffTransaction]" +
                            " ([Month],[ClubCode],[OrderDate],InitQuota, [StoreNo],[PosNo],[PhoneNumber],[StaffCode],[OrderNo],[Amount],[TransactionType],[ReturnedOrderNo],[CrtDate])" +
                            " VALUES (@Month,@ClubCode,@OrderDate, @InitQuota, @StoreNo,@PosNo,@PhoneNumber,@StaffCode,@OrderNo,@Amount,@TransactionType,@ReturnedOrderNo,@CrtDate);";
            string queryDelete = @"DELETE [OfferStaffTransaction] WHERE OrderNo = @OrderNo;";

            using var db = Loyalty();
            db.Open();

            if (request.TransactionType == 2)
            {
                var checkReturnOrderNo = await db.QueryFirstOrDefaultAsync<OfferStaffTransactionDto>(queryCheck, new { request.ReturnedOrderNo });
                if (checkReturnOrderNo == null)
                {
                    return (false, $"Đơn trả hàng {request.OrderNo} Không tìm thấy số đơn hàng gốc {request.ReturnedOrderNo}");
                }
                request.InitQuota = checkReturnOrderNo.InitQuota;
            }

            using var transaction = db.BeginTransaction();
            try
            {
                if (request.TransactionType == 3)
                {
                    await db.ExecuteAsync(queryDelete, new { request.OrderNo }, transaction);
                }
                else
                {
                    await db.ExecuteAsync(queryInsert, request, transaction);
                }
                transaction.Commit();
                return (true, "OK");
            }
            catch (SqlException e)
            {
                transaction.Rollback();
                Log.Error(e, "InsertOfferStaffTransactionAsync SqlException");
                return (false, $"SqlException: {e.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InsertOfferStaffTransactionAsync");
            return (false, ex.Message);
        }
    }

    public async Task<OfferStaffRemnDto?> GetOfferStaffRemnAsync(string staffCode, string phoneNumber, string clubCode)
    {
        try
        {
            string query = @"EXEC SP_API_GET_OfferStaffRemn @PhoneNumber, @ClubCode, @StaffCode;";
            using var db = Loyalty();
            return await db.QueryFirstOrDefaultAsync<OfferStaffRemnDto>(
                query, new { PhoneNumber = phoneNumber, ClubCode = clubCode, StaffCode = staffCode });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetOfferStaffRemnAsync");
            return null;
        }
    }

    public async Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync()
    {
        try
        {
            string query = @"SELECT TOP 1 *
                            FROM [OfferStaffSetup] 
                            WHERE Blocked = 0 AND CAST(getdate() AS DATE) >= CAST(ApplyFrom AS DATE) AND CAST(getdate() AS DATE) <= CAST(ValidBefore AS DATE)
                            ORDER BY ApplyFrom DESC;";
            using var db = Loyalty();
            return await db.QueryFirstOrDefaultAsync<OfferStaffSetupDto>(query);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetOfferStaffSetupAsync");
            return null;
        }
    }
}

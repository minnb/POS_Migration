using Dapper;
using System.Collections.Generic;
using System;
using System.Configuration;
using System.Data;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using System.Linq;
using StackExchange.Redis;
using System.Data.SqlClient;
using System.Reflection;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore.Repository
{
    public class OfferStaffRepository
    {
        public OfferStaffRepository()
        {
        }
        public (bool, string) InsertOfferStaffTransaction(OfferStaffTransactionDto request)
        {
            var methodName = nameof(InsertOfferStaffTransaction);
            try
            {
                string queryCheck = @"SELECT * FROM [OfferStaffTransaction] (NOLOCK) WHERE OrderNo = '" + request.ReturnedOrderNo + "';";
                string query = @"INSERT INTO [dbo].[OfferStaffTransaction]" +
                                " ([Month],[ClubCode],[OrderDate],InitQuota, [StoreNo],[PosNo],[PhoneNumber],[StaffCode],[OrderNo],[Amount],[TransactionType],[ReturnedOrderNo],[CrtDate])" +
                                " VALUES (@Month,@ClubCode,@OrderDate, @InitQuota, @StoreNo,@PosNo,@PhoneNumber,@StaffCode,@OrderNo,@Amount,@TransactionType,@ReturnedOrderNo,@CrtDate);";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    if(request.TransactionType == 2)
                    {
                        var checkReturnOrderNo = db.Query<OfferStaffTransactionDto>(queryCheck).FirstOrDefault();
                        if (checkReturnOrderNo == null)
                        {
                            return (false, $"Đơn trả hàng {request.OrderNo} Không tìm thấy số đơn hàng gốc {request.ReturnedOrderNo}");
                        }
                        request.InitQuota = checkReturnOrderNo.InitQuota;
                    }

                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            if (request.TransactionType == 3) 
                            {
                                db.Execute(@"DELETE [OfferStaffTransaction] WHERE OrderNo = @OrderNo;", request, transaction);
                            }
                            else
                            {
                                db.Execute(query, request, transaction);
                            }
                            transaction.Commit();
                            return (true, "OK");
                        }
                        catch (SqlException e)
                        {
                            transaction.Rollback();
                            var errorMessage = SqlErrorHandler.HandleSqlException(e, methodName);
                            return (false, $"SqlException: {errorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("InsertOfferStaffTransaction.Exception", ex);
                return (false, ex.Message);
            }
        }
        public OfferStaffRemnDto GetOfferStaffRemn(string staffCode, string phoneNumber, string clubCode)
        {
            try
            {
                string query = @"EXEC SP_API_GET_OfferStaffRemn @PhoneNumber, @ClubCode, @StaffCode;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    return db.Query<OfferStaffRemnDto>(query, new { PhoneNumber = phoneNumber, ClubCode = clubCode, StaffCode = staffCode }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetOfferStaffRemn.Exception", ex);
                return null;
            }
        }
        public OfferStaffSetupDto GetOfferStaffSetup()
        {
            try
            {
                string query = @"SELECT TOP 1 *
	                            FROM [OfferStaffSetup] 
	                            WHERE Blocked = 0 AND CAST(getdate() AS DATE) >= CAST(ApplyFrom AS DATE) AND CAST(getdate() AS DATE) <= CAST(ValidBefore AS DATE)
	                            ORDER BY ApplyFrom DESC;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    return db.Query<OfferStaffSetupDto>(query).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetOfferStaffSetup.Exception", ex);
                return null;
            }
        }
    }
}
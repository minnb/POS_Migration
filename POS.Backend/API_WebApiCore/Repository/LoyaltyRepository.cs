using Dapper;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SqlClient;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.WinCustomer;
using TCX.API.Common.Dtos.WinMoney;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore.Repository
{
    public class LoyaltyRepository
    {
        private readonly string _connectStringLoyaltyDb;
        private DapperContext _dapperLOYALTY;
        private readonly RedisConnectionHelper _redisHelper;
        private readonly RedisManager _redisManager;
        public LoyaltyRepository()
        {
            _connectStringLoyaltyDb = ConfigurationManager.ConnectionStrings["DAPPER_LOYALTY"].ConnectionString;
            _redisHelper = new RedisConnectionHelper();
            _dapperLOYALTY = new DapperContext(_connectStringLoyaltyDb);
            _redisManager = new RedisManager();
        }
        public List<StoreMappingModel> GetLoyaltyStoreMapping()
        {
            try
            {
                using (IDbConnection db = _dapperLOYALTY.CreateConnDB)
                {
                    db.Open();
                    List<StoreMappingModel> data = db.Query<StoreMappingModel>("SELECT NewStoreID,NewTerminalID,OldStoreID,OldTerminalID,OldVinPayStoreID,OldVinPayTerminalID FROM StoreMapping (NOLOCK)").ToList();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public string ConnectStringLoyaltyDb()
        {
            return _connectStringLoyaltyDb;
        }
        public bool InsertWinPayAccumulate(IDbConnection db, WinPayAccumulationData winPayAccumulationData, bool IsRetry = false)
        {
            try
            {
                string query = @"INSERT INTO [dbo].[WinPayAccumulate]
                                        ([StoreNo],[PosNo],[PhoneNumber],[RefCode],[Amount],[Condition],[IsSync],[OrderNo],[OrderDate],[TotalAmount]
                                        ,[PaymentAmount],[TenderType],[Day],[Month],[OperationId],[TraceId],[Response],[CrtDate],[Version])"
                               + " VALUES (@StoreNo,@PosNo,@PhoneNumber,@RefCode,@Amount,@Condition,@IsSync,@OrderNo,@OrderDate,@TotalAmount,@PaymentAmount,@TenderType,@Day,@Month,@OperationId,@TraceId,@Response,@CrtDate, @Version)";
                if (IsRetry)
                {
                    var checkData = db.Query<WinPayAccumulationData>("SELECT * FROM [WinPayAccumulate] WHERE [RefCode] =  @RefCode;", new { winPayAccumulationData.RefCode }).FirstOrDefault();

                    if (checkData != null)
                    {
                        db.Execute(@"UPDATE [WinPayAccumulate] SET [TraceId] = @TraceId, IsSync = @IsSync, [Response] = @Response, [CrtDate] = getdate() WHERE [PhoneNumber]= @PhoneNumber AND OrderNo = @OrderNo;", winPayAccumulationData);
                    }
                    else
                    {
                        db.Execute(query, winPayAccumulationData);
                    }

                    if (_redisHelper.DeleteWithParent(winPayAccumulationData.RefCode, "WinPayAccumulation"))
                    {
                        FileHelper.WriteLogs("InsertWinPayAccumulate Delete key redis " + winPayAccumulationData.RefCode.ToString());
                    }
                    return true;
                }
                else
                {
                    db.Execute(query, winPayAccumulationData);
                    return true;
                }
            }
            catch (Exception ex)
            {
                var _redisHelper = new RedisConnectionHelper();
                _redisHelper.Set<WinPayAccumulationData>(winPayAccumulationData.RefCode, winPayAccumulationData, "WinPayAccumulation", TimeSpan.FromDays(3));
                FileHelper.WriteExpLogs("Exception InsertWinPayAccumulate", ex);
                return false;
            }
        }
        public bool InsertMemberRemnItem(List<MemberRemnItem> memberRemnItems, string parentKeyMemberRemnItem, ref string errMess)
        {
            try
            {
                string queryIns = @"INSERT INTO [dbo].[MemberRemnItem] ([Month],[CardLevel],[MemberCard],[ItemNo],[Uom],[MaxValue],[UsedValue],[RemnValue],[OrderNo],[CrtDate],[Type]) " +
                                " VALUES (@Month,@CardLevel,@MemberCard,@ItemNo,@Uom,@MaxValue,@UsedValue,@RemnValue,@OrderNo,@CrtDate,'S')";
                using (IDbConnection db = _dapperLOYALTY.CreateConnDB)
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            db.Execute(queryIns, memberRemnItems, transaction, commandTimeout: 3600);
                            transaction.Commit();
                            _redisHelper.DeleteWithParent(memberRemnItems.FirstOrDefault().MemberCard + "_" + memberRemnItems.FirstOrDefault().OrderNo, parentKeyMemberRemnItem);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            if (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
                            {
                                errMess = string.Format(@"Đơn hàng {0} đã được tính HVDN", memberRemnItems.FirstOrDefault().OrderNo);
                                return false;
                            }
                            FileHelper.WriteExpLogs("InsertMemberRemnItem.BeginTransaction.Rollback " + memberRemnItems.FirstOrDefault().OrderNo, ex);
                            _redisHelper.Set<List<MemberRemnItem>>(memberRemnItems.FirstOrDefault().MemberCard + "_" + memberRemnItems.FirstOrDefault().OrderNo, memberRemnItems, parentKeyMemberRemnItem);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("InsertMemberRemnItem.Exception", ex);
                return false;
            }
        }
        public Tuple<bool, string> RefundMemberRemnItem(string orderNo, string memberCard)
        {
            try
            {
                string queryExc = @"INSERT INTO [dbo].[MemberRemnItemRefund] ([Month],[CardLevel],[MemberCard],[ItemNo],[Uom],[MaxValue],[UsedValue],[RemnValue],[OrderNo],[CrtDate], [Type]) " +
                                    " VALUES (@Month,@CardLevel,@MemberCard,@ItemNo,@Uom,@MaxValue,@UsedValue,@RemnValue,@OrderNo,@CrtDate, 'R')";

                using (IDbConnection db = _dapperLOYALTY.CreateConnDB)
                {
                    db.Open();
                    var dataRefund = db.Query<MemberRemnItem>(@"SELECT * FROM [MemberRemnItem] (NOLOCK) WHERE OrderNo = @orderNo AND MemberCard = @memberCard;", new { orderNo , memberCard }).ToList();
                    if (dataRefund != null && dataRefund.Count > 0)
                    {
                        if (dataRefund.FirstOrDefault(x => x.Type == "R") != null)
                        {
                            return new Tuple<bool, string>(false, string.Format("Đơn hàng {0} đã thực hiện refund và lúc {1}",
                                                        dataRefund.FirstOrDefault().OrderNo, dataRefund.FirstOrDefault().CrtDate.ToString("dd-MM-yyyy HH:mm:ss")));
                        }
                        else if (dataRefund.FirstOrDefault(x => x.Type == "S") != null)
                        {
                            using (var transaction = db.BeginTransaction())
                            {
                                try
                                {
                                    dataRefund.ForEach(x => { x.CrtDate = DateTime.Now; });
                                    db.Execute(queryExc, dataRefund, transaction, commandTimeout: 3600);
                                    transaction.Commit();
                                    return new Tuple<bool, string>(true, "OK");
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    FileHelper.WriteExpLogs("RefundMemberRemnItem.BeginTransaction.Rollback " + orderNo, ex);
                                    return new Tuple<bool, string>(false, "Rollback RefundMemberRemnItem: " + ex.Message);
                                }
                            }
                        }
                    }
                    return new Tuple<bool, string>(false, "Không tìm thấy dữ liệu refund của đơn hàng " + orderNo);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("RefundMemberRemnItem.Exception", ex);
                return new Tuple<bool, string>(false, "RefundMemberRemnItem.Exception: " + ex.Message);
            }
        }
        public bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess)
        {
            try
            {
                string queryUpdate = @"UPDATE [WinMoneyConversion] " +
                                     " SET IsSuccess = 1, StoreNo = @StoreNo, PosNo = @PosNo, CashierID = @CashierID, UpdateTime = @UpdateTime " +
                                     " WHERE PhoneNumber = @PhoneNumber;";
                using (IDbConnection db = _dapperLOYALTY.CreateConnDB)
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            db.Execute(queryUpdate, winMoneyConversion, transaction, commandTimeout: 3600);
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            FileHelper.WriteExpLogs("UpdateWinMoneyConversion.BeginTransaction.Rollback " + winMoneyConversion.PhoneNumber, ex);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("UpdateWinMoneyConversion.Exception", ex);
                return false;
            }
        }
        public bool UpdateStatusLoggingLoyalty(LoggingLoyaltyDto loggingLoyaltyDto)
        {
            try
            {
                string query = @"UPDATE [LoggingLoyalty] SET [Status] = @Status WHERE OrderNo = @OrderNo;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    var result = db.Execute(query, loggingLoyaltyDto);
                    if (result > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        public List<LoggingLoyaltyDto> GetLoggingLoyalty(string actionType, string status)
        {
            try
            {
                string queryData = @"SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [Status] = @status;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    var result = db.Query<LoggingLoyaltyDto>(queryData, new { actionType , status }).ToList();
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetLoggingLoyalty.Exception", ex);
                return null;
            }
        }
        public async Task<List<LoggingLoyaltyDto>> GetListLoggingLoyalty(string orderNo,string actionType)
        {
            try
            {
                string queryData = @"SELECT * FROM [dbo].[LoggingLoyalty] (NOLOCK) WHERE [ActionType] = @actionType AND [OrderNo] = @orderNo;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    var result = await db.QueryAsync<LoggingLoyaltyDto>(queryData, new { actionType, orderNo });
                    if (result != null && result.ToList().Count > 0)
                    {
                        return result.ToList();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetListLoggingLoyalty.Exception", ex);
                return null;
            }
        }
        public async Task<LoggingLoyaltyDto> InsertLoggingLoyalty(LoggingLoyaltyDto loggingLoyaltyDtos, string orderNo = "", bool isRetry = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(orderNo))
                {
                    string queryData = @"SELECT TOP 1 * FROM [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo ORDER BY CrtDate DESC";
                    using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                    { 
                        List<string> status = new List<string>() { "Success", "OK" };
                        var result = await db.QueryAsync<LoggingLoyaltyDto>(queryData, new { OrderNo = orderNo});
                        if (result != null && result.Count() > 0)
                        {
                            if (isRetry && !status.Contains(result.FirstOrDefault().Status) )
                            {
                                await db.ExecuteAsync("DELETE [dbo].[LoggingLoyalty] WHERE OrderNo = @OrderNo;", new { OrderNo = orderNo});
                                return result.FirstOrDefault();
                            }
                            return result.FirstOrDefault();
                        }
                    }
                    return null;
                }

                if(loggingLoyaltyDtos != null && loggingLoyaltyDtos.TransactionType == 0)
                {
                    loggingLoyaltyDtos.TransactionType = 1;
                }
                string query = @"INSERT INTO [dbo].[LoggingLoyalty]
                                    ([AppCode],[OrderNo],[MemberCardNo],[ActionType],[LoyaltyPoints],[Transaction],[Status],[Request],[Response],[CrtDate],OrigOrderNo, Items,[TransactionType],CustName)"
                        + " VALUES (@AppCode,@OrderNo,@MemberCardNo,@ActionType,@LoyaltyPoints,@Transaction,@Status,@Request,@Response,@CrtDate, @OrigOrderNo, @Items, @TransactionType,@CustName)";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            var checkData = await db.QueryAsync<LoggingLoyaltyDto>("SELECT * FROM [dbo].[LoggingLoyalty] WHERE [OrderNo] = @OrderNo AND ActionType = @actionType;", new { loggingLoyaltyDtos.OrderNo, loggingLoyaltyDtos.ActionType }, transaction);
                            if (checkData != null && checkData.Count() > 0)
                            {
                                string updateQuery = "UPDATE LoggingLoyalty SET [LoyaltyPoints] = @LoyaltyPoints, [Status] = @Status, [Response] = @Response, [Items] = @Items, [Transaction] = ISNULL(@Transaction, ''), CrtDate = @CrtDate WHERE OrderNo = @OrderNo AND ActionType = @ActionType;";
                                await db.ExecuteAsync(updateQuery, loggingLoyaltyDtos, transaction);
                                transaction.Commit();
                                return loggingLoyaltyDtos;
                            }

                            var result = await db.ExecuteAsync(query, loggingLoyaltyDtos, transaction);
                            if (result > 0)
                            {
                                transaction.Commit();
                                return loggingLoyaltyDtos;
                            }
                        }
                        catch(SqlException e)
                        {
                            RabbitMQProducer.ProducerRabbtMQCluster(new KibanaService(), RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                                 loggingLoyaltyDtos.OrderNo,
                                                                                 JsonConvert.SerializeObject(loggingLoyaltyDtos));
                            FileHelper.WriteExpLogs("InsertLoggingLoyalty.BeginTransaction.Rollback " + loggingLoyaltyDtos.OrderNo, e);
                            transaction.Rollback();
                            return null;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("InsertLoggingLoyalty.Exception", ex);
                return null;
            }
        }
        public async Task<GiftCodeDto> GetGiftCode(string orderNo, string saleType, string memberCard, int amount)
        {
            try
            {
                using (IDbConnection db = _dapperLOYALTY.CreateConnDB)
                {
                    db.Open();
                    var data = await db.QueryAsync<GiftCodeDto>("EXEC SP_API_GetAndUseGiftCode @OrderNo, @MemberCard, @Amount, @SaleType", 
                                    new { OrderNo = orderNo, MemberCard = memberCard, Amount = amount, SaleType = saleType });
                    return data.FirstOrDefault();
                }
            }
            catch
            {
                return default;
            }
        }
        public async Task<bool>  UpdateMemoryCacheConfig(RedisManager redisManager, string code, bool isBlocked)
        {
            try
            {
                var queryData = @"UPDATE MemoryCacheConfig SET Blocked = @blocked WHERE [Code] = @code;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    var result = await db.ExecuteAsync(queryData, new { code, blocked = isBlocked });
                    redisManager.Delete(RedisConst.Redis_Key_MemoryCacheConfig);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("UpdateMemoryCacheConfig.Exception", ex);
                return false;
            }
        }
        public MemoryCacheConfig GetMemoryCacheConfig(RedisManager redisManager, string code)
        {
            try
            {
                var data = redisManager.HashGet<MemoryCacheConfig>(RedisConst.Redis_Key_MemoryCacheConfig, code, true);
                if (data != null)
                {
                    return data;
                }
                var queryData = @"SELECT * FROM MemoryCacheConfig (NOLOCK) WHERE [Code] = @code;";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    data = db.QueryFirstOrDefault<MemoryCacheConfig>(queryData, new { code }) ?? new MemoryCacheConfig()
                    {
                        Code = code,
                        Desc = "Default",
                        MemoryCacheType = "Default",
                        Blocked = false,
                        ListStore = ""
                    };
                    redisManager.HashSet<MemoryCacheConfig>(RedisConst.Redis_Key_MemoryCacheConfig, code, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetMemoryCacheConfig.Exception", ex);
                return null;
            }
        }
    }
}

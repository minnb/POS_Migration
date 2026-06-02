using Dapper;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Coupon;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore.DbContext;
using VCM.POSBLUE.Model.SAP;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore
{
    public class IssueVoucherService
    {
        public static string ConfigPnL { get; set; } = "VOUCHER";
        public IssueVoucherService()
        {
        }
        private readonly long ExprireTime = 255520000; // 1 day
        private readonly string REDIS_KEY_VOUCHER = StringHelper.CreateKeyRedis(ConfigPnL, "CpnVch:");
        private readonly string REDIS_KEY_CpnVchBOMHeader = StringHelper.CreateKeyRedis(ConfigPnL, "CpnVch:CpnVchBOMHeader");
        private readonly string REDIS_KEY_Gift_CouponValidTime = StringHelper.CreateKeyRedis(ConfigPnL, "CpnVch:Gift_CouponValidTime");
        public async Task<List<CpnVchCodeSendQuota>> GetCpnVchCodeSendQuota(RedisManager redisCluster, KibanaService _kibana)
        {
            try
            {
                string query = @"SELECT A.*, CAST(B.EndingDate AS DATE) EndingDate, CAST(B.[StartingDate] AS DATE) [StartingDate], ISNULL(C.SL, 0) QtyIssue
                                    FROM [CpnVchBOMQuota] (NOLOCK) A
                                    INNER JOIN [CpnVchBOMHeader] (NOLOCK) B ON A.ItemNo = B.ItemNo
                                    LEFT JOIN (SELECT I.ItemNo, COUNT(1) AS SL 
			                                    FROM CpnVchBOMCodeIssue (NOLOCK) I
			                                    WHERE [Enabled] = 1 AND NOT EXISTS (SELECT 1 FROM CpnVchCodeSend (NOLOCK) S WHERE S.[Code] = I.[Code])
			                                    GROUP BY I.ItemNo
			                                    ) C ON C.ItemNo = A.ItemNo
                                    WHERE CAST(getdate() AS DATE) >= CAST([StartingDate] AS DATE) AND CAST(getdate() AS DATE) <= CAST([EndingDate] AS DATE);";
                var result = new List<CpnVchCodeSendQuota>();
                result = await redisCluster.StringGetAsync<List<CpnVchCodeSendQuota>>(REDIS_KEY_VOUCHER + RedisConst.Redis_Key_CpnVchCodeSendQuota);
                if (result == null && !redisCluster.CheckKeyExists(REDIS_KEY_VOUCHER + RedisConst.Redis_Key_CpnVchCodeSendQuota))
                {
                    using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                    {
                        db.Open();
                        result = db.Query<CpnVchCodeSendQuota>(query).ToList();
                        if(result!= null)
                        {
                            redisCluster.StringSet(REDIS_KEY_VOUCHER + RedisConst.Redis_Key_CpnVchCodeSendQuota, result, DateTimeHelper.GetSecondsUntilMidnight());
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("CpnVchCodeSendQuota.Exception:" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "CpnVchCodeSendQuota", 0, "", e.Message.ToString());
                return null;
            }
        }
        public CpnVoucherCodeSendData GetCpnVchCodeSend(RedisManager redisCluster, KibanaService _kibana, CpnVoucherCodeSendData cpnVchCodeSend, string orderNo)
        {
            string keyRedisCodeSend = REDIS_KEY_VOUCHER + "CpnVchCodeSend";
            try
            {
                if (cpnVchCodeSend != null)
                {
                    redisCluster.HashSet<CpnVoucherCodeSendData>(keyRedisCodeSend, cpnVchCodeSend.OrderNo, cpnVchCodeSend);
                    return cpnVchCodeSend;
                }

                return redisCluster.HashGet<CpnVoucherCodeSendData>(keyRedisCodeSend, orderNo);
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("CpnVchCodeSend.Exception:" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "CpnVchCodeSend", 0, "", e.Message.ToString());
                return null;
            }
        }
        public CpnVchCodeQuotaRemn GetRemnQuotaVoucher(RedisManager redisCluster, KibanaService _kibana, CpnVchCodeQuotaRemn cpnVchRemn, string phoneNumber)
        {
            string keyRedis = REDIS_KEY_VOUCHER + "CpnVchCodeRemn";
            try
            {
                if (cpnVchRemn != null)
                {
                    var getDataRemn = redisCluster.HashGet<CpnVchCodeQuotaRemn>(keyRedis, cpnVchRemn.PhoneNumber);
                    if (getDataRemn != null)
                    {
                        cpnVchRemn.Qty += getDataRemn.Qty;
                    }
                    redisCluster.HashSet<CpnVchCodeQuotaRemn>(keyRedis, cpnVchRemn.PhoneNumber, cpnVchRemn);
                    return cpnVchRemn;
                }
                return redisCluster.HashGet<CpnVchCodeQuotaRemn>(keyRedis, phoneNumber);
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("GetRemnQuotaVoucher.Exception:" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "GetRemnQuotaVoucher", 0, "", e.Message.ToString());
                return null;
            }
        }
        public CpnVchBOMHeaderDto GetCpnVchBOMHeader(RedisManager redisCluster, KibanaService _kibana, string itemNo)
        {
            try
            {
                var result = redisCluster.HashGet<CpnVchBOMHeaderDto>(REDIS_KEY_CpnVchBOMHeader, itemNo);
                if (result == null)
                {
                    using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                    {
                        db.Open();
                        string query = @"SELECT * FROM CpnVchBOMHeader (NOLOCK) WHERE ItemNo = @ItemNo;";
                        result = db.QueryFirstOrDefault<CpnVchBOMHeaderDto>(query, new { ItemNo = itemNo });
                        redisCluster.HashSet(REDIS_KEY_CpnVchBOMHeader, itemNo, result, DateTimeHelper.GetSecondsUntilMidnight());
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("GetCpnVchBOMHeader.Exception:" + e.Message.ToString());
                _kibana.LogException("Redis/Timeouts", "GetCpnVchBOMHeader", 0, "", e.Message.ToString());
                return null;
            }
        }
        public Gift_CouponValidTime GetGift_CouponValidTime(RedisManager redisCluster, KibanaService _kibana, string serialNumber)
        {
            try
            {
                var result = redisCluster.HashGet<Gift_CouponValidTime>(REDIS_KEY_Gift_CouponValidTime, serialNumber);
                if (result == null)
                {
                    using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                    {
                        db.Open();
                        string query = @"SELECT TOP 1 * FROM Gift_CouponValidTime (NOLOCK) WHERE [CouponCode] = '" + serialNumber + "' ORDER BY [CrtDate] DESC;";
                        result = db.Query<Gift_CouponValidTime>(query).FirstOrDefault();
                        if(result != null)
                        {
                            redisCluster.HashSet(REDIS_KEY_Gift_CouponValidTime, serialNumber, result, ExprireTime);
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("GetGift_CouponValidTime.Exception:" + e.Message.ToString());
                _kibana.LogException("Redis/Timeouts", "GetGift_CouponValidTime", 0, "", serialNumber + "@" + e.Message.ToString());
                return null;
            }
        }
        public void UpdateStatus_Gift_CouponValidTime(RedisManager redisCluster, KibanaService _kibana, RedeemCouponRequestPOS voucher)
        {
            try
            {
                List<Gift_CouponValidTime> lstCouponValidTime = new List<Gift_CouponValidTime>();
                foreach (var item in voucher.ListSeriNo)
                {
                    var result = redisCluster.HashGet<Gift_CouponValidTime>(REDIS_KEY_Gift_CouponValidTime, item.SeriNo);
                    if (result != null)
                    {
                        result.IsUsed = true;
                        result.OrderNoUsed = voucher.OrderNo;
                        result.GiftCode = voucher.PosID;
                        redisCluster.HashSet(REDIS_KEY_Gift_CouponValidTime, item.SeriNo, result, ExprireTime);
                        lstCouponValidTime.Add(result);
                    }
                }
                if(lstCouponValidTime != null && lstCouponValidTime.Count > 0)
                {
                    using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                    {
                        db.Open();
                        foreach (var item in lstCouponValidTime)
                        {
                            var checkDataDB = db.Query<Gift_CouponValidTime>("SELECT * FROM Gift_CouponValidTime (NOLOK) WHERE [CouponCode] = '" + item.CouponCode + "';").FirstOrDefault();
                            if (checkDataDB != null)
                            {
                                string query = @"UPDATE Gift_CouponValidTime SET IsUsed = @IsUsed, OrderNoUsed = @OrderNoUsed, GiftCode = @GiftCode WHERE [CouponCode] = @CouponCode;";
                                db.Execute(query, item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("UpdateStatus_Gift_CouponValidTime.Exception:" + JsonConvert.SerializeObject(e));
                _kibana.LogException("UpdateStatus_Gift_CouponValidTime", voucher.PosID, 0, "", JsonConvert.SerializeObject(voucher.ListSeriNo) + "@" +JsonConvert.SerializeObject(e));
            }
        }
        public (bool, string) Validate_Gift_CouponValidTime(RedisManager redisCluster, KibanaService _kibana, string serialNumber)
        {
            try
            {
                var dataCouponValidTime = GetGift_CouponValidTime(redisCluster, _kibana, serialNumber);
                if (dataCouponValidTime == null) 
                {
                    using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                    {
                        db.Open();
                        dataCouponValidTime = db.Query<Gift_CouponValidTime>("SELECT * FROM Gift_CouponValidTime (NOLOCK) WHERE CouponCode = '"+ serialNumber + "'").FirstOrDefault();
                    }
                }

                if (dataCouponValidTime != null)
                {
                    if (dataCouponValidTime.FromDate.Date > DateTime.Now.Date)
                    {
                        return (false, $"{serialNumber} có hiệu lực từ ngày {dataCouponValidTime.FromDate.Date:dd/MM/yyyy}, hiện tại chưa thể sử dụng");
                    }
                    else if (dataCouponValidTime.ToDate.Date >= DateTime.Now.Date && dataCouponValidTime.FromDate.Date <= DateTime.Now.Date)
                    {
                        return (true, $"{serialNumber} có hiệu lực từ {dataCouponValidTime.FromDate.Date:dd/MM/yyyy} - {dataCouponValidTime.ToDate.Date:dd/MM/yyyy}");
                    }
                    else
                    {
                        return (false, $"{serialNumber} đã hết hạn sử dụng từ {dataCouponValidTime.FromDate.Date:dd/MM/yyyy} - {dataCouponValidTime.ToDate.Date:dd/MM/yyyy}");
                    }
                }
                else
                {
                    return (true, $"{serialNumber} không có trong CouponValidTime");
                }
            }
            catch(Exception ex)
            {
                _kibana.LogException("Validate_Gift_CouponValidTime", "",0,"", serialNumber + "@" + ex.Message.ToString());
                return (true, $"{serialNumber} Exception:" + ex.Message.ToString());
            }
        }
        public bool Save_Gift_CouponValidTime(RedisManager redisCluster, Gift_CouponValidTime gift_CouponValidTime)
        {
            try
            {
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    string query = @"INSERT INTO [dbo].[Gift_CouponValidTime]
                                    ([PosNo],[MaterialNo],[CouponCode],[FromDate],[ToDate],[MemberCard],[OrderNo],[GiftCode],[IsUsed],[OrderNoUsed],[IsSync],[Message],[CrtDate])" +
                                    " VALUES(@PosNo,@MaterialNo,@CouponCode,@FromDate,@ToDate,@MemberCard,@OrderNo,@GiftCode,@IsUsed,@OrderNoUsed,@IsSync,@Message,@CrtDate)";
                    var result = db.Execute(query, new
                    {
                        gift_CouponValidTime.PosNo,
                        gift_CouponValidTime.MaterialNo,
                        gift_CouponValidTime.CouponCode,
                        gift_CouponValidTime.FromDate,
                        gift_CouponValidTime.ToDate,
                        gift_CouponValidTime.MemberCard,
                        gift_CouponValidTime.OrderNo,
                        gift_CouponValidTime.GiftCode,
                        gift_CouponValidTime.OrderNoUsed,
                        gift_CouponValidTime.IsSync,
                        gift_CouponValidTime.Message,
                        gift_CouponValidTime.IsUsed,
                        CrtDate = DateTime.Now
                    });
                    if (result > 0)
                    {
                        redisCluster.HashSet(REDIS_KEY_Gift_CouponValidTime, gift_CouponValidTime.CouponCode, gift_CouponValidTime, ExprireTime);
                    }
                }               
                return true;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("SetGift_CouponValidTime.Exception:" + e.Message.ToString() + Environment.NewLine + JsonConvert.SerializeObject(gift_CouponValidTime));
                return false;
            }
        }
        public void Create_Gift_CouponValidTime(RedisManager redisCluster, KibanaService _kibana, SAPPL_Created_VchCpn_POS_Request cpnVchCodeCreate, string mess = "")
        {
            try
            {
                foreach (var item in cpnVchCodeCreate.ListSerial)
                {
                    var checkCpnVchHeader = GetCpnVchBOMHeader(redisCluster, _kibana, item.ArticleNo);
                    if (checkCpnVchHeader != null) 
                    { 
                        var activeAfterDay = checkCpnVchHeader.ActiveAfterDay??0;
                        var validDay = checkCpnVchHeader.ValidDay ?? 0;
                        if (activeAfterDay > 0 || validDay > 0)
                        {
                            DateTime fromDate = DateTimeHelper.ConvertStrToDate(item.FromDate,"yyyyMMdd");
                            DateTime toDate = DateTimeHelper.ConvertStrToDate(item.ToDate, "yyyyMMdd");
                            //if (activeAfterDay > 0)
                            //{
                            //    fromDate = DateTime.Now.AddDays(activeAfterDay);
                            //}
                            //if (validDay > 0)
                            //{
                            //    toDate = DateTime.Now.AddDays(activeAfterDay + validDay);
                            //}

                            var gift_CouponValidTime = new Gift_CouponValidTime()
                            {
                                PosNo = cpnVchCodeCreate.POSTerminal,
                                MaterialNo = item.ArticleNo,
                                CouponCode = item.SerialNumber,
                                FromDate = fromDate,
                                ToDate = toDate,
                                MemberCard = item.Type??"",
                                OrderNo = "",
                                GiftCode = "",
                                IsUsed = false,
                                OrderNoUsed = "",
                                IsSync = false,
                                Message = mess,
                                CrtDate = DateTime.Now
                            };
                            if(!Save_Gift_CouponValidTime(redisCluster, gift_CouponValidTime))
                            {
                                Thread.Sleep(500);
                                Save_Gift_CouponValidTime(redisCluster, gift_CouponValidTime);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("Create_Gift_CouponValidTime.Exception:" + e.Message.ToString());
                _kibana.LogException("Create_Gift_CouponValidTime", cpnVchCodeCreate.POSTerminal, 0, "", $"{JsonConvert.SerializeObject(cpnVchCodeCreate.ListSerial)}_{JsonConvert.SerializeObject(e)}");
            }
        }
        public Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel> SendCodeToPOS(CpnVchCodeSendRequest model)
        {
            var modelData = new CpnVchCodeSendModel();
            var result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.BadRequest, "", modelData);
            try
            {
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    try
                    {
                        HttpStatusCode statusCode = HttpStatusCode.OK;
                        string mess = "Success";
                        modelData = db.Query<CpnVchCodeSendModel>("[dbo].[SP_CpnVchCodeSend] @ItemNo,@StoreNo,@PosID,@OrderNo,@PhoneNumber,@QtyOfDay,@LimitQty",
                                    new
                                    {
                                        model.ItemNo,
                                        model.StoreNo,
                                        model.PosID,
                                        model.OrderNo,
                                        model.PhoneNumber,
                                        model.QtyOfDay,
                                        model.LimitQty
                                    }
                                    ).FirstOrDefault();

                        if (modelData == null)
                        {
                            result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.NotFound, $"Không tìm thấy mã Coupon/voucher cho item {model.ItemNo} này", modelData);
                            return result;
                        }

                        if (modelData != null && !string.IsNullOrEmpty(modelData.ItemNo) && string.IsNullOrEmpty(modelData.SerialNumber))
                        {
                            statusCode = HttpStatusCode.NotFound;
                            if (modelData.ItemNo == "OUT")
                            {
                                mess = $"Số điện thoại {model.PhoneNumber} đã hết hạn mức nhận {model.LimitQty} voucher";
                            }
                            else if (modelData.ItemNo == "OUT_OF_DAY")
                            {
                                mess = $"Số điện thoại {model.PhoneNumber} đã hết hạn mức nhận {model.QtyOfDay} voucher của ngày hôm nay";
                            }
                        }
                        else
                        {
                            var insertCodeSend = new CpnVchCodeSendDto
                            {
                                StoreNo = model.StoreNo,
                                PosID = model.PosID,
                                Date = model.BussinessDate,
                                OrderNo = model.OrderNo,
                                ItemNo = model.ItemNo,
                                Code = modelData.SerialNumber,
                                PhoneNumber = model.PhoneNumber ?? "",
                                CreatedDate = DateTime.Now
                            };
                            string insertQuery = @"INSERT INTO [dbo].[CpnVchCodeSend]([StoreNo],[PosID],[Date],[OrderNo],[ItemNo],[Code],[PhoneNumber],[CreatedDate])
                                                       VALUES (@StoreNo,@PosID,@Date,@OrderNo,@ItemNo,@Code,@PhoneNumber, getdate())";
                            db.Execute(insertQuery, insertCodeSend);
                        }
                        result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(true, statusCode, mess, modelData);
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.BadRequest, ex.Message.ToString(), modelData);
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("DeleteRedis_CpnVchCodeSendQuota.Exception:" + e.Message.ToString());
                result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.BadRequest, e.Message, null);
            }
            return result;
        }
    }
}
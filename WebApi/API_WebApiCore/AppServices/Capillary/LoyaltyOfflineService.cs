using Dapper;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Http.Results;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore
{
    public class LoyaltyOfflineService
    {
        private readonly KibanaService _kibanaService;
        private readonly LoyaltyService _loyaltyService;
        private readonly RedisManager _redisManager;
        private readonly string keyIsOfflineCapillary = AppCodeEnum.IsOfflineCapillary.ToString();
        public LoyaltyOfflineService(RedisManager redisManager) 
        {
            _kibanaService = new KibanaService();
            _loyaltyService = new LoyaltyService();
            _redisManager = redisManager;
        }
        public async Task<ResultResponse> CheckIsOfflineCapillary()
        {
            var result = await IsOfflineCapillary();
            if (result == false) 
            {
                return new ResultResponse
                {
                    Data = new {Status = "Online" },
                    Message = $"Capillary đang ở chế độ Online",
                    Status = HttpStatusCode.OK
                };
            }
            else
            {
                return new ResultResponse
                {
                    Data = new { Status = "Offline" },
                    Message = $"Capillary đang ở chế độ Offline",
                    Status = HttpStatusCode.OK
                };
            }
        }
        public async Task<ResultResponse> IsCapillaryAction(string status)
        {
            try
            {
                int blocked = 1;
                if (status.ToLower() == "offline")
                {
                    blocked = 0;
                }
                else if (status.ToLower() == "online")
                {
                    blocked = 1;
                }
                else
                {
                    return new ResultResponse
                    {
                        Data = null,
                        Message = $"Trạng thái không hợp lệ. Vui lòng sử dụng 'online' hoặc 'offline'.",
                        Status = HttpStatusCode.BadRequest
                    };
                }
                
                string query = $"UPDATE [dbo].[MemoryCacheConfig] SET Blocked = {blocked} WHERE Code = 'IsOfflineCapillary';";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    var result =  await db.ExecuteAsync(query);
                    if (result > 0)
                    {
                        _redisManager.Delete(keyIsOfflineCapillary);
                        _localCache.Remove(_localOfflineCacheKey);
                        return await CheckIsOfflineCapillary();
                    }
                    return new ResultResponse
                    {
                        Data = null,
                        Message = $"Không tìm thấy cấu hình MemoryCacheConfig",
                        Status = HttpStatusCode.BadRequest
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Data = new { Status = "Offline" },
                    Message = $"Exception: {ex.Message}",
                    Status = HttpStatusCode.BadRequest
                };
            }
        }
        private async Task<string> GetIsOfflineCapillaryConfig()
        {
            try
            {
                string query = "SELECT IIF([Blocked] = 1, 'ON', 'OFF') AS CapillaryConfig FROM [dbo].[MemoryCacheConfig] (NOLOCK) WHERE Code = 'IsOfflineCapillary';";
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    var result = db.Query<string>(query).FirstOrDefault();
                    if (string.IsNullOrEmpty(result))
                    {
                        string insQuery = @"INSERT INTO [dbo].[MemoryCacheConfig] ([Code],[CacheType],[Desc],[Blocked],[ListStore]) VALUES('IsOfflineCapillary','IISMemmoryCache','Chuyển Loyalty CAP sang Offline',0,'False: OFFLINE; True: ONLINE')";
                        await db.ExecuteAsync(insQuery);
                        return "ON";
                    }
                    return result.ToUpper();
                }
            }
            catch
            {
                return "";
            }
        }
        private bool IsOfflineCapillaryMapping(string result)
        {
            if(result.ToUpper() == "ON")
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private const string _localOfflineCacheKey = "local:IsOfflineCapillary";
        private static readonly System.Runtime.Caching.MemoryCache _localCache = System.Runtime.Caching.MemoryCache.Default;
        private bool SetLocalCacheAndReturn(bool value)
        {
            _localCache.Set(_localOfflineCacheKey, value,
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(10) });
            return value;
        }
        public async Task<bool> IsOfflineCapillary()
        {
            var localCached = _localCache.Get(_localOfflineCacheKey);
            if (localCached != null)
                return (bool)localCached;

            try
            {
                var checkIsLoyaltyOffline = await _redisManager.GetIsOfflineCapillary(keyIsOfflineCapillary);
                if (string.IsNullOrEmpty(checkIsLoyaltyOffline))
                {
                    var isCappillary = await GetIsOfflineCapillaryConfig();
                    if (!string.IsNullOrEmpty(isCappillary))
                    {
                        _redisManager.StringSetString(keyIsOfflineCapillary, isCappillary, expiry:false);
                        return SetLocalCacheAndReturn(IsOfflineCapillaryMapping(isCappillary));
                    }
                    else
                    {
                        return false;
                    }
                }

                var result = IsOfflineCapillaryMapping(checkIsLoyaltyOffline);
                return SetLocalCacheAndReturn(result);
            }
            catch(Exception ex)
            {
                _kibanaService.LogException("IsOfflineCapillary", "",0,"",JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        public ResultResponse GetMemberInfoOfflineSwitch(RedisManager redisManager, LoyaltyRepository loyaltyRepository, string phoneNumber, string storeNo = "", string posNo = "", string techMsg = "")
        {
            var custInfo = new InfoMemberModel
            {
                CardNumber = FormatHelper.PhoneNumberVietNam(phoneNumber),
                VirtualCard = phoneNumber,
                CMND = "",
                MemberName = "OFF",
                Title = "Anh",
                CardLevel = "",
                MemberCSN = phoneNumber,
                PhoneNumber = FormatHelper.PhoneNumberVietNam(phoneNumber),
                OtherInfo = "",
                QRCode = "",
                Dob = "",
                DateOfBirth = "",
                BirthdayGiftInd = false,
                MemberPoint = 0,
                TotalPoint = 0,
                ExtraPoint = false,
                CurrentRate = 0,
                IsOfflineVinID = false,
                IsShowMessage = false,
                IsRedeem = false,
                Status = "Offline",
                System = "CAP",
                ClubCode = ChannelCapEnum.WINCARE.ToString(),
                Email = "",
                Gender = "Female",
                Address = "",
                ExternalId = "",
                AvailablePromotion = _loyaltyService.GetWinCodePromotion(storeNo, posNo, phoneNumber, null),
                MemberBusiness = null,
                OtherStatus = null,
                ExtendedFields = null,
                PointsSummaries = null,
                Source = LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, storeNo),
                MemberType = MemberTypeLoyaltyEnum.WIN.ToString(),
            };
            
            try
            {
                var cusInRedis = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(FormatHelper.PhoneNumberVietNam(phoneNumber)), FormatHelper.PhoneNumberVietNam(phoneNumber));
                if (cusInRedis != null) 
                { 
                    custInfo.MemberPoint = cusInRedis.MemberPoint;
                    custInfo.TotalPoint = cusInRedis.TotalPoint;
                    custInfo.CurrentRate = cusInRedis.CurrentRate;
                    custInfo.CardNumber = cusInRedis.CardNumber;
                    custInfo.MemberName = cusInRedis.MemberName;
                    custInfo.Gender = cusInRedis.Gender;
                    custInfo.Address = "InRedis";
                }
            }
            catch (Exception ex) 
            { 
                FileHelper.WriteExpLogs($"GetMemberInfoOfflineSwitch {phoneNumber}", ex);
            }
            return ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", custInfo, techMsg);
        }
        public ResultResponse GetAddTransactionOfflineSwitch(string orderNo, string phoneNumber)
        {
            return ResponseHelper.ResponseData(HttpStatusCode.OK, $"Offline",
                                        new PointModePOSResponse()
                                        {
                                            PointEarn = 0,
                                            PointRedeem = 0,
                                            CurrentRate = 0,
                                            IsOfflineVinID = true,
                                            OrderNo = orderNo ?? ""
                                        },
                                        $"HV {phoneNumber} đang chạy tích điểm offline"
                                        );
        }

    }
}
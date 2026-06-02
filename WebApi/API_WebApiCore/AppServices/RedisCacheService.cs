using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VCM.POSBLUE.Model.Common;
using Dapper;
using System.Diagnostics;
using System.Threading;
using TCX.API.Common.Models;
using TCX.API.Common.Enums;
using System.Configuration;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.API.Common.Helpers;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using Newtonsoft.Json;
using TCX.API.Common.Shared;
using System.Runtime.Caching;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Telegram;
using TCX.API.Common.Dtos.CentralMD;
using StackExchange.Redis;
using TCX.WebApiCore.DbContext;

namespace TCX.WebApiCore
{
    public class RedisCacheService
    {
        private readonly DapperContext _dapperCentralMD;
        private readonly DapperContext _dapperLoaylty;
        private readonly KibanaService _kibana;
        private readonly int sleep = 50;
        private readonly IDatabase dbRedis;
        private readonly RedisManager _redisManager;
        public RedisCacheService()
        {
            _dapperCentralMD = new DapperContext(AppGlobals.ConnStrCentralMD);
            _dapperLoaylty = new DapperContext(AppGlobals.ConnStrLoyalty);
            _kibana = new KibanaService();
            dbRedis = RedisManager.GetDatabase();
            _redisManager = new RedisManager();
        }
        public bool CheckExistsKey(string key)
        {
            return dbRedis.KeyExists(key);
        }
        public bool Delete(string key)
        {
            if (CheckExistsKey(key))
                return dbRedis.KeyDelete(key);

            return false;
        }
        public T Set<T>(string key, T value, string prefix = null, TimeSpan? timeOut = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                key = prefix + ":" + key;
            }
            if (timeOut == null)
            {
                timeOut = DateTimeHelper.GetTimeUntilHourInTomorrow(1);
            }
            return dbRedis.StringSet(key, JsonConvert.SerializeObject(value), timeOut) ? value : default;
        }
        public T Get<T>(string key)
        {
            var value = dbRedis.StringGet(key);
            return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
        }
        public List<string> GetKeys(string patern_key)
        {
            List<string> result = new List<string>();
            try
            {
                var keys = _redisManager.GetAllKeys(patern_key + "*");
                if (keys.Count() > 0)
                {
                    foreach (var item in keys)
                    {
                        result.Add(item);
                    }
                }
            }
            finally
            {

            }
            return result;
        }
        public List<POSDataSetupModel> GetPOSDataSetupAll()
        {
            long responseTime;
            var st1 = new Stopwatch();
            st1.Start();
            try
            {
                var result = new List<POSDataSetupModel>();
                result = Get<List<POSDataSetupModel>>(RedisConst.Redis_Key_POSDataSetup);
                if (result == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        result = db.Query<POSDataSetupModel>("SELECT * FROM POSDataSetup(NOLOCK)").ToList();
                        Set(RedisConst.Redis_Key_POSDataSetup, result);
                    }
                }
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();

                return result;
            }
            catch (Exception e)
            {
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                FileHelper.WriteLogs("GetPOSDataSetupAll Exception:" + responseTime.ToString() + "@" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "GetPOSDataSetupAll", responseTime, "", e.Message.ToString());
                return null;
            }
        }
        public List<SysWebApiUserDto> GetSysWebApiUser(string connectStringDb)
        {
            long responseTime;
            var st1 = new Stopwatch();
            st1.Start();
            try
            {
                var result = Get<List<SysWebApiUserDto>>(RedisConst.Redis_Key_SysWebApiUser);
                if (result == null)
                {
                    List<SysWebApiUserDto> routes = new List<SysWebApiUserDto>();
                    var dapper = new DapperContext(connectStringDb);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        result = db.Query<SysWebApiUserDto>("SELECT * FROM SysWebApiUser NOLOCK WHERE Blocked = 0;").ToList();
                        Set(RedisConst.Redis_Key_SysWebApiUser, result);
                    }
                }
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                return result;
            }
            catch (Exception e)
            {
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                FileHelper.WriteLogs(RedisConst.Redis_Key_SysWebApiUser + " Exception:" + responseTime.ToString() + "@" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "GetSysWebApiUser", responseTime, "", e.Message.ToString());
                return null;
            }
        }
        public MemberBalanceDto GetMemberBalance(string memberCard)
        {
            long responseTime;
            var st1 = new Stopwatch();
            st1.Start();
            try
            {
                RedisConnectionHelper redisConnectionHelper = new RedisConnectionHelper();
                string key = PrefixRedisEnum.MEMBERBALANCE.ToString();
                //var result = redisConnectionHelper.Get<MemberBalanceDto>(key);
                var result = redisConnectionHelper.HashGetValueIList<MemberBalanceDto>(PrefixRedisEnum.MEMBERBALANCE.ToString(), NumberHelper.TryParseInt64(memberCard));
                if (result == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();

                        if(!redisConnectionHelper.CheckExistsKey(key))
                        {
                            var dataMemberBlance = db.Query<MemberBalanceDto>("SELECT * FROM MemberBalance (NOLOCK) WHERE Enabled = 1;").ToList();
                            if (dataMemberBlance != null && dataMemberBlance.Count > 0)
                            {
                                foreach (var item in dataMemberBlance)
                                {
                                    redisConnectionHelper.HashSetIList(PrefixRedisEnum.MEMBERBALANCE.ToString(), NumberHelper.TryParseInt64(item.MemberCard), JsonConvert.SerializeObject(item));
                                }
                                result = dataMemberBlance.FirstOrDefault(x => x.MemberCard == memberCard);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            result = db.Query<MemberBalanceDto>("SELECT * FROM MemberBalance (NOLOCK) WHERE Enabled = 1 AND MemberCard = '" + memberCard + "';").ToList().FirstOrDefault();
                            if (result != null)
                            {
                                redisConnectionHelper.HashSetIList(key, NumberHelper.TryParseInt64(memberCard), JsonConvert.SerializeObject(result));
                            }
                        }
                    }
                }
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                return result;
            }
            catch (Exception e)
            {
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibana.LogResponse("Redis/Timeouts", "GetMemberBalance", responseTime, "", e.Message.ToString());
                FileHelper.WriteLogs(PrefixRedisEnum.MEMBERBALANCE.ToString() + " Exception:" + e.Message.ToString());
                return null;
            }
        }
        public bool CheckQueueRequestInRedis(string parent_key, string key, int request_limit, ref int processing)
        {
            try
            {
                int count = 0;
                var list_key = GetKeys(parent_key);
                foreach (var item in list_key)
                {
                    if (item.Contains(key))
                    {
                        count++;
                    }
                }
                processing = count;
                if (count > request_limit && request_limit > 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("QueueRequestInRedis", ex);
                return true;
            }
        }
        public List<ExtendedFieldLoyalty> GetExtendedFieldLoyalty()
        {
            long responseTime;
            var st1 = new Stopwatch();
            st1.Start();
            try
            {
                var result = new List<ExtendedFieldLoyalty>();
                result = Get<List<ExtendedFieldLoyalty>>(RedisConst.Redis_Key_OtherStatusLoyalty);
                if (result == null)
                {
                    using (IDbConnection db = _dapperLoaylty.CreateConnDB)
                    {
                        db.Open();
                        result = db.Query<ExtendedFieldLoyalty>("SELECT * FROM ExtendedFields (NOLOCK) WHERE Blocked = 0;").ToList();
                        Set(RedisConst.Redis_Key_OtherStatusLoyalty, result);
                    }
                }
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();

                return result;
            }
            catch (Exception e)
            {
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                FileHelper.WriteLogs("GetOtherStatusLoyalty Exception:" + responseTime.ToString() + "@" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "GetOtherStatusLoyalty", responseTime, "", e.Message.ToString());
                return null;
            }
        }
        public List<NotifyTelegram> NotifyTelegramConfig()
        {
            long responseTime;
            var st1 = new Stopwatch();
            st1.Start();
            try
            {
                var result = new List<NotifyTelegram>();
                result = Get<List<NotifyTelegram>>(RedisConst.Redis_Key_NotifyTelegram);
                if (result == null)
                {
                    using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                    {
                        db.Open();
                        result = db.Query<NotifyTelegram>("SELECT * FROM NotifyTelegram (NOLOCK) WHERE [Blocked] = 0;").ToList();
                        Set(RedisConst.Redis_Key_NotifyTelegram, result);
                    }
                }
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();

                return result;
            }
            catch (Exception e)
            {
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                FileHelper.WriteLogs("NotifyTelegramConfig Exception:" + responseTime.ToString() + "@" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "NotifyTelegramConfig", responseTime, "", e.Message.ToString());
                return null;
            }
        }
        public CpnVchBOMHeaderDto GetCpnVchBOMHeader(string itemNo)
        {
            string keyRedis = @"BLUEPOS:" + RedisConst.Redis_Key_CpnVchBOMHeader;
            try
            {
                var result = _redisManager.HashGet<CpnVchBOMHeaderDto>(keyRedis, itemNo, true);
                if (result == null)
                {
                    using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                    {
                        db.Open();
                        result = db.Query<CpnVchBOMHeaderDto>("SELECT TOP 1 * FROM [CpnVchBOMHeader] (NOLOCK) WHERE ItemNo = '" + itemNo + "';").FirstOrDefault();
                        if (result != null)
                        {
                            _redisManager.HashSet<CpnVchBOMHeaderDto>(keyRedis, itemNo, result);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("GetCpnVchBOMHeader.Exception:" + e.Message.ToString());
                _kibana.LogResponse("Redis/Timeouts", "GetCpnVchBOMHeader", 0, "", e.Message.ToString());
                return null;
            }
        }
    }
}
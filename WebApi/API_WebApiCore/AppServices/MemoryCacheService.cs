using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.Loyalty.WinCode;
using TCX.API.Common.Dtos.StagingDB;
using TCX.API.Common.Dtos.WinCustomer;
using TCX.API.Common.Dtos.WinMoney;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore
{
    public class MemoryCacheService 
    {
        private readonly KibanaService _kibanaService;
        private readonly ObjectCache _cache;
        private readonly string _connectStringDbCentralMD;
        private readonly string _connectStringDbLoyalty;
        private readonly DapperContext _dapperCentralMD;
        public MemoryCacheService() 
        {
            _connectStringDbCentralMD = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
            _connectStringDbLoyalty = ConfigurationManager.ConnectionStrings["DAPPER_LOYALTY"].ConnectionString;
            _dapperCentralMD = new DapperContext(_connectStringDbCentralMD);
            _cache = MemoryCache.Default;
            _kibanaService = new KibanaService();
        }
        public string GetEnvironment()
        {
            return AppGlobals.Environment;
        }
        public string GetStringDbCentralMD()
        {
            return _connectStringDbCentralMD;
        }
        public string GetStringDbLoyalty()
        {
            return _connectStringDbLoyalty;
        }
        public void SetCache(string key, object data)
        {
            MemoryCache.Default.Set(key, data, GetPolicyMemoryCache());
        }
        public T GetCache<T>(string key) where T : class
        {
            return MemoryCache.Default.Get(key) as T;
        }
        public string GetConnectStringCentralMD()
        {
            return _connectStringDbCentralMD;
        }
        public T Get<T>(string key)
        {
            if ((_cache[key] is string memoryData))
            {
                return StringHelper.StringToObject<T>(memoryData);
            }
            return default;
        }
        public void Set<T>(string key, T data)
        {
            _cache.Set(key, JsonConvert.SerializeObject(data), GetPolicyMemoryCache());
        }
        private CacheItemPolicy GetPolicyMemoryCache()
        {
            return new CacheItemPolicy()
                    {
                        AbsoluteExpiration = DateTimeHelper.GetMidnightOffset()
                    };
        }
        public List<T> GetAllMemoryCache<T>()
        {
            try
            {
                List<T> list = new List<T>();
                IDictionaryEnumerator cacheEnumerator = (IDictionaryEnumerator)((IEnumerable)_cache).GetEnumerator();
                while (cacheEnumerator.MoveNext())
                    list.Add((T)cacheEnumerator.Key);

                return list;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("GetAllMemoryCache Exception" + JsonConvert.SerializeObject(e));
                return null;
            }
        }
        public List<string> DeleteMemoryCache(string keyMemory)
        {
            try
            {
                List<string> result = new List<string>();
                if (string.IsNullOrEmpty(keyMemory))
                {
                    var lstKey = GetAllMemoryCache<string>();
                    foreach (string key in lstKey)
                    {
                        _cache.Remove(key);
                        result.Add(key);
                    }
                }
                else
                {
                    _cache.Remove(keyMemory);
                    result.Add(keyMemory);
                }

                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("DeleteAllMemoryCache Exception" + JsonConvert.SerializeObject(e));
                return null;
            }
        }
        public LoyaltyRateDto GetLoyaltyRateData(string code)
        {
            try
            {
                List<LoyaltyRateDto> loyaltyRate = null;
                if (!(_cache[RedisConst.Redis_Key_LoyaltyRate] is string memoryData))
                {
                    var queryData = @"SELECT FromDate, ToDate, Code, Rate, IIF([Enable] = 1, 0, 1) AS Blocked, Pkey, CardType FROM LoyaltyRate (NOLOCK) WHERE [Enable] = 1;";
                    using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                    {
                        db.Open();
                        loyaltyRate =  db.Query<LoyaltyRateDto>(queryData).ToList();
                        if (loyaltyRate == null)
                        {
                            return null;
                        }
                        _cache.Set(RedisConst.Redis_Key_LoyaltyRate, JsonConvert.SerializeObject(loyaltyRate), GetPolicyMemoryCache());
                    }
                }
                else
                {
                    loyaltyRate = JsonConvert.DeserializeObject<List<LoyaltyRateDto>>(memoryData);
                }

                if(loyaltyRate != null)
                {
                    return loyaltyRate.FirstOrDefault(x => x.Code == code);
                }
                return null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetLoyaltyRateData.Exception", ex);
                return null;
            }
        }
        public List<CardLevelDto> GetCardLevelMapping(string connectStringDb)
        {
            try
            {
                if (!(_cache[MemoryCacheConst.MemoryCardLevel] is string memoryData))
                {
                    var dapper = new DapperContext(connectStringDb);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var data = db.Query<CardLevelDto>("SELECT * FROM CardLevel NOLOCK WHERE Blocked = 0;").ToList();
                         if (data != null)
                        {
                            _cache.Set(MemoryCacheConst.MemoryCardLevel, JsonConvert.SerializeObject(data), GetPolicyMemoryCache());
                            _kibanaService.LogRequest("MemoryCache/Set", "GetCardLevelMapping", "", JsonConvert.SerializeObject(data));
                            return data;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                return JsonConvert.DeserializeObject<List<CardLevelDto>>(memoryData);
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("MemoryCache GetCardLevelMapping Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<StoreDto> GetStores()
        {
            try
            {
                var result = GetCache<List<StoreDto>>(MemoryCacheConst.MemoryCacheStores);
                if (result == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var dataStore = db.Query<StoreDto>(@"SELECT DISTINCT S.[No], S.[Name], S.[Address], S.[BusinessAreaNo], S.[PostCode], S.[TaxCode], S.[ResponsibilityCenter], C.ConnectionString " +
                                                            " FROM Store (NOLOCK) S " +
                                                            " INNER JOIN CentralGeneral.dbo.StoreSetServer (NOLOCK) T ON S.[No] = T.StoreNo AND T.[Status] = 1 " +
                                                            " INNER JOIN SysWebApiConfig (NOLOCK) C ON C.Prefix = T.ServerIP AND C.Code = 'KAFKA' " +
                                                            " WHERE S.ClosingMethod = 0;").ToList();
                        
                        if (dataStore != null && dataStore.Count > 0)
                        {
                            List<StoreDto> result2 = new List<StoreDto>();
                            foreach (var item in dataStore)
                            {
                                result2.Add(new StoreDto()
                                {
                                    No = item.No,
                                    StoreNo = item.No,
                                    Name = item.Name,
                                    Address = item.Address,
                                    BusinessAreaNo = item.BusinessAreaNo,
                                    PostCode = item.PostCode,
                                    TaxCode = item.TaxCode,
                                    ResponsibilityCenter = item.ResponsibilityCenter,
                                    ConnectionString = item.ConnectionString,
                                });
                            }
                            SetCache(MemoryCacheConst.MemoryCacheStores, result2);
                            FileHelper.WriteLogs($"MemoryCacheStores {result2.Count}");
                            return result2;
                        }
                        else
                        {
                            FileHelper.WriteLogs("Error get GetAllStores");
                            return null;
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("MemoryCacheStores.Exception:" + "@" + JsonConvert.SerializeObject(e));
                return null;
            }
        }
        public List<StoreSetup> GetStoreSetup()
        {
            try
            {
                var result = GetCache<List<StoreSetup>>(MemoryCacheConst.MemoryCacheStoreSetup);
                if (result == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var dataStore = db.Query<StoreSetup>(@"SELECT ISNULL(StoreNo, '') StoreNo, ISNULL(Code, '') Code, ISNULL([Value], '') [Value], ISNULL([Status], 0) [Status] FROM StoreSetup (NOLOCK);").ToList();
                        if (dataStore != null && dataStore.Count > 0)
                        {
                            List<StoreSetup> result2 = new List<StoreSetup>();
                            foreach (var item in dataStore)
                            {
                                result2.Add(new StoreSetup()
                                {
                                    StoreNo = item.StoreNo,
                                    Code = item.Code,
                                    Value = item.Value,
                                    Status = item.Status 
                                });
                            }
                            FileHelper.WriteLogs($"MemoryCacheService.GetStoreSetup: {result2.Count}");
                            SetCache(MemoryCacheConst.MemoryCacheStoreSetup, result2);
                            return result2;
                        }
                        else
                        {
                            FileHelper.WriteLogs("Error get MemoryCacheStoreSetup");
                            return null;
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("MemoryCacheStoreSetup.Exception:" + "@" + JsonConvert.SerializeObject(e));
                return null;
            }
        }
        public List<SysWebApiUserDto> GetSysWebApiUser()
        {
            try
            {
                var result = GetCache<List<SysWebApiUserDto>>(MemoryCacheConst.MemoryCacheSysWebUserApi);
                if (result == null)
                {
                    var dapper = new DapperContext(_connectStringDbCentralMD);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var result2 = db.Query<SysWebApiUserDto>("SELECT * FROM SysWebApiUser NOLOCK WHERE Blocked = 0;").ToList();
                        if (result2 != null && result2.Count > 0)
                        {
                            SetCache(MemoryCacheConst.MemoryCacheSysWebUserApi, result2);
                            FileHelper.WriteLogs($"GetSysWebApiUser {result2.Count}");
                            return result2;
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
                FileHelper.WriteLogs(MemoryCacheConst.MemoryCacheSysWebUserApi + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<SysWebApiDto> GetSysWebApi(string connectStringDb)
        {
            try
            {
                var result = GetCache<List<SysWebApiDto>>(MemoryCacheConst.MemoryCacheSysWebApi);
                if(result == null)
                {
                    List<SysWebApiDto> routes = new List<SysWebApiDto>();
                    var dapper = new DapperContext(connectStringDb);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var sysWebApi = db.Query<SysWebApi>("SELECT * FROM SysWebApi NOLOCK WHERE Blocked = 0;").ToList();
                        var sysWebApiRoute = db.Query<SysWebApiRoute>("SELECT * FROM SysWebApiRoute NOLOCK WHERE Blocked = 0;").ToList();
                        if (sysWebApi != null && sysWebApi.Count > 0)
                        {
                            foreach (var item in sysWebApi)
                            {
                                var routeApi = sysWebApiRoute.Where(x => x.AppCode == item.AppCode).ToList();

                                var dto = new SysWebApiDto()
                                {
                                    AppCode = item.AppCode,
                                    Host = item.Host,
                                    Authorization = item.Authorization,
                                    UserName = item.UserName,
                                    Password = item.Password,
                                    PrivateKey = item.PrivateKey,
                                    PublicKey = item.PublicKey,
                                    Version = item.Version,
                                    Blocked = item.Blocked,
                                    Description = item.Description,
                                    HttpProxy = item.HttpProxy,
                                    Bypasslist = item.Bypasslist,
                                    SysWebApiRoute = sysWebApiRoute?.Where(x => x.AppCode == item.AppCode).ToList()
                                };
                                routes.Add(dto);
                            }
                        }
                        if (routes != null && routes.Count > 0)
                        {
                            if(AppGlobals.Environment == "DEV")
                            {
                                routes.ForEach(x=>x.HttpProxy = null);
                            }
                            SetCache(MemoryCacheConst.MemoryCacheSysWebApi, routes);
                            FileHelper.WriteLogs($"{AppGlobals.Environment}_MemoryCacheSysWebApi {routes.Count}");
                            return routes;
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
                _kibanaService.LogException("GetSysWebApi", "999999", 0, "", e.Message.ToString());
                return null;
            }
        }
        public StoreMappingModel GetStoreMappingVinID(string storeNo, string posID)
        {
            string memoryData = _cache[MemoryCacheConst.MemoryCacheStoreMappingVinID] as string;
            try
            {
                var dataMapping = new List<StoreMappingModel>();
                if (string.IsNullOrEmpty(memoryData))
                {
                    var VinData = new LoyaltyRepository();
                    dataMapping = VinData.GetLoyaltyStoreMapping();
                    if (dataMapping != null)
                    {
                        _cache.Set(MemoryCacheConst.MemoryCacheStoreMappingVinID, JsonConvert.SerializeObject(dataMapping), GetPolicyMemoryCache());
                        _kibanaService.LogRequest("MemoryCache/Set", "MemoryCacheStoreMappingVinID", "", JsonConvert.SerializeObject(dataMapping));
                    }
                }
                else
                {
                    dataMapping = JsonConvert.DeserializeObject<List<StoreMappingModel>>(memoryData);
                }
                return dataMapping.FirstOrDefault(a => a.NewStoreID == storeNo && a.NewTerminalID == posID);
            }
            catch (Exception e)
            {
                FileHelper.WriteExpLogs("MemoryCacheStoreMappingVinID", e);
                return null;
            }

        }
        public List<WinCodeStoreMemoryDto> GetWinCodeMemory(string connectStringDb)
        {
            try
            {
                var result = GetCache<List<WinCodeStoreMemoryDto>>(MemoryCacheConst.MemoryCacheWinCode);
                if (result == null)
                {
                    var dapper = new DapperContext(connectStringDb);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var winCodeHeader = db.Query<WinCodeHeaderDto>("SELECT * FROM WinCodeHeader NOLOCK WHERE Status = 1 AND CAST(FromDate AS DATE) <= CAST(getdate() AS DATE) AND CAST(ToDate AS DATE) >= CAST(getdate() AS DATE);").ToList();
                        var programCode = winCodeHeader.Select(x => x.ProgramCode).ToArray();

                        var wincodeStore = db.Query<WinCodeStoreDto>("SELECT * FROM WinCodeStore NOLOCK WHERE Status = 1 AND ProgramCode IN @programCode;", new { ProgramCode = programCode }).ToList();
                        
                        if (winCodeHeader != null && winCodeHeader.Count > 0 && wincodeStore != null && wincodeStore.Count > 0)
                        {
                            var result2 = new List<WinCodeStoreMemoryDto>();
                            foreach (var item in winCodeHeader)
                            {
                                foreach (var item2 in wincodeStore.Where(x=>x.ProgramCode == item.ProgramCode).ToList())
                                {
                                    result2.Add(new WinCodeStoreMemoryDto()
                                    {
                                        ProgramCode = item2.ProgramCode,
                                        Status = item2.Status,
                                        StoreNo = item2.StoreNo,
                                        WinCode = item.WinCode,
                                        Quantity = item.Quantity,
                                        DiscountType = item.DiscountType,
                                        ApplyType = item.ApplyType
                                    });
                                }
                            }
                            SetCache(MemoryCacheConst.MemoryCacheWinCode, result2);
                            return result2;
                        }
                        return null;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                _kibanaService.LogException("GetWinCodeMemory", "GetWinCodeMemory", 0, "", e.Message.ToString());
                return null;
            }
        }
        public List<StagingDBConfigDto> GetStagingDBConfig(string strConnectDB) 
        {
            try
            {
                var result = GetCache<List<StagingDBConfigDto>>(MemoryCacheConst.MemoryCacheStagingDBConfig);
                if (result == null)
                {
                    var dapper = new DapperContext(strConnectDB);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var result2 = db.Query<StagingDBConfigDto>("SELECT * FROM StagingDBConfig NOLOCK WHERE Blocked = 0;").ToList();
                        if (result2 != null && result2.Count > 0)
                        {
                            SetCache(MemoryCacheConst.MemoryCacheStagingDBConfig, result2);
                            FileHelper.WriteLogs($"GetStagingDBConfig {result2.Count}");
                            return result;
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
                FileHelper.WriteLogs(MemoryCacheConst.MemoryCacheStagingDBConfig + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<MemberBusiness> GetMemberBusinesses(string strConnectDB)
        {
            try
            {
                var result = GetCache<List<MemberBusiness>>(MemoryCacheConst.MemoryMemberBusiness);
                if (result == null)
                {
                    var dapper = new DapperContext(strConnectDB);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var result2 = db.Query<MemberBusiness>("SELECT * FROM MemberBusiness (NOLOCK) WHERE Blocked = 0;").ToList();
                        if (result2 != null && result2.Count > 0)
                        {
                            FileHelper.WriteLogs($"GetMemberBusinesses {result2.Count}");
                            SetCache(MemoryCacheConst.MemoryMemberBusiness, result2);
                            return result2;
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
                FileHelper.WriteLogs(MemoryCacheConst.MemoryMemberBusiness + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<ConditionAccumulation> GetWinPayAccumulateSetup(string strConnectDB)
        {
            try
            {
                var result = new List<ConditionAccumulation>();
                if (!(_cache[MemoryCacheConst.WinPayAccumulateSetup] is string memoryData))
                {
                    var dapper = new DapperContext(strConnectDB);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        var conditionSetup = db.Query<ConditionAccumulation>("SELECT * FROM WinPayAccumulateSetup (NOLOCK) WHERE CAST(getdate() AS DATE) >= FromDate AND CAST(getdate() AS DATE) <= ToDate AND Blocked = 0;").ToList();
                        var conditionStore = db.Query<ConditionAccumulationStore>(@"SELECT * FROM WinPayAccumulateSetupStore (NOLOCK) WHERE Blocked = 0;").ToList();
                        if (conditionSetup != null && conditionSetup.Count > 0)
                        {
                            foreach(var item in conditionSetup)
                            {
                                var stores = conditionStore.Where(x => x.Condition == item.Condition).ToList();
                                if(stores == null)
                                {
                                    continue;
                                }

                                result.Add(new ConditionAccumulation()
                                {
                                    Condition = item.Condition,
                                    IsDay = item.IsDay,
                                    IsMonth = item.IsMonth,
                                    AccumulationType = item.AccumulationType,
                                    AccumulationValue = item.AccumulationValue,
                                    Priority = item.Priority,
                                    Amount = item.Amount,
                                    IsCheckAmount = item.IsCheckAmount,
                                    IsCheckItem = item.IsCheckItem,
                                    Qty = item.Qty,
                                    FromDate = item.FromDate,
                                    ToDate = item.ToDate,
                                    Blocked = item.Blocked,
                                    Description = item.Description,
                                    DayOfWeek = item.DayOfWeek,
                                    CrtDate = item.CrtDate,
                                    ChgDate = item.ChgDate,
                                    ConditionStore = stores
                                });
                            }

                            _cache.Set(MemoryCacheConst.WinPayAccumulateSetup, JsonConvert.SerializeObject(result), GetPolicyMemoryCache());
                            _kibanaService.LogRequest("MemoryCache/Set", "WinPayAccumulateSetup", "", JsonConvert.SerializeObject(result));
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                result = JsonConvert.DeserializeObject<List<ConditionAccumulation>>(memoryData);
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs(MemoryCacheConst.WinPayAccumulateSetup + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<WinMoneyConversion> GetWinMoneyConversions(string strConnectDB)
        {
            try
            {
                var result = new List<WinMoneyConversion>();
                if (!(_cache[MemoryCacheConst.WinMoneyConversion] is string memoryData))
                {
                    var dapper = new DapperContext(strConnectDB);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        result = db.Query<WinMoneyConversion>("SELECT * FROM WinMoneyConversion (NOLOCK) WHERE IsSuccess = 0;").ToList();
                        if (result != null && result.Count > 0)
                        {
                            _cache.Set(MemoryCacheConst.WinMoneyConversion, JsonConvert.SerializeObject(result), GetPolicyMemoryCache());
                            _kibanaService.LogRequest("MemoryCache/Set", "WinMoneyConversion", "", JsonConvert.SerializeObject(result));
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                result = JsonConvert.DeserializeObject<List<WinMoneyConversion>>(memoryData);
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs(MemoryCacheConst.WinMoneyConversion + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<MemoryCacheConfig> GetCacheConfigLoyalty(string strConnectDB)
        {
            try
            {
                var result = new List<MemoryCacheConfig>();
                if (!(_cache[MemoryCacheConst.MemoryCacheConfigLoyalty] is string memoryData))
                {
                    var dapper = new DapperContext(strConnectDB);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        result = db.Query<MemoryCacheConfig>("SELECT * FROM MemoryCacheConfig (NOLOCK);").ToList();
                        if (result != null && result.Count > 0)
                        {
                            _cache.Set(MemoryCacheConst.MemoryCacheConfigLoyalty, JsonConvert.SerializeObject(result), GetPolicyMemoryCache());
                            _kibanaService.LogRequest("MemoryCache/Set", "MemoryCacheConfig", "", JsonConvert.SerializeObject(result));
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                result = JsonConvert.DeserializeObject<List<MemoryCacheConfig>>(memoryData);
                return result;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs(MemoryCacheConst.MemoryCacheConfigLoyalty + " Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<NotifyConfigDto> GetNotifyConfig(string connectStringDb = "")
        {
            try
            {
                connectStringDb = GetConnectStringCentralMD();               
                var memoryData = GetCache<List<NotifyConfigDto>>(MemoryCacheConst.MemoryGetNotifyConfig);
                if (memoryData == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var result = db.Query<NotifyConfigDto>("SELECT * FROM NotifyConfig (NOLOCK) WHERE Blocked = 0;").ToList();
                        FileHelper.WriteLogs($"GetNotifyConfig {result.Count}");
                        SetCache(MemoryCacheConst.MemoryGetNotifyConfig, result);
                        return result;
                    }
                }
                return memoryData;
            }
            catch (Exception e)
            {
                FileHelper.WriteExpLogs("GetNotifyConfig", e);
                return null;
            }
        }
        public POSDataSetupModel GetPOSDataSetup(string key)
        {
            try
            {
                var result =  GetCache<List<POSDataSetupModel>>(MemoryCacheConst.MemoryGetPOSDataSetup);
                if (result == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var result2 = db.Query<POSDataSetupModel>("SELECT * FROM POSDataSetup (NOLOCK)").ToList();
                        FileHelper.WriteLogs($"GetPOSDataSetup {result2.Count}");
                        SetCache(MemoryCacheConst.MemoryGetPOSDataSetup, result2);
                        return result2.FirstOrDefault(x => x.Code.ToUpper() == key.ToUpper());
                    }
                }
                return result.FirstOrDefault(x => x.Code.ToUpper() == key.ToUpper());
            }
            catch (Exception e)
            {
                FileHelper.WriteExpLogs("MemoryCacheService.GetPOSDataSetup.Exception", e);
                return null;
            }
        }
        public List<OfferStaffSetupDto> GetOfferStaffSetup()
        {
            try
            {
                var data = GetCache<List<OfferStaffSetupDto>>(MemoryCacheConst.MemoryOfferStaffSetup);
                if (data == null)
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var data2 = db.Query<OfferStaffSetupDto>(@"SELECT * FROM OfferStaffSetup (NOLOCK) " +
                                                            "WHERE CAST(ApplyFrom AS DATE) >= CAST(getdate() AS DATE) AND CAST(ValidBefore AS DATE) <= CAST(getdate() AS DATE) AND Blocked = 0;"
                                                            ).ToList();
                        if (data2 != null && data2.Count > 0)
                        {
                            FileHelper.WriteLogs($"GetOfferStaffSetup {data2.Count}");
                            SetCache(MemoryCacheConst.MemoryOfferStaffSetup, data2);
                            return data2;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                return data;
            }
            catch (Exception e)
            {
                FileHelper.WriteLogs("MemoryOfferStaffSetup.Exception:" + "@" + e.Message.ToString());
                return null;
            }
        }
        public List<StoreSetConfig> GetStoreSetConfig()
        {
            var dataCache = GetCache<List<StoreSetConfig>>(MemoryCacheConst.MemoryStoreSetConfig);
            if (dataCache != null)
            {
                return dataCache;
            }
            else
            {
                const string query = @"SELECT DISTINCT A.StoreNo, B.* " +
                      " FROM CentralGeneral.dbo.[StoreSetServer] (NOLOCK) A " +
                      " INNER JOIN SysWebApiConfig (NOLOCK) B ON B.[Prefix] = A.[ServerIP] " +
                      " WHERE B.Blocked = 0 AND A.[Status] = 1;";
                try
                {
                    using (IDbConnection db = _dapperCentralMD.CreateConnDB)
                    {
                        db.Open();
                        var result2 = db.Query<StoreSetConfig>(query).ToList();
                        FileHelper.WriteLogs($"GetStoreSetConfig {result2.Count}");
                        SetCache(MemoryCacheConst.MemoryStoreSetConfig, result2);
                        return result2;
                    }
                }
                catch(Exception e) 
                {
                    FileHelper.WriteLogs("GetStoreSetConfig.Exception: " + JsonConvert.SerializeObject(e));
                    return new List<StoreSetConfig>();
                }
            }
        }
        public string GetVersion(string file)
        {
            try
            {
                var result = GetCache<string>(MemoryCacheConst.MemoryVersionWebApi);
                if (result == null)
                {
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file); // "bin\\VCM.POSBLUE.API.dll");
                    string version = HostHelper.GetDateFileDll(dllPath);
                    SetCache(MemoryCacheConst.MemoryVersionWebApi, version);
                    return version;
                }
                return result;
            }
            catch
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}
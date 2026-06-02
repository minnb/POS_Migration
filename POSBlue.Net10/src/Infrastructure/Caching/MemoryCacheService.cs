using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Infrastructure.Persistence;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Infrastructure.Caching;

/// <summary>
/// Triển khai IMemoryCacheService — port từ MemoryCacheService cũ.
/// Thay System.Runtime.Caching.ObjectCache bằng IMemoryCache, DapperContext bằng ISqlConnectionFactory,
/// ConfigurationManager bằng IConfiguration, KibanaService bằng IRequestLogger, FileHelper bằng Serilog.
/// Lưu cache hết hạn lúc nửa đêm (giữ nguyên hành vi GetPolicyMemoryCache cũ).
/// </summary>
public sealed class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IRequestLogger _logger;
    private readonly string _connStrCentralMD;
    private readonly string _connStrLoyalty;
    private readonly string _environment;

    public MemoryCacheService(IMemoryCache cache, ISqlConnectionFactory connectionFactory,
        IConfiguration configuration, IRequestLogger logger)
    {
        _cache = cache;
        _connectionFactory = connectionFactory;
        _logger = logger;
        _connStrCentralMD = configuration.GetConnectionString("DAPPER_CENTRAL_MD") ?? "";
        _connStrLoyalty = configuration.GetConnectionString("DAPPER_LOYALTY") ?? "";
        _environment = configuration["AppSettings:Environment"] ?? "DEV";
    }

    private MemoryCacheEntryOptions Policy() =>
        new() { AbsoluteExpiration = DateTimeHelper.GetMidnightOffset() };

    private IDbConnection CentralMd() => _connectionFactory.CreateConnection(ConnectionNames.CentralMD);
    private IDbConnection Raw(string connStr) => _connectionFactory.CreateConnectionFromRaw(connStr);

    public string GetEnvironment() => _environment;
    public string GetStringDbCentralMD() => _connStrCentralMD;
    public string GetStringDbLoyalty() => _connStrLoyalty;
    public string GetConnectStringCentralMD() => _connStrCentralMD;

    public void SetCache(string key, object data) => _cache.Set(key, data, Policy());
    public T? GetCache<T>(string key) where T : class => _cache.TryGetValue(key, out var v) ? v as T : null;

    public T? Get<T>(string key) =>
        _cache.TryGetValue(key, out string? s) && !string.IsNullOrEmpty(s) ? ConvertHelper.ToObject<T>(s) : default;

    public void Set<T>(string key, T data) => _cache.Set(key, JsonConvert.SerializeObject(data), Policy());

    public List<string> DeleteMemoryCache(string keyMemory)
    {
        // IMemoryCache không hỗ trợ liệt kê key như ObjectCache cũ.
        // Consumer duy nhất (SettingController) đã bị loại bỏ; chỉ hỗ trợ xoá theo key cụ thể.
        var result = new List<string>();
        if (!string.IsNullOrEmpty(keyMemory))
        {
            _cache.Remove(keyMemory);
            result.Add(keyMemory);
        }
        return result;
    }

    public LoyaltyRateDto? GetLoyaltyRateData(string code)
    {
        try
        {
            var list = GetCache<List<LoyaltyRateDto>>(MemoryCacheConst.Redis_Key_LoyaltyRate);
            if (list == null)
            {
                const string sql = "SELECT FromDate, ToDate, Code, Rate, IIF([Enable] = 1, 0, 1) AS Blocked, Pkey, CardType FROM LoyaltyRate (NOLOCK) WHERE [Enable] = 1;";
                using var db = CentralMd();
                list = db.Query<LoyaltyRateDto>(sql).ToList();
                if (list.Count == 0) return null;
                SetCache(MemoryCacheConst.Redis_Key_LoyaltyRate, list);
            }
            return list.FirstOrDefault(x => x.Code == code);
        }
        catch (Exception ex) { Log.Error(ex, "MemoryCacheService.GetLoyaltyRateData"); return null; }
    }

    public List<CardLevelDto>? GetCardLevelMapping(string connectStringDb)
    {
        try
        {
            var data = GetCache<List<CardLevelDto>>(MemoryCacheConst.MemoryCacheStoreSetup + ":CardLevel");
            if (data != null) return data;
            using var db = Raw(connectStringDb);
            data = db.Query<CardLevelDto>("SELECT * FROM CardLevel NOLOCK WHERE Blocked = 0;").ToList();
            if (data.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryCacheStoreSetup + ":CardLevel", data);
            return data;
        }
        catch (Exception ex) { Log.Information("MemoryCache GetCardLevelMapping Exception: {Message}", ex.Message); return null; }
    }

    public List<StoreDto>? GetStores()
    {
        try
        {
            var result = GetCache<List<StoreDto>>(MemoryCacheConst.MemoryCacheStores);
            if (result != null) return result;
            const string sql = @"SELECT DISTINCT S.[No], S.[Name], S.[Address], S.[BusinessAreaNo], S.[PostCode], S.[TaxCode], S.[ResponsibilityCenter], C.ConnectionString
                                 FROM Store (NOLOCK) S
                                 INNER JOIN CentralGeneral.dbo.StoreSetServer (NOLOCK) T ON S.[No] = T.StoreNo AND T.[Status] = 1
                                 INNER JOIN SysWebApiConfig (NOLOCK) C ON C.Prefix = T.ServerIP AND C.Code = 'KAFKA'
                                 WHERE S.ClosingMethod = 0;";
            using var db = CentralMd();
            var dataStore = db.Query<StoreDto>(sql).ToList();
            if (dataStore.Count == 0) return null;
            foreach (var item in dataStore) item.StoreNo = item.No;
            SetCache(MemoryCacheConst.MemoryCacheStores, dataStore);
            return dataStore;
        }
        catch (Exception ex) { Log.Information("MemoryCacheStores.Exception: {Message}", ex.Message); return null; }
    }

    public List<StoreSetup>? GetStoreSetup()
    {
        try
        {
            var result = GetCache<List<StoreSetup>>(MemoryCacheConst.MemoryCacheStoreSetup);
            if (result != null) return result;
            const string sql = "SELECT ISNULL(StoreNo, '') StoreNo, ISNULL(Code, '') Code, ISNULL([Value], '') [Value], ISNULL([Status], 0) [Status] FROM StoreSetup (NOLOCK);";
            using var db = CentralMd();
            var data = db.Query<StoreSetup>(sql).ToList();
            if (data.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryCacheStoreSetup, data);
            return data;
        }
        catch (Exception ex) { Log.Information("MemoryCacheStoreSetup.Exception: {Message}", ex.Message); return null; }
    }

    public List<SysWebApiUserDto>? GetSysWebApiUser()
    {
        try
        {
            var result = GetCache<List<SysWebApiUserDto>>(MemoryCacheConst.MemoryCacheSysWebUserApi);
            if (result != null) return result;
            using var db = CentralMd();
            var data = db.Query<SysWebApiUserDto>("SELECT * FROM SysWebApiUser WITH (NOLOCK) WHERE Blocked = 0;").ToList();
            if (data.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryCacheSysWebUserApi, data);
            return data;
        }
        catch (Exception ex) { Log.Information("GetSysWebApiUser Exception: {Message}", ex.Message); return null; }
    }

    public List<SysWebApiDto>? GetSysWebApi(string connectStringDb)
    {
        try
        {
            var result = GetCache<List<SysWebApiDto>>(MemoryCacheConst.MemoryCacheSysWebApi);
            if (result != null) return result;

            using var db = Raw(connectStringDb);
            var sysWebApi = db.Query<SysWebApi>("SELECT * FROM SysWebApi NOLOCK WHERE Blocked = 0;").ToList();
            var sysWebApiRoute = db.Query<SysWebApiRoute>("SELECT * FROM SysWebApiRoute NOLOCK WHERE Blocked = 0;").ToList();
            if (sysWebApi.Count == 0) return null;

            var routes = sysWebApi.Select(item => new SysWebApiDto
            {
                AppCode = item.AppCode, Host = item.Host, Authorization = item.Authorization,
                UserName = item.UserName, Password = item.Password, PrivateKey = item.PrivateKey,
                PublicKey = item.PublicKey, Version = item.Version, Blocked = item.Blocked,
                Description = item.Description, HttpProxy = item.HttpProxy, Bypasslist = item.Bypasslist,
                SysWebApiRoute = sysWebApiRoute.Where(x => x.AppCode == item.AppCode).ToList(),
            }).ToList();

            if (_environment == "DEV") routes.ForEach(x => x.HttpProxy = null);
            SetCache(MemoryCacheConst.MemoryCacheSysWebApi, routes);
            return routes;
        }
        catch (Exception ex)
        {
            _logger.LogException("GetSysWebApi", "999999", 0, "", ex.Message);
            return null;
        }
    }

    public List<WinCodeStoreMemoryDto>? GetWinCodeMemory(string connectStringDb)
    {
        try
        {
            var result = GetCache<List<WinCodeStoreMemoryDto>>(MemoryCacheConst.MemoryCacheWinCode);
            if (result != null) return result;

            using var db = Raw(connectStringDb);
            var winCodeHeader = db.Query<WinCodeHeaderDto>("SELECT * FROM WinCodeHeader NOLOCK WHERE Status = 1 AND CAST(FromDate AS DATE) <= CAST(getdate() AS DATE) AND CAST(ToDate AS DATE) >= CAST(getdate() AS DATE);").ToList();
            var programCode = winCodeHeader.Select(x => x.ProgramCode).ToArray();
            var wincodeStore = db.Query<WinCodeStoreDto>("SELECT * FROM WinCodeStore NOLOCK WHERE Status = 1 AND ProgramCode IN @programCode;", new { ProgramCode = programCode }).ToList();

            if (winCodeHeader.Count == 0 || wincodeStore.Count == 0) return null;

            var result2 = new List<WinCodeStoreMemoryDto>();
            foreach (var item in winCodeHeader)
                foreach (var item2 in wincodeStore.Where(x => x.ProgramCode == item.ProgramCode))
                    result2.Add(new WinCodeStoreMemoryDto
                    {
                        ProgramCode = item2.ProgramCode, Status = item2.Status, StoreNo = item2.StoreNo,
                        WinCode = item.WinCode, Quantity = item.Quantity, DiscountType = item.DiscountType, ApplyType = item.ApplyType,
                    });
            SetCache(MemoryCacheConst.MemoryCacheWinCode, result2);
            return result2;
        }
        catch (Exception ex) { _logger.LogException("GetWinCodeMemory", "GetWinCodeMemory", 0, "", ex.Message); return null; }
    }

    public List<StagingDBConfigDto>? GetStagingDBConfig(string strConnectDB)
    {
        try
        {
            var result = GetCache<List<StagingDBConfigDto>>(MemoryCacheConst.MemoryCacheStagingDBConfig);
            if (result != null) return result;
            using var db = Raw(strConnectDB);
            var data = db.Query<StagingDBConfigDto>("SELECT * FROM StagingDBConfig NOLOCK WHERE Blocked = 0;").ToList();
            if (data.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryCacheStagingDBConfig, data);
            return data;
        }
        catch (Exception ex) { Log.Information("GetStagingDBConfig.Exception: {Message}", ex.Message); return null; }
    }

    public List<MemoryCacheConfig>? GetCacheConfigLoyalty(string strConnectDB)
    {
        try
        {
            var result = GetCache<List<MemoryCacheConfig>>(MemoryCacheConst.MemoryCacheConfigLoyalty);
            if (result != null) return result;
            using var db = Raw(strConnectDB);
            var data = db.Query<MemoryCacheConfig>("SELECT * FROM MemoryCacheConfig (NOLOCK);").ToList();
            if (data.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryCacheConfigLoyalty, data);
            return data;
        }
        catch (Exception ex) { Log.Information("GetCacheConfigLoyalty.Exception: {Message}", ex.Message); return null; }
    }

    public List<NotifyConfigDto>? GetNotifyConfig(string connectStringDb = "")
    {
        try
        {
            var memoryData = GetCache<List<NotifyConfigDto>>(MemoryCacheConst.MemoryGetNotifyConfig);
            if (memoryData != null) return memoryData;
            using var db = CentralMd();
            var result = db.Query<NotifyConfigDto>("SELECT * FROM NotifyConfig (NOLOCK) WHERE Blocked = 0;").ToList();
            SetCache(MemoryCacheConst.MemoryGetNotifyConfig, result);
            return result;
        }
        catch (Exception ex) { Log.Error(ex, "GetNotifyConfig"); return null; }
    }

    public PosDataSetupDto? GetPOSDataSetup(string key)
    {
        try
        {
            var result = GetCache<List<PosDataSetupDto>>(MemoryCacheConst.MemoryGetPOSDataSetup);
            if (result == null)
            {
                using var db = CentralMd();
                result = db.Query<PosDataSetupDto>("SELECT * FROM POSDataSetup (NOLOCK)").ToList();
                SetCache(MemoryCacheConst.MemoryGetPOSDataSetup, result);
            }
            return result.FirstOrDefault(x => string.Equals(x.Code, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) { Log.Error(ex, "GetPOSDataSetup"); return null; }
    }

    public List<OfferStaffSetupDto>? GetOfferStaffSetup()
    {
        try
        {
            var data = GetCache<List<OfferStaffSetupDto>>(MemoryCacheConst.MemoryOfferStaffSetup);
            if (data != null) return data;
            const string sql = @"SELECT * FROM OfferStaffSetup (NOLOCK) WHERE CAST(ApplyFrom AS DATE) >= CAST(getdate() AS DATE) AND CAST(ValidBefore AS DATE) <= CAST(getdate() AS DATE) AND Blocked = 0;";
            using var db = CentralMd();
            var data2 = db.Query<OfferStaffSetupDto>(sql).ToList();
            if (data2.Count == 0) return null;
            SetCache(MemoryCacheConst.MemoryOfferStaffSetup, data2);
            return data2;
        }
        catch (Exception ex) { Log.Information("MemoryOfferStaffSetup.Exception: {Message}", ex.Message); return null; }
    }

    public List<StoreSetConfig> GetStoreSetConfig()
    {
        var dataCache = GetCache<List<StoreSetConfig>>(MemoryCacheConst.MemoryStoreSetConfig);
        if (dataCache != null) return dataCache;
        const string query = @"SELECT DISTINCT A.StoreNo, B.*
                              FROM CentralGeneral.dbo.[StoreSetServer] (NOLOCK) A
                              INNER JOIN SysWebApiConfig (NOLOCK) B ON B.[Prefix] = A.[ServerIP]
                              WHERE B.Blocked = 0 AND A.[Status] = 1;";
        try
        {
            using var db = CentralMd();
            var result2 = db.Query<StoreSetConfig>(query).ToList();
            SetCache(MemoryCacheConst.MemoryStoreSetConfig, result2);
            return result2;
        }
        catch (Exception ex) { Log.Information("GetStoreSetConfig.Exception: {Message}", ex.Message); return new List<StoreSetConfig>(); }
    }

    public string GetVersion(string file)
    {
        try
        {
            var result = GetCache<string>(MemoryCacheConst.MemoryVersionWebApi);
            if (result != null) return result;
            var version = VersionHelper.GetAssemblyModifiedDate();
            SetCache(MemoryCacheConst.MemoryVersionWebApi, version);
            return version;
        }
        catch { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }
    }
}

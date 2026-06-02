using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho cache cấu hình master-data (port từ MemoryCacheService cũ).
/// Mỗi method: đọc cache → nếu miss thì query DB (Dapper) → set cache (hết hạn lúc nửa đêm).
/// </summary>
public interface IMemoryCacheService
{
    string GetEnvironment();
    string GetStringDbCentralMD();
    string GetStringDbLoyalty();
    string GetConnectStringCentralMD();

    void SetCache(string key, object data);
    T? GetCache<T>(string key) where T : class;
    T? Get<T>(string key);
    void Set<T>(string key, T data);
    List<string> DeleteMemoryCache(string keyMemory);

    LoyaltyRateDto? GetLoyaltyRateData(string code);
    List<CardLevelDto>? GetCardLevelMapping(string connectStringDb);
    List<StoreDto>? GetStores();
    List<StoreSetup>? GetStoreSetup();
    List<SysWebApiUserDto>? GetSysWebApiUser();
    List<SysWebApiDto>? GetSysWebApi(string connectStringDb);
    List<WinCodeStoreMemoryDto>? GetWinCodeMemory(string connectStringDb);
    List<StagingDBConfigDto>? GetStagingDBConfig(string strConnectDB);
    List<MemoryCacheConfig>? GetCacheConfigLoyalty(string strConnectDB);
    List<NotifyConfigDto>? GetNotifyConfig(string connectStringDb = "");
    PosDataSetupDto? GetPOSDataSetup(string key);
    List<OfferStaffSetupDto>? GetOfferStaffSetup();
    List<StoreSetConfig> GetStoreSetConfig();
    string GetVersion(string file);
}

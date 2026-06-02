using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Repository cho DB Loyalty (port từ LoyaltyRepository cũ, dùng Dapper).
/// Phase này migrate luồng store-mapping + logging-loyalty + gift-code + memory-cache-config.
/// Các method HVDN/WinPay (InsertWinPayAccumulate, MemberRemnItem...) sẽ bổ sung khi migrate WinCustomerService
/// (cần RedisConnectionHelper parent-key).
/// </summary>
public interface ILoyaltyRepository
{
    string ConnectStringLoyaltyDb();
    List<StoreMappingModel> GetLoyaltyStoreMapping();

    bool UpdateStatusLoggingLoyalty(LoggingLoyaltyDto dto);
    List<LoggingLoyaltyDto>? GetLoggingLoyalty(string actionType, string status);
    Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyalty(string orderNo, string actionType);
    Task<LoggingLoyaltyDto?> InsertLoggingLoyalty(LoggingLoyaltyDto? dto, string orderNo = "", bool isRetry = false);

    Task<GiftCodeDto?> GetGiftCode(string orderNo, string saleType, string memberCard, int amount);

    bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess);
    Task<bool> UpdateMemoryCacheConfig(string code, bool isBlocked);
    MemoryCacheConfig? GetMemoryCacheConfig(string code);
}

using System.Data;
using POS.Common.Dtos;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.Loyalty.MemberBusiness;
using POS.Common.Dtos.WinCustomer;
using POS.Common.Dtos.WinMoney;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ILoyaltyRepository
{
    Task<List<StoreMappingModel>?> GetLoyaltyStoreMappingAsync(CancellationToken ct = default);
    string ConnectStringLoyaltyDb();

    Task<bool> InsertWinPayAccumulateAsync(IDbConnection db, WinPayAccumulationData winPayAccumulationData, bool isRetry = false);

    bool InsertMemberRemnItem(List<MemberRemnItem> memberRemnItems, string parentKeyMemberRemnItem, ref string errMess);

    Task<Tuple<bool, string>> RefundMemberRemnItemAsync(string orderNo, string memberCard, CancellationToken ct = default);

    bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess);

    Task<bool> UpdateStatusLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, CancellationToken ct = default);

    Task<List<LoggingLoyaltyDto>?> GetLoggingLoyaltyAsync(string actionType, string status, CancellationToken ct = default);

    Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyaltyAsync(string orderNo, string actionType, CancellationToken ct = default);

    Task<LoggingLoyaltyDto?> InsertLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, string orderNo = "", bool isRetry = false, CancellationToken ct = default);

    Task<GiftCodeDto?> GetGiftCodeAsync(string orderNo, string saleType, string memberCard, int amount, CancellationToken ct = default);

    Task<bool> UpdateMemoryCacheConfigAsync(string code, bool isBlocked, CancellationToken ct = default);

    Task<MemoryCacheConfig?> GetMemoryCacheConfigAsync(string code, CancellationToken ct = default);
}

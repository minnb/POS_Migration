using System.Data;
using POS.Common.Dtos;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.Loyalty.MemberBusiness;
using POS.Common.Dtos.WinCustomer;
using POS.Common.Dtos.WinMoney;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ILoyaltyRepository
{
    string ConnectStringLoyaltyDb();

    Task<bool> UpdateStatusLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, CancellationToken ct = default);

    Task<List<LoggingLoyaltyDto>?> GetLoggingLoyaltyAsync(string actionType, string status, CancellationToken ct = default);

    Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyaltyAsync(string orderNo, string actionType, CancellationToken ct = default);

    Task<LoggingLoyaltyDto?> InsertLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, string orderNo = "", bool isRetry = false, CancellationToken ct = default);

}

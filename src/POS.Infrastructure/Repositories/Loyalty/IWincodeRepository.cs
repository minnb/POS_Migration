using POS.Common.Dtos.Loyalty.WinCode;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface IWincodeRepository
{
    Task<List<WinCodeCustomerDto>?> GetWinCodeCustomerAsync(string phoneNumber, CancellationToken ct = default);
    Task<Tuple<bool, string>> UpdateWincodeCustomerAsync(WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default);
    Task<Tuple<bool, string>> InsertWincodeCustomerAsync(WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default);
}

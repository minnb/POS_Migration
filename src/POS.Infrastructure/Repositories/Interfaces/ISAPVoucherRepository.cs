using POS.Common.Dtos.Vouchers;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ISAPVoucherRepository
{
    Task<VoucherStatusResponse?> GetByVoucherNumberAsync(string voucherNumber, CancellationToken ct = default);
    Task<bool> InsertAsync(VoucherStatusResponse data, CancellationToken ct = default);
    Task<(bool Success, string Message, List<VoucherStatusResponse> Results)> RedeemVouchersAsync(
        List<string> voucherNumbers, CancellationToken ct = default);
}

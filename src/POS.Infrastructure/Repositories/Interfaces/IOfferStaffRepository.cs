using POS.Common.Dtos.MSN;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface IOfferStaffRepository
{
    Task<(bool Success, string Message)> InsertOfferStaffTransactionAsync(OfferStaffTransactionDto request, CancellationToken ct = default);
    Task<OfferStaffRemnDto?> GetOfferStaffRemnAsync(string staffCode, string phoneNumber, string clubCode, CancellationToken ct = default);
    Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync(CancellationToken ct = default);
}

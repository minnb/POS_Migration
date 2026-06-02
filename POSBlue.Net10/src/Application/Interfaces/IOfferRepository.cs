using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

public interface IOfferRepository
{
    Task<(bool Success, string Message)> InsertOfferStaffTransactionAsync(OfferStaffTransactionDto request);
    Task<OfferStaffRemnDto?> GetOfferStaffRemnAsync(string staffCode, string phoneNumber, string clubCode);
    Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync();
}

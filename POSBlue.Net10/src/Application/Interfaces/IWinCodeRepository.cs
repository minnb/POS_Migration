using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Repository cho bảng WinCodeCustomer trên CentralMD DB.
/// Port từ WincodeRepository cũ.
/// </summary>
public interface IWinCodeRepository
{
    Task<List<WinCodeCustomerRecord>> GetWinCodeCustomerAsync(string phoneNumber);
    Task<(bool Success, string Message)> InsertWinCodeCustomerAsync(
        WinLifeUpdatePromotionsRequest request);
    Task<(bool Success, string Message)> UpdateWinCodeCustomerAsync(
        WinLifeUpdatePromotionsRequest request);
}

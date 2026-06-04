using POS.Application.Loyalty.DTOs;
using POS.Shared.Models;

namespace POS.Application.Loyalty.Services;

public interface ILoyaltyService
{
    /// <summary>
    /// GET api/v2/loyalty/customer/get
    /// Trả về (statusCode, ResultResponse) để controller map trực tiếp.
    /// </summary>
    Task<(int StatusCode, ResultResponse Body)> GetCustomerAsync(GetCustomerRequest request);
}

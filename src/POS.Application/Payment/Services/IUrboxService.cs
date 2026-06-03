using POS.Application.Payment.DTOs;

namespace POS.Application.Payment.Services;

public interface IUrboxService
{
    Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> CheckSerialAsync(CheckVoucherPartnerRequest request);
}

using POS.Application.Payment.DTOs;

namespace POS.Application.Payment.Services;

public interface IGotITService
{
    Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> CheckMultipleAsync(CheckVoucherPartnerRequest request);
    Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> MarkUseMultipleAsync(UpdateStatusVoucherPartnerRequest request);
}

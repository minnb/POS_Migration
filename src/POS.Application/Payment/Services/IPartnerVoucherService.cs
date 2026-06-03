using POS.Application.Payment.DTOs;
using POS.Shared.Models;

namespace POS.Application.Payment.Services;

public interface IPartnerVoucherService
{
    Task<(int HttpStatus, ResultResponse Body)> CheckVoucherAsync(CheckVoucherPartnerRequest request);
}

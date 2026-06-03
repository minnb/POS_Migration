using POS.Application.Payment.DTOs;
using POS.Shared.Models;

namespace POS.Application.Payment.Services;

public interface IOneUService
{
    Task<ResultResponse> EstimateAsync(CheckVoucherPartnerRequest request);
}

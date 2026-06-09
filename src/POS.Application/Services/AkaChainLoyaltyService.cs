using POS.Application.Interfaces;
using POS.Common;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Interfaces;

namespace POS.Application.Services;

public sealed class AkaChainLoyaltyService(
    IAkaChainLoyaltyAppService appService
) : IAkaChainLoyaltyService
{
    public Task<ResultResponse> GetMemberProfileAsync(string key, string value)
        => appService.GetMemberProfileAsync(key, value);

    public Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model)
        => appService.AddTransactionAsync(model);

    public Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model)
        => appService.ReturnTransactionAsync(model);

    public Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model)
        => appService.CheckCouponAsync(model);
}

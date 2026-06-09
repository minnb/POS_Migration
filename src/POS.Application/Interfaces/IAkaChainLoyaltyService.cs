using POS.Common;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.PartnerApi;

namespace POS.Application.Interfaces;

public interface IAkaChainLoyaltyService
{
    Task<ResultResponse> GetMemberProfileAsync(string key, string value);
    Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model);
    Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model);
    Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model);
}

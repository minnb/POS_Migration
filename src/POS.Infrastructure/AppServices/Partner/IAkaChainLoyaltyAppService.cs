using POS.Common;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.PartnerApi;

namespace POS.Infrastructure.AppServices.Partner;

/// <summary>
/// HTTP client wrapper cho AkaChain/FMV loyalty API.
/// Chỉ chịu trách nhiệm: serialize request → gọi HTTP → deserialize response.
/// Không chứa business rule.
/// </summary>
public interface IAkaChainLoyaltyAppService
{
    /// <summary>Lấy thông tin member. key = "Phone", value = số điện thoại.</summary>
    Task<ResultResponse> GetMemberProfileAsync(string key, string value);

    /// <summary>Ghi nhận giao dịch bán hàng vào loyalty.</summary>
    Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model);

    /// <summary>Ghi nhận trả hàng vào loyalty.</summary>
    Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model);

    /// <summary>Kiểm tra coupon trước khi apply vào hoá đơn.</summary>
    Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model);

    Task<ResultResponse> RegisterMemberAsync(RegisterMemberDto model);
}

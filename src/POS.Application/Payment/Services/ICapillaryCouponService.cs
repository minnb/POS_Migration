using POS.Application.Payment.DTOs;
using POS.Shared.Models;

namespace POS.Application.Payment.Services;

public interface ICapillaryCouponService
{
    Task<(int HttpStatus, ResultResponse Body)> ValidateCouponAsync(CheckVoucherPartnerRequest request);
    Task<(int HttpStatus, ResultResponse Body)> RedeemCouponAsync(UpdateStatusVoucherPartnerRequest request);
    Task<(int HttpStatus, ResultResponse Body)> GetListCouponByUserAsync(string storeNo, string mobile, string status);
    Task<(int HttpStatus, ResultResponse Body)> GetCouponDetailAsync(string storeNo, string serialNo);
    Task<(int HttpStatus, ResultResponse Body)> ReActivateCouponsAsync(ReActiveCouponRequest request);
}

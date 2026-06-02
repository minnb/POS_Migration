using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use cases cho VoucherTopUpVinIDController (api/vinid/*) — VINID TopUp + EVoucher API.
/// Auth: HMAC signature headers (X-Key-Code, X-Timestamp, X-Nonce, X-Signature).
/// Config từ SysWebApi AppCode="VINID_TOPUP": Host, UserName(=X-Key-Code), PrivateKey(=RSA XML), Routes.
/// </summary>
public interface IVinIdTopUpService
{
    /// <summary>GET api/vinid/TopUpCheckMember — kiểm tra thông tin thành viên TopUp.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> TopUpCheckMemberAsync(
        string phoneNumber, string posID, string storeNo);

    /// <summary>POST api/vinid/TopUpPoinToPhone — nạp điểm cho SĐT qua VINID TopUp.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> TopUpPointToPhoneAsync(
        VinIdTopUpPointPosRequest model);

    /// <summary>GET api/vinid/TopUpCheckStatusOrder — kiểm tra trạng thái đơn hàng TopUp.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> TopUpCheckStatusOrderAsync(
        string orderNo, string posID, string storeNo);

    /// <summary>GET api/vinid/EVoucherVerify — xác thực e-voucher.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherVerifyAsync(
        string storeNo, string posID, string serialNumber, string userPOS);

    /// <summary>POST api/vinid/EVoucherRefund — thu hồi e-voucher.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherRefundAsync(
        VinIdEVoucherRefundPosRequest model);

    /// <summary>POST api/vinid/EVoucherMarkUsed — đánh dấu e-voucher đã sử dụng.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherMarkUsedAsync(
        VinIdEVoucherMarkUsedPosRequest model);
}

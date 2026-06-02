using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use cases cho VoucherController (api/vc/*) — CX Voucher OTP và cập nhật trạng thái voucher.
/// </summary>
public interface IVoucherService
{
    /// <summary>GET api/vc/generateOTP — tạo OTP cho voucher qua CX API.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpAsync(
        string storeNo, string posID, string codeValue);

    /// <summary>GET api/vc/getVoucherSerial — lấy serial voucher sau khi nhập OTP.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetVoucherSerialAsync(
        string storeNo, string posID, string codeValue, string otp);

    /// <summary>POST api/vc/UpdateVoucherStatus — cập nhật trạng thái voucher sang USED.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdateVoucherStatusAsync(
        UpdateVoucherStatusRequest request);
}

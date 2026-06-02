using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Tập hợp toàn bộ use case của WinLifeController:
/// OTP (CX + Winpay), đăng ký thành viên, WinCode, SmartPOS.
/// </summary>
public interface IWinLifeService
{
    /// <summary>GET api/v2/otp/generate — tạo OTP qua CX hoặc Winpay tuỳ action.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpV2Async(
        string posNo, string phoneNumber, string action);

    /// <summary>POST api/v2/otp/verify — xác thực OTP qua CX.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VerifyOtpV2Async(
        PosVerifyOtpRequest request);

    /// <summary>GET api/blue/winlife/generateOTP — tạo OTP qua CX WinLife (route cũ).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpAsync(
        string storeNo, string posID, string codeValue);

    /// <summary>POST api/blue/winlife/register — đăng ký thành viên (WINCARE → Capillary, còn lại → CX WinLife).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> RegisterAsync(
        WinLifeRegisterRequest request);

    /// <summary>POST api/blue/winlife/update-promotions — ghi nhận/xoá WinCode cho đơn hàng.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdatePromotionsAsync(
        WinLifeUpdatePromotionsRequest request);

    /// <summary>GET api/blue/winlife/winCode-histories — lấy lịch sử WinCode qua CX.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> WinCodeHistoriesAsync(
        string csn, string winCode);

    /// <summary>GET api/blue/winlife/smart-pos/customer-by-last-digits-phone — tìm thành viên Capillary theo SĐT.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CustomerByLastDigitsPhoneAsync(
        string storeNo, string posID, string codeValue);

    /// <summary>POST api/blue/winlife/smart-pos/update-customer-info — cập nhật thông tin thành viên Capillary.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdateCustomerInfoAsync(
        WinLifeUpdateCustomerRequest request);
}

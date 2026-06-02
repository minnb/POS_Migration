namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>Kết quả thao tác OTP của CrownX (thay Tuple&lt;bool,string,object&gt; cũ).</summary>
public sealed record CxOtpResult(bool Success, string Message, object? Data);

/// <summary>
/// Service tích hợp CrownX (OTP). Port từ CXService cũ — dùng IApiCallClient + IMemoryCacheService.
/// </summary>
public interface ICXService
{
    Task<CxOtpResult> GenerateOTPAsync(string posNo, string phoneNumber, string action);
    Task<CxOtpResult> VerifyOTPAsync(string posNo, string phoneNumber, string otp, string action);
    bool IsCheckNoneOtp(string phoneNumber);
}

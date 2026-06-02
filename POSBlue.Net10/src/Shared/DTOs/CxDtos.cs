namespace VCM.POSBLUE.Shared.DTOs;

// DTO cho CrownX OTP (giữ nguyên field từ bản cũ).

public class GenerateOTPDto
{
    public string? PhoneNumber { get; set; }
    public string? MerchantId { get; set; }
    public string? Action { get; set; }
}

public class VerifyOTPDto
{
    public string? PhoneNumber { get; set; }
    public string? Otp { get; set; }
    public string? Action { get; set; }
}

public class VerifyOTPData
{
    public string? PhoneNumber { get; set; }
    public string? Otp { get; set; }
    public bool IsValid { get; set; }
}

public class CXResponse
{
    public string? Message { get; set; }
    public string? DeveloperMessage { get; set; }
    public object? Data { get; set; }
}

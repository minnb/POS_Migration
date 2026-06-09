using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Loyalty.CX;

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

public class GenerateOTPData
{
    public string? MessageId { get; set; }
    public string? Status { get; set; }
}

public class VerifyOTPData
{
    public string? PhoneNumber { get; set; }
    public string? Otp { get; set; }
    public bool IsValid { get; set; }
}

public class POSVerifyOTPRequest
{
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string Otp { get; set; } = string.Empty;
    public string? Action { get; set; }
}

public class BadRequestCX
{
    public string? Message { get; set; }
}

public class CXResponse
{
    public string? Message { get; set; }
    public string? DeveloperMessage { get; set; }
    public object? Data { get; set; }
}

public class CXResponseData
{
    public string? MessageId { get; set; }
    public string? Status { get; set; }
}

public class OtpResponse
{
    public string? Message { get; set; }
    public string? DeveloperMessage { get; set; }
    public CXResponseData? Data { get; set; }
}

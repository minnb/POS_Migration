namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>Message gửi cảnh báo/thông báo (giữ nguyên từ SMSMessage cũ).</summary>
public class SmsMessage
{
    public string? AppCode { get; set; }
    public string? MessageType { get; set; } // Error, Info, Warning
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Text { get; set; }
}

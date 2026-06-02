namespace VCM.POSBLUE.Api.Models.Queue;

/// <summary>Request gửi SMS/thông báo (giữ nguyên field từ SMSMessage cũ dùng trong api/v2/sms/send).</summary>
public class SmsSendRequest
{
    public string? AppCode { get; set; }
    public string? MessageType { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Text { get; set; }
}

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Gửi cảnh báo/thông báo (SMS). Thay thế KibanaService.SendMessageSMS → HttpClientService.SendMessageSMS cũ
/// (bản chất là publish SMSMessage vào queue_sms qua RabbitMQ).
/// </summary>
public interface ISmsService
{
    Task SendMessageSmsAsync(string content, string messageType = "Info", string subject = "",
        string messageId = "", string text = "", string appCode = "BLUEPOS");
}

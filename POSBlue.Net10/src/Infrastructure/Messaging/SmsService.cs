using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Infrastructure.Messaging;

/// <summary>
/// Triển khai ISmsService — port từ HttpClientService.SendMessageSMS cũ:
/// build SmsMessage và publish vào queue_sms (priority 10) qua RabbitMQ.
/// </summary>
public sealed class SmsService : ISmsService
{
    private static readonly string[] AllowedTypes = { "Error", "Info", "Warning" };
    private readonly IMessageQueueService _queue;

    public SmsService(IMessageQueueService queue) => _queue = queue;

    public async Task SendMessageSmsAsync(string content, string messageType = "Info", string subject = "",
        string messageId = "", string text = "", string appCode = "BLUEPOS")
    {
        if (!AllowedTypes.Contains(messageType)) messageType = "Info";

        var ip = NetworkHelper.GetIpServer();
        if (string.IsNullOrEmpty(subject)) subject = $"No subject on server {ip}";
        if (string.IsNullOrEmpty(messageId)) messageId = $"{ip}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var mess = new SmsMessage
        {
            AppCode = appCode,
            MessageType = messageType,
            Subject = subject,
            Content = $"🕒 Time: **{DateTime.Now:dd-MM-yyyy HH:mm:ss} on srv {ip}** ℹ️ {content}",
            Text = text,
        };

        await _queue.SendMessageAsync(RabbitMqConstants.Queue_SMS, ip, JsonConvert.SerializeObject(mess), true, 10);
    }
}

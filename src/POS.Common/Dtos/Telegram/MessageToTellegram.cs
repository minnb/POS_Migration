using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Telegram;

public class MessageTelegramRequest
{
    [Required]
    public string MessageType { get; set; } = string.Empty;
    [Required]
    public string Message { get; set; } = string.Empty;
}

public class MessageToTellegram
{
    public string telegram_id { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
}

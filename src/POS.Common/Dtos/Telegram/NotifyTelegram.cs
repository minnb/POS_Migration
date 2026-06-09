namespace POS.Common.Dtos.Telegram;

public class NotifyTelegram
{
    public string GroupId { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string WebApi { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string Notify { get; set; } = string.Empty;
    public bool Blocked { get; set; }
}

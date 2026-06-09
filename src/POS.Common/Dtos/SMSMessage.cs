namespace POS.Common.Dtos;

public class SMSMessage
{
    public string? AppCode { get; set; }
    public string? MessageType { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Text { get; set; }
}

public class SMSMessageV1
{
    public string? ChanelId { get; set; }
    public string? MessageType { get; set; }
    public string? Subject { get; set; }
    public string? Conntent { get; set; }
}

public class SMSMessageV2
{
    public string? AppCode { get; set; }
    public string MessageType { get; set; } = "Info";
    public string? Subject { get; set; }
    public string? Conntent { get; set; }
}

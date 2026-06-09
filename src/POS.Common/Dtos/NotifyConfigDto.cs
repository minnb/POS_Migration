namespace POS.Common.Dtos;

public class NotifyConfigDto
{
    public string? AppCode { get; set; }
    public string? ActionType { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? MessageFormat { get; set; }
    public bool Blocked { get; set; }
    public bool IsOffline { get; set; }
}

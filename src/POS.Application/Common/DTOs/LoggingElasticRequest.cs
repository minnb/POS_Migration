namespace POS.Application.Common.DTOs;

public class LoggingElasticRequest
{
    public string? PosNo { get; set; }
    public string? StoreNo { get; set; }
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? Data { get; set; }
}

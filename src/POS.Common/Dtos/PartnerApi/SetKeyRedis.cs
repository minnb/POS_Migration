namespace POS.Common.Dtos.PartnerApi;

public class SetKeyRedis
{
    public string? Key { get; set; }
    public string? HashField { get; set; }
    public object? Data { get; set; }
}

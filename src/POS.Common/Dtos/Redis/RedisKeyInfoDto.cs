namespace POS.Common.Dtos.Redis;

public class RedisKeyInfoDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long? TtlSeconds { get; set; }
}

namespace POS.Common.Dtos.Redis;

public class RedisKeyValueDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long? TtlSeconds { get; set; }
    public string Value { get; set; } = string.Empty;
}

namespace POS.Common.Dtos.Redis;

public class RedisKeySearchResultDto
{
    public List<RedisKeyInfoDto> Keys { get; set; } = [];
    public bool IsTruncated { get; set; }
}

using POS.Common.Dtos.Redis;

namespace POS.Application.Features.Redis;

public interface IRedisManagementService
{
    Task<RedisKeySearchResultDto> SearchKeysAsync(string pattern, CancellationToken ct = default);
    Task<RedisKeyValueDto?> GetKeyValueAsync(string key, CancellationToken ct = default);
    Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default);
    Task<RedisServerStatusDto> GetServerStatusAsync(CancellationToken ct = default);
}

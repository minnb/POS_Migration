using POS.Infrastructure.Cache;

namespace POS.Infrastructure.Locking;

/// <summary>
/// Redis distributed lock — key cố định toàn cục (không theo ItemNo) để serialize MỌI request
/// phát hành Auto voucher/coupon trên toàn hệ thống, kể cả khi POS.Web scale-out nhiều instance.
/// </summary>
public sealed class VoucherIssueLock(IRedisManager redis) : IVoucherIssueLock
{
    private const string Key = "Lock:VoucherIssue";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(15);

    public async Task<IAsyncDisposable?> AcquireAsync(CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + MaxWait;
        while (DateTime.UtcNow < deadline)
        {
            var token = await redis.AcquireLockAsync(Key, Ttl);
            if (token != null)
                return new Releaser(redis, token);

            await Task.Delay(PollDelay, ct);
        }

        return null;
    }

    private sealed class Releaser(IRedisManager redis, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await redis.ReleaseLockAsync(Key, token);
    }
}

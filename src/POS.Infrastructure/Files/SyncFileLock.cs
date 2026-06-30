using System.Collections.Concurrent;

namespace POS.Infrastructure.Files;

/// <summary>
/// Keyed SemaphoreSlim — đăng ký Singleton để dictionary tồn tại toàn app.
/// Key nên là {TypeSync}_{SiteCode}_{PosTerminal} (KHÔNG kèm ngày) để bị chặn bounded theo số terminal,
/// tránh dictionary phình vô hạn theo ngày.
/// </summary>
public sealed class SyncFileLock : ISyncFileLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken ct = default)
    {
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        return new Releaser(gate);
    }

    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                gate.Release();
        }
    }
}

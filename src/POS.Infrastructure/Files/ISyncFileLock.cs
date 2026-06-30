namespace POS.Infrastructure.Files;

/// <summary>
/// Khóa theo key (keyed SemaphoreSlim) chống sinh trùng file master data trong cùng 1 process.
/// </summary>
public interface ISyncFileLock
{
    /// <summary>
    /// Lấy khóa cho <paramref name="key"/>. Dispose giá trị trả về để nhả khóa (dùng với <c>using</c>).
    /// </summary>
    Task<IDisposable> AcquireAsync(string key, CancellationToken ct = default);
}

using Microsoft.Data.SqlClient;
using POS.Infrastructure.Logging;

namespace POS.Web.Services;

/// <summary>
/// Giữ SqlConnection + SqlTransaction mở sau khi chạy UPDATE.
/// Admin gọi CommitAsync() hoặc RollbackAsync(). Auto-rollback sau 60s.
/// </summary>
public sealed class PendingUpdate : IAsyncDisposable
{
    private readonly SqlConnection _conn;
    private readonly SqlTransaction _tx;
    private readonly IKibanaService _kibana;
    private readonly IFileLogHelper _fileLog;
    private readonly string _actor;
    private readonly string _connKey;
    private readonly string _sqlPreview;
    private readonly CancellationTokenSource _autoRollbackCts;
    private bool _disposed;
    private bool _finalised;

    public int RowsAffected { get; }
    public bool HasWhere { get; }
    public long ElapsedMs { get; }

    internal PendingUpdate(
        SqlConnection conn, SqlTransaction tx,
        int rowsAffected, bool hasWhere, long elapsedMs,
        string actor, string connKey, string sql,
        IKibanaService kibana, IFileLogHelper fileLog)
    {
        _conn = conn;
        _tx = tx;
        RowsAffected = rowsAffected;
        HasWhere = hasWhere;
        ElapsedMs = elapsedMs;
        _actor = actor;
        _connKey = connKey;
        _sqlPreview = sql.Length <= 500 ? sql : sql[..500] + "...";
        _kibana = kibana;
        _fileLog = fileLog;

        _autoRollbackCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        _ = ScheduleAutoRollbackAsync(_autoRollbackCts.Token);
    }

    private async Task ScheduleAutoRollbackAsync(CancellationToken ct)
    {
        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException)
        {
            if (!_finalised)
            {
                _kibana.LogInfo("SqlConsole.Update.AutoRollback", _actor,
                    $"{_connKey} | timeout 60s | {_sqlPreview}");
                await DisposeAsync();
            }
        }
    }

    public async Task CommitAsync()
    {
        if (_finalised) return;
        _finalised = true;
        await _tx.CommitAsync();
        _kibana.LogInfo("SqlConsole.Update.Commit", _actor,
            $"{_connKey} | rows={RowsAffected} | {_sqlPreview}");
        _fileLog.WriteLogs(
            $"[SqlConsole][COMMIT] actor={_actor} db={_connKey} rows={RowsAffected} sql={_sqlPreview}");
        await CleanupAsync();
    }

    public async Task RollbackAsync()
    {
        if (_finalised) return;
        _finalised = true;
        try { await _tx.RollbackAsync(); } catch { /* connection already gone */ }
        _kibana.LogInfo("SqlConsole.Update.Rollback", _actor,
            $"{_connKey} | rows={RowsAffected} | {_sqlPreview}");
        await CleanupAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (!_finalised)
        {
            _finalised = true;
            try { await _tx.RollbackAsync(); } catch { /* best-effort */ }
        }
        await CleanupAsync();
    }

    private async ValueTask CleanupAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _autoRollbackCts.Cancel();
        _autoRollbackCts.Dispose();
        _tx.Dispose();
        await _conn.DisposeAsync();
    }
}

using System.Data;
using Dapper;

namespace POS.Infrastructure.Database;

/// <summary>
/// Base repository cung cấp các helper method dùng chung cho Dapper.
/// Dùng using pattern để đảm bảo connection luôn được Dispose.
/// </summary>
public abstract class BaseRepository(IDbConnectionFactory connectionFactory)
{
    protected readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        int commandTimeout = 30,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<T>(
            new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: ct));
    }

    protected async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        int commandTimeout = 30,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: ct));
    }

    protected async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        int commandTimeout = 30,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteAsync(
            new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: ct));
    }

    /// <summary>
    /// Execute trong transaction — dùng cho các thao tác ghi cần rollback khi lỗi.
    /// </summary>
    protected async Task ExecuteInTransactionAsync(
        Func<IDbConnection, IDbTransaction, Task> action,
        int commandTimeout = 3600,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();
        try
        {
            await action(conn, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

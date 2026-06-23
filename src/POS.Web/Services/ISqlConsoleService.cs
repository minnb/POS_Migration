namespace POS.Web.Services;

public sealed record DbOption(string Key, string Catalog, string Display);

public sealed record SqlValidation(
    bool Ok, string? Error, StatementKind Kind, bool UpdateHasWhere, string? ObjectName = null);

public enum StatementKind { Select, Insert, Update, CreateProcedure, Invalid }

public sealed class SqlQueryResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public List<string> Columns { get; init; } = [];
    public List<object?[]> Rows { get; init; } = [];
    public long ElapsedMs { get; init; }
    public bool Truncated { get; init; }
}

public interface ISqlConsoleService
{
    IReadOnlyList<DbOption> GetDatabases();
    SqlValidation Validate(string sql);
    Task<SqlQueryResult> ExecuteSelectAsync(string connKey, string sql, string actor, CancellationToken ct);
    Task<PendingUpdate> BeginMutationAsync(string connKey, string sql, string actor, CancellationToken ct);
}

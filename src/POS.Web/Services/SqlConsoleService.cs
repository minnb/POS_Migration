using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using POS.Infrastructure.Logging;

namespace POS.Web.Services;

public sealed class SqlConsoleService(
    IConfiguration configuration,
    IKibanaService kibana,
    IFileLogHelper fileLog) : ISqlConsoleService
{
    private const int MaxRows = 500;
    private const int CommandTimeoutSeconds = 60;

    private readonly IReadOnlyList<DbOption> _databases = BuildDatabases(configuration);

    private static IReadOnlyList<DbOption> BuildDatabases(IConfiguration configuration)
    {
        var list = new List<DbOption>();
        foreach (var entry in configuration.GetSection("ConnectionStrings").GetChildren())
        {
            var value = entry.Value ?? string.Empty;
            if (value.Contains("{server}", StringComparison.OrdinalIgnoreCase)) continue;
            var idx = value.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var start = idx + "Initial Catalog=".Length;
            var end = value.IndexOf(';', start);
            var catalog = end >= 0 ? value[start..end] : value[start..];
            if (!string.IsNullOrWhiteSpace(catalog))
                list.Add(new DbOption(entry.Key, catalog, $"{catalog} ({entry.Key})"));
        }
        return list;
    }

    public IReadOnlyList<DbOption> GetDatabases() => _databases;

    public SqlValidation Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return new SqlValidation(false, "SQL không được để trống.", StatementKind.Invalid, false);

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out IList<ParseError> errors);

        if (errors.Count > 0)
        {
            var msg = string.Join("; ", errors.Take(3).Select(e => $"Dòng {e.Line}: {e.Message}"));
            return new SqlValidation(false, $"Lỗi cú pháp SQL: {msg}", StatementKind.Invalid, false);
        }

        var script = (TSqlScript)fragment;
        var statements = script.Batches.SelectMany(b => b.Statements).ToList();

        if (statements.Count == 0)
            return new SqlValidation(false, "Không có câu lệnh SQL nào.", StatementKind.Invalid, false);

        var hasUpdate = false;
        var hasWhere = true;

        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case SelectStatement:
                    break;
                case UpdateStatement upd:
                    hasUpdate = true;
                    if (upd.UpdateSpecification.WhereClause is null)
                        hasWhere = false;
                    break;
                default:
                    var stmtName = stmt.GetType().Name
                        .Replace("Statement", "", StringComparison.Ordinal)
                        .ToUpperInvariant();
                    return new SqlValidation(false,
                        $"Chỉ cho phép SELECT và UPDATE. Phát hiện lệnh không được phép: {stmtName}.",
                        StatementKind.Invalid, false);
            }
        }

        var kind = hasUpdate ? StatementKind.Update : StatementKind.Select;
        return new SqlValidation(true, null, kind, hasWhere);
    }

    public async Task<SqlQueryResult> ExecuteSelectAsync(
        string connKey, string sql, string actor, CancellationToken ct)
    {
        var validation = Validate(sql);
        if (!validation.Ok)
            return new SqlQueryResult { Success = false, Error = validation.Error };

        if (!TryGetConnectionString(connKey, out var connStr))
            return new SqlQueryResult { Success = false, Error = $"Database '{connKey}' không hợp lệ." };

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = CommandTimeoutSeconds };
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            sw.Stop();

            var columns = new List<string>();
            var rows = new List<object?[]>();
            var truncated = false;

            if (reader.FieldCount > 0)
            {
                for (var i = 0; i < reader.FieldCount; i++)
                    columns.Add(reader.GetName(i));

                var rowCount = 0;
                while (await reader.ReadAsync(ct))
                {
                    if (rowCount >= MaxRows) { truncated = true; break; }
                    var row = new object?[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                        row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                    rowCount++;
                }
            }

            kibana.LogInfo("SqlConsole.Select", actor,
                $"{connKey} | {rows.Count} rows | {sw.ElapsedMilliseconds}ms | {Trunc(sql)}");

            return new SqlQueryResult
            {
                Success  = true,
                Columns  = columns,
                Rows     = rows,
                ElapsedMs = sw.ElapsedMilliseconds,
                Truncated = truncated
            };
        }
        catch (OperationCanceledException)
        {
            kibana.LogInfo("SqlConsole.Select.Cancelled", actor, $"{connKey} | {Trunc(sql)}");
            return new SqlQueryResult { Success = false, Error = "Câu lệnh đã bị hủy." };
        }
        catch (Exception ex)
        {
            kibana.LogException("SqlConsole.Select", actor, 0, connKey, ex.Message);
            return new SqlQueryResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<PendingUpdate> BeginUpdateAsync(
        string connKey, string sql, string actor, CancellationToken ct)
    {
        var validation = Validate(sql);
        if (!validation.Ok)
            throw new InvalidOperationException(validation.Error);

        if (!TryGetConnectionString(connKey, out var connStr))
            throw new InvalidOperationException($"Database '{connKey}' không hợp lệ.");

        var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        var sw = Stopwatch.StartNew();
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = CommandTimeoutSeconds };
        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        sw.Stop();

        kibana.LogInfo("SqlConsole.Update.Begin", actor,
            $"{connKey} | rows={rowsAffected} hasWhere={validation.UpdateHasWhere} | {sw.ElapsedMilliseconds}ms | {Trunc(sql)}");

        return new PendingUpdate(conn, tx, rowsAffected, validation.UpdateHasWhere,
            sw.ElapsedMilliseconds, actor, connKey, sql, kibana, fileLog);
    }

    private bool TryGetConnectionString(string connKey, out string connStr)
    {
        connStr = string.Empty;
        if (!_databases.Any(d => d.Key == connKey)) return false;
        var cs = configuration.GetConnectionString(connKey);
        if (string.IsNullOrEmpty(cs)) return false;
        connStr = cs;
        return true;
    }

    private static string Trunc(string s) => s.Length <= 500 ? s : s[..500] + "...";
}

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;

namespace POS.Web.Services;

public sealed class SqlConsoleService(
    IConfiguration configuration,
    CentralMDConnectionFactory centralMdFactory,
    IFileLogHelper fileLog) : ISqlConsoleService
{
    private const int MaxRows = 500;
    private const int CommandTimeoutSeconds = 60;

    private readonly IReadOnlyList<DbOption> _databases = BuildDatabases(configuration);

    // Cờ tắt SQL Console (Security:EnableSqlConsole). Mặc định true; nên đặt false khi expose internet.
    private readonly bool _enabled = configuration.GetValue("Security:EnableSqlConsole", true);

    // Che giá trị nhạy cảm trong SQL trước khi ghi log/audit: keyword = '...'  →  keyword = '***'.
    // Phủ literal có nháy đơn (kể cả N'...' và escape ''), tránh lưu plaintext credential vào audit/Kibana.
    private static readonly Regex SecretRegex = new(
        @"(?<k>\b(?:password|pwd|passwd|token|secret|apikey|api_key)\b\s*=\s*)(N?'(?:[^']|'')*')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool IsEnabled => _enabled;

    private static string MaskSecrets(string sql) => SecretRegex.Replace(sql, m => m.Groups["k"].Value + "'***'");

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

        var hasInsert = false;
        var hasUpdate = false;
        var hasDelete = false;
        var hasProc = false;
        var hasTableDdl = false;
        var hasOther = false;
        var hasWhere = true;
        string? procName = null;
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stmt in statements)
        {
            // Blacklist tuyệt đối: DROP (mọi loại) và TRUNCATE TABLE — chặn trước khi phân loại.
            var typeName = stmt.GetType().Name;
            if (typeName.StartsWith("Drop", StringComparison.Ordinal) ||
                typeName.StartsWith("Truncate", StringComparison.Ordinal))
            {
                var blockedName = typeName.Replace("Statement", "", StringComparison.Ordinal).ToUpperInvariant();
                return new SqlValidation(false,
                    $"Không cho phép lệnh {blockedName} — DROP/TRUNCATE bị chặn tuyệt đối trong SQL Console.",
                    StatementKind.Invalid, false);
            }

            switch (stmt)
            {
                case SelectStatement:
                    break;
                case InsertStatement:
                    hasInsert = true;
                    break;
                case UpdateStatement upd:
                    hasUpdate = true;
                    if (upd.UpdateSpecification.WhereClause is null)
                        hasWhere = false;
                    break;
                case DeleteStatement del:
                    hasDelete = true;
                    if (del.DeleteSpecification.WhereClause is null)
                        hasWhere = false;
                    break;
                case CreateProcedureStatement cp:
                    hasProc = true;
                    procName = cp.ProcedureReference?.Name?.BaseIdentifier?.Value;
                    break;
                case AlterProcedureStatement ap:
                    hasProc = true;
                    procName = ap.ProcedureReference?.Name?.BaseIdentifier?.Value;
                    break;
                case CreateOrAlterProcedureStatement coa:
                    hasProc = true;
                    procName = coa.ProcedureReference?.Name?.BaseIdentifier?.Value;
                    break;
                case CreateTableStatement ct:
                    hasTableDdl = true;
                    if (ct.SchemaObjectName?.BaseIdentifier?.Value is { } ctName) tableNames.Add(ctName);
                    break;
                case AlterTableStatement alt:
                    hasTableDdl = true;
                    if (alt.SchemaObjectName?.BaseIdentifier?.Value is { } altName) tableNames.Add(altName);
                    break;
                default:
                    hasOther = true;
                    break;
            }
        }

        var kind = hasProc      ? StatementKind.CreateProcedure
                 : hasTableDdl  ? StatementKind.TableDdl
                 : hasUpdate    ? StatementKind.Update
                 : hasDelete    ? StatementKind.Delete
                 : hasInsert    ? StatementKind.Insert
                 : hasOther     ? StatementKind.Other
                 : StatementKind.Select;

        var objectName = hasProc     ? procName
                        : hasTableDdl ? (tableNames.Count > 0 ? string.Join(", ", tableNames) : null)
                        : null;

        return new SqlValidation(true, null, kind, hasWhere, objectName);
    }

    public async Task<SqlQueryResult> ExecuteSelectAsync(
        string connKey, string sql, string actor, CancellationToken ct)
    {
        if (!_enabled)
            return new SqlQueryResult { Success = false, Error = "SQL Console đã bị tắt (Security:EnableSqlConsole=false)." };

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
            await using var dbReader = await cmd.ExecuteReaderAsync(ct);
            sw.Stop();

            var columns = new List<string>();
            var rows = new List<object?[]>();
            var truncated = false;

            if (dbReader.FieldCount > 0)
            {
                for (var i = 0; i < dbReader.FieldCount; i++)
                    columns.Add(dbReader.GetName(i));

                var rowCount = 0;
                while (await dbReader.ReadAsync(ct))
                {
                    if (rowCount >= MaxRows) { truncated = true; break; }
                    var row = new object?[dbReader.FieldCount];
                    for (var i = 0; i < dbReader.FieldCount; i++)
                        row[i] = dbReader.IsDBNull(i) ? null : dbReader.GetValue(i);
                    rows.Add(row);
                    rowCount++;
                }
            }

            fileLog.WriteLogs($"[SqlConsole.Select] actor={actor} {connKey} | {rows.Count} rows | {sw.ElapsedMilliseconds}ms | {Trunc(MaskSecrets(sql))}");

            return new SqlQueryResult
            {
                Success   = true,
                Columns   = columns,
                Rows      = rows,
                ElapsedMs = sw.ElapsedMilliseconds,
                Truncated = truncated
            };
        }
        catch (OperationCanceledException)
        {
            fileLog.WriteLogs($"[SqlConsole.Select.Cancelled] actor={actor} {connKey} | {Trunc(MaskSecrets(sql))}");
            return new SqlQueryResult { Success = false, Error = "Câu lệnh đã bị hủy." };
        }
        catch (Exception ex)
        {
            fileLog.WriteExpLogs("SqlConsole.Select", ex);
            return new SqlQueryResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<PendingUpdate> BeginMutationAsync(
        string connKey, string sql, string actor, CancellationToken ct)
    {
        if (!_enabled)
            throw new InvalidOperationException("SQL Console đã bị tắt (Security:EnableSqlConsole=false).");

        var validation = Validate(sql);
        if (!validation.Ok)
            throw new InvalidOperationException(validation.Error);

        if (!TryGetConnectionString(connKey, out var connStr))
            throw new InvalidOperationException($"Database '{connKey}' không hợp lệ.");

        // Tách script thành các batch theo GO (ScriptDom đã loại bỏ GO).
        var batches = SplitBatches(sql);

        var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        var sw = Stopwatch.StartNew();
        var rowsAffected = 0;
        try
        {
            foreach (var batchSql in batches)
            {
                await using var cmd = new SqlCommand(batchSql, conn, tx) { CommandTimeout = CommandTimeoutSeconds };
                var affected = await cmd.ExecuteNonQueryAsync(ct);
                if (affected > 0) rowsAffected += affected;   // DDL trả -1 → bỏ qua
            }
        }
        catch
        {
            // Lỗi khi chạy → rollback ngay, không để transaction treo.
            try { await tx.RollbackAsync(CancellationToken.None); } catch { /* best-effort */ }
            tx.Dispose();
            await conn.DisposeAsync();
            throw;
        }
        sw.Stop();

        fileLog.WriteLogs($"[SqlConsole.Mutation.Begin] actor={actor} {connKey} | kind={validation.Kind} rows={rowsAffected} hasWhere={validation.UpdateHasWhere} | {sw.ElapsedMilliseconds}ms | {Trunc(MaskSecrets(sql))}");

        var executedAt = DateTime.UtcNow;
        var catalog = _databases.FirstOrDefault(d => d.Key == connKey)?.Catalog ?? connKey;
        var elapsedMs = sw.ElapsedMilliseconds;
        var hasWhere = validation.UpdateHasWhere;

        Func<string, Task> auditCallback = status =>
            WriteAuditAsync(actor, connKey, catalog, sql, rowsAffected, hasWhere, status, elapsedMs, executedAt);

        return new PendingUpdate(conn, tx, rowsAffected, hasWhere,
            elapsedMs, actor, connKey, sql, validation.Kind, validation.ObjectName,
            fileLog, auditCallback);
    }

    /// <summary>
    /// Tách SQL gốc thành từng batch theo separator GO bằng offset của ScriptDom.
    /// GO không phải T-SQL nên SqlCommand không chạy được nguyên script nhiều batch.
    /// </summary>
    private static IReadOnlyList<string> SplitBatches(string sql)
    {
        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out IList<ParseError> errors);
        if (errors.Count > 0 || fragment is not TSqlScript script)
            return [sql];

        var result = new List<string>();
        foreach (var batch in script.Batches)
        {
            if (batch.FragmentLength <= 0) continue;
            var text = sql.Substring(batch.StartOffset, batch.FragmentLength);
            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text);
        }
        return result.Count > 0 ? result : [sql];
    }

    private async Task WriteAuditAsync(
        string actor, string connKey, string catalog, string sql,
        int rowsAffected, bool hasWhere, string status, long elapsedMs, DateTime executedAt)
    {
        const string insertSql = @"
            INSERT INTO SqlConsoleAuditLog
                (Actor, DbKey, DbCatalog, SqlText, RowsAffected, HasWhere, Status, ElapsedMs, ExecutedAt)
            VALUES
                (@Actor, @DbKey, @DbCatalog, @SqlText, @RowsAffected, @HasWhere, @Status, @ElapsedMs, @ExecutedAt)";
        try
        {
            await using var conn = (SqlConnection)await centralMdFactory.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(insertSql, conn) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("@Actor",        actor);
            cmd.Parameters.AddWithValue("@DbKey",        connKey);
            cmd.Parameters.AddWithValue("@DbCatalog",    catalog);
            cmd.Parameters.AddWithValue("@SqlText",      MaskSecrets(sql));
            cmd.Parameters.AddWithValue("@RowsAffected", rowsAffected);
            cmd.Parameters.AddWithValue("@HasWhere",     hasWhere);
            cmd.Parameters.AddWithValue("@Status",       status);
            cmd.Parameters.AddWithValue("@ElapsedMs",    elapsedMs);
            cmd.Parameters.AddWithValue("@ExecutedAt",   executedAt);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            fileLog.WriteExpLogs("SqlConsoleService.WriteAudit", ex);
        }
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

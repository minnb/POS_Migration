using Microsoft.Data.SqlClient;

namespace POS.DbMigrator;

/// <summary>
/// Đọc bảng dbo.SchemaVersions (journal chuẩn của DbUp.SqlServer.SqlTableJournal — xem
/// docs/sql/manifest.json comment) để biết Track B nào DBA đã tự ghi nhận là "đã chạy tay".
/// KHÔNG BAO GIỜ tự chạy Track B — chỉ đọc + báo thiếu.
/// </summary>
/// <remarks>
/// Schema bảng xác nhận qua reflection trực tiếp trên DbUp.SqlServer.SqlTableJournal (dbup-sqlserver
/// 5.0.41): cột (Id int identity PK, ScriptName nvarchar(255), Applied datetime). Không dùng lại
/// class SqlTableJournal của DbUp cho việc ensure/verify này (nó cần Func&lt;IDbCommand&gt; factory
/// khá cồng kềnh cho 1 việc đơn giản) — chỉ tái dùng ĐÚNG SQL text đã xác nhận.
/// </remarks>
public static class TrackBJournalVerifier
{
    public static async Task EnsureTableExistsAsync(string connectionString, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SchemaVersions] (
                    [Id] int identity(1,1) not null constraint PK_SchemaVersions primary key,
                    [ScriptName] nvarchar(255) not null,
                    [Applied] datetime not null
                )
            END
            """;
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public static async Task<HashSet<string>> GetAppliedScriptNamesAsync(string connectionString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT [ScriptName] FROM [dbo].[SchemaVersions]", conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(ct))
            result.Add(reader.GetString(0));
        return result;
    }

    public sealed record MissingEntry(ManifestEntry Entry);

    /// <summary>Track B entries của target chưa có dòng tương ứng trong SchemaVersions.</summary>
    public static async Task<List<MissingEntry>> FindMissingAsync(
        string connectionString, List<ManifestEntry> trackBEntries, CancellationToken ct)
    {
        await EnsureTableExistsAsync(connectionString, ct);
        var applied = await GetAppliedScriptNamesAsync(connectionString, ct);
        return trackBEntries
            .Where(e => !applied.Contains(e.File))
            .Select(e => new MissingEntry(e))
            .ToList();
    }
}

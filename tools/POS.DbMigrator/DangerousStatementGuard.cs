using System.Text.RegularExpressions;

namespace POS.DbMigrator;

/// <summary>
/// Lưới an toàn thứ 2 cho Track A — độc lập với cờ runOnce trong manifest.json.
/// Quét từng batch (tách bởi GO) tìm DDL/DML nguy hiểm KHÔNG được guard bởi IF ở đầu batch.
/// </summary>
/// <remarks>
/// Thiết kế xác nhận qua đọc trực tiếp các file thật trong docs/sql/: mọi DDL một-lần đã có
/// (CREATE TABLE, CREATE INDEX, ALTER TABLE...ADD) trong các file Track A hợp lệ luôn được bọc
/// <c>IF ... BEGIN ... END</c> làm TOÀN BỘ nội dung 1 batch (vd SetupPromotion_AddNumOfDaysList.sql,
/// MasterDataDownloadLog.sql) — batch bắt đầu bằng "IF" nên không khớp regex neo đầu batch của các
/// pattern nguy hiểm bên dưới. Batch KHÔNG bắt đầu bằng IF/CREATE PROC/ALTER PROC mà chứa các
/// pattern này ở đầu batch → coi là bare, chưa guard, nguy hiểm.
/// </remarks>
public static class DangerousStatementGuard
{
    private static readonly Regex GoSeparator = new(@"^[ \t]*GO[ \t]*(--.*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex LeadingLineComment = new(@"\G[ \t]*--[^\n]*\n?", RegexOptions.Compiled);
    private static readonly Regex LeadingBlockComment = new(@"\G[ \t]*/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex LeadingWhitespace = new(@"\G[ \t\r\n]+", RegexOptions.Compiled);

    // Batch bắt đầu bằng 1 trong các định nghĩa object này → an toàn, KHÔNG quét tiếp (DML bên
    // trong thân SP/Function là logic nghiệp vụ bình thường, không phải thực thi ngay lúc deploy).
    private static readonly Regex SafeDefinitionStart = new(
        @"^(CREATE\s+OR\s+ALTER\s+|CREATE\s+|ALTER\s+)?(PROCEDURE|PROC|FUNCTION|VIEW|TYPE)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Batch bắt đầu bằng IF (guard tồn tại kiểu IF OBJECT_ID/IF NOT EXISTS/IF COL_LENGTH...) →
    // coi là đã guard, không quét tiếp — xem ghi chú <remarks> ở trên.
    private static readonly Regex GuardedStart = new(@"^IF\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly (Regex Pattern, string Description)[] DangerPatterns =
    [
        (new Regex(@"^CREATE\s+TABLE\b", RegexOptions.IgnoreCase), "CREATE TABLE (bare, không có guard IF OBJECT_ID)"),
        (new Regex(@"^DROP\s+TABLE\b", RegexOptions.IgnoreCase), "DROP TABLE"),
        (new Regex(@"^TRUNCATE\s+TABLE\b", RegexOptions.IgnoreCase), "TRUNCATE TABLE"),
        (new Regex(@"\bsp_rename\b", RegexOptions.IgnoreCase), "sp_rename"),
        (new Regex(@"^ALTER\s+TABLE\b.*\bADD\b", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            "ALTER TABLE ... ADD (bare, không có guard IF NOT EXISTS/IF COL_LENGTH)"),
        (new Regex(@"^CREATE\s+(NONCLUSTERED\s+|UNIQUE\s+|CLUSTERED\s+)*INDEX\b", RegexOptions.IgnoreCase),
            "CREATE INDEX (bare, không có guard IF NOT EXISTS)"),
        (new Regex(@"^DELETE\s+FROM\b", RegexOptions.IgnoreCase), "DELETE FROM (bare, ngoài thân proc)"),
        (new Regex(@"^UPDATE\b(?!.*\bWHERE\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            "UPDATE không có WHERE (bare, ngoài thân proc)"),
    ];

    public sealed record Finding(string ScriptFile, string BatchPreview, string Description);

    /// <summary>Quét 1 script, trả về danh sách phát hiện nguy hiểm (rỗng = an toàn).</summary>
    public static List<Finding> Scan(string scriptFile, string content)
    {
        var findings = new List<Finding>();
        foreach (var rawBatch in SplitByGo(content))
        {
            var trimmed = StripLeadingComments(rawBatch).TrimStart();
            if (trimmed.Length == 0) continue;
            if (SafeDefinitionStart.IsMatch(trimmed)) continue;
            if (GuardedStart.IsMatch(trimmed)) continue;

            foreach (var (pattern, description) in DangerPatterns)
            {
                if (pattern.IsMatch(trimmed))
                {
                    var preview = trimmed.Length > 80 ? trimmed[..80] + "..." : trimmed;
                    findings.Add(new Finding(scriptFile, preview.Replace('\r', ' ').Replace('\n', ' '), description));
                }
            }
        }
        return findings;
    }

    private static IEnumerable<string> SplitByGo(string content) => GoSeparator.Split(content);

    private static string StripLeadingComments(string batch)
    {
        var pos = 0;
        while (pos < batch.Length)
        {
            var ws = LeadingWhitespace.Match(batch, pos);
            if (ws.Success && ws.Length > 0) { pos += ws.Length; continue; }

            var block = LeadingBlockComment.Match(batch, pos);
            if (block.Success && block.Length > 0) { pos += block.Length; continue; }

            var line = LeadingLineComment.Match(batch, pos);
            if (line.Success && line.Length > 0) { pos += line.Length; continue; }

            break;
        }
        return batch[pos..];
    }
}

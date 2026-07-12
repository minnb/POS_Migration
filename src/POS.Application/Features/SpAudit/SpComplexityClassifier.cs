using System.Text.RegularExpressions;
using POS.Common.Enums;

namespace POS.Application.Features.SpAudit;

/// <summary>
/// Heuristic phân loại độ phức tạp của 1 stored procedure dựa trên text định nghĩa.
/// Pure, không I/O — mục tiêu unit test chính của SqlAuditCli.
/// </summary>
public static partial class SpComplexityClassifier
{
    private const int ComplexLineThreshold = 300;
    private const int ModerateLineThreshold = 100;
    private const int SimpleLineThreshold = 50;
    private const int MinJoinCountForScore = 4;

    [GeneratedRegex(@"\bCURSOR\b", RegexOptions.IgnoreCase)]
    private static partial Regex CursorRegex();

    [GeneratedRegex(@"\bSP_EXECUTESQL\b|\bEXEC(?:UTE)?\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex DynamicSqlRegex();

    // EXEC/EXECUTE gọi 1 procedure khác — loại trừ dynamic SQL (đã bắt ở DynamicSqlRegex).
    [GeneratedRegex(@"\bEXEC(?:UTE)?\s+(?!\(|SP_EXECUTESQL\b)[\[\w]", RegexOptions.IgnoreCase)]
    private static partial Regex NestedExecRegex();

    [GeneratedRegex(@"\bWHILE\b", RegexOptions.IgnoreCase)]
    private static partial Regex WhileRegex();

    [GeneratedRegex(@"\bBEGIN\s+TRY\b", RegexOptions.IgnoreCase)]
    private static partial Regex TryCatchRegex();

    [GeneratedRegex(@"#\w+|@\w+\s+TABLE\b", RegexOptions.IgnoreCase)]
    private static partial Regex TempTableRegex();

    [GeneratedRegex(@"\bMERGE\b", RegexOptions.IgnoreCase)]
    private static partial Regex MergeRegex();

    [GeneratedRegex(@"\bJOIN\b", RegexOptions.IgnoreCase)]
    private static partial Regex JoinRegex();

    /// <summary>Số dòng của định nghĩa procedure (dùng cả để tính điểm và lưu vào DTO).</summary>
    public static int CountLines(string definitionText) =>
        definitionText.Count(c => c == '\n') + 1;

    /// <summary>
    /// Phân loại 2 tầng: tầng 1 là các tín hiệu phức tạp "cứng" (bất kỳ điều nào → Complex ngay);
    /// tầng 2 chấm điểm cho phần còn lại. <paramref name="definitionText"/> null/rỗng (VD WITH
    /// ENCRYPTION) → Moderate kèm ghi chú, không throw.
    /// </summary>
    public static (SpComplexity Complexity, string Note) Classify(string? definitionText)
    {
        if (string.IsNullOrWhiteSpace(definitionText))
            return (SpComplexity.Moderate, "Không đọc được định nghĩa (WITH ENCRYPTION?) — không thể xác minh độ đơn giản");

        if (CursorRegex().IsMatch(definitionText)) return (SpComplexity.Complex, "");
        if (DynamicSqlRegex().IsMatch(definitionText)) return (SpComplexity.Complex, "");
        if (NestedExecRegex().IsMatch(definitionText)) return (SpComplexity.Complex, "");

        var lineCount = CountLines(definitionText);
        if (lineCount > ComplexLineThreshold) return (SpComplexity.Complex, "");

        var score = 0;
        if (WhileRegex().IsMatch(definitionText)) score++;
        if (TryCatchRegex().IsMatch(definitionText)) score++;
        if (TempTableRegex().IsMatch(definitionText)) score++;
        if (lineCount >= ModerateLineThreshold) score++;
        if (MergeRegex().IsMatch(definitionText)) score++;
        if (JoinRegex().Matches(definitionText).Count >= MinJoinCountForScore) score++;

        return score == 0 && lineCount < SimpleLineThreshold
            ? (SpComplexity.Simple, "")
            : (SpComplexity.Moderate, "");
    }
}

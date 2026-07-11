using System.Text.RegularExpressions;

namespace POS.DbMigrator;

/// <summary>
/// Chuẩn hóa "ALTER PROC/PROCEDURE" (bare) → "CREATE OR ALTER PROCEDURE" cho các file
/// docs/sql/*.sql chỉ chứa ALTER PROC đơn thuần (không kèm DDL một-lần khác trong cùng file).
/// </summary>
/// <remarks>
/// Danh sách 8 file xác nhận qua grep trực tiếp `ALTER\s+PROC(EDURE)?\s` toàn bộ docs/sql/ —
/// xem plan gốc (t-t-c-c-c-script-temporal-lighthouse.md, Thành phần 5). Regex neo ĐẦU DÒNG
/// (^ALTER...) để tránh khớp nhầm 3 chỗ chữ "ALTER PROC" nằm trong comment tiếng Việt (đã xác
/// nhận: comment luôn có văn bản/khoảng trắng đứng trước, statement thật luôn ở cột 1).
/// </remarks>
public static class NormalizeAlterProc
{
    public static readonly string[] TargetFiles =
    [
        "GetSalesPriceList_AddSaleType.sql",
        "GetSalesPriceList_AddSalesTypeCode.sql",
        "SalesPrice_AddCounterOutput.sql",
        "SalesPrice_EditDelete_AddSalesType.sql",
        "GetPromotionOfferHeaderList_AddDateRangeFilter.sql",
        "GetPromotionOfferHeaderList_FixDuplicateRows.sql",
        "SetupSalePrice_Save.sql",
        "SyncGetDataByTable_AddFilter.sql",
    ];

    private static readonly Regex BareAlterProc = new(
        @"^ALTER[ \t]+PROC(EDURE)?\b", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public sealed record FileResult(string File, int ReplacementCount, string OriginalContent, string NewContent);

    public static List<FileResult> Preview(string sqlDirectory)
    {
        var results = new List<FileResult>();
        foreach (var file in TargetFiles)
        {
            var path = Path.Combine(sqlDirectory, file);
            var original = File.ReadAllText(path);
            var count = 0;
            var updated = BareAlterProc.Replace(original, m =>
            {
                count++;
                return "CREATE OR ALTER PROCEDURE";
            });
            results.Add(new FileResult(file, count, original, updated));
        }
        return results;
    }

    public static void Apply(string sqlDirectory, List<FileResult> preview)
    {
        foreach (var r in preview)
        {
            if (r.ReplacementCount == 0) continue;
            var path = Path.Combine(sqlDirectory, r.File);
            File.WriteAllText(path, r.NewContent);
        }
    }
}

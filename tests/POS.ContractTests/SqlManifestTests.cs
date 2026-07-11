using POS.DbMigrator;

namespace POS.ContractTests;

/// <summary>
/// GUARDRAIL manifest SQL — chặn lỗi "quên đăng ký script SQL mới vào docs/sql/manifest.json"
/// lúc build/test thay vì lúc DBA phát hiện thiếu trên production (xem CLAUDE.md mục "Cổng chặn
/// trùng lặp" + docs/sql/manifest.json).
///
/// Test 5 (content-guard) dùng CHUNG logic quét với tools/POS.DbMigrator (DangerousStatementGuard)
/// — bắt được cả trường hợp gắn nhầm runOnce=false cho 1 script thực ra là DDL một-lần.
/// </summary>
public class SqlManifestTests
{
    private static string SqlDirectory => ManifestScriptProvider.ResolveSqlDirectory(AppContext.BaseDirectory, sqlDirOverride: null);

    [Fact]
    public void MoiFileSqlPhaiCoMatTrongManifest()
    {
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var manifestFiles = manifest.Scripts.Select(s => s.File).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var diskFiles = Directory.GetFiles(SqlDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Cast<string>()
            .ToList();

        var missing = diskFiles.Where(f => !manifestFiles.Contains(f)).ToList();

        Assert.True(missing.Count == 0,
            $"Có file .sql trong docs/sql/ CHƯA đăng ký trong manifest.json: {string.Join(", ", missing)}. " +
            "Xem .claude/skills/database/SKILLS.md — mọi script mới phải đăng ký cùng commit.");
    }

    [Fact]
    public void MoiEntryManifestPhaiTroToiFileCoThat()
    {
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var missing = manifest.Scripts
            .Where(s => !File.Exists(Path.Combine(SqlDirectory, s.File)))
            .Select(s => s.File)
            .ToList();

        Assert.True(missing.Count == 0,
            $"manifest.json tham chiếu file không tồn tại trên đĩa: {string.Join(", ", missing)}.");
    }

    [Fact]
    public void OrderKhongDuocTrung()
    {
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var dupOrders = manifest.Scripts
            .GroupBy(s => s.Order)
            .Where(g => g.Count() > 1)
            .Select(g => $"order={g.Key}: {string.Join(" & ", g.Select(s => s.File))}")
            .ToList();

        Assert.True(dupOrders.Count == 0, $"Trùng giá trị order trong manifest.json: {string.Join("; ", dupOrders)}.");
    }

    [Fact]
    public void TargetPhaiHopLe()
    {
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var invalid = manifest.Scripts
            .Where(s => !ManifestScriptProvider.ValidTargets.Contains(s.Target))
            .Select(s => $"{s.File} (target={s.Target})")
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Target không hợp lệ (phải là {string.Join("/", ManifestScriptProvider.ValidTargets)}): {string.Join(", ", invalid)}.");
    }

    [Fact]
    public void TrackAKhongDuocChuaPatternNguyHiem()
    {
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var allFindings = new List<string>();

        foreach (var entry in manifest.Scripts.Where(s => !s.RunOnce))
        {
            var content = ManifestScriptProvider.ReadScriptContent(SqlDirectory, entry);
            var findings = DangerousStatementGuard.Scan(entry.File, content);
            allFindings.AddRange(findings.Select(f => $"{f.ScriptFile}: {f.Description} — \"{f.BatchPreview}\""));
        }

        Assert.True(allFindings.Count == 0,
            "Phát hiện DDL/DML nguy hiểm trong script gắn runOnce=false (Track A) — script này phải " +
            $"là Track B (runOnce=true), kiểm tra lại manifest.json:\n{string.Join("\n", allFindings)}");
    }
}

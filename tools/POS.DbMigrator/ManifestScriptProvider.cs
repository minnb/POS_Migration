using Newtonsoft.Json;

namespace POS.DbMigrator;

public sealed class ManifestEntry
{
    public int Order { get; set; }
    public string File { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public bool RunOnce { get; set; }
    public string? Phase { get; set; }
    public string? Note { get; set; }
}

public sealed class Manifest
{
    public List<ManifestEntry> Scripts { get; set; } = [];
}

/// <summary>
/// Đọc docs/sql/manifest.json + nội dung từng file .sql tương ứng.
/// Nguồn sự thật duy nhất về thứ tự (order) và phân loại (runOnce) — xem
/// .claude/skills/database/SKILLS.md mục "Noi dat script SQL".
/// </summary>
public static class ManifestScriptProvider
{
    public const string TargetCentralMD = "CentralMD";
    public const string TargetCentralSaleShards = "CentralSaleShards";

    public static readonly string[] ValidTargets = [TargetCentralMD, TargetCentralSaleShards];

    /// <summary>
    /// Tìm thư mục docs/sql/ để chạy. Ưu tiên <paramref name="sqlDirOverride"/> (CLI <c>--sql-dir</c>) —
    /// BẮT BUỘC dùng khi tool được publish/deploy ĐỘC LẬP khỏi git checkout (Docker, Ubuntu bare-metal:
    /// không có POS.slnx nào để dò ngược). Không truyền override → dò ngược tìm POS.slnx từ
    /// <paramref name="startDirectory"/> (tiện cho `dotnet run` ngay trong repo lúc dev) rồi suy ra
    /// {repoRoot}/docs/sql.
    /// </summary>
    public static string ResolveSqlDirectory(string startDirectory, string? sqlDirOverride)
    {
        if (!string.IsNullOrWhiteSpace(sqlDirOverride))
        {
            if (!Directory.Exists(sqlDirOverride))
                throw new DirectoryNotFoundException($"--sql-dir chỉ tới thư mục không tồn tại: '{sqlDirOverride}'.");
            return Path.GetFullPath(sqlDirOverride);
        }

        var repoRoot = RepoRootLocator.FindRepoRoot(startDirectory);
        if (repoRoot is not null)
            return Path.Combine(repoRoot, "docs", "sql");

        throw new InvalidOperationException(
            $"Không tìm thấy POS.slnx đi ngược từ '{startDirectory}', và không có --sql-dir <path>. " +
            "Khi chạy binary đã publish/deploy ngoài git checkout (Docker, Ubuntu bare-metal) BẮT BUỘC " +
            "truyền --sql-dir trỏ tới thư mục chứa manifest.json + toàn bộ *.sql.");
    }

    public static string ManifestPath(string sqlDirectory) => Path.Combine(sqlDirectory, "manifest.json");

    public static Manifest Load(string sqlDirectory)
    {
        var path = ManifestPath(sqlDirectory);
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Manifest>(json)
            ?? throw new InvalidOperationException($"Không đọc được manifest.json tại '{path}'.");
    }

    public static string ReadScriptContent(string sqlDirectory, ManifestEntry entry)
    {
        var path = Path.Combine(sqlDirectory, entry.File);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Manifest tham chiếu file không tồn tại: {entry.File}", path);
        return File.ReadAllText(path);
    }

    /// <summary>Track A (runOnce=false) của 1 target, sắp theo order tăng dần.</summary>
    public static List<ManifestEntry> TrackAFor(Manifest manifest, string target) =>
        manifest.Scripts.Where(s => !s.RunOnce && s.Target == target).OrderBy(s => s.Order).ToList();

    /// <summary>Track B (runOnce=true) của 1 target, sắp theo order tăng dần.</summary>
    public static List<ManifestEntry> TrackBFor(Manifest manifest, string target) =>
        manifest.Scripts.Where(s => s.RunOnce && s.Target == target).OrderBy(s => s.Order).ToList();
}

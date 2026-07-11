namespace POS.DbMigrator;

/// <summary>
/// Dò ngược thư mục cha tìm file POS.slnx — dùng chung cho việc tự suy ra docs/sql (--sql-dir) và
/// appsettings.json của POS.Api (--config) khi chạy trong git checkout (dev/CI).
/// </summary>
public static class RepoRootLocator
{
    /// <summary>Dò ngược từ startDirectory. Trả về null nếu không thấy POS.slnx — nghĩa là chạy
    /// binary đã publish/deploy ngoài git checkout (Docker, Ubuntu bare-metal).</summary>
    public static string? FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "POS.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}

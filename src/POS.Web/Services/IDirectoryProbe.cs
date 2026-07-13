namespace POS.Web.Services;

/// <summary>
/// Seam bọc I/O thư mục/file thật — cho phép unit test giả lập UnauthorizedAccessException và
/// symbolic link mà không cần thao túng ACL/symlink thật (dễ vỡ, phụ thuộc platform/quyền chạy CI).
/// </summary>
public interface IDirectoryProbe
{
    IEnumerable<string> EnumerateDirectories(string path);
    IEnumerable<string> EnumerateFiles(string path);

    /// <summary>true nếu chính <paramref name="path"/> là symbolic link (không theo dõi chain quá 1 cấp).</summary>
    bool IsSymbolicLink(string path);
}

public sealed class PhysicalDirectoryProbe : IDirectoryProbe
{
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public bool IsSymbolicLink(string path)
    {
        try
        {
            if (Directory.Exists(path)) return new DirectoryInfo(path).LinkTarget is not null;
            if (File.Exists(path)) return new FileInfo(path).LinkTarget is not null;
            return false;
        }
        catch
        {
            // best-effort — lỗi thật (quyền, không tồn tại...) sẽ tự lộ ra ở bước enumerate/open sau
            return false;
        }
    }
}

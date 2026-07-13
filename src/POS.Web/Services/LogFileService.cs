using POS.Infrastructure.Logging;

namespace POS.Web.Services;

// Lưu ý luồng download: LogFilePage.razor stream file qua JS.SaveAsFileAsync(Stream) (JsDownloadExtensions)
// dùng DotNetStreamReference trên kênh SignalR /_blazor hiện có (JS interop) — KHÔNG phải 1 HTTP
// endpoint REST riêng. Vì vậy KHÔNG cần thêm nginx location/route riêng cho "download" — block
// /_blazor trong nginx/pos-web.conf (đã có proxy_buffering off + timeout dài) là đủ.
public sealed class LogFileService : ILogFileService
{
    private static readonly string[] AllowedExtensions = [".txt", ".log"];

    // Không còn là giới hạn RAM cứng (DownloadLogFileAsync giờ trả FileStream, không buffer byte[] nữa)
    // — vẫn giữ ngưỡng hợp lý cho UX/thời gian giữ kết nối SignalR khi tải file cực lớn.
    private readonly long _maxDownloadBytes;

    private readonly IFileLogHelper _fileLogHelper;
    private readonly IDirectoryProbe _directoryProbe;
    private readonly string _rootDir;

    public LogFileService(IConfiguration configuration, IFileLogHelper fileLogHelper, IDirectoryProbe directoryProbe)
    {
        _fileLogHelper = fileLogHelper;
        _directoryProbe = directoryProbe;
        _maxDownloadBytes = configuration.GetValue<long?>("LogViewer:MaxDownloadBytes") ?? 500L * 1024 * 1024;

        var explicitRootDir = configuration["Logging:LogDirectory"];
        if (!string.IsNullOrWhiteSpace(explicitRootDir))
        {
            _rootDir = Path.GetFullPath(explicitRootDir);
        }
        else
        {
            // Fallback (tương thích ngược nếu môi trường nào chưa cấu hình Logging:LogDirectory):
            // suy ra thư mục cha của FileLogDirectory.
            var configuredDir = configuration["Logging:FileLogDirectory"] ?? "Logs";
            var fullConfiguredDir = Path.GetFullPath(configuredDir);
            var parent = Directory.GetParent(fullConfiguredDir);
            _rootDir = parent?.FullName ?? fullConfiguredDir;
        }
    }

    public string RootDirectory => _rootDir;

    public Task<LogDirectoryListing> GetDirectoryListingAsync(string relativePath = "", CancellationToken ct = default)
    {
        var normalizedPath = NormalizeRelativePath(relativePath);
        var fullPath = ResolveSafePath(normalizedPath);
        if (fullPath is null)
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], [],
                $"Đường dẫn không hợp lệ hoặc nằm ngoài thư mục gốc log ({_rootDir})."));

        // Chống symlink attack: string path hợp lệ theo prefix check không đảm bảo target thật nằm
        // trong root — 1 symlink tạo BÊN TRONG root có thể trỏ ra ngoài (vd /etc/shadow). Chặn ngay
        // nếu bản thân thư mục đang duyệt là symlink.
        if (_directoryProbe.IsSymbolicLink(fullPath))
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], [],
                $"Đường dẫn không hợp lệ (không hỗ trợ symbolic link): {fullPath}"));

        try
        {
            // Cố ý KHÔNG dùng Directory.Exists làm cổng chặn: khi tiến trình bị chặn quyền,
            // Directory.Exists trả về false (nuốt UnauthorizedAccessException) → UI hiện "rỗng"
            // sai sự thật. Để enumerate ném lỗi thật rồi bắt theo từng loại để báo lên UI.
            var folders = _directoryProbe.EnumerateDirectories(fullPath)
                .Where(dir => !_directoryProbe.IsSymbolicLink(dir)) // loại symlink khỏi cây — không cho chọn
                .Select(dir => new { Dir = dir, Name = Path.GetFileName(dir) })
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .Select(x => new LogFolderInfo(
                    x.Name!,
                    CombineRelative(normalizedPath, x.Name!),
                    HasSubfolders: HasAnySubdirectory(x.Dir)))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var files = new List<LogFileInfo>();
            foreach (var path in _directoryProbe.EnumerateFiles(fullPath))
            {
                ct.ThrowIfCancellationRequested();

                if (!AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    continue;

                if (_directoryProbe.IsSymbolicLink(path)) // loại symlink khỏi bảng — không cho tải
                    continue;

                var info = new FileInfo(path);
                files.Add(new LogFileInfo(
                    RelativePath: CombineRelative(normalizedPath, info.Name),
                    FileName: info.Name,
                    SizeBytes: info.Length,
                    LastModifiedUtc: info.LastWriteTimeUtc));
            }

            files = files.OrderByDescending(f => f.LastModifiedUtc).ToList();

            return Task.FromResult(new LogDirectoryListing(normalizedPath, folders, files));
        }
        catch (OperationCanceledException)
        {
            // Circuit đang dispose / người dùng đổi thư mục — không phải lỗi đọc, để tầng gọi bỏ qua.
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.GetDirectoryListingAsync", ex);
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], [], BuildPermissionMessage(fullPath, ex)));
        }
        catch (DirectoryNotFoundException ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.GetDirectoryListingAsync", ex);
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], [],
                $"Thư mục không tồn tại: {fullPath}"));
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.GetDirectoryListingAsync", ex);
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], [],
                $"Lỗi đọc thư mục {fullPath}: {ex.Message}"));
        }
    }

    private static string BuildPermissionMessage(string fullPath, Exception ex)
    {
        string identity;
        try
        {
            // WindowsIdentity.GetCurrent() ném PlatformNotSupportedException trên Linux (UAT/PROD) → fallback.
            identity = OperatingSystem.IsWindows()
                ? System.Security.Principal.WindowsIdentity.GetCurrent().Name
                : Environment.UserName;
        }
        catch
        {
            identity = Environment.UserName;
        }

        return $"Không có quyền đọc thư mục: {fullPath}. " +
               $"Tiến trình ứng dụng đang chạy dưới tài khoản '{identity}'. " +
               $"Hãy cấp quyền Đọc/Liệt kê (Read & List folder contents) cho tài khoản này trên thư mục gốc log và các thư mục con. " +
               $"Chi tiết kỹ thuật: {ex.Message}";
    }

    public Task<LogFileDownload> DownloadLogFileAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(LogFileDownload.Fail("Chưa chọn file để tải."));

        var fullPath = ResolveSafePath(relativePath);
        if (fullPath is null)
            return Task.FromResult(LogFileDownload.Fail($"Đường dẫn không hợp lệ hoặc nằm ngoài thư mục gốc log ({_rootDir})."));

        if (!AllowedExtensions.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(LogFileDownload.Fail("Chỉ hỗ trợ tải file .log / .txt."));

        try
        {
            // Phòng thủ 2 lớp (đã lọc ở GetDirectoryListingAsync, nhưng chặn lại đây để tránh race
            // condition: symlink được tạo SAU khi listing trả về, TRƯỚC khi user bấm Download).
            if (_directoryProbe.IsSymbolicLink(fullPath))
                return Task.FromResult(LogFileDownload.Fail("Không hỗ trợ tải symbolic link."));

            var info = new FileInfo(fullPath);
            if (!info.Exists)
                return Task.FromResult(LogFileDownload.Fail($"File không tồn tại: {fullPath}"));

            if (info.Length > _maxDownloadBytes)
                return Task.FromResult(LogFileDownload.Fail(
                    $"File quá lớn để tải qua trình duyệt ({info.Length / 1024d / 1024d:F1} MB > {_maxDownloadBytes / 1024 / 1024} MB). " +
                    $"Hãy lấy trực tiếp trên máy chủ tại: {fullPath}"));

            // Streaming thật — KHÔNG nạp toàn bộ file vào byte[] server (tránh RAM pressure khi nhiều
            // user tải file lớn đồng thời). FileStream trả về nguyên vẹn cho caller, caller chịu
            // trách nhiệm dispose sau khi JS interop đọc xong (xem LogFilePage.razor.DownloadAsync).
            // FileShare.ReadWrite: file log của hôm nay đang được ghi (Serilog/FileLogHelper giữ handle
            // ghi) — FileShare.Read mặc định sẽ ném IOException khi file đang có handle ghi.
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 81920, useAsync: true);
            return Task.FromResult(LogFileDownload.Ok(stream, info.Name));
        }
        catch (UnauthorizedAccessException ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.DownloadLogFileAsync", ex);
            return Task.FromResult(LogFileDownload.Fail(BuildPermissionMessage(fullPath, ex)));
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.DownloadLogFileAsync", ex);
            return Task.FromResult(LogFileDownload.Fail($"Lỗi đọc file {fullPath}: {ex.Message}"));
        }
    }

    private string? ResolveSafePath(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var fullPath = string.IsNullOrEmpty(normalized)
            ? _rootDir
            : Path.GetFullPath(Path.Combine(_rootDir, normalized));

        var rootWithSeparator = _rootDir.EndsWith(Path.DirectorySeparatorChar)
            ? _rootDir
            : _rootDir + Path.DirectorySeparatorChar;

        var isRootItself = fullPath.Equals(_rootDir, StringComparison.OrdinalIgnoreCase);
        return isRootItself || fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
    }

    // Kiểm tra folder con có thư mục con nữa không (để quyết định hiện caret expand + gọi lazy ServerData).
    // Lỗi quyền/không đọc được → coi như không có, KHÔNG làm hỏng toàn bộ listing.
    private bool HasAnySubdirectory(string fullDir)
    {
        try { return _directoryProbe.EnumerateDirectories(fullDir).Any(); }
        catch { return false; }
    }

    private static string NormalizeRelativePath(string? relativePath) =>
        (relativePath ?? string.Empty).Replace('\\', '/').Trim('/');

    private static string CombineRelative(string currentPath, string name) =>
        string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";
}

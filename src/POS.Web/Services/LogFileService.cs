using POS.Infrastructure.Logging;

namespace POS.Web.Services;

public sealed class LogFileService : ILogFileService
{
    private static readonly string[] AllowedExtensions = [".txt", ".log"];

    private readonly IFileLogHelper _fileLogHelper;
    private readonly string _rootDir;

    public LogFileService(IConfiguration configuration, IFileLogHelper fileLogHelper)
    {
        _fileLogHelper = fileLogHelper;

        var configuredDir = configuration["Logging:FileLogDirectory"] ?? "Logs";
        var fullConfiguredDir = Path.GetFullPath(configuredDir);
        var parent = Directory.GetParent(fullConfiguredDir);
        _rootDir = parent?.FullName ?? fullConfiguredDir;
    }

    public Task<LogDirectoryListing> GetDirectoryListingAsync(string relativePath = "", CancellationToken ct = default)
    {
        var normalizedPath = NormalizeRelativePath(relativePath);
        try
        {
            var fullPath = ResolveSafePath(normalizedPath);
            if (fullPath is null || !Directory.Exists(fullPath))
                return Task.FromResult(new LogDirectoryListing(normalizedPath, [], []));

            var folders = Directory.EnumerateDirectories(fullPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new LogFolderInfo(name!, CombineRelative(normalizedPath, name!)))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var files = new List<LogFileInfo>();
            foreach (var path in Directory.EnumerateFiles(fullPath))
            {
                ct.ThrowIfCancellationRequested();

                if (!AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
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
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.GetDirectoryListingAsync", ex);
            return Task.FromResult(new LogDirectoryListing(normalizedPath, [], []));
        }
    }

    public async Task<(byte[] Bytes, string FileName)?> DownloadLogFileAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            var fullPath = ResolveSafePath(relativePath);
            if (fullPath is null)
                return null;

            if (!AllowedExtensions.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase))
                return null;

            if (!File.Exists(fullPath))
                return null;

            var bytes = await File.ReadAllBytesAsync(fullPath, ct);
            return (bytes, Path.GetFileName(fullPath));
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.DownloadLogFileAsync", ex);
            return null;
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

    private static string NormalizeRelativePath(string? relativePath) =>
        (relativePath ?? string.Empty).Replace('\\', '/').Trim('/');

    private static string CombineRelative(string currentPath, string name) =>
        string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";
}

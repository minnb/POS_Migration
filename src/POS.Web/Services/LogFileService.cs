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

    public Task<IReadOnlyList<LogFileInfo>> GetLogFilesAsync(CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_rootDir))
                return Task.FromResult<IReadOnlyList<LogFileInfo>>([]);

            var result = new List<LogFileInfo>();
            foreach (var path in Directory.EnumerateFiles(_rootDir, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                if (!AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    continue;

                var info = new FileInfo(path);
                var relativePath = Path.GetRelativePath(_rootDir, path).Replace('\\', '/');
                var folderName = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');

                result.Add(new LogFileInfo(
                    RelativePath: relativePath,
                    FileName: info.Name,
                    FolderName: string.IsNullOrEmpty(folderName) ? "(root)" : folderName,
                    SizeBytes: info.Length,
                    LastModifiedUtc: info.LastWriteTimeUtc));
            }

            return Task.FromResult<IReadOnlyList<LogFileInfo>>(result);
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LogFileService.GetLogFilesAsync", ex);
            return Task.FromResult<IReadOnlyList<LogFileInfo>>([]);
        }
    }

    public async Task<(byte[] Bytes, string FileName)?> DownloadLogFileAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            var fullPath = Path.GetFullPath(Path.Combine(_rootDir, relativePath));
            var rootWithSeparator = _rootDir.EndsWith(Path.DirectorySeparatorChar)
                ? _rootDir
                : _rootDir + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
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
}

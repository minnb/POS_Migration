namespace POS.Web.Services;

public interface ILogFileService
{
    Task<IReadOnlyList<LogFileInfo>> GetLogFilesAsync(CancellationToken ct = default);
    Task<(byte[] Bytes, string FileName)?> DownloadLogFileAsync(string relativePath, CancellationToken ct = default);
}

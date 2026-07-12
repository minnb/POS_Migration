namespace POS.Web.Services;

public interface ILogFileService
{
    Task<LogDirectoryListing> GetDirectoryListingAsync(string relativePath = "", CancellationToken ct = default);
    Task<(byte[] Bytes, string FileName)?> DownloadLogFileAsync(string relativePath, CancellationToken ct = default);
    Task<IReadOnlyList<LogFolderInfo>> GetSubfoldersAsync(string relativePath = "", CancellationToken ct = default);
}

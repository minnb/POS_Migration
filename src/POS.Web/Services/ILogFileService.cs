namespace POS.Web.Services;

public interface ILogFileService
{
    /// <summary>Thư mục gốc tuyệt đối đang được quét (hiển thị lên UI để chẩn đoán quyền truy cập).</summary>
    string RootDirectory { get; }

    Task<LogDirectoryListing> GetDirectoryListingAsync(string relativePath = "", CancellationToken ct = default);
    Task<LogFileDownload> DownloadLogFileAsync(string relativePath, CancellationToken ct = default);
}

using POS.Common.Dtos.DataSync;
using POS.Common.Dtos.FileModel;

namespace POS.Application.Features.DataSync;

/// <summary>
/// Sync file giữa API server và máy POS (SOD files, log upload, upgrade tool).
/// Migrated từ logic private của SyncDataPosController cũ + UtilsAPI.
/// </summary>
public interface ISyncDataPosService
{
    /// <summary>Map đường dẫn tương đối dưới FTPBLUEPOS → physical path (thay HostingEnvironment.MapPath("/FTPBLUEPOS/...") cũ).</summary>
    string MapFtpPath(string relativePath);

    /// <summary>
    /// Map đường dẫn tương đối dưới site root (thư mục CHA của FTPBLUEPOS) → physical path.
    /// Thay HostingEnvironment.MapPath("/...") cũ khi path client gửi có thể chứa sẵn prefix FTPBLUEPOS.
    /// </summary>
    string MapSitePath(string relativePath);

    /// <summary>Đọc 1 giá trị POSDataSetup theo code (WWW_LIMIT_REQUEST_MD, LINKAPI_DOWN...). Cache Redis qua repository.</summary>
    Task<string?> GetPosDataSetupValueAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// List file zip cho POS download (port GetFileFromServerAPI cũ).
    /// Tự xóa file cũ hơn 2 ngày khỏi danh sách.
    /// </summary>
    Task<List<PathFileAPIModel>> GetFileFromServerApiAsync(
        string pathSync, string folderFile, string typeSync, string syncApi, string ipServer,
        CancellationToken ct = default);

    /// <summary>Đếm số request SOD đang xử lý trên server (Redis key GetFileFromFTP:*). Returns (Allowed, Processing).</summary>
    (bool Allowed, int Processing) CheckSodQueueLimit(string ipSrvKey, int limit);

    /// <summary>Check marker tắt SOD-fake toàn cục (key GetFileFromFTP:GetFileFromFTP).</summary>
    bool SodGlobalMarkerExists();

    /// <summary>Đăng ký 1 request SOD vào queue Redis (TTL 10 phút). Returns full key để dequeue.</summary>
    string EnqueueSodRequest(string ipSrvKey, string posTerminal);

    void DequeueSodRequest(string fullKey);

    /// <summary>Upload file log (*.zip) lên FTP — config server/user/pass từ POSDataSetup (FTPSERVER/FTPUSERNAME/FTPPASSWORD).</summary>
    Task UploadFileLogToFtpAsync(string pathFileApi, string pathFtpServer, CancellationToken ct = default);

    /// <summary>List file upgrade tool từ shared folder (AppSettings:FolderShareUpdSource).</summary>
    Task<List<PathFileAPIModel>> DownloadFileUpgradeToolShareFolderAsync(string ipServer, CancellationToken ct = default);

    /// <summary>Xóa file trên local server hoặc shared folder của server khác (credentials AppSettings:RemoteSvrUser/Pass).</summary>
    Task DeleteFileExistAsync(List<PathFileAPIModel> model, string ipServerHost);

    /// <summary>
    /// Chuyển đường dẫn POS gửi (UNC Windows \\ip\FTPBLUEPOS\... ) → physical path local dưới FtpRootPath
    /// (dùng chung cho DowloadFileStream + DeleteFileFromFTP). Không phải UNC dưới FTPBLUEPOS → trả nguyên giá trị.
    /// </summary>
    string ResolveFtpPhysicalPath(string? posPath);

    /// <summary>
    /// Đẩy/sinh file master data đầu ngày cho 1 máy POS (dùng cho nút SyncData trên POS.Web).
    /// Sinh full data (TypeSync=ALL, POSLastCounter=0) qua <see cref="IMasterDataSyncService.EnsureMasterDataFileAsync"/>
    /// nhưng đặt zip vào thư mục CHANGE (nơi máy POS đọc khi gọi typeSync=CHANGE): {AppSettings:FolderShare}\CHANGE\{siteCode}\{posTerminal}.
    /// Idempotent theo ngày (zip hôm nay đã có → trả ngay). KHÔNG đổi logic sinh file txt/zip của POS.Api — chỉ orchestrate.
    /// </summary>
    Task<GetMasterDataFileResult> PushStartOfDayDataAsync(
        string siteCode, string posTerminal, CancellationToken ct = default);
}

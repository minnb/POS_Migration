namespace POS.Infrastructure.Files;

/// <summary>
/// Upload file lên FTP server qua FluentFTP (managed .NET, không cần binary ngoài — chạy được trên
/// Linux/Ubuntu container, khác WinSCP cũ vốn cần spawn winscp.exe chỉ chạy trên Windows).
/// </summary>
public interface IFtpFileTransfer
{
    /// <summary>
    /// Upload toàn bộ *.zip trong sourcePathFolder lên destPathFolderFtp, xóa file local sau khi upload thành công.
    /// Không throw — lỗi được log và bỏ qua (giữ behavior cũ).
    /// </summary>
    void UploadZipFiles(string sourcePathFolder, string destPathFolderFtp,
        string ftpServer, string ftpUsername, string ftpPassword);
}

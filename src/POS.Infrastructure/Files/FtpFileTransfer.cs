using FluentFTP;
using POS.Infrastructure.Logging;

namespace POS.Infrastructure.Files;

/// <summary>
/// Migrated từ UtilsAPI.UploadFilesLogJobWinSCP — đổi backend từ WinSCP (spawn winscp.exe, chỉ chạy
/// Windows) sang FluentFTP (managed .NET thuần, chạy được trên Linux/Ubuntu container nơi API thực
/// tế được deploy). Mọi exception được nuốt + log như code cũ; log bằng ex.ToString() thay vì
/// JsonConvert.SerializeObject(ex) — tránh Newtonsoft đệ quy vào property nội bộ của exception/client
/// (từng gây lỗi che giấu nguyên nhân gốc khi dùng WinSCP.Session đã dispose).
/// </summary>
public sealed class FtpFileTransfer(IFileLogHelper fileLogHelper) : IFtpFileTransfer
{
    public void UploadZipFiles(string sourcePathFolder, string destPathFolderFtp,
        string ftpServer, string ftpUsername, string ftpPassword)
    {
        try
        {
            var files = Directory.GetFiles(sourcePathFolder, "*.zip");
            if (files.Length == 0) return;

            using var client = new FtpClient(ftpServer, ftpUsername, ftpPassword);
            // PASV bắt buộc khi client chạy sau NAT/Docker (container không nhận được kết nối ngược
            // từ server ở Active mode).
            client.Config.DataConnectionType = FtpDataConnectionType.PASV;
            client.Connect();

            if (!client.DirectoryExists(destPathFolderFtp))
                client.CreateDirectory(destPathFolderFtp);

            foreach (var srcFile in files)
            {
                var remotePath = $"{destPathFolderFtp.TrimEnd('/')}/{Path.GetFileName(srcFile)}";
                var status = client.UploadFile(srcFile, remotePath, FtpRemoteExists.Overwrite, false);
                if (status != FtpStatus.Success) continue;

                try
                {
                    if (File.Exists(srcFile))
                        File.Delete(srcFile);
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteLogs($"[LogJob] File log job {srcFile} {ex}");
                }
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"[LogJob] Upload file log job {ex}");
        }
    }
}

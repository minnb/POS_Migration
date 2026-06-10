using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using POS.Infrastructure.Logging;
using WinSCP;

namespace POS.Infrastructure.Files;

/// <summary>
/// Migrated từ UtilsAPI.UploadFilesLogJobWinSCP + createSessionOptions (protocol FTP).
/// WinSCP .NET assembly cần winscp.exe — đường dẫn tùy chỉnh qua AppSettings:WinScpExecutablePath
/// (mặc định: cạnh app binary). Mọi exception được nuốt + log như code cũ.
/// </summary>
public sealed class WinScpFileTransfer(
    IConfiguration configuration,
    IFileLogHelper fileLogHelper) : IFtpFileTransfer
{
    public void UploadZipFiles(string sourcePathFolder, string destPathFolderFtp,
        string ftpServer, string ftpUsername, string ftpPassword)
    {
        try
        {
            var files = Directory.GetFiles(sourcePathFolder, "*.zip");
            if (files.Length == 0) return;

            var sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = ftpServer,
                UserName = ftpUsername,
                Password = ftpPassword
            };

            using var session = new Session();
            var winScpExe = configuration["AppSettings:WinScpExecutablePath"];
            if (!string.IsNullOrEmpty(winScpExe))
                session.ExecutablePath = winScpExe;

            session.Open(sessionOptions);
            if (!session.Opened) return;

            var transferOptions = new TransferOptions { TransferMode = TransferMode.Binary };
            transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;

            if (!session.FileExists(destPathFolderFtp))
                session.CreateDirectory(destPathFolderFtp);

            foreach (var srcFile in files)
            {
                var transferResult = session.PutFiles(srcFile, destPathFolderFtp, false, transferOptions);
                if (!transferResult.IsSuccess) continue;

                try
                {
                    if (File.Exists(srcFile))
                        File.Delete(srcFile);
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteLogs($"[LogJob] File log job {srcFile} {JsonConvert.SerializeObject(ex)}");
                }
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"[LogJob] Upload file log job {JsonConvert.SerializeObject(ex)}");
        }
    }
}

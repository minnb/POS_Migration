using System.Net;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using POS.Common.Const;
using POS.Common.Dtos.FileModel;
using POS.Common.Helpers;
using POS.Infrastructure.Files;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Application.Features.DataSync;

/// <summary>
/// Migrated từ SyncDataPosController cũ (logic private GetFileFromServerAPI, UploadFileLogWinSCP,
/// CheckDeleteFileOld, DeleteFileExist) + UtilsAPI.DowloadFileUpgradeToolShareFolder.
/// Khác bản cũ:
/// - HostingEnvironment.MapPath → AppSettings:FtpRootPath (physical path của thư mục FTPBLUEPOS)
/// - MemoryCacheService.GetPOSDataSetup / CommonBLO.GetDataSetup → ICentralMDRepository.GetPOSDataSetupAsync (Redis cache)
/// - RedisCacheService queue → IRedisService.GetKeysByPattern
/// </summary>
public sealed class SyncDataPosService(
    ICentralMDRepository centralMDRepository,
    IRedisService redis,
    IFtpFileTransfer ftpFileTransfer,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IConfiguration configuration) : ISyncDataPosService
{
    private const string FtpRootName = "FTPBLUEPOS";

    private string FtpRootPath =>
        configuration["AppSettings:FtpRootPath"]
        ?? Path.Combine(AppContext.BaseDirectory, FtpRootName);

    public string MapFtpPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(FtpRootPath, normalized);
    }

    public string MapSitePath(string relativePath)
    {
        var siteRoot = Directory.GetParent(FtpRootPath.TrimEnd(Path.DirectorySeparatorChar))?.FullName
                       ?? FtpRootPath;
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(siteRoot, normalized);
    }

    public async Task<string?> GetPosDataSetupValueAsync(string code, CancellationToken ct = default)
    {
        var dataSetup = await centralMDRepository.GetPOSDataSetupAsync(ct);
        return dataSetup?.FirstOrDefault(x =>
            string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    public async Task<List<PathFileAPIModel>> GetFileFromServerApiAsync(
        string pathSync, string folderFile, string typeSync, string syncApi, string ipServer,
        CancellationToken ct = default)
    {
        var result = new List<PathFileAPIModel>();
        try
        {
            if (syncApi != "YES") return result;

            var linkApiDown = await GetPosDataSetupValueAsync("LINKAPI_DOWN", ct) ?? "";
            var folderShareAPIBluePOS = configuration["AppSettings:FolderShareAPIBluePOS"] ?? "";

            var pathFile = $@"{pathSync}\{typeSync}\{folderFile}";
            var pathFileIPServer = $@"{pathSync}\{typeSync}\{folderFile.Replace("/", @"\")}";
            var folderAPI = pathFile;
            var networkPathDisc = pathSync;
            var filePath = $"{linkApiDown}{FtpRootName}/syncdatapos/pos/{typeSync}/{folderFile}";

            if (typeSync == "ALL")
            {
                // dev-qas-uat: dùng share folder config thay IP server
                if (!string.IsNullOrEmpty(folderShareAPIBluePOS))
                    ipServer = folderShareAPIBluePOS;

                pathFile = MapFtpPath($"{pathSync}/{folderFile}");
                filePath = $"{linkApiDown}{FtpRootName}/{pathSync}/{folderFile}";
                pathFileIPServer = $@"\\{ipServer}\{FtpRootName}\{pathSync.Replace("/", @"\")}\{folderFile.Replace("/", @"\")}";
                networkPathDisc = $@"\\{ipServer}\{FtpRootName}";
                folderAPI = pathFileIPServer;
            }

            var getFilesZip = Directory.GetFiles(pathFile)
                .Select(Path.GetFileName)
                .Where(f => f != null && !f.StartsWith("~$") && f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .Select(f => f!)
                .ToList();

            if (getFilesZip.Count > 0)
            {
                fileLogHelper.WriteLogs($"GetFiles.pathFile: {getFilesZip.Count} files on {pathFile}");
                CheckDeleteFileOld(getFilesZip, pathFile);
                result.AddRange(getFilesZip.Select(item => new PathFileAPIModel
                {
                    FileName = item,
                    FilePath = $"{filePath}/{item}",
                    IPServer = ipServer,
                    PathFileIPServer = $@"{pathFileIPServer}\{item}",
                    NetworkPathDisc = networkPathDisc,
                    FolderAPI = folderAPI.Replace("/", @"\")
                }));
            }
        }
        catch (Exception ex)
        {
            kibanaService.LogException("GetFileFromFTP_CHANGE", folderFile, 0, "",
                $"GetFileFromFTP_CHANGE.GetFileFromServerAPI {pathSync}{folderFile} Exception: " + ex.Message);
        }
        return result;
    }

    public (bool Allowed, int Processing) CheckSodQueueLimit(string ipSrvKey, int limit)
    {
        try
        {
            var keys = redis.GetKeysByPattern($"{RedisConst.Redis_Key_GetFileFromFTP}*");
            var count = keys.Count(k => k.Contains(ipSrvKey));
            if (count > limit && limit > 0)
                return (false, count);
            return (true, count);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CheckSodQueueLimit", ex);
            return (true, 0);
        }
    }

    public bool SodGlobalMarkerExists()
        => redis.KeyExists($"{RedisConst.Redis_Key_GetFileFromFTP}:GetFileFromFTP");

    public string EnqueueSodRequest(string ipSrvKey, string posTerminal)
    {
        var key = StringHelper.InitRandomString(ipSrvKey);
        var fullKey = $"{RedisConst.Redis_Key_GetFileFromFTP}:{key}";
        redis.StringSet(fullKey, posTerminal, ttlSeconds: 600); // TTL 10 phút như cũ
        return fullKey;
    }

    public void DequeueSodRequest(string fullKey)
        => redis.Delete(fullKey);

    public async Task UploadFileLogToFtpAsync(string pathFileApi, string pathFtpServer, CancellationToken ct = default)
    {
        try
        {
            var ftpServer = await GetPosDataSetupValueAsync("FTPSERVER", ct) ?? "";
            var ftpUser   = await GetPosDataSetupValueAsync("FTPUSERNAME", ct) ?? "";
            var ftpPass   = await GetPosDataSetupValueAsync("FTPPASSWORD", ct) ?? "";

            var hasZip = Directory.GetFiles(pathFileApi)
                .Select(Path.GetFileName)
                .Any(f => f != null && !f.StartsWith("~$") && f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (!hasZip) return;

            ftpFileTransfer.UploadZipFiles(pathFileApi, pathFtpServer, ftpServer, ftpUser, ftpPass);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"[Log] Lỗi upload Log: {JsonConvert.SerializeObject(ex)}");
        }
    }

    public async Task<List<PathFileAPIModel>> DownloadFileUpgradeToolShareFolderAsync(string ipServer, CancellationToken ct = default)
    {
        var result = new List<PathFileAPIModel>();
        try
        {
            var rootFolderShare = configuration["AppSettings:FolderShareUpdSource"] ?? "";
            var linkApiDown = await GetPosDataSetupValueAsync("LINKAPI_DOWN", ct) ?? "";

            var pathFolderDownload = $"{FtpRootName}/BluePosUpgrade/Upgrade/UpgradeTool/";
            var dir = new DirectoryInfo($@"{rootFolderShare}\Upgrade\UpgradeTool");

            foreach (var fi in dir.GetFiles())
            {
                result.Add(new PathFileAPIModel
                {
                    FileName = fi.Name,
                    FilePath = $"{linkApiDown}{pathFolderDownload}{fi.Name}",
                    IPServer = ipServer,
                    PathFileIPServer = $@"{rootFolderShare}\Upgrade\UpgradeTool\{fi.Name}",
                    NetworkPathDisc = rootFolderShare,
                    FolderAPI = $@"{rootFolderShare}\Upgrade\UpgradeTool"
                });
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"[Download] Download file upgrade tool {JsonConvert.SerializeObject(ex)}");
        }
        return result;
    }

    public Task DeleteFileExistAsync(List<PathFileAPIModel> model, string ipServerHost)
    {
        if (model.Count == 0) return Task.CompletedTask;

        var first = model[0];
        var pathDisk = first.FolderAPI ?? "";
        var ipServer = first.IPServer;
        var filePath = first.FilePath ?? "";

        fileLogHelper.WriteLogs($"[DeleteFileExist] IPServerClient={ipServer}; IPServerHost={ipServerHost}");

        if (ipServer == ipServerHost)
        {
            // File nằm trên chính server này → map về FtpRootPath
            var indexOfLength = filePath.IndexOf(FtpRootName, StringComparison.Ordinal);
            if (indexOfLength >= 0)
            {
                var pathDelete = filePath.Substring(indexOfLength); // FTPBLUEPOS\SyncDataPos\...
                var filePathDel = MapFtpPath(pathDelete.Substring(FtpRootName.Length).Replace('\\', '/'));
                try
                {
                    File.Delete(filePathDel);
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteLogs($"[DeleteFileExist] Delete serverLocal file {JsonConvert.SerializeObject(ex)}");
                }
            }
        }
        else
        {
            var username = configuration["AppSettings:RemoteSvrUser"] ?? "";
            var password = configuration["AppSettings:RemoteSvrPass"] ?? "";
            var credentials = new NetworkCredential(username, password);

            using var _ = new ConnectToSharedFolder(pathDisk, credentials);
            var getListFiles = Directory.GetFiles(pathDisk)
                .Where(f => !f.StartsWith("~$"))
                .Select(Path.GetFileName)
                .OrderBy(d => d)
                .ToList();

            foreach (var file in model)
            {
                if (getListFiles.Any(d => d == file.FileName))
                {
                    try
                    {
                        File.Delete(file.PathFileIPServer!);
                    }
                    catch (Exception ex)
                    {
                        fileLogHelper.WriteLogs($"[DeleteFileExist] Delete server SharedFolder file {JsonConvert.SerializeObject(ex)}");
                    }
                }
            }
        }
        return Task.CompletedTask;
    }

    // Port từ CheckDeleteFileOld cũ — loại file cũ hơn 2 ngày khỏi danh sách + xóa khỏi disk
    private void CheckDeleteFileOld(List<string> getFilesZip, string pathFile)
    {
        try
        {
            for (var i = 0; i < getFilesZip.Count; i++)
            {
                if (CheckAndDeleteFile(Path.Combine(pathFile, getFilesZip[i]), 2))
                {
                    getFilesZip.Remove(getFilesZip[i]);
                }
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("CheckDeleteFileOld.Exception", ex);
        }
    }

    // Port từ FileHelper.CheckAndDeleteFile cũ — true nếu file cũ hơn numberDay và đã xóa
    private bool CheckAndDeleteFile(string filePath, int numberDay)
    {
        try
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > numberDay)
                {
                    fileInfo.Delete();
                    return true;
                }
                return false;
            }
            fileLogHelper.WriteLogs($"CheckAndDeleteFile: not found {filePath}");
            return false;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("FileHelper.CheckAndDeleteFile.Exception", ex);
            return false;
        }
    }
}

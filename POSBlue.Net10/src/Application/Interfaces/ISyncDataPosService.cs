using VCM.POSBLUE.Shared.DTOs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Service contract for the SyncDataPos use cases.
/// All methods mirror the legacy controller actions but return strongly typed results.
/// </summary>
public interface ISyncDataPosService
{
    Task<HttpResponseMessage> WriteFileByManualAsync(string storeNo, string posTerminal);
    Task<HttpResponseMessage> GetFileFromFTPAsync(string siteCode, string posTerminal, string folderFile, string pathSync, string syncAPI, string typeSync);
    Task<HttpResponseMessage> UploadFileLogJobAsync();
    Task<HttpResponseMessage> RetryProcessSalesAsync();
    Task<HttpResponseMessage> UploadFileSaleAsync();
    Task<HttpResponseMessage> DeleteFileFromAPIAsync(DeleteFileModel model);
    Task<HttpResponseMessage> GetFileScriptFromFTPAsync(string siteCode, string folderFile, string pathSync);
    Task<HttpResponseMessage> GetFileUpgradeToolFromFTPAsync(string pathSync);
    Task<HttpResponseMessage> DeleteFileFromRemoteAsync(string pathDisk, string filePath, string ipServer);
    Task<HttpResponseMessage> DeleteFileFromFTPAsync(string filePath);
    Task<HttpResponseMessage> DeleteFileExistAsync(List<PathFileAPIModel> model);
    Task<HttpResponseMessage> DownloadFileStreamAsync(string fileName, string pathDisk, string filePath);
    Task<HttpResponseMessage> ListFileAsync(string fullPath, string extension);
    Task<HttpResponseMessage> RetryProcessDataRawAsync();
}

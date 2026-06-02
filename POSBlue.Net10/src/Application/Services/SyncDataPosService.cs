using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Configuration;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Service implementation for the legacy SyncDataPosController.
/// The implementation focuses on the core workflow while stubbing out
/// heavy external dependencies (Kafka, SAP DLL, etc.).
/// </summary>
public class SyncDataPosService : ISyncDataPosService
{
    private readonly ISyncDataToPosClient _legacyClient;
    private readonly IDataRawService _rawService;
    private readonly ILogger<SyncDataPosService> _logger;
    private readonly SyncDataPosOptions _options;

    public SyncDataPosService(
        ISyncDataToPosClient legacyClient,
        IDataRawService rawService,
        ILogger<SyncDataPosService> logger,
        IOptions<SyncDataPosOptions> options)
    {
        _legacyClient = legacyClient;
        _rawService = rawService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HttpResponseMessage> WriteFileByManualAsync(string storeNo, string posTerminal)
    {
        // Directly forward to the legacy client (stub returns "OK").
        var result = await _legacyClient.WriteFileByManualAsync(storeNo, posTerminal, true, true);

        var response = new HttpResponseMessage(result == "OK" ? HttpStatusCode.OK : HttpStatusCode.BadRequest)
        {
            Content = new StringContent(result)
        };
        return response;
    }

    // The following methods are placeholders – they can be expanded later.
    public Task<HttpResponseMessage> GetFileFromFTPAsync(string siteCode, string posTerminal, string folderFile, string pathSync, string syncAPI, string typeSync)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> UploadFileLogJobAsync()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> RetryProcessSalesAsync()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> UploadFileSaleAsync()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> DeleteFileFromAPIAsync(DeleteFileModel model)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> GetFileScriptFromFTPAsync(string siteCode, string folderFile, string pathSync)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> GetFileUpgradeToolFromFTPAsync(string pathSync)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> DeleteFileFromRemoteAsync(string pathDisk, string filePath, string ipServer)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> DeleteFileFromFTPAsync(string filePath)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> DeleteFileExistAsync(System.Collections.Generic.List<PathFileAPIModel> model)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> DownloadFileStreamAsync(string fileName, string pathDisk, string filePath)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> ListFileAsync(string fullPath, string extension)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));

    public Task<HttpResponseMessage> RetryProcessDataRawAsync()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
}

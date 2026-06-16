using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Interfaces;
using POS.Common;
using POS.Common.Dtos.FileModel;
using POS.Common.Dtos.Request;
using POS.Common.Helpers;
using POS.Infrastructure.Logging;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Api.Controllers;

/// <summary>
/// Migrated từ VCM.POSBLUE.API.Controllers.SyncDataPosController (route api/posblue).
/// Sync file giữa API server và máy POS: SOD files, sale upload (Kafka), log upload, file delete/download.
///
/// LƯU Ý CONTRACT: hầu hết endpoint cũ trả qua ResponseData.HttpResponseData → HTTP status LUÔN 200,
/// status thật nằm trong body (field Status). Giữ nguyên behavior này — xem helper HttpResponseData bên dưới.
/// Riêng WriteFileByManual dùng Request.CreateResponse cũ → HTTP status thật.
///
/// WriteFileByManual gọi TichHopSAP.dll (net452) → STUB trả NotImplemented, chờ quyết định
/// HTTP-bridge sang app cũ hoặc decompile-port (xem plan).
/// GetFileFromFTP typeSync==ALL → dùng CreateFileSODFakeAsync trực tiếp, bỏ queue check.
/// </summary>
[Route("api/posblue")]
public sealed class SyncDataPosController(
    ISyncDataPosService syncDataPosService,
    IDataRawService dataRawService,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IConfiguration configuration,
    IServiceScopeFactory serviceScopeFactory
) : BaseController
{
    private const string StubSodMessage = "Chức năng tạo file SOD chưa được migrate (TichHopSAP.dll). Liên hệ dev team.";

    // ─── WriteFileByManual — STUB (TichHopSAP.dll) ───────────────────────────

    [HttpGet("WriteFileByManual")]
    public IActionResult WriteFileByManual([FromQuery] string? storeNo, [FromQuery] string? posTerminal)
    {
        if (string.IsNullOrEmpty(storeNo))
            return BadRequest(new ResultResponse { Message = "CH/ST đang bị trống", Status = HttpStatusCode.BadRequest });
        if (string.IsNullOrEmpty(posTerminal))
            return BadRequest(new ResultResponse { Message = "POSTerminal đang bị trống", Status = HttpStatusCode.BadRequest });

        // Code cũ: new SyncDataToPos().writeFileByManual(storeNo, posTerminal, true, true) — TichHopSAP.dll
        return StatusCode((int)HttpStatusCode.NotImplemented, new ResultResponse
        {
            Status = HttpStatusCode.NotImplemented,
            Message = StubSodMessage,
            MessageTechnical = "SyncDataToPos.writeFileByManual (TichHopSAP.dll net452) chưa migrate"
        });
    }

    // ─── GetFileFromFTP ──────────────────────────────────────────────────────

    [HttpGet("GetFileFromFTP")]
    public async Task<IActionResult> GetFileFromFTP(
        [FromQuery][Required(ErrorMessage = "siteCode is required")] string siteCode,
        [FromQuery][Required(ErrorMessage = "posTerminal is required")] string posTerminal,
        [FromQuery] string? folderFile,
        [FromQuery] string? pathSync,
        [FromQuery] string? syncAPI,
        [FromQuery] string? typeSync)
    {
        kibanaService.LogRequest($"GetFileFromFTP_{typeSync}", posTerminal,
            $"GetFileFromFTP: {siteCode}_{posTerminal}_{folderFile}_{pathSync}_{syncAPI}_{typeSync}");
        var ipServer = GetIpAddressClient();
        var listResult = new List<PathFileAPIModel>();
        try
        {
            var pathFile = syncDataPosService.MapFtpPath($"{pathSync}/{folderFile}");
            Directory.CreateDirectory(pathFile);

            if (typeSync == "ALL")
            {
                listResult = await syncDataPosService.GetFileFromServerApiAsync(
                    pathSync ?? "", folderFile ?? "", typeSync, syncAPI ?? "", ipServer);
                if (listResult.Count > 0)
                    return HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);

                var (sodCreated, sodMsg) = await dataRawService.CreateFileSODFakeAsync(siteCode, pathFile);
                kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", posTerminal, 0, "",
                    $"CreateFileSODFake {sodCreated} ({sodMsg}), siteCode {siteCode}");

                if (!sodCreated)
                    return HttpResponseData(HttpStatusCode.BadGateway,
                        $"Response from IPServer {ipServer}, CreateFileSODFake failed: {sodMsg}", null);

                await Task.Delay(500);
                listResult = await syncDataPosService.GetFileFromServerApiAsync(
                    pathSync ?? "", folderFile ?? "", typeSync, syncAPI ?? "", ipServer);
                return HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);
            }
            else // ChangeFirst hoặc Change
            {
                var rootFolderShare = configuration["AppSettings:FolderShare"] ?? "";
                if (syncAPI == "YES")
                {
                    listResult = await syncDataPosService.GetFileFromServerApiAsync(
                        rootFolderShare, folderFile ?? "", typeSync ?? "", syncAPI, ipServer);
                }
                kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", typeSync ?? "", 0, "",
                    $"Path: {rootFolderShare}\\{typeSync}\\{folderFile} listFileResult:{JsonConvert.SerializeObject(listResult)}");
                return HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);
            }
        }
        catch (Exception ex)
        {
            kibanaService.LogException("GetFileFromFTP_ALL", posTerminal, 0, "",
                $"GetFileFromFTP_ALL {pathSync}{folderFile} Exception: " + ex.Message);
            return HttpResponseData(HttpStatusCode.InternalServerError,
                $"Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})", null);
        }
    }

    // ─── UploadFileLogJob ────────────────────────────────────────────────────

    [HttpPost("UploadFileLogJob")]
    public async Task<IActionResult> UploadFileLogJob()
    {
        try
        {
            var form = await Request.ReadFormAsync();
            foreach (var file in form.Files)
            {
                // Tên field của form = tên folder (HttpContext.Request.Files.GetKey(i) cũ)
                var folder = file.Name;
                var pathFile = syncDataPosService.MapFtpPath($"SyncDataPos/General/LogJob/{folder}");
                Directory.CreateDirectory(pathFile);

                var fileSavePath = Path.Combine(pathFile, file.FileName);
                if (System.IO.File.Exists(fileSavePath))
                    System.IO.File.Delete(fileSavePath);
                await using (var fs = new FileStream(fileSavePath, FileMode.Create))
                    await file.CopyToAsync(fs);

                try
                {
                    if (string.Equals(configuration["AppSettings:UploadFileFTP"], "YES", StringComparison.OrdinalIgnoreCase))
                        await syncDataPosService.UploadFileLogToFtpAsync(pathFile, $"SyncDataPos/General/LogJob/{folder}/");
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteLogs($"UploadFileLogJob: Lỗi upload log job {JsonConvert.SerializeObject(ex)}");
                }
            }

            return HttpResponseData(HttpStatusCode.OK, "Success", "");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"UploadFileLogJob Error:{JsonConvert.SerializeObject(ex)}");
            return HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ─── RetryProcessSales ───────────────────────────────────────────────────

    [HttpGet("process/sales/retry")]
    public async Task<IActionResult> RetryProcessSales()
    {
        try
        {
            var pathFile = syncDataPosService.MapFtpPath("SyncDataPos/Sale/Kafka");
            var pathFileBackup = syncDataPosService.MapFtpPath("SyncDataPos/Sale/BackupFiles");
            Directory.CreateDirectory(pathFileBackup);

            if (!Directory.Exists(pathFile))
            {
                fileLogHelper.WriteLogs($"NotFound dir {pathFile}");
                return HttpResponseData(HttpStatusCode.BadRequest, $"NotFound dir {pathFile}", null);
            }

            var files = Directory.GetFiles(pathFile);
            fileLogHelper.WriteLogs($"Start retryProcessSales {files.Length} files ");
            foreach (var file in files)
            {
                fileLogHelper.WriteLogs($"processing fileName: {file}");
                await dataRawService.ProcessFileToStagingDBAsync(
                    pathFile, Path.GetFileName(file), Path.GetExtension(file), pathFileBackup);
            }
            return HttpResponseData(HttpStatusCode.OK, $"RetryProcessSales {files.Length} file Successfully", "");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs("RetryProcessSales.Exception: " + JsonConvert.SerializeObject(ex));
            return HttpResponseData(HttpStatusCode.BadRequest, ex.Message, null);
        }
    }

    // ─── UploadFileSale ──────────────────────────────────────────────────────

    [HttpPost("UploadFileSale")]
    public async Task<IActionResult> UploadFileSale()
    {
        var ipServer = GetIpAddressClient();
        var lstFile = new List<string>();
        var pathFileBackup = syncDataPosService.MapFtpPath("SyncDataPos/Sale/BackupFiles");
        Directory.CreateDirectory(pathFileBackup);
        try
        {
            var form = await Request.ReadFormAsync();
            foreach (var file in form.Files)
            {
                var pathFile = syncDataPosService.MapFtpPath("SyncDataPos/Sale/Kafka");
                Directory.CreateDirectory(pathFile);

                var fileSavePath = Path.Combine(pathFile, file.FileName);
                if (System.IO.File.Exists(fileSavePath))
                    System.IO.File.Delete(fileSavePath);
                await using (var fs = new FileStream(fileSavePath, FileMode.Create))
                    await file.CopyToAsync(fs);
                lstFile.Add(file.FileName);

                var extension = Path.GetExtension(fileSavePath);
                var fileName = Path.GetFileName(fileSavePath);

                // Fire-and-forget như Task.Run cũ — tạo DI scope riêng vì request scope
                // sẽ dispose ngay khi response trả về (tránh ObjectDisposedException).
                _ = Task.Run(async () =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var dataRaw = scope.ServiceProvider.GetRequiredService<IDataRawService>();
                    await dataRaw.ProcessFileToStagingDBAsync(pathFile, fileName, extension, pathFileBackup);
                });
            }

            kibanaService.LogRequest("UploadFileSale", ipServer, JsonConvert.SerializeObject(lstFile));
            return HttpResponseData(HttpStatusCode.OK, $"Updload file to IPServer {ipServer} Success", "");
        }
        catch (Exception ex)
        {
            kibanaService.LogException("UploadFileSale", ipServer, 0, "", JsonConvert.SerializeObject(ex));
            return HttpResponseData(HttpStatusCode.InternalServerError,
                $" Response IPServer:{ipServer}({JsonConvert.SerializeObject(ex)})", null);
        }
    }

    // ─── DeleteFileFromAPI ───────────────────────────────────────────────────

    [HttpPost("DeleteFileFromAPI")]
    public IActionResult DeleteFileFromAPI([FromBody] DeleteFileModel model)
    {
        try
        {
            // UrlServer ví dụ: SyncDataPos/POS/ALL/1535/153501 — relative dưới site root (như MapPath cũ)
            var combineLocal = $"{model.UrlServer}/{model.FileName}";
            var filePathDel = syncDataPosService.MapSitePath(combineLocal);

            if (System.IO.File.Exists(filePathDel))
            {
                try
                {
                    System.IO.File.Delete(filePathDel);
                    return HttpResponseData(HttpStatusCode.OK, "Success", "");
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteLogs($"DeleteFileFromAPI: Delete file {filePathDel} error {JsonConvert.SerializeObject(ex)}");
                    return HttpResponseData(HttpStatusCode.BadRequest, "Fail", $"Error {JsonConvert.SerializeObject(ex)}");
                }
            }

            fileLogHelper.WriteLogs($"DeleteFileFromAPI: Không tồn tại file {model.FileName} trên server API");
            return HttpResponseData(HttpStatusCode.BadRequest, $"Không tồn tại file {model.FileName} trên server API", "");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"DeleteFileFromAPI Error {JsonConvert.SerializeObject(ex)}");
            return HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ─── GetFileScriptFromFTP ────────────────────────────────────────────────

    [HttpGet("GetFileScriptFromFTP")]
    public IActionResult GetFileScriptFromFTP(
        [FromQuery] string? siteCode, [FromQuery] string? folderFile, [FromQuery] string? pathSync)
    {
        // Giữ nguyên behavior cũ: logic download đã bị comment trong code gốc — luôn trả list rỗng
        var listResult = new List<PathFileAPIModel>();
        try
        {
            return HttpResponseData(HttpStatusCode.OK, "Success", listResult);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"GetFileScriptFromFTP Error {JsonConvert.SerializeObject(ex)}");
            return HttpResponseData(HttpStatusCode.InternalServerError, JsonConvert.SerializeObject(ex), null);
        }
    }

    // ─── GetFileUpgradeToolFromFTP ───────────────────────────────────────────

    [HttpGet("GetFileUpgradeToolFromFTP")]
    public async Task<IActionResult> GetFileUpgradeToolFromFTP([FromQuery] string? pathSync)
    {
        // pathSync ví dụ: BluePosUpgrade/Upgrade/UpgradeTool
        var ipServer = GetIpServer();
        try
        {
            var pathFile = syncDataPosService.MapFtpPath(pathSync ?? "");
            Directory.CreateDirectory(pathFile);

            var listResult = await syncDataPosService.DownloadFileUpgradeToolShareFolderAsync(ipServer);
            return HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer} success", listResult);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"GetFileUpgradeToolFromFTP: Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})");
            return HttpResponseData(HttpStatusCode.InternalServerError,
                $"Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})", null);
        }
    }

    // ─── DeleteFileFromRemote ────────────────────────────────────────────────

    [HttpGet("DeleteFileFromRemote")]
    public IActionResult DeleteFileFromRemote(
        [FromQuery] string? pathDisk, [FromQuery] string? filePath, [FromQuery] string? ipServer)
    {
        // Phần xóa remote qua shared folder đã bị comment trong code cũ — chỉ giữ phần xóa local
        var ipServerHost = GetIpServer();
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return HttpResponseData(HttpStatusCode.OK, $"Delete file IPserver {ipServerHost} success", "");
            }

            fileLogHelper.WriteLogs($"DeleteFileFromRemote: Không tồn tại file xóa {filePath}");
            return HttpResponseData(HttpStatusCode.BadRequest,
                $"Delete file IPserver {ipServerHost}:{filePath} fail, do không tồn tại file trên FTP", "");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"DeleteFileFromRemote: Response Server {ipServerHost}({JsonConvert.SerializeObject(ex)})");
            return HttpResponseData(HttpStatusCode.BadRequest,
                $"Response Server {ipServerHost},Disk:{filePath}; PathFile:{filePath}; Error: {JsonConvert.SerializeObject(ex)}", null);
        }
    }

    // ─── DeleteFileFromFTP ───────────────────────────────────────────────────

    [HttpGet("DeleteFileFromFTP")]
    public IActionResult DeleteFileFromFTP([FromQuery] string? filePath)
    {
        var ipServerHost = GetIpServer();
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return HttpResponseData(HttpStatusCode.OK, $"Delete file IPserver {ipServerHost} success", "");
            }

            fileLogHelper.WriteLogs($"DeleteFileFromFTP: Không tồn tại file xóa {filePath}");
            return HttpResponseData(HttpStatusCode.BadRequest,
                $"Delete file IPserver {ipServerHost}:{filePath} fail, do không tồn tại file trên FTP", "");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"DeleteFileFromFTP: Response Server {ipServerHost}({JsonConvert.SerializeObject(ex)})");
            return HttpResponseData(HttpStatusCode.BadRequest,
                $"Response Server {ipServerHost},Disk:{filePath}; PathFile:{filePath}; Error: {JsonConvert.SerializeObject(ex)}", null);
        }
    }

    // ─── DeleteFileExist ─────────────────────────────────────────────────────

    [HttpPost("DeleteFileExist")]
    public async Task<IActionResult> DeleteFileExist([FromBody] List<PathFileAPIModel> model)
    {
        try
        {
            if (model is { Count: > 0 })
                await syncDataPosService.DeleteFileExistAsync(model, GetIpServer());
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"DeleteFileExist Error {JsonConvert.SerializeObject(ex)}");
            return HttpResponseData(HttpStatusCode.BadRequest, "Fail", JsonConvert.SerializeObject(ex));
        }
        return HttpResponseData(HttpStatusCode.OK, "Success", null);
    }

    // ─── DowloadFileStream ───────────────────────────────────────────────────

    [HttpGet("DowloadFileStream")]
    public async Task<IActionResult> DowloadFileStream(
        [FromQuery] string? fileName, [FromQuery] string? pathDisk, [FromQuery] string? filePath)
    {
        try
        {
            var st1 = Stopwatch.StartNew();
            if (!System.IO.File.Exists(filePath))
                return HttpResponseData(HttpStatusCode.BadRequest, $"File: {fileName} không tồn tại", null);

            var readFile = await System.IO.File.ReadAllBytesAsync(filePath!);
            st1.Stop();
            kibanaService.LogResponse("DowloadFileStream", GetIpServer(), st1.ElapsedMilliseconds, "",
                $"StatusCode:OK download file {filePath}{fileName}");
            return File(readFile, "application/x-zip-compressed", fileName);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("DowloadFileStream.Exception", ex);
            return HttpResponseData(HttpStatusCode.BadRequest,
                $"download {filePath}{fileName} BadRequest from server {GetIpServer()}({JsonConvert.SerializeObject(ex)})", null);
        }
    }

    // ─── ListFile ────────────────────────────────────────────────────────────

    [HttpGet("ListFile")]
    public IActionResult ListFile([FromQuery] string fullPath, [FromQuery] string extension)
    {
        var listResult = new List<ListFileNameModel>();
        try
        {
            var getFilesZip = Directory.GetFiles(fullPath)
                .Select(Path.GetFileName)
                .Where(f => f != null && !f.StartsWith("~$")
                    && (f.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase) || extension == "*"))
                .ToList();

            listResult.AddRange(getFilesZip.Select(item => new ListFileNameModel { FileName = item }));
            return HttpResponseData(HttpStatusCode.OK, "Success", listResult);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"ListFile Error {JsonConvert.SerializeObject(ex)}");
            return HttpResponseData(HttpStatusCode.InternalServerError, JsonConvert.SerializeObject(ex), null);
        }
    }

    // ─── RetryProcessDataRaw ─────────────────────────────────────────────────

    [HttpGet("RetryProcessDataRaw")]
    public async Task<IActionResult> GetRetryProcessDataRaw()
    {
        return HttpResponseData(HttpStatusCode.OK, "Success", await dataRawService.RetryInsDataRawToDBAsync());
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Thay ResponseData.HttpResponseData cũ: HTTP status LUÔN 200, status thật trong body.
    /// (Code cũ tạo new HttpResponseMessage() mặc định 200 OK, chỉ set Status vào body JSON.)
    /// </summary>
    private OkObjectResult HttpResponseData(HttpStatusCode status, string message, object? data)
        => Ok(new ResultResponse
        {
            Status = status,
            Message = message,
            Data = data
        });
}

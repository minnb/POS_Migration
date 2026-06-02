using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers
{
    /// <summary>
    /// New .NET Core controller replacing legacy SyncDataPosController.
    /// Implements the same 14 routes, delegating to ISyncDataPosService.
    /// </summary>
    [ApiController]
    [Route("api/posblue/[controller]")]
    public class SyncDataPosController : ControllerBase
    {
        private readonly ISyncDataPosService _service;

        public SyncDataPosController(ISyncDataPosService service) => _service = service;

        // 1. WriteFileByManual
        [HttpGet("WriteFileByManual")]
        public async Task<IActionResult> WriteFileByManual([FromQuery] string storeNo, [FromQuery] string posTerminal)
        {
            var response = await _service.WriteFileByManualAsync(storeNo, posTerminal);
            return StatusCode((int)response.StatusCode, response.Content);
        }

        // 2. GetFileFromFTP
        [HttpGet("GetFileFromFTP")]
        public IActionResult GetFileFromFTP([FromQuery] string siteCode,
            [FromQuery] string posTerminal,
            [FromQuery] string folderFile,
            [FromQuery] string pathSync,
            [FromQuery] string syncAPI,
            [FromQuery] string typeSync)
        {
            // Placeholder – service method not yet implemented.
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 3. UploadFileLogJob (POST with files)
        [HttpPost("UploadFileLogJob")]
        public async Task<IActionResult> UploadFileLogJob([FromForm] IFormFileCollection files)
        {
            // Forward to service stub – not yet implemented.
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 4. RetryProcessSales (GET)
        [HttpGet("process/sales/retry")]
        public async Task<IActionResult> RetryProcessSales()
        {
            var response = await _service.RetryProcessSalesAsync();
            return StatusCode((int)response.StatusCode, response.Content);
        }

        // 5. UploadFileSale (POST with files)
        [HttpPost("UploadFileSale")]
        public async Task<IActionResult> UploadFileSale([FromForm] IFormFileCollection files)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 6. DeleteFileFromAPI (POST)
        [HttpPost("DeleteFileFromAPI")]
        public async Task<IActionResult> DeleteFileFromAPI([FromBody] DeleteFileModel model)
        {
            var response = await _service.DeleteFileFromAPIAsync(model);
            return StatusCode((int)response.StatusCode, response.Content);
        }

        // 7. GetFileScriptFromFTP (GET)
        [HttpGet("GetFileScriptFromFTP")]
        public IActionResult GetFileScriptFromFTP([FromQuery] string siteCode, [FromQuery] string folderFile, [FromQuery] string pathSync)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 8. GetFileUpgradeToolFromFTP (GET)
        [HttpGet("GetFileUpgradeToolFromFTP")]
        public IActionResult GetFileUpgradeToolFromFTP([FromQuery] string pathSync)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 9. DeleteFileFromRemote (GET)
        [HttpGet("DeleteFileFromRemote")]
        public IActionResult DeleteFileFromRemote([FromQuery] string pathDisk, [FromQuery] string filePath, [FromQuery] string ipServer)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 10. DeleteFileFromFTP (GET)
        [HttpGet("DeleteFileFromFTP")]
        public IActionResult DeleteFileFromFTP([FromQuery] string filePath)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 11. DeleteFileExist (POST)
        [HttpPost("DeleteFileExist")]
        public IActionResult DeleteFileExist([FromBody] List<PathFileAPIModel> model)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 12. DowloadFileStream (GET)
        [HttpGet("DowloadFileStream")]
        public IActionResult DowloadFileStream([FromQuery] string fileName, [FromQuery] string pathDisk, [FromQuery] string filePath)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 13. ListFile (GET)
        [HttpGet("ListFile")]
        public IActionResult ListFile([FromQuery] string fullPath, [FromQuery] string extension)
        {
            return StatusCode((int)HttpStatusCode.NotImplemented);
        }

        // 14. RetryProcessDataRaw (GET)
        [HttpGet("RetryProcessDataRaw")]
        public async Task<IActionResult> RetryProcessDataRaw()
        {
            var response = await _service.RetryProcessDataRawAsync();
            return StatusCode((int)response.StatusCode, response.Content);
        }
    }
}

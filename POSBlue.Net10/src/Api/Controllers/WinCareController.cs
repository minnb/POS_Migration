using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.DTOs.WinCare;

namespace VCM.POSBLUE.Api.Controllers
{
    [ApiController]
    [Route("api/posblue/[controller]")]
    public class WinCareController : BaseController
    {
        private readonly IWinCareService _winCareService;

        public WinCareController(IWinCareService winCareService) => _winCareService = winCareService;

        // 1. WinPayAccumulation
        [HttpPost("v2/wincustomer/wpay/shopping-accumulate")]
        public async Task<IActionResult> WinPayAccumulation([FromBody] WinPayAccumulationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var result = await _winCareService.WinPayAccumulationAsync(request);
            return StatusCode((int)result.Status, result);
        }

        // 2. WinPayAccumulationV3
        [HttpPost("v3/wincustomer/wpay/shopping-accumulate")]
        public async Task<IActionResult> WinPayAccumulationV3([FromBody] WinPayAccumulationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var result = await _winCareService.WinPayAccumulationV3Async(request);
            return StatusCode((int)result.Status, result);
        }

        // 3. GenOspQRLogin
        [HttpPost("v2/wincustomer/wpay/ospqrlogin")]
        public async Task<IActionResult> GenOspQRLogin([FromBody] SentNotifyPOSRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var result = await _winCareService.RequestSentNotifyUserAsync(request);
            return Ok(new ResultResponse { Status = HttpStatusCode.OK, Message = result, Data = null });
        }

        // 4. GetInfoBarcode
        [HttpPost("v2/wincare/collect-money/barcode")]
        public IActionResult GetInfoBarcode([FromBody] SaleOrderWinPlusBarcode request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var result = _winCareService.GetInfoBarcode(request);
            return StatusCode((int)result.Status, result);
        }

        // 5. ConfirmCollectedCash
        [HttpPost("v2/wincare/collect-money/confirm")]
        public IActionResult ConfirmCollectedCash([FromBody] ConfirmCollectedCashRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var result = _winCareService.ConfirmCollectedCash(request);
            return StatusCode((int)result.Status, result);
        }

        // 6. RequestSentNotifyUserAsync (notify)
        [HttpPost("v2/wincare/notify")]
        public async Task<IActionResult> RequestSentNotifyUserAsync([FromBody] SentNotifyPOSRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResult.Error("Invalid input", (int)HttpStatusCode.BadRequest));
            var msg = await _winCareService.RequestSentNotifyUserAsync(request);
            return Ok(new ResultResponse { Status = HttpStatusCode.OK, Message = msg, Data = null });
        }
    }
}

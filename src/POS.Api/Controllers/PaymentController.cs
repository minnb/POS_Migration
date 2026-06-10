using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos.PartnerApi;
using POS.Application.Interfaces;
using POS.Infrastructure.Logging;

namespace POS.Api.Controllers;

/// <summary>
/// Migrate từ VCM.POSBLUE.API.Controllers.PaymentController.
/// Chỉ chứa các action liên quan GotIT + Urbox.
/// Coupon (CAP/FMV) và OneU chưa migrate — thêm sau khi có service tương ứng.
/// </summary>
[Authorize(AuthenticationSchemes = "BasicAuth")]
[Route("api/v2/partner")]
public sealed class PaymentController(
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IGotITService gotITService,
    IUrboxService urboxService
) : BaseController
{
    private readonly IKibanaService _kibanaService = kibanaService;
    private readonly IFileLogHelper _fileLogHelper = fileLogHelper;
    private readonly IGotITService _gotITService = gotITService;
    private readonly IUrboxService _urboxService = urboxService;

    /// <summary>Kiểm tra voucher đối tác trước khi thanh toán.</summary>
    [HttpPost]
    [Route("voucher/check")]
    public async Task<IActionResult> ValidateVoucher(
        [FromBody] CheckVoucherPartnerPOSRequest modelPOS,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        const string endpoint = "api/v2/partner/voucher/check";
        _kibanaService.LogRequest(endpoint, modelPOS.PosNo, JsonConvert.SerializeObject(modelPOS));

        try
        {
            switch (modelPOS.Partner.ToUpper())
            {
                case "URBOX":
                    var checkUrbox = await _urboxService.CheckSerialUrbox(modelPOS, ct);
                    sw.Stop();
                    _kibanaService.LogResponse(endpoint, modelPOS.PosNo, sw.ElapsedMilliseconds, "",
                        JsonConvert.SerializeObject(checkUrbox.Item3));
                    if (checkUrbox.Item1)
                        return OkResult(checkUrbox.Item3, checkUrbox.Item2);
                    return StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = checkUrbox.Item2,
                        Data = checkUrbox.Item3
                    });

                case "GOTIT":
                    var checkGotIt = await _gotITService.CheckMultiple(modelPOS, ct);
                    sw.Stop();
                    _kibanaService.LogResponse(endpoint, modelPOS.PosNo, sw.ElapsedMilliseconds, "",
                        JsonConvert.SerializeObject(checkGotIt.Item3));
                    if (checkGotIt.Item1)
                        return OkResult(checkGotIt.Item3, checkGotIt.Item2);
                    return StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = checkGotIt.Item2,
                        Data = checkGotIt.Item3
                    });

                default:
                    sw.Stop();
                    return BadRequestResult(
                        $"Tham số PartnerCode truyền vào không đúng {modelPOS.Partner}");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _fileLogHelper.WriteExpLogs("PaymentController.ValidateVoucher", ex);
            _kibanaService.LogException(endpoint, modelPOS.PosNo, 0, "", ex.Message);
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data = null,
                Message = $"Exception:{ex.Message}",
                Status = HttpStatusCode.InternalServerError,
                MessageTechnical = ex.Message
            });
        }
    }

    /// <summary>Xác nhận sử dụng / thanh toán voucher đối tác.</summary>
    [HttpPost]
    [Route("voucher/update-status")]
    public async Task<IActionResult> UpdateStatusVoucher(
        [FromBody] UpdateStatusVoucherPartnerRequest modelPOS,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        const string endpoint = "api/v2/partner/voucher/update-status";
        _kibanaService.LogRequest(endpoint, modelPOS.PosNo, JsonConvert.SerializeObject(modelPOS));

        try
        {
            switch (modelPOS.Partner.ToUpper())
            {
                case "URBOX":
                    var checkUrbox = await _urboxService.PayCodelUrbox(modelPOS, ct);
                    sw.Stop();
                    _kibanaService.LogResponse(endpoint, modelPOS.PosNo, sw.ElapsedMilliseconds, "",
                        JsonConvert.SerializeObject(checkUrbox.Item3));
                    if (checkUrbox.Item1)
                        return OkResult(checkUrbox.Item3, checkUrbox.Item2);
                    return StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = checkUrbox.Item2,
                        Data = checkUrbox.Item3
                    });

                case "GOTIT":
                    var checkGotIt = await _gotITService.MarkUseMultiple(modelPOS, ct);
                    sw.Stop();
                    _kibanaService.LogResponse(endpoint, modelPOS.PosNo, sw.ElapsedMilliseconds, "",
                        JsonConvert.SerializeObject(checkGotIt.Item3));
                    if (checkGotIt.Item1)
                        return OkResult(checkGotIt.Item3, checkGotIt.Item2);
                    return StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = checkGotIt.Item2,
                        Data = checkGotIt.Item3
                    });

                default:
                    sw.Stop();
                    return BadRequestResult(
                        $"Tham số PartnerCode truyền vào không đúng {modelPOS.Partner}");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _fileLogHelper.WriteExpLogs("PaymentController.UpdateStatusVoucher", ex);
            _kibanaService.LogException(endpoint, modelPOS.PosNo, 0, "", ex.Message);
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
            {
                Data = null,
                Message = $"Lỗi hệ thống:{ex.Message}",
                Status = HttpStatusCode.InternalServerError
            });
        }
    }
}

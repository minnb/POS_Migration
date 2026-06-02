using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Controller Common — GIỮ NGUYÊN route gốc "api/common/...".
/// KHÔNG chứa business logic: chỉ validate input → gọi ICommonService → trả ResultResponse.
/// (Bản mẫu phase này convert 2 endpoint đại diện: GetCurrentTime, CheckIPaddressPos.
///  Các endpoint còn lại của CommonController sẽ được convert theo cùng pattern.)
/// </summary>
[Route("api/common")]
public class CommonController : BaseController
{
    private readonly ICommonService _commonService;

    public CommonController(ICommonService commonService)
    {
        _commonService = commonService;
    }

    [HttpGet("GetCurrentTime")]
    public IActionResult GetCurrentTime()
    {
        try
        {
            return ApiResult(HttpStatusCode.OK, "Success", _commonService.GetCurrentTime());
        }
        catch (Exception ex)
        {
            Serilog.Log.Information("Request GetCurrentTime Exception: " + ex.Message);
            return ApiResult(HttpStatusCode.InternalServerError, "Lỗi hệ thống:" + ex.Message, null);
        }
    }

    [HttpGet("CheckIPaddressPos")]
    public async Task<IActionResult> CheckIPaddressPos([Required] string IPAddress)
    {
        try
        {
            if (string.IsNullOrEmpty(IPAddress))
                return ApiResult(HttpStatusCode.BadRequest, "IPAddress đang bị trống", null);

            var data = await _commonService.CheckIpAddressPosAsync(IPAddress);
            if (data == null)
                return ApiResult(HttpStatusCode.BadRequest,
                    $"Hiện tại chưa thiết lập POSTerminal cho IP {IPAddress}, Vui lòng liên hệ IT để được hỗ trợ", null);

            return ApiResult(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }
}

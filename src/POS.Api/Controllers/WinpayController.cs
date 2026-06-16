using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Common;
using System.Net;

namespace POS.Api.Controllers;

//[Authorize(AuthenticationSchemes = "BasicAuth")]
[Route("api/v2/winpay")]
public sealed class WinpayController : BaseController
{
    //winpay_2
    [HttpGet]
    [Route("get-register-info")]
    public IActionResult WinpayGetInfo([FromQuery] string phone)
    {
        // TODO: extract to helper
        try
        {
            if (phone.Length >= 11 && phone[..2] == "84")
                phone = "0" + phone[2..];
            else if (phone.Length == 9 && phone[0] != '0')
                phone = "0" + phone;
        }
        catch { }

        return StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse
        {
            Data = null,
            Message = "Không tồn tại",
            Status = HttpStatusCode.BadRequest,
            MessageTechnical = string.Empty,
        });
    }
}

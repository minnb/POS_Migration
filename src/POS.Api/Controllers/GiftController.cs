using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Gift;
using POS.Common;
using POS.Common.Dtos.CentralMD;
using System.Net;

namespace POS.Api.Controllers;

[Route("api")]
public sealed class GiftController(IGiftService giftService) : BaseController
{
    private readonly IGiftService _giftService = giftService;

    [HttpPost]
    [Route("v2/gifts/claim")]
    public async Task<IActionResult> ClaimMMLSchemaResponse([FromBody] MMLSchemeRequest request)
    {
        await Task.Delay(1);

        var result = new ResultResponse
        {
            Data = null,
            Message = "OK",
            Status = HttpStatusCode.NotFound,
            MessageTechnical = ""
        };

        return StatusCode((int)result.Status, result);
    }
}

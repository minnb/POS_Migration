using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Gift.DTOs;
using POS.Application.Gift.Services;
using POS.API.Filters;
using POS.Shared.Models;

namespace POS.API.Controllers;

[ApiController]
[ServiceFilter(typeof(BasicAuthFilter))]
[Produces("application/json")]
public class GiftController : ControllerBase
{
    private readonly IGiftService _service;
    private readonly ILogger<GiftController> _logger;

    public GiftController(IGiftService service, ILogger<GiftController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // POST api/v2/gifts/claim
    [HttpPost("api/v2/gifts/claim")]
    public async Task<IActionResult> ClaimMMLSchemaResponse([FromBody] MMLSchemeRequest request)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var (items, message, messageTechnical) = await _service.ClaimMMLSchemeAsync(request, cts.Token);

            if (items.Count > 0)
            {
                return Ok(new ResultResponse
                {
                    Status = (int)HttpStatusCode.OK,
                    Message = "Success",
                    Data = items,
                    MessageTechnical = string.Empty
                });
            }

            return StatusCode(404, new ResultResponse
            {
                Status = (int)HttpStatusCode.NotFound,
                Message = message,
                Data = null,
                MessageTechnical = messageTechnical
            });
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return StatusCode(408, new ResultResponse
            {
                Status = 408,
                Message = "Request timeout",
                Data = null,
                MessageTechnical = "Request exceeded the 25-second limit"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClaimMMLSchemaResponse failed {OrderNo}", request.OrderNo);
            return BadRequest(new ResultResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Message = ex.Message,
                Data = null,
                MessageTechnical = JsonConvert.SerializeObject(ex)
            });
        }
    }

    // POST api/pos/gift
    [HttpPost("api/pos/gift")]
    public async Task<IActionResult> PostGiftCheck([FromBody] GiftDataRequest request)
    {
        try
        {
            var (httpStatus, status, message, data) = await _service.PostGiftCheckAsync(request);

            var body = new ResultResponse
            {
                Status = status,
                Message = message,
                Data = data,
                MessageTechnical = string.Empty
            };

            return StatusCode(httpStatus, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostGiftCheck failed {PosNo}", request.PosNo);
            return BadRequest(new ResultResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Message = ex.Message,
                Data = null,
                MessageTechnical = JsonConvert.SerializeObject(ex)
            });
        }
    }
}

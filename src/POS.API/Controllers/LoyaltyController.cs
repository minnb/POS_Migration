using Microsoft.AspNetCore.Mvc;
using POS.API.Filters;
using POS.Application.Loyalty.DTOs;
using POS.Application.Loyalty.Services;

namespace POS.API.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
[ServiceFilter(typeof(BasicAuthFilter))]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<LoyaltyController> _logger;

    public LoyaltyController(ILoyaltyService loyaltyService, ILogger<LoyaltyController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    /// <summary>Lấy thông tin hội viên Capillary theo SĐT hoặc mã thẻ.</summary>
    [HttpGet("v2/loyalty/customer/get")]
    public async Task<IActionResult> GetCustomer(
        [FromQuery] string numberCard,
        [FromQuery] string posID,
        [FromQuery] string storeNo,
        [FromQuery] string clubCode = "",
        [FromQuery] bool isMobile = false,
        [FromQuery] bool isLog = true)
    {
        if (string.IsNullOrWhiteSpace(numberCard) || numberCard.Length < 9)
        {
            return StatusCode(400, new { Status = 400, Message = "numberCard phải có ít nhất 9 ký tự" });
        }

        var request = new GetCustomerRequest
        {
            NumberCard = numberCard,
            PosId = posID,
            StoreNo = storeNo,
            ClubCode = clubCode,
            IsMobile = isMobile,
            IsLog = isLog
        };

        var (statusCode, body) = await _loyaltyService.GetCustomerAsync(request);
        return StatusCode(statusCode, body);
    }
}

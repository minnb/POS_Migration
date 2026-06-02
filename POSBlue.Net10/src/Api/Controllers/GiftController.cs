using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Api.Models.Gift;

namespace VCM.POSBLUE.Api.Controllers
{
    [ApiController]
    [Route("api/posblue/[controller]")]
    public class GiftController : BaseController
    {
        private readonly IGiftService _giftService;

        public GiftController(IGiftService giftService) => _giftService = giftService;

        // POST api/pos/gift/claim (legacy route: api/v2/gifts/claim)
        [HttpPost]
        [Route("v2/gifts/claim")]
        public async Task<IActionResult> ClaimMMLScheme([FromBody] MMLSchemeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult(HttpStatusCode.BadRequest, "Invalid input", null));

            var result = await _giftService.ClaimMMLSchemeAsync(request);
            return StatusCode((int)result.Status, result);
        }

        // POST api/pos/gift (legacy route: api/pos/gift)
        [HttpPost]
        [Route("pos/gift")]
        public async Task<IActionResult> PostGiftCheck([FromBody] GiftDataRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult(HttpStatusCode.BadRequest, "Invalid input", null));

            var result = await _giftService.PostGiftCheckAsync(request);
            return StatusCode((int)result.Status, result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using POS.Application.Interfaces;
using POS.Common;
using POS.Common.Dtos.POS;
using POS.Common.Helpers;
using POS.Infrastructure.Logging;
using System.Net;

namespace POS.Api.Controllers
{
    [Route("api/v2/kafka")]
    public class KafkaController(
        IKafkaService kafkaService
    ) : BaseController
    {
        private readonly IKafkaService _kafkaService = kafkaService;
        [HttpPost("producer")]
        public async Task<ResultResponse> PostMessageToKafka([FromBody] List<KafkaMessageDto> kafkaMessageDtos)
        {
            if (!ModelState.IsValid)
                return ResponseHelper.Response(HttpStatusCode.BadRequest, "Invalid model state", ModelState, "");

            var authData = GetAuthData();
            if (authData == null)
            {
                return ResponseHelper.Response(HttpStatusCode.Unauthorized, "Lỗi xác thực webapi", ModelState, "Lỗi xác thực webapi");
            }

            return await _kafkaService.PushSalesToTopic(kafkaMessageDtos);
        }
    }
}

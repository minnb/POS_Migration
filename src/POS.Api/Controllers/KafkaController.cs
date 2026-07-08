using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Features.DataSync;
using POS.Common;
using POS.Common.Dtos.POS;
using POS.Common.Helpers;
using POS.Infrastructure.Logging;
using System.Net;
using System.Text;

namespace POS.Api.Controllers
{
    [Route("api/v2/kafka")]
    public class KafkaController(
        IKafkaService kafkaService,
        POS.Infrastructure.Logging.IFileLogHelper fileLogHelper
    ) : BaseController
    {
        private readonly IKafkaService _kafkaService = kafkaService;
        private readonly POS.Infrastructure.Logging.IFileLogHelper _fileLogHelper = fileLogHelper;

        [HttpPost("producer")]
        public async Task<ResultResponse> PostMessageToKafka([FromBody] List<KafkaMessageDto> kafkaMessageDtos)
        {
            LogClientRequest(_fileLogHelper, nameof(PostMessageToKafka), kafkaMessageDtos);

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

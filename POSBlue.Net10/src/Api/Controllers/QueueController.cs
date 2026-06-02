using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VCM.POSBLUE.Api.Models.Queue;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Controller Queue — GIỮ NGUYÊN route gốc "api/v2/..." (thuộc nhóm cần Basic Auth).
/// Thay static RabbitMQProducer/KibanaService bằng IMessageQueueService + ISmsService (inject).
/// </summary>
[Route("api/v2")]
public class QueueController : BaseController
{
    private readonly IMessageQueueService _queue;
    private readonly ISmsService _sms;

    public QueueController(IMessageQueueService queue, ISmsService sms)
    {
        _queue = queue;
        _sms = sms;
    }

    [HttpPost("sms/send")]
    public async Task<IActionResult> RabbitMQProducerSMS([FromBody] SmsSendRequest request)
    {
        if (!ModelState.IsValid) return ApiResult(HttpStatusCode.BadRequest, "Invalid model", null);
        try
        {
            await _sms.SendMessageSmsAsync(request.Content ?? "", request.MessageType ?? "Info", request.Subject ?? "");
            return ApiResult(HttpStatusCode.OK, "OK", null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Exception:{ex.Message}", null, JsonConvert.SerializeObject(ex));
        }
    }

    [HttpPost("rabbit/producer")]
    public async Task<IActionResult> ApiRabbitMQProducer([FromBody] RabbitMessageRequest request)
    {
        if (!ModelState.IsValid) return ApiResult(HttpStatusCode.BadRequest, "Invalid model", null);
        try
        {
            await _queue.SendMessageAsync(
                RabbitMqConstants.Queue_UpdateStatusVoucher,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                JsonConvert.SerializeObject(request));
            return ApiResult(HttpStatusCode.OK, "OK", null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Exception:{ex.Message}", null, JsonConvert.SerializeObject(ex));
        }
    }

    [HttpPost("rabbit/test")]
    public async Task<IActionResult> RabbitMQTest([FromBody] RabbitMessageRequest request)
    {
        if (!ModelState.IsValid) return ApiResult(HttpStatusCode.BadRequest, "Invalid model", null);
        try
        {
            await _queue.ProduceClusterAsync("queue_test", StringHelperRandom(), JsonConvert.SerializeObject(request));
            await Task.Delay(1000);
            var message = await _queue.ConsumeClusterAsync("queue_test", 3);
            if (message is { Count: > 0 })
                return ApiResult(HttpStatusCode.OK, "OK", message);

            return ApiResult(HttpStatusCode.BadRequest, "Lỗi không đọc được message từ rabbitMQ", null);
        }
        catch (Exception ex)
        {
            return ApiResult(HttpStatusCode.InternalServerError, $"Exception:{ex.Message}", null, JsonConvert.SerializeObject(ex));
        }
    }

    private static string StringHelperRandom() => $"test_{Guid.NewGuid():N}";
}

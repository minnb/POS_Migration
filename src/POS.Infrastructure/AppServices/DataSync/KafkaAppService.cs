using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos.POS;
using POS.Common.Helpers;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Repositories.Interfaces;
using System.Net;

namespace POS.Infrastructure.AppServices.DataSync;

public sealed class KafkaAppService(
    ICentralSaleRepository centralSaleRepository,
    IRabbitMQProducer rabbitMQProducer
) : IKafkaAppService
{
    public async Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos, CancellationToken ct = default)
    {
        try
        {
            var tasks = kafkaMessageDtos.Select(async message =>
            {
                var result = await centralSaleRepository.InInsertToTableByJson(
                    StringHelper.Left(message.TransactionId, 4),
                    StringHelper.Left(message.TransactionId, 6),
                    message.TransactionId,
                    message.Message ?? "", "WEB", ct);

                bool flag = result.Item1;

                if (!flag)
                {
                    await rabbitMQProducer.ProducerRabbtMQClusterAsync("pos_sales", JsonConvert.SerializeObject(message));
                }
            });

            await Task.WhenAll(tasks);

            return ResponseHelper.Response(HttpStatusCode.OK, "OK", null, "");
        }
        catch (Exception ex)
        {
            return ResponseHelper.Response(HttpStatusCode.BadRequest, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }
}

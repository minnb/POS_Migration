using POS.Common;
using POS.Common.Dtos.POS;
using POS.Infrastructure.AppServices.DataSync;

namespace POS.Application.Features.DataSync;

public class KafkaService(
    IKafkaAppService appService
) : IKafkaService
{
    public Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos)
        => appService.PushSalesToTopic(kafkaMessageDtos);
}

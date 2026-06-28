using POS.Common;
using POS.Common.Dtos.POS;

namespace POS.Application.Features.DataSync;

public interface IKafkaService
{
    Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos);
}

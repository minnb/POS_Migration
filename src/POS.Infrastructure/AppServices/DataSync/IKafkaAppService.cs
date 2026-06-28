using Microsoft.AspNetCore.Mvc;
using POS.Common;
using POS.Common.Dtos.POS;
using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Infrastructure.AppServices.DataSync
{
    public interface IKafkaAppService
    {
        Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos, CancellationToken ct = default);
    }
}

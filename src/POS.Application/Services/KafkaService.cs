using Microsoft.AspNetCore.Mvc;
using POS.Application.Interfaces;
using POS.Common;
using POS.Common.Dtos.POS;
using POS.Infrastructure.AppServices.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Application.Services
{
    public class KafkaService(
        IKafkaAppService appService
        ) : IKafkaService
    {
        public Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos)
            => appService.PushSalesToTopic(kafkaMessageDtos);
    }
}

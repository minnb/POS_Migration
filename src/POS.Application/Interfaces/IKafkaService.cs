using Microsoft.AspNetCore.Mvc;
using POS.Common;
using POS.Common.Dtos.POS;
using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Application.Interfaces
{
    public interface IKafkaService
    {
        Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos);
    }
}

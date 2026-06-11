using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos.POS;
using POS.Common.Helpers;
using POS.Infrastructure.AppServices.Interfaces;
using POS.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace POS.Infrastructure.AppServices
{
    public sealed class KafkaAppService(
        ICentralSaleRepository centralSaleRepository
        ) :IKafkaAppService
    {
        
    public async Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos, CancellationToken ct = default)
        {
            try
            {
                bool flag = false;
                foreach (var message in kafkaMessageDtos)
                {
                    var result = await centralSaleRepository.InInsertToTableByJson(StringHelper.Left(message.TransactionId,4), StringHelper.Left(message.TransactionId, 6), message.Message??"", ct);
                    flag = result.Item1;
                }

                return ResponseHelper.Response(HttpStatusCode.OK, "OK", null, "");
            }
            catch (Exception ex) 
            {
                return ResponseHelper.Response(HttpStatusCode.BadRequest, ex.Message, null, JsonConvert.SerializeObject(ex));
            }
        }
    }
}

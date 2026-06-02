using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/v2")]
    public class QueueController : BaseController
    {
        private readonly KibanaService _kibanaService;
        public QueueController()
        {
            _kibanaService = new KibanaService();
        }

        [HttpPost]
        [Route("sms/send")]
        public HttpResponseMessage RabbitMQProducerSMS([FromBody] SMSMessage request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                _kibanaService.SendMessageSMS(request.Content, request.MessageType, request.Subject);
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = $"OK",
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/sms/send", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("rabbit/producer")]
        public HttpResponseMessage ApiRabbitMQProducer([FromBody] RabbitMessageDto request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                RabbitMQProducer.SendMessage(RabitMQConst.Queue_UpdateStatusVoucher,DateTimeHelper.UnixTimestampString(), JsonConvert.SerializeObject(request));
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = $"OK",
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/rabbit/producer", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("rabbit/test")]
        public async Task<HttpResponseMessage> RabbitMQTest([FromBody] RabbitMessageDto request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                RabbitMQProducer.ProducerRabbtMQCluster(_kibanaService, "queue_test",StringHelper.InitRandomString("test"),JsonConvert.SerializeObject(request));
                await Task.Delay(1000);
                var message = RabbitMQProducer.ConsumerRabbtMQCluster(_kibanaService, "queue_test", 3);
                if(message!= null && message.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = message,
                        Message = $"OK",
                        Status = HttpStatusCode.OK
                    });
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi không đọc được message từ rabbitMQ",
                    Status = HttpStatusCode.BadRequest
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/rabbit/producer", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }
    }
}

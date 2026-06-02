using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;

namespace TCX.WebApiCore
{
    public static class HttpClientService
    {
        private static readonly string[] _messageType = new string[] { "Info", "Warning", "Error" };
        public static (bool, string) CallWebApiPartner(SysWebApiDto webApiDto, string function, string webMethod, string param, string bodyData)
        {
            try
            {
                long responseTime = 0;
                ServerErrorResponses responseErr = new ServerErrorResponses();
                var router = webApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (router == null)
                {
                    return (false, $"Chưa khai báo {function}");
                }
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "Basic UE9TOjk4NzY1NDMyMTA=" }
                };
                ApiCallHelper apiCallHelper = new ApiCallHelper(webApiDto.Host, 
                                                                router.Route + param, 
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                null,
                                                               webMethod,
                                                               bodyData,
                                                               45,
                                                               webApiDto.HttpProxy,
                                                               webApiDto.Bypasslist
                                                               );
                return (true, apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr));
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("CallWebApiPartner.Exception:", ex);
                return (false, ex.Message);
            }
        }
        public static async Task<(bool, string)> CallWebApiPartnerAsync(SysWebApiDto webApiDto, string function, string webMethod, string param, string bodyData)
        {
            try
            {
                ServerErrorResponses responseErr = new ServerErrorResponses();
                var router = webApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (router == null)
                {
                    return (false, $"Chưa khai báo {function}");
                }
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "Basic UE9TOjk4NzY1NDMyMTA=" }
                };
                ApiCallHelper apiCallHelper = new ApiCallHelper(webApiDto.Host,
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                null,
                                                               webMethod,
                                                               bodyData,
                                                               45,
                                                               webApiDto.HttpProxy,
                                                               webApiDto.Bypasslist
                                                               );
                var response = await apiCallHelper.HttpClientWithApiAsync();
                return (true, response.Item1);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CallWebApiPartner.Exception:", ex);
                return (false, ex.Message);
            }
        }
        public static (bool, string) SendMessageSMS(string content, string notify = "Info", string subject = "", string messageId = "", string text = "", string appCode = "BLUEPOS")
        {
            try
            {
                if (!_messageType.Contains(notify)) 
                {
                    notify = "Info";
                }
                if (string.IsNullOrEmpty(subject))
                {
                    subject = $"No subject on server {ComputerHelper.GetIpServer()}";
                }

                if (string.IsNullOrEmpty(messageId))
                {
                    messageId = $"{ComputerHelper.GetIpServer()}_{DateTimeHelper.UnixTimestampString()}";
                }
                var mess = new SMSMessage
                {
                    AppCode = appCode,
                    MessageType = notify,
                    Subject = $"{subject}",
                    Content = $"🕒 Time: **{DateTime.Now:dd-MM-yyyy HH:mm:ss} on srv {ComputerHelper.GetIpServer()}** ℹ️ {content}",
                    Text = text
                };
                _ = Task.Run(() =>
                {
                    try
                    {
                        RabbitMQProducer.SendMessage(RabitMQConst.Queue_SMS, ComputerHelper.GetIpServer(), JsonConvert.SerializeObject(mess), true, 10);
                    }
                    catch (Exception ex)
                    {
                        FileHelper.WriteLogs($"SendMessageSMS.RabbitMQProducer.Exception: {JsonConvert.SerializeObject(ex)}");
                    }
                });
                return (true, "OK");
            }
            catch (Exception e)
            {
                return (false, $"SendMessageSMS.Exception: {JsonConvert.SerializeObject(e)}");
            }
        }
    }
}

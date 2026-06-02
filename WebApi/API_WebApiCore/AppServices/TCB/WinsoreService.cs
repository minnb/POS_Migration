using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Dtos.Loyalty.WinScore;
using TCX.API.Common.Dtos;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Shared;
using System.Threading.Tasks;
using TCX.API.Common.Shared;
using System.Threading;
using StackExchange.Redis;

namespace TCX.WebApiCore.AppServices.TCB
{
    public class WinsoreService
    {
        private readonly string _winscoreToken = $"Winscore:TokenV2";
        private readonly string _winscoreRedisCache = $"Winscore";
        private readonly MemoryCacheService _memoryCacheService;
        private readonly KibanaService _kibanaService;
        private readonly IDatabase dbRedis;
        public WinsoreService()
        {
            _memoryCacheService = new MemoryCacheService();
            _kibanaService = new KibanaService();
            dbRedis = RedisManager.GetDatabase();
        }
        public (HttpStatusCode, string, object) UpdateStatusWinscoreAsync(string posNo, string phoneNumber, int status)
        {
            long processTime = 0;
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.WINSCORE.ToString().ToUpper() && x.Blocked == false).FirstOrDefault();
            if (sysWebApiDto == null)
            {
                return (HttpStatusCode.NotFound, $"Winscore chưa cấu hình webapi", null);
            }
            try
            {
                var token = GetTokenWinscore(dbRedis, sysWebApiDto, posNo, phoneNumber);
                if (!token.Item1)
                {
                    GetTokenWinscore(dbRedis, sysWebApiDto, posNo, phoneNumber, true);
                    return (HttpStatusCode.BadRequest, "Error getToken Winscore", null);
                }

                var Route = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateStatus".ToUpper());
                var bodyJson = new UpdateStatusWinscore()
                {
                    win_membership = phoneNumber,
                    updated_status = status.ToString(),
                    store_id = StringHelper.Left(posNo, 4),
                    pos_id = posNo
                };
                _kibanaService.LogRequest(sysWebApiDto.Host + Route.Route, posNo, "", $"{phoneNumber}-request: {JsonConvert.SerializeObject(bodyJson)}");
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    {"Ocp-Apim-Subscription-Key", Route.Notes},
                    {"Authorization", "Bearer " + token.Item2}
                };
                var api = new RestSharpApiHelper
                    (
                        "WINSCORE",
                        sysWebApiDto.Host,
                        Route.Route,
                        "POST",
                        headers,
                        null,
                        bodyJson,
                        int.Parse(sysWebApiDto.Version),
                        sysWebApiDto.HttpProxy,
                        9090
                    );
                var response = api.InteractWithApi(ref httpStatusCode, ref processTime);
                _kibanaService.LogResponse(sysWebApiDto.Host + Route.Route, posNo, processTime, "", $"{phoneNumber} updateStatus:{status} ==> {httpStatusCode} with content:" + response);
                if (!string.IsNullOrEmpty(response) && httpStatusCode == HttpStatusCode.OK)
                {
                    return (HttpStatusCode.OK, "Success", null);
                }
                else if (!string.IsNullOrEmpty(response) && response.Contains("408_RequestTimeout"))
                {
                    HttpClientService.SendMessageSMS(MessageHelper.TemplateSMS($"Lỗi WebApi: {sysWebApiDto.Host + Route.Route}{Environment.NewLine}", response), TelegramEnum.WebApi.ToString());
                }
                else if((httpStatusCode == HttpStatusCode.BadGateway || httpStatusCode == HttpStatusCode.InternalServerError) && !string.IsNullOrEmpty(phoneNumber))
                {
                    dbRedis.StringSet(GetKeyWinscore(phoneNumber), JsonConvert.SerializeObject(new {PosNo = posNo, PhoneNumber = phoneNumber, Status = status }), TimeSpan.FromHours(6));
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    dbRedis.StringSet(GetKeyWinscore(phoneNumber), JsonConvert.SerializeObject(new { PosNo = posNo, PhoneNumber = phoneNumber, Status = status }), TimeSpan.FromHours(1));
                }
                return (HttpStatusCode.Forbidden, ex.Message, null);
            }
            return (httpStatusCode, httpStatusCode.ToString(), null);
        }
        public  (HttpStatusCode, string, int) GetStatusWinscoreAsync(string posNo, string phoneNumber)
        {
            long processTime = 0;
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.WINSCORE.ToString().ToUpper() && x.Blocked == false).FirstOrDefault();
            if (sysWebApiDto == null)
            {
                return (HttpStatusCode.NotFound, $"Winscore chưa cấu hình webapi", 0);
            }
            try
            {
                var token = GetTokenWinscore(dbRedis, sysWebApiDto, posNo, phoneNumber);
                if(!token.Item1)
                {
                    GetTokenWinscore(dbRedis, sysWebApiDto, posNo, phoneNumber);
                    return (HttpStatusCode.BadRequest, "Error getToken Winscore", 0);
                }

                var Route = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetStatus".ToUpper());
                var bodyJson = new { win_membership = phoneNumber };
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    {"Ocp-Apim-Subscription-Key", Route.Notes},
                    {"Authorization", "Bearer " + token.Item2}
                };
                int timeOut = 20;
                if (!string.IsNullOrEmpty(sysWebApiDto.Version))
                {
                    timeOut = int.Parse(sysWebApiDto.Version);
                }
                var api = new RestSharpApiHelper
                    (
                        "WINSCORE",
                        sysWebApiDto.Host,
                        Route.Route,
                        "POST",
                        headers,
                        null,
                        bodyJson,
                        timeOut,
                        sysWebApiDto.HttpProxy,
                        9090
                    );
                HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;
                var response = api.InteractWithApi(ref httpStatusCode, ref processTime);
                _kibanaService.LogResponse(sysWebApiDto.Host + Route.Route, posNo, processTime, "",$"{phoneNumber}-{httpStatusCode}-response: {response}");
                if (!string.IsNullOrEmpty(response) && httpStatusCode == HttpStatusCode.OK)
                {
                    var resultResponse = ConvertHelper.ToObject<CheckStatusWinscore>(response);
                    if (resultResponse != null)
                    {
                        return (HttpStatusCode.OK, resultResponse.Display_message, resultResponse.Is_qualified);
                    }
                    else
                    {
                        return (HttpStatusCode.BadRequest, response, 9999);
                    }
                }
                else if (!string.IsNullOrEmpty(response) && response.Contains("408_RequestTimeout"))
                {
                    HttpClientService.SendMessageSMS(MessageHelper.TemplateSMS($"Lỗi WebApi: {sysWebApiDto.Host + Route.Route}{Environment.NewLine}", response), TelegramEnum.WebApi.ToString());
                }

                return (httpStatusCode, httpStatusCode.ToString(), 0);
            }
            catch(Exception ex)
            {
                _kibanaService.LogException("GetStatusWinscoreAsync", posNo, 0, "", $"{phoneNumber} GetStatusWinscore.Exception: {JsonConvert.SerializeObject(ex)}");
                return (HttpStatusCode.Forbidden, ex.Message, 0);
            }
        }
        public void RetryUpdateStatusWinscore()
        {
            try
            {
                var redisDB = new RedisManager();
                var keys = redisDB.GetAllKeys(_winscoreRedisCache, _winscoreToken);
                if(keys != null && keys.Count > 0)
                {
                    foreach (var key in keys)
                    {
                        var dataRetry = redisDB.GetCacheValueToObject<UpdateStatusWinscoreDto>(key);
                        if(dataRetry != null)
                        {
                            var result = UpdateStatusWinscoreAsync(dataRetry.PosNo, dataRetry.PhoneNumber, dataRetry.Status);
                            FileHelper.WriteLogs($"RetryUpdateStatusWinscore: {key} status: {dataRetry.Status} result: {result.Item1}");
                            if (result.Item1 == HttpStatusCode.OK)
                            {
                                redisDB.Delete(key);
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch(Exception e)
            {
                FileHelper.WriteExpLogs("RetryUpdateStatusWinscore.Exception", e);
            }
        }
        private  (bool, string) GetTokenWinscore(IDatabase dbRedis, SysWebApiDto sysWebApiDto, string posNo, string phoneNumber = "", bool isDeleteCache = false)
        {
            long processTime = 0;
            try
            {
                if(isDeleteCache)
                {
                    dbRedis.KeyDelete(_winscoreToken);
                    Task.Delay(10);
                }
                var token = dbRedis.StringGet(_winscoreToken);
                if (!token.IsNullOrEmpty)
                {
                    return (true, token);
                }

                var endPoint = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetToken".ToUpper()).Route;
                string url = sysWebApiDto.Host + endPoint;
                _kibanaService.LogRequest(url, posNo, "", $"{phoneNumber}-GetToken-{sysWebApiDto.HttpProxy}");

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    {"Content-Type", "application/x-www-form-urlencoded"},
                    {"Ocp-Apim-Subscription-Key", sysWebApiDto.PrivateKey}
                };
                Dictionary<string, string> parameter = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", sysWebApiDto.UserName},
                    {"client_secret", sysWebApiDto.Password},
                    {"scope", sysWebApiDto.PublicKey }
                };

                var api = new RestSharpApiHelper
                    (
                        "WINSCORE",
                        sysWebApiDto.Host,
                        endPoint,
                        "POST",
                        headers,
                        parameter,
                        null,
                        int.Parse(sysWebApiDto.Version),
                        sysWebApiDto.HttpProxy,
                        9090
                );

                HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;
                var response = api.InteractWithApi(ref httpStatusCode, ref processTime);
                _kibanaService.LogResponse(url, posNo, processTime, "", $"{phoneNumber}-{httpStatusCode}-response: {response}");
                if (!string.IsNullOrEmpty(response) && httpStatusCode == HttpStatusCode.OK)
                {
                    var resultResponse = ConvertHelper.ToObject<TokenWinscore>(response);
                    if (resultResponse != null)
                    {
                        dbRedis.StringSet(_winscoreToken, resultResponse.Access_token, TimeSpan.FromSeconds(resultResponse.Expires_in - 60));
                        return (true, resultResponse.Access_token);
                    }
                    else
                    {
                        _kibanaService.LogResponse(url, posNo, 0,"",$"Lỗi convert dataJson {response}");
                        return (false, $"Winscore get token error: {response}");
                    }
                }
                else if(!string.IsNullOrEmpty(response) && response.Contains("408_RequestTimeout"))
                {
                    HttpClientService.SendMessageSMS(MessageHelper.TemplateSMS($"408_RequestTimeout: {url}{Environment.NewLine}", response), TelegramEnum.WebApi.ToString());
                }
                return (false, httpStatusCode.ToString());
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetTokenWinscore.Exception", "", 0, "", JsonConvert.SerializeObject(ex));
                HttpClientService.SendMessageSMS(MessageHelper.TemplateSMS($"Lỗi Exception.WebApi GetTokenWinscore", ex.Message), TelegramEnum.WebApi.ToString());
                return (false, ex.Message);
            }
        }
        private string GetKeyWinscore(string phoneNumber)
        {
            return _winscoreRedisCache + ":" + phoneNumber;
        }
    }
}

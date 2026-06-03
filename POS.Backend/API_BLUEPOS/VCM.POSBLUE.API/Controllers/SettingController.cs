
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty.WinCode;
using TCX.API.Common.Enums;
using TCX.API.Common.Shared;
using TCX.API.Common.Utils;
using TCX.WebApiCore;
using TCX.WebApiCore.DbContext;
using VCM.POSBLUE.API.Models;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/v2")]
    public class SettingController : BaseController
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly KibanaService _kibanaService;
        private readonly RedisCacheService _redisCacheService;
        private readonly RedisManager _redisManager;
        public SettingController()
        {
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _redisCacheService = new RedisCacheService();
            _redisManager = new RedisManager();
        }

        [HttpGet]
        [Route("setting/test/redis")]
        public async Task<HttpResponseMessage> TestSentinelRedisAsync()
        {
            try
            {
                var check = await _redisManager.GetIsOfflineCapillary(AppCodeEnum.IsOfflineCapillary.ToString());
                return ResponseData.HttpResponseData(HttpStatusCode.OK, check, null);
            }
            catch(Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.Conflict, "Conflict " + ex.Message.ToString(), ex);
            }
        }

        [HttpGet]
        [Route("setting/cache/in-memory-all")]
        public HttpResponseMessage MemoryCacheAll()
        {
            return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", _memoryCacheService.GetAllMemoryCache<string>());
        }

        [HttpPost]
        [Route("setting/cache/in-memory")]
        public HttpResponseMessage MemoryCacheDeleteAll()
        {
            return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", _memoryCacheService.DeleteMemoryCache(""));
        }

        [HttpDelete]
        [Route("setting/cache/in-memory-delete/{key}")]
        public HttpResponseMessage MemoryCacheDeleteByKey([Required] string key)
        {
            return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", _memoryCacheService.DeleteMemoryCache(key));
        }

        [HttpDelete]
        [Route("cache/redis/{pos}/delete/{key}")]
        public HttpResponseMessage RedisCacheDeleteByKey([Required] string pos, [Required] string key)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                _kibanaService.LogRequest("cache/redis/delete", pos, "", JsonConvert.SerializeObject(key));
                var redisHelper = new RedisConnectionHelper();
                if (redisHelper.Delete(EncryptionHelper.Base64Decode(key)))
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", null);
                }
                return ResponseData.HttpResponseData(HttpStatusCode.NotFound, "NotFound", null);
            }
            catch (Exception ex) 
            {
                return ResponseData.HttpResponseData(HttpStatusCode.Conflict, "Conflict " + ex.Message.ToString(), ex);
            }
        }

        [HttpPost]
        [Route("cache/redis/key/create")]
        public HttpResponseMessage CreateKeyToRedis(CreateKeyRedisDto request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                _kibanaService.LogRequest("cache/redis/key/create", request.AppCode, "", JsonConvert.SerializeObject(request));

                if(_redisCacheService.Set<string>(request.Key, request.Value, request.Prefix, TimeSpan.FromSeconds(request.ExpTime)) != null)
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", null);
                }
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "BadRequest", null);
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.ExpectationFailed, "ExpectationFailed " + ex.Message.ToString(), ex);
            }
        }
    }
}
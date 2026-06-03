using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore
{
    public class CXService
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly KibanaService _kibanaService;
        private readonly string _connectStringDb = AppGlobals.ConnStrCentralMD;
        public CXService() 
        {
            _memoryCacheService = new MemoryCacheService();
            _kibanaService = new KibanaService();
        }
        public Tuple<bool, string, object> GenerateOTP(string posNo, string phoneNumber, string action)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.CX.ToString().ToUpper() && x.Blocked == false).FirstOrDefault();
            if (sysWebApiDto == null) return new Tuple<bool, string, object>(false, API.Common.Constants.MessConst.NotFounDataConfig, null);
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GenerateOTP".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                if (string.IsNullOrEmpty(action))
                {
                    action = CXEnum.WIN_MEMBER_REGISTER.ToString();
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-API-KEY", sysWebApiDto.UserName }
                };
                GenerateOTPDto request = new GenerateOTPDto()
                {
                    PhoneNumber = phoneNumber,
                    MerchantId = router.Notes,
                    Action = action
                };
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                null,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(request),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                _kibanaService.LogRequest(sysWebApiDto.Host + router.Route, posNo, "", phoneNumber + @"@" + StringHelper.ObjectToStringLowercase(request));
                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                _kibanaService.LogResponse(sysWebApiDto.Host + router.Route, posNo, responseTime,"", phoneNumber + @"@" + response);
                _kibanaService.LoggingToLoyaltyDB(posNo, router.Route, "GenerateOTP", responseTime, phoneNumber, phoneNumber, JsonConvert.SerializeObject(request), response, phoneNumber);
                
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<CXResponse>(response);

                    return new Tuple<bool, string, object>(true, "OK", dataResponse);
                }
                else
                {
                    return new Tuple<bool, string, object>(false, response, responseErr);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), responseErr);
            }
        }
        public Tuple<bool, string, object> VerifyOTP(string posNo, string phoneNumber, string otp, string action)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            if (IsCheckNoneOtp(phoneNumber))
            {
                return new Tuple<bool, string, object>(true, "Số điện thoại " + phoneNumber + " không cần check OTP", null);
            }
            if (string.IsNullOrEmpty(otp))
            {
                return new Tuple<bool, string, object>(false, "Vui lòng nhập số Otp", null);
            }
            
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.CX.ToString().ToUpper() && x.Blocked == false).FirstOrDefault();
            if (sysWebApiDto == null) return new Tuple<bool, string, object>(false, API.Common.Constants.MessConst.NotFounDataConfig, null);
            try
            {

                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "VerifyOTP".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-API-KEY", sysWebApiDto.UserName }
                };
                VerifyOTPDto request = new VerifyOTPDto()
                {
                    PhoneNumber = phoneNumber,
                    Otp = otp, 
                    Action = action
                };
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                null,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(request),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );
                _kibanaService.LogRequest(sysWebApiDto.Host + router.Route, posNo, "", phoneNumber + @"@" + StringHelper.ObjectToStringLowercase(request));
                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                _kibanaService.LogResponse(sysWebApiDto.Host + router.Route, posNo, responseTime, "", phoneNumber + @"@" + response);
                _kibanaService.LoggingToLoyaltyDB(posNo, router.Route, "VerifyOTP", responseTime, phoneNumber, phoneNumber, JsonConvert.SerializeObject(request), response, phoneNumber);
                
                if (!string.IsNullOrEmpty(response))
                {
                    if(response == HttpStatusCode.Conflict.ToString())
                    {
                        return new Tuple<bool, string, object>(false, HttpStatusCode.Conflict.ToString(), null);
                    }

                    var dataResponse = ConvertHelper.ToObject<CXResponse>(response);
                    if (dataResponse != null && dataResponse.Data != null)
                    {
                        var otpResult = JsonConvert.DeserializeObject<VerifyOTPData>(JsonConvert.SerializeObject(dataResponse.Data));
                        if(otpResult != null && otpResult.IsValid)
                        {
                            return new Tuple<bool, string, object>(true, "OK", otpResult);
                        }
                    }
                    return new Tuple<bool, string, object>(false, "Số OTP không hợp lệ", dataResponse);
                }
                else
                {
                    return new Tuple<bool, string, object>(false, response, responseErr);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), responseErr);
            }
        }
        public bool IsCheckNoneOtp(string phoneNumber)
        {
            try
            {
                var lstMobileCarrier = StringHelper.ConverStringToArray(_memoryCacheService.GetPOSDataSetup(API.Common.Constants.RedisConst.Redis_Key_WWW_NONE_OTP.ToString()).Value, ';');
                phoneNumber = StringHelper.Left(phoneNumber, 3);
                if (lstMobileCarrier.Length > 0 && lstMobileCarrier.Contains(phoneNumber))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("IsCheckNoneOtp", ex);
                return false;
            }
        }
    }
}
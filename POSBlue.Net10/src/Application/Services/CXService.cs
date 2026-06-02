using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Enums;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Port CXService cũ — gọi CrownX OTP qua IApiCallClient, cấu hình route lấy từ IMemoryCacheService (SysWebApi).
/// Giữ nguyên logic; thay KibanaService→IRequestLogger, ApiCallHelper→IApiCallClient.
/// (LoggingToLoyaltyDB cũ là audit-log phụ ghi DB — sẽ bổ sung khi migrate ILoyaltyDbLogger.)
/// </summary>
public sealed class CXService : ICXService
{
    private readonly IMemoryCacheService _memoryCache;
    private readonly IApiCallClient _apiClient;
    private readonly IRequestLogger _logger;

    public CXService(IMemoryCacheService memoryCache, IApiCallClient apiClient, IRequestLogger logger)
    {
        _memoryCache = memoryCache;
        _apiClient = apiClient;
        _logger = logger;
    }

    private SysWebApiDto? GetCxConfig() =>
        _memoryCache.GetSysWebApi(_memoryCache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, AppCodeEnum.CX.ToString(), StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    public async Task<CxOtpResult> GenerateOTPAsync(string posNo, string phoneNumber, string action)
    {
        phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
        var cfg = GetCxConfig();
        if (cfg == null) return new CxOtpResult(false, MessConst.NotFounDataConfig, null);
        try
        {
            var router = cfg.SysWebApiRoute?.FirstOrDefault(x => string.Equals(x.Name, "GenerateOTP", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
            if (router == null) return new CxOtpResult(false, MessageConst.ApiNotFounDataConfig, null);

            if (string.IsNullOrEmpty(action)) action = CXEnum.WIN_MEMBER_REGISTER.ToString();

            var request = new GenerateOTPDto { PhoneNumber = phoneNumber, MerchantId = router.Notes, Action = action };
            var apiReq = new ApiCallRequest
            {
                Host = cfg.Host ?? "", Endpoint = router.Route ?? "",
                Headers = new Dictionary<string, string> { { "X-API-KEY", cfg.UserName ?? "" } },
                TokenType = TokenTypeEnum.Basic.ToString(), WebMethod = "POST",
                JsonData = StringHelper.ObjectToStringLowercase(request),
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version), ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
            };

            _logger.LogRequest(cfg.Host + router.Route, posNo, "", phoneNumber + "@" + apiReq.JsonData);
            var result = await _apiClient.SendAsync(apiReq);
            _logger.LogResponse(cfg.Host + router.Route, posNo, result.ResponseTimeMs, "", phoneNumber + "@" + result.Response);

            if (!string.IsNullOrEmpty(result.Response))
                return new CxOtpResult(true, "OK", ConvertHelper.ToObject<CXResponse>(result.Response));
            return new CxOtpResult(false, result.Response, result.Error);
        }
        catch (Exception ex)
        {
            return new CxOtpResult(false, ex.Message, null);
        }
    }

    public async Task<CxOtpResult> VerifyOTPAsync(string posNo, string phoneNumber, string otp, string action)
    {
        if (IsCheckNoneOtp(phoneNumber))
            return new CxOtpResult(true, $"Số điện thoại {phoneNumber} không cần check OTP", null);
        if (string.IsNullOrEmpty(otp))
            return new CxOtpResult(false, "Vui lòng nhập số Otp", null);

        phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
        var cfg = GetCxConfig();
        if (cfg == null) return new CxOtpResult(false, MessConst.NotFounDataConfig, null);
        try
        {
            var router = cfg.SysWebApiRoute?.FirstOrDefault(x => string.Equals(x.Name, "VerifyOTP", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
            if (router == null) return new CxOtpResult(false, MessageConst.ApiNotFounDataConfig, null);

            var request = new VerifyOTPDto { PhoneNumber = phoneNumber, Otp = otp, Action = action };
            var apiReq = new ApiCallRequest
            {
                Host = cfg.Host ?? "", Endpoint = router.Route ?? "",
                Headers = new Dictionary<string, string> { { "X-API-KEY", cfg.UserName ?? "" } },
                TokenType = TokenTypeEnum.Basic.ToString(), WebMethod = "POST",
                JsonData = StringHelper.ObjectToStringLowercase(request),
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version), ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
            };

            _logger.LogRequest(cfg.Host + router.Route, posNo, "", phoneNumber + "@" + apiReq.JsonData);
            var result = await _apiClient.SendAsync(apiReq);
            _logger.LogResponse(cfg.Host + router.Route, posNo, result.ResponseTimeMs, "", phoneNumber + "@" + result.Response);

            if (string.IsNullOrEmpty(result.Response))
                return new CxOtpResult(false, result.Response, result.Error);

            if (result.Response == HttpStatusCode.Conflict.ToString())
                return new CxOtpResult(false, HttpStatusCode.Conflict.ToString(), null);

            var dataResponse = ConvertHelper.ToObject<CXResponse>(result.Response);
            if (dataResponse?.Data != null)
            {
                var otpResult = JsonConvert.DeserializeObject<VerifyOTPData>(JsonConvert.SerializeObject(dataResponse.Data));
                if (otpResult is { IsValid: true })
                    return new CxOtpResult(true, "OK", otpResult);
            }
            return new CxOtpResult(false, "Số OTP không hợp lệ", dataResponse);
        }
        catch (Exception ex)
        {
            return new CxOtpResult(false, ex.Message, null);
        }
    }

    public bool IsCheckNoneOtp(string phoneNumber)
    {
        try
        {
            var setup = _memoryCache.GetPOSDataSetup(RedisConst.Redis_Key_WWW_NONE_OTP);
            var lstMobileCarrier = StringHelper.ConverStringToArray(setup?.Value, ';');
            var prefix = StringHelper.Left(phoneNumber, 3);
            return lstMobileCarrier.Length > 0 && lstMobileCarrier.Contains(prefix);
        }
        catch
        {
            return false;
        }
    }
}

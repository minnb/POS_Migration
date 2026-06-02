using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// CX Voucher service — port logic từ VoucherController cũ (api/vc/*).
/// Gọi CX API qua IApiCallClient, config AppCode="CX" từ SysWebApi.
/// Routes: CXUrlGenerateOTP, CXUrlGetVoucher, CXUrlUpdateVoucherStatus từ SysWebApiRoute.
/// </summary>
public sealed class VoucherService : IVoucherService
{
    private const string RouteGenerateOtp = "CXUrlGenerateOTP";
    private const string RouteGetVoucher = "CXUrlGetVoucher";
    private const string RouteUpdateVoucherStatus = "CXUrlUpdateVoucherStatus";

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;

    public VoucherService(IMemoryCacheService cache, IApiCallClient apiClient)
    {
        _cache = cache;
        _apiClient = apiClient;
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpAsync(
        string storeNo, string posID, string codeValue)
    {
        if (string.IsNullOrEmpty(codeValue))
            return (HttpStatusCode.BadRequest, "codeValue đang bị trống", null);
        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "storeNo đang bị trống", null);
        if (string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "posID đang bị trống", null);

        var (cfg, auth) = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var route = GetRoute(cfg, RouteGenerateOtp);
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{route}?codeValue={codeValue}",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = auth,
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<CxGenerateOtpResponse>(result.Response);
        if (resp?.errorCode == "E000")
            return (HttpStatusCode.OK, resp.errorMessage ?? "Success", null);
        if (resp != null)
            return (HttpStatusCode.BadRequest, $"{resp.errorCode}:{resp.errorMessage}", null);
        return (HttpStatusCode.ServiceUnavailable, "Lỗi hệ thống API CrownX, Vui lòng liên hệ IT", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetVoucherSerialAsync(
        string storeNo, string posID, string codeValue, string otp)
    {
        if (string.IsNullOrEmpty(codeValue))
            return (HttpStatusCode.BadRequest, "codeValue đang bị trống", null);
        if (string.IsNullOrEmpty(otp))
            return (HttpStatusCode.BadRequest, "otp đang bị trống", null);
        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "storeNo đang bị trống", null);
        if (string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "posID đang bị trống", null);

        var (cfg, auth) = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var route = GetRoute(cfg, RouteGetVoucher);
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{route}?codeValue={codeValue}&otp={otp}",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = auth,
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<CxGetVoucherSerialResponse>(result.Response);
        if (resp?.errorCode == "E000")
            return (HttpStatusCode.OK, resp.errorMessage ?? "Success", resp.data);
        if (resp != null)
            return (HttpStatusCode.BadRequest, $"{resp.errorCode}:{resp.errorMessage}", null);
        return (HttpStatusCode.ServiceUnavailable, "Lỗi hệ thống API Blue, Vui lòng liên hệ IT", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdateVoucherStatusAsync(
        UpdateVoucherStatusRequest request)
    {
        if (string.IsNullOrEmpty(request.StoreNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(request.PosID))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);
        if (request.ListSerial?.Any(d => string.IsNullOrEmpty(d.VoucherSerial)) == true)
            return (HttpStatusCode.BadRequest, "VoucherSerial đang bị trống", null);

        var (cfg, auth) = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var cxRequest = new CxUpdateVoucherStatusRequest
        {
            data = new CxVoucherStatusData
            {
                voucherDetails = request.ListSerial!.Select(a => new CxVoucherStatusItem
                {
                    voucherSerial = a.VoucherSerial,
                    voucherStatus = "USED",
                }).ToList()
            }
        };

        var route = GetRoute(cfg, RouteUpdateVoucherStatus);
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(cxRequest),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = auth,
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<CxUpdateVoucherStatusResponse>(result.Response);
        if (resp?.errorCode == "E000")
            return (HttpStatusCode.OK, resp.errorMessage ?? "Success", null);
        if (resp != null)
            return (HttpStatusCode.BadRequest, $"{resp.errorCode}:{resp.errorMessage}", resp.data);
        return (HttpStatusCode.ServiceUnavailable, "Gọi API CX thất bại, vui lòng liên hệ IT hỗ trợ", null);
    }

    private (SysWebApiDto? cfg, string auth) GetCxConfig()
    {
        var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "CX", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (cfg == null) return (null, "");
        return (cfg, EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"));
    }

    private static string GetRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";
}

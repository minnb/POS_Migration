using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Use cases PaymentController (api/v2/partner/*) — port logic từ PaymentController cũ.
/// Voucher: UrBox → IUrboxService; GotIt → IPLGService; OneU → IApiCallClient AppCode="ONEU".
/// Coupon: Capillary → IApiCallClient AppCode="CAPILLARY" (token = Base64(user.store:MD5(pass))).
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private static readonly string[] CapillaryHeaders_Coupon =
    [
        "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER",
        "x-winx-loyalty-client", "POS"
    ];

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly IUrboxService _urboxService;
    private readonly IPLGService _plgService;

    public PaymentService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        IUrboxService urboxService,
        IPLGService plgService)
    {
        _cache = cache;
        _apiClient = apiClient;
        _urboxService = urboxService;
        _plgService = plgService;
    }

    // ── voucher/check ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ValidateVoucherAsync(
        CheckVoucherPartnerPOSRequest model)
    {
        return (model.Partner?.ToUpper()) switch
        {
            "URBOX" => await CheckUrboxAsync(model),
            "GOTIT" => await CheckGotItAsync(model),
            "ONEU" => await OneUEstimateAsync(model),
            _ => (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {model.Partner ?? ""}", null)
        };
    }

    // ── voucher/update-status ─────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdateStatusVoucherAsync(
        UpdateStatusVoucherPartnerRequest model)
    {
        return (model.Partner?.ToUpper()) switch
        {
            "URBOX" => await PayUrboxAsync(model),
            "GOTIT" => await UseGotItAsync(model),
            "ONEU" => await OneURedeemAsync(model),
            _ => (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {model.Partner ?? ""}", null)
        };
    }

    // ── coupon/check (CAP) ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ValidateCouponAsync(
        CheckVoucherPartnerPOSRequest model)
    {
        if (!string.Equals(model.Partner, "CAP", StringComparison.OrdinalIgnoreCase))
            return (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {model.Partner ?? ""}", null);

        if (string.IsNullOrEmpty(model.PhoneNumber))
            return (HttpStatusCode.BadRequest,
                "Nhập số điện thoại hội viên WIN để sử dụng Coupon", null);
        if (model.SerialNo?.Count > 1)
            return (HttpStatusCode.BadRequest, "Giới hạn 1 coupon mỗi lần kiểm tra", null);

        var code = model.SerialNo?.FirstOrDefault() ?? "";
        var cfg = GetCapillaryConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = GetRoute(cfg, "ValidateCoupon");
        if (string.IsNullOrEmpty(route)) return (HttpStatusCode.BadRequest, "ValidateCoupon route chưa cấu hình", null);

        var phone = FormatHelper.PhoneNumberWithCountryCode(model.PhoneNumber);
        var token = BuildCapillaryToken(cfg.UserName ?? "", model.StoreNo ?? "", cfg.Password ?? "");
        var endpoint = $"{route}?mobile={phone}&code={code}&details=extended&format=json";

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Basic",
            Headers = new Dictionary<string, string>
            {
                { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                { "x-winx-loyalty-client", "POS" },
            },
        });

        var resp = ConvertHelper.ToObject<object>(result.Response);
        if (!string.IsNullOrEmpty(result.Response) && result.Error?.StatusCode is null or 200)
            return (HttpStatusCode.OK, "Success", resp);
        return (HttpStatusCode.BadRequest, result.Error?.Message ?? "Lỗi Capillary coupon check", null);
    }

    // ── coupon/redeem (CAP) ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CouponRedeemAsync(
        UpdateStatusVoucherPartnerRequest model)
    {
        if (!string.Equals(model.Partner, "CAP", StringComparison.OrdinalIgnoreCase))
            return (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {model.Partner ?? ""}", null);

        if (string.IsNullOrEmpty(model.PhoneNumber))
            return (HttpStatusCode.BadRequest,
                "Nhập số điện thoại hội viên WIN để sử dụng voucher", null);

        var cfg = GetCapillaryConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = GetRoute(cfg, "RedeemCoupon");
        if (string.IsNullOrEmpty(route)) return (HttpStatusCode.BadRequest, "RedeemCoupon route chưa cấu hình", null);

        var phone = FormatHelper.PhoneNumberWithCountryCode(model.PhoneNumber);
        var token = BuildCapillaryToken(cfg.UserName ?? "", model.StoreNo ?? "", cfg.Password ?? "");

        var payload = JsonConvert.SerializeObject(new
        {
            partner = "CAP",
            posNo = model.PosNo,
            storeNo = model.StoreNo,
            phoneNumber = phone,
            orderNo = model.OrderNo,
            serialNo = model.SerialNo?.Select(x => new { code = x.Code, amount = x.Amount, description = x.Description })
        });

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Basic",
            Headers = new Dictionary<string, string>
            {
                { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
            },
        });

        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK) return (HttpStatusCode.OK, resp.Message ?? "OK", resp.Data);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi Capillary coupon redeem", null);
    }

    // ── coupon/list/user (CAP) ────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetCouponByUserAsync(
        string partner, string storeNo, string mobile, string status)
    {
        if (!string.Equals(partner, "CAP", StringComparison.OrdinalIgnoreCase))
            return (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {partner}", null);

        var cfg = GetCapillaryConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = GetRoute(cfg, "GetCouponsByUser");
        if (string.IsNullOrEmpty(route)) return (HttpStatusCode.BadRequest, "GetCouponsByUser route chưa cấu hình", null);

        var notes = cfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "GetCouponsByUser", StringComparison.OrdinalIgnoreCase))?
            .Notes ?? "";

        var token = BuildCapillaryToken(cfg.UserName ?? "", storeNo, cfg.Password ?? "");
        var endpoint = $"{route}?mobile={mobile}&status={status.ToUpper()}{notes}";

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Basic",
            Headers = new Dictionary<string, string>
            {
                { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
            },
        });

        var resp = ConvertHelper.ToObject<object>(result.Response);
        if (!string.IsNullOrEmpty(result.Response) && result.Error?.StatusCode is null or 200)
            return (HttpStatusCode.OK, "OK", resp);
        return (HttpStatusCode.BadRequest, result.Error?.Message ?? "Lỗi Capillary", null);
    }

    // ── coupon/detail (CAP) ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetCouponDetailAsync(
        string partner, string storeNo, string serialNo)
    {
        if (!string.Equals(partner, "CAP", StringComparison.OrdinalIgnoreCase))
            return (HttpStatusCode.BadRequest,
                $"Tham số PartnerCode truyền vào không đúng {partner}", null);

        var cfg = GetCapillaryConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = GetRoute(cfg, "GetCouponDetails");
        if (string.IsNullOrEmpty(route)) return (HttpStatusCode.BadRequest, "GetCouponDetails route chưa cấu hình", null);

        var token = BuildCapillaryToken(cfg.UserName ?? "", storeNo, cfg.Password ?? "");
        var endpoint = $"{route}?code={serialNo}";

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Basic",
            Headers = new Dictionary<string, string>
            {
                { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
            },
        });

        var resp = ConvertHelper.ToObject<object>(result.Response);
        if (!string.IsNullOrEmpty(result.Response) && result.Error?.StatusCode is null or 200)
            return (HttpStatusCode.OK, "OK", resp);
        return (HttpStatusCode.BadRequest, result.Error?.Message ?? "Lỗi Capillary", null);
    }

    // ── coupon/re-active (CAP) ────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ReActiveCouponAsync(
        ReActiveCouponRequest request)
    {
        if (!string.Equals(request.Partner, "CAP", StringComparison.OrdinalIgnoreCase))
            return (HttpStatusCode.BadRequest,
                $"Tham số Partner truyền vào không đúng {request.Partner}", null);

        var cfg = GetCapillaryConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = GetRoute(cfg, "ReActivateCoupons");
        if (string.IsNullOrEmpty(route)) return (HttpStatusCode.BadRequest, "ReActivateCoupons route chưa cấu hình", null);

        var token = BuildCapillaryToken(cfg.UserName ?? "", request.StoreNo, cfg.Password ?? "");
        var payload = JsonConvert.SerializeObject(new
        {
            storeNo = request.StoreNo,
            couponIds = request.SerialNo,
            userName = request.UserName,
        });

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<object>(result.Response);
        if (!string.IsNullOrEmpty(result.Response) && result.Error?.StatusCode is null or 200)
            return (HttpStatusCode.OK, "OK", resp);
        return (HttpStatusCode.BadRequest, result.Error?.Message ?? "Lỗi Capillary re-active", null);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(HttpStatusCode, string, object?)> CheckUrboxAsync(CheckVoucherPartnerPOSRequest model)
    {
        var (ok, msg, data) = await _urboxService.CheckSerialUrbox(model);
        return ok ? (HttpStatusCode.OK, msg, data) : (HttpStatusCode.BadRequest, msg, data);
    }

    private async Task<(HttpStatusCode, string, object?)> CheckGotItAsync(CheckVoucherPartnerPOSRequest model)
    {
        var req = new PlCheckVoucherRequest
        {
            partner = "GOTIT",
            storeNo = model.StoreNo,
            posID = model.PosNo,
            listSeriNo = model.SerialNo?.Select(s => new PlSeriNoRequest { seriNo = s }).ToList(),
        };
        return await _plgService.GotItMultiCheckAsync(req);
    }

    private async Task<(HttpStatusCode, string, object?)> OneUEstimateAsync(CheckVoucherPartnerPOSRequest model)
    {
        var cfg = GetOneUConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var token = await GetOneUTokenAsync(cfg);
        if (token == null) return (HttpStatusCode.BadRequest, "Lỗi không lấy được token OneU", null);

        var route = GetRoute(cfg, "Estimate");
        var payload = JsonConvert.SerializeObject(new
        {
            partner = model.Partner,
            store_no = model.StoreNo,
            pos_no = model.PosNo,
            serial_no = model.SerialNo,
        });

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Bearer",
        });

        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "OK", resp?.Data);
    }

    private async Task<(HttpStatusCode, string, object?)> PayUrboxAsync(UpdateStatusVoucherPartnerRequest model)
    {
        var (ok, msg, data) = await _urboxService.PayCodelUrbox(model);
        return ok ? (HttpStatusCode.OK, msg, data) : (HttpStatusCode.BadRequest, msg, data);
    }

    private async Task<(HttpStatusCode, string, object?)> UseGotItAsync(UpdateStatusVoucherPartnerRequest model)
    {
        var req = new RedeemVoucherRequest
        {
            partner = "GOTIT",
            storeNo = model.StoreNo,
            posID = model.PosNo,
            orderNo = model.OrderNo,
            listSeriNo = model.SerialNo?.Select(x => new PlSeriNoItem
            {
                seriNo = x.Code, isVoucher = true, value = (double)x.Amount
            }).ToList(),
        };
        return await _plgService.GotItUsedAsync(req);
    }

    private async Task<(HttpStatusCode, string, object?)> OneURedeemAsync(UpdateStatusVoucherPartnerRequest model)
    {
        var cfg = GetOneUConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var token = await GetOneUTokenAsync(cfg);
        if (token == null) return (HttpStatusCode.BadRequest, "Lỗi không lấy được token OneU", null);

        var route = GetRoute(cfg, "Redeem");
        var payload = JsonConvert.SerializeObject(new
        {
            partner = model.Partner,
            store_no = model.StoreNo,
            pos_no = model.PosNo,
            order_no = model.OrderNo,
            serial_no = model.SerialNo?.Select(x => x.Code),
        });

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = token,
            TokenType = "Bearer",
        });

        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "OK", resp?.Data);
    }

    private SysWebApiDto? GetCapillaryConfig()
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "CAPILLARY", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private SysWebApiDto? GetOneUConfig()
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "ONEU", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private static string GetRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";

    /// <summary>Capillary Basic token = Base64(UserName.StoreNo:MD5(Password)) — giống CreateTokenCapillary cũ.</summary>
    private static string BuildCapillaryToken(string userName, string storeNo, string password)
        => EncryptionHelper.Base64Encode($"{userName}.{storeNo}:{EncryptionHelper.GetMd5Hash(password)}");

    private async Task<string?> GetOneUTokenAsync(SysWebApiDto cfg)
    {
        try
        {
            var tokenRoute = GetRoute(cfg, "GetToken");
            var payload = JsonConvert.SerializeObject(new
            {
                client_id = cfg.UserName,
                client_secret = cfg.Password,
            });
            var result = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = cfg.Host ?? "",
                Endpoint = tokenRoute,
                WebMethod = "POST",
                JsonData = payload,
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            });
            var resp = ConvertHelper.ToObject<dynamic>(result.Response);
            return resp?.access_token as string;
        }
        catch { return null; }
    }
}

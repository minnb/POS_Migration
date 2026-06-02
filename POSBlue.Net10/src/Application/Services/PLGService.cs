using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// PLG service — port logic từ PLGController cũ (api/plg/*).
/// Dispatch: PLG/UrBox/GiftBox/GotIt/Giftee qua IApiCallClient; CX qua IApiCallClient AppCode="CX".
/// Odoo SAP SOAP: stub pending WCF regen.
/// Config partner từ SysWebApi AppCode tương ứng.
/// </summary>
public sealed class PLGService : IPLGService
{
    // SysWebApi AppCode names
    private const string AppCodeUrBox = "URBOX";
    private const string AppCodeGiftBox = "GIFTBOX";
    private const string AppCodeGotIt = "GOTIT";
    private const string AppCodeGiftee = "GIFTEE";
    private const string AppCodeCx = "CX";

    // Partner API routes (stored as Route values in SysWebApiRoute)
    private const string GiftBoxStatus = "/interface/pos/voucherStatus.m12";
    private const string GiftBoxUsed = "/interface/pos/voucherUsed.m12";
    private const string GotItCheck = "/check.json";
    private const string GotItUseList = "/usemultiple.json";
    private const string GifteeCheck = "/tickets";
    private const string CxUrlInfoV2 = "/api/pos/v2/customer/info";
    private const string CxUrlSale = "/lyt-transactions/api/v1/pos/sale";

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly IUrboxService _urboxService;

    public PLGService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        IUrboxService urboxService)
    {
        _cache = cache;
        _apiClient = apiClient;
        _urboxService = urboxService;
    }

    // ── GetInfoCard (router) ──────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetInfoCardAsync(
        PlCheckVoucherRequest model)
    {
        if (string.IsNullOrEmpty(model.partner))
            return (HttpStatusCode.BadRequest, "Vui lòng nhập partner", null);
        if (model.listSeriNo?.Any(d => string.IsNullOrEmpty(d.seriNo)) != false)
            return (HttpStatusCode.BadRequest, "Vui lòng nhập serialNo", null);

        return model.partner switch
        {
            "UrBox" => await UrBoxCheckAsync(model),
            "GiftBox" => await GiftBoxMultiVoucherStatusAsync(model),
            "GotIt" => await GotItMultiCheckAsync(model),
            "Giftee" => await GifteeStatusTicketsAsync(new GifteeStatusRequest
            {
                StoreNo = model.storeNo, PosTerminal = model.posID
            }),
            "PLG" => await PlgGetInfoCardAsync(model.listSeriNo![0].seriNo!),
            _ => (HttpStatusCode.BadRequest, "Không tìm thấy nhà cung cấp", null)
        };
    }

    // ── SaleVoucher ───────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> SaleVoucherAsync(
        SaleVoucherRequest model)
    {
        // Delegate to PLG partner via IApiCallClient
        var cfg = GetConfig("PLG");
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình PLG WebApi", null);

        var route = GetRoute(cfg, "SaleVoucher");
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK) return (HttpStatusCode.OK, resp.Message ?? "OK", resp.Data);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi PLG API", null);
    }

    // ── RedeemVoucher ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> RedeemVoucherAsync(
        RedeemVoucherRequest model)
    {
        var cfg = GetConfig("PLG");
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình PLG WebApi", null);

        var route = GetRoute(cfg, "RedeemVoucher");
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK) return (HttpStatusCode.OK, resp.Message ?? "OK", resp.Data);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi PLG API", null);
    }

    // ── Check_VC_Odoo (Odoo SAP SOAP — stub) ──────────────────────────────────

    public Task<(HttpStatusCode Status, string Message, object? Data)> CheckVcOdooAsync(
        PlCheckVoucherRequest model)
    {
        // TODO: Implement Odoo SAP SOAP call via WCF client (SI_VC_OUTService)
        // The legacy calls OdooSAP_Task_Check_Voucher → OdooSAP_Check_Call_WS (SOAP XML)
        return Task.FromResult<(HttpStatusCode, string, object?)>(
            (HttpStatusCode.OK, "OK", new List<object>()));
    }

    // ── OdooSAP_Update_Voucher (Odoo SAP SOAP — stub) ─────────────────────────

    public Task<(HttpStatusCode Status, string Message, object? Data)> OdooSapUpdateVoucherAsync(
        OdooUpdateVoucherRequest model)
    {
        // TODO: Implement Odoo SAP SOAP update via WCF client (SI_VC_OUTService)
        return Task.FromResult<(HttpStatusCode, string, object?)>(
            (HttpStatusCode.OK, "Success", null));
    }

    // ── UrBox ─────────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UrBoxCheckAsync(
        PlCheckVoucherRequest model)
    {
        var request = new CheckVoucherPartnerPOSRequest
        {
            amount = 0,
            code = string.Join(",", model.listSeriNo!.Select(a => a.seriNo)),
            store_id = model.storeNo,
            terminal_id = model.posID,
        };
        var (ok, msg, data) = await _urboxService.CheckSerialUrbox(request);
        return ok ? (HttpStatusCode.OK, msg, data) : (HttpStatusCode.BadRequest, msg, data);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UrBoxPaymentAsync(
        RedeemVoucherRequest model)
    {
        var request = new UpdateStatusVoucherPartnerRequest
        {
            Partner = "UrBox",
            StoreNo = model.storeNo,
            PosNo = model.posID,
            OrderNo = model.orderNo,
            Items = null,
            SerialNo = model.listSeriNo?.Select(x => new LstVoucherPartner
            {
                Code = x.seriNo, Amount = (decimal)x.value
            }).ToList(),
        };
        var (ok, msg, data) = await _urboxService.PayCodelUrbox(request);
        return ok ? (HttpStatusCode.OK, msg, data) : (HttpStatusCode.BadRequest, msg, data);
    }

    // ── GiftBox ───────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxMultiVoucherStatusAsync(
        PlCheckVoucherRequest model)
    {
        var cfg = GetConfig(AppCodeGiftBox);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GiftBox WebApi", null);

        var results = new List<object>();
        foreach (var item in model.listSeriNo ?? [])
        {
            var body = JsonConvert.SerializeObject(new GiftBoxVoucherStatusRequest
            {
                authKey = cfg.UserName,
                brandCode = cfg.Password,
                pinNo = item.seriNo,
                storeCode = model.storeNo,
            });
            var resp = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = cfg.Host ?? "",
                Endpoint = GiftBoxStatus,
                WebMethod = "POST",
                JsonData = body,
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            });
            var data = ConvertHelper.ToObject<object>(resp.Response);
            results.Add(data ?? new { error = resp.Response });
        }
        return (HttpStatusCode.OK, "", results);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxVoucherStatusAsync(
        GiftBoxVoucherStatusRequest model)
    {
        var cfg = GetConfig(AppCodeGiftBox);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GiftBox WebApi", null);

        if (string.IsNullOrEmpty(model.authKey)) model.authKey = cfg.UserName;
        if (string.IsNullOrEmpty(model.brandCode)) model.brandCode = cfg.Password;

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = GiftBoxStatus,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
        });
        var resp = ConvertHelper.ToObject<object>(result.Response);
        return (HttpStatusCode.OK, "", resp);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxVoucherUsedAsync(
        RedeemVoucherRequest model)
    {
        var cfg = GetConfig(AppCodeGiftBox);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GiftBox WebApi", null);

        var results = new List<object>();
        foreach (var item in model.listSeriNo ?? [])
        {
            var body = JsonConvert.SerializeObject(new GiftBoxVoucherStatusRequest
            {
                authKey = cfg.UserName,
                brandCode = cfg.Password,
                pinNo = item.seriNo,
                storeCode = model.storeNo,
            });
            var resp = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = cfg.Host ?? "",
                Endpoint = GiftBoxUsed,
                WebMethod = "POST",
                JsonData = body,
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            });
            results.Add(ConvertHelper.ToObject<object>(resp.Response) ?? new { error = resp.Response });
        }
        return (HttpStatusCode.OK, "OK", results);
    }

    // ── Giftee ────────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GifteeStatusTicketsAsync(
        GifteeStatusRequest model)
    {
        var cfg = GetConfig(AppCodeGiftee);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình Giftee WebApi", null);

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = GifteeCheck,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = cfg.UserName ?? "",
            TokenType = "Bearer",
        });
        var resp = ConvertHelper.ToObject<object>(result.Response);
        return (HttpStatusCode.OK, "", resp);
    }

    // ── GotIt ─────────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GotItCheckAsync(
        GotItCheckPosRequest model)
    {
        var cfg = GetConfig(AppCodeGotIt);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GotIt WebApi", null);

        var body = JsonConvert.SerializeObject(new { pin = model.storeNo, code = model.serialNo });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = GotItCheck,
            WebMethod = "POST",
            JsonData = body,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
        });
        var resp = ConvertHelper.ToObject<object>(result.Response);
        return (HttpStatusCode.OK, "", resp);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GotItMultiCheckAsync(
        PlCheckVoucherRequest model)
    {
        var cfg = GetConfig(AppCodeGotIt);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GotIt WebApi", null);

        var codes = model.listSeriNo?.Select(x => x.seriNo).ToList() ?? [];
        var body = JsonConvert.SerializeObject(new { pin = model.storeNo, codes });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = GotItUseList,
            WebMethod = "POST",
            JsonData = body,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
        });
        var resp = ConvertHelper.ToObject<object>(result.Response);
        return (HttpStatusCode.OK, "", resp);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GotItUsedAsync(
        RedeemVoucherRequest model)
    {
        var cfg = GetConfig(AppCodeGotIt);
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình GotIt WebApi", null);

        var codes = model.listSeriNo?.Select(x => x.seriNo).ToList() ?? [];
        var body = JsonConvert.SerializeObject(new { pin = model.storeNo, codes });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = GotItUseList,
            WebMethod = "POST",
            JsonData = body,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
        });
        var resp = ConvertHelper.ToObject<object>(result.Response);
        if (result.Error?.StatusCode > 0 && result.Error.StatusCode != 200)
            return (HttpStatusCode.BadRequest, "Gọi GotIt API thất bại", null);
        return (HttpStatusCode.OK, "OK", resp);
    }

    // ── CX Sale & GetInfoMember ───────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> SaleAsync(
        PlgSaleCxRequest model)
    {
        if (string.IsNullOrEmpty(model.storeNo))
            return (HttpStatusCode.BadRequest, "Mã CH/ST đang bị trống", null);
        if (string.IsNullOrEmpty(model.posID))
            return (HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
        if (string.IsNullOrEmpty(model.orderNo))
            return (HttpStatusCode.BadRequest, "Đơn hàng đang bị trống", null);
        if (model.orderAmount <= 0)
            return (HttpStatusCode.BadRequest, "Tổng giá trị đơn hàng phải lớn hơn 0", null);

        var (cfg, auth) = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var cxBody = new
        {
            csn = model.cardNumber,
            merchantId = model.merchantId ?? "VCM",
            storeCode = model.storeNo,
            posCode = model.posID,
            invoiceNo = model.orderNo,
            totalBillAmount = model.orderAmount,
            spendPoints = model.spendPoints,
            awardAmount = model.awardAmount,
            referenceInvoice = model.referenceInvoice,
        };
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = CxUrlSale,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(cxBody),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = auth,
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK) return (HttpStatusCode.OK, resp.Message ?? "OK", resp.Data);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi CX API", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetInfoMemberAsync(
        string numberCard, string storeNo, string posID)
    {
        if (string.IsNullOrEmpty(numberCard))
            return (HttpStatusCode.BadRequest, "numberCard đang bị trống", null);

        var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
        if (!regexItem.IsMatch(numberCard))
            return (HttpStatusCode.BadRequest,
                "Mã thẻ/QRCode/số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);

        var (cfg, auth) = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{CxUrlInfoV2}?codeValue={numberCard}&clubCode=CVL",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = auth,
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<CxApiResponse>(result.Response);
        if (string.IsNullOrEmpty(resp?.code))
            return (HttpStatusCode.OK, "Success", resp?.data);
        return (HttpStatusCode.BadRequest, $"{resp?.code}:{resp?.message}", null);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private SysWebApiDto? GetConfig(string appCode)
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, appCode, StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private (SysWebApiDto? cfg, string auth) GetCxConfig()
    {
        var cfg = GetConfig(AppCodeCx);
        if (cfg == null) return (null, "");
        return (cfg, EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"));
    }

    private static string GetRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";

    private async Task<(HttpStatusCode, string, object?)> PlgGetInfoCardAsync(string serialNo)
    {
        var cfg = GetConfig("PLG");
        if (cfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình PLG WebApi", null);

        var route = GetRoute(cfg, "InforVoucher");
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{route}?serialNo={serialNo}",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK) return (HttpStatusCode.OK, "OK", resp.Data);
        return (resp?.Status ?? HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi PLG API", null);
    }
}

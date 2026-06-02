using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// VINID TopUp + EVoucher service — port logic từ VoucherTopUpVinIDController cũ (api/vinid/*).
/// Auth: HMAC RSA SHA256 headers. Config AppCode="VINID_TOPUP" từ SysWebApi.
/// Signature: rawData = "{url};{method};{nonce};{timestamp};{keyCode};[body]"
/// </summary>
public sealed class VinIdTopUpService : IVinIdTopUpService
{
    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly ILoyaltyRepository _loyaltyRepo;

    public VinIdTopUpService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        ILoyaltyRepository loyaltyRepo)
    {
        _cache = cache;
        _apiClient = apiClient;
        _loyaltyRepo = loyaltyRepo;
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> TopUpCheckMemberAsync(
        string phoneNumber, string posID, string storeNo)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return (HttpStatusCode.BadRequest, "phoneNumber đang bị trống", null);
        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "storeNo đang bị trống", null);
        if (string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "posID đang bị trống", null);

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var (mappedStoreNo, mappedPosId) = GetStoreMapping(storeNo, posID);
        var route = GetRoute(cfg, "topup_get_user_by_phone");
        var endpoint = $"{route}?phone_number={phoneNumber}&store_code={mappedStoreNo}&pos_code={mappedPosId}";

        var headers = BuildSignatureHeaders(cfg, route, "GET", "");
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            Headers = headers,
        });

        var resp = ConvertHelper.ToObject<VinIdTopUpCheckMemberResponse>(result.Response);
        if (resp?.meta?.code == 200 && resp.data != null)
        {
            var topupItemNo = cfg.SysWebApiRoute?
                .FirstOrDefault(x => x.Name == "TOPUP_ITEM_VINID")?.Notes ?? "";
            return (HttpStatusCode.OK, "Success", new VinIdTopUpCheckPosResponse
            {
                FullName = resp.data.full_name,
                PhoneNumber = resp.data.phone_number,
                DOB = resp.data.dob,
                Gender = resp.data.gender,
                Identify = resp.data.identify,
                Status = resp.data.status,
                CardStatus = resp.data.card_status,
                ArticleNo = topupItemNo,
            });
        }
        return (HttpStatusCode.BadRequest, resp?.meta?.message ?? "Gọi API VINID thất bại", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> TopUpPointToPhoneAsync(
        VinIdTopUpPointPosRequest model)
    {
        if (string.IsNullOrEmpty(model.PhoneNumber))
            return (HttpStatusCode.BadRequest, "PhoneNumber đang bị trống", null);
        if (string.IsNullOrEmpty(model.StoreNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(model.PosID))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);
        if (string.IsNullOrEmpty(model.OrderNo))
            return (HttpStatusCode.BadRequest, "OrderNo đang bị trống", null);
        if (model.PointAmount < 50)
            return (HttpStatusCode.BadRequest, "PointAmount tối thiểu 50", null);
        if (model.IsVAT)
        {
            var vat = model.InfoVAT;
            if (vat == null || string.IsNullOrEmpty(vat.FullName))
                return (HttpStatusCode.BadRequest, "Họ tên xuất VAT đang bị trống", null);
            if (string.IsNullOrEmpty(vat.Address))
                return (HttpStatusCode.BadRequest, "Địa chỉ xuất VAT đang bị trống", null);
            if (string.IsNullOrEmpty(vat.TaxCode))
                return (HttpStatusCode.BadRequest, "MST xuất VAT đang bị trống", null);
            if (string.IsNullOrEmpty(vat.Email))
                return (HttpStatusCode.BadRequest, "Email xuất VAT đang bị trống", null);
        }

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var (mappedStoreNo, mappedPosId) = GetStoreMapping(model.StoreNo, model.PosID);
        var route = GetRoute(cfg, "topup_point_phone");

        var body = new VinIdTopUpPointRequest
        {
            phone_number = model.PhoneNumber,
            store_code = mappedStoreNo,
            pos_code = mappedPosId,
            point_amount = model.PointAmount,
            order_id = model.OrderNo,
            txn_id = model.OrderNo,
            send_receipt = model.IsVAT,
            receipt_info = (model.InfoVAT == null || !model.IsVAT) ? null : new TopUpReceiptInfo
            {
                address = model.InfoVAT.Address,
                email = model.InfoVAT.Email,
                name = model.InfoVAT.FullName,
                tax_code = model.InfoVAT.TaxCode,
            }
        };
        var payload = JsonConvert.SerializeObject(body);
        var headers = BuildSignatureHeaders(cfg, route, "POST", payload);

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            Headers = headers,
        });

        var resp = ConvertHelper.ToObject<VinIdTopUpPointResponse>(result.Response);
        if (resp?.meta?.code == 200)
            return (HttpStatusCode.OK, "OK", null);
        return (resp != null
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.InternalServerError,
            resp?.meta?.message ?? "Gọi API VINID thất bại", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> TopUpCheckStatusOrderAsync(
        string orderNo, string posID, string storeNo)
    {
        if (string.IsNullOrEmpty(orderNo))
            return (HttpStatusCode.BadRequest, "orderNo đang bị trống", null);
        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "storeNo đang bị trống", null);
        if (string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "posID đang bị trống", null);

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var route = GetRoute(cfg, "topup_status_order");
        var endpoint = $"{route}?txn_id={orderNo}&store_code={storeNo}&pos_code={posID}";
        var headers = BuildSignatureHeaders(cfg, route, "GET", "");

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            Headers = headers,
        });

        var resp = ConvertHelper.ToObject<VinIdTopUpStatusOrderApiResponse>(result.Response);
        if (resp?.data != null)
            return (HttpStatusCode.OK, "", new VinIdTopUpStatusOrderPosResponse
            {
                StatusOrder = resp.data.status,
                TypeOrder = resp.data.type,
            });
        return (HttpStatusCode.BadRequest, $"Không tìm thấy đơn hàng {orderNo}", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherVerifyAsync(
        string storeNo, string posID, string serialNumber, string userPOS)
    {
        if (string.IsNullOrEmpty(serialNumber))
            return (HttpStatusCode.BadRequest, "SerialNumber đang bị trống", null);
        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var route = GetRoute(cfg, "evoucher_verify");
        var body = new VinIdEVoucherVerifyRequest
        {
            store_code = "EVOUCHER",
            pos_code = "EVOUCHER",
            serial_number = serialNumber,
            merchant_staff_id = userPOS,
        };
        var payload = JsonConvert.SerializeObject(body);
        var headers = BuildSignatureHeaders(cfg, route, "POST", payload);

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            Headers = headers,
        });

        var resp = ConvertHelper.ToObject<VinIdEVoucherApiResponse>(result.Response);
        if (resp?.meta?.code == 200 && resp.data != null)
            return (HttpStatusCode.OK, "OK", new EVoucherPosResponse
            {
                MinOrderValue = resp.data.min_order_value,
                DiscountValue = resp.data.discount_value,
                DiscountType = resp.data.discount_type,
            });
        return (resp != null
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.InternalServerError,
            resp?.meta?.message ?? "Gọi API VINID thất bại", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherRefundAsync(
        VinIdEVoucherRefundPosRequest model)
    {
        if (model.ListserialNumber == null)
            return (HttpStatusCode.BadRequest, "SerialNumber đang bị trống", null);
        if (string.IsNullOrEmpty(model.StoreNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(model.PosID))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var route = GetRoute(cfg, "evoucher_refund");
        var payload = JsonConvert.SerializeObject(new VinIdEVoucherRefundRequest { serials = model.ListserialNumber });
        var headers = BuildSignatureHeaders(cfg, route, "POST", payload);

        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            Headers = headers,
        });

        var resp = ConvertHelper.ToObject<VinIdEVoucherRevokeResponse>(result.Response);
        if (resp?.meta?.code == 200) return (HttpStatusCode.OK, "OK", null);
        return (HttpStatusCode.BadRequest, resp?.meta?.message ?? "Gọi API VINID thất bại", null);
    }

    public async Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherMarkUsedAsync(
        VinIdEVoucherMarkUsedPosRequest model)
    {
        var serials = model.ListSerialNumber;
        if (serials == null || serials.Any(string.IsNullOrEmpty))
            return (HttpStatusCode.BadRequest, "SerialNumber đang bị trống", null);
        if (string.IsNullOrEmpty(model.StoreNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(model.PosID))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);
        if (string.IsNullOrEmpty(model.OrderNo))
            return (HttpStatusCode.BadRequest, "OrderNo đang bị trống", null);

        var cfg = GetTopUpConfig();
        if (cfg == null) return (HttpStatusCode.BadRequest, "Hệ thống đã ngừng dịch vụ này", null);

        var markUsedRoute = GetRoute(cfg, "evoucher_mark_used");
        var refundRoute = GetRoute(cfg, "evoucher_refund");

        var usedResults = new List<MarkUsedSerialResult>();
        foreach (var serial in serials)
        {
            var body = new VinIdEVoucherUsedRequest
            {
                merchant_code = "VinCommerce",
                store_code = "EVOUCHER",
                pos_code = "EVOUCHER",
                serial_number = serial,
                merchant_staff_id = 35515,
                redeem_ref_id = model.OrderNo,
                merchant_voucher_code = serial,
                merchant_reference_id = "",
                extra_data = null,
            };
            var payload = JsonConvert.SerializeObject(body);
            var headers = BuildSignatureHeaders(cfg, markUsedRoute, "POST", payload);

            var apiResult = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = cfg.Host ?? "",
                Endpoint = markUsedRoute,
                WebMethod = "POST",
                JsonData = payload,
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
                Headers = headers,
            });

            var resp = ConvertHelper.ToObject<VinIdEVoucherRevokeResponse>(apiResult.Response);
            usedResults.Add(new MarkUsedSerialResult
            {
                SerialNumber = serial,
                IsSuccess = resp?.meta?.code == 200,
                Message = resp?.meta?.code == 200 ? "OK" : resp?.meta?.message ?? "",
            });
        }

        var failed = usedResults.Where(x => !x.IsSuccess).ToList();
        if (failed.Count == 0)
            return (HttpStatusCode.OK, "Sử dụng thành công", null);

        // Refund những serial đã mark-used thành công nhưng toàn bộ batch thất bại
        var successOnes = usedResults.Where(x => x.IsSuccess).ToList();
        var revokeResults = new List<RevokeSerialResult>();
        if (successOnes.Count > 0)
        {
            var refundPayload = JsonConvert.SerializeObject(new VinIdEVoucherRefundRequest
            {
                serials = successOnes.Select(x => x.SerialNumber!).ToList()
            });
            var refundHeaders = BuildSignatureHeaders(cfg, refundRoute, "POST", refundPayload);
            var refundResult = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = cfg.Host ?? "",
                Endpoint = refundRoute,
                WebMethod = "POST",
                JsonData = refundPayload,
                Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
                Headers = refundHeaders,
            });
            var refundResp = ConvertHelper.ToObject<VinIdEVoucherRevokeResponse>(refundResult.Response);
            bool refundOk = refundResp?.meta?.code == 200;
            revokeResults.AddRange(successOnes.Select(x => new RevokeSerialResult
            {
                SerialNumber = x.SerialNumber,
                IsSuccess = refundOk,
                Message = refundOk ? "" : refundResp?.meta?.message ?? "",
            }));
        }

        return (HttpStatusCode.BadRequest,
            $"Sử dụng thất bại {usedResults.Count} serial number",
            new SerialMarkUsedResponse { usedList = usedResults, revokeList = revokeResults });
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private SysWebApiDto? GetTopUpConfig()
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "VINID_TOPUP", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private static string GetRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";

    private (string storeNo, string posId) GetStoreMapping(string storeNo, string posId)
    {
        try
        {
            var mapping = _loyaltyRepo.GetLoyaltyStoreMapping()
                .FirstOrDefault(x => x.NewStoreID == storeNo && x.NewTerminalID == posId);
            if (mapping != null)
                return (mapping.OldStoreID ?? storeNo, mapping.OldTerminalID ?? posId);
        }
        catch { }
        return (storeNo, posId);
    }

    private Dictionary<string, string> BuildSignatureHeaders(
        SysWebApiDto cfg, string routePath, string method, string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString();
        var keyCode = cfg.UserName ?? "";
        var rawData = string.IsNullOrEmpty(body)
            ? $"{routePath};{method};{nonce};{timestamp};{keyCode};"
            : $"{routePath};{method};{nonce};{timestamp};{keyCode};{body}";
        var signature = EncryptionHelper.VinPaySignature(rawData, cfg.PrivateKey ?? "");
        return new Dictionary<string, string>
        {
            { "X-Key-Code", keyCode },
            { "X-Timestamp", timestamp },
            { "X-Nonce", nonce },
            { "X-Signature", signature },
            { "X-Request-ID", nonce },
        };
    }
}

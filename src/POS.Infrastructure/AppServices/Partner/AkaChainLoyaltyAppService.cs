using Elastic.CommonSchema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos;
using POS.Common.Dtos.AkaChain;
using POS.Common.Dtos.Capillary.Point;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.PartnerApi;
using POS.Common.Dtos.Vouchers;
using POS.Common.Enums;
using POS.Common.Helpers;
using POS.Common.Mapping;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Infrastructure.AppServices.Partner;

/// <summary>
/// HTTP client wrapper cho FMV/AkaChain loyalty API.
/// Migrated từ TCX.WebApiCore.AppServices.FMV.AkaChainLoyaltyService
/// + TCX.WebApiCore.AppServices.FMV.AkaChainHelper
/// + TCX.WebApiCore.AppServices.FMV.AkaChainMapping.
/// Config (credentials, URLs, timeout) đọc từ DB bảng SysWebApi/SysWebApiRoute,
/// cache vào Redis (key MD:SysWebApi, field FMV, TTL 12h).
/// Token OAuth2 cũng cache vào Redis (key AkaChain:FMV:AccessToken, TTL = expires_in - 60s).
/// </summary>
public sealed class AkaChainLoyaltyAppService(
    IHttpClientFactory httpClientFactory,
    IRedisService redis,
    ICentralMDRepository centralMDRepository,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IServiceScopeFactory serviceScopeFactory
) : IAkaChainLoyaltyAppService
{
    private const string TokenCacheKey = "AkaChain:FMV:AccessToken";

    // ── Config (Redis/DB) ─────────────────────────────────────────────────────

    private Task<SysWebApiDto?> GetFMVConfigAsync(CancellationToken ct = default)
        => centralMDRepository.GetSysWebApiAsync("FMV", ct);

    private static string? GetRoute(SysWebApiDto config, string routeName)
        => config.SysWebApiRoute?.FirstOrDefault(
            x => string.Equals(x.Name, routeName, StringComparison.OrdinalIgnoreCase))?.Route;

    private static int GetTimeout(SysWebApiDto config)
        => int.TryParse(config.Version, out var t) && t > 0 ? t : 30;

    // ── Curl logger ───────────────────────────────────────────────────────────

    private void LogCurl(string context, HttpMethod method, string url,
        IEnumerable<(string Key, string Value)> headers, string? body = null)
    {
        var sb = new StringBuilder($"[CURL:{context}] curl -X {method.Method} '{url}'");
        foreach (var (k, v) in headers)
            sb.Append($" -H '{k}: {v}'");
        if (!string.IsNullOrEmpty(body))
            sb.Append($" -d '{body}'");
        fileLogHelper.WriteLogs(sb.ToString());
    }

    // ── OAuth2 Token ──────────────────────────────────────────────────────────

    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var cached = redis.StringGetRaw(TokenCacheKey);
            if (!string.IsNullOrEmpty(cached)) return cached;

            var config = await GetFMVConfigAsync();
            if (config == null)
            {
                fileLogHelper.WriteExpLogs("GetAccessTokenAsync",
                    new Exception("FMV SysWebApi config not found in DB/Redis"));
                return null;
            }

            var tokenRoute = GetRoute(config, "GetToken");
            if (tokenRoute == null)
            {
                fileLogHelper.WriteExpLogs("GetAccessTokenAsync",
                    new Exception("Route 'GetToken' not found in SysWebApiRoute for FMV"));
                return null;
            }

            config.Host = $"http://fmv-pos-gw-loyaltyos.famimapoint.com.vn";
            tokenRoute = "/connect/token";

            // BUG FIX 1: Dùng Uri.EscapeDataString thay FormUrlEncodedContent.
            // FormUrlEncodedContent encode space → '+' (HTML4).
            // AkaChain token server (IdentityServer) parse scope bằng RFC 3986:
            // space = '%20', không phải '+' → nhận '+' trả HTTP 500.
            // Postman dùng %20 → OK. Code này phải khớp Postman.
            //
            // BUG FIX 2: Dùng httpClientFactory.CreateClient() thay new HttpClient().
            // new HttpClient() trực tiếp bypass IHttpClientFactory → socket exhaustion.
            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "client_credentials" },
                { "client_id",     config.UserName ?? "" },
                { "client_secret", config.Password ?? "" },
                { "scope",         "CustomerJourneyService MasterDataService MemberService TransactionService" }
            };

            var tokenUrl = config.Host + tokenRoute;
            var formBody = string.Join("&", formData.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            // BUG FIX 3 (root cause của HTTP 500): KHÔNG gửi cookie __tenant.
            // Server ABP nhận cookie '__tenant={UserName}' → tra tenant theo client_id
            // → "Tenant not found! There is no tenant with the tenant id or name: ..." → HTTP 500.
            // Token endpoint client_credentials tự xác định tenant qua client_id, không cần cookie.
            // Code cũ (.NET Framework) cũng set cookie này nhưng handler UseCookies=true
            // âm thầm DROP nó → chưa bao giờ được gửi thật → prod cũ chạy bình thường.
            // Postman cũng strip Cookie header thủ công (cookie jar) → 200.
            // Đã verify bằng curl: có cookie → 500, không cookie → 200.
            LogCurl("GetAccessToken", HttpMethod.Post, tokenUrl,
                [
                    ("Content-Type", "application/x-www-form-urlencoded")
                ], formBody);

            var client = httpClientFactory.CreateClient("FMV");
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            // QUAN TRỌNG: new StringContent(..., Encoding.UTF8, "application/x-www-form-urlencoded")
            // tự động append '; charset=utf-8' vào Content-Type header thực gửi đi.
            // LogCurl chỉ log string truyền vào — KHÔNG phản ánh header thực tế.
            // AkaChain token server từ chối 'application/x-www-form-urlencoded; charset=utf-8' → HTTP 500.
            // Fix: tạo ByteArrayContent từ raw bytes rồi set Content-Type thủ công — không có charset.
            var bodyBytes = Encoding.UTF8.GetBytes(formBody);
            tokenRequest.Content = new ByteArrayContent(bodyBytes);
            tokenRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(tokenRequest);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                fileLogHelper.WriteExpLogs("GetAccessTokenAsync",
                    new Exception($"Token endpoint returned HTTP {(int)response.StatusCode}. Body: {responseString}"));
                return null;
            }

            var dataToken = StringHelper.StringToObject<AccessTokenDataAkaChain>(responseString);
            if (dataToken?.Access_token == null)
            {
                fileLogHelper.WriteExpLogs("GetAccessTokenAsync",
                    new Exception($"Failed to parse access token. HTTP {(int)response.StatusCode}. Body: {responseString}"));
                return null;
            }

            var expiresIn = dataToken.Expires_in > 0 ? Math.Max(dataToken.Expires_in - 60, 60) : 240;
            redis.StringSetRaw(TokenCacheKey, dataToken.Access_token, TimeSpan.FromSeconds(expiresIn));

            return dataToken.Access_token;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("AkaChainLoyaltyAppService.GetAccessTokenAsync", ex);
            return null;
        }
    }

    // ── HTTP call core ────────────────────────────────────────────────────────

    /// <summary>
    /// Thay thế AkaChainHelper.CallApiAsync.
    /// URL = config.Host + route + queryParams.
    /// Proxy: TODO — SysWebApiDto.HttpProxy cần custom DelegatingHandler, defer.
    /// </summary>
    private async Task<(HttpStatusCode Status, string Body)> CallApiAsync(
        string routeName, HttpMethod method, string? bodyJson, string? queryParams)
    {
        try
        {
            var config = await GetFMVConfigAsync();
            if (config == null)
                return (HttpStatusCode.BadRequest, "FMV config not found in SysWebApi");

            var route = GetRoute(config, routeName);
            if (route == null)
                return (HttpStatusCode.BadRequest, $"Route '{routeName}' not found in SysWebApiRoute");

            var token = await GetAccessTokenAsync();
            if (token == null)
                return (HttpStatusCode.Unauthorized, "Cannot get FMV access token");

            var client  = httpClientFactory.CreateClient("FMV");
            client.Timeout = TimeSpan.FromSeconds(GetTimeout(config));

            var fullUrl = config.Host + route + (queryParams ?? string.Empty);
            using var request = new HttpRequestMessage(method, fullUrl);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            request.Headers.TryAddWithoutValidation("__merchant", config.PublicKey ?? "");
            request.Headers.TryAddWithoutValidation("__tenant",   config.PrivateKey ?? "");

            if (bodyJson != null)
            {
                request.Content = new StringContent(bodyJson, Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            var curlHeaders = new List<(string, string)>
            {
                ("Authorization", $"Bearer {token}"),
                ("__merchant",    config.PublicKey ?? ""),
                ("__tenant",      config.PrivateKey ?? "")
            };
            if (bodyJson != null) curlHeaders.Add(("Content-Type", "application/json"));
            LogCurl($"CallApi.{routeName}", method, fullUrl, curlHeaders, bodyJson);

            var response = await client.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();
            return ((HttpStatusCode)response.StatusCode, body);
        }
        catch (TaskCanceledException)
        {
            return (HttpStatusCode.RequestTimeout, "FMV request timeout");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("AkaChainLoyaltyAppService.CallApiAsync", ex);
            return (HttpStatusCode.Conflict, ex.Message);
        }
    }

    // ── Public methods ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ResultResponse> GetMemberProfileAsync(string key, string value)
    {
        const string endpoint = "AkaChainLoyalty.GetMemberProfile";
        try
        {
            var formattedValue = "+" + FormatHelper.PhoneNumberWithCountryCode(value);
            var queryParams = $"?memberKey.key={Uri.EscapeDataString(key)}&memberKey.value={Uri.EscapeDataString(formattedValue)}";

            kibanaService.LogRequest(endpoint, value, queryParams);
            var (status, body) = await CallApiAsync("GetMemberProfile", HttpMethod.Get, null, queryParams);
            kibanaService.LogResponse(endpoint, value, 0, "", body);

            if (status != HttpStatusCode.OK)
                return ResponseHelper.MakeResponse(status, status.ToString(), body);

            var data = StringHelper.StringToObject<MemberProfileAkaChain>(body);
            if (data == null)
            {
                var dataError = StringHelper.StringToObject<AkaChainErrorResponse>(body);
                if(dataError == null)
                {
                    return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", body);
                }
                else
                {
                    if (dataError.Error.Code == "701")
                    {
                        return ResponseHelper.MakeResponse(HttpStatusCode.NotFound, dataError.Error.Message ?? $"Không tìm thấy thông tin khách hàng", body);
                    }
                    return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, dataError.Error.Message ?? "JSON conversion error", body);
                }
            }    
            return ResponseHelper.MakeResponse(HttpStatusCode.OK, "Success", AkaChainMapping.MappingInfoMember(data), "AkaChainLoyalty");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return ResponseHelper.MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    /// <inheritdoc/>
    public async Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model)
    {
        const string endpoint = "AkaChainLoyalty.AddTransaction";
        try
        {
            var config = await GetFMVConfigAsync();
            var activityCode = config?.Description ?? "";

            var bodyJson = JsonConvert.SerializeObject(AkaChainMapping.MappingInputDataRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.PosID ?? "", bodyJson);

            var (status, body) = await CallApiAsync("InputDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.PosID ?? "", 0, "", body);

            if (status != HttpStatusCode.OK)
            {
                var err = JsonConvert.DeserializeObject<AkaChainErrorResponse>(body);
                return ResponseHelper.MakeResponse(status, err?.Error?.Message ?? status.ToString(), err);
            }

            var data = JsonConvert.DeserializeObject<AddTransactionAkaChainResponse>(body);
            if (data == null)
                return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            LoggingLoyaltyDto? dataLoyaltyRedeem = null;
            if (data.UsedPoint > 0)
            {
                dataLoyaltyRedeem = new()
                {
                    AppCode = "POS",
                    StoreNo = model.MerchantId ?? (model.StoreNo??StringHelper.Left(model.OrderNo, 4)),
                    MemberCardNo = FormatHelper.PhoneNumberVietNam(model.CardNumber),
                    OrderNo = model.OrderNo,
                    TransactionType = model.TransactionType,
                    OrigOrderNo = model.OrigOrderNo,
                    ActionType = model.TransactionType == 1 ? PointActionTypeEnum.REDEEM.ToString() : PointActionTypeEnum.REVERSE_EARN.ToString(),
                    CustName = "",
                    LoyaltyPoints = (long)data.UsedPoint,
                    Transaction = data.ActivityEntityValueId ?? "",
                    Status = status.ToString(),
                    Request = bodyJson,
                    Response = body,
                    Items = null,
                    OrderTime = model.OrderTime,
                    CrtDate = DateTime.Now
                };
            }

            int pointEarn = 0;
            if (data.OfferData != null && data.OfferData.Count > 0)
            {
                foreach (var offer in data.OfferData)
                {
                    if(offer.RuleData != null && offer.RuleData.Count > 0)
                    {
                        foreach (var rule in offer.RuleData) 
                        { 
                            if(rule.LoyaltyCurrency != null && rule.LoyaltyCurrency.Any())
                            {
                                foreach (var currency in rule.LoyaltyCurrency)
                                {
                                    pointEarn += (int)currency.Value;
                                }
                            }
                        }
                    }
                }
            }

            LoggingLoyaltyDto dataLoyalty = new()
            {
                AppCode = "POS",
                StoreNo = model.MerchantId ?? (model.StoreNo ?? StringHelper.Left(model.OrderNo, 4)),
                MemberCardNo = FormatHelper.PhoneNumberVietNam(model.CardNumber),
                OrderNo = model.OrderNo,
                TransactionType = model.TransactionType,
                OrigOrderNo = model.OrigOrderNo,
                ActionType = model.TransactionType == 1 ? PointActionTypeEnum.EARN.ToString() : PointActionTypeEnum.REVERSE_EARN.ToString(),
                CustName = "",
                LoyaltyPoints = (long)pointEarn,
                Transaction = data.ActivityEntityValueId ?? "",
                Status = status.ToString(),
                Request = bodyJson,
                Response = body,
                Items = null,
                OrderTime = model.OrderTime,
                CrtDate = DateTime.Now
            };

            // Fire-and-forget: ghi LoggingLoyalty không block response POS — dùng scope DI riêng
            // (scope của request sẽ dispose ngay khi response trả về), theo mẫu an toàn
            // SyncDataPosController.cs UploadFileSale.
            _ = Task.Run(async () =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ILoyaltyRepository>();
                try
                {
                    if (dataLoyaltyRedeem != null)
                        await repo.InsertLoggingLoyaltyAsync(dataLoyaltyRedeem);
                    await repo.InsertLoggingLoyaltyAsync(dataLoyalty);
                }
                catch (Exception ex)
                {
                    fileLogHelper.WriteExpLogs($"{endpoint}.BackgroundInsertLoggingLoyalty", ex);
                }
            });

            return ResponseHelper.MakeResponse(HttpStatusCode.OK, "Success", AkaChainMapping.MappingAddTransaction(data, model, pointEarn), "AkaChainLoyalty");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return ResponseHelper.MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    /// <inheritdoc/>
    public async Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model)
    {
        const string endpoint = "AkaChainLoyalty.ReturnTransaction";
        try
        {
            var config = await GetFMVConfigAsync();
            var activityCode = config?.Description ?? "";

            var bodyJson = JsonConvert.SerializeObject(AkaChainMapping.MappingReturnInputDataRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.TerminalId ?? "", bodyJson);

            //var (status, body) = await CallApiAsync("InputReturnDataAsync", HttpMethod.Post, bodyJson, null);
            //kibanaService.LogResponse(endpoint, model.TerminalId ?? "", 0, "", body);

            //var data = JsonConvert.DeserializeObject<AddTransactionAkaChainResponse>(body);
            //if (data == null)
            //    return MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            return ResponseHelper.MakeResponse(HttpStatusCode.OK, "Success", AkaChainMapping.MappingReturnTransaction(new object(), model, 0), "AkaChain"); 
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return ResponseHelper.MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    /// <inheritdoc/>
    public async Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model)
    {
        const string endpoint = "AkaChainLoyalty.CheckCoupon";
        try
        {
            var config = await GetFMVConfigAsync();
            var activityCode = config?.Description ?? "";

            var bodyJson = JsonConvert.SerializeObject(AkaChainMapping.MappingCheckCouponRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.PosNo, bodyJson);

            var (status, body) = await CallApiAsync("InputDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.PosNo, 0, "", body);

            if (status != HttpStatusCode.OK)
            {
                var err = StringHelper.StringToObject<AkaChainErrorResponse>(body);
                return ResponseHelper.MakeResponse(status, err?.Error?.Message ?? status.ToString(), err);
            }

            var data = StringHelper.StringToObject<AddTransactionAkaChainResponse>(body);
            if (data == null)
                return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            if (data.CouponCode == null || data.CouponCode.Count == 0)
                return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, "Coupon không hợp lệ", null, JsonConvert.SerializeObject(data));

            var coupon = data.CouponCode.First();
            var couponResult = new VoucherStatusResponse
            {
                Status             = coupon.CanUse ? "SOLD" : "UNSOLD",
                Return             = "0",
                ActicleNo          = "",
                ActicleType        = "",
                Value              = data.TotalDiscount.ToString(),
                VoucherNumber      = coupon.Code,
                Voucher_Currency   = "VND",
                Validity_From_Date = "",
                Expiry_Date        = "",
                CompanyCode        = "",
                Partner            = PartnerEnum.FMV.ToString(),
                IsEmployee         = false,
                PhoneNumber        = model.PhoneNumber,
                VoucherType        = coupon.Type
            };

            return coupon.CanUse
                ? ResponseHelper.MakeResponse(HttpStatusCode.OK, "Success", couponResult, "AkaChainLoyalty.CheckCoupon")
                : ResponseHelper.MakeResponse(HttpStatusCode.BadRequest,
                    $"Coupon {coupon.Code} {coupon.Reason}", couponResult, "AkaChainLoyalty.CheckCoupon");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return ResponseHelper.MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    public async Task<ResultResponse> RegisterMemberAsync(RegisterMemberDto model)
    {
        const string endpoint = "AkaChainLoyalty.RegisterMember";
        try
        {
            var config = await GetFMVConfigAsync();
            var enrollment = "Enrollment_8046_1739180176";

            var bodyJson = JsonConvert.SerializeObject(AkaChainMapping.MappingInputMemberDataAsync(model, enrollment));
            kibanaService.LogRequest(endpoint, model.PosCode ?? "", bodyJson);

            var (status, body) = await CallApiAsync("InputMemberDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.PosCode ?? "", 0, "", body);

            var data = StringHelper.StringToObject<InputMemberDataAkaChainResponse>(body);
            if (data == null)
            {
                if (status == HttpStatusCode.NotFound)
                {
                    return ResponseHelper.MakeResponse(HttpStatusCode.BadRequest, $"{status} - JSON conversion error", null, $"httpsStatus: {status} - {body}");
                }
                return ResponseHelper.MakeResponse(status, $"{status} - JSON conversion error", null, body);
            }   

            return ResponseHelper.MakeResponse(HttpStatusCode.OK, "Success", data, "AkaChain");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return ResponseHelper.MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }
}

using System.Net;
using System.Text;
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
using POS.Infrastructure.AppServices.Interfaces;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Infrastructure.AppServices;

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
    IFileLogHelper fileLogHelper
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

    // ── OAuth2 Token ──────────────────────────────────────────────────────────

    private async Task<string?> GetAccessTokenAsync()
    {
        // Redis cache hit
        var cached = redis.StringGetRaw(TokenCacheKey);
        if (!string.IsNullOrEmpty(cached)) return cached;

        try
        {
            var config = await GetFMVConfigAsync();
            if (config == null) return null;

            var tokenRoute = GetRoute(config, "GetToken");
            if (tokenRoute == null) return null;

            var client = httpClientFactory.CreateClient("FMV");
            // Cookie: __tenant dùng UserName (= client_id) — giữ nguyên behavior cũ
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Cookie", $".AspNetCore.Culture=c%3Dvi%7Cuic%3Dvi; __tenant={config.UserName}");

            var form = new Dictionary<string, string>
            {
                { "grant_type",    "client_credentials" },
                { "client_id",     config.UserName ?? "" },
                { "client_secret", config.Password ?? "" },
                { "scope",         "CustomerJourneyService MasterDataService MemberService TransactionService" }
            };

            var response = await client.PostAsync(config.Host + tokenRoute, new FormUrlEncodedContent(form));
            var body     = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(body)) return null;

            var tokenData = JsonConvert.DeserializeObject<AccessTokenDataAkaChain>(body);
            if (tokenData?.Access_token == null) return null;

            var expiresIn = int.TryParse(tokenData.Expires_in, out var e) ? Math.Max(e - 60, 60) : 240;
            redis.StringSetRaw(TokenCacheKey, tokenData.Access_token, TimeSpan.FromSeconds(expiresIn));

            return tokenData.Access_token;
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
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

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
                return MakeResponse(status, status.ToString(), body);

            var data = JsonConvert.DeserializeObject<MemberProfileAkaChain>(body);
            if (data == null)
                return MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", body);

            return MakeResponse(HttpStatusCode.OK, "Success", MappingInfoMember(data), "AkaChainLoyalty");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
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

            var bodyJson = JsonConvert.SerializeObject(MappingInputDataRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.PosID ?? "", bodyJson);

            var (status, body) = await CallApiAsync("InputDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.PosID ?? "", 0, "", body);

            if (status != HttpStatusCode.OK)
            {
                var err = JsonConvert.DeserializeObject<AkaChainErrorResponse>(body);
                return MakeResponse(status, err?.Error?.Message ?? status.ToString(), err);
            }

            var data = JsonConvert.DeserializeObject<AddTransactionAkaChainResponse>(body);
            if (data == null)
                return MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            return MakeResponse(HttpStatusCode.OK, "Success", MappingAddTransaction(data, model), "AkaChainLoyalty");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
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

            var bodyJson = JsonConvert.SerializeObject(MappingReturnInputDataRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.TerminalId ?? "", bodyJson);

            var (status, body) = await CallApiAsync("InputReturnDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.TerminalId ?? "", 0, "", body);

            // Giữ nguyên behavior cũ: OK case trả BadRequest (branch OK bị comment trong code gốc)
            if (status == HttpStatusCode.OK)
                return MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            var err = JsonConvert.DeserializeObject<AkaChainErrorResponse>(body);
            return MakeResponse(status, err?.Error?.Message ?? status.ToString(), err);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
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

            var bodyJson = JsonConvert.SerializeObject(MappingCheckCouponRequest(model, activityCode));
            kibanaService.LogRequest(endpoint, model.PosNo, bodyJson);

            var (status, body) = await CallApiAsync("InputDataAsync", HttpMethod.Post, bodyJson, null);
            kibanaService.LogResponse(endpoint, model.PosNo, 0, "", body);

            if (status != HttpStatusCode.OK)
            {
                var err = JsonConvert.DeserializeObject<AkaChainErrorResponse>(body);
                return MakeResponse(status, err?.Error?.Message ?? status.ToString(), err);
            }

            var data = JsonConvert.DeserializeObject<AddTransactionAkaChainResponse>(body);
            if (data == null)
                return MakeResponse(HttpStatusCode.BadRequest, "JSON conversion error", null, body);

            if (data.CouponCode == null || !data.CouponCode.Any())
                return MakeResponse(HttpStatusCode.BadRequest, "Coupon không hợp lệ", null, JsonConvert.SerializeObject(data));

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
                ? MakeResponse(HttpStatusCode.OK, "Success", couponResult, "AkaChainLoyalty.CheckCoupon")
                : MakeResponse(HttpStatusCode.BadRequest,
                    $"Coupon {coupon.Code} {coupon.Reason}", couponResult, "AkaChainLoyalty.CheckCoupon");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            return MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    // ── Mapping (migrated from AkaChainMapping static class) ─────────────────

    private static InfoMemberModel MappingInfoMember(MemberProfileAkaChain profile) => new()
    {
        CardNumber      = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
        VirtualCard     = profile.ReferralCode,
        CMND            = "",
        MemberName      = profile.FullName,
        Title           = "",
        CardLevel       = "",
        MemberCSN       = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
        PhoneNumber     = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
        OtherInfo       = "",
        QRCode          = "",
        Dob             = "",
        DateOfBirth     = "",
        BirthdayGiftInd = false,
        MemberPoint     = profile.TotalPoint,
        TotalPoint      = profile.TotalPoint,
        RedemptionValue = profile.TotalPoint,
        ExtraPoint      = false,
        CurrentRate     = 0,
        IsOfflineVinID  = false,
        IsShowMessage   = false,
        IsRedeem        = true,
        Status          = "Hoạt động",
        System          = MemberCapillaryEnum.FMV.ToString(),
        ClubCode        = MemberCapillaryEnum.FMV.ToString(),
        Email           = "",
        Gender          = "",
        Address         = "",
        ExternalId      = "",
        AvailablePromotion = null,
        MemberBusiness  = null,
        OtherStatus     = null,
        ExtendedFields  = null,
        MemberType      = MemberCapillaryEnum.FMV.ToString(),
        Source          = null,
        PointsSummaries = null
    };

    private static PointModePOSResponse MappingAddTransaction(
        AddTransactionAkaChainResponse response, VinIDSalesRequest model)
    {
        long pointEarn = 0;
        var extraEarn  = new List<PointModePOSData>();

        if (response.RewardBalance?.Any() == true)
        {
            foreach (var pair in response.RewardBalance)
            {
                pointEarn += (long)pair.Value;
                extraEarn.Add(new PointModePOSData
                {
                    LoyaltyMerchantId = pair.CurrencyId,
                    Amount            = 0,
                    EarnedPoints      = (int)pair.Value,
                    EntityType        = response.State,
                    Type              = pair.CurrencyName
                });
            }
        }

        return new PointModePOSResponse
        {
            PointEarn           = pointEarn,
            PointRedeem         = (long)response.UsedPoint,
            RedemptionValue     = (long)response.MemberBalance,
            Balance             = response.MemberBalance > 0 ? (long?)response.MemberBalance : null,
            CurrentRate         = 0,
            IsOfflineVinID      = false,
            EmpCode             = null,
            MasanerPackageInd   = null,
            StaffPercentage     = null,
            NormCustPercentage  = null,
            RedemptionId        = null,
            ReversalId          = null,
            OrderNo             = model.OrderNo,
            CreatedId           = response.ActivityEntityValueId,
            ExtraEarnByCampaign = extraEarn,
            TransLine           = null,
            StatusCode          = 200
        };
    }

    private static AddTransactionAkaChainRequest MappingInputDataRequest(
        VinIDSalesRequest model, string activityCode)
    {
        var cartItems = (model.TransLine ?? []).Select(item => new CartItemAkaChain
        {
            ProductCode     = item.ItemCode ?? "",
            ProductName     = item.Description ?? "",
            ProductCategory = item.Size ?? "",
            Quantity        = item.Quantity,
            UnitPrice       = item.UnitPrice,
            TotalAmount     = item.LineAmountIncVAT
        }).ToList();

        return new AddTransactionAkaChainRequest
        {
            MemberKeys = new MemberKeysAkaChain
            {
                Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.CardNumber)
            },
            UsePoint             = 0,
            IsSimulation         = false,
            SimulationOfferId    = null,
            CustomEntityDataId   = null,
            ActivityCode         = activityCode,
            State                = "Open",
            CouponCode           = [],
            ActivityData = new ActivityDataAkaChain
            {
                BusinessTime      = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                Description       = $"Bán hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                TotalCalcAmount   = model.BillAmount,
                OrderCode         = model.OrderNo,
                CartItems         = cartItems,
                StoreCode         = model.MerchantId ?? "",
                OriginalOrderCode = null
            },
            TouchPointCode          = null,
            UseBasePromotionSchemes = false
        };
    }

    private static InputReturnDataRequest MappingReturnInputDataRequest(
        VinIDRefundRequest model, string activityCode)
    {
        var cartItems = (model.TransLine ?? []).Select(item => new CartItemAkaChain
        {
            ProductCode     = item.ItemCode ?? "",
            ProductName     = item.Description ?? "",
            ProductCategory = item.Size ?? "",
            Quantity        = item.Quantity,
            UnitPrice       = item.UnitPrice,
            TotalAmount     = item.LineAmountIncVAT
        }).ToList();

        return new InputReturnDataRequest
        {
            MemberKeys = new ReturnMemberKeysAkaChain
            {
                PartnerLoyaltyId = FormatHelper.PhoneNumberVietNam(model.CardNumber)
            },
            ActivityCode = activityCode,
            State        = model.TransactionType == 2
                ? ReturnState.FullReturn.ToString()
                : ReturnState.PartialReturn.ToString(),
            ActivityData = new ActivityDataAkaChain
            {
                BusinessTime      = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                Description       = $"Trả hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                TotalCalcAmount   = model.RefundAmount,
                OrderCode         = model.OrderNo ?? "",
                CartItems         = cartItems,
                StoreCode         = model.MerchantId ?? "",
                OriginalOrderCode = model.OrigOrderNo,
                IsReturn          = true
            }
        };
    }

    private static AddTransactionAkaChainRequest MappingCheckCouponRequest(
        CheckVoucherPartnerPOSRequest model, string activityCode)
    {
        decimal totalAmount = 0;
        var cartItems = (model.Items ?? []).Select(item =>
        {
            totalAmount += item.LineAmount;
            return new CartItemAkaChain
            {
                ProductCode     = item.ItemNo ?? "",
                ProductName     = "",
                ProductCategory = "",
                Quantity        = item.Qty,
                UnitPrice       = item.UnitPrice,
                TotalAmount     = item.LineAmount
            };
        }).ToList();

        var coupons = (model.SerialNo ?? []).Select(c => new CouponDetailAkaChain
        {
            CanUse      = true,
            Code        = c,
            Description = "",
            Status      = "Use"
        }).ToList();

        var now = DateTime.Now;
        return new AddTransactionAkaChainRequest
        {
            MemberKeys = new MemberKeysAkaChain
            {
                Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.PhoneNumber ?? "")
            },
            UsePoint     = 0,
            ActivityCode = activityCode,
            State        = "Open",
            CouponCode   = coupons,
            ActivityData = new ActivityDataAkaChain
            {
                BusinessTime      = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                Description       = $"Bán hàng ngày {now:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                TotalCalcAmount   = totalAmount,
                OrderCode         = Guid.NewGuid().ToString(),
                CartItems         = cartItems,
                StoreCode         = model.StoreNo,
                OriginalOrderCode = null
            },
            TouchPointCode          = null,
            UseBasePromotionSchemes = false
        };
    }

    // ── Response helper ───────────────────────────────────────────────────────

    private static ResultResponse MakeResponse(
        HttpStatusCode status, string message, object? data, string technical = "")
        => new() { Status = status, Message = message, Data = data, MessageTechnical = technical };
}

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Loyalty.DTOs.Capillary;
using POS.Application.Loyalty.Services;
using POS.Application.Shared.DTOs;

namespace POS.Infrastructure.External.Capillary;

public class LoyaltyCapillaryHttpService : ILoyaltyCapillaryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LoyaltyCapillaryHttpService> _logger;

    public LoyaltyCapillaryHttpService(
        IHttpClientFactory httpClientFactory,
        ILogger<LoyaltyCapillaryHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ── GetCustomerDetail ──────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapGetCustomerResponse? Data, int StatusCode)>
        GetCustomerDetailAsync(SysWebApiDto config, string storeNo, string posNo, string phoneOrId, string memberType)
    {
        var requestId = Guid.NewGuid().ToString();
        var route = config.GetRoute("GetCustomer");
        if (route == null)
            return (false, "Không tìm thấy route GetCustomer trong cấu hình Capillary", null, 401);

        string param = memberType == "ID"
            ? $"?format=json&id={Uri.EscapeDataString(phoneOrId)}{route.Notes ?? ""}&user_id=true&store_id={storeNo}"
            : $"?format=json&mobile={Uri.EscapeDataString(phoneOrId)}{route.Notes ?? ""}&user_id=true&store_id={storeNo}";

        var extraHeaders = new Dictionary<string, string> { { "X-API-REQUEST", requestId } };

        try
        {
            var (raw, httpStatus) = await CallAsync(config, storeNo, route.Route + param, "GET", null, extraHeaders);

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null, 400);

            var data = JsonConvert.DeserializeObject<CapGetCustomerResponse>(raw);
            if (data?.Response == null)
                return (false, raw, null, 400);

            int statusCode = data.Response.Status?.Code ?? 400;
            if (statusCode == 200)
                return (true, data.Response.Status?.Message ?? "Success", data, 200);

            return (false, data.Response.Status?.Message ?? raw, data, statusCode);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("GetCustomerDetailAsync timeout requestId={RequestId} posNo={PosNo}", requestId, posNo);
            return (false, $"RequestId: {requestId} 408_requestTimedOut", null, 408);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCustomerDetailAsync error requestId={RequestId} posNo={PosNo}", requestId, posNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null, 401);
            return (false, $"RequestId: {requestId} Exception: {ex.Message}", null, 400);
        }
    }

    // ── CustomerRegistration ───────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapCustomerRegistrationResponse? Data)>
        CustomerRegistrationAsync(SysWebApiDto config, CapCustomerRegistrationRequest customer, string storeNo)
    {
        var route = config.GetRoute("CustomerRegistration");
        if (route == null)
            return (false, "Không tìm thấy route CustomerRegistration trong cấu hình Capillary", null);

        var extraHeaders = new Dictionary<string, string>
        {
            { "X-CAP-API-ATTRIBUTION-LOOKUP", "masan.vn.integration.pos" }
        };

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route + (route.Notes ?? ""), "POST",
                ToLowerCaseJson(customer), extraHeaders);

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapCustomerRegistrationResponse>(raw);
            if (data?.CreatedId > 0)
                return (true, "Đăng ký thành viên thành công", data);

            var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(err, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomerRegistrationAsync error storeNo={StoreNo}", storeNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── CustomerUpdate ─────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapCustomerRegistrationResponse? Data)>
        CustomerUpdateAsync(SysWebApiDto config, CapCustomerUpdateRequest customer, string storeNo, string phoneNumber)
    {
        var route = config.GetRoute("UpdateCustomer");
        if (route == null)
            return (false, "Không tìm thấy route UpdateCustomer trong cấu hình Capillary", null);

        var param = $"?source=INSTORE&identifierName=mobile&identifierValue={Uri.EscapeDataString(phoneNumber)}";

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route + param, "PUT", ToLowerCaseJson(customer));

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapCustomerRegistrationResponse>(raw);
            if (data?.CreatedId > 0)
                return (true, "Cập nhật thông tin thành công", data);

            var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(err, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomerUpdateAsync error storeNo={StoreNo}", storeNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── AddTransaction ─────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapAddTransactionResponse? Data)>
        AddTransactionAsync(SysWebApiDto config, object transaction, string storeNo, string memberCard,
            string orderNo = "", List<string>? programIds = null)
    {
        var route = config.GetRoute("AddTransaction");
        if (route == null)
            return (false, "Không tìm thấy route AddTransaction trong cấu hình Capillary", null);

        string param = $"?source=instore&identifierValue={Uri.EscapeDataString(memberCard)}&identifierName=mobile{route.Notes ?? ""}";
        if (programIds?.Count > 0 && !string.IsNullOrEmpty(programIds[0]))
            param = $"?source=instore&identifierValue={Uri.EscapeDataString(memberCard)}&identifierName=mobile&program_id={programIds[0]}{route.Notes ?? ""}";

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route + param, "POST", ToLowerCaseJson(transaction));

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapAddTransactionResponse>(raw);
            if (data?.CreatedId > 0)
            {
                if (data.Warnings == null || data.Warnings.Count == 0)
                    return (true, "Success", data);

                var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
                if (err?.Errors?.Count > 0)
                    return (false, ExtractErrorMessage(err, raw), null);

                return (false, $"Lỗi: {raw}", data);
            }

            var errResp = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(errResp, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddTransactionAsync error storeNo={StoreNo} orderNo={OrderNo}", storeNo, orderNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── GetPointsLedger ────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapLedgerResponse? Data)>
        GetPointsLedgerAsync(SysWebApiDto config, string storeNo, string memberCard)
    {
        var route = config.GetRoute("GetPointsLedger");
        if (route == null)
            return (false, "Không tìm thấy route GetPointsLedger trong cấu hình Capillary", null);

        var param = $"?identifierValue={Uri.EscapeDataString(memberCard)}{route.Notes ?? ""}";
        var extraHeaders = new Dictionary<string, string> { { "X-API-REQUEST", Guid.NewGuid().ToString() } };

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route + param, "GET", null, extraHeaders);

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapLedgerResponse>(raw);
            if (data != null)
                return (true, "Success", data);

            var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(err, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPointsLedgerAsync error storeNo={StoreNo}", storeNo);
            return (false, ex.Message, null);
        }
    }

    // ── PointsRedeem ───────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapPointRedeemResponse? Data)>
        PointsRedeemAsync(SysWebApiDto config, CapPointsRedeemRequest request, string storeNo, string programId)
    {
        var route = config.GetRoute("RedeemPoints");
        if (route == null)
            return (false, "Không tìm thấy route RedeemPoints trong cấu hình Capillary", null);

        string param = route.Route + (route.Notes ?? "");
        if (!string.IsNullOrEmpty(programId))
            param += $"&program_id={programId}";

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, param, "POST", ToLowerCaseJson(request));

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapPointRedeemResponse>(raw);
            if (data?.Response?.Status != null)
            {
                string statusSuccess = data.Response.Status.Success ?? "";
                if (data.Response.Status.Code == 200 && statusSuccess.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                    return (true, data.Response.Status.Message ?? "Success", data);

                string itemMsg = data.Response.Responses?.Points?.Item_status?.Message ?? data.Response.Status.Message ?? raw;
                return (false, itemMsg, data);
            }

            var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(err, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PointsRedeemAsync error storeNo={StoreNo}", storeNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── PointReverse ───────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapPointReverseResponse? Data)>
        PointReverseAsync(SysWebApiDto config, CapPointReverseRequest request, string storeNo)
    {
        var route = config.GetRoute("PointReverse");
        if (route == null)
            return (false, "Không tìm thấy route PointReverse trong cấu hình Capillary", null);

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route, "POST", ToLowerCaseJson(request));

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapPointReverseResponse>(raw);
            if (data?.OrgId > 0)
                return (true, "Success", data);

            var err = JsonConvert.DeserializeObject<CapErrorResponse>(raw);
            return (false, ExtractErrorMessage(err, raw), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PointReverseAsync error storeNo={StoreNo}", storeNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── PointsRedeemable ───────────────────────────────────────────────────

    public async Task<(bool Success, string Message, CapPointRedeemableResponse? Data)>
        PointsRedeemableAsync(SysWebApiDto config, string memberCard, int point, string storeNo)
    {
        var route = config.GetRoute("PointsRedeemable");
        if (route == null)
            return (false, "Không tìm thấy route PointsRedeemable trong cấu hình Capillary", null);

        var param = $"?mobile={Uri.EscapeDataString(memberCard)}&format=json&issue_otp=false&points={point}";

        try
        {
            var (raw, _) = await CallAsync(config, storeNo, route.Route + param, "GET", null);

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null);

            var data = JsonConvert.DeserializeObject<CapPointRedeemableResponse>(raw);
            if (data?.Response == null)
                return (false, raw, null);

            if (data.Response.Status?.Code == 200 &&
                data.Response.Points?.Redeemable?.Is_redeemable == "true")
                return (true, "Success", data);

            if (data.Response.Status?.Code == 500)
                return (false, $"Số điểm hiện tại ít hơn số điểm yêu cầu quy đổi {point}", data);

            return (false, data.Response.Status?.Message ?? raw, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PointsRedeemableAsync error storeNo={StoreNo}", storeNo);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", null);
            return (false, ex.Message, null);
        }
    }

    // ── RedemptionValidationData ───────────────────────────────────────────

    public async Task<(bool Success, string Message, CapRedemptionResponse? Data, int StatusCode)>
        RedemptionValidationDataAsync(SysWebApiDto config, string store, string mobile, string code, bool details)
    {
        var route = config.GetRoute("CouponIsRedemption");
        if (route == null)
            return (false, "Không tìm thấy route CouponIsRedemption trong cấu hình Capillary", null, 401);

        var param = $"?mobile={Uri.EscapeDataString(mobile)}&code={Uri.EscapeDataString(code)}&details={details.ToString().ToLower()}";

        try
        {
            var (raw, _) = await CallAsync(config, store, route.Route + param, "GET", null);

            if (string.IsNullOrEmpty(raw))
                return (false, "Lỗi kết nối tới Capillary API", null, 400);

            var data = JsonConvert.DeserializeObject<CapRedemptionResponse>(raw);
            if (data == null)
                return (false, raw, null, 400);

            int statusCode = data.RedemptionStatus?.Code ?? 400;
            bool isRedeemable = data.Redemption?.FirstOrDefault()?.IsRedeemable == true;

            if (data.RedemptionStatus?.Status == true && isRedeemable)
            {
                string codeValue = data.Redemption!.First().Code ?? "";
                return (true, $"{codeValue} - {data.RedemptionStatus.Message ?? "Success"}", data, statusCode);
            }

            return (false, data.RedemptionStatus?.Message ?? raw, data, statusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedemptionValidationDataAsync error store={Store}", store);
            if (IsUnauthorized(ex.Message))
                return (false, $"(401) Unauthorized. Store {store} chưa được tạo trên Capillary", null, 401);
            return (false, ex.Message, null, 400);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<(string Raw, int HttpStatus)> CallAsync(
        SysWebApiDto config, string storeNo, string relativeUrl, string method,
        string? body, Dictionary<string, string>? extraHeaders = null)
    {
        int timeout = int.TryParse(config.Version, out var t) && t > 0 ? t : 30;
        var token = CreateToken(config.UserName, storeNo, config.Password);

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(config.Host);
        client.Timeout = TimeSpan.FromSeconds(timeout);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER");
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-winx-loyalty-client", "POS");

        if (extraHeaders != null)
            foreach (var (k, v) in extraHeaders)
                client.DefaultRequestHeaders.TryAddWithoutValidation(k, v);

        _logger.LogInformation("LoyaltyCapillary {Method} storeNo={StoreNo} url={Url}", method, storeNo, relativeUrl);

        HttpResponseMessage response = method switch
        {
            "PUT" => await client.PutAsync(relativeUrl,
                new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json")),
            "POST" => await client.PostAsync(relativeUrl,
                new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json")),
            _ => await client.GetAsync(relativeUrl)
        };

        var raw = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("LoyaltyCapillary {Method} storeNo={StoreNo} httpStatus={HttpStatus}",
            method, storeNo, (int)response.StatusCode);
        return (raw, (int)response.StatusCode);
    }

    private static string CreateToken(string userName, string storeNo, string password)
    {
        var plain = $"{userName}.{storeNo}:{Md5Hex(password)}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
    }

    private static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        return string.Concat(md5.ComputeHash(Encoding.ASCII.GetBytes(input)).Select(b => b.ToString("x2")));
    }

    /// <summary>Lowercase JSON property names only (values unchanged) — required by Capillary API.</summary>
    private static string ToLowerCaseJson(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        return Regex.Replace(json, @"""(\w+)"":", m => $"\"{m.Groups[1].Value.ToLower()}\":");
    }

    private static string ExtractErrorMessage(CapErrorResponse? err, string raw)
    {
        if (err?.Errors?.Count > 0)
            return JsonConvert.SerializeObject(err.Errors.First());
        if (err?.Warnings?.Count > 0)
            return JsonConvert.SerializeObject(err.Warnings.First());
        return $"Lỗi kết quả trả về từ Capillary API: {raw}";
    }

    private static bool IsUnauthorized(string message) =>
        message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
}

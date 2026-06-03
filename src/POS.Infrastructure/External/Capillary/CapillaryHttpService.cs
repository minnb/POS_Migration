using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Gift.Services;
using POS.Application.Payment.DTOs;
using POS.Application.Payment.Services;
using POS.Application.Shared.DTOs;
using POS.Application.Shared.Services;
using POS.Shared.Models;

namespace POS.Infrastructure.External.Capillary;

public class CapillaryHttpService : ICapillaryCouponService
{
    private const string CapAppCode = "CAPILLARY";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISysWebApiConfigService _configService;
    private readonly IWinXService _winXService;
    private readonly ILogger<CapillaryHttpService> _logger;

    public CapillaryHttpService(
        IHttpClientFactory httpClientFactory,
        ISysWebApiConfigService configService,
        IWinXService winXService,
        ILogger<CapillaryHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _winXService = winXService;
        _logger = logger;
    }

    public async Task<(int HttpStatus, ResultResponse Body)> ValidateCouponAsync(CheckVoucherPartnerRequest request)
    {
        if (string.IsNullOrEmpty(request.PhoneNumber))
            return (400, Err(400, "Nhập số điện thoại hội viên WIN để sử dụng Coupon"));
        if (request.SerialNo.Count > 1)
            return (400, Err(400, "Giới hạn 1 coupon mỗi lần kiểm tra"));

        var dto = await _configService.GetByAppCodeAsync(CapAppCode);
        if (dto == null) return (400, Err(400, "Không tìm thấy cấu hình Capillary"));

        var route = dto.GetRoute("ValidateCoupon");
        if (route == null) return (400, Err(400, "Không tìm thấy route ValidateCoupon của Capillary"));

        var phone = FormatPhoneWithCountryCode(request.PhoneNumber);
        var code = request.SerialNo[0];

        // WinX dynamic voucher: code starts with "WDV", length 25
        if (code.Length == 25 && code.StartsWith("WDV", StringComparison.OrdinalIgnoreCase))
        {
            var (resolved, msg) = await _winXService.ResolveDynamicVouchersAsync(request.PosNo, code);
            if (string.IsNullOrEmpty(resolved))
                return (400, new ResultResponse { Status = 400, Message = msg, Data = null });
            code = resolved;
        }

        try
        {
            var param = $"?mobile={Uri.EscapeDataString(phone)}&code={Uri.EscapeDataString(code)}&details=extended&format=json";
            var (raw, _) = await CallCapillaryAsync(dto, route.Route + param, request.PosNo, "ValidateCoupon", "GET", null);

            if (string.IsNullOrEmpty(raw))
                return (400, Err(400, "Lỗi kết nối Capillary"));

            var resp = JsonConvert.DeserializeObject<CapValidateResponse>(raw);
            if (resp?.Response?.Status?.Code == 200 && resp.Response.Coupons?.Redeemable?.Series_info != null)
            {
                var rd = resp.Response.Coupons.Redeemable;

                if (string.IsNullOrEmpty(rd.Series_info.Description))
                    return (400, Err(400, $"Voucher/coupon {code} không áp dụng cho sản phẩm"));

                var matchingItem = request.Items?.FirstOrDefault(x => x.Barcode == rd.Series_info.Description);
                if (matchingItem == null)
                    return (400, Err(400, "Đơn hàng không có phẩm được áp dụng Coupon"));

                var data = new ValidateCouponDataResponse
                {
                    Mobile = rd.Mobile ?? string.Empty,
                    DiscountCode = rd.Series_info.Discount_code ?? string.Empty,
                    DiscountType = rd.Series_info.Discount_type ?? string.Empty,
                    DiscountValue = rd.Series_info.Discount_value,
                    ValidTillDate = rd.Series_info.Valid_till_date ?? string.Empty,
                    IsRedeemable = string.Equals(rd.Is_redeemable, "true", StringComparison.OrdinalIgnoreCase),
                    IsApplySku = !string.IsNullOrEmpty(rd.Series_info.Description),
                    DiscountApply = 0,
                    Items = new List<ItemApplyCoupon>
                    {
                        new()
                        {
                            ItemNo = string.Empty,
                            Barcode = rd.Series_info.Description,
                            ExpiryDate = rd.Series_info.Terms_and_condition ?? string.Empty,
                            SellDate = string.Empty,
                            ItemName = string.Empty
                        }
                    }
                };
                return (200, new ResultResponse { Status = 200, Message = "Success", Data = data });
            }

            // Error response from Capillary
            var errResp = JsonConvert.DeserializeObject<CapValidateErrorResponse>(raw);
            if (errResp?.Response?.Coupons?.Redeemable?.Item_status != null)
                return (400, new ResultResponse { Status = 400, Message = $"Voucher/coupon {code} không hợp lệ", Data = null, MessageTechnical = raw });

            return (400, new ResultResponse { Status = 400, Message = raw, Data = null, MessageTechnical = raw });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapillaryHttpService.ValidateCouponAsync failed posNo={PosNo} code={Code}", request.PosNo, code);
            return (400, Err(400, ex.Message, JsonConvert.SerializeObject(ex)));
        }
    }

    public async Task<(int HttpStatus, ResultResponse Body)> RedeemCouponAsync(UpdateStatusVoucherPartnerRequest request)
    {
        if (string.IsNullOrEmpty(request.PhoneNumber))
            return (400, Err(400, "Nhập số điện thoại hội viên WIN để sử dụng voucher"));

        var dto = await _configService.GetByAppCodeAsync(CapAppCode);
        if (dto == null) return (400, Err(400, "Không tìm thấy cấu hình Capillary"));

        var route = dto.GetRoute("CouponRedeem");
        if (route == null) return (400, Err(400, "Không tìm thấy route CouponRedeem của Capillary"));

        var phone = FormatPhoneWithCountryCode(request.PhoneNumber);
        var bodyObj = new
        {
            redemptionRequestList = request.SerialNo.Select(s => new { code = s.Code }).ToList(),
            transactionNumber = request.OrderNo,
            user = new { mobile = phone }
        };
        var bodyJson = JsonConvert.SerializeObject(bodyObj);
        var routeWithNotes = route.Route + (route.Notes ?? string.Empty);
        var requestId = Guid.NewGuid().ToString();

        try
        {
            var (raw, _) = await CallCapillaryAsync(dto, routeWithNotes, request.PosNo, "CouponRedeem", "POST", bodyJson,
                new Dictionary<string, string> { { "X-API-REQUEST", requestId } });

            if (string.IsNullOrEmpty(raw))
                return (400, Err(400, "Lỗi kết nối Capillary"));

            var resp = JsonConvert.DeserializeObject<CapRedeemResponse>(raw);
            if (resp?.Redemption != null && resp.Redemption.Count > 0)
            {
                var codeDone = new List<DataVoucherPartnerResponse>();
                foreach (var item in resp.Redemption)
                {
                    if (item.RedemptionStatus?.Code == 700 && item.RedemptionStatus.Success)
                    {
                        codeDone.Add(new DataVoucherPartnerResponse
                        {
                            Amount = (int)item.CouponValue,
                            Code = item.Code ?? string.Empty,
                            Msg = item.RedemptionStatus.Message ?? string.Empty,
                            Status = item.RedemptionStatus.Code.ToString()
                        });
                    }
                    else
                    {
                        var codeError = new DataVoucherPartnerResponse
                        {
                            Amount = (int)item.CouponValue,
                            Code = item.Code ?? string.Empty,
                            Msg = item.RedemptionStatus?.Message ?? string.Empty,
                            Status = item.RedemptionStatus?.Code.ToString() ?? "0"
                        };
                        return (400, new ResultResponse { Status = 400, Data = new List<DataVoucherPartnerResponse> { codeError }, Message = $"Voucher {item.Code} không hợp lệ" });
                    }
                }
                return (200, new ResultResponse { Status = 200, Data = codeDone, Message = resp.Redemption[0].RedemptionStatus?.Message ?? "OK" });
            }

            return (400, new ResultResponse { Status = 400, Message = raw, Data = null, MessageTechnical = raw });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapillaryHttpService.RedeemCouponAsync failed posNo={PosNo} orderNo={OrderNo}", request.PosNo, request.OrderNo);
            return (503, Err(503, ex.Message));
        }
    }

    public async Task<(int HttpStatus, ResultResponse Body)> GetListCouponByUserAsync(string storeNo, string mobile, string status)
    {
        var dto = await _configService.GetByAppCodeAsync(CapAppCode);
        if (dto == null) return (400, Err(400, "Không tìm thấy cấu hình Capillary"));

        var route = dto.GetRoute("GetCouponsByUser");
        if (route == null) return (400, Err(400, "Không tìm thấy route GetCouponsByUser của Capillary"));

        try
        {
            var param = $"?mobile={Uri.EscapeDataString(mobile)}&status={status.ToUpper()}{route.Notes}";
            var (raw, _) = await CallCapillaryAsync(dto, route.Route + param, storeNo, "GetListCouponByUser", "GET", null);

            if (string.IsNullOrEmpty(raw))
                return (400, Err(400, "Lỗi kết nối Capillary"));

            var resp = JsonConvert.DeserializeObject<CapCouponByUserResponse>(raw);
            if (resp?.Entity?.Customers != null && resp.Entity.Customers.Count > 0)
                return (200, new ResultResponse { Status = 200, Message = "OK", Data = resp.Entity.Customers });

            return (400, new ResultResponse { Status = 400, Message = raw, Data = null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapillaryHttpService.GetListCouponByUserAsync failed storeNo={StoreNo} mobile={Mobile}", storeNo, mobile);
            return (400, Err(400, ex.Message));
        }
    }

    public async Task<(int HttpStatus, ResultResponse Body)> GetCouponDetailAsync(string storeNo, string serialNo)
    {
        var dto = await _configService.GetByAppCodeAsync(CapAppCode);
        if (dto == null) return (400, Err(400, "Không tìm thấy cấu hình Capillary"));

        var route = dto.GetRoute("GetCouponDetails");
        if (route == null) return (400, Err(400, "Không tìm thấy route GetCouponDetails của Capillary"));

        try
        {
            var param = $"?code={Uri.EscapeDataString(serialNo)}{route.Notes}";
            var (raw, _) = await CallCapillaryAsync(dto, route.Route + param, storeNo, "GetCouponDetail", "GET", null);

            if (string.IsNullOrEmpty(raw))
                return (400, Err(400, "Lỗi kết nối Capillary"));

            var resp = JsonConvert.DeserializeObject<CapCouponDetailResponse>(raw);
            if (resp?.Response?.Status?.Code == 200)
            {
                var detail = resp.Response.Coupons?.Coupon?.FirstOrDefault();
                return detail != null
                    ? (200, new ResultResponse { Status = 200, Message = "OK", Data = detail })
                    : (400, new ResultResponse { Status = 400, Message = raw, Data = null });
            }

            return (400, new ResultResponse { Status = 400, Message = raw, Data = null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapillaryHttpService.GetCouponDetailAsync failed storeNo={StoreNo} serialNo={SerialNo}", storeNo, serialNo);
            return (400, Err(400, ex.Message));
        }
    }

    public async Task<(int HttpStatus, ResultResponse Body)> ReActivateCouponsAsync(ReActiveCouponRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync(CapAppCode);
        if (dto == null) return (400, Err(400, "Không tìm thấy cấu hình Capillary"));

        var route = dto.GetRoute("ReActivateCoupons");
        if (route == null) return (400, Err(400, "Không tìm thấy route ReActivateCoupons của Capillary"));

        var bodyJson = JsonConvert.SerializeObject(new { redemptionIds = request.SerialNo });

        try
        {
            var (raw, _) = await CallCapillaryAsync(dto, route.Route, request.StoreNo, "ReActivateCoupons", "POST", bodyJson);

            if (string.IsNullOrEmpty(raw))
                return (400, Err(400, "Lỗi kết nối Capillary"));

            var resp = JsonConvert.DeserializeObject<CapReActivateResponse>(raw);
            if (resp?.Success == true)
                return (200, new ResultResponse { Status = 200, Message = "OK", Data = null });

            var errMsg = resp?.Errors?.FirstOrDefault() is { } err
                ? $"{err.Code} - {err.Message}"
                : "Error";
            return (400, new ResultResponse { Status = 400, Message = errMsg, Data = null, MessageTechnical = raw });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapillaryHttpService.ReActivateCouponsAsync failed storeNo={StoreNo}", request.StoreNo);
            return (400, Err(400, ex.Message));
        }
    }

    private async Task<(string Raw, int HttpStatus)> CallCapillaryAsync(
        SysWebApiDto dto, string relativeUrl, string storeNo, string function,
        string method, string? body, Dictionary<string, string>? extraHeaders = null)
    {
        int timeout = int.TryParse(dto.Version, out var t) && t > 0 ? t : 30;
        var token = CreateCapillaryToken(dto.UserName, storeNo, dto.Password);

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(dto.Host);
        client.Timeout = TimeSpan.FromSeconds(timeout);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER");
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-winx-loyalty-client", "POS");

        if (extraHeaders != null)
            foreach (var (k, v) in extraHeaders)
                client.DefaultRequestHeaders.TryAddWithoutValidation(k, v);

        _logger.LogInformation("Capillary {Function} storeNo={StoreNo} url={Url}", function, storeNo, relativeUrl);

        HttpResponseMessage response;
        if (method == "GET")
        {
            response = await client.GetAsync(relativeUrl);
        }
        else
        {
            using var content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json");
            response = await client.PostAsync(relativeUrl, content);
        }

        var raw = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Capillary {Function} storeNo={StoreNo} httpStatus={HttpStatus}", function, storeNo, (int)response.StatusCode);
        return (raw, (int)response.StatusCode);
    }

    private static string CreateCapillaryToken(string userName, string storeNo, string password)
    {
        var plain = $"{userName}.{storeNo}:{Md5Hex(password)}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
    }

    private static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.ASCII.GetBytes(input);
        return string.Concat(md5.ComputeHash(bytes).Select(b => b.ToString("x2")));
    }

    private static string FormatPhoneWithCountryCode(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        phone = phone.Trim();
        if (phone.StartsWith("+")) return phone;
        if (phone.StartsWith("84") && phone.Length >= 11) return "+" + phone;
        if (phone.StartsWith("0")) return "+84" + phone[1..];
        return "+84" + phone;
    }

    private static ResultResponse Err(int status, string message, string? technical = null)
        => new() { Status = status, Message = message, Data = null, MessageTechnical = technical };

    // ────────────────────────── Internal Capillary response DTOs ──────────────────────────

    private sealed class CapStatus { public int Code { get; set; } }

    // ValidateCoupon
    private sealed class CapValidateResponse
    {
        public CapValidateData? Response { get; set; }
    }
    private sealed class CapValidateData
    {
        public CapStatus? Status { get; set; }
        public CapValidateCoupons? Coupons { get; set; }
    }
    private sealed class CapValidateCoupons
    {
        public CapRedeemable? Redeemable { get; set; }
    }
    private sealed class CapRedeemable
    {
        public string? Mobile { get; set; }
        public string? Code { get; set; }
        public string? Is_redeemable { get; set; }
        public CapSeriesInfo? Series_info { get; set; }
    }
    private sealed class CapSeriesInfo
    {
        public string? Description { get; set; }
        public string? Discount_code { get; set; }
        public string? Discount_type { get; set; }
        public decimal Discount_value { get; set; }
        public string? Valid_till_date { get; set; }
        public string? Tag { get; set; }
        public string? Terms_and_condition { get; set; }
    }
    private sealed class CapValidateErrorResponse
    {
        public CapValidateErrorData? Response { get; set; }
    }
    private sealed class CapValidateErrorData
    {
        public CapValidateErrorCoupons? Coupons { get; set; }
    }
    private sealed class CapValidateErrorCoupons
    {
        public CapValidateErrorRedeemable? Redeemable { get; set; }
    }
    private sealed class CapValidateErrorRedeemable
    {
        public object? Item_status { get; set; }
    }

    // CouponRedeem
    private sealed class CapRedeemResponse
    {
        public List<CapRedemptionItem>? Redemption { get; set; }
    }
    private sealed class CapRedemptionItem
    {
        public string? Code { get; set; }
        public decimal CouponValue { get; set; }
        public CapRedemptionStatus? RedemptionStatus { get; set; }
    }
    private sealed class CapRedemptionStatus
    {
        public int Code { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    // GetCouponsByUser
    private sealed class CapCouponByUserResponse
    {
        public CapCouponByUserEntity? Entity { get; set; }
    }
    private sealed class CapCouponByUserEntity
    {
        public List<object>? Customers { get; set; }
    }

    // GetCouponDetails
    private sealed class CapCouponDetailResponse
    {
        public CapCouponDetailData? Response { get; set; }
    }
    private sealed class CapCouponDetailData
    {
        public CapStatus? Status { get; set; }
        public CapCouponDetailCoupons? Coupons { get; set; }
    }
    private sealed class CapCouponDetailCoupons
    {
        public List<object>? Coupon { get; set; }
    }

    // ReActivateCoupons
    private sealed class CapReActivateResponse
    {
        public bool Success { get; set; }
        public List<CapError>? Errors { get; set; }
    }
    private sealed class CapError
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}

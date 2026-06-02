using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Use cases của WinLifeController — port logic từ WinLifeController + WinLifeService + WincodeService cũ.
/// CX WinLife API gọi qua IApiCallClient với config AppCode="CX" từ SysWebApi.
/// </summary>
public sealed class WinLifeService : IWinLifeService
{
    // CX WinLife API route names (keys trong SysWebApiRoute)
    private const string RouteOtpV2 = "CXWinLife_OTPV2";
    private const string RouteRegister = "CXWinLife_Register";
    private const string RouteRegisterTemp = "CXWinLife_Register_Temp";
    private const string RouteWinCodeHistories = "CXWinLife_WinCodeHistories";

    // Winpay OTP action mapping
    private static readonly Dictionary<string, string> WinpayOtpActions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "WINPAY_REGISTER",  "send_otp"  },
        { "WINPAY_UPDATE_FP", "send_otp2" },
        { "WINPAY_DEACTIVE",  "send_otp3" },
    };

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly ICXService _cxService;
    private readonly IWinpayService _winpayService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IWinCodeRepository _winCodeRepo;

    public WinLifeService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        ICXService cxService,
        IWinpayService winpayService,
        ILoyaltyService loyaltyService,
        IWinCodeRepository winCodeRepo)
    {
        _cache = cache;
        _apiClient = apiClient;
        _cxService = cxService;
        _winpayService = winpayService;
        _loyaltyService = loyaltyService;
        _winCodeRepo = winCodeRepo;
    }

    // ── GenerateOtpV2 ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpV2Async(
        string posNo, string phoneNumber, string action)
    {
        // Validate action against allowed list (WWW_OTP_ACTION config)
        if (!string.IsNullOrEmpty(action))
        {
            var allowedSetup = _cache.GetPOSDataSetup("WWW_OTP_ACTION");
            if (allowedSetup != null)
            {
                var allowed = allowedSetup.Value?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
                if (allowed.Length > 0 && !allowed.Contains(action, StringComparer.OrdinalIgnoreCase))
                    return (HttpStatusCode.BadRequest,
                        $"{action} action không đúng, vui lòng liên hệ IT", null);
            }
        }

        if (string.IsNullOrEmpty(phoneNumber))
            return (HttpStatusCode.BadRequest, "Số điện thoại không đúng", null);

        // Kiểm tra SĐT không cần OTP (whitelist trong CX config)
        if (_cxService.IsCheckNoneOtp(phoneNumber))
            return (HttpStatusCode.OK, "Số điện thoại không cần nhập OTP", null);

        // Winpay OTP flow
        if (WinpayOtpActions.TryGetValue(action, out var winpayAction))
        {
            var (ok, msg) = await SendWinpayOtpAsync(posNo, phoneNumber, winpayAction);
            if (ok)
            {
                return (HttpStatusCode.OK, "Success", new OtpResponse
                {
                    DeveloperMessage = "",
                    Message = "OK",
                    Data = new CXResponseData { MessageId = msg, Status = "PROCESSING" }
                });
            }
            return (HttpStatusCode.BadRequest, msg, null);
        }

        // CX OTP flow
        var result = await _cxService.GenerateOTPAsync(posNo, phoneNumber, action);
        return result.Success
            ? (HttpStatusCode.OK, result.Message, result.Data)
            : (HttpStatusCode.BadRequest, result.Message, null);
    }

    // ── VerifyOtpV2 ───────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VerifyOtpV2Async(
        PosVerifyOtpRequest request)
    {
        var result = await _cxService.VerifyOTPAsync(
            request.PosNo, request.PhoneNumber, request.Otp, request.Action);

        if (result.Success)
            return (HttpStatusCode.OK, result.Message, result.Data);

        // Legacy trả HTTP 200 với Status=Conflict khi OTP sai — giữ nguyên behavior
        return (HttpStatusCode.OK, "Số Otp nhập vào không đúng, vui lòng thử lại", result.Data);
    }

    // ── GenerateOTP (route cũ) ────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GenerateOtpAsync(
        string storeNo, string posID, string codeValue)
    {
        if (string.IsNullOrEmpty(codeValue))
            return (HttpStatusCode.BadRequest, "Số điện thoại không đúng", null);

        var cfg = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var route = GetCxRoute(cfg, RouteOtpV2);
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{route}?codeValue={codeValue}&type=1",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<WinLifeOtpResponse>(result.Response);
        if (resp?.ErrorCode == "E000")
            return (HttpStatusCode.OK, resp.ErrorMessage ?? "Success", null);
        if (resp != null)
            return (HttpStatusCode.BadRequest, $"{resp.ErrorCode}:{resp.ErrorMessage}", null);

        return (HttpStatusCode.ServiceUnavailable, result.Response ?? "Lỗi hệ thống API BLUE", null);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> RegisterAsync(
        WinLifeRegisterRequest request)
    {
        // WINCARE stores → Capillary
        var store = _cache.GetStores()?.FirstOrDefault(x =>
            string.Equals(x.No, request.storeCode, StringComparison.OrdinalIgnoreCase));

        if (store?.PostCode == "WINCARE")
        {
            // Verify OTP trước
            var otpResult = await _cxService.VerifyOTPAsync(
                request.posCode, request.phoneNo, request.otp, "WIN_MEMBER_REGISTER");
            if (!otpResult.Success)
                return (HttpStatusCode.OK, otpResult.Message, otpResult.Data);

            // Đăng ký qua Capillary (dùng ILoyaltyService)
            var capRequest = MapToCustomerRegisterRequest(request);
            var (status, message, data) = await _loyaltyService.CustomerRegistrationAsync(capRequest, "POS");
            return (status, message, data);
        }

        // Còn lại → CX WinLife register
        return await RegisterViaCxAsync(request);
    }

    // ── UpdatePromotions ──────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdatePromotionsAsync(
        WinLifeUpdatePromotionsRequest request)
    {
        if (string.IsNullOrEmpty(request.csn))
            request.csn = request.phoneNumber;

        request.phoneNumber = FormatHelper.PhoneNumberVietNam(request.phoneNumber ?? "");

        // Validate WinCode programs from cache
        var winCodeData = _cache.GetWinCodeMemory(_cache.GetConnectStringCentralMD());
        if (winCodeData == null)
            return (HttpStatusCode.BadRequest, "Không tìm thấy chương trình WinCode", null);

        if (request.action == "A") // Ghi nhận
        {
            if (request.appliedCodes?.Count > 0)
            {
                var existingRecords = await _winCodeRepo.GetWinCodeCustomerAsync(request.phoneNumber ?? "");

                foreach (var item in request.appliedCodes)
                {
                    var wincodeCfg = winCodeData.FirstOrDefault(x =>
                        string.Equals(x.WinCode, item.winCode, StringComparison.OrdinalIgnoreCase));
                    if (wincodeCfg == null)
                        return (HttpStatusCode.BadRequest,
                            $"Không tồn tại chương trình {item.winCode}", null);

                    // Check quota
                    WinCodeCustomerRecord? checkQty = null;
                    if (wincodeCfg.ApplyType == "BY_QTY")
                        checkQty = existingRecords
                            .Where(x => x.WinCode == item.winCode)
                            .OrderByDescending(x => x.CreatedDate)
                            .FirstOrDefault();
                    else if (wincodeCfg.ApplyType == "BY_STORE")
                        checkQty = existingRecords
                            .Where(x => x.WinCode == item.winCode && x.StoreNo == request.storeCode)
                            .OrderByDescending(x => x.CreatedDate)
                            .FirstOrDefault();

                    if (checkQty != null && checkQty.QuantityRecieptedSum >= wincodeCfg.Quantity)
                        return (HttpStatusCode.BadRequest,
                            $"Số thẻ hội viên {request.phoneNumber} đã nhận đủ số lượng {wincodeCfg.Quantity} quà tặng tại cửa hàng {request.storeCode}", null);
                }
            }

            var (ok, msg) = await _winCodeRepo.InsertWinCodeCustomerAsync(request);
            return ok
                ? (HttpStatusCode.OK, msg, null)
                : (HttpStatusCode.BadRequest, msg, null);
        }
        else if (request.action == "I") // Xoá
        {
            var (ok, msg) = await _winCodeRepo.UpdateWinCodeCustomerAsync(request);
            return ok
                ? (HttpStatusCode.OK, msg, null)
                : (HttpStatusCode.BadRequest, msg, null);
        }

        return (HttpStatusCode.BadRequest, $"Action '{request.action}' không hợp lệ (A/I)", null);
    }

    // ── WinCodeHistories ──────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> WinCodeHistoriesAsync(
        string csn, string winCode)
    {
        var cfg = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        var route = GetCxRoute(cfg, RouteWinCodeHistories);
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = $"{route}?csn={csn}&winCode={winCode}",
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<CxApiResponse>(result.Response);
        if (resp != null && string.IsNullOrEmpty(resp.code))
        {
            var history = ConvertHelper.ToObject<WinLifeHistoryModel>(
                JsonConvert.SerializeObject(resp.data));
            return (HttpStatusCode.OK, "Thành công", history);
        }
        return (resp != null
                ? (HttpStatusCode)409
                : HttpStatusCode.ServiceUnavailable,
            resp?.message ?? "Lỗi hệ thống CX", null);
    }

    // ── CustomerByLastDigitsPhone ─────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CustomerByLastDigitsPhoneAsync(
        string storeNo, string posID, string codeValue)
    {
        if (string.IsNullOrEmpty(codeValue) || string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(posID))
            return (HttpStatusCode.BadRequest, "codeValue đang bị trống", null);

        if (!NumberHelper.IsPhoneNumber(codeValue.Trim()))
            return (HttpStatusCode.BadRequest, $"Số điện thoại {codeValue} không hợp lệ", null);

        var (status, message, data) = await _loyaltyService.GetCustomerDetailAsync(
            codeValue.Trim(), posID, storeNo, isMobile: false);

        if (status != HttpStatusCode.OK)
            return (status, message, data);

        // Map InfoMemberDto → CustomerByLastDigitsPhoneResponse
        var infoMember = ConvertHelper.ToObject<InfoMemberDto>(JsonConvert.SerializeObject(data));
        if (infoMember == null)
            return (HttpStatusCode.NotFound, "OK", null);

        var response = new List<CustomerByLastDigitsPhoneResponse>
        {
            new()
            {
                Title = infoMember.Title,
                PhoneNo = infoMember.PhoneNumber,
                Dob = infoMember.Dob,
                FullName = infoMember.MemberName,
            }
        };
        return (HttpStatusCode.OK, "OK", response);
    }

    // ── UpdateCustomerInfo ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdateCustomerInfoAsync(
        WinLifeUpdateCustomerRequest request)
    {
        var updateReq = new CustomerUpdateRequest
        {
            fullName = request.fullName,
            phoneNo = request.phoneNo,
            gender = request.gender,
            title = request.title,
            storeCode = request.storeCode,
            posCode = request.posCode,
            dob = request.dob,
            email = request.email,
            address = request.address,
            province = request.province,
            district = request.district,
            city = request.city,
            ward = request.ward,
        };
        return await _loyaltyService.CustomerUpdateAsync(updateReq, "POS");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private SysWebApiDto? GetCxConfig()
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "CX", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private static string GetCxRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";

    private async Task<(HttpStatusCode, string, object?)> RegisterViaCxAsync(WinLifeRegisterRequest request)
    {
        var cfg = GetCxConfig();
        if (cfg == null) return (HttpStatusCode.ServiceUnavailable, "Chưa cấu hình CX WebApi", null);

        string routeName = string.Equals(request.status, "TEMP", StringComparison.OrdinalIgnoreCase)
            ? RouteRegisterTemp : RouteRegister;
        var route = GetCxRoute(cfg, routeName);

        var cxPayload = new WinLifeRegisterCxRequest
        {
            fullName = request.fullName,
            title = request.title,
            phoneNo = request.phoneNo,
            gender = request.gender,
            merchantId = "VCM",
            otp = request.otp,
            dob = request.dob,
            storeNo = request.storeCode,
            source = "POS",
            staffEntityId = "WCM",
            refStaffId = request.userPOS,
            identifierCardid = request.identifierCardid,
            issuedDate = request.issuedDate,
            issuedPlace = request.issuedPlace,
        };

        var apiResult = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route,
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(cxPayload),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });

        var resp = ConvertHelper.ToObject<WinLifeRegisterCxResponse>(apiResult.Response);
        if (resp == null) return (HttpStatusCode.ServiceUnavailable, "Lỗi hệ thống API BLUE, vui lòng liên hệ IT", null);

        // HTTP 200 from CX → success
        if (string.IsNullOrEmpty(resp.code) && resp.data != null)
        {
            var result = new WinLifeRegisterPosResponse
            {
                fullName = resp.data.fullName,
                phoneNo = resp.data.phoneNo,
                gender = resp.data.gender,
                csn = resp.data.csn,
            };
            return (HttpStatusCode.OK, "Đăng ký thành công thành viên WinLife", result);
        }

        // E011 → phone already registered (Conflict)
        if (resp.code == "E011")
            return (HttpStatusCode.Conflict, resp.message ?? "Số điện thoại đã tồn tại", null);

        // Validation errors
        if (resp.errors?.Count > 0)
        {
            var firstErr = resp.errors[0];
            return (HttpStatusCode.Conflict,
                $"{firstErr.field}: {firstErr.message}", null);
        }

        return (HttpStatusCode.BadRequest, resp.message ?? "Lỗi đăng ký", null);
    }

    private async Task<(bool Success, string Message)> SendWinpayOtpAsync(
        string posNo, string phoneNumber, string winpayAction)
    {
        // Delegate to IWinpayService — maps to legacy _winpayService.SendOtpWinpay
        try
        {
            var result = await _winpayService.SendOtpWinpayAsync(posNo, phoneNumber, winpayAction);
            return result;
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi Exception: {ex.Message}");
        }
    }

    private static CustomerRegisterRequest MapToCustomerRegisterRequest(WinLifeRegisterRequest r)
        => new()
        {
            fullName = r.fullName,
            title = r.title,
            phoneNo = r.phoneNo,
            gender = r.gender,
            otp = r.otp,
            userPOS = r.userPOS,
            storeCode = r.storeCode,
            posCode = r.posCode,
            dob = r.dob,
            identifierCardid = r.identifierCardid,
            issuedDate = r.issuedDate,
            issuedPlace = r.issuedPlace,
            status = r.status,
            email = r.email,
            address = r.address,
            province = r.province,
            district = r.district,
            city = r.city,
            ward = r.ward,
        };
}

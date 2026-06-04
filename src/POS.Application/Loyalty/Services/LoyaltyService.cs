using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Loyalty.DTOs;
using POS.Application.Loyalty.DTOs.Capillary;
using POS.Application.Shared.Services;
using POS.Shared.Models;

namespace POS.Application.Loyalty.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly ILoyaltyCapillaryService _capillaryService;
    private readonly ISysWebApiConfigService _configService;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<LoyaltyService> _logger;

    private const string AppCodeCapillary = "CAPILLARY";
    private const string RedisKeyOffline = "IsOfflineCapillary";
    private const string RedisKeyMemberPrefix = "BLUEPOS:Loyalty:";

    public LoyaltyService(
        ILoyaltyCapillaryService capillaryService,
        ISysWebApiConfigService configService,
        IRedisCacheService cache,
        ILogger<LoyaltyService> logger)
    {
        _capillaryService = capillaryService;
        _configService = configService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(int StatusCode, ResultResponse Body)> GetCustomerAsync(GetCustomerRequest request)
    {
        _logger.LogInformation("GetCustomer {NumberCard} {PosId} {StoreNo}", request.NumberCard, request.PosId, request.StoreNo);

        // 1. VINID check → stub 400
        if (IsVINID(request.NumberCard))
            return Fail(400, "VINID endpoint riêng: api/vinid/GetInfoMember");

        // 2. Xác định loại thẻ
        var memberType = DetermineMemberType(request.NumberCard, request.IsMobile);

        // 3. Không xác định được loại → 400
        if (memberType == MemberCapillaryType.NONE)
            return Fail(400, "Số thẻ không hợp lệ");

        // 4. WINCARE / WINX → stub 404 (chuyển đổi sau)
        if (memberType == MemberCapillaryType.WINCARE)
            return Fail(404, "WinCare chưa hỗ trợ trong phiên bản này");
        if (memberType == MemberCapillaryType.WINX)
            return Fail(404, "WinX chưa hỗ trợ trong phiên bản này");

        // 5. Chuẩn hóa số điện thoại / mã
        var codeValue = memberType == MemberCapillaryType.PHONE
            ? FormatPhoneWithCountryCode(request.NumberCard)
            : request.NumberCard;
        var phoneVN = memberType == MemberCapillaryType.PHONE
            ? FormatPhoneVietNam(codeValue)
            : request.NumberCard;

        // 6. Kiểm tra offline flag từ Redis
        var offlineFlag = await _cache.GetStringAsync(RedisKeyOffline);
        if (offlineFlag != null)
        {
            var cached = await GetCachedMemberAsync(phoneVN);
            return (200, new ResultResponse { Status = 200, Message = "OFF", Data = cached, MessageTechnical = request.ClubCode });
        }

        // 7. Lấy config Capillary từ DB
        var config = await _configService.GetByAppCodeAsync(AppCodeCapillary);
        if (config == null)
        {
            _logger.LogError("Không tìm thấy cấu hình AppCode={AppCode}", AppCodeCapillary);
            return Fail(500, "Không tìm thấy cấu hình Capillary");
        }

        // 8. Gọi Capillary API
        var (success, message, data, statusCode) = await _capillaryService.GetCustomerDetailAsync(
            config, request.StoreNo, request.PosId, codeValue, memberType.ToString());

        // 9. Xử lý lỗi
        if (!success)
        {
            bool isTimeout = statusCode == 408
                || message.Contains("408_requestTimedOut", StringComparison.OrdinalIgnoreCase)
                || message.Contains("A task was canceled", StringComparison.OrdinalIgnoreCase);

            if (isTimeout)
            {
                _logger.LogWarning("Capillary timeout {CodeValue}", codeValue);
                var cached = await GetCachedMemberAsync(phoneVN);
                return (200, new ResultResponse { Status = 200, Message = "OFF", Data = cached, MessageTechnical = request.ClubCode });
            }

            // Capillary trả 500 khi khách hàng không tồn tại
            if (statusCode == 500)
                return Fail(404, "Số điện thoại chưa đăng ký hội viên");

            // Server error khác (502, 503, 504...) → signal offline (101 = SwitchingProtocols)
            if (statusCode is > 500 and < 600)
            {
                _logger.LogWarning("Capillary server error {StatusCode} — chuyển offline", statusCode);
                return (101, new ResultResponse { Status = 200, Message = "Chuyển truy vấn hội viên CAP sang offline", MessageTechnical = message });
            }

            return Fail(400, message ?? "Truy vấn thông tin hội viên WIN thất bại, vui lòng thử lại");
        }

        // 10. Lấy dữ liệu khách hàng
        var customerData = data?.Response?.Customers?.Customer?.FirstOrDefault();
        if (customerData == null)
            return Fail(404, "Số điện thoại chưa đăng ký hội viên");

        var virtualCard = customerData.User_id ?? customerData.Mobile ?? codeValue;
        var qrCode = !string.IsNullOrEmpty(virtualCard) ? $"{memberType}-{phoneVN}" : string.Empty;
        var infoMember = MapToInfoMemberResponse(customerData, request.NumberCard, memberType.ToString(), virtualCard, qrCode);

        // 11. Cache member data — dùng Hash theo phone prefix (4 ký tự đầu)
        await CacheMemberAsync(phoneVN, infoMember);

        _logger.LogInformation("GetCustomer OK {PhoneVN} MemberPoint={MemberPoint}", phoneVN, infoMember.MemberPoint);

        return (200, new ResultResponse { Status = 200, Message = "OK", Data = infoMember, MessageTechnical = request.ClubCode });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<InfoMemberResponse?> GetCachedMemberAsync(string phoneVN)
    {
        if (string.IsNullOrEmpty(phoneVN)) return null;
        var prefix = phoneVN.Length >= 4 ? phoneVN[..4] : phoneVN;
        return await _cache.HashGetAsync<InfoMemberResponse>(RedisKeyMemberPrefix + prefix, phoneVN);
    }

    private async Task CacheMemberAsync(string phoneVN, InfoMemberResponse member)
    {
        if (string.IsNullOrEmpty(phoneVN)) return;
        var prefix = phoneVN.Length >= 4 ? phoneVN[..4] : phoneVN;
        var ttl = DateTime.Today.AddDays(1) - DateTime.Now;
        await _cache.HashSetAsync(
            RedisKeyMemberPrefix + prefix,
            phoneVN,
            member,
            ttl > TimeSpan.Zero ? ttl : TimeSpan.FromHours(1));
    }

    private static (int StatusCode, ResultResponse Body) Fail(int statusCode, string message)
        => (statusCode, new ResultResponse { Status = statusCode, Message = message });

    private static MemberCapillaryType DetermineMemberType(string numberCard, bool isMobile)
    {
        if (string.IsNullOrEmpty(numberCard)) return MemberCapillaryType.NONE;

        bool allDigits = numberCard.All(char.IsDigit);
        int len = numberCard.Length;

        if (!isMobile && allDigits && len >= 9 && len <= 11)
            return MemberCapillaryType.PHONE;

        if (isMobile && len == 12)
            return MemberCapillaryType.WINCARE;

        if (isMobile && len == 14)
            return MemberCapillaryType.WINX;

        if (isMobile && len <= 9 && !numberCard.StartsWith("0"))
            return MemberCapillaryType.ID;

        return MemberCapillaryType.NONE;
    }

    private static bool IsVINID(string numberCard)
    {
        if (string.IsNullOrEmpty(numberCard)) return false;
        if ((numberCard.StartsWith("8888") || numberCard.StartsWith("66668")) && numberCard.Length >= 16)
            return true;
        if (numberCard.StartsWith("P", StringComparison.OrdinalIgnoreCase) && numberCard.Length != 14)
            return true;
        return false;
    }

    private static string FormatPhoneWithCountryCode(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        if (phone.StartsWith("+84")) return phone;
        if (phone.StartsWith("84") && phone.Length > 10) return "+" + phone;
        if (phone.StartsWith("0")) return "+84" + phone[1..];
        return "+84" + phone;
    }

    private static string FormatPhoneVietNam(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        if (phone.StartsWith("+84")) return "0" + phone[3..];
        if (phone.StartsWith("84") && phone.Length > 10) return "0" + phone[2..];
        return phone;
    }

    private static string GetFieldValue(List<CapFieldItem>? fields, string name)
        => fields?.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Value
           ?? string.Empty;

    private static string GetTitleFromGender(string gender)
        => gender.ToLowerInvariant() switch { "male" => "Anh", "female" => "Chị", _ => string.Empty };

    private static InfoMemberResponse MapToInfoMemberResponse(
        CapCustomerData customer, string memberCsn, string memberType, string virtualCard, string qrCode)
    {
        var extFields = customer.Extended_fields?.Field;
        var customFields = customer.Custom_fields?.Field;

        var phoneVN = FormatPhoneVietNam(customer.Mobile ?? string.Empty);
        long.TryParse(customer.Loyalty_points, out var memberPoint);
        long.TryParse(customer.Lifetime_points, out var totalPoint);
        const int defaultRate = 10;

        var gender = GetFieldValue(extFields, "gender");
        var dob = GetFieldValue(extFields, "dob_date");

        return new InfoMemberResponse
        {
            CardNumber = phoneVN,
            VirtualCard = virtualCard,
            CMND = string.Empty,
            MemberName = customer.Firstname ?? string.Empty,
            Title = GetTitleFromGender(gender),
            PhoneNumber = phoneVN,
            CardLevel = customer.Current_slab ?? string.Empty,
            MemberPoint = memberPoint,
            TotalPoint = totalPoint,
            RedemptionValue = memberPoint * defaultRate,
            CurrentRate = defaultRate,
            MemberCSN = memberCsn,
            OtherInfo = GetFieldValue(extFields, "acquisition_channel"),
            QRCode = qrCode,
            Dob = dob,
            DateOfBirth = dob,
            BirthdayGiftInd = false,
            ExtraPoint = false,
            IsRedeem = false,
            IsOfflineVinID = false,
            IsShowMessage = false,
            Status = "Hoạt động",
            System = "CAP",
            ClubCode = "WINCARE",
            Gender = gender,
            Email = GetFieldValue(extFields, "email"),
            Address = GetFieldValue(customFields, "customer_address"),
            ExternalId = GetFieldValue(extFields, "member_type"),
            MemberType = memberType,
            Source = "CAP",
            OtherStatus = null,
            ExtendedFields = new List<ExtendedFieldItem>(),
            AvailablePromotion = new List<BLUEAvailablePromotion>(),
            MemberBusiness = null,
            PointsSummaries = new List<object>()
        };
    }
}

// Đặt internal vì chỉ dùng trong Application layer
internal enum MemberCapillaryType { PHONE, ID, WINCARE, WINX, NONE }

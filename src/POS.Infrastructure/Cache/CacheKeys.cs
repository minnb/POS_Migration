namespace POS.Infrastructure.Cache;

// Tập trung tất cả Redis key patterns — không hardcode string rải rác trong code.
public static class CacheKeys
{
    // ── Loyalty / Member ────────────────────────────────────────────────────
    // Hash: key = prefix + phone[0..3], field = phoneNumber (đầy đủ)
    public static string LoyaltyMember(string phone4Chars) => $"BLUEPOS:Loyalty:{phone4Chars}";

    // Flag: khi set → hệ thống Capillary đang offline, dùng cached data
    public const string IsOfflineCapillary = "IsOfflineCapillary";

    // Hash: key = orderNo, field = orderNo — lưu redemption_id sau khi redeem points
    public static string LoyaltyRedeemPoints(string orderNo) => $"BLUEPOS:Loyalty:RedeemPoints:{orderNo}";

    // String: staff member phone lookup (WINCARE virtual card → phone)
    public static string MemberStaff(string virtualCard) => $"BLUEPOS:MemberStaff:{virtualCard}";

    // ── SysWebApi Config ────────────────────────────────────────────────────
    // Không dùng Redis cho config — đã cache bằng IMemoryCache đến nửa đêm

    // ── Session / Shift ─────────────────────────────────────────────────────
    public static string Session(string terminalId) => $"BLUEPOS:Session:{terminalId}";

    // ── Retry queues (RabbitMQ fallback) ───────────────────────────────────
    public const string RetryAddTransaction = "AddTransaction";
    public const string RetryReturnTransaction = "ReturnTransaction";
}

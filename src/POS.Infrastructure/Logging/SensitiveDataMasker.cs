namespace POS.Infrastructure.Logging;

/// <summary>
/// Che dữ liệu nhạy cảm trước khi đưa vào structured log (Serilog {@Context}).
/// Danh sách từ khóa mở rộng từ IsSensitiveKey trong .claude/skills/web/audit-logging.md §7 —
/// đây là helper dùng chung đầu tiên, thay cho việc mỗi page tự inline check riêng.
/// </summary>
public static class SensitiveDataMasker
{
    private static readonly string[] SensitiveKeywords =
        ["PASSWORD", "SECRET", "TOKEN", "PWD", "CREDENTIAL", "PIN", "PINCODE", "APIKEY"];

    public static bool IsSensitiveKey(string key) =>
        SensitiveKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));

    /// <summary>Trả về dictionary mới, giá trị của key nhạy cảm bị thay bằng "***".</summary>
    public static IReadOnlyDictionary<string, object?> Mask(IReadOnlyDictionary<string, object?> context) =>
        context.ToDictionary(kv => kv.Key, kv => IsSensitiveKey(kv.Key) ? "***" : kv.Value);
}

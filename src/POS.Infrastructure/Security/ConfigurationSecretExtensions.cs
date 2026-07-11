using Microsoft.Extensions.Configuration;

namespace POS.Infrastructure.Security;

/// <summary>
/// Giải mã credentials đã mã hóa (enc:...) trong cấu hình — dùng chung cho POS.Api, POS.Web, POS.Worker.
/// Rút từ block inline từng lặp lại ở mỗi Program.cs để tránh copy-paste (một nguồn sự thật).
/// </summary>
public static class ConfigurationSecretExtensions
{
    /// <summary>
    /// Quét mọi giá trị config chứa token "enc:" (vd Password trong connection string),
    /// giải mã bằng AES-256-GCM với khóa từ env POS_SECRET_KEY, rồi nạp đè in-memory.
    /// Mọi consumer (GetConnectionString / GetSection&lt;RabbitMQOptions&gt;) tự nhận plaintext.
    /// <para>
    /// No-op khi không có token enc: → DEV/base plaintext vẫn chạy, không cần khóa.
    /// Fail-fast (ném <see cref="InvalidOperationException"/>) khi có enc: mà thiếu POS_SECRET_KEY.
    /// </para>
    /// PHẢI gọi ngay sau CreateBuilder, TRƯỚC AddInfrastructure.
    /// </summary>
    public static void DecryptEncryptedSecrets(this ConfigurationManager configuration)
    {
        var encryptedEntries = configuration.AsEnumerable()
            .Where(kv => SecretProtector.HasToken(kv.Value))
            .ToList();
        if (encryptedEntries.Count == 0)
            return;

        var secretKey = Environment.GetEnvironmentVariable("POS_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                "Có giá trị cấu hình mã hóa (enc:...) nhưng thiếu biến môi trường POS_SECRET_KEY. " +
                "Đặt khóa AES base64 (32 byte) vào POS_SECRET_KEY (tạo khóa tại /admin/encrypt-secret của POS.Web).");

        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in encryptedEntries)
            overrides[kv.Key] = SecretProtector.DecryptTokens(kv.Value!, secretKey);
        configuration.AddInMemoryCollection(overrides);
    }
}

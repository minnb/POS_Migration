namespace POS.Api.Middleware;

/// <summary>
/// Cấu hình cho <see cref="PosApiKeyMiddleware"/>. Section "PosApiKeyAuth" trong appsettings.
/// </summary>
public sealed class PosApiKeyAuthOptions
{
    public const string SectionName = "PosApiKeyAuth";

    /// <summary>Độ lệch tối đa (phút) giữa X-Timestamp và giờ server để request còn hợp lệ.</summary>
    public int TimestampWindowMinutes { get; set; } = 10;
}

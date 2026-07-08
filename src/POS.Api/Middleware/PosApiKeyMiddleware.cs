using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Common;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;
using POS.Infrastructure.Security;

namespace POS.Api.Middleware;

/// <summary>
/// Xác thực request từ máy POS theo hai hình thức:
///   1. Header X-Request-Id + X-Timestamp + X-Checksum + X-Pos-No → verify timestamp window rồi
///      SHA-256(RequestId|Timestamp|Secret+PosNo), so sánh bằng thời gian cố định
///      (CryptographicOperations.FixedTimeEquals qua PosApiKeyChecksum). Secret lấy từ Redis cache
///      MD:POSDataSetup (12h TTL, tự invalidate khi admin sửa DB); giải mã token enc:... qua
///      SecretProtector nếu giá trị DB đã được mã hóa.
///   2. Authorization: Basic → pass-through, BasicAuthHandler xử lý (/api/v2/*)
///   3. Authorization: Bearer → pass-through (pending triển khai)
///
/// Rủi ro tồn dư đã được chấp nhận (xem docs/API_CONTRACT.md): không chống replay trong cửa sổ
/// timestamp; secret dùng chung cho toàn bộ POS (không phải secret riêng theo từng PosNo).
/// </summary>
public sealed class PosApiKeyMiddleware(RequestDelegate next, IOptions<PosApiKeyAuthOptions> options)
{
    private const string RequestIdHeader = "X-Request-Id";
    private const string TimestampHeader = "X-Timestamp";
    private const string ChecksumHeader  = "X-Checksum";
    private const string PosNoHeader     = "X-Pos-No";
    private const string SecretCode      = "X-API";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task InvokeAsync(
        HttpContext context,
        ICentralMDRepository centralMDRepo,
        IFileLogHelper fileLogHelper)
    {
        var path = context.Request.Path;

        // Health check và Swagger không cần xác thực
        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault();
        var timestamp = context.Request.Headers[TimestampHeader].FirstOrDefault();
        var checksum  = context.Request.Headers[ChecksumHeader].FirstOrDefault();
        var posNo     = context.Request.Headers[PosNoHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(timestamp) &&
            !string.IsNullOrEmpty(checksum) && !string.IsNullOrEmpty(posNo))
        {
            var clientIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                           ?? context.Connection.RemoteIpAddress?.ToString()
                           ?? "unknown";

            if (!long.TryParse(timestamp, out var timestampUnixSeconds))
            {
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-Timestamp không hợp lệ | Path={path} | IP={clientIp} | PosNo={posNo}");
                await WriteUnauthorizedAsync(context, "Chưa xác thực");
                return;
            }

            if (!PosApiKeyChecksum.IsWithinWindow(timestampUnixSeconds, DateTimeOffset.UtcNow,
                    options.Value.TimestampWindowMinutes))
            {
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-Timestamp lệch quá {options.Value.TimestampWindowMinutes} phút | " +
                    $"Path={path} | IP={clientIp} | PosNo={posNo} | RequestId={requestId}");
                await WriteUnauthorizedAsync(context, "Chưa xác thực");
                return;
            }

            var setupList = await centralMDRepo.GetPOSDataSetupAsync(context.RequestAborted);
            var secretRaw = setupList?
                .FirstOrDefault(x => string.Equals(x.Code, SecretCode, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (string.IsNullOrEmpty(secretRaw))
            {
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-API chưa cấu hình trong POSDataSetup | Path={path} | PosNo={posNo}");
                await WriteUnauthorizedAsync(context, "Cấu hình X-API chưa được thiết lập");
                return;
            }

            string secret;
            if (SecretProtector.HasToken(secretRaw))
            {
                var secretKey = Environment.GetEnvironmentVariable("POS_SECRET_KEY");
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    fileLogHelper.WriteLogs(
                        $"[PosApiKey] Thiếu POS_SECRET_KEY để giải mã X-API | Path={path} | PosNo={posNo}");
                    await WriteUnauthorizedAsync(context, "Cấu hình X-API chưa được thiết lập");
                    return;
                }

                try
                {
                    secret = SecretProtector.DecryptTokens(secretRaw, secretKey);
                }
                catch (CryptographicException ex)
                {
                    // Không log secretRaw/token mã hóa — chỉ log message lỗi giải mã
                    fileLogHelper.WriteLogs(
                        $"[PosApiKey] Giải mã X-API thất bại | Path={path} | PosNo={posNo} | {ex.Message}");
                    await WriteUnauthorizedAsync(context, "Cấu hình X-API chưa được thiết lập");
                    return;
                }
            }
            else
            {
                secret = secretRaw;
            }

            var expected = PosApiKeyChecksum.Compute(requestId, timestampUnixSeconds, secret, posNo);
            if (!PosApiKeyChecksum.Verify(expected, checksum))
            {
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-Checksum không hợp lệ | Path={path} | IP={clientIp} | " +
                    $"PosNo={posNo} | RequestId={requestId}");
                await WriteUnauthorizedAsync(context, "Chưa xác thực");
                return;
            }

            await next(context);
            return;
        }

        // Không đủ header POS mới → kiểm tra Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
        {
            // Basic Auth (/api/v2/* từ POS) hoặc Bearer (pending) → để pipeline tiếp tục
            await next(context);
            return;
        }

        // Thiếu cả header POS mới lẫn Authorization → TỪ CHỐI (fail-closed)
        fileLogHelper.WriteLogs($"[PosApiKey] Thiếu header xác thực | Path={path}");
        await WriteUnauthorizedAsync(context, "Chưa xác thực");
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode  = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonConvert.SerializeObject(new ResultResponse
            {
                Status  = HttpStatusCode.Unauthorized,
                Message = message
            }, JsonSettings));
    }
}

public static class PosApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UsePosApiKeyAuth(this IApplicationBuilder app)
        => app.UseMiddleware<PosApiKeyMiddleware>();
}

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Common;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Api.Middleware;

/// <summary>
/// Xác thực request từ máy POS theo hai hình thức:
///   1. Header X-API  → validate MD5(privateKey).ToUpper() vs giá trị POSDataSetup[X-API]
///   2. Authorization: Basic → pass-through, BasicAuthHandler xử lý (/api/v2/*)
///   3. Authorization: Bearer → pass-through (pending triển khai)
/// Private key lấy từ Redis cache MD:POSDataSetup (12h TTL), tự invalidate khi admin sửa DB.
/// </summary>
public sealed class PosApiKeyMiddleware(RequestDelegate next)
{
    private const string XApiHeader       = "X-API";
    private const string PosDataSetupCode = "X-API";

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

        var xApiValue = context.Request.Headers[XApiHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(xApiValue))
        {
            // POS request qua X-API header — validate
            var setupList = await centralMDRepo.GetPOSDataSetupAsync(context.RequestAborted);
            var privateKey = setupList?
                .FirstOrDefault(x => string.Equals(x.Code, PosDataSetupCode,
                                                   StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (string.IsNullOrEmpty(privateKey))
            {
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-API chưa cấu hình trong POSDataSetup | Path={path}");
                await WriteUnauthorizedAsync(context, "Cấu hình X-API chưa được thiết lập");
                return;
            }

            var expectedHash = ComputeMd5Upper(privateKey);
            if (!string.Equals(xApiValue, expectedHash, StringComparison.Ordinal))
            {
                var clientIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                               ?? context.Connection.RemoteIpAddress?.ToString()
                               ?? "unknown";
                fileLogHelper.WriteLogs(
                    $"[PosApiKey] X-API không hợp lệ | Path={path} | IP={clientIp} | Received={xApiValue}");
                await WriteUnauthorizedAsync(context, "Chưa xác thực");
                return;
            }

            await next(context);
            return;
        }

        // Không có X-API → kiểm tra Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
        {
            // Basic Auth (/api/v2/* từ POS) hoặc Bearer (pending) → để pipeline tiếp tục
            await next(context);
            return;
        }

        // Thiếu cả X-API lẫn Authorization → TỪ CHỐI (fail-closed)
        fileLogHelper.WriteLogs($"[PosApiKey] Thiếu X-API và Authorization | Path={path}");
        await WriteUnauthorizedAsync(context, "Chưa xác thực");
    }

    private static string ComputeMd5Upper(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes); // .NET 5+ — uppercase hex
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

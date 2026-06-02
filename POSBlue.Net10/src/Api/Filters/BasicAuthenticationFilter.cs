using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Api.Filters;

/// <summary>
/// Filter xác thực — GIỮ NGUYÊN 100% logic của BasicAuthenticationAttribute (System.Web.Http) cũ:
///  1) Nếu có header "X-API": đối chiếu MD5(giá trị cấu hình) — sai thì log cảnh báo (KHÔNG chặn request).
///  2) Nếu đường dẫn chứa "api/v2": bắt buộc Basic Auth (Authorization header), decode base64 "user:pass",
///     kiểm tra với bảng SysWebApiUser — sai/không có thì trả 401.
/// Đăng ký global trong Program.cs để áp dụng cho mọi route (giống config.Filters.Add cũ).
/// </summary>
public sealed class BasicAuthenticationFilter : IAsyncAuthorizationFilter
{
    private readonly IPosAuthenticationService _authService;
    private readonly IRequestLogger _requestLogger;

    public BasicAuthenticationFilter(IPosAuthenticationService authService, IRequestLogger requestLogger)
    {
        _authService = authService;
        _requestLogger = requestLogger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var absolutePath = request.Path.Value ?? string.Empty;

        // (1) Kiểm tra X-API (không chặn, chỉ log nếu sai) — giống bản cũ
        string? xApi = request.Headers["X-API"];
        if (!string.IsNullOrEmpty(xApi))
        {
            var configuredXApi = await _authService.GetConfiguredXApiAsync();
            if (!string.IsNullOrEmpty(configuredXApi) &&
                !string.Equals(xApi, EncryptionHelper.GetMd5Hash(configuredXApi), StringComparison.OrdinalIgnoreCase))
            {
                _requestLogger.LogRequest("Unauthorized-X-API", "X-API", "", absolutePath + "@" + xApi);
            }
        }

        // (2) Chỉ enforce Basic Auth cho route chứa "api/v2" — giống bản cũ
        if (absolutePath.IndexOf("api/v2", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            string? authHeader = request.Headers.Authorization;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Basic ".Length).Trim();
                var decoded = EncryptionHelper.Base64Decode(token);
                if (!string.IsNullOrEmpty(decoded) && decoded.Contains(':'))
                {
                    var parts = decoded.Split(':');
                    if (parts.Length >= 2 && await _authService.ValidateWebApiUserAsync(parts[0], parts[1]))
                    {
                        return; // hợp lệ
                    }
                }
            }
            context.Result = new UnauthorizedObjectResult("API Error 401: Unauthorized");
        }
    }
}

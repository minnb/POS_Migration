using System.Diagnostics;
using System.Text;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Api.Middlewares;

/// <summary>
/// Middleware ghi log request/response — thay thế MessageLoggingHandler (DelegatingHandler) cũ.
/// Giữ NGUYÊN hành vi:
///  - Sinh X-RequestId-Header.
///  - Bỏ qua nội dung với route Upload/Download/zip.
///  - Lấy IP từ X-FORWARDED-FOR, fallback REMOTE_ADDR (::1 → 127.0.0.1), thêm " with {X-API}".
///  - Đo thời gian xử lý và log request + response qua IRequestLogger.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestLogger _logger;

    public RequestLoggingMiddleware(RequestDelegate next, IRequestLogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var requestInfo = $"{request.Scheme}://{request.Host}{request.Path}";
        var isGet = HttpMethods.IsGet(request.Method);

        context.Response.Headers["X-RequestId-Header"] = Guid.NewGuid().ToString();

        // Đọc request body (buffering để controller đọc lại được)
        request.EnableBuffering();
        string requestBody = "";
        if (!isGet && request.ContentLength is > 0)
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }
        if (requestInfo.Contains("UploadFileSale") || requestInfo.Contains("UploadFileLogJob"))
            requestBody = "";

        var userHostAddress = ResolveClientAddress(context);

        _logger.LogRequest(userHostAddress, requestInfo, "", requestBody);

        // Bắt response để log
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            buffer.Position = 0;
            string responseBody;
            var upperUri = requestInfo.ToUpperInvariant();
            var contentType = context.Response.ContentType ?? "";
            if (upperUri.Contains("UPLOAD") || upperUri.Contains("DOWNLOAD") || contentType == "application/x-zip-compressed")
            {
                responseBody = "Download-Upload";
            }
            else
            {
                responseBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                buffer.Position = 0;
            }

            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            _logger.LogResponse(requestInfo, userHostAddress, sw.ElapsedMilliseconds, "", responseBody);
        }
    }

    private static string ResolveClientAddress(HttpContext context)
    {
        var headers = context.Request.Headers;
        string? addr = headers["X-FORWARDED-FOR"];
        if (string.IsNullOrEmpty(addr))
        {
            addr = context.Connection.RemoteIpAddress?.ToString();
            if (addr == "::1") addr = "127.0.0.1";
        }
        string? xApi = headers["X-API"];
        if (!string.IsNullOrEmpty(xApi))
            addr += $" with {xApi}";
        return addr ?? "";
    }
}

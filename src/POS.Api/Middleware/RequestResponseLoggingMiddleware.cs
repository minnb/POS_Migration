using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using POS.Infrastructure.Logging;

namespace POS.Api.Middleware;

/// <summary>
/// Log request/response cho MỌI API — thay thế các lời gọi LogRequest thủ công rải rác trước đây
/// (chỉ có ở 3/9 controller, không nhất quán). Bật/tắt qua config "RequestLogging:Enabled".
///
/// Luôn gọi qua IKibanaService (Serilog) — không có cờ "dùng Kibana hay không" riêng; Elasticsearch
/// sink tự no-op nếu Elasticsearch:Nodes rỗng. Cờ "RequestLogging:PersistToFile" (đọc riêng trong
/// SerilogConfiguration.cs) quyết định File sink có nhận log Request/Response hay không.
///
/// Response body được capture qua <see cref="CappedCapturingStream"/> — pass-through trực tiếp tới
/// stream gốc, chỉ giữ lại tối đa MaxBodyBytes đầu tiên trong RAM để log. KHÔNG buffer toàn bộ
/// response vào MemoryStream trước khi gửi client — quan trọng với endpoint stream file lớn
/// (vd DowloadFileStream trả zip vài chục MB) để tránh tốn RAM/tăng độ trễ.
/// </summary>
public sealed class RequestResponseLoggingMiddleware(RequestDelegate next, IOptions<RequestLoggingOptions> options)
{
    private static readonly string[] BinaryContentTypePrefixes =
    [
        "application/x-zip-compressed", "application/zip", "application/octet-stream"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var opt = options.Value;
        if (!opt.Enabled || IsExcluded(context.Request.Path.Value, opt.ExcludePaths))
        {
            await next(context);
            return;
        }

        var kibana = context.RequestServices.GetService<IKibanaService>();
        var endpoint = context.Request.Path.Value ?? string.Empty;
        var clientIp = GetClientIp(context);

        try
        {
            var requestBody = await ReadRequestBodyAsync(context.Request, opt.MaxBodyBytes);
            kibana?.LogRequest(endpoint, clientIp, BuildRequestPayload(context.Request, requestBody));
        }
        catch
        {
            // Logging không được phép phá luồng request chính.
        }

        var originalBody = context.Response.Body;
        var capturing = new CappedCapturingStream(originalBody, opt.MaxBodyBytes);
        context.Response.Body = capturing;
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            context.Response.Body = originalBody;
            try
            {
                var responseBody = BuildResponsePayload(context.Response, capturing);
                kibana?.LogResponse(endpoint, clientIp, sw.ElapsedMilliseconds, "", responseBody);
            }
            catch
            {
                // Logging không được phép phá luồng request chính.
            }
        }
    }

    private static bool IsExcluded(string? path, string[] excludePaths)
        => !string.IsNullOrEmpty(path) &&
           excludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-FORWARDED-FOR"].ToString();
        return string.IsNullOrEmpty(forwarded)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwarded;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, int maxBytes)
    {
        if (request.ContentLength is null or 0) return string.Empty;

        var contentType = request.ContentType ?? string.Empty;
        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return $"[multipart/form-data, {request.ContentLength} bytes, không capture nội dung]";

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096, leaveOpen: true);
        var buffer = new char[maxBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, maxBytes);
        request.Body.Position = 0;

        var text = new string(buffer, 0, read);
        return request.ContentLength > maxBytes ? $"{text}...(truncated)" : text;
    }

    private static string BuildRequestPayload(HttpRequest request, string body)
    {
        var headers = request.Headers
            .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                     && !h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return JsonConvert.SerializeObject(new
        {
            method = request.Method,
            path = request.Path.Value,
            query = request.QueryString.Value,
            headers,
            body
        });
    }

    private static string BuildResponsePayload(HttpResponse response, CappedCapturingStream capturing)
    {
        var contentType = response.ContentType ?? string.Empty;
        var isBinary = BinaryContentTypePrefixes.Any(p => contentType.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        var body = isBinary
            ? $"[binary content-type {contentType}, {capturing.TotalBytesWritten} bytes, không capture nội dung]"
            : capturing.Truncated
                ? $"{capturing.GetCapturedText()}...(truncated)"
                : capturing.GetCapturedText();

        return JsonConvert.SerializeObject(new
        {
            statusCode = response.StatusCode,
            contentType,
            body
        });
    }

    /// <summary>
    /// Stream pass-through: mọi Write/WriteAsync chuyển thẳng tới stream gốc (client vẫn nhận dữ
    /// liệu streaming bình thường, không delay/buffer), đồng thời giữ lại tối đa maxBytes đầu tiên
    /// vào bộ nhớ để phục vụ log — bất kể response lớn bao nhiêu, RAM dùng cho việc capture không
    /// vượt quá maxBytes.
    /// </summary>
    private sealed class CappedCapturingStream(Stream inner, int maxBytes) : Stream
    {
        private readonly MemoryStream _captured = new(Math.Max(maxBytes, 0));

        public long TotalBytesWritten { get; private set; }
        public bool Truncated { get; private set; }

        public string GetCapturedText() => Encoding.UTF8.GetString(_captured.ToArray());

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Capture(buffer.AsSpan(offset, count));
            inner.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            await inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            await inner.WriteAsync(buffer, cancellationToken);
        }

        private void Capture(ReadOnlySpan<byte> data)
        {
            TotalBytesWritten += data.Length;
            if (maxBytes <= 0) return;

            var remaining = maxBytes - (int)_captured.Length;
            if (remaining <= 0)
            {
                Truncated = true;
                return;
            }

            var take = Math.Min(data.Length, remaining);
            _captured.Write(data[..take]);
            if (take < data.Length) Truncated = true;
        }
    }
}

/// <summary>Extension đăng ký middleware trong Program.cs.</summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestResponseLoggingMiddleware>();
}

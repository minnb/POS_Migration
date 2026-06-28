using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POS.Api.Middleware;
using POS.Common;

namespace POS.ContractTests;

/// <summary>
/// GUARDRAIL G3 — chứng minh HÀNH VI của ExceptionHandlingMiddleware, không chỉ "không vỡ
/// startup". Một exception chưa xử lý phải trả đúng hợp đồng ResultResponse mà 5.000 máy POS
/// đang parse: HTTP 500, JSON PascalCase, KHÔNG có field "Data" (NullValueHandling.Ignore).
///
/// Không cần Redis/SQL — dựng HttpContext trong bộ nhớ, RequestServices rỗng (middleware
/// resolve logger bằng GetService(...) + null-safe nên trả null là an toàn).
/// </summary>
public class ExceptionMiddlewareTests
{
    private static async Task<(int statusCode, string contentType, string body)> RunWithThrowingPipeline()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new ExceptionHandlingMiddleware(next);

        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Request.Path = "/api/v2/test";
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;

        await middleware.InvokeAsync(context);

        bodyStream.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(bodyStream).ReadToEndAsync();
        return (context.Response.StatusCode, context.Response.ContentType ?? string.Empty, body);
    }

    [Fact]
    public async Task Unhandled_exception_returns_500_json()
    {
        var (statusCode, contentType, _) = await RunWithThrowingPipeline();

        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        Assert.Contains("application/json", contentType);
    }

    [Fact]
    public async Task Response_matches_ResultResponse_contract()
    {
        var (_, _, body) = await RunWithThrowingPipeline();

        var result = JsonConvert.DeserializeObject<ResultResponse>(body);
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result!.Status);
        Assert.False(string.IsNullOrEmpty(result.Message));
        Assert.Equal("boom", result.MessageTechnical);
    }

    [Fact]
    public async Task Json_uses_PascalCase_and_omits_null_Data()
    {
        var (_, _, body) = await RunWithThrowingPipeline();

        var json = JObject.Parse(body);
        // PascalCase — khớp DefaultContractResolver của Program.cs (KHÔNG camelCase).
        Assert.True(json.ContainsKey("Status"));
        Assert.True(json.ContainsKey("Message"));
        Assert.True(json.ContainsKey("MessageTechnical"));
        // NullValueHandling.Ignore — Data null bị bỏ; POS nhận response KHÔNG có field Data.
        Assert.False(json.ContainsKey("Data"));
    }
}

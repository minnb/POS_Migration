using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Common;
using POS.Infrastructure.Logging;

namespace POS.Api.Middleware;

/// <summary>
/// Lưới an toàn cuối cùng: bắt MỌI exception chưa được controller xử lý và trả về đúng
/// <see cref="ResultResponse"/> mà 5.000 máy POS đang parse — thay vì trang lỗi mặc định
/// của ASP.NET Core (HTML / problem-details, sai contract).
///
/// Đặt ĐẦU pipeline (trước mọi middleware khác) để bao trùm toàn bộ request. Controller
/// vẫn giữ try/catch riêng khi cần message nghiệp vụ cụ thể — middleware này chỉ chạm tới
/// những exception lọt ra ngoài, nên KHÔNG đổi hành vi các đường đi đã xử lý sẵn.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    // Khớp CHÍNH XÁC cấu hình serialize global trong Program.cs:
    // DefaultContractResolver (PascalCase) + NullValueHandling.Ignore (bỏ field null).
    private static readonly JsonSerializerSettings _settings = new()
    {
        ContractResolver = new DefaultContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Đã bắt đầu gửi response → không thể ghi đè, để pipeline tự xử lý.
            if (context.Response.HasStarted) throw;

            var endpoint = context.Request.Path.Value ?? string.Empty;

            // Log qua hạ tầng sẵn có (resolve trong request scope).
            var kibana = context.RequestServices.GetService<IKibanaService>();
            kibana?.LogException(endpoint, string.Empty, (int)HttpStatusCode.InternalServerError,
                "Unhandled exception", ex.ToString());
            var fileLog = context.RequestServices.GetService<IFileLogHelper>();
            fileLog?.WriteExpLogs($"ExceptionHandlingMiddleware:{endpoint}", ex);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var body = JsonConvert.SerializeObject(new ResultResponse
            {
                Status = HttpStatusCode.InternalServerError,
                Message = "Đã xảy ra lỗi hệ thống.",
                Data = null,
                MessageTechnical = ex.Message
            }, _settings);

            await context.Response.WriteAsync(body);
        }
    }
}

/// <summary>Extension đăng ký middleware trong Program.cs.</summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UsePosExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}

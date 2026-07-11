using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Api.Filters;
using POS.Api.Middleware;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Security;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ── Giải mã credentials đã mã hóa (enc:...) — PHẢI trước AddInfrastructure ──
// Logic dùng chung ở POS.Infrastructure.Security.ConfigurationSecretExtensions
// (chung cho POS.Api / POS.Web / POS.Worker — tránh copy-paste).
builder.Configuration.DecryptEncryptedSecrets();

// ── Serilog ───────────────────────────────────────────────────────────────
builder.AddSerilogWithElastic();

// ── Controllers + Newtonsoft.Json + Global filters ────────────────────────
// QUAN TRỌNG: DefaultContractResolver giữ PascalCase như Web API 2 cũ.
// ASP.NET Core mặc định CamelCase → 5.000+ máy POS sẽ vỡ nếu không override.
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidateModelFilter>();
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
    });

// ── Tắt ModelStateInvalidFilter mặc định của [ApiController] ─────────────
// [ApiController] thêm ModelStateInvalidFilter (order -2000) trả problem-details
// format, chạy TRƯỚC ValidateModelFilter của chúng ta.
// SuppressModelStateInvalidFilter = true → ValidateModelFilter kiểm soát hoàn toàn,
// trả ResultResponse chuẩn (đúng contract với 5.000+ máy POS).
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

// ── Memory Cache ──────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Request/Response logging (toàn cục, bật/tắt qua config) ───────────────
builder.Services.Configure<RequestLoggingOptions>(
    builder.Configuration.GetSection(RequestLoggingOptions.SectionName));

// ── PosApiKeyMiddleware options (timestamp window) ────────────────────────
builder.Services.Configure<PosApiKeyAuthOptions>(
    builder.Configuration.GetSection(PosApiKeyAuthOptions.SectionName));

// ── Application services ──────────────────────────────────────────────────
builder.Services.AddApplication();

// ── Infrastructure (DB, Redis, RabbitMQ, Logging) ─────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── SyncTableList.POSLastCounter flush worker ─────────────────────────────
// Channel là in-memory, chỉ sống trong đúng tiến trình ghi master data (POS.Api/POS.Web) —
// KHÔNG đăng ký qua WorkerRolesOptions (dành riêng cho POS.Worker). Xem lý do đầy đủ ở
// .claude/rules/masterdata-sync.md mục "Cập nhật POSLastCounter bất đồng bộ".
builder.Services.AddHostedService<POS.Infrastructure.Sync.SyncTableCounterFlushWorker>();

// ── Authentication: Basic Auth ────────────────────────────────────────────
builder.Services
    .AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);
builder.Services.AddAuthorization();

// ── HTTP Client Factory ───────────────────────────────────────────────────
builder.Services.AddHttpClient();

// ── Swagger (chỉ DEV) ─────────────────────────────────────────────────────
// Swagger chỉ được đăng ký khi chạy Debug/Development.
// UAT và PROD không khởi tạo để tránh lộ API docs.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "POS API",
            Version = "v1",
            Description = "API hỗ trợ 5.000+ máy POS — chỉ khả dụng ở môi trường Development."
        });

        // Basic Auth scheme cho nút Authorize trên Swagger UI
        options.AddSecurityDefinition("BasicAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            Description = "Nhập username/password theo Basic Authentication."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "BasicAuth"
                    }
                },
                []
            }
        });
    });
}

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Log request/response toàn cục — ĐẶT NGOÀI CÙNG để bao trùm cả response lỗi
// chuẩn hoá mà ExceptionHandlingMiddleware tự viết ra khi có exception. ─────
app.UseRequestResponseLogging();

// ── Lưới an toàn cuối: bắt mọi exception chưa xử lý, trả đúng ResultResponse ──
app.UsePosExceptionHandling();
app.UseSerilogRequestLogging(options =>
{
    // Chỉ ghi log request ở mức có giá trị tra cứu — request thành công (2xx/3xx) và
    // 404 dò tìm thông thường hạ xuống Debug (bị chặn bởi MinimumLevel.Default=Warning).
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode is >= 400 and not 404)
            return LogEventLevel.Warning;

        return LogEventLevel.Debug;
    };
});
//app.UsePosApiKeyAuth();
app.UseAuthentication();
app.UseAuthorization();

// ── Swagger UI (chỉ DEV) ──────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "POS API v1");
        options.RoutePrefix = "swagger";   // truy cập: http://localhost:5147/swagger
    });
}

// ── Health check (anonymous) — dùng cho Docker HEALTHCHECK / load balancer ─
app.MapGet("/health", () => Results.Text("OK"));

app.MapControllers();

app.Run();

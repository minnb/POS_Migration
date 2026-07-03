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

var builder = WebApplication.CreateBuilder(args);

// ── Giải mã credentials đã mã hóa (enc:...) — PHẢI trước AddInfrastructure ──
// Quét mọi giá trị config chứa token "enc:" (vd Password trong connection string),
// giải mã bằng AES-256-GCM với khóa từ env POS_SECRET_KEY, rồi nạp đè in-memory.
// Mọi consumer (GetConnectionString / GetSection<RabbitMQOptions>) tự nhận plaintext.
// No-op khi không có token enc: → DEV/base plaintext vẫn chạy, không cần khóa.
{
    var encryptedEntries = builder.Configuration.AsEnumerable()
        .Where(kv => SecretProtector.HasToken(kv.Value))
        .ToList();
    if (encryptedEntries.Count > 0)
    {
        var secretKey = Environment.GetEnvironmentVariable("POS_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                "Có giá trị cấu hình mã hóa (enc:...) nhưng thiếu biến môi trường POS_SECRET_KEY. " +
                "Đặt khóa AES base64 (32 byte) vào POS_SECRET_KEY (tạo khóa tại /admin/encrypt-secret của POS.Web).");

        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in encryptedEntries)
            overrides[kv.Key] = SecretProtector.DecryptTokens(kv.Value!, secretKey);
        builder.Configuration.AddInMemoryCollection(overrides);
    }
}

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

// ── Application services ──────────────────────────────────────────────────
builder.Services.AddApplication();

// ── Infrastructure (DB, Redis, RabbitMQ, Logging) ─────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

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

// ── Lưới an toàn cuối: bắt mọi exception chưa xử lý, trả đúng ResultResponse ──
// Đặt ĐẦU pipeline để bao trùm toàn bộ request.
app.UsePosExceptionHandling();
app.UseSerilogRequestLogging();
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

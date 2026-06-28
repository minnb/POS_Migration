using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Api.Filters;
using POS.Api.Middleware;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

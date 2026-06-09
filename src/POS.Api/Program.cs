using Microsoft.AspNetCore.Authentication;
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

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

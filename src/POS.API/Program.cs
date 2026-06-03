using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Application;
using POS.Infrastructure;
using POS.API.Filters;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// ── Controllers với Newtonsoft.Json (giữ nguyên behavior API cũ) ────────
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Giữ PascalCase như API cũ (không dùng camelCase)
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        // Enum serialize thành integer — HttpStatusCode.OK → 200 (giống API cũ)
        options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
    });

// ── Application + Infrastructure DI ─────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// BasicAuthFilter đăng ký dưới dạng service để dùng với [ServiceFilter]
builder.Services.AddScoped<BasicAuthFilter>();

// Suppress automatic 400 — controller tự validate như API cũ
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

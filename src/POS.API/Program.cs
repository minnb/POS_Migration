using Dapper;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.API.Filters;
using POS.API.HealthChecks;
using POS.Application;
using POS.Infrastructure;
using Serilog;
using StackExchange.Redis;

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
        options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
    });

// ── Application + Infrastructure DI ─────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// BasicAuthFilter
builder.Services.AddScoped<BasicAuthFilter>();

// Suppress automatic 400 — controller tự validate như API cũ
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", failureStatus: HealthStatus.Unhealthy, tags: ["db"])
    .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: ["cache"]);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// GET /health — trả JSON chi tiết từng component
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };
        await ctx.Response.WriteAsync(JsonConvert.SerializeObject(result));
    }
});

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using MudBlazor.Services;
using POS.Application;
using POS.Infrastructure;
using POS.Web.Auth;
using POS.Web.Components;
using POS.Web.Services;
using POS.Web.Services.Pdf;
using QuestPDF.Infrastructure;
using System.Globalization;

// ── Culture mặc định: vi-VN ──────────────────────────────────────────
// Nhất quán định dạng số/ngày (dấu '.' ngăn nghìn) giữa màn hình, PDF và mọi page,
// kể cả khi chạy Docker/Linux (mặc định Invariant → dấu ','). Các chỗ cần Invariant
// (CSS width) đã truyền culture tường minh nên không bị ảnh hưởng.
var viVN = CultureInfo.GetCultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture   = viVN;
CultureInfo.DefaultThreadCurrentUICulture = viVN;

// ── QuestPDF license ─────────────────────────────────────────────────
// BẮT BUỘC set trước khi GeneratePdf, nếu không QuestPDF ném exception.
// LƯU Ý: Community chỉ miễn phí cho tổ chức doanh thu < 1 triệu USD/năm.
// Nếu dự án mua license trả phí → đổi sang LicenseType.Professional / Enterprise.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── MudBlazor ────────────────────────────────────────────────────────
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
});

// ── Blazor Server ─────────────────────────────────────────────────────
// DetailedErrors (chỉ Dev): khi circuit ném exception → client thấy chi tiết,
// đồng thời ghi đầy đủ vào server log để chẩn đoán (mặc định client chỉ thấy "Failed to rejoin").
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment()
            || builder.Configuration.GetValue<bool>("WebApp:EnableDetailedErrors");
    })
    // Nới giới hạn message cho Blazor circuit hub (mặc định 32KB) — defense-in-depth
    // tránh circuit bị tear-down khi render batch / interop lớn. Gắn TRỰC TIẾP vào
    // server components hub (đúng pattern Blazor Server), không dùng global Configure<HubOptions>.
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 512 * 1024; // 512 KB
    });

// ── Infrastructure: DB, Redis, RabbitMQ, Elasticsearch, HttpClients ──
// Bao gồm: CentralMDConnectionFactory, LoyaltyConnectionFactory,
//           IRedisService, IRabbitMQProducer, IKibanaService, IFileLogHelper,
//           tất cả 6 Repository, 4 AppService
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application Services: ICommonService, IHealthCheckService, ... ───
// Bao gồm: ICommonService, IAkaChainLoyaltyService, IGotITService,
//           IUrboxService, IDataRawService, ISyncDataPosService,
//           IHealthCheckService, IKafkaService
builder.Services.AddApplication();

// ── Web-specific services ─────────────────────────────────────────────
builder.Services.AddScoped<IWebUserService, WebUserService>();
builder.Services.AddScoped<ISqlConsoleService, SqlConsoleService>();
builder.Services.AddSingleton<IPdfExportService, PdfExportService>();

// ── Authentication: Cookie cho browser session ─────────────────────────
// TÁCH BIỆT với BasicAuth của POS.Api (không ảnh hưởng nhau)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/login";
        options.LogoutPath       = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(
            builder.Configuration.GetValue<int>("WebApp:SessionTimeoutHours", 8));
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly  = true;
        options.Cookie.SameSite  = SameSiteMode.Lax;
        // Relative redirect để browser giữ nguyên port qua nginx (tránh mất :8080)
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                ctx.Response.Redirect("/login?ReturnUrl=" + returnUrl);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.Redirect("/access-denied");
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization: 3 policy tương ứng 3 role ─────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WebPolicies.StoreAndAbove,
        p => p.RequireRole(WebRoles.StoreOperator, WebRoles.ITOps, WebRoles.SystemAdmin));
    options.AddPolicy(WebPolicies.OpsAndAbove,
        p => p.RequireRole(WebRoles.ITOps, WebRoles.SystemAdmin));
    options.AddPolicy(WebPolicies.AdminOnly,
        p => p.RequireRole(WebRoles.SystemAdmin));
});

// ── Utilities ─────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMemoryCache();

// ── Reverse proxy (nginx) — trust forwarded headers ───────────────────
// Cho phép Kestrel nhận đúng IP / scheme từ nginx X-Forwarded-* headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Xóa whitelist mặc định để chấp nhận proxy nội bộ bất kỳ
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Phải đứng đầu pipeline — đọc X-Forwarded-* từ nginx trước khi middleware khác dùng Host/IP
app.UseForwardedHeaders();

// Blazor framework endpoint selector chỉ match host=localhost (sinh ra lúc build).
// Rewrite Host cho /_framework/ requests để serve được từ external IP.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/_framework"))
        ctx.Request.Headers.Host = "localhost";
    await next();
});

app.UseRouting(); // explicit — đặt routing SAU middleware rewrite Host, disable auto-routing ở đầu pipeline

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Sign-in handler — bridge từ Blazor InteractiveServer sang HTTP pipeline để set cookie
// Token 1 lần dùng, TTL 30s, tạo trong Login.razor sau khi validate credentials
app.MapGet("/account/signin/{token}", async (HttpContext ctx, string token, IMemoryCache cache) =>
{
    if (!cache.TryGetValue($"_login_{token}", out System.Security.Claims.ClaimsPrincipal? principal) || principal is null)
        return Results.Redirect("/login");
    cache.Remove($"_login_{token}");
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
        new AuthenticationProperties { IsPersistent = true });
    return Results.Redirect("/");
}).AllowAnonymous();

// Logout handler — xử lý server-side để clear cookie đúng cách
app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

// Health check endpoint cho Docker HEALTHCHECK và load balancer
app.MapGet("/health", () => Results.Ok("healthy")).AllowAnonymous();

app.Run();

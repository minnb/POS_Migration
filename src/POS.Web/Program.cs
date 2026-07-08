using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using MudBlazor.Services;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Web.Auth;
using POS.Web.Components;
using POS.Web.Services;
using POS.Web.Services.Pdf;
using POS.Infrastructure.Security;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;

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
                "Đặt khóa AES base64 (32 byte) vào POS_SECRET_KEY (tạo khóa tại /admin/encrypt-secret).");

        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in encryptedEntries)
            overrides[kv.Key] = SecretProtector.DecryptTokens(kv.Value!, secretKey);
        builder.Configuration.AddInMemoryCollection(overrides);
    }
}

// ── Serilog ───────────────────────────────────────────────────────────────
// Thiếu dòng này khiến mọi ILogger<T> (kể cả KibanaService.LogInfo/LogException dùng ở
// hầu hết page) rơi về default provider của ASP.NET Core (Console-only trên Linux) — log
// KHÔNG bao giờ chạm "Logging:FileLogDirectory" hay Elasticsearch dù appsettings đã cấu hình đúng.
builder.AddSerilogWithElastic();

// ── Security mode (config-driven topology) ───────────────────────────
// Mode điều khiển cách xử lý forwarded headers cho 3 kịch bản triển khai:
//   BehindProxy : reverse proxy (nginx) terminate TLS, app chạy HTTP nội bộ → tin X-Forwarded-* từ proxy đã khai báo.
//   DirectHttps : Kestrel tự serve HTTPS, không proxy                       → KHÔNG xử lý forwarded headers.
//   Internet    : expose công cộng, không proxy (= DirectHttps + SameSite=Strict).
// RequireHttps TÁCH BIỆT với mode — quyết định CÓ ép HTTPS hay CHO PHÉP HTTP:
//   false → Cookie SameAsRequest, KHÔNG redirect/HSTS  ⇒ chạy được HTTP (vd đang test Production qua HTTP).
//   true  → Cookie Secure=Always + HSTS (+ redirect nếu DirectHttps/Internet) ⇒ ép HTTPS.
// Development luôn nới (HTTP localhost) bất kể cấu hình để không phá dev local.
var securityMode  = (builder.Configuration.GetValue<string>("Security:Mode") ?? "BehindProxy").Trim();
var requireHttps  = builder.Configuration.GetValue<bool>("Security:RequireHttps");
var internetMode  = securityMode.Equals("Internet", StringComparison.OrdinalIgnoreCase);
var behindProxy   = securityMode.Equals("BehindProxy", StringComparison.OrdinalIgnoreCase);
var directHttps   = internetMode || securityMode.Equals("DirectHttps", StringComparison.OrdinalIgnoreCase);
var secureCookies = requireHttps && !builder.Environment.IsDevelopment();
var enableHsts    = builder.Configuration.GetValue<bool>("Security:EnableHsts");
var enableSecHdrs = builder.Configuration.GetValue("Security:EnableSecurityHeaders", true);
var knownProxies  = builder.Configuration.GetSection("Security:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
var knownNetworks = builder.Configuration.GetSection("Security:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();

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
builder.Services.AddScoped<IAuditLogger, DbAuditLogger>();
builder.Services.AddScoped<ILogFileService, LogFileService>();

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
        options.Cookie.SameSite  = internetMode ? SameSiteMode.Strict : SameSiteMode.Lax;
        // Secure: chỉ ép Always khi RequireHttps=true (ngoài Dev) → cookie phiên không bao giờ qua HTTP.
        // Khi RequireHttps=false (vd đang test Production qua HTTP) → SameAsRequest để đăng nhập HTTP vẫn chạy.
        options.Cookie.SecurePolicy = secureCookies
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
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

// ── Reverse proxy — trust forwarded headers (chỉ mode BehindProxy) ────
// Cho Kestrel nhận đúng IP/scheme từ X-Forwarded-* CHỈ TỪ proxy đã khai báo.
// Khai báo Security:KnownProxies (IP) / Security:KnownNetworks (CIDR) ở production
// để KHÔNG tin X-Forwarded-* giả mạo từ client tùy ý.
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var ip in knownProxies)
            if (IPAddress.TryParse(ip, out var addr)) options.KnownProxies.Add(addr);
        foreach (var cidr in knownNetworks)
            if (System.Net.IPNetwork.TryParse(cidr, out var net)) options.KnownIPNetworks.Add(net);

        // Chưa khai báo proxy hợp lệ nào → tạm tin mọi proxy (giữ tương thích triển khai
        // hiện tại). Có cảnh báo lúc khởi động để nhắc ops khai báo IP proxy thật.
        // (KnownProxies/KnownIPNetworks đã được clear ở trên = trust-all khi cả hai rỗng.)
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Cảnh báo nếu đang ở BehindProxy mà chưa khai báo IP proxy → đang tin X-Forwarded-* từ mọi nguồn.
if (behindProxy && knownProxies.Length == 0 && knownNetworks.Length == 0 && !app.Environment.IsDevelopment())
    app.Logger.LogWarning(
        "Security: ForwardedHeaders đang tin MỌI proxy (Security:KnownProxies/KnownNetworks trống). " +
        "Hãy khai báo IP reverse proxy thật ở production để tránh giả mạo X-Forwarded-*.");

// HSTS + HTTPS redirect — chỉ khi RequireHttps=true (ngoài Development).
if (requireHttps && !app.Environment.IsDevelopment())
{
    if (enableHsts) app.UseHsts();
    // Redirect HTTP→HTTPS chỉ khi app/Kestrel tự serve HTTPS (DirectHttps/Internet).
    // BehindProxy: proxy đã terminate TLS → KHÔNG redirect (tránh vòng lặp), chỉ phát HSTS.
    if (directHttps) app.UseHttpsRedirection();
}

// Đọc X-Forwarded-* từ reverse proxy (chỉ mode BehindProxy) — phải trước middleware dùng Host/IP/scheme.
if (behindProxy)
    app.UseForwardedHeaders();

// ── Security headers (M1) — đặt đầu pipeline để phủ mọi response ──────
// CSP hợp với Blazor Server (WebSocket circuit qua connect-src 'self', script ngoài qua 'self')
// + MudBlazor (inject <style> nội tuyến → style-src 'unsafe-inline') + Google Fonts.
// Tắt nhanh bằng Security:EnableSecurityHeaders=false nếu phát hiện vỡ.
if (enableSecHdrs)
{
    app.Use(async (ctx, next) =>
    {
        var h = ctx.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"]        = "DENY";
        h["Referrer-Policy"]        = "strict-origin-when-cross-origin";
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data: https://fonts.gstatic.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "script-src 'self'; " +
            "connect-src 'self'; " +
            "frame-src 'self' blob:; " +   // preview PDF dùng iframe src=blob: (SalesByCategoryPage)
            "form-action 'self'";
        await next();
    });
}

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

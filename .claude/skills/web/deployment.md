---
name: web-deployment
description: Deploy POS.Web production (Docker/nginx/self-contained) — UseRouting, _framework/blazor.web.js 404, nginx WebSocket config, DataProtection keys. Đọc khi deploy hoặc debug lỗi cấu hình production.
---

# Production Deployment — POS.Web

> **Áp dụng khi:** deploy POS.Web lên production (Docker / nginx / self-contained) hoặc gặp lỗi `_framework/blazor.web.js` 404, cookie crash, WebSocket không kết nối.

---

## Cấu hình 3 môi trường (DEV / UAT / Production)

| Hạng mục | DEV (debug local) | UAT | Production |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `UAT` | `Production` |
| Chạy qua nginx? | ❌ Kestrel trực tiếp | ✅ nginx | ✅ nginx |
| appsettings overlay | `appsettings.Development.json` | `appsettings.UAT.json` | `appsettings.Production.json` |
| nginx conf | — | `nginx/pos-web.uat.conf` | `nginx/pos-web.conf` |
| DetailedErrors | tự bật (`IsDevelopment()`) | qua `WebApp:EnableDetailedErrors=true` | `EnableDetailedErrors` (tắt khi ổn định) |

**Quy tắc cốt lõi:**
- DEV debug **KHÔNG đi qua nginx** → mọi circuit crash ở DEV là tầng app/browser, KHÔNG phải nginx. Đừng route DEV qua `pos-web.conf` production (hardcode `proxy_pass 127.0.0.1:5001`).
- Env name `UAT` khiến `IsDevelopment()` VÀ `IsProduction()` đều **false** → `UseDeveloperExceptionPage()` không chạy; detailed errors **chỉ** bật được qua config flag `WebApp:EnableDetailedErrors` (lý do flag này đọc từ config thay vì hardcode `IsDevelopment()`).
- `DetailedErrors` + `MaximumReceiveMessageSize` cấu hình tại `AddInteractiveServerComponents().AddHubOptions(...)` (đúng pattern Blazor Server) — KHÔNG dùng global `Configure<HubOptions>`.

### Xử lý circuit crash / WebSocket ở DEV local (localhost, no proxy)

1. **Hard refresh `Ctrl+Shift+R`** — xóa stale circuit sau rebuild (assembly hash đổi → "Failed to rejoin"). Fix phần lớn trường hợp.
2. `dotnet dev-certs https --trust` — nếu dùng profile https (`wss://localhost:7200/_blazor` handshake fail khi cert chưa trust).
3. Dùng **1 scheme nhất quán** (đừng trộn `:5170` http và `:7200` https → WebSocket origin mismatch).
4. F12 → Console (lỗi thật) + Network filter `_blazor` (WebSocket phải `101 Switching Protocols`).
5. Đọc **server log console** (DEV đã bật DetailedErrors) → lấy exception thật từ page.

> Anti-pattern: ❌ kết luận "nginx sai" khi DEV crash — DEV không có nginx. ❌ tạo `appsettings.UAT.json` mà quên `EnableDetailedErrors=true` → UAT không thấy lỗi (vì `IsDevelopment()=false`).

---

## Pattern: Explicit UseRouting() để middleware chạy TRƯỚC routing

> Áp dụng khi: cần middleware tùy chỉnh chạy TRƯỚC endpoint routing (vd: rewrite Host header,
> request transformation). Trong .NET 9/10 `WebApplication`, `UseRouting()` tự động chèn vào
> ĐẦU pipeline trước mọi middleware → mọi rewrite header/path sau đó là quá muộn.

```csharp
// Program.cs — đặt middleware TRƯỚC app.UseRouting() tường minh
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/_framework"))
        ctx.Request.Headers.Host = "localhost"; // rewrite trước routing
    await next();
});

app.UseRouting(); // ← TƯỜNG MINH — vô hiệu hóa auto-UseRouting ở đầu pipeline

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

> **Anti-pattern:** Không gọi `app.UseRouting()` tường minh → routing tự động chạy trước mọi thứ.
> Ví dụ thực tế: `src/POS.Web/Program.cs`

---

## Pattern: Fix `_framework/blazor.web.js` 404 từ external IP

> Áp dụng khi: deploy Blazor Server trong Docker/nginx với port mapping (external port ≠ internal
> port), `blazor.web.js` trả 404 từ browser nhưng 200 từ `curl localhost`.

**Root cause:** Trong .NET 10, `_framework/` endpoint được build với host selector = `localhost`.
Request từ browser có `Host: <public-ip>:<port>` → không match → 404.

**Kiểm tra nhanh:**
```bash
# Nếu kết quả khác nhau → đây đúng là lỗi này
curl -s http://localhost:5001/_framework/blazor.web.js                          # → 200
curl -s -H "Host: <ip>:5001" http://localhost:5001/_framework/blazor.web.js    # → 404
```

**Fix trong `Program.cs`** (kết hợp với explicit UseRouting ở trên):
```csharp
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/_framework"))
        ctx.Request.Headers.Host = "localhost";
    await next();
});
app.UseRouting(); // BẮT BUỘC đi kèm — xem pattern explicit UseRouting ở trên
```

> Ví dụ thực tế: `src/POS.Web/Program.cs`

---

## Pattern: nginx config cho Blazor Server (production-hardened)

> Áp dụng khi: deploy POS.Web với nginx làm reverse proxy.
> Config chuẩn tại: `nginx/pos-web.conf` — copy trực tiếp, không dùng template đơn giản bên dưới.

### Checklist bắt buộc

| # | Hạng mục | Lý do |
|---|---|---|
| 1 | `proxy_http_version 1.1` | WebSocket chỉ chạy trên HTTP/1.1 |
| 2 | `map $http_upgrade $connection_upgrade` + header | Upgrade WS đúng cách |
| 3 | `proxy_buffering off` + `add_header X-Accel-Buffering "no"` | Tắt nginx buffer cho SSE/WebSocket — thiếu `X-Accel-Buffering` có thể vẫn buffer nội bộ |
| 4 | `proxy_buffer_size 32k; proxy_buffers 8 32k` | Payload HTML > 64KB (SSR + Blazor state) → buffer nhỏ → nginx temp-file → circuit timeout |
| 5 | `location /_blazor` riêng với `proxy_read_timeout 86400s` | WebSocket duy trì 24h, không bị ngắt bởi idle timeout chung |
| 6 | `proxy_set_header Host $http_host` | Giữ đúng host+port browser |

### Config chuẩn (rút gọn từ `nginx/pos-web.conf`)

```nginx
map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}

server {
    listen 8080;
    server_name _;

    proxy_read_timeout    1800s;   # long-polling fallback
    proxy_send_timeout    1800s;
    proxy_connect_timeout   30s;

    # BẮT BUỘC: buffer đủ lớn cho Blazor SSR payload
    proxy_buffer_size         32k;
    proxy_buffers           8 32k;
    proxy_busy_buffers_size   64k;

    # Dedicated block cho /_blazor SignalR WebSocket hub
    location /_blazor {
        proxy_pass         http://127.0.0.1:5001;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection $connection_upgrade;
        proxy_set_header   Host       $http_host;
        proxy_set_header   X-Real-IP  $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_buffering    off;
        add_header         X-Accel-Buffering "no";  # tắt nginx internal buffer layer
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout    86400s;  # 24h — WebSocket không bị ngắt giữa session
        proxy_send_timeout    86400s;
    }

    location / {
        proxy_pass         http://127.0.0.1:5001;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection $connection_upgrade;
        proxy_set_header   Host       $http_host;
        proxy_set_header   X-Real-IP  $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host  $http_host;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering    off;
        add_header         X-Accel-Buffering "no";
    }
}
```

### Anti-patterns nginx + Blazor Server

- ❌ `proxy_buffers 4 16k` (64KB) → production HTML > 64KB → nginx temp-file → circuit timeout
- ❌ `proxy_buffering off` mà không có `X-Accel-Buffering "no"` → nginx vẫn buffer ở internal layer
- ❌ Không có `location /_blazor` riêng → WebSocket dùng chung `proxy_read_timeout 1800s` → idle ngắt sau 30 phút
- ❌ `proxy_set_header Connection "upgrade"` hardcode → không xử lý non-WebSocket request (cần dùng `map`)
- ❌ Load balancer phía trước nginx mà không có **sticky session** → Blazor circuit reconnect vào backend khác → crash

**Build self-contained cho linux:**
```bash
dotnet publish src/POS.Web/POS.Web.csproj -c Release -r linux-x64 --self-contained true -o publish/POS.Web
```

---

## Pattern: Serilog PHẢI được wire tường minh trong từng `Program.cs` — không tự "kế thừa" giữa project

> Áp dụng khi: tạo project host mới (`Program.cs` của POS.Api/POS.Web/POS.Worker) hoặc audit vì sao
> log không xuất hiện ở `Logging:FileLogDirectory`/Elasticsearch dù `appsettings` đã cấu hình đúng.

**Root cause đã gặp thật (2026-07-08):** `src/POS.Web/Program.cs` thiếu hẳn dòng
`builder.AddSerilogWithElastic()` (có ở `src/POS.Api/Program.cs`) — không phải lỗi cấu hình, không
phải lỗi quyền thư mục. `KibanaService` (dùng ở ~50 trang POS.Web theo convention chuẩn) inject
`ILogger<KibanaService>` (abstraction `Microsoft.Extensions.Logging`, không phải `Serilog.Log.Logger`
tĩnh) — provider thật của `ILogger<T>` chỉ đổi sang Serilog (kèm File sink + Elasticsearch sink) khi
có `builder.Host.UseSerilog(...)`, chính là việc `AddSerilogWithElastic()` làm. Thiếu dòng này →
`ILogger<T>` toàn bộ project rơi về default provider ASP.NET Core (Console-only trên Linux) — log
**biến mất hoàn toàn**, không lỗi, không cảnh báo lúc khởi động.

```csharp
// Program.cs — BẮT BUỘC có ở MỌI project host (Api/Web), đặt SAU bước giải mã enc:...,
// TRƯỚC mọi builder.Services.Add...() khác:
using POS.Infrastructure.Logging;

builder.AddSerilogWithElastic();
```

**Checklist khi tạo project host mới hoặc audit logging:**
- Grep `AddSerilogWithElastic|UseSerilog` trong `Program.cs` của project — phải có đúng 1 lần.
- Không có `Serilog.Debugging.SelfLog.Enable(...)` nào trong repo — nếu nghi ngờ Serilog tự nuốt lỗi
  ghi file (permission denied...), tạm thêm dòng này để lộ lỗi qua console/journalctl khi debug.
- `IFileLogHelper.WriteLogs/WriteExpLogs` (`POS.Infrastructure.Logging.FileLogHelper`) là cơ chế ghi
  file THỦ CÔNG, tách biệt hoàn toàn khỏi Serilog/`ILogger<T>` — vẫn hoạt động dù thiếu
  `AddSerilogWithElastic()`, nhưng tự nuốt lỗi im lặng (`catch { }`) nên không phải chỗ để tin tưởng
  chẩn đoán "log có ghi được không".

> Anti-pattern: ❌ giả định Serilog tự áp dụng cho mọi project chỉ vì `POS.Infrastructure` có sẵn
> `SerilogConfiguration.AddSerilogWithElastic()` — đây là extension method, PHẢI được gọi tường minh
> ở từng `Program.cs`, không tự động chạy theo `AddInfrastructure()`.
> Ví dụ thực tế: `src/POS.Web/Program.cs`, `src/POS.Api/Program.cs`,
> `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`.

---

## Pattern: DataProtection keys trong Docker

> Áp dụng khi: app chạy trong Docker container với non-root user (`USER $APP_UID`).
> ASP.NET Core DataProtection cần ghi key vào `/home/app/.aspnet/DataProtection-Keys`.
> Volume Docker do root tạo → user `app` không ghi được → `CryptographicException` khi encrypt cookie.

```dockerfile
# Dockerfile — TRƯỚC USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

USER $APP_UID
```

> Ví dụ thực tế: `src/POS.Web/Dockerfile`

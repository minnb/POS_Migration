# Production Deployment — POS.Web

> **Áp dụng khi:** deploy POS.Web lên production (Docker / nginx / self-contained) hoặc gặp lỗi `_framework/blazor.web.js` 404, cookie crash, WebSocket không kết nối.

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

## Pattern: nginx config cho Blazor Server

> Áp dụng khi: deploy POS.Web với nginx làm reverse proxy (không có hoặc thay thế Docker).

```nginx
server {
    listen 5001;
    server_name _;

    # WebSocket — BẮT BUỘC cho Blazor SignalR circuit
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";

    proxy_set_header Host $http_host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

    proxy_read_timeout 300s;   # Blazor long-polling fallback cần timeout dài
    proxy_send_timeout 300s;

    location / {
        proxy_pass http://localhost:8080;
    }
}
```

**Build self-contained cho linux (chạy không cần .NET runtime):**
```bash
dotnet publish src/POS.Web/POS.Web.csproj -c Release -r linux-x64 --self-contained true -o publish/POS.Web
```

> Anti-pattern: Quên `proxy_set_header Upgrade` → SignalR WebSocket không upgrade được →
> Blazor circuit không kết nối → button/event không phản hồi.

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

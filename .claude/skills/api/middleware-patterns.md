---
name: api-middleware-patterns
description: Middleware tầng pipeline POS.Api — xác thực X-API key toàn cục, tắt Kestrel MinResponseDataRate cho stream file lớn. Đọc khi thêm/sửa middleware trong POS.Api.
---

# Middleware Patterns — POS.Api

> **Áp dụng khi:** thêm middleware chạy ở tầng pipeline HTTP của POS.Api (khác filter/attribute
> theo controller).

---

## Pattern: Middleware xác thực request từ POS (X-API key)

> Áp dụng khi: cần validate MỌI request đến POS.Api ở tầng pipeline (không gắn `[Attribute]` từng controller).
> Fail-closed: thiếu credential → 401, không pass-through.

```csharp
// src/POS.Api/Middleware/PosApiKeyMiddleware.cs
public sealed class PosApiKeyMiddleware(RequestDelegate next)
{
    // Scoped service nhận qua THAM SỐ InvokeAsync — KHÔNG inject vào constructor
    // (middleware là singleton; tham số method được resolve đúng scope mỗi request).
    public async Task InvokeAsync(HttpContext context,
        ICentralMDRepository repo, IFileLogHelper fileLog)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        { await next(context); return; }            // miễn xác thực

        var xApi = context.Request.Headers["X-API"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xApi))
        {
            // privateKey lấy từ GetPOSDataSetupAsync() — đã cache Redis MD:POSDataSetup 12h
            var key = (await repo.GetPOSDataSetupAsync(context.RequestAborted))?
                .FirstOrDefault(x => string.Equals(x.Code, "X-API", StringComparison.OrdinalIgnoreCase))?.Value;
            var expected = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(key ?? "")));  // uppercase hex
            if (string.IsNullOrEmpty(key) || !string.Equals(xApi, expected, StringComparison.Ordinal))
            { await Write401(context, "Chưa xác thực"); return; }
            await next(context); return;
        }
        // Không X-API: có Authorization (Basic /api/v2/* | Bearer pending) → pass-through; thiếu cả → 401
        if (!string.IsNullOrEmpty(context.Request.Headers.Authorization.FirstOrDefault()))
        { await next(context); return; }
        await Write401(context, "Chưa xác thực");
    }
}
// Đăng ký: app.UsePosApiKeyAuth(); SAU UseSerilogRequestLogging(), TRƯỚC UseAuthentication().
```

**Quan trọng:**
- `MD5.HashData()` + `Convert.ToHexString()` → uppercase hex, khớp `MD5(privateKey).toUpper()` phía POS.
- Write401 phải dùng `DefaultContractResolver` + `NullValueHandling.Ignore` để khớp contract `ResultResponse` (PascalCase, bỏ `Data` null).
- ⚠️ Fail-closed → mọi endpoint (trừ `/health`, `/swagger/*`) bắt buộc có header; rà soát script/monitor nội bộ trước khi deploy.

> Ví dụ thực tế: `src/POS.Api/Middleware/PosApiKeyMiddleware.cs`

---

## Pattern: Tắt Kestrel MinResponseDataRate cho 1 request stream file lớn

> Áp dụng khi: endpoint stream file lớn (zip, export...) cho client mạng chậm/không ổn định (vd máy
> POS ở cửa hàng) — Kestrel mặc định tự ngắt kết nối nếu tốc độ gửi xuống dưới 240 byte/giây quá 5
> giây (`MinResponseDataRate`), dù server vẫn đang gửi đúng dữ liệu. Ngắt giữa chừng → client nhận
> file thiếu/lỗi, dễ nhầm là bug server trong khi thực ra là Kestrel chủ động cắt.

```csharp
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

// Trước khi bắt đầu stream — CHỈ tắt cho request này, KHÔNG đụng Program.cs/Kestrel global
// (tránh tắt bảo vệ chống slowloris cho toàn bộ API).
var minRateFeature = HttpContext.Features.Get<IHttpMinResponseDataRateFeature>();
if (minRateFeature != null)
    minRateFeature.MinDataRate = null;

await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
```

**Vì sao scope theo request, không sửa `Program.cs`:** endpoint public khác vẫn cần Kestrel bảo vệ
khỏi slow-loris; chỉ endpoint stream file lớn cho client mạng yếu mới cần nới lỏng.

> Ví dụ thực tế: `src/POS.Api/Controllers/SyncDataPosController.cs` — `DowloadFileStream`.

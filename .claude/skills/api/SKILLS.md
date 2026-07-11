---
name: api-external-config
description: Lấy cấu hình External API (host/credentials/routes) từ Redis/DB qua GetSysWebApiAsync — bắt buộc đọc trước khi tạo AppService gọi partner HTTP (GotIT, Urbox, AkaChain...).
---

# Skill: Lấy cấu hình External API từ Redis/DB (`GetSysWebApiAsync`)

> **Áp dụng khi:** migrate hoặc tạo mới bất kỳ AppService nào cần gọi external HTTP API
> (GotIT, Urbox, AkaChain, Giftee, Capillary, v.v.).
> Mọi thông tin cấu hình (host, credentials, routes, timeout) đều lấy từ DB bảng `SysWebApi` +
> `SysWebApiRoute`, được cache trong Redis.

## Skill con — đọc khi cần

| File | Đọc khi |
|---|---|
| [`middleware-patterns.md`](middleware-patterns.md) | Thêm/sửa middleware pipeline (X-API key auth, Kestrel MinResponseDataRate) |
| [`file-streaming-patterns.md`](file-streaming-patterns.md) | Sinh/stream/publish file quy mô lớn cho POS (Parallel.ForEachAsync, SHA-256, resolve path SyncDataPos) |
| [`logging.md`](logging.md) | Thêm log mới (IFileLogHelper/IKibanaService/middleware log request-response) |
| [`../database/SKILLS.md`](../database/SKILLS.md) | Viết SP/Repository (bao gồm pattern audit-log try/finally, OUTPUT param, UPDLOCK optional filter, đổi cột mã→tên) |
| [`../worker/SKILLS.md`](../worker/SKILLS.md) | Tách BackgroundService ra `POS.Worker` |
| [`../cache/SKILLS.md`](../cache/SKILLS.md) | Cache OAuth2 token hoặc bất kỳ master data nào |

---

## Nguyên tắc cốt lõi

**KHÔNG** hardcode URL hoặc credentials trong `appsettings.json` hay trong code.
**LUÔN** dùng `ICentralMDRepository.GetSysWebApiAsync(appCode)` để lấy config tại runtime.

---

## Luồng lấy config

```
AppService.SomeMethod(...)
  → centralMDRepository.GetSysWebApiAsync(appCode, ct)
      → Redis HashGet "MD:SysWebApi" / field = appCode
          ↓ cache miss
      → SELECT * FROM SysWebApi WHERE Blocked = 0 AND AppCode = @appCode
      → SELECT * FROM SysWebApiRoute WHERE Blocked = 0 AND AppCode = @appCode
      → Redis HashSet (TTL 43200s = 12 giờ)
      ↓
  SysWebApiDto { Host, UserName, Password, Authorization, PublicKey, PrivateKey,
                 Version, HttpProxy, Bypasslist, SysWebApiRoute[] }
```

---

## Redis details

| Thuộc tính | Giá trị |
|---|---|
| Key | `MD:SysWebApi` |
| Type | **Hash** |
| Field | `appCode` (ví dụ: `"GOTIT"`, `"URBOX"`, `"FMV"`) |
| TTL | **43200s (12 giờ)** |

---

## SysWebApiDto — ý nghĩa các field

| Field | Dùng cho |
|---|---|
| `Host` | Base URL của API bên thứ 3 |
| `UserName` | Username / client_id / tenant |
| `Password` | Password / client_secret |
| `Authorization` | API key, brand_id, hoặc header Authorization custom |
| `PublicKey` | RSA public key (ký/mã hoá request) |
| `PrivateKey` | RSA private key |
| `Version` | (overloaded) Phiên bản API (`"V2"`, `"V6"`) **hoặc** timeout tính bằng giây |
| `HttpProxy` | Proxy URL nếu cần |
| `Bypasslist` | Danh sách bypass proxy |
| `SysWebApiRoute[]` | Danh sách endpoint (Name → Route path) |

> **Lưu ý `Version`:** code cũ hay dùng `Version` để lưu timeout (`int.TryParse`).
> Nếu partner dùng `Version` cho API version (`"V2"`, `"V6"`), dùng `string.Equals`.
> Nếu dùng timeout: `int.TryParse(config.Version, out var t) && t > 0 ? t : 30`.

---

## Pattern chuẩn trong AppService

### 1. Inject repository

```csharp
public sealed class {Name}Service(
    ICentralMDRepository centralMDRepository,
    IHttpClientFactory httpClientFactory,
    IFileLogHelper fileLogHelper,
    IKibanaService kibanaService
) : I{Name}AppService
{
    private const string NotFoundConfig = "Không tìm thấy thông tin cấu hình";
```

### 2. Helper lấy config + route + timeout (khuyến nghị đặt thành private methods)

```csharp
// Config
private Task<SysWebApiDto?> GetConfigAsync(CancellationToken ct = default)
    => centralMDRepository.GetSysWebApiAsync("{APP_CODE}", ct);

// Route theo name
private static string? GetRoute(SysWebApiDto config, string routeName)
    => config.SysWebApiRoute?.FirstOrDefault(
        x => string.Equals(x.Name, routeName, StringComparison.OrdinalIgnoreCase))?.Route;

// Timeout (giây) — fallback 30s
private static int GetTimeout(SysWebApiDto config)
    => int.TryParse(config.Version, out var t) && t > 0 ? t : 30;
```

### 3. Gọi ở đầu mỗi method nghiệp vụ

```csharp
public async Task<...> SomeMethod(..., CancellationToken ct = default)
{
    var config = await GetConfigAsync(ct);
    if (config == null) return Fail(NotFoundConfig);

    var route = GetRoute(config, "RouteNameInDB");
    if (route == null) return Fail(NotFoundConfig);

    // Dùng: config.Host + route, config.UserName, config.Password, config.Authorization ...
}
```

---

## appCode — quy ước đặt tên

Dùng `PartnerEnum` nếu partner đã có trong enum, ngược lại dùng string literal trùng với cột `AppCode` trong DB:

```csharp
// Ưu tiên — tránh typo
centralMDRepository.GetSysWebApiAsync(PartnerEnum.URBOX.ToString(), ct)
centralMDRepository.GetSysWebApiAsync(PartnerEnum.GOTIT.ToString(), ct)

// Khi partner chưa có trong PartnerEnum
centralMDRepository.GetSysWebApiAsync("FMV", ct)
centralMDRepository.GetSysWebApiAsync("GIFTEE", ct)
```

---

## Trường hợp đặc biệt: OAuth2 token

Một số partner (AkaChain/FMV) yêu cầu Bearer token. Token cache Redis riêng qua `IRedisService`
(không qua `ICentralMDRepository`, vì token là per-partner chứ không phải master data dùng chung).

> **Pattern đầy đủ + code mẫu**: [`../cache/SKILLS.md`](../cache/SKILLS.md) mục "Pattern 3: OAuth2
> token caching" — đây là nguồn canonical, không lặp code ở đây.

---

## Checklist khi tạo AppService mới gọi external API

- [ ] Xác nhận `AppCode` tồn tại trong bảng `SysWebApi` (hỏi DBA / xem DB dev)
- [ ] Kiểm tra `SysWebApiRoute` có đủ route name cần dùng chưa
- [ ] Inject `ICentralMDRepository` (không inject Redis trực tiếp cho config)
- [ ] Thêm private `GetConfigAsync` + `GetRoute` + `GetTimeout` helpers
- [ ] Guard ngay đầu method: `if (config == null) return Fail(NotFoundConfig)`
- [ ] Nếu cần token OAuth2: xem [`../cache/SKILLS.md`](../cache/SKILLS.md) Pattern 3
- [ ] Xem `CLAUDE.md` — mục "Pattern bắt buộc" để đăng ký DI đúng 3 lớp

---

## Ví dụ tham chiếu

| Partner | AppCode | File |
|---|---|---|
| AkaChain/FMV | `"FMV"` | `src/POS.Infrastructure/AppServices/Partner/AkaChainLoyaltyAppService.cs` |
| GotIT | `PartnerEnum.GOTIT` | `src/POS.Infrastructure/AppServices/Partner/GotITService.cs` |
| Urbox | `PartnerEnum.URBOX` | `src/POS.Infrastructure/AppServices/Partner/UrboxService.cs` |

---

## Pattern: Mã hóa credentials trong appsettings (token `enc:`)

> Cấu hình mã hóa password DB/RabbitMQ trong appsettings (AES-256-GCM, cross-platform) — nguồn
> canonical đầy đủ: **`docs/architecture/appsetting.md`**. Không lặp lại chi tiết ở đây; chỉ nhớ
> gọi `builder.Configuration.DecryptEncryptedSecrets()` ngay sau `CreateBuilder`, trước
> `AddInfrastructure`, trong MỌI `Program.cs` (Api/Web/Worker).

---

## Pattern: Track Counter bump vào SyncTableList.POSLastCounter bất đồng bộ

> Khi 1 Repository ghi (insert/update) vào bảng master data có bump cột `Counter`, cần
> `ISyncTableTrackerService.Track(tableName, counter)` (non-blocking, Channel + BackgroundService)
> để đồng bộ tăng dần `SyncTableList.POSLastCounter` cho POS — KHÔNG update đồng bộ trong transaction
> ghi (gây lock contention).

> **Chi tiết đầy đủ + checklist rollout theo từng `TableName`**:
> `.claude/rules/masterdata-sync.md` mục "Cập nhật POSLastCounter bất đồng bộ" — đây là nguồn
> canonical, không lặp code ở đây.

---

## Pattern: Xác minh tên bảng vật lý / SP trả cột đã format sẵn / SP đổi mã→tên hiển thị / SP OUTPUT param

> 4 pattern Repository/SP dùng chung cho mọi DB (không riêng external API) đã chuyển sang
> **`.claude/skills/database/SKILLS.md`** — đọc file đó khi viết raw SQL/SP call mới.

---

## Pattern: Optional filter trong UPDLOCK transaction / Named CancellationToken

> Đã chuyển sang **`.claude/skills/database/SKILLS.md`** (cùng nhóm pattern Repository/transaction).

---

## Logging (IFileLogHelper / IKibanaService / RequestResponseLoggingMiddleware)

> Đã tách sang file riêng: **[`logging.md`](logging.md)** — 3 cơ chế logging trong POS.Api/
> POS.Infrastructure, khi nào dùng cái nào, pattern middleware log request/response toàn cục
> (capped pass-through stream, không buffer file lớn vào RAM), cờ `RequestLogging:PersistToFile`,
> và các bug/anti-pattern thực tế đã gặp. Đọc file đó TRƯỚC khi thêm log mới ở bất kỳ đâu.

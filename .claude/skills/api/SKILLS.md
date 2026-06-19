# Skill: Lấy cấu hình External API từ Redis/DB (`GetSysWebApiAsync`)

> **Áp dụng khi:** migrate hoặc tạo mới bất kỳ AppService nào cần gọi external HTTP API
> (GotIT, Urbox, AkaChain, Giftee, Capillary, v.v.).
> Mọi thông tin cấu hình (host, credentials, routes, timeout) đều lấy từ DB bảng `SysWebApi` +
> `SysWebApiRoute`, được cache trong Redis.

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

Một số partner (AkaChain/FMV) yêu cầu Bearer token. Token cũng cache Redis nhưng **không** qua `ICentralMDRepository` — cache trực tiếp bằng `IRedisService`:

```csharp
private const string TokenCacheKey = "{Partner}:{Service}:AccessToken";

private async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
{
    var cached = redis.StringGetRaw(TokenCacheKey);
    if (!string.IsNullOrEmpty(cached)) return cached;

    var config = await GetConfigAsync(ct);
    if (config == null) return null;

    var tokenRoute = GetRoute(config, "GetToken");
    if (tokenRoute == null) return null;

    // Gọi token endpoint → parse response
    var tokenData = JsonConvert.DeserializeObject<AccessTokenDto>(body);
    if (tokenData?.AccessToken == null) return null;

    var expiresIn = Math.Max((tokenData.ExpiresIn ?? 300) - 60, 60);
    redis.StringSetRaw(TokenCacheKey, tokenData.AccessToken, TimeSpan.FromSeconds(expiresIn));
    return tokenData.AccessToken;
}
```

> Chi tiết Redis token: xem [cache/SKILLS.md](../cache/SKILLS.md) — mục "OAuth token".

---

## Checklist khi tạo AppService mới gọi external API

- [ ] Xác nhận `AppCode` tồn tại trong bảng `SysWebApi` (hỏi DBA / xem DB dev)
- [ ] Kiểm tra `SysWebApiRoute` có đủ route name cần dùng chưa
- [ ] Inject `ICentralMDRepository` (không inject Redis trực tiếp cho config)
- [ ] Thêm private `GetConfigAsync` + `GetRoute` + `GetTimeout` helpers
- [ ] Guard ngay đầu method: `if (config == null) return Fail(NotFoundConfig)`
- [ ] Nếu cần token OAuth2: inject `IRedisService`, cache token riêng với key `{Partner}:AccessToken`
- [ ] Xem CLAUDE.md — mục "Pattern bắt buộc" để đăng ký DI đúng 3 lớp

---

## Ví dụ tham chiếu

| Partner | AppCode | File |
|---|---|---|
| AkaChain/FMV | `"FMV"` | `src/POS.Infrastructure/AppServices/AkaChainLoyaltyAppService.cs` |
| GotIT | `PartnerEnum.GOTIT` | `src/POS.Infrastructure/AppServices/GotITService.cs` |
| Urbox | `PartnerEnum.URBOX` | `src/POS.Infrastructure/AppServices/UrboxService.cs` |

---

## Pattern: Audit log table với try/finally trong Repository

> Áp dụng khi: cần ghi audit log sau mỗi lần insert/process data, bất kể thành công hay thất bại, kể cả khi có nhiều return path.

```csharp
// Khai báo tracking variables TRƯỚC try
bool   _flag     = false;
string _errorMsg = "";
string _dataType = "";
try
{
    // ... logic chính, cập nhật _flag/_errorMsg/_dataType ở mỗi nhánh ...
    _flag = true;
    return (true, "OK");
}
catch (Exception ex) { _errorMsg = ex.Message; return (false, _errorMsg); }
finally
{
    // finally LUÔN chạy — đảm bảo log dù return ở nhánh nào
    await InsertDataRawJsonAsync(transactionId, _dataType, message, _flag,
        _flag ? null : _errorMsg);
}

private async Task InsertDataRawJsonAsync(string transactionId, string dataType,
    string message, bool flag, string? errorMessage)
{
    try
    {
        using var conn = await directConnectionFactory.CreateOpenConnectionAsync(
            CancellationToken.None);  // CancellationToken.None — log phải chạy kể cả request bị cancel
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ... }, commandTimeout: Timeout));
    }
    catch { /* Swallow — nếu log fail, main processing đã fail → RabbitMQ retry tự động */ }
}
```

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/CentralSaleRepository.cs` — `InInsertToTableByJson` + `InsertDataRawJsonAsync`

**Anti-pattern:** Gọi log function ở từng return path riêng lẻ → dễ bỏ sót khi thêm nhánh mới.

---

## Pattern: POS.Worker — Background worker project độc lập

> Áp dụng khi: cần tách BackgroundService ra khỏi POS.Api để tránh ảnh hưởng health check hoặc restart API.

```csharp
// POS.Worker/Program.cs — Worker Service SDK (Microsoft.NET.Sdk.Worker)
var builder = Host.CreateApplicationBuilder(args);  // HostApplicationBuilder, KHÔNG phải WebApplicationBuilder
builder.AddSerilogWithElastic();                     // overload HostApplicationBuilder trong SerilogConfiguration.cs
builder.Services.AddInfrastructure(builder.Configuration);  // DB, Redis, RabbitMQ, Repos
builder.Services.AddHostedService<PosSalesConsumerWorker>();
// Thêm worker mới sau này: chỉ cần thêm dòng AddHostedService<T>() — không cần project mới
var host = builder.Build();
host.Run();
```

**Điểm quan trọng:**
- SDK: `Microsoft.NET.Sdk.Worker` — không phải `Microsoft.NET.Sdk`
- Docker image: `dotnet/runtime:10.0` (không phải `aspnet`) — nhẹ hơn ~60MB vì không có HTTP
- Env var: `DOTNET_ENVIRONMENT` (không phải `ASPNETCORE_ENVIRONMENT`)
- **KHÔNG gọi `AddApplication()`** — worker chỉ cần `AddInfrastructure()`, gọi thêm sẽ đăng ký HTTP client không cần thiết
- `SerilogConfiguration.cs` cần overload riêng cho `HostApplicationBuilder` (khác `WebApplicationBuilder`)

> Ví dụ thực tế: `src/POS.Worker/`, `Dockerfile.worker`, `docker-compose.yml` service `worker`

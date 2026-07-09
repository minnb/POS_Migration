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
| AkaChain/FMV | `"FMV"` | `src/POS.Infrastructure/AppServices/Partner/AkaChainLoyaltyAppService.cs` |
| GotIT | `PartnerEnum.GOTIT` | `src/POS.Infrastructure/AppServices/Partner/GotITService.cs` |
| Urbox | `PartnerEnum.URBOX` | `src/POS.Infrastructure/AppServices/Partner/UrboxService.cs` |

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

**Gotcha (2026-07-08):** `InInsertToTableByJson` từng mở connection chính (không phải audit log)
qua `StoreRoutedConnectionFactory` (route theo `StoreSetServer`) — khi `ServerIP` của 1 store
không còn kết nối được trên UAT/Prod, method throw "network-related... SQL Server" dù các hàm đọc
cùng bảng vẫn chạy bình thường (chúng dùng `directConnectionFactory` cố định). Đã đổi sang luôn
dùng `directConnectionFactory`. Bài học: chỉ dùng `StoreRoutedConnectionFactory` khi thật sự cần
ghi vào bảng **sharded theo store** (TransHeader...); nếu SP/bảng đích không phụ thuộc shard, ưu
tiên `directConnectionFactory` để tránh thêm 1 điểm lỗi mạng không cần thiết.

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

---

### Pattern: Optional filter param trong UPDLOCK transaction

> Áp dụng khi: cần enforce thêm điều kiện (VoucherType, loại hàng, v.v.) bên trong transaction
> có UPDLOCK — check PHẢI nằm trong transaction, không thể làm ngoài (TOCTOU risk).

```csharp
// Interface — thêm optional param, caller cũ không bị break
Task<...> RedeemVouchersAsync(
    List<(string VoucherNumber, double AmountRedeem)> serials,
    string orderNo,
    string? requiredVoucherType = null,   // ← optional, default null = bỏ qua check
    CancellationToken ct = default);

// Repository — check ngay sau SELECT UPDLOCK, trước UPDATE
if (requiredVoucherType != null)
{
    var wrongType = vouchers.FirstOrDefault(v => v.VoucherType != requiredVoucherType);
    if (wrongType != null) { tx.Rollback(); return (false, $"Voucher ... không phải loại {requiredVoucherType}", []); }
}
```

> Anti-pattern: check VoucherType trước rồi mới gọi transaction → race condition giữa check và update.
> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/SAPVoucherRepository.cs`

---

### Pattern: Named CancellationToken khi thêm optional param vào giữa signature

> Áp dụng khi: thêm optional param mới vào giữa signature của method đang có caller dùng positional args.

```csharp
// Lỗi compile — CancellationToken truyền nhầm vào optional string? mới thêm:
repo.RedeemVouchersAsync(serials, orderNo, ct);     // ct → slot của string? ❌

// Đúng — dùng named param để CancellationToken vào đúng slot:
repo.RedeemVouchersAsync(serials, orderNo, ct: ct); // ✅
```

> Quy tắc: Khi thêm optional param vào giữa signature, scan toàn bộ callers và thêm `ct: ct` nếu cần.
> Ví dụ thực tế: `src/POS.Application/Services/SAPService.cs` — `RedeemCpnVchAsync`

---

## Pattern: Mã hóa credentials trong appsettings (token `enc:` + config hook)
> Áp dụng khi: cần giấu password (DB/RabbitMQ) trong appsettings mà KHÔNG chuyển sang env var,
> vẫn giữ file config commit được. Cross-platform (Docker Linux) → AES-GCM, KHÔNG dùng DPAPI (Windows-only).

```csharp
// 1) Helper static AES-256-GCM — token "enc:" + base64(nonce(12)|tag(16)|ciphertext)
//    src/POS.Infrastructure/Security/SecretProtector.cs
SecretProtector.Encrypt(plain, base64Key);         // → "enc:...."
SecretProtector.DecryptTokens(value, base64Key);   // thay MỌI "enc:..." trong 1 chuỗi
// → cho phép mã hóa CHỈ phần password: "...;Password=enc:XXX;..." (regex dừng ở ';')

// 2) Hook giải mã trong Program.cs — NGAY SAU CreateBuilder, TRƯỚC AddInfrastructure:
var enc = builder.Configuration.AsEnumerable().Where(kv => SecretProtector.HasToken(kv.Value)).ToList();
if (enc.Count > 0) {
    var key = Environment.GetEnvironmentVariable("POS_SECRET_KEY")
              ?? throw new InvalidOperationException("Có enc:... nhưng thiếu POS_SECRET_KEY"); // fail-fast
    var ov = new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in enc) ov[kv.Key] = SecretProtector.DecryptTokens(kv.Value!, key);
    builder.Configuration.AddInMemoryCollection(ov);   // mọi GetConnectionString/GetSection tự nhận plaintext
}
```

- **Không sửa từng factory** — giải mã ở tầng config nên `GetConnectionString` / `GetSection<RabbitMQOptions>` nhận plaintext tự động.
- **No-op khi không có `enc:`** → môi trường chưa mã hóa (Dev) chạy bình thường, không cần khóa. **Fail-fast** nếu có `enc:` mà thiếu khóa.
- Khóa qua env (`POS_SECRET_KEY`), giá trị thật ở `.env` (gitignore) — KHÔNG commit khóa. Dùng CHUNG 1 khóa cho POS.Api và POS.Web.

> **Anti-pattern:** ❌ mã hóa `appsettings.json` (base) → MỌI môi trường (kể cả Dev không có khóa) fail-fast. Chỉ mã hóa file môi trường (Production).
> Ví dụ thực tế: `SecretProtector.cs`, `src/POS.Api/Program.cs`, `src/POS.Web/Program.cs`, trang tạo token `/admin/encrypt-secret`; rollout: `docs/ROLLOUT.md`; tra cứu nhanh: `docs/architecture/appsetting.md`

> **Sinh key/token ngoài app đang chạy (không qua UI `/admin/encrypt-secret`):** `AesGcm` không có trong
> .NET Framework → PowerShell 5.1 Windows không gọi được trực tiếp. Tạo project console tạm (net10.0)
> với `ProjectReference` tới `POS.Infrastructure.csproj`, gọi thẳng `SecretProtector.GenerateKey()` /
> `.Encrypt()` / `.Decrypt()` (verify round-trip ngay trong script trước khi dùng), `dotnet run`, rồi xóa
> project tạm — đảm bảo byte-for-byte tương thích với code decrypt thật, không tự viết lại AES-GCM.

---

## Pattern: Parallel.ForEachAsync cho nhiều DB call độc lập

> Áp dụng khi: cần xử lý N item (ví dụ N bảng SP2) mà mỗi item **mở connection riêng, không shared state** → song song hóa an toàn.

```csharp
// Precompute index TRƯỚC khi song song (index ổn định, không race condition)
var entries = items
    .Where(t => !string.IsNullOrWhiteSpace(t.Key))
    .Select((t, idx) => (Item: t, Index: idx + 1))
    .ToList();

await Parallel.ForEachAsync(entries, new ParallelOptions
{
    MaxDegreeOfParallelism = _opt.MaxParallelTables > 0 ? _opt.MaxParallelTables : 1,
    CancellationToken = ct
}, async (entry, token) =>
{
    // Mỗi iteration mở SqlConnection riêng → hoàn toàn thread-safe
    await repo.StreamTableToFilesAsync(entry.Item, ..., token);
});
```

**Điều kiện an toàn:** (1) mỗi iteration tạo connection/resource riêng; (2) output (file, key) unique per-item; (3) exception 1 item → `AggregateException` wrap throw ra caller.
**Cấu hình:** `MaxParallelTables <= 0` → sequential (fallback an toàn). SQL Server connection pool (default 100) đủ cho parallelism = 4–8.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs` — `EnsureMasterDataFileAsync`

---

## Pattern: SHA-256 companion file cho binary được publish

> Áp dụng khi: publish file binary (zip, archive) ra đĩa và cần ops/monitoring verify integrity sau này.

```csharp
// Sau atomic publish (File.Move overwrite)
File.Move(tmpZip, destPath, overwrite: true);

var hash = await ComputeSha256HexAsync(destPath, ct);
await File.WriteAllTextAsync(destPath + ".sha256", hash, ct);  // "a3f5c2e1..." (64 hex chars)

// Cleanup: xóa .sha256 cùng lúc với zip
TryDeleteFile(destPath);
TryDeleteFile(destPath + ".sha256");

// Helper (BCL .NET 6+ — không cần NuGet)
private static async Task<string> ComputeSha256HexAsync(string filePath, CancellationToken ct)
{
    await using var fs = File.OpenRead(filePath);
    var bytes = await SHA256.HashDataAsync(fs, ct);
    return Convert.ToHexString(bytes).ToLowerInvariant();
}
```

**Quan trọng:** file `.sha256` là companion, KHÔNG thêm vào response API (filter `*.zip` → `.sha256` không bị liệt kê). Verify trên server: `sha256sum {file}.zip` rồi so sánh với nội dung `.sha256`.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`

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

---

## Pattern: Tách N file output theo cờ DB (thay vì appsettings) — idempotent all-or-nothing

> Áp dụng khi: 1 batch job sinh ra nhiều file, và "cái gì tách riêng" là quyết định **vận hành** (DBA
> đổi theo dữ liệu thực tế của từng thời điểm), KHÔNG phải quyết định lúc code/deploy → đặt cờ trên
> chính bảng metadata nguồn (SP1) thay vì `appsettings.json`, để đổi hành vi KHÔNG cần deploy lại app.

```csharp
// 1. Metadata row có thêm cờ (SyncTableInfo.IsSingleFile) — Dapper tự map cột SP mới, không cần sửa Repository.
var outDir = row.IsSingleFile ? Path.Combine(tmpDir, SanitizeForFolder(row.Key)) : Path.Combine(tmpDir, "_common");

// 2. Idempotent check phải là ALL-OR-NOTHING trên TOÀN BỘ danh sách output dự kiến của lượt chạy
//    (tính được ngay sau khi có metadata, TRƯỚC khi chạy job) — không regenerate lẻ từng file.
var expectedNames = new List<string> { CommonName() };
expectedNames.AddRange(singleKeys.Select(SingleName));
if (expectedNames.All(n => IsTodayValid(Path.Combine(targetDir, n))))
    return expectedNames.Select(n => Success(n)).ToList();

// 3. Publish + cleanup dùng CHUNG 1 prefix, loại trừ theo HashSet "vừa publish lượt này"
//    → tự dọn được file mồ côi khi cờ IsSingleFile bị TẮT lại (không cần logic cleanup riêng cho case này).
CleanupSiblingZips(req, publishedNamesThisRun);
```

**Vì sao KHÔNG dùng appsettings cho việc này:** cấu hình trong `appsettings.json` cần deploy/restart để
đổi; cờ trên bảng DB cho phép DBA `UPDATE` trực tiếp + `DEL` cache Redis liên quan để có hiệu lực ngay,
phù hợp khi danh sách "cái gì cần tách riêng" thay đổi theo dữ liệu thực tế từng site/thời điểm.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs` —
> `EnsureMasterDataFileAsync` tách zip theo `SyncTableList.IsSingleFile`
> (`docs/sql/SyncTableList_AddIsSingleFile.sql`), fix timeout download POS với zip quá lớn.

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

## Pattern: Xác minh tên bảng vật lý trước khi viết raw SQL

> Áp dụng khi: viết raw SQL/SP call mới nhắm vào bảng đã tồn tại trong `RPOSMasterData`.
> Rút ra từ sự cố thực tế: `CentralMDRepository` từng dùng `dbo.POSTerminalBanks` và `dbo.Banks`
> (số nhiều — suy đoán theo convention EF DbSet cũ), trong khi tên bảng vật lý thật là
> `dbo.POSTerminalBank`, `dbo.Bank` (số ít). Query chạy thẳng vào production DB thật sẽ throw
> `Invalid object name` — chỉ phát hiện lúc runtime, không phải lúc build.

**Cách xác minh đúng — tra `docs/architecture/centralMD-schema.md` (nguồn sự thật schema DB
theo quy tắc ở `CLAUDE.md`), KHÔNG suy đoán tên bảng theo convention số ít/số nhiều:**
1. Mở `docs/architecture/centralMD-schema.md`, tìm đúng tên bảng + cột + kiểu dữ liệu + PK.
2. Bảng cần dùng chưa có trong doc → đọc `docs/sql/database/CentralMD.sql` (nguồn gốc sinh ra
   `centralMD-schema.md`) để lấy tên chính xác, rồi bổ sung vào `centralMD-schema.md` cùng commit.
3. **KHÔNG** tự thêm/bớt "s" theo thói quen đặt tên DbSet — luôn đối chiếu tên bảng vật lý thật.

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`
> (`GetBankPOSListAsync`/`SaveBankPOSAsync`/`DeleteBankPOSAsync` → `dbo.POSTerminalBank`;
> `GetBankListForDropdownAsync` → `dbo.Bank`)

---

## Pattern: Map SP trả cột đã format/localize sẵn (khác kiểu bảng vật lý)

> Áp dụng khi: gọi 1 SP có sẵn (không tự viết) mà SELECT convert cột sang dạng hiển thị
> (vd `IIF(Status=1, N'Đang dùng', N'Không dùng')`, `Format(Date,'dd/MM/yyyy')`,
> `Convert(varchar,Counter)`) — kiểu cột trả về KHÁC kiểu cột vật lý trong bảng, map thẳng
> vào DTO dùng kiểu vật lý (bool/int/DateTime) sẽ làm Dapper throw lỗi cast ngay dòng đầu tiên.

```csharp
// Repository — KHÔNG map thẳng vào DTO public (BankPOSListDto), dùng row riêng khớp đúng
// cột SP trả (text/string), rồi convert sang kiểu UI cần trong bước project.
var rows = await QueryAsync<BankPOSListRow>(sql, param, ct: ct);
return rows.Select(r => new BankPOSListDto
{
    IsOnline = r.IsOnline == "Có",                              // text tiếng Việt → bool
    Status   = r.Status == "Đang được sử dụng" ? 1 : 0,          // text tiếng Việt → int (round-trip Save)
    StatusText = r.Status,                                      // giữ nguyên text để hiển thị/export
    Counter  = r.Counter,                                       // varchar sẵn — giữ string, không ép int
}).ToList();

private sealed class BankPOSListRow { public string IsOnline {get;set;} = ""; /* ... khớp đúng tên+kiểu cột SP trả */ }
```

**Quan trọng:**
- Giữ nguyên field kiểu "gốc" (vd `Status` int) trên DTO nếu còn nơi khác (form Edit/Save) cần round-trip đúng kiểu đó — chỉ thêm field mới (`StatusText`) cho phần hiển thị, KHÔNG đổi kiểu field đang được dùng để ghi ngược lại DB.
- Dapper `QueryAsync<T>` không throw khi property DTO không có cột khớp (giữ default) — an toàn khi SP sau này thêm cột mới (vd thêm `PartnerId` vào SELECT) mà không cần sửa code map nếu đã khai báo sẵn field tương ứng.

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs` (`GetBankPOSListAsync` + `BankPOSListRow`)

---

## Pattern: Xử lý đường dẫn file POS gửi (SyncDataPos) — luôn giải về FtpRootPath, dùng chung
> Áp dụng khi: endpoint nhận `filePath`/`pathSync` từ máy POS (download/delete/list file trong FTPBLUEPOS).
> Rút ra từ 2 bug thực tế trong `SyncDataPosController` (download OK nhưng delete/list lại rỗng/sai thư mục).

- **POS gửi UNC Windows** (`\\ip\FTPBLUEPOS\...`) — trên **Linux Docker không resolve**. Dùng chung 1 helper
  `ISyncDataPosService.ResolveFtpPhysicalPath(posPath)`: tách phần sau `FTPBLUEPOS` rồi `MapFtpPath` về
  `FtpRootPath` local. Mọi endpoint (download/delete) phải map trước khi `File.Exists`/`Delete`; endpoint xóa
  thêm **guard path-traversal** (`fullLocal.StartsWith(MapFtpPath(""))`).
- **`pathSync` POS gửi đã chứa đủ `SyncDataPos/POS/{typeSync}`** → giải thư mục list/tạo qua
  `MapFtpPath($"{pathSync}/{folderFile}")` cho MỌI typeSync (ALL/CHANGE) để listing khớp nơi file được tạo +
  khớp UNC `PathFileIPServer` + URL download.
- **Anti-pattern**: dùng `AppSettings:FolderShare` + tự ghép `\{typeSync}\` cho nhánh CHANGE → thiếu segment
  `SyncDataPos\POS`, và hardcode `syncdatapos/pos` lowercase → **sai case trên Linux**. Đừng suy đoán path bằng
  `FolderShare`; luôn bám `MapFtpPath` + `pathSync` từ query (đồng nhất với nhánh ALL).
- **Tham số hoá hành vi theo caller qua request DTO, KHÔNG detect caller**: thêm field nullable vào DTO nội bộ
  (vd `GetMasterDataFileRequest.SyncAction`) để override (Web Sync="DELETE-INSERT", null=mặc định TRUNC-INSERT→INSERT)
  — DTO nội bộ nên không phá contract test.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`ResolveFtpPhysicalPath`,
> `GetFileFromServerApiAsync`, `PushStartOfDayDataAsync`), `src/POS.Api/Controllers/SyncDataPosController.cs`
> (`DowloadFileStream`/`DeleteFileFromFTP`/`GetFileFromFTP`), `MasterDataSyncService.ActionFor`

---

## Logging (IFileLogHelper / IKibanaService / RequestResponseLoggingMiddleware)

> Đã tách sang file riêng: **[`logging.md`](logging.md)** — 3 cơ chế logging trong POS.Api/
> POS.Infrastructure, khi nào dùng cái nào, pattern middleware log request/response toàn cục
> (capped pass-through stream, không buffer file lớn vào RAM), cờ `RequestLogging:PersistToFile`,
> và các bug/anti-pattern thực tế đã gặp (`JsonConvert.SerializeObject(ex)` trên object đã dispose,
> filter Serilog theo giá trị property chứ không theo tên property...). Đọc file đó TRƯỚC khi thêm
> log mới ở bất kỳ đâu.

### Pattern: SP trả kết quả qua OUTPUT param khi ủy quyền SP-legacy có result set
> Áp dụng khi: SP mới `EXEC` một SP legacy tự `SELECT` (vd Interface_Errors) và/hoặc có `ROLLBACK` bên trong.

Không thể hứng result set legacy bằng `INSERT...EXEC` nếu SP legacy có `ROLLBACK` ("Cannot use the
ROLLBACK statement within an INSERT-EXEC statement"). Nếu để result set legacy lọt ra, Dapper
`QueryFirstOrDefault<T>` đọc NHẦM set đầu → `null` → báo lỗi giả. Giải pháp: trả `@Ok bit/@Message`
qua **OUTPUT param**; repository dùng `ExecuteAsync` (ExecuteNonQuery nuốt hết result set rồi mới gán output).

```csharp
p.Add("@Ok", dbType: DbType.Boolean, direction: ParameterDirection.Output);
p.Add("@Message", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);
await conn.ExecuteAsync(new CommandDefinition("dbo.usp_X", p, commandType: CommandType.StoredProcedure));
var ok = p.Get<bool?>("@Ok") ?? false;
```
> Ví dụ thực tế: `docs/sql/SetupSalePrice_Save.sql`, `src/POS.Infrastructure/Repositories/Price/PriceRepository.cs` (`SaveAsync`).

---

### Pattern: SP đổi 1 cột từ mã (code) sang tên hiển thị (name) — luôn thêm cột mã gốc riêng cho composite key

> Áp dụng khi: sửa/mở rộng 1 SP list có sẵn để JOIN thêm bảng lookup và **thế** cột mã bằng cột tên hiển thị
> (vd `SalesCode` từ trả `PriceGroupCode` đổi sang trả `PriceGroupName` cho đẹp UI). Rút ra từ sự cố thực tế:
> `GetSalesPriceList` đổi `SalesCode` sang trả tên nhóm giá, nhưng code Sửa/Xóa (`PriceRowKey`) vẫn dùng
> đúng field đó làm khoá gửi tới `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (đang lọc theo **mã**, không phải
> tên) → mọi thao tác Sửa/Xóa sẽ báo "Không tìm thấy dữ liệu" ngay khi Code ≠ Name.

**Quy tắc**: mỗi khi 1 cột SP đang được dùng làm khoá composite (Update/Delete/lookup ngược) bị đổi ý nghĩa
sang giá trị hiển thị, **PHẢI** thêm 1 cột mới song song mang mã gốc (lấy thẳng từ bảng vật lý, không qua
JOIN lookup có thể `LEFT JOIN` miss), map vào 1 field riêng trên DTO (đặt tên rõ ràng kiểu `XxxCode`, có
comment "KHÔNG hiển thị — dùng làm khoá"), rồi sửa nơi build khoá composite dùng field mã mới thay vì field
hiển thị cũ.

```sql
-- SP list — thêm cột mã gốc song song với cột tên hiển thị đã đổi
ISNULL(G.PriceGroupName,'') AS SalesCode,       -- tên hiển thị (đã đổi ý nghĩa)
ISNULL(S.[SalesCode],'')    AS SalesGroupCode,  -- mã gốc — LẤY THẲNG từ bảng vật lý, dùng cho Sửa/Xóa
```
```csharp
// DTO — field mã gốc tách riêng, comment rõ mục đích
public string? SalesCode { get; set; }       // tên hiển thị — cột lưới
public string? SalesGroupCode { get; set; }  // mã gốc — KHÔNG hiển thị, dùng build PriceRowKey
```

> Anti-pattern: tiếp tục dùng field cũ (`row.SalesCode`) để build khoá sau khi ý nghĩa cột đã đổi — lỗi
> không xuất hiện lúc build/test (kiểu vẫn là `string`), chỉ lộ ra khi chạy thật với dữ liệu có Code≠Name.
> Ví dụ thực tế: `docs/sql/GetSalesPriceList_AddSaleType.sql` (`SalesGroupCode`),
> `docs/sql/GetSalesPriceList_AddSalesTypeCode.sql` (`SalesTypeCode`),
> `src/POS.Web/Components/Pages/Catalog/Price/PricesPage.razor` (`TryBuildKey`).

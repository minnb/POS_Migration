# POS Migration — Claude Code Context

## Dự án
Migrate POS.API từ .NET Framework 4.6 → .NET 10.
- Source cũ: `POS.Backend/API_Common/` và `POS.Backend/API_BLUEPOS/`
- Solution mới: `POS.slnx`

## Cấu trúc Solution (Clean Architecture)

```
src/
├── POS.Common/          DTOs, Enums, ResultResponse  (Domain models)
├── POS.Infrastructure/  Repositories, Redis, RabbitMQ (Infrastructure)
├── POS.Application/     Services, Interfaces          (Application/Business logic)
└── POS.Api/             Controllers, Filters          (Presentation)
```

**Dependency flow:**
```
POS.Api → POS.Application → POS.Infrastructure → POS.Common
POS.Api → POS.Infrastructure (DI registration)
POS.Api → POS.Common
```

### POS.Application — quy tắc
- Namespace: `POS.Application.Interfaces` và `POS.Application.Services`
- Interface service: `I{Name}Service` trong `Interfaces/`
- Implementation: `{Name}Service` trong `Services/`
- Service inject repository interface (từ `POS.Infrastructure.Repositories.Interfaces`)
- Service inject `IRedisService` (từ `POS.Infrastructure.Redis`)
- Service inject `IRabbitMQProducer` (từ `POS.Infrastructure.Messaging`)
- Service inject `I{Name}AppService` (từ `POS.Infrastructure.AppServices.Interfaces`) khi cần gọi external HTTP
- **KHÔNG** inject concrete class (chỉ inject interface)
- **Controller BẮT BUỘC inject Application interface** — KHÔNG inject Infrastructure interface trực tiếp

### POS.Infrastructure — quy tắc
- Repositories: `src/POS.Infrastructure/Repositories/`
- Interfaces repository: `src/POS.Infrastructure/Repositories/Interfaces/`
- AppServices (HTTP client wrappers): `src/POS.Infrastructure/AppServices/`
- Interfaces AppService: `src/POS.Infrastructure/AppServices/Interfaces/` — đặt tên `I{Name}AppService`
- Redis: `src/POS.Infrastructure/Redis/` (IRedisService, RedisService)
- Redis internals: `src/POS.Infrastructure/Cache/` (IRedisManager, RedisManager, RedisOptions)
- Messaging: `src/POS.Infrastructure/Messaging/` (IRabbitMQProducer, RabbitMQProducer)
- DB Factories: `src/POS.Infrastructure/Database/`

---

## Quy tắc AppService — BẮT BUỘC khi migrate external HTTP client

> Mọi service gọi external API (GotIT, Urbox, AkaChain, ...) **BẮT BUỘC** tuân theo pattern 3 lớp sau.

### Pattern bắt buộc

```
Controller (POS.Api)
  → inject I{Name}Service              ← POS.Application.Interfaces
    → Application/Services/{Name}Service     (thin wrapper — chỉ delegate, không có logic)
        → inject I{Name}AppService     ← POS.Infrastructure.AppServices.Interfaces
          → Infrastructure/AppServices/{Name}Service  (HTTP client thực sự)
```

### Ví dụ đã có (tham chiếu khi tạo service mới)

| Partner | Application interface | Infrastructure AppService |
|---|---|---|
| AkaChain/FMV | `IAkaChainLoyaltyService` | `IAkaChainLoyaltyAppService` / `AkaChainLoyaltyAppService` |
| GotIT | `IGotITService` | `IGotITAppService` / `GotITService` |
| Urbox | `IUrboxService` | `IUrboxAppService` / `UrboxService` |

### Checklist khi tạo service HTTP client mới

1. **Infrastructure**: Tạo `I{Name}AppService.cs` trong `AppServices/Interfaces/` — namespace `POS.Infrastructure.AppServices.Interfaces`
2. **Infrastructure**: Tạo `{Name}Service.cs` trong `AppServices/` — implements `I{Name}AppService`
3. **Infrastructure DI**: Đăng ký `services.AddScoped<I{Name}AppService, {Name}Service>()`
4. **Application**: Tạo `I{Name}Service.cs` trong `Interfaces/` — namespace `POS.Application.Interfaces`, **cùng signature** với `I{Name}AppService`
5. **Application**: Tạo `{Name}Service.cs` trong `Services/` — implements `I{Name}Service`, inject `I{Name}AppService`, mỗi method chỉ `=> appService.Method(...)`
6. **Application DI**: Đăng ký `services.AddScoped<I{Name}Service, {Name}Service>()`
7. **Controller**: Inject `I{Name}Service` (Application) — **KHÔNG** inject `I{Name}AppService` (Infrastructure)

### Quy tắc đặt tên

- Infrastructure interface: `I{Name}**App**Service` — có suffix `App` để phân biệt
- Application interface: `I{Name}Service` — không có suffix `App`
- Cả hai implementation class đều tên là `{Name}Service` (khác namespace)

---

## Quy tắc BẮT BUỘC khi làm việc với src/POS.Common/

### 1. Serialization: CHỈ dùng Newtonsoft.Json
- Package: `Newtonsoft.Json 13.*` (đã có trong `src/POS.Common/POS.Common.csproj`)
- Dùng `[JsonProperty("tên_gốc")]` nếu tên C# property **khác** với tên JSON field
- **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json` dưới bất kỳ hình thức nào
- Nếu source cũ dùng `[JsonPropertyName]` → convert sang `[JsonProperty]`
- Nếu source cũ dùng `JsonElement` → thay bằng `object?`

### 2. Lý do kinh doanh — KHÔNG ĐƯỢC THAY ĐỔI TÊN FIELD JSON
> 5.000 máy POS đang parse JSON response theo đúng tên field hiện tại.
> Thay đổi bất kỳ tên field nào sẽ phá vỡ production ngay lập tức.

### 3. C# 12 / .NET 10
- File-scoped namespace: `namespace POS.Common.Dtos.{Domain};`
- Nullable reference types: thêm `?` cho reference types
- Non-null required strings: `= string.Empty`
- Giữ nguyên: computed properties, inheritance chain, `[Required]`, `[StringLength]`

---

## Mapping Namespace (cũ → mới)

| Namespace cũ | Namespace mới |
|---|---|
| `TCX.API.Common.Dtos.{X}` | `POS.Common.Dtos.{X}` |
| `VCM.POSBLUE.Model.{X}` | `POS.Common.Dtos.{X}` |
| `VCM.POSBLUE.Model.Dtos.{X}` | `POS.Common.Dtos.{X}` |

---

## Cấu trúc src/POS.Common/ (97 files đã tạo)

```
src/POS.Common/
├── ResultResponse.cs
├── Enums/               (25 files)
└── Dtos/
    ├── (root)           AuthDto, HttpResponseBlueDto, KafkaMessage, NotifyConfigDto,
    │                    RabbitMessageDto, RedisDto, SMSMessage, SysWebApiDto, SysWebApiUserDto
    ├── B2B/
    ├── Capillary/       (Base, Tier, Redemption, Transaction, Customer, Enosta, Point, Coupons, Vouchers)
    ├── CentralMD/
    ├── Coupon/
    ├── CXVoucher/
    ├── DRW/
    ├── Giftee/
    ├── GotIT/
    ├── LogService/
    ├── Loyalty/         (Base, Transaction, CX, MemberBusiness, ProgramPoints, WinCode, WinScore)
    ├── MSN/
    ├── Ops/
    ├── PartnerApi/
    ├── POS/             (POSRequest, Gift/, ValidateTransactionDto)
    ├── Reward/
    ├── ROP/
    ├── StagingDB/
    ├── Tax/
    ├── Telegram/
    ├── TopupVoucherVinID/
    ├── Vouchers/
    ├── WinCare/
    ├── WinCustomer/
    ├── WinMoney/
    ├── Winpay/
    └── WinX/
```

---

## Thêm DTO mới: dùng lệnh `/add-dto`

Xem `.claude/commands/add-dto.md` để biết cách dùng.

---

## Quy tắc cấu hình External API — BẮT BUỘC

> **Chi tiết đầy đủ: `.claude/skills/api/SKILLS.md`** — đọc file này trước khi tạo hoặc migrate bất kỳ AppService nào gọi external HTTP API.

Mọi thông tin cấu hình (host, credentials, routes, timeout) đều lấy từ DB qua `ICentralMDRepository.GetSysWebApiAsync(appCode)` — đã cache Redis tự động.
**KHÔNG** hardcode URL hoặc credentials, **KHÔNG** đọc từ `appsettings.json`.

---

## Quy tắc Cache — Redis StandAlone (BẮT BUỘC)

> **Chi tiết đầy đủ: `.claude/skills/cache/SKILLS.md`** — đọc file này trước khi migrate bất kỳ function nào dùng cache.

### Nguyên tắc cốt lõi

Dự án cũ dùng IIS `MemoryCacheService` → dự án mới **BẮT BUỘC** dùng `IRedisService` (Redis StandAlone).
Mọi nơi code cũ gọi `_memoryCacheService.GetCache<T>(...)`, `GetSysWebApi()`, `GetLoyaltyRateData()`... → phải có Redis cache tương ứng trong project mới.

### Nơi đặt cache logic

| Loại data | Nơi cache | Interface |
|---|---|---|
| Master data từ DB (SysWebApi, stores, rates…) | `CentralMDRepository` hoặc `LoyaltyRepository` | Thêm method vào `ICentralMDRepository` / `ILoyaltyRepository` |
| OAuth2 token của external API | `{Name}AppService` trong `POS.Infrastructure/AppServices/` | Inject `IRedisService` trực tiếp |
| KHÔNG cache config trong Application/Service layer | — | — |

### Redis key convention

- Master data: `MD:{TableName}` — Hash (field = code/appCode) hoặc String (full list)
- OAuth token: `{Partner}:{Service}:AccessToken` — StringRaw

### TTL

- Config tĩnh (SysWebApi, CardLevel, Store...): `43200s` (12h)
- Rate/price data: `3600s` (1h)
- Short-lived (ItemPointsMember): `360s`
- OAuth token: `expires_in - 60s` (từ response)
- **KHÔNG** dùng no-TTL trong production

### Pattern bắt buộc trong Repository

```csharp
// Hash pattern (lookup theo code)
var cached = redis.HashGet<T>(KEY, field);
if (cached != null) return cached;
var data = await QueryFirstOrDefaultAsync<T>(sql, params, ct: ct);
if (data != null) redis.HashSet(KEY, field, data, ttlSeconds: 43200);
return data;

// String pattern (full list)
var cached = await redis.StringGetAsync<List<T>>(KEY);
if (cached?.Count > 0) return cached;
var data = (await QueryAsync<T>(sql, ct: ct)).ToList();
if (data.Count > 0) redis.StringSet(KEY, data, ttlSeconds: 43200);
return data;
```

### Checklist khi gặp MemoryCacheService trong code cũ

1. Tra bảng mapping MemoryCacheConst → Redis key trong `.claude/skills/cache/SKILLS.md`
2. Thêm method vào `ICentralMDRepository` nếu chưa có
3. Implement theo pattern Hash hoặc String với TTL
4. AppService/Service gọi qua Repository — KHÔNG gọi Redis trực tiếp (trừ token)

---

## Quy tắc migrate Controller — Rút ra từ thực tế

### A. DI Registration — BẮT BUỘC sau mỗi interface mới

Mỗi khi tạo `I{Name}Service` mới trong `POS.Application/Interfaces/`:
1. Tạo stub hoặc implementation trong `POS.Application/Services/` (hoặc `POS.Infrastructure/` nếu cần HTTP client / DB)
2. **Đăng ký ngay** trong `src/POS.Application/DependencyInjection.cs`:
   ```csharp
   services.AddScoped<I{Name}Service, {Name}Service>();
   ```
3. Nếu chưa implement thật, dùng stub trả `HttpStatusCode.NotImplemented` — KHÔNG throw exception.

> **Lý do**: Quên đăng ký DI → `InvalidOperationException` lúc runtime, không phải lúc build.

### B. ModelState Validation — `ValidateModelFilter` đã xử lý global

`Program.cs` đã cấu hình `SuppressModelStateInvalidFilter = true` để `ValidateModelFilter` kiểm soát hoàn toàn format response (trả `ResultResponse`, không phải ASP.NET problem-details).

**Hệ quả quan trọng khi migrate controller**:
- `ValidateModelFilter` chạy **trước** action method → `if (!ModelState.IsValid) return ExceptionModels()` trong action là **dead code** (không bao giờ được gọi).
- Vẫn có thể giữ dòng đó cho an toàn, nhưng không cần thiết.
- **TUYỆT ĐỐI KHÔNG** thêm `services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = false)` — sẽ phá vỡ contract.

### C. NullValueHandling.Ignore — Data: null bị omit

`Program.cs` cấu hình `NullValueHandling = NullValueHandling.Ignore`.
- Khi `ResultResponse.Data = null` → field `"Data"` bị bỏ qua trong JSON output.
- POS machines không nhận `"Data": null` mà nhận response không có field `Data`.
- Đây là behavior intentional (giảm bandwidth). **Không thay đổi**.

### D. Return type khi service trả ResultResponse

Nếu service trả `ResultResponse` (không phải plain data), KHÔNG dùng `OkResult(result)` — sẽ double-nest.

Dùng:
```csharp
// Khi HTTP status = service status (dynamic)
return StatusCode((int)result.Status, result);

// Khi HTTP status luôn 200 (như RefundTransaction cũ)
return Ok(result);

// Khi cần tùy chỉnh field (như GetCustomerDetail đặt clubCode vào MessageTechnical)
return StatusCode((int)status, new ResultResponse { Data = ..., Message = ..., Status = ..., MessageTechnical = ... });
```

`OkResult(data)` chỉ dùng khi `data` là object thuần (không phải `ResultResponse`).

### E. Helpers cũ không có trong POS.Common mới

Các helper sau **không tồn tại** trong `src/POS.Common/Helpers/` — inline trực tiếp:

| Helper cũ | Logic inline | Ghi chú |
|---|---|---|
| `NumberHelper.IsPhoneNumber(phone)` | `phone.Length >= 9 && phone.Length <= 11 && phone.All(char.IsDigit)` | Thêm `// TODO: extract to helper` |
| `LoyaltyHelper.MessageNotValidPhone(phone)` | `$"Số thẻ {phone} không hợp lệ"` | |
| `FormatHelper.PhoneNumberVietNam(phone)` | Chưa có trong mới | Cần tạo nếu dùng |
| `FileHelper.WriteExpLogs(...)` | → `_fileLogHelper.WriteExpLogs(...)` | Đã có `IFileLogHelper` |

### F. Swagger chưa được cấu hình

`Program.cs` chưa có `AddSwaggerGen()` / `UseSwagger()`. Route test dùng curl trực tiếp.

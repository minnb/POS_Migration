# POS API — Claude Code Context

## Dự án
POS API trên **.NET 10** (Clean Architecture) phục vụ ~5.000 máy POS.
- Solution: `POS.slnx`

> **Greenfield**: dự án khởi đầu là bản port từ POS.API (.NET Framework 4.6) nhưng **nay
> phát triển mới**, **KHÔNG còn migrate** từ source cũ (`POS.Backend`). Hợp đồng JSON với
> 5.000 máy POS vẫn giữ nguyên cho các endpoint hiện hữu.

## 📚 Bản đồ bộ nhớ & Quy tắc đọc-trước-khi-tạo — BẮT BUỘC

> **Nguồn sự thật duy nhất về cấu trúc dùng chung** (DTOs / Services / Repositories / Helpers)
> là `docs/CURRENT_STRUCTURE.md`. Đọc nó TRƯỚC khi tạo bất kỳ artefact dùng chung nào để **tránh
> trùng lặp**. KHÔNG tạo file registry song song (vd `docs/architecture/*`) — sẽ lệch bản đồ.

### Mục lục tài liệu kiến trúc (đọc theo nhu cầu)

| Khi cần… | Đọc file | Nội dung |
|---|---|---|
| Tra DTO / Service / Repository / Helper đã có + **chữ ký method** + bảng DI | **`docs/CURRENT_STRUCTURE.md`** | Bản đồ bộ nhớ chính — cây `POS.Common/Dtos`, mọi interface + method signature, DI registration, danh sách Helpers |
| Tra nguồn legacy (.NET 4.6) khi migrate 1 chức năng | `docs/PROJECT_INVENTORY.md` + `_migration/INVENTORY.md` | Inventory `VCM.BLUEPOS.*` — chỉ đọc đúng mục của chức năng |
| Kiểm tra contract JSON với 5.000 POS | `docs/API_CONTRACT.md` + `tests/POS.ContractTests/` | Tên field response đã khoá |
| Cách thêm DTO mới | `.claude/commands/add-dto-common.md` (skill `/add-dto-common`) | Quy trình thêm DTO vào `POS.Common` |
| Trạng thái / lịch sử POS.Web | `docs/WEB_STATUS.md`, `docs/CHANGELOG.md` | — |

### Cổng chặn trùng lặp (BẮT BUỘC theo thứ tự)

1. **TRƯỚC khi tạo DTO / Service / Repository / Helper mới** → mở `docs/CURRENT_STRUCTURE.md`,
   tìm ở mục tương ứng (MỤC A cây DTO, MỤC B interface, MỤC D/E chữ ký method, MỤC C DI).
2. **Đã tồn tại** (dù khác tên) → **TÁI DÙNG**, KHÔNG tạo bản trùng. Cần bổ sung → thêm method
   vào interface đã có.
3. **Chưa có** → tạo theo đúng quy ước layer bên dưới, rồi **cập nhật `docs/CURRENT_STRUCTURE.md`
   trong CÙNG commit** (thêm dòng vào cây/bảng tương ứng — chỉ tên class + property/field chính +
   chữ ký + project chứa nó, **KHÔNG chép nguyên code**). Dùng skill `/task-done` để cập nhật doc.
4. **Không chắc** một DTO/Service đã tồn tại chưa → tìm trong `docs/CURRENT_STRUCTURE.md` trước,
   sau đó Grep codebase; **KHÔNG** đoán rồi tạo mới.

> Giữ `docs/CURRENT_STRUCTURE.md` đồng bộ với code là **một phần của định nghĩa "xong"** cho mọi
> task thêm/sửa artefact dùng chung. Doc lệch = lần sau AI tạo trùng.

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
- Namespace: `POS.Application.Features.{Domain}` — interface và implementation **cùng namespace, cùng folder**
- Interface service: `I{Name}Service.cs` trong `Features/{Domain}/`
- Implementation: `{Name}Service.cs` trong `Features/{Domain}/` (cùng folder với interface, không tách `Interfaces/`/`Services/`)
- Service inject repository interface (từ `POS.Infrastructure.Repositories.Interfaces`)
- Service inject `IRedisService` (từ `POS.Infrastructure.Redis`)
- Service inject `IRabbitMQProducer` (từ `POS.Infrastructure.Messaging`)
- Service inject `I{Name}AppService` (từ `POS.Infrastructure.AppServices.{Domain}`) khi cần gọi external HTTP
- **KHÔNG** inject concrete class (chỉ inject interface)
- **Controller BẮT BUỘC inject Application interface** — KHÔNG inject Infrastructure interface trực tiếp

### POS.Infrastructure — quy tắc
- Repositories: `src/POS.Infrastructure/Repositories/{Domain}/` — gom theo domain (MasterData, Sale, Loyalty, Sap…)
  - **Namespace giữ nguyên** `POS.Infrastructure.Repositories` / `POS.Infrastructure.Repositories.Interfaces` (tránh đụng ~20 razor + consumer)
  - Interface repository: `I{Name}Repository.cs` đặt trong cùng folder `{Domain}/` với implementation
- AppServices (HTTP client wrappers): `src/POS.Infrastructure/AppServices/{Domain}/` — gom theo domain (Partner, DataSync…)
  - Namespace: `POS.Infrastructure.AppServices.{Domain}` — interface và implementation **cùng namespace, cùng folder**
  - Đặt tên `I{Name}AppService` để phân biệt với Application interface
- Redis: `src/POS.Infrastructure/Redis/` (IRedisService, RedisService)
- Redis internals: `src/POS.Infrastructure/Cache/` (IRedisManager, RedisManager, RedisOptions)
- Messaging: `src/POS.Infrastructure/Messaging/` (IRabbitMQProducer, RabbitMQProducer)
- DB Factories: `src/POS.Infrastructure/Database/`

---

## Quy tắc AppService — BẮT BUỘC khi tạo external HTTP client

> Mọi service gọi external API (GotIT, Urbox, AkaChain, ...) **BẮT BUỘC** tuân theo pattern 3 lớp sau.

### Pattern bắt buộc

```
Controller (POS.Api)
  → inject I{Name}Service              ← POS.Application.Features.{Domain}
    → Application/Features/{Domain}/{Name}Service    (thin wrapper — chỉ delegate, không có logic)
        → inject I{Name}AppService     ← POS.Infrastructure.AppServices.{Domain}
          → Infrastructure/AppServices/{Domain}/{Name}Service  (HTTP client thực sự)
```

### Ví dụ đã có (tham chiếu khi tạo service mới)

| Partner | Application interface | Infrastructure AppService |
|---|---|---|
| AkaChain/FMV | `IAkaChainLoyaltyService` | `IAkaChainLoyaltyAppService` / `AkaChainLoyaltyAppService` |
| GotIT | `IGotITService` | `IGotITAppService` / `GotITService` |
| Urbox | `IUrboxService` | `IUrboxAppService` / `UrboxService` |

### Checklist khi tạo service HTTP client mới

1. **Infrastructure**: Tạo `I{Name}AppService.cs` trong `AppServices/{Domain}/` — namespace `POS.Infrastructure.AppServices.{Domain}`
2. **Infrastructure**: Tạo `{Name}Service.cs` trong `AppServices/{Domain}/` — implements `I{Name}AppService`, cùng namespace với interface
3. **Infrastructure DI**: Đăng ký `services.AddScoped<I{Name}AppService, {Name}Service>()` trong `src/POS.Infrastructure/DependencyInjection.cs`
4. **Application**: Tạo `I{Name}Service.cs` trong `Features/{Domain}/` — namespace `POS.Application.Features.{Domain}`, **cùng signature** với `I{Name}AppService`
5. **Application**: Tạo `{Name}Service.cs` trong `Features/{Domain}/` — implements `I{Name}Service`, inject `I{Name}AppService`, mỗi method chỉ `=> appService.Method(...)`
6. **Application DI**: Đăng ký `services.AddScoped<I{Name}Service, {Name}Service>()` trong `src/POS.Application/DependencyInjection.cs`
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
- Dùng `[JsonProperty]` — KHÔNG dùng `[JsonPropertyName]` (của System.Text.Json)
- Field kiểu động: dùng `object?` — KHÔNG dùng `JsonElement`

### 2. Lý do kinh doanh — KHÔNG ĐƯỢC THAY ĐỔI TÊN FIELD JSON
> 5.000 máy POS đang parse JSON response theo đúng tên field hiện tại.
> Thay đổi bất kỳ tên field nào sẽ phá vỡ production ngay lập tức.

### 3. C# 12 / .NET 10
- File-scoped namespace: `namespace POS.Common.Dtos.{Domain};`
- Nullable reference types: thêm `?` cho reference types
- Non-null required strings: `= string.Empty`
- Giữ nguyên: computed properties, inheritance chain, `[Required]`, `[StringLength]`

---

## Cấu trúc src/POS.Common/

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

## Thêm DTO mới: dùng lệnh `/add-dto-common`

Xem `.claude/commands/add-dto-common.md` để biết cách dùng.

---

## Quy tắc cấu hình External API — BẮT BUỘC

> **Chi tiết đầy đủ: `.claude/skills/api/SKILLS.md`** — đọc file này trước khi tạo bất kỳ AppService nào gọi external HTTP API.

Mọi thông tin cấu hình (host, credentials, routes, timeout) đều lấy từ DB qua `ICentralMDRepository.GetSysWebApiAsync(appCode)` — đã cache Redis tự động.
**KHÔNG** hardcode URL hoặc credentials, **KHÔNG** đọc từ `appsettings.json`.

---

## Quy tắc Cache — Redis StandAlone (BẮT BUỘC)

> **Chi tiết đầy đủ: `.claude/skills/cache/SKILLS.md`** — đọc file này trước khi thêm bất kỳ chỗ nào dùng cache.

### Nguyên tắc cốt lõi

Cache **BẮT BUỘC** dùng `IRedisService` (Redis StandAlone) — **KHÔNG** dùng in-memory cache
cho dữ liệu chia sẻ. Mọi master data từ DB (SysWebApi, stores, rates…) cần đọc nhiều lần
phải có Redis cache tương ứng.

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

### Checklist khi cần cache một loại data

1. Đặt tên Redis key theo convention trong `.claude/skills/cache/SKILLS.md`
2. Thêm method vào `ICentralMDRepository` / `ILoyaltyRepository` nếu chưa có
3. Implement theo pattern Hash hoặc String với TTL
4. AppService/Service gọi qua Repository — KHÔNG gọi Redis trực tiếp (trừ OAuth token)

---

## Quy tắc Background Worker — POS.Worker (BẮT BUỘC)

> **Chi tiết đầy đủ: `.claude/skills/worker/SKILLS.md`** — đọc file này trước khi thêm bất kỳ
> scheduled job, message consumer, hay tác vụ chạy nền nào vào `POS.Worker`.

### Nguyên tắc cốt lõi

- `POS.Worker` chỉ là **host mỏng** (`Program.cs` đăng ký hosted service).
- **Implementation worker đặt trong `src/POS.Infrastructure/Workers/`** — namespace `POS.Infrastructure.Workers`.
- Worker là **singleton** → resolve repository scoped qua `IServiceScopeFactory.CreateAsyncScope()`, KHÔNG inject thẳng.
- Vòng lặp `ExecuteAsync` KHÔNG được chết: try/catch nuốt exception, set `healthState.Status = "Degraded"`, log, lặp tiếp.
- Hai khuôn mẫu: **timer polling** (`PeriodicTimer`) và **message consumer** (RabbitMQ push, `prefetchCount: 1`, `autoAck: false`).
- Serialize bằng **Newtonsoft.Json**; cập nhật `WorkerHealthState`; heartbeat → Redis key `Worker:Heartbeat:{Name}`.
- Đăng ký mỗi worker mới: `builder.Services.AddHostedService<{Name}Worker>();` trong `Program.cs`.

---

## Quy tắc Controller — BẮT BUỘC

### A. DI Registration — BẮT BUỘC sau mỗi interface mới

Mỗi khi tạo `I{Name}Service` mới trong `POS.Application/Features/{Domain}/`:
1. Tạo stub hoặc implementation trong `POS.Application/Features/{Domain}/` (hoặc `POS.Infrastructure/` nếu cần HTTP client / DB)
2. **Đăng ký ngay** trong `src/POS.Application/DependencyInjection.cs`:
   ```csharp
   services.AddScoped<I{Name}Service, {Name}Service>();
   ```
3. Nếu chưa implement thật, dùng stub trả `HttpStatusCode.NotImplemented` — KHÔNG throw exception.

> **Lý do**: Quên đăng ký DI → `InvalidOperationException` lúc runtime, không phải lúc build.

### B. ModelState Validation — `ValidateModelFilter` đã xử lý global

`Program.cs` đã cấu hình `SuppressModelStateInvalidFilter = true` để `ValidateModelFilter` kiểm soát hoàn toàn format response (trả `ResultResponse`, không phải ASP.NET problem-details).

**Hệ quả quan trọng**:
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

// Khi HTTP status luôn 200
return Ok(result);

// Khi cần tùy chỉnh field (vd đặt giá trị riêng vào MessageTechnical)
return StatusCode((int)status, new ResultResponse { Data = ..., Message = ..., Status = ..., MessageTechnical = ... });
```

`OkResult(data)` chỉ dùng khi `data` là object thuần (không phải `ResultResponse`).

### E. Helpers chưa có trong POS.Common — inline tạm

Một số helper tiện ích chưa tồn tại trong `src/POS.Common/Helpers/`. Khi cần, inline trực
tiếp và đánh dấu `// TODO: extract to helper`:

| Nhu cầu | Logic inline | Ghi chú |
|---|---|---|
| Kiểm tra số điện thoại | `phone.Length >= 9 && phone.Length <= 11 && phone.All(char.IsDigit)` | TODO: extract to helper |
| Message số thẻ không hợp lệ | `$"Số thẻ {phone} không hợp lệ"` | |
| Format SĐT Việt Nam | (chưa có) | Tạo helper nếu dùng nhiều |
| Ghi exception log | `_fileLogHelper.WriteExpLogs(...)` | Đã có `IFileLogHelper` — dùng luôn |

### F. Swagger — chỉ bật ở Development

`Program.cs` đã cấu hình `AddSwaggerGen()` / `UseSwagger()` **chỉ khi `IsDevelopment()`**;
UAT/PROD không bật (tránh lộ API docs). Ở DEV truy cập UI tại `/swagger` (có nút Authorize
cho Basic Auth). Ngoài DEV, test route bằng curl trực tiếp.

---

## Guardrails & Testing — BẮT BUỘC biết

> Dự án có 3 "vành đai bảo vệ" trong `tests/POS.ContractTests/`. **Chạy `dotnet test` trước
> khi commit.** Lệnh: `dotnet test tests/POS.ContractTests`.

### 1. Contract test — khoá tên field JSON (cực quan trọng)

- File: `tests/POS.ContractTests/JsonFieldContractTests.cs` (+ helper `JsonContract.cs`).
- Mục đích: khoá **tên field JSON** của các DTO response mà **5.000 máy POS** đang parse.
  Đổi tên / thêm / xoá field bất kỳ của DTO đã khoá → **test đỏ ngay**.
- **Khi CỐ Ý đổi contract**: cập nhật danh sách field kỳ vọng trong file test **cùng commit** —
  đó là dấu vết cho thấy thay đổi là có chủ đích, không phải tai nạn.
- **Khi tạo DTO response mới**: thêm một `[Fact]` khoá field cho nó (dùng `AssertFields`).

### 2. DI validation test — chặn "quên đăng ký DI"

- File: `tests/POS.ContractTests/DependencyInjectionTests.cs`.
- Dựng lại đúng cách compose DI của `Program.cs`, kiểm tra **mọi phụ thuộc `POS.*`** trong
  constructor của tất cả controller + mọi implementation đã đăng ký đều có trong container.
- Chỉ đọc service descriptor (không build provider) → **không cần Redis/SQL/Rabbit**.
- Quên `services.AddScoped<...>()` → test đỏ lúc build/test thay vì `InvalidOperationException`
  lúc gọi API (xem mục "A. DI Registration").

### 3. Exception middleware — lưới an toàn global (G3)

- Impl: `src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs`; đăng ký **đầu pipeline**
  trong `Program.cs` (`app.UsePosExceptionHandling()`).
- Bắt mọi exception **chưa xử lý**, trả đúng `ResultResponse` (status 500, PascalCase qua
  `DefaultContractResolver`, `NullValueHandling.Ignore` → bỏ field `Data`) — khớp contract POS.
- Controller chỉ giữ try/catch khi cần **message nghiệp vụ riêng**; KHÔNG cần try/catch chỉ để
  format lỗi chung. **KHÔNG gỡ** middleware này.
- Hành vi được khoá bằng `tests/POS.ContractTests/ExceptionMiddlewareTests.cs`.

---

## Quy ước phát triển mới (Greenfield) — BẮT BUỘC

> Dự án **không còn migrate** từ `POS.Backend` (.NET 4.6). Từ nay mọi nghiệp vụ là **code mới**.
> Hợp đồng JSON với 5.000 máy POS **vẫn giữ nguyên** cho các endpoint hiện hữu.

### Tổ chức theo Feature (áp cho code mới)

- Code Application mới đặt theo domain: `POS.Application/Features/{Domain}/`
  (`I{Name}Service.cs` + `{Name}Service.cs`). Không để phẳng chung khi tạo domain mới.
- Repository/AppService mới trong Infrastructure gom theo domain tương ứng.
- **Business logic** đặt ở `POS.Application` (Service); **I/O** (DB/HTTP/cache) ở
  `POS.Infrastructure`. External HTTP theo **AppService 3 lớp** (xem mục cùng tên ở trên).

### Khuôn thêm 1 nghiệp vụ mới

```
DTO (POS.Common/Dtos/{Domain}/)
  → Repository/AppService (POS.Infrastructure/.../{Domain}/)
    → Service (POS.Application/Features/{Domain}/)
      → đăng ký DI (DependencyInjection.cs)
        → Controller (POS.Api/Controllers/)
          → contract test cho DTO response + đảm bảo DI test vẫn xanh
```

Mỗi bước có đúng một nơi để đặt file. Sau khi xong: `dotnet test` phải xanh.

---

## Sinh file master data .zip cho POS (Sync Master Data)

> Tính năng cho máy POS đầu ngày tải master data đã nén. Endpoint giữ **contract cũ** (5.000 POS không đổi).

### Luồng

```
GET api/posblue/GetFileFromFTP?...&typeSync=ALL
  → SyncDataPosController (nhánh typeSync=="ALL")
    → IMasterDataSyncService.EnsureMasterDataFileAsync   (POS.Application/Features/DataSync)
        → ISyncRepository.GetSyncTablesAsync             (SP1 [SyncTable_Get] @IsChange='A')
        → ISyncRepository.StreamTableToFileAsync         (SP2 [SyncGetDataByTable], STREAM SqlDataReader)
        → IFileArchiveService.CreateZipFromDirectory     (nén thư mục tạm)
        → ISyncFileLock                                  (keyed SemaphoreSlim chống sinh trùng)
    → GetFileFromServerApiAsync → trả List<PathFileAPIModel>   (GIỮ NGUYÊN contract)
GET api/posblue/DowloadFileStream?filePath=...  → stream thủ công application/x-zip-compressed (FileShare.Read)
                                                  + ghi log DB dbo.MasterDataDownloadLog (Success/Aborted/Error)
```

> **Download logging**: `DowloadFileStream` stream thủ công (`CopyToAsync(Response.Body, RequestAborted)`) để biết
> kết quả best-effort: `Success` = gửi đủ byte không bị ngắt (KHÔNG đảm bảo POS lưu xong), `Aborted` = client ngắt,
> `Error`. Ghi 1 dòng `dbo.MasterDataDownloadLog` qua `IMasterDataSyncService.LogDownloadAsync` (fail-safe, nuốt lỗi
> nếu bảng chưa tạo). **KHÔNG tự xóa file** sau download (giữ cache ngày; dọn bằng daily-refresh + KeepZipDays).
> Script bảng: `docs/sql/MasterDataDownloadLog.sql`. Log với `ct=CancellationToken.None` để ghi được cả khi client ngắt.

### Quyết định kiến trúc (giữ chuẩn cho session sau)

- **Response GIỮ NGUYÊN `List<PathFileAPIModel>`** — KHÔNG đổi sang shape mới. Service chỉ sinh file, controller
  re-list qua `GetFileFromServerApiAsync` để build response như cũ. `GetMasterDataFileResult` chỉ dùng nội bộ/log.
- **Định dạng file trong zip = JSON envelope `SyncTableList`** (bám `DataRawService.CreateFileSODFakeAsync`):
  `{ FileName, TableName, Action, ProcedureName, ProcessID, Data:[rows] }`, UTF-8 (`Encoding.UTF8`).
  Stream mảng `Data` từ `SqlDataReader` (`SequentialAccess`) bằng Newtonsoft
  `JsonTextWriter` — **KHÔNG** nạp DataTable/RAM. `// TODO: confirm format vs POS parser`.
- **Chia batch file `.txt`** (`MasterDataSync:BatchSizePerFile`, mặc định 10000): bảng lớn tách nhiều file
  `{site}_{table}_{rnd}_{idx}_{batchNo:D3}.txt` (random tạo 1 lần/bảng để cùng prefix + sort đúng). **Batch đầu
  `Action="TRUNC-INSERT"`, các batch sau `Action="INSERT"`** (append) → POS truncate 1 lần rồi nối, tránh mất dữ liệu.
  Vẫn stream từng dòng (constant memory). `BatchSizePerFile <= 0` → không tách (1 file/bảng).
- **Tên zip**: `{siteCode}_{typeSync}_{posTerminal}_{yyyyMMdd}.zip` → sang ngày mới tự sinh lại (daily-refresh).
- **Atomic publish**: ghi `{guid}.zip` tạm → `File.Move(..., overwrite:true)` sang tên chính thức. POS không bao giờ
  tải file ghi dở. Lỗi giữa chừng → cleanup `_tmp`/zip tạm, **KHÔNG** publish, log + throw.
- **Mức nén**: `MasterDataSync:ZipCompressionLevel` (mặc định `Fastest`). KHÔNG dùng `Optimal` — master data JSON
  lớn, Optimal tốn CPU/chậm; Fastest nhanh 2–5× (file lớn hơn ~10–30%, POS giải nén Deflate chuẩn bình thường).
- **Song song hóa SP2**: `MasterDataSync:MaxParallelTables` (mặc định 4). Mỗi bảng dùng `SqlConnection` riêng
  → thread-safe. `≤ 0` = sequential an toàn. Tăng nếu SQL Server còn headroom; mục tiêu 15–25s cho 85 bảng.
- **SHA-256 companion file**: sau khi publish zip, API tự tạo `{zipName}.sha256` cùng thư mục. Ops verify
  bằng `sha256sum`; POS có thể download để self-verify (tùy chọn). Cleanup tự xóa `.sha256` cùng zip.
- **Redis cache SP1** (`MD:SyncTableList`, TTL 3600s): metadata 85 bảng cache Redis — tránh SP1 mỗi request.
  Invalidate thủ công: `DEL MD:SyncTableList` khi DBA thay đổi cấu hình `SyncTableList`.
- **Khóa**: keyed `SemaphoreSlim` Singleton, key = `{typeSync}_{siteCode}_{posTerminal}` (KHÔNG kèm ngày →
  bounded theo terminal) + double-check `File.Exists` sau khóa.
- **Daily-refresh / dọn file cũ**: `GetFileFromServerApiAsync` liệt kê **mọi** .zip trong folder → sau khi publish,
  xóa zip cùng prefix có tên ≠ ngày hôm nay (tránh POS nhận file cũ). Khi đọc file đã tồn tại: kiểm tra
  `LastWriteTime.Date == hôm nay`, nếu cũ → xóa và sinh lại.
- **SP1**: `@IsChange='A'` → bỏ qua `@IsByStore`/`@GroupName` (default SP). `@POSLastCounter=0` khi
  `typeSync==ALL` hoặc `IsFirstDataAll=1`.
- **Filter per-store** (bảng `IsByStore=1`): SP2 `[SyncGetDataByTable]` đã được mở rộng 2 tham số
  `@FilterColumn`/`@FilterValue` (default rỗng, backward-compatible) → `WHERE ([Counter]>N OR 0=N) AND [Col]=@val`
  (parameterized, bracket-quote). Service truyền `@FilterColumn = ColumnFilter`, `@FilterValue = siteCode` khi
  `IsByStore=1` và `ColumnFilter` khác rỗng → file chỉ chứa dòng của store đó. `IsByStore=0` hoặc thiếu ColumnFilter
  → không filter (lấy all). Script SP: `docs/sql/SyncGetDataByTable_AddFilter.sql` (phải apply trên CentralMD).
  **BẮT BUỘC bọc ngoặc** điều kiện Counter trong SP, nếu không `AND` bind chặt hơn `OR` → lọt mọi dòng.

### Vị trí file

| Layer | File | Namespace |
|---|---|---|
| Contracts | `POS.Common/Dtos/DataSync/{SyncTableInfo,GetMasterDataFileRequest,GetMasterDataFileResult}.cs` | `POS.Common.Dtos.DataSync` |
| Infra repo | `POS.Infrastructure/Repositories/DataSync/{I}SyncRepository.cs` | `...Repositories(.Interfaces)` |
| Infra files | `POS.Infrastructure/Files/{IFileArchiveService,FileArchiveService,ISyncFileLock,SyncFileLock,MasterDataSyncOptions}.cs` | `POS.Infrastructure.Files` |
| App service | `POS.Application/Features/DataSync/{I}MasterDataSyncService.cs` | `POS.Application.Features.DataSync` |
| Config | `appsettings.json` → section `"MasterDataSync"` (`SqlCommandTimeoutSeconds`, `KeepZipDays`, `DateInZipName`, `ZipCompressionLevel`) | — |

> Thư mục đích dùng `AppSettings:FtpRootPath` qua `MapFtpPath` — KHÔNG thêm `RootPath` riêng.

---

## POS.Web — Blazor Server Dashboard

> Webapp quản trị nội bộ: `src/POS.Web/` — .NET 10, Blazor Server, MudBlazor 9.5.0

### 1. Stack & Packages

| Package | Version | Ghi chú |
|---------|---------|---------|
| .NET | 10.0 | `net10.0` target framework |
| MudBlazor | 9.5.0 | UI component library — **v9 có breaking changes** |
| BCrypt.Net-Next | 4.2.0 | Hash mật khẩu DashboardUsers |
| Newtonsoft.Json | 13.0.4 | Serialization — giống toàn solution |

### 2. Kiến trúc Auth

```
DB: RPOSMasterData.dbo.DashboardUsers
  ↓
IWebUserService.ValidateLoginAsync(username, password)
  → BCrypt.Verify(password, hash)
  → trả DashboardUser (Id, Username, Role, StoreCodes, FullName)
  ↓
Login.razor (InteractiveServer — KHÔNG gọi SignInAsync trực tiếp)
  → tạo one-time token → IMemoryCache (TTL 30s)
  → Nav.NavigateTo("/account/signin/{token}", forceLoad: true)
  ↓
GET /account/signin/{token} (minimal API endpoint — HTTP pipeline thật)
  → ctx.SignInAsync(CookieAuth, principal, IsPersistent=true)
  → Redirect "/"
  ↓
Cookie session: 8h, SlidingExpiration, HttpOnly, SameSite=Strict
```

> **Lý do bridge token**: Blazor InteractiveServer chạy trên WebSocket circuit — `HttpContext` đã degraded, gọi `SignInAsync` lúc này throw → circuit crash. Phải thoát ra HTTP pipeline thật để set cookie.

### 3. Roles và Access Rules

| Role | Constant | Policy | Xem được |
|------|----------|--------|---------|
| Vận hành cửa hàng | `WebRoles.StoreOperator` | `WebPolicies.StoreAndAbove` | Store/* (filter theo `store_codes` claim) |
| IT Ops | `WebRoles.ITOps` | `WebPolicies.OpsAndAbove` | Store/* + Ops/* (xem tất cả store) |
| System Admin | `WebRoles.SystemAdmin` | `WebPolicies.AdminOnly` | Tất cả |

```csharp
// src/POS.Web/Auth/WebRoles.cs
WebRoles.StoreOperator = "StoreOperator"
WebRoles.ITOps         = "ITOps"
WebRoles.SystemAdmin   = "SystemAdmin"

WebPolicies.StoreAndAbove = "StoreAndAbove"  // cả 3 role
WebPolicies.OpsAndAbove   = "OpsAndAbove"    // ITOps + SystemAdmin
WebPolicies.AdminOnly     = "AdminOnly"      // SystemAdmin only
```

### 4. Services inject được trong POS.Web

POS.Web đăng ký `AddInfrastructure()` + `AddApplication()` → inject trực tiếp qua DI:

**Từ POS.Infrastructure:**
- `IRedisService` — cache (HashGet/Set, StringGet/Set, KeyExists...)
- `IKibanaService` — structured logging → Elasticsearch
- `IFileLogHelper` — file log fallback
- `IRabbitMQProducer` — message queue
- `IKafkaProducer` — Kafka producer
- `ICentralMDRepository` — master data (store config, POS setup...)
- `ICentralSaleRepository` — sales data (orders, transactions...)
- `ILoyaltyRepository` — loyalty (members, points, wincode...)
- `IOfferStaffRepository` — staff discount
- `IWincodeRepository` — wincode/winlife
- `CentralMDConnectionFactory` — inject concrete (không qua interface)
- `LoyaltyConnectionFactory` — inject concrete (không qua interface)

**Từ POS.Application:**
- `ICommonService` — POS common ops (store setup, shift, EOD...)
- `IHealthCheckService` — kiểm tra sức khỏe hạ tầng
- `IAkaChainLoyaltyService` — FMV/AkaChain loyalty
- `IGotITService` — GotIT voucher partner
- `IUrboxService` — Urbox voucher partner
- `IKafkaService` — Kafka publisher
- `IDataRawService` — file sale processing
- `ISyncDataPosService` — POS sync

**Chỉ trong POS.Web:**
- `IWebUserService` — dashboard user auth (login, get user, get store codes)

### 5. Template Page Component chuẩn

```razor
@page "/store/ten-trang"
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth

@inject ICentralSaleRepository SaleRepo
@inject IKibanaService KibanaService
@inject ISnackbar Snackbar

<PageTitle>Tên trang – POS Dashboard</PageTitle>

@if (_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-4"/>
}
else if (_errorMsg != null)
{
    <MudAlert Severity="Severity.Error">@_errorMsg</MudAlert>
}
else
{
    @* nội dung thật *@
}

@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    private bool _loading = true;
    private string? _errorMsg;
    private IReadOnlyList<string> _userStoreCodes = [];

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        var json = state.User.FindFirst("store_codes")?.Value;
        _userStoreCodes = string.IsNullOrEmpty(json)
            ? []
            : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json) ?? [];
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMsg = "Không thể tải dữ liệu.";
            KibanaService.LogException("PageName.OnInitialized", "", 0, "", ex.Message);
        }
        finally { _loading = false; }
    }

    private async Task LoadDataAsync() { /* ... */ }
}
```

> `_userStoreCodes` rỗng = ITOps/Admin (xem tất cả). Khác rỗng = StoreOperator (filter theo list).

### 6. MudBlazor v9 — Breaking Changes BẮT BUỘC biết

#### Charts (thay đổi lớn nhất)

```razor
@* ĐÚNG — v9 *@
@using MudBlazor.Charts

<Line T="double"
      ChartSeries="@_series"
      ChartLabels="@_labels"
      Width="100%" Height="280px"
      ChartOptions="@_lineOpts"/>

<Bar T="double"
     ChartSeries="@_series"
     ChartLabels="@_labels"
     Width="100%" Height="280px"
     ChartOptions="@_barOpts"/>

@* SAI — v8 syntax, KHÔNG dùng *@
@* <MudChart ChartType="ChartType.Line" ChartSeries<double>="..." .../>  *@
```

```csharp
// @code — v9
// ChartSeries<T>.Data là ChartData<T>, KHÔNG phải double[]
private List<ChartSeries<double>> _series =
[
    new ChartSeries<double>
    {
        Name = "Label",
        Data = new ChartData<double>(Array.Empty<double>())  // phải dùng constructor
    }
];

// Options: dùng concrete class ở MudBlazor namespace
private readonly LineChartOptions _lineOpts = new() { LineStrokeWidth = 2, ShowLegend = false };
private readonly BarChartOptions  _barOpts  = new() { ShowLegend = false };

// Kiểm tra empty: dùng bool flag (KHÔNG dùng .Data.Length)
private bool _isEmpty;
// Trong LoadData: _isEmpty = data.Count == 0;
```

| Thứ | v8 (sai) | v9 (đúng) |
|-----|----------|-----------|
| Chart component | `<MudChart ChartType="ChartType.Line">` | `<Line T="double">` hoặc `<Bar T="double">` |
| Series attribute | `ChartSeries<double>="@..."` | `ChartSeries="@..."` (với `T="double"` trên component) |
| X-axis labels | `XAxisLabels` | `ChartLabels` |
| Data type | `double[]` | `ChartData<double>(double[])` |
| Options (line) | `ChartOptions { LineStrokeWidth, YAxisTicks }` | `LineChartOptions { LineStrokeWidth, ShowLegend }` |
| Options (bar) | `ChartOptions { YAxisTicks }` | `BarChartOptions { ShowLegend }` |
| Empty check | `series[0].Data.Length == 0` | bool flag set trong LoadData |

#### Chip component

```razor
@* ĐÚNG *@
<MudChip T="string" Color="..." ...>@label</MudChip>

@* SAI (v8) *@
@* <MudChip Color="..." ...>@label</MudChip> *@
```

### 7. Logging convention trong POS.Web

```csharp
// Load data
KibanaService.LogInfo("PageName.LoadData", _userStoreCodes.FirstOrDefault() ?? "all",
    $"Loading data: {count} items");

// Exception
KibanaService.LogException("PageName.MethodName", "", 0, "", ex.Message);
```

### 8. Quy tắc đặt tên

| Thành phần | Convention | Ví dụ |
|-----------|-----------|-------|
| Page component | `{Domain}Page.razor` | `RevenuePage.razor` |
| Folder | `Components/Pages/{Section}/` | `Components/Pages/Store/` |
| Route | `/section/kebab-case` | `/store/daily-revenue` |

### 9. Serialization trong POS.Web

Dùng **Newtonsoft.Json** (`JsonConvert.*`) — KHÔNG dùng `System.Text.Json`.
Nhất quán với POS.Api và POS terminals.

### 10. Responsive UI Standard — BẮT BUỘC với mọi page mới

> Mọi page/component mới trong POS.Web PHẢI tuân theo chuẩn này.
> Tự áp dụng khi tạo page — không cần nhắc.

#### Breakpoints (MudBlazor built-in)

| Tên | Phạm vi | Target |
|-----|---------|--------|
| **xs** | < 600px | Mobile dọc (iPhone, Android) |
| **sm** | 600–959px | Mobile ngang / Tablet nhỏ |
| **md** | 960px+ | Desktop chuẩn |

#### A. Page Header — Title + Action Button

**KHÔNG** dùng `MudStack Row="true" Justify.SpaceBetween` → tiêu đề bị squeeze, văn bản xuống 2 dòng trên mobile.

**DÙNG** `div.pos-page-header` (CSS đã có trong `app.css`):

```razor
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add"
               Class="pos-page-header-btn">
        Thêm
    </MudButton>
</div>
```

- **Desktop (sm+):** title bên trái, button bên phải — cùng hàng
- **Mobile (xs):** title full-width hàng trên, button full-width hàng dưới

Page chỉ có title (không có button) → dùng `MudText Typo.h5` trực tiếp, không cần `pos-page-header`.

#### B. DataTable — dùng `MudTable` với `HorizontalScrollbar="true"`

```razor
@* BẮT BUỘC: DataTable dùng MudTable (không tự viết <table class="pos-table">) *@
<MudTable Items="@_items" Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" Loading="@_loading"
          HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<MyDto, object>(x => x.FieldA)">Cột A</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Cột A">@context.FieldA</MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                       InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                       RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

> Chi tiết đầy đủ (client-side / server-side / dynamic columns / footer tổng): `.claude/skills/web/SKILLS.md` §DataTable chuẩn.
> Không có `HorizontalScrollbar="true"` → table bị clip trên mobile.
> **Ngoại lệ:** pivot report (cột-ngày động) vẫn dùng `<table class="pos-table rpt-pivot-table">` trong wrapper `overflow-x:auto`.

**Pagination — chuẩn BẮT BUỘC:** `MudTablePager` luôn dùng `PageSizeOptions="new[] { 10, 20, 50, 100 }"`. **Phải bắt đầu bằng `10`** vì `MudTable.RowsPerPage` mặc định = `10`; nếu list không chứa `10`, ô chọn "Số dòng mỗi trang" hiển thị trống / chọn không có tác dụng. KHÔNG hard-set `RowsPerPage="..."` một chiều trên `MudTable` (re-render sẽ reset lựa chọn) — để mặc định `10` đã khớp option đầu.

#### C. Filter Panel

Chuẩn đúng — giữ nguyên MudGrid + MudItem. Luôn đảm bảo:

```razor
@* Nhóm nút cuối filter *@
<MudItem xs="12" sm="12" md="2" Class="d-flex align-center">
    <MudStack Row="true" Spacing="1" Class="w-100">
        <MudButton ... FullWidth="true">Tìm</MudButton>
        <MudButton ... FullWidth="true">Xóa</MudButton>
    </MudStack>
</MudItem>
```

#### D. Button Rules

| Tình huống | Rule |
|-----------|------|
| CTA trong page header | Class `pos-page-header-btn` → tự full-width trên xs |
| Nhóm nút Tìm/Xóa trong filter | `MudStack Row Spacing="1" Class="w-100"` + `FullWidth="true"` mỗi nút |
| Icon button trong table row | Không thay đổi — `MudIconButton Size.Small` đủ vùng chạm |
| Button standalone ngoài form | Bọc trong `MudItem xs="12" sm="auto"` hoặc `Class="w-100 w-sm-auto"` |

#### E. Chip / Badge Row

Mọi container chip phải có `flex-wrap`:

```razor
@* ĐÚNG *@
<div class="d-flex align-center gap-2 flex-wrap mb-4">
    <MudChip T="string" .../>
</div>

@* SAI — chips tràn ngang trên mobile *@
<div class="d-flex align-center gap-2 mb-4">
    <MudChip T="string" .../>
</div>
```

#### F. Sidebar Drawer — Init theo viewport thực

Dùng `IBrowserViewportService` (MudBlazor 9 built-in) để init đúng:

```razor
@inject IBrowserViewportService ViewportService
@implements IAsyncDisposable
```

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var bp = await ViewportService.GetCurrentBreakpointAsync();
        _drawerOpen = bp >= Breakpoint.Md;   // mở sẵn trên desktop, đóng trên mobile
        StateHasChanged();
    }
}

public async ValueTask DisposeAsync()
{
    Nav.LocationChanged -= OnLocationChanged;
}
```

#### G. Checklist — kiểm tra trước khi hoàn thành page mới

```
□ Page header có button  → dùng div.pos-page-header (KHÔNG MudStack Row)
□ DataTable → MudTable có HorizontalScrollbar="true" (pivot table thì wrapper overflow-x:auto)
□ Filter panel button group → xs="12" sm="12" md="2" + FullWidth="true"
□ Chip container → có class "flex-wrap"
□ Không hardcode width (px) cho layout — dùng %, MudGrid, flex: 1
□ Summary/info text nhiều phần → d-flex flex-wrap gap-2 (KHÔNG &nbsp;|&nbsp;)
□ Sidebar drawer (MainLayout) → dùng IBrowserViewportService để init
```

---

### 11. KHÔNG làm những điều sau (POS.Web)

- ❌ Gọi `SignInAsync` trong Blazor InteractiveServer component — dùng bridge token (xem mục 2)
- ❌ Dùng `System.Text.Json` — phải dùng `Newtonsoft.Json`
- ❌ Quên `@rendermode InteractiveServer` trên page có tương tác
- ❌ Quên `@attribute [Authorize(...)]` trên page mới
- ❌ Inject `IDbConnectionFactory` — factory đăng ký là concrete, inject `CentralMDConnectionFactory`
- ❌ Raw SQL trong page/component — phải qua Repository hoặc Service
- ❌ Gọi HTTP đến POS.Api từ POS.Web — inject service trực tiếp qua DI
- ❌ Bỏ qua row-level filter với StoreOperator
- ❌ Dùng `ChartSeries<double>` như attribute HTML trong Razor (v9 syntax sai)
- ❌ Dùng `MudChart ChartType="..."` và `ChartOptions { YAxisTicks, LineStrokeWidth }` — đã đổi trong v9
- ❌ Dùng `MudStack Row="true" Justify.SpaceBetween` cho header title+button — dùng `div.pos-page-header`
- ❌ Tự viết `<table class="pos-table">` cho DataTable mới — dùng `MudTable` (xem SKILLS.md §DataTable chuẩn)
- ❌ MudTable thiếu `HorizontalScrollbar="true"` — table bị clip mobile
- ❌ Chip container không có `flex-wrap` — chips tràn ngang trên mobile
- ❌ `MudTablePager` có `PageSizeOptions` không chứa `10` — ô chọn số dòng/trang hỏng (vì default `RowsPerPage=10`); luôn dùng `{ 10, 20, 50, 100 }`

### 13. MudAutocomplete — BẮT BUỘC tránh circuit crash

> Rút ra từ sự cố thực tế: click ô store picker làm **chết Blazor circuit** ("Failed to rejoin / Failed to resume").

- ❌ **KHÔNG** dùng `ResetValueOnEmptyText="true"` cùng `MinCharacters="0"` — text rỗng khi focus → reset value lặp vô hạn → re-render loop → circuit bị tear-down. Dùng `Clearable="true"` cho nút xóa là đủ.
- ✅ **LUÔN `.Take(N)`** (vd 50) trong `SearchFunc` để bound kết quả. `MaxItems` chỉ giới hạn **hiển thị**, KHÔNG giới hạn dữ liệu component xử lý — list nghìn store vẫn được materialize đầy đủ nếu không `.Take()`.
- ✅ Đặt `MaxItems` hợp lý (vd 50) khớp với `.Take()`.
- 📌 Pattern chuẩn `SearchFunc`:
  ```csharp
  private Task<IEnumerable<StoreDto>> SearchStoreAsync(string value, CancellationToken ct)
  {
      IEnumerable<StoreDto> matches = string.IsNullOrWhiteSpace(value)
          ? _allStores
          : _allStores.Where(s =>
              (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
              (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
      return Task.FromResult(matches.Take(50));
  }
  ```
- 📌 `Program.cs` đã bật `DetailedErrors` (Dev) + nới `HubOptions.MaximumReceiveMessageSize=512KB`. Khi circuit crash, **đọc server log** để lấy exception thật (client chỉ thấy "Failed to rejoin").

### 12. Slash Commands (POS.Web)

| Command | Mục đích |
|---------|---------|
| `/web-add-store-page` | Tạo page mới trong Store section |
| `/web-add-ops-page` | Tạo page mới trong Ops section |
| `/web-add-admin-page` | Tạo page mới trong Admin section |
| `/web-add-feature` | Tạo feature đầy đủ (page + service + model) |
| `/web-check-status` | Build + audit trạng thái POS.Web |
| `/web-gen-hash` | Tạo BCrypt hash cho SQL khởi tạo user dashboard |
| `/add-dto-common` | Thêm DTO mới vào POS.Common (xem `.claude/commands/add-dto-common.md`) |

---

### 13. UI Polish — Trang migrate từ Legacy (BẮT BUỘC đọc khi "làm đẹp UI")

> **Chi tiết đầy đủ: `.claude/skills/web/ui-migrate-legacy.md`** — đọc trước khi sửa markup
> bất kỳ trang nào nhận yêu cầu "sync UI", "trông giống legacy", "làm đẹp".

**Nguyên tắc cốt lõi:**

- GIỮ NGUYÊN 100% `@code { }` — không thêm method/biến/helper. Chỉ sửa markup Razor.
- Màu chip dùng **ternary inline** tại `Color=` — không thêm helper vào `@code`.
- `div.pos-page-header` **là chuẩn dự án** — KHÔNG đổi sang `MudStack Justify.SpaceBetween`.

**4 pattern bắt buộc áp cho mọi trang migrate:**

| Pattern | Áp dụng khi |
|---------|------------|
| **Cột Trạng thái → MudChip màu** | Cột status là text thường |
| **NoRecordsContent → icon Inbox + text** | Bảng rỗng chỉ có MudText |
| **Tab "Thông tin chung" → nhóm field** | Tab editor là 1 MudGrid phẳng |
| **Action bar Lưu/Duyệt → MudPaper justify-end** | Nút nằm trong `div.d-flex` rời |

**Verification bắt buộc sau mỗi task:**
```powershell
dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly   # phải 0 error
dotnet test tests/POS.ContractTests -nologo                         # phải xanh
```

---

### 14. MudBlazor Flat UI Standard — BẮT BUỘC với mọi component mới

> Áp dụng từ phiên thiết kế 2026-06-26. Theme flat đã được cấu hình sẵn trong `PosTheme.cs`.
> Tự áp dụng — không cần nhắc.

#### Quy ước Input

- **Variant:** luôn dùng `Variant="Variant.Outlined"`
- **Margin:** luôn dùng `Margin="Margin.Dense"` (trừ khi layout cần Normal)
- **KHÔNG** dùng `Variant.Filled` cho input (chỉ dùng Filled cho button CTA)

```razor
@* ✅ Chuẩn flat — form input *@
<MudTextField @bind-Value="_filter.StoreNo"
              Label="Mã cửa hàng"
              Variant="Variant.Outlined"
              Margin="Margin.Dense"/>

<MudSelect @bind-Value="_filter.Status"
           Label="Trạng thái"
           Variant="Variant.Outlined"
           Margin="Margin.Dense">
    <MudSelectItem Value="0">Tất cả</MudSelectItem>
</MudSelect>
```

#### Quy ước Card / Paper / Panel

- **Card/Paper chứa nội dung:** `Elevation="1"` hoặc `Elevation="2"` — tạo hairline border 1px
- **Filter panel:** `Elevation="1"`
- **Section phân tách nhẹ (không cần border):** `Elevation="0"`
- **KHÔNG** dùng `Elevation="3"` trở lên cho card/paper thông thường

```razor
@* ✅ Chuẩn flat — card *@
<MudPaper Elevation="1" Class="pa-4 mb-4">
    @* nội dung *@
</MudPaper>

@* ✅ Filter panel *@
<MudPaper Elevation="1" Class="pa-3 mb-4">
    <MudGrid Spacing="2">
        @* filter fields *@
    </MudGrid>
</MudPaper>
```

#### Quy tắc Elevation — QUAN TRỌNG

| Loại component | Elevation dùng | Lý do |
|---------------|---------------|-------|
| Card / Paper / Panel | `0`–`2` | Flat hairline (E1–E5 = `0 0 0 1px`) |
| Button | `DisableElevation` hoặc mặc định | app.css đã bỏ shadow button global |
| **MudPopover / dropdown** | **KHÔNG hạ** — giữ E8 mặc định | MudSelect, MudAutocomplete, MudDatePicker, MudMenu cần nổi |
| **MudDialog** | **KHÔNG hạ** — giữ E12 mặc định | Dialog phải nổi trên overlay |

> **Quy tắc cốt lõi:** Chỉ flat `Elevation 0–5` (card/panel). `Elevation 6+` giữ shadow gốc cho overlay/dropdown/dialog — làm phẳng sẽ khiến dropdown dính bẹt vào nền.

#### Border-radius chuẩn dự án

- Theme: `DefaultBorderRadius = "4px"` (cấu hình trong `PosTheme.cs`)
- CSS token: `--pos-radius-sm: 4px` | `--pos-radius-md: 8px` | `--pos-radius-lg: 12px`
- Dùng `--pos-radius-sm` cho custom HTML element nhỏ (badge, tag)
- **KHÔNG** hardcode `border-radius` trên component MudBlazor — theme tự xử lý

#### Cấm tuyệt đối

- ❌ Thêm thư viện component UI khác (Radzen, Blazorise, Ant Design Blazor...) — chỉ dùng MudBlazor
- ❌ Thêm `box-shadow` inline trên MudPaper/MudCard — dùng `Elevation` attribute
- ❌ Hạ Elevation của MudPopover, MudDialog, MudDrawer — các overlay này cần shadow để tách khỏi nền

---

### 15. Density Standard — BẮT BUỘC với mọi component/page mới

> Áp dụng từ phiên thiết kế 2026-06-26. Mục tiêu: **gọn vừa phải, nhất quán** —
> không nén quá tay; mobile giữ vùng chạm tối thiểu 40px.
> Tự áp dụng — không cần nhắc.

#### Con số chuẩn (Comfortable-tight)

| Thành phần | Desktop | Mobile (xs ≤ 599px) |
|-----------|---------|---------------------|
| **LineHeight** | `1.45` (theme) | `1.5` (CSS override) |
| **MudTable** | `Dense="true"` — luôn | `Dense="true"` — giữ (card view trên mobile) |
| **MudGrid Spacing** | `Spacing="2"` (form/filter), `Spacing="3"` (KPI/chart) | Giống desktop |
| **Form field Margin** | `Margin="Margin.Dense"` — luôn | Giống desktop |
| **MudAppBar** | `Dense="true"` (48px) | `Dense="true"` |
| **MudNavMenu** | `Margin="Margin.Dense"` | Giống desktop |

#### Thang spacing markup ưu tiên

| Mục đích | Class dùng | Tránh |
|---------|-----------|-------|
| Separator giữa sections | `mb-4` (24px) | `mb-5`, `mb-6` |
| Filter panel / card inner | `pa-4` (24px) | `pa-5`, `pa-6` |
| Separator phụ / field gap | `mb-3` (16px) | |
| Icon trước text | `mr-2` (8px) | `mr-3`, `mr-4` |
| Flex gap trong row | `gap-2` (8px) | `gap-4` trở lên |

> Không có `pa-5`, `pa-6`, `mb-5`, `mb-6` trong dự án này.

#### Filter panel — button alignment chuẩn

MudItem chứa button Tìm/Xóa **phải** dùng `Class="d-flex align-center"` (CSS global tự bottom-align trên sm+). Không đổi thành `align-end` trong markup — CSS đã xử lý.

```razor
@* ✅ Chuẩn — filter panel đầy đủ *@
<MudPaper Elevation="1" Class="pa-4 mb-4">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="6" md="3">
            <MudAutocomplete @bind-Value="_filter.StoreNo"
                             Label="Cửa hàng"
                             Variant="Variant.Outlined"
                             Margin="Margin.Dense"/>
        </MudItem>
        <MudItem xs="12" sm="6" md="2">
            <MudDatePicker @bind-Date="_filter.FromDate"
                           Label="Từ ngày"
                           Variant="Variant.Outlined"
                           Margin="Margin.Dense"/>
        </MudItem>
        <MudItem xs="12" sm="12" md="2" Class="d-flex align-center">
            <MudStack Row="true" Spacing="1" Class="w-100">
                <MudButton Variant="Variant.Filled" Color="Color.Primary"
                           FullWidth="true" OnClick="SearchAsync">Tìm</MudButton>
                <MudButton Variant="Variant.Outlined"
                           FullWidth="true" OnClick="ClearFilter">Xóa</MudButton>
            </MudStack>
        </MudItem>
    </MudGrid>
</MudPaper>
```

#### KPI card row — equal height chuẩn

Dùng `d-flex flex-wrap` với wrapper `div[flex:1]`. CSS global tự stretch `MudPaper` fill chiều cao đồng nhất.

```razor
@* ✅ Chuẩn — KPI row equal height *@
<div class="d-flex flex-wrap gap-3 mb-4">
    <div style="flex:1 1 130px">
        <MudPaper Elevation="2" Class="pa-4 text-center">
            <MudText Typo="Typo.h5" Color="Color.Primary">@value1</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">Label ngắn</MudText>
        </MudPaper>
    </div>
    <div style="flex:1 1 130px">
        <MudPaper Elevation="2" Class="pa-4 text-center">
            <MudText Typo="Typo.h5" Color="Color.Error">@value2</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">Label dài hơn hai dòng</MudText>
        </MudPaper>
    </div>
</div>
```

#### Mobile — giữ vùng chạm tối thiểu

CSS global (`app.css`) đã tự xử lý trên `@media (max-width: 599.98px)`:

| Element | Desktop | Mobile |
|---------|---------|--------|
| `MudButton` | 36px | min 40px |
| `MudIconButton` | 36px | 40×40px |
| Dropdown list item | 5px padding | 8px padding |
| Sidebar nav link | 4px padding | 9px padding |
| LineHeight | 1.45 | 1.5 |

**Không tự thêm media query riêng cho từng component** — CSS global đã đủ.

#### Cấm

- ❌ `Dense="false"` trên MudTable — mặc định Dense nếu không set, nhưng không đặt ngược lại
- ❌ `MudGrid` không có `Spacing` — luôn đặt `Spacing="2"` hoặc `Spacing="3"`
- ❌ Form field không có `Margin="Margin.Dense"` trong filter panel
- ❌ Hardcode `min-height` hay `height` trên button/input — để CSS global xử lý

---

### 16. Audit Log — CRUD Operations (BẮT BUỘC với mọi page ghi dữ liệu)

> **Chi tiết đầy đủ: `.claude/skills/web/audit-logging.md`** — đọc file này trước khi tạo
> bất kỳ page nào có thao tác Create / Update / Delete.

**Quy tắc bắt buộc:**

- Mọi page CRUD **BẮT BUỘC** inject `IAuditLogger` và gọi `await AuditLogger.LogAsync(...)` sau
  mỗi thao tác ghi DB thành công. Không log khi thao tác thất bại.
- Serialize bằng **Newtonsoft.Json** — KHÔNG System.Text.Json.
- Form dialog PHẢI trả DTO đầy đủ: `MudDialog.Close(DialogResult.Ok(_model))` — KHÔNG `Ok(true)`.
- Snapshot `oldValue` cho UPDATE: dùng biến `item` đã có trong page — KHÔNG fetch lại DB.
- Chạy migration `src/POS.Web/Auth/migration_dashboard_audit_log.sql` trên `RPOSMasterData`
  TRƯỚC KHI deploy tính năng có audit. Nếu chưa chạy → log fail silently, không crash app.

**Reference implementation:** `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor`

**KHÔNG làm:**
- ❌ Gọi `AuditLogger.LogAsync` mà không `await`
- ❌ Log trước khi xác nhận DB op thành công
- ❌ Dialog trả `Ok(true)` — page không có newValue để log UPDATE/CREATE

---

## Migrate VCM.BLUEPOS (legacy MVC) → POS.Web — BẮT BUỘC

> Nguồn legacy: `src/legacy/` (VCM.BLUEPOS, .NET Framework 4.6, MVC).
> Danh mục: `_migration/INVENTORY.md` — Tracking: `_migration/PROGRESS.md`.

### 1. Đích đến
Dashboard nội bộ **Blazor Server .NET 10 + MudBlazor 9.5.0**, render mode **global `InteractiveServer`**.
Mọi quy ước UI/Auth/DI theo mục **POS.Web** ở trên.

### 2. Quy ước port code
- `System.Data.SqlClient` → **`Microsoft.Data.SqlClient`**.
- Connection string: lấy qua **`IConfiguration`** (KHÔNG hardcode, KHÔNG dùng legacy `ConfigurationManager`).
- Mọi DAL method **`async`** + **`await using`** (`SqlConnection`/`SqlCommand`/`SqlDataReader`), nhận `CancellationToken`.
- Serialize **Newtonsoft.Json**; DTO đặt trong `POS.Common` (giữ contract field nếu tái dùng).
- Business logic → `POS.Application`; I/O (DB/HTTP) → `POS.Infrastructure`.

### 3. Bảng map `.cshtml` (MVC) → `.razor` (Blazor)

| Legacy (MVC) | POS.Web (Blazor) |
|---|---|
| `Views/{Ctrl}/{Action}.cshtml` | `Components/Pages/{Section}/{Name}Page.razor` (`@page`, `@rendermode InteractiveServer`, `@attribute [Authorize]`) |
| Partial view `_Xyz.cshtml` | child component `.razor` hoặc `<MudDialog>` |
| `@model XyzViewModel` | DTO trong `POS.Common` + field trong `@code` |
| Controller Action GET (mở view) | route `@page` + `OnInitializedAsync` |
| Controller Action POST (ajax load) | method trong `@code` gọi Service/Repository qua DI |
| `$.ajax` / jQuery DataTables | `<MudTable HorizontalScrollbar="true">` (client/server-side) |
| `@Html.DropDownList` / select2 | `<MudSelect>` / `<MudAutocomplete>` (xem mục 13 POS.Web) |
| `Html.BeginForm` + validation | `<MudForm>` + `@bind-Value` |
| `ViewBag` / `TempData` | component state fields |
| Export Excel (EPPlus/NPOI) | giữ lib, trả file qua download stream |
| Rotativa PDF / in ấn | `// TODO: chọn lib PDF .NET 10` |
| Auth Forms/AD/SSO | cookie + bridge token (POS.Web §2) |

### 4. Checklist chuyển 1 chức năng
```
□ Mở _migration/INVENTORY.md, đọc ĐÚNG mục của chức năng → CHỈ đọc các file nó liệt kê
□ Tạo DTO POS.Common/Dtos/{Domain}/ (Newtonsoft, [JsonProperty])
□ Repository POS.Infrastructure/.../{Domain}/ — async + await using + Microsoft.Data.SqlClient + IConfiguration
□ Service POS.Application/Features/{Domain}/ + đăng ký DI (DependencyInjection.cs)
□ Page .razor Components/Pages/{Section}/ theo template chuẩn (responsive, flat, density)
□ Row-level store filter cho StoreOperator nếu áp dụng
□ Audit log (IAuditLogger) nếu có CRUD
□ Build POS.Web + dotnet test (DI test + contract test xanh)
□ Cập nhật _migration/PROGRESS.md: ⏳ TODO → ✅ DONE (kèm bảng Tổng kết)
```

### 5. RULE quan trọng — phạm vi đọc khi migrate
> Khi port một chức năng: **CHỈ đọc các file được liệt kê trong mục INVENTORY của chức năng đó**
> (Controller+Action, View, ViewModel, DAL/SP). **TUYỆT ĐỐI KHÔNG quét lại toàn bộ `src/legacy/`** —
> tránh nhiễu ngữ cảnh và lãng phí. Thiếu file → bổ sung vào INVENTORY trước, rồi mới đọc.

---

## Quy tắc DB Schema — BẮT BUỘC biết

### bảng `dbo.Store` (RPOSMasterData)

| Column | Ý nghĩa | Giá trị |
|--------|---------|---------|
| `No` | Mã cửa hàng (primary key) | `"VIN001"`, `"VIN002"`... |
| `Name` | Tên cửa hàng | |
| `ClosingMethod` | Trạng thái hoạt động | `0` = đang mở cửa, `1` = đã đóng cửa |

> **KHÔNG dùng `Blocked`** — column `Blocked` không tồn tại hoặc không phản ánh trạng thái hoạt động của cửa hàng trong dự án này.

**Query chuẩn khi lấy danh sách cửa hàng đang hoạt động:**
```sql
SELECT No AS StoreNo, Name
FROM dbo.Store (NOLOCK)
WHERE ClosingMethod = 0
ORDER BY No
```

**Dùng ở đâu:** `CentralMDRepository.GetStoreListAsync`, mọi query liên quan đến danh sách store picker trong POS.Web.

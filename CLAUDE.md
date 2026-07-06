# POS API — Claude Code Context

## Dự án
POS API trên **.NET 10** (Clean Architecture) phục vụ ~5.000 máy POS.
- Solution: `POS.slnx`

> **Greenfield + Migration có chủ đích**: dự án khởi đầu là bản port từ POS.API (.NET Framework
> 4.6) rồi chuyển sang phát triển mới, ngừng migrate, và xóa hẳn source cũ khỏi máy. Từ
> **2026-07-03**, source cũ **VCM.BLUEPOS** (.NET Framework 4.6) được **tích hợp lại vào
> `src/legacy/`** làm tài liệu tham chiếu **CHỈ ĐỌC** để port một số chức năng nghiệp vụ cụ thể
> sang kiến trúc mới — xem mục **"Quy tắc Migration từ src/legacy/"** ngay bên dưới. Ngoài phạm
> vi các task migration được giao rõ, mặc định vẫn code mới (greenfield), không tự ý "đọc code cũ
> rồi port" khi không được yêu cầu. Hợp đồng JSON với 5.000 máy POS vẫn giữ nguyên cho các
> endpoint hiện hữu.

## Quy tắc Migration từ src/legacy/ (VCM.BLUEPOS) — BẮT BUỘC

> Áp dụng cho MỌI task "port/migrate chức năng X từ code cũ". Đọc mục này TRƯỚC khi mở
> `src/legacy/`. Không áp dụng cho công việc greenfield thông thường (không liên quan legacy).

### Phạm vi & vị trí

- Source cũ: **`src/legacy/`** — solution `VCM.BLUEPOS.sln` (.NET Framework 4.6.2), gồm
  `VCM.BLUEPOS` (Web/API), `VCM.BLUEPOS.Business`, `VCM.BLUEPOS.Common`, `VCM.BLUEPOS.Data`,
  `VCM.BLUEPOS.Model`.
- **CHỈ ĐỌC — TUYỆT ĐỐI KHÔNG sửa/xóa/format lại file nào trong `src/legacy/`.**
- `src/legacy/` **KHÔNG** được thêm vào `POS.slnx` — không phải project được build/deploy cùng
  solution .NET 10, chỉ để Grep/Read đối chiếu logic nghiệp vụ.
- **Bảng ánh xạ kiến trúc cũ → mới**: `docs/migrations/MIGRATION_MAP.md` — khảo sát đầy đủ
  project/assembly, layering, DI, config, DB/SP, cross-cutting concern của `VCM.BLUEPOS`, bảng
  ánh xạ từng loại thành phần sang layer mới, và danh sách các điểm KHÔNG map 1-1 cần quyết định
  thủ công. **Đọc file này trước** khi định vị logic gốc cho bất kỳ task port nào — tránh khảo sát
  lại từ đầu.

### Quy trình port 1 chức năng

1. **Định vị** logic gốc trong `src/legacy/` bằng Grep/Explore theo tên chức năng/route/SP —
   đối chiếu `docs/migrations/MIGRATION_MAP.md` mục 3 (bảng ánh xạ) để biết layer đích tương ứng.
2. **Đọc hiểu nghiệp vụ** (điều kiện, validation, side-effect, external call) — KHÔNG copy
   nguyên cấu trúc class/namespace/tên biến của code cũ.
3. **Thiết kế lại theo chuẩn dự án mới**: DTO ở `POS.Common/Dtos/{Domain}/`, Repository/AppService
   ở `POS.Infrastructure/.../{Domain}/`, Service ở `POS.Application/Features/{Domain}/`,
   Controller ở `POS.Api/Controllers/` (đúng "Khuôn thêm 1 nghiệp vụ mới" ở mục Greenfield bên
   dưới). Vẫn áp dụng "Cổng chặn trùng lặp" — kiểm tra `docs/CURRENT_STRUCTURE.md` trước khi tạo
   DTO/Service/Repository mới.
4. **Trích dẫn nguồn gốc BẮT BUỘC**: mọi method/block logic port sang phải có comment 1 dòng
   ngay phía trên chỉ rõ `file:dòng` gốc, ví dụ:
   ```csharp
   // Ported from src/legacy/VCM.BLUEPOS.Business/Services/OrderService.cs:142-168
   ```
   Áp dụng cho từng đoạn logic nghiệp vụ có ý nghĩa (không cần chú thích từng dòng vụn vặt).
5. **UI (nếu port kèm màn hình)**: theo chuẩn POS.Web hiện hành (MudBlazor 9, `pos-page-header`,
   MudTable `HorizontalScrollbar`, Density Standard, Flat UI Standard...) — KHÔNG bám theo markup
   WebForms/ASPX/Razor cũ.
6. Sau khi port xong: cập nhật `docs/CURRENT_STRUCTURE.md` cùng commit, chạy
   `dotnet test tests/POS.ContractTests` phải xanh.

### KHÔNG làm

- ❌ Copy nguyên namespace/folder structure của `VCM.BLUEPOS` sang dự án mới.
- ❌ Port method mà không trích dẫn `file:dòng` gốc trong comment.
- ❌ Sửa/xóa file trong `src/legacy/` dưới bất kỳ lý do gì (kể cả "dọn code", thêm comment).
- ❌ Thêm project trong `src/legacy/` vào `POS.slnx`.
- ❌ Đổi tên field JSON response hiện hữu khi port (contract 5.000 POS bất biến).

## 📚 Bản đồ bộ nhớ & Quy tắc đọc-trước-khi-tạo — BẮT BUỘC

> **Nguồn sự thật duy nhất về cấu trúc dùng chung** (DTOs / Services / Repositories / Helpers)
> là `docs/CURRENT_STRUCTURE.md`. Đọc nó TRƯỚC khi tạo bất kỳ artefact dùng chung nào để **tránh
> trùng lặp**. KHÔNG tạo file registry song song khác cho DTO/Service/Repository (sẽ lệch bản đồ).
> **Ngoại lệ đã chốt**: `docs/architecture/database-schema.md` là nguồn sự thật cho **schema DB**
> (tên bảng/cột/kiểu dữ liệu/PK) — mục đích khác `CURRENT_STRUCTURE.md` (schema DB vs cấu trúc
> code C#), không tạo thêm file khác cùng mục đích này.

### Mục lục tài liệu kiến trúc (đọc theo nhu cầu)

| Khi cần… | Đọc file | Nội dung |
|---|---|---|
| Tra DTO / Service / Repository / Helper đã có + **chữ ký method** + bảng DI | **`docs/CURRENT_STRUCTURE.md`** | Bản đồ bộ nhớ chính — cây `POS.Common/Dtos`, mọi interface + method signature, DI registration, danh sách Helpers |
| Viết query/SP/Repository đụng bảng DB `RPOSMasterData` (CentralMD) — tra **tên bảng/tên cột/kiểu dữ liệu/PK** | **`docs/architecture/database-schema.md`** | Bản đồ schema DB — toàn bộ bảng + cột + kiểu dữ liệu + PK/FK + danh sách stored procedure, sinh từ `docs/sql/database/CentralMD.sql` |
| Tạo stored procedure mới cho `RPOSMasterData` | **`.claude/skills/database/SKILLS.md`** | Quy tắc đặt tên `usp_{Domain}_{Action}` + TVP, template SP, cách gọi từ Repository |
| Kiểm tra contract JSON với 5.000 POS | `docs/API_CONTRACT.md` + `tests/POS.ContractTests/` | Tên field response đã khoá |
| Cách thêm DTO mới | `.claude/commands/add-dto-common.md` (skill `/add-dto-common`) | Quy trình thêm DTO vào `POS.Common` |
| Tra quy tắc mã hóa credentials appsettings (`enc:` / `POS_SECRET_KEY`) | **`docs/architecture/appsetting.md`** | Dùng mã hóa hay plaintext (tự suy ra từ nội dung file), phạm vi áp dụng, anti-pattern |
| Trạng thái / lịch sử POS.Web | `docs/WEB_STATUS.md`, `docs/CHANGELOG.md` | — |
| Port chức năng từ `src/legacy/` (VCM.BLUEPOS) — tra layer cũ tương ứng layer nào ở dự án mới | **`docs/migrations/MIGRATION_MAP.md`** | Khảo sát kiến trúc cũ, convention dự án mới, bảng ánh xạ cũ→mới, danh sách điểm KHÔNG map 1-1 cần quyết định |
| Viết file phân tích nghiệp vụ trước khi port (`FEATURE_{Name}_ANALYSIS.md`) — template + checklist | **`.claude/skills/migration/SKILLS.md`** | Template chuẩn, khi nào cần viết, checklist hoàn thành + cập nhật `docs/migrations/STATUS.md` |

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
5. **TRƯỚC khi viết SQL query / stored procedure / Repository method đụng tới bảng trong
   `RPOSMasterData` (CentralMD)** → mở `docs/architecture/database-schema.md`, lấy đúng tên
   bảng/tên cột/kiểu dữ liệu/PK. **KHÔNG suy đoán tên cột.** Bảng cần dùng chưa có trong doc →
   đọc `docs/sql/database/CentralMD.sql` (hoặc script mới nhất tương ứng), rồi bổ sung vào
   `database-schema.md` trong cùng commit.

> Giữ `docs/CURRENT_STRUCTURE.md` đồng bộ với code là **một phần của định nghĩa "xong"** cho mọi
> task thêm/sửa artefact dùng chung. Doc lệch = lần sau AI tạo trùng. Tương tự, giữ
> `docs/architecture/database-schema.md` đồng bộ với script DB là một phần của định nghĩa "xong"
> cho mọi task thay đổi schema.

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

## Quy tắc Stored Procedure — BẮT BUỘC khi tạo SP mới

> **Chi tiết đầy đủ: `.claude/skills/database/SKILLS.md`** — đọc file này trước khi tạo
> bất kỳ stored procedure mới nào cho `RPOSMasterData`, hoặc khi chuyển script Dapper
> inline (INSERT/UPDATE) sang SP.

### Nguyên tắc cốt lõi

- **Tên SP mới BẮT BUỘC theo `dbo.usp_{Domain}_{Action}`** (vd `usp_Product_Save`,
  `usp_SetupCoupon_SaveIssue`) — không dùng dạng tên khác cho SP mới tạo.
- TVP đi kèm (nếu truyền list/child rows) đặt tên `dbo.{Name}TVP` (vd `ProductBarcodeTVP`).
- Script SP mới lưu trong `docs/sql/{Domain}_{Action}.sql`, áp dụng **thủ công 1 lần**
  trên `RPOSMasterData` — app không tự tạo SP.
- SP ghi dữ liệu bắt buộc `SET XACT_ABORT ON` + `BEGIN TRY/CATCH` + `ROLLBACK TRANSACTION`
  khi lỗi + `THROW` (không nuốt lỗi trong SP).
- Gọi từ Repository qua `DynamicParameters` + `CommandType.StoredProcedure`, TVP qua
  `AsTableValuedParameter("dbo.{Name}TVP")`, output param qua `ParameterDirection.Output`.
- Tra đúng tên bảng/cột trong `docs/architecture/database-schema.md` trước khi viết SQL.

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

> Mặc định mọi nghiệp vụ là **code mới** — KHÔNG tự ý migrate từ `POS.Backend` (.NET 4.6) cũ
> (source đã xóa khỏi máy). **Ngoại lệ**: các task port cụ thể từ `src/legacy/` (VCM.BLUEPOS)
> theo mục **"Quy tắc Migration từ src/legacy/"** ở đầu file — chỉ áp dụng khi task yêu cầu rõ
> "port/migrate từ code cũ". Hợp đồng JSON với 5.000 máy POS **vẫn giữ nguyên** cho các endpoint
> hiện hữu.

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
    <MudText Typo="Typo.h5" Class="pos-page-header-title" Style="font-weight:400">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Size="Size.Small" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small"
               StartIcon="@Icons.Material.Filled.Add"
               Class="pos-page-header-btn">
        Thêm
    </MudButton>
</div>
```

> Cập nhật 2026-07-04: `.pos-page-header-title` đã giảm `font-size` xuống `1.25rem` (từ mặc định
> h5 MudBlazor ~1.5rem). Icon cạnh title BẮT BUỘC `Size="Size.Small"` để cân tỷ lệ. Nút hành động
> đi kèm dùng `Size="Size.Small"`. **Cập nhật 2026-07-05**: CTA chính (nút "Thêm" trong header)
> dùng `Variant="Variant.Filled"` theo chuẩn Button mới (xem §14 "Quy ước Button") — không còn
> `Outlined` như bản v2.
> **Font-weight title: BẮT BUỘC thêm `Style="font-weight:400"` ngay trên thẻ `MudText` title**
> (chữ "tự nhiên", không đậm) — ghi đè font-weight 800 kế thừa từ `Typography.H5` trong
> `PosTheme.cs`. Đặt cục bộ trên từng `MudText`, **không** sửa `.pos-page-header-title` global
> (sẽ đổi luôn font-weight cho page chưa polish, gây lệch tạm thời). Đã áp dụng cho toàn bộ 9
> page trong menu "Danh mục" + `ProductsPage.razor` — coi đây là chuẩn cho mọi page header mới/
> polish tiếp theo, không còn là ngoại lệ riêng của 1 page.

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

### 13. UI Polish — Chuẩn đồng bộ giao diện (BẮT BUỘC đọc khi "làm đẹp UI")

> **Chi tiết đầy đủ: `.claude/skills/web/ui-polish-standard.md`** — đọc trước khi sửa markup
> bất kỳ trang nào nhận yêu cầu "sync UI", "đồng bộ giao diện", "làm đẹp".

**Nguyên tắc cốt lõi:**

- GIỮ NGUYÊN 100% `@code { }` — không thêm method/biến/helper. Chỉ sửa markup Razor.
- Màu chip dùng **ternary inline** tại `Color=` — không thêm helper vào `@code`.
- `div.pos-page-header` **là chuẩn dự án** — KHÔNG đổi sang `MudStack Justify.SpaceBetween`.

**4 pattern bắt buộc áp cho mọi trang cần polish UI:**

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

### 14. MudBlazor Theme Standard — BẮT BUỘC với mọi component mới

> **Quy chuẩn UI/UX (MudBlazor):** Khi tạo hoặc sửa BẤT KỲ component/page UI nào, BẮT BUỘC đọc
> `.claude/rules/mudblazor-flat-ui.md` (mapping HTML mockup → MudBlazor Component, quy tắc CSS
> Isolation, Button/Elevation/Radius/Sidebar chi tiết) trước khi code — không chỉ đọc mục này.

> Bản v3 — cập nhật 2026-07-05 theo mockup `docs/web/theme/theme_html.html`, thay cho bản v2
> (2026-07-04, sidebar sáng + card borderless — đã lỗi thời, xem lịch sử quyết định trong
> `.claude/rules/mudblazor-flat-ui.md`). Theme đã cấu hình sẵn trong `PosTheme.cs`. Tự áp dụng —
> không cần nhắc. Sidebar/Drawer giờ nền **navy đậm** (`DrawerBackground = "#0D1B2A"`), card có
> **shadow thật** (không còn borderless — `Shadows.Elevation[2] = "0 2px 8px rgba(0,0,0,.08)"`),
> radius **2 cấp**: `DefaultBorderRadius = "12px"` (Paper/Card/Dialog) + `--pos-radius-sm = 8px`
> (Button/Chip/Input, ép qua CSS). Màu trend/delta (%) vẫn giữ **ngữ nghĩa** tăng=xanh/giảm=đỏ
> (`.pos-delta-up/down` trong `app.css`) — dashboard vận hành POS cần tín hiệu tốt/xấu rõ ràng,
> không dùng màu trang trí tùy ý theo từng KPI.

#### Quy ước Input

- **Variant:** luôn dùng `Variant="Variant.Outlined"`
- **Margin:** luôn dùng `Margin="Margin.Dense"` (trừ khi layout cần Normal)
- **KHÔNG** dùng `Variant.Filled` cho input

#### Input font-size — giảm 15%, không đậm

> Cập nhật 2026-07-04. `Typography.Body1` trong `PosTheme.cs` chi phối text hiển thị/gõ trong
> `MudTextField`/`MudSelect`/`MudDatePicker`/`MudAutocomplete` + item trong dropdown popup của
> chúng — **không** ảnh hưởng `MudTable` (cell dùng size cố định riêng của MudBlazor).

```csharp
Body1 = new Body1Typography { FontSize = "0.75rem", FontWeight = "400" },  // 12px, giảm ~15% từ 14px cũ
```

- Đây là thay đổi **global** (1 chỗ trong `PosTheme.cs`) — tự áp dụng cho mọi input toàn app,
  kể cả trong dialog/form, không cần sửa từng page/dialog riêng.
- **KHÔNG** cần thêm `Style="font-size:..."` hay `Style="font-weight:..."` thủ công trên từng
  `MudTextField`/`MudSelect` — theme đã xử lý toàn cục.

#### Quy ước Button — Filled cho CTA, Outlined cho phần còn lại

> Cập nhật 2026-07-05: đảo ngược chuẩn v2 (Outlined mọi nơi). Theo mockup `theme_html.html`,
> `.btn-primary` là nền đặc màu, không phải viền trong suốt. Mỗi khu vực (page-header,
> filter-panel, dialog-actions) có 1 (hoặc vài) hành động chính dùng `Variant.Filled`; các hành
> động còn lại dùng `Variant.Outlined`, phân biệt bằng `Color`. Áp dụng **cả trong dialog**
> (`DialogActions`) — không chỉ page. `MudChip` (status badge) không thuộc rule này.

| Loại hành động | Variant | Color | Ví dụ |
|---|---|---|---|
| CTA chính (Lưu, Thêm mới, Cập nhật, Tìm trong filter) | `Filled` | `Primary` | "Lưu", "Thêm mới", "Tìm" |
| Hành động tích cực chốt luồng (Duyệt, Kích hoạt, Xác nhận) | `Filled` | `Success` | "Duyệt", "Kích hoạt" |
| Phá hủy/không hoàn tác (Xóa, Khóa, Hủy giao dịch) | `Outlined` | `Error` | "Xóa" |
| Trung tính (Hủy/Đóng dialog, Xóa bộ lọc, Quay lại) | `Outlined` | *(không đặt Color)* | "Hủy", "Đóng", "Xóa lọc" |
| Phụ có ngữ nghĩa riêng (Export Excel, Import, In) | `Outlined` | Color phù hợp ngữ cảnh | "Export Excel" |

```razor
@* CTA chính filter panel *@
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SearchAsync">Tìm</MudButton>
<MudButton Variant="Variant.Outlined" OnClick="ClearFilter">Xóa</MudButton>

@* Dialog actions — hành động phá hủy *@
<MudButton Variant="Variant.Outlined" OnClick="Cancel">Hủy</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Error" OnClick="DeleteAsync">Xóa</MudButton>

@* Workflow phê duyệt *@
<MudButton Variant="Variant.Outlined" Color="Color.Error" OnClick="RejectAsync">Từ chối</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Success" OnClick="ApproveAsync">Duyệt</MudButton>
```

- "Lưu" LUÔN là CTA (`Filled`/`Primary`), KHÔNG phải `Success` dù nghe có vẻ "hoàn tất". "Sửa"/
  "Thêm dòng" cũng xếp CTA (`Filled`/`Primary`) vì là lối vào luồng tạo/sửa.
- Nút điều hướng thuần túy (chuyển trang/quay lại, không lưu/xóa gì) xếp Trung tính.
- Nếu không rõ 1 nút thuộc loại nào → ưu tiên Trung tính (`Outlined`, không đặt `Color`) — an
  toàn hơn đoán sai thành destructive/CTA.
- **Bẫy dễ bỏ sót — confirm dialog**: `DialogService.ShowAsync<MudMessageBox>(title, parameters,
  options)` render nút Yes bằng markup **mặc định của MudBlazor**, không sửa được vì không có
  `<YesButton>` slot — **KHÔNG** dùng cách gọi này. Luôn khai báo `<MudMessageBox @ref="_confirmBox">`
  trực tiếp trong Razor với `<YesButton>` tường minh, gọi `await _confirmBox!.ShowAsync()`, và chọn
  Variant/Color theo bản chất hành động Yes (bảng trên). Nếu 1 dialog dùng chung cho nhiều hành
  động khác nhau tùy ngữ cảnh (vd khóa/mở khóa), dùng ternary inline dựa trên field/biến message có
  sẵn — không thêm field mới. Xem pattern đầy đủ trong `.claude/skills/web/SKILLS.md`
  §"MudMessageBox @ref".

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

- **Card/Paper chứa nội dung:** `Elevation="2"` — có shadow thật (`0 2px 8px rgba(0,0,0,.08)`,
  xem `Shadows.Elevation` trong `PosTheme.cs`)
- **Filter panel:** `Elevation="1"` (flat, không shadow) + class `pos-filter-panel` (nền trắng +
  border 1px `var(--pos-border)`, xem `app.css`) — theo mockup `.filter-bar`
- **Section phân tách nhẹ (không cần shadow):** `Elevation="0"` hoặc `Elevation="1"`
- **KHÔNG** dùng `Elevation="3"` trở lên cho card/paper thông thường (elevation 3 = elevation 2)
- **KHÔNG** tự thêm `border`/`box-shadow` CSS cho MudPaper/MudCard — dùng thuộc tính `Elevation`,
  theme đã cấu hình đúng giá trị shadow cho từng mức

```razor
@* ✅ Filter panel — nền soft-tint *@
<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">
    <MudGrid Spacing="2">@* filter fields *@</MudGrid>
</MudPaper>
```

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

| Loại component | Elevation dùng | Ý nghĩa |
|---------------|---------------|-------|
| Filter panel / toolbar | `1` | Flat, không shadow (`"none"`) |
| Card / KPI card / data table wrap | `2` | Shadow thật `0 2px 8px rgba(0,0,0,.08)` |
| Login card / callout nổi bật | `4` | Shadow mạnh hơn `0 4px 20px rgba(0,0,0,.12)` |
| **MudPopover / dropdown** | **KHÔNG hạ** — giữ E8 mặc định | MudSelect, MudAutocomplete, MudDatePicker, MudMenu cần nổi |
| **MudDialog** | **KHÔNG hạ** — giữ E12 mặc định | Dialog phải nổi trên overlay |

> **Quy tắc cốt lõi:** `Shadows.Elevation[0..5]` trong `PosTheme.cs` đã có giá trị shadow thật
> (không còn `"none"` như chuẩn borderless cũ). `Elevation 6+` giữ nguyên thang shadow cho
> overlay/dropdown/dialog, chỉ đổi tint màu theo navy mới.

#### Border-radius chuẩn dự án — 2 cấp

- Theme: `DefaultBorderRadius = "12px"` (cấu hình trong `PosTheme.cs`) — áp dụng cho
  Paper/Card/Dialog/Popover/Menu
- Control (Button/Chip/Input): ép riêng `8px` qua CSS (`.mud-button-root`, `.mud-chip`,
  `.mud-input-outlined-border` trong `app.css`) — KHÔNG đổi qua `DefaultBorderRadius` vì sẽ ảnh
  hưởng luôn Paper/Card
- CSS token: `--pos-radius-sm: 8px` (control) | `--pos-radius-md: 8px` (dự phòng) |
  `--pos-radius-lg: 12px` (card, active/hover nav)
- **KHÔNG** hardcode `border-radius` trên component MudBlazor — theme/CSS token đã xử lý (vd
  không tự thêm `Style="border-radius:4px"` trên `MudProgressLinear`/`MudPaper`)

#### Màu Sidebar / AppBar

- `DrawerBackground = "#0D1B2A"` (navy đậm), `DrawerText`/`DrawerIcon` dùng trắng translucent
  (`rgba(255,255,255,0.6)`) — theo mockup `theme_html.html` (`.sidebar`)
- `AppbarBackground = "#FFFFFF"` (topbar vẫn sáng) — `MudAppBar` dùng `Color="Color.Default"`,
  có shadow riêng `0 1px 4px rgba(0,0,0,.04)` (CSS `.mud-appbar`)
- Active nav item: nền **đặc** `var(--pos-primary)`, chữ/icon trắng, `border-radius:
  var(--pos-radius-sm)` (8px, KHÔNG phải `-lg`) — KHÔNG dùng pill tint nhạt hay thanh viền trái
- Hover (không active): nền `var(--pos-drawer-hover)` (`#1E3448`)
- **3 cấp sidebar theo mockup**: L1 (Cửa hàng/Danh mục/...) — **CHỮ IN HOA**, faint, KHÔNG icon
  (giống `.nav-section-label`); L2 (Vận hành/Báo cáo/...) — **có icon Material riêng** cho từng
  nhóm, sáng nhất (giống `.nav-item`); L3 (leaf link) — icon `ChevronRight` đồng nhất, mờ hơn L2.
  Nhóm "Quản trị" cấu trúc phẳng (không có L2) nên leaf link giữ icon riêng như trước.
- **Icon set giữ `Icons.Material.Outlined.*`** (không dùng emoji dù mockup dùng emoji) — quyết
  định có chủ đích, tránh rủi ro hiển thị không nhất quán giữa OS/browser.
- **Lưu ý kỹ thuật quan trọng**: tham số `Icon` của `MudNavLink`/`MudNavGroup`/`MudIcon` nhận
  **dữ liệu SVG path** (`<path d="...">`), KHÔNG phải text/ligature — KHÔNG BAO GIỜ truyền emoji
  hay ký tự Unicode thường vào `Icon=`, path sẽ vô hiệu và icon biến mất hoàn toàn (không lỗi,
  không cảnh báo). Icon luôn phải là hằng số `Icons.Material.*`.
- `pos-sidebar-brand` (đầu `MudDrawer`, trước `MudNavMenu`): text-only 2 dòng (tên app + subtitle,
  KHÔNG icon/avatar) — khớp `.logo` mockup.
- `pos-sidebar-footer` (cuối `MudDrawer`, `margin-top:auto`): avatar chữ cái đầu (tròn, nền
  Primary) + tên + role + nút logout — user-info đã dời từ `MudAppBar` xuống đây.
- Icon nav sidebar: `1.25rem` → `1.125rem` (18px, CSS `.mud-drawer .mud-icon-root`) — khớp
  `.nav-icon{width:18px}` mockup.
- MudBlazor 9.5.0 class thật cho 3-cấp nav (đã verify từ `MudBlazor.min.css`, KHÔNG đoán):
  `.mud-navmenu` (không phải `.mud-nav-menu`), title của mỗi `MudNavGroup` là chính
  `.mud-nav-group > .mud-nav-link` (không có class `-title` riêng), children nằm trong
  `.mud-collapse-container`. Xem comment chi tiết trong `app.css` gần các selector nav.

#### Màu trend/delta (%) — giữ ngữ nghĩa

- `.pos-delta-up`/`.pos-delta-down` (app.css) — pill badge nền nhạt (`--pos-success-bg`/
  `--pos-danger-bg`), chữ đậm màu `--pos-success`/`--pos-danger`
- **BẮT BUỘC** giữ ngữ nghĩa tăng=xanh/giảm=đỏ cho mọi chỉ số vận hành (doanh thu, lỗi, trạng
  thái máy POS...) — **KHÔNG** dùng màu trang trí tùy ý theo từng KPI (mất tín hiệu tốt/xấu)

#### Cấm tuyệt đối

- ❌ Thêm thư viện component UI khác (Radzen, Blazorise, Ant Design Blazor...) — chỉ dùng MudBlazor
- ❌ Thêm `box-shadow` inline trên MudPaper/MudCard — dùng `Elevation` attribute
- ❌ Hạ Elevation của MudPopover, MudDialog — 2 overlay nổi tạm thời này cần shadow thật (E6+) để
  tách khỏi nền
- ❌ Truyền emoji/text thường vào tham số `Icon=` của `MudNavLink`/`MudNavGroup`/`MudIcon` — tham
  số này nhận SVG path, không phải ligature; icon sẽ biến mất im lặng (xem mục "Màu Sidebar/AppBar")

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

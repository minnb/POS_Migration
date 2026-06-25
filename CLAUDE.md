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

## Thêm DTO mới: dùng lệnh `/add-dto-common`

Xem `.claude/commands/add-dto-common.md` để biết cách dùng.

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

## Quy tắc Background Worker — POS.Worker (BẮT BUỘC)

> **Chi tiết đầy đủ: `.claude/skills/worker/SKILLS.md`** — đọc file này trước khi migrate bất kỳ
> scheduled job, message consumer, hay tác vụ chạy nền nào sang `POS.Worker`.

### Nguyên tắc cốt lõi

- `POS.Worker` chỉ là **host mỏng** (`Program.cs` đăng ký hosted service).
- **Implementation worker đặt trong `src/POS.Infrastructure/Workers/`** — namespace `POS.Infrastructure.Workers`.
- Worker là **singleton** → resolve repository scoped qua `IServiceScopeFactory.CreateAsyncScope()`, KHÔNG inject thẳng.
- Vòng lặp `ExecuteAsync` KHÔNG được chết: try/catch nuốt exception, set `healthState.Status = "Degraded"`, log, lặp tiếp.
- Hai khuôn mẫu: **timer polling** (`PeriodicTimer`) và **message consumer** (RabbitMQ push, `prefetchCount: 1`, `autoAck: false`).
- Serialize bằng **Newtonsoft.Json**; cập nhật `WorkerHealthState`; heartbeat → Redis key `Worker:Heartbeat:{Name}`.
- Đăng ký mỗi worker mới: `builder.Services.AddHostedService<{Name}Worker>();` trong `Program.cs`.

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
| `/web-gen-hash` | Tạo BCrypt hash cho user migration SQL |
| `/add-dto-common` | Thêm DTO mới vào POS.Common (xem `.claude/commands/add-dto-common.md`) |

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

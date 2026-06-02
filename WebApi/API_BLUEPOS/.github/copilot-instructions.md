# Copilot Instructions – VCM.POSBLUE.API

---

## 1. Context (Ngữ cảnh)

`VCM.POSBLUE.API` là Web API backend phục vụ hệ thống POS bán lẻ WinMart/WinMart+.
API này là **cầu nối trung tâm** giữa ~5.000 máy POS (client) và các hệ thống backend/partner bao gồm:

| Hệ thống | Vai trò |
|----------|---------|
| **Capillary** | Loyalty – tích/tiêu điểm thành viên |
| **The WinX** | Partner – QR Code, Voucher, MML Scheme |
| **VinID / CrowX** | Payment, Loyalty legacy |
| **SAP / Odoo** | Voucher, trả hàng |
| **Redis Sentinel** | Cache phân tán (1 master + 2 slave) |
| **RabbitMQ Cluster** | Async logging, queue xử lý |
| **Elasticsearch / Serilog** | Centralized logging |
| **SQL Server (Dapper)** | CentralMD, Loyalty, StagingDB |

- **Target Framework:** .NET Framework 4.6.2
- **C# Version:** 7.3
- **API Style:** ASP.NET Web API 2 (attribute routing)
- **Deployment:** IIS hosted, multi-server environment

---

## 2. Code Standards (Tiêu chuẩn Code)

### 2.1 Cấu trúc thư mục

```
VCM.POSBLUE.API/
├── Controllers/        # API endpoints – kế thừa BaseController
├── AppServices/        # Business logic nằm trong project này
├── Services/           # Service classes (WinLifeService, POSGiftService...)
├── App_Start/          # WebApiConfig, FilterConfig, MessageHandler
├── Const/              # URL constants theo từng partner
├── Helpers/            # ApiHelper, NetworkHelper, VersionHelper...
├── Models/             # Request/Response models cục bộ
└── Global.asax.cs      # Application startup & Timer initialization

API_WebApiCore/         # Shared business logic layer (cross-project)
├── AppServices/        # Capillary, Offer, Partner services
├── DbContext/          # RedisManager, RedisSentinelConnection, Dapper factories
├── Repository/         # Data access (LoyaltyRepository, CentralMDRepository...)
└── Shared/             # OpsMonitoringHelper, LoyaltyHelper, RabbitMQProducer...

API_Common/             # Shared DTOs, Helpers, Constants (cross-project)
├── Dtos/               # Request/Response DTOs theo domain
├── Helpers/            # StringHelper, FormatHelper, FileHelper, HostHelper...
├── Const/              # RedisKeyConst, AppConst...
└── Enums/              # Enums dùng chung
```

### 2.2 Naming Conventions

| Thành phần | Quy tắc | Ví dụ |
|-----------|---------|-------|
| Controller class | `{Domain}Controller : BaseController` | `GiftController`, `LoyaltyController` |
| Service class | `{Domain}Service` | `MMLSchemeService`, `WinXService` |
| Repository class | `{Domain}Repository` | `LoyaltyRepository`, `CentralMDRepository` |
| DTO class (request) | `{Domain}Request` | `MMLSchemeRequest`, `VinIDSalesRequest` |
| DTO class (response) | `{Domain}Response` | `MMLSchemeResponse`, `ResultResponse` |
| Private field | `_camelCase` | `_kibanaService`, `_redisManager` |
| Static shared field | `_shared{Name}` | `_sharedRedisManager`, `_sharedKibana` |
| Redis key method | `Get{Domain}Key(...)` | `GetRedisKeyLoyaltyBalancePoints(...)` |
| Async method | Suffix `Async` hoặc dùng `async Task` | `GetDataMMLSchemeResponseAsync` |
| Route prefix | `[RoutePrefix("api/{domain}")]` | `[RoutePrefix("api/common")]` |

### 2.3 API Response Pattern

Tất cả API endpoint **bắt buộc** trả về `ResultResponse`:

```csharp
// Success
return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
{
    Message = "Success",
    Status = HttpStatusCode.OK,
    Data = data,
    MessageTechnical = ""
});

// Error
return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
{
    Message = ex.Message,
    Status = HttpStatusCode.BadRequest,
    MessageTechnical = JsonConvert.SerializeObject(ex)
});
```

**Không được** trả về raw object, string, hoặc custom response format khác ngoài `ResultResponse`.

### 2.4 Exception Handling

```csharp
// Pattern chuẩn trong Controller
try
{
    // business logic
}
catch (Exception ex)
{
    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
    {
        Message = ex.Message,
        Status = HttpStatusCode.BadRequest,
        MessageTechnical = JsonConvert.SerializeObject(ex)
    });
}

// Pattern chuẩn trong Service/Repository
catch (Exception ex)
{
    FileHelper.WriteExpLogs("{ClassName}.{MethodName}.Exception", ex);
    // hoặc
    _kibanaService.LogException("{endpoint}", posNo, 0, "", JsonConvert.SerializeObject(ex));
    return default; // hoặc throw tùy context
}
```

**Không được** để exception propagate mà không log.

### 2.5 Logging

Dùng **2 layer logging** theo thứ tự ưu tiên:

```csharp
// Layer 1 – KibanaService (structured log → Elasticsearch qua Serilog)
_kibanaService.LogRequest(endpoint, posNo, requestBody);
_kibanaService.LogResponse(endpoint, posNo, responseTimeMs, "", responseBody);
_kibanaService.LogException(endpoint, posNo, 0, "", errorDetail);

// Layer 2 – FileHelper (file log fallback)
FileHelper.WriteLogs($"{context}: {message}");
FileHelper.WriteExpLogs($"{ClassName}.{MethodName}", ex);
```

**Không dùng** `Console.WriteLine`, `Debug.WriteLine`, hay `Trace` cho production logging.

### 2.6 Dependency Management (DI Pattern)

Project **không dùng IoC container** (không có Autofac/Unity/DI framework).
Áp dụng pattern **manual DI + static shared instances**:

```csharp
// ✅ ĐÚNG – static shared instance cho service không có state per-request
private static readonly KibanaService _kibanaService = new KibanaService();
private static readonly MemoryCacheService _memoryCacheService = new MemoryCacheService();
private static readonly RedisManager _redisManager = new RedisManager();

// ✅ ĐÚNG – new instance trong constructor cho service có state per-request
public GiftController()
{
    _posGiftService = new POSGiftService(); // có state riêng
}

// ❌ SAI – new instance mỗi request cho stateless service
public HttpResponseMessage MyAction()
{
    var kibana = new KibanaService(); // tốn GC, không dùng
}
```

### 2.7 Redis Access Pattern

```csharp
// Read operations → dùng replica
var data = _redisManager.HashGet<T>(hashKey, hashField, isServerRead: true);
var value = await _redisManager.GetStringAsync(key); // tự routing đến replica

// Write operations → luôn dùng master (default)
_redisManager.HashSet(hashKey, hashField, value, ttlSeconds);
_redisManager.StringSet(key, value, ttlSeconds);
_redisManager.Delete(key);

// Redis key → LUÔN dùng RedisKeyConst hoặc AppConst, không hardcode
var key = RedisKeyConst.GetRedisKeyLoyaltyBalancePoints(phoneNumber);
```

### 2.8 Database Access Pattern

Dùng **Dapper** qua factory pattern, **không dùng Entity Framework** cho new code:

```csharp
using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
{
    db.Open();
    var result = db.Query<MyDto>(sql, new { param }, commandTimeout: 30).ToList();
}

// Transaction
using (var transaction = db.BeginTransaction())
{
    try
    {
        db.Execute(sql, data, transaction, commandTimeout: 3600);
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

### 2.9 Async/Await Pattern

```csharp
// ✅ Fire-and-forget cho side-effect (logging, notification)
_ = Task.Run(() => HttpClientService.SendMessageSMS(...));

// ✅ Parallel execution cho 2 independent HTTP calls
var task1 = _serviceA.CallAsync(request);
var task2 = _serviceB.CallAsync(request);
await Task.WhenAll(task1, task2);
var result1 = task1.Result;
var result2 = task2.Result;

// ✅ Async Task trong Timer callback
_timer = new Timer(async _ =>
{
    try { await MyService.DoWorkAsync(); }
    catch (Exception ex) { FileHelper.WriteExpLogs("Timer", ex); }
}, null, interval, interval);
```

---

## 3. Copilot Rules (Quy tắc bắt buộc khi sinh code)

### 3.1 Giữ nguyên phong cách code hiện có

- **PHẢI** kế thừa `BaseController` cho mọi Controller mới.
- **PHẢI** dùng `ResultResponse` làm kiểu trả về duy nhất của API.
- **PHẢI** dùng `[RoutePrefix]` + `[Route]` attribute routing, không dùng convention routing.
- **PHẢI** dùng `[ValidateModel]` attribute + kiểm tra `ModelState.IsValid` ở đầu action khi nhận `[FromBody]`.
- **PHẢI** đặt `static readonly` cho các service instance dùng chung (KibanaService, MemoryCacheService, RedisManager).

### 3.2 Không tự ý thêm thư viện mới

- **KHÔNG** thêm NuGet package mới nếu chưa có trong solution (kiểm tra `packages.config` hoặc `.csproj` trước).
- **KHÔNG** dùng `System.Text.Json` – project dùng **Newtonsoft.Json** toàn bộ.
- **KHÔNG** dùng `HttpClient` trực tiếp trong Controller – dùng `ApiHttpClientHelper` hoặc `RestSharpApiHelper` đã có.
- **KHÔNG** thêm IoC/DI framework – dùng manual DI như hiện tại.
- **KHÔNG** dùng `Entity Framework` cho code mới – dùng Dapper.

### 3.3 Giới hạn phạm vi thay đổi

- Khi được yêu cầu sửa 1 function, **CHỈ** sửa function đó, không refactor code xung quanh.
- **KHÔNG** đổi tên method/class/property đang tồn tại (breaking change cho client POS).
- **KHÔNG** thay đổi cấu trúc `ResultResponse` hoặc HTTP status code mapping đang có.
- **KHÔNG** xóa comment tiếng Việt hiện có – đây là documentation cho team.

### 3.4 C# 7.3 / .NET Framework 4.6.2 Constraints

- **KHÔNG** dùng `record`, `init`, `with` expression (C# 9+).
- **KHÔNG** dùng nullable reference types `string?` (C# 8+).
- **KHÔNG** dùng `IAsyncEnumerable` (C# 8+).
- **KHÔNG** dùng `System.Runtime.CompilerServices.Unsafe` hoặc Span<T> APIs không có trong 4.6.2.
- **PHẢI** dùng `Tuple<T1,T2>` hoặc `ValueTuple (T1, T2)` – cả 2 đều được hỗ trợ trong C# 7.3.
- **PHẢI** dùng `async Task` / `async Task<T>` thay vì `async ValueTask` khi không có lý do đặc biệt.

### 3.5 Logging & Monitoring

- Mọi method `catch` trong Controller/Service **PHẢI** có ít nhất 1 trong 2: `FileHelper.WriteExpLogs(...)` hoặc `_kibanaService.LogException(...)`.
- **KHÔNG** dùng `throw` không có log ở tầng Controller.
- Khi thêm tính năng mới, **PHẢI** thêm log ở cả đầu vào (LogRequest) và đầu ra (LogResponse).

### 3.6 Security

- Mọi Controller **PHẢI** được bảo vệ qua `BasicAuthenticationAttribute` đã cấu hình ở `WebApiConfig`.
- **KHÔNG** log raw password, token, hoặc thông tin nhạy cảm vào Kibana/File.
- Thông tin config nhạy cảm (password, API key) **PHẢI** đọc từ `WebConfigurationManager.AppSettings` hoặc `ConnectionStrings`, không hardcode.

---

## 4. Architecture Summary (Tóm tắt kiến trúc)

```
[5000 POS Clients]
        │ HTTP/HTTPS
        ▼
[BasicAuthenticationAttribute]  ← WebApiConfig.cs
        │
        ▼
[MessageLoggingHandler]  ← Request/Response timing + OpenTelemetry trace
        │
        ▼
[Controller : BaseController]
   ├── Validate ModelState
   ├── Call AppService / Repository
   ├── Return ResultResponse
   └── catch → ResultResponse(BadRequest)
        │
        ├──► [AppServices/]   → HTTP calls to partners (WinX, Capillary, VinID...)
        ├──► [Repository/]    → Dapper → SQL Server
        ├──► [RedisManager]   → Redis Sentinel (master write / replica read)
        └──► [RabbitMQProducer] → RabbitMQ Cluster (async logging/queue)
                │
                ▼
         [KibanaService] → Serilog → Elasticsearch
         [FileHelper]    → Local log files (fallback)
         [OpsMonitoring] → Timer 10 phút → Queue_Ops_Logging
```

---

## 5. Key Design Patterns in Use

| Pattern | Áp dụng ở |
|---------|-----------|
| **Repository Pattern** | `LoyaltyRepository`, `CentralMDRepository`, `DataRawJsonRepository` |
| **Service Layer** | `MMLSchemeService`, `WinXService`, `LoyaltyOfflineService` |
| **Static Shared Instance** | `_sharedRedisManager`, `_sharedKibana` trong các Controller |
| **Double-Checked Locking** | `RedisManager.GetConnection()`, `RedisSentinelConnection` |
| **Circuit Breaker (partial)** | `LoyaltyOfflineService.IsOfflineCapillary()` với MemoryCache TTL |
| **Fire-and-forget** | `Task.Run(() => RabbitMQProducer.ProducerRabbtMQCluster(...))` |
| **Facade** | `MemoryCacheService` wrapping nhiều config cache |
| **Factory** | `DapperCentralMDFactory`, `DapperLoyaltyFactory` |
| **Template Method** | `BaseController` cung cấp `NewExceptionModels()`, `ExceptionModels()` |

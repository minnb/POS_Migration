# Infrastructure Specification — POS Backend

> ⭐ **Claude Code PHẢI đọc file này trước khi tạo bất kỳ file nào
> trong `POS.Infrastructure/`.**
>
> File này mô tả chính xác cách dự án kết nối với DB, Cache, Message Queue
> và các external services. Không được tự ý dùng implementation đơn giản hơn
> so với những gì được mô tả ở đây.

---

## 1. Database — Dapper + SQL Server (có thể đổi sang PostgreSQL)

**Driver hiện tại:** `Microsoft.Data.SqlClient`
**ORM:** Dapper — KHÔNG dùng Entity Framework Core

### Cấu hình connection pool
```json
// appsettings.json
{
  "ConnectionStrings": {
    "PosDb": "Server=...;Database=...;User Id=...;Password=...;
              Min Pool Size=5;Max Pool Size=100;
              Connection Timeout=30;Command Timeout=60;"
  }
}
```

### IDbConnectionFactory — bắt buộc dùng, KHÔNG inject SqlConnection trực tiếp
```csharp
// Đã định nghĩa trong CLAUDE.md mục 6 — xem chi tiết ở đó
```

### Các lưu ý đặc thù từ dự án cũ
<!-- TODO: Bạn điền vào đây các đặc thù của DB cũ -->
<!-- Ví dụ:
- Có stored procedures không? Tên gì?
- Có table-valued parameters không?
- Có dùng schema khác ngoài dbo không?
- Có read replica / write replica riêng không?
-->

---

## 2. Cache — Redis

> ⚠️ **KHÔNG dùng `IDistributedCache` thuần.**
> Dự án dùng Redis với cấu hình đặc thù bên dưới — phải implement đúng.

### Topology
<!-- TODO: Điền topology thực tế của dự án cũ -->
<!-- Chọn 1 trong các option sau và điền thông tin: -->

**Option A — Redis Sentinel (High Availability)**
```json
{
  "Redis": {
    "Mode": "Sentinel",
    "SentinelHosts": [
      "sentinel-host-1:26379",
      "sentinel-host-2:26379",
      "sentinel-host-3:26379"
    ],
    "ServiceName": "mymaster",
    "Password": "",
    "DefaultDatabase": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false
  }
}
```

**Option B — Redis Standalone**
```json
{
  "Redis": {
    "Mode": "Standalone",
    "Host": "redis-host",
    "Port": 6379,
    "Password": "",
    "DefaultDatabase": 0
  }
}
```

### Implementation bắt buộc dùng StackExchange.Redis trực tiếp

```csharp
// POS.Infrastructure/Cache/IRedisCacheService.cs
public interface IRedisCacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
}

// POS.Infrastructure/Cache/RedisCacheService.cs
// Implement dùng IDatabase từ StackExchange.Redis
// Serialize/Deserialize dùng System.Text.Json
```

### DI Registration cho Sentinel
```csharp
// KHÔNG dùng: services.AddStackExchangeRedisCache(...)
// PHẢI dùng:
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var options = new ConfigurationOptions();

    // Thêm tất cả sentinel hosts
    foreach (var host in config.GetSection("Redis:SentinelHosts").Get<string[]>())
        options.EndPoints.Add(host);

    options.ServiceName = config["Redis:ServiceName"]; // "mymaster"
    options.Password = config["Redis:Password"];
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 5000;

    return ConnectionMultiplexer.Connect(options);
});
services.AddSingleton<IRedisCacheService, RedisCacheService>();
```

### Cache key conventions
<!-- TODO: Điền các prefix/pattern key đang dùng trong dự án cũ -->
<!-- Ví dụ:
- Session:  "pos:session:{terminalId}"
- Voucher:  "pos:voucher:{voucherCode}"
- Member:   "pos:member:{phoneNumber}"
- MasterData: "pos:masterdata:{fileType}:{version}"
-->

---

## 3. Message Queue — RabbitMQ

> ⚠️ **KHÔNG tự tạo connection RabbitMQ đơn giản.**
> Phải dùng đúng thư viện và cấu hình bên dưới.

### Thư viện
<!-- TODO: Điền thư viện đang dùng trong dự án cũ -->
<!-- Chọn 1: -->
- [ ] **MassTransit** (abstraction layer trên RabbitMQ) — khuyến nghị
- [ ] **EasyNetQ**
- [ ] **RabbitMQ.Client** trực tiếp

### Cấu hình
```json
{
  "RabbitMQ": {
    "Host": "rabbitmq-host",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "pos_user",
    "Password": "",
    "Heartbeat": 60,
    "RequestedConnectionTimeout": 30000
  }
}
```

### Các Queue / Exchange đang dùng
<!-- TODO: Điền danh sách queue/exchange từ dự án cũ -->
<!-- Ví dụ:
| Queue/Exchange | Type | Mục đích | Publisher / Consumer |
|---|---|---|---|
| pos.transaction.created | Direct | Ghi log giao dịch | Publisher: API, Consumer: Worker |
| pos.voucher.used | Fanout | Notify voucher đã dùng | Publisher: API |
| pos.session.closed | Direct | Tổng kết ca | Publisher: API, Consumer: Worker |
-->

### Implementation
```csharp
// POS.Infrastructure/Messaging/IMessagePublisher.cs
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey = null) where T : class;
}

// POS.Infrastructure/Messaging/IMessageConsumer.cs
public interface IMessageConsumer<T> where T : class
{
    Task HandleAsync(T message);
}
```

---

## 4. HTTP Clients — External Services

> Dùng **`IHttpClientFactory`** với **Polly** cho retry/circuit breaker.
> KHÔNG tạo `HttpClient` trực tiếp bằng `new HttpClient()`.

### Danh sách external services
<!-- TODO: Điền từ dự án cũ -->

| Service | Base URL | Auth | Timeout | Retry |
|---|---|---|---|---|
| GotIt | `https://api.gotit.vn` | API Key header | 30s | 3 lần |
| Urbox | `https://api.urbox.vn` | Bearer token | 30s | 3 lần |
| *(thêm service khác)* | | | | |

### DI Registration với Polly
```csharp
services.AddHttpClient<IGotItApiClient, GotItApiClient>(client =>
{
    client.BaseAddress = new Uri(config["ExternalServices:GotIt:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("X-Api-Key", config["ExternalServices:GotIt:ApiKey"]);
})
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p =>
    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

---

## 5. Logging — Serilog

### Sinks đang dùng
<!-- TODO: Điền từ dự án cũ -->
<!-- Ví dụ: File, Console, Seq, Elasticsearch, Application Insights -->

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/pos-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

---

## 6. Thứ tự Build Infrastructure Layer

Claude Code phải build theo đúng thứ tự này, **hoàn thành và verify từng bước**
trước khi chuyển sang bước tiếp:

```
Bước 1: IDbConnectionFactory + test connect DB
         ↓
Bước 2: IRedisCacheService + test connect Redis (đúng topology)
         ↓
Bước 3: IMessagePublisher/Consumer + test connect RabbitMQ
         ↓
Bước 4: IHttpClientFactory cho từng external service
         ↓
Bước 5: Serilog configuration
         ↓
Bước 6: Viết InfrastructureHealthCheck để verify tất cả kết nối
         ↓
✅ Infrastructure xong → mới bắt đầu convert module nghiệp vụ
```

---

## 7. NuGet Packages cần cài

```xml
<!-- Database -->
<PackageReference Include="Dapper" Version="2.*" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.*" />
<!-- <PackageReference Include="Npgsql" Version="8.*" /> khi đổi PostgreSQL -->

<!-- Cache -->
<PackageReference Include="StackExchange.Redis" Version="2.*" />

<!-- Message Queue — chọn 1 -->
<PackageReference Include="MassTransit.RabbitMQ" Version="8.*" />
<!-- hoặc <PackageReference Include="EasyNetQ" Version="7.*" /> -->

<!-- HTTP Resilience -->
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.*" />

<!-- Logging -->
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.*" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.*" />
```
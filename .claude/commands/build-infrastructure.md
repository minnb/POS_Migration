# /build-infrastructure — Build Toàn Bộ Infrastructure Layer

## Mô tả
Chạy command này **một lần duy nhất trước khi convert bất kỳ module nghiệp vụ nào**.
Infrastructure layer phải hoàn chỉnh và kết nối được trước khi làm business logic.

---

## PROMPT

```
Hãy đọc 2 file sau trước khi làm bất cứ gì:
1. CLAUDE.md
2. docs/infrastructure-spec.md  ← quan trọng nhất cho task này

Sau đó build toàn bộ Infrastructure Layer theo đúng thứ tự sau.
Hoàn thành từng bước, KHÔNG nhảy cóc:

---

## BƯỚC 1 — Database (Dapper)

Tạo các file:
- `src/POS.Infrastructure/Persistence/IDbConnectionFactory.cs`
- `src/POS.Infrastructure/Persistence/SqlServerConnectionFactory.cs`
  → Đọc connection string từ config, có connection pool đúng như infrastructure-spec.md
- `src/POS.Infrastructure/Repositories/BaseRepository.cs`
  → Protected method CreateConnection(), có logging query time

Sau khi tạo xong, viết đoạn test connection trong Program.cs
(dùng `await conn.ExecuteScalarAsync("SELECT 1")`) — xóa sau khi confirm OK.

---

## BƯỚC 2 — Redis Cache

Đọc kỹ mục "Cache" trong infrastructure-spec.md để biết topology
(Sentinel / Cluster / Standalone) rồi implement ĐÚNG topology đó.

Tạo các file:
- `src/POS.Infrastructure/Cache/IRedisCacheService.cs`
- `src/POS.Infrastructure/Cache/RedisCacheService.cs`
  → Dùng StackExchange.Redis IDatabase trực tiếp
  → Serialize bằng System.Text.Json
  → Có GetOrSetAsync với distributed lock chống cache stampede
- `src/POS.Infrastructure/Cache/CacheKeys.cs`
  → Tập trung tất cả key patterns, lấy từ infrastructure-spec.md

DI Registration phải đúng topology — xem ví dụ trong infrastructure-spec.md.
KHÔNG dùng services.AddStackExchangeRedisCache() hay IDistributedCache.

---

## BƯỚC 3 — RabbitMQ

Đọc kỹ mục "Message Queue" trong infrastructure-spec.md.
Dùng đúng thư viện (MassTransit / EasyNetQ / RabbitMQ.Client) như dự án cũ.

Tạo các file:
- `src/POS.Infrastructure/Messaging/IMessagePublisher.cs`
- `src/POS.Infrastructure/Messaging/IMessageConsumer.cs`
- `src/POS.Infrastructure/Messaging/RabbitMqPublisher.cs`
- `src/POS.Infrastructure/Messaging/MessageContracts/` ← các message class

Tạo đúng queue/exchange như danh sách trong infrastructure-spec.md.

---

## BƯỚC 4 — HTTP Clients (External Services)

Với mỗi external service trong infrastructure-spec.md, tạo:
- `src/POS.Infrastructure/ExternalServices/{Name}ApiClient.cs`
  → Interface + Implementation
  → Dùng IHttpClientFactory, có Polly retry + circuit breaker
  → Timeout, auth header đúng như spec

---

## BƯỚC 5 — Serilog

Cấu hình Serilog trong Program.cs với đúng sinks như infrastructure-spec.md.
Thêm enrichers: FromLogContext, MachineName, ThreadId.

---

## BƯỚC 6 — Health Checks

Tạo `src/POS.API/HealthChecks/InfrastructureHealthCheck.cs`
Kiểm tra tất cả: DB ping, Redis ping, RabbitMQ connection.
Đăng ký endpoint: GET /health

---

## BƯỚC 7 — DI Registration tổng hợp

Tạo `src/POS.Infrastructure/DependencyInjection.cs`
với extension method `AddInfrastructureServices(this IServiceCollection, IConfiguration)`
đăng ký tất cả services từ bước 1-5.

---

## SAU KHI XONG TẤT CẢ CÁC BƯỚC:

Liệt kê:
1. Tất cả files đã tạo
2. Tất cả NuGet packages cần cài (tên + version)
3. Các TODO còn lại (nếu có thông tin chưa đủ trong infrastructure-spec.md)
4. Xác nhận: Infrastructure layer đã sẵn sàng để build business logic chưa?
```
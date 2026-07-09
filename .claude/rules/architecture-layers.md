# Kiến trúc Layer & Quy ước phát triển mới (Greenfield)

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

## Quy ước phát triển mới (Greenfield) — BẮT BUỘC

> Mặc định mọi nghiệp vụ là **code mới** — KHÔNG tự ý migrate từ `POS.Backend` (.NET 4.6) cũ
> (source đã xóa khỏi máy). **Ngoại lệ**: các task port cụ thể từ `src/legacy/` (VCM.BLUEPOS)
> theo `.claude/rules/legacy-migration.md` — chỉ áp dụng khi task yêu cầu rõ "port/migrate từ
> code cũ". Hợp đồng JSON với 5.000 máy POS **vẫn giữ nguyên** cho các endpoint hiện hữu.

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

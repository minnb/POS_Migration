# Rule: Clean Architecture — Layer Structure & Dependency Flow

## 🎯 Context (Khi nào áp dụng)
Khi tạo, di chuyển, hoặc quyết định đặt 1 file mới (DTO/Repository/Service/Controller) vào đúng
project trong solution `POS.slnx`.

## ✅ DO (Bắt buộc làm)
- Tuân thủ dependency flow 1 chiều:
  ```
  POS.Api → POS.Application → POS.Infrastructure → POS.Common
  POS.Api → POS.Infrastructure (DI registration)
  POS.Api → POS.Common
  ```
- Đặt đúng project theo bảng:

  | Project | Nội dung |
  |---|---|
  | `POS.Common` | DTOs, Enums, ResultResponse (Domain models) |
  | `POS.Infrastructure` | Repositories, Redis, RabbitMQ (Infrastructure) |
  | `POS.Application` | Services, Interfaces (Application/Business logic) |
  | `POS.Api` | Controllers, Filters (Presentation) |

- **POS.Application**:
  - Namespace `POS.Application.Features.{Domain}` — interface và implementation **cùng namespace,
    cùng folder**.
  - Interface service: `I{Name}Service.cs` trong `Features/{Domain}/`.
  - Implementation: `{Name}Service.cs` trong `Features/{Domain}/` (cùng folder với interface,
    không tách `Interfaces/`/`Services/`).
  - Service chỉ được inject: repository interface (`POS.Infrastructure.Repositories.Interfaces`),
    `IRedisService` (`POS.Infrastructure.Redis`), `IRabbitMQProducer`
    (`POS.Infrastructure.Messaging`), `I{Name}AppService` (`POS.Infrastructure.AppServices.{Domain}`)
    khi cần gọi external HTTP.
  - **Controller BẮT BUỘC inject Application interface** — không inject Infrastructure interface
    trực tiếp.
- **POS.Infrastructure**:
  - Repositories: `src/POS.Infrastructure/Repositories/{Domain}/` — gom theo domain (MasterData,
    Sale, Loyalty, Sap…). **Namespace giữ nguyên** `POS.Infrastructure.Repositories` /
    `POS.Infrastructure.Repositories.Interfaces` (tránh đụng ~20 razor + consumer). Interface
    repository `I{Name}Repository.cs` đặt cùng folder `{Domain}/` với implementation.
  - AppServices (HTTP client wrappers): `src/POS.Infrastructure/AppServices/{Domain}/` — gom theo
    domain (Partner, DataSync…). Namespace `POS.Infrastructure.AppServices.{Domain}` — interface và
    implementation **cùng namespace, cùng folder**. Đặt tên `I{Name}AppService` để phân biệt với
    Application interface.
  - Redis: `src/POS.Infrastructure/Redis/` (`IRedisService`, `RedisService`).
  - Redis internals: `src/POS.Infrastructure/Cache/` (`IRedisManager`, `RedisManager`,
    `RedisOptions`).
  - Messaging: `src/POS.Infrastructure/Messaging/` (`IRabbitMQProducer`, `RabbitMQProducer`).
  - DB Factories: `src/POS.Infrastructure/Database/`.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm Service trong `POS.Application` inject concrete class — chỉ được inject interface.
- Cấm Controller inject thẳng Infrastructure interface (`I{Name}AppService`) — phải qua Application
  interface.
- Cấm tách interface/implementation ra 2 folder riêng (`Interfaces/`/`Services/`) trong
  `POS.Application` — phải cùng folder.
- Cấm đổi namespace `POS.Infrastructure.Repositories`/`.Interfaces` khi di chuyển file theo domain.

---

# Rule: AppService 3-Layer Pattern (External HTTP Client)

## 🎯 Context (Khi nào áp dụng)
Khi tạo mới 1 service gọi external API (GotIT, Urbox, AkaChain, ...) — **BẮT BUỘC** tuân theo
pattern 3 lớp này.

## ✅ DO (Bắt buộc làm)
- Tuân thủ đúng pattern bắt buộc:
  ```
  Controller (POS.Api)
    → inject I{Name}Service              ← POS.Application.Features.{Domain}
      → Application/Features/{Domain}/{Name}Service    (thin wrapper — chỉ delegate, không có logic)
          → inject I{Name}AppService     ← POS.Infrastructure.AppServices.{Domain}
            → Infrastructure/AppServices/{Domain}/{Name}Service  (HTTP client thực sự)
  ```
- Thực hiện đủ 7 bước checklist khi tạo service HTTP client mới, theo đúng thứ tự:
  1. **Infrastructure**: Tạo `I{Name}AppService.cs` trong `AppServices/{Domain}/` — namespace
     `POS.Infrastructure.AppServices.{Domain}`
  2. **Infrastructure**: Tạo `{Name}Service.cs` trong `AppServices/{Domain}/` — implements
     `I{Name}AppService`, cùng namespace với interface
  3. **Infrastructure DI**: Đăng ký `services.AddScoped<I{Name}AppService, {Name}Service>()` trong
     `src/POS.Infrastructure/DependencyInjection.cs`
  4. **Application**: Tạo `I{Name}Service.cs` trong `Features/{Domain}/` — namespace
     `POS.Application.Features.{Domain}`, **cùng signature** với `I{Name}AppService`
  5. **Application**: Tạo `{Name}Service.cs` trong `Features/{Domain}/` — implements
     `I{Name}Service`, inject `I{Name}AppService`, mỗi method chỉ `=> appService.Method(...)`
  6. **Application DI**: Đăng ký `services.AddScoped<I{Name}Service, {Name}Service>()` trong
     `src/POS.Application/DependencyInjection.cs`
  7. **Controller**: Inject `I{Name}Service` (Application) — **KHÔNG** inject `I{Name}AppService`
     (Infrastructure)
- Đặt tên đúng quy tắc: Infrastructure interface `I{Name}**App**Service` (suffix `App` để phân
  biệt); Application interface `I{Name}Service` (không suffix `App`); cả hai implementation class
  đều tên là `{Name}Service` (khác namespace).
- Tham chiếu ví dụ đã có khi tạo service mới:

  | Partner | Application interface | Infrastructure AppService |
  |---|---|---|
  | AkaChain/FMV | `IAkaChainLoyaltyService` | `IAkaChainLoyaltyAppService` / `AkaChainLoyaltyAppService` |
  | GotIT | `IGotITService` | `IGotITAppService` / `GotITService` |
  | Urbox | `IUrboxService` | `IUrboxAppService` / `UrboxService` |

## ❌ DON'T (Tuyệt đối cấm)
- Cấm bỏ qua tầng Application để Controller gọi thẳng Infrastructure AppService.
- Cấm đặt tên Application implementation khác `{Name}Service` (phải trùng tên class Infrastructure,
  chỉ khác namespace).
- Cấm để Application `{Name}Service` chứa business logic — chỉ được delegate nguyên văn sang
  AppService (thin wrapper).

---

# Rule: Greenfield — Tổ chức Feature & Khuôn thêm nghiệp vụ mới

## 🎯 Context (Khi nào áp dụng)
Khi bắt đầu code mới cho 1 nghiệp vụ (mặc định mọi nghiệp vụ là **code mới** — greenfield).
**Ngoại lệ**: các task port cụ thể từ `src/legacy/` (VCM.BLUEPOS) theo
`.claude/rules/legacy-migration.md` — chỉ áp dụng khi task yêu cầu rõ "port/migrate từ code cũ".

## ✅ DO (Bắt buộc làm)
- Đặt code Application mới theo domain: `POS.Application/Features/{Domain}/` (`I{Name}Service.cs`
  + `{Name}Service.cs`). Không để phẳng chung khi tạo domain mới.
- Đặt Repository/AppService mới trong Infrastructure gom theo domain tương ứng.
- Đặt **Business logic** ở `POS.Application` (Service); **I/O** (DB/HTTP/cache) ở
  `POS.Infrastructure`. External HTTP theo **AppService 3 lớp** (xem rule cùng tên ở trên).
- Theo đúng "Khuôn thêm 1 nghiệp vụ mới", tuần tự (mỗi bước có đúng một nơi để đặt file):
  ```
  DTO (POS.Common/Dtos/{Domain}/)
    → Repository/AppService (POS.Infrastructure/.../{Domain}/)
      → Service (POS.Application/Features/{Domain}/)
        → đăng ký DI (DependencyInjection.cs)
          → Controller (POS.Api/Controllers/)
            → contract test cho DTO response + đảm bảo DI test vẫn xanh
  ```
- Chạy `dotnet test` phải xanh sau khi hoàn thành mỗi nghiệp vụ mới.
- Giữ nguyên hợp đồng JSON với 5.000 máy POS cho các endpoint hiện hữu.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm tự ý migrate code từ `POS.Backend` (.NET 4.6) cũ — source đã xóa khỏi máy — trừ khi task yêu
  cầu rõ port từ `src/legacy/`.
- Cấm để code Application mới nằm phẳng (không theo `Features/{Domain}/`) khi tạo domain mới.
- Cấm đổi tên field JSON response hiện hữu khi thêm/sửa nghiệp vụ.

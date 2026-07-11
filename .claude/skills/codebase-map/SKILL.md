---
name: codebase-map
description: Tra cứu nhanh một domain/feature nằm ở đâu trong kiến trúc Clean Architecture của POS_Migration (POS.Common → POS.Infrastructure → POS.Application → POS.Api). Dùng khi cần xác định vị trí DTO, Repository, AppService, Service, Controller của một domain, hoặc khi thêm nghiệp vụ mới theo khuôn chuẩn của dự án.
---

# Codebase Map — POS_Migration

Dùng skill này để **định vị nhanh** một domain (VD: `Loyalty`, `GotIT`, `Sap`, `MasterData`) đang nằm
ở những file/namespace nào, hoặc để biết chính xác phải tạo file mới ở đâu khi thêm nghiệp vụ.

## Bản đồ 4 tầng (dependency flow)

```
POS.Api → POS.Application → POS.Infrastructure → POS.Common
POS.Api → POS.Infrastructure (chỉ để đăng ký DI)
POS.Api → POS.Common
```

| Tầng | Vai trò | Path pattern | Namespace |
|---|---|---|---|
| `POS.Common` | DTO, Enum, ResultResponse (Domain models) | `src/POS.Common/Dtos/{Domain}/` | `POS.Common.Dtos.{Domain}` |
| `POS.Infrastructure` (Repository) | Truy cập DB theo domain | `src/POS.Infrastructure/Repositories/{Domain}/I{Name}Repository.cs` + `{Name}Repository.cs` | `POS.Infrastructure.Repositories` / `...Repositories.Interfaces` (namespace giữ nguyên, KHÔNG đổi theo domain) |
| `POS.Infrastructure` (AppService) | HTTP client gọi external API | `src/POS.Infrastructure/AppServices/{Domain}/I{Name}AppService.cs` + `{Name}Service.cs` | `POS.Infrastructure.AppServices.{Domain}` |
| `POS.Application` | Business logic, thin wrapper | `src/POS.Application/Features/{Domain}/I{Name}Service.cs` + `{Name}Service.cs` | `POS.Application.Features.{Domain}` |
| `POS.Api` | Controller | `src/POS.Api/Controllers/` | inject `I{Name}Service` (Application) — KHÔNG inject AppService/Repository trực tiếp |

Ngoài ra:
- Redis: `src/POS.Infrastructure/Redis/` (`IRedisService`), internals ở `src/POS.Infrastructure/Cache/`
- Messaging: `src/POS.Infrastructure/Messaging/` (`IRabbitMQProducer`)
- Background worker: `src/POS.Infrastructure/Workers/` (namespace `POS.Infrastructure.Workers`), host mỏng ở `POS.Worker/Program.cs`
- Blazor dashboard: `src/POS.Web/Components/Pages/{Store,Ops,Admin}/`

## Lệnh tra cứu domain đã có

```bash
# Liệt kê tất cả domain đã tồn tại ở từng tầng
ls src/POS.Common/Dtos/
ls src/POS.Infrastructure/Repositories/
ls src/POS.Infrastructure/AppServices/
ls src/POS.Application/Features/

# Tìm nhanh mọi file liên quan tới 1 domain cụ thể (VD: GotIT)
find src -type d -iname "GotIT"
grep -rl "namespace POS.*GotIT" src --include="*.cs"

# Tìm interface Application vs Infrastructure của 1 partner (phân biệt bằng suffix App)
grep -rn "interface I.*Service" src/POS.Application/Features/{Domain}/
grep -rn "interface I.*AppService" src/POS.Infrastructure/AppServices/{Domain}/
```

## Checklist "Khuôn thêm 1 nghiệp vụ mới" (đối chiếu CLAUDE.md)

```
1. DTO           → src/POS.Common/Dtos/{Domain}/         (dùng /add-dto-common nếu port từ legacy)
2. Repository    → src/POS.Infrastructure/Repositories/{Domain}/   (I/O nội bộ DB)
   hoặc AppService → src/POS.Infrastructure/AppServices/{Domain}/  (I/O ra external HTTP)
3. Service       → src/POS.Application/Features/{Domain}/  (business logic, inject repo/appservice interface)
4. Đăng ký DI     → src/POS.Application/DependencyInjection.cs (Application)
                  → src/POS.Infrastructure/DependencyInjection.cs (Infrastructure, nếu có AppService/Repository mới)
5. Controller    → src/POS.Api/Controllers/  (chỉ inject I{Name}Service từ Application)
6. Contract test → thêm [Fact] khóa field JSON cho DTO response mới trong
                    tests/POS.ContractTests/JsonFieldContractTests.cs
```

## Bẫy đặt tên hay nhầm

| Đúng | Sai |
|---|---|
| `IGotITService` (Application, không suffix App) | `IGotITAppService` bị inject nhầm vào Controller |
| `IGotITAppService` (Infrastructure, có suffix App) | Đặt nhầm ở `POS.Application` |
| Repository namespace giữ nguyên `POS.Infrastructure.Repositories` dù nằm trong folder domain | Đổi namespace theo folder domain (phá ~20 razor + consumer đang `using` namespace cũ) |

Sau khi xác định xong vị trí, chạy `dotnet test tests/POS.ContractTests` (xem skill `git-workflow`)
trước khi commit để đảm bảo không phá contract/DI hiện có.

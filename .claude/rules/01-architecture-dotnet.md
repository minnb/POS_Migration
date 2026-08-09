---
description: Luật thép Clean Architecture .NET 10, quy ước Database/Stored Procedure, cổng chặn trùng lặp, chống SQL Injection cho POS.Api/POS.Application/POS.Infrastructure/POS.Common
paths: ["src/POS.Api/**", "src/POS.Application/**", "src/POS.Infrastructure/**", "src/POS.Common/**"]
---

# Backend Architecture — .NET 10 Clean Architecture

## Dependency flow (1 chiều, không ngoại lệ)

`POS.Api → POS.Application → POS.Infrastructure → POS.Common` (+ Api→Infrastructure chỉ để đăng
ký DI, Api→Common).

| Project | Nội dung |
|---|---|
| `POS.Common` | DTO, Enum, `ResultResponse` |
| `POS.Infrastructure` | Repository, Redis, RabbitMQ, AppService (HTTP client external) |
| `POS.Application` | Service (business logic), Interface |
| `POS.Api` | Controller, Filter, Middleware |

## Cổng chặn trùng lặp — BẮT BUỘC trước khi tạo DTO/Service/Repository mới

Mở `docs/CURRENT_STRUCTURE.md` tìm trước — đã có (dù khác tên) → **tái dùng**, KHÔNG tạo bản
trùng. Tạo mới → cập nhật file đó **cùng commit**. Chi tiết vị trí layer/namespace/bẫy đặt tên:
skill `codebase-map`.

## AppService 3-layer (gọi external HTTP partner)

`Controller → I{Name}Service (Application, thin wrapper) → I{Name}AppService (Infrastructure,
HTTP client thật)`. Cấm Controller bỏ qua Application gọi thẳng Infrastructure. Cấm hardcode
Base URL/credentials — luôn qua `ICentralMDRepository.GetSysWebApiAsync`. Chi tiết + template
đầy đủ: skill `appservice-scaffold` + `api`.

## POS.Common — Serialization

- **Newtonsoft.Json BẮT BUỘC** — cấm `System.Text.Json`/`[JsonPropertyName]`/`JsonElement` dưới
  mọi hình thức.
- `[JsonProperty("x")]` khi tên C# khác tên field JSON.
- **Cấm đổi tên field JSON của DTO response hiện hữu** — 5.000 máy POS parse theo đúng tên hiện
  tại, đổi bất kỳ field nào sẽ phá vỡ production ngay lập tức.

## Controller

- Mọi `I{Name}Service` mới → đăng ký `services.AddScoped<I{Name}Service, {Name}Service>()`
  ngay trong `DependencyInjection.cs` cùng lúc tạo — quên đăng ký = lỗi lúc runtime, không phải
  lúc build.
- Trả `ResultResponse` → `return StatusCode((int)result.Status, result);`. **Cấm**
  `OkResult(result)` khi `result` đã là `ResultResponse` (double-nest).
- `NullValueHandling.Ignore` + `SuppressModelStateInvalidFilter=true` là hành vi cố định của
  pipeline — cấm đổi.
- Swagger chỉ bật khi `IsDevelopment()` — cấm bật ở UAT/PROD.

## 3 vành đai Guardrail — BẮT BUỘC `dotnet test tests/POS.ContractTests` xanh trước mọi commit

1. **Contract test khóa field JSON** — DTO response mới → thêm `[Fact] {Dto}_locked`. Chi tiết
   quy trình: skill `contract-test-guardian`.
2. **DI validation test** — chặn quên đăng ký DI, đọc descriptor (không cần Redis/SQL/Rabbit).
3. **`ExceptionHandlingMiddleware`** — lưới an toàn global. Cấm gỡ; cấm thêm try/catch trong
   controller chỉ để format lỗi chung (middleware đã lo).

## Database / Stored Procedure (RPOSMasterData / RPOSCentralSales / RPOSLoyalty)

- **Reserved keyword** (`[LineNo]`, `[Source]`, `[Status]`, `[Counter]`, `[No]`...) → BẮT BUỘC
  bracket-quote `[ ]`. Lỗi Msg 156 "Incorrect syntax near keyword" mà cột đó **có tồn tại thật**
  = dấu hiệu reserved keyword (khác Msg 207 = tên cột sai).
- Naming SP mới: `dbo.usp_{Domain}_{Action}` + TVP `dbo.{Name}TVP` — SP legacy tên cũ giữ nguyên,
  không đổi để khớp convention.
- **Không suy đoán tên bảng/cột** — tra `docs/architecture/{centralMD,centralsale,loyalty}-schema.md`
  trước; chưa có → đọc script gốc `docs/sql/database/*.sql` rồi bổ sung cùng commit. Cấm tự
  thêm/bớt "s" theo thói quen đặt tên DbSet.
- SP ghi dữ liệu BẮT BUỘC: `SET XACT_ABORT ON` + `TRY/CATCH` + `ROLLBACK TRANSACTION` khi lỗi +
  `THROW` — cấm nuốt lỗi trong SP.
- Cột `Counter bigint` (đồng bộ POS): `MAX(Counter)+1` PHẢI tính **trong 1 SP** với
  `SELECT ... WITH (UPDLOCK, HOLDLOCK)` cùng transaction với `UPDATE` — cấm tính ở tầng C# rồi
  UPDATE riêng (race condition đa request).
- **Chống SQL Injection**: mọi câu SQL/SP call BẮT BUỘC dùng parameterized query (Dapper
  `DynamicParameters`/anonymous object) — cấm nối chuỗi giá trị đến từ request vào SQL text dưới
  bất kỳ hình thức nào.
- Mọi SP mới BẮT BUỘC đăng ký `docs/sql/manifest.json` cùng commit — thiếu → `SqlManifestTests`
  FAIL. Chi tiết template SP, TVP, Single File Constraint, `order` phụ thuộc: skill `database`.

## Chi tiết đầy đủ (HOW) — đọc đúng skill khi cần, KHÔNG suy đoán

| Chủ đề | Skill |
|---|---|
| Vị trí file theo domain, bẫy đặt tên `I{Name}Service` vs `I{Name}AppService` | `codebase-map` |
| Tạo AppService gọi external API mới (GotIT/Urbox/AkaChain-style) | `appservice-scaffold` + `api` |
| Viết SP mới / pattern Repository (UPDLOCK, OUTPUT param, timeline merge...) | `database` |
| Cache Redis (key convention, TTL, lock, throttle) | `cache` |
| Logging (3 cơ chế, khi nào dùng cái nào) | `api` (`logging.md`) |
| Worker / BackgroundService | `worker` |
| Port chức năng từ `src/legacy/` (VCM.BLUEPOS) | `migration` |
| Unit test service Application (Payment...) | `payment-test-generator` |
| Audit coverage contract JSON trước commit | `contract-test-guardian` |
| Git/commit/PR | `git-workflow` |

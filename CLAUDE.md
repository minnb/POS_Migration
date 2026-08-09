# POS API — Claude Code Context

## QUY TẮC GIAO TIẾP VÀ BÁO CÁO (BẮT BUỘC TUÂN THỦ)

> Áp dụng cho **toàn bộ** tương tác, mọi task, không có ngoại lệ.

1. **TRƯỚC KHI BÁO XONG**: Bắt buộc phải đưa ra kết quả cụ thể, trích xuất output, hoặc log
   thực tế để chứng minh rằng công việc đã thực sự hoàn thành và chạy được.
2. **CHỈ BÁO CÁO DỰA TRÊN BẰNG CHỨNG**: Chỉ thông báo những việc mà bạn có thể cung cấp bằng
   chứng rõ ràng. Không báo cáo "đã sửa xong" nếu chỉ mới gõ code mà chưa kiểm chứng.
3. **TRUNG THỰC KHI CHƯA VERIFY**: Nếu không thể tự verify được kết quả (do thiếu môi trường,
   database, quyền truy cập...), HÃY NÓI THẲNG LÀ CHƯA VERIFY ĐƯỢC. Tuyệt đối không tự chẩn
   đoán mò, không đoán bừa kết quả.

## Dự án

POS API trên **.NET 10** (Clean Architecture) phục vụ ~5.000 máy POS.
- Solution: `POS.slnx`

> **Greenfield + Migration có chủ đích**: dự án khởi đầu là bản port từ POS.API (.NET Framework
> 4.6) rồi chuyển sang phát triển mới, ngừng migrate, và xóa hẳn source cũ khỏi máy. Từ
> **2026-07-03**, source cũ **VCM.BLUEPOS** (.NET Framework 4.6) được **tích hợp lại vào
> `src/legacy/`** làm tài liệu tham chiếu **CHỈ ĐỌC** để port một số chức năng nghiệp vụ cụ thể
> sang kiến trúc mới — xem `.claude/rules/legacy-migration.md`. Ngoài phạm vi các task migration
> được giao rõ, mặc định vẫn code mới (greenfield). Hợp đồng JSON với 5.000 máy POS vẫn giữ
> nguyên cho các endpoint hiện hữu.

Cấu trúc solution (chi tiết đầy đủ: `.claude/rules/architecture-layers.md`):

```
src/
├── POS.Common/          DTOs, Enums, ResultResponse  (Domain models)
├── POS.Infrastructure/  Repositories, Redis, RabbitMQ (Infrastructure)
├── POS.Application/     Services, Interfaces          (Application/Business logic)
└── POS.Api/             Controllers, Filters          (Presentation)
```

Dependency flow: `POS.Api → POS.Application → POS.Infrastructure → POS.Common`.

## MỤC LỤC ĐIỀU PHỐI (Router Index) — đọc đúng file trước khi làm việc

> **Nguyên tắc**: KHÔNG suy đoán cấu trúc/schema/pattern. Tìm đúng file dưới đây trước, chỉ khi
> chưa có mới tự quyết định — và phải cập nhật đúng file nguồn trong cùng commit.

| Khi cần… | Đọc file |
|---|---|
| Tra DTO / Service / Repository / Helper đã có + chữ ký method + bảng DI | **`docs/CURRENT_STRUCTURE.md`** (nguồn sự thật cấu trúc code C#) |
| Cấu trúc Clean Architecture, quy tắc POS.Application/Infrastructure, pattern AppService 3 lớp, quy ước Greenfield | **`.claude/rules/architecture-layers.md`** |
| Quy tắc POS.Common (serialization Newtonsoft, cấm đổi field JSON), quy tắc Controller (DI/ModelState/NullValueHandling/return type), 3 vành đai Guardrails & Testing | **`.claude/rules/backend-api-rules.md`** |
| Port/migrate chức năng từ `src/legacy/` (VCM.BLUEPOS) | **`.claude/rules/legacy-migration.md`** (đọc TRƯỚC khi mở `src/legacy/`) + `docs/migrations/MIGRATION_MAP.md` (bảng ánh xạ cũ→mới) |
| Tính năng sinh file master data .zip cho POS (Sync Master Data) | **`.claude/rules/masterdata-sync.md`** |
| Viết query/SP/Repository đụng bảng `RPOSMasterData` (CentralMD) — tên bảng/cột/kiểu dữ liệu/PK | **`docs/architecture/centralMD-schema.md`** (gồm business rules bảng `dbo.Store`, xem thêm `.claude/rules/database-standards.md`) |
| Viết query/SP/Repository đụng bảng `RPOSCentralSales` (giao dịch bán hàng, ca/shift, EOD, void, bonus/point/coupon-voucher) | **`docs/architecture/centralsale-schema.md`** |
| Viết query/SP/Repository đụng bảng `RPOSLoyalty` (log giao dịch loyalty) | **`docs/architecture/loyalty-schema.md`** |
| Tạo mới / sửa / refactor / fix bug stored procedure cho `RPOSMasterData` / `RPOSCentralSales` / `RPOSLoyalty` | **`.claude/rules/database-standards.md`** (LUẬT: naming `usp_{Domain}_{Action}`, TVP, reserved-keyword, **Single File Constraint**, XACT_ABORT, Counter/UPDLOCK, manifest.json) + **`.claude/skills/database/SKILLS.md`** (HOW: template SP, Dapper call, các Pattern) — đọc cả 2 |
| Deploy/vận hành `tools/POS.DbMigrator` (Ubuntu bare-metal, Docker, CLI args, troubleshooting) | **`docs/deploy/pos-dbmigrator-guide.md`** |
| Cấu hình Health Check `/ops/health` khi deploy POS.Web (section `HealthCheck`/`WorkerHeartbeat`, `PosApiBaseUrl` theo môi trường) | **`docs/deploy/web-health-guide.md`** |
| Tích hợp API đối tác ngoài (Loyalty/AkaChain, GotIT, Urbox...) qua config DB `SysWebApi`/`SysWebApiRoute` + cache | **`.claude/rules/external-api-integration.md`** + `.claude/skills/api/SKILLS.md` (đọc cả 2 — rule là luật bắt buộc, skill là template/checklist chi tiết) |
| Thêm cache Redis StandAlone (key convention, TTL, pattern Hash/String) | **`.claude/rules/caching-standards.md`** (LUẬT: key convention, TTL, phân tầng) + **`.claude/skills/cache/SKILLS.md`** (HOW: API + Pattern 1–8) |
| Thêm scheduled job / message consumer trong `POS.Worker` | **`.claude/rules/worker-standards.md`** (LUẬT: thin-host, 8 luật, anti-pattern, heartbeat) + **`.claude/skills/worker/SKILLS.md`** (HOW: 4 khuôn mẫu + templates.md) |
| Logging POS.Api/Infrastructure (chọn IFileLogHelper/IKibanaService/middleware) | **`.claude/rules/logging-standards.md`** (LUẬT: chọn cơ chế, anti-pattern, config) + **`.claude/skills/api/logging.md`** (HOW: signature, code, config chi tiết) |
| Deploy/vận hành `POS.Worker` (Docker, cron Ubuntu, Task Scheduler Windows, health check) | **`docs/worker/worker_status.md`** |
| Kiểm tra contract JSON với 5.000 POS | `docs/API_CONTRACT.md` + `tests/POS.ContractTests/` |
| Cách thêm DTO mới | `.claude/commands/add-dto-common.md` (skill `/add-dto-common`) |
| Tra quy tắc mã hóa credentials appsettings (`enc:` / `POS_SECRET_KEY`) | **`docs/architecture/appsetting.md`** |
| Trạng thái / lịch sử POS.Web | `docs/WEB_STATUS.md`, `docs/CHANGELOG.md` |
| Luồng nghiệp vụ module Chương trình khuyến mãi (Offer/Setup CTKM) | **`docs/web/logic/promotion_technical_spec.md`** |
| Viết bất kỳ page/component UI mới trong `src/POS.Web/` — auth, roles, template page, responsive, density, performance, DataTable/cột/DateTime, component mapping, audit log | **`.claude/rules/blazor-web-app.md`** (LUẬT nền tảng, §17 gộp từ lớp web numbered cũ) + **`.claude/skills/web/SKILLS.md`** (index skill con: form-input/filter-store/datatable/charts/reports/component-patterns) |
| Theme/màu/Input/Button/Card/Elevation/Sidebar MudBlazor (mapping mockup → component) — **LUẬT THÉP mọi UI mới** | **`.claude/rules/mudblazor-flat-ui.md`** |
| Làm đẹp/đồng bộ UI trang đã có (chỉ sửa markup, giữ `@code`) | **`.claude/skills/web/ui-polish-standard.md`** |
| Page có thao tác Create/Update/Delete cần audit log | **`.claude/skills/web/audit-logging.md`** |
| Viết file phân tích nghiệp vụ trước khi port (`FEATURE_{Name}_ANALYSIS.md`) | **`.claude/skills/migration/SKILLS.md`** |
| Định kỳ dọn dẹp/refactor file `.claude/rules/` hoặc `.claude/skills/` khi đã phình to (tách lịch sử, gộp trùng lặp, tách sub-skill) | **`.claude/commands/refactor-skills.md`** (skill `/refactor-skills`) |
| Sinh unit test (xUnit + Moq + FluentAssertions) cho luồng Payment / service tầng Application | **`.claude/rules/unit-testing-standards.md`** (LUẬT: Nguyên tắc Mock, naming) + **`.claude/skills/payment-test-generator/SKILL.md`** (HOW: setup, template; skill `/payment-test-generator`, test ở `tests/POS.UnitTests/`) |
| Bật/dùng MCP server (SQL read-only, Redis) để debug dữ liệu/cache + cách chạy unit test | **`docs/mcp/step-by-step-mcp-guide.md`** (config `.mcp.json` dùng `${VAR}`; secret ở `.claude/settings.local.json` gitignored — KHÔNG hardcode) |
| Tạo mới / sửa / tối ưu 1 skill hoặc rule (đúng chuẩn Agent Skill spec của Anthropic, chạy eval/benchmark description) | **`.claude/skills/skill-creator/SKILL.md`** (official Anthropic skill) — dùng khi soạn/refactor skill trong `.claude/skills/`; bổ trợ `/refactor-skills` |
| **Tự viết MCP server mới** (Python FastMCP / Node TS SDK) cho POS — vd wrap CentralMD/CentralSale/Redis thành tool | **`.claude/skills/mcp-builder/SKILL.md`** (official) — HOW build server; khác `docs/mcp/step-by-step-mcp-guide.md` (chỉ *cấu hình* server sẵn có) |
| Đọc/sinh/sửa file Excel (.xlsx/.csv): làm báo cáo, đối soát, làm sạch dữ liệu tabular trong phiên dev | **`.claude/skills/xlsx/SKILL.md`** (official) — ⚠️ skill Python (openpyxl) thao tác tài liệu, **KHÔNG** phải thư viện C# runtime cho POS.Api/POS.Web |
| Đọc/trích xuất/tạo/merge/split/điền form/OCR file PDF (hóa đơn, biên lai, báo cáo) trong phiên dev | **`.claude/skills/pdf/SKILL.md`** (official) — ⚠️ skill Python thao tác tài liệu, **KHÔNG** phải thư viện C# runtime |
| Test end-to-end page Blazor POS.Web bằng browser automation (Playwright): verify UI, chụp screenshot, xem log browser | **`.claude/skills/webapp-testing/SKILL.md`** (official) — bổ sung hướng test UI thật, khác `tests/POS.ContractTests`/`tests/POS.UnitTests` (xUnit) |
| Đang làm dở gì / bước tiếp theo là gì (bàn giao ca giữa các phiên) | **`COORDINATION.md`** (xem mục "Bàn giao ca" cuối file này) |

### Cổng chặn trùng lặp (BẮT BUỘC theo thứ tự)

1. **TRƯỚC khi tạo DTO / Service / Repository / Helper mới** → mở `docs/CURRENT_STRUCTURE.md`,
   tìm ở mục tương ứng (MỤC A cây DTO, MỤC B interface, MỤC D/E chữ ký method, MỤC C DI).
2. **Đã tồn tại** (dù khác tên) → **TÁI DÙNG**, KHÔNG tạo bản trùng. Cần bổ sung → thêm method
   vào interface đã có.
3. **Chưa có** → tạo theo đúng quy ước layer (`.claude/rules/architecture-layers.md`), rồi **cập
   nhật `docs/CURRENT_STRUCTURE.md` trong CÙNG commit** (thêm dòng vào cây/bảng tương ứng — chỉ
   tên class + property/field chính + chữ ký + project chứa nó, **KHÔNG chép nguyên code**). Dùng
   skill `/task-done` để cập nhật doc.
4. **Không chắc** một DTO/Service đã tồn tại chưa → tìm trong `docs/CURRENT_STRUCTURE.md` trước,
   sau đó Grep codebase; **KHÔNG** đoán rồi tạo mới.
5. **TRƯỚC khi viết SQL query / stored procedure / Repository method đụng tới bảng DB** → mở đúng
   file schema tương ứng (bảng router ở trên), lấy đúng tên bảng/cột/kiểu dữ liệu/PK. **KHÔNG
   suy đoán tên cột.** Bảng cần dùng chưa có trong doc tương ứng → đọc lại script gốc trong
   `docs/sql/database/` (CentralMD.sql / CentralSale.sql / Loyalty.sql — 2 file sau lưu ở
   **UTF-16**, đọc bằng PowerShell `Get-Content -Encoding Unicode -Raw`), rồi bổ sung vào đúng
   file schema trong cùng commit.
6. Tương tự, mọi page/component UI mới trong `src/POS.Web/` phải qua "LUẬT THÉP" ở
   `.claude/rules/blazor-web-app.md` §0 trước khi viết markup.

> Giữ `docs/CURRENT_STRUCTURE.md` đồng bộ với code, và 3 file `docs/architecture/{database,
> centralsale,loyalty}-schema.md` đồng bộ với script DB tương ứng, là **một phần của định nghĩa
> "xong"** cho mọi task liên quan. Doc lệch = lần sau AI tạo trùng.

### Ngoại lệ đã chốt (không tạo file trùng mục đích)

- `docs/architecture/{database,centralsale,loyalty}-schema.md` — nguồn sự thật cho **schema DB**
  (khác `docs/CURRENT_STRUCTURE.md` — schema DB vs cấu trúc code C#).
- `.claude/rules/mudblazor-flat-ui.md` — nguồn sự thật cho **theme/UI pattern MudBlazor**, không
  lặp lại nội dung này ở `.claude/rules/blazor-web-app.md` hay nơi khác.

## Lệnh hệ thống nhanh (Slash Commands)

> Chi tiết cách dùng nằm trong từng file `.claude/commands/*.md` hoặc `.claude/skills/*/SKILL*.md`
> tương ứng — bảng dưới đây chỉ để tra nhanh có lệnh gì, không lặp lại nội dung chi tiết.

| Lệnh | Mục đích |
|---|---|
| `/task-resume` | Khôi phục context sau khi phiên bị gián đoạn (đọc `docs/CHANGELOG.md` + quét TODO còn dở) |
| `/task-done` | Cập nhật tài liệu (`docs/CURRENT_STRUCTURE.md`...) sau khi hoàn thành task |
| `/task-review` | Rà soát code của task vừa làm trong phiên (chỉ đọc, KHÔNG sửa) — `.claude/commands/task-review.md` (thay `/review-task` cũ đã gộp) |
| `/refactor-skills` | Dọn dẹp/refactor định kỳ file `.claude/rules/`/`.claude/skills/` khi phình to |
| `/add-dto-common` | Thêm DTO mới vào `src/POS.Common/` |
| `/payment-test-generator` | Sinh unit test (xUnit+Moq+FluentAssertions) cho luồng Payment / service Application |
| `/blazor-ui [feature\|chart\|table\|kpi\|dialog\|grid]` | Tạo page mới hoàn chỉnh (page + service + model) hoặc chèn component UI (chart / data table / KPI row / confirm dialog / POS status grid) vào page POS.Web đã có — `.claude/skills/blazor-ui/` (gộp 6 lệnh `/web-add-feature` + `/web-ui-*` cũ) |
| `/web-ops [check-status\|gen-hash]` | Build + audit trạng thái POS.Web; tạo BCrypt hash cho SQL khởi tạo user dashboard — `.claude/skills/web-ops/` (gộp `/web-check-status` + `/web-gen-hash` cũ) |

## Bàn giao ca

> **BẮT BUỘC** cập nhật `COORDINATION.md` (ở gốc repo) trước khi kết thúc một task — đây là nguồn
> sự thật cho "đang làm gì / còn thiếu gì / bước tiếp theo là gì" giữa các phiên làm việc. Khác
> `docs/migrations/STATUS.md` (chỉ theo dõi riêng tiến độ port từng feature từ `src/legacy/`).

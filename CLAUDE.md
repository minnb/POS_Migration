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
| Viết query/SP/Repository đụng bảng `RPOSMasterData` (CentralMD) — tên bảng/cột/kiểu dữ liệu/PK | **`docs/architecture/centralMD-schema.md`** (gồm business rules bảng `dbo.Store`) |
| Viết query/SP/Repository đụng bảng `RPOSCentralSales` (giao dịch bán hàng, ca/shift, EOD, void, bonus/point/coupon-voucher) | **`docs/architecture/centralsale-schema.md`** |
| Viết query/SP/Repository đụng bảng `RPOSLoyalty` (log giao dịch loyalty) | **`docs/architecture/loyalty-schema.md`** |
| Tạo mới / sửa / refactor / fix bug stored procedure cho `RPOSMasterData` / `RPOSCentralSales` / `RPOSLoyalty` | **`.claude/skills/database/SKILLS.md`** (quy tắc đặt tên `usp_{Domain}_{Action}`, TVP, template SP, **Single File Constraint** khi sửa SP đã tồn tại) — script CentralMD mới **BẮT BUỘC** đăng ký vào `docs/sql/manifest.json` cùng commit (`tools/POS.DbMigrator`, xem `docs/ROLLOUT.md` §D0) |
| Deploy/vận hành `tools/POS.DbMigrator` (Ubuntu bare-metal, Docker, CLI args, troubleshooting) | **`docs/deploy/pos-dbmigrator-guide.md`** |
| Cấu hình Health Check `/ops/health` khi deploy POS.Web (section `HealthCheck`/`WorkerHeartbeat`, `PosApiBaseUrl` theo môi trường) | **`docs/deploy/web-health-guide.md`** |
| Tích hợp API đối tác ngoài (Loyalty/AkaChain, GotIT, Urbox...) qua config DB `SysWebApi`/`SysWebApiRoute` + cache | **`.claude/rules/external-api-integration.md`** + `.claude/skills/api/SKILLS.md` (đọc cả 2 — rule là luật bắt buộc, skill là template/checklist chi tiết) |
| Thêm cache Redis StandAlone (key convention, TTL, pattern Hash/String) | **`.claude/skills/cache/SKILLS.md`** |
| Thêm scheduled job / message consumer trong `POS.Worker` | **`.claude/skills/worker/SKILLS.md`** |
| Deploy/vận hành `POS.Worker` (Docker, cron Ubuntu, Task Scheduler Windows, health check) | **`docs/worker/worker_status.md`** |
| Kiểm tra contract JSON với 5.000 POS | `docs/API_CONTRACT.md` + `tests/POS.ContractTests/` |
| Cách thêm DTO mới | `.claude/commands/add-dto-common.md` (skill `/add-dto-common`) |
| Tra quy tắc mã hóa credentials appsettings (`enc:` / `POS_SECRET_KEY`) | **`docs/architecture/appsetting.md`** |
| Trạng thái / lịch sử POS.Web | `docs/WEB_STATUS.md`, `docs/CHANGELOG.md` |
| Luồng nghiệp vụ module Chương trình khuyến mãi (Offer/Setup CTKM) | **`docs/web/logic/promotion_technical_spec.md`** |
| Viết bất kỳ page/component UI mới trong `src/POS.Web/` — auth, roles, template page, responsive, density, audit log | **`.claude/rules/blazor-web-app.md`** + **`.claude/skills/web/SKILLS.md`** (đọc cả 2 — SKILLS.md có index các skill con: form-input/filter-store/datatable/charts/reports) |
| Theme/màu/Input/Button/Card/Elevation/Sidebar MudBlazor (mapping mockup → component) — **LUẬT THÉP mọi UI mới** | **`.claude/rules/mudblazor-flat-ui.md`** |
| Làm đẹp/đồng bộ UI trang đã có (chỉ sửa markup, giữ `@code`) | **`.claude/skills/web/ui-polish-standard.md`** |
| Page có thao tác Create/Update/Delete cần audit log | **`.claude/skills/web/audit-logging.md`** |
| Viết file phân tích nghiệp vụ trước khi port (`FEATURE_{Name}_ANALYSIS.md`) | **`.claude/skills/migration/SKILLS.md`** |
| Định kỳ dọn dẹp/refactor file `.claude/rules/` hoặc `.claude/skills/` khi đã phình to (tách lịch sử, gộp trùng lặp, tách sub-skill) | **`.claude/commands/refactor-skills.md`** (skill `/refactor-skills`) |

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

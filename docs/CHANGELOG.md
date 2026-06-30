# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.
>
> **Lưu ý định hướng (2026-06-26):** dự án **không còn migrate** từ POS.API (.NET 4.6 /
> `POS.Backend`) — nay **phát triển mới (greenfield)**. Các entry cũ có từ "migrate"/"Migrated"
> là ghi chép lịch sử tại thời điểm đó, giữ nguyên để tra cứu.

## [2026-06-30] Xác thực request từ POS — PosApiKeyMiddleware (X-API key)

**Layer:** POS.Api
**Loại:** Feature + Pattern mới (Security)

**Thay đổi:**
- `src/POS.Api/Middleware/PosApiKeyMiddleware.cs` (MỚI): middleware validate header `X-API` = MD5(privateKey).ToUpper(); privateKey lấy từ `GetPOSDataSetupAsync()` (Redis cache `MD:POSDataSetup` 12h). Fail-closed: thiếu cả `X-API` lẫn `Authorization` → 401 "Chưa xác thực". Miễn `/health` + `/swagger/*`.
- `src/POS.Api/Program.cs`: thêm `app.UsePosApiKeyAuth()` sau `UseSerilogRequestLogging()`, trước `UseAuthentication()`.

**Pattern mới:** Middleware xác thực X-API (scoped service qua tham số InvokeAsync) → đã cập nhật `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- Scoped service (`ICentralMDRepository`, `IFileLogHelper`) nhận qua THAM SỐ `InvokeAsync`, KHÔNG inject constructor (middleware là singleton).
- `MD5.HashData()` + `Convert.ToHexString()` cho uppercase hex khớp `MD5(...).toUpper()` phía POS.
- ⚠️ Fail-closed: mọi endpoint (trừ `/health`, `/swagger/*`) bắt buộc có `X-API` hoặc `Authorization` — rà soát monitor/script nội bộ trước khi deploy PROD.
- Bearer token validate vẫn **pending** (hiện pass-through nếu có header Authorization).
- Build xanh, DI test xanh (không thêm DI mới — dùng service đã đăng ký).

---

## [2026-06-29] Tối ưu hóa GetFileFromFTP (typeSync=ALL): Parallel + SHA-256 + Redis SP1 cache

**Layer:** POS.Infrastructure, POS.Application, POS.Api
**Loại:** Performance optimization + Security

**Thay đổi:**
- `src/POS.Infrastructure/Files/MasterDataSyncOptions.cs`: thêm `MaxParallelTables = 4` (số bảng SP2 chạy song song)
- `src/POS.Api/appsettings.json`: thêm `MaxParallelTables: 4` vào section `MasterDataSync`
- `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`: thay `foreach` tuần tự → `Parallel.ForEachAsync` (4×); thêm SHA-256 companion file sau atomic publish; xóa `.sha256` cùng zip khi cleanup
- `src/POS.Infrastructure/Repositories/DataSync/SyncRepository.cs`: inject `IRedisService`, cache SP1 metadata (key `MD:SyncTableList`, TTL 3600s)
- `docs/ROLLOUT.md`: cập nhật O1 với Ubuntu/nginx guidance, SHA-256 info, Redis key invalidation
- `CLAUDE.md`: cập nhật section Sync Master Data (MaxParallelTables, SHA-256, Redis SP1)

**Pattern mới:** Parallel.ForEachAsync + SHA-256 companion → đã cập nhật `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- `Parallel.ForEachAsync` an toàn khi mỗi iteration mở `SqlConnection` riêng (không shared state). Precompute `tableIndex` qua `Select((t, idx) => ...)` trước khi parallel để index ổn định.
- File `.sha256` là companion không đưa vào response API (filter `*.zip` không khớp), POS không biết đến nó — xóa cùng zip khi cleanup.
- `MD:SyncTableList` TTL 1h: nếu DBA đổi bảng SyncTableList cần hiệu lực ngay → `DEL MD:SyncTableList` trên Redis.

---

## [2026-06-28] Phase 1+2: IKibanaService → IFileLogHelper + Audit Logging UsersPage & PosMapPage

**Layer:** POS.Web
**Loại:** Refactor (Phase 1) + Feature (Phase 2)

**Thay đổi:**

Phase 1 — Thay toàn bộ IKibanaService bằng IFileLogHelper trong POS.Web:
- `Auth/IAuditLogger.cs` (`DbAuditLogger`): constructor thay `IKibanaService` → `IFileLogHelper`
- `Services/PendingUpdate.cs`: thay 3 Kibana call → FileLogger
- `Services/SqlConsoleService.cs`: thay 4 Kibana call → FileLogger
- 24 `.razor` files (Ops, Admin, Store): thay `@inject IKibanaService KibanaService` → `@inject IFileLogHelper FileLogger`, tất cả call site. Mapping: `LogInfo(fn,e,m)` → `WriteLogs("[{fn}] {e}: {m}")`; `LogException` có ex → `WriteExpLogs(fn, ex)`; `LogException` không có ex → `WriteLogs("[EXCEPTION][fn] msg")`

Phase 2 — Audit logging cho UsersPage và PosMapPage:
- `Admin/Dialogs/UserFormDialog.razor`: `Submit()` trả `DialogResult.Ok(savedUser!)` thay `Ok(true)`; `PasswordHash = string.Empty` để mask hash trước khi serialize
- `Admin/UsersPage.razor`: inject `IAuditLogger`, `AuthState`, `_currentActor`; log `CREATE`/`UPDATE` trong `OpenDialogAsync`; log `LOCK`/`UNLOCK` trong `ConfirmToggleAsync`
- `Ops/Dialogs/PosTerminalEditDialog.razor`: trả `Ok(new PosTerminalSavePayload(...))` thay `Ok(true)`
- `Ops/Dialogs/PosTerminalDetailDialog.razor`: `OpenEditAsync()` forward `result.Data!` thay `Ok(true)` — chained dialog pattern
- `Ops/PosMapPage.razor`: inject `IAuditLogger`, capture `oldJson` trước dialog, log `UPDATE PosTerminal` khi edit thành công
- `Ops/PosTerminalSavePayload.cs` **(mới)**: `record PosTerminalSavePayload(IpAddress, IsEnabled, BillNoseri)` — shared type dùng cho chain dialog forwarding

**Pattern mới:** Chained dialog result forwarding → đã cập nhật `.claude/skills/web/audit-logging.md` (§11)

**Lưu ý cho session sau:**
- Khi dialog lồng nhiều tầng (ViewDialog → EditDialog), dùng shared record + `result.Data!` để forward nguyên payload — không Ok(true)
- `IKibanaService` vẫn còn trong DI (dùng bởi POS.Api/Worker) — chỉ xóa usages trong POS.Web, KHÔNG xóa service registration

---

## [2026-06-28] POSDataSetup CRUD page + Audit Log DB-persistent

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Feature mới + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/POS/Common/CommonDtos.cs`: thêm `POSDataSetupAdminDto` (5 cột: Code, Value, Description, StoreNo, Counter) — tách riêng với `POSDataSetupModel` (contract POS machine, giữ nguyên)
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: thêm 5 CRUD method (`GetPOSDataSetupAdminListAsync`, `GetPOSDataSetupByCodeAsync`, `InsertPOSDataSetupAsync`, `UpdatePOSDataSetupAsync`, `DeletePOSDataSetupAsync`) + `InsertDashboardAuditLogAsync` (ghi audit, try/catch nội bộ)
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement đủ 6 method trên; mọi write invalidate Redis key `MD:POSDataSetup`; UpdatePOSDataSetupAsync KHÔNG đụng Counter/Pkey
- `src/POS.Web/Auth/IAuditLogger.cs`: interface `LogAsync(actor, action, entityType, entityKey, oldValueJson?, newValueJson?)` + impl `DbAuditLogger` (ghi `DashboardAuditLog` qua repository + Kibana song song)
- `src/POS.Web/Auth/migration_dashboard_audit_log.sql`: CREATE TABLE `DashboardAuditLog` + 3 index (ActedAt, Actor, EntityType+EntityKey) — idempotent, chạy trên RPOSMasterData trước khi deploy
- `src/POS.Web/Program.cs`: đăng ký `AddScoped<IAuditLogger, DbAuditLogger>()`
- `src/POS.Web/Components/Pages/Ops/Dialogs/PosDataSetupFormDialog.razor` **(mới)**: Add/Edit dialog — Code read-only khi Edit; `DialogResult.Ok(_model)` (DTO đầy đủ, không Ok(true))
- `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor` **(mới)**: `/ops/pos-data-setup` (OpsAndAbove) — KPI 3 cards (pre-computed, không LINQ inline), filter + MudTable + Delete confirm; log đủ 3 CRUD với oldValue/newValue JSON
- `src/POS.Web/Components/Layout/MainLayout.razor`: thêm nav link "POS Data Setup" vào Ops → Cấu hình group
- `.claude/skills/web/audit-logging.md` **(mới)**: rule chuẩn hóa audit log CRUD toàn dự án — 10 section, checklist 12 mục, reference impl, mask nhạy cảm
- `.claude/skills/web/SKILLS.md`: thêm rule #4 bắt buộc đọc audit-logging.md khi có CRUD
- `CLAUDE.md`: thêm Section 16 (Audit Log — rule mandatory + 3 điểm KHÔNG làm)

**Pattern mới:** Audit Log CRUD pattern → đã cập nhật `.claude/skills/web/audit-logging.md`

**Lưu ý cho session sau:**
- `migration_dashboard_audit_log.sql` **chưa chạy trên DB** — phải chạy trước khi test audit trên môi trường thật (thiếu bảng → log fail silently, không crash app)
- Snapshot oldValue cho UPDATE: dùng biến `item` đã có trong page, KHÔNG fetch lại DB
- Dialog phải trả `DialogResult.Ok(_model)` (không `Ok(true)`) để page có newValue cho CREATE/UPDATE audit
- Khi mở rộng audit sang page khác (Users, Stores...): chỉ inject `IAuditLogger`, không cần thêm DI — đã đăng ký global

---

## [2026-06-28] Đồng bộ tài liệu với reorg theo domain

**Layer:** docs
**Loại:** Tài liệu

**Thay đổi:**
- `docs/CURRENT_STRUCTURE.md`: Mục A cây thư mục — Application `Interfaces/`+`Services/` → `Features/{Domain}/`; Infrastructure `Repositories/{MasterData,Sale,Loyalty,Sap}` + `AppServices/{Partner,DataSync}` + `Security/`; thêm `CentralSaleConnectionFactory`, Workers (Heartbeat/HealthState/RptInsert), `ExceptionHandlingMiddleware`, Gift/Winpay controller. Mục B/C: namespace `Features.{Domain}` / `AppServices.{Domain}` (repo giữ nguyên), thêm DI `ISAPService/IGiftService/ISAPVoucherRepository/IRptCentralSaleRepository/IRptReportSaleDetailRepository/CentralSaleConnectionFactory`. Mục E: ghi chú namespace mới
- `docs/WEB_STATUS.md`: cây POS.Web — Store gom `Reports/Transactions/Operations/Dialogs`, Ops/Admin pages mới, Services/Pdf

**Lưu ý cho session sau:** Repository namespace GIỮ NGUYÊN `POS.Infrastructure.Repositories[.Interfaces]` dù folder gom theo domain (tránh đụng consumer). Mục D (chữ ký repo) không đổi vì chỉ move file. CURRENT_STRUCTURE KHÔNG chứa POS.Web (xem WEB_STATUS).

---

## [2026-06-28] POS.Web Security Hardening — config, headers, credentials, SQL Console

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Pattern mới + Bug fix (cấu hình bảo mật)

**Bối cảnh:** Vá theo báo cáo đánh giá bảo mật POS.Web. Production publish thẳng internet, KHÔNG proxy, đang test qua HTTP. Làm tuần tự từng mục, dừng-báo-cáo sau mỗi mục.

**Thay đổi:**
- `Program.cs`: (C2) DetailedErrors tắt ngoài Dev; (C1+H2) section `Security` config-driven 3 mode (`BehindProxy`/`DirectHttps`/`Internet`) + cờ `RequireHttps` tách biệt việc ép HTTPS — cookie `SecurePolicy`/`SameSite`, `UseHsts`/`UseHttpsRedirection`, `UseForwardedHeaders` (chỉ BehindProxy, có KnownProxies/Networks); (M1) middleware security headers + CSP; (C4) hook giải mã `enc:` từ config trước `AddInfrastructure`
- `Components/App.razor`: bỏ inline `onload` font Roboto (dùng `<link rel=stylesheet>`) để CSP `script-src 'self'` không chặn
- `src/POS.Infrastructure/Security/SecretProtector.cs` **(mới)**: AES-256-GCM, token `enc:`, `DecryptTokens` thay phần password trong connection string
- `Components/Pages/Admin/EncryptSecretPage.razor` **(mới)**: `/admin/encrypt-secret` (AdminOnly) — tạo khóa + mã hóa secret
- `Services/SqlConsoleService.cs` + `ISqlConsoleService.cs`: (H1) mask `password/token/secret/...` trong audit + Kibana log; cờ `IsEnabled` (Security:EnableSqlConsole) gate cả service lẫn page
- `Components/Pages/Admin/SqlConsolePage.razor`: chặn UI khi console bị tắt
- `appsettings.{json,Production,Development}.json`: section `Security` (Prod `Mode=Internet`, `RequireHttps=false`, headers on; Dev headers off để không chặn VS Browser Link)
- `docker-compose.yml` + `.env.example`: `POSWEB_SECRET_KEY` qua `.env` (đã gitignore)
- `docs/ROLLOUT.md` **(mới)**: tài liệu trung tâm các bước cấu hình go-live (C4/C1/H2/H1)

**Pattern mới:**
- Security headers + CSP cho Blazor Server + config-driven HTTPS (`RequireHttps`) → `.claude/skills/web/SKILLS.md`
- Mã hóa credentials trong appsettings (`enc:` + config decryption hook, AES-256-GCM) → `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- Khi thêm cấu hình cần thao tác lúc go-live → **tự cập nhật `docs/ROLLOUT.md`** (đã lưu memory).
- CSP `connect-src 'self'` chặn VS Browser Link → security headers TẮT ở Dev (`EnableSecurityHeaders=false`), BẬT ở Prod/UAT.
- C4 mới là **cơ chế**; password thật vẫn plaintext tới khi ops chạy rollout (tạo khóa + mã hóa). Còn `RequireHttps=false` tới khi có TLS.

---

## [2026-06-26] POS.Web UI Polish — DataTable header, sort labels, filter panel chuẩn hóa

**Layer:** POS.Web
**Loại:** Refactor UI / Pattern mới

**Thay đổi:**
- `wwwroot/app.css`: MudTable header override toàn cục — nền `#D9E5F7`, border-bottom 2px navy, `padding: 10px 16px` (header height ~33px cân bằng với body row có chip ~32px), sort button `min-height:unset padding:0`; đổi `--pos-bg-alt` từ `#EEF1F7` → `#D9E5F7`
- `Pages/Admin/UsersPage.razor`: chuẩn hóa cấu trúc page — KPI row 3 cards (tổng/active/locked) + filter panel (search+role+status) + MudTable không có count text
- `Pages/Admin/AuditPage.razor`: xóa ToolBarContent count text; thêm sort cho cột `DecidedAt`
- `Pages/Store/Transactions/TransactionsPage.razor`: xóa inline result summary block + `FormatSummaryVND` helper
- `Pages/Store/Transactions/VoidsPage.razor`: xóa inline result summary block + fields `_distinctVoiders/_selfVoidCount` + `FormatSummaryVND`
- `Pages/Store/Operations/ShiftSummaryPage.razor`: thêm sort cho toàn bộ 9 cột bảng summary (`ShiftNumberSummaryDto`) và 8 cột bảng detail (`EosShiftDto`) — bao gồm nullable DateTime
- `Pages/Store/Reports/RevenueHourlyPage.razor`: thêm sort cho 7 cột — cột `Ngày` sort bằng `SortOrder` (int) thay vì `TimeLabel` (string)
- 9 pages khác: fix filter panel `Elevation="2"` → `Elevation="1"`

**Pattern mới:**
- MudTable header CSS override toàn cục (1 block CSS, không cần sửa Razor) → `.claude/skills/web/datatable.md`
- Sort nullable DateTime: `x => x.NullableProp ?? DateTime.MinValue` → `datatable.md`
- Sort pre-formatted string date: dùng `SortOrder` (int), không sort `TimeLabel` (string) → `datatable.md`
- Filter panel luôn `Elevation="1"`; DataTable luôn `Elevation="2"` → `datatable.md` anti-patterns
- Không dùng inline result summary text — KPI cards thay thế → `datatable.md` anti-patterns

**Lưu ý cho session sau:** Khi tạo page mới với MudTable, KHÔNG thêm block `@if (!_loading && _items.Count > 0) { <div>Tìm thấy...</div> }` — đây là anti-pattern đã được xác nhận; dùng KPI cards hoặc `InfoFormat` của `MudTablePager`.

---

## [2026-06-26] Guardrails kiến trúc (Giai đoạn 1) + chuyển hướng Greenfield

**Layer:** tests/POS.ContractTests, POS.Api, CLAUDE.md
**Loại:** Pattern mới + Tài liệu + Quyết định kiến trúc

**Bối cảnh:** Đánh giá kiến trúc tổng thể (Clean Architecture đã chuẩn). Quyết định **ngừng
migrate từ dự án cũ (.NET 4.6 / `POS.Backend`)**, chuyển sang **phát triển mới (greenfield)**.
Bổ sung guardrails **additive** (không đụng logic hiện tại) để mở rộng nhiều module an toàn.

**Thay đổi:**
- `tests/POS.ContractTests/JsonFieldContractTests.cs` + `JsonContract.cs` *(mới)*: contract test
  khoá tên field JSON cho DTO response trọng yếu (`ResultResponse`, `InfoMemberModel`,
  `PaymentEntryLoyalty`, `GiftDataRespone`) — đổi/thêm/xoá field → test đỏ.
- `tests/POS.ContractTests/DependencyInjectionTests.cs` *(mới)*: DI validation — mọi phụ thuộc
  `POS.*` của controller + implementation đã đăng ký phải có trong container; chỉ đọc service
  descriptor, không cần Redis/SQL.
- `tests/POS.ContractTests/ExceptionMiddlewareTests.cs` *(mới)*: khoá hành vi exception
  middleware (HTTP 500 + `ResultResponse` PascalCase + bỏ field `Data`).
- `tests/POS.ContractTests/POS.ContractTests.csproj`: thêm ProjectReference
  Common/Application/Infrastructure/Api + FrameworkReference `Microsoft.AspNetCore.App` +
  `Newtonsoft.Json`; xoá `UnitTest1` placeholder.
- `src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs` *(mới)* + `Program.cs`: global
  exception middleware đầu pipeline → trả đúng `ResultResponse` (`DefaultContractResolver`
  PascalCase + `NullValueHandling.Ignore`).
- `CLAUDE.md`: thêm §Guardrails & Testing + §Quy ước phát triển mới (Greenfield); gỡ nội dung
  khung "migrate" (bảng Mapping Namespace cũ→mới, tham chiếu source cũ, framing MemoryCacheService
  code cũ); sửa ghi chú Swagger lỗi thời (đã bật ở Development).

**Pattern mới:**
- Contract test khoá tên field JSON (reflection `[JsonProperty]`) — bảo vệ hợp đồng 5.000 POS.
- DI validation test (descriptor-only, infra-free) — chặn "quên `AddScoped`" lúc test.
- Global exception middleware giữ contract `ResultResponse`.
- Convention feature greenfield: `Features/{Domain}/` + AppService 3 lớp.

**Lưu ý cho session sau:**
- Dự án **không còn migrate** từ `POS.Backend` — mọi nghiệp vụ là code mới; contract JSON 5.000
  POS **vẫn giữ** cho endpoint hiện hữu.
- Khi cố ý đổi field DTO đã khoá → cập nhật danh sách trong `JsonFieldContractTests.cs` **cùng
  commit**; DTO response mới → thêm `[Fact]` khoá field.
- Chạy `dotnet test tests/POS.ContractTests` trước commit (hiện 9 test, build 0 error).
- Còn để dành (Giai đoạn 2): gom file theo `Features/{Domain}/` khi ~30+ service; mapping
  helper / API versioning khi cần.

---

## [2026-06-26] Flat UI + Density Standard — POS.Web design system chuẩn hóa

**Layer:** POS.Web
**Loại:** Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: `DefaultBorderRadius` 8px → 4px; `LineHeight` "1.6" → "1.45"; Shadow array E1-E5 → hairline `"0 0 0 1px rgba(26,43,69,0.12)"` (E6+ giữ nguyên bảo vệ dropdown/dialog)
- `src/POS.Web/wwwroot/app.css`: thêm Flat UI overrides (input border thin), dropdown/sidebar spacing (5px/4px desktop), button-input alignment (`align-self: flex-end` sm+), KPI equal height, mobile safety block (40px min tap targets + LineHeight 1.5)
- `src/POS.Web/Components/Layout/MainLayout.razor`: `MudAppBar Dense="true"` + `MudNavMenu Margin="Margin.Dense"`
- `src/POS.Web/Components/Pages/Store/TransactionDetailDialog.razor`: thêm `Dense="true"` vào 2 MudTable
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: `MudGrid Spacing="3"` (KPI + chart)
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`: `MudGrid Spacing="3"` (2 grid)
- `src/POS.Web/Components/Pages/Store/TopProductPage.razor`: `MudGrid Spacing="3"`
- `CLAUDE.md`: thêm §14 MudBlazor Flat UI Standard + §15 Density Standard

**Pattern mới:** Flat UI shadow array (E1-E5 hairline, E6+ unchanged) + Density Standard (LineHeight/Spacing/Dense) → `.claude/skills/web/theming.md`

**Lưu ý cho session sau:**
- E6+ shadow KHÔNG được làm phẳng — MudPopover (MudSelect/Autocomplete) dùng E8, MudDialog dùng E12; làm phẳng → dropdown dính bẹt vào nền.
- CSS global trong `app.css` đã xử lý mobile tap targets — KHÔNG thêm lại `@media (max-width:599.98px)` cho từng component.

---

## [2026-06-25] Production nginx — fix Blazor Server circuit crash (store combobox hang)

**Layer:** POS.Web + nginx config
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `nginx/pos-web.conf`: tăng buffer 64KB → 256KB (`proxy_buffers 8 32k`); thêm `location /_blazor` riêng với `proxy_read_timeout 86400s` + `X-Accel-Buffering "no"`; thêm `X-Accel-Buffering "no"` vào `location /`
- `src/POS.Web/Program.cs`: `DetailedErrors` đọc từ `WebApp:EnableDetailedErrors` config (không hardcode `IsDevelopment()`) — bật/tắt diagnostics không cần deploy lại code
- `src/POS.Web/appsettings.Production.json`: thêm `"EnableDetailedErrors": true` (tạm thời để diagnose — tắt sau khi xác nhận fix)
- `.claude/skills/web/deployment.md`: cập nhật nginx pattern với checklist đầy đủ + anti-patterns

**Pattern mới:** `nginx Blazor Server production-hardened (/_blazor + buffer + X-Accel-Buffering)` → `.claude/skills/web/deployment.md`

**Lưu ý cho session sau:**
- nginx buffer `4×16k = 64KB` quá nhỏ cho Blazor SSR — production cần `8×32k = 256KB`. `proxy_buffering off` không đủ; phải thêm `add_header X-Accel-Buffering "no"` để tắt nginx internal buffer layer.
- Sau khi diagnose xong production → đổi `EnableDetailedErrors` về `false` trong `appsettings.Production.json`.

---

## [2026-06-25] Store Filter UX — DatePicker click-to-open + đồng nhất font size

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: thêm `Body1 = new Body1Typography { FontSize = "0.875rem" }` — fix font dropdown/autocomplete/picker popup từ 16px → 14px, đồng nhất với DataTable và filter labels
- `src/POS.Web/Components/Pages/Store/` (7 file, 13 MudDatePicker): bỏ `Editable="true"`, thêm `AutoClose="true"` → click ô text mở calendar ngay; chọn xong tự đóng
- `.claude/skills/web/theming.md`: thêm rule bắt buộc Body1 override + giải thích Default không cascade
- `.claude/skills/web/SKILLS.md`: thêm 2 anti-pattern (Body1 missing, MudDatePicker Editable); fix `ResetValueOnEmptyText="true"` bug trong Store Selector snippet

**Pattern mới:**
- `MudDatePicker click-to-open: AutoClose="true" (bỏ Editable)` → `.claude/skills/web/SKILLS.md`
- `PosTheme Body1 typography bắt buộc` → `.claude/skills/web/theming.md`

**Lưu ý cho session sau:**
- `Default.FontSize` trong MudBlazor theme KHÔNG cascade xuống `Body1` — mỗi khi tạo theme mới BẮT BUỘC thêm `Body1 = new Body1Typography { FontSize = "..." }` riêng.
- Mọi `MudDatePicker` trong filter panel dùng `AutoClose="true"` (không `Editable`) — click text = mở calendar, không cần click icon. Store Selector (MudAutocomplete) KHÔNG dùng `ResetValueOnEmptyText="true"` (circuit crash).

---

## [2026-06-24] TopProductPage — Top sản phẩm bán chạy + tối ưu BA/BI

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/RptCentralSale/`: 4 DTO mới — `TopProductKpiDto` (RS1), `TopProductDto` (RS2), `TopProductCategoryDto` (RS3), `ProductOrderLineDto` (drill-through)
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` + interface: `GetTopProductAsync` (QueryMultiple 3 RS + cache Pattern 4 + timeout 45s, `@CategoryNo=NULL`) + `GetProductOrderLinesAsync` (SQL trực tiếp ReportSaleDetail theo ItemNo, TOP 500)
- `src/POS.Web/Components/Pages/Store/TopProductPage.razor` (mới, `/store/top-product`): filter store/ngày/Top-N/sort + compare, KPI 3 card, **CSS bar list** (thay horizontal bar/treemap), MudTable drill-through. Pattern scale-safe (guard re-entrancy, CTS, OnAfterRenderAsync, clamp 92 ngày). **BA/BI:** thêm cột Giá TB/Trả%/Độ phủ/Giảm% (từ field SP đã tính) + cột Biến động (Δ hạng/NEW/Δ DT%) join kỳ trước client-side
- `src/POS.Web/Components/Pages/Store/ProductOrdersDialog.razor` (mới): dialog drill-through hóa đơn của 1 SP
- `src/POS.Web/Components/Layout/MainLayout.razor`: NavLink "Top sản phẩm bán chạy" + auto-expand nhóm Báo cáo
- `docs/migrations/rpt_salebytime_perf.sql`: bổ sung index `(ItemNo, OrderDate)` cho drill-through + đính chính cột ngày thực tế = `OrderDate`

**Pattern mới:**
- `CSS bar list (horizontal) — thay horizontal/treemap MudBlazor không có` → `.claude/skills/web/charts.md`
- `MudTable row → drill-through dialog` + `Tận dụng dữ liệu SP đã tính + so sánh cấp dòng (BA/BI)` → `.claude/skills/web/reports.md`

**Lưu ý cho session sau:**
- MudBlazor v9 KHÔNG có horizontal bar 2 trục / treemap → dùng CSS bar list; format `width:%` BẮT BUỘC `InvariantCulture` (culture VN dùng dấu phẩy → phá CSS).
- Trước khi thêm SP/cột mới cho 1 chỉ số: kiểm tra SP report hiện tại **đã trả cột đó chưa** — nhiều cột bị page vứt (return qty, avg price, order count, discount). Compare cấp dòng = giữ list prev + join theo khóa, đừng chỉ dùng prev cho KPI tổng.
- Chiều "Ngành hàng" của `sp_ReportTopProduct` đang trả NULL (chưa JOIN Item master) → page ẩn tạm filter/treemap/KPI category; RS3 vẫn map sẵn (`TopProductCategoryDto`), bật lại dễ khi SP có JOIN.
- `CURRENT_STRUCTURE.md` KHÔNG track repo `RptCentralSale` → bỏ qua Bước 3 (như các task RptCentralSale trước).

---

## [2026-06-23] RevenueHourlyPage — tối ưu data path + page cho quy mô 10M dòng ReportSaleDetail

**Layer:** POS.Infrastructure, POS.Web, POS.Common
**Loại:** Pattern mới + Refactor (tối ưu hiệu năng)

**Bối cảnh:** đánh giá `RevenueHourlyPage` + cách lấy dữ liệu qua `sp_ReportSaleByTime` khi `ReportSaleDetail` (bảng mart, worker rebuild mỗi 60s) lớn tới ~10M dòng.

**Thay đổi:**
- `src/POS.Common/Dtos/RptCentralSale/SaleByTimeKpiDto.cs` + `SaleByTimeSeriesDto.cs`: DTO map RS1 (KPI) / RS2 (series) của SP (đã có từ vòng tạo trang)
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` + `IRptCentralSaleRepository.cs`: `GetSaleByTimeAsync` thêm **Redis cache** (key `MD:RptSaleByTime:*`, TTL 180s nếu range có hôm nay / 12h nếu quá khứ), tách cache KPI khỏi series, tham số `includeKpi`, timeout riêng 45s (thay 120s); inject thêm `IRedisService`
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`: (1) guard `if (_loading) return;` + disable preset chips khi load; (2) `CancellationTokenSource` theo vòng đời + `IDisposable` + truyền `ct`; (3) hoãn auto-load khỏi prerender → `OnAfterRenderAsync(firstRender)`; (4) clamp 92 ngày khi xem all-stores; (6) line chart luôn hiện legend. Call DAY xin KPI, HOUR/WEEKDAY/compare `includeKpi:false`
- `docs/migrations/rpt_salebytime_perf.sql`: script chờ DBA — index `ReportSaleDetail(StoreNo, SaleDate)` INCLUDE cột đo + thêm `@IncludeKpi BIT` cho SP

**Pattern mới:**
- `Cache report query (SP) — TTL theo độ mới + bỏ result-set dư` → `.claude/skills/cache/SKILLS.md` (Pattern 4)
- `Report page an toàn ở quy mô lớn` (re-entrancy guard + CTS + defer-prerender + clamp all-stores) → `.claude/skills/web/reports.md`

**Lưu ý cho session sau:**
- Report SP nặng → BẮT BUỘC cache Redis TTL-theo-độ-mới + timeout riêng (KHÔNG dùng 120s chung), không cache vô thời hạn.
- Report page tự load → `if (_loading) return;` + CTS + auto-load trong `OnAfterRenderAsync(firstRender)` (KHÔNG trong `OnInitializedAsync` vì prerender chạy 2 lần). Disable cả **preset chips**, không chỉ nút.
- `CURRENT_STRUCTURE.md` KHÔNG track repo `RptCentralSale` (cả `GetDetailRevenueSales`/`GetSalesByCategory` cũng vắng) → đã bỏ qua Bước 3 để không tạo entry mồ côi.
- Đòn bẩy lớn nhất cho cold-cache lần đầu là **index DB** — còn chờ DBA chạy `docs/migrations/rpt_salebytime_perf.sql`.

---

## [2026-06-23] Chuẩn hóa DataTable → MudTable + tách SKILLS.md web + store combobox

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Refactor + Pattern mới (đảo ngược pattern cũ)

**Thay đổi:**
- **Chuyển TOÀN BỘ DataTable từ `<table class="pos-table">` + `PosTableBase<T>` → `MudTable<T>`** (11 page): TransactionsPage, EosShiftsPage, UsersPage, AuditPage, RevenueHourlyPage, DataRawLogPage, LogsPage, DetailRevenuePage (ServerData), SqlConsolePage (cột động), PosMapPage (từ MudDataGrid); sửa header anti-pattern RevenuePage + PosMapPage
- `src/POS.Web/Components/Shared/PosTableBase.cs`: **ĐÃ XÓA** (MudTable có sort/paginate built-in)
- `src/POS.Infrastructure/Repositories/CentralMDRepository.cs` + `ICentralMDRepository.cs`: thêm `GetStoreListAsync()` — query bảng Store (No+Name), cache Redis `MD:StoreList` 12h
- Store combobox 4 page (TransactionsPage, DetailRevenuePage, SalesByCategoryPage, EosShiftsPage): `MudAutocomplete<StoreDto>` hiển thị "StoreNo – Name", tìm theo mã + tên (thay `MudAutocomplete<string>` chỉ có mã)
- `src/POS.Web/Components/Pages/Store/TransactionDetailDialog.razor`: cột "Mô tả" lấy `TenderTypeName` (thay `ReferenceNo`); table → Default size; nút Đóng → Outlined/Secondary
- **Tách `.claude/skills/web/SKILLS.md` (1136 → 613 dòng)** thành 6 file con: `filter-store.md`, `datatable.md`, `charts.md`, `reports.md`, `theming.md`, `deployment.md` + bảng index "Skill con — đọc khi cần"
- `CLAUDE.md` §10.B + `.claude/skills/web/SKILLS.md`: cập nhật chuẩn DataTable = MudTable

**Pattern mới:** `MudTable<T> — DataTable chuẩn` (client/server/dynamic/footer) → đã cập nhật `.claude/skills/web/datatable.md`. **THAY THẾ** pattern `PosTableBase<T>` cũ (changelog 2026-06-18).

**Lưu ý cho session sau:**
- DataTable mới **BẮT BUỘC** dùng `MudTable` (`MudTableSortLabel` + `MudTablePager`). KHÔNG còn `PosTableBase`/`pos-table` (trừ pivot report `rpt-pivot-table` vẫn raw table).
- Server-side paging: `MudTable @ref + ServerData` + `_table.ReloadServerData()` (KHÔNG gọi LoadDataAsync thủ công). Note cũ ở entry 2026-06-23 DetailRevenue (MudPagination Selected/SelectedChanged) đã lỗi thời.
- Store picker: dùng `MdRepo.GetStoreListAsync()` + `MudAutocomplete<StoreDto>`, KHÔNG dùng `GetStoreSetConfigAsync()` (không có Name). Xem `filter-store.md`.
- SKILLS.md web giờ là index — đọc file con tương ứng khi cần, tránh đọc cả file.

---

## [2026-06-23] Sidebar refactor — Ops tách 2 sub-group + bỏ icon cấp 3

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tách "Vận hành" Ops từ flat 6 links → 2 MudNavGroup con (Giám sát: Health/POS map/Alerts/Queues; Nhật ký: Logs/DataRaw Log); xóa `Icon="..."` khỏi toàn bộ 12 MudNavLink cấp 3 trong Store; thêm `_expandOpsMonitor` + `_expandOpsLog` + cập nhật `UpdateExpanded()`

**Pattern mới:** Sidebar 3-cấp — icon chỉ ở cấp 1 (section) và cấp 2 (sub-group), cấp 3 (leaf MudNavLink) không có icon → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:** Khi thêm trang Ops mới: nếu thuộc monitoring (health/status) → vào sub-group Giám sát; nếu thuộc logs/audit → vào sub-group Nhật ký. Leaf links KHÔNG được thêm `Icon=`.

---

## [2026-06-23] DetailRevenuePage — Báo cáo doanh thu chi tiết + menu sidebar refactor

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Feature mới + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tổ chức lại menu "Cửa hàng" thành 3 nhóm con (Vận hành, Giao dịch, Báo cáo); cập nhật `UpdateExpanded()` để auto-expand nhóm con
- `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor` *(tạo mới)*: Page báo cáo doanh thu chi tiết — 11 filters (từ/đến ngày, cửa hàng, tìm kiếm, loại đơn, hình thức bán, đối tác, VAT, đơn hàng gốc, thu ngân) + data table 21 cột + server-side pagination (50 rows/page) + Kibana + console logging
- `src/POS.Web/Components/Pages/Store/BusinessDayPage.razor` *(tạo mới)*: Stub — Ngày kinh doanh
- `src/POS.Web/Components/Pages/Store/ShiftSummaryPage.razor` *(tạo mới)*: Stub — Tổng kết ca
- `src/POS.Web/Components/Pages/Store/RefundsPage.razor` *(tạo mới)*: Stub — Hoàn trả
- `src/POS.Web/Components/Pages/Store/VoidsPage.razor` *(tạo mới)*: Stub — Hủy GD
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor` *(tạo mới)*: Stub — Doanh thu theo giờ
- `src/POS.Web/Components/Pages/Store/PaymentBreakdownPage.razor` *(tạo mới)*: Stub — Phân tích thanh toán
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`: Thêm parameter normalization + detailed console logging (FromDate, ToDate, filters, page info, result count)
- `src/POS.Common/Dtos/RptCentralSale/DetailRevenueSalesDto.cs` *(đã tồn tại)*: 40 properties (ngày, giờ, số đơn, CH, POS, thu ngân, loại đơn, barcode, mã SP, tên SP, ĐVT, SL, đơn giá, giảm giá, thuế%, thuế VND, thành tiền, hình thức bán, đối tác, KM, coupon)
- `src/POS.Infrastructure/Repositories/Interfaces/IRptCentralSaleRepository.cs` *(đã tồn tại)*: Interface `GetDetailRevenueSalesAsync()` với 11 parameters + pageSize/pageNumber
- `src/POS.Infrastructure/DependencyInjection.cs` *(đã cập nhật)*: Line 41 — `AddScoped<IRptCentralSaleRepository, RptCentralSaleRepository>()`

**Pattern mới:**
1. **Server-side pagination với MudPagination** — dùng `Selected` + `SelectedChanged` event (KHÔNG `@bind-Selected`) để tránh conflict; phân biệt với TransactionsPage (client-side PosTableBase)
   - File: `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor`
2. **Menu sidebar nested MudNavGroup** — 3 cấp: parent → 3 sub-group → items; auto-expand theo URL pattern
   - File: `src/POS.Web/Components/Layout/MainLayout.razor`
3. **Tách DTO/Repository cho báo cáo (Rpt prefix)** — `RptCentralSale/` folder + `IRptCentralSaleRepository` riêng khỏi `ICentralSaleRepository` để tránh coupling với POS.Api
   - Files: `src/POS.Common/Dtos/RptCentralSale/`, `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`

**Lưu ý cho session sau:**
- DetailRevenuePage phục thuộc `[dbo].[RPT_GET_DETAIL_REVENUE_SALES_LIST]` SP trên RPOSCentralSales DB — nếu SP không trả data, kiểm tra: FromDate/ToDate format, StoreNo not empty, SalesType="-1" default; test SP trực tiếp với tham số tương ứng
- Menu sidebar UpdateExpanded() phải cover tất cả route mới (`/store/revenue-detail` đã được thêm vào dòng 156)
- Server-side pagination event (`SelectedChanged`) phải gọi `ReloadPageAsync(int newPage)` để gọi lại SP với page number mới (0-based)
- Responsive UI: Filter fields stack dọc trên mobile (xs), nút Tìm/Xóa full-width (`FullWidth="true"`)

---

## [2026-06-19] SAPController — migrate Internal Voucher APIs + business logic fixes

**Layer:** POS.Api, POS.Application, POS.Infrastructure, POS.Common
**Loại:** Feature + Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Api/Controllers/SAPController.cs`: Thêm `CheckReturnVoucher`, `UpdateReturnVoucher`; giữ `CheckVoucher`, `CreateNewVoucher`, `CreateReturnVoucher`, `RedeemCpnVch`
- `src/POS.Application/Interfaces/ISAPService.cs`: Thêm `UpdateReturnVoucherAsync`
- `src/POS.Application/Services/SAPService.cs`: Implement `UpdateReturnVoucherAsync`; fix `CheckVoucherAsync` (RDM→Return="1", EXP status + kiểm tra ngày `Expiry_Date < DateTime.Today`); fix `RedeemCpnVchAsync` (named param `ct: ct` sau khi signature thay đổi)
- `src/POS.Infrastructure/Repositories/Interfaces/ISAPVoucherRepository.cs`: Thêm optional `requiredVoucherType` vào `RedeemVouchersAsync`
- `src/POS.Infrastructure/Repositories/SAPVoucherRepository.cs`: Thêm check VoucherType trong transaction (UPDLOCK); thêm amount validation (0 ≤ AmountRedeem ≤ faceValue); UPDATE per-row với `AmountUsed` + `OrderUsed`
- `src/POS.Common/Dtos/Vouchers/VoucherStatusResponseDto.cs`: Thêm `AmountUsed decimal?`, `OrderUsed string?`
- `src/POS.Common/Dtos/SAP/SAPDto.cs`: Thêm `VoucherUpdateRequest`
- `src/POS.Common/Validation/StringRangeAttribute.cs`: Tạo mới — custom whitelist validation
- `docs/migrations/alter_internal_voucher_add_amountused_orderused.sql`: DDL thêm 2 cột vào `Internal_Voucher`

**Pattern mới:**
- Optional VoucherType filter trong UPDLOCK transaction → `.claude/skills/api/SKILLS.md`
- Named CancellationToken khi thêm optional param vào giữa signature → `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- `Internal_Voucher` cần chạy DDL migration trước khi deploy: `ALTER TABLE ADD AmountUsed DECIMAL(18,2) NULL, OrderUsed NVARCHAR(50) NULL`
- `UpdateReturnVoucher` chỉ cho phép voucher `VoucherType = "BNMH"` (do `CreateReturnVoucher` tạo ra) — check diễn ra trong transaction UPDLOCK
- Khi thêm optional param vào giữa signature → scan callers và thêm `ct: ct` (named) cho CancellationToken

---

## [2026-06-19] RevenuePage — Y-axis auto-scale theo dữ liệu thực tế

**Layer:** POS.Web
**Loại:** Bug fix

**Thay đổi:**
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Thêm `CalcYMax` + `CalcYTick` helpers; set `YAxisSuggestedMax` + `YAxisTicks` trên cả 2 `BarChartOptions` sau khi load data

**Pattern mới:** Y-axis auto-scale cho MudBlazor v9 Bar/Line chart → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `BarChartOptions.YAxisTicks` mặc định = **20** là *khoảng cách giữa tick* (không phải số lượng) → luôn set kèm `YAxisSuggestedMax` khi data là số nhỏ
- `YAxisSuggestedMax` (double?) là gợi ý — MudBlazor tự mở rộng nếu data vượt quá, không bao giờ clip data

---

## [2026-06-19] DataRawJson audit log + tách POS.Worker thành project độc lập

**Layer:** POS.Infrastructure, POS.Api
**Loại:** Feature + Refactor + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Interfaces/ICentralSaleRepository.cs`: Thêm tham số `transactionId` vào `InInsertToTableByJson()`
- `src/POS.Infrastructure/Repositories/CentralSaleRepository.cs`: Refactor `InInsertToTableByJson` dùng try/finally; thêm `InsertDataRawJsonAsync()` private (log vào bảng `DataRawJson`, dùng `directConnectionFactory`); xóa 3 lời gọi `InsertInterfaceErrorAsync` trùng lặp
- `src/POS.Infrastructure/AppServices/KafkaAppService.cs`: Truyền thêm `message.TransactionId`
- `src/POS.Infrastructure/Workers/PosSalesConsumerWorker.cs`: Truyền thêm `msg.TransactionId`
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: Thêm overload `HostApplicationBuilder` + refactor helper `ConfigureSerilogCore` chung — tránh lặp code cấu hình ES/Console
- `src/POS.Infrastructure/DependencyInjection.cs`: Cập nhật comment worker registration
- `src/POS.Api/Program.cs`: Xóa `AddHostedService<PosSalesConsumerWorker>()` — worker đã tách ra
- `src/POS.Worker/` *(tạo mới)*: Project Worker Service — `POS.Worker.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Production.json`
- `Dockerfile.worker` *(tạo mới)*: Multi-stage build dùng `dotnet/runtime:10.0` (không phải aspnet)
- `docker-compose.yml`: Thêm service `worker` (container `pos_worker`, 512MB, `DOTNET_ENVIRONMENT=Production`)
- `POS.slnx`: Thêm `src/POS.Worker/POS.Worker.csproj` vào solution

**Pattern mới:** 
1. `Audit log với try/finally` — `InsertDataRawJsonAsync` pattern trong Repository
2. `POS.Worker project` — Worker Service độc lập với Docker container riêng, hỗ trợ nhiều worker song song qua `AddHostedService<T>()`
3. `SerilogConfiguration dual overload` — cùng 1 extension dùng được cho cả `WebApplicationBuilder` và `HostApplicationBuilder`

**Lưu ý cho session sau:**
- `POS.Worker/Program.cs` KHÔNG gọi `AddApplication()` — worker chỉ cần `AddInfrastructure()` đủ để lấy `ICentralSaleRepository`
- Thêm worker nghiệp vụ mới: chỉ cần thêm class kế thừa `BackgroundService` vào `POS.Infrastructure/Workers/` rồi đăng ký `AddHostedService<T>()` trong `POS.Worker/Program.cs` — không cần project mới
- `DataRawJson` table phải tồn tại trong RPOSCentralSales DB trước khi deploy

---

## [2026-06-19] HealthPage responsive fix + Responsive UI standard vào SKILLS.md

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Ops/HealthPage.razor`: Fix header — `MudStack Row Justify.SpaceBetween` → `div.pos-page-header` (Case B: title + group controls); button thêm `Style="align-self:center; white-space:nowrap"` để không bị stretch theo chiều cao MudSelect+Label; chip container `MudStack Row` → `div.d-flex gap-1 flex-wrap`
- `.claude/skills/web/SKILLS.md`: Thêm section **"Responsive UI — BẮT BUỘC"** — bảng so sánh sai/đúng cho 6 tình huống phổ biến, code mẫu 2 case pos-page-header (A: title+button đơn; B: title+group controls), 4 anti-pattern responsive mới, 1 checklist item nhắc đọc CLAUDE.md §10.G

**Pattern mới:** `pos-page-header Case B — title + group controls` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Khi `MudButton` nằm trong `MudStack Row` cạnh `MudSelect` có `Label`, button sẽ stretch cao bất thường (flex align-items: stretch) — luôn thêm `Style="align-self:center"` vào button để cố định chiều cao.
Responsive UI standard đã có trong cả CLAUDE.md §10 (chi tiết) và SKILLS.md (tóm tắt tra nhanh).

---

## [2026-06-18] Responsive UI Phase 3 — 5 pages/components theo chuẩn mobile

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Drawer responsive init — `IBrowserViewportService.GetCurrentBreakpointAsync()` trong `OnAfterRenderAsync(firstRender)` → drawer đóng trên mobile, mở sẵn trên desktop (≥ md); đổi `IDisposable` → `IAsyncDisposable`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Header `MudStack Row` → `div.pos-page-header` + `pos-page-header-title` + `pos-page-header-btn`; search inner div thêm `flex-wrap`; `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`; summary text `&nbsp;|&nbsp;` → `d-flex flex-wrap gap-3` với 3 `MudText` riêng
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Chip filter container thêm `flex-wrap`

**Lưu ý cho session sau:**
`IBrowserViewportService` inject được trong Blazor Server component — `Breakpoint.Md or Breakpoint.Lg or Breakpoint.Xl or Breakpoint.Xxl` thay vì `>= Breakpoint.Md` để tránh enum so sánh với range values.
Khi sửa `IDisposable` → `IAsyncDisposable`: đổi `Dispose()` → `async ValueTask DisposeAsync()` và `@implements IDisposable` → `@implements IAsyncDisposable`.

---

## [2026-06-18] DataTable standard — PosTableBase\<T\> + EosShiftsPage + sidebar accordion

**Layer:** POS.Web
**Loại:** Feature + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Shared/PosTableBase.cs`: Tạo mới — abstract base class cung cấp sort (single-column), phân trang (PageSize=10), `FormatVND`, `PagedItems`, `TotalFiltered`, `PageCount`
- `src/POS.Web/wwwroot/app.css`: Thêm `.pos-table*` CSS standard (header #EEF1F7/#1A2B45, sort icon ⇅↑↓) + active NavLink highlight (rgba 14% + border-left #3A6FCC)
- `src/POS.Web/Components/Layout/MainLayout.razor`: Sidebar accordion — `NavigationManager.LocationChanged` + `@bind-Expanded` + `IDisposable`; thêm EosShifts nav link
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: Tạo mới — Kết thúc ca bán hàng (filter ngày/store/trạng thái + KPI cards + pos-table); refactored to `@inherits PosTableBase<EosShiftDto>`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: Migrated từ `MudDataGrid` → `@inherits PosTableBase<TransactionListDto>` + `pos-table`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Migrated từ `MudDataGrid + QuickFilter Func<>` → `@inherits PosTableBase<DashboardUser>` + LINQ search với `SearchText` property tự reset `_page = 1`

**Pattern mới:** `PosTableBase<T> — DataTable chuẩn` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Mọi page DataTable mới BẮT BUỘC dùng `@inherits PosTableBase<T>` + `<table class="pos-table">` — KHÔNG dùng `MudDataGrid`.
Khi search filter cần reset page, dùng property C# (`get`/`set { _field = value; _page = 1; }`) thay vì `_searchText` field trực tiếp.

---

## [2026-06-17 20:00] Áp dụng hệ màu DataFlip — PosTheme + CSS variables

**Layer:** POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: Tạo mới — static `MudTheme` với navy primary (#2051A3), sidebar/appbar navy dark (#1B3A5C), teal accent (#1EAA90), semantic status colors, BorderRadius=8px, Button.TextTransform=none
- `src/POS.Web/Components/_Imports.razor`: Thêm `@using POS.Web.Theme` (global cho mọi Layout)
- `src/POS.Web/Components/Layout/MainLayout.razor`: `<MudThemeProvider Theme="@PosTheme.Default"/>`
- `src/POS.Web/Components/Layout/EmptyLayout.razor`: Theme param + bỏ `background:#f0f2f5` hardcode → `var(--mud-palette-background)`
- `src/POS.Web/Components/Layout/MainLayout.razor.css`: Bỏ gradient navy→tím (legacy Blazor template), dùng solid `#1B3A5C`
- `src/POS.Web/Components/Layout/ReconnectModal.razor.css`: Button/spinner dùng `var(--mud-palette-primary)` thay hardcode `#6b9ed2`, `#0087ff`
- `src/POS.Web/wwwroot/app.css`: 28 CSS variables `--pos-*`, scrollbar, `.pos-delta-up/down` utility
- `docs/style-guide.html`: Tạo mới — tài liệu tham chiếu màu (swatches + 6 component mẫu)

**Pattern mới:** `PosTheme.cs — custom MudBlazor Theme` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Trong MudBlazor v9, `Typography.FontWeight` và `LineHeight` là **string** ("600", "1.6"), không phải `int`/`double` — sẽ gây compile error nếu dùng sai type.
`WarningContrastText` phải là màu tối vì #F39C12 (amber) contrast với trắng chỉ 2.4:1 (fail WCAG AA).

---

## [2026-06-17 17:00] Fix deployment POS.Web — blazor.web.js 404 + nginx setup

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Program.cs`: Thêm middleware rewrite `Host: localhost` cho `/_framework/` + **`app.UseRouting()` tường minh** sau middleware (fix root cause: automatic UseRouting chạy trước mọi middleware trong WebApplication .NET 10)
- `src/POS.Web/Components/App.razor`: Google Fonts load non-blocking (`rel="preload"` + `onload`)
- `src/POS.Web/Dockerfile`: `mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app` TRƯỚC `USER $APP_UID` — fix `CryptographicException` khi Docker volume owned bởi root
- `publish/POS.Web/`: Build output self-contained linux-x64 cho nginx deployment

**Patterns mới:** 4 patterns → đã cập nhật `.claude/skills/web/SKILLS.md`:
- `Explicit UseRouting() để middleware chạy TRƯỚC routing`
- `Fix _framework/blazor.web.js 404 từ external IP`
- `nginx config cho Blazor Server`
- `DataProtection keys trong Docker`

**Lưu ý cho session sau:**
Trong .NET 9/10 `WebApplication`, `UseRouting()` tự động chèn vào ĐẦU pipeline — BẮT BUỘC gọi
`app.UseRouting()` tường minh sau bất kỳ middleware nào cần chạy trước routing.
Sau khi deploy nginx, test: `curl -sv -H "Host: <ip>:5001" http://localhost:8080/_framework/blazor.web.js` phải trả 200.

---

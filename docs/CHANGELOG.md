# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.
>
> **Lưu ý định hướng (2026-06-26):** dự án **không còn migrate** từ POS.API (.NET 4.6 /
> `POS.Backend`) — nay **phát triển mới (greenfield)**. Các entry cũ có từ "migrate"/"Migrated"
> là ghi chép lịch sử tại thời điểm đó, giữ nguyên để tra cứu.
>
> **✅ Đã xử lý 2026-07-11**: các khối merge-conflict marker (`<<<<<<< HEAD` / `=======` /
> `>>>>>>>`) từng tồn đọng rải rác trong file này (ghi nhận 2026-07-10, quanh dòng ~1488, ~1581,
> ~1865-1873, ~1914-1955, ~2184-2225 tại thời điểm đó) đã được resolve thủ công — đọc kỹ nội dung
> 2 phía mỗi conflict, xác nhận qua so khớp tiêu đề `## [...]` trùng lặp: hầu hết là các entry
> **rời rạc** (2 nhánh cùng thêm entry khác nhau) → giữ cả 2; phát hiện 1 cặp entry **trùng lặp
> thật** ("Danh mục Bảng giá (9.1)" và "Cài đặt giá / Danh mục Bảng giá") — bản cũ tham chiếu
> `database-schema.md` (tên file trước khi đổi), bản đúng tham chiếu `centralMD-schema.md` (đã đổi
> tên 2026-07-09) → xóa bản cũ, giữ bản đã cập nhật tên file. Verify: `grep -rn '^<<<<<<<\|^=======$\|^>>>>>>>'`
> trên toàn `docs/` → 0 kết quả; không còn tiêu đề `## [...]` nào trùng lặp (kiểm tra bằng
> `sort | uniq -d`).

## [2026-07-13] Tái cấu trúc `.claude/` — tách triệt để Rules ↔ Skills

**Layer:** Tài liệu AI-context (`.claude/**` + `CLAUDE.md`) — KHÔNG đụng source code/appsettings/DB.
**Loại:** Refactor

**Bối cảnh:** Lớp `.claude/skills/` trộn lẫn Rules (tiêu chuẩn/ràng buộc — WHAT/WHY) với Skills
(hướng dẫn thực thi — HOW). Tách theo nguyên tắc: mỗi mẩu nội dung ở đúng 1 nơi; skill chỉ trỏ
ngược về rule, không lặp lại. Giữ nguyên tên thư mục 6 skill đăng ký (folder = tên invocation) để
không vỡ harness; giữ đường dẫn file để router không gãy.

**Thay đổi:**
- **Tạo 5 file rule** (`.claude/rules/`): `caching-standards.md`, `database-standards.md`,
  `worker-standards.md`, `logging-standards.md`, `unit-testing-standards.md` — trích khối normative
  từ `cache`/`database`/`worker`/`api/logging`/`payment-test-generator` tương ứng.
- **Bổ sung rule đã có:** `backend-api-rules.md` (+ "Middleware & API Security", + convention
  `{DtoName}_locked`); `blazor-web-app.md` (+ §17: performance, DataTable standards, column-naming,
  DateTime format, component mapping — gộp từ lớp web numbered).
- **Xóa lớp trùng lặp web numbered:** `web/01-architecture-and-logic.md`, `02-ui-ux-and-components.md`,
  `03-integration-and-performance.md`, `04-datatable-and-lists.md` (rules → `blazor-web-app.md`;
  code pattern → `web/component-patterns.md` mới).
- **Làm mỏng skill (rút rule, thêm con trỏ):** `cache`/`database`/`worker/SKILLS.md`,
  `api/{logging,middleware-patterns,file-streaming-patterns}.md`, `web/{SKILLS,charts,filter-store,
  datatable,ui-polish-standard,security-hardening}.md`, `codebase-map`, `contract-test-guardian`,
  `payment-test-generator`.
- **Rewire:** cập nhật router `CLAUDE.md` (thêm 5 rule mới theo cặp rule+skill), `refactor-skills.md`
  (nguyên tắc phân định + danh sách file mới), pointer trong `WEB_STATUS.md`.

**Pattern mới:** không — đây là refactor tài liệu, không thêm nghiệp vụ. KHÔNG cập nhật
CURRENT_STRUCTURE/appsettings (không đụng code/config).

**Bằng chứng verify:** (1) script quét link `.claude/*.md` → tất cả resolve, 0 gãy; (2) 6 skill
đăng ký còn nguyên tên thư mục; (3) grep hotspot → bảng normative (Redis key/TTL, SP naming,
worker 8 luật, Nguyên tắc Mock) chỉ còn ở đúng 1 file rule, skill chỉ còn con trỏ.

**Lưu ý cho session sau:** LUẬT (naming/TTL/layer/security/"BẮT BUỘC-CẤM") sống ở `.claude/rules/`;
HOW (template/code/các bước) ở `.claude/skills/`. Khi thêm skill mới KHÔNG nhúng khối rule — trỏ về
rule. Lớp web `01–04` đã xóa; luật web nền tảng nay ở `blazor-web-app.md` (§17), code pattern ở
`web/component-patterns.md`.

---

## [2026-07-12] Tích hợp MCP (SQL read-only + Redis) + hạ tầng Unit Test luồng Payment

**Layer:** Tooling/hạ tầng (`.mcp.json`, `.claude/`, `tests/POS.UnitTests/`, `docs/mcp/`) — KHÔNG đụng code `src/`

**Loại:** Pattern mới (tooling + test infra)

**Thay đổi:**
- `.mcp.json` (mới): khai báo 2 MCP server `mssql-rpos-readonly` (npx) + `redis-rpos` (uvx), **chỉ dùng
  `${VAR}`** — không hardcode connection string; được commit (đã thêm `!.mcp.json` vượt rule `*.json`).
- `.claude/settings.local.json` (mới, **gitignored**): chứa endpoint + credential DEV thật, không commit.
- `.gitignore`: thêm `!.mcp.json` + chặn tường minh `settings.local.json`.
- `.claude/skills/payment-test-generator/SKILL.md` (mới, số ít + YAML frontmatter): skill sinh unit
  test xUnit+Moq+FluentAssertions, mã hóa Nguyên tắc Mock (test qua seam interface Application).
- `tests/POS.UnitTests/` (mới): `POS.UnitTests.csproj` (Moq 4.20.72 + FluentAssertions **7.0.0** free) +
  3 file test luồng Payment (`PaymentControllerTests`/`GotITServiceTests`/`UrboxServiceTests`) — **13 test PASS**.
- `POS.slnx`: đăng ký `tests/POS.UnitTests`.
- `docs/mcp/step-by-step-mcp-guide.md` (mới): hướng dẫn dev bật/dùng MCP + chạy/sinh test.
- `CLAUDE.md`: +2 dòng Router Index (skill test + guide MCP).

**Pattern mới:** `payment-test-generator` (skill) — unit test qua interface Application, mock Moq,
assert theo StatusCode (success→200/fail→400/unknown→BadRequest/throw→500), delegation thin-wrapper.
Bẫy đã ghi vào skill: class `GotITService`/`UrboxService` **trùng tên** Application↔Infrastructure
(AppService 3-layer) → phải alias namespace (`using AppPartner = ...`) tránh CS0104.

**Verify:** `dotnet test tests/POS.UnitTests` → 13/13 PASS; `dotnet test tests/POS.ContractTests` →
45/45 PASS (không hồi quy); `dotnet build POS.slnx` → 0 error/0 warning.

**Lưu ý cho session sau:**
- **MCP CHƯA chạy thật** — 2 blocker hạ tầng: `uvx` chưa cài (Redis) + npm/npx bị chặn TLS công ty
  (`UNABLE_TO_VERIFY_LEAF_SIGNATURE`, cần `NODE_EXTRA_CA_CERTS`). NuGet KHÔNG dính (OS trust store) nên
  Unit Test chạy đủ. Chi tiết gỡ chặn: `docs/mcp/step-by-step-mcp-guide.md` mục 1.
- FluentAssertions ghim **7.x** (Apache-2.0 free) — KHÔNG nâng 8.x (license thương mại Xceed).
- MCP SQL luôn dùng login `db_datareader` (read-only) — bảo vệ dữ liệu 5.000 POS.

---

## [2026-07-12] Đóng gói pattern External API Integration (SysWebApi/SysWebApiRoute) thành luật thép

**Layer:** Tài liệu (`.claude/rules/`, `CLAUDE.md`) — không đụng code

**Loại:** Pattern mới (governance, không phải code)

**Thay đổi:**
- `.claude/rules/external-api-integration.md` (mới): trace thực tế `LoyaltyController`
  (AkaChain/FMV) + `PaymentController` (GotIT/Urbox) để đóng gói pattern lấy config partner API
  (bảng `SysWebApi`/`SysWebApiRoute`, cache-aside Redis Hash `MD:SysWebApi` TTL 12h qua
  `ICentralMDRepository.GetSysWebApiAsync`, AppService 3-layer) thành luật DO/DON'T bắt buộc —
  trỏ sang `.claude/skills/api/SKILLS.md` cho template code/checklist chi tiết (không lặp lại).
- `CLAUDE.md:57`: router table dòng "external HTTP API" nay trỏ tới cả rule mới +
  `.claude/skills/api/SKILLS.md` (đọc cả 2 — rule là luật bắt buộc, skill là template chi tiết).

**Pattern mới:** Không phải pattern code mới — cơ chế `SysWebApi`/`SysWebApiRoute` + Redis cache đã
chạy production qua Loyalty/Payment từ trước, chỉ có "skill" hướng dẫn chưa có "rule" enforce. Task
này nâng cấp thành rule bắt buộc, router-index trong `CLAUDE.md`.

**Lưu ý cho session sau:**
- Phát hiện bug ngoài phạm vi task (đã hỏi user, quyết định **chỉ ghi chú, KHÔNG sửa**):
  `LoyaltyController.AddTransaction` (dòng 171-173) có `[HttpPost]` nhưng **thiếu**
  `[Route("v2/loyalty/transaction/add")]` — endpoint hiện tại không map đúng URL tài liệu/log ngụ ý
  (effective route là `POST /api` trống). Nếu đụng lại `LoyaltyController`, nhắc lại bug này với
  user để quyết định fix.
- `PartnerEnum.FMV` đã tồn tại (=13) nhưng `AkaChainLoyaltyAppService.cs:49` vẫn dùng literal
  `"FMV"` thay vì `PartnerEnum.FMV.ToString()` — inconsistency có sẵn trong code, không phải mẫu
  chuẩn để copy khi viết AppService partner mới (dùng `GotITService`/`UrboxService` làm ví dụ
  chuẩn thay thế).
- `RedisConst.Redis_Key_SysWebApi`/`Redis_Key_SysWebApiUser` (`POS.Common/Const/RedisConst.cs:21-22`)
  là hằng số dead, lệch với key thật `"MD:SysWebApi"` — chưa dọn, đã ghi vào rule mới để tránh nhầm.

---

## [2026-07-11] LogFilePage `/admin/logs` — refactor master–detail MudTreeView + MudTable

**Layer:** POS.Web
**Loại:** Refactor UI + Pattern mới

**Thay đổi:**
- `src/POS.Web/Services/ILogFileService.cs`: thêm `GetSubfoldersAsync(relativePath, ct)` — liệt kê subfolder **1 cấp** (tách từ `GetDirectoryListingAsync`).
- `src/POS.Web/Services/LogFileService.cs`: implement `GetSubfoldersAsync`, tái dùng nguyên `ResolveSafePath` (không đổi cơ chế chống path traversal). `GetDirectoryListingAsync`/`DownloadLogFileAsync` giữ nguyên.
- `src/POS.Web/Components/Pages/Admin/LogFilePage.razor`: bỏ breadcrumb + nút folder (`MudLink`/`MudButton`) → `MudGrid` 2 cột — trái `MudTreeView<string>` (lazy-load: top-level qua `Items` nạp 1 lần `OnInitializedAsync`, cấp con qua `ServerData` khi expand từng node), phải `MudTable` file (giữ nguyên 100% — cột Tên/Dung lượng/Lần sửa + nút Download qua `JS.SaveAsFileAsync`).

**Pattern mới:** `MudTreeView` lazy-load qua `ServerData` (lần đầu dùng component này trong dự án) → đã cập nhật `.claude/skills/web/02-ui-ux-and-components.md` mục 7 (bảng mapping + "Pattern: MudTreeView lazy-load").

**Quyết định kiến trúc có chủ đích:** ban đầu định đệ quy dựng toàn bộ cây thư mục 1 lần
(`GetFolderTreeAsync`) khi load trang — tự rà soát lại với vai trò Solution Architect trước khi
code, phát hiện cách này mở rộng blast-radius enumerate so với hành vi breadcrumb cũ (chỉ liệt kê
đúng 1 cấp/lần, on-demand), đặc biệt vì `_rootDir` là **thư mục cha** của `Logging:FileLogDirectory`
(có thể chứa nội dung ngoài ý muốn). Đã đổi sang lazy 2 tầng (`Items` cho top-level + `ServerData`
cho từng cấp expand) để giữ đúng risk profile như hiện trạng.

**API MudBlazor 9.5.0 verify bằng reflection + source, không đoán:** XML doc NuGet không đủ chi
tiết signature `ServerData`. Đã dùng scratch console app (`dotnet run` + reflection trên
`MudBlazor.dll`) + đối chiếu source GitHub tag `v9.5.0` (`MudTreeViewItem.razor.cs`) để xác nhận:
`ServerData: Func<T, Task<IReadOnlyCollection<TreeItemData<T>>>>` nhận **Value của node cha**
(không phải `TreeItemData<T>?`), chỉ gọi khi user bấm expand — top-level phải tự nạp qua `Items`.
Bẫy đã gặp lúc build: `TreeItemData.HasChildren` là **computed read-only** → gán trực tiếp lỗi
`CS0200`; sửa bằng chỉ set `Expandable = true`.

**Verify:** `dotnet build src/POS.Web/POS.Web.csproj` → 0 error. `dotnet test tests/POS.ContractTests` → 45/45 passed.
**CHƯA verify runtime** (chưa chạy app thật trên trình duyệt trong sandbox này) — cần test tay hành vi click/expand node trước khi coi hoàn toàn xong, vì `MudTreeView` là component mới, chưa có tiền lệ trong dự án (khác `MudAutocomplete` — từng có sự cố crash circuit thật, đã ghi bài học trong rule).

**Lưu ý cho session sau:** nếu cần thêm cây phân cấp khác (không chỉ log), tái dùng pattern
`Items` (top-level, nạp 1 lần) + `ServerData` (children, lazy theo expand) — KHÔNG đệ quy dựng toàn
cây, xem `.claude/skills/web/02-ui-ux-and-components.md` "Pattern: MudTreeView lazy-load".

---

## [2026-07-11] Format lại `.claude/rules/` theo chuẩn Rule-based Prompting (DO/DON'T)

**Layer:** Tài liệu (`.claude/rules/`) — không đụng code

**Loại:** Refactor tài liệu

**Bối cảnh:** Các file luật trong `.claude/rules/` viết theo văn phong văn xuôi khiến AI khó nhận
diện đâu là "luật thép" bắt buộc. Yêu cầu viết lại theo khuôn `# Rule / 🎯 Context / ✅ DO /
❌ DON'T`.

**Quyết định phạm vi (quan trọng — đọc trước khi động vào `.claude/rules/` lần sau):**
- **Chỉ format lại 3 file "luật thuần"**: `architecture-layers.md`, `backend-api-rules.md`,
  `legacy-migration.md` — viết lại đầy đủ theo khuôn Rule/DO/DON'T (1 file gốc có thể tách thành
  nhiều khối `# Rule:` theo chủ đề con).
- **3 file còn lại GIỮ NGUYÊN cấu trúc đánh số**: `blazor-web-app.md` (§0-§16),
  `mudblazor-flat-ui.md` (mục 0-11), `masterdata-sync.md` — chỉ thêm 1 khối "⚡ Tóm tắt luật thép"
  ở đầu file, không sửa 1 ký tự nào bên dưới. Lý do: 3 file này là đặc tả kỹ thuật dày đặc (bảng
  hex màu, class CSS, SQL script, nhật ký quyết định) được **~30 file khác** (skills/commands/docs)
  tham chiếu theo **số mục cụ thể** (`§0`, `§10.B`, `mục 11`...) — đổi cấu trúc heading sẽ phá vỡ
  các tham chiếu đó. Đã verify bằng `git diff`: 3 file này 0 dòng bị xóa/sửa, chỉ có phần thêm mới.

**Thay đổi:**
- `.claude/rules/architecture-layers.md`: viết lại thành 3 khối `# Rule:` (Layer Structure &
  Dependency Flow / AppService 3-Layer Pattern / Greenfield Feature Organization).
- `.claude/rules/backend-api-rules.md`: viết lại thành 3 khối `# Rule:` (POS.Common Serialization &
  DTO / Controller DI-ModelState-NullValueHandling-ReturnType / Guardrails & Testing).
- `.claude/rules/legacy-migration.md`: viết lại thành 1 khối `# Rule:`, giữ nguyên quy trình 6 bước
  tuần tự trong mục DO.
- `.claude/rules/blazor-web-app.md`, `mudblazor-flat-ui.md`, `masterdata-sync.md`: thêm khối tóm
  tắt DO/DON'T đầu file, trỏ số mục gốc.

**Pattern mới:** Không — thuần tài liệu, không phát sinh code pattern mới.

**Lưu ý cho session sau:** Cụm từ heading gốc được nơi khác trích dẫn nguyên văn (`AppService 3
lớp`, `Khuôn thêm 1 nghiệp vụ mới`, `Cổng chặn trùng lặp`) đã được giữ lại nguyên văn trong bản
mới — nếu sửa tiếp 3 file đã format, tránh đổi các cụm từ này. Nếu sau này cần format nốt
`blazor-web-app.md`/`mudblazor-flat-ui.md`/`masterdata-sync.md`, phải rà lại toàn bộ ~30 file tham
chiếu số mục trước, không làm tùy tiện.

---

## [2026-07-11] Log mỗi lần SINH file master data (giám sát/đối soát MasterDataZipGeneratorWorker)

**Layer:** POS.Common + POS.Infrastructure + POS.Application + POS.Api + POS.Web

**Loại:** Feature

**Bối cảnh:** `MasterDataZipGeneratorWorker` đã chạy trên Ubuntu host nhưng không có cách đối soát
nó có thật sự SINH ra file `.zip` nào không (chỉ có `journalctl` + heartbeat Redis; trên
ProductionHost sink Elasticsearch bị tắt `Nodes:[""]` nên log Kibana chỉ vào file text). Bảng
`MasterDataDownloadLog` chỉ log POS TẢI/XÓA, không log bước SINH.

**Thay đổi:**
- `docs/sql/MasterDataGenerationLog.sql` (mới) + `docs/sql/manifest.json` (order 815): bảng
  `dbo.MasterDataGenerationLog` — 1 dòng/mỗi zip publish. Cột `StoreNo`/`PosNo` `varchar(10)` (đồng
  nhất `Store.No`/`POSTerminal.No`, KHÁC `MasterDataDownloadLog` dùng `SiteCode`/`PosTerminal`).
- `src/POS.Common/Dtos/DataSync/GetMasterDataFileRequest.cs`: thêm `TriggerSource` (nội bộ, không lên HTTP).
- `src/POS.Common/Dtos/DataSync/MasterDataGenerationLogDto.cs` (mới): read DTO cho trang giám sát.
- `src/POS.Infrastructure/.../SyncRepository.cs` (+interface): `InsertGenerationLogAsync` (raw Dapper) + `GetGenerationLogsAsync` (filter, TOP N).
- `src/POS.Application/.../MasterDataSyncService.cs`: ghi log 1 dòng/zip sau mỗi `PublishZipAsync` (fail-safe `LogGenerationSafe`, `FileSizeBytes` từ `FileInfo`, `InstanceId=MachineName`, `DurationMs`) + 1 dòng `Error` trong `catch`. Ghi ở tầng sâu nhất `EnsureMasterDataFileAsync` → phủ MỌI luồng.
- `SyncDataPosService.cs` (2 chỗ) + `SyncDataPosController.cs` (1 chỗ): set `TriggerSource` = `AutoChange`/`ManualSync`/`PosPull`.
- `src/POS.Web/Components/Pages/Ops/MasterDataGenerationLogPage.razor` (mới) + `MainLayout.razor`: trang `/ops/masterdata-generation-log` (OpsAndAbove) + nav nhóm "Nhật ký" + breadcrumb + expand.
- Docs: `centralMD-schema.md`, `CURRENT_STRUCTURE.md`, `masterdata-sync.md`, `ROLLOUT.md`, `deploy/fix_issue_pos-worker-host.md` (thêm mục Giám sát/đối soát + query SQL).

**Pattern mới:** không — tái dùng đúng pattern fail-safe DB logging đã có của `LogDownloadAsync`
(đã ghi tại `.claude/rules/masterdata-sync.md`, không thêm vào SKILLS).

**Verify:** `dotnet build POS.slnx` → 0 error; `dotnet test tests/POS.ContractTests` → 45/45 passed
(gồm SqlManifest + DI test). **CHƯA verify** với DB/Redis/worker thật (dev không có kết nối) —
cần chạy `MasterDataGenerationLog.sql` trên `RPOSMasterData` rồi để worker chạy 1 cycle.

**Lưu ý cho session sau:** bảng ghi **fail-safe** → KHÔNG bắt buộc chạy script trước deploy (chưa
có bảng thì luồng sinh file vẫn chạy, chỉ không thu được log). Đối soát "đã sinh ↔ đã tải" = JOIN
`MasterDataDownloadLog` theo `FileName`. `TriggerSource='AutoChange'` = do worker sinh.

---

## [2026-07-11] Thêm luật "Single File Constraint" cho sửa SP trong docs/sql

**Layer:** Tài liệu cấu hình AI (không đụng `src/`) — `.claude/skills/database/SKILLS.md`, `CLAUDE.md`

**Loại:** Pattern mới (quy tắc quản lý file SQL, không phải code)

**Thay đổi:**
- `.claude/skills/database/SKILLS.md`: thêm section "Single File Constraint — BẮT BUỘC khi sửa/refactor/fix bug SP đã tồn tại" (đặt giữa "Nơi đặt script SQL" và "Template SP ghi dữ liệu") — cấm tạo file `.sql` thứ 2 song song (`_v2`, `_new`...) cho cùng 1 SP để tránh `tools/POS.DbMigrator` áp dụng trùng/xung đột qua `docs/sql/manifest.json`. Định nghĩa 2 chiến lược: Ghi đè tại chỗ (mặc định, Track A idempotent) vs Backup `.sql.bak` + tạo file mới (chỉ Track B rủi ro cao).
- `CLAUDE.md`: mở rộng dòng router SP trong "MỤC LỤC ĐIỀU PHỐI" từ "Tạo stored procedure mới" → "Tạo mới/sửa/refactor/fix bug stored procedure" để dòng này kích hoạt cho cả task sửa SP đã có, không chỉ SP mới.

**Pattern mới:** Single File Constraint (2 chiến lược Ghi đè/Backup theo Track A/B) → đã cập nhật `.claude/skills/database/SKILLS.md`.

**Lưu ý cho session sau:** Không tạo `.claude/rules/database.md` riêng — `SKILLS.md` đã là nguồn sự thật duy nhất cho mọi quy ước SP (đặt tên, TVP, manifest, checklist, và nay thêm Single File Constraint); mọi bổ sung quy tắc SP mới nên vào thẳng file này.

---

## [2026-07-11] Refactor toàn bộ không gian cấu hình AI (.claude/rules, .claude/skills, .claude/commands)

**Layer:** Tài liệu cấu hình AI (không đụng `src/`) — `.claude/rules/`, `.claude/skills/`, `.claude/commands/`, `docs/web/theme/`

**Loại:** Refactor

**Thay đổi:**
- `.claude/skills/database/SKILLS.md`: gỡ Git conflict marker chưa resolve (giữ cả 2 pattern hợp lệ: timeline merge set-based + SP đổi Status dùng UPDLOCK/HOLDLOCK), gộp thêm 7 pattern Repository/SP chuyển từ `api/SKILLS.md` sang.
- `.claude/skills/api/SKILLS.md` (678→159 dòng): tách `middleware-patterns.md` (X-API key middleware, Kestrel MinResponseDataRate) + `file-streaming-patterns.md` (Parallel.ForEachAsync, SHA-256 companion, tách N file theo cờ DB, resolve path SyncDataPos); dedupe OAuth2 token (canonical ở `cache/SKILLS.md`) và Worker Program.cs bootstrap (canonical ở `worker/SKILLS.md`).
- `.claude/skills/worker/SKILLS.md` (417→148 dòng): tách `templates.md` (Template A/B/Pattern C).
- `.claude/skills/web/SKILLS.md` (1299→292 dòng): tách 7 file mới (`security-hardening.md`, `sidebar-nav.md`, `bulk-import-excel.md`, `image-upload.md`, `syntax-highlight-textarea.md`, `trigger-api-task-via-di.md`, `form-input-special-modes.md`), gộp phần còn lại vào `02/03/04-*.md` và `datatable.md`/`form-input.md`; dedupe page pattern/DI table/auth flow (trỏ `rules/blazor-web-app.md`) và badge dot-pill (trỏ `rules/mudblazor-flat-ui.md` §4a).
- `.claude/skills/web/theming.md` (299→145 dòng): bỏ nội dung trùng `rules/mudblazor-flat-ui.md` (Button/Sidebar/Filter panel/Density), chỉ giữ code `PosTheme.cs` + checklist.
- `.claude/rules/mudblazor-flat-ui.md` (510→361 dòng): chuyển lịch sử quyết định/rollout/TODO sang `docs/web/theme/theme-decision-log.md` (file mới).
- 7 file `.claude/commands/`: sửa 2 command sinh code `MudDataGrid` (phải `MudTable`), viết lại `web-ui-confirm-dialog.md` theo đúng pattern `MudMessageBox @ref`, sửa `/gen-hash` → `/web-gen-hash` (2 file), sửa link hỏng "CLAUDE.md §POS.Web mục 0", bỏ bảng MudBlazor v9 chép lại trong `web-ui-chart.md`.
- Thêm YAML front-matter (`name`/`description`) cho toàn bộ ~40 file `.claude/skills/**/*.md`.

**Pattern mới:** Không phải pattern nghiệp vụ mới — đây chính là đợt refactor cấu trúc skill/rule.

**Lưu ý cho session sau:** `.claude/skills/web/SKILLS.md` giờ chỉ là index + luật lõi (292 dòng) —
patterns cụ thể đã có file riêng, tra bảng "Skill con" đầu file trước khi tìm trong SKILLS.md.
Lịch sử/rollout theme không còn nằm trong `mudblazor-flat-ui.md` — xem
`docs/web/theme/theme-decision-log.md`. Đã verify không còn broken reference/conflict marker
trong `.claude/` bằng grep + script PowerShell kiểm tra toàn bộ path — chưa build/test vì đây là
thay đổi thuần tài liệu, không đụng code `src/`.

---

## [2026-07-11] Sửa `LineNo` chưa bracket-quote trong docs/sql (5 file)

**Layer:** Tài liệu SQL (`docs/sql/`) + `.claude/skills/database/SKILLS.md`

**Loại:** Bug fix (script SQL)

**Thay đổi:**
- `docs/sql/SetupCoupon_Read.sql`, `SetupVoucher_Read.sql`: `ORDER BY LineNo` → `ORDER BY [LineNo]`
- `docs/sql/SetupCoupon_Save.sql`, `SetupVoucher_Save.sql`, `SetupVoucher_SaveIssue.sql`: INSERT
  column-list `(ItemNo, LineNo, ...)` → `(ItemNo, [LineNo], ...)`
- `.claude/skills/database/SKILLS.md`: bổ sung ghi chú vào mục "Reserved keyword — BẮT BUỘC
  bracket-quote `[ ]`" đã có sẵn, liệt kê 5 vị trí vừa sửa + ví dụ sai/đúng, nhấn mạnh INSERT
  column-list và `ORDER BY` là nơi dễ sót ngoặc nhất (không có prefix bảng nhắc nhớ).

**Pattern mới:** Không phải pattern mới — củng cố rule đã có sẵn (`LineNo` từng ghi nhận gây lỗi
Msg 156 trong `CentralSaleRepository.cs`). `docs/sql/database/CentralMD.sql`/`CentralSale.sql` đã
bracket-quote đúng từ trước, không cần sửa.

**Lưu ý cho session sau:** Khi copy/viết script SQL mới có cột `LineNo` (đặc biệt trong INSERT
column-list hoặc `ORDER BY`), luôn bracket-quote `[LineNo]` — đã verify bằng Grep quét toàn bộ
`docs/sql` sau khi sửa, không còn `LineNo` trần nào sót lại. Chưa chạy được script thật trên SQL
Server (sandbox không có kết nối DB) để verify hết lỗi Msg 156.

---

## [2026-07-11] Ghi nhật ký fix deploy POS.Worker (Docker + bare-metal) trên `sit-uat-server`

**Layer:** POS.Worker (tài liệu deploy — không đổi code/appsettings thêm trong đợt này)

**Loại:** Tài liệu (runbook sự cố thực tế)

**Bối cảnh:** Tiếp nối task fix SQL connection + setup Model C bare-metal đã ghi ở entry
"Fix POS.Worker không kết nối được SQL Server..." bên dưới — trong quá trình user thực thi runbook
trên host `sit-uat-server`, phát sinh thêm 5 lỗi vận hành liên tiếp (quyền thư mục publish,
systemd 217/USER, systemd 216/GROUP, script thiếu execute bit, nhầm `DOTNET_ENVIRONMENT` cho Model
C). Ghi lại thành 1 file runbook sự cố riêng để lần deploy Model C tiếp theo (host mới) không lặp
lại đúng chuỗi lỗi này.

**Thay đổi:**
- `docs/deploy/fix_issue_pos-worker-host.md` (**file mới**): nhật ký 6 vấn đề — mỗi mục có triệu
  chứng thực tế (log/error message) → nguyên nhân → lệnh fix cụ thể đã áp dụng thành công, kèm
  checklist rút gọn cho lần deploy Model C kế tiếp trên host mới.

**Pattern mới:** Không có — thuần ghi nhận sự cố vận hành, không phát sinh pattern code.

**Lưu ý cho session sau:** Khi deploy Model C (bare-metal, systemd) lần đầu trên 1 host mới, đọc
`docs/deploy/fix_issue_pos-worker-host.md` TRƯỚC — đặc biệt 2 lỗi dễ tái diễn nhất: (1) quên chạy
`deploy/linux/setup-pos-dirs.sh` trước khi tạo user/group cho service (→ `216/GROUP`); (2) file
`.sh` mất execute bit khi transfer lên host không qua `git clone`/`git pull` (→ báo nhầm
"command not found" thay vì "Permission denied", dễ đoán sai là lỗi đường dẫn).

---

## [2026-07-11] LogFilePage (/admin/logs) — đổi từ liệt kê đệ quy sang duyệt thư mục drill-down

**Layer:** POS.Web

**Loại:** Refactor UI + thay đổi hành vi (không đổi contract JSON POS — DTO nội bộ POS.Web)

**Thay đổi:**
- `src/POS.Web/Services/LogFileInfo.cs`: bỏ property `FolderName` (dư thừa — mỗi listing giờ chỉ
  chứa đúng 1 thư mục, vị trí đã thể hiện qua breadcrumb).
- `src/POS.Web/Services/LogFolderInfo.cs` (mới): `record LogFolderInfo(string Name, string RelativePath)`.
- `src/POS.Web/Services/LogDirectoryListing.cs` (mới): `record LogDirectoryListing(string CurrentRelativePath, IReadOnlyList<LogFolderInfo> Folders, IReadOnlyList<LogFileInfo> Files)`.
- `src/POS.Web/Services/ILogFileService.cs`: thay `GetLogFilesAsync()` (quét
  `SearchOption.AllDirectories` toàn bộ root) bằng `GetDirectoryListingAsync(string relativePath = "", ...)`
  — chỉ liệt kê đúng 1 cấp thư mục (folders + files trực tiếp bên trong).
- `src/POS.Web/Services/LogFileService.cs`: thêm helper `ResolveSafePath` dùng chung cho cả
  listing lẫn download (gộp logic path-traversal-guard đang lặp lại, hành vi validate giữ nguyên).
- `src/POS.Web/Components/Pages/Admin/LogFilePage.razor`: mặc định load danh sách thư mục con của
  root; click 1 thư mục (nút dạng "chip" `MudButton` icon Folder) mới load file `.txt`/`.log` bên
  trong qua `GetDirectoryListingAsync(relativePath)`; breadcrumb thủ công bằng `MudLink` (không
  dùng `MudBreadcrumbs` — component đó thiết kế cho `Href`/URL navigation thật, không hợp để chỉ
  đổi state nội bộ trong cùng 1 trang); tải file về máy giữ nguyên qua `JS.SaveAsFileAsync` (JS
  interop có sẵn, không thêm endpoint HTTP nào).

**Pattern mới (nếu có):** Không thêm pattern mới vào SKILLS.md — chỉ là UI refinement dùng đúng
component có sẵn (`MudButton`, `MudLink`, `MudTable`), chưa đủ tính lặp lại để tách thành chuẩn
dùng chung.

**Lưu ý cho session sau:** Verify mới chạy được `dotnet build src/POS.Web/POS.Web.csproj` (0
error) + `dotnet test tests/POS.ContractTests` (45/45 xanh) — **chưa chạy app thật trên trình
duyệt** (sandbox không có `POS_SECRET_KEY`/SQL/Redis), cần tự kiểm tra bằng mắt: vào `/admin/logs`
→ thấy thư mục gốc (không phải file) → click 1 thư mục → thấy file bên trong → bấm Download → file
tải về đúng nội dung. `docs/CURRENT_STRUCTURE.md` **cố ý không cập nhật** — đã xác nhận file đó
chỉ track POS.Common/POS.Application/POS.Infrastructure (kể cả `IWebUserService` của POS.Web
cũng chưa từng được ghi ở đó), nên `ILogFileService`/DTO mới không thuộc phạm vi doc này.

---

## [2026-07-11] Fix POS.Worker không kết nối được SQL Server khi deploy Docker/bare-metal trên Ubuntu

**Layer:** POS.Worker (appsettings — config only, không đổi code C#)

**Loại:** Bug fix (hạ tầng/deploy)

**Bối cảnh:** User deploy `pos-worker-prod` (Model B, Docker) theo đúng
`docs/deploy/pos-worker-ubuntu-guide.md` mục 3, đã truyền `POS_SECRET_KEY` đúng nhưng
`Rpt_ReportSaleDetail_Insert` báo `SqlException: ...server was not found or not accessible`.
Sau đó tiếp tục dựng thêm 1 instance bare-metal chạy `MasterDataZipGeneratorWorker` (Model C) song
song bằng systemd, gặp thêm chuỗi lỗi cấu hình hệ thống (user/group/thư mục publish) trước khi
chạy được ổn định.

**Nguyên nhân + Thay đổi:**
- `src/POS.Worker/appsettings.Production.json`: `ConnectionStrings:*` + `SetDb:DB1` hardcode
  `Data Source=mssql_2019,14333` — `mssql_2019` là service name trong `docker-compose.yml`, chỉ
  resolve được trong network do `docker compose up` tạo ra. Container `pos-worker-prod` chạy bằng
  `docker run` độc lập (không qua compose) nên hostname này không resolve được. Đổi sang
  `host.docker.internal,14333` — khớp pattern đã dùng sẵn cho `RabbitMQ:Host` trong CHÍNH file này
  (route qua `--add-host host.docker.internal:host-gateway` đã có sẵn trong lệnh `docker run` mẫu).
- `src/POS.Worker/appsettings.ProductionHost.json`: giảm `MasterDataZipGenerator.IntervalSeconds`
  300 → 120 theo yêu cầu vận hành (poll watermark mỗi 2 phút thay vì 5 phút).

**Phát hiện quan trọng cho session sau (chưa sửa code, chỉ ghi nhận):** `Program.cs` — cờ
`--run-once` **chỉ hard-code chạy `PosFileImportService`** (Model A), **không đọc `WorkerRoles`**.
Hiện KHÔNG có cách chạy `MasterDataZipGeneratorWorker` (hay bất kỳ worker nào khác) kiểu "chạy 1
lần rồi thoát" qua crontab — muốn định kỳ chỉ có thể chỉnh `IntervalSeconds` bên trong daemon dài
hạn (systemd), không thể chuyển sang mô hình cron thật cho các worker này nếu không sửa
`Program.cs` để tách logic 1 chu kỳ thành method gọi được riêng (giống mẫu `RunOnceAsync` của
`PosFileImportService`).

**Gotcha vận hành khác phát hiện trong quá trình debug (ghi nhận cho lần deploy sau):**
- Lỗi systemd `status=217/USER` / `status=216/GROUP` = user/group khai báo ở `User=`/`Group=`
  trong unit file chưa tồn tại trên host — phải chạy `deploy/linux/setup-pos-dirs.sh` (tạo group
  `posops`) và `useradd posworker` + `usermod -aG posops posworker` TRƯỚC khi start service, đúng
  thứ tự mục 9.3 của `pos-worker-ubuntu-guide.md` (dễ bị bỏ qua nếu chỉ làm theo mục 3.5).
- Nếu dùng path publish tùy biến khác `/srv/pos/app/...` (vd `/var/www/posWeb/worker`) — BẮT BUỘC
  tách thư mục riêng cho mỗi model (Model A cron / ProductionHost daemon) như guide đã cảnh báo ở
  mục 3.5, nếu không lần `dotnet publish` sau sẽ ghi đè binary đang được process khác dùng.
- Khi cần chạy Model C (`MasterDataZipGeneratorWorker`) trên host bare-metal: dùng
  `DOTNET_ENVIRONMENT=ProductionHost` (file này đã có sẵn địa chỉ SQL/Redis đúng cho bare-metal,
  `127.0.0.1`) rồi **override `WorkerRoles` qua `Environment=` trong unit file**
  (`EnableMasterDataZipGenerator=true`, `EnableHeartbeat=true`, tắt hết còn lại) — KHÔNG dùng
  `DOTNET_ENVIRONMENT=Production` như mô tả gốc ở mục 9.4, vì file `Production.json` sau khi sửa
  ở trên đã trỏ SQL sang `host.docker.internal` (chỉ resolve trong container, không resolve được
  cho process bare-metal).

**Pattern mới:** Không có (config fix + ghi nhận giới hạn code hiện tại, không phát sinh pattern
code mới).

**Lưu ý cho session sau:** `docs/deploy/pos-worker-ubuntu-guide.md` mục 9.4 hiện vẫn ghi
`DOTNET_ENVIRONMENT=Production` cho Model C — đã lỗi thời so với thực tế sau khi
`appsettings.Production.json` đổi sang `host.docker.internal`. Nên cập nhật lại guide (đổi hướng
dẫn Model C sang `ProductionHost` + override `WorkerRoles`) trong lần chạm vào file guide gần
nhất, tránh người sau làm theo hướng dẫn cũ và gặp lại đúng lỗi SQL connection.

---

## [2026-07-11] DROP bảng `Internal_Voucher_Legacy` — dọn tài liệu + script SQL liên quan

**Layer:** POS.Common tài liệu (không đổi code C#/logic runtime — bảng đã hết được tham chiếu từ trước)

**Loại:** Refactor tài liệu (theo sau thao tác hạ tầng DBA đã thực hiện)

**Bối cảnh:** Theo kế hoạch rollout `docs/ROLLOUT.md` §D6 (gộp SAP Voucher vào `CpnVchBOMCodeIssue`),
bảng `Internal_Voucher` đã được `sp_rename` thành `Internal_Voucher_Legacy` làm backup tạm từ đợt
go-live trước. User xác nhận đã `DROP TABLE Internal_Voucher_Legacy` trên CentralMD sau thời gian
ổn định — dọn lại tài liệu/script cho khớp thực tế DB.

**Thay đổi:**
- `docs/architecture/centralMD-schema.md`: xóa mục định nghĩa cột `### Internal_Voucher —
  LEGACY/SUPERSEDED`, bỏ khỏi TOC mục "Voucher / Coupon", xóa dòng default-value
  `Internal_Voucher.CreatedDate`; các chỗ còn nhắc tên bảng (lịch sử `usp_Voucher_Create`/
  `GetByCode`/`Redeem`, mục `CpnVchBOMCodeIssue`) đã ghi rõ "(bảng đã DROP)".
- `docs/sql/`: xóa 2 script không còn chạy được vì bảng nguồn đã mất —
  `Internal_Voucher_RenameLegacy.sql` (rename) và `CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql`
  (migrate data từ Internal_Voucher). Không có stored procedure riêng nào cho bảng này (chỉ có
  `usp_Voucher_*` thao tác trên `CpnVchBOMCodeIssue`) nên không còn gì khác cần xóa.
- `docs/sql/manifest.json`: xóa entry `order: 640`/`650` tương ứng 2 script trên (đúng theo
  ghi chú sẵn có trong manifest — "PHẢI XOA entry này khỏi manifest cùng lúc").
- `docs/ROLLOUT.md` §D6 bước 6: đánh dấu ✅ hoàn tất 2026-07-11, thay TODO "chưa chốt ngày DROP"
  bằng xác nhận đã DROP thật + trỏ tới các file đã dọn.
- `docs/CURRENT_STRUCTURE.md`: sửa ghi chú tại `IVoucherCodeRepository` từ "nay LEGACY" thành
  "đã DROP khỏi CentralMD 2026-07-11".

**Pattern mới:** Không có — đây là dọn tài liệu theo sau 1 thao tác DBA, không phát sinh pattern code.

**Lưu ý cho session sau:** Muốn tra lại cấu trúc cột gốc của `Internal_Voucher` (vd đối chiếu dữ
liệu cũ) → dùng `git log`/`git blame` trên `docs/architecture/centralMD-schema.md` tại thời điểm
trước 2026-07-11, KHÔNG còn giữ trong tài liệu hiện tại. Chưa verify trực tiếp trên DB thật (sandbox
không có quyền truy cập SQL Server) — chỉ dựa trên xác nhận của user.

---

## [2026-07-11] Refactor tài liệu UI `.claude/skills/web/` về chuẩn v3 + fix defect thật

**Layer:** POS.Web (tài liệu skill + comment app.css/reports.md — không đổi logic runtime)

**Loại:** Refactor (tài liệu) + Bug fix (merge conflict + doc lỗi thời)

**Bối cảnh:** Rà lại toàn bộ tài liệu UI trong `.claude/skills/web/` theo chuẩn thiết kế v3
(mockup `docs/web/theme/theme_html.html`). Khảo sát cho thấy phần lớn tài liệu **đã ở v3 từ trước**
(mọi mention borderless/16px/Outlined-mọi-nơi đều đã đóng khung "v2 đã loại bỏ"), nhưng phát hiện
2 defect thật cần sửa.

**Thay đổi:**
- `.claude/skills/web/SKILLS.md`: **giải quyết merge conflict Git chưa resolve** (dòng ~1306-1390,
  `<<<<<<< HEAD`/`=======`/`>>>>>>> b710abed`) — giữ CẢ 2 khối (dot-pill badge/timepicker/multiselect
  + bẫy `FormatThousands` khi bulk-import). **Viết lại §"Sidebar nav 3 cấp"** khớp v3/`app.css`
  (L1 = `div.pos-nav-section-label` tĩnh không icon; L2 = `MudNavGroup Class="pos-nav-l2"` icon riêng;
  L3 = `ChevronRight`; QUẢN TRỊ leaf top-level) — thay mô tả v2 cũ ("cấp 2 = ChevronRight giống cấp 3").
  **Giữ nguyên 100%** logic `UpdateExpanded()`/`Match=NavLinkMatch.All`/accordion.
- `.claude/skills/web/theming.md`: snippet `PosTheme.cs` bổ sung `FontFamily` per-variant + rationale
  CSS-var riêng; thêm `LayoutProperties` (`DefaultBorderRadius=12px`/`DrawerWidthLeft=260px`/`AppbarHeight=50px`).
- `.claude/skills/web/ui-polish-standard.md`: §2 + checklist — badge tĩnh dùng `pos-status-chip` là
  chuẩn mặc định, `MudChip` chỉ khi cần tương tác (đồng bộ mudblazor-flat-ui.md §4a).
- `.claude/skills/web/form-input.md`: thêm tham chiếu chéo Button convention v3 ở §6.
- `src/POS.Web/wwwroot/app.css`: sửa **chỉ comment** v2 lỗi thời (dòng ~545 + ~585-597) — 0 dòng CSS
  thực thi thay đổi.
- `.claude/skills/web/reports.md`: 3 hardcode `#1976D2` → `var(--pos-primary)` trong ví dụ pivot.

**Pattern mới:** không — chỉ đồng bộ tài liệu với chuẩn v3 đã có trong `PosTheme.cs`/`app.css`.

**Lưu ý cho session sau:** Tài liệu skills/web đã đồng bộ v3; nguồn sự thật màu/radius/shadow/typography
vẫn là `PosTheme.cs` + `app.css`. CHANGELOG.md và `.claude/skills/web/SKILLS.md` cũ từng dính merge
conflict — SKILLS.md đã sạch, nhưng **CHANGELOG.md vẫn còn nhiều khối conflict** rải rác (xem ghi chú
đầu file) cần dọn thủ công.

---

## [2026-07-11] POS.DbMigrator: `--config` optional (auto-default POS.Api appsettings) + tự đọc `.env` cạnh binary

**Layer:** tools/POS.DbMigrator (không thuộc `src/`, xem `docs/ROLLOUT.md` §D0)

**Loại:** Feature (tiện ích tùy chọn, không đổi hành vi mặc định khi vẫn truyền tường minh)

**Thay đổi:**
- `tools/POS.DbMigrator/RepoRootLocator.cs` (mới): tách logic dò ngược `POS.slnx` dùng chung.
- `tools/POS.DbMigrator/EnvFileLoader.cs` (mới): tự đọc file `.env` cạnh binary
  (`AppContext.BaseDirectory`), set env var **chỉ khi chưa có sẵn** (không override
  export/systemd/`docker -e`).
- `tools/POS.DbMigrator/Program.cs`: `--config` cho `--verify`/`--apply` giờ **optional** — không
  truyền + đang chạy trong git checkout → tự suy ra `src/POS.Api/appsettings.{Environment}.json`
  (`Environment` = `ASPNETCORE_ENVIRONMENT` → `DOTNET_ENVIRONMENT` → mặc định `Production`). Gọi
  `EnvFileLoader` ngay đầu `Main`.
- `tools/POS.DbMigrator/ManifestScriptProvider.cs`: refactor dùng `RepoRootLocator`, hành vi
  `--sql-dir` không đổi.
- Docs: `docs/deploy/pos-dbmigrator-guide.md` (thêm §0 khuyến nghị CI/CD + §2.1/§2.3/§3.3/§4.3 +
  §5.6/§5.7 troubleshooting), `docs/guide-deploy.md` §2.5, `docs/ROLLOUT.md` §D0, `.env.example`.

**Quyết định quan trọng — §0 mới trong `pos-dbmigrator-guide.md`:** cách khuyến nghị chính cho
CI/CD/production vẫn là truyền **tường minh** cả 3 tham số (`--config`, `--sql-dir`,
`POS_SECRET_KEY` qua biến môi trường) — đã verify thật bằng cách publish ra thư mục hoàn toàn ngoài
repo (không `POS.slnx`) và chạy thành công cả 2 kịch bản (plaintext + `enc:...` với token thật). Cơ
chế auto-default `--config`/tự đọc `.env` chỉ là tiện ích phụ cho dev chạy trong git checkout —
**không hoạt động** khi máy đích không có `POS.slnx` (đúng kịch bản CI/CD artifact-only), nên tài
liệu không khuyến khích phụ thuộc vào chúng cho production.

**Lưu ý cho session sau:** nếu cần sửa tiếp `POS.DbMigrator`, luôn verify bằng cách publish ra thư
mục ngoài repo (`dotnet publish ... -o <thư mục tạm ngoài repo>`) để đảm bảo hành vi đúng cho kịch
bản CI/CD-artifact-only, không chỉ test bằng `dotnet run` trong repo (dễ che lấp lỗi phụ thuộc vào
`POS.slnx`/cấu trúc source).

---

## [2026-07-11] Fix deploy POS.Worker: lệch Redis DB, thiếu POS_SECRET_KEY, mount log sai, tách config Docker/bare-metal

**Layer:** POS.Worker, POS.Api (config), docs

**Loại:** Bug fix (3 lỗi vận hành thật) + Config mới (dual-deployment)

**Bối cảnh:** User deploy `POS.Worker` bằng Docker theo `docs/deploy/pos-worker-ubuntu-guide.md`,
gặp liên tiếp 3 vấn đề thật trên môi trường Production:

1. **`/ops/health` báo "Worker: PosSalesConsumer -> offline"` dù container chạy tốt** — nguyên nhân
   `Redis:DefaultDatabase` lệch giữa 3 service (`Api`/`Web` Production = `0`, `Worker` Production =
   `2`) → heartbeat ghi 1 DB, `/ops/health` đọc DB khác, không bao giờ thấy key.
2. **Container Worker crash-loop lúc khởi động** — `appsettings.Production.json` đã mã hóa
   `enc:...` (từ 2026-07-10, Worker đã có hook giải mã) nhưng lệnh `docker run` thiếu
   `-e POS_SECRET_KEY=...`; `docs/deploy/pos-worker-ubuntu-guide.md` còn ghi sai "Worker chưa hỗ
   trợ enc:..., không cần key" — thông tin lỗi thời từ trước khi hook được tích hợp.
3. **File log không xuất hiện trên Ubuntu host** — `Logging:FileLogDirectory` trỏ
   `/srv/pos/logs/worker` (path trong container) nhưng lệnh `docker run` mẫu mount
   `-v $(pwd)/logs:/app/logs` — sai path, log bị ghi vào filesystem tạm của container (nếu ghi
   được — `Dockerfile.worker` cũng chưa từng `mkdir`/`chown` path đó cho user non-root `app`).
4. **Yêu cầu bổ sung**: Worker cần chạy **song song Docker + bare-metal trên cùng host**, do SQL
   Server cũng chạy Docker trên host đó — container cần `host.docker.internal` để với ra SQL,
   process bare-metal cần `127.0.0.1` (không resolve được `host.docker.internal` ngoài container).
   Dùng chung 1 file cho cả 2 ngữ cảnh khiến 1 bên luôn sai địa chỉ SQL.

**Thay đổi:**
- `src/POS.Worker/appsettings.Production.json`: `Redis:DefaultDatabase` 2→0; `ConnectionStrings`/
  `SetDb:DB1` đổi `127.0.0.1,14333` → `host.docker.internal,14333` (đúng ngữ cảnh Docker).
- `src/POS.Api/appsettings.UAT.json`: `Redis:DefaultDatabase` 0→2 (khớp Web/Worker UAT — lệch theo
  chiều ngược lại so với Production).
- `src/POS.Worker/appsettings.ProductionHost.json` (**file mới**): bản dành cho bare-metal —
  `RabbitMQ:Host`/`ConnectionStrings` dùng `127.0.0.1`, `WorkerRoles:EnableHeartbeat=false` (tránh 2
  instance cùng vai trò ghi đè 1 key heartbeat `Worker:Heartbeat:PosSalesConsumer` — key này bị
  hardcode tên, không phân biệt instance), `Logging:FileLogDirectory`/`Elasticsearch:IndexFormat`
  riêng để không lẫn log với bản Docker.
- `docs/deploy/pos-worker-ubuntu-guide.md`: sửa toàn bộ thông tin lỗi thời (enc:/POS_SECRET_KEY,
  mount log, lệnh kiểm tra Redis DB theo môi trường), thêm mục 3.5 (chạy bare-metal song song +
  mẫu systemd unit `pos-worker-prodhost.service`), cập nhật checklist Model B.
- `docs/ROLLOUT.md`: thêm §O10 (Redis:DefaultDatabase phải đồng bộ 3 service) và §O11 (dùng đúng
  file `Production.json`/`ProductionHost.json` theo ngữ cảnh chạy).
- `docs/worker/worker_status.md`: đồng bộ checklist Model B (POS_SECRET_KEY, mount log, DB Redis
  theo môi trường) + mục chẩn đoán nhanh (4.3) thêm 2 nguyên nhân mới phát hiện.
- `.claude/skills/worker/SKILLS.md`: thêm 2 pattern mới — gotcha heartbeat multi-instance (mục
  "Heartbeat → Redis") và pattern tách file cấu hình theo `DOTNET_ENVIRONMENT` khi 1 codebase chạy
  cả Docker lẫn bare-metal trên cùng host phụ thuộc hạ tầng cũng chạy Docker (mục "Đăng ký trong
  Program.cs").

**Pattern mới:** Tách `appsettings.{Environment}Host.json` cho bare-metal khi cùng codebase chạy
song song Docker + bare-metal trên 1 host có hạ tầng phụ thuộc (SQL/Rabbit) cũng chạy Docker — đã
cập nhật `.claude/skills/worker/SKILLS.md`.

**Lưu ý cho session sau:** Mọi lần sửa `Redis:DefaultDatabase` ở BẤT KỲ file nào trong
`POS.Api`/`POS.Web`/`POS.Worker` phải đối chiếu cả 3 service cùng môi trường (§O10) — lỗi này không
lộ ra lúc build/test, chỉ lộ ra lúc runtime qua `/ops/health` hiển thị sai. Toàn bộ phát hiện trong
đợt này (trừ việc tạo `appsettings.ProductionHost.json`) **chưa verify trên môi trường thật của
user** ngoài việc user tự chạy `docker run`/`ls`/xem log và báo lại kết quả — chỉ verify được qua
đọc code + `dotnet build`/`dotnet test` (45/45 xanh) từ phía Claude, không có quyền truy cập Ubuntu
host thật.

---

## [2026-07-11] `/ops/health` — đưa tham số hardcode ra appsettings (configurable theo môi trường)

**Layer:** POS.Application, POS.Infrastructure, POS.Web, POS.Worker

**Loại:** Refactor (configurability) + Bug fix (gap cấu hình môi trường)

**Bối cảnh:** User yêu cầu kiểm tra các mục monitor trên trang `/ops/health` có cấu hình được theo
từng môi trường (Dev/UAT/Prod) không. Phát hiện: Redis/RabbitMQ/SQL connection string đã configurable
đúng, nhưng `HealthCheck:PosApiBaseUrl` **hoàn toàn thiếu** ở `appsettings.UAT.json`/
`appsettings.Production.json` (POS.Web) — do `WebApplication.CreateBuilder` merge config theo key,
UAT/Prod trước đây âm thầm dùng giá trị Dev `http://localhost:8080`. Ngoài ra 5 tham số của
`WorkerHeartbeatService` (interval 15s, TTL 60s/300s, tên worker, tên queue) và 4 tham số của
`HealthCheckService` (ngưỡng mất tín hiệu 45s, timeout mỗi check 8s, SQL connect timeout 5s, HTTP
timeout 5s) là `const` C#, muốn đổi phải rebuild.

**Thay đổi:**
- `src/POS.Application/Features/Common/HealthCheckService.cs`: 4 giá trị hardcode → đọc từ
  `IConfiguration` qua `GetValue("HealthCheck:{Key}", default)` — `StaleAfterSeconds`,
  `CheckTimeoutSeconds`, `SqlConnectTimeoutSeconds`, `HttpTimeoutSeconds`. `CheckSqlAsync`/
  `CheckCentralSaleTemplateAsync` nhận thêm tham số `connectTimeoutSeconds` thay vì đọc const.
- `src/POS.Infrastructure/Workers/WorkerHeartbeatOptions.cs` (mới): POCO bind section
  `"WorkerHeartbeat"` — `WorkerName`, `QueueName`, `IntervalSeconds`, `NormalTtlSeconds`,
  `StoppedTtlSeconds`, default = giá trị hardcode cũ.
- `src/POS.Infrastructure/Workers/WorkerHeartbeatService.cs`: inject `IOptions<WorkerHeartbeatOptions>`
  thay 5 `const`; Redis key tính động `$"Worker:Heartbeat:{options.WorkerName}"` (tránh lệch với
  `HealthCheck:WorkerName` phía đọc nếu Ops đổi tên worker).
- `src/POS.Infrastructure/DependencyInjection.cs`: đăng ký `WorkerHeartbeatOptions` theo đúng
  pattern `MasterDataSyncOptions`/`FileImportOptions` (`GetSection().Get<T>() ?? new T()` →
  `AddSingleton(Options.Create(...))`).
- `src/POS.Web/appsettings.json` + `appsettings.UAT.json` + `appsettings.Production.json`: bổ sung
  4 key mới vào section `HealthCheck`; **thêm mới hoàn toàn** section `HealthCheck` cho UAT/Production
  (gap chính) — `PosApiBaseUrl` để placeholder `<UAT_POS_API_BASE_URL>`/`<PROD_POS_API_BASE_URL>`
  (không đoán giá trị thật). *Cập nhật sau khi merge*: user đã tự điền Production =
  `http://localhost:5001/health`.
- `src/POS.Worker/appsettings.json` + `appsettings.UAT.json` + `appsettings.Production.json`: thêm
  section `"WorkerHeartbeat"` mới (giá trị = hardcode cũ, full-duplicate cả 3 môi trường theo đúng
  tiền lệ `MasterDataZipGenerator`/`FileImport`).
- `docs/guide-deploy.md`, `docs/ROLLOUT.md` (mục O9 mới), `docs/worker/worker_status.md`,
  `docs/CURRENT_STRUCTURE.md`: cập nhật theo thay đổi trên.

**Pattern mới:** Không có pattern mới — tái dùng đúng Options-binding pattern đã có
(`MasterDataSyncOptions`) cho `WorkerHeartbeatOptions`; không cần cập nhật SKILLS.md.

**⚠️ Cần theo dõi (phát hiện SAU khi user tự điền `appsettings.Production.json`):**
`HealthCheck:PosApiBaseUrl` ở Production hiện là `"http://localhost:5001/health"` (đã có sẵn suffix
`/health`) — nhưng `HealthCheckService.CheckWebApiAsync` LUÔN tự nối thêm `/health` vào cuối
(`var url = $"{baseUrl}/health"`), nên URL gọi thật sự sẽ là
`http://localhost:5001/health/health` (double suffix) — gần như chắc chắn 404. Cần xác nhận lại:
nếu `5001` là cổng thật của POS.Api, giá trị đúng phải là `"http://localhost:5001"` (KHÔNG có
`/health`) để khớp cách code tự nối suffix.

**Lưu ý cho session sau:** Khi cấu hình `HealthCheck:PosApiBaseUrl` cho bất kỳ môi trường nào — chỉ
điền phần base URL (scheme + host + port), KHÔNG kèm sẵn path `/health`, vì `CheckWebApiAsync` tự
nối suffix đó.

---

## [2026-07-11] MasterDataSyncService — bỏ hardcode fallback Action, bắt buộc lấy từ DB (fail loud)

**Layer:** POS.Application

**Loại:** Bug fix (vi phạm nguyên tắc "Action luôn lấy từ DB")

**Bối cảnh:** Rà soát theo yêu cầu người dùng sau khi triển khai entry `ZipWatermarkCounter` bên
dưới: `MasterDataSyncService.cs` có 2 hằng số C# `ActionTruncInsert`/`ActionDeleteInsertFallback`
dùng làm **fallback tự động** khi `SyncTableInfo.Action` (đọc từ cột `SyncTableList.Action` do DBA
cấu hình) rỗng/NULL — nghĩa là nếu SP `[SyncTable_Get]` chưa migrate hoặc DBA quên cấu hình, code sẽ
**tự bịa** giá trị Action thay vì phản ánh đúng cấu hình DB. Vi phạm nguyên tắc dự án: Action **tuyệt
đối** phải lấy từ database, không có trường hợp ngoại lệ hardcode trong C#.

**Thay đổi:**
- `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`: xóa hằng số
  `ActionTruncInsert`/`ActionDeleteInsertFallback`; `ActionFor(batchNo)` giờ dùng thẳng
  `entry.Table.Action` — nếu rỗng/NULL, ném `InvalidOperationException` ngay (fail loud, chỉ rõ cần
  chạy `docs/sql/SyncTableList_AddAction.sql` + DBA cấu hình cột `Action`) thay vì âm thầm dùng giá
  trị mặc định. Giữ nguyên hằng số `ActionInsert = "INSERT"` cho batch 2+ (ràng buộc kỹ thuật chia
  batch, không phải "Action chính" nên không tính là hardcode thay thế cấu hình DB).
- `.claude/rules/masterdata-sync.md` mục "Action linh động theo bảng": cập nhật mô tả hành vi mới.

**Đã cố ý KHÔNG đổi** (ngoài phạm vi, cần quyết định riêng): SP `[SyncTable_Get]` nhánh
`@IsChange='W'` vẫn hardcode `'DELETE-INSERT' AS Action` ngay trong SQL (bỏ qua cột `Action` cấu
hình cho bảng đó) — đây là quyết định nghiệp vụ có từ 2026-07-09 cho luồng Web Sync/push 1 POS,
không phải do session này thêm. Đã báo cho user, chưa nhận được xác nhận đổi.

**Verify:** `dotnet build POS.slnx` → 0 Warning, 0 Error. `dotnet test tests/POS.ContractTests` →
45/45 pass.
**CHƯA verify:** hành vi `InvalidOperationException` khi DB thật thiếu Action — cần môi trường SQL
Server thật để gọi thử, sandbox không có.

**Lưu ý cho session sau:** nếu sau này cần thêm giá trị Action mới, **luôn** cấu hình qua
`UPDATE SyncTableList SET Action=...` trên CentralMD — KHÔNG thêm hằng số/fallback trong C#.

---

## [2026-07-11] MasterDataZipGeneratorWorker — watermark chuyển từ Redis Hash sang cột DB (ZipWatermarkCounter)

**Layer:** POS.Common, POS.Infrastructure, POS.Worker

**Loại:** Fix rủi ro dữ liệu (durability) + Refactor

**Bối cảnh:** Watermark của `MasterDataZipGeneratorWorker` (xem entry `[2026-07-10]` bên dưới) ban
đầu lưu ở Redis Hash `Worker:Watermark:MasterDataZip`. Rà soát phát hiện: nếu Hash này bị mất (xóa
tay, Redis restart mất persistence, evict theo `maxmemory-policy`...), code coi đó là "lần chạy đầu
tiên" → seed watermark bằng counter **hiện tại** rồi bỏ qua generate — mọi thay đổi xảy ra trước khi
mất key sẽ **vĩnh viễn không được trigger sinh zip** (lỗi silent, không throw exception). Đề xuất ban
đầu (2 cột `POSNewCounter`/repurpose `POSLastCounter`) bị đánh giá có rủi ro đổi ý nghĩa cột đang
dùng cho write-path khác + phải sửa SP ghi hot-path đang chạy ổn định — chọn phương án thay thế: thêm
cột hoàn toàn mới, không đụng `POSLastCounter`/`usp_SyncTableList_BulkUpdateCounter`.

**Thay đổi:**
- `docs/sql/SyncTableList_AddZipWatermark.sql` (mới, order 850 trong `docs/sql/manifest.json`,
  `runOnce: true, phase: pre-deploy`): `ALTER TABLE SyncTableList ADD ZipWatermarkCounter bigint NULL
  DEFAULT 0` + backfill `= POSLastCounter` 1 lần + TVP `dbo.TVP_ZipWatermarkUpdate` + SP
  `dbo.usp_SyncTableList_BulkUpdateZipWatermark` (idempotent, cùng mẫu
  `usp_SyncTableList_BulkUpdateCounter`) + SP `[SyncTable_Get]` thêm SELECT cột này vào nhánh
  `@IsChange='C'` (nhánh DUY NHẤT dùng — `'A'`/`'W'` không đổi).
- `POS.Common/Dtos/DataSync/SyncTableInfo.cs`: `+ZipWatermarkCounter`.
- `POS.Infrastructure/Repositories/DataSync/{I}SyncRepository.cs`: `+AckZipWatermarkAsync` (DataTable
  → TVP → SP mới, theo mẫu `SyncTrackerRepository.BulkUpdateCounterAsync`).
- `POS.Worker/Workers/MasterDataZipGeneratorWorker.cs`: xóa `WatermarkKey` const + toàn bộ logic
  `KeyExistsAsync`/seed-lần-đầu + `HashGetAllAsync<long>` watermark; so sánh đổi thành
  `tables.Where(t => t.POSLastCounter > t.ZipWatermarkCounter)` (2 cột cùng 1 dòng SQL, không còn
  round-trip Redis); ACK đổi thành `syncRepo.AckZipWatermarkAsync(snapshot)` với `snapshot` =
  `changedTables.ToDictionary(TableName, POSLastCounter)` đã đọc lúc ĐẦU cycle (KHÔNG re-read DB tại
  thời điểm ACK — tránh nuốt mất thay đổi mới bump giữa lúc đọc và lúc ACK). Lock/Quarantine/
  Heartbeat giữ nguyên trong Redis, không đổi.
- `MasterDataZipGeneratorOptions.cs`, `POS.Worker/appsettings.json`,
  `appsettings.Production.json`: xóa `SeedWatermarkOnFirstRun` (không còn cần thiết — backfill 1 lần
  trong migration thay thế hoàn toàn logic seed lúc runtime).
- Doc đồng bộ cùng commit: `.claude/rules/masterdata-sync.md`, `docs/worker/MasterDataZipGeneratorOptions_detail.md`,
  `docs/architecture/centralMD-schema.md` (thêm cột `ZipWatermarkCounter` + `Action` còn thiếu),
  `docs/CURRENT_STRUCTURE.md`, `docs/ROLLOUT.md` §O8, `docs/worker/worker_status.md`.

**Pattern mới:** watermark ACK của 1 background worker nên là cột DB riêng (bền vững), KHÔNG dùng
Redis Hash không TTL làm nguồn sự thật duy nhất cho "đã xử lý tới đâu" — Redis chỉ phù hợp cho
lock/quarantine/heartbeat (mất thì cùng lắm suy giảm tạm thời, không mất dữ liệu).

**Verify:** `dotnet build POS.slnx` → 0 Warning, 0 Error. `dotnet test tests/POS.ContractTests` →
45/45 pass.
**CHƯA verify:** chạy `SyncTableList_AddZipWatermark.sql` trên CentralMD thật + 1 cycle worker thật
để xác nhận ACK ghi đúng giá trị snapshot — sandbox không có SQL Server/Redis thật.

**Lưu ý cho session sau:** `POSLastCounter` vẫn giữ nguyên ý nghĩa/writer cũ (ghi bởi
`SyncTableCounterFlushWorker`) — KHÔNG nhầm với `ZipWatermarkCounter` (chỉ
`MasterDataZipGeneratorWorker` đọc/ghi). Redis key `Worker:Watermark:MasterDataZip` cũ không cần xóa
ngay (phao rollback), dọn tay sau khi xác nhận bản mới ổn định.

---

## [2026-07-11] Bổ sung Model C (Systemd Daemon) cho docs/deploy/pos-worker-ubuntu-guide.md

**Layer:** Documentation (không đổi code)

**Loại:** Cập nhật tài liệu deploy

**Bối cảnh:** `MasterDataZipGeneratorWorker` (đã có sẵn trong `POS.Worker`, toggle
`WorkerRoles:EnableMasterDataZipGenerator`) cần chạy như process dài hạn **bare-metal trên Ubuntu
host**, không qua Docker — không khớp Model A (cron one-shot) hay Model B (Docker) đã tài liệu hoá
sẵn trong `docs/deploy/pos-worker-ubuntu-guide.md`.

**Thay đổi:**
- `docs/deploy/pos-worker-ubuntu-guide.md`: đổi tiêu đề + bảng phân loại mô hình sang 3 cột A/B/C;
  thêm bảng đầy đủ 5 key `WorkerRoles` (đối chiếu `src/POS.Worker/appsettings.json:46-52`) kèm
  khuyến nghị bật/tắt theo từng mô hình; thêm mục 9 (mới) "Model C — Systemd Daemon" gồm 8 bước
  (cài runtime, `dotnet publish` ra thư mục riêng `worker-daemon`, quyền group `posops`, mẫu unit
  file `/etc/systemd/system/pos-worker.service`, lệnh `systemctl`/`journalctl`, kiểm chứng
  heartbeat Redis, update, rollback); mở rộng Checklist + bảng Tham chiếu cho Model C.

**Pattern mới:** không — đây là runbook thao tác, không phải pattern code.

**Lưu ý cho session sau:** unit file mẫu dùng `Type=notify` nhưng **chưa xác nhận** `POS.Worker`
đã tích hợp `Microsoft.Extensions.Hosting.Systemd` — nếu `systemctl start` timeout, đổi
`Type=simple`. Toàn bộ nội dung Model C mới chỉ verify bằng đọc lại file, **chưa test thật** trên
Ubuntu host (sandbox không có môi trường Linux).

---

## [2026-07-10] Giải mã connection string (`enc:`) cho POS.Worker + rút hook thành extension chung

**Layer:** POS.Infrastructure, POS.Api, POS.Web, POS.Worker

**Loại:** Feature (Worker) + Refactor (khử duplicate)

**Bối cảnh:** POS.Api/POS.Web đã giải mã token `enc:` lúc khởi động; POS.Worker vẫn đọc
connection string plaintext. Crypto core (`SecretProtector`) vốn đã nằm ở project chung
`POS.Infrastructure`, nhưng **block hook ~18 dòng** (quét config → đọc `POS_SECRET_KEY` →
`DecryptTokens` → `AddInMemoryCollection`) bị **copy-paste inline ở cả 2 `Program.cs`**.
Thay vì dán bản thứ 3 vào Worker, rút thành 1 extension dùng chung.

**Thay đổi:**
- `src/POS.Infrastructure/Security/ConfigurationSecretExtensions.cs` **(mới)**: extension
  `DecryptEncryptedSecrets(this ConfigurationManager)` — logic giữ **y hệt** block inline cũ.
- `src/POS.Api/Program.cs`: 23 dòng inline → `builder.Configuration.DecryptEncryptedSecrets();`
- `src/POS.Web/Program.cs`: 23 dòng inline → 1 dòng gọi extension.
- `src/POS.Worker/Program.cs`: thêm `using POS.Infrastructure.Security;` + gọi extension ngay sau
  `Host.CreateApplicationBuilder`, TRƯỚC `AddSerilogWithElastic`/`AddInfrastructure`. **Không đụng**
  DI, hosted worker, `WorkerRolesOptions`, nhánh `--run-once`.
- `POS.Worker.csproj`: **không sửa** — đã reference `POS.Infrastructure` sẵn.
- Doc: `docs/architecture/appsetting.md`, `docs/ROLLOUT.md` §C4, `docs/WEB_STATUS.md` (S5),
  `.claude/skills/api/SKILLS.md` (pattern + anti-pattern "đừng copy-paste block hook").

**Điểm kỹ thuật cần nhớ:** extension nhận `ConfigurationManager` (KHÔNG phải `IConfigurationBuilder`)
vì cần **cả đọc** (`AsEnumerable`) **lẫn ghi** (`AddInMemoryCollection`). Đây đúng là kiểu mà cả
`WebApplicationBuilder.Configuration` (Api/Web) và `HostApplicationBuilder.Configuration` (Worker)
cùng expose → 1 extension chạy được cho cả 3 project.

**Appsettings:** `src/POS.Worker/appsettings.Production.json` **cố ý giữ plaintext** — KHÔNG bịa
token `enc:` giả (token giả sẽ khiến app fail-fast lúc khởi động). Cơ chế tự suy ra từ nội dung file:
plaintext → hook no-op. Ops mã hóa thật lúc go-live qua `/admin/encrypt-secret`, và khi đó môi trường
chạy Worker (container `pos-worker` / cronjob host) **phải** có `POS_SECRET_KEY` giống Api/Web.

**Verify:** `dotnet build POS.slnx` → 0 error. `dotnet test tests/POS.ContractTests` → 40/40 passed
(xác nhận DI Api/Web không vỡ sau refactor).
**CHƯA verify:** chạy runtime thật với `enc:` + `POS_SECRET_KEY` (cần DB/Redis/RabbitMQ + khóa thật)
— cả nhánh giải mã thành công lẫn nhánh fail-fast đều chưa chạy end-to-end.

**Lưu ý cho session sau:** thêm project mới cần đọc credential → chỉ gọi
`builder.Configuration.DecryptEncryptedSecrets()` 1 dòng, **tuyệt đối không** copy lại block hook cũ.

---

## [2026-07-10] MasterDataZipGeneratorWorker — watermark-driven incremental sync trigger

**Layer:** POS.Worker (mới), POS.Application, POS.Infrastructure, POS.Common

**Loại:** Feature mới + Pattern mới + Tài liệu kỹ thuật bổ sung (không code)

**Bối cảnh:** `SyncTableList.POSLastCounter` đã được cập nhật bất đồng bộ từ trước
(`SyncTableCounterFlushWorker`) nhưng chưa có gì tiêu thụ tín hiệu đó để tự động sinh lại master
data `.zip` — chỉ chờ POS tự gọi `GetFileFromFTP` hoặc IT Ops bấm tay nút "Đồng bộ dữ liệu".

**Thay đổi:**
- `src/POS.Worker/Workers/{MasterDataZipGeneratorWorker,MasterDataZipGeneratorOptions}.cs` (mới):
  poll `[SyncTable_Get] 'C'` theo chu kỳ, so `POSLastCounter` với watermark Redis Hash
  `Worker:Watermark:MasterDataZip`; bảng đổi → generate zip song song (`Parallel.ForEachAsync`) cho
  mọi POS terminal `Status=1` qua `ISyncDataPosService.PushMasterDataChangeAsync` (mới). Terminal
  lỗi liên tiếp ≥ `QuarantineThreshold` (Redis Hash `Worker:Quarantine:MasterDataZip`) bị loại khỏi
  các lượt sau — tránh 1 terminal hỏng chặn watermark của cả fleet.
- `GetMasterDataFileRequest.ForceRegenerate` (mới, mặc định `false`) + guard 2 chỗ trong
  `MasterDataSyncService.EnsureMasterDataFileAsync` — bỏ qua short-circuit "đã có zip hôm nay" một
  cách tường minh (short-circuit đó vốn đã gần như dead code vì tên zip nhúng mili-giây).
- `ISyncRepository.GetSyncTableCountersAsync` (mới) — bản KHÔNG cache của `GetSyncTablesAsync`, cần
  cho việc phát hiện thay đổi tức thời (bản có cache TTL 1h sẽ làm trễ phát hiện).
- `WorkerRolesOptions.EnableMasterDataZipGenerator` (mới, mặc định `false` — opt-in).
- `POS.Worker/Program.cs`: thêm `AddApplication()` (trước đây chỉ `AddInfrastructure()` — worker
  mới cần `IMasterDataSyncService`/`ISyncDataPosService` từ Application layer).
- `tests/POS.ContractTests/{DependencyInjectionTests.cs,POS.ContractTests.csproj}`: thêm
  `ProjectReference` → `POS.Worker` + test DI riêng
  (`MasterDataZipGeneratorWorker_dependencies_are_registered`) bắt lỗi thiếu `AddApplication()` lúc
  build/test thay vì lúc host khởi động thật.
- appsettings sync: đã đồng bộ `WorkerRoles.EnableMasterDataZipGenerator` + section
  `AppSettings.FtpRootPath`/`MasterDataSync`/`MasterDataZipGenerator` vào cả
  `appsettings.UAT.json` và `appsettings.Production.json` của `POS.Worker` (giá trị tuning copy từ
  DEV, `FtpRootPath` theo đúng quy ước POS.Api/POS.Web: UAT=`/app/ftpbluepos`,
  Production=`/srv/pos/ftpbluepos`).
- Tài liệu: `.claude/rules/masterdata-sync.md` (mục mới), `docs/CURRENT_STRUCTURE.md`,
  `docs/worker/worker_status.md`, `docs/ROLLOUT.md` (§O8 mới), `docs/sql/SyncTableList_AddAction.sql`
  (comment `DEL MD:SyncTableList:C`), `docs/worker/MasterDataZipGeneratorOptions_detail.md` (mới —
  chi tiết luồng gọi SP/sinh file/thư mục đích), và 4 file tài liệu tổng hợp kỹ thuật cho các
  hosted service còn lại của `POS.Worker` (mới): `docs/worker/{PosFileImportWorker,
  PosSalesConsumerWorker,Rpt_ReportSaleDetail_Insert,WorkerHeartbeatService}.md` — logic chi tiết,
  ràng buộc DB (SP `Sale_InsertDataByOrder_KAFKA`/`Rpt_ReportSaleDetail_Insert`...), và gotcha.

**Pattern mới:** "Poll + fan-out song song + quarantine" → đã thêm vào
`.claude/skills/worker/SKILLS.md` (Pattern C) kèm ngoại lệ mới về vị trí đặt worker khi cần
Application service (`POS.Worker/Workers/` thay vì `POS.Infrastructure/Workers/`).

**⚠️ Rollout blocker phát hiện thêm (chưa tự sửa, cần người vận hành xác nhận)**: service
`pos-worker` trong `docker-compose.yml` (Model B) **không mount** thư mục `ftpbluepos` — nếu bật
`EnableMasterDataZipGenerator=true` qua Docker mà không thêm volume mount tương tự `webapp`
(`-\srv\pos\ftpbluepos:/app/ftpbluepos`), worker khởi động bình thường không lỗi nhưng zip sinh ra
nằm trong thư mục ephemeral, tách biệt hoàn toàn khỏi nơi POS.Api phục vụ — lỗi âm thầm, khó phát
hiện. Đã ghi chi tiết + cảnh báo vào `docs/ROLLOUT.md` §O8 mục 0.

**Verify:** `dotnet build POS.slnx` 0 error; `dotnet test tests/POS.ContractTests` 40/40 pass (gồm
1 test DI mới). **CHƯA verify end-to-end** (cần SQL Server CentralMD + Redis + FtpRootPath ghi
được — không có trong sandbox phát triển). 4 file tài liệu tổng hợp cho 4 worker còn lại cũng dựa
hoàn toàn trên đọc code tĩnh, chưa quan sát runtime thật.

**Lưu ý cho session sau:** heartbeat `Worker:Heartbeat:PosSalesConsumer` thực chất bị **2 worker
khác nhau ghi chung** (`PosSalesConsumerWorker` + `Rpt_ReportSaleDetail_Insert` cùng ghi vào
`WorkerHealthState` singleton) — lỗi SQL report job có thể hiển thị nhầm thành "mất kết nối
RabbitMQ" trên `/ops/health`. Xem `docs/worker/WorkerHeartbeatService.md` §3 trước khi debug bất kỳ
alert nào liên quan tới heartbeat `PosSalesConsumer`.

---

## [2026-07-10] Fix bug cột "Mã CTKM" hiển thị trùng lặp trên OffersPage.razor

**Layer:** POS.Web

**Loại:** Bug fix

**Bối cảnh:** User báo trang `/promotion/offers` hiển thị rất nhiều dòng có "Mã CTKM" giống hệt
nhau (vd `1000000424` lặp lại 6 dòng) nhưng `Tên CTKM`/`OfferType`/ngày tháng khác nhau hoàn toàn —
nghi ngờ ban đầu là bug ở stored procedure `GetPromotionOfferHeaderList`.

**Nguyên nhân thật (đã xác nhận qua `git diff` + đối chiếu `docs/architecture/centralMD-schema.md`):**
đây là regression trong chính đợt "dọn cột trùng lặp" ở entry bên dưới (cùng ngày) — khi gộp 2 cột
"Bonus Buy" (`BonusbuyNo`) và "Promotion No" (`PromotionNo`) thành 1 cột "Mã CTKM", đã chọn nhầm
bind vào `PromotionNo`. Theo schema, `OfferHeader.No` (map DTO field `BonusbuyNo`) mới là khóa
nghiệp vụ thật của 1 CTKM (`NOT NULL`, auto-gen chống trùng từ `6000000001`, dùng làm khóa join
`OfferBuy/OfferGet/OfferBenefits/OfferSite.OfferNo`, dùng cho detail dialog + Deactive + `ORDER BY`
mặc định của SP) — còn `PromotionNo` chỉ là field phụ nullable, không có cơ chế chống trùng, không
được FK nào tham chiếu. Nhiều offer khác nhau (khác `No`) vô tình share cùng `PromotionNo` → hiển
thị như bị trùng lặp. **SP `GetPromotionOfferHeaderList` trả dữ liệu đúng, không có lỗi.**

**Thay đổi:**
- `OffersPage.razor`: cột "Mã CTKM" (RowTemplate) đổi từ `@context.PromotionNo` → `@context.BonusbuyNo`.
- `OffersPage.razor` (Excel export `BuildXlsx`): header cột 1 đổi `"Promotion No"` → `"Mã CTKM"`,
  giá trị đổi từ `e.PromotionNo` → `e.BonusbuyNo` (đồng bộ với bảng UI).
- Không sửa SP/DB. `docs/sql/GetPromotionOfferHeaderList_FixDuplicateRows.sql` (script có sẵn, xử
  lý 1 bug SQL khác — `LEFT JOIN OfferSite` fan-out theo store) là fix hợp lệ cho vấn đề khác, giữ
  nguyên, không liên quan tới bug này.

**Verify:** `dotnet build src/POS.Web/POS.Web.csproj` 0 error; `dotnet test tests/POS.ContractTests`
39/39 pass. **Chưa verify bằng mắt trên DB thật** (sandbox thiếu DB/Redis).

**Lưu ý cho session sau:** trong module Promotion/Offer, "Mã CTKM" luôn luôn là `No`/`BonusbuyNo`/
`BBYNR` — KHÔNG BAO GIỜ dùng `PromotionNo` làm định danh hiển thị chính (field này nullable, không
unique). Trang song sinh `PromotionSetupPage.razor` đã đúng convention này từ đầu (bind `No`), dùng
làm tham chiếu nếu cần kiểm tra lại quy ước.

---

## [2026-07-10] Chuẩn hóa filter Promotion/Offers + Promotion/Setup theo ngày, dọn UI

**Layer:** POS.Web, POS.Common, POS.Infrastructure

**Loại:** Bug fix + Feature + UI standard

**Thay đổi — OffersPage.razor (`/promotion/offers`):**
- Bỏ cột "Bonus Buy" khỏi datatable + Excel export, giữ 1 cột mã duy nhất "Mã CTKM"
  (~~`=PromotionNo`~~ **bind nhầm field — đã sửa lại thành `=BonusbuyNo`**, xem entry fix bug bên trên).
- Fix bug: thứ tự `<MudTh>` (header) và `<MudTd>` (row) bị lệch giữa cột Trạng thái/Hình thức bán
  (MudTable render theo đúng thứ tự khai báo, không tự match theo tên) → đã swap lại cho khớp.
- Cột Action bỏ `width:64px` cố định → auto-resize theo số icon-button thực tế (1-2 nút).
- Filter "Mã CTKM" đổi tên từ "Bonus Buy/Promotion No", tìm đúng field `[No]` (đã đúng từ trước,
  chỉ đổi label). Thêm filter "Từ ngày" (mặc định hôm nay−90 ngày) → SP `GetPromotionOfferHeaderList`
  thêm `@FromDate date=NULL`, lọc `H.[EndingDate] >= @FromDate` trên **cả 12 branch** của SP.
  **Không có "Đến ngày"** — quyết định có chủ đích: nhiều CTKM `EndingDate` đặt xa tới 2030, lọc
  thêm mốc cuối sẽ vô tình ẩn các CTKM đó. Script: `docs/sql/GetPromotionOfferHeaderList_AddDateRangeFilter.sql`
  (DBA cần chạy thủ công trên CentralMD — sandbox không có quyền truy cập DB để tự apply).

**Thay đổi — PromotionSetupPage.razor (`/promotion/setup`):**
- List mode thêm 4 filter mới: Từ ngày, Mã sản phẩm, Loại CTKM, Trạng thái (STATUS 0/1/2 = Đang áp
  dụng/Lên kế hoạch/Ngưng áp dụng — tái dùng đúng 3 giá trị của dropdown Trạng thái ở editor).
  Giữ nguyên filter cũ Mã CTKM/Tên CTKM/Trạng thái duyệt (IsApprove) — 2 khái niệm Trạng thái
  (STATUS) và Trạng thái duyệt (IsApprove) tách biệt, không gộp.
- Nguồn dữ liệu **khác** OffersPage: query trực tiếp `SetupPromotionHEADER` (+ `EXISTS` join
  `SetupPromotionBUY.MAT_NR`/`SetupPromotionGET.MATERIALCODE` cho filter Mã sản phẩm) qua Dapper
  trong `PromotionRepository.GetSetupListAsync` — không phải Stored Procedure, nên sửa trực tiếp
  trong C# (không cần script SQL riêng như OffersPage).
- `PromotionSetupListFilter` (POS.Common) thêm `ItemNo`, `OfferType`, `Status`, `FromDate`.

**Thay đổi — Toàn app:**
- Bỏ `text-transform:uppercase` khỏi `.mud-input-label-inputcontrol` (`app.css`) — label input
  (vd "Loại CTKM") giờ hiển thị chữ thường trên **mọi trang**, đảo ngược quyết định uppercase cũ
  (audit Typography 2026-07-06) theo phản hồi UX. Cập nhật `.claude/rules/mudblazor-flat-ui.md` §11.

**Pattern mới:** "Lọc theo overlap khoảng ngày chỉ dùng 1 mốc `FromDate`" — khi filter theo khoảng
ngày mà bản ghi có thể có `EndingDate` đặt xa trong tương lai (nhiều năm), KHÔNG thêm điều kiện
`ToDate` (dễ vô tình loại các bản ghi vẫn còn hiệu lực) — chỉ lọc `EndingDate/VALIDTO >= @FromDate`.
Áp dụng nhất quán ở cả 2 trang Promotion dù nguồn dữ liệu khác nhau (SP vs raw SQL).

**Lưu ý cho session sau:**
- ~~Build POS.Web tại thời điểm ghi log này đang lỗi do `PromotionSetupPage.razor` tham chiếu
  `HeaderFromTime`/`HeaderToTime` chưa khai báo~~ — đã tự hết: đó là property do task
  "Chỉnh sửa nâng cao PromotionSetupPage" (entry ngay bên dưới) khai báo, chạy song song trong
  cùng ngày. Build/test cả 2 task đều xanh sau khi cả 2 hoàn tất — xem entry bên dưới.
- SP `GetPromotionOfferHeaderList` có 12 branch lặp gần giống nhau (4 tổ hợp `@ItemNo`×`@Status`
  × 2 nhánh `@Exp`) — khi cần thêm tham số/điều kiện mới, luôn đếm lại occurrence của 1 chuỗi WHERE
  đặc trưng (vd `@StoreNo`) để xác nhận sửa đủ cả 12 branch, tránh sót.

---

## [2026-07-10] PromotionSetupPage — chỉnh UI nâng cao + fix bug SetupGroupItem

**Layer:** POS.Web, POS.Common, POS.Infrastructure

**Loại:** Feature + Bug fix

**Thay đổi — PromotionSetupPage.razor (`/promotion/setup`):**
- Khối form cố định: `OpenNewAsync` mặc định `_header.SalesType = "Tại chỗ"` (tìm theo `Text`
  chứa "Tại chỗ", fallback option đầu — cùng pattern `PriceSetupPage.razor:288-291`). Xóa 2
  `MudAlert` hướng dẫn "[Sản phẩm mua]"/"[Sản phẩm khuyến mãi]".
- Tab "Thông tin chung": Từ giờ/Đến giờ đổi `MudTextField` → `MudTimePicker`
  (`Editable="true" TimeFormat="HH:mm" AmPm="false"`), bọc qua 2 property `HeaderFromTime`/
  `HeaderToTime` (`TimeSpan?`) chuyển đổi 2 chiều với `_header.FromTime`/`ToTime` (string "HH:mm",
  DTO không đổi). Verify tham số qua reflection thật trên `MudBlazor.dll` 9.5.0 trước khi dùng
  (không đoán API).
- Tab "Cài đặt nâng cao": Label "Số lần được áp dụng" → "Vòng lặp tính KM". Ẩn field "Độ ưu tiên"
  khỏi UI (`PriorityBBY` giữ mặc định `=1`, không đổi khi sửa CTKM cũ — chỉ ẩn markup). "Ngày áp
  dụng trong tháng" đổi từ `MudNumericField` (1 số) → `MudSelect MultiSelection="true"` (31 ngày),
  lưu field mới `PromotionSetupHeaderDto.ApplyDaysOfMonth` (`List<int>`).

**Quyết định kiến trúc quan trọng (đã xác nhận với user trước khi code):** SP legacy đang chạy
**production** `Setup_Promotion_Insert` (publish draft → `OfferHeader` cho POS đọc, KHÔNG thuộc
repo này, KHÔNG được sửa) có dòng cứng `Convert(int, H.NUMOFDAYS)` khi Duyệt CTKM — đổi cột
`NUMOFDAYS` cũ sang JSON array sẽ crash ngay bước Duyệt. Quyết định: **giữ nguyên 100%**
`NumOfDays`/`NUMOFDAYS` (cột + tham số `@NumOfDays` trong `usp_SaveSetupCTKMAll` không đổi gì),
thêm cột **MỚI** hoàn toàn tách biệt `NUMOFDAYSLIST` (nvarchar(200), JSON array) — chỉ Dashboard
đọc/ghi, KHÔNG publish sang `OfferHeader` (enforcement nhiều-ngày ở POS/CentralSale, nếu cần, là
quyết định/khối lượng công việc riêng, ngoài phạm vi đợt này).

**Thay đổi — code/DB đi kèm quyết định trên:**
- `POS.Common/Dtos/Promotion/PromotionSetupDto.cs`: thêm `ApplyDaysOfMonth` vào
  `PromotionSetupHeaderDto`, giữ nguyên `NumOfDays`.
- `PromotionRepository.cs`: `GetSetupDetailAsync` đọc thêm 1 SELECT riêng `NUMOFDAYSLIST` (không
  map trực tiếp vào DTO vì tên cột không khớp property, tránh Dapper ép kiểu sai), parse qua helper
  mới `ParseDaysOfMonth` (JsonConvert, lọc 1-31, dedupe, sort). `SaveSetupAsync` thêm tham số
  `@NumOfDaysList` (JSON serialize), giữ nguyên dòng `@NumOfDays` cũ.
- `docs/sql/SetupPromotion_AddNumOfDaysList.sql` (mới): `ALTER TABLE` thêm cột, idempotent.
- `docs/sql/SetupPromotion_Save.sql`: BẢN SỬA LẦN 4 — thêm tham số `@NumOfDaysList nvarchar(200) =
  ''`, ghi vào UPDATE + 2 nhánh INSERT.
- `docs/architecture/centralMD-schema.md`, `docs/CURRENT_STRUCTURE.md`: cập nhật cột/field mới.
- `docs/ROLLOUT.md` §D11: **BẮT BUỘC** chạy `SetupPromotion_AddNumOfDaysList.sql` rồi
  `SetupPromotion_Save.sql` (bản mới) trên CentralMD, ĐÚNG THỨ TỰ, trước khi dùng field mới —
  thiếu bước 1 → lỗi `Invalid column name 'NUMOFDAYSLIST'` khi Lưu.

**Bug fix riêng — "Invalid object name 'dbo.SetupGroupItem'"** (modal "Cài đặt nhóm sản phẩm",
`ItemGroupSetupDialog.razor`, mở từ dòng Buy/Get "Nhóm SP" tại `/promotion/setup`): bảng
`dbo.SetupGroupItem` thiếu trên môi trường test — định nghĩa gốc chỉ nằm trong file dump legacy
`src/legacy/Database/CentralMD.sql` (chỉ đọc), chưa từng có script CREATE TABLE độc lập cho DBA
chạy. Code C#/Razor (`PromotionRepository`, dialog) đã đúng từ trước — **không sửa gì ở code**.
Thêm `docs/sql/SetupGroupItem_CreateTable.sql` (idempotent) + cập nhật header
`docs/sql/SetupGroupItem_Save.sql` + `docs/ROLLOUT.md` §D1b ghi rõ thứ tự chạy.

**Pattern mới:** `MudTimePicker` cho ô giờ "HH:mm" tự ép định dạng khi gõ tay; `MudSelect
MultiSelection` cho chọn nhiều giá trị rời rạc (bẫy: `MultiSelectionTextFunc` nhận
`IReadOnlyList<string>`, không phải `IReadOnlyList<T>`) — đã thêm `.claude/skills/web/SKILLS.md`.
Cũng ghi nhận nguyên tắc: khi 1 cột/tham số DB được 1 SP legacy **đang chạy production** (ngoài
repo, không sửa được) tiêu thụ trực tiếp qua `Convert`, KHÔNG đổi kiểu/ngữ nghĩa cột đó — luôn
thêm cột mới tách biệt.

**Lưu ý cho session sau:** Đây là task chạy song song với task "Chuẩn hóa filter Promotion/Offers +
Promotion/Setup" (entry bên trên) trên cùng file `PromotionSetupPage.razor` — 2 task sửa 2 khu vực
khác nhau (list-mode filter vs editor-mode tab), không đụng nhau, cả 2 đều verify xanh sau khi
hoàn tất. Build POS.Web + `dotnet test tests/POS.ContractTests` (39/39) đã xanh tại 1 thời điểm xác
nhận; lần build sau đó gặp lỗi MSB3027 (file DLL bị khóa bởi Visual Studio đang chạy POS.Web) — đây
là lỗi môi trường (file lock), KHÔNG phải lỗi code, không cần sửa. **CHƯA VERIFY UI/DB thật**
(sandbox thiếu `POS_SECRET_KEY`/DB/Redis) — cần DBA chạy đủ 2 script SQL (`SetupPromotion_Add
NumOfDaysList.sql` + `SetupPromotion_Save.sql`) và 2 script SQL fix SetupGroupItem trên CentralMD,
rồi test lại bằng mắt trên môi trường thật.

---

## [2026-07-09] Distributed throttle (Redis) cho sinh file master data .zip

**Layer:** POS.Api, POS.Application, POS.Infrastructure, POS.Common

**Loại:** Feature + Pattern mới

**Bối cảnh:** `MasterDataSyncService.EnsureMasterDataFileAsync` (sinh file master data .zip cho
POS) chỉ có khóa per-terminal trong-process (`ISyncFileLock`) — không giới hạn tổng số lượt sinh
đồng thời trên toàn cụm (nhiều instance API), có thể quá tải SQL Server/CPU khi nhiều POS cùng
sync giờ cao điểm. Thêm Distributed Throttle dựa trên Redis, giới hạn tối đa N=3 lượt chạy đồng
thời.

**Thay đổi:**
- `POS.Infrastructure/Cache/{I}RedisManager.cs`: thêm `TryAcquireSlotAsync`/`ReleaseSlotAsync` —
  sliding-window đếm bằng Sorted Set (`ZREMRANGEBYSCORE` dọn slot quá hạn + `ZCARD` đếm + `ZADD`
  nếu còn chỗ), atomic qua 1 Lua script (`ScriptEvaluateAsync`), không có race condition (TOCTOU)
  giữa nhiều request đồng thời.
- `POS.Infrastructure/Redis/{I}RedisService.cs`: thin wrapper delegate 2 method trên.
- `POS.Common/Const/RedisConst.cs`: thêm `Redis_Key_CreateMasterDataSlots = "MD:CreateMasterData:Slots"`.
- `POS.Infrastructure/Files/MasterDataSyncOptions.cs`: thêm `MaxConcurrentGeneration` (mặc định 3),
  `ThrottleStaleAfterSeconds` (mặc định 600).
- `POS.Application/Features/DataSync/MasterDataThrottleException.cs` (mới).
- `POS.Application/Features/DataSync/MasterDataSyncService.cs`: inject `IRedisService`, acquire
  slot throttle TRƯỚC khóa per-terminal, release trong `finally` (không truyền `ct` — đảm bảo nhả
  được kể cả khi request bị hủy). Đặt tại đúng 1 điểm nghẽn cổ chai chung
  (`EnsureMasterDataFileAsync`) nên bảo vệ được cả 2 luồng gọi: `GetFileFromFTP` (nhánh ALL) và
  `PushStartOfDayDataAsync` (Web Sync push 1 POS từ `PosMapPage.razor`).
- `POS.Api/Controllers/SyncDataPosController.cs`: bắt riêng `MasterDataThrottleException` trong
  `GetFileFromFTP`, trả qua `HttpResponseData` sẵn có — **giữ nguyên contract** (HTTP 200, trạng
  thái thật `Status=429` nằm trong body JSON), KHÔNG đổi HTTP status code thật.
- `appsettings.json` (base/DEV) + `appsettings.UAT.json` + `appsettings.Production.json` (POS.Api):
  thêm 2 key vào section `MasterDataSync` có sẵn — tuning, copy y giá trị DEV.
- `.claude/skills/cache/SKILLS.md`: thêm Pattern 8 "Distributed throttle (sliding-window ZSET)".
- `docs/CURRENT_STRUCTURE.md`, `.claude/rules/masterdata-sync.md`: cập nhật cùng commit.

**Pattern mới:** Distributed throttle (sliding-window ZSET, atomic Lua) → đã cập nhật
`.claude/skills/cache/SKILLS.md` Pattern 8 (bổ sung cạnh Pattern 6 — distributed lock).

**Lưu ý cho session sau:** Pattern throttle này generic (`setKey`/`slotId`/`maxSlots`/`staleAfter`),
tái dùng được cho tác vụ tốn tài nguyên khác cần giới hạn N-concurrent xuyên nhiều instance —
không chỉ riêng master data. Chưa verify runtime bằng Redis thật trong sandbox (không có Redis
server) — chỉ verify qua `dotnet build` (0 error) + `dotnet test tests/POS.ContractTests` (39/39
pass); cần test thủ công sau deploy (xem hướng dẫn trong `.claude/rules/masterdata-sync.md`).

---

## [2026-07-09] SyncTableList.POSLastCounter — cập nhật bất đồng bộ (Channel + BackgroundService) + rollout SalesPrice

**Layer:** POS.Infrastructure, POS.Api, POS.Web, POS.Common

**Loại:** Feature + Pattern mới

**Bối cảnh:** `SyncTableList.POSLastCounter` trước đây **chưa từng được ghi** ở bất kỳ đâu (SP
`[SyncTable_Get]` chỉ SELECT) — luồng sync master data cho POS luôn full-resync `@POSLastCounter=0`.
Tính năng mới để về sau có thể chuyển dần sang incremental sync.

**Thay đổi — Core infra:**
- `POS.Infrastructure/Sync/` (mới, namespace `POS.Infrastructure.Sync`):
  `ISyncTableTrackerService`/`SyncTableTrackerService` (Channel bounded in-process, Singleton,
  `Track(tableName, counter)` non-blocking); `SyncTableCounterFlushWorker` (BackgroundService, drain
  Channel định kỳ `FlushIntervalSeconds` mặc định 5s, coalesce Max theo bảng, heartbeat Redis
  `Worker:Heartbeat:SyncTableCounterFlush-{process}` tái dùng DTO `WorkerHeartbeat` có sẵn);
  `SyncTableTrackerOptions` (bind section `"SyncTableTracker"`).
- `POS.Infrastructure/Repositories/DataSync/{I}SyncTrackerRepository.cs` (mới): `BulkUpdateCounterAsync`
  qua TVP `dbo.TVP_SyncCounterUpdate` → SP `usp_SyncTableList_BulkUpdateCounter`
  (`docs/sql/SyncTableList_BulkUpdateCounter.sql`, UPDATE idempotent `WHERE Counter > POSLastCounter`).
- `DependencyInjection.cs`: đăng ký Singleton tracker + Scoped repo.
- `POS.Api/Program.cs` + `POS.Web/Program.cs`: thêm `AddHostedService<SyncTableCounterFlushWorker>()`
  **trực tiếp** (KHÔNG qua `WorkerRolesOptions`) — ngoại lệ có chủ đích vì Channel in-memory chỉ sống
  đúng tiến trình ghi master data, mà cả `POS.Api` lẫn `POS.Web` đều ghi (POS.Web có CRUD pages inject
  thẳng Repository).
- `appsettings.json` (Development + UAT + Production, cả POS.Api và POS.Web): thêm section
  `SyncTableTracker` (`FlushIntervalSeconds`, `ChannelCapacity`) — tuning, copy y giá trị DEV.

**Thay đổi — Rollout Track() cho 3/12 bảng (Pilot A/B/C):**
- `CentralMDRepository.CreateProductAsync` (SP `usp_Product_Save`, thêm `@OutItemCounter`/
  `@OutBarcodeCounter` OUTPUT — `docs/sql/Product_Save.sql`) → Track `Item`, `Barcodes`.
- `CentralMDRepository.SaveProductLockAsync` (raw SQL, thêm `OUTPUT INSERTED.Counter`, gom Max cả
  batch, Track 1 lần) → Track `ItemBlock`.
- `PriceRepository.SaveAsync`/`UpdatePriceAsync`/`SoftDeletePriceAsync` (SP `usp_SetupSalePrice_Save`/
  `usp_SalesPrice_UpdatePrice`/`usp_SalesPrice_SoftDelete`, script mới
  `docs/sql/SalesPrice_AddCounterOutput.sql`) → Track `SalesPrice`. `PriceSaveResult` thêm field
  `Counter` (nội bộ, không thuộc contract JSON khoá POS). Ca đặc biệt: nhánh update của
  `usp_SetupSalePrice_Save` ủy quyền SP legacy production `Setup_SalePrice_Get_ALL` (không sửa được,
  không có OUTPUT) → giải pháp đọc lại `SELECT MAX(Counter)` sau khi mọi nhánh ghi xong.

**Pattern mới:** "Track Counter bump vào SyncTableList.POSLastCounter bất đồng bộ (Channel +
BackgroundService)" → đã cập nhật `.claude/skills/api/SKILLS.md`.

**Tài liệu:** `.claude/rules/masterdata-sync.md` (mục cơ chế mới + bảng rollout 3/12 write-path),
`docs/web/sync_data/sync_status.md` (tổng kết + checklist tiến độ dạng bảng), `docs/CURRENT_STRUCTURE.md`
(cây `Sync/` + bảng DI), `docs/WEB_STATUS.md`.

**Verify:** `dotnet build POS.slnx` 0 error, `dotnet build src/POS.Web/POS.Web.csproj` 0 error,
`dotnet test tests/POS.ContractTests` 39/39 xanh. **CHƯA VERIFY runtime** (sandbox thiếu
`POS_SECRET_KEY`/DB/Redis) — 3 script SQL mới (`SyncTableList_BulkUpdateCounter.sql`, bản sửa
`Product_Save.sql`, `SalesPrice_AddCounterOutput.sql`) **chưa chạy trên DB thật**, cần DBA áp dụng
trên CentralMD trước khi test.

**Lưu ý cho session sau:** Còn 9/12 bảng chưa rollout Track() — theo checklist
`docs/web/sync_data/sync_status.md`, bám đúng 1 trong 3 mẫu Pilot A (SP + OUTPUT param), Pilot B
(raw SQL + `OUTPUT INSERTED.Counter`), Pilot C (SP ủy quyền SP legacy không sửa được → đọc lại
`MAX(Counter)`). 2 gap phát hiện ngoài phạm vi: `UpdatePosTerminalAsync`/`POSDataSetup` insert/update
không bump `Counter` — cần quyết định riêng trước khi rollout tới các bảng đó. Heartbeat Redis mới
chỉ ghi, chưa tích hợp `/ops/health` (cần generalize `HealthCheck:WorkerName` từ 1 string → mảng).

---

## [2026-07-09] MasterDataDownloadLog: log xóa file + cột "MasterData" trên PosMapPage + chuyển menu "Thiết bị POS" sang VẬN HÀNH

**Layer:** POS.Api, POS.Application, POS.Infrastructure, POS.Common, POS.Web

**Loại:** Feature + Refactor (menu/quyền)

**Thay đổi:**
- `docs/sql/MasterDataDownloadLog.sql`: ALTER TABLE idempotent (`COL_LENGTH` guard) thêm
  `DeletedAt datetime NULL` + `DeleteStatus varchar(20) NULL`.
- `POS.Infrastructure/Repositories/DataSync/{ISyncRepository,SyncRepository}.cs`: thêm
  `UpdateDeleteLogAsync(siteCode, posTerminal, fileName, deleteStatus, deletedAt, ct)` — 1 câu
  `UPDATE ... FROM ... INNER JOIN (SELECT TOP 1 ... ORDER BY DownloadedAt DESC)` cập nhật đúng bản
  ghi log download mới nhất (tránh update nhầm nhiều dòng/race).
- `POS.Application/Features/DataSync/{IMasterDataSyncService,MasterDataSyncService}.cs`: thêm
  `LogDeleteAsync(fileName, deleteStatus, ct)` — fail-safe (try/catch nuốt lỗi, giống
  `LogDownloadAsync`); tách helper `ParseSiteAndTerminal(fileName)` dùng chung cho cả 2 method
  (best-effort parse siteCode/posTerminal từ tên file).
- `POS.Api/Controllers/SyncDataPosController.cs`: `DeleteFileFromFTP` đổi `IActionResult` →
  `async Task<IActionResult>`, gọi `LogDeleteAsync` với `CancellationToken.None` — `"Success"` khi
  xóa file thành công, `"Failed"` khi file không tồn tại HOẶC exception (quyết định có chủ đích:
  cả 2 đều là 1 lượt xóa không thành công POS cần biết). Nhánh path-traversal-blocked không log.
- `POS.Common/Dtos/Ops/PosTerminalListDto.cs`: thêm `DateTime? LastMasterDataDownloadedAt`.
- `POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`
  (`GetPosTerminalListAsync`): thêm `OUTER APPLY` thứ 2 vào `dbo.MasterDataDownloadLog` (TOP 1
  ORDER BY DownloadedAt DESC theo SiteCode+PosTerminal), cùng pattern với `OUTER APPLY POSMonitor`
  đã có, dùng index sẵn có `IX_MasterDataDownloadLog_Site_At`.
- `PosMapPage.razor` (`/catalog/pos-setup`): thêm cột "MasterData" (cuối bảng, có sort) hiển thị
  thời gian tương đối qua helper `FormatRelativeTime` ("vừa xong"/"Xp trước"/"Xh trước"/"X ngày
  trước", luôn tương đối kể cả > 1 ngày — khác cách `PosTerminalDetailDialog.DateTimePos` chuyển
  sang tuyệt đối); màu chữ cảnh báo qua `MasterDataColor` (chưa từng tải hoặc > 7 ngày = đỏ, > 1
  ngày = vàng, còn lại = mặc định).
- `MainLayout.razor`: chuyển `MudNavGroup "Thiết bị POS"` (POSTerminal + POS bank) từ section
  DANH MỤC (`BackOfficeAndAbove`) sang VẬN HÀNH (`OpsAndAbove`, đặt đầu section) — đổi tên field
  `_expandCatPos` → `_expandOpsPos`, cập nhật `UpdateExpanded()` và `BreadcrumbMap` cho
  `/catalog/pos-setup` + `/catalog/bank-pos` (Section đổi `"DANH MỤC"` → `"VẬN HÀNH"`).
- `PosMapPage.razor` + `Catalog/PosDevices/BankPosPage.razor`: đổi
  `@attribute [Authorize(Policy = ...)]` từ `BackOfficeAndAbove` → `OpsAndAbove` — bắt buộc phải
  đổi cùng lúc với menu, nếu không BackOffice vẫn truy cập được bằng URL trực tiếp dù menu đã ẩn.
  **Đây là ngoại lệ** so với đợt tách `BackOfficeAndAbove` cho toàn bộ `/catalog/*` (entry
  2026-07-09 "Thêm role thứ 4 BackOffice" phía trên) — 2 trang thiết bị POS này thuộc phạm vi IT
  Ops, không phải BackOffice.

**Pattern mới:** Không có pattern mới — cả 3 việc đều tái dùng pattern đã có sẵn (fail-safe
insert/update log như `InsertDownloadLogAsync`/`LogDownloadAsync`; `OUTER APPLY` lấy bản ghi mới
nhất như `OUTER APPLY POSMonitor`; đổi policy trang theo `WebPolicies` có sẵn).

**Lưu ý cho session sau:** DBA phải chạy lại `docs/sql/MasterDataDownloadLog.sql` (ALTER TABLE) và
`docs/sql/SyncGetDataByTable_AddFilter.sql`/liên quan trước khi tính năng log xóa + cột MasterData
hoạt động đúng trên môi trường có dữ liệu thật (xem `docs/ROLLOUT.md` §O1). Nếu sau này còn task
"chuyển menu + đổi quyền trang", nhớ luôn đổi CẢ 2 nơi (sidebar `MainLayout.razor` VÀ
`@attribute [Authorize(Policy=...)]` của trang) — chỉ đổi sidebar không chặn được truy cập URL
trực tiếp. Build + `dotnet test tests/POS.ContractTests` 39/39 xanh sau mỗi bước; **CHƯA VERIFY
UI/DB thật** (sandbox thiếu `POS_SECRET_KEY`/DB/Redis).

---

## [2026-07-09] Coupon: nút Xóa (soft-block) + đồng bộ tab mã phát hành với Voucher; đồng nhất nhãn trạng thái "Hiệu lực"

**Layer:** POS.Web, POS.Application, POS.Infrastructure, POS.Common

**Loại:** Feature + Pattern mới (chuẩn nhãn trạng thái)

**Thay đổi:**
- `POS.Common/Dtos/SetupCoupon/SetupCouponDtos.cs`: `CouponSaveResult` tái dùng; `CouponCodeDto`
  thêm 3 field `Status`/`AmountUsed`/`OrderUsed` (mirror `VoucherCodeDto`) — field mới, không đổi
  tên field cũ, an toàn với contract.
- `POS.Infrastructure/Repositories/CouponVoucher/{ICouponRepository,CouponRepository}.cs`: thêm
  `UpdateBlockedAsync(itemNo, blocked, ct)` → SP mới `usp_SetupCoupon_UpdateBlocked` (mirror
  `usp_SetupVoucher_UpdateBlocked`), cập nhật RIÊNG `CpnVchBOMHeader.Blocked`.
- `POS.Application/Features/CouponVoucher/{ICouponService,CouponService}.cs`: thêm
  `UpdateBlockedAsync` wrapper (delegate thuần xuống repository).
- `docs/sql/SetupCoupon_UpdateBlocked.sql` (**MỚI**): SP `usp_SetupCoupon_UpdateBlocked`.
- `docs/sql/SetupCoupon_Read.sql`: sửa `usp_SetupCoupon_GetCodes` thêm SELECT 3 cột
  `Status`/`AmountUsed`/`OrderUsed` (đã có sẵn trên `CpnVchBOMCodeIssue`, không cần migration DB).
- `CouponsPage.razor` (`/promotion/coupons`): thêm nút icon "Xóa" trong cột THAO TÁC (bên cạnh
  "Xem chi tiết") → `MudMessageBox` confirm → `UpdateBlockedAsync(itemNo, true)` (soft-block, KHÔNG
  hard-delete) + audit log `DELETE`/`SetupCoupon`; cột THAO TÁC đổi `width:80px` → `width:1%;
  white-space:nowrap` để không xuống dòng khi có 2 icon; dropdown lọc + badge "Hiệu lực" đổi từ
  "Còn hiệu lực" cho khớp chuẩn nhãn mới.
- `CouponIssuePage.razor` (`/promotion/coupons/issue`): tab "Mã coupon đã phát hành" thêm 4 cột
  Trạng thái khóa/Status/Số tiền đã dùng/Đơn hàng đã dùng (mirror `VoucherIssuePage.razor`, thêm
  helper `StatusDisplay(string)` cục bộ); checkbox "Khóa (Blocked)" bỏ `Disabled="@IsViewMode"` —
  luôn sửa được ở view mode (ngoại lệ duy nhất, khớp Voucher); thêm `_originalBlocked`/
  `BlockedChanged`/`SaveBlockedAsync()` — nút Lưu riêng chỉ hiện khi Blocked đổi, chỉ cập nhật
  riêng field này (không đụng field khác).
- `VouchersPage.razor` (`/promotion/vouchers`): đổi filter mặc định `Status="-1"` (Tất cả) →
  `"1"` (Hiệu lực) — cả lúc mở trang lẫn khi bấm "Xóa" bộ lọc.
- **Đồng nhất nhãn trạng thái "Hiệu lực"/"Hết hiệu lực"** across `CouponsPage.razor` ("Còn hiệu
  lực"→"Hiệu lực"), `OffersPage.razor` ("Có hiệu lực"→"Hiệu lực" ở dropdown + badge qua helper mới
  `EffectDisplay(bool)` thay vì in thẳng chuỗi SP legacy + Excel export), `PricesPage.razor`
  (checkbox lọc "Còn hiệu lực"→"Hiệu lực", badge active giữ nguyên đã đúng). `VouchersPage.razor`
  đã đúng chuẩn sẵn, dùng làm tham chiếu. Không sửa SP legacy `GetPromotionOfferHeaderList` (rủi ro
  dùng chung) — chuẩn hóa client-side.
- `.claude/rules/mudblazor-flat-ui.md` §4a: thêm quy tắc "Nhãn trạng thái còn/hết hiệu lực theo
  ngày PHẢI dùng đúng 'Hiệu lực'/'Hết hiệu lực'" — chuẩn bắt buộc cho page mới sau này.
- `docs/CURRENT_STRUCTURE.md`, `docs/ROLLOUT.md` (mục D3b), `docs/WEB_STATUS.md`: cập nhật theo
  các thay đổi trên.
- `tests/POS.ContractTests/JsonFieldContractTests.cs`: cập nhật `CouponCodeDto_locked` khớp 3
  field mới (đổi có chủ đích, khóa lại field mới).

**Pattern mới:** Chuẩn nhãn trạng thái hiệu lực toàn dự án — "Hiệu lực"/"Hết hiệu lực" (đã cập nhật
`.claude/rules/mudblazor-flat-ui.md` §4a).

**Lưu ý cho session sau:** DBA phải chạy 2 script SQL trên CentralMD trước khi tính năng Xóa/4 cột
mới hoạt động thật: `docs/sql/SetupCoupon_UpdateBlocked.sql` (SP mới) và `docs/sql/SetupCoupon_Read.sql`
(bản đã sửa). Chưa verify UI bằng mắt (sandbox thiếu DB/Redis) — chỉ verify qua build + 39/39
contract test xanh.

---

## [2026-07-09] Thêm role BackOffice (giữa StoreOperator và ITOps) — quản lý Danh mục + Khuyến mãi

**Layer:** POS.Web
**Loại:** Feature (phân quyền)

**Thay đổi:**
- `src/POS.Web/Auth/WebRoles.cs`: thêm `WebRoles.BackOffice` + `WebPolicies.BackOfficeAndAbove`.
- `src/POS.Web/Program.cs`: `StoreAndAbove` thêm `BackOffice`; thêm policy `BackOfficeAndAbove`
  (`BackOffice + ITOps + SystemAdmin`); `OpsAndAbove` giữ nguyên (chỉ `ITOps + SystemAdmin`).
- 31 file `.razor` dưới `Components/Pages/Catalog/**` + `Components/Pages/Promotion/**` (gồm
  `Ops/StorePage.razor` + `Ops/PosMapPage.razor` — route thuộc catalog dù namespace `Ops`): đổi
  `@attribute [Authorize(Policy = WebPolicies.OpsAndAbove)]` → `WebPolicies.BackOfficeAndAbove`.
  8 page `/ops/*` thật (Health/Alerts/Queues/Logs/DataRawLog/PosDataSetup/Redis/SqlConsoleAudit)
  **giữ nguyên** `OpsAndAbove`.
- `Components/Layout/MainLayout.razor`: 2 khối `AuthorizeView` bọc menu DANH MỤC + KHUYẾN MÃI đổi
  sang `BackOfficeAndAbove`; khối VẬN HÀNH (Giám sát/Nhật ký/Cấu hình) giữ nguyên `OpsAndAbove`.
- `Components/Pages/Admin/UsersPage.razor`: thêm option `BackOffice` vào dropdown filter role +
  case trong `RoleDisplay`.
- `Components/Pages/Admin/Dialogs/UserFormDialog.razor`: thêm `MudSelectItem` role `BackOffice`
  vào dropdown tạo/sửa user (đây mới là nơi thật sự gán role — `UsersPage.razor` chỉ có filter).
  BackOffice không có `StoreCodes` scoping — xem tất cả store giống ITOps/SystemAdmin.
- `docs/web/security/roles.md`: cập nhật bảng role (§1, 4 role) + bảng policy (§1, 4 policy),
  tách bảng route §5.2 (`BackOfficeAndAbove` — Danh mục/Khuyến mãi) khỏi §5.3 (`OpsAndAbove` —
  chỉ `/ops/*` thật), cập nhật §6.1/§6.2/§6.3, viết lại §6.4 thành ví dụ đã triển khai.
- `.claude/skills/web/SKILLS.md`: cập nhật bảng "Roles và Policy mapping" (4 role/4 policy).
- `docs/WEB_STATUS.md`: cập nhật B1 (WebRoles+WebPolicies) và D7 (Program – 4 policy).

**Pattern mới:** Không phải pattern UI mới — đây là lần đầu thực thi checklist "thêm role mới"
đã có sẵn trong `docs/web/security/roles.md` §6.4 (nay viết lại thành ví dụ tham chiếu thay vì
"chưa có sẵn"). Không cần thêm gì vào `.claude/skills/web/SKILLS.md` ngoài bảng role/policy.

**Lưu ý cho session sau:** khi cần tách quyền cho 1 nhóm route đang dùng chung policy với nhóm
khác (như catalog/promotion từng dùng chung `OpsAndAbove` với `/ops/*`), luôn kiểm tra CẢ 3 nơi
áp policy: `@attribute [Authorize(...)]` trên từng `.razor`, `AuthorizeView Policy=` trong
`MainLayout.razor`, và `AddPolicy`/`RequireRole` trong `Program.cs` — thiếu 1 trong 3 sẽ tạo lỗ
hổng (route chặn được nhưng menu vẫn hiện, hoặc ngược lại). Chưa verify bằng mắt hành vi đăng
nhập/redirect thực tế với role BackOffice (sandbox thiếu `POS_SECRET_KEY`/DB/Redis) — chỉ verify
qua build + `dotnet test tests/POS.ContractTests` (39/39 xanh).

---

## [2026-07-09] Fix bug mở nhầm dòng chi tiết Hội viên + thêm OrderTime/Transaction

**Layer:** POS.Web, POS.Common, POS.Infrastructure
**Loại:** Bug fix + Feature nhỏ

**Thay đổi:**
- `InvoiceLoyaltyDto.cs`: thêm `DateTime? OrderTime` (nullable — dữ liệu cũ chưa có cột) và
  `string Transaction`.
- `RptLoyaltyRepository.cs`: `SELECT` của `GetInvoiceLoyaltyListAsync` thêm `OrderTime` và
  `[Transaction]` (bracket-quote bắt buộc — trùng reserved keyword `TRANSACTION`).
- `MemberPointsPage.razor`: thêm cột "Thời gian Order" vào `MudTable` (sau "Số hóa đơn") + Excel
  export. **Fix bug**: nút "Xem chi tiết" trước đó gọi `OpenDetailDialog(context.OrderNo)` rồi
  `_currentPageItems.FirstOrDefault(x => x.OrderNo == invoiceNo)` — 1 `OrderNo` có thể sinh nhiều
  dòng khác `ActionType` (PK composite `LoggingLoyalty`, vd `EARN` + `REDEEM` cùng hóa đơn) nên
  luôn mở nhầm dòng đầu tiên trùng `OrderNo`. Sửa: truyền thẳng `context` (object dòng đang
  render) vào `OpenDetailDialog(InvoiceLoyaltyDto invoice)`, bỏ hẳn tra cứu lại + field
  `_currentPageItems` (hết tác dụng).
- `MemberPointsDetailDialog.razor`: thêm field "Mã giao dịch" (`Invoice.Transaction`); đổi label
  `OrderTime` từ "Thời gian Order" → "Giờ giao dịch" (theo yêu cầu, khác label cột trên bảng — cố
  ý giữ khác nhau).
- `docs/architecture/loyalty-schema.md`: cập nhật ghi chú cột `OrderTime`/`Transaction` trỏ đúng
  nơi hiển thị + label hiện tại.
- `docs/CURRENT_STRUCTURE.md`: cập nhật field list `InvoiceLoyaltyDto`.

**Pattern mới:** "Truyền thẳng row object vào dialog chi tiết — KHÔNG tra cứu lại theo key đơn"
→ đã thêm vào `.claude/skills/web/SKILLS.md` (trước pattern "Modal chi tiết nhiều tab").

**Lưu ý cho session sau:** mọi dialog "Xem chi tiết" mở từ `RowTemplate` của `MudTable` PHẢI
truyền thẳng `context` (object dòng) — KHÔNG tra cứu lại theo 1 cột đơn (id/code) trừ khi cột đó
chắc chắn là khóa unique của tập dữ liệu đang hiển thị. Chưa verify bằng mắt trên DB Loyalty thật
(sandbox không có DB) — cần test thủ công 1 `OrderNo` có cả EARN và REDEEM trước khi coi là xong.

---

## [2026-07-09] Đổi tên `database-schema.md` → `centralMD-schema.md`

**Layer:** Tài liệu (không đụng code)
**Loại:** Refactor tài liệu (rename)

**Thay đổi:**
- `git mv docs/architecture/database-schema.md docs/architecture/centralMD-schema.md` — tên file
  rõ nghĩa hơn (khớp tên DB `CentralMD`/`RPOSMasterData`), phân biệt với `centralsale-schema.md`
  và `loyalty-schema.md` cùng thư mục.
- Cập nhật toàn bộ tham chiếu đường dẫn cũ sang tên mới (grep xác nhận không còn `database-schema`
  nào sót lại): `CLAUDE.md`, `README.md`, `docs/CHANGELOG.md`, `docs/WEB_STATUS.md`,
  `docs/architecture/{centralsale,loyalty}-schema.md`, `.claude/skills/{database,api}/SKILLS.md`,
  `docs/web/logic/{promotion_technical_spec,LOGIC_APPROVE_CTKM}.md`,
  `docs/migrations/{MIGRATION_MAP,FEATURE_SetupPrice_ANALYSIS}.md`,
  `docs/sql/{SalesPrice_EditDelete,OfferHeader_Deactivate,SetupCoupon_IssueMore,
  SetupVoucher_IssueMore}.sql`, `src/POS.Common/Dtos/Promotion/OfferHeaderDto.cs` (doc-comment).

**Pattern mới:** không có.

**Lưu ý cho session sau:** file schema DB cho `RPOSMasterData`/CentralMD nay tên
`docs/architecture/centralMD-schema.md` (không còn `database-schema.md`) — router index trong
`CLAUDE.md` đã trỏ đúng tên mới.

---

## [2026-07-09] Tách CLAUDE.md thành Router nhẹ + rule files theo module

**Layer:** Tài liệu (không đụng code)
**Loại:** Refactor tài liệu (Progressive Disclosure)

**Thay đổi:**
- `CLAUDE.md`: **1206 → 83 dòng**. Chỉ còn giữ: Quy tắc giao tiếp/báo cáo, tổng quan dự án,
  Router Index (mục lục điều phối), Cổng chặn trùng lặp. Toàn bộ chi tiết implementation đã
  chuyển sang file con.
- Tạo mới `.claude/rules/legacy-migration.md` (42 dòng) ← mục "Quy tắc Migration từ src/legacy/".
- Tạo mới `.claude/rules/architecture-layers.md` (87 dòng) ← "Cấu trúc Solution", "Quy tắc
  AppService", "Quy ước phát triển mới (Greenfield)".
- Tạo mới `.claude/rules/backend-api-rules.md` (126 dòng) ← quy tắc `POS.Common`
  (Newtonsoft.Json, cấm đổi field JSON), "Quy tắc Controller" A-F, "Guardrails & Testing".
- Tạo mới `.claude/rules/masterdata-sync.md` (104 dòng) ← toàn bộ feature spec "Sinh file
  master data .zip cho POS".
- Tạo mới `.claude/rules/blazor-web-app.md` (437 dòng) ← mục "POS.Web — Blazor Server
  Dashboard" (Stack, Auth, Roles, Services inject, Template page, MudBlazor v9 breaking
  changes, Responsive UI Standard, KHÔNG làm, Slash commands, MudAutocomplete circuit-crash,
  Density Standard, Audit log). **Không** copy §14 "MudBlazor Theme Standard" cũ — đã xác nhận
  trùng lặp gần 100% với `.claude/rules/mudblazor-flat-ui.md` (bản đó chi tiết + cập nhật hơn),
  xóa hẳn khỏi CLAUDE.md thay vì copy 2 nơi.
- `docs/architecture/centralMD-schema.md`: thêm block "Business rules" dưới `### Store` (ý
  nghĩa cột `ClosingMethod`, lưu ý KHÔNG dùng `Blocked`, query chuẩn lấy store hoạt động) — dời
  từ CLAUDE.md sang đây thay vì tạo file rule DB riêng, giữ đúng nguyên tắc "1 nguồn sự thật
  cho schema DB" đã khai trong chính CLAUDE.md.

**Pattern mới:** Không có pattern code mới — đây là tái tổ chức tài liệu thuần túy, không đổi
logic/behavior nào.

**Lưu ý cho session sau:** CLAUDE.md giờ chỉ là **Router** — khi cần chi tiết implementation
(AppService pattern, Controller rules, POS.Web auth/responsive/density, master-data-sync...)
phải mở đúng file `.claude/rules/*.md` tương ứng theo Router Index, KHÔNG còn tìm thấy trong
CLAUDE.md nữa. Toàn bộ nội dung đã đối chiếu grep để đảm bảo không mất logic khi tách.

---

## [2026-07-09] Đồng bộ UI toàn bộ POS.Web theo chuẩn MemberPointsPage.razor

**Layer:** POS.Web
**Loại:** Pattern mới (chốt chuẩn) + Refactor UI diện rộng

**Thay đổi:**
- `.claude/rules/mudblazor-flat-ui.md` §4a: `pos-status-chip` đổi từ "ngoại lệ theo 1 mockup cụ
  thể" sang **CHUẨN MẶC ĐỊNH** cho mọi status badge tĩnh — `MudChip` chỉ còn dùng khi cần tương
  tác (multi-select/closable/trong filter). Thêm §3a mới ghi lại 2 class `pos-btn-mockup`/
  `pos-btn-secondary-mockup` (trước đó chỉ có ở 1 file, chưa vào rule).
- `.claude/skills/web/SKILLS.md`: đồng bộ 2 bảng component tham chiếu (Shared components,
  bảng mapping loại UI) theo chuẩn `pos-status-chip` mới; sửa tiêu đề pattern §"Badge trạng thái
  dot-pill" từ "khi MudChip không khớp mockup" → "(CHUẨN MẶC ĐỊNH)".
- Rà và sửa markup **66 page + 29 dialog** trong `src/POS.Web/Components/Pages/**` (Store, Ops,
  Promotion, Catalog, Admin, root: Login/Index/AccessDenied): status `MudChip` tĩnh → `<span
  class="pos-status-chip pos-status-{semantic}">`; thêm `pos-btn-mockup` (mọi `MudButton`) +
  `pos-btn-secondary-mockup` (chỉ Outlined trung tính, không đặt `Color`); chuẩn hóa
  `Variant.Outlined`/`Margin.Dense`/`Adornment.Start` cho input filter; bổ sung
  `Dense/Hover/Striped/HorizontalScrollbar` còn thiếu trên `MudTable`; chuẩn hóa
  `MudTablePager PageSizeOptions` về `{10,20,50,100}`; chuẩn hóa format ngày giờ
  `yyyy-MM-dd HH:mm:ss` cho cột lịch sử nhiều ngày (loại trừ có chủ đích: dashboard "hôm nay"
  giữ time-only, `PricesPage`/`PriceSetupPage` giữ format string SP trả sẵn — không đổi backend).
- Chỉ sửa Razor markup — không đổi `@code`/DTO/service, ngoại trừ thêm vài helper tĩnh
  `(string CssClass, string Label)` cạnh helper cũ (đã xóa 2 helper cũ hết dùng ở
  `VoidsPage.razor`/`TransactionsPage.razor` sau khi chuyển sang helper mới).

**Pattern mới:** `pos-status-chip` là chuẩn mặc định status badge (đã cập nhật cả
`mudblazor-flat-ui.md` và `.claude/skills/web/SKILLS.md`).

**Lưu ý cho session sau:** Vài batch chạy qua sub-agent bị gián đoạn giữa chừng do lỗi API tạm
thời ("hệ thống bảo mật VCM") — luôn build lại + đọc kỹ file đã sửa trước khi tin tưởng báo cáo
"đã hoàn thành" của agent, đặc biệt kiểm tra `@{` lồng sai trong code-block đã mở (RZ1010) và
helper method cũ còn sót lại không ai gọi. Verify: `dotnet build POS.slnx` 0 lỗi + `dotnet test
tests/POS.ContractTests` 39/39 xanh. **CHƯA VERIFY UI bằng mắt** — sandbox thiếu
`POS_SECRET_KEY`/DB/Redis nên không `dotnet run` được.

---

## [2026-07-09] Trang "Hội viên" (CỬA HÀNG → Giao dịch) + chuẩn hóa badge/button theo mockup

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Feature mới + Pattern mới + Bug fix (input font-size)

**Thay đổi:**
- `src/POS.Common/Dtos/RptLoyalty/InvoiceLoyaltyDto.cs` (mới): DTO báo cáo hóa đơn tích/tiêu điểm
  hội viên (`OrderNo`, `TransactionType`, `OrigOrderNo`, `MemberCardNo`, `StoreNo`, `ActionType`,
  `LoyaltyPoints`, `CrtDate`, `Total`) + computed `MemberCardMasked` (che 4 số cuối).
- `src/POS.Infrastructure/Repositories/Loyalty/{I}RptLoyaltyRepository.cs` (mới): tách khỏi
  `ILoyaltyRepository`, query trực tiếp `LoggingLoyalty` (RPOSLoyalty) — server-side paging
  (`OFFSET/FETCH` + `COUNT(*) OVER()`), tính lại dấu `LoyaltyPoints` theo `ActionType` ngay tại SQL.
  Đăng ký DI trong `POS.Infrastructure/DependencyInjection.cs`.
- `src/POS.Web/Components/Pages/Store/Transactions/MemberPointsPage.razor` (mới) +
  `src/POS.Web/Components/Pages/Store/Dialogs/MemberPointsDetailDialog.razor` (mới): trang
  "Hội viên" — filter (Cửa hàng/Từ-Đến ngày/Hình thức/Số hóa đơn/Số thẻ HV/Loại hóa đơn) +
  `MudTable ServerData` + Export Excel (ClosedXML) + dialog xem chi tiết (hero số điểm + chip).
- `src/POS.Web/Components/Layout/MainLayout.razor`: thêm nav "Hội viên" vào nhóm "Giao dịch" +
  breadcrumb + `_expandStoreTx`.
- `src/POS.Web/wwwroot/app.css`: 3 nhóm rule mới —
  1. `.pos-status-chip` + modifier `.pos-status-{success,error,warning,info}` (badge dot-pill theo
     mockup `docs/web/images/status.jpg`, thay `MudChip` cho cột trạng thái/hình thức).
  2. `.mud-input-root{font-size:0.78125rem !important}` — **fix bug thật**: input value (TextField/
     Autocomplete/Select/DatePicker) không hề bị `Typography.Body1` chi phối như tài liệu cũ ghi
     (verify trực tiếp `MudBlazor.min.css`: `.mud-input-root{font:inherit}` chỉ kế thừa từ `<body>`
     = `Typography.Default`, không phải Body1) — input trước đó to hơn mockup ~0.5px + weight
     không đảm bảo.
  3. `.pos-btn-mockup`/`.pos-btn-secondary-mockup` — nút Tìm/Xóa/Export Excel dùng `Size.Small` nhỏ
     hơn mockup `.btn{padding:7px 14px}`, và `Variant.Outlined` mặc định nền trong suốt khác
     `.btn-secondary` mockup (nền xám đặc) — tái dùng token `--pos-bg-alt`/`--pos-text-body`/`--pos-border`.
- `docs/CURRENT_STRUCTURE.md`, `docs/architecture/loyalty-schema.md`, `docs/WEB_STATUS.md`,
  `.claude/rules/mudblazor-flat-ui.md` §4a, `.claude/skills/web/SKILLS.md`: đồng bộ tài liệu.

**Pattern mới:** Badge dot-pill (`.pos-status-chip`) khi `MudChip` không khớp mockup + input
font-size không do `Body1` chi phối như lầm tưởng trước đây → đã cập nhật
`.claude/skills/web/SKILLS.md` (pattern cuối file) + `.claude/rules/mudblazor-flat-ui.md` §4a.

**Lưu ý cho session sau:**
- Cột `StoreNo` trên bảng `LoggingLoyalty` (RPOSLoyalty) **CHƯA TỒN TẠI** trong DB thật (script gốc
  `docs/sql/database/Loyalty.sql` không có cột này) — `RptLoyaltyRepository` đã viết sẵn
  `WHERE StoreNo=@StoreNo` giả định cột tồn tại (user xác nhận sẽ tự thêm cột sau, ngoài phạm vi
  task). Trước khi cột được thêm, mọi query có `storeNo != null` sẽ lỗi SQL "Invalid column name".
- Nếu cần sửa input font-size/weight cho MudTextField/MudSelect/MudAutocomplete/MudDatePicker, sửa
  ở `.mud-input-root` trong `app.css`, **KHÔNG** sửa `Typography.Body1` trong `PosTheme.cs` (không
  có tác dụng lên input, chỉ ảnh hưởng `<MudText Typo="Typo.body1">`).
- **CHƯA VERIFY UI bằng mắt trên trình duyệt** (sandbox thiếu `POS_SECRET_KEY`/DB Loyalty/Redis) —
  chỉ verify được qua `dotnet build` (0 lỗi) + `dotnet test tests/POS.ContractTests` (39/39).

---

## [2026-07-09] Action linh động theo bảng khi sinh file .txt master data sync

**Layer:** POS.Api, POS.Web (indirect qua service), POS.Application, POS.Infrastructure, POS.Common
**Loại:** Feature (bỏ hardcode) + thay đổi hành vi SP

**Thay đổi:**
- `docs/sql/SyncTableList_AddAction.sql` (mới): thêm cột `SyncTableList.Action` (default
  `TRUNC-INSERT`) + cập nhật SP `[SyncTable_Get]` trả thêm cột `Action`; thêm nhánh **mới**
  `@IsChange='W'` (Web Sync/push 1 POS) — Action nhánh này luôn literal `'DELETE-INSERT'`.
- `src/POS.Common/Dtos/DataSync/SyncTableInfo.cs`: thêm property `Action` (map cột SP).
- `src/POS.Common/Dtos/DataSync/GetMasterDataFileRequest.cs`: đổi field `SyncAction` (string?
  override Action literal) → `IsChangeMode` (string, "A"/"W" — chọn nhánh gọi SP).
- `src/POS.Infrastructure/Repositories/DataSync/{I}SyncRepository.cs`: `GetSyncTablesAsync` nhận
  thêm tham số `isChange` (mặc định "A"); Redis cache tách key `MD:SyncTableList:A` /
  `MD:SyncTableList:W` (Action khác nhau giữa 2 nhánh nên không thể dùng chung 1 cache key).
- `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`: `ActionFor` không còn hardcode
  — batch 1 dùng `SyncTableInfo.Action` (fallback hằng số `TRUNC-INSERT` nếu SP chưa có cột), batch
  sau luôn `INSERT` (ràng buộc kỹ thuật, không đổi theo DB); `IsChangeMode="W"` dùng Action cho MỌI
  batch (fallback `DELETE-INSERT`).
- `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`PushStartOfDayDataAsync`, dùng bởi
  nút "Đồng bộ" trên `PosMapPage.razor`): đổi `SyncAction="DELETE-INSERT"` ép cứng → `IsChangeMode="W"`.
- `docs/CURRENT_STRUCTURE.md`, `CLAUDE.md` (mục "Sinh file master data .zip cho POS"),
  `docs/ROLLOUT.md` (thêm mục O1b): cập nhật đồng bộ theo thay đổi trên.

**Pattern mới:** không phải pattern tái dùng chung (đặc thù tính năng Sync Master Data) — không
cập nhật SKILLS.md.

**Verify:** `dotnet build POS.slnx` (0 error) + `dotnet test tests/POS.ContractTests` (39/39 pass).
**Chưa verify runtime thật** (gọi SP `[SyncTable_Get]`/`[SyncGetDataByTable]` thật, mở file `.txt`
trong zip xem field `Action`) — sandbox không có kết nối DB CentralMD/Redis thật. Cần DBA áp dụng
`docs/sql/SyncTableList_AddAction.sql` trên môi trường có DB trước khi test tay.

**Lưu ý cho session sau:** `SyncTableList.Action` là cấu hình theo TỪNG BẢNG cho nhánh ALL sync
(`@IsChange='A'`) — DBA chỉnh trực tiếp trên DB (`UPDATE SyncTableList SET Action=...`), KHÔNG cấu
hình ở appsettings/code. Nhánh Web Sync (`@IsChange='W'`) luôn trả `DELETE-INSERT` bất kể cột
`Action` trong DB (business rule cố định trong SP, không cấu hình được qua DB). Nhớ `DEL
MD:SyncTableList:A` (và `:W` nếu liên quan) trên Redis sau khi DBA đổi cấu hình để có hiệu lực ngay.

## [2026-07-08] Giảm nhiễu log INF — MinimumLevel Warning + GetLevel tùy biến cho UseSerilogRequestLogging

**Layer:** POS.Api, POS.Web, POS.Worker
**Loại:** Pattern mới (giảm nhiễu log) + Bug fix (config)

**Thay đổi:**
- `src/POS.Api/Program.cs`: thêm `using Serilog.Events;`, `app.UseSerilogRequestLogging()` → truyền
  `GetLevel` tùy biến (exception/5xx → `Error`, 4xx trừ 404 → `Warning`, 2xx/3xx thành công + 404 →
  `Debug`, tự bị chặn bởi `MinimumLevel.Default=Warning`).
- `src/POS.Api/appsettings.json`, `appsettings.UAT.json`: `Serilog:MinimumLevel:Default`
  `Information` → `Warning` (`Microsoft.Hosting.Lifetime` giữ `Information`).
- `src/POS.Worker/appsettings.json`, `appsettings.UAT.json`: tương tự `Default` → `Warning`.
- `src/POS.Web/appsettings.Production.json`: `Default` từ `Error` → `Warning` — **bug thực tế phát
  hiện lúc khảo sát**: giá trị `Error` cũ chặt hơn yêu cầu, làm mất toàn bộ log Warning ở Production
  (không phải chỉ đổi Information→Warning như 2 project kia, đây là sửa 1 giá trị sai hướng ngược).
- `appsettings.Development.json`/`appsettings.Production.json` (Api, Worker) và
  `appsettings.Development.json`/`appsettings.UAT.json` (Web) — **không đổi**, vì không có section
  `Serilog` riêng, tự kế thừa `Default` mới từ file base.

**Pattern mới:** `UseSerilogRequestLogging` GetLevel tùy biến theo status code — đã ghi vào
`.claude/skills/api/logging.md` mục 4.1 (bao gồm lý do 404 bị loại trừ khỏi nhánh Warning, và vì
sao không sửa `SerilogConfiguration.cs` mà sửa tại `Program.cs`).

**Verify:** `dotnet build POS.slnx` (0 error) + `dotnet test tests/POS.ContractTests` (39/39 pass).
**Chưa verify runtime thật** (gọi `/health`, 404, lỗi 4xx/5xx thật để xem log console/file) — sandbox
không có `POS_SECRET_KEY`/DB/Redis nên không chạy `dotnet run` được.

**Lưu ý cho session sau:** `UseSerilogRequestLogging()` (Serilog.AspNetCore) và
`RequestResponseLoggingMiddleware` (custom, `RequestLogging:Enabled`) là **2 cơ chế độc lập** —
tắt middleware custom không tắt được log INF mặc định của `UseSerilogRequestLogging()`. Nếu sau
này thấy log 404/200 ngập trở lại, kiểm tra cả 2 nơi, không chỉ 1.

---

## [2026-07-08] Fix WebSocket SignalR bị rớt qua subdomain HTTPS + đồng bộ Security:Mode UAT

**Layer:** POS.Web
**Loại:** Bug fix (config) — root cause chính nằm ngoài repo (hạ tầng)

**Thay đổi:**
- `src/POS.Web/appsettings.UAT.json`: `Security:Mode` đổi từ `"Internet"` → `"BehindProxy"` —
  UAT thực tế chạy sau **2 tầng reverse proxy** (SSL vhost ngoài không có trong repo → nginx
  `pos-web.uat.conf` trong repo → Kestrel), nhưng `Mode="Internet"` khiến
  `ForwardedHeadersOptions`/`app.UseForwardedHeaders()` (`Program.cs:74-83,184-225`) **không bao
  giờ chạy**, sai với topology thật. Khớp với `appsettings.Production.json` (đã đúng `BehindProxy`
  từ trước).

**Phân tích root cause (đọc trực tiếp, không suy đoán):**
- Console browser báo `Failed to start the transport 'WebSockets'... check that sticky sessions
  are enabled` khi truy cập qua subdomain HTTPS. Đã loại trừ nguyên nhân "nhiều backend thiếu
  sticky session" — xác nhận với người dùng chỉ chạy **1 instance** POS.Web.
- Đọc toàn bộ `nginx/pos-web.conf` + `nginx/pos-web.uat.conf` (tầng 2, trong repo) — đã cấu hình
  đúng chuẩn Blazor Server WebSocket (`location /_blazor` riêng, `proxy_http_version 1.1`,
  `Upgrade`/`Connection` header, timeout 86400s) → **không phải nguyên nhân**.
- Root cause thực sự nằm ở **tầng 1 (SSL vhost ngoài terminate subdomain)** — file này KHÔNG có
  trong repo, không SSH được vào server để tự sửa/verify. Đã cung cấp template nginx đầy đủ +
  lệnh `curl -i -N`/`nginx -T` để người vận hành tự vá và tự verify — xem `docs/ROLLOUT.md` §O7.

**Pattern mới:** không có pattern code mới — đây là phát hiện kiến trúc hạ tầng (2 tầng reverse
proxy cho subdomain), đã ghi vào `docs/ROLLOUT.md` §O7 làm checklist go-live.

**Lưu ý cho session sau:** khi POS.Web báo lỗi WebSocket/SignalR sau khi đổi domain hoặc thêm tầng
proxy mới, luôn hỏi rõ **có bao nhiêu tầng reverse proxy và TLS terminate ở tầng nào** trước khi
kết luận — mỗi tầng proxy trên đường đi đều phải tự khai báo lại `Upgrade`/`Connection`/
`proxy_http_version 1.1`, không tự kế thừa từ tầng trước. Không tự suy diễn "thiếu sticky session"
chỉ từ nội dung thông báo lỗi của SignalR JS client — đó là thông báo chung cho mọi lỗi transport.

---

## [2026-07-08] Serilog Error-only reconfig cho POS.Web + Rich Exception Logging capability

**Layer:** POS.Infrastructure, POS.Web
**Loại:** Bug fix (dead config) + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: bỏ hardcode
  `.MinimumLevel.Information()` + 3 `.MinimumLevel.Override(...)` chạy SAU
  `ReadFrom.Configuration(configuration)` — trước đây các dòng hardcode này **luôn đè** giá trị đọc
  từ `appsettings.json` (`Serilog:MinimumLevel`), khiến section đó **vô tác dụng** trên cả 3 host
  (Api/Web/Worker), chỉ "hoạt động" nhờ trùng giá trị ngẫu nhiên. Giờ để `ReadFrom.Configuration` tự
  đọc — mỗi host tự cấu hình mức log qua appsettings riêng, không ảnh hưởng lẫn nhau.
- `src/POS.Web/appsettings.json`: `Serilog:MinimumLevel:Default` = `Information` → `Warning`.
- `src/POS.Web/appsettings.Production.json`: thêm section `Serilog:MinimumLevel` mới (`Default` +
  `Microsoft`/`System` = `Error`, giữ `Microsoft.Hosting.Lifetime` = `Information` vì khối lượng
  log thấp và hữu ích để biết app start/stop qua file log).
- `src/POS.Web/appsettings.UAT.json`: đồng bộ thêm section `Serilog:MinimumLevel` (copy từ DEV,
  `Default: Warning`) — trước đó UAT không có section này, ngầm dùng DEV base.
- `src/POS.Infrastructure/Logging/SensitiveDataMasker.cs` (mới, static): `IsSensitiveKey(key)` +
  `Mask(IReadOnlyDictionary<string, object?>)` — mở rộng từ khóa nhạy cảm trong
  `.claude/skills/web/audit-logging.md` §7 (`PASSWORD/SECRET/TOKEN/KEY/PWD/CREDENTIAL`), thêm
  `PIN`/`PINCODE`/`APIKEY`. **Helper mask dùng chung đầu tiên trong solution** — trước đây mỗi page
  tự inline `IsSensitiveKey` riêng.
- `IKibanaService.cs`/`KibanaService.cs`: thêm overload
  `LogException(string endpoint, string posNo, int errorCode, string note, Exception ex, IReadOnlyDictionary<string, object?>? context = null)`
  — dùng Serilog structured destructuring `{@Context}` (thay vì string interpolation), tự mask
  context qua `SensitiveDataMasker` **trước khi** vào `Task.Run` fire-and-forget, giữ nguyên
  `Exception` object đầy đủ (stack trace + inner exception) thay vì chỉ nhận `ex.Message` như
  overload cũ. **Overload cũ giữ nguyên 100%** — không breaking change cho `RevenueByStaffPage.razor`
  hay bất kỳ consumer POS.Api nào.
- `.claude/skills/api/logging.md`: thêm mục "5. Cách cấu hình số ngày lưu Log trên Server" (từ task
  Log Retention trước đó cùng ngày) + ghi chú `retainedFileCountLimit`/`fileSizeLimitBytes` đọc từ
  `LogRetentionOptions`.
- `.claude/skills/web/audit-logging.md` §7: trỏ về `SensitiveDataMasker` thay vì khuyến nghị tự
  inline `IsSensitiveKey` mỗi page.
- `docs/CURRENT_STRUCTURE.md`: thêm `SensitiveDataMasker.cs` vào cây `Logging/`, thêm chữ ký
  overload `LogException` mới + `SensitiveDataMasker` vào mục D/E.

**Pattern mới:** Structured exception logging với masking tự động (`{@Context}` +
`SensitiveDataMasker`) → đã ghi trong `docs/CURRENT_STRUCTURE.md`; chưa thêm vào
`.claude/skills/api/SKILLS.md` (cân nhắc thêm nếu pattern này được dùng lại ở nơi khác).

**Lưu ý cho session sau:**
- `SerilogConfiguration.cs` dùng chung cho Api/Web/Worker — muốn đổi mức log CHỈ 1 host, sửa
  `Serilog:MinimumLevel` trong appsettings của đúng host đó, KHÔNG sửa code chung (nay đã config-driven
  hoàn toàn, không còn hardcode nào đè).
- Còn ~150 call site `IFileLogHelper.WriteExpLogs` trong POS.Web (~52 file) ghi thẳng `.txt`,
  KHÔNG qua Serilog — không bị ảnh hưởng bởi `MinimumLevel`, không lên Elasticsearch. Đã xác nhận
  với user là **out of scope** cho task này — cần task migrate riêng nếu muốn đồng bộ.
- `RevenueByStaffPage.razor` là nơi DUY NHẤT dùng `IKibanaService.LogException` trong POS.Web,
  vẫn dùng overload cũ (`ex.Message`, mất stack trace) — có thể fast-follow sang overload mới.

---

## [2026-07-08] Log Retention Policy — POS.Api/POS.Web/POS.Worker

**Layer:** POS.Infrastructure, POS.Api, POS.Web, POS.Worker
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Logging/LogRetentionOptions.cs` (mới): bind section `LogRetention`
  (`SerilogRetainedFileCountLimit`, `SerilogFileSizeLimitBytes`, `RawLogRetentionDays`) — default
  giữ nguyên hành vi cũ nếu bỏ trống section (14 ngày Serilog/không giới hạn size, FileLogHelper
  không tự dọn).
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: `retainedFileCountLimit`/
  `fileSizeLimitBytes` của Serilog File sink đọc từ `LogRetentionOptions` thay vì hardcode `14`.
- `src/POS.Infrastructure/Logging/FileLogHelper.cs`: thêm `retentionDays` vào constructor + sweep
  cơ hội (tối đa 1 lần/24h, kích hoạt bởi lần `WriteLogs`/`WriteExpLogs` kế tiếp) dọn
  `debug/log-*.txt` + `Exception/log-*.txt` cũ hơn `retentionDays`. Không dùng `BackgroundService`
  riêng vì POS.Worker không đảm bảo luôn chạy (`--run-once` cron mode) và không nhìn thấy thư mục
  log vật lý của POS.Api/POS.Web. Đồng thời thêm 2 `lock` riêng (`_debugLock`/`_expLock`) cho
  `WriteLogs`/`WriteExpLogs` — fix rủi ro `IOException` bị nuốt lặng lẽ khi nhiều thread cùng ghi.
- `src/POS.Infrastructure/DependencyInjection.cs`: đăng ký `IOptions<LogRetentionOptions>`, truyền
  `RawLogRetentionDays` vào factory `IFileLogHelper`.
- appsettings (`appsettings.json`/`appsettings.Production.json`/`appsettings.UAT.json`) của cả 3
  project: thêm section `LogRetention` — Dev/UAT = 7 ngày, Production = 10 ngày (đã chốt với user).
- `.claude/skills/api/logging.md`, `docs/CURRENT_STRUCTURE.md`, `docs/ROLLOUT.md` (§O6): tài liệu
  hoá cơ chế + hướng dẫn đổi retention trên server đang chạy (sửa config + restart).

**Pattern mới:** Sweep cơ hội trong chính class ghi log (thay vì `BackgroundService` riêng) khi
class đó được host bởi nhiều process không đồng nhất về vòng đời — đã ghi trong
`docs/CURRENT_STRUCTURE.md` + `.claude/skills/api/logging.md` mục 5.

**Lưu ý cho session sau:** Retention chỉ xóa đúng pattern `debug/log-*.txt`/`Exception/log-*.txt`
— không xóa toàn bộ thư mục; giả định `Logging:FileLogDirectory` chỉ chứa log do 2 cơ chế này ghi.

---

## [2026-07-08] Thêm Redis Management Dashboard — /ops/redis (RedisDashboardPage)

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/Redis/` (mới): `RedisKeyInfoDto` (Key/Type/TtlSeconds), `RedisKeyValueDto`
  (+ Value pretty JSON), `RedisKeySearchResultDto` (Keys + IsTruncated), `RedisServerStatusDto`
  (IsOnline/PingMs/Role/Target/DatabaseIndex/UsedMemoryHuman/ConnectedClients/TotalKeys/
  HitRatePercent/UptimeSeconds/ErrorMessage).
- `src/POS.Infrastructure/Cache/{IRedisManager,RedisManager}.cs`: thêm
  `GetKeysByPatternAsync(pattern, maxResults)` (SCAN dừng sớm, trả `IsTruncated`),
  `GetKeyTtlSecondsAsync`, `GetKeyTypeAsync`, `GetKeyRawValueAsync` (đọc theo type: string/hash/
  list/set/zset), và mới nhất `PingAsync`/`GetServerInfoAsync`/`GetDbSizeAsync`/`DefaultDatabase`
  cho khu vực trạng thái/KPI — **lần đầu tiên codebase gọi Redis `PING`/`INFO` thật** (trước đó
  `HealthCheckService.CheckRedisAsync` chỉ round-trip `StringSet`/`StringGetAsync`). Pattern mới
  ghi ở `.claude/skills/cache/SKILLS.md` (Pattern 7).
- `src/POS.Application/Features/Redis/{IRedisManagementService,RedisManagementService}.cs` (mới):
  `SearchKeysAsync` (cap 1000 key/lần quét), `GetKeyValueAsync` (pretty-print JSON qua
  `JToken.Parse`), `DeleteKeyAsync`, `GetServerStatusAsync` (orchestrate Ping+Info+DbSize, tính
  `HitRatePercent`). Inject thẳng `IRedisManager` — không qua AppService 3 lớp vì đây không phải
  external HTTP client.
- `src/POS.Web/Components/Pages/Ops/RedisDashboardPage.razor` (mới, `/ops/redis`,
  `OpsAndAbove` = ITOps/SystemAdmin): status card (style copy từ `HealthPage.razor` —
  `CardStyle(bool)`/`LatencyDisplay(long,bool)`, border-left màu + chip ONLINE/OFFLINE + latency
  chip) + 5 KPI card chuẩn `.pos-kpi-value`/`.pos-kpi-label` (Bộ nhớ/Clients/Tổng Key/Cache Hit %/
  Uptime, không auto-refresh — chỉ nút "Làm mới" thủ công) + filter panel (pattern SCAN, bắt buộc
  confirm `MudMessageBox` nếu pattern đúng bằng `"*"`) + `MudTable` (Key/Type/TTL, phân trang
  client-side) + `Dialogs/RedisKeyValueDialog.razor` (xem giá trị, pretty JSON) + xóa key (confirm
  `MudMessageBox @ref` + `IAuditLogger.LogAsync` ghi oldValue trước khi xóa).
- `src/POS.Web/Components/Layout/MainLayout.razor`: thêm "Redis Cache" vào nhóm VẬN HÀNH/Giám sát
  + breadcrumb.
- **Bảo mật đã áp dụng theo yêu cầu**: không có method Flush/Clear-all ở bất kỳ đâu (chỉ xóa từng
  key); pattern `*` tuyệt đối bắt buộc xác nhận riêng; SCAN cap cứng 1000 key/lần + cảnh báo
  truncation (không refactor `IRedisManager` sang cursor-based SCAN — tránh ảnh hưởng các nơi
  khác đang gọi `GetKeysByPatternAsync(pattern)` 1 tham số).

**Pattern mới:** Server diagnostics PING/INFO/DBSIZE (khác cache-data pattern 1-6) →
`.claude/skills/cache/SKILLS.md` Pattern 7.

**Lưu ý cho session sau:** Layout KPI đã duyệt qua `AskUserQuestion` với người dùng trước khi code
(status card riêng + KPI row bên dưới, không auto-refresh) — nếu cần thêm dashboard "trạng thái +
KPI" cho service khác (RabbitMQ, Kafka...), tái dùng đúng khuôn `CardStyle`/`LatencyDisplay` này
thay vì tự nghĩ layout mới. Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests`
25/25. **Chưa verify UI thật** (sandbox không có Redis/DB thật/`POS_SECRET_KEY`) — cần tự
`dotnet run` kiểm tra trước khi coi là hoàn thành 100%.

---

## [2026-07-08] Fix: Retry DataRaw Log lỗi "network-related... SQL Server" (ops/data-raw-log)

**Layer:** POS.Infrastructure, POS.Web
**Loại:** Bug fix

**Nguyên nhân:** `InInsertToTableByJson` (CentralSaleRepository) dùng `StoreRoutedConnectionFactory`
routing theo `StoreSetServer` để mở connection riêng cho từng store — khi `ServerIP` của 1 store
không còn kết nối được (UAT/Prod), Retry văng "network-related or instance-specific error", trong
khi các hàm đọc log (`GetDataRawJsonSummaryAsync`/`GetDataRawJsonListAsync`) dùng connection cố
định `CentralSale` nên vẫn đọc bình thường.

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs`: `InInsertToTableByJson`
  đổi sang luôn dùng `directConnectionFactory` (CentralSale cố định) — bỏ hẳn
  `StoreRoutedConnectionFactory`/`StoreSetServer` cho method này (quyết định của user, tránh rủi
  ro ghi nhầm shard nếu tự động fallback). Áp dụng thống nhất cho mọi caller:
  `DataRawLogPage.razor` (Retry), `PosSalesConsumerWorker`, `PosFileImportService`, `KafkaAppService`.
- `src/POS.Web/Components/Pages/Ops/DataRawLogPage.razor`: `RetryAsync` thêm
  `CancellationTokenSource(100s)` truyền vào `InInsertToTableByJson` + catch riêng
  `OperationCanceledException` (UI không treo vô hạn khi DB phản hồi chậm).
- Dự án này không dùng EF Core/DbContext — không áp dụng `IDbContextFactory`; connection Dapper
  đã mở/đóng đúng chuẩn qua `using` trong từng method.

## [2026-07-08] Thêm "Quản lý Log Server" — /admin/logs (LogFilePage)

**Layer:** POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Services/LogFileInfo.cs` (mới): record DTO (RelativePath/FileName/FolderName/SizeBytes/LastModifiedUtc).
- `src/POS.Web/Services/ILogFileService.cs` + `LogFileService.cs` (mới): service riêng POS.Web
  (như `IWebUserService`) — liệt kê + tải file `.txt`/`.log` dưới thư mục cha của
  `Logging:FileLogDirectory` (vd Production `/srv/pos/logs/web` → root `/srv/pos/logs`, gồm cả
  `api/`, `web/`...), đệ quy toàn bộ subfolder. Whitelist extension + chống Path Traversal
  (`Path.GetFullPath` + so khớp prefix root có separator) cả lúc liệt kê lẫn lúc tải; mọi lỗi bọc
  try/catch ghi `IFileLogHelper.WriteExpLogs`, không throw ra UI.
- `src/POS.Web/Components/Pages/Admin/LogFilePage.razor` (mới): trang `/admin/logs`, `AdminOnly`,
  `MudTable` chuẩn v3 (Dense + HorizontalScrollbar + Elevation=2), tải file qua
  `JS.SaveAsFileAsync` (JS interop có sẵn, không qua controller HTTP).
- `src/POS.Web/Program.cs`: đăng ký `AddScoped<ILogFileService, LogFileService>()`.
- `src/POS.Web/Components/Layout/MainLayout.razor`: nav item nằm trong nhóm "VẬN HÀNH" → L2
  "Nhật ký" (cùng Interface Error/DataRawJson Log/Nhật ký thao tác) thay vì "QUẢN TRỊ" — theo yêu
  cầu người dùng di chuyển sau khi review. Vì L2 "Nhật ký" chỉ yêu cầu `OpsAndAbove` (ITOps thấy
  được) trong khi trang vẫn `AdminOnly` (chỉ SystemAdmin), leaf link bọc riêng
  `<AuthorizeView Policy="@WebPolicies.AdminOnly">` để ITOps không thấy link rồi bấm vào bị 403.
  `_expandOpsLog` thêm điều kiện `/admin/logs` để nhóm tự mở đúng route.
- `.claude/skills/web/SKILLS.md`: thêm pattern mới "Đọc/tải file trên server an toàn (whitelist
  extension + chống Path Traversal)" — codify lại 3 bước guard + anti-pattern (StartsWith có
  separator, không dùng `Contains`; check extension ở cả list lẫn download; không leak exception
  ra UI).
- `docs/WEB_STATUS.md`: thêm dòng L1 ghi nhận trang + vị trí menu.

**Pattern mới:** "Đọc/tải file trên server an toàn" → đã ghi vào `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:** `dotnet build src/POS.Web/POS.Web.csproj` 0 lỗi,
`dotnet test tests/POS.ContractTests` 25/25 xanh. Guard chống path traversal đã verify độc lập
qua script `dotnet run` standalone (6/6 case pass: file hợp lệ, traversal `../`, traversal
`web/../../`, absolute path ngoài root, file không tồn tại, extension `.dll` bị chặn) — **chưa
chạy app thật trên trình duyệt** để xem UI/luồng đăng nhập SystemAdmin (sandbox thiếu
`POS_SECRET_KEY`/DB/Redis). Không đi qua POS.Common/Infrastructure/Application/contract test —
đây là tiện ích nội bộ POS.Web đọc filesystem của chính máy chạy Web, không phải response cho
5.000 POS.

---

## [2026-07-08] Fix POS.Web không ghi log file/Elasticsearch — thiếu `AddSerilogWithElastic()`

**Layer:** POS.Web
**Loại:** Bug fix (production logging pipeline)

**Bối cảnh:** User báo log Production không xuất hiện tại `Logging:FileLogDirectory` đã cấu hình
(`/srv/pos/logs/api` cho Api, `/srv/pos/logs/web` cho Web, Ubuntu native/systemd, KHÔNG Docker).
Điều tra đối chiếu `appsettings.Production.json` vs `Program.cs` phát hiện root cause **thuần code**
cho POS.Web: `src/POS.Web/Program.cs` **thiếu hẳn** `builder.AddSerilogWithElastic()` (có ở
`src/POS.Api/Program.cs:40`) — không phải lỗi cấu hình hay quyền thư mục. `KibanaService` (dùng ở
~50 trang POS.Web) inject `ILogger<KibanaService>`, provider chỉ đổi sang Serilog (File + Elasticsearch
sink) khi có `builder.Host.UseSerilog(...)` — thiếu dòng này khiến `ILogger<T>` toàn bộ POS.Web rơi
về default provider ASP.NET Core (Console-only trên Linux), log biến mất không dấu vết.

**Thay đổi:**
- `src/POS.Web/Program.cs`: thêm `using POS.Infrastructure.Logging;` + gọi
  `builder.AddSerilogWithElastic();` ngay sau block giải mã `enc:...`, trước mọi
  `builder.Services.Add...()` khác — khớp đúng vị trí tương ứng trong `src/POS.Api/Program.cs`.
- `.claude/skills/web/deployment.md`: thêm pattern mới "Serilog PHẢI được wire tường minh trong
  từng `Program.cs`" — ghi lại root cause + checklist audit cho session sau.

**Pattern mới:** đã ghi vào `.claude/skills/web/deployment.md` (mục "Serilog PHẢI được wire tường
minh...").

**Lưu ý cho session sau:** `dotnet build src/POS.Web/POS.Web.csproj` 0 lỗi,
`dotnet test tests/POS.ContractTests` 25/25 xanh. **Chưa verify runtime thật** trên server Production
(không có quyền truy cập) — sau khi deploy cần xác nhận `/srv/pos/logs/web/pos-*.log` thực sự được
tạo. Phần POS.Api (đã có `AddSerilogWithElastic()` sẵn, code không lỗi) — nếu log API vẫn thiếu, nghi
vấn chuyển sang `ASPNETCORE_ENVIRONMENT` thật của systemd unit hoặc quyền ghi
`/srv/pos/logs/api` (chưa verify được, cần user tự kiểm tra trên server — xem hướng dẫn lệnh
`systemctl cat`/`namei -om` đã trao đổi trong session). Repo hiện **không có**
`Serilog.Debugging.SelfLog.Enable(...)` ở đâu — nếu cần chẩn đoán sâu hơn lỗi ghi file bị Serilog
nuốt, đây là việc cần làm thêm (chưa làm, vì có sửa code dù nhỏ, cần user xác nhận trước).

---

## [2026-07-08] DeleteFileFromFTP tự dọn companion .sha256

**Layer:** POS.Api
**Loại:** Bug fix (giải phóng dung lượng đĩa)

**Bối cảnh:** Mỗi zip masterdata publish bởi `MasterDataSyncService` có 1 file đồng hành
`{zipName}.sha256` để POS verify integrity. Endpoint `DeleteFileFromFTP` (POS gọi để dọn `.zip`
sau khi xử lý xong) trước đây chỉ xóa đúng file `.zip` theo `filePath`, để lại `.sha256` mồ côi
vĩnh viễn trên đĩa API server — không có cleanup job nào dọn riêng phần này ngoài daily-refresh
tự động (`CleanupSiblingZips`, chỉ chạy khi có publish mới).

**Thay đổi:**
- `src/POS.Api/Controllers/SyncDataPosController.cs` (`DeleteFileFromFTP`, dòng 367-406): sau khi
  xóa `.zip` thành công, thêm bước xóa `{localPath}.sha256` (best-effort, try/catch riêng, log lỗi
  qua `fileLogHelper.WriteLogs`, không ảnh hưởng `body.Status`/`Message` của response chính).
- `docs/api/Masterdata_Sync_Flow.md`: cập nhật mục 1 (tổng quan), sequence diagram, mục 5 (chi
  tiết API #3 — thêm phần "Companion .sha256 — tự động dọn kèm"), và bảng tổng hợp HTTP status
  (mục 7) để phản ánh hành vi mới.

**Pattern áp dụng (đã có sẵn, không phải pattern mới):** tái dùng đúng convention
`TryDeleteFile(zipPath + ".sha256")` đã tồn tại 2 lần trong `MasterDataSyncService.cs`
(`IsTodayZipValid`, `CleanupSiblingZips`) — không thêm entry vào SKILLS.md.

**Lưu ý cho session sau:** build (`dotnet build src/POS.Api/POS.Api.csproj`) và
`dotnet test tests/POS.ContractTests` đã xanh; **chưa verify runtime thật** (gọi endpoint với cặp
file `.zip`+`.sha256` thật) do môi trường sandbox thiếu `POS_SECRET_KEY`/DB/Redis để chạy
`POS.Api`.

---

## [2026-07-08] Fix DowloadFileStream bị Kestrel ngắt khi mạng POS chậm + tài liệu verify SHA-256

**Layer:** POS.Api
**Loại:** Bug fix + Pattern mới

**Bối cảnh:** Rà soát tiếp `GetFileFromFTP`/`DowloadFileStream` (sau task tách zip IsSingleFile) để
trả lời 3 câu hỏi: mức nén zip đã tối ưu chưa, cách POS verify SHA-256, còn lỗ hổng nào khiến POS
tải file mất dữ liệu/lỗi file. Kết luận: nén giữ nguyên `Fastest` (đã lossless, đổi mức chỉ đổi
CPU/tốc độ chứ không đổi an toàn dữ liệu — user chọn không đổi). Phát hiện 2 vấn đề thật trong
`DowloadFileStream`: (1) Kestrel `MinResponseDataRate` mặc định (240 byte/giây sau 5s) chưa được
override — server có thể tự ngắt kết nối khi mạng POS chậm dù đang gửi đúng dữ liệu; (2)
`Content-Type` hardcode `application/x-zip-compressed` cho mọi file kể cả khi POS tải file
`.sha256` (text) qua cùng endpoint.

**Thay đổi:**
- `src/POS.Api/Controllers/SyncDataPosController.cs` (`DowloadFileStream`): thêm
  `HttpContext.Features.Get<IHttpMinResponseDataRateFeature>().MinDataRate = null` trước khi stream
  (chỉ tắt cho request này, không đụng `Program.cs`/Kestrel global); `Content-Type` chọn theo phần
  mở rộng file (`.sha256` → `text/plain`, còn lại giữ `application/x-zip-compressed`).
- `CLAUDE.md` (mục "Sinh file master data .zip cho POS"): thêm hướng dẫn verify SHA-256 phía POS
  (suy tên `{FileName}.sha256`, tải qua `DowloadFileStream`, so sánh hash, mismatch → hủy & tải
  lại) + cảnh báo rõ đây là **integrity** (chống lỗi truyền), không phải **authenticity** (không
  chống giả mạo có chủ đích — cần TLS/HMAC nếu muốn vậy).
- `docs/ROLLOUT.md` (§O1): thêm caveat — cấu hình nginx timeout cho 2 route này chỉ là khuyến nghị,
  chưa xác nhận có reverse proxy thật nào đứng trước POS.Api (repo hiện map thẳng port 80, không có
  route `posblue` trong `nginx/*.conf`) — người vận hành cần tự đối chiếu topology Production thật.

**Pattern mới:** "Tắt Kestrel MinResponseDataRate cho 1 request stream file lớn" → đã thêm vào
`.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:**
- Hiệu quả thực tế của việc tắt `MinResponseDataRate` chưa verify được (cần môi trường throttle
  mạng để test) — chỉ verify được build + `dotnet test tests/POS.ContractTests` (25/25 xanh).
- HTTP Range/206 (resume download dở dang) tiếp tục bị hoãn theo quyết định của user (ưu tiên giải
  pháp tách zip nhỏ ở task trước) — nếu sau này cần resume thật, đây là việc chưa làm.

---

## [2026-07-08] Tách zip master data theo cờ SyncTableList.IsSingleFile (fix timeout download POS)

**Layer:** POS.Api, POS.Application, POS.Common
**Loại:** Bug fix + Pattern mới

**Bối cảnh:** Máy POS gọi `GetFileFromFTP?typeSync=ALL` rồi `DowloadFileStream` để tải zip master
data đầu ngày; với site nhiều dữ liệu, zip vượt >10MB khiến client POS (code cũ
`StorePos.InitDataPos`, `WebClient.DownloadFile`, timeout mặc định ~100s — codebase này KHÔNG nằm
trong repo, không sửa được) báo lỗi `The operation has timed out`. Giải pháp: giảm kích thước từng
file tải bằng cách tách các bảng nặng dữ liệu ra zip riêng, phần còn lại vẫn gom 1 zip như cũ.
Quyết định của user: điều khiển việc tách bằng 1 cột DB (`SyncTableList.IsSingleFile`), KHÔNG dùng
appsettings — vì đây là quyết định vận hành (DBA đổi theo dữ liệu thực tế từng site), không phải
quyết định lúc code/deploy.

**Thay đổi:**
- `docs/sql/SyncTableList_AddIsSingleFile.sql` (mới): thêm cột `IsSingleFile BIT DEFAULT 0` vào
  `dbo.SyncTableList` + cập nhật SP `[SyncTable_Get]` trả thêm cột này (áp dụng thủ công CentralMD).
- `src/POS.Common/Dtos/DataSync/SyncTableInfo.cs`: thêm field `IsSingleFile` (Dapper tự map).
- `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`: refactor
  `EnsureMasterDataFileAsync` — mỗi bảng ghi `.txt` vào sub-folder riêng (`_common` hoặc theo tên
  bảng) trong CÙNG 1 lần `Parallel.ForEachAsync` (không đổi hiệu năng sinh dữ liệu), sau đó zip
  riêng từng sub-folder có dữ liệu. Idempotent **all-or-nothing** trên toàn bộ danh sách zip dự
  kiến của lượt chạy; cleanup dùng chung 1 prefix + HashSet "vừa publish" nên tự dọn zip mồ côi khi
  1 bảng bị tắt cờ. Trả về `List<GetMasterDataFileResult>` thay vì 1 kết quả đơn (DTO nội bộ,
  không phải HTTP body → an toàn đổi signature).
- `src/POS.Application/Features/DataSync/IMasterDataSyncService.cs`: cập nhật signature interface.
- `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`PushStartOfDayDataAsync`): gộp
  `List<GetMasterDataFileResult>` thành 1 kết quả tổng hợp — giữ nguyên contract đơn cho
  `PosMapPage.razor` (POS.Web, phát hiện khi build lỗi do đổi signature `IMasterDataSyncService`).
- `src/POS.Api/Controllers/SyncDataPosController.cs` (`GetFileFromFTP`): cập nhật logging tổng hợp
  qua list — **KHÔNG đổi** response HTTP cho POS (`GetFileFromServerApiAsync` đã liệt kê mọi `*.zip`
  trong thư mục nên tự động hỗ trợ nhiều file, không cần sửa).

**Pattern mới:** "Tách N file output theo cờ DB (thay vì appsettings) — idempotent
all-or-nothing" → đã thêm vào `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:**
- Mặc định `IsSingleFile=0` cho mọi bảng → hành vi **không đổi** (1 zip common như cũ) cho đến khi
  DBA chủ động `UPDATE SyncTableList SET IsSingleFile=1 WHERE TableName IN (...)` + `DEL
  MD:SyncTableList` trên Redis (xem `docs/ROLLOUT.md` §O1). Chưa xác nhận được tên bảng chính xác
  cần tách (Barcodes/Item/SalesPrice...) — DBA tự quyết định theo dữ liệu thực tế, không hardcode.
- Chưa verify được hành vi runtime end-to-end (không có DB CentralMD + FtpRootPath thật trong
  phiên làm việc) — chỉ verify được build + `dotnet test tests/POS.ContractTests` (25/25 xanh).

---

## [2026-07-08] Fix VoidsPage rỗng dữ liệu (SQL reserved keyword) + đồng bộ UI + rule mới

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Bug fix + UI polish + Pattern mới (rule dự án)

**Bối cảnh:** Người dùng xác nhận `TransVoidHeader`/`TransVoidLine` có dữ liệu thật nhưng
`/store/voids` tìm kiếm luôn ra rỗng, không báo lỗi nào trên UI. Điều tra qua log file
(`D:\ROOT\Logs\POS.Web\Exception\log-20260708.txt`) phát hiện `SqlException` Msg 156 "Incorrect
syntax near the keyword 'LineNo'" trong `GetVoidReportAsync`, bị `catch`-log-return-`[]` nuốt âm
thầm (đúng pattern chung toàn `CentralSaleRepository.cs`) nên không lộ ra UI.

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs` (`GetVoidReportAsync`):
  `vl.LineNo,` → `vl.[LineNo],` — `LineNo` là reserved keyword MS SQL, cần bracket-quote (đã
  loại trừ nguyên nhân khác: không phải ký tự ẩn, không phải cột không tồn tại — Msg 156 khác
  Msg 207 "Invalid column name").
- `src/POS.Web/Components/Pages/Store/Transactions/VoidsPage.razor`: đồng bộ UI theo chuẩn
  `PosMapPage.razor` (file mẫu dự án) — filter panel `pa-3` (khớp mẫu), loading bar chỉ hiện khi
  chưa có dữ liệu (`_loading && !_voids.Any()`, tránh giật UI mỗi lần bấm Tìm), `MudTable` thêm
  `Striped="true"` bỏ `Elevation="2"` thừa, thay toàn bộ hex màu cứng (`#DC3545`/`#9e9e9e`) sang
  CSS token theme (`var(--pos-danger)`/`var(--pos-text-muted)`), 4 KPI card thêm viền trái theo
  màu ngữ nghĩa (danger/danger/warning/primary) khớp cách `PosMapPage.razor` phân loại KPI.
- `.claude/skills/database/SKILLS.md` + `CLAUDE.md` (mục "Quy tắc Stored Procedure"): thêm rule
  mới "Reserved keyword — BẮT BUỘC bracket-quote `[ ]`", áp dụng cho **mọi** SQL trong dự án (SP
  mới, script `docs/sql/*.sql`, Dapper inline query trong Repository) — không giới hạn riêng SP.

**Pattern mới:** Reserved keyword bracket-quote khi viết/sửa SQL → đã cập nhật
`.claude/skills/database/SKILLS.md` + tham chiếu ngắn trong `CLAUDE.md`.

**Lưu ý cho session sau:**
- `SqlException` Msg **156** "Incorrect syntax near the keyword 'X'" mà cột `X` **tồn tại thật**
  trong bảng (khác Msg **207** "Invalid column name") → chắc chắn là reserved keyword,
  bracket-quote `[X]` ngay, không cần đoán thêm. Dự án đã có tiền lệ `[Source]` trong
  `GetDataRawJsonListAsync` trước case `[LineNo]` này.
- Repository catch-log-return-default (pattern chung `CentralSaleRepository.cs`) khiến lỗi SQL
  KHÔNG BAO GIỜ lên UI — khi 1 trang "tìm không ra lỗi nhưng cũng không có data", luôn đọc file
  log Exception (`{FileLogDirectory}/Exception/log-{yyyyMMdd}.txt`) trước khi nghi ngờ logic
  filter/routing/kiến trúc sharding.
- Trong lúc điều tra, phát hiện tính năng `SyncTableList.IsSingleFile` (tách zip master data
  riêng theo bảng, theo đề xuất người dùng — xem `docs/sql/SyncTableList_AddIsSingleFile.sql`,
  `MasterDataSyncService.cs`) **đã được code + tài liệu hóa đầy đủ** (`docs/ROLLOUT.md` §O1,
  `CLAUDE.md` mục "Sinh file master data .zip") trong lúc phiên làm việc này diễn ra — không rõ
  bởi tiến trình nào, chỉ xác nhận lại bằng cách đọc trực tiếp code/build/test, không phải do
  session này viết. Build toàn solution (`POS.Api`) 0 lỗi, `dotnet test tests/POS.ContractTests`
  25/25 xanh sau tất cả thay đổi trên.

---

## [2026-07-07] Dashboard mặc định cho role Cửa hàng (StoreOperator) — /store/dashboard

**Layer:** POS.Web, POS.Infrastructure, POS.Application
**Loại:** Feature (landing page mới) + Bug fix (resolve git-conflict marker tồn đọng trong docs)

**Bối cảnh:** StoreOperator đăng nhập trước đây bị redirect vào `/store/revenue` — một trang báo
cáo đơn thuần, không phải dashboard tổng quan. Thiết kế qua `/plan` (đã đánh giá performance kỹ —
loại `GetPosTerminalListAsync()` full-scan ~5.000 dòng khỏi trang mặc định, ưu tiên nguồn cached
`GetSaleByTimeAsync` thay cho `GetRevenueSummaryAsync`/`GetRevenueHourlyAsync` không cache) rồi
triển khai qua nhiều lượt chỉnh UI theo yêu cầu người dùng.

**Thay đổi:**
- `src/POS.Web/Components/Pages/Store/StoreDashboardPage.razor` (MỚI): 3 KPI card (Doanh thu/
  Tổng Bill/Void+tỷ lệ, chuẩn `RevenuePage.razor` — `MudGrid xs=12 sm=4` + border-left 4px) +
  2 bar chart cùng hàng tỉ lệ 50:50 (theo giờ hôm nay + 7 ngày gần nhất, cùng nguồn
  `IRptCentralSaleRepository.GetSaleByTimeAsync`) + `MudTable` Void gần nhất (top 10). Header
  động `HeaderTitle` = "Cửa hàng {StoreNo}-{Name}"; ITOps/Admin mặc định xem **TẤT CẢ cửa hàng**
  (`storeNo=null`, không chặn bằng alert "chọn cửa hàng" — đúng pattern `storeNo=null` đã hỗ trợ
  sẵn ở `GetSaleByTimeAsync`/`GetVoidReportAsync`). Auto-refresh `PeriodicTimer` mặc định 120s.
- `ICentralMDRepository.GetPosTerminalListAsync`: thêm tham số `storeNo` optional (mặc định
  `null` = giữ hành vi cũ cho `PosMapPage.razor`) — filter `WHERE pt.StoreNo=@storeNo` tại DB.
  `BusinessDayService.cs` cập nhật truyền `storeNo`, bỏ `.Where()` client-side thừa.
- `MainLayout.razor`: click logo `pos-sidebar-brand` ("RPOS – Quản lý bán hàng") điều hướng tới
  `/store/dashboard` (không có menu link riêng); role StoreOperator luôn mở sẵn cả 3 sub-group
  "CỬA HÀNG" (Vận hành/Giao dịch/Báo cáo) trong `UpdateExpanded()`, ITOps/Admin giữ logic cũ
  (chỉ mở nhánh khớp route).
- `Index.razor`: StoreOperator redirect `/store/dashboard` (thay `/store/revenue`).
- **Fix phụ**: resolve git-conflict marker lồng nhiều lớp (HEAD/dev/minhnb/7ff26a6...) tồn đọng
  từ trước trong `docs/WEB_STATUS.md` + `docs/CHANGELOG.md` (phát hiện khi chạy `/task-done`) —
  giữ nguyên toàn bộ nội dung 2 bên, chỉ bỏ marker + 1 dòng header trùng lặp thật.

**Pattern mới:** Đã cập nhật `docs/architecture` — không có pattern code mới ngoài phạm vi đã ghi
ở `.claude/skills/web/SKILLS.md` (Store Selector Dual Mode, PeriodicTimer auto-refresh đã có sẵn
từ `PosMapPage.razor`/`HealthPage.razor`, tái dùng nguyên).

**Lưu ý cho session sau:**
1. Khi thêm trang landing/auto-refresh mới, ưu tiên `IRptCentralSaleRepository.GetSaleByTimeAsync`
   (cached Redis, SP-based) hơn các method uncached trong `ICentralSaleRepository`
   (`GetRevenueSummaryAsync`/`GetRevenueHourlyAsync`) — tránh nhân tần suất query không cache lên
   theo số lượt đăng nhập.
2. `docs/WEB_STATUS.md`/`docs/CHANGELOG.md` từng bị 2 phiên Claude Code khác nhau ghi đồng thời
   gây conflict marker kẹt trong file (git status không báo vì không phải merge thật đang chờ —
   marker bị gõ thành nội dung thường). Nếu thấy dòng bắt đầu bằng `<<<<<<<`/`=======`/`>>>>>>>`
   trong bất kỳ file `.md` nào, dừng lại kiểm tra trước khi ghi tiếp — khả năng cao có phiên khác
   đang chạy song song trên cùng repo.
3. Chưa verify UI thật trên browser (không có credentials StoreOperator/Admin thật trong môi
   trường làm việc) — chỉ verify qua `dotnet build`/`dotnet test` + smoke-test HTTP status routing.

---

## [2026-07-07] SQL Console — mở rộng sang blacklist + syntax highlighting + PIN gate 2 lớp + fix crash

**Layer:** POS.Web
**Loại:** Feature + Pattern mới + Bug fix

**Bối cảnh:** `SqlConsolePage.razor` ban đầu chỉ whitelist SELECT/INSERT/UPDATE/DELETE/CREATE·ALTER
PROCEDURE, chặn cả CREATE TABLE hợp lệ của người dùng. Theo yêu cầu, đổi hẳn sang mô hình blacklist
(chỉ chặn DROP/TRUNCATE) — nhưng vì console giờ gần như toàn quyền SQL, người dùng (đúng) yêu cầu
thêm PIN gate làm lớp bảo mật thứ 2. Trong lúc build thêm phát hiện 1 bug nghiêm trọng: nhập sai PIN
làm treo UI vô hạn.

**Thay đổi:**
- `src/POS.Web/Services/{ISqlConsoleService,SqlConsoleService}.cs`: `Validate()` đổi từ whitelist
  AST switch sang blacklist — chặn mọi statement `Drop*`/`Truncate*` (theo tên class ScriptDom,
  không liệt kê từng loại), cho phép còn lại. Thêm `StatementKind.TableDdl` (CREATE/ALTER TABLE) và
  `StatementKind.Other` (mọi statement khác không có case riêng).
- `wwwroot/js/sql-console-highlight.js` (**MỚI**): syntax highlighting vanilla JS cho ô SQL — kỹ
  thuật textarea overlay (xem pattern trong `.claude/skills/web/SKILLS.md`), không dùng thư viện
  ngoài (Monaco/CodeMirror). `app.css` thêm `.pos-sql-editor` + màu token `.sql-kw/.sql-str/...`.
- `src/POS.Web/Database/Migrations/004_DashboardUsers_AddPinHash.sql` (**MỚI**): thêm cột
  `PinHash NVARCHAR(200) NULL` vào `DashboardUsers` — PIN riêng từng tài khoản SystemAdmin (không
  tạo bảng mới theo yêu cầu người dùng).
- `src/POS.Web/Auth/{DashboardUser,IWebUserService,WebUserService}.cs`: thêm `PinHash` property,
  `VerifyPinAsync` (BCrypt.Verify + khoá 5 lần sai/15 phút qua Redis counter — `IRedisService`
  không có increment nguyên tử, dùng read-modify-write), `SetPinAsync` (đổi PIN, hash bằng
  `BCrypt.HashPassword` work factor mặc định 11 khớp `PasswordHash`).
- `SqlConsolePage.razor`: toàn bộ nội dung bọc sau `@if (_pinVerified)` — mỗi lần vào trang phải
  nhập lại PIN, không persist trạng thái mở khoá (quyết định người dùng — an toàn nhất trong các
  phương án đã hỏi).
- `Components/Pages/Admin/Dialogs/ChangeMyPinDialog.razor` (**MỚI**) + `UsersPage.razor`: nút "Đổi
  mã PIN" tự phục vụ cho chính tài khoản đang đăng nhập — **bắt buộc nhập đúng PIN cũ trước khi đổi**
  (trừ lần đầu, `PinHash` còn NULL) để tránh 1 cookie bị đánh cắp tự đặt lại PIN rồi vượt qua chính
  lớp bảo vệ vừa thêm.
- **Fix bug nghiêm trọng**: nhập sai PIN làm nút "Xác nhận" quay vô hạn — nguyên nhân là
  `BCrypt.Verify()` ném exception khi `PinHash` không đúng định dạng BCrypt (dễ xảy ra nếu set PIN
  thủ công bằng `UPDATE` plaintext thay vì qua dialog), exception này không được `catch` ở
  `SqlConsolePage.razor.VerifyPinAsync()` (chỉ có `try/finally`) nên lan ra ngoài event handler,
  **crash Blazor Server circuit** — UI kẹt vĩnh viễn ở khung hình cuối vì server chết không gửi lại
  re-render được nữa. Đã thêm `catch` đầy đủ ở cả `WebUserService.VerifyPinAsync` (bọc riêng
  `BCrypt.Verify`) và `SqlConsolePage.razor.VerifyPinAsync()` (catch-all phòng ngừa). Tiện thể sửa
  luôn `redis.Delete(attemptsKey)` (sync-blocking, `.GetAwaiter().GetResult()` bên trong — rủi ro
  treo circuit tương tự) → đổi sang `await redis.StringSetAsync(key, 0, ttlSeconds:...)`.

**Pattern mới:** "Textarea overlay syntax highlighting" + "PIN/step-up gate cho trang nhạy cảm" +
2 anti-pattern (sync method của `IRedisService` trong Blazor Server component; `try/finally` không
đủ cho input nhạy cảm, cần `try/catch/finally`) → đã cập nhật `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:**
- **KHÔNG BAO GIỜ** gọi method sync của `IRedisService` (`Delete`, `HashGet`, `StringSet`...) từ
  trong Razor component Blazor Server — luôn dùng bản `...Async`. Đây là bug lớp đã xảy ra thật.
- `docs/WEB_STATUS.md` đang có **merge conflict markers chưa resolve** (dòng 2, 114-117, 186, 201 —
  `<<<<<<< HEAD` / `=======` / `>>>>>>>`), không liên quan session này — **CHƯA cập nhật file này**,
  cần resolve conflict trước khi ghi thêm (xem báo cáo cuối task-done).
- Go-live cần chạy migration 004 + `UPDATE DashboardUsers.PinHash` cho từng SystemAdmin — xem
  `docs/ROLLOUT.md` §H1b. Hash không bao giờ commit vào git.

---

## [2026-07-07] Coupon "Phát hành thêm mã" — mirror VoucherIssuePage.IssueMoreAsync + gộp/ẩn field form

**Layer:** POS.Common, POS.Application, POS.Infrastructure, POS.Web
**Loại:** Feature (áp dụng lại pattern đã có, không phải pattern mới) + UI refactor

**Bối cảnh:** `VoucherIssuePage.razor` đã có sẵn cơ chế "phát hành thêm 1 lô mã Auto mới cho
voucher đã tồn tại" (nút PHÁT HÀNH ở trang Xem + dialog thu thập Prefix/LenCode/CharOfNumber/
CharPosition/Quantity, tách khỏi form chính). Coupon (`CouponIssuePage.razor`) chưa có khả năng
tương ứng — `SaveIssueAsync` chỉ sinh mã khi tạo mới/coupon chưa có mã, không thể thêm mã cho
coupon đã phát hành. Theo yêu cầu người dùng, port nguyên cơ chế này sang Coupon.

**Thay đổi:**
- `POS.Common/Dtos/SetupCoupon/SetupCouponDtos.cs`: thêm `CouponIssueMoreRequest` (ItemNo/Prefix/
  LenCode/CharOfNumber/CharPosition/Quantity) — khớp `VoucherIssueMoreRequest`.
- `ICouponRepository`/`CouponRepository`: thêm `IssueMoreAsync(itemNo, codes, ct)` → SP mới
  `usp_SetupCoupon_IssueMore`.
- `docs/sql/SetupCoupon_IssueMore.sql` (**MỚI — CHƯA CHẠY trên DB nào**, xem "Lưu ý" bên dưới): SP
  thêm 1 lô mã Auto vào coupon đã tồn tại, không đổi header, không guard tồn tại mã (khác
  `usp_SetupCoupon_SaveIssue` chỉ insert 1 lần) — mirror `usp_SetupVoucher_IssueMore`, có thêm
  `Value`/`VoucherType` snapshot theo `DiscountValue`/`CpnVchType` của Header (khác Voucher dùng
  `DiscountValue`/`ArticleType`) để khớp field "chụp nhanh" mà `usp_SetupCoupon_SaveAdvanced` đồng
  bộ cho các mã cũ.
- `ICouponService`/`CouponService`: thêm `IssueMoreAsync(request, ct)` — inject thêm
  `IVoucherIssueLock` (Redis distributed lock, dùng CHUNG với Voucher, không tạo lock riêng) để
  chặn sinh mã Auto đồng thời.
- `src/POS.Web/.../Dialogs/CouponIssueMoreDialog.razor` (**MỚI**): dialog thu thập Prefix/LenCode/
  CharOfNumber/CharPosition/Quantity — tái dùng cho CẢ 2 luồng (thu thập tham số trước khi tạo mới,
  và phát hành thêm cho coupon đã có), khớp `VoucherIssueMoreDialog`.
- `CouponIssuePage.razor`: (1) bỏ 5 field Prefix/LenCode/CharOfNumber/CharPosition/Quantity khỏi
  form chính (nay chỉ nhập qua dialog) — khác Voucher, Coupon có thêm state "Sửa" (IsEditing=true,
  IsViewMode=false) không chỉ "Xem", nên điều kiện mở dialog trước khi Lưu dùng `NeedsCodeDialog`
  (Auto + (ItemNo rỗng HOẶC chưa có mã)) — rộng hơn điều kiện `!IsEditing` của Voucher; (2) nút
  "PHÁT HÀNH THÊM" mới ở header, chỉ hiện khi `IsViewMode` (theo yêu cầu người dùng); (3) gộp group
  "Thời gian hiệu lực" + "Cấu hình mã & giảm giá" thành 1 group; (4) ẩn field "Giới hạn số lượng"
  (`LimitQty`, cố định `199999999` — đổi từ `999999999` cũ) + "Số lần sử dụng" (`LimitQtyUsed`, cố
  định `1`) + checkbox "Sử dụng nhiều lần" (`IsMultiUsed`, cố định `false`) khỏi form — 3 giá trị
  này nay hardcode ở mọi nơi gán (`SetDefaultsForNewCoupon`/`LoadDetailAsync`/`SaveAsync`), không
  còn đọc từ DB khi load coupon đã có (luôn ép về giá trị cố định trước khi lưu).
- `docs/CURRENT_STRUCTURE.md`, `docs/ROLLOUT.md` (§D3 — 4→5 script), `docs/WEB_STATUS.md`,
  `.claude/skills/cache/SKILLS.md` (ghi chú `IVoucherIssueLock` nay dùng chung Coupon+Voucher): cập
  nhật theo thay đổi trên.

**Pattern mới:** Không có pattern mới — áp dụng lại 2 pattern đã có (`IVoucherIssueLock` dùng chung
domain Coupon/Voucher; dialog `{Domain}IssueMoreRequest`/`{Domain}IssueMoreDialog` tái dùng cho cả
luồng tạo mới và luồng phát hành thêm).

**Lưu ý cho session sau:**
- **BẮT BUỘC chạy `docs/sql/SetupCoupon_IssueMore.sql` trên RPOSMasterData** (dev/UAT/PROD) trước
  khi nút "PHÁT HÀNH THÊM" hoạt động — SP `usp_SetupCoupon_IssueMore` chưa tồn tại trên DB nào.
- `docs/WEB_STATUS.md` và phần đầu file này vẫn còn **conflict marker Git chưa resolve**
  (`<<<<<<< HEAD`/`=======`/`>>>>>>>`, đã ghi nhận từ entry 2026-07-07 "Fix rule sai..." bên dưới) —
  KHÔNG đụng tới trong task này (ngoài phạm vi), chỉ sửa các dòng nằm ngoài vùng conflict
  (`WEB_STATUS.md` dòng ~322-327, phần cây thư mục Coupon/Voucher). Cần dọn riêng trước khi tin
  tưởng phần đầu 2 file này.

---

## [2026-07-07] Dọn UI danh sách Coupon/Voucher + format thời gian đầy đủ (EosShiftsPage)

**Layer:** POS.Web
**Loại:** UI cleanup

**Thay đổi:**
- `CouponsPage.razor` (`/promotion/coupons`): đổi title "Danh sách Coupon / Voucher" →
  "Danh sách Coupon"; bỏ filter + cột "Loại".
- `VouchersPage.razor` (`/promotion/vouchers`): bỏ filter "Số serial" + "Loại"; bỏ cột "Loại"
  trong `MudTable` (giữ nguyên cột "Loại" trong file Excel export — chỉ yêu cầu bỏ ở filter/table).
- `BusinessDayPage.razor` (`/store/business-day`): cột "Thời gian kết thúc ngày" đã sẵn đúng format
  `dd/MM/yyyy HH:mm:ss` — không cần sửa.
- `EosShiftsPage.razor` (`/store/eos-shifts`): cột "Mở ca" (`HH:mm` → `dd/MM/yyyy HH:mm:ss`) và
  "T/G đóng ca" (`HH:mm dd/MM` → `dd/MM/yyyy HH:mm:ss`) — hiện đủ ngày/giờ/giây.

**Pattern mới:** Không có.

**Lưu ý cho session sau:** Không có gì đặc biệt — các thay đổi độc lập, không ảnh hưởng
service/repository layer.

---

## [2026-07-07] Chống trùng mã Auto voucher (concurrency + Redis distributed lock)

**Layer:** POS.Application, POS.Infrastructure
**Loại:** Bug fix (rủi ro tiềm ẩn, chưa xảy ra trong prod) + Pattern mới

**Bối cảnh:** Phân tích `VoucherIssuePage.razor` → `VoucherService.SaveIssueAsync`/`IssueMoreAsync`
phát hiện thuật toán sinh mã Auto (`CouponVoucherCodeGenerator.GenerateAutoCodes`) dựa vào
`UtcNow.ToUnixTimeMilliseconds() + i` (dễ trùng khi 2 request Auto-issue chạy cùng millisecond) và
luồng check-tồn-tại-rồi-insert tách thành 2 round-trip DB riêng (race window nếu 2 request chạy
song song). Yêu cầu bổ sung: giải pháp phải hoạt động cả khi POS.Web scale-out nhiều instance sau
load balancer → khóa in-process (`SemaphoreSlim`/`ISyncFileLock`) không đủ, cần Redis.

**Thay đổi:**
- `CouponVoucherCodeGenerator.cs`: đổi `Random` (seed theo `UtcNow`) → `RandomNumberGenerator`
  (crypto-strength) cho phần số/ký tự của mã Auto — loại bỏ nguồn trùng chính khi 2 request rơi
  cùng millisecond. `GenerateRandomSerial` (Serial, không cần unique) giữ `Random` cũ.
- `IRedisManager`/`RedisManager.cs` (`POS.Infrastructure/Cache/`): thêm `AcquireLockAsync`/
  `ReleaseLockAsync` — `SET key token NX PX ttl` atomic + Lua script release (so khớp token trước
  `DEL`, tránh xoá nhầm lock của instance khác).
- `IVoucherIssueLock`/`VoucherIssueLock.cs` (MỚI, `POS.Infrastructure/Locking/`): Redis distributed
  lock, key cố định `"Lock:VoucherIssue"` (TTL 30s, poll 300ms, timeout chờ 15s) — đăng ký Singleton
  trong `DependencyInjection.cs`.
- `VoucherService.cs`: bọc toàn bộ đoạn sinh mã + check-tồn-tại + insert (`SaveIssueAsync` nhánh có
  sinh mã, và `IssueMoreAsync`) trong `IVoucherIssueLock.AcquireAsync` — loại bỏ hoàn toàn race
  check-then-insert vì không còn 2 request nào chạy đồng thời đoạn này.
- `docs/CURRENT_STRUCTURE.md`: thêm cây `Locking/`, chữ ký `IRedisManager` mới, entry
  `IVoucherIssueLock` vào bảng Interface/DI.

**Pattern mới:** Distributed lock qua Redis (`IRedisManager.AcquireLockAsync`/`ReleaseLockAsync` +
wrapper domain-specific như `VoucherIssueLock`) → đã cập nhật `.claude/skills/cache/SKILLS.md`
(Pattern 6).

**Lưu ý cho session sau:** Không sửa 2 stored procedure `usp_SetupVoucher_SaveIssue`/
`usp_SetupVoucher_IssueMore` để retry-on-duplicate — vì với lock toàn cục, race đã bị loại trừ nên
không cần thêm phức tạp ở SP; DB unique constraint (`UX_CpnVchBOMCodeIssue_Code`) vẫn là lưới an
toàn cuối cho mã trùng với dữ liệu lịch sử. `CouponService` dùng chung
`CouponVoucherCodeGenerator` + bảng `CpnVchBOMCodeIssue` nên có cùng loại rủi ro nhưng **chưa**
được bọc `IVoucherIssueLock` (ngoài phạm vi task này — cân nhắc áp dụng tương tự nếu cần).

---

## [2026-07-07] Fix rule sai + thêm validate % cho Coupon/Voucher (Giá trị giảm giá)

**Layer:** POS.Web
**Loại:** Bug fix + Business rule mới

**Thay đổi:**
- `CouponIssuePage.razor` (`/promotion/coupons/issue`): xoá rule sai trong `SaveAsync` —
  `if (_advanced.MaxValue > 0 && _advanced.DiscountValue < _advanced.MaxValue)` so sánh trực tiếp
  `DiscountValue` (%) với `MaxValue` (VNĐ), sai đơn vị khi `DiscountType == 1` (Percent), luôn chặn
  lưu vô lý.
- `CouponIssuePage.razor` + `VoucherIssuePage.razor`: thêm rule mới — khi `DiscountType == 1` (%),
  `DiscountValue` phải thoả `0 < DiscountValue <= 100`. Validate ở `SaveAsync`
  (Coupon)/`ValidateHeaderFields()` (Voucher, dùng chung cho cả luồng Import và luồng dialog
  "PHÁT HÀNH VOUCHER" Auto) + set `Max` động trên `MudNumericField` (property `DiscountValueMax`,
  `100` khi Percent / `MaxValue` kiểu số khi Amount) để giới hạn ngay từ UI.

**Pattern mới:** `MudNumericField` validate/giới hạn theo giá trị 1 field khác (Percent vs Amount) →
đã cập nhật `.claude/skills/web/form-input.md` §5a.

**Lưu ý cho session sau:** `CouponAdvancedDialog.razor` (`Submit()`) đã có sẵn đúng rule 0-100%
này từ trước — không cần sửa, chỉ 2 page chính (`CouponIssuePage`/`VoucherIssuePage`) thiếu.
Ngoài ra, `docs/WEB_STATUS.md` và phần dưới `docs/CHANGELOG.md` đang có **conflict marker Git
chưa resolve** (`<<<<<<< HEAD`/`=======`/`>>>>>>>`) committed thẳng vào nội dung file — không phải
do task này gây ra, nhưng cần dọn riêng trước khi tin tưởng nội dung phần đó.

---

## [2026-07-07] Fix rule chặn xác nhận EOD — phân biệt "Chưa mở ca" vs "Chưa đóng ngày"

**Layer:** POS.Application, POS.Web
**Loại:** Bug fix (business rule)

**Bối cảnh:** `docs/web/logic/eod.md` §3 mô tả rule cũ: chặn xác nhận (StoreOperator, không force)
hễ có ≥1 máy POS `IsClosed=false`, không phân biệt "Chưa mở ca" (`LastSaleTime=null`, chưa từng
bán hàng) hay "Chưa đóng ngày" (đã bán hàng nhưng chưa đóng ngày). User yêu cầu tách rõ: cửa hàng
có 1 số máy "Chưa mở ca" nhưng các máy còn lại đã "Đã đóng ngày" hết vẫn phải được xác nhận —
chỉ "Chưa đóng ngày" mới thực sự chặn; ngoại lệ: nếu KHÔNG có máy nào đã đóng ngày (toàn bộ "Chưa
mở ca") thì vẫn chặn.

**Thay đổi:**
- `src/POS.Application/Features/StoreActivities/BusinessDayService.cs`: `ConfirmBusinessDayAsync`
  — thay điều kiện chặn `staging.Any(!IsClosed)` bằng 2 check: (1) `blocking` = máy có
  `LastSaleTime != null && !IsClosed` → luôn chặn; (2) `staging.All(!IsClosed)` (không máy nào
  đóng ngày) → chặn riêng, message khác.
- `src/POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor`: `CanConfirm` đổi thành
  `staging.Any(IsClosed) && !staging.Any(BlocksConfirm)` cho nhánh StoreOperator (không force);
  thêm helper `BlocksConfirm`/`BlockingCount`; tách alert cảnh báo thành 2 nhánh rõ lý do chặn.
- `docs/web/logic/eod.md`: cập nhật §2 (điều kiện bật nút Xác nhận) và §3 (note rule chặn) khớp
  logic mới.

**Lưu ý cho session sau:**
1. ITOps/SystemAdmin (`allowForceConfirm=true`) không đổi hành vi — vẫn force được bất kể trạng
   thái các máy POS.
2. Chưa verify qua UI/DB thực tế (không có môi trường CentralSale/CentralMD trong phiên) — chỉ
   verify bằng build (`dotnet build src/POS.Web/POS.Web.csproj` 0 lỗi) + `dotnet test
   tests/POS.ContractTests` (25/25 xanh) + đối chiếu tay 3 ví dụ nghiệp vụ user đưa ra.
3. Phát hiện phụ (KHÔNG sửa, ngoài phạm vi task): `docs/WEB_STATUS.md` đang có conflict marker
   Git chưa resolve (dòng 2, 114-117, 186-201 — tìm `<<<<<<<`/`=======`/`>>>>>>>`), đã tồn tại từ
   trước (không nằm trong `git status` diff của session này) — cần dọn riêng, không tự ý resolve
   khi không rõ ý đồ 2 nhánh.

---

## [2026-07-06] Runbook deploy POS.Worker lên Ubuntu (Docker) + appsettings.UAT.json

**Layer:** Deployment (`deploy/`, `docs/deploy/`), POS.Worker (config)
**Loại:** Tài liệu deploy mới + bổ sung file cấu hình còn thiếu

**Bối cảnh:** `deploy/windows/POS.Worker.Task.xml` là cách chạy tạm trên Windows dev. Cần chính thức
hóa cách chạy POS.Worker trên Ubuntu (Docker), song song với POS.Web (đã có nginx). Đã khảo sát: hạ
tầng Docker cho Worker đã có sẵn ~95% (`Dockerfile.worker`, `docker-compose.yml`,
`docs/guide-deploy.md` §3.3, `docs/deploy/ubuntu-guide.md`) — chỉ thiếu `appsettings.UAT.json` cho
Worker (Api/Web cũng thiếu file này nhưng ngoài phạm vi task). `appsettings.Production.json` của
Worker đã khớp chính xác với của POS.Api (`mssql_2019,1433`, `host.docker.internal`,
`172.17.0.1:6379`) nên không cần sửa.

**Thay đổi:**
- `src/POS.Worker/appsettings.UAT.json` (MỚI): mirror cấu trúc `appsettings.Production.json`, thay
  giá trị hạ tầng bằng placeholder `<UAT_...>`. Build + `dotnet test tests/POS.ContractTests` đã xanh.
- `docs/deploy/pos-worker-ubuntu-guide.md` (MỚI): runbook đầy đủ — bảng so sánh Windows Task
  Scheduler ↔ Docker (`restart: unless-stopped` là tương đương của `BootTrigger`+`RestartOnFailure`),
  lý do nginx không áp dụng cho Worker (không có HTTP endpoint), build/run/verify/update/rollback
  cho cả UAT và PROD, checklist cuối bài.
- `docs/guide-deploy.md` §3.3: thêm dòng trỏ sang runbook mới.
- `deploy/windows/README.md`: thêm mục cuối bài trỏ sang runbook Ubuntu.

**Lưu ý cho session sau:**
1. Phát hiện phụ (chưa sửa, ngoài phạm vi task): `src/POS.Worker/appsettings.Production.json` mục
   `ConnectionStrings` thiếu key `BootstrapServers` (có trong `appsettings.json` base) — nếu Worker
   thật sự dùng Kafka producer ở Production, cần bổ sung key này (đã bổ sung sẵn trong
   `appsettings.UAT.json` mới tạo, kèm placeholder `<UAT_KAFKA_HOST>`).
2. `docs/CHANGELOG.md` (file này) đang có conflict marker Git chưa resolve còn sót lại từ merge cũ
   (dòng ~291-336 và ~565-602, tìm `<<<<<<<`/`=======`/`>>>>>>>`) — KHÔNG phải do task này gây ra,
   cần dọn riêng khi có thời gian, không tự ý resolve khi không rõ ý đồ của cả 2 nhánh.

---

## [2026-07-06] Phát hành nhiều lần từ 1 mã phát hành Voucher (8.3)

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature + Pattern mới

**Bối cảnh:** `VoucherIssuePage.razor` trước đây chỉ cho phép tạo 1 header + 1 lô mã trong đúng 1
lần lưu (`SaveIssueAsync` → SP `usp_SetupVoucher_SaveIssue`). SP đó có guard `IF NOT EXISTS` chặn
insert mã nếu ItemNo đã có mã — gọi lại để "phát hành thêm" sẽ bị bỏ qua âm thầm. Yêu cầu nghiệp
vụ mới: 1 header có thể nhận thêm nhiều lô mã Auto bất kỳ lúc nào.

**Thay đổi:**
- `docs/sql/SetupVoucher_IssueMore.sql` (MỚI): SP `usp_SetupVoucher_IssueMore` — không guard tồn
  tại, luôn insert mã mới; `ItemNo` không tồn tại → `THROW 50002`. **Chưa chạy trên DB thật.**
- `POS.Common/Dtos/Voucher/SetupVoucherDtos.cs`: thêm `VoucherIssueMoreRequest`.
- `IVoucherRepository`/`VoucherRepository`: thêm `IssueMoreAsync(itemNo, codes, ct)`.
- `IVoucherService`/`VoucherService`: thêm `IssueMoreAsync(request, actor, ct)` — tái dùng
  `CouponVoucherCodeGenerator.GenerateAutoCodes` + `CheckCodesExistAsync` như `SaveIssueAsync`.
- `Dialogs/VoucherIssueMoreDialog.razor` (MỚI): dialog dùng chung cho cả 2 luồng (tạo mới + phát
  hành thêm), chỉ thu thập Prefix/LenCode/CharOfNumber/CharPosition/Quantity.
- `VoucherIssuePage.razor`: gộp 2 nhóm form thành 1 "Thông tin chung", hiện field `MaxAmount`
  (Giảm giá tối đa — trước ẩn/hardcode 0) thành editable, thêm nút "PHÁT HÀNH VOUCHER" (tạo mới)
  và "PHÁT HÀNH" (xem chi tiết), xóa `CodeFieldsLocked` (dead code).
- `docs/CURRENT_STRUCTURE.md`, `docs/architecture/centralMD-schema.md`: cập nhật entry SP mới + mô
  tả method mới.
- `.claude/skills/database/SKILLS.md`: thêm pattern mới "SP tạo lần đầu (guard) vs SP phát hành
  thêm (append, không guard)".

**Pattern mới:** SP "tạo lần đầu" (guard tồn tại) vs SP "phát hành thêm" (append, không guard) →
đã thêm vào `.claude/skills/database/SKILLS.md`.

**Verify:** `dotnet build POS.slnx` 0 lỗi (23 warning có sẵn, không liên quan). `dotnet test
tests/POS.ContractTests` 25/25 xanh. **CHƯA chạy SQL script mới trên DB thật, CHƯA test tay qua
browser** (không có môi trường DB/POS.Web chạy thật trong phiên này).

**Lưu ý cho session sau:**
1. **BẮT BUỘC chạy `docs/sql/SetupVoucher_IssueMore.sql` trên `RPOSMasterData`** (dev/UAT/PROD)
   trước khi tính năng "PHÁT HÀNH" hoạt động — app không tự tạo SP.
2. Đi kèm báo cáo Retail BA (cùng phiên) phát hiện `LimitQty` (giới hạn tổng số mã/header) và
   `MaxAmount` (giảm giá tối đa, vừa hiện lên UI) đều **chưa được enforce ở tầng redemption/SP** —
   xem chi tiết trong plan file phiên đó nếu cần triển khai tiếp.

---

## [2026-07-06] Danh mục nhóm giá (StorePriceGroup) — CRUD mới trong menu Giá bán

**Layer:** POS.Web, POS.Application, POS.Infrastructure, POS.Common + SQL
**Loại:** Feature

**Bối cảnh:** cần chức năng quản lý nhóm giá (mỗi nhóm = mã + tên gắn 1 danh sách cửa hàng;
PriceGroupCode dùng làm SalesCode khi khai báo giá). Dùng 2 bảng có sẵn trên DB:
`StorePriceGroupHeader` (mới, chưa có ở repo) làm header + `StorePriceGroup` (đã có) làm chi tiết
store, link qua `PriceGroupCode`. Legacy `PriceData.CreatePriceGroup/SaveStorePriceGroup` làm tham
chiếu nghiệp vụ (cột Type=1, Pkey='{Store}-{PriceGroupCode}', Priority cấp nhóm, Counter=MAX+1).

**Quyết định nghiệp vụ (chốt với user qua AskUserQuestion):**
1. Xóa = hard-delete header + chi tiết, NHƯNG chặn nếu PriceGroupCode đang dùng trong
   `SalesPrice.SalesCode` (dòng active: IsActive=1 AND YEAR(EndingDate)<>7777).
2. Priority cấp nhóm — 1 giá trị áp cho mọi store trong nhóm.
3. Danh sách store add-only — chỉ thêm mới, không bỏ được store đã gán (store cũ read-only trên UI).
4. Form dạng dialog (khớp mẫu SiteGroupSetupDialog).

**Thay đổi:**
- `POS.Common/Dtos/Price/StorePriceGroupDto.cs` (MỚI): `PriceGroupListFilter`,
  `PriceGroupListItemDto`, `PriceGroupStoreItemDto`, `PriceGroupSaveRequest` (tái dùng `PriceSaveResult`).
- `IPriceRepository`/`PriceRepository.cs`: +4 method `GetPriceGroupListAsync` (inline SQL header +
  subquery StoreCount/Priority, paging), `GetPriceGroupStoresAsync`, `SavePriceGroupAsync` (SP + TVP),
  `DeletePriceGroupAsync` (SP) — Save/Delete OK thì `redis.Delete("MD:PriceGroupOptions")`. Helper
  `BuildStoreTable`. KHÔNG thêm DI mới (interface đã đăng ký) → DI test tự xanh.
- `IPriceService`/`PriceService.cs`: +4 method thin delegate; validate mã/tên/Priority>0/≥1 store ở
  `SavePriceGroupAsync`.
- `docs/sql/StorePriceGroup_Save.sql` (MỚI): TVP `dbo.StorePriceGroupStoreTVP` + SP
  `usp_StorePriceGroup_Save` (upsert header + add-only chi tiết: UPDATE Priority/Name/Counter cho store
  cũ, INSERT store mới, Type=1, Counter=ReplicationCounter=MAX+1, OUTPUT @Ok/@Message).
- `docs/sql/StorePriceGroup_Delete.sql` (MỚI): SP `usp_StorePriceGroup_Delete` (chặn nếu đang dùng
  trong SalesPrice, ngược lại xóa cả 2 bảng, OUTPUT @Ok/@Message).
- `PriceGroupsPage.razor` (MỚI, `/catalog/price-groups`): list + filter + MudTable ServerData +
  MudMessageBox @ref confirm xóa (YesButton Outlined/Error) + audit CREATE/UPDATE/DELETE.
- `Dialogs/PriceGroupSetupDialog.razor` (MỚI): form mã(disabled khi Sửa)/tên/độ ưu tiên + store picker
  MudAutocomplete `.Take(50)` + lưới store (store cũ read-only, store mới xóa được). Store picker dùng
  `@ref` + `ClearAsync()` sau mỗi lần thêm (rỗng ô để thêm mục kế tiếp), chống trùng bỏ qua im lặng
  (không báo lỗi). Khối catch khi Lưu hiện thẳng `ex.Message` để chẩn đoán (vd SP chưa tạo trên DB).
- `MainLayout.razor`: 3 chỗ — NavLink "Danh mục nhóm giá", BreadcrumbMap, `_expandCatPrice`.
- `database-schema.md`: thêm bảng `StorePriceGroupHeader` + ghi chú SP; `CURRENT_STRUCTURE.md`: thêm
  DTO + note service/repo.

**Pattern mới:** "MudAutocomplete thêm-vào-danh-sách (multi-add picker)" — `@ref` + `ClearAsync()` sau
mỗi lần thêm, chống trùng bỏ qua im lặng, KHÔNG dùng ResetValueOnEmptyText (crash circuit). Đã thêm vào
`.claude/skills/web/SKILLS.md` (biến thể của store-picker). Khuôn list/SP/TVP tái dùng có sẵn.

**Lưu ý cho session sau:** chạy tay 2 script SQL trên `RPOSMasterData` (DEV trước) TRƯỚC khi test UI:
`StorePriceGroup_Save.sql`, `StorePriceGroup_Delete.sql` (app không tự tạo SP). Priority chỉ lưu ở dòng
`StorePriceGroup` (header không có cột Priority) → bắt buộc ≥1 store khi tạo để priority luôn được lưu.
Verify: `dotnet build src/POS.Web` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25 — chưa test UI thật.

---

## [2026-07-06] FIX: PriceSetupPage import bảng giá — UOM validate rỗng + giá bán thiếu format

**Layer:** POS.Web, POS.Infrastructure + SQL
**Loại:** Bug fix

**Bối cảnh:** người dùng báo tải file mẫu `/catalog/price-setup` (`/catalog/price-declare`) về rồi
upload lại thì báo "không tồn tại mã đơn vị tính", trong khi nhập tay ĐVT vẫn lưu được bình thường.
Điều tra xác nhận: `DownloadTemplateAsync` (`PriceSetupPage.razor`) ghi dòng mẫu hard-code
(`ItemNo="65000165"`, `UOM="GOI"`) không kiểm chứng với DB — cặp Item+UOM này không tồn tại thật
trong `dbo.ItemUnitOfMeasure` ở môi trường test, khiến `ValidateImportAsync`
(`PriceRepository.cs`) JOIN thất bại → trả lỗi. Nhập tay không gặp lỗi vì dropdown ĐVT
(`GetItemUomsAsync`) chỉ hiển thị các mã có thật cho đúng Item đã chọn — không thể chọn sai "by
construction". Trong lúc kiểm tra thêm phát hiện bug thứ 2: cột "Giá bán" khi import không có dấu
phân cách hàng nghìn như khi nhập tay (`LoadImportAsync` gán thẳng chuỗi thô từ Excel, không qua
`FormatThousands` như ô nhập tay `OnPriceChanged`).

**Thay đổi:**
- `PriceRepository.cs` (`ValidateImportAsync`): SQL thêm fallback `ISNULL(U.Code,
  SalesUnitOfMeasure)` cho cột `Uom` trả về + điều kiện lỗi tương ứng — khi không tìm thấy dòng
  `ItemUnitOfMeasure` khớp chính xác, dùng ĐVT bán mặc định của Item thay vì báo lỗi cứng.
- `PriceSetupPage.razor` (`LoadImportAsync`): `UnitPrice = FormatThousands(v.UnitPrice)` thay vì
  gán thẳng `v.UnitPrice ?? string.Empty` — dòng import nay hiển thị `"20,000"` giống hệt dòng nhập
  tay.
- `docs/sql/SetupSalePrice_Save.sql`, `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`: rewrite
  `dbo.Setup_SalePrice_Get_ALL`/`dbo.usp_SetupSalePrice_Save` sang engine set-based MERGE (hàm mới
  `dbo.tvf_SetupSalePrice_Timeline`) xử lý chồng lấn khoảng ngày hiệu lực (soft-delete + interval
  split) thay vì gọi SP legacy lồng transaction; thêm filter `IsActive`/tombstone
  (`YEAR(EndingDate)<>7777`) + `ORDER BY Counter DESC` khi tra Pkey cho Sửa/Xóa giá.

**Lưu ý cho session sau:** file mẫu Excel vẫn còn dòng ví dụ hard-code (`65000165`/`GOI`) chưa sửa
— nếu Item+UOM đó không tồn tại ở môi trường khác, round-trip tải-mẫu-rồi-upload-ngay vẫn có thể
lỗi (đã giảm nhẹ bằng fallback SalesUnitOfMeasure nhưng chưa triệt để). Nên cân nhắc sinh dữ liệu
mẫu từ query DB thật hoặc để trống ItemNo/UOM ở dòng mẫu trong lần sửa tiếp theo. SP cần chạy tay:
`docs/sql/SetupSalePrice_Save.sql`, `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`. Verify:
`dotnet build src/POS.Web/POS.Web.csproj` 0 lỗi.

---

## [2026-07-06] Bổ sung check Validity_From_Date cho SAP CheckVoucher

**Layer:** POS.Application
**Loại:** Bug fix nghiệp vụ (bổ sung điều kiện còn thiếu)

**Bối cảnh:** `SAPService.CheckVoucherAsync` (dùng chung bởi `SAPController.CheckVoucher` và
`CheckReturnVoucher`) chỉ so sánh `Expiry_Date` (hết hạn) mà chưa từng so sánh `Validity_From_Date`
(ngày hiệu lực bắt đầu) — voucher có `Validity_From_Date` ở tương lai vẫn pass qua và trả về
`Status = OK`, sai nghiệp vụ. Đã kiểm tra `src/legacy/` (VCM.BLUEPOS), không có logic tương đương
để tham chiếu — đây là bổ sung nghiệp vụ mới, không phải port.

**Thay đổi:**
- `src/POS.Application/Features/Sap/SAPService.cs` (`CheckVoucherAsync`): thêm nhánh kiểm tra
  `Validity_From_Date` ngay sau khối `isExpired`, trước khối `AVL`. Voucher hợp lệ khi
  `Validity_From_Date <= hôm nay <= Expiry_Date`.
  - Parse fail/rỗng `Validity_From_Date` → `404 NotFound`, `"Mã Voucher/Coupon không tồn tại"`
    (khác hành vi hiện tại của `Expiry_Date` khi parse fail — quyết định có chủ đích của user).
  - `Validity_From_Date > hôm nay` → `400 BadRequest`, `"Voucher/coupon chưa đến ngày hiệu lực"`,
    set `data.Return = "1"` và ghi đè `data.Status = "EXP"` (tái dùng mã có sẵn, không thêm status mới).

**Lưu ý cho session sau:** Đây là bổ sung điều kiện nghiệp vụ mới cho `CheckVoucherAsync`, không
đổi contract JSON (không thêm/đổi tên field response) — chỉ đổi giá trị `Status`/`Return`/`Message`
trong 2 case lỗi mới. Không cần cập nhật DTO hay `CURRENT_STRUCTURE.md` vì không có
class/method/interface mới được tạo.

---

## [2026-07-06] Đổi ngữ nghĩa IsCheckItem Voucher khớp Coupon (đảo bit)

**Layer:** POS.Web, POS.Application, POS.Common
**Loại:** Bug fix nghiệp vụ (đảo ngữ nghĩa cột, có data migration)

**Bối cảnh:** người dùng báo checkbox "Áp dụng theo danh sách sản phẩm" trên `VoucherIssuePage.razor`
có vẻ gán ngược `IsCheckItem`. Điều tra kỹ (2 vòng Explore đối chiếu 7 nguồn: legacy Controller,
legacy View/checkbox thực tế `chk-total-bill`, `VoucherService.cs`, SP `usp_SetupVoucher_Save`/
`SaveIssue` — logic thật không chỉ comment, `docs/architecture/centralMD-schema.md`) xác nhận code
CŨ **không phải bug** — nó khớp đúng quy ước legacy Voucher (`IsCheckItem=1`=tổng bill), chỉ NGƯỢC
với Coupon (`IsCheckItem=1`=theo sản phẩm) vì 2 module viết độc lập ở legacy VCM.BLUEPOS. Sau khi
trình bày bằng chứng, **người dùng quyết định đổi ngữ nghĩa Voucher khớp Coupon** (chấp nhận rủi ro
đã nêu: cần data migration + rủi ro chưa xác nhận được 100% liệu `CpnVchBOMHeader` có nằm trong
`SyncTableList` hay không — không có bằng chứng POS client tự đọc field này).

**Thay đổi:**
- `VoucherIssuePage.razor`: bỏ đảo `!` — `_applyPerItem = d.IsCheckItem` (load) và
  `_model.IsCheckItem = _applyPerItem` (save), khớp pattern Coupon không đảo.
- `SetupVoucherDtos.cs`: sửa toàn bộ comment "NGƯỢC coupon" → mô tả nghĩa mới (4 vị trí).
- `VoucherService.cs`: đảo điều kiện validate `SaveAsync`/`SaveIssueAsync` (require Items khi
  `IsCheckItem=true`, clear Items khi `false`) — khớp chính xác `CouponService.cs`.
- `VouchersPage.razor`: đảo điều kiện hiển thị chip + export Excel cột "Tổng bill" (giữ nguyên
  label hiển thị, chỉ đảo biểu thức boolean để khớp data sau migration).
- SP `docs/sql/SetupVoucher_Save.sql` + `docs/sql/SetupVoucher_SaveIssue.sql`: đảo branch
  `IF @IsCheckItem = 1` (từ `=0`) khi quyết định insert `CpnVchBOMLine` — **CHƯA chạy trên DB
  thật**, cần chạy lại 2 script này trên `RPOSMasterData` SAU khi deploy code.
- **File SQL mới** `docs/sql/Voucher_FlipIsCheckItem_Migration.sql` — data migration 1 lần, đảo
  bit `IsCheckItem` CHỈ cho `ArticleType IN ('ZVCN','ZVCO')` (Coupon giữ nguyên), có SELECT COUNT
  đối chiếu trước/sau + transaction.
- `docs/architecture/centralMD-schema.md`: cập nhật comment cột `IsCheckItem` (dòng ~1682),
  `CpnVchBOMLine` (dòng ~1766), SP `usp_SetupVoucher_Save`/`SaveIssue` — nay Coupon & Voucher
  dùng chung 1 nghĩa.
- `docs/ROLLOUT.md` — thêm mục **D10** (CRITICAL): thứ tự bắt buộc deploy code → chạy lại 2 SP →
  chạy migration data, kèm cảnh báo hậu quả nếu sai thứ tự.

**Lưu ý cho session sau:** `VoucherService.SaveAsync` (khác `SaveIssueAsync`) hiện KHÔNG có caller
nào trong `POS.Web`/`POS.Api` (orphaned) nhưng vẫn được đồng bộ đảo logic cho nhất quán — nếu sau
này dùng lại, hành vi đã đúng khớp Coupon. Verify: `dotnet build` (POS.Web/POS.Application/
POS.Common) 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25 Passed. **Chưa verify trên DB
thật** (chưa chạy SP + migration script) — bắt buộc theo đúng thứ tự D10 trước khi coi voucher cũ
hiển thị đúng.

---

## [2026-07-06] usp_Product_Save: giới hạn ItemNo 8 ký tự + VariantCode/Pkey Barcodes

**Layer:** POS.Infrastructure (SQL script)
**Loại:** Feature/tuning nghiệp vụ theo yêu cầu người dùng

**Bối cảnh:** người dùng xác nhận Lưu sản phẩm chạy được sau fix `usp_Product_Save` (TRY_CAST,
entry trước), yêu cầu thêm 2 điều chỉnh nghiệp vụ khi tạo sản phẩm.

**Thay đổi** (gộp vào cùng `docs/sql/Product_Save.sql`, không đổi code C#/Razor):
- `ItemNo` tự sinh giới hạn **tối đa 8 ký tự** — seed `1000000001` (10 chữ số) → `10000001`
  (8 chữ số); mệnh đề `WHERE LEN(No) <= 8` khi tính `MAX` để bỏ qua các `No` dài hơn có sẵn
  (từ seed cũ hoặc dữ liệu legacy), giữ dãy số tự sinh luôn nằm trong không gian 8 chữ số.
- `dbo.Barcodes.VariantCode` — trước insert rỗng `''`, nay lưu **cùng giá trị** với
  `UnitOfMeasureCode` (ĐVT chọn trên UI cho từng dòng Barcode).
- `dbo.Barcodes.Pkey` — trước = `BarcodeNo` (một mình), nay = `"{ItemNo}-{BarcodeNo}"`, khớp
  convention Pkey ghép đã dùng ở các bảng khác (vd `dbo.ItemBlock`).

**Lưu ý cho session sau:** vẫn chỉ 1 file script (`docs/sql/Product_Save.sql`, đã gộp cả 2 đợt
fix trong ngày 2026-07-06) — **BẮT BUỘC chạy lại trên RPOSMasterData** để áp cả 2 thay đổi này
(xem D9 `docs/ROLLOUT.md`), script idempotent an toàn chạy đè SP cũ.

---

## [2026-07-06] FIX nghiêm trọng: usp_Product_Save chặn tạo mới MỌI sản phẩm (CAST→TRY_CAST)

**Layer:** POS.Web, POS.Infrastructure (SQL script)
**Loại:** Bug fix (nghiêm trọng — chặn hoàn toàn 1 chức năng)

**Bối cảnh:** người dùng test tạo sản phẩm mới ở `/catalog/products`, dialog báo "Lỗi hệ thống. Vui
lòng thử lại." Đọc trực tiếp log thật `D:\ROOT\Logs\POS.Web\Exception\log-20260706.txt` (không đoán)
xác nhận: `Microsoft.Data.SqlClient.SqlException: Error converting data type nvarchar to bigint`
(Error 8114), stack trace trỏ thẳng `CentralMDRepository.CreateProductAsync` → `dbo.usp_Product_Save`.

**Root cause:** `docs/sql/Product_Save.sql` bước sinh `ItemNo` tự động dùng
`CAST(No AS BIGINT)` chạy trên **toàn bộ** `dbo.Item`. SQL Server evaluate `CAST` cho mọi dòng
trước khi `MAX()` — chỉ cần 1 dòng `No` cũ không phải số thuần (mã hàng alphanumeric còn tồn tại
trong dữ liệu thật) là toàn câu lệnh throw, chặn tạo mới **mọi** sản phẩm chứ không riêng gì 1
trường hợp cụ thể.

**Thay đổi:**
- `docs/sql/Product_Save.sql`: `CAST(No AS BIGINT)` → `TRY_CAST(No AS BIGINT)` (trả `NULL` thay vì
  throw khi không convert được; `MAX` tự bỏ qua `NULL`). Script vẫn idempotent (`DROP`+`CREATE`).
- `docs/ROLLOUT.md`: thêm D9 — **BẮT BUỘC chạy lại script đã fix trên RPOSMasterData**, mức CRITICAL.
- `docs/WEB_STATUS.md`: K2 cập nhật ⚠️ CRITICAL, changelog đầu file.

**Tiện thể (cùng session, theo yêu cầu người dùng):** gộp 2 dropdown "Đơn vị cơ sở"/"Đơn vị bán"
trong `ProductDetailDialog.razor` thành 1 "Đơn vị tính" (bản chất chỉ 1 UOM) — `SaveAsync` tự gán
`_model.SalesUnitOfMeasure = _model.BaseUnitOfMeasure` trước khi gọi `CreateProductAsync`, không
đổi `ProductCreateDto`/SP.

**Pattern/lưu ý cho session sau:** khi debug lỗi "Lỗi hệ thống" chung chung trong POS.Web, **luôn
đọc file log thật** (`{FileLogDirectory}/Exception/log-{yyyyMMdd}.txt`, ví dụ
`D:\ROOT\Logs\POS.Web\Exception\`) trước khi đoán nguyên nhân từ code — message hiển thị trên UI
luôn bị generic hóa (`_errorMsg = "Lỗi hệ thống..."`) để không lộ chi tiết SQL cho end-user, nhưng
log file có đầy đủ `SqlException`/stack trace/Error Number. Cũng cần cảnh giác `CAST`/không dùng
`TRY_CAST`/`TRY_CONVERT` trên cột lưu mã hỗn hợp số+chữ (`No`, `ItemNo`...) khi tính `MAX`/aggregate
— dữ liệu legacy thường không thuần nhất định dạng.

---

## [2026-07-06] Fix hardcode ArticleType khi phát hành Coupon/Voucher

**Layer:** POS.Web, POS.Application
**Loại:** Bug fix

**Bối cảnh:** yêu cầu đảm bảo `ArticleType` luôn được gán đúng giá trị mặc định trước khi lưu
xuống DB cho luồng Phát hành Coupon (`ZCPN`) và Phát hành Voucher (`ZVCN`). Dò luồng phát hiện
`CouponIssuePage.razor`/`CouponRepository.cs` đã default đúng `"ZCPN"` từ trước (DTO default +
repository guard); nhưng `VoucherIssuePage.razor` đang hardcode sai giá trị **`"ZTRD"`** — không
khớp convention hệ thống (`VoucherROPEnum.ZVCN=2`, `SAPService.cs`, `CouponsPage.razor` filter đều
dùng `"ZVCN"` cho Voucher).

**Thay đổi:**
- `CouponIssuePage.razor` (`SaveAsync`): thêm `_model.ArticleType = "ZCPN"` hardcode ngay trước
  khi gọi `CouponService.SaveIssueAsync`.
- `CouponService.cs` (`SaveIssueAsync`/`SaveAdvancedAsync`): thêm defensive-assign
  `request.ArticleType = string.IsNullOrWhiteSpace(...) ? "ZCPN" : request.ArticleType` — lớp bảo
  vệ thứ 2 nếu có caller khác gọi trực tiếp Service không qua UI.
- `VoucherIssuePage.razor`: đổi toàn bộ hardcode sai `"ZTRD"` → `"ZVCN"` (model init, `SetDefaultsForNew`,
  `LoadDetailAsync` fallback) + thêm `_model.ArticleType = "ZVCN"` trong `SaveAsync()` trước khi
  gọi Service.
- `VoucherService.cs` (`SaveIssueAsync` — method thực sự được `VoucherIssuePage` gọi): đổi validate
  bắt buộc chọn loại voucher thành defensive-assign default `"ZVCN"`.
  **Không đổi** `VoucherService.SaveAsync` (method khác dùng chung Coupon/Voucher qua
  `VoucherSaveRequest`) vì hiện không có caller nào trong `POS.Web`/`POS.Api` — ngoài phạm vi task.

**Pattern (không mới, tái khẳng định):** default field bắt buộc trước khi lưu DB nên đặt tối
thiểu ở 2 lớp — UI (hardcode/không cho user sửa) + Service/Repository (defensive-assign) — không
phụ thuộc 1 lớp duy nhất.

**Lưu ý cho session sau:** nếu thấy `VoucherService.SaveAsync`/`VoucherSaveRequest` cần dùng thật
(hiện orphaned), phải review lại default `ArticleType` cho nó — có thể cần cho user chọn giữa
`ZCPN`/`ZVCN` tùy ngữ cảnh thay vì hardcode 1 giá trị. Verify: `dotnet build` (POS.Web +
POS.Application) 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25 Passed.

---

## [2026-07-06] Gap Analysis OffersPage (Danh mục khuyến mãi) + Modal Xem chi tiết + Deactive

**Layer:** POS.Web, POS.Application, POS.Infrastructure, POS.Common
**Loại:** Gap Analysis + Feature (port thiếu) + Feature mới (Deactive)

**Bối cảnh:** đối chiếu `OffersPage.razor` (`/promotion/offers`) với legacy
`PromotionController.PromotionList` (`src/legacy/VCM.BLUEPOS`) — phát hiện port thiếu 3 gap, xử lý
trong task này (KHÔNG động tới `CheckPromotionList`/"Tra cứu khuyến mãi" — hoãn sang giai đoạn sau
theo quyết định người dùng: khi làm sẽ chỉ port nhánh SERVER, bỏ nhánh "Nguồn = POS" kết nối SQL
trực tiếp máy POS bằng credential hard-code + raw SQL string-replace — rủi ro SQL injection).

**Thay đổi (port thiếu từ legacy):**
- `OffersPage.razor`: `BuildXlsx` thêm 2 cột Voucher (`VoucherFromDate`/`VoucherToDate`, DTO đã
  có field sẵn) + thêm cột "Hình thức bán" (`SalesTypeName`) vào lưới chính — dữ liệu có sẵn,
  không đổi DTO/Service/Repository.
- **Modal "Xem chi tiết" 6 tab** (gap lớn nhất — trước đó icon chỉ trang trí, không có `OnClick`):
  - `OfferHeaderDto.cs`: 6 DTO mới — `OfferHeaderDetailDto` (~68 field khớp `dbo.OfferHeader`),
    `OfferBuyDetailLineDto`, `OfferGetDetailLineDto`, `OfferBenefitLineDto`,
    `OfferSiteLineDetailDto`, `OfferPriorityLineDto`.
  - `IPromotionRepository`/`PromotionRepository`: 6 method mới — SQL Dapper trực tiếp trên
    `dbo.OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite/OfferPriority` (KHÔNG qua SP như
    lưới chính, vì legacy cũng dùng EF LINQ trực tiếp cho phần detail — đã tra đúng bảng/cột
    trong `docs/architecture/centralMD-schema.md` trước khi viết).
  - `IPromotionService`/`PromotionService`: 6 method thin-wrapper; riêng `GetOfferSiteDetailAsync`
    map thêm `StyleProfileName` (VM→WinMart, VMP→WinMart+, FS→FlagShip, KS→Kiosk — hardcode switch,
    xác nhận KHÔNG có bảng danh mục backing trong DB).
  - File mới `Dialogs/OfferDetailDialog.razor` — `MudDialog`+`MudTabs`, lazy-load theo tab active
    (cải tiến so với legacy vốn load lại cả 6 bảng mỗi lần đổi tab), export Excel riêng cho tab
    Buy/Get/Site.

**Thay đổi (feature MỚI, không có ở legacy — theo yêu cầu bổ sung):**
- Nút "Deactive" 1 offer LIVE trên `OffersPage.razor`. **Phát hiện & sửa mâu thuẫn quan trọng**:
  yêu cầu gốc mô tả set `Status=0` để deactive, nhưng bằng chứng code/doc (SP gốc
  `Setup_Promotion_Insert`, SP `GetPromotionOfferHeaderList`, `LOGIC_APPROVE_CTKM.md`) đều xác
  nhận `Status=0`=Active, `Status=2`=Deactivated — đã research + xác nhận lại với người dùng
  trước khi implement (tránh bug ngược nghĩa: set Active thay vì tắt).
- SP mới `docs/sql/OfferHeader_Deactivate.sql` (`usp_OfferHeader_Deactivate`) — set `Status=2` +
  `Counter=MAX(Counter)+1` atomic trong 1 transaction (`UPDLOCK, HOLDLOCK` tránh race-condition;
  `Counter` bắt buộc tăng để trigger delta-sync xuống ~5.000 máy POS). **Chưa chạy trên DB thật**
  — cần chạy tay trên `RPOSMasterData` (đã ghi vào `docs/ROLLOUT.md` §D8).
  `DeactivateOfferAsync` thêm vào `IPromotionRepository`/`PromotionRepository` +
  `IPromotionService`/`PromotionService`. UI dùng `MudMessageBox @ref` confirm chuẩn dự án
  (Outlined/Error vì là hành động phá hủy/không hoàn tác).
- Cập nhật lại invariant "Bất khả nghịch" trong `docs/web/logic/LOGIC_APPROVE_CTKM.md` (nay có 1
  ngoại lệ: Deactive).
- Đổi filter mặc định khi vào trang từ "Tất cả" sang "Có hiệu lực" (`_filter.Status = "0"`, cả lúc
  init và lúc bấm "Xóa" bộ lọc).

**Pattern mới:**
- "Modal chi tiết nhiều tab — lazy-load theo tab active (MudTabs mặc định)" →
  `.claude/skills/web/SKILLS.md`.
- "SP đổi Status trên bảng có cột `Counter` đồng bộ POS — atomic `UPDLOCK,HOLDLOCK`" →
  `.claude/skills/database/SKILLS.md`.

**Lưu ý cho session sau:** SQL của 6 query detail + SP Deactive **chưa verify trên
`RPOSMasterData` thật** (môi trường làm việc không có quyền truy cập DB) — bắt buộc QA thủ công
trên DEV (đối chiếu 1 `OfferNo` biết trước qua SSMS) trước khi coi tính năng là hoàn thành. Khi
làm tiếp `CheckPromotionList` ("Tra cứu khuyến mãi") — nhớ quyết định đã chốt: chỉ port nhánh
SERVER, không port nhánh "Nguồn = POS".

---

## [2026-07-06] Gap Analysis ProductList/ProductLock + Ảnh sản phẩm + Xem chi tiết SP

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Bug fix (gap analysis) + Feature mới + Pattern mới

**Bối cảnh:** người dùng yêu cầu Gap Analysis đối chiếu `ProductController.ProductList`/`ProductLock`
(legacy `src/legacy/VCM.BLUEPOS`) với `catalog/products`/`catalog/product-lock` (POS.Web) — xác nhận
migrate 2026-06-30 còn thiếu sót. Sau khi vá xong, người dùng yêu cầu thêm 2 tính năng mới trên cùng
2 trang: upload ảnh sản phẩm và xem chi tiết sản phẩm (kèm barcode + ảnh).

**Phần 1 — Gap Analysis fixes:**
- `ProductsPage.razor`: thêm 2 cột lưới bị thiếu so với legacy (Tên SP (VN), ĐVT Barcode — dữ liệu
  đã có sẵn trong `ProductListItemDto`, chỉ thiếu hiển thị); theo yêu cầu người dùng **không** thêm
  cột `ItemNo_PLG`/`ParentCode`/`Size`. Xác nhận Export Excel đã khớp đủ 11 cột legacy (không phải
  gap). Xóa nút Edit vô hiệu hóa vĩnh viễn + dọn code chết (`ExistingItem`/`IsEdit` trong
  `ProductDetailDialog`) — ProductList gốc không có Edit inline (đó là màn hình `UpdateArticle`
  riêng, ngoài phạm vi).
- `ProductDetailDialog.razor` + `ProductLockPage.razor`: thêm `IAuditLogger` — 2 trang này là ngoại
  lệ duy nhất trong menu Danh mục chưa ghi audit log (CREATE "Product", LOCK/UNLOCK "ProductLock").
- **Quyết định business quan trọng** (xác nhận với người dùng, ghi lại
  `docs/web/logic/product_lock_scope_decision.md`): **không port** tích hợp GrabFood API (tính năng
  thực chất là "Block sản phẩm" ngừng bán, không phải đồng bộ đa kênh realtime) và **không port**
  chế độ ghi trực tiếp CSDL máy POS qua IP terminal (Sync Master Data theo lịch đã đủ) — 2 khoảng
  trống lớn nhất phát hiện trong Gap Analysis, cố ý không làm chứ không phải bỏ sót.

**Phần 2 — Ảnh sản phẩm:** bảng mới `dbo.ProductImage` (`ItemNo`, `Uom`, `ImageBase64` — PK ghép
`(ItemNo, Uom)`, upsert) + SP `usp_ProductImage_Save` (`docs/sql/ProductImage_Save.sql`, **chưa chạy
trên DB thật**, xem D7 `docs/ROLLOUT.md`). DTO `ProductImageDto` + `ICentralMDRepository.SaveProductImageAsync`.
`ProductDetailDialog.razor`: `MudFileUpload` chọn JPG/PNG ≤2MB → đọc base64 → preview `MudImage`
ngay trong dialog trước khi Lưu; lưu ảnh sau khi tạo sản phẩm thành công, lỗi lưu ảnh không rollback
sản phẩm đã tạo (chỉ Snackbar cảnh báo); audit log riêng "ProductImage" chỉ ghi cờ `HasImage`
(không ghi base64 vào `DashboardAuditLog`).

**Phần 3 — Xem chi tiết sản phẩm:** cột Action + nút "Xem" trên `ProductsPage.razor` → dialog mới
`ProductViewDialog.razor` (read-only): field giống `ProductDetailDialog` + danh sách Barcode
(`MudSimpleTable`) + ảnh nếu có. DTO `ProductDetailDto` + `ICentralMDRepository.GetProductDetailAsync`
(đọc `dbo.Item`+`dbo.Barcodes`+`dbo.ProductImage`, trả null nếu không tồn tại). Vì không lưu MIME
type lúc upload, dialog suy đoán PNG/JPEG từ magic-byte prefix base64 (`iVBORw0KGgo` → PNG, còn lại
→ JPEG) khi hiển thị `data:` URI.

**Pattern mới:** "Upload ảnh → base64 + preview trong dialog" — đã thêm vào
`.claude/skills/web/SKILLS.md` (không dùng `varbinary`, không lưu cột MIME riêng, lưu ảnh là thao
tác phụ tách khỏi transaction chính).

**Lưu ý cho session sau:** SP `usp_ProductImage_Save` **chưa chạy trên DB thật** — chức năng ảnh sẽ
lỗi (không crash, chỉ cảnh báo) cho tới khi chạy `docs/sql/ProductImage_Save.sql` trên
`RPOSMasterData`. Chưa test UI thật trên browser (chỉ verify build + `dotnet test
tests/POS.ContractTests` 25/25).

---

## [2026-07-06] FIX: "Duyệt CTKM" publish dữ liệu nháp cũ khi chưa Lưu tạm lại

**Layer:** POS.Web
**Loại:** Bug fix (nghiêm trọng — publish sai dữ liệu lên máy POS)

**Bối cảnh:** người dùng test và tự phát hiện: bấm "Duyệt" mà không bấm "Lưu tạm" lại sau khi
sửa Buy/Get/Site → dữ liệu mới không được publish. Điều tra xác nhận (không đoán, có bằng chứng
code + doc cụ thể, xem lịch sử điều tra trong session): nút "Duyệt CTKM" trong editor
(`PromotionSetupPage.razor`) chỉ điều kiện hiện theo `_header.No` khác rỗng (đã Lưu tạm 1 lần
BẤT KỲ LÚC NÀO trong quá khứ) — không kiểm tra dữ liệu hiện tại đã Lưu hay chưa.
`ApproveAsync`/`usp_SetupPromotion_Approve` hoàn toàn không nhận Buy/Get/Site — chỉ publish lại
đúng dữ liệu **đã có sẵn** trong bảng nháp `SetupPromotionBUY/GET/SITE` từ lần Lưu tạm gần nhất.

**Thay đổi:** `PromotionSetupPage.razor`:
- `SaveAsync()` đổi trả `Task<bool>` (thêm tham số `showSuccessSnackbar = true`, giữ hành vi cũ
  cho nút "Lưu tạm" độc lập).
- Tách `ApproveAsync(bbynr)` cũ thành `ApproveCoreAsync(bbynr)` (logic publish thật, dùng chung).
- Thêm `ApproveFromEditorAsync()` — dùng riêng cho nút "Duyệt CTKM" trong editor: LUÔN
  `SaveAsync(false)` trước, chỉ gọi `ApproveCoreAsync` nếu Lưu thành công.
- Nút Duyệt nhanh ở màn danh sách (`ApproveAsync(context.No)`) **giữ nguyên** — không Lưu tạm
  (vì không có state Buy/Get/Site của đúng CTKM đó trong bộ nhớ trang; tự Lưu tạm ở đây sẽ ghi đè
  dữ liệu thật thành rỗng, tệ hơn bug cũ).
- Gỡ 1 dòng log chẩn đoán tạm (`System.Diagnostics.Debug.WriteLine`) đã thêm trong
  `PromotionRepository.SaveSetupAsync` ở phiên điều tra trước đó (không còn cần — đã loại trừ
  giả thuyết lỗi TVP/Dapper qua điều tra, root cause thật nằm ở tầng UI này).
- `docs/web/logic/LOGIC_APPROVE_CTKM.md` cập nhật sơ đồ + mục 1.1-1.5 phản ánh 2 luồng Duyệt
  khác nhau (editor tự Lưu tạm trước / danh sách không).

**Pattern mới:** "publish luôn kèm auto-save trước, không dùng cờ dirty" — khi 1 hành động B chỉ
hợp lệ nếu state đã được persist bởi hành động A, và A có nhiều điểm mutate rải rác (2-way binding
trên bảng động) khó theo dõi dirty đầy đủ, ưu tiên **luôn chạy A trước B** thay vì cờ dirty dễ sót.

**Lưu ý cho session sau:** đây là bug ảnh hưởng dữ liệu publish lên **5.000 máy POS thật** — nếu
gặp báo cáo tương tự ("Duyệt xong nhưng CTKM lên POS thiếu sản phẩm/cửa hàng"), kiểm tra ngay xem
người dùng có Lưu tạm lại sau khi sửa trước khi Duyệt không, trước khi nghi ngờ SP/Dapper. Verify:
`dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25 — chưa test UI thật (cần người
dùng tự test lại theo đúng kịch bản đã gặp bug).

---

## [2026-07-06] Topbar/AppBar breadcrumb + Typography pixel-perfect theo mockup `theme_html.html`

**Layer:** POS.Web
**Loại:** Pattern mới + Polish UI (2 task liên tiếp trong cùng session)

**Bối cảnh:** Theme v3 (2026-07-05) mới khớp màu/shadow/radius với mockup; font-size/weight/
letter-spacing từng thành phần và khu vực Topbar chưa đối chiếu lại. Đã audit toàn bộ CSS mockup
(`docs/web/theme/theme_html.html`, chỉ 1 `<style>` inline, font `'Segoe UI',system-ui,sans-serif`,
không Google Fonts) và đối chiếu với `PosTheme.cs`/`app.css`/`MainLayout.razor` hiện tại.

**Thay đổi — Typography:**
- `PosTheme.cs`: `Default.LineHeight` 1.45→1.5; `Button` thêm `FontSize="0.75rem"` + xóa
  `LetterSpacing="0.03em"` thừa; `Body1.FontSize` 0.75rem→0.78125rem (12.5px).
- `app.css`: sidebar L1 label weight 400→700/size 11px→10px/letter-spacing 0.8px→1px; sidebar L2
  size 13px→12.5px; thêm `.mud-table .mud-table-body .mud-table-cell` (12.5px, trước chỉ có
  header); thêm `.mud-input-label-inputcontrol` (11px/700/uppercase/ls 0.5px — field label trước
  đó dùng mặc định MudBlazor 16px/400); xóa override line-height mobile-only (dead code vì
  `Default.LineHeight` đã 1.5 mọi breakpoint); thêm 4 class `.pos-kpi-value`/`.pos-kpi-label`/
  `.pos-card-title`/`.pos-section-label` (mockup `.kpi-value/.kpi-label/.card-title/.section-label`).
- `RevenueByStorePage.razor`, `ShiftSummaryPage.razor`: áp `.pos-kpi-value`/`.pos-kpi-label` làm
  **mẫu chuẩn** trên KPI row (giữ nguyên `@code`) — **CHƯA rollout** ~80 file KPI/card-title khác
  dùng `Typo.h5/h6/body2/caption` tương tự (quyết định phạm vi có chủ đích, xem dưới).

**Thay đổi — Topbar/AppBar:**
- `PosTheme.cs`: thêm `LayoutProperties.AppbarHeight="50px"` (khớp mockup `.topbar{height:50px}`,
  đã verify property tồn tại thật qua grep `MudBlazor.dll`).
- `MainLayout.razor`: bỏ `Dense="true"` trên `MudAppBar` (tránh phải tính ngược hệ số 0.75 áp lên
  `AppbarHeight`); thay `MudText` tiêu đề tĩnh "RPOS Dashboard" bằng breadcrumb động — thêm
  `BreadcrumbMap` (43 route, copy đúng Title/text đã có trong `MudNavMenu`, không đặt tên mới) +
  `UpdateBreadcrumb()` gọi trong `OnInitialized` và `OnLocationChanged` (tái dùng lifecycle có
  sẵn). Route không có trong map → fallback về text tĩnh cũ.
- `app.css`: thêm `.pos-breadcrumb`/`.pos-breadcrumb strong` khớp mockup `.breadcrumb`.

**Phát hiện quan trọng (không đoán, đã verify bằng grep + đọc trực tiếp code)**: mockup
`.topbar-right` thực chất chứa nội dung của 1 app khác (nút lọc kỳ + CTA "Tờ trình mới" — app
ngân sách/phê duyệt), không có User Profile/Notification nào để map. Code `MainLayout.razor`
hiện tại cũng KHÔNG có logic User Profile/Notification trong AppBar — toàn bộ nằm ở
`pos-sidebar-footer`. Đã xác nhận với user và **quyết định không thêm** nội dung `.topbar-right`
của mockup vào app (không có ngữ cảnh tương ứng) — chỉ đồng bộ khung sườn (height/breadcrumb).

**Pattern mới:** "AppBar — Breadcrumb động" → đã thêm vào `.claude/skills/web/SKILLS.md`.
Đồng thời cập nhật `.claude/rules/mudblazor-flat-ui.md` mục 11 (chi tiết audit Typography) và
11.1 (checklist bắt buộc cho page mới — `.pos-kpi-value`/`.pos-kpi-label`/`.pos-card-title`/
`.pos-section-label`).

**Lưu ý cho session sau:** (1) `.pos-card-title`/`.pos-section-label` mới chỉ định nghĩa CSS,
CHƯA áp dụng vào page nào — cần rà soát riêng nếu muốn rollout. (2) `BreadcrumbMap` phải cập nhật
thủ công khi thêm route mới vào sidebar — thiếu route không crash, chỉ hiển thị fallback tĩnh.
(3) Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25 (cả 2 lần) — **chưa
chạy `dotnet run` để xác nhận trực quan** chiều cao AppBar 50px và breadcrumb hiển thị đúng trên
browser thật.

---

## [2026-07-06] PromotionSetupPage — modal "Cài đặt nhóm sản phẩm" cho dòng Buy/Get "Nhóm SP"

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature (hoàn thiện phần bị hoãn của task trước)

**Bối cảnh:** task trước (chuyển đổi form "Cài đặt CTKM" khớp 100% field legacy) đã hoãn modal
này lại vì khối lượng tương đương 1 CRUD feature riêng. Đọc trực tiếp
`_ViewSetupGroupItemBuy/Get.cshtml`, `_ViewDataBuyGroupItem/GetGroupItem.cshtml`,
`SetupPromotionController.SetupBuyGroupItem/LoadBuyGroupItem`, `SetupPromotionData.
SetupGroupBuyItem/LoadBuyGroup` — xác nhận bảng `dbo.SetupGroupItem` đã tồn tại thật trong
`src/legacy/Database/CentralMD.sql` (chưa có tài liệu, chưa có SP) và có khuôn mẫu 1:1 vừa port
xong ("Site Group") để tái dùng.

**Thay đổi:**
- `POS.Common/Dtos/Promotion/PromotionSetupDto.cs`: thêm `ItemGroupSaveRequest`,
  `ItemGroupListItemDto`, `ItemGroupItemDto`.
- `IPromotionRepository`/`PromotionRepository.cs`: thêm `SaveItemGroupAsync` (validate item tồn
  tại trong `dbo.Item` trước khi lưu, gọi SP), `GetItemGroupListAsync` (filter+paging),
  `GetItemGroupItemsAsync` (bung `ListItemNo` JOIN `dbo.Item`).
- `IPromotionService`/`PromotionService.cs`: thêm wrapper thin tương ứng.
- `docs/sql/SetupGroupItem_Save.sql` (mới): SP `usp_SetupGroupItem_Save` — mirror
  `usp_SetupGroupSite_Save`.
- `Dialogs/ItemGroupSetupDialog.razor` (mới): modal 2 sub-tab (tạo nhóm mới với autocomplete tìm
  sản phẩm + danh sách nhóm có filter/phân trang/xem chi tiết/chọn gắn vào dòng).
- `PromotionSetupPage.razor`: thay placeholder "(Cấu hình nhóm SP — bổ sung ở task sau)" (2 chỗ,
  bảng Buy + Get) bằng nút "Cấu hình nhóm" mở dialog, gán `GroupCode` trả về vào dòng.
- `docs/architecture/centralMD-schema.md`, `docs/CURRENT_STRUCTURE.md`, `docs/WEB_STATUS.md`: cập
  nhật theo thay đổi trên.

**Hạn chế legacy CHỦ Ý giữ nguyên (theo quyết định người dùng)**: cột `ListItemNo` chỉ lưu
`List<string>` ItemNo (không lưu UOM — bảng chọn sản phẩm trong dialog hiển thị ĐVT read-only,
không phải input); khi `GroupCode` đã tồn tại, SP chỉ update `GroupName`, không ghi đè lại danh
sách sản phẩm (đúng bug/limitation của `SetupGroupBuyItem` legacy).

**Khác legacy (theo quyết định người dùng)**: lưu DB ngay khi bấm "Lưu" trong dialog (SP upsert),
không qua `sessionStorage` như legacy — nhất quán với `SiteGroupSetupDialog` đã làm đợt trước.

**Pattern mới:** không có — mirror 1:1 pattern "Site Group" đã có.

**Lưu ý cho session sau:** SP `usp_SetupGroupItem_Save` **CHƯA chạy trên DB thật** — phải chạy tay
trên `RPOSMasterData` (DEV trước) trước khi test UI. Verify: `dotnet build` 0 lỗi,
`dotnet test tests/POS.ContractTests` 25/25 — chưa test UI thật trên browser vì phụ thuộc SP.
Trong lúc build, gặp lỗi khóa file DLL do process `POS.Web` đang chạy qua Visual Studio (PID
15960) — đã dừng process đó theo xác nhận của người dùng để build được; cần F5 lại trong Visual
Studio để tiếp tục debug.

---

## [2026-07-06] Đóng gói MudBlazor Theme Standard v3 thành Rule chuẩn — bổ sung mapping + CSS isolation

**Layer:** POS.Web (tài liệu, không đổi code)
**Loại:** Pattern mới (tài liệu hóa)

**Bối cảnh:** sau khi hoàn thành rollout theme v3 (sidebar navy, shadow thật, radius 2 cấp, Button
Filled/Outlined theo ngữ nghĩa — xem entry theme v3 phía dưới), người dùng muốn "đóng gói" kiến
thức này thành rule chuẩn để AI tự áp dụng nhất quán cho mọi component/page mới. Rà soát trước khi
viết phát hiện phần lớn nội dung yêu cầu **đã có sẵn** trong `CLAUDE.md §14` + `.claude/rules/
mudblazor-flat-ui.md` — quyết định KHÔNG tạo file rule mới trùng lặp (sẽ là nguồn sự thật thứ 5 cho
cùng chủ đề, vi phạm nguyên tắc "Cổng chặn trùng lặp" của chính dự án), mà bổ sung đúng 2 phần thật
sự còn thiếu vào file đã có vai trò tương ứng.

**Thay đổi:**
- `.claude/rules/mudblazor-flat-ui.md`: thêm mục 0 "Mapping HTML mockup → MudBlazor Component"
  (bảng tổng quát: `div.sidebar`→`MudDrawer`, `div.card`→`MudPaper Elevation=2`, `button.btn-
  primary`→`MudButton Filled/Primary`...) và mục 10 "CSS Isolation — khi nào dùng `.razor.css`"
  (mặc định `app.css` global; `.razor.css` chỉ khi style cục bộ hoàn toàn; ghi đè component con
  MudBlazor tự render → ưu tiên `app.css` vì cần `::deep` mới xuyên qua CSS isolation).
- `.claude/skills/web/SKILLS.md`: thêm "Pattern: Polish/tạo UI theo mockup HTML — quy trình chuẩn"
  ngay sau bảng "MudBlazor — component mapping" hiện có, trỏ sang mục 0 mới ở rules file (không
  copy lại bảng — giữ 1 nguồn sự thật).
- `CLAUDE.md §14`: thêm 1 dòng router tường minh ở đầu mục, yêu cầu đọc `.claude/rules/
  mudblazor-flat-ui.md` trước khi code UI bất kỳ.

**Lưu ý cho session sau:** Trước khi tạo file rule/skill mới cho 1 chủ đề UI, LUÔN kiểm tra
`CLAUDE.md §13/14/15` + `.claude/rules/mudblazor-flat-ui.md` + `.claude/skills/web/SKILLS.md` xem
đã có chưa — dự án đã có sẵn phân lớp tài liệu rõ ràng cho MudBlazor UI, tạo file mới cạnh đó gần
như luôn là trùng lặp.

---

## [2026-07-05] PromotionSetupPage — chuyển đổi form "Cài đặt CTKM" khớp 100% field legacy

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature (bổ sung field/tính năng còn thiếu so với legacy)

**Bối cảnh:** trang `/promotion/setup` đã migrate từ `SetupPromotionController.SetupMain`
(`src/legacy/VCM.BLUEPOS`) nhưng bị đơn giản hoá, thiếu nhiều field. Đọc trực tiếp toàn bộ
`.cshtml` gốc (`SetupMain`, `_DetailOfferHeader/Buy/Get/Site`, `_SetupAdvance`, `_ViewSetupCHST`,
`_AddStoreGroup_ViewDataGroupSite`, `_ViewSetupGroupItemBuy`) để lấy chính xác từng ô nhập liệu,
không chỉ dựa vào ảnh mockup.

**Thay đổi:**
- `POS.Common/Dtos/Promotion/PromotionSetupDto.cs`: thêm field `FromTime/ToTime/Mon..Sun`,
  `MinValue`, `CheckTotalDiscount/TotalDiscountType/TotalDiscountValue`, `AllowUseAfterDay/
  AllowUseAfterTime` vào `PromotionSetupHeaderDto`; thêm `OfferTypeOptionDto`,
  `SiteGroupSaveRequest`, `SiteGroupListItemDto`, `SiteGroupStoreItemDto`.
- `IPromotionRepository`/`PromotionRepository.cs`: `GetOfferTypeOptionsAsync` đổi trả
  `List<OfferTypeOptionDto>` (kèm cờ IsTotalBill/IsSetupBuy/IsSetupGet/IsVoucher/IsGift/
  UserGuide); `GetSetupDetailAsync`/`SaveSetupAsync` thêm field mới; thêm
  `SaveSiteGroupAsync`/`GetSiteGroupListAsync`/`GetSiteGroupStoresAsync` (CRUD nhóm cửa hàng).
- `IPromotionService`/`PromotionService.cs`: thêm wrapper thin tương ứng.
- `docs/sql/SetupPromotion_Save.sql`: sửa `usp_SaveSetupCTKMAll` thêm tham số (cột DB đã có sẵn,
  trước đây SP hard-code rỗng/bỏ qua — KHÔNG ALTER TABLE).
- `docs/sql/SetupGroupSite_Save.sql` (mới): SP `usp_SetupGroupSite_Save` upsert `SetupGroupSite`.
- `PromotionSetupPage.razor`: tái cấu trúc — khối header (Tên/Loại CTKM/Hình thức bán/Trạng thái/
  Voucher/Từ-Đến ngày) chuyển ra NGOÀI 4 tab; tab "Thông tin chung" → bảng tóm tắt lịch (giờ +
  Mon-Sun); toolbar Buy/Get thêm bulk-add "Số lượng dòng" + cột "Điều kiện áp dụng" (ScaleType);
  Buy thêm "Giá trị tổng tiền tối thiểu" (enable theo `IsTotalBill` của OfferType, không phải
  checkbox tự do — khớp hành vi khoá cứng của legacy); Get thêm "Giảm giá tổng bill" (loại trừ với
  bảng dòng, confirm xoá); Site thêm nút "Chọn nhóm CH/ST"; Advanced thêm voucher-delay + đổi
  `MemberCode` sang `MudAutocomplete` (gõ tự do + gợi ý).
- `Dialogs/SiteGroupSetupDialog.razor` (mới): modal 2 sub-tab (tạo nhóm mới + danh sách nhóm có
  filter/phân trang/xem chi tiết store/chọn gắn vào CTKM).
- `docs/architecture/centralMD-schema.md`, `docs/CURRENT_STRUCTURE.md`, `docs/WEB_STATUS.md`: cập
  nhật theo thay đổi trên.

**Pattern mới:** không có pattern mới ngoài phạm vi đã có (dialog/SP/cache theo chuẩn dự án).

**Ngoài phạm vi (hoãn task riêng sau):** modal "CÀI ĐẶT NHÓM SẢN PHẨM" (định nghĩa item cụ thể
trong 1 group khi dòng Buy/Get chọn "Theo nhóm") — khối lượng tương đương 1 CRUD feature riêng,
cần khảo sát thêm bảng DB.

**Lưu ý cho session sau:** 2 script SQL (`SetupPromotion_Save.sql` sửa, `SetupGroupSite_Save.sql`
mới) **CHƯA chạy trên DB thật** — phải chạy tay trên `RPOSMasterData` (DEV trước) trước khi test
UI, nếu không các field mới (giờ/ngày trong tuần, MinValue, TotalDiscount*, voucher-delay, nhóm
cửa hàng) sẽ không lưu được (SP cũ không nhận tham số). Verify: `dotnet build` 0 lỗi,
`dotnet test tests/POS.ContractTests` 25/25 — chưa test UI thật trên browser vì phụ thuộc SP.

---

## [2026-07-05] MudBlazor Theme Standard v3 — chuyển toàn app sang ngôn ngữ thiết kế mockup + font audit

**Layer:** POS.Web
**Loại:** Pattern mới (theme) + Refactor diện rộng (59 file) + Bug fix (font-family không áp dụng)

**Bối cảnh:** người dùng cung cấp mockup HTML/CSS `docs/web/theme/theme_html.html` (demo "BudgetOS",
khác nghiệp vụ) làm nguồn tham chiếu ngôn ngữ thiết kế mới — sidebar navy đậm, card có shadow thật,
radius 2 cấp, Button Filled cho CTA. Chốt phạm vi qua AskUserQuestion: chỉ lấy design token/layout,
KHÔNG port UI nghiệp vụ ngân sách của mockup, không tạo page mới.

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: đổi toàn bộ `PaletteLight` (Primary `#2660A4`, Secondary
  `#4A6070`, Tertiary `#6040A8`, Success/Error/Warning/Info theo mockup, `DrawerBackground
  #0D1B2A`), `DefaultBorderRadius` 16px→12px, `Shadows.Elevation[2-5]` từ `"none"` sang shadow
  thật (`0 2px 8px rgba(0,0,0,.08)` / `0 4px 20px rgba(0,0,0,.12)`). **Bổ sung font audit**: set
  `FontFamily=["Segoe UI","system-ui","sans-serif"]` tường minh trên TỪNG Typography variant
  (H1-H6, Subtitle1/2, Body1/2, Caption, Overline, Button) — không chỉ `Default`; `Default.FontSize`
  14px→13px, `Body1` 12px.
- `src/POS.Web/Components/Layout/MainLayout.razor`: sidebar 3 cấp (L1 UPPERCASE không icon, L2
  icon Material riêng từng nhóm, L3 `ChevronRight` đồng nhất), sidebar-footer (avatar initials +
  tên/role/logout, dời khỏi `MudAppBar`), brand block text-only.
- `src/POS.Web/wwwroot/app.css`: token `--pos-*` đổi theo palette mới, sidebar navy CSS (dùng đúng
  class MudBlazor 9.5.0 thật: `.mud-navmenu`, `.mud-nav-group > .mud-nav-link`), table header
  uppercase/muted, filter panel trắng+border, radius control 8px, `.pos-table` font-size 14px→13px.
- `src/POS.Web/Components/App.razor`: gỡ Google Fonts Roboto `<link>` (không cần, dùng system font).
- **59 file Razor** (5 cụm menu: Danh mục/Cửa hàng/Khuyến mãi/Vận hành/Quản trị): đổi quy ước
  `MudButton` — CTA (Lưu/Thêm mới/Tìm) → `Filled`/`Primary`; hành động chốt luồng (Duyệt) →
  `Filled`/`Success`; phá hủy (Xóa) → `Outlined`/`Error`; trung tính (Hủy/Đóng) → `Outlined` không
  màu. `MudMessageBox @ref` YesButton chọn theo bản chất hành động Yes.

**Pattern mới:**
1. **MudBlazor `Icon=` nhận SVG path, không phải text/ligature** — truyền emoji vào `Icon=` của
   `MudNavLink`/`MudNavGroup`/`MudIcon` khiến icon biến mất im lặng (không lỗi). Đã thử và rollback
   trong session này. Đã ghi vào `CLAUDE.md §14`, `.claude/rules/mudblazor-flat-ui.md`.
2. **Typography per-variant FontFamily không cascade từ `Default`** — MudBlazor sinh CSS variable
   riêng cho mỗi variant (`--mud-typography-h5-family`, `--mud-typography-body1-family`...). Chỉ
   set `Default.FontFamily` gần như không có tác dụng vì hầu hết text hiển thị dùng H5/H6/Body1/
   Body2/Caption/Button. Đã cập nhật `.claude/skills/web/SKILLS.md` (mục "KHÔNG làm").

**Lưu ý cho session sau:** Khi đổi bất kỳ giá trị nào trong `Typography` của `PosTheme.cs` (font
size/family/weight), phải set trên TỪNG variant cần áp dụng — không tin tưởng `Default` cascade
xuống. Còn ~15 page report có inline `font-size` bespoke cho KPI-number/badge (không phải font-
family) — cố ý chưa đổi vì là tuning riêng từng page, không phải lỗi theme. Chưa xác nhận trực
quan trên browser thật trong session này (không có công cụ browser) — cần người dùng tự chạy app.

---

## [2026-07-06] Danh mục Bảng giá (9.1) — cột Hình thức/Trạng thái, filter combobox, fix bug Sửa/Xóa sai dòng

**Layer:** POS.Web, POS.Common, POS.Infrastructure + SQL
**Loại:** Feature + Bug fix

**Thay đổi:**
- `PricesPage.razor`: ẩn cột Site; đổi label "Vùng giá"→"Nhóm giá"; thêm cột **"Hình thức"** (`SaleTypeName`, trước cột "Nhóm giá") + cột **"Trạng thái"** (Hiệu lực/Chưa hiệu lực/Hết hiệu lực — `MudChip` màu, tính client-side từ `StartingDateStr`/`EndingDateStr`); ngày `01/01/9999` hiển thị "Vô thời hạn"; filter Barcode/SalesCode (text tự do) → `MudSelect` "Hình thức bán hàng"/"Nhóm giá" (reuse `PriceService.GetSetupLookupAsync`, không tạo lookup mới); mặc định "Còn hiệu lực" **bỏ check**; format nghìn khi nhập Giá bán (`FormatThousands`, khớp pattern `PriceSetupPage.razor`).
- **FIX bug Sửa/Xóa giá sai dòng**: SP `GetSalesPriceList`/`_Export` (DBA sửa 2026-07-05→06) đổi trả cột `SalesCode` = **tên** nhóm giá (`PriceGroupName`) thay vì mã — code cũ dùng thẳng field này làm khoá gửi `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (đang lọc theo **mã**) → luôn báo "Không tìm thấy dữ liệu" khi Code≠Name. Thêm cột mã gốc `SalesGroupCode`/`SalesTypeCode` vào SP (script `docs/sql/GetSalesPriceList_AddSaleType.sql` → `_AddSalesTypeCode.sql`), map vào `PriceListItemDto`, sửa `TryBuildKey` dùng field mã thay field hiển thị.
- **FIX bug thứ 2 phát hiện khi review**: 1 item/uom/nhóm giá/ngày hiệu lực có thể có nhiều dòng khác nhau theo `SalesType` (hình thức bán hàng) — composite PK cũ (ItemNo, SalesCode, StartingDate, UOM) không đủ định vị. Thêm field `PriceRowKey.SalesType` + tham số `@SalesType` vào `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (script `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`).
- `GetSalesPriceList_Export`: fix thêm 1 bug review-time không liên quan yêu cầu ban đầu — proc tham chiếu sai tên temp table (`#SalsePriceExportTemp` không tồn tại, bảng thật là `#TempSalesPrice`) → nút Xuất Excel sẽ crash runtime nếu không sửa.
- **Đính chính schema**: `centralMD-schema.md` từng ghi "SalesPrice KHÔNG có Id/IsActive" — SAI. Source thật của `GetSalesPriceList` (`AND S.IsActive = 1`, bắt buộc bất kể `@isCheck`) + bản `usp_SalesPrice_SoftDelete` mới (set `IsActive=0` khi xóa mềm) xác nhận bảng CÓ 2 cột này. Trước bản vá này, xóa mềm chỉ set `EndingDate` năm 7777 mà không set `IsActive=0` → dòng đã xóa có thể vẫn hiển thị khi bỏ check "Còn hiệu lực" (SP luôn yêu cầu `IsActive=1`, không điều kiện theo `@isCheck`).
- `PriceListDto.cs`: `PriceListItemDto` +`SalesGroupCode`+`SalesTypeCode`; `PriceListFilter` bỏ `Barcode`, `SalesCode`→`SaleType`+`SalesGroup` (mặc định `"ALL"`). `PriceSetupDto.cs`: `PriceRowKey` +`SalesType`. `PriceRepository.cs`: `GetListAsync`/`GetExportListAsync` đổi tham số EXEC theo SP mới + `NormalizeSalesGroup` (dịch UI sentinel `"ALL"`→`""`); `UpdatePriceAsync`/`SoftDeletePriceAsync` truyền thêm `@SalesType`.

**Pattern mới:** SP đổi 1 cột từ mã sang tên hiển thị → luôn thêm cột mã gốc riêng cho composite key (không tái dùng field hiển thị để build khoá Update/Delete). Đã cập nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** phải chạy đủ 3 script SQL theo thứ tự trước khi test: `GetSalesPriceList_AddSaleType.sql` → `GetSalesPriceList_AddSalesTypeCode.sql` → `SalesPrice_EditDelete_AddSalesType.sql`. Khi 1 SP legacy/tự-quản lý đổi ý nghĩa 1 cột đang dùng làm khoá composite ở nơi khác, luôn rà lại MỌI nơi consume cột đó (không chỉ nơi hiển thị) trước khi merge.

---

## [2026-07-05] Cài đặt giá / Danh mục Bảng giá — Sửa/Xóa giá 9.1, fix lưu SP, format UI, menu

**Layer:** POS.Web, POS.Application, POS.Infrastructure, POS.Common + SQL
**Loại:** Feature + Bug fix

**Thay đổi:**
- `docs/sql/SalesPrice_EditDelete.sql` (MỚI): `usp_SalesPrice_UpdatePrice` (sửa UnitPrice in-place theo composite PK + bump Counter) và `usp_SalesPrice_SoftDelete` (soft-delete = EndingDate năm 7777 + Counter). Bảng `SalesPrice` không có cột `Id` → định vị dòng bằng composite PK `(ItemNo,SalesCode,StartingDate,UOM)`.
- `PricesPage.razor` (9.1): thêm cột Thao tác — sửa giá inline + xóa (confirm) + `IAuditLogger`.
- `PriceSetupPage.razor` (9.3): thêm route thứ 2 `/catalog/price-declare`; đổi tiêu đề "Cài đặt giá"; format ô Giá bán khi nhập (thousand sep `,`, căn phải); `.pos-price-grid table{min-width:1040px}` để lưới cuộn ngang, ô ngày không bị bóp.
- `MainLayout.razor`: menu "Giá bán"→**"Danh mục Bảng giá"** (`/catalog/prices`); thêm "Cài đặt giá" (`/catalog/price-declare`); **ẩn** "Setup giá (Bulk Import)" (`/catalog/price-setup`, route còn).
- `PriceSetupDto.cs`: +`PriceRowKey`. `IPriceService/PriceService` +`UpdatePriceAsync`/`DeletePriceAsync`. `IPriceRepository/PriceRepository` +`UpdatePriceAsync`/`SoftDeletePriceAsync`.
- `docs/sql/SetupSalePrice_Save.sql` (FIX): (1) trả kết quả qua **OUTPUT param** `@Ok/@Message` thay vì result set — vì nhánh update `EXEC Setup_SalePrice_Get_ALL` tự SELECT Interface_Errors (+ROLLBACK bên trong → không hứng được bằng INSERT...EXEC), trước đây Dapper `QueryFirstOrDefault` đọc nhầm set rỗng → báo "thất bại" giả khi Pkey đã tồn tại; (2) chuẩn hóa sentinel "vô thời hạn" `9999-12-31 → 9999-01-01` khi INSERT (khớp legacy) để lần cập nhật sau không sinh khoảng "đuôi" thừa. `PriceRepository.SaveAsync` đổi sang `ExecuteAsync` + đọc output param.
- `FileLogHelper.WriteExpLogs`: ghi `ex.ToString()` (full stack + inner) thay `JsonConvert.SerializeObject(ex)` (dễ ném lỗi → file rỗng).

**Pattern mới:** SP ủy quyền SP-legacy-trả-result-set → dùng OUTPUT param (không result set); + format số khi nhập bằng dấu `,` để khớp `ParsePrice`. Đã cập nhật `.claude/skills/api/SKILLS.md`, `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:** `dbo.SalesPrice` schema thật trên DB CÓ cột `IsActive` (khác `centralMD-schema.md` ghi 15 cột); sentinel vô thời hạn lưu là `9999-01-01`, đã xóa là năm `7777`. Chạy `SalesPrice_EditDelete.sql` + `SetupSalePrice_Save.sql` trên DB trước khi test. Chạy app bằng `dotnet run` (Development) — chạy `.exe` trực tiếp = Production (DB `127.0.0.1,14333`, log `/app/logs`).

---

## [2026-07-05] Middleware log request/response toàn cục cho POS.Api (bật/tắt qua config)

**Layer:** POS.Api, POS.Infrastructure
**Loại:** Feature + Pattern mới

**Bối cảnh:** nhiều API port từ code cũ (VCM.POSBLUE.API, source gốc không còn) chưa rõ POS gửi
request lên như thế nào / API trả response ra sao — khó chẩn đoán khi có lỗi (rút ra từ vụ debug
`UploadFileLogJob`). Trước đó chỉ 3/9 controller tự gọi `LogRequest` thủ công, không nhất quán,
1 kiểu ghi file `.txt` đồng bộ (`FileLogHelper.WriteLogs`) tốn I/O nếu áp cho toàn bộ endpoint.

**Thay đổi:**
- `src/POS.Api/Middleware/RequestResponseLoggingMiddleware.cs` (mới): log request/response cho
  MỌI API qua `IKibanaService.LogRequest`/`LogResponse` (tái dùng Serilog pipeline có sẵn, không
  thêm hạ tầng mới). Dùng `CappedCapturingStream` pass-through — KHÔNG buffer toàn bộ response vào
  `MemoryStream` (an toàn với endpoint stream file lớn như `DowloadFileStream`). Bỏ qua capture nội
  dung multipart upload và response binary, chỉ log metadata.
- `src/POS.Api/Middleware/RequestLoggingOptions.cs` (mới): `Enabled`/`MaxBodyBytes`/`ExcludePaths`.
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: thêm cờ `RequestLogging:PersistToFile`
  — quyết định File sink (`pos-*.log`) có nhận log Request/Response hay chỉ Elasticsearch (lọc
  đúng theo giá trị property `"HttpContext"="Request"/"Response"`, không ảnh hưởng Exception/Info).
- `src/POS.Api/Program.cs`: đăng ký middleware **ngoài cùng pipeline** (trước
  `UsePosExceptionHandling`) để bao trùm cả response lỗi chuẩn hoá.
- Dọn 19 điểm gọi `LogRequest` thủ công cũ (method wrapper + call site) ở `SyncDataPosController.cs`,
  `PaymentController.cs`, `LoyaltyController.cs` — middleware toàn cục thay thế, tránh log trùng.
- `appsettings.json`/`.UAT.json`/`.Production.json` (POS.Api): thêm section `RequestLogging`
  (`Enabled: false` mặc định — opt-in khi cần debug 1 đợt; `PersistToFile: true` vì **chưa cài
  Elasticsearch**, cần bản ghi local trên đĩa server để tra cứu).
- `appsettings.Development.json` (POS.Api): override `RequestLogging:Enabled: true` — tiện bật sẵn
  lúc dev/debug, không cần set biến môi trường thủ công (bug follow-up: lúc verify ban đầu chỉ set
  qua biến môi trường tạm, không lưu vào config nào → lần chạy sau tưởng middleware không hoạt
  động, thực ra do cấu hình quay về mặc định `false`).

**Pattern mới:** Middleware log request/response toàn cục — capped pass-through stream → đã cập
nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** `RequestLogging:Enabled` mặc định `false` ở mọi môi trường (opt-in) —
**riêng Development** đã override `true` trong `appsettings.Development.json`. Nếu thấy log
Request/Response không xuất hiện, kiểm tra `RequestLogging:Enabled` hiệu lực trước khi nghi ngờ
code. Khi Elasticsearch được cài đặt thật và ổn định, cân nhắc đổi `PersistToFile: false` (UAT/PROD)
để giảm I/O đĩa cho log Request/Response (xem `docs/ROLLOUT.md` §O4).

---

## [2026-07-05] Thay WinSCP bằng FluentFTP cho UploadFileLogJob (WinSCP không chạy được trên Linux)

**Layer:** POS.Infrastructure, POS.Application
**Loại:** Bug fix

**Root cause:** `WinScpFileTransfer` dùng thư viện `WinSCP` (.NET assembly) — vốn hoạt động bằng
cách spawn tiến trình `winscp.exe` (Windows PE binary). POS.Api chạy trên container Linux
(`mcr.microsoft.com/dotnet/aspnet:10.0`, xem `Dockerfile`) → `winscp.exe` **không thể** thực thi,
nên `session.Open(...)` luôn throw. Catch-block cố log lại exception bằng
`JsonConvert.SerializeObject(ex)` (Newtonsoft reflection-serialize) lại tự ném
`JsonSerializationException` khác vì exception WinSCP tham chiếu `Session` đã bị `using` dispose
trước khi vào catch (`Session.DebugLogPath` getter throw `ObjectDisposedException`) — che giấu hoàn
toàn lý do thất bại thật.

**Thay đổi:**
- `src/POS.Infrastructure/POS.Infrastructure.csproj`: gỡ package `WinSCP`, thêm `FluentFTP` (managed
  .NET thuần, không cần binary ngoài, chạy được trên Linux/Ubuntu).
- `src/POS.Infrastructure/Files/WinScpFileTransfer.cs` → xoá, thay `FtpFileTransfer.cs` (dùng
  `FluentFTP.FtpClient`, `DataConnectionType = PASV` — cần thiết vì API chạy sau NAT/Docker). Sửa
  luôn cách log exception: `ex.ToString()` thay `JsonConvert.SerializeObject(ex)` (áp dụng cả ở
  `SyncDataPosService.UploadFileLogToFtpAsync`) — tránh Newtonsoft đệ quy vào object nội bộ gây lỗi
  thứ cấp tương tự trong tương lai.
- `src/POS.Infrastructure/DependencyInjection.cs`: `IFtpFileTransfer` → `FtpFileTransfer`.
- Dọn config `AppSettings:WinScpExecutablePath` không còn dùng khỏi 4 file `appsettings*.json`
  (POS.Api + POS.Web) + `docs/CURRENT_STRUCTURE.md`.

**Pattern mới (nếu có):** Không — thay thư viện, giữ nguyên interface `IFtpFileTransfer`.

**Lưu ý cho session sau:** Tính năng đẩy log job qua FTP trung tâm (`UploadFileFTP: "YES"`) có thể
đã fail âm thầm từ lâu (từ khi hạ tầng chuyển sang Docker/Linux) do nguyên nhân trên — sau khi đổi
sang FluentFTP, còn cần đội hạ tầng xác nhận container mở được outbound tới FTP server (port điều
khiển + dải port PASV) và `FTPSERVER/FTPUSERNAME/FTPPASSWORD` (bảng Data Setup) còn đúng.

---

## [2026-07-05] BusinessDayPage — fix crash tìm kiếm + phân quyền force EOD + auto-load

**Layer:** POS.Web, POS.Application, POS.Infrastructure
**Loại:** Bug fix + Feature

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs`: FIX crash `ArgumentException:
  duplicate key ""` khi tìm kiếm — SP `GetSalesEODConfirm` trả cột theo tên legacy (`TerminalID`,
  `AmountTotal`, `CashMoney`, `LastOrderTime`, `CountCustomer`, `CountOrderNo`…) không khớp property
  `PosDayStagingDto` nên Dapper để `PosTerminal = ""` cho mọi dòng → `ToDictionary` trùng key rỗng.
  Thêm `private sealed class SalesEodConfirmRow` (nullable) khớp tên cột SP + `commandType:
  CommandType.StoredProcedure` (trước đó thiếu → SP chạy dạng text) rồi project tường minh sang DTO.
  Xóa khối `const string sql` (query CTE dead-code không được gọi).
- `src/POS.Application/Features/StoreActivities/IBusinessDayService.cs` + `BusinessDayService.cs`:
  thêm param `bool allowForceConfirm = false` cho `ConfirmBusinessDayAsync` (guard "còn POS chưa đóng
  ngày" chỉ chặn khi `!allowForceConfirm`); thêm method `GetCurrentBusinessDateAsync(storeNo)`
  delegate sang `ICentralSaleRepository.GetBusinessDateAsync(...).BussinessDate`.
- `src/POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor`: (1) `_canForceConfirm =
  IsInRole(ITOps)||IsInRole(SystemAdmin)`, `CanConfirm` cho ITOps/Admin force kể cả còn POS mở ngày,
  truyền `_canForceConfirm` xuống service (StoreOperator luôn false — defense-in-depth); cảnh báo +
  confirm dialog nhắc rõ khi force. (2) Sau xác nhận thành công `_businessDate = businessDate.AddDays(1)`
  rồi `SearchAsync()` — tự load lưới ngày D+1. (3) `OnInitializedAsync`: StoreOperator tự lấy ngày
  kinh doanh hiện tại của store (`GetCurrentBusinessDateAsync`; null → hôm nay) + auto-load, khỏi bấm
  "Tìm kiếm"; ITOps/Admin giữ thủ công.
- Doc: `docs/CURRENT_STRUCTURE.md` (signature `IBusinessDayService`), `docs/web/logic/eod.md` (flow).

**Pattern mới (nếu có):** Không — feature theo pattern sẵn có. (Lưu ý kỹ thuật: SP trả cột tên khác
DTO → dùng class trung gian nullable + project, KHÔNG map thẳng SP vào DTO response.)

**Lưu ý cho session sau:** SP `GetSalesEODConfirm` (DB CentralSale per-store) trả cột đặt tên theo
legacy `SaleBusinessStoreModel` — nếu bổ sung cột hiển thị, map qua `SalesEodConfirmRow` chứ đừng đổi
tên property `PosDayStagingDto` (đang dùng ở razor). Cột "Số lượng bán" tạm map từ `CountOrderNo`
(số đơn) — còn `// TODO confirm` vì SP không có cột item-quantity thật.

---

## [2026-07-04] Fix bẫy MudMessageBox — nút Yes không theo chuẩn Outlined (8 page)

**Layer:** POS.Web

**Loại:** Bug fix + cập nhật chuẩn (ngăn tái diễn)

**Nguyên nhân:** `DialogService.ShowAsync<MudMessageBox>(title, new DialogParameters{...}, options)`
render nút Yes bằng markup **mặc định của MudBlazor** — API này không có `<YesButton>` slot để
chỉnh `Variant`, nên nút luôn ra `Variant.Filled` bất kể chuẩn dự án đã chuyển hết sang
`Outlined`. Grep `MudButton.*Variant.Filled` không bắt được vì nút không tồn tại trong markup của
page — đây là lý do đợt rà soát rollout trước đó (2 entry bên dưới) không phát hiện ra.

**Thay đổi:**
- Chuyển 8 file từ `DialogService.ShowAsync<MudMessageBox>(...)` sang khai báo trực tiếp
  `<MudMessageBox @ref="_confirmBox">` + `<YesButton><MudButton Variant="Variant.Outlined" .../>
  </YesButton>` + gọi `_confirmBox!.ShowAsync()`: `BusinessDayPage.razor`, `VouchersPage.razor`,
  `SpecialComboPage.razor`, `PromotionSetupPage.razor`, `PosDataSetupPage.razor`,
  `DataRawLogPage.razor`, `UsersPage.razor`, `BankPosPage.razor`.
- `UsersPage.razor` cần thêm field động `_confirmTitle`/`_confirmYesText`/`_confirmYesColor` vì
  title/màu nút Yes đổi theo trạng thái khóa/mở khóa user.
- Cập nhật chuẩn để không tái diễn: `.claude/skills/web/SKILLS.md` (sửa ví dụ mẫu cũ đang dùng
  `Variant.Filled` trong chính pattern `MudMessageBox @ref`, thêm cảnh báo rõ anti-pattern + danh
  sách 8 file, thêm bullet vào "KHÔNG làm"), `CLAUDE.md` §14 (thêm callout "Bẫy dễ bỏ sót — confirm
  dialog"), `.claude/rules/mudblazor-flat-ui.md` §3 (thêm bullet tương tự).

**Verification:** `dotnet build src/POS.Web/POS.Web.csproj` → 0 error. `dotnet test
tests/POS.ContractTests` → 25/25 pass.

**Lưu ý cho session sau:** Bất kỳ page nào cần confirm dialog PHẢI dùng
`<MudMessageBox @ref>` khai báo trong markup — KHÔNG dùng `DialogService.ShowAsync<MudMessageBox>`
dù có vẻ gọn hơn, vì không thể style nút Yes theo chuẩn Outlined của dự án.

---

## [2026-07-04] MudBlazor Flat UI v2 — rollout đầy đủ toàn bộ 4 cụm menu còn lại (Cửa hàng, Khuyến mãi, Vận hành, Quản trị)

**Layer:** POS.Web

**Loại:** UI polish diện rộng (tiếp nối rollout pilot 9 page "Danh mục" cùng ngày)

**Thay đổi:**
- Áp dụng đầy đủ chuẩn Flat UI v2 (xem entry "MudBlazor Flat UI v2" pilot bên dưới cho chi tiết
  token theme) cho **~35 page + ~25 dialog** còn lại thuộc 4 cụm menu:
  - **Cửa hàng**: `Operations/{BusinessDayPage,EosShiftsPage,ShiftSummaryPage}`,
    `Transactions/{TransactionsPage,RefundsPage,VoidsPage}`,
    `Reports/{RevenuePage,RevenueByStaffPage,RevenueByStorePage,DetailRevenuePage,
    SalesByCategoryPage,RevenueHourlyPage,PaymentBreakdownPage,TopProductPage,LoyaltyPage}`
    + dialog liên quan (`EosShiftDetailDialog`, `TransactionDetailDialog`, `VoidDetailDialog`,
    `ProductOrdersDialog`...).
  - **Khuyến mãi**: `Offers/{OffersPage,PromotionSetupPage,SpecialComboPage}`,
    `CouponVoucher/{CouponsPage,CouponIssuePage,VouchersPage,VouchersPublishedPage,
    VoucherIssuePage}` + dialog (`CouponAdvancedDialog`, `CouponItemPickerDialog`,
    `VoucherItemPickerDialog`...).
  - **Vận hành**: `HealthPage`, `AlertsPage`, `QueuesPage`, `LogsPage`, `DataRawLogPage`,
    `SqlConsoleAuditPage` (route `/ops/activity-log` — tên file khác tên route, phát hiện trong
    lúc rollout), `PosDataSetupPage` + dialog (`PosDataSetupFormDialog`).
  - **Quản trị**: `UsersPage`, `RolesPage`, `ConfigPage`, `AuditPage`, `SqlConsolePage`,
    `EncryptSecretPage` + dialog (`UserFormDialog`).
- Mọi `MudButton` `Variant.Filled`/`Variant.Text` → `Variant.Outlined` (không ngoại lệ); mọi filter/
  input `MudPaper` thêm class `pos-filter-panel`; page-header icon/button `Size="Size.Small"` +
  title `Style="font-weight:400"`; dọn hardcode `Style="border-radius:4px"` trên
  `MudProgressLinear`.
- Phát hiện + sửa 2 dialog bị bỏ sót ở đợt pilot: `Catalog/Price/Dialogs/PriceItemPickerDialog.razor`,
  `Ops/Dialogs/PosTerminalEditDialog.razor`.
- Phát hiện + sửa 1 page bị bỏ sót ở đợt pilot (không nằm trong sidebar nav, chỉ reachable từ
  `VouchersPage`): `VoucherIssuePage.razor` + dialog `VoucherItemPickerDialog.razor` — đối xứng
  với `CouponIssuePage.razor` (đã convert ở đợt pilot).
- `Store/Dialogs/EosDayShiftListDialog.razor` xác nhận **orphaned** (grep không còn page nào mở
  dialog này) — cố ý không convert, giữ nguyên chờ dọn dẹp sau.

**Quy trình thực hiện:** dùng 6 subagent chạy song song (Agent tool, không phải Workflow — không
có opt-in ultracode), mỗi agent nhận đúng 1 bộ rule cơ học (button/filter-panel/header/radius) +
1 file tham chiếu đã convert (`ProductsPage.razor`) làm chuẩn calibrate, xử lý 1 nhóm menu độc
lập. Sau khi tất cả agent xong, tự grep quét lại toàn bộ `Components/Pages/` để xác nhận không còn
`MudButton Variant.Filled/Text` sót (chỉ còn `Login.razor` — cố ý, và `EosDayShiftListDialog.razor`
— orphaned).

**Pattern mới:** Không có pattern mới — đây là rollout cơ học của pattern đã thiết lập ở entry
pilot bên dưới. Đã cập nhật `.claude/rules/mudblazor-flat-ui.md` mục "Trạng thái rollout" để phản
ánh phạm vi đầy đủ.

**Lưu ý cho session sau:**
- Toàn bộ ~44 page + ~34 dialog trong `Components/Pages/` (Danh mục + Cửa hàng + Khuyến mãi +
  Vận hành + Quản trị) nay đã đồng bộ chuẩn Flat UI v2. Page mới tạo sau này phải theo đúng chuẩn
  này ngay từ đầu (xem `CLAUDE.md §14`, `.claude/skills/web/theming.md`).
- Icon set `Icons.Material.Outlined.*` **vẫn chỉ** áp dụng cho `MainLayout.razor` (sidebar +
  AppBar) — icon trong nội dung từng page/button vẫn `Filled` như cũ, đây là quyết định có chủ
  đích, chưa mở rộng.
- Build + `dotnet test tests/POS.ContractTests` (25/25) đã xanh sau toàn bộ đợt rollout — verify
  cuối cùng chạy sau khi cả 6 agent xong + sau khi tự vá 4 gap phát hiện thêm.

---

## [2026-07-04] Sidebar UI polish — bỏ icon riêng cấp 2, ẩn expand icon, đổi tên leaf, thu gọn spacing

**Layer:** POS.Web

**Loại:** Refactor (UI polish, không đổi logic nghiệp vụ)

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: icon `MudNavGroup` cấp 2 (Vận hành/Giao dịch/Báo cáo/Tổ chức/Thiết bị POS/Sản phẩm/Giá bán/Chương trình KM/Coupon & Voucher/Giám sát/Nhật ký/Cấu hình) đổi đồng nhất về `ChevronRight` — giống icon cấp 3, chỉ cấp 1 còn giữ icon riêng; thêm `HideExpandIcon="true"` cho toàn bộ `MudNavGroup` (cấp 1+2) ẩn mũi tên expand mặc định bên phải.
- Đổi tên 6 title leaf: "Tỉnh / Thành"→"Chi nhánh", "Khai báo máy POS"→"POSTerminal", "Máy POS ngân hàng"→"POS bank", "Danh sách SP / Barcode"→"Danh sách", "Setup giá (Bulk Import)"→"Setup giá bán", "Danh mục khuyến mãi"→"Danh mục".
- `src/POS.Web/wwwroot/app.css`: dòng menu cấp 2 thêm `padding-top/bottom:3px` + `line-height:1.5` (thu gọn ~15% so với mặc định MudBlazor `padding:4px`+`line-height:1.75`); `.mud-drawer .mud-nav-link` thêm `letter-spacing:-0.022em` (rút tracking, tránh label dài xuống dòng).
- Giữa chừng có 1 lần nhầm lẫn: đã lỡ xóa toàn bộ `@bind-Expanded` + logic accordion (`UpdateExpanded`, `OnLocationChanged`, `IAsyncDisposable`) tưởng đây là thứ cần bỏ — đã khôi phục lại đầy đủ ngay trong cùng session, giữ nguyên hành vi accordion tự mở/đóng theo route (`docs/WEB_STATUS.md` mục I3).

**Pattern mới:** Không có pattern hoàn toàn mới, nhưng pattern sidebar 3 cấp đã có (`.claude/skills/web/SKILLS.md` §"Sidebar nav (MainLayout) — 3 cấp") bị lệch với thực tế → đã cập nhật lại ví dụ code + anti-pattern trong file đó, và mục 5 `.claude/rules/mudblazor-flat-ui.md`.

**Lưu ý cho session sau:** Muốn ẩn UI chỉ báo expand/collapse của `MudNavGroup` → dùng prop `HideExpandIcon="true"` (KHÔNG xóa `@bind-Expanded`, đó là 2 cơ chế độc lập — Expanded quyết định trạng thái mở/đóng + accordion theo route, HideExpandIcon chỉ ẩn mũi tên hiển thị). `MudNavLink` không có `HideExpandIcon` (chỉ MudNavGroup có).

---

## [2026-07-04] MudBlazor Flat UI v2 — theo mẫu "Mud Mini" (sidebar/appbar sáng, borderless, radius 16px, button Outlined toàn app)

**Layer:** POS.Web

**Loại:** Pattern mới (redesign theme toàn diện) + UI polish 9 page + 9 dialog "Danh mục"

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`:
  - Sidebar/AppBar chuyển từ navy đậm (`#1B3A5C`) sang nền sáng (`#FFFFFF`) + chữ tint navy — theo
    mẫu MudBlazor chính thức "Mud Mini" (`docs/web/images/flat1.jpg`), thay cho Ynex (đã đánh giá
    và loại bỏ vì không phải MudBlazor gốc, rebrand rủi ro cao).
  - `DefaultBorderRadius` 4px → 16px.
  - `Shadows.Elevation[1..5]`: hairline (`0 0 0 1px`) → `"none"` (borderless hoàn toàn — card
    phân tách bằng chênh lệch nền Surface/Background, không viền không bóng).
  - `Typography.H5`: FontWeight 700→800, LetterSpacing -0.01em→-0.02em.
  - `Typography.Body1`: FontSize 0.875rem→0.75rem (giảm ~15%) + FontWeight=400 — chi phối input
    `MudTextField`/`MudSelect`/`MudDatePicker`/`MudAutocomplete` toàn app (không ảnh hưởng MudTable).
- `src/POS.Web/wwwroot/app.css`: thêm token `--pos-primary-bg`/`--pos-teal-bg`; viết lại CSS
  sidebar cho nền sáng (active-item = pill `--pos-primary-bg`, 3 tầng chữ opacity navy); class mới
  `.pos-sidebar-brand`, `.pos-filter-panel` (nền soft-tint filter panel); icon sidebar giảm còn
  `1.25rem`; nav item inset ngang 8px; nâng cấp `.pos-delta-up/down` thành pill badge (giữ ngữ
  nghĩa tăng=xanh/giảm=đỏ); softening viền header MudTable (2px navy → 1px `--pos-border`).
- `src/POS.Web/Components/Layout/MainLayout.razor`: `MudAppBar Color="Color.Primary"` →
  `Color.Default`; thêm `div.pos-sidebar-brand` (logo `MudAvatar` + "RPOS") thay `MudDrawerHeader`
  cũ (text thô "POSMaster POS System"); đổi toàn bộ icon sidebar `Icons.Material.Filled.*` →
  `Outlined.*`; đổi brand text "POS Dashboard – POSMaster" → "RPOS Dashboard", "POSMaster" →
  "RPOS".
- **9 page + 9 dialog trong menu "Danh mục"** (EmployeesPage, StorePage, ProvincesPage,
  PosMapPage, BankPosPage, ProductsPage, ProductLockPage, PricesPage, PriceSetupPage +
  EmployeeFormDialog, EmployeeChangePasswordDialog, StoreCreateDialog, StoreDetailDialog,
  BranchCreateDialog, BranchDetailDialog, PosTerminalDetailDialog, BankPosDetailDialog,
  ProductDetailDialog): mọi `MudButton` `Variant.Filled`/`Variant.Text` → `Variant.Outlined`
  (không ngoại lệ, kể cả nút trong confirm dialog/bulk action/nút Lưu cuối); filter panel thêm
  class `pos-filter-panel`; page-header icon/button thêm `Size="Size.Small"` + title thêm
  `Style="font-weight:400"`; dọn hardcode `Style="border-radius:4px"` trên `MudProgressLinear`.
- `ProductsPage.razor`: bỏ 5 cột thừa trên bảng hiển thị (Mã SP PLG, Tên SP (VN), ĐVT BC, Mã cha,
  Size) — vẫn giữ đủ 11 cột trong Export Excel (không đổi).

**Pattern mới:** Toàn bộ pattern (borderless card, Outlined-mọi-nơi cho button kể cả trong dialog,
input font-size 12px, sidebar brand header + icon Outlined, `pos-filter-panel`) đã ghi vào
`CLAUDE.md §14`, `.claude/rules/mudblazor-flat-ui.md` (rules file mới — có lịch sử quyết định đầy
đủ, kể cả phương án đã cân nhắc và loại bỏ), `.claude/skills/web/theming.md`,
`.claude/skills/web/SKILLS.md`, `.claude/skills/web/ui-polish-standard.md`.

**Lưu ý cho session sau:**
- Chỉ 9 page + 9 dialog "Danh mục" đã migrate đầy đủ sang chuẩn v2. ~31 page/dialog khác (Store
  reports, Ops, Admin, Promotion) vẫn dùng `Filled` cho CTA + chưa có `pos-filter-panel`/page-header
  sizing — rollout tiếp khi có yêu cầu, xem mục TODO cuối `.claude/rules/mudblazor-flat-ui.md`.
- `<PageTitle>` (tab browser) của các page vẫn giữ "... – POS Dashboard" — chỉ đổi brand text ở
  sidebar/AppBar sang "RPOS" theo đúng yêu cầu, chưa rename toàn app.
- Đây là bản v2 kế tiếp bản v1 (2026-06-26, flat hairline) — cách nhau chưa đầy 2 tuần; nếu cần
  đối chiếu/rollback, xem lịch sử quyết định đầy đủ trong `.claude/rules/mudblazor-flat-ui.md`.
- Build lúc thực hiện session này bị chặn nhiều lần do Visual Studio giữ lock file DLL (đang debug
  song song) — đã verify xanh (`dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25)
  sau khi VS nhả lock ở cuối session.

---

## [2026-07-04] Xác nhận kết thúc ngày — port từ legacy StoreActivitiesController sang BusinessDayPage

**Layer:** POS.Web + POS.Application + POS.Infrastructure + POS.Common

**Loại:** Feature (port có chủ đích từ `src/legacy/`) + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/CentralSale/{PosDayStagingDto,BusinessDayConfirmDto,ConfirmBusinessDayRequest,ConfirmBusinessDayResult}.cs`: DTO mới.
- `src/POS.Infrastructure/Repositories/Sale/{I}CentralSaleRepository.cs`: thêm `GetPosDayStagingAsync`, `GetBusinessDayConfirmAsync`, `ConfirmBusinessDayAsync` — connection per-store qua `StoreRoutedConnectionFactory` (không phải `CentralSaleConnectionFactory` central dùng cho báo cáo đa store).
- `src/POS.Application/Features/StoreActivities/{I}BusinessDayService.cs`: mới, merge master POS terminal (`ICentralMDRepository.GetPosTerminalListAsync`, CentralMD) + staging shard (`ICentralSaleRepository`), validate rule "tất cả POS đã đóng ngày" trước khi cho xác nhận. Đăng ký DI trong `POS.Application/DependencyInjection.cs`.
- `docs/sql/BusinessDay_ConfirmEndDate.sql`: bảng `dbo.BusinessDayConfirm` + SP `usp_BusinessDay_ConfirmEndDate` — **chạy trên DB "CentralSale" theo TỪNG STORE** (shard, KHÔNG PHẢI RPOSMasterData/CentralMD), vì cần cùng 1 transaction với `UPDATE dbo.BussinessDateOpen` (advance +1 ngày cho máy POS) — atomic tuyệt đối.
- `src/POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor`: viết lại hoàn toàn (route giữ nguyên `/store/business-day`) — chọn 1 store bắt buộc (mặc định store đầu theo StoreNo, không có "Tất cả"), ngày kinh doanh mặc định hôm nay, KHÔNG tự load khi mở trang (chờ bấm Tìm kiếm); lưới per-POS-terminal + nút Xác nhận.
- `src/POS.Web/Components/Layout/MainLayout.razor`: đổi tên menu "Ngày kinh doanh" → "Xác nhận kết thúc ngày".
- Xóa `src/POS.Web/Components/Pages/Store/Dialogs/EosDayShiftListDialog.razor` (chỉ dùng bởi BusinessDayPage cũ, đã grep xác nhận không dùng chung; `EosShiftDetailDialog`/`GetEosDayListAsync`/`GetEosShiftListAsync` GIỮ NGUYÊN vì dùng chung với ShiftSummaryPage/EosShiftsPage).

**Pattern mới:** SP ghi dữ liệu có yêu cầu atomic với 1 bảng đã tồn tại sẵn ở DB khác CentralMD (ở đây là `BussinessDateOpen` trên DB "CentralSale" theo store) thì đặt bảng/SP mới CÙNG DB đó thay vì mặc định CentralMD — ưu tiên atomicity hơn quy ước mặc định. Chưa đưa vào SKILLS.md vì đây là quyết định case-by-case, không phải quy tắc chung.

**Lưu ý cho session sau:** Rule "tất cả POS đã đóng ngày" dựa trên sự tồn tại của dòng `POSEOD_API` (Store+Terminal+BusinessDate) — đây là giả định hợp lý dựa trên API `UpdatePOSEODAsync` đã có sẵn, CHƯA được xác nhận 100% với vận hành thực tế. Cột "Tiền mặt" dùng `POSShiftHeader`/`POSShiftLine` với giả định tên bảng giống DB CentralSale trung tâm (chưa xác minh trên shard DB) — có TODO comment trong code. Script `docs/sql/BusinessDay_ConfirmEndDate.sql` phải chạy thủ công trên DB CentralSale của TỪNG store (không phải 1 lần duy nhất như các script CentralMD khác).

---

## [2026-07-03] DataSync — fix đường dẫn UNC/CHANGE + Action envelope theo caller

**Layer:** POS.Api + POS.Application + POS.Common

**Loại:** Bug fix + Feature (tiếp nối nút Sync POSMap bên dưới)

**Thay đổi:**
- `SyncDataPosController.DeleteFileFromFTP`: POS gửi `filePath` UNC (`\\ip\FTPBLUEPOS\...`) → trước đây
  `File.Exists` trên UNC thô, trên Linux/Docker không resolve → luôn "không tồn tại". Fix: map UNC→local qua
  helper mới `ISyncDataPosService.ResolveFtpPhysicalPath` + guard path-traversal. `DowloadFileStream` refactor
  dùng chung helper (bỏ khối map inline).
- `SyncDataPosController.GetFileFromFTP` nhánh CHANGE: trước truyền `AppSettings:FolderShare` → list từ
  `{FolderShare}\CHANGE\{folderFile}` (thiếu segment `SyncDataPos\POS`) → không thấy file. Fix: truyền `pathSync`
  từ query; `GetFileFromServerApiAsync` bỏ special-case `if(typeSync=="ALL")`, **luôn** giải qua
  `MapFtpPath($"{pathSync}/{folderFile}")` → listing/URL/UNC nhất quán với nơi file tạo, hết lỗi sai case
  `syncdatapos/pos` trên Linux.
- `MasterDataSyncService.ActionFor` + `GetMasterDataFileRequest.SyncAction` (field mới): Action envelope tách
  theo caller — POS ALL giữ `TRUNC-INSERT`→`INSERT`; Web Sync (`PushStartOfDayDataAsync`) đặt `SyncAction="DELETE-INSERT"`
  → **mọi batch** ghi `DELETE-INSERT`. KHÔNG đổi logic stream/zip, không đổi dữ liệu (web vẫn full data).

**Pattern mới:** "Xử lý đường dẫn file POS gửi (SyncDataPos) — luôn giải về FtpRootPath, dùng chung" +
tham số hoá hành vi theo caller qua field DTO nội bộ → đã cập nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** với mọi endpoint nhận path từ POS, luôn map UNC→local bằng `ResolveFtpPhysicalPath`
trước khi thao tác file; `pathSync` POS gửi đã đủ `SyncDataPos/POS/{typeSync}` nên dùng `MapFtpPath` (đừng ghép
`FolderShare`). Muốn đổi hành vi theo caller → thêm field vào request DTO, đừng detect caller.

---

## [2026-07-03] PosMapPage `/catalog/pos-setup` — nút "Đẩy dữ liệu đầu ngày" cho máy POS

**Layer:** POS.Web + POS.Application

**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Ops/PosMapPage.razor`: thêm cột **Action** (sau `IsOnline`) — nút Sync +
  `MudMessageBox` confirm + spinner-trong-nút + pulse nền dòng (`_syncing` HashSet) + `@onclick:stopPropagation`
  (chặn mở nhầm dialog chi tiết). Ghi **audit log** `SYNC`/`PosTerminal` khi thành công (qua `IAuditLogger`).
- `src/POS.Application/Features/DataSync/ISyncDataPosService.cs` + `SyncDataPosService.cs`: thêm
  `PushStartOfDayDataAsync(siteCode, posTerminal, ct)` — inject `IMasterDataSyncService`, **gọi trực tiếp qua DI**
  (không HTTP sang POS.Api), tái dùng nguyên `EnsureMasterDataFileAsync` (`TypeSync=ALL` full data) — **KHÔNG đổi**
  logic sinh file txt/zip.
- `src/POS.Web/wwwroot/app.css`: keyframe `pos-row-syncing` (pulse nhẹ dòng đang xử lý).
- `src/POS.Web/appsettings.json` (DEV): `FolderShare`/`FtpRootPath`/... khớp POS.Api (key đã tồn tại — không sync UAT/Prod).
- `docs/ROLLOUT.md`: thêm **§O3** — yêu cầu POS.Web `FtpRootPath` trỏ chung thư mục POS.Api phục vụ (UAT/PROD đang rỗng).
- `docs/CURRENT_STRUCTURE.md`: thêm chữ ký `PushStartOfDayDataAsync` vào `ISyncDataPosService`.

**Bug đã fix trong task:** ban đầu dựng `TargetDir = Path.Combine(FolderShare, "CHANGE", ...)` → sai thư mục
(`FTPBLUEPOS\CHANGE\...`). Sửa dùng `MapFtpPath("SyncDataPos/POS/CHANGE/{site}/{terminal}")` để **bám y hệt
controller** (`FTPBLUEPOS\SyncDataPos\POS\CHANGE\{site}\{terminal}` — đúng nơi POS tạo/đọc + URL download).

**Pattern mới:** POS.Web kích hoạt tác vụ server-side (sinh file) bằng cách **gọi trực tiếp Application service
của POS.Api qua DI** thay vì HTTP; khi tái dùng phải **bám đúng convention path `MapFtpPath` của controller**,
KHÔNG tự dựng path bằng `FolderShare` → đã cập nhật `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:** file sinh trên host POS.Web nhưng POS tải qua POS.Api → 2 app phải chung `FtpRootPath`
(share/volume). Khi tái dùng logic file của POS.Api từ Web, luôn tra cách controller dựng `TargetDir` để khớp 100%.

---

## [2026-07-03] Thực thi rollout C4 — mã hóa xong appsettings.Production.json (POS.Api + POS.Web)

**Layer:** POS.Api + POS.Web + Infra/docs

**Loại:** Bảo mật (thực thi rollout — tiếp nối entry 2026-07-02 bên dưới)

**Thay đổi:**
- Sinh khóa `POS_SECRET_KEY` (AES-256, base64) bằng project console tạm (`ProjectReference` tới
  `POS.Infrastructure.csproj`, gọi thẳng `SecretProtector.GenerateKey()`/`Encrypt()`, verify round-trip
  `Decrypt()` trước khi dùng, rồi xóa project tạm) — tránh tự viết lại AES-GCM, đảm bảo tương thích
  100% với code decrypt thật. Kỹ thuật này đã ghi vào `.claude/skills/api/SKILLS.md`.
- `src/POS.Api/appsettings.Production.json` + `src/POS.Web/appsettings.Production.json`: thay toàn bộ
  9 connection string (`Password=...`) + `RabbitMQ.Password` mỗi file bằng token `enc:...` — không còn
  password thật dạng plaintext trong 2 file này.
- `.env` (local, gitignored): thêm `POS_SECRET_KEY` để `docker compose up` dùng được ngay cho service
  `webapp` (= POS.Api).
- `docs/architecture/appsetting.md` (**file mới**): tài liệu tra cứu nhanh — bảng "dùng mã hóa hay
  plaintext" (tự suy ra từ nội dung file, không phải 1 cờ cấu hình riêng), phạm vi áp dụng, anti-pattern,
  link sang `docs/ROLLOUT.md`/`docs/guide-deploy.md`/SKILLS.md.
- `CLAUDE.md`: thêm 1 dòng vào bảng "Mục lục tài liệu kiến trúc" trỏ tới `docs/architecture/appsetting.md`.
- `docs/WEB_STATUS.md`: cập nhật dòng S5 — từ ⚠️ (chưa rollout) → ✅ (đã rollout Production).

**Pattern mới:** Kỹ thuật sinh/mã hóa secret ngoài app đang chạy bằng project console tạm (xem
`.claude/skills/api/SKILLS.md` — cuối section "Pattern: Mã hóa credentials trong appsettings").

**Lưu ý cho session sau:** `appsettings.UAT.json` của cả 2 project **chưa** được mã hóa (ngoài phạm vi
task này — chỉ làm Production theo yêu cầu). Trên server UAT/PROD thật, vẫn cần người vận hành tự đặt
`POS_SECRET_KEY` (biến môi trường/`docker run -e`) — Claude không có quyền truy cập server đó. Khóa +
3 password thật đã đi qua hội thoại này (người dùng đã xác nhận chấp nhận đánh đổi này) — nếu cần đảm
bảo tuyệt đối "khóa chưa từng qua AI", rotate khóa sau qua `/admin/encrypt-secret`.

---

## [2026-07-02] Mở rộng mã hóa credentials (C4) sang POS.Api — đổi tên khóa chung POS_SECRET_KEY

**Layer:** POS.Api + POS.Web + Infra/docs

**Loại:** Bảo mật (chuẩn bị go-live Production)

**Thay đổi:**
- `src/POS.Api/Program.cs`: thêm hook giải mã token `enc:...` (AES-256-GCM qua `SecretProtector`),
  NGAY SAU `CreateBuilder`, TRƯỚC `AddInfrastructure` — copy đúng pattern đã có ở `src/POS.Web/Program.cs`.
  Trước đây cơ chế `SecretProtector` chỉ wired ở POS.Web; POS.Api đọc `appsettings.Production.json`
  plaintext dù `docker-compose.yml` đã âm thầm truyền sẵn biến khóa vào container (không có code tiêu thụ).
- Đổi tên biến môi trường khóa AES từ `POSWEB_SECRET_KEY` → **`POS_SECRET_KEY`** (dùng chung cho cả
  2 project, không còn gắn riêng "Web") — cập nhật `src/POS.Web/Program.cs`,
  `EncryptSecretPage.razor` (`/admin/encrypt-secret`), `SecretProtector.cs` (doc comment + thông báo lỗi),
  `docker-compose.yml`, `.env.example`.
- `docs/guide-deploy.md`: thêm `-e POS_SECRET_KEY=...` vào ví dụ `docker run` của cả POS.Api (§3.1)
  và POS.Web (§3.2) + ghi chú ở checklist.
- `docs/ROLLOUT.md` §C4: viết lại — phạm vi rollout nay là **CẢ HAI** `appsettings.Production.json`
  (POS.Api + POS.Web), cùng 1 khóa, token sinh ở trang `/admin/encrypt-secret` (POS.Web) dùng được cho
  cả 2 file vì cùng plaintext + cùng khóa. Thêm ghi chú naming: service `webapp` trong `docker-compose.yml`
  (root) thực chất là POS.Api, không phải POS.Web.
- `.claude/skills/api/SKILLS.md`, `docs/web/security.md`, `docs/WEB_STATUS.md`: đồng bộ tên biến +
  phạm vi 2 project trong phần mô tả pattern/security.

**Pattern mới:** Không có pattern kỹ thuật mới — tái dùng nguyên `SecretProtector` đã có, chỉ nhân rộng
hook sang project thứ 2 và đổi tên biến môi trường cho đúng phạm vi dùng chung.

**Lưu ý cho session sau:** Thực thi mã hóa Production thật (Bước 1-5 `docs/ROLLOUT.md` §C4) vẫn là việc
của **người vận hành** — Claude không giữ khóa, không tự thay password thật. Tại thời điểm này,
`appsettings.Production.json` của cả POS.Api và POS.Web **vẫn còn plaintext** (password thật) —
cơ chế code đã sẵn sàng cho cả 2 project, chỉ còn chờ ops chạy rollout. `POS.Worker` vẫn ngoài phạm vi
(chưa có hook, vẫn plaintext). Đã verify: `dotnet build` (0 lỗi) + `dotnet test tests/POS.ContractTests`.

---

## [2026-07-02] Dọn sạch docs tham chiếu legacy/migrate — source code legacy đã xóa khỏi máy

**Layer:** Docs (`.claude/skills/`, `docs/`, `CLAUDE.md`, `README.md`) — không đụng code

**Loại:** Refactor tài liệu

**Thay đổi:**
- Xóa hẳn `_migration/INVENTORY.md`, `_migration/PROGRESS.md`, `docs/PROJECT_INVENTORY.md` —
  100% nội dung là inventory/tracking source legacy (.NET Framework 4.6.2, VCM.BLUEPOS) đã bị
  xóa khỏi máy, không còn đối chiếu được.
- Đổi tên `.claude/skills/web/ui-migrate-legacy.md` → `.claude/skills/web/ui-polish-standard.md`
  (giữ nguyên nội dung kỹ thuật — pattern chip màu, empty-state, action bar, MudCard polish —
  chỉ bỏ khung "trang migrate từ legacy" vì không còn phân biệt trang cũ/mới).
- `CLAUDE.md`: bỏ hẳn mục "Migrate VCM.BLUEPOS → POS.Web" (5 mục con), bỏ hàng inventory legacy
  trong bảng doc-map, cập nhật §13 POS.Web trỏ sang `ui-polish-standard.md`.
- `docs/CURRENT_STRUCTURE.md`: bỏ "MỤC H — Những gì chưa có" (bảng Controllers/Services/BLO/
  Helpers "chưa migrate" đối chiếu inventory đã xóa) + số liệu thống kê liên quan.
- `docs/API_CONTRACT.md`: bỏ mục 10 "Notes cho Migration sang .NET 10" (đã hoàn thành từ lâu).
- `.claude/skills/api/SKILLS.md`: pattern "xác minh tên bảng qua legacy EDMX"
  (`src/legacy/*/EF/**/*.edmx`) → thay bằng tra `docs/architecture/centralMD-schema.md`.
- `.claude/skills/cache/SKILLS.md`, `.claude/skills/worker/SKILLS.md`: bỏ khung "migrate từ
  project cũ/IIS MemoryCache", giữ nguyên toàn bộ quy tắc kỹ thuật (Redis pattern, Worker pattern).
- `docs/architecture/centralMD-schema.md`, `docs/web/LOGIC_APPROVE_CTKM.md`: sửa cross-reference
  trỏ tới mục/file đã xóa.
- `README.md`: viết lại hoàn toàn — bản cũ mô tả sai kiến trúc (POS.API/POS.Domain/POS.Shared),
  sót lại từ giai đoạn lên kế hoạch ban đầu, còn trỏ tới `POS.Backend`/`analyze-legacy.md`.
- **Cố ý giữ nguyên**: `docs/CHANGELOG.md`, `docs/WEB_STATUS.md` — entry cũ có chữ "migrate"/
  "Legacy" là ghi chép lịch sử tại thời điểm đó (đã có ghi chú "giữ nguyên để tra cứu" ở đầu file).

**Pattern mới:** Không có — đây là dọn dẹp docs theo yêu cầu trực tiếp, không phát sinh pattern code mới.

**Lưu ý cho session sau:** `src/legacy/`, `_migration/`, `docs/PROJECT_INVENTORY.md` **không còn
tồn tại** — đừng đề xuất đọc/grep các đường dẫn này nữa. Khi cần tra tên bảng/cột DB dùng
`docs/architecture/centralMD-schema.md`; khi cần tra cấu trúc code hiện có dùng
`docs/CURRENT_STRUCTURE.md`. Đã verify: `dotnet build` (0 lỗi) + `dotnet test tests/POS.ContractTests` (25/25 pass).

---

## [2026-07-02] Validate ActicleNo tồn tại trong CpnVchBOMHeader trước khi tạo Voucher SAP

**Layer:** POS.Api (POS.Application + POS.Infrastructure)
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: thêm chữ ký
  `Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default)`.
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement với cache
  Redis Hash `MD:CpnVchBOMHeader` (positive-only, TTL 12h) — point-lookup
  `SELECT TOP 1 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo=@itemNo`.
- `src/POS.Application/Features/Sap/SAPService.cs`: inject thêm `ICentralMDRepository`; trong
  `CreateNewVoucherAsync` validate **toàn bộ** `Article_No` (khác rỗng) TRƯỚC vòng lặp tạo —
  mã không tồn tại → trả `400 "ActicleNo {x} không tồn tại"`, KHÔNG tạo phần tử nào (tránh
  partial vì loop không có transaction). `Article_No` rỗng → bỏ qua (giữ hành vi cũ). Guard tự
  áp cho cả `CreateReturnVoucher` (controller gọi lại `CreateNewVoucherAsync`).

**Pattern mới:** Existence-check cache (positive-only) — validate master data trước khi ghi
→ đã cập nhật `.claude/skills/cache/SKILLS.md` (Pattern 5).

**Quyết định:** đối chiếu cột `CpnVchBOMHeader.ItemNo` (theo quy ước mirror
`CpnVchBOMCodeIssue.ItemNo = ActicleNo`). Không đổi SP/DTO/contract, không tạo SQL mới.

**Lưu ý cho session sau:** Khi cần validate "khóa tồn tại trong master" trước một write, dùng
Pattern 5 (cache dương, không cache âm) — KHÔNG dùng Pattern 1. Nếu DBA thêm master mới cần dùng
ngay trong <12h: `DEL MD:CpnVchBOMHeader`.

---

## [2026-07-02] Vá đồng bộ dữ liệu Coupon (POS.Web) ↔ SAP Voucher (POS.Api) trong CpnVchBOMCodeIssue

**Layer:** Database (docs/sql)
**Loại:** Bug fix (thiếu field khi ghi) + Feature (unify check/redeem 2 nguồn)

**Vấn đề:**
1. `usp_Voucher_Create` (POS.Api tạo voucher SAP) không ghi `ItemNo` — mất liên kết ItemNo↔ActicleNo.
2. `usp_SetupCoupon_SaveIssue` (POS.Web phát hành coupon) chỉ ghi 7/22 cột nghiệp vụ khi insert
   mã — thiếu `Status/Return/ActicleNo/ActicleType/Value/Voucher_Currency/Validity_From_Date/
   Expiry_Date/CompanyCode/VoucherType`, khiến mã coupon KHÔNG thể redeem/check qua POS.Api.
3. `usp_Voucher_GetByCode`/`usp_Voucher_Redeem` chỉ nhận `Source='SAP'` — POS.Api không nhận diện
   được mã Coupon dù dùng chung bảng, trong khi POS.Web phát hành coupon cho khách dùng tại POS,
   và POS dùng coupon/voucher qua chính POS.Api — 2 luồng cần liên thông thật sự.

**Thay đổi:**
- `docs/sql/CpnVchBOMCodeIssue_ItemNoHardening.sql` (**file mới**) — mở rộng
  `CpnVchBOMCodeIssue.ItemNo` varchar(20)→varchar(50) (khớp width `ActicleNo`, tránh lỗi truncate
  khi SAP gửi `Article_No` dài) + thêm index `IX_CpnVchBOMCodeIssue_ItemNo` (bảng chưa có index
  nào trên cột này, cần thiết vì các UPDATE đồng bộ mới chạy trên mọi lần Lưu, không chỉ lần đầu).
- `usp_Voucher_Create`: thêm `ItemNo = @ActicleNo` (mirror) vào INSERT; thêm guard `THROW` khi
  `Code` trùng với 1 dòng ở Source khác (tránh vi phạm unique index bằng SqlException thô).
- `usp_SetupCoupon_SaveIssue`: Section insert Codes (chạy lần đầu) nay ghi đủ
  `ActicleNo(=ItemNo)/ActicleType/Validity_From_Date/Expiry_Date/Voucher_Currency('VND')/
  CompanyCode('WCM')/Status('SOLD')/Return(0)`, thêm `AND Source='COUPON'` vào gate (tránh bị
  "lừa" bởi ItemNo trùng của SAP). Thêm **Section 3b mới**: UPDATE không điều kiện (chạy mọi lần
  Lưu, kể cả sửa coupon sau này) đồng bộ lại `ActicleType`/ngày hiệu lực — KHÔNG đụng
  `Status/Return/Enabled` để không phá vỡ trạng thái đã redeem.
- `usp_SetupCoupon_SaveAdvanced`: thêm UPDATE mới đồng bộ `Value`/`VoucherType` (từ
  `@DiscountValue`/`@CpnVchType`) xuống `CpnVchBOMCodeIssue` — 2 field này chỉ có giá trị thật
  sau khi SP này chạy (không phải tham số của `usp_SetupCoupon_SaveIssue`).
- `usp_Voucher_GetByCode`/`usp_Voucher_Redeem`: bỏ hẳn filter `Source='SAP'` (`Code` đã unique
  toàn bảng nên không còn nhập nhằng) — nhận diện và redeem được cả mã Coupon. Redeem thành công
  nay thêm `Enabled=0` (hành vi MỚI, áp dụng cả 2 Source) — đồng bộ hiển thị "Locked" ở
  `usp_SetupCoupon_GetCodes` (POS.Web). Thêm chặn `Value IS NULL` khi validate amount (đóng lỗ
  hổng: coupon chưa chạy `SaveAdvancedAsync` có `Value` NULL → so sánh với NULL luôn UNKNOWN,
  amount bất kỳ sẽ lọt qua nếu không chặn tường minh).
- Không đổi C# — toàn bộ input đã là tham số sẵn có của các SP liên quan
  (`CouponRepository.cs`, `VoucherCodeRepository.cs`, `SAPService.cs`, `SAPController.cs`).
  Không DTO nào đổi field → contract test (`VoucherStatusResponse_locked`, `CouponCodeDto_locked`)
  giữ nguyên, vẫn xanh.
- `docs/architecture/centralMD-schema.md`: cập nhật mô tả cột `CpnVchBOMCodeIssue` (nhiều field
  nay dùng chung 2 Source thay vì chỉ SAP), 5 SP đã sửa hành vi. `docs/ROLLOUT.md` §D6.1: checklist
  chạy 4 script theo đúng thứ tự (ItemNoHardening trước tiên).

**Rủi ro đã rà soát nhưng KHÔNG sửa trong đợt này** (mức ưu tiên thấp, dead path hiện tại):
`usp_SetupCoupon_GetDetail.QuantityCode`, `usp_SetupCoupon_GetList.QtyCoupon`,
`usp_SetupCoupon_Delete` guard đều lọc `WHERE ItemNo=@ItemNo` không kèm `Source` — cùng loại rủi
ro va chạm ItemNo giữa 2 nguồn (xác suất cực thấp); `GetList`/`Delete` hiện không được gọi từ
trang `.razor` nào.

**Lưu ý cho session sau:**
- **CHƯA chạy SQL script nào trên DB thật** trong task này — xem `docs/ROLLOUT.md` §D6.1 để chạy
  đúng thứ tự (bắt buộc `CpnVchBOMCodeIssue_ItemNoHardening.sql` trước tiên).
- Nếu sau này `usp_SetupCoupon_GetList`/`usp_SetupCoupon_Delete` được wire lại vào UI, nhớ thêm
  `AND Source='COUPON'` vào các query lọc `ItemNo` (xem mục rủi ro ở trên).

## [2026-07-02] Fix bug IsCheckItem bị hard-code 0 khi tạo coupon mới (usp_SetupCoupon_SaveIssue)

**Layer:** Database (docs/sql)
**Loại:** Bug fix (production — đã xác nhận tồn tại trong bản deploy hiện tại qua `docs/sql/database/CentralMD.sql`)

**Vấn đề:** User báo tích "Áp dụng theo danh sách sản phẩm" + chọn sản phẩm khi phát hành coupon
mới nhưng lựa chọn không lưu được — sau khi trang tự reload, checkbox hiện lại **bỏ tích**.

**Nguyên nhân:** `docs/sql/SetupCoupon_Save.sql` — nhánh INSERT tạo `CpnVchBOMHeader` (coupon
mới) trong `usp_SetupCoupon_SaveIssue` hard-code cột `IsCheckItem = 0` thay vì dùng tham số
`@IsCheckItem` truyền vào. Nhánh UPDATE (sửa coupon đã có) vẫn đúng (`SET IsCheckItem =
@IsCheckItem`). `usp_SetupCoupon_SaveAdvanced` (chạy ngay sau `SaveIssueAsync` trong
`CouponIssuePage.razor`) cũng không SET lại `IsCheckItem` → giá trị `0` bị "khóa cứng" vĩnh viễn
cho coupon tạo mới, bất kể người dùng chọn gì trên UI.

**Thay đổi:** `docs/sql/SetupCoupon_Save.sql` — đổi giá trị insert từ `0` → `@IsCheckItem` ở vị
trí cột `IsCheckItem` trong `usp_SetupCoupon_SaveIssue` (nhánh tạo mới).

**Lưu ý cho session sau:**
- **BẮT BUỘC re-run `docs/sql/SetupCoupon_Save.sql` trên RPOSMasterData** để áp dụng fix (an
  toàn — script có `DROP PROCEDURE IF EXISTS` trước mỗi `CREATE`).
- Chưa xác nhận được liệu bug này có phải nguyên nhân DUY NHẤT khiến `CpnVchBOMLine` trống hay
  không (Lines insert ở SP không phụ thuộc `IsCheckItem`, nên về lý thuyết vẫn ghi độc lập) —
  cần theo dõi thêm sau khi user re-run script và test lại.

## [2026-07-02] Gộp SAP Internal Voucher vào CpnVchBOMCodeIssue (bảng dùng chung Coupon+Voucher)

**Layer:** POS.Infrastructure + POS.Application + POS.ContractTests
**Loại:** Refactor (gộp bảng dùng chung) + Bug fix (thiếu PK/race condition khi tạo voucher)

**Bối cảnh:** Phát hiện `CpnVchBOMCodeIssue` (POS.Web, Setup Coupon) và `Internal_Voucher`
(POS.Api, SAP Voucher real-time) ban đầu tưởng là logic trùng lặp, nhưng phân tích sâu cho thấy
đây là 2 tính năng khác nhau (Coupon = batch-generate, không có redeem trong solution; SAP
Voucher = lifecycle tài chính đầy đủ `SOLD→RDM`). Quyết định (đã chốt với chủ dự án): mở rộng
`CpnVchBOMCodeIssue` thành bảng DÙNG CHUNG cho cả 2 (cột discriminator `Source`), thay vì ép 2
domain khác nhau vào chung 1 Repository/Service.

**Thay đổi:**
- **Schema `CpnVchBOMCodeIssue`** (`docs/sql/CpnVchBOMCodeIssue_ExtendSchema.sql`): thêm cột
  `Source varchar(10) DEFAULT('COUPON')` (`'COUPON'`|`'SAP'`) + toàn bộ cột tài chính từ
  `Internal_Voucher` (`Status, Return, ActicleNo, ActicleType, Value, Voucher_Currency,
  Validity_From_Date, Expiry_Date, CompanyCode, Partner, IsEmployee, PhoneNumber, VoucherType,
  AmountUsed, OrderUsed`); mở rộng `Code` varchar(20)→varchar(50). **Rebuild bảng** để thêm
  `ID IDENTITY(1,1) PRIMARY KEY CLUSTERED` (trước đó KHÔNG có PK, tự tính
  `MAX(ID)+ROW_NUMBER()` — rủi ro race condition khi có traffic SAP real-time) + `UNIQUE FILTERED
  INDEX` trên `Code` (khóa nghiệp vụ thật, trước đó chỉ check ở tầng ứng dụng).
- **SP mới** (`docs/sql/Voucher_Read.sql`, `docs/sql/Voucher_Save.sql`): `usp_Voucher_GetByCode`,
  `usp_Voucher_Create` (idempotent, UPDLOCK/HOLDLOCK — fix race condition của code cũ: check-rồi-
  insert là 2 round-trip riêng, không transaction), `usp_Voucher_Redeem` (TVP
  `dbo.VoucherRedeemTVP`, giữ nguyên business rule + message tiếng Việt của
  `SAPVoucherRepository.RedeemVouchersAsync` cũ). Thay raw SQL bằng SP theo đúng convention dự án.
- **SP Coupon cập nhật** (`docs/sql/SetupCoupon_Save.sql`, `docs/sql/SetupCoupon_Read.sql`):
  `usp_SetupCoupon_SaveIssue` bỏ tự tính `ID` (nay IDENTITY), thêm `Source='COUPON'`;
  `usp_SetupCoupon_GetCodes` thêm filter `Source='COUPON'` (phòng thủ).
- **Code mới**: `IVoucherCodeRepository`/`VoucherCodeRepository`
  (`src/POS.Infrastructure/Repositories/CouponVoucher/`) — thay `ISAPVoucherRepository`/
  `SAPVoucherRepository` (đã XÓA, cùng thư mục `Sap/` rỗng đã xóa). `SAPService` đổi constructor
  dependency sang `IVoucherCodeRepository`; `CreateNewVoucherAsync` gộp check-tồn-tại + insert
  thành 1 lệnh `CreateOrGetAsync` atomic (fix bug cũ: không check giá trị trả về `InsertAsync`).
  **`ISAPService`/`SAPController`/DTO (`VoucherStatusResponse`, `CreateVoucherModel`,
  `VoucherUpdateRequest`) giữ NGUYÊN 100%** — JSON contract với 5.000 POS không đổi.
- **Migrate dữ liệu production**: `docs/sql/CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql`
  (idempotent) di chuyển voucher SAP thật từ `Internal_Voucher` sang `CpnVchBOMCodeIssue`
  (`Source='SAP'`). Sau go-live ổn định: `docs/sql/Internal_Voucher_RenameLegacy.sql` đổi tên
  `Internal_Voucher` → `Internal_Voucher_Legacy` (giữ backup tạm, KHÔNG xóa ngay).
- **Contract test mới**: `tests/POS.ContractTests/JsonFieldContractTests.cs` —
  `VoucherStatusResponse_locked` (DTO này trước đó CHƯA có test khóa field — lỗ hổng guardrail có
  sẵn, bổ sung vì task này động chạm trực tiếp tầng lưu trữ của DTO).
- Cập nhật `docs/architecture/centralMD-schema.md` (schema mới + 3 SP mới + đánh dấu
  `Internal_Voucher` LEGACY), `docs/CURRENT_STRUCTURE.md` (xóa `ISAPVoucherRepository`, thêm
  `IVoucherCodeRepository`), `docs/ROLLOUT.md` §D6 (checklist go-live theo đúng thứ tự script).

**Pattern mới:**
- **Bảng dùng chung + cột discriminator (`Source`)** cho 2 domain nghiệp vụ khác nhau nhưng cùng
  bản chất "mã định danh + trạng thái" — thay vì ép chung 1 Repository/Service (vi phạm SRP) hoặc
  giữ 2 bảng trùng lặp mãi mãi. Mỗi domain vẫn có Repository/Service riêng
  (`ICouponRepository` vs `IVoucherCodeRepository`), chỉ dùng chung storage.
- SP tạo mới **idempotent qua UPDLOCK/HOLDLOCK trong 1 transaction** (thay vì check-rồi-insert 2
  round-trip riêng ở tầng C#) khi cần đảm bảo atomic dưới traffic real-time cao.

**Lưu ý cho session sau:**
- **CHƯA chạy SQL script nào trên DB thật** trong task này — theo convention dự án, SP/schema
  áp dụng thủ công 1 lần trên `RPOSMasterData`. Xem `docs/ROLLOUT.md` §D6 để chạy đúng thứ tự
  trước khi deploy code này lên môi trường có kết nối DB thật.
- **TODO chưa chốt ngày**: lên lịch `DROP TABLE Internal_Voucher_Legacy` sau khi hệ thống ổn định
  2-4 tuần kể từ go-live §D6 (không thuộc phạm vi task này).
- Nếu cần domain "voucher/coupon" mới trong tương lai (vd đối tác khác), cân nhắc tái dùng cột
  `Source` (thêm giá trị enum mới) thay vì tạo bảng riêng, nếu shape dữ liệu tương thích.

## [2026-07-02] UI audit + gộp form CouponIssuePage (Phát hành Coupon)

**Layer:** POS.Web + POS.Application
**Loại:** Refactor (UI audit/gọn hóa form) + Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Promotion/CouponVoucher/CouponIssuePage.razor`:
  - Gộp toàn bộ field của `CouponAdvancedDialog` (UOM, CpnVchType, DiscountType/Value, MaxValue,
    LimitQty/LimitQtyUsed, IsMultiUsed/IsCheckAPI/Blocked) xuống thẳng form chính — bind vào field
    `_advanced` sẵn có. Nút "Cài đặt nâng cao" giữ nguyên code (`OpenAdvancedAsync` + dialog) nhưng
    ẩn qua `_showAdvancedButton=false` (dead code có chủ đích, chưa thiết kế lại chỗ đặt).
  - `SaveAsync()` nay gọi nối tiếp `SaveIssueAsync` → đồng bộ header vào `_advanced` →
    `SaveAdvancedAsync`, mỗi bước tự audit-log riêng (`SetupCoupon` / `SetupCouponAdvanced`).
  - Layout rút gọn qua nhiều vòng audit: 2 `MudCard` → 1 `MudCard` chia 6 nhóm `MudPaper Outlined`
    bo viền → gộp còn 3 nhóm (Thông tin chung / Thời gian hiệu lực + Giới hạn sử dụng / Cấu hình
    mã & giảm giá + Tùy chọn) → bỏ hẳn `MudCardHeader` (title+caption+tooltip) → bỏ `HelperText`
    (hint ngắn gộp vào `Label`, hint dài bỏ hẳn) → tiêu đề mỗi nhóm con đổi sang kiểu "legend lồng
    viền" (`position:absolute` đè lên viền trên `MudPaper`).
  - `MudNumericField` đổi `Variant` theo kiểu dữ liệu C#: `int` (LenCode/CharOfNumber/CharPosition/
    Quantity/LimitQty/LimitQtyUsed) → `Variant.Text`; `double` (DiscountValue/MaxValue) →
    `Variant.Outlined` + `Step="0.1"`.
- `src/POS.Application/Features/CouponVoucher/CouponService.cs`: `SaveAdvancedAsync` — rule
  "Từ ngày không được nhỏ hơn hôm nay" chỉ áp dụng khi tạo mới (`ItemNo` rỗng), không áp khi sửa
  coupon cũ (tránh chặn Lưu vô lý với coupon đang active có ngày bắt đầu trong quá khứ).
- `.claude/skills/web/form-input.md`: thêm §1a (nhóm con bo viền trong 1 `MudCard` + tiêu đề kiểu
  legend lồng viền) và §4a (`MudNumericField` Variant theo kiểu dữ liệu int/double) + anti-pattern
  + dòng tham chiếu.

**Pattern mới:**
- Nhóm con bo viền (`MudPaper Outlined`) + tiêu đề "legend lồng viền" (`position:absolute` +
  `background:var(--mud-palette-surface)`) thay cho tách nhiều `MudCard` khi các nhóm field cùng
  1 entity — đã cập nhật `.claude/skills/web/form-input.md` §1a.
- `MudNumericField` Variant theo kiểu dữ liệu C# (int→Text, double/decimal→Outlined+Step) — đã
  cập nhật `.claude/skills/web/form-input.md` §4a.

**Lưu ý cho session sau:**
- `CouponAdvancedDialog.razor` + `OpenAdvancedAsync()` không còn được gọi từ UI nhưng vẫn tồn tại
  trong code — nếu dọn dẹp sau này, nhớ đây là dead code có chủ đích, không phải sót lại do quên.
- Nếu tạo `MudNumericField` mới ở trang khác: tra kiểu C# của property trước — `int` dùng
  `Variant.Text`, `double`/`decimal` dùng `Variant.Outlined` + `Step` (khác chuẩn cũ "mọi input
  luôn Outlined").
- Rule ngày kiểu "chỉ chặn khi tạo mới, bỏ qua khi sửa" (`string.IsNullOrWhiteSpace(request.ItemNo)`)
  là pattern hữu ích chung cho các validate liên quan ngày hiệu lực khi entity đã tồn tại.

**appsettings sync:** không thay đổi appsettings.

---

## [2026-07-01] Migrate 9.1 Danh mục Bảng giá + 9.3 Setup Giá (Bulk Import)

**Layer:** POS.Web + POS.Application + POS.Infrastructure + POS.Common
**Loại:** Feature (migrate VCM.BLUEPOS PriceController/SetupPriceController)

**Thay đổi:**
- `src/POS.Common/Dtos/Price/PriceListDto.cs`, `PriceSetupDto.cs` (mới): DTO list/filter/import/save/context/result (Newtonsoft).
- `src/POS.Infrastructure/Repositories/Price/IPriceRepository.cs` + `PriceRepository.cs` (mới): reuse SP `GetSalesPriceList`/`_Export` (9.1); `ValidateImportAsync` (TVP inline LEFT JOIN Item/ItemUnitOfMeasure/Barcodes) + `SaveAsync` (SP `usp_SetupSalePrice_Save`, TVP).
- `src/POS.Application/Features/Price/IPriceService.cs` + `PriceService.cs` (mới): **port 100% validate `SetupPriceController.SaveItemPrice`** + build Pkey `{SalesType}-{ItemNo}-{UOM}-{SalesCode}`.
- `src/POS.Web/Components/Pages/Catalog/Price/PricesPage.razor` (9.1: list server-side + filter + Export) + `PriceSetupPage.razor` (9.3 streamlined: import Excel + lưới preview sửa inline + item picker + Lưu + audit) + `Dialogs/PriceItemPickerDialog.razor`.
- `src/POS.{Application,Infrastructure}/DependencyInjection.cs`: đăng ký `IPriceService`/`IPriceRepository`.
- `docs/sql/SetupSalePrice_Save.sql` (mới): 2 TVP (`SetupSalePriceImportTVP`, `SetupSalePriceLineTVP`) + `usp_SetupSalePrice_Save`.
- `_migration/PROGRESS.md`: 9.1 + 9.3 → ✅ DONE.

**Pattern mới:** Bulk import Excel → lưới preview validate + sửa inline → đã cập nhật `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:**
- ⚠️ **PHẢI chạy `docs/sql/SetupSalePrice_Save.sql` trên RPOSMasterData** trước khi dùng 9.3. SP mới ủy quyền phần update cho SP legacy `[dbo].[Setup_SalePrice_Get_ALL]` (phải tồn tại sẵn) — chỉ tự INSERT Pkey mới (Counter=MAX+1, defaults VND/VAT/disc/MinQty=1/VariantCode='').
- **Pkey của 9.3 (SetupPrice) = `{SalesType}-{ItemNo}-{UOM}-{SalesCode}`** — KHÁC 9.2 (PriceController: `{ItemNo}-{UOM}-{SalesCode}-{StartDate:yyyyMMdd}`). Đừng nhầm khi làm 9.2.
- Tên bảng vật lý CentralMD: `SalesPrice` (số ít), `Barcodes` (số nhiều), `Item`, `ItemUnitOfMeasure`.
- SalesCode hiện chỉ Store/ALL (bỏ Region/Channel). 9.2 + StorePriceGroup + inline edit/delete = còn TODO.

**appsettings sync:** không thay đổi appsettings.

---

## [2026-07-01] UI polish + tài liệu luồng Duyệt — Cài đặt CTKM (PromotionSetupPage)

**Layer:** POS.Web
**Loại:** Refactor (UI polish, markup-only) + Tài liệu

**Thay đổi:**
- `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` (editor mode, **chỉ markup — giữ 100% `@code`**):
  - MudTabs: `Outlined` + `SliderColor="Color.Primary"` → icon tabs có gạch chân dưới tab active.
  - Gom nhóm cả 5 tab bằng `MudCard` (CardHeader avatar + title + caption + help tooltip); bảng Buy/Get/Site bọc trong card với MudTable `Elevation="0"`.
  - Tooltip + `HelperText` giải thích field khó; tooltip cột "Loại chiết khấu"/"Giá trị CK"; tooltip điều kiện AND/OR.
  - Validation trực quan: `Required`/`RequiredError` (Tên/Loại/Hình thức bán/Từ-Đến ngày) — KHÔNG chặn Save.
  - Nút Lưu có spinner khi `_saving`; ô "Điều kiện" (Buy/Get) `max-width` 160→240px.
- `docs/web/LOGIC_APPROVE_CTKM.md` (mới): tài liệu kỹ thuật luồng "Duyệt CTKM" UI→Service→Repo→SP `usp_SetupPromotion_Approve`→publish `Setup_Promotion_Insert`→`Offer*`; kèm bảng mapping `SetupPromotion*`→`Offer*`.

**Pattern mới:** Polish thân thiện End-user (MudCard + tooltip + `Required` visual + nút loading v9) → đã cập nhật `.claude/skills/web/ui-migrate-legacy.md` §8.

**Lưu ý cho session sau:**
- MudBlazor v9 **không có** `MudButton Loading` — dùng `MudProgressCircular` trong content theo cờ `_saving`.
- Bọc `MudTable` trong `MudCard` phải đặt `Elevation="0"` cho table (tránh 2 lớp bóng).
- `Required`/`RequiredError` chỉ báo trực quan; validation chặn thật vẫn ở server (SaveAsync không đổi).

---

## [2026-07-01] Migrate 8.3 Danh mục Voucher (Full CRUD) + 8.4 Tra cứu Voucher phát hành

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature

**Thay đổi:**
- `docs/sql/SetupVoucher_Read.sql` / `SetupVoucher_Save.sql` / `SetupVoucher_Delete.sql`: SP mới (CentralMD) — GetList/GetDetail, TVP `VoucherLineTVP` + Save (upsert header + replace lines), Delete. **8.4 KHÔNG tạo SP** — reuse `[dbo].[GetTransCpnVchIssueList]` có sẵn trên CentralSales.
- `src/POS.Common/Dtos/Voucher/SetupVoucherDtos.cs`: DTOs 8.3 (VoucherListFilter/ListItem/Detail/Line/SaveRequest/SaveResult/FormLookup) + 8.4 (VoucherPublishedFilter/Item).
- `src/POS.Infrastructure/Repositories/CouponVoucher/`: `IVoucherRepository`/`VoucherRepository` (CentralMD, Dapper SP) + `IVoucherPublishedRepository`/`VoucherPublishedRepository` (StoreRoutedConnectionFactory per-store) + DI.
- `src/POS.Application/Features/CouponVoucher/`: `IVoucherService`/`VoucherService` (validate serial/ngày/items, item search reuse `GetProductListAsync`) + `IVoucherPublishedService`/`VoucherPublishedService` (thin) + DI.
- `src/POS.Web/.../CouponVoucher/VouchersPage.razor` (8.3 list+CRUD+Export), `VouchersPublishedPage.razor` (8.4 lookup+Export, store picker bắt buộc), `Dialogs/VoucherFormDialog.razor` + `VoucherItemPickerDialog.razor`.
- `tests/POS.ContractTests/JsonFieldContractTests.cs`: khóa field `VoucherListItemDto` + `VoucherPublishedItemDto`.
- `docs/ROLLOUT.md` §D4; `_migration/PROGRESS.md` 8.3/8.4 → ✅.

**Pattern mới:** không (bám pattern 3 lớp của Coupon 8.1/8.2 + per-store read của CentralSaleRepository) → KHÔNG cập nhật SKILLS.

**Lưu ý cho session sau:**
- ⚠️ **`IsCheckItem` NGƯỢC nghĩa giữa Voucher và Coupon:** Voucher `true`=tổng bill (KHÔNG có line), `false`=theo sản phẩm (có line). Coupon thì ngược. Đừng copy nhầm logic giữa 2 module.
- ⚠️ **ItemNo voucher = SỐ THUẦN** seed `70000001` (khác coupon `C7...`). SP chỉ `MAX` trên ItemNo thuần số (bỏ mã 'C...') — nếu không sẽ lỗi CAST như legacy `int.Parse(Max)`.
- Voucher & Coupon **dùng chung bảng `CpnVchBOMHeader`/`CpnVchBOMLine`** — phân tách bằng `NOT EXISTS CpnVchBOMIssueRule` (voucher = không có IssueRule). **Cần DBA xác nhận** quy tắc này + prefix ItemNo + filter "Loại"=ArticleType (đã đánh dấu `// TODO` trong SP).
- 8.4 cần SP `[dbo].[GetTransCpnVchIssueList]` tồn tại trên mọi server CentralSales; Resend-SAP **đã hoãn** (phase sau).

---

## [2026-07-01] Migrate 8.1 Cài đặt Coupon + 8.2 Phát hành Coupon

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature

**Thay đổi:**
- `docs/sql/SetupCoupon_Read.sql` / `SetupCoupon_Save.sql` / `SetupCoupon_Delete.sql`: SP mới (CentralMD) — GetList/GetCodes/GetDetail, 2 TVP (`CouponCodeTVP`, `CouponLineTVP`) + CheckCodesExist/SaveIssue/SaveAdvanced, Delete (guard QtyCoupon==0). Legacy dùng EF LINQ (INVENTORY ghi `sp_SetupCoupon_Get` là SAI).
- `src/POS.Common/Dtos/SetupCoupon/SetupCouponDtos.cs` + 2 contract fact.
- `src/POS.Infrastructure/Repositories/CouponVoucher/ICouponRepository`/`CouponRepository` + DI.
- `src/POS.Application/Features/CouponVoucher/ICouponService`/`CouponService` (sinh mã Auto + validate + parse Excel Import) + DI.
- `src/POS.Web/.../CouponVoucher/CouponsPage.razor` (8.1 list+xóa), `CouponIssuePage.razor` (8.2 phát hành Auto/Import + nâng cao + tab mã), `Dialogs/CouponItemPickerDialog` + `CouponAdvancedDialog`.
- `docs/web/coupon-flow.md`: tài liệu kiểm thử QA (12 điểm yếu code E1–E12).

**Pattern mới:** không → KHÔNG cập nhật SKILLS.

**Lưu ý cho session sau:**
- Sinh mã Auto ở tầng Application (C#, thay `Thread.Sleep(1)` legacy bằng offset theo index để mã duy nhất, không block). SP chỉ nhận danh sách mã qua TVP.
- Item picker tái dùng `ICentralMDRepository.GetProductListAsync` (6.1).
- Tài liệu QA `docs/web/coupon-flow.md` liệt kê điểm yếu (dual-write Advanced, audit oldValue sai, mất item ngầm, Quantity không chặn trần…) — dev nên vá dần.

---

## [2026-07-01] Fix BankPosPage/BankPosDetailDialog — sai tên bảng vật lý, SP param sai, crash circuit

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`:
  - `GetBankPOSListAsync`: SP `[dbo].[GetBankPOSList]` KHÔNG có tham số `@Export` (đã tồn tại sẵn từ
    legacy, không phải SP mới) — code cũ truyền dư 1 param → "too many arguments". Sửa lại đúng 6 param
    `@StoreNo,@TextSearch,@BankCode,@Status,@PageSize,@PageNumber`; SP trả `IsOnline`/`Status` dạng text
    tiếng Việt (IIF) và `Counter`/ngày đã format sẵn thành chuỗi — thêm `BankPOSListRow` map riêng rồi
    convert sang kiểu UI cần (xem pattern mới bên dưới).
  - `SaveBankPOSAsync`/`DeleteBankPOSAsync`: sửa tên bảng sai `dbo.POSTerminalBanks` (số nhiều — thực ra
    là tên EF DbSet) → `dbo.POSTerminalBank` (tên bảng vật lý thật, xác minh qua legacy EDMX).
  - `GetBankListForDropdownAsync`: sửa tên bảng sai `dbo.Banks` → `dbo.Bank` (cùng lỗi class với trên).
- `src/POS.Common/Dtos/CentralMD/BankPOSDto.cs`: `BankPOSListDto` thêm `StoreName`, `StatusText`;
  `Counter`/`CreatedDateStr`/`UpdatedDateStr` đổi sang `string?` (khớp kiểu SP thực trả) — giữ `Status`
  là `int` (không đổi) để form Edit round-trip đúng kiểu khi Save.
- `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosPage.razor`: `LoadDataAsync` — bỏ
  `Task.WhenAll` + 1 try/catch chung → await + try/catch riêng từng nguồn (BankPOS list / Store list /
  Bank list) để 1 nguồn lỗi không xoá luôn dropdown Cửa hàng/Ngân hàng; cột "Cửa hàng" hiển thị thêm
  `StoreName`; Excel export cập nhật theo DTO mới.
- `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosDetailDialog.razor`: `OnInitializedAsync` —
  cùng sửa như trên (await + try/catch riêng từng task) vì exception chưa bắt trong lifecycle method
  của dialog làm SẬP LUÔN circuit Blazor Server (không chỉ riêng dialog) → Lưu/Hủy không gọi được nữa,
  console chỉ thấy lỗi phụ `mudResizeListener.js: Cannot send data if the connection is not in the
  'Connected' State` (JS interop đầu tiên bắn ra sau khi circuit đã chết, không phải nguyên nhân gốc).

**Pattern mới:**
- Xác minh tên bảng vật lý qua legacy EDMX (SSDL, không phải CSDL/DbSet pluralized) trước khi viết raw
  SQL nhắm bảng cũ → đã cập nhật `.claude/skills/api/SKILLS.md`
- Map SP trả cột đã format/localize sẵn (khác kiểu bảng vật lý) qua row riêng rồi convert → đã cập nhật
  `.claude/skills/api/SKILLS.md`
- Load nhiều nguồn độc lập trong `OnInitializedAsync` (page lẫn dialog) — await + try/catch riêng từng
  task để tránh crash circuit → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `BankPOSListDto.PartnerId` vẫn khai báo trên DTO nhưng SP gốc người dùng cung cấp KHÔNG SELECT cột
  này — user đã đồng ý tự thêm `B.[PartnerId]` vào SELECT của SP khi deploy; code đã map sẵn field này,
  không cần sửa gì thêm khi SP được cập nhật.
- DB DEV (`RPOSMasterData`) thiếu nhiều SP khác không liên quan BankPOS (`usp_SpecialCombo_GetList`,
  `SP_SALES_BY_STORE_BUSSINESS_DATE`, `GET_REVENUE_ORDER_SALES_BY_STAFF`, `dbo.OptionData`) — user tự
  đồng bộ DB, không phải việc sửa code.
- Khi debug lỗi tương tự (page/dialog "im lặng" không thao tác được, console chỉ có lỗi JS interop
  chung chung) → luôn kiểm tra `D:\ROOT\Logs\POS.Web\Exception\log-{yyyyMMdd}.txt` trước, đây là cách
  nhanh nhất tìm exception gốc thay vì đoán từ console browser.

---

## [2026-07-01] Fix Sidebar (MainLayout) — accordion collapse sai + active highlight trùng

**Layer:** POS.Web
**Loại:** Bug fix

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`:
  - `UpdateExpanded()`: bổ sung 2 route bị thiếu trong điều kiện `Contains(...)` — `/catalog/pos-setup`
    (thiếu trong `_expandCatPos`) và `/catalog/stores` (thiếu trong `_expandCatOrg`). Vì toàn bộ state
    accordion được tính lại từ URI mỗi lần navigate (không giữ trạng thái cũ), thiếu 1 route khiến
    điều hướng tới route đó không match nhánh nào — accordion sụp về false ở MỌI cấp, nhìn như "chọn
    menu thì tất cả menu bị thu lại".
  - Thêm `Match="NavLinkMatch.All"` vào toàn bộ `MudNavLink` (kể cả các link đang comment) — mặc định
    `NavLinkMatch.Prefix` khiến route ngắn (`/promotion/coupons`) bị đánh dấu active luôn khi đang ở
    route dài hơn cùng tiền tố (`/promotion/coupons/issue`), gây 2 leaf link cùng sáng active. Cùng
    lỗi class cũng ảnh hưởng nhóm `/store/revenue*` (chưa được user báo cáo nhưng đã fix luôn).

**Pattern mới:** đã cập nhật ví dụ + anti-pattern trong section "Sidebar nav (MainLayout) — 3 cấp" của
`.claude/skills/web/SKILLS.md` (thêm `Match="NavLinkMatch.All"` vào code mẫu + cảnh báo thiếu route).

**Lưu ý cho session sau:**
- Mỗi khi thêm `MudNavLink` mới vào sidebar, BẮT BUỘC thêm route đó vào đúng điều kiện `Contains(...)`
  tương ứng trong `UpdateExpanded()` — nếu không sẽ tái diễn lỗi accordion collapse toàn bộ.
- `_expandCatPay`/`_expandCatMisc` là dead code (markup 2 group tương ứng đang bị comment) — chưa dọn,
  để nguyên vì không ai yêu cầu, không ảnh hưởng hành vi.

---

## [2026-06-30] Migrate 6.4 — Product Lock (Khóa/Mở khóa sản phẩm theo cửa hàng)

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Feature (migrate VCM.BLUEPOS 6.4 — Central mode only)

**Thay đổi:**
- `src/POS.Common/Dtos/CentralMD/ProductLockDto.cs` (MỚI): 3 DTO — `ProductLockItemDto`, `ProductLockFilter`, `ProductLockSaveDto`
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: +2 method: `GetProductLockListAsync`, `SaveProductLockAsync`
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement 2 methods — JOIN Item+ItemBlock server-side paging; UPSERT `dbo.ItemBlock` trong transaction (Pkey=`"{StoreNo}-{ItemNo}"`)
- `src/POS.Web/Components/Pages/Catalog/Product/ProductLockPage.razor`: replace skeleton → full page — filter (StoreNo bắt buộc, Status, ItemNo/ItemName), MudTable server-side + MultiSelection, chip màu trạng thái, toggle đơn + bulk action, `MudMessageBox @ref` confirm

**Pattern mới:** `MudMessageBox @ref` — confirm dialog đơn giản (thay `IDialogService.ShowMessageBox` không tồn tại trong MudBlazor v9) → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `dbo.ItemBlock.Pkey = "{StoreNo}-{ItemNo}"` — bắt buộc tạo đúng format khi INSERT mới.
- Direct POS DB mode và GrabFood API (6.5) OUT OF SCOPE — để sau; 6.4 chỉ Central DB.
- StoreOperator auto-select store đơn lẻ từ claim; StoreNo bắt buộc chọn trước khi load dữ liệu.
- `GetProductLockListAsync` dùng `BaseRepository.QueryAsync` (không SP, raw SQL với COUNT(*) OVER()).

---

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

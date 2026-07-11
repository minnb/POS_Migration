# Skill: Kiến trúc & Logic nền tảng POS.Web (Blazor Server + MudBlazor 9.5.0)

> **Đọc file này khi:** bắt đầu bất kỳ page/component/service nào trong `src/POS.Web/` — đây là bản
> "hiến pháp" rút gọn các LUẬT BẮT BUỘC về kiến trúc, auth, lifecycle, data access, hiệu năng, logging.
>
> **Quan hệ với các file khác** (file này là index luật, chi tiết + code mẫu ở nơi được trỏ):
> - Pattern page/service/DI đầy đủ + anti-pattern: **`SKILLS.md`** (nguồn chi tiết).
> - Theme/màu/Button/Elevation/Sidebar: **`theming.md`** + `.claude/rules/mudblazor-flat-ui.md`.
> - Form nhập liệu: **`form-input.md`**. Audit CRUD: **`audit-logging.md`**. Polish UI: **`ui-polish-standard.md`**.
>
> Khi luật ở đây và file chi tiết lệch nhau → file chi tiết thắng; sửa lại file này cho khớp trong cùng commit.

---

## 1. Core & Tech Stack

- **Framework:** .NET 10, Blazor Server, MudBlazor 9.5.0 (v9 có breaking changes — xem `charts.md`, `datatable.md`).
- **Render mode:** BẮT BUỘC `@rendermode InteractiveServer` ở mọi page/component **có tương tác**
  (thiếu → button/event bị bỏ qua).
- **Serialization:** BẮT BUỘC `Newtonsoft.Json` (`JsonConvert.*`). **TUYỆT ĐỐI KHÔNG** `System.Text.Json`
  / `JsonSerializer.*` — đồng bộ contract với POS.Api và 5.000 POS terminal.

## 2. Auth & Security

- **Bảo vệ page:** BẮT BUỘC `@attribute [Authorize(Policy = WebPolicies.XXX)]` — đúng section.
- **Roles → Policy** (nguồn `src/POS.Web/Auth/WebRoles.cs`):

  | Role | Policy | Coverage |
  |---|---|---|
  | `StoreOperator` | `StoreAndAbove` | StoreOperator + BackOffice + ITOps + SystemAdmin |
  | `BackOffice` | `BackOfficeAndAbove` | BackOffice + ITOps + SystemAdmin |
  | `ITOps` | `OpsAndAbove` | ITOps + SystemAdmin |
  | `SystemAdmin` | `AdminOnly` | chỉ SystemAdmin |

- **Phân quyền data (row-level):** chỉ **StoreOperator** bị filter theo claim `store_codes`.
  `_userStoreCodes` **rỗng = xem tất cả** (BackOffice/ITOps/SystemAdmin) → truyền `null` vào repo.
  Bỏ filter với `StoreAndAbove` = lộ data cửa hàng khác. (Chi tiết + code: `SKILLS.md` §Row-level filter.)
- **Luật SignIn (bridge token):** KHÔNG gọi `SignInAsync` trong InteractiveServer (HttpContext degraded
  trên WebSocket circuit → crash). Chuẩn: tạo one-time token lưu `IMemoryCache` (TTL 30s) →
  `Nav.NavigateTo("/account/signin/{token}", forceLoad: true)` → minimal API set cookie
  (`SignInAsync`, 8h SlidingExpiration, HttpOnly, SameSite=Strict) → redirect `/`.
- **Trang nhạy cảm (SQL Console...):** PIN gate BCrypt độc lập cookie + `try/catch/finally` đầy đủ quanh
  verify + Security headers/CSP config-driven. Xem `SKILLS.md` §"SQL Console hardening"/§"PIN gate".

## 3. Component Lifecycle & State

- **3 state bắt buộc:** `_loading` → `_errorMsg` → (empty `_isEmpty`) → Content. Xem template `SKILLS.md`.
- **`OnInitializedAsync`:** BẮT BUỘC bọc `try/catch`, `_loading = false` trong `finally`. Lỗi log
  `KibanaService.LogException("Page.OnInitialized", "", 0, "", ex.Message)`.
  > ⚠️ Exception **thoát khỏi** lifecycle method **KHÔNG chỉ crash page** — nó **sập luôn circuit
  > SignalR**, mọi tương tác sau đó (kể cả dialog đang mở) fail. Nếu load ≥2 nguồn **độc lập** →
  > **tách try/catch riêng từng nguồn** (1 nguồn lỗi không kéo sập nguồn khác). Xem `SKILLS.md`
  > §"Load nhiều nguồn độc lập".
- **Event handler async:** dùng `try/catch/finally` **đầy đủ** (không chỉ `try/finally`) — exception
  không lường trước vẫn crash circuit dù `finally` đã chạy.

## 4. Data Access & DI

- **CẤM HttpClient → POS.Api:** inject thẳng Service/Repository qua DI (POS.Web đăng ký
  `AddInfrastructure()` + `AddApplication()`). Cần chạy tác vụ của POS.Api → bọc method Application
  dùng chung, KHÔNG nhồi logic vào `.razor` (xem `SKILLS.md` §"kích hoạt tác vụ server-side qua DI").
- **CẤM raw SQL trong Razor:** mọi truy vấn qua Repository/Service.
- **Inject DB factory:** inject concrete `CentralMDConnectionFactory`/`LoyaltyConnectionFactory` —
  KHÔNG inject `IDbConnectionFactory` (interface không đăng ký trong DI).
- **Redis trong circuit:** LUÔN dùng bản `...Async` (`StringGetAsync`/`HashSetAsync`...) — các method
  sync của `IRedisService` block bằng `.GetAwaiter().GetResult()`, rủi ro treo circuit khi Redis chậm.

## 5. Hiệu năng & Anti-Crash (MudAutocomplete)

- **Giới hạn kết quả:** BẮT BUỘC `.Take(N)` (vd 50) trong `SearchFunc` **và** `MaxItems="50"`.
  `MaxItems` chỉ giới hạn **hiển thị**, KHÔNG chặn materialize toàn bộ list nghìn dòng nếu thiếu `.Take()`.
- **Chống reset-loop crash circuit:** CẤM `ResetValueOnEmptyText="true"` kết hợp `MinCharacters="0"`
  (text rỗng khi focus → reset lặp vô hạn → tear-down circuit). Dùng `Clearable="true"` cho nút xóa.
- Pattern `SearchFunc` chuẩn + biến thể multi-add: `SKILLS.md` §"Store Selector" / `filter-store.md`.

## 6. Logging & Audit (CRUD)

- **System log:** `IKibanaService.LogInfo("Page.LoadData", storeCode ?? "all", "...")` khi thành công;
  `IKibanaService.LogException("Page.Method", "", 0, "", ex.Message)` khi lỗi. **KHÔNG** log dữ liệu
  nhạy cảm (card number, password, token, PII).
- **Audit log (mọi page Create/Update/Delete):** BẮT BUỘC `await AuditLogger.LogAsync(...)` **sau khi
  DB save thành công** — KHÔNG log khi thao tác thất bại, KHÔNG quên `await`.
  - **Serialize** old/new value bằng **Newtonsoft.Json**.
  - **UPDATE:** dùng biến `item` đang có trên page làm `oldValue` — KHÔNG query lại DB.
  - **Form dialog:** trả DTO đầy đủ `MudDialog.Close(DialogResult.Ok(_model))` — KHÔNG `Ok(true)`
    (trang cha cần newValue để ghi audit CREATE/UPDATE).
  - Migration `src/POS.Web/Auth/migration_dashboard_audit_log.sql` phải chạy trên `RPOSMasterData`
    trước khi deploy. Chi tiết đầy đủ: `audit-logging.md`.

---

## Checklist nhanh trước khi báo "xong" 1 page

```
□ @page (dòng đầu) + @attribute [Authorize(Policy=...)] + @rendermode InteractiveServer
□ Newtonsoft.Json (không System.Text.Json)
□ 3 state _loading/_errorMsg/(empty) + OnInitializedAsync try/catch, finally _loading=false
□ Row-level filter theo store_codes nếu policy = StoreAndAbove
□ Không HttpClient→POS.Api, không raw SQL trong Razor, inject factory concrete
□ MudAutocomplete: .Take(N) + MaxItems, không ResetValueOnEmptyText+MinCharacters=0
□ CRUD: AuditLogger.LogAsync (await, sau save OK, oldValue từ item, dialog trả DTO)
□ UI theo LUẬT THÉP mudblazor-flat-ui.md (Button/Elevation/Sidebar/KPI card v3)
```

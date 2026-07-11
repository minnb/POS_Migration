# POS.Web — Security Audit Checklist (chuẩn bị public Internet)

> Audit ngày **2026-07-11** trên `src/POS.Web` + `src/POS.Infrastructure`, đối chiếu
> `docs/web/security/security.md` (Quy định bảo mật) + `docs/web/security/roles.md` (Vai trò &
> Phân quyền). Đây là checklist fix dần — tick `[x]` khi đã vá + verify. Mỗi mục có bằng chứng
> `file:dòng` và hướng fix. **CHƯA VERIFY** = cần DBA/DevOps xác nhận, chưa kết luận được.

## Trạng thái tổng quan

| # | Mức | Vấn đề | Fixed? | Verified? |
|---|---|---|---|---|
| C1 | 🔴 CRITICAL | SQL Console bypass DROP/TRUNCATE qua `EXEC` + connection full-quyền | [ ] | [ ] |
| C2 | 🔴 CRITICAL | Cookie phiên qua HTTP thuần khi `RequireHttps=false` ở Prod | [ ] | [ ] |
| H1 | 🟠 HIGH | Khóa PIN race-condition (không atomic) + fail-open khi Redis lỗi | [ ] | [ ] |
| H2 | 🟠 HIGH | StoreOperator `store_codes` rỗng → thấy TẤT CẢ cửa hàng | [ ] | [ ] |
| H3 | 🟠 HIGH | Không rate-limit `/login` → brute-force credential | [ ] | [ ] |
| H4 | 🟠 HIGH | ForwardedHeaders tin mọi proxy → giả mạo `X-Forwarded-*` | [ ] | [ ] |
| M1 | 🟡 MEDIUM | Thiếu Fallback Authorization Policy | [ ] | [ ] |
| M2 | 🟡 MEDIUM | RevenueByStaff/ByStore fallback all-stores khi tra store fail | [ ] | [ ] |
| M3 | 🟡 MEDIUM | `DetailedErrors` bật được ở Prod qua config | [ ] | [ ] |
| L1 | 🟢 LOW | Bridge token dùng `Guid.NewGuid()` thay CSPRNG | [ ] | [ ] |
| L2 | 🟢 LOW | Bridge token nằm trên URL path (history/access-log) | [ ] | [ ] |
| L3 | 🟢 LOW | Sai lệch doc↔code (policy/route/`login-flow.md` lỗi thời) | [ ] | [ ] |

---

## 🔴 CRITICAL

### [ ] C1 — SQL Console: bypass DROP/TRUNCATE qua `EXEC` + chạy full-quyền
- **Bằng chứng:** `src/POS.Web/Services/SqlConsoleService.cs:88-96` chỉ chặn node `Drop*`/`Truncate*`;
  `EXEC('DROP TABLE X')` parse thành `ExecuteStatement` → `default: hasOther=true` (`:135-137`) →
  `Other` → **được chạy**. Chạy bằng connection app đầy đủ (`:335`), không có login read-only.
- **Fix:**
  - [ ] Chuyển `Validate` sang **allowlist** — chỉ cho các case đã liệt kê; chặn `ExecuteStatement`
    (EXEC/sp_executesql) và mọi thứ rơi vào `default`.
  - [ ] (DBA) Tạo SQL login riêng **quyền tối thiểu** cho SQL Console, connection string riêng.
  - [ ] Cân nhắc allowlist IP/VPN cho `/admin/sql-console` kể cả sau khi public.
- **Verify:** `Validate("EXEC('DROP TABLE X')")` trả `Ok=false`; xác nhận connection là login hạn quyền.
- **CHƯA VERIFY:** `xp_cmdshell` có bật trên SQL Server không (cần DBA) — nếu bật, mức độ cao hơn (RCE).

### [ ] C2 — Cookie phiên đi qua HTTP thuần ở Production
- **Bằng chứng:** `src/POS.Web/Program.cs:60` `secureCookies = requireHttps && !IsDevelopment()`;
  `:131-133` `SecurePolicy = secureCookies ? Always : SameAsRequest`. `RequireHttps=false` ở Prod →
  cookie không `Secure`, không HSTS/redirect (`:204-210`) → session hijack qua Internet.
- **Fix:**
  - [ ] Go-live gate: `Security:RequireHttps=true` + TLS thật ở proxy trước khi mở Internet.
  - [ ] Thêm assertion khởi động: `!IsDevelopment && !RequireHttps` → **fail fast** (không cho start).
  - [ ] `BehindProxy`: proxy set `X-Forwarded-Proto: https` (liên quan H4).
- **Verify:** start với `IsProduction + RequireHttps=false` → fail fast; với `true` → cookie có
  `Secure` + header `Strict-Transport-Security`.
- **CHƯA VERIFY:** giá trị `Security:RequireHttps` thực tế ở Prod (cần DevOps).

---

## 🟠 HIGH

### [ ] H1 — Khóa PIN SQL Console: race-condition + fail-open
- **Bằng chứng:** `src/POS.Web/Auth/WebUserService.cs:200` `StringGetAsync`, `:245-246`
  `attempts++` + `StringSetAsync` — read-then-write không atomic → N request song song chỉ tăng ~1.
  Redis lỗi → default `0` → fail-open (không khóa).
- **Fix:**
  - [ ] Thêm `StringIncrementAsync` (atomic `INCR`) vào `IRedisService`/`RedisManager`, dùng `INCR`
    rồi so sánh; set TTL ở lần tăng đầu.
  - [ ] Fail-closed khi Redis lỗi (từ chối verify PIN nếu không đọc/ghi được counter).
- **Verify:** bắn N request PIN sai song song → counter Redis đạt đúng N, khóa sau đúng 5 lần.

### [ ] H2 — StoreOperator `store_codes` rỗng → thấy TẤT CẢ cửa hàng
- **Bằng chứng:** `src/POS.Web/Components/Pages/Login.razor:98-99` claim chỉ phát khi StoreCodes khác
  rỗng; page tính `_isStoreOperator = _userStoreCodes.Count > 0` (vd `RevenuePage.razor:246`);
  `UserFormDialog.razor:244-254` cho tạo StoreOperator không store code → repo trả mọi store
  (`CentralSaleRepository.cs:408,909,1025`). Vi phạm bất biến row-level.
- **Fix:**
  - [ ] Đổi `_isStoreOperator` sang dựa **Role claim** (`User.IsInRole(StoreOperator)`) thay vì
    `_userStoreCodes.Count > 0` — áp dụng mọi page dữ liệu.
  - [ ] Guard server-side: StoreOperator mà `store_codes` rỗng → từ chối/0 dòng, không "xem tất cả".
  - [ ] Form admin: role StoreOperator bắt buộc nhập ≥1 store code.
- **Verify:** login StoreOperator không store code → không thấy store khác (0 dòng), không phải all.
- **CHƯA VERIFY:** body các SP `Rpt_GetRevenueSalesLists`, `Rpt_ReportSaleByTime`,
  `Rpt_ReportTopProduct`, `Rpt_SalesByCategory`, `Rpt_ReportSaleByPayment`, `Rpt_ReportSalesByStaff`,
  `Rpt_ReportSalesByStore` xử lý `@StoreNo` rỗng = "all" hay "rỗng" (cần đọc script SP).

### [ ] H3 — Không rate-limit `/login`
- **Bằng chứng:** không có rate limiting cho endpoint login (`Program.cs`, ABSENT). Public → brute-force.
- **Fix:**
  - [ ] Thêm ASP.NET Rate Limiter (per-IP + per-username) cho `/login`.
  - [ ] Cân nhắc lockout tạm thời + CAPTCHA sau N lần sai.
- **Verify:** > N lần login sai/phút từ 1 IP → 429/lockout.

### [ ] H4 — ForwardedHeaders tin mọi proxy mặc định
- **Bằng chứng:** `src/POS.Web/Program.cs:178-201` clear KnownProxies/KnownIPNetworks, không cấu hình
  → tin `X-Forwarded-*` mọi nguồn (chỉ `LogWarning`). Giả `X-Forwarded-For` (IP audit giả) +
  `X-Forwarded-Proto` (scheme confusion).
- **Fix:**
  - [ ] Prod bắt buộc cấu hình `Security:KnownProxies`/subnet reverse proxy.
  - [ ] Fail-fast / từ chối forwarded headers nếu chưa cấu hình proxy tin cậy.
- **Verify:** spoof `X-Forwarded-For` từ nguồn lạ không thay đổi IP audit.

---

## 🟡 MEDIUM

### [ ] M1 — Thiếu Fallback Authorization Policy
- **Bằng chứng:** `src/POS.Web/Program.cs:152-162` chỉ đăng ký 4 policy, không `FallbackPolicy`. Page
  tương lai quên `[Authorize]` sẽ anonymous. (Hiện 68 route đều có `[Authorize]` — chỉ là rủi ro tương lai.)
- **Fix:** [ ] `options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()`.
- **Verify:** tạo page test không `[Authorize]` → vẫn đòi đăng nhập.

### [ ] M2 — RevenueByStaff/ByStore fallback all-stores khi tra store fail
- **Bằng chứng:** `RevenueByStaffPage.razor:264-330`, `RevenueByStorePage.razor:275-391`: nếu
  `myStore==null` (store bị block/đổi tên) → `storeNo=""`/serialize toàn bộ store.
- **Fix:** [ ] StoreOperator không resolve được store của mình → lỗi/0 dòng, không fallback all
  (gộp với H2: dựa Role + dùng trực tiếp `store_codes`).
- **Verify:** StoreOperator có store bị Blocked → không xem all-store.

### [ ] M3 — `DetailedErrors` bật được ở Prod
- **Bằng chứng:** `src/POS.Web/Program.cs:80-81` `DetailedErrors = IsDevelopment() || config
  WebApp:EnableDetailedErrors`. Default an toàn; cờ config bật ở Prod → lộ stack trace.
- **Fix:** [ ] Ràng buộc cờ chỉ hiệu lực ngoài Prod, hoặc bỏ đường bật ở Prod.
- **Verify:** Prod + `EnableDetailedErrors=true` → vẫn không lộ chi tiết lỗi ra client.

---

## 🟢 LOW

### [ ] L1 — Bridge token dùng `Guid.NewGuid()` thay CSPRNG
- **Bằng chứng:** `src/POS.Web/Components/Pages/Login.razor:105` `Guid.NewGuid().ToString("N")`.
  Giảm nhẹ mạnh bởi single-use + TTL 30s + cache in-process.
- **Fix:** [ ] `RandomNumberGenerator.GetBytes(32)` → hex/base64url.

### [ ] L2 — Bridge token nằm trên URL path
- **Bằng chứng:** `Login.razor:107` `/account/signin/{token}` → browser history + có thể vào access-log.
- **Fix (tùy chọn):** [ ] dùng cookie tạm/POST thay path; hoặc đảm bảo access-log không ghi path này.

### [ ] L3 — Sai lệch doc↔code
- **Bằng chứng:** `bank-pos`=`OpsAndAbove` (doc `roles.md §5.2` ghi `BackOfficeAndAbove`);
  `price-groups`=`OpsAndAbove` lệch sibling; route drift `/ops/activity-log` vs doc; `login-flow.md`
  còn nói "3 Roles" + "StoreCodes chưa enforce". Code nghiêm hơn doc (fail-safe) → không leo thang.
- **Fix:** [ ] Cập nhật doc cho khớp code (KHÔNG hạ policy code theo doc).

---

## Ghi chú CHƯA VERIFY (cần làm rõ trước khi kết luận "đã đóng")
- [ ] Config Prod thực tế: `Security:RequireHttps`, `Security:KnownProxies`, `WebApp:EnableDetailedErrors` (DevOps).
- [ ] Định nghĩa SP `Rpt_*`/`usp_*` xử lý `@StoreNo` rỗng (đọc script DB) — quyết định mức cuối H2/M2.
- [ ] `xp_cmdshell` có bật trên SQL Server không (DBA) — ảnh hưởng mức C1.

## Verify chung sau mỗi lần fix
```powershell
dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly   # 0 error
dotnet test tests/POS.ContractTests -nologo                        # xanh
```

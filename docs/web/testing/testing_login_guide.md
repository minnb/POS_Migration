# Hướng dẫn test trang Login POS.Web bằng Playwright (skill `webapp-testing`)

> Test end-to-end trang `/login` của POS.Web bằng browser thật (Chromium headless). Bổ sung cho
> xUnit `tests/POS.ContractTests` / `tests/POS.UnitTests` (test logic, KHÔNG render UI).
> Skill nguồn: `.claude/skills/webapp-testing/`. Script demo: `tests/POS.Web.UiTests/smoke_login.py`.

## 0. Tiền đề (kiểm tra 1 lần)

| Thành phần | Kiểm tra | Đã verify trên máy dev |
|---|---|---|
| Python 3.x | `python --version` | ✅ 3.13.12 |
| Playwright (Python) | `python -m pip show playwright` | ✅ 1.61.0 |
| Chromium cho Playwright | thư mục `%USERPROFILE%\AppData\Local\ms-playwright\chromium-*` | ✅ chromium-1228 |
| .NET SDK | `dotnet --version` | ✅ 10.0.300 |

Nếu **thiếu** Playwright / Chromium (máy khác), cài 1 lần:
```powershell
python -m pip install playwright
python -m playwright install chromium
```
> Lưu ý môi trường: npm/npx bị chặn TLS công ty, nhưng `pip install playwright` + `playwright
> install chromium` dùng CDN riêng nên chạy được. Nếu `pip` cũng bị chặn → tải Chromium thủ công
> hoặc báo IT mở proxy.

Vì sao chọn `/login` để test: `src/POS.Web/Components/Pages/Login.razor` là `[AllowAnonymous]`,
**không load dữ liệu lúc init** (chỉ chạm DB khi bấm submit), và Redis kết nối lazy
(`RedisManager` dùng `Lazy<ConnectionMultiplexer>`) → trang render được **không cần Redis/SQL sống**.

---

## 1. Bước 1 — Đảm bảo POS.Web đang chạy ở cổng 5170

Trang chạy ở `http://localhost:5170` (profile `http`, xem
`src/POS.Web/Properties/launchSettings.json`).

**Kiểm tra đã có tiến trình giữ cổng 5170 chưa:**
```powershell
Get-NetTCPConnection -LocalPort 5170 -State Listen -ErrorAction SilentlyContinue |
  ForEach-Object { "PID={0} Proc={1}" -f $_.OwningProcess, (Get-Process -Id $_.OwningProcess).ProcessName }
```

- **Có** dòng `Proc=POS.Web` → server đã chạy sẵn (vd bật trong IDE) → sang **Cách A** (Bước 2A).
- **Trống** → server chưa chạy → dùng **Cách B** (Bước 2B) để tự khởi động.

Xác nhận server phản hồi trang login:
```powershell
(Invoke-WebRequest http://localhost:5170/login -UseBasicParsing -TimeoutSec 15).StatusCode  # kỳ vọng 200
```

---

## 2A. Bước 2A — Chạy test khi server ĐÃ chạy sẵn (khuyến nghị)

Từ gốc repo:
```powershell
python tests/POS.Web.UiTests/smoke_login.py
```

## 2B. Bước 2B — Chạy test khi server CHƯA chạy (helper tự start/stop `dotnet run`)

Helper `with_server.py` sẽ khởi động `dotnet run`, chờ cổng 5170 sẵn sàng, chạy script, rồi tắt
server. **Chỉ dùng khi cổng 5170 còn trống** (nếu đã có tiến trình giữ cổng → helper sẽ báo
`address already in use` → quay lại Cách A):
```powershell
python .claude/skills/webapp-testing/scripts/with_server.py `
  --server "dotnet run --project src/POS.Web/POS.Web.csproj --launch-profile http" --port 5170 `
  --timeout 240 `
  -- python tests/POS.Web.UiTests/smoke_login.py
```
> `--timeout 240`: lần `dotnet run` đầu phải **build** nên có thể lâu; helper tự chờ tới khi cổng mở.

---

## 3. Bước 3 — Đọc kết quả (bằng chứng)

Kết quả kỳ vọng ở stdout (exit code 0):
```
RESULT: PASS - title chứa 'Đăng nhập' (title='Đăng nhập – Dashboard')
RESULT: PASS - nút 'Đăng nhập' hiển thị
RESULT: PASS - ô 'Tên đăng nhập' hiển thị
SCREENSHOT: ...\tests\POS.Web.UiTests\artifacts\login.png
SUMMARY: 3/3 passed
```

- Mỗi assertion in `RESULT: PASS/FAIL`. Có bất kỳ FAIL → **exit code != 0** (tích hợp CI được).
- Ảnh chụp toàn trang: `tests/POS.Web.UiTests/artifacts/login.png` — mở xem để xác nhận trực quan
  (icon POS, ô "Tên đăng nhập"/"Mật khẩu", nút "Đăng nhập").

---

## 4. Kịch bản test đang phủ

Script `tests/POS.Web.UiTests/smoke_login.py` (luồng anonymous, không cần credential/DB):

1. `goto http://localhost:5170/login` → `wait_for_load_state('networkidle')` (chờ Blazor circuit
   render xong — **bắt buộc** với app động, xem `.claude/skills/webapp-testing/SKILL.md`).
2. Assert `page.title()` chứa `"Đăng nhập"` (khớp `<PageTitle>Đăng nhập – Dashboard</PageTitle>`).
3. Assert nút `role=button name="Đăng nhập"` hiển thị.
4. Assert ô `label="Tên đăng nhập"` hiển thị.
5. `page.screenshot(full_page=True)` → `artifacts/login.png`.

---

## 5. Xử lý sự cố (đã gặp thật khi dựng demo)

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| `with_server.py` timeout + log `address already in use` cổng 5170 | POS.Web đã chạy sẵn giữ cổng | Không dùng helper — chạy thẳng script (Cách A) |
| `UnicodeEncodeError: 'charmap' codec ...` | Console Windows mặc định cp1252, không in được tiếng Việt | Script đã tự `sys.stdout.reconfigure(encoding="utf-8")`; nếu tự viết script mới, thêm dòng này, hoặc chạy `python -X utf8 ...` |
| `net::ERR_CONNECTION_REFUSED` | Server chưa chạy / sai cổng | Làm Bước 1; đảm bảo profile `http` (5170), không phải `https` (7200) |
| Assertion FAIL ở title/nút | Trang login đổi markup/text | Cập nhật selector/text kỳ vọng trong `smoke_login.py` cho khớp `Login.razor` |

---

## 6. Test FULL login (đăng nhập thật vào dashboard)

Script: `tests/POS.Web.UiTests/smoke_login_full.py` — điền credential, submit, xác nhận vào
dashboard (điều hướng khỏi `/login`, sidebar hiển thị tên user + nút Đăng xuất). Luồng này
**chạm DB thật** qua `WebUserService.ValidateLoginAsync` (query `DashboardUsers`, `CentralMD`).

### Tiền đề DB (đã có sẵn — KHÔNG cần cấu hình thêm)

- Connection string đã cấu hình: `ConnectionStrings:CentralMD` trong `src/POS.Web/appsettings.json`
  → `RPOSMasterData` (server `10.235.55.122\DRW`). Login query đúng connection này.
- Tài khoản seed mặc định: `admin` / `Admin@0987` (role SystemAdmin) — seed trong
  `src/POS.Web/Database/Migrations/001_DashboardUsers.sql`. Tài khoản khác → tạo bằng `/web-ops gen-hash`
  (sinh BCrypt hash) rồi INSERT vào `DashboardUsers`.

### Chạy (credential qua biến môi trường, KHÔNG hardcode secret)

```powershell
$env:POSWEB_TEST_USER = "admin"
$env:POSWEB_TEST_PASS = "Admin@0987"
python tests/POS.Web.UiTests/smoke_login_full.py
Remove-Item Env:\POSWEB_TEST_PASS
```
> Không set env → script mặc định về seed `admin`/`Admin@0987`.

### Kết quả kỳ vọng (đã verify 2026-07-16, exit code 0)

```
INFO: submitting login as user='admin'
INFO: final_url = http://localhost:5170/ops/health
RESULT: PASS - điều hướng khỏi /login sau submit
RESULT: PASS - sidebar hiển thị user đã đăng nhập (user='admin')
RESULT: PASS - nút Đăng xuất (/logout) hiển thị
SUMMARY: LOGIN OK
```
Ảnh: `tests/POS.Web.UiTests/artifacts/login_full.png` (dashboard `/ops/health` đã xác thực).

Cơ chế bridge-token: submit → `/account/signin/{token}` (`forceLoad`) → set cookie auth → redirect
`/` → landing `/ops/health`. Playwright đi theo chuỗi redirect này tự nhiên.

### Nền tảng để test tiếp các case khác

Từ đây có thể mở rộng: sau khi login thành công, `page` đã giữ cookie phiên → điều hướng tới bất kỳ
trang nào (`/store/...`, `/ops/...`, `/admin/...`) và assert nội dung. Tách bước login thành 1 hàm
`login(page)` dùng lại cho mọi script test sau. Luôn đọc credential từ env, KHÔNG commit secret.

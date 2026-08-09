# Hướng dẫn test trang Phát hành Coupon POS.Web bằng Playwright (skill `webapp-testing`)

> Test end-to-end trang `/promotion/coupons/issue` (`CouponIssuePage.razor`) bằng browser thật
> (Chromium headless). Cùng khuôn với `docs/web/testing/testing_login_guide.md` (trang Login) — bổ sung
> cho xUnit `tests/POS.ContractTests` / `tests/POS.UnitTests` (test logic, KHÔNG render UI).
> Script: `tests/POS.Web.UiTests/smoke_coupon_issue.py`. Kịch bản gốc + edge case đầy đủ:
> `docs/web/testing/coupon-flow.md` (đọc trước — đã cập nhật 2026-07-16 đối chiếu với code thật).

## 0. Tiền đề (giống hệt `testing_login_guide.md` — kiểm tra 1 lần)

| Thành phần | Kiểm tra | Đã verify trên máy dev |
|---|---|---|
| Python 3.x | `python --version` | ✅ |
| Playwright (Python) | `python -m pip show playwright` | ✅ |
| Chromium cho Playwright | thư mục `%USERPROFILE%\AppData\Local\ms-playwright\chromium-*` | ✅ |
| .NET SDK | `dotnet --version` | ✅ |

**Khác biệt quan trọng so với test Login**: trang Login không chạm DB lúc init; trang Phát hành
Coupon **CHẠM DB THẬT** ngay từ `OnInitializedAsync` (`CouponService.GetFormLookupAsync`) và **LƯU
DỮ LIỆU THẬT** (không dry-run, không rollback) — mỗi lần chạy script tạo ra 1 coupon Auto thật
dùng được ở POS. Xem mục 6 "Dữ liệu để lại sau khi test".

**Pre-condition riêng cho trang này** (xem `docs/web/testing/coupon-flow.md` mục 1):
- Đã deploy 3 SP `usp_SetupCoupon_Read/Save/Delete` trên `RPOSMasterData` (xem `docs/ROLLOUT.md`
  §D3). Thiếu → script tự phát hiện banner đỏ và dừng sớm với thông báo rõ ràng (không đoán mò).
- Đăng nhập bằng role **BackOffice**, **ITOps** hoặc **SystemAdmin** (policy
  `WebPolicies.BackOfficeAndAbove`) — StoreOperator bị chặn.
- **KHÔNG cần** seed sẵn `SalesOrderType`/`StoreGroup`/`UnitOfMeasure`/`Item` — script cố tình giữ
  "Hình thức bán hàng"/"Nhóm cửa hàng" ở giá trị mặc định `ALL` (hardcode sẵn trong dropdown, không
  phụ thuộc dữ liệu DB) và không tick "Áp dụng theo danh sách sản phẩm" ở kịch bản positive chính
  → tránh hoàn toàn phụ thuộc master data không có sẵn script seed trong repo.

---

## 1. Bước 1 — Đảm bảo POS.Web đang chạy ở cổng 5170

Giống hệt `testing_login_guide.md` mục 1 — kiểm tra tiến trình giữ cổng 5170:
```powershell
Get-NetTCPConnection -LocalPort 5170 -State Listen -ErrorAction SilentlyContinue |
  ForEach-Object { "PID={0} Proc={1}" -f $_.OwningProcess, (Get-Process -Id $_.OwningProcess).ProcessName }
```
- **Có** `Proc=POS.Web` → server đã chạy sẵn → Cách A (mục 2).
- **Trống** → Cách B, dùng `with_server.py` (xem `testing_login_guide.md` mục 2B — cú pháp y hệt,
  chỉ đổi script gọi ở cuối lệnh).

---

## 2. Bước 2 — Chạy test

**Cách A (server đã chạy sẵn):**
```powershell
python tests/POS.Web.UiTests/smoke_coupon_issue.py
```

**Cách B (server chưa chạy):**
```powershell
python .claude/skills/webapp-testing/scripts/with_server.py `
  --server "dotnet run --project src/POS.Web/POS.Web.csproj --launch-profile http" --port 5170 `
  --timeout 240 `
  -- python tests/POS.Web.UiTests/smoke_coupon_issue.py
```

Credential qua biến môi trường (mặc định `admin`/`Admin@0987` — SystemAdmin, seed sẵn):
```powershell
$env:POSWEB_TEST_USER = "admin"
$env:POSWEB_TEST_PASS = "Admin@0987"
python tests/POS.Web.UiTests/smoke_coupon_issue.py
Remove-Item Env:\POSWEB_TEST_PASS
```

---

## 3. Kết quả kỳ vọng (đã verify thật 2026-07-16, exit code 0)

```
RESULT: PASS - đăng nhập thành công (url=http://localhost:5170/ops/health)
RESULT: PASS - nút 'Phát hành coupon' hiển thị trên list
RESULT: PASS - điều hướng sang trang Phát hành Coupon (title='Phát hành Coupon – POS Dashboard')
RESULT: PASS - dialog 'Phát hành mã coupon' mở sau khi bấm Lưu
RESULT: PASS - sau Lưu điều hướng về /promotion/coupons (KHÔNG reload tại chỗ) (url=...)
RESULT: PASS - coupon vừa tạo xuất hiện trong list (filter theo Từ khóa) (số dòng khớp=1)
RESULT: PASS - mở 'Xem chi tiết' hiển thị đúng tiêu đề (title=' Xem coupon C70000019')
RESULT: PASS - chế độ Xem: nút 'PHÁT HÀNH THÊM' hiển thị
RESULT: PASS - chế độ Xem: nút 'Lưu' KHÔNG hiển thị (chưa đổi Blocked)
RESULT: PASS - chế độ Sửa (URL trực tiếp): tiêu đề đúng (title=' Sửa coupon C70000019')
RESULT: PASS - chế độ Sửa: 'Cách phát hành' bị khóa (CodeFieldsLocked)
RESULT: PASS - chế độ Sửa: tab 'Mã coupon đã phát hành' tồn tại
RESULT: PASS - tab mã coupon hiển thị 5 mã vừa phát hành (số dòng=5)
RESULT: PASS - TC-N01 bỏ trống Tên phát hành
RESULT: PASS - TC-N03 Từ ngày >= Đến ngày
RESULT: PASS - TC-N03b giá trị giảm giá % ngoài khoảng 0-100
RESULT: PASS - TC-N14 tick sản phẩm nhưng danh sách rỗng
SUMMARY: ALL PASSED
```
Ảnh chụp từng bước: `tests/POS.Web.UiTests/artifacts/coupon_issue_NN_*.png` (11 ảnh).

- Mỗi assertion in `RESULT: PASS/FAIL` — bất kỳ FAIL nào → exit code != 0.
- Có `RESULT: FAIL` → mở đúng ảnh `coupon_issue_NN_*.png` tương ứng thứ tự bước để xem trạng thái
  UI thật lúc đó.

---

## 4. Kịch bản đang phủ (map sang `coupon-flow.md`)

| Bước script | TC-ID trong `coupon-flow.md` |
|---|---|
| List → click "Phát hành coupon" → verify tiêu đề | Điều hướng tạo mới (mục 4/5) |
| Tạo coupon Auto hợp lệ (điền Tên + Giá trị giảm giá + dialog sinh mã) | **TC-I01** (Expected Result đã sửa — điều hướng về list, không reload tại chỗ) |
| Filter list theo Từ khóa → verify xuất hiện | TC-L01/TC-L02 (bản rút gọn, chỉ verify filter "Từ khóa") |
| Click "Xem chi tiết" → verify field khóa + nút PHÁT HÀNH THÊM/Lưu | **TC-L07**, **TC-I07** |
| Điều hướng trực tiếp `?id=...` (không `mode=view`) → verify tiêu đề "Sửa coupon", field khóa, tab mã | **TC-L06** (đã sửa — không còn icon Sửa), **TC-I05**, **TC-I06** |
| Bỏ trống Tên phát hành → Lưu | **TC-N01** |
| Từ ngày ≥ Đến ngày (chỉnh qua calendar picker) → Lưu | **TC-N03** (text đã sửa) |
| Giá trị giảm giá = 0 (mặc định) → Lưu | **TC-N03b** (mới, gộp N16/N17 cũ) |
| Tick "Áp dụng theo danh sách sản phẩm" không thêm sản phẩm → Lưu | **TC-N14** |

---

## 5. Giới hạn / KHÔNG phủ (đọc kỹ trước khi coi CI xanh = "đã test hết")

Đã chốt phạm vi với người dùng: **positive flow đầy đủ + 4 case negative trọng điểm**. KHÔNG tự
động hoá (vẫn cần test thủ công theo `coupon-flow.md`):

- **TC-I02** (Import Excel) — cần file `.xlsx` thật, ngoài phạm vi.
- **TC-I03** (Item picker đầy đủ — thêm/xóa nhiều sản phẩm) — cần dữ liệu `Item` không có seed
  script sẵn trong repo.
- **TC-I04** (Advanced dialog) — **KHÔNG THỂ test qua UI** dù có muốn: `_showAdvancedButton=false`
  hardcoded, nút không bao giờ render (xem `coupon-flow.md` TC-I04 đã sửa).
- **TC-I08** (nút "PHÁT HÀNH THÊM" — phát hành thêm 1 lô mã cho coupon đã tồn tại) — script hiện
  KHÔNG bấm nút này (chỉ verify nó *hiển thị* ở chế độ Xem) để tránh tạo thêm mã ngoài dự kiến;
  test thủ công theo `coupon-flow.md` TC-I08 nếu cần phủ đầy đủ.
- **TC-N02, N04, N05, N07-N13** — validate còn lại của luồng Auto/Import (độ dài mã, trùng mã DB,
  file Excel lỗi...) — không nằm trong 4 case đã chọn, xem `coupon-flow.md` mục 5.2 để test tay.
- **TC-N06, TC-N15, TC-N18, TC-N19** — **xác nhận qua chạy thật KHÔNG tái hiện được qua UI**:
  - TC-N06 (Số lượng ≤ 0): `MudNumericField Min="1"` tự clamp về 1 trước khi submit — dialog
    KHÔNG chặn, coupon tạo thành công với Quantity=1 (không phải lỗi).
  - TC-N15/N18/N19: phụ thuộc dialog "Cài đặt nâng cao" đã ẩn (xem TC-I04).
- **12+1 điểm yếu E1-E13** (`coupon-flow.md` mục 6) — đều là edge case cần môi trường/thao tác đặc
  biệt (nhiều tab đồng thời, ngắt mạng, dữ liệu StoreGroup rỗng...), không tự động hoá. **Đặc biệt
  lưu ý khi thao tác thủ công**:
  - **E9/E13**: KHÔNG bấm Lưu lại nhiều lần nếu thấy lỗi ở bước "Lưu cấu hình nâng cao thất bại" —
    xem kỹ DB trước khi thử lại, vì mỗi lần Lưu lại có thể tạo THÊM 1 coupon "rác" có mã thật (đã
    có mã, không audit đầy đủ) do bug E13.
  - **E4**: nếu test với "Nhóm cửa hàng" khác `ALL`, kiểm tra `CpnVchBOMStore` có ≥1 dòng sau khi
    Lưu — group rỗng/toàn store `Status=0` sẽ tạo coupon không áp dụng cửa hàng nào mà UI không
    báo.

---

## 6. Dữ liệu để lại sau khi test (BẮT BUỘC đọc trước khi chạy nhiều lần)

Mỗi lần chạy **tạo 1 coupon Auto THẬT** trong `RPOSMasterData`:
- `Description` = `AUTOTEST Coupon {unix_timestamp}` (duy nhất mỗi lần chạy).
- `Prefix` = `ZTST`, 5 mã Auto (`ZTST...`, độ dài 10 ký tự).
- Hiệu lực: hôm nay+1 → hôm nay+60, Giá trị giảm giá 10%.
- **KHÔNG tự xóa được qua UI** — nút "Xóa" trên list chỉ soft-block (`Blocked=true`), không xóa
  bản ghi (xem `coupon-flow.md` G7). Đây là coupon **thật, dùng được cho POS ngay** — đúng theo
  yêu cầu ban đầu (không dry-run).
- Các case negative (N01/N03/N03b/N14) **KHÔNG tạo coupon** (bị chặn validate trước khi lưu), trừ
  trường hợp code có bug cho phép lưu dù validate — nếu vậy, đó tự nó là 1 phát hiện cần báo cáo.
- Muốn dọn dữ liệu test: lọc theo `Description LIKE 'AUTOTEST Coupon%'` hoặc `Prefix = 'ZTST'`
  trên `CpnVchBOMHeader`/`CpnVchBOMIssueRule`/`CpnVchBOMCodeIssue` — **không có script dọn tự động
  trong phạm vi task này**, DBA tự xử lý nếu cần.
- Chạy nhiều lần liên tiếp → nhiều coupon riêng biệt (ItemNo tự sinh tăng dần, vd
  `C70000015`..`C70000019` khi phát triển guide này) — không trùng lặp, không ghi đè lẫn nhau.

---

## 7. Xử lý sự cố (đã gặp thật khi dựng script này)

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Banner đỏ "Không thể tải dữ liệu form..." ngay khi mở trang | 3 SP `usp_SetupCoupon_*` chưa deploy trên `RPOSMasterData` | Deploy theo `docs/ROLLOUT.md` §D3, chạy lại |
| `check("form load được...")` FAIL nhưng thực ra trang có alert Info bình thường | Đừng nhầm `MudAlert Severity.Info` (gợi ý tick sản phẩm — luôn hiện với coupon mới) với banner lỗi thật — script đã lọc theo class chứa `error`, không dùng `.mud-alert-message` chung chung |
| `Locator.click: Timeout... element is not visible` khi thao tác `MudSelect` | `get_by_label().first` có thể trúng input `type="hidden"` nội bộ của MudSelect (đã xác nhận qua dump DOM) — trang này tránh hẳn vấn đề bằng cách KHÔNG cần chọn "Kiểu giảm giá" (đã mặc định `= 1` Percent trong `CouponAdvancedSaveRequest`) |
| `OSError: [Errno 22] Invalid argument` khi chụp ảnh | Tên file chứa ký tự Windows cấm (`< > : " / \ | ? *`) sinh ra từ tên test case tiếng Việt có `>=`/`%` | Đã sửa bằng hàm `slugify()` trong script — loại ký tự cấm trước khi ghép tên file |
| Đếm số dòng bảng bị dư 1 (`.mud-table-row` khớp cả header) | MudTable render `<tr class="mud-table-row">` cho CẢ header lẫn body | Dùng `tbody.mud-table-body tr` để chỉ đếm dòng dữ liệu |
| `MudDatePicker`/`MudNumericField` không nhận giá trị như gõ | MudBlazor tự **clamp** giá trị theo `Min`/`Max` phía client trước khi submit (xác nhận thật: `Max=100` cho % giảm giá, `Min=1` cho Số lượng mã) | KHÔNG dựa vào việc gõ giá trị ngoài biên để test validate — dùng giá trị mặc định (0) hoặc chỉnh qua UI thật (calendar picker) thay vì giả định input text bị chặn |
| `net::ERR_CONNECTION_REFUSED` | Server chưa chạy / sai cổng | Làm mục 1; đảm bảo profile `http` (5170) |
| `UnicodeEncodeError` trên Windows console | Console mặc định cp1252 | Script đã tự `sys.stdout.reconfigure(encoding="utf-8")` |

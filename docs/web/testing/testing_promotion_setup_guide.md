# Hướng dẫn test trang Cài đặt CTKM POS.Web bằng Playwright (skill `webapp-testing`)

> Test end-to-end trang `/promotion/setup` (`PromotionSetupPage.razor`) bằng browser thật
> (Chromium headless). Cùng khuôn với `docs/web/testing/testing_coupon_issue_guide.md` (trang Phát
> hành Coupon) — bổ sung cho xUnit `tests/POS.ContractTests` / `tests/POS.UnitTests` (test logic,
> KHÔNG render UI).
> Script: `tests/POS.Web.UiTests/smoke_promotion_setup.py`. Kịch bản nghiệp vụ đầy đủ (23 test
> case CTKM-01..CTKM-23): `docs/web/testing/promotion-setup.md` (đọc trước — nguồn đối chiếu chính,
> đã cập nhật 2026-07-16 khớp code thật).

> ⚠️ **PHÁT HIỆN QUAN TRỌNG khi viết guide này (2026-07-16)**: nút **"Duyệt CTKM" hiện KHÔNG hoạt
> động được trên môi trường dev đã test** — mọi lần Duyệt đều lỗi + rollback do bug thật ở SP
> `usp_SetupPromotion_Approve` (transaction count mismatch với SP legacy `Setup_Promotion_Insert`).
> Xem mục 6 "BUG THẬT PHÁT HIỆN" — đọc trước khi coi CTKM-13/14 là đã test được.

## 0. Tiền đề (giống hệt `testing_coupon_issue_guide.md` — kiểm tra 1 lần)

| Thành phần | Kiểm tra | Đã verify trên máy dev |
|---|---|---|
| Python 3.x | `python --version` | ✅ |
| Playwright (Python) | `python -m pip show playwright` | ✅ |
| Chromium cho Playwright | thư mục `%USERPROFILE%\AppData\Local\ms-playwright\chromium-*` | ✅ |
| .NET SDK | `dotnet --version` | ✅ |

**Khác biệt quan trọng so với test Login/Coupon**: trang Cài đặt CTKM chạm DB thật ngay từ
`OnInitializedAsync` (load `OfferType`/`SalesOrderType`/`MemberCode`/`SetupGroupSite`) và **LƯU +
DUYỆT DỮ LIỆU THẬT** (không dry-run, không rollback qua UI) — mỗi lần chạy script **mặc định**
(không set biến lọc gì) tạo ra **1 CTKM nháp chính (positive flow CTKM-02) + 1 CTKM nháp cho MỖI
Loại CTKM có trong dropdown môi trường hiện tại** (phần Sweep, xem mục 4) — tổng số bản ghi thay
đổi theo môi trường (12 Loại → 13 bản ghi trong lần chạy đã verify 2026-07-16). Round-trip mở lại
(CTKM-10/11/23) dùng LẠI cùng bản ghi chính, không tạo mới. 3 case negative (CTKM-03/04/22)
**KHÔNG tạo bản ghi nào** — CTKM-03 bị chặn trước khi bấm được nút (client-side gate), CTKM-04/
CTKM-22 bị chặn ở validate server-side trong `PromotionRepository.SaveSetupAsync` **TRƯỚC KHI** SP
`usp_SaveSetupCTKMAll` được gọi. Xem mục 7 "Dữ liệu để lại" để biết chi tiết đầy đủ.

> **Muốn chỉ test 1 Loại CTKM cụ thể (vd chỉ cần test kỹ ZB06) thay vì chờ sweep hết mọi Loại?**
> Set biến môi trường `POSWEB_TEST_OFFER_TYPES` trước khi chạy — xem mục 2.1. Khi đó script CHỈ
> tạo **1 bản ghi** (test sâu đúng Loại đó), KHÔNG chạy sweep, nhanh hơn nhiều lần.

**Pre-condition riêng cho trang này** (xem `docs/web/testing/promotion-setup.md` mục 0):
- Đã deploy `docs/sql/SetupPromotion_Save.sql` + `docs/sql/SetupPromotion_ApproveAndStatus.sql`
  trên `RPOSMasterData` (xem `docs/ROLLOUT.md` §D1). Thiếu → script tự phát hiện banner đỏ và dừng
  sớm với thông báo rõ ràng (không đoán mò).
- Đăng nhập bằng role **BackOffice**, **ITOps** hoặc **SystemAdmin** (policy
  `WebPolicies.BackOfficeAndAbove` — xác nhận qua `WebRoles.cs:14`, cùng policy dùng cho các trang
  Coupon) — StoreOperator bị chặn.
- **BẮT BUỘC** có ≥1 `dbo.OfferType(Enabled=1)` và ≥1 `dbo.SalesOrderType(IsActive=1)` — script
  chọn **item ĐẦU TIÊN có sẵn trong dropdown**, KHÔNG hardcode mã ZB cụ thể nào (môi trường dev đã
  test có sẵn ZB02/ZB03/ZB04/ZB06/ZB07/ZB08/ZB09/ZB10/ZB12/ZB13/ZB14/ZB15/ZB16...).
- **KHÔNG cần** seed `dbo.Item`/`dbo.SetupGroupSite` — script dùng barcode tự do
  ("TESTSKU001"...) không cần tồn tại thật trong `Item` (validate chỉ yêu cầu `No` không rỗng), và
  KHÔNG thêm dòng ở tab "Cửa hàng áp dụng" (0 dòng Site — không áp dụng cho store thật nào).

---

## 1. Bước 1 — Đảm bảo POS.Web đang chạy ở cổng 5170

Giống hệt `testing_coupon_issue_guide.md` mục 1:
```powershell
Get-NetTCPConnection -LocalPort 5170 -State Listen -ErrorAction SilentlyContinue |
  ForEach-Object { "PID={0} Proc={1}" -f $_.OwningProcess, (Get-Process -Id $_.OwningProcess).ProcessName }
```
- **Có** `Proc=POS.Web` → server đã chạy sẵn → Cách A (mục 2).
- **Trống** → Cách B, dùng `with_server.py` (xem `testing_login_guide.md` mục 2B).

---

## 2. Bước 2 — Chạy test

**Cách A (server đã chạy sẵn):**
```powershell
python tests/POS.Web.UiTests/smoke_promotion_setup.py
```

**Cách B (server chưa chạy):**
```powershell
python .claude/skills/webapp-testing/scripts/with_server.py `
  --server "dotnet run --project src/POS.Web/POS.Web.csproj --launch-profile http" --port 5170 `
  --timeout 240 `
  -- python tests/POS.Web.UiTests/smoke_promotion_setup.py
```

Credential qua biến môi trường (mặc định `admin`/`Admin@0987` — SystemAdmin, seed sẵn):
```powershell
$env:POSWEB_TEST_USER = "admin"
$env:POSWEB_TEST_PASS = "Admin@0987"
python tests/POS.Web.UiTests/smoke_promotion_setup.py
Remove-Item Env:\POSWEB_TEST_PASS
```

### 2.1 Chỉ test 1 (hoặc vài) Loại CTKM cụ thể — `POSWEB_TEST_OFFER_TYPES`

Mặc định (không set biến này) script test Loại **đầu tiên** trong dropdown SÂU (CTKM-02/10/11/
13/14/22) rồi **sweep NÔNG qua TOÀN BỘ** Loại còn lại (mục 4.1) — tốn thời gian nếu chỉ cần tập
trung vào 1 Loại. Set `POSWEB_TEST_OFFER_TYPES` để bỏ qua sweep, chỉ test đúng Loại cần:

```powershell
$env:POSWEB_TEST_OFFER_TYPES = "ZB06"
python tests/POS.Web.UiTests/smoke_promotion_setup.py
Remove-Item Env:\POSWEB_TEST_OFFER_TYPES
```

- Item **đầu tiên** trong danh sách (ngăn cách bởi dấu phẩy nếu nhiều) được dùng cho **luồng chính**
  — test SÂU đúng như Loại mặc định trước đây (Lưu tạm + round-trip Độ ưu tiên/Limit + Duyệt).
- Các item **còn lại** (nếu có, vd `"ZB06,ZB13"`) chỉ được sweep NÔNG (chỉ thử Lưu tạm) — Loại đầu
  đã test sâu ở luồng chính nên KHÔNG sweep lại (tránh trùng).
- CTKM-18 (ẩn tab Buy cho ZB06/ZB13) / CTKM-19 (ZB14/ZB15 vẫn có Buy) **tự SKIP** nếu Loại đang
  filter không liên quan tới 2 nhóm mã này — đỡ tốn 1-2 lượt mở trang không cần thiết.
- Loại truyền vào **không tồn tại** trong dropdown môi trường hiện tại → script dừng sớm báo lỗi
  rõ ràng (KHÔNG âm thầm test Loại khác thay thế).

**Đã verify thật 2026-07-16** với `POSWEB_TEST_OFFER_TYPES=ZB06` (ZB06 là Loại tổng bill + ẩn tab
Buy — trường hợp phức tạp hơn Loại mặc định ZB02):
```
RESULT: PASS - đăng nhập thành công (url=http://localhost:5170/ops/health)
RESULT: PASS - CTKM-01 nút 'Thêm CTKM' hiển thị trên list
INFO: đã chọn Hình thức bán hàng = 'Tại chỗ'
INFO: đã chọn Loại CTKM (theo POSWEB_TEST_OFFER_TYPES) = 'ZB06-Khuyến mãi dựa trên tổng giá trị hóa đơn'
RESULT: PASS - CTKM-02 chọn được Từ ngày/Đến ngày qua calendar picker (from_ok=True to_ok=True)
INFO: tab hiện ra cho Loại CTKM đã chọn: ['Cài đặt nâng cao', 'Cửa hàng áp dụng', 'Sản phẩm khuyến mãi', 'Thông tin chung']
RESULT: PASS - CTKM-02 nút 'Lưu tạm' có thể bấm (không bị disable do CanSave)
RESULT: PASS - CTKM-02 Lưu tạm CTKM tối thiểu hợp lệ thành công (kỳ vọng snackbar chứa 'Lưu CTKM ... thành công')
INFO: CTKM vừa tạo có mã BBYNR = '6000000028'
RESULT: PASS - CTKM vừa tạo xuất hiện trong list (filter theo Mã CTKM) (số dòng khớp=1)
RESULT: PASS - CTKM-10 mở lại: Tên CTKM đọc lại đúng
RESULT: PASS - round-trip Độ ưu tiên/Limit by customer (CTKM-11/23) (priority='3' limit='5,000' (parsed=5.0))
RESULT: PASS - CTKM-13 dialog xác nhận Duyệt xử lý được
RESULT: FAIL - CTKM-13 Duyệt CTKM thành công (publish sang Offer*) (BACKEND BUG — xem mục 6, KHÔNG phải lỗi script)
RESULT: PASS - CTKM vừa xử lý vẫn xuất hiện trong list sau Duyệt (filter theo Mã CTKM) (số dòng khớp=1)
RESULT: FAIL - CTKM-14 chip 'Đã duyệt — chỉ xem' hiển thị
RESULT: FAIL - CTKM-14 nút 'Lưu tạm' KHÔNG hiển thị sau khi duyệt
INFO: SKIP CTKM-03 bỏ trống Tên CTKM — nút 'Lưu tạm' bị disable bởi CanSave
RESULT: PASS - CTKM-04 Đến ngày < Từ ngày
RESULT: PASS - CTKM-22 Số lượng dòng = 0
RESULT: PASS - CTKM-18 ẩn tab Buy cho ZB06/ZB13 (OfferType='ZB06-...', tab Buy visible=False, kỳ vọng=False)
INFO: SKIP CTKM-19 — POSWEB_TEST_OFFER_TYPES không liên quan ZB14/ZB15
INFO: Sweep bỏ qua — POSWEB_TEST_OFFER_TYPES chỉ có 1 Loại (đã test sâu ở luồng chính CTKM-02) hoặc không còn Loại nào khác cần sweep
SUMMARY: SOME FAILED
```
Chỉ **8 screenshot** (thay vì 21 ở lần chạy đầy đủ) và **1 bản ghi CTKM** thật được tạo (thay vì
13) — đúng như mong đợi. 3 FAIL vẫn là bug thật ở `usp_SetupPromotion_Approve` (mục 6), không liên
quan gì tới việc filter.

> **Lưu ý quan trọng đã sửa khi thêm tính năng này**: lúc đầu lần chạy với `ZB06` bị FAIL ngay ở
> CTKM-02 (Lưu tạm) vì luồng chính trước đó chỉ được thiết kế cho Loại mặc định ZB02 (không cần
> `MinValue`/Voucher date) — chưa có logic tự điền `Giá trị tổng bill tối thiểu` cho Loại tổng bill
> như ZB06. Đã sửa bằng cách trích xuất logic thích ứng này (vốn đã có trong sweep) thành helper
> `fill_type_specific_requirements()` dùng chung cho cả luồng chính, sweep, và case CTKM-22 — đảm
> bảo BẤT KỲ Loại nào user filter tới đều được điền đúng field bắt buộc trước khi Lưu.

---

## 3. Kết quả THẬT (đã chạy thật 2026-07-16, exit code 1 — SUMMARY: SOME FAILED)

> **Exit code 1 là ĐÚNG, không phải lỗi script** — 3 assertion FAIL (thuộc 2 test case CTKM-13 +
> CTKM-14) đều phản ánh **cùng 1 bug thật đang tồn tại** ở `usp_SetupPromotion_Approve` (xem mục
> 6). Toàn bộ case còn lại PASS thật — kể cả **12/12 Loại CTKM sweep qua hết** (mục 4) — có
> screenshot làm bằng chứng trong `tests/POS.Web.UiTests/artifacts/`.

```
RESULT: PASS - đăng nhập thành công (url=http://localhost:5170/ops/health)
RESULT: PASS - CTKM-01 nút 'Thêm CTKM' hiển thị trên list
INFO: đã chọn Hình thức bán hàng = 'Tại chỗ'
INFO: đã chọn Loại CTKM = 'ZB02-Tặng sản phẩm'
RESULT: PASS - CTKM-02 chọn được Từ ngày/Đến ngày qua calendar picker (from_ok=True to_ok=True)
INFO: tab hiện ra cho Loại CTKM đã chọn: ['Cài đặt nâng cao', 'Cửa hàng áp dụng', 'Sản phẩm khuyến mãi', 'Sản phẩm mua', 'Thông tin chung']
RESULT: PASS - CTKM-02 nút 'Lưu tạm' có thể bấm (không bị disable do CanSave)
RESULT: PASS - CTKM-02 Lưu tạm CTKM tối thiểu hợp lệ thành công (kỳ vọng snackbar chứa 'Lưu CTKM ... thành công')
INFO: CTKM vừa tạo có mã BBYNR = '6000000015'
RESULT: PASS - CTKM vừa tạo xuất hiện trong list (filter theo Mã CTKM) (số dòng khớp=1)
RESULT: PASS - CTKM-10 mở lại: Tên CTKM đọc lại đúng
RESULT: PASS - round-trip Độ ưu tiên/Limit by customer (CTKM-11/23) (priority='3' limit='5,000' (parsed=5.0))
RESULT: PASS - CTKM-13 dialog xác nhận Duyệt xử lý được
RESULT: FAIL - CTKM-13 Duyệt CTKM thành công (publish sang Offer*) (BACKEND BUG: snackbar 'Lỗi hệ thống...' — xem D:\ROOT\Logs\POS.Web\Exception\log-*.txt để xác nhận nguyên nhân thật (KHÔNG phải lỗi test script))
RESULT: PASS - CTKM vừa xử lý vẫn xuất hiện trong list sau Duyệt (filter theo Mã CTKM) (số dòng khớp=1)
RESULT: FAIL - CTKM-14 chip 'Đã duyệt — chỉ xem' hiển thị
RESULT: FAIL - CTKM-14 nút 'Lưu tạm' KHÔNG hiển thị sau khi duyệt
INFO: SKIP CTKM-03 bỏ trống Tên CTKM — nút 'Lưu tạm' bị disable bởi CanSave (client-side gate), KHÔNG reachable qua UI để test validate server-side
RESULT: PASS - CTKM-04 Đến ngày < Từ ngày (kỳ vọng snackbar chứa: 'Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu')
RESULT: PASS - CTKM-22 Số lượng dòng = 0 (kỳ vọng snackbar chứa: 'Số lượng của mỗi dòng sản phẩm phải ≥ 1')
RESULT: PASS - CTKM-18 ẩn tab Buy cho ZB06/ZB13 (OfferType='ZB06-Khuyến mãi dựa trên tổng giá trị hóa đơn', tab Buy visible=False, kỳ vọng=False)
RESULT: PASS - CTKM-19 ZB14/ZB15 vẫn có tab Buy (OfferType='ZB15-Group items Or Total Value get voucher', tab Buy visible=True, kỳ vọng=True)
INFO: Sweep 12 Loại CTKM có sẵn trong dropdown môi trường này: ['ZB02-Tặng sản phẩm', 'ZB03-Giảm giá theo hạng thẻ thành viên', 'ZB04-Sinh nhật thành viên', 'ZB06-Khuyến mãi dựa trên tổng giá trị hóa đơn', 'ZB07-Mua X Tặng Y', 'ZB08-Gói combo các sản phẩm', 'ZB09-Total Value discount bill', 'ZB10-Single Item Discount', 'ZB12-Total Value get Gift', 'ZB13-Total Value get Voucher/Coupon', 'ZB15-Group items Or Total Value get voucher', 'ZB16-RewardCode']
RESULT: PASS - Sweep OfferType 'ZB02-Tặng sản phẩm': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB03-Giảm giá theo hạng thẻ thành viên': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB04-Sinh nhật thành viên': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB06-Khuyến mãi dựa trên tổng giá trị hóa đơn': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB07-Mua X Tặng Y': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB08-Gói combo các sản phẩm': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB09-Total Value discount bill': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB10-Single Item Discount': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB12-Total Value get Gift': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB13-Total Value get Voucher/Coupon': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB15-Group items Or Total Value get voucher': Lưu tạm thành công
RESULT: PASS - Sweep OfferType 'ZB16-RewardCode': Lưu tạm thành công
INFO: CTKM test đã tạo THẬT trong DB — BBYNR=6000000015, ...
SUMMARY: SOME FAILED
```
Ảnh chụp từng bước: `tests/POS.Web.UiTests/artifacts/promotion_setup_NN_*.png` (21 ảnh — 9 ảnh
positive/negative/conditional + 12 ảnh sweep, 1 ảnh/Loại CTKM).

- Mỗi assertion in `RESULT: PASS/FAIL`; `INFO: SKIP ...` = case không reachable qua UI (không tính
  FAIL, xem mục 5).
- Có `RESULT: FAIL` → mở đúng ảnh `promotion_setup_NN_*.png` theo thứ tự bước để xem trạng thái UI
  thật lúc đó.
- **26/29 assertion `RESULT:` PASS (90%), 3 FAIL — cả 3 đều thuộc về Duyệt CTKM (1 ở CTKM-13 + 2 ở
  CTKM-14), cùng do 1 bug thật duy nhất ở SP `usp_SetupPromotion_Approve`, KHÔNG phải lỗi script.**
  Toàn bộ 12/12 Loại CTKM trong sweep đều Lưu tạm thành công — không phát hiện lỗi riêng theo
  từng Loại trên môi trường đã test. Cộng 1 `INFO: SKIP` (CTKM-03, không tính vào PASS/FAIL). Sau
  khi DBA/Dev sửa SP đó (xem mục 6), chạy lại script này — nếu cả 3 assertion CTKM-13/14 chuyển
  PASS, đó là xác nhận bug đã fix (regression test tự nhiên).

---

## 4. Kịch bản đang phủ (mapping sang `promotion-setup.md`)

| Bước script | TC-ID |
|---|---|
| Mở `/promotion/setup`, verify nút "Thêm CTKM", banner lỗi nếu SP chưa deploy | CTKM-01 |
| Tạo CTKM tối thiểu hợp lệ — chọn Loại CTKM **đầu tiên có sẵn trong dropdown** (KHÔNG hardcode ZB), thích ứng theo tab Buy/Get hiện ra thật, set Độ ưu tiên/Limit ở tab Nâng cao → Lưu tạm | CTKM-02 |
| Filter list theo Mã CTKM → mở lại → verify Tên CTKM + Độ ưu tiên + Limit đọc lại đúng | CTKM-10 (+ round-trip CTKM-11/23) |
| Bấm "Duyệt CTKM" → xử lý dialog xác nhận → verify snackbar thành công | **CTKM-13 — FAIL do bug thật, xem mục 6** |
| Mở lại CTKM đã Duyệt → verify chip readonly + ẩn nút Lưu tạm | **CTKM-14 — FAIL do bug thật (rollback từ CTKM-13), xem mục 6** |
| Bỏ trống Tên CTKM → Lưu | CTKM-03 (**SKIP** — xem mục 5) |
| Đến ngày < Từ ngày → Lưu | CTKM-04 |
| Đặt Số lượng dòng = 0 (đã có ≥1 dòng hợp lệ ở tab bắt buộc khác nếu có) → Lưu | CTKM-22 |
| Chọn OfferType chứa "ZB06"/"ZB13" (nếu có trong dropdown) → verify ẩn tab Buy | CTKM-18 |
| Chọn OfferType chứa "ZB14"/"ZB15" (nếu có trong dropdown) → verify tab Buy vẫn hiện | CTKM-19 |
| **Sweep**: liệt kê Loại CTKM cần test (mặc định = TOÀN BỘ dropdown; hoặc đúng danh sách `POSWEB_TEST_OFFER_TYPES` trừ item đầu đã test sâu ở luồng chính — mục 2.1) → với MỖI Loại, tự thích ứng field cần điền (Buy/Get nếu tab hiện, MinValue nếu tổng bill, Voucher từ/đến ngày nếu checkbox Voucher tự khoá) → thử Lưu tạm → PASS/FAIL riêng từng Loại | *(không thuộc 23 case chính thức — bổ sung theo yêu cầu "test được từng OfferType", xem mục 4.1)* |

### 4.1 Sweep từng OfferType — vì sao cần và cách hoạt động

Positive flow ở CTKM-02 chỉ thử **1 Loại CTKM** (loại đầu tiên trong dropdown). Nhưng mỗi Loại có
tổ hợp yêu cầu field khác nhau theo cờ động `dbo.OfferType` (`IsSetupBuy`/`IsSetupGet`/
`IsTotalBill`/`IsVoucher`/`IsGift`, xem `PROMOTION_SETUP_MANUAL.md` mục 2 "Ma trận cấu hình theo
loại") — 1 Loại pass không đảm bảo Loại khác cũng pass (vd Loại tổng bill cần `MinValue > 0`,
Loại voucher cần Voucher từ/đến ngày). Sweep giải quyết đúng vấn đề này: lặp qua **mọi Loại CTKM
đang có trong dropdown môi trường** (không hardcode mã ZB — đọc trực tiếp từ dropdown lúc chạy),
với mỗi Loại:
1. Chọn Loại đó (`select_option_containing` theo mã ZB đọc được từ `list_all_select_options`).
2. Nếu field "Giá trị tổng bill tối thiểu để hưởng KM" xuất hiện (tab "Thông tin chung") → Loại
   này là tổng bill (`IsTotalBill=1`) → điền 100000.
3. Nếu checkbox "Voucher/Coupon" đang ở trạng thái tick (`is_checkbox_checked`) → Loại này bắt
   buộc voucher (`IsVoucher=1`, tự tick+khoá bởi `OnOfferTypeChanged`) → sang tab "Cài đặt nâng
   cao" điền Voucher từ ngày/đến ngày (validate rule 15 bắt buộc 2 field này khi `IsVoucher=true`).
4. Nếu tab "Sản phẩm mua"/"Sản phẩm khuyến mãi" hiện ra → thêm 1 dòng barcode giả tương ứng.
5. Bấm "Lưu tạm" → verify snackbar thành công; nếu KHÔNG, log nguyên văn snackbar lỗi thật (qua
   `capture_last_alert_text`) để biết chính xác Loại nào fail vì lý do gì — không đoán mò.

**Kết quả đã verify 2026-07-16**: môi trường dev có 12 Loại CTKM (`ZB02/03/04/06/07/08/09/10/12/
13/15/16`) — **cả 12/12 đều Lưu tạm thành công**, không phát hiện lỗi riêng theo Loại nào. Môi
trường khác có Loại khác (vd `ZB05`/`ZB14`) sẽ tự động được sweep qua khi chạy lại script đó, vì
danh sách Loại đọc động từ dropdown, không hardcode.

**Giới hạn của sweep** (đọc trước khi coi 12/12 PASS = "mọi Loại hoàn hảo"):
- Sweep chỉ test **Lưu tạm** (tạo nháp), KHÔNG Duyệt cho từng Loại (Duyệt đang lỗi toàn cục — xem
  mục 6 — Duyệt lặp 12 lần chỉ tạo thêm 12 lần lỗi giống nhau, không có giá trị thông tin mới).
- Sweep dùng 1 dòng Buy/Get đơn giản (LineType=Sản phẩm, barcode giả) — KHÔNG test Nhóm SP, ScaleType
  From/Upto, DiscountType Giá cố định (combo ZB02/ZB07) — xem mục 5 case CTKM-05..09.
- Sweep KHÔNG verify hành vi tính KM ở POS (offer_procedure.sql) cho từng Loại — chỉ verify Lưu
  tạm có thành công hay không ở tầng UI/Repository.

---

## 5. Giới hạn / KHÔNG phủ (đọc kỹ trước khi coi CI xanh = "đã test hết")

Đã chốt phạm vi với người dùng: **positive flow đầy đủ (gồm Duyệt) + 3 case negative trọng điểm +
2 case conditional**. KHÔNG tự động hoá (vẫn cần test thủ công theo `promotion-setup.md`):

- **CTKM-03** (bỏ trống Tên CTKM) — **XÁC NHẬN QUA CHẠY THẬT KHÔNG REACHABLE QUA UI**: nút "Lưu
  tạm" bị `CanSave` (client-side gate, `PromotionSetupPage.razor`) **disable hoàn toàn** khi
  `Description` rỗng — không có cách nào bấm được nút để round-trip lên server và thấy message
  "Vui lòng nhập tên chương trình khuyến mãi". Cùng loại phát hiện với TC-N06 của Coupon (auto-clamp
  MudNumericField) — chỉ khác cơ chế chặn (disable button vs auto-clamp input). Message vẫn ĐÚNG
  và tồn tại ở `PromotionRepository.SaveSetupAsync`, chỉ là dead-code-qua-UI (vẫn có ý nghĩa nếu
  gọi API/service trực tiếp, ví dụ unit test tầng Repository).
- **CTKM-05..09** (AND/OR nhóm SP cụ thể, chiết khấu Số tiền/ScaleType From/Upto, dòng theo Nhóm
  SP qua dialog "Cấu hình nhóm", cửa hàng cụ thể qua dialog "Chọn nhóm CH/ST") — script v1 chỉ điền
  Barcode tự do (LineType=0/Sản phẩm), không mở `ItemGroupSetupDialog`/`SiteGroupSetupDialog`.
- **CTKM-12** (Voucher dates round-trip) — sweep (mục 4.1) đã **gián tiếp phủ phần điền +
  Lưu thành công** cho mọi Loại có `IsVoucher=1` (ZB13/ZB16 trong lần verify 2026-07-16), nhưng
  **CHƯA verify round-trip** (mở lại sau khi Lưu, đọc lại đúng Voucher từ/đến ngày/Số ngày hiệu
  lực/Số lần phát hành như đã nhập) — sweep chỉ kiểm tra Lưu tạm thành công, không mở lại. Có thể
  bổ sung round-trip riêng cho case này ở phiên bản sau.
- **CTKM-15/16** (filter trạng thái duyệt trên tập dữ liệu lớn, phân trang >20 dòng) — cần seed số
  lượng lớn, ngoài phạm vi 1 script smoke.
- **CTKM-17** (Audit log) — cần đọc DB `DashboardAuditLog`, không kiểm qua UI/Playwright.
- **CTKM-20/21** (verify `OfferBenefits.StepAmount`/`OfferBuy.DiscountType` sau Duyệt bằng SQL) —
  **hiện KHÔNG THỂ verify** vì Duyệt đang lỗi (mục 6); câu lệnh SQL tham chiếu ở
  `PROMOTION_SETUP_MANUAL.md` mục 5, chạy tay sau khi bug SP được fix.
- Trang downstream `/promotion/offers` (`OffersPage.razor`, `docs/web/testing/promotion-setup.md`
  PHẦN B, OFFERS-01..08) — ngoài phạm vi yêu cầu (chỉ `PromotionSetupPage.razor`); và vì Duyệt
  đang lỗi, trang đó hiện không nhận được dữ liệu mới nào từ luồng Setup để test.

---

## 6. 🔴 BUG THẬT PHÁT HIỆN — `usp_SetupPromotion_Approve` transaction count mismatch

**Phát hiện khi**: chạy script này lần đầu 2026-07-16, CTKM-13 (Duyệt CTKM) fail với snackbar đỏ
"Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT." — **tái hiện được 100% (3/3 lần chạy thật)**,
không phụ thuộc dữ liệu test cụ thể.

**Bằng chứng** (`D:\ROOT\Logs\POS.Web\Exception\log-20260716.txt`):
```
PromotionRepository.ApproveSetupAsync===>Microsoft.Data.SqlClient.SqlException (0x80131904):
Transaction count after EXECUTE indicates a mismatching number of BEGIN and COMMIT statements.
Previous count = 1, current count = 0.
   ...
   at POS.Infrastructure.Repositories.PromotionRepository.ApproveSetupAsync(...)
Error Number:266,State:2,Class:16
```

**Nguyên nhân (đọc `docs/sql/SetupPromotion_ApproveAndStatus.sql`)**: `usp_SetupPromotion_Approve`
mở `BEGIN TRANSACTION` tường minh, rồi `EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBYNR` (SP
**legacy, không được sửa** — quản lý transaction riêng bên trong nó). Khi SP legacy tự
`BEGIN`/`COMMIT` transaction của chính nó, `@@TRANCOUNT` bị lệch so với kỳ vọng của SP bọc ngoài —
SQL Server phát hiện mismatch NGAY SAU khi câu `EXEC` hoàn tất và raise lỗi 266. Vì
`usp_SetupPromotion_Approve` có `SET XACT_ABORT ON`, lỗi này **tự động rollback TOÀN BỘ
transaction** — bao gồm cả `UPDATE SetupPromotionHEADER SET IsApprove=1` đã chạy TRƯỚC đó trong
cùng transaction. Kết quả: **Duyệt CTKM luôn thất bại và luôn rollback về trạng thái chưa duyệt**,
dù `Setup_Promotion_Insert` (nếu tới lượt chạy) có thể đã publish 1 phần dữ liệu trước khi toàn bộ
bị cuốn theo rollback.

**Tác động**: tính năng "Duyệt CTKM" **hoàn toàn không dùng được** trên môi trường đã test hiện
tại — mọi CTKM tạo mới đều mãi ở trạng thái nháp/chưa duyệt, KHÔNG bao giờ publish được sang
`Offer*` (do đó `/promotion/offers` cũng không nhận được offer mới nào qua luồng UI này).

**Mức độ tin cậy**: **CONFIRMED** — tái hiện 3/3 lần chạy, bằng chứng từ file log thật (không suy
đoán), khớp chính xác với hành vi SQL Server đã biết khi 1 SP có transaction tường minh gọi 1 SP
khác cũng tự quản lý transaction riêng.

**Hướng sửa gợi ý** (CHƯA áp dụng — cần xác nhận với DBA/chủ SP trước khi sửa, vì đây là thay đổi
ngoài phạm vi "viết test script" ban đầu): bỏ `BEGIN TRANSACTION`/`COMMIT TRANSACTION` tường minh
quanh câu `EXEC Setup_Promotion_Insert` trong `usp_SetupPromotion_Approve` (để SP legacy tự quản
lý transaction của nó, không lồng transaction ngoài); hoặc kiểm `@@TRANCOUNT` trước/sau EXEC và xử
lý phù hợp. Cần review kỹ vì thay đổi cấu trúc transaction ảnh hưởng tính atomic của việc ghi
`IsApprove=1` + đọc lại 5 Counter OUTPUT param ngay sau đó.

**Không có trong phạm vi commit này**: sửa file `docs/sql/SetupPromotion_ApproveAndStatus.sql` —
đây là quyết định cần xác nhận riêng với người yêu cầu/DBA trước khi đổi 1 stored procedure đã
deploy (có entry trong `docs/sql/manifest.json`), ngoài phạm vi "viết script test" ban đầu.

---

## 7. Dữ liệu để lại sau khi test (BẮT BUỘC đọc trước khi chạy nhiều lần)

Mỗi lần chạy tạo **CTKM nháp thật** trong `RPOSMasterData` (`SetupPromotionHEADER`/`BUY`/`GET`):
- **1 CTKM chính** (positive flow, CTKM-02): `Description` = `AUTOTEST CTKM {unix_timestamp}`,
  `SalesType`/`OfferType` = option đầu tiên trong dropdown môi trường (ghi log `INFO:` mỗi lần
  chạy), 1 dòng Buy + 1 dòng Get barcode giả (`TESTSKU001`/`TESTSKU002`), `PriorityBBY=3`,
  `LimitQty=5`. Do bug mục 6, **Duyệt luôn fail và rollback** → CTKM này **mãi ở trạng thái nháp
  chưa duyệt**, KHÔNG publish sang `Offer*` (an toàn hơn dự kiến ban đầu — dự kiến approve thành
  công 0 dòng Site nên vô hại, thực tế còn không publish được gì cả).
- **N CTKM sweep** (mục 4.1) — **N phụ thuộc `POSWEB_TEST_OFFER_TYPES` (mục 2.1)**:
  - KHÔNG set biến (mặc định) → N = số Loại CTKM đang có trong dropdown môi trường (12 trong lần
    verify 2026-07-16 → 12 bản ghi thêm).
  - Set với **đúng 1 Loại** (vd `"ZB06"`) → **N = 0** — sweep bỏ qua hoàn toàn (Loại đó đã test
    sâu ở luồng chính), **tổng chỉ 1 bản ghi/lần chạy** — đã verify thật 2026-07-16.
  - Set với **nhiều Loại** (vd `"ZB06,ZB13"`) → N = số Loại còn lại sau item đầu (1 trong ví dụ
    này).
  - Mỗi bản ghi sweep `Description` = `AUTOTEST CTKM {unix_timestamp} SWEEP {mã_ZB}`, chỉ Lưu tạm
    (KHÔNG Duyệt) → **tất cả ở trạng thái nháp chưa duyệt**, KHÔNG publish sang `Offer*`.
- **Tổng mỗi lần chạy = 1 + N bản ghi** — 13 khi không filter (verify 2026-07-16), **1 khi filter
  đúng 1 Loại cụ thể** (verify 2026-07-16 với `POSWEB_TEST_OFFER_TYPES=ZB06`).
- **CTKM-04/CTKM-22** (negative case): request Lưu **BỊ CHẶN ở validate trong
  `PromotionRepository.SaveSetupAsync` TRƯỚC KHI gọi SP** — `usp_SaveSetupCTKMAll` **KHÔNG được
  gọi** khi validate fail (return sớm) → **KHÔNG tạo bản ghi nào trong DB** cho 2 case này.
- **CTKM-03**: SKIP hoàn toàn (không bấm được Lưu) → KHÔNG tạo bản ghi.
- **CTKM-18/CTKM-19**: chỉ mở editor để đọc tab hiện/ẩn, **KHÔNG bấm Lưu** → KHÔNG tạo bản ghi.
- **Trang này KHÔNG có nút Xóa** (chỉ Sửa/Xem + Duyệt trên list) — khác Coupon (vẫn có nút Xóa dù
  chỉ soft-block). Không có cách dọn CTKM nháp qua UI; dọn bằng SQL tay theo
  `Description LIKE 'AUTOTEST CTKM%'` trên `SetupPromotionHEADER`/`SetupPromotionBUY`/
  `SetupPromotionGET` nếu cần — **không có script dọn tự động trong phạm vi task này**.
- Chạy nhiều lần liên tiếp → nhiều CTKM riêng biệt (`BBYNR` tự sinh tăng dần, vd `6000000011`..
  `6000000015` khi phát triển guide này, cộng dồn N sweep mỗi lần) — không trùng lặp, không ghi
  đè lẫn nhau. **Chạy script nhiều lần liên tiếp trên môi trường có nhiều Loại CTKM sẽ tích luỹ
  khá nhanh** (13 bản ghi/lần trong ví dụ này) — cân nhắc dọn SQL tay theo chu kỳ nếu chạy CI lặp lại.
- **Sau khi bug mục 6 được fix**: lần chạy tiếp theo, CTKM chính (KHÔNG phải các bản ghi sweep) SẼ
  Duyệt thành công thật, publish sang `OfferHeader`/`OfferBuy`/`OfferGet`/`OfferBenefits` — **0
  dòng `OfferSite`** (script không thêm Site) → **không áp dụng ở bất kỳ store nào**, an toàn về
  vận hành, nhưng vẫn là bản ghi LIVE tồn tại vĩnh viễn, không sửa lại được sau Duyệt. Các bản ghi
  sweep vẫn giữ nguyên ở trạng thái nháp (script không Duyệt chúng).

---

## 8. Xử lý sự cố (đã gặp thật khi dựng script này)

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| `Locator.click: Timeout 30000ms exceeded` trên `get_by_label(...)` của `MudSelect` (element resolved nhưng "not visible") | `get_by_label` trúng đúng `<input>` nội bộ của MudSelect nhưng input đó `type="hidden"` (hoặc sibling hiển thị bị `style="display:none"` khi chưa có giá trị) — xác nhận qua dump DOM thật | Click vào **ancestor `.mud-input-control`** (`hidden_input.locator("xpath=ancestor::div[contains(@class,'mud-input-control')][1]")`) thay vì click trực tiếp label/input |
| Không biết chọn item nào trong popover MudSelect vừa mở | Popover render `<div role="listbox"><div role="option">...` (MudBlazor 9.5, xác nhận qua dump DOM) — item đầu tiên đã sẵn `tabindex="0"` | `page.get_by_role("option").first.click()` để chọn item đầu; `page.locator('[role="option"]', has_text=...)` để chọn theo substring (case conditional ZB06/ZB13...) |
| Round-trip field "Giới hạn KH / Limit by customer" đọc lại ra `"5,000"` thay vì `"5"` tưởng là sai | `MudNumericField<decimal>` hiển thị theo culture **vi-VN** — dấu phẩy là **decimal separator** (không phải nghìn), "5,000" = 5.000 (3 số lẻ hiển thị) | Parse bằng `float(val.replace(",", "."))` rồi so sánh số, KHÔNG so sánh string "5" == "5,000" |
| CTKM-03 (bỏ trống Tên CTKM) không bao giờ bấm được nút "Lưu tạm" | `CanSave` (client-side gate) disable hẳn nút khi `Description` rỗng — không round-trip lên server được | Check `save_btn.is_enabled()` trước khi click; nếu disable → in `INFO: SKIP`, không tính FAIL (case không reachable qua UI, giống TC-N06 của Coupon) |
| CTKM-22 (Quantity=0) báo sai message ("cần ít nhất 1 dòng Sản phẩm mua" thay vì "Số lượng...≥1") | `PromotionRepository.SaveSetupAsync` chạy validate THEO THỨ TỰ — rule "Buy required" (OfferType đầu dropdown = ZB02, cần CẢ Buy và Get) chạy TRƯỚC rule Quantity — script chỉ thêm dòng ở 1 tab (Get), bỏ sót Buy | Thêm dòng hợp lệ ở CẢ 2 tab (nếu cả 2 đều hiện) TRƯỚC khi đặt Quantity=0 ở dòng muốn test, để cô lập đúng đúng 1 rule đang nhắm tới |
| Cột "Số lượng"/"Giá trị"/"ĐVT" trong dòng Buy/Get không tìm được qua `get_by_label` | `MudNumericField`/`MudTextField` trong các cột này KHÔNG có thuộc tính `Label=` (chỉ có ở "Số lượng dòng" bulk-add trên toolbar) — xác nhận đọc trực tiếp source `PromotionSetupPage.razor` | Định vị theo vị trí cột trong `<tr>` (`row.locator("td").nth(index)`) — chỉ dùng khi thực sự cần (script v1 chỉ cần làm vậy cho case CTKM-22) |
| `Locator.click: Timeout... element is not enabled` trên nút "Lưu tạm" khi chạy negative case | Xem dòng "CTKM-03" ở trên — cùng nguyên nhân `CanSave` | Luôn check `is_enabled()` trước khi click bất kỳ nút Lưu nào trong case negative mới |
| `net::ERR_CONNECTION_REFUSED` | Server chưa chạy / sai cổng | Làm mục 1; đảm bảo profile `http` (5170) |
| `UnicodeEncodeError` trên Windows console | Console mặc định cp1252 | Script đã tự `sys.stdout.reconfigure(encoding="utf-8")` |
| (Ghi nhận, không phải lỗi) Cần biết checkbox "Voucher/Coupon" đang tick hay chưa (sweep mục 4.1) trước khi quyết định điền Voucher từ/đến ngày | `MudCheckBox` — KHÔNG giống `MudSelect` (không có input `type="hidden"` ẩn) — `get_by_label(...).is_checked()` hoạt động trực tiếp, xác nhận qua chạy thật (12/12 Loại sweep pass ngay lần đầu, không cần sửa) | Dùng `page.get_by_label(label, exact=True).first.is_checked()` bọc try/except — không cần workaround ancestor `.mud-input-control` như MudSelect |

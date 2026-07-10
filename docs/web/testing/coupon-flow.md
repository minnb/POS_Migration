# Tài liệu kiểm thử — Coupon Flow (8.1 + 8.2)

> Phạm vi: 2 trang POS.Web
> - **8.1** `GET /promotion/coupons` — `CouponsPage.razor` (danh sách + xóa)
> - **8.2** `GET /promotion/coupons/issue` — `CouponIssuePage.razor` (phát hành Auto/Import + nâng cao + sản phẩm + mã)
>
> Tài liệu này tập trung vào **các điểm yếu thực tế trong code** (mục 6) để QA khai thác, không chỉ happy-path.

---

## 1. Tổng quan

| Hạng mục | Chi tiết |
|---|---|
| Route | `/promotion/coupons` (list), `/promotion/coupons/issue` (form), `/promotion/coupons/issue?id={ItemNo}` (sửa) |
| Render mode | `InteractiveServer` (Blazor Server — có circuit/WebSocket) |
| Policy | `WebPolicies.OpsAndAbove` (ITOps + SystemAdmin). **StoreOperator KHÔNG vào được** |
| Luồng | `Page → ICouponService → ICouponRepository → SP usp_SetupCoupon_*` (DB `RPOSMasterData`) |
| Sinh mã Auto | Ở tầng Application (`CouponService`, C#), KHÔNG ở SP |
| Bảng tác động | `CpnVchBOMIssueRule`, `CpnVchBOMHeader`, `CpnVchBOMCodeIssue`, `CpnVchBOMLine`, `CpnVchBOMStore` |
| Item picker | Tái dùng `ICentralMDRepository.GetProductListAsync` (migrate 6.1) |
| Audit | `IAuditLogger.LogAsync` sau mỗi thao tác ghi thành công (CREATE/UPDATE/DELETE) |

**Pre-condition chung cho MỌI test:**
- Đã deploy 3 script: `docs/sql/SetupCoupon_Read.sql`, `SetupCoupon_Save.sql`, `SetupCoupon_Delete.sql` trên `RPOSMasterData`.
- Đã chạy migration bảng audit (`migration_dashboard_audit_log.sql`) — nếu chưa, audit fail-safe (không crash).
- Đăng nhập bằng user role **ITOps** hoặc **SystemAdmin**.
- Có sẵn dữ liệu: ≥1 `SalesOrderType(IsActive=1)`, ≥1 `StoreGroup(Status=1)` có store, ≥1 `UnitOfMeasure`, ≥1 `Item(Blocked=0)`.

---

## 2. Ma trận chức năng ↔ điều kiện hiển thị

| Thành phần | Điều kiện hiển thị / enable | Nguồn code |
|---|---|---|
| Nút **Xóa** (list) | Chỉ hiện khi `QtyCoupon == 0` | `CouponsPage.razor` `@if (context.QtyCoupon == 0)` |
| Field Auto (Prefix/LenCode/CharOfNumber/CharPosition/Quantity) | Chỉ hiện khi `IssueType == "Auto"`; **khóa** khi `CodeFieldsLocked` | `IsAuto`, `CodeFieldsLocked` |
| Khối Import (upload + file mẫu) | Chỉ hiện khi `IssueType == "Import"` | `else` của `@if (IsAuto)` |
| `CodeFieldsLocked` (khóa Cách phát hành + field Auto/Import) | `IsEditing && _quantityCodeInDb > 0` | `CodeFieldsLocked` |
| Tab **Mã coupon đã phát hành** | Bảng chỉ có dữ liệu khi `IsEditing`; ngược lại hiện alert | `@if (!IsEditing)` |
| Nút **Lưu** | Disable khi `_saving == true` | `Disabled="_saving"` |
| Tiêu đề trang | "Sửa coupon {ItemNo}" khi `IsEditing`, ngược lại "Phát hành Coupon" | `IsEditing` |

> `IsEditing = !string.IsNullOrWhiteSpace(_model.ItemNo)`.

---

## 3. Logic đặc biệt — chọn "Nhóm cửa hàng" (StoreGroupCode)

Khi bấm **Lưu** (SP `usp_SetupCoupon_SaveIssue`), bảng `CpnVchBOMStore` được ghi lại (replace) theo quy tắc:

| Chọn Nhóm cửa hàng | Kết quả trong `CpnVchBOMStore` |
|---|---|
| `ALL` | Chèn **1 dòng** `StoreNo = 'ALL'` |
| Một GroupCode cụ thể | **Bung** thành N dòng — mỗi `StoreNo` thuộc `dbo.StoreGroup WHERE GroupCode = @code` |

⚠️ **Điểm yếu E4:** nếu GroupCode được chọn không map tới store nào (group rỗng hoặc store `Status=0`) → **0 dòng** store được chèn → coupon **không áp dụng cửa hàng nào** mà UI **không cảnh báo**. Xem TC ở mục 6.

---

## 4. Test cases 8.1 — CouponsPage (`/promotion/coupons`)

### TC-L01 — Load danh sách + phân trang (Positive)
- **Scenario:** Trang load danh sách coupon phân trang server-side.
- **Pre-condition:** DB có ≥ 25 coupon (để test phân trang 10/trang).
- **Steps:** 1) Mở `/promotion/coupons`. 2) Đổi "Số dòng mỗi trang" 10 → 20 → 50. 3) Chuyển trang.
- **Expected Result:** Bảng hiển thị đúng cột (STT, Mã phát hành, Mô tả, Cách phát hành, Tiền tố, Kích thước mã, Số chữ cái, Vị trí đứng, Số lượng, Hiệu lực). STT liên tục qua các trang (`_offset + index + 1`). Chip Hiệu lực = xanh "Hiệu lực" / đỏ "Hết hiệu lực". Cột "Số lượng" format `#,##0`.
- **Edge Cases:** Bảng rỗng → hiện icon Inbox + "Không có coupon khớp bộ lọc". Đổi PageSize không được mất lựa chọn (mặc định phải chứa `10`).

### TC-L02 — Bộ lọc + nút Xóa filter (Positive)
- **Scenario:** Lọc theo Mã / Tên / Cách phát hành / Hiệu lực.
- **Pre-condition:** Có coupon Auto và Import, có coupon còn/hết hiệu lực.
- **Steps:** 1) Nhập "Mã phát hành". 2) Chọn Cách phát hành = "Import excel". 3) Chọn Hiệu lực = "Hết hiệu lực". 4) Bấm **Tìm**. 5) Bấm **Xóa**.
- **Expected Result:** Sau **Tìm** → chỉ hiện dòng khớp toàn bộ tiêu chí. Sau **Xóa** → tất cả field filter rỗng, Hiệu lực về "Tất cả" (`Status = "-1"`), bảng reload full.
- **Edge Cases:** Nhập mã không tồn tại → bảng rỗng (không lỗi). Ký tự đặc biệt/`%`/`'` trong ô tìm → không phá query (SP tham số hóa).

### TC-L03 — Xóa coupon chưa phát sinh mã (Positive)
- **Scenario:** Xóa coupon `QtyCoupon == 0`.
- **Pre-condition:** Có coupon với 0 mã.
- **Steps:** 1) Bấm icon 🗑 ở dòng đó. 2) Dialog xác nhận → **Xóa**.
- **Expected Result:** Snackbar "Đã xóa coupon...". Bảng reload không còn dòng. DB: xóa khỏi `CpnVchBOMIssueRule` + `CpnVchBOMHeader` (+ dọn Line/Store). Ghi 1 dòng audit `DELETE / SetupCoupon`.
- **Edge Cases:** Bấm **Hủy** ở dialog → không xóa, không audit. Trong lúc mở dialog, user khác phát mã cho coupon đó → SP guard trả `Deleted=false` + message "đã tồn tại N mã coupon" → Snackbar đỏ, không xóa.

### TC-L04 — Nút Xóa ẩn khi có mã (Negative/UI)
- **Scenario:** Không cho xóa coupon đã phát sinh mã.
- **Pre-condition:** Coupon có `QtyCoupon > 0`.
- **Steps:** Quan sát cột thao tác dòng đó.
- **Expected Result:** Chỉ có icon Sửa ✏, **không có icon Xóa**. (Guard 2 lớp: UI ẩn + SP chặn.)

### TC-L05 — SP chưa deploy (Negative)
- **Scenario:** Chưa chạy script SQL.
- **Pre-condition:** Xóa/không tồn tại `usp_SetupCoupon_GetList`.
- **Steps:** Mở `/promotion/coupons`.
- **Expected Result:** Banner đỏ "Không thể tải danh sách coupon. (Kiểm tra SP usp_SetupCoupon_* đã deploy chưa)". Ghi file log `CouponsPage.Load`. **App không crash**.

### TC-L06 — Điều hướng Sửa (Positive)
- **Steps:** Bấm icon Sửa ✏ ở 1 dòng.
- **Expected Result:** Chuyển sang `/promotion/coupons/issue?id={ItemNo}`, form nạp sẵn dữ liệu (xem TC-I06).

---

## 5. Test cases 8.2 — CouponIssuePage (`/promotion/coupons/issue`)

### 5.1 Positive

#### TC-I01 — Tạo coupon Auto hợp lệ
- **Scenario:** Phát hành coupon tự sinh mã.
- **Pre-condition:** Có SalesType, StoreGroup (có store), role Ops+.
- **Steps:** 1) Mở trang (không `?id`). 2) Nhập Tên phát hành. 3) Cách phát hành = "Tự động tạo mã". 4) Chọn Hình thức bán + Nhóm cửa hàng. 5) Từ ngày / Đến ngày. 6) Prefix = `TEST`, Kích thước mã = `10`, Số chữ cái = `2`, Vị trí đứng = `3`, Số lượng = `5`. 7) Bỏ tick "Áp dụng theo danh sách sản phẩm". 8) Bấm **Lưu**.
- **Expected Result:** Snackbar "Cập nhật thành công coupon C7...". Form reload sang chế độ Sửa (tiêu đề "Sửa coupon C7..."), field Auto **bị khóa** (`CodeFieldsLocked`). Tab "Mã coupon đã phát hành" hiển thị 5 mã (mỗi mã bắt đầu `TEST`, độ dài ≤ 20). DB: 1 dòng IssueRule + 1 Header + 5 CodeIssue + store rows theo group. Audit `CREATE / SetupCoupon`.
- **Edge Cases:** Xem E5 (Số lượng quá lớn), E6 (mã trùng).

#### TC-I02 — Import Excel hợp lệ
- **Steps:** 1) Cách phát hành = "Import excel". 2) Bấm **File mẫu** → tải file `.xlsx` (cột `CodeCoupon`). 3) Điền vài mã hợp lệ vào file. 4) Bấm **Chọn file Excel** → chọn file. 5) Điền Tên + ngày. 6) **Lưu**.
- **Expected Result:** Parse cột A (bỏ dòng 1 header) → lưu các mã. Snackbar thành công. Tab mã hiển thị đúng danh sách.
- **Edge Cases:** File `.xls` cũ, file có dòng trống xen kẽ (bỏ qua dòng rỗng hoàn toàn), file >10MB → xem E11.

#### TC-I03 — Thêm/Xóa sản phẩm
- **Steps:** 1) Tick "Áp dụng theo danh sách sản phẩm". 2) Tab "Danh sách sản phẩm" → **Thêm sản phẩm** → tìm → chọn nhiều → **Chọn**. 3) Xóa 1 dòng bằng icon ✖.
- **Expected Result:** Item thêm vào bảng (không trùng `ItemNo` — dedupe theo `ItemNo`). Xóa dòng → biến mất khỏi bảng ngay. Khi Lưu → ghi `CpnVchBOMLine` (replace toàn bộ theo ItemNo).
- **Edge Cases:** Chọn cùng item 2 lần → chỉ 1 dòng. Xem E3 (bỏ tick checkbox làm mất item).

#### TC-I04 — Cài đặt nâng cao
- **Steps:** 1) Nhập Tên + ngày (bắt buộc trước khi mở). 2) Bấm **Cài đặt nâng cao**. 3) Chọn ĐVT, Kiểu giảm giá = %, Giá trị = 10, các checkbox. 4) **Lưu** trong dialog.
- **Expected Result:** Dialog đóng, Snackbar thành công. `_model.ItemNo` được gán (nếu trước đó chưa có). Audit `UPDATE / SetupCouponAdvanced`. DB: Header cập nhật discount/limit/blocked.
- **Edge Cases:** Xem E1 (tạo coupon không mã), E8 (chặn ngày quá khứ chỉ ở luồng này).

#### TC-I05 — Sửa coupon đã có mã
- **Pre-condition:** Coupon `QtyCoupon > 0`.
- **Steps:** Vào qua `?id=`. Sửa Tên / thêm sản phẩm. **Lưu**.
- **Expected Result:** Cách phát hành + field Auto/Import **bị khóa** (không sinh lại mã — `needCodes=false`). Chỉ cập nhật Header + Line + Store. Audit `UPDATE / SetupCoupon`.
- **Edge Cases:** Xem E10 (ô Số lượng hiện 0).

#### TC-I06 — Nạp form khi Sửa (`?id=`)
- **Expected Result:** Tên, Cách phát hành, Hình thức bán, Nhóm CH, Từ/Đến ngày, checkbox, danh sách sản phẩm, số mã đã phát sinh — nạp đúng từ `GetDetailAsync`.

### 5.2 Negative (validate trong `CouponService`)

| TC | Thao tác | Expected (Snackbar/Banner) |
|---|---|---|
| TC-N01 | Bỏ trống Tên phát hành → Lưu | "Vui lòng nhập tên phát hành coupon" (Warning) |
| TC-N02 | Không chọn Từ ngày / Đến ngày → Lưu | "Vui lòng chọn ngày bắt đầu" / "...kết thúc" (Warning) |
| TC-N03 | Từ ngày > Đến ngày → Lưu | "TỪ NGÀY không lớn hơn ĐẾN NGÀY" (Error) |
| TC-N04 | Auto: Kích thước mã < 5 hoặc > 20 | "Kích thước mã từ 5->20 ký tự" |
| TC-N05 | Auto: LenCode + độ dài Prefix + Số chữ cái > 20 | "Tổng ký tự coupon đã vượt hơn 20" |
| TC-N06 | Auto: Số lượng ≤ 0 | "Vui lòng nhập số lượng phát hành" |
| TC-N07 | Auto/Import: mã đã tồn tại trong DB | "Mã coupon trùng trong DB (...)..." |
| TC-N08 | Import: không chọn file | "Vui lòng chọn file excel để import" |
| TC-N09 | Import: file rỗng (không có mã) | "Vui lòng kiểm tra file Excel, không có mã coupon" |
| TC-N10 | Import: có ô mã trống | "Vui lòng kiểm tra cột CodeCoupon, có giá trị trống" |
| TC-N11 | Import: mã chứa ký tự đặc biệt (vd `A@B`) | "Có N mã coupon ... có ký tự đặc biệt (...)" (regex `^[0-9\-_A-Za-z]*$`) |
| TC-N12 | Import: mã > 20 ký tự | "Có N mã coupon ... vượt quá 20 ký tự (...)" |
| TC-N13 | Import: mã trùng nhau trong file | "File excel có giá trị trùng (...)..." |
| TC-N14 | Tick "theo sản phẩm" nhưng danh sách rỗng → Lưu | "Vui lòng thêm sản phẩm vào voucher/coupon" |
| TC-N15 | Advanced: bỏ trống ĐVT | "Vui lòng chọn đơn vị tính" |
| TC-N16 | Advanced: Giá trị giảm ≤ 0 | "Vui lòng nhập giá trị giảm giá" |
| TC-N17 | Advanced: Kiểu = % và Giá trị > 100 | "Giá trị phần trăm giảm giá không lớn hơn 100" |
| TC-N18 | Advanced: tick "Sử dụng nhiều lần" + Số lần = 0 | "Vui lòng nhập số lần sử dụng" |
| TC-N19 | Advanced: Từ ngày < hôm nay | "TỪ NGÀY không được nhỏ hơn ngày hiện tại" (⚠️ chỉ luồng Advanced — xem E8) |

---

## 6. Edge Cases / Điểm yếu code (TRỌNG TÂM QA)

> Mỗi mục: **Mô tả** → **Cách tái hiện** → **Rủi ro** → **Đề xuất**.

### E1 — "Cài đặt nâng cao" là đường ghi thứ 2 (dual-write)
- **Mô tả:** `SaveAdvancedAsync` tự upsert `CpnVchBOMHeader` và **auto-gen ItemNo** nếu chưa có → tạo được 1 coupon **không có mã coupon nào** trước khi bấm nút "Lưu" chính.
- **Tái hiện:** Trang tạo mới → nhập Tên + ngày → **Cài đặt nâng cao** → Lưu (trong dialog) → thoát ra, **không** bấm "Lưu" chính. Vào lại list → đã có coupon `C7...` với `QtyCoupon = 0`.
- **Rủi ro:** Coupon "rác" không mã, IssueRule/Store có thể chưa nhất quán; audit tách entity `SetupCouponAdvanced` khó truy vết; người dùng tưởng chưa lưu.
- **Đề xuất:** Gộp Advanced vào 1 lần Save, hoặc chặn tạo mới từ Advance khi chưa có ItemNo.

### E2 — Audit UPDATE ghi `oldValue` sai
- **Mô tả:** Trong `SaveAsync`, `before = JsonConvert.SerializeObject(_model)` được lấy **sau khi** đã gán `StartingDateStr/EndingDateStr/IsCheckItem/Items` vào `_model`. Nên `oldValue ≈ newValue`.
- **Tái hiện:** Sửa 1 coupon, đổi Tên, Lưu → mở bảng audit → cột old và new gần như giống nhau (mất giá trị gốc).
- **Rủi ro:** Audit trail không phản ánh thay đổi thực → vô dụng khi truy vết.
- **Đề xuất:** Snapshot `oldValue` từ dữ liệu detail load ban đầu (trước mọi mutation), giống chuẩn `PosDataSetupPage`.

### E3 — Mất sản phẩm ngầm khi bỏ tick checkbox
- **Mô tả:** `_model.Items = _applyPerItem ? _items : []`. Nếu tab đã thêm sản phẩm rồi bỏ tick "Áp dụng theo danh sách sản phẩm", khi Lưu item **bị loại bỏ không cảnh báo**.
- **Tái hiện:** Tick checkbox → thêm 3 sản phẩm → bỏ tick → Lưu → mở lại: sản phẩm mất.
- **Rủi ro:** Mất cấu hình vô ý; coupon áp dụng sai phạm vi (tổng hóa đơn thay vì theo SP).
- **Đề xuất:** Hỏi xác nhận khi bỏ tick mà `_items` không rỗng, hoặc giữ item và cảnh báo.

### E4 — Nhóm cửa hàng map 0 store (im lặng)
- **Mô tả:** Chọn GroupCode không có store active → SP chèn 0 dòng `CpnVchBOMStore`.
- **Tái hiện:** Tạo/sửa `dbo.StoreGroup` để 1 GroupCode có toàn store `Status=0` (hoặc rỗng) → chọn group đó → Lưu → kiểm tra `CpnVchBOMStore` = 0 dòng.
- **Rủi ro:** Coupon không phủ cửa hàng nào → POS không nhận, khó phát hiện.
- **Đề xuất:** Cảnh báo khi group không sinh ra store nào; hoặc trả về số store đã gán trong message.

### E5 — Số lượng (Quantity) không chặn trần
- **Mô tả:** Không giới hạn tối đa. Vòng sinh mã chạy `Quantity` lần trong bộ nhớ + build TVP + insert.
- **Tái hiện:** Auto, Số lượng = `1000000` → Lưu.
- **Rủi ro:** Treo Blazor circuit, timeout SP (300s), ngốn RAM, có thể ảnh hưởng user khác (Blazor Server chia sẻ tài nguyên).
- **Đề xuất:** Đặt trần (vd ≤ 100.000) + chạy nền/queue cho lô lớn.

### E6 — Collision mã Auto
- **Mô tả:** LenCode ngắn (≤ 13) + `CharOfNumber = 0` → mã = N chữ số cuối của timestamp+index; hai lần Lưu sát nhau có thể trùng nhau.
- **Tái hiện:** Auto, Kích thước mã = 5, Số chữ cái = 0, Số lượng = 50 → Lưu nhiều lần liên tiếp.
- **Rủi ro:** DB check (`CheckCodesExist`) trả lỗi "vui lòng chờ trong ít phút để tạo lại" — **không tự retry**, người dùng phải bấm lại.
- **Đề xuất:** Tăng entropy (bắt buộc CharOfNumber > 0 khi LenCode nhỏ) hoặc auto-retry sinh mã.

### E7 — `?id=` không tồn tại
- **Mô tả:** `GetDetailAsync` trả null → set `_errorMsg` nhưng `_model` vẫn là object mặc định (ItemNo rỗng) → `IsEditing=false` → form hiển thị như "tạo mới".
- **Tái hiện:** Mở `/promotion/coupons/issue?id=KHONGTONTAI`.
- **Rủi ro:** Người dùng thấy banner lỗi nhưng vẫn có thể nhập & Lưu → tạo coupon mới ngoài ý muốn.
- **Đề xuất:** Khi id không hợp lệ → chặn form hoặc điều hướng về list.

### E8 — Lệch validate ngày giữa 2 luồng Lưu
- **Mô tả:** "Lưu" (issue) **cho phép** Từ ngày trong quá khứ; "Cài đặt nâng cao" **chặn** `start < today`.
- **Tái hiện:** Đặt Từ ngày = hôm qua → "Lưu" chính OK; nhưng mở "Cài đặt nâng cao" → Lưu → lỗi "TỪ NGÀY không được nhỏ hơn ngày hiện tại".
- **Rủi ro:** Trải nghiệm mâu thuẫn, khó hiểu quy tắc nghiệp vụ.
- **Đề xuất:** Thống nhất 1 quy tắc ngày cho cả 2 luồng.

### E9 — Double-click "Lưu" / mất mạng giữa chừng
- **Mô tả:** Nút Lưu `Disabled="_saving"`; `_saving` set true khi bắt đầu, reset trong `finally`.
- **Tái hiện:** (a) Bấm Lưu liên tục thật nhanh. (b) Ngắt mạng ngay sau khi bấm Lưu.
- **Expected:** (a) Chỉ 1 request → 1 bản ghi (không tạo trùng). (b) Circuit "reconnecting"; nếu circuit chết, state (`_model`, `_saving`) mất → phải nhập lại; kiểm tra **không** ghi 2 lần vào DB.
- **Rủi ro:** Với Auto sinh mã, double-submit trước khi disable kịp có thể tạo 2 lô mã (khả năng thấp nhưng cần verify).
- **Đề xuất:** Thêm token idempotency hoặc kiểm tra tồn tại trước khi tạo.

### E10 — Ô "Số lượng" hiện 0 khi sửa
- **Mô tả:** `LoadDetailAsync` không nạp `_model.Quantity` → ô Số lượng = 0 khi sửa coupon đã có mã (dù ô đang bị khóa).
- **Rủi ro:** Người dùng hiểu nhầm coupon có 0 mã (trong khi số mã thật xem ở tab "Mã coupon").
- **Đề xuất:** Hiển thị `QuantityCode` (số mã thực) hoặc ẩn ô khi khóa.

### E11 — Import chỉ parse lúc Save
- **Mô tả:** File Excel chỉ được đọc/validate khi bấm **Lưu**, không phải lúc chọn file. Giới hạn `maxAllowedSize = 10MB`.
- **Tái hiện:** Chọn file `.txt` đổi đuôi `.xlsx`, hoặc file > 10MB → Lưu.
- **Expected:** Sai định dạng → "Không đọc được file Excel...". File > 10MB → `OpenReadStream` ném lỗi → cùng thông báo.
- **Đề xuất:** Validate & preview ngay khi chọn file; báo rõ giới hạn 10MB.

### E12 — Concurrency / không optimistic lock
- **Mô tả:** 2 user sửa cùng 1 coupon → last-write-wins (không kiểm tra version/Counter).
- **Tái hiện:** 2 phiên mở cùng `?id=C7...`, cùng sửa & Lưu.
- **Rủi ro:** Ghi đè thầm lặng.
- **Đề xuất:** Dùng `Counter`/`LastDateModified` làm optimistic concurrency token.

---

## 7. Checklist regression nhanh

```
□ List load + phân trang + STT liên tục qua trang
□ Filter (mã/tên/cách phát hành/hiệu lực) + nút Xóa reset về "Tất cả"
□ Nút Xóa chỉ hiện khi QtyCoupon==0; xóa → audit DELETE
□ Auto: tạo mới hợp lệ → 5 mã đúng prefix/độ dài; field khóa sau lưu
□ Import: tải file mẫu; import file hợp lệ; các case lỗi N09–N13
□ Advanced: mở dialog, validate N15–N19, lưu OK
□ Sửa coupon có mã: field code khóa, chỉ update header/line/store
□ Toàn bộ validate Negative TC-N01..N19 hiện đúng thông báo
□ Điểm yếu E1–E12: xác nhận hành vi thực tế + log lại để dev vá
□ SP chưa deploy → banner đỏ, app không crash
```

## 8. Ghi chú môi trường

- **Bắt buộc** deploy: `docs/sql/SetupCoupon_Read.sql`, `SetupCoupon_Save.sql`, `SetupCoupon_Delete.sql` (xem `docs/ROLLOUT.md` §D3).
- Bảng audit: chạy `src/POS.Web/Auth/migration_dashboard_audit_log.sql` trên `RPOSMasterData` (nếu chưa → audit fail-safe, không crash).
- Role test: **ITOps** hoặc **SystemAdmin** (policy `OpsAndAbove`). StoreOperator sẽ bị chặn (403/AccessDenied).
- Blazor Server: mọi test cần lưu ý **circuit** (mất mạng, reload tab làm mất state form đang nhập).

---

## 9. Ghi chú kỹ thuật (Dành cho Dev)

> Đọc kèm mã nguồn: `CouponsPage.razor`, `CouponIssuePage.razor`, `CouponService.cs`,
> `CouponRepository.cs`, `CouponVoucherCodeGenerator.cs`, `docs/sql/SetupCoupon_Save.sql`,
> `docs/sql/SetupCoupon_Delete.sql`. Mục đích: giải thích **tại sao** code chạy như vậy, không
> lặp lại nghiệp vụ đã mô tả ở mục 1-8.

### 9.1 Data Flow — cơ chế lưu (SP `usp_SetupCoupon_SaveIssue`, 1 transaction `XACT_ABORT ON`)

SP **không** replace toàn bộ dữ liệu con giống nhau — mỗi bảng có chiến lược ghi khác nhau, đây là
điểm dễ hiểu nhầm nhất khi debug:

| Bảng | Chiến lược | Vì sao |
|---|---|---|
| `CpnVchBOMIssueRule` | **Upsert** theo `ItemNo` | Metadata sinh mã (Prefix/LenCode...), 1 dòng/coupon |
| `CpnVchBOMHeader` | **Upsert** theo `ItemNo`, bump `Counter` | Header chính, đọc bởi `usp_SyncTable_Get` cho POS sync |
| `CpnVchBOMCodeIssue` | **Insert-once** — `IF NOT EXISTS (... AND Source='COUPON')` mới insert; **KHÔNG BAO GIỜ xóa** | Mã đã phát cho khách không được xóa/tạo lại khi user chỉ sửa Tên/ngày. Section 3b đồng bộ lại field "chụp nhanh" (`ArticleType`, `Validity_From_Date`, `Expiry_Date`) ở **mọi lần gọi SP**, nhưng **tuyệt đối không đụng `Status`/`Enabled`/`[Return]`** — nếu đụng, sửa coupon sau khi đã có mã bị khách redeem sẽ vô tình reset mã đó về `Status='SOLD'`/`Enabled=1`, phá vỡ chống double-redeem ở `POS.Api` (`usp_Voucher_Redeem`) |
| `CpnVchBOMLine` (sản phẩm) | **True replace-on-save** — `DELETE ... WHERE ItemNo=@ItemNo` rồi insert lại toàn bộ từ TVP `dbo.CouponLineTVP` | Danh sách sản phẩm áp dụng không có ý nghĩa lịch sử, ghi đè an toàn |
| `CpnVchBOMStore` (cửa hàng áp dụng) | **True replace-on-save** — xóa rồi insert lại (`ALL` → 1 dòng, hoặc bung theo `dbo.StoreGroup`) | Xem thêm mục 3 "Nhóm cửa hàng" + E4 |

**Bẫy kỹ thuật quan trọng nhất**: điều kiện `AND Source = 'COUPON'` ở bước 3 và 3b là **bắt buộc**.
Bảng `CpnVchBOMCodeIssue` dùng **chung** với SAP Voucher (cột `ActicleNo` mirror `ItemNo`). Nếu
thiếu điều kiện này, một `ActicleNo` SAP trùng chuỗi với `ItemNo` coupon sẽ khiến:
- Bước 3 (`IF NOT EXISTS`) bị "lừa" là coupon đã có mã → **vĩnh viễn không insert code** cho coupon đó.
- Bước 3b ghi đè `ActicleType`/ngày hiệu lực của dòng SAP Voucher bằng dữ liệu coupon → **corrupt
  dữ liệu voucher SAP thật**.

### 9.2 Field bắt buộc & validation logic

- **Validate diễn ra ở 2 nơi, KHÔNG đối xứng**:
  1. **Blazor (`ValidateHeaderFields`, `CouponIssuePage.razor`)** — chỉ check tối thiểu (Description
     rỗng, ngày rỗng, `From >= To`, % giảm giá 0-100) để UX phản hồi nhanh **trước khi** mở dialog
     "Phát hành coupon" (tránh user điền dialog xong mới biết header sai).
  2. **`CouponService` (Application layer)** — nguồn sự thật thật sự, chạy lại **toàn bộ** rule kể
     cả những rule Blazor không check (độ dài mã 5-20, tổng ký tự ≤ 20, trùng mã DB, regex mã Import,
     ràng buộc sản phẩm vs `IsCheckItem`...). Client bypass được Blazor validate (vd sửa DOM, gọi
     thẳng qua reflection test) vẫn bị chặn ở đây — **không được xóa/nới lỏng validate ở
     `CouponService` dù trùng lặp với Blazor.**
  - Danh sách đầy đủ message & điều kiện: xem mục 5.2 (TC-N01..N19).
- **Ràng buộc `IsCheckItem` hai chiều** (`CouponService.SaveIssueAsync`): `IsCheckItem=true` mà
  `Items.Count==0` → lỗi (thiếu sản phẩm); `IsCheckItem=false` mà `Items.Count>0` → **cũng lỗi**
  ("đang áp dụng tổng hóa đơn, vui lòng xóa danh sách sản phẩm") — đây là guard Service-side, khác
  với hành vi im lặng ở `SaveAsync` phía Blazor (E3) vì Blazor **tự set `Items=[]` trước khi gọi
  Service** nên guard chiều 2 hiếm khi kích hoạt qua UI thường — chỉ lộ ra khi gọi Service trực tiếp
  (test/API).
- **`needCodes`** (cả 2 lớp: Blazor `NeedsCodeDialog` và Service `SaveIssueAsync`) dùng chung điều
  kiện `string.IsNullOrWhiteSpace(ItemNo) || QuantityCodeInDB == 0` — sinh/validate mã **chỉ** chạy
  khi tạo mới hoặc coupon hiện tại chưa có mã nào; nếu đã có mã, mọi lần Lưu sau chỉ update
  Header/Line/Store (khớp Section 3 SP chỉ insert-once).

### 9.3 Audit Log

- `IAuditLogger.LogAsync` gọi **sau khi** DB write trả `Ok=true` — không log khi thất bại (đúng
  chuẩn `.claude/skills/web/audit-logging.md`).
- **2 entity audit tách biệt cho cùng 1 `ItemNo`**: `"SetupCoupon"` (issue/blocked) và
  `"SetupCouponAdvanced"` (advanced/discount) — vì `SaveIssueAsync` và `SaveAdvancedAsync` là 2 SP
  độc lập (xem E1). Khi truy vết 1 coupon phải xem **cả 2** entity trong bảng audit, không chỉ 1.
- **Actor** lấy từ `AuthState.User.Identity?.Name` (claims cookie) — fallback `"unknown"` nếu
  null (hiếm khi xảy ra vì trang đã có `[Authorize]`).
- **oldValueJson bị lệch (bug đã biết — E2)**: trong `SaveAsync`, `before =
  JsonConvert.SerializeObject(_model)` được lấy **sau khi** `_model.StartingDateStr/EndingDateStr/
  IsCheckItem/Items` đã bị gán giá trị mới → `oldValueJson ≈ newValueJson`. Dev debug audit trail
  thấy "old và new giống hệt nhau" — đây là bug ghi nhận sẵn ở mục 6 (E2), không phải lỗi log.
- Riêng `SaveBlockedAsync` (Xem coupon → đổi Blocked) snapshot `oldJson` **đúng** (lấy trước khi
  gọi `UpdateBlockedAsync`) — không dính bug E2, vì đây là field đơn lẻ không qua `_model` chung.

---

## 10. Technical Deep Dive

### Dependency (Service/Repository được inject)

| Component | Inject | Vai trò |
|---|---|---|
| `CouponsPage.razor` | `ICouponService`, `IFileLogHelper`, `ISnackbar`, `IAuditLogger`, `NavigationManager` | List + xóa (soft — qua `Blocked`, xem TC-L03) |
| `CouponIssuePage.razor` | `ICouponService`, `IDialogService`, `ISnackbar`, `IAuditLogger`, `IFileLogHelper`, `NavigationManager`, `IJSRuntime` (`SaveAsFileAsync` — tải file mẫu Excel) | Form phát hành; `ClosedXML` (`XLWorkbook`) dùng **trực tiếp trong component**, không qua Service — parse/generate Excel là I/O cục bộ phía Blazor, không phải business logic |
| `CouponService` (`POS.Application.Features.CouponVoucher`) | `ICouponRepository`, `ICentralMDRepository` (item picker — tái dùng `GetProductListAsync`, migrate 6.1), `IVoucherIssueLock` (Redis distributed lock, dùng chung với Voucher) | Validate + sinh mã (`CouponVoucherCodeGenerator`, static, không inject) + orchestrate save |
| `CouponRepository` (`POS.Infrastructure.Repositories`) | `CentralMDConnectionFactory` (Dapper, không qua interface) | Gọi SP `usp_SetupCoupon_*` trên `RPOSMasterData` |

### Constraint (giới hạn của form)

- **Nút Lưu** disable khi `_saving == true` — đây là guard **UI-level only**, không có idempotency
  token phía server. Double-click rất nhanh (trước khi Blazor re-render `Disabled`) vẫn có thể gửi
  2 request `SaveAsync` (xem E9). Với Auto issue, request thứ 2 thường bị chặn bởi
  `CheckCodesExistAsync` (mã đã tồn tại) nhưng **không đảm bảo tuyệt đối** vì không có lock ở luồng
  này (xem gạch dưới, khác `IssueMoreAsync`).
- **`CodeFieldsLocked`** (`IsEditing && _quantityCodeInDb > 0`) khóa "Cách phát hành" + field
  Auto/Import — ngăn đổi `IssueType` sau khi đã có mã, vì SP chỉ insert code 1 lần (Section 3) và
  không xử lý việc đổi chiến lược sinh mã giữa chừng.
- **`DiscountValueMax`** (`_advanced.DiscountType == 1 ? 100 : double.MaxValue`) chỉ enforce ở
  client (`MudNumericField Max=`) — server (`CouponService.SaveAdvancedAsync`) validate lại
  `DiscountValue > 100 && DiscountType == 1` độc lập, 2 lớp giống mọi rule khác ở mục 9.2.
- **Redis lock (`IVoucherIssueLock`, key cố định `Lock:VoucherIssue` toàn hệ thống)**: TTL 30s, poll
  300ms, `MaxWait` 15s — hết 15s chưa acquire được → `Fail("Hệ thống đang xử lý phát hành coupon
  khác, vui lòng thử lại sau.")`.
  ⚠️ **Lock chỉ bọc `IssueMoreAsync`** ("Phát hành thêm" cho coupon đã tồn tại) — **KHÔNG bọc
  `SaveIssueAsync`** (tạo mới hoặc sửa header, kể cả khi cần sinh mã Auto lần đầu). Nghĩa là 2 user
  cùng tạo 2 coupon Auto **mới** đồng thời vẫn có nguy cơ va chạm mã (rủi ro thấp hơn nhờ
  `RandomNumberGenerator` crypto-strength, nhưng về lý thuyết không được serialize như luồng "phát
  hành thêm"). Đây là gap thực tế cần biết khi điều tra race condition ở E6/E9, rộng hơn mô tả ban
  đầu trong E6 (E6 chỉ nói về `IssueMoreAsync`).

### Error Handling (cách ứng dụng bắt lỗi từ SP)

- **`CouponService` bắt `try/catch` quanh mọi lệnh ghi** (`SaveIssueAsync`, `SaveAdvancedAsync`,
  `IssueMoreAsync`) và trả thẳng `Fail(ex.Message)` ra Snackbar. Hệ quả: **message exception SQL
  gốc (kể cả lỗi kỹ thuật như timeout, constraint violation, connection lost) hiển thị trực tiếp
  cho end-user** — không có lớp "friendly message" nào ở tầng Application cho nhánh lỗi ngoài dự
  kiến. Khác với API thường (nơi `ExceptionHandlingMiddleware` toàn cục xử lý exception chưa bắt),
  ở đây exception **đã bị nuốt sớm hơn** trong `CouponService` nên middleware không có cơ hội can
  thiệp — đây là hành vi có chủ đích để user thấy lý do lỗi SP (vd trigger/constraint), nhưng cũng
  là điểm QA cần khai thác (lộ thông tin kỹ thuật nội bộ ra Snackbar).
- **Đọc dữ liệu** (`GetHeaderListAsync`, `GetDetailAsync`, `GetFormLookupAsync`) — page tự
  `try/catch` quanh lời gọi Service (Blazor Server không có HTTP response pipeline để middleware
  can thiệp), set `_errorMsg` hiển thị banner đỏ + ghi `FileLogger.WriteExpLogs` + **không crash
  circuit** (xem TC-L05).
- **`DeleteAsync`**: SP `usp_SetupCoupon_Delete` luôn trả đúng 1 dòng `(Deleted bit, Message
  nvarchar)` cho mọi nhánh (không tìm thấy / còn mã / xóa thành công / lỗi transaction — dùng
  `ERROR_MESSAGE()` trong `CATCH` thay vì `THROW`, khác hẳn `usp_SetupCoupon_SaveIssue`/
  `SaveAdvanced` dùng `THROW`). Repository fallback `(false, "Không xóa được coupon")` chỉ khi SP
  không trả dòng nào (không nên xảy ra theo thiết kế SP, nhưng là defensive code phía C#).
- **Mọi SP ghi** (`SaveIssue`, `SaveAdvanced`) dùng `SET XACT_ABORT ON` + `TRY/CATCH` +
  `ROLLBACK TRANSACTION` + `THROW` — đảm bảo Header/IssueRule/Code/Line/Store không bao giờ ghi dở
  dang (all-or-nothing), nhưng lỗi ném lên C# vẫn là raw `SqlException.Message` như mô tả ở gạch
  đầu dòng đầu tiên.

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
| Route | `/promotion/coupons` (list), `/promotion/coupons/issue` (form tạo mới), `/promotion/coupons/issue?id={ItemNo}` (sửa), `/promotion/coupons/issue?id={ItemNo}&mode=view` (xem — xem mục 3.1) |
| Render mode | `InteractiveServer` (Blazor Server — có circuit/WebSocket) |
| Policy | `WebPolicies.BackOfficeAndAbove` — **BackOffice + ITOps + SystemAdmin** (`CouponIssuePage.razor:2`, `CouponsPage.razor:2`; `WebRoles.cs:11-16` xác nhận hệ thống đã có 4 role: StoreOperator/BackOffice/ITOps/SystemAdmin — **StoreOperator KHÔNG vào được**, nhưng role `BackOffice` CÓ vào được, khác với ghi chú cũ "OpsAndAbove" chỉ gồm ITOps+SystemAdmin) |
| Luồng | `Page → ICouponService → ICouponRepository → SP usp_SetupCoupon_*` (DB `RPOSMasterData`) |
| Sinh mã Auto | Ở tầng Application (`CouponService`, C#), KHÔNG ở SP |
| Bảng tác động | `CpnVchBOMIssueRule`, `CpnVchBOMHeader`, `CpnVchBOMCodeIssue`, `CpnVchBOMLine`, `CpnVchBOMStore` |
| Item picker | Tái dùng `ICentralMDRepository.GetProductListAsync` (migrate 6.1) |
| Audit | `IAuditLogger.LogAsync` sau mỗi thao tác ghi thành công (CREATE/UPDATE/DELETE) |

**Pre-condition chung cho MỌI test:**
- Đã deploy 3 script: `docs/sql/SetupCoupon_Read.sql`, `SetupCoupon_Save.sql`, `SetupCoupon_Delete.sql` trên `RPOSMasterData`.
- Đã chạy migration bảng audit (`migration_dashboard_audit_log.sql`) — nếu chưa, audit fail-safe (không crash).
- Đăng nhập bằng user role **BackOffice**, **ITOps** hoặc **SystemAdmin** (policy
  `WebPolicies.BackOfficeAndAbove` — xem G0 mục 1, đã sửa từ "OpsAndAbove" ghi sai ở bản cũ).
- Có sẵn dữ liệu: ≥1 `SalesOrderType(IsActive=1)`, ≥1 `StoreGroup(Status=1)` có store, ≥1 `UnitOfMeasure`, ≥1 `Item(Blocked=0)`.

---

## 2. Ma trận chức năng ↔ điều kiện hiển thị

| Thành phần | Điều kiện hiển thị / enable | Nguồn code |
|---|---|---|
| Nút **Xóa** (list) | Chỉ hiện khi `QtyCoupon == 0` | `CouponsPage.razor` `@if (context.QtyCoupon == 0)` |
| Field Auto (Prefix/LenCode/CharOfNumber/CharPosition/Quantity) | **KHÔNG còn hiện trên form chính** — thu thập qua dialog "Phát hành mã coupon" khi bấm Lưu (coupon mới/chưa có mã) hoặc "PHÁT HÀNH THÊM" (coupon đã tồn tại) | `CouponIssuePage.razor:132-134` (comment xác nhận), `Dialogs/CouponIssueMoreDialog.razor` |
| Khối Import (upload + file mẫu) | Chỉ hiện khi `IssueType == "Import"` **và không phải View mode** | `@if (!IsAuto && !IsViewMode)` (dòng 135) |
| "Cách phát hành" (Select) | Disable khi `CodeFieldsLocked` **hoặc** `IsViewMode` | dòng 115 — doc bản cũ thiếu vế `IsViewMode` |
| `CodeFieldsLocked` (khóa Cách phát hành + field Auto/Import) | `IsEditing && _quantityCodeInDb > 0` | `CodeFieldsLocked` |
| Alert "Tick 'Áp dụng theo danh sách sản phẩm'..." | Hiện khi `!_applyPerItem && _quantityCodeInDb == 0` (KHÔNG phải điều kiện `IsEditing` như bản cũ ghi) | dòng 214 |
| Tab **Danh sách sản phẩm** | Hiện khi `_applyPerItem == true` (độc lập với `IsEditing`) | dòng 224 |
| Tab **Mã coupon đã phát hành** | Hiện khi `_quantityCodeInDb > 0` (KHÔNG phải `IsEditing` như bản cũ ghi — 1 coupon mới tạo trong cùng phiên vẫn `_quantityCodeInDb == 0` cho tới khi Lưu xong và mở lại) | dòng 273 |
| Nút **Lưu** (chính) | Chỉ hiện khi `!IsViewMode`; disable khi `_saving == true` | dòng 62-76 |
| Nút **PHÁT HÀNH THÊM** | Chỉ hiện khi `IsViewMode` (xem mục 3.1) | dòng 38-43 |
| Nút **Lưu** (View mode) | Chỉ hiện khi `IsViewMode && BlockedChanged` — dùng để lưu riêng field Blocked | dòng 44-60 |
| Tiêu đề trang | 3 nhánh: `IsViewMode` → "Xem coupon {ItemNo}"; else `IsEditing` → "Sửa coupon {ItemNo}"; else "Phát hành Coupon" (bản cũ chỉ ghi 2 nhánh, thiếu View mode) | dòng 32 |

> `IsEditing = !string.IsNullOrWhiteSpace(_model.ItemNo)`. `IsViewMode = Mode == "view"` (query
> string `?mode=view`) — xem mục 3.1 "Chế độ Xem" (mới bổ sung) cho toàn bộ hành vi liên quan.

---

## 3. Logic đặc biệt — chọn "Nhóm cửa hàng" (StoreGroupCode)

Khi bấm **Lưu** (SP `usp_SetupCoupon_SaveIssue`), bảng `CpnVchBOMStore` được ghi lại (replace) theo quy tắc:

| Chọn Nhóm cửa hàng | Kết quả trong `CpnVchBOMStore` |
|---|---|
| `ALL` | Chèn **1 dòng** `StoreNo = 'ALL'` |
| Một GroupCode cụ thể | **Bung** thành N dòng — mỗi `StoreNo` thuộc `dbo.StoreGroup WHERE GroupCode = @code` |

⚠️ **Điểm yếu E4:** nếu GroupCode được chọn không map tới store nào (group rỗng hoặc store `Status=0`) → **0 dòng** store được chèn → coupon **không áp dụng cửa hàng nào** mà UI **không cảnh báo**. Xem TC ở mục 6.

---

## 3.1 Chế độ Xem (`?mode=view`) — [BỔ SUNG, trước đây thiếu hoàn toàn]

> Toàn bộ nhánh hành vi này chưa từng được tài liệu hóa ở các bản trước — đối chiếu trực tiếp với
> `CouponIssuePage.razor` để bổ sung. Entry point: icon "Xem chi tiết" (👁, tooltip "Xem chi tiết")
> trên `CouponsPage.razor` → điều hướng `/promotion/coupons/issue?id={ItemNo}&mode=view`.

- **Tiêu đề**: "Xem coupon {ItemNo}" (khác "Sửa coupon .../Phát hành Coupon").
- **Toàn bộ field bị khóa** (`ReadOnly`/`Disabled` theo `IsViewMode`): Tên phát hành, Cách phát
  hành, Từ/Đến ngày, Hình thức bán hàng, Nhóm cửa hàng, Kiểu giảm giá, Giá trị giảm giá, Giảm tối
  đa, checkbox "Áp dụng theo danh sách sản phẩm". Bảng sản phẩm ẩn nút Thêm/Xóa dòng.
- **Ngoại lệ DUY NHẤT vẫn sửa được**: checkbox "Khóa (Blocked)" (dòng 206, comment code ghi rõ
  "Xem coupon: Blocked là ngoại lệ DUY NHẤT được sửa").
- **Bộ nút header đổi hẳn** (dòng 38-61):
  - **"PHÁT HÀNH THÊM"** (luôn hiện) — mở lại dialog "Phát hành mã coupon" (`CouponIssueMoreDialog`,
    title tham số "Phát hành thêm mã coupon") để sinh **thêm 1 lô mã Auto mới** cho coupon đã tồn
    tại, gọi `CouponService.IssueMoreAsync` — có **Redis distributed lock** `IVoucherIssueLock`
    (key cố định `Lock:VoucherIssue`, TTL 30s, poll 300ms, `MaxWait` 15s — xem mục 10) để tránh va
    chạm mã giữa nhiều lượt "phát hành thêm" đồng thời.
  - **"Lưu"** — chỉ hiện khi `BlockedChanged` (đã đổi checkbox Blocked so với giá trị gốc), gọi
    `SaveBlockedAsync` (dòng 602-630) — **một hàm lưu riêng biệt thứ 3** (khác `SaveIssueAsync`/
    `SaveAdvancedAsync`), chỉ update field `Blocked` qua `CouponService.UpdateBlockedAsync`, audit
    snapshot `oldJson` **ĐÚNG** (lấy trước khi gọi update — không dính bug E2, xem mục 9.3).

### TC-I07 — Xem coupon: field khóa + đổi Blocked (Positive)
- **Pre-condition:** Có ≥1 coupon đã tồn tại (bất kỳ trạng thái).
- **Steps:** 1) Từ list, bấm icon "Xem chi tiết". 2) Quan sát toàn bộ field. 3) Tick/bỏ tick
  "Khóa (Blocked)". 4) Bấm **Lưu**.
- **Expected Result:** Mọi field khác Blocked là `ReadOnly`/`Disabled`. Nút "Lưu" **chỉ xuất hiện
  sau khi đổi Blocked** (trước đó chỉ có "PHÁT HÀNH THÊM"). Sau Lưu: Snackbar thành công, audit
  `UPDATE / SetupCoupon` với `oldValueJson` phản ánh đúng giá trị Blocked gốc (khác bug E2).
- **Edge Cases:** Bấm Lưu khi Blocked chưa đổi → nút không hiện, không thể bấm nhầm.

### TC-I08 — Xem coupon: PHÁT HÀNH THÊM mã Auto (Positive)
- **Pre-condition:** Coupon `IssueType == "Auto"` đã có ≥1 mã.
- **Steps:** 1) Từ list, bấm icon "Xem chi tiết". 2) Bấm **PHÁT HÀNH THÊM**. 3) Điền Prefix/Kích
  thước mã/Số chữ cái/Vị trí đứng/Số lượng trong dialog. 4) Bấm **PHÁT HÀNH**.
- **Expected Result:** Ở lại trang (không điều hướng đi), tab "Mã coupon đã phát hành" tự
  `ReloadServerData()` hiển thị thêm N mã mới, Snackbar thành công, audit `ISSUE / SetupCoupon`.
- **Edge Cases:** 2 tab/2 phiên cùng bấm "PHÁT HÀNH THÊM" cho **cùng 1 coupon** gần như đồng thời →
  Redis lock `Lock:VoucherIssue` serialize hóa, phiên thứ 2 chờ tới `MaxWait=15s` rồi mới chạy hoặc
  nhận lỗi "Hệ thống đang xử lý phát hành coupon khác, vui lòng thử lại sau." nếu hết thời gian chờ.
  **Lưu ý:** lock này **CHỈ** bọc `IssueMoreAsync` — **KHÔNG** bọc `SaveIssueAsync` (tạo coupon Auto
  mới lần đầu, xem mục 10 "Constraint").

---

## 4. Test cases 8.1 — CouponsPage (`/promotion/coupons`)

> ⚠️ **[BỔ SUNG — G7, phát hiện khi đọc lại code thật `CouponsPage.razor` để viết auto-test cho
> mục 5]** Trang **List đã được viết lại hoàn toàn** so với mô tả gốc bên dưới (cột bảng, filter
> panel, và cơ chế Xóa **khác hẳn** những gì TC-L01–L04/L06 mô tả) — nhiều khả năng doc này được
> viết cho 1 phiên bản List cũ trước khi redesign. Phạm vi yêu cầu ban đầu chỉ audit
> `CouponIssuePage.razor` (8.2) nên **KHÔNG** làm lại toàn bộ ma trận TC-L01/L02 ở đây (cần 1 lượt
> audit riêng cho `CouponsPage.razor` nếu cần), nhưng 3 sự thật sau **ảnh hưởng trực tiếp** đến
> luồng CouponIssuePage (entry point + auto-test) nên phải sửa ngay:
> 1. **KHÔNG có icon "Sửa" (Edit) nào trên list** — cột "Thao tác" chỉ có 2 icon: "Xem chi tiết"
>    (`Icons.Material.Filled.Visibility` → `?id={ItemNo}&mode=view`) và "Xóa"
>    (`Icons.Material.Filled.Delete`, xem điểm 2). **TC-L06 cũ (điều hướng Sửa qua icon) sai** — xem
>    bản sửa bên dưới.
> 2. **"Xóa" không phải hard-delete** — `DeleteAsync` (`CouponsPage.razor:183-203`) gọi
>    `CouponService.UpdateBlockedAsync(item.ItemNo, true)` (soft-block, **giống hệt cơ chế** đổi
>    checkbox "Khóa (Blocked)" ở View mode) — **KHÔNG** gọi `usp_SetupCoupon_Delete`/xóa khỏi
>    `CpnVchBOMIssueRule`/`CpnVchBOMHeader` như TC-L03/TC-L04 mô tả. Icon Xóa **luôn hiển thị**,
>    không có điều kiện `QtyCoupon == 0` nào cả (TC-L03/TC-L04 toàn bộ đã lỗi thời — audit riêng
>    nếu cần dùng lại, không sửa chi tiết ở đây vì ngoài phạm vi task).
> 3. **Filter panel thực tế** chỉ có 2 field: "Từ khóa (mã / tên / mã coupon)" (1 ô gộp, không tách
>    riêng Mã/Tên) + "Hiệu lực" (dropdown `"-1"`=Tất cả/`"0"`=Hiệu lực **[mặc định]**/`"1"`=Hết hiệu
>    lực) — **không có** filter "Cách phát hành" như TC-L02 mô tả.
> 4. **Cột bảng thực tế**: STT, Thao tác, Mã, Tên, Loại giảm, Giá trị giảm, SL tối đa, Tổng bill,
>    Từ ngày, Đến ngày, Hiệu lực — **không có** cột Cách phát hành/Tiền tố/Kích thước mã/Số chữ
>    cái/Vị trí đứng/Số lượng như TC-L01 liệt kê (số mã đã phát chỉ xem được trong tab "Mã coupon
>    đã phát hành" của trang Issue, không hiện ở list).
> 5. **Hệ quả quan trọng nhất cho CouponIssuePage**: vì không có icon Sửa, chế độ **"Sửa" (Edit,
>    không phải View)** — `?id={ItemNo}` **KHÔNG kèm** `&mode=view` — **chỉ truy cập được bằng cách
>    gõ thẳng URL**, không có bất kỳ nút/link nào trong toàn bộ POS.Web dẫn tới đó (đã grep toàn bộ
>    `src/POS.Web` xác nhận chỉ có 2 điều hướng thật: `/promotion/coupons/issue` (tạo mới) và
>    `/promotion/coupons/issue?id={ItemNo}&mode=view` (xem)). TC-I05/TC-I06 (mục 5.1) **vẫn đúng về
>    mặt hành vi code** (`CodeFieldsLocked`, form nạp từ `GetDetailAsync`...) nhưng **Pre-
>    condition/Steps phải sửa lại**: không phải "bấm icon Sửa từ list", mà "truy cập trực tiếp
>    bằng URL `?id={ItemNo}` (không `mode=view`)" — auto-test dùng `page.goto()` trực tiếp thay vì
>    click icon.

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

### TC-L06 — Truy cập chế độ Sửa qua URL trực tiếp (Positive) [SỬA — không còn icon Sửa]
- **Steps:** Gõ thẳng URL `/promotion/coupons/issue?id={ItemNo}` (**không** `&mode=view`) — **KHÔNG
  có icon/nút nào trên list dẫn tới đây** (đã xác nhận qua grep `src/POS.Web`, xem cảnh báo G7 đầu
  mục 4). Đường vào duy nhất trong UI là icon "Xem chi tiết" → `mode=view` (TC-L07).
- **Expected Result:** Form nạp sẵn dữ liệu, `IsEditing=true`, `IsViewMode=false` (xem TC-I06),
  tiêu đề "Sửa coupon {ItemNo}", nút "Lưu" chính hiển thị, field Auto khóa nếu `QtyCoupon>0`
  (`CodeFieldsLocked`, xem TC-I05).

### TC-L07 — Điều hướng Xem chi tiết (Positive) [BỔ SUNG]
- **Steps:** Bấm icon "Xem chi tiết" (👁, `Icons.Material.Filled.Visibility`) ở 1 dòng — đây là
  icon **DUY NHẤT** trên list dẫn sang trang Issue của 1 coupon đã tồn tại (icon còn lại là "Xóa",
  xem G7 điểm 2 — không liên quan điều hướng).
- **Expected Result:** Chuyển sang `/promotion/coupons/issue?id={ItemNo}&mode=view` — xem hành vi
  đầy đủ ở mục 3.1 "Chế độ Xem" + TC-I07/TC-I08.

---

## 5. Test cases 8.2 — CouponIssuePage (`/promotion/coupons/issue`)

### 5.1 Positive

#### TC-I01 — Tạo coupon Auto hợp lệ
- **Scenario:** Phát hành coupon tự sinh mã.
- **Pre-condition:** Có SalesType, StoreGroup (có store), role Ops+.
- **Steps:** 1) Mở trang (không `?id`). 2) Nhập Tên phát hành. 3) Cách phát hành = "Tự động tạo mã". 4) Chọn Hình thức bán + Nhóm cửa hàng. 5) Từ ngày / Đến ngày. 6) Prefix = `TEST`, Kích thước mã = `10`, Số chữ cái = `2`, Vị trí đứng = `3`, Số lượng = `5`. 7) Bỏ tick "Áp dụng theo danh sách sản phẩm". 8) Bấm **Lưu**.
- **Expected Result:** Snackbar "Cập nhật thành công coupon C7...". **[SỬA — bản cũ ghi sai]**
  Trang **điều hướng thẳng về `/promotion/coupons`** (`Nav.NavigateTo`, dòng 590) ngay sau khi cả
  `SaveIssueAsync` **và** `SaveAdvancedAsync` đều thành công — **KHÔNG** có việc "form reload tại
  chỗ sang chế độ Sửa". Muốn quan sát field Auto bị khóa (`CodeFieldsLocked`) + tab "Mã coupon đã
  phát hành" (5 mã, mỗi mã bắt đầu `TEST`, độ dài ≤ 20), phải quay lại từ list và bấm icon Sửa (xem
  TC-I05/TC-I06) — đây là 2 bước riêng biệt, không phải hệ quả tức thời của 1 lần Lưu. DB: 1 dòng
  IssueRule + 1 Header + 5 CodeIssue + store rows theo group. Audit `CREATE / SetupCoupon` +
  `CREATE / SetupCouponAdvanced` (2 entity riêng — xem mục 9.3).
- **Field bắt buộc điền trên form chính trước khi Lưu (dễ bỏ sót):** "Giá trị giảm giá (%/VNĐ)" —
  field này **luôn** được gửi kèm qua `SaveAdvancedAsync` ngay sau `SaveIssueAsync` (xem TC-I04 đã
  đổi ý nghĩa bên dưới). **[XÁC NHẬN LẠI qua verify DOM thật, 2026-07-16 — sửa nhận định sai trước
  đó]**: `CouponAdvancedSaveRequest.DiscountType` có property initializer `= 1`
  (`SetupCouponDtos.cs:183`) → coupon mới **mặc định đã chọn sẵn "Discount Percent (%)"**, KHÔNG
  cần tự chọn dropdown "Kiểu giảm giá". Nhưng `DiscountValue` (double, không có initializer) mặc
  định = `0` → vì `DiscountType==1` sẵn, `ValidateHeaderFields` (dòng 446) **LUÔN LUÔN** chặn Lưu
  với coupon mới nếu không tự điền "Giá trị giảm giá" > 0 — đây là field **bắt buộc thực sự** dễ bỏ
  sót nhất khi test/thao tác thật.
- **Edge Cases:** Xem E5 (Số lượng quá lớn), E6 (mã trùng), **E13** (mục 6 — retry sau khi
  `SaveAdvancedAsync` lỗi có thể tạo coupon thứ hai trùng lặp).

#### TC-I02 — Import Excel hợp lệ
- **Steps:** 1) Cách phát hành = "Import excel". 2) Bấm **File mẫu** → tải file `.xlsx` (cột `CodeCoupon`). 3) Điền vài mã hợp lệ vào file. 4) Bấm **Chọn file Excel** → chọn file. 5) Điền Tên + ngày. 6) **Lưu**.
- **Expected Result:** Parse cột A (bỏ dòng 1 header) → lưu các mã. Snackbar thành công. Tab mã hiển thị đúng danh sách.
- **Edge Cases:** File `.xls` cũ, file có dòng trống xen kẽ (bỏ qua dòng rỗng hoàn toàn), file >10MB → xem E11.

#### TC-I03 — Thêm/Xóa sản phẩm
- **Steps:** 1) Tick "Áp dụng theo danh sách sản phẩm". 2) Tab "Danh sách sản phẩm" → **Thêm sản phẩm** → tìm → chọn nhiều → **Chọn**. 3) Xóa 1 dòng bằng icon ✖.
- **Expected Result:** Item thêm vào bảng (không trùng `ItemNo` — dedupe theo `ItemNo`). Xóa dòng → biến mất khỏi bảng ngay. Khi Lưu → ghi `CpnVchBOMLine` (replace toàn bộ theo ItemNo).
- **Edge Cases:** Chọn cùng item 2 lần → chỉ 1 dòng. Xem E3 (bỏ tick checkbox làm mất item).

#### TC-I04 — Cài đặt nâng cao — ⚠️ KHÔNG THỂ THỰC HIỆN QUA UI HIỆN TẠI [SỬA]
- **Trạng thái thật của code:** `_showAdvancedButton = false` (hardcoded, `CouponIssuePage.razor:347`)
  → nút "Cài đặt nâng cao" **không bao giờ render**. `OpenAdvancedAsync`/`CouponAdvancedDialog`
  vẫn còn tồn tại trong code nhưng là **dead UI path** — không có cách nào người dùng thường kích
  hoạt được qua giao diện.
- **Vì sao vẫn giữ lại mục này:** để QA/dev biết code chết vẫn tồn tại (rủi ro maintenance — 2
  luồng lưu discount song song, xem E1), và để phân biệt với hành vi THẬT hiện tại: field Kiểu
  giảm giá/Giá trị giảm giá/Giảm tối đa VNĐ đã **inline ngay trên form chính** (Card "Thông tin
  chung", dòng 183-198), luôn được gửi kèm **mọi lần bấm Lưu chính** qua `SaveAdvancedAsync` gọi
  tự động ngay sau `SaveIssueAsync` thành công (dòng 578) — không còn là 1 hành động tùy chọn tách
  biệt như tên gọi "Cài đặt nâng cao" gợi ý.
- **Field bị ẩn/cố định hoàn toàn khỏi UI** (`SetDefaultsForNewCoupon`, dòng 394-403): ĐVT luôn
  `"CAI"`, Giới hạn số lượng `199999999`, Số lần sử dụng `1`, "Sử dụng nhiều lần" luôn `false`.
- **Hệ quả với các TC negative liên quan** (xem đánh dấu tương ứng ở mục 5.2): TC-N15 (ĐVT trống),
  TC-N18 (Số lần sử dụng=0), TC-N19 (ngày quá khứ) **không còn tái hiện được qua UI** — chỉ còn ý
  nghĩa nếu gọi thẳng `CouponService.SaveAdvancedAsync`/`OpenAdvancedAsync` bằng test code (ngoài
  phạm vi Playwright UI test).
- **Edge Cases:** Xem E1 (tạo coupon không mã — vẫn có giá trị lý thuyết vì code chưa xóa hẳn),
  E8 (chặn ngày quá khứ — nay **không quan sát được qua UI**, xem ghi chú cập nhật ở mục 6).

#### TC-I05 — Sửa coupon đã có mã
- **Pre-condition:** Coupon `QtyCoupon > 0`.
- **Steps:** Truy cập **trực tiếp bằng URL** `?id={ItemNo}` (**không** `&mode=view` — xem G7 mục 4,
  không có icon nào trong UI dẫn tới đây, phải gõ/điều hướng thẳng URL). Sửa Tên / thêm sản phẩm.
  **Lưu**.
- **Expected Result:** Cách phát hành + field Auto/Import **bị khóa** (không sinh lại mã — `needCodes=false`). Chỉ cập nhật Header + Line + Store. Audit `UPDATE / SetupCoupon`.
- **Edge Cases:** Xem E10 (ô Số lượng hiện 0).

#### TC-I06 — Nạp form khi Sửa (`?id=`, truy cập trực tiếp bằng URL — xem G7)
- **Expected Result:** Tên, Cách phát hành, Hình thức bán, Nhóm CH, Từ/Đến ngày, checkbox, danh sách sản phẩm, số mã đã phát sinh — nạp đúng từ `GetDetailAsync`.

### 5.2 Negative (validate trong `CouponService`)

| TC | Thao tác | Expected (Snackbar/Banner) |
|---|---|---|
| TC-N01 | Bỏ trống Tên phát hành → Lưu | "Vui lòng nhập tên phát hành coupon" (Warning) |
| TC-N02 | Không chọn Từ ngày / Đến ngày → Lưu | "Vui lòng chọn ngày bắt đầu" / "...kết thúc" (Warning) |
| TC-N03 | Từ ngày **≥** Đến ngày → Lưu | **[SỬA]** `"Từ ngày phải nhỏ hơn Đến ngày"` (Warning — `CouponIssuePage.razor:445`, bản cũ ghi sai text "TỪ NGÀY không lớn hơn ĐẾN NGÀY"; điều kiện thật là `>=` — ngày **bằng nhau** cũng bị chặn, không chỉ trường hợp Từ ngày sau Đến ngày) |
| TC-N03b | **[MỚI]** Kiểu giảm giá = "Discount Percent (%)" (**mặc định sẵn** cho coupon mới, xem TC-I01), Giá trị giảm giá ≤ 0 (mặc định `0`, dễ tái hiện nhất — chỉ cần KHÔNG điền field này) hoặc > 100 → Lưu | `"Giá trị giảm giá theo % phải lớn hơn 0 và không vượt quá 100"` (Warning, `ValidateHeaderFields`, dòng 446-450) — chạy **ngay tại trang chính** khi bấm Lưu, chỉ kích hoạt khi `DiscountType == 1`; đây chính là 2 rule N16/N17 cũ nhưng **không phải luồng "Advanced"** (dialog đã ẩn — xem TC-I04) mà là validate của form chính |
| TC-N04 | Auto: Kích thước mã < 5 hoặc > 20 | "Kích thước mã từ 5->20 ký tự" |
| TC-N05 | Auto: LenCode + độ dài Prefix + Số chữ cái > 20 | "Tổng ký tự coupon đã vượt hơn 20" |
| TC-N06 | ~~Auto: Số lượng ≤ 0~~ | ⚠️ **[SỬA — xác nhận qua auto-test thật 2026-07-16]** **KHÔNG tái hiện được qua UI**: `MudNumericField Min="1"` của ô "Số lượng mã phát hành" (`CouponIssueMoreDialog.razor:36-39`) **tự động clamp** giá trị gõ vào về `1` trước khi submit (giống cơ chế `Max="100"` tự clamp của "Giá trị giảm giá" — xem TC-N03b) — gõ `0` vào ô này rồi bấm "PHÁT HÀNH" **KHÔNG bị chặn**, dialog đóng bình thường và **coupon được tạo thành công với Quantity thực nhận = 1** (không phải 0). Rule service-side "Vui lòng nhập số lượng phát hành" (`CouponService`) do đó **không có đường nào từ UI đưa giá trị ≤0 tới được nó** — chỉ còn ý nghĩa nếu gọi thẳng `CouponService.SaveIssueAsync`/`IssueMoreAsync` bằng test code. |
| TC-N07 | Auto/Import: mã đã tồn tại trong DB | "Mã coupon trùng trong DB (...)..." |
| TC-N08 | Import: không chọn file | "Vui lòng chọn file excel để import" |
| TC-N09 | Import: file rỗng (không có mã) | "Vui lòng kiểm tra file Excel, không có mã coupon" |
| TC-N10 | Import: có ô mã trống | "Vui lòng kiểm tra cột CodeCoupon, có giá trị trống" |
| TC-N11 | Import: mã chứa ký tự đặc biệt (vd `A@B`) | "Có N mã coupon ... có ký tự đặc biệt (...)" (regex `^[0-9\-_A-Za-z]*$`) |
| TC-N12 | Import: mã > 20 ký tự | "Có N mã coupon ... vượt quá 20 ký tự (...)" |
| TC-N13 | Import: mã trùng nhau trong file | "File excel có giá trị trùng (...)..." |
| TC-N14 | Tick "theo sản phẩm" nhưng danh sách rỗng → Lưu | "Vui lòng thêm sản phẩm vào voucher/coupon" |
| TC-N15 | ~~Advanced: bỏ trống ĐVT~~ | ⚠️ **KHÔNG THỂ TÁI HIỆN QUA UI** — ĐVT bị ẩn + hardcode `"CAI"` (`SetDefaultsForNewCoupon`), dialog Advanced không mở được (xem TC-I04). Chỉ còn giá trị nếu gọi thẳng `CouponService.SaveAdvancedAsync` bằng test code |
| TC-N16 | Giá trị giảm ≤ 0 (Kiểu giảm giá bất kỳ) | **[GỘP vào TC-N03b]** — nay validate ở form chính, không phải Advanced |
| TC-N17 | Kiểu = % và Giá trị > 100 | **[GỘP vào TC-N03b]** — nay validate ở form chính, không phải Advanced |
| TC-N18 | ~~Advanced: tick "Sử dụng nhiều lần" + Số lần = 0~~ | ⚠️ **KHÔNG THỂ TÁI HIỆN QUA UI** — checkbox "Sử dụng nhiều lần" bị ẩn + hardcode `false`, dialog Advanced không mở được |
| TC-N19 | ~~Advanced: Từ ngày < hôm nay~~ | ⚠️ **KHÔNG THỂ TÁI HIỆN QUA UI** — dialog Advanced không mở được; xem E8 (mục 6) đã cập nhật ghi chú |

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

### E8 — Lệch validate ngày giữa 2 luồng Lưu ⚠️ [CẬP NHẬT — không còn quan sát được qua UI]
- **Mô tả:** "Lưu" (issue) **cho phép** Từ ngày trong quá khứ; "Cài đặt nâng cao" **chặn** `start < today`.
- **Tái hiện (LÝ THUYẾT — không còn thực hiện được qua UI hiện tại):** Đặt Từ ngày = hôm qua →
  "Lưu" chính OK; nhưng mở "Cài đặt nâng cao" → Lưu → lỗi "TỪ NGÀY không được nhỏ hơn ngày hiện
  tại". **Do `_showAdvancedButton = false` (xem TC-I04), nút "Cài đặt nâng cao" không còn render**
  → nhánh validate `OpenAdvancedAsync`/`CouponAdvancedDialog` này không ai kích hoạt được qua UI
  nữa. Chỉ còn tái hiện được nếu gọi thẳng `CouponService.SaveAdvancedAsync`/`OpenAdvancedAsync`
  bằng test code (unit test/reflection), không phải qua trình duyệt.
- **Rủi ro:** Trải nghiệm mâu thuẫn (vẫn tồn tại trong code, chỉ không lộ ra qua UI); nếu tương lai
  bật lại nút "Cài đặt nâng cao" (`_showAdvancedButton = true`) thì bug này lập tức tái xuất.
- **Đề xuất:** Thống nhất 1 quy tắc ngày cho cả 2 luồng — hoặc xóa hẳn code Advanced dialog chết
  nếu xác nhận không còn dùng, tránh vừa giữ code chết vừa giữ bug tiềm ẩn.

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

### E13 — [MỚI, phát hiện khi đối chiếu code 2026-07-16] Retry sau khi `SaveAdvancedAsync` lỗi có thể tạo COUPON THỨ HAI trùng lặp, mã thật đã phát nhưng "vô hình" với người dùng
- **Mô tả:** Trong `SaveAsync` (`CouponIssuePage.razor:520-599`), sau khi `SaveIssueAsync` thành
  công (đã sinh mã Auto + ghi audit `CREATE/SetupCoupon`, dòng 552-561), code **KHÔNG BAO GIỜ gán
  lại `_model.ItemNo = result.ItemNo`** — chỉ `_advanced.ItemNo` được gán (dòng 565). Nếu bước tiếp
  theo `SaveAdvancedAsync` thất bại (dòng 578-584) → set `_errorMsg`/Snackbar lỗi rồi `return`,
  **KHÔNG điều hướng đi**. Hệ quả: `_model.ItemNo` vẫn rỗng, `_quantityCodeInDb` vẫn = 0 (field này
  chỉ được nạp trong `LoadDetailAsync` lúc load trang ban đầu, không có chỗ nào refresh lại sau
  Save) — trang vẫn hiển thị y hệt trạng thái "tạo mới".
- **Tái hiện:** Tạo coupon Auto mới → cố tình làm `SaveAdvancedAsync` thất bại (vd giá trị
  `DiscountValue` bị Service từ chối do rule N16/N17 áp dụng phía server, hoặc SP `usp_SetupCoupon_
  SaveAdvanced` lỗi tạm thời/mất kết nối DB giữa 2 lệnh gọi) → thấy Snackbar lỗi "..." → bấm **Lưu
  lại** → vì `NeedsCodeDialog` (`ItemNo rỗng || quantityCodeInDb==0`, dòng 438) vẫn `true` → dialog
  "Phát hành mã coupon" mở lại → xác nhận → `SaveIssueAsync` được gọi **LẦN NỮA với `ItemNo` rỗng**
  → server sinh **`ItemNo` MỚI** + **lô mã Auto MỚI** hoàn toàn tách biệt với lô đầu tiên.
- **Rủi ro:** Coupon đầu tiên đã có mã thật (dùng được ở POS ngay lập tức) nhưng **KHÔNG có audit
  `SetupCouponAdvanced`**, discount/giảm giá của nó chưa từng lưu đúng, và **không hiển thị ở đâu
  để người dùng biết nó tồn tại** (form vẫn hiện như đang "tạo mới", không có cách nào tra ngược lại
  `ItemNo` vừa sinh trừ khi chủ động vào list tìm theo Tên phát hành). Mỗi lần bấm Lưu lại sau lỗi
  advanced → phát sinh thêm 1 coupon "rác" có mã thật, không bị chặn bởi bất kỳ guard nào.
- **Đề xuất:** Gán `_model.ItemNo = result.ItemNo;` (và cập nhật `_quantityCodeInDb` tương ứng) ngay
  sau khi `SaveIssueAsync` trả `Ok=true`, **trước khi** gọi `SaveAdvancedAsync` — để lần Lưu lại
  (nếu `SaveAdvancedAsync` lỗi) đi vào nhánh "update coupon đã tồn tại" thay vì "tạo mới".

---

## 7. Checklist regression nhanh

```
□ List load + phân trang + STT liên tục qua trang
□ Filter (mã/tên/cách phát hành/hiệu lực) + nút Xóa reset về "Tất cả"
□ Nút Xóa chỉ hiện khi QtyCoupon==0; xóa → audit DELETE
□ Auto: tạo mới hợp lệ → 5 mã đúng prefix/độ dài; field khóa sau lưu
□ Import: tải file mẫu; import file hợp lệ; các case lỗi N09–N13
□ Advanced (giá trị giảm giá): validate TC-N03b ở form chính OK (dialog "Cài đặt nâng cao"
  KHÔNG còn tồn tại qua UI — xem TC-I04, N15/N18/N19 chỉ test được qua Service trực tiếp)
□ Sửa coupon có mã: field code khóa, chỉ update header/line/store
□ Xem chi tiết (?mode=view): mọi field khóa trừ Blocked; nút đổi thành PHÁT HÀNH THÊM (TC-I07/I08)
□ Toàn bộ validate Negative TC-N01, N02, N03, N03b, N04, N05, N07-N14 hiện đúng thông báo
  (N06/N15/N18/N19 KHÔNG thể test qua UI — MudNumericField Min/Max tự clamp hoặc dialog Advanced
  ẩn, xem TC-N06/TC-I04)
□ Điểm yếu E1–E13: xác nhận hành vi thực tế + log lại để dev vá (đặc biệt E13 — nguy cơ tạo
  coupon trùng lặp có mã thật khi retry sau lỗi advanced)
□ SP chưa deploy → banner đỏ, app không crash
```

## 8. Ghi chú môi trường

- **Bắt buộc** deploy: `docs/sql/SetupCoupon_Read.sql`, `SetupCoupon_Save.sql`, `SetupCoupon_Delete.sql` (xem `docs/ROLLOUT.md` §D3).
- Bảng audit: chạy `src/POS.Web/Auth/migration_dashboard_audit_log.sql` trên `RPOSMasterData` (nếu chưa → audit fail-safe, không crash).
- Role test: **BackOffice**, **ITOps** hoặc **SystemAdmin** (policy `BackOfficeAndAbove` — xem G0
  mục 1). StoreOperator sẽ bị chặn (403/AccessDenied).
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

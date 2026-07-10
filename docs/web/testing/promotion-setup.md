# Tài liệu nghiệp vụ & Test — Cài đặt CTKM (POS.Web)

> Phạm vi: chức năng CRUD đã migrate sang POS.Web (Blazor Server .NET 10).
> - **Cài đặt CTKM** (chương trình khuyến mãi, soạn thảo + duyệt): `/promotion/setup`
> - **Danh mục khuyến mãi** (downstream, xem/tra cứu CTKM đã publish, xuất Excel, Deactive): `/promotion/offers`
>   — bổ sung 2026-07-10 vào tài liệu này vì cùng module, cùng chuỗi dữ liệu (`SetupPromotion*` →
>   `Offer*`), và vừa có bug fix liên quan trực tiếp tới cách hiển thị đúng dữ liệu do §A tạo ra
>   (xem §B "Danh mục khuyến mãi" bên dưới).
>
> Đối tượng đọc: BA, Dev, **Tester**. DB: `RPOSMasterData` (CentralMD).
> Quyền truy cập: vai trò **ITOps** hoặc **System Admin** (policy `OpsAndAbove`). StoreOperator KHÔNG vào được.

---

## 0. Điều kiện tiên quyết khi test (BẮT BUỘC)

Trước khi test, DBA phải chạy **toàn bộ** script SQL dưới đây trên `RPOSMasterData` theo đúng thứ
tự trong từng dòng (xem chi tiết từng mục ở `docs/ROLLOUT.md`). Thiếu bất kỳ script nào → trang
tương ứng báo lỗi đỏ (thường là "Lỗi hệ thống..." chung chung — xem §10.3) khi tải/lưu/duyệt.

| # | Trang | Chức năng | Script cần chạy (đúng thứ tự nếu >1 dòng) | ROLLOUT |
|---|---|---|---|---|
| 1 | `/promotion/setup` | Lưu nháp + Duyệt CTKM | `docs/sql/SetupPromotion_Save.sql`, `docs/sql/SetupPromotion_ApproveAndStatus.sql` (SP `Setup_Promotion_Insert` đã có sẵn, không tạo lại) | [§D1](../../ROLLOUT.md#d1--stored-procedures-cài-đặt-ctkm-111) |
| 2 | `/promotion/setup` (modal "Nhóm SP"/"Nhóm cửa hàng") | Cài đặt nhóm sản phẩm / nhóm cửa hàng dùng ở dòng Buy/Get | `docs/sql/SetupGroupItem_CreateTable.sql` → `docs/sql/SetupGroupItem_Save.sql` → `docs/sql/SetupGroupSite_Save.sql` | [§D1b](../../ROLLOUT.md#d1b--sp--bảng-nhóm-sản-phẩmnhóm-cửa-hàng) |
| 3 | `/promotion/setup` (tab "Cài đặt nâng cao") | Chọn nhiều "Ngày áp dụng trong tháng" (`NUMOFDAYSLIST`) | `docs/sql/SetupPromotion_AddNumOfDaysList.sql` (ALTER TABLE) → chạy lại `docs/sql/SetupPromotion_Save.sql` (bản đã thêm `@NumOfDaysList`) | [§D11](../../ROLLOUT.md#d11--cột-numofdayslist--chọn-nhiều-ngày-áp-dụng-ctkm-trong-tháng) |
| 4 | `/promotion/offers` | Filter "Từ ngày" + fix trùng dòng theo site | `docs/sql/GetPromotionOfferHeaderList_AddDateRangeFilter.sql` + `docs/sql/GetPromotionOfferHeaderList_FixDuplicateRows.sql` (2 script độc lập thứ tự, cả 2 đều `ALTER PROC` toàn SP — thiếu 1 trong 2 sẽ mất tính năng của script kia) | [§D12](../../ROLLOUT.md#d12--sp-getpromotionofferheaderlist--filter-theo-ngày--fix-trùng-dòng-danh-mục-khuyến-mãi) |
| 5 | `/promotion/offers` (nút Deactive) | Tắt hiệu lực 1 offer LIVE | `docs/sql/OfferHeader_Deactivate.sql` | [§D8](../../ROLLOUT.md#d8--stored-procedure-deactive-khuyến-mãi) |

> **Không cần chạy SQL nào** cho bug "cột Mã CTKM hiển thị trùng lặp" (đã sửa 2026-07-10) — đây là
> lỗi binding ở code Razor (`OffersPage.razor`), không phải lỗi SP. Xem §B4 bên dưới.

**Bảng dữ liệu** (đã tồn tại): `SetupPromotionHEADER/BUY/GET/SITE`, `OfferHeader/OfferBuy/OfferGet/OfferBenefit/OfferSite`,
cùng các bảng tham chiếu `OfferType`, `SalesOrderType`, `Item`, `SetupGroupSites`, `SetupGroupItem`, `OptionData`.

---

# PHẦN A — Cài đặt CTKM (Chương trình khuyến mãi)

## 1. Mục đích nghiệp vụ
Tạo/sửa **chương trình khuyến mãi** (CTKM / Bonus Buy): khai báo điều kiện **MUA** (Buy), phần **NHẬN/Chiết khấu** (Get),
phạm vi **cửa hàng** (Site), và các thiết lập nâng cao (voucher, hạng thẻ, ưu tiên...). CTKM soạn ở trạng thái **nháp**,
sau khi **Duyệt** sẽ được phát hành (publish) sang bảng "live" để máy POS áp dụng.

## 2. Kiến trúc 2 giai đoạn (rất quan trọng cho Tester)

```
 [Soạn thảo - nháp]                          [Duyệt - publish]
 SetupPromotionHEADER  ──(usp_SaveSetupCTKMAll)──►  lưu nháp
 SetupPromotionBUY                                       │
 SetupPromotionGET                                       ▼ (usp_SetupPromotion_Approve
 SetupPromotionSITE                                       → EXEC Setup_Promotion_Insert)
                                                          ▼
                            OfferHeader / OfferBuy / OfferGet / OfferBenefit / OfferSite  (bảng LIVE)
                                                          ▼
                                          Hiển thị ở "Danh mục khuyến mãi" /promotion/offers
```

- **Lưu** chỉ ghi vào 4 bảng `SetupPromotion*` (nháp) — chưa ảnh hưởng POS.
- **Duyệt** đánh dấu `IsApprove=1` và chạy SP `Setup_Promotion_Insert @BBY` đẩy dữ liệu sang bảng `Offer*`.
- CTKM **đã duyệt → chỉ xem, KHÔNG sửa được** (form khóa readonly).

## 3. Bảng dữ liệu

| Bảng | Vai trò | Khóa | Cột chính |
|---|---|---|---|
| `SetupPromotionHEADER` | Header CTKM nháp | `BBYNR` | SalesType, BBYTEXT(tên), BBYTYPE(loại), STATUS, VALIDFROM/VALIDTO (yyyyMMdd), IsVoucher, IsApprove, BUYLINKCAT/GETLINKCAT, + advance: LIMIT, VINID, MemberCode, ZPRIOR, NUMOFDAYS, ZVCDATE_ST/EN, ZVCDATE_VA, LIMITNR |
| `SetupPromotionBUY` | Dòng điều kiện mua | `BBYNR` | BUYTYPE(MAT/MGP), MAT_NR, MATGROUP, MAT_QUAN, MEINH, ScaleType |
| `SetupPromotionGET` | Dòng nhận/chiết khấu | `BBYNR` | GETTYPE, MATERIALCODE, MATGROUP, QTY, DISTYPE(%/R/P), BBYVAL, BBYPER |
| `SetupPromotionSITE` | Cửa hàng áp dụng | `BBYNR` | SITEGROUPCODE, SITECODE |
| `Offer*` (Header/Buy/Get/Benefit/Site) | Bảng LIVE sau duyệt | — | Do SP `Setup_Promotion_Insert` ghi |

## 4. Stored Procedures

| SP | Tham số chính | Mục đích |
|---|---|---|
| `usp_SaveSetupCTKMAll` | `@BBYNR OUTPUT`, `@SalesType, @Description, @OfferType, @Status, @ValidFrom, @ValidTo, @IsVoucher, @BuyLinkCat, @GetLinkCat` + advance (`@LimitQty, @MemberOnly, @MemberCode, @Priority, @NumOfDays, @VoucherFrom, @VoucherTo, @VoucherValidDay, @VoucherLimitNumber`) + 3 TVP (`@Buy, @Get, @Site`) | Upsert header + **xóa-chèn lại** Buy/Get/Site (transaction). Auto-gen `BBYNR` khi mới. Chặn lưu nếu đã duyệt. |
| `usp_SetupPromotion_Approve` | `@BBYNR` | Đánh dấu `IsApprove=1` + EXEC `Setup_Promotion_Insert @BBY` (publish). |
| `usp_SetupPromotion_UpdateStatus` | `@BBYNR, @Status` | Đổi trạng thái nhanh ở danh sách. |
| `Setup_Promotion_Insert` (có sẵn) | `@BBY` | Publish nháp → Offer*. |

**Quy tắc dữ liệu:**
- `BBYNR` auto-gen = `MAX(BBYNR)+1`, bắt đầu `6000000001`.
- `STATUS`: `0`=Đang áp dụng, `1`=Lên kế hoạch, `2`=Ngưng áp dụng.
- `BUYLINKCAT`/`GETLINKCAT`: `A`=AND (thỏa tất cả), `O`=OR (thỏa một trong).
- Get `DiscountType`: `0`=% (lưu BBYPER), `1`=Số tiền (R, lưu BBYVAL), `2`=Giá cố định (P, lưu BBYVAL).
- LineType dòng Buy/Get: `0`=Sản phẩm (MAT, dùng MAT_NR/MATERIALCODE), `1`=Nhóm SP (MGP, dùng MATGROUP).
- Voucher dates ghi vào `ZVCDATE_ST/EN` (chỉ khi tick "Voucher/Coupon"); sentinel rỗng = `19000101`.

## 5. Luồng thao tác trên màn hình (UI)

**Màn danh sách** (`/promotion/setup`):
- Bộ lọc: Mã CTKM, Tên CTKM, Trạng thái duyệt (Tất cả / Chưa duyệt / Đã duyệt).
- Bảng phân trang server-side; mỗi dòng có nút **Sửa/Xem**, **Duyệt** (nếu chưa duyệt).
- Nút **Thêm CTKM**.

**Form tạo/sửa** (4 tab):
1. **Thông tin chung**: Tên CTKM*, Loại CTKM*, Hình thức bán*, Trạng thái, Từ ngày*, Đến ngày*, checkbox Voucher/Coupon.
2. **Sản phẩm mua (Buy)**: chọn điều kiện AND/OR; thêm dòng (Loại=SP/Nhóm, chọn sản phẩm, ĐVT, số lượng).
3. **Sản phẩm khuyến mãi (Get)**: như Buy + Loại chiết khấu + Giá trị CK.
4. **Cửa hàng**: thêm nhóm cửa hàng (SITEGROUPCODE) — hệ thống tự bung thành danh sách store.
5. **Cài đặt nâng cao**: Số lần áp dụng, Độ ưu tiên (1–10), Ngày áp dụng trong tháng; Thành viên (bật + Hạng thẻ); Voucher (từ/đến ngày, số ngày hiệu lực, số lần phát hành — chỉ khi đã tick Voucher).

Nút **Lưu** (lưu nháp) và **Duyệt CTKM** (chỉ hiện khi đã có mã).

## 6. Validate (thông báo lỗi mong đợi)
- Thiếu Tên CTKM → "Vui lòng nhập tên chương trình khuyến mãi".
- Thiếu Hình thức bán → "Vui lòng chọn hình thức bán hàng".
- Thiếu Loại CTKM (và chưa có mã) → "Vui lòng chọn loại CTKM".
- Sai định dạng/thiếu ngày → "Ngày bắt đầu / kết thúc không đúng định dạng (dd/MM/yyyy)".
- Đến ngày < Từ ngày → "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu".
- Lưu CTKM đã duyệt → "CTKM {mã} đã được DUYỆT, không được phép lưu tạm".

## 7. TEST CASES — Cài đặt CTKM

> Tiền điều kiện chung: đã chạy script SQL §0; đăng nhập vai trò ITOps/Admin; có ≥1 Loại CTKM (`OfferType.Enabled=1`),
> ≥1 Hình thức bán (`SalesOrderType.IsActive=1`), ≥1 sản phẩm trong `Item`, ≥1 nhóm cửa hàng trong `SetupGroupSites`.

| ID | Mục tiêu | Bước thực hiện | Kết quả mong đợi |
|---|---|---|---|
| CTKM-01 | Mở trang & phân quyền | Đăng nhập ITOps → mở `/promotion/setup` | Trang hiển thị danh sách + nút "Thêm CTKM". Đăng nhập StoreOperator → bị chặn (không thấy menu/403). |
| CTKM-02 | Tạo CTKM tối thiểu hợp lệ | Thêm CTKM → nhập Tên, chọn Loại, Hình thức bán, Từ/Đến ngày → tab Buy thêm 1 SP, tab Get thêm 1 SP → **Lưu** | Thông báo "Lưu CTKM {mã} thành công". Sinh `BBYNR` mới (vd 6000000001). Có dòng trong `SetupPromotionHEADER/BUY/GET`. |
| CTKM-03 | Validate thiếu Tên | Thêm CTKM, bỏ trống Tên → Lưu | Báo "Vui lòng nhập tên chương trình khuyến mãi"; không ghi DB. |
| CTKM-04 | Validate ngày | Nhập Đến ngày < Từ ngày → Lưu | Báo "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu". |
| CTKM-05 | Điều kiện AND/OR | Tạo CTKM, tab Buy chọn "Một trong (OR)", thêm 2 SP → Lưu → mở lại | `SetupPromotionHEADER.BUYLINKCAT='O'`; form hiển thị lại "Một trong (OR)". |
| CTKM-06 | Get có chiết khấu % | Tab Get thêm 1 SP, Loại chiết khấu = Phần trăm, Giá trị = 10 → Lưu | `SetupPromotionGET.DISTYPE='%'`, `BBYPER='10'`, `BBYVAL='0'`. |
| CTKM-07 | Get chiết khấu số tiền | Loại chiết khấu = Số tiền, Giá trị = 5000 | `DISTYPE='R'`, `BBYVAL='5000'`, `BBYPER='0'`. |
| CTKM-08 | Dòng theo Nhóm SP | Tab Buy thêm dòng Loại=Nhóm SP, nhập mã nhóm | `SetupPromotionBUY.BUYTYPE='MGP'`, `MATGROUP=<mã nhóm>`, `MAT_NR=''`. |
| CTKM-09 | Cửa hàng áp dụng | Tab Cửa hàng thêm 1 nhóm CH "ALL" → Lưu | `SetupPromotionSITE` có dòng `SITECODE='ALL'`. Thêm nhóm cụ thể → bung nhiều dòng theo store của nhóm. |
| CTKM-10 | Sửa khi chưa duyệt | Mở CTKM chưa duyệt → đổi Tên, xóa 1 dòng Get → Lưu | Lưu thành công; số dòng Get giảm đúng (replace-on-save: xóa cũ chèn mới). |
| CTKM-11 | Cài đặt nâng cao | Tạo CTKM, tab nâng cao: Số lần áp dụng=2, Ưu tiên=3, bật Thành viên + chọn Hạng thẻ → Lưu → mở lại | `LIMIT='2'`, `ZPRIOR='3'`, `VINID='X'`, `MemberCode=<hạng thẻ>`; form hiển thị lại đúng. |
| CTKM-12 | Voucher dates | Tick "Voucher/Coupon" → tab nâng cao nhập Voucher từ/đến ngày, số ngày hiệu lực, số lần phát hành → Lưu → mở lại | `ZVCDATE_ST/EN` = ngày voucher (yyyyMMdd); `ZVCDATE_VA`, `LIMITNR` đúng; mở lại hiển thị ngày dd/MM/yyyy. |
| CTKM-13 | Duyệt CTKM (publish) | Mở CTKM nháp đã có Buy/Get/Site → bấm **Duyệt** → xác nhận | Báo "Duyệt CTKM {mã} thành công". `IsApprove=1`. Dữ liệu xuất hiện ở bảng `Offer*` và ở trang `/promotion/offers` — cột **"Mã CTKM"** ở `/promotion/offers` phải hiện **đúng `BBYNR`** vừa duyệt (vd `6000000001`), KHÔNG phải giá trị `PromotionNo` (field khác, thường rỗng/khác mã) — xem §B4 nếu sai. |
| CTKM-14 | Khóa sửa sau duyệt | Mở lại CTKM đã duyệt | Form **readonly** (chip "Đã duyệt — chỉ xem"); không có nút Lưu. |
| CTKM-15 | Lọc trạng thái duyệt | Danh sách → lọc "Đã duyệt" / "Chưa duyệt" | Bảng chỉ hiển thị đúng nhóm tương ứng. |
| CTKM-16 | Phân trang | Tạo >20 CTKM → đổi số dòng/trang, chuyển trang | Phân trang server-side đúng tổng số, không trùng/sót dòng. |
| CTKM-17 | Audit log | Sau Lưu/Duyệt | Có bản ghi trong `DashboardAuditLog` (action CREATE/UPDATE/APPROVE, entity `SetupPromotion`). |

---

## 8. Ghi chú kỹ thuật & ngoài phạm vi (cho QA biết)

- ~~**Cài đặt CTKM** — phần đã **hoãn** (không cần test): Giờ áp dụng (TIMEFROM/TO), Ngày-trong-tuần (Mon–Sun),
  AllowUseAfterDay/Time (DB chưa có cột).~~ **Đã hết hoãn** (bản SP sửa lần 2, 2026-07-05): các cột này đã
  tồn tại sẵn trong `SetupPromotionHEADER` và đã được SP `usp_SaveSetupCTKMAll` đọc/ghi đầy đủ — tab
  "Thông tin chung" (bảng giờ/ngày trong tuần) và "Cài đặt nâng cao" (Được dùng sau N ngày/giờ) **có thể test**.
- Dùng cơ chế **replace-on-save** (lưu = xóa hết dòng con theo khóa rồi chèn lại) → khi sửa, các dòng bị bỏ trên
  form sẽ biến mất khỏi DB. Tester lưu ý khi đối chiếu số dòng. Chi tiết cơ chế: xem mục 10 "Data Flow".
- Lỗi hệ thống được nuốt và hiển thị snackbar đỏ + ghi log file (`D:\ROOT\Logs\POS.Web\Exception\log-yyyyMMdd.txt`).
  Khi gặp lỗi không rõ, lấy dòng log mới nhất để Dev phân tích.
- **Nút "Duyệt" ở màn danh sách KHÔNG Lưu tạm trước khi publish** — chỉ publish đúng dữ liệu nháp đã có sẵn
  trong DB (tránh ghi đè Buy/Get/Site thành rỗng do trang danh sách không giữ state form). Nút **"Duyệt CTKM"
  trong editor** thì **luôn Lưu tạm trước** rồi mới publish — nếu Lưu tạm thất bại (vd validate lỗi), Duyệt
  sẽ **không chạy** (dừng ở bước Lưu). Tester test 2 nút Duyệt này ở 2 màn hình khác nhau, đừng coi là 1 hành vi.

## 9. Tham chiếu nguồn (Dev)

| Thành phần | Cài đặt CTKM |
|---|---|
| Page (Blazor) | `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` |
| Service (Application) | `IPromotionService` / `PromotionService` |
| Repository (Infrastructure) | `IPromotionRepository` / `PromotionRepository` |
| DTO (Common) | `Dtos/Promotion/PromotionSetupDto.cs` |
| SQL scripts | `docs/sql/SetupPromotion_Save.sql`, `SetupPromotion_ApproveAndStatus.sql` |

---

## 10. Technical Deep Dive (dành cho Dev)

> Mục này giải thích **tại sao** code chạy như vậy — đọc trước khi sửa `PromotionSetupPage.razor` hoặc
> `PromotionRepository.SaveSetupAsync`/`usp_SaveSetupCTKMAll`.

### 10.1 Data Flow — cơ chế "replace-on-save"

```
PromotionSetupPage.SaveAsync()
  1. Gán lại _header.StartingDate/EndingDate/VoucherFromDate/VoucherToDate từ MudDatePicker
     (DateTime? _fromDate/_toDate/_voucherFromDate/_voucherToDate) → format "dd/MM/yyyy".
     Voucher dates CHỈ gán khi _header.IsVoucher=true, ngược lại ép rỗng (tránh publish
     voucher date cũ khi user tắt Voucher/Coupon sau khi đã có dữ liệu).
  2. Build PromotionSetupSaveRequest { Header, BuyRows, GetRows, SiteGroupCodes }
     — SiteGroupCodes chỉ là list mã nhóm (KHÔNG phải list SiteCode cụ thể).
  3. PromotionService.SaveSetupAsync (thin wrapper) → PromotionRepository.SaveSetupAsync
     3a. Validate ở Repository (KHÔNG ở Service/Page) — xem 10.2.
     3b. BuildSiteTableAsync(SiteGroupCodes) — Repository tự JOIN nhóm cửa hàng ra danh sách
         SiteCode cụ thể (bảng SetupGroupSites hoặc tương đương) TRƯỚC khi gọi SP. Đây là lý do
         Buy/Get dùng TVP theo đúng field UI, còn Site TVP nhận (SiteGroupCode, SiteCode) đã bung.
     3c. Gọi SP usp_SaveSetupCTKMAll với @BBYNR là InputOutput param (rỗng = tạo mới).
  4. SP (trong 1 transaction):
     - Auto-gen BBYNR = MAX(BBYNR)+1 (bắt đầu 6000000001) dùng UPDLOCK,HOLDLOCK chống race
       khi 2 request tạo mới đồng thời.
     - Chặn cứng nếu IsApprove=1 (RAISERROR + ROLLBACK) — xem 10.3.
     - Upsert HEADER (UPDATE nếu đã có BBYNR, INSERT nếu chưa — có nhánh kiểm tra ID có phải
       IDENTITY hay không qua sys.columns, vì cột ID legacy không đồng nhất giữa môi trường).
     - BUY/GET/SITE: DELETE WHERE BBYNR=@BBYNR rồi INSERT lại toàn bộ từ TVP — KHÔNG có UPDATE
       theo dòng, KHÔNG merge diff. Dòng nào bị xóa trên UI (bấm nút Xóa dòng) sẽ không có trong
       TVP → biến mất khỏi DB ngay khi Lưu. Không có "soft delete"/lịch sử dòng cũ.
  5. Repository trả (Ok, Message, BBYNR) → Page gán lại _header.No = bbynr rồi ghi Audit Log.
```

- **Vì sao replace-on-save mà không update-theo-dòng**: form Buy/Get không có khóa dòng ổn định
  (dòng mới bấm "Thêm dòng" chưa có ID) — tính diff thêm/sửa/xóa từng dòng phức tạp hơn nhiều so
  với xóa-chèn-lại trong 1 transaction, và vì lưu nháp không ảnh hưởng POS ngay (chỉ publish mới
  ảnh hưởng), rủi ro mất dữ liệu tạm thời được coi là chấp nhận được.
- **Site tab lưu theo NHÓM, không theo store lẻ**: `_siteRows` trên UI chỉ chứa `SiteGroupCode`;
  việc "bung" thành từng `SiteCode` cụ thể xảy ra ở `BuildSiteTableAsync` (Repository), không phải
  ở Page/Service. Muốn debug "sao Site áp dụng sai cửa hàng" → xem đúng hàm này, không phải UI.

### 10.2 Validate — field bắt buộc & vị trí thực thi

- **Validate 2 tầng, khác nơi**:
  - Tầng UI (MudBlazor `Required`/`RequiredError` trên `MudSelect`/`MudTextField`/`MudDatePicker`)
    chỉ chặn submit form HTML — không chặn được gọi `SaveAsync()` trực tiếp và không validate logic
    liên trường (vd so sánh 2 ngày).
  - Tầng thật (bắt buộc, không thể bỏ qua) nằm ở `PromotionRepository.SaveSetupAsync` (KHÔNG phải
    `PromotionService` — service chỉ delegate thẳng, không có logic) — đây là nơi duy nhất kiểm:
    Tên CTKM rỗng, Hình thức bán rỗng, Loại CTKM rỗng (chỉ bắt buộc khi tạo mới — `h.No` rỗng),
    `Status` phải thuộc `{0,1,2}`, format ngày `dd/MM/yyyy` hợp lệ, `EndingDate >= StartingDate`,
    `CheckTotalDiscount=true` thì `TotalDiscountValue > 0`, và nếu có nhập giờ áp dụng thì
    `FromTime`/`ToTime` phải đủ cả 2 + đúng format `HH:mm` + `ToTime > FromTime`.
  - **Gotcha**: sửa/thêm rule validate phải sửa ở Repository, sửa `RequiredError` trên Razor chỉ
    cải thiện UX (hiện lỗi sớm hơn), không thay thế được rule thật.
- **Nút Lưu bị disable khi nào** (cập nhật 2026-07-10): `Disabled="@(_saving || !CanSave)"` —
  `_saving=true` chặn double-submit; `CanSave` (computed property) chặn bấm Lưu khi thiếu rõ ràng
  Tên CTKM/Hình thức bán/Loại CTKM (khi tạo mới)/Từ ngày/Đến ngày. Đây chỉ là UX giảm round-trip
  không cần thiết — **không thay thế** validate thật ở Repository (vd so sánh `EndingDate >=
  StartingDate` vẫn chỉ được kiểm ở server, `CanSave` không check việc này) — lỗi logic liên trường
  vẫn chỉ hiện ra **sau khi** bấm Lưu qua Snackbar.
- **Khóa sau khi duyệt**: `_isReadonly = _header.IsApprove` (gán khi mở lại 1 CTKM đã duyệt) —
  toàn bộ input/select/checkbox trong form đều bind `Disabled="_isReadonly"` riêng lẻ (không có
  1 `<fieldset disabled>` chung), và nút Lưu tạm/Duyệt bị ẩn hẳn (`@if (!_isReadonly)`) chứ không
  chỉ disable. SP `usp_SaveSetupCTKMAll` cũng tự chặn ở tầng DB (mục 10.3) — 2 lớp bảo vệ độc lập,
  không phụ thuộc UI.

### 10.3 Error Handling — lỗi từ SP trả về đâu

> **Đính chính 2026-07-10**: bản trước của mục này có 1 giả thuyết **sai** — "audit log lỗi khiến
> Save/Approve báo thất bại giả dù DB đã ghi thành công". Đã audit lại toàn bộ chuỗi
> `AuditLogger.LogAsync` → `DbAuditLogger` (`src/POS.Web/Auth/IAuditLogger.cs`) →
> `IFileLogHelper.WriteLogs` + `CentralMDRepository.InsertDashboardAuditLogAsync` — **cả 2 bước
> cuối đều tự bọc try/catch nuốt lỗi nội bộ** ("logging must never throw"). Audit log **KHÔNG BAO
> GIỜ** là nguyên nhân khiến Save/Approve báo lỗi giả — đã xóa đoạn cũ, thay bằng nội dung đúng bên
> dưới.

- **(Đã fix 2026-07-10)** Trước đây `PromotionRepository.SaveSetupAsync`/`ApproveSetupAsync` bọc
  `catch (Exception ex)` chung, trả trực tiếp `ex.Message` cho MỌI loại lỗi — nghĩa là message SQL
  Server thô (kể cả lỗi kết nối/timeout/SP chưa deploy) đi thẳng ra Snackbar không qua lớp dịch nào.
  Nay đã phân tầng: 2 SP (`usp_SaveSetupCTKMAll`, `usp_SetupPromotion_Approve`) dùng
  `THROW <number>, <message>, 1` với error number cố định trong dải nghiệp vụ (`51001` = "CTKM đã
  DUYỆT, không được lưu tạm"; `51002` = "Không tìm thấy CTKM" khi Duyệt) thay cho `RAISERROR` mặc
  định (number luôn = 50000 cho mọi ad-hoc message, không phân biệt được nguồn gốc lỗi).
  `PromotionRepository` giờ bắt `SqlException ex when KnownBusinessErrorNumbers.Contains(ex.Number)`
  trước (hiện nguyên `ex.Message` — an toàn, đây là message soạn sẵn cho end-user), fallback
  `catch (Exception ex)` cho mọi lỗi khác → log đầy đủ qua `IFileLogHelper.WriteExpLogs` + trả
  message chung **"Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT."** — không còn hiện message kỹ
  thuật thô ra UI.
- **Hệ quả cần biết khi đổi message nghiệp vụ trong SP**: đổi text sau `THROW <number>,` trong
  `.sql` script tương đương đổi luôn nội dung Snackbar hiển thị cho end-user — phải cập nhật
  **đồng thời** bảng "Validate" ở mục 6 và test case liên quan. Thêm 1 lỗi nghiệp vụ mới trong SP
  → **bắt buộc** thêm số hiệu vào `KnownBusinessErrorNumbers` (`PromotionRepository.cs`), nếu
  không sẽ rơi vào nhánh "Lỗi hệ thống" chung chung dù là lỗi nghiệp vụ hợp lệ.
- **Lỗi kỹ thuật thật** (mất kết nối, SP chưa deploy...) giờ hiện message chung, chi tiết kỹ thuật
  nằm trong file log (`IFileLogHelper.WriteExpLogs`, xem `D:\ROOT\Logs\...`) — đây vẫn là lý do mục
  0 "Điều kiện tiên quyết" nhấn mạnh phải chạy script SQL trước khi test: nếu SP chưa deploy,
  Snackbar giờ hiện "Lỗi hệ thống..." (không còn lộ `Could not find stored procedure...` thô), Dev
  phải mở file log để thấy nguyên nhân thật.
- **Approve lỗi** (`ApproveCoreAsync`) theo đúng pattern tương tự: `PromotionService.ApproveSetupAsync`
  trả `(Ok, Message)`, `false` → Snackbar đỏ hiện thẳng `message` (đã qua phân tầng ở trên); exception
  tầng Page (network/serialization...) mới rơi vào `catch` riêng của Page và bị thay bằng message
  tổng quát "Duyệt CTKM thất bại." (khác hẳn nhánh `!ok` — nhánh đó hiện đúng message gốc từ Service).
- **Audit log không chặn luồng chính (đã xác nhận fail-safe end-to-end)**: `AuditLogger.LogAsync`
  chạy **sau khi** SP đã Ok (Save) hoặc Approve đã Ok. Toàn bộ chuỗi bên trong nó
  (`fileLog.WriteLogs` + `InsertDashboardAuditLogAsync`) tự nuốt lỗi nội bộ — audit log lỗi (vd
  bảng chưa migrate) **không** ảnh hưởng tới thông báo Save/Approve, và **không** khiến Save/Approve
  báo sai kết quả.

### 10.4 Dependency — Service/Repository được inject

| Trong `PromotionSetupPage.razor` | Vai trò |
|---|---|
| `IPromotionService` | Nghiệp vụ CTKM — danh sách, chi tiết, lưu nháp, duyệt, options (Loại CTKM/Hình thức bán/Hạng thẻ/Nhóm cửa hàng), tìm sản phẩm, tìm CH/ST theo nhóm |
| `IFileLogHelper` | Ghi log lỗi ra file (`WriteExpLogs`) — dùng ở mọi `catch` thay message kỹ thuật cho Dev, tách khỏi Snackbar (message thân thiện cho user) |
| `ISnackbar` | Thông báo kết quả (Success/Warning/Error) cho mọi thao tác Load/Save/Approve |
| `IDialogService` | Mở `SiteGroupSetupDialog` (chọn nhiều nhóm CH/ST) và `ItemGroupSetupDialog` (cấu hình nhóm sản phẩm cho dòng Buy/Get) |
| `IAuditLogger` | Ghi audit log CREATE/UPDATE (Save) và APPROVE — action `SetupPromotion`, entity key = `BBYNR` |

- Chuỗi DI đầy đủ: `PromotionSetupPage` → `IPromotionService` (Application, thin wrapper) →
  `IPromotionRepository`/`PromotionRepository` (Infrastructure, chứa validate + gọi SP qua Dapper).
  **Không có tầng AppService HTTP** ở đây vì đây là truy vấn DB nội bộ, không phải external partner
  API — không áp dụng pattern "AppService 3 lớp" của `.claude/rules/architecture-layers.md`.

---

# PHẦN B — Danh mục khuyến mãi (`/promotion/offers`)

> Trang **downstream**, chỉ đọc + 2 thao tác phụ (Export Excel, Deactive) — KHÔNG soạn thảo CTKM.
> Dữ liệu hiển thị 100% đến từ bảng `Offer*` (bảng LIVE), tức là kết quả sau khi CTKM ở PHẦN A đã
> được **Duyệt** (xem A2 "Kiến trúc 2 giai đoạn"). CTKM chưa duyệt (còn ở `SetupPromotion*`) sẽ
> KHÔNG xuất hiện ở trang này.

## B1. Luồng UI

- **Filter**: Mã CTKM (search theo `No` HOẶC `PromotionNo`, xem B4), Tên CTKM, Từ ngày (mặc định
  hôm nay−1 năm, lọc `EndingDate >= FromDate`, không có "Đến ngày" — chủ đích), Mã sản phẩm, Loại
  CTKM, Trạng thái (Tất cả/Hiệu lực/Hết hiệu lực).
- **Bảng** (server-side, phân trang): STT, Action (Xem chi tiết + Deactive nếu đang Hiệu lực), Mã
  CTKM, Tên CTKM, OfferType, Trạng thái, Từ ngày, Đến ngày, SiteGroup, LimitQty, Ngày cập nhật.
- **Xem chi tiết**: mở dialog tra theo `BonusbuyNo` (= `No`), hiển thị đủ Offer Header/Buy/Get/
  Benefits/Site/Priority.
- **Deactive**: chỉ hiện khi đang Hiệu lực; set `Status=2` trên `OfferHeader`, **không thể hoàn
  tác** từ UI (xem ROLLOUT §D8).
- **Xuất Excel**: xuất đúng resultset đang lọc (không giới hạn trang), cột đầu tiên là "Mã CTKM".

## B2. Stored Procedure

| SP | Vai trò | Trạng thái deploy |
|---|---|---|
| `[dbo].[GetPromotionOfferHeaderList]` | Query chính cho list + export (12 branch theo tổ hợp `@ItemNo`/`@Status`/`@Exp`) | Cần chạy 2 script — xem §0 dòng 4 / ROLLOUT §D12 |
| `dbo.usp_OfferHeader_Deactivate` | Set `Status=2` cho nút Deactive | Cần chạy — xem §0 dòng 5 / ROLLOUT §D8 |

## B3. Quy tắc dữ liệu quan trọng (dễ nhầm)

- **"Mã CTKM" LUÔN LUÔN = `No`/`BonusbuyNo`/`BBYNR`** (khóa nghiệp vụ thật: `NOT NULL`, auto-gen
  chống trùng từ `6000000001`, dùng làm khóa join `OfferBuy/OfferGet/OfferBenefits/OfferSite`,
  dùng cho dialog chi tiết + Deactive + `ORDER BY` mặc định của SP). **KHÔNG BAO GIỜ** dùng cột
  `PromotionNo` (field phụ, nullable, không có cơ chế chống trùng, không FK nào tham chiếu) để
  hiển thị "Mã CTKM" — xem bug đã sửa ở B4.
- 1 CTKM có thể áp dụng nhiều `StoreNo` (bảng `OfferSite`) — SP đã xử lý bằng `EXISTS` thay vì
  `JOIN` để không fan-out dòng (script `GetPromotionOfferHeaderList_FixDuplicateRows.sql`, §0 dòng
  4). Cột `StoreNo` không hiển thị trên UI/Excel (luôn trả rỗng từ SP) — chủ đích, không phải bug.

## B4. Bug đã fix (2026-07-10) — cột "Mã CTKM" hiển thị trùng lặp

**Triệu chứng đã báo cáo**: nhiều dòng có cùng "Mã CTKM" (vd `1000000424` lặp 6 dòng) nhưng
`Tên CTKM`/`OfferType`/ngày tháng khác nhau hoàn toàn.

**Nguyên nhân**: regression từ 1 đợt dọn UI cùng ngày — gộp 2 cột "Bonus Buy" (`BonusbuyNo`) và
"Promotion No" (`PromotionNo`) thành 1 cột "Mã CTKM" nhưng bind nhầm vào `PromotionNo` (field phụ,
không unique). Nhiều offer khác nhau (khác `No` thật) vô tình share cùng `PromotionNo` → trông như
trùng lặp. **SP không có lỗi** — dữ liệu SP trả về luôn đúng (mỗi dòng là 1 `OfferHeader.No` khác
nhau).

**Đã sửa** (`src/POS.Web/Components/Pages/Promotion/Offers/OffersPage.razor`):
- Cột bảng "Mã CTKM" (RowTemplate): `@context.PromotionNo` → `@context.BonusbuyNo`.
- Excel export (`BuildXlsx`): header cột 1 `"Promotion No"` → `"Mã CTKM"`, giá trị
  `e.PromotionNo` → `e.BonusbuyNo`.
- **Không cần chạy SQL nào** cho fix này (thuần code C#/Razor).
- Verify: `dotnet build src/POS.Web/POS.Web.csproj` 0 error; `dotnet test tests/POS.ContractTests`
  39/39 pass. Chưa verify bằng mắt trên DB thật (sandbox thiếu DB/Redis) — **user đã tự test và
  xác nhận OK** (2026-07-10).
- Chi tiết đầy đủ: `docs/CHANGELOG.md` entry "[2026-07-10] Fix bug cột 'Mã CTKM' hiển thị trùng lặp
  trên OffersPage.razor".

## B5. TEST CASES — Danh mục khuyến mãi

> Tiền điều kiện: đã chạy đủ script §0 dòng 4-5; có ≥1 CTKM đã **Duyệt** ở PHẦN A (một số test cần
> CTKM áp dụng nhiều site để kiểm tra B3).

| ID | Mục tiêu | Bước thực hiện | Kết quả mong đợi |
|---|---|---|---|
| OFFERS-01 | Mở trang & phân quyền | Đăng nhập ITOps → mở `/promotion/offers` | Trang hiển thị danh sách + filter + nút Xuất Excel. StoreOperator bị chặn. |
| OFFERS-02 | **Regression — Mã CTKM đúng field** (bug B4) | Duyệt 1 CTKM mới ở `/promotion/setup` (ghi nhớ `BBYNR` sinh ra, vd `6000000001`) → mở `/promotion/offers`, tìm theo mã đó | Cột "Mã CTKM" hiển thị **đúng `BBYNR`** vừa duyệt; KHÔNG hiển thị giá trị `PromotionNo` (thường khác/rỗng). Mỗi dòng có `Tên CTKM`/`OfferType` khớp đúng CTKM tương ứng — KHÔNG có 2 dòng cùng "Mã CTKM" nhưng khác `Tên CTKM`. |
| OFFERS-03 | Không trùng dòng theo site (bug B3, cần D12 đã chạy) | Duyệt 1 CTKM có Site áp dụng ≥2 nhóm cửa hàng khác nhau (nhiều `StoreNo`) → tìm CTKM đó ở `/promotion/offers` | CTKM đó chỉ xuất hiện **đúng 1 dòng** trong bảng, không lặp lại theo số site. |
| OFFERS-04 | Filter "Từ ngày" (cần D12 đã chạy) | Đổi "Từ ngày" sang 1 mốc xa hơn (vd hôm nay−5 năm) → Tìm | Danh sách trả thêm các CTKM có `EndingDate >= mốc mới` nhưng bị ẩn ở mốc mặc định (hôm nay−1 năm). Không có "Đến ngày" để lọc mốc cuối. |
| OFFERS-05 | Xem chi tiết | Bấm icon "Xem chi tiết" 1 dòng | Dialog mở đúng offer (tra theo `No`/`BonusbuyNo`), hiển thị đủ tab Header/Buy/Get/Benefits/Site. |
| OFFERS-06 | Deactive (cần D8 đã chạy) | Bấm Deactive 1 offer đang Hiệu lực → xác nhận | Báo thành công; `Status` đổi "Hiệu lực" → "Hết hiệu lực"; nút Deactive biến mất khỏi dòng đó; **không có nút kích hoạt lại**. |
| OFFERS-07 | Xuất Excel | Lọc 1 tập kết quả → bấm "Xuất Excel" | File tải về đúng số dòng đang lọc (không chỉ trang hiện tại); cột đầu "Mã CTKM" khớp giá trị `BonusbuyNo` giống trên UI (đồng bộ sau fix B4). |
| OFFERS-08 | SP chưa deploy (negative test) | Trên môi trường **chưa chạy** script §0 dòng 4-5 → mở trang / bấm Deactive | Snackbar đỏ "Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT." (không lộ SQL thô); chi tiết lỗi thật nằm trong file log (xem §8 "Lỗi hệ thống được nuốt..."). |

## B6. Tham chiếu nguồn (Dev)

| Thành phần | Danh mục khuyến mãi |
|---|---|
| Page (Blazor) | `src/POS.Web/Components/Pages/Promotion/Offers/OffersPage.razor` |
| Service (Application) | `IPromotionService` / `PromotionService` — dùng chung service với PHẦN A |
| Repository (Infrastructure) | `IPromotionRepository` / `PromotionRepository` — method `GetOfferHeaderListAsync`/`ExportOfferHeaderListAsync`/`DeactivateOfferAsync` |
| DTO (Common) | `Dtos/Promotion/OfferHeaderDto.cs` — `OfferHeaderListItemDto`, `OfferListFilter` |
| SQL scripts | `docs/sql/GetPromotionOfferHeaderList_AddDateRangeFilter.sql`, `docs/sql/GetPromotionOfferHeaderList_FixDuplicateRows.sql`, `docs/sql/OfferHeader_Deactivate.sql` |

# Tài liệu nghiệp vụ & Test — Cài đặt CTKM và Special Combo (POS.Web)

> Phạm vi: 2 chức năng CRUD đã migrate sang POS.Web (Blazor Server .NET 10).
> - **11.1 — Cài đặt CTKM** (chương trình khuyến mãi): `/promotion/setup`
> - **11.2 — Special Combo** (combo sản phẩm): `/promotion/special-combo`
>
> Đối tượng đọc: BA, Dev, **Tester**. DB: `RPOSMasterData` (CentralMD).
> Quyền truy cập: vai trò **ITOps** hoặc **System Admin** (policy `OpsAndAbove`). StoreOperator KHÔNG vào được.

---

## 0. Điều kiện tiên quyết khi test (BẮT BUỘC)

Trước khi test, DBA phải chạy các script SQL tạo Stored Procedure trên `RPOSMasterData`
(xem `docs/ROLLOUT.md` §D1, §D2). Nếu chưa chạy → trang báo lỗi đỏ khi tải/lưu.

| Chức năng | Script cần chạy |
|---|---|
| 11.1 Cài đặt CTKM | `docs/sql/SetupPromotion_Save.sql`, `docs/sql/SetupPromotion_ApproveAndStatus.sql` (SP `Setup_Promotion_Insert` đã có sẵn) |
| 11.2 Special Combo | `docs/sql/SpecialCombo_Read.sql`, `docs/sql/SpecialCombo_Save.sql`, `docs/sql/SpecialCombo_Status.sql` |

**Bảng dữ liệu** (đã tồn tại): `SetupPromotionHEADER/BUY/GET/SITE`, `OfferHeader/OfferBuy/OfferGet/OfferBenefit/OfferSite`,
`SpecialComboHeader/Line/Store`, cùng các bảng tham chiếu `OfferType`, `SalesOrderType`, `Item`, `SetupGroupSites`, `OptionData`.

---

# PHẦN A — Cài đặt CTKM (Chương trình khuyến mãi)

## A1. Mục đích nghiệp vụ
Tạo/sửa **chương trình khuyến mãi** (CTKM / Bonus Buy): khai báo điều kiện **MUA** (Buy), phần **NHẬN/Chiết khấu** (Get),
phạm vi **cửa hàng** (Site), và các thiết lập nâng cao (voucher, hạng thẻ, ưu tiên...). CTKM soạn ở trạng thái **nháp**,
sau khi **Duyệt** sẽ được phát hành (publish) sang bảng "live" để máy POS áp dụng.

## A2. Kiến trúc 2 giai đoạn (rất quan trọng cho Tester)

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

## A3. Bảng dữ liệu

| Bảng | Vai trò | Khóa | Cột chính |
|---|---|---|---|
| `SetupPromotionHEADER` | Header CTKM nháp | `BBYNR` | SalesType, BBYTEXT(tên), BBYTYPE(loại), STATUS, VALIDFROM/VALIDTO (yyyyMMdd), IsVoucher, IsApprove, BUYLINKCAT/GETLINKCAT, + advance: LIMIT, VINID, MemberCode, ZPRIOR, NUMOFDAYS, ZVCDATE_ST/EN, ZVCDATE_VA, LIMITNR |
| `SetupPromotionBUY` | Dòng điều kiện mua | `BBYNR` | BUYTYPE(MAT/MGP), MAT_NR, MATGROUP, MAT_QUAN, MEINH, ScaleType |
| `SetupPromotionGET` | Dòng nhận/chiết khấu | `BBYNR` | GETTYPE, MATERIALCODE, MATGROUP, QTY, DISTYPE(%/R/P), BBYVAL, BBYPER |
| `SetupPromotionSITE` | Cửa hàng áp dụng | `BBYNR` | SITEGROUPCODE, SITECODE |
| `Offer*` (Header/Buy/Get/Benefit/Site) | Bảng LIVE sau duyệt | — | Do SP `Setup_Promotion_Insert` ghi |

## A4. Stored Procedures

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

## A5. Luồng thao tác trên màn hình (UI)

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

## A6. Validate (thông báo lỗi mong đợi)
- Thiếu Tên CTKM → "Vui lòng nhập tên chương trình khuyến mãi".
- Thiếu Hình thức bán → "Vui lòng chọn hình thức bán hàng".
- Thiếu Loại CTKM (và chưa có mã) → "Vui lòng chọn loại CTKM".
- Sai định dạng/thiếu ngày → "Ngày bắt đầu / kết thúc không đúng định dạng (dd/MM/yyyy)".
- Đến ngày < Từ ngày → "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu".
- Lưu CTKM đã duyệt → "CTKM {mã} đã được DUYỆT, không được phép lưu tạm".

## A7. TEST CASES — Cài đặt CTKM

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
| CTKM-13 | Duyệt CTKM (publish) | Mở CTKM nháp đã có Buy/Get/Site → bấm **Duyệt** → xác nhận | Báo "Duyệt CTKM {mã} thành công". `IsApprove=1`. Dữ liệu xuất hiện ở bảng `Offer*` và ở trang `/promotion/offers`. |
| CTKM-14 | Khóa sửa sau duyệt | Mở lại CTKM đã duyệt | Form **readonly** (chip "Đã duyệt — chỉ xem"); không có nút Lưu. |
| CTKM-15 | Lọc trạng thái duyệt | Danh sách → lọc "Đã duyệt" / "Chưa duyệt" | Bảng chỉ hiển thị đúng nhóm tương ứng. |
| CTKM-16 | Phân trang | Tạo >20 CTKM → đổi số dòng/trang, chuyển trang | Phân trang server-side đúng tổng số, không trùng/sót dòng. |
| CTKM-17 | Audit log | Sau Lưu/Duyệt | Có bản ghi trong `DashboardAuditLog` (action CREATE/UPDATE/APPROVE, entity `SetupPromotion`). |

---

# PHẦN B — Special Combo (Combo sản phẩm)

## B1. Mục đích nghiệp vụ
Khai báo **combo bán hàng**: 1 combo gồm nhiều **nhóm sản phẩm** (mỗi nhóm có số lượng min/max và danh sách item lựa chọn),
một **giá combo**, phạm vi **cửa hàng** áp dụng và (tùy chọn) áp dụng theo **hạng thẻ thành viên**.

## B2. Bảng dữ liệu

| Bảng | Vai trò | Khóa logic | Cột chính |
|---|---|---|---|
| `SpecialComboHeader` | Header combo | `Code` | SalesType, Name, ComboQuantity, Amount, IsMember, MemberCode, FromDate, ToDate, IsEnable, Counter |
| `SpecialComboLine` | Dòng SP (gom theo `GroupCode`) | `Code` + Pkey(`Code-GroupCode-ItemNo`) | ItemNo, ItemName, UOM, GroupCode, GroupName, MinimumQuantity, MaximumQuantity, [Order], IsDefault, IsDynamicPrice, IsRequired, IsSendSAP |
| `SpecialComboStore` | Cửa hàng áp dụng | `Code` + Pkey(`Code-StoreNo`) | StoreNo (`"ALL"` hoặc mã), IsEnable |

Tham chiếu (chỉ đọc): `Item`, `SalesPrice`, `PosGroupItem`.

## B3. Stored Procedures

| SP | Tham số | Mục đích |
|---|---|---|
| `usp_SpecialCombo_GetList` | `@StoreNo, @SalesType, @MemberType, @Status, @TextSearch, @PageSize, @PageNumber` | Danh sách header + phân trang (Total/row). |
| `usp_SpecialCombo_GetDetail` | `@Code` | 3 result set: header + tất cả lines + tất cả stores. |
| `usp_SpecialCombo_Save` | `@Code, @SalesType, @Name, @ComboQuantity, @Amount, @IsMember, @MemberCode, @FromDate, @ToDate, @IsEnable, @Actor` + 2 TVP (`@Lines, @Stores`) | Upsert header + **xóa-chèn lại** lines/stores (transaction). |
| `usp_SpecialCombo_UpdateStatus` | `@Code, @IsEnable, @Actor` | Bật/tắt combo. |
| `usp_SpecialCombo_Delete` | `@Code` | Xóa header + lines + stores (transaction). |

**Quy tắc dữ liệu:**
- `Code` auto-gen phía hệ thống = `S{yyyyMMddHHmmss}` khi tạo mới; khi sửa giữ nguyên.
- `PriceMode` (UI) → DB: `0`=Thường (`IsDefault=0, IsDynamicPrice=0`), `1`=Mặc định (`IsDefault=1, IsDynamicPrice=0`), `2`=Giá động (`IsDefault=1, IsDynamicPrice=1`).
- **Mỗi combo tối đa 1 dòng giá động** (`PriceMode=2`).
- Cửa hàng: tick "Áp dụng tất cả (ALL)" → 1 dòng `StoreNo='ALL'`; ngược lại lưu từng store đã chọn.

## B4. Luồng thao tác (UI `/promotion/special-combo`)
- **Danh sách**: lọc theo Cửa hàng / Hình thức bán / Hạng thẻ / Trạng thái / từ khóa (mã/tên). Mỗi dòng: **Sửa**, **Bật/Tắt**, **Xóa**.
- **Form (3 tab)**: Thông tin chung (Tên*, HTB, Số lượng, Giá trị, Từ/Đến ngày*, Có hiệu lực, Hội viên WIN + Hạng thẻ) | Sản phẩm (bảng dòng: Mã nhóm, Tên nhóm, Sản phẩm, ĐVT, SL min/max, Loại giá) | Cửa hàng (ALL hoặc chọn nhiều).
- Nút **Lưu** (replace-on-save).

## B5. Validate (thông báo lỗi mong đợi)
- Thiếu Tên combo → "Vui lòng nhập tên combo".
- Không có dòng sản phẩm → "Combo phải có ít nhất 1 sản phẩm".
- >1 dòng giá động → "Mỗi combo chỉ được tối đa 1 sản phẩm có giá bán động".
- Sai/thiếu ngày → "Ngày bắt đầu / kết thúc không đúng định dạng (dd/MM/yyyy)".
- Đến ngày < Từ ngày → "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu".

## B6. TEST CASES — Special Combo

> Tiền điều kiện: đã chạy 3 script §0; đăng nhập ITOps/Admin; có sản phẩm trong `Item`, store trong `Store`,
> hạng thẻ trong `OptionData(Caption='MEMBERCODETYPE')`.

| ID | Mục tiêu | Bước thực hiện | Kết quả mong đợi |
|---|---|---|---|
| COMBO-01 | Mở trang & phân quyền | Đăng nhập ITOps → `/promotion/special-combo` | Hiển thị danh sách + nút "Thêm combo". StoreOperator bị chặn. |
| COMBO-02 | Tạo combo hợp lệ | Thêm combo → Tên, HTB, Số lượng, Giá trị, Từ/Đến ngày → tab Sản phẩm thêm 2 dòng (1 nhóm) → tab Cửa hàng để "ALL" → **Lưu** | Báo "Lưu combo {Code} thành công". Sinh `Code=S{...}`. `SpecialComboHeader` 1 dòng, `SpecialComboLine` 2 dòng, `SpecialComboStore` 1 dòng (`ALL`). |
| COMBO-03 | Thiếu tên | Bỏ trống Tên → Lưu | Báo "Vui lòng nhập tên combo". |
| COMBO-04 | Combo rỗng sản phẩm | Không thêm dòng nào → Lưu | Báo "Combo phải có ít nhất 1 sản phẩm". |
| COMBO-05 | Quy tắc 1 giá động | Thêm 2 dòng đều đặt Loại giá = "Giá động" → Lưu | Báo "Mỗi combo chỉ được tối đa 1 sản phẩm có giá bán động"; không ghi DB. |
| COMBO-06 | Map PriceMode | Thêm dòng Loại giá = "Giá động" (=2) → Lưu → kiểm DB | `SpecialComboLine.IsDynamicPrice=1`, `IsDefault=1`. Dòng "Thường" (=0): cả hai = 0. |
| COMBO-07 | Validate ngày | Đến ngày < Từ ngày → Lưu | Báo lỗi ngày kết thúc. |
| COMBO-08 | Chọn cửa hàng cụ thể | Bỏ tick ALL → thêm 2 store qua ô tìm → Lưu | `SpecialComboStore` 2 dòng đúng mã store; KHÔNG có "ALL". |
| COMBO-09 | Nhóm sản phẩm | Thêm dòng nhóm A (2 item) + nhóm B (1 item), nhập Min/Max mỗi nhóm → Lưu → mở lại | Lines gom đúng theo `GroupCode`; Min/Max, ĐVT, item hiển thị lại đúng. |
| COMBO-10 | Sửa combo (replace) | Mở combo → xóa 1 dòng, thêm 1 dòng khác → Lưu | Số dòng `SpecialComboLine` cập nhật đúng (xóa cũ, chèn mới); `Counter` tăng. |
| COMBO-11 | Bật/Tắt | Ở danh sách bấm Tắt → bấm Bật | `IsEnable` đổi 0/1; chip trạng thái cập nhật; có audit STATUS. |
| COMBO-12 | Xóa combo | Bấm Xóa → xác nhận | Xóa cả 3 bảng theo `Code`; combo biến mất khỏi danh sách; audit DELETE. |
| COMBO-13 | Lọc danh sách | Lọc theo Hình thức bán / Hạng thẻ / Trạng thái / từ khóa | Kết quả đúng điều kiện; phân trang server-side chính xác. |
| COMBO-14 | Lọc theo cửa hàng | Lọc theo 1 store cụ thể | Hiển thị combo có store đó **hoặc** combo "ALL". |
| COMBO-15 | Hội viên WIN | Bật "Hội viên WIN" + chọn Hạng thẻ → Lưu → mở lại | `IsMember=1`, `MemberCode` đúng; ô Hạng thẻ chỉ bật khi tick Hội viên. |
| COMBO-16 | Auto-gen mã | Tạo 2 combo liên tiếp | Mỗi combo có `Code` khác nhau dạng `S{yyyyMMddHHmmss}`. |
| COMBO-17 | Audit log | Sau Create/Update/Status/Delete | `DashboardAuditLog` có bản ghi tương ứng (entity `SpecialCombo`). |

---

## C. Ghi chú kỹ thuật & ngoài phạm vi (cho QA biết)

- **Cài đặt CTKM** — phần đã **hoãn** (không cần test): Giờ áp dụng (TIMEFROM/TO), Ngày-trong-tuần (Mon–Sun),
  AllowUseAfterDay/Time (DB chưa có cột).
- **Special Combo** — phần đã **hoãn**: bật/tắt trạng thái từng cửa hàng riêng lẻ, các modal quản lý item/store tách rời,
  nhập danh sách store dạng chuỗi thủ công (V3 legacy).
- Cả hai dùng cơ chế **replace-on-save** (lưu = xóa hết dòng con theo khóa rồi chèn lại) → khi sửa, các dòng bị bỏ trên
  form sẽ biến mất khỏi DB. Tester lưu ý khi đối chiếu số dòng.
- Lỗi hệ thống được nuốt và hiển thị snackbar đỏ + ghi log file (`D:\ROOT\Logs\POS.Web\Exception\log-yyyyMMdd.txt`).
  Khi gặp lỗi không rõ, lấy dòng log mới nhất để Dev phân tích.

## D. Tham chiếu nguồn (Dev)
| Thành phần | 11.1 Cài đặt CTKM | 11.2 Special Combo |
|---|---|---|
| Page (Blazor) | `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` | `.../Offers/SpecialComboPage.razor` |
| Service (Application) | `IPromotionService` / `PromotionService` | `ISpecialComboService` / `SpecialComboService` |
| Repository (Infrastructure) | `IPromotionRepository` / `PromotionRepository` | `ISpecialComboRepository` / `SpecialComboRepository` |
| DTO (Common) | `Dtos/Promotion/PromotionSetupDto.cs` | `Dtos/Promotion/SpecialComboDto.cs` |
| SQL scripts | `docs/sql/SetupPromotion_Save.sql`, `SetupPromotion_ApproveAndStatus.sql` | `docs/sql/SpecialCombo_Read.sql`, `SpecialCombo_Save.sql`, `SpecialCombo_Status.sql` |

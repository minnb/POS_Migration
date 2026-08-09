# AUDIT: UI Data Logic vs Backend/DB Procedure — Trang Cài đặt CTKM

> **Phạm vi:** đối chiếu chéo `PromotionSetupPage.razor` (UI) với **toàn bộ pipeline** xử lý dữ
> liệu phía sau: `usp_SaveSetupCTKMAll` (Lưu tạm) → `Setup_Promotion_Insert` (Duyệt/publish) →
> `offer_procedure.sql` (30 stored procedure `BLUEPOS_PRO_Cal_*` — engine tính khuyến mãi chạy
> trên POS lúc bán hàng).
>
> **Phương pháp:** đọc trực tiếp toàn bộ 4 nguồn (Razor 1410 dòng, DTO, `PromotionRepository.cs`,
> 3 file SQL) + **Grep xác minh độc lập** trên `offer_procedure.sql` (đếm số lần một cột DB thực
> sự được tham chiếu trong 28 stored procedure của engine) — không suy đoán, không chỉ dựa vào tài
> liệu đã có (`PROMOTION_SETUP_MANUAL.md`).
>
> **⚠️ Giới hạn đã biết:** `offer_procedure.sql` chứa nhiều biến thể cho cùng 1 loại (`_NEW`,
> `_BK` = backup, `_Duplicate`) — không có cách nào xác định tĩnh (chỉ đọc SQL) biến thể nào đang
> thực sự được gọi bởi POS terminal (logic gọi engine nằm ngoài repo này). Với các phát hiện dưới
> đây, tôi đã kiểm tra **trên toàn bộ 28 proc gộp lại** (không phân biệt biến thể) — nếu 1 cột
> **0 lần xuất hiện trên toàn file**, kết luận "không được đọc" là chắc chắn bất kể biến thể nào
> đang chạy. Với cột có xuất hiện nhưng ít (vd `ConditionBuy`/`ConditionGet`), tôi đánh dấu
> **PLAUSIBLE** thay vì **CONFIRMED** vì không loại trừ được khả năng biến thể đang chạy có using đó.

---

## 1. Data Payload Model (UI → Backend)

Payload thực tế khi bấm "Lưu tạm" (`SaveAsync()` → `PromotionService.SaveSetupAsync(request)`):

```typescript
interface PromotionSetupSaveRequest {
  Header: PromotionSetupHeaderDto;
  BuyRows: OfferBuyLineDto[];   // [] nếu tab Buy đang ẩn theo OfferType
  GetRows: OfferGetLineDto[];   // [] nếu tab Get đang ẩn theo OfferType
  SiteGroupCodes: string[];     // suy từ _siteRows, lọc rỗng
}

interface PromotionSetupHeaderDto {
  No: string; Description: string; SalesType: string; OfferType: string;
  Status: "0"|"1"|"2";
  StartingDate: string; EndingDate: string;        // "dd/MM/yyyy"
  IsVoucher: boolean; IsApprove: boolean;
  ConditionBuy: "AND"|"OR"; ConditionGet: "AND"|"OR";
  LimitQty: number; MemberOnly: boolean; MemberCode: string;
  PriorityBBY: number;                              // 1..10, default 1
  NumOfDays: number;                                 // KHÔNG có control UI nào set — luôn 0
  ApplyDaysOfMonth: number[];                        // multi-select 1..31, cơ chế MỚI (NUMOFDAYSLIST)
  VoucherFromDate: string; VoucherToDate: string;
  VoucherValidDay: number; VoucherLimitNumber: number;
  AllowUseAfterDay: number; AllowUseAfterTime: string;
  FromTime: string; ToTime: string;                  // "HH:mm"
  Mon: boolean; Tue: boolean; Wed: boolean; Thu: boolean; Fri: boolean; Sat: boolean; Sun: boolean;
  MinValue: number;
  IsTotalBill: boolean;                              // KHÔNG control UI nào set — server tự recompute
  MaxQuantity: number;
  CheckTotalDiscount: boolean; TotalDiscountType: 0|1|2; TotalDiscountValue: number;
}

interface OfferBuyLineDto /* = OfferGetLineDto */ {
  LineType: 0|1; No: string; GroupCode: string; Description: string; UnitOfMeasure: string;
  Quantity: number; ScaleType: "A"|"B"|"C"; DiscountType: 0|1|2; DiscountValue: number;
}
```

## 2. Validation logic trên UI (client-side)

Đầy đủ ở [CẦN DUYỆT 1] trong hội thoại — tóm tắt: chỉ chặn thiếu field hiển nhiên (`CanSave`),
`Required` trên 5 field cơ bản, ẩn/hiện tab theo cờ OfferType, không có `MaxLength`/format-check
cho `MemberCode`/`AllowUseAfterTime`, `MudNumericField Min=` chỉ chặn spinner (không chặn gõ tay).

## 3. Giải phẫu Procedure — 3 tầng

### 3.1 `usp_SaveSetupCTKMAll` (`docs/sql/SetupPromotion_Save.sql`) — SP nhận payload UI trực tiếp

| Tham số | Kiểu | Nullable/Default | Ghi chú |
|---|---|---|---|
| `@BBYNR` | `nvarchar(20)` OUTPUT | — | rỗng = tạo mới (auto-gen từ `MAX(BBYNR)+1`, khoá `UPDLOCK,HOLDLOCK`) |
| `@SalesType`,`@Description`,`@OfferType`,`@Status` | `nvarchar` | bắt buộc (không default) | — |
| `@ValidFrom`,`@ValidTo` | `nvarchar(8)` | bắt buộc | yyyyMMdd |
| `@IsVoucher` | `bit` | bắt buộc | — |
| `@BuyLinkCat`,`@GetLinkCat` | `nvarchar(1)` | default `'A'` | 'A'=AND,'O'=OR |
| `@LimitQty`...`@AllowUseAfterTime` (12 tham số Advanced) | `nvarchar`/`bit` | **đều có default** | backward-compat |
| `@FromTime`,`@ToTime`,`@Mon`..`@Sun` | `nvarchar` | default `''` | — |
| `@MinValue` | `nvarchar(50)` | default `'0'` | — |
| `@TotalMinValue` | `int` | default `0` | **KHÔNG lấy từ payload `Header.IsTotalBill`** — C# tự tính lại (xem Gap #6) |
| `@MaxQuantity` | `int` | default `0` | — |
| `@CheckTotalDiscount`..`@TotalDiscountValue` | `nvarchar` | default | — |
| `@Buy`,`@Get`,`@Site` | TVP (`READONLY`) | — | xem cấu trúc TVP dưới |

**TVP `SetupPromotionBuyTVP`/`GetTVP`**: `Quantity nvarchar(50)`, `DiscountValue nvarchar(50)` —
**kiểu chuỗi, không phải numeric** — dù DTO C# là `decimal`.

**Rẽ nhánh chính:**
- Dòng 150-156: nếu `@BBYNR` rỗng → auto-gen số mới.
- Dòng 163-170: nếu CTKM đã `IsApprove=1` → `THROW 51001` (từ chối lưu tạm CTKM đã duyệt).
- Dòng 173-262: UPSERT theo tồn tại `BBYNR`; nhánh INSERT còn rẽ tiếp theo `sys.columns` xem cột
  `ID` có phải IDENTITY hay không (khác biệt môi trường DEV/UAT/PROD).
- Dòng 264-297: REPLACE-ON-SAVE cho Buy/Get/Site (DELETE hết theo BBYNR rồi INSERT lại từ TVP).

### 3.2 `Setup_Promotion_Insert` (Duyệt/publish Setup*→Offer*) — **41 cột `OfferHeader`**

Đọc trực tiếp `docs/web/offers/Setup_Promotion_Insert.sql` (279 dòng, UTF-16). Danh sách đầy đủ 41
cột `INSERT INTO OfferHeader` (dòng 60-70) — dùng để so khớp với payload UI ở mục 4.

Rẽ nhánh chính:
- Dòng 92-105: **auto vô hiệu voucher thiếu ngày hợp lệ** — `UPDATE OfferHeader SET Status=2` khi
  `OfferType IN ('ZB13','ZB14','ZB15','ZB16')` và (VoucherFromDate/To/ValidDay đều ở giá trị mặc
  định "chưa nhập") — **chỉ trigger nếu `IsVoucher=1`**.
- Dòng 111-155: `OfferBuy` — loại trừ tường minh `H.BBYTYPE NOT IN ('ZB06','ZB13')`; `LineGroup`
  suy từ `BUYLINKCAT` ('O'→'B1' cố định, else tăng dần 'B1','B2'...).
- Dòng 161-202: `OfferGet` — CHỈ insert khi `ISNULL(H.TOTALMINVALUE,0) = 0`.
- Dòng 209-243: `OfferBenefits` — CHỈ insert khi `ISNULL(H.TOTALMINVALUE,0) = 1`,
  `StepAmount = convert(float, H.MINVALUE)`.
- Dòng 248-260: `OfferSite` — copy nguyên `SetupPromotionSITE`, **không có điều kiện nào** (kể cả
  0 dòng vẫn "thành công").

### 3.3 `offer_procedure.sql` — 28 stored procedure `BLUEPOS_PRO_Cal_*` (engine POS)

Danh sách đầy đủ (grep `CREATE PROC`):

```
ZB006_ByOfferNo, ZB006_Duplicate, ZB012_ByOfferNo, ZB012_Duplicate, ZB013_ZB14,
ZB05_ZB10, ZB05_ZB10_NEW, ZB05_ZB10_NEW_BK_08062026,
ZB07, ZB07_Group, ZB07_Group_Duplicate_NEW, ZB07_Group_NEW, ZB07_Item, ZB07_Item_NEW, ZB07_NEW,
ZB07_ZB08, ZB07_ZB08_Group, ZB07_ZB08_Item, ZB08, ZB15, ZB16,
ZB17_ByOfferNo, ZB17_ByOfferNo_BK, ZB17_Duplicate, ZB17_Duplicate_BK,
ZB21, ZB21_TenderType, ZB22, ZB24
```

Mỗi proc nhận `@OfferNo`/`@TransNo`/`@StoreNo`/`@TransDate`/context giao dịch — **không nhận** JSON
payload từ UI. Bảng tần suất tham chiếu cột `OfferHeader`/`OfferBenefits` liên quan tính năng UI
(đếm bằng Grep trên toàn bộ 399KB file, không phân biệt biến thể):

| Cột / cơ chế UI cấu hình | Số lần xuất hiện trong `offer_procedure.sql` | Kết luận |
|---|---|---|
| `LimitQty` | 86 | Được đọc rộng rãi — hoạt động đúng như tài liệu |
| `MemberOnly`/`MemberAttribute`/`MemberCode` | 31 | `MemberOnly` được filter (vd dòng 2026); `MemberAttribute`/`MemberCode` xuất hiện nhưng cần review riêng nếu cần khoá theo hạng thẻ |
| `VoucherValidDay`/`VoucherLimitNumber`/`VoucherFromDate`/`VoucherToDate` | 30 | Được dùng cho luồng voucher (ZB13/14/15/16) |
| `TotalDiscountType`/`TotalDiscountValue` | 10 | Được dùng (whole-bill discount, ZB21/ZB09-family) |
| `FromTime`/`ToTime` | **3** (chỉ ở ZB013_ZB14 dòng 2027, ZB15 dòng 8050, ZB16 dòng 8245) | **CHỈ áp dụng cho voucher ZB13/14/15/16** — KHÔNG áp dụng cho ZB02/05/06/07/08/10/12 |
| `Mon`/`Tue`/`Wed`/`Thu`/`Fri`/`Sat`/`Sun`, `DayOfWeek` | **0** | **KHÔNG được calc-proc nào đọc** — toàn bộ 28 proc |
| `NumOfDays` | **0** | **KHÔNG được calc-proc nào đọc** |
| `PriorityBBY` | **0** | **KHÔNG được calc-proc nào đọc** — engine dùng bảng `OfferPriority` (theo `OfferType`, không phải theo CTKM) qua `LEFT JOIN`, và bản thân join này cũng không thấy được dùng để `ORDER BY` trong `ZB013_ZB14` (biến `@Priority` khai báo nhưng không gán/dùng lại) |
| `OfferMaxQuantity` (bảng) | **0** | Khớp `PROMOTION_SETUP_MANUAL.md` — xác nhận độc lập, đúng lý do GATED |
| `ConditionBuy`/`ConditionGet` | **2** (dòng 5653-5654 trong `ZB07_Item_NEW`, dòng 11060 trong `ZB24` — ZB24 ngoài phạm vi trang setup) | PLAUSIBLE — chỉ 1/28 proc trong phạm vi trang này đọc field, và đó là biến thể `_NEW` (không chắc có phải bản đang chạy) |
| `AllowUseAfterDay`/`AllowUseAfterTime` (`ZVCDAY_AFTER`/`ZVCTIME_AFTER`) | **0** | Không đọc được ở tầng calc-proc — nhưng nguyên nhân sâu hơn: **cột này còn không được publish sang `OfferHeader`** (xem Gap #2) |

---

## 4. Data Mapping Matrix (UI field → nháp → live → calc-engine)

| Trường UI | DTO | `SetupPromotionHEADER` (nháp) | `OfferHeader` (live, sau Duyệt) | Calc-engine đọc? |
|---|---|---|---|---|
| Tên CTKM | `Description` | `BBYTEXT` | `Description` | — |
| Loại CTKM | `OfferType` | `BBYTYPE` | `OfferType` | ✅ (điều hướng proc) |
| Từ/Đến ngày | `StartingDate`/`EndingDate` | `VALIDFROM`/`VALIDTO` | `StartingDate`/`EndingDate` | ✅ |
| **Từ giờ/Đến giờ** | `FromTime`/`ToTime` | `TIMEFROM`/`TIMETO` | `FromTime`/`ToTime` | ⚠️ **CHỈ** ZB13/14/15/16 |
| **Mon..Sun** | `Mon`..`Sun` | `MON`..`SUN` | `Mon`..`Sun`, `DayOfWeek` | ❌ **KHÔNG proc nào đọc** |
| Ngưỡng tổng bill | `MinValue` | `MINVALUE` | `MinValue` + `OfferBenefits.StepAmount` | ✅ |
| Cờ tổng bill | *(server tự tính)* | `TOTALMINVALUE` | `IsTotalBill` | ✅ (bẻ hướng publish Get→Benefits) |
| Giới hạn KH | `LimitQty` | `LIMIT` | `LimitQty` | ✅ (86 lần) |
| **Độ ưu tiên (1-10)** | `PriorityBBY` | `ZPRIOR` | `PriorityBBY` | ❌ **KHÔNG proc nào đọc** |
| **Giới hạn SL KM tối đa** | `MaxQuantity` | `MaxQuantity` | *(gated)* `OfferMaxQuantity` | ❌ (đã biết, GATED có chủ đích) |
| **Ngày áp dụng trong tháng** | `ApplyDaysOfMonth` | `NUMOFDAYSLIST` (JSON) | *(KHÔNG publish)* | ❌ (đứt tại publish — không tới được `OfferHeader`) |
| Ngày trong tháng (cũ) | `NumOfDays` | `NUMOFDAYS` | `NumOfDays` | ❌ **KHÔNG proc nào đọc** (và UI không hề set field này) |
| **Được dùng sau N ngày/giờ** | `AllowUseAfterDay`/`Time` | `ZVCDAY_AFTER`/`ZVCTIME_AFTER` | *(KHÔNG publish)* | ❌ (đứt tại publish) |
| Voucher từ/đến/hiệu lực/SL | `Voucher*` | `ZVCDATE_*`/`LIMITNR` | `Voucher*` | ✅ (30 lần) |
| Điều kiện Buy/Get (AND/OR) | `ConditionBuy`/`Get` | `BUYLINKCAT`/`GETLINKCAT` | `ConditionBuy`/`Get` | ⚠️ PLAUSIBLE — chỉ 1 proc trong phạm vi trang (biến thể `_NEW`) |
| Chỉ thành viên | `MemberOnly` | `VINID` | `MemberOnly` | ✅ |
| Nhóm cửa hàng | `SiteGroupCodes` | `SITEGROUPCODE/SITECODE` | `OfferSite` | ✅ (JOIN lọc theo StoreNo) |

---

## 5. Vấn đề cần khắc phục (Gap Analysis)

### 🔴 Nghiêm trọng

**#1 — Lịch áp dụng theo NGÀY TRONG TUẦN (Mon–Sun) không có tác dụng với BẤT KỲ loại CTKM nào.**
UI hiển thị 7 checkbox Mon..Sun (tab "Thông tin chung", mặc định tick cả tuần) như một điều kiện
áp dụng thật. Dữ liệu được lưu đúng vào `SetupPromotionHEADER.MON..SUN` và publish đúng vào
`OfferHeader.Mon..Sun`/`DayOfWeek` (verify được qua `Setup_Promotion_Insert.sql` dòng 79-80). Nhưng
**Grep xác nhận 0/28 stored procedure `BLUEPOS_PRO_Cal_*` tham chiếu các cột này** — không proc nào
gọi `DATEPART(weekday,...)` hay so `O.Mon`/`O.Tue`... Kết quả: một CTKM cấu hình "chỉ áp dụng Thứ 2
– Thứ 6" trên UI **thực chất áp dụng đủ 7 ngày/tuần** trên POS thật. Rủi ro: sai lệch chi phí
khuyến mãi ngoài dự kiến vào cuối tuần.
*Verify:* `Select-String -Pattern "Mon|Tue|Wed|Thur|Sun|DayOfWeek"` trên `offer_procedure.sql` → 0 kết quả.

**#2 — 2 field Voucher delay (`AllowUseAfterDay`/`AllowUseAfterTime`) bị MẤT ở bước Duyệt/publish.**
UI cho nhập "Được dùng sau N ngày" / "Được dùng sau giờ" (tab Nâng cao, chỉ hiện khi Voucher). Dữ
liệu **có** được lưu đúng vào nháp `SetupPromotionHEADER.ZVCDAY_AFTER`/`ZVCTIME_AFTER` (xác nhận qua
`usp_SaveSetupCTKMAll` dòng 197-198, 231, 251). Nhưng đọc trực tiếp câu lệnh `INSERT INTO
OfferHeader (...)` trong `Setup_Promotion_Insert.sql` (dòng 60-70, 41 cột) — **2 cột này hoàn
toàn không có mặt** trong danh sách cột lẫn trong `SELECT`. Dữ liệu tồn tại ở bảng nháp nhưng
**không bao giờ đến được `OfferHeader`** khi Duyệt → engine (dù có đọc cột này hay không, đã xác
nhận 0/28 proc đọc) **không thể** thực thi rule "chỉ dùng voucher sau N ngày kể từ lúc ra bill" vì
dữ liệu chưa từng tới nơi. Đây là lỗi ở đúng SP publish, không phải ở UI hay Save SP.
*Verify:* đọc cột 60-70 của `Setup_Promotion_Insert.sql` — không có `ZVCDAY_AFTER`/`ZVCTIME_AFTER`
hay alias `AllowUseAfterDay`/`AllowUseAfterTime`.

### 🟠 Cao

**#3 — "Độ ưu tiên (1–10)" (`PriorityBBY`/`ZPRIOR`) không có tác dụng.**
Publish đúng vào `OfferHeader.PriorityBBY` (verify `Setup_Promotion_Insert.sql` dòng 84) nhưng
**0/28 calc-proc tham chiếu cột này**. Cơ chế ưu tiên THẬT của engine là bảng `dbo.OfferPriority`
(khoá theo `OfferType`, KHÔNG theo từng CTKM cụ thể) — join xuất hiện ở `ZB013_ZB14`
(dòng 2021), `ZB012*`, `ZB16`, `ZB17*` nhưng bản thân giá trị join (`P.???`) không thấy được dùng
tiếp để `ORDER BY`/so sánh ở `ZB013_ZB14` (biến `@Priority int` khai báo dòng 2008 nhưng không
gán/đọc lại — có thể là code chết trong chính engine, ngoài phạm vi sửa của UI). Kết luận: field
"Độ ưu tiên" trên UI đang **đánh lừa người dùng nghiệp vụ** rằng có thể ưu tiên CTKM theo từng CTKM
cụ thể, trong khi engine (nếu có ưu tiên hoạt động) chỉ phân biệt theo **loại** OfferType.

**#4 — Không có validation bắt buộc "Nhóm cửa hàng áp dụng" (Site) phải ≥ 1 dòng.**
Đọc toàn bộ `PromotionRepository.SaveSetupAsync` (16 rule validate, dòng 254-310) — không có rule
nào kiểm tra `request.SiteGroupCodes`. `usp_SaveSetupCTKMAll` cũng không chặn (`@Site` TVP rỗng vẫn
COMMIT bình thường). Hệ quả: 1 CTKM có thể được Lưu + Duyệt thành công với `OfferSite` = 0 dòng —
CTKM tồn tại "hợp lệ" trên hệ thống nhưng **không áp dụng ở bất kỳ cửa hàng nào**, không có cảnh
báo nào cho người dùng biết điều này.

### 🟡 Trung bình

**#5 — "Ngày áp dụng trong tháng" (`ApplyDaysOfMonth`/`NUMOFDAYSLIST`) không publish sang `OfferHeader`.**
Đây là cơ chế MỚI (multi-select ngày 1-31) thay thế `NumOfDays` cũ. Theo đúng chú thích trong code
(`PromotionSetupDto.cs` dòng 38, `SetupPromotion_Save.sql` bản 4) đây là quyết định **có chủ đích**
— cột `NUMOFDAYSLIST` chỉ tồn tại ở bảng nháp, chưa publish. Khác Gap #2 (đứt ngoài ý muốn), đây là
tính năng **UI cho nhập nhưng công khai chưa nối tới engine** — nếu người dùng nghiệp vụ không biết
điều này, họ có thể lầm tưởng đã cấu hình xong. Field `NumOfDays` cũ (cột thực sự publish) thì lại
**UI không có control nào set** (luôn = 0) — hai nửa cùng 1 tính năng ("giới hạn ngày trong tháng")
đều không hoạt động trọn vẹn theo 2 lý do khác nhau.

**#6 — `Header.IsTotalBill` là field "chết" trong luồng ghi (dead field), tiềm ẩn rủi ro cho dev sau.**
DTO có field `IsTotalBill` (dòng 63, `PromotionSetupHeaderDto`) nhưng **`PromotionSetupPage.razor`
không có bất kỳ control nào gán giá trị cho nó** — trường `_header.IsTotalBill` luôn giữ default
`false` khi tạo mới (chỉ được đọc lại từ DB khi mở sửa, qua `GetSetupDetailAsync`). Khi Lưu,
`PromotionRepository.SaveSetupAsync` dòng 286 **tính lại từ đầu** `isTotalBill = ot?.IsTotalBill ??
false` (tra theo OfferType đang chọn, từ cache Redis) — **hoàn toàn bỏ qua** giá trị
`request.Header.IsTotalBill` gửi lên. Hiện tại không phải bug (giá trị luôn được tính đúng ở
server, cách ly khỏi input không tin cậy từ client — thực ra là *pattern an toàn*), nhưng field
tồn tại trong DTO/JSON payload có thể khiến dev sau tưởng nhầm client kiểm soát được cờ này.

### 🟢 Thấp / Ghi nhận (không phải bug)

**#7 — Kiểu dữ liệu Quantity/DiscountValue: decimal (C#) → `nvarchar(50)` (TVP) → `float` (SQL) — ĐÃ XÁC MINH AN TOÀN.**
`PromotionRepository.BuildBuyTable`/`BuildGetTable` (dòng 985-986, 1003-1004) dùng
`.ToString(CultureInfo.InvariantCulture)` nhất quán cho mọi field decimal trước khi đưa vào TVP —
tránh lỗi convert do dấu thập phân theo văn hoá (vd dấu phẩy VN). Không phát hiện type-mismatch
runtime ở khâu này.

**#8 — `MemberCode`/`AllowUseAfterTime` không có validate định dạng.**
`MemberCode` là ô tự gõ (`CoerceText=false`) không bắt buộc khớp gợi ý; `AllowUseAfterTime` chỉ có
placeholder "hh:mm:ss", không `MaxLength`, không parse-check ở cả client lẫn server (khác
`FromTime`/`ToTime` có `TimeSpan.TryParseExact`). Vì Gap #2 đã xác nhận field này chưa từng tới
được `OfferHeader`, rủi ro thực tế hiện = 0, nhưng nếu Gap #2 được fix trong tương lai (bổ sung
cột publish), format tự do này sẽ cần validate trước.

---

## 6. Tổng hợp mức độ tin cậy bằng chứng

| # | Phát hiện | Mức tin cậy | Cách verify độc lập |
|---|---|---|---|
| 1 | Mon-Sun/DayOfWeek không được đọc | **CONFIRMED** | Grep case-insensitive 0/28 proc, đã loại trừ biến thể |
| 2 | AllowUseAfterDay/Time mất ở publish | **CONFIRMED** | Đọc trực tiếp cột INSERT `Setup_Promotion_Insert.sql` dòng 60-70 |
| 3 | PriorityBBY không được đọc | **CONFIRMED** | Grep 0/28 proc; `OfferPriority` join tồn tại nhưng không dùng giá trị |
| 4 | Thiếu validate Site ≥ 1 | **CONFIRMED** | Đọc toàn bộ 16 rule validate trong `SaveSetupAsync` |
| 5 | NUMOFDAYSLIST không publish | **CONFIRMED** | Đối chiếu cột INSERT + code comment `PromotionSetupDto.cs` |
| 6 | IsTotalBill dead field | **CONFIRMED** | Đọc `OnOfferTypeChanged`/`SaveAsync` (razor) + `SaveSetupAsync` dòng 286 (repo) |
| 7 | Type-safety decimal→TVP | **CONFIRMED an toàn** (không phải gap) | Đọc `BuildBuyTable`/`BuildGetTable` |
| FromTime/ToTime chỉ áp dụng ZB13-16 | **CONFIRMED** | Grep 3/28 proc, đúng 3 dòng thuộc ZB013_ZB14/ZB15/ZB16 |
| ConditionBuy/Get ít được đọc | **PLAUSIBLE** (chưa CONFIRMED) | Chỉ xác nhận được trên văn bản SQL tĩnh; không xác định được biến thể nào đang live |

**CHƯA VERIFY được** (nằm ngoài khả năng của phiên làm việc này — không có quyền truy cập DB/SQL
Server thật, không có mã nguồn phía gọi engine trên POS terminal):
- Biến thể `_NEW`/`_BK`/`_Duplicate` nào trong `offer_procedure.sql` đang thực sự chạy production.
- Bảng `dbo.OfferPriority` có dữ liệu thật hay rỗng (ảnh hưởng tới việc "ưu tiên theo loại" có thật
  sự hoạt động hay không, độc lập với vấn đề #3).
- Hành vi thực tế trên máy POS thật khi test 1 CTKM cấu hình Mon-Fri only / giờ hành chính only.

---

*Báo cáo tạo ngày 2026-07-16. Nguồn: `PromotionSetupPage.razor` (1410 dòng), `PromotionSetupDto.cs`,
`PromotionRepository.cs`, `docs/sql/SetupPromotion_Save.sql`, `docs/web/offers/Setup_Promotion_Insert.sql`,
`docs/web/offers/offer_procedure.sql` (6790 dòng, UTF-16, 28 stored procedure).*

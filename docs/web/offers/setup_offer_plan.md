# Kế hoạch V3: Hoàn thiện logic SETUP CTKM (PromotionSetupPage) — theo publish SP thật

> V3 — dựng lại sau khi đọc **`docs/web/offers/Setup_Promotion_Insert.sql`** (SP publish nháp→Offer*,
> nguồn sự thật của bước Duyệt) và đối chiếu 30 calc-proc trong
> **`docs/web/offers/offer_procedure.sql`**. Thay cho V1/V2. Đây mới là bức tranh chính xác — V1/V2
> đoán mò cơ chế Benefits, nay đã xác minh bằng SP thật.

## Phát hiện QUYẾT ĐỊNH từ `Setup_Promotion_Insert` (bước Duyệt)

Cách publish map `SetupPromotion*` → `Offer*` (đã đọc trực tiếp SP):

1. **Định tuyến dòng Get do cột `SetupPromotionHEADER.TOTALMINVALUE` (0/1) quyết định — KHÔNG do
   OfferType:**
   - `TOTALMINVALUE = 0` → dòng Get ghi vào **`OfferGet`** (loại theo dòng: ZB02/05/07/08/10…).
   - `TOTALMINVALUE = 1` → dòng Get ghi vào **`OfferBenefits`**, với **`StepAmount = H.MINVALUE`**
     (ngưỡng tổng bill), `ValueType/Value` từ DISTYPE/BBYPER/BBYVAL của dòng Get.
   - `OfferHeader.IsTotalBill = ISNULL(H.TOTALMINVALUE,0)`.
2. **`OfferBuy` bị loại trừ ZB06 & ZB13** (`WHERE H.BBYTYPE NOT IN ('ZB06','ZB13')`) và đọc
   **`SetupPromotionBUY.DiscountType/DiscountValue`** khi ghi.
3. `LimitQty = IIF(LIMIT rỗng/null, 9999, LIMIT)` → **LimitQty là tùy chọn** (tự default 9999).
4. Voucher ZB13/14/15/16: nếu VoucherFromDate/ToDate/ValidDay đều default/rỗng → publish **tự set
   `OfferHeader.Status=2` (vô hiệu)**. ⇒ CTKM voucher **bắt buộc** có ngày voucher hợp lệ.
5. `MemberOnly = IIF(VINID='X',1,0)`; `PriorityBBY = ZPRIOR`; `ConditionBuy/Get` = BUYLINKCAT/
   GETLINKCAT ('A'→2/AND, else 1/OR); `LineGroup` (G1/B1/O1…) do publish tự sinh; `Step = Quantity`.
6. `TotalDiscountType` header map `'%'→0,'P'→2, else 1`; `DealPrice=DiscountAmountMax`;
   `ShowDealLines=IsFullPrice`; `NumOfDays` (int đơn, KHÔNG dùng NUMOFDAYSLIST).

### ⇒ LỖI GỐC lớn nhất (V1/V2 chưa thấy):
**Luồng lưu hiện tại KHÔNG hề set `TOTALMINVALUE`.** `usp_SaveSetupCTKMAll` / `SaveSetupAsync`
không có tham số nào ghi cột này → luôn = 0 → **dòng Get của MỌI loại đều rơi vào `OfferGet`,
`OfferBenefits` KHÔNG BAO GIỜ được sinh** cho ZB06/ZB12/ZB13/ZB14/ZB15. Đây là lý do các CTKM
tổng bill không chạy đúng. **Đây là sửa lỗi số 1.**

### ⇒ Nhầm lẫn cơ chế trên UI hiện tại:
Checkbox **"Giảm giá tổng bill" (`CheckTotalDiscount`)** hiện **xóa sạch dòng Get** — nhưng theo
publish SP, loại tổng bill (ZB06/ZB12…) **cần giữ dòng Get** làm sản phẩm hưởng KM (→ OfferBenefits).
`CheckTotalDiscount` thực chất là cơ chế **KHÁC** (giảm % trên toàn hóa đơn, → OfferHeader.
TotalDiscountType/Value, kiểu ZB21/ZB09) — KHÔNG được lẫn với "tổng bill → tặng/giảm 1 list SP".

## Quyết định đã chốt (từ 2 vòng hỏi + review user)
- Sửa cả luồng lưu (Stage 1: DTO+TVP+`usp_SaveSetupCTKMAll`+đọc lại). **KHÔNG sửa
  `Setup_Promotion_Insert`** (SP publish đã đúng — chỉ cần Stage-1 cấp đúng `TOTALMINVALUE`/Buy
  discount cho nó).
- Cờ động `dbo.OfferType` + nhánh ngoại lệ nghiệp vụ (KHÔNG bắt sửa cờ DB).
- Kiểm chứng = build xanh + ContractTests xanh + checklist thủ công.

## Sửa lỗi so với V2 (dựa trên SP thật)
1. **THÊM cột `TOTALMINVALUE`** (sửa lỗi gốc): save `TOTALMINVALUE=1` khi OfferType `IsTotalBill`,
   `MINVALUE` = ngưỡng người dùng nhập → publish sinh đúng OfferBenefits (StepAmount=MINVALUE).
2. **Tách bạch 2 cơ chế tổng bill:** (a) loại `IsTotalBill` → giữ dòng Get làm SP hưởng KM,
   KHÔNG xóa; (b) "Giảm giá tổng bill" whole-bill (CheckTotalDiscount) là tùy chọn riêng cho
   loại giảm toàn hóa đơn, mới xóa Get. Không hiện CheckTotalDiscount cho loại `IsTotalBill`.
3. **LimitQty KHÔNG bắt buộc** (publish default 9999) — bỏ ràng buộc "LimitQty>0 cho tổng bill"
   của V2. Chỉ bind + cho nhập.
4. **OfferMaxQuantity: KHÔNG triển khai** — `Grep` toàn bộ `offer_procedure.sql` (30 calc proc)
   cho **0 tham chiếu** `OfferMaxQuantity`; publish SP cũng không ghi bảng này. Trường "Limit by
   customer" trong kịch bản test thực chất là `LimitQty` (LIMIT) — đã có UI. → Giữ LimitQty.

## Ma trận năng lực theo loại (code thực thi theo CỜ + ngoại lệ publish)

| Cờ / Quy tắc | UI + validate | Cột lưu chính |
|---|---|---|
| `IsSetupBuy=1` và OfferType ∉ {ZB05,ZB10,ZB06,ZB13} | Hiện+cho nhập tab "Sản phẩm mua"; ≥1 dòng | SetupPromotionBUY (+DiscountType/Value) |
| OfferType ∈ {ZB05,ZB10,ZB06,ZB13} | Buy KHÔNG bắt buộc (publish loại ZB06/ZB13 khỏi OfferBuy; ZB05/ZB10 nghiệp vụ không có Buy) | — |
| `IsSetupGet=1` | Hiện+cho nhập tab "Sản phẩm khuyến mãi"; ≥1 dòng có DiscountType/Value | SetupPromotionGET |
| `IsTotalBill=1` | Set **TOTALMINVALUE=1**; hiện ô MinValue (ngưỡng) ở **tab Thông tin chung**, validate MinValue>0; dòng Get = SP hưởng KM (→OfferBenefits); ẨN checkbox "Giảm giá tổng bill" | TOTALMINVALUE=1, MINVALUE |
| `IsTotalBill=0` + user bật "Giảm giá tổng bill" | Cơ chế whole-bill riêng: xóa Get, nhập TotalDiscountType/Value | TOTALDISCOUNT*, TOTALMINVALUE=0 |
| `IsVoucher=1` | Khóa+auto-check Voucher; **bắt buộc** Voucher từ/đến ngày hợp lệ (nếu không publish tự vô hiệu) | ZVCDATE_ST/EN, LIMITNR |
| `IsGift=1` | Chú thích "SP KM là quà (POS popup)"; dữ liệu qua dòng Get | — |

## Triển khai (theo file)

### A. `PromotionSetupPage.razor`
1. Computed: `CurrentOfferTypeIsSetupBuy/Get/Gift` (+ `IsTotalBill` sẵn có); hằng
   `BuyOptionalOfferTypes = {"ZB05","ZB10","ZB06","ZB13"}`; `BuyRequired`.
2. Gate tab Buy theo `CurrentOfferTypeIsSetupBuy`; tab Get theo `CurrentOfferTypeIsSetupGet`;
   chưa chọn loại → hint.
3. **MinValue → tab "Thông tin chung"** (vùng "Điều kiện áp dụng chung"), chỉ hiện khi
   `CurrentOfferTypeIsTotalBill`, label "Giá trị tổng bill tối thiểu để hưởng KM (ngưỡng)".
4. **Ẩn checkbox "Giảm giá tổng bill" (CheckTotalDiscount) khi `CurrentOfferTypeIsTotalBill`**
   (tránh xóa nhầm dòng Get là benefits). Chỉ hiện cho loại không phải total-bill.
5. Thêm cột Buy `DiscountType`+`DiscountValue` (tooltip: ZB02=giá combo/DiscountType=Giá cố định;
   ZB07=ngưỡng giá trị bill; loại khác để %).
6. Sửa nhãn ScaleType tab Get: C = "Bằng (Equal)"; tooltip A/B/C khớp `dien_giai.md`.
7. Tab "Cài đặt nâng cao": thêm ô **Độ ưu tiên (1–10)** (`PriorityBBY`); làm rõ ô **LimitQty** =
   "Số lần tính lặp KM / Limit by customer (0 = không giới hạn)". (KHÔNG thêm OfferMaxQuantity.)
8. Chú thích IsGift ở tab Get.
9. Validate UI theo cờ + ngoại lệ; section ẩn → gửi list rỗng. Voucher type → bắt buộc voucher dates.
10. Tuân `.claude/rules/blazor-web-app.md` (MudTable/pos-btn-mockup/Density).

### B. DTO — `PromotionSetupDto.cs`
- `OfferBuyLineDto`: thêm `int DiscountType` (0) + `decimal DiscountValue`.

### C. Repository — `PromotionRepository.cs`
- `BuildBuyTable`: thêm cột `DiscountType`(int)+`DiscountValue`(string).
- `GetSetupDetailAsync`: đọc lại Buy `DiscountType/DiscountValue`; đọc lại `TOTALMINVALUE` (để mở
  lại đúng trạng thái).
- `SaveSetupAsync`: truyền thêm **`@TotalMinValue`** = (IsTotalBill?1:0) tra qua cờ OfferType (cache);
  validate server theo cờ: Buy (`IsSetupBuy && ∉ BuyOptional`), Get (`IsSetupGet`), IsTotalBill→
  MinValue>0, IsVoucher→voucher dates. Dùng chung hằng `BuyOptionalOfferTypes`.

### D. SQL — `docs/sql/SetupPromotion_Save.sql` (sửa trực tiếp, Track A DROP+CREATE)
- `SetupPromotionBuyTVP`: thêm `DiscountType int NULL`, `DiscountValue nvarchar(50) NULL`.
- `usp_SaveSetupCTKMAll`: thêm tham số **`@TotalMinValue bit=0`** → ghi cột `TOTALMINVALUE`; nhánh
  INSERT `SetupPromotionBUY` ghi thêm `DiscountType/DiscountValue` từ `@Buy`. Giữ XACT_ABORT/THROW.
- Single File Constraint; cập nhật `docs/sql/manifest.json` (nếu có entry) + `docs/ROLLOUT.md` §D1.

### E. Docs
- `docs/CURRENT_STRUCTURE.md`: field mới `OfferBuyLineDto`.
- `docs/web/testing/promotion-setup.md`: cơ chế TOTALMINVALUE, tách CheckTotalDiscount vs total-bill,
  test case theo loại.
- `docs/ROLLOUT.md` + `docs/sql/manifest.json`.

## Rủi ro / Giới hạn (nêu rõ)
1. **Không sửa `Setup_Promotion_Insert`** (đã đúng). Chỉ cấp đúng đầu vào TOTALMINVALUE/Buy discount.
   Vẫn cần verify thực tế bằng cách Duyệt trên DEV rồi soi `Offer*`.
2. **Nghi vấn mapping `TOTALDISCOUNTTYPE`:** publish đọc cột này so `'%'/'P'` nhưng chưa xác minh
   `usp_SaveSetupCTKMAll` lưu dạng `'%'/'P'` hay int `'0'/'1'/'2'` (chưa đọc được thân SP save đầy
   đủ). → Kiểm khi sửa SP; nếu lưu int thì whole-bill % discount đang sai (map về Amount). Sửa cùng đợt.
3. Cờ `dbo.OfferType` là nguồn sự thật runtime; nhánh ngoại lệ {ZB05,ZB10,ZB06,ZB13} bù cho cờ lệch,
   KHÔNG cần user sửa DB.
4. **OfferMaxQuantity: KHÔNG build (đã chốt với user).** 0 tham chiếu trong 30 calc proc + không
   được publish → không có tác dụng runtime. Thay bằng LimitQty (đã có, calc-proc dùng thật). Nếu
   sau này phát sinh consumer, làm editor OfferMaxQuantity ở đợt riêng.

## Verification
1. `dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly` → 0 error.
2. `dotnet test tests/POS.ContractTests -nologo` → xanh (DTO Setup không bị khóa contract).
3. **Checklist thủ công (DEV, sau khi chạy SQL §0):**
   - ZB05/ZB10: Buy không bắt buộc; Get bắt buộc; TOTALMINVALUE=0 → OfferGet đúng.
   - ZB07/ZB08: Buy+Get; Buy DiscountValue=ngưỡng bill (ZB07) → OfferBuy đúng.
   - ZB02: Buy DiscountType=Giá cố định+giá combo; Get quà → OfferBuy.DiscountValue đúng.
   - **ZB06: Buy ẩn; MinValue ở tab Thông tin chung; sau Duyệt phải có dòng trong OfferBenefits với
     StepAmount = MinValue** (đây là bằng chứng lỗi gốc đã fix).
   - ZB12: Buy+Get+MinValue → OfferBuy + OfferBenefits.
   - ZB13/14/15: Voucher tự bật+khóa, nhập voucher dates hợp lệ (bỏ trống → publish tự Status=2);
     OfferBenefits sinh đúng.
   - Nâng cao: ZPRIOR=3 → `SetupPromotionHEADER.ZPRIOR='3'` + OfferHeader.PriorityBBY=3.

## File sẽ sửa
- `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`
- `src/POS.Common/Dtos/Promotion/PromotionSetupDto.cs`
- `src/POS.Infrastructure/Repositories/Promotion/PromotionRepository.cs`
- `docs/sql/SetupPromotion_Save.sql` (+ `docs/sql/manifest.json`, `docs/ROLLOUT.md`)
- `docs/CURRENT_STRUCTURE.md`, `docs/web/testing/promotion-setup.md`

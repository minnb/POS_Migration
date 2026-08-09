# Kế hoạch V4: Hoàn thiện logic SETUP CTKM (PromotionSetupPage)

> V4 — sửa 4 lỗi nghiệp vụ của V3 sau review. Nền tảng: đã đọc trực tiếp
> `docs/web/offers/Setup_Promotion_Insert.sql` (publish nháp→Offer*) và trace calc-proc thật trong
> `docs/web/offers/offer_procedure.sql` (đặc biệt `BLUEPOS_PRO_Cal_ZB006_ByOfferNo` dòng 100–174).

## 0. Chuỗi sống còn UI → Setup → Offer → Calc (bắt buộc khớp từng mắt xích)

```
[UI PromotionSetupPage]  →  [SetupPromotion* (nháp)]  →(Duyệt: Setup_Promotion_Insert)→  [Offer* (LIVE)]  →  [BLUEPOS_PRO_Cal_* (POS engine)]
   Quantity (Get/Buy)         QTY / MAT_QUAN                 OfferGet/Buy.Quantity, .Step               @StepQty = Quantity  (SL KM mỗi lần lặp)
   MinValue (ngưỡng)          MINVALUE                       OfferBenefits.StepAmount                   @StepAmount          (điều kiện bill)
   LimitQty (Limit by cust.)  LIMIT                          OfferHeader.LimitQty                       @LimitQty            (số lần lặp; 0→99)
   IsTotalBill flag           TOTALMINVALUE (0/1)            OfferHeader.IsTotalBill + rẽ Get/Benefits   —
   Buy DiscountType/Value     SetupPromotionBUY.Disc*        OfferBuy.DiscountType/Value                 @StepQty / điều kiện bill (ZB07)
```

Hệ quả trọng yếu (từ calc ZB06): **tổng số lượng KM bị chặn = `Quantity` (mỗi dòng) × `LimitQty`
(số lần lặp)**. Vòng lặp `WHILE @ApplyNumber <= @LimitQty AND @RemainAmount >= @StepAmount`; mỗi
lần hưởng tối đa `@StepQty = Quantity`. ⇒ **Quantity và LimitQty là 2 mắt xích chặn số lượng KM —
KHÔNG được để trống/hiểu sai/gán 0.**

## 1. Bốn phát hiện gốc từ publish SP (giữ từ V3, vẫn đúng)
1. **Rẽ nhánh Get do `SetupPromotionHEADER.TOTALMINVALUE` (0/1):** =0 → `OfferGet`; =1 →
   `OfferBenefits` với `StepAmount = H.MINVALUE`. `OfferHeader.IsTotalBill = TOTALMINVALUE`.
2. **`OfferBuy` loại trừ đúng 2 loại: `ZB06` và `ZB13`** (`WHERE H.BBYTYPE NOT IN ('ZB06','ZB13')`);
   đọc `SetupPromotionBUY.DiscountType/DiscountValue`.
3. Voucher ZB13/14/15/16 thiếu ngày voucher hợp lệ → publish tự `Status=2` (vô hiệu).
4. Publish tự sinh `Step = Quantity`, `LineGroup` (G1/B1/O1…), `ConditionBuy/Get` từ BUYLINKCAT/
   GETLINKCAT.

### LỖI GỐC SỐ 1 (giữ): luồng lưu hiện tại KHÔNG set `TOTALMINVALUE` → `OfferBenefits` không bao
giờ sinh cho ZB06/ZB12/ZB13/ZB14/ZB15.

---

## 2. BỐN SỬA LỖI V4 (theo chỉ đạo review)

### Sửa #1 — KHÔNG bỏ qua giới hạn số lượng KM (Quantity × LimitQty + OfferMaxQuantity)

**Đính chính của tôi (bằng chứng):** grep V3 chỉ khớp đúng chuỗi `OfferMaxQuantity` (0 hit) nên tôi
kết luận sai. Grep rộng họ max-quantity cho **8 hit**. Trace calc ZB06 cho thấy cơ chế chặn SL thật
gồm 3 lớp:
- **(a) `Quantity` (dòng Get/Benefit) × `LimitQty` (header)** — lớp chặn CHÍNH mà engine đang dùng
  (`@StepQty × @LimitQty`). ĐÃ có field nhưng V3 xem nhẹ LimitQty ("optional, default 9999") — SAI.
- **(b) `OfferRetrict`** (LEFT JOIN trong ZB06/ZB12, `Status=1` → loại item khỏi KM) — lớp loại
  trừ item, hiện nạp từ nguồn ngoài, `Setup_Promotion_Insert` KHÔNG ghi.
- **(c) `OfferMaxQuantity`** — bảng có cột `MaxQuantity` (numeric cap theo store+item+UOM); chuỗi
  literal này 0 hit trong calc-proc hiện tại.

**Kế hoạch #1 (bắt buộc làm):**
1. **Nâng `LimitQty` + `Quantity` thành field hạng nhất, validate chặt** (thoả yêu cầu test chéo
   ZB06):
   - `Quantity` mỗi dòng Buy/Get **bắt buộc ≥ 1** (chặn Step=0 — xem Sửa #4). New row default = 1.
   - `LimitQty` (đổi nhãn "Giới hạn KH / Limit by customer") — bind rõ, helper "0 = không giới hạn
     (engine coi = 99 lần lặp)". KHÔNG mô tả là "tùy chọn bỏ qua".
2. **Thêm trường "Giới hạn số lượng KM tối đa / Max Quantity"** vào UI (tab "Cài đặt nâng cao"),
   bind `PromotionSetupHeaderDto.MaxQuantity` (int). Truyền xuống luồng lưu (tham số
   `@MaxQuantity`), lưu tạm vào `SetupPromotionHEADER` (cột phụ) để mở lại đúng.
3. **Publish sang bảng restriction:** viết **script Track B `docs/sql/SetupPromotion_Insert_AddMaxQty.sql`
   (ALTER PROC `Setup_Promotion_Insert`)** bổ sung đoạn `DELETE + INSERT` vào bảng
   **`OfferMaxQuantity`** (schema đúng: `OfferNo/StoreNo/ItemNo/UOM/MaxQuantity/Status`) cho các
   dòng benefit/get của CTKM, `MaxQuantity = @MaxQuantity`.
   > ⚠️ **CỔNG XÁC NHẬN TRƯỚC KHI CHẠY (không tự ý sửa SP production):** hiện **chưa calc-proc nào
   > đọc `OfferMaxQuantity`** (0 hit) — dữ liệu ghi vào sẽ **chưa có tác dụng runtime** cho tới khi
   > engine POS được cập nhật đọc bảng này (nằm ngoài repo POS.Web). Vì vậy bước ALTER SP production
   > + việc bổ sung đọc ở calc-proc phải được **DBA/chủ engine POS xác nhận** target bảng
   > (`OfferMaxQuantity` vs `OfferRetrict`) trước khi apply. Lớp chặn (a) Quantity×LimitQty vẫn hoạt
   > động ngay không phụ thuộc bước này.

### Sửa #2 — Gán rõ cờ `TOTALMINVALUE` từ `IsTotalBill` (Backend)
Khi map DTO → tham số save, **`@TotalMinValue` = (OfferType đang chọn có `IsTotalBill=1` ? 1 : 0)**.
Cụ thể trong `PromotionRepository.SaveSetupAsync`: tra `GetOfferTypeOptionsAsync` (cache) theo
`h.OfferType` → lấy cờ `IsTotalBill` → `p.Add("@TotalMinValue", isTotalBill ? 1 : 0)`. `usp_SaveSetupCTKMAll`
ghi giá trị này vào cột `SetupPromotionHEADER.TOTALMINVALUE`. `MINVALUE` = ngưỡng user nhập.
⇒ publish rẽ đúng Get→OfferBenefits, `StepAmount=MINVALUE`.

### Sửa #3 — Tách bạch ZB13 vs (ZB14, ZB15) ở tab Buy
`Setup_Promotion_Insert` chỉ loại `ZB06` và `ZB13` khỏi `OfferBuy`. ⇒ **ZB14, ZB15 VẪN CÓ điều kiện
mua** (đúng nghiệp vụ: nhóm SP mua + tổng bill → voucher).
- Hằng ẩn/không-bắt-buộc Buy = **`BuyHiddenOfferTypes = { "ZB06", "ZB13" }`** (ẩn tab Buy).
- Hằng Buy-không-bắt-buộc-nhưng-vẫn-hiện (nghiệp vụ không có Buy nhưng cờ DB có thể =1) =
  **`BuyOptionalOfferTypes = { "ZB05", "ZB10" }`**.
- **ZB14, ZB15: KHÔNG thuộc 2 tập trên** → tab Buy hiện & cho nhập bình thường theo cờ `IsSetupBuy`
  của DB; nếu `IsSetupBuy=1` thì validate ≥1 dòng Buy như loại thường.
- (Voucher tự bật/khoá áp cho ZB13/14/15/16 là vấn đề **khác** — không liên quan tab Buy; ghi tách
  ở checklist, không gộp hành vi Buy của 3 loại.)

### Sửa #4 — Bảo đảm `Step = Quantity`, chống Step = 0
Publish tự set `Step = Quantity` (Buy: `MAT_QUAN`; Get: `QTY`). Không có ô "Step" riêng trên UI.
Rủi ro: `Quantity = 0` → `Step = 0` → phép chia/interval trong calc (`@StepQty`, `@IntervalLoop`)
lỗi/chia 0.
- **DTO:** `OfferBuyLineDto`/`OfferGetLineDto` new row default `Quantity = 1`.
- **Validate (UI + Repository):** mọi dòng Buy/Get đã nhập SP/nhóm phải có `Quantity ≥ 1`; chặn Lưu
  nếu có dòng `Quantity ≤ 0`, message rõ ("Số lượng phải ≥ 1").
- Ghi chú rõ trong doc: Step do publish suy ra = Quantity (không lưu Step riêng ở setup tables).

---

## 3. Ma trận năng lực theo loại (V4)

| Loại / Cờ | Tab Buy | Tab Get | Tổng bill | Voucher | Ghi chú |
|---|---|---|---|---|---|
| ZB05, ZB10 | ẩn/không bắt buộc (BuyOptional) | ✔ bắt buộc | — | — | Get + ScaleType A/B/C |
| ZB02 | ✔ (combo: DiscountType=Giá cố định + giá) | ✔ (quà) | — | — | IsGift |
| ZB07, ZB08 | ✔ (ZB07: DiscountValue=ngưỡng bill) | ✔ | — | — | Buy+Get |
| ZB06 | **ẩn** (BuyHidden) | ✔ (→Benefits) | ✔ TOTALMINVALUE=1, MinValue | — | Get=SP hưởng KM |
| ZB12 | ✔ | ✔ (→Benefits) | ✔ | — | Buy + Benefits |
| ZB13 | **ẩn** (BuyHidden) | ✔ (→Benefits) | ✔ | ✔ | Không có Buy |
| ZB14, ZB15 | ✔ (theo cờ IsSetupBuy) | ✔ (→Benefits) | ✔ | ✔ | **VẪN có Buy** |

- `IsTotalBill=1` → set `TOTALMINVALUE=1`, hiện MinValue ở tab "Thông tin chung", giữ dòng Get làm
  benefits, ẩn checkbox "Giảm giá tổng bill" (whole-bill, cơ chế riêng ZB21/ZB09).
- `IsGift=1` → chú thích quà tặng; dữ liệu qua dòng Get.

## 4. Triển khai theo file

### A. `PromotionSetupPage.razor`
- Computed `CurrentOfferTypeIsSetupBuy/Get/Gift/IsTotalBill`; hằng `BuyHiddenOfferTypes={ZB06,ZB13}`,
  `BuyOptionalOfferTypes={ZB05,ZB10}`.
- Gate tab Buy: ẩn nếu ∈ BuyHidden; hiện theo `IsSetupBuy` còn lại (ZB14/ZB15 hiện bình thường).
- MinValue → tab "Thông tin chung" (chỉ khi IsTotalBill); ẩn CheckTotalDiscount khi IsTotalBill.
- Cột Buy DiscountType/DiscountValue (tooltip ZB02 combo / ZB07 ngưỡng bill).
- Sửa nhãn ScaleType Get: C = "Bằng (Equal)" + tooltip A/B/C.
- Tab "Cài đặt nâng cao": **Độ ưu tiên (1–10)** `PriorityBBY`; **LimitQty** nhãn "Giới hạn KH /
  Limit by customer"; **Max Quantity** `MaxQuantity` (Sửa #1).
- New Buy/Get row default `Quantity=1`; validate mọi dòng `Quantity≥1` (Sửa #4).
- Validate theo cờ; Voucher type bắt buộc voucher dates.

### B. DTO `PromotionSetupDto.cs`
- `OfferBuyLineDto`/`OfferGetLineDto`: `Quantity` default 1 (new row); `OfferBuyLineDto` thêm
  `int DiscountType`(0) + `decimal DiscountValue`.
- `PromotionSetupHeaderDto`: thêm `int MaxQuantity`.

### C. `PromotionRepository.cs`
- `BuildBuyTable`: thêm cột `DiscountType`(int)+`DiscountValue`(string).
- `SaveSetupAsync`: tra cờ OfferType → `@TotalMinValue`=(IsTotalBill?1:0) (Sửa #2); truyền
  `@MaxQuantity`; validate: Buy (IsSetupBuy && ∉ BuyHidden && ∉ BuyOptional), Get (IsSetupGet),
  IsTotalBill→MinValue>0, mọi dòng Quantity≥1, IsVoucher→voucher dates.
- `GetSetupDetailAsync`: đọc lại Buy DiscountType/Value, TOTALMINVALUE, MaxQuantity.

### D. SQL
- `docs/sql/SetupPromotion_Save.sql` (Track A): `SetupPromotionBuyTVP` +DiscountType/DiscountValue;
  `usp_SaveSetupCTKMAll` +`@TotalMinValue` (ghi TOTALMINVALUE) +`@MaxQuantity` (ghi cột header) +
  Buy Disc*.
- **`docs/sql/SetupPromotion_Insert_AddMaxQty.sql` (Track B, MỚI):** ALTER `Setup_Promotion_Insert`
  thêm DELETE+INSERT `OfferMaxQuantity` từ dòng Get/Benefit, `MaxQuantity=@... `. **Gắn cổng xác
  nhận DBA/engine (Sửa #1) trước khi apply.**
- Single File Constraint; cập nhật `docs/sql/manifest.json` + `docs/ROLLOUT.md`.

### E. Docs
- `docs/CURRENT_STRUCTURE.md`: field mới DTO.
- `docs/web/testing/promotion-setup.md`: cơ chế TOTALMINVALUE, tách ZB13 vs ZB14/15, Quantity×LimitQty,
  Max Quantity, Step=Quantity.

## 5. Rủi ro / cần xác nhận
1. **ALTER `Setup_Promotion_Insert` (production SP)** để ghi `OfferMaxQuantity`: cần DBA/chủ engine
   POS xác nhận (a) đúng bảng target (`OfferMaxQuantity` vs `OfferRetrict`), (b) sẽ cập nhật calc-proc
   đọc bảng đó — vì hiện 0 calc-proc đọc `OfferMaxQuantity`. Lớp chặn Quantity×LimitQty hoạt động ngay,
   không phụ thuộc bước này.
2. **Nghi vấn mapping `TOTALDISCOUNTTYPE`** (publish so `'%'/'P'`): kiểm khi sửa `usp_SaveSetupCTKMAll`;
   nếu đang lưu int `'0'/'1'/'2'` thì whole-bill % discount sai → sửa cùng đợt.
3. Cờ `dbo.OfferType` là nguồn sự thật runtime; các tập ngoại lệ bù cờ lệch, KHÔNG cần sửa DB.

## 6. Verification
1. `dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly` → 0 error.
2. `dotnet test tests/POS.ContractTests -nologo` → xanh.
3. **Checklist thủ công (DEV, sau khi chạy SQL):**
   - ZB05/ZB10: Buy ẩn/không bắt buộc; Get bắt buộc, Quantity≥1; TOTALMINVALUE=0 → OfferGet.
   - ZB07: Buy+Get; Buy DiscountValue=ngưỡng bill → OfferBuy đúng.
   - ZB02: Buy DiscountType=Giá cố định+giá combo → OfferBuy.DiscountValue.
   - **ZB06: Buy ẩn; MinValue (tab Thông tin chung); Duyệt → OfferBenefits có StepAmount=MinValue;
     kiểm chặn SL: Quantity×LimitQty (vd Quantity=5,LimitQty=1 và Quantity=1,LimitQty=5 đều cap 5).**
   - **ZB13: Buy ẩn. ZB14/ZB15: Buy HIỆN & nhập được** (khác ZB13) → OfferBuy có dòng.
   - ZB12: Buy+Get+MinValue → OfferBuy+OfferBenefits.
   - ZB13/14/15: voucher dates hợp lệ (bỏ trống → publish Status=2).
   - MaxQuantity nhập N → SetupPromotionHEADER lưu; sau ALTER SP (nếu đã xác nhận) → OfferMaxQuantity.
   - Nâng cao: ZPRIOR=3, LimitQty=5 → SetupPromotionHEADER + OfferHeader đúng.

## 7. File sẽ sửa
- `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`
- `src/POS.Common/Dtos/Promotion/PromotionSetupDto.cs`
- `src/POS.Infrastructure/Repositories/Promotion/PromotionRepository.cs`
- `docs/sql/SetupPromotion_Save.sql` + **`docs/sql/SetupPromotion_Insert_AddMaxQty.sql` (mới)**
  (+ `docs/sql/manifest.json`, `docs/ROLLOUT.md`)
- `docs/CURRENT_STRUCTURE.md`, `docs/web/testing/promotion-setup.md`

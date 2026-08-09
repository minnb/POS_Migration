# COORDINATION — Bàn giao ca

> Nguồn sự thật DUY NHẤT cho "đang làm gì / còn thiếu gì / bước tiếp theo là gì" khi bắt đầu hoặc
> kết thúc một phiên làm việc. **BẮT BUỘC cập nhật file này trước khi kết thúc một task** (xem
> `CLAUDE.md` mục "Bàn giao ca").
>
> Khác `docs/migrations/STATUS.md` (chỉ theo dõi tiến độ port từng feature từ `src/legacy/`) —
> file này bao quát **toàn bộ** công việc đang dở trong repo, không riêng migration.

## Trạng thái hiện tại

Chiến dịch dọn dẹp hệ thống điều phối AI (`CLAUDE.md` + `.claude/rules/` + `.claude/skills/`) đã
**hoàn thành phần chính** (Bước 1-2-4 của kế hoạch 4 bước). Bước 3 (đồng bộ tên `SKILL.md` vs
`SKILLS.md` trong `.claude/skills/`) **đang chờ quyết định của user** — chưa làm.

## Việc đang làm dở / Backlog

- [x] **BUG THẬT 🔴 CONFIRMED (phát hiện 2026-07-16) — ✅ ĐÃ FIX 2026-07-16**: SP
      `dbo.usp_SetupPromotion_Approve` (`docs/sql/SetupPromotion_ApproveAndStatus.sql`) lỗi
      SQL Server 266 "Transaction count after EXECUTE..." mỗi khi Duyệt CTKM — nguyên nhân:
      `BEGIN TRANSACTION` tường minh bọc ngoài `EXEC [dbo].[Setup_Promotion_Insert]` (SP legacy tự
      quản transaction riêng), gây lệch `@@TRANCOUNT`. Chẩn đoán DB
      (`docs/sql/_diag/Diag_SetupPromotion_Insert_ZB06.sql`) xác nhận SP legacy EXEC trực tiếp
      (không bọc tran) chạy KHÔNG lỗi → 266 là toàn bộ vấn đề, không có lỗi nghiệp vụ ZB06.
      **Fix:** bỏ `BEGIN/COMMIT TRANSACTION` bọc ngoài, để SP legacy tự quản; đảo thứ tự publish
      trước → `IsApprove=1` sau; đọc MAX(Counter) gán OUTPUT giữ nguyên (chữ ký/C# không đổi).
      **Verify:** deploy lại SP + `smoke_promotion_setup.py` ZB06 → CTKM-13/14 PASS,
      `SUMMARY: ALL PASSED`, không sinh 266 mới trong log; `dotnet test tests/POS.ContractTests`
      49/49 xanh. Chi tiết: `docs/CHANGELOG.md` [2026-07-16].
- [x] **BUG THẬT 🔴 CONFIRMED — ✅ ĐÃ FIX 2026-07-16**: Duyệt CTKM ZB06 báo "thành công" nhưng
      publish 0 dòng sang Offer*. Nguyên nhân: `SetupPromotionHEADER.LIMIT='5.000'` (decimal
      `LimitQty` ToString giữ scale) → SP legacy `Setup_Promotion_Insert` CONVERT '5.000'→int
      (OfferHeader.LimitQty) ném Msg 245, SP **nuốt lỗi** (CATCH chỉ ROLLBACK) → rollback toàn bộ.
      **Fix:** (1) `PromotionRepository.SaveSetupAsync` ghi `@LimitQty` dạng "F0" (số nguyên);
      (2) `usp_SetupPromotion_Approve` THROW 51003 nếu 0 dòng OfferHeader sau publish (chống
      success giả). **Verify DB thật (BBYNR 6000000048):** OfferHeader=1, OfferBenefits=1,
      OfferGet=0, OfferSite=1, LIMIT='5'. Chi tiết: `docs/CHANGELOG.md` [2026-07-16].
      *Lưu ý còn lại:* nhóm cửa hàng '2018' (mặc định test `POSWEB_TEST_SITE_GROUP`) KHÔNG tồn tại
      trong môi trường dev hiện tại — test tự fallback nhóm đầu tiên ('ALL'); tạo nhóm 2018 trong
      `dbo.SetupGroupSite` nếu muốn dùng đúng mặc định.
- [ ] **CHƯA VERIFY E2E (chờ POS.Web chạy)**: smoke test `smoke_promotion_setup.py` nay dùng mã
      sản phẩm THẬT từ `tests/POS.Web.UiTests/test_products.json` (đã điền 6 mã: 10007150/10007152/
      10222426/10201314/10018456, uom HOP/TUY/G1) + bắt buộc chọn nhóm cửa hàng. Config nạp OK
      ("nạp 6 mã sản phẩm thật") nhưng lần chạy cuối POS.Web đã tắt → cần chạy lại
      `POSWEB_TEST_OFFER_TYPES=ZB06` khi app UP để xác nhận dòng sản phẩm nhận đúng ItemNo/Uom và
      `OfferBenefits.No`/`UnitOfMeasure` là mã thật. Nhóm cửa hàng '2018' không có trong dev →
      fallback 'ALL'.
- [ ] **CHỜ DBA — 🔴 CONFIRMED GAP (rà soát 2026-08-09)**: Voucher delay
      (`AllowUseAfterDay`/`AllowUseAfterTime`) **lưu nháp thành công** nhưng **bị rớt khi publish**
      vì bảng `OfferHeader` trên Production đang thiếu cột `ZVCDAY_AFTER`/`ZVCTIME_AFTER`. **Chờ
      DBA alter bảng và update SP publish.**
      *Bằng chứng:* 2 cột CÓ trên `SetupPromotionHEADER`
      (`docs/architecture/centralMD-schema.md:1292-1293`) và được ghi đúng qua
      `@AllowUseAfterDay`/`@AllowUseAfterTime` (`PromotionRepository.cs:356-357` →
      `docs/sql/SetupPromotion_Save.sql:197-198`); nhưng `OfferHeader` (60 cột,
      `centralMD-schema.md:997-1064`) KHÔNG có 2 cột này và
      `docs/web/offers/Setup_Promotion_Insert.sql` không hề tham chiếu chúng (grep 0 hit) → dữ liệu
      dừng ở bảng nháp. Engine tính KM cũng chưa đọc 2 cột (0 hit trong `offer_procedure.sql`) nên
      **chưa có tác dụng runtime** kể cả sau khi publish được.
      *Đã làm trong đợt này (không đụng DB/SP):* (1) validate format `hh:mm:ss` cho
      `AllowUseAfterTime` ở cả UI (`PromotionSetupPage.SaveAsync`) lẫn Repository
      (`SaveSetupAsync`) — trước đó không validate ở bất kỳ tầng nào, chuỗi rác vẫn ghi được vào
      `ZVCTIME_AFTER`; (2) dọn `AllowUseAfterDay`/`AllowUseAfterTime` về mặc định khi bỏ tick
      Voucher (bất đối xứng cũ: 2 field ngày voucher được dọn, 2 field delay thì không → tick
      Voucher, nhập giờ sai, bỏ tick, Lưu ⇒ validate bị bỏ qua nhưng chuỗi rác vẫn ghi xuống DB);
      (3) chuẩn hoá input `"020000"` → `"02:00:00"` khi Enter (`FormatTimeDigitsWithSeconds`, cùng
      tinh thần `FormatTimeDigits` của Từ giờ/Đến giờ) + `Trim()` trước khi ghi tham số SP.
- [x] **BUG THẬT 🔴 CONFIRMED — ✅ ĐÃ FIX 2026-08-09**: dòng Buy/Get **trắng** (bulk-add 10 dòng
      nhưng chỉ nhập 3) lọt xuống DB với `MAT_QUAN='0'`. Validate `Quantity ≥ 1` bị gate bởi
      `HasLineItem(r)` (`PromotionRepository.cs:306-308`) nên không chặn được, còn
      `BuildBuyTable`/`BuildGetTable` ghi **mọi** dòng vào TVP. Nhánh `MGP` của
      `Setup_Promotion_Insert.sql:126-135` KHÔNG lọc `MATGROUP <> ''` (chỉ nhánh `MAT` lọc
      `MAT_NR <> ''` ở `:150`) → publish ra `OfferBuy` có `No=''`, `Quantity=0`, `Step=0` → vỡ phép
      chia không guard trong `docs/web/offers/offer_procedure.sql` (L941, L1001, L2338, L2940,
      L10855, L10860 — `@StepQty` bind từ `Quantity`).
      **Fix:** (1) `BuildBuyTable`/`BuildGetTable` thêm `.Where(HasLineItem)`; (2) UI `SaveAsync`
      lọc dòng trống trước khi dựng request + Snackbar báo số dòng bỏ qua; (3) `MudNumericField`
      Số lượng đổi `Min="0"` → `Min="1"` ở cả tab Buy và Get.
      **Verify:** `dotnet build POS.slnx` 0 error; `dotnet test tests/POS.ContractTests` 49/49 xanh.
      **CHƯA VERIFY E2E** (sandbox không có SQL Server/Redis) — cần chạy checklist thủ công
      `docs/web/testing/promotion-setup.md §10.5` trên môi trường có DB.
- [ ] Quyết định có đồng bộ tên file skill về 1 chuẩn duy nhất (`SKILL.md` hay `SKILLS.md`) hay
      không. Hiện trạng: 6 thư mục dùng `SKILL.md` (`appservice-scaffold`, `codebase-map`,
      `contract-test-guardian`, `git-workflow`, `mudblazor-compliance`, `payment-test-generator`),
      6 thư mục dùng `SKILLS.md` (`api`, `cache`, `database`, `migration`, `web`, `worker`).
- [ ] **CHƯA VERIFY**: xác nhận với user xem các file đang "dở" thấy qua `git status` lúc
      2026-07-15 (`docs/sql/manifest.json`, `tools/POS.DbMigrator/{ManifestScriptProvider,
      Program}.cs`, `tests/POS.ContractTests/DbMigratorScriptOrderTests.cs`,
      `.claude/skills/database/SKILLS.md`, appsettings UAT/Production, thư mục `.github/`) có phải
      WIP hợp lệ từ task `[2026-07-14] Điều tra MasterDataZipGeneratorWorker...` hay không, trước
      khi bất kỳ ai commit — tránh gộp nhầm việc dở của task khác.
- [ ] **CHƯA VERIFY E2E (Promotion Setup V4):** Sandbox hiện tại thiếu SQL Server/Redis. Cần QA test thủ công theo `docs/web/testing/promotion-setup.md §10.5`. Đặc biệt chú ý 3 hành vi UI mới:
  1. Dọn field khi bỏ tick Voucher.
  2. Chuẩn hoá format "020000" → "02:00:00" khi Enter.
  3. Lệnh Trim() khi map xuống ZVCTIME_AFTER.
- [ ] **GATED (Chờ DBA):** Bảng `OfferHeader` trên Production đang thiếu cột `ZVCDAY_AFTER`/`ZVCTIME_AFTER`. Tạm thời voucher delay vẫn rớt ở bước publish. Đã chốt: CỐ Ý KHÔNG SỬA SP/Table ở phase này, chờ DBA alter table.

## Next steps

- Nếu user chốt đồng bộ tên skill → thực hiện đổi tên + cập nhật mọi link trỏ tới (kể cả trong
  `CLAUDE.md`), rồi coi chiến dịch dọn dẹp là xong.
- Nếu user chốt không cần → đóng chiến dịch, không cần thao tác gì thêm.

## Lịch sử bàn giao gần nhất

| Ngày | Việc đã làm | Ghi chú |
|---|---|---|
| 2026-08-09 | **Audit git trước khi commit chọn lọc** — phát hiện commit `fd9823e` ("D") đã gộp 169 file / +42376/-1744 dòng (gồm cả 3 file Promotion đáng lẽ commit riêng, lẫn hàng loạt file `.claude/commands/`, `.claude/skills/{mcp-builder,pdf,skill-creator}/`, test UiTests không liên quan) — dấu hiệu lỡ chạy `git add -A`/`git add .` thay vì add từng file. Đã xác nhận working tree sạch, không còn gì để commit riêng. **Quyết định (đã hỏi user): giữ nguyên lịch sử, KHÔNG rewrite** (branch `minhnb` chưa có upstream nên rủi ro thấp nhưng vẫn chọn an toàn). | **Nhắc phiên sau**: tuyệt đối không `git add -A`/`git add .`/`git commit -a` — luôn add đích danh từng file đã sửa trong phiên, kiểm `git status`/`git diff --staged --stat` trước khi commit để tránh lặp lại sự cố gộp bừa như `fd9823e`. |
| 2026-07-16 | **Viết script Playwright test trang Cài đặt CTKM** (`tests/POS.Web.UiTests/smoke_promotion_setup.py` + `docs/web/testing/testing_promotion_setup_guide.md`), cùng khuôn `smoke_coupon_issue.py`. Phủ CTKM-01/02/04/10/11/13/14/18/19/22/23 (mapping `docs/web/testing/promotion-setup.md`) — chọn Loại CTKM/Hình thức bán **đầu tiên có sẵn trong dropdown** (không hardcode mã ZB), thích ứng tab Buy/Get theo cờ `OfferType` thật. **Bổ sung theo yêu cầu user (2 vòng)**: (1) sweep tự động qua TOÀN BỘ Loại CTKM có trong dropdown — tự điền MinValue (tổng bill)/Voucher từ-đến ngày (voucher bị khoá true) khi cần; (2) biến môi trường `POSWEB_TEST_OFFER_TYPES` để CHỈ test 1/vài Loại cụ thể (vd `ZB06`) thay vì chờ sweep hết — item đầu test SÂU (Lưu tạm+round-trip+Duyệt), item sau chỉ sweep NÔNG, item không tồn tại → dừng sớm báo lỗi rõ (không âm thầm test Loại khác). Trong lúc thêm filter, phát hiện & sửa 1 gap thật: luồng chính (CTKM-02) trước đó chỉ hoạt động đúng với Loại mặc định ZB02 — thiếu logic tự điền MinValue/Voucher date nên filter sang ZB06 fail sai lý do; đã tách logic đó (vốn có trong sweep) thành helper `fill_type_specific_requirements()` dùng chung. Đã chạy thật nhiều lần (cả có/không filter) trên dev (cổng 5170) tới khi mọi selector đúng. | **Không filter: 26/29 PASS + 1 SKIP + 3 FAIL (cùng 1 bug `usp_SetupPromotion_Approve`, xem backlog trên). Filter `ZB06`: chỉ 8 bước/1 bản ghi (so với 21 bước/13 bản ghi khi không filter), cùng 3 FAIL đó, KHÔNG có lỗi script** — output/screenshot đầy đủ trong guide mục 2.1 + mục 3 + `tests/POS.Web.UiTests/artifacts/promotion_setup_*.png`. |
| 2026-07-16 | **Audit chéo UI vs Procedure trang Cài đặt CTKM** (đọc-only, không đổi code) — đối chiếu `PromotionSetupPage.razor` với cả pipeline `usp_SaveSetupCTKMAll` → `Setup_Promotion_Insert` → `offer_procedure.sql` (28 calc-proc engine). Kết quả: `docs/web/offers/ui_procedure_logic_audit.md`. | **2 gap 🔴 CONFIRMED bằng Grep độc lập, có thể thành task fix riêng**: (1) lịch Mon-Sun không được calc-proc nào đọc (áp dụng mọi loại CTKM); (2) 2 field voucher delay `AllowUseAfterDay`/`AllowUseAfterTime` bị rớt ở `Setup_Promotion_Insert` (không có trong danh sách 41 cột INSERT `OfferHeader`). **CHƯA VERIFY**: biến thể `_NEW`/`_BK`/`_Duplicate` nào của mỗi calc-proc đang chạy production — cần hỏi DBA trước khi sửa. Xem chi tiết đầy đủ + mức độ tin cậy từng phát hiện trong file audit. |
| 2026-07-15 | **Hoàn thiện logic SETUP CTKM `PromotionSetupPage.razor` theo publish SP thật** (kế hoạch `docs/web/offers/setup_offer_plan_V4.md`). Sửa lỗi gốc: luồng lưu không set `TOTALMINVALUE` → `OfferBenefits` không sinh cho CTKM tổng bill (ZB06/ZB12/ZB13/ZB14/ZB15). Gate tab Buy theo cờ (ẩn ZB06/ZB13, ZB14/ZB15 giữ Buy), MinValue→tab Thông tin chung, thêm cột Buy DiscountType/Value (ZB02 combo/ZB07 ngưỡng bill), PriorityBBY/MaxQuantity/LimitByCustomer, validate Quantity≥1 (chống Step=0), fix TOTALDISCOUNTTYPE lưu '%'/'R'/'P'. SQL: `SetupPromotion_AddMaxQuantity.sql` (order 95) + `SetupPromotion_Save.sql` bản 5 + gated `SetupPromotion_Insert_AddMaxQty.sql` (Track B, order 115). Build 0 error, ContractTests 49/49 pass. | **CHƯA VERIFY end-to-end** (sandbox thiếu SQL/Redis) — cần chạy SQL §D1 rồi test thủ công theo checklist `promotion-setup.md §10.5`. **GATED**: script publish `OfferMaxQuantity` chờ DBA/engine xác nhận (0 calc-proc đọc bảng này — độc lập xác nhận lại trong audit 2026-07-16). Nghi vấn cũ `TOTALDISCOUNTTYPE` int→symbol đã fix cùng đợt. |
| 2026-07-15 | Gỡ conflict-marker Git trong `CLAUDE.md` (888→114 dòng), di dời phần `dbo.Store` sang `database-standards.md`, tạo `COORDINATION.md`, đổi tên lệnh `/resume`→`/task-resume` | Chi tiết đầy đủ ở `docs/CHANGELOG.md` entry cùng ngày |

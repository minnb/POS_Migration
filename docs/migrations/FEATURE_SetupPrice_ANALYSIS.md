# FEATURE — Setup Giá (Price Setup) — Phân tích Migration

> Nguồn cũ: `src/legacy/VCM.BLUEPOS` (SetupPrice) — .NET Framework 4.6, MVC5 + EF6.
> Đối chiếu bản mới: `src/POS.Web/.../Catalog/Price/PriceSetupPage.razor` + `POS.Application/Features/Price`.
> Ngày lập: 2026-07-03.

---

## ⚠️ 0. Kết luận nhanh — CHỨC NĂNG NÀY ĐÃ ĐƯỢC PORT

Trước khi đi vào chi tiết: **9.3 Setup Giá đã được migrate xong** sang dự án mới. Đây KHÔNG phải
sân cỏ trống. Các artefact đã tồn tại:

| Layer | File đã có (bản mới) |
|---|---|
| UI (9.3 Setup) | `src/POS.Web/Components/Pages/Catalog/Price/PriceSetupPage.razor` (route `/catalog/price-setup`) |
| UI (9.1 Danh sách giá) | `src/POS.Web/Components/Pages/Catalog/Price/PricesPage.razor` + `Dialogs/PriceItemPickerDialog.razor` |
| Application | `POS.Application/Features/Price/{IPriceService,PriceService}.cs` |
| Infrastructure | `POS.Infrastructure/Repositories/Price/{IPriceRepository,PriceRepository}.cs` |
| DTOs | `POS.Common/Dtos/Price/{PriceSetupDto,PriceListDto}.cs` |
| SQL (mới) | `docs/sql/SetupSalePrice_Save.sql` (2 TVP + `usp_SetupSalePrice_Save`) |

**Do đó tài liệu này phục vụ 2 mục đích:**
1. Ghi lại đầy đủ nghiệp vụ gốc + DB/SP (theo yêu cầu khảo sát).
2. **Đối chiếu bản đã port ↔ bản gốc → chỉ ra các điểm LỆCH / CHƯA PORT cần quyết định** (Mục 7).

Nếu mục tiêu chỉ là "port lại từ đầu" thì phần lớn công việc đã xong; việc cần làm thực chất là
**vá các gap ở Mục 7**. Vui lòng xác nhận hướng đi trước khi mình sửa code.

---

## 1. Luồng nghiệp vụ (Business Logic)

### 1.1 Bài toán
Màn hình cho IT/Ops **khai báo giá bán (SalesPrice)** cho từng sản phẩm theo:
- **Hình thức bán hàng** (SalesType — mã `dbo.SalesOrderType`),
- **Nhóm giá** (PriceGroupCode) → trở thành **SalesCode**,
- Khoảng hiệu lực **Từ ngày → Đến ngày**, **Đơn vị tính (UOM)** và **Giá bán (UnitPrice)**.

Hai cách nhập: **khai báo tay** (thêm N dòng trống) hoặc **import Excel** (validate rồi nạp vào lưới).
Kèm màn hình phụ **9.1 Danh sách giá** (tra cứu + sửa giá + xóa mềm).

### 1.2 Luồng UI → Controller → BLO → DAL (legacy)

```
SetupPrice.cshtml (view khai báo)
  ├─ [load form] SetupPrice()              → BLO.ViewSetupPrice()   → Data.ViewSetupPrice()
  │       • dropdown Hình thức bán hàng = SalesOrderType (IsActive=1)   [SetupPriceData.cs:58-64]
  │       • dropdown Nhóm giá           = PriceGroup (Enabled=1)        [SetupPriceData.cs:66-71]
  ├─ [khai báo tay] _AppendListItem(...)   → render partial _AppendListItem.cshtml (N dòng trống)
  ├─ [chọn SP inline] SearchItem(kw,page)  → BLO.SearchItem → SP [SetupPrice_SearchItem_Paging]
  ├─ [nhập barcode]  GetInfoBarcode(bc)    → SearchItem(...,"barcode") (cùng SP)
  ├─ [tải file mẫu]  ExcelExamplePrice()   → view ExcelExamplePrice (EPPlus xuất .xlsx)
  ├─ [import excel]  LoadDataExcelToTable  → BLO.GetListInfoItem → Data.ValidateImport (bulk #temp + join)
  └─ [lưu]           SaveItemPrice(json)   → (validate 10 tầng, build Pkey) → BLO.SaveSalesPrice
                                              → Data.SaveSalesPrice
                                                  • Pkey mới  → EF AddRange INSERT
                                                  • Pkey cũ   → SP [Setup_SalePrice_Get_ALL] @Json
                                                                  └→ loop SP [Setup_SalePrice_Get] (cắt khoảng)

modalListPrice (9.1 Danh sách giá)
  ├─ _ListPrice() → partial ; _ListPriceLoad() → BLO.LoadSalesPrice → SP [SP_LOAD_SALESPRICE]
  ├─ UpdatePrice(Id, UnitPrice)  → Data.UpdatePrice (EF update + bump Counter cùng Pkey)
  └─ DeletePrice(Id)             → Data.DeletePrice (EF soft-delete IsActive=0 + bump Counter)
```

### 1.3 API bên thứ 3
**Không có.** Toàn bộ chạy trong DB `CentralMDPartner` (= `RPOSMasterData`). Không gọi external HTTP.

---

## 2. Business Rules (đánh số + trích dẫn nguồn)

> Tất cả nằm ở `SetupPriceController.SaveItemPrice` trừ khi ghi khác. Bản mới port ở
> `PriceService.SaveAsync` — parity ở cột cuối.

| # | Rule | Nguồn gốc | Đã port? |
|---|---|---|---|
| R1 | Mọi dòng phải có **ItemNo** (không rỗng) | `SetupPriceController.cs:260-268` | ✅ `PriceService.cs:64-66` |
| R2 | Mọi dòng phải có **UOM** | `SetupPriceController.cs:270-279` | ✅ `PriceService.cs:68-70` |
| R3 | **Từ ngày / Đến ngày** đúng định dạng `dd/MM/yyyy` (length ≥ 10 + tháng ≤ 12) | `SetupPriceController.cs:282-323` | ✅ gộp vào `TryParseDmy` `PriceService.cs:76-87,135-136` |
| R4 | Không **trùng** `(ItemNo, UOM)` trong cùng lần lưu | `SetupPriceController.cs:343-352` | ✅ `PriceService.cs:89-94` |
| R5 | **UnitPrice > 0** | `SetupPriceController.cs:354-363` | ✅ `PriceService.cs:96-98` |
| R6 | **StartDate ≤ EndDate** | `SetupPriceController.cs:365-374` | ✅ `PriceService.cs:100-102` |
| R7 | **StartDate ≥ hôm nay** | `SetupPriceController.cs:376-385` | ✅ `PriceService.cs:104-107` |
| R8 | **EndDate ≥ hôm nay** | `SetupPriceController.cs:387-396` | ✅ `PriceService.cs:109-111` |
| R9 | **Pkey = `{SalesType}-{ItemNo}-{UOM}-{SalesCode}`** | `SetupPriceController.cs:425-426` | ✅ `PriceService.cs:123` |
| R10 | Defaults khi INSERT: `CurrencyCode='VND'`, `PriceIncludesVAT=1`, `AllowInvoiceDisc=1`, `AllowLineDisc=1`, `MinimumQuantity=1`, `VariantCode=''` | `SetupPriceController.cs:431-442` | ✅ chuyển vào SP `SetupSalePrice_Save.sql:104-107` |
| R11 | Chỉ xét Pkey **còn hiệu lực** `YEAR(EndingDate) <> 7777` | `SetupPriceData.cs:268` | ✅ `SetupSalePrice_Save.sql:86,111` |
| R12 | Pkey **chưa tồn tại** → INSERT, `Counter = MAX(Counter)+1` (rỗng → 1) | `SetupPriceData.cs:269-345` | ✅ `SetupSalePrice_Save.sql:96-111` |
| R13 | Pkey **đã tồn tại** → gọi SP `Setup_SalePrice_Get_ALL @Json` (cắt khoảng + đóng bản cũ) | `SetupPriceData.cs:358-371` | ✅ ủy quyền SP cũ, `SetupSalePrice_Save.sql:117-118` |
| R14 | Import: `StartingDate/EndingDate/UnitPrice` không rỗng, `UnitPrice ≥ 0`, có `ItemNo` **hoặc** `Barcode`, nếu có ItemNo thì phải có UOM | `SetupPriceController.cs:586-653` | ⚠️ **một phần** — xem Gap G4 |
| R15 | Import: mỗi dòng join `Item`/`ItemUnitOfMeasure`/`Barcodes` → sinh `ErrorMessage` (item/uom/barcode không tồn tại) | `SetupPriceData.cs:490-586` | ✅ `PriceRepository.cs:71-101` |
| R16 | 9.1 Update: `UnitPrice > 0`, phải khác giá cũ, bump `Counter` mọi dòng cùng Pkey | `SetupPriceController.cs:816-819` + `SetupPriceData.cs:686-729` | ❌ **CHƯA port** — Gap G2 |
| R17 | 9.1 Delete: soft-delete `IsActive=0` + bump `Counter` các dòng cùng Pkey còn lại | `SetupPriceData.cs:645-684` | ❌ **CHƯA port** — Gap G2 |

---

## 3. Database & Data Access

### 3.1 Stored Procedure (đối tượng cần có trên `RPOSMasterData`)

| SP | Vai trò | Trạng thái trên bản mới |
|---|---|---|
| `[dbo].[Setup_SalePrice_Get_ALL]` | Nhận JSON list `{Pkey,FromDate,ToDate,UnitPrice}`, loop từng Pkey gọi `Setup_SalePrice_Get` | **TÁI DÙNG SP CŨ** (bản mới `EXEC` lại) — `Script_Stored_Procedures.sql:9135-9216` |
| `[dbo].[Setup_SalePrice_Get]` | Lõi update: cắt khoảng ngày (split trái/phải/xóa khoảng lồng), đóng bản cũ (`IsActive=0`/Counter), INSERT bản mới; log lỗi vào `Interface_Errors` | **TÁI DÙNG SP CŨ** — `Script_Stored_Procedures.sql:8787-9128` |
| `[dbo].[SetupPrice_SearchItem_Paging]` | Tìm item theo mã/tên/barcode, paging (`@Keyword,@Page,@PageSize,@SearchBy`) | Bản mới dùng `IPromotionRepository.SearchItemsAsync` (cần xác nhận SP tương đương) — `Script...:9942` |
| `[dbo].[SP_LOAD_SALESPRICE]` | 9.1 list SalesPrice + filter + paging (`@ItemCode,@ItemName,@BarCode,@SalesCode,@HieuLuc,@PageSize,@PageNumber`) | Bản mới **KHÔNG** dùng SP này; dùng `[dbo].[GetSalesPriceList]` / `[GetSalesPriceList_Export]` (`PriceRepository.cs:30,53`) — Gap G3 |
| `[dbo].[GetSalesPriceList]` / `[GetSalesPriceList_Export]` | 9.1 list/export (được `PriceData` cũ dùng) | **TÁI DÙNG SP CŨ** (positional EXEC) |
| `dbo.usp_SetupSalePrice_Save` **(MỚI)** | INSERT Pkey mới (TVP) + ủy quyền `Setup_SalePrice_Get_ALL` cho Pkey cũ; trả `(Ok,Message)` | **ĐÃ TẠO** — `docs/sql/SetupSalePrice_Save.sql:65-127` (chạy tay 1 lần) |
| `dbo.SetupSalePriceImportTVP` / `dbo.SetupSalePriceLineTVP` **(MỚI)** | TVP cho validate import / lưu | **ĐÃ TẠO** — `docs/sql/SetupSalePrice_Save.sql:33-58` |

> ✅ Không có SP nào **thiếu** cho phần đã port. Danh sách SP legacy cần đảm bảo tồn tại sẵn trên
> `RPOSMasterData`: `Setup_SalePrice_Get_ALL`, `Setup_SalePrice_Get`, `GetSalesPriceList`,
> `GetSalesPriceList_Export`, `SetupPrice_SearchItem_Paging`. Nếu port thêm 9.1 Delete/Update
> (Gap G2) → **cân nhắc gói vào SP mới `usp_SalesPrice_UpdatePrice` / `usp_SalesPrice_SoftDelete`**
> thay vì bê nguyên logic EF (đúng chuẩn dự án mới, tránh raw EF-style multi-save).

### 3.2 Table / View bị tác động

| Đối tượng | Vai trò | Thao tác |
|---|---|---|
| `dbo.SalesPrice` | Bảng giá chính (Pkey, Counter, Starting/EndingDate, UnitPrice, IsActive*) | INSERT / UPDATE (đóng bản cũ) / soft-delete |
| `dbo.SalesOrderType` | Nguồn dropdown Hình thức bán hàng (`IsActive=1`) | READ |
| `dbo.PriceGroup` | **Nguồn dropdown Nhóm giá gốc** (`Enabled=1`) | READ — ⚠️ Gap G1 |
| `dbo.StorePriceGroup` | Map store↔nhóm giá (bản mới đang lấy Nhóm giá từ đây) | READ |
| `dbo.Item` / `dbo.ItemUnitOfMeasure` / `dbo.Barcodes` | Tra item/UOM/barcode khi validate import & item picker | READ |
| `dbo.Store` | `ListStoreByChannel` (channel→store) — **view legacy đã ẩn, thực tế không dùng** | READ |
| `dbo.Interface_Errors` | SP `Setup_SalePrice_Get` ghi lỗi khi cắt khoảng thất bại | INSERT (trong SP) |
| view `ExcelExamplePrice` | Data cho file Excel mẫu (bản mới tự sinh bằng ClosedXML, không cần view) | READ (legacy) |

> ⚠️ **Schema `SalesPrice`**: DDL hiện hành chỉ **15 cột**, **KHÔNG có `IsActive`/`LastTimeUpdate`/`Id`**
> như EF model .NET 4.6. `SetupSalePrice_Save.sql:20-24` đã note điều này; "đánh dấu xóa" dựa trên
> `EndingDate` (`YEAR=7777`) + `Counter`, KHÔNG dùng `IsActive`. **`SalesType` là cột `int`** → mã
> hình thức bán hàng phải là số hợp lệ (convert ngầm khi INSERT). ➜ Ảnh hưởng trực tiếp Gap G2
> (Delete/Update legacy set `IsActive`/`LastTimeUpdate` — không còn cột này ở schema mới).

---

## 4. Cấu trúc source code legacy

| Thành phần | File / symbol |
|---|---|
| Controller | `VCM.BLUEPOS/Controllers/SetupPriceController.cs` |
| Actions | `SetupPrice`, `GetComboSiteByChannel`, `SearchItem`, `_AppendListItem`, `GetInfoBarcode`, `SaveItemPrice`, `ExcelExamplePrice`, `LoadDataExcelToTable`, `_ListPrice`, `_ListPriceLoad`, `DeletePrice`, `UpdatePrice` |
| BLO | `VCM.BLUEPOS.Business/SetupPrice/SetupPriceBLO.cs` (thin pass-through → Data) |
| DAL | `VCM.BLUEPOS.Data/SetupPrice/SetupPriceData.cs` (EF6 `CentralMDPartnerContainer` + raw SP) |
| EF entity | `VCM.BLUEPOS.Data/EF/Central/SalesPrice.cs` (+ `SalesPriceRange`, `SalesPriceRate`) |
| ViewModels | `VCM.BLUEPOS.Model/SetupPrice/` → `ViewSetupPriceModel`, `ViewSetupItemPriceModel`, `SalesPriceModel`, `SaveItemPriceRequest`(+`SaveItemPriceModel`,`ParamCallStoreProcedureRequest`), `ListPriceModel`, `ExcelExamplePriceModel`(+`ImportResultSalePriceModel`,`LoadExcelExamplePriceModel`,`ItemResultCombo`,`PagingResult<T>`), `SetupComboboxModel`, `StorePriceGroupModel`(+`PriceGroupModel`) |
| Views | `Views/SetupPrice/{SetupPrice,_AppendListItem,_ListPrice,LoadDataExcelToTable}.cshtml` |

---

## 5. Ánh xạ DTO/Model cũ → mới

| Legacy model | Bản mới | Ghi chú |
|---|---|---|
| `SaveItemPriceRequest` (client → save) | `PriceSaveRow` (`PriceSetupDto.cs:49`) | bỏ Channel/Region/Store (không dùng) |
| `SalesPriceModel` (build để lưu) | `PriceSaveLine` (`PriceSetupDto.cs:63`) | defaults chuyển vào SP |
| `ListPriceModel` (9.1) | `PriceListItemDto` (`PriceListDto.cs:9`) | |
| `LoadExcelExamplePriceModel` | `PriceImportRow` (`PriceSetupDto.cs:7`) | |
| `ImportResultSalePriceModel` | `PriceImportResultRow` (`PriceSetupDto.cs:21`) | |
| `ItemResultCombo` | `ItemOptionDto` (Promotion) | dùng chung item picker |
| `PriceGroupModel`/combo | `PriceOptionDto` + `PriceSetupLookupDto` | ⚠️ nguồn khác (Gap G1) |
| `Tuple<bool,string>` | `PriceSaveResult` (`PriceSetupDto.cs:76`) | |

---

## 6. Đề án thiết kế Blazor — **đã hiện thực** (đối chiếu chuẩn dự án)

- **UI 9.3**: `PriceSetupPage.razor` — `@rendermode InteractiveServer`, policy `OpsAndAbove`,
  `div.pos-page-header`, `MudTable HorizontalScrollbar`, MudFileUpload + ClosedXML đọc Excel client-side,
  `IAuditLogger.LogAsync(CREATE, SalesPrice)` sau khi lưu. **Đạt** chuẩn Web (Mục 10/14/15/16 CLAUDE.md).
- **State**: `List<PriceEditRow>` (private class trong page), bind trực tiếp trên lưới; không EditForm
  (grid nhiều dòng động). Hợp lý.
- **Service/Repo**: đúng layering (Controller/Page → `IPriceService` → `IPriceRepository` → SP/TVP).
  Validate nghiệp vụ ở `PriceService` (Application), I/O ở `PriceRepository` (Infrastructure). **Đạt.**
- **Cache**: `MD:PriceGroupOptions` TTL 12h (`PriceRepository.cs:19,127-140`). **Đạt** chuẩn cache.

---

## 7. ⚠️ CÁC ĐIỂM LỆCH / CHƯA PORT — cần quyết định

> **CẬP NHẬT sau khi verify schema thực tế** (`docs/sql/database/CentralMD.sql` +
> `docs/architecture/centralMD-schema.md`). Hai kết luận thay đổi so với bản nháp đầu:
> **G1 rút lại (KHÔNG phải lỗi)**, **G2 bị CHẶN bởi schema — cần DBA xác nhận**.

- **G1 — RÚT LẠI. Không phải defect.** Bảng danh mục `dbo.PriceGroup` của legacy **KHÔNG được
  migrate** sang `RPOSMasterData` — grep `CentralMD.sql` chỉ có `dbo.StorePriceGroup`, không có
  `dbo.PriceGroup`. Hơn nữa cột `StorePriceGroup.PriceGroupName` được **cố ý bổ sung 2026-07**
  (`centralMD-schema.md:129`) đúng để làm nguồn dropdown này. ➜ Bản mới lấy Nhóm giá từ
  `DISTINCT StorePriceGroup` [`PriceRepository.cs:131-136`] là **thiết kế đúng cho schema hiện có**.
  **KHÔNG sửa.** (Nhận định "lệch bảng" ở bản nháp là sai do chưa đối chiếu schema mới.)

- **G2 — 9.1 Sửa/Xóa giá CHƯA port, và bị CHẶN bởi mâu thuẫn schema/SP.** `PricesPage.razor` hiện
  chỉ list + export. Legacy có inline edit + soft-delete [R16/R17]. Nhưng **không thể port nguyên**
  vì các sự thật đã verify mâu thuẫn nhau:
  1. **Không có khoá dòng đơn.** `dbo.SalesPrice` **không có cột `Id`** (PK là composite
     `ItemNo,SalesCode,StartingDate,UnitOfMeasureCode` — `centralMD-schema.md:819`). Legacy Update/Delete
     key theo `Id` int — **không tồn tại** ở schema mới. SP list `GetSalesPriceList` **không trả cột
     `ID`** (`Script...:4684-4699`) → `PriceListItemDto.ID` hiện luôn = 0 (không dùng được để định vị).
  2. **Không có cột `IsActive`.** Cả DDL lẫn `centralMD-schema.md` đều xác nhận `SalesPrice` chỉ **15
     cột, không `IsActive`/`LastTimeUpdate`**. Legacy soft-delete = `IsActive=0` → **không áp dụng được**.
  3. **SP list legacy tham chiếu `IsActive=1`** (`Script...:4711`) — mâu thuẫn với bảng 15 cột. Nghĩa là
     SP `GetSalesPriceList` **đang deploy trên RPOSMasterData chắc chắn là bản đã sửa khác** với script
     trong `src/legacy/` (mình không có text SP thật đang chạy).
  4. **Ý nghĩa `EndingDate` năm 7777 mâu thuẫn giữa 2 tài liệu**: `centralMD-schema.md:837` ghi "7777 =
     giá hiệu lực vô thời hạn"; còn `SetupSalePrice_Save.sql:11` + legacy DeletePrice (dòng comment
     `SetupPriceData.cs:657` `EndingDate=7777-07-07`) coi **7777 = đánh dấu xóa**. Trái ngược nhau.

  ➜ **KHÔNG tự chọn cơ chế** (vi phạm "không suy đoán tên cột / không suy diễn logic"). Cần DBA xác nhận:
  (a) cơ chế đánh dấu 1 dòng SalesPrice là **đã xóa** trong schema mới để (i) biến mất khỏi
  `GetSalesPriceList` và (ii) POS replication gỡ đúng; (b) filter "còn hiệu lực" thật của SP list đang
  deploy. **Update giá** thì an toàn: in-place `UPDATE UnitPrice + Counter=MAX+1` theo composite PK
  (mọi cột PK đều có trong list) — bám đúng legacy `UpdatePrice` [`SetupPriceData.cs:686-729`], không
  đụng `IsActive`.

- **G3 — 9.1 dùng SP khác legacy.** Bản mới đọc list qua `GetSalesPriceList[_Export]` thay cho
  `SP_LOAD_SALESPRICE` [`SetupPriceData.cs:608-626`]. Nếu cột/định dạng trả về của 2 SP khác nhau
  (vd `EndingYearStr`, format ngày) → cần đối chiếu `PriceListItemDto` khớp đúng SP đang gọi.
  Chỉ là "cần verify", không chắc là lỗi.

- **G4 — Validate import: một số check ở tầng Controller cũ chưa tái hiện đủ.** Legacy
  `LoadDataExcelToTable` chặn sớm khi thiếu `StartingDate/EndingDate/UnitPrice`, `UnitPrice<0`,
  thiếu `ItemNo`/`Barcode`, hoặc có ItemNo mà thiếu UOM [`SetupPriceController.cs:586-653`]. Bản mới
  đọc Excel client-side rồi chỉ gọi `ValidateImportAsync` (join DB), các check "ô trống" đưa về
  hiển thị `ErrorMessage`/dòng lỗi trên lưới. Hành vi tương đương về mặt kết quả nhưng **thông báo
  chặn có thể khác** (legacy chặn toàn file, bản mới đánh dấu từng dòng). Xác nhận UX chấp nhận được.

- **G5 — "Loại khai báo" (Item vs Barcode).** Legacy để **ẩn** ô này (`display:none`,
  `SetupPrice.cshtml:55`) → thực tế chạy chế độ Item. Bản mới **mở lại cả 2 chế độ** (ITEM_UOM +
  BARCODE, có nhánh `ResolveBarcodeAsync`). Đây là **mở rộng có chủ đích** — không phải lỗi, nhưng
  ghi nhận để QA test nhánh barcode kỹ.

- **G6 — Option "ALL" cho Hình thức bán hàng.** Legacy có `<option value="ALL">ALL</option>` đứng
  đầu [`SetupPrice.cshtml:71`]; bản mới mặc định "Tại chỗ", không có "ALL". Nếu nghiệp vụ cần set giá
  áp cho MỌI hình thức bán → cần thêm lại option "ALL". Cần xác nhận.

---

## 8. Điểm KHÔNG suy diễn (giữ nguyên hành vi gốc)

- Sentinel **EndingDate = `9999-12-31`** (bản mới `NewBlankRow`) được SP quy về `9999-01-01`
  [`Script...:9178`] — giữ nguyên, không đổi.
- Logic **cắt khoảng ngày** khi Pkey trùng nằm hoàn toàn trong SP `Setup_SalePrice_Get`
  [`Script...:8934-9034`: split trái/phải/xóa khoảng lồng/insert mới] — **không** viết lại ở C#,
  ủy quyền SP cũ. Đúng nguyên tắc "không suy diễn".
- `Counter` = con trỏ replication cho POS pull; mọi thay đổi Pkey đều bump `MAX(Counter)+1` — giữ nguyên.

---

## 9. Trạng thái thực hiện

- **G1** — ĐÓNG (không sửa). `dbo.PriceGroup` không có trên RPOSMasterData; nguồn `StorePriceGroup` đúng.
- **G2** — ✅ **ĐÃ PORT** (quyết định: soft-delete = sentinel năm 7777 + bump Counter):
  - SP mới `docs/sql/SalesPrice_EditDelete.sql` → `usp_SalesPrice_UpdatePrice`, `usp_SalesPrice_SoftDelete`
    (**cần chạy tay 1 lần trên RPOSMasterData**).
  - DTO `PriceRowKey`; `IPriceService/PriceService` + `IPriceRepository/PriceRepository` thêm
    `UpdatePriceAsync` / `DeletePriceAsync`(`SoftDeletePriceAsync`).
  - UI `PricesPage.razor`: cột "Thao tác" (Sửa inline giá + Xóa có confirm + `IAuditLogger`).
  - Định vị dòng bằng **composite PK** (bảng không có `Id`); Sửa = in-place UnitPrice + Counter; Xóa =
    EndingDate `7777-07-07` + Counter.
  - Build 0 error (Web razor + libs) · `dotnet test tests/POS.ContractTests` = 25 passed.
- **Còn lại (verify nghiệp vụ, không chặn code)**: G3, G4, G6.

## 10. Đề xuất bước tiếp theo (chờ duyệt)

1. **Quyết G1** (PriceGroup vs StorePriceGroup) — 1 dòng SQL, ảnh hưởng dữ liệu hiển thị.
2. **Quyết G2** (có port Sửa/Xóa giá ở 9.1 không) — nếu có, đây là phần code mới đáng kể + SP mới.
3. Verify G3/G4/G6 với người nghiệp vụ.
4. Sau khi chốt: cập nhật `docs/CURRENT_STRUCTURE.md` + `dotnet test tests/POS.ContractTests`.

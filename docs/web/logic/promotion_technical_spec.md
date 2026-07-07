# Đặc tả kỹ thuật: Module Chương trình khuyến mãi

> Tài liệu này được sinh tự động bằng cách đọc trực tiếp mã nguồn (2 file `.razor`, service,
> repository, DTO) tại thời điểm 2026-07-06. Phần nội dung **bên trong** một số stored procedure
> (SQL body thật) không nằm trong repo — SP chỉ tồn tại phía DB — nên với các SP đó tài liệu chỉ
> nêu **tên, tham số, và mô tả** lấy từ `docs/architecture/database-schema.md`, không suy đoán logic
> chi tiết bên trong. Các chỗ này được đánh dấu rõ ở mục 2.

## 1. Tổng quan (Overview)

Module gồm 2 trang Blazor Server (`@rendermode InteractiveServer`, `[Authorize(Policy =
WebPolicies.OpsAndAbove)]`):

| Trang | Route | Vai trò |
|---|---|---|
| `OffersPage.razor` | `/promotion/offers` | Danh mục CTKM **đã publish** (bảng `Offer*` chuẩn hoá) — read-only, filter/phân trang server-side, export Excel |
| `PromotionSetupPage.razor` | `/promotion/setup` | Soạn thảo/Duyệt CTKM (bảng `SetupPromotion*` flat) — CRUD đầy đủ + workflow Duyệt |

Cả 2 trang cùng inject **`IPromotionService`** (`POS.Application.Features.Promotion`) — một
**thin wrapper không có business logic**, mọi method chỉ delegate 1 dòng xuống
**`IPromotionRepository`** (`POS.Infrastructure.Repositories.Promotion`), nơi chứa toàn bộ logic
thật: validate, SQL/SP, Redis cache.

### File/Component liên quan trực tiếp

| Layer | File |
|---|---|
| UI | `src/POS.Web/Components/Pages/Promotion/Offers/OffersPage.razor` (không có code-behind `.razor.cs`) |
| UI | `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` (1204 dòng, không có code-behind) |
| UI (dialog con) | `SiteGroupSetupDialog`, `ItemGroupSetupDialog` (cùng namespace `POS.Web.Components.Pages.Promotion.Offers`, mở qua `IDialogService` — không thuộc phạm vi khảo sát chi tiết của tài liệu này) |
| Application | `src/POS.Application/Features/Promotion/IPromotionService.cs`, `PromotionService.cs` |
| Infrastructure | `src/POS.Infrastructure/Repositories/Promotion/IPromotionRepository.cs`, `PromotionRepository.cs` |
| Common (DTO) | `src/POS.Common/Dtos/Promotion/OfferHeaderDto.cs`, `PromotionSetupDto.cs` (không gồm `SpecialComboDto.cs` — thuộc chức năng "Combo đặc biệt" khác, cùng thư mục nhưng ngoài phạm vi module này) |

### Kiến trúc dữ liệu — 2 mô hình song song

- **Bảng "chuẩn hoá" (Offer engine)**: `dbo.OfferHeader`, `OfferBuy`, `OfferGet`, `OfferBenefits`,
  `OfferType`, ... — dữ liệu **đã publish**, được 5.000 máy POS đọc để áp dụng khuyến mãi thực tế.
  `OffersPage.razor` chỉ **đọc** nhóm bảng này (qua SP `GetPromotionOfferHeaderList`).
- **Bảng "flat" (Setup/nháp)**: `dbo.SetupPromotionHEADER/BUY/GET/SITE` — nơi Ops soạn thảo/sửa
  CTKM trước khi publish. `PromotionSetupPage.razor` đọc/ghi nhóm bảng này.
- **Cầu nối**: nút "Duyệt CTKM" (`ApproveSetupAsync` → SP `dbo.usp_SetupPromotion_Approve`) publish
  dữ liệu từ nhóm bảng flat sang nhóm bảng chuẩn hoá. Sau khi duyệt, bản ghi Setup chuyển
  `IsApprove=true` và bị khoá sửa (`_isReadonly=true` trên UI).
- Không có Controller `POS.Api` nào cho Promotion (đã xác nhận `Glob`/`Grep` trên
  `src/POS.Api/Controllers/*Promotion*` = 0 kết quả) — đúng convention dự án: POS.Web inject
  service qua DI trong cùng process, không gọi HTTP.

---

## 2. Cấu trúc Dữ liệu (Database & Stored Procedure)

### Bảng liên quan

| Bảng | Nhóm | Ghi chú |
|---|---|---|
| `dbo.OfferHeader`, `OfferBuy`, `OfferGet`, `OfferBenefits` | Chuẩn hoá (đã publish) | Đọc qua SP `GetPromotionOfferHeaderList`; SQL body SP không có trong repo |
| `dbo.OfferType` | Dùng chung | Danh mục Loại CTKM — cache Redis `MD:OfferTypeOptions` (12h) |
| `dbo.SalesOrderType` | Dùng chung | Danh mục Hình thức bán — cache Redis `MD:SalesOrderTypeOptions` (12h) |
| `dbo.SetupPromotionHEADER` | Flat (nháp) | Header CTKM, khoá nghiệp vụ `BBYNR` tự sinh từ `6000000001` (dùng `UPDLOCK/HOLDLOCK`), cờ `IsApprove` khoá sửa |
| `dbo.SetupPromotionBUY` | Flat (nháp) | Dòng "Sản phẩm mua", join `dbo.Item` qua `MAT_NR` |
| `dbo.SetupPromotionGET` | Flat (nháp) | Dòng "Sản phẩm khuyến mãi", join `dbo.Item` qua `MATERIALCODE` |
| `dbo.SetupPromotionSITE` | Flat (nháp) | Site group áp dụng, join `dbo.SetupGroupSite` qua `SITEGROUPCODE` |
| `dbo.SetupGroupSite` | Dùng chung | Nhóm cửa hàng (`GroupCode`, `GroupName`, `ListStore` — JSON array hoặc literal `"ALL"`) — cache Redis `MD:SiteGroupOptions` (12h) |
| `dbo.SetupGroupItem` | Dùng chung | Nhóm sản phẩm (`GroupCode`, `GroupName`, `ListItemNo` — JSON array) |
| `dbo.Item` | Dùng chung | Tra cứu sản phẩm theo barcode/keyword |
| `dbo.Store` | Dùng chung | Tra cứu CH/ST theo mã nhóm cửa hàng (lọc `ClosingMethod=0`) |
| `dbo.OptionData` | Dùng chung | Danh mục Hạng thẻ thành viên (`Caption='MEMBERCODETYPE'`) — cache Redis `MD:MemberCodeOptions` (12h) |

### Stored Procedure

> ⚠️ Với các SP dưới đây, tài liệu chỉ có **tên + tham số + mô tả nghiệp vụ** (đối chiếu
> `docs/architecture/database-schema.md`). **Không có SQL body thật trong repo** để trích dẫn —
> SP nằm phía DB (`RPOSMasterData`), không phải object trong `POS.slnx`.

| SP | Tham số | Gọi từ | Mô tả |
|---|---|---|---|
| `[dbo].[GetPromotionOfferHeaderList]` | `@No, @Description, @Status, @OfferType, @ItemNo, @StoreNo, @Exp, @PageSize, @PageNumber` | `GetOfferHeaderListAsync`, `ExportOfferHeaderListAsync` | Trả danh sách Offer đã publish (chuẩn hoá) kèm cột `Total` (paging server-side). Export dùng `PageNumber=0, PageSize=100000` |
| `dbo.usp_SaveSetupCTKMAll` | `@BBYNR` (InputOutput, tự sinh nếu rỗng), ~30 tham số scalar header (`@SalesType, @Description, @OfferType, @Status, @ValidFrom, @ValidTo, @IsVoucher, @BuyLinkCat, @GetLinkCat, @LimitQty, @MemberOnly, @MemberCode, @Priority, @NumOfDays, @VoucherFrom, @VoucherTo, @VoucherValidDay, @VoucherLimitNumber, @AllowUseAfterDay, @AllowUseAfterTime, @FromTime, @ToTime, @Mon..@Sun, @MinValue, @CheckTotalDiscount, @TotalDiscountType, @TotalDiscountValue`) + 3 TVP `@Buy, @Get, @Site` | `SaveSetupAsync` | Upsert `SetupPromotionHEADER` + replace toàn bộ `BUY/GET/SITE` trong 1 transaction. Timeout 120s |
| `dbo.usp_SetupPromotion_Approve` | `@BBYNR` | `ApproveSetupAsync` | Publish/kích hoạt CTKM từ bảng flat sang bảng chuẩn hoá `Offer*`, set `IsApprove=1`. Timeout 300s |
| `dbo.usp_SetupPromotion_UpdateStatus` | `@BBYNR, @Status` | `UpdateSetupStatusAsync` | Update cột `STATUS` trên `SetupPromotionHEADER`. Timeout 60s |
| `dbo.usp_SetupGroupSite_Save` | `@GroupCode, @GroupName, @ListStore, @UserName` | `SaveSiteGroupAsync` | Upsert `SetupGroupSite` theo `GroupCode` |
| `dbo.usp_SetupGroupItem_Save` | `@GroupCode, @GroupName, @ListItemNo, @UserName` | `SaveItemGroupAsync` | Upsert `SetupGroupItem` theo `GroupCode`; nếu group đã tồn tại chỉ update `GroupName`, **không ghi đè** `ListItemNo` (khớp giới hạn legacy) |

### ⚠️ Caveat vận hành quan trọng (từ `docs/architecture/database-schema.md`)

- 3 Table-Valued Parameter Type `dbo.SetupPromotionBuyTVP` / `SetupPromotionGetTVP` /
  `SetupPromotionSiteTVP` mà `SaveSetupAsync` dùng **chưa được định nghĩa** trong `CentralMD.sql` —
  cần tạo trước khi tính năng chạy được trên môi trường DEV/UAT/PROD.
- 3 script SP `docs/sql/SetupGroupSite_Save.sql`, `docs/sql/SetupGroupItem_Save.sql`,
  `docs/sql/SetupPromotion_Save.sql` **chưa được chạy trên DB thật** — phải áp dụng thủ công trên
  `RPOSMasterData` (DEV trước, rồi UAT/PROD) trước khi tính năng hoạt động.

### Redis Cache

| Key | TTL | Nguồn dữ liệu | Invalidate |
|---|---|---|---|
| `MD:OfferTypeOptions` | 43200s (12h) | `dbo.OfferType` | Không tự động — hết hạn theo TTL |
| `MD:SalesOrderTypeOptions` | 43200s | `dbo.SalesOrderType` | Không tự động |
| `MD:SiteGroupOptions` | 43200s | `dbo.SetupGroupSite` | **Xóa thủ công** trong `SaveSiteGroupAsync` sau khi lưu thành công |
| `MD:MemberCodeOptions` | 43200s | `dbo.OptionData` | Không tự động |

---

## 3. Data Models & DTOs

Toàn bộ trong `POS.Common.Dtos.Promotion` (Newtonsoft.Json, không AutoMapper — Dapper map trực
tiếp cột SQL → property qua alias `AS` trong câu SQL/SP).

### `OfferHeaderDto.cs` (dùng bởi `OffersPage`)

| DTO | Mục đích | Property chính |
|---|---|---|
| `OfferHeaderListItemDto` | 1 dòng danh mục Offer đã publish | `ID, BonusbuyNo, PromotionNo, Description, OfferType, SalesType, SalesTypeName, ItemNo, ItemName, Status, StyleProfile, StartingDate, EndingDate, LocalSiteGroup, LimitQty, VoucherFromDate, VoucherToDate, Counter, Pkey, LastDateModified, Total` |
| `OfferListFilter` | Filter danh mục | `TextSearch, PromotionName, Status, OfferType, ItemNo, PageNumber, PageSize=20` |
| `OptionItemDto` | Option chung (Value/Text) | `Value, Text` |

### `PromotionSetupDto.cs` (dùng bởi `PromotionSetupPage` + dialog con)

| DTO | Mục đích | Property chính |
|---|---|---|
| `PromotionSetupListItemDto` | 1 dòng danh sách Setup | `No, Description, OfferType, SalesType, Status, ValidFrom, ValidTo, IsApprove, Total` |
| `PromotionSetupListFilter` | Filter danh sách Setup | `OfferNo, OfferName, ApproveStatus, PageNumber, PageSize=20` |
| `PromotionSetupHeaderDto` | Header form editor (44 field khớp `SetupPromotionHEADER`) | `No, Description, SalesType, OfferType, Status="1", StartingDate, EndingDate, IsVoucher, IsApprove, ConditionBuy="AND", ConditionGet="AND", LimitQty, MemberOnly, MemberCode, PriorityBBY=1, NumOfDays, VoucherFromDate, VoucherToDate, VoucherValidDay, VoucherLimitNumber, AllowUseAfterDay, AllowUseAfterTime, FromTime, ToTime, Mon..Sun=true, MinValue, CheckTotalDiscount, TotalDiscountType, TotalDiscountValue` |
| `OfferTypeOptionDto` | Option Loại CTKM + cờ điều khiển UI | `Value, Text, IsTotalBill, IsSetupBuy, IsSetupGet, IsVoucher, IsGift, UserGuide` |
| `IOfferLineItem` (interface) | Hợp nhất dòng Buy/Get | `LineType, No, GroupCode, Description, UnitOfMeasure` |
| `OfferBuyLineDto : IOfferLineItem` | Dòng "Sản phẩm mua" | + `Quantity, ScaleType="C"` |
| `OfferGetLineDto : IOfferLineItem` | Dòng "Sản phẩm khuyến mãi" | + `Quantity, ScaleType="C", DiscountType, DiscountValue` |
| `OfferSiteLineDto` | Site group áp dụng | `SiteGroupCode, GroupName` |
| `PromotionSetupSaveRequest` | Request Lưu tạm | `Header, BuyRows, GetRows, SiteGroupCodes` |
| `PromotionSetupDetailDto` | Response chi tiết 1 CTKM | `Header, BuyRows, GetRows, SiteRows` |
| `ItemOptionDto` | Kết quả tìm sản phẩm | `No, Description, Uom` |
| `SiteGroupSaveRequest` | Lưu nhóm cửa hàng | `GroupCode, GroupName, StoreListRaw` |
| `SiteGroupListItemDto` | Danh sách nhóm cửa hàng | `GroupCode, GroupName, StoreCount, Status, LastUpdateDate, Total` |
| `SiteGroupStoreItemDto` | 1 CH/ST trong nhóm | `StoreNo, StoreName` |
| `ItemGroupSaveRequest` | Lưu nhóm sản phẩm | `GroupCode, GroupName, ItemNos` |
| `ItemGroupListItemDto` | Danh sách nhóm sản phẩm | `GroupCode, GroupName, ItemCount, Status, LastUpdateDate, Total` |
| `ItemGroupItemDto` | 1 sản phẩm trong nhóm | `No, Description, Uom` |

> `SpecialComboDto.cs` cùng thư mục nhưng thuộc chức năng "Combo đặc biệt" (`ISpecialComboService`)
> — không liên quan `IPromotionService`/2 trang đang xét, không liệt kê chi tiết ở đây.

---

## 4. UI Logic & Luồng nghiệp vụ (Business Flow)

### 4.1 Trang Danh sách — `OffersPage.razor`

- Load dữ liệu: `MudTable ServerData="LoadServerData"` → `PromotionService.GetOfferHeaderListAsync
  (_filter, token)` trả `(items, total)`; `_offset` tính từ `state.Page * state.PageSize` để hiển
  thị STT đúng qua trang.
- Filter (`OfferListFilter`): `OfferType` (select, load từ `GetOfferTypeOptionsAsync`), `Status`
  (`-1`=Tất cả/`0`=Có hiệu lực/`2`=Hết hiệu lực), `TextSearch` (Bonus Buy/Promotion No),
  `ItemNo`, `PromotionName`. Nút Tìm gọi `_table.ReloadServerData()`; nút Xóa reset filter + reload.
- Cột `Trạng thái` hiển thị `MudChip` màu theo `IsActive(status)` — hàm helper check chuỗi tiếng
  Việt chứa "hiệu lực" và không chứa "Hết" (không dựa vào mã số).
- Export Excel: `ExportOfferHeaderListAsync(_filter)` (không phân trang thật — `PageSize=100000`
  phía Repository) → `BuildXlsx` dùng `ClosedXML` dựng 11 cột → `IJSRuntime.SaveAsFileAsync` tải
  file `DanhMucKhuyenMai_{yyyyMMddHHmmss}.xlsx` về client.
- Không có audit log trên trang này (chỉ đọc dữ liệu, không ghi).

### 4.2 Trang Cài đặt/Thêm mới — `PromotionSetupPage.razor`

Trang gồm 2 mode dựng trong cùng 1 file (biến `_editing`), không có code-behind riêng.

**List mode** (`!_editing`): `GetSetupListAsync` với filter `OfferNo` (khớp đúng `BBYNR`),
`OfferName` (LIKE mô tả), `ApproveStatus` (`-1` Tất cả/`0` Chưa duyệt/`1` Đã duyệt). Mỗi dòng có
nút Sửa/Xem (tùy `IsApprove`) và nút Duyệt nhanh (chỉ hiện khi chưa duyệt).

**Editor mode** (`_editing`): kiến trúc gồm 1 khối form cố định (Mã CTKM, Hình thức bán, Tên CTKM,
Trạng thái, Loại CTKM, Voucher/Coupon checkbox, Từ ngày/Đến ngày) + `MudTabs` **5 tab**:

1. **Thông tin chung** — bảng tóm tắt 1 dòng (chính `_header`) cho phép sửa `FromTime/ToTime` và
   7 checkbox Mon..Sun (lịch áp dụng trong tuần).
2. **Sản phẩm mua** — `MudTable` trên `_buyRows` (`List<OfferBuyLineDto>`); toolbar có bulk-add
   (1-50 dòng), `ConditionBuy` (AND/OR), `MinValue` (chỉ bật khi Loại CTKM có cờ `IsTotalBill`).
   Mỗi dòng chọn Loại (Sản phẩm/Nhóm SP) → nếu Sản phẩm thì autocomplete barcode (`OnBarcodeBlur`
   gọi `SearchItemsAsync` tự điền Description/UOM khi khớp chính xác); nếu Nhóm SP thì mở dialog
   `ItemGroupSetupDialog`.
3. **Sản phẩm khuyến mãi** — tương tự tab Buy nhưng trên `_getRows`
   (`List<OfferGetLineDto>`), thêm `DiscountType`/`DiscountValue` mỗi dòng, và toggle
   `CheckTotalDiscount` (giảm giá tổng bill) — khi bật sẽ **xóa toàn bộ `_getRows`** sau khi user
   xác nhận qua `MudMessageBox` (`_confirmDiscountBox`), vì 2 cơ chế loại trừ lẫn nhau.
4. **Cửa hàng áp dụng** — `_siteRows` (`List<OfferSiteLineDto>`); thêm bằng autocomplete
   (`SearchSiteGroupAsync`, lọc local trong `_siteGroups` đã preload) hoặc dialog
   `SiteGroupSetupDialog`; xem chi tiết CH/ST của 1 nhóm qua `GetSiteGroupStoresAsync`.
5. **Cài đặt nâng cao** — Giới hạn & ưu tiên (`LimitQty`, `PriorityBBY` 1-10, `NumOfDays`),
   Thành viên (`MemberOnly` + `MemberCode` autocomplete gõ tự do), và khối Voucher/Coupon (chỉ
   hiện khi `_header.IsVoucher`): `VoucherFromDate/ToDate`, `VoucherValidDay`,
   `VoucherLimitNumber`, `AllowUseAfterDay`, `AllowUseAfterTime`.

**Binding**: hầu hết `@bind-Value`/`@bind-Date` trực tiếp trên property DTO (two-way binding chuẩn
Blazor); vài chỗ dùng `Value`/`ValueChanged` tường minh để chặn side-effect (`CheckTotalDiscount` —
chặn bằng confirm dialog trước khi set; ô Số lượng — ép kiểu decimal↔int qua `ValueChanged`).

**Validation**: Chỉ có validation **hiển thị** (`Required`/`RequiredError` của MudBlazor) trên
SalesType, Description, OfferType, Từ ngày/Đến ngày — không dùng DataAnnotations. Validation
**thật sự có hiệu lực** nằm ở tầng Repository (`PromotionRepository.SaveSetupAsync`):

```text
- Description, SalesType bắt buộc
- OfferType bắt buộc CHỈ KHI tạo mới (No rỗng) — bản ghi cũ có thể thiếu
- Status phải thuộc {"0","1","2"}
- StartingDate/EndingDate phải parse được "dd/MM/yyyy", EndingDate >= StartingDate
- Nếu CheckTotalDiscount=true thì TotalDiscountValue phải > 0
- Nếu FromTime hoặc ToTime có giá trị thì cả 2 phải có, parse "HH:mm", ToTime > FromTime
```

**Luồng Lưu tạm** (`SaveAsync`):
```
_header.StartingDate/EndingDate ← format lại từ _fromDate/_toDate (dd/MM/yyyy)
build PromotionSetupSaveRequest { Header, BuyRows, GetRows, SiteGroupCodes }
  → PromotionService.SaveSetupAsync(request) → (Ok, Message, BBYNR)
  → nếu Ok: cập nhật _header.No = BBYNR
           → AuditLogger.LogAsync(actor, isNew ? "CREATE" : "UPDATE", "SetupPromotion", bbynr,
                                   oldValueJson: null, newValueJson: JSON(request))
```
> Ghi chú: `oldValueJson` luôn `null` kể cả khi UPDATE — trang không snapshot state cũ trước khi
> sửa (khác với chuẩn Audit Log ở `CLAUDE.md` §16 vốn khuyến nghị snapshot `oldValue` cho UPDATE).

**Luồng Duyệt CTKM** — có 2 điểm vào, xử lý khác nhau để tránh publish nhầm dữ liệu:

```
ApproveAsync(bbynr)            [nút Duyệt nhanh ở List — KHÔNG mở editor]
  → confirm dialog
  → ApproveCoreAsync(bbynr)    (KHÔNG Lưu tạm trước — publish đúng bản nháp đã có sẵn trong DB)

ApproveFromEditorAsync()        [nút "Duyệt CTKM" trong Editor]
  → confirm dialog (cảnh báo sẽ Lưu tạm trước khi Duyệt)
  → SaveAsync(showSuccessSnackbar:false)   ← BẮT BUỘC, tránh duyệt nhầm state cũ hơn state đang sửa
  → ApproveCoreAsync(_header.No)

ApproveCoreAsync(bbynr):
  → PromotionService.ApproveSetupAsync(bbynr) → (Ok, Message)
  → AuditLogger.LogAsync(actor, "APPROVE", "SetupPromotion", bbynr, null, null)
  → nếu đang mở đúng record: _isReadonly = true, _header.IsApprove = true
  → nếu ở List mode: reload bảng
```

---

## 5. APIs & Services Integration

Không có REST API/Controller (`POS.Api`) cho Promotion — POS.Web inject `IPromotionService`
trực tiếp qua DI (Blazor Server, cùng process). Toàn bộ 18 method của `IPromotionService`
(namespace `POS.Application.Features.Promotion`) đều là delegate 1-dòng xuống
`IPromotionRepository` cùng chữ ký — không có mapping/business logic riêng ở tầng Application.

| Method | Input | Output | Trang/nơi gọi | SP / bảng tương ứng |
|---|---|---|---|---|
| `GetOfferHeaderListAsync` | `OfferListFilter, ct` | `(List<OfferHeaderListItemDto>, int Total)` | OffersPage (list) | SP `GetPromotionOfferHeaderList` |
| `ExportOfferHeaderListAsync` | `OfferListFilter, ct` | `List<OfferHeaderListItemDto>` | OffersPage (export) | SP `GetPromotionOfferHeaderList` (PageSize=100000) |
| `GetOfferTypeOptionsAsync` | `ct` | `List<OfferTypeOptionDto>` | Cả 2 trang | `dbo.OfferType` (cache `MD:OfferTypeOptions`) |
| `GetSalesOrderTypeOptionsAsync` | `ct` | `List<OptionItemDto>` | PromotionSetupPage | `dbo.SalesOrderType` (cache `MD:SalesOrderTypeOptions`) |
| `GetSetupListAsync` | `PromotionSetupListFilter, ct` | `(List<PromotionSetupListItemDto>, int Total)` | PromotionSetupPage (list) | `dbo.SetupPromotionHEADER` (inline SQL) |
| `GetSetupDetailAsync` | `string bbynr, ct` | `PromotionSetupDetailDto?` | PromotionSetupPage (mở editor) | `SetupPromotionHEADER/BUY/GET/SITE` (multi-resultset inline SQL) |
| `SaveSetupAsync` | `PromotionSetupSaveRequest, ct` | `(bool Ok, string Message, string BBYNR)` | PromotionSetupPage (Lưu tạm) | SP `usp_SaveSetupCTKMAll` |
| `ApproveSetupAsync` | `string bbynr, ct` | `(bool Ok, string Message)` | PromotionSetupPage (Duyệt) | SP `usp_SetupPromotion_Approve` |
| `UpdateSetupStatusAsync` | `string bbynr, string status, ct` | `bool` | (định nghĩa sẵn, không thấy gọi từ 2 trang đang xét) | SP `usp_SetupPromotion_UpdateStatus` |
| `SearchItemsAsync` | `string keyword, ct` | `List<ItemOptionDto>` | PromotionSetupPage (autocomplete SP) | `dbo.Item` |
| `GetSiteGroupOptionsAsync` | `ct` | `List<OfferSiteLineDto>` | PromotionSetupPage | `dbo.SetupGroupSite` (cache `MD:SiteGroupOptions`) |
| `GetMemberCodeOptionsAsync` | `ct` | `List<OptionItemDto>` | PromotionSetupPage | `dbo.OptionData` (cache `MD:MemberCodeOptions`) |
| `SaveSiteGroupAsync` | `SiteGroupSaveRequest, string actor, ct` | `(bool Ok, string Message)` | `SiteGroupSetupDialog` (suy luận, ngoài phạm vi đọc chi tiết) | SP `usp_SetupGroupSite_Save` (invalidate cache) |
| `GetSiteGroupListAsync` | `groupCode, groupName, pageNumber, pageSize, ct` | `(List<SiteGroupListItemDto>, int Total)` | `SiteGroupSetupDialog` (suy luận) | `dbo.SetupGroupSite` (inline SQL) |
| `GetSiteGroupStoresAsync` | `groupCode, storeNo, storeName, ct` | `List<SiteGroupStoreItemDto>` | PromotionSetupPage (xem CH/ST của nhóm) | `dbo.SetupGroupSite` + `dbo.Store` |
| `SaveItemGroupAsync` | `ItemGroupSaveRequest, string actor, ct` | `(bool Ok, string Message)` | `ItemGroupSetupDialog` (suy luận) | SP `usp_SetupGroupItem_Save` |
| `GetItemGroupListAsync` | `groupCode, groupName, pageNumber, pageSize, ct` | `(List<ItemGroupListItemDto>, int Total)` | `ItemGroupSetupDialog` (suy luận) | `dbo.SetupGroupItem` (inline SQL) |
| `GetItemGroupItemsAsync` | `groupCode, itemNo, itemName, ct` | `List<ItemGroupItemDto>` | `ItemGroupSetupDialog` (suy luận) | `dbo.SetupGroupItem` + `dbo.Item` |

> Các dòng ghi "suy luận" là method có trong `IPromotionService` nhưng **không được gọi trực tiếp**
> từ `OffersPage.razor`/`PromotionSetupPage.razor` — theo tên method và bối cảnh (dialog cấu hình
> nhóm cửa hàng/nhóm sản phẩm mở từ `PromotionSetupPage`), nhiều khả năng được `SiteGroupSetupDialog`/
> `ItemGroupSetupDialog` gọi, nhưng nội dung 2 file dialog này **chưa được đọc** trong lần khảo sát
> này — không khẳng định chắc chắn.

---

## Giới hạn của tài liệu (những gì CHƯA verify được)

- **Không đọc** nội dung file `SiteGroupSetupDialog.razor` và `ItemGroupSetupDialog.razor` — chỉ
  biết chúng tồn tại và được mở qua `IDialogService.ShowAsync<...>`. Mối liên hệ giữa các method
  "suy luận" ở mục 5 và 2 dialog này là suy luận hợp lý theo tên, **không phải xác nhận trực tiếp**.
- **Không có SQL body thật** của các SP trong repo (SP là object phía DB `RPOSMasterData`, không
  phải file trong `POS.slnx`) — mọi mô tả hành vi SP ở mục 2 lấy từ chú thích trong
  `docs/architecture/database-schema.md`, không phải đọc trực tiếp T-SQL.
- Chưa xác minh được các TVP `SetupPromotionBuyTVP/GetTVP/SiteTVP` và 3 SP mới
  (`usp_SetupGroupSite_Save`, `usp_SetupGroupItem_Save`) đã được áp dụng trên DB thật (DEV/UAT/PROD)
  hay chưa — theo ghi chú trong `database-schema.md` thì **tại thời điểm viết tài liệu là CHƯA**.

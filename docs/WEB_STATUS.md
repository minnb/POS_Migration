# POS.Web — Báo cáo hiện trạng
<<<<<<< HEAD
> Cập nhật: 2026-07-06 (Topbar/AppBar + Typography audit theo mockup `theme_html.html` —
> (1) **Typography pixel-perfect**: `PosTheme.cs` (`Default.LineHeight` 1.45→1.5, `Button.FontSize`
> thêm 12px + bỏ letter-spacing thừa, `Body1.FontSize` 12px→12.5px), `app.css` (sidebar L1/L2 size,
> `.mud-table-body .mud-table-cell` 12.5px mới, `.mud-input-label-inputcontrol` uppercase/bold mới,
> xóa dead-code line-height mobile-only), 4 class mới `.pos-kpi-value/.pos-kpi-label/.pos-card-title/
> .pos-section-label` áp mẫu trên `RevenueByStorePage.razor`/`ShiftSummaryPage.razor` (CHƯA rollout
> 80 file còn lại). (2) **Topbar**: bỏ `Dense="true"` trên `MudAppBar`, thêm
> `LayoutProperties.AppbarHeight="50px"` khớp mockup; thay title tĩnh "RPOS Dashboard" bằng
> breadcrumb động (`BreadcrumbMap` 43 route, copy đúng text sidebar). Xác nhận rõ: mockup topbar
> KHÔNG có User Profile/Notification (chỉ có ở sidebar-footer) nên không map gì thêm vào đó. Cập
> nhật `.claude/rules/mudblazor-flat-ui.md` (mục 11, 11.1 checklist), `.claude/skills/web/SKILLS.md`
> (pattern Breadcrumb động). Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests`
> 25/25. Chưa xác nhận trực quan trên browser thật (chưa `dotnet run` để đo bằng DevTools). Chi
> tiết: `docs/CHANGELOG.md`)
> Trước đó 2026-07-06 (PromotionSetupPage `/promotion/setup` — hoàn thiện phần bị hoãn của task
> trước: modal "Cài đặt nhóm sản phẩm" cho dòng Buy/Get chọn Loại = "Nhóm SP" (LineType=1). Đọc
> trực tiếp `_ViewSetupGroupItemBuy/Get.cshtml`, `_ViewDataBuyGroupItem/GetGroupItem.cshtml` legacy
> — xác nhận bảng `dbo.SetupGroupItem` đã tồn tại thật trong `src/legacy/Database/CentralMD.sql`
> (chưa có tài liệu ở `database-schema.md`, chưa có SP nào). Port theo đúng khuôn mẫu "Site Group"
> đã làm đợt trước: dialog mới `ItemGroupSetupDialog.razor` (2 sub-tab: "Cài đặt nhóm sản phẩm" —
> tạo mới, tìm sản phẩm qua autocomplete, ĐVT hiển thị read-only vì cột DB không lưu UOM; "Danh
> sách nhóm sản phẩm" — filter/phân trang/xem chi tiết sản phẩm/chọn gắn vào dòng đang sửa, phục
> hồi tính năng đang bị `display:none` ẩn khỏi UI của legacy). Giữ nguyên 2 hạn chế legacy theo
> quyết định người dùng: (1) `ListItemNo` chỉ lưu `List<string>` ItemNo, không lưu UOM; (2) nhóm
> đã tồn tại chỉ sửa được Tên, không ghi đè lại danh sách sản phẩm. Khác legacy: lưu DB ngay khi
> bấm "Lưu" trong dialog (không qua sessionStorage) — nhất quán với Site Group. SP mới
> `usp_SetupGroupItem_Save` (`docs/sql/SetupGroupItem_Save.sql`) — **chưa chạy trên DB thật**, cần
> chạy tay trên `RPOSMasterData` (DEV trước) trước khi test UI. Verify: `dotnet build` 0 lỗi,
> `dotnet test tests/POS.ContractTests` 25/25. Chi tiết: `docs/CHANGELOG.md`)
> Trước đó 2026-07-05 (PromotionSetupPage `/promotion/setup` — chuyển đổi lại form "Cài đặt CTKM"
> khớp 100% ô nhập liệu legacy `src/legacy/VCM.BLUEPOS/Views/SetupPromotion/*.cshtml` (đọc trực
> tiếp HTML gốc, không chỉ ảnh mockup). Thay đổi chính: (1) tách field header (Tên/Loại CTKM/Hình
> thức bán/Trạng thái/Voucher/Từ-Đến ngày) ra khối `MudPaper` cố định NGOÀI 4 tab; (2) tab "Thông
> tin chung" đổi thành bảng tóm tắt lịch áp dụng (Từ giờ/Đến giờ + Mon-Sun checkbox, field mới
> `FromTime/ToTime/Mon..Sun` — cột DB `TIMEFROM/TIMETO/MON..SUN` trước đây SP hard-code rỗng); (3)
> tab "Sản phẩm mua" thêm toolbar bulk-add "Số lượng dòng" + "Giá trị tổng tiền tối thiểu"
> (`MinValue`, enable theo cờ `IsTotalBill` của OfferType — không phải checkbox tự do, khớp hành
> vi legacy khoá cứng checkbox) + cột "Điều kiện áp dụng" (ScaleType A/B/C); (4) tab "Sản phẩm
> khuyến mãi" thêm checkbox "Giảm giá tổng bill" (`CheckTotalDiscount`/`TotalDiscountType`/
> `TotalDiscountValue`) loại trừ với bảng dòng Get (confirm xoá khi bật); (5) tab "Cửa hàng áp
> dụng" thêm nút "Chọn nhóm CH/ST" mở dialog mới `SiteGroupSetupDialog.razor` (2 sub-tab: tạo mới
> nhóm + danh sách nhóm có filter/phân trang/xem chi tiết store/chọn gắn vào CTKM); (6) tab "Cài
> đặt nâng cao" thêm `AllowUseAfterDay`/`AllowUseAfterTime` (voucher delay), đổi `MemberCode` từ
> `MudSelect` sang `MudAutocomplete` (gõ tự do + gợi ý, khớp bản chất text-input của legacy). SP
> `usp_SaveSetupCTKMAll` sửa thêm tham số (KHÔNG ALTER TABLE — cột đã có sẵn), SP mới
> `usp_SetupGroupSite_Save` — 2 script `.sql` cần chạy tay trên `RPOSMasterData` (DEV trước) TRƯỚC
> khi test UI, chưa chạy tự động. Ngoài phạm vi đợt này (hoãn task riêng): modal "CÀI ĐẶT NHÓM SẢN
> PHẨM" (định nghĩa item cụ thể trong group cho dòng Buy/Get "Theo nhóm"). Verify: `dotnet build`
> 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25. Chưa xác nhận trực quan trên browser thật (SP
> chưa chạy trên DB thật) — cần chạy 2 script SQL rồi tự kiểm tra UI. Chi tiết: `docs/CHANGELOG.md`)
> Trước đó 2026-07-05 (MudBlazor Theme Standard v3 — chuyển toàn bộ theme sang ngôn ngữ thiết kế
> mockup `docs/web/theme/theme_html.html` (tham chiếu, KHÔNG port UI nghiệp vụ ngân sách của mockup).
> `PosTheme.cs`: đổi toàn bộ Palette (Primary `#2660A4`/Secondary `#4A6070`/Tertiary `#6040A8`/
> Success `#1F7A4A`/Error `#B52B27`/Warning `#D4860A`/Info `#3D8FD9`, sidebar `DrawerBackground
> #0D1B2A`), `DefaultBorderRadius` 16px→12px, `Shadows.Elevation[2-5]` từ "none" sang shadow thật.
> `MainLayout.razor` + `app.css`: sidebar navy đậm 3 cấp (L1 in hoa không icon, L2 icon riêng từng
> nhóm, L3 `ChevronRight` đồng nhất), sidebar-footer (avatar initials + tên/role/logout, dời khỏi
> AppBar), table header uppercase/muted, filter panel trắng+border. Quy ước `MudButton` đảo ngược:
> Filled/Primary cho CTA (Lưu/Thêm mới/Tìm), Filled/Success cho hành động chốt luồng (Duyệt),
> Outlined/Error cho phá hủy (Xóa), Outlined không màu cho trung tính (Hủy/Đóng) — rollout đủ 59
> file/5 cụm menu. **Font audit bổ sung cùng ngày**: phát hiện `Typography.Default.FontFamily`
> KHÔNG cascade xuống các variant khác (H1-H6/Subtitle/Body/Caption/Overline/Button đều có CSS
> variable riêng trong MudBlazor) — set `FontFamily=["Segoe UI","system-ui","sans-serif"]` tường
> minh trên TỪNG variant trong `PosTheme.cs`; `Default.FontSize` 14px→13px, `Body1` 12px (input),
> `.pos-table` 14px→13px (app.css); gỡ Google Fonts Roboto `<link>` khỏi `App.razor` (không còn
> cần). Đã cập nhật `CLAUDE.md §14`, `.claude/rules/mudblazor-flat-ui.md` (v3), `.claude/skills/
> web/SKILLS.md` (pattern per-variant FontFamily + MudMessageBox YesButton). Verify: `dotnet build`
> 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25. Chưa xác nhận trực quan trên browser thật —
> cần tự chạy app kiểm tra. Chi tiết: `docs/CHANGELOG.md`)
=======
> Cập nhật: 2026-07-06 (PricesPage `/catalog/prices` — nâng cấp 9.1 Danh mục Bảng giá: (1) thêm
> cột "Hình thức" (SaleTypeName) trước "Nhóm giá" + cột "Trạng thái" (Hiệu lực/Chưa hiệu lực/Hết
> hiệu lực, MudChip màu, tính client-side theo Start/EndingDateStr); (2) ngày `01/01/9999` hiển thị
> "Vô thời hạn"; (3) filter Barcode/SalesCode (text tự do) → combobox "Hình thức bán hàng"/"Nhóm
> giá" (reuse `PriceService.GetSetupLookupAsync`), ẩn cột Site; (4) format nghìn khi nhập "Giá bán"
> (`FormatThousands`, khớp pattern `PriceSetupPage.razor`); (5) **FIX bug Sửa/Xóa giá**: SP
> `GetSalesPriceList` đổi trả `SalesCode`=tên nhóm giá (không phải mã) → thêm cột `SalesGroupCode`/
> `SalesTypeCode` (mã gốc) + field `PriceRowKey.SalesType` để định vị đúng dòng khi 1 item/uom/nhóm
> giá/ngày hiệu lực có nhiều dòng khác SalesType; (6) phát hiện `SalesPrice` thực ra CÓ cột
> `IsActive`/`LastTimeUpdate` (đính chính ghi chú cũ), `usp_SalesPrice_SoftDelete` nay set
> `IsActive=0` khi xóa mềm (trước đây có thể sót hiển thị dòng đã xóa khi bỏ check "Còn hiệu lực").
> SP cần chạy tay: `GetSalesPriceList_AddSaleType.sql` → `_AddSalesTypeCode.sql`,
> `SalesPrice_EditDelete_AddSalesType.sql`. Verify: `dotnet test tests/POS.ContractTests` 25/25 (build
> POS.Web bị khoá file do instance đang chạy — không phải lỗi biên dịch). Chi tiết: `docs/CHANGELOG.md`)
>>>>>>> 7ff26a64942c307f60c821c0812ddb403e305471
> Trước đó 2026-07-05 (BusinessDayPage `/store/business-day` — 4 điều chỉnh: (1) FIX crash
> "duplicate key" khi tìm kiếm — SP `GetSalesEODConfirm` trả cột tên legacy (`TerminalID`,
> `AmountTotal`…) ≠ property DTO → Dapper để trống `PosTerminal`, thêm class trung gian
> `SalesEodConfirmRow` + `CommandType.StoredProcedure` map tường minh trong `CentralSaleRepository`;
> (2) phân quyền force EOD: ITOps/SystemAdmin xác nhận được kể cả khi còn POS chưa đóng ngày —
> thêm param `allowForceConfirm` cho `IBusinessDayService.ConfirmBusinessDayAsync`, page kiểm
> `IsInRole`; (3) sau xác nhận thành công tự load lại ngày D+1 (advance `_businessDate`); (4)
> StoreOperator mở trang tự chọn cửa hàng + ngày kinh doanh hiện tại của store
> (`GetCurrentBusinessDateAsync` → `BussinessDateOpen`) và auto-load, khỏi bấm "Tìm kiếm". Verify:
> `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25. Chi tiết: `docs/CHANGELOG.md`,
> `docs/web/logic/eod.md`)
> Trước đó 2026-07-04 (Fix bẫy `DialogService.ShowAsync<MudMessageBox>` — nút Yes render bằng
> markup mặc định của MudBlazor, không style được theo chuẩn Outlined, grep `Variant.Filled` không
> bắt được vì nút không tồn tại trong markup page. Chuyển 8 file sang `<MudMessageBox @ref>` +
> `<YesButton><MudButton Variant="Variant.Outlined">`: `BusinessDayPage`, `VouchersPage`,
> `SpecialComboPage`, `PromotionSetupPage`, `PosDataSetupPage`, `DataRawLogPage`, `UsersPage`
> (thêm field động `_confirmTitle`/`_confirmYesText`/`_confirmYesColor`), `BankPosPage`. Cập nhật
> chuẩn ngăn tái diễn ở `.claude/skills/web/SKILLS.md`, `CLAUDE.md §14`,
> `.claude/rules/mudblazor-flat-ui.md §3`. Verify: `dotnet build` 0 lỗi, `dotnet test
> tests/POS.ContractTests` 25/25. Chi tiết: `docs/CHANGELOG.md`)
> Trước đó 2026-07-04 (MudBlazor Flat UI v2 — rollout đầy đủ 4 cụm menu còn lại: Cửa hàng, Khuyến
> mãi, Vận hành, Quản trị — ~35 page + ~25 dialog, tiếp nối pilot 9 page "Danh mục" cùng ngày. Mọi
> `MudButton` `Filled`/`Text`→`Outlined` không ngoại lệ; filter/input `MudPaper` thêm
> `pos-filter-panel`; page-header icon/button `Size.Small` + title `font-weight:400`; dọn hardcode
> `border-radius:4px`. Phát hiện + vá 2 dialog bị bỏ sót đợt pilot (`PriceItemPickerDialog`,
> `PosTerminalEditDialog`) + 1 page bị bỏ sót vì không có trong sidebar nav (`VoucherIssuePage` +
> `VoucherItemPickerDialog`, đối xứng `CouponIssuePage` đã convert ở pilot). Thực hiện qua 6
> subagent song song, verify cuối bằng `dotnet build` (0 lỗi) + `dotnet test
> tests/POS.ContractTests` (25/25). Chi tiết: `docs/CHANGELOG.md`,
> `.claude/rules/mudblazor-flat-ui.md`)
> Trước đó 2026-07-04 (Sidebar UI polish `MainLayout.razor`: icon sub-group cấp 2 đổi đồng nhất về
> `ChevronRight` giống leaf cấp 3 (bỏ Icon riêng: WorkHistory/PointOfSale/Assessment/Business/Inventory/
> PriceChange/Campaign/ConfirmationNumber/Monitor/Article/Tune); thêm `HideExpandIcon="true"` cho mọi
> `MudNavGroup` (cấp 1+2) ẩn mũi tên expand mặc định bên phải — giữ nguyên `@bind-Expanded` +
> accordion tự mở/đóng theo route (I3); đổi tên 6 title leaf: "Tỉnh / Thành"→"Chi nhánh", "Khai báo máy
> POS"→"POSTerminal", "Máy POS ngân hàng"→"POS bank", "Danh sách SP / Barcode"→"Danh sách", "Setup giá
> (Bulk Import)"→"Setup giá bán", "Danh mục khuyến mãi"→"Danh mục"; CSS `app.css`: dòng menu cấp 2
> `padding-top/bottom:3px` + `line-height:1.5` (thu gọn ~15% so với mặc định MudBlazor), `letter-spacing:
> -0.022em` trên `.mud-drawer .mud-nav-link` (rút ngắn tracking, tránh xuống dòng tên dài))
> Trước đó 2026-07-04 (MudBlazor Flat UI v2 — theo mẫu "Mud Mini": sidebar/AppBar chuyển nền sáng
> `#FFFFFF` (từ navy `#1B3A5C`), card borderless (`Shadows` E1-E5 = `"none"`, thay hairline),
> `DefaultBorderRadius` 4px→16px, input font-size giảm 15% (`Body1` 0.75rem + FontWeight 400), mọi
> `MudButton` `Filled`/`Text`→`Outlined` không ngoại lệ kể cả trong dialog; áp dụng đầy đủ cho 9
> page + 9 dialog menu "Danh mục" (Employees/Store/Provinces/PosMap/BankPos/Products/ProductLock/
> Prices/PriceSetup); sidebar đổi icon `Filled`→`Outlined` + thêm `div.pos-sidebar-brand` thay
> `MudDrawerHeader` cũ; brand text đổi "POSMaster"→"RPOS". Chi tiết:
> `.claude/rules/mudblazor-flat-ui.md`, `CLAUDE.md §14`, `docs/CHANGELOG.md`)
> Trước đó 2026-07-04 (BusinessDayPage `/store/business-day`: viết lại hoàn toàn thành "Xác nhận kết
> thúc ngày" — port từ legacy `StoreActivitiesController.ConfirmEndingDateStores`/`CheckFinishDate`.
> Thêm `IBusinessDayService` (`POS.Application.Features.StoreActivities`), 3 method mới trong
> `ICentralSaleRepository` (`GetPosDayStagingAsync`, `GetBusinessDayConfirmAsync`,
> `ConfirmBusinessDayAsync`), bảng `dbo.BusinessDayConfirm` + SP `usp_BusinessDay_ConfirmEndDate`
> (DB CentralSale theo từng store — KHÔNG phải CentralMD — để atomic với update `BussinessDateOpen`);
> xem `docs/sql/BusinessDay_ConfirmEndDate.sql`. Menu đổi tên "Ngày kinh doanh"→"Xác nhận kết thúc
> ngày"; xóa `EosDayShiftListDialog.razor` (orphaned))
> Trước đó 2026-07-03 (PosMapPage `/catalog/pos-setup`: thêm cột Action → nút "Đẩy dữ liệu đầu ngày" cho
> máy POS — confirm `MudMessageBox`, spinner+pulse nền dòng khi xử lý, `@onclick:stopPropagation`, audit log `SYNC`;
> gọi trực tiếp `ISyncDataPosService.PushStartOfDayDataAsync` qua DI (không HTTP) tái dùng `EnsureMasterDataFileAsync`
> sinh zip full-data ALL vào `{FtpRootPath}\SyncDataPos\POS\CHANGE\{site}\{terminal}`; rollout §O3)
> Trước đó 2026-07-03 (Mã hóa credentials appsettings.Production.json — C4 rollout thực thi: sinh
> `POS_SECRET_KEY`, mã hóa 9 connection string + RabbitMQ password trong CẢ POS.Api và POS.Web
> `appsettings.Production.json`, key ghi vào `.env` local; thêm decrypt hook cho POS.Api (trước đó chỉ
> POS.Web), đổi tên biến chung `POSWEB_SECRET_KEY`→`POS_SECRET_KEY`; tài liệu tra cứu mới
> `docs/architecture/appsetting.md`)
> Trước đó 2026-07-02 (UI audit CouponIssuePage `/promotion/coupons/issue`: gộp toàn bộ field của `CouponAdvancedDialog` xuống form chính — nút "Cài đặt nâng cao" giữ code nhưng ẩn (`_showAdvancedButton=false`); `SaveAsync` gọi nối tiếp `SaveIssueAsync`→`SaveAdvancedAsync`; fix rule ngày `SaveAdvancedAsync` chỉ chặn "Từ ngày quá khứ" khi tạo mới; layout rút gọn dần từ 2 MudCard → 1 MudCard chia nhóm con `MudPaper Outlined` bo viền (Thông tin chung / Thời gian hiệu lực + Giới hạn / Cấu hình mã & giảm giá), bỏ `MudCardHeader`, bỏ `HelperText` (gộp hint ngắn vào Label), tiêu đề nhóm con kiểu legend lồng viền; `MudNumericField` đổi Variant theo kiểu dữ liệu (int→Text, double→Outlined+Step) — pattern mới, đã cập nhật `.claude/skills/web/form-input.md` §1a/§4a)
> Trước đó 2026-07-01 (Catalog/Price: 9.1 Danh mục Bảng giá `/catalog/prices` (list + filter + Export, reuse SP GetSalesPriceList*) + 9.3 Setup giá Bulk Import `/catalog/price-setup` (import Excel validate + lưới preview sửa inline + item picker + Lưu + audit; SP mới usp_SetupSalePrice_Save TVP, ủy quyền Setup_SalePrice_Get_ALL) — service 3 lớp IPriceService)
> Trước đó 2026-07-01 (UI polish PromotionSetupPage `/promotion/setup`: MudTabs icon+gạch chân active, MudCard gom nhóm cả 5 tab, tooltip/HelperText giải thích, validation trực quan `Required`/`RequiredError`, nút Lưu spinner khi `_saving`, combobox "Điều kiện" 160→240px — markup-only, giữ 100% @code; + tài liệu `docs/web/LOGIC_APPROVE_CTKM.md`)
> Trước đó 2026-07-01 (Promotion/CouponVoucher: 8.1 Cài đặt Coupon + 8.2 Phát hành Coupon + 8.3 Danh mục Voucher (CRUD) + 8.4 Tra cứu Voucher phát hành — service 3 lớp, SP mới usp_SetupCoupon_*/usp_SetupVoucher_*, 8.4 reuse SP CentralSales)
> Trước đó 2026-07-01 (Bug fix: sidebar accordion (I3) + active NavLink highlight (I4) sai logic; BankPosPage/BankPosDetailDialog — sai tên bảng vật lý + SP param + crash circuit khi lookup lỗi)
> Trước đó 2026-06-30 (thêm Catalog section: ProductsPage 6.1+6.2+6.3, ProductLockPage 6.4 — migrate từ VCM.BLUEPOS)
> Trước đó 2026-06-28 (Security hardening: config-driven HTTPS/cookie + RequireHttps, security headers/CSP, mã hóa credentials AES-256-GCM `enc:`, SQL Console mask+toggle, DetailedErrors off Prod)

---

## Cây thư mục hiện tại
_(bỏ qua bin/ và obj/)_

```
src/POS.Web/
├── Auth/
│   ├── DashboardUser.cs / IWebUserService.cs / WebUserService.cs / WebRoles.cs
│   ├── IAuditLogger.cs               ← interface + DbAuditLogger (ghi DashboardAuditLog)
│   ├── migration_dashboard_users.sql
│   ├── migration_sql_console_audit.sql
│   └── migration_dashboard_audit_log.sql  ← DashboardAuditLog + 3 index (chạy trước deploy)
├── Theme/
│   └── PosTheme.cs                  ← MudBlazor custom theme (flat, navy + teal)
├── Components/
│   ├── _Imports.razor / App.razor / Routes.razor / RedirectToLogin.razor / RedirectToAccessDenied.razor
│   ├── Layout/
│   │   ├── EmptyLayout.razor / MainLayout.razor (+ .razor.css) / ReconnectModal.razor (+ .razor.css)
│   └── Pages/
│       ├── AccessDenied.razor / Index.razor / Login.razor
│       ├── Admin/
│       │   ├── UsersPage.razor / RolesPage.razor / ConfigPage.razor / AuditPage.razor
│       │   ├── SqlConsolePage.razor / EncryptSecretPage.razor   ← AdminOnly
│       │   └── Dialogs/UserFormDialog.razor
│       ├── Ops/
│       │   ├── HealthPage.razor / AlertsPage.razor / QueuesPage.razor
│       │   ├── LogsPage.razor / DataRawLogPage.razor / StorePage.razor / PosMapPage.razor
│       │   ├── PosDataSetupPage.razor         ← /ops/pos-data-setup — CRUD cấu hình POS
│       │   ├── PosTerminalSavePayload.cs      ← shared record: payload chain PosMapPage→DetailDialog→EditDialog
│       │   └── Dialogs/ (PosTerminalDetailDialog, PosTerminalEditDialog, StoreDetailDialog,
│       │                  PosDataSetupFormDialog)
│       ├── Catalog/
│       │   └── Product/
│       │       ├── ProductsPage.razor               ← /catalog/products — danh sách + thêm mới + xuất Excel
│       │       ├── ProductLockPage.razor             ← /catalog/product-lock — khóa/mở khóa SP theo cửa hàng
│       │       └── Dialogs/ (ProductDetailDialog — form tạo SP mới, dynamic barcode rows)
│       │   └── Price/
│       │       ├── PricesPage.razor                   ← /catalog/prices — 9.1 Danh mục Bảng giá (list + filter + Export)
│       │       ├── PriceSetupPage.razor               ← /catalog/price-setup — 9.3 Setup giá Bulk Import (import Excel + lưới preview)
│       │       └── Dialogs/ (PriceItemPickerDialog — tìm & chọn SP thêm dòng)
│       ├── Promotion/
│       │   ├── Offers/
│       │   │   ├── PromotionSetupPage.razor   ← /promotion/setup — Cài đặt CTKM (header cố định ngoài 4 tab: Thông tin chung=bảng lịch giờ/Mon-Sun, Sản phẩm mua/khuyến mãi=bulk-add+ScaleType+MinValue/TotalDiscount, Cửa hàng áp dụng, Cài đặt nâng cao=voucher delay+MemberCode autocomplete — khớp 100% field legacy SetupMain.cshtml)
│       │   │   ├── SpecialComboPage.razor      ← /promotion/special-combo — Special Combo
│       │   │   ├── OffersPage.razor            ← /promotion/offers — Danh mục khuyến mãi (Offer* live)
│       │   │   └── Dialogs/ (SiteGroupSetupDialog — modal "Cài đặt nhóm cửa hàng": tạo mới nhóm CH/ST + danh sách filter/phân trang/xem chi tiết store/chọn gắn vào CTKM; ItemGroupSetupDialog — modal "Cài đặt nhóm sản phẩm" cho dòng Buy/Get "Nhóm SP": tạo mới nhóm + danh sách filter/phân trang/xem chi tiết sản phẩm/chọn gắn vào dòng)
│       │   └── CouponVoucher/
│       │       ├── CouponsPage.razor / CouponIssuePage.razor        ← 8.1/8.2 Coupon (list+xóa / phát hành Auto·Import — 1 form gộp đủ field, không qua dialog nâng cao)
│       │       ├── VouchersPage.razor                                ← 8.3 Danh mục Voucher (list + CRUD + Export)
│       │       ├── VouchersPublishedPage.razor                       ← 8.4 Tra cứu Voucher phát hành (CentralSales per-store)
│       │       └── Dialogs/ (CouponItemPickerDialog, VoucherFormDialog, VoucherItemPickerDialog,
│       │                      CouponAdvancedDialog — không còn dùng, giữ code cho thiết kế lại sau)
│       └── Store/
│           ├── Reports/ (Revenue, DetailRevenue, RevenueHourly, PaymentBreakdown, SalesByCategory, TopProduct, Loyalty)
│           ├── Transactions/ (TransactionsPage, RefundsPage, VoidsPage)
│           ├── Operations/ (BusinessDayPage, EosShiftsPage, ShiftSummaryPage)
│           └── Dialogs/ (VoidDetailDialog, TransactionDetailDialog, EosDayShiftListDialog, EosShiftDetailDialog, ProductOrdersDialog)
├── Services/
│   ├── ISqlConsoleService.cs / SqlConsoleService.cs / PendingUpdate.cs / JsDownloadExtensions.cs
│   └── Pdf/ (IPdfExportService, PdfExportService, PivotReportData, ReportHeaderModel)
├── Properties/launchSettings.json
├── wwwroot/
│   ├── app.css          ← CSS design tokens --pos-* + .pos-table* (pivot report) ; js/download.js (PDF blob)
│   ├── favicon.png / lib/bootstrap/ (template, chưa xóa)
├── appsettings.json / .Development.json / .Production.json / .UAT.json(gitignored)
├── Dockerfile
├── POS.Web.csproj
└── Program.cs          ← security config-driven (Security:Mode/RequireHttps), headers/CSP, decryption hook
```

---

## Kết quả kiểm tra

| # | Hạng mục | File | Trạng thái | Vấn đề (nếu có) |
|---|----------|------|-----------|-----------------|
| A1 | Project file – target framework | POS.Web.csproj | ✅ | net10.0 |
| A2 | Project ref – POS.Infrastructure | POS.Web.csproj | ✅ | |
| A3 | Project ref – POS.Application | POS.Web.csproj | ✅ | |
| A4 | Project ref – POS.Common | POS.Web.csproj | ✅ | |
| A5 | Package MudBlazor | POS.Web.csproj | ✅ | 9.5.0 |
| A6 | Package BCrypt.Net-Next | POS.Web.csproj | ✅ | 4.2.0 |
| A7 | Package Newtonsoft.Json | POS.Web.csproj | ✅ | 13.0.4 |
| A8 | Package Microsoft.AspNetCore.Components.Authorization | POS.Web.csproj | ✅ | Không cần — built-in .NET 10, bỏ đúng để tránh NU1510 |
| B1 | WebRoles + WebPolicies (3 const mỗi loại) | Auth/WebRoles.cs | ✅ | StoreOperator, ITOps, SystemAdmin / StoreAndAbove, OpsAndAbove, AdminOnly |
| B2 | DashboardUser model (7 fields) | Auth/DashboardUser.cs | ✅ | Id, Username, PasswordHash, FullName, Role, StoreCodes?, IsActive |
| B3 | IWebUserService (8 methods) | Auth/IWebUserService.cs | ✅ | ValidateLoginAsync, GetByUsernameAsync, GetStoreCodes, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync (soft), ActivateAsync, UsernameExistsAsync |
| B4 | WebUserService – inject CentralMDConnectionFactory (concrete) | Auth/WebUserService.cs | ✅ | Primary constructor injection, không qua interface |
| B5 | WebUserService – inject IFileLogHelper | Auth/WebUserService.cs | ✅ | |
| B6 | WebUserService – BCrypt.Verify | Auth/WebUserService.cs | ✅ | `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)` |
| B7 | WebUserService – GetStoreCodes JSON deserialize | Auth/WebUserService.cs | ✅ | `JsonConvert.DeserializeObject<List<string>>(user.StoreCodes)` |
| B8 | SQL migration – CREATE TABLE DashboardUsers | Auth/migration_dashboard_users.sql | ✅ | IF NOT EXISTS, đủ cột, UNIQUE constraint trên Username |
| B9 | SQL migration – seed admin | Auth/migration_dashboard_users.sql | ⚠️ | Seed tồn tại nhưng `HASH_PLACEHOLDER` chưa được thay bằng BCrypt hash thật |
| C1 | appsettings – ConnectionStrings (CentralMD, Loyalty, StagingDB) | appsettings.json | ✅ | 3 key có mặt + thêm: Partner, EInvoice, IFSAP, CentralSale |
| C2 | appsettings – Redis (Mode, SentinelHosts, MasterName, DefaultDatabase) | appsettings.json | ✅ | Mode=StandAlone, DefaultDatabase=2 |
| C3 | appsettings – RabbitMQ (Host, Port, Username, Password) | appsettings.json | ✅ | |
| C4 | appsettings – Elasticsearch (Nodes, IndexFormat) | appsettings.json | ✅ | IndexFormat=`pos-web-logs-{0:yyyy.MM.dd}` |
| C5 | appsettings – Logging.FileLogDirectory | appsettings.json | ✅ | `D:\\ROOT\\Logs\\POS.Web` |
| C6 | appsettings – WebApp (AppName, SessionTimeoutHours) | appsettings.json | ✅ | AppName="POS Dashboard – WinMart", SessionTimeoutHours=8 |
| D1 | Program – AddMudServices() | Program.cs | ✅ | SnackbarConfiguration.PositionClass = BottomRight |
| D2 | Program – AddRazorComponents().AddInteractiveServerComponents() | Program.cs | ✅ | |
| D3 | Program – AddInfrastructure(builder.Configuration) | Program.cs | ✅ | |
| D4 | Program – AddApplication() | Program.cs | ✅ | |
| D5 | Program – AddScoped\<IWebUserService, WebUserService\>() | Program.cs | ✅ | |
| D6 | Program – Cookie authentication | Program.cs | ✅ | LoginPath=/login, SlidingExpiration, HttpOnly, **SameSite=Lax** (đổi từ Strict để fix Safari iOS) |
| D7 | Program – 3 policy (StoreAndAbove, OpsAndAbove, AdminOnly) | Program.cs | ✅ | |
| D8 | Program – AddCascadingAuthenticationState() | Program.cs | ✅ | |
| D9 | Program – Middleware order + explicit UseRouting() | Program.cs | ✅ | Host-rewrite → **UseRouting() tường minh** → UseAuthentication → UseAuthorization → UseAntiforgery → MapStaticAssets → MapRazorComponents |
| D10 | Dockerfile – DataProtection-Keys ownership | Dockerfile | ✅ | `mkdir -p + chown app:app` TRƯỚC `USER $APP_UID` |
| D11 | nginx config – WebSocket + Host passthrough | nginx | ✅ | proxy_set_header Upgrade + Connection + proxy_read_timeout 300s |
| D10 | Program – MapGet("/logout", ...) | Program.cs | ✅ | SignOutAsync + Redirect("/login") + AllowAnonymous |
| E1 | App.razor – MudBlazor.min.css | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.css` |
| E2 | App.razor – MudBlazor.min.js | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.js` |
| E3 | App.razor – \<Routes/\> component | Components/App.razor | ✅ | `@rendermode="InteractiveServer"` |
| E4 | App.razor – Google Fonts Roboto | Components/App.razor | ✅ | `fonts.googleapis.com/css?family=Roboto` |
| E5 | Routes.razor – AuthorizeRouteView (không phải RouteView) | Components/Routes.razor | ✅ | DefaultLayout = MainLayout |
| E6 | Routes.razor – NotAuthorized: kiểm tra IsAuthenticated | Components/Routes.razor | ✅ | `context.User.Identity?.IsAuthenticated != true` |
| E7 | RedirectToLogin component | Components/RedirectToLogin.razor | ✅ | NavigateTo("/login", forceLoad:true) |
| E8 | RedirectToAccessDenied component | Components/RedirectToAccessDenied.razor | ✅ | NavigateTo("/access-denied", forceLoad:true) |
| F0 | PosTheme.cs – custom MudTheme (navy primary, teal accent, semantic colors) | Theme/PosTheme.cs | ✅ | Primary=#2051A3, **Drawer/Appbar=#FFFFFF** (v2 — đổi từ navy #1B3A5C, chữ tint navy), **BorderRadius=16px** (v2 — tăng từ 4px), **LineHeight=1.45**, Button.TextTransform=none, **Body1=0.75rem + FontWeight=400** (v2 — giảm ~15% từ 0.875rem, không đậm), **Shadows E1-E5="none"** (v2 — borderless, thay hairline `0 0 0 1px`; E6+ giữ nguyên cho dropdown/dialog) |
| F1 | MainLayout – MudThemeProvider **Theme="@PosTheme.Default"** + providers | Layout/MainLayout.razor | ✅ | Đã truyền custom theme |
| F2 | MainLayout – MudAppBar: toggle drawer + hiển thị tên user + logout | Layout/MainLayout.razor | ✅ | Href="/logout" trên MudIconButton. **v2:** `Color="Color.Default"` (đổi từ `Color.Primary`) để ăn theo AppbarBackground sáng; title đổi "POS Dashboard – POSMaster"→"RPOS Dashboard"; icon Menu/Logout đổi `Filled`→`Outlined` |
| F3 | MainLayout – Sidebar "Cửa hàng" (Policy=StoreAndAbove) | Layout/MainLayout.razor | ✅ | 3 sub-group (Vận hành/Giao dịch/Báo cáo) + 12 leaf links — icon sub-group (cấp 2) đổi đồng nhất về `ChevronRight` giống leaf (cấp 3), `HideExpandIcon="true"` ẩn mũi tên expand mặc định bên phải |
| F4 | MainLayout – Sidebar "Vận hành" (Policy=OpsAndAbove) | Layout/MainLayout.razor | ✅ | 2 sub-group: Giám sát (4 links) + Nhật ký (2 links) — icon sub-group `ChevronRight` giống leaf, `HideExpandIcon="true"` |
| F5 | MainLayout – Sidebar "Quản trị" (Policy=AdminOnly) | Layout/MainLayout.razor | ✅ | 4 nav link, `HideExpandIcon="true"` |
| F6 | EmptyLayout – layout căn giữa cho Login | Layout/EmptyLayout.razor | ✅ | flex + align-items:center + **background:var(--mud-palette-background)** (không còn hardcode #f0f2f5), có MudBlazor providers + PosTheme |
| G1 | Login.razor – @page "/login" | Pages/Login.razor | ✅ | |
| G2 | Login.razor – @layout Layout.EmptyLayout | Pages/Login.razor | ✅ | |
| G3 | Login.razor – @attribute [AllowAnonymous] | Pages/Login.razor | ✅ | |
| G4 | Login.razor – @rendermode InteractiveServer | Pages/Login.razor | ✅ | |
| G5 | Login.razor – MudTextField username + password | Pages/Login.razor | ✅ | Password có toggle show/hide (Adornment.End pattern đúng MudBlazor 9.x) |
| G6 | Login.razor – DoLogin gọi ValidateLoginAsync | Pages/Login.razor | ✅ | |
| G7 | Login.razor – DoLogin tạo ClaimsPrincipal + gọi SignInAsync | Pages/Login.razor | ✅ | Claims: Name, Role, full_name, store_codes |
| G8 | Index.razor – @page "/" + [Authorize] | Pages/Index.razor | ✅ | |
| G9 | Index.razor – redirect theo role | Pages/Index.razor | ✅ | SystemAdmin→/admin/users, ITOps→/ops/health, other→/store/revenue |
| G10 | RevenuePage – /store/revenue + StoreAndAbove + InteractiveServer | Pages/Store/RevenuePage.razor | ✅ | |
| G11 | HealthPage – /ops/health + OpsAndAbove + InteractiveServer | Pages/Ops/HealthPage.razor | ✅ | |
| G12 | UsersPage – /admin/users + AdminOnly + InteractiveServer | Pages/Admin/UsersPage.razor | ✅ | KPI row (3 cards: tổng/active/locked) + filter panel (search+role+status) + MudTable LINQ filter |
| G13 | AccessDenied – /access-denied + [AllowAnonymous] | Pages/AccessDenied.razor | ✅ | |
| G14 | TransactionsPage – /store/transactions + StoreAndAbove | Pages/Store/TransactionsPage.razor | ✅ | MudTable client-side sort/paginate + store combobox (StoreNo+Name) |
| G15 | EosShiftsPage – /store/eos-shifts + StoreAndAbove | Pages/Store/EosShiftsPage.razor | ✅ | Kết thúc ca — filter + KPI cards + MudTable + GetEosShiftListAsync |
| G16 | DetailRevenuePage – /store/revenue-detail + StoreAndAbove | Pages/Store/DetailRevenuePage.razor | ✅ | Báo cáo doanh thu chi tiết — 11 filters + 21-col MudTable ServerData (server-side paging) |
| G17 | BusinessDayPage – /store/business-day + StoreAndAbove | Pages/Store/Operations/BusinessDayPage.razor | ✅ | "Xác nhận kết thúc ngày" — port từ legacy StoreActivitiesController. Chọn 1 store (bắt buộc, mặc định store đầu theo StoreNo) + ngày kinh doanh, bấm Tìm kiếm mới load; lưới per-POS-terminal (staging) + nút Xác nhận (chặn nếu còn POS chưa đóng ngày hoặc đã xác nhận) → advance `BussinessDateOpen` +1 ngày cho máy POS |
| G18 | ShiftSummaryPage – /store/shift-summary + StoreAndAbove | Pages/Store/ShiftSummaryPage.razor | ✅ | Stub — Tổng kết ca (UI construction in progress) |
| G19 | RefundsPage – /store/refunds + StoreAndAbove | Pages/Store/RefundsPage.razor | ✅ | Stub — Hoàn trả (UI construction in progress) |
| G20 | VoidsPage – /store/voids + StoreAndAbove | Pages/Store/VoidsPage.razor | ✅ | Stub — Hủy GD (UI construction in progress) |
| G21 | RevenueHourlyPage – /store/revenue-hourly + StoreAndAbove | Pages/Store/RevenueHourlyPage.razor | ✅ | Doanh thu theo giờ — KPI + Line/Bar charts + MudTable (FooterContent dòng Tổng) + store combobox. **Tối ưu 10M dòng:** Redis cache repo (TTL theo độ mới) + includeKpi + CancellationToken + guard re-entrancy + hoãn load khỏi prerender + clamp 92 ngày khi all-stores |
| G22 | PaymentBreakdownPage – /store/payment-breakdown + StoreAndAbove | Pages/Store/PaymentBreakdownPage.razor | ✅ | Stub — Phân tích thanh toán (UI construction in progress) |
| G23 | TopProductPage – /store/top-product + StoreAndAbove | Pages/Store/TopProductPage.razor | ✅ | Top sản phẩm bán chạy — sp_ReportTopProduct (cache Pattern 4, clamp 92 ngày). KPI 3 card + CSS bar list + MudTable drill-through (ProductOrdersDialog). **BA/BI:** surface metrics (trả%/giá TB/độ phủ/giảm%) + so sánh cấp SP (Δ hạng/NEW). Ngành hàng ẩn tạm (SP chưa JOIN Item master) |
| I1 | DataTable standard – `MudTable<T>` built-in | (mọi page có bảng) | ✅ | MudTableSortLabel + MudTablePager + ServerData; PosTableBase ĐÃ XÓA. Chi tiết: `.claude/skills/web/datatable.md` |
| I2 | `.pos-table*` CSS – nay chỉ cho pivot report | wwwroot/app.css | ✅ | pos-table/pos-table-wrap còn dùng cho `rpt-pivot-table` (SalesByCategoryPage) |
| I3 | Sidebar accordion – tự mở/đóng theo route | Layout/MainLayout.razor | ✅ | NavigationManager.LocationChanged + @bind-Expanded + IAsyncDisposable; mọi `MudNavGroup` (cấp 1+2) thêm `HideExpandIcon="true"` ẩn mũi tên expand phải, chỉ còn icon trái làm chỉ báo |
| I4 | Sidebar active NavLink highlight | wwwroot/app.css | ✅ | rgba(255,255,255,0.14) bg + white text + 3px border-left #3A6FCC |
| I5 | Sidebar drawer responsive init — đóng trên mobile, mở trên desktop | Layout/MainLayout.razor | ✅ | IBrowserViewportService.GetCurrentBreakpointAsync() trong OnAfterRenderAsync(firstRender) |
| I6 | MudTable header CSS override toàn cục | wwwroot/app.css | ✅ | Nền `--pos-bg-alt` (#D9E5F7), border-bottom 2px navy, padding 10px 16px, sort button min-height:unset padding:0 — áp dụng tất cả MudTable không cần sửa Razor |
| I7 | Sort label cột đặc biệt | datatable.md | ✅ | Nullable DateTime → `?? DateTime.MinValue`; string date → dùng `SortOrder` int property |
| I8 | Filter panel Elevation chuẩn | (mọi page có filter) | ✅ | `MudPaper Elevation="1"` cho filter panel, `Elevation="2"` cho DataTable. **v2:** thêm class `pos-filter-panel` (nền soft-tint `--pos-primary-bg`) — đã áp dụng 9 page menu "Danh mục" |
| I9 | Không có result summary text inline | (mọi page có table) | ✅ | Xóa `@if (!_loading && _items.Count > 0) { <div>Tìm thấy X dòng</div> }` — KPI cards thay thế |
| I6 | Page header responsive — title+button không vỡ layout mobile | Pages/Admin/UsersPage.razor | ✅ | div.pos-page-header + pos-page-header-title + pos-page-header-btn |
| I7 | DataTable scroll ngang trên mobile | mọi page có MudTable | ✅ | `HorizontalScrollbar="true"` trên MudTable (thay wrapper overflow-x:auto cũ) |
| I8 | Chip filter flex-wrap — chips không tràn ngang mobile | Pages/Store/RevenuePage.razor | ✅ | flex-wrap thêm vào MudPaper filter container |
| I9 | Summary text flex-wrap — &nbsp;\|&nbsp; đổi sang flex items | Pages/Store/TransactionsPage.razor | ✅ | d-flex flex-wrap gap-3 thay separator |
| I10 | HealthPage responsive — header + chip section | Pages/Ops/HealthPage.razor | ✅ | pos-page-header Case B (title + group controls); chip div.d-flex flex-wrap; button align-self:center chống stretch |
| I11 | Responsive UI standard — qui tắc chung mọi page | .claude/skills/web/SKILLS.md | ✅ | Section mới: bảng so sánh sai/đúng, 2 case pos-page-header, anti-patterns, checklist item |
| I12 | RevenuePage – Y-axis auto-scale (`YAxisSuggestedMax` + `YAxisTicks`) | Pages/Store/RevenuePage.razor | ✅ | CalcYMax (dataMax+2.5 ceil) + CalcYTick (spacing 1/2/5/10) — hết cứng max=20 |
| S1 | DetailedErrors tắt ngoài Dev (C2) | appsettings.Production/UAT.json | ✅ | `EnableDetailedErrors:false`; Program đọc `IsDev() || config` |
| S2 | Cookie.Secure + HTTPS/HSTS config-driven (C1) | Program.cs / appsettings | ✅ (cơ chế) ⚠️ (đang tắt) | `Security:RequireHttps` (Prod=false để test HTTP) → cookie SameAsRequest. Bật `true` khi có TLS. SameSite=Strict (Mode=Internet) |
| S3 | Security headers + CSP (M1) | Program.cs | ✅ | X-Content-Type-Options/X-Frame-Options/Referrer-Policy/CSP; `frame-src 'self' blob:` cho PDF; TẮT ở Dev (`EnableSecurityHeaders=false`) tránh chặn Browser Link |
| S4 | ForwardedHeaders an toàn (H2) | Program.cs | ✅ | Mode=Internet → KHÔNG xử lý `X-Forwarded-*` (no-proxy). BehindProxy mới nạp `KnownProxies`/`KnownNetworks` |
| S5 | Mã hóa credentials appsettings (C4) — POS.Web + POS.Api | SecretProtector.cs + Program.cs (2 project) + EncryptSecretPage.razor | ✅ (cơ chế) ✅ (đã rollout Production) | AES-256-GCM token `enc:`, khóa chung `POS_SECRET_KEY`; hook giải mã wired ở CẢ POS.Api và POS.Web; trang `/admin/encrypt-secret` (POS.Web) sinh token cho cả 2. `appsettings.Production.json` của cả 2 project đã mã hóa (9 connection string + RabbitMQ); key đã ghi vào `.env` local — **UAT/PROD server thật vẫn cần tự set `POS_SECRET_KEY`**. Tra cứu nhanh: `docs/architecture/appsetting.md`; chi tiết: `docs/ROLLOUT.md` |
| S6 | SQL Console hardening (H1) | SqlConsoleService.cs / SqlConsolePage.razor | ✅ | Mask `password/token/secret/...` trong audit+Kibana; cờ `Security:EnableSqlConsole` gate service+page |
| S7 | AllowedHosts = domain thật (H2) | appsettings.Production.json | ⚠️ | Còn `"*"` — cần đặt domain dashboard khi go-live (docs/ROLLOUT.md) |
| G24 | PosDataSetupPage – /ops/pos-data-setup + OpsAndAbove | Pages/Ops/PosDataSetupPage.razor | ✅ | CRUD cấu hình POS — KPI 3 cards (pre-computed) + filter panel + MudTable + Add/Edit dialog; Redis invalidate sau mỗi write |
| J1 | IAuditLogger / DbAuditLogger — audit CRUD vào DashboardAuditLog | Auth/IAuditLogger.cs | ✅ | LogAsync(actor, action, entityType, entityKey, oldValueJson?, newValueJson?); ghi DB + Kibana; try/catch nội bộ; đăng ký Scoped trong Program.cs |
| J2 | PosDataSetupFormDialog – Add/Edit form, trả DTO đầy đủ | Pages/Ops/Dialogs/PosDataSetupFormDialog.razor | ✅ | Code read-only khi Edit; trả DialogResult.Ok(_model) (không Ok(true)) để page có newValue; duplicate Code → thông báo thân thiện |
| J3 | migration_dashboard_audit_log.sql – bảng DashboardAuditLog + 3 index | Auth/migration_dashboard_audit_log.sql | ⚠️ | Script idempotent — **PHẢI CHẠY trên RPOSMasterData trước deploy**; chưa chạy → log fail silently |
| J4 | audit-logging.md – rule audit CRUD chuẩn hóa cho toàn dự án | .claude/skills/web/audit-logging.md | ✅ | Pattern: snapshot oldValue từ item đã có, await LogAsync sau DB success, dialog trả DTO, checklist 12 mục |
| K1 | ProductsPage – /catalog/products + OpsAndAbove | Pages/Catalog/Product/ProductsPage.razor | ✅ | Danh sách SP/Barcode — SP GetProductList server-side paging; filter (mã/tên/barcode/thuế suất); nút Thêm mới + dialog tạo SP; Export Excel (ClosedXML); pos-page-header. Migrate 6.1+6.2+6.3 |
| K2 | ProductDetailDialog – form tạo sản phẩm mới | Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor | ✅ | 8 field (ItemName/Full/UoM/SalesUoM/FamilyCode/TaxCode/Blocked/BlockedVINID) + dynamic barcode table; INSERT dbo.Item + dbo.Barcode trong transaction; auto ItemNo (Max+1). Edit button disabled pending UPDATE route |
| K3 | ProductLockPage – /catalog/product-lock + OpsAndAbove | Pages/Catalog/Product/ProductLockPage.razor | ✅ | Khóa/mở khóa SP theo cửa hàng — StoreNo bắt buộc; MudTable server-side + MultiSelection + chip màu; toggle đơn + bulk action; MudMessageBox @ref confirm; UPSERT dbo.ItemBlock. Migrate 6.4 (Central mode) |
| J5 | IKibanaService → IFileLogHelper — migration toàn POS.Web | 24 .razor + 3 .cs (PendingUpdate, SqlConsoleService, DbAuditLogger) | ✅ | LogInfo → WriteLogs(`[{fn}] {entity}: {msg}`); LogException có ex → WriteExpLogs; LogException không có ex → WriteLogs(`[EXCEPTION][{fn}] msg`) |
| J6 | Audit log UsersPage (CREATE/UPDATE/LOCK/UNLOCK) + PosMapPage (UPDATE PosTerminal, chained dialog) | UsersPage.razor / UserFormDialog.razor / PosMapPage.razor / PosTerminalEditDialog.razor / PosTerminalDetailDialog.razor / PosTerminalSavePayload.cs (mới) | ✅ | UserFormDialog trả DTO đầy đủ (PasswordHash masked); DetailDialog forward result.Data!; PosMapPage capture oldJson trước dialog |
| K4 | PricesPage – /catalog/prices + OpsAndAbove | Pages/Catalog/Price/PricesPage.razor | ✅ | 9.1 Danh mục Bảng giá — reuse SP `GetSalesPriceList`/`_Export` (Dapper server-side paging); filter mã/tên + combobox "Hình thức bán hàng"/"Nhóm giá" (reuse `GetSetupLookupAsync`) + "Còn hiệu lực" (mặc định off); cột Hình thức + Trạng thái (MudChip); format nghìn khi sửa giá; Sửa/Xóa định vị bằng `SalesGroupCode`+`SalesTypeCode` (mã gốc, không dùng cột hiển thị); Export Excel (ClosedXML); pos-page-header. Migrate 9.1 (2026-07-06: fix bug Sửa/Xóa sai dòng) |
| K5 | PriceSetupPage + PriceItemPickerDialog – /catalog/price-setup + OpsAndAbove | Pages/Catalog/Price/PriceSetupPage.razor + Dialogs/PriceItemPickerDialog.razor | ✅ | 9.3 Setup giá (streamlined) — chọn Hình thức bán + cửa hàng → import Excel (MudFileUpload+ClosedXML) → ValidateImportAsync → lưới preview MudTable sửa inline (giá/ngày) + RowStyleFunc highlight lỗi + item picker thêm dòng → Lưu (block khi còn lỗi) + audit log. SP mới `usp_SetupSalePrice_Save` (TVP, ủy quyền Setup_SalePrice_Get_ALL). Migrate 9.3 |
| H1 | Build pass (0 error, 14 warning pre-existing) | — | ✅ | `dotnet build POS.Web` → Build succeeded. 0 Error(s). ContractTests 23/23 pass (DI validation xanh). |

---

## Tóm tắt

- ✅ Hoàn thành: **92 / 94 hạng mục**
- ⚠️ Có vấn đề: **2 hạng mục** (B9 — SQL seed hash placeholder; J3 — migration chưa chạy trên DB)
- ❌ Còn thiếu: **0 hạng mục**

> +2 hạng mục mới (session 2026-07-01): K4 (PricesPage 9.1), K5 (PriceSetupPage 9.3 + PriceItemPickerDialog). ⚠️ SP `docs/sql/SetupSalePrice_Save.sql` phải chạy trên RPOSMasterData trước khi dùng 9.3.
> +3 hạng mục (session 2026-06-30): K1 (ProductsPage 6.1+6.2+6.3), K2 (ProductDetailDialog), K3 (ProductLockPage 6.4).
> Previous +2 (session 2026-06-28 Phase1+2): J5, J6. Previous +5: G24, J1-J4. Previous: S1-S7, G16-G23, I1-I12.

---

## Các vấn đề cần xử lý

### 🟡 Cần bổ sung trước khi chạy SQL migration
**B9 — SQL seed có HASH_PLACEHOLDER**
File: `src/POS.Web/Auth/migration_dashboard_users.sql`

```sql
-- Thay HASH_PLACEHOLDER bằng hash thật, ví dụ trong C#:
-- string hash = BCrypt.Net.BCrypt.HashPassword("Admin@2024!");
INSERT INTO DashboardUsers (Username, PasswordHash, ...)
VALUES ('admin', 'HASH_PLACEHOLDER', ...)   -- ← CHƯA THAY
```
→ Chạy đoạn C# để sinh hash, copy vào file SQL trước khi execute.

### 🟢 Quan sát bổ sung (không ảnh hưởng app)
- **wwwroot/lib/bootstrap/**: ~30 file CSS Bootstrap từ template vẫn còn trong `wwwroot/`. Không được reference (dùng MudBlazor CDN), không gây lỗi, nhưng chiếm dung lượng. Có thể xóa lúc cleanup.
- **ReconnectModal.razor**: File từ template gốc, được giữ vì `App.razor` dùng `<ReconnectModal/>`. Không cần sửa.

### 🟢 Đã xong — không cần làm thêm
Tất cả 54 hạng mục còn lại: Project references, Auth layer, Configuration, Program.cs pipeline, Blazor root components, Layouts, tất cả Pages, Build.

---

## Build output

```
dotnet build src/POS.Web/POS.Web.csproj

  POS.Common        → bin/Debug/net10.0/POS.Common.dll
  POS.Infrastructure → bin/Debug/net10.0/POS.Infrastructure.dll
  POS.Application   → bin/Debug/net10.0/POS.Application.dll
  POS.Web           → bin/Debug/net10.0/POS.Web.dll

Build succeeded.
    3 Warning(s) — MUD0002 Title pre-existing (VoidsPage + TransactionsPage ×2)
    0 Error(s)

Time Elapsed — [2026-06-28 after Phase1+2: IKibanaService→FileLogger migration + audit UsersPage/PosMapPage]
```

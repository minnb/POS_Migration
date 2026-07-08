# POS.Web — Báo cáo hiện trạng
> Cập nhật: 2026-07-08 (Fix WebSocket SignalR bị rớt qua subdomain HTTPS: xác định root cause là
> tầng SSL vhost NGOÀI repo thiếu forward `Upgrade`/`Connection` header khi POS.Web chạy sau 2 tầng
> reverse proxy — nginx trong repo (`pos-web.conf`/`pos-web.uat.conf`) đã đúng chuẩn, không cần sửa.
> Đã sửa `appsettings.UAT.json`: `Security:Mode` `Internet`→`BehindProxy` cho khớp topology thật.
> Checklist vá tầng ngoài + lệnh verify: `docs/ROLLOUT.md` §O7. **CHƯA VERIFY trên server thật**
> (không có quyền SSH production/UAT) — cần người vận hành tự áp dụng và xác nhận bằng F12 Network
> tab thấy `_blazor` status `101`. Chi tiết `docs/CHANGELOG.md`.)
> Trước đó 2026-07-08 (Serilog reconfig riêng POS.Web — giảm phình log Production: bỏ hardcode
> `MinimumLevel` trong `SerilogConfiguration.cs` (dùng chung Api/Web/Worker) để `Serilog:MinimumLevel`
> trong appsettings có hiệu lực thật; POS.Web Dev = `Warning`, Production = `Error`
> (`Microsoft`/`System` cũng `Error`, giữ `Microsoft.Hosting.Lifetime=Information`). Không đổi
> POS.Api/POS.Worker. Thêm capability mới `IKibanaService.LogException(..., Exception ex, context)`
> — structured `{@Context}`, tự mask field nhạy cảm qua `SensitiveDataMasker` mới (không migrate
> ~150 call site `WriteExpLogs` hiện có, out of scope). Verify: `dotnet build` POS.Web + solution 0
> lỗi, `dotnet test tests/POS.ContractTests` 39/39, verify thủ công qua console app tạm xác nhận
> log Info/Warning bị lọc, log Error lọt qua, và `Password`/`Pin` trong context bị mask `"***"`
> trong khi `StoreNo` giữ nguyên. Chi tiết `docs/CHANGELOG.md`.)
> Trước đó 2026-07-08 (Thêm `RedisDashboardPage` — /ops/redis (OpsAndAbove): tìm key theo pattern
> (SCAN cap 1000, chặn `*` cần confirm), xem giá trị (pretty JSON), xóa key (confirm + audit log),
> status card + 5 KPI card (Bộ nhớ/Clients/Tổng Key/Cache Hit %/Uptime) kiểu `HealthPage.razor`.
> Backend mới: `IRedisManagementService` (POS.Application.Features.Redis), mở rộng `IRedisManager`
> với `PingAsync`/`GetServerInfoAsync`/`GetDbSizeAsync`/`GetKeysByPatternAsync(pattern,maxResults)`/
> `GetKeyTtlSecondsAsync`/`GetKeyTypeAsync`/`GetKeyRawValueAsync` (POS.Infrastructure.Cache), 4 DTO
> mới `RedisKeyInfoDto`/`RedisKeyValueDto`/`RedisKeySearchResultDto`/`RedisServerStatusDto`
> (POS.Common.Dtos.Redis). Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests`
> 25/25. Chưa verify UI thật (sandbox không có Redis/DB thật) — cần tự `dotnet run` kiểm tra. Chi
> tiết: `docs/CHANGELOG.md`.)
> Trước đó 2026-07-08 (`DataRawLogPage` — fix Retry văng "network-related... SQL Server" trên
> UAT/Prod: `CentralSaleRepository.InInsertToTableByJson` đổi từ `StoreRoutedConnectionFactory`
> (route theo `StoreSetServer`, ServerIP 1 số store không còn kết nối được) sang
> `directConnectionFactory` cố định — khớp với các hàm đọc log vốn đã dùng connection cố định.
> Áp dụng cho mọi caller (Web/Worker/FileImport/Kafka). Thêm `CancellationTokenSource(100s)` +
> catch `OperationCanceledException` riêng cho Retry. Verify: `dotnet build` POS.Infrastructure 0
> lỗi, `dotnet test tests/POS.ContractTests` 25/25 (build POS.Web bị khóa file bởi instance đang
> chạy, chưa build lại được — cần verify lại). Chi tiết: `docs/CHANGELOG.md`.)
> Trước đó 2026-07-08 (Fix production logging: `Program.cs` thiếu `builder.AddSerilogWithElastic()`
> khiến toàn bộ `ILogger<T>`/`KibanaService` không ghi được vào `Logging:FileLogDirectory` hay
> Elasticsearch — chỉ ra Console. Đã thêm dòng gọi đúng vị trí như POS.Api. Chi tiết
> `docs/CHANGELOG.md`, pattern ghi ở `.claude/skills/web/deployment.md`.)
> Trước đó 2026-07-08 (VoidsPage — fix lỗi SQL reserved keyword `LineNo` khiến trang luôn rỗng +
> đồng bộ UI theo chuẩn `PosMapPage.razor`, xem G20. Chi tiết `docs/CHANGELOG.md`.)
> Trước đó 2026-07-07 (Dashboard mặc định cho role Cửa hàng `/store/dashboard` — landing page mới
> thay `/store/revenue` cho StoreOperator, xem G24. Kèm resolve git-conflict marker tồn đọng trong
> file này + `docs/CHANGELOG.md` — xem chi tiết `docs/CHANGELOG.md`.)
> Trước đó 2026-07-06 (Đổi ngữ nghĩa `IsCheckItem` trên `VoucherIssuePage.razor`/`VouchersPage.razor`
> `/promotion/vouchers*`: sau điều tra xác nhận code cũ khớp đúng legacy nhưng NGƯỢC nghĩa Coupon —
> theo quyết định người dùng, đổi Voucher khớp Coupon (`IsCheckItem=1`=theo sản phẩm). Đã sửa
> C#/Razor + 2 SP script + docs; **CHƯA chạy SP/migration data trên DB thật** — xem D10
> `docs/ROLLOUT.md`, BẮT BUỘC đúng thứ tự (deploy code → 2 SP → migration data) trước khi voucher
> cũ hiển thị đúng trên UI. Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests`
> 25/25. Chưa test UI thật trên browser vì phụ thuộc DB migration chưa chạy.)
> Trước đó 2026-07-06 (Người dùng xác nhận Lưu sản phẩm đã chạy được sau fix `usp_Product_Save`
> (TRY_CAST) — tiếp tục 2 điều chỉnh nghiệp vụ theo yêu cầu, gộp vào cùng
> `docs/sql/Product_Save.sql` (đã fix trước đó, chưa cần chạy lại thêm script riêng): (1) `ItemNo`
> tự sinh giới hạn **tối đa 8 ký tự** — seed `1000000001`→`10000001`, chỉ tính `MAX` trên `No` hiện
> có dài ≤8 ký tự; (2) `dbo.Barcodes`: `VariantCode` (trước rỗng) nay lưu cùng giá trị với
> `UnitOfMeasureCode` (ĐVT từ UI); `Pkey` (trước = BarcodeNo) nay = `"{ItemNo}-{BarcodeNo}"`. Chỉ
> sửa SQL script — không đổi code C#/Razor (`CentralMDRepository.CreateProductAsync` gọi
> `usp_Product_Save` nguyên trạng). **BẮT BUỘC chạy lại `docs/sql/Product_Save.sql` trên
> RPOSMasterData** để áp 2 thay đổi này — xem D9 `docs/ROLLOUT.md`. Chưa test lại UI sau fix (chờ
> chạy SQL).)
> Trước đó 2026-07-06 (FIX nghiêm trọng: `usp_Product_Save` chặn tạo mới MỌI sản phẩm —
> `ProductDetailDialog.razor` `/catalog/products` báo "Lỗi hệ thống" khi Lưu. Log thật
> (`D:\ROOT\Logs\POS.Web\Exception\log-20260706.txt`) xác nhận
> `SqlException: Error converting data type nvarchar to bigint` (8114) trong
> `dbo.usp_Product_Save`: bước sinh `ItemNo` tự động dùng `CAST(No AS BIGINT)` trên toàn bộ
> `dbo.Item` — chỉ cần 1 dòng `No` cũ không phải số thuần (mã hàng alphanumeric legacy) là throw,
> chặn tạo mới không phân biệt sản phẩm nào. Fix: `CAST`→`TRY_CAST` trong
> `docs/sql/Product_Save.sql` (`MAX` tự bỏ qua `NULL`). **BẮT BUỘC chạy lại script đã fix trên
> RPOSMasterData** — xem D9 `docs/ROLLOUT.md`, script idempotent, an toàn chạy đè SP cũ. Ngoài ra
> gộp 2 dropdown "Đơn vị cơ sở"/"Đơn vị bán" thành 1 "Đơn vị tính" trên UI theo yêu cầu người dùng
> (bản chất chỉ 1 UOM) — `SaveAsync` tự gán `SalesUnitOfMeasure = BaseUnitOfMeasure` trước khi gọi
> SP (không đổi contract `ProductCreateDto`/SP). Verify: `dotnet build` 0 lỗi,
> `dotnet test tests/POS.ContractTests` 25/25. Chưa test lại UI sau fix (chờ chạy SQL).)
> Trước đó 2026-07-06 (Fix hardcode `ArticleType` khi phát hành Coupon/Voucher —
> `CouponIssuePage.razor` `/promotion/coupons/issue` đã đúng `"ZCPN"` từ trước, bổ sung hardcode +
> defensive-assign ở Service cho chắc; `VoucherIssuePage.razor` `/promotion/vouchers/issue` phát
> hiện hardcode SAI `"ZTRD"` → sửa thành `"ZVCN"` đúng convention hệ thống. Verify: `dotnet build`
> 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25.)
> Trước đó 2026-07-06 (Gap Analysis + vá `OffersPage.razor` `/promotion/offers` — đối chiếu với
> legacy `PromotionController.PromotionList` (`src/legacy/VCM.BLUEPOS`), phát hiện port thiếu: (1)
> Excel export thiếu 2 cột Voucher (`VoucherFromDate`/`VoucherToDate`, DTO đã có field) + thiếu cột
> "Hình thức bán" trên lưới chính — đã thêm cả 2, không đổi DTO/Service/Repository; (2) modal "Xem
> chi tiết" 6 tab (Header/Buy/Benefits/Get/Site/Priority) — trước đó icon chỉ trang trí, không có
> logic — đã port đầy đủ: 6 DTO mới (`OfferHeaderDetailDto` ~68 field, `OfferBuyDetailLineDto`,
> `OfferGetDetailLineDto`, `OfferBenefitLineDto`, `OfferSiteLineDetailDto`, `OfferPriorityLineDto`
> trong `OfferHeaderDto.cs`), 6 method Repository (SQL Dapper trực tiếp trên `dbo.OfferHeader/
> OfferBuy/OfferGet/OfferBenefits/OfferSite/OfferPriority` — KHÔNG qua SP như lưới chính, đã tra
> đúng tên bảng/cột trong `database-schema.md`), 6 method Service tương ứng (`GetOfferSiteDetailAsync`
> map thêm `StyleProfileName`: VM→WinMart/VMP→WinMart+/FS→FlagShip/KS→Kiosk), dialog mới
> `Dialogs/OfferDetailDialog.razor` (`MudDialog`+`MudTabs`, lazy-load theo tab active — pattern mới
> ghi vào `.claude/skills/web/SKILLS.md`), export Excel riêng cho tab Buy/Get/Site. Phạm vi
> CheckPromotionList (trang "Tra cứu khuyến mãi", chưa tồn tại) hoãn sang giai đoạn sau theo quyết
> định người dùng (chỉ port nhánh SERVER, bỏ nhánh POS kết nối trực tiếp SQL máy POS — rủi ro SQL
> injection). Sau đó thêm tính năng MỚI (không có ở legacy): nút "Deactive" 1 offer LIVE — phát
> hiện & sửa mâu thuẫn quan trọng trong yêu cầu gốc (đề bài ghi "Status=0" nhưng bằng chứng
> code/doc xác nhận Status=0=Active, Status=2=Deactivated; đã xác nhận lại với người dùng trước
> khi làm), SP mới `usp_OfferHeader_Deactivate` (`docs/sql/OfferHeader_Deactivate.sql` — **chưa
> chạy trên DB thật**, cần chạy tay trên RPOSMasterData DEV) set `Status=2`+`Counter=MAX(Counter)+1`
> atomic (`UPDLOCK,HOLDLOCK`, bắt buộc để trigger delta-sync xuống POS — pattern mới ghi vào
> `.claude/skills/database/SKILLS.md`), `DeactivateOfferAsync` ở Repository/Service, confirm dialog
> `MudMessageBox @ref` chuẩn dự án; cập nhật lại invariant "Bất khả nghịch" trong
> `docs/web/logic/LOGIC_APPROVE_CTKM.md`. Cuối cùng: đổi filter mặc định khi vào trang từ "Tất cả"
> sang "Có hiệu lực". Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25.
> Chưa verify SQL 6 query detail + SP Deactive trên `RPOSMasterData` thật (không có quyền truy cập
> DB trong môi trường làm việc) — cần QA thủ công trên DEV trước khi coi là hoàn thành.)
> Trước đó 2026-07-06 (Xem chi tiết sản phẩm — thêm cột Action + nút "Xem" trên `ProductsPage`
> `/catalog/products`, mở dialog mới `ProductViewDialog.razor` (read-only): hiển thị đầy đủ field
> giống `ProductDetailDialog` (tên/ĐVT/loại hàng/thuế suất/trạng thái/tích điểm), kèm danh sách
> Barcode (Barcode, ĐVT) và ảnh sản phẩm nếu có. DTO mới `ProductDetailDto` + repository method
> `ICentralMDRepository.GetProductDetailAsync` (JOIN đọc `dbo.Item` + `dbo.Barcodes` +
> `dbo.ProductImage`, trả null nếu ItemNo không tồn tại). Vì không lưu MIME type lúc upload, dialog
> suy đoán PNG/JPEG từ magic-byte prefix của chuỗi base64 (`iVBORw0KGgo` → PNG, còn lại → JPEG) khi
> hiển thị `data:` URI — không thêm cột DB mới. Verify: `dotnet build` 0 lỗi,
> `dotnet test tests/POS.ContractTests` 25/25. Chưa test UI thật trên browser.)
> Trước đó 2026-07-06 (Thêm ảnh sản phẩm — `ProductDetailDialog.razor` `/catalog/products`: bảng
> mới `dbo.ProductImage` (ItemNo, Uom, ImageBase64 — PK ghép ItemNo+Uom, upsert), SP mới
> `dbo.usp_ProductImage_Save` (`docs/sql/ProductImage_Save.sql` — **chưa chạy trên DB thật**, cần
> chạy tay trên RPOSMasterData DEV trước khi test UI); DTO `ProductImageDto` +
> `ICentralMDRepository.SaveProductImageAsync`. UI: `MudFileUpload` chọn JPG/PNG tối đa 2MB, đọc
> vào `MemoryStream` → base64, preview `MudImage` ngay trong dialog trước khi Lưu (theo mẫu đọc
> file của `PriceSetupPage.ReadImportFileAsync`); ảnh là 1 ảnh duy nhất/sản phẩm với
> `Uom=BaseUnitOfMeasure`, lưu sau khi `CreateProductAsync` thành công, lỗi lưu ảnh không rollback
> sản phẩm đã tạo (chỉ Snackbar cảnh báo); audit log riêng "ProductImage" (chỉ ghi cờ `HasImage`,
> không ghi base64 vào `DashboardAuditLog`). Chưa có màn hình xem/sửa lại ảnh sau khi tạo (ngoài
> phạm vi — xem plan). Verify: `dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25.
> Chưa test UI thật trên browser (cần chạy SQL script trước).)
> Trước đó 2026-07-06 (Gap Analysis + vá `ProductsPage`/`ProductLockPage` `/catalog/products`,
> `/catalog/product-lock` — đối chiếu lại với legacy `ProductController.ProductList`/`ProductLock`
> (`src/legacy/VCM.BLUEPOS`), phát hiện migrate 6.30 thiếu sót. Đã vá: (1) `ProductsPage` thêm 2
> cột lưới bị thiếu dù DTO đã có field (`ItemName2`→"Tên SP (VN)", `BarcodeUnit`→"ĐVT Barcode");
> theo yêu cầu người dùng KHÔNG thêm cột `ItemNo_PLG`/`ParentCode`/`Size` (giữ nguyên, không hiển
> thị); (2) xóa nút Edit vô hiệu hóa vĩnh viễn ("chưa hỗ trợ") cùng tham số `ExistingItem`/`IsEdit`
> chết trong `ProductDetailDialog` — ProductList gốc không có Edit inline (đó là màn hình
> `UpdateArticle` riêng, ngoài phạm vi 2 action ban đầu); (3) thêm `IAuditLogger` cho
> `ProductDetailDialog` (CREATE "Product") và `ProductLockPage` (LOCK/UNLOCK "ProductLock" theo
> từng item) — trước đó 2 trang này là ngoại lệ duy nhất trong toàn bộ menu Danh mục không ghi
> audit log. Xác nhận với business 2 khoảng trống lớn nhất phát hiện trong Gap Analysis KHÔNG cần
> port: tích hợp GrabFood API (tính năng thực chất là "Block sản phẩm" ngừng bán, không phải đồng
> bộ đa kênh bán realtime kiểu GrabFood) và chế độ ghi trực tiếp CSDL máy POS qua IP terminal (Sync
> Master Data theo lịch đã đủ) — quyết định ghi tại
> `docs/web/logic/product_lock_scope_decision.md`. Verify: `dotnet build` 0 lỗi,
> `dotnet test tests/POS.ContractTests` 25/25. Chưa xác nhận trực quan trên browser thật.)
> Trước đó 2026-07-06 (FIX nghiêm trọng: `PromotionSetupPage` `/promotion/setup` — nút "Duyệt
> CTKM" trong editor publish nhầm dữ liệu nháp CŨ khi user sửa Buy/Get/Site sau lần Lưu tạm gần
> nhất rồi bấm Duyệt thẳng, không Lưu tạm lại — do nút chỉ điều kiện theo `_header.No` khác rỗng,
> không kiểm tra dữ liệu hiện tại đã lưu chưa; `ApproveAsync`/SP Duyệt không nhận Buy/Get/Site,
> chỉ publish lại đúng bảng nháp đã có sẵn. Fix: thêm `ApproveFromEditorAsync()` — nút Duyệt
> trong editor LUÔN Lưu tạm state hiện tại trước (`SaveAsync` đổi trả `Task<bool>`), chỉ Duyệt
> tiếp nếu Lưu thành công; tách `ApproveCoreAsync(bbynr)` dùng chung. Nút Duyệt nhanh ở màn danh
> sách giữ nguyên (không Lưu tạm — không có state Buy/Get/Site của đúng CTKM trong bộ nhớ trang).
> Cập nhật `docs/web/logic/LOGIC_APPROVE_CTKM.md`. Verify: `dotnet build` 0 lỗi,
> `dotnet test tests/POS.ContractTests` 25/25. Chưa test UI thật (chờ người dùng tự test lại theo
> đúng kịch bản đã gặp bug). Chi tiết: `docs/CHANGELOG.md`)
> Trước đó 2026-07-06 (Topbar/AppBar + Typography audit theo mockup `theme_html.html` —
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
> Trước đó 2026-07-06 (PricesPage `/catalog/prices` — nâng cấp 9.1 Danh mục Bảng giá: (1) thêm
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
│       │   ├── RedisDashboardPage.razor       ← /ops/redis — Redis Management Dashboard (search key theo pattern + status card/KPI + xem giá trị + xóa key, OpsAndAbove)
│       │   ├── PosTerminalSavePayload.cs      ← shared record: payload chain PosMapPage→DetailDialog→EditDialog
│       │   └── Dialogs/ (PosTerminalDetailDialog, PosTerminalEditDialog, StoreDetailDialog,
│       │                  PosDataSetupFormDialog, RedisKeyValueDialog)
│       ├── Catalog/
│       │   └── Product/
│       │       ├── ProductsPage.razor               ← /catalog/products — danh sách + thêm mới + xuất Excel + xem chi tiết
│       │       ├── ProductLockPage.razor             ← /catalog/product-lock — khóa/mở khóa SP theo cửa hàng
│       │       └── Dialogs/ (ProductDetailDialog — form tạo SP mới, dynamic barcode rows, upload ảnh;
│       │                      ProductViewDialog — xem chi tiết SP + barcode list + ảnh, read-only)
│       │   └── Price/
│       │       ├── PricesPage.razor                   ← /catalog/prices — 9.1 Danh mục Bảng giá (list + filter + Export)
│       │       ├── PriceSetupPage.razor               ← /catalog/price-setup — 9.3 Setup giá Bulk Import (import Excel + lưới preview)
│       │       └── Dialogs/ (PriceItemPickerDialog — tìm & chọn SP thêm dòng)
│       ├── Promotion/
│       │   ├── Offers/
│       │   │   ├── PromotionSetupPage.razor   ← /promotion/setup — Cài đặt CTKM (header cố định ngoài 4 tab: Thông tin chung=bảng lịch giờ/Mon-Sun, Sản phẩm mua/khuyến mãi=bulk-add+ScaleType+MinValue/TotalDiscount, Cửa hàng áp dụng, Cài đặt nâng cao=voucher delay+MemberCode autocomplete — khớp 100% field legacy SetupMain.cshtml)
│       │   │   ├── SpecialComboPage.razor      ← /promotion/special-combo — Special Combo
│       │   │   ├── OffersPage.razor            ← /promotion/offers — Danh mục khuyến mãi (Offer* live) — filter mặc định "Có hiệu lực"; modal Xem chi tiết 6 tab; nút Deactive (Status=2+Counter=MAX+1 qua usp_OfferHeader_Deactivate — chưa chạy SP trên DB thật)
│       │   │   └── Dialogs/ (SiteGroupSetupDialog — modal "Cài đặt nhóm cửa hàng": tạo mới nhóm CH/ST + danh sách filter/phân trang/xem chi tiết store/chọn gắn vào CTKM; ItemGroupSetupDialog — modal "Cài đặt nhóm sản phẩm" cho dòng Buy/Get "Nhóm SP": tạo mới nhóm + danh sách filter/phân trang/xem chi tiết sản phẩm/chọn gắn vào dòng; OfferDetailDialog — modal "Xem chi tiết" 6 tab Header/Buy/Benefits/Get/Site/Priority cho 1 offer LIVE, lazy-load theo tab active, export Excel riêng Buy/Get/Site)
│       │   └── CouponVoucher/
│       │       ├── CouponsPage.razor / CouponIssuePage.razor        ← 8.1/8.2 Coupon (list — bỏ filter/cột "Loại" / phát hành Auto·Import — Prefix/LenCode/CharOfNumber/CharPosition/Quantity thu thập qua dialog CouponIssueMoreDialog khi Lưu (Auto+chưa có mã), nút "PHÁT HÀNH THÊM" ở trang Xem để thêm lô mã Auto mới cho coupon đã có — khớp VoucherIssuePage)
│       │       ├── VouchersPage.razor                                ← 8.3 Danh mục Voucher (list + CRUD + Export — bỏ filter "Số serial"/"Loại" + cột "Loại")
│       │       ├── VouchersPublishedPage.razor                       ← 8.4 Tra cứu Voucher phát hành (CentralSales per-store)
│       │       └── Dialogs/ (CouponItemPickerDialog, CouponIssueMoreDialog — MỚI, phát hành mã Auto (mới/thêm),
│       │                      VoucherFormDialog, VoucherIssueMoreDialog, VoucherItemPickerDialog,
│       │                      CouponAdvancedDialog — không còn dùng, giữ code cho thiết kế lại sau)
│       └── Store/
│           ├── StoreDashboardPage.razor  ← /store/dashboard — Dashboard mặc định StoreOperator (landing page)
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
| F3 | MainLayout – Sidebar "Cửa hàng" (Policy=StoreAndAbove) | Layout/MainLayout.razor | ✅ | 3 sub-group (Vận hành/Giao dịch/Báo cáo) + 12 leaf links — icon sub-group (cấp 2) đổi đồng nhất về `ChevronRight` giống leaf (cấp 3), `HideExpandIcon="true"` ẩn mũi tên expand mặc định bên phải. **Role StoreOperator**: cả 3 sub-group luôn mở sẵn (`UpdateExpanded` bỏ qua route-match, ép `true`) để sidebar trải đều — ITOps/Admin vẫn theo route như cũ. Không còn menu "Dashboard" riêng — truy cập `/store/dashboard` qua click `pos-sidebar-brand` (logo "RPOS – Quản lý bán hàng", `@onclick Nav.NavigateTo`) |
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
| G9 | Index.razor – redirect theo role | Pages/Index.razor | ✅ | SystemAdmin→/admin/users, ITOps→/ops/health, StoreOperator→/store/dashboard (đổi từ /store/revenue) |
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
| G20 | VoidsPage – /store/voids + StoreAndAbove | Pages/Store/Transactions/VoidsPage.razor | ✅ | Lịch sử hủy giao dịch — filter panel + 4 KPI card + MudTable (chuẩn UI đồng bộ `PosMapPage.razor`). Fix 2026-07-08: `GetVoidReportAsync` lỗi SQL `LineNo` (reserved keyword) khiến trang luôn rỗng dù DB có dữ liệu, đã bracket-quote `[LineNo]` |
| G21 | RevenueHourlyPage – /store/revenue-hourly + StoreAndAbove | Pages/Store/RevenueHourlyPage.razor | ✅ | Doanh thu theo giờ — KPI + Line/Bar charts + MudTable (FooterContent dòng Tổng) + store combobox. **Tối ưu 10M dòng:** Redis cache repo (TTL theo độ mới) + includeKpi + CancellationToken + guard re-entrancy + hoãn load khỏi prerender + clamp 92 ngày khi all-stores |
| G22 | PaymentBreakdownPage – /store/payment-breakdown + StoreAndAbove | Pages/Store/PaymentBreakdownPage.razor | ✅ | Stub — Phân tích thanh toán (UI construction in progress) |
| G23 | TopProductPage – /store/top-product + StoreAndAbove | Pages/Store/TopProductPage.razor | ✅ | Top sản phẩm bán chạy — sp_ReportTopProduct (cache Pattern 4, clamp 92 ngày). KPI 3 card + CSS bar list + MudTable drill-through (ProductOrdersDialog). **BA/BI:** surface metrics (trả%/giá TB/độ phủ/giảm%) + so sánh cấp SP (Δ hạng/NEW). Ngành hàng ẩn tạm (SP chưa JOIN Item master) |
| G24 | StoreDashboardPage – /store/dashboard + StoreAndAbove + InteractiveServer | Pages/Store/StoreDashboardPage.razor | ✅ | Dashboard mặc định StoreOperator (landing page, thay /store/revenue). 3 KPI card (Doanh thu/Tổng Bill/Void+tỷ lệ) — chuẩn hóa theo `RevenuePage.razor` (`MudGrid` `xs=12 sm=4` + `MudPaper border-left 4px` + caption/h5, KHÔNG dùng flex+pos-kpi-value như bản đầu). `Task.WhenAll` 3 call: `RptRepo.GetSaleByTimeAsync(..,"HOUR")` (cache Redis sẵn, gộp KPI+chart giờ), `RptRepo.GetSaleByTimeAsync(..,"DAY")` (7 ngày gần nhất, không cần KPI), `SaleRepo.GetVoidReportAsync`. Auto-refresh PeriodicTimer mặc định 120s (giống PosMapPage). 2 bar chart FULL WIDTH xếp chồng (theo giờ hôm nay + 7 ngày gần nhất — bỏ layout `lg=7/lg=5` cạnh nhau để trục giờ dễ nhìn hơn) + MudTable Void gần nhất (top 10, link "Xem tất cả" → /store/voids). Tiêu đề header động `HeaderTitle` = "Cửa hàng {StoreNo}-{Name}" (tra `_allStores` — nay load cho MỌI role, kể cả StoreOperator, để lấy Name) thay vì "Dashboard Cửa hàng" tĩnh. **Đã bỏ**: KPI/panel trạng thái máy POS + ca làm việc (không còn gọi `GetPosTerminalListAsync`/`GetEosShiftListAsync` từ trang này); dòng caption "StoreNo · Lần cuối HH:mm:ss" (field `_lastLoaded` xóa luôn); menu "Dashboard" trên sidebar (`MainLayout.razor`) — truy cập qua click logo `pos-sidebar-brand` ("RPOS – Quản lý bán hàng") thay vì nav link |
| — | `ICentralMDRepository.GetPosTerminalListAsync` thêm tham số `storeNo` (optional, mặc định null) | Repositories/MasterData/CentralMDRepository.cs | ✅ | Trước đây luôn quét full `POSTerminal` (~5.000 dòng); nay filter `WHERE pt.StoreNo=@storeNo` tại DB khi có giá trị. Hiện dùng bởi `BusinessDayService` (đã filter theo store); `PosMapPage.razor` giữ hành vi cũ (không truyền storeNo). StoreDashboardPage không còn gọi method này (đã bỏ panel POS status) |
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
| G25 | RedisDashboardPage – /ops/redis + OpsAndAbove | Pages/Ops/RedisDashboardPage.razor | ✅ | Redis Management Dashboard — status card (kiểu HealthPage, border-left màu + chip ONLINE/OFFLINE + latency) + 5 KPI card (Bộ nhớ/Clients/Tổng Key/Cache Hit %/Uptime, không auto-refresh) + filter panel (pattern SCAN, chặn `*` cần confirm) + MudTable (Key/Type/TTL) + xem giá trị (RedisKeyValueDialog, pretty JSON) + xóa key (confirm + audit log). Backend: `IRedisManagementService` (POS.Application.Features.Redis) → `IRedisManager` mở rộng PingAsync/GetServerInfoAsync/GetDbSizeAsync (POS.Infrastructure.Cache) |
| J1 | IAuditLogger / DbAuditLogger — audit CRUD vào DashboardAuditLog | Auth/IAuditLogger.cs | ✅ | LogAsync(actor, action, entityType, entityKey, oldValueJson?, newValueJson?); ghi DB + Kibana; try/catch nội bộ; đăng ký Scoped trong Program.cs |
| J2 | PosDataSetupFormDialog – Add/Edit form, trả DTO đầy đủ | Pages/Ops/Dialogs/PosDataSetupFormDialog.razor | ✅ | Code read-only khi Edit; trả DialogResult.Ok(_model) (không Ok(true)) để page có newValue; duplicate Code → thông báo thân thiện |
| J3 | migration_dashboard_audit_log.sql – bảng DashboardAuditLog + 3 index | Auth/migration_dashboard_audit_log.sql | ⚠️ | Script idempotent — **PHẢI CHẠY trên RPOSMasterData trước deploy**; chưa chạy → log fail silently |
| J4 | audit-logging.md – rule audit CRUD chuẩn hóa cho toàn dự án | .claude/skills/web/audit-logging.md | ✅ | Pattern: snapshot oldValue từ item đã có, await LogAsync sau DB success, dialog trả DTO, checklist 12 mục |
| K1 | ProductsPage – /catalog/products + OpsAndAbove | Pages/Catalog/Product/ProductsPage.razor | ✅ | Danh sách SP/Barcode — SP GetProductList server-side paging; filter (mã/tên/barcode/thuế suất); nút Thêm mới + dialog tạo SP; Export Excel (ClosedXML); pos-page-header; grid 9 cột + cột Action (nút Xem → ProductViewDialog, 2026-07-06). Migrate 6.1+6.2+6.3 |
| K6 | ProductViewDialog – xem chi tiết SP (read-only) | Pages/Catalog/Product/Dialogs/ProductViewDialog.razor | ⚠️ | Header field read-only giống ProductDetailDialog + danh sách Barcode (MudSimpleTable) + ảnh (nếu có); GetProductDetailAsync đọc dbo.Item+dbo.Barcodes+dbo.ProductImage (2026-07-06) — phần ảnh phụ thuộc `dbo.ProductImage` **chưa chạy SP trên DB thật** (xem K2/D7 docs/ROLLOUT.md), tạm thời sẽ không có ảnh cho tới khi chạy script |
| K2 | ProductDetailDialog – form tạo sản phẩm mới | Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor | ⚠️ | 7 field (ItemName/Full/1 UOM chung "Đơn vị tính"/FamilyCode/TaxCode/Blocked/BlockedVINID — gộp UoM/SalesUoM thành 1 dropdown 2026-07-06, `SaveAsync` tự set `SalesUnitOfMeasure=BaseUnitOfMeasure`) + dynamic barcode table; INSERT dbo.Item + dbo.Barcode trong transaction; auto ItemNo (Max+1). Chỉ Create (Edit inline đã bỏ 2026-07-06 — ngoài phạm vi ProductList gốc); audit log CREATE "Product". Thêm upload ảnh sản phẩm (JPG/PNG ≤2MB, preview trước Lưu) → UPSERT dbo.ProductImage qua usp_ProductImage_Save — **2 SP chưa chạy/chưa fix trên DB thật**: `usp_Product_Save` (bug CAST→TRY_CAST, xem D9) **CRITICAL đang chặn Tạo mới**, `usp_ProductImage_Save` chưa chạy lần nào (D7). Xem docs/ROLLOUT.md |
| K3 | ProductLockPage – /catalog/product-lock + OpsAndAbove | Pages/Catalog/Product/ProductLockPage.razor | ✅ | Khóa/mở khóa SP theo cửa hàng — StoreNo bắt buộc; MudTable server-side + MultiSelection + chip màu; toggle đơn + bulk action; MudMessageBox @ref confirm; UPSERT dbo.ItemBlock; audit log LOCK/UNLOCK "ProductLock" theo item (2026-07-06). Migrate 6.4 — chỉ chế độ Central, KHÔNG tích hợp GrabFood / KHÔNG ghi trực tiếp máy POS, quyết định business xác nhận tại docs/web/logic/product_lock_scope_decision.md |
| J5 | IKibanaService → IFileLogHelper — migration toàn POS.Web | 24 .razor + 3 .cs (PendingUpdate, SqlConsoleService, DbAuditLogger) | ✅ | LogInfo → WriteLogs(`[{fn}] {entity}: {msg}`); LogException có ex → WriteExpLogs; LogException không có ex → WriteLogs(`[EXCEPTION][{fn}] msg`) |
| J6 | Audit log UsersPage (CREATE/UPDATE/LOCK/UNLOCK) + PosMapPage (UPDATE PosTerminal, chained dialog) | UsersPage.razor / UserFormDialog.razor / PosMapPage.razor / PosTerminalEditDialog.razor / PosTerminalDetailDialog.razor / PosTerminalSavePayload.cs (mới) | ✅ | UserFormDialog trả DTO đầy đủ (PasswordHash masked); DetailDialog forward result.Data!; PosMapPage capture oldJson trước dialog |
| K4 | PricesPage – /catalog/prices + OpsAndAbove | Pages/Catalog/Price/PricesPage.razor | ✅ | 9.1 Danh mục Bảng giá — reuse SP `GetSalesPriceList`/`_Export` (Dapper server-side paging); filter mã/tên + combobox "Hình thức bán hàng"/"Nhóm giá" (reuse `GetSetupLookupAsync`) + "Còn hiệu lực" (mặc định off); cột Hình thức + Trạng thái (MudChip); format nghìn khi sửa giá; Sửa/Xóa định vị bằng `SalesGroupCode`+`SalesTypeCode` (mã gốc, không dùng cột hiển thị); Export Excel (ClosedXML); pos-page-header. Migrate 9.1 (2026-07-06: fix bug Sửa/Xóa sai dòng) |
| K5 | PriceSetupPage + PriceItemPickerDialog – /catalog/price-setup + OpsAndAbove | Pages/Catalog/Price/PriceSetupPage.razor + Dialogs/PriceItemPickerDialog.razor | ✅ | 9.3 Setup giá (streamlined) — chọn Hình thức bán + cửa hàng → import Excel (MudFileUpload+ClosedXML) → ValidateImportAsync → lưới preview MudTable sửa inline (giá/ngày) + RowStyleFunc highlight lỗi + item picker thêm dòng → Lưu (block khi còn lỗi) + audit log. SP mới `usp_SetupSalePrice_Save` (TVP, ủy quyền Setup_SalePrice_Get_ALL). Migrate 9.3 |
| H1 | Build pass (0 error, 14 warning pre-existing) | — | ✅ | `dotnet build POS.Web` → Build succeeded. 0 Error(s). ContractTests 23/23 pass (DI validation xanh). |
| L1 | LogFilePage – /admin/logs + AdminOnly + InteractiveServer | Pages/Admin/LogFilePage.razor | ✅ | Quản lý Log Server — liệt kê + tải file `.txt`/`.log` dưới thư mục cha của `Logging:FileLogDirectory` (vd Prod `/srv/pos/logs/web` → root `/srv/pos/logs`, gồm cả `api/`, `web/`...), đệ quy toàn bộ subfolder. `ILogFileService`/`LogFileService` (Services/) — service riêng của POS.Web (như `IWebUserService`), whitelist extension `.txt`/`.log` cả lúc list lẫn download, chống path traversal bằng `Path.GetFullPath` + so khớp prefix root (cùng pattern `SyncDataPosController.DowloadFileStream`); mọi lỗi bọc try/catch ghi `IFileLogHelper.WriteExpLogs`, không throw ra UI. Download qua `JS.SaveAsFileAsync` (byte[], JS interop có sẵn) — không qua controller HTTP. Đăng ký `AddScoped<ILogFileService, LogFileService>()` trong Program.cs; nav item nằm trong nhóm "VẬN HÀNH" → L2 "Nhật ký" (cạnh Interface Error/DataRawJson Log/Nhật ký thao tác, `_expandOpsLog` đã thêm `/admin/logs`) — do L2 "Nhật ký" chỉ yêu cầu `OpsAndAbove` (ITOps thấy được) trong khi trang `/admin/logs` là `AdminOnly` (chỉ SystemAdmin), leaf link bọc riêng `<AuthorizeView Policy="@WebPolicies.AdminOnly">` để ITOps không thấy link 403. Verify: build 0 error + ContractTests 25/25 xanh + script standalone xác nhận 6 ca path-traversal/extension đều chặn đúng (traversal `../`, nested `../../`, absolute path ngoài root, extension `.dll`) — **chưa chạy app thật trên trình duyệt** (sandbox thiếu POS_SECRET_KEY/DB/Redis). |

---

## Tóm tắt

- ✅ Hoàn thành: **91 / 95 hạng mục**
- ⚠️ Có vấn đề: **4 hạng mục** (B9 — SQL seed hash placeholder; J3 — migration chưa chạy trên DB;
  K2 — **CRITICAL**: `usp_Product_Save` lỗi 8114 (CAST→TRY_CAST) chặn Tạo mới sản phẩm (D9) **+**
  `usp_ProductImage_Save`/bảng `dbo.ProductImage` chưa chạy trên DB (D7); K6 — phụ thuộc K2 (ảnh))
- ❌ Còn thiếu: **0 hạng mục**

> +1 hạng mục mới (session 2026-07-06): K6 (ProductViewDialog — xem chi tiết SP + barcode + ảnh).
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

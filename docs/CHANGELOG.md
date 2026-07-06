# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.
>
> **Lưu ý định hướng (2026-06-26):** dự án **không còn migrate** từ POS.API (.NET 4.6 /
> `POS.Backend`) — nay **phát triển mới (greenfield)**. Các entry cũ có từ "migrate"/"Migrated"
> là ghi chép lịch sử tại thời điểm đó, giữ nguyên để tra cứu.

## [2026-07-06] Danh mục Bảng giá (9.1) — cột Hình thức/Trạng thái, filter combobox, fix bug Sửa/Xóa sai dòng

**Layer:** POS.Web, POS.Common, POS.Infrastructure + SQL
**Loại:** Feature + Bug fix

**Thay đổi:**
- `PricesPage.razor`: ẩn cột Site; đổi label "Vùng giá"→"Nhóm giá"; thêm cột **"Hình thức"** (`SaleTypeName`, trước cột "Nhóm giá") + cột **"Trạng thái"** (Hiệu lực/Chưa hiệu lực/Hết hiệu lực — `MudChip` màu, tính client-side từ `StartingDateStr`/`EndingDateStr`); ngày `01/01/9999` hiển thị "Vô thời hạn"; filter Barcode/SalesCode (text tự do) → `MudSelect` "Hình thức bán hàng"/"Nhóm giá" (reuse `PriceService.GetSetupLookupAsync`, không tạo lookup mới); mặc định "Còn hiệu lực" **bỏ check**; format nghìn khi nhập Giá bán (`FormatThousands`, khớp pattern `PriceSetupPage.razor`).
- **FIX bug Sửa/Xóa giá sai dòng**: SP `GetSalesPriceList`/`_Export` (DBA sửa 2026-07-05→06) đổi trả cột `SalesCode` = **tên** nhóm giá (`PriceGroupName`) thay vì mã — code cũ dùng thẳng field này làm khoá gửi `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (đang lọc theo **mã**) → luôn báo "Không tìm thấy dữ liệu" khi Code≠Name. Thêm cột mã gốc `SalesGroupCode`/`SalesTypeCode` vào SP (script `docs/sql/GetSalesPriceList_AddSaleType.sql` → `_AddSalesTypeCode.sql`), map vào `PriceListItemDto`, sửa `TryBuildKey` dùng field mã thay field hiển thị.
- **FIX bug thứ 2 phát hiện khi review**: 1 item/uom/nhóm giá/ngày hiệu lực có thể có nhiều dòng khác nhau theo `SalesType` (hình thức bán hàng) — composite PK cũ (ItemNo, SalesCode, StartingDate, UOM) không đủ định vị. Thêm field `PriceRowKey.SalesType` + tham số `@SalesType` vào `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (script `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`).
- `GetSalesPriceList_Export`: fix thêm 1 bug review-time không liên quan yêu cầu ban đầu — proc tham chiếu sai tên temp table (`#SalsePriceExportTemp` không tồn tại, bảng thật là `#TempSalesPrice`) → nút Xuất Excel sẽ crash runtime nếu không sửa.
- **Đính chính schema**: `database-schema.md` từng ghi "SalesPrice KHÔNG có Id/IsActive" — SAI. Source thật của `GetSalesPriceList` (`AND S.IsActive = 1`, bắt buộc bất kể `@isCheck`) + bản `usp_SalesPrice_SoftDelete` mới (set `IsActive=0` khi xóa mềm) xác nhận bảng CÓ 2 cột này. Trước bản vá này, xóa mềm chỉ set `EndingDate` năm 7777 mà không set `IsActive=0` → dòng đã xóa có thể vẫn hiển thị khi bỏ check "Còn hiệu lực" (SP luôn yêu cầu `IsActive=1`, không điều kiện theo `@isCheck`).
- `PriceListDto.cs`: `PriceListItemDto` +`SalesGroupCode`+`SalesTypeCode`; `PriceListFilter` bỏ `Barcode`, `SalesCode`→`SaleType`+`SalesGroup` (mặc định `"ALL"`). `PriceSetupDto.cs`: `PriceRowKey` +`SalesType`. `PriceRepository.cs`: `GetListAsync`/`GetExportListAsync` đổi tham số EXEC theo SP mới + `NormalizeSalesGroup` (dịch UI sentinel `"ALL"`→`""`); `UpdatePriceAsync`/`SoftDeletePriceAsync` truyền thêm `@SalesType`.

**Pattern mới:** SP đổi 1 cột từ mã sang tên hiển thị → luôn thêm cột mã gốc riêng cho composite key (không tái dùng field hiển thị để build khoá Update/Delete). Đã cập nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** phải chạy đủ 3 script SQL theo thứ tự trước khi test: `GetSalesPriceList_AddSaleType.sql` → `GetSalesPriceList_AddSalesTypeCode.sql` → `SalesPrice_EditDelete_AddSalesType.sql`. Khi 1 SP legacy/tự-quản lý đổi ý nghĩa 1 cột đang dùng làm khoá composite ở nơi khác, luôn rà lại MỌI nơi consume cột đó (không chỉ nơi hiển thị) trước khi merge.

---

## [2026-07-05] Cài đặt giá / Danh mục Bảng giá — Sửa/Xóa giá 9.1, fix lưu SP, format UI, menu

**Layer:** POS.Web, POS.Application, POS.Infrastructure, POS.Common + SQL
**Loại:** Feature + Bug fix

**Thay đổi:**
- `docs/sql/SalesPrice_EditDelete.sql` (MỚI): `usp_SalesPrice_UpdatePrice` (sửa UnitPrice in-place theo composite PK + bump Counter) và `usp_SalesPrice_SoftDelete` (soft-delete = EndingDate năm 7777 + Counter). Bảng `SalesPrice` không có cột `Id` → định vị dòng bằng composite PK `(ItemNo,SalesCode,StartingDate,UOM)`.
- `PricesPage.razor` (9.1): thêm cột Thao tác — sửa giá inline + xóa (confirm) + `IAuditLogger`.
- `PriceSetupPage.razor` (9.3): thêm route thứ 2 `/catalog/price-declare`; đổi tiêu đề "Cài đặt giá"; format ô Giá bán khi nhập (thousand sep `,`, căn phải); `.pos-price-grid table{min-width:1040px}` để lưới cuộn ngang, ô ngày không bị bóp.
- `MainLayout.razor`: menu "Giá bán"→**"Danh mục Bảng giá"** (`/catalog/prices`); thêm "Cài đặt giá" (`/catalog/price-declare`); **ẩn** "Setup giá (Bulk Import)" (`/catalog/price-setup`, route còn).
- `PriceSetupDto.cs`: +`PriceRowKey`. `IPriceService/PriceService` +`UpdatePriceAsync`/`DeletePriceAsync`. `IPriceRepository/PriceRepository` +`UpdatePriceAsync`/`SoftDeletePriceAsync`.
- `docs/sql/SetupSalePrice_Save.sql` (FIX): (1) trả kết quả qua **OUTPUT param** `@Ok/@Message` thay vì result set — vì nhánh update `EXEC Setup_SalePrice_Get_ALL` tự SELECT Interface_Errors (+ROLLBACK bên trong → không hứng được bằng INSERT...EXEC), trước đây Dapper `QueryFirstOrDefault` đọc nhầm set rỗng → báo "thất bại" giả khi Pkey đã tồn tại; (2) chuẩn hóa sentinel "vô thời hạn" `9999-12-31 → 9999-01-01` khi INSERT (khớp legacy) để lần cập nhật sau không sinh khoảng "đuôi" thừa. `PriceRepository.SaveAsync` đổi sang `ExecuteAsync` + đọc output param.
- `FileLogHelper.WriteExpLogs`: ghi `ex.ToString()` (full stack + inner) thay `JsonConvert.SerializeObject(ex)` (dễ ném lỗi → file rỗng).

**Pattern mới:** SP ủy quyền SP-legacy-trả-result-set → dùng OUTPUT param (không result set); + format số khi nhập bằng dấu `,` để khớp `ParsePrice`. Đã cập nhật `.claude/skills/api/SKILLS.md`, `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:** `dbo.SalesPrice` schema thật trên DB CÓ cột `IsActive` (khác `database-schema.md` ghi 15 cột); sentinel vô thời hạn lưu là `9999-01-01`, đã xóa là năm `7777`. Chạy `SalesPrice_EditDelete.sql` + `SetupSalePrice_Save.sql` trên DB trước khi test. Chạy app bằng `dotnet run` (Development) — chạy `.exe` trực tiếp = Production (DB `127.0.0.1,14333`, log `/app/logs`).

---

## [2026-07-05] Middleware log request/response toàn cục cho POS.Api (bật/tắt qua config)

**Layer:** POS.Api, POS.Infrastructure
**Loại:** Feature + Pattern mới

**Bối cảnh:** nhiều API port từ code cũ (VCM.POSBLUE.API, source gốc không còn) chưa rõ POS gửi
request lên như thế nào / API trả response ra sao — khó chẩn đoán khi có lỗi (rút ra từ vụ debug
`UploadFileLogJob`). Trước đó chỉ 3/9 controller tự gọi `LogRequest` thủ công, không nhất quán,
1 kiểu ghi file `.txt` đồng bộ (`FileLogHelper.WriteLogs`) tốn I/O nếu áp cho toàn bộ endpoint.

**Thay đổi:**
- `src/POS.Api/Middleware/RequestResponseLoggingMiddleware.cs` (mới): log request/response cho
  MỌI API qua `IKibanaService.LogRequest`/`LogResponse` (tái dùng Serilog pipeline có sẵn, không
  thêm hạ tầng mới). Dùng `CappedCapturingStream` pass-through — KHÔNG buffer toàn bộ response vào
  `MemoryStream` (an toàn với endpoint stream file lớn như `DowloadFileStream`). Bỏ qua capture nội
  dung multipart upload và response binary, chỉ log metadata.
- `src/POS.Api/Middleware/RequestLoggingOptions.cs` (mới): `Enabled`/`MaxBodyBytes`/`ExcludePaths`.
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: thêm cờ `RequestLogging:PersistToFile`
  — quyết định File sink (`pos-*.log`) có nhận log Request/Response hay chỉ Elasticsearch (lọc
  đúng theo giá trị property `"HttpContext"="Request"/"Response"`, không ảnh hưởng Exception/Info).
- `src/POS.Api/Program.cs`: đăng ký middleware **ngoài cùng pipeline** (trước
  `UsePosExceptionHandling`) để bao trùm cả response lỗi chuẩn hoá.
- Dọn 19 điểm gọi `LogRequest` thủ công cũ (method wrapper + call site) ở `SyncDataPosController.cs`,
  `PaymentController.cs`, `LoyaltyController.cs` — middleware toàn cục thay thế, tránh log trùng.
- `appsettings.json`/`.UAT.json`/`.Production.json` (POS.Api): thêm section `RequestLogging`
  (`Enabled: false` mặc định — opt-in khi cần debug 1 đợt; `PersistToFile: true` vì **chưa cài
  Elasticsearch**, cần bản ghi local trên đĩa server để tra cứu).
- `appsettings.Development.json` (POS.Api): override `RequestLogging:Enabled: true` — tiện bật sẵn
  lúc dev/debug, không cần set biến môi trường thủ công (bug follow-up: lúc verify ban đầu chỉ set
  qua biến môi trường tạm, không lưu vào config nào → lần chạy sau tưởng middleware không hoạt
  động, thực ra do cấu hình quay về mặc định `false`).

**Pattern mới:** Middleware log request/response toàn cục — capped pass-through stream → đã cập
nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** `RequestLogging:Enabled` mặc định `false` ở mọi môi trường (opt-in) —
**riêng Development** đã override `true` trong `appsettings.Development.json`. Nếu thấy log
Request/Response không xuất hiện, kiểm tra `RequestLogging:Enabled` hiệu lực trước khi nghi ngờ
code. Khi Elasticsearch được cài đặt thật và ổn định, cân nhắc đổi `PersistToFile: false` (UAT/PROD)
để giảm I/O đĩa cho log Request/Response (xem `docs/ROLLOUT.md` §O4).

---

## [2026-07-05] Thay WinSCP bằng FluentFTP cho UploadFileLogJob (WinSCP không chạy được trên Linux)

**Layer:** POS.Infrastructure, POS.Application
**Loại:** Bug fix

**Root cause:** `WinScpFileTransfer` dùng thư viện `WinSCP` (.NET assembly) — vốn hoạt động bằng
cách spawn tiến trình `winscp.exe` (Windows PE binary). POS.Api chạy trên container Linux
(`mcr.microsoft.com/dotnet/aspnet:10.0`, xem `Dockerfile`) → `winscp.exe` **không thể** thực thi,
nên `session.Open(...)` luôn throw. Catch-block cố log lại exception bằng
`JsonConvert.SerializeObject(ex)` (Newtonsoft reflection-serialize) lại tự ném
`JsonSerializationException` khác vì exception WinSCP tham chiếu `Session` đã bị `using` dispose
trước khi vào catch (`Session.DebugLogPath` getter throw `ObjectDisposedException`) — che giấu hoàn
toàn lý do thất bại thật.

**Thay đổi:**
- `src/POS.Infrastructure/POS.Infrastructure.csproj`: gỡ package `WinSCP`, thêm `FluentFTP` (managed
  .NET thuần, không cần binary ngoài, chạy được trên Linux/Ubuntu).
- `src/POS.Infrastructure/Files/WinScpFileTransfer.cs` → xoá, thay `FtpFileTransfer.cs` (dùng
  `FluentFTP.FtpClient`, `DataConnectionType = PASV` — cần thiết vì API chạy sau NAT/Docker). Sửa
  luôn cách log exception: `ex.ToString()` thay `JsonConvert.SerializeObject(ex)` (áp dụng cả ở
  `SyncDataPosService.UploadFileLogToFtpAsync`) — tránh Newtonsoft đệ quy vào object nội bộ gây lỗi
  thứ cấp tương tự trong tương lai.
- `src/POS.Infrastructure/DependencyInjection.cs`: `IFtpFileTransfer` → `FtpFileTransfer`.
- Dọn config `AppSettings:WinScpExecutablePath` không còn dùng khỏi 4 file `appsettings*.json`
  (POS.Api + POS.Web) + `docs/CURRENT_STRUCTURE.md`.

**Pattern mới (nếu có):** Không — thay thư viện, giữ nguyên interface `IFtpFileTransfer`.

**Lưu ý cho session sau:** Tính năng đẩy log job qua FTP trung tâm (`UploadFileFTP: "YES"`) có thể
đã fail âm thầm từ lâu (từ khi hạ tầng chuyển sang Docker/Linux) do nguyên nhân trên — sau khi đổi
sang FluentFTP, còn cần đội hạ tầng xác nhận container mở được outbound tới FTP server (port điều
khiển + dải port PASV) và `FTPSERVER/FTPUSERNAME/FTPPASSWORD` (bảng Data Setup) còn đúng.

---

## [2026-07-05] BusinessDayPage — fix crash tìm kiếm + phân quyền force EOD + auto-load

**Layer:** POS.Web, POS.Application, POS.Infrastructure
**Loại:** Bug fix + Feature

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs`: FIX crash `ArgumentException:
  duplicate key ""` khi tìm kiếm — SP `GetSalesEODConfirm` trả cột theo tên legacy (`TerminalID`,
  `AmountTotal`, `CashMoney`, `LastOrderTime`, `CountCustomer`, `CountOrderNo`…) không khớp property
  `PosDayStagingDto` nên Dapper để `PosTerminal = ""` cho mọi dòng → `ToDictionary` trùng key rỗng.
  Thêm `private sealed class SalesEodConfirmRow` (nullable) khớp tên cột SP + `commandType:
  CommandType.StoredProcedure` (trước đó thiếu → SP chạy dạng text) rồi project tường minh sang DTO.
  Xóa khối `const string sql` (query CTE dead-code không được gọi).
- `src/POS.Application/Features/StoreActivities/IBusinessDayService.cs` + `BusinessDayService.cs`:
  thêm param `bool allowForceConfirm = false` cho `ConfirmBusinessDayAsync` (guard "còn POS chưa đóng
  ngày" chỉ chặn khi `!allowForceConfirm`); thêm method `GetCurrentBusinessDateAsync(storeNo)`
  delegate sang `ICentralSaleRepository.GetBusinessDateAsync(...).BussinessDate`.
- `src/POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor`: (1) `_canForceConfirm =
  IsInRole(ITOps)||IsInRole(SystemAdmin)`, `CanConfirm` cho ITOps/Admin force kể cả còn POS mở ngày,
  truyền `_canForceConfirm` xuống service (StoreOperator luôn false — defense-in-depth); cảnh báo +
  confirm dialog nhắc rõ khi force. (2) Sau xác nhận thành công `_businessDate = businessDate.AddDays(1)`
  rồi `SearchAsync()` — tự load lưới ngày D+1. (3) `OnInitializedAsync`: StoreOperator tự lấy ngày
  kinh doanh hiện tại của store (`GetCurrentBusinessDateAsync`; null → hôm nay) + auto-load, khỏi bấm
  "Tìm kiếm"; ITOps/Admin giữ thủ công.
- Doc: `docs/CURRENT_STRUCTURE.md` (signature `IBusinessDayService`), `docs/web/logic/eod.md` (flow).

**Pattern mới (nếu có):** Không — feature theo pattern sẵn có. (Lưu ý kỹ thuật: SP trả cột tên khác
DTO → dùng class trung gian nullable + project, KHÔNG map thẳng SP vào DTO response.)

**Lưu ý cho session sau:** SP `GetSalesEODConfirm` (DB CentralSale per-store) trả cột đặt tên theo
legacy `SaleBusinessStoreModel` — nếu bổ sung cột hiển thị, map qua `SalesEodConfirmRow` chứ đừng đổi
tên property `PosDayStagingDto` (đang dùng ở razor). Cột "Số lượng bán" tạm map từ `CountOrderNo`
(số đơn) — còn `// TODO confirm` vì SP không có cột item-quantity thật.

---

## [2026-07-04] Fix bẫy MudMessageBox — nút Yes không theo chuẩn Outlined (8 page)

**Layer:** POS.Web

**Loại:** Bug fix + cập nhật chuẩn (ngăn tái diễn)

**Nguyên nhân:** `DialogService.ShowAsync<MudMessageBox>(title, new DialogParameters{...}, options)`
render nút Yes bằng markup **mặc định của MudBlazor** — API này không có `<YesButton>` slot để
chỉnh `Variant`, nên nút luôn ra `Variant.Filled` bất kể chuẩn dự án đã chuyển hết sang
`Outlined`. Grep `MudButton.*Variant.Filled` không bắt được vì nút không tồn tại trong markup của
page — đây là lý do đợt rà soát rollout trước đó (2 entry bên dưới) không phát hiện ra.

**Thay đổi:**
- Chuyển 8 file từ `DialogService.ShowAsync<MudMessageBox>(...)` sang khai báo trực tiếp
  `<MudMessageBox @ref="_confirmBox">` + `<YesButton><MudButton Variant="Variant.Outlined" .../>
  </YesButton>` + gọi `_confirmBox!.ShowAsync()`: `BusinessDayPage.razor`, `VouchersPage.razor`,
  `SpecialComboPage.razor`, `PromotionSetupPage.razor`, `PosDataSetupPage.razor`,
  `DataRawLogPage.razor`, `UsersPage.razor`, `BankPosPage.razor`.
- `UsersPage.razor` cần thêm field động `_confirmTitle`/`_confirmYesText`/`_confirmYesColor` vì
  title/màu nút Yes đổi theo trạng thái khóa/mở khóa user.
- Cập nhật chuẩn để không tái diễn: `.claude/skills/web/SKILLS.md` (sửa ví dụ mẫu cũ đang dùng
  `Variant.Filled` trong chính pattern `MudMessageBox @ref`, thêm cảnh báo rõ anti-pattern + danh
  sách 8 file, thêm bullet vào "KHÔNG làm"), `CLAUDE.md` §14 (thêm callout "Bẫy dễ bỏ sót — confirm
  dialog"), `.claude/rules/mudblazor-flat-ui.md` §3 (thêm bullet tương tự).

**Verification:** `dotnet build src/POS.Web/POS.Web.csproj` → 0 error. `dotnet test
tests/POS.ContractTests` → 25/25 pass.

**Lưu ý cho session sau:** Bất kỳ page nào cần confirm dialog PHẢI dùng
`<MudMessageBox @ref>` khai báo trong markup — KHÔNG dùng `DialogService.ShowAsync<MudMessageBox>`
dù có vẻ gọn hơn, vì không thể style nút Yes theo chuẩn Outlined của dự án.

---

## [2026-07-04] MudBlazor Flat UI v2 — rollout đầy đủ toàn bộ 4 cụm menu còn lại (Cửa hàng, Khuyến mãi, Vận hành, Quản trị)

**Layer:** POS.Web

**Loại:** UI polish diện rộng (tiếp nối rollout pilot 9 page "Danh mục" cùng ngày)

**Thay đổi:**
- Áp dụng đầy đủ chuẩn Flat UI v2 (xem entry "MudBlazor Flat UI v2" pilot bên dưới cho chi tiết
  token theme) cho **~35 page + ~25 dialog** còn lại thuộc 4 cụm menu:
  - **Cửa hàng**: `Operations/{BusinessDayPage,EosShiftsPage,ShiftSummaryPage}`,
    `Transactions/{TransactionsPage,RefundsPage,VoidsPage}`,
    `Reports/{RevenuePage,RevenueByStaffPage,RevenueByStorePage,DetailRevenuePage,
    SalesByCategoryPage,RevenueHourlyPage,PaymentBreakdownPage,TopProductPage,LoyaltyPage}`
    + dialog liên quan (`EosShiftDetailDialog`, `TransactionDetailDialog`, `VoidDetailDialog`,
    `ProductOrdersDialog`...).
  - **Khuyến mãi**: `Offers/{OffersPage,PromotionSetupPage,SpecialComboPage}`,
    `CouponVoucher/{CouponsPage,CouponIssuePage,VouchersPage,VouchersPublishedPage,
    VoucherIssuePage}` + dialog (`CouponAdvancedDialog`, `CouponItemPickerDialog`,
    `VoucherItemPickerDialog`...).
  - **Vận hành**: `HealthPage`, `AlertsPage`, `QueuesPage`, `LogsPage`, `DataRawLogPage`,
    `SqlConsoleAuditPage` (route `/ops/activity-log` — tên file khác tên route, phát hiện trong
    lúc rollout), `PosDataSetupPage` + dialog (`PosDataSetupFormDialog`).
  - **Quản trị**: `UsersPage`, `RolesPage`, `ConfigPage`, `AuditPage`, `SqlConsolePage`,
    `EncryptSecretPage` + dialog (`UserFormDialog`).
- Mọi `MudButton` `Variant.Filled`/`Variant.Text` → `Variant.Outlined` (không ngoại lệ); mọi filter/
  input `MudPaper` thêm class `pos-filter-panel`; page-header icon/button `Size="Size.Small"` +
  title `Style="font-weight:400"`; dọn hardcode `Style="border-radius:4px"` trên
  `MudProgressLinear`.
- Phát hiện + sửa 2 dialog bị bỏ sót ở đợt pilot: `Catalog/Price/Dialogs/PriceItemPickerDialog.razor`,
  `Ops/Dialogs/PosTerminalEditDialog.razor`.
- Phát hiện + sửa 1 page bị bỏ sót ở đợt pilot (không nằm trong sidebar nav, chỉ reachable từ
  `VouchersPage`): `VoucherIssuePage.razor` + dialog `VoucherItemPickerDialog.razor` — đối xứng
  với `CouponIssuePage.razor` (đã convert ở đợt pilot).
- `Store/Dialogs/EosDayShiftListDialog.razor` xác nhận **orphaned** (grep không còn page nào mở
  dialog này) — cố ý không convert, giữ nguyên chờ dọn dẹp sau.

**Quy trình thực hiện:** dùng 6 subagent chạy song song (Agent tool, không phải Workflow — không
có opt-in ultracode), mỗi agent nhận đúng 1 bộ rule cơ học (button/filter-panel/header/radius) +
1 file tham chiếu đã convert (`ProductsPage.razor`) làm chuẩn calibrate, xử lý 1 nhóm menu độc
lập. Sau khi tất cả agent xong, tự grep quét lại toàn bộ `Components/Pages/` để xác nhận không còn
`MudButton Variant.Filled/Text` sót (chỉ còn `Login.razor` — cố ý, và `EosDayShiftListDialog.razor`
— orphaned).

**Pattern mới:** Không có pattern mới — đây là rollout cơ học của pattern đã thiết lập ở entry
pilot bên dưới. Đã cập nhật `.claude/rules/mudblazor-flat-ui.md` mục "Trạng thái rollout" để phản
ánh phạm vi đầy đủ.

**Lưu ý cho session sau:**
- Toàn bộ ~44 page + ~34 dialog trong `Components/Pages/` (Danh mục + Cửa hàng + Khuyến mãi +
  Vận hành + Quản trị) nay đã đồng bộ chuẩn Flat UI v2. Page mới tạo sau này phải theo đúng chuẩn
  này ngay từ đầu (xem `CLAUDE.md §14`, `.claude/skills/web/theming.md`).
- Icon set `Icons.Material.Outlined.*` **vẫn chỉ** áp dụng cho `MainLayout.razor` (sidebar +
  AppBar) — icon trong nội dung từng page/button vẫn `Filled` như cũ, đây là quyết định có chủ
  đích, chưa mở rộng.
- Build + `dotnet test tests/POS.ContractTests` (25/25) đã xanh sau toàn bộ đợt rollout — verify
  cuối cùng chạy sau khi cả 6 agent xong + sau khi tự vá 4 gap phát hiện thêm.

---

## [2026-07-04] Sidebar UI polish — bỏ icon riêng cấp 2, ẩn expand icon, đổi tên leaf, thu gọn spacing

**Layer:** POS.Web

**Loại:** Refactor (UI polish, không đổi logic nghiệp vụ)

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: icon `MudNavGroup` cấp 2 (Vận hành/Giao dịch/Báo cáo/Tổ chức/Thiết bị POS/Sản phẩm/Giá bán/Chương trình KM/Coupon & Voucher/Giám sát/Nhật ký/Cấu hình) đổi đồng nhất về `ChevronRight` — giống icon cấp 3, chỉ cấp 1 còn giữ icon riêng; thêm `HideExpandIcon="true"` cho toàn bộ `MudNavGroup` (cấp 1+2) ẩn mũi tên expand mặc định bên phải.
- Đổi tên 6 title leaf: "Tỉnh / Thành"→"Chi nhánh", "Khai báo máy POS"→"POSTerminal", "Máy POS ngân hàng"→"POS bank", "Danh sách SP / Barcode"→"Danh sách", "Setup giá (Bulk Import)"→"Setup giá bán", "Danh mục khuyến mãi"→"Danh mục".
- `src/POS.Web/wwwroot/app.css`: dòng menu cấp 2 thêm `padding-top/bottom:3px` + `line-height:1.5` (thu gọn ~15% so với mặc định MudBlazor `padding:4px`+`line-height:1.75`); `.mud-drawer .mud-nav-link` thêm `letter-spacing:-0.022em` (rút tracking, tránh label dài xuống dòng).
- Giữa chừng có 1 lần nhầm lẫn: đã lỡ xóa toàn bộ `@bind-Expanded` + logic accordion (`UpdateExpanded`, `OnLocationChanged`, `IAsyncDisposable`) tưởng đây là thứ cần bỏ — đã khôi phục lại đầy đủ ngay trong cùng session, giữ nguyên hành vi accordion tự mở/đóng theo route (`docs/WEB_STATUS.md` mục I3).

**Pattern mới:** Không có pattern hoàn toàn mới, nhưng pattern sidebar 3 cấp đã có (`.claude/skills/web/SKILLS.md` §"Sidebar nav (MainLayout) — 3 cấp") bị lệch với thực tế → đã cập nhật lại ví dụ code + anti-pattern trong file đó, và mục 5 `.claude/rules/mudblazor-flat-ui.md`.

**Lưu ý cho session sau:** Muốn ẩn UI chỉ báo expand/collapse của `MudNavGroup` → dùng prop `HideExpandIcon="true"` (KHÔNG xóa `@bind-Expanded`, đó là 2 cơ chế độc lập — Expanded quyết định trạng thái mở/đóng + accordion theo route, HideExpandIcon chỉ ẩn mũi tên hiển thị). `MudNavLink` không có `HideExpandIcon` (chỉ MudNavGroup có).

---

## [2026-07-04] MudBlazor Flat UI v2 — theo mẫu "Mud Mini" (sidebar/appbar sáng, borderless, radius 16px, button Outlined toàn app)

**Layer:** POS.Web

**Loại:** Pattern mới (redesign theme toàn diện) + UI polish 9 page + 9 dialog "Danh mục"

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`:
  - Sidebar/AppBar chuyển từ navy đậm (`#1B3A5C`) sang nền sáng (`#FFFFFF`) + chữ tint navy — theo
    mẫu MudBlazor chính thức "Mud Mini" (`docs/web/images/flat1.jpg`), thay cho Ynex (đã đánh giá
    và loại bỏ vì không phải MudBlazor gốc, rebrand rủi ro cao).
  - `DefaultBorderRadius` 4px → 16px.
  - `Shadows.Elevation[1..5]`: hairline (`0 0 0 1px`) → `"none"` (borderless hoàn toàn — card
    phân tách bằng chênh lệch nền Surface/Background, không viền không bóng).
  - `Typography.H5`: FontWeight 700→800, LetterSpacing -0.01em→-0.02em.
  - `Typography.Body1`: FontSize 0.875rem→0.75rem (giảm ~15%) + FontWeight=400 — chi phối input
    `MudTextField`/`MudSelect`/`MudDatePicker`/`MudAutocomplete` toàn app (không ảnh hưởng MudTable).
- `src/POS.Web/wwwroot/app.css`: thêm token `--pos-primary-bg`/`--pos-teal-bg`; viết lại CSS
  sidebar cho nền sáng (active-item = pill `--pos-primary-bg`, 3 tầng chữ opacity navy); class mới
  `.pos-sidebar-brand`, `.pos-filter-panel` (nền soft-tint filter panel); icon sidebar giảm còn
  `1.25rem`; nav item inset ngang 8px; nâng cấp `.pos-delta-up/down` thành pill badge (giữ ngữ
  nghĩa tăng=xanh/giảm=đỏ); softening viền header MudTable (2px navy → 1px `--pos-border`).
- `src/POS.Web/Components/Layout/MainLayout.razor`: `MudAppBar Color="Color.Primary"` →
  `Color.Default`; thêm `div.pos-sidebar-brand` (logo `MudAvatar` + "RPOS") thay `MudDrawerHeader`
  cũ (text thô "POSMaster POS System"); đổi toàn bộ icon sidebar `Icons.Material.Filled.*` →
  `Outlined.*`; đổi brand text "POS Dashboard – POSMaster" → "RPOS Dashboard", "POSMaster" →
  "RPOS".
- **9 page + 9 dialog trong menu "Danh mục"** (EmployeesPage, StorePage, ProvincesPage,
  PosMapPage, BankPosPage, ProductsPage, ProductLockPage, PricesPage, PriceSetupPage +
  EmployeeFormDialog, EmployeeChangePasswordDialog, StoreCreateDialog, StoreDetailDialog,
  BranchCreateDialog, BranchDetailDialog, PosTerminalDetailDialog, BankPosDetailDialog,
  ProductDetailDialog): mọi `MudButton` `Variant.Filled`/`Variant.Text` → `Variant.Outlined`
  (không ngoại lệ, kể cả nút trong confirm dialog/bulk action/nút Lưu cuối); filter panel thêm
  class `pos-filter-panel`; page-header icon/button thêm `Size="Size.Small"` + title thêm
  `Style="font-weight:400"`; dọn hardcode `Style="border-radius:4px"` trên `MudProgressLinear`.
- `ProductsPage.razor`: bỏ 5 cột thừa trên bảng hiển thị (Mã SP PLG, Tên SP (VN), ĐVT BC, Mã cha,
  Size) — vẫn giữ đủ 11 cột trong Export Excel (không đổi).

**Pattern mới:** Toàn bộ pattern (borderless card, Outlined-mọi-nơi cho button kể cả trong dialog,
input font-size 12px, sidebar brand header + icon Outlined, `pos-filter-panel`) đã ghi vào
`CLAUDE.md §14`, `.claude/rules/mudblazor-flat-ui.md` (rules file mới — có lịch sử quyết định đầy
đủ, kể cả phương án đã cân nhắc và loại bỏ), `.claude/skills/web/theming.md`,
`.claude/skills/web/SKILLS.md`, `.claude/skills/web/ui-polish-standard.md`.

**Lưu ý cho session sau:**
- Chỉ 9 page + 9 dialog "Danh mục" đã migrate đầy đủ sang chuẩn v2. ~31 page/dialog khác (Store
  reports, Ops, Admin, Promotion) vẫn dùng `Filled` cho CTA + chưa có `pos-filter-panel`/page-header
  sizing — rollout tiếp khi có yêu cầu, xem mục TODO cuối `.claude/rules/mudblazor-flat-ui.md`.
- `<PageTitle>` (tab browser) của các page vẫn giữ "... – POS Dashboard" — chỉ đổi brand text ở
  sidebar/AppBar sang "RPOS" theo đúng yêu cầu, chưa rename toàn app.
- Đây là bản v2 kế tiếp bản v1 (2026-06-26, flat hairline) — cách nhau chưa đầy 2 tuần; nếu cần
  đối chiếu/rollback, xem lịch sử quyết định đầy đủ trong `.claude/rules/mudblazor-flat-ui.md`.
- Build lúc thực hiện session này bị chặn nhiều lần do Visual Studio giữ lock file DLL (đang debug
  song song) — đã verify xanh (`dotnet build` 0 lỗi, `dotnet test tests/POS.ContractTests` 25/25)
  sau khi VS nhả lock ở cuối session.

---

## [2026-07-04] Xác nhận kết thúc ngày — port từ legacy StoreActivitiesController sang BusinessDayPage

**Layer:** POS.Web + POS.Application + POS.Infrastructure + POS.Common

**Loại:** Feature (port có chủ đích từ `src/legacy/`) + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/CentralSale/{PosDayStagingDto,BusinessDayConfirmDto,ConfirmBusinessDayRequest,ConfirmBusinessDayResult}.cs`: DTO mới.
- `src/POS.Infrastructure/Repositories/Sale/{I}CentralSaleRepository.cs`: thêm `GetPosDayStagingAsync`, `GetBusinessDayConfirmAsync`, `ConfirmBusinessDayAsync` — connection per-store qua `StoreRoutedConnectionFactory` (không phải `CentralSaleConnectionFactory` central dùng cho báo cáo đa store).
- `src/POS.Application/Features/StoreActivities/{I}BusinessDayService.cs`: mới, merge master POS terminal (`ICentralMDRepository.GetPosTerminalListAsync`, CentralMD) + staging shard (`ICentralSaleRepository`), validate rule "tất cả POS đã đóng ngày" trước khi cho xác nhận. Đăng ký DI trong `POS.Application/DependencyInjection.cs`.
- `docs/sql/BusinessDay_ConfirmEndDate.sql`: bảng `dbo.BusinessDayConfirm` + SP `usp_BusinessDay_ConfirmEndDate` — **chạy trên DB "CentralSale" theo TỪNG STORE** (shard, KHÔNG PHẢI RPOSMasterData/CentralMD), vì cần cùng 1 transaction với `UPDATE dbo.BussinessDateOpen` (advance +1 ngày cho máy POS) — atomic tuyệt đối.
- `src/POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor`: viết lại hoàn toàn (route giữ nguyên `/store/business-day`) — chọn 1 store bắt buộc (mặc định store đầu theo StoreNo, không có "Tất cả"), ngày kinh doanh mặc định hôm nay, KHÔNG tự load khi mở trang (chờ bấm Tìm kiếm); lưới per-POS-terminal + nút Xác nhận.
- `src/POS.Web/Components/Layout/MainLayout.razor`: đổi tên menu "Ngày kinh doanh" → "Xác nhận kết thúc ngày".
- Xóa `src/POS.Web/Components/Pages/Store/Dialogs/EosDayShiftListDialog.razor` (chỉ dùng bởi BusinessDayPage cũ, đã grep xác nhận không dùng chung; `EosShiftDetailDialog`/`GetEosDayListAsync`/`GetEosShiftListAsync` GIỮ NGUYÊN vì dùng chung với ShiftSummaryPage/EosShiftsPage).

**Pattern mới:** SP ghi dữ liệu có yêu cầu atomic với 1 bảng đã tồn tại sẵn ở DB khác CentralMD (ở đây là `BussinessDateOpen` trên DB "CentralSale" theo store) thì đặt bảng/SP mới CÙNG DB đó thay vì mặc định CentralMD — ưu tiên atomicity hơn quy ước mặc định. Chưa đưa vào SKILLS.md vì đây là quyết định case-by-case, không phải quy tắc chung.

**Lưu ý cho session sau:** Rule "tất cả POS đã đóng ngày" dựa trên sự tồn tại của dòng `POSEOD_API` (Store+Terminal+BusinessDate) — đây là giả định hợp lý dựa trên API `UpdatePOSEODAsync` đã có sẵn, CHƯA được xác nhận 100% với vận hành thực tế. Cột "Tiền mặt" dùng `POSShiftHeader`/`POSShiftLine` với giả định tên bảng giống DB CentralSale trung tâm (chưa xác minh trên shard DB) — có TODO comment trong code. Script `docs/sql/BusinessDay_ConfirmEndDate.sql` phải chạy thủ công trên DB CentralSale của TỪNG store (không phải 1 lần duy nhất như các script CentralMD khác).

---

## [2026-07-03] DataSync — fix đường dẫn UNC/CHANGE + Action envelope theo caller

**Layer:** POS.Api + POS.Application + POS.Common

**Loại:** Bug fix + Feature (tiếp nối nút Sync POSMap bên dưới)

**Thay đổi:**
- `SyncDataPosController.DeleteFileFromFTP`: POS gửi `filePath` UNC (`\\ip\FTPBLUEPOS\...`) → trước đây
  `File.Exists` trên UNC thô, trên Linux/Docker không resolve → luôn "không tồn tại". Fix: map UNC→local qua
  helper mới `ISyncDataPosService.ResolveFtpPhysicalPath` + guard path-traversal. `DowloadFileStream` refactor
  dùng chung helper (bỏ khối map inline).
- `SyncDataPosController.GetFileFromFTP` nhánh CHANGE: trước truyền `AppSettings:FolderShare` → list từ
  `{FolderShare}\CHANGE\{folderFile}` (thiếu segment `SyncDataPos\POS`) → không thấy file. Fix: truyền `pathSync`
  từ query; `GetFileFromServerApiAsync` bỏ special-case `if(typeSync=="ALL")`, **luôn** giải qua
  `MapFtpPath($"{pathSync}/{folderFile}")` → listing/URL/UNC nhất quán với nơi file tạo, hết lỗi sai case
  `syncdatapos/pos` trên Linux.
- `MasterDataSyncService.ActionFor` + `GetMasterDataFileRequest.SyncAction` (field mới): Action envelope tách
  theo caller — POS ALL giữ `TRUNC-INSERT`→`INSERT`; Web Sync (`PushStartOfDayDataAsync`) đặt `SyncAction="DELETE-INSERT"`
  → **mọi batch** ghi `DELETE-INSERT`. KHÔNG đổi logic stream/zip, không đổi dữ liệu (web vẫn full data).

**Pattern mới:** "Xử lý đường dẫn file POS gửi (SyncDataPos) — luôn giải về FtpRootPath, dùng chung" +
tham số hoá hành vi theo caller qua field DTO nội bộ → đã cập nhật `.claude/skills/api/SKILLS.md`.

**Lưu ý cho session sau:** với mọi endpoint nhận path từ POS, luôn map UNC→local bằng `ResolveFtpPhysicalPath`
trước khi thao tác file; `pathSync` POS gửi đã đủ `SyncDataPos/POS/{typeSync}` nên dùng `MapFtpPath` (đừng ghép
`FolderShare`). Muốn đổi hành vi theo caller → thêm field vào request DTO, đừng detect caller.

---

## [2026-07-03] PosMapPage `/catalog/pos-setup` — nút "Đẩy dữ liệu đầu ngày" cho máy POS

**Layer:** POS.Web + POS.Application

**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Ops/PosMapPage.razor`: thêm cột **Action** (sau `IsOnline`) — nút Sync +
  `MudMessageBox` confirm + spinner-trong-nút + pulse nền dòng (`_syncing` HashSet) + `@onclick:stopPropagation`
  (chặn mở nhầm dialog chi tiết). Ghi **audit log** `SYNC`/`PosTerminal` khi thành công (qua `IAuditLogger`).
- `src/POS.Application/Features/DataSync/ISyncDataPosService.cs` + `SyncDataPosService.cs`: thêm
  `PushStartOfDayDataAsync(siteCode, posTerminal, ct)` — inject `IMasterDataSyncService`, **gọi trực tiếp qua DI**
  (không HTTP sang POS.Api), tái dùng nguyên `EnsureMasterDataFileAsync` (`TypeSync=ALL` full data) — **KHÔNG đổi**
  logic sinh file txt/zip.
- `src/POS.Web/wwwroot/app.css`: keyframe `pos-row-syncing` (pulse nhẹ dòng đang xử lý).
- `src/POS.Web/appsettings.json` (DEV): `FolderShare`/`FtpRootPath`/... khớp POS.Api (key đã tồn tại — không sync UAT/Prod).
- `docs/ROLLOUT.md`: thêm **§O3** — yêu cầu POS.Web `FtpRootPath` trỏ chung thư mục POS.Api phục vụ (UAT/PROD đang rỗng).
- `docs/CURRENT_STRUCTURE.md`: thêm chữ ký `PushStartOfDayDataAsync` vào `ISyncDataPosService`.

**Bug đã fix trong task:** ban đầu dựng `TargetDir = Path.Combine(FolderShare, "CHANGE", ...)` → sai thư mục
(`FTPBLUEPOS\CHANGE\...`). Sửa dùng `MapFtpPath("SyncDataPos/POS/CHANGE/{site}/{terminal}")` để **bám y hệt
controller** (`FTPBLUEPOS\SyncDataPos\POS\CHANGE\{site}\{terminal}` — đúng nơi POS tạo/đọc + URL download).

**Pattern mới:** POS.Web kích hoạt tác vụ server-side (sinh file) bằng cách **gọi trực tiếp Application service
của POS.Api qua DI** thay vì HTTP; khi tái dùng phải **bám đúng convention path `MapFtpPath` của controller**,
KHÔNG tự dựng path bằng `FolderShare` → đã cập nhật `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:** file sinh trên host POS.Web nhưng POS tải qua POS.Api → 2 app phải chung `FtpRootPath`
(share/volume). Khi tái dùng logic file của POS.Api từ Web, luôn tra cách controller dựng `TargetDir` để khớp 100%.

---

## [2026-07-03] Thực thi rollout C4 — mã hóa xong appsettings.Production.json (POS.Api + POS.Web)

**Layer:** POS.Api + POS.Web + Infra/docs

**Loại:** Bảo mật (thực thi rollout — tiếp nối entry 2026-07-02 bên dưới)

**Thay đổi:**
- Sinh khóa `POS_SECRET_KEY` (AES-256, base64) bằng project console tạm (`ProjectReference` tới
  `POS.Infrastructure.csproj`, gọi thẳng `SecretProtector.GenerateKey()`/`Encrypt()`, verify round-trip
  `Decrypt()` trước khi dùng, rồi xóa project tạm) — tránh tự viết lại AES-GCM, đảm bảo tương thích
  100% với code decrypt thật. Kỹ thuật này đã ghi vào `.claude/skills/api/SKILLS.md`.
- `src/POS.Api/appsettings.Production.json` + `src/POS.Web/appsettings.Production.json`: thay toàn bộ
  9 connection string (`Password=...`) + `RabbitMQ.Password` mỗi file bằng token `enc:...` — không còn
  password thật dạng plaintext trong 2 file này.
- `.env` (local, gitignored): thêm `POS_SECRET_KEY` để `docker compose up` dùng được ngay cho service
  `webapp` (= POS.Api).
- `docs/architecture/appsetting.md` (**file mới**): tài liệu tra cứu nhanh — bảng "dùng mã hóa hay
  plaintext" (tự suy ra từ nội dung file, không phải 1 cờ cấu hình riêng), phạm vi áp dụng, anti-pattern,
  link sang `docs/ROLLOUT.md`/`docs/guide-deploy.md`/SKILLS.md.
- `CLAUDE.md`: thêm 1 dòng vào bảng "Mục lục tài liệu kiến trúc" trỏ tới `docs/architecture/appsetting.md`.
- `docs/WEB_STATUS.md`: cập nhật dòng S5 — từ ⚠️ (chưa rollout) → ✅ (đã rollout Production).

**Pattern mới:** Kỹ thuật sinh/mã hóa secret ngoài app đang chạy bằng project console tạm (xem
`.claude/skills/api/SKILLS.md` — cuối section "Pattern: Mã hóa credentials trong appsettings").

**Lưu ý cho session sau:** `appsettings.UAT.json` của cả 2 project **chưa** được mã hóa (ngoài phạm vi
task này — chỉ làm Production theo yêu cầu). Trên server UAT/PROD thật, vẫn cần người vận hành tự đặt
`POS_SECRET_KEY` (biến môi trường/`docker run -e`) — Claude không có quyền truy cập server đó. Khóa +
3 password thật đã đi qua hội thoại này (người dùng đã xác nhận chấp nhận đánh đổi này) — nếu cần đảm
bảo tuyệt đối "khóa chưa từng qua AI", rotate khóa sau qua `/admin/encrypt-secret`.

---

## [2026-07-02] Mở rộng mã hóa credentials (C4) sang POS.Api — đổi tên khóa chung POS_SECRET_KEY

**Layer:** POS.Api + POS.Web + Infra/docs

**Loại:** Bảo mật (chuẩn bị go-live Production)

**Thay đổi:**
- `src/POS.Api/Program.cs`: thêm hook giải mã token `enc:...` (AES-256-GCM qua `SecretProtector`),
  NGAY SAU `CreateBuilder`, TRƯỚC `AddInfrastructure` — copy đúng pattern đã có ở `src/POS.Web/Program.cs`.
  Trước đây cơ chế `SecretProtector` chỉ wired ở POS.Web; POS.Api đọc `appsettings.Production.json`
  plaintext dù `docker-compose.yml` đã âm thầm truyền sẵn biến khóa vào container (không có code tiêu thụ).
- Đổi tên biến môi trường khóa AES từ `POSWEB_SECRET_KEY` → **`POS_SECRET_KEY`** (dùng chung cho cả
  2 project, không còn gắn riêng "Web") — cập nhật `src/POS.Web/Program.cs`,
  `EncryptSecretPage.razor` (`/admin/encrypt-secret`), `SecretProtector.cs` (doc comment + thông báo lỗi),
  `docker-compose.yml`, `.env.example`.
- `docs/guide-deploy.md`: thêm `-e POS_SECRET_KEY=...` vào ví dụ `docker run` của cả POS.Api (§3.1)
  và POS.Web (§3.2) + ghi chú ở checklist.
- `docs/ROLLOUT.md` §C4: viết lại — phạm vi rollout nay là **CẢ HAI** `appsettings.Production.json`
  (POS.Api + POS.Web), cùng 1 khóa, token sinh ở trang `/admin/encrypt-secret` (POS.Web) dùng được cho
  cả 2 file vì cùng plaintext + cùng khóa. Thêm ghi chú naming: service `webapp` trong `docker-compose.yml`
  (root) thực chất là POS.Api, không phải POS.Web.
- `.claude/skills/api/SKILLS.md`, `docs/web/security.md`, `docs/WEB_STATUS.md`: đồng bộ tên biến +
  phạm vi 2 project trong phần mô tả pattern/security.

**Pattern mới:** Không có pattern kỹ thuật mới — tái dùng nguyên `SecretProtector` đã có, chỉ nhân rộng
hook sang project thứ 2 và đổi tên biến môi trường cho đúng phạm vi dùng chung.

**Lưu ý cho session sau:** Thực thi mã hóa Production thật (Bước 1-5 `docs/ROLLOUT.md` §C4) vẫn là việc
của **người vận hành** — Claude không giữ khóa, không tự thay password thật. Tại thời điểm này,
`appsettings.Production.json` của cả POS.Api và POS.Web **vẫn còn plaintext** (password thật) —
cơ chế code đã sẵn sàng cho cả 2 project, chỉ còn chờ ops chạy rollout. `POS.Worker` vẫn ngoài phạm vi
(chưa có hook, vẫn plaintext). Đã verify: `dotnet build` (0 lỗi) + `dotnet test tests/POS.ContractTests`.

---

## [2026-07-02] Dọn sạch docs tham chiếu legacy/migrate — source code legacy đã xóa khỏi máy

**Layer:** Docs (`.claude/skills/`, `docs/`, `CLAUDE.md`, `README.md`) — không đụng code

**Loại:** Refactor tài liệu

**Thay đổi:**
- Xóa hẳn `_migration/INVENTORY.md`, `_migration/PROGRESS.md`, `docs/PROJECT_INVENTORY.md` —
  100% nội dung là inventory/tracking source legacy (.NET Framework 4.6.2, VCM.BLUEPOS) đã bị
  xóa khỏi máy, không còn đối chiếu được.
- Đổi tên `.claude/skills/web/ui-migrate-legacy.md` → `.claude/skills/web/ui-polish-standard.md`
  (giữ nguyên nội dung kỹ thuật — pattern chip màu, empty-state, action bar, MudCard polish —
  chỉ bỏ khung "trang migrate từ legacy" vì không còn phân biệt trang cũ/mới).
- `CLAUDE.md`: bỏ hẳn mục "Migrate VCM.BLUEPOS → POS.Web" (5 mục con), bỏ hàng inventory legacy
  trong bảng doc-map, cập nhật §13 POS.Web trỏ sang `ui-polish-standard.md`.
- `docs/CURRENT_STRUCTURE.md`: bỏ "MỤC H — Những gì chưa có" (bảng Controllers/Services/BLO/
  Helpers "chưa migrate" đối chiếu inventory đã xóa) + số liệu thống kê liên quan.
- `docs/API_CONTRACT.md`: bỏ mục 10 "Notes cho Migration sang .NET 10" (đã hoàn thành từ lâu).
- `.claude/skills/api/SKILLS.md`: pattern "xác minh tên bảng qua legacy EDMX"
  (`src/legacy/*/EF/**/*.edmx`) → thay bằng tra `docs/architecture/database-schema.md`.
- `.claude/skills/cache/SKILLS.md`, `.claude/skills/worker/SKILLS.md`: bỏ khung "migrate từ
  project cũ/IIS MemoryCache", giữ nguyên toàn bộ quy tắc kỹ thuật (Redis pattern, Worker pattern).
- `docs/architecture/database-schema.md`, `docs/web/LOGIC_APPROVE_CTKM.md`: sửa cross-reference
  trỏ tới mục/file đã xóa.
- `README.md`: viết lại hoàn toàn — bản cũ mô tả sai kiến trúc (POS.API/POS.Domain/POS.Shared),
  sót lại từ giai đoạn lên kế hoạch ban đầu, còn trỏ tới `POS.Backend`/`analyze-legacy.md`.
- **Cố ý giữ nguyên**: `docs/CHANGELOG.md`, `docs/WEB_STATUS.md` — entry cũ có chữ "migrate"/
  "Legacy" là ghi chép lịch sử tại thời điểm đó (đã có ghi chú "giữ nguyên để tra cứu" ở đầu file).

**Pattern mới:** Không có — đây là dọn dẹp docs theo yêu cầu trực tiếp, không phát sinh pattern code mới.

**Lưu ý cho session sau:** `src/legacy/`, `_migration/`, `docs/PROJECT_INVENTORY.md` **không còn
tồn tại** — đừng đề xuất đọc/grep các đường dẫn này nữa. Khi cần tra tên bảng/cột DB dùng
`docs/architecture/database-schema.md`; khi cần tra cấu trúc code hiện có dùng
`docs/CURRENT_STRUCTURE.md`. Đã verify: `dotnet build` (0 lỗi) + `dotnet test tests/POS.ContractTests` (25/25 pass).

---

## [2026-07-02] Validate ActicleNo tồn tại trong CpnVchBOMHeader trước khi tạo Voucher SAP

**Layer:** POS.Api (POS.Application + POS.Infrastructure)
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: thêm chữ ký
  `Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default)`.
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement với cache
  Redis Hash `MD:CpnVchBOMHeader` (positive-only, TTL 12h) — point-lookup
  `SELECT TOP 1 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo=@itemNo`.
- `src/POS.Application/Features/Sap/SAPService.cs`: inject thêm `ICentralMDRepository`; trong
  `CreateNewVoucherAsync` validate **toàn bộ** `Article_No` (khác rỗng) TRƯỚC vòng lặp tạo —
  mã không tồn tại → trả `400 "ActicleNo {x} không tồn tại"`, KHÔNG tạo phần tử nào (tránh
  partial vì loop không có transaction). `Article_No` rỗng → bỏ qua (giữ hành vi cũ). Guard tự
  áp cho cả `CreateReturnVoucher` (controller gọi lại `CreateNewVoucherAsync`).

**Pattern mới:** Existence-check cache (positive-only) — validate master data trước khi ghi
→ đã cập nhật `.claude/skills/cache/SKILLS.md` (Pattern 5).

**Quyết định:** đối chiếu cột `CpnVchBOMHeader.ItemNo` (theo quy ước mirror
`CpnVchBOMCodeIssue.ItemNo = ActicleNo`). Không đổi SP/DTO/contract, không tạo SQL mới.

**Lưu ý cho session sau:** Khi cần validate "khóa tồn tại trong master" trước một write, dùng
Pattern 5 (cache dương, không cache âm) — KHÔNG dùng Pattern 1. Nếu DBA thêm master mới cần dùng
ngay trong <12h: `DEL MD:CpnVchBOMHeader`.

---

## [2026-07-02] Vá đồng bộ dữ liệu Coupon (POS.Web) ↔ SAP Voucher (POS.Api) trong CpnVchBOMCodeIssue

**Layer:** Database (docs/sql)
**Loại:** Bug fix (thiếu field khi ghi) + Feature (unify check/redeem 2 nguồn)

**Vấn đề:**
1. `usp_Voucher_Create` (POS.Api tạo voucher SAP) không ghi `ItemNo` — mất liên kết ItemNo↔ActicleNo.
2. `usp_SetupCoupon_SaveIssue` (POS.Web phát hành coupon) chỉ ghi 7/22 cột nghiệp vụ khi insert
   mã — thiếu `Status/Return/ActicleNo/ActicleType/Value/Voucher_Currency/Validity_From_Date/
   Expiry_Date/CompanyCode/VoucherType`, khiến mã coupon KHÔNG thể redeem/check qua POS.Api.
3. `usp_Voucher_GetByCode`/`usp_Voucher_Redeem` chỉ nhận `Source='SAP'` — POS.Api không nhận diện
   được mã Coupon dù dùng chung bảng, trong khi POS.Web phát hành coupon cho khách dùng tại POS,
   và POS dùng coupon/voucher qua chính POS.Api — 2 luồng cần liên thông thật sự.

**Thay đổi:**
- `docs/sql/CpnVchBOMCodeIssue_ItemNoHardening.sql` (**file mới**) — mở rộng
  `CpnVchBOMCodeIssue.ItemNo` varchar(20)→varchar(50) (khớp width `ActicleNo`, tránh lỗi truncate
  khi SAP gửi `Article_No` dài) + thêm index `IX_CpnVchBOMCodeIssue_ItemNo` (bảng chưa có index
  nào trên cột này, cần thiết vì các UPDATE đồng bộ mới chạy trên mọi lần Lưu, không chỉ lần đầu).
- `usp_Voucher_Create`: thêm `ItemNo = @ActicleNo` (mirror) vào INSERT; thêm guard `THROW` khi
  `Code` trùng với 1 dòng ở Source khác (tránh vi phạm unique index bằng SqlException thô).
- `usp_SetupCoupon_SaveIssue`: Section insert Codes (chạy lần đầu) nay ghi đủ
  `ActicleNo(=ItemNo)/ActicleType/Validity_From_Date/Expiry_Date/Voucher_Currency('VND')/
  CompanyCode('WCM')/Status('SOLD')/Return(0)`, thêm `AND Source='COUPON'` vào gate (tránh bị
  "lừa" bởi ItemNo trùng của SAP). Thêm **Section 3b mới**: UPDATE không điều kiện (chạy mọi lần
  Lưu, kể cả sửa coupon sau này) đồng bộ lại `ActicleType`/ngày hiệu lực — KHÔNG đụng
  `Status/Return/Enabled` để không phá vỡ trạng thái đã redeem.
- `usp_SetupCoupon_SaveAdvanced`: thêm UPDATE mới đồng bộ `Value`/`VoucherType` (từ
  `@DiscountValue`/`@CpnVchType`) xuống `CpnVchBOMCodeIssue` — 2 field này chỉ có giá trị thật
  sau khi SP này chạy (không phải tham số của `usp_SetupCoupon_SaveIssue`).
- `usp_Voucher_GetByCode`/`usp_Voucher_Redeem`: bỏ hẳn filter `Source='SAP'` (`Code` đã unique
  toàn bảng nên không còn nhập nhằng) — nhận diện và redeem được cả mã Coupon. Redeem thành công
  nay thêm `Enabled=0` (hành vi MỚI, áp dụng cả 2 Source) — đồng bộ hiển thị "Locked" ở
  `usp_SetupCoupon_GetCodes` (POS.Web). Thêm chặn `Value IS NULL` khi validate amount (đóng lỗ
  hổng: coupon chưa chạy `SaveAdvancedAsync` có `Value` NULL → so sánh với NULL luôn UNKNOWN,
  amount bất kỳ sẽ lọt qua nếu không chặn tường minh).
- Không đổi C# — toàn bộ input đã là tham số sẵn có của các SP liên quan
  (`CouponRepository.cs`, `VoucherCodeRepository.cs`, `SAPService.cs`, `SAPController.cs`).
  Không DTO nào đổi field → contract test (`VoucherStatusResponse_locked`, `CouponCodeDto_locked`)
  giữ nguyên, vẫn xanh.
- `docs/architecture/database-schema.md`: cập nhật mô tả cột `CpnVchBOMCodeIssue` (nhiều field
  nay dùng chung 2 Source thay vì chỉ SAP), 5 SP đã sửa hành vi. `docs/ROLLOUT.md` §D6.1: checklist
  chạy 4 script theo đúng thứ tự (ItemNoHardening trước tiên).

**Rủi ro đã rà soát nhưng KHÔNG sửa trong đợt này** (mức ưu tiên thấp, dead path hiện tại):
`usp_SetupCoupon_GetDetail.QuantityCode`, `usp_SetupCoupon_GetList.QtyCoupon`,
`usp_SetupCoupon_Delete` guard đều lọc `WHERE ItemNo=@ItemNo` không kèm `Source` — cùng loại rủi
ro va chạm ItemNo giữa 2 nguồn (xác suất cực thấp); `GetList`/`Delete` hiện không được gọi từ
trang `.razor` nào.

**Lưu ý cho session sau:**
- **CHƯA chạy SQL script nào trên DB thật** trong task này — xem `docs/ROLLOUT.md` §D6.1 để chạy
  đúng thứ tự (bắt buộc `CpnVchBOMCodeIssue_ItemNoHardening.sql` trước tiên).
- Nếu sau này `usp_SetupCoupon_GetList`/`usp_SetupCoupon_Delete` được wire lại vào UI, nhớ thêm
  `AND Source='COUPON'` vào các query lọc `ItemNo` (xem mục rủi ro ở trên).

## [2026-07-02] Fix bug IsCheckItem bị hard-code 0 khi tạo coupon mới (usp_SetupCoupon_SaveIssue)

**Layer:** Database (docs/sql)
**Loại:** Bug fix (production — đã xác nhận tồn tại trong bản deploy hiện tại qua `docs/sql/database/CentralMD.sql`)

**Vấn đề:** User báo tích "Áp dụng theo danh sách sản phẩm" + chọn sản phẩm khi phát hành coupon
mới nhưng lựa chọn không lưu được — sau khi trang tự reload, checkbox hiện lại **bỏ tích**.

**Nguyên nhân:** `docs/sql/SetupCoupon_Save.sql` — nhánh INSERT tạo `CpnVchBOMHeader` (coupon
mới) trong `usp_SetupCoupon_SaveIssue` hard-code cột `IsCheckItem = 0` thay vì dùng tham số
`@IsCheckItem` truyền vào. Nhánh UPDATE (sửa coupon đã có) vẫn đúng (`SET IsCheckItem =
@IsCheckItem`). `usp_SetupCoupon_SaveAdvanced` (chạy ngay sau `SaveIssueAsync` trong
`CouponIssuePage.razor`) cũng không SET lại `IsCheckItem` → giá trị `0` bị "khóa cứng" vĩnh viễn
cho coupon tạo mới, bất kể người dùng chọn gì trên UI.

**Thay đổi:** `docs/sql/SetupCoupon_Save.sql` — đổi giá trị insert từ `0` → `@IsCheckItem` ở vị
trí cột `IsCheckItem` trong `usp_SetupCoupon_SaveIssue` (nhánh tạo mới).

**Lưu ý cho session sau:**
- **BẮT BUỘC re-run `docs/sql/SetupCoupon_Save.sql` trên RPOSMasterData** để áp dụng fix (an
  toàn — script có `DROP PROCEDURE IF EXISTS` trước mỗi `CREATE`).
- Chưa xác nhận được liệu bug này có phải nguyên nhân DUY NHẤT khiến `CpnVchBOMLine` trống hay
  không (Lines insert ở SP không phụ thuộc `IsCheckItem`, nên về lý thuyết vẫn ghi độc lập) —
  cần theo dõi thêm sau khi user re-run script và test lại.

## [2026-07-02] Gộp SAP Internal Voucher vào CpnVchBOMCodeIssue (bảng dùng chung Coupon+Voucher)

**Layer:** POS.Infrastructure + POS.Application + POS.ContractTests
**Loại:** Refactor (gộp bảng dùng chung) + Bug fix (thiếu PK/race condition khi tạo voucher)

**Bối cảnh:** Phát hiện `CpnVchBOMCodeIssue` (POS.Web, Setup Coupon) và `Internal_Voucher`
(POS.Api, SAP Voucher real-time) ban đầu tưởng là logic trùng lặp, nhưng phân tích sâu cho thấy
đây là 2 tính năng khác nhau (Coupon = batch-generate, không có redeem trong solution; SAP
Voucher = lifecycle tài chính đầy đủ `SOLD→RDM`). Quyết định (đã chốt với chủ dự án): mở rộng
`CpnVchBOMCodeIssue` thành bảng DÙNG CHUNG cho cả 2 (cột discriminator `Source`), thay vì ép 2
domain khác nhau vào chung 1 Repository/Service.

**Thay đổi:**
- **Schema `CpnVchBOMCodeIssue`** (`docs/sql/CpnVchBOMCodeIssue_ExtendSchema.sql`): thêm cột
  `Source varchar(10) DEFAULT('COUPON')` (`'COUPON'`|`'SAP'`) + toàn bộ cột tài chính từ
  `Internal_Voucher` (`Status, Return, ActicleNo, ActicleType, Value, Voucher_Currency,
  Validity_From_Date, Expiry_Date, CompanyCode, Partner, IsEmployee, PhoneNumber, VoucherType,
  AmountUsed, OrderUsed`); mở rộng `Code` varchar(20)→varchar(50). **Rebuild bảng** để thêm
  `ID IDENTITY(1,1) PRIMARY KEY CLUSTERED` (trước đó KHÔNG có PK, tự tính
  `MAX(ID)+ROW_NUMBER()` — rủi ro race condition khi có traffic SAP real-time) + `UNIQUE FILTERED
  INDEX` trên `Code` (khóa nghiệp vụ thật, trước đó chỉ check ở tầng ứng dụng).
- **SP mới** (`docs/sql/Voucher_Read.sql`, `docs/sql/Voucher_Save.sql`): `usp_Voucher_GetByCode`,
  `usp_Voucher_Create` (idempotent, UPDLOCK/HOLDLOCK — fix race condition của code cũ: check-rồi-
  insert là 2 round-trip riêng, không transaction), `usp_Voucher_Redeem` (TVP
  `dbo.VoucherRedeemTVP`, giữ nguyên business rule + message tiếng Việt của
  `SAPVoucherRepository.RedeemVouchersAsync` cũ). Thay raw SQL bằng SP theo đúng convention dự án.
- **SP Coupon cập nhật** (`docs/sql/SetupCoupon_Save.sql`, `docs/sql/SetupCoupon_Read.sql`):
  `usp_SetupCoupon_SaveIssue` bỏ tự tính `ID` (nay IDENTITY), thêm `Source='COUPON'`;
  `usp_SetupCoupon_GetCodes` thêm filter `Source='COUPON'` (phòng thủ).
- **Code mới**: `IVoucherCodeRepository`/`VoucherCodeRepository`
  (`src/POS.Infrastructure/Repositories/CouponVoucher/`) — thay `ISAPVoucherRepository`/
  `SAPVoucherRepository` (đã XÓA, cùng thư mục `Sap/` rỗng đã xóa). `SAPService` đổi constructor
  dependency sang `IVoucherCodeRepository`; `CreateNewVoucherAsync` gộp check-tồn-tại + insert
  thành 1 lệnh `CreateOrGetAsync` atomic (fix bug cũ: không check giá trị trả về `InsertAsync`).
  **`ISAPService`/`SAPController`/DTO (`VoucherStatusResponse`, `CreateVoucherModel`,
  `VoucherUpdateRequest`) giữ NGUYÊN 100%** — JSON contract với 5.000 POS không đổi.
- **Migrate dữ liệu production**: `docs/sql/CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql`
  (idempotent) di chuyển voucher SAP thật từ `Internal_Voucher` sang `CpnVchBOMCodeIssue`
  (`Source='SAP'`). Sau go-live ổn định: `docs/sql/Internal_Voucher_RenameLegacy.sql` đổi tên
  `Internal_Voucher` → `Internal_Voucher_Legacy` (giữ backup tạm, KHÔNG xóa ngay).
- **Contract test mới**: `tests/POS.ContractTests/JsonFieldContractTests.cs` —
  `VoucherStatusResponse_locked` (DTO này trước đó CHƯA có test khóa field — lỗ hổng guardrail có
  sẵn, bổ sung vì task này động chạm trực tiếp tầng lưu trữ của DTO).
- Cập nhật `docs/architecture/database-schema.md` (schema mới + 3 SP mới + đánh dấu
  `Internal_Voucher` LEGACY), `docs/CURRENT_STRUCTURE.md` (xóa `ISAPVoucherRepository`, thêm
  `IVoucherCodeRepository`), `docs/ROLLOUT.md` §D6 (checklist go-live theo đúng thứ tự script).

**Pattern mới:**
- **Bảng dùng chung + cột discriminator (`Source`)** cho 2 domain nghiệp vụ khác nhau nhưng cùng
  bản chất "mã định danh + trạng thái" — thay vì ép chung 1 Repository/Service (vi phạm SRP) hoặc
  giữ 2 bảng trùng lặp mãi mãi. Mỗi domain vẫn có Repository/Service riêng
  (`ICouponRepository` vs `IVoucherCodeRepository`), chỉ dùng chung storage.
- SP tạo mới **idempotent qua UPDLOCK/HOLDLOCK trong 1 transaction** (thay vì check-rồi-insert 2
  round-trip riêng ở tầng C#) khi cần đảm bảo atomic dưới traffic real-time cao.

**Lưu ý cho session sau:**
- **CHƯA chạy SQL script nào trên DB thật** trong task này — theo convention dự án, SP/schema
  áp dụng thủ công 1 lần trên `RPOSMasterData`. Xem `docs/ROLLOUT.md` §D6 để chạy đúng thứ tự
  trước khi deploy code này lên môi trường có kết nối DB thật.
- **TODO chưa chốt ngày**: lên lịch `DROP TABLE Internal_Voucher_Legacy` sau khi hệ thống ổn định
  2-4 tuần kể từ go-live §D6 (không thuộc phạm vi task này).
- Nếu cần domain "voucher/coupon" mới trong tương lai (vd đối tác khác), cân nhắc tái dùng cột
  `Source` (thêm giá trị enum mới) thay vì tạo bảng riêng, nếu shape dữ liệu tương thích.

## [2026-07-02] UI audit + gộp form CouponIssuePage (Phát hành Coupon)

**Layer:** POS.Web + POS.Application
**Loại:** Refactor (UI audit/gọn hóa form) + Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Promotion/CouponVoucher/CouponIssuePage.razor`:
  - Gộp toàn bộ field của `CouponAdvancedDialog` (UOM, CpnVchType, DiscountType/Value, MaxValue,
    LimitQty/LimitQtyUsed, IsMultiUsed/IsCheckAPI/Blocked) xuống thẳng form chính — bind vào field
    `_advanced` sẵn có. Nút "Cài đặt nâng cao" giữ nguyên code (`OpenAdvancedAsync` + dialog) nhưng
    ẩn qua `_showAdvancedButton=false` (dead code có chủ đích, chưa thiết kế lại chỗ đặt).
  - `SaveAsync()` nay gọi nối tiếp `SaveIssueAsync` → đồng bộ header vào `_advanced` →
    `SaveAdvancedAsync`, mỗi bước tự audit-log riêng (`SetupCoupon` / `SetupCouponAdvanced`).
  - Layout rút gọn qua nhiều vòng audit: 2 `MudCard` → 1 `MudCard` chia 6 nhóm `MudPaper Outlined`
    bo viền → gộp còn 3 nhóm (Thông tin chung / Thời gian hiệu lực + Giới hạn sử dụng / Cấu hình
    mã & giảm giá + Tùy chọn) → bỏ hẳn `MudCardHeader` (title+caption+tooltip) → bỏ `HelperText`
    (hint ngắn gộp vào `Label`, hint dài bỏ hẳn) → tiêu đề mỗi nhóm con đổi sang kiểu "legend lồng
    viền" (`position:absolute` đè lên viền trên `MudPaper`).
  - `MudNumericField` đổi `Variant` theo kiểu dữ liệu C#: `int` (LenCode/CharOfNumber/CharPosition/
    Quantity/LimitQty/LimitQtyUsed) → `Variant.Text`; `double` (DiscountValue/MaxValue) →
    `Variant.Outlined` + `Step="0.1"`.
- `src/POS.Application/Features/CouponVoucher/CouponService.cs`: `SaveAdvancedAsync` — rule
  "Từ ngày không được nhỏ hơn hôm nay" chỉ áp dụng khi tạo mới (`ItemNo` rỗng), không áp khi sửa
  coupon cũ (tránh chặn Lưu vô lý với coupon đang active có ngày bắt đầu trong quá khứ).
- `.claude/skills/web/form-input.md`: thêm §1a (nhóm con bo viền trong 1 `MudCard` + tiêu đề kiểu
  legend lồng viền) và §4a (`MudNumericField` Variant theo kiểu dữ liệu int/double) + anti-pattern
  + dòng tham chiếu.

**Pattern mới:**
- Nhóm con bo viền (`MudPaper Outlined`) + tiêu đề "legend lồng viền" (`position:absolute` +
  `background:var(--mud-palette-surface)`) thay cho tách nhiều `MudCard` khi các nhóm field cùng
  1 entity — đã cập nhật `.claude/skills/web/form-input.md` §1a.
- `MudNumericField` Variant theo kiểu dữ liệu C# (int→Text, double/decimal→Outlined+Step) — đã
  cập nhật `.claude/skills/web/form-input.md` §4a.

**Lưu ý cho session sau:**
- `CouponAdvancedDialog.razor` + `OpenAdvancedAsync()` không còn được gọi từ UI nhưng vẫn tồn tại
  trong code — nếu dọn dẹp sau này, nhớ đây là dead code có chủ đích, không phải sót lại do quên.
- Nếu tạo `MudNumericField` mới ở trang khác: tra kiểu C# của property trước — `int` dùng
  `Variant.Text`, `double`/`decimal` dùng `Variant.Outlined` + `Step` (khác chuẩn cũ "mọi input
  luôn Outlined").
- Rule ngày kiểu "chỉ chặn khi tạo mới, bỏ qua khi sửa" (`string.IsNullOrWhiteSpace(request.ItemNo)`)
  là pattern hữu ích chung cho các validate liên quan ngày hiệu lực khi entity đã tồn tại.

**appsettings sync:** không thay đổi appsettings.

---

## [2026-07-01] Migrate 9.1 Danh mục Bảng giá + 9.3 Setup Giá (Bulk Import)

**Layer:** POS.Web + POS.Application + POS.Infrastructure + POS.Common
**Loại:** Feature (migrate VCM.BLUEPOS PriceController/SetupPriceController)

**Thay đổi:**
- `src/POS.Common/Dtos/Price/PriceListDto.cs`, `PriceSetupDto.cs` (mới): DTO list/filter/import/save/context/result (Newtonsoft).
- `src/POS.Infrastructure/Repositories/Price/IPriceRepository.cs` + `PriceRepository.cs` (mới): reuse SP `GetSalesPriceList`/`_Export` (9.1); `ValidateImportAsync` (TVP inline LEFT JOIN Item/ItemUnitOfMeasure/Barcodes) + `SaveAsync` (SP `usp_SetupSalePrice_Save`, TVP).
- `src/POS.Application/Features/Price/IPriceService.cs` + `PriceService.cs` (mới): **port 100% validate `SetupPriceController.SaveItemPrice`** + build Pkey `{SalesType}-{ItemNo}-{UOM}-{SalesCode}`.
- `src/POS.Web/Components/Pages/Catalog/Price/PricesPage.razor` (9.1: list server-side + filter + Export) + `PriceSetupPage.razor` (9.3 streamlined: import Excel + lưới preview sửa inline + item picker + Lưu + audit) + `Dialogs/PriceItemPickerDialog.razor`.
- `src/POS.{Application,Infrastructure}/DependencyInjection.cs`: đăng ký `IPriceService`/`IPriceRepository`.
- `docs/sql/SetupSalePrice_Save.sql` (mới): 2 TVP (`SetupSalePriceImportTVP`, `SetupSalePriceLineTVP`) + `usp_SetupSalePrice_Save`.
- `_migration/PROGRESS.md`: 9.1 + 9.3 → ✅ DONE.

**Pattern mới:** Bulk import Excel → lưới preview validate + sửa inline → đã cập nhật `.claude/skills/web/SKILLS.md`.

**Lưu ý cho session sau:**
- ⚠️ **PHẢI chạy `docs/sql/SetupSalePrice_Save.sql` trên RPOSMasterData** trước khi dùng 9.3. SP mới ủy quyền phần update cho SP legacy `[dbo].[Setup_SalePrice_Get_ALL]` (phải tồn tại sẵn) — chỉ tự INSERT Pkey mới (Counter=MAX+1, defaults VND/VAT/disc/MinQty=1/VariantCode='').
- **Pkey của 9.3 (SetupPrice) = `{SalesType}-{ItemNo}-{UOM}-{SalesCode}`** — KHÁC 9.2 (PriceController: `{ItemNo}-{UOM}-{SalesCode}-{StartDate:yyyyMMdd}`). Đừng nhầm khi làm 9.2.
- Tên bảng vật lý CentralMD: `SalesPrice` (số ít), `Barcodes` (số nhiều), `Item`, `ItemUnitOfMeasure`.
- SalesCode hiện chỉ Store/ALL (bỏ Region/Channel). 9.2 + StorePriceGroup + inline edit/delete = còn TODO.

**appsettings sync:** không thay đổi appsettings.

---

## [2026-07-01] UI polish + tài liệu luồng Duyệt — Cài đặt CTKM (PromotionSetupPage)

**Layer:** POS.Web
**Loại:** Refactor (UI polish, markup-only) + Tài liệu

**Thay đổi:**
- `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` (editor mode, **chỉ markup — giữ 100% `@code`**):
  - MudTabs: `Outlined` + `SliderColor="Color.Primary"` → icon tabs có gạch chân dưới tab active.
  - Gom nhóm cả 5 tab bằng `MudCard` (CardHeader avatar + title + caption + help tooltip); bảng Buy/Get/Site bọc trong card với MudTable `Elevation="0"`.
  - Tooltip + `HelperText` giải thích field khó; tooltip cột "Loại chiết khấu"/"Giá trị CK"; tooltip điều kiện AND/OR.
  - Validation trực quan: `Required`/`RequiredError` (Tên/Loại/Hình thức bán/Từ-Đến ngày) — KHÔNG chặn Save.
  - Nút Lưu có spinner khi `_saving`; ô "Điều kiện" (Buy/Get) `max-width` 160→240px.
- `docs/web/LOGIC_APPROVE_CTKM.md` (mới): tài liệu kỹ thuật luồng "Duyệt CTKM" UI→Service→Repo→SP `usp_SetupPromotion_Approve`→publish `Setup_Promotion_Insert`→`Offer*`; kèm bảng mapping `SetupPromotion*`→`Offer*`.

**Pattern mới:** Polish thân thiện End-user (MudCard + tooltip + `Required` visual + nút loading v9) → đã cập nhật `.claude/skills/web/ui-migrate-legacy.md` §8.

**Lưu ý cho session sau:**
- MudBlazor v9 **không có** `MudButton Loading` — dùng `MudProgressCircular` trong content theo cờ `_saving`.
- Bọc `MudTable` trong `MudCard` phải đặt `Elevation="0"` cho table (tránh 2 lớp bóng).
- `Required`/`RequiredError` chỉ báo trực quan; validation chặn thật vẫn ở server (SaveAsync không đổi).

---

## [2026-07-01] Migrate 8.3 Danh mục Voucher (Full CRUD) + 8.4 Tra cứu Voucher phát hành

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature

**Thay đổi:**
- `docs/sql/SetupVoucher_Read.sql` / `SetupVoucher_Save.sql` / `SetupVoucher_Delete.sql`: SP mới (CentralMD) — GetList/GetDetail, TVP `VoucherLineTVP` + Save (upsert header + replace lines), Delete. **8.4 KHÔNG tạo SP** — reuse `[dbo].[GetTransCpnVchIssueList]` có sẵn trên CentralSales.
- `src/POS.Common/Dtos/Voucher/SetupVoucherDtos.cs`: DTOs 8.3 (VoucherListFilter/ListItem/Detail/Line/SaveRequest/SaveResult/FormLookup) + 8.4 (VoucherPublishedFilter/Item).
- `src/POS.Infrastructure/Repositories/CouponVoucher/`: `IVoucherRepository`/`VoucherRepository` (CentralMD, Dapper SP) + `IVoucherPublishedRepository`/`VoucherPublishedRepository` (StoreRoutedConnectionFactory per-store) + DI.
- `src/POS.Application/Features/CouponVoucher/`: `IVoucherService`/`VoucherService` (validate serial/ngày/items, item search reuse `GetProductListAsync`) + `IVoucherPublishedService`/`VoucherPublishedService` (thin) + DI.
- `src/POS.Web/.../CouponVoucher/VouchersPage.razor` (8.3 list+CRUD+Export), `VouchersPublishedPage.razor` (8.4 lookup+Export, store picker bắt buộc), `Dialogs/VoucherFormDialog.razor` + `VoucherItemPickerDialog.razor`.
- `tests/POS.ContractTests/JsonFieldContractTests.cs`: khóa field `VoucherListItemDto` + `VoucherPublishedItemDto`.
- `docs/ROLLOUT.md` §D4; `_migration/PROGRESS.md` 8.3/8.4 → ✅.

**Pattern mới:** không (bám pattern 3 lớp của Coupon 8.1/8.2 + per-store read của CentralSaleRepository) → KHÔNG cập nhật SKILLS.

**Lưu ý cho session sau:**
- ⚠️ **`IsCheckItem` NGƯỢC nghĩa giữa Voucher và Coupon:** Voucher `true`=tổng bill (KHÔNG có line), `false`=theo sản phẩm (có line). Coupon thì ngược. Đừng copy nhầm logic giữa 2 module.
- ⚠️ **ItemNo voucher = SỐ THUẦN** seed `70000001` (khác coupon `C7...`). SP chỉ `MAX` trên ItemNo thuần số (bỏ mã 'C...') — nếu không sẽ lỗi CAST như legacy `int.Parse(Max)`.
- Voucher & Coupon **dùng chung bảng `CpnVchBOMHeader`/`CpnVchBOMLine`** — phân tách bằng `NOT EXISTS CpnVchBOMIssueRule` (voucher = không có IssueRule). **Cần DBA xác nhận** quy tắc này + prefix ItemNo + filter "Loại"=ArticleType (đã đánh dấu `// TODO` trong SP).
- 8.4 cần SP `[dbo].[GetTransCpnVchIssueList]` tồn tại trên mọi server CentralSales; Resend-SAP **đã hoãn** (phase sau).

---

## [2026-07-01] Migrate 8.1 Cài đặt Coupon + 8.2 Phát hành Coupon

**Layer:** POS.Common, POS.Infrastructure, POS.Application, POS.Web
**Loại:** Feature

**Thay đổi:**
- `docs/sql/SetupCoupon_Read.sql` / `SetupCoupon_Save.sql` / `SetupCoupon_Delete.sql`: SP mới (CentralMD) — GetList/GetCodes/GetDetail, 2 TVP (`CouponCodeTVP`, `CouponLineTVP`) + CheckCodesExist/SaveIssue/SaveAdvanced, Delete (guard QtyCoupon==0). Legacy dùng EF LINQ (INVENTORY ghi `sp_SetupCoupon_Get` là SAI).
- `src/POS.Common/Dtos/SetupCoupon/SetupCouponDtos.cs` + 2 contract fact.
- `src/POS.Infrastructure/Repositories/CouponVoucher/ICouponRepository`/`CouponRepository` + DI.
- `src/POS.Application/Features/CouponVoucher/ICouponService`/`CouponService` (sinh mã Auto + validate + parse Excel Import) + DI.
- `src/POS.Web/.../CouponVoucher/CouponsPage.razor` (8.1 list+xóa), `CouponIssuePage.razor` (8.2 phát hành Auto/Import + nâng cao + tab mã), `Dialogs/CouponItemPickerDialog` + `CouponAdvancedDialog`.
- `docs/web/coupon-flow.md`: tài liệu kiểm thử QA (12 điểm yếu code E1–E12).

**Pattern mới:** không → KHÔNG cập nhật SKILLS.

**Lưu ý cho session sau:**
- Sinh mã Auto ở tầng Application (C#, thay `Thread.Sleep(1)` legacy bằng offset theo index để mã duy nhất, không block). SP chỉ nhận danh sách mã qua TVP.
- Item picker tái dùng `ICentralMDRepository.GetProductListAsync` (6.1).
- Tài liệu QA `docs/web/coupon-flow.md` liệt kê điểm yếu (dual-write Advanced, audit oldValue sai, mất item ngầm, Quantity không chặn trần…) — dev nên vá dần.

---

## [2026-07-01] Fix BankPosPage/BankPosDetailDialog — sai tên bảng vật lý, SP param sai, crash circuit

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`:
  - `GetBankPOSListAsync`: SP `[dbo].[GetBankPOSList]` KHÔNG có tham số `@Export` (đã tồn tại sẵn từ
    legacy, không phải SP mới) — code cũ truyền dư 1 param → "too many arguments". Sửa lại đúng 6 param
    `@StoreNo,@TextSearch,@BankCode,@Status,@PageSize,@PageNumber`; SP trả `IsOnline`/`Status` dạng text
    tiếng Việt (IIF) và `Counter`/ngày đã format sẵn thành chuỗi — thêm `BankPOSListRow` map riêng rồi
    convert sang kiểu UI cần (xem pattern mới bên dưới).
  - `SaveBankPOSAsync`/`DeleteBankPOSAsync`: sửa tên bảng sai `dbo.POSTerminalBanks` (số nhiều — thực ra
    là tên EF DbSet) → `dbo.POSTerminalBank` (tên bảng vật lý thật, xác minh qua legacy EDMX).
  - `GetBankListForDropdownAsync`: sửa tên bảng sai `dbo.Banks` → `dbo.Bank` (cùng lỗi class với trên).
- `src/POS.Common/Dtos/CentralMD/BankPOSDto.cs`: `BankPOSListDto` thêm `StoreName`, `StatusText`;
  `Counter`/`CreatedDateStr`/`UpdatedDateStr` đổi sang `string?` (khớp kiểu SP thực trả) — giữ `Status`
  là `int` (không đổi) để form Edit round-trip đúng kiểu khi Save.
- `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosPage.razor`: `LoadDataAsync` — bỏ
  `Task.WhenAll` + 1 try/catch chung → await + try/catch riêng từng nguồn (BankPOS list / Store list /
  Bank list) để 1 nguồn lỗi không xoá luôn dropdown Cửa hàng/Ngân hàng; cột "Cửa hàng" hiển thị thêm
  `StoreName`; Excel export cập nhật theo DTO mới.
- `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosDetailDialog.razor`: `OnInitializedAsync` —
  cùng sửa như trên (await + try/catch riêng từng task) vì exception chưa bắt trong lifecycle method
  của dialog làm SẬP LUÔN circuit Blazor Server (không chỉ riêng dialog) → Lưu/Hủy không gọi được nữa,
  console chỉ thấy lỗi phụ `mudResizeListener.js: Cannot send data if the connection is not in the
  'Connected' State` (JS interop đầu tiên bắn ra sau khi circuit đã chết, không phải nguyên nhân gốc).

**Pattern mới:**
- Xác minh tên bảng vật lý qua legacy EDMX (SSDL, không phải CSDL/DbSet pluralized) trước khi viết raw
  SQL nhắm bảng cũ → đã cập nhật `.claude/skills/api/SKILLS.md`
- Map SP trả cột đã format/localize sẵn (khác kiểu bảng vật lý) qua row riêng rồi convert → đã cập nhật
  `.claude/skills/api/SKILLS.md`
- Load nhiều nguồn độc lập trong `OnInitializedAsync` (page lẫn dialog) — await + try/catch riêng từng
  task để tránh crash circuit → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `BankPOSListDto.PartnerId` vẫn khai báo trên DTO nhưng SP gốc người dùng cung cấp KHÔNG SELECT cột
  này — user đã đồng ý tự thêm `B.[PartnerId]` vào SELECT của SP khi deploy; code đã map sẵn field này,
  không cần sửa gì thêm khi SP được cập nhật.
- DB DEV (`RPOSMasterData`) thiếu nhiều SP khác không liên quan BankPOS (`usp_SpecialCombo_GetList`,
  `SP_SALES_BY_STORE_BUSSINESS_DATE`, `GET_REVENUE_ORDER_SALES_BY_STAFF`, `dbo.OptionData`) — user tự
  đồng bộ DB, không phải việc sửa code.
- Khi debug lỗi tương tự (page/dialog "im lặng" không thao tác được, console chỉ có lỗi JS interop
  chung chung) → luôn kiểm tra `D:\ROOT\Logs\POS.Web\Exception\log-{yyyyMMdd}.txt` trước, đây là cách
  nhanh nhất tìm exception gốc thay vì đoán từ console browser.

---

## [2026-07-01] Fix Sidebar (MainLayout) — accordion collapse sai + active highlight trùng

**Layer:** POS.Web
**Loại:** Bug fix

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`:
  - `UpdateExpanded()`: bổ sung 2 route bị thiếu trong điều kiện `Contains(...)` — `/catalog/pos-setup`
    (thiếu trong `_expandCatPos`) và `/catalog/stores` (thiếu trong `_expandCatOrg`). Vì toàn bộ state
    accordion được tính lại từ URI mỗi lần navigate (không giữ trạng thái cũ), thiếu 1 route khiến
    điều hướng tới route đó không match nhánh nào — accordion sụp về false ở MỌI cấp, nhìn như "chọn
    menu thì tất cả menu bị thu lại".
  - Thêm `Match="NavLinkMatch.All"` vào toàn bộ `MudNavLink` (kể cả các link đang comment) — mặc định
    `NavLinkMatch.Prefix` khiến route ngắn (`/promotion/coupons`) bị đánh dấu active luôn khi đang ở
    route dài hơn cùng tiền tố (`/promotion/coupons/issue`), gây 2 leaf link cùng sáng active. Cùng
    lỗi class cũng ảnh hưởng nhóm `/store/revenue*` (chưa được user báo cáo nhưng đã fix luôn).

**Pattern mới:** đã cập nhật ví dụ + anti-pattern trong section "Sidebar nav (MainLayout) — 3 cấp" của
`.claude/skills/web/SKILLS.md` (thêm `Match="NavLinkMatch.All"` vào code mẫu + cảnh báo thiếu route).

**Lưu ý cho session sau:**
- Mỗi khi thêm `MudNavLink` mới vào sidebar, BẮT BUỘC thêm route đó vào đúng điều kiện `Contains(...)`
  tương ứng trong `UpdateExpanded()` — nếu không sẽ tái diễn lỗi accordion collapse toàn bộ.
- `_expandCatPay`/`_expandCatMisc` là dead code (markup 2 group tương ứng đang bị comment) — chưa dọn,
  để nguyên vì không ai yêu cầu, không ảnh hưởng hành vi.

---

## [2026-06-30] Migrate 6.4 — Product Lock (Khóa/Mở khóa sản phẩm theo cửa hàng)

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Feature (migrate VCM.BLUEPOS 6.4 — Central mode only)

**Thay đổi:**
- `src/POS.Common/Dtos/CentralMD/ProductLockDto.cs` (MỚI): 3 DTO — `ProductLockItemDto`, `ProductLockFilter`, `ProductLockSaveDto`
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: +2 method: `GetProductLockListAsync`, `SaveProductLockAsync`
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement 2 methods — JOIN Item+ItemBlock server-side paging; UPSERT `dbo.ItemBlock` trong transaction (Pkey=`"{StoreNo}-{ItemNo}"`)
- `src/POS.Web/Components/Pages/Catalog/Product/ProductLockPage.razor`: replace skeleton → full page — filter (StoreNo bắt buộc, Status, ItemNo/ItemName), MudTable server-side + MultiSelection, chip màu trạng thái, toggle đơn + bulk action, `MudMessageBox @ref` confirm

**Pattern mới:** `MudMessageBox @ref` — confirm dialog đơn giản (thay `IDialogService.ShowMessageBox` không tồn tại trong MudBlazor v9) → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `dbo.ItemBlock.Pkey = "{StoreNo}-{ItemNo}"` — bắt buộc tạo đúng format khi INSERT mới.
- Direct POS DB mode và GrabFood API (6.5) OUT OF SCOPE — để sau; 6.4 chỉ Central DB.
- StoreOperator auto-select store đơn lẻ từ claim; StoreNo bắt buộc chọn trước khi load dữ liệu.
- `GetProductLockListAsync` dùng `BaseRepository.QueryAsync` (không SP, raw SQL với COUNT(*) OVER()).

---

## [2026-06-30] Xác thực request từ POS — PosApiKeyMiddleware (X-API key)

**Layer:** POS.Api
**Loại:** Feature + Pattern mới (Security)

**Thay đổi:**
- `src/POS.Api/Middleware/PosApiKeyMiddleware.cs` (MỚI): middleware validate header `X-API` = MD5(privateKey).ToUpper(); privateKey lấy từ `GetPOSDataSetupAsync()` (Redis cache `MD:POSDataSetup` 12h). Fail-closed: thiếu cả `X-API` lẫn `Authorization` → 401 "Chưa xác thực". Miễn `/health` + `/swagger/*`.
- `src/POS.Api/Program.cs`: thêm `app.UsePosApiKeyAuth()` sau `UseSerilogRequestLogging()`, trước `UseAuthentication()`.

**Pattern mới:** Middleware xác thực X-API (scoped service qua tham số InvokeAsync) → đã cập nhật `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- Scoped service (`ICentralMDRepository`, `IFileLogHelper`) nhận qua THAM SỐ `InvokeAsync`, KHÔNG inject constructor (middleware là singleton).
- `MD5.HashData()` + `Convert.ToHexString()` cho uppercase hex khớp `MD5(...).toUpper()` phía POS.
- ⚠️ Fail-closed: mọi endpoint (trừ `/health`, `/swagger/*`) bắt buộc có `X-API` hoặc `Authorization` — rà soát monitor/script nội bộ trước khi deploy PROD.
- Bearer token validate vẫn **pending** (hiện pass-through nếu có header Authorization).
- Build xanh, DI test xanh (không thêm DI mới — dùng service đã đăng ký).

---

## [2026-06-29] Tối ưu hóa GetFileFromFTP (typeSync=ALL): Parallel + SHA-256 + Redis SP1 cache

**Layer:** POS.Infrastructure, POS.Application, POS.Api
**Loại:** Performance optimization + Security

**Thay đổi:**
- `src/POS.Infrastructure/Files/MasterDataSyncOptions.cs`: thêm `MaxParallelTables = 4` (số bảng SP2 chạy song song)
- `src/POS.Api/appsettings.json`: thêm `MaxParallelTables: 4` vào section `MasterDataSync`
- `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`: thay `foreach` tuần tự → `Parallel.ForEachAsync` (4×); thêm SHA-256 companion file sau atomic publish; xóa `.sha256` cùng zip khi cleanup
- `src/POS.Infrastructure/Repositories/DataSync/SyncRepository.cs`: inject `IRedisService`, cache SP1 metadata (key `MD:SyncTableList`, TTL 3600s)
- `docs/ROLLOUT.md`: cập nhật O1 với Ubuntu/nginx guidance, SHA-256 info, Redis key invalidation
- `CLAUDE.md`: cập nhật section Sync Master Data (MaxParallelTables, SHA-256, Redis SP1)

**Pattern mới:** Parallel.ForEachAsync + SHA-256 companion → đã cập nhật `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- `Parallel.ForEachAsync` an toàn khi mỗi iteration mở `SqlConnection` riêng (không shared state). Precompute `tableIndex` qua `Select((t, idx) => ...)` trước khi parallel để index ổn định.
- File `.sha256` là companion không đưa vào response API (filter `*.zip` không khớp), POS không biết đến nó — xóa cùng zip khi cleanup.
- `MD:SyncTableList` TTL 1h: nếu DBA đổi bảng SyncTableList cần hiệu lực ngay → `DEL MD:SyncTableList` trên Redis.

---

## [2026-06-28] Phase 1+2: IKibanaService → IFileLogHelper + Audit Logging UsersPage & PosMapPage

**Layer:** POS.Web
**Loại:** Refactor (Phase 1) + Feature (Phase 2)

**Thay đổi:**

Phase 1 — Thay toàn bộ IKibanaService bằng IFileLogHelper trong POS.Web:
- `Auth/IAuditLogger.cs` (`DbAuditLogger`): constructor thay `IKibanaService` → `IFileLogHelper`
- `Services/PendingUpdate.cs`: thay 3 Kibana call → FileLogger
- `Services/SqlConsoleService.cs`: thay 4 Kibana call → FileLogger
- 24 `.razor` files (Ops, Admin, Store): thay `@inject IKibanaService KibanaService` → `@inject IFileLogHelper FileLogger`, tất cả call site. Mapping: `LogInfo(fn,e,m)` → `WriteLogs("[{fn}] {e}: {m}")`; `LogException` có ex → `WriteExpLogs(fn, ex)`; `LogException` không có ex → `WriteLogs("[EXCEPTION][fn] msg")`

Phase 2 — Audit logging cho UsersPage và PosMapPage:
- `Admin/Dialogs/UserFormDialog.razor`: `Submit()` trả `DialogResult.Ok(savedUser!)` thay `Ok(true)`; `PasswordHash = string.Empty` để mask hash trước khi serialize
- `Admin/UsersPage.razor`: inject `IAuditLogger`, `AuthState`, `_currentActor`; log `CREATE`/`UPDATE` trong `OpenDialogAsync`; log `LOCK`/`UNLOCK` trong `ConfirmToggleAsync`
- `Ops/Dialogs/PosTerminalEditDialog.razor`: trả `Ok(new PosTerminalSavePayload(...))` thay `Ok(true)`
- `Ops/Dialogs/PosTerminalDetailDialog.razor`: `OpenEditAsync()` forward `result.Data!` thay `Ok(true)` — chained dialog pattern
- `Ops/PosMapPage.razor`: inject `IAuditLogger`, capture `oldJson` trước dialog, log `UPDATE PosTerminal` khi edit thành công
- `Ops/PosTerminalSavePayload.cs` **(mới)**: `record PosTerminalSavePayload(IpAddress, IsEnabled, BillNoseri)` — shared type dùng cho chain dialog forwarding

**Pattern mới:** Chained dialog result forwarding → đã cập nhật `.claude/skills/web/audit-logging.md` (§11)

**Lưu ý cho session sau:**
- Khi dialog lồng nhiều tầng (ViewDialog → EditDialog), dùng shared record + `result.Data!` để forward nguyên payload — không Ok(true)
- `IKibanaService` vẫn còn trong DI (dùng bởi POS.Api/Worker) — chỉ xóa usages trong POS.Web, KHÔNG xóa service registration

---

## [2026-06-28] POSDataSetup CRUD page + Audit Log DB-persistent

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Feature mới + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/POS/Common/CommonDtos.cs`: thêm `POSDataSetupAdminDto` (5 cột: Code, Value, Description, StoreNo, Counter) — tách riêng với `POSDataSetupModel` (contract POS machine, giữ nguyên)
- `src/POS.Infrastructure/Repositories/MasterData/ICentralMDRepository.cs`: thêm 5 CRUD method (`GetPOSDataSetupAdminListAsync`, `GetPOSDataSetupByCodeAsync`, `InsertPOSDataSetupAsync`, `UpdatePOSDataSetupAsync`, `DeletePOSDataSetupAsync`) + `InsertDashboardAuditLogAsync` (ghi audit, try/catch nội bộ)
- `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`: implement đủ 6 method trên; mọi write invalidate Redis key `MD:POSDataSetup`; UpdatePOSDataSetupAsync KHÔNG đụng Counter/Pkey
- `src/POS.Web/Auth/IAuditLogger.cs`: interface `LogAsync(actor, action, entityType, entityKey, oldValueJson?, newValueJson?)` + impl `DbAuditLogger` (ghi `DashboardAuditLog` qua repository + Kibana song song)
- `src/POS.Web/Auth/migration_dashboard_audit_log.sql`: CREATE TABLE `DashboardAuditLog` + 3 index (ActedAt, Actor, EntityType+EntityKey) — idempotent, chạy trên RPOSMasterData trước khi deploy
- `src/POS.Web/Program.cs`: đăng ký `AddScoped<IAuditLogger, DbAuditLogger>()`
- `src/POS.Web/Components/Pages/Ops/Dialogs/PosDataSetupFormDialog.razor` **(mới)**: Add/Edit dialog — Code read-only khi Edit; `DialogResult.Ok(_model)` (DTO đầy đủ, không Ok(true))
- `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor` **(mới)**: `/ops/pos-data-setup` (OpsAndAbove) — KPI 3 cards (pre-computed, không LINQ inline), filter + MudTable + Delete confirm; log đủ 3 CRUD với oldValue/newValue JSON
- `src/POS.Web/Components/Layout/MainLayout.razor`: thêm nav link "POS Data Setup" vào Ops → Cấu hình group
- `.claude/skills/web/audit-logging.md` **(mới)**: rule chuẩn hóa audit log CRUD toàn dự án — 10 section, checklist 12 mục, reference impl, mask nhạy cảm
- `.claude/skills/web/SKILLS.md`: thêm rule #4 bắt buộc đọc audit-logging.md khi có CRUD
- `CLAUDE.md`: thêm Section 16 (Audit Log — rule mandatory + 3 điểm KHÔNG làm)

**Pattern mới:** Audit Log CRUD pattern → đã cập nhật `.claude/skills/web/audit-logging.md`

**Lưu ý cho session sau:**
- `migration_dashboard_audit_log.sql` **chưa chạy trên DB** — phải chạy trước khi test audit trên môi trường thật (thiếu bảng → log fail silently, không crash app)
- Snapshot oldValue cho UPDATE: dùng biến `item` đã có trong page, KHÔNG fetch lại DB
- Dialog phải trả `DialogResult.Ok(_model)` (không `Ok(true)`) để page có newValue cho CREATE/UPDATE audit
- Khi mở rộng audit sang page khác (Users, Stores...): chỉ inject `IAuditLogger`, không cần thêm DI — đã đăng ký global

---

## [2026-06-28] Đồng bộ tài liệu với reorg theo domain

**Layer:** docs
**Loại:** Tài liệu

**Thay đổi:**
- `docs/CURRENT_STRUCTURE.md`: Mục A cây thư mục — Application `Interfaces/`+`Services/` → `Features/{Domain}/`; Infrastructure `Repositories/{MasterData,Sale,Loyalty,Sap}` + `AppServices/{Partner,DataSync}` + `Security/`; thêm `CentralSaleConnectionFactory`, Workers (Heartbeat/HealthState/RptInsert), `ExceptionHandlingMiddleware`, Gift/Winpay controller. Mục B/C: namespace `Features.{Domain}` / `AppServices.{Domain}` (repo giữ nguyên), thêm DI `ISAPService/IGiftService/ISAPVoucherRepository/IRptCentralSaleRepository/IRptReportSaleDetailRepository/CentralSaleConnectionFactory`. Mục E: ghi chú namespace mới
- `docs/WEB_STATUS.md`: cây POS.Web — Store gom `Reports/Transactions/Operations/Dialogs`, Ops/Admin pages mới, Services/Pdf

**Lưu ý cho session sau:** Repository namespace GIỮ NGUYÊN `POS.Infrastructure.Repositories[.Interfaces]` dù folder gom theo domain (tránh đụng consumer). Mục D (chữ ký repo) không đổi vì chỉ move file. CURRENT_STRUCTURE KHÔNG chứa POS.Web (xem WEB_STATUS).

---

## [2026-06-28] POS.Web Security Hardening — config, headers, credentials, SQL Console

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Pattern mới + Bug fix (cấu hình bảo mật)

**Bối cảnh:** Vá theo báo cáo đánh giá bảo mật POS.Web. Production publish thẳng internet, KHÔNG proxy, đang test qua HTTP. Làm tuần tự từng mục, dừng-báo-cáo sau mỗi mục.

**Thay đổi:**
- `Program.cs`: (C2) DetailedErrors tắt ngoài Dev; (C1+H2) section `Security` config-driven 3 mode (`BehindProxy`/`DirectHttps`/`Internet`) + cờ `RequireHttps` tách biệt việc ép HTTPS — cookie `SecurePolicy`/`SameSite`, `UseHsts`/`UseHttpsRedirection`, `UseForwardedHeaders` (chỉ BehindProxy, có KnownProxies/Networks); (M1) middleware security headers + CSP; (C4) hook giải mã `enc:` từ config trước `AddInfrastructure`
- `Components/App.razor`: bỏ inline `onload` font Roboto (dùng `<link rel=stylesheet>`) để CSP `script-src 'self'` không chặn
- `src/POS.Infrastructure/Security/SecretProtector.cs` **(mới)**: AES-256-GCM, token `enc:`, `DecryptTokens` thay phần password trong connection string
- `Components/Pages/Admin/EncryptSecretPage.razor` **(mới)**: `/admin/encrypt-secret` (AdminOnly) — tạo khóa + mã hóa secret
- `Services/SqlConsoleService.cs` + `ISqlConsoleService.cs`: (H1) mask `password/token/secret/...` trong audit + Kibana log; cờ `IsEnabled` (Security:EnableSqlConsole) gate cả service lẫn page
- `Components/Pages/Admin/SqlConsolePage.razor`: chặn UI khi console bị tắt
- `appsettings.{json,Production,Development}.json`: section `Security` (Prod `Mode=Internet`, `RequireHttps=false`, headers on; Dev headers off để không chặn VS Browser Link)
- `docker-compose.yml` + `.env.example`: `POSWEB_SECRET_KEY` qua `.env` (đã gitignore)
- `docs/ROLLOUT.md` **(mới)**: tài liệu trung tâm các bước cấu hình go-live (C4/C1/H2/H1)

**Pattern mới:**
- Security headers + CSP cho Blazor Server + config-driven HTTPS (`RequireHttps`) → `.claude/skills/web/SKILLS.md`
- Mã hóa credentials trong appsettings (`enc:` + config decryption hook, AES-256-GCM) → `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- Khi thêm cấu hình cần thao tác lúc go-live → **tự cập nhật `docs/ROLLOUT.md`** (đã lưu memory).
- CSP `connect-src 'self'` chặn VS Browser Link → security headers TẮT ở Dev (`EnableSecurityHeaders=false`), BẬT ở Prod/UAT.
- C4 mới là **cơ chế**; password thật vẫn plaintext tới khi ops chạy rollout (tạo khóa + mã hóa). Còn `RequireHttps=false` tới khi có TLS.

---

## [2026-06-26] POS.Web UI Polish — DataTable header, sort labels, filter panel chuẩn hóa

**Layer:** POS.Web
**Loại:** Refactor UI / Pattern mới

**Thay đổi:**
- `wwwroot/app.css`: MudTable header override toàn cục — nền `#D9E5F7`, border-bottom 2px navy, `padding: 10px 16px` (header height ~33px cân bằng với body row có chip ~32px), sort button `min-height:unset padding:0`; đổi `--pos-bg-alt` từ `#EEF1F7` → `#D9E5F7`
- `Pages/Admin/UsersPage.razor`: chuẩn hóa cấu trúc page — KPI row 3 cards (tổng/active/locked) + filter panel (search+role+status) + MudTable không có count text
- `Pages/Admin/AuditPage.razor`: xóa ToolBarContent count text; thêm sort cho cột `DecidedAt`
- `Pages/Store/Transactions/TransactionsPage.razor`: xóa inline result summary block + `FormatSummaryVND` helper
- `Pages/Store/Transactions/VoidsPage.razor`: xóa inline result summary block + fields `_distinctVoiders/_selfVoidCount` + `FormatSummaryVND`
- `Pages/Store/Operations/ShiftSummaryPage.razor`: thêm sort cho toàn bộ 9 cột bảng summary (`ShiftNumberSummaryDto`) và 8 cột bảng detail (`EosShiftDto`) — bao gồm nullable DateTime
- `Pages/Store/Reports/RevenueHourlyPage.razor`: thêm sort cho 7 cột — cột `Ngày` sort bằng `SortOrder` (int) thay vì `TimeLabel` (string)
- 9 pages khác: fix filter panel `Elevation="2"` → `Elevation="1"`

**Pattern mới:**
- MudTable header CSS override toàn cục (1 block CSS, không cần sửa Razor) → `.claude/skills/web/datatable.md`
- Sort nullable DateTime: `x => x.NullableProp ?? DateTime.MinValue` → `datatable.md`
- Sort pre-formatted string date: dùng `SortOrder` (int), không sort `TimeLabel` (string) → `datatable.md`
- Filter panel luôn `Elevation="1"`; DataTable luôn `Elevation="2"` → `datatable.md` anti-patterns
- Không dùng inline result summary text — KPI cards thay thế → `datatable.md` anti-patterns

**Lưu ý cho session sau:** Khi tạo page mới với MudTable, KHÔNG thêm block `@if (!_loading && _items.Count > 0) { <div>Tìm thấy...</div> }` — đây là anti-pattern đã được xác nhận; dùng KPI cards hoặc `InfoFormat` của `MudTablePager`.

---

## [2026-06-26] Guardrails kiến trúc (Giai đoạn 1) + chuyển hướng Greenfield

**Layer:** tests/POS.ContractTests, POS.Api, CLAUDE.md
**Loại:** Pattern mới + Tài liệu + Quyết định kiến trúc

**Bối cảnh:** Đánh giá kiến trúc tổng thể (Clean Architecture đã chuẩn). Quyết định **ngừng
migrate từ dự án cũ (.NET 4.6 / `POS.Backend`)**, chuyển sang **phát triển mới (greenfield)**.
Bổ sung guardrails **additive** (không đụng logic hiện tại) để mở rộng nhiều module an toàn.

**Thay đổi:**
- `tests/POS.ContractTests/JsonFieldContractTests.cs` + `JsonContract.cs` *(mới)*: contract test
  khoá tên field JSON cho DTO response trọng yếu (`ResultResponse`, `InfoMemberModel`,
  `PaymentEntryLoyalty`, `GiftDataRespone`) — đổi/thêm/xoá field → test đỏ.
- `tests/POS.ContractTests/DependencyInjectionTests.cs` *(mới)*: DI validation — mọi phụ thuộc
  `POS.*` của controller + implementation đã đăng ký phải có trong container; chỉ đọc service
  descriptor, không cần Redis/SQL.
- `tests/POS.ContractTests/ExceptionMiddlewareTests.cs` *(mới)*: khoá hành vi exception
  middleware (HTTP 500 + `ResultResponse` PascalCase + bỏ field `Data`).
- `tests/POS.ContractTests/POS.ContractTests.csproj`: thêm ProjectReference
  Common/Application/Infrastructure/Api + FrameworkReference `Microsoft.AspNetCore.App` +
  `Newtonsoft.Json`; xoá `UnitTest1` placeholder.
- `src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs` *(mới)* + `Program.cs`: global
  exception middleware đầu pipeline → trả đúng `ResultResponse` (`DefaultContractResolver`
  PascalCase + `NullValueHandling.Ignore`).
- `CLAUDE.md`: thêm §Guardrails & Testing + §Quy ước phát triển mới (Greenfield); gỡ nội dung
  khung "migrate" (bảng Mapping Namespace cũ→mới, tham chiếu source cũ, framing MemoryCacheService
  code cũ); sửa ghi chú Swagger lỗi thời (đã bật ở Development).

**Pattern mới:**
- Contract test khoá tên field JSON (reflection `[JsonProperty]`) — bảo vệ hợp đồng 5.000 POS.
- DI validation test (descriptor-only, infra-free) — chặn "quên `AddScoped`" lúc test.
- Global exception middleware giữ contract `ResultResponse`.
- Convention feature greenfield: `Features/{Domain}/` + AppService 3 lớp.

**Lưu ý cho session sau:**
- Dự án **không còn migrate** từ `POS.Backend` — mọi nghiệp vụ là code mới; contract JSON 5.000
  POS **vẫn giữ** cho endpoint hiện hữu.
- Khi cố ý đổi field DTO đã khoá → cập nhật danh sách trong `JsonFieldContractTests.cs` **cùng
  commit**; DTO response mới → thêm `[Fact]` khoá field.
- Chạy `dotnet test tests/POS.ContractTests` trước commit (hiện 9 test, build 0 error).
- Còn để dành (Giai đoạn 2): gom file theo `Features/{Domain}/` khi ~30+ service; mapping
  helper / API versioning khi cần.

---

## [2026-06-26] Flat UI + Density Standard — POS.Web design system chuẩn hóa

**Layer:** POS.Web
**Loại:** Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: `DefaultBorderRadius` 8px → 4px; `LineHeight` "1.6" → "1.45"; Shadow array E1-E5 → hairline `"0 0 0 1px rgba(26,43,69,0.12)"` (E6+ giữ nguyên bảo vệ dropdown/dialog)
- `src/POS.Web/wwwroot/app.css`: thêm Flat UI overrides (input border thin), dropdown/sidebar spacing (5px/4px desktop), button-input alignment (`align-self: flex-end` sm+), KPI equal height, mobile safety block (40px min tap targets + LineHeight 1.5)
- `src/POS.Web/Components/Layout/MainLayout.razor`: `MudAppBar Dense="true"` + `MudNavMenu Margin="Margin.Dense"`
- `src/POS.Web/Components/Pages/Store/TransactionDetailDialog.razor`: thêm `Dense="true"` vào 2 MudTable
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: `MudGrid Spacing="3"` (KPI + chart)
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`: `MudGrid Spacing="3"` (2 grid)
- `src/POS.Web/Components/Pages/Store/TopProductPage.razor`: `MudGrid Spacing="3"`
- `CLAUDE.md`: thêm §14 MudBlazor Flat UI Standard + §15 Density Standard

**Pattern mới:** Flat UI shadow array (E1-E5 hairline, E6+ unchanged) + Density Standard (LineHeight/Spacing/Dense) → `.claude/skills/web/theming.md`

**Lưu ý cho session sau:**
- E6+ shadow KHÔNG được làm phẳng — MudPopover (MudSelect/Autocomplete) dùng E8, MudDialog dùng E12; làm phẳng → dropdown dính bẹt vào nền.
- CSS global trong `app.css` đã xử lý mobile tap targets — KHÔNG thêm lại `@media (max-width:599.98px)` cho từng component.

---

## [2026-06-25] Production nginx — fix Blazor Server circuit crash (store combobox hang)

**Layer:** POS.Web + nginx config
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `nginx/pos-web.conf`: tăng buffer 64KB → 256KB (`proxy_buffers 8 32k`); thêm `location /_blazor` riêng với `proxy_read_timeout 86400s` + `X-Accel-Buffering "no"`; thêm `X-Accel-Buffering "no"` vào `location /`
- `src/POS.Web/Program.cs`: `DetailedErrors` đọc từ `WebApp:EnableDetailedErrors` config (không hardcode `IsDevelopment()`) — bật/tắt diagnostics không cần deploy lại code
- `src/POS.Web/appsettings.Production.json`: thêm `"EnableDetailedErrors": true` (tạm thời để diagnose — tắt sau khi xác nhận fix)
- `.claude/skills/web/deployment.md`: cập nhật nginx pattern với checklist đầy đủ + anti-patterns

**Pattern mới:** `nginx Blazor Server production-hardened (/_blazor + buffer + X-Accel-Buffering)` → `.claude/skills/web/deployment.md`

**Lưu ý cho session sau:**
- nginx buffer `4×16k = 64KB` quá nhỏ cho Blazor SSR — production cần `8×32k = 256KB`. `proxy_buffering off` không đủ; phải thêm `add_header X-Accel-Buffering "no"` để tắt nginx internal buffer layer.
- Sau khi diagnose xong production → đổi `EnableDetailedErrors` về `false` trong `appsettings.Production.json`.

---

## [2026-06-25] Store Filter UX — DatePicker click-to-open + đồng nhất font size

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: thêm `Body1 = new Body1Typography { FontSize = "0.875rem" }` — fix font dropdown/autocomplete/picker popup từ 16px → 14px, đồng nhất với DataTable và filter labels
- `src/POS.Web/Components/Pages/Store/` (7 file, 13 MudDatePicker): bỏ `Editable="true"`, thêm `AutoClose="true"` → click ô text mở calendar ngay; chọn xong tự đóng
- `.claude/skills/web/theming.md`: thêm rule bắt buộc Body1 override + giải thích Default không cascade
- `.claude/skills/web/SKILLS.md`: thêm 2 anti-pattern (Body1 missing, MudDatePicker Editable); fix `ResetValueOnEmptyText="true"` bug trong Store Selector snippet

**Pattern mới:**
- `MudDatePicker click-to-open: AutoClose="true" (bỏ Editable)` → `.claude/skills/web/SKILLS.md`
- `PosTheme Body1 typography bắt buộc` → `.claude/skills/web/theming.md`

**Lưu ý cho session sau:**
- `Default.FontSize` trong MudBlazor theme KHÔNG cascade xuống `Body1` — mỗi khi tạo theme mới BẮT BUỘC thêm `Body1 = new Body1Typography { FontSize = "..." }` riêng.
- Mọi `MudDatePicker` trong filter panel dùng `AutoClose="true"` (không `Editable`) — click text = mở calendar, không cần click icon. Store Selector (MudAutocomplete) KHÔNG dùng `ResetValueOnEmptyText="true"` (circuit crash).

---

## [2026-06-24] TopProductPage — Top sản phẩm bán chạy + tối ưu BA/BI

**Layer:** POS.Common, POS.Infrastructure, POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Common/Dtos/RptCentralSale/`: 4 DTO mới — `TopProductKpiDto` (RS1), `TopProductDto` (RS2), `TopProductCategoryDto` (RS3), `ProductOrderLineDto` (drill-through)
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` + interface: `GetTopProductAsync` (QueryMultiple 3 RS + cache Pattern 4 + timeout 45s, `@CategoryNo=NULL`) + `GetProductOrderLinesAsync` (SQL trực tiếp ReportSaleDetail theo ItemNo, TOP 500)
- `src/POS.Web/Components/Pages/Store/TopProductPage.razor` (mới, `/store/top-product`): filter store/ngày/Top-N/sort + compare, KPI 3 card, **CSS bar list** (thay horizontal bar/treemap), MudTable drill-through. Pattern scale-safe (guard re-entrancy, CTS, OnAfterRenderAsync, clamp 92 ngày). **BA/BI:** thêm cột Giá TB/Trả%/Độ phủ/Giảm% (từ field SP đã tính) + cột Biến động (Δ hạng/NEW/Δ DT%) join kỳ trước client-side
- `src/POS.Web/Components/Pages/Store/ProductOrdersDialog.razor` (mới): dialog drill-through hóa đơn của 1 SP
- `src/POS.Web/Components/Layout/MainLayout.razor`: NavLink "Top sản phẩm bán chạy" + auto-expand nhóm Báo cáo
- `docs/migrations/rpt_salebytime_perf.sql`: bổ sung index `(ItemNo, OrderDate)` cho drill-through + đính chính cột ngày thực tế = `OrderDate`

**Pattern mới:**
- `CSS bar list (horizontal) — thay horizontal/treemap MudBlazor không có` → `.claude/skills/web/charts.md`
- `MudTable row → drill-through dialog` + `Tận dụng dữ liệu SP đã tính + so sánh cấp dòng (BA/BI)` → `.claude/skills/web/reports.md`

**Lưu ý cho session sau:**
- MudBlazor v9 KHÔNG có horizontal bar 2 trục / treemap → dùng CSS bar list; format `width:%` BẮT BUỘC `InvariantCulture` (culture VN dùng dấu phẩy → phá CSS).
- Trước khi thêm SP/cột mới cho 1 chỉ số: kiểm tra SP report hiện tại **đã trả cột đó chưa** — nhiều cột bị page vứt (return qty, avg price, order count, discount). Compare cấp dòng = giữ list prev + join theo khóa, đừng chỉ dùng prev cho KPI tổng.
- Chiều "Ngành hàng" của `sp_ReportTopProduct` đang trả NULL (chưa JOIN Item master) → page ẩn tạm filter/treemap/KPI category; RS3 vẫn map sẵn (`TopProductCategoryDto`), bật lại dễ khi SP có JOIN.
- `CURRENT_STRUCTURE.md` KHÔNG track repo `RptCentralSale` → bỏ qua Bước 3 (như các task RptCentralSale trước).

---

## [2026-06-23] RevenueHourlyPage — tối ưu data path + page cho quy mô 10M dòng ReportSaleDetail

**Layer:** POS.Infrastructure, POS.Web, POS.Common
**Loại:** Pattern mới + Refactor (tối ưu hiệu năng)

**Bối cảnh:** đánh giá `RevenueHourlyPage` + cách lấy dữ liệu qua `sp_ReportSaleByTime` khi `ReportSaleDetail` (bảng mart, worker rebuild mỗi 60s) lớn tới ~10M dòng.

**Thay đổi:**
- `src/POS.Common/Dtos/RptCentralSale/SaleByTimeKpiDto.cs` + `SaleByTimeSeriesDto.cs`: DTO map RS1 (KPI) / RS2 (series) của SP (đã có từ vòng tạo trang)
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` + `IRptCentralSaleRepository.cs`: `GetSaleByTimeAsync` thêm **Redis cache** (key `MD:RptSaleByTime:*`, TTL 180s nếu range có hôm nay / 12h nếu quá khứ), tách cache KPI khỏi series, tham số `includeKpi`, timeout riêng 45s (thay 120s); inject thêm `IRedisService`
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`: (1) guard `if (_loading) return;` + disable preset chips khi load; (2) `CancellationTokenSource` theo vòng đời + `IDisposable` + truyền `ct`; (3) hoãn auto-load khỏi prerender → `OnAfterRenderAsync(firstRender)`; (4) clamp 92 ngày khi xem all-stores; (6) line chart luôn hiện legend. Call DAY xin KPI, HOUR/WEEKDAY/compare `includeKpi:false`
- `docs/migrations/rpt_salebytime_perf.sql`: script chờ DBA — index `ReportSaleDetail(StoreNo, SaleDate)` INCLUDE cột đo + thêm `@IncludeKpi BIT` cho SP

**Pattern mới:**
- `Cache report query (SP) — TTL theo độ mới + bỏ result-set dư` → `.claude/skills/cache/SKILLS.md` (Pattern 4)
- `Report page an toàn ở quy mô lớn` (re-entrancy guard + CTS + defer-prerender + clamp all-stores) → `.claude/skills/web/reports.md`

**Lưu ý cho session sau:**
- Report SP nặng → BẮT BUỘC cache Redis TTL-theo-độ-mới + timeout riêng (KHÔNG dùng 120s chung), không cache vô thời hạn.
- Report page tự load → `if (_loading) return;` + CTS + auto-load trong `OnAfterRenderAsync(firstRender)` (KHÔNG trong `OnInitializedAsync` vì prerender chạy 2 lần). Disable cả **preset chips**, không chỉ nút.
- `CURRENT_STRUCTURE.md` KHÔNG track repo `RptCentralSale` (cả `GetDetailRevenueSales`/`GetSalesByCategory` cũng vắng) → đã bỏ qua Bước 3 để không tạo entry mồ côi.
- Đòn bẩy lớn nhất cho cold-cache lần đầu là **index DB** — còn chờ DBA chạy `docs/migrations/rpt_salebytime_perf.sql`.

---

## [2026-06-23] Chuẩn hóa DataTable → MudTable + tách SKILLS.md web + store combobox

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Refactor + Pattern mới (đảo ngược pattern cũ)

**Thay đổi:**
- **Chuyển TOÀN BỘ DataTable từ `<table class="pos-table">` + `PosTableBase<T>` → `MudTable<T>`** (11 page): TransactionsPage, EosShiftsPage, UsersPage, AuditPage, RevenueHourlyPage, DataRawLogPage, LogsPage, DetailRevenuePage (ServerData), SqlConsolePage (cột động), PosMapPage (từ MudDataGrid); sửa header anti-pattern RevenuePage + PosMapPage
- `src/POS.Web/Components/Shared/PosTableBase.cs`: **ĐÃ XÓA** (MudTable có sort/paginate built-in)
- `src/POS.Infrastructure/Repositories/CentralMDRepository.cs` + `ICentralMDRepository.cs`: thêm `GetStoreListAsync()` — query bảng Store (No+Name), cache Redis `MD:StoreList` 12h
- Store combobox 4 page (TransactionsPage, DetailRevenuePage, SalesByCategoryPage, EosShiftsPage): `MudAutocomplete<StoreDto>` hiển thị "StoreNo – Name", tìm theo mã + tên (thay `MudAutocomplete<string>` chỉ có mã)
- `src/POS.Web/Components/Pages/Store/TransactionDetailDialog.razor`: cột "Mô tả" lấy `TenderTypeName` (thay `ReferenceNo`); table → Default size; nút Đóng → Outlined/Secondary
- **Tách `.claude/skills/web/SKILLS.md` (1136 → 613 dòng)** thành 6 file con: `filter-store.md`, `datatable.md`, `charts.md`, `reports.md`, `theming.md`, `deployment.md` + bảng index "Skill con — đọc khi cần"
- `CLAUDE.md` §10.B + `.claude/skills/web/SKILLS.md`: cập nhật chuẩn DataTable = MudTable

**Pattern mới:** `MudTable<T> — DataTable chuẩn` (client/server/dynamic/footer) → đã cập nhật `.claude/skills/web/datatable.md`. **THAY THẾ** pattern `PosTableBase<T>` cũ (changelog 2026-06-18).

**Lưu ý cho session sau:**
- DataTable mới **BẮT BUỘC** dùng `MudTable` (`MudTableSortLabel` + `MudTablePager`). KHÔNG còn `PosTableBase`/`pos-table` (trừ pivot report `rpt-pivot-table` vẫn raw table).
- Server-side paging: `MudTable @ref + ServerData` + `_table.ReloadServerData()` (KHÔNG gọi LoadDataAsync thủ công). Note cũ ở entry 2026-06-23 DetailRevenue (MudPagination Selected/SelectedChanged) đã lỗi thời.
- Store picker: dùng `MdRepo.GetStoreListAsync()` + `MudAutocomplete<StoreDto>`, KHÔNG dùng `GetStoreSetConfigAsync()` (không có Name). Xem `filter-store.md`.
- SKILLS.md web giờ là index — đọc file con tương ứng khi cần, tránh đọc cả file.

---

## [2026-06-23] Sidebar refactor — Ops tách 2 sub-group + bỏ icon cấp 3

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tách "Vận hành" Ops từ flat 6 links → 2 MudNavGroup con (Giám sát: Health/POS map/Alerts/Queues; Nhật ký: Logs/DataRaw Log); xóa `Icon="..."` khỏi toàn bộ 12 MudNavLink cấp 3 trong Store; thêm `_expandOpsMonitor` + `_expandOpsLog` + cập nhật `UpdateExpanded()`

**Pattern mới:** Sidebar 3-cấp — icon chỉ ở cấp 1 (section) và cấp 2 (sub-group), cấp 3 (leaf MudNavLink) không có icon → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:** Khi thêm trang Ops mới: nếu thuộc monitoring (health/status) → vào sub-group Giám sát; nếu thuộc logs/audit → vào sub-group Nhật ký. Leaf links KHÔNG được thêm `Icon=`.

---

## [2026-06-23] DetailRevenuePage — Báo cáo doanh thu chi tiết + menu sidebar refactor

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Feature mới + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tổ chức lại menu "Cửa hàng" thành 3 nhóm con (Vận hành, Giao dịch, Báo cáo); cập nhật `UpdateExpanded()` để auto-expand nhóm con
- `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor` *(tạo mới)*: Page báo cáo doanh thu chi tiết — 11 filters (từ/đến ngày, cửa hàng, tìm kiếm, loại đơn, hình thức bán, đối tác, VAT, đơn hàng gốc, thu ngân) + data table 21 cột + server-side pagination (50 rows/page) + Kibana + console logging
- `src/POS.Web/Components/Pages/Store/BusinessDayPage.razor` *(tạo mới)*: Stub — Ngày kinh doanh
- `src/POS.Web/Components/Pages/Store/ShiftSummaryPage.razor` *(tạo mới)*: Stub — Tổng kết ca
- `src/POS.Web/Components/Pages/Store/RefundsPage.razor` *(tạo mới)*: Stub — Hoàn trả
- `src/POS.Web/Components/Pages/Store/VoidsPage.razor` *(tạo mới)*: Stub — Hủy GD
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor` *(tạo mới)*: Stub — Doanh thu theo giờ
- `src/POS.Web/Components/Pages/Store/PaymentBreakdownPage.razor` *(tạo mới)*: Stub — Phân tích thanh toán
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`: Thêm parameter normalization + detailed console logging (FromDate, ToDate, filters, page info, result count)
- `src/POS.Common/Dtos/RptCentralSale/DetailRevenueSalesDto.cs` *(đã tồn tại)*: 40 properties (ngày, giờ, số đơn, CH, POS, thu ngân, loại đơn, barcode, mã SP, tên SP, ĐVT, SL, đơn giá, giảm giá, thuế%, thuế VND, thành tiền, hình thức bán, đối tác, KM, coupon)
- `src/POS.Infrastructure/Repositories/Interfaces/IRptCentralSaleRepository.cs` *(đã tồn tại)*: Interface `GetDetailRevenueSalesAsync()` với 11 parameters + pageSize/pageNumber
- `src/POS.Infrastructure/DependencyInjection.cs` *(đã cập nhật)*: Line 41 — `AddScoped<IRptCentralSaleRepository, RptCentralSaleRepository>()`

**Pattern mới:**
1. **Server-side pagination với MudPagination** — dùng `Selected` + `SelectedChanged` event (KHÔNG `@bind-Selected`) để tránh conflict; phân biệt với TransactionsPage (client-side PosTableBase)
   - File: `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor`
2. **Menu sidebar nested MudNavGroup** — 3 cấp: parent → 3 sub-group → items; auto-expand theo URL pattern
   - File: `src/POS.Web/Components/Layout/MainLayout.razor`
3. **Tách DTO/Repository cho báo cáo (Rpt prefix)** — `RptCentralSale/` folder + `IRptCentralSaleRepository` riêng khỏi `ICentralSaleRepository` để tránh coupling với POS.Api
   - Files: `src/POS.Common/Dtos/RptCentralSale/`, `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`

**Lưu ý cho session sau:**
- DetailRevenuePage phục thuộc `[dbo].[RPT_GET_DETAIL_REVENUE_SALES_LIST]` SP trên RPOSCentralSales DB — nếu SP không trả data, kiểm tra: FromDate/ToDate format, StoreNo not empty, SalesType="-1" default; test SP trực tiếp với tham số tương ứng
- Menu sidebar UpdateExpanded() phải cover tất cả route mới (`/store/revenue-detail` đã được thêm vào dòng 156)
- Server-side pagination event (`SelectedChanged`) phải gọi `ReloadPageAsync(int newPage)` để gọi lại SP với page number mới (0-based)
- Responsive UI: Filter fields stack dọc trên mobile (xs), nút Tìm/Xóa full-width (`FullWidth="true"`)

---

## [2026-06-19] SAPController — migrate Internal Voucher APIs + business logic fixes

**Layer:** POS.Api, POS.Application, POS.Infrastructure, POS.Common
**Loại:** Feature + Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Api/Controllers/SAPController.cs`: Thêm `CheckReturnVoucher`, `UpdateReturnVoucher`; giữ `CheckVoucher`, `CreateNewVoucher`, `CreateReturnVoucher`, `RedeemCpnVch`
- `src/POS.Application/Interfaces/ISAPService.cs`: Thêm `UpdateReturnVoucherAsync`
- `src/POS.Application/Services/SAPService.cs`: Implement `UpdateReturnVoucherAsync`; fix `CheckVoucherAsync` (RDM→Return="1", EXP status + kiểm tra ngày `Expiry_Date < DateTime.Today`); fix `RedeemCpnVchAsync` (named param `ct: ct` sau khi signature thay đổi)
- `src/POS.Infrastructure/Repositories/Interfaces/ISAPVoucherRepository.cs`: Thêm optional `requiredVoucherType` vào `RedeemVouchersAsync`
- `src/POS.Infrastructure/Repositories/SAPVoucherRepository.cs`: Thêm check VoucherType trong transaction (UPDLOCK); thêm amount validation (0 ≤ AmountRedeem ≤ faceValue); UPDATE per-row với `AmountUsed` + `OrderUsed`
- `src/POS.Common/Dtos/Vouchers/VoucherStatusResponseDto.cs`: Thêm `AmountUsed decimal?`, `OrderUsed string?`
- `src/POS.Common/Dtos/SAP/SAPDto.cs`: Thêm `VoucherUpdateRequest`
- `src/POS.Common/Validation/StringRangeAttribute.cs`: Tạo mới — custom whitelist validation
- `docs/migrations/alter_internal_voucher_add_amountused_orderused.sql`: DDL thêm 2 cột vào `Internal_Voucher`

**Pattern mới:**
- Optional VoucherType filter trong UPDLOCK transaction → `.claude/skills/api/SKILLS.md`
- Named CancellationToken khi thêm optional param vào giữa signature → `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- `Internal_Voucher` cần chạy DDL migration trước khi deploy: `ALTER TABLE ADD AmountUsed DECIMAL(18,2) NULL, OrderUsed NVARCHAR(50) NULL`
- `UpdateReturnVoucher` chỉ cho phép voucher `VoucherType = "BNMH"` (do `CreateReturnVoucher` tạo ra) — check diễn ra trong transaction UPDLOCK
- Khi thêm optional param vào giữa signature → scan callers và thêm `ct: ct` (named) cho CancellationToken

---

## [2026-06-19] RevenuePage — Y-axis auto-scale theo dữ liệu thực tế

**Layer:** POS.Web
**Loại:** Bug fix

**Thay đổi:**
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Thêm `CalcYMax` + `CalcYTick` helpers; set `YAxisSuggestedMax` + `YAxisTicks` trên cả 2 `BarChartOptions` sau khi load data

**Pattern mới:** Y-axis auto-scale cho MudBlazor v9 Bar/Line chart → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `BarChartOptions.YAxisTicks` mặc định = **20** là *khoảng cách giữa tick* (không phải số lượng) → luôn set kèm `YAxisSuggestedMax` khi data là số nhỏ
- `YAxisSuggestedMax` (double?) là gợi ý — MudBlazor tự mở rộng nếu data vượt quá, không bao giờ clip data

---

## [2026-06-19] DataRawJson audit log + tách POS.Worker thành project độc lập

**Layer:** POS.Infrastructure, POS.Api
**Loại:** Feature + Refactor + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Interfaces/ICentralSaleRepository.cs`: Thêm tham số `transactionId` vào `InInsertToTableByJson()`
- `src/POS.Infrastructure/Repositories/CentralSaleRepository.cs`: Refactor `InInsertToTableByJson` dùng try/finally; thêm `InsertDataRawJsonAsync()` private (log vào bảng `DataRawJson`, dùng `directConnectionFactory`); xóa 3 lời gọi `InsertInterfaceErrorAsync` trùng lặp
- `src/POS.Infrastructure/AppServices/KafkaAppService.cs`: Truyền thêm `message.TransactionId`
- `src/POS.Infrastructure/Workers/PosSalesConsumerWorker.cs`: Truyền thêm `msg.TransactionId`
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: Thêm overload `HostApplicationBuilder` + refactor helper `ConfigureSerilogCore` chung — tránh lặp code cấu hình ES/Console
- `src/POS.Infrastructure/DependencyInjection.cs`: Cập nhật comment worker registration
- `src/POS.Api/Program.cs`: Xóa `AddHostedService<PosSalesConsumerWorker>()` — worker đã tách ra
- `src/POS.Worker/` *(tạo mới)*: Project Worker Service — `POS.Worker.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Production.json`
- `Dockerfile.worker` *(tạo mới)*: Multi-stage build dùng `dotnet/runtime:10.0` (không phải aspnet)
- `docker-compose.yml`: Thêm service `worker` (container `pos_worker`, 512MB, `DOTNET_ENVIRONMENT=Production`)
- `POS.slnx`: Thêm `src/POS.Worker/POS.Worker.csproj` vào solution

**Pattern mới:** 
1. `Audit log với try/finally` — `InsertDataRawJsonAsync` pattern trong Repository
2. `POS.Worker project` — Worker Service độc lập với Docker container riêng, hỗ trợ nhiều worker song song qua `AddHostedService<T>()`
3. `SerilogConfiguration dual overload` — cùng 1 extension dùng được cho cả `WebApplicationBuilder` và `HostApplicationBuilder`

**Lưu ý cho session sau:**
- `POS.Worker/Program.cs` KHÔNG gọi `AddApplication()` — worker chỉ cần `AddInfrastructure()` đủ để lấy `ICentralSaleRepository`
- Thêm worker nghiệp vụ mới: chỉ cần thêm class kế thừa `BackgroundService` vào `POS.Infrastructure/Workers/` rồi đăng ký `AddHostedService<T>()` trong `POS.Worker/Program.cs` — không cần project mới
- `DataRawJson` table phải tồn tại trong RPOSCentralSales DB trước khi deploy

---

## [2026-06-19] HealthPage responsive fix + Responsive UI standard vào SKILLS.md

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Ops/HealthPage.razor`: Fix header — `MudStack Row Justify.SpaceBetween` → `div.pos-page-header` (Case B: title + group controls); button thêm `Style="align-self:center; white-space:nowrap"` để không bị stretch theo chiều cao MudSelect+Label; chip container `MudStack Row` → `div.d-flex gap-1 flex-wrap`
- `.claude/skills/web/SKILLS.md`: Thêm section **"Responsive UI — BẮT BUỘC"** — bảng so sánh sai/đúng cho 6 tình huống phổ biến, code mẫu 2 case pos-page-header (A: title+button đơn; B: title+group controls), 4 anti-pattern responsive mới, 1 checklist item nhắc đọc CLAUDE.md §10.G

**Pattern mới:** `pos-page-header Case B — title + group controls` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Khi `MudButton` nằm trong `MudStack Row` cạnh `MudSelect` có `Label`, button sẽ stretch cao bất thường (flex align-items: stretch) — luôn thêm `Style="align-self:center"` vào button để cố định chiều cao.
Responsive UI standard đã có trong cả CLAUDE.md §10 (chi tiết) và SKILLS.md (tóm tắt tra nhanh).

---

## [2026-06-18] Responsive UI Phase 3 — 5 pages/components theo chuẩn mobile

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Drawer responsive init — `IBrowserViewportService.GetCurrentBreakpointAsync()` trong `OnAfterRenderAsync(firstRender)` → drawer đóng trên mobile, mở sẵn trên desktop (≥ md); đổi `IDisposable` → `IAsyncDisposable`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Header `MudStack Row` → `div.pos-page-header` + `pos-page-header-title` + `pos-page-header-btn`; search inner div thêm `flex-wrap`; `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`; summary text `&nbsp;|&nbsp;` → `d-flex flex-wrap gap-3` với 3 `MudText` riêng
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Chip filter container thêm `flex-wrap`

**Lưu ý cho session sau:**
`IBrowserViewportService` inject được trong Blazor Server component — `Breakpoint.Md or Breakpoint.Lg or Breakpoint.Xl or Breakpoint.Xxl` thay vì `>= Breakpoint.Md` để tránh enum so sánh với range values.
Khi sửa `IDisposable` → `IAsyncDisposable`: đổi `Dispose()` → `async ValueTask DisposeAsync()` và `@implements IDisposable` → `@implements IAsyncDisposable`.

---

## [2026-06-18] DataTable standard — PosTableBase\<T\> + EosShiftsPage + sidebar accordion

**Layer:** POS.Web
**Loại:** Feature + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Shared/PosTableBase.cs`: Tạo mới — abstract base class cung cấp sort (single-column), phân trang (PageSize=10), `FormatVND`, `PagedItems`, `TotalFiltered`, `PageCount`
- `src/POS.Web/wwwroot/app.css`: Thêm `.pos-table*` CSS standard (header #EEF1F7/#1A2B45, sort icon ⇅↑↓) + active NavLink highlight (rgba 14% + border-left #3A6FCC)
- `src/POS.Web/Components/Layout/MainLayout.razor`: Sidebar accordion — `NavigationManager.LocationChanged` + `@bind-Expanded` + `IDisposable`; thêm EosShifts nav link
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: Tạo mới — Kết thúc ca bán hàng (filter ngày/store/trạng thái + KPI cards + pos-table); refactored to `@inherits PosTableBase<EosShiftDto>`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: Migrated từ `MudDataGrid` → `@inherits PosTableBase<TransactionListDto>` + `pos-table`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Migrated từ `MudDataGrid + QuickFilter Func<>` → `@inherits PosTableBase<DashboardUser>` + LINQ search với `SearchText` property tự reset `_page = 1`

**Pattern mới:** `PosTableBase<T> — DataTable chuẩn` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Mọi page DataTable mới BẮT BUỘC dùng `@inherits PosTableBase<T>` + `<table class="pos-table">` — KHÔNG dùng `MudDataGrid`.
Khi search filter cần reset page, dùng property C# (`get`/`set { _field = value; _page = 1; }`) thay vì `_searchText` field trực tiếp.

---

## [2026-06-17 20:00] Áp dụng hệ màu DataFlip — PosTheme + CSS variables

**Layer:** POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: Tạo mới — static `MudTheme` với navy primary (#2051A3), sidebar/appbar navy dark (#1B3A5C), teal accent (#1EAA90), semantic status colors, BorderRadius=8px, Button.TextTransform=none
- `src/POS.Web/Components/_Imports.razor`: Thêm `@using POS.Web.Theme` (global cho mọi Layout)
- `src/POS.Web/Components/Layout/MainLayout.razor`: `<MudThemeProvider Theme="@PosTheme.Default"/>`
- `src/POS.Web/Components/Layout/EmptyLayout.razor`: Theme param + bỏ `background:#f0f2f5` hardcode → `var(--mud-palette-background)`
- `src/POS.Web/Components/Layout/MainLayout.razor.css`: Bỏ gradient navy→tím (legacy Blazor template), dùng solid `#1B3A5C`
- `src/POS.Web/Components/Layout/ReconnectModal.razor.css`: Button/spinner dùng `var(--mud-palette-primary)` thay hardcode `#6b9ed2`, `#0087ff`
- `src/POS.Web/wwwroot/app.css`: 28 CSS variables `--pos-*`, scrollbar, `.pos-delta-up/down` utility
- `docs/style-guide.html`: Tạo mới — tài liệu tham chiếu màu (swatches + 6 component mẫu)

**Pattern mới:** `PosTheme.cs — custom MudBlazor Theme` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Trong MudBlazor v9, `Typography.FontWeight` và `LineHeight` là **string** ("600", "1.6"), không phải `int`/`double` — sẽ gây compile error nếu dùng sai type.
`WarningContrastText` phải là màu tối vì #F39C12 (amber) contrast với trắng chỉ 2.4:1 (fail WCAG AA).

---

## [2026-06-17 17:00] Fix deployment POS.Web — blazor.web.js 404 + nginx setup

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Program.cs`: Thêm middleware rewrite `Host: localhost` cho `/_framework/` + **`app.UseRouting()` tường minh** sau middleware (fix root cause: automatic UseRouting chạy trước mọi middleware trong WebApplication .NET 10)
- `src/POS.Web/Components/App.razor`: Google Fonts load non-blocking (`rel="preload"` + `onload`)
- `src/POS.Web/Dockerfile`: `mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app` TRƯỚC `USER $APP_UID` — fix `CryptographicException` khi Docker volume owned bởi root
- `publish/POS.Web/`: Build output self-contained linux-x64 cho nginx deployment

**Patterns mới:** 4 patterns → đã cập nhật `.claude/skills/web/SKILLS.md`:
- `Explicit UseRouting() để middleware chạy TRƯỚC routing`
- `Fix _framework/blazor.web.js 404 từ external IP`
- `nginx config cho Blazor Server`
- `DataProtection keys trong Docker`

**Lưu ý cho session sau:**
Trong .NET 9/10 `WebApplication`, `UseRouting()` tự động chèn vào ĐẦU pipeline — BẮT BUỘC gọi
`app.UseRouting()` tường minh sau bất kỳ middleware nào cần chạy trước routing.
Sau khi deploy nginx, test: `curl -sv -H "Host: <ip>:5001" http://localhost:8080/_framework/blazor.web.js` phải trả 200.

---

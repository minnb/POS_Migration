# FEATURE_Barcode_ANALYSIS.md — Phân tích chức năng Barcode (Legacy VCM.BLUEPOS)

> **Trạng thái**: Tài liệu phân tích (analysis). **Chưa migrate code nào.** Trace đầy đủ từ
> controller → BLO → Data → SP cho domain "Barcode", trích dẫn `file:dòng` cho mọi rule. Tuyệt
> đối không suy diễn logic không có trong code — phần mơ hồ được liệt kê ở mục 6 để hỏi lại.

## 0. Phạm vi đã đọc

Đọc toàn bộ: `BarcodeController.cs` (129 dòng), `BarcodeBLO.cs` (42 dòng), `BarcodeData.cs`
(84 dòng), `Model/Barcode/Barcode.cs` (58 dòng), `Views/Barcode/BarcodeList.cshtml` (254 dòng),
`BaseController.cs` (212 dòng, lớp cha), `DataTablesViewModel.cs`, `Helpers/BarcodeHelper.cs`
(đọc để xác nhận KHÔNG dùng trong domain này), `EF/Central/Barcode.cs`, `EF/Central/BarcodeSetup.cs`,
định nghĩa SP `[dbo].[GetBarcodeList]` trong `Script_Stored_Procedures.sql` (~dòng 676-757),
`packages.config`/`.csproj` (kiểm tra `Spire.Barcode`).

BarcodeController chỉ có **3 action public** — không có Create/Update/Delete/in-tem nào khác
trong domain này.

## 1. Sơ đồ luồng (request → response)

- **`BarcodeList()` [GET, route mặc định]** (`BarcodeController.cs:29-33`, `[DisplayName("Danh
  mục Barcode")]`)
  → không gọi BLO — chỉ `return View()` → trả trang Razor `Views/Barcode/BarcodeList.cshtml`
  (form filter + bảng DataTables rỗng, dữ liệu load bằng AJAX).

- **`GetBarcodeList()` [POST]** (`BarcodeController.cs:35-63`)
  → đọc `Request.Form`: `draw/start/length/sortColumn/sortColumnDirection` (đọc nhưng KHÔNG
    dùng — SP không nhận sort), `searchValue` (đọc nhưng KHÔNG dùng — `searching:false` phía
    client), `BarcodeNo`, `ItemNo` (dòng 38-50)
  → tính `pageSize`, `skip`, `pageIndex = skip/pageSize` (dòng 44-46, không guard `pageSize==0`)
  → `_barcodeBLO.GetBarcodeList(barcodeNo, itemNo, out recordsTotal, pageIndex, pageSize)`
    (`BarcodeController.cs:61`)
  → `BarcodeBLO.GetBarcodeList` (`BarcodeBLO.cs:27-30`, thin wrapper)
  → `BarcodeData.GetBarcodeList` (`BarcodeData.cs:21-53`) — mở `CentralMDPartnerContainer`,
    raw SQL text `"[dbo].[GetBarcodeList] @BarcodeNo,@ItemNo,@Export,@PageSize,@PageNumber"` với
    `@Export=1` (dòng 30-36)
  → SP `[dbo].[GetBarcodeList]` nhánh `@Export=1` (script ~676-734) — JOIN `[dbo].[Barcodes]` (B)
    INNER JOIN `[dbo].[Item]` (I) ON `I.[No]=B.[ItemNo]`, phân trang `OFFSET/FETCH`
  → response `Json(DataTablesViewModel<BarcodeResponseModel> { draw, recordsFiltered=recordsTotal,
    recordsTotal, data })` (`BarcodeController.cs:62`).

- **`ExportBarcodeList(string BarcodeNo, string ItemNo)` [GET qua `FormMethod.Get`, không có
  attribute HTTP tường minh]** (`BarcodeController.cs:65-120`)
  → `_barcodeBLO.ExportBarcodeList(...)` (`BarcodeController.cs:67`)
  → `BarcodeBLO.ExportBarcodeList` (`BarcodeBLO.cs:32-35`, thin wrapper)
  → `BarcodeData.ExportBarcodeList` (`BarcodeData.cs:55-76`) — **cùng SP** `GetBarcodeList` với
    `@Export=2`, `@PageSize=string.Empty`, `@PageNumber=string.Empty` (dòng 63-68)
  → SP nhánh `@Export=2` (script ~736-756) — cùng JOIN, KHÔNG có `OFFSET/FETCH` → trả toàn bộ
    dòng khớp filter
  → nếu `data != null`: build Excel bằng EPPlus, 7 cột cứng (Barcode, Mã sản phẩm, Mã sản phẩm
    PLG, Tên sản phẩm, ĐVT Barcode, Giảm giá(%), Ngày cập nhật — dòng 86-93), style header (dòng
    95-101), ghi thẳng `Response.OutputStream` rồi `Response.End()` (dòng 106-116)
  → response file `.xlsx`, tên `BarcodeList_{yyyyMMddhhmmssffff}.xlsx` (dòng 106,110)
  → nếu `data == null`: `return Json(string.Empty, JsonRequestBehavior.AllowGet)` — không báo lỗi
    rõ ràng (dòng 119).

**Không có** chức năng in tem/barcode-image/PDF trong domain này — chỉ danh mục + xuất Excel.
Thư viện sinh ảnh barcode (`Zen.Barcode`, `Gma.QrCodeNet` qua `Helpers/BarcodeHelper.cs`) tồn tại
trong solution nhưng **không được dùng bởi Controller/BLO/Data của domain Barcode** — chỉ dùng ở
`Views/Order/PrintInvoiceOrderSales*.cshtml` (domain Order, ngoài phạm vi) để vẽ Code128/QR lên
hóa đơn in.

## 2. Business rule / validation / edge case / error handling (đánh số, trích dẫn)

1. **Filter theo substring, không exact match**: SP dùng `CHARINDEX(@BarcodeNo,
   B.[BarcodeNo]) > 0` — nhập bất kỳ chuỗi con nào đều khớp (script ~703,724,750, cả 2 nhánh
   Export).
2. **Filter `ItemNo` có 2 cách khớp**: `CHARINDEX(@ItemNo, RIGHT(REPLICATE('0',8)+I.[No], 8)) >
   0` (khớp theo mã SAP zero-pad 8 ký tự, substring) **HOẶC** `@ItemNo = I.[No2]` (khớp tuyệt đối
   theo mã Phúc Long/PLG) — script ~704,725,751.
3. **`""` nghĩa là bỏ qua filter**: cả `@BarcodeNo=''` và `@ItemNo=''` là điều kiện "không lọc"
   (`OR @BarcodeNo=''`) — Data layer luôn coalesce `null → string.Empty` trước khi gọi SP
   (`BarcodeData.cs:31-32`).
4. **INNER JOIN bắt buộc barcode phải có Item hợp lệ**: `INNER JOIN [dbo].[Item] I ON
   I.[No]=B.[ItemNo]` (script ~702,723,749) — barcode không có Item tương ứng bị loại hoàn toàn
   khỏi kết quả, không có cảnh báo "barcode mồ côi".
5. **Mapping tên cột response dễ nhầm**: response field `ItemNoSAP` = cột `Barcodes.ItemNo` (khóa
   join thực), còn field `ItemNo` = cột `Item.No2` (mã Phúc Long/PLG), **KHÔNG PHẢI** `Item.No`
   (script comment ~711-712,740-741; UI label khớp `BarcodeList.cshtml:75-76`).
6. **Sắp xếp cố định**: `ORDER BY B.[LastDateModified] DESC` cứng trong SP (script ~727,753),
   khớp `ordering:false` phía client (`BarcodeList.cshtml:136`) — `sortColumn`/
   `sortColumnDirection` đọc ở Controller nhưng không dùng (`BarcodeController.cs:41-42`).
7. **Excel export KHÔNG phân trang**: nhánh `@Export=2` không có `OFFSET/FETCH` (script
   ~736-756) — filter rỗng sẽ kéo toàn bộ bảng `Barcodes` JOIN `Item`.
8. **Tổng bản ghi lấy từ cột đầu tiên**: `totalRecord = data.FirstOrDefault().Total`
   (`BarcodeData.cs:43`) — giả định mọi dòng mang cùng `@Total`, chỉ hoạt động khi `data` khác
   rỗng (đã check dòng 38-41).
9. **`recordsFiltered` luôn bằng `recordsTotal`**: Controller gán cùng giá trị cho cả hai
   (`BarcodeController.cs:62`) — SP tính `@Total` đã áp filter sẵn.
10. **Nuốt exception, trả rỗng — `GetBarcodeList`**: `catch (Exception ex)` không log, set
    `totalRecord=0`, trả `List` rỗng (`BarcodeData.cs:48-52`) — lỗi DB bị ẩn hoàn toàn.
11. **Nuốt exception, trả null — `ExportBarcodeList`**: `catch` không log, trả `null`
    (`BarcodeData.cs:72-75`) → Controller `if (data != null)` bỏ qua tạo file → cuối cùng
    `Json(string.Empty)` — người dùng bấm "Xuất Excel" không nhận file, không có thông báo lỗi
    (`BarcodeController.cs:119`).
12. **Nghi vấn bug type-mismatch tham số SQL (chưa verify runtime)**: `ExportBarcodeList` truyền
    `SqlParameter("@PageSize", string.Empty)`/`("@PageNumber", string.Empty)`
    (`BarcodeData.cs:67-68`) trong khi SP khai `@PageSize int, @PageNumber int` (script
    ~681-682) — convert ngầm `'' → int` thường ném "Conversion failed..." → nếu đúng, lỗi này bị
    nuốt bởi rule #11, tính năng Export **có thể luôn thất bại âm thầm trong production** (xem
    câu hỏi §6.3).
13. **Trim input sai điều kiện (nghi vấn bug copy-paste)**: `BarcodeController.cs:56-59` —
    `if (!string.IsNullOrEmpty(barcodeNo)) { itemNo = itemNo.Trim(); }` — điều kiện check
    `barcodeNo` chứ không phải `itemNo`. Hệ quả: nếu `barcodeNo` rỗng nhưng `itemNo` có khoảng
    trắng, `itemNo` KHÔNG được trim; nếu `barcodeNo` non-empty mà `itemNo` là `null` →
    `NullReferenceException` không được bắt.
14. **Rủi ro chia cho 0**: `pageIndex = skip/pageSize` (`BarcodeController.cs:46`) không check
    `pageSize==0` — nếu `length` thiếu/0 sẽ ném `DivideByZeroException`. UI luôn gửi `length`
    (`displayLength:10, lengthChange:false` — `BarcodeList.cshtml:137,139`) nhưng endpoint là
    POST public nên vẫn có thể bị gọi trực tiếp thiếu tham số.
15. **`ExportBarcodeList` không trim input** — khác `GetBarcodeList` (dù có bug ở #13) —
    `BarcodeController.cs:65-67`.
16. **Cột `VariantCode` bị ẩn khỏi UI/Excel nhưng vẫn có trong response JSON của
    `GetBarcodeList`**: comment ở Excel mapping (`:81`), header Excel (`:91`), cột DataTables
    (`BarcodeList.cshtml:79,201-207`) — dữ liệu vẫn được SP trả nhưng không hiển thị/xuất.
17. **Filter theo vendor đã bị tắt**: `--AND I.[VendorNo] = 'PLH'` bị comment trong cả COUNT và
    SELECT của cả 2 nhánh Export (script ~705,726,752) — trước có filter `VendorNo='PLH'`, hiện
    đã tắt (lấy tất cả vendor).
18. **CommandTimeout hardcode** `2*60` (120s) ở cả 2 method Data layer (`BarcodeData.cs:27,62`).

## 3. Model / DTO / Entity liên quan

Tất cả trong `src/legacy/VCM.BLUEPOS.Model/Barcode/Barcode.cs`:

| Class | Dòng | Vai trò | Property chính |
|---|---|---|---|
| `BarcodeResponseModel` | 9-23 | Response `GetBarcodeList` | `ID(int), BarcodeNo, ItemNoSAP, ItemNo(=Item.No2), ItemName, UnitBarcode, VariantCode, DiscountPercent(decimal?), LastDateModified(string, format sẵn từ SQL CONVERT(...,103)), Counter(long?), Pkey, Total(int)` |
| `ExportBarcodeResponseModel` | 25-34 | Response `ExportBarcodeList`/Excel | `BarcodeNo, ItemNoSAP, ItemNo, ItemName, UnitBarcode, DiscountPercent(decimal?), LastDateModified(string)` — thu gọn, không có `VariantCode/Counter/Pkey/Total/ID` |
| `CreateBarcodeListModel` | 36-51 | **Không dùng bởi domain Barcode** — dùng bởi domain **Product** (`ProductBLO.SaveBarcodeList`, `ProductData.SaveBarcodeList`, `ProductController.cs:220,244`) | `BarcodeNo, ItemNo, ShowForItem(int), Description, Blocked(int), LastDateModified(DateTime?), VariantCode, UnitOfMeasureCode, DiscountPercent(decimal?), Counter(long?), Pkey, CreatedUser, CreatedDate(DateTime?)` — xem câu hỏi §6.5 |
| `DataTablesViewModel<T>` | (`VCM.BLUEPOS/Models/DataTablesViewModel.cs:8-14`) | Wrapper JSON DataTables server-side | `draw(string), recordsFiltered(int), recordsTotal(int), data(IEnumerable<T>)` |

EF entity (không dùng qua LINQ bởi `BarcodeData.cs` — chỉ raw SQL/SP; entity chỉ tồn tại phục
vụ EDMX designer):

| Entity | File | Cột chính | Ghi chú |
|---|---|---|---|
| `Barcode` | `EF/Central/Barcode.cs:15-28` | `BarcodeNo, ItemNo, ShowForItem(byte), Description, Blocked(byte), LastDateModified(DateTime?), VariantCode, UnitOfMeasureCode, DiscountPercent(double?), Counter(long?), Pkey` | map bảng `[dbo].[Barcodes]` |
| `BarcodeSetup` | `EF/Central/BarcodeSetup.cs:15-32` | `Id, GroupCode, Barcode, PolicyLen(int?), Pkey, Counter(long?), Status(bool?), CreatedDate(DateTime?), IsRequire(bool?), IsFixedQuantity(bool?), Ref1..Ref5` | **"mồ côi"** — không có BLO/Data/Controller nào trong solution đọc/ghi (xem câu hỏi §6.6) |

## 4. Database / Stored Procedure

| SP | Tham số | Nhánh | Ghi chú |
|---|---|---|---|
| `[dbo].[GetBarcodeList]` | `@BarcodeNo, @ItemNo, @Export, @PageSize, @PageNumber` | `@Export=1` (script ~676-734) | List có phân trang `OFFSET/FETCH`, dùng bởi `GetBarcodeList` |
| `[dbo].[GetBarcodeList]` | (như trên) | `@Export=2` (script ~736-756) | Export — không phân trang, `@PageSize`/`@PageNumber` truyền `string.Empty` dù SP khai `int` (xem rule #12) |

Cả 2 nhánh: `INNER JOIN [dbo].[Barcodes] B` + `[dbo].[Item] I ON I.[No]=B.[ItemNo]`, `@Total`
tính bằng `SELECT COUNT(...)` riêng biệt (không dùng window function).

## 5. Config / hằng số / magic number / phụ thuộc ngầm

1. `CommandTimeout = 2*60` (120s) — `BarcodeData.cs:27,62`, hardcode.
2. `ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial` —
   `BarcodeController.cs:70`.
3. Màu nền header Excel hardcode `"#b1afaf"` — `BarcodeController.cs:100`.
4. Format tên file Excel `"BarcodeList_{yyyyMMddhhmmssffff}"` — `BarcodeController.cs:106` —
   dùng `hh` (giờ 12h) thay vì `HH` (24h), nghi ngờ lỗi format (xem câu hỏi §6.10).
5. DataTables client hardcode: `displayLength:10, lengthChange:false, searching:false,
   ordering:false, orderMulti:false` (`BarcodeList.cshtml:126,134,136-139`) — người dùng không
   đổi được page size/sort/search qua UI dù backend hỗ trợ tham số tương ứng.
6. Static/ambient state kế thừa từ `BaseController` (ảnh hưởng mọi action vì kế thừa):
   `BaseController.ListController` (static, dựng lại mỗi lần khởi tạo controller —
   `BaseController.cs:38,46,170-202`), `MenuPermissionModel.ListMenuPermission` (static, gán tại
   `BaseController.cs:147`), cookie-based auth ngầm (`AuthCookie.CheckLogin`/`CurrentUser` —
   `BaseController.cs:93,114`) — không có `[Authorize]` tường minh trên `BarcodeController`.
7. Phân quyền theo menu chỉ áp dụng cho action có `[DisplayName]`
   (`GetListParentAuthorize()` — `BaseController.cs:203-208`) — trong `BarcodeController` chỉ
   `BarcodeList` có attribute này; `GetBarcodeList`/`ExportBarcodeList` bỏ qua hoàn toàn check
   quyền theo menu (chỉ còn check đã login).
8. `IBarcodeData` không qua DI: `BarcodeBLO` tự `new BarcodeData()` trực tiếp
   (`BarcodeBLO.cs:22-24`), khác `IBarcodeBLO` được đăng ký Autofac
   (`AutofacConfig.cs:75`) và inject vào `BarcodeController` (`:20,23`).
9. Package `Spire.Barcode` 5.9.4` khai trong `packages.config:55` + `.csproj:153-154` nhưng
   **không tìm thấy usage nào** qua grep toàn solution — dependency có vẻ chết.
10. Sinh ảnh barcode/QR thực tế (domain **Order**, không phải Barcode) qua
    `Helpers/BarcodeHelper.cs`: `BarcodeSymbology.Code128` hardcode (dòng 25,42),
    `GetDefaultMetrics(20)`+`Scale=1` hardcode (dòng 27-28,45), thư viện `Zen.Barcode.Core 2.0.0`.
    QR dùng `Gma.QrCodeNet`, `ErrorCorrectionLevel.H` hardcode (dòng 58,81),
    `FixedModuleSize(4, QuietZoneModules.Two)` hardcode (dòng 62,85). `packages.config` có 2 dòng
    khai `Gma.QrCodeNet` khác version (`0.4.1.2` dòng 13, `1.0.0` dòng 14) — khả năng trùng lặp.
11. Không tìm thấy `ConfigurationManager.AppSettings`/`Web.config` key nào đọc trực tiếp trong 3
    file chính của domain này.

## 6. Câu hỏi cho người phụ trách (logic mơ hồ — KHÔNG suy diễn)

1. `BarcodeController.cs:56-59` — điều kiện trim `itemNo` đang check biến `barcodeNo` thay vì
   `itemNo`. Bug copy-paste hay cố ý? Cần xác nhận hành vi mong muốn trước khi port.
2. `BarcodeController.cs:44-46` — `pageIndex = skip/pageSize` không guard `pageSize==0`. Có đảm
   bảo chắc chắn `length` luôn > 0 từ mọi caller không, hay cần thêm validate khi migrate?
3. `BarcodeData.cs:63-68` — truyền `string.Empty` cho tham số `@PageSize`/`@PageNumber` (SP khai
   `int`). Cần verify thực tế (log SQL/chạy thử) xem "Xuất Excel" có đang luôn lỗi âm thầm trong
   production do type-conversion exception bị nuốt hay không — nếu đúng, khi migrate nên coi đây
   là bug cần sửa, không port nguyên trạng.
4. Script SQL — comment `--AND I.[VendorNo] = 'PLH'` lặp lại 3 lần. Bỏ filter vendor là chủ ý
   vĩnh viễn (đa vendor) hay cần tùy chọn bật lại theo tenant/site trong kiến trúc mới?
5. `CreateBarcodeListModel` khai trong `Model/Barcode/Barcode.cs` nhưng chỉ domain **Product**
   dùng. Khi phân chia migrate theo domain, phần "tạo/lưu barcode" nên xếp vào domain Product hay
   Barcode? Cần thống nhất để tránh trùng lặp giữa các nhóm phân tích song song.
6. `BarcodeSetup` (EF entity, gợi ý policy sinh mã barcode: GroupCode/PolicyLen/IsRequire/
   IsFixedQuantity) không có BLO/Data/Controller nào đọc/ghi. Tính năng đã lên kế hoạch nhưng
   chưa implement, bảng deprecated, hay dùng bởi ứng dụng khác (vd POS terminal client) ngoài
   phạm vi source đang xem?
7. `IBarcodeData` định nghĩa nhưng không đăng ký DI — oversight so với pattern DI chuẩn của
   solution (nơi khác dùng Autofac inject qua interface), hay cố ý?
8. Package `Spire.Barcode` không tìm thấy usage nào qua grep toàn solution. Xác nhận có thể bỏ
   hẳn khỏi migration, hay có chỗ dùng dynamic/reflection nằm ngoài phạm vi đã quét?
9. SP tính `@Total` bằng `SELECT COUNT(...)` riêng biệt (chạy lại toàn bộ WHERE) thay vì window
   function `COUNT(*) OVER()` trong cùng query — cần tối ưu khi viết SP mới theo chuẩn
   `usp_{Domain}_{Action}`, hay giữ nguyên pattern cũ?
10. `BarcodeController.cs:106` — dùng `"yyyyMMddhhmmssffff"` (giờ 12h `hh`, không AM/PM) thay vì
    `HH` (24h) cho tên file Excel. Đã từng gây trùng tên file/ticket hỗ trợ nào chưa, hay chỉ là
    lỗi tiềm ẩn chưa phát hiện?
11. `ExportBarcodeList` không trim input `BarcodeNo`/`ItemNo` như `GetBarcodeList` (dù có bug) —
    có nên đồng bộ hành vi validate giữa 2 action khi migrate không?
12. `ExportBarcodeList` không có `[HttpGet]`/`[HttpPost]` tường minh — xác nhận endpoint chỉ được
    gọi qua GET (form `FormMethod.Get`) và không cần chặn POST vì lý do bảo mật/CSRF khi thiết kế
    kiến trúc mới.

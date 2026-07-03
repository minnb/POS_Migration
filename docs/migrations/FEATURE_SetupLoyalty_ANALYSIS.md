# FEATURE_SetupLoyalty_ANALYSIS.md — Phân tích chức năng SetupLoyalty (Legacy VCM.BLUEPOS)

> **Trạng thái**: Tài liệu phân tích (analysis). **Chưa migrate code nào.** Trace đầy đủ từ
> controller → BLO → Data → SP/EF cho domain "SetupLoyalty", trích dẫn `file:dòng` cho mọi rule.
> Tuyệt đối không suy diễn logic không có trong code — phần mơ hồ được liệt kê ở mục 7 để hỏi lại.

## 0. Lệch tên giữa các layer — xác nhận qua constructor injection

> **Quan trọng khi migrate**: tên Controller khác tên BLO/Data. Đây KHÔNG phải lỗi phân tích —
> đã xác nhận trực tiếp qua code.

- Controller: `SetupLoyaltyController` (`src/legacy/VCM.BLUEPOS/Controllers/SetupLoyaltyController.cs:30-38`)
  inject `ILoyaltyBLO setupLoyaltyBLO` (namespace `VCM.BLUEPOS.Business.Loyalty`), gán vào field `_setupLoyaltyBLO`.
- Interface + implementation thật nằm trong `LoyaltyBLO.cs`
  (`src/legacy/VCM.BLUEPOS.Business/Loyalty/LoyaltyBLO.cs:14-124`), class tên `LoyaltyBLO`.
- Bên trong `LoyaltyBLO`, constructor **không inject** `ILoyaltyData` mà **tự `new` trực tiếp**:
  `_data = new LoyaltyData();` (`LoyaltyBLO.cs:39-43`).
- Tầng Data: `LoyaltyData` (`src/legacy/VCM.BLUEPOS.Data/Loyalty/LoyaltyData.cs:48`). Interface
  `ILoyaltyData` tồn tại (`LoyaltyData.cs:22-46`) nhưng **không được dùng để DI ở đâu cả** — dead
  abstraction (xem câu hỏi §7.6).

## 1. Sơ đồ luồng (request → response) — đầy đủ mọi action

> Mỗi bullet: `Action [HTTP method] (file:dòng)` → BLO method (file:dòng) → Data method
> (file:dòng) → SP/bảng/EF entity → response.

- **`SetupLoyalty()` [GET]** (`SetupLoyaltyController.cs:41-45`)
  → `_commonBLO.GetComboxMemberCardType()` (ngoài scope domain này, thuộc `ICommonBLO`)
  → trả `View()` kèm `ViewBag.ListMemberCardType`.

- **`LoadSetupLoyaltyList()` [đọc `Request.Form`]** (`SetupLoyaltyController.cs:47-84`)
  → parse `SearchFromDate`/`SearchToDate` bằng `DateTime.ParseExact("dd/MM/yyyy")` — **không try/catch**
  → `LoyaltyBLO.GetSetupLoyaltyList(...)` (`LoyaltyBLO.cs:44-47`)
  → `LoyaltyData.GetSetupLoyaltyList(...)` (`LoyaltyData.cs:44-103`)
  → SP `[dbo].[GET_GIFT_REDEEM_LOYALTY_LIST] @FromDate,@ToDate,@ItemNo,@Status` qua
    `CentralMDPartnerContainer` (`LoyaltyData.cs:54,59-63`) — **không tìm thấy định nghĩa SP này**
    trong `Script_Stored_Procedures.sql` (xem §7.4)
  → lọc thêm `searchText` bằng LINQ **in-memory sau khi SP trả về** (`LoyaltyData.cs:87-90`)
  → response `Json(DataTablesViewModel<GiftRedeemResponseModel>)`.

- **`_ProductList(...)` [POST, partial view]** (`SetupLoyaltyController.cs:87-94`)
  → chỉ set `ViewBag` rồi trả `PartialView()` — không gọi BLO/Data.

- **`LoadProductList()` [POST]** (`SetupLoyaltyController.cs:97-117`)
  → `pageNumber = skip / pageSize` với `pageSize = length ?? 1` (khác công thức phân trang của
    `LoadSetupLoyaltyList`, xem §7.7)
  → `LoyaltyBLO.GetProductList(...)` (`LoyaltyBLO.cs:48-51`)
  → `LoyaltyData.GetProductList(...)` (`LoyaltyData.cs:105-132`)
  → SP `[dbo].[GET_ITEM_TABLE_NOT_IN_GIFT_REDEEM_TABLE] @TextSearch,@PageSize,@PageNumber` qua
    `CentralMDPartnerContainer` (`LoyaltyData.cs:114-117`) — **không tìm thấy định nghĩa SP**
  → response `DataTablesViewModel<SetupLoyaltyResponseModel>`.

- **`GetItemGiftRedeemList()` [POST]** (`SetupLoyaltyController.cs:120-142`)
  → `status` hardcode `""` ("Tất cả", dòng 139)
  → `LoyaltyBLO.GetItemGiftRedeemList(...)` (`LoyaltyBLO.cs:52-55`)
  → `LoyaltyData.GetItemGiftRedeemList(...)` (`LoyaltyData.cs:134-164`)
  → SP `[dbo].[GET_GIFT_REDEEM_LIST] @TextSearch,@Status,@PageSize,@PageNumber` qua
    `CentralMDPartnerContainer` (`LoyaltyData.cs:144-148`) — **không tìm thấy định nghĩa SP**
  → response `DataTablesViewModel<ListGiftRedeemModalModel>`.

- **`CreateSetupLoyalty(CreateSetupLoyaltyModel req)` [POST]** (`SetupLoyaltyController.cs:145-163`)
  → deserialize `req.ItemNoStr` (JSON) → `List<ProductListForLoyaltyResponseModel>` — **không try/catch**
  → build model: `Point` parse từ `PointStr`, `FromDate/ToDate` parse `dd/MM/yyyy`, **`Status=1`
    hardcode**, `CreatedUser=LoginUser.UserName`
  → `LoyaltyBLO.CreateSetupLoyalty(model)` (`LoyaltyBLO.cs:56-59`)
  → `LoyaltyData.CreateSetupLoyalty(req)` (`LoyaltyData.cs:165-337`) — ghi trực tiếp EF (không qua
    SP) bảng `GiftRedeems` + `GiftRedeemModifyLogs` qua `CentralMDPartnerContainer`
  → response `Json(ResultResponseModel)`.

- **`CreateSetupLoyaltyForTabGiftRedeem(...)` [POST]** (`SetupLoyaltyController.cs:166-183`)
  → deserialize `req.ListItemStr`, `Status=1` hardcode, `CreatedUser=LoginUser.UserName`
  → `LoyaltyBLO.CreateSetupLoyaltyForTabGiftRedeem(...)` (`LoyaltyBLO.cs:68-71`)
  → `LoyaltyData.CreateSetupLoyaltyForTabGiftRedeem(req)` (`LoyaltyData.cs:338-598`) — ghi trực
    tiếp EF `GiftRedeems`/`GiftRedeemModifyLogs`, nhiều nhánh insert/update theo so sánh ngày
  → response `Json(ResultResponseModel)`.

- **`UpdateSetupLoyalty(...)` [POST]** (`SetupLoyaltyController.cs:186-208`)
  → `Status=Convert.ToInt16(req.Status)` (không hardcode như Create), `UpdatedUser=LoginUser.UserName`
  → `LoyaltyBLO.UpdateSetupLoyalty(...)` (`LoyaltyBLO.cs:60-63`)
  → `LoyaltyData.UpdateSetupLoyalty(req)` (`LoyaltyData.cs:599-754`) — tìm theo `Pkey`,
    insert nếu chưa có / update nếu có
  → response `Json(ResultResponseModel)`.

- **`DeleteSetupLoyalty(...)` [POST]** (`SetupLoyaltyController.cs:211-215`)
  → `LoyaltyBLO.DeleteSetupLoyalty(req)` (`LoyaltyBLO.cs:64-67`)
  → `LoyaltyData.DeleteSetupLoyalty(req)` (`LoyaltyData.cs:756-805`) — xóa `GiftRedeems`/
    `GiftRedeemModifyLogs` theo `Pkey` nếu `FromDate` chưa tới ngày hiện tại
  → response `JsonResult(ResultResponseModel)`.

- **`ImportExcelGiftRedeem(HttpPostedFileBase ImportFile)` [POST]** (`SetupLoyaltyController.cs:218-333`)
  → validate file null/extension `.xls`/`.xlsx`
  → đọc bằng Spire.Xls `Workbook.LoadFromStream` (không lưu ra disk)
  → validate từng dòng: `ItemNo/PointRedeem/FromDate/ToDate` không rỗng (dòng 259-293)
  → `LoyaltyBLO.ImportExcelGiftRedeem(createUserName, data)` (`LoyaltyBLO.cs:73-76`)
  → `LoyaltyData.ImportExcelGiftRedeem(userName, dt)` (`LoyaltyData.cs:808-1409`, ~600 dòng,
    transaction EF, nhiều nhánh insert/update)
  → response `Json({Status, Message})` (anonymous object — khác `ResultResponseModel`).

- **`ImportExcelGiftRedeem_V2`** — **TOÀN BỘ ĐÃ COMMENT / DEAD CODE**
  (`SetupLoyaltyController.cs:335-553`). Không active, nhưng chứa 1 business rule không còn tồn
  tại ở bản active (`ClubCode` phải là `WIN`/`PLH`, dòng 510-518) — xem câu hỏi §7.10.

- **`ExcelExampleGiftRedeem()` [GET]** (`SetupLoyaltyController.cs:555-561`)
  → đọc file tĩnh `~/Files/ImportGiftRedeem.xlsx`, trả `File(...)` — không gọi BLO/Data.

- **`ExportExcelGiftRedeemList(...)` [GET]** (`SetupLoyaltyController.cs:563-623`)
  → `LoyaltyBLO.ExportExcelGiftRedeemList(...)` (`LoyaltyBLO.cs:83-86`)
  → `LoyaltyData.ExportExcelGiftRedeemList(...)` (`LoyaltyData.cs:1551-1577`)
  → **cùng SP** `GET_GIFT_REDEEM_LOYALTY_LIST` như `LoadSetupLoyaltyList` (`LoyaltyData.cs:1559`)
  → build Excel bằng EPPlus, header tiếng Việt cứng (10 cột), ghi thẳng `Response.OutputStream`
    rồi `Response.End()` (dòng 610-619).

- **`SetupMemberEarnItem()` [GET]** (`SetupLoyaltyController.cs:628-631`) → chỉ `View()`.

- **`SetupMemberEarnItemList()` [đọc Form]** (`SetupLoyaltyController.cs:632-656`)
  → `LoyaltyBLO.SetupMemberEarnItemList(...)` (`LoyaltyBLO.cs:89-92`)
  → `LoyaltyData.SetupMemberEarnItemList(...)` (`LoyaltyData.cs:1581-1609`)
  → SP `[dbo].[GetMemberEarnItemList] @TextSearch,@Status,@Exp='1',@PageSize,@PageNumber` qua
    `CentralMDPartnerContainer` (dòng 1589-1594) — **không tìm thấy định nghĩa SP**
  → response `DataTablesViewModel<MemberEarnItemResponseModel>`.

- **`ExportExcel_MemberEarnItemList(...)` [GET]** (`SetupLoyaltyController.cs:657-718`)
  → `LoyaltyBLO.ExportExcel_MemberEarnItemList(...)` (`LoyaltyBLO.cs:93-96`)
  → `LoyaltyData.ExportExcel_MemberEarnItemList(...)` (`LoyaltyData.cs:1611-1636`)
  → **cùng SP** `GetMemberEarnItemList` nhưng `@Exp='2'`, `@PageSize/@PageNumber=string.Empty`
    (dòng 1618-1623, xem câu hỏi kiểu tham số §7 note)
  → xuất Excel EPPlus 10 cột.

- **`UpdateSetupMemberEarnItem(...)` [POST]** (`SetupLoyaltyController.cs:720-743`)
  → `CrtUser=LoginUser.UserName`, `UpdDate=DateTime.Now`
  → `LoyaltyBLO.UpdateSetupMemberEarnItem(...)` (`LoyaltyBLO.cs:97-100`)
  → `LoyaltyData.UpdateSetupMemberEarnItem(req)` (`LoyaltyData.cs:1844-1964`) — ghi trực tiếp EF
    `MemberEarnItems`/`MemberEarnItemLogs`.

- **`DeleteSetupMemberEarnItem(...)` [POST]** (`SetupLoyaltyController.cs:745-750`)
  → `LoyaltyBLO.DeleteSetupMemberEarnItem(req)` (`LoyaltyBLO.cs:101-104`)
  → `LoyaltyData.DeleteSetupMemberEarnItem(req)` (`LoyaltyData.cs:1638-1685`) — tìm theo
    `ItemNo+Uom`, xóa nếu `FromDate` chưa tới hạn.

- **`DownloadTemplate_MemberEarnItem()` [GET]** (`SetupLoyaltyController.cs:752-758`)
  → đọc file tĩnh `~/Files/ImportMemberEarnItemTemplate.xlsx` — không gọi BLO/Data.

- **`ImportExcelMemberEarnItem(HttpPostedFileBase ImportFile)` [POST]** (`SetupLoyaltyController.cs:760-995`)
  → validate file null/extension → **lưu ra disk** `~/Uploads/{filename}` rồi
    `Workbook.LoadFromFile` (khác `ImportExcelGiftRedeem` dùng stream trực tiếp) → xóa file cũ
    trước/sau khi đọc (dòng 787-790, 802-805)
  → convert từng dòng → `ImportExcelSetupMemberEarnItemModel`, **`Blocked` hardcode = 0** dù cột
    Excel có `Blocked` (dòng 848, dòng 847 bị comment)
  → validate: `ItemNo/Uom/SaleQty/StampsQty/FromDate/ToDate` không rỗng (dòng 868-920);
    `FromDate<=ToDate` trừ `Blocked==1` (dòng 927-936); `FromDate>=hôm nay` trừ `Blocked==1`
    (dòng 938-947); `ToDate>=hôm nay` trừ `Blocked==1` (dòng 949-957)
  → `LoyaltyBLO.ImportExcelMemberEarnItem(...)` (`LoyaltyBLO.cs:105-108`)
  → `LoyaltyData.ImportExcelMemberEarnItem(...)` (`LoyaltyData.cs:1686-1843`).

- **`GiftCouponList()` [GET]** (`SetupLoyaltyController.cs:997-1001`) → chỉ `View()`.

- **`GetGiftCodeList()` [đọc Form]** (`SetupLoyaltyController.cs:1002-1056`)
  → `LoyaltyBLO.GetGiftCouponList(...)` (`LoyaltyBLO.cs:109-112`)
  → `LoyaltyData.GetGiftCouponList(...)` (`LoyaltyData.cs:1967-2000`)
  → SP `[dbo].[GetGiftCouponList] @FromDate,@ToDate,@OrderNo,@Coupon,@TextSearch,@IsUsed,
    @SendCX,@Export=1,@PageSize,@PageNumber` — **dùng `LoyaltyContainer` (DB context khác)**,
    KHÔNG phải `CentralMDPartnerContainer` như mọi method khác (`LoyaltyData.cs:1971`, xem §7.5)
    — **không tìm thấy định nghĩa SP**
  → response `DataTablesViewModel<GiftCouponResponseModel>`.

- **`ExportExcelGiftCouponList(...)` [GET]** (`SetupLoyaltyController.cs:1058-1123`)
  → `LoyaltyBLO.ExportExcelGetGiftCouponList(...)` (`LoyaltyBLO.cs:113-116`)
  → `LoyaltyData.ExportExcelGetGiftCouponList(...)` (`LoyaltyData.cs:2002-2034`) — **cùng SP**
    `GetGiftCouponList` với `@Export=2`, qua `LoyaltyContainer`
  → xuất Excel EPPlus 11 cột (kèm "Số đơn hàng đã sử dụng").

**Không có tích hợp external partner API** (AkaChain/FMV/Urbox/GotIT/HttpClient) trong toàn bộ
Business/Data layer domain này — đã grep case-insensitive, 0 match. Field `SendCX`/`IsSync`
trong `GiftCouponResponseModel` gợi ý có đồng bộ ra CrownX, nhưng việc **gửi** đó không nằm
trong 3 file đã đọc — chỉ đọc lại trạng thái `IsSync` đã có sẵn từ SP.

## 2. Business rule / validation / edge case / error handling (đánh số, trích dẫn)

1. `CreateSetupLoyalty`: nếu bảng `GiftRedeems` trống toàn bộ, không cho tạo chương trình có
   `FromDate < DateTime.Now.Date` → `Fail` "Thêm mới không thành công. Vì ngày bắt đầu có hiệu
   lực nhỏ hơn ngày hiện tại" — `LoyaltyData.cs:178-185`.
2. `CreateSetupLoyalty`: `Pkey` sinh theo công thức `{ItemNo}-{FromDate:yyyyMMdd}` — khóa nghiệp
   vụ định danh 1 chương trình theo item + ngày bắt đầu — `LoyaltyData.cs:197,212,236,264,282,304`.
3. `CreateSetupLoyalty`: khi đã có dữ liệu nhưng không tìm thấy theo `Pkey` → vẫn check
   `FromDate < DateTime.Now.Date` trước khi cho insert — `LoyaltyData.cs:243-252`.
4. `CreateSetupLoyalty`: khi `Pkey` đã tồn tại (update case), vẫn check `FromDate <
   DateTime.Now.Date` trước khi cho update — `LoyaltyData.cs:292-299`.
5. `CreateSetupLoyalty`: mọi insert `GiftRedeem`/`GiftRedeemModifyLog` tăng `Counter` bằng
   `Max(Counter)+1` đọc lại trước mỗi insert, không dùng identity — **race condition risk** khi
   2 request insert đồng thời (không có lock/transaction bao ngoài foreach) —
   `LoyaltyData.cs:187,203,254,272,301,307`.
6. `CreateSetupLoyaltyForTabGiftRedeem`: nếu `fromDateNew <= DateTime.Now` khi update → luôn
   `Fail` — `LoyaltyData.cs:420-427`.
7. `CreateSetupLoyaltyForTabGiftRedeem`: nếu `fromDateNew > _ToDate` (khoảng mới nằm sau khoảng
   cũ) → cho insert bản ghi mới với `Pkey` mới — `LoyaltyData.cs:430-480`.
8. `CreateSetupLoyaltyForTabGiftRedeem`: nếu khoảng cũ đã hết hiệu lực (`_ToDate <=
   DateTime.Now`) → từ chối update — `LoyaltyData.cs:481-488`.
9. `CreateSetupLoyaltyForTabGiftRedeem`: nếu `fromDateNew <= _ToDate` (chồng lấn thời gian) →
   từ chối, "đã tồn tại (hoặc không thỏa điều kiện về thời gian hiệu lực)" — `LoyaltyData.cs:491-498`.
10. `UpdateSetupLoyalty`: khi bảng trống, insert mới với `Status=Convert.ToInt16(req.Status)`
    (KHÔNG hardcode =1 như Create) — `LoyaltyData.cs:608-625`.
11. `UpdateSetupLoyalty`: khi có dữ liệu nhưng không tìm thấy theo `Pkey` → **vẫn cho insert mới**
    thay vì báo lỗi "không tìm thấy" (khác hành vi của Delete) — `LoyaltyData.cs:666-711`.
12. `DeleteSetupLoyalty`: nếu không tìm thấy `Pkey` → `Fail` "Không tìm thấy dữ liệu" —
    `LoyaltyData.cs:765-772`.
13. `DeleteSetupLoyalty`: nếu `fromDate <= DateTime.Now` (đã/đang hiệu lực) → từ chối xóa —
    `LoyaltyData.cs:777-784`.
14. `ImportExcelGiftRedeem` (Controller): mỗi dòng phải có `ItemNo/PointRedeem/FromDate/ToDate`
    không rỗng — `SetupLoyaltyController.cs:259-293`.
15. `ImportExcelGiftRedeem` (Data): nếu bảng trống toàn bộ và `excel_FromDate.Date <=
    DateTime.Now.Date` → lỗi "không được phép cập nhật..." — `LoyaltyData.cs:836-839`.
16. `ImportExcelGiftRedeem`: nếu `excel_ToDate < DateTime.Now.Date` (dòng excel hết hiệu lực) và
    `Pkey` đã tồn tại → update `Status=0`; nếu `Pkey` không tồn tại → lỗi "không tồn tại trong hệ
    thống" — `LoyaltyData.cs:870-901`.
17. `ImportExcelGiftRedeem`: nếu `excel_FromDate` trùng `ItemNo+FromDate` đã tồn tại cho item
    khác và `excel_FromDate <= isToDate` bản ghi cũ → lỗi "này bị trùng" —
    `LoyaltyData.cs:908-920`.
18. `ImportExcelGiftRedeem`: nhánh dữ liệu đã tồn tại toàn cục — nếu `excel_FromDate <
    DateTime.Now.Date` → luôn từ chối cập nhật — `LoyaltyData.cs:999-1002`.
19. `ImportExcelGiftRedeem`: rule so sánh `maxFromDate`/`maxToDate` hiện có của `ItemNo` để quyết
    định insert mới / cập nhật điểm giữ nguyên ngày / cập nhật hết hiệu lực — logic phức tạp,
    không tóm gọn được thành 1 rule đơn — `LoyaltyData.cs:1021-1394` (xem câu hỏi §7.1).
20. `ImportExcelGiftRedeem`: toàn bộ bọc trong `db.Database.BeginTransaction()`; lỗi bất kỳ →
    rollback + trả message generic **"Lỗi hệ thống, Import file excel không thành công"** (không
    trả `ex.Message` thật, khác các method khác) — `LoyaltyData.cs:816-818,1401-1405`.
21. `SetupMemberEarnItemList`/`GetProductList`/`GetItemGiftRedeemList`/`GetSetupLoyaltyList`:
    **nuốt mọi exception**, trả `totalRecord=0` + list rỗng, không log, không throw — UI chỉ
    thấy "không có dữ liệu" dù thực chất lỗi hệ thống —
    `LoyaltyData.cs:98-102,127-131,159-163,1604-1608`.
22. `ImportExcelMemberEarnItem`: check `ItemNo` phải tồn tại trong bảng `Items`; nếu có mã không
    tồn tại → trả `(3, "", "ma1|ma2|...")` — `LoyaltyData.cs:1696-1711`.
23. `ImportExcelMemberEarnItem`: check `ItemNo` bị khóa kinh doanh (`Blocked==1` trong `Items`) →
    từ chối toàn bộ file — `LoyaltyData.cs:1715-1723`.
24. `ImportExcelMemberEarnItem`: check `Uom` phải tồn tại trong `Barcodes` với `Blocked==0` —
    **logic có dấu hiệu sai** (điều kiện `if` check `listUOM` — nguồn input — thay vì kết quả
    truy vấn `checkUOM`) — `LoyaltyData.cs:1727-1736` (xem câu hỏi §7.2).
25. `ImportExcelMemberEarnItem`: check trùng `Pkey = ItemNo+Uom+FromDate` khi bản ghi đang có
    hiệu lực (`Blocked==false`) → từ chối "đã tồn tại trong hệ thống" — `LoyaltyData.cs:1740-1744`.
26. `ImportExcelMemberEarnItem`: nếu có bản ghi cũ đã `Blocked==true` mà khoảng ngày mới trùng
    khoảng ngày cũ → **xóa bản ghi cũ trước khi insert mới** (không lưu lịch sử) —
    `LoyaltyData.cs:1758-1774`.
27. `ImportExcelMemberEarnItem`: nếu khoảng ngày trùng với bản ghi đang active (`Blocked==false`)
    → từ chối toàn bộ, "bị trùng thời gian hiệu lực" — `LoyaltyData.cs:1778-1791`.
28. Controller `ImportExcelMemberEarnItem`: `FromDate<=ToDate` trừ dòng `Blocked==1` —
    `SetupLoyaltyController.cs:927-936`.
29. Controller `ImportExcelMemberEarnItem`: `FromDate>=hôm nay` trừ dòng `Blocked==1` —
    `SetupLoyaltyController.cs:938-947`.
30. Controller `ImportExcelMemberEarnItem`: `ToDate>=hôm nay` trừ dòng `Blocked==1` —
    `SetupLoyaltyController.cs:949-957`.
31. `UpdateSetupMemberEarnItem`: khi `req.Blocked=="0"` (active lại), nếu `_toDate <
    DateTime.Now.Date` → từ chối "... nên không Active được" — `LoyaltyData.cs:1854-1867`.
32. `UpdateSetupMemberEarnItem`: khi active lại, check chồng lấn thời gian với bản ghi active
    khác cùng `ItemNo+Uom` → từ chối nếu trùng — **rủi ro `InvalidOperationException`** nếu list
    `Blocked==false` rỗng (`.Min()/.Max()` không check `.Any()` trước) —
    `LoyaltyData.cs:1869-1884` (xem câu hỏi §7.3).
33. `DeleteSetupMemberEarnItem`: nếu `fromDate <= DateTime.Now` (đang/đã dùng) → từ chối xóa —
    `LoyaltyData.cs:1658-1665`.
34. File Excel import chỉ chấp nhận extension `.xls`/`.xlsx` —
    `SetupLoyaltyController.cs:226-235` (GiftRedeem), `769-778` (MemberEarnItem).
35. `ExportExcelGiftRedeemList`/`ExportExcel_MemberEarnItemList`/`ExportExcelGiftCouponList`:
    nếu `data` rỗng vẫn `return Json(string.Empty)` — không có message báo "không có dữ liệu" —
    dòng 622, 717, 1122.
36. Toàn bộ response Excel dùng `Response.End()` sau khi ghi (pattern MVC cũ, throw
    `ThreadAbortException` được runtime tự xử lý) — dòng 616-619, 711-714, 1116-1119.

## 3. Model / DTO / Entity liên quan

File `src/legacy/VCM.BLUEPOS.Model/Loyalty/SetupLoyaltyModel.cs` (334 dòng — đọc toàn bộ):

| Class | Dòng | Vai trò | Property chính |
|---|---|---|---|
| `GiftRedeemResponseModel` | 9-28 | Response `GetSetupLoyaltyList` | `ID, ItemNo, ItemName, SalesUnitOfMeasure, PointRedeem, StatusStr, FromDateStr, ToDateStr, CreatedUser, CreatedDate, UpdatedUser, UpdatedDate, Counter, Pkey, ClubCode, Total` |
| `SetupLoyaltyResponseModel` | 29-39 | Response `GetProductList` | `ID, ItemNo, ItemName, SalesUnitOfMeasure, Counter, Pkey, Total` |
| `ProductListForLoyaltyResponseModel` | 41-48 | Phần tử `ListItem` trong `CreateSetupLoyaltyModel` | `ID, ItemNo, ItemName, SalesUnitOfMeasure` |
| `ItemGiftRedeemForTabGiftRedeemModel` | 50-59 | Phần tử `ListItem` của `CreateSetupLoyaltyTabGiftRedeemModel` | `ItemNo, PointRedeem, ClubCode, Status, Pkey, FromDate, ToDate` (FromDate/ToDate kiểu `string`) |
| `ListGiftRedeemModalModel` | 61-76 | Response `GetItemGiftRedeemList` | tương tự `GiftRedeemResponseModel` nhưng có `Status` thay vì `StatusStr` |
| `CreateSetupLoyaltyModel` | 78-100 | Request `CreateSetupLoyalty` | có cả field string tạm (`ItemNoStr, PointStr, FromDateStr, ToDateStr`) và field đã convert (`Point, FromDate, ToDate`) |
| `CreateSetupLoyaltyTabGiftRedeemModel` | 102-124 | Request `CreateSetupLoyaltyForTabGiftRedeem` | tương tự trên + `ListItemStr` |
| `UpdateSetupLoyaltyModel` | 125-145 | Request `UpdateSetupLoyalty` | `ID` kiểu `string` (khác `CreateSetupLoyaltyModel.ID` kiểu `int`) |
| `DeleteLoyaltyModel` | 147-166 | Request `DeleteSetupLoyalty` | — |
| `ExportExcelGiftRedeemResponseModel` | 168-181 | Response export Excel tỷ lệ đổi quà | — |
| `GiftRedeemModifyLogModel` | 183-198 | Model log | **không thấy dùng ở đâu trong 3 file đã đọc** (xem câu hỏi §7.9) |
| `ImportExcelGiftRedeemModel` | 200-210 | Model cho `ImportExcelGiftRedeem_V2` (đã comment) | dead code |
| `MemberEarnItemResponseModel` | 214-230 | Response `SetupMemberEarnItemList` | — |
| `ExportMemberEarnItemResponseModel` | 232-245 | Response export Excel tỷ lệ tích tem | — |
| `DeleteSetupMemberEarnItemModel` | 247-259 | Request `DeleteSetupMemberEarnItem` | — |
| `ImportExcelSetupMemberEarnItemModel` | 261-277 | Model nội bộ convert Excel trong Controller | — |
| `UpdateSetupMemberEarnItemModel` | 279-295 | Request `UpdateSetupMemberEarnItem` | `Blocked` kiểu `string` |
| `GiftCouponResponseModel` | 297-312 | Response `GetGiftCodeList` | `CrtDateStr, OrderNo, OrderNoUsed, MemberCard, CouponCode, MaterialNo, FromDateStr, ToDateStr, IsUsed, IsSync, Message, Total` |
| `ExportExcelGiftCouponResponseModel` | 314-328 | Response export Excel coupon | thứ tự property khác (`OrderNoUsed` cuối) |

EF entity (đọc riêng, dùng qua `CentralMDPartnerContainer`):

| Entity | File | Cột chính |
|---|---|---|
| `EF.Central.GiftRedeem` | `GiftRedeem.cs:15-26` | `Id, ItemNo, Point(int?), FromDate(DateTime?), ToDate(DateTime?), Status(short?), Counter(long?), Pkey, GiftOption` — **KHÔNG có cột `ClubCode`** (xem câu hỏi §7.10) |
| `EF.Central.GiftRedeemModifyLog` | `GiftRedeemModifyLog.cs:15-31` | `Id, ItemNo, Point, FromDate, ToDate, Status(bool?), StoreNo, POSNo, Counter, Pkey, CreatedDate, CreatedUser, UpdatedDate, UpdatedUser` |
| `EF.Central.MemberEarnItem` | `MemberEarnItem.cs:15-28` | `ItemNo, Uom, Size, SaleQty(int), StampsQty(int), FromDate(DateTime), ToDate(DateTime), Blocked(bool), CrtUser, CrtdDate, UpdDate` — **không có PK riêng**, khóa nghiệp vụ = `ItemNo+Uom+FromDate` |
| `EF.Central.MemberEarnItemLog` | `MemberEarnItemLog.cs:15-42` | `Id(long), StoreNo, ItemNo, Description, Uom, Size, SaleQty, StampsQty, FromDate, ToDate, FromTime, ToTime, Blocked, CreatedUser, CreatedDate, UpdatedUser, UpdatedDate, Counter, Pkey, GuiID, Ref1-Ref5` (`Ref1..Ref5`/`GuiID` không thấy dùng trong `LoyaltyData.cs`) |

Model dùng chung:
- `ResultResponseModel` (`src/legacy/VCM.BLUEPOS.Model/ResultResponseModel.cs:10-31`):
  `Status(ResultEnum), Message, Item1..Item12(string), Flag(int), IsStatus(int), isFlag(bool?),
  ListStoreNo(string), Total(int)` — response chuẩn cho create/update/delete trong domain này.
- `DataTablesViewModel<T>` (`src/legacy/VCM.BLUEPOS/Models/DataTablesViewModel.cs:8-14`):
  `draw(string), recordsFiltered(int), recordsTotal(int), data(IEnumerable<T>)`.

## 4. Database / Stored Procedure

| SP | Tham số | DB context | Tìm thấy định nghĩa? |
|---|---|---|---|
| `GET_GIFT_REDEEM_LOYALTY_LIST` | `@FromDate,@ToDate,@ItemNo,@Status` | `CentralMDPartnerContainer` | ❌ Không có trong `Script_Stored_Procedures.sql` (146 SP, đã grep case-insensitive) |
| `GET_ITEM_TABLE_NOT_IN_GIFT_REDEEM_TABLE` | `@TextSearch,@PageSize,@PageNumber` | `CentralMDPartnerContainer` | ❌ Không tìm thấy |
| `GET_GIFT_REDEEM_LIST` | `@TextSearch,@Status,@PageSize,@PageNumber` | `CentralMDPartnerContainer` | ❌ Không tìm thấy |
| `GetMemberEarnItemList` | `@TextSearch,@Status,@Exp,@PageSize,@PageNumber` | `CentralMDPartnerContainer` | ❌ Không tìm thấy |
| `GetGiftCouponList` | `@FromDate,@ToDate,@OrderNo,@Coupon,@TextSearch,@IsUsed,@SendCX,@Export,@PageSize,@PageNumber` | **`LoyaltyContainer`** (khác context) | ❌ Không tìm thấy |

> **Cả 5 SP đều KHÔNG có định nghĩa trong `src/legacy/Database/Stored_Procedures/Script_Stored_Procedures.sql`**
> — file này có 146 `CREATE PROCEDURE` nhưng không SP nào khớp 5 tên trên. Xem câu hỏi §7.4 —
> không thể xác nhận tham số/bảng nguồn thật sự nếu không có định nghĩa SP. Bảng ghi trực tiếp
> qua EF (không qua SP): `GiftRedeems`, `GiftRedeemModifyLogs`, `MemberEarnItems`,
> `MemberEarnItemLogs` (tất cả thao tác Create/Update/Delete/Import dùng EF `SaveChanges`, chỉ
> các thao tác **đọc danh sách** mới gọi SP).

## 5. Config / hằng số / magic number / phụ thuộc ngầm

1. `db.Database.CommandTimeout = 2*60` (120s) — lặp lại rất nhiều nơi, hardcode literal mỗi
   method: `LoyaltyData.cs:56,112,141,171,344,605,1558,1692,1850,1973,2009`.
2. `db.Database.CommandTimeout = 5*60` (300s) riêng cho thao tác nặng/import:
   `ImportExcelGiftRedeem` (`LoyaltyData.cs:814`), `SetupMemberEarnItemList`/
   `ExportExcel_MemberEarnItemList` (dòng 1587, 1617).
3. `LoginUser.UserName` — phụ thuộc ngầm vào field `public ADUserModel LoginUser` của
   `BaseController` (`BaseController.cs:36`), set trong `OnActionExecuting` từ
   `AuthCookie.CurrentUser(...)` (`BaseController.cs:114`) — cookie-based, không phải DI/Session
   trực tiếp.
4. `BaseController.ListController` — `public static List<ControllerDataModel>` dùng chung toàn
   ứng dụng, dựng lại bằng reflection **mỗi lần khởi tạo controller** (`BaseController.cs:38,46,170-202`)
   — không riêng domain này nhưng ảnh hưởng mọi request.
5. `[DisplayName("...")]` trên action → cơ chế phân quyền dựa vào reflection + string-match
   Controller/Action (`BaseController.cs:124,203-208`).
6. Magic string định dạng ngày `"dd/MM/yyyy"` lặp lại khắp Controller —
   `SetupLoyaltyController.cs:78-79,151-152,194-195,295-296,828-829`.
7. Magic string `"yyyyMMdd"` dùng build `Pkey` — lặp lại nhiều chỗ trong `LoyaltyData.cs` (xem
   rule §2.2).
8. Hardcode đường dẫn file tĩnh: `~/Files/ImportGiftRedeem.xlsx` (dòng 557),
   `~/Files/ImportMemberEarnItemTemplate.xlsx` (dòng 754), thư mục upload tạm `~/Uploads/`
   (dòng 784, 793) — không đọc từ `Web.config`.
9. `@Exp` param truyền ký tự literal `'1'`/`'2'` (list vs export) cho SP `GetMemberEarnItemList`
   — không có hằng số đặt tên (magic value) — `LoyaltyData.cs:1592,1621`.
10. `@Export` param `1`/`2` tương tự cho SP `GetGiftCouponList` — `LoyaltyData.cs:1983,2018`.
11. **Không tìm thấy bất kỳ giá trị nào đọc từ `ConfigurationManager.AppSettings`/`Web.config`**
    trong cả 3 file chính của domain này — connection string EF ẩn trong constructor rỗng của
    `CentralMDPartnerContainer`/`LoyaltyContainer` (không trace được vì không đọc file context).
12. `ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial` hardcode, lặp lại
    ở cả 3 hàm export (dòng 568, 663, 1064).
13. `ViewBag.ListMemberCardType`, `ViewBag.FromDate/ToDate/ClubCode/PointRedeem` — phụ thuộc
    ViewBag truyền dữ liệu vào View (dòng 43, 89-92).
14. Pattern `ListItem`/`ListItemStr`: client gửi JSON string qua form field, Controller
    `JsonConvert.DeserializeObject` — phụ thuộc ngầm format JSON đúng từ frontend, không có
    schema validation.

## 6. Phạm vi đã đọc (để xác nhận độ đầy đủ)

Đọc toàn bộ (không đọc mẫu): `SetupLoyaltyController.cs` (1137 dòng), `LoyaltyBLO.cs` (126 dòng),
`LoyaltyData.cs` (2043 dòng), `SetupLoyaltyModel.cs` (334 dòng), 4 EF entity file (`GiftRedeem.cs`,
`GiftRedeemModifyLog.cs`, `MemberEarnItem.cs`, `MemberEarnItemLog.cs`), `ResultResponseModel.cs`,
`DataTablesViewModel.cs`, `BaseController.cs`. Đã grep xác nhận: 0 external partner API call
(AkaChain/FMV/Urbox/GotIT/HttpClient), 0/5 SP tìm thấy trong `Script_Stored_Procedures.sql`.

## 7. Câu hỏi cho người phụ trách (logic mơ hồ — KHÔNG suy diễn)

1. **`ImportExcelGiftRedeem` (`LoyaltyData.cs:1021-1394`, nhánh dòng 986)**: điều kiện
   `data.ToString() != null || data.ToString() != ""` **luôn đúng** (`data` là `bool`,
   `.ToString()` luôn trả `"True"`/`"False"`, không bao giờ `null`/`""`) — có vẻ bug copy-paste
   từ nhánh `if (data.ToString() == null || data.ToString() == "")` ở dòng 826 (cũng luôn sai
   cùng lý do). Mục đích ban đầu của check này là gì (nghi ngờ muốn check `!data` = bảng rỗng)?
   Nghi ngờ đây là dead branch không bao giờ chạy đúng như dự kiến.
2. **`ImportExcelMemberEarnItem` (`LoyaltyData.cs:1727-1736`)**: biến `checkUOM` (kết quả query
   DB) được tính nhưng **không dùng lại** trong `if` — điều kiện lại check `listUOM` (chính list
   nguồn từ input Excel, luôn có dữ liệu). Có phải bug (nên là `if (checkUOM==null ||
   checkUOM.Count==0)` như đã làm với `ItemNo` ở dòng 1703) hay chủ ý không validate UOM tồn tại
   DB thật sự?
3. **`UpdateSetupMemberEarnItem` (`LoyaltyData.cs:1869-1884`)**: `checkItemNo.Select(...).Min()/
   .Max()` không check `.Any()` trước — nếu list rỗng sẽ throw `InvalidOperationException`. Đây
   là bug chưa xử lý edge case, hay nghiệp vụ đảm bảo luôn có ≥1 bản ghi active trước khi user
   bấm "Active lại"?
4. **5 SP** (`GET_GIFT_REDEEM_LOYALTY_LIST`, `GET_ITEM_TABLE_NOT_IN_GIFT_REDEEM_TABLE`,
   `GET_GIFT_REDEEM_LIST`, `GetMemberEarnItemList`, `GetGiftCouponList`) không có định nghĩa
   trong `Script_Stored_Procedures.sql` (đã grep case-insensitive, 0/146 khớp). Các SP này định
   nghĩa ở script/DB nào khác? Không thể xác nhận tham số/bảng nguồn thật nếu thiếu định nghĩa —
   cần bản script đúng trước khi thiết kế Repository mới cho domain này.
5. **`GetGiftCouponList`/`ExportExcelGetGiftCouponList` dùng `LoyaltyContainer`** trong khi mọi
   method còn lại trong `LoyaltyData.cs` dùng `CentralMDPartnerContainer`. `LoyaltyContainer` trỏ
   DB vật lý nào (cùng server khác DB hay khác server hẳn)? Quan trọng vì `ILoyaltyRepository`
   mới có thể cần 2 connection factory khác nhau cho cùng 1 domain.
6. **`ILoyaltyData` interface tồn tại nhưng không được dùng để DI** (`LoyaltyBLO` tự `new
   LoyaltyData()` thay vì inject interface) — interface này có dùng ở nơi khác (unit test, module
   DI khác) không, hay hoàn toàn là dead code? Ảnh hưởng thiết kế Repository interface bên kiến
   trúc mới vì không có ví dụ IoC thực tế để đối chiếu từ layer Data gốc.
7. **`LoadProductList()` tính `pageNumber = skip/pageSize`** (`pageSize` mặc định `1` khi
   `length` null) trong khi `LoadSetupLoyaltyList()` dùng trực tiếp `skip`/`pageSize` không qua
   công thức chia — 2 action cùng domain nhưng công thức phân trang khác hẳn. Cố ý (viết ở 2 thời
   điểm khác nhau) hay 1 trong 2 là bug cần thống nhất khi migrate?
8. **`CreateSetupLoyaltyModel.Status` luôn bị override thành `1`** trong Controller trước khi
   gọi BLO (`SetupLoyaltyController.cs:153`) dù model có field `Status` từ request — field
   `Status` client gửi lên có mục đích gì nếu luôn bị bỏ qua? Chủ ý (luôn tạo mới = có hiệu lực)
   hay sót logic?
9. **`GiftRedeemModifyLogModel`** định nghĩa nhưng không thấy dùng ở đâu trong 3 file đã đọc đầy
   đủ — có dùng ở tầng khác (View/JS) hay là model thừa?
10. **`ImportExcelGiftRedeem_V2`** (đã comment toàn bộ) chứa rule `ClubCode` phải là `WIN`/`PLH`
    (dòng 510-518) — rule này KHÔNG tồn tại ở action `ImportExcelGiftRedeem` đang active (không
    có `ClubCode` trong `ImportExcelGiftRedeemModel` của bản V1, và entity `GiftRedeem` cũng
    không có cột `ClubCode`). Rule ClubCode WIN/PLH còn giá trị nghiệp vụ cần đưa vào bản migrate
    mới không, hay đã bị loại bỏ chủ ý?

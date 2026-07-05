# Xác nhận kết thúc ngày (EOD) — Phân tích luồng LEGACY (VCM.BLUEPOS)

> Phạm vi: chỉ hệ thống cũ `src/legacy/` (.NET Framework 4.6, ASP.NET MVC). Không mô tả hệ thống
> mới — xem `docs/web/logic/eod.md` cho bản port .NET 10.

## 1. Phát hiện kiến trúc quan trọng — PULL, không phải POS push API

Khác với giả định thông thường ("máy POS gọi API đẩy dữ liệu lên server"), legacy VCM.BLUEPOS
**không có bất kỳ API endpoint nào để máy POS chủ động gọi lên**:

- Toàn bộ `src/legacy/VCM.BLUEPOS/` chỉ có route MVC dạng `routes.MapRoute(...)` (xem
  `App_Start/RouteConfig.cs`) phục vụ trình duyệt người dùng — không có `ApiController`, không có
  `MapHttpRoute`/`WebApiConfig`, không có route `api/...`.
- Ngược lại: **server trung tâm tự mở `SqlConnection` trực tiếp tới SQL Server cục bộ của từng máy
  POS** (database tên `StorePLH`, connection string dạng `Data Source={IP máy POS}; Initial
  Catalog=StorePLH; User ID=POS; Password=POS@1234` — xem
  `StoreActivitiesController.cs:314` và `SyncDataController.cs`), SELECT dữ liệu bán hàng rồi
  INSERT/bulk-copy vào DB trung tâm.
- Việc đồng bộ này do **nhân viên IT/vận hành bấm nút thủ công trên dashboard**
  (`SyncDataController` — route `dong-bo-sale-to-central`), không phải cơ chế tự động/real-time.
- Mỗi máy POS có 1 service riêng (`ServiceAPI.exe`, nhắc tới trong
  `MonitorPOSController.cs:276` cùng `BLUEPOS.exe`/`sqlservr.exe`) — **source code của service này
  không có trong repo** `src/legacy/` hiện tại, nên không xác minh được nó có tự expose API gì
  không. Kết luận trong tài liệu này chỉ dựa trên phần source **có sẵn để đọc**.
- Comment trong code .NET 10 mới nhắc "Migrated từ VCM.POSBLUE.Data.Common.CommonData" — đây là
  **nhầm lẫn/viết tắt tên** của người viết migration trước đây, KHÔNG có project "VCM.POSBLUE"
  thật; class thật là `VCM.BLUEPOS.Data.Common.CommonData` (đã xác nhận bằng Explore agent, grep
  toàn `src/legacy/` không ra kết quả "POSBLUE").

## 2. Luồng đầy đủ (4 giai đoạn)

```
[GIAI ĐOẠN 1 — Tại máy POS, ngoài phạm vi web]
  Máy POS bán hàng → ghi trực tiếp vào SQL Server CỤC BỘ của máy (DB "StorePLH")
    → bảng TransHeader (OrderNo, StoreNo, POSTerminalNo, OrderDate, AmountInclVAT, CashierID,
       CustomerName, TransactionType...), TransLine (Quantity, ItemNo, LineAmountIncVAT...)
    → hoàn toàn cục bộ, không liên quan web dashboard ở bước này

[GIAI ĐOẠN 2 — Đồng bộ sale POS → Central (THỦ CÔNG, do IT/vận hành bấm trên dashboard)]
  SyncDataController (route "dong-bo-sale-to-central")
    → SyncSalePosToCentral (view) / SyncSale (POST AJAX từ TRÌNH DUYỆT, không phải POS gọi)
    → Input: ListOrder, StoreNo, PosTerminal, IPAddress (IP máy POS, do người dùng chọn trên UI)
    → Với mỗi orderNo:
        1. SalesStagingBLO.StagingListTableFromPOS(storeNo) → lấy danh sách bảng cần đồng bộ
           (TransHeader, TransLine, TransPaymentEntry...)
        2. Mở SqlConnection trực tiếp tới IP máy POS → SELECT * FROM [TableName] WHERE
           DocumentNo=@orderNo (StoreActivitiesController.cs:237, GetSaleMisCentral dòng ~1432-1488)
        3. CommonData.InsertBulk(table, TableName, storeNo, orderNo, posTerminal)
           (VCM.BLUEPOS.Data/Common/CommonData.cs:1187-1257) → SqlBulkCopy ghi thẳng vào bảng cùng
           tên trên DB trung tâm (CentralSalesStagingContainer, routed theo
           ServerIPConnection.GetIPServerByStore) — schema bảng POS và Central GIỐNG HỆT nhau nên
           bulk copy 1:1 theo tên cột, không cần transform.
        4. InsertTrans_Invoice(storeNo, orderNo) — trigger job hóa đơn điện tử.

[GIAI ĐOẠN 3 — Cập nhật tổng kết EOD theo từng máy POS]
  Sau khi đồng bộ xong 1 batch order:
    StoreActivitiesData.InsertUpdatePOSEOD(List<POSEODModel>)
      (src/legacy/VCM.BLUEPOS.Data/StoreActivities/StoreActivitiesData.cs:530-566)
    → INSERT/UPDATE bảng POSEOD (Central DB): StoreNo, POSTerminalNo, BussinessDate,
      TotalSale, TotalAmount, MaxBillNumber, CreatedDate
    → (Xem mục 4 — TotalSale ở đây là SỐ LƯỢNG BILL, không phải số tiền)

[GIAI ĐOẠN 4 — Xác nhận kết thúc ngày (nhân viên bấm trên dashboard)]
  StoreActivitiesController.CheckFinishDate (POST, dòng 349-663)
    → Rule 1: không xác nhận ngày tương lai
    → Rule 2 (chỉ StyleProfile "KS"/"FS"): kiểm tra ShiftHeader Ca 1 + Ca 2 đã đóng
    → Rule 3 (MỌI store): SaleBusinessStoreStaging (SP SP_END_DATE_CONFIRM_STAGING) — 1 dòng/máy
      POS, cột IsClosed — nếu còn máy POS IsClosed=false → CHẶN xác nhận
    → Rule 4: CheckTotalSale() đối chiếu POS ↔ Central (xem mục 4) — lệch thì tự động chạy lại
      GIAI ĐOẠN 2 (UploadSaleFromPOSToCentral) để đồng bộ nốt đơn hàng thiếu
    → Rule 5: cảnh báo ngày trước đó chưa xác nhận (ListCheckBusinessDate)
    → Commit: ConfirmBusinessDate → INSERT bảng BussinessDateConfirm (Code=Store+yyyyMMdd,
      TotalAmount, IsConfirm=true, ConfirmDate, CreatedUser) + UPDATE BussinessDateOpen.BussinessDate
      += 1 ngày (CÙNG 1 EF SaveChanges() — atomic vì cùng 1 DB "Central Sales" theo store)
```

## 3. Bảng & Database liên quan

| Bảng | Ở đâu | Vai trò | Cột chính |
|---|---|---|---|
| `TransHeader` | **Cả 2 nơi**: SQL Server cục bộ máy POS (`StorePLH`) VÀ Central Sales (theo store) | Hóa đơn bán hàng | `OrderNo, StoreNo, POSTerminalNo, OrderDate, AmountInclVAT, TransactionType, CashierID, CustomerName, DocumentType` |
| `TransLine` | Cả 2 nơi (giống TransHeader) | Chi tiết dòng hàng | `DocumentNo, LineNo, ItemNo, Quantity, UnitPrice, LineAmountIncVAT, LineType` |
| `POSEOD` | Central Sales (theo store) | Tổng kết EOD theo (Store, Terminal, Ngày) | `StoreNo, POSTerminalNo, BussinessDate, TotalSale (⚠️ = SỐ LƯỢNG BILL), TotalAmount (= tiền), MaxBillNumber, CreatedDate` |
| `ShiftHeader` | Central Sales (theo store) | Trạng thái ca theo `ShiftNumber` | `ShiftCode, StoreNo, BussinessDate, ShiftNumber, StaffCode, IsShiftClosed, IsShiftOpened` |
| `BussinessDate` | Central Sales (theo store) | 1 dòng/(Store, PosTerminal, Ngày) — cờ đóng ngày per-terminal | `StoreNo, PosTerminal, BussinessDate1, IsClosed, OpenDate, CloseDate` |
| `BussinessDateOpen` | Central Sales (theo store) | Ngày kinh doanh hiện tại đang mở của store | `StoreNo, BussinessDate` (bị **+1 ngày** khi xác nhận) |
| `BussinessDateConfirm` | Central Sales (theo store) | Ledger xác nhận kết thúc ngày | `Code (=Store+yyyyMMdd), StoreNo, BussinessDate, TotalAmount, IsConfirm, ConfirmDate, CreatedUser` |
| `POSTerminal` | CentralMD (MD Partner) | Master máy POS | `No, StoreNo, IPAddress, Placement` (lọc `Placement != "POSWEB"` để loại kênh bán web) |

**Ghi chú Placement/POSWEB**: mọi tính toán đối chiếu số liệu bán hàng (`CheckTotalSale`,
`ListPosTerminalMissOrderNo`, `SaleBusinessStoreStaging`) đều **loại trừ** đơn hàng có
`DocumentType = "POSWEB"` hoặc terminal có `Placement = "POSWEB"` — tức kênh bán qua web không tính
vào rule "tất cả máy POS phải đóng ngày". Ngoài ra `CheckTotalSale` còn loại các POS Terminal có số
thứ tự (2 ký tự cuối mã terminal) **≥ 70** — theo yêu cầu nghiệp vụ bổ sung 25/11/2022 (comment
trong code, không rõ lý do nghiệp vụ chi tiết).

## 4. Logic tính "amount" và "số lượng bill" — điểm dễ hiểu nhầm

### 4.1 `POSEOD.TotalSale` thực chất là SỐ LƯỢNG HÓA ĐƠN (bill count), KHÔNG PHẢI số tiền

Bằng chứng (`StoreActivitiesData.cs:400-454`, hàm `CheckTotalSale`):

```csharp
// Lay data ban hang cua các POS khong phai la POSWEB
var totalSale = db.POSEODs.Where(...).Sum(d => d.TotalSale);          // <-- từ bảng POSEOD

var listDataTransHeader = db.TransHeaders.Where(...).ToList();
var countTransheader = data.Select(x => x.OrderNo).Count();           // <-- ĐẾM SỐ HÓA ĐƠN

if (totalSale != countTransheader)   // <-- SO SÁNH TRỰC TIẾP 2 SỐ ĐẾM, không phải tiền!
    result = (false, totalSale, countTransheader);
```

`POSEOD.TotalSale` được ghi bằng `listOrderPOS.Count()` (đếm số OrderNo) —
xem `StoreActivitiesController.cs:333` (`UploadSaleFromPOSToCentral`) và tương tự trong
`SyncDataController` (`TotalSale = resultOK.Count`). **Đây là số đếm hóa đơn, dùng để đối chiếu
"đã đồng bộ đủ đơn hàng chưa"**, không phản ánh doanh thu.

### 4.2 Số tiền thực tế (amount) nằm ở 2 nơi khác nhau

- **`POSEOD.TotalAmount`** — có trường riêng trong model (`POSEODModel.TotalAmount`) nhưng
  **không thấy chỗ nào trong `StoreActivitiesController`/`StoreActivitiesData` gán giá trị này**
  khi insert từ `UploadSaleFromPOSToCentral` (chỉ set `TotalSale`, `MaxBillNumber`) — khả năng cột
  này được ghi từ luồng khác (`SyncDataController`/`SyncDataBLO.UpdateEOD`) hoặc còn để trống.
- **`SaleBusinessStoreModel.AmountTotal`** — đây mới là số tiền thực tế dùng để hiển thị trên UI
  "Xác nhận kết thúc ngày" và ghi vào `BussinessDateConfirm.TotalAmount` khi commit:
  ```csharp
  var dataStoreResult = _activitiesBLO.SaleBusinessStore(SiteNo, ngayKD);
  var totalAmount = dataStoreResult.Sum(d => d.AmountTotal);   // StoreActivitiesController.cs:1383
  ```
  `SaleBusinessStore`/`SaleBusinessStoreStaging` gọi SP `SP_END_DATE_CONFIRM`/
  `SP_END_DATE_CONFIRM_STAGING` (đã **compile sẵn trong DB cũ, không có script .sql trong repo** để
  đọc chính xác công thức — SUM `AmountInclVAT` theo `TransactionType` là suy luận hợp lý dựa theo
  cách các nơi khác trong code tính `AmountInclVAT`, nhưng KHÔNG xác minh được 100% từ source).

### 4.3 Số lượng khách/hóa đơn hiển thị trên UI (`CountOrderNo`, `CountCustomer`)

Nằm trong `SaleBusinessStoreModel` (trả về từ SP `SP_END_DATE_CONFIRM_STAGING`) — **cùng lý do
trên, không có script SP để xác minh chính xác công thức COUNT DISTINCT theo cột nào** (OrderNo
hay MemberCardNo). Đây là giới hạn đã biết của việc phân tích legacy — SP business-critical này
được viết trực tiếp trong DB, không nằm trong source control.

## 5. File/Class tham chiếu (đầy đủ, kèm dòng)

| Layer | File | Vai trò |
|---|---|---|
| Controller (dashboard, đồng bộ thủ công) | `src/legacy/VCM.BLUEPOS/Controllers/SyncDataController.cs` | `SyncSalePosToCentral`, action `SyncSale` (~1490-1533), `GetSaleMisCentral` (~1432-1488) — pull dữ liệu từ IP máy POS |
| Controller (dashboard, xác nhận) | `src/legacy/VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` | `ConfirmEndingDateStores` (210), `CheckFinishDate` (349-663), `UploadSaleFromPOSByOrder` (218-304), `UploadSaleFromPOSToCentral` (306-345) |
| Controller (giám sát máy POS) | `src/legacy/VCM.BLUEPOS/Controllers/MonitorPOSController.cs` | Theo dõi tiến trình `BLUEPOS.exe`/`ServiceAPI.exe`/`sqlservr.exe` trên máy POS (dòng ~276) — không có source `ServiceAPI.exe` |
| BLO | `src/legacy/VCM.BLUEPOS.Business/StoreActivities/StoreActivitiesBLO.cs` | Pass-through thuần túy tới `StoreActivitiesData` |
| BLO (staging) | `src/legacy/VCM.BLUEPOS.Business/SalesStaging/SalesStagingBLO.cs` | `StagingListTableFromPOS`, `StagingListOrder` — xác định bảng/đơn hàng cần đồng bộ |
| BLO (common, bulk insert) | `src/legacy/VCM.BLUEPOS.Business/Common/CommonBLO.cs` | Pass-through `InsertBulk`, `InsertTrans_Invoice` |
| DAL | `src/legacy/VCM.BLUEPOS.Data/StoreActivities/StoreActivitiesData.cs` | `CheckTotalSale` (400-454), `ListPosTerminalMissOrderNo` (479-529), `InsertUpdatePOSEOD` (530-572), `ListCheckBusinessDate` (573-606), `ConfirmBusinessDate` (750-869) |
| DAL (common) | `src/legacy/VCM.BLUEPOS.Data/Common/CommonData.cs` | `InsertBulk` (1187-1257, `SqlBulkCopy`), `InsertTrans_Invoice` (1258+) |
| SP (không có source) | `SP_END_DATE_CONFIRM_STAGING`, `SP_END_DATE_CONFIRM` | Compile sẵn trong DB "Central Sales" cũ — không có script trong `src/legacy/Database/` (thư mục này chỉ chứa SP domain CentralMD, ví dụ `GetProductList`, `GetPOSTerminalList`...) |

## 6. Giới hạn của phân tích này

- **Không có source `ServiceAPI.exe`** chạy trên máy POS — không xác minh được máy POS có tự expose
  API nào khác ngoài phạm vi đọc được hay không.
- **Không có script SQL** cho `SP_END_DATE_CONFIRM_STAGING`/`SP_END_DATE_CONFIRM` — công thức
  chính xác tính `AmountTotal`, `CountOrderNo`, `CountCustomer` chỉ suy luận từ cách dùng ở tầng
  C#, chưa đối chiếu được 100% với SQL gốc.
- Kết luận "không có API POS push" dựa trên toàn bộ source **hiện có** trong `src/legacy/` tại
  thời điểm phân tích (đã Explore agent quét toàn bộ thư mục, grep `ApiController`/`POSBLUE`/route
  `api/...` — không có kết quả thật nào).

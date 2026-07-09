# Database Schema — RPOSCentralSales

> **Nguồn**: `docs/sql/database/CentralSale.sql` (script tạo DB `RPOSCentralSales`, generated 7/8/2026).
> **Mục đích tài liệu này**: bản đồ tra cứu **tên bảng / tên cột / kiểu dữ liệu / PK** để viết
> query/SP/Dapper mapping chính xác tuyệt đối — **KHÔNG suy đoán tên cột**.
>
> ## ⚠️ QUY TẮC BẮT BUỘC
> Trước khi viết bất kỳ SQL query, stored procedure, hoặc Repository method nào đụng tới các
> bảng trong `RPOSCentralSales`, **BẮT BUỘC đối chiếu file này trước** để lấy đúng tên bảng, tên
> cột, kiểu dữ liệu, độ dài. Sai tên cột / kiểu dữ liệu sẽ gây lỗi runtime hoặc sai dữ liệu bán
> hàng cho 5.000 máy POS. Nếu bảng cần dùng chưa có trong tài liệu này → đọc lại
> `docs/sql/database/CentralSale.sql` (hoặc script cập nhật mới nhất), sau đó bổ sung vào file này
> trong cùng commit.
>
> Database khác có script riêng — không nằm trong file này: `docs/architecture/centralMD-schema.md`
> (`RPOSMasterData`/CentralMD) và `docs/architecture/loyalty-schema.md` (`RPOSLoyalty`, khi có
> script tương ứng). Khi có script cho DB mới, tạo file theo cùng khuôn mẫu và thêm mục lục ở đây.

## Quy ước chung trong DB này

- **Không có FOREIGN KEY nào** trong toàn bộ script (`grep FOREIGN KEY/REFERENCES` không ra kết
  quả) — quan hệ giữa các bảng (`TransHeader.OrderNo` ↔ `TransLine.DocumentNo`,
  `TransHeader.StoreNo` ↔ `RPOSMasterData..Store.No`...) là **liên kết ngầm theo convention**,
  không được enforce ở DB. Nhiều SP còn `JOIN` chéo database (`RPOSMasterData..Store`,
  `RPOSMasterData..Item`, `RPOSMasterData..Staff`, `RPOSMasterData..MCH`) — DB này **không độc
  lập**, phụ thuộc `RPOSMasterData` cùng server.
- **Không dùng pattern `Counter`+`Pkey` như CentralMD**: cột `Counter bigint` xuất hiện ở khá
  nhiều bảng giao dịch (dùng để đồng bộ/delta, mặc định `0`) nhưng **không đi kèm** cột `Pkey` —
  chỉ duy nhất `ItemBlockVAT` có cột `Pkey varchar(20)` đơn lẻ (không phải cặp đồng bộ). PK thật
  của các bảng giao dịch là **composite theo khoá nghiệp vụ** (`OrderNo`+`LineNo`,
  `DocumentNo`+`LineNo`...), khác hẳn CentralMD.
- **PK phổ biến theo 3 dạng**:
  1. Khoá nghiệp vụ composite (`OrderNo`, `LineNo`, `OrderLineNo`...) trên các bảng chi tiết giao
     dịch (`TransLine`, `TransPaymentEntry`, `TransDiscountEntry`...) — không có cột `ID` riêng.
  2. `ID`/`Id` identity `int`/`bigint` IDENTITY(1,1) trên các bảng log/staging
     (`Interface_Errors`, `JobRunning`, `UploadSaleLog`, `TrackingPartner`...).
  3. Một số bảng **không có PK** (`PK: (none)`) — thường là bảng log/tracking phụ không cần định
     danh dòng (`BussinessDateOpen`, `EmailSentLog`, `POSDocumentNos`...).
- **`timestamp`/rowversion**: nhiều bảng giao dịch có cột `[timestamp] [timestamp] NOT NULL` đầu
  tiên (kiểu `rowversion` tự sinh, dùng để phát hiện thay đổi dòng — không phải cột thời gian).
  Đặc biệt `TransCpnVchIssue` **dùng chính cột `timestamp` này làm PRIMARY KEY**.
- **`NOLOCK`/`(NOLOCK)`** dùng phổ biến trong toàn bộ stored procedure của DB này (đọc dữ liệu
  giao dịch không khoá, chấp nhận dirty read để tránh block ghi từ POS) — theo đúng pattern legacy.
- Kiểu `[nvarchar](max)`, `[varchar](max)` ghi là `nvarchar(max)` / `varchar(max)`.
- Nhiều bảng có hậu tố `Orig` (`TransHeaderOrig`, `TransLineOrig`, `TransBluePointOrig`) — bản
  lưu **trước khi cập nhật/void** của bảng gốc tương ứng (audit trail), cấu trúc gần như giống hệt
  bảng gốc nhưng PK đổi thành `ID` identity thay vì khoá nghiệp vụ.
- Bảng `TransVoidHeader`/`TransVoidLine` có cấu trúc gần như song song với
  `TransHeader`/`TransLine` — lưu **giao dịch đã bị void/huỷ**.

## Mục lục theo domain

| Domain | Bảng |
|---|---|
| [Business Day / EOD](#business-day--eod) | BusinessDayConfirm, BussinessDate, BussinessDateConfirm, BussinessDateOpen, BussinessDateOpenLog, POSBussinessDate, POSBussinessDate_Log, POSEOD, POSEOD_API, POSFinishSale |
| [POS Shift](#pos-shift) | POSShiftHeader, POSShiftLine |
| [Transaction Header/Line (Sale)](#transaction-headerline-sale) | TransHeader, TransHeaderOrig, TransLine, TransLineOrig, TransInputData, TransOptionLine, TransPartnerLine, TransPaymentEntry, TransPaymentInfo, TransPointLine, TransInfocodeEntry |
| [Transaction Void](#transaction-void) | TransVoidHeader, TransVoidLine |
| [Transaction Discount/Bonus/Point/Coupon-Voucher](#transaction-discountbonuspointcoupon-voucher) | TransBonus, TransBluePoint, TransBluePointOrig, TransCpnVchIssue, TransDiscountCouponEntry, TransDiscountEntry |
| [Report](#report) | ReportSaleDetail |
| [Sync](#sync) | SyncTableFromPOS |
| [Logging / Error](#logging--error) | DataRawJson, EmailSentLog, Interface_Errors, JobRunning, LogSendEmail, POSError, UploadSaleByTableLog, UploadSaleLog |
| [Survey](#survey) | SurveyResult |
| [Partner Tracking](#partner-tracking) | TrackingPartner, INB_VoucherToSAP |
| [Khác](#khác) | ItemBlockVAT, POSDocumentNos, StoreExcludeInvoice |
| [Stored Procedures](#stored-procedures) | 17 SP hiện có trong script |

---

## Business Day / EOD

### BusinessDayConfirm
PK: `Code`
```
Code              varchar(20)     NOT NULL
StoreNo           varchar(10)     NOT NULL
BusinessDate      date            NOT NULL
TotalRevenue      decimal(18, 2)  NOT NULL   -- DEFAULT 0
TotalShifts       int             NOT NULL   -- DEFAULT 0
ConfirmedBy       nvarchar(100)   NOT NULL
ConfirmedDate     datetime        NOT NULL   -- DEFAULT getdate()
```
> Ghi 1 dòng/lần xác nhận chốt ngày kinh doanh (`usp_BusinessDay_ConfirmEndDate`), `Code` do SP tự
> sinh `= StoreNo + yyyyMMdd`. Xác nhận trùng (`StoreNo`+`BusinessDate` đã tồn tại) → SP `THROW`.

### BussinessDate
PK: `Code`
```
Code              varchar(50)     NOT NULL
StoreNo           varchar(10)     NULL
PosTerminal       varchar(10)     NULL
BussinessDate     date            NULL
IsOpened          bit             NULL       -- DEFAULT 1
IsClosed          bit             NULL       -- DEFAULT 0
BeginAmount       float           NULL
OpenDate          datetime        NULL       -- DEFAULT getdate()
CloseAmount       float           NULL
CloseDate         datetime        NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
```

### BussinessDateConfirm
PK: `Code`
```
timestamp         timestamp       NOT NULL
Code              varchar(50)     NOT NULL
StoreNo           varchar(10)     NULL
BussinessDate     date            NULL
TotalAmount       float           NULL
IsConfirm         bit             NULL       -- DEFAULT 0
ConfirmDate       datetime        NULL
FileNameDetail    varchar(200)    NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
```

### BussinessDateOpen
PK: (none)
```
timestamp         timestamp       NOT NULL
Code              varchar(50)     NULL
StoreNo           varchar(10)     NULL
BussinessDate     date            NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```
> `usp_BusinessDay_ConfirmEndDate` UPDATE `BussinessDate = BussinessDate + 1 ngày` trên bảng này
> theo `StoreNo` — đại diện "ngày kinh doanh hiện hành" của store, không có PK vì luôn thao tác
> theo filter `StoreNo`.

### BussinessDateOpenLog
PK: (none)
```
timestamp         timestamp       NOT NULL
Code              varchar(50)     NULL
StoreNo           varchar(10)     NULL
BussinessDate     date            NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```

### POSBussinessDate
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
Code              varchar(50)     NOT NULL
StoreNo           varchar(10)     NULL
PosTerminal       varchar(10)     NULL
BussinessDate     date            NULL
BeginAmount       float           NULL
CloseAmount       float           NULL
IsOpened          bit             NULL       -- DEFAULT 1
IsClosed          bit             NULL       -- DEFAULT 0
OpenDate          datetime        NULL       -- DEFAULT getdate()
CloseDate         datetime        NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL
```
> Ghi ngày kinh doanh theo từng POS terminal (khác `BussinessDate` — theo store). Job
> `Register_Insert_ALL` gộp/dedupe bảng này rồi archive sang `CentralSales..BussinessDate` +
> `POSBussinessDate_Log`, sau đó xoá khỏi `POSBussinessDate`.

### POSBussinessDate_Log
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
Code              varchar(50)     NOT NULL
StoreNo           varchar(10)     NULL
PosTerminal       varchar(10)     NULL
BussinessDate     date            NULL
BeginAmount       float           NULL
CloseAmount       float           NULL
IsOpened          bit             NULL
IsClosed          bit             NULL
OpenDate          datetime        NULL
CloseDate         datetime        NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL
UpdatedDate       datetime        NULL
LastUpdated       datetime        NULL
```
> Bảng lưu trữ (archive) các dòng đã xử lý xong từ `POSBussinessDate` — ghi bởi
> `Register_Insert_ALL`.

### POSEOD
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
POSTerminalNo     varchar(20)     NULL
StoreNo           varchar(20)     NULL
BussinessDate     datetime        NULL
TotalSale         int             NULL
TotalAmount       float           NULL
MaxBillNumber     varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```
> Kết quả EOD (End Of Day) theo POS terminal — dùng trong `GetSalesEODConfirm` để so khớp trạng
> thái đóng ca/ngày với dữ liệu thực tế từ `TransHeader`/`TransPaymentEntry`.

### POSEOD_API
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
POSTerminal       varchar(50)     NULL
StoreNo           varchar(50)     NULL
BussinessDate     date            NULL
TotalSale         int             NULL
CreatedDate       datetime        NULL
```

### POSFinishSale
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
Code              varchar(50)     NOT NULL
StaffCode         varchar(50)     NULL
StoreNo           varchar(10)     NULL
PosTerminal       varchar(10)     NULL
BussinessDate     date            NULL
IsOpened          bit             NULL       -- DEFAULT 1
IsFinished        bit             NULL       -- DEFAULT 0
OpenDate          datetime        NULL       -- DEFAULT getdate()
FinishDate        datetime        NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```
> Đánh dấu trạng thái "hoàn tất bán hàng" theo nhân viên/terminal/ngày. Cũng được
> `Register_Insert_ALL` gộp/dedupe rồi archive sang `CentralSales..FinishSale` +
> `POSFinishSale_Log` (2 bảng đích không có trong script này — có thể ở DB/script khác).

---

## POS Shift

### POSShiftHeader
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
ShiftCode         varchar(50)     NOT NULL
StaffCode         varchar(50)     NULL
StoreNo           varchar(10)     NULL
PosTerminal       varchar(10)     NULL
BussinessDate     date            NULL
ShiftNumber       varchar(10)     NULL
BeginAmount       float           NULL
CloseAmount       float           NULL
OutAmount         float           NULL
QuantityBox       int             NULL
QuantityCoupon    int             NULL
QuantityVoucher   int             NULL
IsShiftOpened     bit             NULL       -- DEFAULT 1
IsShiftClosed     bit             NULL       -- DEFAULT 0
OpenShiftDate     datetime        NULL       -- DEFAULT getdate()
CloseShiftDate    datetime        NULL
CreatedUser       varchar(20)     NULL
Source            varchar(10)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```
> `Register_Insert` gộp các `ShiftCode` bị nhân đôi (do ghi cả dòng mở ca + đóng ca riêng) thành
> 1 dòng duy nhất trên bảng này. `API_POS_CHECK_SHIFT_HEADER` tra ca gần nhất theo
> `StoreNo`+`PosTerminal`+`BussinessDate`.

### POSShiftLine
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
ShiftCode         varchar(50)     NOT NULL
LineNo            int             NOT NULL
CashCode          varchar(10)     NULL
CashValue         int             NULL
CashQuantity      int             NULL
CashAmount        float           NULL
CreatedUser       varchar(20)     NULL
UpdatedUser       varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
UpdatedDate       datetime        NULL       -- DEFAULT getdate()
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```
> Chi tiết kiểm đếm tiền mặt theo mệnh giá (`CashCode`/`CashValue`) của 1 ca (`ShiftCode`).

---

## Transaction Header/Line (Sale)

### TransHeader
PK: `OrderNo`
```
timestamp                    timestamp       NOT NULL
OrderNo                      nvarchar(20)    NOT NULL
OrderDate                    datetime        NOT NULL
CustomerNo                   nvarchar(20)    NOT NULL
CustomerName                 nvarchar(100)   NOT NULL
Address                      nvarchar(150)   NOT NULL
PhoneNo                      nvarchar(20)    NOT NULL
TablePhoneNo                 nvarchar(20)    NOT NULL
HouseNo                      nvarchar(50)    NOT NULL
CityNo                       nvarchar(30)    NOT NULL
DistrictNo                   nvarchar(30)    NOT NULL
WardNo                       nvarchar(30)    NOT NULL
StreetNo                     nvarchar(30)    NOT NULL
ShipToCityNo                 nvarchar(30)    NOT NULL
ShipToDistrictNo             nvarchar(30)    NOT NULL
ShipToWardNo                 nvarchar(30)    NOT NULL
ShipToStreetNo               nvarchar(30)    NOT NULL
DeliveryDate                 datetime        NOT NULL
DeliveryTimeFrom              datetime       NOT NULL
DeliveryTimeTo               datetime        NOT NULL
ZoneNo                       nvarchar(20)    NOT NULL   -- 'AGENTBANKWITHDRAW' = giao dịch rút tiền qua đại lý ngân hàng (đảo dấu doanh thu trong report)
ShipToAddress                nvarchar(150)   NOT NULL
DeliveryComment               nvarchar(150)  NOT NULL
BillToName                   nvarchar(150)   NOT NULL
BillToAddress                nvarchar(150)   NOT NULL
VATRegistrationNo            nvarchar(20)    NOT NULL
StoreNo                      nvarchar(10)    NOT NULL
POSTerminalNo                nvarchar(10)    NOT NULL
ShiftNo                      nvarchar(10)    NOT NULL
CashierID                    nvarchar(10)    NOT NULL
DiscountAmount               float           NULL
AmountExclVAT                float           NULL
VATAmount                    float           NULL
AmountInclVAT                float           NULL
GeneralComment                nvarchar(150)  NOT NULL
UserID                       nvarchar(50)    NOT NULL
PrepaymentAmount              float          NULL
DeliveringMethod              int            NOT NULL   -- kênh bán: 0=thường,10=tại chỗ,21=BeFood,30=ShopeeFood,40=Beamin,50=Gojek (theo Rpt_ReportSaleDetail_Insert)
PaymentAtPOSAmount            float          NULL
InChangeAmount                float          NULL
OrderStatus                  int             NOT NULL
ShipToName                   nvarchar(150)   NOT NULL
IsTenancy                    tinyint         NOT NULL
InInstalments                tinyint         NOT NULL
ShipToHouseNo                nvarchar(50)    NOT NULL
ShipToPhoneNo                nvarchar(20)    NOT NULL
AmountDiscountAtPOS           float          NULL
IssuedVATInvoice              tinyint        NOT NULL
VoucherDiscountNo            nvarchar(20)    NOT NULL   -- 'KIOS' = đơn thanh toán qua KIOSK
VoucherDiscountValue          float          NULL
PointConversionRate           float          NULL
TanencyNo                    nvarchar(20)    NOT NULL   -- kênh/đối tác: 'SNG'=Scan&Go, 'WCM'=ĐH Online, 'APY'/'WPH'/'PNP'/'MBC'/'PLG' = đối tác khác
StepProcess                  int             NOT NULL
SalesIsReturn                tinyint         NOT NULL   -- 0 = đơn bán, 1 = đơn trả hàng
MemberCardNo                 nvarchar(30)    NOT NULL
MemberPoint                  float           NULL
ReturnedReceiptNo             nvarchar(20)   NOT NULL
ReturnedOrderNo               nvarchar(20)   NOT NULL
VATNumber                    nvarchar(20)    NOT NULL
VATTemplateCode               nvarchar(20)   NOT NULL
VATSerial                    nvarchar(20)    NOT NULL
TransactionType               int            NOT NULL
EventNo                      nvarchar(20)    NOT NULL
PrintedNumber                 int            NOT NULL
OrderTime                    datetime        NOT NULL
IsFullReturn                  tinyint        NOT NULL
SendingStatus                 nvarchar(10)   NOT NULL
BuyerName                    nvarchar(100)   NOT NULL
BillNumber                   nvarchar(20)    NOT NULL
StartingTime                  datetime       NOT NULL
EndingTime                   datetime        NOT NULL
RefKey1                      nvarchar(50)    NOT NULL
RefKey2                      nvarchar(50)    NOT NULL
Counter                      bigint          NULL       -- DEFAULT 0
CreatedDate                   datetime       NULL       -- DEFAULT getdate()
LastUpdated                   datetime       NULL       -- DEFAULT getdate()
IsOfflineVinID                bit            NULL
IsAwardVinID                  bit            NULL
MemberPointsEarn              float          NULL
MemberPointsRedeem            float          NULL
IsExtraPoint                  bit            NULL
VinidCsn                     varchar(20)     NULL
CompanyCodeEmp               varchar(20)     NULL
DocumentType                  nvarchar(50)   NULL
UserVoid                     nvarchar(50)    NULL
MemberPointsRedeemReturned    float          NULL
ReturnVoucherNo               nvarchar(50)   NULL
ReturnVoucherExpire           nvarchar(50)   NULL
Ref1                         nvarchar(500)   NULL
Ref2                         nvarchar(500)   NULL
Ref3                         nvarchar(500)   NULL
Ref4                         nvarchar(500)   NULL
Ref5                         nvarchar(500)   NULL
Ref6                         nvarchar(500)   NULL
Ref7                         nvarchar(500)   NULL
Ref8                         nvarchar(500)   NULL
Ref9                         nvarchar(500)   NULL
Ref10                        nvarchar(500)   NULL
CardLevel                    varchar(50)     NULL
IsPrintedLabel                 bit           NULL
Label                        nvarchar(500)   NULL
MemberClub                   varchar(50)     NULL
Note                         nvarchar(500)   NULL
RefKey3                      nvarchar(50)    NULL
RefKey4                      nvarchar(50)    NULL
RefKey5                      nvarchar(50)    NULL
RefKey6                      nvarchar(50)    NULL
RefKey7                      nvarchar(50)    NULL
RefKey8                      nvarchar(50)    NULL
RefKey9                      nvarchar(50)    NULL
RefKey10                     nvarchar(50)    NULL
SalesType                    nvarchar(50)    NULL
```
> Bảng header giao dịch bán hàng chính. `OrderNo` là khoá nghiệp vụ toàn hệ thống, liên kết ngầm
> tới `TransLine.DocumentNo` và hầu hết các bảng `Trans*Entry`/`Trans*Line` khác qua `OrderNo`.

### TransHeaderOrig
PK: `ID`
```
ID                            bigint          NOT NULL   -- IDENTITY(1,1)
OrderNo                       nvarchar(20)    NULL
OrderDate                     datetime        NULL
CustomerNo                    nvarchar(20)    NULL
CustomerName                  nvarchar(100)   NULL
Address                       nvarchar(150)   NULL
PhoneNo                       nvarchar(20)    NULL
TablePhoneNo                  nvarchar(20)    NULL
HouseNo                       nvarchar(50)    NULL
CityNo                        nvarchar(30)    NULL
DistrictNo                    nvarchar(30)    NULL
WardNo                        nvarchar(30)    NULL
StreetNo                      nvarchar(30)    NULL
ShipToCityNo                  nvarchar(30)    NULL
ShipToDistrictNo               nvarchar(30)   NULL
ShipToWardNo                  nvarchar(30)    NULL
ShipToStreetNo                nvarchar(30)    NULL
DeliveryDate                  datetime        NULL
DeliveryTimeFrom               datetime       NULL
DeliveryTimeTo                 datetime       NULL
ZoneNo                        nvarchar(20)    NULL
ShipToAddress                 nvarchar(150)   NULL
DeliveryComment                nvarchar(150)  NULL
BillToName                    nvarchar(150)   NULL
BillToAddress                 nvarchar(150)   NULL
VATRegistrationNo             nvarchar(20)    NULL
StoreNo                       nvarchar(10)    NULL
POSTerminalNo                 nvarchar(10)    NULL
ShiftNo                       nvarchar(10)    NULL
CashierID                     nvarchar(10)    NULL
DiscountAmount                float           NULL
AmountExclVAT                 float           NULL
VATAmount                     float           NULL
AmountInclVAT                 float           NULL
GeneralComment                 nvarchar(150)  NULL
UserID                        nvarchar(50)    NULL
PrepaymentAmount               float          NULL
DeliveringMethod               int            NULL
PaymentAtPOSAmount             float          NULL
InChangeAmount                 float          NULL
OrderStatus                   int             NULL
ShipToName                    nvarchar(150)   NULL
IsTenancy                     tinyint         NULL
InInstalments                 tinyint         NULL
ShipToHouseNo                 nvarchar(50)    NULL
ShipToPhoneNo                 nvarchar(20)    NULL
AmountDiscountAtPOS            float          NULL
IssuedVATInvoice               tinyint        NULL
VoucherDiscountNo             nvarchar(20)    NULL
VoucherDiscountValue           float          NULL
PointConversionRate            float          NULL
TanencyNo                     nvarchar(20)    NULL
StepProcess                   int             NULL
SalesIsReturn                 tinyint         NULL
MemberCardNo                  nvarchar(30)    NULL
MemberPoint                   float           NULL
ReturnedReceiptNo              nvarchar(20)   NULL
ReturnedOrderNo                nvarchar(20)   NULL
VATNumber                     nvarchar(20)    NULL
VATTemplateCode                nvarchar(20)   NULL
VATSerial                     nvarchar(20)    NULL
TransactionType                int            NULL
EventNo                       nvarchar(20)    NULL
PrintedNumber                  int            NULL
OrderTime                     datetime        NULL
IsFullReturn                   tinyint        NULL
SendingStatus                  nvarchar(10)   NULL
BuyerName                     nvarchar(100)   NULL
BillNumber                    nvarchar(20)    NULL
StartingTime                   datetime       NULL
EndingTime                    datetime        NULL
RefKey1                       nvarchar(50)    NULL
RefKey2                       nvarchar(50)    NULL
Counter                       bigint          NULL
CreatedDate                    datetime       NULL
LastUpdated                    datetime       NULL
IsOfflineVinID                 bit            NULL
IsAwardVinID                   bit            NULL
MemberPointsEarn               float          NULL
MemberPointsRedeem             float          NULL
IsExtraPoint                   bit            NULL
VinidCsn                      varchar(20)     NULL
CompanyCodeEmp                varchar(20)     NULL
DocumentType                   nvarchar(50)   NULL
UserVoid                      nvarchar(50)    NULL
MemberPointsRedeemReturned     float          NULL
ReturnVoucherNo                nvarchar(50)   NULL
ReturnVoucherExpire            nvarchar(50)   NULL
IsUpdated                     bit             NULL       -- DEFAULT 0
```
> Bản lưu **trước khi cập nhật** của `TransHeader` (audit trail) — cấu trúc gần như trùng khớp
> `TransHeader` (thiếu `timestamp`; thiếu các cột `Ref1-10`/`CardLevel`/`IsPrintedLabel`/`Label`/
> `MemberClub`/`Note`/`RefKey3-10`/`SalesType` được thêm về sau vào `TransHeader`; thêm `ID`
> identity + `IsUpdated`). Mọi cột (trừ `ID`) đều `NULL`-able dù `TransHeader` gốc có cột
> `NOT NULL`, vì đây là bảng lưu bản chụp trước cập nhật.

### TransLine
PK: `LineNo` + `DocumentNo` (kèm UNIQUE `DocumentNo`+`LineNo`)
```
timestamp                    timestamp       NOT NULL
LineNo                       int             NOT NULL
DocumentNo                   nvarchar(20)    NOT NULL   -- = TransHeader.OrderNo
LineType                     int             NULL
LocationCode                 nvarchar(20)    NULL
ItemNo                       nvarchar(20)    NULL
Description                  nvarchar(500)   NULL
UnitOfMeasure                 nvarchar(20)   NULL
Quantity                     float           NULL
UnitPrice                    float           NULL
OfferUnitPrice                float          NULL
AmountBeforeDiscount           float         NULL
DiscountPercent               float          NULL
DiscountAmount                float          NULL
VATPercent                   float           NULL
VATAmount                    float           NULL
LineAmountIncVAT               float         NULL
OfferNo                      nvarchar(20)    NULL
DiscountType                  int            NULL
TriggerQuantity                float         NULL
StaffID                      nvarchar(20)    NULL
PrepaymentAmount              float          NULL
VATCode                      nvarchar(20)    NULL       -- 1=Non Tax,2=0%,3/5=5%,4=10%,6=8% (theo Rpt_GetRevenueSalesLists)
DeliveringMethod              int            NULL
Barcode                      nvarchar(50)    NULL
OrderDiscountPercent           float         NULL
OrderDiscountAmount            float         NULL
DivisionCode                  nvarchar(10)   NULL
CategoryCode                  nvarchar(10)   NULL
ProductGroupCode               nvarchar(10)  NULL
BlockedMemberPoint             tinyint       NULL       -- 0 = có tích điểm, khác 0 = không tích điểm (theo Rpt_GetRevenueSalesLists)
SerialNo                     nvarchar(100)   NULL
OrigTransStore                 nvarchar(10)  NULL
OrigTransPos                  nvarchar(10)   NULL
OrigTransNo                   int            NULL
OrigTransLineNo                int           NULL
OrigOrderNo                   nvarchar(20)   NULL
OrigLineNumber                 int           NULL
BlockedPromotion               tinyint       NULL
Infocodes                    nvarchar(30)    NULL
MemberPointsEarn              float          NULL
ReturnedQuantity               float         NULL
DeliveryQuantity               float         NULL
DeliveryStatus                 int           NULL
MemberPointsRedeem             float         NULL
VariantNo                    nvarchar(20)    NULL
LotNo                        nvarchar(20)    NULL
ExpireDate                    datetime       NULL
AmountCalPoint                 float         NULL
WaitingListNo                  nvarchar(20)  NULL
ArticleType                  nvarchar(20)    NULL
ScanTime                      datetime       NULL
DiscountCode                  varchar(50)    NULL
PromotionGroup                 varchar(50)   NULL
Counter                      bigint          NULL       -- DEFAULT 0
LineNoEffect                  int            NULL
LastUpdated                   datetime       NULL       -- DEFAULT getdate()
DiscountQuantity               float         NULL       -- DEFAULT 0
DiscountUnit                  float          NULL       -- DEFAULT 0
SnGLineID                    int             NULL
QtyDiscount                   float          NULL
QtyDiscountReturned             float        NULL
GuiID                        varchar(100)    NULL       -- DEFAULT ''
CreatedDate                   datetime       NULL       -- DEFAULT getdate()
IsPreventedNegativeInventory    bit          NULL
Inventory                    float           NULL
Ref1                          nvarchar(500)  NULL
Ref2                          nvarchar(500)  NULL
Ref3                          nvarchar(500)  NULL
Ref4                          nvarchar(500)  NULL
Ref5                          nvarchar(500)  NULL
Ref6                          nvarchar(500)  NULL
Ref7                          nvarchar(500)  NULL
Ref8                          nvarchar(500)  NULL
Ref9                          nvarchar(500)  NULL
Ref10                         nvarchar(500)  NULL
ChangeCardCodeValue            nvarchar(100) NULL
ChangeCardId                  nvarchar(100)  NULL
ChangeCardOTP                  bit           NULL
ChangeCardType                 nvarchar(20)  NULL
ComboName                    nvarchar(200)   NULL
ComboQuantity                  float         NULL
ComboType                    varchar(20)     NULL
GiftGroup                    nvarchar(50)    NULL
GiftRedeemPoint                float         NULL
Group                        nvarchar(50)    NULL
IsAllowReturn                  bit           NULL
IsBirthdayGift                 bit           NULL
IsChangeCardFee                bit           NULL
IsCombo                      bit             NULL
IsDynamicPrice                 bit           NULL
IsFee                        bit             NULL
IsGift                       bit             NULL
IsGiftRedeem                  bit            NULL
IsTopping                    bit             NULL
IsVAT                        bit             NULL
Note                         nvarchar(500)   NULL
ParentCode                   nvarchar(20)    NULL
```

### TransLineOrig
PK: `ID`
```
ID                            bigint          NOT NULL   -- IDENTITY(1,1)
LineNo                        int             NOT NULL
DocumentNo                    nvarchar(20)    NOT NULL
LineType                      int             NULL
LocationCode                  nvarchar(20)    NULL
ItemNo                        nvarchar(20)    NULL
Description                   nvarchar(100)   NULL
UnitOfMeasure                  nvarchar(20)   NULL
Quantity                      float           NULL
UnitPrice                     float           NULL
OfferUnitPrice                 float          NULL
AmountBeforeDiscount            float         NULL
DiscountPercent                float          NULL
DiscountAmount                 float          NULL
VATPercent                    float           NULL
VATAmount                     float           NULL
LineAmountIncVAT                float         NULL
OfferNo                       nvarchar(20)    NULL
DiscountType                   int            NULL
TriggerQuantity                 float         NULL
StaffID                       nvarchar(20)    NULL
PrepaymentAmount               float          NULL
VATCode                       nvarchar(20)    NULL
DeliveringMethod               int            NULL
Barcode                       nvarchar(20)    NULL
OrderDiscountPercent            float         NULL
OrderDiscountAmount             float        NULL
DivisionCode                   nvarchar(10)   NULL
CategoryCode                   nvarchar(10)   NULL
ProductGroupCode                nvarchar(10)  NULL
BlockedMemberPoint              tinyint       NULL
SerialNo                      nvarchar(30)    NULL
OrigTransStore                  nvarchar(10)  NULL
OrigTransPos                   nvarchar(10)   NULL
OrigTransNo                    int            NULL
OrigTransLineNo                 int           NULL
OrigOrderNo                    nvarchar(20)   NULL
OrigLineNumber                  int           NULL
BlockedPromotion                tinyint       NULL
Infocodes                     nvarchar(30)    NULL
MemberPointsEarn                float         NULL
ReturnedQuantity                 float        NULL
DeliveryQuantity                 float        NULL
DeliveryStatus                  int           NULL
MemberPointsRedeem               float        NULL
VariantNo                     nvarchar(20)    NULL
LotNo                         nvarchar(20)    NULL
ExpireDate                    datetime        NULL
AmountCalPoint                  float         NULL
WaitingListNo                   nvarchar(20)  NULL
ArticleType                   nvarchar(20)    NULL
ScanTime                      datetime        NULL
DiscountCode                   varchar(50)    NULL
PromotionGroup                  varchar(50)   NULL
Counter                       bigint          NULL
LineNoEffect                   int            NULL
LastUpdated                    datetime       NULL
DiscountQuantity                 float        NULL
DiscountUnit                   float          NULL
SnGLineID                     int             NULL
QtyDiscount                    float          NULL
QtyDiscountReturned              float        NULL
GuiID                         varchar(100)    NULL
CreatedDate                    datetime       NULL
```
> Bản lưu trước khi cập nhật của `TransLine` (audit trail) — cấu trúc gần trùng `TransLine` nhưng
> thiếu `timestamp`; thiếu các cột mở rộng thêm sau này vào `TransLine`: `Inventory`,
> `IsPreventedNegativeInventory`, `Ref1-Ref10`, `ChangeCard*` (4 cột), `Combo*` (3 cột),
> `GiftGroup`, `GiftRedeemPoint`, `Group`, các cột `Is*` cuối bảng (`IsAllowReturn`,
> `IsBirthdayGift`, `IsChangeCardFee`, `IsCombo`, `IsDynamicPrice`, `IsFee`, `IsGift`,
> `IsGiftRedeem`, `IsTopping`, `IsVAT`), `Note`, `ParentCode`. Ngoài ra `Description` chỉ
> `nvarchar(100)` (thay vì `500`), `Barcode`/`SerialNo` cũng ngắn hơn bản `TransLine` hiện tại.

### TransInfocodeEntry
PK: `OrderNo` + `OrderLineNo` + `LineNo`
```
timestamp         timestamp       NOT NULL
OrderNo           nvarchar(20)    NOT NULL
OrderLineNo       int             NOT NULL
LineNo            int             NOT NULL
TransactionType   int             NOT NULL
Infocode          nvarchar(20)    NOT NULL
Infomation        nvarchar(255)   NULL
TypeOfInput       int             NOT NULL
TextType          int             NOT NULL
ItemNo            nvarchar(20)    NOT NULL
SourceCode        nvarchar(20)    NOT NULL
Amount            float           NULL
SubCode           nvarchar(20)    NOT NULL
ParentLineNo      int             NOT NULL
Counter           bigint          NULL       -- DEFAULT 0
LastUpdated       datetime        NULL       -- DEFAULT getdate()
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```

### TransInputData
PK: `TransNo` + `LineNumber`
```
timestamp         timestamp       NOT NULL
TransNo           nvarchar(20)    NOT NULL
LineNumber        int             NOT NULL
TableName         nvarchar(50)    NOT NULL
DataType          nvarchar(100)   NOT NULL
DataValue         nvarchar(250)   NOT NULL
Counter           bigint          NULL       -- DEFAULT 0
LastUpdated       datetime        NULL       -- DEFAULT getdate()
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```
> Key/value input phụ theo dòng giao dịch (`Rpt_ReportSaleDetail_Insert` dùng `DataType`
> để tra `SOURCEBILL`/`HANDLINGSTAFF`).

### TransOptionLine
PK: `OrderNo` + `LineNo`
```
OrderNo           nvarchar(20)    NOT NULL
LineNo            int             NOT NULL
OrderLineNo       int             NULL
SaleTypeCode      nvarchar(50)    NULL
Type              nvarchar(50)    NULL
ItemNo            nvarchar(50)    NULL
UOM               nvarchar(50)    NULL
Description       nvarchar(200)   NULL
OptionType        nvarchar(50)    NULL
OptionName        nvarchar(50)    NULL
OptionQuantity    float           NULL
Note              nvarchar(500)   NULL
ItemNoRef         varchar(20)     NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
IsDefault         bit             NULL
```

### TransPartnerLine
PK: `OrderNo` + `LineNo`
```
OrderNo           nvarchar(20)    NOT NULL
LineNo            int             NOT NULL
OrderLineNo       int             NULL
PartnerCode       nvarchar(50)    NULL
ServiceType       nvarchar(50)    NULL
ServiceCode       nvarchar(50)    NULL
Serial            nvarchar(100)   NULL
Pin               nvarchar(100)   NULL
Expiry            datetime        NULL
Value             float           NULL
Note              nvarchar(500)   NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```

### TransPaymentEntry
PK: `OrderNo` + `LineNo`
```
timestamp             timestamp       NOT NULL
OrderNo               nvarchar(20)    NOT NULL
LineNo                int             NOT NULL
StoreNo               nvarchar(10)    NOT NULL
POSTerminalNo         nvarchar(10)    NOT NULL
TransactionNo         int             NOT NULL
ReceiptNo             nvarchar(20)    NOT NULL
StatementCode         nvarchar(20)    NOT NULL
CardNo                nvarchar(50)    NOT NULL
ExchangeRate          float           NULL
TenderType            nvarchar(20)    NOT NULL   -- 'PTCS' = tiền mặt, 'ZCRE' = ngân hàng (theo GetSalesEODConfirm)
TenderTypeName        nvarchar(50)    NULL
AmountTendered        float           NULL
CurrencyCode          nvarchar(20)    NOT NULL
AmountInCurrency      float           NULL
CardOrAccount         nvarchar(20)    NOT NULL
PaymentDate           datetime        NOT NULL
PaymentTime           datetime        NOT NULL
ShiftNo               nvarchar(10)    NOT NULL
ShiftDate             datetime        NOT NULL
StaffID               nvarchar(20)    NOT NULL
CardPaymentType       int             NOT NULL
CardValue             float           NULL
ReferenceNo           nvarchar(500)   NULL
PayForOrderNo         nvarchar(20)    NOT NULL
Counter               bigint          NULL       -- DEFAULT 0
ApprovalCode          nvarchar(50)    NULL
BankPOSCode           nvarchar(20)    NULL
BankCardType          nvarchar(20)    NULL
IsOnline              bit             NULL
LastUpdated           datetime        NULL       -- DEFAULT getdate()
CreatedDate           datetime        NULL       -- DEFAULT getdate()
CommissionPercent     float           NULL
CustomerPaymentCode   nvarchar(100)   NULL
GiftRedeemPoint       float           NULL
IsCommission          bit             NULL
OrderLineNo           int             NULL
Partner               nvarchar(50)    NULL
PaymentType           nvarchar(100)   NULL
TransactionId         nvarchar(100)   NULL
```

### TransPaymentInfo
PK: `OrderNo` + `LineNo`
```
OrderNo           nvarchar(20)    NOT NULL
LineNo            int             NOT NULL
PaymentLineNo     int             NOT NULL
SourceType        nvarchar(50)    NULL
Code              nvarchar(200)   NOT NULL
Value             nvarchar(max)   NOT NULL
SeriNo            varchar(500)    NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```

### TransPointLine
PK: `OrderNo` + `LineNo`
```
OrderNo                   nvarchar(20)    NOT NULL
LineNo                    int             NOT NULL
OrderLineNo               int             NULL
ClubCode                  nvarchar(50)    NULL
MerchantId                nvarchar(50)    NULL
EarnPoints                float           NULL
RedeemPoints              float           NULL
MemberNumber              nvarchar(100)   NULL
RedeemPointsReturned      float           NULL
ReceiptNumber             nvarchar(100)   NULL
OrigReceiptNumber         nvarchar(100)   NULL
ReferenceNumber           nvarchar(100)   NULL
OrigReferenceNumber       nvarchar(100)   NULL
MemberName                nvarchar(200)   NULL
CardLevel                 nvarchar(50)    NULL
MemberCSN                 nvarchar(50)    NULL
MemberPoint               float           NULL
TotalPoint                float           NULL
```

---

## Transaction Void

### TransVoidHeader
PK: `OrderNo`
```
timestamp                     timestamp       NOT NULL
OrderNo                       nvarchar(20)    NOT NULL
OrderDate                     datetime        NOT NULL
CustomerNo                    nvarchar(20)    NOT NULL
CustomerName                  nvarchar(100)   NOT NULL
Address                       nvarchar(150)   NOT NULL
PhoneNo                       nvarchar(20)    NOT NULL
TablePhoneNo                  nvarchar(20)    NOT NULL
HouseNo                       nvarchar(50)    NOT NULL
CityNo                        nvarchar(30)    NOT NULL
DistrictNo                    nvarchar(30)    NOT NULL
WardNo                        nvarchar(30)    NOT NULL
StreetNo                      nvarchar(30)    NOT NULL
ShipToCityNo                  nvarchar(30)    NOT NULL
ShipToDistrictNo              nvarchar(30)    NOT NULL
ShipToWardNo                  nvarchar(30)    NOT NULL
ShipToStreetNo                nvarchar(30)    NOT NULL
DeliveryDate                  datetime        NOT NULL
DeliveryTimeFrom              datetime        NOT NULL
DeliveryTimeTo                datetime        NOT NULL
ZoneNo                        nvarchar(20)    NOT NULL
ShipToAddress                 nvarchar(150)   NOT NULL
DeliveryComment                nvarchar(150)  NOT NULL
BillToName                    nvarchar(150)   NOT NULL
BillToAddress                 nvarchar(150)   NOT NULL
VATRegistrationNo             nvarchar(20)    NOT NULL
StoreNo                       nvarchar(10)    NOT NULL
POSTerminalNo                 nvarchar(10)    NOT NULL
ShiftNo                       nvarchar(10)    NOT NULL
CashierID                     nvarchar(10)    NOT NULL
DiscountAmount                float           NULL
AmountExclVAT                 float           NULL
VATAmount                     float           NULL
AmountInclVAT                 float           NULL
GeneralComment                 nvarchar(150)  NOT NULL
UserID                        nvarchar(50)    NOT NULL
PrepaymentAmount               float          NULL
DeliveringMethod               int            NOT NULL
PaymentAtPOSAmount             float          NULL
InChangeAmount                 float          NULL
OrderStatus                   int             NOT NULL
ShipToName                    nvarchar(150)   NOT NULL
IsTenancy                     tinyint         NOT NULL
InInstalments                  tinyint        NOT NULL
ShipToHouseNo                  nvarchar(50)   NOT NULL
ShipToPhoneNo                  nvarchar(20)   NOT NULL
AmountDiscountAtPOS            float          NULL
IssuedVATInvoice               tinyint        NOT NULL
VoucherDiscountNo             nvarchar(20)    NOT NULL
VoucherDiscountValue           float          NULL
PointConversionRate            float          NULL
TanencyNo                     nvarchar(20)    NOT NULL
StepProcess                   int             NOT NULL
SalesIsReturn                 tinyint         NOT NULL
MemberCardNo                  nvarchar(30)    NOT NULL
MemberPoint                   float           NULL
ReturnedReceiptNo              nvarchar(20)   NOT NULL
ReturnedOrderNo                nvarchar(20)   NOT NULL
VATNumber                     nvarchar(20)    NOT NULL
VATTemplateCode                nvarchar(20)   NOT NULL
VATSerial                     nvarchar(20)    NOT NULL
TransactionType                int            NOT NULL
EventNo                       nvarchar(20)    NOT NULL
PrintedNumber                  int            NOT NULL
OrderTime                     datetime        NOT NULL
IsFullReturn                   tinyint        NOT NULL
SendingStatus                  nvarchar(10)   NOT NULL
BuyerName                     nvarchar(100)   NOT NULL
BillNumber                    nvarchar(20)    NOT NULL
StartingTime                   datetime       NOT NULL
EndingTime                    datetime        NOT NULL
RefKey1                       nvarchar(50)    NOT NULL
RefKey2                       nvarchar(50)    NOT NULL
Counter                       bigint          NULL       -- DEFAULT 0
LastUpdated                    datetime       NULL       -- DEFAULT getdate()
DocumentType                   nvarchar(50)   NULL
UserVoid                      nvarchar(50)    NULL
MemberPointsRedeemReturned     float          NULL
ReturnVoucherNo                nvarchar(50)   NULL
ReturnVoucherExpire            nvarchar(50)   NULL
CreatedDate                    datetime       NULL       -- DEFAULT getdate()
Ref1                           nvarchar(500)   NULL
Ref2                           nvarchar(500)   NULL
Ref3                           nvarchar(500)   NULL
Ref4                           nvarchar(500)   NULL
Ref5                           nvarchar(500)   NULL
Ref6                           nvarchar(500)   NULL
Ref7                           nvarchar(500)   NULL
Ref8                           nvarchar(500)   NULL
Ref9                           nvarchar(500)   NULL
Ref10                          nvarchar(500)   NULL
IsPrintedLabel                  bit           NULL
Label                         nvarchar(500)   NULL
MemberClub                    varchar(50)     NULL
Note                          nvarchar(500)   NULL
RefKey10                      nvarchar(50)    NULL
RefKey3                       nvarchar(50)    NULL
RefKey4                       nvarchar(50)    NULL
RefKey5                       nvarchar(50)    NULL
RefKey6                       nvarchar(50)    NULL
RefKey7                       nvarchar(50)    NULL
RefKey8                       nvarchar(50)    NULL
RefKey9                       nvarchar(50)    NULL
SalesType                     nvarchar(50)    NULL
```
> Cấu trúc gần như song song `TransHeader` — chứa dữ liệu giao dịch **đã bị void/huỷ**
> (`UserVoid` = người huỷ). Lưu ý thứ tự cột `RefKey10` xuất hiện **trước** `RefKey3..RefKey9`
> trong định nghĩa gốc (khác `TransHeader` liệt kê `RefKey3..RefKey10` theo thứ tự tăng dần) —
> giữ đúng thứ tự nguồn để đối chiếu 1-1 khi cần.

### TransVoidLine
PK: `LineNo` + `DocumentNo`
```
timestamp                     timestamp       NOT NULL
LineNo                        int             NOT NULL
DocumentNo                    nvarchar(20)    NOT NULL
LineType                      int             NULL
LocationCode                  nvarchar(20)    NULL
ItemNo                        nvarchar(20)    NULL
Description                   nvarchar(500)   NULL
UnitOfMeasure                  nvarchar(20)   NULL
Quantity                      float           NULL
UnitPrice                     float           NULL
OfferUnitPrice                 float          NULL
AmountBeforeDiscount            float         NULL
DiscountPercent                float          NULL
DiscountAmount                 float          NULL
VATPercent                    float           NULL
VATAmount                     float           NULL
LineAmountIncVAT                float         NULL
OfferNo                       nvarchar(20)    NULL
DiscountType                   int            NULL
TriggerQuantity                 float         NULL
StaffID                       nvarchar(20)    NULL
PrepaymentAmount               float          NULL
VATCode                       nvarchar(20)    NULL
DeliveringMethod               int            NULL
Barcode                       nvarchar(50)    NULL
OrderDiscountPercent            float         NULL
OrderDiscountAmount             float        NULL
DivisionCode                   nvarchar(10)   NULL
CategoryCode                   nvarchar(10)   NULL
ProductGroupCode                nvarchar(10)  NULL
BlockedMemberPoint              tinyint       NULL
SerialNo                      nvarchar(100)   NULL
OrigTransStore                  nvarchar(10)  NULL
OrigTransPos                   nvarchar(10)   NULL
OrigTransNo                    int            NULL
OrigTransLineNo                 int           NULL
OrigOrderNo                    nvarchar(20)   NULL
OrigLineNumber                  int           NULL
BlockedPromotion                tinyint       NULL
Infocodes                     nvarchar(30)    NULL
MemberPointsEarn                float         NULL
ReturnedQuantity                 float        NULL
DeliveryQuantity                 float        NULL
DeliveryStatus                  int           NULL
MemberPointsRedeem               float        NULL
VariantNo                      nvarchar(20)   NULL
LotNo                         nvarchar(20)    NULL
ExpireDate                     datetime       NULL
AmountCalPoint                  float         NULL
WaitingListNo                   nvarchar(20)  NULL
ArticleType                    nvarchar(20)   NULL
ScanTime                       datetime       NULL
Counter                       bigint          NULL       -- DEFAULT 0
LineNoEffect                   int            NULL
LastUpdated                    datetime       NULL       -- DEFAULT getdate()
QtyDiscount                    float          NULL
QtyDiscountReturned              float        NULL
GuiID                         varchar(100)    NULL       -- DEFAULT ''
CreatedDate                    datetime       NULL       -- DEFAULT getdate()
IsPreventedNegativeInventory     bit          NULL
Inventory                      float          NULL
Ref1                           nvarchar(500)  NULL
Ref2                           nvarchar(500)  NULL
Ref3                           nvarchar(500)  NULL
Ref4                           nvarchar(500)  NULL
Ref5                           nvarchar(500)  NULL
Ref6                           nvarchar(500)  NULL
Ref7                           nvarchar(500)  NULL
Ref8                           nvarchar(500)  NULL
Ref9                           nvarchar(500)  NULL
Ref10                          nvarchar(500)  NULL
ChangeCardCodeValue             nvarchar(100) NULL
ChangeCardId                   nvarchar(100)  NULL
ChangeCardOTP                   bit           NULL
ChangeCardType                  nvarchar(20)  NULL
ComboName                      nvarchar(200)  NULL
ComboQuantity                   float         NULL
ComboType                      varchar(20)    NULL
GiftGroup                      nvarchar(50)   NULL
GiftRedeemPoint                 float         NULL
Group                          nvarchar(50)   NULL
IsAllowReturn                   bit           NULL
IsBirthdayGift                  bit           NULL
IsChangeCardFee                 bit           NULL
IsCombo                        bit            NULL
IsDynamicPrice                  bit           NULL
IsFee                          bit            NULL
IsGift                         bit            NULL
IsGiftRedeem                    bit           NULL
IsTopping                      bit            NULL
IsVAT                          bit            NULL
Note                           nvarchar(500)  NULL
ParentCode                     nvarchar(20)   NULL
```
> Cấu trúc gần như trùng khớp `TransLine` — chi tiết dòng của giao dịch đã bị void.

---

## Transaction Discount/Bonus/Point/Coupon-Voucher

### TransBonus
PK: `OrderNo` + `LineNo` + `OrderLineNo` + `SourceCode`
```
timestamp                 timestamp       NOT NULL
OrderNo                   nvarchar(20)    NOT NULL
LineNo                    int             NOT NULL
SourceCode                nvarchar(20)    NOT NULL
OrderLineNo               decimal(12, 0)  NOT NULL
Extra_Quantity            float           NULL
Extra_Amount              float           NULL
Extra_VinPoint_Earn       float           NULL
Extra_VinPoint_Redeem     float           NULL
Status                    tinyint         NOT NULL
ReferenceNo               nvarchar(24)    NULL
SerialNo                  nvarchar(24)    NULL
Counter                   bigint          NULL       -- DEFAULT 0
CreatedDate               datetime        NULL       -- DEFAULT getdate()
LastUpdated               datetime        NULL       -- DEFAULT getdate()
```
> Ghi chú `Sale_InsertToTableByJsonV2`: bảng này hiện **KHÔNG dùng** (`IF @table_name =
> 'TransBonus' return` — skip có chủ đích từ 17/10/2024).

### TransBluePoint
PK: `OrderNo` + `LineNo`
```
timestamp                          timestamp       NOT NULL
TransactionType                    int             NULL
LineNo                             int             NOT NULL
OrderNo                            varchar(50)     NOT NULL
OrderLineNo                        int             NULL
ItemNo                             varchar(20)     NULL
Uom                                varchar(10)     NULL
Barcode                            varchar(50)     NULL
Quantity                           float           NULL
UnitPrice                          float           NULL
Amount                             float           NULL
DiscountAmount                     float           NULL
LineAmount                         float           NULL
ExtraQuantityEarn                  float           NULL
ExtraAmountEarn                    float           NULL
ExtraQuantityNotEarn               float           NULL
ExtraQuantityNotEarnReturned       float           NULL
OrigOrderNo                        varchar(50)     NULL
OrigLineNumber                     int             NULL
CreatedDate                        datetime        NULL       -- DEFAULT getdate()
DiscountType                       varchar(20)     NULL
RecordNo                           int             NULL
OrigReferenceNumber                varchar(50)     NULL
ReferenceNumber                    varchar(50)     NULL
ReceiptNumber                      varchar(50)     NULL
OrigReceiptNumber                  varchar(50)     NULL
VinidCsn                           varchar(20)     NULL
CompanyCodeEmp                     varchar(20)     NULL
EmployeeCode                       varchar(20)     NULL
```

### TransBluePointOrig
PK: `ID`
```
ID                                  bigint          NOT NULL   -- IDENTITY(1,1)
TransactionType                    int             NULL
LineNo                              int             NOT NULL
OrderNo                             varchar(50)     NOT NULL
OrderLineNo                         int             NULL
ItemNo                              varchar(20)     NULL
Uom                                 varchar(10)     NULL
Barcode                             varchar(50)     NULL
Quantity                            float           NULL
UnitPrice                           float           NULL
Amount                              float           NULL
DiscountAmount                      float           NULL
LineAmount                          float           NULL
ExtraQuantityEarn                   float           NULL
ExtraAmountEarn                     float           NULL
ExtraQuantityNotEarn                float           NULL
ExtraQuantityNotEarnReturned        float           NULL
OrigOrderNo                         varchar(50)     NULL
OrigLineNumber                      int             NULL
CreatedDate                         datetime        NULL
DiscountType                        varchar(20)     NULL
RecordNo                            int             NULL
OrigReferenceNumber                 varchar(50)     NULL
ReferenceNumber                     varchar(50)     NULL
ReceiptNumber                       varchar(50)     NULL
OrigReceiptNumber                   varchar(50)     NULL
VinidCsn                            varchar(20)     NULL
CompanyCodeEmp                      varchar(20)     NULL
EmployeeCode                        varchar(20)     NULL
```
> Bản lưu trước khi cập nhật của `TransBluePoint` (audit trail) — cấu trúc trùng khớp
> `TransBluePoint` nhưng thiếu cột `timestamp`, thay bằng `ID` identity làm PK.

### TransCpnVchIssue
PK: `timestamp` (dùng chính cột rowversion làm khoá)
```
timestamp                 timestamp       NOT NULL
OrderNo                   varchar(50)     NULL
Voucher_Type              varchar(5)      NULL
SerialNo                  varchar(20)     NOT NULL
Voucher_Value             float           NULL
Voucher_Currency          varchar(10)     NULL
Validity_From_Date        date            NULL
Expiry_Date               date            NULL
Processing_Type           varchar(5)      NULL
Status                    varchar(5)      NOT NULL   -- 'SOLD' = đã bán, 'EXP' = đã trả hàng
Site                      varchar(5)      NULL
Article_No                varchar(20)     NULL
ItemName                  nvarchar(500)   NULL
Bonus_Buy                 varchar(20)     NULL
POSNo                     varchar(5)      NULL
ReceiptNo                 varchar(30)     NULL
TranDate                  date            NULL
TranTime                  time(7)         NULL
CouponDiscType            int             NULL       -- 1 = %, 2 = Amount (extended property MS_Description)
MaxQtyUse                 int             NULL
MaxAmount                 float           NULL
MaxQuantityIssue          int             NULL
ApplyType                 varchar(10)     NULL       -- 'ZVCN'/'ZVCO'/'ZCOU'/'ZCPN' — loại coupon/voucher
IsVinID                   bit             NULL       -- 0 = All, 1 = Áp dụng theo VinID
IsOffline                 bit             NULL       -- DEFAULT 1
IsSend                    bit             NULL       -- DEFAULT 0; 0 = chưa gửi SAP, 1 = đã gửi
IsCheckItem               bit             NULL       -- 0 = áp dụng tổng bill, 1 = áp dụng theo item
Counter                   bigint          NULL       -- DEFAULT 1
CreatedDate               datetime        NULL       -- DEFAULT getdate()
LastUpdated               datetime        NULL       -- DEFAULT getdate()
FileName                  varchar(500)    NULL
RewardCodeTitle           nvarchar(200)   NULL
RewardCodeLink            nvarchar(200)   NULL
RewardCodeDescription     nvarchar(500)   NULL
IsMemberPromotion         bit             NULL
```

### TransDiscountCouponEntry
PK: `OrderNo` + `OrderLineNo` + `LineNo`
```
timestamp         timestamp       NOT NULL
OrderNo           nvarchar(20)    NOT NULL
OrderLineNo       int             NOT NULL
LineNo            int             NOT NULL
OfferType         nvarchar(20)    NOT NULL
OfferNo           nvarchar(20)    NOT NULL
Quantity          float           NULL
DiscountType      int             NOT NULL
DiscountAmount    float           NULL
Barcode           nvarchar(50)    NULL
ParentLineNo      int             NOT NULL
ItemNo            nvarchar(20)    NOT NULL
LineGroup         nvarchar(10)    NULL
Counter           bigint          NULL       -- DEFAULT 0
LastUpdated       datetime        NULL       -- DEFAULT getdate()
Company           nvarchar(100)   NULL
IsOffline         bit             NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
PhoneNumber       varchar(20)     NULL
Ref1              nvarchar(500)   NULL
Ref2              nvarchar(500)   NULL
Ref3              nvarchar(500)   NULL
Ref4              nvarchar(500)   NULL
Ref5              nvarchar(500)   NULL
IsCheckAPI        bit             NULL
IsTotalBill       bit             NULL
```

### TransDiscountEntry
PK: `OrderNo` + `OrderLineNo` + `LineNo`
```
timestamp         timestamp       NOT NULL
OrderNo           nvarchar(20)    NOT NULL
OrderLineNo       int             NOT NULL
LineNo            int             NOT NULL
OfferType         nvarchar(20)    NOT NULL
OfferNo           nvarchar(20)    NOT NULL
Quantity          float           NULL
DiscountType      int             NOT NULL
UnitPrice         float           NULL
DiscountAmount    float           NULL
Barcode           nvarchar(20)    NOT NULL
ParentLineNo      int             NOT NULL
ItemNo            nvarchar(20)    NOT NULL
UOM               varchar(10)     NULL
LineGroup         nvarchar(50)    NULL
IsTotalBill       bit             NULL
Counter           bigint          NULL       -- DEFAULT 0
LineNoEffect      int             NULL
LastUpdated       datetime        NULL       -- DEFAULT getdate()
IsVinID           bit             NULL
DiscountValue     float           NULL
GuiID             varchar(100)    NULL       -- DEFAULT ''
CusType           varchar(50)     NULL
Ref1              nvarchar(500)   NULL
Ref2              nvarchar(500)   NULL
Ref3              nvarchar(500)   NULL
Ref4              nvarchar(500)   NULL
Ref5              nvarchar(500)   NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
IsCondGet         varchar(20)     NULL
IsConfirmed       bit             NULL
StepValue         float           NULL
```

---

## Report

### ReportSaleDetail
PK: (none) — UNIQUE NONCLUSTERED (`OrderNo`, `LineNo`)
```
PromotionID              varchar(500)    NULL
CouponCode               varchar(500)    NULL
LineNo                   int             NOT NULL
OrderNo                  nvarchar(20)    NOT NULL
OrderTime                datetime        NOT NULL
OrderDate                datetime        NOT NULL
StoreNo                  nvarchar(10)    NOT NULL
POSTerminalNo            nvarchar(10)    NOT NULL
CashierID                nvarchar(10)    NOT NULL
ReturnedOrderNo          nvarchar(20)    NOT NULL
SalesIsReturn            tinyint         NOT NULL   -- 0 = đơn bán, 1 = đơn trả hàng
Barcode                  nvarchar(20)    NULL
ItemNo                   nvarchar(20)    NULL
Description              nvarchar(100)   NULL
UnitOfMeasure            nvarchar(20)    NULL
Quantity                 float           NULL
UnitPrice                float           NULL
DiscountAmount           float           NULL
VATCode                  nvarchar(20)    NULL
LineAmountIncVAT         float           NULL
VATAmount                float           NULL
HouseNo                  nvarchar(50)    NOT NULL
CityNo                   nvarchar(30)    NOT NULL
MemberCardNo             nvarchar(30)    NOT NULL
MemberPointsEarn         float           NULL
MemberPointsRedeem       float           NULL
BlockedMemberPoint       tinyint         NULL
AmountCalPoint           float           NULL
RefKey1                  nvarchar(50)    NOT NULL
VoucherDiscountNo        nvarchar(20)    NOT NULL
DeliveringMethod         int             NOT NULL
DivisionCode             nvarchar(10)    NULL
UserID                   nvarchar(50)    NOT NULL
SerialNo                 nvarchar(30)    NULL
DeliveryComment          nvarchar(150)   NOT NULL
TanencyNo                nvarchar(20)    NOT NULL
BusinessAreaNo           nvarchar(20)    NULL
StyleProfile             nvarchar(20)    NULL
CustomerName             nvarchar(100)   NOT NULL
AmountDiscountAtPOS      float           NULL
VATPercent               float           NULL
IsTenancy                tinyint         NOT NULL
SalesType                varchar(20)     NULL
SOURCEBILL               nvarchar(250)   NOT NULL   -- mặc định 'Winmart-Local' nếu đơn hàng tại cửa hàng
HANDLINGSTAFF            nvarchar(250)   NOT NULL
ReturnVoucherNo          nvarchar(50)    NULL
ReturnVoucherExpire      nvarchar(50)    NULL
ZoneNo                   nvarchar(20)    NULL
Ref1                     nvarchar(500)   NULL
Ref2                     nvarchar(500)   NULL
Ref3                     nvarchar(500)   NULL
Ref4                     nvarchar(500)   NULL
Ref5                     nvarchar(500)   NULL
Ref6                     nvarchar(500)   NULL
Ref7                     nvarchar(500)   NULL
Ref8                     nvarchar(500)   NULL
Ref9                     nvarchar(500)   NULL
Ref10                    nvarchar(500)   NULL
```
> Bảng báo cáo phi chuẩn hoá (denormalized), nạp bởi `Rpt_ReportSaleDetail_Insert` từ
> `TransHeader`+`TransLine` (join thêm `TransInputData` lấy `SOURCEBILL`/`HANDLINGSTAFF`). Dùng
> làm nguồn cho các report `Rpt_GetSalesSKUDailyList`, `Rpt_ReportTopProduct`.

---

## Sync

### SyncTableFromPOS
PK: `Id`
```
Id                int             NOT NULL   -- IDENTITY(1,1)
TableName         varchar(50)     NULL
GroupName         varchar(50)     NULL
DocumentNoName    varchar(50)     NULL
OrdBy             int             NULL
Status            int             NULL
```
> Danh mục cấu hình bảng đồng bộ từ POS lên `RPOSCentralSales` (không phải bảng dữ liệu giao dịch
> — chỉ metadata cấu hình).

---

## Logging / Error

### DataRawJson
PK: `Id`
```
TransactionId     varchar(30)         NOT NULL
DataType          varchar(20)         NOT NULL
Message           nvarchar(max)       NOT NULL
Flag              bit                 NOT NULL
ErrorMessage      nvarchar(max)       NULL
CrtDate           datetime            NOT NULL
Id                uniqueidentifier    NOT NULL   -- ROWGUIDCOL
Source            nvarchar(20)        NULL
```

### EmailSentLog
PK: (none)
```
listEmailTo       varchar(2000)   NULL
lstrSubject       nvarchar(2000)  NULL
date              date            NULL
NumberSent        int             NULL
createdDate       datetime        NULL       -- DEFAULT getdate()
```

### Interface_Errors
PK: `ErrorID`
```
ErrorID           int             NOT NULL   -- IDENTITY(1,1)
UserName          varchar(100)    NULL
ErrorNumber       int             NULL
ErrorState        int             NULL
ErrorSeverity     int             NULL
ErrorLine         int             NULL
ErrorProcedure    varchar(max)    NULL
ErrorMessage      varchar(max)    NULL
ErrorDateTime     datetime        NULL
```
> Bảng bẫy lỗi chung — nhiều SP ghi lỗi vào đây trong khối `CATCH` (`SUSER_SNAME()`,
> `ERROR_NUMBER()`, `ERROR_STATE()`, `ERROR_SEVERITY()`, `ERROR_LINE()`, `ERROR_PROCEDURE()`,
> `ERROR_MESSAGE()`).

### JobRunning
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
TransType         varchar(50)     NULL
PageNumber        varchar(50)     NULL
IsFinished        bit             NULL       -- DEFAULT 0
StartTime         datetime        NULL
EndTime           datetime        NULL
```

### LogSendEmail
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
PKey              varchar(1000)   NULL
DateTime          datetime        NULL       -- DEFAULT getdate()
DataType          varchar(50)     NULL
NumberOfSend      int             NULL
```

### POSError
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
Type              varchar(50)     NULL
StoreNo           varchar(20)     NULL
PosTerminal       varchar(20)     NULL
FileName          varchar(50)     NULL
MsgError          nvarchar(3000)  NULL
CreatedDate       datetime        NULL
LastedUpdate      datetime        NULL       -- DEFAULT getdate()
```

### UploadSaleByTableLog
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
OrderNo           varchar(50)     NULL
SQLInsert         nvarchar(max)   NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```

### UploadSaleLog
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
OrderNo           varchar(50)     NULL
FileName          varchar(1000)   NULL
TableName         varchar(500)    NULL
IsError           int             NULL
MsgError          nvarchar(1000)  NULL
CreatedDate       datetime        NULL       -- DEFAULT getdate()
IpServer          varchar(30)     NULL
ProcessID         varchar(50)     NULL
```

---

## Survey

### SurveyResult
PK: `Id`
```
Id                int             NOT NULL   -- IDENTITY(1,1)
AnswerCode        varchar(50)     NOT NULL
QuestionCode      varchar(50)     NOT NULL
PhoneNumber       nvarchar(20)    NOT NULL
OrderNo           nvarchar(20)    NULL
Type              varchar(20)     NULL
CreatedDate       datetime2(7)    NULL
```

---

## Partner Tracking

### TrackingPartner
PK: `Id`
```
Id                bigint          NOT NULL   -- IDENTITY(1,1)
OrderNo           nvarchar(20)    NOT NULL
StoreNo           nvarchar(10)    NOT NULL
POSNo             nvarchar(10)    NOT NULL
PartnerCode       varchar(20)     NOT NULL
FunctionCode      varchar(20)     NOT NULL
OrderData         nvarchar(max)   NULL
PartnerData       nvarchar(max)   NULL
Description       nvarchar(500)   NULL
Amount            float           NULL
ReferenceNo       nvarchar(50)    NULL
Ref1              nvarchar(max)   NULL
Ref2              nvarchar(max)   NULL
Ref3              nvarchar(max)   NULL
Ref4              nvarchar(max)   NULL
Ref5              nvarchar(max)   NULL
CreatedDate       datetime        NULL
CreatedBy         varchar(50)     NULL
```

### INB_VoucherToSAP
PK: (none)
```
Voucher_Type          varchar(5)      NULL
SerialNo              varchar(20)     NULL
Voucher_Value         float           NULL
Voucher_Currency      varchar(10)     NULL
Validity_From_Date    varchar(20)     NULL
Expiry_Date           varchar(20)     NULL
Processing_Type       varchar(5)      NULL
Status                varchar(5)      NULL
Site                  varchar(5)      NULL
Article_No            varchar(20)     NULL
Bonus_Buy             varchar(20)     NULL
POSNo                 varchar(5)      NULL
ReceiptNo             varchar(30)     NULL
TranDate              varchar(20)     NULL
TranTime              varchar(20)     NULL
FileName              varchar(500)    NULL
```
> Bảng trung gian gửi voucher sang SAP (interface outbound) — không có PK, không thấy SP nào
> trong script này thao tác trực tiếp lên bảng (có thể ghi bởi tiến trình/job khác ngoài script).

---

## Khác

### ItemBlockVAT
PK: `ID`
```
ID                int             NOT NULL   -- IDENTITY(1,1)
ItemNo            varchar(20)     NULL
Counter           bigint          NULL
Pkey              varchar(20)     NULL
Status            bit             NULL       -- DEFAULT 1
CretedDate        datetime        NULL       -- DEFAULT getdate()
```

### POSDocumentNos
PK: (none)
```
timestamp         timestamp       NOT NULL
StoreNo           nvarchar(10)    NOT NULL
POSTerminal       nvarchar(10)    NOT NULL
TransDate         date            NOT NULL
LastNumber        nvarchar(20)    NOT NULL
LastDateTime      datetime        NOT NULL
Counter           bigint          NULL
DocumentType      varchar(50)     NULL
LastUpdated       datetime        NULL       -- DEFAULT getdate()
```

### StoreExcludeInvoice
PK: `Id`
```
Id                int             NOT NULL   -- IDENTITY(1,1)
StoreNo           varchar(50)     NULL
Status            bit             NULL       -- DEFAULT 1
CreatedDate       datetime        NULL       -- DEFAULT getdate()
```
> Danh sách store bị loại trừ khỏi xuất hoá đơn (theo tên bảng — ý nghĩa cụ thể của `Status`
> chưa xác nhận được từ script, không suy đoán thêm).

---

## Stored Procedures

| SP | Bảng đọc/ghi (best-effort) | Mục đích (suy ra từ tên + skim body) |
|---|---|---|
| `API_POS_CHECK_SHIFT_HEADER` | đọc `POSShiftHeader` | Tra ca gần nhất (`ShiftCode`, `IsShiftClosed`) theo `StoreNo`+`PosTerminal`+`BusinessDate`. |
| `API_SALE_INFO_ORDERNO` | đọc `TransHeader`, `TransLine`, `TransPaymentEntry`, `TransDiscountEntry`, `TransInfocodeEntry`, `TransBonus`, `TransDiscountCouponEntry`, `TransCpnVchIssue`, `TransBluePoint`, `TransInputData`, `TransPaymentInfo` | Trả toàn bộ dữ liệu chi tiết 1 đơn hàng (`OrderNo`) — 11 result set, mỗi bảng liên quan 1 set, dùng để tra cứu/soi đơn hàng. |
| `GetSalesEODConfirm` | đọc `RPOSMasterData..POSTerminal`, `POSEOD`, `TransHeader`, `TransPaymentEntry` | Tổng hợp trạng thái EOD theo từng POS terminal trong ngày (đã đóng chưa, doanh thu, tiền mặt/ngân hàng, số khách) để đối chiếu chốt ca. |
| `GetTransCpnVchIssueList` | đọc `TransCpnVchIssue` | Danh sách voucher/coupon đã phát hành, có phân trang (`@PageSize`/`@PageNumber`), lọc theo ngày/site/loại/trạng thái. |
| `GetTransCpnVchIssueListExport` | đọc `TransCpnVchIssue` | Bản export (không phân trang) của danh sách voucher/coupon đã phát hành, cùng bộ lọc với SP trên. |
| `Register_Insert` | đọc/ghi `POSShiftHeader`; ghi `Interface_Errors` khi lỗi | Gộp các `ShiftCode` bị ghi trùng (do có cả dòng mở ca + đóng ca riêng) thành 1 dòng, xoá dòng dư thừa. |
| `Register_Insert_ALL` | đọc/xoá `POSBussinessDate`, `POSFinishSale`, `POSShiftHeader`, `POSShiftLine`; ghi `CentralSales..BussinessDate`/`FinishSale`/`ShiftHeader`/`ShiftLine`, `POSBussinessDate_Log`, `POSFinishSale_Log`, `POSShiftHeader_Log`, `POSShiftLine_Log`; ghi `Interface_Errors` khi lỗi | Job dọn dẹp/archive định kỳ: dedupe rồi chuyển dữ liệu ngày kinh doanh/ca đã xử lý sang bảng lưu trữ, xoá khỏi bảng nguồn. |
| `Rpt_GetRevenueSalesLists` | đọc `TransHeader`, `TransLine` | Báo cáo doanh thu chi tiết theo dòng bán hàng, phân trang, nhiều bộ lọc (loại đơn, VAT, đối tác, từ khoá). |
| `Rpt_GetSalesSKUDailyList` | đọc `ReportSaleDetail` | Báo cáo doanh số theo SKU/ngày, lọc theo store/loại đơn/sản phẩm/barcode (dùng `STRING_SPLIT` cho lọc nhiều giá trị), có chế độ export. |
| `Rpt_ReportSaleDetail_Insert` | đọc `TransLine`, `TransHeader`, `TransInputData`, `RPOSMasterData..StoreSetup`(?); ghi `ReportSaleDetail` | Job ETL nạp dữ liệu chi tiết bán hàng từ `TransHeader`/`TransLine` vào bảng báo cáo phẳng `ReportSaleDetail` theo khoảng ngày, chỉ nạp đơn hàng chưa có trong báo cáo. |
| `Rpt_ReportSalesByStaff` | đọc `RPOSMasterData..Staff`, `TransHeader`, `TransLine` | Báo cáo doanh thu + số hoá đơn theo nhân viên thu ngân (`CashierID`) trong khoảng ngày. |
| `Rpt_ReportSalesByStore` | đọc `TransHeader`, `TransLine` (join danh sách store truyền vào qua JSON) | Báo cáo doanh thu theo cửa hàng + ngày, có phân trang và tính tổng toàn bộ (window function). |
| `Rpt_ReportTopProduct` | đọc `ReportSaleDetail` | Top N sản phẩm bán chạy theo doanh thu hoặc số lượng, trong khoảng ngày/store/ngành hàng. |
| `Rpt_SalesByCategory` | đọc `RPOSMasterData..Store`, `RPOSMasterData..Item`, `RPOSMasterData..MCH`, `TransLine`, `TransHeader` | Báo cáo doanh thu theo ngành hàng (MCH) + cửa hàng + ngày, trừ giao dịch trả hàng. |
| `Sale_InsertDataByOrder_KAFKA` | đọc `TransHeader`, `TransVoidLine`; gọi `Sale_InsertToTableByJsonV2` | Entry point nhận message Kafka (JSON), kiểm tra trùng theo `@Type` (`SALE`/`VOID`/`ORIGSALE`), rồi lặp qua từng bảng trong JSON để insert qua `Sale_InsertToTableByJsonV2`. |
| `Sale_InsertToTableByJsonV2` | ghi động vào bảng tên `@table_name` (bất kỳ bảng nào trong `RPOSCentralSales` khớp JSON) | Insert generic từ JSON vào 1 bảng bất kỳ — tự sinh danh sách cột + kiểu dữ liệu từ `sys.columns`/`OPENJSON`, build câu SQL động rồi `EXEC`. Bỏ qua riêng bảng `TransBonus` (cố ý, từ 2024-10-17). |
| `usp_BusinessDay_ConfirmEndDate` | đọc/ghi `BusinessDayConfirm`; ghi `BussinessDateOpen` | Xác nhận chốt ngày kinh doanh của 1 store: chặn xác nhận trùng, đẩy `BussinessDateOpen` sang ngày kế tiếp, ghi 1 dòng vào `BusinessDayConfirm`. Dùng `XACT_ABORT`+`TRY/CATCH`+`THROW`. |

---

## Cập nhật tài liệu này

Khi `docs/sql/database/CentralSale.sql` (hoặc script DB khác) thay đổi (thêm/sửa bảng, cột, SP):

1. Đọc lại phần script bị thay đổi.
2. Cập nhật đúng mục tương ứng trong file này (tên bảng/cột/kiểu dữ liệu/PK — **không chép
   nguyên `CREATE TABLE`**, chỉ cần cột + kiểu + nullable + ghi chú PK/FK).
3. Nếu thêm bảng mới hoàn toàn → thêm 1 mục ở đúng nhóm domain trong Mục lục + phần chi tiết.
4. Nếu là SP mới → thêm vào bảng "Stored Procedures" theo cùng khuôn (tên + bảng liên quan + mục đích).

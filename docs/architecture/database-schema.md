# Database Schema — RPOSMasterData (CentralMD)

> **Nguồn**: `docs/sql/database/CentralMD.sql` (script tạo DB `RPOSMasterData`, generated 7/2/2026).
> **Mục đích tài liệu này**: bản đồ tra cứu **tên bảng / tên cột / kiểu dữ liệu / PK** để viết
> query/SP/Dapper mapping chính xác tuyệt đối — **KHÔNG suy đoán tên cột**.
>
> ## ⚠️ QUY TẮC BẮT BUỘC
> Trước khi viết bất kỳ SQL query, stored procedure, hoặc Repository method nào đụng tới các
> bảng trong `RPOSMasterData`/CentralMD, **BẮT BUỘC đối chiếu file này trước** để lấy đúng tên
> bảng, tên cột, kiểu dữ liệu, độ dài. Sai tên cột / kiểu dữ liệu sẽ gây lỗi runtime hoặc sai dữ
> liệu cho 5.000 máy POS. Nếu bảng cần dùng chưa có trong tài liệu này → đọc lại
> `docs/sql/database/CentralMD.sql` (hoặc script cập nhật mới nhất), sau đó bổ sung vào file này
> trong cùng commit.
>
> Database khác (`RPOSSale`, `RPOSLoyalty`...) có script riêng — không nằm trong file này. Khi
> có script cho các DB đó, tạo file tương ứng (`sale-schema.md`, `loyalty-schema.md`...) theo cùng
> khuôn mẫu và thêm mục lục ở đây.

## Quy ước chung trong DB này

- **Không có FK thực sự giữa hầu hết các bảng** — chỉ có 1 FK duy nhất:
  `SysWebApiRoute.AppCode` → `SysWebApi.AppCode`. Quan hệ giữa các bảng khác (`Store.No`,
  `Item.No`...) là **liên kết ngầm theo convention**, không được enforce ở DB.
- **Pattern `Counter` + `Pkey`**: rất nhiều bảng dùng cặp `Counter bigint` (tăng dần, dùng để
  đồng bộ POS lấy delta — xem `SyncTableList`/`SyncGetDataByTable`) + `Pkey varchar(50)` (khoá
  logic dùng ở tầng ứng dụng) **thay cho PRIMARY KEY thật**. Các bảng loại này được đánh dấu
  `PK: (none — dùng Counter/Pkey)` bên dưới.
- **`NOLOCK`/`(NOLOCK)`** dùng phổ biến trong toàn bộ stored procedure của DB này (đọc dữ liệu
  không khoá) — theo đúng pattern legacy đã có.
- Các bảng có PK/UNIQUE/FK thật được ghi rõ ngay dưới tên bảng.
- Kiểu `[nvarchar](max)`, `[varchar](max)` ghi là `nvarchar(max)` / `varchar(max)`.

## Mục lục theo domain

| Domain | Bảng |
|---|---|
| [Store & Company](#store--company) | Store, StoreGroup, StorePriceGroup, StoreSetup, StoreTemp, Branch, CompanyInformation, RetailSetup, SetupGroupSite |
| [POS Terminal & Device](#pos-terminal--device) | POSTerminal, POSTerminalBank, POSTerminalSetup, PosterminalMapping, POSInfo, POSMonitor, POSVersion, POSVATCode, SmartPOS, POSDataSetup |
| [POS Hotkey / Group](#pos-hotkey--group) | PosGroup, PosGroupItem, POSHotKeyGroup, POSHotKeyItem, POSHotKeyItemGroup |
| [Item / Product](#item--product) | Item, Barcodes, BarcodeSetup, ItemBlock, ItemBlockVAT, ItemExtra, ItemMaxSalesQty, ItemOption, ItemPaymentBlocked, ItemPointsMember, ItemUnitOfMeasure, LinkedItem, ItemEarnScale |
| [Sales Price](#sales-price) | SalesPrice, SalesPriceRange, SalesPriceRate, SalesOrderType, SalesOrderTypeByStore |
| [Promotion / Offer](#promotion--offer) | OfferHeader, OfferBuy, OfferGet, OfferBenefits, OfferMaxQuantity, OfferPriority, OfferRetrict, OfferSite, OfferType, SetupPromotionBUY, SetupPromotionGET, SetupPromotionHEADER, SetupPromotionSITE, ItemDiscount, ItemDiscountMember, MMLItemDiscount, MMLSchemeHeader, MMLSchemeItem, MMLSchemeResponse, SpecialComboHeader, SpecialComboLine, SpecialComboStore |
| [Bank & Payment](#bank--payment) | Bank, BankCardType, BankDiscount, BankDiscountItem, BankDiscountStore, TenderType, TenderTypeConfig, TenderTypeImage, TenderTypeSetup, PaymentMethodQRCode, MappingQRCode |
| [Voucher / Coupon](#voucher--coupon) | CpnVchBOMHeader, CpnVchBOMCodeIssue (dùng chung Coupon+SAP Voucher, xem cột `Source`), CpnVchBOMIssueRule, CpnVchBOMLine, CpnVchBOMQuota, CpnVchBOMStore, CpnVchCodeSend, Internal_Voucher (⚠️ LEGACY — xem CpnVchBOMCodeIssue), RewardCode, RewardCodeSend, RewardHeader, WinCodeHeader, WinCodeStore, WinCodeCustomer |
| [Loyalty / Offline Percent](#loyalty--offline-percent) | CXOfflinePercent, VinIDOfflinePercent, LoyaltyRate |
| [Staff & User](#staff--user) | Staff, User |
| [Weight Scale](#weight-scale) | WeightScale_AssortmentItem, WeightScale_AssortmentSite, WeightScale_INGREDIENT, WeightScale_Item_Change, WeightScale_Log, WeightScale_Multimex, WeightScale_PLU_LIST, WeightScale_PLU_PRICE_STORE, WeightScale_Processing |
| [Sync / Master Data Distribution](#sync--master-data-distribution) | SyncTableList, SyncTableFromPOS, MasterDataDownloadLog |
| [SysWebApi](#syswebapi) | SysWebApi, SysWebApiConfig, SysWebApiRoute, SysWebApiUser |
| [Survey](#survey) | SurveyQuestion, SurveyAnswer |
| [Dashboard / Web Admin (POS.Web)](#dashboard--web-admin-posweb) | DashboardUsers, DashboardAuditLog, SqlConsoleAuditLog |
| [Notify / Marketing](#notify--marketing) | Notify, OptionData |
| [Reason / Source](#reason--source) | ReasonCode, SourceBill |
| [Khác](#khác) | Interface_Errors, UnitOfMeasure |
| [Stored Procedures](#stored-procedures) | 35 SP hiện có trong script |

---

## Store & Company

### Store
PK: `No`
```
No                        nvarchar(10)      NOT NULL
IssueVoucherOnline        bit               NULL
ResponsibilityCenter      nvarchar(10)      NULL
Name                      nvarchar(150)     NOT NULL
Address                   nvarchar(250)     NULL
City                      nvarchar(250)     NULL
PostCode                  nvarchar(20)      NULL
StoreManagerID            nvarchar(20)      NULL
StoreOpenfrom             datetime          NULL
StoreOpento               datetime          NULL
PhoneNo                   nvarchar(50)      NULL
CountryCode               nvarchar(10)      NULL
LocationCode              nvarchar(10)      NULL
CurrencyCode              nvarchar(10)      NULL
StoreOpenAfterMidnight     tinyint          NULL
LastDateModified          datetime          NULL
FunctionalityProfile      nvarchar(10)      NULL
MenuProfile               nvarchar(10)      NULL
InterfaceProfile          nvarchar(10)      NULL
StyleProfile              nvarchar(20)      NULL
HardwareProfile           nvarchar(10)      NULL
StatementNos              nvarchar(10)      NULL
OneStatementPerDay        tinyint           NULL
StatementMethod           int               NULL
ClosingMethod             int               NULL     -- 0 = đang mở cửa, 1 = đã đóng cửa
RoundingAccount           nvarchar(20)      NULL
TotalDiscountTender       nvarchar(10)      NULL
PrintReceiptLogo          nvarchar(80)      NULL
PrintReceiptBitmapNo      int               NULL
ItemNoOnReceipt           int               NULL
County                    nvarchar(30)      NULL
EmailAddress              nvarchar(30)      NULL
TaxCode                   nvarchar(20)      NULL
BusinessAreaNo            nvarchar(20)      NULL
InfocodeReturn            nvarchar(20)      NULL
TenderTypeReturn          nvarchar(20)      NULL
BranchNo                  nvarchar(20)      NULL
ForEvent                  tinyint           NULL
InfocodeAdjustBill        nvarchar(20)      NULL
InfocodeAdjustLine        nvarchar(20)      NULL
CustomerDefault           nvarchar(20)      NULL
Counter                   bigint            NULL
Pkey                      varchar(50)       NULL
```
> Xem quy tắc dùng cột `ClosingMethod` (KHÔNG dùng `Blocked`) ở CLAUDE.md mục "Quy tắc DB Schema".

### StoreGroup
PK: (none — dùng Counter/Pkey)
```
Id            int             NOT NULL
StoreNo       varchar(50)     NULL
GroupCode     varchar(50)     NULL
Pkey          varchar(100)    NULL
Counter       bigint          NULL
Status        bit             NULL
CreatedDate   datetime        NULL
```

### StorePriceGroup
PK: (none). `PriceGroupCode` = giá trị `SalesCode` khi lưu giá (9.3 Setup Giá — SP list join
`SalesPrice.SalesCode = StorePriceGroup.PriceGroupCode`). Dropdown "Nhóm giá" =
`SELECT DISTINCT PriceGroupCode, PriceGroupName`.
```
Store                 nvarchar(10)   NOT NULL
PriceGroupCode        nvarchar(30)   NOT NULL
PriceGroupName        nvarchar(...)  NULL      -- tên hiển thị nhóm giá (bổ sung 2026-07)
Type                  int            NOT NULL
Priority              int            NOT NULL
ReplicationCounter    int            NOT NULL
Counter               bigint         NULL
Pkey                  varchar(50)    NULL
```

### StoreSetup
PK: (none) — key/value config theo store
```
ID            int             NOT NULL
StoreNo       varchar(50)     NULL
Code          varchar(200)    NULL
Value         nvarchar(1000)  NULL
Pkey          varchar(250)    NULL
Counter       bigint          NULL
Status        bit             NULL
CreatedDate   datetime        NULL
CreatedUser   varchar(50)     NULL
```

### StoreTemp
PK: (none) — bảng tạm 1 cột
```
StoreNo    nvarchar(50)    NOT NULL
```

### Branch
PK: `No` (`CONSTRAINT [Branch$0]`)
```
timestamp                  timestamp       NOT NULL
No                         nvarchar(20)    NOT NULL   -- PK
Description                nvarchar(225)   NOT NULL
Address                    nvarchar(225)   NOT NULL
VATRegistrationNo          nvarchar(30)    NOT NULL
PhoneNo                    nvarchar(30)    NOT NULL
FaxNo                      nvarchar(30)    NOT NULL
BankAccountNo              nvarchar(30)    NOT NULL
BankName                   nvarchar(100)   NOT NULL
BankAddress                nvarchar(225)   NOT NULL
BankAcountName             nvarchar(10)    NOT NULL
VietnameseDescription      nvarchar(225)   NOT NULL
VietnameseAddress          nvarchar(225)   NOT NULL
UrlElecInvoice             nvarchar(150)   NOT NULL
Counter                    bigint          NULL       -- default 0
Pkey                       varchar(50)     NULL        -- default ''
```

### CompanyInformation
PK: (none)
```
PrimaryKey                    nvarchar(10)    NOT NULL
Name                          nvarchar(150)   NOT NULL
Address                       nvarchar(150)   NOT NULL
City                          nvarchar(30)    NOT NULL
PhoneNo                       nvarchar(30)    NOT NULL
PhoneNo2                      nvarchar(30)    NOT NULL
TelexNo                       nvarchar(30)    NOT NULL
FaxNo                         nvarchar(30)    NOT NULL
GiroNo                        nvarchar(20)    NOT NULL
BankName                      nvarchar(50)    NOT NULL
BankBranchNo                  nvarchar(20)    NOT NULL
BankAccountNo                 nvarchar(30)    NOT NULL
PaymentRoutingNo              nvarchar(20)    NOT NULL
CustomsPermitNo               nvarchar(10)    NOT NULL
CustomsPermitDate             datetime        NOT NULL
VATRegistrationNo             nvarchar(20)    NOT NULL
RegistrationNo                nvarchar(20)    NOT NULL
TelexAnswerBack               nvarchar(20)    NOT NULL
ShipToName                    nvarchar(150)   NOT NULL
ShipToAddress                 nvarchar(150)   NOT NULL
ShipToCity                    nvarchar(30)    NOT NULL
ShipToContact                 nvarchar(70)    NOT NULL
LocationCode                  nvarchar(10)    NOT NULL
PostCode                      nvarchar(20)    NOT NULL
County                        nvarchar(30)    NOT NULL
ShipToPostCode                nvarchar(20)    NOT NULL
ShipToCounty                  nvarchar(30)    NOT NULL
EMail                         nvarchar(80)    NOT NULL
HomePage                      nvarchar(80)    NOT NULL
CountryRegionCode             nvarchar(10)    NOT NULL
ShipToCountryRegionCode       nvarchar(10)    NOT NULL
IBAN                          nvarchar(50)    NOT NULL
SWIFTCode                     nvarchar(20)    NOT NULL
IndustrialClassification      nvarchar(30)    NOT NULL
ICPartnerCode                 nvarchar(20)    NOT NULL
ICInboxType                   int             NOT NULL
ICInboxDetails                nvarchar(250)   NOT NULL
SystemIndicator                int            NOT NULL
CustomSystemIndicatorText     nvarchar(250)   NOT NULL
SystemIndicatorStyle          int             NOT NULL
AllowBlankPaymentInfo         tinyint         NOT NULL
ResponsibilityCenter          nvarchar(10)    NOT NULL
CheckAvailPeriodCalc          varchar(32)     NOT NULL
CheckAvailTimeBucket          int             NOT NULL
BaseCalendarCode              nvarchar(10)    NOT NULL
CalConvergenceTimeFrame       varchar(32)     NOT NULL
Counter                       bigint          NULL
Pkey                          varchar(50)     NULL
```

### RetailSetup
PK: (none) — bảng cấu hình global 1 dòng
```
Key                         nvarchar(10)    NOT NULL
LocalStoreNo                nvarchar(20)    NOT NULL
LSRetailInUse               tinyint         NOT NULL
EANLicenseNo                nvarchar(10)    NOT NULL
DefVATBusPostGrPrice        nvarchar(10)    NOT NULL
DefPriceIncludesVAT         tinyint         NOT NULL
SourceCode                  nvarchar(10)    NOT NULL
StoreNoNos                  nvarchar(10)    NOT NULL
ItemSalesStatisticsOn       int             NOT NULL
POSTerminalStatistics       tinyint         NOT NULL
StaffStatistics              tinyint        NOT NULL
PaymentStatistics            tinyint        NOT NULL
LastDateModified             datetime       NOT NULL
DaysBeforeTransArchive       int            NOT NULL
CommissionActive             tinyint        NOT NULL
CalculateInStatemPosting     tinyint        NOT NULL
CalculateInSalesPosting      tinyint        NOT NULL
ExcludeReturns                tinyint       NOT NULL
BalAccType                    int           NOT NULL
BalAccNo                      nvarchar(20)  NOT NULL
OriginalSalespersinReturns     tinyint      NOT NULL
Dimension1Mandatory            tinyint      NOT NULL
OnlyTwoDimensions               tinyint     NOT NULL
xNumberofRetries                 int        NOT NULL
DefaultPriceGroup                nvarchar(10) NOT NULL
PostTotalDisc                     tinyint    NOT NULL
PostInfocodeDisc                  tinyint    NOT NULL
PostLineDisc                       tinyint   NOT NULL
PostPeriodicDisc                    tinyint  NOT NULL
PostCustDisc                         tinyint NOT NULL
PostCouponDisc                       tinyint  NOT NULL
PostLineDiscOffer                    tinyint NOT NULL
PostTotalDiscOffer                   tinyint NOT NULL
PostTenderTypeDisc                   tinyint NOT NULL
ItemPostingDate                      int     NOT NULL
DefaultCustomerPosting               int     NOT NULL
UpdateCostAmount                     tinyint NOT NULL
ItemLabelsForNegStock                tinyint NOT NULL
ItemLabelsOnPriceChange              tinyint NOT NULL
PostAlwaysReserveItems                tinyint NOT NULL
DeletePrintedLabels                   tinyint NOT NULL
POItemLookupMethod                    int    NOT NULL
Difference                            decimal(38,20) NOT NULL
AutocreateBarcodes                    int    NOT NULL
CreateItemsNoSeries                   nvarchar(10) NOT NULL
DefaultItemHierarchy                  nvarchar(20) NOT NULL
DistributionLocation                  nvarchar(20) NOT NULL
DefStoreHierarchy                     nvarchar(20) NOT NULL
AllowRename                           tinyint NOT NULL
Counter                               bigint  NULL
Pkey                                  varchar(50) NULL
```

### SetupGroupSite
PK: (none)
```
ID               int             NOT NULL
GroupCode        varchar(50)     NOT NULL
GroupName        nvarchar(250)   NULL
ListStore        varchar(max)    NULL     -- danh sách StoreNo, phân tách (dùng để expand ra SetupPromotionSITE)
CreatedDate      datetime        NULL
CreatedUser      varchar(50)     NULL
LastUpdateUser   varchar(50)     NULL
LastUpdateDate   datetime        NULL
Status           bit             NULL
```

### SetupGroupItem
PK: `ID` identity (`PK_BuyGroupItem` — tên constraint tàn tích từ thời đặt tên trước khi đổi tên
bảng, giữ nguyên không đổi). Bảng song sinh với `SetupGroupSite` — nhóm sản phẩm cụ thể dùng cho
dòng Buy/Get "Theo nhóm" (`SetupPromotionBUY/GET.BonusBuyNo` = `GroupCode`).
```
ID               int             NOT NULL IDENTITY
GroupCode        varchar(50)     NULL
GroupName        nvarchar(250)   NULL
ListItemNo       varchar(max)    NULL     -- JSON array List<string> ItemNo — KHÔNG lưu UOM (hạn chế legacy giữ nguyên)
CreatedUser      varchar(50)     NULL
CreatedDate      datetime        NULL
LastUpdateUser   varchar(50)     NULL
LastUpdateDate   datetime        NULL
Status           bit             NULL
```

---

## POS Terminal & Device

### POSTerminal
PK: `No` (`PK_POSTerminal`) — UNIQUE: `IPAddress` (`IDX_POSTerminal_No`)
```
MACAddress                nvarchar(20)    NULL
IPAddress                 varchar(20)     NOT NULL   -- UNIQUE
No                        nvarchar(10)    NOT NULL   -- PK
StoreNo                   nvarchar(10)    NOT NULL
TerminalType              int             NULL
Description               nvarchar(100)   NULL
Placement                 nvarchar(30)    NULL
StatementMethod           int             NULL
TerminalStatement         tinyint         NULL
DefaultPriceGroup         nvarchar(10)    NULL
TerminalNetworkID         nvarchar(20)    NULL
TerminalIPAddress         nvarchar(15)    NULL
TerminalConnection        int             NULL
ShowItemPictures          tinyint         NULL
ShowItemHtml              tinyint         NULL
DisplayTerminalClosed     tinyint         NULL
DisplayLinkedItem         tinyint         NULL
AutoLogoffAfter_Min       int             NULL
ReturnInTransaction       tinyint         NULL
ItemNoOnReceipt           int             NULL
LastDateModified          datetime        NULL
PrintReceiptLogo          nvarchar(80)    NULL
PrintReceiptBitmapNo      int             NULL
RcptTextMaxLength         int             NULL
ReceiptBarcode            tinyint         NULL
ReceiptSetupLocation      int             NULL
DisplayTextMaxLength      int             NULL
CustomerDisplayText1      nvarchar(40)    NULL
CustomerDisplayText2      nvarchar(40)    NULL
PrintReceiptBCType        int             NULL
ReceiptBarcodeWidth       int             NULL
ReceiptBarcodeHeight      int             NULL
DefaultSalesType          nvarchar(20)    NULL
StaffLoginValidation      int             NULL
HardwareProfile           nvarchar(10)    NULL
MenuProfile               nvarchar(10)    NULL
InterfaceProfile          nvarchar(10)    NULL
FunctionalityProfile      nvarchar(10)    NULL
StyleProfile              nvarchar(20)    NULL
PrintNumberOfItems        tinyint         NULL
PrintSecondReceipt        tinyint         NULL
SalesTypeFilter           nvarchar(250)   NULL
DualDisHost               nvarchar(30)    NULL
BillNoseri                nvarchar(50)    NULL
Counter                   bigint          NULL
Pkey                      varchar(50)     NULL
CreatedDate               datetime        NULL
CreatedBy                 varchar(50)     NULL
UpdatedDate               datetime        NULL
UpdatedBy                 varchar(50)     NULL
Status                    bit             NULL       -- default 1
```

### POSTerminalBank
PK: (none)
```
BankPOSCode      nvarchar(20)    NOT NULL
BankPOSName      nvarchar(100)   NULL
BankCode         nvarchar(20)    NOT NULL
StoreNoFull      nvarchar(20)    NULL
StoreNo          nvarchar(10)    NOT NULL
POSNo            nvarchar(10)    NOT NULL
AccessKey        varchar(300)    NULL
IsOnline         bit             NULL
Status           bit             NULL
Counter          bigint          NULL
Pkey             varchar(50)     NULL
CreatedDate      datetime        NULL
CreatedUser      nvarchar(50)    NULL
UpdatedDate      datetime        NULL
UpdatedUser      nvarchar(50)    NULL
POSTerminal      varchar(20)     NULL
```

### POSTerminalSetup
PK: (none) — key/value config theo POS
```
ID            int             NOT NULL
StoreNo       varchar(50)     NULL
POSNo         varchar(50)     NULL
Code          varchar(200)    NULL
Value         nvarchar(1000)  NULL
Pkey          varchar(250)    NULL
Counter       bigint          NULL
Status        bit             NULL
CreatedDate   datetime        NULL
CreatedUser   varchar(50)     NULL
```

### PosterminalMapping
PK: (none) — mapping số hoá đơn theo chi nhánh/mã số thuế
```
ID                int             NOT NULL
BranchNo          varchar(10)     NULL
TaxCode           varchar(20)     NULL
CompTaxCode       varchar(30)     NULL
NumberMapping     varchar(20)     NULL
StoreNo           varchar(10)     NULL
PosterminalNo     varchar(20)     NULL
BillTaxCode       varchar(50)     NULL
IsUsed            bit             NULL
CreatedDate       datetime        NULL
CreatedUser       nvarchar(200)   NULL
UpdatedDate       datetime        NULL
UpdatedUser       nvarchar(200)   NULL
```

### POSInfo
PK: (none)
```
IPAddress     varchar(20)    NOT NULL
POSID         varchar(20)    NOT NULL
StoreNo       varchar(20)    NOT NULL
Counter       int            NULL
PKey          varchar(50)    NULL
CreatedDate   datetime       NULL
```

### POSMonitor
PK: `ID` (identity, `PK_POSMonitor`)
```
ID                        int identity(1,1)  NOT NULL
StoreNo                   varchar(10)        NULL
IpAddress                 varchar(50)        NULL
ComputerName              nvarchar(200)      NULL
PosTerminalID             varchar(10)        NULL
BluePosVersion            varchar(10)        NULL
BluePosVersionUpdate      datetime           NULL
BluePosDatabaseStatus     smallint           NULL
JobVersion                varchar(10)        NULL
ScriptVersion             varchar(10)        NULL
IsOpenBluePos             bit                NULL
DateTimePos               datetime           NULL
FirstTimeEvent            datetime           NULL
LastTimeEvent             datetime           NULL
IsMonitor                 bit                NULL     -- default 1
IntervalJob               int                NULL     -- default 5
LastTimeInsertAll         datetime           NULL
LastTimeInsertChange      datetime           NULL
CreateDate                datetime           NULL     -- default getdate()
```

### POSVersion
PK: `Source` (`PK_POSVersion`)
```
LastVersion    varchar(10)    NOT NULL
CurVersion     varchar(10)    NULL
UpdateTime     datetime       NULL
Counter        bigint         NULL
Source         varchar(50)    NOT NULL   -- PK
Pkey           varchar(50)    NULL
IsUpdate       bit            NULL
Folder         varchar(50)    NULL
```

### POSVATCode
PK: (none)
```
VATCode       nvarchar(10)     NOT NULL
Description   nvarchar(30)     NOT NULL
VATPercent    decimal(38,20)   NOT NULL
FiscalID      int              NOT NULL
Counter       bigint           NULL
Pkey          varchar(50)      NULL
Status        bit              NULL
```

### SmartPOS
PK: (none) — nội dung hiển thị/thông báo trên SmartPOS
```
ID              int             NOT NULL
Code            varchar(50)     NULL
Title           nvarchar(250)   NULL
Description     nvarchar(500)   NULL
Note            nvarchar(500)   NULL
Content         nvarchar(max)   NULL
ContentType     varchar(50)     NULL
Target          varchar(100)    NULL
POSType         varchar(20)     NULL
CusType         varchar(20)     NULL
Pkey            varchar(250)    NULL
Counter         bigint          NULL
Status          bit             NULL
CreatedDate     datetime        NULL
CreatedUser     varchar(50)     NULL
```

### POSDataSetup
PK: `Code` (`PK_POSDataSetup`)
```
Code          nvarchar(50)    NOT NULL   -- PK
Value         nvarchar(max)   NULL
Description   nvarchar(100)   NOT NULL
StoreNo       nvarchar(10)    NOT NULL
Counter       bigint          NULL       -- default 0
Pkey          varchar(50)     NULL       -- default ''
```
> Reference implementation CRUD + audit log: `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor`.

---

## POS Hotkey / Group

### PosGroup
PK: (none)
```
Id                int             NOT NULL
GroupCode         varchar(20)     NULL
Description       nvarchar(300)   NULL
Level             int             NULL
ParentCode        varchar(20)     NULL
Seq               int             NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
Status            smallint        NULL
LastDateModified  datetime        NULL
```

### PosGroupItem
PK: (none)
```
Id                int             NOT NULL
GroupCode         varchar(20)     NULL
ItemNo            varchar(20)     NULL
UnitOfMeasure     nvarchar(10)    NULL
Seq               int             NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
Status            smallint        NULL
LastDateModified  datetime        NULL
```

### POSHotKeyGroup
PK: (none)
```
timestamp     timestamp       NOT NULL
StoreGroup    nvarchar(10)    NOT NULL
Code          nvarchar(20)    NOT NULL
Description   nvarchar(250)   NULL
Seq           int             NULL
Counter       bigint          NULL
Pkey          varchar(50)     NULL
Status        bit             NULL
```

### POSHotKeyItem
PK: (none)
```
StoreGroup    nvarchar(10)    NOT NULL
Barcode       nvarchar(20)    NOT NULL
ItemNo        nvarchar(20)    NOT NULL
Description   nvarchar(150)   NOT NULL
Seq           int             NULL
Counter       bigint          NULL
Pkey          varchar(50)     NULL
Status        bit             NULL
```

### POSHotKeyItemGroup
PK: (none)
```
ID            bigint          NOT NULL
StoreGroup    nvarchar(10)    NULL
GroupCode     nvarchar(20)    NULL
Barcode       nvarchar(20)    NULL
Counter       bigint          NULL
Pkey          varchar(50)     NULL
Status        bit             NULL
```

---

## Item / Product

### Item
PK: `No` (`CONSTRAINT [Item$0]`)
```
No                          nvarchar(20)     NOT NULL   -- PK
No2                         nvarchar(20)     NOT NULL   -- Mã SP Phúc Long (dùng trong GetProductList)
Description                 nvarchar(500)    NOT NULL
SearchDescription           nvarchar(500)    NOT NULL
LongDescription             nvarchar(500)    NOT NULL
BaseUnitOfMeasure           nvarchar(10)     NOT NULL
StatisticsGroup             int              NOT NULL
CommissionGroup             int              NOT NULL
UnitPrice                   decimal(38,20)   NOT NULL
CostingMethod                int             NOT NULL
UnitCost                    decimal(38,20)   NOT NULL
StandardCost                decimal(38,20)   NOT NULL
VendorNo                    nvarchar(20)     NOT NULL
VendorItemNo                nvarchar(20)     NOT NULL
MaximumInventory             decimal(38,20)  NOT NULL
ReorderQuantity              decimal(38,20)  NOT NULL
GrossWeight                  decimal(38,20)  NOT NULL
NetWeight                    decimal(38,20)  NOT NULL
UnitsPerParcel                decimal(38,20) NOT NULL
UnitVolume                    decimal(38,20) NOT NULL
Blocked                       tinyint        NOT NULL
LastDateModified               datetime      NOT NULL
PriceIncludesVAT                tinyint      NOT NULL
TaxGroupCode                    nvarchar(10) NOT NULL   -- 2=0%,3=5%,4=10%,5=8%,6=Non Tax
PreventNegativeInventory        int          NOT NULL
MinimumOrderQuantity             decimal(38,20) NOT NULL
MaximumOrderQuantity             decimal(38,20) NOT NULL
SafetyStockQuantity              decimal(38,20) NOT NULL
OrderMultiple                    decimal(38,20) NOT NULL
SalesUnitOfMeasure                nvarchar(10) NOT NULL
ManufacturerCode                  nvarchar(10) NOT NULL
ItemCategoryCode                  nvarchar(10) NOT NULL
ProductGroupCode                  nvarchar(10) NOT NULL
ServiceItemGroup                  nvarchar(10) NOT NULL
ItemTrackingCode                  nvarchar(10) NOT NULL
ProductionBOMNo                   nvarchar(20) NOT NULL
BlockedVINID                      tinyint      NOT NULL
CommonItemNo                      nvarchar(20) NOT NULL
ItemFamilyCode                    nvarchar(20) NOT NULL
DivisionCode                      nvarchar(20) NOT NULL
KeyinginPrice                     tinyint      NOT NULL
ZeroPriceValid                     tinyint     NOT NULL
Counter                            bigint      NULL      -- default 0
Pkey                                varchar(50) NULL     -- default ''
ParentCode                          varchar(20) NULL
Size                                 varchar(20) NULL
ImageName                            varchar(200) NULL
IsVAT                                 bit        NULL
```

### Barcodes
PK: (none) — khoá logic `BarcodeNo` + `ItemNo`
```
BarcodeNo             nvarchar(50)     NOT NULL
ItemNo                nvarchar(20)     NOT NULL
ShowForItem           tinyint          NOT NULL
Description           nvarchar(100)    NOT NULL
Blocked               tinyint          NOT NULL
LastDateModified      datetime         NULL
VariantCode           nvarchar(10)     NOT NULL
UnitOfMeasureCode     nvarchar(10)     NOT NULL
DiscountPercent       decimal(38,20)   NOT NULL
Counter               bigint           NULL
Pkey                  varchar(50)      NULL
```

### BarcodeSetup
PK: (none)
```
Id                int              NOT NULL
GroupCode         varchar(50)      NULL
Barcode           varchar(50)      NULL
PolicyLen         int              NULL
Pkey              varchar(100)     NULL
Counter           bigint           NULL
Status            bit              NULL
CreatedDate       datetime         NULL
IsRequire         bit              NULL
IsFixedQuantity   bit              NULL
Ref1..Ref5        nvarchar(500)    NULL   -- 5 cột Ref1, Ref2, Ref3, Ref4, Ref5
```

### ItemBlock
PK: (none)
```
Id                int             NOT NULL
ItemNo            varchar(20)     NULL
UnitOfMeasure     nvarchar(10)    NULL
StoreNo           varchar(20)     NULL
Status            bit             NULL
UpdatedDate       datetime        NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
```

### ItemBlockVAT
PK: (none)
```
ID            int             NOT NULL
ItemNo        varchar(20)     NULL
Counter       bigint          NULL
Pkey          varchar(20)     NULL
Status        bit             NULL
CretedDate    datetime        NULL
```

### ItemExtra
PK: (none) — item bổ trợ (topping/tùy chọn theo SaleType)
```
Id                    int             NOT NULL
ItemNo                varchar(20)     NULL
UnitOfMeasure         nvarchar(10)    NULL
ItemNoRef             varchar(20)     NULL
UnitOfMeasureRef      nvarchar(50)    NULL
Description           nvarchar(300)   NULL
SaleTypeCode          varchar(20)     NULL
Type                  varchar(50)     NULL
Counter               bigint          NULL
Pkey                  varchar(100)    NULL
Status                smallint        NULL
LastDateModified      datetime        NULL
IsDefault             bit             NULL
Seq                   int             NULL
```

### ItemMaxSalesQty
PK: (none)
```
Barcode        nvarchar(20)     NOT NULL
StoreNo        nvarchar(20)     NOT NULL
MaxQuantity    decimal(38,20)   NOT NULL
FromDate       datetime         NULL
ToDate         datetime         NULL
Counter        bigint           NULL
Pkey           varchar(50)      NULL
```

### ItemOption
PK: (none)
```
Id                int             NOT NULL
ItemNo            varchar(20)     NULL
ItemNoOption      varchar(20)     NULL
OptionType        varchar(30)     NULL
OptionName        nvarchar(50)    NULL
Quantity          float           NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
Status            smallint        NULL
LastDateModified  datetime        NULL
IsDefault         bit             NULL
Seq               int             NULL
```

### ItemPaymentBlocked
PK: (none) — chặn hình thức thanh toán theo item/khoảng thời gian
```
ID              bigint          NOT NULL
ItemNo          nvarchar(20)    NULL
BarcodeNo       nvarchar(50)    NULL
UOM             nvarchar(10)    NULL
FromDate        datetime        NULL
ToDate          datetime        NULL
PaymentMethod   nvarchar(50)    NULL
Status          bit             NULL
Counter         int             NULL
Pkey            varchar(50)     NULL
CreatedDate     datetime        NULL
UpdatedDate     datetime        NULL
CreatedBy       nvarchar(100)   NULL
UpdatedBy       nvarchar(100)   NULL
```

### ItemPointsMember
PK: (none) — không có cột Counter/Pkey (bảng ngoại lệ)
```
PointsCode    varchar(20)     NOT NULL
ItemNo        varchar(20)     NOT NULL
Barcode       varchar(20)     NOT NULL
ItemName      nvarchar(500)   NOT NULL
Uom           varchar(20)     NOT NULL
ShelfLife     int             NOT NULL
Blocked       bit             NOT NULL
CrtDate       datetime        NOT NULL
DaysOfUsed    int             NOT NULL
```

### ItemUnitOfMeasure
PK: (none)
```
ItemNo                 nvarchar(20)     NOT NULL
Code                   nvarchar(10)     NOT NULL
QtyPerUnitOfMeasure    decimal(38,20)   NOT NULL
Length                 decimal(38,20)   NOT NULL
Width                  decimal(38,20)   NOT NULL
Height                 decimal(38,20)   NOT NULL
Cubage                 decimal(38,20)   NOT NULL
Weight                 decimal(38,20)   NOT NULL
Counter                bigint           NULL
Pkey                   varchar(50)      NULL
```

### LinkedItem
PK: (none)
```
ItemNo           nvarchar(20)     NOT NULL
UnitOfMeasure    nvarchar(10)     NOT NULL
LinkedItemNo     nvarchar(20)     NOT NULL
NoOfItems        decimal(38,20)   NOT NULL
Blocked          tinyint          NOT NULL
PrimaryKey       nvarchar(41)     NOT NULL
SalesType        nvarchar(20)     NOT NULL
DepositItem      tinyint          NOT NULL
Counter          bigint           NULL
Pkey             varchar(50)      NULL
```

### ItemEarnScale
PK: (none)
```
StoreGroup      nvarchar(20)     NOT NULL
StartingDate    date             NOT NULL
ItemNo          nvarchar(20)     NOT NULL
EndingDate      date             NOT NULL
Scale           decimal(38,5)    NOT NULL
Counter         bigint           NULL
Pkey            varchar(50)      NULL
```

---

## Sales Price

### SalesPrice
PK: composite `(ItemNo, SalesCode, StartingDate, UnitOfMeasureCode)` (`PK_SalesPrice`)
```
ItemNo                  nvarchar(20)     NOT NULL   -- PK
SalesCode               nvarchar(20)     NOT NULL   -- PK
StartingDate            datetime         NOT NULL   -- PK
CurrencyCode            nvarchar(10)     NOT NULL
UnitOfMeasureCode       nvarchar(10)     NOT NULL   -- PK
UnitPrice               float            NOT NULL
PriceIncludesVAT        tinyint          NOT NULL
AllowInvoiceDisc        tinyint          NOT NULL
SalesType               int              NOT NULL
MinimumQuantity         float            NULL
EndingDate              datetime         NOT NULL
VariantCode             nvarchar(10)     NOT NULL
AllowLineDisc           tinyint          NOT NULL
Counter                 bigint           NULL
Pkey                    varchar(50)      NULL
IsActive                bit              NULL       -- xác nhận qua GetSalesPriceList (WHERE IsActive=1) + usp_SalesPrice_SoftDelete (2026-07)
LastTimeUpdate          datetime         NULL       -- xác nhận qua usp_SalesPrice_UpdatePrice/_SoftDelete (2026-07); chưa có trong CentralMD.sql dump
```
> `EndingDate` năm `9999` (setup lưu `9999-12-31`, SP quy về `9999-01-01`) = giá hiệu lực vô thời hạn.
> `EndingDate` năm `7777` = **dòng đã xóa mềm** (bị loại khỏi `usp_SetupSalePrice_Save` qua điều kiện
> `YEAR(EndingDate) <> 7777`; cơ chế xóa của `usp_SalesPrice_SoftDelete`). `SalesCode` = mã
> Store/PriceGroup áp dụng.
> **Đính chính (2026-07)**: bảng **CÓ** `IsActive`/`LastTimeUpdate` (khác ghi chú cũ "KHÔNG có Id/IsActive")
> — phát hiện qua source thật của `GetSalesPriceList` (`AND S.IsActive = 1`, luôn bắt buộc bất kể `@isCheck`)
> và bản cập nhật `usp_SalesPrice_SoftDelete` (set `IsActive = 0` + `LastTimeUpdate = getdate()` khi xóa mềm,
> `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`). Sửa/Xóa vẫn định vị dòng bằng composite PK + `SalesType`
> (không dùng `IsActive`/`Id` làm khoá — bảng không có `Id`).

### SalesPriceRange
PK: (none) — bảng giá theo khoảng số lượng
```
ItemNo               nvarchar(20)    NOT NULL
UnitOfMeasureCode    nvarchar(10)    NOT NULL
StoreNo              nvarchar(20)    NOT NULL
FromDate             datetime        NOT NULL
ToDate               datetime        NOT NULL
FromQuantity         float           NOT NULL
ToQuantity           float           NOT NULL
UnitPrice            float           NOT NULL
SalesType            int             NULL
IsMember             bit             NOT NULL
Status               bit             NULL
Counter              bigint          NULL
Pkey                 varchar(50)     NULL
CreatedBy            varchar(50)     NULL
CreatedDate          datetime        NULL
UpdatedBy            varchar(50)     NULL
UpdatedDate          datetime        NULL
```

### SalesPriceRate
PK: (none)
```
ID                   int             NOT NULL
ItemNo               nvarchar(20)    NOT NULL
StoreNo              nvarchar(20)    NOT NULL
ValidDate            datetime        NOT NULL
UnitOfMeasureCode    nvarchar(10)    NOT NULL
Rate                 float           NULL
Counter              bigint          NULL
Pkey                 varchar(50)     NULL
CreatedDate          datetime        NULL
```

### SalesOrderType
PK: (none) — loại đơn hàng hiển thị trên POS (ship, tại quầy...)
```
ID              bigint          NOT NULL
Code            nvarchar(50)    NULL
TenderType      varchar(50)     NULL
Description     nvarchar(200)   NULL
Percent         float           NULL
ImageName       nvarchar(200)   NULL
IsActive        bit             NULL
Order           int             NULL
HotKey          nvarchar(50)    NULL
Counter         int             NULL
Pkey            varchar(50)     NULL
CreatedDate     datetime        NULL
UpdatedDate     datetime        NULL
CreatedBy       nvarchar(100)   NULL
UpdatedBy       nvarchar(100)   NULL
```

### SalesOrderTypeByStore
PK: (none)
```
ID                int             NOT NULL
StoreNo           varchar(50)     NULL
SalesOrderType    nvarchar(50)    NULL
Pkey              varchar(250)    NULL
Counter           bigint          NULL
Status            bit             NULL
CreatedDate       datetime        NULL
CreatedUser       varchar(50)     NULL
```

---

## Promotion / Offer

### OfferHeader
PK: (none) — bảng lớn nhất trong nhóm Offer (60 cột)
```
No                          nvarchar(20)    NOT NULL
Type                        int             NOT NULL
Description                 nvarchar(250)   NOT NULL
Status                      int             NOT NULL   -- 0 = Active
OfferType                   nvarchar(10)    NOT NULL
PriceGroup                  nvarchar(20)    NOT NULL
RoundingMethod               nvarchar(10)   NOT NULL
CurrencyCode                 nvarchar(10)   NOT NULL
LastDateModified              datetime      NOT NULL
ValidationPeriodID            nvarchar(10)  NOT NULL
ValidationDescription         nvarchar(30)  NOT NULL
StartingDate                  datetime      NOT NULL
EndingDate                    datetime      NOT NULL
BlockPeriodicDiscount          tinyint      NOT NULL
DealPrice                       float       NULL
ShowDealLines                   int         NOT NULL
SalesTypeFilter                  nvarchar(50) NOT NULL
SelectionType                     int        NOT NULL
CustomerDiscGroup                 nvarchar(10) NOT NULL
MemberValue                        nvarchar(10) NOT NULL
DiscountTrackingNo                 nvarchar(10) NOT NULL
CouponCode                          nvarchar(10) NOT NULL
CouponQtyNeeded                      float     NULL
MemberType                            int      NOT NULL
MemberAttribute                        nvarchar(10) NOT NULL
MemberAttributeValue                    nvarchar(30) NOT NULL
BlockSalesCommission                     tinyint NOT NULL
BlockManualPriceChange                    tinyint NOT NULL
BlockInfoCodeDiscount                      tinyint NOT NULL
BlockLineDiscountOffer                      tinyint NOT NULL
BlockTotalDiscountOffer                      tinyint NOT NULL
BlockTenderTypeDiscount                       tinyint NOT NULL
BlockMemberPoints                              tinyint NOT NULL
ConditionBuy                                    int    NOT NULL
MemberOnly                                       tinyint NOT NULL
ConditionGet                                      int   NOT NULL
NoSeries                                          nvarchar(20) NOT NULL
FromTime                                          nvarchar(10) NOT NULL
ToTime                                            nvarchar(10) NOT NULL
Mon, Tue, Wed, Thu, Fri, Sat, Sun                  tinyint (mỗi cột) NOT NULL
NumOfDays                                          int   NOT NULL
DayOfWeek                                          nvarchar(50) NOT NULL
TenderTypeCode                                     nvarchar(10) NOT NULL
TenderTypeValue                                    float NULL
TenderTypeOfferPercent                             float NULL
TenderTypeOfferAmount                              float NULL
BankCode                                           nvarchar(10) NOT NULL
LocalSiteGroup                                     nvarchar(20) NOT NULL
LimitQty                                           float NULL
VoucherFromDate                                    datetime NULL
VoucherToDate                                      datetime NULL
VoucherValidDay                                    int   NULL
VoucherLimitNumber                                 int   NULL
PromotionNo                                        varchar(20) NULL
PriorityBBY                                        int   NULL
Counter                                            bigint NULL
Pkey                                               varchar(50) NULL
MinValue                                           float NULL
TotalDiscountType                                  int   NULL
TotalDiscountValue                                 float NULL
IsVoucher                                          bit   NULL
IsTotalBill                                        bit   NULL
IsGift                                             bit   NULL
MemberCode                                         varchar(50) NULL
DiscountAmountMax                                  float NULL
IsFullPrice                                        bit   NULL
Remark                                             nvarchar(500) NULL
SalesType                                          varchar(50) NULL
```
> Đây là bảng OfferHeader "chuẩn hoá" (offer engine cũ). Song song còn có nhóm
> `SetupPromotionHEADER/BUY/GET/SITE` (flat, dùng cho UI CTKM/nhập liệu — xem SP
> `usp_SaveSetupCTKMAll`). Hai nhóm bảng phục vụ mục đích khác nhau, **không tự động đồng bộ**.

### OfferBuy
PK: (none)
```
OfferNo           nvarchar(20)     NOT NULL
LineNo            int              NOT NULL
LineType          int              NOT NULL
No                nvarchar(20)     NOT NULL
Description       nvarchar(150)    NOT NULL
UnitOfMeasure     nvarchar(20)     NOT NULL
DiscountType      int              NOT NULL
DiscountValue     decimal(38,20)   NOT NULL
Quantity          decimal(38,20)   NOT NULL
Step              decimal(38,20)   NOT NULL
BonusBuyNo        nvarchar(20)     NOT NULL
LineGroup         nvarchar(50)     NOT NULL
ScaleType         nvarchar(10)     NULL
Counter           bigint           NULL
Pkey              varchar(50)      NULL
```

### OfferGet
PK: (none) — cùng cấu trúc với `OfferBuy`
```
OfferNo           nvarchar(20)     NOT NULL
LineNo            int              NOT NULL
LineType          int              NOT NULL
No                nvarchar(20)     NOT NULL
Description       nvarchar(150)    NOT NULL
UnitOfMeasure     nvarchar(20)     NOT NULL
DiscountType      int              NOT NULL
DiscountValue     decimal(38,20)   NOT NULL
Quantity          decimal(38,20)   NOT NULL
Step              decimal(38,20)   NOT NULL
BonusBuyNo        nvarchar(20)     NOT NULL
LineGroup         nvarchar(50)     NOT NULL
ScaleType         nvarchar(10)     NULL
Counter           bigint           NULL
Pkey              varchar(50)      NULL
```

### OfferBenefits
PK: (none)
```
OfferNo          nvarchar(20)     NOT NULL
LineNo           int              NOT NULL
Type             int              NOT NULL
No               nvarchar(20)     NOT NULL
VariantCode      nvarchar(10)     NOT NULL
Description      nvarchar(250)    NOT NULL
ValueType        int              NOT NULL
Value            decimal(38,2)    NOT NULL
StepAmount       decimal(38,2)    NOT NULL
LineGroup        nvarchar(50)     NOT NULL
Quantity         int              NOT NULL
UnitOfMeasure    nvarchar(10)     NOT NULL
Counter          bigint           NULL
Pkey             varchar(50)      NULL
```

### OfferMaxQuantity
PK: (none)
```
ID               int             NOT NULL
OfferNo          varchar(20)     NULL
StoreNo          varchar(10)     NULL
ItemNo           varchar(20)     NULL
UOM              nvarchar(20)    NULL
MaxQuantity      float           NULL
Status           bit             NULL
Remark           nvarchar(200)   NULL
CreatedDate      datetime        NULL
CreatedUser      nvarchar(200)   NULL
UpdatedDate      datetime        NULL
UpdatedUser      nvarchar(200)   NULL
RefUpdatedDate   datetime        NULL
IsDeleted        bit             NULL
QuantitySale     float           NULL
IsRetricted      bit             NULL
```

### OfferPriority
PK: (none) — `Pkey` là NOT NULL (ngoại lệ)
```
OfferType     nvarchar(10)    NOT NULL
Priority      int             NULL
IsMember      bit             NULL
IsDuplicate   bit             NULL
Counter       bigint          NULL
Pkey          varchar(50)     NOT NULL
```

### OfferRetrict
PK: (none)
```
ID                bigint... int NOT NULL   -- [ID] int NOT NULL
StoreNo           varchar(20)     NULL
BonusBuyCode      varchar(20)     NULL
ArticleCode       varchar(20)     NULL
UOM               nvarchar(20)    NULL
Status            bit             NULL
CreatedDate       datetime        NULL
UpdatedDate       datetime        NULL
RefModifiedDate   datetime        NULL
RefID             int             NULL
Ref1..Ref5        nvarchar(500)   NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
```

### OfferSite
PK: (none)
```
OfferNo             nvarchar(20)    NOT NULL
PriceGroupCode      nvarchar(50)    NOT NULL
StoreNo             nvarchar(10)    NOT NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
```

### OfferType
PK: (none) — danh mục loại CTKM
```
ID              int             NOT NULL
OfferType       varchar(20)     NULL
OfferName       nvarchar(500)   NULL
IsTotalBill     bit             NULL
IsSetupBuy      bit             NULL
IsSetupGet      bit             NULL
IsVoucher       bit             NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
```

### SetupPromotionBUY
PK: `ID` identity (`PK_SetupPromotionBUY`) — nhóm bảng nhập liệu CTKM "flat" (UI/dashboard)
```
ID              int identity(1,1)  NOT NULL   -- PK
Key             varchar(50)        NULL
FileNameRead    nvarchar(1000)     NULL
CREATEDDATE     datetime           NULL
Remark          nvarchar(1000)     NULL
LINEINDICATOR   nvarchar(200)      NULL
BUYTYPE         nvarchar(200)      NULL   -- 'MGP' | 'MAT'
MATGROUP        nvarchar(200)      NULL
BBYNR           nvarchar(200)      NULL   -- FK logic → SetupPromotionHEADER.BBYNR
MAT_NR          nvarchar(200)      NULL
MAT_QUAN        nvarchar(200)      NULL
MEINH           nvarchar(200)      NULL
ScaleType       nvarchar(200)      NULL
DiscountType    nvarchar(200)      NULL
DiscountValue   nvarchar(200)      NULL
```

### SetupPromotionGET
PK: `ID` identity (`PK_SetupPromotionGET`)
```
ID              int identity(1,1)  NOT NULL   -- PK
Key             varchar(50)        NULL
FileNameRead    nvarchar(1000)     NULL
CREATEDDATE     datetime           NULL
Remark          nvarchar(1000)     NULL
LINEINDICATOR   nvarchar(200)      NULL
BBYNR           nvarchar(200)      NULL   -- FK logic → SetupPromotionHEADER.BBYNR
GETTYPE         nvarchar(200)      NULL   -- 'MGP' | 'MAT'
MATGROUP        nvarchar(200)      NULL
MATERIALCODE    nvarchar(200)      NULL
DISTYPE         nvarchar(200)      NULL   -- '%' | 'R' | 'P'
QTY             nvarchar(200)      NULL
SCALETYPE       nvarchar(200)      NULL
BBYVAL          nvarchar(200)      NULL
BBYPER          nvarchar(200)      NULL
PRICEUNIT       nvarchar(200)      NULL
MEINH           nvarchar(200)      NULL
```

### SetupPromotionHEADER
PK: `ID` identity (`PK_SetupPromotionHEADER`) — header CTKM flat (44 cột)
```
ID                    int identity(1,1)  NOT NULL   -- PK
Key                   varchar(50)        NULL
FileNameRead          nvarchar(1000)     NULL
CREATEDDATE           datetime           NULL
Remark                nvarchar(1000)     NULL
HongFileName          varchar(500)       NULL
LINEINDICATOR         nvarchar(200)      NULL
SalesType             nvarchar(200)      NULL
BBYNR                 nvarchar(200)      NULL   -- mã CTKM, business key (auto-gen từ 6000000001)
BBYTEXT               nvarchar(200)      NULL   -- = Description
BBYTYPE               nvarchar(200)      NULL   -- = OfferType
VALIDFROM             nvarchar(200)      NULL   -- yyyyMMdd
VALIDTO               nvarchar(200)      NULL   -- yyyyMMdd
TIMEFROM              nvarchar(200)      NULL
TIMETO                nvarchar(200)      NULL
MON, TUE, WED, THUR, FRI, SAT, SUN   nvarchar(200) (mỗi cột)  NULL
NUMOFDAYS             nvarchar(200)      NULL
BUYLINKCAT            nvarchar(200)      NULL   -- 'A' = AND, 'O' = OR
GETLINKCAT            nvarchar(200)      NULL
PROMOTION             nvarchar(200)      NULL
PROMOTIONTEXT         nvarchar(200)      NULL
LIMIT                 nvarchar(200)      NULL
TOTALMINVALUE         nvarchar(200)      NULL
MINVALUE              nvarchar(200)      NULL
TOTALDISCOUNT         nvarchar(200)      NULL
TOTALDISCOUNTTYPE     nvarchar(200)      NULL
TOTALDISCOUNTVALUE    nvarchar(200)      NULL
BANKCODE              nvarchar(200)      NULL
CARDTYPE              nvarchar(200)      NULL
FULLPRICE             nvarchar(200)      NULL
STATUS                nvarchar(200)      NULL
SITEGROUPCODE         nvarchar(200)      NULL
VINID                 nvarchar(200)      NULL   -- 'X' nếu MemberOnly
ZPRIOR                nvarchar(200)      NULL   -- = Priority
ZVCDATE_ST            nvarchar(200)      NULL   -- voucher from (fallback = VALIDFROM)
ZVCDATE_EN            nvarchar(200)      NULL   -- voucher to (fallback = VALIDTO)
ZVCDATE_VA            nvarchar(200)      NULL   -- = VoucherValidDay
LIMITNR               nvarchar(200)      NULL   -- = VoucherLimitNumber
IsVoucher             bit                NULL
IsApprove             bit                NULL   -- 1 = đã duyệt → KHÔNG cho sửa nữa
MemberCode            varchar(50)        NULL
DiscountAmountMax     float              NULL
IsFULLPRICE           bit                NULL
ZVCDAY_AFTER          int                NULL
ZVCTIME_AFTER         varchar(10)        NULL
```

### SetupPromotionSITE
PK: `ID` identity (`PK_SetupPromotionSITE`)
```
ID              int identity(1,1)  NOT NULL   -- PK
Key             varchar(50)        NULL
FileNameRead    nvarchar(1000)     NULL
CREATEDDATE     datetime           NULL
Remark          nvarchar(1000)     NULL
BBYNR           nvarchar(200)      NULL   -- FK logic → SetupPromotionHEADER.BBYNR
SITEGROUPCODE   nvarchar(200)      NULL
SITECODE        nvarchar(200)      NULL   -- = StoreNo (đã expand từ SiteGroupCode)
```

### ItemDiscount
PK: (none)
```
Id                   bigint          NOT NULL
Code                 varchar(50)     NOT NULL
StoreNo              varchar(20)     NULL
Title                nvarchar(300)   NULL
SubTitle             nvarchar(300)   NULL
Description          nvarchar(500)   NULL
ItemNo               varchar(20)     NULL
UOM                  varchar(20)     NULL
FromDate             datetime2(7)    NULL
ToDate               datetime2(7)    NULL
IsGenerateBarcode    bit             NULL
Status               bit             NULL
Seq                  int             NULL
Counter              bigint          NULL
Pkey                 varchar(50)     NULL
LastDateModified     datetime        NULL
LastUserModified     nvarchar(50)    NULL
WinCode              varchar(50)     NULL
MinAmount            float           NULL
DiscountBy           varchar(50)     NULL
PromotionCode        varchar(50)     NULL
PromotionItem        varchar(50)     NULL
Ref1..Ref5           nvarchar(500)   NULL
```

### ItemDiscountMember
PK: (none)
```
Id                  bigint          NOT NULL
Code                varchar(50)     NOT NULL
StoreNo             varchar(20)     NULL
Title               nvarchar(300)   NULL
SubTitle            nvarchar(300)   NULL
Description         nvarchar(500)   NULL
ItemNo              varchar(20)     NULL
UOM                 varchar(20)     NULL
BarcodeNo           nvarchar(50)    NULL
Quantity            float           NULL
Condition           varchar(20)     NULL
CusType             varchar(20)     NULL
FromDate            datetime2(7)    NULL
ToDate              datetime2(7)    NULL
Status              bit             NULL
Seq                 int             NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
LastDateModified    datetime        NULL
LastUserModified    nvarchar(50)    NULL
```

### MMLItemDiscount
PK: (none) — giảm giá theo giờ (MML = Multi-Meal-Line?)
```
Id                  bigint          NOT NULL
StoreNo             varchar(20)     NULL
Description         nvarchar(500)   NULL
ItemNo              varchar(20)     NULL
UOM                 varchar(20)     NULL
DiscountPrice       float           NULL
FromDate            datetime        NULL
ToDate              datetime        NULL
FromTime            varchar(10)     NULL
ToTime              varchar(10)     NULL
BonusBuyCode        varchar(50)     NULL
Status              bit             NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
LastDateModified    datetime        NULL
LastUserModified    nvarchar(50)    NULL
RefID               varchar(200)    NULL
RefModifiedDate     datetime        NULL
MBSDiscountPrice    float           NULL
DiscountType        int             NULL
Ref1..Ref5          nvarchar(500)   NULL
```

### MMLSchemeHeader
PK: (none)
```
ID              int             NOT NULL
Code            varchar(30)     NOT NULL
FromDate        date            NULL
ToDate          date            NULL
IsMember        bit             NULL
IsCallAPI       bit             NULL
MinAmount       float           NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
Ref1..Ref5      nvarchar(200)   NULL
```

### MMLSchemeItem
PK: (none)
```
ID              int             NOT NULL
HeaderCode      varchar(30)     NOT NULL   -- FK logic → MMLSchemeHeader.Code
Code            varchar(30)     NULL
ItemNo          varchar(50)     NULL
UOM             nvarchar(20)    NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
Ref1..Ref5      nvarchar(200)   NULL
CategoryCode    varchar(20)     NOT NULL
```

### MMLSchemeResponse
PK: (none)
```
ID              int             NOT NULL
HeaderCode      varchar(30)     NOT NULL   -- FK logic → MMLSchemeHeader.Code
Code            varchar(30)     NOT NULL
Title           nvarchar(500)   NULL
Link            varchar(500)    NULL
Description     nvarchar(500)   NULL
IsGenQR         bit             NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
Ref1..Ref5      nvarchar(200)   NULL
```

### SpecialComboHeader
PK: (none) — combo tự chọn món (F&B)
```
ID              int             NOT NULL
Code            varchar(50)     NULL
SalesType       varchar(50)     NULL
Name            nvarchar(500)   NULL
ComboQuantity   float           NULL
Amount          float           NULL
IsMember        bit             NULL
MemberCode      varchar(20)     NULL
FromDate        datetime        NULL
ToDate          datetime        NULL
IsEnable        bit             NULL
Pkey            varchar(50)     NULL
Counter         bigint          NULL
CreatedDate     datetime        NULL
CreatedBy       varchar(200)    NULL
UpdatedDate     datetime        NULL
UpdatedBy       varchar(200)    NULL
```

### SpecialComboLine
PK: (none)
```
ID                  int             NOT NULL
Code                varchar(50)     NULL   -- FK logic → SpecialComboHeader.Code
ItemNo              nvarchar(20)    NULL
ItemName            nvarchar(500)   NULL
UOM                 nvarchar(10)    NULL
GroupCode           nvarchar(20)    NULL
GroupName           nvarchar(500)   NULL
IsRequired          bit             NULL
IsDefault           bit             NULL
IsSendSAP           bit             NULL
MinimumQuantity     float           NULL
MaximumQuantity     float           NULL
Order               int             NULL
IsEnable            bit             NULL
IsDynamicPrice      bit             NULL
Pkey                varchar(50)     NULL
Counter             bigint          NULL
CreatedDate         datetime        NULL
CreatedBy           varchar(200)    NULL
UpdatedDate         datetime        NULL
UpdatedBy           varchar(200)    NULL
```

### SpecialComboStore
PK: (none)
```
ID              int             NOT NULL
Code            varchar(50)     NULL   -- FK logic → SpecialComboHeader.Code
StoreNo         nvarchar(10)    NULL
IsEnable        bit             NULL
Pkey            varchar(50)     NULL
Counter         bigint          NULL
CreatedDate     datetime        NULL
CreatedBy       varchar(200)    NULL
UpdatedDate     datetime        NULL
UpdatedBy       varchar(200)    NULL
```

---

## Bank & Payment

### Bank
PK: (none)
```
BankCode      nvarchar(20)     NOT NULL
BankName      nvarchar(100)    NOT NULL
Counter       int              NULL
Pkey          varchar(50)      NULL
CreatedDate   datetime         NULL
UpdatedDate   datetime         NULL
Status        bit              NULL
```

### BankCardType
PK: (none)
```
Code          nvarchar(20)     NOT NULL
Name          nvarchar(100)    NOT NULL
Type          nvarchar(200)    NULL
Counter       bigint           NULL
Pkey          varchar(50)      NULL
CreatedDate   datetime         NULL
UpdatedDate   datetime         NULL
Status        bit              NULL
```

### BankDiscount
PK: (none)
```
ID                    int              NOT NULL
Code                  varchar(50)      NULL
StoreGroup            varchar(50)      NULL
BankCode              varchar(50)      NULL
TenderType            varchar(20)      NULL
DiscountType          varchar(10)      NULL
DiscountValue         float            NULL
MinBillAmount         float            NULL
MaxDiscountAmount     float            NULL
IsViaTenderType       bit              NULL
ViaTenderType         varchar(20)      NULL
IsTotalBill           bit              NULL
FromDate              datetime         NULL
ToDate                datetime         NULL
IsEnable              bit              NULL
Pkey                  varchar(50)      NULL
Counter               bigint           NULL
CreatedDate           datetime         NULL
CreatedUser           varchar(200)     NULL
IsMember              bit              NULL
CustType              varchar(50)      NULL
Ref1..Ref3            nvarchar(500)    NULL
IsFullPrice           bit              NULL
```

### BankDiscountItem
PK: (none)
```
ID                  int             NOT NULL
BankDiscountCode    varchar(50)     NULL   -- FK logic → BankDiscount.Code
ItemNo              varchar(20)     NULL
UOM                 varchar(20)     NULL
Pkey                varchar(50)     NULL
Counter             bigint          NULL
IsEnable            bit             NULL
CreatedDate         datetime        NULL
```

### BankDiscountStore
PK: (none)
```
ID                  int             NOT NULL
StoreGroup          varchar(50)     NULL
StoreNo             varchar(50)     NULL
IsEnable            bit             NULL
Pkey                varchar(50)     NULL
Counter             bigint          NULL
CreatedDate         datetime        NULL
CreatedUser         varchar(200)    NULL
BankDiscountCode    varchar(50)     NULL   -- FK logic → BankDiscount.Code
```

### TenderType
PK: (none)
```
timestamp                 timestamp        NOT NULL
StoreNo                   nvarchar(10)     NOT NULL
Code                      nvarchar(10)     NOT NULL
PrimaryCode               nvarchar(10)     NULL
Description               nvarchar(100)    NULL
Function                  int              NULL
ChangeTenderCode          nvarchar(10)     NULL
Rounding                  int              NULL
RoundingTo                decimal(38,20)   NULL
MinAmountEntered          decimal(38,20)   NULL
MaxAmountEntered          decimal(38,20)   NULL
MinAmountAllowed          decimal(38,20)   NULL
MaxAmountAllowed          decimal(38,20)   NULL
MayBeUsed                 tinyint          NULL
AllowedReturnedOrder      bit              NULL
ManagerKeyControl         tinyint          NULL
KeyboardEntryAllowed      tinyint          NULL
OvertenderAllowed         tinyint          NULL
OvertenderMaxAmt          decimal(38,20)   NULL
DrawerOpens               tinyint          NULL
CardAccountNo             tinyint          NULL
AskForDate                tinyint          NULL
ForeignCurrency           tinyint          NULL
FloatAllowed              tinyint          NULL
BankAccountType           int              NULL
BankAccountNo             nvarchar(20)     NULL
BankAccountName           nvarchar(50)     NULL
ReturnVoucher             tinyint          NULL
AccountNo                 nvarchar(20)     NULL
Counter                   bigint           NULL
Pkey                      varchar(50)      NULL
Order                     int              NULL
Status                    bit              NULL
```

### TenderTypeConfig
PK: (none)
```
ID                bigint...int NOT NULL     -- [ID] int NOT NULL
TenderTypeCode    varchar(50)      NULL
IssueVATTo        nvarchar(50)     NULL
IsFullPayment     bit              NULL
Refkey1..Refkey10 nvarchar(200)    NULL   -- 10 cột Refkey1..Refkey10
Enabled           bit              NULL
CreatedDate       datetime         NULL
Pkey              varchar(50)      NULL
Counter           bigint           NULL
```

### TenderTypeImage
PK: (none)
```
Id                  bigint          NOT NULL
TenderTypeCode      varchar(50)     NOT NULL
SubTitle            nvarchar(300)   NULL
Description         nvarchar(500)   NULL
Image               nvarchar(max)   NULL
Status              bit             NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
LastDateModified    datetime        NULL
LastUserModified    nvarchar(50)    NULL
```

### TenderTypeSetup
PK: (none)
```
timestamp                timestamp       NOT NULL
Code                     nvarchar(10)    NOT NULL
PrimaryCode              nvarchar(10)    NULL
Description              nvarchar(30)    NOT NULL
DefaultFunction          int             NOT NULL
DefaultCardTender        tinyint         NOT NULL
DefaultCurrencyTender    tinyint         NOT NULL
Caption                  nvarchar(20)    NOT NULL
SeqOnPOS                 int             NOT NULL
IsInstallmentSell        tinyint         NOT NULL
PaymentMethod            int             NOT NULL
RefKey1                  nvarchar(10)    NOT NULL
RefKey2                  nvarchar(10)    NOT NULL
CurrencyCode             nvarchar(10)    NULL
Counter                  bigint          NULL
Pkey                     varchar(50)     NULL
ChangeTenderCode         nvarchar(10)    NULL
MayBeUsed                tinyint         NULL
AllowedReturnedOrder     bit             NULL
Order                    int             NULL
Status                   bit             NULL
```

### PaymentMethodQRCode
PK: (none)
```
ID                  bigint          NOT NULL
TenderCode          nvarchar(20)    NULL
ChangeTenderCode    nvarchar(20)    NULL
Description         nvarchar(500)   NULL
Counter             int             NULL
Pkey                varchar(50)     NULL
CreatedDate         datetime        NULL
UpdatedDate         datetime        NULL
CreatedBy           nvarchar(100)   NULL
UpdatedBy           nvarchar(100)   NULL
IsActive            bit             NULL
MarketingID         bigint          NULL
```

### MappingQRCode
PK: (none) — cấu hình mapping cột hiển thị mã QR động
```
ID              bigint          NOT NULL
MarketingID     bigint          NULL
IsEncrypt       bit             NULL
Position        int             NULL
Table           nvarchar(200)   NULL
Column          nvarchar(200)   NULL
Counter         int             NULL
Pkey            varchar(50)     NULL
CreatedDate     datetime        NULL
UpdatedDate     datetime        NULL
CreatedBy       nvarchar(100)   NULL
UpdatedBy       nvarchar(100)   NULL
```

---

## Voucher / Coupon

### CpnVchBOMHeader
PK: (none) — header Coupon/Voucher (BOM = phát hành theo mã)
```
ItemNo              nvarchar(20)     NOT NULL   -- business key, ví dụ "C70000001"
ItemName            nvarchar(300)    NOT NULL
UnitOfMeasure       nvarchar(10)     NOT NULL
DiscountType        int              NOT NULL
DiscountValue       decimal(38,20)   NOT NULL
MaxAmount           decimal(38,20)   NOT NULL
ArticleType         nvarchar(10)     NOT NULL   -- 'ZCPN' coupon | voucher khác
ValueOfVoucher      decimal(38,20)   NOT NULL
StartingDate        datetime         NOT NULL
EndingDate          datetime         NOT NULL
Blocked             tinyint          NOT NULL
CouponCode          nvarchar(50)     NOT NULL   -- = Serial (voucher)
LimitQty            int              NOT NULL
LastDateModified    datetime         NOT NULL
Counter             bigint           NULL
IsCheckItem         bit              NULL       -- 1 = tổng bill, 0 = theo sản phẩm
Pkey                varchar(50)      NULL
IssueType           varchar(20)      NULL       -- 'Auto' | 'Import'
MinAmount           float            NULL
Company             nvarchar(100)    NULL
RefKey1..RefKey5    nvarchar(300)    NULL
IsMultiUse          bit              NULL
LimitQtyUsed        int              NULL
CpnVchType          varchar(10)      NULL
IsCheckAPI          bit              NULL
SaleType            varchar(50)      NULL
StoreGroupCode      varchar(50)      NULL
```

### CpnVchBOMCodeIssue
PK: `ID` IDENTITY(1,1) (`PK_CpnVchBOMCodeIssue`); UNIQUE FILTERED INDEX trên `Code`
(`UX_CpnVchBOMCodeIssue_Code`, `WHERE Code IS NOT NULL`).

> Từ đợt gộp Internal_Voucher (xem `docs/sql/CpnVchBOMCodeIssue_ExtendSchema.sql`), bảng này
> dùng CHUNG cho 3 nguồn dữ liệu, phân biệt bằng cột `Source`:
> - `Source = 'COUPON'` — mã coupon nội bộ, phát hành hàng loạt qua POS.Web (admin,
>   `ICouponRepository`/`usp_SetupCoupon_SaveIssue`).
> - `Source = 'VOUCHER'` — mã voucher nội bộ, phát hành hàng loạt qua POS.Web
>   (`IVoucherRepository`/`usp_SetupVoucher_SaveIssue`). `ItemNo` = số thuần (seed `70000001`,
>   khác coupon `C7...`). Voucher **KHÔNG** ghi `CpnVchBOMIssueRule` (giữ tách khỏi coupon trong
>   `usp_SetupVoucher_GetList`). Điền đủ field redeem (`Status='SOLD'`, `Value`, `VoucherType`...).
> - `Source = 'SAP'` — voucher SAP tích hợp ERP, real-time qua POS.Api (`IVoucherCodeRepository`/
>   `usp_Voucher_*`). Thay thế bảng `Internal_Voucher` cũ (⚠️ LEGACY, xem mục riêng bên dưới).
>
> **Redeem/check dùng CHUNG cho cả 2 Source** (từ đợt vá `usp_Voucher_GetByCode`/`usp_Voucher_Redeem`
> — 2 SP không còn lọc theo `Source`, vì `Code` đã unique toàn bảng): mã coupon phát hành từ
> POS.Web có vòng đời `Status` đầy đủ `SOLD → RDM` giống hệt voucher SAP, và `Enabled` chuyển
> `1 → 0` khi mã bị redeem qua `api/sap/*` (đồng bộ hiển thị "Locked" ở
> `usp_SetupCoupon_GetCodes`, POS.Web). `ItemNo` cũng dùng chung 2 nguồn — SAP set
> `ItemNo = ActicleNo` (mirror), Coupon vốn đã có `ItemNo` tự nhiên (= `CpnVchBOMHeader.ItemNo`).
```
ID                    bigint          IDENTITY(1,1) NOT NULL   -- PK
ItemNo                varchar(50)     NULL     -- COUPON: FK logic → CpnVchBOMHeader.ItemNo;
                                                -- SAP: = ActicleNo (mirror, cho tra cứu chéo)
Code                  varchar(50)     NULL     -- mã coupon HOẶC VoucherNumber SAP (unique)
EmployeeCode          varchar(30)     NULL     -- dead column, không dùng ở đâu trong code
Enabled               bit             NULL     -- 1 lúc tạo (cả 2 Source); chuyển 0 khi redeem
                                                -- qua usp_Voucher_Redeem (áp dụng cả 2 Source)
CreatedDate           datetime        NULL
Counter               bigint          NULL     -- COUPON: version stamp (MAX(Counter)+1/lô)
Pkey                  varchar(50)     NULL     -- COUPON: = ItemNo, nhóm theo lô, không unique
Source                varchar(10)     NOT NULL DEFAULT('COUPON')  -- 'COUPON' | 'VOUCHER' | 'SAP'
Status                varchar(20)     NULL     -- SOLD/RDM/EXP/AVL — cả 2 Source, 'SOLD' lúc tạo
Return                int             NULL     -- legacy field, gần như không dùng
ActicleNo             varchar(50)     NULL     -- SAP: mã hàng SAP; COUPON: = ItemNo (mirror)
                                                -- (chính tả gốc "Acticle" giữ nguyên khớp field SAP thật)
ActicleType           varchar(20)     NULL     -- SAP: tự sinh 'ZCPN'/'ZVCN'; COUPON: = ArticleType
Value                 decimal(18,2)   NULL     -- mệnh giá; COUPON: = DiscountValue/ValueOfVoucher
                                                -- (chỉ có giá trị thật SAU usp_SetupCoupon_SaveAdvanced)
Voucher_Currency      varchar(10)     NULL     -- hard-code "VND" cho cả 2 Source
Validity_From_Date    date            NULL     -- COUPON: = CpnVchBOMHeader.StartingDate
Expiry_Date           date            NULL     -- COUPON: = CpnVchBOMHeader.EndingDate
CompanyCode           varchar(20)     NULL     -- hard-code "WCM" cho cả 2 Source
Partner               varchar(50)     NULL     -- SAP: hard-code "SAP"; COUPON: để NULL
IsEmployee            bit             NULL     -- không dùng trong logic hiện tại (cả 2 Source)
PhoneNumber           varchar(20)     NULL     -- COUPON: để NULL (không có dữ liệu lúc phát hành)
VoucherType           varchar(50)     NULL     -- SAP: vd "BNMH"; COUPON: = CpnVchType (chỉ có giá
                                                -- trị thật SAU usp_SetupCoupon_SaveAdvanced)
AmountUsed            decimal(18,2)   NULL     -- số tiền đã redeem (có thể < Value)
OrderUsed             nvarchar(50)    NULL     -- OrderNo đã redeem
```

### CpnVchBOMIssueRule
PK: (none) — nếu có dòng trong bảng này ⇒ coupon phát hành kiểu "Import list mã có sẵn"; nếu
KHÔNG có ⇒ coupon dạng "Voucher chuẩn" (xem `usp_SetupVoucher_GetList` dùng `NOT EXISTS`)
```
ID              int             NOT NULL
ItemNo          varchar(20)     NULL   -- FK logic → CpnVchBOMHeader.ItemNo
Prefix          varchar(50)     NULL
LenCode         int             NULL
IssueType       varchar(10)     NULL
CharOfNumber    int             NULL
CharPosition    int             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
```

### CpnVchBOMLine
PK: (none) — line item áp dụng coupon (khi `IsCheckItem = 0`)
```
ItemNo          nvarchar(20)    NOT NULL   -- FK logic → CpnVchBOMHeader.ItemNo
LineNo          int             NOT NULL
LineItemNo      nvarchar(20)    NOT NULL
Description     nvarchar(100)   NOT NULL
UnitOfMeasure    nvarchar(10)   NOT NULL
Counter          bigint         NULL
Barcode          varchar(50)    NULL
Pkey             varchar(50)    NOT NULL
```

### CpnVchBOMQuota
PK: (none) — hạn mức sử dụng coupon theo item/ngày
```
ItemNo            nvarchar(20)    NOT NULL
UnitOfMeasure     varchar(10)     NOT NULL
IsCheckMember     bit             NOT NULL
LimitQty          int             NOT NULL
Blocked           bit             NOT NULL
QtyOfDay          int             NOT NULL
```

### CpnVchBOMStore
PK: (none) — store được áp dụng coupon
```
ID              int             NOT NULL
ItemNo          varchar(20)     NULL   -- FK logic → CpnVchBOMHeader.ItemNo
StoreNo         varchar(20)     NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
```

### CpnVchCodeSend
PK: (none) — log gửi mã coupon cho khách (theo order)
```
ID              bigint          NOT NULL
StoreNo         varchar(50)     NULL
PosID           varchar(50)     NULL
Date            date            NULL
OrderNo         varchar(50)     NULL
ItemNo          varchar(50)     NULL
Code            varchar(50)     NULL
CreatedDate     datetime        NULL
PhoneNumber     varchar(12)     NULL
```

### Internal_Voucher — ⚠️ LEGACY/SUPERSEDED
> Đã được thay thế bởi `CpnVchBOMCodeIssue` (cột `Source='SAP'`) + `IVoucherCodeRepository`/
> `usp_Voucher_*`. Sau go-live, bảng này được `sp_rename` thành `Internal_Voucher_Legacy`
> (xem `docs/sql/Internal_Voucher_RenameLegacy.sql`) và giữ lại tạm thời làm backup — không còn
> code nào trong solution ghi/đọc bảng này. Giữ định nghĩa cột dưới đây để tra cứu lịch sử/đối
> chiếu dữ liệu migrate. Lên lịch DROP hẳn sau 2-4 tuần ổn định (xem `docs/ROLLOUT.md` §D6).

PK: `ID` identity (`PK_Internal_Voucher`)
```
ID                    int identity(1,1)  NOT NULL   -- PK
VoucherNumber         varchar(50)        NOT NULL
Status                varchar(20)        NULL
Return                int                NULL
ActicleNo             varchar(50)        NULL
ActicleType           varchar(20)        NULL
Value                 decimal(18,2)      NULL
Voucher_Currency      varchar(10)        NULL
Validity_From_Date    date               NULL
Expiry_Date           date               NULL
CompanyCode           varchar(20)        NULL
Partner               varchar(50)        NULL
IsEmployee            bit                NULL
PhoneNumber           varchar(20)        NULL
VoucherType           varchar(50)        NULL
CreatedDate           datetime           NULL   -- default getdate()
AmountUsed            decimal(18,2)      NULL
OrderUsed             nvarchar(50)       NULL
```

### RewardCode
PK: (none)
```
ID              int             NOT NULL
RewardNo        varchar(20)     NULL   -- FK logic → RewardHeader.RewardNo
Code            varchar(20)     NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
```

### RewardCodeSend
PK: (none) — log gửi mã reward
```
ID              bigint          NOT NULL
StoreNo         varchar(50)     NULL
PosID           varchar(50)     NULL
Date            date            NULL
OrderNo         varchar(50)     NULL
OfferNo         varchar(50)     NULL
Code            varchar(50)     NULL
CreatedDate     datetime        NULL
IPServer        varchar(50)     NULL
```

### RewardHeader
PK: (none)
```
ID              int             NOT NULL
RewardNo        varchar(20)     NULL
Title           nvarchar(500)   NULL
FromDate        date            NULL
ToDate          date            NULL
OfferNo         varchar(20)     NULL
Link            varchar(500)    NULL
Description     nvarchar(500)   NULL
Enabled         bit             NULL
IsReward        bit             NULL
CreatedDate     datetime        NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
SubDesc         nvarchar(150)   NULL
```

### WinCodeHeader
PK: (none) — `ID` là `uniqueidentifier` (GUID, không phải PK constraint)
```
ID              uniqueidentifier   NOT NULL
ProgramCode     varchar(20)        NOT NULL
WinCode         varchar(10)        NOT NULL
FromDate        datetime           NOT NULL
ToDate          datetime           NOT NULL
Quantity        int                NOT NULL
Status          bit                NOT NULL
CreatedDate     datetime           NOT NULL
UpdatedDate     datetime           NULL
CreatedBy       nvarchar(50)       NOT NULL
UpdatedBy       nvarchar(50)       NULL
Pkey            varchar(20)        NOT NULL
Counter         bigint             NOT NULL
DiscountType    varchar(20)        NOT NULL
ApplyType       varchar(30)        NOT NULL
OfferType       varchar(20)        NULL
```

### WinCodeStore
PK: (none)
```
ID              uniqueidentifier   NOT NULL
ProgramCode     varchar(20)        NOT NULL   -- FK logic → WinCodeHeader.ProgramCode
StoreNo         varchar(20)        NULL
Status          bit                NULL
CreatedDate     datetime           NULL
UpdatedDate     datetime           NULL
CreatedBy       nvarchar(50)       NULL
UpdatedBy       nvarchar(50)       NULL
Pkey            varchar(20)        NULL
Counter         bigint             NULL
```

### WinCodeCustomer
PK: (none) — log dùng WinCode theo order/khách
```
ID                        uniqueidentifier   NOT NULL
WinCode                   varchar(30)        NULL
MemberCard                varchar(20)        NULL
Csn                       varchar(20)        NULL
QuantityRecieptedSum      int                NULL
QuantityReciepted         int                NULL
OrderNo                   varchar(20)        NULL
StoreNo                   varchar(10)        NULL
PosNo                     varchar(10)        NULL
IsDeleted                 bit                NULL
TransDate                 datetime           NULL
CreatedDate               datetime           NULL
UpdatedDate                datetime          NULL
CreatedBy                  nvarchar(50)      NULL
UpdatedBy                   nvarchar(50)     NULL
PromotionCode                varchar(30)     NULL
```

---

## Loyalty / Offline Percent

### CXOfflinePercent
PK: (none) — % tính điểm offline theo khoảng amount (CX = ?)
```
ID              int             NOT NULL
FromAmount      float           NULL
ToAmount        float           NULL
Percent         float           NULL
FromDate        datetime        NULL
ToDate          datetime        NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
CreatedUser     nvarchar(300)   NULL
Counter         int             NULL
Pkey            varchar(50)     NULL
```

### VinIDOfflinePercent
PK: (none) — cùng cấu trúc `CXOfflinePercent`, áp cho VinID
```
ID              int             NOT NULL
FromAmount      float           NULL
ToAmount        float           NULL
Percent         float           NULL
FromDate        datetime        NULL
ToDate          datetime        NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
CreatedUser     nvarchar(300)   NULL
Counter         int             NULL
Pkey            varchar(50)     NULL
```

### LoyaltyRate
PK: (none)
```
Code            nvarchar(20)    NOT NULL
FromDate        datetime        NULL
ToDate          datetime        NULL
Enable          bit             NULL
Rate            float           NOT NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
CreatedDate     datetime        NULL
CreatedBy       nvarchar(50)    NULL
UpdatedDate     datetime        NULL
Updatedby       nvarchar(50)    NULL
CardType        nvarchar(20)    NULL
TextInBill      nvarchar(500)   NULL
```

---

## Staff & User

### Staff
PK: `ID` (`CONSTRAINT [Staff$0]`)
```
ID                        varchar(50)     NOT NULL   -- PK, mã nhân viên
Password                  nvarchar(200)   NULL
ChangePassword            tinyint         NULL
StoreNo                   nvarchar(10)    NULL
VoidTransaction           int             NULL
ManagerPrivileges         int             NULL
TenderDeclaration         int             NULL
FloatingDeclaration       int             NULL
PriceOverride             int             NULL
MaxDiscountToGive         float           NULL
SuspendTransaction        int             NULL
MaxTotalDiscount          float           NULL
OpenDrawWithoutSale       int             NULL
FirstName                 nvarchar(50)    NULL
LastName                  nvarchar(50)    NULL
EmploymentType            int             NULL   -- 0 = Nhân viên, 1 = Quản lý
FraudSortField            float           NULL
LastDateModified          datetime        NULL
NameOnReceipt             nvarchar(15)    NULL
Address                   nvarchar(150)   NULL
City                      nvarchar(50)    NULL
PostCode                  nvarchar(20)    NULL
HomePhoneNo               nvarchar(30)    NULL
WorkPhoneNo               nvarchar(30)    NULL
HourlyRate                float           NULL
PayrollNo                 nvarchar(20)    NULL
Blocked                   tinyint         NULL   -- 0/'' = đang hoạt động, khác = ngưng
DateToBeBlocked           datetime        NULL
LeftHanded                tinyint         NULL
SalesPerson               nvarchar(10)    NULL
PermissionGroup           nvarchar(10)    NULL
ReturnInTransaction       int             NULL
VoidPrepayment            int             NULL
VoidPrepaymentLine        int             NULL
ChangePrepaymentAmt       int             NULL
AddPrepaymentAmt          int             NULL
VoidLine                  int             NULL
AddPayment                int             NULL
SplitBills                int             NULL
Language                  nvarchar(10)    NULL
CreateCustomers           int             NULL
ViewSalesHistory          int             NULL
UpdateCustomers           int             NULL
InventoryActive           tinyint         NULL
InventoryMainMenu         nvarchar(10)    NULL
POSStyleProfile           nvarchar(10)    NULL
POSMenuProfile            nvarchar(10)    NULL
DeliverStatus             int             NULL
Counter                   bigint          NULL
Pkey                      varchar(50)     NULL
```

### User
PK: `UserName` (`CONSTRAINT [User$0]`)
```
timestamp                 timestamp       NOT NULL
UserName                  nvarchar(50)    NOT NULL   -- PK
FullName                  nvarchar(80)    NOT NULL
State                     int             NOT NULL
ExpiryDate                datetime        NOT NULL
WindowsSecurityID         nvarchar(119)   NOT NULL
ChangePassword            tinyint         NOT NULL
LicenseType               int             NOT NULL
AuthenticationEmail       nvarchar(250)   NOT NULL
StoreNo                   nvarchar(10)    NULL
Counter                   bigint          NULL
Pkey                      varchar(50)     NULL
```

---

## Weight Scale

### WeightScale_AssortmentItem
PK: (none)
```
ID              int             NOT NULL
Assortment      varchar(50)     NULL
ItemNo          varchar(50)     NULL
Category        varchar(10)     NULL
FromDate        date            NULL
ToDate          date            NULL
Pkey            varchar(100)    NULL
Counter         bigint          NULL
CreateDate      datetime        NULL
IsChange        bit             NULL
```

### WeightScale_AssortmentSite
PK: (none)
```
ID              int             NOT NULL
Assortment      varchar(50)     NULL
StoreNo         varchar(50)     NULL
Channel         varchar(5)      NULL
FromDate        date            NULL
ToDate          date            NULL
IsActive        varchar(10)     NULL
Pkey            varchar(100)    NULL
Counter         bigint          NULL
CreateDate      datetime        NULL
IsChange        bit             NULL
```

### WeightScale_INGREDIENT
PK: (none)
```
INGREDIENT      int             NOT NULL
INGRE_ET1       nvarchar(50)    NULL
INGRE_ET2       nvarchar(50)    NULL
CreateDate      datetime        NULL
```

### WeightScale_Item_Change
PK: (none) — nhật ký thay đổi item cần đồng bộ ra máy cân điện tử
```
Id              bigint          NOT NULL
ItemNo          varchar(20)     NULL
Date            date            NULL
CreatedDate     datetime        NULL
UpdatedDate     datetime        NULL
Remark          nvarchar(4000)  NULL
```

### WeightScale_Log
PK: (none)
```
ID              int             NOT NULL
StoreNo         varchar(20)     NULL
FileName        varchar(500)    NULL
ErrMsg          nvarchar(4000)  NULL
TotalPlu        int             NULL
CreatedDate     datetime        NULL
ProcessID       varchar(200)    NULL
```

### WeightScale_Multimex
PK: (none) — cờ store dùng format Multimex
```
ID              int             NOT NULL
StoreNo         varchar(50)     NULL
Enabled         bit             NULL
CreatedDate     datetime        NULL
```

### WeightScale_PLU_LIST
PK: (none) — danh sách PLU xuất cho máy cân
```
PLU_NO          varchar(6)      NOT NULL
PLU_TYPE        varchar(1)      NULL
ITEMCODE        varchar(6)      NULL
ITEMNAME        nvarchar(150)   NULL
LABEL_NO        varchar(2)      NULL
PACKEDATE       int             NULL
SELLBYDATE      int             NULL
INGREDIENT      varchar(2)      NULL
BESTBFDATE      int             NULL
ET1             nvarchar(50)    NULL
ET2             nvarchar(50)    NULL
ITEM_NO         varchar(20)     NULL
UOM             varchar(10)     NULL
BARCODE         varchar(50)     NULL
PKEY            varchar(50)     NULL
Counter         bigint          NULL
Enable          bit             NULL
CREATEDDATE     datetime        NULL
```

### WeightScale_PLU_PRICE_STORE
PK: (none)
```
ID                  int             NOT NULL
ItemNo              varchar(50)     NULL
StoreNo             varchar(50)     NULL
Pkey                varchar(100)    NULL
Counter             bigint          NULL
CreateDate          datetime        NULL
IsChange            bit             NULL
PLU_NO              varchar(6)      NULL
UOM                 varchar(6)      NULL
Blocked             bit             NULL
PRICE               decimal(18,0)   NULL
UnitPrice           decimal(18,0)   NULL
DiscountAmount      decimal(18,0)   NULL
```

### WeightScale_Processing
PK: (none) — trạng thái tiến trình sinh file PLU theo store
```
ID                  int             NOT NULL
StoreNo             varchar(20)     NULL
IsChange            bit             NULL
IsFinish            bit             NULL
CreatedDate         datetime        NULL
FinishedDate        datetime        NULL
IsFormatMultimex    bit             NULL
IsFormatStandard    bit             NULL
```

---

## Sync / Master Data Distribution

> Xem quy trình đầy đủ ở CLAUDE.md mục "Sinh file master data .zip cho POS".

### SyncTableList
PK: `ID` identity — danh mục bảng được đồng bộ xuống POS (dùng bởi `SyncTable_Get`)
```
ID                int identity(1,1)  NOT NULL   -- PK
TableName         varchar(50)        NULL
POSLastCounter    bigint             NULL   -- default 0
LastUpdated       datetime           NULL   -- default getdate()
Enabled           bit                NULL   -- default 1
Procedure         varchar(50)        NULL
IsOnlyChange      bit                NULL
IsAll             bit                NULL
OrderByName       int                NULL
IsByStore         bit                NULL   -- default 0 — bảng có filter theo store
IsSAP             bit                NULL   -- default 1
IsApplyDel        bit                NULL
GroupName         varchar(50)        NULL
ColumnFilter      varchar(50)        NULL   -- tên cột filter khi IsByStore=1 (vd 'No' → Store.No)
IsFirstDataAll    bit                NULL   -- default 0
```

### SyncTableFromPOS
PK: `Id` identity
```
Id                int identity(1,1)  NOT NULL   -- PK
TableName         varchar(50)        NULL
GroupName         varchar(50)        NULL
DocumentNoName    varchar(50)        NULL
OrdBy             int                NULL
Status            int                NULL
```

### MasterDataDownloadLog
PK: `Id` identity (`PK_MasterDataDownloadLog`)
```
Id                int...bigint identity(1,1) NOT NULL   -- PK, [Id] [bigint] IDENTITY(1,1)
SiteCode          varchar(50)        NULL
PosTerminal       varchar(50)        NULL
FileName          nvarchar(260)      NULL
FilePath          nvarchar(1000)     NULL
FileSizeBytes     bigint             NOT NULL   -- default 0
DurationMs        bigint             NOT NULL   -- default 0
Status            varchar(20)        NOT NULL   -- 'Success' | 'Aborted' | 'Error'
ClientIp          varchar(64)        NULL
DownloadedAt      datetime           NOT NULL   -- default getdate()
```
> Script tạo bảng riêng: `docs/sql/MasterDataDownloadLog.sql`. Ghi qua
> `IMasterDataSyncService.LogDownloadAsync` — fail-safe nếu bảng chưa tồn tại.

---

## SysWebApi

### SysWebApi
PK: `AppCode` (`PK_SysWebApi$`)
```
AppCode         varchar(10)     NOT NULL   -- PK
Host            varchar(250)    NOT NULL
Version         varchar(10)     NOT NULL
Authorization   varchar(30)     NOT NULL
UserName        nvarchar(50)    NOT NULL
Password        nvarchar(128)   NOT NULL
PublicKey       nvarchar(max)   NOT NULL
PrivateKey      nvarchar(max)   NOT NULL
Blocked         bit             NOT NULL
HttpProxy       varchar(100)    NOT NULL
Bypasslist      varchar(100)    NOT NULL
Description     nvarchar(100)   NOT NULL
```
> Đây là nguồn cấu hình external API dùng qua `ICentralMDRepository.GetSysWebApiAsync(appCode)`
> (cache Redis `MD:SysWebApi`, TTL 12h) — xem `.claude/skills/api/SKILLS.md`.

### SysWebApiConfig
PK: composite `(Code, Name)` (`PK_SysWebApiConfig$`)
```
Code                varchar(20)     NOT NULL   -- PK
Name                varchar(20)     NOT NULL   -- PK
Prefix              varchar(500)    NOT NULL
Description         nvarchar(130)   NOT NULL
Blocked             bit             NOT NULL
ConnectionString    nvarchar(2000)  NULL
ProcedureName       varchar(50)     NULL
```

### SysWebApiRoute
PK: composite `(AppCode, Name)` (`PK_SysWebApiRoute$`) — **FK**: `AppCode` → `SysWebApi.AppCode`
```
AppCode         varchar(10)     NOT NULL   -- PK, FK → SysWebApi.AppCode
Name            varchar(150)    NOT NULL   -- PK
Route           nvarchar(100)   NOT NULL
Description     nvarchar(100)   NOT NULL
Blocked         bit             NOT NULL
Version         varchar(10)     NOT NULL
Notes           nvarchar(1000)  NOT NULL
```

### SysWebApiUser
PK: `UserName` (`SysWebApiUser$`)
```
AppCode         varchar(10)     NOT NULL
UserName        nvarchar(50)    NOT NULL   -- PK
Password        nvarchar(128)   NOT NULL
Description     nvarchar(100)   NOT NULL
Authorization   varchar(30)     NOT NULL
Blocked         bit             NOT NULL
```

---

## Survey

### SurveyQuestion
PK: (none)
```
Id                  bigint          NOT NULL
QuestionCode        varchar(50)     NOT NULL
Type                varchar(20)     NULL
QuestionType        varchar(20)     NULL
Content             nvarchar(500)   NULL
Status              bit             NULL
Seq                 int             NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
LastDateModified    datetime        NULL
LastUserModified    nvarchar(50)    NULL
```

### SurveyAnswer
PK: (none)
```
Id                  bigint          NOT NULL
AnswerCode          varchar(50)     NOT NULL
QuestionCode        varchar(50)     NOT NULL   -- FK logic → SurveyQuestion.QuestionCode
AnswerContent       nvarchar(500)   NULL
Status              bit             NULL
Seq                 int             NULL
Counter             bigint          NULL
Pkey                varchar(50)     NULL
LastDateModified    datetime        NULL
LastUserModified    nvarchar(50)    NULL
```

---

## Dashboard / Web Admin (POS.Web)

### DashboardUsers
PK: `Id` identity — UNIQUE: `Username`
```
Id              int identity(1,1)  NOT NULL   -- PK
Username        nvarchar(100)      NOT NULL   -- UNIQUE
PasswordHash    nvarchar(256)      NOT NULL   -- BCrypt
FullName        nvarchar(200)      NOT NULL
Role            nvarchar(50)       NOT NULL   -- StoreOperator | ITOps | SystemAdmin
StoreCodes      nvarchar(max)      NULL       -- JSON array mã store (StoreOperator)
IsActive        bit                NOT NULL   -- default 1
CreatedAt       datetime2(7)       NOT NULL   -- default getdate()
UpdatedAt       datetime2(7)       NOT NULL   -- default getdate()
```

### DashboardAuditLog
PK: `Id` identity (`PK_DashboardAuditLog`)
```
Id              bigint identity(1,1)  NOT NULL   -- PK
Actor           nvarchar(100)         NOT NULL
Action          nvarchar(20)          NOT NULL
EntityType      nvarchar(50)          NOT NULL
EntityKey       nvarchar(200)         NOT NULL
OldValue        nvarchar(max)         NULL
NewValue        nvarchar(max)         NULL
ActedAt         datetime2(3)          NOT NULL   -- default sysutcdatetime()
```
> Ghi qua `IAuditLogger.LogAsync(...)` — xem `.claude/skills/web/audit-logging.md`.

### SqlConsoleAuditLog
PK: `Id` identity — audit cho tool SQL console nội bộ (Admin)
```
Id              bigint identity(1,1)  NOT NULL   -- PK
Actor           nvarchar(100)         NOT NULL
DbKey           nvarchar(50)          NOT NULL
DbCatalog       nvarchar(100)         NOT NULL
SqlText         nvarchar(max)         NOT NULL
RowsAffected    int                   NOT NULL
HasWhere        bit                   NOT NULL
Status          nvarchar(20)          NOT NULL
ElapsedMs       bigint                NOT NULL
ExecutedAt      datetime2(7)          NOT NULL
DecidedAt       datetime2(7)          NOT NULL   -- default getdate()
```

---

## Notify / Marketing

### Notify
PK: (none)
```
ID              bigint          NOT NULL
Code            varchar(50)     NULL
Title           nvarchar(250)   NULL
Description     nvarchar(500)   NULL
Note            nvarchar(500)   NULL
Content         nvarchar(max)   NULL
ContentType     varchar(50)     NULL
Target          varchar(100)    NULL
Pkey            varchar(250)    NULL
Counter         bigint          NULL
Status          bit             NULL
CreatedDate     datetime        NULL
CreatedUser     varchar(50)     NULL
```

### OptionData
PK: (none) — danh mục PaymentType/SalesType/TenderType hiển thị trên UI cấu hình
```
timestamp               timestamp       NOT NULL
Code                    nvarchar(50)    NOT NULL
PrimaryCode             nvarchar(50)    NULL
Description             nvarchar(500)   NULL
TenderType               nvarchar(10)   NULL
TenderTypeName            nvarchar(150) NULL
PaymentType                varchar(50)  NULL
PaymentTypeName              nvarchar(150) NULL
SalesType                     nvarchar(10) NULL
SalesTypeName                   nvarchar(150) NULL
Caption                          nvarchar(50)  NOT NULL
CurrencyCode                      nvarchar(10) NULL
AllowedReturnedOrder                bit        NULL
IsTPay                                bit      NULL
MayBeUsed                              tinyint NULL
IsActive                                 bit    NULL
Status                                    bit   NULL
Order                                      int  NULL
SeqOnPOS                                   int  NULL
HotKey                                     nvarchar(50) NULL
Counter                                    bigint NULL
Pkey                                       varchar(50) NULL
CreatedDate                                datetime NULL
UpdatedDate                                datetime NULL
CreatedBy                                  nvarchar(100) NULL
UpdatedBy                                  nvarchar(100) NULL
```

---

## Reason / Source

### ReasonCode
PK: (none) — có cả cột `Code` (business key legacy) và `ID` (int)
```
Code              nvarchar(10)    NOT NULL
Description       nvarchar(50)    NOT NULL
Group             nvarchar(10)    NOT NULL
HandpointCode     int             NOT NULL
Counter           bigint          NULL
Pkey              varchar(50)     NULL
Status            bit             NULL
ID                int             NOT NULL
```

### SourceBill
PK: (none) — nguồn hoá đơn (kênh bán: tại quầy, app, ship...)
```
ID                          int             NOT NULL
Code                        nvarchar(20)    NOT NULL
Description                 nvarchar(250)   NULL
Channel                     varchar(30)     NULL
Seq                         int             NULL
Status                      bit             NULL
Counter                     bigint          NULL
Pkey                        varchar(50)     NULL
CreatedDate                 datetime        NULL
CreatedBy                   nvarchar(50)    NULL
UpdatedDate                 datetime        NULL
Updatedby                   nvarchar(50)    NULL
Percent                     float           NULL
TenderType                  nvarchar(50)    NULL
AllowTenderTypePayment      nvarchar(500)   NULL
Ref1..Ref5                  nvarchar(500)   NULL
```

---

## Khác

### Interface_Errors
PK: (none) — log lỗi tích hợp (legacy interface job)
```
ErrorID           int             NOT NULL
UserName          varchar(100)    NULL
ErrorNumber       int             NULL
ErrorState        int             NULL
ErrorSeverity     int             NULL
ErrorLine         int             NULL
ErrorProcedure    varchar(max)    NULL
ErrorMessage      varchar(max)    NULL
ErrorDateTime     datetime        NULL
ProcessID         varchar(100)    NULL
```

### UnitOfMeasure
PK: (none) — danh mục đơn vị tính toàn cục
```
Code            nvarchar(10)    NOT NULL
Description     nvarchar(10)    NOT NULL
Counter         bigint          NULL
Pkey            varchar(50)     NULL
```

---

## Default constraints đáng chú ý

| Bảng.Cột | Default |
|---|---|
| `Branch.Counter` | `0` |
| `Branch.Pkey` | `''` |
| `DashboardAuditLog.ActedAt` | `sysutcdatetime()` |
| `DashboardUsers.IsActive` | `1` |
| `DashboardUsers.CreatedAt` / `UpdatedAt` | `getdate()` |
| `Internal_Voucher.CreatedDate` | `getdate()` |
| `Item.Counter` | `0` |
| `Item.Pkey` | `''` |
| `MasterDataDownloadLog.FileSizeBytes` / `DurationMs` | `0` |
| `MasterDataDownloadLog.DownloadedAt` | `getdate()` |
| `POSDataSetup.Counter` | `0` |
| `POSDataSetup.Pkey` | `''` |
| `POSMonitor.IsMonitor` | `1` |
| `POSMonitor.IntervalJob` | `5` |
| `POSMonitor.CreateDate` | `getdate()` |
| `POSTerminal.Counter` | `0` |
| `POSTerminal.Pkey` | `''` |
| `POSTerminal.Status` | `1` |
| `SqlConsoleAuditLog.DecidedAt` | `getdate()` |
| `Staff.Counter` | `0` |
| `Staff.Pkey` | `''` |
| `Store.Counter` | `0` |
| `Store.Pkey` | `''` |
| `SyncTableList.POSLastCounter` | `0` |
| `SyncTableList.LastUpdated` | `getdate()` |
| `SyncTableList.Enabled` | `1` |
| `SyncTableList.IsByStore` | `0` |
| `SyncTableList.IsSAP` | `1` |
| `SyncTableList.IsFirstDataAll` | `0` |
| `User.Counter` | `0` |
| `User.Pkey` | `''` |

## Foreign key duy nhất

```
ALTER TABLE [dbo].[SysWebApiRoute] WITH CHECK ADD CONSTRAINT [FK_AppCode_SysWebApi]
FOREIGN KEY([AppCode]) REFERENCES [dbo].[SysWebApi] ([AppCode])
```

---

## Stored Procedures

> Toàn bộ SP dưới đây định nghĩa trong `docs/sql/database/CentralMD.sql`. Chỉ ghi **tên + tham
> số + mục đích + bảng chính liên quan** — xem file gốc để lấy full body khi cần gọi từ
> Repository (Dapper, async + `IConfiguration`, xem quy tắc Repository trong `CLAUDE.md`).

### GetBankPOSList
```
(@StoreNo nvarchar(10)='', @TextSearch nvarchar(50)='', @BankCode varchar(50)='',
 @Status nvarchar(50)='', @PageSize int, @PageNumber int)
```
Danh sách `POSTerminalBank` (join `Store`), phân trang OFFSET/FETCH, tìm theo `BankPOSCode`/`POSTerminal`.

### GetEmployeeList
```
(@StaffCode varchar(50), @StaffName nvarchar(50), @StoreNo nvarchar(10), @TypeGroup varchar(10),
 @Status varchar(10), @PageSize int, @PageNumber int)
```
Danh sách `Staff` (join `Store`), phân trang. `@TypeGroup`: `'0'`=Nhân viên, `'1'`=Quản lý, `'-1'`=tất cả.

### GetEmployeeList_Export
```
(@StaffCode varchar(50), @StaffName nvarchar(50), @StoreNo nvarchar(10), @TypeGroup varchar(10), @Status varchar(10))
```
Giống `GetEmployeeList` nhưng không phân trang — dùng cho export Excel.

### GetProductList
```
(@ItemCode nvarchar(20)='', @ItemName nvarchar(500)='', @BarCode nvarchar(50)='',
 @TaxCode nvarchar(10)='', @PageSize int, @PageNumber int)
```
Danh sách `Item` join `Barcodes`, filter `Blocked = 0`, phân trang qua temp table `#TempProduct2`.

### GetProductList_Export
```
(@ItemCode nvarchar(20)='', @ItemName nvarchar(500)='', @BarCode nvarchar(50)='', @TaxCode nvarchar(10)='')
```
Giống `GetProductList` không phân trang — export Excel.

### GetPromotionOfferHeaderList
```
(@No nvarchar(20)='', @Description nvarchar(250)='', @Status nvarchar(10)='', @OfferType nvarchar(10)='',
 @ItemNo varchar(20)='', @StoreNo varchar(10)='', @Exp int, @PageSize int, @PageNumber int)
```
Danh sách `OfferHeader`, lọc theo item áp dụng (`OfferBuy`/`OfferGet`/`OfferBenefits` qua temp
table `#TempPro`/`#TempProEx`), tính `Status` hiệu lực theo `StartingDate`/`EndingDate`.

### GetSalesPriceList
```
(@ItemCode nvarchar(20), @ItemName nvarchar(500), @SaleType nvarchar(50), @SalesGroup nvarchar(20),
 @isCheck int, @PageSize int, @PageNumber int)
```
Danh sách `SalesPrice`, join `StorePriceGroup` (`#TmpStorePriceGroup`) + `SalesOrderType` (trả
`SaleTypeName`). `@isCheck`: `1`=có hiệu lực, `0`=tất cả. `@SaleType`/`@SalesGroup`=`''`→tất cả.
Cột `SalesCode` trả về = **`PriceGroupName`** (tên hiển thị) — cột `SalesGroupCode` trả
`SalesPrice.SalesCode` (mã gốc), cột `SalesTypeCode` trả `SalesPrice.SalesType` (mã gốc hình thức
bán hàng) — **cả 2 mã gốc dùng làm khoá cho `usp_SalesPrice_UpdatePrice`/`_SoftDelete`**, KHÔNG
dùng `SalesCode`/`SaleTypeName` hiển thị để build khoá (1 item/uom/nhóm giá/ngày hiệu lực có thể
có nhiều dòng khác nhau theo `SalesType`). Script: `docs/sql/GetSalesPriceList_AddSaleType.sql`
(2026-07, đổi từ `@BarCode`/`@SalesCode` cũ + thêm `SalesGroupCode`) →
`docs/sql/GetSalesPriceList_AddSalesTypeCode.sql` (2026-07, thêm `SalesTypeCode`) — chạy thủ công
theo thứ tự.

### GetSalesPriceList_Export
```
(@ItemCode nvarchar(20), @ItemName nvarchar(500), @SaleType nvarchar(50), @SalesGroup nvarchar(20), @isCheck int)
```
Giống `GetSalesPriceList` không phân trang — export Excel. `BarcodeNo` luôn trả `''` (đã bỏ join
Barcode).

### usp_SalesPrice_UpdatePrice
```
(@ItemNo nvarchar(20), @SalesCode nvarchar(20), @StartingDate date, @UnitOfMeasureCode nvarchar(10),
 @UnitPrice float, @Actor nvarchar(200)=NULL, @SalesType nvarchar(50)='')
```
9.1 Sửa giá in-place: định vị dòng theo composite PK + `@SalesType` (rỗng=không lọc — 1 item/uom/
nhóm giá/ngày hiệu lực có thể có nhiều dòng khác nhau theo hình thức bán hàng, thiếu điều kiện này
có thể sửa nhầm dòng), `UPDATE UnitPrice` + bump `Counter=MAX+1` cho toàn bộ dòng cùng `Pkey`. Trả
`(Ok bit, Message)`. Script: `docs/sql/SalesPrice_EditDelete.sql` →
`docs/sql/SalesPrice_EditDelete_AddSalesType.sql` (2026-07, thêm `@SalesType`).

### usp_SalesPrice_SoftDelete
```
(@ItemNo nvarchar(20), @SalesCode nvarchar(20), @StartingDate date, @UnitOfMeasureCode nvarchar(10),
 @Actor nvarchar(200)=NULL, @SalesType nvarchar(50)='')
```
9.1 Xóa mềm: định vị dòng theo composite PK + `@SalesType` (như trên), set `EndingDate='7777-07-07'`
(sentinel đã xóa) + bump `Counter` cho dòng đích và các dòng cùng `Pkey`. Trả `(Ok bit, Message)`.
Script: `docs/sql/SalesPrice_EditDelete.sql` → `docs/sql/SalesPrice_EditDelete_AddSalesType.sql`
(2026-07, thêm `@SalesType`).

### Setup_Promotion_Insert
```
(@BBY varchar(20))
```
Kích hoạt/insert CTKM theo `BBYNR` vào `OfferHeader` (offer engine chuẩn hoá) từ dữ liệu flat
`SetupPromotionHEADER`/`BUY`/`GET`/`SITE`.

### Setup_SalePrice_Get_ALL
```
(@Json nvarchar(max)='', @IsInsert bit=1)
```
Insert/update loạt giá bán vào `SalesPrice` từ payload JSON
`[{"Pkey":...,"FromDate":...,"ToDate":...,"UnitPrice":...}]`.

### SyncGetDataByTable
```
(@TableName varchar(50)='', @ColumnOrderBy varchar(50)='', @POSLastCounter bigint=0,
 @FilterColumn varchar(128)='', @FilterValue nvarchar(128)='')
```
SP2 trong luồng Sync Master Data — build dynamic SQL đọc toàn bộ 1 bảng (SELECT theo
`@TableName`), hỗ trợ filter theo `@FilterColumn`/`@FilterValue` khi `SyncTableList.IsByStore=1`
(xem CLAUDE.md mục "Sinh file master data .zip cho POS"). **Điều kiện Counter phải bọc ngoặc**
khi kết hợp filter (`AND`/`OR` precedence) — script cập nhật: `docs/sql/SyncGetDataByTable_AddFilter.sql`.

### SyncTable_Get
```
(@IsChange varchar(1)='A', @IsByStore int=-1, @GroupName varchar(50)='')
```
SP1 trong luồng Sync Master Data — trả metadata các bảng cần đồng bộ từ `SyncTableList`
(`@IsChange='A'` bỏ qua `@IsByStore`/`@GroupName`). Kết quả cache Redis `MD:SyncTableList` (TTL 3600s).

### usp_SaveSetupCTKMAll
```
(@BBYNR nvarchar(20) OUTPUT, @SalesType nvarchar(50), @Description nvarchar(250),
 @OfferType nvarchar(50), @Status nvarchar(10), @ValidFrom nvarchar(8), @ValidTo nvarchar(8),
 @IsVoucher bit, @BuyLinkCat nvarchar(1)='A', @GetLinkCat nvarchar(1)='A',
 @LimitQty nvarchar(50)='0', @MemberOnly bit=0, @MemberCode nvarchar(50)='',
 @Priority nvarchar(10)='1', @NumOfDays nvarchar(10)='0',
 @VoucherFrom nvarchar(8)='', @VoucherTo nvarchar(8)='',
 @VoucherValidDay nvarchar(10)='0', @VoucherLimitNumber nvarchar(10)='0',
 @Buy dbo.SetupPromotionBuyTVP READONLY, @Get dbo.SetupPromotionGetTVP READONLY,
 @Site dbo.SetupPromotionSiteTVP READONLY,
 -- Bản sửa lần 2 (2026-07-05) — tham số mới, cột DB đã có sẵn (không ALTER TABLE):
 @FromTime nvarchar(10)='', @ToTime nvarchar(10)='',
 @Mon nvarchar(5)='', @Tue nvarchar(5)='', @Wed nvarchar(5)='', @Thu nvarchar(5)='',
 @Fri nvarchar(5)='', @Sat nvarchar(5)='', @Sun nvarchar(5)='',
 @MinValue nvarchar(50)='0',
 @CheckTotalDiscount nvarchar(5)='', @TotalDiscountType nvarchar(10)='0', @TotalDiscountValue nvarchar(50)='0',
 @AllowUseAfterDay nvarchar(10)='0', @AllowUseAfterTime nvarchar(10)='')
```
Upsert đầy đủ 1 CTKM (`SetupPromotionHEADER` + replace `BUY`/`GET`/`SITE`) trong 1 transaction.
Auto-gen `BBYNR` khi tạo mới (bắt đầu `6000000001`, `UPDLOCK`/`HOLDLOCK` chống trùng). Chặn sửa
khi CTKM đã `IsApprove = 1`. **Tham số TVP `dbo.SetupPromotionBuyTVP`/`GetTVP`/`SiteTVP` không
định nghĩa trong `CentralMD.sql`** — cần tra script tạo Table Type riêng khi porting.

**Cập nhật 2026-07-05**: các cột `TIMEFROM/TIMETO/MON..SUN/MINVALUE/TOTALDISCOUNT/
TOTALDISCOUNTTYPE/TOTALDISCOUNTVALUE/ZVCDAY_AFTER/ZVCTIME_AFTER` trên `SetupPromotionHEADER`
(đã có sẵn cột vật lý — xem mục "SetupPromotionHEADER" bên trên) **nay được SP ghi/đọc thật**
(trước đó SP hard-code rỗng hoặc bỏ qua hoàn toàn) — khớp form "Cài đặt CTKM" đầy đủ field legacy.
Script cập nhật: `docs/sql/SetupPromotion_Save.sql`. SP mới `usp_SetupGroupSite_Save` (upsert
`SetupGroupSite` theo `GroupCode`, sinh `ID` qua `UPDLOCK/HOLDLOCK`) — script `docs/sql/
SetupGroupSite_Save.sql`. Cả 2 script **chưa chạy trên DB thật** — cần chạy tay trên
`RPOSMasterData` (DEV trước khi UAT/PROD).

**Cập nhật 2026-07-06**: SP mới `usp_SetupGroupItem_Save` (upsert `SetupGroupItem` theo
`GroupCode` — mirror `usp_SetupGroupSite_Save`; nhóm đã tồn tại chỉ sửa `GroupName`, KHÔNG ghi đè
`ListItemNo` — khớp hạn chế legacy `SetupGroupBuyItem`) — script `docs/sql/SetupGroupItem_Save.sql`,
**chưa chạy trên DB thật** — cần chạy tay trên `RPOSMasterData` (DEV trước). Dùng bởi modal "Cài
đặt nhóm sản phẩm" khi dòng Buy/Get chọn Loại = "Nhóm SP".

### usp_SetupCoupon_CheckCodesExist
```
(@Codes dbo.CouponCodeTVP READONLY)
```
Kiểm tra danh sách mã coupon đã tồn tại trong `CpnVchBOMCodeIssue` (dùng khi import mã mới).

### usp_SetupCoupon_Delete
```
(@ItemNo nvarchar(20))
```
Xoá coupon issue-type (`CpnVchBOMIssueRule` + `CpnVchBOMCodeIssue` theo `ItemNo`); chặn nếu
không tìm thấy `CpnVchBOMIssueRule`.

### usp_SetupCoupon_GetCodes
```
(@ItemNo nvarchar(20), @PageSize int, @PageNumber int)
```
Danh sách mã đã phát hành (`CpnVchBOMCodeIssue`) theo `ItemNo`, phân trang, `Total` qua
`COUNT(*) OVER()`.

### usp_SetupCoupon_GetDetail
```
(@ItemNo nvarchar(20))
```
Chi tiết coupon: RS1 = header (`CpnVchBOMHeader` join `CpnVchBOMIssueRule`).

### usp_SetupCoupon_GetList
```
(@IssueType varchar(20)='', @ItemNo nvarchar(20)='', @Description nvarchar(300)='',
 @Status varchar(10)='-1', @PageSize int, @PageNumber int)
```
Danh sách coupon issue-type (CTE `Data` từ `CpnVchBOMHeader`), `@Status`: `-1`=tất cả,
`1`=hiệu lực, `0`=hết hiệu lực (so với `@today`).

### usp_SetupCoupon_SaveAdvanced
```
(@ItemNo nvarchar(20), @Description nvarchar(300), @ArticleType nvarchar(10), @UOM nvarchar(10),
 @IssueType varchar(20), @CpnVchType varchar(10), @DiscountType int, @DiscountValue decimal(38,20),
 @MaxAmount decimal(38,20), @StartingDate datetime, @EndingDate datetime, @Blocked bit,
 @IsMultiUse bit, @IsCheckAPI bit, @LimitQty int, @LimitQtyUsed int, @SaleType varchar(50),
 @StoreGroupCode varchar(50), @OutItemNo nvarchar(20) OUTPUT)
```
Upsert `CpnVchBOMHeader` (field nâng cao) + `CpnVchBOMIssueRule`. Auto-gen `ItemNo` dạng
`C{số}` bắt đầu `C70000001` khi tạo mới. Đồng thời UPDATE `Value`/`VoucherType` (từ
`@DiscountValue`/`@CpnVchType`) xuống mọi dòng `CpnVchBOMCodeIssue WHERE ItemNo=@ItemNo AND
Source='COUPON'` — 2 field này chỉ có giá trị thật sau khi SP này chạy (không phải tham số của
`usp_SetupCoupon_SaveIssue`).

### usp_SetupCoupon_SaveIssue
```
(@ItemNo nvarchar(20), @Description nvarchar(300), @ArticleType nvarchar(10), @SaleType varchar(50),
 @StoreGroupCode varchar(50), @StartingDate datetime, @EndingDate datetime, @Prefix nvarchar(50),
 @LenCode int, @IssueType varchar(20), @CharOfNumber int, @CharPosition int, @IsCheckItem bit,
 @Codes dbo.CouponCodeTVP READONLY, @Lines dbo.CouponLineTVP READONLY, @OutItemNo nvarchar(20) OUTPUT)
```
Upsert coupon issue-type: `CpnVchBOMHeader` + `CpnVchBOMIssueRule` + import `@Codes` vào
`CpnVchBOMCodeIssue` (`Source='COUPON'`, `ID` nay IDENTITY — không tự tính) + `@Lines` vào
`CpnVchBOMLine`. Auto-gen `ItemNo` dạng `C7...` khi tạo mới. Insert Codes lần đầu điền đủ
`ActicleNo(=ItemNo)/ActicleType/Validity_From_Date/Expiry_Date/Voucher_Currency('VND')/
CompanyCode('WCM')/Status('SOLD')` để POS.Api check/redeem được (xem `usp_Voucher_GetByCode/
Redeem`); đồng thời UPDATE không điều kiện các field này (trừ `Status/Return/Enabled`) cho mọi
mã ĐÃ tồn tại của `ItemNo`, chạy mỗi lần Lưu — giữ đồng bộ khi Header bị sửa sau này.

### usp_SetupPromotion_Approve
```
(@BBYNR nvarchar(20))
```
Duyệt CTKM: set `IsApprove` trên `SetupPromotionHEADER` (raise error nếu không tồn tại `BBYNR`).

### usp_SetupPromotion_UpdateStatus
```
(@BBYNR nvarchar(20), @Status nvarchar(10))
```
Update cột `STATUS` trên `SetupPromotionHEADER` theo `BBYNR`.

### usp_SetupSalePrice_Save
```
(@Lines dbo.SetupSalePriceLineTVP READONLY, @Actor nvarchar(200)=NULL)
```
Ghi loạt giá vào `SalesPrice`. `Counter` mới = `MAX(Counter) WHERE YEAR(EndingDate) <> 7777` + 1
(convention: `EndingDate` năm `7777` = giá vô thời hạn, không tính vào max).

### usp_SetupVoucher_CheckCodesExist
```
(@Codes dbo.CouponCodeTVP READONLY)
```
Kiểm tra danh sách mã đã tồn tại trong `CpnVchBOMCodeIssue` (toàn bảng, `Code` unique toàn cục —
dùng khi phát hành voucher Auto/Import). Script: `docs/sql/SetupVoucher_SaveIssue.sql`.

### usp_SetupVoucher_Delete
```
(@ItemNo nvarchar(20))
```
Xoá cascade `CpnVchBOMCodeIssue` (Source='VOUCHER') + `CpnVchBOMLine` + `CpnVchBOMHeader` theo `ItemNo`.
Chặn nếu không tồn tại HOẶC nếu có bất kỳ mã (Source='VOUCHER') đã `Status='RDM'` (đã redeem) →
trả `Deleted=0, Message='Voucher đã được sử dụng'`. Trường hợp này muốn ngừng dùng voucher thì
vào trang Xem (`/promotion/vouchers/issue?id=...`) bật "Khóa (Blocked)" thay vì xóa.

### usp_SetupVoucher_GetCodes
```
(@ItemNo nvarchar(20), @PageSize int, @PageNumber int)
```
Danh sách mã voucher đã phát hành (`CpnVchBOMCodeIssue WHERE ItemNo=@ItemNo AND Source='VOUCHER'`),
gồm `Status`/`AmountUsed`/`OrderUsed` (chụp trạng thái redeem), phân trang, `Total` qua `COUNT(*) OVER()`.
Script: `docs/sql/SetupVoucher_SaveIssue.sql`.

### usp_SetupVoucher_UpdateBlocked
```
(@ItemNo nvarchar(20), @Blocked bit)
```
Cập nhật RIÊNG cột `Blocked` trên `CpnVchBOMHeader` (+ `LastDateModified`) — dùng ở trang Xem voucher
(`/promotion/vouchers/issue?id=...`): sau khi phát hành, mọi field khác đã khóa, chỉ còn cho phép
khóa/mở khóa voucher. Trả `(Ok bit, Message)`. Script: `docs/sql/SetupVoucher_UpdateBlocked.sql`.

### usp_SetupVoucher_GetDetail
```
(@ItemNo nvarchar(20))
```
Chi tiết voucher: RS1 = header từ `CpnVchBOMHeader` (map `CouponCode` → `SerialNo`) + `QuantityCode`
(= COUNT `CpnVchBOMCodeIssue WHERE Source='VOUCHER'`, để khóa field sinh mã khi > 0); RS2 = sản phẩm áp dụng.

### usp_SetupVoucher_SaveIssue
```
(@ItemNo nvarchar(20), @Serial nvarchar(50), @ItemName nvarchar(300), @ArticleType nvarchar(10),
 @UnitOfMeasure nvarchar(10), @DiscountType int, @DiscountValue decimal(38,20),
 @ValueOfVoucher decimal(38,20), @MaxAmount decimal(38,20), @LimitQty int, @IsCheckItem bit,
 @Blocked bit, @StartingDate datetime, @EndingDate datetime,
 @Codes dbo.CouponCodeTVP READONLY, @Lines dbo.VoucherLineTVP READONLY, @OutItemNo nvarchar(20) OUTPUT)
```
**Phát hành voucher**: upsert `CpnVchBOMHeader` (số thuần seed `70000001`, **KHÔNG** ghi
`CpnVchBOMIssueRule`) + insert `@Codes` vào `CpnVchBOMCodeIssue` (`Source='VOUCHER'`, lần đầu điền đủ
`Status='SOLD'`/`Value`/`VoucherType`/`Voucher_Currency='VND'`/`CompanyCode='WCM'` để POS.Api
check/redeem) + replace `@Lines` vào `CpnVchBOMLine` khi `@IsCheckItem=0`. Serial trùng → `THROW`.
Script: `docs/sql/SetupVoucher_SaveIssue.sql`.

### usp_SetupVoucher_GetList
```
(@ItemNo nvarchar(20)='', @ItemName nvarchar(300)='', @Serial nvarchar(50)='',
 @ArticleType nvarchar(10)='', @Status varchar(10)='-1', @PageSize int, @PageNumber int)
```
Danh sách voucher chuẩn: CTE `Data` từ `CpnVchBOMHeader` **WHERE NOT EXISTS** dòng tương ứng
trong `CpnVchBOMIssueRule` (phân biệt voucher chuẩn vs coupon issue-type). `Status` tính từ
`Blocked` + so `EndingDate` với `@today`.

### usp_SetupVoucher_Save
```
(@ItemNo nvarchar(20), @Serial nvarchar(50), @ItemName nvarchar(300), @ArticleType nvarchar(10),
 @UnitOfMeasure nvarchar(10), @DiscountType int, @DiscountValue decimal(38,20),
 @ValueOfVoucher decimal(38,20), @MaxAmount decimal(38,20), @LimitQty int, @IsCheckItem bit,
 @Blocked bit, @StartingDate datetime, @EndingDate datetime,
 @Lines dbo.VoucherLineTVP READONLY, @Actor nvarchar(100))
```
Upsert `CpnVchBOMHeader` (voucher chuẩn) + validate `@Serial` (CouponCode) không trùng.
`@IsCheckItem=1` → tổng bill (không cần `@Lines`); `=0` → theo sản phẩm (`@Lines` → `CpnVchBOMLine`).

### usp_SpecialCombo_Delete
```
(@Code nvarchar(50))
```
Xoá combo: `SpecialComboLine` + `SpecialComboStore` + `SpecialComboHeader` theo `Code`, trong 1 transaction.

### usp_SpecialCombo_GetDetail
```
(@Code nvarchar(50))
```
Chi tiết combo: RS1 = `SpecialComboHeader` theo `Code`.

### usp_SpecialCombo_GetList
```
(@StoreNo nvarchar(50)='', @SalesType nvarchar(50)='', @MemberType nvarchar(50)='',
 @Status nvarchar(10)='-1', @TextSearch nvarchar(250)='', @PageSize int, @PageNumber int)
```
Danh sách combo (`SpecialComboHeader`), `@Status`: `-1`=tất cả, `1`=đang áp dụng, `0`=ngưng.

### usp_SpecialCombo_Save
```
(@Code nvarchar(50), @SalesType nvarchar(50), @Name nvarchar(250), @ComboQuantity decimal(18,3),
 @Amount decimal(18,3), @IsMember bit, @MemberCode nvarchar(50), @FromDate datetime, @ToDate datetime,
 @IsEnable bit, @Actor nvarchar(100), @Lines dbo.SpecialComboLineTVP READONLY,
 @Stores dbo.SpecialComboStoreTVP READONLY)
```
Upsert `SpecialComboHeader` + replace `SpecialComboLine`/`SpecialComboStore`. `@Code` do
repository tự sinh khi tạo mới.

### usp_SpecialCombo_UpdateStatus
```
(@Code nvarchar(50), @IsEnable bit, @Actor nvarchar(100)='')
```
Update `IsEnable`/`UpdatedDate`/`UpdatedBy` trên `SpecialComboHeader` theo `Code`.

### usp_Voucher_Create
```
(@Code varchar(50), @ActicleNo varchar(50)=NULL, @ActicleType varchar(20)=NULL,
 @Value varchar(50)=NULL, @Voucher_Currency varchar(10)=NULL, @Validity_From_Date varchar(20)=NULL,
 @Expiry_Date varchar(20)=NULL, @CompanyCode varchar(20)=NULL, @Partner varchar(50)=NULL,
 @PhoneNumber varchar(20)=NULL, @VoucherType varchar(50)=NULL)
```
SAP Internal Voucher — tạo mới **idempotent** vào `CpnVchBOMCodeIssue` (`Source='SAP'`,
`ItemNo=@ActicleNo` mirror, `Status='SOLD'`), `UPDLOCK/HOLDLOCK` chống race. Nếu `Code` đã tồn
tại thì KHÔNG insert lại (không lỗi). Nếu `Code` đã tồn tại ở **Source khác** (Coupon) → `THROW`
lỗi nghiệp vụ rõ ràng (tránh vi phạm `UX_CpnVchBOMCodeIssue_Code` bằng SqlException thô, và
tránh trả nhầm dữ liệu Coupon làm voucher SAP). Luôn `SELECT` lại đúng 1 dòng hiện tại từ DB để
trả về (dù vừa tạo hay đã có sẵn). Đòi hỏi `ItemNo` đã mở rộng `varchar(50)` — xem
`docs/sql/CpnVchBOMCodeIssue_ItemNoHardening.sql` (PHẢI chạy trước). Script: `docs/sql/Voucher_Save.sql`.
Thay thế `Internal_Voucher.InsertAsync` cũ.

### usp_Voucher_GetByCode
```
(@Code varchar(50))
```
Tra 1 voucher/coupon theo `Code` trong `CpnVchBOMCodeIssue` — **không lọc theo `Source`** (`Code`
đã unique toàn bảng), nên nhận diện được cả mã Coupon (POS.Web phát hành) lẫn voucher SAP. Format
`Validity_From_Date`/`Expiry_Date` kiểu `dd/MM/yyyy`. Script: `docs/sql/Voucher_Read.sql`. Thay
thế `Internal_Voucher.GetByVoucherNumberAsync` cũ.

### usp_Voucher_Redeem
```
(@Lines dbo.VoucherRedeemTVP READONLY, @OrderNo nvarchar(50), @RequiredVoucherType varchar(50)=NULL)
```
Redeem hàng loạt trong 1 transaction `UPDLOCK/HOLDLOCK` trên `CpnVchBOMCodeIssue` — **không lọc
theo `Source`**, áp dụng chung cho cả Coupon lẫn SAP Voucher (POS.Api và POS.Web liên thông nhau):
check đủ số lượng → tất cả `Status='SOLD'` → đúng `VoucherType` nếu có `@RequiredVoucherType` →
`0 ≤ AmountRedeem ≤ Value` từng dòng (`Value IS NULL` cũng bị chặn — coupon chưa chạy
`usp_SetupCoupon_SaveAdvanced` thì `Value` NULL, phải chặn tường minh vì so sánh với NULL luôn
UNKNOWN) → UPDATE `Status='RDM', AmountUsed, OrderUsed, Enabled=0`. `Enabled=0` là field MỚI —
đồng bộ hiển thị "Locked" ở `usp_SetupCoupon_GetCodes` (POS.Web) khi mã bị redeem qua POS.Api.
Trả 2 result set: `(Success, Message)` rồi danh sách voucher sau khi cập nhật (rỗng nếu thất
bại). Lỗi nghiệp vụ KHÔNG throw (trả `Success=0` + message tiếng Việt); chỉ lỗi hệ thống mới
`THROW`. Script: `docs/sql/Voucher_Save.sql`. Thay thế `Internal_Voucher.RedeemVouchersAsync` cũ.

---

## Table-Valued Parameters (TVP) được tham chiếu nhưng KHÔNG định nghĩa trong `CentralMD.sql`

Các SP ở trên dùng `READONLY` param kiểu `dbo.*TVP` — script `CREATE TYPE` tương ứng **không có
trong `CentralMD.sql`** (định nghĩa ở migration/script khác). Khi cần port sang Dapper, tra tên
sau để tìm script gốc hoặc suy ra cấu trúc từ cách SP dùng chúng (`SELECT ... FROM @Param`):

- `dbo.CouponCodeTVP` — dùng trong `usp_SetupCoupon_CheckCodesExist`, `usp_SetupCoupon_SaveIssue` (cột `Code`)
- `dbo.CouponLineTVP` — dùng trong `usp_SetupCoupon_SaveIssue`
- `dbo.SetupPromotionBuyTVP` / `GetTVP` / `SiteTVP` — dùng trong `usp_SaveSetupCTKMAll`
- `dbo.SetupSalePriceLineTVP` — dùng trong `usp_SetupSalePrice_Save`
- `dbo.VoucherLineTVP` — dùng trong `usp_SetupVoucher_Save`, `usp_SetupVoucher_SaveIssue`
- `dbo.SpecialComboLineTVP` / `SpecialComboStoreTVP` — dùng trong `usp_SpecialCombo_Save`
- `dbo.VoucherRedeemTVP` (cột `Code`, `AmountRedeem`) — dùng trong `usp_Voucher_Redeem`, định
  nghĩa trong `docs/sql/Voucher_Save.sql`

---

## Cập nhật tài liệu này

Khi `docs/sql/database/CentralMD.sql` (hoặc script DB khác) thay đổi (thêm/sửa bảng, cột, SP):

1. Đọc lại phần script bị thay đổi.
2. Cập nhật đúng mục tương ứng trong file này (tên bảng/cột/kiểu dữ liệu/PK — **không chép
   nguyên `CREATE TABLE`**, chỉ cần cột + kiểu + nullable + ghi chú PK/FK).
3. Nếu thêm bảng mới hoàn toàn → thêm 1 mục ở đúng nhóm domain trong Mục lục + phần chi tiết.
4. Nếu là SP mới → thêm vào mục "Stored Procedures" theo cùng khuôn (tên + tham số + mục đích).

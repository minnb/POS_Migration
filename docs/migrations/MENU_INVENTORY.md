# MENU_INVENTORY.md — Kiểm kê Menu/Chức năng (Legacy VCM.BLUEPOS)

> **Trạng thái**: Tài liệu kiểm kê (inventory). **Chưa migrate code nào.** Nguồn:
> `src/legacy/VCM.BLUEPOS/VCM.BLUEPOS.csproj` (danh sách 35 Controller) +
> `[DisplayName("...")]` attribute trên từng action method. Trích dẫn `file:dòng` cho mọi mục.
>
> **Giới hạn quan trọng (đọc trước khi dùng)**: Menu thực tế của VCM.BLUEPOS là **hoàn toàn
> DB-driven** (bảng `dbo.Menu` + `dbo.MenuRole` trong `CentralMDPartner`, quản lý qua màn hình
> `RoleController.MngMenu`). Script `src/legacy/Database/CentralMD.sql` chỉ có `CREATE TABLE`
> cho `Menu`/`MenuRole`, **không có dữ liệu seed (INSERT)** — nghĩa là **cây phân cấp thực tế,
> thứ tự hiển thị (`Orderby`), tên menu chính xác hiển thị cho user, và menu nào đang
> `Status=1` (active)** không nằm trong source control và **không thể lấy được chỉ bằng cách
> đọc code**. Danh sách dưới đây được suy ra từ `[DisplayName]` — cùng cơ chế mà
> `BaseController.GetListController()` dùng để dựng bảng phân quyền theo Controller/Action
> (`src/legacy/VCM.BLUEPOS/Controllers/BaseController.cs:170-202`) — đây là danh sách **đầy đủ
> nhất có thể lấy từ source** cho "chức năng nào tồn tại", nhưng **không** phản ánh chính xác
> 100% cây cha-con / thứ tự / tên hiển thị thật trên UI (nằm ở dữ liệu DB). Khi port 1 menu cụ
> thể, cần đối chiếu thêm dữ liệu bảng `Menu` thật (nếu có quyền truy vấn DB legacy) trước khi
> chốt thiết kế navigation mới.

---

## 1. Cơ chế Menu trong VCM.BLUEPOS

- **Bảng dữ liệu**: `dbo.Menu` (`ID, MenuName, Controller, Action, Status, IsAction, Icon,
  ParentMenu, Orderby, CreatedDate, CreatedUser, LastupdateDate, LastUpdateUser`) — tự tham
  chiếu qua `ParentMenu` (schema: `src/legacy/Database/CentralMD.sql:1961-1979`; EF entity:
  `src/legacy/VCM.BLUEPOS.Data/EF/Central/Menu.cs`). `dbo.MenuRole` (`ID, MenuID, RoleCode,
  Status, ...`) map menu ↔ role (schema: `CentralMD.sql:1986-1998`; EF entity: `MenuRole.cs`).
- **3 cấp phân cấp tại runtime** (`Views/Shared/_Header.cshtml:24,179,206`): cấp 1 =
  `ParentMenu == 0` (tab ngang trên cùng), cấp 2 = `ParentMenu == {ID cấp 1}` (menu chính), cấp 3
  = `ParentMenu == {ID cấp 2}` (submenu dropdown). `IsAction = true` đánh dấu 1 dòng `Menu` chỉ
  dùng để phân quyền nút bấm (không phải trang điều hướng) — vd cặp
  `Controller="SYS_Order", Action="SYS_ButtonAction"` bị `continue` bỏ qua khi render header
  (`_Header.cshtml:201-204`).
- **Load menu theo user**: `AccountBLO.LoadMenuByUser(userName)`
  (`src/legacy/VCM.BLUEPOS.Business/Account/AccountBLO.cs:50-53`) →
  `AccountData.LoadMenuByUser`
  (`src/legacy/VCM.BLUEPOS.Data/Account/AccountData.cs:156-183`) — join `Menus ⋈ MenuRoles ⋈
  AdminUsers` theo `RoleCode`, lọc `UserName` + `Menu.Status == true`. Kết quả gán vào static
  `MenuPermissionModel.ListMenuPermission` trong `BaseController.OnActionExecuting`
  (`BaseController.cs:146-147`) — cũng là danh sách dùng để **chặn truy cập** action không có
  trong menu của user (`BaseController.cs:149-155`).
- **Màn hình quản trị Menu**: `RoleController.MngMenu` (`[DisplayName("Quản lý menu")]`,
  `RoleController.cs:713-720`) + `_ListMenu`/`UpdateMenu`/`GetMenu`/`DeleteMenu`
  (`RoleController.cs:722-825`) — CRUD trực tiếp lên bảng `Menu` qua `AuthenBLO`/`AuthenData`
  (`LoadAllMenu`, `UpdateMenu`, `GetMenuByID`, `DeleteMenu`).
- **Nguồn "tên hiển thị" suy ra được từ code**: `[DisplayName("...")]` trên action method +
  `ControllerDataModel`/`GetListController()` (`BaseController.cs:170-202`) — đây là cơ chế
  phản chiếu (reflection) dựng bảng toàn bộ Controller/Action có gắn `DisplayName`, dùng cho màn
  hình phân quyền (`RoleController.PermUser`/`RoleGroup`). Về mặt vận hành, mỗi dòng `Menu` DB
  trỏ tới đúng 1 cặp `Controller`+`Action` — nên tập `[DisplayName]` trong code chính là **tập
  ứng viên đầy đủ** cho menu (tên hiển thị trong `[DisplayName]` thường trùng hoặc rất gần
  `MenuName` thật trong DB, nhưng không đảm bảo 100% giống hệt).

---

## 2. Danh sách đầy đủ chức năng có `[DisplayName]` (135 dòng — 130 active + 5 bị comment)

> Nhóm theo Controller (thứ tự bảng Compile trong `.csproj`). Cột **Menu Name** = nội dung
> `[DisplayName("...")]`. Trích `file:dòng` là vị trí attribute trong file gốc.

### BarcodeController
| Action | Menu Name | file:dòng |
|---|---|---|
| BarcodeList | Danh mục Barcode | BarcodeController.cs:29 |

### CampaignController
| Action | Menu Name | file:dòng |
|---|---|---|
| ListCampaign | Danh sách CTKM | CampaignController.cs:41 |

### CheckAPIController
| Action | Menu Name | file:dòng |
|---|---|---|
| CX_ViewCheckMember | Kiểm tra thành viên CX | CheckAPIController.cs:40 |
| Partner_ViewCheckVoucher | Kiểm tra voucher partner | CheckAPIController.cs:111 |
| PLG_ViewCheckCoupon | Kiểm tra coupon | CheckAPIController.cs:242 |

### CommonController
| Action | Menu Name | file:dòng |
|---|---|---|
| ScanOrder | Scan đơn hàng | CommonController.cs:423 |

### CrownXController
| Action | Menu Name | file:dòng |
|---|---|---|
| TransactionDataCrownXPOS | Báo cáo tích/tiêu CrownX/PLG | CrownXController.cs:36 |
| TransactionByMemberStamp | Báo cáo tổng hợp tích/tiêu tem | CrownXController.cs:231 |
| DetailTransactionByMemberEarnStamp | Báo cáo chi tiết tích tem | CrownXController.cs:401 |
| CheckStaffMemberRemn | Báo cáo hạn mức sử dụng của Masaner | CrownXController.cs:564 |
| ReportStaffMemberRemnList | Báo cáo đơn hàng khuyến mãi Masaner | CrownXController.cs:608 |

### HomeController
| Action | Menu Name | file:dòng |
|---|---|---|
| Index | Trang chủ | HomeController.cs:43 |

### InvoiceController
| Action | Menu Name | file:dòng |
|---|---|---|
| MngInvoice | Quản lý hóa đơn điện tử | InvoiceController.cs:68 |
| ReportInvoice | Báo cáo hóa đơn điện tử | InvoiceController.cs:1475 |
| NoseriVAT | NoseriVAT | InvoiceController.cs:1706 |
| VATNumber | Dải hóa đơn | InvoiceController.cs:1792 |
| BranchInvoice | Danh sách chi nhánh | InvoiceController.cs:1846 |
| IssueInvoice | Xuất hóa đơn điện tử | InvoiceController.cs:1988 |
| DummyOrder | Hóa đơn dummy | InvoiceController.cs:2596 |
| XMLInvoice | Tạo XML hóa đơn | InvoiceController.cs:2751 |
| SignNormal | Ký số normal | InvoiceController.cs:2785 |
| MngInvoiceTemp | Quản lý hóa đơn điện tử Temp | InvoiceController.cs:2814 |
| CustomerInfoByInvoiceMTT | KH định danh xuất hóa đơn MTT | InvoiceController.cs:3178 |
| DummyOrderMTT | Tạo hóa đơn dummy MTT | InvoiceController.cs:3583 |

### LogsDataController
| Action | Menu Name | file:dòng |
|---|---|---|
| EInvoiceAPIMappingErrorLog | Danh sách mã lỗi E-Invoice | LogsDataController.cs:37 |
| EInvoicePublishLog | Danh sách Log phát hành | LogsDataController.cs:60 |
| EInvoiceErrorsLog | Log lỗi | LogsDataController.cs:104 |
| Loyalty_CX_LogAPI | Log API CX | LogsDataController.cs:201 |
| LogAPIVoucher | Log API Voucher | LogsDataController.cs:363 |
| LogMissOrderSale | Log thiếu đơn hàng bán | LogsDataController.cs:407 |
| LogCloseSalePOS | Log kết thúc ngày tại POS | LogsDataController.cs:443 |
| LogFileSalePOS | Log File Sale tại POS | LogsDataController.cs:532 |
| LogSyncChangeTableByStore | Danh sách bảng đồng bộ | LogsDataController.cs:607 |

### MasterDataController
| Action | Menu Name | file:dòng |
|---|---|---|
| EmployeeList | Danh mục nhân viên | MasterDataController.cs:85 |
| SetupPOSList | Khai báo máy POS bán hàng | MasterDataController.cs:231 |
| StoreList | Danh mục siêu thị, cửa hàng | MasterDataController.cs:541 |
| BankList | Danh mục ngân hàng | MasterDataController.cs:881 |
| BankPOSList | Khai báo máy POS ngân hàng | MasterDataController.cs:1008 |
| POSVersionlist | Cập nhật POS Version | MasterDataController.cs:1213 |
| UpdateVoucherNumberPOS | Khai báo tỷ lệ đổi quà | MasterDataController.cs:1245 |
| ProvinceList | Danh mục tỉnh thành | MasterDataController.cs:1350 |
| SetupSalesOrderType | Khai báo Sales Order Type | MasterDataController.cs:1417 |
| BankCardTypeList | Khai báo thẻ ngân hàng | MasterDataController.cs:1481 |
| SetupEWalletList | Khai báo thanh toán ví điện tử | MasterDataController.cs:1590 |
| ChangePassWord | Đổi mật khẩu | MasterDataController.cs:1814 |
| SetupImageSlider | Khai báo Images Slider | MasterDataController.cs:1885 |
| SetupCurrencyRate | Khai báo quy đổi tiền tệ | MasterDataController.cs:2603 |
| SetupQRInformation | Khai báo QR trên hóa dơn | MasterDataController.cs:2751 |
| UserLoginPOSWeb | Danh sách User login POS Web | MasterDataController.cs:3005 |
| SetupPrintByPOSWeb | Cài đặt máy in POSWeb | MasterDataController.cs:3120 |

### MonitorPOSController
| Action | Menu Name | file:dòng |
|---|---|---|
| MonitorPOS | Trạng thái máy POS | MonitorPOSController.cs:45 |
| SignalStoreList | Máy POS bắt đầu ngày | MonitorPOSController.cs:361 |

### NotifyController
| Action | Menu Name | file:dòng |
|---|---|---|
| Notify | Quản lý thông báo | NotifyController.cs:28 |

### OrderController
| Action | Menu Name | file:dòng |
|---|---|---|
| OrderList | Danh sách đơn hàng | OrderController.cs:72 |
| OrderListWinLife | Danh sách đơn hàng WinLife | OrderController.cs:517 |

### PartnerController
| Action | Menu Name | file:dòng |
|---|---|---|
| ItemListNowFood | Danh sách sản phẩm | PartnerController.cs:108 |
| ItemListByMappingPartnerCreate | Khai báo sản phẩm | PartnerController.cs:176 |
| OrderSalesByNowFood | Danh sách đơn hàng NowFood | PartnerController.cs:394 |
| StoreHeadNowFoodByCreate | Khai báo Cửa hàng Head | PartnerController.cs:580 |
| ItemListNowFoodByCheckCup | Kiểm tra loại ly | PartnerController.cs:639 |
| ItemListNowFoodByLock | Khóa món sản phẩm | PartnerController.cs:1073 |
| UpdateItemByPartner | Cập nhật sản phẩm | PartnerController.cs:1494 |
| StorePartnerMapping | Mapping cửa hàng | PartnerController.cs:3212 |
| SetupToppingOptionPartner | Tạo Topping | PartnerController.cs:3452 |

### PriceController
| Action | Menu Name | file:dòng |
|---|---|---|
| PriceList | Danh mục bảng giá | PriceController.cs:36 |
| CreatePriceGroup | Tạo Price Group | PriceController.cs:329 |
| CreatePriceGroupV2 | Tạo Price Group V2 | PriceController.cs:365 |

### ProductController
| Action | Menu Name | file:dòng |
|---|---|---|
| ProductList | Danh mục Barcode *(lệch tên — xem ghi chú §5)* | ProductController.cs:98 |
| ProductLock | Khóa món | ProductController.cs:342 |

### PromotionController
| Action | Menu Name | file:dòng |
|---|---|---|
| PromotionList | Danh mục khuyến mãi | PromotionController.cs:44 |
| CheckPromotionList | Tra cứu khuyến mãi | PromotionController.cs:291 |

### ReportController (25 báo cáo — controller nhiều menu nhất)
| Action | Menu Name | file:dòng |
|---|---|---|
| ReportDeleteOrder | Báo cáo hủy hàng | ReportController.cs:57 |
| DetailedRevenueReport | Báo cáo doanh thu chi tiết | ReportController.cs:503 |
| PaymentOrderSalesReport | Báo cáo hình thức thanh toán | ReportController.cs:738 |
| RevenueOrderSalesByStaff | Báo cáo doanh thu theo nhân viên | ReportController.cs:956 |
| RevenueOrderSalesByStore | BC doanh thu theo cửa hàng | ReportController.cs:1145 |
| RevenueOrderSalesByMCH | BC doanh thu theo ngành hàng | ReportController.cs:1361 |
| RevenueOrderSalesDetailByMCH | BC doanh thu theo quầy hàng | ReportController.cs:1759 |
| VoucherReceiptSalesReport | BC sử dụng BNMH - Voucher | ReportController.cs:2050 |
| ReportShiftEndVM | BC kết ca VinMart | ReportController.cs:2193 |
| ReportShiftEndPLG | Báo cáo kết ca | ReportController.cs:2816 |
| PromotionDiscountValueReport | Báo cáo tổng hợp KM giảm giá | ReportController.cs:3021 |
| DetailPromotionDiscountValueReport | Báo cáo chi tiết KM giảm giá | ReportController.cs:3217 |
| PromotionOfferTypeByComboReport | Báo cáo tổng hợp KM ComBo | ReportController.cs:3473 |
| DetailPromotionOfferTypeByComboReport | Báo cáo chi tiết KM ComBo | ReportController.cs:3678 |
| ReportUsedCup | Báo cáo sử dụng ly | ReportController.cs:3893 |
| RevenueOrderSalesByProduct | BC doanh thu theo sản phẩm | ReportController.cs:4226 |
| GeneralPaymentOrderSalesReport | BC TH phương thức thanh toán | ReportController.cs:4397 |
| ReportSalesType | Báo cáo salesType | ReportController.cs:4569 |
| DetailRevenueOrderSalesReportWinLife | Báo cáo doanh thu chi tiết WinLife | ReportController.cs:4822 |
| PaymentOrderSalesReportWinLife | Báo cáo hình thức thanh toán WinLife | ReportController.cs:5030 |
| RevenueSalesReportByHourly | Báo cáo doanh thu theo giờ | ReportController.cs:5209 |
| CumulativeSalesReport | Cumulative Sales Reports | ReportController.cs:5897 |
| RevenueOrderSalesByStoreWinLife | Báo cáo doanh thu theo cửa hàng WinLife | ReportController.cs:6375 |
| ReportSalesDetailPromotion | Báo cáo chi tiết khuyến mãi | ReportController.cs:6571 |

### RewardController
| Action | Menu Name | file:dòng |
|---|---|---|
| RewardIssue | Phát hành mã dự thưởng | RewardController.cs:32 |

### RoleController
| Action | Menu Name | file:dòng |
|---|---|---|
| RoleGroup | Nhóm quyền | RoleController.cs:38 |
| PermUser | Phân quyền user | RoleController.cs:137 |
| MngMenu | Quản lý menu | RoleController.cs:713 |

### SetupCouponController
| Action | Menu Name | file:dòng |
|---|---|---|
| SetupCoupon | Cài đặt coupon | SetupCouponController.cs:35 |
| CreatedCpnVch | Tạo mã coupon/voucher | SetupCouponController.cs:698 |

### SetupItemController
| Action | Menu Name | file:dòng |
|---|---|---|
| SetupItem | Cài đặt sản phẩm | SetupItemController.cs:70 |
| SetupItemV2 | Cài đặt sản phẩm *(bản V2 — trùng tên với SetupItem)* | SetupItemController.cs:140 |
| ItemPartnerList | Cài đặt sản phẩm bán kênh NOWFOOD | SetupItemController.cs:727 |
| SetupItemPartner | Cài đặt sản phẩm bán kênh đối tác | SetupItemController.cs:789 |
| PosGroupList | Quản lý danh mục sản phẩm | SetupItemController.cs:926 |

### SetupLoyaltyController
| Action | Menu Name | file:dòng |
|---|---|---|
| SetupLoyalty | Khai báo tỷ lệ đổi quà | SetupLoyaltyController.cs:40 |
| SetupMemberEarnItem | Khai báo tỷ lệ tích tem đổi quà | SetupLoyaltyController.cs:627 |
| GiftCouponList | Báo cáo coupon đã phát hành | SetupLoyaltyController.cs:997 |

### SetupPriceController
| Action | Menu Name | file:dòng |
|---|---|---|
| SetupPrice | Cài đặt giá | SetupPriceController.cs:39 |

### SetupPromotionController
| Action | Menu Name | file:dòng |
|---|---|---|
| SetupMain | Cài đặt chương trình khuyến mãi | SetupPromotionController.cs:37 |
| SetupSpecialComboList | Khai báo Special Combo | SetupPromotionController.cs:1918 |

### StoreActivitiesController
| Action | Menu Name | file:dòng |
|---|---|---|
| ConfirmEndingDateStores | Xác nhận kết thúc ngày | StoreActivitiesController.cs:210 |
| HistoryShiftEnd | Lịch sử kết thúc ca tại POS | StoreActivitiesController.cs:665 |
| HistoryDateEndStore | Lịch sử kết thúc ngày tại POS | StoreActivitiesController.cs:786 |
| HistoryDateEnd | Lịch sử xác nhận kết thúc ngày | StoreActivitiesController.cs:974 |
| ChangeBusinessDate | Thay đổi ngày kinh doanh | StoreActivitiesController.cs:1136 |
| ITConfirmEndDateStores | IT xác nhận kết thúc ngày | StoreActivitiesController.cs:1199 |

### SyncDataController
| Action | Menu Name | file:dòng |
|---|---|---|
| SyncDataByDate | Đồng bộ dữ liệu đầu ngày | SyncDataController.cs:62 |
| SyncDataByTable | Đồng bộ dữ liệu theo bảng | SyncDataController.cs:135 |
| SyncDataBySAP | Đồng bộ dữ liệu SAP | SyncDataController.cs:201 |
| QueryScript | Query script | SyncDataController.cs:258 |
| ExcuteScript | Excute data bằng Script | SyncDataController.cs:1012 |
| SyncSalePosToCentral | Đồng bộ sale lên central | SyncDataController.cs:1416 |
| SyncDataByPOSWeb | Đồng bộ dữ liệu POS Web | SyncDataController.cs:1643 |
| SyncFileSalesPosToCentral | Đồng bộ File Sales POS To Central | SyncDataController.cs:1699 |
| MissSalePosToCentralReport | Kiểm tra đồng bộ sales POS - Central | SyncDataController.cs:1851 |

### VoucherController
| Action | Menu Name | file:dòng |
|---|---|---|
| VoucherPublished | Tra cứu V/C đã phát hành | VoucherController.cs:416 |
| VoucherCouponList | Danh mục Voucher/Coupon | VoucherController.cs:703 |

---

## 3. Chức năng bị comment (disabled — không active, liệt kê để biết lịch sử)

| Controller | Action | Menu Name (đã comment) | file:dòng |
|---|---|---|---|
| ExtraFeeController | SetupExtraFee | Cài đặt phụ thu | ExtraFeeController.cs:33 |
| PartnerController | ItemListNowFoodByCreate | Tạo sản phẩm | PartnerController.cs:228 |
| PartnerController | ItemListNowFoodByUpdate | Cập nhật sản phẩm | PartnerController.cs:1354 |
| ReportController | KPIStaffReport | BC tốc độ tính KPI nhân viên | ReportController.cs:2705 |
| RoleController | PermUserPOSWeb | Phân quyền user POSWeb | RoleController.cs:829 |

> Không port các mục này trừ khi được yêu cầu rõ ràng — trạng thái comment nghĩa là tính năng đã
> bị tắt/thay thế, cần hỏi lại trước khi coi là "cần migrate".

---

## 4. Controller KHÔNG có `[DisplayName]` (không xuất hiện trong bảng phân quyền/menu)

| Controller | Ghi chú suy đoán (cần xác nhận, KHÔNG chắc chắn) |
|---|---|
| AccountController | Xử lý login/logout — chạy trước khi có `LoginUser`, không cần entry trong menu |
| AuthenController | Tương tự Account — hỗ trợ xác thực, không phải trang điều hướng |
| HotKeyController | Có thể là API nội bộ hỗ trợ phím tắt cho 1 màn hình khác, không phải trang độc lập |
| OrderSalesPrintController | Có thể chỉ là action in hóa đơn/phiếu, được gọi từ trang Order khác (không cần menu riêng) |
| ReturnOrderSalesPrintController | Tương tự — in phiếu trả hàng, phụ thuộc trang khác |
| StoreController | Cần đọc thêm để xác nhận vai trò — không loại trừ khả năng dùng nội bộ (AJAX) cho trang khác |
| VinIDController | Cần đọc thêm để xác nhận — có thể là API tích hợp VinID gọi từ JS, không phải trang điều hướng |

> Đây là **suy đoán dựa trên tên/pattern chung**, KHÔNG phải kết luận đã verify từng dòng code.
> Trước khi port bất kỳ action nào trong các controller này, đọc lại toàn bộ action của
> controller đó để xác nhận vai trò thật (theo quy trình 6 bước trong CLAUDE.md).

---

## 5. Điểm bất thường phát hiện được (cần hỏi lại trước khi port)

1. **`ProductController.ProductList`** gắn `[DisplayName("Danh mục Barcode")]`
   (`ProductController.cs:98`) — trùng tên với `BarcodeController.BarcodeList` (mục 2). Có thể
   là copy-paste nhầm attribute khi tạo controller mới, hoặc 2 tên khác nhau cho cùng 1 khái
   niệm nghiệp vụ. **Không suy diễn** — hỏi người phụ trách nghiệp vụ thật trước khi coi 2
   action này là cùng 1 menu hay 2 menu riêng biệt.
2. **`SetupItemController.SetupItem` và `SetupItemV2`** cùng `[DisplayName("Cài đặt sản phẩm")]`
   (dòng 70 và 140) — khả năng cao `V2` là bản thay thế mới hơn còn `SetupItem` (V1) là bản cũ
   giữ lại tạm. Cần xác nhận bản nào đang thực sự được trỏ tới từ menu DB trước khi chọn port
   bản nào.
3. **`PartnerController.UpdateItemByPartner`** (dòng 1494, active) trùng tên hiển thị "Cập nhật
   sản phẩm" với bản đã bị comment `ItemListNowFoodByUpdate` (dòng 1354) — xác nhận bản active
   là bản đang dùng thật.

---

## 6. Ghi chú khi port navigation sang POS.Web (Blazor Server)

- **Không thể copy cây menu 1-1** từ `dbo.Menu` sang `NavMenu` Blazor vì dữ liệu cây thật nằm
  trong DB (không có trong source) — nếu task port yêu cầu dựng lại sidebar, cần xin export dữ
  liệu bảng `Menu`/`MenuRole` từ DB legacy thật (không đoán thứ tự/nhóm từ tên Controller).
  Chỉ khi kích thước nhỏ hoặc không quan trọng thứ tự, có thể nhóm tạm theo Controller như
  bảng ở mục 2 và xác nhận lại với người phụ trách nghiệp vụ.
- **Mapping phân quyền**: `dbo.MenuRole` (nhiều `RoleCode` tuỳ ý, không giới hạn số lượng) ≠
  mô hình 3 role cố định hiện có của `POS.Web` (`WebRoles.StoreOperator/ITOps/SystemAdmin`,
  `WebPolicies.StoreAndAbove/OpsAndAbove/AdminOnly`). Khi port 1 menu cụ thể sang `POS.Web`,
  phải **quyết định thủ công** menu đó map vào policy nào — không có script chuyển đổi tự động
  vì 2 mô hình phân quyền khác bản chất (role tuỳ ý theo DB vs. 3 policy cố định theo code).
- Dùng bảng ở mục 2 làm **danh sách ứng viên tính năng cần port**, đối chiếu với
  `docs/migrations/STATUS.md` để theo dõi tiến độ — không suy ra thứ tự ưu tiên từ tài liệu này
  (thứ tự liệt kê theo Controller, không phải mức độ quan trọng).

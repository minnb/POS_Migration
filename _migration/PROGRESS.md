# PROGRESS — VCM.BLUEPOS → POS.Web Migration

> Nguồn gốc: `_migration/INVENTORY.md`
> Cập nhật lần cuối: 2026-06-30
> Trạng thái: ⏳ TODO | 🔄 IN PROGRESS | ✅ DONE | ❌ SKIP (không port)

---

## 1. AUTHENTICATION & ACCOUNT MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 1.1 | Đăng nhập & Xác thực | `POST /Account/Login` | Trung bình | ⏳ TODO |
| 1.2 | Đăng nhập MS365/SSO | `GET /Account/LoginWithMS365` | Cao | ⏳ TODO |
| 1.3 | Đăng xuất | `GET /Account/Logout` | Thấp | ⏳ TODO |
| 1.4 | Đổi mật khẩu | `POST /Account/UpdateChangePassWord` | Trung bình | ⏳ TODO |
| 1.5 | Error Page | `GET /Account/Error` | Thấp | ⏳ TODO |

---

## 2. HOME & DASHBOARD

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 2.1 | Trang chủ Dashboard | `GET /Home/Index` | Trung bình | ⏳ TODO |
| 2.2 | Health Check Services | `POST /Home/CheckService` | Trung bình | ⏳ TODO |
| 2.3 | Task Scheduler Monitoring | `GET /Home/_ViewTaskScheduler` | Cao | ⏳ TODO |
| 2.4 | SQL Server Agent Monitoring | `GET /Home/_ViewJobAgentSQL` | Cao | ⏳ TODO |
| 2.5 | Checklist & Daily Confirmation | `GET /Home/_ViewCheckList` | Trung bình | ⏳ TODO |

---

## 3. ORDER MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 3.1 | Danh sách Đơn hàng POS | `GET /Order/OrderList` | Cao | ⏳ TODO |
| 3.2 | Chi tiết Đơn hàng | `POST /Order/GetDetailOrderList` | Cao | ⏳ TODO |
| 3.3 | Export Đơn hàng Excel | `GET /Order/ExportExcelOrderList` | Trung bình | ⏳ TODO |
| 3.4 | Cập nhật Sales Type | `POST /Order/UpdateSalesType` | Trung bình | ⏳ TODO |
| 3.5 | Đơn hàng WinLife | `GET /Order/OrderListWinLife` | Cao | ⏳ TODO |

---

## 4. INVOICE MANAGEMENT (Hóa đơn Điện tử)

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 4.1 | Quản lý Hóa đơn | `GET /Invoice/MngInvoice` | Cao | ⏳ TODO |
| 4.2 | Điều chỉnh & Thay thế Hóa đơn | `POST /Invoice/_ViewDieuChinhThayThe` | Cao | ⏳ TODO |
| 4.3 | In Hóa đơn & Gửi Email | `GET /Invoice/PrintAgainInvoice` | Trung bình | ⏳ TODO |
| 4.4 | Phát hành Hóa đơn | `GET /Invoice/IssueInvoice` | Cao | ⏳ TODO |
| 4.5 | Hóa đơn Dummy | `GET /Invoice/DummyOrder` | Trung bình | ⏳ TODO |
| 4.6 | XML & Ký số Hóa đơn | `GET /Invoice/XMLInvoice` | Cao | ⏳ TODO |
| 4.7 | Dải Hóa đơn & Noseri VAT | `GET /Invoice/VATNumber` | Trung bình | ⏳ TODO |
| 4.8 | Báo cáo Hóa đơn | `GET /Invoice/ReportInvoice` | Trung bình | ⏳ TODO |
| 4.9 | Khách hàng định danh MTT | `GET /Invoice/CustomerInfoByInvoiceMTT` | Trung bình | ⏳ TODO |

---

## 5. MASTER DATA

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 5.1 | Danh mục Nhân viên | `GET /MasterData/EmployeeList` | Trung bình | ✅ DONE |
| 5.2 | Khai báo Máy POS | `GET /MasterData/SetupPOSList` | Trung bình | ⏳ TODO |
| 5.3 | Danh mục Cửa hàng | `GET /MasterData/StoreList` | Trung bình | ⏳ TODO |
| 5.4 | Danh mục Ngân hàng | `GET /MasterData/BankList` | Thấp | ⏳ TODO |
| 5.5 | Máy POS Ngân hàng (Bank POS) | `GET /MasterData/BankPOSList` | Trung bình | ✅ DONE |
| 5.6 | POS Version Management | `GET /MasterData/POSVersionlist` | Trung bình | ⏳ TODO |
| 5.7 | Danh mục Tỉnh/Thành | `GET /MasterData/ProvinceList` | Thấp | ⏳ TODO |
| 5.8 | Sales Order Type | `GET /MasterData/SetupSalesOrderType` | Thấp | ⏳ TODO |
| 5.9 | Bank Card Type | `GET /MasterData/BankCardTypeList` | Thấp | ⏳ TODO |
| 5.10 | E-Wallet Setup | `GET /MasterData/SetupEWalletList` | Trung bình | ⏳ TODO |
| 5.11 | Images Slider Setup | `GET /MasterData/SetupImageSlider` | Trung bình | ⏳ TODO |
| 5.12 | Currency Rate Setup | `GET /MasterData/SetupCurrencyRate` | Thấp | ⏳ TODO |
| 5.13 | QR Information Setup | `GET /MasterData/SetupQRInformation` | Trung bình | ⏳ TODO |
| 5.14 | User Login POS Web | `GET /MasterData/UserLoginPOSWeb` | Trung bình | ⏳ TODO |
| 5.15 | Setup Print By POSWeb | `GET /MasterData/SetupPrintByPOSWeb` | Trung bình | ⏳ TODO |

---

## 6. PRODUCT MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 6.1 | Danh mục Barcode / Product List | `GET /Product/ProductList` | Trung bình | ✅ DONE |
| 6.2 | Tạo/Cập nhật Sản phẩm | `GET /Product/CreateArticle` | Trung bình | ✅ DONE |
| 6.3 | Export Sản phẩm | `GET /Product/ExportProductList` | Trung bình | ✅ DONE |
| 6.4 | Khóa Sản phẩm | `GET /Product/ProductLock` | Trung bình | ✅ DONE |
| 6.5 | Khóa/Mở khóa Sản phẩm GrabFood | `POST /Product/SetupLockItemByGrabFoodAPI` | Cao | ⏳ TODO |

---

## 7. PROMOTION & CAMPAIGN

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 7.1 | Danh mục Khuyến mãi | `GET /Promotion/PromotionList` | Cao | ✅ DONE |
| 7.2 | Chi tiết Khuyến mãi | `POST /Promotion/GetDetailOfferHeaderList` | Cao | ⏳ TODO |
| 7.3 | Export Khuyến mãi | `GET /Promotion/ExportExcelGetOfferHeaderList` | Trung bình | ⏳ TODO |
| 7.4 | Campaign List (CTKM) | `GET /Campaign/ListCampaign` | Cao | ⏳ TODO |
| 7.5 | Campaign CRUD & Import | `POST /Campaign/Create` | Cao | ⏳ TODO |

---

## 8. COUPON & VOUCHER MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 8.1 | Cài đặt Coupon | `GET /SetupCoupon/SetupCoupon` | Trung bình | ✅ DONE |
| 8.2 | Phát hành Coupon | `POST /SetupCoupon/SaveIssueCoupon` | Cao | ✅ DONE |
| 8.3 | Danh mục Voucher | `GET /Voucher/VoucherList` | Trung bình | ✅ DONE |
| 8.4 | Tra cứu Voucher Phát hành | `GET /Voucher/VoucherPublished` | Trung bình | ✅ DONE |

> **8.3 + 8.4 ✅ DONE** — POS.Web `/promotion/vouchers` (8.3: list + filter + Full CRUD qua dialog + item picker +
> Export Excel) và `/promotion/vouchers-published` (8.4: chọn cửa hàng bắt buộc → tra cứu CentralSales per-store +
> filter + Export). Service 3 lớp `IVoucherService`/`IVoucherPublishedService` (POS.Api tái dùng được).
> - **8.3 dùng CHUNG bảng `CpnVchBOMHeader`/`CpnVchBOMLine` với Coupon** — phân tách bằng `NOT EXISTS CpnVchBOMIssueRule`.
>   SP mới: `docs/sql/SetupVoucher_Read.sql`, `SetupVoucher_Save.sql` (TVP), `SetupVoucher_Delete.sql` (ROLLOUT §D4).
> - ⚠️ **Logic quan trọng đã port đúng:** (a) `IsCheckItem` NGƯỢC nghĩa coupon — voucher `true`=tổng bill (no line),
>   `false`=theo sản phẩm; (b) ItemNo voucher = **số thuần** seed `70000001` (SP chỉ MAX trên ItemNo thuần số,
>   bỏ mã coupon 'C...' → tránh lỗi CAST của legacy); (c) serial (CouponCode) bắt buộc duy nhất (SP kiểm, trả Ok=false).
> - **8.4 tái dùng SP có sẵn `[dbo].[GetTransCpnVchIssueList]`** trên CentralSales (routed per-store qua
>   `StoreRoutedConnectionFactory`); item picker tái dùng `GetProductListAsync`.
> - **Hoãn:** màn Line-CRUD riêng của legacy (thay bằng replace-on-save trong form); Resend-SAP (8.4).

> **8.1 + 8.2 ✅ DONE** — POS.Web `/promotion/coupons` (list + filter mã/tên/cách phát hành/hiệu lực, chip trạng thái,
> xóa khi QtyCoupon==0) và `/promotion/coupons/issue` (form phát hành Auto/Import Excel + item picker + cài đặt nâng cao
> + tab mã coupon; sửa qua `?id=`). Service 3 lớp `ICouponRepository` → `ICouponService` (POS.Api tái dùng được);
> tái dùng `ICentralMDRepository.GetProductListAsync` (6.1) cho item picker. Sinh mã Auto ở tầng Application.
> - **⚠️ Legacy dùng EF LINQ, KHÔNG có SP** (INVENTORY ghi `sp_SetupCoupon_Get` là sai). SP mới cần chạy trên
>   RPOSMasterData: `docs/sql/SetupCoupon_Read.sql`, `SetupCoupon_Save.sql`, `SetupCoupon_Delete.sql` (xem ROLLOUT §D3).
> - Bảng: `CpnVchBOMIssueRule`, `CpnVchBOMHeader`, `CpnVchBOMCodeIssue`, `CpnVchBOMLine`, `CpnVchBOMStore`.
> - **Hoãn:** file mẫu Excel sinh tối giản (1 cột `CodeCoupon`) thay vì lấy mẫu từ bảng `ExcelExampleCoupon`.

---

## 9. PRICE MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 9.1 | Danh mục Bảng giá | `GET /Price/PriceList` | Cao | ⏳ TODO |
| 9.2 | Tạo/Cập nhật Bảng giá | `GET /Price/CreatePriceList` | Cao | ⏳ TODO |
| 9.3 | Setup Giá (Bulk Import) | `GET /SetupPrice/SetupPrice` | Cao | ⏳ TODO |

---

## 10. SETUP ITEM

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 10.1 | Cài đặt Sản phẩm | `GET /SetupItem/SetupItem` | Cao | ⏳ TODO |
| 10.2 | Cài đặt Sản phẩm Đối tác | `GET /SetupItem/ItemPartnerList` | Cao | ⏳ TODO |
| 10.3 | POS Group (Danh mục sản phẩm) | `GET /SetupItem/PosGroupList` | Trung bình | ⏳ TODO |

---

## 11. SETUP PROMOTION

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 11.1 | Cài đặt CTKM | `GET /SetupPromotion/SetupMain` | Cao | ✅ DONE (P1+P2) |
| 11.2 | Special Combo | `GET /SetupPromotion/SetupSpecialComboList` | Cao | ✅ DONE |

---

## 12. SETUP LOYALTY

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 12.1 | Cài đặt Tỷ lệ Đổi Quà | `GET /SetupLoyalty/SetupLoyalty` | Trung bình | ⏳ TODO |
| 12.2 | Setup Tích Tem (Member Earn Item) | `GET /SetupLoyalty/SetupMemberEarnItem` | Trung bình | ⏳ TODO |
| 12.3 | Báo cáo Coupon Phát hành | `GET /SetupLoyalty/GiftCouponList` | Trung bình | ⏳ TODO |

---

## 13. REWARD MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 13.1 | Phát hành Mã Dự thưởng | `GET /Reward/RewardIssue` | Trung bình | ⏳ TODO |

---

## 14. EXTRA FEE

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 14.1 | Cài đặt Phí Thêm | `GET /ExtraFee/SetupExtraFee` | Trung bình | ⏳ TODO |

---

## 15. REPORT MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 15.1 | Báo cáo Hủy Hàng | `GET /Report/ReportDeleteOrder` | Cao | ⏳ TODO |
| 15.2 | Báo cáo Doanh thu Chi tiết | `GET /Report/DetailedRevenueReport` | Cao | ⏳ TODO |
| 15.3 | Báo cáo Hình thức Thanh toán | `GET /Report/PaymentOrderSalesReport` | Cao | ⏳ TODO |
| 15.4 | Báo cáo Doanh thu Theo Nhân viên | `GET /Report/RevenueOrderSalesByStaff` | Cao | ✅ DONE |
| 15.5 | Báo cáo Doanh thu Theo Cửa hàng | `GET /Report/RevenueOrderSalesByStore` | Cao | ✅ DONE |
| 15.6 | Báo cáo Doanh thu Theo Ngành hàng (MCH) | `GET /Report/RevenueOrderSalesByMCH` | Cao | ⏳ TODO |
| 15.7 | Báo cáo Sử dụng Voucher/BNMH | `GET /Report/VoucherReceiptSalesReport` | Cao | ⏳ TODO |
| 15.8 | Báo cáo Kết Ca | `GET /Report/ReportShiftEndVM` | Cao | ⏳ TODO |
| 15.9 | Báo cáo Doanh thu Theo Giờ | `GET /Report/RevenueSalesReportByHourly` | Cao | ⏳ TODO |
| 15.10 | Báo cáo Tích lũy (Cumulative Sales) | `GET /Report/CumulativeSalesReport` | Cao | ⏳ TODO |
| 15.11 | Báo cáo Chi tiết Khuyến mãi | `GET /Report/ReportSalesDetailPromotion` | Cao | ⏳ TODO |
| 15.12 | Báo cáo Giảm giá Khuyến mãi | `GET /Report/PromotionDiscountValueReport` | Cao | ⏳ TODO |
| 15.13 | Báo cáo Combo | `GET /Report/PromotionOfferTypeByComboReport` | Cao | ⏳ TODO |
| 15.14 | Báo cáo Sử dụng Ly | `GET /Report/ReportUsedCup` | Cao | ⏳ TODO |
| 15.15 | Báo cáo Sale ODoo | `GET /Report/SaleOdoo` | Cao | ⏳ TODO |
| 15.16 | Báo cáo Lỗi Đồng bộ | `GET /Report/SalesFailBusDateReport` | Cao | ⏳ TODO |
| 15.17 | Báo cáo WinLife | `GET /Report/DetailRevenueOrderSalesReportWinLife` | Cao | ⏳ TODO |
| 15.18 | Báo cáo Sales Type | `GET /Report/ReportSalesType` | Cao | ⏳ TODO |

---

## 16. BARCODE MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 16.1 | Danh mục Barcode | `GET /Barcode/BarcodeList` | Trung bình | ⏳ TODO |

---

## 17. HOT KEY SETUP

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 17.1 | Hot Key Barcode | `GET /HotKey/HotKeyTable` | Trung bình | ⏳ TODO |

---

## 18. MONITOR POS SYSTEM

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 18.1 | Trạng thái Máy POS | `GET /MonitorPOS/MonitorPOS` | Cao | ⏳ TODO |
| 18.2 | Điều khiển Service Remote | `POST /MonitorPOS/StartServiceByRemote` | Cao | ⏳ TODO |
| 18.3 | Trạng thái Khai báo Ngày Store | `GET /MonitorPOS/SignalStoreList` | Trung bình | ⏳ TODO |

---

## 19. NOTIFICATION MANAGEMENT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 19.1 | Quản lý Thông báo | `GET /Notify/Notify` | Trung bình | ⏳ TODO |

---

## 20. ROLE & PERMISSION

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 20.1 | Quản lý Nhóm Quyền | `GET /Role/RoleGroup` | Trung bình | ⏳ TODO |
| 20.2 | Phân quyền User | `GET /Role/PermUser` | Cao | ⏳ TODO |
| 20.3 | Quản lý Menu | `GET /Role/MngMenu` | Trung bình | ⏳ TODO |

---

## 21. STORE ACTIVITIES

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 21.1 | Xác nhận Kết thúc Ca / Ngày | `GET /StoreActivities/ConfirmEndingShiftStores` | Cao | ⏳ TODO |
| 21.2 | Kết thúc Ca Làm việc | `POST /StoreActivities/FinsishShift` | Cao | ⏳ TODO |
| 21.3 | Thay đổi Ngày Kinh doanh | `GET /StoreActivities/ChangeBusinessDate` | Cao | ⏳ TODO |
| 21.4 | Lịch sử Kết ca / Kết ngày | `GET /StoreActivities/HistoryShiftEnd` | Trung bình | ⏳ TODO |
| 21.5 | Giải trình Lỗi Doanh số | `POST /StoreActivities/_ViewGiaiTrinh` | Trung bình | ⏳ TODO |

---

## 22. SYNC DATA

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 22.1 | Đồng bộ Dữ liệu Đầu ngày | `GET /SyncData/SyncDataByDate` | Cao | ⏳ TODO |
| 22.2 | Đồng bộ Dữ liệu SAP | `GET /SyncData/SyncDataBySAP` | Cao | ⏳ TODO |
| 22.3 | Thực thi Script SQL | `GET /SyncData/ExcuteScript` | Cao | ⏳ TODO |
| 22.4 | Đồng bộ Sale lên Central | `GET /SyncData/SyncSalePosToCentral` | Cao | ⏳ TODO |
| 22.5 | Báo cáo Miss Sale | `GET /SyncData/MissSalePosToCentralReport` | Trung bình | ⏳ TODO |

---

## 23. PARTNER INTEGRATION

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 23.1 | Sản phẩm NowFood | `GET /Partner/ItemListNowFood` | Cao | ⏳ TODO |
| 23.2 | Đơn hàng NowFood | `GET /Partner/OrderSalesByNowFood` | Cao | ⏳ TODO |
| 23.3 | Khóa/Mở Sản phẩm GrabFood | `GET /Partner/ItemListNowFoodByLock` | Cao | ⏳ TODO |
| 23.4 | Mapping Cửa hàng GrabFood / BeFood | `GET /Partner/StorePartnerMapping` | Trung bình | ⏳ TODO |

---

## 24. LOYALTY & MEMBERSHIP

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 24.1 | Báo cáo CrownX Point | `GET /CrownX/TransactionDataCrownXPOS` | Cao | ⏳ TODO |
| 24.2 | Báo cáo Tích Tem | `GET /CrownX/TransactionByMemberStamp` | Cao | ⏳ TODO |
| 24.3 | Hạn mức Sử dụng Masaner | `GET /CrownX/CheckStaffMemberRemn` | Trung bình | ⏳ TODO |

---

## 25. LOGS & ERROR TRACKING

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 25.1 | Log E-Invoice | `GET /LogsData/EInvoicePublishLog` | Trung bình | ⏳ TODO |
| 25.2 | Log API CrownX / Voucher | `GET /LogsData/Loyalty_CX_LogAPI` | Trung bình | ⏳ TODO |
| 25.3 | Log Thiếu Đơn hàng / Kết ca / File Sale | `GET /LogsData/LogMissOrderSale` | Trung bình | ⏳ TODO |

---

## 26. CHECK API

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 26.1 | Kiểm tra Thành viên CrownX | `GET /CheckAPI/CX_ViewCheckMember` | Trung bình | ⏳ TODO |
| 26.2 | Kiểm tra Voucher & Coupon Partner | `GET /CheckAPI/Partner_ViewCheckVoucher` | Trung bình | ⏳ TODO |

---

## 27. VIN ID REPORT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 27.1 | Báo cáo VinID & Blue Points | `GET /VinID/VinIDReport` | Cao | ⏳ TODO |

---

## 28. COMMON UTILITIES

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 28.1 | Load Combobox | `GET /Common/GetComboxStoreList` | Thấp | ⏳ TODO |
| 28.2 | Scan Order | `GET /Common/ScanOrder` | Trung bình | ⏳ TODO |

---

## 29. ORDER PRINT

| # | Chức năng | Route | Độ phức tạp | Trạng thái |
|---|-----------|-------|-------------|------------|
| 29.1 | In Đơn hàng Bán | `POST /OrderSalesPrint/PrintInvoiceOrderSales` | Trung bình | ⏳ TODO |

---

## Tổng kết

| Độ phức tạp | Số lượng |
|-------------|---------|
| Thấp | 9 |
| Trung bình | 43 |
| Cao | 47 |
| **Tổng** | **99** |

| Trạng thái | Số lượng |
|-----------|---------|
| ⏳ TODO | 88 |
| 🔄 IN PROGRESS | 0 |
| ✅ DONE | 11 |
| ❌ SKIP | 0 |

### Đã hoàn thành


| # | Chức năng | POS.Web | Ghi chú |
|---|-----------|---------|---------|
| 5.1 | Danh mục Nhân viên | `/catalog/employees` | List + Filter + Export Excel (ClosedXML), server-side paging qua SP `GetEmployeeList`/`GetEmployeeList_Export`. **Ẩn cột mật khẩu** (khác legacy). CRUD chưa migrate. |
| 7.1 | Danh mục Khuyến mãi | `/promotion/offers` | List + Filter + Export Excel, auto-load trang 1, server-side paging qua SP `GetPromotionOfferHeaderList`. Service 3 lớp `IPromotionRepository`→`IPromotionService` (POS.Api tái dùng được). Modal chi tiết 6 tab (7.2) chưa làm. |
| 11.1 | Cài đặt CTKM (P1+P2) | `/promotion/setup` | List + form Header + grid Buy/Get/Site + **tab Cài đặt nâng cao** (Voucher/LimitQty/Thành viên+Hạng thẻ/Priority/Ngày-trong-tháng) + Lưu + Duyệt. SP: `docs/sql/SetupPromotion_Save.sql` (đã thêm advance — chạy lại), `SetupPromotion_ApproveAndStatus.sql`, `Setup_Promotion_Insert` (có sẵn). Hoãn: giờ/ngày-trong-tuần & AllowUseAfter (legacy ẩn / thiếu cột). |
| 11.2 | Special Combo | `/promotion/special-combo` | List + filter + form (Header + Lines gom theo GroupCode + Store ALL/multi) + Lưu replace-on-save + bật/tắt + xóa; quy tắc ≤1 item giá động. SP mới: `docs/sql/SpecialCombo_Read.sql`, `SpecialCombo_Save.sql` (2 TVP), `SpecialCombo_Status.sql` (CHẠY trên CentralMD). Service 3 lớp tái dùng cho POS.Api. Hoãn: toggle store riêng, modal item/store tách. |
| 5.5 | Máy POS Ngân hàng | `/catalog/bank-pos` | List + filter client-side + KPI (Tổng/Online/Offline) + CRUD (Create/Update/Delete + confirm) + Export Excel (ClosedXML). SP đọc: `GetBankPOSList` (@Export=2). Ghi trực tiếp SQL vào `dbo.POSTerminalBanks` (Dapper). BankPOSCode tự sinh `{StoreNo}_{BankCode}_{POSNo}`. Audit log đầy đủ. Bank dropdown cache Redis 12h (`MD:BankList`). **Hoãn:** Import Excel. |
| 15.4 | Doanh thu Theo Nhân viên | `/store/revenue-by-staff` | Preset chips + filter (store, staff free-text, ngày) + KPI (nhân viên/doanh số/hóa đơn) + MudTable client-side sort + Export Excel. SP: `[dbo].[GET_REVENUE_ORDER_SALES_BY_STAFF]` (CentralSales, timeout 300s). StoreOperator: locked store. |
| 15.5 | Doanh thu Theo Cửa hàng | `/store/revenue-by-store` | Preset chips + filter (store, ngày) + KPI (SumXxx từ row đầu) + MudTable **server-side paging** + footer "Tổng tất cả" (MudTFootRow) + Export toàn bộ. SP: `[dbo].[SP_SALES_BY_STORE_BUSSINESS_DATE]` (CentralSales, @ListStoreJson, timeout 300s). |
| 6.1 | Danh mục Sản phẩm / Barcode | `/catalog/products` | Filter (mã SP, tên SP, barcode, **thuế suất động từ `dbo.POSVATCode` cache Redis 12h**) + MudTable **server-side paging** + Export Excel (ClosedXML). SP: `[dbo].[GetProductList]` / `[GetProductList_Export]` (CentralMD, @ItemCode/@BarCode — khác tên field model). |
| 6.3 | Export Sản phẩm | `/catalog/products` | Tích hợp vào 6.1 (nút Excel dùng SP `[GetProductList_Export]`). |
| 6.2 | Tạo sản phẩm mới | `/catalog/products` | Dialog "Thêm mới" từ trang danh sách. Form: 8 field + dynamic barcode table (≥1 row, BarcodeNo phải số). INSERT `dbo.Item` + `dbo.Barcode` trong transaction (Dapper, không SP). Dropdown ArticleType/UnitOfMeasure cache Redis 12h. **UPDATE chưa implement** (không tìm thấy source route). DBA cần xác nhận tên cột `dbo.ArticleType`, `dbo.UnitOfMeasure`, `dbo.POSVATCode`. |
| 6.4 | Khóa sản phẩm | `/catalog/product-lock` | Filter (store bắt buộc, status, mã/tên SP) + MudTable server-side + chip màu trạng thái + toggle icon/bulk action. UPSERT `dbo.ItemBlock` (Pkey="{StoreNo}-{ItemNo}") trong transaction. **Central mode only** — Direct POS DB và GrabFood API (6.5) để sau. |

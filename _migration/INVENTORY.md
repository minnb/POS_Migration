# INVENTORY — VCM.BLUEPOS Legacy MVC

> Project: VCM.BLUEPOS (.NET Framework 4.6, MVC)
> Mục đích: Danh mục chức năng để port sang POS.Web (.NET 10, Blazor Server)
> Ngày tạo: 2026-06-30

## Tổng quan

Hệ thống POS legacy (VCM.BLUEPOS) là ứng dụng MVC lớn với hơn **35 Controllers** và gần **1000+ Actions**, quản lý toàn bộ quy trình bán hàng tại các cửa hàng bán lẻ, tích hợp khuyến mãi, voucher, coupon, hóa đơn điện tử, báo cáo, quản lý inventory, liên kết đối tác (GrabFood, NowFood, BeFood, ZaloFood), và hệ thống quản lý quyền người dùng.

### Thống kê

| Thông số | Giá trị |
|----------|--------|
| Tổng Controllers | 35 |
| Tổng Actions | 1000+ |
| Nhóm chức năng | 30 |
| DAL Classes | 15+ |
| Business Logic Classes | 15+ |
| Model Classes | 280+ |
| Các API tích hợp | CrownX, Partner, Campaign, NowFood, GrabFood, BeFood, ZaloFood, SAP, e-Invoice |

### Stack Legacy

- **Pattern**: ASP.NET MVC 5, .NET Framework 4.6
- **Database**: SQL Server (Central, Sales, Staging, Local schemas)
- **ORM**: Entity Framework + Custom DAL
- **Auth**: Forms Authentication, Active Directory, JWT (SSO)
- **Frontend**: jQuery, DataTables, Bootstrap, Rotativa (PDF)

---

## Chức năng

---

### 1. AUTHENTICATION & ACCOUNT MANAGEMENT

#### 1.1 Đăng nhập & Xác thực
- **Route/URL**: `POST /Account/Login`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/AccountController.cs` → `Login(LoginViewModel req)`
- **View .cshtml**: `VCM.BLUEPOS/Views/Account/Login.cshtml`
- **ViewModel/Model**: `VCM.BLUEPOS.Model/LoginViewModel.cs`
- **DAL methods + SQL**: `IAccountBLO.Login()` → SP `[dbo].[sp_Login]`
- **Phụ thuộc**: `IAccountBLO`, `IServiceLogBLO`
- **Độ phức tạp**: Trung bình

#### 1.2 Đăng nhập MS365/SSO
- **Route/URL**: `GET /Account/LoginWithMS365`, `GET /Account/ADCallback`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/AccountController.cs` → `LoginWithMS365()`, `ADCallback(token)`
- **View .cshtml**: `VCM.BLUEPOS/Views/Account/LoginWithMS365.cshtml`
- **ViewModel/Model**: JWT token payload
- **DAL methods + SQL**: Decode JWT → lookup user
- **Phụ thuộc**: `DecodeJWTHelpers`, `IServiceLogBLO`
- **Độ phức tạp**: Cao

#### 1.3 Đăng xuất
- **Route/URL**: `GET /Account/Logout`, `GET /Account/ADLogoutCallback`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/AccountController.cs` → `Logout()`, `ADLogoutCallback()`
- **View .cshtml**: —
- **ViewModel/Model**: —
- **DAL methods + SQL**: Xóa session
- **Phụ thuộc**: FormsAuthentication
- **Độ phức tạp**: Thấp

#### 1.4 Đổi mật khẩu
- **Route/URL**: `GET /Account/ChangePassWord`, `POST /Account/UpdateChangePassWord`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/AccountController.cs` → `ChangePassWord()`, `UpdateChangePassWord()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Account/ChangePassWord.cshtml`
- **ViewModel/Model**: `ChangePasswordViewModel`
- **DAL methods + SQL**: `IAccountBLO.UpdatePassword()` → SP `[dbo].[sp_UpdatePassword]`
- **Phụ thuộc**: `IAccountBLO`
- **Độ phức tạp**: Trung bình

#### 1.5 Error Page
- **Route/URL**: `GET /Account/Error`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/AccountController.cs` → `Error()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Account/Error.cshtml`
- **ViewModel/Model**: —
- **DAL methods + SQL**: —
- **Phụ thuộc**: —
- **Độ phức tạp**: Thấp

---

### 2. HOME & DASHBOARD

#### 2.1 Trang chủ Dashboard
- **Route/URL**: `GET /Home/Index`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HomeController.cs` → `Index()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Home/Index.cshtml`
- **ViewModel/Model**: `HomeViewModel`
- **DAL methods + SQL**: `ICommonBLO.GetServerStatus()`
- **Phụ thuộc**: `IAuthenBLO`, `ICommonBLO`
- **Độ phức tạp**: Trung bình

#### 2.2 Health Check Services
- **Route/URL**: `POST /Home/CheckService`, `POST /Home/CheckServiceAll`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HomeController.cs` → `CheckService()`, `CheckServiceAll()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Home/_ViewCheckService.cshtml`
- **ViewModel/Model**: `ServiceStatusModel`
- **DAL methods + SQL**: HTTP ping / TCP check
- **Phụ thuộc**: HttpClient, TcpClient
- **Độ phức tạp**: Trung bình

#### 2.3 Task Scheduler Monitoring
- **Route/URL**: `GET /Home/_ViewTaskScheduler`, `POST /Home/CheckActionTaskScheduler`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HomeController.cs` → `_ViewTaskScheduler()`, `CheckActionTaskScheduler()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Home/_ViewTaskScheduler.cshtml`
- **ViewModel/Model**: `TaskSchedulerModel`
- **DAL methods + SQL**: Windows Task Scheduler API (remote)
- **Phụ thuộc**: `Microsoft.Win32.TaskScheduler`
- **Độ phức tạp**: Cao

#### 2.4 SQL Server Agent Monitoring
- **Route/URL**: `GET /Home/_ViewJobAgentSQL`, `GET /Home/_ViewServerSQL`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HomeController.cs` → `_ViewJobAgentSQL()`, `_ViewServerSQL()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Home/_ViewJobAgentSQL.cshtml`
- **ViewModel/Model**: `SQLAgentJobModel`
- **DAL methods + SQL**: Query `msdb.dbo.sysjobs`, `msdb.dbo.sysjobhistory`
- **Phụ thuộc**: SQL Server Agent
- **Độ phức tạp**: Cao

#### 2.5 Checklist & Daily Confirmation
- **Route/URL**: `GET /Home/_ViewCheckList`, `POST /Home/CheckListUser`, `GET /Home/_ViewReportCheckList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HomeController.cs` → `_ViewCheckList()`, `CheckListUser()`, `_ViewResponseReportCheckList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Home/_ViewCheckList.cshtml`
- **ViewModel/Model**: `CheckListModel`
- **DAL methods + SQL**: `ICommonBLO.GetCheckList()`, SP `[dbo].[sp_CheckList_Get]`
- **Phụ thuộc**: `ICommonBLO`
- **Độ phức tạp**: Trung bình

---

### 3. ORDER MANAGEMENT

#### 3.1 Danh sách Đơn hàng POS
- **Route/URL**: `GET /Order/OrderList`, `POST /Order/GetOrderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderController.cs` → `OrderList()`, `GetOrderList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Order/OrderList.cshtml`
- **ViewModel/Model**: `VCM.BLUEPOS.Model/OrderListModel.cs`
- **DAL methods + SQL**: `OrderDAL.GetOrderList()` → SP `[dbo].[sp_GetOrderList]`
- **Phụ thuộc**: `IOrderBLO`, `ICommonBLO`
- **Độ phức tạp**: Cao

#### 3.2 Chi tiết Đơn hàng
- **Route/URL**: `POST /Order/GetDetailOrderList`, `POST /Order/GetDetailOrderListByPromotion`, `POST /Order/GetDetailPaymentOrderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderController.cs` → `GetDetailOrderList()`, `GetDetailOrderListByPromotion()`, `GetDetailPaymentOrderList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Order/_DetailOrder.cshtml`
- **ViewModel/Model**: `OrderDetailModel`, `OrderPromotionDetailModel`, `OrderPaymentDetailModel`
- **DAL methods + SQL**: `OrderDAL.GetOrderDetail()`, SP `[dbo].[sp_GetOrderDetail]`, `[dbo].[sp_GetOrderPaymentDetail]`
- **Phụ thuộc**: `IOrderBLO`
- **Độ phức tạp**: Cao

#### 3.3 Export Đơn hàng Excel
- **Route/URL**: `GET /Order/ExportExcelOrderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderController.cs` → `ExportExcelOrderList()`
- **View .cshtml**: —
- **ViewModel/Model**: `OrderListModel`
- **DAL methods + SQL**: `OrderDAL.GetOrderList()` → SP `[dbo].[sp_GetOrderList]`
- **Phụ thuộc**: EPPlus / NPOI
- **Độ phức tạp**: Trung bình

#### 3.4 Cập nhật Sales Type
- **Route/URL**: `POST /Order/UpdateSalesType`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderController.cs` → `UpdateSalesType()`
- **View .cshtml**: —
- **ViewModel/Model**: `UpdateSalesTypeModel`
- **DAL methods + SQL**: `OrderDAL.UpdateSalesType()` → SP `[dbo].[sp_UpdateSalesType]`
- **Phụ thuộc**: `IOrderBLO`
- **Độ phức tạp**: Trung bình

#### 3.5 Đơn hàng WinLife
- **Route/URL**: `GET /Order/OrderListWinLife`, `POST /Order/GetOrderListWinLife`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderController.cs` → `OrderListWinLife()`, `GetOrderListWinLife()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Order/OrderListWinLife.cshtml`
- **ViewModel/Model**: `WinLifeOrderModel`
- **DAL methods + SQL**: `OrderDAL.GetOrderListWinLife()` → SP `[dbo].[sp_GetOrderListWinLife]`
- **Phụ thuộc**: `IOrderBLO`
- **Độ phức tạp**: Cao

---

### 4. INVOICE MANAGEMENT (Hóa đơn Điện tử)

#### 4.1 Quản lý Hóa đơn
- **Route/URL**: `GET /Invoice/MngInvoice`, `POST /Invoice/MngInvoiceLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `MngInvoice()`, `MngInvoiceLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/MngInvoice.cshtml`
- **ViewModel/Model**: `InvoiceListModel`
- **DAL methods + SQL**: `InvoiceDAL.GetInvoiceList()` → SP `[dbo].[sp_GetInvoiceList]`
- **Phụ thuộc**: `IInvoiceBLO`, `ISalesStagingBLO`
- **Độ phức tạp**: Cao

#### 4.2 Điều chỉnh & Thay thế Hóa đơn
- **Route/URL**: `POST /Invoice/_ViewDieuChinhThayThe`, `POST /Invoice/ReplaceInvoice`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `_ViewDieuChinhThayThe()`, `ReplaceInvoice()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/_ViewDieuChinhThayThe.cshtml`
- **ViewModel/Model**: `InvoiceAdjustmentModel`
- **DAL methods + SQL**: `InvoiceDAL.ReplaceInvoice()` → SAP web service
- **Phụ thuộc**: `WSInvoicePortal` (SAP integration)
- **Độ phức tạp**: Cao

#### 4.3 In Hóa đơn & Gửi Email
- **Route/URL**: `GET /Invoice/PrintAgainInvoice`, `GET /Invoice/PrintPDF`, `POST /Invoice/SendMail`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `PrintAgainInvoice()`, `PrintHTML()`, `PrintPDF()`, `SendMail()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/PrintInvoice.cshtml`
- **ViewModel/Model**: `InvoicePrintModel`
- **DAL methods + SQL**: `InvoiceDAL.GetInvoiceDetail()`
- **Phụ thuộc**: Rotativa (PDF), SMTP
- **Độ phức tạp**: Trung bình

#### 4.4 Phát hành Hóa đơn
- **Route/URL**: `GET /Invoice/IssueInvoice`, `POST /Invoice/IssueVATInvoice`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `IssueInvoice()`, `IssueVATInvoice()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/IssueInvoice.cshtml`
- **ViewModel/Model**: `IssueInvoiceModel`
- **DAL methods + SQL**: `InvoiceDAL.IssueInvoice()` → SP `[dbo].[sp_IssueVATInvoice]`
- **Phụ thuộc**: Partner API (verify MST)
- **Độ phức tạp**: Cao

#### 4.5 Hóa đơn Dummy
- **Route/URL**: `GET /Invoice/DummyOrder`, `POST /Invoice/CreatedDummy`, `GET /Invoice/DummyOrderMTT`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `DummyOrder()`, `CreatedDummy()`, `DummyOrderMTT()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/DummyOrder.cshtml`
- **ViewModel/Model**: `DummyInvoiceModel`
- **DAL methods + SQL**: `InvoiceDAL.CreateDummy()`
- **Phụ thuộc**: `IInvoiceBLO`
- **Độ phức tạp**: Trung bình

#### 4.6 XML & Ký số Hóa đơn
- **Route/URL**: `GET /Invoice/XMLInvoice`, `GET /Invoice/SignNormal`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `XMLInvoice()`, `SignNormal()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/XMLInvoice.cshtml`
- **ViewModel/Model**: `InvoiceXMLModel`
- **DAL methods + SQL**: `InvoiceDAL.GenerateXML()`, sign via `WSInvoicePortal`
- **Phụ thuộc**: `WSInvoicePortal`
- **Độ phức tạp**: Cao

#### 4.7 Dải Hóa đơn & Noseri VAT
- **Route/URL**: `GET /Invoice/VATNumber`, `GET /Invoice/NoseriVAT`, `GET /Invoice/BranchInvoice`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `VATNumber()`, `NoseriVAT()`, `BranchInvoice()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/VATNumber.cshtml`
- **ViewModel/Model**: `VATNumberModel`
- **DAL methods + SQL**: `InvoiceDAL.GetVATNumberRange()`
- **Phụ thuộc**: `IInvoiceBLO`
- **Độ phức tạp**: Trung bình

#### 4.8 Báo cáo Hóa đơn
- **Route/URL**: `GET /Invoice/ReportInvoice`, `POST /Invoice/ReportInvoiceLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `ReportInvoice()`, `ReportInvoiceLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/ReportInvoice.cshtml`
- **ViewModel/Model**: `InvoiceReportModel`
- **DAL methods + SQL**: `InvoiceDAL.GetInvoiceReport()` → SP `[dbo].[sp_GetInvoiceReport]`
- **Phụ thuộc**: `IInvoiceBLO`
- **Độ phức tạp**: Trung bình

#### 4.9 Khách hàng định danh MTT
- **Route/URL**: `GET /Invoice/CustomerInfoByInvoiceMTT`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/InvoiceController.cs` → `CustomerInfoByInvoiceMTT()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Invoice/CustomerInfoByInvoiceMTT.cshtml`
- **ViewModel/Model**: `CustomerMTTModel`
- **DAL methods + SQL**: `InvoiceDAL.GetCustomerMTT()`
- **Phụ thuộc**: `IInvoiceBLO`
- **Độ phức tạp**: Trung bình

---

### 5. MASTER DATA

#### 5.1 Danh mục Nhân viên
- **Route/URL**: `GET /MasterData/EmployeeList`, `POST /MasterData/GetEmployeeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `EmployeeList()`, `GetEmployeeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/EmployeeList.cshtml`
- **ViewModel/Model**: `EmployeeModel`
- **DAL methods + SQL**: `MasterDataDAL.GetEmployeeList()` → SP `[dbo].[sp_EmployeeList_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.2 Khai báo Máy POS
- **Route/URL**: `GET /MasterData/SetupPOSList`, `POST /MasterData/GetSetupPOSList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupPOSList()`, `GetSetupPOSList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupPOSList.cshtml`
- **ViewModel/Model**: `SetupPOSModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SetupPOS_Get]`, `[dbo].[sp_SetupPOS_Save]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.3 Danh mục Cửa hàng
- **Route/URL**: `GET /MasterData/StoreList`, `POST /MasterData/GetStoreList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `StoreList()`, `GetStoreList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/StoreList.cshtml`
- **ViewModel/Model**: `StoreModel`
- **DAL methods + SQL**: SP `[dbo].[sp_StoreList_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.4 Danh mục Ngân hàng
- **Route/URL**: `GET /MasterData/BankList`, `POST /MasterData/GetBankList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `BankList()`, `GetBankList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/BankList.cshtml`
- **ViewModel/Model**: `BankModel`
- **DAL methods + SQL**: SP `[dbo].[sp_BankList_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Thấp

#### 5.5 Máy POS Ngân hàng (Bank POS)
- **Route/URL**: `GET /MasterData/BankPOSList`, `POST /MasterData/GetBankPOSList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `BankPOSList()`, `GetBankPOSList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/BankPOSList.cshtml`
- **ViewModel/Model**: `BankPOSModel`
- **DAL methods + SQL**: SP `[dbo].[sp_BankPOSList_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.6 POS Version Management
- **Route/URL**: `GET /MasterData/POSVersionlist`, `POST /MasterData/GetPOSVersionList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `POSVersionlist()`, `GetPOSVersionList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/POSVersionlist.cshtml`
- **ViewModel/Model**: `POSVersionModel`
- **DAL methods + SQL**: SP `[dbo].[sp_POSVersion_Get]`
- **Phụ thuộc**: `IMasterDataBLO`, file upload
- **Độ phức tạp**: Trung bình

#### 5.7 Danh mục Tỉnh/Thành
- **Route/URL**: `GET /MasterData/ProvinceList`, `POST /MasterData/GetProvinceList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `ProvinceList()`, `GetProvinceList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/ProvinceList.cshtml`
- **ViewModel/Model**: `ProvinceModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ProvinceList_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Thấp

#### 5.8 Sales Order Type
- **Route/URL**: `GET /MasterData/SetupSalesOrderType`, `POST /MasterData/GetSalesOrderTypeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupSalesOrderType()`, `GetSalesOrderTypeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupSalesOrderType.cshtml`
- **ViewModel/Model**: `SalesOrderTypeModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SalesOrderType_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Thấp

#### 5.9 Bank Card Type
- **Route/URL**: `GET /MasterData/BankCardTypeList`, `POST /MasterData/LoadBankCardTypeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `BankCardTypeList()`, `LoadBankCardTypeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/BankCardTypeList.cshtml`
- **ViewModel/Model**: `BankCardTypeModel`
- **DAL methods + SQL**: SP `[dbo].[sp_BankCardType_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Thấp

#### 5.10 E-Wallet Setup
- **Route/URL**: `GET /MasterData/SetupEWalletList`, `POST /MasterData/LoadSetupEWalletList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupEWalletList()`, `LoadSetupEWalletList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupEWalletList.cshtml`
- **ViewModel/Model**: `EWalletModel`
- **DAL methods + SQL**: SP `[dbo].[sp_EWallet_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.11 Images Slider Setup
- **Route/URL**: `GET /MasterData/SetupImageSlider`, `POST /MasterData/GetSetupImageSliderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupImageSlider()`, `GetSetupImageSliderList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupImageSlider.cshtml`
- **ViewModel/Model**: `ImageSliderModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ImageSlider_Get]`
- **Phụ thuộc**: `IMasterDataBLO`, file upload
- **Độ phức tạp**: Trung bình

#### 5.12 Currency Rate Setup
- **Route/URL**: `GET /MasterData/SetupCurrencyRate`, `POST /MasterData/GetSetupCurrencyRateList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupCurrencyRate()`, `GetSetupCurrencyRateList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupCurrencyRate.cshtml`
- **ViewModel/Model**: `CurrencyRateModel`
- **DAL methods + SQL**: SP `[dbo].[sp_CurrencyRate_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Thấp

#### 5.13 QR Information Setup
- **Route/URL**: `GET /MasterData/SetupQRInformation`, `POST /MasterData/GetQRInformationList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupQRInformation()`, `GetQRInformationList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupQRInformation.cshtml`
- **ViewModel/Model**: `QRInformationModel`
- **DAL methods + SQL**: SP `[dbo].[sp_QRInformation_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.14 User Login POS Web
- **Route/URL**: `GET /MasterData/UserLoginPOSWeb`, `POST /MasterData/UserLoginPOSWebList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `UserLoginPOSWeb()`, `UserLoginPOSWebList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/UserLoginPOSWeb.cshtml`
- **ViewModel/Model**: `UserLoginPOSWebModel`
- **DAL methods + SQL**: SP `[dbo].[sp_UserLoginPOSWeb_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

#### 5.15 Setup Print By POSWeb
- **Route/URL**: `GET /MasterData/SetupPrintByPOSWeb`, `POST /MasterData/SetupPrintByPOSWebList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MasterDataController.cs` → `SetupPrintByPOSWeb()`, `SetupPrintByPOSWebList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MasterData/SetupPrintByPOSWeb.cshtml`
- **ViewModel/Model**: `SetupPrintModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SetupPrint_Get]`
- **Phụ thuộc**: `IMasterDataBLO`
- **Độ phức tạp**: Trung bình

---

### 6. PRODUCT MANAGEMENT

#### 6.1 Danh mục Barcode / Product List
- **Route/URL**: `GET /Product/ProductList`, `POST /Product/GetProductList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ProductController.cs` → `ProductList()`, `GetProductList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Product/ProductList.cshtml`
- **ViewModel/Model**: `ProductModel`
- **DAL methods + SQL**: `ProductDAL.GetProductList()` → SP `[dbo].[sp_ProductList_Get]`
- **Phụ thuộc**: `IProductBLO`
- **Độ phức tạp**: Trung bình

#### 6.2 Tạo/Cập nhật Sản phẩm
- **Route/URL**: `GET /Product/CreateArticle`, `POST /Product/SaveArticleList`, `POST /Product/SaveBarcodeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ProductController.cs` → `CreateArticle()`, `SaveArticleList()`, `SaveBarcodeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Product/CreateArticle.cshtml`
- **ViewModel/Model**: `ArticleModel`
- **DAL methods + SQL**: SP `[dbo].[sp_Article_Save]`, `[dbo].[sp_Barcode_Save]`
- **Phụ thuộc**: `IProductBLO`
- **Độ phức tạp**: Trung bình

#### 6.3 Export Sản phẩm
- **Route/URL**: `GET /Product/ExportProductList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ProductController.cs` → `ExportProductList()`
- **View .cshtml**: —
- **ViewModel/Model**: `ProductModel`
- **DAL methods + SQL**: `ProductDAL.GetProductList()`
- **Phụ thuộc**: EPPlus / NPOI
- **Độ phức tạp**: Trung bình

#### 6.4 Khóa Sản phẩm
- **Route/URL**: `GET /Product/ProductLock`, `POST /Product/GetStoreProductLock`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ProductController.cs` → `ProductLock()`, `GetStoreProductLock()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Product/ProductLock.cshtml`
- **ViewModel/Model**: `ProductLockModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ProductLock_Get]`, `[dbo].[sp_ProductLock_Save]`
- **Phụ thuộc**: `IProductBLO`
- **Độ phức tạp**: Trung bình

#### 6.5 Khóa/Mở khóa Sản phẩm GrabFood
- **Route/URL**: `POST /Product/SetupLockItemByGrabFoodAPI`, `POST /Product/SetupLockItemGrabFoodAPIV2`, `POST /Product/SetupActiveItemByGrabFoodAPI`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ProductController.cs` → `SetupLockItemByGrabFoodAPI()`, `SetupLockItemGrabFoodAPIV2()`, `SetupActiveItemByGrabFoodAPI()`
- **View .cshtml**: —
- **ViewModel/Model**: `GrabFoodLockModel`
- **DAL methods + SQL**: GrabFood API calls
- **Phụ thuộc**: GrabFood REST API
- **Độ phức tạp**: Cao

---

### 7. PROMOTION & CAMPAIGN

#### 7.1 Danh mục Khuyến mãi
- **Route/URL**: `GET /Promotion/PromotionList`, `POST /Promotion/GetOfferHeaderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PromotionController.cs` → `PromotionList()`, `GetOfferHeaderList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Promotion/PromotionList.cshtml`
- **ViewModel/Model**: `OfferHeaderModel`
- **DAL methods + SQL**: `PromotionDAL.GetOfferHeaderList()` → SP `[dbo].[sp_OfferHeader_Get]`
- **Phụ thuộc**: `IPromotionBLO`
- **Độ phức tạp**: Cao

#### 7.2 Chi tiết Khuyến mãi
- **Route/URL**: `POST /Promotion/GetDetailOfferHeaderList`, `POST /Promotion/GetDetailOfferBuyList`, `POST /Promotion/GetDetailOfferBenefitsList`, `POST /Promotion/GetDetailOfferGetList`, `POST /Promotion/GetDetailOfferSiteList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PromotionController.cs` → `GetDetailOfferHeaderList()`, `GetDetailOfferBuyList()`, `GetDetailOfferBenefitsList()`, `GetDetailOfferGetList()`, `GetDetailOfferSiteList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Promotion/_DetailOffer*.cshtml`
- **ViewModel/Model**: `OfferDetailModel`, `OfferBuyModel`, `OfferBenefitsModel`, `OfferGetModel`, `OfferSiteModel`
- **DAL methods + SQL**: SP `[dbo].[sp_OfferBuy_Get]`, `[dbo].[sp_OfferGet_Get]`, `[dbo].[sp_OfferSite_Get]`
- **Phụ thuộc**: `IPromotionBLO`
- **Độ phức tạp**: Cao

#### 7.3 Export Khuyến mãi
- **Route/URL**: `GET /Promotion/ExportExcelGetOfferHeaderList`, `GET /Promotion/ExportToExcel_PromotionOfferBuy`, `GET /Promotion/ExportToExcel_PromotionOfferGet`, `GET /Promotion/ExportToExcel_PromotionOfferSite`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PromotionController.cs` → `ExportExcelGetOfferHeaderList()`, ...
- **View .cshtml**: —
- **ViewModel/Model**: `OfferHeaderModel`
- **DAL methods + SQL**: `PromotionDAL.GetOfferHeaderList()`
- **Phụ thuộc**: EPPlus / NPOI
- **Độ phức tạp**: Trung bình

#### 7.4 Campaign List (CTKM)
- **Route/URL**: `GET /Campaign/ListCampaign`, `POST /Campaign/GetListCampaign`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CampaignController.cs` → `ListCampaign()`, `GetListCampaign()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Campaign/ListCampaign.cshtml`
- **ViewModel/Model**: `CampaignModel`
- **DAL methods + SQL**: `CampaignBLO` → Campaign external API
- **Phụ thuộc**: Campaign REST API (3rd-party)
- **Độ phức tạp**: Cao

#### 7.5 Campaign CRUD & Import
- **Route/URL**: `POST /Campaign/Detail`, `POST /Campaign/Create`, `POST /Campaign/SyncCampaign`, `POST /Campaign/CopyCampaign`, `POST /Campaign/Import`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CampaignController.cs` → `Detail()`, `Create()`, `SyncCampaign()`, `CopyCampaign()`, `Import()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Campaign/Detail.cshtml`
- **ViewModel/Model**: `CampaignDetailModel`
- **DAL methods + SQL**: Campaign API, Excel import
- **Phụ thuộc**: Campaign REST API, EPPlus
- **Độ phức tạp**: Cao

---

### 8. COUPON & VOUCHER MANAGEMENT

#### 8.1 Cài đặt Coupon
- **Route/URL**: `GET /SetupCoupon/SetupCoupon`, `POST /SetupCoupon/SetupCouponLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupCouponController.cs` → `SetupCoupon()`, `SetupCouponLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupCoupon/SetupCoupon.cshtml`
- **ViewModel/Model**: `SetupCouponModel`
- **DAL methods + SQL**: `SetupCouponDAL.GetCouponList()` → SP `[dbo].[sp_SetupCoupon_Get]`
- **Phụ thuộc**: `ISetupCouponBLO`
- **Độ phức tạp**: Trung bình

#### 8.2 Phát hành Coupon
- **Route/URL**: `POST /SetupCoupon/SaveIssueCoupon`, `POST /SetupCoupon/SaveAdvancedCoupon`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupCouponController.cs` → `SaveIssueCoupon()`, `SaveAdvancedCoupon()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupCoupon/IssueCoupon.cshtml`
- **ViewModel/Model**: `IssueCouponModel`
- **DAL methods + SQL**: SP `[dbo].[sp_IssueCoupon_Save]`
- **Phụ thuộc**: `ISetupCouponBLO`
- **Độ phức tạp**: Cao

#### 8.3 Danh mục Voucher
- **Route/URL**: `GET /Voucher/VoucherList`, `POST /Voucher/CreateVoucher`, `POST /Voucher/UpdateVoucher`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/VoucherController.cs` → `VoucherList()`, `CreateVoucher()`, `UpdateVoucher()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Voucher/VoucherList.cshtml`
- **ViewModel/Model**: `VoucherModel`
- **DAL methods + SQL**: `VoucherDAL.GetVoucherList()` → SP `[dbo].[sp_VoucherList_Get]`
- **Phụ thuộc**: `IVoucherBLO`
- **Độ phức tạp**: Trung bình

#### 8.4 Tra cứu Voucher Phát hành
- **Route/URL**: `GET /Voucher/VoucherPublished`, `POST /Voucher/GetVoucherPublishedList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/VoucherController.cs` → `VoucherPublished()`, `GetVoucherPublishedList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Voucher/VoucherPublished.cshtml`
- **ViewModel/Model**: `VoucherPublishedModel`
- **DAL methods + SQL**: SP `[dbo].[sp_VoucherPublished_Get]`
- **Phụ thuộc**: `IVoucherBLO`
- **Độ phức tạp**: Trung bình

---

### 9. PRICE MANAGEMENT

#### 9.1 Danh mục Bảng giá
- **Route/URL**: `GET /Price/PriceList`, `POST /Price/GetPriceList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PriceController.cs` → `PriceList()`, `GetPriceList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Price/PriceList.cshtml`
- **ViewModel/Model**: `PriceModel`
- **DAL methods + SQL**: `PriceDAL.GetPriceList()` → SP `[dbo].[sp_PriceList_Get]`
- **Phụ thuộc**: `IPriceBLO`
- **Độ phức tạp**: Cao

#### 9.2 Tạo/Cập nhật Bảng giá
- **Route/URL**: `GET /Price/CreatePriceList`, `POST /Price/CreatePriceList`, `GET /Price/UpdatePriceList`, `POST /Price/UpdatePriceList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PriceController.cs` → `CreatePriceList()`, `UpdatePriceList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Price/CreatePriceList.cshtml`
- **ViewModel/Model**: `PriceCreateModel`
- **DAL methods + SQL**: SP `[dbo].[sp_Price_Save]`
- **Phụ thuộc**: `IPriceBLO`
- **Độ phức tạp**: Cao

#### 9.3 Setup Giá (Bulk Import)
- **Route/URL**: `GET /SetupPrice/SetupPrice`, `POST /SetupPrice/SearchItem`, `POST /SetupPrice/LoadDataExcelToTable`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupPriceController.cs` → `SetupPrice()`, `SearchItem()`, `LoadDataExcelToTable()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupPrice/SetupPrice.cshtml`
- **ViewModel/Model**: `SetupPriceModel`
- **DAL methods + SQL**: `SetupPriceDAL.GetPriceList()` → SP `[dbo].[sp_SetupPrice_Get]`
- **Phụ thuộc**: `ISetupPriceBLO`, EPPlus
- **Độ phức tạp**: Cao

---

### 10. SETUP ITEM (Cài đặt Sản phẩm)

#### 10.1 Cài đặt Sản phẩm
- **Route/URL**: `GET /SetupItem/SetupItem`, `GET /SetupItem/SetupItemV2`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupItemController.cs` → `SetupItem()`, `SetupItemV2()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupItem/SetupItem.cshtml`
- **ViewModel/Model**: `SetupItemModel`
- **DAL methods + SQL**: `SetupItemDAL` → SP `[dbo].[sp_SetupItem_Get]`, `[dbo].[sp_SetupItem_Save]`
- **Phụ thuộc**: `ISetupItemBLO`
- **Độ phức tạp**: Cao

#### 10.2 Cài đặt Sản phẩm Đối tác
- **Route/URL**: `GET /SetupItem/ItemPartnerList`, `GET /SetupItem/SetupItemPartner`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupItemController.cs` → `ItemPartnerList()`, `SetupItemPartner()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupItem/SetupItemPartner.cshtml`
- **ViewModel/Model**: `ItemPartnerModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ItemPartner_Get]`
- **Phụ thuộc**: NowFood, GrabFood, BeFood, ZaloFood APIs
- **Độ phức tạp**: Cao

#### 10.3 POS Group (Danh mục sản phẩm)
- **Route/URL**: `GET /SetupItem/PosGroupList`, `POST /SetupItem/CreatePosGroup`, `POST /SetupItem/UpdatePosGroup`, `POST /SetupItem/DeletePosGroup`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupItemController.cs` → `PosGroupList()`, `CreatePosGroup()`, `UpdatePosGroup()`, `DeletePosGroup()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupItem/PosGroupList.cshtml`
- **ViewModel/Model**: `PosGroupModel`
- **DAL methods + SQL**: SP `[dbo].[sp_PosGroup_Get]`, `[dbo].[sp_PosGroup_Save]`
- **Phụ thuộc**: `ISetupItemBLO`
- **Độ phức tạp**: Trung bình

---

### 11. SETUP PROMOTION

#### 11.1 Cài đặt CTKM
- **Route/URL**: `GET /SetupPromotion/SetupMain`, `POST /SetupPromotion/ListOfferHeaderLoad`, `POST /SetupPromotion/SaveSetupCTKM`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupPromotionController.cs` → `SetupMain()`, `ListOfferHeaderLoad()`, `SaveSetupCTKM()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupPromotion/SetupMain.cshtml`
- **ViewModel/Model**: `SetupPromotionModel`
- **DAL methods + SQL**: `SetupPromotionDAL` → SP `[dbo].[sp_SetupPromotion_Save]`
- **Phụ thuộc**: `ISetupPromotionBLO`
- **Độ phức tạp**: Cao

#### 11.2 Special Combo
- **Route/URL**: `GET /SetupPromotion/SetupSpecialComboList`, `POST /SetupPromotion/CreateSpecialComboV2`, `POST /SetupPromotion/UpdateSpecialComboV2`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupPromotionController.cs` → `SetupSpecialComboList()`, `CreateSpecialComboV2()`, `UpdateSpecialComboV2()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupPromotion/SetupSpecialComboList.cshtml`
- **ViewModel/Model**: `SpecialComboModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SpecialCombo_Get]`, `[dbo].[sp_SpecialCombo_Save]`
- **Phụ thuộc**: `ISetupPromotionBLO`
- **Độ phức tạp**: Cao

---

### 12. SETUP LOYALTY

#### 12.1 Cài đặt Tỷ lệ Đổi Quà
- **Route/URL**: `GET /SetupLoyalty/SetupLoyalty`, `POST /SetupLoyalty/LoadSetupLoyaltyList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupLoyaltyController.cs` → `SetupLoyalty()`, `LoadSetupLoyaltyList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupLoyalty/SetupLoyalty.cshtml`
- **ViewModel/Model**: `SetupLoyaltyModel`
- **DAL methods + SQL**: `SetupLoyaltyDAL.GetLoyaltyList()` → SP `[dbo].[sp_SetupLoyalty_Get]`
- **Phụ thuộc**: `ISetupLoyaltyBLO`
- **Độ phức tạp**: Trung bình

#### 12.2 Setup Tích Tem (Member Earn Item)
- **Route/URL**: `GET /SetupLoyalty/SetupMemberEarnItem`, `POST /SetupLoyalty/SetupMemberEarnItemList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupLoyaltyController.cs` → `SetupMemberEarnItem()`, `SetupMemberEarnItemList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupLoyalty/SetupMemberEarnItem.cshtml`
- **ViewModel/Model**: `MemberEarnItemModel`
- **DAL methods + SQL**: SP `[dbo].[sp_MemberEarnItem_Get]`
- **Phụ thuộc**: `ISetupLoyaltyBLO`
- **Độ phức tạp**: Trung bình

#### 12.3 Báo cáo Coupon Phát hành
- **Route/URL**: `GET /SetupLoyalty/GiftCouponList`, `POST /SetupLoyalty/GetGiftCodeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SetupLoyaltyController.cs` → `GiftCouponList()`, `GetGiftCodeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SetupLoyalty/GiftCouponList.cshtml`
- **ViewModel/Model**: `GiftCouponModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GiftCode_Get]`
- **Phụ thuộc**: `ISetupLoyaltyBLO`
- **Độ phức tạp**: Trung bình

---

### 13. REWARD MANAGEMENT

#### 13.1 Phát hành Mã Dự thưởng
- **Route/URL**: `GET /Reward/RewardIssue`, `POST /Reward/RewardIssueLoad`, `POST /Reward/SaveReward`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/RewardController.cs` → `RewardIssue()`, `RewardIssueLoad()`, `SaveReward()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Reward/RewardIssue.cshtml`
- **ViewModel/Model**: `RewardModel`
- **DAL methods + SQL**: `RewardDAL.GetRewardList()` → SP `[dbo].[sp_Reward_Get]`, `[dbo].[sp_Reward_Save]`
- **Phụ thuộc**: `IRewardBLO`
- **Độ phức tạp**: Trung bình

---

### 14. EXTRA FEE

#### 14.1 Cài đặt Phí Thêm
- **Route/URL**: `GET /ExtraFee/SetupExtraFee`, `POST /ExtraFee/SetupExtraFeeLoad`, `POST /ExtraFee/SaveExtraFee`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ExtraFeeController.cs` → `SetupExtraFee()`, `SetupExtraFeeLoad()`, `SaveExtraFee()`
- **View .cshtml**: `VCM.BLUEPOS/Views/ExtraFee/SetupExtraFee.cshtml`
- **ViewModel/Model**: `ExtraFeeModel`
- **DAL methods + SQL**: `ExtraFeeDAL.GetExtraFeeList()` → SP `[dbo].[sp_ExtraFee_Get]`, `[dbo].[sp_ExtraFee_Save]`
- **Phụ thuộc**: `IExtraFeeBLO`
- **Độ phức tạp**: Trung bình

---

### 15. REPORT MANAGEMENT

#### 15.1 Báo cáo Hủy Hàng
- **Route/URL**: `GET /Report/ReportDeleteOrder`, `POST /Report/GetDeleteRowTotalOrderList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `ReportDeleteOrder()`, `GetDeleteRowTotalOrderList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/ReportDeleteOrder.cshtml`
- **ViewModel/Model**: `DeleteOrderReportModel`
- **DAL methods + SQL**: `ReportDAL.GetDeleteRowTotalOrderList()` → SP `[dbo].[sp_GetDeleteRowTotalOrderList]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.2 Báo cáo Doanh thu Chi tiết
- **Route/URL**: `GET /Report/DetailedRevenueReport`, `POST /Report/GetDetailRevenueReport`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `DetailedRevenueReport()`, `GetDetailRevenueReport()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/DetailedRevenueReport.cshtml`
- **ViewModel/Model**: `DetailRevenueModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetDetailRevenueReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.3 Báo cáo Hình thức Thanh toán
- **Route/URL**: `GET /Report/PaymentOrderSalesReport`, `POST /Report/GetPaymentOrderSalesList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `PaymentOrderSalesReport()`, `GetPaymentOrderSalesList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/PaymentOrderSalesReport.cshtml`
- **ViewModel/Model**: `PaymentReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetPaymentOrderSalesList]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.4 Báo cáo Doanh thu Theo Nhân viên
- **Route/URL**: `GET /Report/RevenueOrderSalesByStaff`, `POST /Report/GetRevenueOrderSalesByStaff`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `RevenueOrderSalesByStaff()`, `GetRevenueOrderSalesByStaff()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/RevenueOrderSalesByStaff.cshtml`
- **ViewModel/Model**: `RevenueByStaffModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetRevenueOrderSalesByStaff]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.5 Báo cáo Doanh thu Theo Cửa hàng
- **Route/URL**: `GET /Report/RevenueOrderSalesByStore`, `POST /Report/GetRevenueOrderSalesByStore`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `RevenueOrderSalesByStore()`, `GetRevenueOrderSalesByStore()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/RevenueOrderSalesByStore.cshtml`
- **ViewModel/Model**: `RevenueByStoreModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetRevenueOrderSalesByStore]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.6 Báo cáo Doanh thu Theo Ngành hàng (MCH)
- **Route/URL**: `GET /Report/RevenueOrderSalesByMCH`, `POST /Report/SalesByMCHLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `RevenueOrderSalesByMCH()`, `SalesByMCHLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/RevenueOrderSalesByMCH.cshtml`
- **ViewModel/Model**: `RevenueByMCHModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetRevenueOrderSalesByMCH]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.7 Báo cáo Sử dụng Voucher/BNMH
- **Route/URL**: `GET /Report/VoucherReceiptSalesReport`, `POST /Report/GetVoucherReceiptList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `VoucherReceiptSalesReport()`, `GetVoucherReceiptList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/VoucherReceiptSalesReport.cshtml`
- **ViewModel/Model**: `VoucherReceiptReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetVoucherReceiptList]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.8 Báo cáo Kết Ca
- **Route/URL**: `GET /Report/ReportShiftEndVM`, `GET /Report/ReportShiftEndVMP`, `GET /Report/ReportShiftEndPLG`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `ReportShiftEndVM()`, `ReportShiftEndVMP()`, `ReportShiftEndPLG()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/ReportShiftEnd*.cshtml`
- **ViewModel/Model**: `ShiftEndReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ReportShiftEnd]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.9 Báo cáo Doanh thu Theo Giờ
- **Route/URL**: `GET /Report/RevenueSalesReportByHourly`, `POST /Report/GetRevenueSalesByHourly`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `RevenueSalesReportByHourly()`, `GetRevenueSalesByHourly()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/RevenueSalesReportByHourly.cshtml`
- **ViewModel/Model**: `RevenueByHourlyModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetRevenueSalesByHourly]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.10 Báo cáo Tích lũy (Cumulative Sales)
- **Route/URL**: `GET /Report/CumulativeSalesReport`, `POST /Report/GetCumulativeSalesList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `CumulativeSalesReport()`, `GetCumulativeSalesList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/CumulativeSalesReport.cshtml`
- **ViewModel/Model**: `CumulativeSalesModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetCumulativeSalesList]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.11 Báo cáo Chi tiết Khuyến mãi
- **Route/URL**: `GET /Report/ReportSalesDetailPromotion`, `POST /Report/GetSalesDetailPromotion`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `ReportSalesDetailPromotion()`, `GetSalesDetailPromotion()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/ReportSalesDetailPromotion.cshtml`
- **ViewModel/Model**: `SalesDetailPromotionModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetSalesDetailPromotion]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.12 Báo cáo Giảm giá Khuyến mãi
- **Route/URL**: `GET /Report/PromotionDiscountValueReport`, `GET /Report/DetailPromotionDiscountValueReport`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `PromotionDiscountValueReport()`, `DetailPromotionDiscountValueReport()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/PromotionDiscountValueReport.cshtml`
- **ViewModel/Model**: `PromotionDiscountModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetPromotionDiscountValueReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.13 Báo cáo Combo
- **Route/URL**: `GET /Report/PromotionOfferTypeByComboReport`, `GET /Report/DetailPromotionOfferTypeByComboReport`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `PromotionOfferTypeByComboReport()`, `DetailPromotionOfferTypeByComboReport()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/PromotionOfferTypeByComboReport.cshtml`
- **ViewModel/Model**: `ComboReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetComboReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.14 Báo cáo Sử dụng Ly
- **Route/URL**: `GET /Report/ReportUsedCup`, `POST /Report/_ViewReportCup`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `ReportUsedCup()`, `_ViewReportCup()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/ReportUsedCup.cshtml`
- **ViewModel/Model**: `CupUsageReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetCupUsageReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.15 Báo cáo Sale ODoo
- **Route/URL**: `GET /Report/SaleOdoo`, `POST /Report/SaleOdooLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `SaleOdoo()`, `SaleOdooLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/SaleOdoo.cshtml`
- **ViewModel/Model**: `SaleOdooModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetSaleOdoo]`
- **Phụ thuộc**: `IReportBLO`, ODoo integration
- **Độ phức tạp**: Cao

#### 15.16 Báo cáo Lỗi Đồng bộ
- **Route/URL**: `GET /Report/SalesFailBusDateReport`, `POST /Report/SalesFailBusDateLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `SalesFailBusDateReport()`, `SalesFailBusDateLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/SalesFailBusDateReport.cshtml`
- **ViewModel/Model**: `SalesFailReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetSalesFailBusDateReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.17 Báo cáo WinLife
- **Route/URL**: `GET /Report/DetailRevenueOrderSalesReportWinLife`, `GET /Report/PaymentOrderSalesReportWinLife`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `DetailRevenueOrderSalesReportWinLife()`, `PaymentOrderSalesReportWinLife()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/WinLifeReport.cshtml`
- **ViewModel/Model**: `WinLifeReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetWinLifeReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

#### 15.18 Báo cáo Sales Type
- **Route/URL**: `GET /Report/ReportSalesType`, `POST /Report/ReportSalesTypeLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/ReportController.cs` → `ReportSalesType()`, `ReportSalesTypeLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Report/ReportSalesType.cshtml`
- **ViewModel/Model**: `SalesTypeReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetSalesTypeReport]`
- **Phụ thuộc**: `IReportBLO`
- **Độ phức tạp**: Cao

---

### 16. BARCODE MANAGEMENT

#### 16.1 Danh mục Barcode
- **Route/URL**: `GET /Barcode/BarcodeList`, `POST /Barcode/GetBarcodeList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/BarcodeController.cs` → `BarcodeList()`, `GetBarcodeList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Barcode/BarcodeList.cshtml`
- **ViewModel/Model**: `BarcodeModel`
- **DAL methods + SQL**: `BarcodeDAL.GetBarcodeList()` → SP `[dbo].[sp_BarcodeList_Get]`
- **Phụ thuộc**: `IBarcodeBLO`
- **Độ phức tạp**: Trung bình

---

### 17. HOT KEY SETUP

#### 17.1 Hot Key Barcode
- **Route/URL**: `GET /HotKey/HotKeyTable`, `POST /HotKey/HotKeyLoad`, `POST /HotKey/InsertBarCode`, `POST /HotKey/DeleteHotKeyBarcode`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/HotKeyController.cs` → `HotKeyTable()`, `HotKeyLoad()`, `InsertBarCode()`, `DeleteHotKeyBarcode()`
- **View .cshtml**: `VCM.BLUEPOS/Views/HotKey/HotKeyTable.cshtml`
- **ViewModel/Model**: `HotKeyModel`
- **DAL methods + SQL**: `HotKeyDAL.GetHotKeyList()` → SP `[dbo].[sp_HotKey_Get]`, `[dbo].[sp_HotKey_Save]`
- **Phụ thuộc**: `IHotKeyBLO`
- **Độ phức tạp**: Trung bình

---

### 18. MONITOR POS SYSTEM

#### 18.1 Trạng thái Máy POS
- **Route/URL**: `GET /MonitorPOS/MonitorPOS`, `POST /MonitorPOS/MonitorPOSLoad`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MonitorPOSController.cs` → `MonitorPOS()`, `MonitorPOSLoad()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MonitorPOS/MonitorPOS.cshtml`
- **ViewModel/Model**: `MonitorPOSModel`
- **DAL methods + SQL**: `MonitorPOSDAL.GetPOSStatus()` → SP `[dbo].[sp_MonitorPOS_Get]`
- **Phụ thuộc**: `IMonitorPOSBLO`, Ping, WMI
- **Độ phức tạp**: Cao

#### 18.2 Điều khiển Service Remote
- **Route/URL**: `POST /MonitorPOS/StartServiceByRemote`, `POST /MonitorPOS/KillProcess`, `POST /MonitorPOS/RestartPC`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MonitorPOSController.cs` → `StartServiceByRemote()`, `KillProcess()`, `RestartPC()`
- **View .cshtml**: —
- **ViewModel/Model**: `RemoteControlModel`
- **DAL methods + SQL**: WMI remote execution
- **Phụ thuộc**: `System.Management` (WMI), WinRM
- **Độ phức tạp**: Cao

#### 18.3 Trạng thái Khai báo Ngày Store
- **Route/URL**: `GET /MonitorPOS/SignalStoreList`, `POST /MonitorPOS/LoadSignalStoreList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/MonitorPOSController.cs` → `SignalStoreList()`, `LoadSignalStoreList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/MonitorPOS/SignalStoreList.cshtml`
- **ViewModel/Model**: `SignalStoreModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SignalStore_Get]`
- **Phụ thuộc**: `IMonitorPOSBLO`
- **Độ phức tạp**: Trung bình

---

### 19. NOTIFICATION MANAGEMENT

#### 19.1 Quản lý Thông báo
- **Route/URL**: `GET /Notify/Notify`, `POST /Notify/NotifyLoad`, `POST /Notify/_SetupNotify`, `POST /Notify/ConfirmNotify`, `POST /Notify/DeleteNotify`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/NotifyController.cs` → `Notify()`, `NotifyLoad()`, `_SetupNotify()`, `ConfirmNotify()`, `DeleteNotify()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Notify/Notify.cshtml`
- **ViewModel/Model**: `NotifyModel`
- **DAL methods + SQL**: `NotifyDAL.GetNotifyList()` → SP `[dbo].[sp_Notify_Get]`, `[dbo].[sp_Notify_Save]`
- **Phụ thuộc**: `INotifyBLO`
- **Độ phức tạp**: Trung bình

---

### 20. ROLE & PERMISSION

#### 20.1 Quản lý Nhóm Quyền
- **Route/URL**: `GET /Role/RoleGroup`, `POST /Role/LoadDataRole`, `POST /Role/UpdateRole`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/RoleController.cs` → `RoleGroup()`, `LoadDataRole()`, `UpdateRole()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Role/RoleGroup.cshtml`
- **ViewModel/Model**: `RoleModel`
- **DAL methods + SQL**: `RoleDAL.GetRoleList()` → SP `[dbo].[sp_Role_Get]`, `[dbo].[sp_Role_Save]`
- **Phụ thuộc**: `IRoleBLO`
- **Độ phức tạp**: Trung bình

#### 20.2 Phân quyền User
- **Route/URL**: `GET /Role/PermUser`, `POST /Role/LoadDataPermission`, `POST /Role/UpdatePerm`, `POST /Role/UpdatePermV2`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/RoleController.cs` → `PermUser()`, `LoadDataPermission()`, `UpdatePerm()`, `UpdatePermV2()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Role/PermUser.cshtml`
- **ViewModel/Model**: `PermissionModel`
- **DAL methods + SQL**: SP `[dbo].[sp_Permission_Get]`, `[dbo].[sp_Permission_Save]`
- **Phụ thuộc**: `IRoleBLO`
- **Độ phức tạp**: Cao

#### 20.3 Quản lý Menu
- **Route/URL**: `GET /Role/MngMenu`, `POST /Role/_ListMenu`, `POST /Role/UpdateMenu`, `POST /Role/DeleteMenu`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/RoleController.cs` → `MngMenu()`, `_ListMenu()`, `UpdateMenu()`, `DeleteMenu()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Role/MngMenu.cshtml`
- **ViewModel/Model**: `MenuModel`
- **DAL methods + SQL**: SP `[dbo].[sp_Menu_Get]`, `[dbo].[sp_Menu_Save]`
- **Phụ thuộc**: `IRoleBLO`
- **Độ phức tạp**: Trung bình

---

### 21. STORE ACTIVITIES

#### 21.1 Xác nhận Kết thúc Ca / Ngày
- **Route/URL**: `GET /StoreActivities/ConfirmEndingShiftStores`, `GET /StoreActivities/ConfirmEndingDateStores`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` → `ConfirmEndingShiftStores()`, `ConfirmEndingDateStores()`
- **View .cshtml**: `VCM.BLUEPOS/Views/StoreActivities/ConfirmEndingShiftStores.cshtml`
- **ViewModel/Model**: `EndingShiftModel`
- **DAL methods + SQL**: `StoreActivitiesDAL.ConfirmEndingShift()` → SP `[dbo].[sp_ConfirmEndingShift]`
- **Phụ thuộc**: `IStoreActivitiesBLO`, Sales Staging DB
- **Độ phức tạp**: Cao

#### 21.2 Kết thúc Ca Làm việc
- **Route/URL**: `POST /StoreActivities/FinsishShift`, `POST /StoreActivities/FinsishShiftTH`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` → `FinsishShift()`, `FinsishShiftTH()`
- **View .cshtml**: —
- **ViewModel/Model**: `FinishShiftModel`
- **DAL methods + SQL**: SP `[dbo].[sp_FinishShift]`
- **Phụ thuộc**: `IStoreActivitiesBLO`
- **Độ phức tạp**: Cao

#### 21.3 Thay đổi Ngày Kinh doanh
- **Route/URL**: `GET /StoreActivities/ChangeBusinessDate`, `POST /StoreActivities/BusinessDateUpdate`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` → `ChangeBusinessDate()`, `BusinessDateUpdate()`
- **View .cshtml**: `VCM.BLUEPOS/Views/StoreActivities/ChangeBusinessDate.cshtml`
- **ViewModel/Model**: `BusinessDateModel`
- **DAL methods + SQL**: SP `[dbo].[sp_BusinessDate_Update]`
- **Phụ thuộc**: `IStoreActivitiesBLO`
- **Độ phức tạp**: Cao

#### 21.4 Lịch sử Kết ca / Kết ngày
- **Route/URL**: `GET /StoreActivities/HistoryShiftEnd`, `GET /StoreActivities/HistoryDateEndStore`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` → `HistoryShiftEnd()`, `HistoryDateEndStore()`
- **View .cshtml**: `VCM.BLUEPOS/Views/StoreActivities/HistoryShiftEnd.cshtml`
- **ViewModel/Model**: `ShiftHistoryModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ShiftHistory_Get]`
- **Phụ thuộc**: `IStoreActivitiesBLO`
- **Độ phức tạp**: Trung bình

#### 21.5 Giải trình Lỗi Doanh số
- **Route/URL**: `POST /StoreActivities/_ViewGiaiTrinh`, `POST /StoreActivities/UpdateGiaiTrinh`, `POST /StoreActivities/DeleteGiaiTrinh`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/StoreActivitiesController.cs` → `_ViewGiaiTrinh()`, `UpdateGiaiTrinh()`, `DeleteGiaiTrinh()`
- **View .cshtml**: `VCM.BLUEPOS/Views/StoreActivities/_ViewGiaiTrinh.cshtml`
- **ViewModel/Model**: `GiaiTrinhModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GiaiTrinh_Save]`
- **Phụ thuộc**: `IStoreActivitiesBLO`
- **Độ phức tạp**: Trung bình

---

### 22. SYNC DATA

#### 22.1 Đồng bộ Dữ liệu Đầu ngày
- **Route/URL**: `GET /SyncData/SyncDataByDate`, `POST /SyncData/DeleteSynDateLog`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SyncDataController.cs` → `SyncDataByDate()`, `DeleteSynDateLog()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SyncData/SyncDataByDate.cshtml`
- **ViewModel/Model**: `SyncDataModel`
- **DAL methods + SQL**: `SyncDataDAL.SyncData()` → SP `[dbo].[sp_SyncDataByDate]`
- **Phụ thuộc**: `ISyncDataBLO`
- **Độ phức tạp**: Cao

#### 22.2 Đồng bộ Dữ liệu SAP
- **Route/URL**: `GET /SyncData/SyncDataBySAP`, `POST /SyncData/GetSyncDataBySAP`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SyncDataController.cs` → `SyncDataBySAP()`, `GetSyncDataBySAP()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SyncData/SyncDataBySAP.cshtml`
- **ViewModel/Model**: `SyncSAPModel`
- **DAL methods + SQL**: SAP API calls
- **Phụ thuộc**: SAP REST API
- **Độ phức tạp**: Cao

#### 22.3 Thực thi Script SQL
- **Route/URL**: `GET /SyncData/ExcuteScript`, `POST /SyncData/ExcuteScriptToPOS`, `POST /SyncData/ExcuteScriptToServer`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SyncDataController.cs` → `ExcuteScript()`, `ExcuteScriptToPOS()`, `ExcuteScriptToServer()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SyncData/ExcuteScript.cshtml`
- **ViewModel/Model**: `ExecuteScriptModel`
- **DAL methods + SQL**: Dynamic SQL execution
- **Phụ thuộc**: `ISyncDataBLO`
- **Độ phức tạp**: Cao

#### 22.4 Đồng bộ Sale lên Central
- **Route/URL**: `GET /SyncData/SyncSalePosToCentral`, `POST /SyncData/SyncSale`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SyncDataController.cs` → `SyncSalePosToCentral()`, `SyncSale()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SyncData/SyncSalePosToCentral.cshtml`
- **ViewModel/Model**: `SyncSaleModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SyncSaleToCentral]`
- **Phụ thuộc**: `ISyncDataBLO`
- **Độ phức tạp**: Cao

#### 22.5 Báo cáo Miss Sale
- **Route/URL**: `GET /SyncData/MissSalePosToCentralReport`, `POST /SyncData/GetMissSalesPOSToCentralReport`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/SyncDataController.cs` → `MissSalePosToCentralReport()`, `GetMissSalesPOSToCentralReport()`
- **View .cshtml**: `VCM.BLUEPOS/Views/SyncData/MissSalePosToCentralReport.cshtml`
- **ViewModel/Model**: `MissSaleReportModel`
- **DAL methods + SQL**: SP `[dbo].[sp_GetMissSalesReport]`
- **Phụ thuộc**: `ISyncDataBLO`
- **Độ phức tạp**: Trung bình

---

### 23. PARTNER INTEGRATION

#### 23.1 Sản phẩm NowFood
- **Route/URL**: `GET /Partner/ItemListNowFood`, `POST /Partner/UpdateItemListNowFood`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PartnerController.cs` → `ItemListNowFood()`, `UpdateItemListNowFood()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Partner/ItemListNowFood.cshtml`
- **ViewModel/Model**: `NowFoodItemModel`
- **DAL methods + SQL**: `PartnerDAL.GetNowFoodItemList()` + NowFood API
- **Phụ thuộc**: NowFood REST API
- **Độ phức tạp**: Cao

#### 23.2 Đơn hàng NowFood
- **Route/URL**: `GET /Partner/OrderSalesByNowFood`, `POST /Partner/GetOrderSalesListByNowFood`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PartnerController.cs` → `OrderSalesByNowFood()`, `GetOrderSalesListByNowFood()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Partner/OrderSalesByNowFood.cshtml`
- **ViewModel/Model**: `NowFoodOrderModel`
- **DAL methods + SQL**: NowFood API
- **Phụ thuộc**: NowFood REST API
- **Độ phức tạp**: Cao

#### 23.3 Khóa/Mở Sản phẩm GrabFood
- **Route/URL**: `GET /Partner/ItemListNowFoodByLock`, `POST /Partner/ApiItemListNowFoodByLock`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PartnerController.cs` → `ItemListNowFoodByLock()`, `ApiItemListNowFoodByLock()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Partner/ItemListNowFoodByLock.cshtml`
- **ViewModel/Model**: `GrabFoodLockModel`
- **DAL methods + SQL**: GrabFood API
- **Phụ thuộc**: GrabFood REST API
- **Độ phức tạp**: Cao

#### 23.4 Mapping Cửa hàng GrabFood / BeFood
- **Route/URL**: `GET /Partner/StorePartnerMapping`, `POST /Partner/GetStoreMappingByGrabFood`, `POST /Partner/GetStoreMappingByBeFood`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/PartnerController.cs` → `StorePartnerMapping()`, `GetStoreMappingByGrabFood()`, `GetStoreMappingByBeFood()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Partner/StorePartnerMapping.cshtml`
- **ViewModel/Model**: `StoreMappingModel`
- **DAL methods + SQL**: SP `[dbo].[sp_StoreMappingPartner_Get]`
- **Phụ thuộc**: GrabFood API, BeFood API
- **Độ phức tạp**: Trung bình

---

### 24. LOYALTY & MEMBERSHIP

#### 24.1 Báo cáo CrownX Point
- **Route/URL**: `GET /CrownX/TransactionDataCrownXPOS`, `POST /CrownX/GetTransPointLine`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CrownXController.cs` → `TransactionDataCrownXPOS()`, `GetTransPointLine()`
- **View .cshtml**: `VCM.BLUEPOS/Views/CrownX/TransactionDataCrownXPOS.cshtml`
- **ViewModel/Model**: `CrownXTransactionModel`
- **DAL methods + SQL**: `CrownXDAL.GetTransactionList()` → SP `[dbo].[sp_CrownXTransaction_Get]`
- **Phụ thuộc**: `ICrownXBLO`
- **Độ phức tạp**: Cao

#### 24.2 Báo cáo Tích Tem
- **Route/URL**: `GET /CrownX/TransactionByMemberStamp`, `GET /CrownX/DetailTransactionByMemberEarnStamp`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CrownXController.cs` → `TransactionByMemberStamp()`, `DetailTransactionByMemberEarnStamp()`
- **View .cshtml**: `VCM.BLUEPOS/Views/CrownX/TransactionByMemberStamp.cshtml`
- **ViewModel/Model**: `StampTransactionModel`
- **DAL methods + SQL**: SP `[dbo].[sp_MemberStamp_Get]`
- **Phụ thuộc**: `ICrownXBLO`
- **Độ phức tạp**: Cao

#### 24.3 Hạn mức Sử dụng Masaner
- **Route/URL**: `GET /CrownX/CheckStaffMemberRemn`, `POST /CrownX/GetStaffMemberRemnList`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CrownXController.cs` → `CheckStaffMemberRemn()`, `GetStaffMemberRemnList()`
- **View .cshtml**: `VCM.BLUEPOS/Views/CrownX/CheckStaffMemberRemn.cshtml`
- **ViewModel/Model**: `StaffMemberRemnModel`
- **DAL methods + SQL**: SP `[dbo].[sp_StaffMemberRemn_Get]`
- **Phụ thuộc**: `ICrownXBLO`
- **Độ phức tạp**: Trung bình

---

### 25. LOGS & ERROR TRACKING

#### 25.1 Log E-Invoice
- **Route/URL**: `GET /LogsData/EInvoicePublishLog`, `POST /LogsData/LoadPublishLog`, `GET /LogsData/EInvoiceErrorsLog`, `POST /LogsData/LoadInvoiceErrorsLog`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/LogsDataController.cs` → `EInvoicePublishLog()`, `LoadPublishLog()`, `EInvoiceErrorsLog()`, `LoadInvoiceErrorsLog()`
- **View .cshtml**: `VCM.BLUEPOS/Views/LogsData/EInvoicePublishLog.cshtml`
- **ViewModel/Model**: `InvoiceLogModel`
- **DAL methods + SQL**: `LogsDataDAL.GetInvoiceLog()` → SP `[dbo].[sp_EInvoiceLog_Get]`
- **Phụ thuộc**: `ILogsDataBLO`
- **Độ phức tạp**: Trung bình

#### 25.2 Log API CrownX / Voucher
- **Route/URL**: `GET /LogsData/Loyalty_CX_LogAPI`, `POST /LogsData/LoadLoyaltyCXLogAPI`, `GET /LogsData/LogAPIVoucher`, `POST /LogsData/LoadLogAPIVoucher`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/LogsDataController.cs` → `Loyalty_CX_LogAPI()`, `LoadLoyaltyCXLogAPI()`, `LogAPIVoucher()`, `LoadLogAPIVoucher()`
- **View .cshtml**: `VCM.BLUEPOS/Views/LogsData/Loyalty_CX_LogAPI.cshtml`
- **ViewModel/Model**: `APILogModel`
- **DAL methods + SQL**: SP `[dbo].[sp_APILog_Get]`
- **Phụ thuộc**: `ILogsDataBLO`
- **Độ phức tạp**: Trung bình

#### 25.3 Log Thiếu Đơn hàng / Kết ca / File Sale
- **Route/URL**: `GET /LogsData/LogMissOrderSale`, `GET /LogsData/LogCloseSalePOS`, `GET /LogsData/LogFileSalePOS`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/LogsDataController.cs` → `LogMissOrderSale()`, `LogCloseSalePOS()`, `LogFileSalePOS()`
- **View .cshtml**: `VCM.BLUEPOS/Views/LogsData/LogMissOrderSale.cshtml`
- **ViewModel/Model**: `SaleLogModel`
- **DAL methods + SQL**: SP `[dbo].[sp_SaleLog_Get]`
- **Phụ thuộc**: `ILogsDataBLO`
- **Độ phức tạp**: Trung bình

---

### 26. CHECK API

#### 26.1 Kiểm tra Thành viên CrownX
- **Route/URL**: `GET /CheckAPI/CX_ViewCheckMember`, `POST /CheckAPI/CX_Check`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CheckAPIController.cs` → `CX_ViewCheckMember()`, `CX_Check()`
- **View .cshtml**: `VCM.BLUEPOS/Views/CheckAPI/CX_ViewCheckMember.cshtml`
- **ViewModel/Model**: `CheckMemberModel`
- **DAL methods + SQL**: CrownX REST API call
- **Phụ thuộc**: CrownX API
- **Độ phức tạp**: Trung bình

#### 26.2 Kiểm tra Voucher & Coupon Partner
- **Route/URL**: `GET /CheckAPI/Partner_ViewCheckVoucher`, `POST /CheckAPI/Partner_Check`, `GET /CheckAPI/PLG_ViewCheckCoupon`, `POST /CheckAPI/Coupon_Check`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CheckAPIController.cs` → `Partner_ViewCheckVoucher()`, `Partner_Check()`, `PLG_ViewCheckCoupon()`, `Coupon_Check()`
- **View .cshtml**: `VCM.BLUEPOS/Views/CheckAPI/Partner_ViewCheckVoucher.cshtml`
- **ViewModel/Model**: `CheckVoucherModel`
- **DAL methods + SQL**: Partner REST API call
- **Phụ thuộc**: Partner REST API
- **Độ phức tạp**: Trung bình

---

### 27. VIN ID REPORT

#### 27.1 Báo cáo VinID & Blue Points
- **Route/URL**: `GET /VinID/VinIDReport`, `POST /VinID/ReportVinIDLoad`, `GET /VinID/TransactionBluePoints`, `POST /VinID/GetTransactionBluePointHeader`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/VinIDController.cs` → `VinIDReport()`, `ReportVinIDLoad()`, `TransactionBluePoints()`, `GetTransactionBluePointHeader()`
- **View .cshtml**: `VCM.BLUEPOS/Views/VinID/VinIDReport.cshtml`
- **ViewModel/Model**: `VinIDReportModel`, `BluePointsModel`
- **DAL methods + SQL**: `VinIDDAL.GetVinIDReport()` → SP `[dbo].[sp_VinIDReport_Get]`
- **Phụ thuộc**: `IVinIDBLO`
- **Độ phức tạp**: Cao

---

### 28. COMMON UTILITIES

#### 28.1 Load Combobox
- **Route/URL**: `GET /Common/GetComboxStoreList`, `GET /Common/GetComboxUnitOfMeasure`, `GET /Common/GetComboxBankList`, `GET /Common/LoadComboxStoreByUserName`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CommonController.cs` → `GetComboxStoreList()`, `GetComboxUnitOfMeasure()`, `GetComboxBankList()`, `LoadComboxStoreByUserName()`
- **View .cshtml**: —
- **ViewModel/Model**: `ComboboxModel`
- **DAL methods + SQL**: `CommonDAL.GetStoreList()` → SP `[dbo].[sp_StoreCombobox_Get]`
- **Phụ thuộc**: `ICommonBLO`
- **Độ phức tạp**: Thấp

#### 28.2 Scan Order
- **Route/URL**: `GET /Common/ScanOrder`, `POST /Common/ScanOrderLoad`, `POST /Common/InsertScanOrder`, `POST /Common/DeleteScanOrder`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/CommonController.cs` → `ScanOrder()`, `ScanOrderLoad()`, `InsertScanOrder()`, `DeleteScanOrder()`
- **View .cshtml**: `VCM.BLUEPOS/Views/Common/ScanOrder.cshtml`
- **ViewModel/Model**: `ScanOrderModel`
- **DAL methods + SQL**: SP `[dbo].[sp_ScanOrder_Get]`, `[dbo].[sp_ScanOrder_Save]`
- **Phụ thuộc**: `ICommonBLO`
- **Độ phức tạp**: Trung bình

---

### 29. ORDER PRINT

#### 29.1 In Đơn hàng Bán
- **Route/URL**: `POST /OrderSalesPrint/PrintInvoiceOrderSales`, `POST /OrderSalesPrint/PingIPAddress`
- **Controller + Action**: `VCM.BLUEPOS/Controllers/OrderSalesPrintController.cs` → `PrintInvoiceOrderSales()`, `PingIPAddress()`
- **View .cshtml**: `VCM.BLUEPOS/Views/OrderSalesPrint/PrintInvoice.cshtml`
- **ViewModel/Model**: `PrintInvoiceModel`
- **DAL methods + SQL**: `OrderSalesPrintDAL.GetOrderForPrint()` → SP `[dbo].[sp_OrderPrint_Get]`
- **Phụ thuộc**: `IOrderSalesPrintBLO`, network printer
- **Độ phức tạp**: Trung bình

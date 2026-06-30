# Hướng Dẫn Cài Đặt và Cấu Hình Hệ Thống (Portal.RPOS)

Tài liệu này hướng dẫn chi tiết cách thiết lập môi trường, cấu hình cơ sở dữ liệu, các tham số hệ thống và các bước khởi chạy ứng dụng Portal RPOS local để phát triển.

---

## 1. Yêu Cầu Môi Trường (System Prerequisites)

Để cài đặt và chạy dự án local, máy phát triển cần đáp ứng các điều kiện sau:

- **Hệ điều hành:** Windows 10 / 11 hoặc Windows Server.
- **IDE:** Visual Studio 2017, 2019 hoặc 2022 (khuyến nghị cài đặt đầy đủ các workload liên quan đến ASP.NET và phát triển web).
- **Framework:** .NET Framework 4.8.
- **Cơ sở dữ liệu:** SQL Server (hệ thống sử dụng các DB UAT nằm trên IP test nội bộ hoặc bạn có thể kết nối DB cục bộ).
- **Máy chủ Web local:** IIS Express (tích hợp sẵn trong VS) hoặc Local IIS.

---

## 2. Cấu Hình Cơ Sở Dữ Liệu (Database Connections)

Hệ thống RPOS kết nối tới nhiều database phục vụ cho các nghiệp vụ khác nhau (Master Data, Sales, Loyalty, E-Invoice, SAP Integration). Các kết nối được khai báo trong phần `<connectionStrings>` của file [Web.config](file:///d:/Projects/Portal.RPOS/VCM.BLUEPOS/Web.config).

### Các kết nối chính (Môi trường Test/UAT):

- **CentralMDPartnerContainer / CentralMDContainer:** Kết nối đến database Master Data (`RPOSMasterData`).
  - *Data Source:* `10.235.55.122\drw`
  - *Initial Catalog:* `RPOSMasterData`
  - *Tài khoản:* User: `RPOS` / Pass: `RPOS@1234`
- **CentralGeneralContainer:** Kết nối đến database chứa cấu hình chung (`CentralGeneral`).
  - *Data Source:* `10.235.55.122\drw`
  - *Initial Catalog:* `CentralGeneral`
- **CentralSalesContainer / ReadCentralSalesContainer:** Kết nối đến database doanh số bán hàng (`CentralSales`).
  - *Data Source:* `10.235.55.122\PLH`
  - *Initial Catalog:* `CentralSales`
- **CentralSalesStagingContainer:** Database tạm để staging dữ liệu giao dịch (`CentralSalesStaging`).
  - *Data Source:* `10.235.55.122\PLH`
- **LoyaltyContainer:** Database phục vụ tính năng tích điểm, thành viên Phúc Long / WinMart (`Loyalty`).
  - *Data Source:* `10.235.55.122\drw`
- **EInvoiceContainer / EInvoicePLHContainer:** Database hóa đơn điện tử (`EInvoice`).
  - *Data Source:* Trỏ tới `10.235.55.122\PLH` hoặc `10.235.55.122\drw`
- **IFSAPContainer:** Database tích hợp dữ liệu đẩy sang hệ thống SAP (`IFSAP`).
  - *Data Source:* `10.235.55.122\drw`
- **PartnerMDEntities / PartnerPLHContainer:** Database quản lý đối tác liên kết (`PartnerMD`).

> [!NOTE]
> Các chuỗi kết nối sử dụng Entity Framework Database-First đều có phần metadata trỏ đến các file Model được map sẵn, ví dụ: `metadata=res://*/EF.Central.CentralMDPartner.csdl|...` và sử dụng `provider=System.Data.SqlClient`.

---

## 3. Cấu Hình Tham Số Ứng Dụng (AppSettings)

Trong `<appSettings>` của [Web.config](file:///d:/Projects/Portal.RPOS/VCM.BLUEPOS/Web.config), cần chú ý các cấu hình nghiệp vụ quan trọng sau để hệ thống hoạt động đúng:

### 3.1. Xác Thực LDAP/Active Directory (Masan Group)
Dự án sử dụng cơ chế đăng nhập Windows AD của Masan Group:
- `PathLapAd`: `LDAP://10.235.2.11/DC=Masan,DC=local`
- `PathLapIP`: `10.235.2.11`
- `LapAdUserNameAdmin` & `LapAdPasswordAdmin`: Tài khoản dịch vụ dùng để truy vấn thông tin user AD.

### 3.2. Cấu Hình Ảnh & CDN (Image Storage)
Địa chỉ lưu trữ hình ảnh sản phẩm/khuyến mãi:
- **Test:**
  - `urlImg`: `http://cdn-phuclong.masan.local/`
  - `CDN_Image`: Đường dẫn network share `\\10.235.52.127\CDN_Image` (nơi upload/lưu file vật lý).

### 3.3. Tích Hợp API Các Đối Tác Giao Đồ Ăn (Food Delivery Platforms)
Cấu hình các API endpoint tích hợp với GrabFood, NowFood/ShopeeFood, BeFood:
- **GrabFood (UAT/Test):** Kết nối qua Gateway nội bộ `http://10.235.64.109:8080` hoặc `https://apipartnertrain.phuclong.com.vn`.
- **NowFood (UAT/Test):** Trỏ đến `https://apipartnertrain.phuclong.com.vn/api/v1/nowfood/...`
- **BeFood (UAT/Test):** Trỏ đến `https://apipartnertrain.phuclong.com.vn/api/v1/befood/...`

### 3.4. Hệ Thống Đồng Bộ & Hóa Đơn Điện Tử
Cấu hình đường dẫn FTP và thư mục chia sẻ để đồng bộ dữ liệu hóa đơn và dữ liệu xuống máy POS:
- `EInvoiceRoot`: Thư mục lưu file hóa đơn local (mặc định `C:\EInvoiceFile`).
- `FtpServerInvoice`: IP máy chủ FTP (mặc định `10.233.13.102`).
- `BluePosFolderRoot`: Thư mục đồng bộ dữ liệu POS (mặc định `C:\FTPPOS`).
- `FolderShare`: Đường dẫn share đồng bộ `\\10.235.64.101\ftpbluepos\SyncDataPos\POS`.

---

## 4. Hướng Dẫn Các Bước Khởi Chạy Local (Quick Start)

Lập trình viên thực hiện theo các bước sau để chạy dự án trên máy cá nhân:

### Bước 1: Clone dự án & Kiểm tra thư mục packages
Mở thư mục dự án trên máy phát triển của bạn. Các thư viện NuGet được quản lý thông qua file `packages.config` của từng project.

### Bước 2: Restore NuGet Packages
Mở Visual Studio, mở file solution [VCM.BLUEPOS.sln](file:///d:/Projects/Portal.RPOS/VCM.BLUEPOS.sln). 
Nhấp chuột phải vào Solution ở cửa sổ *Solution Explorer* và chọn **Restore NuGet Packages** để tải xuống các thư viện phụ thuộc (như EntityFramework 6.x, Autofac, Newtonsoft.Json, v.v.).

### Bước 3: Kiểm tra cấu hình kết nối mạng (UAT)
Do hệ thống mặc định kết nối đến các máy chủ Database Test (`10.235.55.122`) và API Partner Test (`10.235.49.11` / `10.235.64.109`), hãy đảm bảo máy tính phát triển của bạn đã được kết nối VPN Masan/Phúc Long hoặc đang nằm trong mạng nội bộ của Masan.

> [!WARNING]
> Nếu không kết nối được VPN/Mạng nội bộ, các kết nối Database sẽ bị timeout. Trong trường hợp đó, bạn cần chuyển đổi các Connection Strings sang Database Local của mình và chạy các đoạn Script tạo bảng tương ứng (nằm trong thư mục [SqlScript](file:///d:/Projects/Portal.RPOS/VCM.BLUEPOS/SqlScript)).

### Bước 4: Đặt Startup Project và Build
1. Trong Visual Studio, nhấp chuột phải vào project **VCM.BLUEPOS** và chọn **Set as StartUp Project**.
2. Chọn cấu hình chạy là `Debug` và CPU là `Any CPU`.
3. Nhấp phím `Ctrl + Shift + B` để build toàn bộ solution. Đảm bảo build thành công không có lỗi.

### Bước 5: Chạy ứng dụng
1. Nhấn `F5` hoặc nút **Start/IIS Express** trong Visual Studio để chạy ứng dụng.
2. Trình duyệt sẽ tự động mở trang chủ Portal tại địa chỉ mặc định (ví dụ: `https://localhost:44391` hoặc cổng được cấu hình ngẫu nhiên).
3. Đăng nhập bằng tài khoản Windows AD hoặc tài khoản Test được cấp quyền trong database `AdminUser`.

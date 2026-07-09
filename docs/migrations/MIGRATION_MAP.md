# MIGRATION_MAP.md — Khảo sát kiến trúc & Bảng ánh xạ Migration

> **Trạng thái**: Tài liệu khảo sát (survey). **Chưa migrate code nào.** Mục đích: làm cơ sở
> quyết định kiến trúc trước khi port từng chức năng cụ thể từ `src/legacy/VCM.BLUEPOS/` sang
> dự án mới, theo quy trình đã định nghĩa trong `CLAUDE.md` (mục "Quy tắc Migration từ
> src/legacy/"). Toàn bộ trích dẫn cũ dùng đường dẫn tương đối từ `src/legacy/`.
>
> Ghi chú framework: `.csproj` các project cũ khai `TargetFrameworkVersion=v4.8`
> (`VCM.BLUEPOS/VCM.BLUEPOS.csproj:19`) nhưng `Web.config:240` lại khai
> `<httpRuntime targetFramework="4.6.1" />` — bản thân solution cũ cũng không nhất quán phiên bản
> framework.

---

## 1. Khảo sát kiến trúc dự án CŨ (`src/legacy/VCM.BLUEPOS.sln`, .NET Framework 4.6/4.8)

### 1.1 Project/Assembly và vai trò

5 project, dependency graph (từ `ProjectReference`):

```
VCM.BLUEPOS (web)  ──▶ Business, Common, Data, Model
VCM.BLUEPOS.Business ──▶ Data, Model            (KHÔNG reference Common)
VCM.BLUEPOS.Data     ──▶ Common, Model
VCM.BLUEPOS.Common    (base layer, không ref project nào khác)
VCM.BLUEPOS.Model     (base layer, không ref project nào khác)
```

| Project | Vai trò | Ghi chú |
|---|---|---|
| `VCM.BLUEPOS` | ASP.NET MVC 5 web/API — Controllers (34 file), Views, `App_Start/` (Bundle/Filter/Route Config, OWIN Startup), `Models/` (chủ yếu Autofac wiring + view-support, không phải domain model), `Helpers/`, `Infrastructure/ServiceLocator.cs`, `SqlScript/` (file `.txt` chứa SQL export), `IncludeDLL/` (DLL vendor tham chiếu trực tiếp: `TichHopSAP.dll`, `WinSCPnet.dll`, `DocSoThanhChu.dll`). Dùng EF6, Autofac, OWIN, ApplicationInsights, EPPlus, Spire.XLS/Pdf/Barcode, Rotativa (PDF qua wkhtmltopdf). |
| `VCM.BLUEPOS.Business` | Tầng "BLO" (Business Logic Object) — 1 folder/1 class/feature (`Account`, `Invoice`, `Order`, `Product`, `Promotion`, `SetupPrice`, `StoreActivities`, `SyncData`, `VinID`, `Voucher`...), mỗi feature có `IXxxBLO` + `XxxBLO` trong cùng 1 file. |
| `VCM.BLUEPOS.Data` | DAL — cùng convention folder/feature với `IXxxData`/`XxxData`. Chứa `EF/` với **15 EDMX (Database First)** cho nhiều DB khác nhau (`CentralGeneral`, `CentralMDPartner`, `CentralSales`, `CentralSalesStaging`, `INBOUND`, `PLHWebMobileModel`, `EInvoices`, `IFSAP`, `Loyalty`, `PLG`, `PLHLog` x2, `PartnerMD`, `PartnerPLH`, `DBRead/CentralSales` — read-replica riêng). Có `Caching/RedisCacheHelper.cs` (StackExchange.Redis). |
| `VCM.BLUEPOS.Common` | Helper cross-cutting: `HashMD5.cs`, `ConvertData.cs`, `ResultResponse.cs`, `ParentAuthorize.cs`, `ControllerDataModel.cs`, `Constants/POSDataSetupConstant.cs`, `Helpers/LogsFile.cs`, `Helpers/Utils.cs`, `Helpers/Constants.cs`. |
| `VCM.BLUEPOS.Model` | POCO thuần theo feature folder (request/response/view model, DTO) — **không chứa EF entity** (entity nằm trong `Data.EF.*`), nhưng Data lại project thẳng kết quả SP vào các "ResponseModel" này (xem 1.5) → cột SP đổi là ảnh hưởng trực tiếp response model. |

### 1.2 Tổ chức tầng — có 2 luồng song song (layered đúng chuẩn + bypass)

- **Controller → BLO (đa số)**: constructor injection `IXxxBLO`, vd
  `ProductController(IProductBLO productBLO, IAuthenBLO authenBLO, ...)`
  (`VCM.BLUEPOS/Controllers/ProductController.cs:54-60`), gọi `_productBLO.GetProductList(...)`
  (`ProductController.cs:148`).
- **Business logic nằm thẳng trong controller action**:
  `ProductController.SetupLockItemByGrabFoodAPI` (`ProductController.cs:483-593`) tự dựng
  `HttpClient`, JSON payload, business rule ngay trong action, kèm secret hardcode
  `"Basic UE9TOjk4NzY1NDMyMTA="` (`ProductController.cs:554`) và tắt validate TLS cert:
  `ServicePointManager.ServerCertificateValidationCallback += (...) => true;`
  (`ProductController.cs:636,761,891`).
- **Controller bỏ qua CẢ Business lẫn Data**, tự mở `SqlConnection`/`SqlCommand`:
  `SyncDataController.DeleteSynDate` tự build connection string và chạy
  `TRUNCATE TABLE SyncDateLog` trực tiếp (`Controllers/SyncDataController.cs:99-128`, lặp lại ở
  318, 380, 453, 515, 577, 650); cũng thấy ở `InvoiceController.cs:2500`. Một số controller khác
  (`HomeController`, `PromotionController`, `StoreActivitiesController`) dùng thẳng EF
  `DbContext` (`Data.EF.Central`) mà không qua Business/Data class.
- **`BaseController`** (`Controllers/BaseController.cs`) tự làm auth/authorization không qua
  session, dùng **reflection quét toàn bộ action của mọi controller** để dựng bảng phân quyền mỗi
  lần khởi tạo (`GetListController()`, dòng 170-202), và tự `new AccountBLO()` trực tiếp (dòng
  35, 48) dù class có cả constructor DI `BaseController(IAccount accountBLO)` (dòng 56) **không
  bao giờ thực sự được dùng** — 2 đường khởi tạo cạnh tranh nhau.
- **Business layer**: convention `XxxBLO.cs` gồm interface + class trong cùng file (vd
  `Business/Product/ProductBLO.cs:13-122`). Đăng ký qua Autofac ở biên Controller↔BLO
  (`InstancePerLifetimeScope`), **nhưng mọi BLO tự `new` Data dependency của mình**, vd
  `public ProductBLO() { _data = new ProductData(); }` (`ProductBLO.cs:42-45`) — xác nhận là
  pattern hệ thống (33/33 file BLO). Đa số method BLO chỉ pass-through xuống Data
  (`ProductBLO.GetProductList` gọi thẳng `_data.GetProductList(...)`, dòng 47-50) — business
  logic thật sự phần lớn nằm trong Data (SQL/SP) hoặc thẳng trong controller, không nằm ở BLO.
  Kiểu trả về: DTO "XxxResponseModel" hoặc `ResultResponseModel`/`ResultResponse`
  (`Common/ResultResponse.cs:7-14`, field `Data` kiểu `object`).
- **Data layer**: trộn EF6 (Database First/EDMX) + ADO.NET thô, **không có lớp repository trừu
  tượng** ngoài class `XxxData` theo feature, vd `Data/Product/ProductData.cs:19-43`. Pattern
  điển hình — mở `DbContext` rồi gọi SP qua `Database.SqlQuery<T>`:
  ```csharp
  // Data/Product/ProductData.cs:66-76
  using (var db = new CentralMDPartnerContainer())
  {
      db.Database.CommandTimeout = 2 * 60;
      var data = db.Database.SqlQuery<ProductListResponseModel>(
          "[dbo].[GetProductList] @ItemCode, @ItemName, @BarCode, @TaxCode, @PageSize, @PageNumber",
          new SqlParameter("@ItemCode", itemNo ?? string.Empty), ...).ToList();
  ```
  Trường hợp phức tạp hơn dùng `SqlConnection`/`SqlCommand`/`SqlDataAdapter` với
  `CommandType.StoredProcedure` trực tiếp, tái dùng connection string của EF context:
  ```csharp
  // Data/Common/CommonData.cs:1643-1664
  using (var con = new SqlConnection(db.Database.Connection.ConnectionString))
  using (var cmd = new SqlCommand("SyncGetDataByStore", con))
  using (var da = new SqlDataAdapter(cmd))
  {
      cmd.CommandTimeout = 120;
      cmd.Parameters.AddWithValue("@POSLastCounter", posLastCounter);
      cmd.CommandType = CommandType.StoredProcedure;
      da.Fill(ds);
  ```
  **Không có `SqlHelper`/`DBHelper` dùng chung** — mỗi method Data tự lặp lại boilerplate
  connect/execute/fill, nhiều nơi try/catch nuốt lỗi và trả rỗng/default
  (`ProductData.cs:86-90,114-117`). Có cả SQL nối chuỗi động cho tên bảng, vd
  `string.Format("DELETE FROM [{0}] WHERE Pkey= '{1}';", table.TableName, rowPkey["Pkey"])`
  (`CommonData.cs:1821`) — dạng dễ dính SQL injection.
- **Common layer**: helper hash/convert/log/constants/attribute phân quyền dùng cho
  `BaseController` (`ParentAuthorize.cs`).

### 1.3 DI / IoC

**Autofac có dùng nhưng chỉ ở 1 biên duy nhất, cộng thêm 1 Service Locator chồng lên.**

- Package: `Autofac 4.9.4`, `Autofac.Integration.Mvc`, `Autofac.Extras.CommonServiceLocator`,
  `CommonServiceLocator` (`VCM.BLUEPOS/VCM.BLUEPOS.csproj:53-64`).
- `Global.asax.cs: Application_Start()` gọi `AutofacConfig.ConfigureContainer();` (dòng 20).
- `AutofacConfig.ConfigureContainer()` (`Models/AutofacConfig.cs:52-104`): đăng ký MVC
  controllers, 3 type Data (`AccountData`, `LoginData`, `ServiceLogData`, dòng 58-60), và ~30
  mapping `IXxxBLO → XxxBLO` (`InstancePerLifetimeScope`, dòng 63-97), rồi
  `DependencyResolver.SetResolver(new AutofacDependencyResolver(container));` (dòng 102).
- Một **Service Locator singleton tự viết** cũng tồn tại song song:
  `Infrastructure/ServiceLocator.cs:12-51` — static, lock-guard, bọc `AutofacServiceLocator`,
  expose `ServiceLocator.GetInstance<T>()`.
- DI **không được tôn trọng dưới biên Controller↔BLO**: mọi BLO tự `new XxxData()` (33/33), và
  `BaseController`/`AuthCookie.GetCacheMenu` tự `new AccountBLO()` thay vì resolve qua container
  (`BaseController.cs:35,48`; `Models/AuthCookie.cs:116`).

### 1.4 Configuration

- Toàn bộ cấu hình nằm trong `Web.config` (`<connectionStrings>` + `<appSettings>`), đọc qua
  `ConfigurationManager.AppSettings[...]`/`WebConfigurationManager.AppSettings[...]` rải rác
  ngay trong controller, không có class settings/options tập trung, vd
  `private string POSUser = @"" + ConfigurationManager.AppSettings["POSUser"];`
  (`ProductController.cs:42-53`).
- `<connectionStrings>` (`Web.config:14-50`) khai **~24 connection string EF `EntityClient`**
  (mỗi EDMX container 1 connection), gồm cặp failover per-store
  (`SetDB1_CentralSale`/`SetDB2_CentralSale`, `Read_SetDB1_CentralSale`/`Read_SetDB2_CentralSale`),
  **mật khẩu SQL plaintext ngay trong file**, vd `Web.config:16`
  (`user id=RPOS;password=RPOS@1234`).
- `<appSettings>` trộn config thật với secret plaintext và bật/tắt DEV-PROD bằng cách
  **comment/uncomment thủ công trong cùng 1 file** thay vì dùng config transform, vd
  `Web.config:52-59` (`setDB1`/`setDB1_Read` — khối `<!-- PRD -->` bị comment ngay dưới khối
  `<!-- Test -->` đang active). Nhiều secret plaintext khác: `LapAdPasswordAdmin` (dòng 79),
  `FtpPasswordInvoice` (dòng 91), `EInvoicePw` (dòng 96), `remoteSvrPass` (dòng 113),
  `PLGPassword` (dòng 119).
- `Web.Debug.config`/`Web.Release.config` gần như **không dùng thật** — nội dung mặc định của
  Visual Studio template, chỉ `Web.Release.config:18` có transform thật
  (`RemoveAttributes(debug)`); không có transform connection string/appSetting nào — xác nhận
  chuyển môi trường được làm thủ công bằng sửa `Web.config` trực tiếp.

### 1.5 Database access / Stored Procedure

- Kết nối mở/đóng qua `using` (dựa vào connection pooling ADO.NET) cho cả EF `DbContext` và
  `SqlConnection` thô; một số nơi dùng `connection.Open()/Close()` thủ công trong try/catch,
  không `using` (`CommonData.cs:1793-1799,1826-1832`).
- Gọi SP theo 2 cách: (a) EF6 `Database.SqlQuery<T>("[dbo].[Proc] @p1,@p2", new SqlParameter(...))`
  (`ProductData.cs:70-76,100-105`); (b) `SqlCommand` thô +
  `CommandType.StoredProcedure` + `SqlDataAdapter.Fill(DataSet)`
  (`CommonData.cs:1652-1664`, `InvoiceData.cs:1976-1982`). **Không có helper chung** — pattern
  connect/execute/fill lặp lại độc lập ở `StoreActivitiesData.cs`, `SetupPriceData.cs`,
  `ProductData.cs`, `Order/OrderSalesPrintData.cs`, `MasterData/MasterIData.cs`,
  `Invoice/InvoiceData.cs`, `Common/CommonData.cs`.
- Script SQL rời rạc: `src/legacy/Database/Stored_Procedures/Script_Stored_Procedures.sql`
  (**143 `CREATE PROCEDURE`** trong 1 file) và `VCM.BLUEPOS/SqlScript/*.txt` (vài file export SQL
  ad hoc, không phải migration có quản lý).

### 1.6 Cross-cutting concerns

| Mảng | Thực trạng cũ |
|---|---|
| **Logging** | Không có log4net/NLog/Serilog trong `packages.config`. Logger tự viết `LogsFile.WriteLogFile` (`Common/Helpers/LogsFile.cs:13-43`, ghi file theo function/ngày) + bản `WriteLogFileV2` (dòng 45-106) hardcode IP allowlist (`10.235.64.104/.105`) và UNC path. **Bị trùng lặp** ở `VCM.BLUEPOS/Helpers/LogsFile.cs` (copy gần giống, không share). Đa số lời gọi log trong hot path `BaseController` **bị comment** (dòng 69,73,76,79,83,86,91,95,116,126,131,166) — log gần như tắt. `ApplicationInsights.config` chỉ auto-collect request/dependency, **không có `TrackException`/`TrackTrace`** nào trong code (đã grep xác nhận). |
| **Auth** | `Web.config:242` khai `<authentication mode="Windows"/>` nhưng chỉ là hình thức — auth thật là **cookie ticket Forms-Auth tự tay build/giải mã**: `Models/AuthCookie.cs` (`UserLogin<T>` dùng `FormsAuthenticationTicket` + `FormsAuthentication.Encrypt`, dòng 17-25,38-47; `CheckLogin`/`CurrentUser` tự decrypt, dòng 26-64). Authorization = **bảng phân quyền dựng bằng reflection** trong `BaseController.OnActionExecuting` (dòng 60-168) đối chiếu menu-per-user (`accountBLO.LoadMenuByUser`, dòng 146) — cơ chế tự chế, không dùng `[Authorize]`/role chuẩn. |
| **Exception handling** | `Global.asax.cs` **không có `Application_Error`**. Lưới an toàn duy nhất là `filters.Add(new HandleErrorAttribute())` (`App_Start/FilterConfig.cs:10`), nhưng `Web.config:246` đặt `<customErrors mode="Off"/>` → `HandleErrorAttribute` thực chất **vô hiệu**, lỗi chưa bắt sẽ lộ raw stack trace. Còn lại là try/catch rải rác, nhiều chỗ nuốt lỗi im lặng và trả default (`ProductController.PingIP` dòng 65-80 catch rỗng; `ProductData.GetProductList` dòng 86-90 trả list rỗng khi lỗi). |
| **Caching** | Song song 2 cơ chế: (1) `System.Runtime.Caching.MemoryCache.Default` (in-process, không share giữa nhiều instance) cho menu login — `Models/AuthCookie.cs:73-129`, key dùng **hằng số chung** `Constants.CacheListMenu` (không theo user) — rủi ro đúng đắn khi chạy nhiều instance/load-balancer. (2) Redis qua `StackExchange.Redis`, bọc trong `Data/Caching/RedisCacheHelper.cs:9-41` (`Lazy<ConnectionMultiplexer>` singleton, prefix theo env `PPOS_{env}_`, fallback về DB khi Redis lỗi). |

### 1.7 Pattern đáng chú ý khác (ảnh hưởng quyết định migration)

- **Static mutable state dùng chung giữa các request**: `BaseController.ListController` là
  `public static List<ControllerDataModel>` dựng lại bằng reflection **mỗi lần khởi tạo
  controller** (dòng 38,46,170-202). Nghiêm trọng hơn:
  `Model/Account/MenuModel.cs:42-45` (`MenuPermissionModel.ListMenuPermission`) là
  **`public static List<MenuModel>`** bị **ghi đè mỗi request** từ
  `BaseController.OnActionExecuting` (dòng 147) — race condition/rò dữ liệu giữa user khi có
  request đồng thời, **không thể port nguyên trạng** sang mô hình hosting concurrent hiện đại.
- **Secret cứng trong code controller**: token Basic-Auth base64 hardcode
  (`ProductController.cs:554,1017`), và tắt validate TLS cert toàn cục qua
  `ServicePointManager.ServerCertificateValidationCallback` gán từ trong action (không bao giờ gỡ)
  — mutate static state process-wide từ code per-request.
- **15 EDMX/EF6 Database First** cho nhiều DB khác nhau + connection string failover thủ công —
  không có tương đương trực tiếp ở EF Core/Dapper, là khối lượng migration cơ học nhưng lớn.
  143 SP cần "có nhà" trong kiến trúc mới (dịch/giữ SQL, map qua Dapper, hoặc viết lại).
- **Model/DTO dùng chung xuyên tầng**: "ResponseModel" vừa là target `SqlQuery<T>(storedProc)`
  vừa là JSON contract trả về UI — đổi cột SP là ảnh hưởng thẳng ra ngoài, không có lớp chống ăn
  mòn (anti-corruption layer) ở giữa.

---

## 2. Convention dự án MỚI (POS API — .NET 10, `POS.slnx`)

> Nguồn: `CLAUDE.md` (đã đọc trong context) + verify trực tiếp code hiện có. **Lưu ý quan trọng**:
> dự án mới **không có project/layer tên "Domain"** như giả định trong yêu cầu khảo sát — 4 layer
> thật là `POS.Common` (DTO/Enum/ResultResponse — gần vai trò "Shared/Contracts", **không** phải
> Domain model có behavior), `POS.Infrastructure`, `POS.Application`, `POS.Api`. Ngoài ra còn
> `POS.Web` (Blazor Server dashboard nội bộ) và `POS.Worker` (background jobs), không nằm trong 4
> layer API chính. Không tìm thấy `SKILL.md` ở root — tài liệu tương ứng là `.claude/skills/*/SKILLS.md`
> theo từng chủ đề (api, cache, database, worker, web).

### 2.1 Cấu trúc layer thật & dependency flow

```
POS.Api → POS.Application → POS.Infrastructure → POS.Common
POS.Api → POS.Infrastructure (chỉ để đăng ký DI)
POS.Api → POS.Common
```

| Layer | Vai trò | Tương đương gần nhất bên cũ |
|---|---|---|
| `POS.Common` | DTO, Enum, `ResultResponse` (contract JSON, KHÔNG chứa business logic) | gần `VCM.BLUEPOS.Model` (POCO) — nhưng KHÔNG có logic, không phải Domain |
| `POS.Infrastructure` | Repository (Dapper), AppService (HTTP client wrapper ra external partner), Redis, RabbitMQ/Kafka, DB connection factory, Worker impl | gần `VCM.BLUEPOS.Data` + phần hạ tầng rải rác trong web project |
| `POS.Application` | Service theo domain (`Features/{Domain}/IXxxService` + `XxxService`) — business logic mỏng, chủ yếu điều phối Repository/AppService | gần `VCM.BLUEPOS.Business` (BLO) nhưng **inject đúng interface**, không tự `new` |
| `POS.Api` | Controller, Filter, Middleware | gần `VCM.BLUEPOS` (web) phần Controllers, nhưng auth/exception/validate đã tách rõ |
| `POS.Web` | Blazor Server dashboard nội bộ (MudBlazor) | không có tương đương cũ — VCM.BLUEPOS không có dashboard admin riêng, nghiệp vụ admin nằm lẫn trong web POS |
| `POS.Worker` | Host mỏng cho scheduled job/message consumer (impl thật ở `POS.Infrastructure/Workers/`) | gần các job chạy nền/aspx cron cũ (nếu có) — cần xác nhận khi khảo sát chi tiết từng chức năng |

### 2.2 Naming & tổ chức folder

- Tổ chức **theo Feature/Domain**, không theo layer kỹ thuật phẳng: `Features/{Domain}/` (vd
  `Features/Common/`, `Features/Promotion/`, `Features/CouponVoucher/`) — interface và
  implementation **cùng namespace, cùng file/folder** (khác với BLO cũ vốn cũng để chung file,
  nhưng ở đây interface application (`IXxxService`) tách biệt rõ với interface hạ tầng
  (`IXxxAppService`) khi có gọi HTTP external, xem 2.3).
- Naming Service: `I{Name}Service.cs` + `{Name}Service.cs` (đã verify: `src/POS.Application/Features/Common/ICommonService.cs` + `CommonService.cs`).
- Naming Repository: `I{Name}Repository.cs` + `{Name}Repository.cs`, gom theo domain
  (`Repositories/MasterData/`, `Repositories/Sale/`, `Repositories/Loyalty/`,
  `Repositories/CouponVoucher/`, `Repositories/Promotion/`, `Repositories/Price/`,
  `Repositories/DataSync/` — đã verify qua Glob).
- Naming AppService (external HTTP client, pattern 3 lớp riêng — xem CLAUDE.md mục "Quy tắc
  AppService"): `I{Name}AppService`/`{Name}Service` ở Infrastructure, `I{Name}Service`/
  `{Name}Service` (thin wrapper, chỉ delegate) ở Application — vd AkaChain, GotIT, Urbox.

### 2.3 Pattern kiến trúc

- **KHÔNG dùng CQRS/MediatR** — đã verify: không có package `MediatR` trong bất kỳ `.csproj`
  nào. Controller inject thẳng `IXxxService` (Application), gọi method trực tiếp — pattern
  Service/Repository truyền thống, không command/query/handler riêng.
- **KHÔNG dùng AutoMapper** — đã verify: không có package `AutoMapper`. Mapping DTO thực hiện
  thủ công (constructor/property assignment), hoặc — phổ biến hơn — Dapper `QueryAsync<T>` map
  thẳng kết quả SQL/SP vào DTO đích (không qua entity trung gian).
- Application Service phần lớn là **thin wrapper delegate xuống Repository/AppService** (xem ví
  dụ `CommonService.cs:19-20`: `public Task<...> X(...) => centralSaleRepository.X(...);`) — rất
  giống vai trò pass-through của `XxxBLO` cũ, nhưng khác biệt cốt lõi: **inject qua interface
  thật (constructor injection, container quản lý toàn bộ vòng đời)**, không tự `new` như BLO cũ.
- DI: `Microsoft.Extensions.DependencyInjection` chuẩn ASP.NET Core, đăng ký tập trung trong
  `DependencyInjection.cs` của từng project (`POS.Application/DependencyInjection.cs`,
  tương tự `POS.Infrastructure/DependencyInjection.cs`) — method `AddApplication()`/
  `AddInfrastructure()` gọi từ `Program.cs`. Có test riêng khoá việc quên đăng ký DI
  (`tests/POS.ContractTests/DependencyInjectionTests.cs`).

### 2.4 Result pattern

- Không dùng `Result<T>`/`Either`/functional result kiểu FP. Dùng 1 class DTO duy nhất
  **`ResultResponse`** (`src/POS.Common/ResultResponse.cs:7-19`): `Status` (HttpStatusCode),
  `Message` (string), `Data` (object?), `MessageTechnical` (string) — **giữ nguyên 100% so với
  `VCM.BLUEPOS.Common/ResultResponse.cs`** vì đây là **contract JSON khoá với 5.000 máy POS**
  (không được đổi tên field). `NullValueHandling.Ignore` khiến `Data: null` bị omit khỏi JSON.
- `BaseController` (`src/POS.Api/Controllers/BaseController.cs`) cung cấp helper
  `OkResult`/`BadRequestResult`/`ExceptionModels`/`NewExceptionModels` — **thay thế trực tiếp**
  các helper cùng tên/vai trò bên Web API 2 cũ (comment trong code ghi rõ "Thay ... cũ").

### 2.5 Validation

- Dùng **DataAnnotations chuẩn** (`[Required]`, `[StringLength]`...) trên DTO request, vd
  `src/POS.Common/Dtos/POS/POSRequest.cs:8-11`.
- Validate được xử lý **global** qua `ValidateModelFilter`
  (`src/POS.Api/Filters/ValidateModelFilter.cs`) — action filter chạy trước action, tự trả
  `ResultResponse` 400 nếu `ModelState` invalid; `Program.cs` set
  `SuppressModelStateInvalidFilter = true` để filter này toàn quyền kiểm soát format response
  (không dùng ASP.NET problem-details mặc định). **Không dùng FluentValidation** (đã verify:
  không có package này trong `.csproj`).

### 2.6 DTO / Mapping

- DTO đặt tại `POS.Common/Dtos/{Domain}/`, serialize bằng **Newtonsoft.Json** bắt buộc (không
  `System.Text.Json`), `[JsonProperty("tên_gốc")]` khi tên C# khác tên JSON field.
- Không có tầng mapping tự động (AutoMapper/Mapster) — Repository dùng Dapper generic
  `QueryAsync<T>`/`QueryFirstOrDefaultAsync<T>` map thẳng cột SQL/SP → property DTO theo tên
  (case-insensitive), Service thường chỉ pass-through, Controller trả thẳng DTO qua
  `OkResult(data)`.

### 2.7 DB access / Stored Procedure

- Dùng **Dapper**, không EF/EDMX. `BaseRepository`
  (`src/POS.Infrastructure/Database/BaseRepository.cs:10-68`) cung cấp helper chung
  `QueryAsync`/`QueryFirstOrDefaultAsync`/`ExecuteAsync`/`ExecuteInTransactionAsync` — **đây
  chính là `SqlHelper` dùng chung mà bản cũ KHÔNG có** (mỗi `XxxData` cũ tự lặp lại boilerplate).
- **Đa DB được consolidate qua `IDbConnectionFactory`** — thay cho 15 EDMX + ~24 connection
  string cũ: `CentralMDConnectionFactory`, `CentralSaleConnectionFactory`,
  `LoyaltyConnectionFactory`, `StagingDbConnectionFactory`, và
  **`StoreRoutedConnectionFactory`** (đã verify tồn tại trong `POS.Infrastructure/Database/`) —
  đây là điểm thay thế trực tiếp cho cơ chế `SetDB1`/`SetDB2` per-store comment-toggle thủ công
  của bản cũ, bằng 1 factory routing theo `storeNo` tại runtime.
- SP mới bắt buộc đặt tên `usp_{Domain}_{Action}`, TVP `dbo.{Name}TVP`, script lưu
  `docs/sql/{Domain}_{Action}.sql`, apply thủ công 1 lần lên `RPOSMasterData`
  (`.claude/skills/database/SKILLS.md`) — khác hẳn cách cũ (143 SP dồn 1 file, tên không theo
  convention thống nhất, không có tài liệu tra cứu tập trung — nay có
  `docs/architecture/centralMD-schema.md`).
- Gọi từ Repository: `DynamicParameters` + `CommandType.StoredProcedure`, TVP qua
  `AsTableValuedParameter("dbo.{Name}TVP")`, output param qua `ParameterDirection.Output` — đã
  verify pattern này xuất hiện ở 11 Repository hiện có.

### 2.8 Cross-cutting concerns (dự án mới)

| Mảng | Cách làm mới |
|---|---|
| **Logging** | `IKibanaService` (structured log → Elasticsearch, `LogInfo`/`LogException`) + `IFileLogHelper` (file log fallback, `WriteExpLogs`) — cả hai đều **thật sự được gọi** trong controller/middleware (khác bản cũ: log bị comment gần hết). |
| **Auth (POS.Api)** | `[Authorize(AuthenticationSchemes = "BasicAuth")]` khai báo trực tiếp trên controller/action thuộc `api/v2/...` — khớp hành vi `BasicAuthenticationAttribute` cũ nhưng dùng cơ chế `Authorize` chuẩn ASP.NET Core, không còn cookie tự chế + reflection. |
| **Auth (POS.Web)** | Cookie session (BCrypt hash trong `DashboardUsers`) + policy-based role (`StoreOperator`/`ITOps`/`SystemAdmin`) qua `WebPolicies` — mô hình phân quyền hoàn toàn khác cơ chế menu-by-reflection cũ. |
| **Exception handling** | `ExceptionHandlingMiddleware` (`src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs`) đặt đầu pipeline, bắt **mọi** exception chưa xử lý, luôn trả đúng `ResultResponse` (500) khớp contract POS — thay thế hoàn toàn tình trạng "không có `Application_Error`, `customErrors=Off`" của bản cũ. Có test khoá hành vi (`ExceptionMiddlewareTests.cs`). |
| **Caching** | **Chỉ Redis StandAlone** (`IRedisService`) cho dữ liệu chia sẻ — cấm in-memory cache cho shared data (loại bỏ hẳn rủi ro `MemoryCache.Default` không share giữa instance như bản cũ). Convention key/TTL rõ ràng (`MD:{TableName}`, TTL 43200s cho config tĩnh...). |

---

## 3. BẢNG ÁNH XẠ KIẾN TRÚC — Cũ → Mới

| # | Thành phần CŨ (`VCM.BLUEPOS`) | Vai trò cũ | → Thành phần MỚI | Ghi chú khi port |
|---|---|---|---|---|
| 1 | `VCM.BLUEPOS/Controllers/XxxController.cs` | MVC Controller, inject `IXxxBLO` | `POS.Api/Controllers/XxxController.cs` kế thừa `BaseController` | Đổi từ ASP.NET MVC 5 sang ASP.NET Core `[ApiController]`; response luôn qua `OkResult`/`BadRequestResult`, không `Request.CreateResponse` |
| 2 | `VCM.BLUEPOS.Business/{Domain}/XxxBLO.cs` (`IXxxBLO`+`XxxBLO`) | Business Logic Object — điều phối, ít logic thật | `POS.Application/Features/{Domain}/IXxxService.cs` + `XxxService.cs` | Tách interface/impl namespace giống nhau nhưng **inject Repository qua constructor thật**, không `new XxxData()` |
| 3 | `VCM.BLUEPOS.Data/{Domain}/XxxData.cs` (`IXxxData`+`XxxData`, EF6 SqlQuery hoặc raw ADO.NET) | DAL — gọi SP/EF | `POS.Infrastructure/Repositories/{Domain}/IXxxRepository.cs` + `XxxRepository.cs` (kế thừa `BaseRepository`, Dapper) | Bỏ EDMX; SP gọi qua Dapper `DynamicParameters` + `CommandType.StoredProcedure`; nếu SP legacy có tên tuỳ tiện → SP mới tạo phải đổi tên theo `usp_{Domain}_{Action}` (SP cũ giữ nguyên nếu tái dùng được, không đổi ngầm) |
| 4 | `VCM.BLUEPOS.Model/{Domain}/XxxResponseModel.cs`, `XxxRequestModel.cs` | POCO — request/response/DTO | `POS.Common/Dtos/{Domain}/XxxDto.cs` (hoặc giữ tên nếu là **response DTO đã khoá contract** với POS) | **BẮT BUỘC** kiểm tra field JSON có đang được 5.000 POS parse không trước khi đổi tên bất kỳ property nào; dùng `[JsonProperty]` (Newtonsoft), không `[JsonPropertyName]` |
| 5 | `VCM.BLUEPOS.Common/Helpers/*.cs` (hash, convert, log, constants) | Helper cross-cutting | `POS.Common/Helpers/` (nếu thuần function không phụ thuộc DI) hoặc inline tạm + `// TODO: extract to helper` nếu chưa có chỗ | Theo mục E "Helpers chưa có" trong CLAUDE.md — không tạo project Common mới, dùng đúng `POS.Common` hiện có |
| 6 | EF6 EDMX/`DbContext` (vd `CentralMDPartnerContainer`, `CentralSaleContainer`...) | ORM Database First, per-DB context | `IDbConnectionFactory` tương ứng (`CentralMDConnectionFactory`, `CentralSaleConnectionFactory`, `LoyaltyConnectionFactory`, `StagingDbConnectionFactory`) | Đối chiếu `docs/architecture/centralMD-schema.md` để lấy đúng tên bảng/cột — KHÔNG suy đoán từ EDMX cũ (có thể lệch) |
| 7 | Cặp connection string `SetDB1_*`/`SetDB2_*` (routing per-store thủ công qua comment) | Failover/routing theo store | `StoreRoutedConnectionFactory` | Logic routing theo `storeNo` chuyển thành code thật (runtime), không còn comment tay trong config |
| 8 | Autofac (`AutofacConfig.cs`) + `ServiceLocator` tự chế | DI container + service locator dự phòng | `Microsoft.Extensions.DependencyInjection` (`AddApplication()`/`AddInfrastructure()` trong `DependencyInjection.cs`) | Bỏ hẳn Service Locator pattern; mọi resolve phải qua constructor injection, có test `DependencyInjectionTests.cs` khoá việc quên đăng ký |
| 9 | `Web.config` `<appSettings>`/`<connectionStrings>` (plaintext, comment-toggle env) | Config | `appsettings.json` + `ICentralMDRepository.GetSysWebApiAsync(appCode)` (config external API từ DB, cache Redis) + mã hoá `enc:`/`POS_SECRET_KEY` cho credential | KHÔNG hardcode URL/credential mới; xem `docs/architecture/appsetting.md` |
| 10 | `Common/Helpers/LogsFile.cs` (custom file logger, phần lớn bị comment) + Application Insights (passive) | Logging | `IKibanaService` (structured → Elasticsearch) + `IFileLogHelper` (file fallback) | Log phải thật sự được gọi ở mọi nhánh lỗi — không để dead code như bản cũ |
| 11 | `Models/AuthCookie.cs` (Forms-auth ticket tự chế) + `BaseController` reflection-based menu authorization | Auth + Authorization | `POS.Api`: `[Authorize(AuthenticationSchemes = "BasicAuth")]`. `POS.Web`: Cookie auth + `WebPolicies` (`StoreAndAbove`/`OpsAndAbove`/`AdminOnly`) | 2 mô hình khác nhau tuỳ đích đến (API cho POS terminal vs Web dashboard nội bộ) — xem mục 4 |
| 12 | `App_Start/FilterConfig.cs` (`HandleErrorAttribute`, vô hiệu vì `customErrors=Off`) + try/catch rải rác nuốt lỗi | Exception handling | `ExceptionHandlingMiddleware` (global, đầu pipeline) + try/catch trong controller chỉ khi cần message nghiệp vụ riêng | Middleware mới **luôn** trả đúng `ResultResponse` — không còn tình trạng lộ raw stack trace |
| 13 | `MemoryCache.Default` (in-process, key không theo user) + `RedisCacheHelper` (StackExchange.Redis) | Caching | `IRedisService` (Redis StandAlone) — Hash/String pattern theo TTL convention | Cấm in-memory cache cho shared data; menu/permission cache (nếu port) phải key theo user/session, không dùng static field như bản cũ |
| 14 | `ResultResponseModel`/`ResultResponse` (`Status`,`Message`,`Data`,`MessageTechnical`) | Response envelope | `POS.Common/ResultResponse.cs` — **giữ nguyên field** | Không đổi tên field — đây là 1 trong các DTO khoá contract quan trọng nhất |
| 15 | `IncludeDLL/*.dll` (SAP, SFTP, số-thành-chữ) gọi thẳng từ web project | Tích hợp vendor qua DLL tham chiếu trực tiếp | AppService 3 lớp (`I{Name}AppService` ở Infrastructure → `I{Name}Service` ở Application) nếu là gọi HTTP; nếu là thư viện native (vd `DocSoThanhChu.dll`) → đánh giá case-by-case, có thể giữ dạng NuGet/port sang thư viện .NET tương đương | Không có pattern sẵn cho "thư viện tính toán thuần" (không phải HTTP client) — xem mục 4 |

---

## 4. Điểm KHÔNG map 1-1 — cần quyết định trước khi port

1. **BLO cũ trộn 3 vai trò khác nhau trong cùng 1 class** — điều phối thuần (pass-through), business rule thật, và đôi khi cả DB access tắt qua Data. Khi port từng `XxxBLO`, phải **tách thủ công**: phần điều phối → `Service` (Application), phần business rule thật (nếu có) → vẫn ở `Service` nhưng viết lại rõ ràng, phần nào đang lẫn logic DB → chuyển hẳn xuống `Repository`. Không có công thức tự động — cần đọc hiểu từng BLO để quyết định ranh giới.
2. **Business logic nằm thẳng trong Controller action** (vd `ProductController.SetupLockItemByGrabFoodAPI` gọi HTTP GrabFood ngay trong action) — không có "Controller" tương ứng ở kiến trúc mới cho phép làm vậy (`Controller BẮT BUỘC inject Application interface` theo CLAUDE.md). Phải quyết định: tách thành `IGrabFoodAppService` (Infrastructure, pattern AppService 3 lớp) trước khi port, không port nguyên trạng logic HTTP vào controller mới.
3. **Controller bypass Business+Data, tự mở `SqlConnection`** (vd `SyncDataController.DeleteSynDate`) — không có tương đương "controller tự chạy SQL" ở kiến trúc mới. Bắt buộc phải tạo Repository method mới ngay từ đầu, không có lựa chọn "giữ tạm cho nhanh".
4. **Static mutable state per-request** (`MenuPermissionModel.ListMenuPermission`, `BaseController.ListController`) — đây là bug tiềm ẩn ở bản cũ (race condition khi nhiều request đồng thời), **không map sang bất kỳ static field nào ở dự án mới**. Nếu tính năng menu/phân quyền cần port, phải thiết kế lại thành request-scoped (claim trong cookie, như `POS.Web` đã làm với claim `store_codes`) — đây là thiết kế mới hoàn toàn, không phải "port 1-1".
5. **2 mô hình DI cạnh tranh ở bản cũ** (Autofac đăng ký thật + `new XxxData()` thủ công + `ServiceLocator` song song) — khi port, phải xác định **method nào thực sự chạy qua DI, method nào đang "giả" DI**, vì interface `IXxxBLO`/`IXxxData` tồn tại không đảm bảo nó thực sự được resolve qua container ở runtime. Cần đọc code, không tin vào interface signature.
6. **"Domain" layer không tồn tại ở dự án mới** — yêu cầu khảo sát giả định có `Domain` riêng (kiểu Clean Architecture 4 tầng chuẩn: Api/Application/Domain/Infrastructure), nhưng thực tế `POS.Common` chỉ chứa DTO thụ động (không có entity với behavior/invariant). Nếu 1 chức năng cần port có domain rule phức tạp (nhiều invariant, state machine...), cần **quyết định**: nhét rule đó vào `Service` (Application, theo đúng convention hiện có — khuyến nghị để nhất quán với phần còn lại của dự án) hay đề xuất thêm 1 khái niệm "Domain" mới (lệch chuẩn hiện tại, cần bàn với team trước).
7. **15 EDMX/143 SP không có mapping 1-1 sang factory nào có sẵn** — factory hiện có (`CentralMD`, `CentralSale`, `Loyalty`, `Staging`, `StoreRouted`) chỉ phủ được 1 phần trong 15 DB cũ (`CentralGeneral`, `INBOUND`, `PLHWebMobileModel`, `EInvoices`, `IFSAP`, `PLG`, `PLHLog`, `PartnerMD`, `PartnerPLH` **chưa thấy factory tương ứng**). Từng chức năng port phải xác định: dùng factory có sẵn, hay cần tạo factory mới (theo đúng chuẩn `IDbConnectionFactory`) — quyết định theo từng task, không giả định trước.
8. **Thư viện vendor tham chiếu DLL trực tiếp** (`IncludeDLL/TichHopSAP.dll`, `WinSCPnet.dll`, `DocSoThanhChu.dll`) — không rõ các DLL này có NuGet package tương đương chạy được trên .NET 10 hay không (một số lib .NET Framework cũ dùng API không có trên .NET Core/10, vd COM interop, `System.Web`). Cần khảo sát riêng từng DLL trước khi cam kết port chức năng phụ thuộc chúng — có thể phải tìm thư viện thay thế hoặc viết lại logic thuần .NET.
9. **Secret plaintext trong `Web.config`** không có nơi "map thẳng" — không được copy nguyên giá trị connection string/password cũ vào `appsettings.json` dạng plaintext. Phải đi qua cơ chế mã hoá `enc:`/`POS_SECRET_KEY` đã có (`docs/architecture/appsetting.md`) như 1 bước bắt buộc, không phải tuỳ chọn.
10. **`ResultResponseModel` (Model cũ) vs `ResultResponse` (Common cũ)** — bản cũ có **2 class response wrapper khác nhau** (`VCM.BLUEPOS.Model/ResultResponseModel.cs` và `VCM.BLUEPOS.Common/ResultResponse.cs`) dùng lẫn lộn tuỳ controller. Dự án mới chỉ có **1** `ResultResponse` duy nhất — khi port, phải xác định controller cũ đang dùng class nào, field gì, rồi map cả hai về đúng 1 `ResultResponse` mới mà không làm lệch field JSON đang chạy thật trên POS.

---

## Kết luận & bước tiếp theo (đề xuất, chưa thực hiện)

Tài liệu này chỉ dừng ở khảo sát. Khi bắt đầu port 1 chức năng cụ thể:

1. Chọn 1 dòng trong bảng mục 3 làm điểm bắt đầu (theo đúng feature được giao).
2. Đọc kỹ toàn bộ luồng cũ liên quan (Controller → BLO → Data → SP), chú ý các trường hợp
   "KHÔNG map 1-1" ở mục 4 nếu chạm phải.
3. Áp quy trình 6 bước trong CLAUDE.md (mục "Quy tắc Migration từ src/legacy/"): định vị →
   đọc hiểu nghiệp vụ → thiết kế theo layer mới → trích dẫn `file:dòng` gốc trong comment →
   UI theo chuẩn POS.Web (nếu có) → cập nhật `docs/CURRENT_STRUCTURE.md` + test xanh.
4. Không tự ý mở rộng phạm vi ngoài chức năng được giao (vd không "tiện tay" viết lại toàn bộ
   BLO liên quan nếu task chỉ yêu cầu 1 method).

# VCM.POSBLUE.API – Project Inventory

> Generated: 2026-06-08 | Source: .NET Framework 4.6.2 | Target: .NET 10

---

## A. Cấu trúc file theo project

### A1. API_BLUEPOS (VCM.POSBLUE.API) — 232 file .cs

**Tổng quan 4 assembly trong solution:**

| Assembly | Mục đích | Số file .cs |
|----------|----------|-------------|
| VCM.POSBLUE.API | Entry point, Controllers, App_Start, Config | ~50 |
| VCM.POSBLUE.Business | Business logic layer (BLO classes) | 10 |
| VCM.POSBLUE.Data | Data access: EF DbContext, Dapper, Redis | 91 |
| VCM.POSBLUE.Model | DTOs / Request-Response models | 49 |

**Controllers (19 file):**

| File path | Loại | Class chính | Phụ thuộc chính |
|-----------|------|-------------|-----------------|
| Controllers/BaseController.cs | Controller (base) | BaseController : ApiController | — |
| Controllers/CommonController.cs | Controller | CommonController | CommonBLO, PLGBLO, VinIDBLO, KibanaService |
| Controllers/LoyaltyController.cs | Controller | LoyaltyController | LoyaltyService, MemoryCacheService, CXService, RedisManager, LoyaltyOfflineService, LoyaltyRepository, AkaChainLoyaltyService |
| Controllers/CapillaryController.cs | Controller | CapillaryController | LoyaltyService, MemoryCacheService, RedisManager, LoyaltyOfflineService, KibanaService, CouponCapillaryService, MemberPointsService, LoyaltyRepository |
| Controllers/GiftController.cs | Controller | GiftController | POSGiftService, KibanaService, MemoryCacheService, MMLSchemeService, WinXService |
| Controllers/PaymentController.cs | Controller | PaymentController | UrboxService, GotITService, LoyaltyService, LoyaltyRepository, MemoryCacheService, CouponCapillaryService, OneUService, WinXService, RedisManager |
| Controllers/OfferController.cs | Controller | OfferController | MemberBusinessService, OfferEmployeeService, ProgramPointsService, MemberPointsService, WincodeService, KibanaService |
| Controllers/VoucherController.cs | Controller | VoucherController : ApiController | VinIDBLO, ConfigurationManager (CXUrl/CXUser/CXPassword) |
| Controllers/VoucherTopUpVinIDController.cs | Controller | VoucherTopUpVinIDController | (VinID top-up integration) |
| Controllers/WinCareController.cs | Controller | WinCareController | WinCareService, WinCustomerService, KibanaService |
| Controllers/WinLifeController.cs | Controller | WinLifeController | WinLifeService, KibanaService |
| Controllers/WinpayController.cs | Controller | WinpayController | WinpayService, KibanaService |
| Controllers/PLGController.cs | Controller | PLGController | PLGBLO, KibanaService |
| Controllers/SAPController.cs | Controller | SAPController | SAPBLO, KibanaService |
| Controllers/QueueController.cs | Controller | QueueController | RabbitMQService, KibanaService |
| Controllers/SettingController.cs | Controller | SettingController | MemoryCacheService, KibanaService |
| Controllers/SyncDataPosController.cs | Controller | SyncDataPosController | SyncFileLogBLO, KibanaService |
| Controllers/ValidateController.cs | Controller | ValidateController | CommonBLO, KibanaService |
| Controllers/HomeController.cs | Controller | HomeController | — (health/home endpoint) |

**Business Layer (VCM.POSBLUE.Business — 9 file logic):**

| File path | Loại | Class chính | Interface |
|-----------|------|-------------|-----------|
| Business/Authen/LoginBLO.cs | Service | LoginBLO | ILoginBLO |
| Business/Common/CommonBLO.cs | Service | CommonBLO | ICommonBLO |
| Business/LogService/LogServiceBLO.cs | Service | LogServiceBLO | ILogServiceBLO |
| Business/Menu/MenuBLO.cs | Service | MenuBLO | IMenuBLO |
| Business/PLG/PLGBLO.cs | Service | PLGBLO | IPLGBLO |
| Business/Salary/SalaryBLO.cs | Service | SalaryBLO | ISalaryBLO |
| Business/SAP/SAPBLO.cs | Service | SAPBLO | ISAPBLO |
| Business/VINID/VinIDBLO.cs | Service | VinIDBLO | IVinIDBLO |
| Business/SyncFileLog/SyncFileLogBLO.cs | Service | SyncFileLogBLO | ISyncFileLogBLO |

**AppServices trong VCM.POSBLUE.API (3 file):**

| File | Class | Interface |
|------|-------|-----------|
| AppServices/POSGiftService.cs | POSGiftService | IPOSGiftService |
| AppServices/UsingVoucherPartnerService.cs | UsingVoucherPartnerService | — |
| AppServices/WinLifeService.cs | WinLifeService | — |

**Data Layer (VCM.POSBLUE.Data — 91 file, phân theo thư mục):**

| Thư mục | Nội dung |
|---------|----------|
| DataBaseContext/ | EF DbContext: CentralGeneralContainer, CentralMDContainer, CentralMDEntityContainer, CentralSaleContainer, DCMStarContainer, LoyaltyEntityContainer, SalaryContainer, PLGContextContainer, CentralSaleStagingContainer, KIOSContainer |
| Common/ | CommonData (data access tương ứng CommonBLO) |
| Authen/ | LoginData |
| Menu/ | MenuData |
| PLG/ | PLGData |
| SAPData/ | SAPData |
| VINID/ | VinIDData |
| Salary/ | SalaryData |
| LogServiceAPI/ | LogServiceData |
| Redis/ | RedisConnect (static), RedisSentinelData |
| Reposites/ | POSGiftInfoRepository |
| WriteLog/ | ServerIPConnection |

---

### A2. API_WebApiCore (TCX.WebApiCore) — 69 file .cs

| File path | Loại | Class chính | Interface | Phụ thuộc chính |
|-----------|------|-------------|-----------|-----------------|
| AppServices/KibanaService.cs | Service | KibanaService | — | Serilog, HttpClientService (fire-and-forget) |
| AppServices/DapperService.cs | Service | DapperService | — | RedisCacheService, DapperContext, ConfigurationManager |
| AppServices/RedisCacheService.cs | Service | RedisCacheService | — | DapperContext ×2, KibanaService, RedisManager |
| AppServices/MemoryCacheService.cs | Service | MemoryCacheService | — | System.Runtime.Caching.MemoryCache |
| AppServices/RabbitMQService.cs | Service | RabbitMQService | — | RabbitMQConnection, RabbitMQClusterConnection |
| AppServices/KafkaService.cs | Service | KafkaService | — | KafkaProducerFactory, Confluent.Kafka |
| AppServices/CXService.cs | Service | CXService | — | RedisManager, DapperService |
| AppServices/HttpClientService.cs | Service | HttpClientService (static) | — | System.Net.Http.HttpClient |
| AppServices/DataRawService.cs | Service | DataRawService | — | DataRawJsonRepository |
| AppServices/Capillary/LoyaltyService.cs | Service | LoyaltyService | — | ApiHelper, ApiHttpClientHelper, RedisManager, KibanaService, DapperService |
| AppServices/Capillary/LoyaltyCapillaryService.cs | Service | LoyaltyCapillaryService | — | CapillaryService (API_Common), ApiHelper |
| AppServices/Capillary/LoyaltyOfflineService.cs | Service | LoyaltyOfflineService | — | DapperService, RedisManager |
| AppServices/Capillary/CouponCapillaryService.cs | Service | CouponCapillaryService | — | CapillaryService, RedisManager |
| AppServices/Capillary/LoyaltyExtentionService.cs | Service | LoyaltyExtentionService | — | LoyaltyService, CXService |
| AppServices/Capillary/CapillaryHelper.cs | Helper | CapillaryHelper (static) | — | — |
| AppServices/FMV/AkaChainLoyaltyService.cs | Service | AkaChainLoyaltyService | — | AkaChainHelper, HttpClientService |
| AppServices/FMV/AkaChainHelper.cs | Helper | AkaChainHelper (static) | — | — |
| AppServices/Offer/MMLSchemeService.cs | Service | MMLSchemeService | — | DapperService, RedisManager |
| AppServices/Offer/MemberBusinessService.cs | Service | MemberBusinessService | — | DapperService, RedisManager |
| AppServices/Offer/MemberPointsService.cs | Service | MemberPointsService | — | DapperService, RedisManager |
| AppServices/Offer/OfferEmployeeService.cs | Service | OfferEmployeeService | — | OfferStaffRepository |
| AppServices/Offer/ProgramPointsService.cs | Service | ProgramPointsService | — | DapperService |
| AppServices/Offer/WincodeService.cs | Service | WincodeService | — | WincodeRepository |
| AppServices/Partner/UrboxService.cs | Service | UrboxService | — | ApiHelper, UrboxHelper |
| AppServices/Partner/GotITService.cs | Service | GotITService | — | RestSharpApiHelper |
| AppServices/Partner/OneUService.cs | Service | OneUService | — | HttpClientService |
| AppServices/Partner/WinpayService.cs | Service | WinpayService | — | ApiHelper, RestSharpApiHelper |
| AppServices/Partner/WinXService.cs | Service | WinXService | — | ApiHelper |
| AppServices/Partner/AQuaService.cs | Service | AQuaService | — | HttpClientService |
| AppServices/TCB/WinsoreService.cs | Service | WinsoreService | — | HttpClientService |
| AppServices/Voucher/IssueVoucherService.cs | Service | IssueVoucherService | — | DapperService |
| AppServices/Voucher/ROPVoucherService.cs | Service | ROPVoucherService | — | DapperService, RedisManager |
| AppServices/Wincare/WinCareService.cs | Service | WinCareService | — | ApiHelper, WincareAppHelper |
| AppServices/Wincare/WinCustomerService.cs | Service | WinCustomerService | — | HttpClientService |
| DbContext/RedisManager.cs | Connection | RedisManager | — | ConfigurationManager (RedisConnectionDefault/Second), RedisSentinelConnection, StackExchange.Redis |
| DbContext/RedisSentinelConnection.cs | Connection | RedisSentinelConnection (static) | — | StackExchange.Redis |
| DbContext/RedisClusterManager.cs | Connection | RedisClusterManager | — | StackExchange.Redis |
| DbContext/RabbitMQConnection.cs | Connection | RabbitMQConnection (static) | — | RabbitMQ.Client (port 5672, heartbeat 30s, recovery 10s) |
| DbContext/RabbitMQClusterConnection.cs | Connection | RabbitMQClusterConnection (static) | — | RabbitMQ.Client |
| DbContext/DapperContext.cs | Connection | DapperContext | — | System.Data.SqlClient |
| Factory/DapperConnectionFactory.cs | Factory | DapperConnectionFactory (static) | — | DapperContext |
| Factory/KafkaProducerFactory.cs | Factory | KafkaProducerFactory | — | Confluent.Kafka |
| Factory/IpAddressEnricher.cs | Helper | IpAddressEnricher | Serilog.Core.ILogEventEnricher | — |
| Repository/LoyaltyRepository.cs | Repository | LoyaltyRepository | — | DapperService, RedisManager |
| Repository/CentralMDRepository.cs | Repository | CentralMDRepository | — | DapperService, RedisManager |
| Repository/DataRawJsonRepository.cs | Repository | DataRawJsonRepository | — | DapperService |
| Repository/OfferStaffRepository.cs | Repository | OfferStaffRepository | — | DapperService |
| Repository/WincodeRepository.cs | Repository | WincodeRepository | — | DapperService |
| Middleware/RateLimitMiddleware.cs | Middleware | RateLimitMiddleware : ActionFilterAttribute | — | MemoryCache per IP, giới hạn 100 req/min → HTTP 429 |
| Authorization/BasicAuthenticationAttribute.cs | Filter | BasicAuthenticationAttribute | — | — |
| Shared/ApiHelper.cs | Helper | ApiHelper | — | KibanaService, HttpClient, proxy support |
| Shared/ApiHttpClientHelper.cs | Helper | ApiHttpClientHelper | — | HttpClientProvider |
| Shared/RestSharpApiHelper.cs | Helper | RestSharpApiHelper | — | RestSharp |
| Shared/OpsMonitoringHelper.cs | Helper | OpsMonitoringHelper (static) | — | KibanaService, RabbitMQProducer, RabitMQConst.Queue_Ops_Logging |
| Shared/LoyaltyHelper.cs | Helper | LoyaltyHelper (static) | — | — |
| Shared/UrboxHelper.cs | Helper | UrboxHelper (static) | — | — |
| Shared/WincareAppHelper.cs | Helper | WincareAppHelper (static) | — | — |
| Shared/VINIDHelper.cs | Helper | VINIDHelper (static) | — | — |
| Shared/NotifyConfigHelper.cs | Helper | NotifyConfigHelper | — | MemoryCacheService |
| Shared/RequestQueue.cs | Helper | RequestQueue | — | ConcurrentQueue |
| Shared/ResponseHelper.cs | Helper | ResponseHelper (static) | — | — |
| Shared/ExecutionTimer.cs | Helper | ExecutionTimer : IDisposable | — | Stopwatch |
| Shared/ValidateModelAttribute.cs | Filter | ValidateModelAttribute : ActionFilterAttribute | — | ModelState |
| Shared/RedisConnectionHelper.cs | Helper | RedisConnectionHelper (static) | — | RedisManager |

---

### A3. API_Common (TCX.API.Common) — 421 file .cs

| Thư mục | Loại | Số file | Ghi chú |
|---------|------|---------|---------|
| Enums/ | Enum | 25 | CapillaryEnum, CXEnum, LoyaltyEnum, VoucherROPEnum, WinpayEnum, v.v. |
| Const/ | Const | 10 | RedisKeyConst, RabitMQConst, MessageConst, GiftStatusConst, v.v. |
| Dtos/ | DTO | 157 | Phân theo domain: Capillary, CentralMD, Loyalty, Ops, Partner, POS, ROP, WinCare, WinMoney, Winpay |
| Models/ | Model | 188 | Authen, Common, CrownX, GiftBox, Odoo, Partner, PLG, SAP, UrBox, VINID, WinLife |
| Helpers/ | Helper | 15 | ConvertHelper, DateTimeHelper, StringHelper, EncryptionHelper, ValidateHelper, v.v. |
| Shared/ | Utility | 12+ | AESUtils, ApiCallHelper, HttpClientProvider, EncryptionUtil, SqlErrorHandler, v.v. |
| Attributes/ | Attribute | 4 | Base64Validation, NotEmptyItems, StringRange, ValidateValueRange |
| AppService/ | Service | 1 | CapillaryService (implements ICapillaryLoyaltyService) — full loyalty client |

**Key classes trong API_Common:**

| Class | Loại | Ghi chú |
|-------|------|---------|
| CapillaryService | Service | ICapillaryLoyaltyService — GetCustomerDetail, Registration, AddTransaction, PointsRedeem, PointReverse, RedemptionValidation |
| RedisKeyConst | Const | Static methods trả về Redis key pattern (prefix-based) |
| RabitMQConst | Const | Queue names: Queue_Ops_Logging, v.v. |
| ConvertHelper | Helper | ToObject&lt;T&gt;, ToJson, ParseInt, v.v. |
| StringHelper | Helper | ObjectToStringLowercase, Left, v.v. |
| EncryptionHelper | Helper | AES encrypt/decrypt |
| DateTimeHelper | Helper | GetSecondsUntilMidnight, v.v. |
| FileHelper | Helper | WriteExpLogs, WriteLogs (file-based logging fallback) |
| HostHelper | Helper | GetIpAddress, GetTotalCpuCores, GetTotalRamMB |

---

## B. NuGet Packages Hiện Tại → .NET 10

### VCM.POSBLUE.API (packages.config → .csproj `<Reference>`)

| Package | Version hiện tại | .NET 10 tương đương | Ghi chú |
|---------|-----------------|---------------------|---------|
| Antlr | 3.5.0.2 | **Bỏ** | Chỉ dùng bởi old ASP.NET MVC bundling |
| Autofac | 8.0.0 | Autofac 9.x hoặc built-in DI | Khuyến nghị dùng built-in `IServiceCollection` |
| Dapper | 2.1.35 | Dapper 2.1.x | Giữ nguyên, tương thích tốt |
| Elasticsearch.Net | 7.17.5 | Elastic.Clients.Elasticsearch 8.x | Serilog sink chuyển sang `Serilog.Sinks.Elasticsearch 10.x` |
| EntityFramework | 6.5.1 | **Microsoft.EntityFrameworkCore 9.x** | **Breaking change lớn** — EDMX → code-first migration |
| EntityFramework.SqlServer | 6.5.1 | Microsoft.EntityFrameworkCore.SqlServer 9.x | Đi kèm EF Core |
| Confluent.Kafka | 1.0.0 | Confluent.Kafka 2.6.x | Breaking changes ở API producer/consumer |
| RabbitMQ.Client | 6.2.2 | **RabbitMQ.Client 7.x** | Breaking changes: async model, channel API thay đổi |
| StackExchange.Redis | 2.8.0 | StackExchange.Redis 2.8.x | Giữ nguyên, đã hỗ trợ .NET 10 |
| Newtonsoft.Json | 13.0.3 | Newtonsoft.Json 13.x | Giữ nguyên (hoặc chuyển sang `System.Text.Json` built-in) |
| Newtonsoft.Json.Bson | 1.0.2 | Newtonsoft.Json.Bson 1.0.x | Giữ nguyên nếu dùng BSON |
| RestSharp | 112.1.0 | RestSharp 112.x | Giữ nguyên |
| Serilog | 4.0.1 | Serilog 4.x | Giữ nguyên |
| Serilog.Enrichers.Environment | 3.0.1 | Serilog.Enrichers.Environment 3.x | Giữ nguyên |
| Serilog.Formatting.Compact | 3.0.0 | Serilog.Formatting.Compact 3.x | Giữ nguyên |
| Serilog.Formatting.Elasticsearch | 10.0.0 | Serilog.Formatting.Elasticsearch 10.x | Giữ nguyên |
| Serilog.Sinks.Elasticsearch | 10.0.0 | Serilog.Sinks.Elasticsearch 10.x | Giữ nguyên |
| Serilog.Sinks.File | 6.0.0 | Serilog.Sinks.File 6.x | Giữ nguyên |
| Serilog.Sinks.PeriodicBatching | 5.0.0 | Serilog.Sinks.PeriodicBatching 5.x | Giữ nguyên |
| Microsoft.AspNet.WebApi.Client | 6.0.0 | **Bỏ** | Thay bằng `System.Net.Http.HttpClient` built-in |
| Microsoft.AspNet.WebApi.Core | 6.0.0 | **Bỏ** | Thay bằng ASP.NET Core built-in controllers |
| Microsoft.Web.Infrastructure | 2.0.0 | **Bỏ** | Không cần trong ASP.NET Core |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | **Built-in .NET 10** | Không cần package riêng |
| Microsoft.Extensions.Logging.Abstractions | 9.0.0 | **Built-in .NET 10** | Không cần package riêng |
| Microsoft.Extensions.Options | 9.0.0 | **Built-in .NET 10** | Không cần package riêng |
| Polly | 8.6.5 | Polly 8.x | Giữ nguyên |
| librdkafka.redist | 2.5.0 | **Bỏ** | Bundled trong Confluent.Kafka 2.x |
| System.* (BCL backports) | various | **Bỏ** | Tất cả built-in .NET 10 |
| Microsoft.Bcl.* | various | **Bỏ** | Tất cả built-in .NET 10 |

**Tóm tắt package action:**
- Bỏ hoàn toàn: 12 package (ASP.NET MVC, BCL backports, system libs)
- Upgrade breaking change: 3 package (EF Core, RabbitMQ.Client 7, Confluent.Kafka 2)
- Giữ nguyên / upgrade minor: 14 package

---

## C. Connection Strings & AppSettings

> `web.config` không được commit vào git (chứa thông tin nhạy cảm).
> Danh sách key được trích xuất từ source code (`ConfigurationManager.ConnectionStrings[key]`).

### Connection Strings

| Tên key | Loại | Dùng bởi | Ghi chú |
|---------|------|----------|---------|
| CentralGeneralContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — store, POS setup |
| CentralMDContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — master data |
| CentralMDEntityContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — entity variant |
| CentralSaleContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — sales transactions |
| DCMStarContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — DCM Star |
| LoyaltyEntityContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — loyalty |
| SalaryContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — salary |
| PLGContextContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — PLG vouchers |
| CentralSaleStagingContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — staging |
| KIOSContainer | EF (SQL Server) | VCM.POSBLUE.Data | EF DbContext — KIOS |
| DAPPER_CENTRAL_MD | ADO.NET (SQL Server) | DapperService | Master data reads via stored proc |
| DAPPER_LOYALTY | ADO.NET (SQL Server) | DapperService | Loyalty DB reads via stored proc |
| DAPPER_PARTNER | ADO.NET (SQL Server) | DapperService | Partner DB |
| DAPPER_PLHLog | ADO.NET (SQL Server) | DapperService | PLH logging DB |
| RedisConnectionDefault | Redis (StackExchange) | RedisManager | Write connection (hoặc Sentinel primary) |
| RedisConnectionSecond | Redis (StackExchange) | RedisManager | Read replica (hoặc Sentinel replica) |

> Redis có hỗ trợ Redis Sentinel — `RedisSentinelConnection.IsRedisSentinel()` đọc từ config.

### AppSettings Keys

| Key | Dùng bởi | Ghi chú |
|-----|----------|---------|
| CXUrl | VoucherController | Endpoint CrownX voucher service |
| CXUser | VoucherController | CrownX auth username |
| CXPassword | VoucherController | CrownX auth password |
| RabbitMQ connection | RabbitMQConnection | Format: `host;username;password` |
| Redis Sentinel config | RedisSentinelConnection | Enable/disable Sentinel mode |
| Capillary API endpoints | CapillaryService (API_Common) | Base URL external loyalty |
| SAP API endpoints | SAPBLO | SAP Odoo/Voucher service URLs |
| AkaChain API endpoints | AkaChainHelper | FMV blockchain service |
| Serilog/Elasticsearch URL | App_Start/WebApiConfig | Log sink target |

---

## D. Dependency Map (Service → phụ thuộc vào gì)

| Class | Phụ thuộc | Lifetime hiện tại (.NET 4.6) |
|-------|-----------|------------------------------|
| CommonController | CommonBLO, PLGBLO, VinIDBLO | `new` trong action method → Transient |
| CommonController | KibanaService | `new` trong action method → Transient |
| LoyaltyController | LoyaltyService, MemoryCacheService, CXService, RedisManager, LoyaltyOfflineService, LoyaltyRepository, AkaChainLoyaltyService | `new` trong constructor → Transient |
| PaymentController | UrboxService, GotITService, LoyaltyService, LoyaltyRepository, MemoryCacheService, CouponCapillaryService, OneUService, WinXService, RedisManager | `new` trong constructor → Transient |
| LoyaltyService | ApiHelper, ApiHttpClientHelper, KibanaService, RedisManager, DapperService | `new` → Transient |
| LoyaltyRepository | DapperService, RedisManager | `new` → Transient |
| DapperService | RedisCacheService, DapperContext | `new` trong constructor → Transient |
| RedisCacheService | DapperContext ×2, KibanaService, RedisManager | `new` → Transient |
| RedisManager | ConfigurationManager (connection string) | Static lazy init → **Singleton thực tế** |
| RedisSentinelConnection | ConfigurationManager | Static lazy init → **Singleton thực tế** |
| RabbitMQConnection | ConfigurationManager | Static lazy init → **Singleton thực tế** |
| RabbitMQClusterConnection | ConfigurationManager | Static lazy init → **Singleton thực tế** |
| KibanaService | Serilog.Log (static), HttpClientService (static) | `new` → Transient (stateless) |
| HttpClientService | System.Net.Http.HttpClient | Static methods → **Singleton thực tế** |
| MemoryCacheService | System.Runtime.Caching.MemoryCache | `new` → dùng global cache instance |
| MMLSchemeService | DapperService, RedisManager | `new` → Transient |
| OfferStaffRepository | DapperService | `new` → Transient |
| OpsMonitoringHelper | KibanaService (new), RabbitMQProducer (static) | Static methods → fire-and-forget |
| CommonBLO | CommonData | `new` → Transient |
| LoginBLO | LoginData | `new` → Transient |
| VinIDBLO | ConfigurationManager, EF DbContext | `new` → Transient |
| PLGBLO | PLGData | `new` → Transient |
| POSGiftService | POSGiftInfoRepository | `new` → Transient |
| CapillaryService (API_Common) | ApiCallHelper, EncryptionHelper, HttpClientProvider | `new` → Transient |

> **Lưu ý migration:** Tất cả dependency hiện tại được `new` thủ công (không có IoC container ở controller level). Khi sang .NET 10, cần đăng ký tất cả vào `IServiceCollection` với lifetime phù hợp. RedisManager, RabbitMQConnection phải đăng ký `Singleton`.

---

## E. Danh sách Timer / Background Jobs

| Class | Trigger | Mục đích | Queue/Sink |
|-------|---------|----------|-----------|
| OpsMonitoringHelper.OpsLogging | Per request (async fire-and-forget) | Ghi error log vào RabbitMQ → Ops dashboard | `RabitMQConst.Queue_Ops_Logging` → table `webapi_error_logs` |
| OpsMonitoringHelper.OpsMonitoring | Per request (async fire-and-forget) | Upsert server status (CPU, RAM, version) | `RabitMQConst.Queue_Ops_Logging` → table `webapi_status` |
| KibanaService.LogRequest/LogResponse | Per request (fire-and-forget `Task.Run`) | Structured log → Elasticsearch qua Serilog | Serilog sink → Elasticsearch |
| KibanaService.LogException | Per error (fire-and-forget `Task.Run`) | Error log → Elasticsearch | Serilog sink → Elasticsearch |
| RabbitMQService | On demand | Publish messages to RabbitMQ exchange | Cluster + non-cluster connections |
| KafkaService | On demand | Produce Kafka events | Confluent.Kafka producer |

> Không có `System.Threading.Timer` hay `System.Timers.Timer` nào trong codebase.
> Background processing hoàn toàn là fire-and-forget async (`Task.Run`, `_ = Task.Run(...)`) triggered từ request.

---

## F. Tóm Tắt Số Liệu

| Loại | Số lượng |
|------|---------|
| **Controllers** | 19 (18 real + 1 BaseController) |
| **Services / BLO** | 9 BLO (BLUEPOS) + 34 AppServices (WebApiCore) + 1 (API_Common) = **44** |
| **Repositories** | 5 (WebApiCore) + 1 (BLUEPOS.Data) = **6** |
| **Data Access (BLO Data layer)** | 9 (CommonData, LoginData, MenuData, v.v.) |
| **EF DbContext** | 10 (database connections) |
| **DTOs + Models** | 49 (BLUEPOS.Model) + 157 (API_Common Dtos) + 188 (API_Common Models) = **394** |
| **Enums** | 25 |
| **Constants** | 10 |
| **Helpers / Utilities** | 15 (API_Common) + 13 (WebApiCore Shared) = **28** |
| **Tổng file .cs** | 232 + 69 + 421 = **722** |
| **NuGet packages cần bỏ** | 12 |
| **NuGet packages cần upgrade breaking** | 3 (EF Core, RabbitMQ.Client, Confluent.Kafka) |
| **Connection strings** | 16 (10 EF + 4 Dapper + 2 Redis) |
| **External integrations** | Capillary, SAP (Odoo/Voucher), AkaChain/FMV, UrBox, GotIT, OneU, WinX, AQua, Winpay, VINID, WinCare, Winsore |

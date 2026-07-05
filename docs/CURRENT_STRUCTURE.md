# CURRENT_STRUCTURE.md — Bản đồ hiện trạng POS Migration (.NET 10)

> Generated: 2026-06-15 | **Synced: 2026-07-01** (thêm MỤC G — chữ ký Helpers POS.Common) | Branch: dev
> Projects thực tế: `POS.Api`, `POS.Application`, `POS.Common`, `POS.Infrastructure`, `POS.Worker`
> (Note: task đề cập VCM.POSBLUE.* nhưng project đã được đổi tên sang POS.* khi khởi tạo solution mới)
>
> **Reorg 2026-06 (đã phản ánh bên dưới):**
> - `POS.Application`: bỏ `Interfaces/`+`Services/` phẳng → gom theo `Features/{Domain}/` (Common, Partner, DataSync, Sap, Gift) — namespace `POS.Application.Features.{Domain}`
> - `POS.Infrastructure`: `Repositories/` gom theo `{Domain}/` (MasterData, Sale, Loyalty, Sap) — **namespace GIỮ NGUYÊN** `POS.Infrastructure.Repositories[.Interfaces]`; `AppServices/` gom theo `{Domain}/` (Partner, DataSync) — namespace `POS.Infrastructure.AppServices.{Domain}`
> - `POS.Web` (cấu trúc chi tiết ở `docs/WEB_STATUS.md`): Pages gom `Store/{Reports,Transactions,Operations,Dialogs}`, `Ops/`, `Admin/`
>
> POS.Web KHÔNG nằm trong file này (xem `docs/WEB_STATUS.md`).

---

## MỤC A — Cây thư mục thực tế

```
src/
├── POS.Api/
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── appsettings.Production.json
│   ├── Authentication/
│   │   └── BasicAuthHandler.cs
│   ├── Controllers/
│   │   ├── BaseController.cs
│   │   ├── CommonController.cs
│   │   ├── GiftController.cs
│   │   ├── KafkaController.cs
│   │   ├── LoyaltyController.cs
│   │   ├── PaymentController.cs
│   │   ├── SAPController.cs
│   │   ├── SyncDataPosController.cs
│   │   └── WinpayController.cs
│   ├── Filters/
│   │   └── ValidateModelFilter.cs
│   ├── Middleware/
│   │   ├── BasicAuthHandler.cs
│   │   ├── ExceptionHandlingMiddleware.cs   ← G3 global exception → ResultResponse (UsePosExceptionHandling)
│   │   ├── PosApiKeyMiddleware.cs           ← Xác thực X-API (MD5) / Authorization, fail-closed (UsePosApiKeyAuth)
│   │   ├── RequestResponseLoggingMiddleware.cs ← Log request/response MỌI API qua IKibanaService, cấu hình RequestLogging:Enabled (UseRequestResponseLogging, đặt ngoài cùng pipeline)
│   │   └── RequestLoggingOptions.cs         ← Options cho middleware trên (Enabled/MaxBodyBytes/ExcludePaths)
│   ├── Program.cs
│   └── Properties/
│       └── launchSettings.json
│
├── POS.Application/
│   ├── DependencyInjection.cs
│   └── Features/                      ← gom theo domain; namespace POS.Application.Features.{Domain}
│       ├── Common/
│       │   ├── ICommonService.cs / CommonService.cs
│       │   └── IHealthCheckService.cs / HealthCheckService.cs
│       ├── Partner/
│       │   ├── IAkaChainLoyaltyService.cs / AkaChainLoyaltyService.cs
│       │   ├── IGotITService.cs / GotITService.cs
│       │   └── IUrboxService.cs / UrboxService.cs
│       ├── DataSync/
│       │   ├── IDataRawService.cs / DataRawService.cs
│       │   ├── ISyncDataPosService.cs / SyncDataPosService.cs
│       │   ├── IKafkaService.cs / KafkaService.cs
│       │   └── IMasterDataSyncService.cs / MasterDataSyncService.cs   ← EnsureMasterDataFileAsync (Parallel SP2 × MaxParallelTables) + LogDownloadAsync
│       ├── Sap/
│       │   └── ISAPService.cs / SAPService.cs
│       └── Gift/
│           └── IGiftService.cs / GiftService.cs
│
├── POS.Common/
│   ├── ResultResponse.cs
│   ├── Const/
│   │   ├── RedisConst.cs
│   │   └── RedisKeyConst.cs
│   ├── Dtos/
│   │   ├── AuthDto.cs
│   │   ├── HttpResponseBlueDto.cs
│   │   ├── KafkaMessage.cs
│   │   ├── NotifyConfigDto.cs
│   │   ├── RabbitMessageDto.cs
│   │   ├── RedisDto.cs
│   │   ├── SMSMessage.cs
│   │   ├── SysWebApiDto.cs
│   │   ├── SysWebApiUserDto.cs
│   │   ├── AkaChain/
│   │   │   └── AkaChainDto.cs
│   │   ├── B2B/
│   │   │   ├── TransHeaderB2BDto.cs
│   │   │   ├── TransHistoryB2BDto.cs
│   │   │   └── TransLineB2BDto.cs
│   │   ├── Capillary/
│   │   │   ├── CapillaryBaseDto.cs
│   │   │   ├── Update_pos_enroll.cs
│   │   │   ├── Coupons/CapillaryCouponsDto.cs
│   │   │   ├── Customer/
│   │   │   │   ├── CustomerLoyaltyDetailDto.cs
│   │   │   │   ├── CustomerRegistrationDto.cs
│   │   │   │   ├── CustomerUpdateRequest.cs
│   │   │   │   ├── GetCustDetailCapillaryResponse.cs
│   │   │   │   └── UpdateMobileEnrollRequest.cs
│   │   │   ├── Enosta/
│   │   │   │   ├── TransactionReturnRequest.cs
│   │   │   │   └── TransactionReturnResponse.cs
│   │   │   ├── Point/
│   │   │   │   ├── PointDto.cs
│   │   │   │   └── PointModePOSResponse.cs
│   │   │   ├── Redemption/RedemptionResponse.cs
│   │   │   ├── Tier/TierUpdateCriteriaCapillary.cs
│   │   │   ├── Transaction/
│   │   │   │   ├── AddTransactionRequest.cs
│   │   │   │   ├── AddTransactionResponse.cs
│   │   │   │   ├── TransactionCapillary.cs
│   │   │   │   └── TransactionDetailResponse.cs
│   │   │   └── Vouchers/ValidateVoucherCapillary.cs
│   │   ├── CentralMD/
│   │   │   ├── CentralMDDto.cs  (StoreDto, StoreSetup, SysWebApiConfig, StoreSetConfig, SyncTableList,
│   │   │   │                      CpnVchCodeQuotaRemn, CpnVchCodeSendQuota, CpnVchCodeSendDto,
│   │   │   │                      CpnVchBOMHeaderDto, CpnVchBOMLineDto, ItemDto, ItemPointsMemberDto,
│   │   │   │                      LoyaltyRateDto, MMLSchemeHeader, MMLSchemeItem, MMLSchemeResponse,
│   │   │   │                      MMLSchemeRequest, MMLSchemeItemsRequest)
│   │   │   ├── EmployeeDto.cs   (EmployeeListItemDto, EmployeeListFilter, EmployeeCreateDto)        ← 5.1 Danh mục NV + tạo mới/đổi mật khẩu POS
│   │   │   ├── ProductListDto.cs (ProductListItemDto, ProductListFilter, PosVatCodeDto)             ← 6.1
│   │   │   ├── ProductCreateDto.cs (ProductCreateDto, BarcodeRowDto, ArticleTypeDto, UnitOfMeasureDto) ← 6.2
│   │   │   └── ProductLockDto.cs (ProductLockItemDto, ProductLockFilter, ProductLockSaveDto)        ← 6.4
│   │   ├── DataSync/
│   │   │   ├── SyncTableInfo.cs          ← map SP1 row (TableName, POSLastCounter, Procedure, OrderByName, IsByStore, ColumnFilter, IsFirstDataAll, GroupName)
│   │   │   ├── GetMasterDataFileRequest.cs   ← SiteCode, PosTerminal, FolderFile, PathSync, TypeSync, TargetDir, SyncAction? (override Action mọi batch: Web Sync="DELETE-INSERT", null=TRUNC-INSERT→INSERT)
│   │   │   └── GetMasterDataFileResult.cs    ← nội bộ service (Success, FileName, RelativePath, TableCount, Message) — không lên HTTP body
│   │   ├── Coupon/CouponDto.cs
│   │   ├── SetupCoupon/SetupCouponDtos.cs   ← 8.1/8.2 (List/Detail/IssueSave/AdvancedSave/Code…) + CouponHeaderListFilter/CouponHeaderListItemDto (master list /promotion/coupons)
│   │   ├── Voucher/SetupVoucherDtos.cs      ← 8.3/8.4 (VoucherList/Detail/Save + VoucherPublished lookup)
│   │   ├── CXVoucher/CXVoucherDto.cs
│   │   ├── DRW/UpdateStatusSfaffDiscountDto.cs
│   │   ├── FileModel/FileModelDto.cs
│   │   ├── Giftee/GifteeDto.cs
│   │   ├── GotIT/GotITDto.cs
│   │   ├── LogService/LogServiceDto.cs
│   │   ├── Loyalty/
│   │   │   ├── InfoMemberDto.cs
│   │   │   ├── LoyaltyBaseDto.cs
│   │   │   ├── TransactionLoyaltyDto.cs
│   │   │   ├── CX/CXDto.cs
│   │   │   ├── MemberBusiness/MemberBusinessDto.cs
│   │   │   ├── ProgramPoints/ProgramPointsDto.cs
│   │   │   ├── WinCode/WinCodeDto.cs
│   │   │   └── WinScore/WinScoreDto.cs
│   │   ├── MSN/MSNDto.cs
│   │   ├── Ops/
│   │   │   ├── HealthCheckItemDto.cs
│   │   │   ├── Ops_Logging.cs
│   │   │   └── Ops_Monitoring.cs
│   │   ├── PartnerApi/
│   │   │   ├── CheckVoucherPartnerDto.cs
│   │   │   ├── SetKeyRedis.cs
│   │   │   └── UrboxDto.cs
│   │   ├── POS/
│   │   │   ├── KafkaMessageDto.cs
│   │   │   ├── KafkaMessagePOS.cs
│   │   │   ├── POSRequest.cs
│   │   │   ├── ValidateTransactionDto.cs
│   │   │   ├── Common/CommonDtos.cs
│   │   │   └── Gift/GiftBarcodeRequest.cs
│   │   ├── RabbitMessageDto.cs (root — đã liệt kê)
│   │   ├── Request/RequestDto.cs
│   │   ├── Reward/RewardDto.cs
│   │   ├── ROP/ROPDto.cs
│   │   ├── StagingDB/
│   │   │   ├── DataJsonDto.cs
│   │   │   ├── DataRawJsonDto.cs
│   │   │   └── StagingDBConfigDto.cs
│   │   ├── Tax/
│   │   │   ├── InvoiceCreated.cs
│   │   │   └── TaxCustInfo.cs
│   │   ├── Telegram/
│   │   │   ├── MessageToTellegram.cs
│   │   │   └── NotifyTelegram.cs
│   │   ├── TopupVoucherVinID/TopupVoucherVinIDDto.cs
│   │   ├── Vouchers/
│   │   │   ├── VoucherDto.cs
│   │   │   └── VoucherStatusResponseDto.cs
│   │   ├── WinCare/WinCareDto.cs
│   │   ├── WinCustomer/WinCustomerDto.cs
│   │   ├── WinMoney/WinMoneyConversion.cs
│   │   ├── Winpay/WinpayDto.cs
│   │   └── WinX/WinXDto.cs
│   ├── Enums/
│   │   ├── ADConnectionStatus.cs
│   │   ├── ApiEnum.cs
│   │   ├── AppCodeEnum.cs
│   │   ├── CapillaryEnum.cs
│   │   ├── CXEnum.cs
│   │   ├── DiscountTypeEnum.cs
│   │   ├── EnumLogin.cs
│   │   ├── EnvironmentEnum.cs
│   │   ├── EStatus.cs
│   │   ├── EStatusResponse.cs
│   │   ├── GiftStatusEnum.cs
│   │   ├── KafkaEnum.cs
│   │   ├── LoyaltyEnum.cs
│   │   ├── MemberBusinessesEnum.cs
│   │   ├── OpsDashboardEnum.cs
│   │   ├── PartnerEnum.cs
│   │   ├── PrefixEnum.cs
│   │   ├── SAP_PLH_Enum.cs
│   │   ├── StampEnum.cs
│   │   ├── SystemEnum.cs
│   │   ├── TelegramEnum.cs
│   │   ├── VATEnum.cs
│   │   ├── VoucherROPEnum.cs
│   │   ├── WinLifeRegisterEnum.cs
│   │   └── WinpayEnum.cs
│   └── Helpers/
│       ├── DateTimeHelper.cs
│       ├── FileLogHelper.cs
│       ├── FormatHelper.cs
│       ├── HostHelper.cs
│       ├── ResponseHelper.cs
│       └── StringHelper.cs
│
├── POS.Worker/
│   ├── POS.Worker.csproj           (SDK: Microsoft.NET.Sdk.Worker)
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Production.json
│
└── POS.Infrastructure/
    ├── DependencyInjection.cs
    ├── AppServices/                   ← gom theo domain; namespace POS.Infrastructure.AppServices.{Domain}
    │   ├── Partner/
    │   │   ├── IAkaChainLoyaltyAppService.cs / AkaChainLoyaltyAppService.cs
    │   │   ├── IGotITAppService.cs / GotITService.cs
    │   │   └── IUrboxAppService.cs / UrboxService.cs
    │   └── DataSync/
    │       └── IKafkaAppService.cs / KafkaAppService.cs
    ├── Cache/
    │   ├── IRedisManager.cs
    │   ├── RedisManager.cs
    │   └── RedisOptions.cs
    ├── Database/
    │   ├── BaseRepository.cs
    │   ├── CentralMDConnectionFactory.cs
    │   ├── CentralSaleConnectionFactory.cs
    │   ├── IDbConnectionFactory.cs
    │   ├── LoyaltyConnectionFactory.cs
    │   ├── StagingDbConnectionFactory.cs
    │   └── StoreRoutedConnectionFactory.cs
    ├── Files/
    │   ├── ConnectToSharedFolder.cs
    │   ├── IFtpFileTransfer.cs
    │   ├── FtpFileTransfer.cs
    │   ├── IFileArchiveService.cs / FileArchiveService.cs   ← ZipFile.CreateFromDirectory (Singleton, MasterDataSyncOptions.ZipCompressionLevel)
    │   ├── ISyncFileLock.cs / SyncFileLock.cs               ← keyed SemaphoreSlim (Singleton), chống sinh zip trùng
    │   └── MasterDataSyncOptions.cs                         ← bind section "MasterDataSync": SqlCommandTimeoutSeconds, BatchSizePerFile, MaxParallelTables, ZipCompressionLevel, KeepZipDays, DateInZipName
    ├── Logging/
    │   ├── ElasticsearchOptions.cs
    │   ├── FileLogHelper.cs
    │   ├── IFileLogHelper.cs
    │   ├── IKibanaService.cs
    │   ├── KibanaService.cs
    │   └── SerilogConfiguration.cs
    ├── Messaging/
    │   ├── IKafkaProducer.cs
    │   ├── IRabbitMQProducer.cs
    │   ├── KafkaProducer.cs
    │   ├── RabbitMQOptions.cs
    │   └── RabbitMQProducer.cs
    ├── Redis/
    │   ├── IRedisService.cs
    │   └── RedisService.cs
    ├── Security/
    │   └── SecretProtector.cs          ← AES-256-GCM, token enc: (giải mã credentials trong appsettings)
    ├── Workers/
    │   ├── PosSalesConsumerWorker.cs   (BackgroundService — đăng ký trong POS.Worker/Program.cs)
    │   ├── Rpt_ReportSaleDetail_Insert.cs
    │   ├── WorkerHealthState.cs
    │   └── WorkerHeartbeatService.cs
    └── Repositories/                   ← gom theo {Domain}; namespace GIỮ NGUYÊN POS.Infrastructure.Repositories[.Interfaces]
        ├── MasterData/
        │   └── ICentralMDRepository.cs / CentralMDRepository.cs
        ├── Sale/
        │   ├── ICentralSaleRepository.cs / CentralSaleRepository.cs
        │   ├── IRptCentralSaleRepository.cs / RptCentralSaleRepository.cs
        │   ├── IRptReportSaleDetailRepository.cs / RptReportSaleDetailRepository.cs
        │   └── IDataRawJsonRepository.cs / DataRawJsonRepository.cs
        ├── Loyalty/
        │   ├── ILoyaltyRepository.cs / LoyaltyRepository.cs
        │   ├── IOfferStaffRepository.cs / OfferStaffRepository.cs
        │   └── IWincodeRepository.cs / WincodeRepository.cs
        ├── CouponVoucher/                  ← 8.1–8.4 + SAP Voucher (dùng chung bảng CpnVchBOMCodeIssue, cột Source)
        │   ├── ICouponRepository.cs / CouponRepository.cs                   ← 8.1/8.2 Coupon (CentralMD, SP usp_SetupCoupon_* + GetHeaderListAsync→usp_CpnVchBOMHeader_GetList cho master list, Source='COUPON')
        │   ├── IVoucherRepository.cs / VoucherRepository.cs                 ← 8.3 Voucher (CentralMD, SP usp_SetupVoucher_*)
        │   ├── IVoucherPublishedRepository.cs / VoucherPublishedRepository.cs ← 8.4 (CentralSales per-store, reuse SP GetTransCpnVchIssueList)
        │   └── IVoucherCodeRepository.cs / VoucherCodeRepository.cs         ← SAP Internal Voucher real-time (CentralMD, SP usp_Voucher_*, CpnVchBOMCodeIssue Source='SAP'; thay ISAPVoucherRepository/bảng Internal_Voucher cũ, nay LEGACY)
        ├── Price/                          ← 9.1/9.3 Bảng giá (CentralMD)
        │   └── IPriceRepository.cs / PriceRepository.cs                     ← reuse SP GetSalesPriceList*; validate TVP + SP usp_SetupSalePrice_Save; Sửa/Xóa giá 9.1 qua usp_SalesPrice_UpdatePrice/_SoftDelete
        └── DataSync/
            └── ISyncRepository.cs / SyncRepository.cs   ← SP1 (GetSyncTablesAsync, Redis cache MD:SyncTableList) + SP2 stream (StreamTableToFilesAsync)
```

---

## MỤC B — Danh sách Interface & Implementation

### POS.Application

| Interface | Implementation | Namespace (interface + impl cùng namespace/folder) | Project |
|-----------|---------------|-----------|---------|
| `ICommonService` | `CommonService` | `POS.Application.Features.Common` | POS.Application |
| `IHealthCheckService` | `HealthCheckService` | `POS.Application.Features.Common` | POS.Application |
| `IAkaChainLoyaltyService` | `AkaChainLoyaltyService` | `POS.Application.Features.Partner` | POS.Application |
| `IGotITService` | `GotITService` | `POS.Application.Features.Partner` | POS.Application |
| `IUrboxService` | `UrboxService` | `POS.Application.Features.Partner` | POS.Application |
| `IDataRawService` | `DataRawService` | `POS.Application.Features.DataSync` | POS.Application |
| `ISyncDataPosService` | `SyncDataPosService` | `POS.Application.Features.DataSync` | POS.Application |
| `IKafkaService` | `KafkaService` | `POS.Application.Features.DataSync` | POS.Application |
| `IMasterDataSyncService` | `MasterDataSyncService` | `POS.Application.Features.DataSync` | POS.Application |
| `ISAPService` | `SAPService` | `POS.Application.Features.Sap` | POS.Application |
| `IGiftService` | `GiftService` | `POS.Application.Features.Gift` | POS.Application |
| `ICouponService` | `CouponService` | `POS.Application.Features.CouponVoucher` | POS.Application |
| `IVoucherService` | `VoucherService` | `POS.Application.Features.CouponVoucher` | POS.Application |
| `IVoucherPublishedService` | `VoucherPublishedService` | `POS.Application.Features.CouponVoucher` | POS.Application |
| `IPriceService` | `PriceService` | `POS.Application.Features.Price` | POS.Application |
| `IBusinessDayService` | `BusinessDayService` | `POS.Application.Features.StoreActivities` | POS.Application |

### POS.Infrastructure — Repositories

> Namespace GIỮ NGUYÊN sau reorg: interface = `POS.Infrastructure.Repositories.Interfaces`, impl = `POS.Infrastructure.Repositories` (chỉ folder gom theo {Domain}).

| Interface | Implementation | Folder | Project |
|-----------|---------------|--------|---------|
| `ICentralMDRepository` | `CentralMDRepository` | MasterData/ | POS.Infrastructure |
| `ICentralSaleRepository` | `CentralSaleRepository` | Sale/ | POS.Infrastructure |
| `IRptCentralSaleRepository` | `RptCentralSaleRepository` | Sale/ | POS.Infrastructure |
| `IRptReportSaleDetailRepository` | `RptReportSaleDetailRepository` | Sale/ | POS.Infrastructure |
| `IDataRawJsonRepository` | `DataRawJsonRepository` | Sale/ | POS.Infrastructure |
| `ILoyaltyRepository` | `LoyaltyRepository` | Loyalty/ | POS.Infrastructure |
| `IOfferStaffRepository` | `OfferStaffRepository` | Loyalty/ | POS.Infrastructure |
| `IWincodeRepository` | `WincodeRepository` | Loyalty/ | POS.Infrastructure |
| `IVoucherCodeRepository` | `VoucherCodeRepository` | CouponVoucher/ | POS.Infrastructure |
| `ICouponRepository` | `CouponRepository` | CouponVoucher/ | POS.Infrastructure |
| `IVoucherRepository` | `VoucherRepository` | CouponVoucher/ | POS.Infrastructure |
| `IVoucherPublishedRepository` | `VoucherPublishedRepository` | CouponVoucher/ | POS.Infrastructure |
| `IPriceRepository` | `PriceRepository` | Price/ | POS.Infrastructure |
| `ISyncRepository` | `SyncRepository` | DataSync/ | POS.Infrastructure |
| _(static, no interface)_ | `SecretProtector` | `POS.Infrastructure.Security` | POS.Infrastructure |

### POS.Infrastructure — AppServices

| Interface | Implementation | Namespace | Project |
|-----------|---------------|-----------|---------|
| `IAkaChainLoyaltyAppService` | `AkaChainLoyaltyAppService` | `POS.Infrastructure.AppServices.Partner` | POS.Infrastructure |
| `IGotITAppService` | `GotITService` (class name) | `POS.Infrastructure.AppServices.Partner` | POS.Infrastructure |
| `IUrboxAppService` | `UrboxService` (class name) | `POS.Infrastructure.AppServices.Partner` | POS.Infrastructure |
| `IKafkaAppService` | `KafkaAppService` | `POS.Infrastructure.AppServices.DataSync` | POS.Infrastructure |

### POS.Infrastructure — Cache / Redis / Messaging / Logging / Files

| Interface | Implementation | Namespace | Project |
|-----------|---------------|-----------|---------|
| `IRedisManager` | `RedisManager` | `POS.Infrastructure.Cache` | POS.Infrastructure |
| `IRedisService` | `RedisService` | `POS.Infrastructure.Redis` | POS.Infrastructure |
| `IRabbitMQProducer` | `RabbitMQProducer` | `POS.Infrastructure.Messaging` | POS.Infrastructure |
| `IKafkaProducer` | `KafkaProducer` | `POS.Infrastructure.Messaging` | POS.Infrastructure |
| `IFileLogHelper` | `FileLogHelper` | `POS.Infrastructure.Logging` | POS.Infrastructure |
| `IKibanaService` | `KibanaService` | `POS.Infrastructure.Logging` | POS.Infrastructure |
| `IFtpFileTransfer` | `FtpFileTransfer` | `POS.Infrastructure.Files` | POS.Infrastructure |
| `IFileArchiveService` | `FileArchiveService` | `POS.Infrastructure.Files` | POS.Infrastructure |
| `ISyncFileLock` | `SyncFileLock` | `POS.Infrastructure.Files` | POS.Infrastructure |
| `IDbConnectionFactory` | `CentralMDConnectionFactory`, `LoyaltyConnectionFactory`, `StagingDbConnectionFactory`, `StoreRoutedConnectionFactory` | `POS.Infrastructure.Database` | POS.Infrastructure |

---

## MỤC C — DI Registration thực tế

### `POS.Application.DependencyInjection.AddApplication()`

| Service (Interface → Impl) | Lifetime | Ghi chú |
|---------------------------|----------|---------|
| `ICommonService` → `CommonService` | Scoped | Business chính: POS, Sale, Shift, KIOS |
| `IAkaChainLoyaltyService` → `AkaChainLoyaltyService` | Scoped | Wrapper → `IAkaChainLoyaltyAppService` |
| `IGotITService` → `GotITService` | Scoped | Wrapper → `IGotITAppService` |
| `IUrboxService` → `UrboxService` | Scoped | Wrapper → `IUrboxAppService` |
| `IDataRawService` → `DataRawService` | Scoped | File sale → Kafka → StagingDB |
| `ISyncDataPosService` → `SyncDataPosService` | Scoped | Sync file POS ↔ server |
| `IHealthCheckService` → `HealthCheckService` | Scoped | Chẩn đoán kết nối hạ tầng |
| `IKafkaService` → `KafkaService` | Scoped | Publish sale messages lên Kafka |
| `ISAPService` → `SAPService` | Scoped | SAP voucher/coupon |
| `IGiftService` → `GiftService` | Scoped | Gift barcode |
| `IMasterDataSyncService` → `MasterDataSyncService` | Scoped | Sinh zip master data + log download |
| `ICouponService` → `CouponService` | Scoped | 8.1/8.2 Coupon — sinh mã Auto + validate + Excel + GetHeaderListAsync (master list Coupon/Voucher) |
| `IVoucherService` → `VoucherService` | Scoped | 8.3 Voucher — validate serial/ngày/items |
| `IVoucherPublishedService` → `VoucherPublishedService` | Scoped | 8.4 — thin wrapper (CentralSales per-store) |
| `IPriceService` → `PriceService` | Scoped | 9.1/9.3 Bảng giá — validate SaveItemPrice + build Pkey; 9.1 Sửa/Xóa giá |
| `IBusinessDayService` → `BusinessDayService` | Scoped | Xác nhận kết thúc ngày — merge `ICentralMDRepository.GetPosTerminalListAsync` (master POS) + `ICentralSaleRepository.GetPosDayStagingAsync` (staging shard); validate rule "tất cả POS đã đóng ngày" trước khi gọi `ConfirmBusinessDayAsync` |

### `POS.Infrastructure.DependencyInjection.AddInfrastructure()`

| Service | Lifetime | Ghi chú |
|---------|----------|---------|
| `CentralMDConnectionFactory` (concrete, no interface) | Singleton | DB Factory — không qua interface |
| `LoyaltyConnectionFactory` (concrete, no interface) | Singleton | DB Factory — không qua interface |
| `StagingDbConnectionFactory` (concrete, no interface) | Singleton | DB Factory — không qua interface |
| `CentralSaleConnectionFactory` (concrete, no interface) | Singleton | DB Factory — CentralSales |
| `StoreRoutedConnectionFactory` (concrete, no interface) | Singleton | DB Factory — routing per-store, cache ServerIP vào Redis |
| `ICentralMDRepository` → `CentralMDRepository` | Scoped | Master Data DB |
| `ICentralSaleRepository` → `CentralSaleRepository` | Scoped | Sales DB (per-store routing) |
| `IRptCentralSaleRepository` → `RptCentralSaleRepository` | Scoped | Report sales (POS.Web dashboard) |
| `IRptReportSaleDetailRepository` → `RptReportSaleDetailRepository` | Scoped | Report sale detail |
| `IDataRawJsonRepository` → `DataRawJsonRepository` | Scoped | StagingDB |
| `ILoyaltyRepository` → `LoyaltyRepository` | Scoped | Loyalty DB |
| `IOfferStaffRepository` → `OfferStaffRepository` | Scoped | Staff discount DB |
| `IWincodeRepository` → `WincodeRepository` | Scoped | WinCode / WinLife DB |
| `IVoucherCodeRepository` → `VoucherCodeRepository` | Scoped | SAP voucher real-time (CpnVchBOMCodeIssue, Source='SAP') |
| `ICouponRepository` → `CouponRepository` | Scoped | 8.1/8.2 Coupon (CentralMD) |
| `IVoucherRepository` → `VoucherRepository` | Scoped | 8.3 Voucher (CentralMD) |
| `IVoucherPublishedRepository` → `VoucherPublishedRepository` | Scoped | 8.4 Voucher phát hành (CentralSales per-store) |
| `IPriceRepository` → `PriceRepository` | Scoped | 9.1/9.3 Bảng giá (CentralMD) |
| `ISyncRepository` → `SyncRepository` | Scoped | SP1 GetSyncTables (Redis cache) + SP2 StreamTableToFiles |
| `IFileArchiveService` → `FileArchiveService` | Singleton | ZipFile.CreateFromDirectory (compression level configurable) |
| `ISyncFileLock` → `SyncFileLock` | Singleton | keyed SemaphoreSlim chống sinh zip trùng |
| `IRedisManager` → `RedisManager` | Singleton | StackExchange.Redis low-level |
| `IRedisService` → `RedisService` | Singleton | High-level Redis wrapper (sử dụng trong code) |
| `IRabbitMQProducer` → `RabbitMQProducer` | Singleton | IAsyncDisposable, tạo IChannel per-publish |
| `IKafkaProducer` → `KafkaProducer` | Singleton | IProducer thread-safe |
| `IFtpFileTransfer` → `FtpFileTransfer` | Singleton | Upload zip qua FluentFTP (managed, chạy trên Linux) |
| `IFileLogHelper` → `FileLogHelper` (factory) | Singleton | baseDirectory từ `Logging:FileLogDirectory` |
| `IKibanaService` → `KibanaService` | Singleton | Serilog → Elasticsearch |
| Named HttpClient `"FMV"` | — | UseCookies=false, BaseAddress per-request |
| Named HttpClient `"GotIT"` | — | BaseAddress per-request |
| `IAkaChainLoyaltyAppService` → `AkaChainLoyaltyAppService` | Scoped | FMV/AkaChain HTTP client |
| `IGotITAppService` → `GotITService` | Scoped | GotIT HTTP client |
| `IUrboxAppService` → `UrboxService` | Scoped | Urbox HTTP client (tạo HttpClient riêng per-call) |
| `IKafkaAppService` → `KafkaAppService` | Scoped | Kafka producer wrapper |

### `POS.Api/Program.cs`

| Registration | Lifetime | Ghi chú |
|-------------|----------|---------|
| Controllers + Newtonsoft.Json | — | DefaultContractResolver (PascalCase), NullValueHandling.Ignore, DateTimeZoneHandling.Local |
| `ValidateModelFilter` (global) | — | Thay ModelStateInvalidFilter mặc định |
| `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true` | — | Cho phép ValidateModelFilter kiểm soát hoàn toàn |
| `MemoryCache` | Singleton | Đã đăng ký, chưa wire vào biz logic (xem TODO trong `BasicAuthHandler.cs`) |
| Authentication `"BasicAuth"` → `BasicAuthHandler` | — | Chỉ áp dụng route api/v2/... |
| `PosApiKeyMiddleware` (`UsePosApiKeyAuth`) | — | Sau Serilog, trước UseAuthentication. Validate X-API (MD5 vs POSDataSetup[X-API]); fail-closed — miễn `/health` + `/swagger/*` |
| `HttpClient` (generic factory) | — | `IHttpClientFactory` |
| Swagger | — | Chỉ đăng ký khi `IsDevelopment()` |
| `app.MapGet("/health", ...)` | — | Health endpoint public (Docker HEALTHCHECK) |

### `POS.Worker/Program.cs`

| Registration | Ghi chú |
|-------------|---------|
| `AddInfrastructure()` | DB, Redis, RabbitMQ, Repos — **KHÔNG** gọi `AddApplication()` (worker không cần HTTP AppServices) |
| `AddHostedService<PosSalesConsumerWorker>()` | Consumer queue `pos_sales` — retry insert CentralSale |
| `AddSerilogWithElastic()` (overload `HostApplicationBuilder`) | Cùng ES sink với POS.Api, index riêng `pos-worker-logs-*` |

---

## MỤC D — Repository: method signatures

### `ICentralMDRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<MMLSchemeHeader?> GetMMLSchemeHeaderAsync(string code, CancellationToken ct = default)
Task<List<MMLSchemeItem>?> GetMMLSchemeItemAsync(CancellationToken ct = default)
Task<MMLSchemeResponse?> GetMMLSchemeResponseAsync(string headerCode, string code, CancellationToken ct = default)
Task<LoyaltyRateDto?> GetLoyaltyRateDataAsync(string code, CancellationToken ct = default)
Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default)
Task<ItemPointsMemberDto?> GetItemPointsMemberAsync(string pointsCode, string itemNo, string uom, CancellationToken ct = default)
Task<SysWebApiDto?> GetSysWebApiAsync(string appCode, CancellationToken ct = default)
Task<List<POSDataSetupModel>?> GetPOSDataSetupAsync(CancellationToken ct = default)          // cache Redis 12h
Task<List<StoreSetConfig>?> GetStoreSetConfigAsync(CancellationToken ct = default)            // cache Redis 12h
Task<List<StoreDto>> GetStoreListAsync(CancellationToken ct = default)                         // StoreNo+Name, cache MD:StoreList 12h — store picker UI
Task<List<BranchDto>> GetBranchListAsync(CancellationToken ct = default)                       // No+Description, cache MD:BranchList 12h — combobox Chi nhánh
Task<List<BranchAdminDto>> GetBranchAdminListAsync(CancellationToken ct = default)             // No+Description+Address+VATRegistrationNo, không cache — ProvincesPage
Task<bool> BranchCodeExistsAsync(string branchNo, CancellationToken ct = default)              // check trùng mã chi nhánh
Task<bool> CreateBranchAsync(BranchCreateDto dto, CancellationToken ct = default)              // INSERT dbo.Branch, invalidate MD:BranchList
Task<bool> UpdateBranchInfoAsync(string branchNo, string description, string? address, string? vatRegistrationNo, CancellationToken ct = default)  // UPDATE Description/Address/VATRegistrationNo, invalidate MD:BranchList
Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model, CancellationToken ct = default)
Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress, CancellationToken ct = default)
Task<List<POSDataSetupModel>?> GetDataSetupListAsync(CancellationToken ct = default)          // không cache
Task<List<POSVersionModel>?> GetPOSVersionAsync(CancellationToken ct = default)
Task<bool> CheckCouponLineAsync(string itemNo, string barCode, CancellationToken ct = default)
Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default)   // cache Redis Hash MD:CpnVchBOMHeader (positive-only)
Task<bool> InsertSignalStoreAsync(SignalStoreModel model, CancellationToken ct = default)
// ── Web admin: danh sách POS monitor / terminal ──
Task<List<PosMonitorStatusDto>> GetPosMonitorStatusAsync(CancellationToken ct = default)
Task<List<PosTerminalListDto>> GetPosTerminalListAsync(CancellationToken ct = default)
Task<bool> UpdatePosTerminalAsync(string posNo, string ipAddress, bool? status, string? billNoseri, string updatedBy, CancellationToken ct = default)
Task<List<StoreListDto>> GetStoreAdminListAsync(CancellationToken ct = default)
Task<bool> StoreCodeExistsAsync(string storeNo, CancellationToken ct = default)                 // check trùng mã CH
Task<bool> CreateStoreAsync(StoreCreateDto dto, CancellationToken ct = default)                  // INSERT dbo.Store, invalidate MD:StoreList
Task<bool> UpdateStoreClosingMethodAsync(string storeNo, int closingMethod, CancellationToken ct = default)  // đổi trạng thái đóng/mở, invalidate MD:StoreList
Task<List<TenderTypeSetupDto>> GetTenderTypesAsync(CancellationToken ct = default)
// ── POSDataSetup CRUD (Web admin UI) — invalidate Redis MD:POSDataSetup sau mỗi write ──
Task<List<POSDataSetupAdminDto>> GetPOSDataSetupAdminListAsync(CancellationToken ct = default)
Task<POSDataSetupAdminDto?> GetPOSDataSetupByCodeAsync(string code, CancellationToken ct = default)
Task<(bool success, bool duplicateCode)> InsertPOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default)
Task<bool> UpdatePOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default)    // KHÔNG đụng Counter/Pkey
Task<bool> DeletePOSDataSetupAsync(string code, CancellationToken ct = default)
// ── Danh mục Nhân viên — dbo.Staff (migrate 5.1 + create/change password) ──
Task<(List<EmployeeListItemDto> Items, int Total)> GetEmployeeListAsync(EmployeeListFilter filter, CancellationToken ct = default)   // SP GetEmployeeList, paging server-side
Task<List<EmployeeListItemDto>> ExportEmployeeListAsync(EmployeeListFilter filter, CancellationToken ct = default)                  // SP GetEmployeeList_Export
Task<bool> StaffCodeExistsAsync(string staffCode, CancellationToken ct = default)                                                   // check trùng mã NV
Task<bool> CreateEmployeeAsync(EmployeeCreateDto dto, CancellationToken ct = default)          // INSERT Staff — Password plain text (contract POS), Counter=MAX+1, Pkey=StaffCode
Task<bool> ChangeEmployeePasswordAsync(string staffCode, string newPassword, CancellationToken ct = default)  // UPDATE Password, chỉ khi Blocked=0/NULL, Counter=MAX+1
// ── BankPOS (migrate 5.5) ──
Task<List<BankPOSListDto>> GetBankPOSListAsync(CancellationToken ct = default)
Task<(bool success, bool duplicateCode)> SaveBankPOSAsync(BankPOSSaveDto dto, string actor, CancellationToken ct = default)
Task<bool> DeleteBankPOSAsync(string bankPOSCode, CancellationToken ct = default)
Task<List<BankDropdownDto>> GetBankListForDropdownAsync(CancellationToken ct = default)         // cache Redis 12h
// ── Product List/Create (migrate 6.1/6.2) — dbo.Item + dbo.Barcode ──
Task<(List<ProductListItemDto> Items, int Total)> GetProductListAsync(ProductListFilter filter, CancellationToken ct = default)
Task<List<ProductListItemDto>> ExportProductListAsync(ProductListFilter filter, CancellationToken ct = default)
Task<List<PosVatCodeDto>> GetPosVatCodesAsync(CancellationToken ct = default)                   // cache MD:PosVatCodes 12h
Task<List<ArticleTypeDto>> GetArticleTypesAsync(CancellationToken ct = default)                  // cache MD:ArticleTypes 12h
Task<List<UnitOfMeasureDto>> GetUnitOfMeasuresAsync(CancellationToken ct = default)              // cache MD:UnitOfMeasures 12h
Task<(bool Success, string ItemNo, string Message)> CreateProductAsync(ProductCreateDto dto, CancellationToken ct = default)
// ── Product Lock (migrate 6.4) — dbo.ItemBlock, Pkey="{StoreNo}-{ItemNo}" ──
Task<(List<ProductLockItemDto> Items, int Total)> GetProductLockListAsync(ProductLockFilter filter, CancellationToken ct = default)
Task<(bool Success, string Message)> SaveProductLockAsync(ProductLockSaveDto dto, CancellationToken ct = default)
// ── Dashboard Audit Log — try/catch nội bộ, caller không cần bọc thêm ──
Task InsertDashboardAuditLogAsync(string actor, string action, string entityType, string entityKey, string? oldValueJson = null, string? newValueJson = null, CancellationToken ct = default)
```

### `ICentralSaleRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode, CancellationToken ct = default)
Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode, CancellationToken ct = default)
Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model, CancellationToken ct = default)
Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate, CancellationToken ct = default)
Task<bool> CheckSaleReturnAsync(string orderNo, CancellationToken ct = default)
Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo, CancellationToken ct = default)
Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal, CancellationToken ct = default)
Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo, CancellationToken ct = default)
Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model, CancellationToken ct = default)
Task<(bool, string)> InInsertToTableByJson(string storeNo, string posNo, string transactionId, string message, CancellationToken ct = default)

// Business Day Confirm (Xác nhận kết thúc ngày) — connection per-store qua StoreRoutedConnectionFactory
Task<List<PosDayStagingDto>> GetPosDayStagingAsync(string storeNo, DateTime businessDate, CancellationToken ct = default)
Task<BusinessDayConfirmDto?> GetBusinessDayConfirmAsync(string storeNo, DateTime businessDate, CancellationToken ct = default)
Task<ConfirmBusinessDayResult> ConfirmBusinessDayAsync(ConfirmBusinessDayRequest request, CancellationToken ct = default)
```

### `IDataRawJsonRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<(bool Success, string Message, string? Detail)> InsertInvoiceCreatedAsync(
    List<InvoiceCreated> invoiceCreated, string connectStringDb, CancellationToken ct = default)

Task<(bool Success, string Message, object? Data, HttpStatusCode StatusCode)> ValidateTransactionAsync(
    StoreDto storeInfo, string connectStringDb, string orderNo, string appCode = "WCM", CancellationToken ct = default)

Task<(bool Success, string Message)> InsertDataRawJsonAsync(
    string connectStringDb, List<DataRawJsonDto> request, CancellationToken ct = default)

Task<string?> GetMessageWarningsVATCheckAsync(string actionType, CancellationToken ct = default)

Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default)
```

### `ILoyaltyRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<List<StoreMappingModel>?> GetLoyaltyStoreMappingAsync(CancellationToken ct = default)
string ConnectStringLoyaltyDb()
Task<bool> InsertWinPayAccumulateAsync(IDbConnection db, WinPayAccumulationData winPayAccumulationData, bool isRetry = false)
bool InsertMemberRemnItem(List<MemberRemnItem> memberRemnItems, string parentKeyMemberRemnItem, ref string errMess)
Task<Tuple<bool, string>> RefundMemberRemnItemAsync(string orderNo, string memberCard, CancellationToken ct = default)
bool UpdateWinMoneyConversion(WinMoneyConversion winMoneyConversion, ref string errMess)
Task<bool> UpdateStatusLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, CancellationToken ct = default)
Task<List<LoggingLoyaltyDto>?> GetLoggingLoyaltyAsync(string actionType, string status, CancellationToken ct = default)
Task<List<LoggingLoyaltyDto>?> GetListLoggingLoyaltyAsync(string orderNo, string actionType, CancellationToken ct = default)
Task<LoggingLoyaltyDto?> InsertLoggingLoyaltyAsync(LoggingLoyaltyDto loggingLoyaltyDto, string orderNo = "", bool isRetry = false, CancellationToken ct = default)
Task<GiftCodeDto?> GetGiftCodeAsync(string orderNo, string saleType, string memberCard, int amount, CancellationToken ct = default)
Task<bool> UpdateMemoryCacheConfigAsync(string code, bool isBlocked, CancellationToken ct = default)
Task<MemoryCacheConfig?> GetMemoryCacheConfigAsync(string code, CancellationToken ct = default)
```

### `IOfferStaffRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<(bool Success, string Message)> InsertOfferStaffTransactionAsync(OfferStaffTransactionDto request, CancellationToken ct = default)
Task<OfferStaffRemnDto?> GetOfferStaffRemnAsync(string staffCode, string phoneNumber, string clubCode, CancellationToken ct = default)
Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync(CancellationToken ct = default)
```

### `IWincodeRepository` (`POS.Infrastructure.Repositories.Interfaces`)

```csharp
Task<List<WinCodeCustomerDto>?> GetWinCodeCustomerAsync(string phoneNumber, CancellationToken ct = default)
Task<Tuple<bool, string>> UpdateWincodeCustomerAsync(WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default)
Task<Tuple<bool, string>> InsertWincodeCustomerAsync(WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default)
```

---

## MỤC E — Service & AppService: method signatures

> **Namespace sau reorg** (các nhãn `*.Interfaces` ở header bên dưới là CŨ — chữ ký method KHÔNG đổi nên giữ nguyên):
> Application services = `POS.Application.Features.{Common|Partner|DataSync|Sap|Gift}`;
> AppServices = `POS.Infrastructure.AppServices.{Partner|DataSync}`.
> **Service mới chưa liệt kê chữ ký ở đây** (xem code): `ISAPService` (`Features.Sap`), `IGiftService` (`Features.Gift`);
> Repositories mới (Mục D): `IVoucherCodeRepository`, `IRptCentralSaleRepository`, `IRptReportSaleDetailRepository`.

### Application Services

#### `ICommonService` (`POS.Application.Interfaces`)

```csharp
Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode)
Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode)
Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model)
Task<bool> InsertSignalStoreAsync(SignalStoreModel model)
Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate)
Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model)
Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress)
Task<List<POSDataSetupModel>> GetDataSetupAsync()
Task<List<POSVersionModel>> GetPOSVersionAsync()
Task<bool> CheckSaleReturnAsync(string orderNo)
Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo)
Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal)
Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo)
Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoModel model)
Task<InsuranceModel?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model)
Task<CheckTotalBillResponse?> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest model)
Task LogSaleKiosAsync(LogSaleKiosModel model)
Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo)
Task<(bool Success, int Code, string Message, RewardCodeSendModel? Data)> SendCodeRewardAsync(RewardCodeRequest model)
Task WriteLogApiAsync(LogAPIModel model)
```

#### `IAkaChainLoyaltyService` (`POS.Application.Interfaces`)

```csharp
Task<ResultResponse> GetMemberProfileAsync(string key, string value)
Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model)
Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model)
Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model)
```

#### `IGotITService` (`POS.Application.Interfaces`)

```csharp
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
    CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
    UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
```

#### `IUrboxService` (`POS.Application.Interfaces`)

```csharp
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
    CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
    UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
```

#### `IDataRawService` (`POS.Application.Interfaces`)

```csharp
Task<(bool Success, string Message)> CreateFileSODFakeAsync(string storeNo, string localPath, CancellationToken ct = default)
Task ProcessFileToStagingDBAsync(string pathFile, string fileName, string? extension, string pathBackup, CancellationToken ct = default)
Task<List<string>> RetryInsDataRawToDBAsync(CancellationToken ct = default)
```

#### `ISyncDataPosService` (`POS.Application.Interfaces`)

```csharp
string MapFtpPath(string relativePath)
string MapSitePath(string relativePath)
Task<string?> GetPosDataSetupValueAsync(string code, CancellationToken ct = default)
Task<List<PathFileAPIModel>> GetFileFromServerApiAsync(
    string pathSync, string folderFile, string typeSync, string syncApi, string ipServer,
    CancellationToken ct = default)
(bool Allowed, int Processing) CheckSodQueueLimit(string ipSrvKey, int limit)
bool SodGlobalMarkerExists()
string EnqueueSodRequest(string ipSrvKey, string posTerminal)
void DequeueSodRequest(string fullKey)
Task UploadFileLogToFtpAsync(string pathFileApi, string pathFtpServer, CancellationToken ct = default)
Task<List<PathFileAPIModel>> DownloadFileUpgradeToolShareFolderAsync(string ipServer, CancellationToken ct = default)
Task DeleteFileExistAsync(List<PathFileAPIModel> model, string ipServerHost)
Task<GetMasterDataFileResult> PushStartOfDayDataAsync(string siteCode, string posTerminal, CancellationToken ct = default)  // POS.Web nút SyncData: sinh zip full-data ALL vào {FtpRootPath}\SyncDataPos\POS\CHANGE\{site}\{terminal} (MapFtpPath, bám controller); delegate IMasterDataSyncService.EnsureMasterDataFileAsync (không đổi logic sinh file)
string ResolveFtpPhysicalPath(string? posPath)  // UNC POS gửi (\\ip\FTPBLUEPOS\...) → physical path local dưới FtpRootPath; dùng chung DowloadFileStream + DeleteFileFromFTP
```

#### `IHealthCheckService` (`POS.Application.Interfaces`)

```csharp
Task<List<HealthCheckItemDto>> CheckAllAsync(string? storeNo, CancellationToken ct = default)
```

#### `IKafkaService` (`POS.Application.Interfaces`)

```csharp
Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos)
```

#### `IBusinessDayService` (`POS.Application.Features.StoreActivities`)

```csharp
Task<List<PosDayStagingDto>> GetPosDayStagingAsync(string storeNo, DateTime businessDate, CancellationToken ct = default)
Task<BusinessDayConfirmDto?> GetConfirmStatusAsync(string storeNo, DateTime businessDate, CancellationToken ct = default)
Task<DateTime?> GetCurrentBusinessDateAsync(string storeNo, CancellationToken ct = default)
Task<ConfirmBusinessDayResult> ConfirmBusinessDayAsync(string storeNo, DateTime businessDate, string confirmedBy, bool allowForceConfirm = false, CancellationToken ct = default)
```
> `allowForceConfirm=true` (role ITOps/SystemAdmin) bỏ qua guard "còn POS chưa đóng ngày" (force EOD); StoreOperator luôn `false`.

### Infrastructure — AppServices

#### `IAkaChainLoyaltyAppService` (`POS.Infrastructure.AppServices.Interfaces`)

```csharp
Task<ResultResponse> GetMemberProfileAsync(string key, string value)
Task<ResultResponse> AddTransactionAsync(VinIDSalesRequest model)
Task<ResultResponse> ReturnTransactionAsync(VinIDRefundRequest model)
Task<ResultResponse> CheckCouponAsync(CheckVoucherPartnerPOSRequest model)
```

#### `IGotITAppService` (`POS.Infrastructure.AppServices.Interfaces`)

```csharp
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
    CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
    UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
```

#### `IUrboxAppService` (`POS.Infrastructure.AppServices.Interfaces`)

```csharp
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
    CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
    UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
```

#### `IKafkaAppService` (`POS.Infrastructure.AppServices.Interfaces`)

```csharp
Task<ResultResponse> PushSalesToTopic(List<KafkaMessageDto> kafkaMessageDtos, CancellationToken ct = default)
```

### Infrastructure — Redis / Messaging / Logging

#### `IRedisService` (`POS.Infrastructure.Redis`)

```csharp
// Hash operations
Task<T?> HashGetAsync<T>(string key, string field)
T? HashGet<T>(string key, string field)
Task HashSetAsync<T>(string key, string field, T value, int? ttlSeconds = null)
void HashSet<T>(string key, string field, T value, int? ttlSeconds = null)
void HashDelete(string key, string field)

// String operations
Task<T?> StringGetAsync<T>(string key)
string? StringGetRaw(string key)
void StringSet<T>(string key, T value, int? ttlSeconds = null)
void StringSetRaw(string key, string value, TimeSpan? ttl = null)

// Key operations
bool KeyExists(string key)
void Delete(string key)
List<string> GetKeysByPattern(string pattern)   // SCAN pattern hẹp
```

#### `IRedisManager` (`POS.Infrastructure.Cache`)

```csharp
// String
Task<string?> GetStringAsync(string key)
Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
Task<bool> DeleteAsync(string key)

// Hash
Task<T?> HashGetAsync<T>(string hashKey, string hashField)
Task<bool> HashSetAsync<T>(string hashKey, string hashField, T value, int ttlSeconds = 0)
Task<bool> HashDeleteAsync(string hashKey, string hashField)
Task<IDictionary<string, T>> HashGetAllAsync<T>(string hashKey)

// List
Task<long> ListRightPushAsync(string key, string value)

// Utility
Task<bool> KeyExistsAsync(string key)
Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
Task<List<string>> GetKeysByPatternAsync(string pattern)
```

#### `IKibanaService` (`POS.Infrastructure.Logging`)

```csharp
void LogRequest(string endpoint, string posNo, string requestBody)
void LogResponse(string endpoint, string posNo, long responseTimeMs, string note, string responseBody)
void LogException(string endpoint, string posNo, int errorCode, string note, string errorDetail)
void LogInfo(string endpoint, string posNo, string message)
```

#### `IFileLogHelper` (`POS.Infrastructure.Logging`)

```csharp
void WriteLogs(string message)
void WriteExpLogs(string function, Exception ex)
```

#### `IFtpFileTransfer` (`POS.Infrastructure.Files`)

```csharp
void UploadZipFiles(string sourcePathFolder, string destPathFolderFtp,
    string ftpServer, string ftpUsername, string ftpPassword)
```

#### `IDbConnectionFactory` (`POS.Infrastructure.Database`)

```csharp
Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
IDbConnection CreateOpenConnection()
```

---

## MỤC F — DTOs & Models có sẵn

### POS.Common root

| Class | Namespace | Các field chính |
|-------|-----------|----------------|
| `ResultResponse` | `POS.Common` | `Status` (HttpStatusCode), `Message` (string), `Data` (object?), `MessageTechnical` (string) |

### POS.Common.Dtos (root)

| Class | File | Các field chính |
|-------|------|----------------|
| `AuthDto` | AuthDto.cs | `AppCode`, `StoreNo`, `PosNo` |
| `HttpResponseBlueDto` | HttpResponseBlueDto.cs | Response wrapper HTTP cũ |
| `KafkaMessage` | KafkaMessage.cs | Kafka message wrapper |
| `NotifyConfigDto` | NotifyConfigDto.cs | Notify config |
| `RabbitMessageDto` | RabbitMessageDto.cs | RabbitMQ message |
| `RedisDto` | RedisDto.cs | Redis data models |
| `SMSMessage` | SMSMessage.cs | SMS payload |
| `SysWebApiDto` | SysWebApiDto.cs | `AppCode`, `Host`, `UserName`, `Password`, `Timeout`, ... |
| `SysWebApiUserDto` | SysWebApiUserDto.cs | SysWebApi user config |

### POS.Common.Dtos.CentralMD

| Class | File | Các field chính |
|-------|------|----------------|
| `StoreDto` | CentralMDDto.cs | `StoreNo`, `No`, `Name`, `Address`, `TaxCode`, `ConnectionString`, ... |
| `BranchDto` | CentralMDDto.cs | `No`, `Description` — dbo.Branch, dùng cho combobox Chi nhánh |
| `BranchAdminDto` | CentralMDDto.cs | `No`, `Description`, `Address`, `VATRegistrationNo` — dbo.Branch, dùng cho DataTable ProvincesPage |
| `BranchCreateDto` | CentralMDDto.cs | `No`, `Description`, `Address`, `VATRegistrationNo` — payload tạo mới chi nhánh (ProvincesPage) |
| `StoreCreateDto` | CentralMDDto.cs | `StoreNo`, `Name`, `Address`, `BranchNo`, `ClosingMethod` — payload tạo mới cửa hàng (StorePage) |
| `StoreSetup` | CentralMDDto.cs | `StoreNo`, `Code`, `Value`, `Status` |
| `SysWebApiConfig` | CentralMDDto.cs | `Code`, `Name`, `Prefix`, `ConnectionString`, `Blocked`, ... |
| `StoreSetConfig` | CentralMDDto.cs | extends SysWebApiConfig + `StoreNo` |
| `SyncTableList` | CentralMDDto.cs | `FileName`, `TableName`, `Action`, `ProcedureName`, `ProcessID`, `Data` |
| `LoyaltyRateDto` | CentralMDDto.cs | `FromDate`, `ToDate`, `Code`, `Rate`, `Blocked`, `CardType` |
| `ItemPointsMemberDto` | CentralMDDto.cs | `PointsCode`, `ItemNo`, `Barcode`, `ItemName`, `Uom`, `ShelfLife`, `Blocked` |
| `MMLSchemeHeader` | CentralMDDto.cs | `HeaderCode`, `FromDate`, `ToDate`, `MinAmount`, `IsMember`, `IsCallAPI`, `Ref1-5` |
| `MMLSchemeItem` | CentralMDDto.cs | `HeaderCode`, `Code`, `ItemNo`, `UOM`, `CategoryCode`, `Enabled`, `Ref1-5` |
| `MMLSchemeResponse` | CentralMDDto.cs | `HeaderCode`, `Code`, `Title`, `Link`, `IsGenQR`, `Enabled`, `Description` |
| `MMLSchemeRequest` | CentralMDDto.cs | `PosNo`, `OrderNo`, `StoreNo`, `Code`, `MemberCardNo`, `IsMember`, `Items`, `Payments` |
| `CpnVchBOMHeaderDto` | CentralMDDto.cs | `ItemNo`, `DiscountType`, `DiscountValue`, `MaxAmount`, `StartingDate`, `EndingDate`, `IsMultiUse`, ... |
| `CpnVchBOMLineDto` | CentralMDDto.cs | `ItemNo`, `LineNo`, `LineItemNo`, `Barcode` |
| `ItemDto` | CentralMDDto.cs | `ItemNo`, `Uom`, `DivisionCode`, `TaxGroupCode`, `VATPercent`, `IsVAT` |

### POS.Common.Dtos.POS

| Class | File | Các field chính |
|-------|------|----------------|
| `KafkaMessageDto` | KafkaMessageDto.cs | Kafka message cho sale data |
| `KafkaMessagePOS` | KafkaMessagePOS.cs | POS Kafka message |
| `POSRequest` | POSRequest.cs | Base POS request |
| `ValidateTransactionDto` | ValidateTransactionDto.cs | Validate transaction |
| `GiftBarcodeRequest` | Gift/GiftBarcodeRequest.cs | Gift barcode |

### POS.Common.Dtos.POS.Common (CommonDtos.cs)

File này chứa nhiều model dùng cho CommonController:
`POSDataSetupModel`, `StoreSetConfig`, `POSMonitorInsertRequest`, `POSMonitorInsertResponse`,
`PosTerminalModel`, `POSVersionModel`, `BusinessDateResponse`, `BussinessDateOpenModel`,
`SignalStoreModel`, `ShiftHeaderModel`, `SaleTableModel`, `TransHeader`, `POSDocumentNoModel`,
`TransHeaderOrderModel`, `POSEOD_APIModel`, `CheckTotalBillResponse`, `KiosInsertSaleRequest`,
`KiosInsertSalePOSRequest`, `SyncSaleObject`, `LogSaleKiosModel`, `KiosCheckOrderResponse`,
`UpdateOrderInfoModel`, `ResponseUpdateTransModel`, `InsuranceModel`, `LogAPIModel`,
`LoggingElastic`, `DeleteFileModel`, `ListFileNameModel`, `StoreMappingModel`

### POS.Common.Dtos.PartnerApi

| Class | File | Các field chính |
|-------|------|----------------|
| `CheckVoucherPartnerPOSRequest` | CheckVoucherPartnerDto.cs | `Partner`, `PosNo`, `StoreNo`, `ListCode`, ... |
| `UpdateStatusVoucherPartnerRequest` | CheckVoucherPartnerDto.cs | `Partner`, `PosNo`, `ListCode`, ... |
| `DataVoucherPartnerResponse` | CheckVoucherPartnerDto.cs | Response từ partner |
| `SetKeyRedis` | SetKeyRedis.cs | Redis key setter |
| `UrboxDto` | UrboxDto.cs | Urbox request/response |
| `UrboxProducts` | UrboxDto.cs | Urbox product list |

### POS.Common.Dtos.Loyalty

| Class | Namespace | Các field chính |
|-------|-----------|----------------|
| `LoyaltyBaseDto` | Dtos.Loyalty | Base fields cho loyalty |
| `TransactionLoyaltyDto` + `LoggingLoyaltyDto` | Dtos.Loyalty | Transaction + logging |
| `InfoMemberDto` | Dtos.Loyalty | `MemberCard`, `Phone`, `Name`, `Points`, ... |
| `VinIDSalesRequest` | Dtos.Loyalty | AkaChain/FMV add transaction request |
| `VinIDRefundRequest` | Dtos.Loyalty | AkaChain/FMV refund request |
| `PaymentEntryLoyalty` | Dtos.Loyalty | Payment entry trong loyalty transaction |
| `MemberRemnItem` | Dtos.Loyalty | Member item remnant |
| `WinPayAccumulationData` | Dtos.Loyalty | WinPay accumulation |
| `GiftCodeDto` | Dtos.Loyalty | Gift code data |
| `MemoryCacheConfig` | Dtos.Loyalty | Cache block config |
| `CXDto` | Dtos.Loyalty.CX | CrownX data |
| `MemberBusinessDto` | Dtos.Loyalty.MemberBusiness | Member business data |
| `ProgramPointsDto` | Dtos.Loyalty.ProgramPoints | Program points |
| `WinCodeCustomerDto` | Dtos.Loyalty.WinCode | WinCode customer |
| `WinLife_UpdatePromotions_POS_Request` | Dtos.Loyalty.WinCode | WinLife promotion |
| `WinScoreDto` | Dtos.Loyalty.WinScore | WinScore data |

### POS.Common.Dtos.StagingDB

| Class | File | Các field chính |
|-------|------|----------------|
| `DataJsonDto` | DataJsonDto.cs | JSON data payload |
| `DataRawJsonDto` | DataRawJsonDto.cs | Raw JSON data từ POS |
| `StagingDBConfigDto` | StagingDBConfigDto.cs | StagingDB config |

### POS.Common.Dtos.Ops

| Class | File | Các field chính |
|-------|------|----------------|
| `HealthCheckItemDto` | HealthCheckItemDto.cs | `Name` (string), `Ok` (bool), `Detail` (string?) |
| `Ops_Logging` | Ops_Logging.cs | Ops logging record |
| `Ops_Monitoring` | Ops_Monitoring.cs | Server monitoring record |

### POS.Common.Dtos.CentralSale

| Class | File | Các field chính |
|-------|------|----------------|
| `EosDayDto` | EosDayDto.cs | `StoreNo`, `BussinessDate`, `TotalShifts`, `ClosedShifts`, `OpenShifts`, `TotalRevenue`, `TotalTransactions`, `TotalTienHeThong`, `TotalTienMat`, `TotalChenLech` — dùng bởi `GetEosDayListAsync` |
| `EosShiftDto` | EosShiftDto.cs | per-shift: `StoreNo`, `PosTerminal`, `BussinessDate`, `ShiftNumber`, `BeginAmount`, `TienMat`, `TienHeThong`, `ChenLech`, `StaffCode`, `OpenShiftDate`, `CloseShiftDate`, `IsShiftClosed`, `TotalRevenue`, `TransactionCount` |
| `PosDayStagingDto` | PosDayStagingDto.cs | 1 dòng/POS terminal (trang Xác nhận kết thúc ngày): `PosTerminal`, `Placement`, `IsClosed`, `CloseTime`, `LastSaleTime`, `TotalRevenue`, `TienMat`, `CustomerCount`, `TotalQuantity` |
| `BusinessDayConfirmDto` | BusinessDayConfirmDto.cs | `StoreNo`, `BusinessDate`, `TotalRevenue`, `TotalShifts`, `ConfirmedBy`, `ConfirmedDate` — map bảng `dbo.BusinessDayConfirm` (DB CentralSale theo store, KHÔNG phải CentralMD) |
| `ConfirmBusinessDayRequest` | ConfirmBusinessDayRequest.cs | `StoreNo`, `BusinessDate`, `TotalRevenue`, `TotalShifts`, `ConfirmedBy` |
| `ConfirmBusinessDayResult` | ConfirmBusinessDayResult.cs | `Success` (bool), `Message`, `NewBusinessDate` |

### Các domain DTO khác (có file, chưa đọc chi tiết)

| Domain | File | Ghi chú |
|--------|------|---------|
| AkaChain | AkaChainDto.cs | FMV/AkaChain specific models |
| B2B | TransHeaderB2BDto, TransHistoryB2BDto, TransLineB2BDto | B2B trans |
| Capillary | ~12 files (Customer, Transaction, Point, Voucher, ...) | Capillary loyalty models |
| Coupon | CouponDto.cs | Coupon domain |
| CXVoucher | CXVoucherDto.cs | CrownX voucher |
| DRW | UpdateStatusSfaffDiscountDto.cs | Staff discount |
| FileModel | FileModelDto.cs | `PathFileAPIModel`, `ListFileNameModel`, ... |
| Giftee | GifteeDto.cs | Giftee partner |
| GotIT | GotITDto.cs | GotIT partner |
| LogService | LogServiceDto.cs | Log service |
| MSN | MSNDto.cs | MSN / OfferStaff |
| Reward | RewardDto.cs | `RewardCodeRequest`, `RewardCodeSendModel` |
| ROP | ROPDto.cs | ROP voucher |
| Tax | InvoiceCreated.cs, TaxCustInfo.cs | Tax / e-invoice |
| Telegram | MessageToTellegram.cs, NotifyTelegram.cs | Telegram notify |
| TopupVoucherVinID | TopupVoucherVinIDDto.cs | VinID top-up |
| Vouchers | VoucherDto.cs, VoucherStatusResponseDto.cs | Generic voucher |
| WinCare | WinCareDto.cs | WinCare partner |
| WinCustomer | WinCustomerDto.cs | WinCustomer service |
| WinMoney | WinMoneyConversion.cs | WinMoney conversion |
| Winpay | WinpayDto.cs | Winpay partner |
| WinX | WinXDto.cs | WinX partner |

### POS.Common Enums (25 files)

| Enum file | Ghi chú |
|-----------|---------|
| ADConnectionStatus | AD connection states |
| ApiEnum | API status codes |
| AppCodeEnum | Application codes |
| CapillaryEnum | Capillary loyalty states |
| CXEnum | CrownX states |
| DiscountTypeEnum | Discount types |
| EnumLogin | Login status |
| EnvironmentEnum | DEV/UAT/PROD |
| EStatus | General status |
| EStatusResponse | Response status |
| GiftStatusEnum | Gift states |
| KafkaEnum | Kafka message types |
| LoyaltyEnum | Loyalty program states |
| MemberBusinessesEnum | Member business types |
| OpsDashboardEnum | Ops monitoring types |
| PartnerEnum | Partner codes |
| PrefixEnum | Card prefix types |
| SAP_PLH_Enum | SAP/PLH enum |
| StampEnum | Stamp types |
| SystemEnum | System codes |
| TelegramEnum | Telegram message types |
| VATEnum | VAT types |
| VoucherROPEnum | ROP voucher types |
| WinLifeRegisterEnum | WinLife registration states |
| WinpayEnum | Winpay states |

### POS.Common Redis Constants

| Key | Giá trị | Dùng cho |
|-----|---------|---------|
| `Redis_Key_ItemPointsMember` | `"MD:ItemPointsMember"` | Hash lookup per item |
| `Redis_Key_LoyaltyRate` | `"MD:LoyaltyRate"` | Hash lookup per code |
| `Redis_Key_MMLSchemeHeader` | `"MD:MMLSchemeHeader"` | Hash |
| `Redis_Key_MMLSchemeItem` | `"MD:MMLSchemeItem"` | Hash |
| `Redis_Key_MMLSchemeResponse` | `"MD:MMLSchemeResponse"` | Hash |
| `Redis_Key_SysWebApi` | `"SysWebApi"` | Hash lookup per appCode |
| `Redis_Key_POSDataSetup` | `"POSDataSetup"` | String (full list) |
| `Redis_Key_GetFileFromFTP` | `"GetFileFromFTP"` | String — SOD queue counter |
| `Redis_Key_MemoryCacheConfig` | `"BLUEPOS:Loyalty_MemoryCacheConfig"` | Hash |
| `Redis_Key_Loyalty_MemberPoints` | `"BLUEPOS:Loyalty_MemberPoints"` | Sharded hash |
| `Redis_Key_Loyalty_BalancePoints` | `"BLUEPOS:Loyalty_BalancePoints"` | Sharded hash (3 chars) |
| `Redis_Key_Loyalty_RedeemPoints` | `"BLUEPOS:Loyalty_RedeemPoints"` | Sharded hash |
| `Redis_Key_Stores` | `"Stores"` | Full store list |
| `Redis_Key_StoresMappingVinID` | `"StoresMappingVinID"` | VinID store mapping |
| `Redis_Key_NotifyTelegram` | `"NotifyTelegram"` | Notify config |
| `Redis_Key_NotifyConfig` | `"NotifyConfig"` | Notify config |

---

## MỤC G — Configuration keys

_(Nguồn: `src/POS.Api/appsettings.json` — giá trị nhạy cảm đã ẩn)_

| Key path | Kiểu | Ghi chú |
|----------|------|---------|
| `AllowedHosts` | string | `"*"` |
| `Logging:FileLogDirectory` | string | `"D:\\ROOT\\Logs"` |
| `Logging:LogLevel:Default` | string | `"Information"` |
| `Serilog:MinimumLevel:Default` | string | `"Information"` |
| `Serilog:MinimumLevel:Override:Microsoft` | string | `"Warning"` |
| `Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime` | string | `"Information"` |
| `Serilog:MinimumLevel:Override:System` | string | `"Warning"` |
| `Elasticsearch:Nodes` | string[] | `["http://10.x.x.x:9200"]` |
| `Elasticsearch:IndexFormat` | string | `"pos-api-logs-{0:yyyy.MM.dd}"` |
| `Elasticsearch:Username` | string | _(ẩn)_ |
| `Elasticsearch:Password` | string | _(ẩn)_ |
| `RequestLogging:Enabled` | bool | `false` — bật log request/response toàn cục (RequestResponseLoggingMiddleware) |
| `RequestLogging:PersistToFile` | bool | `true` — có ghi thêm log Request/Response vào File sink (`pos-*.log`) hay chỉ Elasticsearch; đọc riêng trong `SerilogConfiguration.cs`, không qua `RequestLoggingOptions` |
| `RequestLogging:MaxBodyBytes` | int | `8192` — cắt bớt body log quá dài |
| `RequestLogging:ExcludePaths` | string[] | `["/health", "/swagger"]` |
| `Redis:Mode` | string | `"StandAlone"` |
| `Redis:SentinelHosts` | string[] | `["10.x.x.x:6379"]` |
| `Redis:MasterName` | string | `"mymaster"` |
| `Redis:Password` | string | _(ẩn)_ |
| `Redis:ConnectTimeout` | int | 5500 |
| `Redis:SyncTimeout` | int | 5500 |
| `Redis:DefaultDatabase` | int | 2 |
| `Redis:KeepAlive` | int | 180 |
| `Redis:ConnectRetry` | int | 2 |
| `RabbitMQ:Host` | string | `"10.x.x.x"` |
| `RabbitMQ:Port` | int | 5672 |
| `RabbitMQ:Username` | string | _(ẩn)_ |
| `RabbitMQ:Password` | string | _(ẩn)_ |
| `RabbitMQ:VirtualHost` | string | `"/"` |
| `RabbitMQ:RequestedHeartbeat` | int | 60 |
| `ConnectionStrings:CentralMD` | string | SQL Server — RPOSMasterData _(creds ẩn)_ |
| `ConnectionStrings:Loyalty` | string | SQL Server — Loyalty DB _(creds ẩn)_ |
| `ConnectionStrings:StagingDB` | string | SQL Server — StagingDB _(creds ẩn)_ |
| `ConnectionStrings:Partner` | string | SQL Server — Partner_QAS _(creds ẩn)_ |
| `ConnectionStrings:EInvoice` | string | SQL Server — EInvoice _(creds ẩn)_ |
| `ConnectionStrings:IFSAP` | string | SQL Server — RPOSMasterData _(creds ẩn)_ |
| `ConnectionStrings:CentralGeneral` | string | SQL Server — RPOSCentralGeneral _(creds ẩn)_ |
| `ConnectionStrings:CentralSale` | string | SQL Server — RPOSCentralSales _(creds ẩn)_ |
| `ConnectionStrings:CentralSaleTemplate` | string | Template `{server}` — routing per-store _(creds ẩn)_ |
| `ConnectionStrings:BootstrapServers` | string | Kafka brokers `"10.x.x.x:9092,10.x.x.x:9092"` |
| `SetDb:DB1` | string | `"10.x.x.x\\DRW"` (fallback server) |
| `SetDb:DB2..DB6` | string | `""` (chưa cấu hình) |
| `AppSettings:Environment` | string | `"DEV"` |
| `AppSettings:UploadFileFTP` | string | `"YES"` |
| `AppSettings:FolderShare` | string | `"\\\\Dev-fitweb01\\pos"` |
| `AppSettings:FolderShareUpdSource` | string | `"\\\\DEV-FITWEB01\\BluePosUpgrade"` |
| `AppSettings:FolderShareAPIBluePOS` | string | `"Dev-bposweb01\\ftpbluepos"` |
| `AppSettings:FtpRootPath` | string | `"D:\\ROOT\\FTPBLUEPOS"` |
| `AppSettings:RemoteSvrUser` | string | _(ẩn)_ |
| `AppSettings:RemoteSvrPass` | string | _(ẩn)_ |

---

## MỤC G — Helpers dùng chung (`POS.Common/Helpers`): chữ ký method

> Tất cả là **static** trừ `FileLogHelper` (có `IFileLogHelper`, inject qua DI). Namespace `POS.Common.Helpers`.
> **Đọc mục này TRƯỚC khi viết helper mới** — nhiều tiện ích chuỗi/ngày/SĐT đã có sẵn.

### `DateTimeHelper` (static)
```csharp
bool TryParseToDateTime(string input, string inputFormat, out DateTime dateTime)
string? ConvertToIsoFormat(string inputDateTimeString, string inputFormat, string outputFormat)
DateTime StringToDateTime(string dateTimeString, string format)      // parse fail → 1900-01-01
DateTime ConvertStrToDate(string input, string format)               // ParseExact (ném lỗi nếu sai)
TimeSpan GetExpiryTimeLastMonth()
long GetTotalSecondMidnight(int numberDate = 1)
long GetSecondsUntilMidnight()
DateTimeOffset GetMidnightOffset()
TimeSpan GetTimeUntilHourInTomorrow(int hour)
TimeSpan GetTimeUntilMidnight()
string GetNullableString(this DateTime? time)                        // extension; null → "NULL"
string UnixTimestampString(int seconds = 0)
int GetMinuteElapsed(DateTime fromDateTime, DateTime toDateTime)
```

### `StringHelper` (static)
```csharp
JObject? StringToJObject(string json)                                // Newtonsoft JObject.Parse, lỗi → null
string Uppering(string str)
string RandomRequestID(string posNo, bool isRandom = false)          // posNo + unixTs [+ 2 số random]
string InitRandomString(string prefix)
string CreateKeyRedis(string appCode, string key)                    // "{appCode}:{key}"
string ReplaceString(string str, string oldValue, string newValue)
string FormatNumberString(int value)                                 // "N0"
string FormatDate_yyyyMMdd(string date)
bool ValidatePhoneNumber(string phoneNumber)                         // len 9–11, bắt đầu '0', toàn số
string Left(string input, int count)
string Right(string input, int count)
string FormartDate(string strDate, char fromChar, char toChar, string formatDate)
string IsNull(string str, string strDefault = "")
string[] ConverStringToArray(string str, char c)
string ConvertArrayToString(string[] arr, char c)
string ObjectToStringLowercase(object obj)                           // JSON camelCase (Newtonsoft)
T? StringToObject<T>(string json)                                    // JsonConvert.DeserializeObject
```

### `FormatHelper` (static)
```csharp
DateTime FormatDateTime(DateTime? dateTime)                          // null → DateTime.MinValue
string PhoneNumberWithCountryCode(string phoneNumber)                // → "84" + Right(9)
string PhoneNumberVietNam(string phoneNumber)                        // 84/+84 → "0…"
string CustomerCapillary(string member, string storeNo)             // "{member}-{storeNo}"
string GetTitleGender(string gender)                                 // Male→"CHỊ", Female→"ANH", else "OTHER"
string GetCharGender(string gender)                                  // Male→"M", Female→"F", else "O"
```

### `HostHelper` (static) — thông tin host/máy (Windows)
```csharp
int GetTotalCpuCores()
long GetTotalRamMB()                                                 // P/Invoke GlobalMemoryStatusEx
decimal GetMemoryUsageMB()
decimal GetCpuUsage()
string GetDateFileDll(string dllPath)
string GetIpAddress()                                                // IPv4 đầu tiên; fallback hostname
string GetServerName()                                               // Dns.GetHostName(); fallback "BLUEPOS"
```

### `ResponseHelper` (static)
```csharp
ResultResponse Response(HttpStatusCode status, string message, object? data, string technical = "")
```

### `FileLogHelper : IFileLogHelper` (POS.Common.Helpers)
```csharp
ctor FileLogHelper(string logDirectory)                              // ghi log_{yyyy-MM-dd}.txt, nuốt lỗi
void WriteLogs(string context, string message)
void WriteExpLogs(string context, Exception ex)
```
> ⚠️ **Trùng tên, KHÁC signature:** có 2 `IFileLogHelper`. Bản DI dùng toàn app là
> `POS.Infrastructure.Logging.IFileLogHelper` (`WriteLogs(string message)` / `WriteExpLogs(string function, Exception ex)` — xem MỤC E). Khi inject `IFileLogHelper` hãy xác nhận đang dùng namespace nào.

---

## Tóm tắt

- **Tổng số Interface đã có:** 22
  - Application: 8 (`ICommonService`, `IAkaChainLoyaltyService`, `IGotITService`, `IUrboxService`, `IDataRawService`, `ISyncDataPosService`, `IHealthCheckService`, `IKafkaService`)
  - Infrastructure Repositories: 6
  - Infrastructure AppServices: 4
  - Infrastructure Cache/Redis/Messaging/Logging/Files/DB: 9 (`IRedisManager`, `IRedisService`, `IRabbitMQProducer`, `IKafkaProducer`, `IFileLogHelper`, `IKibanaService`, `IFtpFileTransfer`, `IDbConnectionFactory` + 1 thêm)

- **Tổng số Repository đã có:** 6
  (`CentralMDRepository`, `CentralSaleRepository`, `DataRawJsonRepository`, `LoyaltyRepository`, `OfferStaffRepository`, `WincodeRepository`)

- **Tổng số Service đã có:** 12
  - Application Services: 8
  - Infrastructure AppServices: 4

- **Tổng số Controller hiện có:** 5
  (`BaseController`, `CommonController`, `LoyaltyController` [chỉ AkaChain], `PaymentController` [GotIT+Urbox], `SyncDataPosController`, `KafkaController`)

- **Tổng số DTO file đã có:** 79 file trong `POS.Common/Dtos/` (113 file .cs tổng cộng trong POS.Common)

- **Tổng số Enum đã có:** 25

# Kiến trúc Chi tiết — POS Backend .NET Core 10

## Clean Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    POS.API                          │
│     Controllers │ Middleware │ Filters │ Extensions  │
└────────────────────────┬────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────┐
│                POS.Application                      │
│   Services │ DTOs │ Validators │ Interfaces          │
│   (Business Logic — KHÔNG biết Infrastructure)      │
└──────────┬─────────────────────────┬────────────────┘
           │ depends on              │ implements via DI
┌─────────────────────┐   ┌─────────────────────────────┐
│    POS.Domain       │   │    POS.Infrastructure       │
│  Entities │ Enums   │   │  Repositories (Dapper)      │
│  Exceptions         │   │  IDbConnectionFactory        │
│  (Core — 0 deps)    │   │  External Services           │
└─────────────────────┘   └─────────────────────────────┘
           │                         │
           └────────────┬────────────┘
                        │ tất cả dùng
               ┌────────▼────────┐
               │   POS.Shared    │
               │ Extensions      │
               │ Constants       │
               │ Helpers         │
               └─────────────────┘
```

---

## Chi tiết từng Layer

### POS.API (Presentation Layer)
**Trách nhiệm:** Nhận HTTP request, validate cơ bản, gọi Application layer, trả response.

```
POS.API/
├── Controllers/
│   ├── BaseController.cs           ← Base class cho tất cả controllers
│   ├── SessionController.cs
│   ├── PaymentController.cs
│   ├── LoyaltyController.cs
│   ├── MasterDataController.cs
│   ├── OfferController.cs
│   └── CommonController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs   ← Global error handler
│   ├── RequestLoggingMiddleware.cs      ← Log mọi request/response
│   └── PosAuthMiddleware.cs            ← Xác thực POS device
├── Filters/
│   └── ValidateModelFilter.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs  ← Đăng ký DI
│   └── ApplicationBuilderExtensions.cs
└── Program.cs
```

### POS.Application (Business Logic Layer)
**Trách nhiệm:** Chứa toàn bộ business logic. Không biết DB, không biết HTTP.

```
POS.Application/
├── Common/
│   ├── Interfaces/IDateTimeService.cs
│   ├── Services/DateTimeService.cs
│   └── DTOs/DocumentNoResponse.cs
├── Session/
│   ├── Interfaces/ISessionService.cs
│   ├── Services/SessionService.cs
│   ├── DTOs/CloseShiftRequest.cs / CloseShiftResponse.cs
│   └── Validators/CloseShiftRequestValidator.cs
├── Payment/
│   ├── Interfaces/IVoucherService.cs / IGotItService.cs / IUrboxService.cs
│   ├── Services/VoucherService.cs / GotItService.cs / UrboxService.cs
│   ├── DTOs/...
│   └── Validators/...
├── Loyalty/
│   ├── Interfaces/IMemberService.cs
│   ├── Services/MemberService.cs
│   ├── DTOs/...
│   └── Validators/...
├── MasterData/
│   ├── Interfaces/IMasterDataService.cs
│   ├── Services/MasterDataService.cs
│   └── DTOs/...
└── Offer/
    ├── Interfaces/IOfferService.cs
    ├── Services/OfferService.cs
    └── DTOs/...
```

### POS.Domain (Domain Layer)
**Trách nhiệm:** Entities thuần túy, Enums, Domain Exceptions. KHÔNG có dependency nào.

```
POS.Domain/
├── Entities/
│   ├── PosSession.cs
│   ├── Voucher.cs
│   ├── Member.cs
│   └── ...
├── Enums/
│   ├── SessionStatus.cs
│   ├── VoucherType.cs
│   └── ...
└── Common/
    ├── Exceptions/
    │   ├── PosBusinessException.cs
    │   ├── PosNotFoundException.cs
    │   └── PosValidationException.cs
    └── BaseEntity.cs
```

### POS.Infrastructure (Infrastructure Layer)
**Trách nhiệm:** Triển khai cụ thể các interfaces từ Application. Dapper, HTTP clients, v.v.

```
POS.Infrastructure/
├── Persistence/
│   ├── IDbConnectionFactory.cs        ← Interface tạo IDbConnection
│   ├── SqlServerConnectionFactory.cs  ← Impl cho SQL Server (hiện tại)
│   └── PostgresConnectionFactory.cs   ← Impl cho PostgreSQL (khi cần đổi)
├── Repositories/
│   ├── BaseRepository.cs              ← Base class dùng Dapper
│   ├── SessionRepository.cs
│   ├── VoucherRepository.cs
│   ├── MemberRepository.cs
│   └── ...
└── ExternalServices/
    ├── GotItApiClient.cs              ← HTTP client cho GotIt
    ├── UrboxApiClient.cs              ← HTTP client cho Urbox
    └── ...
```

### POS.Shared (Cross-cutting)
```
POS.Shared/
├── Models/
│   └── ApiResponse.cs             ← Response wrapper chung
├── Extensions/
│   ├── StringExtensions.cs
│   └── DateTimeExtensions.cs
├── Constants/
│   ├── ErrorCodes.cs              ← Tất cả error codes
│   └── ApiRoutes.cs               ← Tất cả route constants
└── Helpers/
    └── HashHelper.cs
```

---

## Program.cs — Cấu trúc chuẩn

```csharp
var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Custom registrations (từ Extension methods)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// Middleware pipeline (THỨ TỰ QUAN TRỌNG)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PosAuthMiddleware>();
app.MapControllers();

app.Run();
```
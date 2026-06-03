# CLAUDE.md — POS Backend API (.NET Core 10)

> **ĐÂY LÀ FILE BẮT BUỘC ĐỌC TRƯỚC KHI LÀM BẤT KỲ VIỆC GÌ.**
> Mọi quyết định về kiến trúc, naming, pattern đều dựa trên file này.

---

## 1. Bối cảnh dự án

Dự án này là phiên bản mới của hệ thống **POS Backend API** cho chuỗi siêu thị bán lẻ tạp hóa.

| Hạng mục | Dự án CŨ | Dự án MỚI |
|---|---|---|
| Framework | .NET Framework 4.6 | **.NET Core 10** |
| Kiến trúc | Monolithic, không rõ pattern | **Clean Architecture** |
| Async | Không nhất quán | **Bắt buộc async/await toàn bộ** |
| DI | Manual / Unity | **Built-in DI (.NET Core)** |
| Logging | Log4Net / custom | **Serilog (structured logging)** |
| Validation | Manual | **FluentValidation** |
| Mapping | Manual | **AutoMapper** |
| API Route | Giữ nguyên 100% | **Không thay đổi bất kỳ route nào** |
| API Response | Giữ nguyên 100% | **Không thay đổi format response** |

Thư mục dự án cũ nằm **cùng cấp** với thư mục này:
```
/root-workspace/
├── POS.Backend/          ← Dự án CŨ (.NET Framework 4.6) — CHỈ ĐỌC, KHÔNG SỬA
└── POS.Backend.New/      ← Dự án MỚI (file này đang ở đây)
```

---

## 2. Kiến trúc Solution

```
POS.Backend.New/
├── CLAUDE.md                          ← File này
├── .claude/
│   └── commands/                      ← Các slash commands tùy chỉnh
│       ├── analyze-legacy.md          ← Phân tích code cũ
│       ├── convert-module.md          ← Convert 1 module
│       └── review-code.md             ← Review & refactor
├── docs/
│   ├── architecture.md                ← Chi tiết kiến trúc
│   ├── api-mapping.md                 ← Bảng mapping endpoint cũ → mới
│   ├── conventions.md                 ← Coding conventions bắt buộc
│   └── modules/                       ← Tài liệu từng module (tạo dần)
├── src/
│   ├── POS.API/                       ← Presentation Layer
│   ├── POS.Application/               ← Business Logic Layer
│   ├── POS.Domain/                    ← Domain Layer
│   ├── POS.Infrastructure/            ← Infrastructure Layer
│   └── POS.Shared/                    ← Shared Utilities
└── tests/
    ├── POS.UnitTests/
    └── POS.IntegrationTests/
```

### Sơ đồ phụ thuộc (Dependency Rule)

```
POS.API → POS.Application → POS.Domain
              ↓
       POS.Infrastructure
              ↓
         POS.Shared (tất cả đều có thể dùng)
```

> ⚠️ **TUYỆT ĐỐI KHÔNG** để `POS.Domain` phụ thuộc vào bất kỳ layer nào khác.

---

## 3. Các Module Cần Convert

Dưới đây là danh sách module ưu tiên. Khi nhận yêu cầu convert, hãy đọc thêm
`docs/modules/{module-name}.md` nếu file đó tồn tại.

| # | Module | Thư mục Application | Mức độ ưu tiên |
|---|---|---|---|
| 1 | Session (CA / End-of-Day) | `POS.Application/Session/` | 🔴 Cao nhất |
| 2 | Common (DateTime, DocumentNo) | `POS.Application/Common/` | 🔴 Cao |
| 3 | Payment — Voucher nội bộ | `POS.Application/Payment/` | 🔴 Cao |
| 4 | Payment — GotIt | `POS.Application/Payment/` | 🟡 Trung bình |
| 5 | Payment — Urbox | `POS.Application/Payment/` | 🟡 Trung bình |
| 6 | Loyalty / Hội viên | `POS.Application/Loyalty/` | 🟡 Trung bình |
| 7 | Master Data Download | `POS.Application/MasterData/` | 🟡 Trung bình |
| 8 | Offer | `POS.Application/Offer/` | 🟢 Thấp |

---

## 3.1 Quy tắc BẮT BUỘC về Route và Response

> 🔴 **ĐÂY LÀ 2 RÀNG BUỘC QUAN TRỌNG NHẤT CỦA DỰ ÁN**

### Route — Giữ nguyên 100%
- **KHÔNG thêm prefix** `/v1/` hay bất kỳ prefix nào
- **KHÔNG đổi tên** segment nào trong URL
- **KHÔNG thay đổi HTTP verb** (GET/POST/PUT/DELETE)
- Route trong controller mới phải **copy chính xác** từ route của controller cũ

```csharp
// Ví dụ: nếu API cũ là POST /api/pos/closeshift
// thì controller mới phải là:
[Route("api/pos")]
public class SessionController : ControllerBase
{
    [HttpPost("closeshift")]  // ← ĐÚNG, giữ nguyên
    public async Task<IActionResult> CloseShift(...) { }

    // ❌ SAI — tự ý đổi route
    [HttpPost("close-shift")]   // ← KHÔNG được thêm dấu gạch ngang
    [HttpPost("session/close")] // ← KHÔNG được đổi cấu trúc
}
```

### Response — Giữ nguyên cấu trúc JSON 100%
- **KHÔNG bọc thêm wrapper** (`ApiResponse<T>`, `data: {...}`, v.v.)
- **KHÔNG đổi tên field** trong JSON output
- **KHÔNG thêm field mới** vào response
- **KHÔNG bỏ field** dù field đó không còn dùng đến
- Đọc kỹ code cũ để biết chính xác JSON structure trước khi viết DTO



### 4.1 Async/Await
```csharp
// ✅ ĐÚNG
public async Task<ApiResponse<T>> GetDataAsync(...)
{
    var result = await _service.DoSomethingAsync();
    return ApiResponse<T>.Ok(result);
}

// ❌ SAI — KHÔNG dùng .Result, .Wait(), hay blocking call
var result = _service.DoSomethingAsync().Result;
```

### 4.2 Response Format — GIỮ NGUYÊN HOÀN TOÀN NHƯ API CŨ

> ⚠️ **RÀNG BUỘC CỨNG KHÔNG ĐƯỢC VI PHẠM:**
> Client POS phần cứng **không thay đổi**, nên mọi response của API mới
> phải có **cấu trúc JSON giống hệt** API cũ — cùng tên field, cùng kiểu dữ liệu,
> cùng HTTP status code, cùng cấu trúc lỗi.

**Quy trình bắt buộc khi implement một endpoint:**
1. Đọc code cũ để xác định chính xác cấu trúc response (tên field, kiểu dữ liệu)
2. Tạo Response DTO khớp **100%** với JSON mà API cũ trả về
3. Không thêm, không bớt, không đổi tên bất kỳ field nào trong response

```csharp
// ✅ ĐÚNG — Response DTO phản ánh ĐÚNG cấu trúc JSON của API cũ
// (tên class chỉ là tên nội bộ, JSON serialization mới quan trọng)
public class ValidateVoucherResponse
{
    // Tên properties phải khớp CHÍNH XÁC với JSON field của API cũ
    // Ví dụ: nếu API cũ trả {"voucher_code": "...", "amount": 0}
    //        thì DTO phải có VoucherCode với [JsonPropertyName("voucher_code")]
    //        hoặc đặt tên thẳng là VoucherCode nếu API cũ dùng PascalCase
}

// ❌ SAI — Tự ý đổi cấu trúc response
public class ValidateVoucherResponse
{
    public bool Success { get; set; }    // Thêm field không có trong API cũ
    public string Message { get; set; } // Thêm field không có trong API cũ
    public object Data { get; set; }    // Bọc thêm wrapper không có trong API cũ
}
```

**Về HTTP Status Code:** Giữ nguyên logic trả status code như API cũ.
Không tự ý chuẩn hóa về 200/400/404/500 nếu API cũ có cách xử lý khác.

### 4.3 Dependency Injection
```csharp
// ✅ Constructor injection — LUÔN dùng cách này
public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;
    private readonly ILogger<VoucherService> _logger;

    public VoucherService(IVoucherRepository repo, ILogger<VoucherService> logger)
    {
        _repo = repo;
        _logger = logger;
    }
}

// ❌ KHÔNG dùng static, singleton tự tạo, hay ServiceLocator
```

### 4.4 Logging với Serilog
```csharp
// Log có structured data — LUÔN dùng named parameters
_logger.LogInformation("Validating voucher {VoucherCode} for store {StoreId}", voucherCode, storeId);
_logger.LogError(ex, "Voucher validation failed for {VoucherCode}", voucherCode);

// KHÔNG dùng string interpolation trong log
_logger.LogInformation($"Validating voucher {voucherCode}"); // ❌ SAI
```

### 4.5 Error Handling
```csharp
// Dùng custom exceptions định nghĩa trong POS.Domain/Common/Exceptions/
throw new PosBusinessException("POS_PAY_001", "Voucher không hợp lệ");
throw new PosNotFoundException("POS_LOY_001", $"Không tìm thấy hội viên {memberId}");
```

### 4.6 Không dùng các API lỗi thời
- ❌ `HttpContext.Current` → dùng `IHttpContextAccessor`
- ❌ `ConfigurationManager` → dùng `IConfiguration` / Options Pattern
- ❌ `Thread.Sleep` → dùng `await Task.Delay`
- ❌ `WebClient` → dùng `IHttpClientFactory`

---

## 5. Naming Conventions

| Loại | Convention | Ví dụ |
|---|---|---|
| Controller | `{Module}Controller` | `PaymentController` |
| Service Interface | `I{Name}Service` | `IVoucherService` |
| Service Implementation | `{Name}Service` | `VoucherService` |
| Repository Interface | `I{Entity}Repository` | `IVoucherRepository` |
| Request DTO | `{Action}{Entity}Request` | `ValidateVoucherRequest` |
| Response DTO | `{Action}{Entity}Response` | `ValidateVoucherResponse` |
| Validator | `{Request}Validator` | `ValidateVoucherRequestValidator` |
| Error Code | `POS_{MODULE}_{NUMBER}` | `POS_PAY_001` |

---

## 6. Database

- Giữ nguyên database schema từ dự án cũ — **KHÔNG thay đổi tên bảng, tên cột**
- Dùng **Dapper** làm ORM — KHÔNG dùng Entity Framework Core
- Kết nối DB thông qua `IDbConnection` (interface), không phụ thuộc vào driver cụ thể
- Driver mặc định ban đầu: `Microsoft.Data.SqlClient` (MS SQL), nhưng code phải viết để **dễ đổi sang PostgreSQL** (`Npgsql`) chỉ bằng cách thay driver và connection string — không sửa query logic

### Quy tắc viết Dapper để tương thích đa DB

```csharp
// ✅ ĐÚNG — dùng named parameters (@param), tương thích cả SQL Server lẫn PostgreSQL
var result = await conn.QueryAsync<Voucher>(
    "SELECT * FROM Vouchers WHERE VoucherCode = @VoucherCode AND StoreId = @StoreId",
    new { VoucherCode = code, StoreId = storeId });

// ❌ SAI — dùng positional parameters hoặc string concat
"SELECT * FROM Vouchers WHERE VoucherCode = '" + code + "'"  // SQL injection + không portable
```

### Cấu trúc Repository với Dapper

```csharp
// POS.Infrastructure/Repositories/BaseRepository.cs
public abstract class BaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Mỗi repository tự mở/đóng connection — không share connection
    protected IDbConnection CreateConnection() => _connectionFactory.CreateConnection();
}

// POS.Infrastructure/Persistence/IDbConnectionFactory.cs
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

// Implement cho SQL Server (hiện tại)
public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    public SqlServerConnectionFactory(IConfiguration config)
        => _connectionString = config.GetConnectionString("PosDb");

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}

// Implement cho PostgreSQL (khi cần đổi — chỉ swap DI registration)
// public class PostgresConnectionFactory : IDbConnectionFactory
// {
//     public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
// }
```

### Đăng ký DI

```csharp
// Hiện tại dùng SQL Server
services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();

// Khi đổi sang PostgreSQL: chỉ thay 1 dòng này, không đụng vào Repository
// services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
```

### Connection string trong appsettings.json

```json
{
  "ConnectionStrings": {
    "PosDb": "Server=...;Database=...;User Id=...;Password=...;"
  }
}
```

- **KHÔNG hardcode** connection string trong code
- **KHÔNG inject `SqlConnection` trực tiếp** — luôn qua `IDbConnectionFactory`

---

## 7. Cấu hình môi trường

```
appsettings.json              ← Config chung
appsettings.Development.json  ← Config dev (local)
appsettings.Staging.json      ← Config staging
appsettings.Production.json   ← Config production (KHÔNG commit secret)
```

---

## 8. Checklist trước khi hoàn thành một task

Trước khi báo "xong" một module, Claude phải tự kiểm tra:

- [ ] Tất cả methods đều là `async Task<>`?
- [ ] Route của endpoint khớp **chính xác** với route API cũ (đã đối chiếu từng ký tự)?
- [ ] Cấu trúc JSON response giống **hệt** API cũ (đã đối chiếu từng field)?
- [ ] Có `FluentValidation` cho mọi Request DTO?
- [ ] Có logging tại: nhận request, kết quả, lỗi?
- [ ] Error handling dùng custom exception?
- [ ] Không có hardcode string (connection string, URL, key)?
- [ ] Đã đăng ký DI trong `Program.cs` hoặc `DependencyInjection.cs`?
- [ ] Đã cập nhật `docs/api-mapping.md` đánh dấu ✅ endpoint vừa hoàn thành?
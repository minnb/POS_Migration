# Coding Conventions — POS Backend .NET Core 10

## 1. Naming Conventions

### Files & Classes
| Loại | Pattern | Ví dụ |
|---|---|---|
| Controller | `{Module}Controller` | `PaymentController.cs` |
| Service Interface | `I{Name}Service` | `IVoucherService.cs` |
| Service Impl | `{Name}Service` | `VoucherService.cs` |
| Repository Interface | `I{Entity}Repository` | `IVoucherRepository.cs` |
| Repository Impl | `{Entity}Repository` | `VoucherRepository.cs` |
| Request DTO | `{Action}{Entity}Request` | `ValidateVoucherRequest.cs` |
| Response DTO | `{Action}{Entity}Response` | `ValidateVoucherResponse.cs` |
| Validator | `{RequestName}Validator` | `ValidateVoucherRequestValidator.cs` |
| EF Config | `{Entity}Configuration` | `VoucherConfiguration.cs` |
| Exception | `Pos{Type}Exception` | `PosNotFoundException.cs` |
| Middleware | `{Name}Middleware` | `ExceptionHandlingMiddleware.cs` |

### Properties & Variables
```csharp
// ✅ Private fields: _camelCase với underscore prefix
private readonly IVoucherService _voucherService;
private readonly ILogger<PaymentController> _logger;

// ✅ Properties: PascalCase
public string VoucherCode { get; set; }
public DateTime ExpiryDate { get; set; }

// ✅ Local variables: camelCase
var voucherCode = request.Code;
int totalAmount = 0;

// ✅ Constants: UPPER_SNAKE_CASE
public const string DEFAULT_CURRENCY = "VND";

// ✅ Enums: PascalCase
public enum VoucherType { Internal, GotIt, Urbox }
```

---

## 2. Cấu trúc Controller chuẩn

```csharp
// Route phải copy CHÍNH XÁC từ controller cũ — không tự đặt
[ApiController]
[Route("api/pos")]          // ← Lấy từ RoutePrefix của controller cũ
[Produces("application/json")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService,
                              ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>Kết thúc ca (giữ nguyên route từ API cũ)</summary>
    [HttpPost("closeshift")]   // ← Copy chính xác từ Route/action cũ
    public async Task<IActionResult> CloseShift(
        [FromBody] CloseShiftRequest request)
    {
        _logger.LogInformation("CloseShift called for terminal {TerminalId}", request.TerminalId);

        // Service trả về đúng kiểu mà API cũ từng trả
        var result = await _sessionService.CloseShiftAsync(request);

        // Trả về result trực tiếp — KHÔNG bọc thêm wrapper
        return Ok(result);
    }
}
```

> ⚠️ **Lưu ý quan trọng:** Tên class Controller (ví dụ `SessionController`)
> chỉ là tên nội bộ để tổ chức code. Route thực tế hoàn toàn do
> `[Route(...)]` và `[HttpPost/Get/Put/Delete(...)]` quyết định —
> không liên quan đến tên class.

---

## 3. Cấu trúc Service chuẩn

```csharp
public interface IVoucherService
{
    // Kiểu trả về phải khớp với cấu trúc JSON của API cũ
    Task<ValidateVoucherResponse> ValidateAsync(ValidateVoucherRequest request);
    Task<UseVoucherResponse> UseAsync(UseVoucherRequest request);
}

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;
    private readonly ILogger<VoucherService> _logger;

    public VoucherService(IVoucherRepository repo,
                          ILogger<VoucherService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<ValidateVoucherResponse> ValidateAsync(ValidateVoucherRequest request)
    {
        _logger.LogInformation("Validating voucher {Code} at store {StoreId}",
            request.Code, request.StoreId);

        var voucher = await _repo.GetByCodeAsync(request.Code)
            ?? throw new PosNotFoundException(ErrorCodes.VoucherNotFound,
                $"Voucher {request.Code} không tồn tại");

        if (voucher.ExpiryDate < DateTime.UtcNow)
            throw new PosBusinessException(ErrorCodes.VoucherExpired,
                "Voucher đã hết hạn");

        _logger.LogInformation("Voucher {Code} validated successfully", request.Code);

        // Response DTO phải có field names ĐÚNG như JSON cũ
        return new ValidateVoucherResponse { /* fields từ API cũ */ };
    }
}
```

> **Về Response DTO:** Sau khi đọc code cũ, xác định chính xác
> JSON structure rồi mới tạo DTO. Dùng `[JsonPropertyName("field_name")]`
> nếu API cũ dùng snake_case hoặc camelCase khác với C# naming.

---

## 4. Cấu trúc FluentValidation chuẩn

```csharp
public class ValidateVoucherRequestValidator : AbstractValidator<ValidateVoucherRequest>
{
    public ValidateVoucherRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã voucher không được để trống")
            .MaximumLength(50).WithMessage("Mã voucher tối đa 50 ký tự");

        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Mã cửa hàng không được để trống")
            .GreaterThan(0).WithMessage("Mã cửa hàng không hợp lệ");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0");
    }
}
```

---

## 5. Error Codes

### Format: `POS_{MODULE}_{3-DIGIT-NUMBER}`

```
POS_AUTH_001   Thiếu thông tin xác thực
POS_AUTH_002   Token không hợp lệ hoặc hết hạn
POS_AUTH_003   Không có quyền truy cập

POS_SES_001    Ca làm việc không tồn tại
POS_SES_002    Ca đã đóng
POS_SES_003    Ngày đã kết thúc

POS_PAY_001    Voucher không hợp lệ
POS_PAY_002    Voucher đã được sử dụng
POS_PAY_003    Voucher hết hạn
POS_PAY_004    Voucher không đủ điều kiện áp dụng
POS_PAY_005    Lỗi kết nối GotIt
POS_PAY_006    Lỗi kết nối Urbox

POS_LOY_001    Hội viên không tồn tại
POS_LOY_002    Tài khoản hội viên bị khóa
POS_LOY_003    Điểm tích lũy không đủ

POS_MDT_001    File master data không tồn tại
POS_MDT_002    Lỗi tạo file download

POS_OFR_001    Offer không tồn tại
POS_OFR_002    Offer đã hết hiệu lực

POS_SYS_999    Lỗi hệ thống không xác định
```

---

## 6. Route Conventions

> ⚠️ **KHÔNG có "route convention" mới. Route là bất biến.**

Route của mọi endpoint trong project mới phải **copy chính xác** từ project cũ.

```csharp
// Bước 1: Đọc route trong controller CŨ
// Ví dụ tìm thấy:
[RoutePrefix("api/pos")]
public class PosController : ApiController
{
    [HttpPost, Route("closeshift")]
    public IHttpActionResult CloseShift(...) { }
}

// Bước 2: Viết lại trong controller MỚI — route phải ĐỒNG NHẤT
[Route("api/pos")]
public class SessionController : ControllerBase
{
    [HttpPost("closeshift")]  // ← Copy chính xác, không sửa
    public async Task<IActionResult> CloseShift(...) { }
}
```

**Các lỗi thường gặp cần tránh:**

| ❌ Sai | ✅ Đúng | Lý do |
|---|---|---|
| `/api/v1/pos/closeshift` | `/api/pos/closeshift` | Không thêm `/v1/` |
| `/api/pos/close-shift` | `/api/pos/closeshift` | Không thêm dấu gạch ngang |
| `/api/session/closeshift` | `/api/pos/closeshift` | Không đổi tên segment |
| `GET /api/pos/closeshift` | `POST /api/pos/closeshift` | Không đổi HTTP verb |

---

## 8. Cấu trúc Repository chuẩn (Dapper)

```csharp
public interface IVoucherRepository
{
    Task<Voucher> GetByCodeAsync(string voucherCode);
    Task<IEnumerable<Voucher>> GetActiveByStoreAsync(int storeId);
    Task<int> UpdateStatusAsync(string voucherCode, int status);
}

public class VoucherRepository : BaseRepository, IVoucherRepository
{
    private readonly ILogger<VoucherRepository> _logger;

    public VoucherRepository(IDbConnectionFactory connectionFactory,
                              ILogger<VoucherRepository> logger)
        : base(connectionFactory)
    {
        _logger = logger;
    }

    public async Task<Voucher> GetByCodeAsync(string voucherCode)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Voucher>(
            @"SELECT VoucherCode, Amount, ExpiryDate, Status
              FROM Vouchers
              WHERE VoucherCode = @VoucherCode",
            new { VoucherCode = voucherCode });
    }

    public async Task<int> UpdateStatusAsync(string voucherCode, int status)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(
            @"UPDATE Vouchers
              SET Status = @Status, UpdatedAt = GETDATE()
              WHERE VoucherCode = @VoucherCode",
            new { VoucherCode = voucherCode, Status = status });
    }
}
```

**Quy tắc Dapper bắt buộc:**
- Luôn dùng `using var conn = CreateConnection()` — tự đóng connection sau dùng
- Luôn dùng named parameters `@ParamName` — không nối chuỗi SQL
- Tên bảng và cột giữ nguyên như schema cũ — không tự ý đổi tên

> ⚠️ **Giữ nguyên format DateTime như API cũ trả về.**
> Không tự ý chuẩn hóa sang UTC hay ISO 8601 nếu API cũ không làm vậy.

- Đọc code cũ để xác định API cũ trả DateTime theo format nào
- Giữ nguyên format đó trong API mới
- Dùng `IDateTimeService` thay vì `DateTime.Now` trực tiếp để dễ unit test

```csharp
// ✅ ĐÚNG — dùng service để dễ mock khi test
var now = _dateTimeService.Now; // hoặc .UtcNow tùy API cũ dùng gì

// ❌ SAI — khó mock khi unit test
var now = DateTime.Now;
```

---

## 9. 🔴 Cấu hình API Đối tác — Lấy từ DB, KHÔNG từ appsettings

> **Rule tuyệt đối:** Mọi config để gọi API đối tác ngoài (URL, route, credentials, key...)
> phải lấy từ bảng `SysWebApi` + `SysWebApiRoute` trong CentralMD DB thông qua
> `ISysWebApiConfigService` — **KHÔNG** hardcode trong appsettings.json.

### Pattern ĐÚNG

```csharp
// ✅ ĐÚNG — inject ISysWebApiConfigService
public class UrboxHttpService : IUrboxService
{
    private readonly ISysWebApiConfigService _configService;

    public async Task<...> CheckSerialAsync(...)
    {
        var dto = await _configService.GetByAppCodeAsync("URBOX");
        if (dto == null) return (false, "Không tìm thấy cấu hình URBOX", null);

        var apiRoute = dto.GetRoute("CheckCodeUrbox");
        // Dùng dto.Host, dto.UserName, dto.Password, dto.PrivateKey...
    }
}
```

### Pattern SAI

```csharp
// ❌ SAI — dùng IConfiguration cho partner config
public class UrboxHttpService : IUrboxService
{
    private readonly IConfiguration _config;

    public async Task<...> CheckSerialAsync(...)
    {
        var host = _config["Urbox:Host"];       // ❌ SAI
        var appId = _config["Urbox:AppId"];     // ❌ SAI
    }
}
```

### AppCode cho các đối tác đã biết

| Đối tác | AppCode | Route Names |
|---|---|---|
| WinX | `WINX` | `PosPostTransactions` |
| Urbox | `URBOX` | `CheckCodeUrbox`, `PayCodeUrbox` |
| GotIT | `GOTIT` | `CheckMultiple`, `CheckMultipleV6`, `MarkUseMultiple`, `MarkUseMultipleV6` |
| OneU | `ONEU` | `Token` (Notes=audience), `Estimate`, `Redeem` |
| Capillary | `CAP` | *(cần xác nhận khi convert)* |
| VinID | `VINID` | *(cần xác nhận khi convert)* |

### Những gì VẪN lấy từ appsettings.json

| Key | Lý do |
|---|---|
| `ConnectionStrings:*` | Config DB — do ops quản lý theo môi trường |
| `BasicAuth:Username/Password` | Auth của API server này |
| `GotIT:Environment` | Config môi trường deploy (`PRD`/`DEV`) — không phải partner config |
| `Serilog:*` | Logging config |
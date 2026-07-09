# Backend API Rules — POS.Common / Controller / Guardrails

## Quy tắc BẮT BUỘC khi làm việc với src/POS.Common/

### 1. Serialization: CHỈ dùng Newtonsoft.Json
- Package: `Newtonsoft.Json 13.*` (đã có trong `src/POS.Common/POS.Common.csproj`)
- Dùng `[JsonProperty("tên_gốc")]` nếu tên C# property **khác** với tên JSON field
- **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json` dưới bất kỳ hình thức nào
- Dùng `[JsonProperty]` — KHÔNG dùng `[JsonPropertyName]` (của System.Text.Json)
- Field kiểu động: dùng `object?` — KHÔNG dùng `JsonElement`

### 2. Lý do kinh doanh — KHÔNG ĐƯỢC THAY ĐỔI TÊN FIELD JSON
> 5.000 máy POS đang parse JSON response theo đúng tên field hiện tại.
> Thay đổi bất kỳ tên field nào sẽ phá vỡ production ngay lập tức.

### 3. C# 12 / .NET 10
- File-scoped namespace: `namespace POS.Common.Dtos.{Domain};`
- Nullable reference types: thêm `?` cho reference types
- Non-null required strings: `= string.Empty`
- Giữ nguyên: computed properties, inheritance chain, `[Required]`, `[StringLength]`

---

## Cấu trúc src/POS.Common/

```
src/POS.Common/
├── ResultResponse.cs
├── Enums/               (25 files)
└── Dtos/
    ├── (root)           AuthDto, HttpResponseBlueDto, KafkaMessage, NotifyConfigDto,
    │                    RabbitMessageDto, RedisDto, SMSMessage, SysWebApiDto, SysWebApiUserDto
    ├── B2B/
    ├── Capillary/       (Base, Tier, Redemption, Transaction, Customer, Enosta, Point, Coupons, Vouchers)
    ├── CentralMD/
    ├── Coupon/
    ├── CXVoucher/
    ├── DRW/
    ├── Giftee/
    ├── GotIT/
    ├── LogService/
    ├── Loyalty/         (Base, Transaction, CX, MemberBusiness, ProgramPoints, WinCode, WinScore)
    ├── MSN/
    ├── Ops/
    ├── PartnerApi/
    ├── POS/             (POSRequest, Gift/, ValidateTransactionDto)
    ├── Reward/
    ├── ROP/
    ├── StagingDB/
    ├── Tax/
    ├── Telegram/
    ├── TopupVoucherVinID/
    ├── Vouchers/
    ├── WinCare/
    ├── WinCustomer/
    ├── WinMoney/
    ├── Winpay/
    └── WinX/
```

Thêm DTO mới: dùng lệnh `/add-dto-common` (xem `.claude/commands/add-dto-common.md`).

---

## Quy tắc Controller — BẮT BUỘC

### A. DI Registration — BẮT BUỘC sau mỗi interface mới

Mỗi khi tạo `I{Name}Service` mới trong `POS.Application/Features/{Domain}/`:
1. Tạo stub hoặc implementation trong `POS.Application/Features/{Domain}/` (hoặc `POS.Infrastructure/` nếu cần HTTP client / DB)
2. **Đăng ký ngay** trong `src/POS.Application/DependencyInjection.cs`:
   ```csharp
   services.AddScoped<I{Name}Service, {Name}Service>();
   ```
3. Nếu chưa implement thật, dùng stub trả `HttpStatusCode.NotImplemented` — KHÔNG throw exception.

> **Lý do**: Quên đăng ký DI → `InvalidOperationException` lúc runtime, không phải lúc build.

### B. ModelState Validation — `ValidateModelFilter` đã xử lý global

`Program.cs` đã cấu hình `SuppressModelStateInvalidFilter = true` để `ValidateModelFilter` kiểm soát hoàn toàn format response (trả `ResultResponse`, không phải ASP.NET problem-details).

**Hệ quả quan trọng**:
- `ValidateModelFilter` chạy **trước** action method → `if (!ModelState.IsValid) return ExceptionModels()` trong action là **dead code** (không bao giờ được gọi).
- Vẫn có thể giữ dòng đó cho an toàn, nhưng không cần thiết.
- **TUYỆT ĐỐI KHÔNG** thêm `services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = false)` — sẽ phá vỡ contract.

### C. NullValueHandling.Ignore — Data: null bị omit

`Program.cs` cấu hình `NullValueHandling = NullValueHandling.Ignore`.
- Khi `ResultResponse.Data = null` → field `"Data"` bị bỏ qua trong JSON output.
- POS machines không nhận `"Data": null` mà nhận response không có field `Data`.
- Đây là behavior intentional (giảm bandwidth). **Không thay đổi**.

### D. Return type khi service trả ResultResponse

Nếu service trả `ResultResponse` (không phải plain data), KHÔNG dùng `OkResult(result)` — sẽ double-nest.

Dùng:
```csharp
// Khi HTTP status = service status (dynamic)
return StatusCode((int)result.Status, result);

// Khi HTTP status luôn 200
return Ok(result);

// Khi cần tùy chỉnh field (vd đặt giá trị riêng vào MessageTechnical)
return StatusCode((int)status, new ResultResponse { Data = ..., Message = ..., Status = ..., MessageTechnical = ... });
```

`OkResult(data)` chỉ dùng khi `data` là object thuần (không phải `ResultResponse`).

### E. Helpers chưa có trong POS.Common — inline tạm

Một số helper tiện ích chưa tồn tại trong `src/POS.Common/Helpers/`. Khi cần, inline trực
tiếp và đánh dấu `// TODO: extract to helper`:

| Nhu cầu | Logic inline | Ghi chú |
|---|---|---|
| Kiểm tra số điện thoại | `phone.Length >= 9 && phone.Length <= 11 && phone.All(char.IsDigit)` | TODO: extract to helper |
| Message số thẻ không hợp lệ | `$"Số thẻ {phone} không hợp lệ"` | |
| Format SĐT Việt Nam | (chưa có) | Tạo helper nếu dùng nhiều |
| Ghi exception log | `_fileLogHelper.WriteExpLogs(...)` | Đã có `IFileLogHelper` — dùng luôn |

### F. Swagger — chỉ bật ở Development

`Program.cs` đã cấu hình `AddSwaggerGen()` / `UseSwagger()` **chỉ khi `IsDevelopment()`**;
UAT/PROD không bật (tránh lộ API docs). Ở DEV truy cập UI tại `/swagger` (có nút Authorize
cho Basic Auth). Ngoài DEV, test route bằng curl trực tiếp.

---

## Guardrails & Testing — BẮT BUỘC biết

> Dự án có 3 "vành đai bảo vệ" trong `tests/POS.ContractTests/`. **Chạy `dotnet test` trước
> khi commit.** Lệnh: `dotnet test tests/POS.ContractTests`.

### 1. Contract test — khoá tên field JSON (cực quan trọng)

- File: `tests/POS.ContractTests/JsonFieldContractTests.cs` (+ helper `JsonContract.cs`).
- Mục đích: khoá **tên field JSON** của các DTO response mà **5.000 máy POS** đang parse.
  Đổi tên / thêm / xoá field bất kỳ của DTO đã khoá → **test đỏ ngay**.
- **Khi CỐ Ý đổi contract**: cập nhật danh sách field kỳ vọng trong file test **cùng commit** —
  đó là dấu vết cho thấy thay đổi là có chủ đích, không phải tai nạn.
- **Khi tạo DTO response mới**: thêm một `[Fact]` khoá field cho nó (dùng `AssertFields`).

### 2. DI validation test — chặn "quên đăng ký DI"

- File: `tests/POS.ContractTests/DependencyInjectionTests.cs`.
- Dựng lại đúng cách compose DI của `Program.cs`, kiểm tra **mọi phụ thuộc `POS.*`** trong
  constructor của tất cả controller + mọi implementation đã đăng ký đều có trong container.
- Chỉ đọc service descriptor (không build provider) → **không cần Redis/SQL/Rabbit**.
- Quên `services.AddScoped<...>()` → test đỏ lúc build/test thay vì `InvalidOperationException`
  lúc gọi API (xem mục "A. DI Registration").

### 3. Exception middleware — lưới an toàn global (G3)

- Impl: `src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs`; đăng ký **đầu pipeline**
  trong `Program.cs` (`app.UsePosExceptionHandling()`).
- Bắt mọi exception **chưa xử lý**, trả đúng `ResultResponse` (status 500, PascalCase qua
  `DefaultContractResolver`, `NullValueHandling.Ignore` → bỏ field `Data`) — khớp contract POS.
- Controller chỉ giữ try/catch khi cần **message nghiệp vụ riêng**; KHÔNG cần try/catch chỉ để
  format lỗi chung. **KHÔNG gỡ** middleware này.
- Hành vi được khoá bằng `tests/POS.ContractTests/ExceptionMiddlewareTests.cs`.

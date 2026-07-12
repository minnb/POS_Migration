# Rule: POS.Common — Serialization & DTO Structure

## 🎯 Context (Khi nào áp dụng)
Khi tạo/sửa bất kỳ DTO nào trong `src/POS.Common/`.

## ✅ DO (Bắt buộc làm)
- Dùng **Newtonsoft.Json** cho mọi serialization (`Newtonsoft.Json 13.*`, đã có trong
  `src/POS.Common/POS.Common.csproj`).
- Dùng `[JsonProperty("tên_gốc")]` nếu tên C# property **khác** tên JSON field.
- Dùng `[JsonProperty]` cho mọi thuộc tính cần kiểm soát tên JSON.
- Field kiểu động: dùng `object?`.
- File-scoped namespace: `namespace POS.Common.Dtos.{Domain};`.
- Nullable reference types: thêm `?` cho reference types.
- Non-null required strings: khởi tạo `= string.Empty`.
- Giữ nguyên các thành phần đã có: computed properties, inheritance chain, `[Required]`,
  `[StringLength]`.
- Đặt DTO đúng domain trong cây thư mục:
  ```
  src/POS.Common/
  ├── ResultResponse.cs
  ├── Enums/               (25 files)
  └── Dtos/
      ├── (root)           AuthDto, HttpResponseBlueDto, KafkaMessage, NotifyConfigDto,
      │                    RabbitMessageDto, RedisDto, SMSMessage, SysWebApiDto, SysWebApiUserDto
      ├── B2B/  ├── Capillary/ (Base, Tier, Redemption, Transaction, Customer, Enosta, Point,
      │            Coupons, Vouchers)
      ├── CentralMD/  ├── Coupon/  ├── CXVoucher/  ├── DRW/  ├── Giftee/  ├── GotIT/
      ├── LogService/
      ├── Loyalty/         (Base, Transaction, CX, MemberBusiness, ProgramPoints, WinCode, WinScore)
      ├── MSN/  ├── Ops/  ├── PartnerApi/
      ├── POS/             (POSRequest, Gift/, ValidateTransactionDto)
      ├── Reward/  ├── ROP/  ├── StagingDB/  ├── Tax/  ├── Telegram/  ├── TopupVoucherVinID/
      ├── Vouchers/  ├── WinCare/  ├── WinCustomer/  ├── WinMoney/  ├── Winpay/  └── WinX/
  ```
- Thêm DTO mới: dùng lệnh `/add-dto-common` (xem `.claude/commands/add-dto-common.md`).

## ❌ DON'T (Tuyệt đối cấm)
- **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json` dưới bất kỳ hình thức nào.
- Cấm dùng `[JsonPropertyName]` (của `System.Text.Json`).
- Cấm dùng `JsonElement` cho field kiểu động.
- **Cấm đổi tên field JSON** của DTO response hiện hữu — 5.000 máy POS đang parse JSON theo đúng
  tên field hiện tại; đổi bất kỳ tên field nào sẽ phá vỡ production ngay lập tức.

---

# Rule: Controller — DI / ModelState / NullValueHandling / Return Type

## 🎯 Context (Khi nào áp dụng)
Khi tạo/sửa Controller trong `POS.Api/Controllers/` hoặc tạo `I{Name}Service` mới.

## ✅ DO (Bắt buộc làm)
- **DI Registration** — mỗi khi tạo `I{Name}Service` mới trong `POS.Application/Features/{Domain}/`:
  1. Tạo stub hoặc implementation trong `POS.Application/Features/{Domain}/` (hoặc
     `POS.Infrastructure/` nếu cần HTTP client / DB).
  2. **Đăng ký ngay** trong `src/POS.Application/DependencyInjection.cs`:
     ```csharp
     services.AddScoped<I{Name}Service, {Name}Service>();
     ```
  3. Nếu chưa implement thật, dùng stub trả `HttpStatusCode.NotImplemented` — không throw
     exception.
  > Quên đăng ký DI → `InvalidOperationException` lúc runtime, không phải lúc build.
- Return type khi service trả `ResultResponse`:
  ```csharp
  // Khi HTTP status = service status (dynamic)
  return StatusCode((int)result.Status, result);

  // Khi HTTP status luôn 200
  return Ok(result);

  // Khi cần tùy chỉnh field (vd đặt giá trị riêng vào MessageTechnical)
  return StatusCode((int)status, new ResultResponse { Data = ..., Message = ..., Status = ..., MessageTechnical = ... });
  ```
  `OkResult(data)` chỉ dùng khi `data` là object thuần (không phải `ResultResponse`).
- Helper tiện ích chưa có trong `src/POS.Common/Helpers/` → inline trực tiếp, đánh dấu
  `// TODO: extract to helper`:

  | Nhu cầu | Logic inline | Ghi chú |
  |---|---|---|
  | Kiểm tra số điện thoại | `phone.Length >= 9 && phone.Length <= 11 && phone.All(char.IsDigit)` | TODO: extract to helper |
  | Message số thẻ không hợp lệ | `$"Số thẻ {phone} không hợp lệ"` | |
  | Format SĐT Việt Nam | (chưa có) | Tạo helper nếu dùng nhiều |
  | Ghi exception log | `_fileLogHelper.WriteExpLogs(...)` | Đã có `IFileLogHelper` — dùng luôn |
- Swagger chỉ bật khi `IsDevelopment()` (`Program.cs` đã cấu hình `AddSwaggerGen()`/
  `UseSwagger()`) — DEV truy cập UI tại `/swagger` (nút Authorize cho Basic Auth); ngoài DEV test
  route bằng curl trực tiếp.
- Hiểu đúng hệ quả `ValidateModelFilter`: `Program.cs` đã cấu hình
  `SuppressModelStateInvalidFilter = true` nên `ValidateModelFilter` chạy **trước** action method
  và kiểm soát hoàn toàn format response (trả `ResultResponse`, không phải ASP.NET
  problem-details) — `if (!ModelState.IsValid) return ExceptionModels()` trong action là dead code
  (giữ lại cũng không sao, nhưng không cần thiết).
- Hiểu đúng hệ quả `NullValueHandling.Ignore` (`Program.cs`): khi `ResultResponse.Data = null` →
  field `"Data"` bị bỏ qua trong JSON output (POS nhận response không có field `Data`, không phải
  `"Data": null`) — đây là behavior intentional để giảm bandwidth.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm dùng `OkResult(result)` khi `result` đã là `ResultResponse` — sẽ double-nest.
- **TUYỆT ĐỐI KHÔNG** thêm
  `services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = false)` — sẽ
  phá vỡ contract.
- Cấm thay đổi behavior `NullValueHandling.Ignore`.
- Cấm bật Swagger ở UAT/PROD (tránh lộ API docs).

---

# Rule: Guardrails & Testing (3 vành đai bảo vệ)

## 🎯 Context (Khi nào áp dụng)
Trước mỗi lần commit thay đổi liên quan DTO response, DI registration, hoặc Controller — dự án có
3 "vành đai bảo vệ" trong `tests/POS.ContractTests/`.

## ✅ DO (Bắt buộc làm)
- **Chạy `dotnet test tests/POS.ContractTests` trước khi commit.**
- **1. Contract test — khoá tên field JSON** (`tests/POS.ContractTests/JsonFieldContractTests.cs`
  + helper `JsonContract.cs`): khoá tên field JSON của các DTO response mà 5.000 máy POS đang
  parse.
  - Khi CỐ Ý đổi contract: cập nhật danh sách field kỳ vọng trong file test **cùng commit** — dấu
    vết cho thấy thay đổi có chủ đích.
  - Khi tạo DTO response mới: thêm một `[Fact]` khoá field cho nó (dùng `AssertFields`).
- **2. DI validation test — chặn "quên đăng ký DI"**
  (`tests/POS.ContractTests/DependencyInjectionTests.cs`): dựng lại đúng cách compose DI của
  `Program.cs`, kiểm tra mọi phụ thuộc `POS.*` trong constructor của tất cả controller + mọi
  implementation đã đăng ký đều có trong container. Chỉ đọc service descriptor (không build
  provider) → không cần Redis/SQL/Rabbit.
- **3. Exception middleware — lưới an toàn global**
  (`src/POS.Api/Middleware/ExceptionHandlingMiddleware.cs`, đăng ký đầu pipeline trong
  `Program.cs` qua `app.UsePosExceptionHandling()`): bắt mọi exception chưa xử lý, trả đúng
  `ResultResponse` (status 500, PascalCase qua `DefaultContractResolver`,
  `NullValueHandling.Ignore` → bỏ field `Data`) — khớp contract POS. Hành vi được khoá bằng
  `tests/POS.ContractTests/ExceptionMiddlewareTests.cs`.
- Controller chỉ giữ try/catch khi cần message nghiệp vụ riêng.

## ❌ DON'T (Tuyệt đối cấm)
- Đổi tên/thêm/xoá field JSON của DTO đã khoá mà không cập nhật test — test đỏ ngay, không được
  bỏ qua.
- Cấm quên `services.AddScoped<...>()` khi thêm implementation mới — test DI validation phải bắt
  được lỗi này ở lúc build/test, không để lộ ra `InvalidOperationException` lúc gọi API thật.
- **KHÔNG gỡ** `ExceptionHandlingMiddleware`.
- Cấm thêm try/catch trong controller chỉ để format lỗi chung (middleware đã lo).

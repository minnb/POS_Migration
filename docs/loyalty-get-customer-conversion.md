# Loyalty — Convert `GET api/v2/loyalty/customer/get`

> File này là **template đại diện** cho toàn bộ module Loyalty.
> Kiến trúc và pattern ở đây áp dụng cho tất cả endpoint Loyalty còn lại.

---

## 1. Phân tích endpoint (code cũ)

### Route & Parameters
- **Route**: `GET api/v2/loyalty/customer/get`
- **Auth**: Basic Auth (giữ nguyên `BasicAuthFilter`)
- **Query params**:
  | Param | Type | Required | Mô tả |
  |---|---|---|---|
  | `numberCard` | string | ✅ | SĐT hoặc mã thẻ hội viên (tối thiểu 9 ký tự) |
  | `posID` | string | ✅ | Mã máy POS |
  | `storeNo` | string | ✅ | Mã cửa hàng |
  | `clubCode` | string | ❌ | Mã club (default = "") |
  | `isMobile` | bool | ❌ | Request từ mobile app (default = false) |
  | `isLog` | bool | ❌ | Bật log (default = true) |

### Luồng xử lý (call stack)

```
LoyaltyController.GetCustomerDetail()
  │
  ├─ Validate: numberCard không rỗng, length >= 9
  │
  ├─ NumberHelper.IsMemberCapillary(numberCard, isMobile)
  │   → Xác định member type:
  │     PHONE   = SĐT 9-11 số, isDigits, isMobile=false
  │     ID      = <= 9 ký tự, không bắt đầu bằng "0", isMobile=true
  │     WINCARE = 12 ký tự, isMobile=true
  │     WINX    = 14 ký tự, isMobile=true
  │     NONE    = không hợp lệ
  │
  ├─ LoyaltyHelper.IsVINID(numberCard)
  │   → prefix "8888"/"66668" + length>=16, hoặc prefix "P" + length≠14
  │   → VINID card → nhánh VINID legacy (VINIDGetInfoMember) [scope này: stub 400]
  │
  ├─ if NONE → return 400 "Số thẻ không hợp lệ"
  │
  ├─ if Capillary (PHONE/ID/WINCARE/WINX):
  │   ├─ IsOfflineCapillary() → check Redis flag
  │   │   → if offline → GetMemberInfoOfflineSwitch() → trả cached data
  │   │
  │   └─ LoyaltyService.GetInfoMemberCapillary(...)
  │       │
  │       ├─ PHONE: FormatPhoneWithCountryCode → "+84xxx"
  │       ├─ WINCARE: WinCareService.GetInfoCustomerStaff() → resolve to Capillary ID [stub]
  │       ├─ WINX: WinXService.ResolveWinXUserIdCapillary() → resolve to Capillary ID [stub]
  │       │
  │       ├─ LoyaltyCapillaryService.GetCustomerLoyaltyDetail()
  │       │   └─ CapillaryService.GetCustomerDetail()
  │       │       → HTTP GET Capillary:
  │       │         PHONE: /api/customer?format=json&mobile=+84xxx&user_id=true&store_id={storeNo}
  │       │         ID:    /api/customer?format=json&id={id}&user_id=true&store_id={storeNo}
  │       │         Auth:  Basic Base64(userName.storeNo:MD5(password))
  │       │         Headers: X-CAP-API-ATTRIBUTION-LOOKUP-TYPE: MOBILE_TRIGGER
  │       │                  x-winx-loyalty-client: POS
  │       │         Timeout: từ SysWebApi.Version (giây)
  │       │         AppCode DB: "CAPILLARY"
  │       │
  │       ├─ Map CapillaryResponse → InfoMemberModel:
  │       │   - CardNumber/PhoneNumber = mobile (format VN "0xxx")
  │       │   - MemberName = firstname
  │       │   - VirtualCard = user_id ?? mobile
  │       │   - CardLevel = map từ current_slab
  │       │   - MemberPoint = redeemable loyalty_points
  │       │   - TotalPoint = lifetime points
  │       │   - RedemptionValue = MemberPoint × rate
  │       │   - Email/Gender/Dob/Address = từ extended_fields
  │       │   - AvailablePromotion = [] [stub]
  │       │   - MemberBusiness = null [stub]
  │       │   - OtherStatus = null [stub]
  │       │   - PointsSummaries = [] [stub]
  │       │   - Status = "Hoạt động", System = "CAP"
  │       │
  │       ├─ Redis: cache InfoMemberModel theo phone key
  │       │
  │       └─ Handle error:
  │           408/timeout → GetMemberInfoOfflineSwitch()
  │           5xx         → SwitchingProtocols(101) → offline
  │           else        → return error
  │
  └─ Map controller response:
      200 OK    → { Status:200, Message:"OK", Data: InfoMemberModel, MessageTechnical: clubCode }
      408       → offline data
      101       → { Status:200, Message:"OFF", Data: offline data }
      else      → { Status: X, Message: X, Data: X, MessageTechnical: X }
```

### Response JSON (phải khớp 100% với API cũ)

```json
{
  "Status": 200,
  "Message": "OK",
  "Data": {
    "CardNumber": "0901234567",
    "VirtualCard": "84901234567",
    "CMND": "",
    "MemberName": "Nguyen Van A",
    "Title": "Anh",
    "PhoneNumber": "0901234567",
    "CardLevel": "Member",
    "MemberPoint": 1500,
    "TotalPoint": 5000,
    "RedemptionValue": 15000,
    "CurrentRate": 10,
    "MemberCSN": "0901234567",
    "OtherInfo": "",
    "QRCode": "PHONE-0901234567",
    "ExtraPoint": false,
    "IsRedeem": false,
    "IsOfflineVinID": false,
    "IsShowMessage": false,
    "Status": "Hoạt động",
    "System": "CAP",
    "ClubCode": "WINCARE",
    "DateOfBirth": "01/01/1990",
    "Dob": "01/01/1990",
    "BirthdayGiftInd": false,
    "Gender": "M",
    "Email": "email@example.com",
    "Address": "...",
    "ExternalId": "WIN",
    "OtherStatus": null,
    "MemberType": "WIN",
    "Source": "CAP",
    "ExtendedFields": [],
    "AvailablePromotion": [],
    "MemberBusiness": null,
    "PointsSummaries": []
  },
  "MessageTechnical": ""
}
```

### Sub-flows stub trong scope này (chuyển đổi sau)

| Sub-flow | Mô tả | Xử lý tạm |
|---|---|---|
| VINID legacy | SĐT/thẻ VINID prefix "8888"/"66668"/"P" | Return 400 "VINID endpoint riêng" |
| WINCARE resolve | Nhân viên WinCare (12 ký tự) | Return 404 "WinCare chưa hỗ trợ" |
| WINX resolve | Thành viên WinX (14 ký tự) | Return 404 "WinX chưa hỗ trợ" |
| GetWinCodePromotion | Lấy danh sách mã CTKM | Return `[]` |
| GetMemberBusinessData | Lấy data hộ kinh doanh | Return `null` |
| GetOtherStatusLoyalty | WinScore/WinMoney status | Return `null` |
| GetProgramPointsSummaries | Tóm tắt điểm theo chương trình | Return `[]` |

---

## 2. Kiến trúc project mới (đã tối ưu)

### So sánh số tầng gọi

| | Code cũ | Project mới |
|---|---|---|
| Tầng 1 | `LoyaltyController` | `LoyaltyController` |
| Tầng 2 | `LoyaltyService` (business logic, 1300+ dòng) | `LoyaltyService` (business logic + mapping) |
| Tầng 3 | `LoyaltyCapillaryService` (auth token, logging) | `LoyaltyCapillaryHttpService` ← **gộp tầng 3+4 cũ** |
| Tầng 4 | `CapillaryService` (HTTP call sync) | *(đã gộp vào tầng 3)* |
| DI | `new()` trực tiếp — không testable | Constructor injection toàn bộ |

**Kết quả: giảm từ 4 tầng → 2 tầng hiệu quả.**

### Sơ đồ call flow mới

```
LoyaltyController
  → ILoyaltyService
    → ILoyaltyCapillaryService  (→ LoyaltyCapillaryHttpService: HTTP + auth + logging)
    → IDistributedCache          (Redis offline cache — dùng interface built-in .NET)
    → ISysWebApiConfigService    (lấy config Capillary từ DB)
```

### Lý do dùng `IDistributedCache` thay vì `IRedisService` tùy chỉnh

Code cũ dùng `RedisManager` với hash operations. Project mới chỉ cần key-value:
- Check offline flag: `await _cache.GetStringAsync("IsOfflineCapillary")`
- Cache member: `await _cache.SetStringAsync("BLUEPOS:Loyalty:{phone}", json, options)`
- Get cached: `await _cache.GetStringAsync("BLUEPOS:Loyalty:{phone}")`

`IDistributedCache` là standard .NET interface → mockable trong test, không cần tạo thêm 4 file
(`IRedisService`, `RedisService`, `ILoyaltyOfflineService`, `LoyaltyOfflineService`).

### Những gì đã có trong project mới (tái sử dụng)

| Component | File | Dùng cho |
|---|---|---|
| `ILoyaltyCapillaryService` | `POS.Application/Loyalty/Services/` | Interface 9 methods Capillary |
| `LoyaltyCapillaryHttpService` | `POS.Infrastructure/External/Capillary/` | HTTP call + auth token + logging (✅ đã impl) |
| `ISysWebApiConfigService` | `POS.Application/Shared/Services/` | Lấy config Capillary từ DB |
| `SysWebApiDto` | `POS.Application/Shared/DTOs/` | DTO config với `GetRoute()` |
| `CapCustomerDtos.cs` (và 4 file Capillary DTOs) | `POS.Application/Loyalty/DTOs/Capillary/` | DTO phản hồi từ Capillary API |
| `ResultResponse` | `POS.Shared/Models/` | Response wrapper `{Status, Message, Data, MessageTechnical}` |
| `BasicAuthFilter` | `POS.API/Filters/` | Authentication |

---

## 3. Checklist chuyển đổi

### Phase 1 — DTOs & Contracts
- [ ] **[DTO-1]** `POS.Application/Loyalty/DTOs/GetCustomerRequest.cs`
  - Fields: `NumberCard`, `PosId`, `StoreNo`, `ClubCode` (default ""), `IsMobile` (default false), `IsLog` (default true)
- [ ] **[DTO-2]** `POS.Application/Loyalty/DTOs/InfoMemberResponse.cs`
  - 30+ fields khớp 100% với JSON response mẫu ở mục 1
  - Sub-classes: `BLUEAvailablePromotion`, `MemberBusinessData`, `ExtendedFieldItem`, `ProgramPointData`
- [x] **[DTO-3]** `POS.Application/Loyalty/DTOs/Capillary/` ✅ — 5 files đã có:
  - `CapCommonDtos.cs`, `CapCustomerDtos.cs`, `CapTransactionDtos.cs`, `CapPointsDtos.cs`, `CapRedemptionDtos.cs`
- [ ] **[VAL-1]** `POS.Application/Loyalty/Validators/GetCustomerRequestValidator.cs`
  - `NumberCard`: NotEmpty, MinimumLength(9)
  - `PosId`, `StoreNo`: NotEmpty

### Phase 2 — Service Interface
- [x] **[IF-1]** `POS.Application/Loyalty/Services/ILoyaltyCapillaryService.cs` ✅ — 9 methods
- [ ] **[IF-2]** `POS.Application/Loyalty/Services/ILoyaltyService.cs`
  - `GetCustomerAsync(GetCustomerRequest request)` → `(int StatusCode, ResultResponse Body)`
  - *(Các endpoint Loyalty tiếp theo sẽ thêm method vào interface này)*

### Phase 3 — Infrastructure
- [x] **[INF-1]** `POS.Infrastructure/External/Capillary/LoyaltyCapillaryHttpService.cs` ✅ — đã impl đủ 9 methods
- [ ] **[INF-2]** Đăng ký Redis cache trong `POS.Infrastructure/DependencyInjection.cs`:
  ```csharp
  services.AddStackExchangeRedisCache(options =>
      options.Configuration = configuration["Redis:ConnectionString"]);
  ```
- [ ] **[INF-3]** `appsettings.json` — thêm:
  ```json
  "Redis": { "ConnectionString": "localhost:6379" }
  ```

### Phase 4 — Application Service
- [ ] **[SVC-1]** `POS.Application/Loyalty/Services/LoyaltyService.cs`

  **Inject:**
  - `ILoyaltyCapillaryService _capillaryService`
  - `ISysWebApiConfigService _configService`
  - `IDistributedCache _cache`
  - `ILogger<LoyaltyService> _logger`

  **Logic `GetCustomerAsync`:**
  ```
  1. IsVINID(numberCard)  → return 400 stub
  2. DetermineMemberType(numberCard, isMobile) → PHONE/ID/WINCARE/WINX/NONE
  3. NONE                 → return 400 "Số thẻ không hợp lệ"
  4. WINCARE/WINX         → return 404 stub
  5. IsOffline = cache.GetString("IsOfflineCapillary") != null
     → if true: return cached member từ cache.GetString("BLUEPOS:Loyalty:{phone}")
  6. PHONE: format "+84xxx"
  7. config = await _configService.GetByAppCodeAsync("CAPILLARY")
  8. result = await _capillaryService.GetCustomerDetailAsync(config, ...)
  9. 408/timeout → return cached offline data
     5xx         → return (101, offline response)
  10. Map CapGetCustomerResponse → InfoMemberResponse (30+ fields)
  11. Cache: await _cache.SetStringAsync("BLUEPOS:Loyalty:{phone}", json, midnight TTL)
  12. Return (200, ResultResponse { Status=200, Message="OK", Data=infoMember, MessageTechnical=clubCode })
  ```

  **Helper methods (private):**
  - `DetermineMemberType(string numberCard, bool isMobile)` → enum `MemberCapillaryType`
  - `IsVINID(string numberCard)` → bool
  - `MapToInfoMemberResponse(CapCustomerData customer, string memberCsn, string appCode)` → InfoMemberResponse

### Phase 5 — Controller & DI
- [ ] **[CTRL-1]** `POS.API/Controllers/LoyaltyController.cs`
  ```csharp
  [Route("api")]
  public class LoyaltyController : ControllerBase
  {
      [HttpGet("v2/loyalty/customer/get")]
      public async Task<IActionResult> GetCustomer(
          [FromQuery] string numberCard, [FromQuery] string posID,
          [FromQuery] string storeNo,   [FromQuery] string clubCode = "",
          [FromQuery] bool isMobile = false, [FromQuery] bool isLog = true)
  ```
  - Validate `numberCard.Length >= 9` trước khi gọi service
  - Map `(statusCode, body)` từ service → `StatusCode(statusCode, body)`

- [ ] **[DI-1]** `POS.Application/DependencyInjection.cs` — đăng ký:
  ```csharp
  services.AddScoped<ILoyaltyService, LoyaltyService>();
  ```

- [ ] **[DI-2]** `POS.Infrastructure/DependencyInjection.cs` — xác nhận đã đăng ký:
  ```csharp
  services.AddScoped<ILoyaltyCapillaryService, LoyaltyCapillaryHttpService>();
  // + AddStackExchangeRedisCache từ INF-2
  ```

### Phase 6 — Verification
- [ ] **[VRF-1]** `dotnet build` thành công không có warning/error
- [ ] **[VRF-2]** Test PHONE: SĐT 10 số, `isMobile=false` → response 30+ fields khớp JSON mẫu
- [ ] **[VRF-3]** Test invalid: `numberCard` < 9 ký tự → HTTP 400
- [ ] **[VRF-4]** Test WINCARE (12 ký tự, `isMobile=true`) → HTTP 404 stub
- [ ] **[VRF-5]** Test offline: set Redis key `IsOfflineCapillary=1` → trả dữ liệu cached
- [ ] **[DOC-1]** Cập nhật `docs/api-mapping.md` đánh dấu ✅ endpoint này

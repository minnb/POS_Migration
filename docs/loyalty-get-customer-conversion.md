# Loyalty — Convert `GET api/v2/loyalty/customer/get`

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
  │       ├─ Redis HashSet: cache InfoMemberModel theo phone
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

### Những gì đã có trong project mới (tái sử dụng)

| Component | File | Dùng cho |
|---|---|---|
| `ISysWebApiConfigService` | `POS.Application/Shared/Services/` | Lấy config Capillary từ DB |
| `SysWebApiDto` | `POS.Application/Shared/DTOs/` | DTO config với `GetRoute()` |
| `CapillaryHttpService` | `POS.Infrastructure/External/Capillary/` | Pattern HTTP call, token gen, phone format |
| `ResultResponse` | `POS.Shared/Models/` | Response wrapper |
| `BasicAuthFilter` | `POS.API/Filters/` | Authentication |

---

## 2. Checklist chuyển đổi

### Phase 1 — DTOs & Contracts
- [ ] **[DTO-1]** `POS.Application/Loyalty/DTOs/GetCustomerRequest.cs`
  - Fields: NumberCard, PosId, StoreNo, ClubCode, IsMobile, IsLog
- [ ] **[DTO-2]** `POS.Application/Loyalty/DTOs/InfoMemberResponse.cs`
  - 30+ fields khớp 100% với `InfoMemberModel` cũ
  - Sub-classes: ExtendedFieldItem, BLUEAvailablePromotion, MemberBusinessData, ProgramPointData
- [x] **[DTO-3]** `POS.Application/Loyalty/DTOs/Capillary/` ✅ — 5 files:
  - `CapCommonDtos.cs` — CapStatusCode, CapItemStatus, CapErrorResponse, CapServerError, CapIdentifier, CapFieldItem, CapCustomFields, CapExtendedFields
  - `CapCustomerDtos.cs` — CapGetCustomerResponse + full nested types, CapCustomerRegistration, CapCustomerUpdate, CapLedgerResponse
  - `CapTransactionDtos.cs` — CapAddTransactionRequest, CapAddTransactionResponse, CapLineItem, CapPaymentMode, CapRawSideEffect
  - `CapPointsDtos.cs` — CapPointsRedeemRequest/Response, CapPointReverseRequest/Response, CapPointRedeemableResponse
  - `CapRedemptionDtos.cs` — CapRedemptionResponse, CapRedemptionStatus, CapRedemptionData
- [ ] **[VAL-1]** `POS.Application/Loyalty/Validators/GetCustomerRequestValidator.cs`
  - NumberCard: NotEmpty, MinimumLength(9)
  - PosId, StoreNo: NotEmpty

### Phase 2 — Service Interfaces
- [x] **[IF-1]** `POS.Application/Loyalty/Services/ILoyaltyCapillaryService.cs` ✅
  - 9 methods: GetCustomerDetailAsync, CustomerRegistrationAsync, CustomerUpdateAsync, AddTransactionAsync, GetPointsLedgerAsync, PointsRedeemAsync, PointReverseAsync, PointsRedeemableAsync, RedemptionValidationDataAsync
- [ ] **[IF-2]** `POS.Application/Loyalty/Services/ILoyaltyOfflineService.cs`
  - `IsOfflineCapillaryAsync()` → bool
  - `GetCachedMemberAsync(string phoneNumber)` → InfoMemberResponse?
- [ ] **[IF-3]** `POS.Application/Loyalty/Services/ILoyaltyService.cs`
  - `GetCustomerAsync(GetCustomerRequest request)` → `(int StatusCode, ResultResponse Body)`
- [ ] **[IF-4]** `POS.Application/Shared/Services/IRedisService.cs`
  - `HashGetAsync<T>()`, `HashSetAsync<T>()`, `StringGetAsync()`, `StringSetAsync()`

### Phase 3 — Infrastructure
- [ ] **[INF-1]** Add NuGet `StackExchange.Redis` vào `POS.Infrastructure`
- [ ] **[INF-2]** `POS.Infrastructure/Services/RedisService.cs`
  - Implement `IRedisService` với `IConnectionMultiplexer`
- [x] **[INF-3]** `POS.Infrastructure/External/Capillary/LoyaltyCapillaryHttpService.cs` ✅
  - Implement `ILoyaltyCapillaryService` (9 methods, full async)
  - Pattern: `CallAsync()`, `CreateToken()`, `ToLowerCaseJson()` (lowercase property names)
  - PHONE: query by mobile, ID: query by id
  - DI đã đăng ký trong `POS.Infrastructure/DependencyInjection.cs`
- [ ] **[INF-4]** `POS.Infrastructure/Services/LoyaltyOfflineService.cs`
  - Implement `ILoyaltyOfflineService` dùng `IRedisService`
  - Key pattern: `BLUEPOS:Loyalty:{phone4chars}`, field = phoneNumber

### Phase 4 — Application Service
- [ ] **[SVC-1]** `POS.Application/Loyalty/Services/LoyaltyService.cs`
  - `DetermineMemberType(numberCard, isMobile)` → enum (PHONE/ID/WINCARE/WINX/NONE)
  - `IsVINID(numberCard)` → bool
  - PHONE/ID: full Capillary flow
  - WINCARE/WINX: return 404 stub
  - VINID: return 400 stub
  - Offline fallback khi 408/5xx
  - Map CapillaryResponse → InfoMemberResponse
  - Redis cache sau khi lấy được data

### Phase 5 — Controller & DI
- [ ] **[CTRL-1]** `POS.API/Controllers/LoyaltyController.cs`
  - `[HttpGet("v2/loyalty/customer/get")]`
  - Validate numberCard length >= 9 tại controller
  - Map (statusCode, body) từ service → IActionResult
- [ ] **[DI-1]** `POS.Application/DependencyInjection.cs` — đăng ký `ILoyaltyService`
- [ ] **[DI-2]** `POS.Infrastructure/DependencyInjection.cs` — đăng ký Redis, `ILoyaltyCapillaryService`, `ILoyaltyOfflineService`
- [ ] **[CFG-1]** `appsettings.json` — thêm section `Redis:ConnectionString`

### Phase 6 — Verification
- [ ] **[VRF-1]** `dotnet build` thành công không có warning/error
- [ ] **[VRF-2]** Test PHONE type: gọi với SĐT hợp lệ → verify 30+ fields trong response JSON
- [ ] **[VRF-3]** Test invalid: numberCard < 9 ký tự → verify 400 response
- [ ] **[VRF-4]** Test offline: set Redis key `IsOfflineCapillary=1` → verify trả dữ liệu cached
- [ ] **[VRF-5]** Test WINCARE stub → verify 404 response đúng message
- [ ] **[DOC-1]** Cập nhật `docs/api-mapping.md` đánh dấu ✅ endpoint này

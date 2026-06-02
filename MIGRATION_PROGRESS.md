# TIẾN ĐỘ CHUYỂN ĐỔI DỰ ÁN (.NET 4.6 -> .NET 10)

## 1. LỜI GỌI HỆ THỐNG DÀNH CHO AI AGENT (BẮT BUỘC ĐỌC)
- Khi bắt đầu phiên làm việc **MỚI**, AI Agent **BẮT BUỘC** phải đọc file này và file `MIGRATION_RULES.md` đầu tiên.
- SAU KHI hoàn thành convert bất kỳ module/file nào, AI Agent **BẮT BUỘC** phải mở file này ra và cập nhật lại tiến độ trước khi kết thúc tác vụ.

## 2. TRẠNG THÁI TỔNG QUAN
- **Tiến độ ước tính:** **≈ 95 %** — tất cả 17 controller files đã chuyển đổi xong
- **Cập nhật lần cuối:** 2026-06-03 (+07:00)

---

## 3. CÁC MODULE ĐÃ HOÀN THÀNH (DONE)

### 3.1 Controllers (Api layer)

| Controller (WebApi → src) | Endpoints | Ghi chú |
|---|---|---|
| `BaseController.cs` | — | ✅ |
| `HomeController.cs` | — | ✅ |
| `CommonController.cs` | — | ✅ |
| `QueueController.cs` | 3 | ✅ |
| `SyncDataPosController.cs` | — | ✅ |
| `SettingController.cs` | — | ✅ removed per user request |
| `CapillaryController.cs` | — | ✅ removed per user request |
| `WinCareController.cs` | — | ✅ |
| `GiftController.cs` | — | ✅ |
| `OfferController.cs` | — | ✅ |
| `PaymentController.cs` | 7 | ✅ Routes: `api/v2/partner/*`. **Rewritten hoàn toàn** — sửa 5 issues: wrong .NET 4.x syntax, 5 missing endpoints, phantom interfaces, wrong try-catch, wrong error messages |
| `WinpayController.cs` | — | ✅ |
| `LoyaltyController.cs` | 22 | ✅ Routes: `api/v2/loyalty/*` + `api/vinid/*`. **Đã rà soát & sửa 5 lỗi validation** (Sales, SalesV2, InitTransaction, AddTransaction, RefundTransaction) |
| `ValidateController.cs` | 5 | ✅ Routes: `api/v2/otp/*`, `api/v2/validate/*`, `api/v2/invoice/*` |
| `SAPController.cs` | 11 | ✅ Routes: `api/sap/*`. SAP SOAP là stub (cần WCF regen). ROP REST đầy đủ |
| `WinLifeController.cs` | 8 | ✅ Routes: `api/v2/otp/*`, `api/blue/winlife/*` |
| `VoucherController.cs` | 3 | ✅ Routes: `api/vc/*` — CX Voucher OTP + UpdateStatus |
| `VoucherTopUpVinIDController.cs` | 6 | ✅ Routes: `api/vinid/*` — VINID TopUp + EVoucher (HMAC RSA signature) |
| `PLGController.cs` | 16 | ✅ Routes: `api/plg/*` — UrBox/GiftBox/GotIt/Giftee/Odoo SAP/CX |

### 3.2 Application Services

| Interface → Implementation | Mô tả |
|---|---|
| `ICommonService` → `CommonService` | Health check, IP lookup |
| `ICXService` → `CXService` | CrownX OTP (Generate + Verify) |
| `ILoyaltyService` → `LoyaltyService` | VINID + Capillary loyalty (22 use cases) |
| `ISAPService` → `SAPService` | SAP/ROP/PLG/Capillary voucher (11 use cases) |
| `IWinLifeService` → `WinLifeService` | WinLife OTP, đăng ký thành viên, WinCode (8 use cases) |
| `IValidateService` → `ValidateService` | Tax code, transaction validate, invoice, member business |
| `IOfferService` → `OfferService` | Offer/promotion |
| `IGiftService` → stub | Gift cards |
| `IWinCareService` → stub | WinCare |
| `IWinpayService` → `WinpayService` | Winpay (+ thêm `SendOtpWinpayAsync`) |
| `IVoucherService` → `VoucherService` | CX Voucher OTP + UpdateStatus |
| `IVinIdTopUpService` → `VinIdTopUpService` | VINID TopUp + EVoucher |
| `IPLGService` → `PLGService` | PLG partner vouchers dispatcher |
| `IUrboxService` → (infra) | UrBox check + payment |

### 3.3 Infrastructure

| Component | Mô tả |
|---|---|
| `SqlConnectionFactory` | Dapper connection factory |
| `LoyaltyRepository` | VINID store mapping, loyalty DB |
| `CommonRepository` | Common DB queries |
| `OfferRepository` | Offer DB |
| `PosTerminalRepository` | Terminal lookup |
| `ValidateRepository` | Transaction validate, invoice insert, member business |
| `WinCodeRepository` | WinCodeCustomer table (insert/update) |
| `SapVoucherSoapClient` | SAP SOAP stub (pending WCF regen từ WSDL) |
| `RopVoucherService` | ROP REST API đầy đủ (Basic Auth, routes từ SysWebApiRoute) |
| `MemoryCacheService` | Master data cache |
| `RedisManager` | Redis Sentinel |
| `RabbitMqMessageQueueService` | RabbitMQ messaging |
| `SmsService` | SMS via RabbitMQ queue |
| `ApiCallClient` | HTTP partner API client |
| `OneFlexiAxisGateway` | WinLife SOAP |
| `SerilogRequestLogger` | Logging |

### 3.4 Shared DTOs (mới tạo/cập nhật phiên này)

| File | Nội dung |
|---|---|
| `SapDtos.cs` | SAP SOAP I/O, ROP API, PLG request/response, SendCode DTOs |
| `ValidateDtos.cs` | Tax, transaction validate, invoice, member business DTOs |
| `WinLifeDtos.cs` | WinLife OTP, register, update-promotions, history DTOs |
| `VoucherDtos.cs` | CX voucher OTP/serial, VINID TopUp/EVoucher DTOs |
| `PlgDtos.cs` | PLG/UrBox/GiftBox/GotIt/Giftee request DTOs + UrBox partner DTOs |
| `LoyaltyRequestDtos.cs` | VinId Sales/Refund, Customer register/update, ScanAndGo, etc. |
| `LoyaltyResponseDtos.cs` | InfoMemberDto, ScanAndGoPosResponseDto, etc. |
| `VinIdApiDtos.cs` | Raw VINID REST API shapes |

---

## 4. ĐANG TIẾN HÀNH / CÒN LẠI (stubs chờ implement)

| Item | Vị trí | Việc cần làm |
|---|---|---|
| **SAP SOAP** | `SapVoucherSoapClient.cs` | Regenerate WCF client từ WSDL SAP bằng `dotnet-svcutil` |
| **Odoo SAP SOAP** | `PLGService.CheckVcOdooAsync / OdooSapUpdateVoucherAsync` | Regenerate WCF client `SI_VC_OUTService` |
| **SyncDataPosService** | `SyncDataPosService.cs` | Tích hợp Kafka + Staging DB |
| **LegacySyncDataToPosClient** | `LegacySyncDataToPosClient.cs` | Thay bằng DLL thực khi có |
| **WinX dynamic voucher** | `SAPService.ResolveWinXVoucherAsync` | Implement khi có WinX API config |
| **Olala partner** | `SAPService.CheckOlalaByPhoneAsync` | Port `UsingVoucherPartnerService` |
| **LoyaltyRetry** | `LoyaltyService.LoyaltyRetryAsync` | Port `LoyaltyOfflineService` (Redis-based) |

---

## 5. LƯU Ý KỸ THUẬT QUAN TRỌNG

### Routes giữ nguyên 100%
- `LoyaltyController` dùng `[Route("api")]` — không có prefix chung
- `WinLifeController` dùng `[Route("api")]` — endpoint có cả `api/v2/otp/*` và `api/blue/winlife/*`
- `SAPController` dùng `[Route("api/sap")]`
- `VoucherController` dùng `[Route("api/vc")]`
- `VoucherTopUpVinIDController` dùng `[Route("api/vinid")]`
- `PLGController` dùng `[Route("api/plg")]`
- `ValidateController` dùng `[Route("api/v2")]`

### Các fix quan trọng phiên này
- **LoyaltyController** — sửa 5 lỗi validation thiếu so với legacy:
  - `Sales`: thêm check `InvoiceNo` + `OrderAmount > 0`
  - `SalesV2`: thêm check `MerchantId` + `TerminalId` + `InvoiceNo` + `OrderAmount > 0`
  - `InitTransaction`: thêm check `QRCode` + `MerchantId` + `TerminalId` + VINID card guard
  - `AddTransaction` + `RefundTransaction`: thêm `ModelState.IsValid`
- **IUrboxService**: cập nhật import từ `Api.Models.Gift` → `Shared.DTOs` (fix vi phạm clean architecture)
- `CheckVoucherPartnerPOSRequest`, `UpdateStatusVoucherPartnerRequest`, `LstVoucherPartner` chuyển sang `Shared/DTOs/PlgDtos.cs`

### SysWebApi AppCode conventions
| AppCode | Dùng cho |
|---|---|
| `CX` | CrownX API (OTP, member, sale, refund) |
| `VINID` | VINID Loyalty REST API |
| `VINCART` | ScanAndGo / Extra Sales API |
| `CAPILLARY` | Capillary Loyalty API |
| `WINLIFE` | WinLife SOAP (OneFlexiAxis) |
| `ROP` | ROP Voucher REST API |
| `PLG` | Phúc Long Group API |
| `PARTNER` | VietQR / Tax partner API |
| `URBOX` | UrBox voucher API |
| `GIFTBOX` | GiftBox voucher API |
| `GOTIT` | GotIt voucher API |
| `GIFTEE` | Giftee voucher API |
| `VINID_TOPUP` | VINID TopUp + EVoucher API |
| `WINPAY` | Winpay API |

---

> **NOTE:** Đây là "bộ nhớ trung tâm" cho toàn bộ AI Agent tham gia dự án. Mọi cập nhật, sửa đổi hay bổ sung tiến độ **phải** được ghi lại ở đây ngay khi một module được hoàn thành hoặc có thay đổi quan trọng.

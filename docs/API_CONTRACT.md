# API Contract — POS API (POS.Api)

> **Mục đích:** Tài liệu "golden contract" — danh sách đầy đủ mọi route, HTTP verb, request schema,
> response schema, và external dependency mà **5.000 máy POS** đang phụ thuộc. Dùng làm checklist
> QA mỗi khi thêm/sửa endpoint hiện hữu — tên field JSON trong tài liệu này **không được đổi**.
>
> **Ngày tạo:** 2026-06-08 (chụp lại từ contract gốc .NET Framework 4.6.2/WebAPI 2 — stack nguồn đã
> ngừng dùng, dự án nay là **POS.Api trên .NET 10**). Khi thêm endpoint mới, cập nhật tài liệu này
> trong cùng commit.

---

## 1. Universal Response Format

Tất cả endpoint trả về `ResultResponse` (trừ HomeController là MVC):

```json
{
  "Data": <object | null>,
  "Message": "string",
  "Status": <HttpStatusCode int>,
  "MessageTechnical": "string | null"
}
```

`MessageTechnical` chứa stack trace / technical detail — không hiển thị cho end-user.

**Status codes dùng trong source:**
| Code | Ý nghĩa |
|------|---------|
| 200 OK | Thành công |
| 400 BadRequest | Validation lỗi, logic lỗi |
| 404 NotFound | Không tìm thấy resource |
| 409 Conflict | Trùng lặp (idempotent duplicate), OTP sai |
| 429 TooManyRequests | Rate limit (RateLimitMiddleware: 100 req/min/IP) |
| 500 InternalServerError | Exception không xử lý được |

---

## 2. Authentication

| Cơ chế | Controller | Ghi chú |
|--------|-----------|---------|
| Không có inbound auth | Hầu hết controllers | API chạy trong nội bộ / VPN |
| Basic Auth (inbound) | `PLGController` | `AuthenAPI.AuthorizationBasic(ActionContext)` — một số endpoint yêu cầu header `Authorization: Basic <base64>` |
| Basic Auth (outbound) | `VoucherController`, `PLGController`, `SAPController` | Gọi ra CrownX / PLH API với `Authorization: Basic Base64(user:pass)` |
| HMAC SHA256 (outbound) | `VoucherTopUpVinIDController` | Gọi ra VinID — header: `X-Key-Code`, `X-Timestamp`, `X-Nonce`, `X-Signature`, `X-Request-ID`; raw data = `"{url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};[body]"` → SHA256 |
| Bearer Token (outbound) | `PLGController` (Giftee) | `Authorization: Bearer {GifteeToken}` |

---

## 3. Logging & Side Effects chung

- **KibanaService**: Serilog wrapper, structured logging mọi request/response.
- **OpsMonitoringHelper**: Fire-and-forget RabbitMQ message tới queue `Queue_Ops_Logging` (table `webapi_error_logs` / `webapi_status`).
- **Pattern `_ = Task.Run(...)`**: Fire-and-forget logging — không ảnh hưởng response.
- **Timeout HTTP client**: `TimeSpan.FromMinutes(2)` là chuẩn mặc định cho external calls; ngoại lệ GiftController dùng 30s `CancellationTokenSource`.

---

## 4. Controllers & Routes

---

### 4.1 CommonController

**RoutePrefix:** `api/common`  
**Base:** `BaseController` → `ApiController`  
**Auth:** Không

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/common/TransactionIssue` | Query: `storeNo`, `posTerminal` | object | Tra cứu phiếu issue |
| 2 | GET | `api/common/GetCurrentTime` | — | `{ ServerTime: datetime }` | Thời gian server |
| 3 | GET | `api/common/GetBusinessDate` | Query: `storeNo` | `string` (business date) | Ngày kinh doanh |
| 4 | GET | `api/common/CheckEndShift` | Query: `storeNo`, `posTerminal` | object | Check trạng thái chốt ca |
| 5 | POST | `api/common/POSMonitor` | Body: `POSMonitorInsertRequest` | null | Insert monitor POS; fire-and-forget log |
| 6 | GET | `api/common/CheckIPaddressPos` | Query: `posTerminal` | `string` (IP) | Kiểm tra IP máy POS |
| 7 | GET | `api/common/POSDataSetup` | Query: `storeNo` | `List<POSDataSetup>` | Cấu hình POS từ Redis/DB |
| 8 | GET | `api/common/GetPOSVersion` | — | `string` | Phiên bản DLL API |
| 9 | GET | `api/common/GetOrderInfo` | Query: `orderNo` | object | Thông tin đơn hàng |
| 10 | GET | `api/common/WriteFileByManual` | Query: `storeNo`, `posTerminal` | `string` | Trigger write SOD file |
| 11 | GET | `api/common/GetListPOSDocumentNo` | Query: `storeNo`, `posTerminal` | `List<string>` | Danh sách số phiếu POS |
| 12 | GET | `api/common/CheckCouponLine` | Query: `couponNo` | object | Kiểm tra coupon line |
| 13 | POST | `api/common/UpdateOrderTrans` | Body: `UpdateOrderInfoModel` | null | Cập nhật trạng thái đơn hàng |
| 14 | GET | `api/common/GetInsurance` | Query: `orderNo` | object | Tra cứu bảo hiểm đơn hàng |
| 15 | PUT | `api/common/UpdateEOD` | Body: `POSEOD_APIModel` | null | Cập nhật EOD; ghi vào `BussinessDateOpen` |
| 16 | GET | `api/common/CheckTotalBill` | Query: `orderNo` | object | Kiểm tra tổng tiền hóa đơn |
| 17 | POST | `api/common/kios/insert-sale` | Body: `KiosInsertSalePOSRequest` | object | Insert đơn bán từ Kios |
| 18 | GET | `api/common/kios/check-order` | Query: `orderNo` | object | Kiểm tra trạng thái đơn Kios |
| 19 | GET | `api/common/SendCodeReward` | Query: `phone`, `storeNo` | null | Gửi mã phần thưởng |
| 20 | POST | `api/common/logging` | Body: log object | null | Ghi log từ POS client |

**Key models (tóm tắt):**

```
POSMonitorInsertRequest   { StoreNo, PosTerminal, Status, ... }
UpdateOrderInfoModel      { OrderNo, StoreNo, PosTerminal, ... }
POSEOD_APIModel           { StoreNo, PosTerminal, BusinessDate, ... }
KiosInsertSalePOSRequest  { OrderNo, StoreNo, PosTerminal, Items[], ... }
```

---

### 4.2 LoyaltyController

**RoutePrefix:** `api`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** Dùng `private static readonly` singleton cho `LoyaltyService`, `RedisManager`, `MemoryCacheService`, `AkaChainLoyaltyService`, `LoyaltyOfflineService` — tránh per-request instantiation.

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/v2/loyalty/customer/get` | Query: `numberCard`, `posID`, `storeNo`, `clubCode?`, `isMobile?`, `isLog?` | Member profile object | Gọi `AkaChainLoyaltyService.GetMemberProfile`. Logic Capillary offline (commented out) |
| 2 | POST | `api/v2/loyalty/customer` | Body: `WinLife_Register_POS_Request` | object | Đăng ký hội viên; verify OTP via `CXService.VerifyOTP` trước |
| 3 | POST | `api/v2/loyalty/customer/update` | Body: `WinLife_SmartPOS_Update_Customer_Req_POS` | object | Cập nhật thông tin hội viên |
| 4 | POST | `api/v2/loyalty/transaction/add` | Body: `VinIDSalesRequest` | object | Tích điểm; gọi `AkaChainLoyaltyService.InputDataAsync` |
| 5 | POST | `api/v2/loyalty/transaction/refund` | Body: `VinIDRefundRequest` | object | Hoàn điểm; check offline fallback trước |
| 6 | POST | `api/v2/loyalty/other-status` | Body: `OtherStatusUpdate { PhoneNumber, ... }` | object | Cập nhật trạng thái phụ hội viên |

**Key models:**

```
WinLife_Register_POS_Request    { phoneNo, otp, posCode, fullName, ... }
VinIDSalesRequest               { CardNumber, OrderNo, TotalAmount, StoreNo, PosTerminal, Items[] }
VinIDRefundRequest              { CardNumber, OrderNo, OrigOrderNo, StoreNo, PosTerminal }
OtherStatusUpdate               { PhoneNumber, ... }
```

**Offline fallback pattern:**
```
if (await _loyaltyOfflineService.IsOfflineCapillary())
  → return GetMemberInfoOfflineSwitch() / GetAddTransactionOfflineSwitch()
```

---

### 4.3 GiftController

**RoutePrefix:** Không có — dùng full path trong `[Route("...")]`  
**Base:** `ApiController` (trực tiếp)  
**Auth:** Không  
**Đặc biệt:** Dùng `[ValidateModel]` attribute; 30s CancellationToken timeout cho `claim`

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/gifts/claim` | Body: claim request | `ClaimMMLSchemaResponse` | `CancellationTokenSource(30s)`. Parallel `Task.WhenAll(winXTask, mmlTask)` nếu code KHÔNG phải `THEWINX_QRCode` |
| 2 | POST | `api/pos/gift` | Body: gift request | object | Tặng quà từ POS |

**Lưu ý quan trọng:**
- `api/v2/gifts/claim`: nếu timeout 30s → trả 408 / error; parallel WhenAll chạy winX task và MML task đồng thời.

---

### 4.4 PaymentController

**RoutePrefix:** `api/v2/partner`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** Routing theo `Partner` field — switch case: `URBOX | GOTIT | ONEU | CAP`

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/partner/voucher/check` | Body: `CheckVoucherPartnerPOSRequest` | voucher info object | Route tới partner service tương ứng theo `Partner` |
| 2 | POST | `api/v2/partner/voucher/update-status` | Body: update request | object | Cập nhật trạng thái voucher partner |
| 3 | POST | `api/v2/partner/coupon/check` | Body: coupon check request | coupon info | Route theo `Partner` |
| 4 | POST | `api/v2/partner/coupon/redeem` | Body: coupon redeem request | object | Sử dụng coupon partner |
| 5 | GET | `api/v2/partner/coupon/list/user` | Query: `phoneNumber`, `partner` | `List<coupon>` | Danh sách coupon của user |
| 6 | GET | `api/v2/partner/coupon/detail` | Query: `couponCode`, `partner` | coupon detail | Chi tiết coupon |
| 7 | POST | `api/v2/partner/coupon/re-active` | Body: re-active request | object | Kích hoạt lại coupon |

**Partner values (case-insensitive toUpper):** `URBOX`, `GOTIT`, `ONEU`, `CAP`

---

### 4.5 CapillaryController

**RoutePrefix:** `api`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** Redis hash dedup — `HashGet<string>(key, orderNo)` → trả 409 nếu đã tồn tại

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/loyalty/transaction/points/refund` | Body: refund request | object | Hoàn điểm Capillary |
| 2 | POST | `api/v2/loyalty/point/topup/revert` | Body: revert request | object | Rollback topup điểm |
| 3 | POST | `api/v2/loyalty/point/topup` | Body: topup request | object | Nạp điểm Capillary; Redis dedup theo `OrderNo` → 409 nếu duplicate |
| 4 | POST | `api/v2/loyalty/transaction/points` | Body: `VinIDSalesRequest`-like | object | Ghi nhận giao dịch điểm |
| 5 | POST | `api/v2/loyalty/transaction/program-points` | Body: request | object | Điểm chương trình đặc biệt |
| 6 | POST | `api/v2/loyalty/transaction/member-business` | Body: member-business request | object | Giao dịch hội viên doanh nghiệp |
| 7 | POST | `api/v2/loyalty/winscore/update` | Body: winscore request | object | Cập nhật WinScore |
| 8 | GET | `api/v2/loyalty/points/history` | Query: `phoneNumber`, params | `List<point history>` | Lịch sử điểm |
| 9 | POST | `api/v2/loyalty/capillary/mobile-enroll` | Body: mobile enroll request | object | Đăng ký mobile Capillary |
| 10 | GET | `api/v2/loyalty/capillary/transactions` | Query: params | `List<transaction>` | Lịch sử giao dịch Capillary |
| 11 | GET | `api/v2/loyalty/capillary/check` | Query: params | status object | Kiểm tra kết nối Capillary |
| 12 | GET | `api/v2/loyalty/capillary/check/switch/winx` | — | status object | Kiểm tra trạng thái switch WinX mode |
| 13 | POST | `api/v2/loyalty/capillary/action` | `[FromUri]` params (action type) | object | **Admin endpoint** — switch Capillary/WinX mode; ghi Redis |
| 14 | GET | `api/v2/loyalty/capillary/customer/redemptions` | Query: `phoneNumber` | `List<redemption>` | Lịch sử redemption của hội viên |
| 15 | POST | `api/v2/loyalty/capillary/points/redeem/revert` | Body: revert request | object | Rollback redemption điểm |

**Lưu ý quan trọng:**
- Endpoint #3, #4: Redis hash dedup key = `RedisConst.GetRedisKeyLoyaltyMemberPoints(model.OrderNo)`, field = `model.OrderNo`. Nếu key tồn tại → 409 Conflict.
- Endpoint #13: `[FromUri]` không phải `[FromBody]` — tham số trong query string.

---

### 4.6 OfferController

**RoutePrefix:** `api`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** Routes cũ `promotion/staff/*` gọi external HTTP với timeout 2 phút; routes mới `v2/offer/staff/*` gọi internal service

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/offer/staff/points/topup/retry` | Body: retry request | object | Retry topup điểm staff |
| 2 | GET | `api/v2/offer/staff/points/topup` | Query: params | object | Tra cứu topup điểm staff |
| 3 | GET | `api/v2/offer/staff/check` | Query: `storeNo`, `posTerminal`, `couponCode` | coupon status | Kiểm tra offer staff (v2 internal) |
| 4 | POST | `api/v2/offer/staff/apply` | Body: apply request | object | Áp dụng offer staff (v2 internal) |
| 5 | GET | `api/promotion/staff/force-check` | Query: params | coupon status | **Legacy** — gọi external `PromotionStaffUrl` với timeout 2 phút; headers: `UserName`, `Password` |
| 6 | GET | `api/promotion/staff/check` | Query: params | coupon status | **Legacy** — gọi external `PromotionStaffUrl`; timeout 2 phút |
| 7 | POST | `api/promotion/staff/redeem` | Body: redeem request | object | **Legacy** — gọi external redeem; timeout 2 phút |
| 8 | POST | `api/promotion/staff/refund` | Body: refund request | object | **Legacy** — gọi external refund; timeout 2 phút |

**AppSettings used:** `PromotionStaffUrl`, `PromotionStaffUser`, `PromotionStaffPass`  
**External call pattern (legacy endpoints):**
```csharp
client.DefaultRequestHeaders.Add("UserName", PromotionStaffUser);
client.DefaultRequestHeaders.Add("Password", PromotionStaffPass);
client.Timeout = TimeSpan.FromMinutes(2);
```

---

### 4.7 VoucherController

**RoutePrefix:** `api/vc`  
**Base:** `ApiController` (trực tiếp, KHÔNG kế thừa BaseController)  
**Auth inbound:** Không  
**Auth outbound:** Basic Auth tới CrownX (`Authorization: Basic Base64(CXUser:CXPassword)`)

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/vc/generateOTP` | Query: `phoneNumber`, `storeNo`, `posTerminal` | `{ OTP: string }` | Tạo OTP cho khách; gọi CrownX API |
| 2 | GET | `api/vc/getVoucherSerial` | Query: `voucherSerial`, `storeNo`, `posTerminal` | voucher info | Lấy thông tin voucher serial |
| 3 | POST | `api/vc/UpdateVoucherStatus` | Body: update request | null | Cập nhật trạng thái voucher sau sử dụng |

**AppSettings used:** `CXUrl`, `CXUser`, `CXPassword`

---

### 4.8 VoucherTopUpVinIDController

**RoutePrefix:** `api/vinid`  
**Base:** `BaseController`  
**Auth inbound:** Không  
**Auth outbound VinID:** HMAC SHA256 — headers: `X-Key-Code`, `X-Timestamp`, `X-Nonce`, `X-Signature`, `X-Request-ID`

**HMAC rawData format:**
```
"{url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};[body]" → SHA256(rawData + key)
```

**Store mapping:** `logVID.StoreMapping(storeNo, posID)` → trả `OldTerminalID`, `OldStoreID`

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/vinid/TopUpCheckMember` | Query: `phone`, `storeNo`, `posTerminal` | member info | Tra cứu thông tin thành viên VinID; HMAC outbound |
| 2 | POST | `api/vinid/TopUpPoinToPhone` | Body: topup request | object | Nạp điểm cho số điện thoại; HMAC outbound |
| 3 | GET | `api/vinid/TopUpCheckStatusOrder` | Query: `orderNo`, `storeNo` | order status | Kiểm tra trạng thái đơn topup |
| 4 | GET | `api/vinid/EVoucherVerify` | Query: `voucherCode`, params | voucher info | Xác thực e-voucher VinID |
| 5 | POST | `api/vinid/EVoucherRefund` | Body: refund request | object | Thu hồi e-voucher |
| 6 | POST | `api/vinid/EVoucherMarkUsed` | Body: mark-used request | object | Đánh dấu e-voucher đã sử dụng *(note: có version cũ commented out ở trên)* |

**AppSettings used:** `vc_topup_link`, `vc_topup_keycode`, `topup_get_user_by_phone`, `topup_point_phone`, `topup_status_order`, `evoucher_verify`, `evoucher_mark_used`, `evoucher_revoke`, `evoucher_refund`, `TOPUP_ITEM_VINID`

---

### 4.9 WinCareController

**RoutePrefix:** `api`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** `WinPayAccumulation` (endpoint #1/#2) đọc condition từ DB/Redis cache, validate date range trước khi xử lý

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/wincustomer/wpay/shopping-accumulate` | Body: `WinPayShoppingAccumulateRequest` | object | Tích lũy mua sắm WinPay v2; check date condition |
| 2 | POST | `api/v3/wincustomer/wpay/shopping-accumulate` | Body: `WinPayShoppingAccumulateRequest` | object | Tích lũy mua sắm WinPay v3 (cập nhật logic) |
| 3 | POST | `api/v2/wincare/OrderSupplierPublic/GenOspQRLogin` | Body: QR login request | `{ QRCode: string }` | Tạo QR đăng nhập nhà cung cấp |
| 4 | POST | `api/v2/wincare/collect-money/barcode` | Body: `{ Barcode, StoreNo, PosTerminal }` | object | Thanh toán thu tiền bằng barcode |
| 5 | POST | `api/v2/wincare/collect-money/confirm` | Body: confirm request | object | Xác nhận thu tiền |
| 6 | POST | `api/v2/wincare/notify` | Body: notify request | null | Gửi thông báo WinCare |

**Commented out (không active):**
- `api/v2/salary/view-salary-old`
- `api/v2/salary/view-salary-news`

**Services used:** `WinCareService`, `WinCustomerService`, `ROPVoucherService`, `MemoryCacheService`, `KibanaService`, `SalaryBLO`

---

### 4.10 WinLifeController

**RoutePrefix:** `api`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:**
- OTP endpoints route tới CrownX hoặc WinPay tùy theo `action` param mapping
- `blue/winlife/register` + `update-promotions`: WINCARE stores → Capillary, các store khác → WinLife service

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/v2/otp/generate` | Query: `phone`, `storeNo`, `posTerminal`, `action?` | `{ OTP: string }` | Tạo OTP — route tới CX hoặc WinPay dựa vào `action` |
| 2 | POST | `api/v2/otp/verify` | Body: `{ Phone, OTP, Action, StoreNo, PosTerminal }` | object | Xác thực OTP |
| 3 | GET | `api/blue/winlife/generateOTP` | Query: `phone`, `storeNo`, `posTerminal` | `{ OTP }` | OTP cho WinLife (legacy endpoint) |
| 4 | POST | `api/blue/winlife/register` | Body: `WinLife_Register_POS_Request` | member object | Đăng ký hội viên WinLife/Capillary; routing theo store |
| 5 | POST | `api/blue/winlife/update-promotions` | Body: update request | object | Ghi nhận giao dịch áp dụng CTKM đặc biệt |
| 6 | GET | `api/blue/winlife/winCode-histories` | Query: `phone`, `storeNo`, dates | `List<history>` | Lịch sử WinCode |
| 7 | GET | `api/blue/winlife/smart-pos/customer-by-last-digits-phone` | Query: `lastDigits`, `storeNo` | `List<customer>` | Tra cứu khách theo 4 số cuối SĐT |
| 8 | POST | `api/blue/winlife/smart-pos/update-customer-info` | Body: update request | object | Cập nhật thông tin khách Smart POS |

**AppSettings used:** `CXUrl`, `CXUser`, `CXPassword`  
**External:** CrownX (outbound Basic Auth), WinLife service (internal)

---

### 4.11 WinpayController

**RoutePrefix:** `api/v2/winpay`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** `FormatHelper.PhoneNumberVietNam()` chuẩn hóa số điện thoại trước khi gọi service; response mapping từ `NewHttpResponseDto` với status 200/409/404/400

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/winpay/register` | Body: `RegisterWinpayPOS` | object | Đăng ký WinPay; ModelState validation |
| 2 | GET | `api/v2/winpay/get-register-info` | Query: `phone` | object | Tra cứu thông tin đăng ký; phone normalized → last 4 digits làm key |
| 3 | POST | `api/v2/winpay/unregister` | Body: `UnregisterWinpayPOS { PhoneNumber, ... }` | object | Hủy đăng ký WinPay |
| 4 | POST | `api/v2/winpay/payment` | Body: `PaymentWinpayPOS { PhoneNumber, ... }` | object | Thanh toán WinPay (`action=payment`) |
| 5 | POST | `api/v2/winpay/refund` | Body: `RefundWinpayPOS { PhoneNumber, ... }` | object | Hoàn tiền WinPay |
| 6 | POST | `api/v2/winpay/deposit` | Body: `PaymentWinpayPOS` | object | Nạp tiền WinPay (`action=cashin`) |
| 7 | POST | `api/v2/winpay/withdraw` | Body: `WithdrawWinpayPOS { PhoneNumber, ... }` | object | Rút tiền WinPay (`action=cashout`) |
| 8 | POST | `api/v2/winpay/fp-update` | Body: `UpdateFingerWinpayPOS { PhoneNumber, ... }` | object | Cập nhật fingerprint |
| 9 | POST | `api/v2/winpay/fp-verify` | Body: `VerifyFingerWinpayPOS { PhoneNumber, ... }` | object | Xác thực fingerprint |
| 10 | POST | `api/v2/winpay/cashback` | Body: `CashbackWinpayPOS { PhoneNumber, ... }` | object | Hoàn tiền cashback |

**Response status mapping (ConvertHttpResponseMessage):**
```
NewHttpResponseDto.StatusCode == 200  → 200 OK
NewHttpResponseDto.StatusCode == 409  → 409 Conflict
NewHttpResponseDto.StatusCode == 404  → 404 NotFound
else                                  → 400 BadRequest
```

---

### 4.12 PLGController

**RoutePrefix:** `api/plg`  
**Base:** `ApiController` (trực tiếp)  
**Auth inbound:** Một số endpoint yêu cầu `AuthenAPI.AuthorizationBasic(ActionContext)`  
**External calls:** CrownX (Basic), SAP Odoo (SOAP + NetworkCredential), UrBox, GiftBox, GotIt, Giftee (Bearer)

| # | Method | Route | Auth | Request | Response Data | Notes |
|---|--------|-------|------|---------|---------------|-------|
| 1 | POST | `api/plg/GetInfoCard` | ✅ Basic | Body: `PLCheckVoucherRequest { partner, storeNo, posID, listSeriNo[] }` | `List<InforVoucherResponse>` | Route tới PLG / UrBox / GiftBox / GotIt / Giftee dựa theo `partner` |
| 2 | PUT | `api/plg/SaleVoucher` | ✅ Basic | Body: `SaleVoucherRequestPOS { partner, storeNo, posID, seriNo, salePrice, staffCode }` | `VoucherCardTransResponse` | Chỉ `partner="PLG"` được bán; fire-and-forget `LogVoucher` |
| 3 | POST | `api/plg/RedeemVoucher` | ✅ Basic | Body: `ReedemVoucherRequestPOS { partner, storeNo, posID, orderNo, listSeriNo[], totalBill, staffCode }` | null | Route tới PLG / UrBox / GiftBox / GotIt |
| 4 | POST | `api/plg/Check_VC_Odoo` | ❌ | Body: `PLCheckVoucherRequest` | `List<InforVoucherResponse>` | Gọi SAP Odoo SOAP WS `SI_VC_CHECK_OUT` |
| 5 | POST | `api/plg/OdooSAP_Update_Voucher` | ❌ | Body: `OdooUpdateVoucherRequest { seriNo, isVoucher, statusCode, salePrice, salePlant, saleTrans, discount, redeemAmount, redeemDate, redeemPlant, redeemTrans }` | `OdooUpdateVCResponse` | Gọi SAP Odoo SOAP `SI_VC_UPDATE_OUT` |
| 6 | POST | `api/plg/Sale` | ✅ Basic | Body: `PLSaleCXRequestModel { cardNumber, storeNo, posID, spendPoints, awardAmount, orderNo, orderAmount }` | `PLPointModel { PointEarn, PointRedeem, CurrentRate }` | Tích điểm Phúc Long qua CrownX |
| 7 | GET | `api/plg/GetInfoMember` | ✅ Basic | Query: `numberCard`, `storeNo`, `posID` | `PLInfoMemberModel` | Tra cứu hội viên Phúc Long qua CrownX API |

**InforVoucherResponse:**
```
{ SeriNo, TypeVoucher, StatusVoucher, DescVoucher, Value }
StatusVoucher codes: A=active, R=used, E=expired, D=cancelled, L=locked, I=invalid
```

**Partner routing (GetInfoCard / RedeemVoucher):** `PLG` | `UrBox` | `GiftBox` | `GotIt` | `Giftee`

**UrBox auth headers:** `app-id: {UrBoxId}`, `app-secret: {UrBoxSecret}`, `Signature: {HMAC}`  
**GiftBox auth:** `Signature` header, `authKey`, `brandCode` in body  
**GotIt:** No special auth header  
**Giftee:** `Authorization: Bearer {GifteeToken}`, `X-Giftee: 1`

**AppSettings:** `UrBoxUrl`, `UrBox_Secret`, `UrBox_Id`, `GiftBoxUrl`, `GiftBox_authKey`, `GiftBox_BrandCode`, `GotItUrl`, `GifteeUrl`, `GifteeToken`, `CXUrl`, `CXUser`, `CXPassword`, `OdooUser`, `OdooPassword`, `OdooEndPoint`

---

### 4.13 SAPController

**RoutePrefix:** `api/sap`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** SAP SOAP WS (`SI_VoucherSerialNo_OUT`), ROP API, Capillary coupon, PLH (Phúc Long) API  
**Processing_Type:** `A`=create, `C`=check, `U`=update  
**Voucher max length:** 20 ký tự

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/sap/CheckVoucherBySerials` | Query: `voucherNumber*`, `posTerminal*`, `IsLog?` | `VoucherStatusResponse` | Kiểm tra voucher qua ROP API; bắt buộc `posTerminal` (lấy `siteNo = posTerminal.Substring(0,4)`) |
| 2 | GET | `api/sap/CheckVoucher` | Query: `voucherNumber*`, `siteNo?`, `posTerminal?`, `companyCode?`, `isEmployee?`, `partner?`, `isCardVoucher?`, `quantity?`, `isVoucher?`, `phoneNumber?`, `IsLog?` | `VoucherStatusResponse` | Logic phức tạp: dynamic WinX voucher → Capillary coupon → PLG proxy → Olala phone → ROP → SAP SOAP |
| 3 | POST | `api/sap/CreateNewVoucher` | Body: `List<CreateVoucherModel>` | `List<VoucherStatusResponse>` | Tạo voucher mới trên SAP/ROP |
| 4 | POST | `api/sap/UpdateVoucher` | Body: `List<VoucherUpdateRequest>` | `List<VoucherStatusResponse>` | Cập nhật trạng thái SAP voucher; valid status: `SOLD`, `RDM`, `EXP` |
| 5 | POST | `api/sap/winlife/redeemCpnVch` | Body: `VoucherUpdateModel` | `List<VoucherStatusResponse>` | Redeem đa năng: ROP + Capillary + SAP + PLG; hỗ trợ `IsRetry` flag |
| 6 | GET | `api/sap/CheckReturnVoucher` | Query: `voucherNumber*`, `siteNo?`, `posTerminal?`, `IsLog?` | `VoucherStatusResponse` | Kiểm tra BNMH (biên nhận mua hàng); `Voucher_Type="R"` |
| 7 | POST | `api/sap/UpdateReturnVoucher` | Body: `List<VoucherUpdateRequest>` | `List<VoucherStatusResponse>` | Cập nhật trạng thái BNMH |

**VoucherStatusResponse:**
```
{ Status, Return, ActicleNo, ActicleType, Value, VoucherNumber, Voucher_Currency,
  Validity_From_Date, Expiry_Date, CompanyCode, Partner, IsEmployee }
Return: "0"=success, "E"=error, "E4"=not found
```

**CreateVoucherModel:**
```
{ VoucherNumber*, From_Date*, Expiry_Date*, SiteCode*, Article_No*, BonusBuy*, Value* (>0), POSTerminal }
```

**VoucherUpdateRequest:**
```
{ VoucherNumber*, Status* (SOLD|RDM|EXP), ArticleNo*, ArticleType*, SiteCode, POSTerminal, CompanyCode, OrderNo }
```

**VoucherUpdateModel (redeemCpnVch):**
```
{ SiteCode, POSTerminal, IsVoucher, OrderNo, TotalBill, PhoneNumber?, IsRetry,
  ListSeriNo[]: { voucherNumber, articleNo, articleType, status, value, Partner, CompanyCode, isVoucher } }
```

**CheckVoucher routing logic:**
1. `ValidateHelper.IsDynamicVoucher(voucherNumber)` → WinX resolution
2. `ValidateHelper.IsVoucherCapillary(voucherNumber, partner)` → Capillary coupon
3. `companyCode=="PLG"` → PLH proxy HTTP call
4. `StringHelper.ValidatePhoneNumber(voucherNumber)` → Olala phone voucher
5. `IsCheckRequestROP(voucherNumber, prifixVoucherROP)` → ROP API
6. Default → SAP SOAP WS

**AppSettings:** `PLG_ChecVoucher`, `PLG_CheckCoupon`, `PLG_RedeemVoucher`, `PLG_RedeemCoupon`, `PLGUser`, `PLGPassword`, `linkAPIPLH`, `SAPUser`, `SAPPassword`

---

### 4.14 QueueController

**RoutePrefix:** `api/v2`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** Test endpoint `rabbit/test` sẽ ghi và đọc lại từ RabbitMQ cluster

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/sms/send` | Body: `SMSMessage { Content, MessageType, Subject }` | null | Gửi SMS qua `KibanaService.SendMessageSMS` |
| 2 | POST | `api/v2/rabbit/producer` | Body: `RabbitMessageDto` | null | Đẩy message vào `Queue_UpdateStatusVoucher` |
| 3 | POST | `api/v2/rabbit/test` | Body: `RabbitMessageDto` | `List<message>` | Test: produce vào `queue_test`, `await Task.Delay(1000)`, consume lại tối đa 3 message |

---

### 4.15 SettingController

**RoutePrefix:** `api/v2`  
**Base:** `BaseController`  
**Auth:** Không  
**Mục đích:** Admin/ops endpoints quản lý cache và Redis

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/v2/setting/test/redis` | — | `string` | Đọc `IsOfflineCapillary` key từ Redis Sentinel |
| 2 | GET | `api/v2/setting/cache/in-memory-all` | — | `Dict<string, object>` | Dump toàn bộ in-memory cache |
| 3 | POST | `api/v2/setting/cache/in-memory` | — | bool | Xóa toàn bộ in-memory cache (`DeleteMemoryCache("")`) |
| 4 | DELETE | `api/v2/setting/cache/in-memory-delete/{key}` | Path: `key` | bool | Xóa 1 key khỏi in-memory cache |
| 5 | DELETE | `api/v2/cache/redis/{pos}/delete/{key}` | Path: `pos`, `key` (Base64 encoded) | null | Xóa Redis key; `key` được `Base64Decode` trước khi delete |
| 6 | POST | `api/v2/cache/redis/key/create` | Body: `CreateKeyRedisDto { Key, Value, Prefix, ExpTime (seconds), AppCode }` | null | Tạo Redis key với TTL |

---

### 4.16 SyncDataPosController

**RoutePrefix:** `api/posblue`  
**Base:** `BaseController`  
**Auth:** Không  
**Mục đích:** Đồng bộ file data giữa API server và POS máy tính; upload/download sale data  
**Đặc biệt:**
- File server root: `FTPBLUEPOS/`
- Redis queue limit cho SOD file write: key `Redis_Key_GetFileFromFTP`, max = `WWW_LIMIT_REQUEST_MD`
- Fire-and-forget `Task.Run(() => _dataRawService.ProcessFileToStagingDB(...))` sau khi upload

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | GET | `api/posblue/WriteFileByManual` | Query: `storeNo*`, `posTerminal*` | `string` | Trigger tạo file SOD thủ công |
| 2 | GET | `api/posblue/GetFileFromFTP` | Query: `siteCode*`, `posTerminal*`, `folderFile`, `pathSync`, `syncAPI`, `typeSync` | `List<PathFileAPIModel>` | Lấy file từ FTP/shared folder; `typeSync=ALL` kích hoạt write SOD + Redis queue check |
| 3 | POST | `api/posblue/UploadFileLogJob` | Multipart form: files | `string` | Upload log files lên server; optionally sync lên FTP |
| 4 | GET | `api/posblue/process/sales/retry` | — | `string` | Retry process các file sale trong thư mục Kafka |
| 5 | POST | `api/posblue/UploadFileSale` | Multipart form: files | `string` | Upload file bán hàng; async send vào Kafka (`ProcessFileToStagingDB`) |
| 6 | POST | `api/posblue/DeleteFileFromAPI` | Body: `DeleteFileModel { UrlServer, FileName }` | `string` | Xóa file trên API server |
| 7 | GET | `api/posblue/GetFileScriptFromFTP` | Query: `siteCode`, `folderFile`, `pathSync` | `List<PathFileAPIModel>` | Lấy script DB (body rỗng — implementation commented out) |
| 8 | GET | `api/posblue/GetFileUpgradeToolFromFTP` | Query: `pathSync` | `List<PathFileAPIModel>` | Download tool upgrade POS từ shared folder |
| 9 | GET | `api/posblue/DeleteFileFromRemote` | Query: `pathDisk`, `filePath`, `ipServer` | `string` | Xóa file trên server remote (xác định theo IP) |
| 10 | GET | `api/posblue/DeleteFileFromFTP` | Query: `filePath` | `string` | Xóa file theo absolute path |
| 11 | POST | `api/posblue/DeleteFileExist` | Body: `List<PathFileAPIModel>` | null | Xóa danh sách file; tự xác định local hay network share theo IP |
| 12 | GET | `api/posblue/DowloadFileStream` | Query: `fileName`, `pathDisk`, `filePath` | `application/x-zip-compressed` binary | Download file ZIP; `Content-Disposition: attachment` |
| 13 | GET | `api/posblue/ListFile` | Query: `fullPath`, `extension` | `List<{ FileName }>` | Liệt kê file trong thư mục theo extension |
| 14 | GET | `api/posblue/RetryProcessDataRaw` | — | object | Retry insert DataRaw vào DB |

**PathFileAPIModel:**
```
{ FileName, FilePath, IPServer, PathFileIPServer, NetworkPathDisc, FolderAPI }
```

**ConnectionStrings used:** `DAPPER_STAGINGDB`, `BootstrapServers` (Kafka)  
**AppSettings:** `remoteSvrUser`, `remoteSvrPass`, `UploadFileFTP`, `FolderShare`, `FolderShareUpdSource`, `FolderShareAPIBluePOS`

---

### 4.17 ValidateController

**RoutePrefix:** `api/v2`  
**Base:** `BaseController`  
**Auth:** Không  
**Đặc biệt:** `invoice/create` fire-and-forget gọi PARTNER webApi (`CallWebApiPartnerAsync`)

| # | Method | Route | Request | Response Data | Notes |
|---|--------|-------|---------|---------------|-------|
| 1 | POST | `api/v2/telegram/send` | Body: `MessageTelegramRequest { Message, MessageType }` | null | Gửi Telegram notification qua `HttpClientService.SendMessageSMS` |
| 2 | GET | `api/v2/validate/tax-code` | Query: `taxCode` | `TaxCustInfo` | Xác thực MST qua PARTNER webApi (VietQR); meta.Code==200 → 200, meta.Code==9998 → 409 |
| 3 | GET | `api/v2/validate/transaction` | Query: `orderNo*` (15 ký tự) | transaction detail | Validate phiếu tính tiền; format check + `ValidateHelper.IsValidOrderNo` |
| 4 | POST | `api/v2/invoice/create` | Body: `InvoiceCreatedRequest { OrderNo[], SiteNo, CompanyName, CustomerName, TaxCode, Address, PhoneNumber, Email, Passport, CCCD, DVQHNS }` | null | Tạo hóa đơn; fire-and-forget gọi PARTNER API `UpdateTaxInfo` |
| 5 | POST | `api/v2/validate/member-business` | Body: `ValidateMemberBusiness { MemberCard, StoreNo, PosNo, Key }` | object | Xác thực hội viên doanh nghiệp; Key decode Base64 phải contain `"{StoreNo}_{PosNo}_{MemberCard}"` |

**validate/member-business Key validation:**
```csharp
EncryptionHelper.Base64Decode(Key).Contains(
    string.Join("_", StoreNo, PosNo, MemberCard)
)
```

---

### 4.18 HomeController

**Type:** MVC Controller (`System.Web.Mvc.Controller`) — **KHÔNG phải WebAPI**  
**Route:** `/` (default MVC route)  
**Auth:** Không

| # | Method | Route | Response | Notes |
|---|--------|-------|----------|-------|
| 1 | GET | `/` | HTML View | Health check page: `{ IPServer, DbConnection: true, Version }` |

---

### 4.19 BaseController *(Base class)*

Không có route — là abstract base class cho hầu hết controllers.

**Helper methods:**
```
NewExceptionModels() → 409 Conflict với ModelState error
ExceptionModels()    → 400 BadRequest với ModelState error
GetIpServer()        → Server IP (InterNetwork interface)
GetIpAddressClient() → Server IP + "@" + X-FORWARDED-FOR header
GetPosNo()           → alias GetIpServer()
```

---

## 5. AppSettings Keys — Tổng hợp

| Key | Controller(s) | Mô tả |
|-----|--------------|-------|
| `CXUrl` | Voucher, WinLife, PLG | CrownX base URL |
| `CXUser` | Voucher, WinLife, PLG | CrownX username |
| `CXPassword` | Voucher, WinLife, PLG | CrownX password |
| `PromotionStaffUrl` | Offer | URL promotion staff service |
| `PromotionStaffUser` | Offer | Username header |
| `PromotionStaffPass` | Offer | Password header |
| `vc_topup_link` | VinID | VinID base URL |
| `vc_topup_keycode` | VinID | Key code HMAC |
| `topup_get_user_by_phone` | VinID | Endpoint path |
| `topup_point_phone` | VinID | Endpoint path |
| `topup_status_order` | VinID | Endpoint path |
| `evoucher_verify` | VinID | Endpoint path |
| `evoucher_mark_used` | VinID | Endpoint path |
| `evoucher_revoke` | VinID | Endpoint path |
| `evoucher_refund` | VinID | Endpoint path |
| `TOPUP_ITEM_VINID` | VinID | Item code VinID |
| `UrBoxUrl` | PLG | UrBox base URL |
| `UrBox_Secret` | PLG | UrBox secret key |
| `UrBox_Id` | PLG | UrBox app ID |
| `GiftBoxUrl` | PLG | GiftBox base URL |
| `GiftBox_authKey` | PLG | GiftBox auth key |
| `GiftBox_BrandCode` | PLG | GiftBox brand code |
| `GotItUrl` | PLG | GotIt base URL |
| `GifteeUrl` | PLG | Giftee base URL |
| `GifteeToken` | PLG | Giftee Bearer token |
| `OdooUser` | PLG | SAP Odoo username |
| `OdooPassword` | PLG | SAP Odoo password |
| `OdooEndPoint` | PLG | SAP Odoo SOAP endpoint |
| `PLG_ChecVoucher` | SAP | PLH check voucher path |
| `PLG_CheckCoupon` | SAP | PLH check coupon path |
| `PLG_RedeemVoucher` | SAP | PLH redeem voucher path |
| `PLG_RedeemCoupon` | SAP | PLH redeem coupon path |
| `PLGUser` | SAP | PLH API username |
| `PLGPassword` | SAP | PLH API password |
| `linkAPIPLH` | SAP | PLH API base URL |
| `SAPUser` | SAP | SAP SOAP username |
| `SAPPassword` | SAP | SAP SOAP password |
| `remoteSvrUser` | SyncDataPos | File server username |
| `remoteSvrPass` | SyncDataPos | File server password |
| `UploadFileFTP` | SyncDataPos | `YES`/`NO` — có upload FTP không |
| `FolderShare` | SyncDataPos | Root shared folder |
| `FolderShareUpdSource` | SyncDataPos | Source upgrade folder |
| `FolderShareAPIBluePOS` | SyncDataPos | API share folder |

---

## 6. ConnectionString Keys

| Key | Dùng tại |
|-----|----------|
| `RedisConnectionDefault` | RedisManager (write) |
| `RedisConnectionSecond` | RedisManager (read replica) |
| `DAPPER_STAGINGDB` | SyncDataPosController |
| `BootstrapServers` | SyncDataPosController (Kafka) |
| *(+ 12 keys khác trong DapperService)* | CommonBLO, SAPBLO, các repository |

---

## 7. External Dependencies — Tổng hợp

| System | Protocol | Auth | Controllers | Notes |
|--------|----------|------|------------|-------|
| **CrownX (CX)** | REST/JSON | Basic Auth | Voucher, WinLife, PLG | OTP, member info, tích điểm Phúc Long |
| **VinID** | REST/JSON | HMAC SHA256 | VinIDController | Topup, e-voucher; store mapping qua DB |
| **Capillary** | REST/JSON (via service layer) | Service-managed | Loyalty, Capillary, SAP | Loyalty points, member, coupon; offline fallback |
| **WinX** | REST/JSON (via service layer) | Service-managed | SAP, Loyalty, Capillary | Dynamic voucher resolution |
| **SAP (SOAP)** | SOAP/XML via WSDL | NetworkCredential | SAP | `SI_VoucherSerialNo_OUT`, `VoucherSerialNo` WS |
| **SAP Odoo (SOAP)** | SOAP/XML via WSDL | NetworkCredential | PLG | `SI_VC_CHECK_OUT`, `SI_VC_UPDATE_OUT` |
| **PLH API (Phúc Long)** | REST/JSON | Basic Auth | SAP | Proxy check/redeem voucher/coupon Phúc Long |
| **UrBox** | REST/JSON | `app-id` + `app-secret` + `Signature` headers | PLG | Check/redeem voucher |
| **GiftBox** | REST/JSON | `Signature` header + body `authKey` | PLG | Check/redeem voucher |
| **GotIt** | REST/JSON | No special auth | PLG | Check/redeem voucher |
| **Giftee** | REST/JSON | Bearer Token | PLG | Check voucher (GET) |
| **PromotionStaff API** | REST/JSON | Custom headers (`UserName`, `Password`) | Offer | Legacy promotion; timeout 2 phút |
| **RabbitMQ** | AMQP | Cluster connection | Queue, OpsMonitoring | SMS, voucher status queue, ops logging |
| **Redis** | StackExchange | Sentinel/Direct | Nhiều controllers | Cache, idempotency dedup, offline switch |
| **Kafka** | Confluent.Kafka | Bootstrap servers | SyncDataPos | Upload sale data pipeline |
| **AkaChain** | REST (via service) | Service-managed | Loyalty | GetMemberProfile, InputDataAsync |

---

## 8. Middleware

| Middleware | Mô tả |
|-----------|-------|
| `RateLimitMiddleware` | 100 requests/minute/IP → 429 TooManyRequests |
| `[ValidateModel]` | Action filter — validate ModelState trước action (dùng ở GiftController, SyncDataPosController) |

---

## 9. Redis Keys quan trọng

| Key Pattern | Dùng tại | TTL |
|------------|---------|-----|
| `RedisConst.GetRedisKeyLoyaltyMemberPoints(orderNo)` | Capillary dedup | Midnight |
| `AppCodeEnum.IsOfflineCapillary.ToString()` | Loyalty offline switch | Persistent |
| `RedisConst.Redis_Key_GetFileFromFTP` | SOD file request queue | 10 phút |
| `RedisConst.Redis_Key_WWW_ROP_V_PREFIX` | SAP — prefix voucher ROP | Cached |

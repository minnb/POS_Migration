# Báo cáo Phân tích Code Cũ — POS Backend (VCM.POSBLUE.API)

> Tạo bởi: `/analyze-legacy` — 2026-06-03
> Nguồn: `POS.Backend/API_BLUEPOS/VCM.POSBLUE.API/`

---

## 1. Tổng quan hệ thống

| Hạng mục | Giá trị |
|---|---|
| Framework | .NET Framework 4.6 — Web API 2 (`ApiController`) |
| Route Prefix | Đa dạng, mỗi controller có `[RoutePrefix]` riêng |
| Auth | BasicAuth (`AuthenAPI.AuthorizationBasic`) — một số controller tự check |
| Response chung | `ResultResponse { Status, Message, Data, MessageTechnical }` |
| Logging | Serilog + KibanaService (log tới Elasticsearch) |
| Caching | IMemoryCache + Redis Sentinel (`RedisManager`) |
| Queue | RabbitMQ (`RabbitMQProducer`) |
| DB | SQL Server qua Dapper (legacy `DapperLoyaltyFactory`, `DataBaseContext`) |
| External | SAP (SOAP/WCF), CrownX (REST), Capillary (REST), OneFlexiAxis (WCF) |

---

## 2. Danh sách Controllers và Endpoints

### 2.1 CommonController — `api/common`
> Nghiệp vụ: Các API tiện ích chung của POS (ngày kinh doanh, thông tin đơn hàng, sync data, kios)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/common/TransactionIssue` | Kiểm tra số lượng giao dịch theo article | Đơn giản |
| GET | `api/common/GetCurrentTime` | Lấy thời gian hiện tại của server | Đơn giản |
| GET | `api/common/GetBusinessDate` | Lấy ngày kinh doanh của cửa hàng; tạo record nếu chưa có | Trung bình |
| GET | `api/common/CheckEndShift` | Kiểm tra trạng thái đóng ca (SHIFT_HEADER) | Đơn giản |
| POST | `api/common/POSMonitor` | Insert bản ghi monitor POS (fire-and-forget) | Đơn giản |
| GET | `api/common/CheckIPaddressPos` | Tra cứu POS Terminal theo IP | Đơn giản |
| GET | `api/common/POSDataSetup` | Lấy cấu hình setup POS | Đơn giản |
| GET | `api/common/GetPOSVersion` | Lấy danh sách phiên bản POS | Đơn giản |
| GET | `api/common/GetOrderInfo` | Lấy thông tin đơn hàng để trả hàng (kiểm tra storeNo, đơn trả) | Trung bình |
| GET | `api/common/WriteFileByManual` | Ghi file dữ liệu thủ công cho POS | Trung bình |
| GET | `api/common/GetListPOSDocumentNo` | Lấy danh sách số chứng từ POS | Đơn giản |
| GET | `api/common/CheckCouponLine` | Kiểm tra coupon theo itemNo/barCode | Đơn giản |
| POST | `api/common/UpdateOrderTrans` | Cập nhật thông tin đơn hàng trả hàng | Trung bình |
| GET | `api/common/GetInsurance` | Lấy thông tin bảo hiểm theo receipt | Đơn giản |
| PUT | `api/common/UpdateEOD` | Cập nhật End-of-Day cho POS | Đơn giản |
| GET | `api/common/CheckTotalBill` | Kiểm tra tổng số hóa đơn của POS terminal | Đơn giản |
| POST | `api/common/kios/insert-sale` | Insert dữ liệu bán hàng từ kios | Trung bình |
| GET | `api/common/kios/check-order` | Kiểm tra đơn hàng kios | Đơn giản |
| GET | `api/common/SendCodeReward` | Gửi mã reward về POS (tích hợp PLG) | Trung bình |
| POST | `api/common/logging` | Log dữ liệu tới Elasticsearch (Kibana) | Đơn giản |

### 2.2 LoyaltyController — `api`
> Nghiệp vụ: Tích hợp chương trình thành viên WIN (VinID legacy + Capillary mới)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/v2/loyalty/retry` | Retry các giao dịch loyalty thất bại | Phức tạp |
| GET | `api/v2/loyalty/customer/get` | Lấy thông tin hội viên (phone/card), hỗ trợ VINID + Capillary | Phức tạp |
| POST | `api/v2/loyalty/customer` | Tạo/đăng ký hội viên mới | Phức tạp |
| POST | `api/v2/loyalty/customer/update` | Cập nhật thông tin hội viên | Trung bình |
| POST | `api/v2/loyalty/transaction/add` | Tích điểm giao dịch bán hàng | Phức tạp |
| POST | `api/v2/loyalty/transaction/refund` | Trừ điểm giao dịch trả hàng | Phức tạp |
| POST | `api/v2/loyalty/other-status` | Cập nhật trạng thái khác (winscore...) | Trung bình |
| GET | `api/vinid/GetInfoMember` | Lấy thông tin hội viên VINID (legacy) | Trung bình |
| GET | `api/vinid/Sales` | Giao dịch bán hàng VINID (legacy GET) | Phức tạp |
| POST | `api/vinid/SalesV2` | Giao dịch bán hàng VINID v2 (POST) | Phức tạp |
| GET | `api/vinid/Refund` | Trả hàng VINID (legacy GET) | Phức tạp |
| POST | `api/vinid/RefundV2` | Trả hàng VINID v2 (POST) | Phức tạp |
| POST | `api/vinid/InitTransaction` | Khởi tạo giao dịch ví VINID | Phức tạp |
| GET | `api/vinid/GetTransaction` | Lấy thông tin giao dịch VINID | Trung bình |
| PUT | `api/vinid/CancelTransaction` | Hủy giao dịch VINID | Trung bình |
| GET | `api/vinid/ScanAndGo` | Giao dịch Scan & Go VINID | Phức tạp |
| PUT | `api/vinid/ScanAndGo/UpdateStatusOrder` | Cập nhật trạng thái đơn Scan & Go | Trung bình |
| POST | `api/vinid/ExtraSales` | Giao dịch bán hàng extra VINID | Trung bình |
| POST | `api/vinid/ExtraRefund` | Trả hàng extra VINID | Trung bình |
| POST | `api/vinid/ScanAndGo/SnGRefund` | Trả hàng Scan & Go | Phức tạp |
| POST | `api/vinid/ScanAndGo/SnGTopup` | Topup Scan & Go | Trung bình |
| GET | `api/vinid/TransactionEnquiry` | Tra cứu giao dịch VINID | Trung bình |

### 2.3 PaymentController — `api/v2/partner`
> Nghiệp vụ: Thanh toán voucher/coupon từ đối tác (Urbox, GotIt, OneU, Capillary)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/partner/voucher/check` | Kiểm tra voucher đối tác (Urbox/GotIt/OneU) | Trung bình |
| POST | `api/v2/partner/voucher/update-status` | Thanh toán/cập nhật trạng thái voucher | Trung bình |
| POST | `api/v2/partner/coupon/check` | Kiểm tra coupon Capillary (CAP) | Phức tạp |
| POST | `api/v2/partner/coupon/redeem` | Sử dụng coupon Capillary | Trung bình |
| GET | `api/v2/partner/coupon/list/user` | Lấy danh sách coupon của user | Trung bình |
| GET | `api/v2/partner/coupon/detail` | Lấy chi tiết coupon | Trung bình |
| POST | `api/v2/partner/coupon/re-active` | Kích hoạt lại coupon | Trung bình |

### 2.4 VoucherController — `api/vc`
> Nghiệp vụ: Tích hợp voucher CrownX (CX)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/vc/generateOTP` | Tạo OTP để lấy voucher CX | Đơn giản |
| GET | `api/vc/getVoucherSerial` | Lấy serial voucher CX sau khi có OTP | Đơn giản |
| POST | `api/vc/UpdateVoucherStatus` | Đánh dấu đã dùng voucher CX (mark USED) | Đơn giản |

### 2.5 GiftController — không có RoutePrefix
> Nghiệp vụ: Tặng quà (MML Scheme, WinX QR)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/gifts/claim` | Claim quà tặng (MML + WinX QRCode song song) | Phức tạp |
| POST | `api/pos/gift` | Check/Use gift code nội bộ (CREATE/USED flow) | Trung bình |

### 2.6 OfferController — `api`
> Nghiệp vụ: Ưu đãi nhân viên (staff quota) + topup điểm

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/offer/staff/points/topup/retry` | Retry topup điểm nhân viên qua RabbitMQ | Đơn giản |
| GET | `api/v2/offer/staff/points/topup` | Xử lý topup điểm từ RabbitMQ queue | Trung bình |
| GET | `api/v2/offer/staff/check` | Kiểm tra ưu đãi nhân viên | Đơn giản |
| POST | `api/v2/offer/staff/apply` | Áp dụng ưu đãi nhân viên | Trung bình |
| GET | `api/promotion/staff/force-check` | Force check quota nhân viên (gọi API partner) | Trung bình |
| GET | `api/promotion/staff/check` | Kiểm tra quota nhân viên | Trung bình |
| POST | `api/promotion/staff/redeem` | Redeem quota nhân viên | Trung bình |
| POST | `api/promotion/staff/refund` | Hoàn trả quota nhân viên | Trung bình |

### 2.7 CapillaryController — `api`
> Nghiệp vụ: Tích hợp Capillary CRM (điểm thành viên, coupon, offline switch)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/loyalty/transaction/points/refund` | Refund điểm Capillary | Trung bình |
| POST | `api/v2/loyalty/point/topup/revert` | Revert topup điểm | Trung bình |
| POST | `api/v2/loyalty/point/topup` | Topup điểm Capillary | Trung bình |
| POST | `api/v2/loyalty/transaction/points` | Giao dịch tích điểm hội viên | Phức tạp |
| POST | `api/v2/loyalty/transaction/program-points` | Tích điểm program đặc biệt | Trung bình |
| POST | `api/v2/loyalty/transaction/member-business` | Giao dịch member business | Trung bình |
| POST | `api/v2/loyalty/winscore/update` | Cập nhật WinScore | Đơn giản |
| GET | `api/v2/loyalty/points/history` | Lấy lịch sử điểm | Đơn giản |
| POST | `api/v2/loyalty/capillary/mobile-enroll` | Đăng ký mobile hội viên qua Capillary | Trung bình |
| GET | `api/v2/loyalty/capillary/transactions` | Tra cứu giao dịch Capillary | Trung bình |
| GET | `api/v2/loyalty/capillary/check` | Kiểm tra offline status Capillary | Đơn giản |
| GET | `api/v2/loyalty/capillary/check/switch/winx` | Kiểm tra switch Capillary/WinX | Đơn giản |
| POST | `api/v2/loyalty/capillary/action` | Switch online/offline Capillary | Trung bình |
| GET | `api/v2/loyalty/capillary/customer/redemptions` | Lấy thông tin redemptions Capillary | Trung bình |
| POST | `api/v2/loyalty/capillary/points/redeem/revert` | Revert điểm redeem Capillary | Trung bình |

### 2.8 QueueController — `api/v2`
> Nghiệp vụ: Quản lý SMS/RabbitMQ

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/sms/send` | Gửi SMS qua Kibana/Telegram | Đơn giản |
| POST | `api/v2/rabbit/producer` | Đưa message vào RabbitMQ | Đơn giản |
| POST | `api/v2/rabbit/test` | Test RabbitMQ produce + consume | Đơn giản |

### 2.9 PLGController — `api/plg`
> Nghiệp vụ: Tích hợp voucher đối tác (PLG/Phúc Long, UrBox, GiftBox, GotIt, Giftee, SAP Odoo)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/plg/GetInfoCard` | Kiểm tra thông tin voucher (PLG/UrBox/GiftBox/GotIt/Giftee) | Phức tạp |
| PUT | `api/plg/SaleVoucher` | Bán voucher PLG | Trung bình |
| POST | `api/plg/RedeemVoucher` | Sử dụng voucher (PLG/UrBox/GiftBox/GotIt) | Phức tạp |
| POST | `api/plg/Check_VC_Odoo` | Kiểm tra voucher qua SAP Odoo (WCF) | Phức tạp |
| POST | `api/plg/OdooSAP_Update_Voucher` | Cập nhật trạng thái voucher SAP Odoo | Phức tạp |
| POST | `api/plg/Sale` | Bán voucher (thêm) | Trung bình |
| GET | `api/plg/GetInfoMember` | Lấy thông tin thành viên từ PLG | Trung bình |

### 2.10 SAPController — `api/sap`
> Nghiệp vụ: Tích hợp SAP (kiểm tra/tạo/cập nhật voucher SAP + ROP), voucher WinLife/Capillary

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/sap/CheckVoucherBySerials` | Kiểm tra voucher theo serial qua ROP | Trung bình |
| GET | `api/sap/CheckVoucher` | Kiểm tra voucher/coupon (SAP/ROP/Capillary/PLG) | Phức tạp |
| POST | `api/sap/CreateNewVoucher` | Tạo voucher mới (SAP/ROP) | Trung bình |
| POST | `api/sap/UpdateVoucher` | Cập nhật trạng thái voucher (SAP/ROP) | Trung bình |
| POST | `api/sap/winlife/redeemCpnVch` | Thanh toán voucher/coupon tổng hợp (ROP/Capillary/SAP/PLG) | **Rất phức tạp** |
| GET | `api/sap/CheckReturnVoucher` | Kiểm tra voucher trả hàng | Trung bình |
| POST | `api/sap/UpdateReturnVoucher` | Cập nhật trạng thái voucher trả hàng | Trung bình |
| POST | `api/sap/CreateReturnVoucher` | Tạo voucher cho đơn trả hàng | Trung bình |
| GET | `api/sap/EVoucher` | Lấy thông tin eVoucher | Trung bình |
| GET | `api/sap/SendCodeCpnVch` | Gửi mã voucher/coupon | Trung bình |
| GET | `api/sap/retry` | Retry giao dịch voucher thất bại | Trung bình |

### 2.11 SettingController — `api/v2`
> Nghiệp vụ: Quản lý cache và setting hệ thống

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/v2/setting/test/redis` | Test kết nối Redis Sentinel | Đơn giản |
| GET | `api/v2/setting/cache/in-memory-all` | Lấy toàn bộ in-memory cache | Đơn giản |
| POST | `api/v2/setting/cache/in-memory` | Xóa toàn bộ in-memory cache | Đơn giản |
| DELETE | `api/v2/setting/cache/in-memory-delete/{key}` | Xóa cache theo key | Đơn giản |
| DELETE | `api/v2/cache/redis/{pos}/delete/{key}` | Xóa Redis cache theo key | Đơn giản |
| POST | `api/v2/cache/redis/key/create` | Tạo key mới trong Redis | Đơn giản |

### 2.12 SyncDataPosController — `api/posblue`
> Nghiệp vụ: Sync file dữ liệu giữa server API và POS terminal (FTP/shared folder)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/posblue/WriteFileByManual` | Ghi file data thủ công | Trung bình |
| GET | `api/posblue/GetFileFromFTP` | Lấy file từ FTP/shared folder (SOD) | **Rất phức tạp** |
| POST | `api/posblue/UploadFileLogJob` | Upload file log job | Trung bình |
| GET | `api/posblue/process/sales/retry` | Retry xử lý file sale từ Kafka | Trung bình |
| POST | `api/posblue/UploadFileSale` | Upload file sale (đẩy vào Kafka) | Trung bình |
| POST | `api/posblue/DeleteFileFromAPI` | Xóa file từ API server | Đơn giản |
| GET | `api/posblue/GetFileScriptFromFTP` | Lấy file script DB từ FTP | Đơn giản |
| GET | `api/posblue/GetFileUpgradeToolFromFTP` | Lấy file upgrade tool từ FTP | Đơn giản |
| GET | `api/posblue/DeleteFileFromRemote` | Xóa file từ remote server | Đơn giản |
| GET | `api/posblue/DeleteFileFromFTP` | Xóa file từ FTP | Đơn giản |
| POST | `api/posblue/DeleteFileExist` | Xóa nhiều file tồn tại | Đơn giản |
| GET | `api/posblue/DowloadFileStream` | Download file dạng stream | Đơn giản |
| GET | `api/posblue/ListFile` | Liệt kê file trong thư mục | Đơn giản |
| GET | `api/posblue/RetryProcessDataRaw` | Retry xử lý data raw | Đơn giản |

### 2.13 ValidateController — `api/v2`
> Nghiệp vụ: Validate nghiệp vụ, hóa đơn điện tử, Telegram

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/telegram/send` | Gửi message Telegram | Đơn giản |
| GET | `api/v2/validate/tax-code` | Tra cứu mã số thuế | Đơn giản |
| GET | `api/v2/validate/transaction` | Kiểm tra hợp lệ giao dịch (orderNo) | Đơn giản |
| POST | `api/v2/invoice/create` | Tạo yêu cầu xuất hóa đơn điện tử | Trung bình |
| POST | `api/v2/validate/member-business` | Validate thành viên doanh nghiệp khi thanh toán | Trung bình |

### 2.14 WinCareController — `api`
> Nghiệp vụ: WinPay tích lũy, WinCare thu nợ, ROP voucher

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/wincustomer/wpay/shopping-accumulate` | Tích lũy WinPay khi mua hàng (v2) | Trung bình |
| POST | `api/v3/wincustomer/wpay/shopping-accumulate` | Tích lũy WinPay khi mua hàng (v3) | Trung bình |
| POST | `api/v2/wincare/OrderSupplierPublic/GenOspQRLogin` | Tạo QR Login cho đơn nhà cung cấp | Trung bình |
| POST | `api/v2/wincare/collect-money/barcode` | Lấy thông tin barcode thu tiền WinCare | Đơn giản |
| POST | `api/v2/wincare/collect-money/confirm` | Xác nhận thu tiền WinCare | Đơn giản |
| POST | `api/v2/wincare/notify` | Gửi thông báo WinCare | Đơn giản |

### 2.15 WinLifeController — `api`
> Nghiệp vụ: OTP (CrownX/WinPay), đăng ký hội viên WinLife, update promotions, SmartPOS

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/v2/otp/generate` | Tạo OTP (CX hoặc WinPay tùy action) | Trung bình |
| POST | `api/v2/otp/verify` | Xác thực OTP | Đơn giản |
| GET | `api/blue/winlife/generateOTP` | Tạo OTP WinLife qua CrownX (legacy) | Đơn giản |
| POST | `api/blue/winlife/register` | Đăng ký hội viên WinLife (CX/Capillary) | Phức tạp |
| POST | `api/blue/winlife/update-promotions` | Ghi nhận CTKM đặc biệt (WinCode) | Trung bình |
| GET | `api/blue/winlife/winCode-histories` | Lịch sử WinCode | Đơn giản |
| GET | `api/blue/winlife/smart-pos/customer-by-last-digits-phone` | Tìm KH qua số ĐT (SmartPOS) | Trung bình |
| POST | `api/blue/winlife/smart-pos/update-customer-info` | Cập nhật thông tin KH (SmartPOS) | Trung bình |

### 2.16 WinpayController — `api/v2/winpay`
> Nghiệp vụ: Thanh toán ví WinPay (đăng ký, thanh toán, nạp tiền, rút tiền, vân tay)

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| POST | `api/v2/winpay/register` | Đăng ký WinPay | Trung bình |
| GET | `api/v2/winpay/get-register-info` | Lấy thông tin đăng ký WinPay | Đơn giản |
| POST | `api/v2/winpay/unregister` | Hủy đăng ký WinPay | Trung bình |
| POST | `api/v2/winpay/payment` | Thanh toán WinPay | Trung bình |
| POST | `api/v2/winpay/refund` | Hoàn tiền WinPay | Trung bình |
| POST | `api/v2/winpay/deposit` | Nạp tiền WinPay (cashin) | Trung bình |
| POST | `api/v2/winpay/withdraw` | Rút tiền WinPay (cashout) | Trung bình |
| POST | `api/v2/winpay/fp-update` | Cập nhật vân tay WinPay | Trung bình |
| POST | `api/v2/winpay/fp-verify` | Xác thực vân tay WinPay | Trung bình |
| POST | `api/v2/winpay/cashback` | Cashback WinPay | Trung bình |

### 2.17 VoucherTopUpVinIDController — `api/vinid`
> Nghiệp vụ: Nạp điểm/eVoucher VinID

| Method | Route | Mô tả | Độ phức tạp |
|---|---|---|---|
| GET | `api/vinid/TopUpCheckMember` | Kiểm tra thông tin KH VinID để nạp điểm | Trung bình |
| POST | `api/vinid/TopUpPoinToPhone` | Nạp điểm VinID cho số điện thoại | Trung bình |
| GET | `api/vinid/TopUpCheckStatusOrder` | Kiểm tra trạng thái đơn nạp điểm | Đơn giản |
| GET | `api/vinid/EVoucherVerify` | Xác thực eVoucher VinID | Trung bình |
| POST | `api/vinid/EVoucherRefund` | Thu hồi eVoucher VinID | Trung bình |
| POST | `api/vinid/EVoucherMarkUsed` | Đánh dấu đã dùng eVoucher VinID | Phức tạp |

---

## 3. Dependencies Bên Ngoài (External Services)

| Service | Giao thức | Authentication | Mục đích |
|---|---|---|---|
| **SAP** | SOAP/WCF (`SI_VoucherSerialNo_OUTService`) | NetworkCredential | Kiểm tra/tạo/cập nhật voucher nội bộ |
| **ROP** (hệ thống nội bộ) | REST HTTP | Basic Auth | Voucher mới thay SAP |
| **Capillary CRM** | REST | Token/API Key | Hội viên, điểm, coupon |
| **CrownX (CX)** | REST | Basic Auth (`CXUser:CXPassword`) | OTP, WinLife, voucher nội bộ |
| **OneFlexiAxis** | SOAP/WCF | — | VINID legacy loyalty |
| **UrBox** | REST | HMAC Signature (`app-id`, `app-secret`) | Voucher đối tác |
| **GotIt** | REST | API Token | Voucher đối tác |
| **GiftBox** | REST | HMAC Signature | Voucher đối tác |
| **Giftee** | REST | Bearer Token | Voucher đối tác |
| **SAP Odoo** | SOAP/WCF (`SI_VC_OUTService`) | NetworkCredential | Voucher PLG/Phúc Long |
| **VinID** | REST | X-Key-Code + HMAC Signature | Nạp điểm/eVoucher |
| **WinPay** | REST | Internal auth | Ví điện tử nhân viên |
| **Promotion Staff API** | REST | Header (UserName/Password) | Ưu đãi nhân viên quota |
| **Kafka** | Kafka producer | BootstrapServers | Stream file sale data |
| **RabbitMQ** | AMQP | Internal | Queue retry/topup điểm |
| **Elasticsearch** (Kibana) | REST | — | Structured logging |
| **Redis Sentinel** | Redis | — | Cache + offline switch |
| **SMS/Telegram** | REST | — | Thông báo nghiệp vụ |

---

## 4. Patterns & Anti-Patterns

### Cần GIỮ LẠI (logic nghiệp vụ)
- Routing prefix và action names — copy y chang
- Cấu trúc `ResultResponse { Status, Message, Data, MessageTechnical }` — không thay đổi
- Logic switch offline Capillary (check `_loyaltyOfflineService.IsOfflineCapillary()`)
- Logic routing voucher: ROP vs SAP vs Capillary dựa trên prefix voucher
- Fire-and-forget pattern (`Task.Run(...)`) cho log

### Cần THAY THẾ khi convert
| Anti-pattern cũ | Thay thế mới |
|---|---|
| `WebConfigurationManager.AppSettings["key"]` | `IConfiguration` / Options Pattern |
| `new HttpClient()` trong controller | `IHttpClientFactory` |
| `new UrboxService()` (tự tạo trong constructor) | Constructor injection DI |
| `.Result`, `.Wait()` (blocking) | `await` async |
| `HttpContext.Current.Cache` | `IMemoryCache` |
| `ConfigurationManager.ConnectionStrings` | `IConfiguration.GetConnectionString()` |
| `Thread.Sleep()` | `await Task.Delay()` |
| `File.ReadAllBytes`, `Directory.GetFiles` trực tiếp | Service layer + IConfiguration |
| Log bằng `Serilog.Log.Logger.Information(...)` static | `ILogger<T>` injected |

### Vấn đề tiềm ẩn
1. **LoyaltyController quá lớn** (275KB, ~22 action methods) — cần split thành nhiều service, nhưng **route phải giữ nguyên**
2. **SAPController.winlife/redeemCpnVch** — logic rất phức tạp: xử lý song song ROP + Capillary + SAP + PLG trong 1 endpoint
3. **PLGController** — chứa nhiều helper methods không phải action, cần tách service class
4. **SyncDataPosController** — phụ thuộc vào `HostingEnvironment.MapPath` (không tồn tại trong .NET Core) → cần dùng `IWebHostEnvironment`
5. **VoucherTopUpVinIDController** — dùng blocking `.Result` rất nhiều → cần async hoàn toàn
6. **BasicAuth tự check** (`AuthenAPI.AuthorizationBasic`) — cần implement thành middleware/filter

---

## 5. Kế hoạch Convert theo Thứ tự Ưu tiên

### Tier 1 — Nền tảng (làm trước)
Không phải feature, nhưng cần có trước khi convert module nào:
1. **Scaffold solution** (`POS.API`, `POS.Application`, `POS.Domain`, `POS.Infrastructure`, `POS.Shared`)
2. **Infrastructure cơ bản**: DbConnectionFactory (Dapper), BasicAuthFilter, Serilog, IMemoryCache, Redis
3. **Domain exceptions** (`PosBusinessException`, `PosNotFoundException`, v.v.)

### Tier 2 — Module độ ưu tiên cao
| # | Module | Endpoint count | Lý do ưu tiên |
|---|---|---|---|
| 1 | **Common** | 20 | POS khởi động cần ngay (GetBusinessDate, CheckIPaddressPos, GetPOSVersion) |
| 2 | **Loyalty (WinLife v2)** | 7 endpoints (`v2/loyalty/*`) | Tích điểm mua hàng là core flow |
| 3 | **SAP Voucher (nội bộ)** | `CheckVoucher`, `UpdateVoucher` | Thanh toán voucher nội bộ |

### Tier 3 — Module trung bình
| # | Module | Ghi chú |
|---|---|---|
| 4 | **Payment (Urbox/GotIt/OneU)** | Phụ thuộc external, cần mock |
| 5 | **Capillary** | Phức tạp nhưng đang dùng rộng rãi |
| 6 | **WinLife/OTP** | Phụ thuộc CrownX API |
| 7 | **Gift (MML/WinX)** | Ít critical hơn |

### Tier 4 — Module thấp hơn
| # | Module |
|---|---|
| 8 | WinPay |
| 9 | WinCare |
| 10 | Setting/Validate |
| 11 | SyncDataPos (phụ thuộc file system nhiều) |
| 12 | PLG (phức tạp, ít POS dùng) |
| 13 | SAP Odoo (WCF phức tạp) |
| 14 | VoucherTopUpVinID (legacy, ít dùng) |
| 15 | Queue (RabbitMQ ops) |

---

## 6. Ước tính File cần tạo (mỗi module)

Module điển hình (ví dụ Common):
```
POS.API/Controllers/CommonController.cs             (1)
POS.Application/Common/Services/ICommonService.cs   (1)
POS.Application/Common/Services/CommonService.cs    (1)
POS.Application/Common/DTOs/GetBusinessDateRequest.cs + Response.cs + ...
POS.Application/Common/Validators/...Validator.cs
POS.Infrastructure/Repositories/ICommonRepository.cs
POS.Infrastructure/Repositories/CommonRepository.cs
```
→ Ước tính ~10-15 files/module trung bình, ~20-30 files cho module phức tạp (Loyalty, SAP).

---

## 7. Ghi chú đặc biệt

- `api/sap/winlife/redeemCpnVch` là endpoint **phức tạp nhất** hệ thống (450+ dòng logic): xử lý voucher ROP + Capillary + SAP + PLG trong 1 call. Cần phân tích kỹ và có unit test khi convert.
- `api/posblue/GetFileFromFTP` sử dụng `HostingEnvironment.MapPath` và file share Windows — cần redesign hoàn toàn với `IWebHostEnvironment.ContentRootPath`.
- `LoyaltyController` (275KB) nên được split thành ít nhất 2-3 service class khi convert, nhưng **toàn bộ route phải giữ nguyên**.
- Auth cơ bản dùng BasicAuth header (`Authorization: Basic base64(user:pass)`) — cần giữ nguyên cơ chế này vì POS client không thay đổi.

---

## 8. Cấu trúc `ResultResponse` (Response Model gốc)

```csharp
// Giữ nguyên 100% cấu trúc này trong API mới
public class ResultResponse
{
    public HttpStatusCode Status { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public string MessageTechnical { get; set; } // nullable — không phải lúc nào cũng có
}
```

**Lưu ý:** Khi `Status` là `HttpStatusCode.OK` thì serialize thành `200`, `BadRequest` thành `400`, v.v. — đây là chuẩn của API cũ, phải giữ nguyên.

# API Inventory & Tracking — POS Backend

> **NGUYÊN TẮC:** Route và Response format của API mới phải giữ nguyên **100%** như API cũ.
> File này dùng để **theo dõi tiến độ convert**, không phải để định nghĩa route mới.

---

## Hướng dẫn đọc bảng

| Ký hiệu | Ý nghĩa |
|---|---|
| ⬜ Chưa làm | Endpoint chưa được convert |
| 🔄 Đang làm | Đang trong quá trình convert |
| ✅ Xong | Đã convert, đã test, route và response khớp API cũ |
| ❌ Bỏ qua | Không convert module này |

> **Cột "Endpoint"** = route của API cũ = route của API mới (không có gì thay đổi).

---

## Module: Common (`api/common`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/common/TransactionIssue` | ✅ | Query param: articleNo, siteCode |
| GET | `api/common/GetCurrentTime` | ✅ | Trả DateTime server |
| GET | `api/common/GetBusinessDate` | ✅ | Query: siteCode, posTerminal; tự tạo record nếu chưa có |
| GET | `api/common/CheckEndShift` | ✅ | Query: siteCode, posTerminal, businessDate |
| POST | `api/common/POSMonitor` | ✅ | Body: POSMonitorInsertRequest; fire-and-forget |
| GET | `api/common/CheckIPaddressPos` | ✅ | Query: IPAddress |
| GET | `api/common/POSDataSetup` | ✅ | Không có param |
| GET | `api/common/GetPOSVersion` | ✅ | Không có param |
| GET | `api/common/GetOrderInfo` | ✅ | Query: orderNo, storeNo, posNo |
| GET | `api/common/WriteFileByManual` | 🔄 | TODO: cần implement file system logic với IWebHostEnvironment |
| GET | `api/common/GetListPOSDocumentNo` | ✅ | Query: siteCode, posTerminal |
| GET | `api/common/CheckCouponLine` | ✅ | Query: itemNo, barCode |
| POST | `api/common/UpdateOrderTrans` | ✅ | Body: UpdateOrderInfoModel |
| GET | `api/common/GetInsurance` | ✅ | Query: receiptNo, posNo, staffCode |
| PUT | `api/common/UpdateEOD` | ✅ | Body: POSEOD_APIModel |
| GET | `api/common/CheckTotalBill` | ✅ | Query: storeNo, posTerminal, bussinessDate, posTotal |
| POST | `api/common/kios/insert-sale` | ✅ | Body: KiosInsertSalePOSRequest |
| GET | `api/common/kios/check-order` | ✅ | Query: storeNo, posNo, orderNo |
| GET | `api/common/SendCodeReward` | 🔄 | TODO: pending PLG module conversion |
| POST | `api/common/logging` | ✅ | Body: LoggingElastic |

---

## Module: Loyalty / Hội viên (`api/v2/loyalty/*` + `api/vinid/*`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/v2/loyalty/retry` | ⬜ | Retry giao dịch thất bại |
| GET | `api/v2/loyalty/customer/get` | ✅ | Query: numberCard, posID, storeNo, clubCode, isMobile |
| POST | `api/v2/loyalty/customer` | ⬜ | Tạo/đăng ký hội viên |
| POST | `api/v2/loyalty/customer/update` | ⬜ | Cập nhật thông tin hội viên |
| POST | `api/v2/loyalty/transaction/add` | ⬜ | Tích điểm bán hàng |
| POST | `api/v2/loyalty/transaction/refund` | ⬜ | Trừ điểm trả hàng |
| POST | `api/v2/loyalty/other-status` | ⬜ | Cập nhật trạng thái khác |
| GET | `api/vinid/GetInfoMember` | ⬜ | Legacy VINID |
| GET | `api/vinid/Sales` | ⬜ | Legacy VINID bán hàng |
| POST | `api/vinid/SalesV2` | ⬜ | VINID bán hàng v2 |
| GET | `api/vinid/Refund` | ⬜ | Legacy VINID trả hàng |
| POST | `api/vinid/RefundV2` | ⬜ | VINID trả hàng v2 |
| POST | `api/vinid/InitTransaction` | ⬜ | Khởi tạo giao dịch ví VINID |
| GET | `api/vinid/GetTransaction` | ⬜ | Lấy thông tin giao dịch VINID |
| PUT | `api/vinid/CancelTransaction` | ⬜ | Hủy giao dịch VINID |
| GET | `api/vinid/ScanAndGo` | ⬜ | Scan & Go VINID |
| PUT | `api/vinid/ScanAndGo/UpdateStatusOrder` | ⬜ | Cập nhật trạng thái Scan & Go |
| POST | `api/vinid/ExtraSales` | ⬜ | Extra sales VINID |
| POST | `api/vinid/ExtraRefund` | ⬜ | Extra refund VINID |
| POST | `api/vinid/ScanAndGo/SnGRefund` | ⬜ | Trả hàng Scan & Go |
| POST | `api/vinid/ScanAndGo/SnGTopup` | ⬜ | Topup Scan & Go |
| GET | `api/vinid/TransactionEnquiry` | ⬜ | Tra cứu giao dịch VINID |

---

## Module: Payment — Voucher Đối tác (`api/v2/partner`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/partner/voucher/check` | ✅ | Body có field Partner: URBOX/GOTIT/ONEU |
| POST | `api/v2/partner/voucher/update-status` | ✅ | Body có field Partner: URBOX/GOTIT/ONEU |
| POST | `api/v2/partner/coupon/check` | ✅ | Body có field Partner: CAP; WinX dynamic voucher (WDV...) tự resolve |
| POST | `api/v2/partner/coupon/redeem` | ✅ | Body có field Partner: CAP |
| GET | `api/v2/partner/coupon/list/user` | ✅ | Query: partner, storeNo, mobile, status |
| GET | `api/v2/partner/coupon/detail` | ✅ | Query: partner, storeNo, serialNo |
| POST | `api/v2/partner/coupon/re-active` | ✅ | Body có field Partner: CAP |

---

## Module: Voucher CrownX (`api/vc`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/vc/generateOTP` | ⬜ | Query: storeNo, posID, codeValue |
| GET | `api/vc/getVoucherSerial` | ⬜ | Query: storeNo, posID, codeValue, otp |
| POST | `api/vc/UpdateVoucherStatus` | ⬜ | Body: UpdateVoucherStatusPosRequest |

---

## Module: Gift / Tặng quà

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/gifts/claim` | ✅ | Body: MMLSchemeRequest; MML + WinX QRCode; 30s timeout |
| POST | `api/pos/gift` | ✅ | Body: GiftDataRequest; Status: CHECK/CREATE/USED |

---

## Module: Offer / Ưu đãi Nhân viên

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/offer/staff/points/topup/retry` | ⬜ | Retry topup điểm nhân viên |
| GET | `api/v2/offer/staff/points/topup` | ⬜ | Query: queueName, number, isRetry |
| GET | `api/v2/offer/staff/check` | ⬜ | Query: storeNo, posNo, phoneNumber |
| POST | `api/v2/offer/staff/apply` | ⬜ | Body: OfferStaffTransactionRequest |
| GET | `api/promotion/staff/force-check` | ⬜ | Query: storeNo, posID, staffCode |
| GET | `api/promotion/staff/check` | ⬜ | Query: storeNo, posID, quota_code |
| POST | `api/promotion/staff/redeem` | ⬜ | Body: PromotionStaffRedeemPOSRequest |
| POST | `api/promotion/staff/refund` | ⬜ | Body: PromotionStaffRefundPOSRequest |

---

## Module: Capillary CRM (`api/v2/loyalty/capillary/*` + điểm)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/loyalty/transaction/points/refund` | ⬜ | Body: VinIDRefundRequest |
| POST | `api/v2/loyalty/point/topup/revert` | ⬜ | Body: RevertTopUpPointsPOSRequest |
| POST | `api/v2/loyalty/point/topup` | ⬜ | Body: PointTopupPOSRequest |
| POST | `api/v2/loyalty/transaction/points` | ⬜ | Body: VinIDSalesRequest |
| POST | `api/v2/loyalty/transaction/program-points` | ⬜ | Body: ProgramPointsTransactionDto |
| POST | `api/v2/loyalty/transaction/member-business` | ⬜ | Body: MemberBusinessRequest |
| POST | `api/v2/loyalty/winscore/update` | ⬜ | Body: UpdateStatusWinscorePOS |
| GET | `api/v2/loyalty/points/history` | ⬜ | Query: storeNo, phoneNumber |
| POST | `api/v2/loyalty/capillary/mobile-enroll` | ⬜ | Body: Update_pos_enroll |
| GET | `api/v2/loyalty/capillary/transactions` | ⬜ | Query: orderNo, type, storeNo |
| GET | `api/v2/loyalty/capillary/check` | ⬜ | Query: status (optional) |
| GET | `api/v2/loyalty/capillary/check/switch/winx` | ⬜ | Kiểm tra switch CAP/WinX |
| POST | `api/v2/loyalty/capillary/action` | ⬜ | Query: status, userName |
| GET | `api/v2/loyalty/capillary/customer/redemptions` | ⬜ | Query: storeNo, id |
| POST | `api/v2/loyalty/capillary/points/redeem/revert` | ⬜ | Body: RevertPointsRedeemRequest |

---

## Module: SAP Voucher (`api/sap`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/sap/CheckVoucherBySerials` | ⬜ | Query: voucherNumber, posTerminal |
| GET | `api/sap/CheckVoucher` | ⬜ | Query: voucherNumber, siteNo, posTerminal, companyCode, ... |
| POST | `api/sap/CreateNewVoucher` | ⬜ | Body: List<CreateVoucherModel> |
| POST | `api/sap/UpdateVoucher` | ⬜ | Body: List<VoucherUpdateRequest> |
| POST | `api/sap/winlife/redeemCpnVch` | ⬜ | Body: VoucherUpdateModel — **endpoint phức tạp nhất** |
| GET | `api/sap/CheckReturnVoucher` | ⬜ | |
| POST | `api/sap/UpdateReturnVoucher` | ⬜ | |
| POST | `api/sap/CreateReturnVoucher` | ⬜ | |
| GET | `api/sap/EVoucher` | ⬜ | |
| GET | `api/sap/SendCodeCpnVch` | ⬜ | |
| GET | `api/sap/retry` | ⬜ | Retry giao dịch voucher thất bại |

---

## Module: PLG / Voucher Đối tác Cũ (`api/plg`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/plg/GetInfoCard` | ⬜ | Body: PLCheckVoucherRequest; partner: PLG/UrBox/GiftBox/GotIt/Giftee |
| PUT | `api/plg/SaleVoucher` | ⬜ | Body: SaleVoucherRequestPOS; chỉ partner PLG |
| POST | `api/plg/RedeemVoucher` | ⬜ | Body: ReedemVoucherRequestPOS; multi-partner |
| POST | `api/plg/Check_VC_Odoo` | ⬜ | WCF Odoo |
| POST | `api/plg/OdooSAP_Update_Voucher` | ⬜ | WCF Odoo |
| POST | `api/plg/Sale` | ⬜ | |
| GET | `api/plg/GetInfoMember` | ⬜ | |

---

## Module: Queue (`api/v2`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/sms/send` | ⬜ | Body: SMSMessage |
| POST | `api/v2/rabbit/producer` | ⬜ | Body: RabbitMessageDto |
| POST | `api/v2/rabbit/test` | ⬜ | Body: RabbitMessageDto |

---

## Module: Setting (`api/v2/setting`, `api/v2/cache`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/v2/setting/test/redis` | ⬜ | Test Redis Sentinel |
| GET | `api/v2/setting/cache/in-memory-all` | ⬜ | |
| POST | `api/v2/setting/cache/in-memory` | ⬜ | |
| DELETE | `api/v2/setting/cache/in-memory-delete/{key}` | ⬜ | |
| DELETE | `api/v2/cache/redis/{pos}/delete/{key}` | ⬜ | |
| POST | `api/v2/cache/redis/key/create` | ⬜ | |

---

## Module: SyncDataPos (`api/posblue`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/posblue/WriteFileByManual` | ⬜ | |
| GET | `api/posblue/GetFileFromFTP` | ⬜ | Phụ thuộc file system |
| POST | `api/posblue/UploadFileLogJob` | ⬜ | Upload file |
| GET | `api/posblue/process/sales/retry` | ⬜ | |
| POST | `api/posblue/UploadFileSale` | ⬜ | Kafka |
| POST | `api/posblue/DeleteFileFromAPI` | ⬜ | |
| GET | `api/posblue/GetFileScriptFromFTP` | ⬜ | |
| GET | `api/posblue/GetFileUpgradeToolFromFTP` | ⬜ | |
| GET | `api/posblue/DeleteFileFromRemote` | ⬜ | |
| GET | `api/posblue/DeleteFileFromFTP` | ⬜ | |
| POST | `api/posblue/DeleteFileExist` | ⬜ | |
| GET | `api/posblue/DowloadFileStream` | ⬜ | |
| GET | `api/posblue/ListFile` | ⬜ | |
| GET | `api/posblue/RetryProcessDataRaw` | ⬜ | |

---

## Module: Validate / Invoice (`api/v2`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/telegram/send` | ⬜ | |
| GET | `api/v2/validate/tax-code` | ⬜ | Query: taxCode |
| GET | `api/v2/validate/transaction` | ⬜ | Query: orderNo (15 ký tự) |
| POST | `api/v2/invoice/create` | ⬜ | Body: InvoiceCreatedRequest |
| POST | `api/v2/validate/member-business` | ⬜ | Body: ValidateMemberBusiness |

---

## Module: WinCare / WinPay Tích lũy

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/wincustomer/wpay/shopping-accumulate` | ⬜ | Body: WinPayAccumulationDto |
| POST | `api/v3/wincustomer/wpay/shopping-accumulate` | ⬜ | Phiên bản v3 |
| POST | `api/v2/wincare/OrderSupplierPublic/GenOspQRLogin` | ⬜ | |
| POST | `api/v2/wincare/collect-money/barcode` | ⬜ | |
| POST | `api/v2/wincare/collect-money/confirm` | ⬜ | |
| POST | `api/v2/wincare/notify` | ⬜ | |

---

## Module: WinLife / OTP / SmartPOS

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/v2/otp/generate` | ⬜ | Query: posNo, phoneNumber, action |
| POST | `api/v2/otp/verify` | ⬜ | Body: POSVerifyOTPRequest |
| GET | `api/blue/winlife/generateOTP` | ⬜ | Legacy CX OTP |
| POST | `api/blue/winlife/register` | ⬜ | Đăng ký hội viên WinLife |
| POST | `api/blue/winlife/update-promotions` | ⬜ | |
| GET | `api/blue/winlife/winCode-histories` | ⬜ | Query: csn, winCode |
| GET | `api/blue/winlife/smart-pos/customer-by-last-digits-phone` | ⬜ | Query: storeNo, posID, codeValue |
| POST | `api/blue/winlife/smart-pos/update-customer-info` | ⬜ | |

---

## Module: WinPay (`api/v2/winpay`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | `api/v2/winpay/register` | ⬜ | |
| GET | `api/v2/winpay/get-register-info` | ⬜ | Query: phone |
| POST | `api/v2/winpay/unregister` | ⬜ | |
| POST | `api/v2/winpay/payment` | ⬜ | |
| POST | `api/v2/winpay/refund` | ⬜ | |
| POST | `api/v2/winpay/deposit` | ⬜ | cashin |
| POST | `api/v2/winpay/withdraw` | ⬜ | cashout |
| POST | `api/v2/winpay/fp-update` | ⬜ | Vân tay |
| POST | `api/v2/winpay/fp-verify` | ⬜ | |
| POST | `api/v2/winpay/cashback` | ⬜ | |

---

## Module: VinID TopUp/eVoucher (`api/vinid`)

| Method | Endpoint | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | `api/vinid/TopUpCheckMember` | ⬜ | Query: phoneNumber, posID, storeNo |
| POST | `api/vinid/TopUpPoinToPhone` | ⬜ | Body: VinIDTopUpPoinPosRequest |
| GET | `api/vinid/TopUpCheckStatusOrder` | ⬜ | Query: orderNo, posID, storeNo |
| GET | `api/vinid/EVoucherVerify` | ⬜ | Query: storeNo, posID, serialNumber, userPOS |
| POST | `api/vinid/EVoucherRefund` | ⬜ | Body: VinIDEVoucherRefundPOSRequest |
| POST | `api/vinid/EVoucherMarkUsed` | ⬜ | Body: VinIDEVoucherUsedPosRequest |

---

## Tổng kết

| Module | Số endpoint | Trạng thái |
|---|---|---|
| Common | 20 | ✅ 18/20 (2 endpoint TODO) |
| Loyalty (v2 + VINID) | 22 | ⬜ |
| Payment (Partner Voucher) | 7 | ✅ 7/7 |
| Voucher CrownX | 3 | ⬜ |
| Gift | 2 | ✅ 2/2 |
| Offer | 8 | ⬜ |
| Capillary | 15 | ⬜ |
| SAP Voucher | 11 | ⬜ |
| PLG | 7 | ⬜ |
| Queue | 3 | ⬜ |
| Setting | 6 | ⬜ |
| SyncDataPos | 14 | ⬜ |
| Validate/Invoice | 5 | ⬜ |
| WinCare | 6 | ⬜ |
| WinLife/OTP | 8 | ⬜ |
| WinPay | 10 | ⬜ |
| VinID TopUp | 6 | ⬜ |
| **TỔNG** | **153** | **0/153 (0%)** |

---

## Ghi chú quan trọng

**Tuyệt đối không có cột "Endpoint mới"** — vì endpoint không thay đổi.

**Endpoint chú ý đặc biệt:**
- `api/sap/winlife/redeemCpnVch` — phức tạp nhất, cần phân tích kỹ
- `api/posblue/GetFileFromFTP` — phụ thuộc file system Windows, cần redesign
- `api/v2/loyalty/customer/get` — handle cả VINID legacy lẫn Capillary

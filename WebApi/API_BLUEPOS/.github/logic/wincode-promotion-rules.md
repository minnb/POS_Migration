# WinCode Promotion – Business Rules & Logic

> **Scope:** Luồng kiểm tra và trả về danh sách WinCode Promotion cho Hội viên tại thời điểm tra thẻ POS.
> **File liên quan:**
> - `API_WebApiCore/AppServices/Offer/WincodeService.cs` – `GetWinCodeByStore()`
> - `API_WebApiCore/AppServices/Capillary/LoyaltyService.cs` – `GetWinCodePromotion()`
> - `API_WebApiCore/AppServices/MemoryCacheService.cs` – `GetWinCodeMemory()`
> - `API_WebApiCore/Repository/WincodeRepository.cs` – `GetWinCodeCustomer()`

---

## 1. Luồng tổng quát

```
GetWinCodePromotion(storeNo, posNo, phoneNumber, coupons)
        │
        ▼
WincodeService.GetWinCodeByStore(merchantId="VCM", storeNo, phoneNumber)
        │
        ├──[1] GetWinCodeMemory()          ← MemoryCache (từ CentralMD DB)
        │       Bảng: WinCodeHeader + WinCodeStore
        │       TTL cache: theo MemoryCacheConst.MemoryCacheWinCode
        │
        ├──[2] Filter WinCode theo cửa hàng
        │       StoreNo == storeNo  → áp dụng cửa hàng cụ thể
        │       StoreNo == "ALL"    → áp dụng tất cả cửa hàng
        │
        ├──[3] GetWinCodeCustomer(phoneNumber) ← SQL Server (realtime)
        │       Lấy lịch sử HV đã nhận WinCode
        │
        ├──[4] Tính số lượng còn lại theo ApplyType
        │       BY_QTY   → kiểm tra toàn hệ thống
        │       BY_STORE → kiểm tra theo cửa hàng
        │
        └──[5] return List<WincodeResult>  → gán vào InfoMemberModel.AvailablePromotion
```

---

## 2. Cấu trúc dữ liệu

### 2.1 WinCode Config (MemoryCache từ DB)

| Field | Mô tả |
|-------|-------|
| `ProgramCode` | Mã chương trình (VD: `PROG001`) |
| `WinCode` | Mã WinCode (VD: `W001`, `W002`) |
| `StoreNo` | Mã cửa hàng hoặc `"ALL"` |
| `Quantity` | Tổng số lượng tối đa HV được nhận |
| `DiscountType` | Loại giảm giá: `BILL` (theo hóa đơn) hoặc `ITEM` (theo sản phẩm) |
| `ApplyType` | Cơ chế kiểm tra: `BY_QTY` hoặc `BY_STORE` |
| `Status` | `1` = active, `0` = inactive |
| `FromDate / ToDate` | Thời hạn hiệu lực của chương trình |

### 2.2 WinCode Customer History (SQL Server – realtime)

| Field | Mô tả |
|-------|-------|
| `WinCode` | Mã WinCode đã dùng |
| `StoreNo` | Cửa hàng đã dùng |
| `QuantityRecieptedSum` | Tổng số lần HV đã nhận WinCode này |
| `CreatedDate` | Ngày tạo (dùng để lấy bản ghi mới nhất) |

### 2.3 WincodeResult (Response trả về POS)

| Field | Mô tả |
|-------|-------|
| `WinCode` | Mã WinCode |
| `ProgramCode` | Mã chương trình |
| `Quantity` | Số lượng **còn lại** HV có thể nhận |
| `DiscountType` | Loại giảm giá |
| `MerchantId` | Luôn là `"VCM"` |

---

## 3. Business Rules

---

### Rule 1: Điều kiện WinCode được load vào cache

```sql
-- WinCodeHeader phải thỏa:
Status = 1
AND CAST(FromDate AS DATE) <= CAST(GETDATE() AS DATE)
AND CAST(ToDate AS DATE)   >= CAST(GETDATE() AS DATE)

-- WinCodeStore phải thỏa:
Status = 1
AND ProgramCode IN (danh sách ProgramCode từ WinCodeHeader)
```

> ⚠️ WinCode hết hạn hoặc bị tắt (`Status = 0`) sẽ **không xuất hiện** trong response.

---

### Rule 2: Filter WinCode theo cửa hàng

```
StoreNo = "{mã cụ thể}"  → chỉ áp dụng cho cửa hàng đó
StoreNo = "ALL"          → áp dụng cho TẤT CẢ cửa hàng
```

> ⚠️ **Quan trọng:** Không được gán `wincode.StoreNo = storeNo` trực tiếp trên object cache
> (gây race condition khi 5000 POS gọi đồng thời).
> Thay vào đó dùng `.Where(x => x.StoreNo == storeNo || x.StoreNo == "ALL")`.

---

### Rule 3: ApplyType – 2 cơ chế giới hạn số lần nhận

#### BY_QTY (= 0): Giới hạn theo tổng số lần – không phân biệt cửa hàng

```
Điều kiện check:
    checkQty = lịch sử HV với WinCode này (bất kể cửa hàng nào)
               → lấy bản ghi mới nhất theo CreatedDate

Quota còn lại:
    remainQty = config.Quantity - checkQty.QuantityRecieptedSum

Ví dụ:
    config.Quantity = 3
    HV đã nhận 2 lần (ở bất kỳ cửa hàng nào)
    → remainQty = 1  ✅ còn được nhận 1 lần nữa
```

#### BY_STORE (= 1): Giới hạn theo cửa hàng – mỗi cửa hàng tính riêng

```
Điều kiện check:
    checkQty = lịch sử HV với WinCode này TẠI cửa hàng hiện tại
               → lấy bản ghi mới nhất theo CreatedDate

Quota còn lại:
    remainQty = config.Quantity - checkQty.QuantityRecieptedSum

Ví dụ:
    config.Quantity = 2
    HV đã nhận 2 lần tại cửa hàng A → remainQty = 0 ❌
    HV nhận tại cửa hàng B → checkQty = null → remainQty = 2 ✅
```

---

### Rule 4: Tính toán số lượng còn lại

```
qty = 0  (mặc định – HV chưa dùng WinCode này)

Nếu checkQty != null:
    qty = checkQty.QuantityRecieptedSum

remainQty = config.Quantity - qty

Điều kiện ADD vào kết quả:
    remainQty > 0  → add WincodeResult { Quantity = remainQty }
    remainQty == 0 → bỏ qua (continue)
    remainQty < 0  → không xảy ra (qty <= config.Quantity theo thiết kế)
```

---

### Rule 5: Trường hợp HV chưa có lịch sử

```
GetWinCodeCustomer(phoneNumber) trả về null hoặc empty
→ checkQty = null với mọi WinCode
→ qty = 0 với tất cả
→ HV được nhận toàn bộ số lượng config (Quantity đầy đủ)
```

---

### Rule 6: Các tính năng đã có nhưng chưa kích hoạt (commented out)

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Coupon từ Capillary | ❌ Commented | Cần active khi tích hợp coupon CAP |
| Offer CBNV (nhân viên) | ❌ Commented | Cần active khi triển khai ưu đãi nội bộ |

---

## 4. Sơ đồ quyết định (Decision Tree)

```
WinCode config loaded từ cache
        │
        ├── StoreNo == "ALL" hoặc StoreNo == storeNo?
        │       │
        │     Không → bỏ qua WinCode này
        │       │
        │      Có
        │       ▼
        │   ApplyType?
        │   ├── BY_QTY  → checkQty = lịch sử toàn hệ thống
        │   └── BY_STORE → checkQty = lịch sử tại cửa hàng hiện tại
        │       │
        │       ▼
        │   checkQty tồn tại?
        │   ├── Không → qty = 0 (HV chưa dùng)
        │   └── Có    → qty = QuantityRecieptedSum
        │       │
        │       ▼
        │   remainQty = config.Quantity - qty
        │       │
        │   ├── remainQty > 0  → ADD vào kết quả (Quantity = remainQty)
        │   └── remainQty <= 0 → SKIP
        │
        ▼
return List<WincodeResult>
```

---

## 5. Các lưu ý kỹ thuật quan trọng

| # | Vấn đề | Giải pháp đã áp dụng |
|---|--------|----------------------|
| 1 | **Race condition cache** – mutate `StoreNo` trực tiếp trên object MemoryCache | ✅ Dùng `.Where(x => x.StoreNo == storeNo \|\| x.StoreNo == "ALL")` – không gán |
| 2 | **Null check sai** – `.ToList()` không bao giờ trả về `null` | ✅ Đổi sang kiểm tra `.Count == 0` |
| 3 | **Exception bị nuốt** – `catch` không log | ✅ Thêm `FileHelper.WriteExpLogs(...)` |
| 4 | **DB call đồng bộ** – `GetWinCodeCustomer()` là synchronous trong luồng `async` | ⚠️ Cần xem xét chuyển sang `async` khi refactor |

---

## 6. Ví dụ thực tế

**Scenario:** HV `0912345678` tại cửa hàng `WM001`

| WinCode | StoreNo | ApplyType | Quantity config | Đã nhận | Còn lại | Kết quả |
|---------|---------|-----------|----------------|---------|---------|---------|
| W001 | WM001 | BY_STORE | 2 | 1 (WM001) | 1 | ✅ Add |
| W002 | ALL | BY_QTY | 3 | 3 (bất kỳ) | 0 | ❌ Skip |
| W003 | WM002 | BY_STORE | 2 | 0 | - | ❌ Skip (sai store) |
| W004 | ALL | BY_STORE | 1 | 0 (WM001) | 1 | ✅ Add |
| W005 | WM001 | BY_QTY | 2 | 0 | 2 | ✅ Add |

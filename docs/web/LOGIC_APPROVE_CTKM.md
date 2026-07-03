# LOGIC_APPROVE_CTKM — Tài liệu kỹ thuật luồng "Duyệt CTKM"

> **Mục đích:** tài liệu *code-flow kỹ thuật* phục vụ **bảo trì** — trace toàn bộ luồng từ khi
> người dùng bấm nút **"Duyệt"** trên giao diện Blazor xuống tận Database.
> **Đối tượng đọc:** Dev bảo trì, kỹ sư hệ thống.
> **Khác với** [`offer-coupon-flow.md`](offer-coupon-flow.md) (tài liệu nghiệp vụ + test case).

| Thuộc tính | Giá trị |
|---|---|
| Chức năng | **Cài đặt CTKM** (Chương trình khuyến mãi / Bonus Buy) |
| Route | `/promotion/setup` |
| Trang | [`PromotionSetupPage.razor`](../../src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor) |
| Database | `RPOSMasterData` (CentralMD) |
| Quyền | `WebPolicies.OpsAndAbove` → **ITOps** hoặc **SystemAdmin** (StoreOperator bị chặn) |

## Sơ đồ tổng thể (Sequence)

```
[UI Blazor]  PromotionSetupPage.razor
  │  bấm "Duyệt" → ApproveAsync(bbynr)
  │  ├─ Confirm dialog (MudMessageBox)   ─── Hủy → dừng
  │  ▼
[Application]  IPromotionService.ApproveSetupAsync(bbynr)   ← thin wrapper (chỉ delegate)
  │  ▼
[Infrastructure]  PromotionRepository.ApproveSetupAsync(bbynr)
  │  Dapper ExecuteAsync → SP, timeout 300s, DB RPOSMasterData
  │  ▼
[Database]  dbo.usp_SetupPromotion_Approve @BBYNR   (BEGIN TRAN / XACT_ABORT ON)
  │  ├─ 1) UPDATE SetupPromotionHEADER SET IsApprove = 1
  │  └─ 2) EXEC Setup_Promotion_Insert @BBY  ── publish nháp → Offer* (LIVE)
  │  COMMIT (lỗi → ROLLBACK + THROW)
  ▼
[Post]  Audit log (DashboardAuditLog) + khóa form UI + hiển thị ở /promotion/offers → POS áp dụng
```

---

## 1. Tổng quan luồng kích hoạt (Trigger Workflow)

### 1.1. Hàm C# được gọi

Cả hai nút "Duyệt" đều gọi chung một hàm:

```csharp
private async Task ApproveAsync(string bbynr)
```

Vị trí: [`PromotionSetupPage.razor:639`](../../src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor#L639).

| Nút | Vị trí | Điều kiện hiển thị |
|---|---|---|
| `MudIconButton` (icon `CheckCircle`) — màn **danh sách** | [dòng ~104-105](../../src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor#L104) | chỉ hiện khi `!context.IsApprove` (CTKM chưa duyệt) |
| `MudButton "Duyệt CTKM"` — màn **editor** | [dòng ~437-438](../../src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor#L437) | chỉ hiện khi `!_isReadonly` **và** `_header.No` khác rỗng |

### 1.2. Validate tại UI (Frontend) trước khi gửi

| Điều kiện | Cơ chế | Ghi chú |
|---|---|---|
| **Phân quyền** | `@attribute [Authorize(Policy = WebPolicies.OpsAndAbove)]` | Chỉ ITOps/SystemAdmin vào được trang |
| **Chưa duyệt** | Nút Duyệt chỉ render khi `IsApprove == false` | Đã duyệt → nút biến mất |
| **Đã có mã (BBYNR)** | Trong editor, nút chỉ hiện khi `_header.No` khác rỗng | Phải **Lưu** trước để sinh `BBYNR` mới thấy nút Duyệt |
| **Xác nhận** | `MudMessageBox`: *"Duyệt và phát hành CTKM {bbynr}? Sau khi duyệt sẽ không sửa được."* | Người dùng bấm Hủy → `return`, không gọi service |

> **Lưu ý:** không có validate **field-level** khi Duyệt (khác với thao tác Lưu). Duyệt chỉ cần một
> `BBYNR` hợp lệ đã tồn tại; mọi ràng buộc dữ liệu đã được kiểm tại bước Lưu.

### 1.3. Trình tự trong `ApproveAsync`

1. Hiện confirm dialog → nếu `Canceled` thì `return`.
2. Gọi `PromotionService.ApproveSetupAsync(bbynr)` → nhận `(bool ok, string message)`.
3. Nếu `!ok` → `Snackbar.Add(message, Severity.Error)` + `return`.
4. Ghi audit: `AuditLogger.LogAsync(_actor, "APPROVE", "SetupPromotion", bbynr, null, null)`.
5. `Snackbar.Add(message, Severity.Success)`.
6. Cập nhật UI: nếu đang ở editor và `_header.No == bbynr` → `_isReadonly = true; _header.IsApprove = true` (khóa form);
   nếu ở màn danh sách → `await _table.ReloadServerData()`.
7. `catch` → `FileLogger.WriteExpLogs("PromotionSetupPage.Approve", ex)` + snackbar đỏ.

---

## 2. Tầng nghiệp vụ (Service / Business Logic Layer)

| Thành phần | File |
|---|---|
| Interface | [`IPromotionService`](../../src/POS.Application/Features/Promotion/IPromotionService.cs) |
| Implementation | [`PromotionService.cs:35`](../../src/POS.Application/Features/Promotion/PromotionService.cs#L35) |

```csharp
public Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default)
    => repository.ApproveSetupAsync(bbynr, ct);
```

- Tầng Application là **thin wrapper** — chỉ **delegate** thẳng xuống repository, đúng quy ước AppService/3-lớp.
- **KHÔNG** có logic tính toán, **KHÔNG** có nhánh IF/ELSE quyết định luồng.
- **KHÔNG** gọi API bên thứ 3, **KHÔNG** gọi service liên kết nào khác.

---

## 3. Tầng dữ liệu (Data Access Layer)

| Thành phần | File |
|---|---|
| Interface | [`IPromotionRepository`](../../src/POS.Infrastructure/Repositories/Promotion/IPromotionRepository.cs) |
| Implementation | [`PromotionRepository.cs:245`](../../src/POS.Infrastructure/Repositories/Promotion/PromotionRepository.cs#L245) |
| Connection | `CentralMDConnectionFactory` → DB **RPOSMasterData** |

```csharp
public async Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default)
{
    try
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "dbo.usp_SetupPromotion_Approve", new { BBYNR = bbynr },
            commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));
        return (true, $"Duyệt CTKM {bbynr} thành công");
    }
    catch (Exception ex)
    {
        return (false, ex.Message);
    }
}
```

- Thực thi bằng **Dapper** (`ExecuteAsync`) gọi Stored Procedure `dbo.usp_SetupPromotion_Approve`.
- Tham số: `@BBYNR` = mã CTKM. `commandTimeout = 300` giây (publish có thể chạy lâu với CTKM lớn).
- Exception bị **nuốt** tại repository → trả `(false, ex.Message)`; UI hiển thị message này qua snackbar
  (không throw lên middleware).

### 3.1. Stored Procedure `dbo.usp_SetupPromotion_Approve`

Script: [`docs/sql/SetupPromotion_ApproveAndStatus.sql`](../sql/SetupPromotion_ApproveAndStatus.sql).

```sql
CREATE PROCEDURE dbo.usp_SetupPromotion_Approve
(
    @BBYNR  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SetupPromotionHEADER WHERE BBYNR = @BBYNR)
    BEGIN
        RAISERROR (N'Không tìm thấy CTKM %s', 16, 1, @BBYNR);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SetupPromotionHEADER
        SET    IsApprove = 1
        WHERE  BBYNR = @BBYNR;

        -- Publish draft sang Offer* (SP nghiệp vụ đã có sẵn)
        EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBYNR;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
```

**Các bảng bị tác động:**

| Thao tác | Bảng | Điều kiện / Nội dung |
|---|---|---|
| `UPDATE` | `dbo.SetupPromotionHEADER` | `SET IsApprove = 1 WHERE BBYNR = @BBYNR` |
| `INSERT` (gián tiếp qua `Setup_Promotion_Insert`) | `OfferHeader`, `OfferBuy`, `OfferGet`, `OfferBenefit`, `OfferSite` | Publish dữ liệu nháp sang bảng LIVE để máy POS áp dụng |

**Cơ chế Transaction / Rollback:** CÓ.
- Một transaction bao trọn cả `UPDATE` + `EXEC Setup_Promotion_Insert`.
- `SET XACT_ABORT ON` + khối `TRY/CATCH` với `IF XACT_STATE() <> 0 ROLLBACK; THROW` → mọi lỗi
  (kể cả lỗi bên trong SP publish) đều **rollback nguyên tử**: không có trạng thái nửa vời
  (đã đánh dấu duyệt nhưng chưa publish, hoặc ngược lại).
- Guard đầu SP: `BBYNR` không tồn tại → `RAISERROR` + `RETURN` (không mở transaction).

### 3.2. Chi tiết mapping `SetupPromotion*` (nháp) → `Offer*` (LIVE)

> Transformation được thực thi bởi SP **`dbo.Setup_Promotion_Insert @BBY`** (SP nghiệp vụ có sẵn trong DB,
> **không nằm trong source repo** — không reverse SP body ở đây). Bảng dưới mô tả **ánh xạ cột mức schema**
> để đối chiếu khi bảo trì. Nguồn cột: [`SetupPromotion_Save.sql`](../sql/SetupPromotion_Save.sql) (bên nháp) và
> `docs/architecture/database-schema.md` — bảng `Offer*` (bên LIVE).
> **Nguồn chính thức của logic vẫn là SP `Setup_Promotion_Insert`.**

**`SetupPromotionHEADER` → `OfferHeader`**

| Cột nguồn (SetupPromotionHEADER) | Cột đích (OfferHeader) | Ghi chú |
|---|---|---|
| `BBYNR` | `No` | Mã offer |
| `SalesType` | `SalesType` / `SalesTypeFilter` | Hình thức bán |
| `BBYTEXT` | `Description` | Tên CTKM |
| `BBYTYPE` | `OfferType` / `Type` | Loại CTKM |
| `STATUS` | `Status` | 0=Đang áp dụng, 1=Lên kế hoạch, 2=Ngưng |
| `VALIDFROM` (yyyyMMdd) | `StartingDate` (datetime) | Chuyển chuỗi → ngày |
| `VALIDTO` (yyyyMMdd) | `EndingDate` (datetime) | |
| `IsVoucher` | `IsVoucher` | |
| `BUYLINKCAT` (A/O) | `ConditionBuy` (int) | A=AND, O=OR |
| `GETLINKCAT` (A/O) | `ConditionGet` (int) | |
| `VINID` ('X' nếu chỉ member) | `MemberOnly` | |
| `LIMIT` | `LimitQty` | Số lần áp dụng |
| `MemberCode` | `MemberCode` | Hạng thẻ |
| `ZPRIOR` | `PriorityBBY` | Độ ưu tiên |
| `NUMOFDAYS` | `NumOfDays` | Ngày áp dụng trong tháng |
| `ZVCDATE_ST` / `ZVCDATE_EN` | `VoucherFromDate` / `VoucherToDate` | |
| `ZVCDATE_VA` | `VoucherValidDay` | Số ngày hiệu lực voucher |
| `LIMITNR` | `VoucherLimitNumber` | Số lần phát hành |

**`SetupPromotionBUY` → `OfferBuy`**

| Cột nguồn | Cột đích | Ghi chú |
|---|---|---|
| `BBYNR` | `OfferNo` | |
| `BUYTYPE` (MAT/MGP) | `LineType` (0/1) | MAT=0 (Sản phẩm), MGP=1 (Nhóm SP) |
| `MAT_NR` | `No` | Mã sản phẩm |
| `MATGROUP` | `BonusBuyNo` / `LineGroup` | Mã nhóm SP |
| `MAT_QUAN` | `Quantity` | |
| `MEINH` | `UnitOfMeasure` | ĐVT |
| `ScaleType` | `ScaleType` | |

**`SetupPromotionGET` → `OfferGet`** (và `OfferBenefit` cho nhánh total-bill)

| Cột nguồn | Cột đích | Ghi chú |
|---|---|---|
| `BBYNR` | `OfferNo` | |
| `GETTYPE` (MAT/MGP) | `LineType` | MAT=0, MGP=1 |
| `MATERIALCODE` | `No` | Mã sản phẩm |
| `MATGROUP` | `BonusBuyNo` | Mã nhóm SP |
| `QTY` | `Quantity` | |
| `MEINH` | `UnitOfMeasure` | |
| `DISTYPE` (%/R/P) | `DiscountType` (0/1/2) | %=0 (phần trăm), R=1 (số tiền), P=2 (giá cố định) |
| `BBYVAL` / `BBYPER` | `DiscountValue` | BBYPER khi %, BBYVAL khi R/P |

**`SetupPromotionSITE` → `OfferSite`**

| Cột nguồn | Cột đích | Ghi chú |
|---|---|---|
| `BBYNR` | `OfferNo` | |
| `SITECODE` | `StoreNo` | Mã cửa hàng (đã bung từ nhóm khi lưu) |
| `SITEGROUPCODE` | `PriceGroupCode` / `LocalSiteGroup` | Mã nhóm cửa hàng |

---

## 4. Sự kiện sau khi Duyệt (Post-Approve Events)

| Sự kiện | Chi tiết |
|---|---|
| **Audit log** | `IAuditLogger.LogAsync(_actor, "APPROVE", "SetupPromotion", bbynr, null, null)` → 1 dòng trong `DashboardAuditLog` (action `APPROVE`, entity `SetupPromotion`, key = BBYNR). Chỉ ghi khi thao tác **thành công**. |
| **Khóa UI** | Editor chuyển readonly (chip *"Đã duyệt — chỉ xem"*), nút Lưu/Duyệt biến mất. Ở màn danh sách: bảng reload, dòng chuyển trạng thái "Đã duyệt". |
| **Publish nghiệp vụ** | Dữ liệu xuất hiện ở bảng `Offer*` → hiển thị tại [`/promotion/offers`](../../src/POS.Web/Components/Pages/Promotion/Offers/OffersPage.razor) → **máy POS tải và áp dụng CTKM**. |
| **Bất khả nghịch** | CTKM đã duyệt **không sửa/không hủy** được từ UI. Muốn thay đổi phải thao tác trực tiếp DB (ngoài phạm vi ứng dụng). |

---

## 5. Xử lý lỗi & Edge case

| Tình huống | Hành vi |
|---|---|
| `BBYNR` không tồn tại trong `SetupPromotionHEADER` | SP `RAISERROR('Không tìm thấy CTKM %s')` → repository catch → `(false, message)` → snackbar đỏ. |
| Lỗi trong `Setup_Promotion_Insert` (publish) | Transaction rollback (XACT_ABORT + THROW) → repository catch → `(false, ex.Message)` → snackbar đỏ; **không có** trạng thái nửa vời. |
| Người dùng bấm Hủy ở confirm dialog | `ApproveAsync` `return` sớm — không gọi service, không ghi DB. |
| Exception phía Blazor (mất kết nối…) | `catch` trong `ApproveAsync` → `FileLogger.WriteExpLogs("PromotionSetupPage.Approve", ex)` + snackbar đỏ; log file tại `D:\ROOT\Logs\POS.Web\Exception\log-yyyyMMdd.txt`. |

---

## 6. Tham chiếu nguồn (Dev)

| Lớp | File | Điểm neo |
|---|---|---|
| UI (Blazor) | [`PromotionSetupPage.razor`](../../src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor) | `ApproveAsync` (~L639), nút Duyệt (L104, L437) |
| Service (Application) | [`PromotionService.cs`](../../src/POS.Application/Features/Promotion/PromotionService.cs) | `ApproveSetupAsync` (L35) |
| Repository (Infrastructure) | [`PromotionRepository.cs`](../../src/POS.Infrastructure/Repositories/Promotion/PromotionRepository.cs) | `ApproveSetupAsync` (L245) |
| SP Duyệt | [`docs/sql/SetupPromotion_ApproveAndStatus.sql`](../sql/SetupPromotion_ApproveAndStatus.sql) | `usp_SetupPromotion_Approve` |
| SP Lưu (nguồn cột nháp) | [`docs/sql/SetupPromotion_Save.sql`](../sql/SetupPromotion_Save.sql) | `usp_SaveSetupCTKMAll` |
| SP publish (có sẵn trong DB) | *(ngoài source repo)* | `Setup_Promotion_Insert @BBY` |

**Liên quan:**
- Tài liệu nghiệp vụ + test case: [`offer-coupon-flow.md`](offer-coupon-flow.md).
- Rollout / script SQL cần chạy trước khi test: [`docs/ROLLOUT.md`](../ROLLOUT.md).

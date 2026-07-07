# Xác nhận kết thúc ngày (EOD) — Logic tổng quan

> Trang: `/store/business-day` — `BusinessDayPage.razor`. Port có chủ đích từ legacy
> `StoreActivitiesController.ConfirmEndingDateStores` / `CheckFinishDate`
> (`src/legacy/VCM.BLUEPOS/Controllers/StoreActivitiesController.cs`).

## 1. Mục đích

Xác nhận 1 cửa hàng đã "kết thúc ngày kinh doanh" cho 1 ngày cụ thể — chỉ cho phép xác nhận khi
**toàn bộ máy POS đang hoạt động (Status=1) của cửa hàng đó đã đóng ngày**. Sau khi xác nhận,
ngày kinh doanh của cửa hàng (bảng `BussinessDateOpen`) tự động **advance +1 ngày**, để API
`GetBusinessDate` (đang phục vụ ~5.000 máy POS) trả đúng ngày kinh doanh mới.

## 2. Luồng nghiệp vụ

```
User mở /store/business-day
  → chọn 1 cửa hàng (MudSelect, bắt buộc — role Store chỉ thấy cửa hàng được phân quyền,
    role Ops/Admin thấy tất cả) + ngày kinh doanh (mặc định hôm nay)
  → bấm "Tìm kiếm" (KHÔNG tự load khi mở trang)
    * Ngoại lệ StoreOperator: mở trang tự chọn cửa hàng của họ + ngày kinh doanh HIỆN TẠI của
      store (BussinessDateOpen, qua GetCurrentBusinessDateAsync; null → hôm nay) và tự load luôn,
      không cần bấm "Tìm kiếm". ITOps/Admin vẫn thao tác thủ công.
      → BusinessDayService.GetPosDayStagingAsync(storeNo, businessDate)
      → BusinessDayService.GetConfirmStatusAsync(storeNo, businessDate)
  → hiển thị bảng 1 dòng / 1 máy POS (chỉ POS Status=1) + trạng thái xác nhận hiện tại
  → nút "Xác nhận" bật khi CHƯA xác nhận trước đó VÀ (role ITOps/SystemAdmin — force được kể cả
    khi còn POS chưa đóng ngày; HOẶC StoreOperator + có ít nhất 1 máy POS "Đã đóng ngày" VÀ
    không còn máy nào ở trạng thái "Chưa đóng ngày" — máy "Chưa mở ca" KHÔNG chặn xác nhận)
  → bấm "Xác nhận" → MudMessageBox confirm → BusinessDayService.ConfirmBusinessDayAsync(..., allowForceConfirm)
      → chặn lại lần cuối (re-validate) nếu còn POS chưa đóng ngày (BỎ QUA khi allowForceConfirm=
        true, tức ITOps/SystemAdmin), hoặc đã xác nhận rồi
      → CentralSaleRepository.ConfirmBusinessDayAsync → SP usp_BusinessDay_ConfirmEndDate
          (1 transaction: advance BussinessDateOpen +1 ngày + insert ledger BusinessDayConfirm)
      → thành công → AuditLogger.LogAsync(..., "UPDATE", "BusinessDay", ...) + reload lại lưới
```

## 3. Trạng thái từng máy POS (3 mức, hiển thị trên cột "Trạng thái")

| Điều kiện | Nhãn | Màu chip |
|---|---|---|
| Có dòng trong `POSEOD_API` cho (Store, Terminal, Ngày) | **Đã đóng ngày** | Success (xanh) |
| Không có dòng `POSEOD_API` **và** chưa từng có `TransHeader` trong ngày (`LastSaleTime = null`) | **Chưa mở ca** | Default (xám) |
| Không có dòng `POSEOD_API` **nhưng** đã có giao dịch (`LastSaleTime != null`) | **Chưa đóng ngày** | Warning (cam) |

> Rule chặn xác nhận (áp dụng cho StoreOperator, không áp dụng khi ITOps/SystemAdmin force):
> - **"Chưa đóng ngày"** (đã có giao dịch — `LastSaleTime != null` — nhưng chưa đóng ngày) →
>   **LUÔN chặn xác nhận**, bất kể còn bao nhiêu máy khác đã đóng ngày.
> - **"Chưa mở ca"** (`LastSaleTime == null`, chưa từng bán hàng) → **KHÔNG chặn xác nhận**, coi
>   như máy đó không tham gia ngày kinh doanh này — TRỪ KHI **toàn bộ** máy POS của cửa hàng đều
>   ở trạng thái này (không có máy nào "Đã đóng ngày") → khi đó vẫn chặn (không có gì để xác nhận).

## 4. Database & Procedure

### 4.1 Đọc dữ liệu (raw SQL tham số hoá — KHÔNG dùng stored procedure)

| Nguồn | Nơi gọi | DB / Connection | Bảng đọc |
|---|---|---|---|
| Danh sách máy POS master (lọc `Status=1`) | `CentralMDRepository.GetPosTerminalListAsync()` | **CentralMD (RPOSMasterData)** | `POSTerminal` LEFT/OUTER APPLY `POSMonitor` |
| Staging số liệu theo máy POS + ngày | `CentralSaleRepository.GetPosDayStagingAsync(storeNo, businessDate)` | **CentralSale — per-store shard** (`StoreRoutedConnectionFactory`) | `TransHeader`, `TransLine`, `POSShiftHeader`, `POSShiftLine`, `POSEOD_API` |
| Trạng thái đã xác nhận (nếu có) | `CentralSaleRepository.GetBusinessDayConfirmAsync(storeNo, businessDate)` | CentralSale — per-store shard | `dbo.BusinessDayConfirm` |

`BusinessDayService.GetPosDayStagingAsync` (Application layer) **merge 2 nguồn đầu**: danh sách
POS master (Status=1) là gốc, bổ sung số liệu staging + `Placement` ("Loại POS").

### 4.2 Ghi dữ liệu — 1 stored procedure duy nhất

**`dbo.usp_BusinessDay_ConfirmEndDate`** — script: `docs/sql/BusinessDay_ConfirmEndDate.sql`.

> ⚠️ Chạy trên DB **"CentralSale" theo TỪNG STORE** (shard, cùng DB với `BussinessDateOpen`) —
> **KHÔNG PHẢI CentralMD/RPOSMasterData** như quy ước SP mặc định của dự án. Lý do: cần 1
> transaction atomic giữa "advance ngày cho máy POS" và "ghi ledger xác nhận".

Tham số: `@StoreNo, @BusinessDate, @TotalRevenue, @TotalShifts, @ConfirmedBy`.

Logic (trong 1 `BEGIN TRAN`):
1. Nếu `dbo.BusinessDayConfirm` đã có dòng (Store, Ngày) → `THROW 50001` ("đã xác nhận trước đó").
2. `UPDATE dbo.BussinessDateOpen SET BussinessDate = DATEADD(DAY,1,@BusinessDate) WHERE StoreNo=@StoreNo AND BussinessDate=@BusinessDate` — nếu `@@ROWCOUNT=0` → `THROW 50002` (ngày không khớp/đã đổi).
3. `INSERT INTO dbo.BusinessDayConfirm (...)`.
4. `COMMIT` (lỗi ở bất kỳ bước nào → `ROLLBACK` + `THROW`).

### 4.3 Bảng liên quan

| Bảng | DB | Vai trò trong tính năng này |
|---|---|---|
| `POSTerminal` | CentralMD | Master máy POS — nguồn duy nhất xác định "còn tồn tại/đang active" (`Status=1`), cột `Placement` = "Loại POS" |
| `TransHeader` / `TransLine` | CentralSale (per-store) | Tính doanh thu, số lượt khách, số lượng bán, thời điểm bán cuối |
| `POSShiftHeader` / `POSShiftLine` | CentralSale (per-store) | Tính "Tiền mặt" (⚠️ giả định tên bảng giống schema CentralSale trung tâm, chưa xác minh 100% trên shard — xem TODO trong code) |
| `POSEOD_API` | CentralSale (per-store) | Nguồn sự thật cho "đã đóng ngày" — có dòng = terminal đã tự báo cáo EOD (ghi qua API `UpdatePOSEODAsync` sẵn có) |
| `BussinessDateOpen` | CentralSale (per-store) | Ngày kinh doanh hiện tại của cửa hàng — bị advance +1 khi xác nhận; là bảng API `GetBusinessDate` (POS-facing) đọc |
| `dbo.BusinessDayConfirm` (MỚI) | CentralSale (per-store) | Ledger xác nhận — PK `Code = StoreNo + yyyyMMdd` |

## 5. DTOs (`POS.Common/Dtos/CentralSale/`)

- `PosDayStagingDto` — 1 dòng/máy POS: `PosTerminal, Placement, IsClosed, CloseTime, LastSaleTime, TotalRevenue, TienMat, CustomerCount, TotalQuantity`
- `BusinessDayConfirmDto` — `StoreNo, BusinessDate, TotalRevenue, TotalShifts, ConfirmedBy, ConfirmedDate`
- `ConfirmBusinessDayRequest` — input cho SP: `StoreNo, BusinessDate, TotalRevenue, TotalShifts, ConfirmedBy`
- `ConfirmBusinessDayResult` — `Success, Message, NewBusinessDate`

## 6. Sơ đồ file

```
POS.Web/Components/Pages/Store/Operations/BusinessDayPage.razor   ← UI
POS.Web/Components/Layout/MainLayout.razor                        ← menu "Xác nhận kết thúc ngày"
POS.Application/Features/StoreActivities/IBusinessDayService.cs
POS.Application/Features/StoreActivities/BusinessDayService.cs    ← rule + merge master/staging
POS.Infrastructure/Repositories/Sale/ICentralSaleRepository.cs
POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs     ← GetPosDayStagingAsync,
                                                                       GetBusinessDayConfirmAsync,
                                                                       ConfirmBusinessDayAsync
POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs ← GetPosTerminalListAsync (tái dùng, có sẵn)
POS.Common/Dtos/CentralSale/{PosDayStagingDto,BusinessDayConfirmDto,ConfirmBusinessDayRequest,ConfirmBusinessDayResult}.cs
docs/sql/BusinessDay_ConfirmEndDate.sql                            ← bảng + SP (chạy trên CentralSale/store)
```

## 7. Giả định / TODO chưa xác minh 100%

- "Đã đóng ngày" dựa vào tồn tại `POSEOD_API` — giả định hợp lý theo API `UpdatePOSEODAsync` sẵn
  có, chưa kiểm chứng với vận hành thực tế.
- Tên bảng `POSShiftHeader`/`POSShiftLine` trên shard DB từng store — giả định giống schema
  CentralSale trung tâm, có TODO comment trong `CentralSaleRepository.GetPosDayStagingAsync`.
- Công thức "Số lượt khách hàng"/"Số lượng bán" tự suy ra từ `TransHeader`/`TransLine` (SP legacy
  gốc `SP_END_DATE_CONFIRM_STAGING` đã compile sẵn, không có script đối chiếu).
- Ngoài phạm vi (chưa port): đối chiếu doanh số POS↔Central tự động re-sync, xác nhận bù nhiều
  ngày cũ, nhánh IT force-confirm/huỷ xác nhận, báo cáo PDF cuối ngày.

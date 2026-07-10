# SyncTableList.POSLastCounter — trạng thái triển khai

> Tổng kết ngắn gọn cơ chế cập nhật `SyncTableList.POSLastCounter` bất đồng bộ (triển khai
> 2026-07-09). Chi tiết đầy đủ + lý do kiến trúc: `.claude/rules/masterdata-sync.md` mục "Cập nhật
> `POSLastCounter` bất đồng bộ". Cấu trúc code: `docs/CURRENT_STRUCTURE.md`.

## Bối cảnh

`SyncTableList.POSLastCounter` trước đây **chưa từng được ghi** ở bất kỳ đâu trong codebase (SP
`[SyncTable_Get]` chỉ SELECT) — luồng sync master data cho POS luôn full-resync với
`@POSLastCounter=0`. Đây là tính năng xây mới để về sau có thể chuyển dần sang incremental sync,
không phải sửa lại code đang chạy.

Mỗi bảng master data tự có cột `Counter bigint` riêng (bump thủ công kiểu `MAX(Counter)+1` trong SP
hoặc C#, KHÔNG phải IDENTITY). Mục tiêu: mỗi khi 1 write-path bump `Counter`, đẩy giá trị mới vào
`SyncTableList.POSLastCounter` — nhưng **không** update đồng bộ trong transaction ghi, để tránh
row-level lock contention trên `SyncTableList` khi nhiều request ghi master data cùng lúc.

## Cách triển khai

Kiến trúc: **`System.Threading.Channels` in-process + `BackgroundService` batch-flush định kỳ**
(đã cân nhắc thêm RabbitMQ và SQL Job/Trigger, không chọn — xem lý do trong
`.claude/rules/masterdata-sync.md`).

```
Repository ghi Counter thành công
  → ISyncTableTrackerService.Track(tableName, counter)   [non-blocking, ghi Channel in-memory]
      ↓ (không chặn request, không mở transaction phụ)
SyncTableCounterFlushWorker (BackgroundService)
  → mỗi FlushIntervalSeconds (mặc định 5s): drain Channel, coalesce Max theo bảng
  → ISyncTrackerRepository.BulkUpdateCounterAsync(...)
      → SP usp_SyncTableList_BulkUpdateCounter (TVP dbo.TVP_SyncCounterUpdate)
      → UPDATE SyncTableList SET POSLastCounter=... WHERE Counter > ISNULL(POSLastCounter,0)
        (idempotent — an toàn khi nhiều tiến trình cùng flush 1 bảng)
```

## Worker chạy như nào

- `SyncTableCounterFlushWorker` là `BackgroundService` chuẩn .NET, nhưng **KHÔNG** đăng ký qua
  `WorkerRolesOptions`/`POS.Worker` như các worker khác trong repo (`PosFileImportWorker`,
  `PosSalesConsumerWorker`...). Lý do: `Channel` là bộ nhớ in-process, chỉ tồn tại trong đúng tiến
  trình đang ghi dữ liệu — mà ghi master data xảy ra ở **cả `POS.Api` lẫn `POS.Web`** (POS.Web có
  trang CRUD inject thẳng `ICentralMDRepository`). Nếu chạy worker này ở `POS.Worker` (tiến trình
  khác), Channel sẽ rỗng vĩnh viễn vì không ai `Track()` vào đó.
- Do đó `AddHostedService<SyncTableCounterFlushWorker>()` được đăng ký **trực tiếp** trong
  `POS.Api/Program.cs` và `POS.Web/Program.cs` — mỗi tiến trình tự chạy 1 bản instance riêng, tự
  flush Channel của chính nó.
- Vòng lặp: `PeriodicTimer(FlushIntervalSeconds)` (mặc định 5s, cấu hình qua appsettings section
  `SyncTableTracker`). Mỗi tick: nếu Channel không có event nào → bỏ qua, không gọi DB. Có event →
  gom `Dictionary<TableName, MaxCounter>` rồi gọi 1 câu UPDATE batch duy nhất cho cả lượt.
- Lỗi khi flush (mất kết nối DB tạm thời...) → log lỗi (`ILogger`), **không** làm chết worker —
  thử lại ở tick kế tiếp (event đã bị drain khỏi Channel của tick lỗi sẽ mất, nhưng giá trị counter
  tự "chữa lành" ở lần bump tiếp theo của bảng đó).
- **Giám sát (monitor)**: mỗi tick, worker ghi heartbeat vào Redis key
  `Worker:Heartbeat:SyncTableCounterFlush-{tên process}` (JSON, tái dùng DTO `WorkerHeartbeat` có
  sẵn — cùng dạng với heartbeat của `PosSalesConsumer`). TTL = `FlushIntervalSeconds × 3` lúc đang
  chạy, 300s khi dừng có chủ đích. **Chưa** tích hợp vào trang `/ops/health` (`HealthPage.razor`)
  hay `GET /api/common/CheckConnection` — 2 nơi đó hiện hard-code check 1 worker duy nhất qua config
  `HealthCheck:WorkerName`; muốn hiển thị worker này ở UI cần generalize config đó thành mảng (việc
  riêng, chưa làm).

## Update Counter như nào

Mỗi write-path bump `Counter` phải lấy được giá trị `Counter` **vừa ghi** (không phải tính lại), rồi
gọi `Track(tableName, counter)` ngay sau khi transaction ghi DB thành công:

- **Write qua Stored Procedure** (đa số các bảng): thêm tham số `OUTPUT` trả `Counter` vừa bump ra
  ngoài SP (ví dụ `@OutItemCounter bigint OUTPUT` trong `usp_Product_Save`), C# đọc lại giá trị
  `OUTPUT` sau `ExecuteAsync` rồi gọi `Track()`.
- **Write qua raw SQL trong C#** (một số bảng như `ItemBlock`): thêm `OUTPUT INSERTED.Counter` vào
  câu `UPDATE`/`INSERT`, đọc bằng `ExecuteScalarAsync<long>` thay vì `ExecuteAsync`.
- **Ghi theo batch/vòng lặp** (nhiều dòng trong 1 request, ví dụ khóa/mở khóa nhiều sản phẩm cùng
  lúc): gom giá trị `Max(Counter)` của cả batch trong vòng lặp, chỉ gọi `Track()` **1 lần** sau khi
  vòng lặp kết thúc — không track từng dòng riêng lẻ (giữ đúng tinh thần "batch, không chặn hot
  path").
- **Trường hợp đặc biệt — SP ủy quyền cho SP legacy production không sửa được** (`SalesPrice`,
  nhánh update của `usp_SetupSalePrice_Save` gọi `Setup_SalePrice_Get_ALL` đã "proven trên
  production"): SP legacy tự bump Counter nội bộ nhưng không có `OUTPUT`, và **không được sửa**
  (giữ nguyên logic update legacy theo `docs/sql/SetupSalePrice_Save.sql`). Giải pháp: đọc lại
  `SELECT MAX(Counter) FROM dbo.SalesPrice` **sau khi cả 2 nhánh insert/update đã chạy xong**, gán
  vào `@OutCounter OUTPUT` của SP bọc ngoài (`usp_SetupSalePrice_Save`). Đây là 1 câu SELECT phụ,
  chấp nhận được vì `SaveAsync` là thao tác lưu bulk (không phải hot path tần suất cao).
- SP `usp_SyncTableList_BulkUpdateCounter` chỉ ghi đè `POSLastCounter` khi giá trị mới **lớn hơn**
  giá trị hiện có (`WHERE Counter > ISNULL(POSLastCounter,0)`) — nên dù `Track()` gọi trùng, gọi
  không đúng thứ tự, hay 2 tiến trình cùng flush 1 bảng, kết quả cuối cùng vẫn đúng (chỉ giá trị lớn
  nhất thắng).

## Checklist — TableName đã/chưa triển khai Track()

| # | TableName | Write-path (Repository/SP) | Trạng thái |
|---|---|---|---|
| 1 | `Item` | `CentralMDRepository.CreateProductAsync` → SP `usp_Product_Save` (`@OutItemCounter` OUTPUT) | ✅ **Đã triển khai** (Pilot A) |
| 2 | `Barcodes` | `CentralMDRepository.CreateProductAsync` → SP `usp_Product_Save` (`@OutBarcodeCounter` OUTPUT) | ✅ **Đã triển khai** (Pilot A) |
| 3 | `ItemBlock` | `CentralMDRepository.SaveProductLockAsync` (raw SQL, `OUTPUT INSERTED.Counter`, track 1 lần/batch) | ✅ **Đã triển khai** (Pilot B) |
| 4 | `Branch` | `CentralMDRepository.CreateBranchAsync` / `UpdateBranchInfoAsync` | ⬜ Chưa triển khai |
| 5 | `Store` | `CentralMDRepository.CreateStoreAsync` / `UpdateStoreClosingMethodAsync` | ⬜ Chưa triển khai |
| 6 | `Staff` | `CentralMDRepository.CreateEmployeeAsync` / `ChangeEmployeePasswordAsync` | ⬜ Chưa triển khai |
| 7 | `POSTerminalBank` | `CentralMDRepository.SaveBankPOSAsync` | ⬜ Chưa triển khai |
| 8 | `SalesPrice` | `PriceRepository.SaveAsync`/`UpdatePriceAsync`/`SoftDeletePriceAsync` → SP `usp_SetupSalePrice_Save`/`usp_SalesPrice_UpdatePrice`/`usp_SalesPrice_SoftDelete` (`docs/sql/SalesPrice_AddCounterOutput.sql`) | ✅ **Đã triển khai** (Pilot C) |
| 9 | `SetupVoucher*` / `CpnVchBOM*` | `VoucherRepository` → SP `SetupVoucher_Save.sql` | ⬜ Chưa triển khai |
| 10 | `CpnVchBOMHeader/Line/IssueRule/CodeIssue/Store` | `CouponRepository` → SP `SetupCoupon_Save.sql` | ⬜ Chưa triển khai |
| 11 | `SpecialComboHeader` | `SpecialComboRepository` → SP `SpecialCombo_Save.sql` | ⬜ Chưa triển khai |
| 12 | `OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite` | `PromotionRepository.ApproveSetupAsync` → SP `usp_SetupPromotion_Approve` (bọc SP legacy `Setup_Promotion_Insert`, không sửa được — đọc lại `MAX(Counter)` sau khi ghi xong, kiểu Pilot C, xem `docs/sql/SetupPromotion_ApproveAndStatus.sql`) | ✅ **Đã triển khai** (Pilot D) |
| 13 | `OfferPriority` | Không tìm thấy — chưa khảo sát ra write-path nào (repo mới + legacy) ghi bảng này, chỉ có đọc (`GetOfferPriorityDetailAsync`) | ⬜ Không áp dụng — cần DBA/business xác nhận nguồn ghi dữ liệu trước khi quyết định có rollout hay không |

**Gap phát hiện ngoài phạm vi** (không thuộc checklist Track — cần quyết định riêng nếu rollout tới):
`CentralMDRepository.UpdatePosTerminalAsync` (bảng `POSTerminal`) và `InsertPOSDataSetupAsync` /
`UpdatePOSDataSetupAsync` (bảng `POSDataSetup`) hiện **không bump cột `Counter`** khi ghi — nếu muốn
2 bảng này tham gia sync tăng dần, phải thêm bump Counter trước, rồi mới thêm Track().

## Việc còn lại (chưa làm, chỉ ghi nhận)

- [ ] Rollout `Track()` cho 6 write-path còn lại ở bảng checklist trên (mục 4–7, 9–11), theo đúng
      mẫu Pilot A (SP + OUTPUT param), Pilot B (raw SQL + `OUTPUT INSERTED.Counter`), hoặc Pilot C
      (SP ủy quyền SP legacy không sửa được → đọc lại `MAX(Counter)` sau khi ghi xong).
- [ ] Xác nhận write-path thật của `OfferPriority` (mục 13) — hiện chưa tìm thấy chỗ nào ghi bảng
      này trong ứng dụng; cần hỏi DBA/business trước khi quyết định rollout hay bỏ qua vĩnh viễn.
- [ ] Generalize `HealthCheck:WorkerName` (POS.Web) từ 1 string → mảng, để `HealthPage.razor`
      (`/ops/health`) và `GET /api/common/CheckConnection` hiển thị được heartbeat của
      `SyncTableCounterFlush` cùng lúc với `PosSalesConsumer`.
- [ ] Quyết định có bump `Counter` cho `POSTerminal`/`POSDataSetup` hay không (gap nêu trên).
- [ ] Chạy trên CentralMD môi trường thật (chưa áp dụng ngoài sandbox phát triển):
      `docs/sql/SyncTableList_BulkUpdateCounter.sql`, bản sửa `docs/sql/Product_Save.sql` (thêm
      OUTPUT param), `docs/sql/SalesPrice_AddCounterOutput.sql` (chạy SAU
      `SetupSalePrice_Save.sql` + `SalesPrice_EditDelete*.sql`), và bản sửa
      `docs/sql/SetupPromotion_ApproveAndStatus.sql` (thêm 5 OUTPUT Counter cho
      `usp_SetupPromotion_Approve` — Pilot D).
- [ ] Verify runtime end-to-end trên môi trường có DB/Redis thật (sandbox hiện tại không verify
      được — chỉ mới build + `dotnet test tests/POS.ContractTests` xanh).

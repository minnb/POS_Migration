# Rpt_ReportSaleDetail_Insert — Chi tiết kỹ thuật

> Tổng hợp logic kỹ thuật, ràng buộc DB, và các "gotcha" phát hiện được khi rà soát code (2026-07-10).
> Tổng quan/inventory: `docs/worker/worker_status.md` mục 1.1 (#3). File này đào sâu **cơ chế cụ thể
> bên trong worker + SP** — không lặp lại nội dung file trên.

---

## 1. Vị trí file

| File | Vai trò |
|---|---|
| `src/POS.Infrastructure/Workers/Rpt_ReportSaleDetail_Insert.cs` | `BackgroundService` — polling timer, chu kỳ hardcode 1 phút |
| `src/POS.Infrastructure/Repositories/Sale/IRptReportSaleDetailRepository.cs` / `RptReportSaleDetailRepository.cs` | Gọi thẳng SP, không xử lý logic gì thêm |
| `docs/sql/database/CentralSale.sql` (UTF-16) | SP `Rpt_ReportSaleDetail_Insert` |
| `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton chia sẻ — worker này là 1 trong 2 writer (xem `docs/worker/WorkerHeartbeatService.md`) |

---

## 2. Logic kỹ thuật

### 2.1 Vòng lặp — full body (61 dòng, không rút gọn vì rất ngắn và quan trọng)

```csharp
private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);   // HARDCODE — KHÔNG đọc IConfiguration

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    healthState.Status = "Running";
    while (!stoppingToken.IsCancellationRequested)
    {
        var nextTick = DateTime.UtcNow.Add(Interval);      // tính mốc TRƯỚC khi gọi SP
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRptReportSaleDetailRepository>();
            var today = DateTime.Now.Date;
            await repo.ExecuteInsertAsync(today, today, stoppingToken);
            healthState.IncrementProcessed();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "...");
            healthState.Status = "Degraded";
            // Swallow so the loop continues.
        }

        var delay = nextTick - DateTime.UtcNow;
        if (delay > TimeSpan.Zero) await Task.Delay(delay, stoppingToken);
        // delay <= 0 → KHÔNG delay gì cả, vào tick tiếp theo NGAY LẬP TỨC
    }
}
```

**Xác nhận chính xác**: `Interval` là `private static readonly TimeSpan.FromMinutes(1)` — **hardcode thật, không đọc từ `IConfiguration` ở đâu cả**. Đây là gap đã được ghi nhận trước (`docs/worker/worker_status.md` mục 5, đề xuất #2) — không phải phát hiện mới, nhưng xác nhận lại bằng đọc code trực tiếp.

**1 scope DI mới mỗi tick** — resolve `IRptReportSaleDetailRepository` (scoped) từng lần, không giữ instance qua các tick.

**Lỗi 1 tick không giết worker** — catch tổng, log, set `Degraded`, **"Swallow so the loop continues"** (comment gốc trong code) — vòng lặp tự phục hồi ở tick kế nếu lỗi transient qua đi.

### 2.2 Gotcha kỹ thuật quan trọng nhất — cơ chế "tự chữa lành" khi SP chạy chậm biến mất

`nextTick` tính **TRƯỚC** khi `await repo.ExecuteInsertAsync(...)`. Nếu SP chạy **lâu hơn 60s**, biểu thức `delay = nextTick - DateTime.UtcNow` cho kết quả **âm** → điều kiện `if (delay > TimeSpan.Zero)` fail → **KHÔNG `Task.Delay` nào cả** → vòng lặp lập tức bắt đầu tick kế tiếp. Nói cách khác: chu kỳ "1 phút" **âm thầm suy biến thành vòng lặp liên tục sát nhau** (busy loop, gần như không nghỉ) ngay khi thời gian chạy SP ≥ 60 giây — không có cảnh báo, không có log riêng cho tình huống này.

Không có `Timer`/`PeriodicTimer` (khác các worker khác dùng `PeriodicTimer`) — đây là vòng `while` tuần tự đơn giản, nên **tự nó không thể chồng lấn (self-overlap)**: tại một thời điểm chỉ có tối đa 1 lệnh gọi SP đang chạy từ chính instance này. Nhưng điều đó không loại trừ overlap từ **nguồn khác** (xem mục 4).

---

## 3. Ràng buộc DB — SP `dbo.Rpt_ReportSaleDetail_Insert`

Gọi qua `RptReportSaleDetailRepository.ExecuteInsertAsync(fromDate, toDate, ct)` — Dapper `CommandDefinition`, **`CommandTimeout = 120s`** (gấp đôi chu kỳ 60s của worker — một lý do kỹ thuật khác khiến SP được phép chạy lâu hơn cả 1 chu kỳ trước khi ADO.NET tự timeout). Dùng `CentralSaleConnectionFactory` (kết nối cố định, không qua routing theo store).

### 3.1 Tham số `@FromDate`/`@ToDate` — worker luôn truyền `today, today`

```sql
DECLARE @StartDateTime DATETIME = CAST(@FromDate AS DATE);              -- hôm nay 00:00:00.000
DECLARE @EndDateTime   DATETIME = DATEADD(day, 1, CAST(@ToDate AS DATE)); -- ngày mai 00:00:00.000
...
WHERE H.CreatedDate BETWEEN @StartDateTime AND @EndDateTime   -- lọc theo CreatedDate, KHÔNG phải OrderDate
```

→ Cửa sổ lọc thực tế là **"toàn bộ hôm nay"** (từ 00:00:00 hôm nay tới 00:00:00 ngày mai), không phải "kể từ lần chạy trước" — nghĩa là mỗi lần chạy, SP quét lại toàn bộ đơn hàng phát sinh **trong ngày**, không phải delta kể từ tick trước.

### 3.2 Cơ chế chống trùng — 2 lớp

**Lớp 1 — Anti-join ứng dụng (KHÔNG giới hạn theo ngày!):**

```sql
IF OBJECT_ID('ReportSaleDetail') IS NOT NULL
BEGIN
    INSERT INTO #ReportSaleOrderCreated(OrderNo)
    SELECT DISTINCT D.OrderNo FROM ReportSaleDetail D (Nolock)   -- ⚠ KHÔNG có filter ngày nào ở đây
END
...
WHERE ... AND ISNULL(C.OrderNo,'') = ''   -- loại OrderNo đã có trong ReportSaleDetail (mọi thời điểm)
```

SP lấy **toàn bộ** `OrderNo` đã từng ghi vào `ReportSaleDetail` (không giới hạn ngày) để loại trừ — đây chính là cơ chế khiến việc gọi lặp lại mỗi 60s với cùng `@FromDate=@ToDate=today` **an toàn theo thiết kế**: đơn đã insert rồi sẽ không insert lại, chỉ đơn mới phát sinh trong ngày mới lọt qua.

**Lớp 2 — Ràng buộc UNIQUE ở DB (chỉ tạo khi bảng chưa tồn tại):**

```sql
IF OBJECT_ID('ReportSaleDetail') IS NULL
BEGIN
    SELECT ... INTO ReportSaleDetail FROM ...
    ...
    ALTER TABLE ReportSaleDetail ADD UNIQUE (OrderNo, [LineNo]);   -- ràng buộc duy nhất — chỉ thêm ở nhánh tạo bảng lần đầu
END
ELSE
BEGIN
    INSERT INTO ReportSaleDetail SELECT ... FROM ...   -- các lần sau
END
```

Bảng `ReportSaleDetail` được **tạo tự động lần chạy đầu tiên** (`SELECT ... INTO`), kèm nhiều index (`Orderdate`, `StoreNo`, `OrderNo`, combo `(StoreNo, OrderDate)`, `ItemNo`, `Barcode`, `SerialNo`, `ReturnedOrderNo`) và ràng buộc `UNIQUE (OrderNo, LineNo)`. Ràng buộc này là **backstop thật ở tầng DB** — nếu Lớp 1 (anti-join) vì lý do nào đó bị vượt qua (vd 2 tiến trình chạy đồng thời, xem mục 4), constraint violation sẽ chặn insert trùng thay vì để dữ liệu sai lặng lẽ vào bảng.

### 3.3 Nguồn dữ liệu & phạm vi join

Đọc: `TransLine` (JOIN `TransHeader`), `RPOSMasterData.dbo.Store`, `TransInputData` ×2 (DataType = `SOURCEBILL`/`HANDLINGSTAFF`), `TransDiscountEntry`, `TransDiscountCouponEntry`, và chính `ReportSaleDetail` (để anti-join). Ghi vào `ReportSaleDetail`. Toàn bộ JOIN dùng `(NOLOCK)` — dirty read được chấp nhận có chủ đích trong toàn bộ SP.

**Không có `BEGIN TRANSACTION`/`TRY...CATCH`/`sp_getapplock` nào trong SP** — xác nhận qua đọc toàn bộ nội dung. Statement `INSERT ... SELECT` tự atomic ở mức 1 câu lệnh (autocommit), nhưng không có transaction bao ngoài để rollback có chủ đích khi cần.

---

## 4. Gotchas (điểm cần lưu ý)

1. **⚠️ Interval hardcode 1 phút — KHÔNG đọc `IConfiguration`.** Muốn đổi chu kỳ phải sửa code + build lại, không thể chỉnh qua appsettings như các worker khác (`PosFileImportWorker` đọc `FileImport:PollIntervalSeconds`, `MasterDataZipGeneratorWorker` đọc `MasterDataZipGenerator:IntervalSeconds`). Đã ghi nhận trước ở `docs/worker/worker_status.md`, xác nhận lại ở đây.
2. **Chu kỳ "1 phút" âm thầm suy biến thành busy-loop khi SP chạy ≥ 60s** — không có cảnh báo riêng, không có log đặc biệt cho tình huống này (chỉ thấy gián tiếp qua log tick dồn dập nếu để ý thời gian). Xem cơ chế chi tiết ở mục 2.2.
3. **Hoàn toàn KHÔNG có cơ chế chống chạy chồng lấn (overlap) giữa các NGUỒN gọi khác nhau.** Bản thân vòng `while` của worker tự nó không thể tự chồng lấn (tuần tự), nhưng nếu có **nguồn thứ 2** gọi cùng SP đồng thời (vd: một engineer chạy tay `EXEC Rpt_ReportSaleDetail_Insert` trong SSMS trong lúc worker cũng đang tick, hoặc — về mặt giả định — 2 tiến trình `POS.Worker` cùng bật `WorkerRoles:EnableSqlReportWorker=true`) thì: (a) cả 2 đọc cùng 1 snapshot "OrderNo đã có" qua NOLOCK trước khi bên nào commit, (b) cả 2 tính ra tập "đơn mới" trùng nhau, (c) bên insert sau vi phạm `UNIQUE (OrderNo, LineNo)` và toàn bộ statement `INSERT...SELECT` của bên đó rollback (đơn hợp lệ không trùng trong cùng batch cũng bị cuốn theo, dù không mất dữ liệu vĩnh viễn vì sẽ được nhặt lại ở tick sau — nhưng tick đó coi như thất bại, `healthState.Status="Degraded"`). **Đây là rủi ro tiềm ẩn (latent), chưa xác nhận đã từng xảy ra thật** — hiện tại không tìm thấy cấu hình `replicas`/scale nào trong `docker-compose.yml`, nên rủi ro này thuộc dạng thiết kế, không phải sự cố đã quan sát.
4. **`@FromDate`/`@ToDate` lọc theo `TransHeader.CreatedDate`, không phải `OrderDate`** — cần hiểu đúng ý nghĩa: đây là "đơn được TẠO trong ngày" (thời điểm ghi vào DB), không nhất thiết trùng với ngày bán hàng thực tế trên chứng từ nếu 2 giá trị lệch nhau (vd nhập trễ).
5. **Anti-join Lớp 1 không giới hạn theo ngày** — mỗi tick, SP quét `DISTINCT OrderNo` từ **toàn bộ** `ReportSaleDetail` (không phải chỉ hôm nay) để loại trừ. Với bảng lớn dần theo thời gian, đây là điểm cần theo dõi hiệu năng dài hạn (không có bằng chứng đã là vấn đề thật, chỉ là điểm cần chú ý khi bảng phình to).
6. **`WorkerHealthState` bị ghi chung với `PosSalesConsumerWorker`** — heartbeat publish dưới tên cứng `"PosSalesConsumer"` (không phải tên worker này!) nghĩa là lỗi SQL report job có thể bị **hiển thị nhầm thành "mất kết nối RabbitMQ"** trên `/ops/health`. Đây là gotcha nghiêm trọng, phân tích đầy đủ + kịch bản cụ thể ở `docs/worker/WorkerHeartbeatService.md` §3.
7. **`CommandTimeout=120s` (gấp đôi chu kỳ 60s)** — về mặt thiết kế, SP được phép chạy dài hơn cả 1 chu kỳ trước khi ADO.NET tự hủy; kết hợp với gotcha #2, một SP chạy chậm dai dẳng có thể khiến worker liên tục tick sát nhau trong thời gian dài mà không có cơ chế nào tự ngắt.

---

## 5. Trạng thái verify

- Đã đọc toàn bộ `Rpt_ReportSaleDetail_Insert.cs` (61 dòng), `IRptReportSaleDetailRepository.cs`/`RptReportSaleDetailRepository.cs` (đầy đủ), và toàn bộ thân SP `dbo.Rpt_ReportSaleDetail_Insert` trong `docs/sql/database/CentralSale.sql` (đọc UTF-16 qua `Get-Content -Encoding Unicode -Raw`).
- **CHƯA chạy thử thật** (cần SQL Server CentralSale reachable, dữ liệu `TransHeader`/`TransLine` thật) — mọi phát hiện dựa trên đọc code/SP tĩnh. Đặc biệt "busy-loop khi SP chạy chậm" và "race điều kiện đa nguồn" chưa được tái hiện/quan sát trên hệ thống thật, chỉ suy luận từ đọc logic.

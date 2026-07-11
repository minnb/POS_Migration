# MasterDataZipGeneratorWorker — Chi tiết triển khai

> Tài liệu kỹ thuật chi tiết cho `MasterDataZipGeneratorWorker` (triển khai 2026-07-10, watermark
> chuyển từ Redis Hash sang cột DB `SyncTableList.ZipWatermarkCounter` ngày 2026-07-11). Tóm tắt
> vận hành/checklist go-live: xem **`docs/ROLLOUT.md` §O8**. Quyết định kiến trúc + lịch sử: xem
> **`.claude/rules/masterdata-sync.md`** mục "Worker sinh zip theo watermark + quarantine". File
> này tập trung vào **cơ chế thực thi cụ thể**: cấu hình, gọi SP nào theo thứ tự nào, sinh file gì,
> ghi ra thư mục nào.

---

## 1. Mục đích

Trước worker này, `SyncTableList.POSLastCounter` được cập nhật bất đồng bộ (bởi
`SyncTableCounterFlushWorker`) nhưng **không có gì đọc giá trị đó để tự động hành động** — master
data `.zip` chỉ được sinh khi:
- POS tự gọi `GET api/posblue/GetFileFromFTP?typeSync=ALL`, hoặc
- IT Ops bấm tay nút "Đồng bộ dữ liệu" trên `/catalog/pos-setup` (`PosMapPage.razor`).

`MasterDataZipGeneratorWorker` là **consumer đầu tiên** của tín hiệu `POSLastCounter`: định kỳ so
sánh counter hiện tại của các bảng được đánh dấu `IsOnlyChange=1` với 1 "watermark" lưu trong
**cột `SyncTableList.ZipWatermarkCounter`** (SQL Server, bền vững — xem mục 2 và
`docs/sql/SyncTableList_AddZipWatermark.sql`); bảng nào tăng counter → tự động sinh lại `.zip` cho
**mọi POS terminal đang bật**.

> **Lịch sử**: watermark ban đầu (2026-07-10) lưu ở Redis Hash `Worker:Watermark:MasterDataZip`. Vấn
> đề phát hiện: nếu Hash này bị mất (xóa tay, Redis restart mất persistence, evict theo
> `maxmemory-policy`...), code coi đó là "lần chạy đầu tiên" → seed watermark bằng counter **hiện
> tại** rồi bỏ qua generate — mọi thay đổi xảy ra trước khi mất key sẽ **vĩnh viễn không được
> trigger sinh zip** (watermark "nhảy cóc" qua thay đổi chưa xử lý), lỗi silent không throw
> exception. Ngày 2026-07-11 đã chuyển watermark sang cột DB `ZipWatermarkCounter` (bền vững, không
> thể mất do restart Redis) — chi tiết mục 2 và 6 bên dưới.

---

## 2. Sơ đồ luồng 1 chu kỳ (`RunCycleAsync`)

```
PeriodicTimer tick (mỗi IntervalSeconds, mặc định 300s)
 │
 ├─ Tạo 1 IServiceScope cho toàn bộ chu kỳ (KHÔNG tạo scope mới cho từng terminal)
 │   resolve: ISyncRepository, ISyncDataPosService, ICentralMDRepository, IRedisManager
 │
 ├─ [LOCK] IRedisManager.AcquireLockAsync("Worker:Lock:MasterDataZipGenerator", LockTtlMinutes)
 │    không lấy được lock → có instance khác đang chạy lượt này → log + return (bỏ qua tick)
 │
 ├─ [BƯỚC 1 — PHÁT HIỆN THAY ĐỔI]
 │   ISyncRepository.GetSyncTableCountersAsync("C")
 │     → gọi thẳng SP [dbo].[SyncTable_Get] @IsChange='C'   (KHÔNG qua Redis cache)
 │     → trả về DANH SÁCH BẢNG có SyncTableList.IsOnlyChange=1, kèm POSLastCounter VÀ
 │        ZipWatermarkCounter hiện tại (2 cột cùng 1 dòng SQL — xem mục 6)
 │   0 dòng → log Warning "không bảng nào IsOnlyChange=1" → return (worker rảnh, không phải lỗi)
 │
 ├─ [BƯỚC 2 — SO WATERMARK] (từ 2026-07-11: watermark là cột DB, không còn Redis)
 │   changedTables = tables.Where(t => t.POSLastCounter > t.ZipWatermarkCounter)
 │     Không có khái niệm "watermark chưa tồn tại" — migration
 │     docs/sql/SyncTableList_AddZipWatermark.sql backfill ZipWatermarkCounter = POSLastCounter 1 lần
 │     duy nhất, nên cột luôn có giá trị hợp lệ; không cần seed/skip-cycle-đầu trong runtime nữa.
 │   0 bảng đổi → log Debug, return (chu kỳ bình thường, đa số các lượt sẽ dừng ở đây)
 │
 ├─ [BƯỚC 3 — LẤY DANH SÁCH TERMINAL]
 │   ICentralMDRepository.GetPosTerminalListAsync(storeNo: null)  → TOÀN BỘ terminal (1 query, không
 │                                                                    lặp theo từng store)
 │   filter .Where(t => t.IsEnabled)   [tương đương POSTerminal.Status = 1]
 │
 ├─ [BƯỚC 4 — LỌC QUARANTINE]
 │   IRedisManager.HashGetAllAsync<int>("Worker:Quarantine:MasterDataZip")
 │   eligible = enabledTerminals bỏ terminal có FailedCount >= QuarantineThreshold (mặc định 3)
 │   → terminal bị quarantine KHÔNG được thử trong lượt này (chỉ log Warning 1 dòng tổng)
 │
 ├─ [BƯỚC 5 — SINH FILE SONG SONG]
 │   Parallel.ForEachAsync(eligible, MaxDegreeOfParallelism = MaxParallelTerminals mặc định 4)
 │     mỗi terminal → ISyncDataPosService.PushMasterDataChangeAsync(StoreNo, No)
 │       → xem chi tiết đầy đủ ở mục 4 (đây là nơi thực sự gọi SP1 (cached)/SP2 + ghi file + nén zip)
 │     thành công     → Interlocked++ successCount; HDEL quarantine[store:no] (reset về 0)
 │     ném MasterDataThrottleException → Interlocked++ failedCount; log Warning; KHÔNG tính quarantine
 │     lỗi khác        → Interlocked++ failedCount; HINCRBY quarantine[store:no] += 1; log Error
 │
 ├─ [BƯỚC 6 — ACK WATERMARK]
 │   failedCount == 0 (trong số terminal ĐÃ THỬ, không tính terminal bị quarantine bỏ qua)
 │     → snapshot = changedTables.ToDictionary(TableName, POSLastCounter đã đọc ở BƯỚC 1)
 │     → ISyncRepository.AckZipWatermarkAsync(snapshot) — TVP dbo.TVP_ZipWatermarkUpdate → SP
 │       dbo.usp_SyncTableList_BulkUpdateZipWatermark, UPDATE ZipWatermarkCounter = snapshot value
 │       (idempotent: chỉ ghi đè khi > giá trị hiện có) — KHÔNG re-read DB tại thời điểm ACK
 │   failedCount > 0
 │     → KHÔNG ACK → lượt sau tự động retry đúng các bảng đã đổi (không mất dữ liệu)
 │
 └─ WriteHeartbeat("Running" nếu failedCount==0, ngược lại "Degraded")
 finally: ReleaseLockAsync
```

---

## 3. Cấu hình đầy đủ

### 3.1 `POS.Worker/appsettings.json` → section `"MasterDataZipGenerator"`

| Key | Mặc định | Ý nghĩa |
|---|---|---|
| `IntervalSeconds` | `300` | Chu kỳ poll counter (giây). Tối thiểu ép về 30s trong code dù cấu hình thấp hơn. |
| `LockTtlMinutes` | `30` | TTL của distributed lock `Worker:Lock:MasterDataZipGenerator` — nếu 1 chu kỳ chạy lâu hơn giá trị này, về lý thuyết 1 instance khác có thể vào cùng lúc (không hỏng dữ liệu vì vẫn còn khóa per-terminal `ISyncFileLock` bên trong, chỉ lãng phí tài nguyên). |
| `MaxParallelTerminals` | `4` | Số terminal xử lý song song (`Parallel.ForEachAsync`). Cân nhắc cùng `MasterDataSync:MaxConcurrentGeneration` (giới hạn cụm) để tránh bị throttle liên tục. |
| `QuarantineThreshold` | `3` | Số lần lỗi liên tiếp trước khi 1 terminal bị bỏ qua ở các lượt sau. |

> **Đã xóa (2026-07-11)**: `SeedWatermarkOnFirstRun` — không còn cần thiết vì watermark giờ là cột
> DB `ZipWatermarkCounter`, luôn có giá trị hợp lệ nhờ backfill 1 lần trong migration script (xem
> mục 1 và 6), không còn khái niệm "watermark chưa tồn tại" cần seed ở runtime.

### 3.2 `POS.Worker/appsettings.json` → section `"MasterDataSync"` (dùng chung với luồng generate hiện có — POS.Api/POS.Web)

| Key | Giá trị | Ý nghĩa |
|---|---|---|
| `SqlCommandTimeoutSeconds` | `600` | Timeout SP1/SP2. |
| `KeepZipDays` | `2` | Lưới an toàn dọn file mồ côi. |
| `DateInZipName` | `true` | Nhúng ngày+giờ vào tên zip. |
| `ZipCompressionLevel` | `"Fastest"` | Mức nén — Fastest nhanh hơn Optimal 2–5×. |
| `BatchSizePerFile` | `10000` | Số dòng tối đa mỗi file `.txt` trước khi tách batch mới. |
| `MaxParallelTables` | `4` | Số bảng SP2 chạy song song **trong 1 lần generate cho 1 terminal**. |
| `MaxConcurrentGeneration` | `3` | Số lượt generate chạy đồng thời **trên toàn cụm** (Redis ZSET throttle) — dùng chung bởi CẢ `GetFileFromFTP`, nút "Đồng bộ dữ liệu", VÀ worker này. |
| `ThrottleStaleAfterSeconds` | `600` | Slot throttle "mồ côi" (process crash giữa chừng) tự dọn sau bao lâu. |

### 3.3 `POS.Worker/appsettings.json` → `"AppSettings":"FtpRootPath"`

```json
"AppSettings": { "FtpRootPath": "D:\\ROOT\\FTPBLUEPOS" }
```

**Bắt buộc** trỏ đúng thư mục vật lý ghi được — nếu bỏ trống, `MapFtpPath` âm thầm fallback về
`AppContext.BaseDirectory\FTPBLUEPOS` (cạnh file thực thi worker) và POS **sẽ không bao giờ thấy
file được sinh ra**.

### 3.4 `POS.Worker/appsettings.json` → `"WorkerRoles":"EnableMasterDataZipGenerator"`

```json
"WorkerRoles": { "EnableMasterDataZipGenerator": false }
```

Mặc định `false` (opt-in). Chỉ `AddHostedService<MasterDataZipGeneratorWorker>()` khi cờ này
`true` (`POS.Worker/Program.cs`).

### 3.5 Điều kiện DB bắt buộc (không cấu hình trong appsettings — DBA thao tác trực tiếp trên CentralMD)

```sql
UPDATE dbo.SyncTableList SET IsOnlyChange = 1 WHERE TableName IN ('Item', 'Barcodes', 'SalesPrice');
```

Nếu không bảng nào được đánh dấu `IsOnlyChange=1`, `SP [SyncTable_Get] @IsChange='C'` luôn trả 0
dòng → worker luôn ở trạng thái "không có gì để làm" (KHÔNG phải lỗi, chỉ là chưa cấu hình).

---

## 4. Chi tiết gọi Stored Procedure

Có **2 lời gọi SP1 khác nhau, mục đích khác nhau** — không nhầm lẫn:

| Gọi bởi | Method | Cache Redis? | Mục đích |
|---|---|---|---|
| `MasterDataZipGeneratorWorker.RunCycleAsync` (mỗi tick) | `ISyncRepository.GetSyncTableCountersAsync("C")` | **KHÔNG** — luôn query DB trực tiếp | Đọc `POSLastCounter` **mới nhất** để so watermark — nếu dùng bản có cache (TTL 1h) sẽ phát hiện thay đổi trễ tới 1 giờ |
| `MasterDataSyncService.EnsureMasterDataFileAsync` (bên trong `PushMasterDataChangeAsync`, mỗi terminal) | `ISyncRepository.GetSyncTablesAsync("C")` | **CÓ** — Redis key `MD:SyncTableList:C`, TTL 3600s | Lấy metadata bảng (`Action`, `IsSingleFile`, `ColumnFilter`...) để build file — metadata này hiếm đổi, cache OK |

### 4.1 SP1 — `[dbo].[SyncTable_Get] @IsChange='C'`

```sql
EXEC [dbo].[SyncTable_Get] @IsChange = 'C'
```

Trả về (nhánh `IF @IsChange = 'C'` trong SP, đã thêm `ZipWatermarkCounter` ngày 2026-07-11 — xem
`docs/sql/SyncTableList_AddZipWatermark.sql`):
```sql
SELECT TableName, POSLastCounter, ISNULL(ZipWatermarkCounter,0) AS ZipWatermarkCounter,
       [Procedure], [OrderByName], IsByStore,
       ISNULL(ColumnFilter,'') ColumnFilter, GroupName,
       ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,
       ISNULL(IsSingleFile,0) AS IsSingleFile,
       ISNULL(Action,'TRUNC-INSERT') AS Action
FROM SyncTableList (Nolock)
WHERE [Enabled] = 1 AND IsOnlyChange = 1
```

Map vào `SyncTableInfo` (`POS.Common.Dtos.DataSync`) — nhánh `'A'`/`'W'` KHÔNG SELECT
`ZipWatermarkCounter` (Dapper để mặc định `0`, không sao vì 2 nhánh đó không dùng property này).

### 4.2 SP2 — `[dbo].[SyncGetDataByTable]` (1 lần/bảng, bên trong generate)

Gọi bởi `ISyncRepository.StreamTableToFilesAsync` — **STREAM** `SqlDataReader`
(`CommandBehavior.SequentialAccess`), KHÔNG nạp `DataTable`/RAM:

```csharp
cmd.Parameters.AddWithValue("@TableName", table.TableName);
cmd.Parameters.AddWithValue("@ColumnOrderBy", columnOrderBy);   // "" cho mọi bảng hiện tại
cmd.Parameters.AddWithValue("@POSLastCounter", posLastCounter); // = 0 (xem mục 4.3)
cmd.Parameters.AddWithValue("@FilterColumn", filterColumn);     // "" nếu bảng không IsByStore
cmd.Parameters.AddWithValue("@FilterValue", filterValue);       // "" nếu bảng không IsByStore
```

### 4.3 Điểm quan trọng — Worker này LUÔN lấy FULL DATA của bảng, KHÔNG phải chỉ dòng thay đổi

`PushMasterDataChangeAsync` đặt `TypeSync = "ALL"` (xem mục 5). Trong
`MasterDataSyncService.GenerateAndPublishAsync`:

```csharp
var counter = req.TypeSync == "ALL" || entry.Table.IsFirstDataAll ? 0L : entry.Table.POSLastCounter;
```

Vì `TypeSync == "ALL"` luôn đúng ở đây → `@POSLastCounter` luôn truyền `0` cho SP2 → SP2 trả **toàn
bộ dòng của bảng** (không lọc theo Counter). Nói cách khác: **watermark chỉ dùng để quyết định CÓ
NÊN sinh lại zip hay không** (bảng nào đổi → kích hoạt); một khi đã quyết định sinh, nội dung sinh
ra là **snapshot đầy đủ của bảng đó tại thời điểm generate**, không phải delta.

---

## 5. Chi tiết sinh file (bên trong `PushMasterDataChangeAsync` → `EnsureMasterDataFileAsync`)

`ISyncDataPosService.PushMasterDataChangeAsync(siteCode, posTerminal)` build request:

```csharp
var req = new GetMasterDataFileRequest
{
    SiteCode = siteCode,
    PosTerminal = posTerminal,
    FolderFile = $"{siteCode}/{posTerminal}",
    PathSync = "SyncDataPos/POS/CHANGE",
    TypeSync = "ALL",
    IsChangeMode = "C",
    TargetDir = MapFtpPath("SyncDataPos/POS/CHANGE/{siteCode}/{posTerminal}"),
    ForceRegenerate = true
};
```

Sau đó gọi `IMasterDataSyncService.EnsureMasterDataFileAsync(req)` — **cùng 1 hàm** mà
`GetFileFromFTP?typeSync=ALL` và nút "Đồng bộ dữ liệu" đang dùng (không có code sinh file riêng cho
worker).

### 5.1 Thư mục đích (nơi zip thật sự nằm)

```
{AppSettings:FtpRootPath}\SyncDataPos\POS\CHANGE\{siteCode}\{posTerminal}\
```

Ví dụ với `FtpRootPath = D:\ROOT\FTPBLUEPOS`, site `2018`, terminal `201801`:

```
D:\ROOT\FTPBLUEPOS\SyncDataPos\POS\CHANGE\2018\201801\
```

Đây **chính là** thư mục mà endpoint `GET api/posblue/GetFileFromFTP?typeSync=Change` liệt kê file
khi POS hỏi — worker chỉ cần ghi đúng chỗ này, không cần gọi API nào thêm.

### 5.2 Các bước sinh file (bên trong `EnsureMasterDataFileAsync`)

1. **Kiểm tra short-circuit "đã có zip hôm nay"** — vì `req.ForceRegenerate = true` nên bước này
   **luôn bị bỏ qua** cho worker (xem `.claude/rules/masterdata-sync.md` — short-circuit này vốn đã
   gần như dead code với luồng cũ vì tên zip nhúng mili-giây, nhưng cờ `ForceRegenerate` làm việc bỏ
   qua này **tường minh** thay vì phụ thuộc hành vi ngẫu nhiên).
2. **Giữ slot throttle cụm** — `IRedisService.TryAcquireSlotAsync("MD:CreateMasterData:Slots", ...,
   MaxConcurrentGeneration=3, ...)`. Không giữ được → ném `MasterDataThrottleException` (worker bắt
   riêng, không tính vào quarantine — xem mục 2 Bước 5).
3. **Khóa per-terminal** — `ISyncFileLock.AcquireAsync("ALL_{siteCode}_{posTerminal}")` (keyed
   `SemaphoreSlim`, chống 2 tiến trình cùng sinh trùng zip cho cùng 1 terminal).
4. **Tạo thư mục tạm**: `{TargetDir}\_tmp_{guid}\`
   - `_tmp_{guid}\common\` — chứa file `.txt` của các bảng **KHÔNG** `IsSingleFile`
   - `_tmp_{guid}\{TableName}\` — 1 thư mục riêng cho mỗi bảng có `IsSingleFile=1`
5. **Với mỗi bảng** (song song tối đa `MaxParallelTables=4`, mỗi bảng dùng `SqlConnection` riêng):
   - Gọi SP2 (mục 4.2), stream từng dòng ghi vào file `.txt` dạng **JSON envelope**:
     ```json
     {
       "FileName": "2018_Item_ab12cd34_1_001.txt",
       "TableName": "Item",
       "Action": "TRUNC-INSERT",
       "ProcedureName": "...",
       "ProcessID": "2018xyz...",
       "Data": [ { "col1": "...", "col2": "..." }, ... ]
     }
     ```
   - Tên file batch: `{SiteCode}_{TableName}_{rnd}_{tableIndex}_{batchNo:D3}.txt` (`rnd` random 1
     lần/bảng để cùng prefix; `batchNo` zero-pad 3 chữ số để sort đúng thứ tự).
   - Tách file mới mỗi khi đạt `BatchSizePerFile` dòng (mặc định 10000).
   - **Action theo batch** (vì `IsChangeMode="C"` không phải `"W"`):
     `batchNo==1 → Action từ SyncTableList (mặc định "TRUNC-INSERT")`, `batchNo>1 → "INSERT"` (nối
     tiếp, tránh truncate lặp lại).
   - Bảng SP2 trả 0 dòng → **không tạo file** cho bảng đó (tránh POS nhận `TRUNC-INSERT` +
     `Data:[]` rỗng → xóa nhầm dữ liệu POS).
6. **Nén zip** — mỗi thư mục (`common\` và từng `{TableName}\`) được nén riêng thành 1 file zip tạm
   (`{guid}.zip`, `ZipCompressionLevel=Fastest`), rồi `File.Move(overwrite:true)` **atomic** sang
   tên chính thức — POS không bao giờ tải phải file đang ghi dở.
   - Zip "common" (gộp mọi bảng không `IsSingleFile`):
     `{SiteCode}_ALL_{PosTerminal}_{yyyyMMdd}_{HHmmssfff}.zip`
   - Zip riêng cho bảng `IsSingleFile=1`:
     `{SiteCode}_ALL_{PosTerminal}_{TableName}_{yyyyMMdd}_{HHmmssfff}.zip`
7. **SHA-256 companion** — sau khi publish mỗi zip, tự tính SHA-256 và ghi
   `{tên_zip}.zip.sha256` (text, hex lowercase) cùng thư mục.
8. **Dọn zip cũ cùng prefix** (`CleanupSiblingZips`) — xóa mọi zip khác cùng
   `{SiteCode}_ALL_{PosTerminal}_*` không nằm trong bộ zip vừa publish lượt này (kể cả zip do worker
   sinh lần trước, hoặc do nút "Đồng bộ dữ liệu" sinh — 2 luồng dùng chung thư mục, luôn chỉ giữ lại
   bản mới nhất).
9. **Dọn thư mục tạm** `_tmp_{guid}\` trong `finally` — kể cả khi lỗi giữa chừng (không publish file
   thiếu bảng).
10. **Nhả slot throttle** trong `finally` (dùng `CancellationToken.None` để chắc chắn nhả được kể
    cả khi request bị hủy).

### 5.3 Ví dụ cây thư mục thật sau khi chạy xong (site 2018, terminal 201801)

```
D:\ROOT\FTPBLUEPOS\SyncDataPos\POS\CHANGE\2018\201801\
├── 2018_ALL_201801_20260710_143205123.zip           ← common (mọi bảng IsSingleFile=0)
├── 2018_ALL_201801_20260710_143205123.zip.sha256
├── 2018_ALL_201801_Item_20260710_143207891.zip       ← bảng Item (IsSingleFile=1)
├── 2018_ALL_201801_Item_20260710_143207891.zip.sha256
├── 2018_ALL_201801_SalesPrice_20260710_143209456.zip ← bảng SalesPrice (IsSingleFile=1)
└── 2018_ALL_201801_SalesPrice_20260710_143209456.zip.sha256
```

(Tên file thật đổi mỗi lần generate vì nhúng `HHmmssfff` — số lượng zip phụ thuộc số bảng
`IsSingleFile=1` đang cấu hình trong `SyncTableList`.)

---

## 6. Watermark (cột DB) + Redis key — tổng hợp toàn bộ

### 6.1 Watermark — `SyncTableList.ZipWatermarkCounter` (SQL Server, KHÔNG phải Redis)

> **Thay đổi 2026-07-11**: watermark chuyển từ Redis Hash `Worker:Watermark:MasterDataZip` sang cột
> `bigint NULL DEFAULT 0` trên chính bảng `dbo.SyncTableList` (cùng dòng với `POSLastCounter`) — xem
> `docs/sql/SyncTableList_AddZipWatermark.sql`. Lý do: Redis Hash có thể mất (xóa tay, restart mất
> persistence, evict) → mất dấu vết thay đổi vĩnh viễn (xem cảnh báo ở mục 1). Cột SQL Server bền
> vững qua restart Redis/Worker, loại bỏ hẳn lớp lỗi đó.

- **Ghi bởi**: DUY NHẤT `MasterDataZipGeneratorWorker`, qua `ISyncRepository.AckZipWatermarkAsync`
  → TVP `dbo.TVP_ZipWatermarkUpdate` → SP `dbo.usp_SyncTableList_BulkUpdateZipWatermark` (idempotent:
  `WHERE Counter > ISNULL(ZipWatermarkCounter,0)`).
- **Đọc bởi**: cùng 1 câu SELECT SP1 `[SyncTable_Get] @IsChange='C'` trả `POSLastCounter` VÀ
  `ZipWatermarkCounter` trong cùng 1 dòng — không cần round-trip Redis riêng.
- **KHÔNG đụng** `POSLastCounter`/SP `usp_SyncTableList_BulkUpdateCounter` (ghi bởi
  `SyncTableCounterFlushWorker`, chạy mỗi 5s từ cả `POS.Api`/`POS.Web`) — 2 cột/2 SP hoàn toàn tách
  biệt, tránh đụng vào hot path ghi dữ liệu đang chạy ổn định.
- **Backfill 1 lần** trong migration: `ZipWatermarkCounter = ISNULL(POSLastCounter,0)` — cycle đầu
  tiên sau khi deploy code mới thấy "không đổi gì", tương đương hành vi seed cũ nhưng không cần
  logic seed trong C#.

**Lệnh vận hành thường dùng (SQL, thay cho `redis-cli HGETALL` trước đây):**
```sql
SELECT TableName, POSLastCounter, ZipWatermarkCounter, LastUpdated
FROM dbo.SyncTableList
WHERE IsOnlyChange = 1
ORDER BY TableName;
```

### 6.2 Redis key — chỉ còn Lock/Quarantine/Heartbeat + cache metadata (không đổi)

| Key | Loại | TTL | Ghi bởi | Ý nghĩa |
|---|---|---|---|---|
| `Worker:Quarantine:MasterDataZip` | Hash (field=`{StoreNo}:{No}`, value=FailedCount) | Không hết hạn | `MasterDataZipGeneratorWorker` | Số lần lỗi liên tiếp mỗi terminal |
| `Worker:Lock:MasterDataZipGenerator` | String (distributed lock, SET NX PX) | `LockTtlMinutes` (30 phút) | `MasterDataZipGeneratorWorker` | Đảm bảo chỉ 1 instance chạy 1 lượt |
| `Worker:Heartbeat:MasterDataZipGenerator` | String (JSON `WorkerHeartbeat`) | ~`LockTtlMinutes` lúc Running, 300s lúc Stopped | `MasterDataZipGeneratorWorker` | Giám sát worker còn sống |
| `MD:SyncTableList:C` | String (JSON `List<SyncTableInfo>`) | 3600s | `SyncRepository.GetSyncTablesAsync("C")` | Cache metadata bảng (Action/IsSingleFile...) dùng khi generate |
| `MD:CreateMasterData:Slots` | Sorted Set (sliding-window throttle) | tự dọn slot quá `ThrottleStaleAfterSeconds` | `MasterDataSyncService` (dùng chung mọi nguồn generate) | Giới hạn `MaxConcurrentGeneration` lượt generate đồng thời toàn cụm |

> `Worker:Watermark:MasterDataZip` (Redis Hash cũ) **đã retired** — code không còn tham chiếu key
> này. Không cần chủ động xóa ngay sau khi deploy (xem rollout note trong
> `.claude/rules/masterdata-sync.md`); dọn tay bằng `redis-cli DEL Worker:Watermark:MasterDataZip`
> sau khi xác nhận bản mới chạy ổn định qua vài cycle.

**Lệnh vận hành thường dùng:**
```bash
redis-cli HGETALL Worker:Quarantine:MasterDataZip
redis-cli HDEL Worker:Quarantine:MasterDataZip 2018:201801   # gỡ quarantine 1 terminal sau khi đã sửa lỗi
redis-cli GET Worker:Heartbeat:MasterDataZipGenerator
redis-cli DEL MD:SyncTableList:C                              # sau khi DBA đổi cấu hình SyncTableList
```

---

## 7. Quarantine & ACK — quy tắc chính xác

- Terminal **thành công** → `HDEL` khỏi quarantine (reset `FailedCount` về 0 — lần fail tiếp theo
  tính lại từ đầu).
- Terminal **lỗi nghiệp vụ** (`result.Success == false`) hoặc **exception** (trừ
  `MasterDataThrottleException`) → `FailedCount += 1`. Đạt `QuarantineThreshold` (3) → các lượt
  **sau** sẽ bỏ qua hẳn terminal này (không thử, chỉ log Warning tổng số bị bỏ qua).
- `MasterDataThrottleException` (hết slot throttle cụm — lỗi tài nguyên chung, không phải lỗi của
  riêng terminal) → tính vào `failedCount` của lượt (chặn ACK watermark lượt này) nhưng **KHÔNG**
  cộng vào quarantine của terminal đó.
- **ACK watermark**: chỉ tịnh tiến khi `failedCount == 0` trong số terminal **đã thử** lượt này
  (không tính terminal bị quarantine bỏ qua từ đầu). Vì `POSLastCounter`/`ZipWatermarkCounter` là
  giá trị global/bảng (không phải per-terminal), không thể "ACK một phần" — 1 terminal active lỗi
  vẫn giữ nguyên watermark để lượt sau tự retry đúng các bảng đã đổi. Giá trị ACK luôn là
  `POSLastCounter` đã snapshot lúc ĐẦU cycle (không re-read DB lúc ACK — xem mục 6.1).
- **Gap đã biết** (ghi trong `.claude/rules/masterdata-sync.md`): terminal bị quarantine rồi gỡ thủ
  công có thể đã bỏ lỡ dữ liệu (watermark tịnh tiến trong lúc nó bị loại). Khắc phục: sau khi `HDEL`
  quarantine, **bấm lại nút "Đồng bộ dữ liệu"** cho đúng terminal đó (full resync không phụ thuộc
  watermark) — không cần code thêm, cơ chế full-resync đã có sẵn.

---

## 8. Vị trí file mã nguồn liên quan

| Layer | File | Namespace |
|---|---|---|
| Worker | `POS.Worker/Workers/MasterDataZipGeneratorWorker.cs` | `POS.Worker.Workers` |
| Worker options | `POS.Worker/Workers/MasterDataZipGeneratorOptions.cs` (đã xóa `SeedWatermarkOnFirstRun`) | `POS.Worker.Workers` |
| App service | `POS.Application/Features/DataSync/ISyncDataPosService.cs` — `PushMasterDataChangeAsync` | `POS.Application.Features.DataSync` |
| App service | `POS.Application/Features/DataSync/MasterDataSyncService.cs` — `EnsureMasterDataFileAsync` (đã thêm guard `ForceRegenerate`) | `POS.Application.Features.DataSync` |
| Infra repo | `POS.Infrastructure/Repositories/DataSync/SyncRepository.cs` — `GetSyncTableCountersAsync` (không cache), `AckZipWatermarkAsync` (mới, 2026-07-11) | `POS.Infrastructure.Repositories` |
| Infra repo interface | `POS.Infrastructure/Repositories/DataSync/ISyncRepository.cs` — `+AckZipWatermarkAsync` | `POS.Infrastructure.Repositories.Interfaces` |
| DTO | `POS.Common/Dtos/DataSync/GetMasterDataFileRequest.cs` — `+ForceRegenerate` | `POS.Common.Dtos.DataSync` |
| DTO | `POS.Common/Dtos/DataSync/SyncTableInfo.cs` — `+ZipWatermarkCounter` (mới, 2026-07-11) | `POS.Common.Dtos.DataSync` |
| DB script | `docs/sql/SyncTableList_AddZipWatermark.sql` — cột `ZipWatermarkCounter` + TVP `TVP_ZipWatermarkUpdate` + SP `usp_SyncTableList_BulkUpdateZipWatermark` + SP `SyncTable_Get` (thêm SELECT nhánh `'C'`) | CentralMD (áp dụng thủ công, order 850, `docs/sql/manifest.json`) |
| DI | `POS.Worker/Program.cs` — `AddApplication()`, `Configure<MasterDataZipGeneratorOptions>`, `AddHostedService` có điều kiện | — |
| Config | `POS.Worker/appsettings.json`, `appsettings.Production.json` (đã xóa `SeedWatermarkOnFirstRun`) | — |
| Test guardrail | `tests/POS.ContractTests/DependencyInjectionTests.cs` — `MasterDataZipGeneratorWorker_dependencies_are_registered` | `POS.ContractTests` |

---

## 9. Trạng thái verify

- ✅ `dotnet build POS.slnx` — 0 Warning, 0 Error (verify lại 2026-07-11 sau khi chuyển watermark
  sang cột DB).
- ✅ `dotnet test tests/POS.ContractTests` — 45/45 pass (verify lại 2026-07-11).
- ⚠️ **CHƯA verify end-to-end** trên môi trường thật (cần SQL Server CentralMD, Redis,
  `FtpRootPath` ghi được) — sandbox phát triển không có các hạ tầng này. Đặc biệt **chưa verify**:
  chạy `docs/sql/SyncTableList_AddZipWatermark.sql` trên CentralMD thật, xác nhận backfill đúng,
  và chạy 1 cycle worker thật để xác nhận `ZipWatermarkCounter` được ACK đúng giá trị snapshot.
  Checklist verify thủ công đầy đủ: xem `docs/ROLLOUT.md` §O8.

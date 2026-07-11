# Sinh file master data .zip cho POS (Sync Master Data)

> Tính năng cho máy POS đầu ngày tải master data đã nén. Endpoint giữ **contract cũ** (5.000 POS không đổi).

## Luồng

```
GET api/posblue/GetFileFromFTP?...&typeSync=ALL
  → SyncDataPosController (nhánh typeSync=="ALL")
    → IMasterDataSyncService.EnsureMasterDataFileAsync   (POS.Application/Features/DataSync)
        → ISyncRepository.GetSyncTablesAsync             (SP1 [SyncTable_Get] @IsChange='A', trả kèm Action theo bảng)
        → ISyncRepository.StreamTableToFileAsync         (SP2 [SyncGetDataByTable], STREAM SqlDataReader)
        → IFileArchiveService.CreateZipFromDirectory     (nén thư mục tạm)
        → ISyncFileLock                                  (keyed SemaphoreSlim chống sinh trùng)
    → GetFileFromServerApiAsync → trả List<PathFileAPIModel>   (GIỮ NGUYÊN contract)
GET api/posblue/DowloadFileStream?filePath=...  → stream thủ công application/x-zip-compressed (FileShare.Read)
                                                  + ghi log DB dbo.MasterDataDownloadLog (Success/Aborted/Error)
GET api/posblue/DeleteFileFromFTP?filePath=...  → xóa file vật lý (+ .sha256 companion)
                                                  + cập nhật DeletedAt/DeleteStatus vào đúng bản ghi download log
```

> **Download logging**: `DowloadFileStream` stream thủ công (`CopyToAsync(Response.Body, RequestAborted)`) để biết
> kết quả best-effort: `Success` = gửi đủ byte không bị ngắt (KHÔNG đảm bảo POS lưu xong), `Aborted` = client ngắt,
> `Error`. Ghi 1 dòng `dbo.MasterDataDownloadLog` qua `IMasterDataSyncService.LogDownloadAsync` (fail-safe, nuốt lỗi
> nếu bảng chưa tạo). **KHÔNG tự xóa file** sau download (giữ cache ngày; dọn bằng daily-refresh + KeepZipDays).
> Script bảng: `docs/sql/MasterDataDownloadLog.sql`. Log với `ct=CancellationToken.None` để ghi được cả khi client ngắt.

> **Delete logging** (cập nhật 2026-07-09): sau khi POS xử lý xong file tải về, gọi `DeleteFileFromFTP` xóa
> file gốc trên FTP — controller gọi `IMasterDataSyncService.LogDeleteAsync(fileName, status, ct)` để cập nhật
> `DeletedAt`/`DeleteStatus` (`'Success'` | `'Failed'`) vào **đúng bản ghi download log tương ứng** (KHÔNG tạo
> dòng mới). Tìm bản ghi qua `ISyncRepository.UpdateDeleteLogAsync`: khớp `FileName` (+`SiteCode`/`PosTerminal`
> parse best-effort từ tên file, dùng chung helper `ParseSiteAndTerminal` với `LogDownloadAsync`) và lấy dòng
> `DownloadedAt` **mới nhất** (1 câu UPDATE với subquery `TOP 1 ORDER BY DownloadedAt DESC`, tránh update nhầm
> nhiều dòng/race điều kiện). Log `'Failed'` cho cả nhánh exception lẫn nhánh file-không-tồn-tại (quyết định có
> chủ đích — cả 2 đều là 1 lượt xóa không thành công POS cần biết); nhánh path-traversal-blocked KHÔNG log (không
> phải luồng xóa hợp lệ). Fail-safe giống `LogDownloadAsync`: nếu 2 cột `DeletedAt`/`DeleteStatus` chưa được
> ALTER TABLE (script gộp chung `docs/sql/MasterDataDownloadLog.sql`), lỗi bị nuốt, không phá luồng xóa file.

## Quyết định kiến trúc (giữ chuẩn cho session sau)

- **Response GIỮ NGUYÊN `List<PathFileAPIModel>`** — KHÔNG đổi sang shape mới. Service chỉ sinh file, controller
  re-list qua `GetFileFromServerApiAsync` để build response như cũ. `GetMasterDataFileResult` chỉ dùng nội bộ/log.
- **Định dạng file trong zip = JSON envelope `SyncTableList`** (bám `DataRawService.CreateFileSODFakeAsync`):
  `{ FileName, TableName, Action, ProcedureName, ProcessID, Data:[rows] }`, UTF-8 (`Encoding.UTF8`).
  Stream mảng `Data` từ `SqlDataReader` (`SequentialAccess`) bằng Newtonsoft
  `JsonTextWriter` — **KHÔNG** nạp DataTable/RAM. `// TODO: confirm format vs POS parser`.
- **Chia batch file `.txt`** (`MasterDataSync:BatchSizePerFile`, mặc định 10000): bảng lớn tách nhiều file
  `{site}_{table}_{rnd}_{idx}_{batchNo:D3}.txt` (random tạo 1 lần/bảng để cùng prefix + sort đúng). **Batch đầu
  `Action="TRUNC-INSERT"`, các batch sau `Action="INSERT"`** (append) → POS truncate 1 lần rồi nối, tránh mất dữ liệu.
  Vẫn stream từng dòng (constant memory). `BatchSizePerFile <= 0` → không tách (1 file/bảng).
- **Tách nhiều zip theo `SyncTableList.IsSingleFile`** (cập nhật 2026-07-08, fix timeout download
  POS với site nhiều dữ liệu): bảng có `IsSingleFile=1` → đóng gói **riêng 1 zip/bảng**
  (`{siteCode}_{typeSync}_{posTerminal}_{TableName}_{yyyyMMdd}_{HHmmssfff}.zip`); các bảng còn lại
  (`IsSingleFile=0`, mặc định) → gom chung 1 zip **"common"** (tên như cũ, không đổi khi chưa có
  bảng nào bật cờ → an toàn rollout). `EnsureMasterDataFileAsync` trả `List<GetMasterDataFileResult>`
  (1 phần tử/zip đã publish) thay vì 1 kết quả đơn — DTO này không phải HTTP body nên không ảnh
  hưởng contract JSON với POS. `GetFileFromServerApiAsync` không cần sửa vì đã liệt kê **mọi**
  `*.zip` trong thư mục đích. Cột `IsSingleFile` + SP `[SyncTable_Get]` cập nhật qua
  `docs/sql/SyncTableList_AddIsSingleFile.sql` (áp dụng thủ công trên CentralMD, DBA tự
  `UPDATE ... SET IsSingleFile=1` cho bảng cần tách — không cấu hình trong appsettings/code).
  All-or-nothing: nếu **bất kỳ** zip nào trong lượt (common + từng bảng single) thiếu/không hợp lệ
  hôm nay → regenerate **toàn bộ** lượt đó (không regenerate lẻ từng zip).
- **Tên zip**: `{siteCode}_{typeSync}_{posTerminal}_{yyyyMMdd}.zip` → sang ngày mới tự sinh lại (daily-refresh).
- **Atomic publish**: ghi `{guid}.zip` tạm → `File.Move(..., overwrite:true)` sang tên chính thức. POS không bao giờ
  tải file ghi dở. Lỗi giữa chừng → cleanup `_tmp`/zip tạm, **KHÔNG** publish, log + throw.
- **Mức nén**: `MasterDataSync:ZipCompressionLevel` (mặc định `Fastest`). KHÔNG dùng `Optimal` — master data JSON
  lớn, Optimal tốn CPU/chậm; Fastest nhanh 2–5× (file lớn hơn ~10–30%, POS giải nén Deflate chuẩn bình thường).
- **Song song hóa SP2**: `MasterDataSync:MaxParallelTables` (mặc định 4). Mỗi bảng dùng `SqlConnection` riêng
  → thread-safe. `≤ 0` = sequential an toàn. Tăng nếu SQL Server còn headroom; mục tiêu 15–25s cho 85 bảng.
- **SHA-256 companion file**: sau khi publish zip, API tự tạo `{zipName}.sha256` cùng thư mục. Ops verify
  bằng `sha256sum`; POS có thể download để self-verify (tùy chọn). Cleanup tự xóa `.sha256` cùng zip.
  - **Xác thực phía POS (hướng dẫn tích hợp, 2026-07-08)**: `.sha256` KHÔNG xuất hiện trong response
    `GetFileFromFTP` (`GetFileFromServerApiAsync` chỉ liệt kê `*.zip`) — POS tự suy tên theo quy ước
    `{FileName}.sha256`, rồi gọi `DowloadFileStream?filePath=...&fileName={FileName}.sha256` để lấy
    nội dung (text, 64 ký tự hex, lowercase; `DowloadFileStream` trả `Content-Type: text/plain` cho
    file này, `application/x-zip-compressed` cho `.zip`). Quy trình verify khuyến nghị: (1) tải
    `.sha256` trước hoặc sau khi tải zip; (2) tính SHA-256 của file zip đã tải về (BCL `SHA256` .NET,
    hoặc `SHA256CryptoServiceProvider` trên .NET Framework cũ); (3) so sánh hex (không phân biệt hoa
    thường) với nội dung `.sha256`; (4) khớp → cho phép unzip/import; lệch → **hủy file, tải lại**,
    KHÔNG import dữ liệu nghi ngờ corrupt.
  - **Giới hạn phạm vi bảo vệ — BẮT BUỘC hiểu đúng**: cơ chế này là **integrity** (phát hiện file bị
    corrupt/truncate do lỗi mạng/disk khi truyền), **KHÔNG PHẢI authenticity** (chứng minh đúng
    nguồn gốc, chống giả mạo có chủ đích) — vì hash được phát cùng kênh HTTP với file zip, kẻ tấn
    công có khả năng chặn/sửa response thì cũng sửa được cả 2. Muốn chống giả mạo thật cần HMAC
    (khóa bí mật chia sẻ) hoặc dựa vào TLS (HTTPS) để đảm bảo kênh truyền không bị can thiệp.
- **Redis cache SP1** (`MD:SyncTableList:{isChange}` — `:A`/`:W` riêng vì Action khác nhau giữa 2
  nhánh SP, TTL 3600s): metadata 85 bảng cache Redis — tránh SP1 mỗi request. Invalidate thủ công:
  `DEL MD:SyncTableList:A` / `DEL MD:SyncTableList:W` khi DBA thay đổi cấu hình `SyncTableList`.
- **Action linh động theo bảng (2026-07-09, sửa lại 2026-07-11 — bỏ fallback hardcode)**:
  `SyncTableList.Action` (cột DB, script `docs/sql/SyncTableList_AddAction.sql`) — SP
  `[SyncTable_Get]` trả kèm cột `Action`, DBA cấu hình theo từng bảng. `MasterDataSyncService.ActionFor`:
  `IsChangeMode="A"`/`"C"` → batch đầu dùng ĐÚNG `SyncTableInfo.Action` từ DB, batch sau luôn
  `INSERT` (ràng buộc kỹ thuật khi chia batch — tránh truncate/xóa lặp lại giữa các batch cùng bảng,
  không phải giá trị "Action chính" nên không đổi theo cấu hình DB); `IsChangeMode="W"` (Web
  Sync/push 1 POS, `PushStartOfDayDataAsync`) → áp dụng `SyncTableInfo.Action` cho MỌI batch (nhánh
  SP `@IsChange='W'` luôn trả `Action='DELETE-INSERT'` — hardcode ở SP, không phải ở C#).
  **KHÔNG còn fallback hardcode trong C#** (`ActionTruncInsert`/`ActionDeleteInsertFallback` đã bị
  xóa 2026-07-11 — Action tuyệt đối phải lấy từ DB, `entry.Table.Action` rỗng/NULL → C# ném
  `InvalidOperationException` ngay (fail loud), không tự đoán giá trị thay DBA). Hệ quả: **BẮT BUỘC**
  đã chạy `docs/sql/SyncTableList_AddAction.sql` trên CentralMD trước khi dùng luồng sinh master
  data này, nếu không mọi request sẽ lỗi 500 thay vì âm thầm dùng `TRUNC-INSERT`/`DELETE-INSERT`.
- **Khóa**: keyed `SemaphoreSlim` Singleton, key = `{typeSync}_{siteCode}_{posTerminal}` (KHÔNG kèm ngày,
  KHÔNG kèm tên bảng → 1 lock bao trọn cả lượt sinh N zip) + double-check trước khi sinh.
- **Distributed throttle qua Redis** (2026-07-09, giới hạn tổng số lượt sinh chạy đồng thời trên toàn
  cụm, khác `ISyncFileLock` chỉ chặn trùng theo terminal trong 1 process): trước khi vào khóa
  per-terminal, `MasterDataSyncService.EnsureMasterDataFileAsync` gọi
  `IRedisService.TryAcquireSlotAsync(RedisConst.Redis_Key_CreateMasterDataSlots, slotId,
  MasterDataSyncOptions.MaxConcurrentGeneration, ThrottleStaleAfterSeconds)` — không giữ được slot
  (đã có `MaxConcurrentGeneration` lượt khác đang chạy, mặc định 3) → ném
  `MasterDataThrottleException`. Cơ chế: 1 Redis Sorted Set `MD:CreateMasterData:Slots`
  (`RedisManager.TryAcquireSlotAsync`/`ReleaseSlotAsync`), member = slot id (GUID/lượt gọi), score =
  timestamp (ms) — acquire là 1 Lua script atomic (`ZREMRANGEBYSCORE` dọn slot quá hạn +
  `ZCARD` đếm + `ZADD` nếu còn chỗ, không có race condition TOCTOU giữa nhiều request đồng thời).
  Slot quá hạn `ThrottleStaleAfterSeconds` (mặc định 600s) tự bị dọn ở lượt acquire kế tiếp — chữa
  lành khi API crash giữa chừng (Set/ZSET không hỗ trợ TTL theo từng member như String key).
  `ReleaseSlotAsync` gọi trong `finally` KHÔNG nhận `CancellationToken` — đảm bảo nhả được slot kể
  cả khi request client đã hủy (giống pattern `ct=CancellationToken.None` của
  `LogDownloadAsync`/`LogDeleteAsync`). Áp dụng cho CẢ 2 luồng gọi `EnsureMasterDataFileAsync`
  (`GetFileFromFTP` nhánh ALL và `PushStartOfDayDataAsync`/Web Sync) vì đặt tại đúng 1 điểm nghẽn cổ
  chai chung, không đặt riêng ở từng controller/caller.
  `SyncDataPosController.GetFileFromFTP` bắt riêng `MasterDataThrottleException` → trả qua
  `HttpResponseData` sẵn có (`Ok(...)` với `Status=HttpStatusCode.TooManyRequests` trong body) — GIỮ
  NGUYÊN quy ước "HTTP status luôn 200, trạng thái thật trong field `Status`" của endpoint này, KHÔNG
  trả HTTP 429/503 thật. `PushStartOfDayDataAsync` không bắt riêng — exception nổi lên
  `PosMapPage.razor` (đã có try/catch hiển thị Snackbar theo `ex.Message`).
  Config: section `"MasterDataSync"` có sẵn, thêm 2 key `MaxConcurrentGeneration` (mặc định 3),
  `ThrottleStaleAfterSeconds` (mặc định 600).
- **Daily-refresh / dọn file cũ**: `GetFileFromServerApiAsync` liệt kê **mọi** .zip trong folder → sau khi publish,
  xóa zip cùng prefix (`{siteCode}_{typeSync}_{posTerminal}_`, dùng chung cho cả zip common lẫn zip riêng theo
  bảng) không thuộc bộ zip vừa publish trong lượt này (tránh POS nhận file cũ, đồng thời dọn zip mồ côi của
  bảng vừa bị tắt `IsSingleFile`). Khi đọc file đã tồn tại: kiểm tra `LastWriteTime.Date == hôm nay`, nếu cũ →
  xóa và sinh lại.
- **SP1**: `@IsChange='A'` (ALL sync) hoặc `@IsChange='W'` (Web Sync/push 1 POS, nhánh mới) → cả 2 đều
  bỏ qua `@IsByStore`/`@GroupName` (default SP). `@POSLastCounter=0` khi `typeSync==ALL` hoặc
  `IsFirstDataAll=1`.
- **Filter per-store** (bảng `IsByStore=1`): SP2 `[SyncGetDataByTable]` đã được mở rộng 2 tham số
  `@FilterColumn`/`@FilterValue` (default rỗng, backward-compatible) → `WHERE ([Counter]>N OR 0=N) AND [Col]=@val`
  (parameterized, bracket-quote). Service truyền `@FilterColumn = ColumnFilter`, `@FilterValue = siteCode` khi
  `IsByStore=1` và `ColumnFilter` khác rỗng → file chỉ chứa dòng của store đó. `IsByStore=0` hoặc thiếu ColumnFilter
  → không filter (lấy all). Script SP: `docs/sql/SyncGetDataByTable_AddFilter.sql` (phải apply trên CentralMD).
  **BẮT BUỘC bọc ngoặc** điều kiện Counter trong SP, nếu không `AND` bind chặt hơn `OR` → lọt mọi dòng.

## Cập nhật `POSLastCounter` bất đồng bộ (2026-07-09)

> **Bối cảnh**: rà soát xác nhận `SyncTableList.POSLastCounter` trước đây **chưa từng được ghi** ở
> bất kỳ đâu (SP `[SyncTable_Get]` chỉ SELECT) — luồng sync luôn full-resync `@POSLastCounter=0`.
> Cơ chế dưới đây là tính năng mới, để về sau có thể chuyển sang incremental sync thật.

- **Nguồn giá trị**: mỗi bảng master data tự có cột `Counter bigint` riêng (KHÔNG phải
  IDENTITY/ROWVERSION — pattern thủ công `(SELECT ISNULL(MAX(Counter),0)+1 FROM Table)`, tính
  trong SP hoặc C#, xem `docs/architecture/centralMD-schema.md` mục pattern `Counter`+`Pkey`).
  Mỗi khi 1 write-path bump `Counter`, cần đẩy giá trị mới vào `SyncTableList.POSLastCounter`
  tương ứng — nhưng **KHÔNG** update đồng bộ trong cùng transaction ghi (tránh row-level lock
  contention trên `SyncTableList` khi nhiều request ghi master data đồng thời).
- **Kiến trúc**: `System.Threading.Channels` in-process (Singleton) +
  `BackgroundService` batch-flush định kỳ — **KHÔNG** dùng RabbitMQ (over-engineer cho 1 câu
  UPDATE) hay SQL Job/Trigger (nằm ngoài Clean Architecture, không audit/log qua Kibana được).
  - `ISyncTableTrackerService.Track(tableName, counter)` (`POS.Infrastructure/Sync/`) — Singleton,
    ghi non-blocking vào `Channel<(string,long)>` bounded (`SyncTableTrackerOptions.ChannelCapacity`,
    mặc định 5000, `DropOldest` khi đầy — chấp nhận được vì giá trị tự "chữa lành" ở lần bump kế
    tiếp). Repository gọi `Track()` **ngay sau khi** transaction ghi Counter thành công.
  - `SyncTableCounterFlushWorker` (`BackgroundService`) — mỗi `FlushIntervalSeconds` (mặc định 5s)
    drain hết Channel, coalesce theo `Max` mỗi bảng, gọi `ISyncTrackerRepository.BulkUpdateCounterAsync`
    (TVP `dbo.TVP_SyncCounterUpdate` → SP `dbo.usp_SyncTableList_BulkUpdateCounter`, script
    `docs/sql/SyncTableList_BulkUpdateCounter.sql`). UPDATE **idempotent** — chỉ ghi đè khi
    `Counter > ISNULL(POSLastCounter,0)`, an toàn khi nhiều tiến trình cùng flush 1 bảng.
  - **Heartbeat monitor**: mỗi tick flush, worker ghi Redis key
    `Worker:Heartbeat:SyncTableCounterFlush-{AppDomain.FriendlyName}` (JSON `WorkerHeartbeat` DTO —
    tái dùng đúng DTO của `WorkerHeartbeatService`/`PosSalesConsumer`), TTL = `FlushIntervalSeconds × 3`
    lúc chạy, 300s lúc dừng có chủ đích. Chưa tích hợp vào `HealthCheckService`/`HealthPage.razor`/
    `CommonController` (những nơi đó hiện chỉ hard-code check 1 worker qua config
    `HealthCheck:WorkerName` — muốn hiển thị worker này ở `/ops/health` cần generalize config đó
    thành mảng, việc khác ngoài phạm vi đợt này).
- **⚠️ Ngoại lệ kiến trúc có chủ đích**: repo có quy ước ngầm "chỉ `POS.Worker` host
  `BackgroundService`" (`POS.Api`/`POS.Web` gọi `AddInfrastructure()` nhưng không chạy worker nào —
  xem comment tại `DependencyInjection.cs`). Nhưng `Channel` là in-memory, chỉ sống trong đúng tiến
  trình ghi dữ liệu — mà ghi dữ liệu (`CentralMDRepository`...) xảy ra ở **cả `POS.Api` lẫn
  `POS.Web`** (POS.Web có CRUD pages inject thẳng `ICentralMDRepository`, xem
  `.claude/rules/blazor-web-app.md`). Do đó `AddHostedService<SyncTableCounterFlushWorker>()` được
  đăng ký **trực tiếp** trong `POS.Api/Program.cs` và `POS.Web/Program.cs` (KHÔNG qua
  `WorkerRolesOptions` — option đó chỉ dành cho tiến trình `POS.Worker` riêng biệt).
- **Trạng thái rollout write-path** (theo mẫu Pilot A/B — mọi rollout tiếp theo bám đúng 2 mẫu này):
  | Write-path | Bảng | Trạng thái |
  |---|---|---|
  | `CentralMDRepository.CreateProductAsync` (SP `usp_Product_Save`, thêm `@OutItemCounter`/`@OutBarcodeCounter` OUTPUT) | `Item`, `Barcodes` | ✅ Pilot A — đã Track |
  | `CentralMDRepository.SaveProductLockAsync` (raw SQL, thêm `OUTPUT INSERTED.Counter`, track 1 lần/batch) | `ItemBlock` | ✅ Pilot B — đã Track |
  | `PriceRepository.SaveAsync`/`UpdatePriceAsync`/`SoftDeletePriceAsync` (SP `usp_SetupSalePrice_Save`/`usp_SalesPrice_UpdatePrice`/`usp_SalesPrice_SoftDelete`, script `docs/sql/SalesPrice_AddCounterOutput.sql`) | `SalesPrice` | ✅ Pilot C — đã Track |
  | `CreateBranchAsync` / `UpdateBranchInfoAsync` | `Branch` | ⬜ chưa rollout |
  | `CreateStoreAsync` / `UpdateStoreClosingMethodAsync` | `Store` | ⬜ chưa rollout |
  | `CreateEmployeeAsync` / `ChangeEmployeePasswordAsync` | `Staff` | ⬜ chưa rollout |
  | `SaveBankPOSAsync` | `POSTerminalBank` | ⬜ chưa rollout |
  | SP `SetupVoucher_Save.sql` (`VoucherRepository`) | `SetupVoucher*`/`CpnVchBOM*` | ⬜ chưa rollout |
  | SP `SetupCoupon_Save.sql` (`CouponRepository`) | `CpnVchBOMHeader/Line/IssueRule/CodeIssue/Store` | ⬜ chưa rollout |
  | SP `SpecialCombo_Save.sql` (`SpecialComboRepository`) | `SpecialComboHeader` | ⬜ chưa rollout |
  | `PromotionRepository.ApproveSetupAsync` (SP `usp_SetupPromotion_Approve`, bọc SP legacy `Setup_Promotion_Insert` không sửa được — đọc lại `MAX(Counter)` sau khi ghi xong, script `docs/sql/SetupPromotion_ApproveAndStatus.sql`) | `OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite` | ✅ Pilot D — đã Track (lưu ý: SP `usp_SaveSetupCTKMAll` chỉ ghi bảng draft `SetupPromotionHEADER/BUY/GET/SITE`, không có Counter/không liên quan Track — write-path thật để publish sang `Offer*` là `ApproveSetupAsync`) |
  | *(không tìm thấy write-path nào trong ứng dụng — chỉ có đọc)* | `OfferPriority` | ⬜ không áp dụng, cần DBA/business xác nhận nguồn ghi dữ liệu |
- **2 gap phát hiện ngoài phạm vi** (không sửa cùng đợt rollout Track — quyết định riêng nếu cần):
  `CentralMDRepository.UpdatePosTerminalAsync` (bảng `POSTerminal`) và `InsertPOSDataSetupAsync`/
  `UpdatePOSDataSetupAsync` (bảng `POSDataSetup`) hiện **không bump cột `Counter`** — nếu sau này
  rollout Track tới các bảng này, phải quyết định có thêm bump Counter trước hay không.

## Worker sinh zip theo watermark + quarantine (`MasterDataZipGeneratorWorker`, 2026-07-10)

> Tiêu thụ tín hiệu `POSLastCounter` ở mục trên — trước đây không có gì đọc giá trị này để tự động
> trigger sinh zip; worker này là consumer đầu tiên.

- **Vị trí**: `POS.Worker/Workers/{MasterDataZipGeneratorWorker,MasterDataZipGeneratorOptions}.cs`
  (namespace `POS.Worker.Workers`, khác `POS.Infrastructure.Workers`) — **phải** đặt ở `POS.Worker`
  vì cần `IMasterDataSyncService`/`ISyncDataPosService` (`POS.Application`), mà
  `POS.Infrastructure` không được reference `POS.Application`.
- **Đăng ký DI**: `POS.Worker/Program.cs` giờ gọi thêm `AddApplication()` (trước đây chỉ
  `AddInfrastructure()` — worker không cần HTTP AppServices). `AddHostedService<MasterDataZipGeneratorWorker>()`
  chỉ khi `WorkerRoles:EnableMasterDataZipGenerator=true` (mặc định `false`, opt-in — xem
  `docs/ROLLOUT.md`).
- **Cơ chế phát hiện thay đổi**: mỗi `IntervalSeconds` (mặc định 300s), gọi
  `ISyncRepository.GetSyncTableCountersAsync("C")` — bản **KHÔNG cache** của `GetSyncTablesAsync`
  (bản có cache TTL 1h sẽ làm việc phát hiện thay đổi bị trễ tới 1 giờ). `@IsChange='C'` chỉ trả
  bảng `SyncTableList.IsOnlyChange=1` — DBA phải tự `UPDATE` cột này cho bảng cần theo dõi, nếu
  không worker luôn nhận 0 dòng và không làm gì (không phải lỗi code).
- **Watermark — cột DB `SyncTableList.ZipWatermarkCounter`** (đổi 2026-07-11, thay thế thiết kế Redis
  Hash `Worker:Watermark:MasterDataZip` ban đầu). **Lý do đổi**: Redis Hash có thể mất (xóa tay,
  restart mất persistence, evict theo `maxmemory-policy`) — code cũ coi mất key là "lần chạy đầu",
  seed lại watermark bằng counter **hiện tại** rồi bỏ qua generate → mọi thay đổi xảy ra trước khi
  mất key **vĩnh viễn không được trigger sinh zip** (lỗi silent, không throw exception). Chuyển
  watermark sang cột SQL Server loại bỏ hẳn lớp lỗi này (bền vững qua restart Redis/Worker).
  - `docs/sql/SyncTableList_AddZipWatermark.sql`: `ALTER TABLE` thêm `ZipWatermarkCounter bigint NULL
    DEFAULT 0`, backfill `= ISNULL(POSLastCounter,0)` 1 lần, thêm SELECT cột này vào SP
    `[SyncTable_Get]` nhánh `@IsChange='C'` (nhánh DUY NHẤT dùng — `'A'`/`'W'` không đổi), thêm TVP
    `dbo.TVP_ZipWatermarkUpdate` + SP `dbo.usp_SyncTableList_BulkUpdateZipWatermark`.
  - **Cố ý KHÔNG đụng** cột `POSLastCounter`/SP `usp_SyncTableList_BulkUpdateCounter` (ghi bởi
    `SyncTableCounterFlushWorker`, hot path chạy mỗi 5s từ cả `POS.Api`/`POS.Web`) — 2 cột/2 SP hoàn
    toàn tách biệt để không tăng blast radius vào write-path đang chạy ổn định.
  - **Phát hiện thay đổi**: `tables.Where(t => t.POSLastCounter > t.ZipWatermarkCounter)` — cả 2 cột
    đọc trong CÙNG 1 dòng SP1 `[SyncTable_Get] @IsChange='C'`, không còn round-trip Redis riêng,
    không còn khái niệm "watermark chưa tồn tại" cần seed ở runtime (backfill 1 lần trong migration
    đã đảm bảo cột luôn có giá trị hợp lệ — option `SeedWatermarkOnFirstRun` đã xóa khỏi
    `MasterDataZipGeneratorOptions`).
  - **ACK bằng giá trị SNAPSHOT, không re-read**: `ISyncRepository.AckZipWatermarkAsync` nhận
    `Dictionary<TableName, POSLastCounter>` đã đọc lúc ĐẦU cycle (từ `changedTables`, giữ nguyên
    trong biến C#) — KHÔNG re-read DB tại thời điểm ACK. Lý do: giữa lúc đọc counter (đầu cycle) và
    lúc ACK (cuối cycle, sau khi generate xong cho N terminal song song, có thể mất hàng chục giây),
    nếu có write-path khác bump counter cho đúng bảng đó, ACK bằng giá trị SỐNG sẽ vô tình "nuốt"
    thay đổi mới chưa từng được generate trong cycle này — lặp lại đúng lớp lỗi ban đầu (mất dấu
    thay đổi) chỉ khác nguyên nhân. SP `usp_SyncTableList_BulkUpdateZipWatermark` idempotent
    (`WHERE Counter > ISNULL(ZipWatermarkCounter,0)`), cùng mẫu `usp_SyncTableList_BulkUpdateCounter`.
  - **Rollout**: migration BẮT BUỘC chạy TRƯỚC khi deploy code Worker mới (code SELECT cột chưa tồn
    tại → lỗi mỗi cycle nếu chạy sai thứ tự). Redis key `Worker:Watermark:MasterDataZip` cũ **không
    cần chủ động xóa ngay** — để vài ngày làm phao rollback (nếu revert code, key cũ vẫn hợp lệ),
    dọn tay bằng `redis-cli DEL Worker:Watermark:MasterDataZip` sau khi xác nhận bản mới ổn định.
- **Regen path**: tái dùng chính xác luồng "Đồng bộ dữ liệu" của `PosMapPage.razor` — thêm
  `ISyncDataPosService.PushMasterDataChangeAsync(siteCode, posTerminal)`, giống
  `PushStartOfDayDataAsync` nhưng `IsChangeMode="C"` (chỉ bảng đã đổi) và `ForceRegenerate=true`
  (`GetMasterDataFileRequest.ForceRegenerate`, mới) — bỏ qua short-circuit "đã có zip hợp lệ hôm
  nay" trong `MasterDataSyncService.EnsureMasterDataFileAsync` (short-circuit đó vốn đã gần như
  dead code vì tên zip nhúng mili-giây hiện tại, nhưng thêm cờ tường minh để không phụ thuộc vào
  hành vi ngẫu nhiên đó).
- **Phạm vi generate**: mọi `POSTerminal.Status=1` (`ICentralMDRepository.GetPosTerminalListAsync(storeNo: null)`
  rồi filter `IsEnabled`), xử lý song song qua `Parallel.ForEachAsync` (`MaxParallelTerminals`,
  mặc định 4).
- **Quarantine pattern** (Redis Hash `Worker:Quarantine:MasterDataZip`, field = `{StoreNo}:{No}`,
  value = số lần lỗi liên tiếp): terminal lỗi đạt `QuarantineThreshold` (mặc định 3) → các lượt sau
  **bỏ qua hẳn** terminal đó (không thử, chỉ log Warning), tránh 1 terminal hỏng vĩnh viễn (sai
  path, kẹt lock...) chặn watermark của toàn bộ fleet. Thành công → xóa field quarantine (reset về
  0). `MasterDataThrottleException` (throttle cụm — tài nguyên chung, không phải lỗi riêng
  terminal) **không** tính vào quarantine.
- **ACK watermark**: chỉ tịnh tiến khi **mọi terminal đã thử trong lượt này** (không tính terminal
  bị quarantine bỏ qua từ đầu) đều thành công — `POSLastCounter`/`ZipWatermarkCounter` là giá trị
  global/bảng, không thể "ACK một phần". Có lỗi → watermark giữ nguyên, lượt sau tự retry đúng các
  bảng đã đổi.
- **Gap đã biết, chấp nhận có chủ đích**: khi 1 terminal bị quarantine rồi được gỡ thủ công (`HDEL
  Worker:Quarantine:MasterDataZip {store}:{terminal}` sau khi sửa nguyên nhân gốc), watermark có
  thể đã tịnh tiến qua nó trong lúc bị quarantine → terminal đó có thể thiếu dữ liệu của những lần
  đổi đã bỏ lỡ. Khắc phục: sau khi gỡ quarantine, vận hành **phải** bấm lại nút "Đồng bộ dữ liệu"
  (`PushStartOfDayDataAsync`, full resync không phụ thuộc watermark) cho đúng terminal đó — không
  cần code mới, đây là cơ chế full-resync có sẵn.
- **Heartbeat**: `Worker:Heartbeat:MasterDataZipGenerator` (JSON `WorkerHeartbeat`, tái dùng DTO có
  sẵn), `Status`="Running"/"Degraded" (có terminal lỗi trong lượt)/"Stopped".
- **Cache Redis liên quan**: `MD:SyncTableList:C` (SP1 cache, dùng bởi `EnsureMasterDataFileAsync`
  khi generate qua `IsChangeMode="C"` — khác `GetSyncTableCountersAsync` không cache). `DEL
  MD:SyncTableList:C` sau khi DBA đổi cấu hình `SyncTableList` (tương tự `:A`/`:W` đã có).
- **Đã KHÔNG sửa** (ngoài phạm vi, thấy khi rà soát code): tên zip nhúng
  `_{yyyyMMdd}_{HHmmssfff}` (mili-giây hiện tại) khiến short-circuit "đã có zip hợp lệ hôm nay"
  trong `EnsureMasterDataFileAsync` gần như không bao giờ khớp tên chính xác — nghĩa là daily-
  refresh idempotency tài liệu ở mục trên có thể không hoạt động như mô tả. Chưa verify trên hệ
  thống thật.

## Vị trí file

| Layer | File | Namespace |
|---|---|---|
| Contracts | `POS.Common/Dtos/DataSync/{SyncTableInfo,GetMasterDataFileRequest,GetMasterDataFileResult}.cs` | `POS.Common.Dtos.DataSync` |
| Infra repo | `POS.Infrastructure/Repositories/DataSync/{I}SyncRepository.cs` | `...Repositories(.Interfaces)` |
| Infra repo | `POS.Infrastructure/Repositories/DataSync/{I}SyncTrackerRepository.cs` — `BulkUpdateCounterAsync` | `...Repositories(.Interfaces)` |
| Infra sync tracker | `POS.Infrastructure/Sync/{ISyncTableTrackerService,SyncTableTrackerService,SyncTableCounterFlushWorker,SyncTableTrackerOptions}.cs` | `POS.Infrastructure.Sync` |
| Infra files | `POS.Infrastructure/Files/{IFileArchiveService,FileArchiveService,ISyncFileLock,SyncFileLock,MasterDataSyncOptions}.cs` | `POS.Infrastructure.Files` |
| App service | `POS.Application/Features/DataSync/{I}MasterDataSyncService.cs` | `POS.Application.Features.DataSync` |
| App exception | `POS.Application/Features/DataSync/MasterDataThrottleException.cs` | `POS.Application.Features.DataSync` |
| Infra throttle | `POS.Infrastructure/Cache/IRedisManager.cs` — `TryAcquireSlotAsync`/`ReleaseSlotAsync` (ZSET+Lua) + `POS.Infrastructure/Redis/IRedisService.cs` thin wrapper | `POS.Infrastructure.Cache` / `POS.Infrastructure.Redis` |
| Worker | `POS.Worker/Workers/{MasterDataZipGeneratorWorker,MasterDataZipGeneratorOptions}.cs` — poll watermark (cột DB), generate zip, quarantine terminal lỗi | `POS.Worker.Workers` |
| Config | `appsettings.json` → section `"MasterDataSync"` (`SqlCommandTimeoutSeconds`, `KeepZipDays`, `DateInZipName`, `ZipCompressionLevel`, `MaxConcurrentGeneration`, `ThrottleStaleAfterSeconds`) | — |
| Config | `POS.Worker/appsettings.json` → section `"MasterDataZipGenerator"` (`IntervalSeconds`, `LockTtlMinutes`, `MaxParallelTerminals`, `QuarantineThreshold`) + `"WorkerRoles":"EnableMasterDataZipGenerator"` | — |
| Config | `appsettings.json` (POS.Api + POS.Web) → section `"SyncTableTracker"` (`FlushIntervalSeconds`, `ChannelCapacity`) | — |
| DB script | `docs/sql/SyncTableList_AddIsSingleFile.sql` — cột `SyncTableList.IsSingleFile` + SP `[SyncTable_Get]` | CentralMD (áp dụng thủ công) |
| DB script | `docs/sql/SyncTableList_AddAction.sql` — cột `SyncTableList.Action` + SP `[SyncTable_Get]` (thêm nhánh `@IsChange='W'`) | CentralMD (áp dụng thủ công) |
| DB script | `docs/sql/SyncTableList_BulkUpdateCounter.sql` — TVP `dbo.TVP_SyncCounterUpdate` + SP `usp_SyncTableList_BulkUpdateCounter` | CentralMD (áp dụng thủ công) |
| DB script | `docs/sql/SyncTableList_AddZipWatermark.sql` — cột `SyncTableList.ZipWatermarkCounter` (backfill) + TVP `dbo.TVP_ZipWatermarkUpdate` + SP `usp_SyncTableList_BulkUpdateZipWatermark` + SP `[SyncTable_Get]` (thêm SELECT nhánh `'C'`) — thay thế Redis watermark của `MasterDataZipGeneratorWorker` | CentralMD (áp dụng thủ công, BẮT BUỘC trước deploy code) |
| DB script | `docs/sql/Product_Save.sql` — thêm `@OutItemCounter`/`@OutBarcodeCounter` OUTPUT cho `usp_Product_Save` | CentralMD (áp dụng thủ công) |
| DB script | `docs/sql/SalesPrice_AddCounterOutput.sql` — thêm `@OutCounter` OUTPUT (`usp_SetupSalePrice_Save`) + cột `Counter` vào result set (`usp_SalesPrice_UpdatePrice`/`usp_SalesPrice_SoftDelete`) | CentralMD (áp dụng thủ công, SAU `SetupSalePrice_Save.sql`+`SalesPrice_EditDelete*.sql`) |

> Thư mục đích dùng `AppSettings:FtpRootPath` qua `MapFtpPath` — KHÔNG thêm `RootPath` riêng.

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
```

> **Download logging**: `DowloadFileStream` stream thủ công (`CopyToAsync(Response.Body, RequestAborted)`) để biết
> kết quả best-effort: `Success` = gửi đủ byte không bị ngắt (KHÔNG đảm bảo POS lưu xong), `Aborted` = client ngắt,
> `Error`. Ghi 1 dòng `dbo.MasterDataDownloadLog` qua `IMasterDataSyncService.LogDownloadAsync` (fail-safe, nuốt lỗi
> nếu bảng chưa tạo). **KHÔNG tự xóa file** sau download (giữ cache ngày; dọn bằng daily-refresh + KeepZipDays).
> Script bảng: `docs/sql/MasterDataDownloadLog.sql`. Log với `ct=CancellationToken.None` để ghi được cả khi client ngắt.

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
- **Action linh động theo bảng (2026-07-09)**: `SyncTableList.Action` (cột DB, script
  `docs/sql/SyncTableList_AddAction.sql`) — SP `[SyncTable_Get]` trả kèm cột `Action`, DBA cấu hình
  theo từng bảng thay vì hardcode ở C#. `MasterDataSyncService.ActionFor`: `IsChangeMode="A"` (ALL
  sync, mặc định) → batch đầu dùng `SyncTableInfo.Action` (fallback `TRUNC-INSERT` nếu SP chưa có
  cột), batch sau luôn `INSERT` (ràng buộc kỹ thuật — tránh truncate/xóa lặp lại giữa các batch
  cùng bảng, không đổi theo cấu hình DB); `IsChangeMode="W"` (Web Sync/push 1 POS,
  `PushStartOfDayDataAsync`) → nhánh SP `@IsChange='W'` luôn trả `Action='DELETE-INSERT'`, áp dụng
  cho MỌI batch (fallback hằng số `ActionDeleteInsertFallback` nếu SP chưa có cột).
- **Khóa**: keyed `SemaphoreSlim` Singleton, key = `{typeSync}_{siteCode}_{posTerminal}` (KHÔNG kèm ngày,
  KHÔNG kèm tên bảng → 1 lock bao trọn cả lượt sinh N zip) + double-check trước khi sinh.
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

## Vị trí file

| Layer | File | Namespace |
|---|---|---|
| Contracts | `POS.Common/Dtos/DataSync/{SyncTableInfo,GetMasterDataFileRequest,GetMasterDataFileResult}.cs` | `POS.Common.Dtos.DataSync` |
| Infra repo | `POS.Infrastructure/Repositories/DataSync/{I}SyncRepository.cs` | `...Repositories(.Interfaces)` |
| Infra files | `POS.Infrastructure/Files/{IFileArchiveService,FileArchiveService,ISyncFileLock,SyncFileLock,MasterDataSyncOptions}.cs` | `POS.Infrastructure.Files` |
| App service | `POS.Application/Features/DataSync/{I}MasterDataSyncService.cs` | `POS.Application.Features.DataSync` |
| Config | `appsettings.json` → section `"MasterDataSync"` (`SqlCommandTimeoutSeconds`, `KeepZipDays`, `DateInZipName`, `ZipCompressionLevel`) | — |
| DB script | `docs/sql/SyncTableList_AddIsSingleFile.sql` — cột `SyncTableList.IsSingleFile` + SP `[SyncTable_Get]` | CentralMD (áp dụng thủ công) |
| DB script | `docs/sql/SyncTableList_AddAction.sql` — cột `SyncTableList.Action` + SP `[SyncTable_Get]` (thêm nhánh `@IsChange='W'`) | CentralMD (áp dụng thủ công) |

> Thư mục đích dùng `AppSettings:FtpRootPath` qua `MapFtpPath` — KHÔNG thêm `RootPath` riêng.

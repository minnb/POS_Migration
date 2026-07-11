# PosFileImportWorker / PosFileImportService — Chi tiết kỹ thuật

> Tổng hợp logic kỹ thuật, ràng buộc DB, và các "gotcha" phát hiện được khi rà soát code (2026-07-10).
> Tổng quan/inventory: `docs/worker/worker_status.md` mục 1.1 (#1). Go-live checklist: `docs/ROLLOUT.md` §O2.
> File này đào sâu **cơ chế cụ thể bên trong** — không lặp lại nội dung 2 file trên.

---

## 1. Vị trí file

| File | Vai trò |
|---|---|
| `src/POS.Infrastructure/Workers/PosFileImportWorker.cs` | `BackgroundService` wrapper — vòng lặp Timer cho Model B (long-running) |
| `src/POS.Infrastructure/Workers/PosFileImportService.cs` | Logic thật "1 chu kỳ quét" — dùng chung cho cả Model A (`--run-once`) và Model B (đăng ký `AddSingleton` trong `POS.Worker/Program.cs`) |
| `src/POS.Infrastructure/Files/FileImportOptions.cs` | Options bind section `"FileImport"` |
| `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs` — `InInsertToTableByJson` | Nơi thật sự ghi dữ liệu vào DB |
| `docs/sql/database/CentralSale.sql` (UTF-16) | SP `Sale_InsertDataByOrder_KAFKA`, `Sale_InsertToTableByJsonV2` |
| `tests/POS.ContractTests/PosFileImportFileNameTests.cs` | Khóa contract parse tên file `Type_PosNo_TransactionId.txt` |

**Ghi chú**: `docs/CURRENT_STRUCTURE.md` hiện **chưa có entry** cho `PosFileImportWorker`/`PosFileImportService`/`FileImportOptions` dù đây là "nguồn sự thật cấu trúc code" — cần bổ sung riêng nếu có task chạm vào layer này.

---

## 2. Logic kỹ thuật

### 2.1 Hai chế độ chạy dùng chung 1 logic

- **Model A (cron thật, Ubuntu host, `--run-once`)**: `POS.Worker/Program.cs` **không đăng ký `IHostedService` nào** khi có flag `--run-once`/env `WORKER_RUN_ONCE=true`. Thay vào đó resolve thẳng `PosFileImportService` (đã `AddSingleton`) và gọi `RunOnceAsync(CancellationToken.None)` **1 lần**, trả exit code 0 (OK) / 1 (exception chưa bắt lọt ra ngoài `RunOnceAsync`) — khác hẳn Model B, ở đây exception **không bị nuốt**.
- **Model B (Docker dài hạn)**: `PosFileImportWorker.ExecuteAsync` — vòng `while (!stoppingToken.IsCancellationRequested)`, mỗi vòng gọi `RunOnceAsync` bọc try/catch: lỗi bất kỳ (trừ `OperationCanceledException` khi đang cancel) → `status="Degraded"`, log lỗi, **KHÔNG** kill loop — luôn tiếp tục ghi heartbeat rồi `Task.Delay(interval)` sang vòng kế. Heartbeat cuối cùng khi thoát: `WriteHeartbeat("Stopped", 300)` — TTL **cứng 300s** bất kể `PollIntervalSeconds` cấu hình gì.
- **Thực tế deploy hiện tại (xem `docs/ROLLOUT.md` §O2)**: Docker/Model B chạy với `WorkerRoles:EnableFileProcessing=false` — worker này **chỉ thực sự chạy ở Model A** (cron `--run-once` trên host Ubuntu), dù code Model B vẫn tồn tại và hoạt động được nếu bật.

### 2.2 `RunOnceAsync` — 1 chu kỳ quét

```
Enabled=false hoặc InboxFolder rỗng → log Warning, return 0 (no-op, KHÔNG phải lỗi)
Directory.CreateDirectory cho Inbox/Error/Work (mỗi chu kỳ, no-op nếu đã có)
stableBefore = UtcNow - StableSeconds
zips = EnumerateFiles(InboxFolder, FileFilter, TopDirectoryOnly)
         .Where(f => f.LastWriteTimeUtc <= stableBefore)     ← "stable" = tuổi file, KHÔNG phải lock-check
         .OrderBy(f => f.LastWriteTimeUtc)                    ← cũ nhất trước
         .Take(MaxFilesPerCycle)
foreach zip → ProcessZipAsync(...)
```

**"Stable" chỉ là age-filter dựa trên `LastWriteTimeUtc`** — không phát hiện file đã ghi xong nhưng writer vẫn giữ handle mở; trường hợp đó bị bắt gián tiếp khi bước "claim" (`File.Move`) ném exception vì file đang bị khóa (xem 2.3). Quét chỉ ở **top-level** (`TopDirectoryOnly`) — file trong sub-folder của `InboxFolder` bị bỏ qua hoàn toàn (khác với bên trong zip, nơi `.txt` được tìm `AllDirectories` — bất đối xứng có chủ đích/vô tình, cần lưu ý).

### 2.3 `ProcessZipAsync` — claim mechanism (chống đa-instance)

**Cơ chế xác nhận qua đọc code — KHÔNG có Redis/DB lock nào**, chỉ dựa vào tính atomic của `File.Move`:

```csharp
var workDir = Path.Combine(workRoot, Guid.NewGuid().ToString("N"));   // random riêng mỗi zip
var claimedZip = Path.Combine(workDir, zipName);
try { Directory.CreateDirectory(workDir); File.Move(inboxZipPath, claimedZip); }
catch (Exception ex) { /* thua cuộc giành file, hoặc file đang khóa */ TryDeleteDir(workDir); return 0; }
```

Instance nào `File.Move` thành công thì "sở hữu" file đó; instance thua nhận exception và bỏ qua (không retry trong cùng chu kỳ). An toàn với nhiều instance chạy chung 1 `InboxFolder` mà không cần khóa ngoài.

**Extract**: `IFileArchiveService.ExtractToDirectory` → `ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles:true)` — dựa hoàn toàn vào chống zip-slip **built-in của .NET** (không có validate riêng); zip-slip ném exception → rơi vào nhánh lỗi (mục 2.4), zip vào `ErrorFolder`, không silent-accept.

**1 scope DI / 1 zip**: resolve `ICentralSaleRepository` 1 lần cho cả zip, dùng lại cho mọi `.txt` bên trong (mỗi lần gọi `InInsertToTableByJson` tự mở/đóng `SqlConnection` riêng bên trong).

### 2.4 Parse tên file & xử lý từng `.txt`

Contract tên file: `Type_PosNo_TransactionId.txt` — `TryParseFileName` (`PosFileImportService.cs`, **public static, đã unit-test** ở `PosFileImportFileNameTests.cs`):

```csharp
var p = stem.Split('_', 3);              // TỐI ĐA 3 phần — TransactionId nuốt trọn phần _ còn lại
if (p.Length < 3) return false;
type = p[0]; posNo = p[1]; transactionId = p[2];
return posNo.Length > 0 && transactionId.Length > 0;
```

`storeNo = StringHelper.Left(posNo, 4)`.

Nội dung `.txt` = **JSON envelope `{Type, Data}`** (chính là payload `KafkaMessagePOS`, KHÔNG phải `KafkaMessageDto` đầy đủ) — toàn bộ nội dung raw được truyền nguyên làm tham số `message` cho `InInsertToTableByJson`.

Từng file: parse lỗi / rỗng / insert trả `(false, ...)` → return `false` → tính vào `anyFailed` ở cấp zip → **toàn bộ zip** bị chuyển `ErrorFolder`, dù các `.txt` khác trong zip đã insert DB thành công (không rollback được ở tầng file — chỉ file bị cách ly, dữ liệu DB đã ghi vẫn còn).

### 2.5 Vòng đời zip & dọn dẹp

| Kết quả | Hành động |
|---|---|
| Mọi `.txt` OK | Xóa file zip đã claim (`TryDeleteFile`) |
| ≥1 record lỗi (không throw) | `MoveToError` — đổi tên `{stem}_{yyyyMMddHHmmss}_{guid}{ext}`, chuyển sang `ErrorFolder` |
| Exception giữa chừng (extract lỗi, enum `.txt` lỗi...) | Bắt ở catch tổng → coi như (b), cùng hành động |
| `ErrorFolder` rỗng/chưa cấu hình | **Xóa luôn file lỗi** (`TryDeleteFile`) — mất dữ liệu âm thầm nếu quên cấu hình |

`TryDeleteDir(workDir)` dọn thư mục tạm chạy **sau** nhánh xử lý, **KHÔNG nằm trong `finally`** — hiện tại vẫn luôn chạy tới vì catch không `return` sớm, nhưng đây là điểm dễ vỡ nếu sau này ai thêm `return` sớm trong try/catch mà quên gọi dọn dẹp (xem Gotcha #6).

---

## 3. Ràng buộc DB (Stored Procedure `Sale_InsertDataByOrder_KAFKA`)

Gọi qua `CentralSaleRepository.InInsertToTableByJson(storeNo, posNo, transactionId, message, source="FILE", ct)` — dùng **`CentralSaleConnectionFactory` cố định** (không qua `StoreRoutedConnectionFactory`/StoreSetServer — comment trong code: đổi có chủ đích để tránh network error khi ServerIP của store không kết nối được trên UAT/Prod).

```sql
CREATE PROC [dbo].[Sale_InsertDataByOrder_KAFKA] (@Type varchar(50), @Json nvarchar(max) = '')
```

| `@Type` | Hành vi SP | STATUS trả về |
|---|---|---|
| `ORIGSALE` | `Return` ngay, KHÔNG có `SELECT` nào — no-op tuyệt đối | *(không có row nào)* |
| `SALE` | Check `EXISTS TransHeader.OrderNo` — trùng → `Return` sớm, không insert | `2` nếu trùng |
| `VOID` | Check `EXISTS TransVoidLine.DocumentNo` — trùng → `Return` sớm | `7` nếu trùng |
| khác | Vào `BEGIN TRANSACTION`, cursor duyệt mọi key JSON top-level, `EXEC Sale_InsertToTableByJsonV2 @TableName, @Json` cho từng bảng, `COMMIT` | `1` nếu OK, `0` nếu lỗi (catch → `ROLLBACK` + ghi `Interface_Errors`) |

**Ràng buộc quan trọng nhất — điểm nghẽn C# chỉ kiểm tra `STATUS==0`**: `CentralSaleRepository.cs` chỉ coi `STATUS==0` là lỗi. `STATUS=2` (trùng SALE), `STATUS=7` (trùng VOID), và trường hợp `ORIGSALE` (không có row nào, `checkStatus=null`) đều **rơi vào nhánh thành công** (`_flag=true`). Nghĩa là: dữ liệu trùng được server dedupe âm thầm và báo về C# y như thành công thật — không phân biệt được qua log ứng dụng.

**Transaction**: 1 transaction bọc toàn bộ cursor loop qua mọi bảng trong JSON — lỗi ở bất kỳ bảng nào rollback toàn bộ message đó (all-or-nothing), không phải per-table.

**`Sale_InsertToTableByJsonV2`** (gọi 1 lần/bảng): dynamic INSERT xây từ `sys.columns`/`sys.types` + `OPENJSON`. Hardcode `IF @table_name = 'TransBonus' return` (comment "Honglk 2024-10-17" — tạm skip, chưa có ticket). Không có FK nào trong `RPOSCentralSales` (theo `docs/architecture/centralsale-schema.md`) — chống trùng dữ liệu con (`TransLine`, `TransPaymentEntry`...) hoàn toàn phụ thuộc vào gate dedupe `OrderNo`/`DocumentNo` ở SP cha; nếu gate đó bị bỏ qua (Type khác SALE/VOID/ORIGSALE) thì không còn lớp bảo vệ nào chống insert trùng.

**Log audit `DataRawJson`**: `finally` luôn gọi `InsertDataRawJsonAsync(transactionId, dataType, message, flag, errorMsg, source="FILE")` — bọc `catch {}` rỗng với comment nói "PushSalesToTopic() sẽ retry qua RabbitMQ" — **comment này SAI NGỮ CẢNH cho luồng file-import**: worker này không có fallback RabbitMQ nào, nếu ghi log audit lỗi thì mất dấu vết hoàn toàn (chỉ còn `ILogger`).

---

## 4. Cấu hình (`FileImportOptions`, section `"FileImport"`)

| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `Enabled` | `true` | `false` → no-op ngay từ đầu |
| `InboxFolder` | `""` | Rỗng → coi như disabled |
| `ErrorFolder` | `""` | Rỗng → file lỗi bị **xóa luôn**, không giữ lại |
| `WorkFolder` | `""` | Rỗng → mặc định `{InboxFolder}/_work` |
| `FileFilter` | `"*.zip"` | Glob quét inbox |
| `PollIntervalSeconds` | `30` | Chỉ áp dụng Model B |
| `StableSeconds` | `10` | Ngưỡng tuổi file coi là "ghi xong" |
| `MaxFilesPerCycle` | `20` | Giới hạn số zip/chu kỳ — dư ra chờ chu kỳ sau (FIFO theo tuổi, không có chống đói) |
| `Source` | `"FILE"` | Ghi vào `DataRawJson.Source` — phân biệt với `"WORKER"` (RabbitMQ) / `"WEB"` (HTTP) |

---

## 5. Gotchas (điểm cần lưu ý)

1. **⚠️ Race điều kiện đã biết + CHẤP NHẬN CÓ CHỦ ĐÍCH** (xem `docs/ROLLOUT.md` §O2): ở Docker, `InboxFolder` trỏ **CÙNG thư mục** (`SyncDataPos/Sale/Kafka`) mà `UploadFileSale` (POS.Api) ghi file zip vào và tự xử lý fire-and-forget qua `IDataRawService.ProcessFileToStagingDBAsync` (đẩy Kafka). Nếu `UploadFileSale` xử lý xong trước → worker không bao giờ thấy file. Nếu Kafka-push lỗi và để file lại → worker sẽ claim và xử lý qua **con đường hoàn toàn khác** (insert DB trực tiếp, `Source="FILE"`, không qua Kafka) — 2 luồng độc lập cùng chạm 1 thư mục.
2. **STATUS 2/7 (trùng SALE/VOID) và ORIGSALE (no-op) đều báo "thành công"** — không có cách nào phân biệt qua log ứng dụng giữa "insert thật" và "server tự dedupe/no-op". Muốn biết cần tra trực tiếp DB (`Interface_Errors` chỉ ghi STATUS=0).
3. **Parse tên file không validate ngữ nghĩa** — `Split('_', 3)` chỉ check độ dài phần `PosNo`/`TransactionId` > 0, không check `Type` có hợp lệ hay không. Tên file có thêm `_` bất thường trong phần `Type` sẽ dịch chuyển toàn bộ field mà **không có warning nào** (khác trường hợp thiếu phần — có warning rõ ràng).
4. **`ErrorFolder` rỗng = mất dữ liệu âm thầm** — không phải lỗi hiển nhiên, dễ bị bỏ sót khi setup môi trường mới.
5. **Toàn bộ zip bị cách ly dù chỉ 1 `.txt` lỗi** — các `.txt` khác đã insert DB thành công không được "tách" ra khỏi zip lỗi; nếu retry thủ công zip đó sau này sẽ insert lại các bản ghi đã thành công (chỉ được cứu bởi SP-level dedupe STATUS 2/7, không phải cơ chế ở tầng file).
6. **Dọn `_tmp`/`workDir` không nằm trong `finally`** — hiện hoạt động đúng vì catch không return sớm, nhưng là điểm dễ vỡ: thêm 1 `return` sớm trong tương lai (không kèm dọn dẹp) sẽ rò rỉ thư mục `_work/{guid}/`. `TryDeleteFile`/`TryDeleteDir` nuốt exception hoàn toàn im lặng (không log) — dọn dẹp thất bại (file bị khóa/permission) sẽ không để lại dấu vết nào.
7. **Comment "PushSalesToTopic() sẽ retry" trong `catch {}` của `InsertDataRawJsonAsync` là stale/sai ngữ cảnh** khi áp cho luồng file-import — worker này không có RabbitMQ fallback nào, ghi log audit lỗi = mất dấu vết vĩnh viễn.
8. **Model A (cron) không nuốt exception** — khác Model B, nếu `RunOnceAsync` ném exception ra ngoài ở chế độ `--run-once`, process thoát với exit code 1 (cron script dựa vào đó để biết fail) — cần hiểu đúng khác biệt này khi debug 2 môi trường.
9. **`message.Replace("'", "")` xóa mọi dấu nháy đơn trong toàn bộ payload** trước khi build `@Json` gửi SP (biện pháp phòng SQL injection cho dynamic SQL trong `Sale_InsertToTableByJsonV2`) — **tên khách hàng/sản phẩm có dấu `'` (vd `O'Brien`) sẽ bị mất ký tự đó vĩnh viễn, không log cảnh báo**. Đây là hành vi chung của `InInsertToTableByJson`, dùng chung bởi cả `PosFileImportService` lẫn `PosSalesConsumerWorker` (xem `docs/worker/PosSalesConsumerWorker.md`).

---

## 6. Trạng thái verify

- Đã đọc toàn bộ `PosFileImportWorker.cs` (82 dòng), `PosFileImportService.cs` (233 dòng), `FileImportOptions.cs`, `CentralSaleRepository.InInsertToTableByJson` (đầy đủ), SP `Sale_InsertDataByOrder_KAFKA`/`Sale_InsertToTableByJsonV2` (`docs/sql/database/CentralSale.sql`).
- **CHƯA chạy thử thật** (cần môi trường có SQL Server CentralSale, thư mục `FTPBLUEPOS` ghi được) — mọi phát hiện ở trên dựa trên đọc code tĩnh, không phải quan sát runtime.

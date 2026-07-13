---
name: api-file-streaming-patterns
description: Xử lý file quy mô lớn trong POS.Api — song song hóa Parallel.ForEachAsync, SHA-256 companion file, tách N file output theo cờ DB, resolve đường dẫn file POS gửi (SyncDataPos). Đọc khi làm việc với sinh/stream/publish file cho máy POS.
---

# File & Stream Patterns — POS.Api (HOW)

> **Áp dụng khi:** POS.Api sinh, publish, hoặc stream file (thường là master data .zip) cho ~5.000
> máy POS — batch job xử lý nhiều bảng/file song song, verify integrity, hoặc route đường dẫn file
> POS gửi lên.
>
> **Rules (nguồn canonical):** quyết định kiến trúc (`.sha256` không nằm trong response, flag
> trên bảng DB thay vì appsettings, atomic publish, all-or-nothing regenerate, giới hạn phạm vi
> bảo vệ SHA-256) là **Rules** ở **`.claude/rules/masterdata-sync.md`**. File này chỉ giữ code mẫu
> (HOW) cho các quyết định đó.

---

## Pattern: Parallel.ForEachAsync cho nhiều DB call độc lập

> Áp dụng khi: cần xử lý N item (ví dụ N bảng SP2) mà mỗi item **mở connection riêng, không shared state** → song song hóa an toàn.

```csharp
// Precompute index TRƯỚC khi song song (index ổn định, không race condition)
var entries = items
    .Where(t => !string.IsNullOrWhiteSpace(t.Key))
    .Select((t, idx) => (Item: t, Index: idx + 1))
    .ToList();

await Parallel.ForEachAsync(entries, new ParallelOptions
{
    MaxDegreeOfParallelism = _opt.MaxParallelTables > 0 ? _opt.MaxParallelTables : 1,
    CancellationToken = ct
}, async (entry, token) =>
{
    // Mỗi iteration mở SqlConnection riêng → hoàn toàn thread-safe
    await repo.StreamTableToFilesAsync(entry.Item, ..., token);
});
```

**Điều kiện an toàn:** (1) mỗi iteration tạo connection/resource riêng; (2) output (file, key) unique per-item; (3) exception 1 item → `AggregateException` wrap throw ra caller.
**Cấu hình:** `MaxParallelTables <= 0` → sequential (fallback an toàn). SQL Server connection pool (default 100) đủ cho parallelism = 4–8.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs` — `EnsureMasterDataFileAsync`

---

## Pattern: SHA-256 companion file cho binary được publish

> Áp dụng khi: publish file binary (zip, archive) ra đĩa và cần ops/monitoring verify integrity sau này.

```csharp
// Sau atomic publish (File.Move overwrite)
File.Move(tmpZip, destPath, overwrite: true);

var hash = await ComputeSha256HexAsync(destPath, ct);
await File.WriteAllTextAsync(destPath + ".sha256", hash, ct);  // "a3f5c2e1..." (64 hex chars)

// Cleanup: xóa .sha256 cùng lúc với zip
TryDeleteFile(destPath);
TryDeleteFile(destPath + ".sha256");

// Helper (BCL .NET 6+ — không cần NuGet)
private static async Task<string> ComputeSha256HexAsync(string filePath, CancellationToken ct)
{
    await using var fs = File.OpenRead(filePath);
    var bytes = await SHA256.HashDataAsync(fs, ct);
    return Convert.ToHexString(bytes).ToLowerInvariant();
}
```

**Quan trọng:** file `.sha256` là companion, KHÔNG thêm vào response API (filter `*.zip` → `.sha256` không bị liệt kê). Verify trên server: `sha256sum {file}.zip` rồi so sánh với nội dung `.sha256`.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs`

---

## Pattern: Tách N file output theo cờ DB (thay vì appsettings) — idempotent all-or-nothing

> Áp dụng khi: 1 batch job sinh ra nhiều file, và "cái gì tách riêng" là quyết định **vận hành** (DBA
> đổi theo dữ liệu thực tế của từng thời điểm), KHÔNG phải quyết định lúc code/deploy → đặt cờ trên
> chính bảng metadata nguồn (SP1) thay vì `appsettings.json`, để đổi hành vi KHÔNG cần deploy lại app.

```csharp
// 1. Metadata row có thêm cờ (SyncTableInfo.IsSingleFile) — Dapper tự map cột SP mới, không cần sửa Repository.
var outDir = row.IsSingleFile ? Path.Combine(tmpDir, SanitizeForFolder(row.Key)) : Path.Combine(tmpDir, "_common");

// 2. Idempotent check phải là ALL-OR-NOTHING trên TOÀN BỘ danh sách output dự kiến của lượt chạy
//    (tính được ngay sau khi có metadata, TRƯỚC khi chạy job) — không regenerate lẻ từng file.
var expectedNames = new List<string> { CommonName() };
expectedNames.AddRange(singleKeys.Select(SingleName));
if (expectedNames.All(n => IsTodayValid(Path.Combine(targetDir, n))))
    return expectedNames.Select(n => Success(n)).ToList();

// 3. Publish + cleanup dùng CHUNG 1 prefix, loại trừ theo HashSet "vừa publish lượt này"
//    → tự dọn được file mồ côi khi cờ IsSingleFile bị TẮT lại (không cần logic cleanup riêng cho case này).
CleanupSiblingZips(req, publishedNamesThisRun);
```

**Vì sao KHÔNG dùng appsettings cho việc này:** cấu hình trong `appsettings.json` cần deploy/restart để
đổi; cờ trên bảng DB cho phép DBA `UPDATE` trực tiếp + `DEL` cache Redis liên quan để có hiệu lực ngay,
phù hợp khi danh sách "cái gì cần tách riêng" thay đổi theo dữ liệu thực tế từng site/thời điểm.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/MasterDataSyncService.cs` —
> `EnsureMasterDataFileAsync` tách zip theo `SyncTableList.IsSingleFile`
> (`docs/sql/SyncTableList_AddIsSingleFile.sql`), fix timeout download POS với zip quá lớn.

---

## Pattern: Xử lý đường dẫn file POS gửi (SyncDataPos) — luôn giải về FtpRootPath, dùng chung

> Áp dụng khi: endpoint nhận `filePath`/`pathSync` từ máy POS (download/delete/list file trong FTPBLUEPOS).
> Rút ra từ 2 bug thực tế trong `SyncDataPosController` (download OK nhưng delete/list lại rỗng/sai thư mục).

- **POS gửi UNC Windows** (`\\ip\FTPBLUEPOS\...`) — trên **Linux Docker không resolve**. Dùng chung 1 helper
  `ISyncDataPosService.ResolveFtpPhysicalPath(posPath)`: tách phần sau `FTPBLUEPOS` rồi `MapFtpPath` về
  `FtpRootPath` local. Mọi endpoint (download/delete) phải map trước khi `File.Exists`/`Delete`; endpoint xóa
  thêm **guard path-traversal** (`fullLocal.StartsWith(MapFtpPath(""))`).
- **`pathSync` POS gửi đã chứa đủ `SyncDataPos/POS/{typeSync}`** → giải thư mục list/tạo qua
  `MapFtpPath($"{pathSync}/{folderFile}")` cho MỌI typeSync (ALL/CHANGE) để listing khớp nơi file được tạo +
  khớp UNC `PathFileIPServer` + URL download.
- **Anti-pattern**: dùng `AppSettings:FolderShare` + tự ghép `\{typeSync}\` cho nhánh CHANGE → thiếu segment
  `SyncDataPos\POS`, và hardcode `syncdatapos/pos` lowercase → **sai case trên Linux**. Đừng suy đoán path bằng
  `FolderShare`; luôn bám `MapFtpPath` + `pathSync` từ query (đồng nhất với nhánh ALL).
- **Tham số hoá hành vi theo caller qua request DTO, KHÔNG detect caller**: thêm field nullable vào DTO nội bộ
  (vd `GetMasterDataFileRequest.SyncAction`) để override (Web Sync="DELETE-INSERT", null=mặc định TRUNC-INSERT→INSERT)
  — DTO nội bộ nên không phá contract test.

> Ví dụ thực tế: `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`ResolveFtpPhysicalPath`,
> `GetFileFromServerApiAsync`, `PushStartOfDayDataAsync`), `src/POS.Api/Controllers/SyncDataPosController.cs`
> (`DowloadFileStream`/`DeleteFileFromFTP`/`GetFileFromFTP`), `MasterDataSyncService.ActionFor`

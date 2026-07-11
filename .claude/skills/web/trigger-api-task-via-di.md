---
name: web-trigger-api-task-via-di
description: POS.Web kích hoạt tác vụ server-side vốn thuộc POS.Api (sinh file master data...) qua DI trực tiếp, không gọi HTTP. Đọc khi 1 page POS.Web cần chạy tác vụ của POS.Api.
---

# Pattern: POS.Web kích hoạt tác vụ server-side của POS.Api qua DI (không HTTP)

> Áp dụng khi: page POS.Web cần chạy 1 tác vụ vốn thuộc POS.Api (sinh file master data, xử lý file…).
> Luật dự án: **KHÔNG** gọi HTTP sang POS.Api — inject thẳng Application service (đã đăng ký chung qua
> `AddApplication()`/`AddInfrastructure()`) và gọi method. Bọc glue vào 1 method Application dùng chung,
> KHÔNG nhồi logic vào `.razor`.

```csharp
// Application: method mới delegate service sinh file có sẵn của POS.Api (KHÔNG chép/đổi logic sinh file)
public async Task<GetMasterDataFileResult> PushStartOfDayDataAsync(string siteCode, string posTerminal, CancellationToken ct = default)
{
    // BẮT BUỘC bám ĐÚNG cách controller dựng đường dẫn đích — dùng MapFtpPath, KHÔNG tự Path.Combine(FolderShare,...)
    var folderFile = $"{siteCode}/{posTerminal}";
    const string pathSync = "SyncDataPos/POS/CHANGE";
    var targetDir = MapFtpPath($"{pathSync}/{folderFile}");     // = FtpRootPath\SyncDataPos\POS\CHANGE\{site}\{terminal}
    var req = new GetMasterDataFileRequest { SiteCode = siteCode, PosTerminal = posTerminal,
        FolderFile = folderFile, PathSync = pathSync, TypeSync = "ALL", TargetDir = targetDir };
    return await masterDataSyncService.EnsureMasterDataFileAsync(req, ct);   // tái dùng nguyên
}
```

- **UI**: nút trong cột Action → `MudMessageBox` confirm → `_syncing` HashSet (theo key row) đổi nút thành
  `MudProgressCircular` + `RowClassFunc` pulse nền; bọc nút trong `<div @onclick:stopPropagation="true">` nếu
  row có `OnRowClick`. Ghi `IAuditLogger.LogAsync(actor,"SYNC",entity,key,null,detailJson)` **khi thành công**.
- **Anti-pattern (bug thực tế)**: tự dựng đường dẫn FTP bằng `Path.Combine(configuration["AppSettings:FolderShare"],...)`
  → sai gốc + thiếu segment (`SyncDataPos\POS`) so với `SyncDataPosController.GetFileFromFTP`. Luôn tra controller
  để lấy đúng `pathSync`/`MapFtpPath`, vì file phải nằm đúng nơi POS tạo/đọc + khớp URL download.
- **Rollout**: file sinh trên host POS.Web nhưng POS tải qua POS.Api → POS.Web `AppSettings:FtpRootPath` phải trỏ
  **chung thư mục vật lý** POS.Api phục vụ (UNC share / cùng volume Docker). Xem `docs/ROLLOUT.md` §O3.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Ops/PosMapPage.razor` (`SyncDataAsync`),
> `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`PushStartOfDayDataAsync`)

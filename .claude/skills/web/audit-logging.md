---
name: web-audit-logging
description: Chuẩn Audit Log bắt buộc cho mọi page Create/Update/Delete trong POS.Web — IAuditLogger, mask dữ liệu nhạy cảm, dialog trả DTO đầy đủ. Đọc khi tạo/sửa page có thao tác CRUD.
---

# Skill: Audit Log — CRUD Operations (POS.Web)

> **Đọc file này khi:** tạo hoặc sửa page/dialog có thao tác **Create, Update, Delete** dữ liệu
> trong POS.Web. Bắt buộc áp dụng cho mọi page CRUD mới.

---

## 1. Khi nào PHẢI ghi log

| Tình huống | Ghi log? |
|-----------|----------|
| Tạo dòng mới (Insert) | ✅ Bắt buộc |
| Sửa dòng (Update) | ✅ Bắt buộc |
| Xóa dòng (Delete) | ✅ Bắt buộc |
| Đọc / xem / filter / export | ❌ Không ghi |
| Thao tác DB thất bại | ❌ Không ghi (chỉ log khi thành công) |

**Nguyên tắc:** đặt lệnh `await AuditLogger.LogAsync(...)` **SAU KHI** xác nhận operation DB trả `true`
(hoặc kết quả thành công tương đương) — không phải trước.

---

## 2. Infrastructure sẵn có

### IAuditLogger — interface dùng trong page

```csharp
// src/POS.Web/Auth/IAuditLogger.cs
public interface IAuditLogger
{
    Task LogAsync(string actor, string action, string entityType,
                  string entityKey, string? oldValueJson = null, string? newValueJson = null);
}
```

Đã đăng ký DI: `builder.Services.AddScoped<IAuditLogger, DbAuditLogger>()` trong `Program.cs`.
**KHÔNG** thêm lại vào DI khi dùng cho page mới.

### Bảng lưu trữ — DashboardAuditLog (RPOSMasterData)

| Column | Kiểu | Ý nghĩa |
|--------|------|---------|
| `Actor` | NVARCHAR(100) | Username người thực hiện |
| `Action` | NVARCHAR(20) | `CREATE` \| `UPDATE` \| `DELETE` |
| `EntityType` | NVARCHAR(50) | Tên thực thể logic (vd `POSDataSetup`) |
| `EntityKey` | NVARCHAR(200) | Giá trị khóa chính |
| `OldValue` | NVARCHAR(MAX) NULL | JSON state trước thay đổi |
| `NewValue` | NVARCHAR(MAX) NULL | JSON state sau thay đổi |
| `ActedAt` | DATETIME2(3) UTC | Tự động (DEFAULT SYSUTCDATETIME()) |

Migration: `src/POS.Web/Auth/migration_dashboard_audit_log.sql` — chạy trên DB trước khi deploy.

---

## 3. Cách inject và gọi

### Inject vào page

```razor
@using POS.Web.Auth
@using Newtonsoft.Json

@inject IAuditLogger AuditLogger
```

### Lấy actor

```csharp
// Trong @code — gọi một lần ở OnInitializedAsync
[CascadingParameter]
private Task<AuthenticationState> AuthState { get; set; } = null!;

private string _currentActor = "unknown";

protected override async Task OnInitializedAsync()
{
    var state = await AuthState;
    _currentActor = state.User.Identity?.Name ?? "unknown";
    // ... rest of init
}
```

### Gọi LogAsync — LUÔN await

```csharp
// CREATE
await AuditLogger.LogAsync(_currentActor, "CREATE", "TênEntityType",
    savedDto.Code,
    oldValueJson: null,
    newValueJson: JsonConvert.SerializeObject(savedDto));

// UPDATE
await AuditLogger.LogAsync(_currentActor, "UPDATE", "TênEntityType",
    item.Code,
    oldValueJson: JsonConvert.SerializeObject(item),   // snapshot TRƯỚC khi sửa
    newValueJson: JsonConvert.SerializeObject(savedDto));

// DELETE
await AuditLogger.LogAsync(_currentActor, "DELETE", "TênEntityType",
    item.Code,
    oldValueJson: JsonConvert.SerializeObject(item),
    newValueJson: null);
```

> **BẮT BUỘC `await`** — không gọi `.Result`, không fire-and-forget.
> try/catch đã có nội bộ trong repository (`InsertDashboardAuditLogAsync`):
> audit failure **không làm gãy main flow**, nhưng vẫn phải await để xử lý đúng thứ tự.

---

## 4. Hằng số action — khuyến nghị

Hiện tại dùng raw string `"CREATE"` / `"UPDATE"` / `"DELETE"`. Khi dự án mở rộng thêm nhiều page,
**nên** tạo class hằng số để tránh lỗi typo:

```csharp
// Đề xuất: src/POS.Web/Auth/AuditActions.cs
namespace POS.Web.Auth;

public static class AuditActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
}
```

> **Hiện tại** file này chưa tồn tại — xác nhận với team trước khi tạo.
> Trong thời gian chờ: dùng đúng string `"CREATE"`, `"UPDATE"`, `"DELETE"` (chữ HOA).

---

## 5. Quy ước oldValue / newValue

### Serialize bằng Newtonsoft.Json (KHÔNG System.Text.Json)

```csharp
// ✅ Đúng
JsonConvert.SerializeObject(item)

// ❌ Sai
System.Text.Json.JsonSerializer.Serialize(item)
```

### Quy ước theo thao tác

| Thao tác | `oldValueJson` | `newValueJson` |
|---------|---------------|---------------|
| CREATE | `null` | JSON của dòng vừa tạo |
| UPDATE | JSON của dòng **TRƯỚC** khi sửa | JSON của dòng **SAU** khi sửa |
| DELETE | JSON của dòng bị xóa | `null` |

### Snapshot oldValue cho UPDATE

Snapshot là biến `item` **đã có sẵn trong page** khi user nhấn nút Sửa — **không cần fetch lại từ DB**.
Page truyền `item` vào dialog, sau khi dialog đóng thành công, `item` vẫn giữ state cũ:

```csharp
private async Task OpenDialogAsync(MyDto? item)  // item = null → CREATE, khác null → EDIT
{
    // ... show dialog ...
    var result = await dialog.Result;
    if (result is { Canceled: false } && result.Data is MyDto savedDto)
    {
        if (item is null)   // CREATE — item chưa tồn tại, không có oldValue
        {
            await AuditLogger.LogAsync(_currentActor, "CREATE", "EntityType",
                savedDto.Key, null, JsonConvert.SerializeObject(savedDto));
        }
        else               // UPDATE — item = state TRƯỚC khi sửa
        {
            await AuditLogger.LogAsync(_currentActor, "UPDATE", "EntityType",
                item.Key,
                JsonConvert.SerializeObject(item),     // ← snapshot cũ
                JsonConvert.SerializeObject(savedDto)); // ← state mới từ dialog
        }
        Snackbar.Add(item is null ? "Thêm thành công!" : "Cập nhật thành công!", Severity.Success);
        await LoadDataAsync();
    }
}
```

---

## 6. Yêu cầu với Form Dialog

Dialog form BẮT BUỘC trả về **DTO đầy đủ** sau khi Save, không phải `true`:

```csharp
// ✅ Đúng — dialog trả DTO để page có newValue
MudDialog.Close(DialogResult.Ok(_model));

// ❌ Sai — page không có newValue để log
MudDialog.Close(DialogResult.Ok(true));
```

Page nhận kết quả:

```csharp
var result = await dialog.Result;
if (result is { Canceled: false } && result.Data is MyDto savedDto)
{
    // savedDto là DTO đầy đủ từ form
}
```

---

## 7. Mask dữ liệu nhạy cảm

**Nếu entity có trường nhạy cảm** (password, token, secret, key, pwd, credential...), phải thay
giá trị bằng `"***"` trước khi serialize để tránh lưu plaintext vào audit log:

```csharp
// Ví dụ — tạo bản sao đã mask trước khi serialize
var masked = new MyDto
{
    Code        = item.Code,
    Description = item.Description,
    Value       = IsSensitiveKey(item.Code) ? "***" : item.Value,  // mask nếu Value là credential
};
JsonConvert.SerializeObject(masked);
```

```csharp
// Heuristic đơn giản để phát hiện key nhạy cảm (inline tạm — TODO: extract helper)
private static bool IsSensitiveKey(string code) =>
    code.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
    code.Contains("SECRET",   StringComparison.OrdinalIgnoreCase) ||
    code.Contains("TOKEN",    StringComparison.OrdinalIgnoreCase) ||
    code.Contains("KEY",      StringComparison.OrdinalIgnoreCase) ||
    code.Contains("PWD",      StringComparison.OrdinalIgnoreCase) ||
    code.Contains("CREDENTIAL", StringComparison.OrdinalIgnoreCase);
```

> **Cập nhật**: helper dùng chung đã có tại `POS.Infrastructure.Logging.SensitiveDataMasker`
> (`IsSensitiveKey(string)` / `Mask(IReadOnlyDictionary<string, object?>)`) — dùng cho
> `IKibanaService.LogException(..., Exception ex, context)` khi cần log chi tiết tham số lỗi qua
> Serilog `{@Context}`. Cho audit log entity (mục này) vẫn có thể tái dùng
> `SensitiveDataMasker.IsSensitiveKey` thay vì tự viết `IsSensitiveKey` inline như ví dụ trên.
> Nếu entity **không có** trường nhạy cảm (vd POSDataSetup.Value là config POS), serialize trực tiếp.

---

## 8. Checklist — dán vào mỗi page CRUD mới

```
□ @inject IAuditLogger AuditLogger
□ @using Newtonsoft.Json
□ _currentActor lấy từ state.User.Identity?.Name ?? "unknown" trong OnInitializedAsync
□ Dialog trả DTO đầy đủ: MudDialog.Close(DialogResult.Ok(_model)) — KHÔNG Ok(true)
□ Snapshot oldValue cho UPDATE: dùng biến item đã có, KHÔNG fetch lại DB
□ await LogAsync NGAY SAU khi DB op thành công, trước Snackbar/reload
□ CREATE: oldValueJson = null, newValueJson = JsonConvert.SerializeObject(savedDto)
□ UPDATE: oldValueJson = JsonConvert.SerializeObject(item), newValueJson = JsonConvert.SerializeObject(savedDto)
□ DELETE: oldValueJson = JsonConvert.SerializeObject(item), newValueJson = null
□ Action string đúng hoa: "CREATE" | "UPDATE" | "DELETE"
□ EntityType: tên thực thể logic nhất quán (vd "POSDataSetup")
□ Mask trường nhạy cảm trước khi serialize (nếu entity có)
```

---

## 9. Reference Implementation

Page chuẩn để copy theo:

- **Page:** `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor`
- **Dialog:** `src/POS.Web/Components/Pages/Ops/Dialogs/PosDataSetupFormDialog.razor`
- **Interface:** `src/POS.Web/Auth/IAuditLogger.cs`
- **Repository method:** `ICentralMDRepository.InsertDashboardAuditLogAsync`

---

## 10. Vận hành — BẮT BUỘC trước khi deploy

Bảng `DashboardAuditLog` phải được tạo trên DB trước khi triển khai tính năng có audit:

```
Database  : RPOSMasterData (CentralMD)
Script    : src/POS.Web/Auth/migration_dashboard_audit_log.sql
Idempotent: có — IF NOT EXISTS, chạy lại an toàn
```

Chạy thủ công hoặc tích hợp vào deploy pipeline. Nếu bảng chưa có mà code đã deploy → ghi log sẽ
fail silently (try/catch nội bộ trong repository) — không crash app, nhưng mất log.

---

## 11. Chained Dialog — Forward payload qua nhiều tầng

> Áp dụng khi: View Dialog (read-only) mở Edit Dialog (CRUD) bên trong; page chính chỉ thấy ViewDialog.

Vấn đề: EditDialog trả `Ok(true)` → ViewDialog không biết giá trị mới → không forward được cho page để audit.

Giải pháp: shared record + forward result.Data

Bước 1 — Record trong file `.cs` riêng (cùng namespace, không trong `@code`):
```csharp
// namespace POS.Web.Components.Pages.{Section}
public record MyEditSavePayload(string FieldA, bool FieldB, string? FieldC);
```

Bước 2 — EditDialog trả record thay vì `true`:
```csharp
MudDialog.Close(DialogResult.Ok(new MyEditSavePayload(fieldA, fieldB, fieldC)));
```

Bước 3 — ViewDialog forward nguyên `result.Data`:
```csharp
var result = await dialog.Result;
if (result is { Canceled: false })
    MudDialog.Close(DialogResult.Ok(result.Data!));
```

Bước 4 — Page capture old snapshot TRUOC khi mo dialog, log sau:
```csharp
var oldJson = JsonConvert.SerializeObject(new { item.FieldA, item.FieldB });
var result = await dialog.Result;
if (result is { Canceled: false } && result.Data is MyEditSavePayload saved)
{
    var newJson = JsonConvert.SerializeObject(new { FieldA = saved.FieldA, FieldB = saved.FieldB });
    await AuditLogger.LogAsync(_currentActor, "UPDATE", "EntityType", item.Key, oldJson, newJson);
}
```

Tham khao: `PosMapPage → PosTerminalDetailDialog → PosTerminalEditDialog`
Record: `src/POS.Web/Components/Pages/Ops/PosTerminalSavePayload.cs`

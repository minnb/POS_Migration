# /web-ui-confirm-dialog — Thêm confirm dialog vào page hoặc component đã có

Dùng lệnh này để thêm **hộp thoại xác nhận** vào một page POS.Web.
Sinh code theo pattern bắt buộc `MudMessageBox @ref` — **KHÔNG** dựng dialog component tùy biến
riêng và **KHÔNG** gọi `DialogService.ShowAsync<MudMessageBox>(...)` (xem
`.claude/skills/web/SKILLS.md` mục "Pattern: MudMessageBox @ref" + `.claude/rules/mudblazor-flat-ui.md` §3
"Bẫy confirm dialog").

---

## Cách dùng

```
/web-ui-confirm-dialog
```

Hoặc cung cấp thông tin ngay:
```
/web-ui-confirm-dialog UsersPage.razor action=DeleteUser severity=Error
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin

**1. File cần thêm vào**
> Đường dẫn: `src/POS.Web/Components/Pages/{Section}/{File}.razor`

**2. Tên hành động cần confirm**
> Ví dụ: "xóa user", "reset config", "tắt POS terminal", "xóa cache"

**3. Bản chất hành động Yes** — quyết định Variant/Color của `<YesButton>`
   (xem bảng Button convention `.claude/rules/mudblazor-flat-ui.md` §3):
- **Phá hủy/không hoàn tác** (xóa, hủy giao dịch, khóa) → `Variant.Outlined` + `Color.Error`
- **Tích cực/chốt luồng, không phá hủy** (kích hoạt, mở khóa, đồng bộ lại, retry) →
  `Variant.Filled` + `Color.Primary` (hoặc `Color.Success`/`Color.Warning` nếu ngữ cảnh cần nhấn
  mạnh cảnh báo, vd "Xác nhận kết thúc ngày")

**4. Tên method thực thi sau khi user xác nhận**
> Ví dụ: `DeleteUserAsync`, `ResetConfigAsync`, `ShutdownTerminalAsync`

**5. Dialog dùng cho 1 hành động cố định hay nhiều hành động khác bản chất (vd khóa/mở khóa)?**
> Nếu nhiều hành động → cần Title/YesText/Color động (xem Bước 3, phần "Title/YesText/Color động").

---

### Bước 2 — Đọc file page hiện tại

Đọc file để xác định:
- Vị trí chèn `<MudMessageBox @ref="...">` — đặt gần đầu content, TRƯỚC mọi `@if` bao ngoài
- Đã có field `_confirmBox`/`_confirmMsg` trùng tên chưa (đổi tên nếu page đã có dialog confirm khác)

---

### Bước 3 — Sinh code

#### Trường hợp tĩnh — 1 hành động cố định

```razor
@* Khai báo trong Razor template — đặt gần đầu content, TRƯỚC mọi @if bao ngoài *@
<MudMessageBox @ref="_confirmBox" Title="Xác nhận xóa user" CancelText="Hủy">
    <MessageContent>@_confirmMsg</MessageContent>
    <YesButton><MudButton Variant="Variant.Outlined" Color="Color.Error">Xóa</MudButton></YesButton>
</MudMessageBox>
```

```csharp
// Thêm vào @code
private MudMessageBox? _confirmBox;
private string _confirmMsg = string.Empty;

private async Task ConfirmDeleteUserAsync(string targetName)
{
    _confirmMsg = $"Bạn có chắc muốn xóa '{targetName}'? Không thể hoàn tác.";
    var ok = await _confirmBox!.ShowAsync();
    if (ok != true) return;

    await DeleteUserAsync(targetName);   // ← thay tên method thực thi
}
```

```razor
@* Thêm vào button trigger trong markup *@
<MudIconButton Icon="@Icons.Material.Filled.Delete"
               Color="Color.Error"
               Size="Size.Small"
               Title="Xóa"
               OnClick="@(() => ConfirmDeleteUserAsync(item.Username))"/>
@* ← thay action và parameter cho đúng *@
```

#### Trường hợp động — dialog dùng chung nhiều hành động khác bản chất (vd khóa/mở khóa)

```razor
<MudMessageBox @ref="_confirmBox" Title="@_confirmTitle" CancelText="Hủy">
    <MessageContent>@_confirmMsg</MessageContent>
    <YesButton>
        <MudButton Variant="@(_confirmYesColor == Color.Success ? Variant.Filled : Variant.Outlined)"
                   Color="@_confirmYesColor">
            @_confirmYesText
        </MudButton>
    </YesButton>
</MudMessageBox>
```

```csharp
private MudMessageBox? _confirmBox;
private string _confirmMsg = string.Empty;
private string _confirmTitle = string.Empty;
private string _confirmYesText = string.Empty;
private Color _confirmYesColor;

private async Task ConfirmToggleAsync(MyItem item)
{
    var locking = item.IsActive;
    _confirmTitle   = locking ? "Xác nhận khóa" : "Xác nhận kích hoạt";
    _confirmMsg     = $"Bạn có chắc muốn {(locking ? "khóa" : "kích hoạt")} '{item.Name}'?";
    _confirmYesText = locking ? "Khóa" : "Kích hoạt";
    _confirmYesColor = locking ? Color.Error : Color.Success;

    var ok = await _confirmBox!.ShowAsync();
    if (ok != true) return;

    await ToggleAsync(item);   // ← thay tên method thực thi
}
```

---

### Bước 4 — Xác nhận

Báo:
- Đã thêm `<MudMessageBox @ref="_confirmBox">` và method confirm vào file nào
- Button trigger đã chèn vào vị trí nào trong markup
- Nếu là audit CRUD (xóa/khóa/kích hoạt...) → nhắc gọi `IAuditLogger.LogAsync(...)` sau khi thao
  tác thành công (xem `.claude/skills/web/audit-logging.md`)

---

## Lưu ý

- **KHÔNG** dùng `IDialogService.ShowMessageBox(...)` — overload đó không tồn tại trong MudBlazor v9.
- **KHÔNG** gọi `DialogService.ShowAsync<MudMessageBox>(title, parameters, options)` — cách này
  render nút Yes bằng markup mặc định của MudBlazor, không có `<YesButton>` slot để chỉnh
  Variant/Color theo bản chất hành động. Đây là lỗi có thật đã xảy ra ở 8 page trong dự án.
- Luôn khai báo `<MudMessageBox @ref="_confirmBox">` trực tiếp trong Razor của page/component —
  KHÔNG tạo dialog component riêng (`PosConfirmDialog` hay tương tự) cho nhu cầu confirm đơn giản.
- Chọn Variant/Color theo đúng bảng Button convention (`.claude/rules/mudblazor-flat-ui.md` §3) —
  không mặc định mọi Yes button là `Color.Error`.
- Không để method thực thi (`{Action}Async`) chạy trước khi `ShowAsync()` trả về — luôn kiểm tra
  `if (ok != true) return;` trước khi gọi hành động thật.

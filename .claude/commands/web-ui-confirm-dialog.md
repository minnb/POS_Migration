# /web-ui-confirm-dialog — Thêm confirm dialog vào page hoặc component đã có

Dùng lệnh này để thêm **hộp thoại xác nhận** (MudDialog) vào một page POS.Web.
Kiểm tra `PosConfirmDialog` đã có chưa — tạo component nếu cần, chỉ sinh phần inject nếu đã có.

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

**3. Mức độ nguy hiểm:**
- `Error` — nguy hiểm, không thể hoàn tác (xóa, reset...)
- `Warning` — cần cẩn thận nhưng có thể phục hồi
- `Info` — thao tác bình thường cần xác nhận

**4. Tên method thực thi sau khi user xác nhận**
> Ví dụ: `DeleteUserAsync`, `ResetConfigAsync`, `ShutdownTerminalAsync`

---

### Bước 2 — Kiểm tra PosConfirmDialog

Kiểm tra file `src/POS.Web/Components/Shared/PosConfirmDialog.razor`:
- **Đã có** → bỏ qua Phần B, chỉ sinh Phần A
- **Chưa có** → sinh cả Phần A và Phần B

---

### Bước 3 — Sinh code

#### Phần A — Thêm vào file đã chọn

```razor
@* Thêm vào phần inject (đầu file, nếu chưa có) *@
@inject IDialogService DialogService
```

```csharp
// Thêm vào @code — method confirm
private async Task Confirm{Action}Async(string targetName)
{
    var parameters = new DialogParameters<PosConfirmDialog>
    {
        { x => x.Title,   "Xác nhận xóa user" },              // ← thay nội dung
        { x => x.Message, $"Bạn có chắc muốn xóa '{targetName}'? Thao tác không thể hoàn tác." },
        { x => x.Color,   Color.Error }                        // ← Color.Error|Warning|Info
    };

    var options = new DialogOptions
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.ExtraSmall,
        FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<PosConfirmDialog>("", parameters, options);
    var result = await dialog.Result;

    if (result is { Canceled: false })
    {
        await {Action}Async(targetName);  // ← thay tên method thực thi
    }
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

---

#### Phần B — Tạo PosConfirmDialog.razor (nếu chưa có)

File: `src/POS.Web/Components/Shared/PosConfirmDialog.razor`

```razor
@using MudBlazor

<MudDialog>
    <TitleContent>
        <MudIcon Icon="@Icons.Material.Filled.Warning"
                 Color="@Color" Class="mr-2" Style="vertical-align:middle"/>
        @Title
    </TitleContent>
    <DialogContent>
        <MudText>@Message</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel" Variant="Variant.Text">Huỷ</MudButton>
        <MudButton OnClick="Confirm"
                   Color="@Color"
                   Variant="Variant.Filled"
                   Class="ml-2">
            Xác nhận
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Title   { get; set; } = "Xác nhận";
    [Parameter] public string Message { get; set; } = "Bạn có chắc muốn thực hiện thao tác này?";
    [Parameter] public Color  Color   { get; set; } = Color.Warning;

    private void Cancel()  => MudDialog.Cancel();
    private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
}
```

---

### Bước 4 — Xác nhận

Báo:
- Đã inject `IDialogService` và thêm method `Confirm{Action}Async` vào file nào
- `PosConfirmDialog.razor` đã tạo hay đã có sẵn
- Button trigger cần chèn vào vị trí nào trong markup

---

## Lưu ý

- `IMudDialogInstance` là interface đúng của MudBlazor v9 — **không** dùng `MudDialogInstance` (v8)
- `[CascadingParameter]` bắt buộc để dialog nhận được instance từ MudDialog provider
- Nếu `PosConfirmDialog` đã tồn tại: không tạo lại, chỉ thêm `@inject IDialogService` và method vào page
- Color `Error` → nút Xác nhận màu đỏ, `Warning` → màu vàng, `Info` → màu xanh
- `DialogResult.Ok(true)` cho phép caller kiểm tra `result.Data` nếu cần trả về thêm data
- Không để method thực thi (`{Action}Async`) trong dialog component — chỉ trả kết quả, page tự xử lý

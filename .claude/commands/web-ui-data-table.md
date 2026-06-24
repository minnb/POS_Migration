# /web-ui-data-table — Thêm data table vào page đã có

Dùng lệnh này để chèn **MudDataGrid** vào một page POS.Web đang có sẵn.
Command hỏi cấu hình cột rồi sinh code đầy đủ kèm search và phân trang.

---

## Cách dùng

```
/web-ui-data-table
```

Hoặc cung cấp thông tin ngay:
```
/web-ui-data-table TransactionPage.razor model=TransactionRowModel
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin

**1. File page cần thêm vào**
> Đường dẫn: `src/POS.Web/Components/Pages/{Section}/{File}.razor`

**2. Tên DTO / Model của từng row**
> Ví dụ: `TransactionRowModel`, `OrderSummaryDto`
> Nếu chưa có → gợi ý tạo file `{Feature}Model.cs` trong `Features/{Section}/{Feature}/`

**3. Các cột — mỗi cột hỏi:**
- Tên hiển thị (header)
- Property name trên model
- Kiểu dữ liệu (`string`, `decimal`, `int`, `DateTime`, `bool`)
- Cột nào là **status** → render `MudChip T="string"` thay `PropertyColumn`
- Cột nào cần **format** (datetime `dd/MM/yyyy HH:mm`, currency VND...)

**4. Có ô search text không?**
- Nếu có: search trên các property nào?

**5. Có phân trang không?**
- Nếu có: số row mặc định mỗi trang? (10 / 20 / 50)

---

### Bước 2 — Đọc file page hiện tại

Đọc file để xác định:
- Vị trí chèn (cuối content, sau chart...)
- List variable nào đã có (`_items`, `_data`...)
- Using namespace nào còn thiếu

---

### Bước 3 — Sinh code

#### Phần Razor (chèn vào markup)

```razor
@* ── Data Table ────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="mt-4">
    <MudDataGrid T="TransactionRowModel"   @* ← thay đúng model type *@
                 Items="@_items"
                 Filterable="false"
                 SortMode="SortMode.Multiple"
                 Pageable="true"
                 PageSize="20"
                 QuickFilter="@_quickFilter"
                 Hover="true" Striped="true" Dense="true">

        <ToolBarContent>
            <MudText Typo="Typo.subtitle1">Danh sách giao dịch</MudText>
            <MudSpacer/>
            <MudTextField @bind-Value="_searchText"
                          Placeholder="Tìm kiếm..."
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Search"
                          IconSize="Size.Small"
                          Immediate="true"
                          Class="mt-0" Style="max-width:250px"/>
        </ToolBarContent>

        <Columns>
            <PropertyColumn Property="x => x.OrderNo"    Title="Số HĐ"/>
            <PropertyColumn Property="x => x.StoreCode"  Title="Cửa hàng"/>
            <PropertyColumn Property="x => x.SaleDate"   Title="Ngày"
                            Format="dd/MM/yyyy HH:mm"/>
            <PropertyColumn Property="x => x.NetAmount"  Title="Thành tiền"
                            Format="N0"/>
            @* Cột status — dùng TemplateColumn *@
            <TemplateColumn Title="Trạng thái" Sortable="false">
                <CellTemplate>
                    <MudChip T="string"
                             Color="@GetStatusColor(context.Item.Status)"
                             Size="Size.Small" Variant="Variant.Filled">
                        @context.Item.Status
                    </MudChip>
                </CellTemplate>
            </TemplateColumn>
        </Columns>

        <NoRecordsContent>
            <MudAlert Severity="Severity.Info" Dense="true" Class="ma-2">
                Không có dữ liệu phù hợp.
            </MudAlert>
        </NoRecordsContent>

        <PagerContent>
            <MudDataGridPager T="TransactionRowModel" PageSizeOptions="new[] { 10, 20, 50, 100 }"/>
        </PagerContent>

    </MudDataGrid>
</MudPaper>
```

> **Pagination chuẩn:** `PageSizeOptions` luôn = `new[] { 10, 20, 50, 100 }` (cho cả `MudTablePager` lẫn `MudDataGridPager`). Phải bắt đầu bằng `10` vì default `RowsPerPage = 10`; thiếu `10` → ô chọn số dòng/trang hỏng.

#### Phần @code (thêm vào code block)

```csharp
// Data list — thêm vào field declarations
private List<TransactionRowModel> _items = [];   // ← thay đúng type
private string _searchText = string.Empty;

// Quick filter function — thêm vào field declarations
private Func<TransactionRowModel, bool> _quickFilter =>
    x => string.IsNullOrWhiteSpace(_searchText)
         || x.OrderNo.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
         || x.StoreCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
         // TODO: thêm properties cần search

// Status color helper — thêm vào cuối @code
private static Color GetStatusColor(string? status) => status switch
{
    "Thành công" or "online"  => Color.Success,
    "Lỗi" or "offline"        => Color.Error,
    "Cảnh báo" or "warning"   => Color.Warning,
    _                         => Color.Default
};

// Trong LoadDataAsync():
// _items = await FeatureService.GetItemsAsync(...);  // TODO: implement
```

---

### Bước 4 — Xác nhận

Báo:
- Vị trí cụ thể cần chèn code (sau dòng nào trong markup)
- Properties cần thêm vào Model nếu chưa có
- QuickFilter cần search trên fields nào

---

## Lưu ý

- `QuickFilter` chạy client-side trên data đã load — với dataset lớn (>500 rows) nên filter server-side qua repository
- `Format="N0"` trên `PropertyColumn` dùng standard .NET format strings
- `TemplateColumn` với `Sortable="false"` cho cột status — không sort theo MudChip
- Nếu cần export Excel → thêm `MudButton` trong `ToolBarContent` gọi method `ExportExcelAsync()` (implement sau)
- Không inject `IDbConnectionFactory` — data phải đến từ Service hoặc Repository

# /web-ui-data-table — Thêm data table vào page đã có

Dùng lệnh này để chèn **MudTable** vào một page POS.Web đang có sẵn.
Command hỏi cấu hình cột rồi sinh code đầy đủ kèm search và phân trang.
Luật dự án bắt buộc `MudTable` — **KHÔNG** `MudDataGrid` (xem `.claude/rules/blazor-web-app.md` §10.B).

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
<MudPaper Elevation="2" Class="mt-4 pa-4">
    <div class="d-flex align-center mb-3">
        <MudText Typo="Typo.subtitle1">Danh sách giao dịch</MudText>
        <MudSpacer/>
        <MudTextField @bind-Value="_searchText"
                      Placeholder="Tìm kiếm..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      IconSize="Size.Small"
                      Immediate="true"
                      Margin="Margin.Dense"
                      Class="mt-0" Style="max-width:250px"/>
    </div>

    <MudTable Items="@_filteredItems" Hover="true" Striped="true" Dense="true"
              Breakpoint="Breakpoint.Sm" HorizontalScrollbar="true" Loading="@_loading">
        <HeaderContent>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.OrderNo)">OrderNo</MudTableSortLabel></MudTh>
            <MudTh>StoreCode</MudTh>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.SaleDate)">SaleDate</MudTableSortLabel></MudTh>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.NetAmount)">NetAmount</MudTableSortLabel></MudTh>
            <MudTh>Trạng thái</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="OrderNo">@context.OrderNo</MudTd>
            <MudTd DataLabel="StoreCode">@context.StoreCode</MudTd>
            <MudTd DataLabel="SaleDate">@context.SaleDate.ToString("yyyy-MM-dd HH:mm:ss")</MudTd>
            <MudTd DataLabel="NetAmount">@context.NetAmount.ToString("N0")</MudTd>
            <MudTd DataLabel="Trạng thái">
                <span class="pos-status-chip @GetStatusChipClass(context.Status)">@context.Status</span>
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudAlert Severity="Severity.Info" Dense="true" Class="ma-2">
                Không có dữ liệu phù hợp.
            </MudAlert>
        </NoRecordsContent>
        <PagerContent>
            <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                           InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                           RowsPerPageString="Số dòng mỗi trang:"/>
        </PagerContent>
    </MudTable>
</MudPaper>
```

> **Pagination chuẩn:** `PageSizeOptions` luôn = `new[] { 10, 20, 50, 100 }` — phải bắt đầu bằng
> `10` vì default `RowsPerPage = 10`; thiếu `10` → ô chọn số dòng/trang hỏng. Chi tiết đầy đủ
> (client/server-side paging, cột động): `.claude/skills/web/datatable.md`.

#### Phần @code (thêm vào code block)

```csharp
// Data list — thêm vào field declarations
private List<TransactionRowModel> _items = [];   // ← thay đúng type
private string _searchText = string.Empty;
private bool _loading = true;

// MudTable không có QuickFilter built-in (khác MudDataGrid) — lọc bằng property tính toán
private List<TransactionRowModel> _filteredItems =>
    string.IsNullOrWhiteSpace(_searchText)
        ? _items
        : _items.Where(x =>
            x.OrderNo.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || x.StoreCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            // TODO: thêm properties cần search
          ).ToList();

// Badge dot-pill class — xem .claude/rules/mudblazor-flat-ui.md §4a (KHÔNG dùng MudChip cho badge tĩnh)
private static string GetStatusChipClass(string? status) => status switch
{
    "Thành công" or "online"  => "pos-status-success",
    "Lỗi" or "offline"        => "pos-status-error",
    "Cảnh báo" or "warning"   => "pos-status-warning",
    _                         => "pos-status-info"
};

// Trong LoadDataAsync():
// _items = await FeatureService.GetItemsAsync(...);  // TODO: implement
// _loading = false;
```

---

### Bước 4 — Xác nhận

Báo:
- Vị trí cụ thể cần chèn code (sau dòng nào trong markup)
- Properties cần thêm vào Model nếu chưa có
- `_filteredItems` cần search trên fields nào

---

## Lưu ý

- `_filteredItems` lọc client-side trên data đã load — với dataset lớn (>500 rows) nên filter server-side qua repository + `MudTable ServerData` (xem `.claude/skills/web/datatable.md`)
- Cột số tiền/ngày format thủ công trong `RowTemplate` (`ToString("N0")`/`ToString("yyyy-MM-dd HH:mm:ss")`) — không có `Format=` attribute như `MudDataGrid`
- Badge trạng thái dùng `<span class="pos-status-chip pos-status-{semantic}">` — KHÔNG `MudChip` (xem `.claude/rules/mudblazor-flat-ui.md` §4a)
- Nếu cần export Excel → thêm `MudButton` phía trên bảng gọi method `ExportExcelAsync()` (implement sau)
- Không inject `IDbConnectionFactory` — data phải đến từ Service hoặc Repository

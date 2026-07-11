# /web-ui-status-grid — Thêm POS status grid vào page Ops

Dùng lệnh này để chèn **grid hiển thị trạng thái POS terminals** vào page Ops đang có sẵn.
Sinh code với filter chips (Online/Offline/Warning) và card grid hoặc data table.

---

## Cách dùng

```
/web-ui-status-grid
```

Hoặc cung cấp thông tin ngay:
```
/web-ui-status-grid HealthPage.razor layout=card
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin

**1. File page cần thêm vào**
> Đường dẫn: `src/POS.Web/Components/Pages/Ops/{File}.razor`

**2. Ngoài status, hiển thị thêm field nào?**
> Gợi ý (chọn nhiều):
> - POS ID / Terminal ID
> - Tên cửa hàng (Store Name)
> - Mã cửa hàng (Store Code)
> - Địa chỉ IP
> - Thời gian kết nối cuối (Last Seen)
> - Phiên bản phần mềm (Version)

**3. Layout:**
- `card` — card grid dạng lưới, nhìn tổng quan nhiều terminal
- `table` — MudTable chi tiết có sort/filter/page

**4. Có filter bar theo trạng thái không?**
- Nếu có: filter chips: Tất cả / Online / Offline / Cảnh báo (với số đếm)

---

### Bước 2 — Đọc file page hiện tại

Đọc file để xác định:
- Vị trí chèn
- Model/DTO đã dùng chưa
- Using namespace thiếu

---

### Bước 3 — Sinh code

#### Phần Razor — Filter bar (nếu chọn)

```razor
@* ── Status Filter ──────────────────────────────────────────────────── *@
<MudPaper Elevation="0" Class="mb-3 d-flex align-center gap-2" Style="background:transparent;">
    <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mr-1">Trạng thái:</MudText>
    @foreach (var (label, filter) in _statusFilters)
    {
        var count = filter == null
            ? _allTerminals.Count
            : _allTerminals.Count(t => t.Status == filter);
        <MudChip T="string"
                 Color="@(_statusFilter == filter ? Color.Primary : Color.Default)"
                 Variant="@(_statusFilter == filter ? Variant.Filled : Variant.Outlined)"
                 OnClick="@(() => FilterStatus(filter))"
                 Size="Size.Small">
            @label (@count)
        </MudChip>
    }
</MudPaper>
```

#### Phần Razor — Card grid layout

```razor
@* ── Terminal Card Grid ──────────────────────────────────────────────── *@
@if (_filteredTerminals.Count == 0)
{
    <MudAlert Severity="Severity.Info">Không có terminal nào phù hợp.</MudAlert>
}
else
{
    <MudGrid>
        @foreach (var terminal in _filteredTerminals)
        {
            <MudItem xs="12" sm="6" md="4" lg="3">
                <MudPaper Elevation="2" Class="pa-3"
                          Style="@($"border-left:4px solid {GetStatusBorderColor(terminal.Status)}")">
                    <div class="d-flex align-center justify-space-between mb-1">
                        <MudText Typo="Typo.subtitle2">@terminal.TerminalId</MudText>
                        <MudChip T="string"
                                 Color="@GetStatusChipColor(terminal.Status)"
                                 Size="Size.Small" Variant="Variant.Filled">
                            @terminal.Status
                        </MudChip>
                    </div>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">@terminal.StoreName</MudText>
                    <MudText Typo="Typo.caption" Color="Color.Tertiary" Class="mt-1">
                        @terminal.IpAddress — Last seen: @terminal.LastSeen?.ToString("HH:mm")
                    </MudText>
                </MudPaper>
            </MudItem>
        }
    </MudGrid>
}
```

#### Phần Razor — Table layout

> Dùng `MudTable` (KHÔNG `MudDataGrid`) — luật bắt buộc dự án, xem
> `.claude/rules/blazor-web-app.md` §10.B + `.claude/skills/web/datatable.md`.

```razor
@* ── Terminal Table ──────────────────────────────────────────────────── *@
<MudTable Items="@_filteredTerminals" Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<PosTerminalModel, object>(x => x.TerminalId)">Terminal ID</MudTableSortLabel></MudTh>
        <MudTh>Cửa hàng</MudTh>
        <MudTh>IP</MudTh>
        <MudTh>Last Seen</MudTh>
        <MudTh>Trạng thái</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Terminal ID">@context.TerminalId</MudTd>
        <MudTd DataLabel="Cửa hàng">@context.StoreCode</MudTd>
        <MudTd DataLabel="IP">@context.IpAddress</MudTd>
        <MudTd DataLabel="Last Seen">@(context.LastSeen?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—")</MudTd>
        <MudTd DataLabel="Trạng thái">
            <span class="pos-status-chip @GetStatusChipClass(context.Status)">@context.Status</span>
        </MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudAlert Severity="Severity.Info" Dense="true" Class="ma-2">
            Không có terminal nào.
        </MudAlert>
    </NoRecordsContent>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                       InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                       RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

#### Phần @code (thêm vào code block)

```csharp
// ── Terminal fields ───────────────────────────────────────────────────
private List<PosTerminalModel> _allTerminals     = [];
private List<PosTerminalModel> _filteredTerminals = [];
private string? _statusFilter;   // null = Tất cả

private readonly (string Label, string? Filter)[] _statusFilters =
[
    ("Tất cả",   null),
    ("Online",   "online"),
    ("Offline",  "offline"),
    ("Cảnh báo", "warning"),
];

// ── Filter ────────────────────────────────────────────────────────────
private void FilterStatus(string? status)
{
    _statusFilter     = status;
    _filteredTerminals = status == null
        ? _allTerminals
        : _allTerminals.Where(t => t.Status == status).ToList();
}

// ── Color helpers ────────────────────────────────────────────────────
private static Color GetStatusChipColor(string? status) => status switch
{
    "online"  => Color.Success,
    "offline" => Color.Error,
    "warning" => Color.Warning,
    _         => Color.Default
};

private static string GetStatusBorderColor(string? status) => status switch
{
    "online"  => "var(--mud-palette-success)",
    "offline" => "var(--mud-palette-error)",
    "warning" => "var(--mud-palette-warning)",
    _         => "var(--mud-palette-divider)"
};

// Dùng cho badge dot-pill trong MudTable — xem .claude/rules/mudblazor-flat-ui.md §4a
private static string GetStatusChipClass(string? status) => status switch
{
    "online"  => "pos-status-success",
    "offline" => "pos-status-error",
    "warning" => "pos-status-warning",
    _         => "pos-status-info"
};

// ── Trong LoadDataAsync() ────────────────────────────────────────────
// _allTerminals      = await FeatureService.GetTerminalsAsync(ct);  // TODO: implement
// _filteredTerminals = _allTerminals;
// _isEmpty = _allTerminals.Count == 0;
```

#### Model (tạo nếu chưa có)

```csharp
// src/POS.Web/Features/Ops/{Feature}/PosTerminalModel.cs
namespace POS.Web.Features.Ops.{Feature};

public class PosTerminalModel
{
    public string TerminalId { get; init; } = string.Empty;
    public string StoreCode  { get; init; } = string.Empty;
    public string StoreName  { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string Status     { get; init; } = "offline";   // "online"|"offline"|"warning"
    public DateTime? LastSeen { get; init; }
    public string? Version   { get; init; }
    // thêm fields đã chọn ở Bước 1
}
```

---

### Bước 4 — Xác nhận

Báo:
- File đã chỉnh sửa và vị trí chèn
- Model cần tạo (nếu chưa có)
- Method trong Service / Repository cần implement để lấy terminal status

---

## Lưu ý

- Page Ops không cần row-level filter — ITOps và Admin xem tất cả terminal
- Status value: dùng lowercase `"online"` / `"offline"` / `"warning"` nhất quán với color helpers
- Card grid phù hợp khi có < 50 terminal; table tốt hơn khi nhiều hơn
- `FilterStatus()` filter client-side — phù hợp vì data terminal load 1 lần

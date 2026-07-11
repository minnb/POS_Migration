# /web-ui-chart — Thêm biểu đồ vào page đã có

Dùng lệnh này để chèn **biểu đồ MudBlazor v9** vào một page POS.Web đang có sẵn.
Sinh code đúng syntax v9 — tránh các lỗi breaking changes từ v8.

---

## Cách dùng

```
/web-ui-chart
```

Hoặc cung cấp thông tin ngay:
```
/web-ui-chart RevenuePage.razor type=Line title="Doanh thu theo ngày"
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin

**1. File page cần thêm vào**
> Đường dẫn: `src/POS.Web/Components/Pages/{Section}/{File}.razor`

**2. Loại biểu đồ**
- `Line` — xu hướng theo thời gian (doanh thu ngày/tháng, số đơn...)
- `Bar` — so sánh (theo cửa hàng, theo giờ, theo danh mục...)
- Cả hai — 2 chart cạnh nhau trong MudGrid

**3. Tiêu đề chart** (hiển thị trên MudText subtitle)

**4. Trục X** — nhãn là gì?
> Ví dụ: ngày (`dd/MM`), giờ (`00h–23h`), cửa hàng (store code), danh mục...

**5. Trục Y** — đơn vị là gì?
> Ví dụ: triệu đồng, số lượng đơn, phần trăm...
> Ghi chú trong tiêu đề chart nếu cần (ví dụ: "Doanh thu theo ngày (triệu đồng)")

**6. Có nhiều series không?**
- Nếu có: bao nhiêu series? Tên từng series?
> Ví dụ: "Doanh thu" + "Lợi nhuận", hoặc "Bán ra" + "Trả hàng"

---

### Bước 2 — Đọc file page hiện tại

Đọc file để xác định:
- Vị trí chèn (sau KPI row, trước table...)
- `@using MudBlazor.Charts` đã có chưa
- `_loading`, `_errorMsg` đã khai báo chưa

---

### Bước 3 — Sinh code

#### Phần using (thêm vào đầu file nếu chưa có)

```razor
@using MudBlazor.Charts
```

#### Phần Razor — Line chart

```razor
@* ── Line Chart ─────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="pa-4 mb-4">
    <MudText Typo="Typo.subtitle1" Class="mb-3">
        <MudIcon Icon="@Icons.Material.Filled.ShowChart" Size="Size.Small" Class="mr-1"/>
        Doanh thu theo ngày (triệu đồng)     @* ← thay tiêu đề *@
    </MudText>
    @if (_loading)
    {
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="280px"/>
    }
    else if (_chartEmpty)
    {
        <MudAlert Severity="Severity.Info" Dense="true">
            Không có dữ liệu trong khoảng thời gian này.
        </MudAlert>
    }
    else
    {
        <Line T="double"
              ChartSeries="@_lineSeries"
              ChartLabels="@_lineLabels"
              Width="100%" Height="280px"
              ChartOptions="@_lineOpts"/>
    }
</MudPaper>
```

#### Phần Razor — Bar chart

```razor
@* ── Bar Chart ──────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="pa-4 mb-4">
    <MudText Typo="Typo.subtitle1" Class="mb-3">
        <MudIcon Icon="@Icons.Material.Filled.BarChart" Size="Size.Small" Class="mr-1"/>
        Doanh thu theo giờ (triệu đồng)      @* ← thay tiêu đề *@
    </MudText>
    @if (_loading)
    {
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="280px"/>
    }
    else if (_barEmpty)
    {
        <MudAlert Severity="Severity.Info" Dense="true">Không có dữ liệu.</MudAlert>
    }
    else
    {
        <Bar T="double"
             ChartSeries="@_barSeries"
             ChartLabels="@_barLabels"
             Width="100%" Height="280px"
             ChartOptions="@_barOpts"/>
    }
</MudPaper>
```

#### Phần @code (thêm vào code block)

```csharp
// ── Chart fields — thêm vào field declarations ───────────────────────
private bool _chartEmpty = true;    // Line
private bool _barEmpty   = true;    // Bar (bỏ nếu không dùng)

private List<ChartSeries<double>> _lineSeries =
[
    new ChartSeries<double>
    {
        Name = "Doanh thu",                              // ← thay tên series
        Data = new ChartData<double>(Array.Empty<double>())
    }
    // thêm series nếu nhiều series:
    // new ChartSeries<double> { Name = "Lợi nhuận", Data = new ChartData<double>(Array.Empty<double>()) }
];

private string[] _lineLabels = [];

private List<ChartSeries<double>> _barSeries =
[
    new ChartSeries<double>
    {
        Name = "Doanh thu",
        Data = new ChartData<double>(Array.Empty<double>())
    }
];

private string[] _barLabels =
    Enumerable.Range(0, 24).Select(h => $"{h:00}h").ToArray();  // ← thay nếu khác trục X

private readonly LineChartOptions _lineOpts = new()
{
    LineStrokeWidth = 2,
    ShowLegend = false,   // true nếu nhiều series
};

private readonly BarChartOptions _barOpts = new()
{
    ShowLegend = false,
};

// ── BuildChartData — gọi trong LoadDataAsync() ────────────────────────
private void BuildChartData(List<SomeDto> data)
{
    _chartEmpty = data.Count == 0;
    if (_chartEmpty) return;

    _lineLabels = data.Select(d => d.Date.ToString("dd/MM")).ToArray();  // ← thay format
    var values  = data.Select(d => Math.Round((double)(d.Amount / 1_000_000m), 1)).ToArray();
    _lineSeries =
    [
        new ChartSeries<double>
        {
            Name = "Doanh thu",
            Data = new ChartData<double>(values)
        }
    ];
}
```

---

### Bước 4 — Xác nhận

Báo:
- Vị trí cụ thể cần chèn (sau dòng nào)
- Fields nào cần thêm vào `@code`
- Method `BuildChartData` cần data từ repository nào

---

## Lưu ý quan trọng — MudBlazor v9

> Chi tiết đầy đủ breaking changes v8→v9 (bảng so sánh, Y-axis auto-scale...):
> **`.claude/skills/web/charts.md`** — đọc file đó nếu code sinh ra không compile hoặc cần biến thể
> khác (bar-list, nhiều series).

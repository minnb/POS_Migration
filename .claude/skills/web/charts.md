---
name: web-charts
description: Thêm biểu đồ Line/Bar bằng MudBlazor v9 (cú pháp đổi hoàn toàn so với v8) — ChartSeries, ChartData, LineChartOptions/BarChartOptions, Y-axis auto-scale. Đọc trước khi thêm chart vào page.
---

# Charts — MudBlazor v9 (Line / Bar)

> **Áp dụng khi:** thêm biểu đồ đường/cột vào page POS.Web.
> MudBlazor 9.5.0 đổi hoàn toàn cú pháp chart so với v8 — đọc kỹ phần breaking changes.

---

## MudBlazor v9 — breaking changes

> Bảng đối chiếu v8 (sai) → v9 (đúng) đầy đủ là **Rule** ở **`.claude/rules/blazor-web-app.md` §6**
> (`<Line T="double">`/`<Bar T="double">` thay `<MudChart>`, `ChartData<double>`, `LineChartOptions`/
> `BarChartOptions`, `ChartLabels`...). File này giữ code mẫu dùng đúng v9 (HOW) bên dưới.

```razor
@using MudBlazor.Charts

<Line T="double" ChartSeries="@_series" ChartLabels="@_labels"
      Width="100%" Height="280px" ChartOptions="@_lineOpts"/>

<Bar T="double" ChartSeries="@_series" ChartLabels="@_labels"
     Width="100%" Height="280px" ChartOptions="@_barOpts"/>
```

```csharp
// ChartSeries<T> — khai báo đúng v9
private List<ChartSeries<double>> _series =
[
    new ChartSeries<double>
    {
        Name = "Label",
        Data = new ChartData<double>(Array.Empty<double>())  // constructor bắt buộc
    }
];

private readonly LineChartOptions _lineOpts = new() { LineStrokeWidth = 2, ShowLegend = false };
private readonly BarChartOptions  _barOpts  = new() { ShowLegend = false };
private bool _isEmpty;   // kiểm tra empty qua flag — KHÔNG qua .Data.Length
```

---

## Pattern: Y-axis auto-scale theo dữ liệu thực tế (margin ~5%)

> Áp dụng khi: chart Bar/Line hiển thị data nhỏ (vài triệu đồng) mà trục Y luôn max=20, hoặc cột
> cao nhất chỉ chiếm 40-50% chiều cao trục Y (buffer cộng/nhân cố định quá lớn so với data thật).

**Nguyên nhân gốc:** `BarChartOptions.YAxisTicks`/`LineChartOptions.YAxisTicks` là kiểu `Int32`,
đại diện *khoảng cách giữa gridline* (spacing), **không phải số lượng tick**. MudBlazor render đỉnh
trục Y = `spacing × ceil(max(data, YAxisSuggestedMax) / spacing)`. Nếu chọn buffer/spacing không
tỷ lệ với data thật (buffer cộng cố định `+2.5`, hoặc bảng spacing cứng theo ngưỡng `≤5/≤10/≤20`),
kết quả bị làm tròn lên một mốc cao hơn nhiều so với max thật → cột/đường trông rất nhỏ so với trục.

**Giải pháp:** dùng margin theo **%** của giá trị max thật (mặc định 5%) thay vì buffer cố định,
chia đều thành N gridline (mặc định 5) để suy ra `YAxisTicks` (spacing) sát nhất có thể:

```csharp
// Sau khi tính xong mảng values, trước khi set BarChartOptions/LineChartOptions:
var (yMax, yTick) = CalcYAxis(values);
_barOpts = new BarChartOptions { ShowLegend = false, YAxisSuggestedMax = yMax, YAxisTicks = yTick };

// Margin ~5% quanh giá trị max — cột/đường luôn sát đỉnh trục Y.
// Ghi chú: YAxisTicks là Int32 → với max rất nhỏ (vd <10), spacing tối thiểu =1 có thể tạo margin
// thực tế lớn hơn 5% (giới hạn kỹ thuật của thư viện, không khắc phục được bằng công thức).
private static (double YMax, int YTick) CalcYAxis(double[] values, double marginPct = 0.05, int gridLines = 5)
{
    var max = values.Length > 0 ? values.Max() : 0;
    if (max <= 0) return (5, 1);

    var target = max * (1 + marginPct);
    var tick   = Math.Max(1, (int)Math.Ceiling(target / gridLines));
    var yMax   = tick * (int)Math.Ceiling(target / tick);
    return (yMax, tick);
}
```

> `YAxisSuggestedMax` là "gợi ý" — nếu data vượt qua, MudBlazor tự mở rộng (không clip).
> Với data max rất nhỏ (< ~10 đơn vị hiển thị), margin thực tế có thể lớn hơn 5% do `YAxisTicks`
> không nhận spacing dưới 1 — đây là giới hạn của MudBlazor, không phải lỗi công thức.

**Ví dụ thực tế đã áp dụng:** `RevenuePage.razor` (2 Bar), `RevenueHourlyPage.razor` (1 Line + 2 Bar),
`ShiftSummaryPage.razor` (1 Bar).

---

## Anti-patterns

- ❌ Dùng `<MudChart ChartType="...">` (v8 syntax) → compile error với MudBlazor 9.5.0
- ❌ Dùng `ChartOptions { YAxisTicks, LineStrokeWidth }` → đã đổi sang `LineChartOptions` / `BarChartOptions` trong v9
- ❌ `BarChartOptions { ShowLegend = false }` không set `YAxisSuggestedMax` → `YAxisTicks` default=20 (spacing!) làm Y-axis luôn max=20 dù data chỉ 2–8M
- ❌ Buffer cộng cố định (`max + 2.5`) hoặc bảng spacing cứng theo ngưỡng tuyệt đối (`≤5→1, ≤10→2...`)
  → không tỷ lệ với data thật, dễ khiến cột chỉ chiếm 40-50% chiều cao trục khi max nhỏ. Dùng
  `CalcYAxis` (margin %) ở trên thay thế.
- ❌ `ChartSeries<double>="@..."` như HTML attribute trong Razor (v9 syntax sai)

---

## Pattern: CSS bar list (horizontal) — thay horizontal bar / treemap

> Áp dụng khi: cần "Top N" dạng thanh ngang, hoặc dual-metric (DT + SL) — MudBlazor v9 **chỉ có Bar dọc + Line**,
> KHÔNG có horizontal bar 2 trục hay treemap. Tự dựng bằng CSS, không thêm thư viện.

```razor
@using System.Globalization
@foreach (var p in _items)
{
    <div style="display:flex; align-items:center; gap:12px; margin-bottom:10px;">
        <div style="width:30px; text-align:center; font-weight:700;">@p.Rank</div>
        <div style="flex:1; min-width:0;">
            <div style="display:flex; justify-content:space-between; font-size:0.8rem;">
                <span style="overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">@p.Name</span>
                <span>@FormatVND(p.Revenue) · @p.Qty.ToString("N0")</span>
            </div>
            <div style="background:var(--mud-palette-action-disabled-background); height:14px; border-radius:4px; overflow:hidden;">
                <div style="height:100%; @BarWidthStyle(p) background:@BarColor(p); border-radius:4px;"></div>
            </div>
        </div>
    </div>
}
```

```csharp
// BẮT BUỘC InvariantCulture cho width:% — culture VN dùng dấu phẩy thập phân → "18,5%" phá CSS.
private string BarWidthStyle(T p)
{
    var pct = _max > 0 ? (double)(Value(p) / _max) * 100d : 0d;
    return $"width:{pct.ToString("0.##", CultureInfo.InvariantCulture)}%;";
}
```

**Anti-pattern:** format `width:@pct%` trực tiếp trong culture VN → `18,5%` (sai CSS, thanh không vẽ).

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/TopProductPage.razor`

---

## Ví dụ thực tế

| Loại | File |
|---|---|
| Line + Bar + KPI + Y-axis auto-scale | `src/POS.Web/Components/Pages/Store/RevenuePage.razor` |
| Nhiều chart (line/bar theo giờ/thứ) | `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor` |
| CSS bar list (Top N) + drill-through | `src/POS.Web/Components/Pages/Store/TopProductPage.razor` |

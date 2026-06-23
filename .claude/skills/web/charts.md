# Charts — MudBlazor v9 (Line / Bar)

> **Áp dụng khi:** thêm biểu đồ đường/cột vào page POS.Web.
> MudBlazor 9.5.0 đổi hoàn toàn cú pháp chart so với v8 — đọc kỹ phần breaking changes.

---

## MudBlazor v9 — breaking changes bắt buộc biết

| Thứ | v8 (sai — không dùng) | v9 (đúng) |
|---|---|---|
| Chart component | `<MudChart ChartType="ChartType.Line">` | `<Line T="double">` hoặc `<Bar T="double">` |
| Series attribute | `ChartSeries<double>="@..."` như HTML attr | `ChartSeries="@..."` với `T="double"` trên tag |
| X-axis labels | `XAxisLabels` | `ChartLabels` |
| Data type | `double[]` | `new ChartData<double>(double[])` |
| Options (line) | `ChartOptions { LineStrokeWidth, YAxisTicks }` | `LineChartOptions { LineStrokeWidth, ShowLegend }` |
| Options (bar) | `ChartOptions { YAxisTicks }` | `BarChartOptions { ShowLegend }` |
| Empty check | `series[0].Data.Length == 0` | bool flag set trong LoadData |
| Chip | `<MudChip Color="...">` | `<MudChip T="string" Color="...">` |

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

## Pattern: Y-axis auto-scale theo dữ liệu thực tế

> Áp dụng khi: chart Bar/Line hiển thị data nhỏ (vài triệu đồng) mà trục Y luôn max=20.

**Nguyên nhân:** `BarChartOptions.YAxisTicks` default = **20** là *khoảng cách giữa tick*, không phải số lượng tick.
Khi data max = 8M và spacing = 20 → MudBlazor vẽ tick 0 và 20 → trục Y nhìn cứng max=20.

**Giải pháp:** tính `YAxisSuggestedMax` và `YAxisTicks` sau khi có data:

```csharp
// Sau khi tính xong mảng values, trước khi set BarChartOptions:
var yMax = CalcYMax(values);
_barOpts = new BarChartOptions { ShowLegend = false, YAxisSuggestedMax = yMax, YAxisTicks = CalcYTick(yMax) };

private static double CalcYMax(double[] values)
{
    var max = values.Length > 0 ? values.Max() : 0;
    if (max <= 0) return 5;
    return Math.Ceiling(max + 2.5);   // buffer ~2.5 đơn vị, làm tròn lên
}

private static int CalcYTick(double yMax)
{
    if (yMax <= 5)  return 1;
    if (yMax <= 10) return 2;
    if (yMax <= 20) return 5;
    return 10;
}
```

> `YAxisSuggestedMax` là "gợi ý" — nếu data vượt qua, MudBlazor tự mở rộng (không clip).

---

## Anti-patterns

- ❌ Dùng `<MudChart ChartType="...">` (v8 syntax) → compile error với MudBlazor 9.5.0
- ❌ Dùng `ChartOptions { YAxisTicks, LineStrokeWidth }` → đã đổi sang `LineChartOptions` / `BarChartOptions` trong v9
- ❌ `BarChartOptions { ShowLegend = false }` không set `YAxisSuggestedMax` → `YAxisTicks` default=20 (spacing!) làm Y-axis luôn max=20 dù data chỉ 2–8M
- ❌ `ChartSeries<double>="@..."` như HTML attribute trong Razor (v9 syntax sai)

---

## Ví dụ thực tế

| Loại | File |
|---|---|
| Line + Bar + KPI + Y-axis auto-scale | `src/POS.Web/Components/Pages/Store/RevenuePage.razor` |
| Nhiều chart (line/bar theo giờ/thứ) | `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor` |

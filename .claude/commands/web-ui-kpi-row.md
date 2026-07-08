# /web-ui-kpi-row — Thêm hàng KPI cards vào page đã có

Dùng lệnh này để chèn **KPI summary row** vào một page POS.Web đang có sẵn.
Command hỏi thông tin rồi sinh code sẵn sàng paste vào đúng vị trí.

> **LUẬT THÉP**: đây là khuôn mẫu **BẮT BUỘC** — xem CLAUDE.md §"POS.Web" mục 0 và
> `.claude/rules/mudblazor-flat-ui.md` mục 11 "KPI card — khuôn mẫu chuẩn". KHÔNG tự viết
> `MudGrid`/`MudPaper` tùy ý khi thêm KPI row.

---

## Cách dùng

```
/web-ui-kpi-row
```

Hoặc cung cấp thông tin ngay:
```
/web-ui-kpi-row RevenuePage.razor kpis=3
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin

**1. File page cần thêm vào**
> Ví dụ: `RevenuePage.razor`, `TransactionPage.razor`
> Đường dẫn đầy đủ: `src/POS.Web/Components/Pages/{Section}/{File}.razor`

**2. Có bao nhiêu KPI?** (2, 3, hoặc 4)

**3. Mỗi KPI — hỏi lần lượt:**
- Tên label hiển thị (ví dụ: "Doanh thu hôm nay", "Số hóa đơn")
- Property name trong ViewModel (ví dụ: `TodayRevenue`, `OrderCount`)
- Format: `currency` | `number` | `percent`
- Màu accent (semantic): `Primary` | `Success` | `Error` | `Warning` | `Info` | `Tertiary`

**4. Có icon minh họa trong card không?** (Variant B — chỉ dùng cho Ops/Admin dashboard dạng
cấu hình/tổng quan, KHÔNG dùng cho report doanh thu)

**5. Có trend so sánh kỳ trước không?**
- Nếu có: property name của giá trị kỳ trước (ví dụ: `PreviousRevenue`), và có phải delta dạng
  "điểm %" không (percentage-point, vd tỷ lệ %) hay dạng tăng trưởng % thông thường

---

### Bước 2 — Đọc file page hiện tại

Đọc file page để xác định:
- Vị trí chèn (sau page header / filter panel, trước bảng/chart)
- Các `_data` fields đã có để không bị trùng tên
- `_Imports.razor` đã có `@using POS.Web.Components.Shared` (để dùng `<PosDeltaBadge>` không cần
  `@using` riêng trong page) — nếu chưa, thêm dòng đó vào `_Imports.razor` (dùng chung toàn app)

---

### Bước 3 — Sinh code

#### Variant A — không icon (mặc định, dùng cho report/dashboard số liệu)

```razor
@* ── KPI Cards ─────────────────────────────────────────────────────── *@
<div class="d-flex flex-wrap gap-3 mb-4">
    <div style="flex:1 1 140px">
        <MudPaper Elevation="2" Class="pa-4 text-center" Style="border-left:4px solid var(--mud-palette-primary)">
            <MudText Typo="Typo.h5" Class="pos-kpi-value" Color="Color.Primary">
                @FormatCurrency(_data.TodayRevenue)   @* ← thay đúng property + format helper *@
            </MudText>
            <MudText Typo="Typo.body2" Class="pos-kpi-label" Color="Color.Secondary">Doanh thu hôm nay</MudText>
            @* Nếu có trend so sánh kỳ trước — bỏ khối này nếu không có *@
            <PosDeltaBadge Current="_data.TodayRevenue" Previous="_data.PreviousRevenue"
                           Enabled="_compareEnabled" Class="mt-1"/>
        </MudPaper>
    </div>
    @* ... lặp cho từng KPI, đổi border-left + Color theo semantic đã chọn ... *@
</div>
```

#### Variant B — có icon minh họa (Ops/Admin — vd tổng quan cấu hình/tài khoản)

```razor
<div class="d-flex flex-wrap gap-3 mb-4">
    <div style="flex:1 1 160px">
        <MudPaper Elevation="2" Class="pa-4 pos-kpi-card-icon" Style="border-left:4px solid var(--mud-palette-primary)">
            <div>
                <MudText Typo="Typo.body2" Class="pos-kpi-label" Color="Color.Secondary">Tổng cấu hình</MudText>
                <MudText Typo="Typo.h5" Class="pos-kpi-value" Color="Color.Primary">@_data.TotalCount</MudText>
            </div>
            <MudIcon Icon="@Icons.Material.Filled.Settings" Class="pos-kpi-icon" Style="color:var(--mud-palette-primary)"/>
        </MudPaper>
    </div>
    @* ... lặp cho từng KPI ... *@
</div>
```

#### Phần @code (thêm vào code block)

```csharp
// KPI fields — thêm vào ViewModel hoặc trực tiếp nếu đơn giản
// Chèn vào LoadDataAsync():
// _data.TodayRevenue = summaryResult.TodayRevenue;  // TODO: gọi thật

// Nếu có trend — cần cờ bật/tắt so sánh kỳ trước (thường đi kèm MudSwitch "So sánh kỳ trước")
private bool _compareEnabled;

// Format helpers — thêm vào cuối @code nếu chưa có
private static string FormatCurrency(decimal amount) => amount switch
{
    >= 1_000_000_000m => $"{amount / 1_000_000_000m:N1} tỷ",
    >= 1_000_000m     => $"{amount / 1_000_000m:N0} triệu",
    _                 => $"{amount:N0} ₫"
};

private static string FormatNumber(decimal value)   => value.ToString("N0");
private static string FormatPercent(decimal value)  => $"{value:F1}%";
```

**KHÔNG** viết `RenderFragment TrendBadge()` riêng trong page — luôn dùng
`<PosDeltaBadge Current="..." Previous="..." Enabled="..." LowerIsBetter="..." AsPercentPoint="..."/>`
(component có sẵn tại `Components/Shared/PosDeltaBadge.razor`).

---

### Bước 4 — Xác nhận

Báo:
- Vị trí cụ thể cần chèn code Razor (dòng bao nhiêu, sau element nào)
- Properties cần thêm vào ViewModel (nếu chưa có)
- Method nào trong Service cần cập nhật để trả về KPI data

---

## Lưu ý

- Wrapper KPI row luôn `d-flex flex-wrap gap-3 mb-4` + `div[style="flex:1 1 Npx"]` mỗi card —
  **KHÔNG** dùng `MudGrid`/`MudItem` (đã đổi hẳn sang flex-wrap từ 2026-07-08, xem
  `.claude/rules/mudblazor-flat-ui.md` mục 11).
- `flex:1 1 Npx` — N tùy độ dài nội dung: card số ngắn (~120-140px), card tiền tệ dài hơn
  (~150-160px), card có icon (~160px).
- Border-left color dùng CSS var: `--mud-palette-primary`, `--mud-palette-success`,
  `--mud-palette-error`, `--mud-palette-warning`, `--mud-palette-info`, `--mud-palette-tertiary` —
  **KHÔNG** hardcode hex, luôn khớp với `Color=` trên `MudText` giá trị.
- Value **luôn** `Typo="Typo.h5" Class="pos-kpi-value"`; Label **luôn** `Typo="Typo.body2"
  Class="pos-kpi-label"` — không dùng `h4`/`h6`/`caption`.
- Trend/delta → `<PosDeltaBadge>` đã có sẵn trong `Components/Shared/` — không viết lại logic.
- KPI data nên load song song với data khác bằng `Task.WhenAll`.

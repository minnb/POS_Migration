# /web-ui-kpi-row — Thêm hàng KPI cards vào page đã có

Dùng lệnh này để chèn **KPI summary row** vào một page POS.Web đang có sẵn.
Command hỏi thông tin rồi sinh code sẵn sàng paste vào đúng vị trí.

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
- Màu accent: `Primary` | `Success` | `Error` | `Warning` | `Info`

**4. Có trend so sánh kỳ trước không?**
- Nếu có: property name của giá trị kỳ trước (ví dụ: `PreviousRevenue`)

---

### Bước 2 — Đọc file page hiện tại

Đọc file page để xác định:
- Vị trí chèn (sau PageHeader, trước filter/chart)
- Các `_data` fields đã có để không bị trùng tên
- Namespace/using đã có

---

### Bước 3 — Sinh code

#### Phần Razor (chèn vào markup)

```razor
@* ── KPI Cards ─────────────────────────────────────────────────────── *@
<MudGrid Class="mb-4">
    <MudItem xs="12" sm="6" md="3">
        <MudPaper Elevation="2" Class="pa-4"
                  Style="border-left:4px solid var(--mud-palette-primary);">
            <MudText Typo="Typo.caption" Color="Color.Secondary">Doanh thu hôm nay</MudText>
            <MudText Typo="Typo.h5" Color="Color.Primary" Class="mt-1">
                @FormatCurrency(_data.TodayRevenue)   @* ← thay đúng property *@
            </MudText>
            @if (_data.PreviousRevenue > 0)            @* ← bỏ nếu không có trend *@
            {
                var diff = _data.TodayRevenue - _data.PreviousRevenue;
                <MudText Typo="Typo.caption" Color="@(diff >= 0 ? Color.Success : Color.Error)">
                    @(diff >= 0 ? "▲" : "▼") @Math.Abs(diff / _data.PreviousRevenue * 100m):F1}%
                </MudText>
            }
        </MudPaper>
    </MudItem>
    @* ... lặp cho từng KPI ... *@
</MudGrid>
```

#### Phần @code (thêm vào code block)

```csharp
// KPI fields — thêm vào ViewModel hoặc trực tiếp nếu đơn giản
// Chèn vào LoadDataAsync():
// _data.TodayRevenue = summaryResult.TodayRevenue;  // TODO: gọi thật

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

---

### Bước 4 — Xác nhận

Báo:
- Vị trí cụ thể cần chèn code Razor (dòng bao nhiêu, sau element nào)
- Properties cần thêm vào ViewModel (nếu chưa có)
- Method nào trong Service cần cập nhật để trả về KPI data

---

## Lưu ý

- `sm="6" md="3"` → 4 KPI trên desktop, 2 cột tablet, 1 cột mobile — điều chỉnh theo số lượng KPI
- Border-left color dùng CSS var: `--mud-palette-primary`, `--mud-palette-success`, `--mud-palette-error`, `--mud-palette-warning`
- Nếu `PosKpiCard` đã tạo trong `Components/Shared/` → dùng `<PosKpiCard Label="..." Value="..." .../>` thay MudPaper inline
- KPI data nên load song song với data khác bằng `Task.WhenAll`

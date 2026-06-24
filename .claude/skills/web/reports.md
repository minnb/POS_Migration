# Reports — Pivot table & Report page layout

> **Áp dụng khi:** tạo trang báo cáo dạng ma trận (pivot) hoặc trang xuất báo cáo/PDF.

---

## Pivot Report Table — Pattern báo cáo dạng ma trận

> Áp dụng khi: cần hiển thị báo cáo dạng pivot — hàng = category/entity, cột = ngày/tháng, ô = (số lượng, doanh thu).
> **Ngoại lệ có chủ đích:** pivot dùng `<table class="pos-table rpt-pivot-table">` (raw HTML) thay vì `MudTable`,
> vì MudTable không hợp cho ma trận cột động theo ngày. Đây là exception duy nhất so với quy tắc datatable.md.

### Data model

```csharp
// Record pivot row — Dictionary key = ngày, value = tuple (Qty, Amt)
private record PivotRow(
    string MCHCode,
    string MCHName,
    int    TotalQty,
    double TotalAmt,
    Dictionary<DateTime, (int Qty, double Amt)> ByDate);

private List<DateTime>  _dates     = [];   // danh sách ngày = cột
private List<PivotRow>  _pivotRows = [];   // danh sách hàng
private int             _totalQty;
private double          _totalAmt;
```

### BuildPivot logic

```csharp
private void BuildPivot(DateTime fromDate, DateTime toDate)
{
    // Collect distinct dates có trong data (không nhất thiết liên tiếp)
    _dates = _items
        .Select(x => x.BussinessDate.Date)
        .Distinct().OrderBy(d => d).ToList();

    // Group by entity (MCHCode + MCHName)
    var groups = _items
        .GroupBy(x => new { x.MCHCode, x.MCHName })
        .OrderBy(g => g.Key.MCHCode).ToList();

    _pivotRows = groups.Select(g =>
    {
        var byDate = g
            .GroupBy(x => x.BussinessDate.Date)
            .ToDictionary(
                d => d.Key,
                d => (Qty: d.Sum(x => x.OrderTotal), Amt: d.Sum(x => x.AmountTotal)));

        return new PivotRow(
            MCHCode:  g.Key.MCHCode,
            MCHName:  g.Key.MCHName,
            TotalQty: g.Sum(x => x.OrderTotal),
            TotalAmt: g.Sum(x => x.AmountTotal),
            ByDate:   byDate);
    }).ToList();

    _totalQty = _pivotRows.Sum(r => r.TotalQty);
    _totalAmt = _pivotRows.Sum(r => r.TotalAmt);
}
```

### Pivot table markup

```razor
<div style="overflow-x:auto;">
    <table class="pos-table rpt-pivot-table">
        <thead>
            <tr>
                <th style="width:48px; text-align:center;">STT</th>
                <th style="min-width:200px;">Tên gian hàng</th>
                <th style="min-width:110px; text-align:right;">
                    Số lượng/<br/>Số tiền
                </th>
                @foreach (var date in _dates)
                {
                    <th style="min-width:90px; text-align:right;">
                        @date.ToString("dd/MM")<br/>
                        <span style="font-weight:400; font-size:0.78rem;">(@GetDow(date))</span>
                    </th>
                }
            </tr>
        </thead>
        <tbody>
            @{ int stt = 1; }
            @foreach (var row in _pivotRows)
            {
                <tr>
                    <td style="text-align:center; vertical-align:top;">@(stt++)</td>
                    <td style="vertical-align:top;">
                        <div style="font-weight:600; font-size:0.88rem;">@row.MCHCode</div>
                        <div style="color:#1976D2; font-size:0.82rem;">@row.MCHName</div>
                    </td>
                    <td style="text-align:right; vertical-align:top; white-space:nowrap;">
                        <div>@row.TotalQty.ToString("N0")</div>
                        <div style="color:#1976D2; font-weight:500;">@row.TotalAmt.ToString("N0")</div>
                    </td>
                    @foreach (var date in _dates)
                    {
                        var cellQty = row.ByDate.TryGetValue(date, out var cv) ? cv.Item1 : 0;
                        var cellAmt = row.ByDate.TryGetValue(date, out var ca) ? ca.Item2 : 0.0;
                        <td style="text-align:right; vertical-align:top; white-space:nowrap;">
                            <div>@(cellQty > 0 ? cellQty.ToString("N0") : "")</div>
                            <div style="color:#1976D2;">@(cellAmt > 0 ? cellAmt.ToString("N0") : "")</div>
                        </td>
                    }
                </tr>
            }
        </tbody>
        <tfoot>
            <tr class="rpt-pivot-total">
                <td colspan="2" style="text-align:center; font-weight:700;">Total</td>
                <td style="text-align:right; white-space:nowrap;">
                    <div>@_totalQty.ToString("N0")</div>
                    <div>@_totalAmt.ToString("N0")</div>
                </td>
                @foreach (var date in _dates)
                {
                    var qty = _pivotRows.Sum(r => r.ByDate.TryGetValue(date, out var v) ? v.Item1 : 0);
                    var amt = _pivotRows.Sum(r => r.ByDate.TryGetValue(date, out var va) ? va.Item2 : 0.0);
                    <td style="text-align:right; white-space:nowrap;">
                        <div>@(qty > 0 ? qty.ToString("N0") : "")</div>
                        <div>@(amt > 0 ? amt.ToString("N0") : "")</div>
                    </td>
                }
            </tr>
        </tfoot>
    </table>
</div>
```

### Helper: Day of week

```csharp
private static string GetDow(DateTime d) => d.DayOfWeek switch
{
    DayOfWeek.Monday    => "Mon",
    DayOfWeek.Tuesday   => "Tue",
    DayOfWeek.Wednesday => "Wed",
    DayOfWeek.Thursday  => "Thu",
    DayOfWeek.Friday    => "Fri",
    DayOfWeek.Saturday  => "Sat",
    DayOfWeek.Sunday    => "Sun",
    _ => ""
};
```

**CSS classes cần có (đã khai báo trong `app.css`):**
- `pos-table` — base table style
- `rpt-pivot-table` — thêm border/style riêng cho pivot report
- `rpt-pivot-total` — style hàng Total ở footer

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

---

## Report Page Layout — Header chuẩn cho trang báo cáo

> Áp dụng khi: page xuất báo cáo dạng bảng (có thể in / xuất PDF).

### Cấu trúc markup chuẩn

```razor
<MudPaper Elevation="2" Class="mb-4 pa-4">

    @* 1. Action bar (PDF button bên phải) *@
    <div style="display:flex; justify-content:flex-end; margin-bottom:8px;">
        <MudButton Variant="Variant.Filled"
                   Color="Color.Success"
                   StartIcon="@Icons.Material.Filled.PictureAsPdf"
                   OnClick="@OnExportPdfClick"
                   Size="Size.Small">Xuất PDF</MudButton>
    </div>

    @* 2. User info + timestamp *@
    <div style="display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:12px; font-size:0.82rem; color:#555;">
        <div>
            <div>ID của người dùng: <strong>@_userId</strong></div>
            <div>Tên người dùng: <strong>@_userFullName</strong></div>
        </div>
        <div style="text-align:right;">
            Ngày giờ: <strong>@_printedAt</strong>
        </div>
    </div>

    @* 3. Report title *@
    <div style="text-align:center; margin-bottom:16px;">
        <div style="font-size:1.1rem; font-weight:700; letter-spacing:0.5px; text-transform:uppercase;">
            Tên báo cáo
        </div>
    </div>

    @* 4. Filter summary (store + date range) *@
    <div style="font-size:0.84rem; margin-bottom:12px;">
        <div>
            Cửa hàng:
            <strong>
                @if (!string.IsNullOrEmpty(_reportStoreNo))
                { @($"{_reportStoreNo} – {_reportStoreName}") }
                else
                { @("Tất cả") }
            </strong>
        </div>
        <div>
            Ngày giao dịch:
            <strong>
                @((_filterFromDateNullable ?? DateTime.Today).ToString("dd/MM/yyyy"))
                –
                @((_filterToDateNullable ?? DateTime.Today).ToString("dd/MM/yyyy"))
            </strong>
        </div>
    </div>

    @* 5. Nội dung bảng *@
    @* ... *@

</MudPaper>
```

### State fields cần thêm cho report header

```csharp
// Report header info — lấy sau khi có AuthState
private string _userId       = string.Empty;
private string _userFullName = string.Empty;
private string _printedAt    = string.Empty;  // set sau khi load data xong

// Resolved store info cho header (sau khi load data)
private string _reportStoreNo   = string.Empty;
private string _reportStoreName = string.Empty;
```

```csharp
// Trong OnInitializedAsync — lấy user info từ claims
_userId       = state.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
_userFullName = state.User.FindFirst("full_name")?.Value ?? string.Empty;

// Trong LoadDataAsync — sau khi có data
_printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
_reportStoreNo   = storeNo;
_reportStoreName = _items.FirstOrDefault(x => x.StoreNo == storeNo)?.StoreName ?? string.Empty;
if (string.IsNullOrEmpty(_reportStoreNo)) _reportStoreName = string.Empty;
```

### Xuất PDF — helper dùng chung (`IPdfExportService` + QuestPDF)

> Dùng `IPdfExportService` (`src/POS.Web/Services/Pdf/`) — render PDF bằng QuestPDF, khung header
> (title/user/timestamp/store/date range) + cơ chế download **dùng chung mọi page**.
> License QuestPDF set 1 lần trong `Program.cs` (`QuestPDF.Settings.License = LicenseType.Community;`).

**Nguyên tắc:** truyền **dữ liệu đã load sẵn trên màn hình** vào helper (KHÔNG query lại DB) → PDF khớp 100% form người dùng đang xem.

```razor
@using POS.Web.Services
@using POS.Web.Services.Pdf
@inject IPdfExportService PdfExport
@inject IJSRuntime JS
```

```csharp
private bool _exporting;   // disable nút khi đang xuất

private async Task OnExportPdfClick()
{
    if (_isEmpty || _pivotRows.Count == 0) return;
    _exporting = true; StateHasChanged();
    try
    {
        var header = new ReportHeaderModel
        {
            Title = "Tổng hợp doanh thu theo ngành hàng",   // helper tự .ToUpper()
            UserId = _userId, UserFullName = _userFullName, PrintedAt = DateTime.Now,
            StoreLabel = string.IsNullOrEmpty(_reportStoreNo) ? "Tất cả" : $"{_reportStoreNo} – {_reportStoreName}",
            FromDate = _from, ToDate = _to
        };
        var data = new PivotReportData
        {
            Dates = _dates, TotalQty = _totalQty, TotalAmt = _totalAmt,
            Rows = _pivotRows.Select(r => new PivotReportRow(
                r.MCHCode, r.MCHName, r.TotalQty, r.TotalAmt,
                r.ByDate.ToDictionary(kv => kv.Key, kv => ((long)kv.Value.Qty, kv.Value.Amt)))).ToList()
        };
        var bytes = PdfExport.BuildPivotReport(header, data);
        await JS.SaveAsFileAsync($"Report_{header.FromDate:yyyyMMdd}-{header.ToDate:yyyyMMdd}.pdf", bytes, "application/pdf");
    }
    catch (Exception ex)
    {
        Snackbar.Add("Không thể xuất PDF. Vui lòng thử lại.", Severity.Error);
        KibanaService.LogException("PageName.ExportPdf", "", 0, "", ex.Message);
    }
    finally { _exporting = false; StateHasChanged(); }
}
```

> Download: `IJSRuntime.SaveAsFileAsync(fileName, bytes, contentType)` (`JsDownloadExtensions`) → JS `posDownloadFileFromStream` (`wwwroot/js/download.js`, đã include trong `App.razor`). Stream byte, không base64.
>
> **Báo cáo dạng cột thường** (không pivot): thêm method `BuildTableReport(...)` vào `IPdfExportService` theo cùng khung `ComposeReportHeader` — chỉ viết phần body bảng.
>
> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

---

## Pattern: Report page an toàn ở quy mô lớn (SP nặng / bảng ~10M dòng)

> Áp dụng khi: trang report tự `LoadData` từ SP nặng, có filter/preset nhiều, range rộng (vd "Năm nay").
> 4 việc BẮT BUỘC để không dồn tải DB và không treo connection:

```csharp
@implements IDisposable
private CancellationTokenSource? _cts;
private const int MaxDaysAllStores = 92;

// 1) HOÃN auto-load khỏi prerender — OnInitializedAsync chạy 2 lần (prerender + circuit).
//    Đọc auth/store list trong OnInitializedAsync; auto-load lần đầu để ở đây:
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender) await LoadDataAsync();
}

private async Task LoadDataAsync()
{
    if (_loading) return;                 // 2) chống re-entrancy: preset/nút bấm liên tiếp không dồn query
    _cts?.Cancel(); _cts?.Dispose();      // 3) hủy lượt cũ, mở token theo lượt này
    _cts = new CancellationTokenSource();
    var ct = _cts.Token;
    _loading = true; StateHasChanged();
    try
    {
        // 4) clamp range khi xem "tất cả cửa hàng" — RS2 = days × stores, payload phình to
        if (string.IsNullOrEmpty(storeNo) && (to - from).Days + 1 > MaxDaysAllStores)
        {
            from = to.AddDays(-(MaxDaysAllStores - 1)); _fromDate = from; _activePreset = "";
            Snackbar.Add($"Xem \"tất cả cửa hàng\" giới hạn {MaxDaysAllStores} ngày — đã tự điều chỉnh.", Severity.Info);
        }
        var t = Repo.GetSaleByTimeAsync(from, to, storeNo, "DAY", ct: ct);   // truyền ct xuống repo
        // ...
    }
    catch (OperationCanceledException) { /* hủy: bỏ qua, KHÔNG hiện lỗi đỏ */ }
    catch (Exception ex) { _errorMsg = "..."; }
    finally { _loading = false; if (!ct.IsCancellationRequested) StateHasChanged(); }
}

public void Dispose() { _cts?.Cancel(); _cts?.Dispose(); }
```

**Anti-pattern (đã gặp):**
- `await LoadDataAsync()` trong `OnInitializedAsync` → query nặng chạy trong prerender, chặn first-byte + chạy lặp.
- Quên `Disabled="@_loading"` trên **preset chips / autocomplete** (chỉ disable nút Tìm) → vẫn dồn query.
- Không truyền `ct` → user rời trang, SP cold vẫn chạy tới hết timeout, giữ connection.
- KHÔNG đặt `_loading = true` làm giá trị mặc định field → guard `if (_loading) return;` sẽ chặn luôn lần auto-load đầu.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`

---

## Pattern: MudTable row → drill-through dialog

> Áp dụng khi: click 1 dòng báo cáo → mở dialog chi tiết (vd Top SP → hóa đơn chứa SP đó).

```razor
<MudTable Items="@_rows" T="MyDto" Hover="true" OnRowClick="@OnRowClicked" RowStyle="cursor:pointer"> ... </MudTable>
```
```csharp
private async Task OnRowClicked(TableRowClickEventArgs<MyDto> args)
{
    var r = args.Item;
    if (r == null) return;
    var parameters = new DialogParameters<MyDialog> { { x => x.Id, r.Id }, { x => x.From, _from } };
    await DialogService.ShowAsync<MyDialog>($"Chi tiết — {r.Id}", parameters,
        new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true });
}
```
> Dialog: `[CascadingParameter] IMudDialogInstance MudDialog`, `[Parameter]` cho input, load trong `OnInitializedAsync`,
> `MudDialog.Close()`. Mẫu: `ProductOrdersDialog.razor`, `TransactionDetailDialog.razor`.

---

## Pattern: Tận dụng dữ liệu SP đã tính + so sánh cấp dòng (BA/BI)

> Áp dụng khi: report đọc SP nặng. Trước khi thêm cột/SP mới, kiểm tra SP **đã trả** cột nào mà page đang vứt.

- **Surface discarded columns**: SP report thường trả nhiều cột (return qty, avg price, order count, discount...)
  mà page chỉ hiện vài cột. Tính % inline trong `RowTemplate` (vd `ReturnQty/SoldQty`, `Discount/Gross`) —
  **free win, không sửa SP/DTO**. Tô màu ngưỡng (trả > 5% đỏ, giảm > 20% cam) để thành insight.
- **So sánh cấp dòng (không sửa SP)**: khi có toggle "so sánh kỳ trước", page đã gọi SP kỳ trước cho KPI —
  **giữ luôn list prev**, `ToDictionary(x => x.Key)`, rồi join theo khóa để hiện Δ hạng (▲/▼/NEW) + Δ% mỗi dòng.
  Đừng chỉ dùng prev cho KPI tổng rồi vứt list.

**Anti-pattern:** thêm SP/cột mới cho chỉ số mà SP hiện tại đã tính sẵn nhưng page bỏ qua.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/TopProductPage.razor`

---
name: web-datatable-code-recipes
description: Code mẫu đầy đủ MudTable<T> — client-side paging, server-side paging, cột động, sort cột đặc biệt. Đọc khi cần copy-paste pattern DataTable đầy đủ (luật ngắn gọn xem 04-datatable-and-lists.md).
---

# DataTable chuẩn — `MudTable<T>`

> **Áp dụng khi:** tạo bất kỳ bảng dữ liệu nào trong POS.Web.
> **BẮT BUỘC** dùng MudBlazor `<MudTable>` — sort + phân trang built-in, KHÔNG tự viết HTML `<table>` hay base class.
> (Lịch sử: project từng dùng `<table class="pos-table">` + `PosTableBase<T>`; đã chuyển hết sang MudTable.)

---

## Pattern A: Client-side paging (data load 1 lần, paginate tại client)

> Áp dụng khi: load toàn bộ list 1 lần rồi sort/paginate trong bộ nhớ (đa số trường hợp).

```razor
<MudTable Items="@_items"
          Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" Elevation="2"
          Loading="@_loading" LoadingProgressColor="Color.Primary"
          HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<MyDto, object>(x => x.FieldA)">Tiêu đề A</MudTableSortLabel></MudTh>
        <MudTh Style="text-align:right"><MudTableSortLabel SortBy="new Func<MyDto, object>(x => x.Amount)">Số tiền</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Tiêu đề A">@context.FieldA</MudTd>
        <MudTd DataLabel="Số tiền" Style="text-align:right">@FormatVND(context.Amount)</MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudText Class="pa-4" Style="color:#9e9e9e">Không có dữ liệu.</MudText>
    </NoRecordsContent>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                       InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                       RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

**Key points:**
- Pagination: `PageSizeOptions` chuẩn = `new[] { 10, 20, 50, 100 }`. **Phải bắt đầu bằng `10`** (= default `MudTable.RowsPerPage`); thiếu `10` → ô chọn số dòng/trang hỏng (trống, chọn không có tác dụng). Không hard-set `RowsPerPage="..."` một chiều trên `MudTable`.
- Sort: `<MudTableSortLabel SortBy="new Func<T, object>(x => x.Field)">` — MudTable tự sort, KHÔNG cần `_sortCol`/`SortBy()`.
- Filter/search phụ: dùng computed property (vd `FilteredItems`) làm `Items`, KHÔNG cần reset `_page`.
- `Loading="@_loading"` → MudTable tự hiện progress overlay (bỏ `MudProgressCircular` trong tbody cũ).
- `HorizontalScrollbar="true"` thay cho wrapper `overflow-x:auto`.
- Search box / count → đặt trong `<ToolBarContent>` (kèm `<MudSpacer/>`).
- Dòng tổng → `<FooterContent>` (dùng `<MudTh>` cho từng ô).
- `FormatVND` không còn từ base class → khai báo local: `private static string FormatVND(decimal v) => $"{v:N0} ₫";`

## Pattern B: Server-side paging (gọi SP/DB theo từng trang)

> Áp dụng khi: data quá lớn, SP nhận `PageSize`/`PageNumber` và trả `Total`.

```razor
<MudTable @ref="_table" ServerData="ServerReloadAsync"
          Hover="true" Striped="true" Dense="true" Loading="@_loading"
          Breakpoint="Breakpoint.Sm" Elevation="2" HorizontalScrollbar="true">
    <HeaderContent>...</HeaderContent>
    <RowTemplate>...</RowTemplate>
    <PagerContent><MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"/></PagerContent>
</MudTable>
```

```csharp
private MudTable<MyDto> _table = null!;

private async Task<TableData<MyDto>> ServerReloadAsync(TableState state, CancellationToken token)
{
    var result = await Repo.GetPagedAsync(/* filters */, state.PageSize, state.Page); // state.Page 0-based
    var total  = result.FirstOrDefault()?.Total ?? 0;
    return new TableData<MyDto> { Items = result, TotalItems = total };
}

// Nút "Tìm" / Enter / Reset → KHÔNG gọi load thủ công, mà:
private Task SearchAsync() => _table.ReloadServerData();
```

> OnInitializedAsync KHÔNG cần gọi load — MudTable tự gọi `ServerData` lần đầu khi render.

## Dynamic columns (cột động từ SQL kết quả)

`Items="@rows"` với `rows` là `List<object?[]>`; loop `_columns` trong HeaderContent, loop index trong RowTemplate:
```razor
<RowTemplate>
    @for (var i = 0; i < context.Length; i++)
    {
        <MudTd DataLabel="@(i < _columns.Count ? _columns[i] : "")">@context[i]</MudTd>
    }
</RowTemplate>
```

---

## Sort — cột đặc biệt

**Nullable DateTime:** dùng `?? DateTime.MinValue` để sort đúng mà không nullref
```csharp
SortBy="new Func<EosShiftDto, object>(x => x.OpenShiftDate ?? DateTime.MinValue)"
```

**Pre-formatted string date:** DTO có 2 field — `TimeLabel` (string hiển thị) và `SortOrder` (int thứ tự). Sort theo `SortOrder`, KHÔNG sort theo `TimeLabel`:
```csharp
// ❌ Sai — sort string "Thứ 2 01/07" cho kết quả ngẫu nhiên
SortBy="new Func<SaleByTimeSeriesDto, object>(x => x.TimeLabel)"

// ✅ Đúng — sort theo int thứ tự thời gian
SortBy="new Func<SaleByTimeSeriesDto, object>(x => x.SortOrder)"
```
> Ví dụ: `src/POS.Web/Components/Pages/Store/Reports/RevenueHourlyPage.razor`

---

## Anti-patterns

- ❌ Tự viết `<table class="pos-table">` + `@onclick SortBy` cho DataTable mới — dùng `MudTable` + `MudTableSortLabel`.
- ❌ `@inherits PosTableBase<T>` — base class đã xóa, dùng MudTable built-in.
- ❌ `MudPagination` thủ công cho table — dùng `<MudTablePager>` trong `<PagerContent>`.
- ❌ Server-side: gọi `LoadDataAsync()` trực tiếp từ nút Tìm — phải `_table.ReloadServerData()`.
- ❌ Wrapper `<MudPaper Style="overflow-x:auto">` quanh MudTable — dùng `HorizontalScrollbar="true"` trên MudTable.
- ❌ Inline result summary text giữa filter panel và table (`@if (!_loading && _items.Count > 0) { <div>Tìm thấy X dòng</div> }`) — dùng KPI cards hoặc `InfoFormat` trong `MudTablePager`.
- ❌ Filter panel `Elevation="2"` — chuẩn là `Elevation="1"` cho MudPaper chứa filter panel.

> **Ngoại lệ:** Pivot report (hàng × cột-ngày động) vẫn dùng `<table class="pos-table rpt-pivot-table">` — xem `reports.md`. MudTable không hợp cho ma trận cột động theo ngày.

---

## Ví dụ thực tế

| Loại | File |
|---|---|
| Client-side + sort | `src/POS.Web/Components/Pages/Store/Transactions/TransactionsPage.razor`, `Store/Operations/EosShiftsPage.razor` |
| Client-side + search trong ToolBarContent | `src/POS.Web/Components/Pages/Admin/UsersPage.razor` |
| Server-side paging | `src/POS.Web/Components/Pages/Store/Reports/DetailRevenuePage.razor` |
| Footer tổng | `src/POS.Web/Components/Pages/Store/Reports/RevenueHourlyPage.razor` |
| Dynamic columns | `src/POS.Web/Components/Pages/Admin/SqlConsolePage.razor` |

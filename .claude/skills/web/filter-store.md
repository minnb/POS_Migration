# Filter Store — Combobox Cửa hàng chuẩn

> **Áp dụng bắt buộc** cho mọi page `StoreAndAbove` có bộ lọc theo cửa hàng.
> Pattern đảm bảo: StoreOperator chỉ thấy store của mình (readonly), ITOps/Admin chọn tự do với tìm kiếm theo mã + tên.

---

## 1. Repository — `GetStoreListAsync`

**Interface:** `src/POS.Infrastructure/Repositories/Interfaces/ICentralMDRepository.cs`

```csharp
Task<List<StoreDto>> GetStoreListAsync(CancellationToken ct = default);
```

**Implementation:** `src/POS.Infrastructure/Repositories/CentralMDRepository.cs`

- Query: `SELECT No AS StoreNo, Name FROM dbo.Store (NOLOCK) WHERE Blocked = 0 ORDER BY No`
- Cache Redis key: `MD:StoreList` | TTL: `43200s` (12h)
- Return type: `List<StoreDto>` — reuse DTO đã có trong `POS.Common.Dtos.CentralMD`

> `StoreDto.StoreNo` = mã cửa hàng | `StoreDto.Name` = tên cửa hàng

---

## 2. Using directives cần thêm

```razor
@using POS.Common.Dtos.CentralMD
```

---

## 3. @code — Fields và Lifecycle

```csharp
// Store picker fields
private bool                 _isStoreOperator;
private string?              _filterStoreNo;         // dùng cho StoreOperator (locked) + query param
private IReadOnlyList<string> _userStoreCodes = [];
private List<StoreDto>       _allStores       = [];
private StoreDto?            _selectedStore;         // binding cho ITOps/Admin autocomplete

// Trong OnInitializedAsync — sau khi parse _userStoreCodes:
_isStoreOperator = _userStoreCodes.Count > 0;

if (_isStoreOperator)
    _filterStoreNo = _userStoreCodes[0];

try
{
    _allStores = await MdRepo.GetStoreListAsync();   // cache Redis 12h — chi phí thấp cho cả 2 role
}
catch (Exception ex)
{
    KibanaService.LogException("PageName.LoadStoreCodes", "", 0, "", ex.Message);
}
```

---

## 4. Tính storeNo trong LoadDataAsync

```csharp
// ITOps/Admin: lấy từ _selectedStore; StoreOperator: dùng _filterStoreNo (đã lock)
var storeNo = _isStoreOperator ? _filterStoreNo : _selectedStore?.StoreNo;

// Trường hợp SP nhận string (không phải nullable):
var storeNo = _isStoreOperator ? _filterStoreNo : (_selectedStore?.StoreNo ?? "");
```

---

## 5. ResetFilterAsync

```csharp
private async Task ResetFilterAsync()
{
    // ... reset các filter khác ...
    if (!_isStoreOperator) _selectedStore = null;   // KHÔNG reset _filterStoreNo (StoreOperator giữ nguyên)
    await LoadDataAsync();
}
```

---

## 6. SearchStoreAsync

```csharp
private Task<IEnumerable<StoreDto>> SearchStoreAsync(string value, CancellationToken ct)
{
    IEnumerable<StoreDto> matches = string.IsNullOrWhiteSpace(value)
        ? _allStores
        : _allStores.Where(s =>
            (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
    return Task.FromResult(matches.Take(50));   // BẮT BUỘC: giới hạn để tránh materialize toàn bộ list
}
```

---

## 7. StoreDisplayText — hiển thị tên đầy đủ cho StoreOperator

```csharp
private string StoreDisplayText => _allStores.FirstOrDefault(s => s.StoreNo == _filterStoreNo) is { } st
    ? $"{st.StoreNo} – {st.Name}"
    : (_filterStoreNo ?? "");
```

---

## 8. Markup Razor — MudItem chứa store picker

```razor
@* Thêm @using POS.Common.Dtos.CentralMD ở đầu file *@

<MudItem xs="12" sm="6" md="3">
    @if (_isStoreOperator)
    {
        @* Readonly — StoreOperator chỉ thấy store của mình, có tên đầy đủ *@
        <MudTextField Value="@StoreDisplayText"
                      Label="Cửa hàng"
                      Variant="Variant.Outlined"
                      Margin="Margin.Dense"
                      ReadOnly="true"
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Store"/>
    }
    else
    {
        @* Autocomplete — ITOps/Admin tìm theo mã hoặc tên *@
        <MudAutocomplete T="StoreDto"
                         @bind-Value="_selectedStore"
                         Label="Cửa hàng"
                         Placeholder="Tất cả cửa hàng"
                         Variant="Variant.Outlined"
                         Margin="Margin.Dense"
                         SearchFunc="@SearchStoreAsync"
                         ToStringFunc="@(s => s == null ? "" : $"{s.StoreNo} – {s.Name}")"
                         Clearable="true"
                         AdornmentIcon="@Icons.Material.Filled.Store"
                         Adornment="Adornment.Start"
                         MinCharacters="0"
                         MaxItems="50"/>
    }
</MudItem>
```

**Điều chỉnh `md` theo số cột trong filter panel:**
- Filter đơn giản (2–3 cột): `md="3"` hoặc `md="4"`
- Filter đông (5+ cột): `md="2"`

---

## 9. Checklist áp dụng

```
□ Thêm @using POS.Common.Dtos.CentralMD
□ Đổi _allStoreCodes: List<string> → _allStores: List<StoreDto>
□ Thêm _selectedStore: StoreDto?
□ OnInitializedAsync: load _allStores cho CẢ 2 role (cache nên chi phí thấp)
□ LoadDataAsync: tính storeNo từ _isStoreOperator ? _filterStoreNo : _selectedStore?.StoreNo
□ ResetFilterAsync: _selectedStore = null (không dùng _filterStoreNo = null/empty)
□ SearchStoreAsync: return IEnumerable<StoreDto>, filter theo cả StoreNo + Name, có .Take(50) cuối
□ Thêm StoreDisplayText property
□ Markup: MudAutocomplete T="StoreDto" với ToStringFunc + Clearable="true" + MaxItems="50"
□ Markup: KHÔNG dùng ResetValueOnEmptyText="true" (gây circuit crash khi MinCharacters="0")
□ Markup: readonly TextField dùng StoreDisplayText (không dùng _filterStoreNo trực tiếp)
```

---

## 10. Anti-patterns

- ❌ `GetStoreSetConfigAsync()` cho store picker — chỉ có `StoreNo`, không có `Name`
- ❌ `MudAutocomplete T="string"` với `CoerceValue="true"` — user gõ tự do, không validate
- ❌ Chỉ load `_allStores` cho ITOps/Admin — StoreOperator cũng cần để hiển thị tên đầy đủ
- ❌ `_filterStoreNo = null` trong ResetFilterAsync cho ITOps — dùng `_selectedStore = null`
- ❌ `ResetValueOnEmptyText="true"` + `MinCharacters="0"` — text rỗng khi focus → reset value lặp vô hạn → re-render loop → **Blazor circuit bị tear-down** ("Failed to rejoin"). Dùng `Clearable="true"` thay thế.
- ❌ Bỏ `.Take(50)` trong SearchStoreAsync — `MaxItems` chỉ giới hạn hiển thị, toàn bộ list vẫn bị materialize → lag với list lớn
- ❌ `ToStringFunc` trả về chỉ `StoreNo` — mất tên cửa hàng trong dropdown

---

## 11. Biến thể: MudAutocomplete "thêm vào danh sách" (multi-add picker)

> Khi picker KHÔNG phải chọn 1 giá trị mà để **thêm liên tiếp nhiều mục vào 1 list/lưới** (vd chọn
> nhiều cửa hàng gán vào nhóm): giữ `@ref` và gọi `await _picker.ClearAsync()` NGAY sau khi thêm để ô
> tự rỗng, sẵn sàng chọn mục tiếp theo. Chống trùng bằng cách **bỏ qua im lặng** (không Snackbar/alert
> — trùng là thao tác bình thường của người dùng, không phải lỗi). KHÔNG dùng
> `ResetValueOnEmptyText`/`MinCharacters=0` để tự clear (gây reset-loop crash circuit — xem
> `01-architecture-and-logic.md` §5); `ClearAsync()` sau add là cách an toàn.

```csharp
private MudAutocomplete<StoreDto>? _picker;
private async Task AddStoreAsync(StoreDto? s) {
    var code = s?.StoreNo?.Trim();
    if (!string.IsNullOrEmpty(code) && _rows.All(r => !r.Store.Equals(code, StringComparison.OrdinalIgnoreCase)))
        _rows.Add(new(){ Store = code, StoreName = s!.Name });
    if (_picker != null) await _picker.ClearAsync();   // rỗng ô để thêm mục kế tiếp
}
```
> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Price/Dialogs/PriceGroupSetupDialog.razor`

---

## 12. Pages đã áp dụng

| Page | File |
|------|------|
| Giao dịch | `src/POS.Web/Components/Pages/Store/Transactions/TransactionsPage.razor` |
| Doanh thu chi tiết | `src/POS.Web/Components/Pages/Store/Reports/DetailRevenuePage.razor` |
| Doanh thu theo ngành hàng | `src/POS.Web/Components/Pages/Store/Reports/SalesByCategoryPage.razor` |
| Kết thúc ca | `src/POS.Web/Components/Pages/Store/Operations/EosShiftsPage.razor` |

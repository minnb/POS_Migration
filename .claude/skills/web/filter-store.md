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
    if (string.IsNullOrWhiteSpace(value))
        return Task.FromResult<IEnumerable<StoreDto>>(_allStores);
    return Task.FromResult(_allStores.Where(s =>
        (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false)));
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
                         ResetValueOnEmptyText="true"/>
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
□ SearchStoreAsync: return IEnumerable<StoreDto>, filter theo cả StoreNo + Name
□ Thêm StoreDisplayText property
□ Markup: MudAutocomplete T="StoreDto" với ToStringFunc + ResetValueOnEmptyText
□ Markup: readonly TextField dùng StoreDisplayText (không dùng _filterStoreNo trực tiếp)
```

---

## 10. Anti-patterns

- ❌ `GetStoreSetConfigAsync()` cho store picker — chỉ có `StoreNo`, không có `Name`
- ❌ `MudAutocomplete T="string"` với `CoerceValue="true"` — user gõ tự do, không validate
- ❌ Chỉ load `_allStores` cho ITOps/Admin — StoreOperator cũng cần để hiển thị tên đầy đủ
- ❌ `_filterStoreNo = null` trong ResetFilterAsync cho ITOps — dùng `_selectedStore = null`
- ❌ Bỏ `ResetValueOnEmptyText="true"` — khi user xóa text, `_selectedStore` không reset về `null`
- ❌ `ToStringFunc` trả về chỉ `StoreNo` — mất tên cửa hàng trong dropdown

---

## 11. Pages đã áp dụng

| Page | File |
|------|------|
| Giao dịch | `src/POS.Web/Components/Pages/Store/TransactionsPage.razor` |
| Doanh thu chi tiết | `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor` |
| Doanh thu theo ngành hàng | `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor` |
| Kết thúc ca | `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor` |

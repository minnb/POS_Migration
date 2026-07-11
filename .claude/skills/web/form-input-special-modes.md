---
name: web-form-input-special-modes
description: Biến thể form nâng cao trong POS.Web — chế độ chỉ xem (view-only) cho bản ghi đã khóa, field ngoại lệ có nút Lưu điều kiện, MudTimePicker HH:mm, MudSelect multi-selection, format số tiền khi nhập. Đọc sau khi đã áp dụng form-input.md cơ bản.
---

# Form Input — Special Modes & Controls

> Đọc file này SAU khi đã áp dụng chuẩn form cơ bản ở [`form-input.md`](form-input.md). File này
> chỉ chứa các biến thể/control đặc biệt, không lặp lại nguyên tắc chung.

---

## 1. Chế độ CHỈ XEM (View-only) — bản ghi đã khóa, KHÔNG dùng `Disabled`

> Áp dụng khi: nghiệp vụ quy định bản ghi **không còn sửa được sau khi tạo** (vd voucher đã phát
> hành mã, coupon đã sinh code) — khác readonly TẠM THỜI (`Disabled="@_isReadonly"`, dùng khi
> user có thể bật/tắt qua lại giữa chế độ xem và sửa). Với khóa VĨNH VIỄN, **KHÔNG** dùng input
> `Disabled` (nhìn vẫn giống ô nhập liệu, dễ gây hiểu lầm còn sửa được) — thay bằng **cặp `MudText`
> label/value**, giữ nguyên layout `MudGrid`/`MudItem` như form nhập để 2 chế độ không "nhảy" bố cục.

```razor
<MudPaper Outlined="true" Class="pa-3 mb-3 rounded-lg" Style="position:relative">
    <MudText Typo="Typo.subtitle2" Color="Color.Primary"
             Style="position:absolute; top:-11px; left:12px; padding:0 6px;
                    background:var(--mud-palette-surface); line-height:1;">
        Thông tin voucher
    </MudText>
    <MudGrid Spacing="2" Class="mt-1">
        <MudItem xs="12" sm="6" md="3">
            <MudText Typo="Typo.caption" Color="Color.Secondary">Mã phát hành</MudText>
            <MudText Typo="Typo.body1">@_model.ItemNo</MudText>
        </MudItem>
        @* mỗi field 1 cặp caption (label) + body1 (value), CÙNG breakpoint như lúc còn là input *@
    </MudGrid>
</MudPaper>
```

- Giữ **nguyên breakpoint** `xs`/`sm`/`md` của field tương ứng ở chế độ nhập liệu — chỉ đổi
  input thành `MudText`, không đổi layout.
- Đổi **cả khối** (không đổi từng field lẻ) — bọc CẢ section trong `@if (IsReadOnlyMode) {...} else {...}`.
- Bảng con (`MudTable` sản phẩm...) giữ nguyên component nhưng **ẩn cột hành động** (thêm/xoá) khi
  view-only, thay vì disable từng nút.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/CouponVoucher/VoucherIssuePage.razor`
> (nhánh `@if (IsEditing) { ... }`).

---

## 2. Sửa 1 field NGOẠI LỆ trên bản ghi đã khóa — nút Lưu điều kiện

> Áp dụng khi: bản ghi ở chế độ CHỈ XEM (mục 1) nhưng nghiệp vụ vẫn cho sửa **đúng 1 field** (vd bật/tắt
> `Blocked`/`Active`) mà không mở lại toàn bộ form. KHÔNG tái dùng action bar Lưu-luôn-hiện
> (`form-input.md` §6) — nút Lưu phải **ẩn mặc định**, chỉ hiện lại khi field ngoại lệ thực sự đổi
> giá trị so với lúc nạp.

**`@code` — snapshot giá trị gốc + cờ đã đổi:**
```csharp
private bool _originalBlocked;                           // snapshot NGAY sau khi nạp chi tiết
private bool BlockedChanged => IsEditing && _model.Blocked != _originalBlocked;

private async Task LoadDetailAsync(string itemNo)
{
    var d = await Service.GetDetailAsync(itemNo);
    _model.Blocked = d.Blocked;
    _originalBlocked = d.Blocked;
}

private async Task SaveExceptionFieldAsync()
{
    if (!BlockedChanged) return;
    var result = await Service.UpdateBlockedAsync(_model.ItemNo, _model.Blocked);
    if (!result.Ok) { Snackbar.Add(result.Message, Severity.Error); return; }
    await AuditLogger.LogAsync(actor, "UPDATE", "Entity", _model.ItemNo,
        oldValueJson: JsonConvert.SerializeObject(new { _model.ItemNo, Blocked = _originalBlocked }),
        newValueJson: JsonConvert.SerializeObject(new { _model.ItemNo, _model.Blocked }));
    _originalBlocked = _model.Blocked;                    // reset snapshot → nút Lưu ẩn lại
}
```

**Razor — nút Lưu trong `pos-page-header-btn` (không phải action bar):**
```razor
@if (!IsEditing || BlockedChanged)
{
    <MudButton Variant="Variant.Filled" Color="Color.Success" Disabled="_saving"
               OnClick="@(() => IsEditing ? SaveExceptionFieldAsync() : SaveAsync())">
        ...
    </MudButton>
}
```

- Field ngoại lệ **vẫn tương tác được** dù cả phần còn lại đã chuyển sang view-only (mục 1) — đặt trong
  1 `MudPaper` con riêng, kèm `MudText Typo="Typo.caption"` ghi rõ đây là ngoại lệ (vd "chỉ được
  phép khóa/mở khóa").
- Gọi **API/SP RIÊNG** cho field ngoại lệ (vd `usp_SetupVoucher_UpdateBlocked`) — KHÔNG gọi lại SP
  Save đầy đủ, tránh vô tình ghi đè field khác không có trên UI view-only.
- Vẫn **audit log CRUD** như mọi Update khác — xem `audit-logging.md`.

> Ví dụ thực tế: `VoucherIssuePage.razor` — `SaveBlockedAsync()` + `BlockedChanged`.

---

## 3. `MudTimePicker` cho ô nhập giờ "HH:mm" (thay `MudTextField` gõ tay)

> Áp dụng khi: cần ô nhập giờ tự ép định dạng `HH:mm` lúc user gõ tay, thay vì `MudTextField`
> với `Placeholder="hh:mm"` (không validate/format gì, dễ lưu sai chuỗi).

DTO thường lưu giờ dạng `string "HH:mm"` (khớp cột DB kiểu text) — KHÔNG đổi DTO, chỉ bọc 1 cặp
property `TimeSpan?` ở `@code` của page để chuyển đổi 2 chiều:

```csharp
private TimeSpan? HeaderFromTime
{
    get => ParseHm(_header.FromTime);
    set => _header.FromTime = value?.ToString(@"hh\:mm") ?? string.Empty;
}
private static TimeSpan? ParseHm(string s)
    => TimeSpan.TryParseExact(s, new[] { @"hh\:mm", @"h\:mm" }, CultureInfo.InvariantCulture, out var t) ? t : null;
```
```razor
<MudTimePicker Time="HeaderFromTime" TimeChanged="@(t => HeaderFromTime = t)" Disabled="_isReadonly"
               AmPm="false" TimeFormat="HH:mm" Editable="true"
               Variant="Variant.Outlined" Margin="Margin.Dense"/>
```
`AmPm="false"` + `Editable="true"` là bắt buộc — thiếu `Editable` thì user không gõ tay được, chỉ
chọn qua picker UI. Verify tham số qua reflection thật trên `MudBlazor.dll` (namespace
`MudBlazor.MudTimePicker`) trước khi dùng — v9 không có sẵn ví dụ nào trong repo để soi theo.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`
> (`HeaderFromTime`/`HeaderToTime`, tab "Thông tin chung").

---

## 4. `MudSelect` multi-selection cho chọn nhiều giá trị rời rạc (vd nhiều ngày trong tháng)

> Áp dụng khi: cần chọn NHIỀU giá trị từ 1 danh sách cố định nhỏ (ngày 1-31, thứ trong tuần...),
> lưu lại dạng `List<T>` — KHÔNG cần `MudAutocomplete`/chip rời.

`MudSelect<T>` có sẵn `MultiSelection="true"` + `SelectedValues`/`SelectedValuesChanged`
(`IReadOnlyCollection<T>`) — không cần tự viết checkbox group:

```razor
<MudSelect T="int" MultiSelection="true" SelectedValues="_header.ApplyDaysOfMonth"
           SelectedValuesChanged="@(v => _header.ApplyDaysOfMonth = v.OrderBy(d => d).ToList())"
           Label="Ngày áp dụng trong tháng" Disabled="_isReadonly"
           MultiSelectionTextFunc="@(selected => selected.Count == 0 ? "Tất cả" : string.Join(", ", selected.Select(int.Parse).OrderBy(d => d)))">
    @for (var day = 1; day <= 31; day++)
    {
        var d = day;
        <MudSelectItem T="int" Value="@d">Ngày @d</MudSelectItem>
    }
</MudSelect>
```
Bẫy: `MultiSelectionTextFunc` nhận `IReadOnlyList<string>` (chuỗi hiển thị của item đã chọn),
**KHÔNG PHẢI** `IReadOnlyList<T>` — cần `Select(int.Parse)` lại nếu muốn sort theo giá trị số.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`
> (`_header.ApplyDaysOfMonth`, tab "Cài đặt nâng cao").

---

## 5. Format số tiền khi user nhập (thousand separator) mà không phá parse

> Áp dụng khi: ô nhập số tiền trong lưới/form cần hiển thị `30,000` nhưng vẫn lưu đúng.

Dùng `Value`/`ValueChanged` (không `@bind`) + `Immediate="true"`, format bằng dấu `,`
(InvariantCulture `#,##0`) — KHỚP cách service `ParsePrice` tách chuỗi (`Replace(",")`) và format
hiển thị `###,###` ở list. Dùng `.` (vi-VN) sẽ bị parse nhầm thành dấu thập phân.

```razor
<MudTextField T="string" Value="context.UnitPrice"
              ValueChanged="@(v => row.UnitPrice = FormatThousands(v))"
              InputMode="InputMode.numeric" Immediate="true" Class="pos-price-input"/>
```
```csharp
// digits-only → long → ToString("#,##0", CultureInfo.InvariantCulture)
```
> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Price/PriceSetupPage.razor` (`FormatThousands`/`OnPriceChanged`).

**Bẫy khi nạp dòng từ nguồn khác (bulk import, preload API) vào CÙNG lưới**: `ValueChanged` chỉ
fire khi user gõ tay — gán giá trị bằng code (vd sau `ValidateImportAsync`) KHÔNG đi qua
`FormatThousands`, dòng import hiển thị số thô không dấu phẩy trong khi dòng nhập tay có. Luôn gọi
tường minh `FormatThousands(rawValue)` ngay lúc build view-model cho dòng nạp từ nguồn ngoài, đừng
trông chờ event UI tự chạy lại. Đã gặp + sửa ở `PriceSetupPage.LoadImportAsync`.

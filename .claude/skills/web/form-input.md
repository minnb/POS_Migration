---
name: web-form-input
description: Chuẩn form nhập liệu cơ bản trong POS.Web — nhóm field bằng MudCard, breakpoint, quy ước attribute, Placeholder vs HelperText, validation, action bar. Đọc khi tạo/sửa form nhập liệu; biến thể nâng cao ở form-input-special-modes.md.
---

# Skill: Thiết kế Form nhập liệu (MudBlazor)

> **Đọc file này khi:** tạo/sửa **form nhập liệu** trong `src/POS.Web/` — trang tạo/sửa bản ghi,
> tab editor, dialog form có nhiều trường. Chuẩn hoá cách bố cục + validation + trợ giúp End-user.
>
> **Nguồn mẫu chuẩn:** tab "Thông tin chung" của
> `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`.
>
> Liên quan: [`ui-polish-standard.md §8`](ui-polish-standard.md) (polish markup-only),
> [`audit-logging.md`](audit-logging.md) (bắt buộc khi form có Create/Update/Delete),
> [`datatable.md`](datatable.md) (bảng dữ liệu / dòng con dạng grid).

---

## 1. Nguyên tắc cốt lõi

1. **Mỗi nhóm field ngữ nghĩa = 1 `MudCard`** (`Elevation="1"`, `Class="mb-4"`). Không để 1 lưới field
   phẳng dài — chia thành các section có tiêu đề để End-user quét nhanh.
2. **`MudCardHeader`** gồm: `CardHeaderAvatar` (icon nhóm) + `CardHeaderContent` (title `Typo.subtitle1`
   + caption `Typo.caption` mô tả ngắn) + `CardHeaderActions` (icon `HelpOutline` bọc `MudTooltip`).
3. **`MudCardContent`** chứa `MudGrid Spacing="2"` + các `MudItem` responsive.
4. **Mọi input:** `Variant="Variant.Outlined"` + `Margin="Margin.Dense"` (chuẩn flat/density dự án;
   ngoại lệ `MudNumericField` kiểu `int` → xem §4a).
5. **Validation trực quan** (không cần MudForm/@code): `Required="true"` + `RequiredError="..."`.
6. **Trợ giúp:** `HelperText` cho field khó + tooltip ở header nhóm.
7. **Readonly mode:** mọi input nhận `Disabled="@_isReadonly"` (hoặc điều kiện tương ứng).

### 1a. Nhóm con bo viền trong 1 `MudCard` (khi cần gộp nhiều nhóm vào 1 khối)

> Dùng khi các nhóm field liên quan chặt đến CÙNG 1 bản ghi và việc tách hẳn nhiều `MudCard`
> gây phân mảnh màn hình (vd nhiều field ngắn thuộc cùng 1 entity). Thay vì lồng `MudCard` trong
> `MudCard` (double shadow xấu), dùng `MudPaper Outlined="true" Class="pa-3 mb-3 rounded-lg"` làm
> khối con — có viền + bo góc theo theme, không có elevation riêng. Khối con cuối cùng bỏ `mb-3`.
> Nếu cả trang chỉ còn 1 khối `MudCard` duy nhất bọc các nhóm con (không cần title/caption tổng
> quát riêng) → bỏ luôn `MudCardHeader`, `MudCard` chỉ còn `MudCardContent` — gọn hơn, đỡ tốn 1
> hàng chiều cao. Vẫn cân nhắc giữ `MudCardHeader` nếu trang có nhiều `MudCard` khác nhau cần
> phân biệt bằng title.

**Tiêu đề nhóm con kiểu "legend lồng vào viền"** (thay vì `MudText` nằm trong luồng nội dung, tốn
thêm 1 dòng cao): đặt `Style="position:relative"` trên `MudPaper`, tiêu đề dùng `position:absolute`
đè lên viền trên + nền `background:var(--mud-palette-surface)` để "cắt" viền tại vị trí chữ (hiệu
ứng giống `<fieldset><legend>` nhưng vẫn 100% MudBlazor component + inline style nhỏ — không dùng
raw HTML). Thêm `Class="mt-1"` cho `MudGrid` bên trong để field không dính sát viền.

```razor
<MudPaper Outlined="true" Class="pa-3 mb-3 rounded-lg" Style="position:relative">
    <MudText Typo="Typo.subtitle2" Color="Color.Primary"
             Style="position:absolute; top:-11px; left:12px; padding:0 6px;
                    background:var(--mud-palette-surface); line-height:1;">
        Tên nhóm con
    </MudText>
    <MudGrid Spacing="2" Class="mt-1">
        @* fields *@
    </MudGrid>
</MudPaper>
```

> Ví dụ thực tế: `CouponIssuePage.razor` (§11).

---

## 2. Template chuẩn — 1 section form

```razor
<MudCard Elevation="1" Class="mb-4">
    <MudCardHeader>
        <CardHeaderAvatar>
            <MudIcon Icon="@Icons.Material.Filled.Info" Color="Color.Primary"/>
        </CardHeaderAvatar>
        <CardHeaderContent>
            <MudText Typo="Typo.subtitle1">Tên nhóm</MudText>
            <MudText Typo="Typo.caption" Color="Color.Secondary">Mô tả ngắn nhóm field này</MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudTooltip Text="Giải thích chi tiết / lưu ý cho nhóm field.">
                <MudIcon Icon="@Icons.Material.Filled.HelpOutline" Color="Color.Default"/>
            </MudTooltip>
        </CardHeaderActions>
    </MudCardHeader>
    <MudCardContent>
        <MudGrid Spacing="2">
            <MudItem xs="12">
                <MudTextField @bind-Value="_model.Name" Label="Tên" Required="true"
                              RequiredError="Vui lòng nhập tên" Disabled="_isReadonly"
                              Variant="Variant.Outlined" Margin="Margin.Dense"
                              HelperText="Tên hiển thị cho người dùng cuối"/>
            </MudItem>
            <MudItem xs="12" sm="6" md="4">
                <MudSelect @bind-Value="_model.Type" Label="Loại" Required="true"
                           RequiredError="Vui lòng chọn loại" Disabled="_isReadonly"
                           Variant="Variant.Outlined" Margin="Margin.Dense"
                           HelperText="Nhóm phân loại">
                    @foreach (var o in _typeOptions)
                    {
                        <MudSelectItem T="string" Value="@o.Value">@o.Text</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>
            <MudItem xs="12" sm="6" md="4">
                <MudDatePicker @bind-Date="_fromDate" Label="Từ ngày" Required="true"
                               RequiredError="Vui lòng chọn ngày" Disabled="_isReadonly"
                               Variant="Variant.Outlined" Margin="Margin.Dense" DateFormat="dd/MM/yyyy"/>
            </MudItem>
            <MudItem xs="12" Class="d-flex align-center">
                <MudCheckBox @bind-Value="_model.Flag" Label="Bật tính năng" Disabled="_isReadonly"
                             Color="Color.Primary"/>
                <MudTooltip Text="Giải thích checkbox này bật/tắt điều gì.">
                    <MudIcon Icon="@Icons.Material.Filled.HelpOutline" Size="Size.Small"
                             Color="Color.Default" Class="ml-1"/>
                </MudTooltip>
            </MudItem>
        </MudGrid>
    </MudCardContent>
</MudCard>
```

> Section cuối cùng của form: bỏ `mb-4` (dùng `Class=""` hoặc không set) để không thừa khoảng dưới.
> Section chỉ hiện có điều kiện: bọc cả `<MudCard>` trong `@if (...) { }` (KHÔNG ẩn từng field lẻ).

---

## 3. Responsive breakpoint cho `MudItem`

| Loại field | Cột khuyến nghị | Ghi chú |
|---|---|---|
| Field dài (tên, mô tả, địa chỉ) | `xs="12"` | 1 field/hàng mọi màn hình |
| Field ngắn (select, date, số) — 3/hàng | `xs="12" sm="6" md="4"` | mobile 1 cột, tablet 2, desktop 3 |
| Field ngắn — 2/hàng (cặp từ/đến ngày) | `xs="12" sm="6" md="6"` | |
| Field ngắn — 4/hàng (nhóm số nhỏ) | `xs="12" sm="6" md="3"` | |
| Checkbox / switch | `xs="12" Class="d-flex align-center"` | tự canh giữa dọc với tooltip |

> Luôn bắt đầu `xs="12"` (mobile 1 cột). `MudGrid` luôn có `Spacing="2"` (chuẩn density §15 CLAUDE.md).

---

## 4. Quy ước attribute field (BẮT BUỘC)

| Attribute | Giá trị | Áp dụng |
|---|---|---|
| `Variant` | `Variant.Outlined` | mọi input **trừ `MudNumericField` kiểu `int`** (xem ngoại lệ bên dưới) |
| `Margin` | `Margin.Dense` | mọi input |
| `Disabled` | `@_isReadonly` (hoặc điều kiện) | mọi input — hỗ trợ chế độ chỉ xem |
| `Required` + `RequiredError` | field bắt buộc | tự hiện dấu `*` + báo đỏ inline (KHÔNG chặn Save) |
| `HelperText` | chuỗi tĩnh ngắn | field khó hiểu / có quy ước (vd "0 = không giới hạn") |
| `Clearable="true"` | field optional cho phép xoá | select/textfield không bắt buộc |
| `DateFormat="dd/MM/yyyy"` | `MudDatePicker` | chuẩn ngày VN toàn dự án |

### 4a. Ngoại lệ Variant cho `MudNumericField` — phân biệt theo kiểu dữ liệu C#

> Chốt từ phiên thiết kế `CouponIssuePage.razor` (Phát hành Coupon). Áp dụng cho MỌI
> `MudNumericField` mới trong dự án — kiểu C# của property quyết định Variant, không dùng
> `Variant.Outlined` mặc định như các input khác.

| Kiểu C# của property | Variant | Ghi chú |
|---|---|---|
| `int` (số nguyên) | `Variant.Text` | Giữ `HideSpinButtons="true"` + `Min`/`Max` theo field |
| `double` / `decimal` (số thập phân) | `Variant.Outlined` | Thêm `Step` phù hợp (vd `0.1`) để gợi ý bước nhảy thập phân |

```razor
@* int — Variant.Text *@
<MudNumericField @bind-Value="_model.Quantity" Label="Số lượng" Variant="Variant.Text"
                 Min="0" HideSpinButtons="true" HelperText="Số lượng mã coupon cần sinh"/>

@* double/decimal — Variant.Outlined + Step *@
<MudNumericField @bind-Value="_model.DiscountValue" Label="Giá trị giảm giá" Variant="Variant.Outlined"
                 Min="0" Step="0.1" HelperText="% (tối đa 100) hoặc VNĐ, tuỳ Kiểu giảm giá"/>
```

**Icon gợi ý cho header nhóm:** `Info` (thông tin cơ bản), `DateRange` (thời gian),
`Sell` (giá/số lượng), `CardMembership` (thành viên/khách), `Settings`/`Tune` (nâng cao),
`Store` (cửa hàng/địa điểm), `CardGiftcard` (voucher/quà).

### 4b. `Placeholder` vs `HelperText` — khi nào dùng cái nào

> Chốt từ audit `VoucherIssuePage.razor` (Phát hành Voucher). Trước đó skill chỉ có `HelperText`,
> thiếu quy tắc khi nào nên dùng `Placeholder`. **KHÔNG dùng cả 2 trên cùng 1 field** — MudBlazor chỉ
> hiện được 1 trong 2 tại cùng vị trí, `HelperText` sẽ đè mất `Placeholder`.

| Thuộc tính | Hành vi UI | Dùng khi |
|---|---|---|
| `HelperText="..."` | Text nhỏ **LUÔN hiển thị** dưới field, bất kể có giá trị hay không | Giải thích quy tắc/định dạng/giới hạn cần nhớ **trong lúc nhập** (vd "0 = không giới hạn") |
| `Placeholder="..."` | Text mờ **CHỈ hiện khi field rỗng**, biến mất ngay khi có giá trị | Gợi ý cho field optional hoặc field tự sinh — chỉ có ý nghĩa **khi đang rỗng** |

```razor
@* Field tự sinh (ReadOnly) — chỉ cần placeholder, không cần helper vì user không gõ được *@
<MudTextField Value="@_model.ItemNo" Label="Mã phát hành" ReadOnly="true"
              Variant="Variant.Outlined" Margin="Margin.Dense" Placeholder="Tự sinh khi lưu"/>

@* Field optional — placeholder giải thích ý nghĩa khi để trống *@
<MudTextField @bind-Value="_model.Serial" Label="Số serial"
              Variant="Variant.Outlined" Margin="Margin.Dense"
              Placeholder="Tùy chọn — để trống nếu phát hành theo mã"/>

@* Field cần nhớ quy tắc khi nhập — dùng HelperText (không biến mất khi gõ) *@
<MudNumericField @bind-Value="_model.LimitQty" Label="Giới hạn số lượng" Variant="Variant.Text"
                 Min="0" HideSpinButtons="true" HelperText="0 = không giới hạn"/>
```

---

## 5. Validation — trực quan, KHÔNG chặn Save (mặc định dự án)

- Dùng `Required="true"` + `RequiredError="..."` → MudBlazor tự hiện `*` + báo đỏ khi field bị chạm & rỗng.
- **KHÔNG** bọc `MudForm` + kiểm `IsValid` trong `SaveAsync` nếu không có yêu cầu chặn thật —
  server (SP/service) vẫn validate như hợp đồng hiện có.
- Field `Disabled` không tham gia validate → chế độ chỉ xem an toàn.

> Nếu **cần chặn Save thật** (yêu cầu rõ ràng): bọc `<MudForm @ref="_form">`, gọi `await _form.Validate()`
> + kiểm `_form.IsValid` đầu `SaveAsync`. Đây là thay đổi `@code` — chỉ làm khi được yêu cầu.

### 5a. Business rule phụ thuộc field khác (vd Percent vs Amount)

> Chốt từ `CouponIssuePage.razor`/`VoucherIssuePage.razor` — field "Giá trị giảm giá" đơn vị đổi
> theo field "Kiểu giảm giá" (`DiscountType`): `1` = Percent (%, giới hạn `0 < x <= 100`), `2` =
> Amount (VNĐ, không giới hạn %). **KHÔNG** so sánh trực tiếp field này với field khác đơn vị
> khác (vd VNĐ) khi đang ở chế độ %.

```csharp
// Property tính Max động cho MudNumericField — đặt cạnh IsAuto/IsEditing trong @code
private double DiscountValueMax => _model.DiscountType == 1 ? 100 : double.MaxValue;
```

```razor
<MudNumericField @bind-Value="_model.DiscountValue" Label="Giá trị giảm giá (%/VNĐ)"
                 Variant="Variant.Outlined" Min="0" Max="@DiscountValueMax" Step="0.1"
                 Required="true" RequiredError="Vui lòng nhập giá trị giảm giá" Margin="Margin.Dense"/>
```

```csharp
// Validate chặn Save thật (business rule, khác Required trực quan §5) — đặt đầu SaveAsync/ValidateHeaderFields
if (_model.DiscountType == 1 && (_model.DiscountValue <= 0 || _model.DiscountValue > 100))
{
    Snackbar.Add("Giá trị giảm giá theo % phải lớn hơn 0 và không vượt quá 100", Severity.Warning);
    return;
}
```

- `Max` động trên `MudNumericField` chỉ giới hạn UX (spinner/nhập tay không tự chặn hết mọi cách
  gõ) — **vẫn bắt buộc** validate lại đầu `SaveAsync` để chặn Save thật.
- Không thêm rule so sánh 2 field khác đơn vị (vd "giảm tối đa VNĐ" so với "% giảm giá") — sai
  nghiệp vụ, luôn false hoặc luôn true tuỳ giá trị nhập, gây chặn/bỏ sót vô lý.

> Ví dụ thực tế: `CouponIssuePage.razor`, `VoucherIssuePage.razor`.

---

## 6. Action bar (Lưu / nút chính)

> Variant/Color mỗi nút theo **bảng Button convention v3** (`.claude/rules/mudblazor-flat-ui.md` §3):
> "Lưu" = CTA `Filled`/`Primary`; "Duyệt"/"Kích hoạt" = `Filled`/`Success`; "Hủy"/"Đóng" = `Outlined`
> trung tính; "Xóa" = `Outlined`/`Error`. Nhất quán với `ui-polish-standard.md §5`.

Đặt NGOÀI `MudTabs`/danh sách card, bọc `@if (!_isReadonly)`:

```razor
<MudPaper Elevation="1" Class="pa-4 mt-4 d-flex justify-end gap-2 flex-wrap">
    <MudTooltip Text="Lưu bản ghi">
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="_saving" OnClick="SaveAsync">
            @if (_saving) { <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2"/> }
            else { <MudIcon Icon="@Icons.Material.Filled.Save" Class="mr-2"/> }
            Lưu
        </MudButton>
    </MudTooltip>
</MudPaper>
```

> MudBlazor v9 **không có** prop `Loading` cho `MudButton` — render spinner theo cờ `_saving` (xem `ui-polish-standard.md §8`).

> **Ngoại lệ hợp lệ — trang full-page (không phải dialog/tab con):** nút Lưu đặt trong
> `div.pos-page-header` (`pos-page-header-btn`), cạnh nút "Danh sách"/"Quay lại" — KHÔNG cần thêm
> action bar riêng ở cuối trang. Áp dụng cho page dạng "Phát hành/Tạo mới" điều hướng bằng URL riêng
> (vd `CouponIssuePage.razor`, `VoucherIssuePage.razor`) thay vì dialog. Vẫn giữ spinner theo `_saving`.

---

## 7. Checklist khi tạo form nhập liệu

```
□ Mỗi nhóm field = 1 MudCard (Elevation="1" mb-4); section cuối bỏ mb-4
□ MudCardHeader: avatar icon + title (subtitle1) + caption + help tooltip (CardHeaderActions)
□ MudCardContent > MudGrid Spacing="2" > MudItem responsive (bắt đầu xs="12")
□ Mọi input: Variant.Outlined + Margin.Dense + Disabled="@_isReadonly"
□ Field bắt buộc: Required + RequiredError (không chặn Save trừ khi yêu cầu)
□ Field khó: HelperText ngắn gọn
□ Checkbox/switch: xs="12" d-flex align-center + tooltip help nếu cần
□ Section điều kiện: bọc cả MudCard trong @if
□ Action bar: MudPaper justify-end + nút loading spinner theo _saving
□ Form có Create/Update/Delete → áp audit-logging.md
□ Build + dotnet test ContractTests xanh
```

**Bổ sung khi bản ghi bị khóa vĩnh viễn sau khi tạo (view-only + field ngoại lệ)** — xem
[`form-input-special-modes.md`](form-input-special-modes.md) mục 1-2 cho pattern đầy đủ:
```
□ Chế độ xem dùng cặp MudText label/value — KHÔNG dùng Disabled cho toàn bộ input
□ Field ngoại lệ vẫn tương tác được, đặt trong MudPaper con riêng + ghi chú caption
□ Nút Lưu ẩn mặc định, chỉ hiện khi field ngoại lệ đổi giá trị (snapshot _originalX + XChanged)
□ Field ngoại lệ gọi API/SP RIÊNG (không gọi lại SP Save đầy đủ)
```

---

## 8. Anti-patterns

- ❌ Lưới field phẳng 1 khối lớn không chia section → khó đọc, không có ngữ cảnh.
- ❌ `MudPaper` + `subtitle2` + `MudDivider` cho section form mới → dùng `MudCard` + `MudCardHeader` (chuẩn form).
- ❌ Quên `Margin="Margin.Dense"` / `Variant="Variant.Outlined"` → lệch density & flat standard.
- ❌ Quên `Disabled="@_isReadonly"` → form "chỉ xem" vẫn sửa được.
- ❌ Ẩn từng field lẻ trong section điều kiện thay vì bọc cả `MudCard` trong `@if`.
- ❌ Dùng `MudButton Loading="..."` (không tồn tại v9) hoặc `StartIcon` + spinner cùng lúc.
- ❌ Hardcode `border-radius`/`box-shadow`/width px cho field — để theme + MudGrid xử lý.
- ❌ Dùng `Variant.Outlined` cho `MudNumericField` kiểu `int`, hoặc `Variant.Text` cho kiểu
  `double`/`decimal` — ngược chuẩn ngoại lệ ở §4a.
- ❌ Vừa dùng `HelperText` vừa `Placeholder` trên cùng 1 field → `HelperText` đè mất `Placeholder`
  (xem §4b, chọn đúng 1 theo bảng).
- ❌ Dùng `Disabled="@_isReadonly"` để mô phỏng bản ghi **khóa vĩnh viễn sau khi tạo** → field vẫn
  trông như ô nhập, gây hiểu lầm còn sửa được. Dùng cặp `MudText` label/value (§9) khi nghiệp vụ
  quy định không còn sửa lại được (khác readonly TẠM THỜI mà `Disabled` vẫn đúng).
- ❌ Field ngoại lệ (được phép sửa dù bản ghi đã khóa) hiện nút Lưu **LUÔN LUÔN** thay vì chỉ khi
  giá trị thực sự đổi (§10) — user bấm Lưu vô ích, tạo audit log rác không có thay đổi thật.

---

## 9. Biến thể nâng cao — xem file riêng

> Chế độ CHỈ XEM (view-only, bản ghi khóa vĩnh viễn), sửa 1 field ngoại lệ với nút Lưu điều kiện,
> `MudTimePicker`, `MudSelect` multi-selection, format số tiền khi nhập — đã tách sang
> **[`form-input-special-modes.md`](form-input-special-modes.md)**, đọc file đó khi gặp đúng
> tình huống tương ứng.

---

## 10. Tham chiếu

| Loại | File |
|---|---|
| Form mẫu chuẩn (5 tab, nhiều section MudCard) | `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` |
| Dialog form (trả DTO đầy đủ cho audit) | `src/POS.Web/Components/Pages/Ops/Dialogs/PosDataSetupFormDialog.razor` |
| Nhóm con bo viền trong 1 MudCard + MudNumericField Variant theo kiểu dữ liệu (§4a) + Business rule phụ thuộc field khác (§5a) | `src/POS.Web/Components/Pages/Promotion/CouponVoucher/CouponIssuePage.razor` |
| Placeholder vs HelperText (§4b), biến thể nâng cao (§9) | `src/POS.Web/Components/Pages/Promotion/CouponVoucher/VoucherIssuePage.razor` |
| Polish markup-only (tooltip/validation/loading) | `.claude/skills/web/ui-polish-standard.md` §8 |
| Audit CRUD sau khi lưu | `.claude/skills/web/audit-logging.md` |

# Skill: Thiết kế Form nhập liệu (MudBlazor)

> **Đọc file này khi:** tạo/sửa **form nhập liệu** trong `src/POS.Web/` — trang tạo/sửa bản ghi,
> tab editor, dialog form có nhiều trường. Chuẩn hoá cách bố cục + validation + trợ giúp End-user.
>
> **Nguồn mẫu chuẩn:** tab "Thông tin chung" của
> `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`.
>
> Liên quan: [`ui-migrate-legacy.md §8`](ui-migrate-legacy.md) (polish markup-only),
> [`audit-logging.md`](audit-logging.md) (bắt buộc khi form có Create/Update/Delete),
> [`datatable.md`](datatable.md) (bảng dữ liệu / dòng con dạng grid).

---

## 1. Nguyên tắc cốt lõi

1. **Mỗi nhóm field ngữ nghĩa = 1 `MudCard`** (`Elevation="1"`, `Class="mb-4"`). Không để 1 lưới field
   phẳng dài — chia thành các section có tiêu đề để End-user quét nhanh.
2. **`MudCardHeader`** gồm: `CardHeaderAvatar` (icon nhóm) + `CardHeaderContent` (title `Typo.subtitle1`
   + caption `Typo.caption` mô tả ngắn) + `CardHeaderActions` (icon `HelpOutline` bọc `MudTooltip`).
3. **`MudCardContent`** chứa `MudGrid Spacing="2"` + các `MudItem` responsive.
4. **Mọi input:** `Variant="Variant.Outlined"` + `Margin="Margin.Dense"` (chuẩn flat/density dự án).
5. **Validation trực quan** (không cần MudForm/@code): `Required="true"` + `RequiredError="..."`.
6. **Trợ giúp:** `HelperText` cho field khó + tooltip ở header nhóm.
7. **Readonly mode:** mọi input nhận `Disabled="@_isReadonly"` (hoặc điều kiện tương ứng).

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
| `Variant` | `Variant.Outlined` | mọi input |
| `Margin` | `Margin.Dense` | mọi input |
| `Disabled` | `@_isReadonly` (hoặc điều kiện) | mọi input — hỗ trợ chế độ chỉ xem |
| `Required` + `RequiredError` | field bắt buộc | tự hiện dấu `*` + báo đỏ inline (KHÔNG chặn Save) |
| `HelperText` | chuỗi tĩnh ngắn | field khó hiểu / có quy ước (vd "0 = không giới hạn") |
| `Clearable="true"` | field optional cho phép xoá | select/textfield không bắt buộc |
| `DateFormat="dd/MM/yyyy"` | `MudDatePicker` | chuẩn ngày VN toàn dự án |

**Icon gợi ý cho header nhóm:** `Info` (thông tin cơ bản), `DateRange` (thời gian),
`Sell` (giá/số lượng), `CardMembership` (thành viên/khách), `Settings`/`Tune` (nâng cao),
`Store` (cửa hàng/địa điểm), `CardGiftcard` (voucher/quà).

---

## 5. Validation — trực quan, KHÔNG chặn Save (mặc định dự án)

- Dùng `Required="true"` + `RequiredError="..."` → MudBlazor tự hiện `*` + báo đỏ khi field bị chạm & rỗng.
- **KHÔNG** bọc `MudForm` + kiểm `IsValid` trong `SaveAsync` nếu không có yêu cầu chặn thật —
  server (SP/service) vẫn validate như hợp đồng hiện có.
- Field `Disabled` không tham gia validate → chế độ chỉ xem an toàn.

> Nếu **cần chặn Save thật** (yêu cầu rõ ràng): bọc `<MudForm @ref="_form">`, gọi `await _form.Validate()`
> + kiểm `_form.IsValid` đầu `SaveAsync`. Đây là thay đổi `@code` — chỉ làm khi được yêu cầu.

---

## 6. Action bar (Lưu / nút chính)

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

> MudBlazor v9 **không có** prop `Loading` cho `MudButton` — render spinner theo cờ `_saving` (xem `ui-migrate-legacy.md §8`).

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

---

## 8. Anti-patterns

- ❌ Lưới field phẳng 1 khối lớn không chia section → khó đọc, không có ngữ cảnh.
- ❌ `MudPaper` + `subtitle2` + `MudDivider` cho section form mới → dùng `MudCard` + `MudCardHeader` (chuẩn form).
- ❌ Quên `Margin="Margin.Dense"` / `Variant="Variant.Outlined"` → lệch density & flat standard.
- ❌ Quên `Disabled="@_isReadonly"` → form "chỉ xem" vẫn sửa được.
- ❌ Ẩn từng field lẻ trong section điều kiện thay vì bọc cả `MudCard` trong `@if`.
- ❌ Dùng `MudButton Loading="..."` (không tồn tại v9) hoặc `StartIcon` + spinner cùng lúc.
- ❌ Hardcode `border-radius`/`box-shadow`/width px cho field — để theme + MudGrid xử lý.

---

## 9. Tham chiếu

| Loại | File |
|---|---|
| Form mẫu chuẩn (5 tab, nhiều section MudCard) | `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` |
| Dialog form (trả DTO đầy đủ cho audit) | `src/POS.Web/Components/Pages/Ops/Dialogs/PosDataSetupFormDialog.razor` |
| Polish markup-only (tooltip/validation/loading) | `.claude/skills/web/ui-migrate-legacy.md` §8 |
| Audit CRUD sau khi lưu | `.claude/skills/web/audit-logging.md` |

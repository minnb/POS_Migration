---
name: web-ui-polish-standard
description: Checklist + pattern polish markup-only (giữ nguyên @code) — status badge dot-pill, NoRecordsContent, tách nhóm field, action bar, tooltip/validation/loading thân thiện. Đọc khi nhận yêu cầu "làm đẹp UI"/"sync UI".
---

# Skill: UI Polish — Chuẩn đồng bộ giao diện MudBlazor

> **Đọc file này khi:** nhận yêu cầu "làm đẹp UI", "sync UI", "đồng bộ giao diện",
> hoặc bất kỳ task nào chỉ sửa markup mà **không động tới `@code {}`**.
>
> **Ràng buộc bất di bất dịch:** GIỮ NGUYÊN 100% `@code { }` — method, biến, API call,
> event handler. Chỉ bọc/đổi phần HTML/Razor markup. Không thêm helper màu vào `@code` —
> dùng ternary inline ngay tại attribute markup.

---

## 1. Checklist áp dụng (tự check theo thứ tự)

> Giá trị chuẩn (Elevation/Button convention/Density) nay là "luật thép" ở
> `.claude/rules/blazor-web-app.md` (§10, §17) và `.claude/rules/mudblazor-flat-ui.md`
> — checklist dưới đây chỉ là danh sách thao tác polish, không lặp lại giá trị chi tiết.

```
□ 1. Page header có button  → div.pos-page-header (KHÔNG MudStack Justify.SpaceBetween),
                              icon + nút đi kèm dùng Size="Size.Small" (title đã thu nhỏ 1.25rem)
□ 2. Page header chỉ title  → MudText Typo.h5 trực tiếp (không cần pos-page-header)
□ 3. Filter panel           → blazor-web-app.md §17.2 / mudblazor-flat-ui.md §7 (MudPaper Elevation="1" pos-filter-panel)
□ 4. Filter button group    → MudItem xs="12" sm="12" md="2" Class="d-flex align-center"
                              + MudStack Row Spacing="1" w-100 + FullWidth="true" mỗi nút
                              — màu nút theo Button convention `.claude/rules/mudblazor-flat-ui.md` §3
□ 5. DataTable              → blazor-web-app.md §17.2 (Elevation trên MudTable, Dense/Hover/Striped/HorizontalScrollbar)
□ 6. Cột Trạng thái (tĩnh)  → span.pos-status-chip .pos-status-{success/error/warning/info}
                              (MudChip chỉ khi cần tương tác: multi-select/closable/filter)
□ 7. NoRecordsContent       → pattern §3 file này (icon + text canh giữa) — blazor-web-app.md §17.2 dẫn cùng pattern
□ 8. Editor tab "Thông tin" → tách nhóm field có subtitle + divider (pattern §4)
□ 9. Action bar Lưu/Duyệt  → MudPaper Elevation="1" pa-3 mt-4 justify-end (pattern §5)
                              — màu nút theo Button convention `.claude/rules/mudblazor-flat-ui.md` §3
□ 10. Chip container        → d-flex flex-wrap gap-2 (KHÔNG thiếu flex-wrap)
□ 11. Spacing               → chỉ mb-1..4, pa-2..4, gap-1..3 (CẤM mb-5/6, pa-5/6, gap-4+)
□ 12. Input form            → Variant.Outlined + Margin.Dense (tất cả field) — chi tiết form-input.md
```

---

## 2. Pattern: Cột Trạng thái

> **CHUẨN MẶC ĐỊNH v3 (2026-07-09):** badge trạng thái **tĩnh** trong `MudTable`/dialog dùng
> `<span class="pos-status-chip pos-status-{success,error,warning,info}">Label</span>` (dot-pill nền
> tint nhạt + chữ đậm cùng tông + chấm tròn — xem `.claude/rules/mudblazor-flat-ui.md` §4a). `MudChip`
> **chỉ** dùng khi chip cần **tương tác** (multi-select, closable `OnClose`, chip trong filter/
> `MudAutocomplete`). Pattern `MudChip` màu ternary bên dưới vẫn hợp lệ cho chip tương tác.

```razor
@* Badge tĩnh — CHUẨN MẶC ĐỊNH (không @code helper màu; helper trả (CssClass, Label) nếu cần) *@
<MudTd DataLabel="Trạng thái">
    <span class="pos-status-chip @(context.IsActive ? "pos-status-success" : "pos-status-error")">
        @(context.IsActive ? "Hiệu lực" : "Hết hiệu lực")
    </span>
</MudTd>
```

### Chip tương tác → `MudChip` màu (ternary inline)

**KHÔNG** thêm helper method vào `@code`. Dùng ternary inline ngay tại `Color=`:

```razor
@* Trạng thái 3 mức (0=hiệu lực, 1=đã duyệt, else=hết hạn) *@
<MudTd DataLabel="Trạng thái">
    <MudChip T="string" Size="Size.Small" Variant="Variant.Filled"
             Color="@(context.Status == "0" ? Color.Success
                    : context.Status == "1" ? Color.Info
                    : Color.Default)">
        @StatusText(context.Status)    @* gọi helper đã có trong @code — giữ nguyên *@
    </MudChip>
</MudTd>

@* Trạng thái 2 mức (bool / active-inactive) *@
<MudTd DataLabel="Trạng thái">
    <MudChip T="string" Size="Size.Small" Variant="Variant.Filled"
             Color="@(IsActive(context.Status) ? Color.Success : Color.Error)">
        @context.Status
    </MudChip>
</MudTd>
```

**Màu quy ước:**

| Ý nghĩa | Color |
|---------|-------|
| Đang hoạt động / Có hiệu lực | `Color.Success` |
| Đã duyệt / Đang chờ | `Color.Info` |
| Ngưng / Hết hiệu lực | `Color.Error` |
| Trung tính / Không xác định | `Color.Default` |

---

## 3. Pattern: Empty-state MudTable (NoRecordsContent)

Thay thế `<MudText Color="Color.Secondary" ...>Không có dữ liệu</MudText>` bằng:

```razor
<NoRecordsContent>
    <div class="d-flex flex-column align-center pa-4"
         style="color:var(--mud-palette-text-secondary)">
        <MudIcon Icon="@Icons.Material.Filled.Inbox" Size="Size.Large" Class="mb-2"/>
        <MudText Typo="Typo.body2">Không có {domain} khớp bộ lọc.</MudText>
    </div>
</NoRecordsContent>
```

> Giữ nguyên văn bản mô tả context-aware (vd "nhân viên", "khuyến mãi", "sản phẩm").
> Dùng `Icons.Material.Filled.Inbox` cho tất cả — nhất quán toàn app.

---

## 4. Pattern: Tab "Thông tin chung" — tách nhóm field

Một lưới field phẳng (`MudGrid`) trông như form khai. Tách thành 2–3 nhóm theo ngữ nghĩa,
mỗi nhóm có tiêu đề phụ + divider. **Không** di chuyển hay đổi binding của `MudItem`.

```razor
@* Nhóm 1 *@
<MudText Typo="Typo.subtitle2" Color="Color.Primary" Class="mb-1">
    <MudIcon Icon="@Icons.Material.Filled.Info" Size="Size.Small"
             Class="mr-1" Style="vertical-align:middle"/>
    Thông tin cơ bản
</MudText>
<MudDivider Class="mb-3"/>
<MudGrid Spacing="2">
    @* ...các MudItem hiện có, KHÔNG đổi binding... *@
</MudGrid>

@* Nhóm 2 — Class="mt-4" để tạo khoảng cách với nhóm trước *@
<MudText Typo="Typo.subtitle2" Color="Color.Primary" Class="mb-1 mt-4">
    <MudIcon Icon="@Icons.Material.Filled.DateRange" Size="Size.Small"
             Class="mr-1" Style="vertical-align:middle"/>
    Thời gian áp dụng
</MudText>
<MudDivider Class="mb-3"/>
<MudGrid Spacing="2">
    @* ...MudItem tiếp theo... *@
</MudGrid>
```

**Icon gợi ý theo ngữ nghĩa nhóm:**

| Nhóm | Icon |
|------|------|
| Thông tin cơ bản / tên / mã | `Icons.Material.Filled.Info` |
| Thời gian / ngày tháng | `Icons.Material.Filled.DateRange` |
| Giá / số lượng / tài chính | `Icons.Material.Filled.Sell` |
| Hội viên / khách hàng | `Icons.Material.Filled.CardMembership` |
| Cài đặt nâng cao / config | `Icons.Material.Filled.Settings` |
| Cửa hàng / địa điểm | `Icons.Material.Filled.Store` |

---

## 5. Pattern: Action bar Lưu/Duyệt

Thay `<div class="d-flex gap-2 flex-wrap mt-4">…</div>` bằng MudPaper có nền phân tách. Button
theo bảng Button convention v3 (`.claude/rules/mudblazor-flat-ui.md` §3) —
CTA/hành động tích cực dùng `Filled`, hành động trung tính/phá hủy dùng `Outlined`:

```razor
@* Giữ nguyên bao ngoài @if (!_isReadonly) nếu có *@
<MudPaper Elevation="1" Class="pa-3 mt-4 d-flex justify-end gap-2 flex-wrap">
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Save"
               Disabled="_saving" OnClick="SaveAsync">
        Lưu tạm
    </MudButton>
    @* Các nút khác (Duyệt, Hủy...) — giữ nguyên OnClick/Disabled; chọn Variant/Color theo bản
       chất hành động: Duyệt = Filled/Success (tích cực, chốt luồng), Hủy = Outlined (trung tính) *@
    @if (!string.IsNullOrEmpty(_header.No))
    {
        <MudButton Variant="Variant.Filled" Color="Color.Success"
                   StartIcon="@Icons.Material.Filled.CheckCircle"
                   Disabled="_saving" OnClick="@(() => ApproveAsync(_header.No))">
            Duyệt
        </MudButton>
    }
</MudPaper>
```

---

## 6. Quy tắc "không làm" khi polish UI

- ❌ Không sửa `@code { }`, không thêm biến/method/helper mới
- ❌ Không đổi `div.pos-page-header` sang `MudStack Justify.SpaceBetween` — đây là chuẩn dự án
- ❌ Không đổi binding (`@bind-Value`, `@onclick`, `ValueChanged`, `@foreach`, `@if`)
- ❌ Không đổi route, policy, rendermode
- ❌ Không thêm `pa-5/6`, `mb-5/6`, `gap-4+` — ngoài thang spacing chuẩn
- ❌ Không dùng `MudChip` mà thiếu `T="string"` — v9 bắt buộc
- ❌ Không bỏ `flex-wrap` trên chip container — chips tràn ngang mobile

---

## 7. Verification sau khi polish

```powershell
dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly
dotnet test tests/POS.ContractTests -nologo
```

Cả hai phải **xanh (0 error)** trước khi báo hoàn thành.

---

## 8. Pattern: Polish "thân thiện End-user" (MudCard + tooltip + validation trực quan + loading)
> Áp dụng khi: yêu cầu "đẹp/thân thiện hơn" nhưng vẫn **giữ 100% `@code`** (markup-only).

- **Gom nhóm bằng `MudCard`**: `MudCardHeader` (`CardHeaderAvatar` icon + `CardHeaderContent` title/caption +
  `CardHeaderActions` tooltip help) → `MudCardContent`. Khi bọc **MudTable trong MudCard** → đặt
  `Elevation="0"` cho MudTable để tránh box-in-box (2 lớp bóng).
- **Tooltip giải thích**: `MudTooltip` bọc `MudIcon HelpOutline` ở header card; `HelperText="..."` tĩnh cho
  field khó; tooltip bọc `<MudTh>` cho cột khó (đặt `<span>` bên trong).
- **Validation trực quan (KHÔNG cần MudForm/@code)**: `Required="true"` + `RequiredError="..."` → tự hiện dấu `*`
  + báo đỏ inline khi chạm & rỗng. KHÔNG chặn Save (server vẫn validate). Field `Disabled` không validate.
- **Nút loading (MudBlazor v9 KHÔNG có prop `Loading`)**: bỏ `StartIcon`, render trong content theo cờ sẵn có:
  ```razor
  @if (_saving) { <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2"/> }
  else { <MudIcon Icon="@Icons.Material.Filled.Save" Class="mr-2"/> }
  Lưu tạm
  ```
> Anti-pattern: ❌ `<MudButton Loading="...">` (không tồn tại v9). ❌ MudTable trong MudCard mà quên
> `Elevation="0"` → 2 lớp bóng. ❌ Bọc `MudForm` + check `IsValid` trong Save khi yêu cầu là markup-only (đụng `@code`).
> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor`

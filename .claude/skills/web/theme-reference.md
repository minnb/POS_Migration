# MudBlazor Theme Standard — Reference đầy đủ (POS.Web)

> Nguồn canonical duy nhất cho theme/màu/Input/Button/Card/Elevation/Sidebar/Typography MudBlazor —
> được `.claude/rules/02-blazor-mudblazor-ui.md` (luật thép, đọc trước) trỏ tới. Số mục (mục N) giữ
> nguyên qua các lần sửa — các skill/rule khác trích dẫn theo số mục này.
>
> **Bản v3** (chuyển hoàn toàn sang phong cách mockup `docs/web/theme/theme_html.html`): sidebar
> navy đậm, card có shadow thật, radius 2 cấp, Button Filled cho CTA. Lịch sử quyết định đầy đủ +
> TODO rollout còn thiếu: `docs/web/theme/theme-decision-log.md`.

## 0. Mapping HTML mockup → MudBlazor Component

> Dùng bảng này khi polish/tạo mới bất kỳ page/component nào đối chiếu với mockup HTML
> (`docs/web/theme/theme_html.html` hoặc mockup tương lai khác cùng ngôn ngữ thiết kế). Tổng hợp từ
> các quyết định đã áp dụng thật trong `MainLayout.razor`/`app.css`/59 file page — không suy đoán.

| HTML mockup | MudBlazor | Ghi chú |
|---|---|---|
| `div.sidebar` | `<MudDrawer Elevation="0">` | navy tự tách nền bằng màu, không cần shadow (xem mục 1) |
| `div.nav-section` (label, không icon) | `<MudNavGroup>` cấp L1, **không** đặt `Icon=` | CHỮ IN HOA qua CSS `app.css`, không qua C# |
| `div.nav-item` (có icon) | `<MudNavGroup>`/`<MudNavLink Icon="@Icons.Material.Outlined.X">` | `Icon=` nhận SVG path — **không** dùng emoji/text (xem mục 5) |
| `div.card` | `<MudPaper Elevation="2">` | shadow thật theo `Shadows.Elevation`, không tự thêm `box-shadow` CSS |
| `div.filter-bar` | `<MudPaper Elevation="1" Class="pos-filter-panel">` | flat, nền trắng + border (xem mục 7) |
| `table` | `<MudTable Dense="true" HorizontalScrollbar="true">` | không tự viết `<table class="pos-table">` cho DataTable mới (xem mục 4) |
| `.badge`/`.chip` (status) | `<MudChip T="string" Color="...">` | màu ternary inline tại `Color=`, không thêm helper `@code` |
| `button.btn-primary` | `<MudButton Variant="Variant.Filled" Color="Color.Primary">` | xem bảng đầy đủ ở mục 3 |
| `.kpi-value` | `<MudText Typo="Typo.h5">` trong `<MudPaper Elevation="2">` | xem mục 11 "KPI card row" |
| `input`/`select` | `<MudTextField>`/`<MudSelect> Variant="Variant.Outlined" Margin="Margin.Dense"` | xem mục 2 |

- Bảng này là **mapping cấu trúc** (thành phần nào dùng component nào) — màu sắc/spacing cụ thể
  tra ở các mục số bên dưới, KHÔNG lặp lại ở đây.
- Mockup mới có phần tử không nằm trong bảng → tìm component MudBlazor gần nghĩa nhất theo tinh
  thần "dùng component sẵn có của MudBlazor, không tự viết HTML thuần" — không tự chế `<div>` thay
  cho component đã có.

## 1. Surface & Shadow (Elevation) — có shadow thật, không còn borderless

- `Shadows.Elevation` trong `PosTheme.cs`: index 0-1 = `"none"` (flat, filter panel/toolbar);
  index 2-3 = `"0 2px 8px rgba(0,0,0,0.08)"` (card có shadow thật — khớp mockup `--shadow`);
  index 4-5 = `"0 4px 20px rgba(0,0,0,0.12)"` (shadow mạnh hơn — mockup `--shadow-lg`, dùng cho
  Login card). Index 6-25 giữ nguyên thang cũ, chỉ đổi tint sang navy mới nếu cần đồng bộ.
- `MudPaper`/`MudCard` chứa nội dung: `Elevation="2"` (card, có shadow). Filter panel/toolbar:
  `Elevation="1"` (flat, không shadow).
- **KHÔNG** tự thêm `border`/`box-shadow` CSS cho MudPaper/MudCard — dùng thuộc tính `Elevation`.
- **KHÔNG** hạ Elevation của `MudPopover`/`MudDialog`/`MudMenu` — giữ nguyên E8/E12.
- `MudDrawer` (sidebar): `Elevation="0"` — sidebar navy tự tách khỏi nền sáng bằng màu, không cần
  shadow riêng (nếu để Elevation cao sẽ tự nhận box-shadow theo mục 1, gây viền nổi thừa quanh
  sidebar).

## 2. Form & Input

- `MudTextField`/`MudSelect`/`MudDatePicker`: luôn `Variant="Variant.Outlined"` +
  `Margin="Margin.Dense"`. (Không đổi qua các bản.)
- Border-radius control (Button/Chip/Input): `8px` — ép riêng qua CSS (`.mud-button-root`,
  `.mud-chip`, `.mud-input-outlined-border` trong `app.css`), KHÔNG qua `DefaultBorderRadius`
  (giá trị đó nay dành cho Paper/Card/Dialog = `12px`, xem mục 8).
- **Font-size input**: `Typography.Body1` trong `PosTheme.cs` = `0.78125rem` (12.5px — khớp chính
  xác mockup `.input,.select,.textarea{font-size:12.5px}`) + `FontWeight "400"`. Xem mục 11 cho
  toàn bộ đợt rà soát Typography pixel-perfect.

## 3. Button — Filled cho CTA, Outlined cho phần còn lại

> Theo mockup, `.btn-primary` là nền đặc màu (Filled), không phải viền trong suốt.

| Loại hành động | Variant | Color | Ví dụ |
|---|---|---|---|
| CTA chính (Lưu, Thêm mới, Cập nhật, Tìm) | `Filled` | `Primary` | "Lưu", "Thêm mới", "Tìm" |
| Hành động tích cực chốt luồng (Duyệt, Kích hoạt) | `Filled` | `Success` | "Duyệt" |
| Phá hủy/không hoàn tác (Xóa, Khóa) | `Outlined` | `Error` | "Xóa" |
| Trung tính (Hủy/Đóng dialog, Xóa bộ lọc) | `Outlined` | *(không đặt Color)* | "Hủy", "Đóng" |
| Phụ có ngữ nghĩa riêng (Export Excel...) | `Outlined` | Color phù hợp | "Export Excel" |

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SearchAsync">Tìm</MudButton>
<MudButton Variant="Variant.Outlined" OnClick="ClearFilter">Xóa</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Error" OnClick="DeleteAsync">Xóa</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Success" OnClick="ApproveAsync">Duyệt</MudButton>
```

- "Lưu" LUÔN là CTA (`Filled`/`Primary`), KHÔNG phải `Success`. "Sửa"/"Thêm dòng" cũng xếp CTA.
- Nút điều hướng thuần túy (quay lại, chuyển trang) xếp Trung tính.
- Không rõ 1 nút thuộc loại nào → ưu tiên Trung tính (`Outlined`, không `Color`).
- **Bẫy confirm dialog**: `DialogService.ShowAsync<MudMessageBox>(...)` render nút Yes bằng
  markup mặc định, không sửa được — **KHÔNG** dùng cách gọi này. Luôn khai báo
  `<MudMessageBox @ref="_confirmBox">` trực tiếp + `<YesButton>` tường minh, chọn Variant/Color
  theo bản chất hành động Yes (bảng trên). Dialog dùng chung nhiều hành động (khóa/mở khóa) →
  ternary inline dựa trên field/biến message có sẵn, không thêm field mới — vd
  `ProductLockPage.razor`, `UsersPage.razor` (`_confirmYesColor`). Pattern đầy đủ:
  `.claude/skills/web/SKILL.md` §"MudMessageBox @ref".

### 3a. Class CSS phụ trợ cho MudButton — `pos-btn-mockup` / `pos-btn-secondary-mockup`

> 2 class định nghĩa trong `app.css:161-168`:
> ```css
> .pos-btn-mockup { padding: 7px 14px !important; }
> .pos-btn-secondary-mockup { background-color: var(--pos-bg-alt) !important; color: var(--pos-text-body) !important; border: 1px solid var(--pos-border) !important; }
> ```

- **`pos-btn-mockup`**: thêm vào **MỌI** `MudButton` (Filled và Outlined, mọi Color) — chỉ chỉnh
  padding khớp mockup `theme_html.html .btn{padding:7px 14px}`, an toàn với mọi Color.
- **`pos-btn-secondary-mockup`**: **CHỈ** thêm vào `MudButton Variant="Variant.Outlined"` **trung
  tính** (không đặt `Color`, ví dụ "Hủy", "Đóng", "Xóa lọc", "Export Excel") — class này ép cứng
  `background/color/border` sang tông xám trung tính. **KHÔNG** thêm vào `Outlined` có `Color`
  ngữ nghĩa (`Color.Error` phá hủy, `Color.Success`...) — sẽ xóa mất tín hiệu màu, phá vỡ ý nghĩa
  "Phá hủy" ở bảng Variant/Color trên. Outlined có Color chỉ nhận `pos-btn-mockup`.

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" Class="pos-btn-mockup" OnClick="SearchAsync">Tìm</MudButton>
<MudButton Variant="Variant.Outlined" Class="pos-btn-mockup pos-btn-secondary-mockup" OnClick="ClearFilter">Xóa</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Error" Class="pos-btn-mockup" OnClick="DeleteAsync">Xóa</MudButton>
```

## 4. Bảng dữ liệu (MudTable)

- Luôn dùng `<MudTable>`, `Dense="true"` **bắt buộc**, `Hover="true"`, `Striped="true"`,
  `HorizontalScrollbar="true"`.
- Header bảng (`.mud-table .mud-table-head .mud-table-cell` và `.pos-table thead`): nền
  `var(--pos-bg-alt)`, chữ `var(--pos-text-muted)` (muted, không phải heading đậm), **in hoa**
  (`text-transform:uppercase`), `font-size: 0.6875rem`, `letter-spacing:0.4px`, border-bottom 2px
  — khớp `th` mockup.
- `.rpt-pivot-table` (pivot report) **giữ nguyên** viền cyan cũ — ngoài phạm vi, thiết kế riêng.
- `MudTablePager` `PageSizeOptions` luôn bắt đầu bằng `10`.

### 4a. Status badge dạng dot-pill — CHUẨN MẶC ĐỊNH

- **`.pos-status-chip` + modifier `.pos-status-{success,error,warning,info}`** (`app.css`, đặt cạnh
  `.pos-delta-up/.pos-delta-down`) là **CHUẨN MẶC ĐỊNH** cho mọi badge tĩnh hiển thị trạng thái/
  phân loại/hình thức trong `MudTable` hoặc dialog chi tiết — "viên thuốc nền tint nhạt + chữ đậm
  cùng tông + chấm tròn nhỏ bên trái". Markup: `<span class="pos-status-chip pos-status-success">
  Label</span>` — dot vẽ bằng `::before` + `background-color:currentColor`, không cần markup con
  riêng.
- **`MudChip`** (`Variant="Variant.Filled"`, nền đặc + `ContrastText`) chỉ còn dùng khi chip cần
  **tương tác thật** (multi-select, có nút đóng `OnClose`, chip trong `MudAutocomplete`/filter
  chọn nhiều) — KHÔNG dùng cho badge tĩnh chỉ hiển thị label.
- Precedent gốc của kỹ thuật "pill nền tint dùng token `--pos-{semantic}`/`--pos-{semantic}-bg`"
  là `Components/Shared/PosDeltaBadge.razor` (dùng `<div>` thuần, không `MudChip`) — áp dụng đúng
  tiền lệ này khi cần thêm biến thể badge mới, không phát minh cách khác.
- Helper hiển thị màu trả về `(string CssClass, string Label)` thay vì `(Color Color, string Label)`
  khi dùng kiểu badge này (xem `TransTypeDisplay`/`ActionTypeDisplay` trong `MemberPointsPage.razor`).
- **Nhãn trạng thái "còn/hết hiệu lực theo ngày"** (Coupon/Voucher/Offer/Price và mọi entity có
  `StartingDate`/`EndingDate` tương tự) — dùng ĐÚNG 2 chữ **"Hiệu lực"** (đang còn hiệu lực) /
  **"Hết hiệu lực"** (đã hết hiệu lực) cho cả dropdown lọc lẫn badge bảng và cột Excel export.
  **KHÔNG** tự đặt biến thể khác ("Còn hiệu lực", "Có hiệu lực", "Đang hiệu lực"...) — chuẩn theo
  `VoucherListItemDto` (`VouchersPage.razor` → `EffectDisplay(bool)`). Nếu status gốc đến từ 1 SP
  legacy trả sẵn chuỗi tiếng Việt khác chữ (vd `OffersPage.razor` — SP `GetPromotionOfferHeaderList`
  trả "Có hiệu lực"/"Hết hiệu lực") — **KHÔNG sửa SP**, suy ra `bool isActive` từ chuỗi gốc rồi map
  qua helper `EffectDisplay(bool)` cục bộ để hiển thị đúng 2 chữ chuẩn.

## 5. Sidebar / AppBar — navy đậm, 3 cấp phân biệt icon

- `DrawerBackground = "#0D1B2A"` (navy đậm, mockup `--navy`). `DrawerText`/`DrawerIcon` =
  `rgba(255,255,255,0.6)`.
- `AppbarBackground = "#FFFFFF"` (topbar vẫn sáng) — `MudAppBar Color="Color.Default"`, thêm
  shadow riêng `0 1px 4px rgba(0,0,0,.04)` (CSS `.mud-appbar`) khác Elevation=1 dùng chung filter
  panel (flat).
- Active nav item: nền **đặc** `var(--pos-primary)`, chữ/icon trắng, `border-radius:
  var(--pos-radius-sm)` (8px — **không phải** `-lg` 12px). Hover (không active): nền
  `var(--pos-drawer-hover)` (`#1E3448`).
- **3 cấp sidebar theo cấu trúc mockup** (mockup chỉ có 2 cấp: `.nav-section-label` không icon +
  `.nav-item` có icon — ánh xạ sang 3 cấp của POS.Web):
  - **L1** (Cửa hàng/Danh mục/Khuyến mãi/Vận hành/Quản trị): **CHỮ IN HOA**, `font-weight:700`,
    `font-size:0.625rem` (10px), `letter-spacing:1px`, màu faint (`rgba(255,255,255,0.4)`),
    **KHÔNG icon**.
  - **L2** (Vận hành/Giao dịch/Báo cáo/Tổ chức/Giám sát...): **có icon Material riêng** cho từng
    nhóm (`Icons.Material.Outlined.Schedule`, `.ReceiptLong`, `.Assessment`, `.Groups`,
    `.PointOfSale`, `.Inventory2`, `.Sell`, `.Campaign`, `.ConfirmationNumber`, `.MonitorHeart`,
    `.Article`, `.Tune`...), `font-size:0.8125rem` (13px — lệch có chủ đích so với mockup gốc
    `12.5px` vì đọc khó trên sidebar navy), sáng nhất trong các mục chưa active.
  - **L3** (leaf link): icon `ChevronRight` đồng nhất cho mọi mục, mờ hơn L2.
  - Nhóm "Quản trị" cấu trúc phẳng (không có L2, leaf nằm trực tiếp dưới L1) → 6 leaf giữ icon
    riêng có ý nghĩa (People/Security/Settings/History/Storage/Lock) vì chúng nằm ở độ sâu tương
    đương L2, `font-size:0.8125rem` giống L2.
  - **Cấu trúc DOM hiện tại**: KHÔNG còn domain nào trong sidebar giữ `MudNavGroup` bọc L1 — mọi
    L1 dùng `<div class="pos-nav-section-label">` (nhãn tĩnh, không click), L2 là `MudNavGroup`
    con trực tiếp của `MudNavMenu` (class `pos-nav-l2`), leaf phẳng của "Quản trị" cũng là
    `MudNavLink` con trực tiếp của `MudNavMenu`. Lý do đổi + lịch sử đầy đủ:
    `docs/web/theme/theme-decision-log.md`.
- **Icon set giữ `Icons.Material.Outlined.*`** — mockup dùng emoji nhưng dự án đã quyết định
  KHÔNG dùng emoji cho toàn bộ nav (đã thử 1 lần và rollback — xem `theme-decision-log.md`).
- ⚠️ **Bug đã gặp và fix**: tham số `Icon=` của `MudNavLink`/`MudNavGroup`/`MudIcon` nhận **SVG
  path** (`<path d="...">`), KHÔNG phải text/ligature. Truyền emoji vào đó khiến path vô hiệu,
  icon **biến mất hoàn toàn** (không lỗi, không cảnh báo). Nếu cần hiển thị icon dạng text/emoji,
  phải nhúng trực tiếp trong `ChildContent` (vd `<span>📊</span>Label`), không qua `Icon=`.
- `pos-sidebar-brand` (đầu `MudDrawer`): text-only 2 dòng (tên app + subtitle), KHÔNG
  icon/avatar — khớp `.logo` mockup.
- `pos-sidebar-footer` (cuối `MudDrawer`, `margin-top:auto`): avatar chữ cái đầu (tròn, nền
  Primary) + tên + role + nút logout. `AppBar` chỉ còn menu-toggle + title + spacer.
- Icon nav sidebar: `1.125rem` (18px) — khớp `.nav-icon{width:18px}` mockup.
- Indent 3 cấp thu gọn: `12px`/`20px`/`28px` (L1/L2/L3) thay mặc định MudBlazor `16/36/48px`.
- **MudBlazor 9.5.0 class thật cho nav** (đã verify từ `MudBlazor.min.css` thật trong NuGet cache):
  ```
  .mud-navmenu                        ← MudNavMenu (KHÔNG phải .mud-nav-menu)
    .mud-nav-group                    ← L1 hoặc L2 NavGroup
      .mud-nav-link                   ← title chính là link này (KHÔNG có class -title riêng)
      .mud-collapse-container
        .mud-nav-link                  ← leaf link
  ```

## 6. Page header — Filled cho CTA

- `.pos-page-header-title`: `font-size:1.25rem`, icon cạnh title `Size="Size.Small"`.
- Nút CTA trong header (`pos-page-header-btn`): `Size="Size.Small"` + `Variant="Variant.Filled"
  Color="Color.Primary"`.
- Font-weight title: `Style="font-weight:400"` cục bộ trên từng `MudText` title.

## 7. Filter panel — trắng + border (không còn soft-tint)

- `MudPaper` chứa filter fields: class `pos-filter-panel` = nền **trắng** (`var(--pos-surface)`) +
  `border: 1px solid var(--pos-border)` — khớp mockup `.filter-bar` (nền trắng, border 1px,
  không tint).
- Dùng: `<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">` (Elevation=1 = flat, khớp
  mục 1).

## 8. Hệ màu (Palette)

| Property | Giá trị | Nguồn mockup |
|---|---|---|
| Primary | `#2660A4` (Darken `#1E50A0`, Lighten `#3D8FD9`) | `--steel` |
| Secondary | `#4A6070` | `--gray5` |
| Tertiary | `#6040A8` | `--purple` |
| Success / Error / Warning / Info | `#1F7A4A` / `#B52B27` / `#D4860A` / `#3D8FD9` | `--green`/`--red`/`--gold`/`--sky` |
| Background / Surface | `#F0F4F8` / `#FFFFFF` | `--gray1` |
| DrawerBackground | `#0D1B2A` | `--navy` |

- Font-family: `Typography.Default.FontFamily` = `["Segoe UI", "system-ui", "sans-serif"]`.
- Warning 2 cấp: mockup có `--gold` (nhẹ) và `--orange` (mạnh hơn) nhưng MudBlazor chỉ có 1 slot
  `Warning`. Dùng `--gold` cho slot chính thức; `--orange` chỉ tồn tại dưới dạng CSS token
  `--pos-warning-strong`/`--pos-warning-strong-bg`, không phải Palette slot.

## 9. Màu trend/delta (%) — BẮT BUỘC giữ ngữ nghĩa (không đổi)

- `.pos-delta-up`/`.pos-delta-down` (app.css) — pill badge nền nhạt (`--pos-success-bg`/
  `--pos-danger-bg`), chữ đậm màu `--pos-success`/`--pos-danger`.
- **Quyết định giữ nguyên qua mọi bản**: dashboard vận hành POS giữ ngữ nghĩa tăng=xanh/giảm=đỏ —
  KHÔNG dùng màu trang trí tùy ý theo từng KPI.

## 11. Typography — chuẩn pixel-perfect

**Theme (`PosTheme.cs`, áp dụng toàn app):** `Default.LineHeight=1.5`; `Button.FontSize=0.75rem`
(12px, không letter-spacing); `Body1.FontSize=0.78125rem` (12.5px, khớp input/select/textarea).

**CSS toàn cục (`app.css`):**
- Sidebar L1: weight 700, size 10px, letter-spacing 1px. Sidebar L2: size 13px (xem mục 5).
- `.mud-table .mud-table-body .mud-table-cell`: `font-size:0.78125rem` (12.5px).
- `.mud-input-label-inputcontrol`: `font-size:0.6875rem; font-weight:700; letter-spacing:0.5px` —
  **chữ thường** (KHÔNG `text-transform:uppercase`) cho label input trên toàn app.

**Class CSS riêng cho thành phần không có class dùng chung** (do từng page tự chọn `Typo=`):

```css
.pos-kpi-value    { font-size:1.375rem; font-weight:800; line-height:1; letter-spacing:normal; }
.pos-kpi-label    { font-size:0.6875rem; font-weight:600; text-transform:uppercase; letter-spacing:0.6px; }
.pos-card-title   { font-size:0.8125rem; font-weight:700; }
.pos-section-label{ font-size:0.6875rem; font-weight:700; text-transform:uppercase; letter-spacing:0.7px; padding-bottom:6px; border-bottom:1px solid var(--pos-border); margin-bottom:10px; }
/* KPI card icon variant (Ops/Admin — PosDataSetupPage, UsersPage): nhãn+giá trị trái, icon minh họa lớn mờ phải */
.pos-kpi-card-icon{ display:flex; justify-content:space-between; align-items:flex-start; }
.pos-kpi-icon     { font-size:2.5rem; opacity:0.18; }
```

### KPI card — khuôn mẫu chuẩn (golden standard)

- **Wrapper**: luôn `<div class="d-flex flex-wrap gap-3 mb-4"><div style="flex:1 1 Npx">...`
  (KHÔNG `MudGrid`/`MudItem` cho KPI row).
- **Variant A (không icon)**: `MudPaper Elevation="2" Class="pa-4 text-center"` + accent
  `Style="border-left:4px solid var(--mud-palette-{semantic})"` (luôn 4px, luôn token theme khớp
  `Color=` của value — KHÔNG hex cứng) + `MudText Typo.h5 Class="pos-kpi-value"` (value) +
  `MudText Typo.body2 Class="pos-kpi-label"` (label).
- **Variant B (icon minh họa, Ops/Admin)**: `MudPaper Class="pa-4 pos-kpi-card-icon"` bọc `<div>`
  chứa label+value (label trên, value dưới) + `MudIcon Class="pos-kpi-icon"` cạnh bên phải.
- **Delta/trend badge**: dùng chung component `Components/Shared/PosDeltaBadge.razor`
  (`Current`/`Previous`/`Enabled`/`LowerIsBetter`/`AsPercentPoint`) — render `.pos-delta-up`/
  `.pos-delta-down` đã có sẵn trong `app.css`. KHÔNG tự viết `RenderFragment TrendBadge()` cục bộ
  trong từng page.

Dùng bằng cách thêm `Class="pos-kpi-value"` **cạnh** `Typo="Typo.h5"` hiện có trên `MudText`
(giữ nguyên Typo để không đổi hành vi ngữ nghĩa, CSS class chỉ ép lại giá trị hiển thị).

- **Đã rollout đầy đủ cho mọi page có KPI card**: toàn bộ file có KPI/summary card trong
  `Store/Reports`, `Store/Operations`, `Store/Transactions`, `Store/StoreDashboardPage`, `Ops`,
  `Admin/UsersPage`, `Catalog/PosDevices/BankPosPage` đều dùng `.pos-kpi-value`/`.pos-kpi-label`
  đúng chuẩn.
- **CHƯA áp dụng**: `.pos-card-title`/`.pos-section-label` — mới chỉ định nghĩa trong `app.css`,
  chưa rollout vào page nào.

### Checklist Typography — BẮT BUỘC áp dụng ngay khi tạo page/component mới

| Thành phần | Việc cần làm khi tạo mới | Ai lo |
|---|---|---|
| Input/Select/DatePicker text, Button, MudTable (header + body), Sidebar nav | Không cần làm gì — theme/CSS toàn cục đã đúng | Tự động |
| Field label (label của input) | Không cần làm gì — `.mud-input-label-inputcontrol` đã ép bold/11px toàn cục (chữ thường) | Tự động |
| **KPI value** (số lớn trong KPI card) | `<MudText Typo="Typo.h5" Class="pos-kpi-value" ...>` — giữ `Typo=` hiện có, thêm `Class` | **[Tự làm]** |
| **KPI label** (nhãn nhỏ dưới KPI value) | `<MudText Typo="Typo.body2" Class="pos-kpi-label" ...>` | **[Tự làm]** |
| **KPI trend/delta badge** | Dùng `<PosDeltaBadge Current="..." Previous="..." Enabled="..."/>` — KHÔNG viết `RenderFragment TrendBadge()` riêng | **[Tự làm]** |
| **Card title** (tiêu đề trong `MudPaper`/`MudCard`, không phải page header) | Thêm `Class="pos-card-title"` vào `MudText` tiêu đề card | **[Tự làm]** |
| **Section label** (nhãn phân nhóm trong form, có gạch chân) | Thêm `Class="pos-section-label"` vào `MudText` | **[Tự làm]** |
| Page header title | Dùng `div.pos-page-header` + `.pos-page-header-title` sẵn có — KHÔNG dùng 4 class ở trên | Đã có class riêng |

- **KHÔNG** tự bịa font-size/weight/letter-spacing bằng `Style="..."` inline cho 4 thành phần
  `[Tự làm]` — luôn dùng đúng class đã định nghĩa trong `app.css` (mục 11).
- Cần thành phần typography khác chưa có class tương ứng → thêm class mới vào `app.css` cùng
  nhóm `.pos-*` này và cập nhật bảng trên **trong cùng commit** — không tạo file CSS/rule riêng
  cho typography.

## 10. CSS Isolation — khi nào dùng `.razor.css`

- **Mặc định dùng `app.css` (global)** cho mọi style liên quan token thiết kế dùng chung: màu,
  radius, shadow, sidebar, table header, filter panel... Đây là nơi chứa 100% CSS custom hiện tại
  của dự án (`--pos-*` token + mọi override MudBlazor).
- **Chỉ tạo `{Component}.razor.css`** (CSS isolation) khi style thật sự **cục bộ cho 1 component/
  page cụ thể**, không tái dùng ở nơi khác, và **không cần** ghi đè CSS của component con do
  MudBlazor tự render.
- **Khi cần ghi đè style của component con MudBlazor tự render** (`MudNavLink`, `MudNavGroup`,
  `MudTable` internals, `MudTablePager`...) → **ưu tiên `app.css`** với selector toàn cục
  (`.mud-nav-link`, `.mud-table-head .mud-table-cell`...). CSS isolation cần combinator `::deep`
  mới xuyên qua được markup của component con — phức tạp hơn selector toàn cục mà không có lợi ích
  thêm, và tạo thêm 1 nguồn CSS thứ hai cho cùng 1 khu vực (dễ lệch khi sửa sau này).
- **Ví dụ thực tế**: toàn bộ CSS sidebar 3 cấp nằm ở `app.css` — không có `MainLayout.razor.css`,
  dù `MainLayout.razor` là component phức tạp nhất trong app.

## Lịch sử quyết định & trạng thái rollout

> Các đề xuất đã cân nhắc-và-loại-bỏ, tiến độ rollout theo từng chiến dịch, và TODO còn lại:
> **`docs/web/theme/theme-decision-log.md`** — đọc file đó khi cần tra cứu "tại sao lại làm thế
> này" hoặc xem rollout còn thiếu gì. Mục này chỉ giữ rule đang áp dụng.

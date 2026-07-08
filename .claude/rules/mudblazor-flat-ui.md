# MudBlazor Theme Standard — quy tắc áp dụng thực tế (POS.Web)

> File này là bản tóm tắt trỏ về nguồn sự thật thật sự: **`CLAUDE.md` §13 "UI Polish", §14
> "MudBlazor Theme Standard", §15 "Density Standard"** và
> **`.claude/skills/web/ui-polish-standard.md`**. Khi sửa 1 trong 4 nơi này, phải đối chiếu 3 nơi
> còn lại — tránh lệch giữa các tài liệu.
>
> **Bản v3 — cập nhật 2026-07-05**, thay cho bản v2 (2026-07-04, sidebar sáng + borderless +
> Button Outlined mọi nơi — đã lỗi thời, xem lịch sử ở mục "Đã cân nhắc và loại bỏ"). Bản v3
> chuyển hoàn toàn sang phong cách mockup `docs/web/theme/theme_html.html` (do người dùng cung
> cấp): sidebar navy đậm, card có shadow thật, radius 2 cấp, Button Filled cho CTA. Khác v2 (dựa
> theo 1 ảnh mẫu MudBlazor "Mud Mini" tham khảo), v3 bám sát 1 file mockup HTML/CSS cụ thể — mọi
> giá trị màu/radius/shadow đều đối chiếu trực tiếp CSS gốc của file đó, không suy đoán.

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
| `.kpi-value` | `<MudText Typo="Typo.h5">` trong `<MudPaper Elevation="2">` | xem CLAUDE.md §15 "KPI card row" |
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
- **Font-size input**: `Typography.Body1` trong `PosTheme.cs` = `0.78125rem` (12.5px, cập nhật
  2026-07-06 từ `0.75rem`/12px — khớp chính xác mockup `.input,.select,.textarea{font-size:12.5px}`)
  + `FontWeight "400"`. Xem mục 11 cho toàn bộ đợt rà soát Typography pixel-perfect.

## 3. Button — Filled cho CTA, Outlined cho phần còn lại

> Cập nhật 2026-07-05: **đảo ngược** chuẩn v2 (Outlined mọi nơi). Theo mockup, `.btn-primary` là
> nền đặc màu (Filled), không phải viền trong suốt.

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
- **Bẫy confirm dialog** (không đổi qua các bản): `DialogService.ShowAsync<MudMessageBox>(...)`
  render nút Yes bằng markup mặc định, không sửa được — **KHÔNG** dùng cách gọi này. Luôn khai báo
  `<MudMessageBox @ref="_confirmBox">` trực tiếp + `<YesButton>` tường minh, chọn Variant/Color
  theo bản chất hành động Yes (bảng trên). Dialog dùng chung nhiều hành động (khóa/mở khóa) →
  ternary inline dựa trên field/biến message có sẵn, không thêm field mới — vd
  `ProductLockPage.razor`, `UsersPage.razor` (`_confirmYesColor`). Pattern đầy đủ:
  `.claude/skills/web/SKILLS.md` §"MudMessageBox @ref".

## 4. Bảng dữ liệu (MudTable)

- Luôn dùng `<MudTable>`, `Dense="true"` **bắt buộc** (Density Standard §15), `Hover="true"`,
  `Striped="true"`, `HorizontalScrollbar="true"`.
- Header bảng (`.mud-table .mud-table-head .mud-table-cell` và `.pos-table thead`): nền
  `var(--pos-bg-alt)`, chữ `var(--pos-text-muted)` (muted, không phải heading đậm), **in hoa**
  (`text-transform:uppercase`), `font-size: 0.6875rem`, `letter-spacing:0.4px`, border-bottom 2px
  — khớp `th` mockup. Đây là thay đổi trực quan rõ nhất của v3 trên mọi `MudTable`.
- `.rpt-pivot-table` (pivot report) **giữ nguyên** viền cyan cũ — ngoài phạm vi, thiết kế riêng.
- `MudTablePager` `PageSizeOptions` luôn bắt đầu bằng `10`.

## 5. Sidebar / AppBar — navy đậm, 3 cấp phân biệt icon

- `DrawerBackground = "#0D1B2A"` (navy đậm, mockup `--navy`) — **đảo ngược v2** (sidebar sáng).
  `DrawerText`/`DrawerIcon` = `rgba(255,255,255,0.6)`.
- `AppbarBackground = "#FFFFFF"` (topbar vẫn sáng) — `MudAppBar Color="Color.Default"`, thêm
  shadow riêng `0 1px 4px rgba(0,0,0,.04)` (CSS `.mud-appbar`) khác Elevation=1 dùng chung filter
  panel (flat).
- Active nav item: nền **đặc** `var(--pos-primary)`, chữ/icon trắng, `border-radius:
  var(--pos-radius-sm)` (8px — **không phải** `-lg` 12px, đây là bug đã phát hiện và sửa: mockup
  `.nav-item` dùng `--radius` 8px, không phải `--radius-lg`). Hover (không active): nền
  `var(--pos-drawer-hover)` (`#1E3448`).
- **3 cấp sidebar theo cấu trúc mockup** (mockup chỉ có 2 cấp: `.nav-section-label` không icon +
  `.nav-item` có icon — ánh xạ sang 3 cấp của POS.Web):
  - **L1** (Cửa hàng/Danh mục/Khuyến mãi/Vận hành/Quản trị): **CHỮ IN HOA**, `font-weight:700`,
    `font-size:0.625rem` (10px), `letter-spacing:1px` (cập nhật 2026-07-06, khớp chính xác mockup
    `.nav-section-label{font-size:10px;font-weight:700;letter-spacing:1px}` — trước đó weight sai
    400 thay vì 700), màu faint (`rgba(255,255,255,0.4)`), **KHÔNG icon**.
  - **L2** (Vận hành/Giao dịch/Báo cáo/Tổ chức/Giám sát...): **có icon Material riêng** cho từng
    nhóm (`Icons.Material.Outlined.Schedule`, `.ReceiptLong`, `.Assessment`, `.Groups`,
    `.PointOfSale`, `.Inventory2`, `.Sell`, `.Campaign`, `.ConfirmationNumber`, `.MonitorHeart`,
    `.Article`, `.Tune`...), `font-size:0.8125rem` (13px, cập nhật 2026-07-06 theo yêu cầu người
    dùng — tăng lại từ `0.78125rem`/12.5px vì đọc khó trên sidebar navy; **lệch có chủ đích** so
    với mockup `.nav-item{font-size:12.5px}`, không còn pixel-perfect ở riêng điểm này), sáng nhất
    trong các mục chưa active — giống `.nav-item` mockup.
  - **L3** (leaf link): icon `ChevronRight` đồng nhất cho mọi mục, mờ hơn L2.
  - Nhóm "Quản trị" cấu trúc phẳng (không có L2, leaf nằm trực tiếp dưới L1) → 6 leaf giữ icon
    riêng có ý nghĩa (People/Security/Settings/History/Storage/Lock) vì chúng nằm ở độ sâu tương
    đương L2.

  > **Cập nhật 2026-07-06 — L1 hết là `MudNavGroup`, trừ Quản trị.** Theo ảnh mẫu
  > `docs/web/images/menu_sidebar.jpg`, user yêu cầu L2 **luôn hiển thị** dưới L1, không cần
  > click. `MudNavGroup` (MudBlazor 9.5.0, đã tra XML doc) **không có** tham số khóa "luôn mở,
  > không phản hồi click" (`Expandable`/`ReadOnly` không tồn tại; `Disabled` chặn cả style/L2 bên
  > trong nên không phù hợp). Giải pháp: bỏ hẳn `MudNavGroup` bọc L1 cho 4 domain (CỬA HÀNG/
  > DANH MỤC/KHUYẾN MÃI/VẬN HÀNH), thay bằng `<div class="pos-nav-section-label">` (nhãn tĩnh,
  > không click) + đưa các `MudNavGroup` L2 lên làm con trực tiếp của `MudNavMenu` (gắn thêm
  > `Class="pos-nav-l2"` để CSS phân biệt với Quản trị — cũng là `.mud-nav-group` top-level nhưng
  > không có class này). **QUẢN TRỊ giữ nguyên 100%** cấu trúc `MudNavGroup` bọc L1→leaf cũ — vì
  > vậy nó vẫn là `.mud-nav-group` top-level DUY NHẤT không có `.pos-nav-l2`, nên rule CSS L1 cũ
  > (`.mud-navmenu > .mud-nav-group > .mud-nav-link`) **không cần đổi selector**, tự động chỉ còn
  > áp dụng cho Quản trị. Indent dịch lên 1 bậc do bớt 1 tầng lồng: L2 (nay top-level) `20px→12px`,
  > L3 `28px→20px` (Quản trị/L1 giữ nguyên `12px`). Field `@code` aggregate không còn dùng
  > (`_expandStore`/`_expandCatalog`/`_expandPromotion`/`_expandOps`) đã xóa khỏi
  > `MainLayout.razor` — các field lẻ theo từng L2 (`_expandStoreOps`...) và `_expandAdmin` giữ
  > nguyên, bind trực tiếp vào `MudNavGroup` L2/Quản trị như cũ.
- **Icon set giữ `Icons.Material.Outlined.*`** — mockup dùng emoji nhưng đã quyết định KHÔNG dùng
  emoji cho toàn bộ nav (dù đã thử 1 lần và rollback — xem mục "Đã cân nhắc và loại bỏ").
- ⚠️ **Bug đã gặp và fix**: tham số `Icon=` của `MudNavLink`/`MudNavGroup`/`MudIcon` nhận **SVG
  path** (`<path d="...">`), KHÔNG phải text/ligature. Truyền emoji vào đó khiến path vô hiệu,
  icon **biến mất hoàn toàn** (không lỗi, không cảnh báo — rất khó phát hiện qua code review).
  Nếu cần hiển thị icon dạng text/emoji, phải nhúng trực tiếp trong `ChildContent` (vd
  `<span>📊</span>Label`), không qua `Icon=`. Ở v3, quyết định cuối là KHÔNG dùng emoji nên vấn đề
  này không còn áp dụng, nhưng vẫn là bẫy kỹ thuật cần nhớ nếu sau này cân nhắc lại.
- `pos-sidebar-brand` (đầu `MudDrawer`): text-only 2 dòng (tên app + subtitle), KHÔNG
  icon/avatar — khớp `.logo` mockup. **Khác v2** (v2 dùng `MudAvatar` tròn + tên).
- `pos-sidebar-footer` (cuối `MudDrawer`, `margin-top:auto`): avatar chữ cái đầu (tròn, nền
  Primary) + tên + role + nút logout — **user-info đã dời từ `MudAppBar` xuống đây** (v2 để ở
  AppBar). `AppBar` giờ chỉ còn menu-toggle + title + spacer.
- Icon nav sidebar: `1.25rem` → `1.125rem` (18px) — khớp `.nav-icon{width:18px}` mockup.
- Indent 3 cấp thu gọn: `12px`/`20px`/`28px` (L1/L2/L3) thay mặc định MudBlazor `16/36/48px` —
  khít hơn nhiều, đúng tinh thần mockup `.nav-section{padding:12px}`.
- **MudBlazor 9.5.0 class thật cho nav** (đã verify từ `MudBlazor.min.css` thật trong NuGet cache,
  KHÔNG đoán — bug ban đầu dùng sai tên class `.mud-nav-menu`/`.mud-nav-group-title` khiến CSS
  không áp dụng được gì):
  ```
  .mud-navmenu                        ← MudNavMenu (KHÔNG phải .mud-nav-menu)
    .mud-nav-group                    ← L1 NavGroup
      .mud-nav-link                   ← L1 title chính là link này (KHÔNG có class -title riêng)
      .mud-collapse-container
        .mud-nav-group                ← L2 NavGroup
          .mud-nav-link                ← L2 title
          .mud-collapse-container
            .mud-nav-link              ← L3 leaf link
  ```

## 6. Page header — Filled cho CTA

- `.pos-page-header-title`: `font-size:1.25rem`, icon cạnh title `Size="Size.Small"` — không đổi
  qua các bản.
- Nút CTA trong header (`pos-page-header-btn`): `Size="Size.Small"` + **`Variant="Variant.Filled"
  Color="Color.Primary"`** — đảo ngược v2 (Outlined), theo mục 3.
- Font-weight title: `Style="font-weight:400"` cục bộ trên từng `MudText` title — không đổi.

## 7. Filter panel — trắng + border (không còn soft-tint)

- `MudPaper` chứa filter fields: class `pos-filter-panel` = nền **trắng** (`var(--pos-surface)`) +
  `border: 1px solid var(--pos-border)` — **đảo ngược v2** (soft-tint `var(--pos-primary-bg)`),
  theo mockup `.filter-bar` (nền trắng, border 1px, không tint).
- Dùng: `<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">` (Elevation=1 = flat, khớp
  mục 1).

## 8. Hệ màu (Palette) — đổi toàn bộ theo mockup

> Khác v2 (giữ nguyên hex thương hiệu), v3 đổi **toàn bộ** Primary/Secondary/Tertiary/Success/
> Error/Warning/Info theo đúng mockup — quyết định có chủ đích vì yêu cầu gốc là "CustomTheme
> khớp 100% mockup", không chỉ đổi cách dùng màu cũ.

| Property | Giá trị mới | Nguồn mockup |
|---|---|---|
| Primary | `#2660A4` (Darken `#1E50A0`, Lighten `#3D8FD9`) | `--steel` |
| Secondary | `#4A6070` | `--gray5` |
| Tertiary | `#6040A8` (slot cũ `#1EAA90` không dùng ở đâu, tái sử dụng an toàn) | `--purple` |
| Success / Error / Warning / Info | `#1F7A4A` / `#B52B27` / `#D4860A` / `#3D8FD9` | `--green`/`--red`/`--gold`/`--sky` |
| Background / Surface | `#F0F4F8` / `#FFFFFF` | `--gray1` |
| DrawerBackground | `#0D1B2A` | `--navy` |

- Font-family: `Typography.Default.FontFamily` đổi sang `["Segoe UI", "system-ui", "sans-serif"]`
  đúng theo mockup (yêu cầu bổ sung riêng, không nằm trong quyết định "giữ nguyên Typography" ban
  đầu của Bước 2).
- Warning 2 cấp: mockup có `--gold` (nhẹ) và `--orange` (mạnh hơn) nhưng MudBlazor chỉ có 1 slot
  `Warning`. Dùng `--gold` cho slot chính thức; `--orange` chỉ tồn tại dưới dạng CSS token
  `--pos-warning-strong`/`--pos-warning-strong-bg`, không phải Palette slot.
- Token đổi tên: `--pos-teal`/`--pos-teal-bg` → `--pos-purple`/`--pos-purple-bg` (không dùng ở
  đâu ngoài khai báo, an toàn đổi tên).

## 9. Màu trend/delta (%) — BẮT BUỘC giữ ngữ nghĩa (không đổi)

- `.pos-delta-up`/`.pos-delta-down` (app.css) — pill badge nền nhạt (`--pos-success-bg`/
  `--pos-danger-bg`), chữ đậm màu `--pos-success`/`--pos-danger`. Giá trị hex bên dưới đổi theo
  mục 8 nhưng ngữ nghĩa và cơ chế giữ nguyên.
- **Quyết định giữ nguyên qua mọi bản**: dashboard vận hành POS giữ ngữ nghĩa tăng=xanh/giảm=đỏ —
  KHÔNG dùng màu trang trí tùy ý theo từng KPI.

## 11. Typography — rà soát pixel-perfect (2026-07-06)

> Đợt rà soát riêng, sau v3: v3 mới chỉ khớp `font-family` + bảng màu; font-size/weight/
> letter-spacing/line-height của từng thành phần chưa đối chiếu lại với `theme_html.html`.
> Đã audit toàn bộ CSS mockup (single inline `<style>`, không Google Fonts/local font — chỉ
> `'Segoe UI', system-ui, sans-serif`) và sửa theo phạm vi **chỉ Theme + CSS toàn cục** (đã chốt
> với user — không rollout Typo attribute qua 82 file dùng `Typo.h5/h6/body2/caption`).

**Sửa trong `PosTheme.cs` (Typography, áp dụng toàn app):**

| Variant | Trước | Sau | Khớp mockup |
|---|---|---|---|
| `Default.LineHeight` | `1.45` | `1.5` | `body{line-height:1.5}` |
| `Button.FontSize` | *(không set → ~14px mặc định)* | `0.75rem` (12px) | `.btn{font-size:12px}` |
| `Button.LetterSpacing` | `0.03em` | *(đã xóa)* | mockup không có letter-spacing cho `.btn` |
| `Body1.FontSize` | `0.75rem` (12px) | `0.78125rem` (12.5px) | `.input,.select,.textarea{font-size:12.5px}` |

**Sửa trong `app.css` (selector toàn cục có sẵn):**

- Sidebar L1 (`.mud-drawer .mud-navmenu > .mud-nav-group > .mud-nav-link`): weight 400→700,
  size 11px→10px, letter-spacing 0.8px→1px (xem mục 5).
- Sidebar L2 (nav-item lồng): size 13px→12.5px (xem mục 5) — **rollback 2026-07-06**: tăng lại
  12.5px→13px theo yêu cầu người dùng, xem mục 5.
- `.mud-table .mud-table-body .mud-table-cell`: **mới thêm** `font-size:0.78125rem` (12.5px) —
  trước đó chỉ có override cho `.mud-table-head`, body cell dùng size mặc định MudBlazor không
  khớp mockup `table{font-size:12.5px}`.
- `.mud-input-label-inputcontrol`: **mới thêm** `font-size:0.6875rem;font-weight:700;
  text-transform:uppercase;letter-spacing:0.5px` — khớp mockup `.field label`. Trước đó field
  label dùng mặc định MudBlazor (`font-size:1rem;font-weight:400`, không uppercase) vì
  `Typography` theme không cascade vào label input (đã verify class thật qua `MudBlazor.min.css`).
- Xóa override `--mud-typography-default-lineheight:1.5` riêng cho mobile (media query) — nay
  `Default.LineHeight` đã là `1.5` sẵn cho mọi breakpoint, override cũ thành dead code.

**Class CSS mới cho thành phần KHÔNG có class dùng chung** (KPI value/label, card title, section
label — do từng page tự chọn `Typo=` nên sửa theme global sẽ ảnh hưởng chỗ khác không liên quan):

```css
.pos-kpi-value    { font-size:1.375rem; font-weight:800; line-height:1; letter-spacing:normal; }
.pos-kpi-label    { font-size:0.6875rem; font-weight:600; text-transform:uppercase; letter-spacing:0.6px; }
.pos-card-title   { font-size:0.8125rem; font-weight:700; }
.pos-section-label{ font-size:0.6875rem; font-weight:700; text-transform:uppercase; letter-spacing:0.7px; padding-bottom:6px; border-bottom:1px solid var(--pos-border); margin-bottom:10px; }
/* KPI card icon variant (Ops/Admin — PosDataSetupPage, UsersPage): nhãn+giá trị trái, icon minh họa lớn mờ phải */
.pos-kpi-card-icon{ display:flex; justify-content:space-between; align-items:flex-start; }
.pos-kpi-icon     { font-size:2.5rem; opacity:0.18; }
```

### KPI card — khuôn mẫu chuẩn (golden standard, chốt 2026-07-08)

- **Wrapper**: luôn `<div class="d-flex flex-wrap gap-3 mb-4"><div style="flex:1 1 Npx">...`
  (KHÔNG `MudGrid`/`MudItem` cho KPI row — xem mục "KPI card row" ở Density Standard).
- **Variant A (không icon)**: `MudPaper Elevation="2" Class="pa-4 text-center"` + accent
  `Style="border-left:4px solid var(--mud-palette-{semantic})"` (luôn 4px, luôn token theme khớp
  `Color=` của value — KHÔNG hex cứng) + `MudText Typo.h5 Class="pos-kpi-value"` (value) +
  `MudText Typo.body2 Class="pos-kpi-label"` (label).
- **Variant B (icon minh họa, Ops/Admin)**: `MudPaper Class="pa-4 pos-kpi-card-icon"` bọc `<div>`
  chứa label+value (label trên, value dưới) + `MudIcon Class="pos-kpi-icon"` cạnh bên phải.
- **Delta/trend badge**: dùng chung component `Components/Shared/PosDeltaBadge.razor`
  (`Current`/`Previous`/`Enabled`/`LowerIsBetter`/`AsPercentPoint`) — render `.pos-delta-up`/
  `.pos-delta-down` đã có sẵn trong `app.css`. KHÔNG tự viết `RenderFragment TrendBadge()` cục bộ
  trong từng page (đã gộp 3 bản trùng lặp — xem "Trạng thái rollout").

Dùng bằng cách thêm `Class="pos-kpi-value"` **cạnh** `Typo="Typo.h5"` hiện có trên `MudText`
(giữ nguyên Typo để không đổi hành vi ngữ nghĩa, CSS class chỉ ép lại giá trị hiển thị).

- **Đã rollout đầy đủ cho mọi page có KPI card** (cập nhật 2026-07-08, chiến dịch chuẩn hóa KPI
  card — xem "Trạng thái rollout" bên dưới): toàn bộ 15 file có KPI/summary card trong
  `Store/Reports`, `Store/Operations`, `Store/Transactions`, `Store/StoreDashboardPage`, `Ops`,
  `Admin/UsersPage`, `Catalog/PosDevices/BankPosPage` đều dùng `.pos-kpi-value`/`.pos-kpi-label`
  đúng chuẩn: value `Typo.h5` + `Class="pos-kpi-value"`, label `Typo.body2` + `Class="pos-kpi-label"`.
- **CHƯA áp dụng**: `.pos-card-title`/`.pos-section-label` — mới chỉ định nghĩa trong `app.css`,
  chưa áp dụng vào page nào (chưa xác định page nào đang dùng "card title" theo đúng nghĩa mockup —
  cần rà soát riêng nếu muốn rollout).

### 11.1 Checklist Typography — BẮT BUỘC áp dụng ngay khi tạo page/component mới

> Mục đích: page mới sinh ra đã đúng chuẩn ngay từ đầu, không cần đợt polish riêng về sau.
> `PosTheme.cs` + `app.css` đã lo phần nền tảng (line-height, input, table, sidebar, field label,
> button) — page mới **không cần làm gì thêm** cho các mục đó. Chỉ cần tự tay áp đúng class ở các
> mục có `[Tự làm]` bên dưới vì chúng do page tự chọn `Typo=`, theme không thể tự suy ra ngữ nghĩa.

| Thành phần | Việc cần làm khi tạo mới | Ai lo |
|---|---|---|
| Input/Select/DatePicker text, Button, MudTable (header + body), Sidebar nav | Không cần làm gì — theme/CSS toàn cục đã đúng | Tự động |
| Field label (label của input) | Không cần làm gì — `.mud-input-label-inputcontrol` đã ép uppercase/bold/11px toàn cục | Tự động |
| **KPI value** (số lớn trong KPI card) | `<MudText Typo="Typo.h5" Class="pos-kpi-value" ...>` — giữ `Typo=` hiện có, thêm `Class` | **[Tự làm]** |
| **KPI label** (nhãn nhỏ dưới KPI value) | `<MudText Typo="Typo.body2" Class="pos-kpi-label" ...>` | **[Tự làm]** |
| **KPI trend/delta badge** | Dùng `<PosDeltaBadge Current="..." Previous="..." Enabled="..."/>` — KHÔNG viết `RenderFragment TrendBadge()` riêng trong page | **[Tự làm]** |
| **Card title** (tiêu đề trong `MudPaper`/`MudCard`, không phải page header) | Thêm `Class="pos-card-title"` vào `MudText` tiêu đề card | **[Tự làm]** |
| **Section label** (nhãn phân nhóm trong form, có gạch chân) | Thêm `Class="pos-section-label"` vào `MudText` | **[Tự làm]** |
| Page header title | Dùng `div.pos-page-header` + `.pos-page-header-title` sẵn có — KHÔNG dùng 4 class ở trên (page header là style riêng, không thuộc mockup `.card-title`/`.kpi-value`) | Đã có class riêng |

- **KHÔNG** tự bịa font-size/weight/letter-spacing bằng `Style="..."` inline cho 4 thành phần
  `[Tự làm]` — luôn dùng đúng class đã định nghĩa trong `app.css` (mục 11) để đảm bảo mọi page
  hiển thị đồng nhất, tránh mỗi page một số liệu khác nhau.
- Cần thành phần typography khác chưa có class tương ứng (vd biến thể mới của mockup) → thêm class
  mới vào `app.css` cùng nhóm `.pos-*` này và cập nhật bảng trên **trong cùng commit** — không tạo
  file CSS/rule riêng cho typography (tránh lệch với mục 11 đã có).

## Đã cân nhắc và loại bỏ (giữ lại lịch sử quyết định)

| Đề xuất | Lý do loại bỏ |
|---|---|
| Theme Ynex (rebrand Indigo, navbar đầy icon, dark mode, dashboard Ecommerce mới) | Không phải MudBlazor gốc, rebrand toàn app rủi ro cao (quyết định thời v1) |
| `Outlined="true"` cho MudPaper/MudCard (v2) | Thay bằng Elevation=none (borderless) — nay v3 lại đổi tiếp sang Elevation có shadow thật |
| Đổi hex Primary/Success/Error/Warning/Info (v2) | v2 giữ nguyên thương hiệu; **v3 đã đổi** theo mockup theo yêu cầu "CustomTheme khớp 100%" — quyết định v2 không còn áp dụng |
| `Dense="false"` cho MudTable | Vi phạm Density Standard §15 (bắt buộc `Dense="true"`) — không đổi qua các bản |
| Sidebar sáng + `MudAvatar` brand (v2) | v3 đảo ngược hoàn toàn sang navy đậm + brand text-only theo mockup mới |
| Emoji làm icon sidebar (thử ở v3, đã rollback) | Kỹ thuật thất bại: `Icon=` của MudNavLink/MudIcon nhận SVG path, không phải text — emoji khiến icon biến mất hoàn toàn. Đã rollback về Material Icons Outlined; sau đó người dùng yêu cầu thêm cấu trúc 3 cấp phân biệt icon (L1 không icon/IN HOA, L2 có icon riêng, L3 chevron đồng nhất) |
| Filter panel soft-tint `--pos-primary-bg` (v2) | v3 đổi sang trắng + border theo đúng mockup `.filter-bar` |
| Button Outlined mọi nơi (v2) | v3 đảo ngược: Filled cho CTA/hành động tích cực, Outlined cho phần còn lại — theo `.btn-primary` mockup |

## Trạng thái rollout (cập nhật 2026-07-05, v3 — toàn bộ theme + 5 cụm menu)

- **Theme/Layout/Sidebar** (Bước 2-3.7): `PosTheme.cs` (Palette/Radius/Shadow/FontFamily),
  `MainLayout.razor` (sidebar navy, appbar, sidebar-footer, 3 cấp icon), `app.css` (token, sidebar
  3-tầng, table header, filter panel, radius control) — hoàn tất, build + test xanh.
- **Button convention** (Bước 4): rollout đủ **5 cụm menu — Danh mục (14 file), Cửa hàng (17
  file), Khuyến mãi (11 file), Vận hành (12 file), Quản trị (5 file)** = 59 file, build + test
  xanh sau mỗi cụm. Đã rà kỹ 12 dialog dùng `MudMessageBox @ref` (bao gồm 2 dialog dùng chung
  nhiều hành động — `ProductLockPage`, `UsersPage` — dùng ternary inline theo bản chất hành động).
- **Không đổi trong đợt này**: `Login.razor` (không thuộc menu nào, cố ý không đổi từ đợt v2),
  `EosDayShiftListDialog.razor` (orphaned — không còn page nào mở, xác nhận lại qua grep vẫn
  đúng — vẫn áp Button convention mới cho nhất quán dù orphaned).
- **Icon set**: vẫn CHỈ `Icons.Material.Outlined.*` cho `MainLayout.razor` (sidebar). Icon trong
  nội dung từng page (page-header icon, button StartIcon) vẫn giữ `Icons.Material.Filled.*` như
  trước — quyết định có chủ đích từ v2, chưa thay đổi ở v3.
- **Typography audit** (2026-07-06, xem mục 11): `PosTheme.cs` (line-height/Button/Body1) +
  `app.css` (sidebar label, MudTable body cell, field label) sửa toàn cục — build + test xanh.
- **Chuẩn hóa KPI card** (2026-07-08, chiến dịch riêng — xem mục 11 "KPI card — khuôn mẫu chuẩn"):
  rà soát toàn bộ `src/POS.Web/Components/Pages/**/*.razor`, tìm ra 19 file có KPI card, chuẩn hóa
  15 file lệch chuẩn (3 file đã chuẩn từ trước không cần sửa, 1 file không có KPI card thật ngoài
  giai đoạn rà soát ban đầu):
  - Thêm class `.pos-kpi-value`/`.pos-kpi-label` cho toàn bộ card còn thiếu; chuẩn hóa `Typo` (value
    luôn `h5`, label luôn `body2` — trước đó lẫn lộn `h4/h5/h6` và `body2/caption`).
  - Chuẩn hóa wrapper KPI row về **`d-flex flex-wrap`** (bỏ `MudGrid`/`MudItem` — 6 file trước đó
    dùng MudGrid: `RevenueHourlyPage`, `TopProductPage`, `RevenuePage`, `StoreDashboardPage`,
    `Ops/StorePage`, `Ops/PosMapPage`).
  - Chuẩn hóa accent `border-left` về **4px** + `var(--mud-palette-{semantic})` (bỏ hex cứng như
    `#1976D2`/`#7B1FA2`/`#F57C00`/`#388E3C`/`#2051A3`/`#27AE60`/`#DC3545`, bỏ 3px lẻ tẻ).
  - Thêm 2 class mới `.pos-kpi-card-icon`/`.pos-kpi-icon` (codify lại inline-style đã copy-paste ở
    `PosDataSetupPage.razor`/`UsersPage.razor`) — "Variant B" cho KPI card có icon minh họa.
  - Tạo `Components/Shared/PosDeltaBadge.razor` (đăng ký `@using` toàn cục trong `_Imports.razor`),
    gộp 3 bản `RenderFragment TrendBadge()` trùng lặp gần giống hệt nhau (từng nằm rải rác trong
    `RevenueHourlyPage`, `PaymentBreakdownPage`, `TopProductPage` — hardcode màu inline thay vì dùng
    `.pos-delta-up`/`.pos-delta-down` sẵn có) thành 1 component dùng chung, hỗ trợ cả delta dạng %
    tăng trưởng lẫn delta "điểm %" (`AsPercentPoint`, dùng cho card "Tỷ lệ KHÔNG tiền mặt" trong
    `PaymentBreakdownPage`). Đây là ngoại lệ `@code` DUY NHẤT trong chiến dịch — mọi thay đổi khác
    chỉ ở markup.
  - Build + `dotnet test tests/POS.ContractTests` xanh sau mỗi batch (3 batch: Store/Reports →
    Store/Operations+Transactions+Dashboard → Ops+Admin+Catalog).
  - **Chưa verify bằng mắt** (chạy app thật) — sandbox không có `POS_SECRET_KEY`/DB/Redis nên
    `dotnet run` không khởi động được; chỉ verify qua build + contract test.

## 10. CSS Isolation — khi nào dùng `.razor.css`

> Chốt lại quyết định kỹ thuật đã áp dụng thật khi làm sidebar (Bước 3.5) — trước đây chỉ nằm
> trong plan nội bộ, chưa từng ghi vào rule file.

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
- **Ví dụ thực tế**: toàn bộ CSS sidebar 3 cấp (Bước 3-3.7) nằm ở `app.css` — không có
  `MainLayout.razor.css`, dù `MainLayout.razor` là component phức tạp nhất trong app.

## TODO còn lại (chưa làm — chỉ ghi nhận)

- `Login.razor`, vài page report (`PaymentBreakdownPage.razor`, `TopProductPage.razor`) có
  `border-radius` hardcode khác cho mini progress bar/card phụ — cố ý chưa đổi vì không phải
  hardcode trên component MudBlazor chuẩn.
- Icon set `Outlined` chưa mở rộng ra ngoài `MainLayout.razor` — nếu muốn đồng bộ icon toàn app
  (page-header, button StartIcon...) thì đây là thay đổi diện rộng riêng, cần quyết định rõ ràng
  trước khi làm (ảnh hưởng ~40+ page).
- `Shadows.Elevation[6..25]` chưa đổi tint sang navy mới (`rgba(13,27,42,x)`) — vẫn giữ tint cũ
  `rgba(26,43,69,x)`, rủi ro thấp vì chưa có ví dụ cụ thể từ mockup cho elevation cao.
- `.pos-card-title`/`.pos-section-label` chưa áp dụng vào page nào — cần rà soát riêng nếu muốn
  rollout (khác `.pos-kpi-value`/`.pos-kpi-label` đã rollout đầy đủ 2026-07-08, xem "Trạng thái
  rollout").
- Chuẩn hóa KPI card (2026-07-08) mới verify qua build + contract test, **chưa chạy app thật để
  xem bằng mắt** — cần verify lại khi có môi trường đủ `POS_SECRET_KEY` + DB + Redis.

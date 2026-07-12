# MudBlazor Theme Standard — quy tắc áp dụng thực tế (POS.Web)

> File này là bản tóm tắt trỏ về nguồn sự thật thật sự: **`CLAUDE.md` §13 "UI Polish", §14
> "MudBlazor Theme Standard", §15 "Density Standard"** và
> **`.claude/skills/web/ui-polish-standard.md`**. Khi sửa 1 trong 4 nơi này, phải đối chiếu 3 nơi
> còn lại — tránh lệch giữa các tài liệu.
>
> **Bản v3 — cập nhật 2026-07-05**, thay cho bản v2 (2026-07-04, sidebar sáng + borderless +
> Button Outlined mọi nơi — đã lỗi thời, xem lịch sử ở `docs/web/theme/theme-decision-log.md`). Bản v3
> chuyển hoàn toàn sang phong cách mockup `docs/web/theme/theme_html.html` (do người dùng cung
> cấp): sidebar navy đậm, card có shadow thật, radius 2 cấp, Button Filled cho CTA. Khác v2 (dựa
> theo 1 ảnh mẫu MudBlazor "Mud Mini" tham khảo), v3 bám sát 1 file mockup HTML/CSS cụ thể — mọi
> giá trị màu/radius/shadow đều đối chiếu trực tiếp CSS gốc của file đó, không suy đoán.

> ## ⚡ Tóm tắt luật thép (đọc nhanh)
> **✅ DO:**
> - Trước khi viết markup: tra bảng mapping HTML mockup → MudBlazor Component (mục 0)
> - Card/Paper có nội dung: `Elevation="2"` (shadow thật); filter panel/toolbar: `Elevation="1"`
>   (flat) (mục 1, mục 7)
> - Input/Select/DatePicker: luôn `Variant="Variant.Outlined"` + `Margin="Margin.Dense"` (mục 2)
> - Button CTA (Lưu/Thêm/Tìm): `Filled`+`Primary`; Hủy/trung tính: `Outlined` không màu; Phá hủy:
>   `Outlined`+`Color.Error` — mọi `MudButton` thêm class `pos-btn-mockup`, nút trung tính thêm
>   thêm `pos-btn-secondary-mockup` (mục 3, mục 3a)
> - DataTable: `MudTable Dense Hover Striped HorizontalScrollbar`; badge trạng thái tĩnh dùng
>   `.pos-status-chip` + modifier `.pos-status-{success,error,warning,info}` (mục 4, mục 4a)
> - Sidebar: `DrawerBackground="#0D1B2A"`, 3 cấp L1 (uppercase, không icon)/L2 (icon riêng)/L3
>   (chevron) (mục 5)
> - KPI card: dùng đúng khuôn mẫu chuẩn (`.pos-kpi-value`/`.pos-kpi-label` + `<PosDeltaBadge>`) —
>   xem checklist Typography bắt buộc (mục 11, mục 11.1)
> - Màu Palette theo đúng bảng hex đã chốt (mục 8); màu trend/delta tăng=xanh/giảm=đỏ giữ nguyên
>   qua mọi bản (mục 9)
>
> **❌ DON'T:**
> - Cấm tự thêm `border`/`box-shadow` CSS cho `MudPaper`/`MudCard` — dùng `Elevation` (mục 1)
> - Cấm truyền emoji vào tham số `Icon=` của `MudNavLink`/`MudIcon` (nhận SVG path, icon sẽ biến
>   mất không cảnh báo) (mục 5)
> - Cấm gọi `DialogService.ShowAsync<MudMessageBox>(...)` cho confirm dialog — luôn khai báo
>   `<MudMessageBox @ref>` tường minh (mục 3)
> - Cấm `MudTablePager PageSizeOptions` không chứa `10` (mục 4)
> - Cấm phát minh class/spacing/màu mới khi đã có sẵn trong file này — bổ sung vào đúng mục tương
>   ứng nếu thật sự chưa có, không tạo rule song song khác
>
> *(Chi tiết đầy đủ — bảng màu/CSS/code mẫu/lịch sử quyết định — xem các mục đánh số bên dưới,
> KHÔNG bị đổi.)*

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

### 3a. Class CSS phụ trợ cho MudButton — `pos-btn-mockup` / `pos-btn-secondary-mockup`

> Chốt 2026-07-09 (chiến dịch đồng bộ 95 file theo `MemberPointsPage.razor`). 2 class định nghĩa
> trong `app.css:161-168`:
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

- Luôn dùng `<MudTable>`, `Dense="true"` **bắt buộc** (Density Standard §15), `Hover="true"`,
  `Striped="true"`, `HorizontalScrollbar="true"`.
- Header bảng (`.mud-table .mud-table-head .mud-table-cell` và `.pos-table thead`): nền
  `var(--pos-bg-alt)`, chữ `var(--pos-text-muted)` (muted, không phải heading đậm), **in hoa**
  (`text-transform:uppercase`), `font-size: 0.6875rem`, `letter-spacing:0.4px`, border-bottom 2px
  — khớp `th` mockup. Đây là thay đổi trực quan rõ nhất của v3 trên mọi `MudTable`.
- `.rpt-pivot-table` (pivot report) **giữ nguyên** viền cyan cũ — ngoài phạm vi, thiết kế riêng.
- `MudTablePager` `PageSizeOptions` luôn bắt đầu bằng `10`.

### 4a. Status badge dạng dot-pill — CHUẨN MẶC ĐỊNH (cập nhật 2026-07-09, chiến dịch đồng bộ toàn app)

> Chốt lần đầu 2026-07-09 khi restyle `MemberPointsPage.razor` (giới hạn ở 1 mockup cụ thể), sau đó
> **mở rộng thành chuẩn mặc định toàn app** cùng ngày, trong chiến dịch đồng bộ 95 file
> `Components/Pages/**/*.razor` lấy `MemberPointsPage.razor` làm page mẫu. Quyết định cũ ("MudChip
> vẫn là mặc định, `pos-status-chip` chỉ dùng khi khớp 1 mockup cụ thể") **đã lỗi thời** — xem
> `docs/web/theme/theme-decision-log.md`.

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
  `StartingDate`/`EndingDate` tương tự) — chốt 2026-07-09: dùng ĐÚNG 2 chữ **"Hiệu lực"** (đang còn
  hiệu lực) / **"Hết hiệu lực"** (đã hết hiệu lực) cho cả dropdown lọc lẫn badge bảng và cột Excel
  export. **KHÔNG** tự đặt biến thể khác ("Còn hiệu lực", "Có hiệu lực", "Đang hiệu lực"...) — trước
  đó 3 trang Coupons/Vouchers/Offers từng lệch chữ nhau, đã đồng bộ lại theo `VoucherListItemDto`
  (`VouchersPage.razor` → `EffectDisplay(bool)`) làm tham chiếu chuẩn. Nếu status gốc đến từ 1 SP
  legacy trả sẵn chuỗi tiếng Việt khác chữ (vd `OffersPage.razor` — SP `GetPromotionOfferHeaderList`
  trả "Có hiệu lực"/"Hết hiệu lực") — **KHÔNG sửa SP**, suy ra `bool isActive` từ chuỗi gốc rồi map
  qua helper `EffectDisplay(bool)` cục bộ để hiển thị đúng 2 chữ chuẩn.

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
    đương L2, `font-size:0.8125rem` giống L2 (xem cập nhật 2026-07-09 bên dưới).

  > **Cập nhật 2026-07-06 — L1 hết là `MudNavGroup`, trừ Quản trị (lúc đó).** Theo ảnh mẫu
  > `docs/web/images/menu_sidebar.jpg`, user yêu cầu L2 **luôn hiển thị** dưới L1, không cần
  > click. `MudNavGroup` (MudBlazor 9.5.0, đã tra XML doc) **không có** tham số khóa "luôn mở,
  > không phản hồi click" (`Expandable`/`ReadOnly` không tồn tại; `Disabled` chặn cả style/L2 bên
  > trong nên không phù hợp). Giải pháp: bỏ hẳn `MudNavGroup` bọc L1 cho 4 domain (CỬA HÀNG/
  > DANH MỤC/KHUYẾN MÃI/VẬN HÀNH), thay bằng `<div class="pos-nav-section-label">` (nhãn tĩnh,
  > không click) + đưa các `MudNavGroup` L2 lên làm con trực tiếp của `MudNavMenu` (gắn thêm
  > `Class="pos-nav-l2"` để CSS phân biệt). Lúc này QUẢN TRỊ tạm giữ nguyên cấu trúc `MudNavGroup`
  > bọc L1→leaf cũ (quyết định có chủ đích khi đó) — **đã đổi tiếp ở bản cập nhật 2026-07-09 bên
  > dưới**, không còn đúng nữa.

  > **Cập nhật 2026-07-09 — QUẢN TRỊ cũng bỏ `MudNavGroup`, đồng bộ 100% với 4 domain còn lại.**
  > Lý do: dù CSS khai báo cùng `font-size:0.625rem` cho cả 2 cách (div label vs `MudNavGroup`
  > title), user quan sát bằng mắt trên browser thấy title "QUẢN TRỊ" nhỏ hơn rõ rệt so với "CỬA
  > HÀNG"/"VẬN HÀNH" — nghi do khác biệt DOM rendering thực tế của `MudNavGroup` title so với
  > `<div>` thuần (không xác định được nguyên nhân chính xác chỉ qua đọc CSS tĩnh). Xử lý: đổi
  > title "QUẢN TRỊ" sang `<div class="pos-nav-section-label">` giống 4 domain kia, và đưa 6
  > `MudNavLink` leaf (Users/Roles/Configs/Audit log/SQL Console/Mã hóa Secret) lên làm con trực
  > tiếp của `MudNavMenu` (không còn `MudNavGroup` bọc). Không còn domain nào trong sidebar giữ
  > `MudNavGroup` bọc L1 — mọi `.mud-nav-group` top-level còn lại đều mang class `.pos-nav-l2`.
  > CSS: xóa selector `.mud-navmenu > .mud-nav-group > .mud-nav-link` (không còn phần tử nào khớp),
  > thêm selector `.mud-navmenu > .mud-nav-link` (leaf QUẢN TRỊ, style như L2 — `font-size:
  > 0.8125rem`, có icon ý nghĩa riêng, không phải chevron chung như L3 thường). Field `@code`
  > `_expandAdmin` đã xóa khỏi `MainLayout.razor` (không còn nhóm để expand/collapse). Indent giữ
  > nguyên `12px` cho leaf QUẢN TRỊ (như L2). Các field lẻ theo từng L2 khác (`_expandStoreOps`...)
  > không đổi.
- **Icon set giữ `Icons.Material.Outlined.*`** — mockup dùng emoji nhưng đã quyết định KHÔNG dùng
  emoji cho toàn bộ nav (dù đã thử 1 lần và rollback — xem `docs/web/theme/theme-decision-log.md`).
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
  letter-spacing:0.5px` — khớp mockup `.field label` về cỡ/độ đậm. Trước đó field
  label dùng mặc định MudBlazor (`font-size:1rem;font-weight:400`) vì
  `Typography` theme không cascade vào label input (đã verify class thật qua `MudBlazor.min.css`).
  **Cập nhật 2026-07-10**: đã bỏ `text-transform:uppercase` (từng thêm ở đợt audit này) — chốt lại
  **giữ chữ thường** cho label input trên toàn app, xem `docs/web/theme/theme-decision-log.md`.
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
  trong từng page (đã gộp 3 bản trùng lặp — xem `docs/web/theme/theme-decision-log.md`).

Dùng bằng cách thêm `Class="pos-kpi-value"` **cạnh** `Typo="Typo.h5"` hiện có trên `MudText`
(giữ nguyên Typo để không đổi hành vi ngữ nghĩa, CSS class chỉ ép lại giá trị hiển thị).

- **Đã rollout đầy đủ cho mọi page có KPI card** (cập nhật 2026-07-08, chiến dịch chuẩn hóa KPI
  card — xem `docs/web/theme/theme-decision-log.md`): toàn bộ 15 file có KPI/summary card trong
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
| Field label (label của input) | Không cần làm gì — `.mud-input-label-inputcontrol` đã ép bold/11px toàn cục (chữ thường, KHÔNG uppercase — chốt 2026-07-10) | Tự động |
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

## Lịch sử quyết định & trạng thái rollout

> Các đề xuất đã cân nhắc-và-loại-bỏ, tiến độ rollout theo từng chiến dịch, và TODO còn lại đã
> chuyển sang **`docs/web/theme/theme-decision-log.md`** — đọc file đó khi cần tra cứu "tại sao
> lại làm thế này" hoặc xem rollout còn thiếu gì. Mục này chỉ giữ rule đang áp dụng.

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

> TODO còn lại (chưa làm) — xem `docs/web/theme/theme-decision-log.md`.

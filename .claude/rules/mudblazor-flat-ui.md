# MudBlazor Flat UI — quy tắc áp dụng thực tế (POS.Web)

> File này là bản tóm tắt trỏ về nguồn sự thật thật sự: **`CLAUDE.md` §13 "UI Polish", §14
> "MudBlazor Flat UI Standard", §15 "Density Standard"** và
> **`.claude/skills/web/ui-polish-standard.md`**. Khi sửa 1 trong 4 nơi này, phải đối chiếu 3 nơi
> còn lại — tránh lệch giữa các tài liệu.
>
> **Bản v2 — cập nhật 2026-07-04**, thay cho bản gốc (hairline border, radius 4px, sidebar navy
> đậm — đã lỗi thời). Bản v2 theo mẫu MudBlazor chính thức "Mud Mini"
> (`docs/web/images/flat1.jpg`) — khác Ynex (không phải MudBlazor gốc, đã đánh giá và loại bỏ ở
> vòng trước) ở chỗ đây là demo dùng cùng framework nên áp dụng trực tiếp qua theme, không cần
> dựng lại component. Cùng ngày, sau khi áp dụng lên `ProductsPage.razor` làm pilot, đã tinh
> chỉnh thêm: button chuyển hẳn sang Outlined, filter panel có nền soft-tint, page-header/icon
> thu nhỏ, sidebar bỏ icon to + bỏ `MudDrawerHeader`.

## 1. Surface & Shadow (Elevation) — BORDERLESS, không còn hairline

- Cơ chế "phẳng" của dự án đã đổi từ **hairline border** sang **borderless hoàn toàn**:
  `Shadows.Elevation[1..5] = "none"` trong `PosTheme.cs` (giống Elevation 0). Card phân tách với
  nền chỉ bằng chênh lệch màu Surface `#FFFFFF` vs Background `#F2F4F8` — không viền, không bóng.
- `MudPaper`/`MudCard` chứa nội dung: `Elevation="1"` hoặc `Elevation="2"` (2 mức này giờ render
  giống hệt nhau về mặt shadow — chỉ còn ý nghĩa quy ước, không còn phân biệt thị giác).
- **KHÔNG** tự thêm `border`/`box-shadow` CSS cho MudPaper/MudCard để "tạo viền" — đi ngược
  nguyên tắc borderless, nền + radius 16px là đủ để tách khối.
- **KHÔNG** hạ Elevation của `MudPopover`/`MudDialog`/`MudMenu` — các overlay nổi tạm thời này giữ
  nguyên Elevation cao (E8/E12, shadow thật) để tách khỏi nền.
- `MudDrawer` (sidebar) **không** thuộc nhóm overlay cần giữ shadow — sidebar giờ cũng borderless
  đồng bộ với card (xem mục 5).

## 2. Form & Input

- `MudTextField`/`MudSelect`/`MudDatePicker`: luôn `Variant="Variant.Outlined"` +
  `Margin="Margin.Dense"`. (Không đổi so với bản v1.)
- Border-radius lấy từ theme `DefaultBorderRadius="16px"` (đã cấu hình trong `PosTheme.cs` —
  **tăng từ 4px**) và token CSS `--pos-radius-sm(4px)/md(8px)/lg(12px)` — **không hardcode**
  `border-radius` trên component MudBlazor (vd không tự thêm `Style="border-radius:4px"` trên
  `MudProgressLinear` — sẽ lệch với 16px toàn app; đã dọn ở `ProductsPage.razor`, còn ~35 page
  khác dùng cùng pattern cũ chưa dọn — xem TODO cuối file).
- **Font-size input**: cập nhật 2026-07-04 — `Typography.Body1` trong `PosTheme.cs` giảm còn
  `0.75rem` (12px, ~15% từ 14px cũ) + `FontWeight="400"`. Chi phối text trong
  `MudTextField`/`MudSelect`/`MudDatePicker`/`MudAutocomplete` + dropdown popup của chúng, áp
  dụng **global** (tự động cả trong dialog/form, không cần sửa từng file). **Không** ảnh hưởng
  `MudTable` (cell dùng size cố định riêng của MudBlazor).

## 3. Button — Outlined mọi nơi, phân biệt bằng Color

> Cập nhật 2026-07-04: bỏ `Variant.Filled` kể cả cho CTA chính của page.

```razor
<MudButton Variant="Variant.Outlined">Default</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Primary">Primary</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Secondary">Secondary</MudButton>
<MudButton Variant="Variant.Outlined" Disabled="true">Disabled</MudButton>
```

- CTA chính (nút "Thêm mới" trong `pos-page-header`, nút "Tìm" trong filter panel):
  `Variant="Variant.Outlined" Color="Color.Primary"` + `Size="Size.Small"` (khớp header đã thu
  nhỏ — xem mục 6).
- Hành động trung tính (Xóa/Clear, Hủy): `Variant="Variant.Outlined"` không đặt `Color`.
- Hành động phụ có ngữ nghĩa riêng (Export Excel...): giữ `Color` phù hợp, vẫn `Outlined`.
- Đã áp dụng ở `ProductsPage.razor` (nút "Thêm mới", "Tìm"). "Xóa" và "Excel" vốn đã Outlined từ
  trước nên không cần đổi.
- **Bẫy confirm dialog (phát hiện 2026-07-04, đã sửa 8 page)**: `DialogService.ShowAsync<MudMessageBox>
  (title, parameters, options)` render nút Yes bằng markup **mặc định của MudBlazor** — không có
  `<YesButton>` slot để chỉnh `Variant`, nút luôn ra `Filled` bất kể chuẩn dự án. Grep
  `MudButton.*Variant.Filled` KHÔNG bắt được lỗi này (nút không nằm trong markup của page). Luôn
  dùng `<MudMessageBox @ref="_confirmBox">` khai báo trực tiếp trong Razor +
  `<YesButton><MudButton Variant="Variant.Outlined" .../></YesButton>` + `_confirmBox!.ShowAsync()`
  — xem pattern đầy đủ (kể cả case Title/YesText/Color động) trong
  `.claude/skills/web/SKILLS.md` §"MudMessageBox @ref". Đã sửa: BusinessDayPage, VouchersPage,
  SpecialComboPage, PromotionSetupPage, PosDataSetupPage, DataRawLogPage, UsersPage, BankPosPage.

## 4. Bảng dữ liệu (MudTable)

- Luôn dùng `<MudTable>` (không tự viết `<table>` thô), với `Dense="true"` **bắt buộc** (theo
  Density Standard §15 — không hạ xuống `Dense="false"` để "thoáng" hơn), `Hover="true"`,
  `Striped="true"`, `HorizontalScrollbar="true"`.
- Màu header bảng: nền `--pos-bg-alt`, viền dưới đã **softening** từ `2px solid var(--pos-primary)`
  → `1px solid var(--pos-border)` (áp dụng cho cả `.mud-table` và `.pos-table`) — khớp tổng thể
  borderless v2, không còn viền navy đậm lạc quẻ trong 1 card đã bỏ hết viền. `.rpt-pivot-table`
  (pivot report) **giữ nguyên** viền cyan 2px cũ — là thiết kế riêng biệt có chủ đích, không thuộc
  hệ borderless chung.
- `MudTablePager` `PageSizeOptions` luôn bắt đầu bằng `10` (khớp `RowsPerPage` mặc định của
  `MudTable`).

## 5. Sidebar / AppBar — nền sáng, icon nhỏ gọn, brand header riêng

- `DrawerBackground`/`AppbarBackground`: `"#FFFFFF"` (đổi từ navy đậm `#1B3A5C`).
- `DrawerText`/`AppbarText`: dùng tint navy (`rgba(26,43,69,...)`/`#1A2B45`) thay vì trắng.
- Active nav item: pill bo tròn nền `var(--pos-primary-bg)` (`#E9EEF8`), chữ/icon
  `var(--pos-primary)`, `border-radius: var(--pos-radius-lg)` (12px, **không** phải `-md` 8px —
  đã tăng để pill tròn hơn khớp ảnh mẫu) — **không** dùng thanh viền trái (`border-left`).
- `.mud-nav-link` có inset ngang `margin-left/right: 8px` + `width: calc(100% - 16px)` — không
  bám sát mép sidebar, khớp gutter trong ảnh `flat1.jpg`.
- `MudAppBar` trong `MainLayout.razor` dùng `Color="Color.Default"` (đổi từ `Color.Primary`) để
  tự ăn theo `AppbarBackground`/`AppbarText` sáng — các phần tử con dùng `Color="Color.Inherit"`
  tự động theo, không cần sửa riêng.
- **Icon nav sidebar**: giảm còn `1.25rem` qua CSS `.mud-drawer .mud-icon-root` — mặc định
  MudBlazor 24px quá to so với text 13-14px cạnh bên (áp dụng luôn cho icon expand/collapse của
  `MudNavGroup`, cùng selector).
- **Icon set sidebar đổi sang `Icons.Material.Outlined.*`** (từ `Filled`) — nhẹ/mảnh hơn, khớp
  cảm giác "Mud Mini". Đã đổi toàn bộ icon trong `MudNavGroup`/`MudNavLink` ở `MainLayout.razor`
  (và tiện thể luôn 2 icon AppBar Menu/Logout vì cùng file — side-effect nhỏ, không phải yêu cầu
  ban đầu nhưng nhất quán và không có tác dụng phụ xấu).
- **Đã bỏ `MudDrawerHeader`** ("POSMaster POS System" — text thô, không có logo). Thay bằng
  `div.pos-sidebar-brand` (logo `MudAvatar Color="Color.Primary"` chữ "P" + `MudText` "POSMaster")
  đặt ngay đầu `MudDrawer`, trước `MudNavMenu` — khớp cấu trúc logo+brand của ảnh mẫu, tên rút
  gọn (không lặp lại nguyên văn "POS Dashboard – POSMaster" đã có sẵn trên AppBar).
- **Cập nhật 2026-07-04 (round 2 — decluttering thêm)**: icon `MudNavGroup` **cấp 2** (sub-group,
  vd Vận hành/Giao dịch/Báo cáo/Giám sát/Nhật ký...) đổi đồng nhất về `ChevronRight` — **giống hệt**
  icon cấp 3 (leaf) thay vì icon riêng biệt theo ngữ nghĩa (`Monitor`/`Assessment`/`Business`...) —
  chỉ còn cấp 1 giữ icon landmark riêng. Đồng thời mọi `MudNavGroup` (cấp 1 + cấp 2) thêm
  `HideExpandIcon="true"` để ẩn mũi tên expand/collapse mặc định (`ArrowDropDown`) bên phải —
  tránh 2 icon cùng lúc (icon trái + mũi tên phải) cho 1 mục. **Không đổi** cơ chế `@bind-Expanded`/
  accordion tự mở-đóng theo route (`docs/WEB_STATUS.md` mục I3) — chỉ ẩn phần hiển thị, giữ nguyên
  hành vi. Xem pattern đầy đủ + code mẫu: `.claude/skills/web/SKILLS.md` §"Sidebar nav (MainLayout)
  — 3 cấp". Cùng đợt: rút gọn dòng menu cấp 2 (`padding-top/bottom:3px` + `line-height:1.5`, thu
  ~15% so với mặc định) và `letter-spacing:-0.022em` cho `.mud-drawer .mud-nav-link` (rút tracking,
  tránh xuống dòng tên dài) trong `app.css`; đổi tên 6 title leaf cho ngắn gọn hơn (vd "Tỉnh / Thành"
  → "Chi nhánh", "Khai báo máy POS" → "POSTerminal").

## 6. Page header — thu nhỏ title/icon/button cho cân đối

- `.pos-page-header-title`: thêm `font-size: 1.25rem` (giảm từ mặc định h5 MudBlazor ~1.5rem —
  quá to so với tổng thể borderless, nhỏ gọn v2).
- Icon cạnh title: **bắt buộc** `Size="Size.Small"` (trước đây không set → mặc định 24px, lệch tỷ
  lệ với title đã thu nhỏ).
- Nút hành động đi kèm title (`pos-page-header-btn`): thêm `Size="Size.Small"` + đổi
  `Variant="Variant.Outlined"` (xem mục 3) để cân xứng với title/icon đã nhỏ lại.
- **Font-weight title: BẮT BUỘC `Style="font-weight:400"`** trên thẻ `MudText` title (chữ "tự
  nhiên", không đậm) — ghi đè font-weight 800 kế thừa từ `Typography.H5`. Đặt cục bộ trên từng
  `MudText`, **không** sửa `.pos-page-header-title` global (ảnh hưởng mọi page khác kể cả page
  chưa polish). Áp dụng cả cho page title-only không dùng `pos-page-header` (vd `ProductLockPage`
  dùng `MudText Typo="Typo.h5" Class="mb-4" Style="font-weight:400"` trực tiếp).
- Đã áp dụng cho toàn bộ 9 page trong menu "Danh mục" (Employees, Store, Provinces, PosMap,
  BankPos, Products, ProductLock, Prices, PriceSetup) — đây là chuẩn chung, không còn là ngoại lệ
  riêng của `ProductsPage.razor`.

## 7. Filter panel — nền soft-tint

- `MudPaper` chứa filter fields: thêm class `pos-filter-panel` (CSS mới, `background-color:
  var(--pos-primary-bg)`) — phân biệt vùng nhập liệu với card dữ liệu trắng (MudTable) bên dưới,
  không cần thêm viền.
- Dùng: `<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">`.

## 8. Hệ màu (Palette) — GIỮ NGUYÊN hex thương hiệu

- Primary: **Navy `#2051A3`** (Darken `#1B3A5C`, Lighten `#3A6FCC`) — **không đổi**, chỉ đổi cách
  dùng (nền sidebar/appbar chuyển sang trắng, Primary giờ dùng làm accent/text/active-state).
- Success `#27AE60`, Error `#DC3545`, Warning `#F39C12`, Info `#3A6FCC` — không đổi.
- Background `#F2F4F8` / `#EEF1F7`, Surface `#FFFFFF` — không đổi.
- Token mới: `--pos-primary-bg` (`#E9EEF8`) — dùng cho active-nav highlight VÀ nền
  `pos-filter-panel`; `--pos-teal-bg` (`#E6F7F4`) — dự phòng cho icon-badge KPI sau này nếu cần.

## 9. Màu trend/delta (%) — BẮT BUỘC giữ ngữ nghĩa

- `.pos-delta-up`/`.pos-delta-down` (app.css) — đã nâng cấp thành pill badge nền nhạt
  (`--pos-success-bg`/`--pos-danger-bg`), chữ đậm màu `--pos-success`/`--pos-danger`.
- **Quyết định rõ ràng**: dashboard vận hành POS (doanh thu, lỗi, trạng thái máy) giữ ngữ nghĩa
  **tăng=xanh/giảm=đỏ** — **KHÔNG** dùng màu trang trí tùy ý theo từng KPI kiểu mẫu tham khảo
  (ảnh mẫu dùng màu trang trí không ngữ nghĩa, nhưng đã quyết định KHÔNG áp dụng phần này vì mất
  tín hiệu tốt/xấu cho nhân viên vận hành).

## Đã cân nhắc và loại bỏ (giữ lại lịch sử quyết định)

| Đề xuất | Lý do loại bỏ |
|---|---|
| Theme Ynex (rebrand Indigo, navbar đầy icon, dark mode, dashboard Ecommerce mới) | Không phải MudBlazor gốc, rebrand toàn app rủi ro cao, nhiều phần không phục vụ nghiệp vụ POS |
| `Outlined="true"` cho MudPaper/MudCard | Thay bằng cơ chế borderless (Elevation=none) thay vì Outlined bool |
| Đổi hex Primary/Success/Error/Warning/Info | Giữ nguyên thương hiệu — chỉ đổi cách dùng (nền sáng, borderless, radius) |
| `Dense="false"` cho MudTable | Vi phạm Density Standard §15 (bắt buộc `Dense="true"`) |
| Font-size tăng (Body1/Body2/Caption) | Giữ nguyên — chỉ đổi H5 weight/letter-spacing, không đổi size |
| Màu trend trang trí không ngữ nghĩa (theo ảnh mẫu Mud Mini) | Mất tín hiệu tốt/xấu cho dashboard vận hành — giữ semantic xanh/đỏ |

## Trạng thái rollout (cập nhật 2026-07-04, đợt 2 — toàn bộ menu)

> Ban đầu chỉ áp dụng cho 9 page + 9 dialog menu "Danh mục" (pilot). Đợt 2 cùng ngày đã rollout
> đầy đủ cho **toàn bộ 4 cụm menu còn lại: Cửa hàng, Khuyến mãi, Vận hành, Quản trị** — tổng
> ~35 page + ~25 dialog. Cả `dotnet build` và `dotnet test tests/POS.ContractTests` (25/25) xanh
> sau đợt rollout này.

- **Button `Variant.Filled`/`Variant.Text` → `Outlined`**: áp dụng cho TOÀN BỘ `MudButton` trong
  mọi page + dialog thuộc 5 menu (Danh mục, Cửa hàng, Khuyến mãi, Vận hành, Quản trị) — không có
  ngoại lệ, kể cả nút Hủy/Đóng, nút trong confirm dialog/`MudMessageBox`, bulk action, nút Lưu
  cuối cùng. `MudChip` (status/tab badge) vẫn giữ `Variant.Filled` khi cần — không thuộc rule
  button. Đã grep xác nhận không còn `MudButton Variant.Filled/Text` nào sót trong
  `Components/Pages/` ngoại trừ `Login.razor` (không thuộc menu nào, cố ý không đổi) và
  `EosDayShiftListDialog.razor` (orphaned — không còn page nào mở dialog này, xác nhận qua grep).
- **Page header title/icon thu nhỏ + font-weight:400**: áp dụng cho tất cả page có
  `div.pos-page-header` hoặc title-only `MudText Typo.h5` trong cả 5 menu.
- **`pos-filter-panel`**: áp dụng cho mọi `MudPaper` đóng vai trò vùng nhập liệu/filter/khai báo
  trong cả 5 menu — không áp cho card nội dung thuần (KPI display, info card, MudCard nhóm field).
- **Hardcode `border-radius:4px`** trên `MudProgressLinear` đã dọn sạch trong cả 5 menu.
- **Icon set `Outlined`**: vẫn CHỈ áp dụng cho `MainLayout.razor` (sidebar + 2 icon AppBar) — icon
  bên trong nội dung từng page (page-header icon, icon trong button...) **vẫn giữ nguyên
  `Icons.Material.Filled.*`** như trước — đây là quyết định có chủ đích, chưa từng yêu cầu đổi
  đồng bộ icon set toàn app, chỉ sidebar mới cần cảm giác "Mud Mini" nhẹ/mảnh.
- 2 dialog bị bỏ sót ở đợt 1 (phát hiện và sửa ở đợt 2): `PriceItemPickerDialog.razor`
  (Catalog/Price) và `PosTerminalEditDialog.razor` (Ops) — nay đã Outlined đầy đủ.
- 1 page bị bỏ sót ở đợt 1 vì không nằm trong sidebar nav (chỉ reachable từ VouchersPage):
  `VoucherIssuePage.razor` + dialog `VoucherItemPickerDialog.razor` — nay đã convert đầy đủ,
  đối xứng với `CouponIssuePage.razor` (đã convert ở đợt 1).

## TODO còn lại (chưa làm — chỉ ghi nhận)

- `Login.razor`, vài page report (`PaymentBreakdownPage.razor`, `TopProductPage.razor`) có
  `border-radius` hardcode khác (8px/12px/2px) cho mini progress bar/card phụ, hoặc custom
  `<div style="border-radius:4px">` cho Pareto-bar visualization (không phải `MudProgressLinear`)
  — cố ý chưa đổi vì không phải hardcode trên component MudBlazor chuẩn.
- Icon set `Outlined` chưa mở rộng ra ngoài `MainLayout.razor` — nếu sau này muốn đồng bộ icon
  toàn app (page-header, button StartIcon...) thì đây sẽ là thay đổi diện rộng riêng, cần quyết
  định rõ ràng trước khi làm (ảnh hưởng ~35+ page).

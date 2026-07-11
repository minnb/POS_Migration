# Lịch sử quyết định & trạng thái rollout — MudBlazor Theme (POS.Web)

> Đây là **nhật ký lịch sử** (tại sao lại làm thế này, đã rollout tới đâu) — không phải rule đang
> áp dụng. Rule/pattern hiện hành nằm ở **`.claude/rules/mudblazor-flat-ui.md`**; chỉ đọc file này
> khi cần tra cứu bối cảnh 1 quyết định cụ thể hoặc xem tiến độ rollout còn thiếu gì.

---

## Đã cân nhắc và loại bỏ (giữ lại lịch sử quyết định)

| Đề xuất | Lý do loại bỏ |
|---|---|
| Theme Ynex (rebrand Indigo, navbar đầy icon, dark mode, dashboard Ecommerce mới) | Không phải MudBlazor gốc, rebrand toàn app rủi ro cao (quyết định thời v1) |
| `Outlined="true"` cho MudPaper/MudCard (v2) | Thay bằng Elevation=none (borderless) — nay v3 lại đổi tiếp sang Elevation có shadow thật |
| Đổi hex Primary/Success/Error/Warning/Info (v2) | v2 giữ nguyên thương hiệu; **v3 đã đổi** theo mockup theo yêu cầu "CustomTheme khớp 100%" — quyết định v2 không còn áp dụng |
| `Dense="false"` cho MudTable | Vi phạm Density Standard (bắt buộc `Dense="true"`) — không đổi qua các bản |
| Sidebar sáng + `MudAvatar` brand (v2) | v3 đảo ngược hoàn toàn sang navy đậm + brand text-only theo mockup mới |
| Emoji làm icon sidebar (thử ở v3, đã rollback) | Kỹ thuật thất bại: `Icon=` của MudNavLink/MudIcon nhận SVG path, không phải text — emoji khiến icon biến mất hoàn toàn. Đã rollback về Material Icons Outlined; sau đó người dùng yêu cầu thêm cấu trúc 3 cấp phân biệt icon (L1 không icon/IN HOA, L2 có icon riêng, L3 chevron đồng nhất) |
| Filter panel soft-tint `--pos-primary-bg` (v2) | v3 đổi sang trắng + border theo đúng mockup `.filter-bar` |
| Button Outlined mọi nơi (v2) | v3 đảo ngược: Filled cho CTA/hành động tích cực, Outlined cho phần còn lại — theo `.btn-primary` mockup |
| `text-transform:uppercase` cho field label input (`.mud-input-label-inputcontrol`, thêm ở đợt Typography audit 2026-07-06) | Chốt lại 2026-07-10: người dùng phản hồi label input (vd "Loại CTKM") viết hoa không đúng thẩm mỹ dự án mong muốn — bỏ `text-transform:uppercase`, **giữ chữ thường**, vẫn giữ `font-size:0.6875rem;font-weight:700;letter-spacing:0.5px`. Không đổi `.pos-kpi-label`/`.pos-section-label` (2 class riêng, không phải input label — vẫn uppercase theo đúng phạm vi ban đầu) |

---

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
- **Typography audit** (2026-07-06): `PosTheme.cs` (line-height/Button/Body1) +
  `app.css` (sidebar label, MudTable body cell, field label) sửa toàn cục — build + test xanh.
- **Chuẩn hóa KPI card** (2026-07-08, chiến dịch riêng — xem `mudblazor-flat-ui.md` §11 "KPI card
  — khuôn mẫu chuẩn" cho pattern hiện hành):
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
- **Đồng bộ toàn app theo `MemberPointsPage.razor`** (2026-07-09, chiến dịch lớn nhất — lấy
  `Store/Transactions/MemberPointsPage.razor` + `Store/Dialogs/MemberPointsDetailDialog.razor` làm
  page mẫu, khảo sát 95 file `Components/Pages/**/*.razor`, 66 page + 29 dialog):
  - **Status badge**: đổi `MudChip` tĩnh → `.pos-status-chip pos-status-{semantic}` (xem
    `mudblazor-flat-ui.md` §4a) — đây là điểm khiến §4a đổi từ "MudChip mặc định" sang
    "pos-status-chip mặc định".
  - **Button**: rollout `pos-btn-mockup` (mọi nút) + `pos-btn-secondary-mockup` (chỉ Outlined
    trung tính, xem §3a) trên toàn bộ `MudButton`.
  - **Filter/Input**: bổ sung `Adornment.Start` còn thiếu ở filter panel; giữ nguyên cấu trúc
    `pos-filter-panel` đã đúng ở phần lớn file.
  - **MudTable**: bổ sung `Dense/Hover/Striped/HorizontalScrollbar` còn thiếu; chuẩn hóa
    `MudTablePager PageSizeOptions` về `{10,20,50,100}`.
  - **Date/time**: chuẩn hóa có chọn lọc về `yyyy-MM-dd HH:mm:ss` cho cột lịch sử nhiều ngày —
    KHÔNG đổi máy móc nơi time-only là thiết kế có chủ đích (dashboard realtime "hôm nay") hoặc
    nơi format đến từ SP dưới dạng string đã format sẵn (`PricesPage`/`PriceSetupPage` —
    ngoài phạm vi, cần sửa DTO/SP riêng).
  - Batch theo domain: Store → Ops → Promotion → Catalog → Admin+root, build + contract test xanh
    sau mỗi batch.
  - **Chưa verify bằng mắt** — cùng lý do sandbox thiếu `POS_SECRET_KEY`/DB/Redis như batch KPI.
- **Đồng bộ title "QUẢN TRỊ" về `.pos-nav-section-label`** (2026-07-09, sau khi user báo bằng mắt
  thấy title nhỏ hơn 4 domain kia dù CSS khai báo cùng `font-size:0.625rem` — xem chi tiết
  `mudblazor-flat-ui.md` §5 "Cập nhật 2026-07-09"): `MainLayout.razor` (bỏ `MudNavGroup` bọc QUẢN
  TRỊ, 6 leaf lên top-level, xóa field `_expandAdmin`), `app.css` (xóa selector
  `.mud-navmenu > .mud-nav-group > .mud-nav-link` không còn dùng, thêm selector
  `.mud-navmenu > .mud-nav-link` style như L2 cho leaf QUẢN TRỊ) — build +
  `dotnet test tests/POS.ContractTests` xanh. **Chưa verify bằng mắt** (cùng lý do sandbox thiếu
  secret/DB/Redis) — cần user tự kiểm tra lại sidebar sau khi chạy app thật.

---

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
  rollout" ở trên).
- Chuẩn hóa KPI card (2026-07-08) mới verify qua build + contract test, **chưa chạy app thật để
  xem bằng mắt** — cần verify lại khi có môi trường đủ `POS_SECRET_KEY` + DB + Redis.

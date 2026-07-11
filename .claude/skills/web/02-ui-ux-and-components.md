# Skill: UI/UX & Component Standards POS.Web (MudBlazor 9.5.0)

> **Đọc file này khi:** viết/sửa markup của bất kỳ page/component nào trong `src/POS.Web/` — đây là
> bản "hiến pháp" rút gọn LUẬT BẮT BUỘC về layout, styling, data display, dialog, responsive, KPI card.
>
> **Quan hệ với các file khác** (file này là index luật, chi tiết + code mẫu ở nơi được trỏ):
> - Kiến trúc/auth/lifecycle/data access: **`01-architecture-and-logic.md`**.
> - Theme màu/Button/Elevation/Sidebar/Density đầy đủ: **`.claude/rules/mudblazor-flat-ui.md`** (nguồn
>   sự thật duy nhất cho theme — KHÔNG lặp giá trị hex/px ở đây).
> - Pattern DataTable/Store filter/Dialog/Chart chi tiết: **`SKILLS.md`**, `datatable.md`, `filter-store.md`,
>   `charts.md`, `form-input.md`.
> - Polish UI trang đã có: **`ui-polish-standard.md`**.
>
> Khi luật ở đây và file chi tiết lệch nhau → file chi tiết thắng; sửa lại file này cho khớp trong cùng commit.

---

## 1. Layout & Grid

- **Mọi form nhập liệu:** BẮT BUỘC `MudGrid` + `MudItem xs="12" sm="..." md="..."` — KHÔNG tự viết
  `<div>`/CSS Grid/Flexbox thuần cho bố cục field. `MudGrid` luôn có `Spacing="2"` (form/filter) hoặc
  `Spacing="3"` (KPI/chart) — không để trống. Chi tiết cách nhóm field bằng `MudCard`/validation/
  chế độ chỉ xem: **`form-input.md`**.
- **Filter panel:** bọc trong `MudPaper Elevation="1" Class="pos-filter-panel pa-4"` (flat, không
  shadow — theo mockup `.filter-bar`).
- **Data table:** `Elevation="2"` đặt **trực tiếp trên `<MudTable>`** (card, có shadow thật) — KHÔNG
  bọc thêm `MudPaper` ngoài chỉ để mang Elevation (`MudTable` tự render container riêng). Chi tiết:
  **`04-datatable-and-lists.md`** §2.
- KPI card row là ngoại lệ có chủ đích — dùng `d-flex flex-wrap` (KHÔNG `MudGrid`), xem §6.

## 2. Typography & Styling

- **CẤM inline CSS tùy ý** (`Style="color:...; font-size:..."`) cho màu sắc/kích thước chữ/spacing —
  các giá trị này đã có sẵn qua theme + class `.pos-*`. Ngoại lệ hẹp đã được chốt trong dự án
  (`Style="vertical-align:middle"` cạnh icon, `Style="align-self:center"` cho button cạnh
  `MudSelect`) — không mở rộng thêm ngoại lệ tùy tiện.
- **Cần style cục bộ thật sự riêng cho 1 component/page, không tái dùng nơi khác** → BẮT BUỘC CSS
  Isolation (`{Component}.razor.css`) — KHÔNG viết `<style>` inline trong `.razor`.
- **Cần ghi đè CSS của component con MudBlazor tự render** (`MudNavLink`, `MudTable` internals...)
  → ưu tiên `app.css` với selector toàn cục, KHÔNG dùng `.razor.css` (cần `::deep`, phức tạp hơn
  không cần thiết) — xem `.claude/rules/mudblazor-flat-ui.md` mục 10.
- **Màu/Typography/Elevation:** BẮT BUỘC lấy từ `CustomMudTheme` (`PosTheme.cs`) qua `Color="Color.Primary"`,
  `Typo="Typo.h5"`, `Elevation="..."` hoặc class `.pos-*` có sẵn trong `app.css` — CẤM hardcode hex
  (`#2660A4`...) hoặc px tùy ý trong markup Razor.
- Class typography chuyên biệt (KPI value/label, card title, section label) → dùng đúng `.pos-kpi-value`
  / `.pos-kpi-label` / `.pos-card-title` / `.pos-section-label` đã định nghĩa sẵn — xem checklist đầy
  đủ ở `.claude/rules/mudblazor-flat-ui.md` mục 11.1.

## 3. Data Display — MudTable

- **BẮT BUỘC** dùng `<MudTable>` — KHÔNG tự viết `<table>` HTML thuần cho data table mới (ngoại lệ
  duy nhất: pivot report cột-ngày động, xem `reports.md`).
- **Data lớn / cần phân trang phía server:** dùng `ServerData` + `@ref` + `_table.ReloadServerData()`
  — KHÔNG tự viết `MudPagination` thủ công. Data nhỏ tải 1 lần → `Items` client-side là đủ.
- **`HorizontalScrollbar="true"` BẮT BUỘC** trên mọi `MudTable` — thiếu → table bị clip trên mobile.
- `Dense="true"` + `Hover="true"` + `Striped="true"` — chuẩn mặc định (Density Standard).
- `MudTablePager PageSizeOptions` luôn `{ 10, 20, 50, 100 }`, BẮT BUỘC bắt đầu bằng `10`.
- **CẤM** thông báo dạng text thuần kiểu "Tìm thấy X dòng" phía trên/dưới bảng — số liệu summary
  BẮT BUỘC thể hiện qua KPI Cards (xem §6), không phải câu text rời rạc.
- Chi tiết đầy đủ (client/server/dynamic columns/footer tổng): `datatable.md`.

## 4. Dialogs

- **Layout nhất quán:** `DialogContent` chứa `MudGrid`/`MudForm` theo `form-input.md`; nút hành động
  đặt trong `DialogActions` — vị trí Lưu/Hủy **luôn cùng 1 chỗ** giữa các dialog (Hủy bên trái/trung
  tính, Lưu bên phải/CTA — theo Button convention `mudblazor-flat-ui.md` mục 3).
- **Đóng dialog thành công BẮT BUỘC trả DTO đầy đủ:**
  ```csharp
  MudDialog.Close(DialogResult.Ok(_model));   // ĐÚNG — trang cha nhận đủ dữ liệu để audit/refresh
  ```
  **TUYỆT ĐỐI CẤM** `DialogResult.Ok(true)` — trang cha (đặc biệt luồng audit CRUD) cần `newValue`
  đầy đủ, `Ok(true)` không mang dữ liệu gì.
- **Confirm dialog đơn giản (xóa/khóa/duyệt...):** BẮT BUỘC `MudMessageBox @ref` khai báo tường minh
  trong Razor + `<YesButton>` chọn đúng Variant/Color theo bản chất hành động (phá hủy → Outlined/
  Error; tích cực/chốt luồng → Filled/Primary hoặc Success/Warning tùy ngữ cảnh). **CẤM**
  `DialogService.ShowAsync<MudMessageBox>(...)` — không có slot `<YesButton>` để chỉnh màu, đã gây
  lỗi thật ở 8 page trong dự án. Pattern đầy đủ: `SKILLS.md` §"MudMessageBox @ref".

## 5. Responsive & Mobile

- **Bắt breakpoint bằng `IBrowserViewportService`** (`GetCurrentBreakpointAsync()` trong
  `OnAfterRenderAsync(firstRender)`) — dùng cho init trạng thái phụ thuộc viewport (vd sidebar drawer
  mở/đóng mặc định). KHÔNG viết `@media` query rải rác trong `.razor` để suy luận breakpoint bằng
  JS/CSS thủ công — MudBlazor + `app.css` đã xử lý phần lớn responsive qua class `.pos-*` toàn cục.
- Page header title+button → `div.pos-page-header` (KHÔNG `MudStack Row Justify.SpaceBetween`).
- Chip/badge container → luôn kèm `flex-wrap`.
- Breakpoint chuẩn dự án: `xs` < 600px, `sm` 600–959px, `md` 960px+ — chi tiết đầy đủ
  `.claude/rules/blazor-web-app.md` mục 10 và `mudblazor-flat-ui.md`.

## 6. KPI / Summary Cards

- **BẮT BUỘC** dùng khuôn mẫu chuẩn ("golden standard") ở `.claude/rules/mudblazor-flat-ui.md` mục 11
  — KHÔNG tự phát minh `MudGrid`/`MudPaper` layout riêng cho KPI card.
- Wrapper: `<div class="d-flex flex-wrap gap-3 mb-4">` (KHÔNG `MudGrid`/`MudItem` cho KPI row).
- Card: `MudPaper Elevation="2" Class="pa-4 text-center"` (Variant A, không icon) hoặc
  `Class="pa-4 pos-kpi-card-icon"` (Variant B, có icon minh họa — Ops/Admin).
- **Tiêu đề (label):** `MudText Typo="Typo.body2" Class="pos-kpi-label"` — chữ nhỏ, muted
  (`text-transform:uppercase`, màu nhạt theo token theme).
- **Số liệu (value):** `MudText Typo="Typo.h5" Class="pos-kpi-value"` — đậm, màu primary/semantic
  qua `border-left` accent 4px `var(--mud-palette-{semantic})`.
- **Trend/delta (%):** dùng component dùng chung `<PosDeltaBadge Current="..." Previous="..."
  Enabled="..."/>` — KHÔNG viết `RenderFragment TrendBadge()` cục bộ trong `@code` của page (đã có
  3 bản trùng lặp bị gộp lại thành component này).

---

## Checklist nhanh trước khi báo "xong" 1 page (UI/UX)

```
□ Form nhập liệu dùng MudGrid + MudItem (không tự viết layout thuần)
□ Filter panel → MudPaper Elevation="1" pos-filter-panel; Data table → Elevation="2" trực tiếp trên MudTable (không bọc thêm MudPaper)
□ Không inline CSS tùy ý; custom style cục bộ → CSS Isolation (.razor.css)
□ Màu/Typo/Elevation lấy từ theme/class .pos-* — không hardcode hex/px
□ MudTable: HorizontalScrollbar="true", Dense/Hover/Striped, PageSizeOptions bắt đầu bằng 10
□ Không hiển thị "Tìm thấy X dòng" dạng text — dùng KPI Cards
□ Dialog đóng thành công → DialogResult.Ok(_model), KHÔNG Ok(true)
□ Confirm dialog → MudMessageBox @ref + <YesButton> tường minh, không ShowAsync<MudMessageBox>
□ Breakpoint dùng IBrowserViewportService, không @media rải rác trong .razor
□ KPI card đúng khuôn mẫu mudblazor-flat-ui.md mục 11 (wrapper d-flex, pos-kpi-value/label, PosDeltaBadge)
```

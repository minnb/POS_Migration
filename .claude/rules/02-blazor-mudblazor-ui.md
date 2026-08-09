---
description: Luật thép Blazor Server + MudBlazor 9.5.0 cho POS.Web — auth/roles, Flat UI v9, DataTable, KPI card, confirm dialog, responsive/density
paths: ["src/POS.Web/**"]
---

# POS.Web — Blazor Server + MudBlazor 9.5.0

## Cổng chặn bắt buộc — trước khi viết markup page/component mới

1. Đọc skill `blazor-ui` (mega-skill: feature/chart/table/kpi/dialog/grid) +
   `.claude/skills/web/SKILL.md` (index + luật nền tảng) trước khi viết markup.
2. Có KPI/summary card → dùng khuôn mẫu chuẩn có sẵn (`skills/web/theme-reference.md` mục 11) —
   cấm tự viết `MudGrid`/`MudPaper` tùy ý.
3. Không chắc 1 pattern đã có chuẩn chưa → tìm trong skill trước, cấm đoán rồi tự viết CSS/markup
   mới. Thật sự chưa có → thêm vào đúng skill **cùng commit**.

## Auth & Roles

- Đăng nhập dùng **bridge token** qua minimal API endpoint (`/account/signin/{token}`) — **cấm**
  gọi `SignInAsync` trực tiếp trong Blazor InteractiveServer component (circuit crash, xem
  `skills/web/architecture-reference.md` §2).
- 4 role: `StoreOperator`/`BackOffice`/`ITOps`/`SystemAdmin` → policy
  `StoreAndAbove`/`BackOfficeAndAbove`/`OpsAndAbove`/`AdminOnly`.
- StoreOperator lọc row-level theo `store_codes` claim; rỗng = ITOps/Admin xem tất cả.

## Quy tắc bắt buộc (không ngoại lệ)

- **Serialization**: Newtonsoft.Json (`JsonConvert.*`) — cấm `System.Text.Json`.
- Mọi page tương tác: `@attribute [Authorize(Policy = ...)]` + `@rendermode InteractiveServer`.
- **Inject service qua DI trực tiếp** (POS.Application/Infrastructure) — cấm gọi HTTP đến
  POS.Api, cấm raw SQL trong page/component (phải qua Repository/Service).
- Page header có nút → `div.pos-page-header` — cấm `MudStack Row Justify.SpaceBetween`.
- DataTable → `<MudTable HorizontalScrollbar="true" Dense="true">` — cấm tự viết `<table>` hay
  `MudDataGrid` (ngoại lệ: pivot report). `MudTablePager PageSizeOptions` luôn chứa `10` đầu
  tiên.
- Confirm dialog → khai báo `<MudMessageBox @ref>` tường minh — cấm
  `DialogService.ShowAsync<MudMessageBox>(...)` (mất `<YesButton>` slot để chỉnh Variant/Color).
- KPI card → khuôn mẫu `.pos-kpi-value`/`.pos-kpi-label` + `<PosDeltaBadge>` — cấm viết
  `RenderFragment TrendBadge()` riêng trong page.
- Chip container → luôn có class `flex-wrap`. `MudAutocomplete` → cấm
  `ResetValueOnEmptyText="true"` cùng `MinCharacters="0"` (gây circuit crash) — luôn `.Take(N)`
  trong `SearchFunc`.
- Page có Create/Update/Delete → BẮT BUỘC inject `IAuditLogger`, gọi `LogAsync(...)` sau mỗi
  thao tác ghi thành công.
- MudBlazor v9: chart dùng `<Line T="double">`/`<Bar T="double">` — cấm cú pháp
  `<MudChart ChartType="...">` (v8, đã breaking-change).

## Density & Responsive (bắt buộc mọi page mới)

`Dense="true"` cho MudTable + `Margin="Margin.Dense"` cho field; `MudGrid Spacing="2"` (form) /
`"3"` (KPI/chart); mobile giữ vùng chạm tối thiểu 40px — CSS global (`app.css`) đã tự xử lý, cấm
tự thêm media query riêng cho từng component.

## Chi tiết đầy đủ (HOW) — đọc đúng skill khi cần

| Chủ đề | Skill |
|---|---|
| Tạo page/component mới (feature/chart/table/kpi/dialog/grid) | `blazor-ui` |
| Auth flow đầy đủ, roles table, services inject được, template page, v9 breaking changes, responsive, density, tổ chức thư mục Pages | `web` (`architecture-reference.md`) |
| Theme/màu/Elevation/Button/Table/Sidebar/Typography MudBlazor | `web` (`theme-reference.md`) |
| Audit 1 trang đã đúng chuẩn chưa (grep checklist) | `mudblazor-compliance` |
| Audit log CRUD | `web` (`audit-logging.md`) |
| Đồng bộ/làm đẹp UI trang đã có (chỉ sửa markup) | `web` (`ui-polish-standard.md`) |

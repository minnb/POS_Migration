---
name: mudblazor-compliance
description: Audit một trang .razor trong POS.Web đối chiếu chuẩn Responsive UI (§10), Flat UI (§14), Density (§15) và danh sách cấm (§11) trong CLAUDE.md — phát hiện vi phạm bằng lệnh grep cụ thể, phân biệt false positive (pivot table, page chỉ có title). Dùng sau khi tạo/sửa page .razor mới, hoặc khi audit định kỳ toàn bộ POS.Web.
---

# MudBlazor Compliance Audit — POS_Migration

Dự án hiện **chưa có bất kỳ test/analyzer nào** kiểm tra file `.razor` — `tests/POS.ContractTests`
chỉ khóa DTO/DI/middleware, không đụng tới Razor markup. Skill này là lớp kiểm tra đầu tiên: audit 1
trang `.razor` đối chiếu đúng rule đã có sẵn trong CLAUDE.md, **không tự đặt rule mới**.

**Nguồn sự thật** (khi sửa vi phạm, trỏ đúng nguồn dưới đây, không diễn giải lại):
- CLAUDE.md mục `10. Responsive UI Standard`, `11. KHÔNG làm những điều sau (POS.Web)`,
  `14. MudBlazor Flat UI Standard`, `15. Density Standard`
- `.claude/skills/web/SKILLS.md` — page component chuẩn, folder/role/policy
- `.claude/skills/web/datatable.md` — pattern DataTable đầy đủ (client/server-side, dynamic columns)

## Quy trình audit 1 file

Chạy lần lượt 12 lệnh grep dưới đây vào file `.razor` mục tiêu. Với **mỗi hit**, đối chiếu cột "Ngoại
lệ" trước khi kết luận là vi phạm thật — đây là audit bằng grep nên luôn đọc context quanh dòng match,
không tự động kết luận chỉ từ 1 dòng lệnh.

| # | Vi phạm | Lệnh phát hiện | Ngoại lệ (KHÔNG tính là lỗi) |
|---|---|---|---|
| 1 | Header dùng `MudStack Row Justify.SpaceBetween` thay vì `div.pos-page-header` | `grep -n 'MudStack.*Row="true".*SpaceBetween' {file}` | Page chỉ có title, không có button → dùng `MudText Typo.h5` trực tiếp là đúng |
| 2 | `MudTable` thiếu `HorizontalScrollbar="true"` | `grep -n -A5 '<MudTable' {file}` rồi soát 5 dòng sau có attribute không | Pivot report dùng `<table class="pos-table rpt-pivot-table">` trong wrapper `overflow-x:auto` |
| 3 | `MudTablePager` có `PageSizeOptions` nhưng không chứa `10` | `grep -n 'PageSizeOptions' {file}` | — |
| 4 | Input dùng `Variant="Variant.Filled"` hoặc thiếu `Margin="Margin.Dense"` | `grep -n 'Variant="Variant.Filled"' {file}` | Button CTA được phép `Filled` (chỉ input field mới cấm) |
| 5 | `MudPaper`/`MudCard` có `Elevation="3"` trở lên | `grep -nE 'Elevation="[3-9]"' {file}` | KHÔNG áp dụng cho `MudPopover`/`MudDialog`/`MudDrawer`/`MudMenu` — các overlay này PHẢI giữ elevation mặc định, đừng báo là lỗi |
| 6 | Container chứa `MudChip` thiếu class `flex-wrap` | `grep -n -B2 'MudChip' {file}` rồi soát div cha | — |
| 7 | Dùng `&nbsp;` (kiểu `&nbsp;\|&nbsp;`) làm separator trong summary text | `grep -n '&nbsp;' {file}` | — |
| 8 | Cú pháp MudChart v8 cũ | `grep -nE 'ChartType="ChartType\.|XAxisLabels|ChartSeries<' {file}` | — |
| 9 | `<MudChip` thiếu `T="string"` | `grep -n '<MudChip' {file} \| grep -v 'T='` | — |
| 10 | Hardcode `px` cho layout (width/min-height) | `grep -nE 'width:\s*[0-9]+px\|min-height:\s*[0-9]+px' {file}` | Kích thước icon nhỏ cố định (16px, 24px...) không phải layout |
| 11 | Page tương tác thiếu `@attribute [Authorize` hoặc `@rendermode InteractiveServer` | `grep -L '@attribute \[Authorize' {file}` (không match nghĩa là thiếu) | Trang thật sự public (hiếm, xác nhận thủ công) |
| 12 | `MudAutocomplete` dùng `ResetValueOnEmptyText="true"` + `MinCharacters="0"`, hoặc `SearchFunc` thiếu `.Take(N)` | `grep -n 'ResetValueOnEmptyText\|MinCharacters="0"' {file}` + `grep -n 'SearchFunc' {file}` rồi soát method có `.Take(` không | — |

Output kỳ vọng: danh sách vi phạm thật (đã loại false positive) kèm số dòng, không liệt kê raw grep output chưa lọc.

## Quy trình audit toàn bộ POS.Web (định kỳ)

```bash
find src/POS.Web/Components/Pages -name "*.razor" | while read -r f; do
  echo "=== $f ==="
  grep -n '&nbsp;' "$f"                                    # #7 — dễ bỏ sót nhất khi đọc nhanh
  grep -nE 'Elevation="[3-9]"' "$f"                         # #5 — dễ báo nhầm overlay, tự soát lại
done
```

Chạy riêng 2 lệnh trên trước (dễ false-negative khi đọc mắt thường), sau đó mới chạy đủ 12 lệnh cho
từng file có nghi vấn.

## Cách sửa từng loại vi phạm

- **#1–#6, #9–#11**: xem ví dụ đúng trong CLAUDE.md mục `10. Responsive UI Standard` (A–F) và
  `14. MudBlazor Flat UI Standard` — copy đúng snippet tương ứng, không tự sáng tác cú pháp mới.
- **#2 (DataTable)** nếu cần dựng lại toàn bộ bảng (client/server-side, dynamic columns, footer tổng):
  đọc `.claude/skills/web/datatable.md` — không lặp lại nội dung đó ở đây.
- **#7 (`&nbsp;`)**: thay bằng `<div class="d-flex flex-wrap gap-2">` chứa nhiều `MudText` con, đúng
  format CLAUDE.md §10 bảng "Summary/info text nhiều phần".
- **#12 (MudAutocomplete)**: dùng nguyên pattern `SearchFunc` chuẩn trong CLAUDE.md mục
  `13. MudAutocomplete — BẮT BUỘC tránh circuit crash` (có `.Take(50)` + `Clearable="true"`, KHÔNG
  `ResetValueOnEmptyText` + `MinCharacters="0"` cùng lúc).

## Checklist cuối cùng

```
□ Đã chạy đủ 12 lệnh grep trong bảng, không chỉ chạy #7/#5
□ Mỗi hit đã đối chiếu cột "Ngoại lệ" trước khi báo là lỗi thật
□ Không báo sai Elevation của MudPopover/MudDialog/MudDrawer/MudMenu là vi phạm
□ Không báo sai page chỉ-có-title (không button) là thiếu pos-page-header
□ Vi phạm thật → sửa theo đúng snippet trong CLAUDE.md §10/§14, không tự sáng tác cú pháp
□ Vi phạm DataTable phức tạp → đọc .claude/skills/web/datatable.md, không tự suy diễn
```

# STATUS.md — Tiến độ migration từ src/legacy/ (VCM.BLUEPOS)

> Tổng hợp toàn cảnh tiến độ port từng chức năng từ `src/legacy/VCM.BLUEPOS` sang
> `POS.Web`/`POS.Api` mới. Cập nhật file này là một phần của "xong" cho mọi task phân tích
> hoặc port — xem `.claude/skills/migration/SKILLS.md`.

Trạng thái dùng 1 trong 4 giá trị: `Chưa bắt đầu` / `Đang phân tích` / `Đã phân tích — chưa port`
/ `Đã port`.

| Feature | File phân tích | Trạng thái | Ghi chú |
|---|---|---|---|
| Barcode | `FEATURE_Barcode_ANALYSIS.md` | Đã phân tích — chưa port | — |
| SetupLoyalty | `FEATURE_SetupLoyalty_ANALYSIS.md` | Đã phân tích — chưa port | 3 SP không tìm thấy định nghĩa trong script cũ — cần hỏi lại trước khi port (xem mục 7 của file phân tích) |

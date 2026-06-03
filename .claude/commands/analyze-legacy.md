# /analyze-legacy — Phân tích Code Cũ và Lập Kế hoạch Convert

## Mô tả
Dùng lệnh này để Claude đọc toàn bộ (hoặc một phần) source code cũ,
phân tích nghiệp vụ, và tạo ra kế hoạch convert chi tiết.

## Cách dùng trong Antigravity + Claude Code

```
/analyze-legacy
```

Sau đó Claude sẽ hỏi bạn muốn phân tích phạm vi nào.

---

## PROMPT ĐẦY ĐỦ (copy & paste vào Claude Code chat)

```
Tôi cần bạn phân tích source code cũ của dự án POS Backend (.NET Framework 4.6)
để chuẩn bị kế hoạch convert sang .NET Core 10.

**BƯỚC 1 — Đọc context dự án mới trước:**
Hãy đọc các file sau của dự án MỚI:
- CLAUDE.md (quy tắc và kiến trúc)
- docs/conventions.md (coding conventions)
- docs/api-mapping.md (mapping endpoint)

**BƯỚC 2 — Phân tích source code cũ:**
Hãy quét và đọc toàn bộ source code trong thư mục `../POS.Backend/` (dự án cũ).
Tập trung vào:
- Tất cả các file Controller (tìm trong thư mục Controllers/)
- Các file Service / Manager / Handler chứa business logic
- Các file Model / Entity
- File cấu hình (Web.config, AppSettings)
- Các External Service integrations (GotIt, Urbox, v.v.)

**BƯỚC 3 — Tạo báo cáo phân tích:**
Sau khi đọc xong, hãy tạo file `docs/analysis-report.md` với nội dung:

1. **Danh sách tất cả Controllers** tìm thấy, với:
   - Tên Controller
   - Các Action methods (HTTP verb + route)
   - Mô tả nghiệp vụ ngắn gọn của từng action
   - Mức độ phức tạp (Đơn giản / Trung bình / Phức tạp)

2. **Danh sách Dependencies bên ngoài** (external APIs, services):
   - Tên service
   - Cách gọi (URL, authentication)
   - Các endpoints được dùng

3. **Các Pattern & Anti-pattern** phát hiện trong code cũ:
   - Những gì cần giữ lại (logic nghiệp vụ)
   - Những gì cần thay thế (HttpContext.Current, ConfigurationManager, v.v.)
   - Các vấn đề tiềm ẩn

4. **Kế hoạch Convert ưu tiên:**
   - Thứ tự convert các module (từ đơn giản đến phức tạp)
   - Ước tính số lượng files cần tạo cho mỗi module
   - Các rủi ro hoặc điểm cần chú ý đặc biệt

5. **Cập nhật `docs/api-mapping.md`:**
   - Điền đầy đủ endpoint cũ vào bảng mapping
   - Đề xuất endpoint mới theo convention trong docs/conventions.md

**QUAN TRỌNG:**
- Không tạo bất kỳ file code (.cs) nào ở bước này
- Chỉ đọc, phân tích và viết tài liệu
- Nếu không chắc về nghiệp vụ, ghi chú "Cần xác nhận với team" vào báo cáo
```

---

## Kết quả mong đợi

Sau khi chạy lệnh này, bạn sẽ có:
- `docs/analysis-report.md` với toàn bộ phân tích
- `docs/api-mapping.md` được cập nhật đầy đủ endpoint cũ

Tiếp theo: dùng `/convert-module` để bắt đầu convert từng module.

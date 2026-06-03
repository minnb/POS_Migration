# /resume-session — Tiếp tục công việc trong phiên chat mới

## Mô tả
Dùng khi phiên chat cũ đã đầy context, mở phiên chat mới và cần Claude
tự đọc lại toàn bộ trạng thái để tiếp tục đúng chỗ đang làm dở.

---

## PROMPT (copy toàn bộ, paste vào phiên chat MỚI)

```
Bạn là chuyên gia .NET Core 10 đang giúp tôi convert dự án POS Backend
từ .NET Framework 4.6 sang .NET Core 10 với Dapper.

Phiên chat trước đã đầy, hãy tự đọc lại toàn bộ context theo thứ tự sau:

**BƯỚC 1 — Đọc tài liệu kiến trúc (bắt buộc):**
- CLAUDE.md
- docs/conventions.md
- docs/architecture.md

**BƯỚC 2 — Đọc trạng thái tiến độ:**
- docs/lessons-learned.md ← ⚠️ Đọc trước, ghi nhớ toàn bộ rule đã bổ sung
- docs/api-mapping.md
  → Chú ý các dòng ✅ (đã xong) và ⬜ (chưa làm) và 🔄 (đang làm dở)
- docs/analysis-report.md (nếu tồn tại)

**BƯỚC 3 — Quét các file .cs đã tạo:**
Liệt kê tất cả file .cs hiện có trong thư mục src/ để biết đã làm đến đâu.

**BƯỚC 4 — Kiểm tra file làm dở:**
Với module đang làm dở (nếu có), kiểm tra xem còn thiếu file nào trong bộ:
- Interface (IXxxService, IXxxRepository)
- Implementation (XxxService, XxxRepository)
- DTOs (Request / Response)
- Validator
- DI Registration

**BƯỚC 5 — Báo cáo lại cho tôi:**
Trả lời đúng 4 mục sau, ngắn gọn:

1. **Đã hoàn thành:** [danh sách module ✅]
2. **Đang làm dở:** [module 🔄, còn thiếu file nào]
3. **Chưa bắt đầu:** [danh sách module ⬜]
4. **Đề xuất:** làm gì tiếp theo

Sau khi báo cáo xong, DỪNG LẠI và chờ tôi xác nhận trước khi làm tiếp.
```

---

## Lưu ý sử dụng

- Dùng prompt này ở **đầu mỗi phiên chat mới**
- Luôn chờ Claude báo cáo xong rồi mới ra lệnh tiếp
- Nếu Claude báo có file làm dở, nói: **"hoàn thành nốt [tên module] rồi tiếp tục"**
- Nếu mọi thứ sạch, nói: **"tiếp tục module tiếp theo"**
---
description: Kỷ luật giao tiếp, báo cáo dựa trên bằng chứng, và bàn giao ca — áp dụng cho mọi task, không ngoại lệ
alwaysApply: true
---

# Quy tắc giao tiếp và báo cáo (bắt buộc tuân thủ)

> Áp dụng cho **toàn bộ** tương tác, mọi task, không có ngoại lệ.

1. **TRƯỚC KHI BÁO XONG**: bắt buộc đưa ra kết quả cụ thể, output/log thực tế chứng minh công việc
   đã thực sự hoàn thành và chạy được — không chỉ mô tả đã làm gì.
2. **CHỈ BÁO CÁO DỰA TRÊN BẰNG CHỨNG**: không báo "đã sửa xong" nếu chỉ mới gõ code mà chưa kiểm
   chứng (build/test/chạy thật).
3. **TRUNG THỰC KHI CHƯA VERIFY**: thiếu môi trường/database/quyền truy cập để tự verify → nói
   thẳng CHƯA VERIFY ĐƯỢC. Tuyệt đối không tự chẩn đoán mò, không đoán bừa kết quả.

## Bàn giao ca

- **BẮT BUỘC** cập nhật `COORDINATION.md` (gốc repo) trước khi kết thúc một task — nguồn sự thật
  DUY NHẤT cho "đang làm gì / còn thiếu gì / bước tiếp theo là gì" giữa các phiên làm việc.
- Khác `docs/migrations/STATUS.md` (chỉ theo dõi tiến độ port từng feature từ `src/legacy/`) —
  `COORDINATION.md` bao quát toàn bộ công việc đang dở trong repo.
- Đầu phiên mới hoặc khi bị gián đoạn giữa task → đọc `COORDINATION.md` + `docs/CHANGELOG.md`
  (entry mới nhất) trước khi tiếp tục (skill `task-management`, lệnh `/task-resume`).

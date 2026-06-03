# /add-rule — Cập nhật Rule Bắt Buộc Sau Khi Phát Hiện Lỗi

## Mô tả
Dùng khi phát hiện Claude Code bỏ qua hoặc làm sai một logic quan trọng.
Command này yêu cầu Claude đưa rule đó vào các file .md liên quan để
các lần convert tiếp theo không lặp lại lỗi tương tự.

---

## PROMPT (copy toàn bộ, paste vào chat — thay phần [MÔ TẢ LỖI])

```
Tôi vừa phát hiện và fix một lỗi quan trọng mà bạn đã bỏ qua trong quá trình convert.

**Mô tả lỗi:**
[MÔ TẢ LỖI CỤ THỂ Ở ĐÂY]
Ví dụ: "Khi mapping field X từ response cũ, bạn đã tự đổi kiểu dữ liệu từ int
sang string thay vì giữ nguyên như code cũ, dẫn đến POS client parse lỗi."

**Yêu cầu:**
Hãy đưa lỗi này thành rule bắt buộc bằng cách cập nhật các file .md sau:

1. **CLAUDE.md**
   - Thêm rule vào mục phù hợp (section 3.1 nếu liên quan route/response,
     section 4 nếu là quy tắc chung, hoặc tạo mục mới nếu cần)
   - Đánh dấu 🔴 để nổi bật
   - Viết rõ: SAI là gì, ĐÚNG là gì, kèm ví dụ code nếu cần

2. **docs/conventions.md**
   - Thêm vào mục liên quan hoặc tạo mục mới
   - Ghi rõ pattern SAI và pattern ĐÚNG để dễ đối chiếu

3. **docs/lessons-learned.md** (tạo mới nếu chưa có)
   - Ghi lại lỗi theo format:
     ## [Ngày] — [Tên lỗi ngắn gọn]
     - **Triệu chứng:** POS client bị lỗi gì
     - **Nguyên nhân:** Claude đã làm gì sai
     - **Fix:** Đã sửa thế nào
     - **Rule bổ sung:** Rule mới đã thêm vào file nào

4. **Cập nhật checklist trong CLAUDE.md**
   - Thêm 1 dòng kiểm tra mới vào "Checklist trước khi hoàn thành một task"
   - Nội dung checklist phải detect được đúng lỗi vừa xảy ra

Sau khi cập nhật xong, đọc lại toàn bộ module đang làm dở (nếu có)
và kiểm tra xem lỗi tương tự có xuất hiện ở chỗ nào khác không.
Nếu có, fix luôn trước khi tiếp tục.
```

---

## Ví dụ điền [MÔ TẢ LỖI]

**Ví dụ 1 — Lỗi mapping kiểu dữ liệu:**
```
Khi mapping field TransactionId từ response cũ, bạn đã tự đổi từ
kiểu long sang string. POS client expect số nguyên nhưng nhận được
chuỗi nên parse thất bại.
Rule cần thêm: Không tự ý đổi kiểu dữ liệu của bất kỳ field nào
trong response — phải đọc code cũ xác định kiểu chính xác rồi mới viết DTO.
```

**Ví dụ 2 — Lỗi bỏ qua field null:**
```
API cũ trả field ErrorMessage = null khi thành công, bạn đã bỏ field
đó khỏi DTO khiến JSON thiếu field. POS client check field đó để
xác định trạng thái nên bị lỗi.
Rule cần thêm: Không bỏ field nào khỏi Response DTO dù field đó
có vẻ không cần thiết — phải giữ đủ tất cả field như JSON cũ.
```

**Ví dụ 3 — Lỗi logic nghiệp vụ:**
```
Bạn đã tự thay đổi điều kiện kiểm tra voucher hết hạn từ
">= ExpiryDate" thành "> ExpiryDate", làm voucher hết hạn đúng ngày
vẫn được chấp nhận.
Rule cần thêm: Không tự ý thay đổi toán tử so sánh hoặc điều kiện
trong business logic — copy chính xác từ code cũ.
```
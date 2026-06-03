# /review-code — Review và Refactor Code Vừa Tạo

## Mô tả
Dùng sau khi convert xong một module để kiểm tra chất lượng code.

---

## PROMPT (thay `{MODULE_NAME}`)

```
Hãy review toàn bộ code của module **{MODULE_NAME}** vừa được tạo.

**Đọc lại trước:**
- CLAUDE.md (checklist bắt buộc)
- docs/conventions.md

**Kiểm tra từng file trong `src/POS.Application/{MODULE_NAME}/`
và `src/POS.API/Controllers/{Module}Controller.cs`:**

1. **[QUAN TRỌNG NHẤT] Route có khớp chính xác với API cũ không?**
   - Mở file controller cũ tương ứng trong `../POS.Backend/`
   - So sánh từng ký tự của `[Route(...)]` và `[HttpPost/Get/Put/Delete(...)]`
   - Có bị thêm `/v1/`, đổi tên segment, hoặc đổi HTTP verb không?

2. **[QUAN TRỌNG NHẤT] JSON response có giống hệt API cũ không?**
   - Đọc code cũ để xác định JSON structure thực tế
   - So sánh field names trong Response DTO với JSON cũ
   - Có bị bọc thêm wrapper (`data: {...}`, `success: true`, v.v.) không?
   - Có field nào bị thêm/bớt/đổi tên không?

3. **Async/Await:**
   - Tất cả methods có phải `async Task<>`?
   - Có chỗ nào dùng `.Result` hoặc `.Wait()` không?

4. **Validation:**
   - Mọi Request DTO có Validator không?
   - Validator có đủ rules cần thiết không?

5. **Logging:**
   - Có log ở đầu mỗi method quan trọng không?
   - Có log error khi catch exception không?
   - Có dùng string interpolation thay vì structured logging không?

6. **Error Handling:**
   - Có dùng custom exceptions đúng cách không?
   - Có try-catch không cần thiết không?
   - Error codes có theo convention `POS_{MODULE}_{NUMBER}` không?

7. **DI Registration:**
   - Tất cả services và repositories đã được đăng ký chưa?

8. **Naming:**
   - Đúng theo conventions.md chưa?

9. **Hardcode:**
   - Có string nào bị hardcode không?
   - Connection string, URL, API key có trong config không?

**Sau khi review:**
- Tự fix tất cả vấn đề tìm thấy
- Liệt kê danh sách những gì đã sửa
- Nếu có vấn đề nghiệp vụ không chắc, ghi thành TODO comment
```
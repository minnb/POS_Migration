# /review-task — Rà soát code của task vừa làm trong phiên (chỉ đọc, KHÔNG sửa)

Dùng lệnh này sau khi vừa hoàn thành (hoặc tạm xong) một task trong phiên chat, để một
"Senior Dev" rà soát lại toàn bộ code vừa thay đổi trước khi commit.

> **Chế độ CHỈ ĐỌC (read-only).** Lệnh này TUYỆT ĐỐI KHÔNG được Edit/Write/sửa bất kỳ file
> nào. Chỉ báo cáo. Chỉ được sửa code ở lượt sau, khi người dùng xác nhận rõ ràng.

---

## Cách dùng

```
/review-task
```

Tùy chọn — thu hẹp phạm vi vào 1 vùng cụ thể:
```
/review-task chỉ tập trung POS.Web
/review-task chỉ các file trong src/POS.Infrastructure
```

---

## Quy trình Claude thực hiện

### Bước 1: Xác định phạm vi thay đổi
1. Ưu tiên các file đã tạo/sửa **trong phiên chat hiện tại** (task vừa làm).
2. Đối chiếu bằng `git diff` để lấy chính xác nội dung thay đổi:
   ```powershell
   git --no-pager status --short
   git --no-pager diff            # thay đổi chưa staged
   git --no-pager diff --staged   # thay đổi đã staged
   ```
3. Nếu người dùng nêu phạm vi (vd "chỉ POS.Web") → chỉ rà soát vùng đó.
4. Nếu không có thay đổi nào (git diff rỗng và phiên chưa sửa file) → báo "Không có thay đổi
   để review" và dừng.

### Bước 2: Đóng vai Senior Dev, rà soát toàn bộ code vừa thay đổi
Áp dụng đúng prompt gốc:

> Đóng vai trò Senior Dev, hãy rà soát lại toàn bộ code vừa thay đổi. Tìm kiếm các lỗi
> logic, rủi ro hiệu năng, và đối chiếu xem có tuân thủ file CLAUDE.md không. Hãy báo cáo
> chi tiết các vấn đề, tóm tắt các file đã sửa để tôi nắm bối cảnh, và tuyệt đối KHÔNG ĐƯỢC
> TỰ Ý SỬA CODE cho đến khi tôi xác nhận.

Cụ thể cần soi:
- **Lỗi logic**: điều kiện sai, null/boundary, race condition, sai luồng nghiệp vụ.
- **Rủi ro hiệu năng**: query N+1, thiếu cache Redis theo chuẩn, `.ToList()` không giới hạn,
  I/O đồng bộ trên hot path, thiếu `CancellationToken`, block circuit (POS.Web).
- **Tuân thủ CLAUDE.md + `.claude/rules/`**: đối chiếu đúng các luật liên quan tới vùng code
  vừa sửa, ví dụ:
  - `System.Text.Json` bị cấm → phải `Newtonsoft.Json`; cấm đổi tên field JSON DTO response.
  - Controller inject Application interface (không inject Infrastructure trực tiếp);
    AppService 3 lớp; DI đã đăng ký chưa.
  - Nếu đụng DTO response → đã có contract test khóa field chưa (`tests/POS.ContractTests`).
  - Nếu đụng `.razor` (POS.Web) → LUẬT THÉP UI: `MudTable HorizontalScrollbar`,
    `div.pos-page-header`, `@rendermode`/`@attribute [Authorize]`, `MudAutocomplete` an toàn
    circuit, audit log cho CRUD...
  - Nếu đụng SP/SQL → naming `usp_{Domain}_{Action}`, bracket-quote reserved keyword,
    `SET XACT_ABORT ON`, manifest.json.

### Bước 3: Báo cáo (định dạng cố định)
```
## 📋 Tóm tắt file đã thay đổi
- <path>: <mô tả ngắn thay đổi làm gì>
...

## 🐞 Vấn đề phát hiện
### 🔴 Nghiêm trọng (phải sửa)
- [file:line] <mô tả + vì sao sai + gợi ý hướng sửa>
### 🟡 Nên sửa
- ...
### 🔵 Góp ý / tùy chọn
- ...

## ✅ Đối chiếu CLAUDE.md
- <luật đã kiểm> → Đạt / Vi phạm (nêu rõ)

## 👉 Bước tiếp theo
Liệt kê các sửa đề xuất. CHỜ xác nhận của người dùng rồi mới sửa.
```

Nếu không có vấn đề → nói rõ "Không phát hiện vấn đề nghiêm trọng" thay vì bịa lỗi.

### Bước 4: Dừng lại và chờ
- KHÔNG tự sửa code.
- Kết thúc bằng câu hỏi: người dùng muốn sửa mục nào (nếu có) thì xác nhận.

---

## Ràng buộc bắt buộc
- **Read-only tuyệt đối** trong lượt chạy lệnh này: chỉ dùng Read / Grep / Glob / git diff.
  KHÔNG Edit, KHÔNG Write, KHÔNG chạy lệnh làm thay đổi repo.
- Chỉ báo cáo dựa trên code thật đã đọc (theo QUY TẮC GIAO TIẾP trong CLAUDE.md) — không đoán mò.
- Chỉ đối chiếu những luật CLAUDE.md **liên quan** tới vùng code vừa sửa, không liệt kê máy móc
  toàn bộ.

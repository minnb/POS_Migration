# /convert-module — Convert Một Module Từ Code Cũ Sang .NET Core 10

## Mô tả
Dùng lệnh này để convert một module cụ thể.
Chạy sau khi đã có `docs/analysis-report.md` từ lệnh `/analyze-legacy`.

## Cách dùng

```
/convert-module [tên module]

Ví dụ:
/convert-module Session
/convert-module Payment-Voucher
/convert-module Loyalty
```

---

## PROMPT ĐẦY ĐỦ (thay `{MODULE_NAME}` và `{SOURCE_FILES}`)

```
Hãy convert module **{MODULE_NAME}** từ dự án POS Backend cũ sang .NET Core 10.

**BƯỚC 1 — Đọc context (BẮT BUỘC trước khi viết bất kỳ dòng code nào):**
1. Đọc `CLAUDE.md` — quy tắc kiến trúc tổng thể
2. Đọc `docs/conventions.md` — coding conventions
3. Đọc `docs/analysis-report.md` — phần liên quan đến module {MODULE_NAME}

**BƯỚC 2 — Đọc source code cũ của module:**
Đọc các file sau từ dự án cũ:
{SOURCE_FILES}
(Ví dụ: ../POS.Backend/Controllers/VoucherController.cs
         ../POS.Backend/Services/VoucherService.cs
         ../POS.Backend/Models/Voucher.cs)

**BƯỚC 3 — Tạo đầy đủ các files sau (theo đúng thứ tự):**

**3a. Domain Layer** (`src/POS.Domain/`):
- Entities liên quan (nếu chưa có)
- Enums liên quan (nếu chưa có)

**3b. Application Layer** (`src/POS.Application/{MODULE_NAME}/`):
- `Interfaces/I{Name}Service.cs` — Interface với đầy đủ XML doc comments
- `DTOs/{Action}{Entity}Request.cs` — Request DTOs
- `DTOs/{Action}{Entity}Response.cs` — Response DTOs
- `Validators/{Request}Validator.cs` — FluentValidation rules
- `Services/{Name}Service.cs` — Implementation

**3c. Infrastructure Layer** (`src/POS.Infrastructure/`):
- `Repositories/I{Entity}Repository.cs` — Interface
- `Repositories/{Entity}Repository.cs` — EF Core implementation
- `ExternalServices/{Name}Client.cs` — Nếu có gọi API bên ngoài

**3d. API Layer** (`src/POS.API/Controllers/`):
- `{Module}Controller.cs` — Controller với route **copy chính xác từ code cũ**
- `[Route(...)]` và `[HttpPost/Get(...)]` phải khớp **từng ký tự** với route cũ
- Response trả về trực tiếp, **KHÔNG bọc wrapper**

**RÀNG BUỘC CỨNG — ƯU TIÊN CAO NHẤT:**
- ✅ Route API mới = Route API cũ (copy chính xác, không thêm /v1/, không đổi tên)
- ✅ JSON response của API mới = JSON response của API cũ (cùng field name, cùng structure)
- ✅ HTTP Status Code của API mới = HTTP Status Code của API cũ

**3e. Đăng ký DI:**
Cập nhật hoặc tạo `src/POS.Infrastructure/DependencyInjection.cs`
và `src/POS.Application/DependencyInjection.cs`

**3f. Cập nhật tài liệu:**
- Đánh dấu ✅ các endpoint đã convert trong `docs/api-mapping.md`
- Tạo `docs/modules/{module-name}.md` mô tả ngắn gọn module vừa tạo

**QUY TẮC CỨNG — KHÔNG ĐƯỢC VI PHẠM (theo thứ tự ưu tiên):**

🔴 **Ưu tiên #1 — Route bất biến:**
- Đọc route trong code cũ → copy nguyên xi vào controller mới
- Không thêm `/v1/`, không đổi tên segment, không đổi HTTP verb

🔴 **Ưu tiên #2 — Response bất biến:**
- Đọc kỹ code cũ để xác định JSON structure thực tế trả về
- Tạo Response DTO có field names khớp 100% với JSON cũ
- Không bọc thêm bất kỳ wrapper nào (`ApiResponse<T>`, `data: {...}`, v.v.)
- Không thêm/bớt/đổi tên field nào

🟡 **Ưu tiên #3 — Code quality (áp dụng bên trong, không ảnh hưởng API contract):**
- Mọi method trong Service phải là `async Task<>`
- Mọi Request DTO phải có Validator tương ứng
- Logging tại: đầu method (INFO), kết quả (INFO), exception (ERROR)
- Dùng custom exceptions từ `POS.Domain/Common/Exceptions/`
- Không hardcode bất kỳ string nào — dùng Constants hoặc config

**NGHIỆP VỤ:**
- Giữ nguyên 100% logic nghiệp vụ từ code cũ
- Chỉ thay đổi cách implement (async, DI, pattern)
- Nếu gặp logic không rõ ràng, viết comment `// TODO: Xác nhận nghiệp vụ với team`

**SAU KHI XONG:**
Liệt kê tóm tắt:
1. Các files đã tạo
2. Các dependencies cần cài (NuGet packages)
3. Các điểm cần chú ý hoặc confirm với team
```

---

## Ví dụ cho module Payment/Voucher

```
/convert-module với nội dung:

MODULE_NAME: Payment-Voucher

SOURCE_FILES:
- ../POS.Backend/Controllers/VoucherController.cs
- ../POS.Backend/Services/VoucherService.cs
- ../POS.Backend/Services/Interfaces/IVoucherService.cs
- ../POS.Backend/Models/Voucher.cs
- ../POS.Backend/Models/VoucherTransaction.cs
```

---

## Thứ tự convert được khuyến nghị

1. `Common` — đơn giản nhất, ít dependency
2. `Session` — cần thiết cho hầu hết API khác
3. `Payment-Voucher` — voucher nội bộ trước
4. `Payment-GotIt` — sau khi có pattern từ Voucher
5. `Payment-Urbox`
6. `Loyalty`
7. `MasterData`
8. `Offer` — phức tạp nhất, để cuối
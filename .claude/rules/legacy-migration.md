# Rule: Migration từ src/legacy/ (VCM.BLUEPOS)

## 🎯 Context (Khi nào áp dụng)
Áp dụng cho MỌI task "port/migrate chức năng X từ code cũ". Đọc rule này **TRƯỚC** khi mở
`src/legacy/`. Không áp dụng cho công việc greenfield thông thường (không liên quan legacy).

## ✅ DO (Bắt buộc làm)
- Phạm vi & vị trí source cũ: **`src/legacy/`** — solution `VCM.BLUEPOS.sln` (.NET Framework
  4.6.2), gồm `VCM.BLUEPOS` (Web/API), `VCM.BLUEPOS.Business`, `VCM.BLUEPOS.Common`,
  `VCM.BLUEPOS.Data`, `VCM.BLUEPOS.Model`.
- Chỉ dùng `src/legacy/` để Grep/Read đối chiếu logic nghiệp vụ.
- **Đọc `docs/migrations/MIGRATION_MAP.md` trước** khi định vị logic gốc cho bất kỳ task port
  nào (bảng ánh xạ kiến trúc cũ → mới: project/assembly, layering, DI, config, DB/SP,
  cross-cutting concern, và danh sách điểm KHÔNG map 1-1 cần quyết định thủ công) — tránh khảo sát
  lại từ đầu.
- Theo đúng **quy trình port 1 chức năng**, tuần tự 6 bước:
  1. **Định vị** logic gốc trong `src/legacy/` bằng Grep/Explore theo tên chức năng/route/SP —
     đối chiếu `docs/migrations/MIGRATION_MAP.md` mục 3 (bảng ánh xạ) để biết layer đích tương ứng.
  2. **Đọc hiểu nghiệp vụ** (điều kiện, validation, side-effect, external call).
  3. **Thiết kế lại theo chuẩn dự án mới**: DTO ở `POS.Common/Dtos/{Domain}/`, Repository/
     AppService ở `POS.Infrastructure/.../{Domain}/`, Service ở
     `POS.Application/Features/{Domain}/`, Controller ở `POS.Api/Controllers/` — đúng
     "Khuôn thêm 1 nghiệp vụ mới" (xem `.claude/rules/architecture-layers.md`). Vẫn áp dụng
     "Cổng chặn trùng lặp" — kiểm tra `docs/CURRENT_STRUCTURE.md` trước khi tạo
     DTO/Service/Repository mới.
  4. **Trích dẫn nguồn gốc BẮT BUỘC**: mọi method/block logic port sang phải có comment 1 dòng
     ngay phía trên chỉ rõ `file:dòng` gốc, áp dụng cho từng đoạn logic nghiệp vụ có ý nghĩa
     (không cần chú thích từng dòng vụn vặt), ví dụ:
     ```csharp
     // Ported from src/legacy/VCM.BLUEPOS.Business/Services/OrderService.cs:142-168
     ```
  5. **UI (nếu port kèm màn hình)**: theo chuẩn POS.Web hiện hành (MudBlazor 9, `pos-page-header`,
     MudTable `HorizontalScrollbar`, Density Standard, Flat UI Standard...).
  6. Sau khi port xong: cập nhật `docs/CURRENT_STRUCTURE.md` cùng commit, chạy
     `dotnet test tests/POS.ContractTests` phải xanh.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm copy nguyên namespace/folder structure của `VCM.BLUEPOS` sang dự án mới — thiết kế lại theo
  chuẩn `POS.Application`/`POS.Infrastructure`/`POS.Common` hiện hành.
- Cấm port method mà không trích dẫn `file:dòng` gốc trong comment.
- **Cấm sửa/xóa file trong `src/legacy/` dưới bất kỳ lý do gì** (kể cả "dọn code", thêm comment) —
  CHỈ ĐỌC tuyệt đối.
- Cấm thêm project trong `src/legacy/` vào `POS.slnx`.
- Cấm bám theo markup WebForms/ASPX/Razor cũ khi port kèm UI.
- Cấm đổi tên field JSON response hiện hữu khi port (contract 5.000 POS bất biến).

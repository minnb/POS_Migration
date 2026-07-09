# Quy tắc Migration từ src/legacy/ (VCM.BLUEPOS) — BẮT BUỘC

> Áp dụng cho MỌI task "port/migrate chức năng X từ code cũ". Đọc mục này TRƯỚC khi mở
> `src/legacy/`. Không áp dụng cho công việc greenfield thông thường (không liên quan legacy).

## Phạm vi & vị trí

- Source cũ: **`src/legacy/`** — solution `VCM.BLUEPOS.sln` (.NET Framework 4.6.2), gồm
  `VCM.BLUEPOS` (Web/API), `VCM.BLUEPOS.Business`, `VCM.BLUEPOS.Common`, `VCM.BLUEPOS.Data`,
  `VCM.BLUEPOS.Model`.
- **CHỈ ĐỌC — TUYỆT ĐỐI KHÔNG sửa/xóa/format lại file nào trong `src/legacy/`.**
- `src/legacy/` **KHÔNG** được thêm vào `POS.slnx` — không phải project được build/deploy cùng
  solution .NET 10, chỉ để Grep/Read đối chiếu logic nghiệp vụ.
- **Bảng ánh xạ kiến trúc cũ → mới**: `docs/migrations/MIGRATION_MAP.md` — khảo sát đầy đủ
  project/assembly, layering, DI, config, DB/SP, cross-cutting concern của `VCM.BLUEPOS`, bảng
  ánh xạ từng loại thành phần sang layer mới, và danh sách các điểm KHÔNG map 1-1 cần quyết định
  thủ công. **Đọc file này trước** khi định vị logic gốc cho bất kỳ task port nào — tránh khảo sát
  lại từ đầu.

## Quy trình port 1 chức năng

1. **Định vị** logic gốc trong `src/legacy/` bằng Grep/Explore theo tên chức năng/route/SP —
   đối chiếu `docs/migrations/MIGRATION_MAP.md` mục 3 (bảng ánh xạ) để biết layer đích tương ứng.
2. **Đọc hiểu nghiệp vụ** (điều kiện, validation, side-effect, external call) — KHÔNG copy
   nguyên cấu trúc class/namespace/tên biến của code cũ.
3. **Thiết kế lại theo chuẩn dự án mới**: DTO ở `POS.Common/Dtos/{Domain}/`, Repository/AppService
   ở `POS.Infrastructure/.../{Domain}/`, Service ở `POS.Application/Features/{Domain}/`,
   Controller ở `POS.Api/Controllers/` (đúng "Khuôn thêm 1 nghiệp vụ mới" — xem
   `.claude/rules/architecture-layers.md`). Vẫn áp dụng "Cổng chặn trùng lặp" — kiểm tra
   `docs/CURRENT_STRUCTURE.md` trước khi tạo DTO/Service/Repository mới.
4. **Trích dẫn nguồn gốc BẮT BUỘC**: mọi method/block logic port sang phải có comment 1 dòng
   ngay phía trên chỉ rõ `file:dòng` gốc, ví dụ:
   ```csharp
   // Ported from src/legacy/VCM.BLUEPOS.Business/Services/OrderService.cs:142-168
   ```
   Áp dụng cho từng đoạn logic nghiệp vụ có ý nghĩa (không cần chú thích từng dòng vụn vặt).
5. **UI (nếu port kèm màn hình)**: theo chuẩn POS.Web hiện hành (MudBlazor 9, `pos-page-header`,
   MudTable `HorizontalScrollbar`, Density Standard, Flat UI Standard...) — KHÔNG bám theo markup
   WebForms/ASPX/Razor cũ.
6. Sau khi port xong: cập nhật `docs/CURRENT_STRUCTURE.md` cùng commit, chạy
   `dotnet test tests/POS.ContractTests` phải xanh.

## KHÔNG làm

- ❌ Copy nguyên namespace/folder structure của `VCM.BLUEPOS` sang dự án mới.
- ❌ Port method mà không trích dẫn `file:dòng` gốc trong comment.
- ❌ Sửa/xóa file trong `src/legacy/` dưới bất kỳ lý do gì (kể cả "dọn code", thêm comment).
- ❌ Thêm project trong `src/legacy/` vào `POS.slnx`.
- ❌ Đổi tên field JSON response hiện hữu khi port (contract 5.000 POS bất biến).

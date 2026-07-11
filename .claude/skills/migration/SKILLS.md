---
name: migration-legacy-port
description: Template + checklist viết file FEATURE_{Name}_ANALYSIS.md khi port chức năng từ src/legacy/ (VCM.BLUEPOS) sang POS.Web/POS.Api mới. Đọc trước khi bắt đầu port hoặc viết phân tích nghiệp vụ.
---

# Skill: Phân tích & Port chức năng từ src/legacy/ (VCM.BLUEPOS)

> **Áp dụng khi:** chuẩn bị port bất kỳ chức năng nào từ `src/legacy/VCM.BLUEPOS/` sang
> `POS.Web`/`POS.Api` mới. Đọc file này TRƯỚC khi viết file phân tích nghiệp vụ hoặc bắt đầu
> port. Skill này **không thay thế** quy trình 6 bước đã có trong `CLAUDE.md` (mục "Quy tắc
> Migration từ src/legacy/") — nó bổ sung **template + checklist cụ thể** cho artefact "file
> phân tích nghiệp vụ" mà quy trình đó ngầm giả định phải có trước bước "Thiết kế lại".

---

## Quy trình tổng quan (pointer — không lặp lại nội dung)

1. Đọc `CLAUDE.md` mục "Quy tắc Migration từ src/legacy/" — quy trình 6 bước, danh sách
   "KHÔNG làm", yêu cầu trích dẫn `file:dòng` bắt buộc.
2. Đọc `docs/migrations/MIGRATION_MAP.md` mục 3 (bảng ánh xạ kiến trúc cũ→mới) để biết layer
   đích tương ứng, và mục 4 (điểm KHÔNG map 1-1) nếu chức năng chạm phải pattern đặc biệt
   (business logic trong controller, static mutable state, DB access bypass Business/Data...).
3. Nếu port màn hình/chức năng cụ thể (không phải riêng navigation), tra
   `docs/migrations/MENU_INVENTORY.md` để biết Controller/Action/tên menu gốc trước khi định vị
   code — tránh tìm sai controller do tên menu và tên Controller/Action không luôn khớp nhau
   (xem mục 5 của file đó về các trường hợp lệch tên đã phát hiện).
4. Nếu chức năng đủ phức tạp (xem mục dưới) → viết `FEATURE_{Name}_ANALYSIS.md` theo template
   trong skill này trước khi thiết kế lại.
5. Port theo đúng convention dự án mới (xem CLAUDE.md), trích nguồn `file:dòng`.
6. Cập nhật `docs/CURRENT_STRUCTURE.md` + `docs/migrations/STATUS.md`, chạy
   `dotnet test tests/POS.ContractTests` phải xanh.

---

## Tài liệu bổ trợ đã có trong `docs/migrations/`

| File | Nội dung | Dùng khi |
|---|---|---|
| `MIGRATION_MAP.md` | Khảo sát kiến trúc cũ→mới, bảng ánh xạ, điểm không map 1-1 | Bắt đầu bất kỳ task port nào |
| `MENU_INVENTORY.md` | Kiểm kê toàn bộ Controller/Action có `[DisplayName]` (135 dòng — 130 active + 5 disabled) của `VCM.BLUEPOS`, cơ chế menu DB-driven (`dbo.Menu`/`dbo.MenuRole`), điểm bất thường (tên trùng/lệch) | Tra cứu "chức năng X nằm ở Controller/Action nào", hoặc trước khi dựng lại navigation/sidebar cho `POS.Web` |
| `STATUS.md` | Tiến độ phân tích/port theo từng feature | Cập nhật sau mỗi task phân tích hoặc port |
| `FEATURE_{Name}_ANALYSIS.md` (nhiều file) | Phân tích nghiệp vụ chi tiết từng feature | Tham khảo mẫu template, hoặc tra chức năng đã phân tích trước đó |

> **Lưu ý khi port navigation/menu sang `POS.Web` (Blazor)**: cây phân cấp + thứ tự hiển thị
> thật của menu nằm trong dữ liệu bảng `dbo.Menu` (DB), **không có trong source control** — xem
> mục 1 và mục 6 của `MENU_INVENTORY.md`. Không tự suy đoán cây menu từ cách nhóm Controller;
> nếu task yêu cầu dựng lại sidebar đầy đủ, cần xin export dữ liệu bảng `Menu`/`MenuRole` từ DB
> legacy thật trước khi thiết kế. Phân quyền theo `RoleCode` tuỳ ý (`MenuRole`) cũng không map
> 1-1 sang 3 policy cố định hiện có của `POS.Web` (`WebPolicies.StoreAndAbove/OpsAndAbove/
> AdminOnly`) — quyết định mapping thủ công theo từng menu.

---

## Khi nào cần viết `FEATURE_{Name}_ANALYSIS.md`

Viết file phân tích khi chức năng có **nhiều action** hoặc **nhiều business rule/edge case**
cần trace kỹ trước khi thiết kế lại (vd domain có CRUD + import Excel + nhiều SP). **Không bắt
buộc** cho chức năng chỉ 1-2 dòng logic đơn giản, không có rule ẩn — trường hợp đó có thể đọc
hiểu rồi port thẳng theo quy trình 6 bước trong CLAUDE.md.

---

## Template `FEATURE_{Name}_ANALYSIS.md`

> Chốt dựa trên 2 file mẫu đã có: `docs/migrations/FEATURE_Barcode_ANALYSIS.md`,
> `docs/migrations/FEATURE_SetupLoyalty_ANALYSIS.md`. Đặt file mới cùng thư mục
> `docs/migrations/`.

```markdown
# FEATURE_{Name}_ANALYSIS.md — Phân tích chức năng {Name} (Legacy VCM.BLUEPOS)

> **Trạng thái**: Tài liệu phân tích (analysis). **Chưa migrate code nào.** Trace đầy đủ từ
> controller → BLO → Data → SP/EF cho domain "{Name}", trích dẫn `file:dòng` cho mọi rule.
> Tuyệt đối không suy diễn logic không có trong code — phần mơ hồ được liệt kê ở mục cuối để
> hỏi lại.

## 0. Phạm vi đã đọc

Liệt kê đầy đủ file đã đọc trọn vẹn (kèm số dòng), gồm cả file đọc để xác nhận "KHÔNG dùng
trong domain này" nếu có nghi vấn ban đầu.

## 0b. Lệch tên giữa các layer (chỉ thêm mục này nếu phát hiện)

> Khi tên Controller/BLO/Data/Interface không khớp nhau — xác nhận qua constructor injection
> thật (không suy đoán từ tên file/class). Nêu rõ: interface nào thực sự được dùng để DI, class
> nào là dead abstraction (nếu có).

## 1. Sơ đồ luồng (request → response)

Mỗi action: `Action [HTTP method] (file:dòng)` → BLO method (file:dòng) → Data method
(file:dòng) → SP/EF entity/bảng → response. Trích `file:dòng` cho MỌI bước, không tóm tắt
chung chung.

## 2. Business rule / validation / edge case / error handling

Đánh số từng rule, mỗi rule trích `file:dòng`. Bao gồm cả rule ẩn (hardcode, filter ngầm,
side-effect không rõ ràng từ tên method).

## 3. Model / DTO / Entity liên quan

## 4. Database / SP liên quan

Tên bảng, cột, SP dùng trong domain. Nếu SP không tìm thấy định nghĩa trong script cũ — ghi rõ
"không tìm thấy định nghĩa" thay vì suy đoán, và liệt kê vào mục Câu hỏi mở.

## 5. Config / hằng số / magic number

## 6. Câu hỏi cho người phụ trách

Mọi điểm mơ hồ, SP thiếu định nghĩa, business rule không rõ ý đồ — liệt kê ở đây, KHÔNG tự
suy diễn để lấp khoảng trống.
```

---

## Checklist khi hoàn thành file phân tích

1. Đã trích `file:dòng` cho mọi rule quan trọng (không có rule nào mô tả chung chung không dẫn
   nguồn).
2. Mọi điểm mơ hồ được liệt kê ở mục "Câu hỏi cho người phụ trách" — không tự suy diễn.
3. Đã cập nhật `docs/migrations/STATUS.md` — chuyển trạng thái feature sang
   **"Đã phân tích — chưa port"**.

## Checklist khi port xong (nhắc lại ngắn gọn — chi tiết xem CLAUDE.md)

1. Mọi method/block logic port có comment `// Ported from src/legacy/...:dòng`.
2. `docs/CURRENT_STRUCTURE.md` đã cập nhật cùng commit.
3. `dotnet test tests/POS.ContractTests` xanh.
4. `docs/migrations/STATUS.md` — chuyển trạng thái feature sang **"Đã port"**.

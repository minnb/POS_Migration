---
description: Rà soát logic code hoặc action plan MỚI THỰC HIỆN trong phiên chat hiện tại (chỉ đọc, KHÔNG tự ý sửa), tìm lỗi logic, hổng bảo mật, rủi ro hiệu năng, và vi phạm CLAUDE.md / Clean Architecture.
allowed-tools: Read, Grep, Glob, Bash(git status --short), Bash(git diff HEAD), Bash(git --no-pager diff), Bash(git --no-pager diff --staged)
argument-hint: [optional focus area]
---

# Systematic Code Review

> **Chế độ CHỈ ĐỌC (read-only) tuyệt đối.** Lượt chạy lệnh này KHÔNG được Edit/Write/sửa bất kỳ file
> nào, không chạy lệnh làm thay đổi repo. Chỉ báo cáo. Việc sửa code chỉ diễn ra ở lượt sau, khi
> người dùng xác nhận rõ ràng.

## Cách dùng

```
/task-review
/task-review chỉ tập trung POS.Web
/task-review chỉ các file trong src/POS.Infrastructure
```

## 1. Context & Diff

Dưới đây là trạng thái các file đang thay đổi (Chỉ bao gồm các thay đổi mới nhất trong phiên này):

!`git status --short`
!`git diff HEAD`

Mục tiêu ưu tiên (nếu có do User truyền vào): $ARGUMENTS

**Xác định phạm vi:**
1. Ưu tiên các file đã tạo/sửa **trong phiên chat hiện tại** (task vừa làm).
2. Nếu `git diff HEAD` chưa đủ (có thay đổi đã staged/chưa staged cần tách bạch) → đối chiếu thêm
   `git --no-pager diff` và `git --no-pager diff --staged`.
3. User nêu phạm vi (vd "chỉ POS.Web") → chỉ rà soát vùng đó.
4. Không có thay đổi nào (diff rỗng và phiên chưa sửa file) → báo **"Không có thay đổi để review"**
   và DỪNG. Không tự đi tìm file cũ để review thay thế.

## 2. Review Checklist (Chỉ Đọc & Báo Cáo - KHÔNG tự sửa file)

Đóng vai một Senior QA/Tech Lead.

**RÀO CHẮN NGỮ CẢNH (Rất quan trọng):** Bạn CHỈ ĐƯỢC PHÉP phân tích và review các logic code hoặc
action plan MỚI ĐƯỢC TẠO RA HOẶC SỬA ĐỔI TRONG PHIÊN CHAT NÀY (thể hiện rõ ở phần `git diff` bên
trên). TUYỆT ĐỐI KHÔNG review lan man sang các file cũ, logic cũ, hoặc các phần code không bị thay
đổi.

Hãy báo cáo các vấn đề theo các hạng mục sau dựa trên phần code mới:

- **Logic & Edge Cases:** Code MỚI có bắt try/catch đầy đủ không? Có xử lý null/rỗng/boundary
  không? Vòng lặp có nguy cơ infinite/busy-loop không? Có lỗi chia cho 0, race condition, sai luồng
  nghiệp vụ không?
- **Security:** Code MỚI có hardcode credential/secret không (phải theo `enc:`/`POS_SECRET_KEY`)?
  Truy vấn SQL mới có bị SQL Injection không (phải parameterized)? Dữ liệu nhập từ ngoài có được
  validate không? Có path traversal khi đọc/ghi file theo input người dùng không?
- **Performance:** Truy vấn/vòng lặp MỚI có nguy cơ N+1 query, `.ToList()` không giới hạn, thiếu
  cache Redis theo chuẩn, I/O đồng bộ trên hot path, thiếu `CancellationToken`, hay block Blazor
  circuit (POS.Web) không?
- **Architecture & Convention:** Code MỚI có tuân thủ Clean Architecture .NET 10 và
  `CLAUDE.md` + `.claude/rules/` không? Chỉ đối chiếu những luật **liên quan** tới vùng code vừa
  sửa, không liệt kê máy móc toàn bộ:
  - Dependency flow `POS.Api → POS.Application → POS.Infrastructure → POS.Common`; Controller inject
    Application interface, KHÔNG inject Infrastructure trực tiếp; AppService 3 lớp cho external HTTP.
  - Service/repository mới đã đăng ký DI chưa (`DependencyInjection.cs`).
  - `System.Text.Json` bị CẤM → phải `Newtonsoft.Json`; CẤM đổi tên field JSON của DTO response
    hiện hữu (contract 5.000 máy POS).
  - Đụng DTO response → đã có contract test khóa field trong `tests/POS.ContractTests` chưa.
  - Đụng `.razor` (POS.Web) → LUẬT THÉP UI: `MudTable HorizontalScrollbar`, `div.pos-page-header`,
    `@rendermode InteractiveServer`, `@attribute [Authorize]`, `MudAutocomplete` an toàn circuit,
    audit log cho CRUD, Density/Flat UI standard.
  - Đụng SP/SQL → naming `usp_{Domain}_{Action}`, bracket-quote reserved keyword
    (`[No]`/`[Status]`/`[Counter]`...), `SET NOCOUNT ON`/`SET XACT_ABORT ON`/`THROW`, đăng ký
    `docs/sql/manifest.json` với `order` đúng phụ thuộc.
  - Đụng worker → đặt code ở `POS.Infrastructure/Workers/`, dùng `IServiceScopeFactory`, vòng lặp
    không được chết, đăng ký `AddHostedService`.
  - Port từ `src/legacy/` → có comment trích dẫn `file:dòng` gốc chưa.

**Quy tắc báo cáo (bắt buộc, theo QUY TẮC GIAO TIẾP trong CLAUDE.md):**
- Trước khi báo xong — chỉ ra kết quả cụ thể chứng minh điều đó (trích `file:dòng`, đoạn diff, hoặc
  output lệnh thực tế).
- Chỉ báo những việc có thể cho thấy bằng chứng. Không phát hiện vấn đề → nói rõ "Không phát hiện
  vấn đề nghiêm trọng", KHÔNG bịa lỗi cho đủ mục.
- Chưa verify được → hãy nói thẳng là CHƯA VERIFY ĐƯỢC, thay vì chẩn đoán mò.

## 3. Output Format

Xuất báo cáo review bằng tiếng Việt với cấu trúc:

1. 📋 **Tóm tắt file đã thay đổi** — `<path>: <thay đổi làm gì>` (để User nắm lại bối cảnh).
2. 🛡️ **Tóm tắt nhanh** — An toàn / Có rủi ro cho các thay đổi vừa thực hiện.
3. 🔴 **Critical** — Lỗi logic, Crash, Bảo mật. Cần sửa ngay.
4. 🟡 **Warning** — Vi phạm kiến trúc/convention, edge case chưa xử lý, rủi ro hiệu năng. Nên sửa.
5. 🟢 **Nice-to-have** — Gợi ý cải thiện khả năng đọc/tái dùng. Không bắt buộc.
6. ✅ **Đối chiếu CLAUDE.md** — `<luật đã kiểm>` → Đạt / Vi phạm (nêu rõ).
7. ❓ **Chưa verify được** — Liệt kê rõ những điểm không thể kiểm chứng trong phiên (thiếu DB, môi
   trường, quyền truy cập...) và cần ai/việc gì để xác nhận.
8. 👉 **Bước tiếp theo** — Liệt kê các sửa đề xuất, CHỜ xác nhận của người dùng rồi mới sửa.

Mỗi mục ghi theo dạng: `file:dòng` — mô tả vấn đề — bằng chứng — đề xuất sửa (mô tả, KHÔNG tự sửa file).

Kết thúc: **KHÔNG chỉnh sửa bất kỳ file nào.** Hỏi User muốn áp dụng fix cho mục nào (nếu có).

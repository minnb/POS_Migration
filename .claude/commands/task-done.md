# /task-done — Cập nhật tài liệu sau khi hoàn thành task

Dùng lệnh này sau khi hoàn thành **bất kỳ task nào** trong solution POS —
feature mới, bug fix, pattern mới, hay refactor.
Command tự động cập nhật SKILLS, CURRENT_STRUCTURE, WEB_STATUS và CHANGELOG.

---

## Cách dùng

```
/task-done
```

Hoặc cung cấp mô tả ngay:
```
/task-done "Thêm TransactionPage cho Store section — MudDataGrid + date filter"
```

---

## Quy trình Claude thực hiện

### Bước 1 — Phát hiện thay đổi

Quét các file đã thay đổi trong session:
- Liệt kê file `.cs`, `.razor`, `.json`, `.md` mới hoặc đã sửa
- Xác định project bị ảnh hưởng:

| Thư mục | Layer |
|---|---|
| `src/POS.Api/` | `api` |
| `src/POS.Web/` | `web` |
| `src/POS.Infrastructure/` | `infrastructure` |
| `src/POS.Common/` | `common` |

- Xác định loại thay đổi:
  - Thêm file mới (Controller, Service, Repository, Component, Page, DTO)
  - Thêm pattern/standard mới (cách xử lý, cách inject, cách viết)
  - Fix bug
  - Refactor

Nếu không tự phát hiện được → hỏi user:
> "Task vừa hoàn thành là gì? (mô tả ngắn 1 dòng)"

---

### Bước 2 — Cập nhật SKILLS nếu có pattern mới

**Chỉ cập nhật** nếu task tạo ra pattern/standard **mới** chưa có trong skill file.
**Không cập nhật** nếu chỉ là feature thường theo pattern đã có sẵn.

Xác định skill file theo layer:

| Layer bị ảnh hưởng | Skill file cần cập nhật |
|---|---|
| POS.Api, POS.Infrastructure (không phải Redis) | `.claude/skills/api/SKILLS.md` |
| Redis, Cache | `.claude/skills/cache/SKILLS.md` |
| POS.Web, Blazor | `.claude/skills/web/SKILLS.md` |

Nếu có pattern mới → thêm vào **cuối section phù hợp** trong skill file:

```markdown
### Pattern: {tên pattern ngắn gọn}
> Áp dụng khi: {điều kiện kích hoạt pattern này}

```csharp
// code mẫu ngắn gọn — đủ để hiểu, không cần đầy đủ
```

> Ví dụ thực tế: `{đường dẫn file vừa tạo}`
```

**Quy tắc khi thêm pattern:**
- Tối đa 20 dòng cho mỗi pattern — tập trung vào "tại sao", không phải "cái gì"
- Nếu pattern fix bug hay sai lầm thường gặp → ghi rõ anti-pattern tương ứng
- Không duplicate pattern đã có — kiểm tra trước khi thêm

---

### Bước 3 — Cập nhật CURRENT_STRUCTURE.md nếu có file mới

**Chỉ chạy** nếu có file mới được tạo (Controller, Service, Repository,
Interface, Component, Page, DTO).

Đọc `docs/CURRENT_STRUCTURE.md` và cập nhật đúng mục:

- **Mục A** (cây thư mục) → thêm file mới vào đúng vị trí
- **Mục B** (Interface & Implementation) → thêm interface/class mới
- **Mục C** (DI Registration) → nếu có đăng ký DI mới
- **Mục D/E** (Repository/Service methods) → nếu có method mới

> Không viết lại cả file — chỉ thêm/sửa đúng dòng liên quan.

---

### Bước 4 — Cập nhật WEB_STATUS.md nếu task thuộc POS.Web

**Chỉ chạy** nếu có file thay đổi trong `src/POS.Web/`.

Đọc `docs/WEB_STATUS.md` và cập nhật:
- Hạng mục tương ứng: đổi ❌ hoặc ⚠️ thành ✅
- Nếu là feature mới chưa có trong bảng → thêm dòng mới vào đúng section
- Cập nhật dòng `> Cập nhật:` ở đầu file với ngày hiện tại

---

### Bước 5 — Ghi vào CHANGELOG.md

Đọc `docs/CHANGELOG.md`. Nếu chưa có → tạo mới với header:

```markdown
# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.

---
```

Thêm entry mới vào **đầu file** (mới nhất ở trên cùng, sau header):

```markdown
## [{ngày giờ}] {tên task ngắn gọn}

**Layer:** POS.Api | POS.Web | POS.Infrastructure | POS.Common
**Loại:** Feature | Pattern mới | Bug fix | Refactor

**Thay đổi:**
- `{file 1}`: {mô tả 1 dòng}
- `{file 2}`: {mô tả 1 dòng}

**Pattern mới (nếu có):** {tên pattern} → đã cập nhật `.claude/skills/{layer}/SKILLS.md`

**Lưu ý cho session sau:** {1-2 câu về điều cần nhớ khi làm task tương tự}

---
```

---

### Bước 6 — Tóm tắt ra màn hình

In kết quả ngắn gọn:

```
✅ task-done hoàn thành
─────────────────────────────
Task: {tên task}
Files thay đổi: {số lượng}

Đã cập nhật:
• SKILLS.md ({layer}): {có/không} {nếu có: → tên pattern}
• CURRENT_STRUCTURE.md: {có/không}
• WEB_STATUS.md: {có/không}
• CHANGELOG.md: ✅ luôn cập nhật

Session sau nhớ: {1 câu từ "Lưu ý cho session sau"}
```

---

## Lưu ý quan trọng

- CHANGELOG.md **luôn** được cập nhật — dù không có pattern mới hay file mới
- SKILLS.md **chỉ** cập nhật khi có pattern thực sự mới — không thêm vì task "có vẻ quan trọng"
- Không commit hay push — chỉ cập nhật file tài liệu local
- Nếu `docs/CURRENT_STRUCTURE.md` chưa tồn tại → bỏ qua bước 3, ghi chú vào CHANGELOG

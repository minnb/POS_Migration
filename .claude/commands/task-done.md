# /task-done — Cập nhật tài liệu sau khi hoàn thành task

Dùng lệnh này sau khi hoàn thành **bất kỳ task nào** trong solution POS —
feature mới, bug fix, pattern mới, hay refactor.
Command tự động cập nhật SKILLS, CURRENT_STRUCTURE, WEB_STATUS, CHANGELOG **và đồng bộ appsettings UAT/Production**.

---

## Cách dùng

```
/task-done
```

Hoặc cung cấp mô tả ngay:
```
/task-done "Thêm TransactionPage cho Store section — MudTable + date filter"
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
  - Thêm file mới (Controller, Service, Repository, Component, Page, DTO, Helper)
  - Thêm pattern/standard mới (cách xử lý, cách inject, cách viết)
  - Fix bug
  - Refactor

Nếu không tự phát hiện được → hỏi user:
> "Task vừa hoàn thành là gì? (mô tả ngắn 1 dòng)"

---

### Bước 2 — Đồng bộ appsettings UAT/Production (khi appsettings.json thay đổi)

**Chỉ chạy** nếu `appsettings.json` của bất kỳ project nào bị sửa trong session.

**Project cần kiểm tra:**

| Project | DEV base | Môi trường cần sync |
|---|---|---|
| `POS.Api` | `src/POS.Api/appsettings.json` | `appsettings.UAT.json` + `appsettings.Production.json` |
| `POS.Worker` | `src/POS.Worker/appsettings.json` | `appsettings.UAT.json` + `appsettings.Production.json` |
| `POS.Web` | `src/POS.Web/appsettings.json` | `appsettings.UAT.json` + `appsettings.Production.json` |

**Quy trình:**
1. Đọc `appsettings.json` và cả 2 file UAT/Production của cùng project
2. So sánh: section/key nào có trong DEV nhưng **thiếu** trong UAT hoặc Production?
3. Với mỗi section/key thiếu → thêm vào file môi trường theo quy tắc sau:

**Quy tắc thêm giá trị theo loại section:**

| Section | Loại | Hành động |
|---|---|---|
| `MasterDataSync`, `*Options*`, tuning số (timeout, batch size, parallelism) | **Tuning** | Copy y hệt giá trị DEV — default an toàn cho mọi môi trường |
| `ConnectionStrings`, `Redis`, `RabbitMQ`, `Elasticsearch` | **Hạ tầng** | Thêm key với placeholder `"<UAT_...>"` / `"<PROD_...">`|
| `AppSettings` (paths, URLs, server IPs) | **Hạ tầng** | Thêm placeholder hoặc `""` (rỗng) |
| `Logging` | **Mixed** | Level → copy; `FileLogDirectory` → `"/app/logs"` |
| `Security` (POS.Web) | **Hạ tầng** | Thêm key với giá trị production-safe mặc định (vd `RequireHttps: true`) |

**Ví dụ — thêm section `MasterDataSync` vào UAT:**
```json
"MasterDataSync": {
  "SqlCommandTimeoutSeconds": 600,
  "KeepZipDays": 2,
  "DateInZipName": true,
  "ZipCompressionLevel": "Fastest",
  "BatchSizePerFile": 10000,
  "MaxParallelTables": 4
}
```
> (copy y DEV vì đây là tuning — không có credential, không phụ thuộc môi trường)

**Ví dụ — thêm key hạ tầng mới vào UAT:**
```json
"SomeNewService": {
  "Host": "<UAT_SOME_SERVICE_HOST>",
  "ApiKey": "<UAT_SOME_SERVICE_APIKEY>"
}
```

**Lưu ý quan trọng:**
- **KHÔNG** đồng bộ key đã có rồi trong UAT/Production (tránh ghi đè giá trị prod thật)
- **KHÔNG** copy `ConnectionStrings` hay credentials từ DEV sang môi trường khác — chỉ thêm placeholder
- Nếu section đã có trong UAT/Production nhưng **thiếu key con** mới → thêm key con đó vào đúng section
- Sau khi thêm placeholder → ghi chú vào CHANGELOG để ops team biết cần điền giá trị thật

**Tóm tắt ra màn hình:**
```
• appsettings sync:
  POS.Api → UAT ✅ (thêm "MasterDataSync") | Production ✅ (thêm "MasterDataSync")
  POS.Worker → không thay đổi
```

---

### Bước 3 — Cập nhật SKILLS nếu có pattern mới

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

### Bước 4 — Cập nhật CURRENT_STRUCTURE.md nếu có file mới

**Chỉ chạy** nếu có file mới được tạo (Controller, Service, Repository,
Interface, Component, Page, DTO, Helper).

Đọc `docs/CURRENT_STRUCTURE.md` và cập nhật đúng mục:

- **Mục A** (cây thư mục) → thêm file mới vào đúng vị trí
- **Mục B** (Interface & Implementation) → thêm interface/class mới
- **Mục C** (DI Registration) → nếu có đăng ký DI mới
- **Mục D/E** (Repository/Service methods) → nếu có method mới
- **Mục F** (DTOs & Models có sẵn) → nếu có **DTO/model mới**: ghi tên class + các **field/property chính** (schema) + project chứa nó
- **Mục G** (Helpers `POS.Common/Helpers`) → nếu có **helper mới**, hoặc thêm method vào helper đã có: ghi **chữ ký method**

> **Chỉ ghi schema/chữ ký — KHÔNG chép nguyên code.** Mục đích là "bản đồ để tái dùng", không phải bản sao source.
> Không viết lại cả file — chỉ thêm/sửa đúng dòng liên quan. Nếu mục (F/G) chưa tồn tại trong file → tạo mục đó rồi thêm.

---

### Bước 5 — Cập nhật WEB_STATUS.md nếu task thuộc POS.Web

**Chỉ chạy** nếu có file thay đổi trong `src/POS.Web/`.

Đọc `docs/WEB_STATUS.md` và cập nhật:
- Hạng mục tương ứng: đổi ❌ hoặc ⚠️ thành ✅
- Nếu là feature mới chưa có trong bảng → thêm dòng mới vào đúng section
- Cập nhật dòng `> Cập nhật:` ở đầu file với ngày hiện tại

---

### Bước 6 — Ghi vào CHANGELOG.md

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

### Bước 7 — Tóm tắt ra màn hình

In kết quả ngắn gọn:

```
✅ task-done hoàn thành
─────────────────────────────
Task: {tên task}
Files thay đổi: {số lượng}

Đã cập nhật:
• appsettings sync: POS.Api UAT ✅ Production ✅ | (hoặc: không có thay đổi appsettings)
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
- appsettings sync: **chỉ THÊM key mới**, KHÔNG ghi đè key đã có trong UAT/Production — tránh mất giá trị thật
- Không commit hay push — chỉ cập nhật file tài liệu local
- Nếu `docs/CURRENT_STRUCTURE.md` chưa tồn tại → bỏ qua bước 4, ghi chú vào CHANGELOG

# /refactor-skills — Dọn dẹp/refactor định kỳ các file skill & rule

Dùng lệnh này định kỳ (vd mỗi vài tuần, hoặc khi thấy Claude đọc chậm/lạc hướng do file quá dài)
để rà soát toàn bộ `.claude/rules/*.md`, `.claude/skills/**/*.md`, `.claude/commands/*.md` và
`CLAUDE.md` — phát hiện file đã phình to, nội dung lịch sử/rollout lỗi thời chiếm chỗ, và nội
dung trùng lặp giữa các file — rồi đề xuất (và khi được xác nhận, thực hiện) tách/gộp/rút gọn.

> **Lý do cần lệnh này**: `docs/CURRENT_STRUCTURE.md` và các file `.claude/rules/`,
> `.claude/skills/**/SKILLS.md` là nguồn sự thật bắt buộc đọc trước khi code (xem `CLAUDE.md`
> mục "MỤC LỤC ĐIỀU PHỐI"). Các skill/rule này liên tục được các task khác (`/task-done`,
> `/blazor-ui`...) **nối thêm** pattern mới vào cuối section — theo thời gian file phình to
> (vd `.claude/skills/web/SKILLS.md` ~1300 dòng, `.claude/rules/mudblazor-flat-ui.md` ~510 dòng
> phần lớn là log quyết định theo ngày). File càng dài, Claude càng dễ đọc sót quy tắc hoặc tạo
> trùng lặp — đúng thứ mà "Cổng chặn trùng lặp" trong `CLAUDE.md` đang cố ngăn.

---

## Cách dùng

```
/refactor-skills
```
→ Audit toàn bộ, chỉ in ra báo cáo + đề xuất, **KHÔNG tự sửa file**.

```
/refactor-skills .claude/rules/mudblazor-flat-ui.md
```
→ Audit + đề xuất chi tiết cho đúng 1 file. Vẫn chờ xác nhận trước khi sửa.

```
/refactor-skills apply .claude/rules/mudblazor-flat-ui.md
```
→ Audit file đó rồi **thực hiện luôn** đề xuất (vẫn in plan trước khi Edit, chỉ bỏ bước chờ user gõ "đồng ý").

---

## Nguyên tắc bất biến — BẮT BUỘC tuân thủ khi refactor doc

1. **KHÔNG BAO GIỜ xóa nội dung quy tắc đang có hiệu lực.** Chỉ được phép:
   - (a) **Di chuyển** nguyên văn sang file khác (tách file) — không diễn giải lại, không tóm tắt.
   - (b) **Rút gọn phần LỊCH SỬ đã lỗi thời** — chỉ áp dụng cho đoạn mà chính file đó đã ghi nhận
     rõ ràng là "đã lỗi thời"/"đã thay thế"/"KHÔNG còn áp dụng"/"rollback" (vd mục "Đã cân nhắc và
     loại bỏ" của `mudblazor-flat-ui.md`) — thay bằng 1-2 dòng tóm tắt + link sang file lịch sử lưu
     bản đầy đủ. **KHÔNG** rút gọn quy tắc đang active dù nó dài.
   - (c) **Gộp nội dung trùng lặp** giữa ≥2 file thành 1 nguồn sự thật + câu tham chiếu ở nơi còn lại.
2. **Mọi thay đổi cấu trúc PHẢI cập nhật lại toàn bộ nơi trỏ tới, TRONG CÙNG lượt sửa**:
   router table `CLAUDE.md` ("MỤC LỤC ĐIỀU PHỐI"), bảng "Skill con" trong `SKILLS.md` liên quan,
   mọi mục "xem thêm"/"chi tiết đầy đủ" ở file khác trỏ tới file/section vừa đổi.
3. **Không tự động áp dụng thay đổi** (tách file, rút gọn lịch sử, gộp) mà chưa cho user xem plan —
   trừ khi chạy với `apply <file>` tường minh.
4. **Tách file mới phải theo đúng tiền lệ cấu trúc đã có trong dự án** — không phát minh convention
   thư mục mới. Tiền lệ tham khảo:
   - `.claude/skills/web/SKILLS.md` (index) → các file chủ đề hẹp (`filter-store.md`, `datatable.md`,
     `charts.md`, `reports.md`, `component-patterns.md`, `form-input.md`...). **LUẬT nền tảng KHÔNG
     nằm ở skill** — đã gộp về `.claude/rules/blazor-web-app.md` (§17) từ 2026-07-13 (lớp numbered
     `01–04` cũ đã xóa).
   - `.claude/rules/` — mỗi file 1 domain lớn (`architecture-layers.md`, `backend-api-rules.md`,
     `blazor-web-app.md`, `mudblazor-flat-ui.md`, `legacy-migration.md`, `masterdata-sync.md`,
     `caching-standards.md`, `database-standards.md`, `worker-standards.md`, `logging-standards.md`,
     `unit-testing-standards.md`)

   > **Nguyên tắc phân định Rules ↔ Skills (chốt 2026-07-13):** Rules (`.claude/rules/`) = tiêu
   > chuẩn/ràng buộc bất biến (WHAT/WHY — naming, TTL, layer, security, "BẮT BUỘC/CẤM"). Skills
   > (`.claude/skills/`) = hướng dẫn thực thi (HOW — template, code mẫu, các bước). Mỗi mẩu nội dung
   > chỉ ở 1 nơi; skill trỏ ngược về rule thay vì lặp lại. KHÔNG nhúng khối rule mới vào skill.
5. **Không refactor file chưa vượt ngưỡng** dù "trông dài dòng" — tránh over-engineering. Chỉ hành
   động khi có số liệu cụ thể vượt ngưỡng ở Bước 1.
6. Không tự `git commit`/`git push` — để user tự review diff trước khi commit.

---

## Quy trình Claude thực hiện

### Bước 1 — Quét & đo kích thước

Liệt kê toàn bộ file thuộc phạm vi: `.claude/rules/*.md`, `.claude/skills/**/*.md`,
`.claude/commands/*.md`, `CLAUDE.md`. Với mỗi file, lấy:
- Số dòng (`wc -l`)
- Ngày sửa gần nhất (`git log -1 --format=%cd -- <file>`)
- Đã từng bị tách ra từ file khác chưa (biết qua tên file đánh số `0N-*.md` hoặc bảng "Skill con"
  trong file mẹ)

Phân loại theo ngưỡng (áp dụng riêng theo loại file — `.claude/commands/*.md` vốn dài vì có
template code mẫu, ngưỡng cao hơn skill/rule thuần văn bản quy tắc):

| Loại file | 🟢 OK | 🟡 Theo dõi | 🔴 Cần refactor |
|---|---|---|---|
| `.claude/rules/*.md`, `.claude/skills/**/*.md` (trừ `SKILLS.md` gốc của domain đã tách) | < 300 dòng | 300–500 dòng | > 500 dòng |
| `.claude/skills/{domain}/SKILLS.md` (file mục lục gốc, đã có bảng "Skill con") | < 500 dòng | 500–800 dòng | > 800 dòng |
| `.claude/commands/*.md` | < 200 dòng | 200–350 dòng | > 350 dòng |
| `CLAUDE.md` (root) | < 250 dòng | 250–400 dòng | > 400 dòng |

Chỉ in bảng 🟡/🔴 ra màn hình ở bước này — chưa đọc nội dung chi tiết.

---

### Bước 2 — Phân tích nội dung từng file 🔴 (và 🟡 nếu user chỉ định cụ thể file đó)

Đọc toàn bộ file, chia thành section theo heading `##`/`###`, gắn nhãn từng section:

- **[QUY TẮC]** — quy tắc/pattern/checklist đang áp dụng, cần cho AI tương lai tra cứu trước khi
  code → **GIỮ NGUYÊN VỊ TRÍ** trừ khi là ứng viên tách file ở Bước 3.
- **[LỊCH SỬ]** — mô tả 1 quyết định gắn ngày tháng cụ thể, đặc biệt đoạn mà chính file đã tự ghi
  nhận là lỗi thời (heading kiểu "Đã cân nhắc và loại bỏ", "Trạng thái rollout", "Lịch sử quyết
  định", hoặc câu trong nội dung "đã lỗi thời"/"không còn áp dụng"/"thay cho bản v...") → ứng viên
  rút gọn/tách sang file lịch sử riêng.
- **[TRÙNG LẶP]** — nội dung có ý nghĩa giống/trùng với section ở file khác trong phạm vi
  `.claude/rules/` + `.claude/skills/`. Xác định bằng Grep heading tương tự + đọc đối chiếu nội
  dung (không chỉ khớp từ khóa) → ứng viên gộp về 1 nguồn.

Với file có bảng "Trạng thái rollout"/nhật ký theo ngày dài dần theo thời gian (mẫu
`mudblazor-flat-ui.md`, `masterdata-sync.md`): coi toàn bộ các mục ghi log đã hoàn tất (không còn
việc tồn đọng, không phải "TODO còn lại") là **[LỊCH SỬ]** — đây là nhóm mang lại hiệu quả rút gọn
lớn nhất vì tăng dần vô hạn theo mỗi đợt cập nhật.

---

### Bước 3 — Đề xuất tách/gộp/rút gọn (KHÔNG tự sửa ngay, trừ khi chạy với `apply`)

In bảng đề xuất:

```
## Đề xuất refactor — {tên file}  ({số dòng hiện tại} dòng)

| # | Hành động | Nội dung (section) | Đích | Ước tính giảm |
|---|---|---|---|---|
| 1 | Tách sang file lịch sử | "Đã cân nhắc và loại bỏ" + "Trạng thái rollout" | `.claude/rules/{ten}.history.md` (mới) | -320 dòng |
| 2 | Gộp trùng lặp | "Filter panel button group" (trùng §7 blazor-web-app.md) | Giữ 1 bản ở rule chính, {file kia} thay bằng tham chiếu | -18 dòng |
| 3 | Rút gọn tại chỗ | Đoạn log "Cập nhật 2026-07-06..." dài 40 dòng | Tóm 3 dòng + link file lịch sử | -35 dòng |

Sau refactor ước tính: {N} dòng (từ {M} dòng) — vẫn giữ 100% quy tắc đang hiệu lực.
```

Nếu chạy `/refactor-skills` (không tham số) → dừng ở đây, hỏi user muốn áp dụng cho file nào.
Nếu chạy `/refactor-skills apply <file>` → tiếp tục Bước 4 ngay cho đúng file đó.

---

### Bước 4 — Thực thi (sau khi user xác nhận, hoặc chạy với `apply`)

- **Tách file lịch sử**: tạo `.claude/rules/{ten}.history.md` hoặc `.claude/skills/{domain}/{ten}.history.md`
  (cùng cấp file gốc). Copy nguyên văn từng mục [LỊCH SỬ] đã chọn — giữ nguyên câu chữ/ngày tháng,
  không viết lại. Ở file gốc, thay đoạn đó bằng 1-2 dòng: tóm tắt trạng thái hiện tại + `> Lịch sử
  đầy đủ: {đường dẫn file history}`.
- **Tách section [QUY TẮC] quá dài thành sub-skill mới** (chỉ khi rơi vào 🔴 do 1 section cụ thể
  quá lớn, không phải do tích lũy lịch sử): tạo file mới cùng thư mục domain, thêm dòng vào bảng
  "Skill con"/mục lục của file mẹ trỏ tới file mới — theo đúng mẫu bảng đã có trong
  `.claude/skills/web/SKILLS.md`.
- **Gộp trùng lặp**: xác định bản đầy đủ/mới nhất, giữ lại đúng 1 nơi; nơi còn lại thay bằng câu
  tham chiếu 1 dòng kiểu đã dùng sẵn trong dự án (vd `"> Chi tiết đầy đủ: {đường dẫn} — nguồn sự
  thật duy nhất"`).
- **Cập nhật NGAY trong cùng lượt** (bắt buộc, không tách sang task sau):
  1. `CLAUDE.md` — bảng "MỤC LỤC ĐIỀU PHỐI" nếu đường dẫn/mô tả đổi.
  2. Bảng "Skill con"/mục lục nội bộ của `SKILLS.md` domain liên quan (nếu có tách sub-skill mới).
  3. Grep toàn repo (`.claude/**/*.md`, `docs/**/*.md`, root `CLAUDE.md`) theo đúng tên file cũ để
     tìm mọi nơi khác đang trỏ tới — sửa lại đường dẫn/số mục nếu section bị dời.

---

### Bước 5 — Kiểm tra tham chiếu chéo không bị gãy

Sau khi sửa xong:
1. Grep toàn bộ `*.md` trong `.claude/` và `docs/` tìm chuỗi đường dẫn `.claude/rules/` và
   `.claude/skills/` — đối chiếu từng match với cấu trúc file thực tế sau refactor, đảm bảo không
   còn trỏ tới file đã xóa/đổi tên hoặc mục (`§N`, "mục N") đã dời số.
2. Kiểm tra riêng `CLAUDE.md` — mọi hàng trong bảng "MỤC LỤC ĐIỀU PHỐI" vẫn trỏ đúng file tồn tại.
3. Nếu phát hiện tham chiếu gãy → sửa ngay, liệt kê vào báo cáo cuối.

---

### Bước 6 — Báo cáo tóm tắt

```
✅ /refactor-skills hoàn thành
─────────────────────────────
Phạm vi quét: {N} file (.claude/rules, .claude/skills, .claude/commands, CLAUDE.md)
🔴 Cần refactor: {danh sách file + số dòng}
🟡 Theo dõi (chưa hành động): {danh sách file + số dòng}

Đã áp dụng cho: {file đã refactor, hoặc "chưa áp dụng — chờ xác nhận"}
• {file gốc}: {M} dòng → {N} dòng (-{M-N})
  - Tách lịch sử → {file}.history.md ({K} dòng)
  - Gộp trùng lặp với {file khác}
Đã cập nhật tham chiếu:
• CLAUDE.md router table: {có/không thay đổi}
• {SKILLS.md domain}: {có/không thay đổi bảng Skill con}
• Tham chiếu chéo đã sửa: {N} chỗ (hoặc "không có tham chiếu gãy")

Chưa xử lý (còn 🔴 chưa refactor, cần chạy lại `apply` riêng): {danh sách hoặc "không có"}
```

---

## Lưu ý quan trọng

- Đây là thao tác trên file `.md` thuần — **không cần** `dotnet build`/`dotnet test`. Nhưng sau khi
  tách/gộp, các slash command khác vẫn append pattern mới vào **đúng section đã tồn tại theo tên
  heading** (vd `/task-done` Bước 3 tìm heading section trong `SKILLS.md` theo layer) — nếu đổi tên
  heading khi refactor, phải kiểm tra các command khác (`task-done.md`) có đang tham chiếu heading
  đó theo tên cứng hay không.
- Giữ nguyên 100% nội dung nghiệp vụ khi tách — chỉ đổi **vị trí**, KHÔNG diễn giải lại/tóm tắt quy
  tắc đang hiệu lực. Chỉ được tóm tắt phần đã xác nhận là [LỊCH SỬ] lỗi thời.
- File lịch sử tách ra (`*.history.md`) vẫn là tài liệu tham chiếu hợp lệ — không xóa, không phải
  "dọn rác". Không tự ý xóa file `.history.md` cũ ở lần refactor sau.
- Không commit/push tự động — để user tự review diff (`git diff`) trước khi commit.
- Nếu 1 file 🔴 hoàn toàn là do tích lũy pattern [QUY TẮC] hợp lệ (không có phần lịch sử để rút
  gọn, không trùng lặp file khác) → đề xuất **tách sub-skill theo chủ đề** (Bước 4 mục 2) thay vì
  cố rút gọn nội dung đang cần thiết.

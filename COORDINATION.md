# COORDINATION — Bàn giao ca

> Nguồn sự thật DUY NHẤT cho "đang làm gì / còn thiếu gì / bước tiếp theo là gì" khi bắt đầu hoặc
> kết thúc một phiên làm việc. **BẮT BUỘC cập nhật file này trước khi kết thúc một task** (xem
> `CLAUDE.md` mục "Bàn giao ca").
>
> Khác `docs/migrations/STATUS.md` (chỉ theo dõi tiến độ port từng feature từ `src/legacy/`) —
> file này bao quát **toàn bộ** công việc đang dở trong repo, không riêng migration.

## Trạng thái hiện tại

Chiến dịch dọn dẹp hệ thống điều phối AI (`CLAUDE.md` + `.claude/rules/` + `.claude/skills/`) đã
**hoàn thành phần chính** (Bước 1-2-4 của kế hoạch 4 bước). Bước 3 (đồng bộ tên `SKILL.md` vs
`SKILLS.md` trong `.claude/skills/`) **đang chờ quyết định của user** — chưa làm.

## Việc đang làm dở / Backlog

- [ ] Quyết định có đồng bộ tên file skill về 1 chuẩn duy nhất (`SKILL.md` hay `SKILLS.md`) hay
      không. Hiện trạng: 6 thư mục dùng `SKILL.md` (`appservice-scaffold`, `codebase-map`,
      `contract-test-guardian`, `git-workflow`, `mudblazor-compliance`, `payment-test-generator`),
      6 thư mục dùng `SKILLS.md` (`api`, `cache`, `database`, `migration`, `web`, `worker`).
- [ ] **CHƯA VERIFY**: xác nhận với user xem các file đang "dở" thấy qua `git status` lúc
      2026-07-15 (`docs/sql/manifest.json`, `tools/POS.DbMigrator/{ManifestScriptProvider,
      Program}.cs`, `tests/POS.ContractTests/DbMigratorScriptOrderTests.cs`,
      `.claude/skills/database/SKILLS.md`, appsettings UAT/Production, thư mục `.github/`) có phải
      WIP hợp lệ từ task `[2026-07-14] Điều tra MasterDataZipGeneratorWorker...` hay không, trước
      khi bất kỳ ai commit — tránh gộp nhầm việc dở của task khác.

## Next steps

- Nếu user chốt đồng bộ tên skill → thực hiện đổi tên + cập nhật mọi link trỏ tới (kể cả trong
  `CLAUDE.md`), rồi coi chiến dịch dọn dẹp là xong.
- Nếu user chốt không cần → đóng chiến dịch, không cần thao tác gì thêm.

## Lịch sử bàn giao gần nhất

| Ngày | Việc đã làm | Ghi chú |
|---|---|---|
| 2026-07-15 | Gỡ conflict-marker Git trong `CLAUDE.md` (888→114 dòng), di dời phần `dbo.Store` sang `database-standards.md`, tạo `COORDINATION.md`, đổi tên lệnh `/resume`→`/task-resume` | Chi tiết đầy đủ ở `docs/CHANGELOG.md` entry cùng ngày |

---
name: git-workflow
description: Chuẩn hóa quy trình Git/GitHub cho POS_Migration — kiểm tra trạng thái, bắt buộc chạy guardrail tests (tests/POS.ContractTests) trước khi commit, quy ước đặt tên branch, format commit message, push an toàn, và checklist trước khi tạo Pull Request. Dùng khi chuẩn bị commit, push, hoặc tạo PR cho bất kỳ thay đổi nào trong repo này.
---

# Git/GitHub Workflow — POS_Migration

Áp dụng skill này **mỗi khi** chuẩn bị `git commit`, `git push`, hoặc tạo Pull Request trong repo
này. Mục tiêu: không commit code vỡ contract JSON của 5.000 máy POS, không quên đăng ký DI, và giữ
lịch sử git sạch, dễ review.

## Bước 1 — Soát trạng thái trước khi làm gì khác

```bash
git status
git diff
git diff --staged
```

- Nếu có file/branch lạ không phải do bạn tạo trong phiên này → dừng lại, hỏi người dùng trước khi
  đụng vào (có thể là việc dở dang của họ).
- Trước bất kỳ lệnh nào có thể mất dữ liệu chưa commit (`checkout`, `restore`, `reset --hard`,
  `clean -f`) → bắt buộc `git status` trước, và `git stash -u` nếu có thay đổi cần giữ.

## Bước 2 — BẮT BUỘC: chạy guardrail tests trước khi commit

Dự án có 3 "vành đai bảo vệ" nằm trong `tests/POS.ContractTests/` (xem CLAUDE.md gốc):
1. `JsonFieldContractTests.cs` — khóa tên field JSON mà 5.000 máy POS đang parse
2. `DependencyInjectionTests.cs` — chặn quên đăng ký DI
3. `ExceptionMiddlewareTests.cs` — khóa hành vi lưới an toàn global

```bash
dotnet test tests/POS.ContractTests
```

- **Đỏ → không commit.** Sửa lỗi hoặc — nếu bạn CỐ Ý đổi contract — cập nhật field kỳ vọng trong
  chính file test đó **cùng commit**, kèm lý do trong commit message.
- Nếu thay đổi động vào Blazor UI (`POS.Web`), chạy thêm `dotnet build` toàn solution để bắt lỗi
  Razor trước khi commit.

```bash
dotnet build POS.slnx
```

## Bước 3 — Quy ước đặt tên branch

- Nhánh làm việc của Claude Code: `claude/<slug-ngắn-mô-tả-việc>` (kebab-case, tiếng Anh, ngắn gọn).
- Không tạo branch mới nếu đã có branch được chỉ định sẵn cho task — dùng đúng branch đó.
- Không đổi tên/xóa branch người dùng đang dùng mà không hỏi trước.

## Bước 4 — Format commit message

Quy ước: `<type>: <mô tả ngắn, tập trung vào WHY>`

| type | Dùng khi |
|---|---|
| `feat` | Thêm nghiệp vụ/tính năng mới |
| `fix` | Sửa lỗi |
| `refactor` | Tái cấu trúc, không đổi hành vi |
| `docs` | Chỉ sửa tài liệu (CLAUDE.md, SKILL.md, README) |
| `test` | Chỉ thêm/sửa test |

- 1–2 câu, giải thích **tại sao** thay đổi, không liệt kê lại diff.
- KHÔNG commit nếu người dùng chưa yêu cầu commit — chỉ vì bạn vừa sửa xong file.
- Luôn dùng HEREDOC để giữ format message chính xác:

```bash
git add <file1> <file2>   # add từng file cụ thể — KHÔNG git add -A / git add .
git commit -m "$(cat <<'EOF'
fix: tránh trùng key Redis khi 2 store cùng mã khuyến mãi

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- Trước khi stage bằng lệnh rộng, kiểm tra lại `git status` xem có file nhạy cảm
  (`.env`, `appsettings.*.json` chứa secret, credentials) lọt vào không.

## Bước 5 — Push an toàn

```bash
git push -u origin <branch-name>
```

- Nếu lỗi mạng: retry tối đa 4 lần, backoff 2s → 4s → 8s → 16s.
- **KHÔNG** `--force` trừ khi người dùng yêu cầu rõ ràng; nếu buộc phải force, dùng
  `--force-with-lease` và luôn hỏi trước.
- **KHÔNG** `--no-verify` để né hook — nếu hook fail, sửa nguyên nhân gốc rồi commit lại (đừng
  amend commit cũ khi hook chặn commit trước — commit đó chưa từng xảy ra).

## Bước 6 — Trước khi tạo Pull Request

1. Tìm template: `.github/pull_request_template.md`, `.github/PULL_REQUEST_TEMPLATE/`, hoặc
   `PULL_REQUEST_TEMPLATE.md` ở root. Nếu có → dùng đúng heading của template, chỉ điền nội dung
   liên quan tới diff, bỏ qua phần hỏi secret/token.
2. Kiểm tra chưa có PR mở nào khác từ cùng branch (tránh tạo trùng).
3. Title ngắn gọn (< 70 ký tự), phần mô tả nêu tóm tắt thay đổi + test plan.
4. **KHÔNG** tự ý tạo PR nếu người dùng chưa yêu cầu — chỉ commit + push khi được yêu cầu vậy.

## Checklist nhanh trước khi commit

```
□ git status — không có file lạ/nhạy cảm bị stage nhầm
□ dotnet test tests/POS.ContractTests — xanh
□ Commit message theo <type>: <why>, không amend commit người khác
□ Branch đúng convention claude/<slug> hoặc branch được chỉ định sẵn
□ Chỉ push/tạo PR khi được yêu cầu rõ ràng
```

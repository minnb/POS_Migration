# POS API — .NET 10

Backend + dashboard nội bộ cho hệ thống POS, phục vụ ~5.000 máy POS. Kiến trúc Clean
Architecture, phát triển mới (greenfield) trên .NET 10 — không còn migrate từ source
.NET Framework 4.6 cũ (đã gỡ khỏi repo).

## Cấu trúc thư mục

```
POS_Migration/
├── CLAUDE.md                    ← ⭐ Đọc file này trước tiên (context cho Claude Code)
├── POS.slnx                     ← Solution file
├── .claude/
│   ├── commands/                ← Slash command (/add-dto-common, /web-add-feature, ...)
│   └── skills/                  ← Skill chi tiết theo domain (api, cache, database, worker, web)
├── docs/
│   ├── CURRENT_STRUCTURE.md     ← Bản đồ DTO/Service/Repository/Helper hiện có (đọc trước khi tạo mới)
│   ├── architecture/database-schema.md  ← Bản đồ schema DB (RPOSMasterData/CentralMD)
│   ├── API_CONTRACT.md          ← Golden contract JSON cho 5.000 máy POS
│   ├── WEB_STATUS.md, CHANGELOG.md      ← Trạng thái / lịch sử POS.Web
│   ├── web/                     ← Tài liệu nghiệp vụ POS.Web (login-flow, coupon-flow, ...)
│   └── sql/                     ← Script SQL / stored procedure áp dụng thủ công
├── src/
│   ├── POS.Common/              ← DTOs, Enums, ResultResponse (Domain models)
│   ├── POS.Infrastructure/      ← Repositories, Redis, RabbitMQ, AppServices (I/O)
│   ├── POS.Application/         ← Services, business logic
│   ├── POS.Api/                 ← Controllers, Middleware — API phục vụ máy POS
│   ├── POS.Web/                 ← Blazor Server dashboard nội bộ (MudBlazor)
│   └── POS.Worker/              ← Background worker host
└── tests/
    └── POS.ContractTests/       ← Contract test khoá field JSON + DI validation + exception middleware
```

## Quy trình làm việc với Claude Code

`CLAUDE.md` là nguồn quy tắc chính (kiến trúc, quy ước layer, cache, worker, stored
procedure, POS.Web...). Trước khi tạo DTO/Service/Repository mới, đọc
`docs/CURRENT_STRUCTURE.md` để tránh trùng lặp. Trước khi viết SQL/SP đụng bảng
`RPOSMasterData`, đọc `docs/architecture/database-schema.md`.

## Tech Stack

- .NET 10 (Clean Architecture)
- Dapper + Microsoft.Data.SqlClient (database access)
- Newtonsoft.Json (serialization — bắt buộc toàn solution, không dùng System.Text.Json)
- Redis StandAlone (cache), RabbitMQ / Kafka (messaging)
- Blazor Server + MudBlazor 9.5.0 (POS.Web dashboard)

## Testing

```powershell
dotnet test tests/POS.ContractTests
```

Bắt buộc chạy trước khi commit — khoá tên field JSON contract, kiểm tra đăng ký DI, và
hành vi exception middleware.

# POS Backend API — .NET Core 10

Phiên bản mới của hệ thống POS Backend, được convert từ .NET Framework 4.6.

## Cấu trúc thư mục

```
POS.Backend.New/
├── CLAUDE.md                    ← ⭐ Đọc file này trước tiên (dành cho Claude Code)
├── .claude/
│   └── commands/
│       ├── analyze-legacy.md    ← Phân tích toàn bộ code cũ
│       ├── convert-module.md    ← Convert từng module
│       └── review-code.md       ← Review & refactor sau convert
├── docs/
│   ├── architecture.md          ← Kiến trúc chi tiết
│   ├── api-mapping.md           ← Bảng tracking endpoint cũ → mới
│   ├── conventions.md           ← Coding conventions bắt buộc
│   └── modules/                 ← Tài liệu từng module (tạo dần)
├── src/
│   ├── POS.API/                 ← Controllers, Middleware
│   ├── POS.Application/         ← Business Logic, Services, DTOs
│   ├── POS.Domain/              ← Entities, Enums, Exceptions
│   ├── POS.Infrastructure/      ← Dapper, Repositories, External APIs
│   └── POS.Shared/              ← Shared utilities, Constants
└── tests/
    ├── POS.UnitTests/
    └── POS.IntegrationTests/
```

## Quy trình làm việc với Claude Code

### Bước 1: Phân tích code cũ (chạy 1 lần)
Dùng prompt trong `.claude/commands/analyze-legacy.md`

### Bước 2: Convert từng module (lặp lại cho mỗi module)
Dùng prompt trong `.claude/commands/convert-module.md`

### Bước 3: Review sau mỗi module
Dùng prompt trong `.claude/commands/review-code.md`

## Tech Stack

- .NET Core 10
- Dapper (database access — dễ đổi MS SQL ↔ PostgreSQL)
- AutoMapper
- FluentValidation
- Serilog
- Swagger / OpenAPI

## Liên quan

- Dự án cũ: `../POS.Backend/` (.NET Framework 4.6) — CHỈ ĐỌC
- Tài liệu kiến trúc: `docs/architecture.md`
- Tracking tiến độ convert: `docs/api-mapping.md`
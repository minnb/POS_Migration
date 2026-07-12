# Hướng dẫn dùng MCP + Unit Test (POS/RPOS) — Step by Step

> Cho dev: cách bật 2 MCP server (SQL read-only, Redis) để Claude soi dữ liệu khi debug, và cách
> chạy/sinh unit test luồng Payment. Đọc từ trên xuống, làm theo đúng thứ tự.

---

## 0. Đã có sẵn gì trong repo (không phải làm lại)

| File | Vai trò |
|---|---|
| `.mcp.json` | Khai báo 2 MCP server, **chỉ dùng `${BIẾN}`** — không chứa secret, được commit |
| `.claude/settings.local.json` | Chứa endpoint + mật khẩu DEV thật — **đã gitignore**, KHÔNG commit |
| `.claude/skills/payment-test-generator/SKILL.md` | Skill sinh unit test đúng chuẩn dự án |
| `tests/POS.UnitTests/` | Project unit test (xUnit + Moq + FluentAssertions), 13 test mẫu |

---

## 1. Cài công cụ nền (làm 1 lần / máy)

MCP server chạy như tiến trình con, cần runtime tương ứng:

```powershell
# SQL MCP chạy bằng Node/npx  → cần Node.js (kiểm tra)
node -v ; npx -v

# Redis MCP chạy bằng uvx (Python) → CHƯA có trên máy, cài uv:
winget install --id=astral-sh.uv      # hoặc:  pip install uv
uvx --version                         # xác nhận sau khi cài
```

### ⚠️ Gỡ chặn TLS công ty cho npm (BẮT BUỘC nếu dùng SQL MCP qua npx)

Mạng công ty chặn TLS → `npx` báo `UNABLE_TO_VERIFY_LEAF_SIGNATURE`. Trỏ Node tới CA công ty:

```powershell
# Xin file CA công ty (.pem/.crt) từ IT, rồi set biến môi trường (user-level):
setx NODE_EXTRA_CA_CERTS "C:\path\to\company-ca.pem"
# mở lại terminal/Claude Code cho biến có hiệu lực
```
> NuGet/`dotnet` KHÔNG dính lỗi này (dùng OS trust store) → phần Unit Test chạy bình thường.

---

## 2. Điền endpoint DEV vào `.claude/settings.local.json`

Sửa giá trị placeholder thành DEV thật của bạn (file này KHÔNG lên git):

```json
{
  "env": {
    "MSSQL_HOST": "10.x.x.x",
    "MSSQL_PORT": "1433",
    "MSSQL_DATABASE": "RPOSMasterData",
    "MSSQL_RO_USER": "rpos_ro",
    "MSSQL_RO_PASSWORD": "<mật khẩu DEV>",
    "REDIS_URL": "redis://10.x.x.x:6379/0"
  }
}
```

### 🔒 Tạo login SQL chỉ-đọc (nhờ DBA chạy trên DEV) — an toàn 5.000 POS
```sql
CREATE LOGIN rpos_ro WITH PASSWORD = 'DevOnly@123';
-- Cấp db_datareader ở CẢ 3 DB:
USE RPOSMasterData;   CREATE USER rpos_ro FOR LOGIN rpos_ro; ALTER ROLE db_datareader ADD MEMBER rpos_ro;
USE RPOSCentralSales; CREATE USER rpos_ro FOR LOGIN rpos_ro; ALTER ROLE db_datareader ADD MEMBER rpos_ro;
USE RPOSLoyalty;      CREATE USER rpos_ro FOR LOGIN rpos_ro; ALTER ROLE db_datareader ADD MEMBER rpos_ro;
```
> `db_datareader` = chỉ SELECT. Kể cả AI/MCP lỡ tay cũng KHÔNG ghi/xóa được dữ liệu giao dịch/kết ca.

---

## 3. Bật & kiểm tra MCP trong Claude Code

```
# Trong Claude Code (phiên tương tác):
/mcp                     # xem trạng thái; mssql-rpos-readonly + redis-rpos phải "connected"
```
- Chưa "connected" → xem lại mục 1 (uvx/CA) và mục 2 (endpoint đúng chưa, DEV có mở port không).
- Lần đầu `npx`/`uvx` sẽ tải package (chậm vài chục giây) — bình thường.

---

## 4. Dùng MCP khi phát triển / debug

Chỉ cần hỏi Claude bằng tiếng Việt, nó tự gọi tool MCP:

| Bạn muốn | Ví dụ câu lệnh cho Claude |
|---|---|
| Soi giao dịch/kết ca | "Query top 10 dòng `RPOSCentralSales..<bảng>` của store X hôm nay để debug lỗi EOD" |
| Xem schema | "List cột của bảng `Store` trong RPOSMasterData" |
| Soi cache Master Data | "Đọc TTL và các field của Redis hash `MD:SysWebApi`" |
| Kiểm tra worker | "Xem key `Worker:Heartbeat:*` trong Redis còn sống không" |

**Giới hạn có chủ đích:** MCP chỉ để **debug/khám phá dữ liệu**. Khi viết SP/query production, vẫn
theo "Cổng chặn trùng lặp #5" trong `CLAUDE.md` (tra `docs/architecture/*-schema.md` trước). Không
tự invalidate cache qua MCP trừ khi có chủ đích.

---

## 5. Unit Test luồng Payment

### 5.1 Chạy test (không cần MCP, không cần DB)
```powershell
dotnet test tests/POS.UnitTests        # 13 test luồng Payment
dotnet test tests/POS.ContractTests    # 45 guardrail — phải luôn xanh
```

### 5.2 Sinh thêm test mới (dùng skill)
Trong Claude Code, gõ:
```
/payment-test-generator   (hoặc: "sinh unit test cho <service/controller> theo skill payment-test-generator")
```
Skill sẽ tự tuân thủ **Nguyên tắc Mock**:
- Test qua **interface tầng Application** (mock Moq), KHÔNG gọi HTTP/DB thật.
- Assert theo `StatusCode`: success→200, partner-fail→400, unknown→BadRequest, exception→500.
- ⚠️ Bẫy hay gặp: class `GotITService`/`UrboxService` trùng tên (Application vs Infrastructure) →
  dùng alias `using AppPartner = POS.Application.Features.Partner;`.

### 5.3 Định nghĩa "xong" cho mỗi lần thêm test
```
[ ] dotnet test tests/POS.UnitTests      → PASS
[ ] dotnet test tests/POS.ContractTests  → PASS (không hồi quy)
[ ] dotnet build POS.slnx -clp:ErrorsOnly → 0 error
[ ] không thêm production code, không đổi field JSON
```

---

## 6. Tra cứu nhanh

| Việc | Lệnh |
|---|---|
| Trạng thái MCP | `/mcp` |
| Chạy toàn bộ test | `dotnet test tests/POS.UnitTests` và `dotnet test tests/POS.ContractTests` |
| Build kiểm tra lỗi | `dotnet build POS.slnx -clp:ErrorsOnly` |
| Cài uv (Redis MCP) | `winget install --id=astral-sh.uv` |
| Gỡ TLS npm | `setx NODE_EXTRA_CA_CERTS "<đường-dẫn-ca.pem>"` |

> **Nhắc lại an toàn:** đổi endpoint/mật khẩu chỉ sửa `.claude/settings.local.json` (đã gitignore).
> TUYỆT ĐỐI không đưa connection string thật vào `.mcp.json` hay `appsettings`.

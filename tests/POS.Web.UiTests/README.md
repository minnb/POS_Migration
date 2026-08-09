# POS.Web UI Tests (skill `webapp-testing`)

Smoke test end-to-end cho POS.Web bằng **Playwright (Python)** — bổ sung cho xUnit
`tests/POS.ContractTests` / `tests/POS.UnitTests` (test logic, không render UI thật).

## Tiền đề (đã có sẵn trên máy dev hiện tại)

- Python 3.13 + package `playwright` + browser Chromium (`playwright install chromium`).
- .NET 10 SDK (`dotnet`).
- POS.Web chạy profile `http` tại `http://localhost:5170`.

## Chạy

**Cách A — POS.Web đã chạy sẵn** (vd đang bật trong IDE ở cổng 5170): chạy thẳng script.

```bash
python tests/POS.Web.UiTests/smoke_login.py
```

**Cách B — chưa chạy**: helper `with_server.py` tự khởi động `dotnet run`, chờ cổng 5170 sẵn sàng,
chạy script, rồi tắt server. (Yêu cầu cổng 5170 còn trống — nếu đã có tiến trình giữ cổng thì dùng
Cách A.)

```bash
python .claude/skills/webapp-testing/scripts/with_server.py \
  --server "dotnet run --project src/POS.Web/POS.Web.csproj --launch-profile http" --port 5170 \
  --timeout 240 \
  -- python tests/POS.Web.UiTests/smoke_login.py
```

> Script tự ép stdout UTF-8 để in được tiếng Việt trên console Windows (cp1252).

## Kết quả

- stdout in `RESULT: PASS/FAIL` từng assertion + `SUMMARY: n/n passed`; exit code != 0 nếu có FAIL.
- Ảnh chụp: `tests/POS.Web.UiTests/artifacts/login.png`.

## Phạm vi

Case cơ bản chỉ phủ trang `/login` (anonymous, không cần DB). Test sau đăng nhập cần
`DashboardUsers` trong `RPOSMasterData` + bridge-token cookie — mở rộng sau.

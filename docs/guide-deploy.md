# Hướng dẫn triển khai UAT / PROD (nginx + systemd + Docker)

> Dành cho DevOps. Áp dụng cho 3 service: **POS.Api**, **POS.Web**, **POS.Worker**.
> Mỗi service đã có đủ 3 môi trường: `Development` / `UAT` / `Production`.

> ⚠️ **Cập nhật topology (2026-07-13)**: UAT/PROD thật chạy **POS.Api + POS.Web native qua systemd**
> trên Ubuntu (không qua Docker), **chỉ POS.Worker chạy Docker**. Đây là điểm dựa theo thông tin
> người vận hành cung cấp trực tiếp — tôi (Claude) không có quyền SSH vào server UAT/PROD thật để
> tự xác nhận 100%, nên coi đây là input đáng tin từ người nắm hạ tầng thật.
> `docker-compose.yml` ở gốc repo + `Dockerfile` (POS.Api) + `src/POS.Web/Dockerfile` **vẫn giữ
> nguyên, KHÔNG xóa** — dùng cho môi trường **SIT/dev cục bộ** (bằng chứng: tên container
> `sit_dotnet_api`/`mssql_2019`, bind mount tương đối `./logs`), không đại diện cho cách UAT/PROD
> thật đang chạy.

---

## 1. Tổng quan

| Service | Cách chạy (UAT/PROD) | Cổng lắng nghe | Biến môi trường chọn config | Sau nginx? |
|---|---|---|---|---|
| **POS.Api** | Native — systemd (`deploy/linux/systemd/pos-api.service`) | `127.0.0.1:5001` | `ASPNETCORE_ENVIRONMENT` | ✅ |
| **POS.Web** | Native — systemd (`deploy/linux/systemd/pos-web.service`) | `127.0.0.1:5002` | `ASPNETCORE_ENVIRONMENT` | ✅ |
| **POS.Worker** | Docker (`Dockerfile.worker`) | — (không HTTP) | `DOTNET_ENVIRONMENT` | ❌ |

**Giá trị biến môi trường:** `UAT` hoặc `Production` → nạp tương ứng `appsettings.UAT.json` / `appsettings.Production.json`.

> ⚠️ POS.Worker dùng `DOTNET_ENVIRONMENT` (KHÔNG phải `ASPNETCORE_ENVIRONMENT`).

> **Môi trường SIT/dev cục bộ** vẫn dùng Docker cho cả 3 service qua `docker-compose.yml` gốc —
> xem comment đầu file đó. Từ §3.1/§3.2 trở xuống, tài liệu này mô tả **UAT/PROD thật (systemd)**;
> §3.3 (POS.Worker) vẫn là Docker cho mọi môi trường.

---

## 2. Chuẩn bị trước khi deploy (BẮT BUỘC)

Mỗi service có file `appsettings.UAT.json` chứa **placeholder** cần điền giá trị hạ tầng thật:

```
src/POS.Api/appsettings.UAT.json
src/POS.Web/appsettings.UAT.json
src/POS.Worker/appsettings.UAT.json
```

Thay tất cả placeholder `<...>`:
- `<UAT_SQL_HOST>`, `<UAT_SQL_USER>`, `<UAT_SQL_PASSWORD>`
- `<UAT_REDIS_HOST>`, `<UAT_RABBIT_HOST>`, `<UAT_RABBIT_PASSWORD>`
- `<UAT_KAFKA_HOST>`, `<UAT_API_SERVER_IP>`, `<UAT_EINVOICE_*>`
- `<UAT_POS_API_BASE_URL>` (`src/POS.Web/appsettings.UAT.json` → `HealthCheck:PosApiBaseUrl`) —
  URL nội bộ POS.Web gọi được tới POS.Api trong UAT (dùng cho mục "POS.Api" ở `/ops/health`),
  KHÁC `<UAT_API_SERVER_IP>` (IP public dùng cho mục đích khác — xem `BaseController.cs`)

> PROD: `appsettings.Production.json` đã có sẵn giá trị thật — chỉ kiểm tra lại trước khi build.
> **Ngoại lệ cần điền tay**: `src/POS.Web/appsettings.Production.json` → `HealthCheck:PosApiBaseUrl`
> hiện là placeholder `<PROD_POS_API_BASE_URL>` (chưa xác định được network layout thật giữa
> container POS.Web ↔ POS.Api ở Production) — Ops kiểm tra `docker network inspect` / cấu hình
> nginx vhost để điền đúng URL nội bộ trước khi deploy, nếu không mục "POS.Api" trên
> `/ops/health` sẽ luôn báo lỗi ở Production.
> Khi UAT/PROD ổn định: đặt `WebApp:EnableDetailedErrors = false` trong `appsettings.Production.json` (POS.Web) để không lộ stack trace.

> 🔒 **Mã hóa credentials (C4)**: nếu `appsettings.{UAT|Production}.json` (POS.Api **và** POS.Web) chứa
> token `enc:...` thay vì password thật, container **BẮT BUỘC** có env `POS_SECRET_KEY` (xem §3.1/§3.2 và
> `docs/ROLLOUT.md` §C4) — thiếu khóa → app fail-fast lúc khởi động. Chưa mã hóa (còn plaintext) thì bỏ qua biến này.

---

## 2.5. Chạy `POS.DbMigrator` (BẮT BUỘC trước khi start container, mỗi lần đổi SQL)

> Chi tiết đầy đủ: `docs/ROLLOUT.md` §D0. Tóm tắt thao tác ở đây cho DevOps.

```bash
dotnet tools/POS.DbMigrator/bin/Release/net10.0/POS.DbMigrator.dll --verify --config appsettings.Production.json
# → đọc cảnh báo Track B (script rủi ro cao) còn thiếu, xử lý theo docs/ROLLOUT.md §D6/D10/O1/O1b

dotnet tools/POS.DbMigrator/bin/Release/net10.0/POS.DbMigrator.dll --whatif
# → xem trước danh sách Track A sẽ chạy lại (không cần kết nối DB)

dotnet tools/POS.DbMigrator/bin/Release/net10.0/POS.DbMigrator.dll --apply --config appsettings.Production.json
# → chạy Track A thật (idempotent, tự chạy lại toàn bộ mỗi lần). Exit code ≠ 0 → DỪNG deploy, không
#   build/run container mới cho tới khi lỗi được xử lý.
```

`--config` trỏ tới đúng `appsettings.{UAT|Production}.json` của môi trường đang deploy (cùng file
dùng cho `POS.Api`/`POS.Web` — có `ConnectionStrings:CentralMD`, `CentralGeneral`,
`CentralSaleTemplate`). Nếu file có token `enc:...`, migrator cần `POS_SECRET_KEY` trong biến môi
trường của shell chạy lệnh này (giống §2 mục mã hóa credentials) — hoặc đặt file `.env` (copy từ
`.env.example`) cạnh `POS.DbMigrator.dll` để tool tự đọc, không cần `export` mỗi lần (xem
`docs/deploy/pos-dbmigrator-guide.md` §2.3).

> `--config` giờ optional nếu chạy `dotnet tools/POS.DbMigrator/...` **ngay trong git checkout** này
> (tool tự suy ra `src/POS.Api/appsettings.{Environment}.json` theo
> `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`, mặc định `Production` — xem
> `docs/deploy/pos-dbmigrator-guide.md` §2.1). Khuyến nghị **vẫn truyền `--config` tường minh** như
> ví dụ dưới đây trong pipeline CI/CD — tránh phụ thuộc vào biến môi trường của shell đang chạy.

---

## 3. Build & chạy: POS.Api/POS.Web (native, systemd) + POS.Worker (Docker)

### 3.1. POS.Api (systemd)

```bash
# Publish (build Release, xuất ra thư mục chạy thật)
dotnet publish src/POS.Api/POS.Api.csproj -c Release -o /opt/pos/api

# Cài + khởi động (lần đầu — xem đầy đủ deploy/linux/systemd/README.md)
sudo cp deploy/linux/systemd/pos-api.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now pos-api

# Re-deploy (cập nhật version mới)
sudo systemctl stop pos-api
dotnet publish src/POS.Api/POS.Api.csproj -c Release -o /opt/pos/api
sudo systemctl start pos-api
```
> Unit mẫu `deploy/linux/systemd/pos-api.service` đã đặt `ASPNETCORE_URLS=http://127.0.0.1:5001`
> (cổng này phải khớp `proxy_pass` trong nginx) + `Environment=ASPNETCORE_ENVIRONMENT=Production`
> (đổi trực tiếp trong file unit cho UAT). Đổi `User=`/`WorkingDirectory=` khớp tài khoản/thư mục
> publish thật của server (file mẫu dùng placeholder `pos-api`/`/opt/pos/api`).
> `EnvironmentFile=-/etc/pos/pos-api.env`: chỉ cần tạo file này (chứa `POS_SECRET_KEY=...`) khi
> `appsettings.{UAT|Production}.json` có token `enc:...` (xem §C4 `docs/ROLLOUT.md`) — bỏ qua nếu
> file còn plaintext.
> `AppSettings:FtpRootPath`/`FolderShare` trỏ thẳng vào đường dẫn thật trên host (không còn khái
> niệm bind-mount container) — dùng chung `/srv/pos/ftpbluepos` (PROD) / `/srv/pos/uat/ftpbluepos`
> (UAT) như trước, tạo + cấp quyền bằng `deploy/linux/setup-pos-dirs.sh` (xem
> `docs/deploy/ubuntu-guide.md`) TRƯỚC khi start service lần đầu — user `pos-api` cần là member
> group `posops` để ghi/đọc được thư mục này.

### 3.2. POS.Web (systemd)

```bash
dotnet publish src/POS.Web/POS.Web.csproj -c Release -o /opt/pos/web

sudo cp deploy/linux/systemd/pos-web.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now pos-web

# Re-deploy
sudo systemctl stop pos-web
dotnet publish src/POS.Web/POS.Web.csproj -c Release -o /opt/pos/web
sudo systemctl start pos-web
# Sau re-deploy, user nên hard refresh (Ctrl+Shift+R) để nạp client mới.
```
> Cổng `127.0.0.1:5002` phải khớp `proxy_pass` trong `nginx/pos-web.uat.conf`/`pos-web.conf`.
> ⚠️ **Data Protection Keys** (thay cho named volume Docker `dp-keys` trước đây): `src/POS.Web/Program.cs`
> giờ gọi tường minh `PersistKeysToFileSystem` vào `/var/lib/pos-web/dataprotection-keys` (mặc định
> trên Linux, đổi được qua config `DataProtection:KeyPath`) — **PHẢI tạo thư mục này + cấp quyền ghi
> cho user `pos-web` TRƯỚC khi start service lần đầu** (xem `deploy/linux/systemd/README.md` bước 4),
> nếu không mất key qua mỗi lần restart → toàn bộ cookie đăng nhập cũ vô hiệu.
> ⚠️ **`ftpbluepos` dùng CHUNG với POS.Api** — nút "Đẩy dữ liệu đầu ngày" (`/catalog/pos-setup`) ghi
> file master-data vào đây, máy POS tải lại qua `DowloadFileStream` của POS.Api; `AppSettings:FtpRootPath`
> của cả 2 service PHẢI trỏ **cùng 1 đường dẫn vật lý thật trên host** (không còn khái niệm mount
> riêng theo container) — `/srv/pos/ftpbluepos` (PROD) / `/srv/pos/uat/ftpbluepos` (UAT).

### 3.3. POS.Worker (Docker — không đổi cơ chế, chỉ thêm mount log)

> Runbook chi tiết riêng cho POS.Worker (bảng so sánh Task Scheduler ↔ Docker, checklist,
> `appsettings.UAT.json` mới thêm): **`docs/deploy/pos-worker-ubuntu-guide.md`**.

```bash
# Build
docker build -t pos-worker:uat -f Dockerfile.worker .

# Run (UAT) — không expose cổng
docker run -d --name pos-worker-uat \
  -e DOTNET_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  --add-host host.docker.internal:host-gateway \
  --user "1654:1654" \
  -v /srv/pos/logs:/srv/pos/logs \
  -v /srv/pos/uat/ftpbluepos:/app/ftpbluepos \
  --restart unless-stopped \
  pos-worker:uat
```
> PROD: đổi `DOTNET_ENVIRONMENT=Production`, tag `pos-worker:prod`, tên container khác — **và đổi path
> mount ftpbluepos** sang `-v /srv/pos/ftpbluepos:/app/ftpbluepos` (KHÔNG tiền tố `uat`, lý do giống
> §3.1 — UAT/PROD chạy chung host, không được dùng chung thư mục). `PosFileImportWorker` quét
> `SyncDataPos/Sale/Kafka` bên trong thư mục này — đúng folder mà POS.Api (`UploadFileSale`) đang ghi
> file sale vào, xem `docs/deploy/ubuntu-guide.md` để biết chi tiết + rủi ro đã biết của việc dùng chung.
> `--user "1654:1654"` + `-v /srv/pos/logs:/srv/pos/logs` (**mới**, xem `docs/ROLLOUT.md` §O11): GID
> `1654` PHẢI khớp GID thật của group `posops` trên host (`getent group posops` để xác nhận) — để
> container ghi log vào `/srv/pos/logs/worker` với group `posops`, cho phép POS.Api/POS.Web (systemd,
> `Group=posops`) đọc lại được. Chạy `deploy/linux/setup-pos-log-dirs.sh` TRƯỚC lần `docker run` đầu.

---

## 4. Cấu hình nginx

### 4.1. POS.Web

File config đã có sẵn trong repo:

| Môi trường | File | Cổng listen | proxy_pass |
|---|---|---|---|
| UAT | `nginx/pos-web.uat.conf` | `8081` | `127.0.0.1:5002` |
| PROD | `nginx/pos-web.conf` | `8080` | `127.0.0.1:5001` |

```bash
# UAT
sudo cp nginx/pos-web.uat.conf /etc/nginx/sites-available/pos-web-uat
sudo ln -s /etc/nginx/sites-available/pos-web-uat /etc/nginx/sites-enabled/pos-web-uat

# PROD
sudo cp nginx/pos-web.conf /etc/nginx/sites-available/pos-web
sudo ln -s /etc/nginx/sites-available/pos-web /etc/nginx/sites-enabled/pos-web

# Test + reload (zero-downtime)
sudo nginx -t && sudo systemctl reload nginx
```

> 🔑 Điểm quan trọng nhất cho Blazor Server (POS.Web): config phải có `location /_blazor` riêng (WebSocket, timeout 24h, `X-Accel-Buffering "no"`) và buffer ≥ 256KB. Hai file trên đã cấu hình sẵn. **Đừng** sửa `proxy_pass` lệch cổng container.

### 4.2. POS.Api

POS.Api là REST API thuần (không WebSocket) → config nginx đơn giản. Tạo `/etc/nginx/sites-available/pos-api`:

```nginx
server {
    listen 8090;            # cổng public của API (đổi theo nhu cầu)
    server_name _;

    proxy_connect_timeout 30s;
    proxy_read_timeout    120s;
    proxy_send_timeout    120s;
    client_max_body_size  20m;   # cho upload file SOD/ảnh nếu có

    location / {
        proxy_pass         http://127.0.0.1:5001;   # khớp -p host POS.Api
        proxy_http_version 1.1;
        proxy_set_header   Host              $http_host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```
> POS.Api KHÔNG cần `location /_blazor` (không phải Blazor). UAT: dùng cổng/upstream riêng (vd listen `8091` → `5003`).

---

## 5. Kiểm tra sau deploy

```bash
# 1. Service chạy (POS.Api/POS.Web — systemd) + healthy
sudo systemctl status pos-api pos-web        # Active: active (running)
sudo journalctl -u pos-api -n 50 --no-pager  # không có exception lúc khởi động
sudo journalctl -u pos-web -n 50 --no-pager

# 1b. Worker (Docker — không đổi)
docker ps                            # cột STATUS = "healthy"
docker logs --tail 50 pos-worker-uat

# 2. Health endpoint (trực tiếp, chưa qua nginx)
curl -fsS http://127.0.0.1:5001/health    # POS.Api → "healthy"
curl -fsS http://127.0.0.1:5002/health    # POS.Web → "healthy"

# 3. Qua nginx
curl -I http://<server>:8081/      # POS.Web UAT → 200
```

**POS.Web — kiểm tra WebSocket (quan trọng):**
Mở trình duyệt → F12 → Network → lọc `_blazor` → phải thấy 1 kết nối WebSocket trạng thái `101 Switching Protocols`. Nếu không có → kiểm tra lại `location /_blazor` và cổng `proxy_pass`.

---

## 6. Cập nhật phiên bản mới (re-deploy)

```bash
# POS.Api / POS.Web (systemd) — xem §3.1/§3.2
sudo systemctl stop pos-web
dotnet publish src/POS.Web/POS.Web.csproj -c Release -o /opt/pos/web
sudo systemctl start pos-web

# POS.Worker (Docker — không đổi)
docker build -t pos-worker:uat -f Dockerfile.worker .
docker stop pos-worker-uat && docker rm pos-worker-uat
docker run -d --name pos-worker-uat ...   # lệnh run như §3.3
```
> Không cần reload nginx khi re-deploy Api/Web (cổng không đổi).
> Sau re-deploy POS.Web, user nên hard refresh (`Ctrl+Shift+R`) để nạp client mới.

---

## 7. Rollback nhanh

```bash
# POS.Api / POS.Web (systemd) — giữ bản publish cũ ở thư mục khác trước mỗi lần deploy
# (vd /opt/pos/web-prev), rồi trỏ lại WorkingDirectory/ExecStart trong unit hoặc swap symlink:
sudo systemctl stop pos-web
sudo rm /opt/pos/web && sudo ln -s /opt/pos/web-prev /opt/pos/web   # nếu dùng symlink versioning
sudo systemctl start pos-web

# POS.Worker (Docker — không đổi, giữ tag image cũ vd pos-worker:uat-prev)
docker stop pos-worker-uat && docker rm pos-worker-uat
docker run -d --name pos-worker-uat ... pos-worker:uat-prev
```
> Khuyến nghị versioning thư mục publish (`/opt/pos/web-<timestamp>` + symlink `/opt/pos/web` trỏ
> vào bản đang chạy) để rollback tức thời không cần publish lại — chi tiết tùy quy ước Ops thật.

---

## Checklist deploy (tóm tắt)

```
□ Điền hết placeholder <...> trong appsettings.{UAT|Production}.json
□ Chạy POS.DbMigrator --verify → xử lý Track B thiếu → --whatif → --apply (xem §2.5) — TRƯỚC khi
  publish/restart service mới
□ Đã chạy deploy/linux/setup-pos-dirs.sh cho đúng môi trường (PROD: mặc định /srv/pos; UAT: /srv/pos/uat
  — KHÔNG dùng chung, xem docs/deploy/ubuntu-guide.md)
□ Đã chạy deploy/linux/setup-pos-log-dirs.sh (thư mục log dùng chung, xem docs/ROLLOUT.md §O11)
  TRƯỚC khi start pos-api/pos-web/pos-worker lần đầu
□ Nếu appsettings có token enc:... → điền POS_SECRET_KEY vào /etc/pos/pos-api.env + pos-web.env
  (systemd EnvironmentFile, cùng khóa cho cả 2)
□ POS.Api/POS.Web: publish đúng thư mục (`dotnet publish -c Release -o ...`), user systemd
  (`pos-api`/`pos-web`) là member group `posops`
□ Run với đúng biến môi trường (Api/Web: ASPNETCORE_ENVIRONMENT trong unit | Worker: DOTNET_ENVIRONMENT)
□ POS.Web: thư mục Data Protection Keys (`/var/lib/pos-web/dataprotection-keys`) đã tạo + user
  `pos-web` ghi được TRƯỚC lần start đầu tiên (giữ nguyên qua các lần re-deploy)
□ POS.Api/POS.Web: `AppSettings:FtpRootPath` cùng trỏ 1 đường dẫn vật lý thật trên host (CHUNG giữa
  2 service, xem §3.2) — thiếu/lệch → nút "Đẩy dữ liệu đầu ngày" ghi file lệch chỗ, POS không tải được
□ Cổng nginx `proxy_pass` khớp `ASPNETCORE_URLS` trong từng systemd unit
□ nginx -t OK → systemctl reload nginx
□ systemctl status pos-api pos-web = active (running); docker ps (Worker) = healthy
□ POS.Web: F12 thấy WebSocket /_blazor = 101 Switching Protocols
□ PROD ổn định → tắt EnableDetailedErrors (POS.Web)
```

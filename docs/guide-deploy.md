# Hướng dẫn triển khai UAT / PROD (nginx + Docker)

> Dành cho DevOps. Áp dụng cho 3 service: **POS.Api**, **POS.Web**, **POS.Worker**.
> Mỗi service đã có đủ 3 môi trường: `Development` / `UAT` / `Production`.

---

## 1. Tổng quan

| Service | Dockerfile | Cổng trong container | Biến môi trường chọn config | Sau nginx? |
|---|---|---|---|---|
| **POS.Api** | `Dockerfile` (gốc) | `80` | `ASPNETCORE_ENVIRONMENT` | ✅ |
| **POS.Web** | `src/POS.Web/Dockerfile` | `8080` | `ASPNETCORE_ENVIRONMENT` | ✅ |
| **POS.Worker** | `Dockerfile.worker` | — (không HTTP) | `DOTNET_ENVIRONMENT` | ❌ |

**Giá trị biến môi trường:** `UAT` hoặc `Production` → nạp tương ứng `appsettings.UAT.json` / `appsettings.Production.json`.

> ⚠️ POS.Worker dùng `DOTNET_ENVIRONMENT` (KHÔNG phải `ASPNETCORE_ENVIRONMENT`).

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
trường của shell chạy lệnh này (giống §2 mục mã hóa credentials).

---

## 3. Build & chạy container

### 3.1. POS.Api

```bash
# Build
docker build -t pos-api:uat -f Dockerfile .

# Run (UAT)
docker run -d --name pos-api-uat \
  -e ASPNETCORE_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  -e POS_SECRET_KEY="${POS_SECRET_KEY}" \
  --add-host host.docker.internal:host-gateway \
  -p 5001:80 \
  -v $(pwd)/logs:/app/logs \
  -v /srv/pos/uat/ftpbluepos:/app/ftpbluepos \
  --restart unless-stopped \
  pos-api:uat
```
> PROD: đổi `ASPNETCORE_ENVIRONMENT=Production`, tag `pos-api:prod`, tên container khác. Cổng host (`5001`) phải khớp `proxy_pass` trong nginx.
> `-e POS_SECRET_KEY=...`: chỉ cần khi `appsettings.{UAT|Production}.json` có token `enc:...` (xem §C4 `docs/ROLLOUT.md`) — bỏ qua nếu file còn plaintext.
> ⚠️ **PROD đổi path mount ftpbluepos**: dùng `-v /srv/pos/ftpbluepos:/app/ftpbluepos` (KHÔNG có tiền tố
> `uat`). UAT và PROD chạy `docker run` trên **CÙNG một host** này (khác port/tên container) — nếu dùng
> chung 1 thư mục host, dữ liệu test UAT sẽ lẫn vào master-data/sale file PROD thật. Tạo + cấp quyền 2
> thư mục này bằng `deploy/linux/setup-pos-dirs.sh` (xem `docs/deploy/ubuntu-guide.md`) TRƯỚC khi chạy
> `docker run` lần đầu.

### 3.2. POS.Web

```bash
# Build
docker build -t pos-web:uat -f src/POS.Web/Dockerfile .

# Run (UAT) — container nghe cổng 8080
docker run -d --name pos-web-uat \
  -e ASPNETCORE_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  -e POS_SECRET_KEY="${POS_SECRET_KEY}" \
  --add-host host.docker.internal:host-gateway \
  -p 5002:8080 \
  -v $(pwd)/logs:/app/logs \
  -v /srv/pos/uat/ftpbluepos:/app/ftpbluepos \
  -v pos-web-dpkeys-uat:/home/app/.aspnet/DataProtection-Keys \
  --restart unless-stopped \
  pos-web:uat
```
> ⚠️ Volume `DataProtection-Keys` BẮT BUỘC giữ qua các lần rebuild — nếu mất key, cookie đăng nhập cũ vô hiệu (user phải login lại).
> Cổng host (`5002`) phải khớp `proxy_pass` trong `nginx/pos-web.uat.conf`.
> `-e POS_SECRET_KEY=...`: khóa AES giải mã `enc:...` — **cùng giá trị** với khóa dùng cho POS.Api (khóa dùng chung, xem §C4 `docs/ROLLOUT.md`). Bỏ qua nếu appsettings còn plaintext.
> ⚠️ **Mount `ftpbluepos` dùng CHUNG với POS.Api** (`AppSettings:FtpRootPath` = `/app/ftpbluepos` ở
> cả 2 project) — nút "Đẩy dữ liệu đầu ngày" (`/catalog/pos-setup`) ghi file master-data vào đây, máy
> POS tải lại qua `DowloadFileStream` của POS.Api; thiếu mount này thì 2 container không cùng thư mục
> vật lý, file sinh ra ở POS.Web sẽ không thấy được ở POS.Api. **PROD đổi path** giống §3.1: dùng
> `-v /srv/pos/ftpbluepos:/app/ftpbluepos` (KHÔNG tiền tố `uat` — UAT/PROD chạy chung host, không dùng
> chung thư mục). Tạo + cấp quyền bằng `deploy/linux/setup-pos-dirs.sh` (xem
> `docs/deploy/ubuntu-guide.md`) TRƯỚC khi chạy `docker run` lần đầu.

### 3.3. POS.Worker

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
  -v $(pwd)/logs:/app/logs \
  -v /srv/pos/uat/ftpbluepos:/app/ftpbluepos \
  --restart unless-stopped \
  pos-worker:uat
```
> PROD: đổi `DOTNET_ENVIRONMENT=Production`, tag `pos-worker:prod`, tên container khác — **và đổi path
> mount** sang `-v /srv/pos/ftpbluepos:/app/ftpbluepos` (KHÔNG tiền tố `uat`, lý do giống POS.Api ở §3.1
> — UAT/PROD chạy chung host, không được dùng chung thư mục). `PosFileImportWorker` quét
> `SyncDataPos/Sale/Kafka` bên trong thư mục này — đúng folder mà POS.Api (`UploadFileSale`) đang ghi
> file sale vào, xem `docs/deploy/ubuntu-guide.md` để biết chi tiết + rủi ro đã biết của việc dùng chung.

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
# 1. Container chạy + healthy
docker ps                          # cột STATUS = "healthy"
docker logs --tail 50 pos-web-uat  # không có exception lúc khởi động

# 2. Health endpoint (qua container)
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
# Build image mới → dừng container cũ → chạy container mới (cùng tên/volume)
docker build -t pos-web:uat -f src/POS.Web/Dockerfile .
docker stop pos-web-uat && docker rm pos-web-uat
docker run -d --name pos-web-uat ...   # lệnh run như mục 3.2 (GIỮ NGUYÊN volume dp-keys)
```
> Không cần reload nginx khi chỉ đổi image (cổng không đổi).
> Sau re-deploy, user POS.Web nên hard refresh (`Ctrl+Shift+R`) để nạp client mới.

---

## 7. Rollback nhanh

```bash
# Giữ tag image cũ trước mỗi lần deploy (vd pos-web:uat-prev)
docker stop pos-web-uat && docker rm pos-web-uat
docker run -d --name pos-web-uat ... pos-web:uat-prev   # chạy lại image cũ
```

---

## Checklist deploy (tóm tắt)

```
□ Điền hết placeholder <...> trong appsettings.{UAT|Production}.json
□ Chạy POS.DbMigrator --verify → xử lý Track B thiếu → --whatif → --apply (xem §2.5) — TRƯỚC khi
  build/run container mới
□ Đã chạy deploy/linux/setup-pos-dirs.sh cho đúng môi trường (PROD: mặc định /srv/pos; UAT: /srv/pos/uat
  — KHÔNG dùng chung, xem docs/deploy/ubuntu-guide.md)
□ Nếu appsettings có token enc:... → set -e POS_SECRET_KEY=... khi docker run (POS.Api + POS.Web, cùng khóa)
□ Build image đúng Dockerfile từng service
□ Run với đúng biến môi trường (Api/Web: ASPNETCORE_ENVIRONMENT | Worker: DOTNET_ENVIRONMENT)
□ POS.Web: mount volume DataProtection-Keys (giữ qua rebuild)
□ POS.Web: mount /app/ftpbluepos (CHUNG thư mục với POS.Api, xem §3.2) — thiếu → nút "Đẩy dữ liệu
  đầu ngày" ghi file lệch chỗ, POS không tải được
□ Cổng host (-p) khớp proxy_pass trong nginx
□ nginx -t OK → systemctl reload nginx
□ docker ps = healthy + /health trả "healthy"
□ POS.Web: F12 thấy WebSocket /_blazor = 101 Switching Protocols
□ PROD ổn định → tắt EnableDetailedErrors (POS.Web)
```

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

> PROD: `appsettings.Production.json` đã có sẵn giá trị thật — chỉ kiểm tra lại trước khi build.
> Khi UAT/PROD ổn định: đặt `WebApp:EnableDetailedErrors = false` trong `appsettings.Production.json` (POS.Web) để không lộ stack trace.

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
  --add-host host.docker.internal:host-gateway \
  -p 5001:80 \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  pos-api:uat
```
> PROD: đổi `ASPNETCORE_ENVIRONMENT=Production`, tag `pos-api:prod`, tên container khác. Cổng host (`5001`) phải khớp `proxy_pass` trong nginx.

### 3.2. POS.Web

```bash
# Build
docker build -t pos-web:uat -f src/POS.Web/Dockerfile .

# Run (UAT) — container nghe cổng 8080
docker run -d --name pos-web-uat \
  -e ASPNETCORE_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  --add-host host.docker.internal:host-gateway \
  -p 5002:8080 \
  -v $(pwd)/logs:/app/logs \
  -v pos-web-dpkeys-uat:/home/app/.aspnet/DataProtection-Keys \
  --restart unless-stopped \
  pos-web:uat
```
> ⚠️ Volume `DataProtection-Keys` BẮT BUỘC giữ qua các lần rebuild — nếu mất key, cookie đăng nhập cũ vô hiệu (user phải login lại).
> Cổng host (`5002`) phải khớp `proxy_pass` trong `nginx/pos-web.uat.conf`.

### 3.3. POS.Worker

```bash
# Build
docker build -t pos-worker:uat -f Dockerfile.worker .

# Run (UAT) — không expose cổng
docker run -d --name pos-worker-uat \
  -e DOTNET_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  --add-host host.docker.internal:host-gateway \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  pos-worker:uat
```

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
□ Build image đúng Dockerfile từng service
□ Run với đúng biến môi trường (Api/Web: ASPNETCORE_ENVIRONMENT | Worker: DOTNET_ENVIRONMENT)
□ POS.Web: mount volume DataProtection-Keys (giữ qua rebuild)
□ Cổng host (-p) khớp proxy_pass trong nginx
□ nginx -t OK → systemctl reload nginx
□ docker ps = healthy + /health trả "healthy"
□ POS.Web: F12 thấy WebSocket /_blazor = 101 Switching Protocols
□ PROD ổn định → tắt EnableDetailedErrors (POS.Web)
```

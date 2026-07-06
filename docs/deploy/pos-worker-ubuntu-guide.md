# Deploy POS.Worker trên Ubuntu (Docker) — UAT & PROD

> Kế thừa từ Docker setup có sẵn (`Dockerfile.worker`, `docker-compose.yml`, `docs/guide-deploy.md`
> §3.3). File này là **runbook thao tác** dành riêng cho POS.Worker, chạy **song song** với POS.Web
> (đã có `nginx/pos-web.conf` đứng trước) trên **cùng 1 Ubuntu host**. Không lặp lại nội dung đã có ở
> nơi khác — chỉ trỏ tới và bổ sung phần còn thiếu (đặc biệt: `appsettings.UAT.json` cho Worker).

## 0. Vì sao KHÔNG có nginx cho POS.Worker

`POS.Worker` là `Microsoft.NET.Sdk.Worker` — **không có Kestrel/HTTP endpoint nào** (xác nhận qua
`Dockerfile.worker`: không `EXPOSE`, không `HEALTHCHECK`; `docker-compose.yml`: service `worker`
không có `ports:`). Vì vậy nginx **không đứng trước** POS.Worker như cách nó đứng trước POS.Web
(`location /_blazor`, `location /`...) — nginx trong dự án này chỉ phục vụ POS.Web (và tùy chọn
POS.Api, REST thuần, xem `docs/guide-deploy.md` §4.2). POS.Worker chạy **headless**, giám sát qua
log + heartbeat Redis, không qua HTTP.

Cơ chế "giữ sống + tự khởi động lại" tương đương `deploy/windows/POS.Worker.Task.xml`
(Windows Task Scheduler) trên Ubuntu là **Docker**, không phải nginx hay systemd riêng:

| Windows Task Scheduler (`POS.Worker.Task.xml`) | Docker trên Ubuntu |
|---|---|
| `<BootTrigger>` — chạy lúc máy khởi động | Docker daemon tự chạy lúc boot (`systemctl enable docker`, mặc định đã bật trên Ubuntu cài qua apt) |
| `<RestartOnFailure><Interval>PT1M</Interval><Count>999</Count>` | `--restart unless-stopped` — Docker tự khởi động lại container khi process thoát, không giới hạn số lần, và tự chạy lại khi Docker daemon khởi động |
| `<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>` | 1 container = 1 tiến trình, không có khái niệm "chạy chồng" như Task Scheduler |
| `run-worker.bat` → `logs\worker-console.log` | `docker logs pos-worker-{prod\|uat}` (stdout/stderr container) |
| Chạy dưới tài khoản SYSTEM | Chạy dưới user `app` (UID:GID `1654:1654`, non-root, cố định trong image `mcr.microsoft.com/dotnet/aspnet:10.0`) |

Kết luận: import `POS.Worker.Task.xml` bằng `schtasks` ↔ chạy `docker run ... --restart unless-stopped`
trên Ubuntu — cùng mục đích, khác cơ chế.

## 1. Hiện trạng hạ tầng đã có sẵn (đã khảo sát, không cần tạo lại)

| Thành phần | File | Trạng thái |
|---|---|---|
| Build image | `Dockerfile.worker` (gốc repo) | Sẵn sàng — multi-stage, publish framework `Microsoft.AspNetCore.App` (cần vì `POS.Infrastructure` dùng `IHttpClientFactory`) |
| Chạy trong SIT | `docker-compose.yml` (service `worker`) | Sẵn sàng — dùng khi test cả cụm SIT (Api + SQL Server + Worker cùng network) |
| Lệnh `docker run` UAT/PROD chuẩn | `docs/guide-deploy.md` §3.3 | Sẵn sàng — dùng làm mẫu ở mục 3 bên dưới |
| Thư mục dùng chung `ftpbluepos` | `deploy/linux/setup-pos-dirs.sh` + `docs/deploy/ubuntu-guide.md` | Sẵn sàng — chạy 1 lần/host |
| Cấu hình `FileImport` (path, poll interval...) | `docs/ROLLOUT.md` §O2 | Đã mô tả đầy đủ |
| `appsettings.Production.json` (Worker) | `src/POS.Worker/appsettings.Production.json` | **Đã đúng** — khớp chính xác với `src/POS.Api/appsettings.Production.json` (cùng `Data Source=mssql_2019,1433`, `RabbitMQ:Host=host.docker.internal`, `Redis:SentinelHosts=172.17.0.1:6379`) vì cả 2 cùng chạy trong `docker-compose.yml`. **Không cần sửa gì cho PROD.** |
| `appsettings.UAT.json` (Worker) | `src/POS.Worker/appsettings.UAT.json` | **Vừa tạo mới** (file này trước đây không tồn tại — xem mục 2) |

> ⚠️ **Lưu ý phạm vi**: `POS.Api` và `POS.Web` cũng đang thiếu `appsettings.UAT.json` tương tự — đây
> là lỗ hổng có sẵn trong repo, **ngoài phạm vi việc deploy Worker lần này**. Nếu cần deploy UAT đầy
> đủ cho cả 3 service, tạo thêm 2 file tương tự cho Api/Web theo đúng placeholder đã mô tả ở
> `docs/guide-deploy.md` §2.

## 2. `appsettings.UAT.json` cho POS.Worker — điền placeholder trước khi build

File `src/POS.Worker/appsettings.UAT.json` đã được tạo với cấu trúc giống hệt
`appsettings.Production.json`, khác ở chỗ toàn bộ giá trị hạ tầng thay bằng placeholder. **Path
`FileImport` giữ nguyên `/app/ftpbluepos/...`** (container-side, không đổi theo môi trường — chỉ có
bind-mount phía host là khác giữa UAT/PROD, xem mục 3).

Điền các placeholder sau (giống quy ước `docs/guide-deploy.md` §2 dùng cho Api/Web):

| Placeholder | Ý nghĩa | Ví dụ |
|---|---|---|
| `<UAT_REDIS_HOST>` | Redis UAT (host:port) | `172.17.0.1:6379` nếu Redis native cùng host, hoặc IP riêng |
| `<UAT_RABBIT_HOST>` | RabbitMQ UAT | `host.docker.internal` nếu RabbitMQ native cùng host |
| `<UAT_RABBIT_PASSWORD>` | Mật khẩu RabbitMQ UAT | — |
| `<UAT_SQL_HOST>` | SQL Server UAT (`host,port`) | `127.0.0.1,14333` nếu dùng chung SQL container với Api/Web UAT |
| `<UAT_SQL_USER>` / `<UAT_SQL_PASSWORD>` | Tài khoản SQL dùng cho 8/9 connection string (trừ EInvoice) | `sa` / ... |
| `<UAT_EINVOICE_USER>` / `<UAT_EINVOICE_PASSWORD>` | Riêng connection string `EInvoice` | — |
| `<UAT_KAFKA_HOST>` | `BootstrapServers` (Kafka) | — |

> Worker **chưa có hook giải mã `enc:...`** (khác Api/Web — xem `docs/architecture/appsetting.md`).
> Điền **plaintext thật** vào các placeholder trên, không dùng `enc:...` cho file này.

## 3. Build & chạy container

```bash
cd /đường-dẫn-tới-repo-code   # thư mục chứa Dockerfile.worker

# ── PROD ──────────────────────────────────────────────────────────────────
docker build -t pos-worker:prod -f Dockerfile.worker .

docker run -d --name pos-worker-prod \
  -e DOTNET_ENVIRONMENT=Production \
  -e TZ=Asia/Ho_Chi_Minh \
  --add-host host.docker.internal:host-gateway \
  -v $(pwd)/logs:/app/logs \
  -v /srv/pos/ftpbluepos:/app/ftpbluepos \
  --restart unless-stopped \
  pos-worker:prod

# ── UAT (đổi tag/tên container + path ftpbluepos + biến môi trường) ───────
docker build -t pos-worker:uat -f Dockerfile.worker .

docker run -d --name pos-worker-uat \
  -e DOTNET_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  --add-host host.docker.internal:host-gateway \
  -v $(pwd)/logs:/app/logs \
  -v /srv/pos/uat/ftpbluepos:/app/ftpbluepos \
  --restart unless-stopped \
  pos-worker:uat
```

> Không cần `-p` (Worker không mở cổng nào). Không cần `-e POS_SECRET_KEY=...` (Worker chưa mã hóa
> credentials). **PROD dùng `/srv/pos/ftpbluepos`, UAT dùng `/srv/pos/uat/ftpbluepos`** — đúng lý do
> đã nêu ở `docs/deploy/ubuntu-guide.md` (UAT/PROD chạy chung 1 Ubuntu host, phải tách thư mục để
> không lẫn dữ liệu sale/master-data thật). Cả 2 thư mục phải đã được tạo trước bằng
> `deploy/linux/setup-pos-dirs.sh` (PROD: mặc định; UAT: `sudo ./deploy/linux/setup-pos-dirs.sh /srv/pos/uat`).

## 4. Kiểm chứng sau deploy

```bash
# 1. Container đang chạy (không có cột "healthy" vì Worker không có HEALTHCHECK)
docker ps --filter name=pos-worker

# 2. Log khởi động — kỳ vọng thấy cả 3 dòng sau (không có exception)
docker logs --tail 50 pos-worker-prod   # hoặc pos-worker-uat
#   PosSalesConsumer ...
#   Rpt_ReportSaleDetail_Insert ...
#   [PosFileImport] Started ...

# 3. Mount đúng chỗ (bind mount, không phải named volume)
docker inspect pos-worker-prod --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
#   /srv/pos/ftpbluepos -> /app/ftpbluepos
```

**Test luồng file-import** (dùng đúng quyền `posops`, không cần sudo — xem
`docs/deploy/ubuntu-guide.md` §4.3):

```bash
cp mau-hop-le.zip /srv/pos/ftpbluepos/SyncDataPos/Sale/Kafka/   # PROD
# hoặc /srv/pos/uat/ftpbluepos/SyncDataPos/Sale/Kafka/          # UAT
```

Trong ≤ 30s (`PollIntervalSeconds`): file biến mất (thành công) hoặc rơi vào
`.../SyncDataPos/Sale/error/` (lỗi định dạng — tên phải đúng `Type_PosNo_TransactionId.txt` bên
trong zip, xem `docs/ROLLOUT.md` §O2).

**Heartbeat Redis** (DB 2):
```bash
redis-cli -n 2 GET Worker:Heartbeat:PosFileImport
```
Giá trị phải vừa cập nhật (mỗi chu kỳ poll).

## 5. Cập nhật phiên bản mới (re-deploy)

```bash
docker build -t pos-worker:prod -f Dockerfile.worker .
docker stop pos-worker-prod && docker rm pos-worker-prod
docker run -d --name pos-worker-prod ...   # lệnh run như mục 3 (giữ nguyên volume/tên)
```
Worker không giữ state trong container (không có DataProtection-Keys như POS.Web) nên re-deploy đơn
giản hơn — không cần lo mất key/session.

## 6. Rollback nhanh

```bash
# Giữ tag image cũ trước mỗi lần deploy (vd pos-worker:prod-prev)
docker stop pos-worker-prod && docker rm pos-worker-prod
docker run -d --name pos-worker-prod ... pos-worker:prod-prev
```

## 7. Vận hành & rủi ro đã biết

- **Log**: Serilog → Elasticsearch (`pos-worker-logs-*`) + file log tại `/app/logs` (bind-mounted ra
  `./logs` trên host). Log khởi động/crash: `docker logs pos-worker-{prod|uat}`.
- **Dừng/chạy/gỡ**: `docker stop|start|rm pos-worker-{prod|uat}` (thay cho `schtasks /End|/Run|/Delete`).
- **Rủi ro chung thư mục `Sale/Kafka` với `UploadFileSale` (POS.Api)** và **dọn dẹp
  `error/`/`BackupFiles/`**: xem đầy đủ tại `docs/deploy/ubuntu-guide.md` §6-7 — không lặp lại ở đây.
- **Đổi cấu hình `FileImport`** (bật/tắt, đổi `PollIntervalSeconds`...): sửa
  `appsettings.{Production|UAT}.json` tương ứng → build lại image → re-deploy (mục 5). Vì giá trị đã
  bake vào image lúc publish, **không sửa file trực tiếp trong container đang chạy**.

## Checklist

```
□ deploy/linux/setup-pos-dirs.sh đã chạy cho đúng môi trường (PROD: mặc định; UAT: /srv/pos/uat)
□ src/POS.Worker/appsettings.UAT.json đã điền hết placeholder <UAT_...> (chỉ cần cho UAT)
□ docker build -t pos-worker:{prod|uat} -f Dockerfile.worker . thành công
□ docker run với đúng -e DOTNET_ENVIRONMENT, đúng path -v ftpbluepos (PROD ≠ UAT)
□ docker ps → container Up; docker logs → thấy 3 dòng khởi động, không exception
□ Test thả 1 zip hợp lệ vào Sale/Kafka/ → biến mất hoặc vào error/ trong ≤30s
□ Redis Worker:Heartbeat:PosFileImport vừa cập nhật
□ dotnet test tests/POS.ContractTests vẫn xanh (không đổi DTO/DI ở việc deploy này)
```

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Quy trình deploy đầy đủ POS.Api/POS.Web/POS.Worker (build, `docker run`, nginx, `POS_SECRET_KEY`) | `docs/guide-deploy.md` |
| Thư mục dùng chung `ftpbluepos` (POS.Api ↔ POS.Worker) | `docs/deploy/ubuntu-guide.md` |
| Cấu hình `FileImport`, định dạng file, rủi ro vận hành | `docs/ROLLOUT.md` §O2 |
| Quy tắc mã hóa `enc:`/`POS_SECRET_KEY` (chưa áp dụng cho Worker) | `docs/architecture/appsetting.md` |
| Deploy POS.Worker trên Windows (Task Scheduler, dev/bare-metal) | `deploy/windows/README.md` |
| nginx cho POS.Web (không áp dụng cho Worker) | `nginx/pos-web.conf`, `nginx/pos-web.uat.conf` |

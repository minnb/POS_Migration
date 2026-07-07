# Deploy POS.Worker trên Ubuntu — Model A (Cronjob Host) & Model B (Docker)

> Kế thừa từ Docker setup có sẵn (`Dockerfile.worker`, `docker-compose.yml`, `docs/guide-deploy.md`
> §3.3). File này là **runbook thao tác** dành riêng cho POS.Worker, chạy **song song** với POS.Web
> (đã có `nginx/pos-web.conf` đứng trước) trên **cùng 1 Ubuntu host**. Không lặp lại nội dung đã có ở
> nơi khác — chỉ trỏ tới và bổ sung phần còn thiếu (đặc biệt: `appsettings.UAT.json` cho Worker).

> **Feature toggle `WorkerRoles`** (`WorkerRolesOptions.cs`) tách POS.Worker thành **2 mô hình chạy
> đồng thời**, KHÔNG phải 2 project riêng — cùng 1 image/binary, khác cách chạy + cấu hình:
>
> | | Model A — Cronjob host | Model B — Docker container |
> |---|---|---|
> | Đảm nhiệm | `PosFileImportService` (file .zip, dùng chung `ftpbluepos` với Web/Api) | `PosSalesConsumerWorker` (RabbitMQ) + `Rpt_ReportSaleDetail_Insert` (SQL) + heartbeat |
> | Cách chạy | `dotnet POS.Worker.dll --run-once` qua crontab, chạy 1 chu kỳ rồi thoát | `docker run`/`docker-compose` service `pos-worker`, process dài hạn |
> | Cấu hình | `appsettings.CronHost.json` (`DOTNET_ENVIRONMENT=CronHost`) | `appsettings.Production.json`/`UAT.json` + env `WorkerRoles__*` override |
> | Mục 3-4 dưới đây | — xem **mục 5** | Docker (giữ nguyên nội dung gốc) |
>
> Mục 1-4 bên dưới mô tả Model B (Docker) như bản gốc; **mục 5** (mới) mô tả riêng Model A.

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

## 3. Build & chạy container (Model B — RabbitMQ + SQL, KHÔNG xử lý file)

> Container **KHÔNG** còn mount `ftpbluepos` — `WorkerRoles:EnableFileProcessing=false` (đặt cứng
> trong `appsettings.Production.json`/`UAT.json`, không cần lặp lại qua `-e` nếu dùng file đúng
> bản mới; ví dụ dưới đây vẫn khai rõ qua env để minh bạch và để dễ đổi khi cần debug tạm thời).

```bash
cd /đường-dẫn-tới-repo-code   # thư mục chứa Dockerfile.worker

# ── PROD ──────────────────────────────────────────────────────────────────
docker build -t pos-worker:prod -f Dockerfile.worker .

docker run -d --name pos-worker-prod \
  -e DOTNET_ENVIRONMENT=Production \
  -e TZ=Asia/Ho_Chi_Minh \
  -e WorkerRoles__EnableFileProcessing=false \
  -e WorkerRoles__EnableRabbitMQConsumer=true \
  -e WorkerRoles__EnableSqlReportWorker=true \
  -e WorkerRoles__EnableHeartbeat=true \
  --add-host host.docker.internal:host-gateway \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  pos-worker:prod

# ── UAT (đổi tag/tên container + biến môi trường) ─────────────────────────
docker build -t pos-worker:uat -f Dockerfile.worker .

docker run -d --name pos-worker-uat \
  -e DOTNET_ENVIRONMENT=UAT \
  -e TZ=Asia/Ho_Chi_Minh \
  -e WorkerRoles__EnableFileProcessing=false \
  -e WorkerRoles__EnableRabbitMQConsumer=true \
  -e WorkerRoles__EnableSqlReportWorker=true \
  -e WorkerRoles__EnableHeartbeat=true \
  --add-host host.docker.internal:host-gateway \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  pos-worker:uat
```

> Không cần `-p` (Worker không mở cổng nào). Không cần `-e POS_SECRET_KEY=...` (Worker chưa mã hóa
> credentials). Không cần mount `ftpbluepos` nữa trong container này — việc xử lý file đã chuyển
> sang **Model A** (mục 5).

## 4. Kiểm chứng sau deploy (Model B)

```bash
# 1. Container đang chạy (không có cột "healthy" vì Worker không có HEALTHCHECK)
docker ps --filter name=pos-worker

# 2. Log khởi động — kỳ vọng thấy 2 dòng sau (không có exception, KHÔNG có "[PosFileImport] Started"
#    vì EnableFileProcessing=false trong container này)
docker logs --tail 50 pos-worker-prod   # hoặc pos-worker-uat
#   PosSalesConsumer ...
#   Rpt_ReportSaleDetail_Insert ...
```

**Heartbeat Redis** (DB 2):
```bash
redis-cli -n 2 GET Worker:Heartbeat:PosSalesConsumer
```
Giá trị phải vừa cập nhật (mỗi 15s — xem `WorkerHeartbeatService`). **Không** kiểm tra
`Worker:Heartbeat:PosFileImport` ở container Model B — key này chỉ được ghi bởi Model A (mục 5).

## 5. Model A — Cronjob thật trên Ubuntu host (xử lý file, KHÔNG dùng Docker)

> Chạy `PosFileImportService` bằng `dotnet POS.Worker.dll --run-once` theo lịch crontab — **1 chu kỳ
> quét rồi thoát**, không phải process residency. Cần publish riêng POS.Worker ra host (framework-
> dependent, không đóng gói Docker image) vì phải chạy native để đọc/ghi trực tiếp
> `/srv/pos/ftpbluepos` bằng đường dẫn host (không qua bind-mount container).

### 5.1. Cài .NET runtime trên host

```bash
# Ubuntu 22.04/24.04 — cài ASP.NET Core Runtime 10 (POS.Infrastructure dùng IHttpClientFactory)
sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-10.0
```

### 5.2. Publish & đặt binary

```bash
cd /đường-dẫn-tới-repo-code
dotnet publish src/POS.Worker/POS.Worker.csproj -c Release -o /srv/pos/app/worker
```

### 5.3. Quyền — user chạy cron phải thuộc group `posops`

`ftpbluepos` là `chmod 2770` (setgid, không world-writable — xem `deploy/linux/setup-pos-dirs.sh`).
User chạy crontab phải nằm trong group `posops` (gid 1654):

```bash
sudo usermod -aG posops <username>
# đăng nhập lại hoặc `newgrp posops` để nhận quyền ngay trong phiên hiện tại
```

### 5.4. Script wrapper + crontab

Script `deploy/linux/run-worker-file-import-once.sh` (đã có sẵn trong repo) — copy cùng thư mục
publish hoặc chạy thẳng từ repo, chú ý `WORKER_DIR` phải khớp nơi publish ở bước 5.2:

```bash
cp deploy/linux/run-worker-file-import-once.sh /srv/pos/app/worker/
chmod +x /srv/pos/app/worker/run-worker-file-import-once.sh

mkdir -p /srv/pos/logs/worker-cron
sudo chown <username>:posops /srv/pos/logs/worker-cron
```

`crontab -e` (dưới user thuộc group `posops`):

```
* * * * * /srv/pos/app/worker/run-worker-file-import-once.sh >> /srv/pos/logs/worker-cron/cron.log 2>&1
```

### 5.5. Kiểm chứng Model A

```bash
# Chạy tay 1 lần trước khi tin tưởng crontab
DOTNET_ENVIRONMENT=CronHost dotnet /srv/pos/app/worker/POS.Worker.dll --run-once
echo $?   # 0 = OK, khác 0 = lỗi (xem log)

# Test luồng thật — thả 1 zip hợp lệ vào đúng InboxFolder host-path
cp mau-hop-le.zip /srv/pos/ftpbluepos/SyncDataPos/Sale/Kafka/
# chạy tay hoặc chờ cron (≤1 phút) — file biến mất (OK) hoặc rơi vào .../Sale/error/ (lỗi định dạng)

# Sau khi cài crontab — xác nhận có chạy đều, không bị flock chặn liên tục
tail -f /srv/pos/logs/worker-cron/cron.log
```

> Model A **không** ghi heartbeat Redis (`WorkerRoles:EnableHeartbeat=false` trong
> `appsettings.CronHost.json`) — giám sát qua **exit code + log cron** (`cron.log`), không qua Redis.
> Nếu cần alert khi cron ngừng chạy, theo dõi mốc thời gian sửa đổi cuối của `cron.log`.

## 6. Cập nhật phiên bản mới (re-deploy)

**Model B (Docker):**
```bash
docker build -t pos-worker:prod -f Dockerfile.worker .
docker stop pos-worker-prod && docker rm pos-worker-prod
docker run -d --name pos-worker-prod ...   # lệnh run như mục 3 (giữ nguyên volume/tên)
```
Worker không giữ state trong container (không có DataProtection-Keys như POS.Web) nên re-deploy đơn
giản hơn — không cần lo mất key/session.

**Model A (cron host):**
```bash
dotnet publish src/POS.Worker/POS.Worker.csproj -c Release -o /srv/pos/app/worker
```
Publish đè trực tiếp — không cần dừng gì (giữa 2 lần cron kích hoạt là "khoảng nghỉ" tự nhiên; nếu
muốn chắc chắn không ghi đè giữa lúc 1 chu kỳ đang chạy, publish ra thư mục tạm rồi `mv` đổi tên).

## 7. Rollback nhanh

**Model B:**
```bash
# Giữ tag image cũ trước mỗi lần deploy (vd pos-worker:prod-prev)
docker stop pos-worker-prod && docker rm pos-worker-prod
docker run -d --name pos-worker-prod ... pos-worker:prod-prev
```

**Model A:** giữ bản publish cũ ở thư mục khác (vd `/srv/pos/app/worker-prev`) trước khi publish đè;
rollback = đổi `WORKER_DIR` trong script hoặc `cp -r` bản cũ đè lại.

## 8. Vận hành & rủi ro đã biết

- **Log**: Serilog → Elasticsearch (`pos-worker-logs-*` cho Model B, `pos-worker-cron-logs-*` cho
  Model A) + file log tại `/app/logs` (Model B, bind-mounted ra `./logs`) hoặc
  `/srv/pos/logs/worker-cron` (Model A). Log khởi động/crash Model B: `docker logs
  pos-worker-{prod|uat}`; Model A: `/srv/pos/logs/worker-cron/cron.log`.
- **Dừng/chạy/gỡ Model B**: `docker stop|start|rm pos-worker-{prod|uat}`. **Model A**: xóa dòng
  crontab (`crontab -e`) — không có process nào để "stop" (mỗi lượt chạy rồi tự thoát).
- **Rủi ro chung thư mục `Sale/Kafka` với `UploadFileSale` (POS.Api)** và **dọn dẹp
  `error/`/`BackupFiles/`**: xem đầy đủ tại `docs/deploy/ubuntu-guide.md` §6-7 — không lặp lại ở đây.
  Áp dụng cho Model A dù chạy ngoài Docker (cùng thư mục vật lý `/srv/pos/ftpbluepos`).
- **`flock -n`** trong script Model A bỏ qua lượt cron mới nếu lượt trước chưa xong — nếu thấy
  `cron.log` có khoảng trống bất thường (>vài phút không có dòng mới), kiểm tra tiến trình
  `dotnet POS.Worker.dll --run-once` có bị treo (SQL/network chậm) không.
- **Đổi cấu hình `FileImport`/`WorkerRoles`**: Model B sửa `appsettings.{Production|UAT}.json` (hoặc
  override qua `-e WorkerRoles__*`) → build lại image → re-deploy (mục 6). Model A sửa
  `appsettings.CronHost.json` → `dotnet publish` lại (mục 6) — không sửa file trực tiếp trong thư
  mục đang publish khi cron có thể đang chạy.

## Checklist

```
Model B (Docker):
□ docker build -t pos-worker:{prod|uat} -f Dockerfile.worker . thành công
□ docker run với đúng -e DOTNET_ENVIRONMENT + WorkerRoles__EnableFileProcessing=false
□ docker ps → container Up; docker logs → thấy 2 dòng khởi động (PosSalesConsumer,
  Rpt_ReportSaleDetail_Insert), KHÔNG có "[PosFileImport] Started", không exception
□ Redis Worker:Heartbeat:PosSalesConsumer vừa cập nhật

Model A (cron host):
□ deploy/linux/setup-pos-dirs.sh đã chạy cho đúng môi trường (PROD: mặc định; UAT: /srv/pos/uat)
□ aspnetcore-runtime-10.0 đã cài trên host; dotnet publish ra /srv/pos/app/worker thành công
□ User chạy cron đã vào group posops (usermod -aG posops)
□ appsettings.CronHost.json đúng path /srv/pos/ftpbluepos/... + ConnectionStrings:CentralSale
□ Chạy tay 1 lần (--run-once) exit code 0, log "[Cron] PosFileImport run-once xong"
□ Test thả 1 zip hợp lệ vào Sale/Kafka/ → biến mất hoặc vào error/ trong ≤1 phút (qua cron)
□ crontab đã cài, cron.log có dòng mới đều đặn, không bị flock chặn kéo dài

Chung:
□ src/POS.Worker/appsettings.UAT.json đã điền hết placeholder <UAT_...> (chỉ cần cho UAT)
□ dotnet test tests/POS.ContractTests vẫn xanh (không đổi DTO/DI ở việc deploy này)
```

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Quy trình deploy đầy đủ POS.Api/POS.Web/POS.Worker (build, `docker run`, nginx, `POS_SECRET_KEY`) | `docs/guide-deploy.md` |
| Thư mục dùng chung `ftpbluepos` (POS.Api ↔ POS.Worker) | `docs/deploy/ubuntu-guide.md` |
| Cấu hình `FileImport`/`WorkerRoles`, định dạng file, rủi ro vận hành, 2 mô hình A/B | `docs/ROLLOUT.md` §O2 |
| Quy tắc mã hóa `enc:`/`POS_SECRET_KEY` (chưa áp dụng cho Worker) | `docs/architecture/appsetting.md` |
| Deploy POS.Worker trên Windows (Task Scheduler, dev/bare-metal) | `deploy/windows/README.md` |
| nginx cho POS.Web (không áp dụng cho Worker) | `nginx/pos-web.conf`, `nginx/pos-web.uat.conf` |
| Ba khuôn mẫu worker (timer/consumer/one-shot) + feature toggle `WorkerRoles` | `.claude/skills/worker/SKILLS.md` |

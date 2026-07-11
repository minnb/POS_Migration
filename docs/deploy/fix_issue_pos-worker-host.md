# Fix Issue — Triển khai POS.Worker trên Ubuntu host (`sit-uat-server`)

> Nhật ký thực tế các lỗi gặp phải + cách fix khi triển khai POS.Worker theo
> `docs/deploy/pos-worker-ubuntu-guide.md` trên host `sit-uat-server`. Đọc file này TRƯỚC khi
> deploy lại từ đầu trên host mới — tránh lặp lại đúng chuỗi lỗi đã gặp.

## Bối cảnh

Triển khai 2 phần trên cùng 1 Ubuntu host:
1. **Model B (Docker)** — container `pos-worker-prod` chạy `PosSalesConsumerWorker` +
   `Rpt_ReportSaleDetail_Insert` + heartbeat.
2. **Model C thực chất chạy bare-metal (systemd)** — service `pos-worker-prodhost.service` chạy
   `MasterDataZipGeneratorWorker`, định kỳ 2 phút/lần.

## Vấn đề 1 — Model B (Docker): SQL Server "not found or not accessible"

**Triệu chứng:**
```
SqlException: A network-related or instance-specific error occurred while establishing a
connection to SQL Server... (provider: TCP Provider, error: 40)
```

**Nguyên nhân:** `src/POS.Worker/appsettings.Production.json` hardcode
`Data Source=mssql_2019,14333` — `mssql_2019` là service name trong `docker-compose.yml`, chỉ
resolve được trong network do `docker compose up` tạo ra. Container `pos-worker-prod` được chạy
bằng `docker run` **độc lập** (không qua compose) → hostname không resolve được.

**Fix:** đổi `ConnectionStrings:*` + `SetDb:DB1` trong `appsettings.Production.json` sang
`Data Source=host.docker.internal,14333` — khớp pattern đã dùng sẵn cho `RabbitMQ:Host` trong
CHÍNH file này (route qua `--add-host host.docker.internal:host-gateway` đã có sẵn trong lệnh
`docker run` mẫu ở guide mục 3). Sau khi sửa: `docker build` lại image + `docker stop/rm/run` lại
container (KHÔNG chỉ sửa file trên host — `appsettings.Production.json` được COPY vào image lúc
build).

## Vấn đề 2 — `dotnet publish` báo lỗi quyền (`Access to the path ... is denied`)

**Triệu chứng:**
```
error MSB3021: Unable to copy file ".../Microsoft.Bcl.AsyncInterfaces.dll" to
"/var/www/posWeb/worker/Microsoft.Bcl.AsyncInterfaces.dll". Access to the path ... is denied.
```

**Nguyên nhân:** thư mục output `-o` chưa thuộc quyền ghi của user đang chạy `dotnet publish`
(dùng path tùy biến `/var/www/posWeb/worker` thay vì `/srv/pos/app/worker` như guide gốc).

**Fix:**
```bash
sudo mkdir -p /var/www/posWeb/worker
sudo chown -R "$USER":"$USER" /var/www/posWeb/worker
dotnet publish src/POS.Worker/POS.Worker.csproj -c Release -o /var/www/posWeb/worker
```

## Vấn đề 3 — systemd `status=217/USER`

**Triệu chứng:** service `pos-worker-prodhost.service` restart-loop liên tục, `Main process
exited, code=exited, status=217/USER`.

**Nguyên nhân:** user `posworker` khai báo ở `User=` trong unit file **chưa tồn tại** trên host —
bước tạo user (mục 9.3 guide) đã bị bỏ qua khi copy mẫu unit từ mục 3.5.

**Fix:**
```bash
sudo useradd -r -s /usr/sbin/nologin posworker
sudo usermod -aG posops posworker
sudo systemctl restart pos-worker-prodhost.service
```

## Vấn đề 4 — systemd `status=216/GROUP`

**Triệu chứng:** sau khi tạo `posworker`, service vẫn restart-loop,
`status=216/GROUP`.

**Nguyên nhân:** group `posops` khai báo ở `Group=` trong unit file **chưa tồn tại** — group này
do `deploy/linux/setup-pos-dirs.sh` tạo ra (gid cố định 1654, khớp UID:GID container `app`), nhưng
script chưa từng chạy thành công trên host này.

**Fix (gặp lại 2 lần trong quá trình deploy — đều cùng nguyên nhân "chưa chạy setup-pos-dirs.sh"):**
```bash
sudo ./deploy/linux/setup-pos-dirs.sh
getent group posops              # xác nhận: posops:x:1654:
sudo usermod -aG posops posworker
id posworker                     # xác nhận thấy 1654(posops) trong danh sách groups
sudo systemctl restart pos-worker-prodhost.service
```

## Vấn đề 5 — `sudo: ./deploy/linux/setup-pos-dirs.sh: command not found`

**Triệu chứng:** chạy đúng thư mục gốc repo, file tồn tại (`ls -la deploy/linux/` thấy đủ), vẫn
báo "command not found" thay vì "Permission denied".

**Nguyên nhân:** file có quyền `-rw-r--r--` — **thiếu execute bit**. Khi thiếu execute bit, shell/
`sudo` báo "command not found" thay vì "Permission denied" (dễ gây hiểu lầm là sai đường dẫn).
Thường xảy ra khi file được đưa lên host qua cách không giữ nguyên permission gốc trong git (vd
`scp`/copy thủ công thay vì `git clone`/`git pull`).

**Fix:**
```bash
chmod +x deploy/linux/setup-pos-dirs.sh deploy/linux/run-worker-file-import-once.sh
sudo ./deploy/linux/setup-pos-dirs.sh
```

## Vấn đề 6 — Model C (bare-metal) dùng nhầm `DOTNET_ENVIRONMENT=Production`

**Không phải lỗi đã xảy ra thật, nhưng phát hiện và chặn trước khi deploy:** guide gốc mục 9.4
hướng dẫn Model C dùng `DOTNET_ENVIRONMENT=Production`. Sau khi Vấn đề 1 được fix (SQL host đổi
sang `host.docker.internal`), giá trị này **chỉ resolve được trong container Docker** — nếu Model C
(chạy bare-metal thật, không phải container) cũng dùng `Production.json`, sẽ gặp lại đúng lỗi SQL
connection của Vấn đề 1, lần này theo chiều ngược lại.

**Fix:** Model C dùng `DOTNET_ENVIRONMENT=ProductionHost` (file `appsettings.ProductionHost.json`
đã có sẵn `127.0.0.1,14333` — đúng cho bare-metal) + override `WorkerRoles` qua `Environment=`
trong unit file để chuyển từ vai trò mặc định (RabbitMQ+SQL) sang `MasterDataZipGenerator`:
```ini
Environment=DOTNET_ENVIRONMENT=ProductionHost
Environment=WorkerRoles__EnableFileProcessing=false
Environment=WorkerRoles__EnableRabbitMQConsumer=false
Environment=WorkerRoles__EnableSqlReportWorker=false
Environment=WorkerRoles__EnableHeartbeat=true
Environment=WorkerRoles__EnableMasterDataZipGenerator=true
```
Đã cập nhật lại `docs/deploy/pos-worker-ubuntu-guide.md` mục 9.4 + bảng đầu file theo đúng fix này.

## Thay đổi cấu hình khác (không phải fix lỗi — theo yêu cầu vận hành)

`src/POS.Worker/appsettings.ProductionHost.json`: `MasterDataZipGenerator.IntervalSeconds`
300 → 120 (poll watermark mỗi 2 phút thay vì 5 phút).

## Giới hạn đã biết — chưa xử lý

`Program.cs` — cờ `--run-once` hiện **chỉ hard-code chạy `PosFileImportService`** (Model A), không
đọc `WorkerRoles`. Không có cách chạy `MasterDataZipGeneratorWorker` kiểu "chạy 1 lần rồi thoát"
qua crontab thật — hiện chỉ mô phỏng "định kỳ" bằng cách giảm `IntervalSeconds` trong daemon dài
hạn (systemd). Muốn có cron thật cho worker này cần sửa code (tách logic 1 chu kỳ thành method
gọi được riêng, theo mẫu `PosFileImportService.RunOnceAsync`) — ngoài phạm vi đợt fix này.

## Checklist rút gọn cho lần deploy Model C (bare-metal) tiếp theo trên host mới

```
□ git clone/pull đầy đủ repo (không copy chọn lọc file) — tránh mất quyền +x của .sh
□ chmod +x deploy/linux/*.sh (phòng trường hợp mất quyền thực thi khi transfer)
□ sudo ./deploy/linux/setup-pos-dirs.sh  → getent group posops phải ra posops:x:1654:
□ sudo useradd -r -s /usr/sbin/nologin posworker && sudo usermod -aG posops posworker
□ mkdir + chown thư mục publish cho đúng user trước khi dotnet publish
□ dotnet publish ra thư mục RIÊNG cho model này (không dùng chung với Model A cron)
□ Unit file: DOTNET_ENVIRONMENT=ProductionHost (KHÔNG phải Production) + WorkerRoles__* đúng vai trò
□ POS_SECRET_KEY trong Environment= khớp giá trị dùng chung 3 service khác
□ sudo systemctl daemon-reload && enable && start → status = active (running)
□ journalctl -u <service> -f không còn exception
□ redis-cli -n 0 GET Worker:Heartbeat:MasterDataZipGenerator vừa cập nhật theo IntervalSeconds mới
```

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Runbook đầy đủ 3 model (A/B/C) | `docs/deploy/pos-worker-ubuntu-guide.md` |
| Chi tiết root cause + code liên quan | `docs/CHANGELOG.md` mục [2026-07-11] "Fix POS.Worker không kết nối được SQL Server..." |
| Cơ chế watermark/quarantine của `MasterDataZipGeneratorWorker` | `.claude/rules/masterdata-sync.md` |

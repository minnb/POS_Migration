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

> **Cập nhật 2026-07-15 — `appsettings.ProductionHost.json` đã bị XOÁ, gộp vào
> `appsettings.CronHost.json`.** Model C nay dùng `DOTNET_ENVIRONMENT=CronHost` (dùng chung file
> với Model A) — `WorkerRoles__*` override trong `Environment=` giữ nguyên như trên. Chi tiết:
> `docs/deploy/pos-worker-ubuntu-guide.md` mục 9.4 + `docs/CHANGELOG.md` [2026-07-15].

## Vấn đề 7 (2026-07-14) — Model C sinh zip CHANGE liên tục dù không có thay đổi dữ liệu thật

**Triệu chứng:** sau khi deploy `MasterDataZipGeneratorWorker` (Model C), log ghi nhận worker sinh
lại file `.zip` CHANGE cho mọi POS terminal ở **mọi cycle** (mỗi `IntervalSeconds`), kể cả khi
không có thay đổi dữ liệu nào — không thấy exception, worker chạy ổn định, chỉ liên tục "phát
hiện thay đổi" sai.

**Nguyên nhân:** migration `docs/sql/SyncTableList_AddZipWatermark.sql` (manifest order 850,
`runOnce: true, phase: pre-deploy`) **chưa được chạy** trên CentralMD PROD trước khi deploy code
worker mới — đúng cảnh báo đã ghi sẵn ở `docs/ROLLOUT.md §O8` bước 4 nhưng bị bỏ sót lúc go-live.
Xác nhận bằng: `SELECT TOP 5 TableName, POSLastCounter, ZipWatermarkCounter FROM
dbo.SyncTableList;` báo lỗi "Invalid column name 'ZipWatermarkCounter'" (cột chưa tồn tại).

Vì cột chưa tồn tại, SP `[dbo].[SyncTable_Get]` bản đang chạy trên PROD (nhánh `@IsChange='C'`)
là bản CŨ hơn order 850, không SELECT cột này → Dapper để `SyncTableInfo.ZipWatermarkCounter`
mặc định `0` (không throw khi thiếu cột) → so sánh `POSLastCounter (luôn > 0) > ZipWatermarkCounter
(luôn = 0)` luôn đúng → worker coi MỌI bảng `IsOnlyChange=1` là "vừa đổi" ở MỌI cycle. **Không phải
bug logic C#** — logic so sánh counter trong `MasterDataZipGeneratorWorker.cs` đúng thiết kế.

**Fix:** chạy `docs/sql/SyncTableList_AddZipWatermark.sql` trên CentralMD PROD (theo đúng
`docs/ROLLOUT.md §O8` bước 4), verify 2 cột `POSLastCounter`/`ZipWatermarkCounter` bằng nhau sau
backfill, `redis-cli DEL MD:SyncTableList:C`. Không cần restart worker — cycle tiếp theo tự đọc
đúng cột mới.

**Bài học:** migration `runOnce: true, phase: pre-deploy` không được `POS.DbMigrator` tự áp dụng —
nếu bỏ sót, hậu quả có thể **âm thầm** (Dapper không throw khi thiếu cột, chỉ để property về giá
trị mặc định) thay vì báo lỗi rõ ràng ngay. Trước khi bật `WorkerRoles:EnableMasterDataZipGenerator`
ở môi trường mới, **luôn** chạy lại query verify ở `ROLLOUT.md §O8` bước 4 trước, không chỉ tin
rằng "deploy xong là xong".

## Thay đổi cấu hình khác (không phải fix lỗi — theo yêu cầu vận hành)

`src/POS.Worker/appsettings.ProductionHost.json` (nay đã gộp vào `appsettings.CronHost.json`, xem
cập nhật 2026-07-15 ở Vấn đề 6): `MasterDataZipGenerator.IntervalSeconds` 300 → 120 (poll
watermark mỗi 2 phút thay vì 5 phút).

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
□ Unit file: DOTNET_ENVIRONMENT=CronHost (KHÔNG phải Production) + WorkerRoles__* đúng vai trò
□ POS_SECRET_KEY trong Environment= khớp giá trị dùng chung 3 service khác
□ sudo systemctl daemon-reload && enable && start → status = active (running)
□ journalctl -u <service> -f không còn exception
□ redis-cli -n 0 GET Worker:Heartbeat:MasterDataZipGenerator vừa cập nhật theo IntervalSeconds mới
```

## Giám sát / đối soát worker có SINH file không (thêm 2026-07-11)

> Trước đây chỉ có `journalctl` (log text) + heartbeat Redis để biết worker "còn sống", **không**
> biết nó có thật sự tạo ra file `.zip` nào không. Nay mỗi lượt sinh file được ghi 1 dòng vào bảng
> `dbo.MasterDataGenerationLog` (RPOSMasterData) — tra cứu bằng SQL hoặc trang POS.Web
> `/ops/masterdata-generation-log`.

**BẮT BUỘC 1 lần**: chạy `docs/sql/MasterDataGenerationLog.sql` trên `RPOSMasterData` (idempotent,
fail-safe — chưa chạy thì worker vẫn sinh file bình thường nhưng KHÔNG có gì để tra cứu).

- **Worker có sinh file gì hôm nay không** (`TriggerSource='AutoChange'` = do worker tự động):
  ```sql
  SELECT TOP 50 GeneratedAt, StoreNo, PosNo, FileName, FileSizeBytes,
         TableCount, DurationMs, Status, InstanceId
  FROM dbo.MasterDataGenerationLog
  WHERE TriggerSource = 'AutoChange'
  ORDER BY GeneratedAt DESC;
  ```
  Có dòng `Status='Success'` = worker đã sinh file thật (kèm host `InstanceId`, dung lượng, thời
  điểm). `InstanceId` = tên máy Ubuntu host → xác nhận đúng tiến trình bare-metal đang chạy.
- **Đối soát "đã sinh ↔ POS đã tải"** (JOIN theo `FileName`):
  ```sql
  SELECT g.FileName, g.GeneratedAt, g.FileSizeBytes, d.DownloadedAt, d.Status AS DownloadStatus
  FROM dbo.MasterDataGenerationLog g
  LEFT JOIN dbo.MasterDataDownloadLog d ON d.FileName = g.FileName
  WHERE g.GeneratedAt >= CAST(GETDATE() AS date)
  ORDER BY g.GeneratedAt DESC;
  ```
  `DownloadedAt IS NULL` = file đã sinh nhưng POS chưa tải.
- **Lượt sinh lỗi**: `WHERE Status='Error'` — cột `Message` chứa lý do (file thiếu bảng… đã throw,
  không publish zip nào).

> Chi tiết cơ chế + vị trí file: `.claude/rules/masterdata-sync.md` mục "Generation logging".

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Runbook đầy đủ 3 model (A/B/C) | `docs/deploy/pos-worker-ubuntu-guide.md` |
| Chi tiết root cause + code liên quan | `docs/CHANGELOG.md` mục [2026-07-11] "Fix POS.Worker không kết nối được SQL Server..." |
| Cơ chế watermark/quarantine của `MasterDataZipGeneratorWorker` | `.claude/rules/masterdata-sync.md` |

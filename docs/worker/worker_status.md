# POS.Worker — Deployment Checklist & Handbook

> **Vai trò file này**: bảng tổng hợp điều hướng (handbook) cho toàn bộ `POS.Worker` — KHÔNG lặp
> lại nội dung runbook chi tiết đã có. Với mỗi phần, xem link "Chi tiết" để có lệnh đầy đủ.
>
> Nguồn sự thật đã tồn tại trước file này (không tạo trùng mục đích):
> - `.claude/skills/worker/SKILLS.md` — quy tắc code, kiến trúc, 3 khuôn mẫu worker
> - `docs/deploy/pos-worker-ubuntu-guide.md` — runbook Ubuntu đầy đủ (Model A cron + Model B Docker)
> - `deploy/windows/README.md` — runbook Windows Task Scheduler đầy đủ
> - `docs/ROLLOUT.md` §O2 — checklist go-live riêng cho `FileImport`
>
> File này bổ sung: **bảng tổng hợp Inventory** (chưa nơi nào có ở 1 chỗ), phần **log rotation**
> tường minh (chưa có ở runbook Ubuntu), làm rõ **quy tắc truyền tham số trên Windows** (3 câu hỏi
> cụ thể), và mục **đề xuất chuẩn hóa** cho các khoảng trống phát hiện được khi khảo sát.
>
> Cập nhật lần đầu: 2026-07-10. Xác thực bằng cách đọc source code + config thật trong repo —
> **CHƯA chạy worker thật để verify runtime** (sandbox không có SQL/Redis/RabbitMQ/Docker).

---

## 1. Worker Inventory

### 1.1 Danh sách HostedService / logic worker

| # | Tên (class) | File | Pattern | Interval | Mục tiêu | Đăng ký khi nào |
|---|---|---|---|---|---|---|
| 1 | `PosFileImportWorker` (wrapper vòng lặp) + `PosFileImportService` (logic 1 chu kỳ, dùng chung) | `src/POS.Infrastructure/Workers/PosFileImportWorker.cs`, `PosFileImportService.cs` | Timer polling **hoặc** one-shot (cùng 1 class logic, 2 cách gọi) | `FileImport:PollIntervalSeconds` (mặc định **30s**, đọc từ config) | Quét `.zip` trong `FileImport:InboxFolder` (chung với POS.Api `UploadFileSale`), giải nén `.txt`, insert qua `ICentralSaleRepository.InInsertToTableByJson`. Zip OK → xoá; có lỗi → move `ErrorFolder`. | Model B: `WorkerRoles:EnableFileProcessing=true` → `AddHostedService<PosFileImportWorker>()`. Model A: gọi thẳng `PosFileImportService.RunOnceAsync()` từ `Program.cs` khi có `--run-once`, **không qua `AddHostedService`**. |
| 2 | `PosSalesConsumerWorker` | `src/POS.Infrastructure/Workers/PosSalesConsumerWorker.cs` | Message consumer (RabbitMQ push, `AsyncEventingBasicConsumer`) | — (giữ connection; lỗi → retry sau 10s) | Consume queue **`pos_sales`**, deserialize `KafkaMessageDto`, insert qua `ICentralSaleRepository.InInsertToTableByJson` — đường nạp sale song song với `PosFileImportWorker`. `prefetchCount=1`, `autoAck=false`, nack không requeue khi lỗi (tránh poison loop). | `WorkerRoles:EnableRabbitMQConsumer=true` |
| 3 | `Rpt_ReportSaleDetail_Insert` | `src/POS.Infrastructure/Workers/Rpt_ReportSaleDetail_Insert.cs` | Timer polling | **Cứng 1 phút** (`TimeSpan.FromMinutes(1)`, **chưa đọc từ `IConfiguration`** — xem mục 5) | Gọi `IRptReportSaleDetailRepository.ExecuteInsertAsync(today, today)` — chạy SP `Rpt_ReportSaleDetail_Insert` mỗi phút với `@FromDate=@ToDate=hôm nay`. | `WorkerRoles:EnableSqlReportWorker=true` |
| 4 | `WorkerHeartbeatService` | `src/POS.Infrastructure/Workers/WorkerHeartbeatService.cs` | Timer polling | `WorkerHeartbeat:IntervalSeconds` (mặc định **15s**, từ 2026-07-11 đọc qua `IOptions<WorkerHeartbeatOptions>`, trước đó hardcode const) | Ghi `WorkerHeartbeat` (JSON) vào Redis key `Worker:Heartbeat:{WorkerHeartbeat:WorkerName}` (mặc định **`Worker:Heartbeat:PosSalesConsumer`**) — TTL `NormalTtlSeconds` (mặc định 60s) khi `Running`, `StoppedTtlSeconds` (mặc định 300s) khi `Stopped`. **Report tên cấu hình `WorkerHeartbeat:WorkerName`** (mặc định `"PosSalesConsumer"`, cùng nguồn config với `HealthCheck:WorkerName` phía đọc — đổi 1 nơi thì phải đổi cả 2 để khớp key Redis) dù đang đo `WorkerHealthState` dùng chung — xem giới hạn ở mục 5. | `WorkerRoles:EnableHeartbeat=true` |
| 5 | `MasterDataZipGeneratorWorker` | `src/POS.Worker/Workers/MasterDataZipGeneratorWorker.cs` | Timer polling (`PeriodicTimer`) + distributed lock (chỉ 1 instance chạy 1 lượt) | `MasterDataZipGenerator:IntervalSeconds` (mặc định **300s**) | Poll `SP [SyncTable_Get] 'C'`, so `POSLastCounter` với watermark cột DB `SyncTableList.ZipWatermarkCounter` (từ 2026-07-11 — trước đó là Redis Hash `Worker:Watermark:MasterDataZip`, đã retired vì có thể mất khi Redis restart/evict); bảng nào đổi → generate lại master data `.zip` cho mọi `POSTerminal.Status=1` (song song qua `Parallel.ForEachAsync`, `MaxParallelTerminals` mặc định 4) qua `ISyncDataPosService.PushMasterDataChangeAsync`. Terminal lỗi liên tiếp `QuarantineThreshold` lần (mặc định 3) → bị bỏ qua các lượt sau (Redis Hash `Worker:Quarantine:MasterDataZip`), tránh 1 terminal hỏng chặn watermark của cả fleet. Chi tiết đầy đủ: `.claude/rules/masterdata-sync.md` mục "Worker sinh zip theo watermark + quarantine". | `WorkerRoles:EnableMasterDataZipGenerator=true` (mặc định **false** — opt-in, cần cấu hình `AppSettings:FtpRootPath` + DBA đánh dấu `SyncTableList.IsOnlyChange=1` + chạy `docs/sql/SyncTableList_AddZipWatermark.sql` trước, xem `docs/ROLLOUT.md`) |

> `PosFileImportWorker` (Model B) cũng tự ghi heartbeat riêng key `Worker:Heartbeat:PosFileImport`
> (trong chính `PosFileImportWorker.ExecuteAsync`, không qua `WorkerHeartbeatService`) — TTL =
> `max(60, PollIntervalSeconds × 3)`. `MasterDataZipGeneratorWorker` cũng tự ghi heartbeat riêng key
> `Worker:Heartbeat:MasterDataZipGenerator` (`Status`="Running"/"Degraded"/"Stopped"), cùng pattern.

### 1.2 State & health phụ trợ

| Thành phần | File | Vai trò |
|---|---|---|
| `WorkerHealthState` | `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton `Status` (`Running`/`Degraded`/`Stopped`) + `ProcessedCount` (Interlocked) — writer: các worker; reader: `WorkerHeartbeatService`. |
| `WorkerRolesOptions` | `src/POS.Infrastructure/Workers/WorkerRolesOptions.cs` | Feature toggle 5 cờ `bool`, bind section `"WorkerRoles"`. Chỉ áp dụng cho **Model B** (long-running); Model A (`--run-once`) bỏ qua hoàn toàn các cờ này. |
| `PosFileImportService.RunOnceAsync` | `src/POS.Infrastructure/Workers/PosFileImportService.cs` | Logic "1 chu kỳ quét" dùng chung cho cả Model A và Model B — tách khỏi `BackgroundService` để tái dùng. |

### 1.3 Bảng tham số bắt buộc (CLI / Environment Variable)

| Tham số | Giá trị | Ý nghĩa | Bằng chứng |
|---|---|---|---|
| `args: --run-once` | flag, không có giá trị | Bật **Model A** — chạy `PosFileImportService.RunOnceAsync()` 1 lần rồi thoát (`return 0` OK / `return 1` lỗi), **không đăng ký `IHostedService` nào** | `Program.cs:19,22,33-48` |
| Env `WORKER_RUN_ONCE=true` | `"true"` (case-insensitive) | Tương đương `--run-once`, dùng khi không truyền được args (vd script cron gọi qua biến môi trường) | `Program.cs:20` |
| Env `DOTNET_ENVIRONMENT` | `Development` \| `Production` \| `UAT` \| `CronHost` | Chọn file `appsettings.{Env}.json` overlay lên `appsettings.json` (Generic Host chuẩn) | 4 file appsettings trong `src/POS.Worker/` |
| Env `WorkerRoles__EnableFileProcessing` / `EnableRabbitMQConsumer` / `EnableSqlReportWorker` / `EnableHeartbeat` / `EnableMasterDataZipGenerator` | `"true"`/`"false"` | Override section `WorkerRoles` qua biến môi trường (double-underscore = section nesting của `IConfiguration`) — **chỉ có tác dụng ở Model B**. `EnableMasterDataZipGenerator` mặc định `false` trong `appsettings.json`, chưa thêm vào `docker-compose.yml:94-97` (opt-in thủ công khi rollout, xem `docs/ROLLOUT.md`) | `docker-compose.yml:94-97`, `WorkerRolesOptions.cs` |

> **Không có** flag CLI generic kiểu `--job=<tên>` để chọn chạy đúng 1 job cụ thể theo tên — xem
> mục 5 "Đề xuất chuẩn hóa".

---

## 2. Deployment Checklist — Ubuntu (Production)

> Chi tiết đầy đủ (lệnh, giải thích từng bước): **`docs/deploy/pos-worker-ubuntu-guide.md`**.
> Mục này chỉ tóm tắt checklist thao tác nhanh + bổ sung phần **log rotation** còn thiếu.

### 2.1 Model B — Dockerized (`PosSalesConsumerWorker` + `Rpt_ReportSaleDetail_Insert` + heartbeat)

```
□ cd /đường-dẫn-repo && docker build -t pos-worker:prod -f Dockerfile.worker .
□ sudo mkdir -p /srv/pos/logs/worker && sudo chown 1654:1654 /srv/pos/logs/worker
□ docker run -d --name pos-worker-prod \
    -e DOTNET_ENVIRONMENT=Production -e TZ=Asia/Ho_Chi_Minh \
    -e POS_SECRET_KEY="<cùng giá trị dùng cho pos-api-prod/pos-web-prod>" \
    -e WorkerRoles__EnableFileProcessing=false \
    -e WorkerRoles__EnableRabbitMQConsumer=true \
    -e WorkerRoles__EnableSqlReportWorker=true \
    -e WorkerRoles__EnableHeartbeat=true \
    --add-host host.docker.internal:host-gateway \
    -v /srv/pos/logs/worker:/srv/pos/logs/worker --restart unless-stopped pos-worker:prod
□ docker ps --filter name=pos-worker  → STATUS "Up" liên tục (không restart loop)
□ docker logs --tail 50 pos-worker-prod → thấy PosSalesConsumer + Rpt_ReportSaleDetail_Insert khởi
  động, KHÔNG có "[PosFileImport] Started", không exception (đặc biệt không có
  "InvalidOperationException... POS_SECRET_KEY" — appsettings.Production.json đã mã hóa enc:...)
□ redis-cli -n 0 GET Worker:Heartbeat:PosSalesConsumer → giá trị mới cập nhật (≤60s) — PROD dùng
  DB 0 (khớp Api/Web), KHÔNG phải DB 2 như trước 2026-07-11 (xem gotcha ở mục 4.2)
```

Hoặc dùng sẵn `docker compose up -d --build` (service `pos-worker` trong `docker-compose.yml`,
env `WorkerRoles__*` đã cấu hình sẵn giống lệnh trên).

> **Chạy song song bare-metal cùng roles (Model B'), khi SQL Server cũng chạy Docker trên cùng
> host**: dùng `appsettings.CronHost.json` (`DOTNET_ENVIRONMENT=CronHost` — dùng chung file với
> Model A/Model C, xem cập nhật 2026-07-15; địa chỉ `127.0.0.1` thay vì `host.docker.internal`,
> `EnableHeartbeat=false` mặc định) + `WorkerRoles__*` override qua `Environment=` thay vì lặp lại
> `Production.json` — xem `docs/deploy/pos-worker-ubuntu-guide.md` mục 3.5 và
> `docs/ROLLOUT.md` §O11.

> Container **không** mở cổng, **không** có `HEALTHCHECK` (`Dockerfile.worker` không `EXPOSE`) —
> giám sát bằng `docker logs` + Redis heartbeat, không phải HTTP health endpoint.

### 2.2 Model A — Cron-based (`PosFileImportService`, chạy native trên host, KHÔNG dùng Docker)

```
□ sudo apt-get install -y aspnetcore-runtime-10.0                     (cài 1 lần/host)
□ sudo ./deploy/linux/setup-pos-dirs.sh                                (tạo group posops gid 1654,
                                                                          chmod 2770 ftpbluepos)
□ sudo usermod -aG posops <user-chạy-cron>                            (rồi newgrp posops / re-login)
□ dotnet publish src/POS.Worker/POS.Worker.csproj -c Release -o /srv/pos/app/worker
□ cp deploy/linux/run-worker-file-import-once.sh /srv/pos/app/worker/ && chmod +x ...
□ mkdir -p /srv/pos/logs/worker-cron && chown <user>:posops /srv/pos/logs/worker-cron
□ Chạy tay 1 lần trước khi tin crontab:
    DOTNET_ENVIRONMENT=CronHost dotnet /srv/pos/app/worker/POS.Worker.dll --run-once
    echo $?   # phải = 0
□ crontab -e  (dưới user thuộc group posops):
    * * * * * /srv/pos/app/worker/run-worker-file-import-once.sh >> /srv/pos/logs/worker-cron/cron.log 2>&1
□ Thả 1 file .zip hợp lệ vào InboxFolder → biến mất (OK) hoặc vào error/ (lỗi) trong ≤1 phút
□ tail -f /srv/pos/logs/worker-cron/cron.log → có dòng mới đều đặn mỗi phút, không có khoảng trống
```

> Script wrapper dùng `flock -n` — nếu 1 chu kỳ trước chưa xong khi cron kích hoạt lượt mới, lượt
> mới **bị bỏ qua** (không xếp hàng, không chạy song song 2 tiến trình quét cùng thư mục).

### 2.3 Quyền user (`chmod`/`chown`) & restart

| Thành phần | User/quyền |
|---|---|
| Docker container `pos-worker` | Chạy dưới `USER $APP_UID` (UID:GID `1654:1654`, non-root, cố định trong image `aspnet:10.0`) |
| Thư mục `ftpbluepos` (chỉ Model B nếu bật `EnableFileProcessing`, và Model A) | `chmod 2770` (setgid) + `chown 1654:1654`, group `posops` gid 1654 — user vận hành cần `usermod -aG posops` |
| Cron job (Model A) | Chạy dưới user thường (không phải root) thuộc group `posops` — **không cần sudo** để đọc/ghi `ftpbluepos` nhờ setgid |
| Restart Model B | `docker restart pos-worker-prod` hoặc `docker stop && docker rm && docker run ...` (re-deploy, xem mục 6 runbook Ubuntu) |
| Restart/dừng Model A | Không có "process" để restart — mỗi lượt cron tự chạy rồi thoát. "Dừng" = xoá dòng trong `crontab -e` |

### 2.4 Log rotation — tránh đầy ổ cứng (bổ sung, chưa có ở runbook gốc)

**Model B (Docker)** — mặc định driver `json-file` của Docker **không tự giới hạn dung lượng**,
log tích lũy vô hạn trong `/var/lib/docker/containers/.../*.json`. Thêm giới hạn khi `docker run`
hoặc trong `docker-compose.yml`:

```bash
docker run -d --name pos-worker-prod \
  --log-opt max-size=10m --log-opt max-file=5 \
  ... pos-worker:prod
```
```yaml
# docker-compose.yml, service pos-worker
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "5"
```

Riêng thư mục bind-mount `./logs:/app/logs` (Serilog file sink) đã tự xoay vòng qua
`LogRetention:SerilogRetainedFileCountLimit` (config `appsettings.Production.json`, mặc định
**10** file) — không cần `logrotate` thêm cho phần này, Serilog tự xoá file cũ.

**Model A (cron, file `cron.log`)** — script wrapper chỉ `>>` append, **không có cơ chế xoay vòng
nào** cho `/srv/pos/logs/worker-cron/cron.log`. Thêm `logrotate` (1 lần/host):

```bash
sudo tee /etc/logrotate.d/pos-worker-cron > /dev/null <<'EOF'
/srv/pos/logs/worker-cron/cron.log {
    daily
    rotate 14
    compress
    delaycompress
    missingok
    notifempty
    su <user> posops
}
EOF
sudo logrotate -d /etc/logrotate.d/pos-worker-cron   # dry-run kiểm tra cú pháp
```

Serilog file log của Model A (`Logging:FileLogDirectory=/srv/pos/logs/worker-cron`,
`appsettings.CronHost.json`) tự xoay theo `LogRetention:RawLogRetentionDays` — nhưng **`cron.log`
(stdout/stderr redirect thủ công trong crontab) không đi qua Serilog**, nên bắt buộc `logrotate`
riêng như trên.

---

## 3. Deployment Checklist — Windows (Local/Legacy)

> Chi tiết đầy đủ: **`deploy/windows/README.md`**. Mục này làm rõ 3 câu hỏi truyền tham số.

### 3.1 Publish & chuẩn bị

```
□ cd deploy\windows && .\publish-worker.ps1 -Output "D:\POS\Worker"
□ dotnet --list-runtimes  → phải có Microsoft.NETCore.App 10.x (framework-dependent, không self-contained)
□ Copy POS.Worker.Task.xml vào D:\POS\Worker (script publish chưa tự copy file này)
□ Tạo sẵn D:\ROOT\Logs + D:\ROOT\FILEIMPORT\{inbox,error,_work} và cấp quyền ghi cho tài khoản chạy
  (worker tự tạo folder FileImport nếu thiếu, nhưng ổ D:\ và quyền ghi phải có sẵn)
```

### 3.2 Execution Command — quy tắc truyền tham số

**Câu hỏi 1 — Chạy 1 task cụ thể (cần flag nào)?**
Trên Windows, launcher `run-worker.bat` **không forward bất kỳ args nào** cho
`POS.Worker.exe`/`dotnet POS.Worker.dll` (`run-worker.bat:28-31` chỉ gọi thẳng, không có `%*`).
→ **Không dùng Windows để chạy `--run-once` (Model A)** — cơ chế đó chỉ dùng cho Ubuntu cron
(xem mục 2.2). Trên Windows, "chọn task cụ thể" = sửa `WorkerRoles:Enable*` trong
`appsettings.json` (đặt các cờ không cần thành `false`) rồi `schtasks /End` + `/Run` lại — **không
có cách truyền qua tham số dòng lệnh** trên Windows launcher hiện tại.

**Câu hỏi 2 — Chạy nhiều task trong cùng 1 instance (pattern hỗ trợ)?**
**Có** — đây là hành vi mặc định của Model B. `appsettings.json` mặc định cả 4 cờ `WorkerRoles.Enable*
= true` → `Program.cs` đăng ký cả 4 `AddHostedService<>()` trong cùng 1 Generic Host process. Không
cần pattern đặc biệt gì thêm — chỉ cần không tắt cờ nào.

**Câu hỏi 3 — User account & path:**
- Tài khoản: mặc định **SYSTEM** (`POS.Worker.Task.xml` → `<UserId>S-1-5-18</UserId>`,
  `RunLevel HighestAvailable`) — đủ quyền vì DB/Redis/RabbitMQ dùng auth riêng (không cần domain
  account), chỉ cần SYSTEM ghi được `D:\ROOT\...` và thư mục publish.
- **"Run whether user is logged on or not"**: đã set sẵn qua `RunLevel HighestAvailable` +
  `BootTrigger` (Cách A import XML) hoặc `/RU SYSTEM` (Cách B CLI) — chạy được cả khi không ai
  đăng nhập, vì SYSTEM không cần phiên đăng nhập tương tác.
- **Path thực thi**: `run-worker.bat` **BẮT BUỘC** `cd /d "%~dp0"` trước khi chạy (`run-worker.bat:12`)
  vì Generic Host đọc `appsettings.json` theo Current Working Directory, mà Task Scheduler mặc
  định CWD = `C:\Windows\System32`. `<WorkingDirectory>` trong `POS.Worker.Task.xml` **phải khớp**
  thư mục publish thật (`D:\POS\Worker` mặc định) — sai chỗ này → worker không đọc được config,
  lỗi khó chẩn đoán vì không throw rõ ràng.

### 3.3 Đăng ký Task Scheduler

```
□ Cách A (đầy đủ restart-on-failure + IgnoreNew — khuyến nghị):
    schtasks /Create /TN "POS.Worker" /XML "D:\POS\Worker\POS.Worker.Task.xml" /F
□ Cách B (nhanh, không có restart-on-failure):
    schtasks /Create /TN "POS.Worker" /TR "D:\POS\Worker\run-worker.bat" /SC ONSTART /RU SYSTEM /RL HIGHEST /F
□ schtasks /Run /TN "POS.Worker"
□ schtasks /Query /TN "POS.Worker" /V /FO LIST | findstr /I "Status Last"
```

`POS.Worker.Task.xml` đã cấu hình sẵn: `BootTrigger` (chạy lúc khởi động máy),
`MultipleInstancesPolicy=IgnoreNew` (không chạy chồng), `RestartOnFailure` (Interval 1 phút, tối
đa 999 lần) — tương đương `docker run --restart unless-stopped` bên Ubuntu.

### 3.4 Kiểm chứng

```
□ Xem D:\POS\Worker\logs\worker-console.log → 3 dòng khởi động: PosSalesConsumer,
  Rpt_ReportSaleDetail_Insert, "[PosFileImport] Started ..."
□ Thả 1 zip hợp lệ vào D:\ROOT\FILEIMPORT\inbox → biến mất (OK, ≤30s) hoặc vào error\ (lỗi)
□ redis-cli GET Worker:Heartbeat:PosFileImport (DB 2) → cập nhật mỗi cycle
```

> Ghi chú từ runbook gốc: Task Scheduler + `.bat` chỉ phù hợp chạy thử/dev. Production dài hạn nên
> cân nhắc Windows Service (`Microsoft.Extensions.Hosting.WindowsServices` + `AddWindowsService()`)
> hoặc NSSM để có recovery/logging chuẩn SCM — **hiện tại repo chưa làm việc này** (ghi nhận, không
> phải việc của tài liệu này).

---

## 4. Troubleshooting & Health Check

### 4.1 Xem log

| Môi trường | Lệnh |
|---|---|
| Docker (Model B) | `docker logs --tail 100 pos-worker-prod` (hoặc `-uat`), `docker logs -f ...` để tail realtime |
| Ubuntu cron (Model A) | `tail -f /srv/pos/logs/worker-cron/cron.log` — log của **wrapper script**, không phải journalctl |
| `journalctl` | **Chỉ áp dụng nếu Docker chạy qua systemd** (`systemctl status docker`) — thấy log daemon Docker, **KHÔNG thấy log ứng dụng bên trong container** (container không tự đăng ký unit systemd riêng trong setup hiện tại). Dùng `docker logs`, không phải `journalctl -u pos-worker`. |
| Windows | `D:\POS\Worker\logs\worker-console.log` (stdout/stderr redirect thủ công trong `run-worker.bat`) |
| Windows Event Viewer | `Event Viewer → Applications and Services Logs → Microsoft → Windows → TaskScheduler → Operational` — xem lịch sử Task chạy/lỗi/exit code. Task Scheduler **không** tự ghi log ứng dụng vào Event Viewer — chỉ ghi sự kiện của chính Task (start/stop/restart), log ứng dụng vẫn ở `worker-console.log` |
| Tập trung (mọi môi trường) | Serilog → Elasticsearch, index `pos-worker-logs-{yyyy.MM.dd}` (Model B/Windows) hoặc `pos-worker-cron-logs-{yyyy.MM.dd}` (Model A) — filter theo prefix `[WorkerName]` trong message (quy ước bắt buộc, xem `.claude/skills/worker/SKILLS.md` mục 8) |

### 4.2 Biết worker đã chạy đúng lịch hay chưa

| Cơ chế | Cách kiểm tra | Áp dụng cho |
|---|---|---|
| Redis heartbeat | `redis-cli -n <DB> GET Worker:Heartbeat:PosSalesConsumer` / `...:PosFileImport` — **`<DB>` PHẢI khớp `Redis:DefaultDatabase` của POS.Worker VÀ của POS.Web** (PROD hiện = `0`, UAT = `2`; lệch giữa 2 service → `/ops/health` báo sai "offline" dù worker sống, xem `docs/ROLLOUT.md` §O10) | Model B (mọi worker có `EnableHeartbeat=true`) + `PosFileImportWorker` khi chạy long-running |
| Trang Ops `/ops/health` | `HealthCheckService.CheckWorkerHeartbeatAsync` — đọc key `Worker:Heartbeat:{HealthCheck:WorkerName}` (mặc định **chỉ `"PosSalesConsumer"`**, config `src/POS.Web/appsettings.json` section `HealthCheck`), báo "Đang chạy" nếu nhịp cuối ≤ `HealthCheck:StaleAfterSeconds` (mặc định 45s, từ 2026-07-11 đọc từ config thay vì hardcode), "Suy giảm" nếu `Status=Degraded`, "Mất tín hiệu" nếu quá ngưỡng đó không có nhịp | Chỉ 1 worker/lần theo config — xem giới hạn ở mục 5 |
| Exit code + `cron.log` | `echo $?` sau khi chạy tay `--run-once`; hoặc quan sát `cron.log` có dòng mới đều mỗi phút, không có khoảng trống bất thường | **Model A — không có heartbeat Redis** (`EnableHeartbeat=false` cứng trong `appsettings.CronHost.json`), đây là cơ chế giám sát DUY NHẤT |
| `ProcessedCount` trong log | Mỗi worker log `"... OK — txId: ..."` hoặc `"Exec success for ..."` sau mỗi item xử lý thành công | Tất cả |

### 4.3 Chẩn đoán nhanh khi nghi ngờ worker "chết"

```
1. Model B: docker ps --filter name=pos-worker → có "Up" không? (container tự thoát = crash lúc
   khởi động, thường do sai connection string/Redis/RabbitMQ không reachable)
2. docker logs --tail 200 <container> → tìm exception đầu tiên sau dòng "Started". Thấy
   "InvalidOperationException... thiếu biến môi trường POS_SECRET_KEY" → thiếu `-e POS_SECRET_KEY`
   khi appsettings đã mã hóa enc:... (xem docs/ROLLOUT.md §C4), KHÔNG phải lỗi hạ tầng.
3. `/ops/health` báo "Worker offline" nhưng docker logs KHÔNG có exception nào → khả năng cao là
   lệch `Redis:DefaultDatabase` giữa POS.Worker và POS.Web (xem §O10, mục 4.2) chứ không phải
   worker chết — đối chiếu số DB trước khi kết luận.
4. Model A: kiểm tra crontab còn dòng (crontab -l), cron.log có dòng mới trong ≤2 phút gần nhất
5. Redis: TTL hết hạn (60s không có nhịp mới) = worker treo hoặc bị kill mà không kịp ghi "Stopped"
6. Windows: schtasks /Query /TN "POS.Worker" /V /FO LIST | findstr /I "Status Last" → "Running"?
   Event Viewer TaskScheduler Operational → tìm sự kiện restart gần nhất (nếu có → đang crash loop)
```

---

## 5. Đề xuất chuẩn hóa (ghi nhận — chưa implement)

Phát hiện khi khảo sát code, không thuộc phạm vi sửa của tài liệu này — ghi lại để cân nhắc sau:

1. **Không có flag CLI generic `--job=<tên>`** để chọn chạy đúng 1 job cụ thể — hiện tại `Program.cs`
   chỉ có nhánh cứng `--run-once` (luôn là `PosFileImportService`). Nếu tương lai cần thêm job
   one-shot khác (vd dọn file cũ, đối soát định kỳ), nên đổi sang dictionary
   `Dictionary<string, Func<IServiceProvider, CancellationToken, Task>>` map theo `--job=`, tránh
   phải sửa `Program.cs` mỗi lần thêm job mới.
2. **`Rpt_ReportSaleDetail_Insert` interval hardcode** `TimeSpan.FromMinutes(1)` — khác các worker
   khác (`PosFileImportWorker` đọc `FileImport:PollIntervalSeconds` từ config). Nên chuyển sang
   đọc từ `IConfiguration` (vd section `"RptReportSaleDetail:IntervalMinutes"`) để chỉnh không cần
   rebuild.
3. **`/ops/health` chỉ giám sát 1 `HealthCheck:WorkerName`** (mặc định `"PosSalesConsumer"`) — không
   thấy heartbeat `PosFileImport` cùng lúc trên UI dù cả 2 đều ghi Redis. Nên đổi
   `HealthCheckService.CheckAllAsync` sang duyệt **mảng** tên worker thay vì 1 giá trị đơn (đã có
   tiền lệ ghi nhận nhu cầu tương tự ở `.claude/rules/masterdata-sync.md` cho
   `SyncTableCounterFlushWorker`).
4. **Windows chưa có kịch bản tương đương Model A (cron)** — `run-worker.bat` không forward args
   nên không thể chạy `--run-once` qua Task Scheduler dạng "chạy 1 lần rồi thoát theo lịch". Nếu
   cần mô hình tương tự trên Windows (vd Task Scheduler trigger mỗi phút thay vì BootTrigger
   long-running), cần thêm script `.bat` riêng forward `--run-once` + set `MultipleInstancesPolicy`
   phù hợp (khác `IgnoreNew` hiện tại vốn thiết kế cho long-running).

---

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Quy tắc code worker (3 khuôn mẫu, checklist tạo mới, WorkerHealthState/Heartbeat) | `.claude/skills/worker/SKILLS.md` |
| Runbook Ubuntu đầy đủ (Model A + Model B, build/run/rollback) | `docs/deploy/pos-worker-ubuntu-guide.md` |
| Runbook Windows đầy đủ (Task Scheduler, publish, .bat) | `deploy/windows/README.md` |
| Go-live checklist `FileImport` (thư mục, quyền, path) | `docs/ROLLOUT.md` §O2 |
| Quy trình deploy tổng quát (Api/Web/Worker, nginx, `POS_SECRET_KEY`) | `docs/guide-deploy.md` |
| Thư mục dùng chung `ftpbluepos` (POS.Api ↔ POS.Worker) | `docs/deploy/ubuntu-guide.md` |

# Deploy POS.Worker trên Windows (Task Scheduler + .bat)

Chạy thử `POS.Worker` (consumer RabbitMQ + `PosFileImportWorker` quét file `.zip`) trên máy Windows,
launcher là `run-worker.bat` do **Windows Task Scheduler** khởi động.

- **Runtime:** framework-dependent — máy đích **đã cài .NET 10 Runtime**.
- **Cấu hình:** `DOTNET_ENVIRONMENT=Development` → chỉ `appsettings.json` (base, host `10.235.x` thật)
  áp dụng. **KHÔNG** dùng `Production` (host docker `mssql_2019`/`host.docker.internal` — không reachable).
- **Mô hình chạy:** long-running — Task Scheduler bật 1 lần lúc boot, giữ sống, restart-on-failure.

> ⚠️ Worker **tự loop mãi** bên trong (`PosSalesConsumerWorker` giữ kết nối, `PosFileImportWorker`
> quét mỗi `PollIntervalSeconds`). KHÔNG cấu hình Task Scheduler chạy lặp mỗi N phút — sẽ đẻ nhiều
> tiến trình. Dùng trigger **At startup** + **IgnoreNew** như file XML kèm theo.

---

## 1. Publish

Trên máy có .NET 10 SDK (hoặc chính máy đích):

```powershell
cd deploy\windows
.\publish-worker.ps1 -Output "D:\POS\Worker"
```

Script sẽ `dotnet publish -c Release` và copy `run-worker.bat` vào `D:\POS\Worker`.
Thư mục publish gồm: `POS.Worker.dll`, `appsettings.json`, `appsettings.Production.json`,
`appsettings.UAT.json`, `run-worker.bat`.

Kiểm tra runtime có sẵn:
```powershell
dotnet --list-runtimes   # cần Microsoft.NETCore.App 10.x
```

Copy thêm file `POS.Worker.Task.xml` vào `D:\POS\Worker` (để lệnh đăng ký bên dưới trỏ đúng).

## 2. Chuẩn bị thư mục & quyền

- Thư mục `FileImport` (theo `appsettings.json`): `D:\ROOT\FILEIMPORT\{inbox,error,_work}` —
  **worker tự tạo** khi chạy, chỉ cần **ổ D: tồn tại + tài khoản chạy có quyền ghi**.
- Thư mục log file: `D:\ROOT\Logs` (theo `Logging:FileLogDirectory`) — nên tạo sẵn + cấp quyền ghi.
- Tài khoản chạy (mặc định **SYSTEM**) phải:
  - Ghi được `D:\ROOT\...` và `D:\POS\Worker\logs`.
  - Kết nối được SQL `10.235.55.122\DRW`, Redis `10.235.52.189:6379`, RabbitMQ `10.235.52.189:5672`
    (DB/Redis/Rabbit dùng auth riêng, không cần Windows domain account).

## 3. Chạy tay để kiểm tra trước (khuyến nghị)

```powershell
cd D:\POS\Worker
.\run-worker.bat
```
Quan sát `D:\POS\Worker\logs\worker-console.log`. Kỳ vọng thấy log khởi động 3 worker:
`PosSalesConsumer`, `Rpt_ReportSaleDetail_Insert`, `[PosFileImport] Started ...`.
`Ctrl+C` để dừng.

## 4. Đăng ký Task Scheduler

**Cách A — import XML (đầy đủ restart-on-failure + IgnoreNew):**
```powershell
schtasks /Create /TN "POS.Worker" /XML "D:\POS\Worker\POS.Worker.Task.xml" /F
```
> Nếu publish vào thư mục khác `D:\POS\Worker`: sửa `<Command>` và `<WorkingDirectory>` trong XML trước.

**Cách B — nhanh gọn (CLI, chạy lúc boot dưới SYSTEM):**
```powershell
schtasks /Create /TN "POS.Worker" /TR "D:\POS\Worker\run-worker.bat" /SC ONSTART /RU SYSTEM /RL HIGHEST /F
```
> Cách B không set được "restart-on-failure" / "không chạy chồng" — dùng Cách A nếu cần các mục đó.

**Cách C — GUI (taskschd.msc):** Create Task → *General*: Run whether user is logged on or not +
Run with highest privileges → *Triggers*: At startup → *Actions*: Start a program =
`D:\POS\Worker\run-worker.bat`, **Start in** = `D:\POS\Worker` → *Settings*: "If the task is already
running, do not start a new instance" + "If the task fails, restart every 1 min" + bỏ chọn
"Stop the task if it runs longer than...".

## 5. Khởi động & xác minh

```powershell
schtasks /Run /TN "POS.Worker"
schtasks /Query /TN "POS.Worker" /V /FO LIST | findstr /I "Status Last"
```

Test luồng file import:
1. Tạo `x.txt` chứa 1 JSON `KafkaMessageDto` hợp lệ → nén thành `x.zip` → thả vào `D:\ROOT\FILEIMPORT\inbox`.
2. Chờ ≤ `PollIntervalSeconds` (30s) → log `[PosFileImport] Inserted OK ...`, zip **biến mất** (thành công) hoặc
   nằm trong `D:\ROOT\FILEIMPORT\error` (thất bại).
3. Heartbeat Redis: `GET Worker:Heartbeat:PosFileImport` (DB 2) phải cập nhật mỗi cycle.

## 6. Vận hành

```powershell
schtasks /End    /TN "POS.Worker"    # dừng
schtasks /Run    /TN "POS.Worker"    # chạy lại
schtasks /Delete /TN "POS.Worker" /F # gỡ
```

- Log chi tiết: Serilog → Elasticsearch (`pos-worker-logs-*`) + file log tại `D:\ROOT\Logs`.
- Log console (startup/crash): `D:\POS\Worker\logs\worker-console.log`.
- Đổi cấu hình `FileImport` (folder, interval, `Enabled`): sửa `D:\POS\Worker\appsettings.json` rồi
  `schtasks /End` + `/Run` lại.

> **Ghi chú:** Task Scheduler + .bat đủ cho chạy thử. Với Production dài hạn nên cân nhắc chạy như
> **Windows Service** (thêm gói `Microsoft.Extensions.Hosting.WindowsServices` + `builder.Services.AddWindowsService()`,
> hoặc bọc bằng NSSM) để có recovery/logging chuẩn của Service Control Manager.

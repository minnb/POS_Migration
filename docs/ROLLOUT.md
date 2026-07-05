# Rollout — Cấu hình bảo mật Production (POS.Web)

> Tài liệu **trung tâm** cho mọi cấu hình cần thao tác khi go-live Production.
> Mỗi khi thêm cấu hình bảo mật/hạ tầng mới cần người vận hành thực hiện, **cập nhật file này**.

## Checklist rollout (tổng quan)

| # | Hạng mục | Việc cần làm khi go-live | Mức | Mục chi tiết |
|---|---|---|---|---|
| C4 | Mã hóa credentials | Tạo khóa → `.env` → mã hóa → thay `enc:...` trong Production.json (POS.Api **+** POS.Web) | CRITICAL | [§C4](#c4--mã-hóa-credentials-trong-appsettings) |
| C1 | HTTPS + Cookie.Secure | Có TLS → đặt `Security:RequireHttps=true` | CRITICAL (khi ra internet) | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| H2 | AllowedHosts | Đặt domain dashboard thật thay cho `"*"` | HIGH | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| H1 | SQL Console | Cân nhắc `Security:EnableSqlConsole=false` | HIGH | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| O1 | Master data sync (POS.Api) | Đảm bảo `FtpRootPath` ghi được + tinh chỉnh `MasterDataSync` | MEDIUM | [§O1](#o1--sinh-file-master-data-zip-cho-pos-posapi) |
| O2 | File import worker (POS.Worker) | Tạo 3 thư mục inbox/error/_work + cấp quyền ghi + điền path `FileImport` | MEDIUM | [§O2](#o2--worker-nạp-sale-từ-file-zip-posworker) |
| O3 | Nút SyncData trên POS.Web (`/catalog/pos-setup`) | Đặt `FtpRootPath` của **POS.Web** = ĐÚNG thư mục vật lý POS.Api phục vụ (chung share/volume) | MEDIUM | [§O3](#o3--nút-syncdata-đẩy-dữ-liệu-đầu-ngày-posweb) |
| O4 | Log request/response toàn cục (POS.Api) | Mặc định TẮT (`RequestLogging:Enabled=false`) — chỉ bật khi cần debug 1 đợt cụ thể; `PersistToFile=true` mặc định vì chưa cài Elasticsearch | LOW (opt-in khi cần) | [§O4](#o4--log-requestresponse-toàn-cục-posapi) |
| D1 | SP Cài đặt CTKM (11.1) | Chạy 2 script SQL tạo SP trên CentralMD | REQUIRED (cho `/promotion/setup`) | [§D1](#d1--stored-procedures-cài-đặt-ctkm-111) |
| D2 | SP Special Combo (11.2) | Chạy 3 script SQL tạo SP trên CentralMD | REQUIRED (cho `/promotion/special-combo`) | [§D2](#d2--stored-procedures-special-combo-112) |
| D3 | SP Setup Coupon (8.1/8.2) | Chạy 4 script SQL tạo SP + TVP trên CentralMD (gồm `CpnVchBOMHeader_GetList.sql` cho master list) | REQUIRED (cho `/promotion/coupons`) | [§D3](#d3--stored-procedures-setup-coupon-8182) |
| D4 | SP Voucher (8.3) + reuse (8.4) | Chạy 3 script SQL tạo SP + TVP trên CentralMD; 8.4 tái dùng SP CentralSales | REQUIRED (cho `/promotion/vouchers`) | [§D4](#d4--stored-procedures-voucher-8384) |
| D5 | SP Setup Giá (9.3) | Chạy script SQL tạo TVP + SP lưu bảng giá trên CentralMD | REQUIRED (cho `/catalog/price-setup`) | [§D5](#d5--stored-procedures-setup-giá-93) |
| D6 | Gộp SAP Voucher vào CpnVchBOMCodeIssue | Chạy 5 script SQL (extend schema, 2 SP mới + TVP, migrate data, rename legacy) trên CentralMD, đúng thứ tự, có cửa sổ bảo trì. **+D6.1**: chạy thêm 4 script vá đồng bộ dữ liệu Coupon↔SAP Voucher (ItemNo hardening, SetupCoupon_Save/Voucher_Read/Voucher_Save bản mới) | CRITICAL (cho `api/sap/*`, 5.000 POS) | [§D6](#d6--gộp-sap-internal-voucher-vào-cpnvchbomcodeissue) |

---

## C4 — Mã hóa credentials trong appsettings

> Mã hóa password DB/RabbitMQ trong `appsettings.Production.json` bằng AES-256-GCM.
> Cơ chế đã build sẵn (`SecretProtector` + hook giải mã trong `Program.cs` + trang `/admin/encrypt-secret`
> của POS.Web) — **áp dụng cho CẢ POS.Api và POS.Web**, dùng chung 1 khóa `POS_SECRET_KEY`.
> Việc rollout do **người vận hành** thực hiện vì cần khóa bí mật — Claude không giữ khóa, không thay password thật.

---

## Nguyên tắc an toàn (đọc trước)

- **Mã hóa CẢ HAI file `appsettings.Production.json`** — `src/POS.Api/appsettings.Production.json` VÀ
  `src/POS.Web/appsettings.Production.json` (2 project dùng chung nhiều credential giống nhau: SQL `sa`,
  EInvoice, RabbitMQ). **KHÔNG** mã hóa `appsettings.json` (base) của bất kỳ project nào.
  Hook giải mã chạy ở **mọi môi trường**: nếu base có `enc:` mà máy Dev không set khóa → app **fail-fast, không khởi động**.
  Để base plaintext thì Dev chạy bình thường, không cần khóa.
- **Khóa KHÔNG bao giờ vào git.** Khóa nằm ở `.env` (đã `.gitignore`) hoặc env của host.
  Ciphertext `enc:...` nằm trong `appsettings.Production.json` — an toàn để commit.
- **Giữ khóa cẩn thận.** Mất khóa = không giải mã được → phải dán lại plaintext rồi mã hóa bằng khóa mới.
- **Phạm vi:** hook wired trong **cả POS.Api và POS.Web** (`Program.cs` mỗi project), đọc chung env
  `POS_SECRET_KEY`. **POS.Worker** vẫn plaintext — ngoài phạm vi bước này (chưa có hook).
- **Lưu ý naming:** service `webapp` trong `docker-compose.yml` (root) thực chất build/chạy **POS.Api**
  (tên service gây nhầm lẫn, giữ nguyên lịch sử) — không phải POS.Web. POS.Web không có trong compose
  này, deploy riêng qua `docker run` (xem `docs/guide-deploy.md`).

---

## Các bước

### Bước 1 — Tạo khóa AES-256
- **Cách A (trong app):** chạy app → đăng nhập SystemAdmin → vào `/admin/encrypt-secret` → bấm **"Tạo khóa mới"** → copy chuỗi base64.
- **Cách B (CLI):**
  ```bash
  openssl rand -base64 32
  ```

### Bước 2 — Nạp khóa vào môi trường (CHƯA đụng appsettings)

**Local/SIT (docker-compose.yml — chỉ có POS.Api + SQL Server):**
- Tạo/sửa file `.env` ở thư mục chứa `docker-compose.yml`:
  ```
  POS_SECRET_KEY=<chuỗi-base64-32-byte-vừa-tạo>
  ```
- `docker-compose.yml` đã có sẵn `POS_SECRET_KEY: ${POS_SECRET_KEY}` (service `webapp` = POS.Api) → tự đọc từ `.env`.
- Khởi động lại:
  ```bash
  docker compose up -d --build
  ```
  App vẫn chạy bình thường (chưa có `enc:` nào → hook no-op).

**UAT/PROD thật (docker run riêng từng container, xem `docs/guide-deploy.md`):**
- Cả 2 lệnh `docker run` (POS.Api §3.1 và POS.Web §3.2) đều cần thêm
  `-e POS_SECRET_KEY="${POS_SECRET_KEY}"` — **cùng giá trị khóa** cho cả 2 container.
- Chưa có `enc:` trong appsettings thì thêm biến này cũng vô hại (hook no-op).

*(Dev/IIS Express: đặt biến môi trường `POS_SECRET_KEY` cho tiến trình — nhưng Dev KHÔNG cần nếu không mã hóa base.)*

### Bước 3 — Mã hóa từng password (app đang chạy + đã có khóa)
Chỉ **POS.Web** có trang UI mã hóa — dùng trang này để sinh token cho **cả 2 file** (cùng khóa
`POS_SECRET_KEY` → token tạo ở POS.Web giải mã đúng khi POS.Api đọc). Vào `/admin/encrypt-secret`,
nhập lần lượt và bấm **Mã hóa**, copy chuỗi `enc:...`:

| Plaintext | Dùng cho |
|---|---|
| `VnDevops@2026!` | 8 connection string dùng `sa` (CentralMD, Loyalty, StagingDB, Partner, IFSAP, CentralGeneral, CentralSale, CentralSaleTemplate) — **lặp lại ở cả POS.Api và POS.Web** |
| `Invoice@123456` | connection string `EInvoice` — **cả 2 file** |
| `Msn@2024` | RabbitMQ Password — **cả 2 file** |

> Mỗi lần bấm cho ra ciphertext **khác nhau** (nonce ngẫu nhiên) nhưng đều giải mã về đúng plaintext →
> mã hóa 1 lần cho mỗi plaintext, rồi **dán cùng 1 token vào mọi chỗ có password đó, ở cả 2 file**.

### Bước 4 — Thay vào `appsettings.Production.json` (chỉ thay phần password)
Áp dụng cho **cả `src/POS.Api/appsettings.Production.json` và `src/POS.Web/appsettings.Production.json`**:
- **Connection string:** chỉ đổi đoạn `Password=...`, giữ nguyên phần còn lại:
  ```
  ...;User ID=sa;Password=enc:AAAA....;MultipleActiveResultSets=True;...
  ```
- **RabbitMQ:**
  ```json
  "Password": "enc:BBBB....",
  ```
- Làm tương tự cho cả 9 connection string + RabbitMQ **trong mỗi file** (2 file × 9 conn string + RabbitMQ).
  **Không** đụng `User ID`, host, catalog.

### Bước 5 — Khởi động lại & xác minh
```bash
docker compose up -d
```
(hoặc `docker run` lại từng container theo `docs/guide-deploy.md` nếu deploy UAT/PROD thật)
- ✅ POS.Api: `curl http://127.0.0.1:5001/health` → `"healthy"` (DB/Redis/RabbitMQ kết nối OK sau giải mã).
- ✅ POS.Web: app lên, đăng nhập + dashboard có dữ liệu → DB/RabbitMQ kết nối OK.
- 🔒 **Test fail-safe (cả 2 service):** tạm xóa `POS_SECRET_KEY` khỏi `.env`/container env rồi restart →
  app **phải báo lỗi khởi động rõ ràng** (`"Có giá trị cấu hình mã hóa (enc:...) nhưng thiếu ... POS_SECRET_KEY"`).
  Đặt khóa lại → chạy bình thường.

---

## Lưu ý vận hành

- **Commit:** `appsettings.Production.json` (chứa `enc:...`, cả 2 project) commit được; `.env` thì KHÔNG (đã ignore).
  Sau rollout, file Production không còn password thật.
- **Đổi khóa (rotation):** phải mã hóa lại tất cả token bằng khóa mới rồi thay đồng loạt **ở cả 2 file**.
- **Đổi password DB thật:** nhớ mã hóa lại token tương ứng, cập nhật **cả 2 file**.
- ⚠️ **Tách bạch:** bước này chỉ bịt việc *lộ password trong file*. Nó **không** thay cho việc cần **HTTPS** (C1)
  khi app ra internet — vẫn cần TLS + `Security:RequireHttps=true` trước khi go-live công khai.

---

## Cấu hình Production khác cần thực hiện khi go-live

Tất cả nằm trong khối `"Security"` của `src/POS.Web/appsettings.Production.json` (trừ ghi chú riêng).
Cơ chế đã có trong code; đây là **giá trị cần đặt** khi triển khai.

### C1 — Bật HTTPS + Cookie.Secure (khi đã có TLS)
- Hiện `"RequireHttps": false` → cookie `SameAsRequest`, KHÔNG redirect/HSTS (để test qua HTTP).
- Khi Production có HTTPS (Kestrel có cert, hoặc đặt sau thiết bị terminate TLS):
  ```json
  "Security": { "RequireHttps": true }
  ```
  → Cookie `Secure=Always` + `UseHsts()` (+ `UseHttpsRedirection()` vì `Mode=Internet`).
- ⚠️ App hiện HTTP-only (`Dockerfile: ASPNETCORE_URLS=http://+:8080`). Phải có lớp TLS thật trước khi bật, nếu không trình duyệt sẽ không gửi lại cookie Secure → mất đăng nhập.

### H2 — AllowedHosts = domain thật
- Hiện kế thừa `"AllowedHosts": "*"` từ base → hở Host-header injection.
- Thêm vào `appsettings.Production.json`:
  ```json
  "AllowedHosts": "dashboard.ten-mien-that.vn"
  ```

### H1 — Tắt SQL Console nếu không dùng qua internet
- SQL Console là bề mặt nguy hiểm nhất khi app ra internet công cộng.
- Nếu không thực sự cần, thêm vào khối `Security` của `appsettings.Production.json`:
  ```json
  "Security": { "EnableSqlConsole": false }
  ```
  → trang `/admin/sql-console` báo "đã bị tắt" và service từ chối mọi lệnh (gate cả 2 lớp).

### (Tham khảo) Nếu chuyển sang chạy SAU reverse proxy
- Đổi `"Security": { "Mode": "BehindProxy" }` và khai báo IP proxy thật:
  ```json
  "Security": { "KnownProxies": ["172.17.0.1"], "KnownNetworks": ["172.16.0.0/12"] }
  ```
  → app tin `X-Forwarded-*` CHỈ từ proxy đã khai (tránh giả mạo IP/scheme/host). Để trống = tạm tin mọi proxy + cảnh báo log.

---

## O1 — Sinh file master data .zip cho POS (POS.Api)

> Endpoint `GET api/posblue/GetFileFromFTP?...&typeSync=ALL` sinh master data thật từ CentralMD
> (SP1 `[SyncTable_Get]` + SP2 `[SyncGetDataByTable]`), nén `.zip`, POS tải qua `DowloadFileStream`.

- **`AppSettings:FtpRootPath`** (POS.Api `appsettings`) phải trỏ tới thư mục **tồn tại + ghi được** trên host API
  — đây là nơi ghi zip master data (`{FtpRootPath}/{pathSync}/{folderFile}/ALL_{site}_{terminal}_{yyyyMMdd}.zip`).
  Đây cũng là path mà tính năng sync/download dùng chung — kiểm tra quyền ghi của user chạy app.
- **2 stored procedure phải tồn tại** trên DB `CentralMD`: `[dbo].[SyncTable_Get]`, `[dbo].[SyncGetDataByTable]`.
- **BẮT BUỘC apply** `docs/sql/SyncGetDataByTable_AddFilter.sql` trên `CentralMD` (RPOSMasterData) — mở rộng
  `[SyncGetDataByTable]` thêm `@FilterColumn`/`@FilterValue` để lọc per-store (bảng `IsByStore=1`). Backward-compatible
  (default rỗng) nên apply trước khi deploy API là an toàn. **Nếu chưa apply** → API gọi SP với 5 tham số sẽ lỗi
  "too many arguments". Đảm bảo các cột filter (`Store.No`, `Staff.StoreNo`…) **có index** để seek nhanh.
- **Nên apply** `docs/sql/MasterDataDownloadLog.sql` trên `RPOSMasterData` — tạo bảng log lượt POS tải file
  (`DowloadFileStream`). App fail-safe: nếu bảng chưa tạo, download vẫn chạy, chỉ không ghi được log (nuốt lỗi).
- **Section `"MasterDataSync"`** trong `appsettings` (giá trị mặc định chạy được, chỉ chỉnh nếu cần):
  ```json
  "MasterDataSync": {
    "SqlCommandTimeoutSeconds": 600,
    "KeepZipDays": 2,
    "DateInZipName": true,
    "ZipCompressionLevel": "Fastest",
    "BatchSizePerFile": 10000,
    "MaxParallelTables": 4
  }
  ```
  - `SqlCommandTimeoutSeconds`: tăng nếu bảng master data lớn (timeout SP1/SP2).
  - `KeepZipDays`: lưới an toàn dọn file mồ côi; file cũ trong ngày đã tự xóa khi sinh file mới.
  - `DateInZipName`: giữ `true` để sang ngày mới tự sinh lại.
  - `ZipCompressionLevel`: `Fastest` (mặc định) — nhanh 2–5× so với `Optimal`, file lớn hơn ~10–30%.
  - `BatchSizePerFile`: 10000 rows/file — bảng lớn tách nhiều batch giúp POS import nhanh hơn.
  - `MaxParallelTables`: 4 — số bảng SP2 chạy song song. Bắt đầu 4; tăng nếu SQL Server còn headroom,
    giảm nếu DB bị quá tải. `≤ 0` = sequential (an toàn tuyệt đối nhưng chậm).
- **Đa-instance**: nếu chạy nhiều instance POS.Api **chung 1 thư mục FTP**, khóa keyed-SemaphoreSlim chỉ chặn
  trong từng process — atomic `File.Move(overwrite)` đảm bảo không hỏng file, xấu nhất là sinh trùng (chấp nhận được).
- **SHA-256 companion file**: sau khi sinh zip thành công, API tự tạo `{zipName}.sha256` cùng thư mục.
  Ops verify bằng `sha256sum {file}.zip` và so sánh với nội dung `.sha256`. File này cũng bị xóa cùng zip khi cleanup.
- **Production Ubuntu + nginx** — **KHÔNG** đổi `FtpRootPath` trong `appsettings.Production.json`, giữ
  nguyên `/app/ftpbluepos` (đúng sẵn). Thư mục thật nằm trên host Ubuntu, gắn vào container qua **bind
  mount** (`docker run -v` / `docker-compose.yml`), tạo + cấp quyền bằng `deploy/linux/setup-pos-dirs.sh`
  — chi tiết đầy đủ (path, quyền, kiểm tra) xem **`docs/deploy/ubuntu-guide.md`**. Tóm tắt path host:
  `/srv/pos/ftpbluepos` (PROD, SIT) / `/srv/pos/uat/ftpbluepos` (UAT — chạy chung host với PROD nên phải
  khác path, xem `docs/guide-deploy.md` §3.1).
  Nếu sau này cần bật `FolderShare` (nhánh `typeSync != ALL` trong `GetFileFromFTP`, đang tắt) hoặc
  `FolderShareUpdSource` (upgrade-tool distribution, đang tắt) — cả 2 key đọc path **tuyệt đối, thô**,
  KHÔNG qua `MapFtpPath` — phải đặt bằng path **container-side** nằm trong root đã mount, ví dụ
  `/app/ftpbluepos/upgrade`, KHÔNG phải path phía host.
  nginx cần tăng timeout (sinh zip lần đầu có thể 15–30s):
  ```nginx
  location /api/posblue/GetFileFromFTP { proxy_read_timeout 120s; }
  location /api/posblue/DowloadFileStream { proxy_buffering off; proxy_read_timeout 600s; proxy_send_timeout 600s; }
  ```
- **Redis key `MD:SyncTableList`** (SP1 cache, TTL 1h): tự invalidate. Nếu DBA thay đổi cấu hình
  `SyncTableList` và cần hiệu lực ngay → `DEL MD:SyncTableList` trên Redis.
- **Bảng `IsByStore=1`** lọc theo `siteCode` qua `ColumnFilter` (SP2 đã mở rộng) — đảm bảo cột filter
  (`Store.No`, `Staff.StoreNo`…) có index để seek nhanh.

---

## O3 — Nút SyncData "Đẩy dữ liệu đầu ngày" (POS.Web)

> Trang `/catalog/pos-setup` ([PosMapPage.razor](../src/POS.Web/Components/Pages/Ops/PosMapPage.razor)) có cột
> **Action → nút Sync** cho IT Ops chủ động sinh file master data đầu ngày cho 1 máy POS. Gọi **trực tiếp qua DI**
> (`ISyncDataPosService.PushStartOfDayDataAsync`) → tái dùng nguyên `IMasterDataSyncService.EnsureMasterDataFileAsync`
> (KHÔNG đổi logic sinh file txt/zip của POS.Api). Sinh zip full-data (`TypeSync=ALL`, tên `{site}_ALL_{terminal}_{yyyyMMdd}.zip`)
> đặt vào ĐÚNG thư mục POS.Api tạo/đọc khi máy POS request `CHANGE` (bám `MapFtpPath` như controller):
> `{AppSettings:FtpRootPath}\SyncDataPos\POS\CHANGE\{siteCode}\{posTerminal}`.

- **BẮT BUỘC (điểm quan trọng nhất)**: file sinh trên **filesystem của host POS.Web**, nhưng máy POS tải qua endpoint
  `DowloadFileStream` của **POS.Api**. Vì vậy `AppSettings:FtpRootPath` của **POS.Web phải trỏ tới ĐÚNG thư mục vật lý
  mà POS.Api đang phục vụ** — chung UNC share (Windows) hoặc chung bind-mount/volume (Docker).
  Nếu 2 app không chia sẻ thư mục này → file sinh ra máy POS **không thấy để tải**.
  (Thư mục đích dựng qua `MapFtpPath` = `FtpRootPath` + `SyncDataPos\POS\CHANGE\{site}\{terminal}` — KHÔNG dùng `FolderShare`.)
- **Hiện trạng cần sửa khi go-live**:
  - DEV (`appsettings.json`): `POS.Web:FtpRootPath` = `D:\ROOT\FTPBLUEPOS` — đã khớp POS.Api.
  - UAT/PROD (`appsettings.UAT.json` / `appsettings.Production.json` của POS.Web): `FtpRootPath` đang **rỗng**
    → phải điền path chỉ đến chung thư mục POS.Api phục vụ. Với Docker, POS.Web phải mount **cùng volume** `ftpbluepos`
    của POS.Api và đặt path container-side tương ứng (vd `/app/ftpbluepos`).
- **Idempotent theo ngày**: bấm lại trong cùng ngày trả nhanh zip đã có (không sinh lại). Sang ngày mới tự sinh lại
  (`DateInZipName=true`).
- **Không cần** cấu hình SysWebApi/X-API (vì gọi trực tiếp, không qua HTTP).
- **Quyền ghi**: user chạy POS.Web phải ghi được vào thư mục share; và 2 SP `[SyncTable_Get]` + `[SyncGetDataByTable]`
  (đã nêu ở O1) phải tồn tại trên CentralMD (POS.Web dùng chung connection CentralMD).

---

## O2 — Worker nạp sale từ file .zip (POS.Worker)

> `PosFileImportWorker` là **đường nạp sale thứ hai** (song song với `PosSalesConsumerWorker` đọc RabbitMQ).
> Worker quét `InboxFolder`, gặp `.zip` thì giải nén ra các `.txt` (mỗi file = 1 `KafkaMessageDto`) và insert DB
> qua đúng luồng `ICentralSaleRepository.InInsertToTableByJson` (source = `FILE` để truy vết trong `DataRawJson`).

- **Docker (SIT/UAT/PROD)**: `InboxFolder`/`ErrorFolder`/`WorkFolder` nằm **lồng bên trong** cùng bind
  mount `ftpbluepos` dùng chung với POS.Api (KHÔNG phải volume `/app/fileimport` riêng như trước đây) —
  xem `docs/deploy/ubuntu-guide.md` để biết đầy đủ path/quyền/setup. `deploy/linux/setup-pos-dirs.sh` tạo
  sẵn `SyncDataPos/Sale/{Kafka,BackupFiles,error,_work}` + cấp quyền, không cần tạo tay.
- **Dev/bare-metal (Windows)**: 3 thư mục theo path cấu hình trong `appsettings.json` — worker tự tạo khi
  chạy, chỉ cần ổ đĩa tồn tại + tài khoản chạy có quyền ghi (xem `deploy/windows/README.md`).
  - `InboxFolder` — nơi hệ thống nguồn đặt file `.zip` cần nạp.
  - `ErrorFolder` — worker move zip **xử lý thất bại** vào đây (giữ để retry/audit thủ công; worker **không** tự quét lại).
  - `WorkFolder` — thư mục temp giải nén. Được dọn sau mỗi file (cả khi thành công lẫn thất bại).
- ⚠️ **Docker (SIT/UAT/PROD) — xung đột đã biết, chấp nhận có chủ đích**: `InboxFolder` trỏ vào
  `SyncDataPos/Sale/Kafka` — **đúng folder mà `UploadFileSale` (POS.Api) đã và đang ghi file vào**, rồi
  tự xử lý ngay (in-process, đẩy Kafka). `PosFileImportWorker` giờ CŨNG poll folder này (chu kỳ 30s,
  insert thẳng DB, bỏ qua Kafka). Trong điều kiện bình thường `UploadFileSale` xử lý gần như ngay lập
  tức nên hầu như luôn "thắng" trước khi Worker kịp thấy file; Worker chủ yếu chỉ chạm tới file khi
  Kafka-push đã lỗi (trường hợp `RetryProcessSales` vốn dùng để retry thủ công) — nếu file đó không đúng
  định dạng `.txt`/`Type_PosNo_TransactionId.txt`, Worker sẽ move nó sang `SyncDataPos/Sale/error/` thay
  vì để `RetryProcessSales` xử lý. **Theo dõi `SyncDataPos/Sale/error/` định kỳ** — mỗi file ở đây là 1
  giao dịch sale chưa được xử lý bởi bất kỳ luồng nào, cần đối soát thủ công, KHÔNG tự động xóa.
- **Section `"FileImport"`** trong `appsettings` (Docker UAT/Production — giá trị giống hệt nhau ở 2 file,
  khác biệt UAT/PROD nằm ở host path bind-mount, không nằm ở giá trị container-side này):
  ```json
  "FileImport": {
    "Enabled": true,
    "InboxFolder": "/app/ftpbluepos/SyncDataPos/Sale/Kafka",
    "ErrorFolder": "/app/ftpbluepos/SyncDataPos/Sale/error",
    "WorkFolder": "/app/ftpbluepos/SyncDataPos/Sale/_work",
    "FileFilter": "*.zip",
    "PollIntervalSeconds": 30,
    "StableSeconds": 10,
    "MaxFilesPerCycle": 20,
    "Source": "FILE"
  }
  ```
  (`appsettings.json` base — dev Windows — vẫn giữ path `D:\ROOT\FILEIMPORT\...` cũ, không liên quan Docker.)
  - `Enabled`: đặt `false` để tắt worker (worker idle, không quét).
  - `StableSeconds`: bỏ qua zip mới ghi trong N giây (tránh nhận file đang upload dở). Tăng nếu nguồn ghi file chậm.
  - `PollIntervalSeconds`: chu kỳ quét; `MaxFilesPerCycle`: số zip xử lý tối đa mỗi vòng.
- **Định dạng file `.txt`** (BẮT BUỘC đúng để insert được):
  - **Tên file**: `Type_PosNo_TransactionId.txt` → worker tách lấy `Type`, `PosNo`, `TransactionId`;
    `StoreNo = LEFT(PosNo, 4)` (dùng route DB shard). Tên sai định dạng → file bị bỏ qua (log warning).
  - **Nội dung file**: JSON `{Type, Data}` (payload `KafkaMessagePOS` — chính là phần `Message` của
    message RabbitMQ, KHÔNG phải envelope `KafkaMessageDto`). Toàn bộ nội dung được truyền nguyên làm
    `message` cho SP `Sale_InsertDataByOrder_KAFKA`.
  - Zip không có `.txt`, tên file sai, hoặc SP báo lỗi → có ≥1 record lỗi → zip vào `ErrorFolder`.
- **Vòng đời zip**: xử lý **toàn bộ .txt OK** → xóa zip; có **≥1 record lỗi** → move zip sang `ErrorFolder` (tên gắn timestamp+guid).
- **Đa-instance**: worker "claim" zip bằng `File.Move` khỏi inbox trước khi xử lý → an toàn khi chạy nhiều instance chung thư mục
  (instance không claim được sẽ bỏ qua). Không cần khóa ngoài.
- **Giám sát**: heartbeat ghi Redis key **`Worker:Heartbeat:PosFileImport`** (TTL ~3× interval) — dùng cho trang Ops theo dõi worker.

---

## O4 — Log request/response toàn cục (POS.Api)

> `RequestResponseLoggingMiddleware` (`src/POS.Api/Middleware/RequestResponseLoggingMiddleware.cs`)
> log request/response cho **mọi** endpoint POS.Api qua `IKibanaService` — thay thế các lời gọi
> `LogRequest` thủ công rải rác trước đây (chỉ có ở 3/9 controller, không nhất quán, dùng để debug
> các API chưa rõ POS gửi request/response ra sao, ví dụ vụ `UploadFileLogJob`).

- **Mặc định TẮT** (`RequestLogging:Enabled=false`) ở mọi môi trường — bật thủ công (đổi config,
  restart, không cần build lại) khi cần debug 1 đợt cụ thể, tắt lại sau khi xong.
- **`RequestLogging:PersistToFile`** (mặc định `true`) — vì **Elasticsearch chưa được cài đặt** ở
  thời điểm viết tài liệu này, log Request/Response cần có bản ghi trên đĩa server (`pos-*.log`,
  đọc từ `Logging:FileLogDirectory`) để còn tra cứu được. **Sau khi Elasticsearch được cài đặt và
  xác nhận hoạt động ổn định**, có thể đổi `PersistToFile=false` để log Request/Response chỉ gửi
  Elasticsearch, giảm I/O đĩa (log Exception/Info không bị ảnh hưởng bởi cờ này, luôn ghi đủ file).
- Không cần thêm hạ tầng gì khác — middleware tái dùng nguyên `IKibanaService`/Serilog pipeline đã
  có sẵn; Elasticsearch sink tự no-op nếu `Elasticsearch:Nodes` rỗng.
- Body request/response bị cắt theo `RequestLogging:MaxBodyBytes` (mặc định 8192 byte); multipart
  upload (`UploadFileLogJob`, `UploadFileSale`) và response nhị phân (`DowloadFileStream`, zip) chỉ
  log metadata, không capture nội dung file.

---

## D1 — Stored Procedures Cài đặt CTKM (11.1)

> Trang `GET /promotion/setup` (POS.Web) lưu/duyệt CTKM qua các SP dưới đây trên **CentralMD (RPOSMasterData)**.
> Bảng `SetupPromotionHEADER/BUY/GET/SITE` và SP `[dbo].[Setup_Promotion_Insert]` được xác nhận **đã có sẵn**.

- **BẮT BUỘC chạy 2 script** (tạo SP + TVP mới) trên `RPOSMasterData` trước khi dùng trang:
  - `docs/sql/SetupPromotion_Save.sql` — tạo 3 TYPE TVP (`SetupPromotionBuyTVP`/`GetTVP`/`SiteTVP`) + `dbo.usp_SaveSetupCTKMAll`
    (upsert header + replace Buy/Get/Site theo BBYNR, transaction). **Phase 2: `usp_SaveSetupCTKMAll` đã thêm tham số
    advanced** (LimitQty/MemberOnly/MemberCode/Priority/NumOfDays/Voucher*) → **chạy lại script này** để cập nhật proc
    (idempotent: tự DROP/CREATE). TVP không đổi.
    **Fix 2026-07-01:** nhánh INSERT header (tạo mới) giờ tự kiểm tra `sys.columns.is_identity` cho cột `ID` trước khi
    insert — trước đó giả định `ID` luôn là IDENTITY (theo EDMX legacy) nên môi trường nào có `ID` không phải IDENTITY
    sẽ lỗi `Cannot insert the value NULL into column 'ID'`. **Chạy lại script này** trên các môi trường đã deploy bản cũ.
  - `docs/sql/SetupPromotion_ApproveAndStatus.sql` — `dbo.usp_SetupPromotion_Approve` (đánh dấu duyệt + EXEC `Setup_Promotion_Insert` publish sang Offer*) và `dbo.usp_SetupPromotion_UpdateStatus`.
- **SP tái dùng (đã có, không tạo lại):** `[dbo].[Setup_Promotion_Insert] @BBY`.
- **Nếu chưa chạy script** → trang báo lỗi khi Lưu/Duyệt (SP không tồn tại). Repository nuốt lỗi, hiện snackbar đỏ.
- Cột INSERT bám đúng schema legacy; nếu sau này `SetupPromotionHEADER` thêm cột NOT NULL mới → cập nhật `usp_SaveSetupCTKMAll`.
- **Lưu ý tên bảng:** bảng nhóm cửa hàng vật lý là `dbo.SetupGroupSite` (số ít) — tên DbSet EF legacy `SetupGroupSites`
  (số nhiều) chỉ là tên property C#, không phải tên bảng SQL thật.

---

## D2 — Stored Procedures Special Combo (11.2)

> Trang `GET /promotion/special-combo` (POS.Web) đọc/lưu combo qua các SP dưới đây trên **CentralMD (RPOSMasterData)**.
> 3 bảng `SpecialComboHeader/Line/Store` được xác nhận **đã có sẵn**.

- **BẮT BUỘC chạy 3 script** trên `RPOSMasterData` trước khi dùng trang:
  - `docs/sql/SpecialCombo_Read.sql` — `usp_SpecialCombo_GetList` (list + paging), `usp_SpecialCombo_GetDetail` (header + lines + stores).
  - `docs/sql/SpecialCombo_Save.sql` — 2 TYPE TVP (`SpecialComboLineTVP`, `SpecialComboStoreTVP`) + `usp_SpecialCombo_Save`
    (upsert header + replace lines/stores, transaction; `PriceMode` 0/1/2 → `IsDefault`/`IsDynamicPrice`).
  - `docs/sql/SpecialCombo_Status.sql` — `usp_SpecialCombo_UpdateStatus` (bật/tắt), `usp_SpecialCombo_Delete` (xóa header+lines+stores).
- **Nếu chưa chạy** → trang báo lỗi khi tải/lưu (SP không tồn tại); repository nuốt lỗi, hiện snackbar đỏ.
- Code combo auto-gen phía repository (`S{yyyyMMddHHmmss}`) khi tạo mới; sửa thì giữ Code cũ.

---

## D3 — Stored Procedures Setup Coupon (8.1/8.2)

> Trang `GET /promotion/coupons` (danh sách master Coupon/Voucher — **read-only**) và
> `/promotion/coupons/issue` (phát hành + nâng cao) đọc/ghi qua các SP dưới đây trên
> **CentralMD (RPOSMasterData)**. 5 bảng `CpnVchBOMIssueRule`, `CpnVchBOMHeader`,
> `CpnVchBOMCodeIssue`, `CpnVchBOMLine`, `CpnVchBOMStore` được xác nhận **đã có sẵn**.
> ⚠️ Legacy dùng **EF LINQ trực tiếp** (không có SP) — các SP dưới đây là **mới**, viết lại cho .NET 10.

- **BẮT BUỘC chạy 4 script** trên `RPOSMasterData` trước khi dùng trang:
  - `docs/sql/CpnVchBOMHeader_GetList.sql` — `usp_CpnVchBOMHeader_GetList` (danh sách master
    Coupon/Voucher: list **thẳng** `CpnVchBOMHeader`, KHÔNG join IssueRule, mọi ArticleType, filter
    KeyWord/Type/Status, Status theo EndingDate). **Đây là SP mà trang `/promotion/coupons` dùng** (port
    trung thực từ SP legacy `GetCpnVchBOMHeaderList`).
  - `docs/sql/SetupCoupon_Read.sql` — `usp_SetupCoupon_GetList` (list join IssueRule+Header — nay chỉ giữ
    cho khả năng tái dùng ở màn "Phát hành Coupon"/POS.Api, KHÔNG còn dùng ở trang list), `usp_SetupCoupon_GetCodes`
    (mã coupon theo ItemNo), `usp_SetupCoupon_GetDetail` (header+rule + danh sách sản phẩm — dùng khi sửa ở issue page).
  - `docs/sql/SetupCoupon_Save.sql` — 2 TYPE TVP (`CouponCodeTVP`, `CouponLineTVP`) +
    `usp_SetupCoupon_CheckCodesExist` (check trùng mã) + `usp_SetupCoupon_SaveIssue` (upsert IssueRule+Header,
    insert Codes 1 lần, replace Lines/Stores, tự sinh ItemNo `C7...`, transaction) +
    `usp_SetupCoupon_SaveAdvanced` (upsert field nâng cao: discount/limit/blocked...).
  - `docs/sql/SetupCoupon_Delete.sql` — `usp_SetupCoupon_Delete` (guard: chỉ xóa khi QtyCoupon==0, trả Deleted+Message).
- **Sinh mã Auto** chạy ở tầng Application (`CouponService`, C#) — không nằm trong SP; SP chỉ nhận danh sách mã qua TVP.
- **Nếu chưa chạy script** → trang báo lỗi khi tải/lưu/xóa (SP không tồn tại); service nuốt lỗi, hiện snackbar đỏ.
- Store áp dụng: `StoreGroupCode='ALL'` → 1 dòng `StoreNo='ALL'`; ngược lại bung theo `dbo.StoreGroup` (GroupCode).

---

## D4 — Stored Procedures Voucher (8.3/8.4)

> Trang `/promotion/vouchers` (8.3 CRUD) và `/promotion/vouchers-published` (8.4 tra cứu).
> ⚠️ 8.3 dùng CHUNG bảng `CpnVchBOMHeader`/`CpnVchBOMLine` với Coupon (8.1/8.2) — phân tách bằng
> **NOT EXISTS CpnVchBOMIssueRule** (voucher = nhập serial thủ công; coupon = có sinh mã). Legacy dùng EF LINQ,
> các SP dưới đây là **mới**.

- **BẮT BUỘC chạy 4 script** trên `RPOSMasterData` trước khi dùng trang 8.3:
  - `docs/sql/SetupVoucher_Read.sql` — `usp_SetupVoucher_GetList` (filter + paging, lọc NOT EXISTS IssueRule),
    `usp_SetupVoucher_GetDetail` (header + sản phẩm áp dụng + `QuantityCode` = COUNT mã Source='VOUCHER').
    **Chạy lại bản mới** (đã thêm cột `QuantityCode` cho trang Phát hành Voucher).
  - `docs/sql/SetupVoucher_Save.sql` — TVP `dbo.VoucherLineTVP` + `usp_SetupVoucher_Save` (upsert header + replace
    lines, transaction). **ItemNo voucher = số thuần seed 70000001** (bỏ qua mã coupon 'C...'). **Serial (CouponCode)
    bắt buộc duy nhất.** **IsCheckItem=1 → tổng bill (no lines); =0 → theo sản phẩm** (NGƯỢC nghĩa coupon).
  - `docs/sql/SetupVoucher_SaveIssue.sql` — **MỚI (trang Phát hành Voucher), chạy lại bản mới nhất**:
    `usp_SetupVoucher_SaveIssue` (upsert header + mint mã vào `CpnVchBOMCodeIssue` với `Source='VOUCHER'`,
    **KHÔNG** ghi IssueRule → giữ tách khỏi Coupon; điền đủ field redeem `Status='SOLD'`/`Value`/`VoucherType`),
    `usp_SetupVoucher_GetCodes` (list mã — **đã thêm cột `Status`/`AmountUsed`/`OrderUsed`** cho tab "Mã đã phát
    hành"), `usp_SetupVoucher_CheckCodesExist` (check trùng mã toàn bảng). Tái dùng TVP `dbo.CouponCodeTVP`/
    `dbo.VoucherLineTVP`.
  - `docs/sql/SetupVoucher_Delete.sql` — **Chạy lại bản mới**: `usp_SetupVoucher_Delete` xóa cascade
    `CpnVchBOMCodeIssue` (Source='VOUCHER') + `CpnVchBOMLine` + `CpnVchBOMHeader`; **chặn xóa** nếu có mã
    `Status='RDM'` (đã sử dụng) → trả lỗi "Voucher đã được sử dụng" (khi đó dùng nút Xem để bật Blocked thay vì xóa).
  - `docs/sql/SetupVoucher_UpdateBlocked.sql` — **MỚI**: `usp_SetupVoucher_UpdateBlocked` cập nhật riêng
    field `Blocked` — trang Xem voucher (sau phát hành) chỉ còn cho phép khóa/mở khóa, mọi field khác readonly.
- **8.4 KHÔNG cần SP mới** — tái dùng SP có sẵn **`[dbo].[GetTransCpnVchIssueList]` trên CentralSales**
  (đọc `TransCpnVchIssue`, routed per-store qua `StoreRoutedConnectionFactory`). Đảm bảo SP này tồn tại trên
  mọi server CentralSales. StoreNo là **bắt buộc**; `@Export=1` (paging) / `=2` (export). Resend-SAP: **HOÃN** (phase sau).
- **Nếu chưa chạy** → trang báo lỗi khi tải/lưu; service nuốt lỗi, hiện snackbar/banner đỏ.

---

## D5 — Stored Procedures Setup Giá (9.3)

> Trang `/catalog/price-setup` (POS.Web) validate import Excel + lưu bảng giá qua TVP + SP dưới đây
> trên **CentralMD (RPOSMasterData)**. Port từ VCM.BLUEPOS `SetupPriceData.SaveSalesPrice`.

- **BẮT BUỘC chạy 1 script** trên `RPOSMasterData` trước khi dùng trang:
  - `docs/sql/SetupSalePrice_Save.sql` — 2 TYPE TVP (`SetupSalePriceImportTVP`, `SetupSalePriceLineTVP`) +
    `dbo.usp_SetupSalePrice_Save` (INSERT Pkey mới + ủy quyền update Pkey đã tồn tại qua `Setup_SalePrice_Get_ALL`).
- **⚠️ 2026-07-03 — CHẠY LẠI script này** (fix lỗi 266 *"Transaction count ... mismatching BEGIN and COMMIT"*
  khi Lưu bảng giá với Pkey đã tồn tại): `usp_SetupSalePrice_Save` nay **COMMIT phần INSERT TRƯỚC** rồi mới
  `EXEC Setup_SalePrice_Get_ALL` **NGOÀI transaction** (SP legacy tự quản transaction; nếu lồng trong `BEGIN TRAN`
  của ta sẽ lệch `@@TRANCOUNT`). Không re-apply → tiếp tục lỗi "Lưu bảng giá thất bại".
- **⚠️ Schema `dbo.SalesPrice`**: SP INSERT đúng **15 cột** thực có của bảng — **KHÔNG** ghi `IsActive` /
  `LastTimeUpdate` / `Id` (những cột này KHÔNG tồn tại trong schema hiện hành, khác EF model legacy .NET 4.6).
  Nếu deploy proc cũ (còn `IsActive`/`LastTimeUpdate`) → lưu giá lỗi `Invalid column name`. **Chạy lại script này**
  để cập nhật proc theo schema đúng.
- **PHỤ THUỘC**: SP có sẵn **`[dbo].[Setup_SalePrice_Get_ALL]`** phải tồn tại trên `RPOSMasterData` (legacy dùng cho
  update Pkey đã tồn tại) và cũng phải khớp schema 15 cột nói trên. `SalesType` là cột `[int]` → giá trị mã hình
  thức bán hàng phải là số hợp lệ (SQL convert ngầm khi INSERT).
- **Nếu chưa chạy / proc cũ** → trang báo lỗi khi lưu; service trả `Ok=0`, hiện snackbar đỏ.

---

## D6 — Gộp SAP Internal Voucher vào CpnVchBOMCodeIssue

> `api/sap/*` (`SAPController`, 5.000 POS + SAP ERP) chuyển từ bảng `Internal_Voucher` sang dùng
> CHUNG bảng `CpnVchBOMCodeIssue` với Setup Coupon (8.1/8.2), phân biệt bằng cột `Source`
> (`'COUPON'` | `'SAP'`). Code đã đổi sang `IVoucherCodeRepository`/`VoucherCodeRepository` +
> SP `usp_Voucher_*` — **JSON contract của `api/sap/*` KHÔNG đổi**, chỉ đổi tầng lưu trữ bên dưới.
> ⚠️ Đây là thao tác trên **dữ liệu production thật** (voucher SAP đang lưu hành) — cần cửa sổ
> bảo trì ngắn (Phase B rebuild bảng dùng `sp_rename`) và làm đúng thứ tự.

**BẮT BUỘC chạy theo ĐÚNG thứ tự sau** trên `RPOSMasterData` (Dev/UAT trước, Production sau,
khung giờ ít traffic SAP/POS):

1. `docs/sql/CpnVchBOMCodeIssue_ExtendSchema.sql` — Phase A (thêm cột + `Source`), Phase B
   (**rebuild bảng** để thêm `ID IDENTITY(1,1) PRIMARY KEY CLUSTERED` — trước đó bảng không có
   PK, tự tính `MAX(ID)+ROW_NUMBER()`), Phase C (`UNIQUE FILTERED INDEX` trên `Code`). **Chạy
   pre-check trùng `Code`** trước Phase C (query có sẵn trong comment script) — phải rỗng.
2. `docs/sql/SetupCoupon_Save.sql` + `docs/sql/SetupCoupon_Read.sql` (bản đã cập nhật — bỏ tự
   tính `ID`, thêm `Source='COUPON'`/filter `Source='COUPON'`) — **BẮT BUỘC chạy lại**, nếu quên
   thì phát hành coupon mới sẽ lỗi insert vào cột đã đổi thành IDENTITY.
3. `docs/sql/Voucher_Read.sql` — tạo `usp_Voucher_GetByCode`.
4. `docs/sql/Voucher_Save.sql` — tạo TVP `dbo.VoucherRedeemTVP` + `usp_Voucher_Create` +
   `usp_Voucher_Redeem`.
5. `docs/sql/CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql` — di chuyển dữ liệu từ
   `Internal_Voucher` (idempotent, `WHERE NOT EXISTS` — chạy lại an toàn). **Chạy 2 LẦN**: trước
   khi deploy code mới, và **ngay sau khi deploy xong** (vét dữ liệu ghi bởi code cũ trong lúc
   rolling deploy có khoảng chồng lấp giữa các instance POS.Api).
   - Verify trước khi coi là xong: `COUNT(*)` của `Internal_Voucher` khớp
     `COUNT(*) FROM CpnVchBOMCodeIssue WHERE Source='SAP'`; query "còn sót" (comment sẵn trong
     script) phải trả về rỗng.

**Sau khi deploy code + verify runtime ổn định** (smoke test `CreateNewVoucher`/`CheckVoucher`/
`redeemCpnVch` trên vài voucher thật, theo dõi log lỗi):

6. `docs/sql/Internal_Voucher_RenameLegacy.sql` — `sp_rename` bảng `Internal_Voucher` thành
   `Internal_Voucher_Legacy`. ⚠️ **ĐIỂM KHÔNG THỂ QUAY LẠI DỄ DÀNG** — sau bước này, rollback code
   về bản cũ sẽ lỗi cứng (bảng không còn tên cũ) thay vì âm thầm ghi nhầm dữ liệu; đây là lựa
   chọn chủ đích (fail loud). Giữ `Internal_Voucher_Legacy` làm backup tạm.
   - **TODO chưa chốt ngày**: lên lịch `DROP TABLE Internal_Voucher_Legacy` sau khi hệ thống ổn
     định 2–4 tuần kể từ go-live (theo dõi riêng, không thuộc phạm vi đợt deploy code này).

**Nếu chưa chạy đủ 5 script đầu trước khi deploy** → `api/sap/*` lỗi runtime (SP không tồn tại
hoặc bảng thiếu cột) ngay khi POS/SAP gọi vào; **không** có fallback tự động về `Internal_Voucher`
(code cũ đã xóa khỏi solution).

### D6.1 — Vá lỗ hổng đồng bộ dữ liệu Coupon ↔ SAP Voucher (bổ sung sau go-live §D6)

> Sau khi dùng thử §D6, phát hiện: (1) `usp_Voucher_Create` không ghi `ItemNo`; (2)
> `usp_SetupCoupon_SaveIssue` chỉ ghi 7/22 cột khi insert mã coupon — thiếu toàn bộ field cần để
> POS.Api check/redeem được; (3) `usp_Voucher_GetByCode`/`usp_Voucher_Redeem` chỉ nhận
> `Source='SAP'`, chưa nhận diện được mã Coupon dù dùng chung bảng. Cả 3 đã được vá — cần chạy
> lại **4 script sau, ĐÚNG THỨ TỰ**, trên cùng khung giờ bảo trì như §D6 (không đổi code
> C#/không cần build lại POS.Api/POS.Web):

1. `docs/sql/CpnVchBOMCodeIssue_ItemNoHardening.sql` — **PHẢI chạy đầu tiên**: mở rộng
   `ItemNo` varchar(20)→varchar(50) (khớp width `ActicleNo`, tránh lỗi truncate khi SAP gửi
   `Article_No` dài) + thêm index `IX_CpnVchBOMCodeIssue_ItemNo`.
2. `docs/sql/SetupCoupon_Save.sql` (bản đã cập nhật lần 2) — insert mã Coupon nay điền đủ
   `ActicleNo/ActicleType/Validity_From_Date/Expiry_Date/Voucher_Currency/CompanyCode/Status/
   [Return]`, thêm UPDATE đồng bộ mọi lần Lưu (Section 3b) + UPDATE `Value/VoucherType` trong
   `usp_SetupCoupon_SaveAdvanced` (Section 3c).
3. `docs/sql/Voucher_Read.sql` (bản đã cập nhật) — `usp_Voucher_GetByCode` bỏ filter `Source`.
4. `docs/sql/Voucher_Save.sql` (bản đã cập nhật) — `usp_Voucher_Create` thêm `ItemNo` + guard
   trùng Code khác Source; `usp_Voucher_Redeem` bỏ filter `Source` + thêm `Enabled=0` khi redeem.

Smoke test sau khi chạy: phát hành 1 coupon test qua POS.Web → `GET api/sap/CheckVoucher` với mã
đó phải trả `200 OK` (trước đây 404) → `POST api/sap/winlife/redeemCpnVch` phải redeem thành
công → coupon hiện "Locked" ở tab "Mã coupon đã phát hành" (POS.Web).

---

## Tham chiếu cơ chế (đã có trong code)

| Thành phần | Vị trí |
|---|---|
| Mã hóa/giải mã AES-256-GCM | `src/POS.Infrastructure/Security/SecretProtector.cs` |
| Hook giải mã `enc:` lúc khởi động | `src/POS.Api/Program.cs` **và** `src/POS.Web/Program.cs` (ngay sau `CreateBuilder`, trước `AddInfrastructure`) |
| Trang tạo khóa / mã hóa | `src/POS.Web/Components/Pages/Admin/EncryptSecretPage.razor` (`/admin/encrypt-secret`, SystemAdmin) — dùng chung cho token của cả 2 project |
| Truyền khóa qua container (local/SIT) | `docker-compose.yml` → `POS_SECRET_KEY: ${POS_SECRET_KEY}` (service `webapp` = POS.Api, lấy từ `.env`) |
| Truyền khóa qua container (UAT/PROD) | `docker run -e POS_SECRET_KEY=...` cho cả POS.Api và POS.Web (xem `docs/guide-deploy.md`) |
| Mẫu file env | `.env.example` |

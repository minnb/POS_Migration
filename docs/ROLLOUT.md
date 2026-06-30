# Rollout — Cấu hình bảo mật Production (POS.Web)

> Tài liệu **trung tâm** cho mọi cấu hình cần thao tác khi go-live Production.
> Mỗi khi thêm cấu hình bảo mật/hạ tầng mới cần người vận hành thực hiện, **cập nhật file này**.

## Checklist rollout (tổng quan)

| # | Hạng mục | Việc cần làm khi go-live | Mức | Mục chi tiết |
|---|---|---|---|---|
| C4 | Mã hóa credentials | Tạo khóa → `.env` → mã hóa → thay `enc:...` trong Production.json | CRITICAL | [§C4](#c4--mã-hóa-credentials-trong-appsettings) |
| C1 | HTTPS + Cookie.Secure | Có TLS → đặt `Security:RequireHttps=true` | CRITICAL (khi ra internet) | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| H2 | AllowedHosts | Đặt domain dashboard thật thay cho `"*"` | HIGH | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| H1 | SQL Console | Cân nhắc `Security:EnableSqlConsole=false` | HIGH | [§Cấu hình khác](#cấu-hình-production-khác-cần-thực-hiện-khi-go-live) |
| O1 | Master data sync (POS.Api) | Đảm bảo `FtpRootPath` ghi được + tinh chỉnh `MasterDataSync` | MEDIUM | [§O1](#o1--sinh-file-master-data-zip-cho-pos-posapi) |
| D1 | SP Cài đặt CTKM (11.1) | Chạy 2 script SQL tạo SP trên CentralMD | REQUIRED (cho `/promotion/setup`) | [§D1](#d1--stored-procedures-cài-đặt-ctkm-111) |
| D2 | SP Special Combo (11.2) | Chạy 3 script SQL tạo SP trên CentralMD | REQUIRED (cho `/promotion/special-combo`) | [§D2](#d2--stored-procedures-special-combo-112) |

---

## C4 — Mã hóa credentials trong appsettings

> Mã hóa password DB/RabbitMQ trong `appsettings.Production.json` bằng AES-256-GCM.
> Cơ chế đã build sẵn (`SecretProtector` + hook giải mã trong `Program.cs` + trang `/admin/encrypt-secret`).
> Việc rollout do **người vận hành** thực hiện vì cần khóa bí mật — Claude không giữ khóa, không thay password thật.

---

## Nguyên tắc an toàn (đọc trước)

- **Chỉ mã hóa `src/POS.Web/appsettings.Production.json`** — KHÔNG mã hóa `appsettings.json` (base).
  Hook giải mã chạy ở **mọi môi trường**: nếu base có `enc:` mà máy Dev không set khóa → app **fail-fast, không khởi động**.
  Để base plaintext thì Dev chạy bình thường, không cần khóa.
- **Khóa KHÔNG bao giờ vào git.** Khóa nằm ở `.env` (đã `.gitignore`) hoặc env của host.
  Ciphertext `enc:...` nằm trong `appsettings.Production.json` — an toàn để commit.
- **Giữ khóa cẩn thận.** Mất khóa = không giải mã được → phải dán lại plaintext rồi mã hóa bằng khóa mới.
- **Phạm vi:** hook chỉ wired trong **POS.Web**. POS.Api / POS.Worker (nếu dùng chung DB) vẫn plaintext — ngoài phạm vi bước này.

---

## Các bước

### Bước 1 — Tạo khóa AES-256
- **Cách A (trong app):** chạy app → đăng nhập SystemAdmin → vào `/admin/encrypt-secret` → bấm **"Tạo khóa mới"** → copy chuỗi base64.
- **Cách B (CLI):**
  ```bash
  openssl rand -base64 32
  ```

### Bước 2 — Nạp khóa vào môi trường (CHƯA đụng appsettings)
- Tạo/sửa file `.env` ở thư mục chứa `docker-compose.yml`:
  ```
  POSWEB_SECRET_KEY=<chuỗi-base64-32-byte-vừa-tạo>
  ```
- `docker-compose.yml` đã có sẵn `POSWEB_SECRET_KEY: ${POSWEB_SECRET_KEY}` (service `webapp`) → tự đọc từ `.env`.
- Khởi động lại app:
  ```bash
  docker compose up -d --build
  ```
  App vẫn chạy bình thường (chưa có `enc:` nào → hook no-op).
- *(Dev/IIS Express: đặt biến môi trường `POSWEB_SECRET_KEY` cho tiến trình — nhưng Dev KHÔNG cần nếu không mã hóa base.)*

### Bước 3 — Mã hóa từng password (app đang chạy + đã có khóa)
Vào `/admin/encrypt-secret`, nhập lần lượt và bấm **Mã hóa**, copy chuỗi `enc:...`:

| Plaintext | Dùng cho |
|---|---|
| `VnDevops@2026!` | 8 connection string dùng `sa` (CentralMD, Loyalty, StagingDB, Partner, IFSAP, CentralGeneral, CentralSale, CentralSaleTemplate) |
| `Invoice@123456` | connection string `EInvoice` |
| `Msn@2024` | RabbitMQ Password |

> Mỗi lần bấm cho ra ciphertext **khác nhau** (nonce ngẫu nhiên) nhưng đều giải mã về đúng plaintext →
> có thể mã hóa 1 lần rồi **dán cùng 1 token cho các chuỗi cùng password**.

### Bước 4 — Thay vào `appsettings.Production.json` (chỉ thay phần password)
- **Connection string:** chỉ đổi đoạn `Password=...`, giữ nguyên phần còn lại:
  ```
  ...;User ID=sa;Password=enc:AAAA....;MultipleActiveResultSets=True;...
  ```
- **RabbitMQ:**
  ```json
  "Password": "enc:BBBB....",
  ```
- Làm tương tự cho cả 9 connection string + RabbitMQ. **Không** đụng `User ID`, host, catalog.

### Bước 5 — Khởi động lại & xác minh
```bash
docker compose up -d
```
- ✅ App lên, đăng nhập + dashboard có dữ liệu → DB/RabbitMQ kết nối OK (đã giải mã đúng).
- 🔒 **Test fail-safe:** tạm xóa `POSWEB_SECRET_KEY` khỏi `.env` rồi restart → app **phải báo lỗi khởi động rõ ràng**
  (`"Có giá trị cấu hình mã hóa (enc:...) nhưng thiếu ... POSWEB_SECRET_KEY"`). Đặt khóa lại → chạy bình thường.

---

## Lưu ý vận hành

- **Commit:** `appsettings.Production.json` (chứa `enc:...`) commit được; `.env` thì KHÔNG (đã ignore).
  Sau rollout, file Production không còn password thật.
- **Đổi khóa (rotation):** phải mã hóa lại tất cả token bằng khóa mới rồi thay đồng loạt.
- **Đổi password DB thật:** nhớ mã hóa lại token tương ứng.
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
- **Production Ubuntu + nginx** — thay giá trị trong `appsettings.Production.json`:
  ```json
  "AppSettings": {
    "FtpRootPath": "/opt/posapi/ftpbluepos",
    "FolderShare": "/opt/posapi/ftpbluepos",
    "FolderShareAPIBluePOS": "",
    "FolderShareUpdSource": "/opt/posapi/ftpbluepos/upgrade"
  }
  ```
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

## D1 — Stored Procedures Cài đặt CTKM (11.1)

> Trang `GET /promotion/setup` (POS.Web) lưu/duyệt CTKM qua các SP dưới đây trên **CentralMD (RPOSMasterData)**.
> Bảng `SetupPromotionHEADER/BUY/GET/SITE` và SP `[dbo].[Setup_Promotion_Insert]` được xác nhận **đã có sẵn**.

- **BẮT BUỘC chạy 2 script** (tạo SP + TVP mới) trên `RPOSMasterData` trước khi dùng trang:
  - `docs/sql/SetupPromotion_Save.sql` — tạo 3 TYPE TVP (`SetupPromotionBuyTVP`/`GetTVP`/`SiteTVP`) + `dbo.usp_SaveSetupCTKMAll`
    (upsert header + replace Buy/Get/Site theo BBYNR, transaction). **Phase 2: `usp_SaveSetupCTKMAll` đã thêm tham số
    advanced** (LimitQty/MemberOnly/MemberCode/Priority/NumOfDays/Voucher*) → **chạy lại script này** để cập nhật proc
    (idempotent: tự DROP/CREATE). TVP không đổi.
  - `docs/sql/SetupPromotion_ApproveAndStatus.sql` — `dbo.usp_SetupPromotion_Approve` (đánh dấu duyệt + EXEC `Setup_Promotion_Insert` publish sang Offer*) và `dbo.usp_SetupPromotion_UpdateStatus`.
- **SP tái dùng (đã có, không tạo lại):** `[dbo].[Setup_Promotion_Insert] @BBY`.
- **Nếu chưa chạy script** → trang báo lỗi khi Lưu/Duyệt (SP không tồn tại). Repository nuốt lỗi, hiện snackbar đỏ.
- Cột INSERT bám đúng schema legacy; nếu sau này `SetupPromotionHEADER` thêm cột NOT NULL mới → cập nhật `usp_SaveSetupCTKMAll`.

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

## Tham chiếu cơ chế (đã có trong code)

| Thành phần | Vị trí |
|---|---|
| Mã hóa/giải mã AES-256-GCM | `src/POS.Infrastructure/Security/SecretProtector.cs` |
| Hook giải mã `enc:` lúc khởi động | `src/POS.Web/Program.cs` (ngay sau `CreateBuilder`) |
| Trang tạo khóa / mã hóa | `src/POS.Web/Components/Pages/Admin/EncryptSecretPage.razor` (`/admin/encrypt-secret`, SystemAdmin) |
| Truyền khóa qua container | `docker-compose.yml` → `POSWEB_SECRET_KEY: ${POSWEB_SECRET_KEY}` (lấy từ `.env`) |
| Mẫu file env | `.env.example` |

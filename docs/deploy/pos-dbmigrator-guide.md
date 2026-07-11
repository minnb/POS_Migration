# POS.DbMigrator — Hướng dẫn triển khai & vận hành

> Runbook cho DevOps/DBA. Nguồn: đọc trực tiếp `tools/POS.DbMigrator/*.cs`,
> `tools/POS.DbMigrator/POS.DbMigrator.csproj`, và tự chạy thật từng lệnh trong tài liệu này để lấy
> output/error message chính xác (không phải suy đoán). Thiết kế tổng thể + quyết định kiến trúc:
> xem `docs/ROLLOUT.md` §D0. Quy ước đăng ký script mới: `.claude/skills/database/SKILLS.md`.

---

## 1. Tổng quan & luồng hoạt động

### 1.1. Mục đích

`POS.DbMigrator` là console app (.NET 10, dùng thư viện `dbup-sqlserver`) tự động đưa schema/SP
trên `RPOSMasterData` (CentralMD) — và các DB `RPOSCentralSales` theo từng store (shard) — về đúng
trạng thái mà `docs/sql/manifest.json` mô tả. Thay cho việc DBA phải nhớ chạy tay 47 file `.sql`
mỗi lần deploy.

Manifest phân mọi script trong `docs/sql/*.sql` thành **2 track**, xử lý khác nhau hoàn toàn:

| Track | `runOnce` trong manifest | Đặc điểm | Tool có tự chạy? |
|---|---|---|---|
| **A** | `false` | Idempotent — `DROP+CREATE`/`CREATE OR ALTER PROCEDURE` (SP/View/Function/TVP) | **Có** — chạy lại **toàn bộ** danh sách mỗi lần `--apply` (dùng `NullJournal` của DbUp — không có khái niệm "đã chạy rồi") |
| **B** | `true` | DDL một-lần rủi ro cao — rebuild bảng, đảo dữ liệu, `sp_rename` | **KHÔNG BAO GIỜ** — tool chỉ đọc bảng `dbo.SchemaVersions` để báo thiếu; DBA chạy tay rồi tự `INSERT` xác nhận |

### 1.2. CLI arguments

Đọc trực tiếp từ `Program.cs` (đúng với build hiện tại):

| Argument | Bắt buộc đi kèm | Cần kết nối DB? | Tác dụng |
|---|---|---|---|
| `--whatif` | — | Không | In toàn bộ Track A sẽ chạy (theo `order`) + Track B phải chạy tay + kết quả content-guard. Dùng để xem trước, an toàn tuyệt đối. |
| `--verify` | `--config <path>` | Có | Đọc `dbo.SchemaVersions` trên DB đích, so với Track B trong manifest → báo file nào **chưa** có mặt. Read-only, không ghi gì lên DB (ngoại trừ tự tạo bảng `SchemaVersions` nếu chưa có). |
| `--apply` | `--config <path>` | Có | Thực thi Track A thật (ghi DB). Luôn quét content-guard trước — phát hiện DDL/DML nguy hiểm thì **dừng ngay, không chạy gì**. |
| `--normalize-alter-proc` | — (tùy chọn `--dry-run`) | Không | Chuẩn hóa `ALTER PROC` bare → `CREATE OR ALTER PROCEDURE` trong 8 file cố định (xem `NormalizeAlterProc.TargetFiles`). Việc bảo trì repo, không phải bước deploy thường xuyên. |
| `--sql-dir <path>` | đi kèm bất kỳ lệnh trên | — | Trỏ thẳng tới thư mục chứa `manifest.json` + toàn bộ `*.sql`. **BẮT BUỘC** khi chạy binary đã publish/deploy ngoài git checkout (Docker, Ubuntu bare-metal) — không có `POS.slnx` nào để tool tự dò. Bỏ qua thì tool tự dò ngược từ thư mục chạy tìm `POS.slnx` (chỉ dùng được khi `dotnet run`/chạy ngay trong repo lúc dev). |
| `--config <path>` | — | — | File JSON kiểu `appsettings.json`, chỉ cần mục `ConnectionStrings`. Có thể là chính `appsettings.Production.json` của POS.Api/POS.Web. |

Không truyền lệnh nào hợp lệ → in hướng dẫn sử dụng, exit code `1`.

**Đã verify thật** (chạy trực tiếp binary, không suy đoán):

```
$ dotnet POS.DbMigrator.dll --whatif --sql-dir docs/sql
=== Target: CentralMD ===
-- Track A (tu dong chay lai TOAN BO moi lan --apply, 40 file) --
  [   90] SetupPromotion_AddNumOfDaysList.sql  // PHAI chay truoc order 100 ...
  ...
-- Track B (PHAI chay tay, 6 file) --
  [  600] CpnVchBOMCodeIssue_ExtendSchema.sql  (phase=pre-deploy)  // ...
  ...
-- Content-guard (quét Track A tìm DDL/DML nguy hiểm chưa guard) --
  Không phát hiện gì — mọi Track A đều an toàn.
```

### 1.3. Luồng hoạt động mỗi lần deploy có đổi SQL

```
1. --verify --config <appsettings> [--sql-dir <path>]
   → đọc Track B còn thiếu trong SchemaVersions (đúng phase pre-deploy/post-deploy).
2. Track B thiếu & THẬT SỰ chưa từng chạy → chạy tay theo docs/ROLLOUT.md §D6/D10/O1/O1b
   (backup, cửa sổ bảo trì) → tự INSERT xác nhận vào SchemaVersions (xem §2.4).
3. --whatif [--sql-dir <path>]
   → xem trước Track A sẽ chạy (không cần DB).
4. --apply --config <appsettings> [--sql-dir <path>]
   → chạy Track A thật. Exit code ≠ 0 → DỪNG pipeline deploy, đừng start container app mới.
```

Bước 3–4 chạy **trước** khi `docker run`/start `POS.Api`/`POS.Web` (xem `docs/guide-deploy.md` §2.5).

### 1.4. Exit code

| Exit code | Ý nghĩa |
|---|---|
| `0` | Thành công / không phát hiện vấn đề gì. |
| `1` | Có Track B thiếu (`--verify`), có DDL nguy hiểm bị content-guard chặn, hoặc apply thất bại (kể cả partial-failure trên shard). |
| Khác (vd `82`, `255`, `-532462766`...) | **Exception .NET chưa bắt** (connection string sai, DB không kết nối được, thiếu `--sql-dir`...) — xem §5 Troubleshooting. Không dùng exit code này để phân loại lỗi cụ thể, chỉ cần biết ≠ 0 là có sự cố. |

---

## 2. Cấu hình môi trường

### 2.1. `--config <path>` — file connection string

Không cần file `.json` riêng cho migrator — **dùng thẳng** `appsettings.{UAT|Production}.json` sẵn
có của `POS.Api`/`POS.Web` (chúng có đúng section `ConnectionStrings` cần). Migrator chỉ đọc mục
này, không đụng phần còn lại của file.

Key cần có trong `ConnectionStrings`, tùy target nào có script (xem `manifest.json`):

| Key | Dùng cho | Bắt buộc khi |
|---|---|---|
| `CentralMD` | Track A + Track B trên `RPOSMasterData` | Luôn (đây là target chính, 40/41 script Track A) |
| `CentralGeneral` | Dò danh sách shard qua `RPOSCentralGeneral.dbo.StoreSetServer` | Có script `target: "CentralSaleShards"` trong manifest (hiện tại: `BusinessDay_ConfirmEndDate.sql`) |
| `CentralSaleTemplate` | Build connection string từng shard (thay `{server}`) | Cùng điều kiện trên |

Ví dụ tối thiểu (không cần các key khác của appsettings.json như `Loyalty`/`StagingDB`/Redis...):

```json
{
  "ConnectionStrings": {
    "CentralMD": "Data Source=<host>;Initial Catalog=RPOSMasterData;User ID=<user>;Password=<pass>;TrustServerCertificate=True",
    "CentralGeneral": "Data Source=<host>;Initial Catalog=RPOSCentralGeneral;User ID=<user>;Password=<pass>;TrustServerCertificate=True",
    "CentralSaleTemplate": "Data Source={server};Initial Catalog=RPOSCentralSales;User ID=<user>;Password=<pass>;TrustServerCertificate=True"
  }
}
```

### 2.2. `--sql-dir <path>` — thư mục script

Thư mục phải chứa **đúng** `manifest.json` + toàn bộ `*.sql` mà nó tham chiếu (đơn giản nhất: chính
`docs/sql/` của repo, hoặc bản copy y nguyên baked vào image Docker — xem §4).

Không truyền → tool tự dò ngược từ thư mục chạy binary tìm file `POS.slnx`, rồi suy ra
`{đó}/docs/sql`. Cơ chế này **chỉ hoạt động khi chạy trong git checkout** (dev machine, CI chạy
`dotnet run` ngay trong repo) — **verify thật**: chạy binary đã publish ra 1 thư mục ngoài repo mà
không truyền `--sql-dir` sẽ crash:

```
Unhandled exception. System.InvalidOperationException: Không tìm thấy POS.slnx đi ngược từ
'C:\deploy\pos-dbmigrator\', và không có --sql-dir <path>. Khi chạy binary đã publish/deploy ngoài
git checkout (Docker, Ubuntu bare-metal) BẮT BUỘC truyền --sql-dir trỏ tới thư mục chứa
manifest.json + toàn bộ *.sql.
```

→ **luôn truyền `--sql-dir`** khi deploy thật (Ubuntu bare-metal, Docker). Chỉ bỏ qua khi chạy
`dotnet run`/debug ngay trong repo lúc phát triển.

### 2.3. `POS_SECRET_KEY` — chỉ cần khi config đã mã hóa

Migrator tái dùng đúng cơ chế giải mã `enc:...` dùng chung với `POS.Api`/`POS.Web`
(`POS.Infrastructure.Security.ConfigurationSecretExtensions.DecryptEncryptedSecrets()` — gọi ngay
sau khi load file `--config`).

- File `--config` còn **plaintext** (password thật, không có chuỗi `enc:...`) → **không cần** biến
  này, hook tự nhận biết không có gì để giải mã (no-op).
- File `--config` có `Password=enc:...` → **bắt buộc** set env `POS_SECRET_KEY` (khóa AES-256
  base64, 32 byte — cùng khóa dùng cho POS.Api/POS.Web, sinh tại `/admin/encrypt-secret` của
  POS.Web). Thiếu khóa → fail-fast ngay khi load config, thông báo rõ ràng, không chạy gì.

Chi tiết cơ chế: `docs/architecture/appsetting.md`.

### 2.4. Quyền SQL Server cần có

Tài khoản trong connection string `CentralMD` cần quyền: `CREATE PROCEDURE`, `ALTER PROCEDURE` (hoặc
tương đương `ddl_admin`/`db_owner`) trên `RPOSMasterData` — Track A liên tục `DROP`/`CREATE`/`ALTER`
object. Tài khoản trong `CentralGeneral` chỉ cần `SELECT` trên
`RPOSCentralGeneral.dbo.StoreSetServer`.

**Chưa verify được**: quyền thật của tài khoản `IFSAP` (đang dùng trong `appsettings.Production.json`
hiện tại của repo) trên production — cần DBA xác nhận trước lần `--apply` đầu tiên.

**Track B — ghi nhận đã chạy tay** (đọc bởi `--verify`, bảng do migrator tự tạo nếu chưa có, cùng
schema chuẩn `SqlTableJournal` của DbUp — đã verify qua reflection, không đoán):

```sql
-- Bảng dbo.SchemaVersions (Id int identity PK, ScriptName nvarchar(255), Applied datetime)
INSERT INTO dbo.SchemaVersions (ScriptName, Applied) VALUES ('TenFileScript.sql', GETDATE());
```

---

## 3. Hướng dẫn chạy trực tiếp trên Ubuntu (Bare-metal)

### 3.1. Prerequisites

- Máy **build** (CI hoặc máy dev): .NET 10 **SDK** — để `dotnet publish`.
- Máy **chạy** (Ubuntu production/UAT): **ASP.NET Core Runtime 10.0** (không phải chỉ .NET Runtime
  thuần) — do `POS.Infrastructure` (dependency của migrator) có `<FrameworkReference
  Include="Microsoft.AspNetCore.App" />` (dùng `IHttpClientFactory`). Thiếu ASP.NET Core Runtime sẽ
  lỗi ngay lúc khởi động kiểu "It was not possible to find any compatible framework version".
  Cài theo hướng dẫn chính thức Microsoft cho Ubuntu (`dotnet-install.sh` hoặc gói `aspnetcore-
  runtime-10.0` nếu repo apt của bản Ubuntu đang dùng đã có — kiểm tra tên gói thật trên máy đích
  trước, tên gói theo phiên bản .NET có thể khác giữa các bản Ubuntu).

### 3.2. Publish (verify thật trên máy build)

Framework-dependent (khuyến nghị — image/artifact nhỏ hơn, dùng chung runtime đã cài trên máy đích):

```bash
dotnet publish tools/POS.DbMigrator/POS.DbMigrator.csproj \
  -c Release -o ./publish/pos-dbmigrator \
  --self-contained false -r linux-x64
```

**Đã chạy thật lệnh này** (trên máy build Windows, target `linux-x64`) — publish thành công, sinh
`POS.DbMigrator.dll` + apphost `POS.DbMigrator` + toàn bộ dependency (~52 MB, bao gồm cả
Confluent.Kafka/RabbitMQ.Client/StackExchange.Redis — kế thừa transitive từ `POS.Infrastructure`,
migrator chỉ dùng 2 class nhỏ của project đó nhưng không tách được dependency nhẹ hơn ở bản hiện
tại).

Self-contained (không cần cài runtime trên máy đích, đổi lại artifact nặng hơn — **chưa verify
thật** lệnh này, chỉ là cú pháp `dotnet` chuẩn):

```bash
dotnet publish tools/POS.DbMigrator/POS.DbMigrator.csproj \
  -c Release -o ./publish/pos-dbmigrator \
  --self-contained true -r linux-x64 -p:PublishTrimmed=false
```

Copy `./publish/pos-dbmigrator/` (toàn bộ thư mục) và `docs/sql/` sang máy Ubuntu, ví dụ
`/opt/pos-dbmigrator/app/` và `/opt/pos-dbmigrator/sql/`.

### 3.3. Chạy (cú pháp + ví dụ tham số thật)

```bash
cd /opt/pos-dbmigrator/app

# Xem trước — an toàn, không cần config/DB
dotnet POS.DbMigrator.dll --whatif --sql-dir /opt/pos-dbmigrator/sql

# Kiểm tra Track B còn thiếu trên Production
dotnet POS.DbMigrator.dll --verify \
  --config /opt/pos/appsettings.Production.json \
  --sql-dir /opt/pos-dbmigrator/sql

# Nếu config có token enc:... thì set khóa trước (cùng POS_SECRET_KEY dùng cho POS.Api/POS.Web)
export POS_SECRET_KEY="<khóa AES base64>"

# Apply thật — BẮT BUỘC chạy trước khi start/restart POS.Api, POS.Web
dotnet POS.DbMigrator.dll --apply \
  --config /opt/pos/appsettings.Production.json \
  --sql-dir /opt/pos-dbmigrator/sql
echo "Exit code: $?"   # phải = 0, khác 0 thì DỪNG, không deploy tiếp
```

> Dùng file publish là apphost (`./POS.DbMigrator --whatif ...` sau khi `chmod +x`) hay qua
> `dotnet POS.DbMigrator.dll` đều được — 2 cách tương đương với publish framework-dependent.

---

## 4. Triển khai qua Docker

> **Chưa verify bằng build/run container thật** — Docker daemon không chạy được trong sandbox lúc
> soạn tài liệu này (`docker info` báo "failed to connect to the docker API... daemon is running?").
> Dockerfile dưới đây viết theo đúng pattern đã verify của `Dockerfile.worker`/`Dockerfile` hiện có
> trong repo (multi-stage, base image, lý do cần `aspnet` runtime) — **bạn cần tự `docker build`
> một lần để xác nhận trước khi đưa vào CI/CD**.

### 4.1. Dockerfile (`tools/POS.DbMigrator/Dockerfile`)

```dockerfile
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/POS.Common/POS.Common.csproj                  src/POS.Common/
COPY src/POS.Infrastructure/POS.Infrastructure.csproj   src/POS.Infrastructure/
COPY tools/POS.DbMigrator/POS.DbMigrator.csproj         tools/POS.DbMigrator/
RUN dotnet restore tools/POS.DbMigrator/POS.DbMigrator.csproj
COPY src/POS.Common/       src/POS.Common/
COPY src/POS.Infrastructure/ src/POS.Infrastructure/
COPY tools/POS.DbMigrator/ tools/POS.DbMigrator/
RUN dotnet publish tools/POS.DbMigrator/POS.DbMigrator.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime — PHẢI dùng aspnet (không phải dotnet/runtime thuần): POS.Infrastructure có
# FrameworkReference Microsoft.AspNetCore.App (IHttpClientFactory) — giống lý do Dockerfile.worker.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV TZ=Asia/Ho_Chi_Minh

COPY --from=build /app/publish .

# Bake đúng bộ script SQL tại thời điểm build image — image = artifact bất biến, tránh lệch
# version script giữa các lần deploy/rollback. Muốn override 1 script để test nhanh không rebuild
# thì dùng --entrypoint như ví dụ ở §4.3.
COPY docs/sql/ /app/sql/

USER $APP_UID
ENTRYPOINT ["dotnet", "POS.DbMigrator.dll", "--sql-dir", "/app/sql"]
CMD ["--whatif"]
```

> Build context phải là **root repo** (không phải `tools/POS.DbMigrator/`) vì cần copy cả
> `src/POS.Common`, `src/POS.Infrastructure`, `docs/sql/` — giống cách `Dockerfile.worker` build
> từ root hiện tại.

### 4.2. Build

```bash
docker build -t pos-dbmigrator:latest -f tools/POS.DbMigrator/Dockerfile .
```

### 4.3. Run

```bash
# Xem trước — không cần mount gì, dùng CMD mặc định --whatif
docker run --rm pos-dbmigrator:latest

# Verify Track B trên Production — mount config (chứa connection string) read-only
docker run --rm \
  -e POS_SECRET_KEY="${POS_SECRET_KEY}" \
  -v /srv/pos/appsettings.Production.json:/app/config/appsettings.Production.json:ro \
  pos-dbmigrator:latest \
  --verify --config /app/config/appsettings.Production.json

# Apply thật — TRƯỚC khi docker run container POS.Api/POS.Web mới (xem docs/guide-deploy.md §2.5)
docker run --rm \
  -e POS_SECRET_KEY="${POS_SECRET_KEY}" \
  -v /srv/pos/appsettings.Production.json:/app/config/appsettings.Production.json:ro \
  pos-dbmigrator:latest \
  --apply --config /app/config/appsettings.Production.json
echo "Exit code: $?"   # kiểm tra trong script CI/CD, ≠ 0 thì dừng pipeline
```

> `-e POS_SECRET_KEY=...`: bỏ qua nếu `appsettings.Production.json` còn plaintext (xem §2.3).
> Không cần mount `docs/sql/` — đã bake sẵn trong image lúc build (§4.1). Chỉ mount đè khi cần test
> 1 bản script sửa tay chưa rebuild image:
> ```bash
> docker run --rm --entrypoint dotnet \
>   -v /host/docs/sql:/override/sql:ro \
>   pos-dbmigrator:latest \
>   POS.DbMigrator.dll --sql-dir /override/sql --whatif
> ```

---

## 5. Xử lý sự cố (Troubleshooting)

Migrator **không viết file log** — mọi output ra `stdout`/`stderr` (Console). Đọc log bằng:
- Bare-metal: output thẳng ra terminal chạy lệnh; muốn lưu lại thì tự redirect
  (`dotnet POS.DbMigrator.dll --apply ... 2>&1 | tee /var/log/pos-dbmigrator/$(date +%F).log`).
- Docker: `docker logs <container-id>` (container tự exit sau khi chạy xong — không phải service
  chạy mãi, nên xem log ngay sau khi `docker run` hoặc dùng `docker run` không kèm `-d` để thấy log
  trực tiếp trên terminal).
- CI/CD: log nằm trong console output của job/step — đọc trong dashboard CI (GitHub Actions/
  Jenkins/GitLab...).

### 5.1. `Không tìm thấy POS.slnx đi ngược từ '...' và không có --sql-dir <path>`

**Nguyên nhân**: chạy binary đã publish/deploy ở vị trí ngoài git checkout (không có file
`POS.slnx` trong bất kỳ thư mục cha nào) mà quên truyền `--sql-dir`. **Verify thật** — tái hiện
được 100% bằng cách publish ra 1 thư mục ngoài repo rồi chạy `--whatif` không kèm `--sql-dir`.

**Khắc phục**: luôn thêm `--sql-dir <path-tới-thư-mục-chứa-manifest.json>` khi chạy ở Ubuntu
bare-metal hoặc Docker (xem §2.2, §3.3, §4.3).

### 5.2. `Thiếu ConnectionStrings:CentralMD trong file config.`

**Nguyên nhân**: file truyền qua `--config` không có mục `ConnectionStrings:CentralMD` (thiếu key,
sai tên key, hoặc trỏ nhầm file). **Verify thật** — tái hiện bằng file config chỉ có
`ConnectionStrings:Loyalty`, thiếu `CentralMD`.

**Khắc phục**: mở file `--config`, xác nhận đúng cấu trúc ở §2.1. Nếu dùng chung
`appsettings.Production.json` của POS.Api — key `CentralMD` phải tồn tại y nguyên tên (case-
sensitive theo `IConfiguration`, thường không phân biệt hoa/thường nhưng giữ đúng chính tả để
tránh nhầm).

### 5.3. `A network-related or instance-specific error occurred while establishing a connection to SQL Server... (error: 40 - Could not open a connection to SQL Server)`

**Nguyên nhân**: connection string đúng cú pháp nhưng SQL Server không tới được — sai host/port, SQL
Server chưa bật TCP/IP, firewall chặn, hoặc chạy migrator từ máy không có route mạng tới DB. **Verify
thật** — tái hiện bằng connection string trỏ tới host không tồn tại.

**Khắc phục**:
1. `ping`/`telnet <host> 1433` (hoặc named instance port tương ứng) từ đúng máy/container chạy
   migrator — không phải từ máy dev.
2. Trong Docker: đảm bảo container migrator có network route tới DB (không chạy `--network none`,
   kiểm tra DNS/host DB có resolve được từ trong container không).
3. Xác nhận lại `TrustServerCertificate=True` nếu SQL Server dùng self-signed cert (đã có sẵn trong
   connection string mẫu của repo).

> ⚠️ **Lỗi dễ nhầm lẫn nếu tự thêm `<InvariantGlobalization>true</InvariantGlobalization>`** vào
> `POS.DbMigrator.csproj`: `Microsoft.Data.SqlClient` sẽ throw
> `System.NotSupportedException: Globalization Invariant Mode is not supported` **ngay khi mở
> connection**, che mất lỗi kết nối thật — **verify thật, đã xảy ra và đã sửa** trong quá trình viết
> tài liệu này (đã bỏ property đó khỏi `.csproj`, có comment giải thích tại chỗ). Nếu thấy lỗi này
> trong log, kiểm tra `.csproj` đã có `InvariantGlobalization` bị thêm lại chưa.

### 5.4. `[label] CẢNH BÁO — cần chạy tay các file sau và update vào SchemaVersions: ...` (từ `--verify`)

**Không phải lỗi** — đây là hoạt động đúng thiết kế: liệt kê script **Track B** (rủi ro cao) mà
migrator **cố ý không tự chạy**. Migrator vẫn tiếp tục các lệnh khác bình thường (`--apply` vẫn chạy
Track A, không bị chặn bởi cảnh báo này).

**Xử lý**: đối chiếu `phase` (`pre-deploy`/`post-deploy`) + `note` in kèm với đúng mục tương ứng
trong `docs/ROLLOUT.md` (§D6/§D6.1/§D10/§O1/§O1b) — DBA chạy tay script đó (backup + cửa sổ bảo trì
theo hướng dẫn), rồi:
```sql
INSERT INTO dbo.SchemaVersions (ScriptName, Applied) VALUES ('TenFile.sql', GETDATE());
```
Chạy lại `--verify` để xác nhận cảnh báo đã hết cho file đó.

### 5.5. `[CONTENT-GUARD] Phát hiện DDL/DML nguy hiểm trong Track A — DỪNG, không chạy gì`

**Nguyên nhân**: 1 script gắn `runOnce: false` (Track A) trong `manifest.json` nhưng nội dung chứa
pattern nguy hiểm không được guard (`CREATE TABLE`/`DROP TABLE`/`ALTER TABLE...ADD` bare/`CREATE
INDEX` bare/`sp_rename`/`DELETE`,`UPDATE` không WHERE ở top-level) — thường do đăng ký nhầm track
cho script mới, hoặc sửa nội dung 1 script Track A cũ thêm DDL nguy hiểm mà quên đổi `runOnce`.

**Khắc phục**: đổi `runOnce: true` + thêm `phase` phù hợp cho entry đó trong `manifest.json`, hoặc
nếu pattern bị báo nhầm (false-positive hợp lệ) — xem lại `DangerousStatementGuard.cs` để hiểu chính
xác vì sao bị flag trước khi quyết định. `dotnet test tests/POS.ContractTests` sẽ bắt được lỗi này
sớm hơn (lúc code review) nếu script được thêm đúng quy trình ở `.claude/skills/database/SKILLS.md`.

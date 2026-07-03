# Hướng dẫn Ubuntu host: thư mục dùng chung `ftpbluepos` (POS.Api ↔ POS.Worker)

> Dành cho người vận hành hạ tầng (DevOps/Ops) triển khai/duy trì **SIT** (`docker-compose.yml`) và
> **UAT/PROD** (`docker run` theo `docs/guide-deploy.md`) trên Ubuntu. Tài liệu này chỉ nói về thư mục
> chia sẻ file `ftpbluepos` — cấu hình DB/Redis/RabbitMQ/nginx/`POS_SECRET_KEY` xem `docs/guide-deploy.md`
> và `docs/ROLLOUT.md`.

## 1. Vì sao cần việc này

POS.Api sinh/đọc mọi file trao đổi với 5.000 máy POS (master-data zip, sale upload, log upload) tại
`AppSettings:FtpRootPath` = `/app/ftpbluepos` (đường dẫn **bên trong container**). POS.Worker
(`PosFileImportWorker`) cũng cần đọc file trong đúng cây thư mục này (`FileImport:InboxFolder` v.v.).

Trước đây thư mục này là **named volume** (`ftpbluepos_data`, chỉ POS.Api) hoặc **không mount gì cả**
(POS.Worker, cả trong `docker-compose.yml` lẫn `docker run` ở `docs/guide-deploy.md`) — nghĩa là:
- File chỉ tồn tại trong writable layer của container → **mất khi container bị tạo lại**.
- Không cách nào để người vận hành `ls`/`scp`/backup file trực tiếp từ Ubuntu shell.
- Không cách nào để một hệ thống khác "thả" file vào cho POS.Worker xử lý.

Giải pháp: **bind mount** một thư mục thật trên Ubuntu vào `/app/ftpbluepos` ở **cả 2 container**
(`webapp` = POS.Api, `worker` = POS.Worker) — thao tác 1 lần trên host, không đổi code/logic ứng dụng.

## 2. Tổng quan — thư mục nào, mount vào đâu

| Host (Ubuntu) | Container path (cả webapp lẫn worker) | Dùng cho |
|---|---|---|
| `/srv/pos/ftpbluepos` | `/app/ftpbluepos` | **SIT** (`docker-compose.yml`) và **PROD** (`docker run`, `docs/guide-deploy.md`) |
| `/srv/pos/uat/ftpbluepos` | `/app/ftpbluepos` | **UAT** (`docker run`, `docs/guide-deploy.md`) — path riêng vì UAT chạy **chung host** với PROD |

> ⚠️ SIT và PROD dùng **cùng path** `/srv/pos/ftpbluepos` — an toàn vì 2 môi trường này chạy trên
> **2 host Ubuntu khác nhau**, không bao giờ đụng nhau. UAT thì khác: theo `docs/guide-deploy.md`, UAT
> và PROD chạy `docker run` trên **cùng một máy** (chỉ khác port/tên container) → nếu UAT cũng mount
> `/srv/pos/ftpbluepos`, dữ liệu test sẽ lẫn vào master-data/sale file PROD thật. Vì vậy UAT bắt buộc
> dùng `/srv/pos/uat/ftpbluepos`.

Cây thư mục bên trong (giống nhau cho cả PROD và UAT, chỉ khác gốc):

```
ftpbluepos/                       (= /app/ftpbluepos trong container)
├── SyncDataPos/
│   ├── Sale/
│   │   ├── Kafka/                ← POS.Api (UploadFileSale) ghi file sale upload vào đây
│   │   │                            PosFileImportWorker CŨNG quét thư mục này (xem mục 6 — rủi ro)
│   │   ├── BackupFiles/          ← file sale đã xử lý Kafka thành công được move vào đây
│   │   ├── error/                ← PosFileImportWorker move zip xử lý lỗi vào đây (KHÔNG tự xóa)
│   │   └── _work/                ← thư mục tạm PosFileImportWorker giải nén (tự dọn sau mỗi file)
│   ├── General/LogJob/{folder}/  ← log POS upload lên
│   └── POS/ALL/{site}/{terminal}/← file master-data .zip (+ .sha256) POS tải về hằng ngày
└── BluePosUpgrade/                ← công cụ upgrade POS (hiện chưa dùng ở UAT/PROD)
```

Toàn bộ cây này **tự sinh** khi POS.Api/POS.Worker chạy (code tự `Directory.CreateDirectory`) — bước
thiết lập ở mục 4 chỉ tạo sẵn khung + cấp đúng quyền, không bắt buộc phải có trước mới chạy được.

## 3. UID/GID container — vì sao quan trọng

Container `webapp` và `worker` đều chạy **non-root**, user `app` với **UID:GID cố định là `1654:1654`**
(base image `mcr.microsoft.com/dotnet/aspnet:10.0`, đã xác nhận thực tế trong `docker-compose.yml`).
Thư mục host bind-mount vào phải cho phép UID này ghi được — nếu không, container sẽ lỗi quyền ngay khi
cố ghi file (vd `GetFileFromFTP` sinh zip đầu tiên sẽ throw exception ghi log rõ).

## 4. Thiết lập lần đầu (chạy 1 lần trên mỗi Ubuntu host)

### 4.1. Kiểm tra môi trường (1 lần/host, trước khi tin tưởng bind mount)

```bash
# Docker cài qua apt/Docker-CE, KHÔNG phải snap (snap từng chặn bind-mount ngoài $HOME):
dpkg -l | grep docker-ce || echo "CẢNH BÁO: không thấy docker-ce qua apt — kiểm tra lại cách cài Docker"

# Không bật userns-remap (mặc định Docker KHÔNG bật) — nếu có, UID 1654 trong container sẽ bị dịch
# sang UID khác trên host, và mọi lệnh chown 1654 bên dưới phải đổi theo:
docker info --format '{{json .SecurityOptions}}' | grep -o 'name=userns' && \
  echo "CẢNH BÁO: userns-remap đang bật — xem /etc/subuid trước khi tiếp tục" || \
  echo "OK: không có userns-remap"
```

### 4.2. Tạo thư mục + cấp quyền — dùng script có sẵn trong repo

Script `deploy/linux/setup-pos-dirs.sh` (đã commit trong repo) tự động: tạo group `posops` đúng
GID container (1654), tạo cây thư mục, `chown`/`chmod` đúng quyền. **An toàn chạy lại nhiều lần**
(idempotent).

```bash
cd /đường-dẫn-tới-repo-code   # thư mục chứa docker-compose.yml

# PROD (và SIT — dùng chung lệnh này vì không bao giờ chung host):
sudo ./deploy/linux/setup-pos-dirs.sh

# UAT (CHỈ cần nếu deploy UAT theo docs/guide-deploy.md trên cùng host với PROD):
sudo ./deploy/linux/setup-pos-dirs.sh /srv/pos/uat
```

Script làm gì (để tham khảo nếu muốn tự làm tay thay vì chạy script):
```bash
# 1. Group posops đúng GID 1654 (khớp GID container "app")
sudo groupadd -g 1654 posops

# 2. Tạo cây thư mục
sudo mkdir -p /srv/pos/ftpbluepos/SyncDataPos/Sale/{Kafka,BackupFiles,error,_work}
sudo mkdir -p /srv/pos/ftpbluepos/BluePosUpgrade

# 3. Ownership + setgid (2770 = setgid + rwxrwx--- : container + group posops đọc/ghi/xóa,
#    người khác trên host không có quyền — đây là dữ liệu sale/master-data thật)
sudo chown -R 1654:1654 /srv/pos/ftpbluepos
sudo find /srv/pos/ftpbluepos -type d -exec chmod 2770 {} \;
```

> **Vì sao setgid (`2770`) quan trọng**: khi người vận hành tự tay `scp`/tạo file trực tiếp trong thư
> mục này (không qua container), setgid đảm bảo file mới đó tự động mang group `posops` — nếu không,
> container (GID 1654) có thể không đọc/xóa được file do người dùng tự tạo. Setgid trên thư mục cha còn
> tự động lan xuống mọi thư mục con được tạo thêm sau này (kể cả thư mục app tự sinh lúc chạy).

### 4.3. Cấp quyền cho người vận hành (không cần sudo)

```bash
sudo usermod -aG posops <tên-user-vận-hành>
```
User đó cần **đăng nhập lại** (hoặc chạy `newgrp posops` trong phiên hiện tại) để quyền có hiệu lực.
Sau đó có thể `ls`/`cat`/`rm`/`scp` trực tiếp trong `/srv/pos/ftpbluepos/` mà không cần `sudo`.

### 4.4. Áp dụng cấu hình container

Cấu hình bind mount **đã được đưa vào code sẵn** (không cần người vận hành tự gõ path) — chỉ cần deploy
lại theo quy trình hiện có:

- **SIT** (`docker-compose.yml`): `webapp` và `worker` đã khai báo sẵn
  `/srv/pos/ftpbluepos:/app/ftpbluepos` — chỉ cần:
  ```bash
  sudo docker compose up -d --build
  ```
- **UAT/PROD** (`docs/guide-deploy.md`, `docker run` riêng lẻ): đã có sẵn `-v` cho `ftpbluepos` trong ví
  dụ lệnh `docker run` ở §3.1 (POS.Api) và §3.3 (POS.Worker) — **PROD dùng `/srv/pos/ftpbluepos`, UAT
  dùng `/srv/pos/uat/ftpbluepos`** (đọc kỹ ghi chú ⚠️ ngay dưới mỗi lệnh mẫu trong file đó trước khi chạy).

### 4.5. Di trú dữ liệu cũ — CHỈ áp dụng cho SIT (named volume → bind mount)

Chỉ môi trường SIT đang có dữ liệu thật trong named volume `ftpbluepos_data` cần bước này (UAT/PROD
theo `docs/guide-deploy.md` trước giờ không mount gì, không có dữ liệu cũ để mất).

```bash
# 0. Dừng 2 container đang dùng volume cũ
sudo docker compose stop webapp worker

# 1. Tạo + cấp quyền thư mục mới TRƯỚC khi copy (mục 4.2)
sudo ./deploy/linux/setup-pos-dirs.sh

# 2. Xác nhận đúng tên volume thật (có tiền tố tên thư mục project, không hardcode đoán)
SRC_VOLUME=$(docker volume ls --format '{{.Name}}' --filter name=ftpbluepos_data)
echo "Volume nguồn: $SRC_VOLUME"     # PHẢI ra đúng 1 dòng — dừng lại nếu ra 0 hoặc >1 dòng

# 3. Copy dữ liệu qua container tạm (cp -a giữ nguyên owner/mode — vốn đã là 1654:1654)
sudo docker run --rm \
  -v "${SRC_VOLUME}":/from:ro \
  -v /srv/pos/ftpbluepos:/to \
  alpine:3 sh -c 'cp -a /from/. /to/ && echo "Đã copy $(find /from -type f | wc -l) file"'

# 4. Đối chiếu số file
sudo find /srv/pos/ftpbluepos -type f | wc -l

# 5. Khởi động lại với cấu hình mount mới (đã có sẵn trong docker-compose.yml)
sudo docker compose up -d --build

# 6. CHỈ sau khi xác nhận chạy ổn định vài ngày (không phải ngay trong buổi thao tác này):
#      docker volume rm "$SRC_VOLUME"
```

Rủi ro mất dữ liệu ở bước này thấp — file trong `ftpbluepos_data` chủ yếu là master-data zip tự sinh lại
hằng ngày (`KeepZipDays`) và sale file đã có `BackupFiles`, nhưng vẫn nên di trú cho chắc thay vì bỏ qua.

## 5. Kiểm chứng sau khi triển khai

1. **Mount đúng chỗ**:
   ```bash
   docker inspect sit_dotnet_api --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
   docker inspect pos_worker     --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
   ```
   Cả 2 phải thấy `/srv/pos/ftpbluepos -> /app/ftpbluepos` (loại `bind`, không phải volume tên `ftpbluepos_data`).

2. **POS.Api ghi được** — gọi thử (đổi `{site}`/`{terminal}` theo dữ liệu thật):
   ```
   GET /api/posblue/GetFileFromFTP?siteCode={site}&posTerminal={terminal}&typeSync=ALL&...
   ```
   → kiểm tra `.zip` + `.sha256` xuất hiện thật trên **host**:
   ```bash
   ls -la /srv/pos/ftpbluepos/SyncDataPos/POS/ALL/{site}/{terminal}/
   ```

3. **POS.Api tải được** — gọi `GET /api/posblue/DowloadFileStream?...` với file vừa sinh → thành công,
   có dòng mới trong bảng `dbo.MasterDataDownloadLog`.

4. **Luồng cũ không bị ảnh hưởng (regression)** — upload 1 file qua `POST /api/posblue/UploadFileSale`
   như trước giờ vẫn làm → xác nhận vẫn đẩy Kafka bình thường, file biến mất khỏi `Sale/Kafka/` và xuất
   hiện ở `Sale/BackupFiles/` trong thời gian tương đương trước khi có PosFileImportWorker (tức Worker
   không tình cờ giành file trước khi luồng cũ kịp xử lý).

5. **PosFileImportWorker hoạt động** — với quyền `posops` (không sudo), thả trực tiếp 1 file `.zip` hợp
   lệ (chứa `.txt` tên đúng `Type_PosNo_TransactionId.txt`) vào:
   ```bash
   cp mau-hop-le.zip /srv/pos/ftpbluepos/SyncDataPos/Sale/Kafka/
   ```
   Trong vòng 30s (`PollIntervalSeconds`), kiểm tra:
   ```bash
   docker logs --tail 50 pos_worker | grep PosFileImport
   ```
   File phải biến mất (thành công) hoặc xuất hiện trong `SyncDataPos/Sale/error/` (lỗi định dạng).
   Đối chiếu heartbeat: `GET Worker:Heartbeat:PosFileImport` trên Redis (DB 2) phải vừa cập nhật.

6. **Quyền vận hành** — user thuộc `posops` (không sudo) phải `ls`/`touch`/`rm` được trực tiếp trong
   `/srv/pos/ftpbluepos/`.

7. **Guardrail dự án**: `dotnet test tests/POS.ContractTests` vẫn phải xanh (task này chỉ đổi path
   cấu hình + docker, không đổi DTO/DI).

## 6. ⚠️ Rủi ro đã biết — cần theo dõi vận hành

`PosFileImportWorker` quét **đúng thư mục** `SyncDataPos/Sale/Kafka` mà `UploadFileSale`/`RetryProcessSales`
(POS.Api) đã dùng từ trước — đây là quyết định có chủ đích (đơn giản hóa, dùng 1 thư mục chung) chứ
không phải nhầm lẫn. Hệ quả cần biết khi vận hành:

- Trong điều kiện bình thường, `UploadFileSale` xử lý file gần như ngay khi upload (in-process) nên
  hầu như luôn "xử lý xong trước" khi Worker kịp poll (chu kỳ 30s) — Worker hiếm khi chạm vào file mới.
- Worker chủ yếu chỉ thấy file khi nó bị "kẹt" lại do đẩy Kafka thất bại (trường hợp trước đây chỉ chờ
  `RetryProcessSales` gọi tay). Nếu file kẹt đó không đúng định dạng `.txt`/`Type_PosNo_TransactionId.txt`
  mà Worker yêu cầu, Worker sẽ move nó sang `SyncDataPos/Sale/error/` — **thay đổi hành vi vận hành**:
  trước đây file kẹt nằm chờ ở `Sale/Kafka` chờ retry tay, giờ có thể bị chuyển sang `error/` trước đó.

**Việc cần làm định kỳ**: theo dõi `SyncDataPos/Sale/error/` (số lượng file + tuổi file cũ nhất). Mỗi
file ở đây là **1 giao dịch sale thật chưa được xử lý bởi bất kỳ luồng nào** (không phải cache tạm) —
cần đối soát thủ công, **KHÔNG tự động xóa/cron-dọn**. Nên cân nhắc thêm cảnh báo (số file > ngưỡng,
hoặc file cũ hơn N giờ) nếu có hệ thống monitoring — nằm ngoài phạm vi lần triển khai này.

## 7. Dọn dẹp & backup — lưu ý dung lượng đĩa

| Thư mục | Tự dọn? | Khuyến nghị |
|---|---|---|
| `SyncDataPos/POS/ALL/{site}/{terminal}/` | Có — `KeepZipDays` xóa zip cũ, nhưng **chỉ khi có request mới** cho đúng site/terminal đó. Terminal ngừng gọi vĩnh viễn sẽ để lại zip cuối cùng mãi mãi. | Theo dõi dung lượng `/srv/pos/ftpbluepos` định kỳ; cân nhắc quét dọn zip quá cũ (vd > 30 ngày) bằng cron riêng nếu cần. |
| `SyncDataPos/Sale/BackupFiles/` | **Không** thấy cơ chế dọn trong code. | Theo dõi dung lượng; đây là cache (file đã đẩy Kafka thành công), an toàn để dọn bằng tay/cron nếu đầy. |
| `SyncDataPos/Sale/error/` | **Không** — worker không tự retry/xóa. | **KHÔNG tự động xóa** — dữ liệu nghiệp vụ cần đối soát (xem mục 6). Nên backup định kỳ (rsync/tar) vì đây là dữ liệu sale không tái tạo được. |
| `SyncDataPos/Sale/_work/`, `SyncDataPos/Sale/Kafka/` | Tự dọn (xử lý xong là mất/move) | Không cần backup — dữ liệu tạm/đang xử lý. |
| `SyncDataPos/General/LogJob/` | **Không** thấy cơ chế dọn trong code. | Theo dõi dung lượng. |
| `BluePosUpgrade/` | N/A (đang tắt ở UAT/PROD) | Nếu bật sau này: file cài đặt do người vận hành upload tay, nên đưa vào backup định kỳ hiện có. |

## 8. Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Quy trình deploy đầy đủ (build image, `docker run`, nginx, `POS_SECRET_KEY`) | `docs/guide-deploy.md` |
| Checklist go-live tổng hợp (mục O1 sinh master-data, O2 file-import worker) | `docs/ROLLOUT.md` |
| Script thiết lập thư mục | `deploy/linux/setup-pos-dirs.sh` |
| Deploy POS.Worker trên Windows (dev/bare-metal, không liên quan Docker) | `deploy/windows/README.md` |
| Code path-mapping (`MapFtpPath`/`MapSitePath`) | `src/POS.Application/Features/DataSync/SyncDataPosService.cs` |
| Code PosFileImportWorker | `src/POS.Infrastructure/Workers/PosFileImportWorker.cs` |

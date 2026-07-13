# Fix Issue — POS.Web HTTP 500 trên `sit-uat-server`

> Nhật ký thực tế lỗi `HTTP ERROR 500` gặp phải khi vận hành POS.Web trên host `sit-uat-server`
> + cách chẩn đoán/fix. Đọc file này TRƯỚC khi debug lại lỗi 500 tương tự trên host này hoặc host
> mới có cấu hình tương tự — tránh đi lại đúng chuỗi chẩn đoán đã làm. Cùng phong cách với
> `docs/deploy/fix_issue_pos-worker-host.md` (POS.Worker).

## Bối cảnh

2026-07-13: user báo build POS.Web deploy lên "production" bị `HTTP ERROR 500`. Qua điều tra,
server thực tế là `sit-uat-server` (SIT/UAT, không phải Production thật) và **unit systemd cài
thật trên host này khác hẳn template trong repo** — đây là điểm gây nhầm lẫn lớn nhất khi debug,
xem Vấn đề 2 trước khi áp bất kỳ lệnh nào từ `docs/guide-deploy.md`/`deploy/linux/systemd/*.service`.

Có 2 vấn đề **độc lập, không liên quan nhau** chồng lên nhau — sửa xong vấn đề 1 (process sống
lại) không có nghĩa hết lỗi 500 (vấn đề 2 vẫn còn). Luôn test lại sau mỗi bước fix, không dừng ở
log đầu tiên nhìn có vẻ hợp lý.

## Vấn đề 1 — Crash-loop kéo dài do journal corrupt (`status=209/STDOUT`)

**Triệu chứng:**
```
sudo journalctl -u pos-web -n 200 --no-pager
...
(dotnet)[...]: pos-web.service: Failed to set up standard output: No such file or directory
(dotnet)[...]: pos-web.service: Failed at step STDOUT spawning /usr/bin/dotnet: No such file or directory
systemd[1]: pos-web.service: Main process exited, code=exited, status=209/STDOUT
```
Restart counter đã lên tới 121+ (`Restart=always RestartSec=10` trong unit → cứ 10s restart 1
lần, kéo dài từ 07/07 tới 13/07 — 6 ngày). `status=209/STDOUT` là mã lỗi nội bộ systemd
(`EXIT_STDOUT`) — process bị chặn **trước khi** dotnet/POS.Web.dll kịp chạy, không liên quan
`appsettings.json`/`Program.cs`.

**Nguyên nhân:** `systemd-journald` báo journal storage bị hỏng ngay trong log:
```
File /var/log/journal/.../system.journal corrupted or uncleanly shut down, renaming and replacing.
```
Nghi do server từng tắt đột ngột/mất điện. Trong lúc journald tự rename+tạo file journal mới,
service `pos-web` (retry mỗi 10s) liên tục "trúng" đúng lúc hạ tầng log chưa sẵn sàng để systemd
gắn stdout/stderr cho tiến trình → crash-loop kéo dài nhiều ngày thay vì tự phục hồi.

> Lưu ý: unit file THẬT trên host này dùng `StandardOutput=append:/srv/pos/logs/web/app.log` +
> `StandardError=append:/srv/pos/logs/web/app.err` (ghi ra **file**, không qua journal) — xem
> Vấn đề 2. Việc mở file log này lúc journald đang phục hồi rất có thể là cơ chế trực tiếp gây
> lỗi STDOUT, chứ không hẳn do bản thân journal corrupt chặn socket `/run/systemd/journal/stdout`.

**Fix:**
```bash
sudo journalctl --verify                    # xem mức độ hỏng
sudo systemctl restart systemd-journald
sudo systemctl restart pos-web
sudo systemctl status pos-web --no-pager -l # phải "active (running)" ổn định, không auto-restart nữa
```
Nếu vẫn corrupt lặp lại sau restart → nghi lỗi phần cứng/đĩa, kiểm tra thêm
`dmesg | grep -i -E "error|ext4|i/o|sdb"`.

**Kết quả sau fix:** process sống ổn định — nhưng **lỗi 500 vẫn còn** (đây là lúc phát hiện Vấn
đề 3, vấn đề thật sự user gặp phải). Đừng dừng chẩn đoán ở bước này.

## Vấn đề 2 — Unit systemd thật khác hẳn template trong repo (đọc trước khi tra path/port)

`docs/guide-deploy.md` §3.2 và `deploy/linux/systemd/pos-web.service` chỉ là **file mẫu** với
placeholder — trên `sit-uat-server`, unit đã cài thật (`sudo systemctl cat pos-web`) lệch hoàn
toàn:

| | Template repo | Thật trên `sit-uat-server` |
|---|---|---|
| `WorkingDirectory` | `/opt/pos/web` | `/var/www/posWeb/web` |
| `User`/`Group` | `pos-web`/`posops` | `minh_ngbinh`/`posops` |
| `EnvironmentFile` | `/etc/pos/pos-web.env` | `/var/www/posWeb/config/.env` |
| `ASPNETCORE_URLS` | `http://127.0.0.1:5002` | `http://localhost:5001` |
| `StandardOutput`/`StandardError` | mặc định (journal) | `append:/srv/pos/logs/web/app.log` / `.../app.err` |

**Hệ quả khi debug:** mọi lệnh `ls /opt/pos/web`, mọi giả định "cổng 5002 = pos-web" từ docs sẽ
**sai** trên host này. `curl http://127.0.0.1:5002/` từng đánh lừa chẩn đoán ban đầu — cổng đó
thực ra là 1 **process orphan cũ** (xem Vấn đề 4), không phải instance `pos-web` thật.

**Luôn chạy trước khi debug bất kỳ path/port nào:**
```bash
sudo systemctl cat pos-web
sudo systemctl show pos-web --property=ExecStart,WorkingDirectory
sudo ss -tlnp | grep -E ':5001|:5002'
```

## Vấn đề 3 — HTTP 500 thật trên `/login`: DataProtection không ghi được key

**Triệu chứng:** sau khi Vấn đề 1 đã fix (process sống ổn định), test route:
```bash
curl -s -o /dev/null -w 'health=%{http_code}\n' http://127.0.0.1:5001/health   # 200 — OK
curl -s -o /dev/null -w 'root=%{http_code}\n'   http://127.0.0.1:5001/         # 302 — OK (redirect /login)
curl -s -o /dev/null -w 'login=%{http_code}\n'  http://127.0.0.1:5001/login    # 500 — LỖI THẬT
```
`/` không lỗi vì `[Authorize]` redirect (302) trước khi kịp render component nào; `/login` mới
thật sự render Razor Component interactive → lộ lỗi.

Log thật (`sudo tail -150 /srv/pos/logs/web/app.log` ngay sau khi gọi `curl .../login`):
```
System.UnauthorizedAccessException: Access to the path '/var/lib/pos-web' is denied.
 ---> System.IO.IOException: Permission denied
   at ... FileSystemXmlRepository.GetAllElementsCore()
   at ... KeyRingProvider.CreateCacheableKeyRingCore(...)
System.Security.Cryptography.CryptographicException: An error occurred while trying to encrypt the provided data.
   at ... KeyRingBasedDataProtector.Protect(Byte[] plaintext)
   at ... ServerComponentSerializer.CreateSerializedServerComponent(...)
```

**Nguyên nhân:** `src/POS.Web/Program.cs:118-134` mặc định persist Data Protection Keys vào
`/var/lib/pos-web/dataprotection-keys` trên Linux (không set `DataProtection:KeyPath` trong
appsettings). Trang dùng `AddInteractiveServerRenderMode()` (như `/login`) cần `Protect()` state
component ngay khi render → cần đọc/ghi thư mục này. User chạy service thật là `minh_ngbinh`
(không phải `pos-web` như template — xem Vấn đề 2), và thư mục `/var/lib/pos-web` chưa được tạo
với quyền ghi cho user này → mọi request render interactive đều 500.

Đây đúng là bước "PHẢI tạo thư mục này + cấp quyền ghi cho user chạy POS.Web TRƯỚC khi start
service lần đầu" mà `docs/guide-deploy.md:145-149` đã cảnh báo — bị bỏ sót/làm sai user khi
deploy trên host này.

**Fix:**
```bash
sudo mkdir -p /var/lib/pos-web/dataprotection-keys
sudo chown -R minh_ngbinh:posops /var/lib/pos-web   # đổi đúng user thật đang chạy service (Vấn đề 2)
sudo chmod -R u+rwX,g+rwX /var/lib/pos-web
sudo systemctl restart pos-web

# verify
curl -s -o /dev/null -w 'login=%{http_code}\n' http://127.0.0.1:5001/login   # kỳ vọng 200
```
Đã xác nhận: **hết lỗi 500**, truy cập webapp bình thường qua nginx/domain thật.

> 2 dòng lỗi `InvalidOperationException ... thiếu biến môi trường POS_SECRET_KEY` từng thấy
> trong `app.err` chỉ là rác cũ từ đợt crash-loop 07/07 (file dừng ghi từ hôm đó) — **không phải**
> nguyên nhân của lỗi 500 lần này. Đừng nhầm 2 loại lỗi khi đọc log file cũ dài ngày.

## Vấn đề 4 — Chưa xử lý: process orphan chiếm cổng 5002, nginx `pos-api` trỏ nhầm

Phát hiện khi chẩn đoán Vấn đề 2/3, **chưa khắc phục**, cần làm ở lần sau:

- `sudo ss -tlnp | grep :5002` cho thấy 1 process `dotnet` khác (PID khác với `pos-web` thật)
  đang bind cổng 5002. Test cho thấy đây là **1 bản POS.Web cũ/hỏng** (`/health` → 200 nhưng
  `/login` → 404, do publish thiếu/hỏng static web assets manifest), **không phải** `POS.Api`.
- `sudo grep -r proxy_pass /etc/nginx/` cho thấy site `pos-api` lại `proxy_pass` sang
  `127.0.0.1:5002` — tức traffic dành cho `POS.Api` đang bị route nhầm vào process POS.Web
  orphan này.
- Ngoài ra còn site nginx `myapp` cũng trỏ `127.0.0.1:5001` — chưa rõ có phải cấu hình thừa/trùng
  lặp với site `pos-web` hay không.

**Việc cần làm lần sau:**
1. Xác định `POS.Api` thật đang chạy ở cổng nào (`sudo systemctl status pos-api`,
   `sudo ss -tlnp | grep dotnet`).
2. Sửa `proxy_pass` của site `pos-api` trong nginx trỏ đúng cổng đó.
3. `kill` process orphan đang chiếm 5002 sau khi xác nhận không còn gì phụ thuộc, tìm hiểu vì sao
   nó không bị thay thế khi `pos-web` restart (có thể được start tay ngoài systemd, hoặc là 1
   unit/service khác không phải `pos-web.service`).
4. Làm rõ và dọn site nginx `myapp` nếu là cấu hình thừa.
5. `/srv/pos/logs/web/app.log` không có logrotate (13.6MB tính tới 13/07) — cân nhắc cấu hình
   `logrotate` cho `/srv/pos/logs/web/*.log`.

## Checklist chẩn đoán rút gọn — lỗi 500 trên POS.Web (host bất kỳ)

```
□ sudo systemctl cat <service> — LUÔN lấy path/port/user THẬT, không tin template repo
□ sudo systemctl status <service> --no-pager -l — có đang crash-loop (auto-restart liên tục) không?
  → có: xem journalctl tìm status=2xx/... hoặc exception cụ thể trước khi đoán nguyên nhân app-level
□ sudo ss -tlnp | grep :<port> — xác nhận đúng PID/port đang test, coi chừng process orphan cũ
  còn sống trên port khác
□ curl .../health (Minimal API) vs curl .../login hoặc route Razor Component khác — nếu health
  OK mà route Razor 404 → nghi publish thiếu static web assets manifest; nếu 500 → đọc log thật
□ Đọc đúng file log THẬT theo unit (journalctl HOẶC StandardOutput=append:<file> tùy unit) —
  đừng giả định journal khi unit đã redirect ra file riêng
□ DataProtection: nếu 500 xảy ra trên trang InteractiveServer nhưng route redirect/Minimal API
  vẫn OK → nghi ngay quyền ghi thư mục DataProtection:KeyPath (mặc định
  /var/lib/pos-web/dataprotection-keys trên Linux) cho ĐÚNG user thật chạy service
□ Sau mỗi bước fix: test lại toàn bộ (health/root/login) — nhiều vấn đề độc lập có thể chồng lên
  nhau, đừng dừng ở dấu hiệu "có vẻ đã fix" đầu tiên
```

## Tham chiếu

| Nội dung | Xem tại |
|---|---|
| Runbook deploy POS.Web chuẩn (template) | `docs/guide-deploy.md` §3.2 |
| Data Protection Keys — lý do bắt buộc tạo thư mục trước khi start lần đầu | `docs/guide-deploy.md:145-149`, `src/POS.Web/Program.cs:118-134` |
| Mã hóa credentials `enc:...` / `POS_SECRET_KEY` | `docs/architecture/appsetting.md`, `src/POS.Infrastructure/Security/ConfigurationSecretExtensions.cs` |
| Health check `/ops/health` config | `docs/deploy/web-health-guide.md` |
| Fix issue tương tự cho POS.Worker (cùng host `sit-uat-server`) | `docs/deploy/fix_issue_pos-worker-host.md` |

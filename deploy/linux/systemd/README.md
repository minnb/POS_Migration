# Systemd unit mẫu cho POS.Api / POS.Web (native, UAT/PROD)

> Áp dụng khi POS.Api/POS.Web chạy **native** trên Ubuntu (không qua Docker) — chỉ POS.Worker dùng
> Docker (xem `docs/guide-deploy.md`). Nginx đứng trước, proxy vào `127.0.0.1:5001`/`5002` như đã
> cấu hình sẵn trong `nginx/pos-web*.conf` — không đổi gì phía nginx.

## Placeholder cần thay trước khi dùng thật

| Placeholder | Ý nghĩa | Ghi chú |
|---|---|---|
| `pos-api` / `pos-web` | Tên tài khoản Linux chạy từng service | Tạo mới nếu chưa có (xem bên dưới) |
| `/opt/pos/api` / `/opt/pos/web` | Thư mục publish (`dotnet publish -c Release -o ...`) | Đổi theo layout thật của server |
| `/etc/pos/pos-api.env` / `pos-web.env` | File chứa `POS_SECRET_KEY` (chỉ cần nếu appsettings có `enc:...`) | `chmod 600`, `chown root:root` |

## Cài đặt lần đầu

```bash
# 1. Tạo user riêng cho từng service (không login được, không có home tương tác)
sudo useradd --system --no-create-home --shell /usr/sbin/nologin pos-api
sudo useradd --system --no-create-home --shell /usr/sbin/nologin pos-web

# 2. Group posops PHẢI đã tồn tại (tạo bởi deploy/linux/setup-pos-dirs.sh) — thêm 2 user vào group
sudo usermod -aG posops pos-api
sudo usermod -aG posops pos-web

# 3. Publish code
sudo mkdir -p /opt/pos/api /opt/pos/web
dotnet publish src/POS.Api/POS.Api.csproj -c Release -o /opt/pos/api
dotnet publish src/POS.Web/POS.Web.csproj -c Release -o /opt/pos/web
sudo chown -R pos-api:pos-api /opt/pos/api
sudo chown -R pos-web:pos-web /opt/pos/web

# 4. Thư mục Data Protection Keys (POS.Web) — PHẢI tạo trước, xem src/POS.Web/Program.cs
#    (mặc định /var/lib/pos-web/dataprotection-keys trên Linux nếu không cấu hình
#    DataProtection:KeyPath riêng trong appsettings)
sudo mkdir -p /var/lib/pos-web/dataprotection-keys
sudo chown pos-web:pos-web /var/lib/pos-web/dataprotection-keys
sudo chmod 700 /var/lib/pos-web/dataprotection-keys

# 5. Cài + khởi động unit
sudo cp deploy/linux/systemd/pos-api.service /etc/systemd/system/
sudo cp deploy/linux/systemd/pos-web.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now pos-api pos-web

# 6. Kiểm tra
sudo systemctl status pos-api pos-web
sudo journalctl -u pos-api -n 50 --no-pager
```

> **Thứ tự bắt buộc**: chạy `deploy/linux/setup-pos-dirs.sh` (group `posops`, ftpbluepos) VÀ
> `deploy/linux/setup-pos-log-dirs.sh` (thư mục log) TRƯỚC bước 5 ở trên — service start trước khi
> thư mục log tồn tại/có quyền đúng sẽ ghi log lỗi ngay từ lần khởi động đầu tiên.

## Re-deploy (cập nhật version mới)

```bash
sudo systemctl stop pos-api
dotnet publish src/POS.Api/POS.Api.csproj -c Release -o /opt/pos/api
sudo systemctl start pos-api
```

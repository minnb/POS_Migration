#!/usr/bin/env bash
# deploy/linux/setup-pos-log-dirs.sh
#
# One-time (idempotent — an toàn chạy lại nhiều lần) thiết lập thư mục log dùng chung
# "/srv/pos/logs" cho POS.Api (systemd, native) + POS.Web (systemd, native) + POS.Worker (Docker).
# Xem docs/guide-deploy.md + deploy/linux/systemd/README.md.
#
# KHÁC deploy/linux/setup-pos-dirs.sh (ftpbluepos, chown theo 1 UID:GID Docker cố định 1654:1654):
# ở đây 3 service chạy theo 3 cơ chế khác nhau (2 native systemd với user riêng + 1 Docker container)
# nên KHÔNG có 1 UID chung để chown — thay vào đó dùng GROUP "posops" làm mẫu số chung + setgid
# (2750) trên mọi thư mục, để file mới do BẤT KỲ service nào tạo ra (dù qua systemd Group=posops
# hay Docker --user <uid>:<gid posops>) đều tự động thuộc group posops, không phụ thuộc UID/GID
# riêng của tiến trình tạo ra nó.
#
# Group "posops" PHẢI đã được tạo bởi deploy/linux/setup-pos-dirs.sh (gid cố định 1654) — script
# này KHÔNG tự tạo lại để tránh 2 nguồn tạo group cùng tên chạy độc lập, có thể lệch gid.
#
# Usage:
#   sudo ./deploy/linux/setup-pos-log-dirs.sh [BASE_DIR]
# BASE_DIR mặc định /srv/pos/logs (khớp Logging:LogDirectory trong appsettings.Production.json của
# cả POS.Api/POS.Web/POS.Worker).
set -euo pipefail

BASE_DIR="${1:-/srv/pos/logs}"
GROUP_NAME=posops
EXPECTED_GID=1654

if [[ $EUID -ne 0 ]]; then
  echo "Cần chạy bằng root (sudo)." >&2
  exit 1
fi

# Group PHẢI tồn tại sẵn đúng gid — không tự tạo (xem lý do ở comment đầu file).
if ! getent group "$GROUP_NAME" >/dev/null 2>&1; then
  echo "ERROR: group '$GROUP_NAME' chưa tồn tại. Chạy deploy/linux/setup-pos-dirs.sh trước." >&2
  exit 1
fi
existing_gid=$(getent group "$GROUP_NAME" | cut -d: -f3)
if [[ "$existing_gid" != "$EXPECTED_GID" ]]; then
  echo "ERROR: group '$GROUP_NAME' có gid $existing_gid (kỳ vọng $EXPECTED_GID) — kiểm tra lại trước khi tiếp tục." >&2
  exit 1
fi

# Cây thư mục — 1 subfolder/service, khớp Logging:FileLogDirectory từng appsettings.Production.json
# (POS.Api: /srv/pos/logs/api, POS.Web: /srv/pos/logs/web, POS.Worker: /srv/pos/logs/worker).
mkdir -p "$BASE_DIR/api" "$BASE_DIR/web" "$BASE_DIR/worker"

# Không ép owner UID (Api/Web dùng user systemd riêng "pos-api"/"pos-web", Worker dùng UID container
# tùy chọn) — chỉ ép GROUP + setgid, đủ để cả 3 đọc/ghi chéo nhau qua group posops mà không cần
# chung UID.
chgrp -R "$GROUP_NAME" "$BASE_DIR"
find "$BASE_DIR" -type d -exec chmod 2750 {} +
find "$BASE_DIR" -type f -exec chmod 640 {} +

echo ""
echo "Xong: $BASE_DIR sẵn sàng — group $GROUP_NAME (gid $EXPECTED_GID) đọc/ghi được qua setgid 2750."
echo "Yêu cầu còn lại trước khi start service:"
echo "  1. User systemd 'pos-api'/'pos-web' PHẢI là member của $GROUP_NAME:"
echo "       sudo usermod -aG $GROUP_NAME pos-api"
echo "       sudo usermod -aG $GROUP_NAME pos-web"
echo "  2. Container pos-worker PHẢI chạy --user với GID = $EXPECTED_GID (xem docs/guide-deploy.md §3.3):"
echo "       docker run ... --user \"<uid>:$EXPECTED_GID\" -v $BASE_DIR:$BASE_DIR ..."
echo "  3. systemd unit (pos-api.service/pos-web.service) đặt Group=$GROUP_NAME + UMask=0027"
echo "     (đã có sẵn trong deploy/linux/systemd/*.service)."

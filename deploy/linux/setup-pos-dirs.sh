#!/usr/bin/env bash
# deploy/linux/setup-pos-dirs.sh
#
# One-time (idempotent — an toàn chạy lại nhiều lần) thiết lập thư mục bind-mount dùng chung
# "ftpbluepos" trên Ubuntu host cho POS.Api + POS.Worker (xem docs/deploy/ubuntu-guide.md).
#
# Tạo cây thư mục, group "posops" đúng bằng GID của container (1654) để người vận hành đọc/ghi/xóa
# file trực tiếp không cần sudo, và set ownership + setgid để container lẫn người dùng tạo file mới
# đều tự động đúng quyền.
#
# Usage:
#   sudo ./deploy/linux/setup-pos-dirs.sh [BASE_DIR]
#
# BASE_DIR mặc định /srv/pos — dùng cho PROD (docker-compose.yml SIT và docker run PROD trong
# docs/guide-deploy.md đều mount /srv/pos/ftpbluepos, an toàn vì SIT/PROD không bao giờ chung host).
#
# UAT (docs/guide-deploy.md) chạy docker run trên CÙNG host với PROD — BẮT BUỘC truyền path khác để
# không lẫn dữ liệu test vào dữ liệu PROD thật:
#   sudo ./deploy/linux/setup-pos-dirs.sh /srv/pos/uat
set -euo pipefail

BASE_DIR="${1:-/srv/pos}"
APP_UID=1654
APP_GID=1654
GROUP_NAME=posops

if [[ $EUID -ne 0 ]]; then
  echo "Cần chạy bằng root (sudo)." >&2
  exit 1
fi

# 1. Group posops PHẢI đúng GID 1654 (trùng GID container "app") — để chown 1654:1654 và
#    chown 1654:posops là một, và để `usermod -aG posops <user>` cấp đúng quyền group container dùng.
#    Guard tránh đụng group/GID khác đã tồn tại sẵn trên host (vd 1 user thường trùng GID ngẫu nhiên).
if getent group "$GROUP_NAME" >/dev/null 2>&1; then
  existing_gid=$(getent group "$GROUP_NAME" | cut -d: -f3)
  if [[ "$existing_gid" != "$APP_GID" ]]; then
    echo "ERROR: group '$GROUP_NAME' đã tồn tại với gid $existing_gid (cần $APP_GID). Xử lý tay trước khi chạy lại." >&2
    exit 1
  fi
  echo "Group $GROUP_NAME đã tồn tại đúng gid $APP_GID — bỏ qua bước tạo."
elif getent group "$APP_GID" >/dev/null 2>&1; then
  other_name=$(getent group "$APP_GID" | cut -d: -f1)
  echo "ERROR: gid $APP_GID đã được group '$other_name' dùng (không phải '$GROUP_NAME'). Xử lý tay trước khi chạy lại." >&2
  exit 1
else
  groupadd -g "$APP_GID" "$GROUP_NAME"
  echo "Đã tạo group $GROUP_NAME (gid $APP_GID)."
fi

# 2. Cây thư mục — tạo sẵn các subfolder đã biết trước (app vẫn tự tạo thêm khi cần, vd
#    SyncDataPos/POS/ALL/{site}/{terminal}/, nhờ thừa hưởng setgid từ thư mục cha ở bước 3).
mkdir -p \
  "$BASE_DIR/ftpbluepos/SyncDataPos/Sale/Kafka" \
  "$BASE_DIR/ftpbluepos/SyncDataPos/Sale/BackupFiles" \
  "$BASE_DIR/ftpbluepos/SyncDataPos/Sale/error" \
  "$BASE_DIR/ftpbluepos/SyncDataPos/Sale/_work" \
  "$BASE_DIR/ftpbluepos/BluePosUpgrade"

# 3. Ownership + setgid.
#    - chown 1654:1654: khớp UID:GID container "app" (mcr.microsoft.com/dotnet/aspnet:10.0) — container
#      ghi được ngay, không cần thêm bước gì trong Dockerfile.
#    - chmod 2770 (setgid + rwxrwx---): container VÀ mọi user thuộc group posops đọc/ghi/xóa được;
#      user khác trên host không có quyền gì (dữ liệu sale/master-data, không world-readable).
#      Setgid trên thư mục khiến MỌI file/thư mục con mới tạo — bởi container hay bởi người vận hành
#      gõ lệnh trực tiếp — tự động mang group posops, bất kể primary group của người/tiến trình tạo ra
#      nó, và bit này tự lan xuống các thư mục con được tạo thêm sau này.
chown -R "$APP_UID:$APP_GID" "$BASE_DIR/ftpbluepos"
find "$BASE_DIR/ftpbluepos" -type d -exec chmod 2770 {} +

echo ""
echo "Xong: $BASE_DIR/ftpbluepos sẵn sàng cho uid:gid $APP_UID:$APP_GID (group $GROUP_NAME)."
echo "Cấp quyền cho người vận hành:"
echo "  sudo usermod -aG $GROUP_NAME <username>"
echo "  (user đó cần đăng nhập lại, hoặc chạy 'newgrp $GROUP_NAME', để nhận quyền ngay trong phiên hiện tại)"

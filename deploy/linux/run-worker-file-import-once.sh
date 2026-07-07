#!/usr/bin/env bash
# deploy/linux/run-worker-file-import-once.sh
#
# Wrapper cho Model A (cronjob thật trên Ubuntu host) — gọi POS.Worker ở chế độ "--run-once":
# quét 1 chu kỳ InboxFolder chung với POS.Api/POS.Web rồi thoát (không phải process residency).
# Dùng crontab để lặp lịch (vd mỗi 1 phút) — xem docs/deploy/pos-worker-ubuntu-guide.md.
#
# `flock -n` chống chạy chồng nếu 1 chu kỳ trước đó chưa xong khi cron kích hoạt lượt mới —
# lượt mới sẽ bị bỏ qua (không xếp hàng) thay vì chạy song song 2 tiến trình cùng quét 1 thư mục.
#
# Usage (crontab, dưới user thuộc group "posops" — xem deploy/linux/setup-pos-dirs.sh):
#   * * * * * /srv/pos/app/worker/run-worker-file-import-once.sh >> /srv/pos/logs/worker-cron/cron.log 2>&1
set -euo pipefail

WORKER_DIR="${WORKER_DIR:-/srv/pos/app/worker}"
LOCK_FILE="${LOCK_FILE:-/tmp/pos-worker-file-import.lock}"

cd "$WORKER_DIR"

exec flock -n "$LOCK_FILE" \
  env DOTNET_ENVIRONMENT=CronHost dotnet POS.Worker.dll --run-once

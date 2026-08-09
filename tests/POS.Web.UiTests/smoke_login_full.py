"""
Full login test cho POS.Web (demo skill webapp-testing) — luồng đăng nhập THẬT.

Điền credential vào form /login, submit, xác nhận đăng nhập thành công (điều hướng khỏi /login
và sidebar hiển thị tên user + nút Đăng xuất). Luồng này chạm DB thật qua
WebUserService.ValidateLoginAsync (query bảng DashboardUsers, ConnectionStrings:CentralMD).

Credential đọc từ biến môi trường (KHÔNG hardcode secret trong repo), mặc định = seed admin:
  POSWEB_TEST_USER (default 'admin')
  POSWEB_TEST_PASS (default 'Admin@0987'  — seed trong 001_DashboardUsers.sql)

Yêu cầu: POS.Web đang chạy ở http://localhost:5170 và DB CentralMD reachable.
Exit code 0 nếu đăng nhập thành công, != 0 nếu thất bại.
"""

import os
import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

BASE_URL = "http://localhost:5170"
USER = os.environ.get("POSWEB_TEST_USER", "admin")
PASSWORD = os.environ.get("POSWEB_TEST_PASS", "Admin@0987")
ARTIFACT = Path(__file__).parent / "artifacts" / "login_full.png"
ARTIFACT.parent.mkdir(parents=True, exist_ok=True)

ok = True


def check(name: str, passed: bool, detail: str = ""):
    global ok
    ok = ok and passed
    status = "PASS" if passed else "FAIL"
    print(f"RESULT: {status} - {name}" + (f" ({detail})" if detail else ""))


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    # 1. Mở trang login
    page.goto(f"{BASE_URL}/login")
    page.wait_for_load_state("networkidle")

    # 2. Điền credential + submit
    page.get_by_label("Tên đăng nhập").fill(USER)
    page.get_by_label("Mật khẩu").fill(PASSWORD)
    print(f"INFO: submitting login as user='{USER}'")
    page.get_by_role("button", name="Đăng nhập").click()

    # 3. Chờ redirect qua bridge-token -> dashboard "/" (hoặc dừng lại ở /login nếu sai)
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(2500)  # để chuỗi redirect signin/{token} -> "/" settle
    final_url = page.url
    print(f"INFO: final_url = {final_url}")

    # 4. Đánh giá kết quả
    left_login = "/login" not in final_url
    check("điều hướng khỏi /login sau submit", left_login, f"url={final_url}")

    if not left_login:
        # Còn ở /login -> lấy message lỗi làm bằng chứng thất bại
        alert = page.locator(".mud-alert-message")
        err = alert.first.inner_text() if alert.count() > 0 else "(không có alert)"
        check("đăng nhập thành công", False, f"lỗi hiển thị: {err}")
    else:
        # Đã vào app: xác nhận UI đã xác thực
        user_name = page.locator(".pos-sidebar-user-name")
        logout = page.locator('a[href="/logout"]')
        name_txt = user_name.first.inner_text() if user_name.count() > 0 else ""
        check("sidebar hiển thị user đã đăng nhập", user_name.count() > 0, f"user='{name_txt}'")
        check("nút Đăng xuất (/logout) hiển thị", logout.count() > 0)

    page.screenshot(path=str(ARTIFACT), full_page=True)
    print(f"SCREENSHOT: {ARTIFACT}")
    browser.close()

print("SUMMARY: " + ("LOGIN OK" if ok else "LOGIN FAILED"))
sys.exit(0 if ok else 1)

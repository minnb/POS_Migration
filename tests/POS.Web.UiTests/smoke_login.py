"""
Smoke test cho trang Login của POS.Web (demo skill webapp-testing).

Chỉ chứa logic Playwright — vòng đời server (dotnet run) do
.claude/skills/webapp-testing/scripts/with_server.py quản lý.

Kịch bản (luồng anonymous, KHÔNG cần credential/DB):
  1. Mở http://localhost:5170/login, chờ networkidle (Blazor circuit render xong).
  2. Assert <title> chứa "Đăng nhập".
  3. Assert nút "Đăng nhập" hiển thị.
  4. Assert ô "Tên đăng nhập" hiển thị.
  5. Chụp artifacts/login.png làm bằng chứng trực quan.

Exit code != 0 nếu bất kỳ assertion nào FAIL.
"""

import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

# Console Windows mặc định cp1252 -> ép UTF-8 để in được tiếng Việt.
sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

BASE_URL = "http://localhost:5170"
ARTIFACT = Path(__file__).parent / "artifacts" / "login.png"
ARTIFACT.parent.mkdir(parents=True, exist_ok=True)

results = []


def check(name: str, ok: bool, detail: str = ""):
    status = "PASS" if ok else "FAIL"
    print(f"RESULT: {status} - {name}" + (f" ({detail})" if detail else ""))
    results.append(ok)


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    page.goto(f"{BASE_URL}/login")
    page.wait_for_load_state("networkidle")  # chờ JS/Blazor circuit chạy

    # 2. Title
    title = page.title()
    check("title chứa 'Đăng nhập'", "Đăng nhập" in title, f"title='{title}'")

    # 3. Nút Đăng nhập
    login_btn = page.get_by_role("button", name="Đăng nhập")
    check("nút 'Đăng nhập' hiển thị", login_btn.is_visible())

    # 4. Ô Tên đăng nhập (MudBlazor render <label> + <input>)
    username = page.get_by_label("Tên đăng nhập")
    check("ô 'Tên đăng nhập' hiển thị", username.is_visible())

    # 5. Screenshot bằng chứng
    page.screenshot(path=str(ARTIFACT), full_page=True)
    print(f"SCREENSHOT: {ARTIFACT}")

    browser.close()

passed = sum(results)
total = len(results)
print(f"SUMMARY: {passed}/{total} passed")
sys.exit(0 if passed == total else 1)

"""
Auto-test Playwright cho chức năng Phát hành Coupon (POS.Web) — CouponIssuePage.razor.

Bám theo docs/web/testing/coupon-flow.md (đã cập nhật 2026-07-16 sau khi đối chiếu với code thật)
và khuôn mẫu tests/POS.Web.UiTests/smoke_login_full.py.

Phạm vi (đã chốt với user — xem plan): positive flow đầy đủ (tạo coupon Auto thật, DÙNG ĐƯỢC CHO
POS — không rollback/dry-run) + 4 case negative trọng điểm (TC-N01, TC-N03, TC-N03b, TC-N14).
KHÔNG phủ Import Excel, Item picker, Advanced dialog (unreachable qua UI — xem TC-I04 trong
coupon-flow.md), TC-N06/N15/N18/N19 (cũng unreachable — MudNumericField Min/Max tự clamp hoặc
dialog Advanced ẩn, xác nhận qua chạy thật), hay 12+1 điểm yếu E1-E13 (edge case, xem mục 6
coupon-flow.md — test thủ công).

Sự thật quan trọng đã xác nhận qua code thật (KHÔNG suy đoán — xem coupon-flow.md để biết dòng
code tương ứng):
  - Sau khi Lưu thành công, trang điều hướng THẲNG về /promotion/coupons (không "reload tại chỗ").
  - CouponsPage list KHÔNG có icon "Sửa" — chỉ có "Xem chi tiết" (?mode=view) và "Xóa" (thực chất
    là soft-block qua UpdateBlockedAsync, KHÔNG phải hard-delete). Muốn vào chế độ Sửa (không
    View) phải điều hướng thẳng bằng URL ?id={ItemNo} (không &mode=view).
  - Filter panel trên list chỉ có 1 ô "Từ khóa (mã / tên / mã coupon)" + dropdown "Hiệu lực".
  - DiscountType mặc định = 1/Percent (`CouponAdvancedSaveRequest.DiscountType = 1`, xác nhận qua
    verify DOM thật) — KHÔNG cần tự chọn dropdown "Kiểu giảm giá". Nhưng "Giá trị giảm giá" mặc
    định = 0 → BẮT BUỘC tự điền > 0 trước khi Lưu, nếu không `ValidateHeaderFields` luôn chặn.

Credential đọc từ biến môi trường (KHÔNG hardcode secret), mặc định = seed admin (SystemAdmin,
thỏa policy BackOfficeAndAbove):
  POSWEB_TEST_USER (default 'admin')
  POSWEB_TEST_PASS (default 'Admin@0987')

Yêu cầu: POS.Web chạy ở http://localhost:5170, DB CentralMD reachable, đã deploy 3 SP
usp_SetupCoupon_Read/Save/Delete (xem docs/ROLLOUT.md §D3). Nếu thiếu, script phát hiện banner lỗi
và dừng sớm với thông báo rõ ràng thay vì báo FAIL mơ hồ.

Exit code 0 nếu mọi RESULT đều PASS, != 0 nếu có FAIL.
"""

import os
import sys
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

BASE_URL = "http://localhost:5170"
USER = os.environ.get("POSWEB_TEST_USER", "admin")
PASSWORD = os.environ.get("POSWEB_TEST_PASS", "Admin@0987")
ARTIFACT_DIR = Path(__file__).parent / "artifacts"
ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)

ISSUE_URL = f"{BASE_URL}/promotion/coupons/issue"
LIST_URL = f"{BASE_URL}/promotion/coupons"

# Chuỗi định danh duy nhất mỗi lần chạy — tránh trùng dữ liệu giữa các lần chạy, dễ lọc/dọn tay.
RUN_STAMP = str(int(time.time()))
TEST_DESC = f"AUTOTEST Coupon {RUN_STAMP}"
TEST_PREFIX = "ZTST"

ok = True
_shot_seq = 0


def check(name: str, passed: bool, detail: str = ""):
    global ok
    ok = ok and passed
    status = "PASS" if passed else "FAIL"
    print(f"RESULT: {status} - {name}" + (f" ({detail})" if detail else ""))


def slugify(name: str) -> str:
    """Loại bỏ ký tự Windows cấm dùng trong tên file (< > : " / \\ | ? *) và khoảng trắng."""
    import re
    safe = name.lower().replace(" ", "_")
    return re.sub(r'[<>:"/\\|?*]', "", safe)


def shot(page, name: str):
    global _shot_seq
    _shot_seq += 1
    path = ARTIFACT_DIR / f"coupon_issue_{_shot_seq:02d}_{slugify(name)}.png"
    page.screenshot(path=str(path), full_page=True)
    print(f"SCREENSHOT: {path}")


def login(page):
    page.goto(f"{BASE_URL}/login")
    page.wait_for_load_state("networkidle")
    page.get_by_label("Tên đăng nhập").fill(USER)
    page.get_by_label("Mật khẩu").fill(PASSWORD)
    page.get_by_role("button", name="Đăng nhập").click()
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(2000)
    left_login = "/login" not in page.url
    check("đăng nhập thành công", left_login, f"url={page.url}")
    return left_login


def form_error_banner(page):
    """Banner đỏ (Severity.Error) khi SP chưa deploy / lỗi tải form (xem TC-L05 coupon-flow.md).
    Chỉ lọc theo class chứa 'error' — trang luôn có sẵn 1 MudAlert Severity.Info (gợi ý tick sản
    phẩm) khi coupon mới chưa có mã, KHÔNG được coi đó là lỗi."""
    alerts = page.locator(".mud-alert")
    for i in range(alerts.count()):
        cls = (alerts.nth(i).get_attribute("class") or "").lower()
        if "error" in cls:
            msg = alerts.nth(i).locator(".mud-alert-message")
            return msg.first.inner_text() if msg.count() > 0 else alerts.nth(i).inner_text()
    return None


def goto_new_issue_page(page):
    page.goto(ISSUE_URL)
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(300)


def pick_date_today(page, label: str):
    """Mở MudDatePicker theo Label, lùi về tháng hiện tại (so khớp header 'Tháng M năm YYYY'),
    bấm vào ô ngày = hôm nay. MudBlazor 9.5 KHÔNG có class '-today' riêng cho ô hiện tại (đã xác
    nhận qua dump DOM thật) — phải so khớp số ngày + loại trừ ô '.mud-hidden' (ngày tháng liền kề)."""
    import re
    from datetime import date

    today = date.today()
    target_header = f"Tháng {today.month} năm {today.year}"

    page.get_by_label(label, exact=True).click()
    page.wait_for_timeout(400)
    header = page.locator(".mud-picker-calendar-header-transition p")
    for _ in range(6):
        if header.count() > 0 and header.first.inner_text().strip() == target_header:
            break
        prev_btn = page.locator('button[aria-label*="Previous month"]')
        if prev_btn.count() == 0:
            return False
        prev_btn.first.click()
        page.wait_for_timeout(300)
    else:
        return False

    day_btn = page.locator(".mud-picker-calendar-day:not(.mud-hidden)",
                            has_text=re.compile(rf"^{today.day}$"))
    if day_btn.count() == 0:
        return False
    day_btn.first.click()
    page.wait_for_timeout(300)
    return True


def fill_issue_more_dialog(page, prefix=TEST_PREFIX, char_of_number=2, char_position=3, quantity=5, lencode=None):
    """Điền dialog 'Phát hành mã coupon' (CouponIssueMoreDialog) đang mở, KHÔNG submit."""
    page.get_by_label("Tiền tố").fill(prefix)
    if lencode is not None:
        page.get_by_label("Kích thước mã (5-20)").fill(str(lencode))
    page.get_by_label("Số chữ cái").fill(str(char_of_number))
    page.get_by_label("Vị trí đứng").fill(str(char_position))
    page.get_by_label("Số lượng mã phát hành").fill(str(quantity))


def snackbar_has_text(page, text: str, timeout=6000) -> bool:
    try:
        page.get_by_text(text, exact=False).first.wait_for(state="visible", timeout=timeout)
        return True
    except Exception:
        return False


def run_negative_case(page, name: str, expected_message: str, setup):
    """Chuẩn bị 1 case negative trên trang issue mới toanh, bấm Lưu, verify snackbar."""
    goto_new_issue_page(page)
    err = form_error_banner(page)
    if err:
        check(name, False, f"banner lỗi form: {err}")
        return
    setup(page)
    page.get_by_role("button", name="Lưu").click()
    found = snackbar_has_text(page, expected_message)
    check(name, found, f"kỳ vọng snackbar chứa: '{expected_message}'")
    shot(page, name)


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    if not login(page):
        shot(page, "login_failed")
        browser.close()
        print("SUMMARY: 0/0 passed — KHÔNG THỂ ĐĂNG NHẬP, dừng sớm")
        sys.exit(1)

    # ── 1. Điều hướng từ list → trang Phát hành ─────────────────────────────
    page.goto(LIST_URL)
    page.wait_for_load_state("networkidle")
    issue_btn = page.get_by_role("button", name="Phát hành coupon")
    check("nút 'Phát hành coupon' hiển thị trên list", issue_btn.count() > 0)
    issue_btn.first.click()
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(500)  # <PageTitle> có thể cập nhật sau networkidle (render Blazor)
    check("điều hướng sang trang Phát hành Coupon", "Phát hành Coupon" in page.title(),
          f"title='{page.title()}'")

    err = form_error_banner(page)
    if err:
        check("form load được (không có banner lỗi SP)", False, err)
        shot(page, "form_error_banner")
        browser.close()
        print("SUMMARY: dừng sớm — SP usp_SetupCoupon_* có thể chưa deploy")
        sys.exit(1)

    # ── 2. Positive: tạo coupon Auto hợp lệ (TC-I01 đã sửa trong coupon-flow.md) ──
    page.get_by_label("Tên phát hành").fill(TEST_DESC)
    # Kiểu giảm giá đã mặc định = "Discount Percent (%)" (CouponAdvancedSaveRequest.DiscountType
    # có property initializer = 1, xác nhận qua verify DOM thật) — không cần tự chọn.
    page.get_by_label("Giá trị giảm giá (%/VNĐ)").fill("10")
    shot(page, "positive_form_filled")

    page.get_by_role("button", name="Lưu").click()
    page.wait_for_timeout(500)
    dialog_visible = page.get_by_label("Tiền tố").count() > 0
    check("dialog 'Phát hành mã coupon' mở sau khi bấm Lưu", dialog_visible)

    if dialog_visible:
        fill_issue_more_dialog(page, quantity=5)
        shot(page, "positive_dialog_filled")
        page.get_by_role("button", name="PHÁT HÀNH").click()
        page.wait_for_load_state("networkidle")
        page.wait_for_timeout(1000)

    navigated_to_list = "/promotion/coupons" in page.url and "/issue" not in page.url
    check("sau Lưu điều hướng về /promotion/coupons (KHÔNG reload tại chỗ)", navigated_to_list,
          f"url={page.url}")
    shot(page, "positive_after_save")

    # ── 3. Verify coupon xuất hiện trong list (filter theo Từ khóa) ─────────
    item_no = None
    if navigated_to_list:
        page.get_by_label("Từ khóa (mã / tên / mã coupon)", exact=False).fill(TEST_DESC)
        page.get_by_role("button", name="Tìm").click()
        page.wait_for_timeout(800)
        row = page.locator(".mud-table-row", has_text=TEST_DESC)
        check("coupon vừa tạo xuất hiện trong list (filter theo Từ khóa)", row.count() >= 1,
              f"số dòng khớp={row.count()}")
        shot(page, "list_filtered")

        if row.count() >= 1:
            view_btn = row.first.locator("button").first  # icon đầu tiên = "Xem chi tiết" (dòng thao tác)
            view_btn.click()
            page.wait_for_load_state("networkidle")
            page.wait_for_timeout(300)

            title_txt = page.locator(".pos-page-header-title").first.inner_text()
            check("mở 'Xem chi tiết' hiển thị đúng tiêu đề", "Xem coupon" in title_txt,
                  f"title='{title_txt}'")
            if "Xem coupon" in title_txt:
                item_no = title_txt.split("Xem coupon", 1)[1].strip()

            issue_more_btn = page.get_by_role("button", name="PHÁT HÀNH THÊM")
            check("chế độ Xem: nút 'PHÁT HÀNH THÊM' hiển thị", issue_more_btn.count() > 0)
            save_btn_in_view = page.get_by_role("button", name="Lưu")
            check("chế độ Xem: nút 'Lưu' KHÔNG hiển thị (chưa đổi Blocked)",
                  save_btn_in_view.count() == 0)
            shot(page, "view_mode")

    # ── 4. Chế độ Sửa qua URL trực tiếp (KHÔNG có icon Sửa trên list — xem G7) ──
    if item_no:
        page.goto(f"{ISSUE_URL}?id={item_no}")
        page.wait_for_load_state("networkidle")
        page.wait_for_timeout(300)

        edit_title = page.locator(".pos-page-header-title").first.inner_text()
        check("chế độ Sửa (URL trực tiếp): tiêu đề đúng", f"Sửa coupon {item_no}" in edit_title,
              f"title='{edit_title}'")

        issue_type_select = page.get_by_label("Cách phát hành")
        is_disabled = issue_type_select.count() > 0 and issue_type_select.first.is_disabled()
        check("chế độ Sửa: 'Cách phát hành' bị khóa (CodeFieldsLocked)", is_disabled)

        codes_tab = page.get_by_text("Mã coupon đã phát hành", exact=False)
        check("chế độ Sửa: tab 'Mã coupon đã phát hành' tồn tại", codes_tab.count() > 0)
        if codes_tab.count() > 0:
            codes_tab.first.click()
            page.wait_for_timeout(500)
            code_rows = page.locator("tbody.mud-table-body tr")
            check("tab mã coupon hiển thị 5 mã vừa phát hành", code_rows.count() == 5,
                  f"số dòng={code_rows.count()}")
        shot(page, "edit_mode_locked")
    else:
        check("chế độ Sửa qua URL trực tiếp", False, "không lấy được ItemNo từ bước trước")

    # ── 5. Negative cases (mỗi case trên 1 trang issue mới) ─────────────────
    run_negative_case(
        page, "TC-N01 bỏ trống Tên phát hành",
        "Vui lòng nhập tên phát hành coupon",
        setup=lambda p: None,
    )

    def _setup_n03(p):
        p.get_by_label("Tên phát hành").fill(f"{TEST_DESC} N03")
        # Giá trị giảm giá phải điền hợp lệ (>0, <=100) để cô lập đúng validate ngày đang test —
        # nếu để mặc định 0, check "% giảm giá" (chạy SAU check ngày) sẽ không ảnh hưởng vì check
        # ngày luôn chạy TRƯỚC trong ValidateHeaderFields, nhưng điền sẵn giúp thông báo lỗi rõ
        # ràng nếu việc chỉnh ngày dưới đây thất bại (tránh nhầm sang lỗi % giảm giá).
        p.get_by_label("Giá trị giảm giá (%/VNĐ)").fill("10")
        date_changed = pick_date_today(p, "Đến ngày")
        if not date_changed:
            print("WARN: TC-N03 không đổi được 'Đến ngày' qua calendar picker — kết quả có thể sai")

    run_negative_case(
        page, "TC-N03 Từ ngày >= Đến ngày",
        "Từ ngày phải nhỏ hơn Đến ngày",
        setup=_setup_n03,
    )

    def _setup_n03b(p):
        p.get_by_label("Tên phát hành").fill(f"{TEST_DESC} N03b")
        # Giá trị giảm giá mặc định = 0 (Kiểu giảm giá mặc định = Percent) — KHÔNG cần điền gì
        # thêm để tái hiện "phải lớn hơn 0". (Lưu ý: MudNumericField Max=100 khi Percent sẽ tự
        # clamp mọi giá trị gõ >100 xuống 100 ở client — nên nhánh ">100" KHÔNG tái hiện được
        # bằng cách gõ số lớn hơn 100 vào ô này; nhánh "<=0" mặc định là cách tái hiện đáng tin cậy.)

    run_negative_case(
        page, "TC-N03b giá trị giảm giá % ngoài khoảng 0-100",
        "Giá trị giảm giá theo % phải lớn hơn 0 và không vượt quá 100",
        setup=_setup_n03b,
    )

    # TC-N06 (Số lượng ≤ 0) — ĐÃ LOẠI KHỎI PHẠM VI TỰ ĐỘNG, xác nhận qua chạy thật 2026-07-16:
    # MudNumericField Min="1" của ô "Số lượng mã phát hành" tự động clamp giá trị gõ "0" về 1
    # trước khi submit (giống cơ chế Max="100" tự clamp của "Giá trị giảm giá" — xem TC-N03b) —
    # dialog KHÔNG bị chặn, coupon được tạo thành công với Quantity thực nhận = 1 (không phải 0).
    # Xem coupon-flow.md TC-N06 đã cập nhật. KHÔNG viết case "negative" ở đây vì không có gì để
    # assert thất bại — hành vi thật là "luôn thành công", không phải lỗi validate.

    def _setup_n14(p):
        p.get_by_label("Tên phát hành").fill(f"{TEST_DESC} N14")
        p.get_by_label("Giá trị giảm giá (%/VNĐ)").fill("10")
        p.get_by_label("Áp dụng theo danh sách sản phẩm", exact=False).first.check()

    goto_new_issue_page(page)
    err = form_error_banner(page)
    if err:
        check("TC-N14 tick sản phẩm nhưng danh sách rỗng", False, f"banner lỗi form: {err}")
    else:
        _setup_n14(page)
        page.get_by_role("button", name="Lưu").click()
        page.wait_for_timeout(500)
        if page.get_by_label("Tiền tố").count() > 0:
            fill_issue_more_dialog(page, quantity=1)
            page.get_by_role("button", name="PHÁT HÀNH").click()
        found = snackbar_has_text(page, "Vui lòng thêm sản phẩm vào voucher/coupon")
        check("TC-N14 tick sản phẩm nhưng danh sách rỗng", found,
              "kỳ vọng snackbar 'Vui lòng thêm sản phẩm vào voucher/coupon'")
        shot(page, "tc_n14_empty_items")

    browser.close()

if item_no:
    print(f"INFO: coupon test đã tạo THẬT trong DB — ItemNo={item_no}, "
          f"Description='{TEST_DESC}', Prefix='{TEST_PREFIX}', 5 mã Auto — KHÔNG tự xóa được qua UI")

print("SUMMARY: " + ("ALL PASSED" if ok else "SOME FAILED"))
sys.exit(0 if ok else 1)

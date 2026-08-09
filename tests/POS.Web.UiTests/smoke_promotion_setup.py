"""
Auto-test Playwright cho chức năng Cài đặt CTKM (POS.Web) — PromotionSetupPage.razor.

Bám theo docs/web/testing/promotion-setup.md (23 test case CTKM-01..CTKM-23, đối chiếu trực tiếp
với code thật 2026-07-16) và khuôn mẫu tests/POS.Web.UiTests/smoke_coupon_issue.py.

Phạm vi (đã chốt với user — xem plan): positive flow ĐẦY ĐỦ gồm cả Duyệt CTKM (tạo 1 CTKM thật,
publish thật sang bảng Offer* — KHÔNG dry-run, KHÔNG rollback, KHÔNG sửa lại được sau khi duyệt)
+ 3 case negative (CTKM-03, CTKM-04, CTKM-22) + 2 case conditional tự SKIP nếu môi trường không có
OfferType tương ứng (CTKM-18, CTKM-19) + **SWEEP toàn bộ Loại CTKM có trong dropdown** (bổ sung
theo yêu cầu user "test được từng OfferType"): với MỖI Loại đang có sẵn trong môi trường, tự thích
ứng field cần điền theo tab/checkbox thực tế hiện ra (thêm dòng Buy/Get nếu tab hiện, điền MinValue
nếu là loại tổng bill, điền Voucher từ/đến ngày nếu checkbox Voucher bị OfferType tự khoá true) rồi
thử Lưu tạm — báo PASS/FAIL riêng cho từng Loại kèm message lỗi THẬT nếu có. KHÔNG phủ: AND/OR
nhóm SP cụ thể, cửa hàng cụ thể (CTKM-05..09), phân trang/audit log (CTKM-15..17), verify DB sau
Duyệt bằng SQL (CTKM-20/21), trang downstream /promotion/offers — xem guide mục "Giới hạn" để biết
lý do từng case.

Sự thật quan trọng đã xác nhận qua đọc trực tiếp PromotionSetupPage.razor (1410 dòng, KHÔNG suy
đoán):
  - Trang này VỪA là list VỪA là editor (toggle `_editing`), KHÔNG có route riêng cho create/edit
    — "Thêm CTKM"/"Sửa"/"Xem" đều chỉ đổi state trong cùng 1 component, KHÔNG đổi URL.
  - Sau khi "Lưu tạm" thành công, trang VẪN Ở LẠI editor (KHÔNG tự chuyển về list như CouponIssuePage)
    — `_header.No` được gán ngay, nút "Duyệt CTKM" xuất hiện ngay dưới form, KHÔNG cần quay lại
    list rồi mở lại.
  - Tab "Sản phẩm mua"/"Sản phẩm khuyến mãi" ẩn/hiện HOÀN TOÀN động theo cờ `IsSetupBuy`/
    `IsSetupGet` của `dbo.OfferType` đang chọn — KHÔNG có mã ZB nào được đảm bảo tồn tại trong mọi
    môi trường, nên script chọn "Loại CTKM" ĐẦU TIÊN có sẵn trong dropdown rồi thích ứng theo tab
    thực tế hiện ra, KHÔNG hardcode mã ZB nào cho positive flow.
  - Nút bulk "Thêm dòng (Ctrl+A)" (Buy/Get) dùng field "Số lượng dòng" mặc định = 10 — PHẢI set về
    1 trước khi bấm, nếu không sẽ thêm 10 dòng trống.
  - Cột "Barcode/Mã Nhóm SP" (placeholder "Nhập barcode...") bind `context.No`, cột "ĐVT" bind
    `context.UnitOfMeasure` — cả 2 là MudTextField text tự do. Để TEST THẬT (publish ra dữ liệu
    Offer* dùng được), điền ItemNo + Uom CÓ THẬT trong `dbo.Item` — nguồn mã lấy từ file cấu hình
    `test_products.json` (override qua POSWEB_TEST_PRODUCTS_FILE), chọn NGẪU NHIÊN qua
    random_product(). Chưa cấu hình → fallback mã placeholder + WARN (chỉ đủ qua validate
    HasLineItem, KHÔNG đảm bảo item tồn tại thật).
  - Cột "Số lượng" của dòng Buy/Get dùng `MudNumericField Min="0"` (KHÔNG phải Min="1" như ô
    "Số lượng mã phát hành" của Coupon) — KHÔNG tự clamp về 1, nên CTKM-22 (Quantity=0) tái hiện
    được thật qua UI.
  - `MudMessageBox` xác nhận Duyệt có Title "Duyệt CTKM", nút Yes chữ "Duyệt" (KHÁC chữ nút mở
    dialog "Duyệt CTKM" ở form).

Credential đọc từ biến môi trường (KHÔNG hardcode secret), mặc định = seed admin (SystemAdmin,
thỏa policy BackOfficeAndAbove):
  POSWEB_TEST_USER (default 'admin')
  POSWEB_TEST_PASS (default 'Admin@0987')

Lọc Loại CTKM cần test (mặc định KHÔNG set = test toàn bộ như cũ — sweep hết mọi Loại trong
dropdown, tốn thời gian nếu môi trường có nhiều Loại). Set biến này để CHỈ test 1 hoặc vài Loại cụ
thể, ví dụ chỉ cần test kỹ riêng ZB06:
  $env:POSWEB_TEST_OFFER_TYPES = "ZB06"
  python tests/POS.Web.UiTests/smoke_promotion_setup.py
Nhiều Loại (item đầu test SÂU — Lưu tạm+round-trip+Duyệt, các item sau chỉ sweep NÔNG — Lưu tạm):
  $env:POSWEB_TEST_OFFER_TYPES = "ZB06,ZB13"
Loại không tồn tại trong dropdown môi trường hiện tại → dừng sớm báo lỗi rõ ràng (KHÔNG âm thầm
test Loại khác thay thế).

Yêu cầu: POS.Web chạy ở http://localhost:5170, DB CentralMD reachable, đã deploy
docs/sql/SetupPromotion_Save.sql + docs/sql/SetupPromotion_ApproveAndStatus.sql, và có ≥1
dbo.OfferType(Enabled=1) + ≥1 dbo.SalesOrderType(IsActive=1). Nếu thiếu, script phát hiện banner
lỗi / dropdown rỗng và dừng sớm với thông báo rõ ràng thay vì báo FAIL mơ hồ.

Exit code 0 nếu mọi RESULT đều PASS, != 0 nếu có FAIL.
"""

import json
import os
import random
import re
import sys
import time
from datetime import date, timedelta
from pathlib import Path
from playwright.sync_api import sync_playwright

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

BASE_URL = "http://localhost:5170"
USER = os.environ.get("POSWEB_TEST_USER", "admin")
PASSWORD = os.environ.get("POSWEB_TEST_PASS", "Admin@0987")
ARTIFACT_DIR = Path(__file__).parent / "artifacts"
ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)

# Lọc Loại CTKM cần test — mặc định RỖNG = giữ hành vi cũ (luồng chính lấy Loại ĐẦU TIÊN có sẵn
# trong dropdown; sweep chạy qua TẤT CẢ Loại có trong dropdown). Set biến này để CHỈ test 1 hoặc
# vài Loại cụ thể (danh sách ngăn cách bởi dấu phẩy), tránh phải chạy đủ mọi Loại mỗi lần — ví dụ
# test kỹ riêng ZB06:
#   $env:POSWEB_TEST_OFFER_TYPES = "ZB06"
# Item ĐẦU TIÊN trong danh sách được luồng chính (CTKM-02/10/11/13/14/23) dùng để test SÂU (Lưu
# tạm + round-trip + Duyệt); các item còn lại (nếu có) chỉ được sweep NÔNG (chỉ thử Lưu tạm) —
# xem mục 4.1 guide. Không khớp mã nào trong dropdown → luồng chính dừng sớm báo lỗi rõ ràng
# (KHÔNG âm thầm fallback sang Loại khác — người dùng đã chỉ định rõ muốn test Loại nào).
OFFER_TYPE_FILTER = [c.strip().upper() for c in
                      os.environ.get("POSWEB_TEST_OFFER_TYPES", "").split(",") if c.strip()]

# Nhóm cửa hàng mặc định chọn cho MỌI CTKM được Lưu tạm — BẮT BUỘC (page + backend nay yêu cầu
# ≥1 nhóm cửa hàng áp dụng, nếu không publish ra 0 dòng Offer* lúc Duyệt). Mặc định '2018'; đổi
# qua biến môi trường nếu môi trường không có nhóm này. Nếu code cấu hình không có trong dropdown
# → tự fallback nhóm ĐẦU TIÊN có sẵn (in INFO rõ ràng), để test không đứt ở môi trường khác.
SITE_GROUP_CODE = os.environ.get("POSWEB_TEST_SITE_GROUP", "2018")

# Nguồn mã sản phẩm THẬT (ItemNo + Uom) để thêm dòng Sản phẩm mua/khuyến mãi. Đọc từ file JSON
# (mặc định test_products.json cạnh script; override qua POSWEB_TEST_PRODUCTS_FILE). Người dùng tự
# điền mã thật có trong dbo.Item vào file này → test chọn NGẪU NHIÊN. List rỗng/không hợp lệ →
# fallback placeholder + WARN (publish có thể không ra dữ liệu thật).
PRODUCTS_FILE = Path(os.environ.get(
    "POSWEB_TEST_PRODUCTS_FILE", str(Path(__file__).parent / "test_products.json")))


def _load_products() -> list:
    try:
        raw = json.loads(PRODUCTS_FILE.read_text(encoding="utf-8"))
    except FileNotFoundError:
        print(f"WARN: không thấy file cấu hình sản phẩm {PRODUCTS_FILE} — fallback mã placeholder")
        return []
    except Exception as ex:
        print(f"WARN: đọc {PRODUCTS_FILE} lỗi ({ex}) — fallback mã placeholder")
        return []
    items = []
    for p in (raw.get("products") or []):
        code = str(p.get("itemNo", "")).strip()
        uom = str(p.get("uom", "")).strip()
        if code and not code.upper().startswith("REPLACE_ME"):
            items.append({"itemNo": code, "uom": uom})
    if not items:
        print(f"WARN: {PRODUCTS_FILE} chưa có mã sản phẩm hợp lệ (còn REPLACE_ME?) — fallback placeholder")
    else:
        print(f"INFO: nạp {len(items)} mã sản phẩm thật từ {PRODUCTS_FILE.name}")
    return items


PRODUCTS = _load_products()
_rng = random.Random(int(time.time()))
_placeholder_seq = 0


def random_product() -> dict:
    """Trả 1 mã sản phẩm THẬT ngẫu nhiên từ cấu hình; nếu chưa cấu hình → placeholder (uom rỗng)."""
    global _placeholder_seq
    if PRODUCTS:
        return _rng.choice(PRODUCTS)
    _placeholder_seq += 1
    return {"itemNo": f"TESTSKU{_placeholder_seq:03d}", "uom": ""}


SETUP_URL = f"{BASE_URL}/promotion/setup"

# Chuỗi định danh duy nhất mỗi lần chạy — tránh trùng dữ liệu giữa các lần chạy, dễ lọc/dọn tay.
RUN_STAMP = str(int(time.time()))
TEST_DESC = f"AUTOTEST CTKM {RUN_STAMP}"

ok = True
_shot_seq = 0


def check(name: str, passed: bool, detail: str = ""):
    global ok
    ok = ok and passed
    status = "PASS" if passed else "FAIL"
    print(f"RESULT: {status} - {name}" + (f" ({detail})" if detail else ""))


def slugify(name: str) -> str:
    """Loại bỏ ký tự Windows cấm dùng trong tên file (< > : " / \\ | ? *) và khoảng trắng."""
    safe = name.lower().replace(" ", "_")
    return re.sub(r'[<>:"/\\|?*]', "", safe)


def shot(page, name: str):
    global _shot_seq
    _shot_seq += 1
    path = ARTIFACT_DIR / f"promotion_setup_{_shot_seq:02d}_{slugify(name)}.png"
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
    """Banner đỏ (Severity.Error) khi SP chưa deploy / lỗi tải form/list. Trang này có thể hiện
    MudAlert Severity.Info (gợi ý chọn Loại CTKM khi OfferType rỗng) — KHÔNG được coi là lỗi, chỉ
    lọc theo class chứa 'error'."""
    alerts = page.locator(".mud-alert")
    for i in range(alerts.count()):
        cls = (alerts.nth(i).get_attribute("class") or "").lower()
        if "error" in cls:
            msg = alerts.nth(i).locator(".mud-alert-message")
            return msg.first.inner_text() if msg.count() > 0 else alerts.nth(i).inner_text()
    return None


def snackbar_has_text(page, text: str, timeout=8000) -> bool:
    try:
        page.get_by_text(text, exact=False).first.wait_for(state="visible", timeout=timeout)
        return True
    except Exception:
        return False


def goto_list(page):
    page.goto(SETUP_URL)
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(300)


def open_new_ctkm(page):
    """Bấm 'Thêm CTKM' từ list mode để mở editor mode (KHÔNG đổi URL — cùng component)."""
    page.get_by_role("button", name="Thêm CTKM").click()
    page.wait_for_timeout(400)


def open_select(page, label: str):
    """Mở popover của MudSelect theo Label. get_by_label() trúng input nội bộ `type='hidden'`
    (hoặc collapsed) của MudSelect — KHÔNG click được (xác nhận qua dump DOM thật: input thật giữ
    aria-label nhưng ẩn, phần tử hiển thị được là div sibling không có aria-label riêng) — phải
    click vào ancestor `.mud-input-control` (bọc ngoài input + fieldset) để mở popover."""
    hidden_input = page.get_by_label(label, exact=True).first
    control = hidden_input.locator("xpath=ancestor::div[contains(@class,'mud-input-control')][1]")
    control.click()
    page.wait_for_timeout(350)
    return control


def pick_first_select_option(page, label: str) -> str:
    """Mở MudSelect theo Label, click item ĐẦU TIÊN trong popover (`role="option"`, xác nhận qua
    dump DOM thật — MudBlazor 9.5 render `<div role="listbox"><div role="option">...`, item đầu
    luôn `tabindex="0"` sẵn). KHÔNG hardcode giá trị vì "Hình thức bán hàng"/"Loại CTKM" phụ thuộc
    dữ liệu DB, không đảm bảo tồn tại mã cụ thể nào trong mọi môi trường."""
    open_select(page, label)
    option = page.get_by_role("option").first
    option.wait_for(state="visible", timeout=4000)
    text = option.inner_text()
    option.click()
    page.wait_for_timeout(300)
    return text


def select_option_containing(page, label: str, substr: str) -> str | None:
    """Mở MudSelect, tìm option có text chứa substr (dùng cho case conditional theo mã ZB cụ thể
    — KHÔNG đảm bảo tồn tại). Trả về text option đã chọn, hoặc None nếu không tìm thấy (đóng lại
    dropdown bằng Escape, không chọn gì)."""
    open_select(page, label)
    option = page.locator('[role="option"]', has_text=re.compile(re.escape(substr), re.IGNORECASE))
    if option.count() == 0:
        page.keyboard.press("Escape")
        page.wait_for_timeout(150)
        return None
    text = option.first.inner_text()
    option.first.click()
    page.wait_for_timeout(300)
    return text


def pick_offer_type_strict(page):
    """Chọn 'Loại CTKM' cho LUỒNG CHÍNH (test sâu). Nếu `OFFER_TYPE_FILTER` có set (biến môi
    trường `POSWEB_TEST_OFFER_TYPES`), BẮT BUỘC chọn đúng item đầu tiên trong filter — không khớp
    → trả None (caller dừng sớm, KHÔNG âm thầm fallback, vì user đã chỉ định rõ muốn test Loại
    nào). Không set filter → giữ hành vi cũ: lấy item đầu tiên có sẵn trong dropdown."""
    if OFFER_TYPE_FILTER:
        return select_option_containing(page, "Loại CTKM", OFFER_TYPE_FILTER[0])
    return pick_first_select_option(page, "Loại CTKM")


def pick_offer_type_soft(page):
    """Chọn 'Loại CTKM' cho case NEGATIVE (CTKM-03/04/22) — cố gắng dùng đúng Loại user đã filter
    (test kỹ Loại đó luôn cả ở validate lỗi) nhưng KHÔNG dừng script nếu không khớp — case negative
    không phụ thuộc Loại cụ thể, fallback sang item đầu tiên là đủ, tránh dừng sớm oan."""
    if OFFER_TYPE_FILTER:
        text = select_option_containing(page, "Loại CTKM", OFFER_TYPE_FILTER[0])
        if text:
            return text
    return pick_first_select_option(page, "Loại CTKM")


def pick_date_offset(page, label: str, days_offset: int) -> bool:
    """Tổng quát hoá pick_date_today của smoke_coupon_issue.py: mở MudDatePicker theo Label, điều
    hướng tới đúng tháng (so khớp header 'Tháng M năm YYYY'), bấm ô ngày = hôm nay + days_offset.
    Điều hướng CẢ 2 CHIỀU (Previous/Next month) — khác bản gốc chỉ lùi. Loop-guard 4 vòng, đủ cho
    offset ≤ 30 ngày dùng trong script này."""
    target = date.today() + timedelta(days=days_offset)
    target_header = f"Tháng {target.month} năm {target.year}"

    page.get_by_label(label, exact=True).click()
    page.wait_for_timeout(400)
    header = page.locator(".mud-picker-calendar-header-transition p")
    for _ in range(4):
        if header.count() > 0 and header.first.inner_text().strip() == target_header:
            break
        current_text = header.first.inner_text().strip() if header.count() > 0 else ""
        m = re.match(r"Tháng (\d+) năm (\d+)", current_text)
        going_forward = True
        if m:
            cur_month, cur_year = int(m.group(1)), int(m.group(2))
            going_forward = (target.year, target.month) > (cur_year, cur_month)
        btn = page.locator(f'button[aria-label*="{"Next" if going_forward else "Previous"} month"]')
        if btn.count() == 0:
            return False
        btn.first.click()
        page.wait_for_timeout(300)
    else:
        return False

    day_btn = page.locator(".mud-picker-calendar-day:not(.mud-hidden)",
                            has_text=re.compile(rf"^{target.day}$"))
    if day_btn.count() == 0:
        return False
    day_btn.first.click()
    page.wait_for_timeout(300)
    return True


def find_visible_tab_labels(page) -> set:
    """Đọc text các tab-header hiện có trong MudTabs — dùng để biết 'Sản phẩm mua'/'Sản phẩm
    khuyến mãi' có đang render cho OfferType vừa chọn hay không, quyết định ngay tại runtime."""
    labels = set()
    for want in ["Thông tin chung", "Sản phẩm mua", "Sản phẩm khuyến mãi", "Cửa hàng áp dụng", "Cài đặt nâng cao"]:
        if page.get_by_text(want, exact=True).count() > 0:
            labels.add(want)
    return labels


def add_one_line(page, tab_label: str, product: dict | None = None) -> dict:
    """Chuyển sang tab tab_label, set 'Số lượng dòng'=1 (mặc định 10), bấm 'Thêm dòng (Ctrl+A)',
    điền mã sản phẩm THẬT (ItemNo vào ô 'Nhập barcode...' = context.No, Uom vào ô 'ĐVT' =
    context.UnitOfMeasure — cả 2 là MudTextField text tự do, xem PromotionSetupPage.razor). `product`
    None → tự lấy random_product() từ cấu hình test_products.json. Trả về product đã dùng.
    Số lượng để mặc định=1 (đủ qua validate Quantity>=1)."""
    if product is None:
        product = random_product()
    page.get_by_text(tab_label, exact=True).first.click()
    page.wait_for_timeout(300)
    count_field = page.get_by_label("Số lượng dòng").first
    count_field.fill("1")
    page.get_by_role("button", name="Thêm dòng (Ctrl+A)").click()
    page.wait_for_timeout(300)
    barcode_input = page.get_by_placeholder("Nhập barcode...").first
    barcode_input.fill(product["itemNo"])
    barcode_input.blur()
    page.wait_for_timeout(300)
    # ĐVT (Uom) — MudTextField context.UnitOfMeasure, ô trong cột data-label="ĐVT" của cùng dòng.
    if product.get("uom"):
        row = barcode_input.locator("xpath=ancestor::tr[1]")
        uom_input = row.locator('td[data-label="ĐVT"] input').first
        if uom_input.count() > 0:
            uom_input.fill(product["uom"])
            uom_input.blur()
            page.wait_for_timeout(200)
    return product


def add_site_group(page, code=SITE_GROUP_CODE) -> str | None:
    """Chọn 1 nhóm cửa hàng ở tab 'Cửa hàng áp dụng' qua MudAutocomplete 'Thêm nhóm cửa hàng'.
    Trang nay BẮT BUỘC ≥1 nhóm cửa hàng (CanSave + backend). Ưu tiên đúng `code` (mặc định 2018);
    không tìm thấy → fallback nhóm ĐẦU TIÊN có sẵn (in INFO). Trả text option đã chọn, hoặc None
    nếu môi trường KHÔNG có nhóm cửa hàng nào (caller nên coi là lỗi môi trường)."""
    page.get_by_text("Cửa hàng áp dụng", exact=True).first.click()
    page.wait_for_timeout(300)
    ac = page.get_by_label("Thêm nhóm cửa hàng", exact=True)
    ac.click()
    ac.fill(code)
    page.wait_for_timeout(600)
    # MudAutocomplete 9.5 render option là .mud-list-item trong popover; text = ToStringFunc
    # ("{SiteGroupCode} – {GroupName}") nên match theo code là đủ.
    opt = page.locator(".mud-list-item", has_text=re.compile(re.escape(code), re.IGNORECASE))
    used_fallback = False
    if opt.count() == 0:
        # Fallback: liệt kê tất cả (SearchFunc trả toàn bộ khi text rỗng) rồi lấy nhóm đầu tiên.
        ac.fill("")
        page.wait_for_timeout(600)
        opt = page.locator(".mud-list-item")
        used_fallback = True
    if opt.count() == 0:
        page.keyboard.press("Escape")
        return None
    text = opt.first.inner_text().strip()
    opt.first.click()
    page.wait_for_timeout(400)
    if used_fallback:
        print(f"INFO: nhóm cửa hàng '{code}' không có trong dropdown — fallback nhóm đầu tiên: '{text}'")
    else:
        print(f"INFO: đã chọn nhóm cửa hàng áp dụng = '{text}'")
    return text


def fill_type_specific_requirements(page, min_value="100000"):
    """Điền các field CHỈ bắt buộc với 1 SỐ Loại CTKM cụ thể, tự phát hiện qua UI thực tế đang
    hiện ra cho Loại đang chọn (KHÔNG hardcode theo mã ZB) — dùng chung cho cả luồng chính (CTKM-02)
    và sweep, để 1 Loại BẤT KỲ (không chỉ Loại đầu tiên trong dropdown) đều Lưu tạm được:
      - Tổng bill (IsTotalBill=1): field 'Giá trị tổng bill tối thiểu để hưởng KM' chỉ hiện ở tab
        'Thông tin chung' khi có cờ này — validate bắt buộc > 0.
      - Voucher bị OfferType tự tick+khoá (IsVoucher=1, VoucherCheckboxLocked): validate bắt buộc
        có Voucher từ/đến ngày (rule 15) — không điền sẽ fail SAI LÝ DO (không phải lỗi đang test)."""
    page.get_by_text("Thông tin chung", exact=True).first.click()
    page.wait_for_timeout(250)
    min_value_field = page.get_by_label("Giá trị tổng bill tối thiểu để hưởng KM")
    if min_value_field.count() > 0:
        min_value_field.first.fill(min_value)

    if is_checkbox_checked(page, "Voucher/Coupon"):
        page.get_by_text("Cài đặt nâng cao", exact=True).first.click()
        page.wait_for_timeout(250)
        pick_date_offset(page, "Voucher từ ngày", 0)
        pick_date_offset(page, "Voucher đến ngày", 30)


def set_quantity_on_first_row(page, tab_label: str, value: str) -> bool:
    """Best-effort: đặt 'Số lượng' của dòng ĐẦU TIÊN trong tab_label bằng 0 (test CTKM-22). Cột
    'Số lượng' KHÔNG có Label riêng (MudNumericField trần trong MudTd) — định vị theo vị trí cột
    thứ 5 (Loại/Barcode/Sản phẩm/ĐVT/Số lượng) tính từ dòng <tr> chứa ô Barcode. Trả False nếu
    không định vị được (script sẽ in WARN, không đổ lỗi validate sai do không set được input)."""
    page.get_by_text(tab_label, exact=True).first.click()
    page.wait_for_timeout(300)
    barcode_input = page.get_by_placeholder("Nhập barcode...").first
    if barcode_input.count() == 0:
        return False
    row = barcode_input.locator("xpath=ancestor::tr[1]")
    qty_cell = row.locator("td").nth(4)
    qty_input = qty_cell.locator("input").first
    if qty_input.count() == 0:
        return False
    qty_input.fill(value)
    qty_input.blur()
    page.wait_for_timeout(200)
    return True


def list_all_select_options(page, label: str) -> list:
    """Mở MudSelect theo Label, đọc TOÀN BỘ text các `role="option"` trong popover rồi đóng lại
    bằng Escape (KHÔNG chọn gì) — dùng để liệt kê hết Loại CTKM có sẵn trong dropdown môi trường
    hiện tại, phục vụ sweep test từng OfferType (KHÔNG hardcode danh sách mã ZB)."""
    open_select(page, label)
    options = page.locator('[role="option"]')
    texts = [options.nth(i).inner_text().strip() for i in range(options.count())]
    page.keyboard.press("Escape")
    page.wait_for_timeout(200)
    return texts


def is_checkbox_checked(page, label: str) -> bool:
    """Đọc trạng thái tick hiện tại của 1 MudCheckBox theo Label — dùng để phát hiện checkbox
    'Voucher/Coupon' có bị OfferType tự động tick+khoá không (VoucherCheckboxLocked), quyết định
    có cần điền Voucher từ/đến ngày trước khi Lưu (validate rule 15 — IsVoucher=true bắt buộc có
    2 field này) hay không."""
    try:
        return page.get_by_label(label, exact=True).first.is_checked()
    except Exception:
        return False


def capture_last_alert_text(page) -> str:
    """Best-effort: lấy nguyên văn snackbar/alert cuối cùng đang hiển thị — dùng để log lý do THẬT
    khi 1 OfferType trong sweep không Lưu thành công như kỳ vọng, KHÔNG đoán mò nguyên nhân."""
    for sel in ['.mud-snackbar .mud-alert-message', '.mud-snackbar', '.mud-alert-message']:
        loc = page.locator(sel)
        if loc.count() > 0:
            try:
                return loc.last.inner_text(timeout=1000)
            except Exception:
                continue
    return "(không xác định được nội dung snackbar)"


def confirm_message_box(page, yes_text: str) -> bool:
    """Xử lý MudMessageBox xác nhận (Duyệt CTKM) — scope trong role=dialog để không nhầm với nút
    cùng chữ ngoài dialog."""
    dialog = page.get_by_role("dialog")
    try:
        dialog.first.wait_for(state="visible", timeout=4000)
    except Exception:
        return False
    yes_btn = dialog.get_by_role("button", name=yes_text)
    if yes_btn.count() == 0:
        return False
    yes_btn.first.click()
    page.wait_for_timeout(500)
    return True


def extract_bbynr(message: str) -> str | None:
    m = re.search(r"CTKM\s+(\S+)\s+thành công", message)
    return m.group(1) if m else None


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    if not login(page):
        shot(page, "login_failed")
        browser.close()
        print("SUMMARY: 0/0 passed — KHÔNG THỂ ĐĂNG NHẬP, dừng sớm")
        sys.exit(1)

    # ── CTKM-01: mở trang, verify nút "Thêm CTKM", banner lỗi nếu SP chưa deploy ───
    goto_list(page)
    err = form_error_banner(page)
    if err:
        check("CTKM-01 form/list load được (không có banner lỗi SP)", False, err)
        shot(page, "list_error_banner")
        browser.close()
        print("SUMMARY: dừng sớm — SP SetupPromotion_Save.sql/ApproveAndStatus.sql có thể chưa deploy")
        sys.exit(1)

    add_btn = page.get_by_role("button", name="Thêm CTKM")
    check("CTKM-01 nút 'Thêm CTKM' hiển thị trên list", add_btn.count() > 0)

    # ── CTKM-02: tạo CTKM tối thiểu hợp lệ, thích ứng theo dữ liệu môi trường ──────
    open_new_ctkm(page)
    page.get_by_label("Tên CTKM", exact=True).fill(TEST_DESC)

    sales_type_text = pick_first_select_option(page, "Hình thức bán hàng")
    print(f"INFO: đã chọn Hình thức bán hàng = '{sales_type_text}'")
    offer_type_text = pick_offer_type_strict(page)
    if OFFER_TYPE_FILTER and not offer_type_text:
        check(f"CTKM-02 chọn được Loại CTKM theo yêu cầu (POSWEB_TEST_OFFER_TYPES={OFFER_TYPE_FILTER[0]})",
              False, "không tồn tại trong dropdown môi trường này — dừng sớm, KHÔNG tự chọn Loại khác")
        shot(page, "offer_type_filter_not_found")
        browser.close()
        print(f"SUMMARY: dừng sớm — Loại CTKM '{OFFER_TYPE_FILTER[0]}' không có trong dropdown môi trường này")
        sys.exit(1)
    print(f"INFO: đã chọn Loại CTKM{' (theo POSWEB_TEST_OFFER_TYPES)' if OFFER_TYPE_FILTER else ''} = '{offer_type_text}'")
    page.wait_for_timeout(300)

    from_ok = pick_date_offset(page, "Từ ngày", 0)
    to_ok = pick_date_offset(page, "Đến ngày", 30)
    check("CTKM-02 chọn được Từ ngày/Đến ngày qua calendar picker", from_ok and to_ok,
          f"from_ok={from_ok} to_ok={to_ok}")

    # Thích ứng field CHỈ bắt buộc với 1 số Loại (MinValue nếu tổng bill, Voucher từ/đến ngày nếu
    # Voucher bị khoá true) — BẮT BUỘC gọi trước khi set Nâng cao/Buy/Get, vì user có thể filter
    # bất kỳ Loại nào qua POSWEB_TEST_OFFER_TYPES (không chỉ Loại đầu tiên "an toàn" ZB02).
    fill_type_specific_requirements(page)

    # Nâng cao: set Độ ưu tiên + Limit by customer để verify round-trip sau khi mở lại.
    page.get_by_text("Cài đặt nâng cao", exact=True).first.click()
    page.wait_for_timeout(300)
    page.get_by_label("Độ ưu tiên (1–10)", exact=True).fill("3")
    page.get_by_label("Giới hạn KH / Limit by customer", exact=True).fill("5")

    visible_tabs = find_visible_tab_labels(page)
    print(f"INFO: tab hiện ra cho Loại CTKM đã chọn: {sorted(visible_tabs)}")
    if "Sản phẩm mua" in visible_tabs:
        p_buy = add_one_line(page, "Sản phẩm mua")
        print(f"INFO: dòng Sản phẩm mua = ItemNo '{p_buy['itemNo']}' Uom '{p_buy.get('uom','')}'")
    if "Sản phẩm khuyến mãi" in visible_tabs:
        p_get = add_one_line(page, "Sản phẩm khuyến mãi")
        print(f"INFO: dòng Sản phẩm khuyến mãi = ItemNo '{p_get['itemNo']}' Uom '{p_get.get('uom','')}'")

    # BẮT BUỘC chọn nhóm cửa hàng áp dụng — không có site thì CanSave disable 'Lưu tạm' và backend
    # cũng chặn; đồng thời publish lúc Duyệt mới có dòng sang Offer*/OfferSite.
    site_text = add_site_group(page)
    if not site_text:
        check("CTKM-02 chọn được nhóm cửa hàng áp dụng", False,
              "môi trường KHÔNG có nhóm cửa hàng nào (dbo.SetupGroupSite) — dừng sớm")
        shot(page, "no_site_group")
        browser.close()
        print("SUMMARY: dừng sớm — không có nhóm cửa hàng nào trong dbo.SetupGroupSite")
        sys.exit(1)

    shot(page, "positive_form_filled")

    save_btn = page.get_by_role("button", name="Lưu tạm")
    check("CTKM-02 nút 'Lưu tạm' có thể bấm (không bị disable do CanSave)", save_btn.is_enabled())
    save_btn.click()
    saved_ok = snackbar_has_text(page, "Lưu CTKM")
    check("CTKM-02 Lưu tạm CTKM tối thiểu hợp lệ thành công", saved_ok,
          "kỳ vọng snackbar chứa 'Lưu CTKM ... thành công'")
    shot(page, "positive_after_save")

    bbynr = None
    if saved_ok:
        no_field = page.get_by_label("Mã CTKM", exact=True)
        if no_field.count() > 0:
            bbynr = no_field.first.input_value()
        print(f"INFO: CTKM vừa tạo có mã BBYNR = '{bbynr}'")

    # ── CTKM-10 (đọc lại) + round-trip CTKM-11/23 ─────────────────────────────────
    if bbynr:
        page.get_by_role("button", name="Quay lại").click()
        page.wait_for_timeout(400)
        page.get_by_label("Mã CTKM", exact=True).fill(bbynr)
        page.get_by_role("button", name="Tìm").click()
        page.wait_for_timeout(800)
        row = page.locator(".mud-table-row", has_text=bbynr)
        check("CTKM vừa tạo xuất hiện trong list (filter theo Mã CTKM)", row.count() >= 1,
              f"số dòng khớp={row.count()}")
        if row.count() >= 1:
            row.first.locator("button").first.click()
            page.wait_for_timeout(500)
            desc_val = page.get_by_label("Tên CTKM", exact=True).input_value()
            check("CTKM-10 mở lại: Tên CTKM đọc lại đúng", desc_val == TEST_DESC,
                  f"expected='{TEST_DESC}' actual='{desc_val}'")

            page.get_by_text("Cài đặt nâng cao", exact=True).first.click()
            page.wait_for_timeout(300)
            priority_val = page.get_by_label("Độ ưu tiên (1–10)", exact=True).input_value()
            limit_val = page.get_by_label("Giới hạn KH / Limit by customer", exact=True).input_value()
            # "Giới hạn KH/Limit by customer" là MudNumericField<decimal> — hiển thị theo culture
            # vi-VN (dấu phẩy = decimal separator, KHÔNG phải nghìn) — "5,000" nghĩa là 5.000, tức
            # giá trị 5 với 3 số lẻ hiển thị, KHÔNG phải 5000 (xác nhận qua chạy thật 2026-07-16).
            try:
                limit_num = float(limit_val.strip().replace(",", "."))
            except ValueError:
                limit_num = None
            check("round-trip Độ ưu tiên/Limit by customer (CTKM-11/23)",
                  priority_val.strip() == "3" and limit_num == 5.0,
                  f"priority='{priority_val}' limit='{limit_val}' (parsed={limit_num})")
            shot(page, "reopen_roundtrip")

        # ── CTKM-13: Duyệt CTKM ────────────────────────────────────────────────
        approve_btn = page.get_by_role("button", name="Duyệt CTKM")
        if approve_btn.count() > 0:
            approve_btn.first.click()
            dialog_confirmed = confirm_message_box(page, "Duyệt")
            check("CTKM-13 dialog xác nhận Duyệt xử lý được", dialog_confirmed)
            # BẮT BUỘC dùng message CHÍNH XÁC kèm bbynr — chuỗi "Duyệt CTKM" đơn thuần KHÔNG đủ
            # phân biệt vì đó cũng chính là TEXT CỦA NÚT bấm (luôn hiển thị trên form), match nhầm
            # nút sẽ tạo false positive (đã xác nhận qua chạy thật 2026-07-16).
            expected_approve_msg = f"Duyệt CTKM {bbynr} thành công"
            approve_ok = snackbar_has_text(page, expected_approve_msg, timeout=6000) if dialog_confirmed else False
            approve_detail = f"kỳ vọng snackbar '{expected_approve_msg}'"
            if not approve_ok:
                backend_error = snackbar_has_text(page, "Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT.", timeout=1500)
                if backend_error:
                    approve_detail = ("BACKEND BUG: snackbar 'Lỗi hệ thống...' — xem "
                                       "D:\\ROOT\\Logs\\POS.Web\\Exception\\log-*.txt để xác nhận "
                                       "nguyên nhân thật (KHÔNG phải lỗi test script)")
            check("CTKM-13 Duyệt CTKM thành công (publish sang Offer*)", approve_ok, approve_detail)
            shot(page, "after_approve")

            # ── CTKM-14: khóa readonly sau khi duyệt ───────────────────────────
            page.get_by_role("button", name="Quay lại").click()
            page.wait_for_timeout(400)
            page.get_by_label("Mã CTKM", exact=True).fill(bbynr)
            page.get_by_role("button", name="Tìm").click()
            page.wait_for_timeout(800)
            row2 = page.locator(".mud-table-row", has_text=bbynr)
            check("CTKM vừa xử lý vẫn xuất hiện trong list sau Duyệt (filter theo Mã CTKM)",
                  row2.count() >= 1, f"số dòng khớp={row2.count()}")
            if row2.count() >= 1:
                row2.first.locator("button").first.click()
                page.wait_for_timeout(500)
                readonly_chip = page.get_by_text("Đã duyệt — chỉ xem", exact=False)
                check("CTKM-14 chip 'Đã duyệt — chỉ xem' hiển thị", readonly_chip.count() > 0)
                save_after_approve = page.get_by_role("button", name="Lưu tạm")
                check("CTKM-14 nút 'Lưu tạm' KHÔNG hiển thị sau khi duyệt", save_after_approve.count() == 0)
                shot(page, "readonly_after_approve")
        else:
            check("CTKM-13 nút 'Duyệt CTKM' hiển thị sau khi Lưu tạm", False,
                  "không thấy nút — không thể test Duyệt")
    else:
        check("CTKM-10/13/14 (đọc lại + Duyệt)", False, "không lấy được BBYNR từ bước Lưu tạm")

    # ── Negative cases (mỗi case trên 1 CTKM mới) ─────────────────────────────────
    def run_negative_case(name, expected_message, setup):
        goto_list(page)
        err = form_error_banner(page)
        if err:
            check(name, False, f"banner lỗi list: {err}")
            return
        open_new_ctkm(page)
        setup(page)
        save_btn = page.get_by_role("button", name="Lưu tạm")
        # `CanSave` (client-side gate, xem PromotionSetupPage.razor) disable "Lưu tạm" khi
        # Description/SalesType/OfferType(mới)/Từ ngày/Đến ngày rỗng — nếu case đang test chính là
        # 1 trong các field đó rỗng, nút KHÔNG click được (giống TC-N06 của Coupon) — SKIP, KHÔNG
        # tính FAIL, vì không có gì để reproduce lỗi validate SERVER (không bao giờ round-trip lên).
        if not save_btn.is_enabled():
            print(f"INFO: SKIP {name} — nút 'Lưu tạm' bị disable bởi CanSave (client-side gate), "
                  "KHÔNG reachable qua UI để test validate server-side")
            return
        save_btn.click()
        found = snackbar_has_text(page, expected_message)
        check(name, found, f"kỳ vọng snackbar chứa: '{expected_message}'")
        shot(page, name)

    def _setup_ctkm03(p):
        # Bỏ trống Tên CTKM — điền các field khác để cô lập đúng validate đang test.
        pick_first_select_option(p, "Hình thức bán hàng")
        pick_offer_type_soft(p)
        pick_date_offset(p, "Từ ngày", 0)
        pick_date_offset(p, "Đến ngày", 10)

    run_negative_case("CTKM-03 bỏ trống Tên CTKM", "Vui lòng nhập tên chương trình khuyến mãi", _setup_ctkm03)

    def _setup_ctkm04(p):
        p.get_by_label("Tên CTKM", exact=True).fill(f"{TEST_DESC} CTKM04")
        pick_first_select_option(p, "Hình thức bán hàng")
        pick_offer_type_soft(p)
        pick_date_offset(p, "Từ ngày", 5)
        date_changed = pick_date_offset(p, "Đến ngày", 1)  # Đến ngày < Từ ngày
        if not date_changed:
            print("WARN: CTKM-04 không đổi được 'Đến ngày' qua calendar picker — kết quả có thể sai")
        # Cần site để CanSave enable (nay bắt buộc ≥1 nhóm CH) → mới bấm được Lưu tạm để tái hiện
        # lỗi validate NGÀY ở server. Rule ngày (267) chạy TRƯỚC rule site (313) nên vẫn ra đúng
        # message ngày, không bị "thiếu nhóm cửa hàng" che.
        add_site_group(p)

    run_negative_case("CTKM-04 Đến ngày < Từ ngày",
                       "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", _setup_ctkm04)

    def _setup_ctkm22(p):
        p.get_by_label("Tên CTKM", exact=True).fill(f"{TEST_DESC} CTKM22")
        pick_first_select_option(p, "Hình thức bán hàng")
        offer_text = pick_offer_type_soft(p)
        pick_date_offset(p, "Từ ngày", 0)
        pick_date_offset(p, "Đến ngày", 10)
        # Nếu Loại đang test (vd bị filter tới ZB06 — tổng bill) cần MinValue/Voucher date, phải
        # điền TRƯỚC — nếu không, rule "MinValue>0"/"thiếu Voucher date" (chạy TRƯỚC rule Quantity
        # trong SaveSetupAsync) sẽ chặn với message KHÁC, che mất đúng lỗi Quantity đang cô lập.
        fill_type_specific_requirements(p)
        # Cần site để CanSave enable (rule Quantity ở server chạy TRƯỚC rule site nên message
        # Quantity vẫn hiện đúng, không bị che).
        add_site_group(p)
        tabs = find_visible_tab_labels(p)
        target_tab = "Sản phẩm khuyến mãi" if "Sản phẩm khuyến mãi" in tabs else (
            "Sản phẩm mua" if "Sản phẩm mua" in tabs else None)
        if target_tab is None:
            print(f"WARN: CTKM-22 Loại CTKM '{offer_text}' không có tab Buy/Get để đặt Quantity=0")
            return
        # PHẢI thêm dòng hợp lệ ở TAB KHÁC (nếu cũng hiện/bắt buộc) TRƯỚC — nếu không, rule "cần
        # ít nhất 1 dòng Sản phẩm mua/khuyến mãi" (chạy TRƯỚC rule Quantity trong SaveSetupAsync)
        # sẽ chặn với message KHÁC, che mất chính xác lỗi Quantity đang muốn cô lập (đã xác nhận
        # qua chạy thật 2026-07-16 — Loại đầu tiên trong dropdown yêu cầu CẢ Buy VÀ Get).
        other_tab = "Sản phẩm mua" if target_tab == "Sản phẩm khuyến mãi" else "Sản phẩm khuyến mãi"
        if other_tab in tabs:
            add_one_line(p, other_tab)
        add_one_line(p, target_tab)
        qty_set = set_quantity_on_first_row(p, target_tab, "0")
        if not qty_set:
            print("WARN: CTKM-22 không định vị được ô 'Số lượng' của dòng — kết quả có thể sai")

    run_negative_case("CTKM-22 Số lượng dòng = 0", "Số lượng của mỗi dòng sản phẩm phải ≥ 1", _setup_ctkm22)

    # ── Conditional: CTKM-18 (ẩn tab Buy cho ZB06/ZB13), CTKM-19 (ZB14/ZB15 vẫn có Buy) ──
    def _try_conditional_offer_type(zb_codes, expect_buy_visible, case_name):
        goto_list(page)
        if form_error_banner(page):
            return
        open_new_ctkm(page)
        matched = None
        for code in zb_codes:
            matched = select_option_containing(page, "Loại CTKM", code)
            if matched:
                break
        if not matched:
            print(f"INFO: SKIP {case_name} (không có OfferType {'/'.join(zb_codes)} trong dropdown môi trường này)")
            return
        page.wait_for_timeout(300)
        tabs = find_visible_tab_labels(page)
        buy_visible = "Sản phẩm mua" in tabs
        check(case_name, buy_visible == expect_buy_visible,
              f"OfferType='{matched}', tab Buy visible={buy_visible}, kỳ vọng={expect_buy_visible}")
        shot(page, case_name)

    # CTKM-18/19 test đúng 2 nhóm mã cụ thể (ZB06/ZB13 vs ZB14/ZB15) — khi user đã filter 1 Loại cụ
    # thể không liên quan (vd chỉ muốn test ZB03), 2 check này không có giá trị thông tin mới cho
    # yêu cầu hiện tại → SKIP để đỡ tốn 1 lượt mở trang mỗi cái, KHÔNG chạy mặc định "cho chắc".
    run_ctkm18 = (not OFFER_TYPE_FILTER) or any(c in ("ZB06", "ZB13") for c in OFFER_TYPE_FILTER)
    run_ctkm19 = (not OFFER_TYPE_FILTER) or any(c in ("ZB14", "ZB15") for c in OFFER_TYPE_FILTER)
    if run_ctkm18:
        _try_conditional_offer_type(["ZB06", "ZB13"], False, "CTKM-18 ẩn tab Buy cho ZB06/ZB13")
    else:
        print("INFO: SKIP CTKM-18 — POSWEB_TEST_OFFER_TYPES không liên quan ZB06/ZB13")
    if run_ctkm19:
        _try_conditional_offer_type(["ZB14", "ZB15"], True, "CTKM-19 ZB14/ZB15 vẫn có tab Buy")
    else:
        print("INFO: SKIP CTKM-19 — POSWEB_TEST_OFFER_TYPES không liên quan ZB14/ZB15")

    # ── Sweep Loại CTKM ──────────────────────────────────────────────────────────────
    # KHÔNG set POSWEB_TEST_OFFER_TYPES: sweep TẤT CẢ Loại có trong dropdown (hành vi mặc định cũ)
    # — mỗi Loại có tổ hợp yêu cầu field khác nhau (Buy bắt buộc/ẩn/tuỳ chọn, Get bắt buộc,
    # MinValue>0 khi tổng bill, Voucher từ/đến ngày khi IsVoucher bị OfferType tự khoá true) nên
    # 1 Loại pass không đảm bảo Loại khác cũng pass. CÓ set: chỉ sweep NÔNG các Loại CÒN LẠI trong
    # filter (bỏ Loại đầu tiên — đã được luồng chính test SÂU ở CTKM-02 rồi, tránh test trùng) —
    # đây là cách để user CHỈ test 1 Loại cụ thể mà không phải chờ sweep hết toàn bộ dropdown.
    goto_list(page)
    if form_error_banner(page):
        print("INFO: SKIP sweep Loại CTKM — banner lỗi list")
    else:
        open_new_ctkm(page)
        if OFFER_TYPE_FILTER:
            dropdown_all = list_all_select_options(page, "Loại CTKM")
            by_code = {t.split("-", 1)[0].strip().upper(): t for t in dropdown_all}
            remaining_codes = OFFER_TYPE_FILTER[1:]  # bỏ item đầu — đã test sâu ở luồng chính
            all_offer_types = []
            for code in remaining_codes:
                if code in by_code:
                    all_offer_types.append(by_code[code])
                else:
                    print(f"INFO: SKIP sweep '{code}' (POSWEB_TEST_OFFER_TYPES) — không có trong "
                          "dropdown môi trường này")
            if not all_offer_types:
                print("INFO: Sweep bỏ qua — POSWEB_TEST_OFFER_TYPES chỉ có 1 Loại (đã test sâu ở "
                      "luồng chính CTKM-02) hoặc không còn Loại nào khác cần sweep")
        else:
            all_offer_types = list_all_select_options(page, "Loại CTKM")
            print(f"INFO: Sweep {len(all_offer_types)} Loại CTKM có sẵn trong dropdown môi trường này: {all_offer_types}")

        for label in all_offer_types:
            goto_list(page)
            if form_error_banner(page):
                continue
            open_new_ctkm(page)
            code = label.split("-", 1)[0].strip()
            case_name = f"Sweep OfferType '{label}': Lưu tạm thành công"

            page.get_by_label("Tên CTKM", exact=True).fill(f"{TEST_DESC} SWEEP {code}")
            pick_first_select_option(page, "Hình thức bán hàng")
            matched = select_option_containing(page, "Loại CTKM", code)
            if not matched:
                check(case_name, False, "không chọn lại được đúng option vừa liệt kê (nội bộ)")
                continue
            pick_date_offset(page, "Từ ngày", 0)
            pick_date_offset(page, "Đến ngày", 30)

            fill_type_specific_requirements(page)

            tabs = find_visible_tab_labels(page)
            if "Sản phẩm mua" in tabs:
                add_one_line(page, "Sản phẩm mua")
            if "Sản phẩm khuyến mãi" in tabs:
                add_one_line(page, "Sản phẩm khuyến mãi")

            add_site_group(page)  # bắt buộc ≥1 nhóm cửa hàng để Lưu tạm được

            save_btn = page.get_by_role("button", name="Lưu tạm")
            if not save_btn.is_enabled():
                print(f"INFO: SKIP sweep '{label}' — nút 'Lưu tạm' bị disable bởi CanSave")
                continue
            save_btn.click()
            ok_saved = snackbar_has_text(page, "Lưu CTKM", timeout=6000)
            detail = "" if ok_saved else f"snackbar thật: '{capture_last_alert_text(page)}'"
            check(case_name, ok_saved, detail)
            shot(page, f"sweep_{code}")

    browser.close()

if bbynr:
    print(f"INFO: CTKM test đã tạo THẬT trong DB — BBYNR={bbynr}, Description='{TEST_DESC}', "
          f"đã chọn nhóm cửa hàng '{SITE_GROUP_CODE}' (hoặc fallback nhóm đầu tiên), ĐÃ DUYỆT (nếu "
          f"CTKM-13 pass) → publish sang OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite. "
          f"KHÔNG có nút Xóa trên trang này.")

print("SUMMARY: " + ("ALL PASSED" if ok else "SOME FAILED"))
sys.exit(0 if ok else 1)

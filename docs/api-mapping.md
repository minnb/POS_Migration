# API Inventory & Tracking — POS Backend

> **NGUYÊN TẮC:** Route và Response format của API mới phải giữ nguyên **100%** như API cũ.
> File này dùng để **theo dõi tiến độ convert**, không phải để định nghĩa route mới.

---

## Hướng dẫn đọc bảng

| Ký hiệu | Ý nghĩa |
|---|---|
| ⬜ Chưa làm | Endpoint chưa được convert |
| 🔄 Đang làm | Đang trong quá trình convert |
| ✅ Xong | Đã convert, đã test, route và response khớp API cũ |
| ❌ Bỏ qua | Không convert module này |

> **Cột "Endpoint"** = route của API cũ = route của API mới (không có gì thay đổi).
> Sau khi chạy `/analyze-legacy`, Claude sẽ điền đầy đủ danh sách endpoint vào đây.

---

## Module: Session (CA / End-of-Day)

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | *(chờ phân tích code cũ)* | ⬜ | |
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Common

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | *(chờ phân tích code cũ)* | ⬜ | |
| GET | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Payment — Voucher Nội bộ

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | *(chờ phân tích code cũ)* | ⬜ | |
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Payment — GotIt

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Payment — Urbox

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Loyalty

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | *(chờ phân tích code cũ)* | ⬜ | |
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Master Data

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | *(chờ phân tích code cũ)* | ⬜ | |

---

## Module: Offer

| Method | Endpoint (giữ nguyên) | Trạng thái | Ghi chú |
|---|---|---|---|
| GET | *(chờ phân tích code cũ)* | ⬜ | |
| POST | *(chờ phân tích code cũ)* | ⬜ | |

---

## Ghi chú quan trọng

Bảng này sẽ được **Claude tự điền đầy đủ** sau khi chạy prompt trong
`.claude/commands/analyze-legacy.md`. Lúc đó mỗi dòng sẽ có route thực tế
lấy từ code cũ, không phải ước đoán.

**Tuyệt đối không có cột "Endpoint mới"** — vì endpoint không thay đổi.
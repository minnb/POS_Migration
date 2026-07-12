# Unit Test — 3 mẫu cơ bản nhất (POS_Migration)

> Tài liệu tham chiếu nhanh cho dev khi viết unit test mới. Đây là **doc**, không phải test thật chạy
> CI — test thật đặt tại `tests/POS.UnitTests/` (xUnit + Moq + FluentAssertions, `net10.0`).

## 3 mẫu — dùng khi nào

| # | Mẫu | Dùng khi | File |
|---|---|---|---|
| 1 | Pure logic test | Test 1 method/class `static`, không I/O, không DI — input xác định → output xác định | [`01-pure-logic-test.md`](./01-pure-logic-test.md) |
| 2 | Service delegation test (Moq) | Test `{Name}Service` (Application, thin wrapper) delegate đúng sang `I{Name}AppService` (Infrastructure) — pattern AppService 3 lớp | [`02-service-delegation-test.md`](./02-service-delegation-test.md) |
| 3 | Controller test (Moq + FluentAssertions) | Test Controller trả đúng HTTP status/shape theo từng nhánh logic (thành công / thất bại / exception) | [`03-controller-test.md`](./03-controller-test.md) |

Thứ tự trên cũng là **thứ tự ưu tiên khi viết test mới**: tách được logic thuần thì viết mẫu 1 trước
(rẻ, ổn định nhất) — chỉ dùng mẫu 2/3 khi thật sự cần verify hành vi qua DI/HTTP layer.

## Vị trí đặt test thật

```
tests/POS.UnitTests/{Domain}/{Name}Tests.cs
```

**KHÔNG** đặt vào `tests/POS.ContractTests/` — project đó chỉ dành riêng cho 3 vành đai bảo vệ contract
JSON/DI/exception-middleware (xem `.claude/rules/backend-api-rules.md`), không phải nơi chứa unit test
nghiệp vụ thông thường.

## Chạy test

```bash
dotnet test tests/POS.UnitTests
dotnet test tests/POS.UnitTests --filter FullyQualifiedName~{TenClassTest}
```

## Nguyên tắc chung cả 3 mẫu

- Arrange – Act – Assert rõ ràng, tách biệt từng phần.
- Tên method: `Method_condition_expectedResult`.
- Mock đúng **interface ở đúng tầng** Controller/Service đang inject — không mock nhầm tầng dưới.
- Không thêm production code khi viết test; không đổi field JSON của DTO response (contract 5.000
  máy POS bất biến).

## Muốn sinh test đầy đủ cho luồng Payment (GotIT/Urbox)?

3 mẫu ở đây chỉ là **template tối giản** để tham khảo nhanh. Khi cần sinh bộ test đầy đủ, đúng chuẩn
cho luồng Payment hoặc 1 service Application mới, dùng skill:

> **`.claude/skills/payment-test-generator/SKILL.md`** — đóng gói quy trình sinh test đầy đủ (bảng
> target, cách dựng `Tuple` return của từng partner, checklist bằng chứng `dotnet test` PASS).

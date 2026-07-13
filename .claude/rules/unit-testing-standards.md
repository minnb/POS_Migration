# Rule: Unit Testing Standards (xUnit + Moq + FluentAssertions)

## 🎯 Context (Khi nào áp dụng)
Khi viết unit test cho service tầng Application (`PaymentController`, `GotITService`/`UrboxService`,
và mọi `I{Name}Service` tương lai) trong `tests/POS.UnitTests`. Đây là **tiêu chuẩn bắt buộc**
(WHAT/WHY). Setup csproj, bản đồ target, code template và quy trình sinh test nằm ở skill
**`payment-test-generator`** (`.claude/skills/payment-test-generator/SKILL.md`).

> Khác `tests/POS.ContractTests` (3 vành đai guardrail — contract/DI/exception middleware) — xem
> `.claude/rules/backend-api-rules.md`. Unit test **KHÔNG** đụng `POS.ContractTests`.

## ✅ DO (Bắt buộc làm)
- **Nguyên tắc Mock (cốt lõi):**
  1. **Test vào seam interface Application** (`IGotITService`/`IUrboxService`...), KHÔNG test thẳng
     Infrastructure AppService, KHÔNG gọi HTTP/DB thật.
  2. **Mock bằng Moq**, assert bằng **FluentAssertions**, framework **xUnit** (đồng bộ toàn repo).
  3. **Không thêm production code, không đổi field JSON** của DTO response (contract 5.000 POS bất
     biến) — test chỉ *đọc* DTO.
  4. **Không phá Clean Architecture** — test là consumer của interface, dependency flow không đổi.
  5. **Tách project** — test đặt ở `tests/POS.UnitTests`, KHÔNG trộn vào `POS.ContractTests`.
- **Quy ước đặt tên & bố cục:**
  - Namespace file-scoped: `namespace POS.UnitTests.Features.{Domain};`.
  - Tên method test: `Method_condition_expectedResult` (vd `ValidateVoucher_gotitFails_returns400`).
  - Bố cục **Arrange–Act–Assert** rõ ràng; mỗi `[Fact]` một hành vi; dùng `[Theory]`+`[InlineData]`
    khi cùng logic khác tham số.
- **Đọc `BaseController`/code thật trước khi assert** — chỉ assert trên shape response đã xác nhận
  (`ObjectResult.StatusCode`...), KHÔNG đoán.
- Bằng chứng bắt buộc trước khi báo "xong": `dotnet test tests/POS.UnitTests` PASS +
  `dotnet test tests/POS.ContractTests` vẫn PASS + build 0 error.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm test thẳng Infrastructure AppService hoặc gọi HTTP/DB thật trong unit test.
- Cấm thêm production code hoặc đổi field JSON DTO chỉ để phục vụ test.
- Cấm trộn unit test vào `tests/POS.ContractTests`.
- Cấm assert shape response theo phỏng đoán khi chưa đọc `BaseController`.

---

> Setup `tests/POS.UnitTests/POS.UnitTests.csproj`, bản đồ target luồng Payment, dựng Tuple return
> của partner, code template, bẫy CS0104 ambiguous: **`.claude/skills/payment-test-generator/SKILL.md`**
> — KHÔNG lặp lại Nguyên tắc Mock/naming ở đây.

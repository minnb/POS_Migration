# Mẫu 3 — Controller Test (Moq + FluentAssertions trên `IActionResult`)

## Khi nào dùng

Test 1 Controller action trả đúng **HTTP status/shape response** theo từng nhánh logic — thành công,
thất bại nghiệp vụ (`success=false`), tham số không hợp lệ, hoặc exception. Đây là lớp test quan trọng
nhất để bắt regression về routing/status code trước khi lộ ra production.

## Nguyên tắc

- Mock đúng **interface Application** mà Controller inject (VD `IGotITService`) — **KHÔNG** mock
  `I{Name}AppService` (Infrastructure), vì Controller không được inject thẳng tầng đó
  (`.claude/rules/architecture-layers.md`).
- **Đọc kỹ `BaseController`** (`OkResult(...)`/`StatusCode(...)` trả kiểu gì) trước khi assert — assert
  đúng shape thật (`ObjectResult.StatusCode`), không đoán.
- Assert bằng FluentAssertions: `.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(...)`.
- Mỗi `[Fact]` phủ đúng 1 nhánh: thành công → 200; `success=false` → 400; partner/tham số không hợp lệ
  → BadRequest; mock ném exception → 500.
- Tên method: `Action_condition_expectedResult` (VD `ValidateVoucher_gotitFails_returns400`).

## Code mẫu (rút gọn từ `PaymentControllerTests` — pattern chuẩn trong
`.claude/skills/payment-test-generator/SKILL.md` §6)

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using POS.Api.Controllers;
using POS.Application.Features.Partner;
using POS.Common.Dtos.PartnerApi;
using Xunit;

namespace POS.UnitTests.Features.Partner;

public class PaymentControllerTests
{
    private static PaymentController BuildSut(
        Mock<IGotITService>? gotit = null, Mock<IUrboxService>? urbox = null)
    {
        gotit ??= new Mock<IGotITService>();
        urbox ??= new Mock<IUrboxService>();
        // Đọc ctor thật của PaymentController/BaseController để biết đủ tham số cần truyền
        // (IKibanaService, IFileLogHelper...) trước khi mock — không đoán signature.
        return new PaymentController(/* kibana */ null!, /* fileLog */ null!,
                                     gotit.Object, urbox.Object);
    }

    [Fact]
    public async Task ValidateVoucher_gotitFails_returns400()
    {
        // Arrange
        var gotit = new Mock<IGotITService>();
        gotit
            .Setup(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tuple.Create(false, "voucher invalid", new List<DataVoucherPartnerResponse>()));
        var sut = BuildSut(gotit: gotit);

        // Act
        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "GOTIT" }, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(400);
        gotit.Verify(
            x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateVoucher_unknownPartner_returnsBadRequest()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "UNKNOWN" }, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }
}
```

## Verify

```bash
dotnet test tests/POS.UnitTests --filter FullyQualifiedName~PaymentControllerTests
```

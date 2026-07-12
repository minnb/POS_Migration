# Mẫu 2 — Service Delegation Test (Moq, tầng Application)

## Khi nào dùng

Test 1 `{Name}Service` ở **POS.Application** — theo pattern AppService 3 lớp bắt buộc của dự án
(`.claude/rules/architecture-layers.md`), Application service chỉ là **thin wrapper** delegate sang
`I{Name}AppService` (Infrastructure), KHÔNG có business logic riêng. Test này xác nhận đúng 1 điều:
service gọi đúng method, đúng tham số, đúng 1 lần, và trả về **nguyên vẹn** kết quả (không transform).

## Nguyên tắc

- Mock đúng **interface Infrastructure** mà service inject (`I{Name}AppService`), KHÔNG mock class cụ
  thể, KHÔNG gọi HTTP/DB thật.
- `Setup(...).ReturnsAsync(expected)` rồi **assert kết quả trả về là chính object đó**
  (`.Should().BeSameAs(expected)` hoặc `Assert.Same`) — chứng minh service không tạo object mới/biến
  đổi dữ liệu, đúng tinh thần "thin wrapper".
- **`Verify(..., Times.Once)`** — đảm bảo delegate đúng 1 lần, không gọi thừa/thiếu.
- ⚠️ **Bẫy hay gặp:** class Application và Infrastructure trong pattern AppService 3 lớp thường
  **trùng tên** (VD `GotITService` tồn tại ở cả `POS.Application.Features.Partner` lẫn
  `POS.Infrastructure.AppServices.Partner`) — import cả 2 namespace sẽ lỗi `CS0104 ambiguous`. Fix:
  alias namespace Application, gọi đích danh qua alias.

## Code mẫu (rút gọn từ `GotITServiceTests` — pattern chuẩn trong
`.claude/skills/payment-test-generator/SKILL.md` §6)

```csharp
using System.Threading;
using FluentAssertions;
using Moq;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Partner;
using Xunit;

// Alias để tránh CS0104 — GotITService tồn tại ở cả 2 namespace (Application vs Infrastructure)
using AppPartner = POS.Application.Features.Partner;

namespace POS.UnitTests.Features.Partner;

public class GotITServiceTests
{
    [Fact]
    public async Task CheckMultiple_delegatesToAppService_exactlyOnceWithSameResult()
    {
        // Arrange
        var appService = new Mock<IGotITAppService>();   // interface Infrastructure
        var request = new CheckVoucherPartnerPOSRequest();
        var expected = Tuple.Create(true, "ok", new List<DataVoucherPartnerResponse>());

        appService
            .Setup(x => x.CheckMultiple(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new AppPartner.GotITService(appService.Object);   // service Application (thin wrapper)

        // Act
        var actual = await sut.CheckMultiple(request, CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(expected);   // KHÔNG transform — trả nguyên vẹn
        appService.Verify(
            x => x.CheckMultiple(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

## Verify

```bash
dotnet test tests/POS.UnitTests --filter FullyQualifiedName~GotITServiceTests
```

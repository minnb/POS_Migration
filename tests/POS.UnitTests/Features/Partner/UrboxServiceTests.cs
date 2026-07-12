using FluentAssertions;
using Moq;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Partner;
// UrboxService trùng tên ở cả Application (thin wrapper) lẫn Infrastructure (HTTP client) — hệ quả
// AppService 3-layer. Alias để chỉ đích danh SUT tầng Application, tránh CS0104 ambiguous.
using AppPartner = POS.Application.Features.Partner;

namespace POS.UnitTests.Features.Partner;

/// <summary>
/// Unit test cho thin-wrapper <see cref="UrboxService"/> (POS.Application) — sinh bởi skill
/// payment-test-generator. Xác nhận mỗi method delegate ĐÚNG 1 lần sang IUrboxAppService
/// (Infrastructure) với cùng tham số. Return type Urbox có 4 phần tử Tuple (thêm List&lt;UrboxProducts&gt;).
/// </summary>
public class UrboxServiceTests
{
    private static Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>
        UrboxResult()
        => Tuple.Create(true, "ok", new List<DataVoucherPartnerResponse>(),
                        new List<UrboxProducts>());

    [Fact]
    public async Task CheckSerialUrbox_delegatesTo_appService()
    {
        var app = new Mock<IUrboxAppService>();
        var expected = UrboxResult();
        var req = new CheckVoucherPartnerPOSRequest { Partner = "URBOX" };
        app.Setup(x => x.CheckSerialUrbox(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(expected);
        var sut = new AppPartner.UrboxService(app.Object);

        var actual = await sut.CheckSerialUrbox(req, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        app.Verify(x => x.CheckSerialUrbox(req, It.IsAny<CancellationToken>()), Times.Once);
        app.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PayCodelUrbox_delegatesTo_appService()
    {
        var app = new Mock<IUrboxAppService>();
        var expected = UrboxResult();
        var req = new UpdateStatusVoucherPartnerRequest { Partner = "URBOX" };
        app.Setup(x => x.PayCodelUrbox(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(expected);
        var sut = new AppPartner.UrboxService(app.Object);

        var actual = await sut.PayCodelUrbox(req, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        app.Verify(x => x.PayCodelUrbox(req, It.IsAny<CancellationToken>()), Times.Once);
        app.VerifyNoOtherCalls();
    }
}

using FluentAssertions;
using Moq;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Partner;
// GotITService trùng tên ở cả Application (thin wrapper) lẫn Infrastructure (HTTP client) — hệ quả
// AppService 3-layer. Alias để chỉ đích danh SUT tầng Application, tránh CS0104 ambiguous.
using AppPartner = POS.Application.Features.Partner;

namespace POS.UnitTests.Features.Partner;

/// <summary>
/// Unit test cho thin-wrapper <see cref="GotITService"/> (POS.Application) — sinh bởi skill
/// payment-test-generator. Xác nhận mỗi method delegate ĐÚNG 1 lần sang IGotITAppService
/// (Infrastructure) với cùng tham số, không thêm logic.
/// </summary>
public class GotITServiceTests
{
    [Fact]
    public async Task CheckMultiple_delegatesTo_appService()
    {
        var app = new Mock<IGotITAppService>();
        var expected = Tuple.Create(true, "ok", new List<DataVoucherPartnerResponse>());
        var req = new CheckVoucherPartnerPOSRequest { Partner = "GOTIT" };
        app.Setup(x => x.CheckMultiple(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(expected);
        var sut = new AppPartner.GotITService(app.Object);

        var actual = await sut.CheckMultiple(req, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        app.Verify(x => x.CheckMultiple(req, It.IsAny<CancellationToken>()), Times.Once);
        app.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MarkUseMultiple_delegatesTo_appService()
    {
        var app = new Mock<IGotITAppService>();
        var expected = Tuple.Create(true, "ok", new List<DataVoucherPartnerResponse>());
        var req = new UpdateStatusVoucherPartnerRequest { Partner = "GOTIT" };
        app.Setup(x => x.MarkUseMultiple(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(expected);
        var sut = new AppPartner.GotITService(app.Object);

        var actual = await sut.MarkUseMultiple(req, CancellationToken.None);

        actual.Should().BeSameAs(expected);
        app.Verify(x => x.MarkUseMultiple(req, It.IsAny<CancellationToken>()), Times.Once);
        app.VerifyNoOtherCalls();
    }
}
